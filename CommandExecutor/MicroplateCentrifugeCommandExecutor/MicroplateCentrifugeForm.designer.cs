
namespace MicroplateCentrifugeCommandExecutor
{
    partial class MicroplateCentrifugeForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MicroplateCentrifugeForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.LisenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LoaderSettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axCentrifugeLoader = new AxCentrifugeLoaderLib.AxCentrifugeLoader();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axCentrifugeLoader)).BeginInit();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "MicroplateCentrifuge";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LisenceToolStripMenuItem,
            this.LoaderSettingToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(211, 104);
            // 
            // LisenceToolStripMenuItem
            // 
            this.LisenceToolStripMenuItem.Name = "LisenceToolStripMenuItem";
            this.LisenceToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.LisenceToolStripMenuItem.Text = "License";
            this.LisenceToolStripMenuItem.Click += new System.EventHandler(this.NoticeToolStripMenuItem_Click);
            // 
            // LoaderSettingToolStripMenuItem
            // 
            this.LoaderSettingToolStripMenuItem.Name = "LoaderSettingToolStripMenuItem";
            this.LoaderSettingToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.LoaderSettingToolStripMenuItem.Text = "Settings";
            this.LoaderSettingToolStripMenuItem.Click += new System.EventHandler(this.loderSettingToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.ExitToolStripMenuItem.Text = "Exit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.FinishToolStripMenuItem_Click);
            // 
            // axCentrifugeLoader
            // 
            this.axCentrifugeLoader.Enabled = true;
            this.axCentrifugeLoader.Location = new System.Drawing.Point(77, 11);
            this.axCentrifugeLoader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.axCentrifugeLoader.Name = "axCentrifugeLoader";
            this.axCentrifugeLoader.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axCentrifugeLoader.OcxState")));
            this.axCentrifugeLoader.Size = new System.Drawing.Size(77, 64);
            this.axCentrifugeLoader.TabIndex = 2;
            this.axCentrifugeLoader.Error += new AxCentrifugeLoaderLib._DCentrifugeLoaderEvents_ErrorEventHandler(this.axCentrifugeLoader_Error);
            // 
            // MicroplateCentrifugeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(200, 104);
            this.ControlBox = false;
            this.Controls.Add(this.axCentrifugeLoader);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MicroplateCentrifugeForm";
            this.ShowInTaskbar = false;
            this.Text = "MicrolateCetrifuge";
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axCentrifugeLoader)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem LisenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoaderSettingToolStripMenuItem;
        private AxCentrifugeLoaderLib.AxCentrifugeLoader axCentrifugeLoader;
    }
}

