using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using HSCentric.Const;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text.RegularExpressions;

namespace HSCentric
{

	[Serializable]
	public class HSUnit
	{
		[DllImport("kernel32.dll")]
		public static extern int WinExec(string exeName, int operType);

		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}

		public (string Mode, DateTime AwakeTime, int AwakePeriod, string TeamName, string StrategyName) BasicConfigValue
		{
			get
			{
				ReadConfigValue();
				return (m_mode, m_awakeTime, m_awakePeriod, m_teamName, m_strategyName);
			}
		}
		public bool Enable
		{
			get { return m_enable; }
			set { m_enable = value; }
		}
		public string NickName
		{
			get
			{
				return m_hsPath.Length > 0 ? Directory.GetParent(m_hsPath).Name : ""; ;
			}
		}
		public FileVerison Version
		{
			get
			{
				try
				{
					FileVersionInfo info = FileVersionInfo.GetVersionInfo(m_hsPath);
					return new FileVerison(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
				}
				catch
				{
					return new FileVerison(0,0,0,0);
				}
			}
		}
		public string Path
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


		public bool SwitchBepinEx(bool _switch)
		{
			DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath));
			foreach (FileInfo logFile in rootHS.GetFiles("*winhttp*.dll", SearchOption.TopDirectoryOnly)) //查找文件
			{
				if (true == _switch)
					logFile.MoveTo(System.IO.Path.GetDirectoryName(m_hsPath) + "/winhttp.dll");
				else
					logFile.MoveTo(System.IO.Path.GetDirectoryName(m_hsPath) + "/autostop-winhttp.dll");

				break;
			}
			return true;
		}
		public bool IsActive()
		{
			TaskUnit currentTask = CurrentTask;
			TimeSpan time_now = DateTime.Now.TimeOfDay;
			return time_now >= currentTask.StartTime.TimeOfDay && time_now <= currentTask.StopTime.TimeOfDay;
		}
		public bool IsProcessAlive()
		{
			List<Process> hearthstoneProcess = HearthstoneProcess();
			if (hearthstoneProcess.Count <= 0)
			{
				return false;
			}
			return true;
		}
		public bool IsLogUpdated()
		{
			//检查炉石日志, 5分钟没更新
			DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/Logs");
			foreach (FileInfo logFile in rootHS.GetFiles("hearthstone_*.log", SearchOption.TopDirectoryOnly)) //查找文件
			{
				double inteval = 5f;
				TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);
				if (timeSpan.TotalMinutes < inteval)
				{
					return true;
				}
			}
			return false;
		}
		public void KillHS(string msg = "")
		{
			List<Process> hearthstoneProcess = HearthstoneProcess();
			foreach (Process processHS in hearthstoneProcess)
			{
				processHS.Kill();
				Out.Log(string.Format("[{0}]结束 {1}", NickName, msg));
			}
		}
		public void StartHS(string msg = "")
		{
			if (NeedAdjustMode())
				AdjustMode();

			WinExec(m_hsPath, 2);
			Out.Log(string.Format("[{0}]启动 {1}", NickName, msg));
		}
		public bool NeedAdjustMode()
		{
			var basicConfigValue = BasicConfigValue;
			TaskUnit currentTask = CurrentTask;
			if (currentTask.Mode.ToString() != basicConfigValue.Mode ||
				currentTask.TeamName != basicConfigValue.TeamName ||
				currentTask.StrategyName != basicConfigValue.StrategyName)
			{
				return true;
			}
			return false;
		}
		public bool AdjustMode()
		{
			TaskUnit currentTask = CurrentTask;
			WriteConfigValue(currentTask.Mode, currentTask.TeamName, currentTask.StrategyName);
			Out.Log(string.Format("[{0}]切换模式[{1}][{2}][{3}]", NickName, currentTask.Mode.ToString(), currentTask.TeamName, currentTask.StrategyName));
			return true;
		}


		private List<Process> HearthstoneProcess()
		{
			List<Process> ps = new List<Process>(Process.GetProcessesByName("Hearthstone"));
			for (int i = ps.Count - 1; i >= 0; --i)
			{
				string path = "";
				try
				{
					path = ps[i].MainModule.FileName.ToString();
				}
				catch
				{
				}
				finally
				{
					if (path != m_hsPath)
						ps.RemoveAt(i);
				}

			}
			return ps;
		}
		private void ReadConfigValue()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_configLastEdit)
				return;

			m_configLastEdit = fileConfig.LastWriteTime;
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			foreach (string line in fileLines)
			{
				if (line.IndexOf("唤醒时间 = ") == 0)
				{
					try
					{
						int start_pos = "唤醒时间 = ".Length;
						if (line.Length > start_pos)
							m_awakeTime = Convert.ToDateTime(line.Substring(start_pos));
						else
							m_awakeTime = new DateTime(2000, 1, 1);
					}
					catch
					{
						m_awakeTime = new DateTime(2000, 1, 1);
					}
				}
				else if (line.IndexOf("唤醒时间间隔 = ") == 0)
				{
					try
					{
						int start_pos = "唤醒时间间隔 = ".Length;
						if (line.Length > start_pos)
							m_awakePeriod = Convert.ToInt32(line.Substring(start_pos)) * 60;
						else
							m_awakePeriod = 22 * 60;
					}
					catch
					{
						m_awakePeriod = 22 * 60;
					}
					continue;
				}
				else if (line.IndexOf("插件运行模式 = ") == 0)
				{
					try
					{
						int start_pos = "插件运行模式 = ".Length;
						if (line.Length > start_pos)
							m_mode = line.Substring(start_pos);
						else
							m_mode = "";
					}
					catch
					{
						m_mode = "";
					}
				}
				else if (line.IndexOf("使用的队伍名称 = ") == 0)
				{
					try
					{
						int start_pos = "使用的队伍名称 = ".Length;
						if (line.Length > start_pos)
							m_teamName = line.Substring(start_pos);
						else
							m_teamName = "";
					}
					catch
					{
						m_teamName = "";
					}
				}
				else if (line.IndexOf("战斗策略 = ") == 0)
				{
					try
					{
						int start_pos = "战斗策略 = ".Length;
						if (line.Length > start_pos)
							m_strategyName = line.Substring(start_pos);
						else
							m_strategyName = "";
					}
					catch
					{
						m_strategyName = "";
					}
				}
				else
					continue;
			}
		}
		private void WriteConfigValue(TASK_MODE Mode, string TeamName, string StrategyName)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			string[] fileLines = File.ReadAllLines(pathConfig.ToString());
			for (int i = 0, ii = fileLines.Length; i < ii; ++i)
			{
				if (fileLines[i].IndexOf("插件运行模式 = ") == 0)
				{
					fileLines[i] = "插件运行模式 = " + Mode.ToString();
				}
				else if (fileLines[i].IndexOf("使用的队伍名称 = ") == 0)
				{
					fileLines[i] = "使用的队伍名称 = " + TeamName;
				}
				else if (fileLines[i].IndexOf("战斗策略 = ") == 0)
				{
					fileLines[i] = "战斗策略 = " + StrategyName;
				}
			}
			File.WriteAllLines(pathConfig.ToString(), fileLines);
		}

		public void ReadLog()
		{
			DirectoryInfo pathLog = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/Log/mercenarylog@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log");
			if (!File.Exists(pathLog.ToString()))
				return;

			foreach (string line in File.ReadLines(pathLog.ToString()).Reverse<string>())
			{
				if (line.IndexOf("战令信息") > 0)
				{
					try
					{
						Regex regex = new Regex(@"^.*等级:([\d]*).*经验:([\d]*).*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 3)
						{
							m_rewardXP.Level = Convert.ToInt32(match.Groups[1].Value);
							m_rewardXP.ProgressXP = Convert.ToInt32(match.Groups[2].Value);
						}
					}
					catch
					{
					}
					return;
				}
			}
		}


		private string m_mode = "";
		private DateTime m_awakeTime = new DateTime(2000, 1, 1);
		private int m_awakePeriod = 25;
		private string m_teamName = "";
		private string m_strategyName = "";
		private RewardXP m_rewardXP = new RewardXP();

		private string m_hsPath = "";//炉石路径
		private bool m_enable = false;//启用状态
		private DateTime m_configLastEdit = new DateTime(2000,1,1);
		private TaskManager m_taskManager = new TaskManager();
	}
}
