﻿using HSCentric.Const;
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
			set { m_enable = value; }
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
			TimeSpan time_now = DateTime.Now.TimeOfDay;
			return Enable && time_now >= currentTask.StartTime.TimeOfDay && time_now <= currentTask.StopTime.TimeOfDay;
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
				return result && LogsUpdated(m_hbLogFileDir);
			else if (Common.IsBGMode(CurrentTask.Mode))
			{
				string exeDirectory = System.IO.Path.GetDirectoryName(HSPath);
				string logDirectory = System.IO.Path.Combine(exeDirectory, "BepinEx", "Log", ID, "battlegrounds");
				return result && LogsUpdated(logDirectory);
			}
			else if (Common.IsMercMode(CurrentTask.Mode))
			{
				string exeDirectory = System.IO.Path.GetDirectoryName(HSPath);
				string logDirectory = System.IO.Path.Combine(exeDirectory, "BepinEx", "Log", ID, "mercenarylog");
				return result && LogsUpdated(logDirectory);
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
			if (true == NeedUpdateHS())
				HSUnitManager.Get().InterruptBeforeUpdate();

			if (need_hb)
			{
				int try_count = 0;
				while(false == HSSuccessLogin())
				{

					int delay = GetQueueSec();
					if (delay < 30)
						try_count++;
					if (try_count >= 5)
					{
						Out.Error($"[{ID}] HS登录检测失败");
						KillHS();
						return;
					}

					if (delay < 0)
						Out.Info(string.Format("[{0}] HS启动等待", ID, try_count));
					else
						Out.Debug(string.Format("[{0}] HS排队等待：{2}秒", ID, try_count, delay));
					await Delay(30 * 1000);
					if (pid != m_pid)
						return;
				}

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

		public void ReadMercLog()
		{
			try
			{
				// 炉石启动才读日志
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				//佣兵日志获取经验
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepinEx/Log/" + ID + "/mercenarylog/");
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("mercenarylog@*.log", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.佣兵日志] = targetFile.LastWriteTime;
				foreach (string line in File.ReadLines(targetFile.FullName).Reverse<string>())
				{
					if (line.IndexOf("战令信息") > 0)
					{
						Regex regex = new Regex(@"^.*等级:([\d]*).*经验:([\d]*).*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 3)
						{

							XPUpdate(new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							});
						}
						break;
					}
				}
			}
			catch
			{
			}
		}

		public void ReadBGLog()
		{
			try
			{
				// 炉石启动才读日志
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				// 酒馆日志路径
				var rootPath = Path.Combine(Path.GetDirectoryName(m_hsPath), "BepinEx", "Log", ID, "battlegrounds");
				if (!Directory.Exists(rootPath))
					return;

				// 找到今天的日志文件
				string todayPattern = $"battlegrounds@{DateTime.Now:yyyy-MM-dd}.log";
				var targetFile = new DirectoryInfo(rootPath)
					.GetFiles(todayPattern, SearchOption.TopDirectoryOnly)
					.FirstOrDefault();
				if (targetFile == null)
					return;

				// 判断文件有没有更新过
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.酒馆日志])
					return;
				DateTime lastCheckTime = m_fileLastEdit[(int)FILE_TYPE.酒馆日志];
				m_fileLastEdit[(int)FILE_TYPE.酒馆日志] = targetFile.LastWriteTime;

				// 先把所有行读到内存，再倒序处理
				var lines = new List<string>();
				using (var fs = new FileStream(
					targetFile.FullName,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				using (var reader = new StreamReader(fs, Encoding.UTF8))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						lines.Add(line);
					}
				}

				// 读取总经验
				for (int i = lines.Count - 1; i >= 0; i--)
				{
					var line = lines[i];
					var regex = new Regex(@"(\d{2}:\d{2}:\d{2}\.\d{3}).*战令信息.*等级:(\d+) 经验:(\d+)");
					var match = regex.Match(line);
					if (!match.Success) continue;

					// 时间比对
					if (DateTime.TryParseExact(
							match.Groups[1].Value,
							"HH:mm:ss.fff",
							null,
							DateTimeStyles.None,
							out DateTime parsedTime))
					{
						var current = DateTime.Today.Add(parsedTime.TimeOfDay);
						if (current > lastCheckTime)
						{
							XPUpdate(new RewardXP
							{
								Level = int.Parse(match.Groups[2].Value),
								ProgressXP = int.Parse(match.Groups[3].Value),
							});
						}
					}
					break; // 找到最新一条就行
				}

				// 读取部分经验
				for (int i = lines.Count - 1; i >= 0; i--)
				{
					var line = lines[i];
					var regex = new Regex(@"^(\d{2}:\d{2}:\d{2}\.\d{3})\t\[经验变动\] (.*)，传统通行证，获得经验:(\d+)$");
					var match = regex.Match(line);
					if (!match.Success) continue;

					if (DateTime.TryParseExact(
							match.Groups[1].Value,
							"HH:mm:ss.fff",
							null,
							DateTimeStyles.None,
							out DateTime parsedTime))
					{
						var current = DateTime.Today.Add(parsedTime.TimeOfDay);
						if (current > lastCheckTime)
						{
							int xp = int.Parse(match.Groups[3].Value);
							var desc = match.Groups[2].Value;
							if (desc.Contains("完成任务"))
								m_totalGaintXP_Quest += xp;
							else if (desc.Contains("完成成就"))
								m_totalGaintXP_Achieve += xp;
							else
								m_totalGaintXP_Other += xp;
						}
					}
					// 不用 break，想把最后一次“部分经验”都处理了
				}
			}
			catch (Exception ex)
			{
				// 读日志抛异常也打出来，方便定位
				Out.Error($"[{ID}] 读取酒馆日志异常: {ex.Message}\n堆栈: {ex.StackTrace}");
			}
		}

		public void ReadMercRecordLog()
		{
			try
			{
				// 炉石启动才读日志
				if (string.IsNullOrEmpty(m_hsLogFileDir))
					return;

				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepinEx/Log/" + ID);
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("gamerecord@*.log", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志] = targetFile.LastWriteTime;
				string[] lines = File.ReadAllLines(targetFile.FullName);
				if (lines.Length == 0)
					return;

				string[] lineSplit = lines.Last().Split('\t');
				m_pvpRate = int.Parse(lineSplit[2]);
			}
			catch
			{
			}
		}

		public bool ReadHBLog()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hbPath))
					return true;
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hbPath) + "/Logs");
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return true;
				List<FileInfo> testList = rootHS.GetFiles("Hearthbuddy*.txt", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return true;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.兄弟日志])
					return true;
				m_fileLastEdit[(int)FILE_TYPE.兄弟日志] = targetFile.LastWriteTime;
				System.Text.Encoding GB2312 = System.Text.Encoding.GetEncoding("GB2312");
				int anomalyCount = 0; // 计数连续出现子串"检测到异常情况，将随机点击"的次数
				foreach (string line in File.ReadLines(targetFile.FullName, System.Text.Encoding.GetEncoding("GB2312")).Reverse<string>())
				{
					if (line.IndexOf("[监控插件] 合计: 战令") > 0)
					{
						Regex regex = new Regex(@"^.*合计: 战令([\d]*)级\(([\d]*)/[\d]*\)\([\d]*/小时\)\s(.*)\s[\d]*/[\d]*.*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 4)
						{
							XPUpdate(new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							});
						}
						m_classicRate = match.Groups[3].Value;
						return true;
					}

					// 尾部连续出现“检测到异常情况，将随机点击”，需要退出
					if (line.Contains("检测到异常情况，将随机点击"))
					{
						anomalyCount++;
						if (anomalyCount >= 5) // 连续出现 5 次
						{
							return false;
						}
					}
					else
					{
						anomalyCount = 0; // 如果不连续出现，则重置计数器
					}
				}

			}
			catch
			{
			}
			return true;
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