namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class ResourcesHubView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTopNav = new System.Windows.Forms.Panel();
            this.flowNavTabs = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlContentHost = new System.Windows.Forms.Panel();
            this.pnlTopNav.SuspendLayout();
            this.SuspendLayout();

            this.pnlTopNav.BackColor = System.Drawing.Color.White;
            this.pnlTopNav.Controls.Add(this.flowNavTabs);
            this.pnlTopNav.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopNav.Location = new System.Drawing.Point(0, 0);
            this.pnlTopNav.Name = "pnlTopNav";
            this.pnlTopNav.Padding = new System.Windows.Forms.Padding(16, 10, 16, 8);
            this.pnlTopNav.Size = new System.Drawing.Size(1200, 56);
            this.pnlTopNav.TabIndex = 0;

            this.flowNavTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowNavTabs.Location = new System.Drawing.Point(16, 10);
            this.flowNavTabs.Name = "flowNavTabs";
            this.flowNavTabs.Size = new System.Drawing.Size(1168, 38);
            this.flowNavTabs.TabIndex = 0;

            this.pnlContentHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlContentHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContentHost.Location = new System.Drawing.Point(0, 56);
            this.pnlContentHost.Name = "pnlContentHost";
            this.pnlContentHost.Size = new System.Drawing.Size(1200, 694);
            this.pnlContentHost.TabIndex = 1;

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlContentHost);
            this.Controls.Add(this.pnlTopNav);
            this.Name = "ResourcesHubView";
            this.Size = new System.Drawing.Size(1200, 750);
            this.pnlTopNav.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlTopNav;
        private System.Windows.Forms.FlowLayoutPanel flowNavTabs;
        private System.Windows.Forms.Panel pnlContentHost;
    }
}
