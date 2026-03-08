using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace HSCentric
{
	[Serializable]
	public class HSUnit
	{
		public HSUnit()
		{
			m_taskManager = new TaskManager(this, null, new TaskUnit(), false);
		}

		[Serializable]
		public class CacheConfig
		{
			[Serializable]
			public class MercCacheConfig
			{
				public string mode = "";
				public DateTime awakeTime = new DateTime(2000, 1, 1);
				public int awakePeriod = 25;
				public string teamName = "";
				public string strategyName = "";
				public bool PluginEnable = false;
				public bool scale = false;
				public string map = "2-5";
				public int numCore = 0;
				public int numTotal = 6;
			}
			[Serializable]
			public class BGCacheConfig
			{
				public bool PluginEnable = false;
			}

			public MercCacheConfig mercCacheConfig = new MercCacheConfig();
			public BGCacheConfig bgCacheConfig = new BGCacheConfig();
		}

		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}

		public CacheConfig BasicConfigValue
		{
			get
			{
				m_cacheConfig.mercCacheConfig = ReadConfigValue_Merc() ?? m_cacheConfig.mercCacheConfig;
				m_cacheConfig.bgCacheConfig = ReadConfigValue_BG() ?? m_cacheConfig.bgCacheConfig;
				return m_cacheConfig;
			}
		}

		public bool Enable
		{
			get { return m_enable; }
			set
			{
				if (m_enable != value && value == true)
				{
					for (int i = 0; i < m_fileLastEdit.Length; ++i)
						m_fileLastEdit[i] = DateTime.Now;
				}

				m_enable = value;

			}
		}

		public string ID
		{
			get { return m_ID; }
			set { m_ID = value; }
		}

		public string Token
		{
			get { return m_token; }
			set { m_token = value; }
		}

		public string HBPath
		{
			get { return m_hbPath; }
			set { m_hbPath = value; }
		}

		public string HSPath
		{
			get { return m_hsPath; }
			set { m_hsPath = value; }
		}

		public TaskUnit CurrentTask
		{
			get { return Tasks.GetCurrentTask(); }
		}

		public TaskManager Tasks
		{
			get { return m_taskManager; }
			set { m_taskManager = value; }
		}

		public RewardXP XP
		{
			get { return m_rewardXP; }
			set { m_rewardXP = value; }
		}

		public string XPRate
		{
			get 
			{
				if (m_totalRunningTime == 0)
					return "0+0+0";
				return $"{((double)(m_totalGaintXP - m_totalGaintXP_Quest - m_totalGaintXP_Achieve - m_totalGaintXP_Other) / m_totalRunningTime * 3600):0}+" +
					$"{((double)(m_totalGaintXP_Quest) / m_totalRunningTime * 3600):0}+" +
					$"{((double)(m_totalGaintXP_Achieve + m_totalGaintXP_Other) / m_totalRunningTime * 3600):0}";
			}
		}

		public Int64 TotalRunningTime
		{
			get { return m_totalRunningTime; }
			set { m_totalRunningTime = value; }
		}

		public Int64 TotalGaintXP
		{
			get { return m_totalGaintXP; }
			set { m_totalGaintXP = value; }
		}

		public Int64 TotalGaintXP_Quest
		{
			get { return m_totalGaintXP_Quest; }
			set { m_totalGaintXP_Quest = value; }
		}

		public Int64 TotalGaintXP_Achieve
		{
			get { return m_totalGaintXP_Achieve; }
			set { m_totalGaintXP_Achieve = value; }
		}

		public Int64 TotalGaintXP_Other
		{
			get { return m_totalGaintXP_Other; }
			set { m_totalGaintXP_Other = value; }
		}

		public string ClassicRate
		{
			get { return m_classicRate; }
			set { m_classicRate = value; }
		}

		public int MercPvpRate
		{
			get { return m_pvpRate; }
			set { m_pvpRate = value; }
		}

		public int HSModPort
		{
			get { return m_hsmodPort; }
			set { m_hsmodPort = value; }
		}

		public int StatsMonth
		{
			get { return m_statsMonth; }
			set { m_statsMonth = value; }
		}
		public DateTime LastXPUpdateTime
		{
			get { return m_lastXPUpdateTime; }
			set
			{
				if (m_lastXPUpdateTime != value)
				{
					if (value == DateTime.MaxValue)
						Out.Debug(string.Format($"[{ID}] 更新经验效率：关闭"));
					else if (m_lastXPUpdateTime == DateTime.MaxValue)
						Out.Debug(string.Format($"[{ID}] 更新经验效率：开启"));
				}
				m_lastXPUpdateTime = value; 
			}
		}

		public void InitConfig()
		{
			InitMercPlugin();
			InitHsMod();
			InitBGPlugin();
		}

		private void InitMercPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
			{
				// 获取文件的目录路径
				string directory = System.IO.Path.GetDirectoryName(pathConfig.ToString());

				// 如果目录不存在，创建目录
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// 创建文件并写入空内容或初始内容，UTF-8 编码
				using (StreamWriter sw = new StreamWriter(pathConfig.ToString(), false, new UTF8Encoding(false)))
				{
					sw.Write("");  // 可以在这里写入默认内容，如果不需要可以留空
				}
			}

			MyConfig.WriteIniValue("配置", "是否自动升级技能", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("配置", "是否自动制作佣兵", true.ToString(), pathConfig.ToString());
		}

		private void InitBGPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/Battlegrounds.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
			{
				// 获取文件的目录路径
				string directory = System.IO.Path.GetDirectoryName(pathConfig.ToString());

				// 如果目录不存在，创建目录
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// 创建文件并写入空内容或初始内容，UTF-8 编码
				using (StreamWriter sw = new StreamWriter(pathConfig.ToString(), false, new UTF8Encoding(false)))
				{
					sw.Write("");  // 可以在这里写入默认内容，如果不需要可以留空
				}
			}

			MyConfig.WriteIniValue("配置", "前四摆烂", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("配置", "维持分段", "2000", pathConfig.ToString());
			MyConfig.WriteIniValue("配置", "快捷键调试", false.ToString(), pathConfig.ToString());
		}

		private void InitHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == File.Exists(pathConfig.ToString()))
			{
				// 获取文件的目录路径
				string directory = System.IO.Path.GetDirectoryName(pathConfig.ToString());

				// 如果目录不存在，创建目录
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// 创建文件并写入空内容或初始内容，UTF-8 编码
				using (StreamWriter sw = new StreamWriter(pathConfig.ToString(), false, new UTF8Encoding(false)))
				{
					sw.Write("");  // 可以在这里写入默认内容，如果不需要可以留空
				}
			}

			MyConfig.WriteIniValue("全局", "HsMod状态", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("全局", "设置模板", "AwayFromKeyboard", pathConfig.ToString());
 			MyConfig.WriteIniValue("全局", "游戏帧率", "15", pathConfig.ToString());
			MyConfig.WriteIniValue("优化", "自动置换卡牌", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("炉石", "快速战斗", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("开发", "网站端口", m_hsmodPort.ToString(), pathConfig.ToString());
			
		}

		public void ReleaseConfig()
		{
			ReleaseMercPlugin();
			ReleaseHsMod();
		}
				

		private void ReleaseMercPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (true == File.Exists(pathConfig.ToString()))
				MyConfig.WriteIniValue("配置", "插件开关", false.ToString(), pathConfig.ToString());
		}

		private void ReleaseBGPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/Battlegrounds.cfg");
			if (true == File.Exists(pathConfig.ToString()))
				MyConfig.WriteIniValue("配置", "插件开关", false.ToString(), pathConfig.ToString());
		}

		private void ReleaseHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (true == File.Exists(pathConfig.ToString()))
				MyConfig.WriteIniValue("全局", "HsMod状态", false.ToString(), pathConfig.ToString());
		}


		public bool IsActive()
		{
			TaskUnit currentTask = CurrentTask;
			TimeSpan now = DateTime.Now.TimeOfDay;
			TimeSpan start = currentTask.StartTime.TimeOfDay;
			TimeSpan stop = currentTask.StopTime.TimeOfDay;

			if (!Enable)
				return false;

			if (stop >= start)
			{
				// 正常同一天区间
				return now >= start && now <= stop;
			}
			else
			{
				// 跨天区间，例如 23:00 - 02:00
				return now >= start || now <= stop;
			}
		}

		public bool IsProcessAlive()
		{
			if (HearthstoneProcess() != null)
			{
				return true;
			}
			else
			{
				m_pid = 0;
				m_hsLogFileDir = "";
				m_hbLogFileDir = "";
				return false;
			}
		}

		public bool IsLogUpdated()
		{
			//炉石还没启动
			if (string.IsNullOrEmpty(m_hsLogFileDir))
				return true;

			bool result = LogsUpdated(m_hsLogFileDir);
			if (Common.IsBuddyMode(CurrentTask.Mode))
				return LogsUpdated(m_hbLogFileDir);
			else if (Common.IsBGMode(CurrentTask.Mode))
			{
				string exeDirectory = System.IO.Path.GetDirectoryName(HSPath);
				string logDirectory = System.IO.Path.Combine(exeDirectory, "BepinEx", "Log", ID, "battlegrounds");
				return LogsUpdated(logDirectory);
			}
			else if (Common.IsMercMode(CurrentTask.Mode))
			{
				string exeDirectory = System.IO.Path.GetDirectoryName(HSPath);
				string logDirectory = System.IO.Path.Combine(exeDirectory, "BepinEx", "Log", ID, "mercenarylog");
				return LogsUpdated(logDirectory);
			}

			return result;
		}

		public bool LogsUpdated(string log_dir)
		{
			if (string.IsNullOrEmpty(log_dir))
				return true;

			// 如果目录不存在，返回 false
			if (!Directory.Exists(log_dir))
				return false;

			string[] logFiles = Directory.GetFiles(log_dir);
			double interval = 5f;

			// 遍历每个文件，检查修改时间
			foreach (string filePath in logFiles)
			{
				FileInfo logFile = new FileInfo(filePath);
				TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);

				// 如果有文件在指定间隔内被修改过，返回 true
				if (timeSpan.TotalMinutes < interval)
				{
					return true;
				}
			}
			// 如果没有任何文件在指定间隔内被修改过，返回 false
			return false;
		}

		public void KillHS()
		{
			try
			{
				HearthstoneProcess()?.Kill();
				Out.Info($"[{ID}] 关闭进程");
				m_pid = 0;
				m_hsLogFileDir = "";
				m_hbLogFileDir = "";
			}
			catch (Exception ex)
			{
				Out.Error($"[{ID}] 关闭进程失败：" + ex.Message);
			}

			Common.Delay(5 * 1000);
		}

		// 声明 Win32 API 函数
		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		public async void Start(string msg, bool need_hb)
		{
			StartHS(msg);
			int pid = m_pid;
			await Delay(30*1000);
			if (pid != m_pid)
				return;
			m_hsLogFileDir = GetHSLogPath();
			Out.Debug(string.Format("[{0}] 记录炉石日志路径 {1}", ID, m_hsLogFileDir));

			//最小化窗口
			Process proc = Process.GetProcessById(m_pid);
			while (proc.MainWindowHandle == IntPtr.Zero)
			{
				Thread.Sleep(100);
				proc.Refresh();
			}
			const int SW_MINIMIZE = 6;
			IntPtr hWnd = proc.MainWindowHandle;
			ShowWindow(hWnd, SW_MINIMIZE);
			Out.Debug(string.Format("[{0}] 最小化窗口", ID));

			//升级
			await Delay(30 * 1000);
			while (false == HSSuccessLogin())
			{
				if (pid != m_pid)
					return;

				if (true == NeedUpdateHS())
				{
					HSUnitManager.Get().InterruptBeforeUpdate();
					return;
				}
				else
				{
					Out.Debug(string.Format("[{0}] HS启动等待", ID));
					await Delay(10 * 1000);
				}
			}
			Out.Info(string.Format("[{0}] HS正常运行", ID));

			if (need_hb)
			{
				StartHB(msg);
				await Delay(30 * 1000);
				m_hbLogFileDir = System.IO.Path.GetDirectoryName(m_hbPath) + "/Logs";
			}
		}

		public void StartHS(string msg = "")
		{
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = m_hsPath;
			process.StartInfo.Arguments += " " + m_token;
			process.StartInfo.Arguments += " --hsunitid:" + m_ID;
			process.StartInfo.Arguments += " --startmethod:hscentric";
			string path_record = System.IO.Path.Combine("BepinEX", "Log", m_ID, "gamerecord@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log");
			process.StartInfo.Arguments += " --matchPath:" + path_record;
			process.Start();
			process.WaitForInputIdle();
			m_pid = process.Id;
			m_hsLogFileDir = "";
			Out.Info(string.Format("[{0}] 启动 {1} [pid:{2}]", ID, msg, m_pid));
		}

		public void StartHB(string msg = "")
		{
			var currentTask = CurrentTask;
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = HBPath;
			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(HBPath);
			process.StartInfo.Arguments = "--autostart --config:Default";
			process.StartInfo.Arguments += " --pid:" + m_pid.ToString();
			process.StartInfo.Arguments += " --deck:" + currentTask.TeamName;
			process.StartInfo.Arguments += " --behavior:" + ((int)(BEHAVIOR_MODE)Enum.Parse(typeof(BEHAVIOR_MODE), currentTask.StrategyName)).ToString();
			process.StartInfo.Arguments += " --rule:" + ((int)currentTask.Mode - (int)TASK_MODE.狂野).ToString();
			process.StartInfo.Arguments += " --os:10";
			process.Start();
			process.WaitForInputIdle();
			int pid = process.Id;
			m_hbLogFileDir = "";

			Out.Info(string.Format("[{0}] 启动HB {1} [pid:{2}] [arg:{3}]", ID, msg, pid, process.StartInfo.Arguments));
		}

		public bool NeedAdjustMode()
		{
			var basicConfigValue = BasicConfigValue;
			TaskUnit currentTask = CurrentTask;
			if (Common.IsBuddyMode(currentTask.Mode))
			{
				return basicConfigValue.mercCacheConfig.PluginEnable ||
					basicConfigValue.bgCacheConfig.PluginEnable;
			}
			else if (Common.IsMercMode(currentTask.Mode))
			{
				if (basicConfigValue.mercCacheConfig.PluginEnable == false ||
					currentTask.Mode.ToString() != basicConfigValue.mercCacheConfig.mode ||
					currentTask.TeamName != basicConfigValue.mercCacheConfig.teamName ||
					currentTask.StrategyName != basicConfigValue.mercCacheConfig.strategyName ||
					currentTask.Scale != basicConfigValue.mercCacheConfig.scale ||
					currentTask.Map != basicConfigValue.mercCacheConfig.map ||
					currentTask.MercTeamNumCore != basicConfigValue.mercCacheConfig.numCore ||
					currentTask.MercTeamNumTotal != basicConfigValue.mercCacheConfig.numTotal ||
					basicConfigValue.bgCacheConfig.PluginEnable)
				{
					return true;
				}
				return false;
			}
			else if (Common.IsBGMode(currentTask.Mode))
			{
				if (basicConfigValue.bgCacheConfig.PluginEnable == false || 
					basicConfigValue.mercCacheConfig.PluginEnable)
				{
					return true;
				}
				return false;
			}
			return false;
		}

		public bool AdjustMode()
		{
			TaskUnit currentTask = CurrentTask;
			WriteConfigValue(currentTask);
			var basicConfigValue = BasicConfigValue;
			return true;
		}

		private Process HearthstoneProcess()
		{
			if (m_pid == 0)
				return null;

			Process target = null;
			try
			{
				target = Process.GetProcessById(m_pid);
			}
			catch
			{
				return null;
			}
			return target;
		}

		private CacheConfig.MercCacheConfig ReadConfigValue_Merc()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return null;
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵配置])
				return null;
			m_fileLastEdit[(int)FILE_TYPE.佣兵配置] = fileConfig.LastWriteTime;

			CacheConfig.MercCacheConfig resultConfig = new CacheConfig.MercCacheConfig();
			resultConfig.awakeTime = MyConfig.ReadIniValue<DateTime>("配置", "唤醒时间", new DateTime(2000, 1, 1), pathConfig.ToString());
			resultConfig.awakePeriod = 60 * MyConfig.ReadIniValue<int>("配置", "唤醒时间间隔", 22, pathConfig.ToString());
			resultConfig.mode = MyConfig.ReadIniValue<string>("配置", "插件运行模式", "", pathConfig.ToString());
			resultConfig.teamName = MyConfig.ReadIniValue<string>("配置", "使用的队伍名称", "", pathConfig.ToString());
			resultConfig.strategyName = MyConfig.ReadIniValue<string>("配置", "战斗策略", "", pathConfig.ToString());
			resultConfig.PluginEnable = MyConfig.ReadIniValue<bool>("配置", "插件开关", false, pathConfig.ToString());
			resultConfig.scale = MyConfig.ReadIniValue<bool>("配置", "自动齿轮加速", false, pathConfig.ToString());
			resultConfig.map = MyConfig.ReadIniValue<string>("配置", "要刷的地图", "2-5", pathConfig.ToString());
			resultConfig.numCore = MyConfig.ReadIniValue<int>("配置", "队伍核心人数", 0, pathConfig.ToString());
			resultConfig.numTotal = MyConfig.ReadIniValue<int>("配置", "总队伍人数", 6, pathConfig.ToString());

			return resultConfig;
		}
		private CacheConfig.BGCacheConfig ReadConfigValue_BG()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/Battlegrounds.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return null;
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.酒馆配置])
				return null;
			m_fileLastEdit[(int)FILE_TYPE.酒馆配置] = fileConfig.LastWriteTime;

			CacheConfig.BGCacheConfig resultConfig = new CacheConfig.BGCacheConfig();
			resultConfig.PluginEnable = MyConfig.ReadIniValue<bool>("配置", "插件开关", false, pathConfig.ToString());

			return resultConfig;
		}

		private void WriteConfigValue(TaskUnit task)
		{
			WriteConfigHSMod(task);
			WriteConfigValueMercPlugin(task);
			WriteConfigValueBGPlugin(task);
			Out.Debug($"[{ID}] 写入配置 mode:{task?.Mode} teamName:{task?.TeamName} strategyName:{task?.StrategyName} " +
				$"Enable:{Enable} Scale:{task?.Scale} Map:{task?.Map} " +
				$"MercTeamNumTotal:{task?.MercTeamNumTotal} MercTeamNumCore:{task?.MercTeamNumCore}" +
				$"ClaimReward:{task?.ClaimReward} ClaimAchievement:{task?.ClaimAchievement} RefreshQuest:{task?.RefreshQuest} "
				);
		}

		private void WriteConfigHSMod(TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			// 佣兵模式有自己的齿轮
			if (Common.IsMercMode(task.Mode))
				MyConfig.WriteIniValue("全局", "变速齿轮状态", task.Scale.ToString(), pathConfig.ToString());
			else
				MyConfig.WriteIniValue("全局", "变速齿轮状态", false.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("全局", "变速倍率", "8", pathConfig.ToString());
		}

		private void WriteConfigValueMercPlugin(TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			bool Enable = Common.IsMercMode(task.Mode);
			MyConfig.WriteIniValue("配置", "插件开关", Enable.ToString(), pathConfig.ToString());
			if (Enable == true)
			{
				MyConfig.WriteIniValue("配置", "插件运行模式", task.Mode.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "使用的队伍名称", task.TeamName.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "战斗策略", task.StrategyName.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "自动齿轮加速", task.Scale.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "要刷的地图", task.Map.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "总队伍人数", task.MercTeamNumTotal.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "队伍核心人数", task.MercTeamNumCore.ToString(), pathConfig.ToString());
			}
		}

		private void WriteConfigValueBGPlugin(TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/Battlegrounds.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			bool Enable = Common.IsBGMode(task.Mode);
			MyConfig.WriteIniValue("配置", "插件开关", Enable.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("自动", "刷新任务", task.RefreshQuest.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("自动", "领取成就", task.ClaimAchievement.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("自动", "领取奖励", task.ClaimReward.ToString(), pathConfig.ToString());
		}

		public async Task Delay(int milliseconds)
		{
			await Task.Delay(milliseconds);
		}

		public string GetHSLogPath()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return "";

			return MyConfig.ReadIniValue<string>("开发", "炉石日志", "", pathConfig.ToString());

		}

		private IHSStatusReader GetStatusReader()
		{
			return HSStatusReaderFactory.Create(
				new HSStatusReaderContext(
					ID,
					HSPath,
					HBPath,
					HSModPort,
					GetFileLastEditTime,
					SetFileLastEditTime));
		}

		private DateTime GetFileLastEditTime(FILE_TYPE fileType)
		{
			return m_fileLastEdit[(int)fileType];
		}

		private void SetFileLastEditTime(FILE_TYPE fileType, DateTime value)
		{
			m_fileLastEdit[(int)fileType] = value;
		}

		private void ApplyRewardXP(RewardXP rewardXP)
		{
			if (rewardXP == null)
				return;
			XPUpdate(rewardXP);
		}

		private void ApplyBattlegroundsStatus(BattlegroundsStatusSnapshot status)
		{
			if (status == null)
				return;

			ApplyRewardXP(status.RewardXP);
			if (status.PvpRate.HasValue)
			{
				m_pvpRate = status.PvpRate.Value;
			}
			m_totalGaintXP_Quest += status.QuestXpGain;
			m_totalGaintXP_Achieve += status.AchievementXpGain;
			m_totalGaintXP_Other += status.OtherXpGain;
		}

		private void ApplyMercenaryRecord(MercenaryRecordSnapshot status)
		{
			if (status == null || !status.PvpRate.HasValue)
				return;

			m_pvpRate = status.PvpRate.Value;
		}

		private bool ApplyBuddyStatus(BuddyStatusSnapshot status)
		{
			if (status == null)
				return true;

			ApplyRewardXP(status.RewardXP);
			if (!string.IsNullOrWhiteSpace(status.ClassicRate))
			{
				m_classicRate = status.ClassicRate;
			}
			return !status.HasAnomaly;
		}

		public void RefreshMercenaryStatus()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				MercenaryStatusSnapshot status;
				if (GetStatusReader().TryReadMercenaryStatus(out status))
				{
					ApplyRewardXP(status.RewardXP);
				}
			}
			catch
			{
			}
		}

		public void RefreshBattlegroundsStatus()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				BattlegroundsStatusSnapshot status;
				if (GetStatusReader().TryReadBattlegroundsStatus(out status))
				{
					ApplyBattlegroundsStatus(status);
				}
			}
			catch (Exception ex)
			{
				Out.Error($"[{ID}] 读取酒馆状态异常: {ex.Message}\n堆栈: {ex.StackTrace}");
			}
		}

		public void RefreshMercenaryRecord()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				MercenaryRecordSnapshot status;
				if (GetStatusReader().TryReadMercenaryRecord(out status))
				{
					ApplyMercenaryRecord(status);
				}
			}
			catch
			{
			}
		}

		public bool RefreshBuddyStatus()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hbPath))
					return true;

				BuddyStatusSnapshot status;
				if (GetStatusReader().TryReadBuddyStatus(out status))
				{
					return ApplyBuddyStatus(status);
				}
			}
			catch
			{
			}
			return true;
		}

		public void CallConcedeAndClose()
		{
			try
			{
				GetStatusReader().TryCallConcedeAndClose();
			}
			catch
			{
			}
		}

		public void CallConcedeAndCloseByApi()
		{
			CallConcedeAndClose();
		}

		public bool? ReadHSLog()
		{
			try
			{
				string logFilePath = System.IO.Path.Combine(m_hsLogFileDir, "Hearthstone.log");
				if (!System.IO.File.Exists(logFilePath))
				{
					return null;
				}

				int anomalyCount = 0;
				// 用 FileStream + FileShare 允许其他进程写也能读
				using (var fs = new FileStream(
					logFilePath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				using (var reader = new StreamReader(fs, Encoding.UTF8))
				{
					// 读所有行并倒序
					var lines = new List<string>();
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						lines.Add(line);
					}
					for (int i = lines.Count - 1; i >= 0; i--)
					{
						line = lines[i];

						if (line.Contains("Network.DisconnectFromGameServer()"))
						{
							anomalyCount++;
							if (anomalyCount >= 20)
							{
								Out.Error($"[{ID}] 持续无法连接服务器");
								return false;
							}
						}
						else
						{
							anomalyCount = 0;
						}

						if (line.Contains("无法通过暴雪战网服务进行登录。请等待几分钟并再次尝试"))
						{
							Out.Error($"[{ID}] 无法通过暴雪战网服务进行登录");
							return false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Out.Error($"[{ID}] 读取日志异常: {ex.Message}\n堆栈: {ex.StackTrace}");
			}
			return true;
		}

		public bool NeedUpdateHS()
		{
			string logFilePath = System.IO.Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return false;

			// 打开文件，允许其他进程同时访问（如日志进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 使用栈来倒序处理文件中的行
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();

					// 检查是否包含指定的更新提示信息
					if (line.Contains("《炉石传说》已更新，请下载最新版本。"))
					{
						return true;
					}
				}
			}

			return false;
		}
		public bool HSSuccessLogin()
		{
			string logFilePath = System.IO.Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return false;

			// 打开文件，允许其他进程同时访问（如写入日志的进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 倒序读取文件内容：由于 StreamReader 不能倒序读取，我们可以采用一个反向扫描的方法。
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();
					if (line.Contains("[Startup] Startup stage LaunchGame"))
					{
						return true;
					}
				}
			}

			return false;
		}
		public int GetQueueSec()
		{
			string logFilePath = System.IO.Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return 0;

			// 打开文件，允许其他进程同时访问（如日志进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 使用栈来倒序处理文件中的行
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();

					// 判断是否包含指定的更新提示信息
					if (line.Contains("当前排队人数"))
					{
						// 正则表达式匹配 "预计" 后面的数字
						string pattern = @"(\d+秒)";

						// 使用正则表达式提取数字
						Match match = Regex.Match(line, pattern);
						if (match.Success)
						{
							return int.Parse(match.Groups[1].Value);
						}
						else
						{
							return -1;  // 如果正则匹配失败，返回 -1 表示错误
						}
					}
				}
			}
			return -1;
		}


		public void UpdateStatsMonth()
		{
			if (m_statsMonth == DateTime.Now.Month)
				return;

			DateTime check_point = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 1, 0, 0);
			if (DateTime.Now > check_point)
			{
				m_statsMonth = DateTime.Now.Month;
				m_pvpRate = 0;
				m_classicRate = "";
				m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志] = DateTime.Now;
				m_fileLastEdit[(int)FILE_TYPE.兄弟日志] = DateTime.Now;
			}
		}

		private void XPUpdate(RewardXP rewardXP)
		{
			TimeSpan time_span = DateTime.Now - LastXPUpdateTime;
			int xp_gaint = rewardXP.TotalXP-m_rewardXP.TotalXP;
			if (time_span.TotalSeconds >= 0)
			{
				m_totalRunningTime += (int)time_span.TotalSeconds;
				m_totalGaintXP += xp_gaint;
				if (xp_gaint > 0)
					Out.Debug(string.Format($"[{ID}] 更新经验效率：经验增量[{xp_gaint}]，效率[{XPRate}]"));
			}
			LastXPUpdateTime = DateTime.Now;
			m_rewardXP = rewardXP;
		}

		private DateTime m_lastXPUpdateTime = DateTime.MaxValue;
		private Int64 m_totalRunningTime = 0;
		private Int64 m_totalGaintXP = 0;
		private Int64 m_totalGaintXP_Quest = 0;
		private Int64 m_totalGaintXP_Achieve = 0;
		private Int64 m_totalGaintXP_Other = 0;
		private RewardXP m_rewardXP = new RewardXP();
		private int m_pvpRate = 0;
		private string m_classicRate = "";
		private string m_hbPath = "";//hb路径
		private string m_hsPath = "";
		private string m_token = "";//token
		private int m_pid = 0;//进程id
		private string m_hsLogFileDir = "";//炉石进程对应的日志
		private string m_hbLogFileDir = "";//HB进程对应的日志
		private string m_ID = "";//自定id
		private bool m_enable = false;//启用状态
		private int m_hsmodPort = 58744;//hsmod端口
		private TaskManager m_taskManager;

		private DateTime[] m_fileLastEdit = new DateTime[(int)FILE_TYPE.Total]{
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
		};

		private CacheConfig m_cacheConfig = new CacheConfig();
		private int m_statsMonth = -1;
		public int m_consecutiveFailureCount = 0;
	}
}
