using HSCentric.Const;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HSCentric
{
    internal sealed class ApiException : Exception
    {
        internal int Status { get; }
        internal ApiException(int status, string message) : base(message) { Status = status; }
    }

    internal sealed class TaskInput
    {
        public string Mode { get; set; }
        public string Start { get; set; }
        public string Stop { get; set; }
        public string TeamName { get; set; }
        public string StrategyName { get; set; }
        public string Map { get; set; }
        public int NumCore { get; set; }
        public int NumTotal { get; set; } = 6;
        public bool Scale { get; set; }
        public bool RefreshQuest { get; set; }
        public bool ClaimAchievement { get; set; }
        public bool ClaimReward { get; set; }

        internal TaskUnit ToTask(bool special = false)
        {
            TASK_MODE mode;
            if (!Enum.TryParse(Mode, out mode) || !Enum.IsDefined(typeof(TASK_MODE), mode))
                throw new ApiException(400, "请选择有效模式。");
            DateTime start, stop;
            string[] formats = { "HH:mm", "HH:mm:ss" };
            if (!DateTime.TryParseExact(Start, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out start) ||
                !DateTime.TryParseExact(Stop, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out stop))
                throw new ApiException(400, "请输入有效的开始与停止时间。");
            if (!special && start.TimeOfDay == stop.TimeOfDay) throw new ApiException(400, "开始与停止时间不能相同；跨日时段可以将停止时间设在开始时间之前。");
            if (NumCore < 0 || NumTotal < 1 || NumTotal > 6 || NumCore > NumTotal)
                throw new ApiException(400, "队伍总数须为 1–6，核心人数不能超过总数。");
            if (!Common.IsBGMode(mode) && (string.IsNullOrWhiteSpace(TeamName) || string.IsNullOrWhiteSpace(StrategyName)))
                throw new ApiException(400, "请填写队伍和策略名称。");
            return new TaskUnit { Mode = mode, StartTime = start, StopTime = stop, TeamName = TeamName ?? "", StrategyName = StrategyName ?? "",
                Map = Map ?? "", MercTeamNumCore = NumCore, MercTeamNumTotal = NumTotal, Scale = Scale,
                RefreshQuest = RefreshQuest, ClaimAchievement = ClaimAchievement, ClaimReward = ClaimReward };
        }

        internal static TaskInput From(TaskUnit task)
        {
            if (task == null) return null;
            return new TaskInput { Mode = task.Mode.ToString(), Start = task.StartTime.ToString("HH:mm:ss"), Stop = task.StopTime.ToString("HH:mm:ss"),
                TeamName = task.TeamName, StrategyName = task.StrategyName, Map = task.Map, NumCore = task.MercTeamNumCore, NumTotal = task.MercTeamNumTotal,
                Scale = task.Scale, RefreshQuest = task.RefreshQuest, ClaimAchievement = task.ClaimAchievement, ClaimReward = task.ClaimReward };
        }
    }

    internal sealed class AccountInput
    {
        public string Id { get; set; }
        public string Revision { get; set; }
        public bool Enable { get; set; }
        public string Token { get; set; }
        public string HsPath { get; set; }
        public string HbPath { get; set; }
        public int HsModPort { get; set; }
        public bool SwitchTask { get; set; }
        public List<TaskInput> Tasks { get; set; }
        public TaskInput SpecialTask { get; set; }

        internal HSUnit Validate(HSUnit existing)
        {
            Id = (Id ?? "").Trim();
            if (Id.Length == 0 || Id.Length > 100 || Id.Any(char.IsControl)) throw new ApiException(400, "账号 ID 须为 1–100 个可见字符。");
            if (existing != null && Id != existing.ID) throw new ApiException(400, "已有账号 ID 不能更改，请新建账号。");
            if (string.IsNullOrWhiteSpace(Token) && (existing == null || string.IsNullOrEmpty(existing.Token))) throw new ApiException(400, "请填写炉石 Token。");
            if (string.IsNullOrWhiteSpace(HsPath)) throw new ApiException(400, "请填写炉石程序路径。");
            if (HsModPort < 1 || HsModPort > 65535) throw new ApiException(400, "HSMod 端口须为 1–65535。");
            if (Tasks == null || Tasks.Count == 0 || Tasks.Count > 100) throw new ApiException(400, "请添加 1–100 个每日时段。");
            if (SwitchTask && SpecialTask == null) throw new ApiException(400, "启用自动换模式时，请设置替代模式。");
            var result = existing == null ? new HSUnit() : (HSUnit)existing.DeepClone();
            var tasks = new TaskManager(result, null, SpecialTask == null ? new TaskUnit() : SpecialTask.ToTask(true), SwitchTask);
            foreach (var input in Tasks)
            {
                if (input == null) throw new ApiException(400, "时段内容不能为空。");
                var task = input.ToTask();
                if (Common.IsBuddyMode(task.Mode) && string.IsNullOrWhiteSpace(HbPath)) throw new ApiException(400, "传统模式需要填写 HearthBuddy 路径。");
                if (!tasks.Add(task)) throw new ApiException(400, "每日时段存在重合，请调整开始或停止时间。");
            }
            if (SwitchTask && Common.IsBuddyMode(tasks.GetTaskSpec().Mode) && string.IsNullOrWhiteSpace(HbPath))
                throw new ApiException(400, "传统替代模式需要填写 HearthBuddy 路径。");
            result.ID = Id; result.Enable = Enable; result.HSPath = HsPath.Trim(); result.HBPath = (HbPath ?? "").Trim();
            result.HSModPort = HsModPort; result.Tasks = tasks;
            if (!string.IsNullOrWhiteSpace(Token)) result.Token = Token.Trim();
            return result;
        }

        internal static AccountInput From(HSUnit unit)
        {
            return new AccountInput { Id = unit.ID, Enable = unit.Enable, HsPath = unit.HSPath, HbPath = unit.HBPath, HsModPort = unit.HSModPort,
                SwitchTask = unit.Tasks.SwitchTask, Tasks = unit.Tasks.GetTasks().Select(TaskInput.From).ToList(), SpecialTask = TaskInput.From(unit.Tasks.GetTaskSpec()),
                Token = "", Revision = Fingerprint(unit) };
        }

        internal static string Fingerprint(HSUnit unit)
        {
            var value = new { unit.ID, unit.Enable, unit.Token, unit.HSPath, unit.HBPath, unit.HSModPort, unit.Tasks.SwitchTask,
                Tasks = unit.Tasks.GetTasks().Select(TaskInput.From), Special = TaskInput.From(unit.Tasks.GetTaskSpec()) };
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value))));
        }
    }
}
