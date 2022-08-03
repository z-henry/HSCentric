
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
			this.listHS = new System.Windows.Forms.ListView();
			this.contextMenuStrip_RMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.启用ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.更新ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.启动ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip_RMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// btn_add
			// 
			this.btn_add.Location = new System.Drawing.Point(444, 274);
			this.btn_add.Name = "btn_add";
			this.btn_add.Size = new System.Drawing.Size(75, 23);
			this.btn_add.TabIndex = 8;
			this.btn_add.Text = "添加";
			this.btn_add.UseVisualStyleBackColor = true;
			this.btn_add.Click += new System.EventHandler(this.btn_add_Click);
			// 
			// btn_del
			// 
			this.btn_del.Location = new System.Drawing.Point(525, 274);
			this.btn_del.Name = "btn_del";
			this.btn_del.Size = new System.Drawing.Size(75, 23);
			this.btn_del.TabIndex = 9;
			this.btn_del.Text = "删除";
			this.btn_del.UseVisualStyleBackColor = true;
			this.btn_del.Click += new System.EventHandler(this.btn_del_Click);
			// 
			// listHS
			// 
			this.listHS.FullRowSelect = true;
			this.listHS.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listHS.HideSelection = false;
			this.listHS.Location = new System.Drawing.Point(12, 13);
			this.listHS.MultiSelect = false;
			this.listHS.Name = "listHS";
			this.listHS.Size = new System.Drawing.Size(593, 255);
			this.listHS.TabIndex = 10;
			this.listHS.UseCompatibleStateImageBehavior = false;
			this.listHS.View = System.Windows.Forms.View.Details;
			this.listHS.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listHS_MouseDoubleClick);
			this.listHS.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listHS_MouseUp);
			// 
			// contextMenuStrip_RMenu
			// 
			this.contextMenuStrip_RMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.启用ToolStripMenuItem,
            this.更新ToolStripMenuItem,
            this.启动ToolStripMenuItem});
			this.contextMenuStrip_RMenu.Name = "contextMenuStrip1";
			this.contextMenuStrip_RMenu.Size = new System.Drawing.Size(181, 92);
			// 
			// 更新ToolStripMenuItem
			// 
			this.启用ToolStripMenuItem.Name = "启用ToolStripMenuItem";
			this.启用ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.启用ToolStripMenuItem.Text = "启用";
			this.启用ToolStripMenuItem.Click += new System.EventHandler(this.启用ToolStripMenuItem_Click);
			// 
			// 更新ToolStripMenuItem1
			// 
			this.更新ToolStripMenuItem.Name = "更新ToolStripMenuItem1";
			this.更新ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.更新ToolStripMenuItem.Text = "更新";
			this.更新ToolStripMenuItem.Click += new System.EventHandler(this.更新ToolStripMenuItem1_Click);
			// 
			// 启动ToolStripMenuItem
			// 
			this.启动ToolStripMenuItem.Name = "启动ToolStripMenuItem";
			this.启动ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.启动ToolStripMenuItem.Text = "启动";
			this.启动ToolStripMenuItem.Click += new System.EventHandler(this.启动ToolStripMenuItem_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(617, 309);
			this.Controls.Add(this.listHS);
			this.Controls.Add(this.btn_del);
			this.Controls.Add(this.btn_add);
			this.Name = "MainForm";
			this.Text = "炉石监控程序 4.0.0";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
			this.contextMenuStrip_RMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
		private List<HSUnit> m_listHS = new List<HSUnit>();
		private object m_lockHS = new object();
		private int m_TickCount = 0;//计时器时钟

		private Timer timer1;
		private Button btn_add;
		private Button btn_del;
		private ListView listHS;
		private ContextMenuStrip contextMenuStrip_RMenu;
		private ToolStripMenuItem 启用ToolStripMenuItem;
		private ToolStripMenuItem 更新ToolStripMenuItem;
		private ToolStripMenuItem 启动ToolStripMenuItem;
	}
}

