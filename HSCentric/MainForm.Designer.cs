
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace HSCentric
{
	partial class MainForm
	{
		/// <summary>
		/// 必需的设计器变量。
		/// </summary>
		private IContainer components;

		/// <summary>
		/// 清理所有正在使用的资源。
		/// </summary>
		/// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows 窗体设计器生成的代码

		/// <summary>
		/// 设计器支持所需的方法 - 不要修改
		/// 使用代码编辑器修改此方法的内容。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.btn_add = new System.Windows.Forms.Button();
			this.btn_del = new System.Windows.Forms.Button();
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.SuspendLayout();
			// 
			// btn_add
			// 
			this.btn_add.Location = new System.Drawing.Point(89, 134);
			this.btn_add.Name = "btn_add";
			this.btn_add.Size = new System.Drawing.Size(75, 23);
			this.btn_add.TabIndex = 8;
			this.btn_add.Text = "添加";
			this.btn_add.UseVisualStyleBackColor = true;
			this.btn_add.Click += new System.EventHandler(this.btn_add_Click);
			// 
			// btn_del
			// 
			this.btn_del.Location = new System.Drawing.Point(247, 134);
			this.btn_del.Name = "btn_del";
			this.btn_del.Size = new System.Drawing.Size(75, 23);
			this.btn_del.TabIndex = 9;
			this.btn_del.Text = "删除";
			this.btn_del.UseVisualStyleBackColor = true;
			this.btn_del.Click += new System.EventHandler(this.btn_del_Click);
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.Location = new System.Drawing.Point(12, 12);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(383, 116);
			this.checkedListBox1.TabIndex = 10;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(407, 166);
			this.Controls.Add(this.checkedListBox1);
			this.Controls.Add(this.btn_del);
			this.Controls.Add(this.btn_add);
			this.Name = "MainForm";
			this.Text = "炉石监控程序 3.1.0";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
			this.ResumeLayout(false);

		}
		#endregion
		private Timer timer1;
		private Button btn_add;
		private Button btn_del;
		private CheckedListBox checkedListBox1;



		private List<HSUnit> m_listHS = new List<HSUnit>();
		private object m_lockHS = new object();
	}
}

