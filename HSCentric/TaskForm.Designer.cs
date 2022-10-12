
namespace HSCentric
{
	partial class TaskForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.comboBox_mode = new System.Windows.Forms.ComboBox();
			this.textBox_team = new System.Windows.Forms.TextBox();
			this.dateTimePicker_start = new System.Windows.Forms.DateTimePicker();
			this.dateTimePicker_stop = new System.Windows.Forms.DateTimePicker();
			this.btn_ok = new System.Windows.Forms.Button();
			this.btn_cancel = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.checkBoxScale = new System.Windows.Forms.CheckBox();
			this.comboBox_strategy = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(33, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "模式";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 39);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "队伍名称";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 12);
			this.label3.TabIndex = 2;
			this.label3.Text = "开始时间";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 123);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(53, 12);
			this.label4.TabIndex = 3;
			this.label4.Text = "停止时间";
			// 
			// comboBox_mode
			// 
			this.comboBox_mode.FormattingEnabled = true;
			this.comboBox_mode.Location = new System.Drawing.Point(68, 10);
			this.comboBox_mode.Name = "comboBox_mode";
			this.comboBox_mode.Size = new System.Drawing.Size(121, 20);
			this.comboBox_mode.TabIndex = 4;
			// 
			// textBox_team
			// 
			this.textBox_team.Location = new System.Drawing.Point(68, 36);
			this.textBox_team.Name = "textBox_team";
			this.textBox_team.Size = new System.Drawing.Size(120, 21);
			this.textBox_team.TabIndex = 5;
			// 
			// dateTimePicker_start
			// 
			this.dateTimePicker_start.Format = System.Windows.Forms.DateTimePickerFormat.Time;
			this.dateTimePicker_start.Location = new System.Drawing.Point(68, 90);
			this.dateTimePicker_start.Name = "dateTimePicker_start";
			this.dateTimePicker_start.Size = new System.Drawing.Size(121, 21);
			this.dateTimePicker_start.TabIndex = 6;
			// 
			// dateTimePicker_stop
			// 
			this.dateTimePicker_stop.Format = System.Windows.Forms.DateTimePickerFormat.Time;
			this.dateTimePicker_stop.Location = new System.Drawing.Point(68, 117);
			this.dateTimePicker_stop.Name = "dateTimePicker_stop";
			this.dateTimePicker_stop.Size = new System.Drawing.Size(120, 21);
			this.dateTimePicker_stop.TabIndex = 7;
			// 
			// btn_ok
			// 
			this.btn_ok.Location = new System.Drawing.Point(35, 169);
			this.btn_ok.Name = "btn_ok";
			this.btn_ok.Size = new System.Drawing.Size(75, 23);
			this.btn_ok.TabIndex = 8;
			this.btn_ok.Text = "确定";
			this.btn_ok.UseVisualStyleBackColor = true;
			this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
			// 
			// btn_cancel
			// 
			this.btn_cancel.Location = new System.Drawing.Point(116, 169);
			this.btn_cancel.Name = "btn_cancel";
			this.btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.btn_cancel.TabIndex = 9;
			this.btn_cancel.Text = "取消";
			this.btn_cancel.UseVisualStyleBackColor = true;
			this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 66);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(53, 12);
			this.label5.TabIndex = 10;
			this.label5.Text = "策略名称";
			// 
			// checkBoxScale
			// 
			this.checkBoxScale.AutoSize = true;
			this.checkBoxScale.Location = new System.Drawing.Point(12, 144);
			this.checkBoxScale.Name = "checkBoxScale";
			this.checkBoxScale.Size = new System.Drawing.Size(72, 16);
			this.checkBoxScale.TabIndex = 12;
			this.checkBoxScale.Text = "启用齿轮";
			this.checkBoxScale.UseVisualStyleBackColor = true;
			// 
			// comboBox_strategy
			// 
			this.comboBox_strategy.FormattingEnabled = true;
			this.comboBox_strategy.Location = new System.Drawing.Point(67, 63);
			this.comboBox_strategy.Name = "comboBox_strategy";
			this.comboBox_strategy.Size = new System.Drawing.Size(121, 20);
			this.comboBox_strategy.TabIndex = 13;
			// 
			// TaskForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(202, 204);
			this.Controls.Add(this.comboBox_strategy);
			this.Controls.Add(this.checkBoxScale);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.btn_cancel);
			this.Controls.Add(this.btn_ok);
			this.Controls.Add(this.dateTimePicker_stop);
			this.Controls.Add(this.dateTimePicker_start);
			this.Controls.Add(this.textBox_team);
			this.Controls.Add(this.comboBox_mode);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "TaskForm";
			this.Text = "TaskForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBox_mode;
		private System.Windows.Forms.TextBox textBox_team;
		private System.Windows.Forms.DateTimePicker dateTimePicker_start;
		private System.Windows.Forms.DateTimePicker dateTimePicker_stop;
		private System.Windows.Forms.Button btn_ok;
		private System.Windows.Forms.Button btn_cancel;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox checkBoxScale;
		private System.Windows.Forms.ComboBox comboBox_strategy;
	}
}