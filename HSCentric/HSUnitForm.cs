using HSCentric.Const;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace HSCentric
{
	public partial class HSUnitForm : Form
	{
		public HSUnitForm(HSUnit unit = null)
		{
			InitializeComponent();
			if (unit != null)
				m_unit = (HSUnit)unit.DeepClone();
			checkbox_Enable.Checked = m_unit.Enable;
			textbox_HBPath.Text = m_unit.HBPath;
			textbox_ID.Text = m_unit.ID;
			textbox_Token.Text = m_unit.Token;

			this.listTasks.Columns.Add(LIST_TASK_COLUMN.模式.ToString(), 80);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.队伍.ToString(), 40);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.策略.ToString(), 40);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.启动时间.ToString(), 80);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.停止时间.ToString(), 80);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.齿轮.ToString(), 40);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.地图.ToString(), 40);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.核心.ToString(), 40);
			this.listTasks.Columns.Add(LIST_TASK_COLUMN.总数.ToString(), 40);

			UI_Flush();
		}

		public HSUnit Unit
		{
			get { return m_unit; }
		}


		private void btn_ok_Click(object sender, EventArgs e)
		{
			if (textbox_ID.Text.Length <= 0)
			{
				MessageBox.Show("请输入一个炉石ID", "Error");
				return;
			}
			if (textbox_Token.Text.Length <= 0)
			{
				MessageBox.Show("请输入炉石Token", "Error");
				return;
			}
			if (listTasks.Items.Count <= 0)
			{
				MessageBox.Show("请添加一个模式", "Error");
				return;
			}

			m_unit.HBPath = textbox_HBPath.Text;
			m_unit.ID = textbox_ID.Text;
			m_unit.Token = textbox_Token.Text;
			m_unit.Enable = checkbox_Enable.Checked;
			DialogResult = DialogResult.OK;
			Close();
		}
		private void btn_cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void btn_selecthbpath_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Hearthbuddy.exe|*.exe";
			openFileDialog.DereferenceLinks = false;
			openFileDialog.ShowDialog();
			if (string.IsNullOrEmpty(openFileDialog.FileName))
				return;

			textbox_HBPath.Text = openFileDialog.FileName;
		}

		private void 添加ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TaskForm dlg = new TaskForm();
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			if (Common.IsBuddyMode(dlg.Task.Mode) &&
				textbox_HBPath.Text.Length == 0)
			{
				MessageBox.Show("添加兄弟模式前先添加兄弟路径", "ERROR");
				return;
			}

			if (!m_unit.Tasks.Add(dlg.Task))
			{
				MessageBox.Show("所选时间段有重合，添加失败", "ERROR");
				return;
			}
			UI_Flush();
		}

		private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listTasks.SelectedItems.Count > 0)
			{
				if (!m_unit.Tasks.Remove(listTasks.SelectedItems[0].Index))
				{
					MessageBox.Show("删除失败", "ERROR");
					return;
				}

				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个任务", "ERROR");
		}

		private void 修改ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (listTasks.SelectedItems.Count > 0)
			{
				int index = listTasks.SelectedItems[0].Index;
				TaskForm dlg = new TaskForm((TaskUnit)m_unit.Tasks.GetTask(index));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				if (Common.IsBuddyMode(dlg.Task.Mode) &&
					textbox_HBPath.Text.Length == 0)
				{
					MessageBox.Show("添加兄弟模式前先添加兄弟路径", "ERROR");
					return;
				}

				if (!m_unit.Tasks.Modify(index, dlg.Task))
				{
					MessageBox.Show("所选时间段有重合，添加失败", "ERROR");
					return;
				}
				UI_Flush();
			}
			else
				MessageBox.Show("请选中一个任务", "ERROR");
		}

		private void listTasks_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (listTasks.SelectedItems.Count < 0)
				{
					contextMenuStrip_RMenu.Items[(int)R_TASK_MENU.删除].Enabled = false; 
					contextMenuStrip_RMenu.Items[(int)R_TASK_MENU.修改].Enabled = false;

				}
				contextMenuStrip_RMenu.Show(listTasks, new Point(e.X, e.Y));
			}
		}

		private void listTasks_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (listTasks.SelectedItems.Count > 0)
			{
				int index = listTasks.SelectedItems[0].Index;
				TaskForm dlg = new TaskForm((TaskUnit)m_unit.Tasks.GetTask(index));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				if (!m_unit.Tasks.Modify(index, dlg.Task))
				{
					MessageBox.Show("所选时间段有重合，添加失败", "ERROR");
					return;
				}
				UI_Flush();
			}
		}
		private void UI_Flush()
		{
			listTasks.Items.Clear();

			foreach (TaskUnit task in m_unit.Tasks.GetTasks())
			{
				ListViewItem item = new ListViewItem();
				item.Text = task.Mode.ToString();

				foreach (LIST_TASK_COLUMN list_item in Enum.GetValues(typeof(LIST_TASK_COLUMN)))
				{
					if (list_item == LIST_TASK_COLUMN.模式)
						continue;

					ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
					switch (list_item)
					{
						case LIST_TASK_COLUMN.队伍:
							subitem.Text = task.TeamName;
							break;
						case LIST_TASK_COLUMN.策略:
							subitem.Text = task.StrategyName;
							break;
						case LIST_TASK_COLUMN.停止时间:
							subitem.Text = task.StopTime.ToString("T");
							break;
						case LIST_TASK_COLUMN.启动时间:
							subitem.Text = task.StartTime.ToString("T");
							break;
						case LIST_TASK_COLUMN.齿轮:
							subitem.Text = task.Scale? "√":"";
							break;
						case LIST_TASK_COLUMN.地图:
							if (Common.IsBuddyMode(task.Mode))
								subitem.Text = "--";
							else
								subitem.Text = task.Map;
							break;
						case LIST_TASK_COLUMN.核心:
							if (Common.IsBuddyMode(task.Mode))
								subitem.Text = "--";
							else
								subitem.Text = task.MercTeamNumCore.ToString();
							break;
						case LIST_TASK_COLUMN.总数:
							if (Common.IsBuddyMode(task.Mode))
								subitem.Text = "--";
							else
								subitem.Text = task.MercTeamNumTotal.ToString();
							break;
						default:
							break;
					}
					item.SubItems.Add(subitem);
				}
				listTasks.Items.Add(item);
			}
		}


		private HSUnit m_unit = new HSUnit();
	}
}
