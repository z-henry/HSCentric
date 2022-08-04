using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HSCentric.Const;

namespace HSCentric
{
	public sealed partial class MainForm : Form
	{
		

		[DllImport("kernel32.dll")]
		public static extern int WinExec(string exeName, int operType);

		public MainForm()
		{
			this.InitializeComponent();
			this.timer1.Interval = 1000;
			this.timer1.Tick += this.TickProcess;
			this.timer1.Start();

			this.listHS.Columns.Add("启用", 40);
			this.listHS.Columns.Add("成员", 120);
			this.listHS.Columns.Add("模式", 80);
			this.listHS.Columns.Add("唤醒时间", 120);
			this.listHS.Columns.Add("版本", 120);
			this.listHS.Columns.Add("启用时间段", 120);

			LoadConfig();

			UI_Flush();
		}

		private void TickProcess(object sender, EventArgs e)
		{
			//30秒检测重启
			if (++m_TickCount > 30)
			{
				m_TickCount = 0;

				lock (m_lockHS)
				{
					for( int i = 0, ii = m_listHS.Count;i < ii; ++i)
					{
						HSUnit hsUnit = m_listHS[i];

						// 没启用就跳过
						if (!hsUnit.Enable)
							continue;

						backup(i);

						// 不在启用时间段,启动了就干掉
						if (!hsUnit.IsActive())
						{
							hsUnit.SwitchBepinEx(false);
							if (hsUnit.IsAlive())
							{
								hsUnit.KillHS();
								Out.Log(string.Format("[{0}]未到启用时间，关闭进程", hsUnit.NickName));
							}
							continue;
						}
						hsUnit.SwitchBepinEx(true);

						// 炉石在运行
						if (hsUnit.IsAlive())
						{
							// 炉石在运行，不更新日志就滚蛋
							if (!hsUnit.IsResponding())
							{
								hsUnit.KillHS();
								Out.Log(string.Format("[{0}]无响应，关闭进程", hsUnit.NickName));
								continue;
							}
						}
						//炉石没运行就判断是否需要启动
						else
						{
							if ("挂机收菜" == hsUnit.Mode)
							{
								// 挂机收菜模式下，
								// 1. 到唤醒时间唤醒
								if (DateTime.Now >= hsUnit.AwakeTime)
									Out.Log(string.Format("[{0}]唤醒", hsUnit.NickName));
								// 2. 没到唤醒时间，但是距离结束不到5分钟了，唤醒
								else if ((hsUnit.StopTime.TimeOfDay - DateTime.Now.TimeOfDay).TotalSeconds < 300f)
									Out.Log(string.Format("[{0}]快结束了，唤醒", hsUnit.NickName));
								else
									continue;
							}
							hsUnit.StartHS();
							Out.Log(string.Format("[{0}]启动", hsUnit.NickName));
							continue;
						}
					}
				}
				UI_Flush();
				return;
			}
		}
		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Stop();
			lock (m_lockHS)
			{
				foreach (HSUnit hsUnit in m_listHS)
					hsUnit.SwitchBepinEx(false);
			}
			SaveConfig();

		}
		private void btn_add_Click(object sender, EventArgs e)
		{
			HSUnitForm dlg = new HSUnitForm();
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			lock (m_lockHS)
			{
				m_listHS.Add(new HSUnit(dlg.Path, dlg.Enable, dlg.StartTime, dlg.StopTime));
			}
			UI_Flush();
		}
		private void btn_del_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				lock (m_lockHS)
				{
					m_listHS.RemoveAt(listHS.SelectedItems[0].Index);
				}
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}
		private void UI_Flush()
		{
			lock (m_lockHS)
			{
				listHS.Items.Clear();

				FileVerison max_version = new FileVerison(0, 0, 0, 0);
				foreach (HSUnit unit in m_listHS)
					max_version = max_version > unit.Version ? max_version : unit.Version;

				foreach (HSUnit unit in m_listHS)
				{
					Color default_color;
					if (!unit.Enable)
						default_color = Color.Pink;
					else if (unit.IsActive())
						default_color = Color.YellowGreen;
					else
						default_color = Color.White;

					ListViewItem item = new ListViewItem();
					item.UseItemStyleForSubItems = false;
					item.BackColor = default_color;
					item.Text = unit.Enable ? "√" : "";

					foreach (LIST_COLUMN list_item in Enum.GetValues(typeof(LIST_COLUMN)))
					{
						if (list_item == LIST_COLUMN.启用)
							continue;

						ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
						subitem.BackColor = default_color;
						switch (list_item)
						{
							case LIST_COLUMN.唤醒时间:
								subitem.Text = unit.AwakeTime.ToString("G");
								break;
							case LIST_COLUMN.成员:
								subitem.Text = unit.NickName;
								break;
							case LIST_COLUMN.模式:
								subitem.Text = unit.Mode;
								break;
							case LIST_COLUMN.版本:
								subitem.Text = unit.Version.ToString();
								if (unit.Version < max_version)
									subitem.BackColor = Color.Yellow;
								break;
							case LIST_COLUMN.启用时间段:
								subitem.Text = unit.StartTime.ToString("t") + " - " + unit.StopTime.ToString("t");
								break;
							default:
								break;
						}
						item.SubItems.Add(subitem);
					}
					listHS.Items.Add(item);
				}
				SaveConfig();
			}

		}

		private void 启用ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				lock (m_lockHS)
				{
					int index = listHS.SelectedItems[0].Index;
					m_listHS[index].Enable = !m_listHS[index].Enable;
				}
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void listHS_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (listHS.SelectedItems.Count > 0)
				{
					int index = listHS.SelectedItems[0].Index;
					contextMenuStrip_RMenu.Items[(int)RMenu.启用].Text = listHS.Items[index].SubItems[(int)LIST_COLUMN.启用].Text == "√" ? "停用" : "启用";
					if (listHS.Items[index].SubItems[(int)LIST_COLUMN.成员].Text == "Hearthstone")
						contextMenuStrip_RMenu.Items[(int)RMenu.更新].Enabled = false;
					else
						contextMenuStrip_RMenu.Items[(int)RMenu.更新].Enabled = true;

					contextMenuStrip_RMenu.Show(listHS, new Point(e.X, e.Y));
				}
			}
		}

		private void 更新ToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;
				if (listHS.Items[index].SubItems[(int)LIST_COLUMN.成员].Text == "Hearthstone")
				{
					MessageBox.Show("成员：Hearthstone无法更新", "ERROR");
					return;
				}

				m_listHS[index].Enable = false;
				if (m_listHS[index].IsAlive())
				{
					m_listHS[index].KillHS();
					Delay(5000);
				}

				//运行备份更新bat
				Process proc;
				try
				{
					proc = new Process();
					proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"script\backup\upgrade.bat";
					proc.StartInfo.Arguments = "\"" + System.IO.Path.GetDirectoryName(m_listHS[index].Path) + "\"";
					proc.StartInfo.CreateNoWindow = false;
					proc.Start();
					proc.WaitForExit();
					MessageBox.Show(string.Format("成员：{0}，更新完成", listHS.Items[index].SubItems[(int)LIST_COLUMN.成员].Text));
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString()), "ERROR");
				}
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void Delay(int mm)
		{
			DateTime timeInput = DateTime.Now;
			while (timeInput.AddMilliseconds((double)mm) > DateTime.Now)
			{
				Application.DoEvents();
			}
		}

		private void listHS_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;
				HSUnitForm dlg = new HSUnitForm(m_listHS[index].Path, m_listHS[index].Enable, m_listHS[index].StartTime, m_listHS[index].StopTime);
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				lock (m_lockHS)
				{
					m_listHS[index] = new HSUnit(dlg.Path, dlg.Enable, dlg.StartTime, dlg.StopTime);
				}
				UI_Flush();
			}
		}
		private void LoadConfig()
		{
			HSUnitSection config = ConfigurationManager.GetSection("userinfo") as HSUnitSection;
			foreach (HSUnitElement elem in config.HSUnit.Cast<HSUnitElement>().ToList())
			{
				m_listHS.Add(new HSUnit(elem.Path, elem.Enable, Convert.ToDateTime(elem.StartTime), Convert.ToDateTime(elem.StopTime)));
			}
// 			m_listHS = ConfigurationManager.GetSection("userinfo") as List<HSUnit>;
		}
		private void SaveConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			HSUnitSection section = config.GetSection("userinfo") as HSUnitSection;
			section.HSUnit.Clear();
			foreach (HSUnit unit in m_listHS)
			{
				section.HSUnit.Add(new HSUnitElement() { 
					Path = unit.Path, 
					StartTime = unit.StartTime.ToString("G"),
					StopTime = unit.StopTime.ToString("G"),
					Enable = unit.Enable,
				});
			}
			config.Save();
			ConfigurationManager.RefreshSection("userinfo");
		}

		private void 启动ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;
				lock (m_lockHS)
				{
					if (!m_listHS[index].IsAlive())
						m_listHS[index].StartHS();
				}
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void 备份插件设置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;

				try
				{
					backup(index);
					MessageBox.Show(string.Format("成员：{0}，备份完成", listHS.Items[index].SubItems[(int)LIST_COLUMN.成员].Text));
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString()), "ERROR");
				}

				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void backup(int index)
		{
			//运行备份更新bat
			Process proc;
			proc = new Process();
			proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"script\backup\backup.bat";
			proc.StartInfo.Arguments = "\"" + System.IO.Path.GetDirectoryName(m_listHS[index].Path) + "\"";
			proc.StartInfo.CreateNoWindow = false;
			proc.Start();
			proc.WaitForExit();
		}
	}
}
