
namespace HSCentric
{
	partial class HSUnitForm
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
			this.textbox_Path = new System.Windows.Forms.TextBox();
			this.textbox_StartHour = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.textbox_StartMin = new System.Windows.Forms.TextBox();
			this.textbox_StopMin = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.textbox_StopHour = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.btn_selectpath = new System.Windows.Forms.Button();
			this.btn_ok = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.checkbox_Enable = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 43);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "炉石路径";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 80);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "启用区间";
			// 
			// textbox_Path
			// 
			this.textbox_Path.Location = new System.Drawing.Point(71, 40);
			this.textbox_Path.Name = "textbox_Path";
			this.textbox_Path.Size = new System.Drawing.Size(287, 21);
			this.textbox_Path.TabIndex = 2;
			// 
			// textbox_StartHour
			// 
			this.textbox_StartHour.Location = new System.Drawing.Point(71, 77);
			this.textbox_StartHour.Name = "textbox_StartHour";
			this.textbox_StartHour.Size = new System.Drawing.Size(29, 21);
			this.textbox_StartHour.TabIndex = 3;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(106, 80);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(17, 12);
			this.label3.TabIndex = 5;
			this.label3.Text = "时";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(164, 80);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 12);
			this.label4.TabIndex = 6;
			this.label4.Text = "分";
			// 
			// textbox_StartMin
			// 
			this.textbox_StartMin.Location = new System.Drawing.Point(129, 77);
			this.textbox_StartMin.Name = "textbox_StartMin";
			this.textbox_StartMin.Size = new System.Drawing.Size(29, 21);
			this.textbox_StartMin.TabIndex = 7;
			// 
			// textbox_StopMin
			// 
			this.textbox_StopMin.Location = new System.Drawing.Point(278, 77);
			this.textbox_StopMin.Name = "textbox_StopMin";
			this.textbox_StopMin.Size = new System.Drawing.Size(29, 21);
			this.textbox_StopMin.TabIndex = 11;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(313, 80);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(17, 12);
			this.label5.TabIndex = 10;
			this.label5.Text = "分";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(255, 80);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(17, 12);
			this.label6.TabIndex = 9;
			this.label6.Text = "时";
			// 
			// textbox_StopHour
			// 
			this.textbox_StopHour.Location = new System.Drawing.Point(220, 77);
			this.textbox_StopHour.Name = "textbox_StopHour";
			this.textbox_StopHour.Size = new System.Drawing.Size(29, 21);
			this.textbox_StopHour.TabIndex = 8;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(187, 80);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(17, 12);
			this.label7.TabIndex = 12;
			this.label7.Text = "至";
			// 
			// btn_selectpath
			// 
			this.btn_selectpath.Location = new System.Drawing.Point(364, 40);
			this.btn_selectpath.Name = "btn_selectpath";
			this.btn_selectpath.Size = new System.Drawing.Size(40, 23);
			this.btn_selectpath.TabIndex = 13;
			this.btn_selectpath.Text = "...";
			this.btn_selectpath.UseVisualStyleBackColor = true;
			this.btn_selectpath.Click += new System.EventHandler(this.btn_selectpath_Click);
			// 
			// btn_ok
			// 
			this.btn_ok.Location = new System.Drawing.Point(326, 108);
			this.btn_ok.Name = "btn_ok";
			this.btn_ok.Size = new System.Drawing.Size(75, 23);
			this.btn_ok.TabIndex = 14;
			this.btn_ok.Text = "确认";
			this.btn_ok.UseVisualStyleBackColor = true;
			this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(12, 9);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(29, 12);
			this.label8.TabIndex = 15;
			this.label8.Text = "启用";
			// 
			// checkbox_Enable
			// 
			this.checkbox_Enable.AutoSize = true;
			this.checkbox_Enable.Location = new System.Drawing.Point(71, 8);
			this.checkbox_Enable.Name = "checkbox_Enable";
			this.checkbox_Enable.Size = new System.Drawing.Size(15, 14);
			this.checkbox_Enable.TabIndex = 16;
			this.checkbox_Enable.UseVisualStyleBackColor = true;
			// 
			// HSUnitForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(412, 135);
			this.Controls.Add(this.checkbox_Enable);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.btn_ok);
			this.Controls.Add(this.btn_selectpath);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.textbox_StopMin);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.textbox_StopHour);
			this.Controls.Add(this.textbox_StartMin);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.textbox_StartHour);
			this.Controls.Add(this.textbox_Path);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "HSUnitForm";
			this.Text = "HSUnitForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textbox_Path;
		private System.Windows.Forms.TextBox textbox_StartHour;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textbox_StartMin;
		private System.Windows.Forms.TextBox textbox_StopMin;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox textbox_StopHour;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button btn_selectpath;
		private System.Windows.Forms.Button btn_ok;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkbox_Enable;
	}
}