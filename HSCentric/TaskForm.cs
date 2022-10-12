using HSCentric.Const;
using System;
using System.Windows.Forms;

namespace HSCentric
{
	public partial class TaskForm : Form
	{
		public TaskForm(TaskUnit task = null)
		{
			InitializeComponent();
			foreach (var iter in Enum.GetValues(typeof(Const.BEHAVIOR_MODE)))
				comboBox_strategy.Items.Add(iter.ToString());

			if (task != null)
				m_task = (TaskUnit)task.DeepClone();

			foreach (TASK_MODE mode_iter in Enum.GetValues(typeof(TASK_MODE)))
				comboBox_mode.Items.Add(mode_iter.ToString());

			comboBox_mode.SelectedIndex = (int)Task.Mode;
			textBox_team.Text = Task.TeamName;
			comboBox_strategy.Text = Task.StrategyName;
			dateTimePicker_start.Value = Task.StartTime;
			dateTimePicker_stop.Value = Task.StopTime;
			checkBoxScale.Checked = Task.Scale;
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

			if (comboBox_strategy.Text.Length <= 0)
			{
				MessageBox.Show("请填写策略名称");
				return;
			}
			Task.StrategyName = comboBox_strategy.Text;

			Task.StartTime = dateTimePicker_start.Value;
			Task.StopTime = dateTimePicker_stop.Value;
			if (!Task.IsTimeLegal())
			{
				MessageBox.Show("请填写正确的时间段");
				return;
			}
			Task.Scale = checkBoxScale.Checked;
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
