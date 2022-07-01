
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
		// Token: 0x04000003 RID: 3
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
			this.run = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.pathInput = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.pathInput2 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// run
			// 
			this.run.Location = new System.Drawing.Point(302, 112);
			this.run.Name = "run";
			this.run.Size = new System.Drawing.Size(75, 23);
			this.run.TabIndex = 0;
			this.run.Text = "run";
			this.run.UseVisualStyleBackColor = true;
			this.run.Click += new System.EventHandler(this.run_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(25, 32);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 1;
			this.label1.Text = "战网路径";
			// 
			// pathInput
			// 
			this.pathInput.Location = new System.Drawing.Point(84, 29);
			this.pathInput.Name = "pathInput";
			this.pathInput.Size = new System.Drawing.Size(212, 21);
			this.pathInput.TabIndex = 2;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(302, 29);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 3;
			this.button1.Text = "选择路径";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(301, 61);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 6;
			this.button2.Text = "选择路径";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// pathInput2
			// 
			this.pathInput2.Location = new System.Drawing.Point(83, 61);
			this.pathInput2.Name = "textBox1";
			this.pathInput2.Size = new System.Drawing.Size(212, 21);
			this.pathInput2.TabIndex = 5;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(24, 64);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 4;
			this.label2.Text = "炉石路径";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(400, 145);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.pathInput2);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.pathInput);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.run);
			this.Name = "MainForm";
			this.Text = "炉石监控程序";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		// Token: 0x04000002 RID: 2
		private Timer Tick = new Timer();

		private Button run;

		private Label label1;
		private Label label2;

		private TextBox pathInput;
		private TextBox pathInput2;

		// Token: 0x04000007 RID: 7
		private Button button1;
		private Button button2;


		private bool isRuning;
	}
}

