namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class BackupRestoreView
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.tblCards = new System.Windows.Forms.TableLayoutPanel();
            this.pnlBackup = new System.Windows.Forms.Panel();
            this.lblBHeader = new System.Windows.Forms.Label();
            this.lblBDesc = new System.Windows.Forms.Label();
            this.btnBackup = new System.Windows.Forms.Button();
            this.pnlRestore = new System.Windows.Forms.Panel();
            this.lblRHeader = new System.Windows.Forms.Label();
            this.lblRDesc = new System.Windows.Forms.Label();
            this.btnRestore = new System.Windows.Forms.Button();
            this.pnlStatus = new System.Windows.Forms.Panel();
            this.lblSHeader = new System.Windows.Forms.Label();
            this.lblDbStatus = new System.Windows.Forms.Label();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlBackup.SuspendLayout();
            this.pnlRestore.SuspendLayout();
            this.pnlStatus.SuspendLayout();
            this.tblCards.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.AutoSize = true;
            this.pnlHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.pnlHeader.Controls.Add(this.lblTitle);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(395, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Database Manual Backup & System Restore";

            this.tblCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCards.AutoSize = true;
            this.tblCards.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblCards.ColumnCount = 2;
            this.tblCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCards.RowCount = 1;
            this.tblCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCards.Controls.Add(this.pnlBackup, 0, 0);
            this.tblCards.Controls.Add(this.pnlRestore, 1, 0);
            this.tblCards.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.tblCards.Name = "tblCards";

            this.pnlBackup.BackColor = System.Drawing.Color.White;
            this.pnlBackup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBackup.MinimumSize = new System.Drawing.Size(320, 220);
            this.pnlBackup.Padding = new System.Windows.Forms.Padding(20);
            this.pnlBackup.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pnlBackup.Name = "pnlBackup";
            this.pnlBackup.Controls.Add(this.btnBackup);
            this.pnlBackup.Controls.Add(this.lblBDesc);
            this.pnlBackup.Controls.Add(this.lblBHeader);

            this.lblBHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBHeader.AutoSize = true;
            this.lblBHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblBHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblBHeader.Name = "lblBHeader";
            this.lblBHeader.Size = new System.Drawing.Size(222, 21);
            this.lblBHeader.TabIndex = 0;
            this.lblBHeader.Text = "Create Local Database Backup";

            this.lblBDesc.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBDesc.AutoSize = true;
            this.lblBDesc.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblBDesc.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblBDesc.Padding = new System.Windows.Forms.Padding(0, 10, 0, 20);
            this.lblBDesc.Name = "lblBDesc";
            this.lblBDesc.Size = new System.Drawing.Size(420, 50);
            this.lblBDesc.TabIndex = 1;
            this.lblBDesc.Text = "Export a standalone copy (.db file) of your complete system records, loading schedules, trucks, crew, billing, and receipts.";

            this.btnBackup.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnBackup.Height = 44;
            this.btnBackup.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnBackup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBackup.FlatAppearance.BorderSize = 0;
            this.btnBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackup.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnBackup.ForeColor = System.Drawing.Color.White;
            this.btnBackup.Name = "btnBackup";
            this.btnBackup.TabIndex = 2;
            this.btnBackup.Text = "💾  CREATE BACKUP SNAPSHOT";
            this.btnBackup.UseVisualStyleBackColor = false;
            this.btnBackup.Click += new System.EventHandler(this.BtnBackup_Click);

            this.pnlRestore.BackColor = System.Drawing.Color.White;
            this.pnlRestore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlRestore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRestore.MinimumSize = new System.Drawing.Size(320, 220);
            this.pnlRestore.Padding = new System.Windows.Forms.Padding(20);
            this.pnlRestore.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.pnlRestore.Name = "pnlRestore";
            this.pnlRestore.Controls.Add(this.btnRestore);
            this.pnlRestore.Controls.Add(this.lblRDesc);
            this.pnlRestore.Controls.Add(this.lblRHeader);

            this.lblRHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRHeader.AutoSize = true;
            this.lblRHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblRHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblRHeader.Name = "lblRHeader";
            this.lblRHeader.Size = new System.Drawing.Size(189, 21);
            this.lblRHeader.TabIndex = 0;
            this.lblRHeader.Text = "Restore System Database";

            this.lblRDesc.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRDesc.AutoSize = true;
            this.lblRDesc.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblRDesc.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblRDesc.Padding = new System.Windows.Forms.Padding(0, 10, 0, 20);
            this.lblRDesc.Name = "lblRDesc";
            this.lblRDesc.Size = new System.Drawing.Size(420, 50);
            this.lblRDesc.TabIndex = 1;
            this.lblRDesc.Text = "Restore system records from a previously saved SQLite backup snapshot file. Warning: This will overwrite current records.";

            this.btnRestore.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnRestore.Height = 44;
            this.btnRestore.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnRestore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestore.FlatAppearance.BorderSize = 0;
            this.btnRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnRestore.ForeColor = System.Drawing.Color.White;
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.TabIndex = 2;
            this.btnRestore.Text = "🔄  RESTORE FROM BACKUP FILE";
            this.btnRestore.UseVisualStyleBackColor = false;
            this.btnRestore.Click += new System.EventHandler(this.BtnRestore_Click);

            this.pnlStatus.BackColor = System.Drawing.Color.White;
            this.pnlStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatus.AutoSize = true;
            this.pnlStatus.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlStatus.Padding = new System.Windows.Forms.Padding(20);
            this.pnlStatus.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.pnlStatus.Name = "pnlStatus";
            this.pnlStatus.Controls.Add(this.lblDbStatus);
            this.pnlStatus.Controls.Add(this.lblSHeader);

            this.lblSHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSHeader.AutoSize = true;
            this.lblSHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblSHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblSHeader.Name = "lblSHeader";
            this.lblSHeader.Size = new System.Drawing.Size(209, 20);
            this.lblSHeader.TabIndex = 0;
            this.lblSHeader.Text = "Active Database Information";

            this.lblDbStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDbStatus.AutoSize = true;
            this.lblDbStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDbStatus.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDbStatus.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblDbStatus.Name = "lblDbStatus";
            this.lblDbStatus.Size = new System.Drawing.Size(126, 15);
            this.lblDbStatus.TabIndex = 1;
            this.lblDbStatus.Text = "Database Path: ...";

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.Padding = new System.Windows.Forms.Padding(24);
            this.Controls.Add(this.pnlStatus);
            this.Controls.Add(this.tblCards);
            this.Controls.Add(this.pnlHeader);
            this.Name = "BackupRestoreView";
            this.Size = new System.Drawing.Size(1040, 660);
            this.pnlBackup.ResumeLayout(false);
            this.pnlBackup.PerformLayout();
            this.pnlRestore.ResumeLayout(false);
            this.pnlRestore.PerformLayout();
            this.pnlStatus.ResumeLayout(false);
            this.pnlStatus.PerformLayout();
            this.tblCards.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.TableLayoutPanel tblCards;
        private System.Windows.Forms.Panel pnlBackup;
        private System.Windows.Forms.Label lblBHeader;
        private System.Windows.Forms.Label lblBDesc;
        private System.Windows.Forms.Button btnBackup;
        private System.Windows.Forms.Panel pnlRestore;
        private System.Windows.Forms.Label lblRHeader;
        private System.Windows.Forms.Label lblRDesc;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Panel pnlStatus;
        private System.Windows.Forms.Label lblSHeader;
        private System.Windows.Forms.Label lblDbStatus;
    }
}
