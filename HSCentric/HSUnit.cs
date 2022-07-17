using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Windows.Forms;

namespace HSCentric
{
	class HSUnit
	{
		public HSUnit(string HSPath)
		{
			m_HSPath = HSPath;
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

		public bool IsHsRuning()
		{
			List<Process> hearthstoneProcess = HearthstoneProcess();
			if (hearthstoneProcess.Count <= 0)
			{
				Out.Log(string.Format("[轮询检测] 无炉石进程"));
				return false;
			}

			//检查炉石日志, 5分钟没更新就杀死
			bool killHS = true;
			DirectoryInfo rootHS = new DirectoryInfo(Path.GetDirectoryName(m_HSPath) + "/Logs");
			foreach (FileInfo logFile in rootHS.GetFiles("hearthstone_*.log", SearchOption.TopDirectoryOnly)) //查找文件
			{
				double inteval = 5f;
				TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);
				if (timeSpan.TotalMinutes < inteval)
				{
					killHS = false;
					break;
				}
			}

			if (killHS == true)
			{
				foreach (Process processHS in hearthstoneProcess)
				{
					Out.Log(string.Format("[轮询检测] 结束炉石进程"));
					processHS.Kill();
					processHS.WaitForExit();
					Delay(5000);
				}
				return false;
			}
			Out.Log(string.Format("[轮询检测] 炉石进程正常{0}", m_HSPath));
			return true;
		}


		public bool IsExperienceMode()
		{
			string[] fileLines = File.ReadAllLines(Path.GetDirectoryName(m_HSPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			foreach (string line in fileLines)
			{
				if (line.IndexOf("插件运行模式") != 0)
					continue;


				if (line.Contains("挂机收菜"))
				{
					Out.Log(string.Format("[轮询检测] 挂机收菜模式"));
					return true;
				}
				else
				{
					Out.Log(string.Format("[轮询检测] 非挂机收菜模式"));
					return false;
				}
			}
			Out.Log(string.Format("[轮询检测] 无模式设置"));
			return false;
		}


		public bool AwakeExperienceMode()
		{
			string[] fileLines = File.ReadAllLines(Path.GetDirectoryName(m_HSPath) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			foreach (string line in fileLines)
			{
				if (line.IndexOf("唤醒时间") != 0)
					continue;

				if (DateTime.Now > Convert.ToDateTime(line.Remove(0, "唤醒时间 = ".Length)))
				{
					Out.Log(string.Format("[轮询检测] 到达唤醒时间"));
					return true;
				}
				else
				{
					Out.Log(string.Format("[轮询检测] 未到达唤醒时间"));
					return false;
				}
			}
			Out.Log(string.Format("[轮询检测] 未设置唤醒时间"));
			return true;
		}

		public void StartHS()
		{
			MainForm.WinExec(m_HSPath, 2);
			Out.Log(string.Format("[轮询检测] 启动炉石{0}", m_HSPath));
		}

		private void Delay(int mm)
		{
			DateTime timeInput = DateTime.Now;
			while (timeInput.AddMilliseconds((double)mm) > DateTime.Now)
			{
				Application.DoEvents();
			}
		}



		private string m_HSPath;
	}
}
