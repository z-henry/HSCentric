﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HSCentric.Const;
using System.Net.Sockets;
using System.Windows.Documents;
using log4net.Config;
// using MimeKit;

namespace HSCentric
{
	public sealed partial class MainForm : Form
	{
		public MainForm()
		{

			FixWorkingDirectory(); // 修正工作目录

			this.InitializeComponent();

			this.timer1.Interval = 100;
			this.timer1.Tick += this.TickProcess;

			//listHS
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用.ToString(), 40);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.成员.ToString(), 80);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.等级.ToString(), 40);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.经验.ToString(), 50);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.经验效率.ToString(), 100);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.当前模式.ToString(), 70);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.预设模式.ToString(), 70);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用时间段.ToString(), 95);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.PVP分数.ToString(), 60);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.传统模式等级.ToString(), 140);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.唤醒时间.ToString(), 130);
			this.textbox_BNetPath.Text = ConfigurationManager.AppSettings["bnet_path"];

			//listlog
 			listLog.Columns.Add("Logs", -2); // 添加一列并设置宽度为控件宽度
 			listLog.HeaderStyle = ColumnHeaderStyle.None; // 隐藏列头
 			listLog.FullRowSelect = true; // 使整个行可选


			// 订阅 Out 类的事件
			Out.Logged += OnLogged;

			//初始化
			UpdateManger.Get().Init(new Action(HSUnitManager.Get().RecoverAfterUpdated), textbox_BNetPath.Text);
			HSUnitManager.Get().Init(new Action(UpdateManger.Get().Start));
			UI_Flush();
			MyRestFul.Init(ConfigurationManager.AppSettings["rest_url"]);
			this.timer1.Start();

			this.Resize += Form1_Resize;
		}
		private static void FixWorkingDirectory()
		{
			// 获取程序的实际运行目录
			string actualPath = System.IO.Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location);

			// 设置为当前工作目录
			Environment.CurrentDirectory = actualPath;
		}

		// 窗体的 Resize 事件处理
		private void Form1_Resize(object sender, EventArgs e)
		{
// 			if (this.WindowState == FormWindowState.Minimized)
// 			{
// 				MinimizeToTray();
// 			}
		}

		// 最小化到托盘的通用方法
		private void MinimizeToTray()
		{
			this.Hide();
			notifyIcon1.ShowBalloonTip(1000, "HSCentirc", "应用程序已最小化到托盘。", ToolTipIcon.Info);
		}

		// 显示窗口并还原
		private void ShowWindow()
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
		}

		private void TickProcess(object sender, EventArgs e)
		{
			//检测重启
			TimeSpan timespan_checkpriod = new TimeSpan(0, 0, 10);//检测间隔
			if (DateTime.Now > m_CheckTime_runinfo)
			{
				m_CheckTime_runinfo = DateTime.Now.AddSeconds(timespan_checkpriod.TotalSeconds);
				HSUnitManager.Get().Check();
				UI_Flush();
			}

			//检测日志
			TimeSpan timespan_readlogpriod = new TimeSpan(0, 1, 0);
			if (DateTime.Now > m_CheckTime_readlog)
			{
				m_CheckTime_readlog = DateTime.Now.AddSeconds(timespan_readlogpriod.TotalSeconds);
				HSUnitManager.Get().CheckLog();
			}

			label_currenttime.Text = DateTime.Now.ToString("G");
			label_checktime.Text = m_CheckTime_runinfo.ToString("G");
			label_checktime.BackColor = GetColor(timespan_checkpriod.TotalMilliseconds, new TimeSpan(m_CheckTime_runinfo.Ticks - DateTime.Now.Ticks).TotalMilliseconds,
				new List<Color>() { Color.YellowGreen, Color.Yellow, Color.Red });
		}
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (flag_exit == false)
			{
				e.Cancel = true;
				MinimizeToTray();
			}
		}
		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Stop();
			HSUnitManager.Get().Release();
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			if (config.AppSettings.Settings["bnet_path"] != null)
				config.AppSettings.Settings["bnet_path"].Value = textbox_BNetPath.Text;
			else
				config.AppSettings.Settings.Add("bnet_path", textbox_BNetPath.Text);
			config.Save();
		}
		private void btn_add_Click(object sender, EventArgs e)
		{
			HSUnitForm dlg = new HSUnitForm();
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			HSUnitManager.Get().Add(dlg.Unit);
			UI_Flush();
		}
		private void btn_del_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				HSUnitManager.Get().Remove(listHS.SelectedItems[0].Index);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}
		private void UI_Flush()
		{
			List<HSUnit> listsHS = HSUnitManager.Get().GetHSUnits();
			listHS.Items.Clear();

			string tooltips_str = $"共{listsHS.Count}个任务";
			foreach (HSUnit unit in listsHS)
			{
				tooltips_str += $"\r\n{unit.ID}：";
				var basicConfigValue = unit.BasicConfigValue;
				var currentTask = unit.CurrentTask;
				Color default_color;
				if (!unit.Enable)
				{
					default_color = Color.Pink;
					tooltips_str += $"已停用。";
				}
				else if (unit.IsActive())
				{
					default_color = Color.YellowGreen;
					tooltips_str += $"激活中。";
				}
				else
				{
					default_color = Color.White;
					tooltips_str += $"休眠中。";
				}

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
							subitem.Text = basicConfigValue.mercCacheConfig.awakeTime.ToString("G");
							if (unit.Enable && basicConfigValue.mercCacheConfig.mode == TASK_MODE.挂机收菜.ToString())
								subitem.BackColor = GetColor(basicConfigValue.mercCacheConfig.awakePeriod, new TimeSpan(basicConfigValue.mercCacheConfig.awakeTime.Ticks - DateTime.Now.Ticks).TotalSeconds,
									new List<Color>() { Color.White, default_color });
							break;
						case LIST_UNIT_COLUMN.成员:
							subitem.Text = unit.ID;
							break;
						case LIST_UNIT_COLUMN.当前模式:
							if (basicConfigValue.mercCacheConfig.PluginEnable)
								subitem.Text = basicConfigValue.mercCacheConfig.mode;
							else if(basicConfigValue.bgCacheConfig.PluginEnable)
								subitem.Text = "酒馆";
							else
								subitem.Text = "传统对战";
							break;
						case LIST_UNIT_COLUMN.预设模式:
							subitem.Text = currentTask.Mode.ToString();
							//tooltips_str += $"{subitem.Text}，";
							break;
						case LIST_UNIT_COLUMN.启用时间段:
							subitem.Text = currentTask.StartTime.ToString("HH:mm") + "-" + currentTask.StopTime.ToString("HH:mm");
							break;
						case LIST_UNIT_COLUMN.等级:
							subitem.Text = unit.XP.Level.ToString();
							//tooltips_str += $"{unit.XP.Level}级，";
							break;
						case LIST_UNIT_COLUMN.经验:
							subitem.Text = unit.XP.TotalXP.ToString();
							//tooltips_str += $"{unit.XP.TotalXP}经验，";
							break;
						case LIST_UNIT_COLUMN.PVP分数:
							subitem.Text = unit.MercPvpRate.ToString();
							break;
						case LIST_UNIT_COLUMN.传统模式等级:
							subitem.Text = unit.ClassicRate;
							//tooltips_str += $"{unit.ClassicRate}";
							break;
						case LIST_UNIT_COLUMN.经验效率:
							subitem.Text = unit.XPRate;
							break;
						default:
							break;
					}
					item.SubItems.Add(subitem);
				}
				listHS.Items.Add(item);

				if (tooltips_str.Length > 64)
					tooltips_str = tooltips_str.Substring(0, 60) + "..."; // 截断字符串
				notifyIcon1.Text = tooltips_str;
			}
		}
		private void 启用ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				string unitID = HSUnitManager.Get().GetIDByIndex(listHS.SelectedItems[0].Index);
				Out.Info($"[{unitID}] 界面操作：启用/停用");
				HSUnitManager.Get().FlipEnable(listHS.SelectedItems[0].Index);
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

		private void listHS_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;
				HSUnitForm dlg = new HSUnitForm(HSUnitManager.Get().GetUnitByIndex(index));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				HSUnitManager.Get().Modify(index, dlg.Unit);
				UI_Flush();
			}
		}

		private void 启动ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				string unitID = HSUnitManager.Get().GetIDByIndex(listHS.SelectedItems[0].Index);
				Out.Info($"[{unitID}] 界面操作：启动一次");

				int index = listHS.SelectedItems[0].Index;

				HSUnitManager.Get().StartOnce(index);
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void 备份插件设置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				HSUnitManager.Get().BackUpConfig();
				MessageBox.Show(string.Format("备份完成"));
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString()), "ERROR");
			}
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

		private void btn_selecthspath_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Battle.net Launcher.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
				return;

			textbox_BNetPath.Text = openFileDialog.FileName;

			if (MessageBox.Show("设置保存成功，下次启动时生效，是否马上重启软件？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				Application.Exit();
				System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
			}
		}

		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			ShowWindow();
		}

		private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowWindow();
		}

		private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			flag_exit = true;
			Close();
		}

		private void 部署插件配置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				HSUnitManager.Get().InitConfig(listHS.SelectedItems[0].Index);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void 关闭插件ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				HSUnitManager.Get().ReleaseConfig(listHS.SelectedItems[0].Index);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void 重置经验效率ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				string unitID = HSUnitManager.Get().GetIDByIndex(listHS.SelectedItems[0].Index);
				Out.Info($"[{unitID}] 界面操作：重置经验效率");
				HSUnitManager.Get().ResetXPRate(listHS.SelectedItems[0].Index);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		// 处理 日志事件
		private void OnLogged(string level, string message)
		{
			if (listLog.InvokeRequired)
			{
				// 确保线程安全
				listLog.Invoke(new Action(() => OnLogged(level, message)));
				return;
			}
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var item = new ListViewItem($"[{timestamp}] {level}: {message}");
			switch (level)
			{
				case "Info": item.ForeColor = Color.Black; break;
				case "Error": item.ForeColor = Color.Red; break;
				default: return;
			}

			// 向 ListBox 添加日志并限制行数
			if (listLog.Items.Count >= 500)
				listLog.Items.RemoveAt(0); // 移除最早的一行

			listLog.Items.Add(item);
			listLog.EnsureVisible(listLog.Items.Count - 1); // 滚动到最新行

		}

		private void listLog_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			// 获取当天日志文件路径
			string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log"; // 当天日志文件名
			string logFilePath = Out.LogBaseName + logFileName;

			// 检查日志文件是否存在
			if (System.IO.File.Exists(logFilePath))
			{
				// 使用默认文本编辑器打开日志文件
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = System.IO.Path.Combine(Environment.CurrentDirectory,logFilePath),
					UseShellExecute = true
				});
			}
		}

		private void 停用一天ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				string unitID = HSUnitManager.Get().GetIDByIndex(listHS.SelectedItems[0].Index);
				Out.Info($"[{unitID}] 界面操作：停用一天");
				HSUnitManager.Get().SetPause(listHS.SelectedItems[0].Index, 24);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}
	}
}
