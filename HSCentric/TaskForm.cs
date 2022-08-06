using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HSCentric
{
	public partial class TaskForm : Form
	{
		public TaskForm(TaskUnit task = null)
		{
			InitializeComponent();
			if (task != null)
				m_task = (TaskUnit)task.DeepClone();

			foreach (TASK_MODE mode_iter in Enum.GetValues(typeof(TASK_MODE)))
				comboBox_mode.Items.Add(mode_iter.ToString());

			comboBox_mode.SelectedIndex = (int)Task.Mode;
			textBox_team.Text = Task.TeamName;
			textBox_strategy.Text = Task.StrategyName;
			dateTimePicker_start.Value = Task.StartTime;
			dateTimePicker_stop.Value = Task.StopTime;
		}

		public TaskUnit Task
		{
			get { return m_task; }
		}

		private void btn_ok_Click(object sender, EventArgs e)
		{
			if (comboBox_mode.SelectedIndex < 0)
			{
				MessageBox.Show("请选择模式");
				return;
			}
			Task.Mode = (TASK_MODE)comboBox_mode.SelectedIndex;

			if (textBox_team.Text.Length <= 0)
			{
				MessageBox.Show("请填写队伍名称");
				return;
			}
			Task.TeamName = textBox_team.Text;

			if (textBox_strategy.Text.Length <= 0)
			{
				MessageBox.Show("请填写策略名称");
				return;
			}
			Task.StrategyName = textBox_strategy.Text;

			Task.StartTime = dateTimePicker_start.Value;
			Task.StopTime = dateTimePicker_stop.Value;
			if (!Task.IsTimeLegal())
			{
				MessageBox.Show("请填写正确的时间段");
				return;
			}
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btn_cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}


		private TaskUnit m_task = new TaskUnit();
	}
}
