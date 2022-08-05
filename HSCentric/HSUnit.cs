﻿using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace HSCentric
{
	public class HSUnit
	{
		public HSUnit(string _HSPath, bool _Enable = false, DateTime? _StartTime = null, DateTime? _StopTime = null)
		{
			m_HSPath = _HSPath;
			m_Enable = _Enable;
			m_NickName = Directory.GetParent(m_HSPath).Name;
			m_StartTime = _StartTime ?? new DateTime(2000, 1, 1, 0, 0, 0);
			m_StopTime = _StopTime ?? new DateTime(2000, 1, 1, 23, 59, 59);
		}

		public string Mode
		{
			get
			{
				RefreshPluginConfig();
				return m_Mode;
			}
		}
		public DateTime AwakeTime
		{
			get
			{
				RefreshPluginConfig();
				return m_AwakeTime;
			}
		}
		public int AwakePeriod
		{
			get
			{
				RefreshPluginConfig();
				return m_AwakePeriod;
			}
		}
		public bool Enable
		{
			get { return m_Enable; }
			set { m_Enable = value; }
		}
		public string NickName
		{
			get { return m_NickName; }
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
		public DateTime StartTime
		{
			get { return m_StartTime; }
		}
		public DateTime StopTime
		{
			get { return m_StopTime; }
		}
		public string Path
		{
			get { return m_HSPath; }
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
			TimeSpan time_now = DateTime.Now.TimeOfDay;
			if (StartTime < StopTime)
				return time_now >= StartTime.TimeOfDay && time_now <= StopTime.TimeOfDay;
			else
				return time_now >= StartTime.TimeOfDay || time_now <= StopTime.TimeOfDay;
		}
		public bool IsAlive()
		{
			List<Process> hearthstoneProcess = HearthstoneProcess();
			if (hearthstoneProcess.Count <= 0)
			{
				return false;
			}
			return true;
		}
		public bool IsResponding()
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
				Out.Log(string.Format("[{0}]结束炉石进程", m_NickName));
			}
		}
		public void StartHS()
		{
			MainForm.WinExec(m_HSPath, 2);
			Out.Log(string.Format("[{0}]启动炉石进程", m_NickName));
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
		private void RefreshPluginConfig()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_HSPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_configLastEdit)
				return;

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
				else
					continue;
			}
		}

		private string m_HSPath;//炉石路径
		private bool m_Enable;//启用状态
		private string m_NickName;//昵称
		private DateTime m_StartTime;
		private DateTime m_StopTime;
		private string m_Mode;
		private DateTime m_AwakeTime;
		private int m_AwakePeriod;
		private DateTime m_configLastEdit = new DateTime(2000,1,1);


	}
}
