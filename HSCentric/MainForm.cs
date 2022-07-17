using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HSCentric
{
	public sealed partial class MainForm : Form
	{
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250


		[DllImport("kernel32.dll")]
		public static extern int WinExec(string exeName, int operType);

		public MainForm()
		{
			this.InitializeComponent();
			this.timer1.Interval = 30000;
			this.timer1.Tick += this.process;
			this.timer1.Start();
		}

		private void process(object sender, EventArgs e)
		{
			lock (m_lockHS)
			{
				for (int i = 0; i < m_listHS.Count; ++i)
				{
					HSUnit hsUnit = m_listHS[i];
					if (false == checkedListBox1.GetItemChecked(i))
						continue;
					if (!hsUnit.IsHsRuning())
					{
						if (hsUnit.IsExperienceMode())
							if (!hsUnit.AwakeExperienceMode())
								return;
						hsUnit.StartHS();
					}
				}
			}
		}
		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Stop();
		}

		private void btn_add_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Hearthstone.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
			{
				return;
			}
			lock (m_lockHS)
			{
				AddRecord(openFileDialog.FileName, false);
			}
		}

		private void btn_del_Click(object sender, EventArgs e)
		{
			lock (m_lockHS)
			{
				int index = checkedListBox1.SelectedIndex;
				if (index != -1)
				{
					DelRecord(index);
				}
			}
		}

		private void AddRecord(string path, bool enable)
		{
			checkedListBox1.Items.Add(path, enable);
			m_listHS.Add(new HSUnit(path));
		}

		private void DelRecord(int index)
		{
			checkedListBox1.Items.RemoveAt(index);
			m_listHS.RemoveAt(index);
		}
	}
}
