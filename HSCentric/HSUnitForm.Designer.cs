
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
			this.components = new System.ComponentModel.Container();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textbox_Path = new System.Windows.Forms.TextBox();
			this.btn_selectpath = new System.Windows.Forms.Button();
			this.btn_ok = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.checkbox_Enable = new System.Windows.Forms.CheckBox();
			this.btn_cancel = new System.Windows.Forms.Button();
			this.listTasks = new System.Windows.Forms.ListView();
			this.contextMenuStrip_RMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.添加ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.删除ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.修改ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.textbox_Token = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.textbox_ID = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.btn_selecthbpath = new System.Windows.Forms.Button();
			this.textbox_HBPath = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.contextMenuStrip_RMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 45);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "炉石路径";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 134);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "模式设置";
			// 
			// textbox_Path
			// 
			this.textbox_Path.Location = new System.Drawing.Point(71, 40);
			this.textbox_Path.Name = "textbox_Path";
			this.textbox_Path.Size = new System.Drawing.Size(287, 21);
			this.textbox_Path.TabIndex = 2;
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
			this.btn_ok.Location = new System.Drawing.Point(232, 333);
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
			this.label8.Location = new System.Drawing.Point(36, 16);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(29, 12);
			this.label8.TabIndex = 15;
			this.label8.Text = "启用";
			// 
			// checkbox_Enable
			// 
			this.checkbox_Enable.AutoSize = true;
			this.checkbox_Enable.Location = new System.Drawing.Point(71, 16);
			this.checkbox_Enable.Name = "checkbox_Enable";
			this.checkbox_Enable.Size = new System.Drawing.Size(15, 14);
			this.checkbox_Enable.TabIndex = 16;
			this.checkbox_Enable.UseVisualStyleBackColor = true;
			// 
			// btn_cancel
			// 
			this.btn_cancel.Location = new System.Drawing.Point(325, 333);
			this.btn_cancel.Name = "btn_cancel";
			this.btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.btn_cancel.TabIndex = 17;
			this.btn_cancel.Text = "取消";
			this.btn_cancel.UseVisualStyleBackColor = true;
			this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
			// 
			// listTasks
			// 
			this.listTasks.FullRowSelect = true;
			this.listTasks.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listTasks.HideSelection = false;
			this.listTasks.Location = new System.Drawing.Point(14, 149);
			this.listTasks.MultiSelect = false;
			this.listTasks.Name = "listTasks";
			this.listTasks.Size = new System.Drawing.Size(390, 178);
			this.listTasks.TabIndex = 18;
			this.listTasks.UseCompatibleStateImageBehavior = false;
			this.listTasks.View = System.Windows.Forms.View.Details;
			this.listTasks.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listTasks_MouseDoubleClick);
			this.listTasks.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listTasks_MouseUp);
			// 
			// contextMenuStrip_RMenu
			// 
			this.contextMenuStrip_RMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.添加ToolStripMenuItem,
            this.删除ToolStripMenuItem,
            this.修改ToolStripMenuItem});
			this.contextMenuStrip_RMenu.Name = "contextMenuStrip_RMenu";
			this.contextMenuStrip_RMenu.Size = new System.Drawing.Size(101, 70);
			// 
			// 添加ToolStripMenuItem
			// 
			this.添加ToolStripMenuItem.Name = "添加ToolStripMenuItem";
			this.添加ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
			this.添加ToolStripMenuItem.Text = "添加";
			this.添加ToolStripMenuItem.Click += new System.EventHandler(this.添加ToolStripMenuItem_Click);
			// 
			// 删除ToolStripMenuItem
			// 
			this.删除ToolStripMenuItem.Name = "删除ToolStripMenuItem";
			this.删除ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
			this.删除ToolStripMenuItem.Text = "删除";
			this.删除ToolStripMenuItem.Click += new System.EventHandler(this.删除ToolStripMenuItem_Click);
			// 
			// 修改ToolStripMenuItem
			// 
			this.修改ToolStripMenuItem.Name = "修改ToolStripMenuItem";
			this.修改ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
			this.修改ToolStripMenuItem.Text = "修改";
			this.修改ToolStripMenuItem.Click += new System.EventHandler(this.修改ToolStripMenuItem_Click);
			// 
			// textbox_Token
			// 
			this.textbox_Token.Location = new System.Drawing.Point(71, 94);
			this.textbox_Token.Name = "textbox_Token";
			this.textbox_Token.Size = new System.Drawing.Size(287, 21);
			this.textbox_Token.TabIndex = 20;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(30, 97);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(35, 12);
			this.label3.TabIndex = 19;
			this.label3.Text = "token";
			// 
			// textbox_ID
			// 
			this.textbox_ID.Location = new System.Drawing.Point(126, 13);
			this.textbox_ID.Name = "textbox_ID";
			this.textbox_ID.Size = new System.Drawing.Size(232, 21);
			this.textbox_ID.TabIndex = 22;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(103, 16);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 12);
			this.label4.TabIndex = 21;
			this.label4.Text = "id";
			// 
			// btn_selecthbpath
			// 
			this.btn_selecthbpath.Location = new System.Drawing.Point(364, 67);
			this.btn_selecthbpath.Name = "btn_selecthbpath";
			this.btn_selecthbpath.Size = new System.Drawing.Size(40, 23);
			this.btn_selecthbpath.TabIndex = 25;
			this.btn_selecthbpath.Text = "...";
			this.btn_selecthbpath.UseVisualStyleBackColor = true;
			this.btn_selecthbpath.Click += new System.EventHandler(this.btn_selecthbpath_Click);
			// 
			// textbox_HBPath
			// 
			this.textbox_HBPath.Location = new System.Drawing.Point(71, 67);
			this.textbox_HBPath.Name = "textbox_HBPath";
			this.textbox_HBPath.Size = new System.Drawing.Size(287, 21);
			this.textbox_HBPath.TabIndex = 24;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(12, 72);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(53, 12);
			this.label5.TabIndex = 23;
			this.label5.Text = "兄弟路径";
			// 
			// HSUnitForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(412, 368);
			this.Controls.Add(this.btn_selecthbpath);
			this.Controls.Add(this.textbox_HBPath);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.textbox_ID);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.textbox_Token);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.listTasks);
			this.Controls.Add(this.btn_cancel);
			this.Controls.Add(this.checkbox_Enable);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.btn_ok);
			this.Controls.Add(this.btn_selectpath);
			this.Controls.Add(this.textbox_Path);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "HSUnitForm";
			this.Text = "HSUnitForm";
			this.contextMenuStrip_RMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textbox_Path;
		private System.Windows.Forms.Button btn_selectpath;
		private System.Windows.Forms.Button btn_ok;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkbox_Enable;
		private System.Windows.Forms.Button btn_cancel;
		private System.Windows.Forms.ListView listTasks;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip_RMenu;
		private System.Windows.Forms.ToolStripMenuItem 添加ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 删除ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 修改ToolStripMenuItem;
		private System.Windows.Forms.TextBox textbox_Token;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textbox_ID;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button btn_selecthbpath;
		private System.Windows.Forms.TextBox textbox_HBPath;
		private System.Windows.Forms.Label label5;
	}
}