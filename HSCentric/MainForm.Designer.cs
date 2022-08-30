﻿
using System;
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
			this.启动ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.备份插件设置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label_currenttime = new System.Windows.Forms.Label();
			this.label_checktime = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.btn_selectpath = new System.Windows.Forms.Button();
			this.textbox_Path = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.contextMenuStrip_RMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// btn_add
			// 
			this.btn_add.Location = new System.Drawing.Point(522, 274);
			this.btn_add.Name = "btn_add";
			this.btn_add.Size = new System.Drawing.Size(75, 23);
			this.btn_add.TabIndex = 8;
			this.btn_add.Text = "添加";
			this.btn_add.UseVisualStyleBackColor = true;
			this.btn_add.Click += new System.EventHandler(this.btn_add_Click);
			// 
			// btn_del
			// 
			this.btn_del.Location = new System.Drawing.Point(603, 274);
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
			this.listHS.Location = new System.Drawing.Point(12, 35);
			this.listHS.MultiSelect = false;
			this.listHS.Name = "listHS";
			this.listHS.Size = new System.Drawing.Size(666, 233);
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
            this.启动ToolStripMenuItem,
            this.备份插件设置ToolStripMenuItem});
			this.contextMenuStrip_RMenu.Name = "contextMenuStrip1";
			this.contextMenuStrip_RMenu.Size = new System.Drawing.Size(149, 70);
			// 
			// 启用ToolStripMenuItem
			// 
			this.启用ToolStripMenuItem.Name = "启用ToolStripMenuItem";
			this.启用ToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.启用ToolStripMenuItem.Text = "启用";
			this.启用ToolStripMenuItem.Click += new System.EventHandler(this.启用ToolStripMenuItem_Click);
			// 
			// 启动ToolStripMenuItem
			// 
			this.启动ToolStripMenuItem.Name = "启动ToolStripMenuItem";
			this.启动ToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.启动ToolStripMenuItem.Text = "启动一次";
			this.启动ToolStripMenuItem.Click += new System.EventHandler(this.启动ToolStripMenuItem_Click);
			// 
			// 备份插件设置ToolStripMenuItem
			// 
			this.备份插件设置ToolStripMenuItem.Name = "备份插件设置ToolStripMenuItem";
			this.备份插件设置ToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.备份插件设置ToolStripMenuItem.Text = "备份插件设置";
			this.备份插件设置ToolStripMenuItem.Click += new System.EventHandler(this.备份插件设置ToolStripMenuItem_Click);
			// 
			// label_currenttime
			// 
			this.label_currenttime.AutoSize = true;
			this.label_currenttime.Location = new System.Drawing.Point(83, 279);
			this.label_currenttime.Name = "label_currenttime";
			this.label_currenttime.Size = new System.Drawing.Size(29, 12);
			this.label_currenttime.TabIndex = 11;
			this.label_currenttime.Text = "----";
			// 
			// label_checktime
			// 
			this.label_checktime.AutoSize = true;
			this.label_checktime.Location = new System.Drawing.Point(303, 279);
			this.label_checktime.Name = "label_checktime";
			this.label_checktime.Size = new System.Drawing.Size(29, 12);
			this.label_checktime.TabIndex = 12;
			this.label_checktime.Text = "----";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 279);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(65, 12);
			this.label1.TabIndex = 13;
			this.label1.Text = "当前时间：";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(208, 279);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(89, 12);
			this.label2.TabIndex = 14;
			this.label2.Text = "下次检测时间：";
			// 
			// btn_selectpath
			// 
			this.btn_selectpath.Location = new System.Drawing.Point(363, 6);
			this.btn_selectpath.Name = "btn_selectpath";
			this.btn_selectpath.Size = new System.Drawing.Size(40, 23);
			this.btn_selectpath.TabIndex = 17;
			this.btn_selectpath.Text = "...";
			this.btn_selectpath.UseVisualStyleBackColor = true;
			this.btn_selectpath.Click += new System.EventHandler(this.btn_selectpath_Click);
			// 
			// textbox_Path
			// 
			this.textbox_Path.Location = new System.Drawing.Point(70, 6);
			this.textbox_Path.Name = "textbox_Path";
			this.textbox_Path.Size = new System.Drawing.Size(287, 21);
			this.textbox_Path.TabIndex = 16;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(11, 11);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 12);
			this.label3.TabIndex = 15;
			this.label3.Text = "炉石路径";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(690, 309);
			this.Controls.Add(this.btn_selectpath);
			this.Controls.Add(this.textbox_Path);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label_checktime);
			this.Controls.Add(this.label_currenttime);
			this.Controls.Add(this.listHS);
			this.Controls.Add(this.btn_del);
			this.Controls.Add(this.btn_add);
			this.Name = "MainForm";
			this.Text = "大话家来帮你监控 5.0.2";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
			this.contextMenuStrip_RMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
		private DateTime m_CheckTime_runinfo = new DateTime(2000,1,1,0,0,0);
		private DateTime m_CheckTime_readlog = new DateTime(2000, 1, 1, 0, 0, 0);

		private Timer timer1;
		private Button btn_add;
		private Button btn_del;
		private ListView listHS;
		private ContextMenuStrip contextMenuStrip_RMenu;
		private ToolStripMenuItem 启用ToolStripMenuItem;
		private ToolStripMenuItem 启动ToolStripMenuItem;
		private ToolStripMenuItem 备份插件设置ToolStripMenuItem;
		private Label label_currenttime;
		private Label label_checktime;
		private Label label1;
		private Label label2;
		private Button btn_selectpath;
		private TextBox textbox_Path;
		private Label label3;
	}
}

