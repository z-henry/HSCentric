using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HSCentric
{
	public partial class HSUnitForm : Form
	{
		public HSUnitForm(string _HSPath, bool _Enable, DateTime _StartTime, DateTime _StopTime)
		{
			InitializeComponent();
			checkbox_Enable.Checked = _Enable;
			textbox_Path.Text = _HSPath;
			textbox_StartHour.Text = _StartTime.Hour.ToString();
			textbox_StartMin.Text = _StartTime.Minute.ToString();
			textbox_StopHour.Text = _StopTime.Hour.ToString();
			textbox_StopMin.Text = _StopTime.Minute.ToString();
		}
		public HSUnitForm()
		{
			InitializeComponent();
			textbox_StartHour.Text = "0";
			textbox_StartMin.Text = "0";
			textbox_StopHour.Text = "23";
			textbox_StopMin.Text = "59";

		}

		public bool Enable
		{
			get
			{
				return m_Enable;
			}
		}
		public DateTime StartTime
		{
			get
			{
				return new DateTime(2000, 1, 1, m_StartHour, m_StartMin, 59);
			}
		}
		public DateTime StopTime
		{
			get
			{
				return new DateTime(2000, 1, 1, m_StopHour, m_StopMin, 59);
			}
		}
		public string Path
		{
			get
			{
				return m_Path;
			}
		}



		private void btn_ok_Click(object sender, EventArgs e)
		{
			if (!CheckHour(textbox_StartHour.Text))
			{
				MessageBox.Show("开始小时 请输入0-23的数字", "Error");
				return;
			}

			if (!CheckHour(textbox_StopHour.Text))
			{
				MessageBox.Show("开始小时 请输入0-23的数字", "Error");
				return;
			}

			if (!CheckMin(textbox_StartMin.Text))
			{ 
				MessageBox.Show("结束分钟 请输入0-59的数字", "Error");
				return;
			}

			if (!CheckMin(textbox_StopMin.Text))
			{ 
				MessageBox.Show("结束分钟 请输入0-59的数字", "Error");
				return;
			}

			if (textbox_Path.Text.Length <= 0)
			{
				MessageBox.Show("请选择炉石路径", "Error");
				return;
			}

			m_Path = textbox_Path.Text;
			m_Enable = checkbox_Enable.Checked;
			m_StartHour = int.Parse(textbox_StartHour.Text);
			m_StartMin = int.Parse(textbox_StartMin.Text);
			m_StopHour = int.Parse(textbox_StopHour.Text);
			m_StopMin = int.Parse(textbox_StopMin.Text);
			DialogResult = DialogResult.OK;
			Close();
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
		}

		private bool CheckMin(string _text)
		{

			Regex regex = new Regex(@"^[0-5]?\d$");
			Match match = regex.Match(_text);
			return match.Success;
		}
		private bool CheckHour(string _text)
		{

			Regex regex = new Regex(@"^2[0-3]|[0-1]?\d$");
			Match match = regex.Match(_text);
			return match.Success;
		}

		private bool m_Enable;
		private string m_Path;
		private int m_StartHour;
		private int m_StartMin;
		private int m_StopHour;
		private int m_StopMin;
	}
}
