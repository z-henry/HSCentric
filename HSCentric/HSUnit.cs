using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using HSCentric.Const;

namespace HSCentric
{
	[Serializable]
	public class HSUnit
	{
		public HSUnit(string _HSPath = null, bool _Enable = false, List<TaskUnit> _Tasks = null)
		{
			m_HSPath = _HSPath ?? "";
			m_Enable = _Enable;
			m_taskManager = new TaskManager(_Tasks);
		}

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
				return (m_Mode, m_AwakeTime, m_AwakePeriod, m_TeamName, m_StrategyName);
			}
		}
		public bool Enable
		{
			get { return m_Enable; }
			set { m_Enable = value; }
		}
		public string NickName
		{
			get
			{
				return m_HSPath.Length > 0 ? Directory.GetParent(m_HSPath).Name : ""; ;
			}
		}
		public FileVerison Version
		{
			get
			{
				try
				{
					FileVersionInfo info = FileVersionInfo.GetVersionInfo(m_HSPath);
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
			get { return m_HSPath; }
			set
			{
				m_HSPath = value;
			}
		}
		public TaskUnit CurrentTask
		{
			get { return Tasks.GetCurrentTask(); }
		}
		public TaskManager Tasks
		{
			get { return m_taskManager; }
		}

		public bool SwitchBepinEx(bool _switch)
		{
			DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_HSPath));
			foreach (FileInfo logFile in rootHS.GetFiles("*winhttp*.dll", SearchOption.TopDirectoryOnly)) //查找文件
			{
				if (true == _switch)
					logFile.MoveTo(System.IO.Path.GetDirectoryName(m_HSPath) + "/winhttp.dll");
				else
					logFile.MoveTo(System.IO.Path.GetDirectoryName(m_HSPath) + "/autostop-winhttp.dll");

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
			DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_HSPath) + "/Logs");
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
		public void KillHS()
		{
			List<Process> hearthstoneProcess = HearthstoneProcess();
			foreach (Process processHS in hearthstoneProcess)
			{
				processHS.Kill();
				Out.Log(string.Format("[{0}]结束炉石进程", NickName));
			}
		}
		public void StartHS()
		{
			AdjustMode();
			MainForm.WinExec(m_HSPath, 2);
			Out.Log(string.Format("[{0}]启动炉石进程", NickName));
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
				string path = ps[i].MainModule.FileName.ToString();
				if (path != m_HSPath)
					ps.RemoveAt(i);
			}
			return ps;
		}
		private void ReadConfigValue()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_HSPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
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
							m_AwakeTime = Convert.ToDateTime(line.Substring(start_pos));
						else
							m_AwakeTime = new DateTime(2000, 1, 1);
					}
					catch
					{
						m_AwakeTime = new DateTime(2000, 1, 1);
					}
				}
				else if (line.IndexOf("唤醒时间间隔 = ") == 0)
				{
					try
					{
						int start_pos = "唤醒时间间隔 = ".Length;
						if (line.Length > start_pos)
							m_AwakePeriod = Convert.ToInt32(line.Substring(start_pos)) * 60;
						else
							m_AwakePeriod = 22 * 60;
					}
					catch
					{
						m_AwakePeriod = 22 * 60;
					}
					continue;
				}
				else if (line.IndexOf("插件运行模式 = ") == 0)
				{
					try
					{
						int start_pos = "插件运行模式 = ".Length;
						if (line.Length > start_pos)
							m_Mode = line.Substring(start_pos);
						else
							m_Mode = "";
					}
					catch
					{
						m_Mode = "";
					}
				}
				else if (line.IndexOf("使用的队伍名称 = ") == 0)
				{
					try
					{
						int start_pos = "使用的队伍名称 = ".Length;
						if (line.Length > start_pos)
							m_TeamName = line.Substring(start_pos);
						else
							m_TeamName = "";
					}
					catch
					{
						m_TeamName = "";
					}
				}
				else if (line.IndexOf("战斗策略 = ") == 0)
				{
					try
					{
						int start_pos = "战斗策略 = ".Length;
						if (line.Length > start_pos)
							m_StrategyName = line.Substring(start_pos);
						else
							m_StrategyName = "";
					}
					catch
					{
						m_StrategyName = "";
					}
				}
				else
					continue;
			}
		}
		private void WriteConfigValue(TASK_MODE Mode, string TeamName, string StrategyName)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_HSPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
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

		private string m_Mode = "";
		private DateTime m_AwakeTime = new DateTime(2000, 1, 1);
		private int m_AwakePeriod = 25;
		private string m_TeamName = "";
		private string m_StrategyName = "";

		private string m_HSPath = "";//炉石路径
		private bool m_Enable = false;//启用状态
		private DateTime m_configLastEdit = new DateTime(2000,1,1);
		private TaskManager m_taskManager = new TaskManager();
	}
}
