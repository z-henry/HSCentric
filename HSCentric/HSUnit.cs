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
		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}

		public (string Mode, DateTime AwakeTime, int AwakePeriod, string TeamName, string StrategyName, bool MercPluginEnable, bool Scale, string Map, int NumCore, int NumTotal) BasicConfigValue
		{
			get
			{
				ReadConfigValue();
				return (m_CacheMode, m_CacheAwakeTime, m_CacheAwakePeriod, m_CacheTeamName, m_CacheStrategyName, m_CacheMercPluginEnable, m_CacheScale, m_CacheMap, m_CacheNumCore, m_CachNumTotal);
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



		public void SwitchMercPlugin(bool _switch)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			for (int i = 0, ii = fileLines.Length; i < ii; ++i)
			{
				if (fileLines[i].IndexOf("插件开关 = ") == 0)
				{
					fileLines[i] = "插件开关 = " + _switch.ToString();
				}
			}
			File.WriteAllLines(pathConfig.ToString(), fileLines);
		}
		public void InitHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			for (int i = 0, ii = fileLines.Length; i < ii; ++i)
			{
				if (fileLines[i].IndexOf("HsMod状态 = ") == 0)
				{
					fileLines[i] = "HsMod状态 = true";
				}
				else if (fileLines[i].IndexOf("设置模板 = ") == 0)
				{
					fileLines[i] = "设置模板 = AwayFromKeyboard";
				}
				// 				else if(fileLines[i].IndexOf("自动开盒 = ") == 0)
				// 				{
				// 					fileLines[i] = "自动开盒 = true";
				// 				}
				// 				else if (fileLines[i].IndexOf("结算展示 = ") == 0)
				// 				{
				// 					fileLines[i] = "结算展示 = false";
				// 				}
				// 				else if (fileLines[i].IndexOf("应用焦点 = ") == 0)
				// 				{
				// 					fileLines[i] = "应用焦点 = false";
				// 				}
				// 				else if (fileLines[i].IndexOf("报错退出 = ") == 0)
				// 				{
				// 					fileLines[i] = "报错退出 = true";
				// 				}
				// 				else if (fileLines[i].IndexOf("弹出消息 = ") == 0)
				// 				{
				// 					fileLines[i] = "弹出消息 = false";
				// 				}
				// 				else if (fileLines[i].IndexOf("自动领奖 = ") == 0)
				// 				{
				// 					fileLines[i] = "自动领奖 = true";
				// 				}
				// 				else if (fileLines[i].IndexOf("游戏内消息 = ") == 0)
				// 				{
				// 					fileLines[i] = "游戏内消息 = false";
				// 				}
				// 				else if (fileLines[i].IndexOf("对手卡牌特效 = ") == 0)
				// 				{
				// 					fileLines[i] = "对手卡牌特效 = false";
				// 				}
				// 				else if (fileLines[i].IndexOf("金卡特效 = ") == 0)
				// 				{
				// 					fileLines[i] = "金卡特效 = Disabled";
				// 				}
				// 				else if (fileLines[i].IndexOf("钻石卡特效 = ") == 0)
				// 				{
				// 					fileLines[i] = "钻石卡特效 = Disabled";
				// 				}
			}
			File.WriteAllLines(pathConfig.ToString(), fileLines);
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
				if (basicConfigValue.MercPluginEnable == true)
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
				if (basicConfigValue.MercPluginEnable == false ||
					currentTask.Mode.ToString() != basicConfigValue.Mode ||
					currentTask.TeamName != basicConfigValue.TeamName ||
					currentTask.StrategyName != basicConfigValue.StrategyName ||
					currentTask.Scale != basicConfigValue.Scale ||
					currentTask.Map != basicConfigValue.Map ||
					currentTask.MercTeamNumCore != basicConfigValue.NumCore ||
					currentTask.MercTeamNumTotal != basicConfigValue.NumTotal)
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
				WriteConfigValue(false);
			}
			else
			{
				WriteConfigValue(true, currentTask.Mode, currentTask.TeamName, currentTask.StrategyName, currentTask.Scale);
			}
			var basicConfigValue = BasicConfigValue;
			Out.Log(string.Format("[{0}]切换模式 [enable:{1}] [mode:{2}] [team:{3}] [strategy:{4}]",
				ID, basicConfigValue.MercPluginEnable, basicConfigValue.Mode, basicConfigValue.TeamName, basicConfigValue.StrategyName));
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
		private void ReadConfigValue()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.主进程日志])
				return;

			m_fileLastEdit[(int)FILE_TYPE.主进程日志] = fileConfig.LastWriteTime;
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			foreach (string line in fileLines)
			{
				if (line.IndexOf("唤醒时间 = ") == 0)
				{
					try
					{
						int start_pos = "唤醒时间 = ".Length;
						if (line.Length > start_pos)
							m_CacheAwakeTime = Convert.ToDateTime(line.Substring(start_pos));
						else
							m_CacheAwakeTime = new DateTime(2000, 1, 1);
					}
					catch
					{
						m_CacheAwakeTime = new DateTime(2000, 1, 1);
					}
				}
				else if (line.IndexOf("唤醒时间间隔 = ") == 0)
				{
					try
					{
						int start_pos = "唤醒时间间隔 = ".Length;
						if (line.Length > start_pos)
							m_CacheAwakePeriod = Convert.ToInt32(line.Substring(start_pos)) * 60;
						else
							m_CacheAwakePeriod = 22 * 60;
					}
					catch
					{
						m_CacheAwakePeriod = 22 * 60;
					}
					continue;
				}
				else if (line.IndexOf("插件运行模式 = ") == 0)
				{
					try
					{
						int start_pos = "插件运行模式 = ".Length;
						if (line.Length > start_pos)
							m_CacheMode = line.Substring(start_pos);
						else
							m_CacheMode = "";
					}
					catch
					{
						m_CacheMode = "";
					}
				}
				else if (line.IndexOf("使用的队伍名称 = ") == 0)
				{
					try
					{
						int start_pos = "使用的队伍名称 = ".Length;
						if (line.Length > start_pos)
							m_CacheTeamName = line.Substring(start_pos);
						else
							m_CacheTeamName = "";
					}
					catch
					{
						m_CacheTeamName = "";
					}
				}
				else if (line.IndexOf("战斗策略 = ") == 0)
				{
					try
					{
						int start_pos = "战斗策略 = ".Length;
						if (line.Length > start_pos)
							m_CacheStrategyName = line.Substring(start_pos);
						else
							m_CacheStrategyName = "";
					}
					catch
					{
						m_CacheStrategyName = "";
					}
				}
				else if (line.IndexOf("插件开关 = ") == 0)
				{
					try
					{
						int start_pos = "插件开关 = ".Length;
						if (line.Length > start_pos)
							m_CacheMercPluginEnable = Convert.ToBoolean(line.Substring(start_pos));
						else
							m_CacheMercPluginEnable = false;
					}
					catch
					{
						m_CacheMercPluginEnable = false;
					}
				}
				else if (line.IndexOf("自动齿轮加速 = ") == 0)
				{
					try
					{
						int start_pos = "自动齿轮加速 = ".Length;
						if (line.Length > start_pos)
							m_CacheScale = Convert.ToBoolean(line.Substring(start_pos));
						else
							m_CacheScale = false;
					}
					catch
					{
						m_CacheScale = false;
					}
				}
				else
					continue;
			}
		}
		private void WriteConfigValue(bool Enable, TASK_MODE? Mode = null, string TeamName = null, string StrategyName = null, bool Scale = false)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			for (int i = 0, ii = fileLines.Length; i < ii; ++i)
			{
				if (fileLines[i].IndexOf("插件运行模式 = ") == 0)
				{
					if (Mode != null)
						fileLines[i] = "插件运行模式 = " + Mode.ToString();
				}
				else if (fileLines[i].IndexOf("使用的队伍名称 = ") == 0)
				{
					if (TeamName != null)
						fileLines[i] = "使用的队伍名称 = " + TeamName;
				}
				else if (fileLines[i].IndexOf("战斗策略 = ") == 0)
				{
					if (StrategyName != null)
						fileLines[i] = "战斗策略 = " + StrategyName;
				}
				else if (fileLines[i].IndexOf("插件开关 = ") == 0)
				{
					fileLines[i] = "插件开关 = " + Enable.ToString();
				}
				else if (fileLines[i].IndexOf("自动齿轮加速 = ") == 0)
				{
					fileLines[i] = "自动齿轮加速 = " + Scale.ToString();
				}
			}
			File.WriteAllLines(pathConfig.ToString(), fileLines);
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

		public Task<string> GetHSLogPath()
		{
			var task = Task.Run(() => {
				Thread.Sleep(15*1000);
				DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(HSUnitManager.m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
				if (false == System.IO.File.Exists(pathConfig.ToString()))
					return "";
				string[] fileLines = File.ReadAllLines(pathConfig.ToString());
				foreach (var line in fileLines)
				{
					if (line.IndexOf("炉石日志 = ") == 0)
					{
						int start_pos = "炉石日志 = ".Length;
						if (line.Length > start_pos)
							return line.Substring(start_pos);
						else
							return "";
					}
				}
				return "";
			});
			return task;
		}

		//到时候弄成结构体
		private string m_CacheMode = "";
		private DateTime m_CacheAwakeTime = new DateTime(2000, 1, 1);
		private int m_CacheAwakePeriod = 25;
		private string m_CacheTeamName = "";
		private string m_CacheStrategyName = "";
		private bool m_CacheMercPluginEnable = false;
		private bool m_CacheScale = false;
		private string m_CacheMap = "2-5";
		private int m_CacheNumCore = 0;
		private int m_CachNumTotal = 6;

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
	}
}
