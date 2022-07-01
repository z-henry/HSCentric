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
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
