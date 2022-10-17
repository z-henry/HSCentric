using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using HSCentric.Const;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace HSCentric
{

	[Serializable]
	public class HSUnit
	{
		[Serializable]
		public class CacheConfig
		{
			public string mode = "";
			public DateTime awakeTime = new DateTime(2000, 1, 1);
			public int awakePeriod = 25;
			public string teamName = "";
			public string strategyName = "";
			public bool mercPluginEnable = false;
			public bool scale = false;
			public string map = "2-5";
			public int numCore = 0;
			public int numTotal = 6;
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
				m_cacheConfig = ReadConfigValue() ?? m_cacheConfig;
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


		public void InitHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return; 
			Common.IniWriteValue("全局", "HsMod状态", true.ToString(), pathConfig.ToString());
			Common.IniWriteValue("全局", "设置模板", "AwayFromKeyboard", pathConfig.ToString());
		}

		public bool IsActive()
		{
			TaskUnit currentTask = CurrentTask;
			TimeSpan time_now = DateTime.Now.TimeOfDay;
			return time_now >= currentTask.StartTime.TimeOfDay && time_now <= currentTask.StopTime.TimeOfDay;
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
				return false;
			}
		}
		public bool IsLogUpdated()
		{
			if (string.IsNullOrEmpty (m_hsLogFile))
				return true;

			string rootPath = System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + '/' + m_hsLogFile;
			if (false == System.IO.File.Exists(rootPath))
				return false;

			FileInfo logFile = new FileInfo(rootPath);
			double inteval = 5f;
			TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);
			if (timeSpan.TotalMinutes < inteval)
			{
				return true;
			}
			return false;

		}
		public void KillHS(string msg = "")
		{
			HearthstoneProcess()?.Kill();
			Out.Log(string.Format("[{0}]结束 {1}", ID, msg));
			m_pid = 0;
			m_hsLogFile = "";
			Common.Delay(5 * 1000);
		}
		public async void StartHS(string msg = "")
		{
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = HSUnitManager.m_hsPath;
			process.StartInfo.Arguments += " " + m_token;
			process.StartInfo.Arguments += " --hsunitid:" + m_ID;
			process.StartInfo.Arguments += " --startmethod:hscentric";
			process.Start();
			process.WaitForInputIdle();
			m_pid = process.Id;
			m_hsLogFile = "";
			Out.Log(string.Format("[{0}]启动 {1} [pid:{2}]", ID, msg, m_pid));
			m_hsLogFile = await GetHSLogPath();
			Out.Log(string.Format("[{0}]记录炉石日志路径 {1}", ID, m_hsLogFile));
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

			Out.Log(string.Format("[{0}]启动HB {1} [pid:{2}] [arg:{3}]", ID, msg, pid, process.StartInfo.Arguments));
		}

		public bool NeedAdjustMode()
		{
			var basicConfigValue = BasicConfigValue;
			TaskUnit currentTask = CurrentTask;
			if (Common.IsBuddyMode(currentTask.Mode))
			{
				if (basicConfigValue.mercPluginEnable == true)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				if (basicConfigValue.mercPluginEnable == false ||
					currentTask.Mode.ToString() != basicConfigValue.mode ||
					currentTask.TeamName != basicConfigValue.teamName ||
					currentTask.StrategyName != basicConfigValue.strategyName ||
					currentTask.Scale != basicConfigValue.scale ||
					currentTask.Map != basicConfigValue.map ||
					currentTask.MercTeamNumCore != basicConfigValue.numCore ||
					currentTask.MercTeamNumTotal != basicConfigValue.numTotal)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		public bool AdjustMode()
		{
			TaskUnit currentTask = CurrentTask;
			if (Common.IsBuddyMode(currentTask.Mode))
			{
				WriteConfigValue(false, null);
			}
			else
			{
				WriteConfigValue(true, currentTask);
			}
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
		private CacheConfig ReadConfigValue()
		{
			CacheConfig resultConfig = new CacheConfig();
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return null;
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵配置])
				return null;
			m_fileLastEdit[(int)FILE_TYPE.佣兵配置] = fileConfig.LastWriteTime;

			resultConfig.awakeTime = Common.IniReadValue<DateTime>("配置", "唤醒时间", new DateTime(2000, 1, 1), pathConfig.ToString());
			resultConfig.awakePeriod = 60 * Common.IniReadValue<int>("配置", "唤醒时间间隔", 22, pathConfig.ToString());
			resultConfig.mode = Common.IniReadValue<string>("配置", "插件运行模式", "", pathConfig.ToString());
			resultConfig.teamName = Common.IniReadValue<string>("配置", "使用的队伍名称", "", pathConfig.ToString());
			resultConfig.strategyName = Common.IniReadValue<string>("配置", "战斗策略", "", pathConfig.ToString());
			resultConfig.mercPluginEnable = Common.IniReadValue<bool>("配置", "插件开关", false, pathConfig.ToString());
			resultConfig.scale = Common.IniReadValue<bool>("配置", "自动齿轮加速", false, pathConfig.ToString());
			resultConfig.map = Common.IniReadValue<string>("配置", "要刷的地图", "2-5", pathConfig.ToString());
			resultConfig.numCore = Common.IniReadValue<int>("配置", "队伍核心人数", 0, pathConfig.ToString());
			resultConfig.numTotal = Common.IniReadValue<int>("配置", "总队伍人数", 6, pathConfig.ToString());

// 			Out.Log($"[{ID}]更新缓存 awakeTime:{resultConfig.awakeTime} awakePeriod:{resultConfig.awakePeriod} " +
// 				$"mode:{resultConfig.mode} teamName:{resultConfig.teamName} strategyName:{resultConfig.strategyName} " +
// 				$"mercPluginEnable:{resultConfig.mercPluginEnable} scale:{resultConfig.scale} map:{resultConfig.map} " +
// 				$"numCore:{resultConfig.numCore} numTotal:{resultConfig.numTotal}"
// 				);
			return resultConfig;
		}

		private void WriteConfigValue(bool Enable, TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			Common.IniWriteValue("配置", "插件开关", Enable.ToString(), pathConfig.ToString());
			if (task != null)
			{
				Common.IniWriteValue("配置", "插件运行模式", task.Mode.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "使用的队伍名称", task.TeamName.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "战斗策略", task.StrategyName.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "自动齿轮加速", task.Scale.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "要刷的地图", task.Map.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "总队伍人数", task.MercTeamNumTotal.ToString(), pathConfig.ToString());
				Common.IniWriteValue("配置", "队伍核心人数", task.MercTeamNumCore.ToString(), pathConfig.ToString());
			}
			Out.Log($"[{ID}]写入配置 mode:{task.Mode} teamName:{task.TeamName} strategyName:{task.StrategyName} " +
				$"Enable:{Enable} Scale:{task.Scale} Map:{task.Map} " +
				$"MercTeamNumTotal:{task.MercTeamNumTotal} MercTeamNumCore:{task.MercTeamNumCore}"
				);
		}

		public Task<string> GetHSLogPath()
		{
			var task = Task.Run(() =>
			{
				Thread.Sleep(15 * 1000);
				DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
				if (false == System.IO.File.Exists(pathConfig.ToString()))
					return "";

				return Common.IniReadValue<string>("开发", "炉石日志", "", pathConfig.ToString());
			});
			return task;
		}


		public void ReadMercLog()
		{
			try
			{
				//佣兵日志获取经验
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepinEx/Log/" + ID);
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
							RewardXP rewardXP = new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							};
							m_rewardXP = m_rewardXP.TotalXP > rewardXP.TotalXP ? m_rewardXP : rewardXP;
						}
						break;
					}
				}
			}
			catch
			{
			}
		}
		public void ReadMercRecordLog()
		{
			try
			{
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepinEx/Log/" + ID);
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

		public void ReadHBLog()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hbPath))
					return;
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hbPath) + "/Logs");
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("Hearthbuddy*.txt", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.兄弟日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.兄弟日志] = targetFile.LastWriteTime;
				System.Text.Encoding GB2312 = System.Text.Encoding.GetEncoding("GB2312");
				foreach (string line in File.ReadLines(targetFile.FullName, System.Text.Encoding.GetEncoding("GB2312")).Reverse<string>())
				{
					if (line.IndexOf("[监控插件] 合计: 战令") > 0)
					{
						Regex regex = new Regex(@"^.*合计: 战令([\d]*)级\(([\d]*)/[\d]*\)\([\d]*/小时\)\s(.*)\s[\d]*/[\d]*.*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 4)
						{
							RewardXP rewardXP = new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							};
							m_rewardXP = m_rewardXP.TotalXP > rewardXP.TotalXP ? m_rewardXP : rewardXP;
						}
						m_classicRate = match.Groups[3].Value;
						return;
					}
				}
			}
			catch
			{
			}
		}

		private RewardXP m_rewardXP = new RewardXP();
		private int m_pvpRate = 0;
		private string m_classicRate = "";
		private string m_hbPath = "";//hb路径
		private string m_token = "";//token
		private int m_pid = 0;//进程id
		private string m_hsLogFile = "";//炉石进程对应的日志
		private string m_ID = "";//自定id
		private bool m_enable = false;//启用状态
		private TaskManager m_taskManager = new TaskManager();
		private DateTime[] m_fileLastEdit = new DateTime[(int)FILE_TYPE.Total]{
			new DateTime(2000,1,1),
			new DateTime(2000,1,1),
			new DateTime(2000,1,1),
			new DateTime(2000,1,1)
		};
		private CacheConfig m_cacheConfig = new CacheConfig();
	}
}
