using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HSCentric
{
	public sealed partial class MainForm : Form
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		private Process[] HearthstoneProcess
		{
			get
			{
				return Process.GetProcessesByName("Hearthstone");
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000002 RID: 2 RVA: 0x0000205C File Offset: 0x0000025C
		private Process[] BattleNetProcess
		{
			get
			{
				return Process.GetProcessesByName("Battle.net");
			}
		}

		// Token: 0x06000003 RID: 3
		[DllImport("kernel32.dll")]
		private static extern int WinExec(string exeName, int operType);

		// Token: 0x06000004 RID: 4 RVA: 0x00002068 File Offset: 0x00000268
		public MainForm()
		{
			this.InitializeComponent();
			this.timer1.Interval = 30000;
			this.timer1.Tick += this.process;
			this.timer1.Start();
			this.AutoGetBattleNetPath();
			this.AutoGetHSPath();
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020C4 File Offset: 0x000002C4
		private void AutoGetHSPath()
		{
			string text = this.FindInstallPathFromRegistry("Hearthstone");
			if (!string.IsNullOrEmpty(text) && Directory.Exists(text) && File.Exists(Path.Combine(text, "Hearthstone.exe")))
			{
				string text2 = Path.Combine(text, "Hearthstone.exe");
				this.pathInput2.Text = text2;
				Out.Log(string.Format("自动获取炉石路径 {0}", text2));
			}
		}

		private void AutoGetBattleNetPath()
		{
			string text = this.FindInstallPathFromRegistry("Battle.net");
			if (!string.IsNullOrEmpty(text) && Directory.Exists(text) && File.Exists(Path.Combine(text, "Battle.net Launcher.exe")))
			{
				string text2 = Path.Combine(text, "Battle.net Launcher.exe");
				this.pathInput.Text = text2;
				Out.Log(string.Format("自动获取战网路径 {0}", text2));
			}
		}


		// Token: 0x06000006 RID: 6 RVA: 0x00002118 File Offset: 0x00000318
		public string FindInstallPathFromRegistry(string uninstallKeyName)
		{
			try
			{
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + uninstallKeyName);
				if (registryKey == null)
				{
					return null;
				}
				object value = registryKey.GetValue("InstallLocation");
				registryKey.Close();
				if (value != null && !string.IsNullOrEmpty(value.ToString()))
				{
					return value.ToString();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			return null;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002190 File Offset: 0x00000390
		private void process(object sender, EventArgs e)
		{
			if (!isRuning)
			{
				return;
			}
			if (!IsHsRuning())
			{
				StartHS();
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002200 File Offset: 0x00000400
		private bool IsHsRuning()
		{
			Process[] hearthstoneProcess = HearthstoneProcess;
			if (hearthstoneProcess.Length <= 0)
			{
				Out.Log(string.Format("[轮询检测] 无炉石进程"));
				return false;
			}

			//检查炉石日志, 5分钟没更新就杀死
			bool killHS = true;
			DirectoryInfo rootHS = new DirectoryInfo(Path.GetDirectoryName(pathInput2.Text) + "/Logs");
			foreach (FileInfo logFile in rootHS.GetFiles("hearthstone_*.log", SearchOption.TopDirectoryOnly)) //查找文件
			{
				double inteval = 5f;
				TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);
				if (timeSpan.TotalMinutes < inteval)
				{
					Out.Log(string.Format("[轮询检测] 有日志在{0}min内更新过 {1}", inteval, logFile.Name));
					killHS = false;
					break;
				}
			}

			if(killHS == true)
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
			Out.Log(string.Format("[轮询检测] 炉石进程正常"));
			return true;


		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002228 File Offset: 0x00000428
		private bool IsBattleRuning()
		{
			Process[] battleNetProcess = BattleNetProcess;
			return battleNetProcess.Length > 0;
		}

		private bool IsExperienceMode()
		{
			string[] fileLines = File.ReadAllLines(Path.GetDirectoryName(pathInput2.Text) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
			foreach(string line in fileLines)
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


		private bool AwakeExperienceMode()
		{
			string[] fileLines = File.ReadAllLines(Path.GetDirectoryName(pathInput2.Text) + "/BepInEx/config/io.github.jimowushuang.hs.cfg");
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

			// Token: 0x0600000B RID: 11 RVA: 0x00002250 File Offset: 0x00000450
		private void StartHS()
		{
			if (IsExperienceMode())
				if (!AwakeExperienceMode())
					return;

			if (!IsBattleRuning())
			{
				StartBattle();
			}
			Process[] battleNetProcess = this.BattleNetProcess;
			int num = 0;
			if (num < battleNetProcess.Length)
			{
				Process.Start(battleNetProcess[num].MainModule.FileName, "--exec=\"launch WTCG\"");
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000229B File Offset: 0x0000049B
		private void StartBattle()
		{
			while (BattleNetProcess.Length < 1 && isRuning)
			{
				MainForm.WinExec(pathInput.Text, 2);
				Delay(5000);
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000022D0 File Offset: 0x000004D0
		private void Delay(int mm)
		{
			DateTime timeInput = DateTime.Now;
			while (timeInput.AddMilliseconds((double)mm) > DateTime.Now)
			{
				Application.DoEvents();
			}
		}




		// Token: 0x06000010 RID: 16 RVA: 0x0000234F File Offset: 0x0000054F
		private void Main_Load(object sender, EventArgs e)
		{
		}

		// Token: 0x04000001 RID: 1

		private void button2_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Hearthstone.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
			{
				return;
			}
			pathInput2.Text = openFileDialog.FileName;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Battle.net Launcher.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
			{
				return;
			}
			pathInput.Text = openFileDialog.FileName;
		}

		private void run_Click(object sender, EventArgs e)
		{
			isRuning = !this.isRuning;
			run.Text = ((!this.isRuning) ? "run" : "stop");
		}

		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Stop();
		}
	}
}
