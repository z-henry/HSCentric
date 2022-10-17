using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HSCentric.Const;
using System.Net.Sockets;
// using MimeKit;

namespace HSCentric
{
	public sealed partial class MainForm : Form
	{
		public MainForm()
		{
// 			TcpClient client = new TcpClient("cn.version.battle.net", 1119);
// 			using (NetworkStream stream = client.GetStream())
// 			{
// 				byte[] req = Encoding.UTF8.GetBytes("v1/products/hsb/versions\r\n");
// 
// 				stream.Write(req, 0, req.Length);
// 
// 				var message = MimeMessage.Load(stream);
// 				int a = 0;
// 			}

			this.InitializeComponent();
			this.timer1.Interval = 100;
			this.timer1.Tick += this.TickProcess;

			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用.ToString(), 40);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.成员.ToString(), 80);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.当前模式.ToString(), 70);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.预设模式.ToString(), 70);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.唤醒时间.ToString(), 130);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.启用时间段.ToString(), 120);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.等级.ToString(), 60);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.经验.ToString(), 50);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.PVP分数.ToString(), 60);
			this.listHS.Columns.Add(LIST_UNIT_COLUMN.传统模式等级.ToString(), 140);

			HSUnitManager.Init();
			UI_Flush();
			MyRestFul.Init(ConfigurationManager.AppSettings["rest_url"]);
			textbox_Path.Text = ConfigurationManager.AppSettings["hs_path"];
			this.timer1.Start();
		}
		private void TickProcess(object sender, EventArgs e)
		{
			//检测重启
			TimeSpan timespan_checkpriod = new TimeSpan(0, 0, 10);//检测间隔
			if (DateTime.Now > m_CheckTime_runinfo)
			{
				m_CheckTime_runinfo = DateTime.Now.AddSeconds(timespan_checkpriod.TotalSeconds);
				HSUnitManager.Check();
				UI_Flush();
			}

			//检测日志
			TimeSpan timespan_readlogpriod = new TimeSpan(0, 1, 0);
			if (DateTime.Now > m_CheckTime_readlog)
			{
				m_CheckTime_readlog = DateTime.Now.AddSeconds(timespan_readlogpriod.TotalSeconds);
				HSUnitManager.CheckLog();
			}

			label_currenttime.Text = DateTime.Now.ToString("G");
			label_checktime.Text = m_CheckTime_runinfo.ToString("G");
			label_checktime.BackColor = GetColor(timespan_checkpriod.TotalMilliseconds, new TimeSpan(m_CheckTime_runinfo.Ticks - DateTime.Now.Ticks).TotalMilliseconds,
				new List<Color>() { Color.YellowGreen, Color.Yellow, Color.Red });
		}
		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Stop();
			HSUnitManager.Release();
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.AppSettings.Settings["hs_path"].Value = textbox_Path.Text;
			config.Save();
		}
		private void btn_add_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(HSUnitManager.m_hsPath))
			{
				MessageBox.Show("请先设置炉石路径", "ERROR");
				return;
			}
			HSUnitForm dlg = new HSUnitForm();
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			HSUnitManager.Add(dlg.Unit);
			UI_Flush();
		}
		private void btn_del_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				HSUnitManager.Remove(listHS.SelectedItems[0].Index);
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}
		private void UI_Flush()
		{
			List<HSUnit> listsHS = HSUnitManager.GetHSUnits();
			listHS.Items.Clear();

			foreach (HSUnit unit in listsHS)
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
							subitem.Text = unit.ID;
							break;
						case LIST_UNIT_COLUMN.当前模式:
							subitem.Text = basicConfigValue.MercPluginEnable ? basicConfigValue.Mode : "非佣兵";
							break;
						case LIST_UNIT_COLUMN.预设模式:
							subitem.Text = currentTask.Mode.ToString();
							break;
						case LIST_UNIT_COLUMN.启用时间段:
							subitem.Text = currentTask.StartTime.ToString("T") + " - " + currentTask.StopTime.ToString("T");
							break;
						case LIST_UNIT_COLUMN.等级:
							subitem.Text = $"等级:{unit.XP.Level}";
							break;
						case LIST_UNIT_COLUMN.经验:
							subitem.Text = unit.XP.TotalXP.ToString();
							break;
						case LIST_UNIT_COLUMN.PVP分数:
							subitem.Text = unit.MercPvpRate.ToString();
							break;
						case LIST_UNIT_COLUMN.传统模式等级:
							subitem.Text = unit.ClassicRate;
							break;
						default:
							break;
					}
					item.SubItems.Add(subitem);
				}
				listHS.Items.Add(item);
			}
		}
		private void 启用ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				HSUnitManager.FlipEnable(listHS.SelectedItems[0].Index);
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
				HSUnitForm dlg = new HSUnitForm(HSUnitManager.Get(index));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				HSUnitManager.Modify(index, dlg.Unit);
				UI_Flush();
			}
		}

		private void 启动ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listHS.SelectedItems.Count > 0)
			{
				int index = listHS.SelectedItems[0].Index;

				HSUnitManager.StartOnce(index);
			}
			else
				MessageBox.Show("请选中一个成员", "ERROR");
		}

		private void 备份插件设置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				HSUnitManager.BackUpConfig();
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

		private void btn_selectpath_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Hearthstone.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
				return;

			textbox_Path.Text = openFileDialog.FileName;

			if (MessageBox.Show("设置保存成功，下次启动时生效，是否马上重启软件？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				Application.Exit();
				System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);

			}
		}
	}
}
