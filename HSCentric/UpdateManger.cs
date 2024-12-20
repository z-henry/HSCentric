using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace HSCentric
{
	public class UpdateManger
	{
		public static UpdateManger Get()
		{
			return s_instance;
		}

		public void Init(Action callbackUpdateComplete, string path)
		{
			m_callbackUpdateComplete = callbackUpdateComplete;
			m_path = path;
		}

		public void Start()
		{
			if (m_timer == null)
			{
				m_timer = new Timer(Check, "UpdateManger.Check", 10 * 1000, 30 * 1000);
			}
			else
			{
				m_timer.Change(10 * 1000, 30 * 1000);
			}
		}

		public void Stop()
		{
			m_timer.Change(-1, 0);
			KillBattleNet();
		}

		private void Check(object state)
		{
			if (BattleNetProcess.Length <= 0)
			{
				m_bnetStartTime = new DateTime(0);
				StartBattleNet();
				return;
			}

			if (true == ReadUpdateStatusFromBNetLog())
			{
				m_callbackUpdateComplete();
				Stop();
				return;
			}
		}

		private Process[] BattleNetProcess
		{
			get { return Process.GetProcessesByName("Battle.net"); }
		}

		private Process[] BattleNetUpdateAgentProcess
		{
			get { return Process.GetProcessesByName("Agent"); }
		}

		private void StartBattleNet()
		{
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = m_path;
			process.Start();
			process.WaitForInputIdle();
			Out.Info("[升级]启动战网");
			m_bnetStartTime = DateTime.Now;
		}

		private void KillBattleNet()
		{
			foreach (Process process in this.BattleNetProcess)
			{
				process.Kill();
				Out.Info("[升级]停止战网");
			}
			foreach (Process process in this.BattleNetUpdateAgentProcess)
			{
				process.Kill();
				Out.Info("[升级]停止战网Agent");
			}
		}

		private bool ReadUpdateStatusFromBNetLog()
		{
			try
			{
				if (string.IsNullOrEmpty(m_path))
				{
					Out.Error("[升级]无战网日志");
					return true;
				}
				DirectoryInfo rootHS = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Battle.net/Logs");
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
				{
					Out.Error("[升级]无战网日志");
					return true;
				}
				List<FileInfo> testList = rootHS.GetFiles("battle.net-*.log", SearchOption.TopDirectoryOnly).ToList();
				List<FileInfo> targetFile = testList.FindAll(x => string.Compare(x.Name, $"battle.net-{m_bnetStartTime.ToUniversalTime():yyyyMMddTHHmmss.ffffff}.log") > 0);
				if (targetFile.Count != 1)
				{
					Out.Error("[升级]无战网日志");
					return true;
				}
				using (FileStream fs = new FileStream(targetFile[0].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (StreamReader sr = new StreamReader(fs))
					{
						StringBuilder sb = new StringBuilder();
						while (!sr.EndOfStream)
						{
							if (sr.ReadLine().IndexOf("Update completed: hs_beta") >= 0)
							{
								Out.Info("[升级]升级完毕");
								return true;
							}
						}
					}
				}
			}
			catch
			{
				Out.Error("[升级]战网日志读取失败");
				return false;
			}
			Out.Debug("[升级]升级中");
			return false;
		}

		private static UpdateManger s_instance = new UpdateManger();
		private Action m_callbackUpdateComplete = null;
		private Timer m_timer = null;
		private string m_path = "";
		private DateTime m_bnetStartTime = new DateTime(0);
	}
}