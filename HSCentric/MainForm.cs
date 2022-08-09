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
			this.timer1.Interval = 100;
			this.timer1.Tick += this.TickProcess;
			this.timer1.Start();

			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用.ToString(), 40);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.成员.ToString(), 120);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.当前模式.ToString(), 80);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.预设模式.ToString(), 80);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.唤醒时间.ToString(), 120);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用时间段.ToString(), 140);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.版本.ToString(), 90);

			LoadConfig();

			UI_Flush();
		}

		private void TickProcess(object sender, EventArgs e)
		{
			TimeSpan timespan_checkpriod = new TimeSpan(0, 0, 10);//检测间隔
			TimeSpan timespan_farmendding = new TimeSpan(0, 2, 0);//收菜模式结束一定时间内启动收尾
			//30秒检测重启
			if (DateTime.Now > m_CheckTime)
			{
				m_CheckTime = DateTime.Now.AddSeconds(timespan_checkpriod.TotalSeconds);

				lock (m_lockHS)
				{
					for( int i = 0, ii = m_listHS.Count;i < ii; ++i)
					{
						HSUnit hsUnit = m_listHS[i];

						// 没启用就跳过
						if (!hsUnit.Enable)
							continue;

						// 备份配置
						backup(i);

						// 不在启用时间段,启动了就干掉
						if (!hsUnit.IsActive())
						{
							if (hsUnit.IsProcessAlive())
							{
								Out.Log(string.Format("[{0}]未到启用时间", hsUnit.NickName));
								hsUnit.KillHS();
							}
							hsUnit.SwitchBepinEx(false);
							continue;
						}
						hsUnit.SwitchBepinEx(true);


						// 在运行就判断是否需要杀掉
						if (hsUnit.IsProcessAlive())
						{
							string msg_kill_reason = "";
							// 需要切换模式
							if (hsUnit.NeedAdjustMode())
								msg_kill_reason = "切换模式";
							// 不更新日志就滚蛋
							else if (!hsUnit.IsLogUpdated())
								msg_kill_reason = "炉石进程日志不更新";

							if (msg_kill_reason.Length > 0)
							{
								Out.Log(string.Format("[{0}] 结束进程 [reason{1}]", hsUnit.NickName, msg_kill_reason));
								hsUnit.KillHS();
							}
						}
						//炉石没运行就判断是否需要启动
						else
						{
							string msg_start_reason = "正常拽起";
							var basicConfigValue = hsUnit.BasicConfigValue;
							var currentTask = hsUnit.CurrentTask;
							if (TASK_MODE.挂机收菜 == currentTask.Mode)
							{
								// 挂机收菜模式下，
								// 1. 到唤醒时间唤醒
								if (DateTime.Now >= basicConfigValue.AwakeTime)
									msg_start_reason = string.Format("收菜唤醒时间到了", timespan_farmendding.TotalSeconds);
								// 2. 没到唤醒时间，但是距离结束不到X分钟了，唤醒
								else if ((currentTask.StopTime.TimeOfDay - DateTime.Now.TimeOfDay).TotalSeconds < timespan_farmendding.TotalSeconds)
									msg_start_reason = string.Format("收菜模式[{0}]秒内结束", timespan_farmendding.TotalSeconds);
								// 3. 其他情况就不拽起了
								else
									msg_start_reason = "";
							}

							if (msg_start_reason.Length > 0)
							{
								Out.Log(string.Format("[{0}] 启动进程 [reason{1}]", hsUnit.NickName, msg_start_reason));
								hsUnit.StartHS();
							}
						}
					}
				}
				UI_Flush();
			}

			label_currenttime.Text = DateTime.Now.ToString("G");
			label_checktime.Text = m_CheckTime.ToString("G");
			label_checktime.BackColor = GetColor(timespan_checkpriod.TotalMilliseconds, new TimeSpan(m_CheckTime.Ticks - DateTime.Now.Ticks).TotalMilliseconds,
				new List<Color>() { Color.YellowGreen, Color.Yellow, Color.Red });
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
				m_listHS.Add(dlg.Unit);
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
					var basicConfigValue = unit.BasicConfigValue;
					var currentTask = unit.CurrentTask;
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

					foreach (LIST_UNIT_COLUMN list_item in Enum.GetValues(typeof(LIST_UNIT_COLUMN)))
					{
						if (list_item == LIST_UNIT_COLUMN.启用)
							continue;

						ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
						subitem.BackColor = default_color;
						switch (list_item)
						{
							case LIST_UNIT_COLUMN.唤醒时间:
								subitem.Text = basicConfigValue.AwakeTime.ToString("G");
								if (unit.Enable && basicConfigValue.Mode == TASK_MODE.挂机收菜.ToString())
									subitem.BackColor = GetColor(basicConfigValue.AwakePeriod, new TimeSpan(basicConfigValue.AwakeTime.Ticks - DateTime.Now.Ticks).TotalSeconds,
										new List<Color>() { Color.White, default_color });
								break;
							case LIST_UNIT_COLUMN.成员:
								subitem.Text = unit.NickName;
								break;
							case LIST_UNIT_COLUMN.当前模式:
								subitem.Text = basicConfigValue.Mode;
								break;
							case LIST_UNIT_COLUMN.预设模式:
								subitem.Text = currentTask.Mode.ToString();
								break;
							case LIST_UNIT_COLUMN.版本:
								subitem.Text = unit.Version.ToString();
								if (unit.Version < max_version)
									subitem.BackColor = Color.Yellow;
								break;
							case LIST_UNIT_COLUMN.启用时间段:
								subitem.Text = currentTask.StartTime.ToString("T") + " - " + currentTask.StopTime.ToString("T");
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
				int index = listHS.SelectedItems[0].Index;
				lock (m_lockHS)
				{
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
					contextMenuStrip_RMenu.Items[(int)R_UNIT_MENU.启用].Text = listHS.Items[index].SubItems[(int)LIST_UNIT_COLUMN.启用].Text == "√" ? "停用" : "启用";
					if (listHS.Items[index].SubItems[(int)LIST_UNIT_COLUMN.成员].Text == "Hearthstone")
						contextMenuStrip_RMenu.Items[(int)R_UNIT_MENU.更新].Enabled = false;
					else
						contextMenuStrip_RMenu.Items[(int)R_UNIT_MENU.更新].Enabled = true;

					contextMenuStrip_RMenu.Show(listHS, new Point(e.X, e.Y));
				}
			}
		}

		private void 更新ToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;
				if (listHS.Items[index].SubItems[(int)LIST_UNIT_COLUMN.成员].Text == "Hearthstone")
				{
					MessageBox.Show("成员：Hearthstone无法更新", "ERROR");
					return;
				}
				lock (m_lockHS)
				{
					m_listHS[index].Enable = false;
					if (m_listHS[index].IsProcessAlive())
					{
						m_listHS[index].KillHS();
						Delay(1000);
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
						MessageBox.Show(string.Format("成员：{0}，更新完成", listHS.Items[index].SubItems[(int)LIST_UNIT_COLUMN.成员].Text));
					}
					catch (Exception ex)
					{
						MessageBox.Show(string.Format("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString()), "ERROR");
					}
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
				HSUnitForm dlg = new HSUnitForm(m_listHS[index]);
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				lock (m_lockHS)
				{
					m_listHS[index] = dlg.Unit;
				}
				UI_Flush();
			}
		}
		private void LoadConfig()
		{
			HSUnitSection config = ConfigurationManager.GetSection("userinfo") as HSUnitSection;
			foreach (HSUnitElement hs in config.HSUnit.Cast<HSUnitElement>().ToList())
			{
				List<TaskUnit> tasks = new List<TaskUnit>();
				foreach (TaskElement task in hs.Tasks.Cast<TaskElement>().ToList())
				{
					tasks.Add(new TaskUnit()
					{
						Mode = (TASK_MODE)Enum.Parse(typeof(TASK_MODE), task.Mode),
						StartTime = Convert.ToDateTime(task.StartTime),
						StopTime = Convert.ToDateTime(task.StopTime),
						StrategyName = task.StrategyName,
						TeamName = task.TeamName,
					});
				}
				m_listHS.Add(new HSUnit(hs.Path, hs.Enable, tasks));
			}
// 			m_listHS = ConfigurationManager.GetSection("userinfo") as List<HSUnit>;
		}
		private void SaveConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			HSUnitSection section = config.GetSection("userinfo") as HSUnitSection;
			section.HSUnit.Clear();
			int index_hs = 0;
			foreach (HSUnit hs in m_listHS)
			{
				int index_task = 0;
				TaskCollection tasks = new TaskCollection();
				foreach (TaskUnit task in hs.Tasks.GetTasks())
				{
					tasks.Add(new TaskElement()
					{
						ID = ++index_task,
						Mode = task.Mode.ToString(),
						TeamName = task.TeamName,
						StrategyName = task.StrategyName,
						StartTime = task.StartTime.ToString("G"),
						StopTime = task.StopTime.ToString("G"),
					});
				}
				section.HSUnit.Add(new HSUnitElement()
				{
					ID = ++index_hs,
					Path = hs.Path,
					Enable = hs.Enable,
					Tasks = tasks,
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
					if (!m_listHS[index].IsProcessAlive())
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
					MessageBox.Show(string.Format("成员：{0}，备份完成", listHS.Items[index].SubItems[(int)LIST_UNIT_COLUMN.成员].Text));
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
			proc.StartInfo.CreateNoWindow = true;   //不创建该进程的窗口
			proc.StartInfo.UseShellExecute = false;   //不使用shell壳运行
			proc.Start();
			proc.WaitForExit();
		}

		private Color GetColor(double period, double val, List<Color> colors)
		{
			if (colors.Count <= 0)
				return Color.White;
			if (colors.Count == 1)
				return colors[0];

			val = period - val;
			int step_count = colors.Count - 1;
			double step_interval = period / step_count;

			val = Math.Min(Math.Max(0, val), period - 1);
			int current_step_index = (int)(val / step_interval);
			int current_step_progress = (int)(val - step_interval * current_step_index);
			if (current_step_index >= step_count)
				return colors[0];

			Color color_from = colors[current_step_index];
			Color color_to = colors[current_step_index+1];

			int r = color_from.R + (int)((color_to.R - color_from.R) / step_interval * current_step_progress);
			int g = color_from.G + (int)((color_to.G - color_from.G) / step_interval * current_step_progress);
			int b = color_from.B + (int)((color_to.B - color_from.B) / step_interval * current_step_progress);

			r = Math.Min(Math.Max(0, r), 255);
			g = Math.Min(Math.Max(0, g), 255);
			b = Math.Min(Math.Max(0, b), 255);
			return Color.FromArgb(r, g, b);
		}
	}
}
