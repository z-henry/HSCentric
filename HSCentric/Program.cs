using System;
using System.Windows.Forms;

namespace HSCentric
{
	// Token: 0x02000003 RID: 3
	internal static class Program
	{
		// Token: 0x06000013 RID: 19 RVA: 0x0000260B File Offset: 0x0000080B
		[STAThread]
		private static void Main()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((obj, args) => MiniDump.Write());
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());
			}

			catch (Exception ex)
			{
				Out.Log("空间名：" + ex.Source + "；" + '\n' +
					"方法名：" + ex.TargetSite + '\n' +
					"故障点：" + ex.StackTrace.Substring(ex.StackTrace.LastIndexOf("\\") + 1, ex.StackTrace.Length - ex.StackTrace.LastIndexOf("\\") - 1) + '\n' +
					"错误提示：" + ex.Message);
			}
		}
	}
}
