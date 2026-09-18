namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class ReportsView
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlFilter = new System.Windows.Forms.Panel();
            this.tblFilterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.flowFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblType = new System.Windows.Forms.Label();
            this.cbReportType = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cbStatusFilter = new System.Windows.Forms.ComboBox();
            this.lblSrch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.flowActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrintPaySlip = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.gridReport = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.pnlHeader.SuspendLayout();
            this.pnlFilter.SuspendLayout();
            this.tblFilterLayout.SuspendLayout();
            this.flowFilters.SuspendLayout();
            this.flowActions.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridReport)).BeginInit();
            this.pnlSummary.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.AutoSize = true;
            this.pnlHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Name = "pnlHeader";

            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(320, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Reports & Analytics Center";

            this.pnlFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilter.AutoSize = true;
            this.pnlFilter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlFilter.BackColor = System.Drawing.Color.White;
            this.pnlFilter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlFilter.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.pnlFilter.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.pnlFilter.Controls.Add(this.tblFilterLayout);
            this.pnlFilter.Name = "pnlFilter";
            this.pnlFilter.TabIndex = 1;

            this.tblFilterLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFilterLayout.AutoSize = true;
            this.tblFilterLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblFilterLayout.ColumnCount = 1;
            this.tblFilterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblFilterLayout.RowCount = 2;
            this.tblFilterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFilterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFilterLayout.Controls.Add(this.flowFilters, 0, 0);
            this.tblFilterLayout.Controls.Add(this.flowActions, 0, 1);
            this.tblFilterLayout.Name = "tblFilterLayout";

            this.flowFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowFilters.AutoSize = true;
            this.flowFilters.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowFilters.WrapContents = true;
            this.flowFilters.Margin = new System.Windows.Forms.Padding(0);
            this.flowFilters.Name = "flowFilters";
            this.flowFilters.Controls.Add(this.lblType);
            this.flowFilters.Controls.Add(this.cbReportType);
            this.flowFilters.Controls.Add(this.lblStatus);
            this.flowFilters.Controls.Add(this.cbStatusFilter);
            this.flowFilters.Controls.Add(this.lblSrch);
            this.flowFilters.Controls.Add(this.txtSearch);

            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblType.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblType.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(89, 17);
            this.lblType.TabIndex = 0;
            this.lblType.Text = "Report Type:";

            this.cbReportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbReportType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbReportType.FormattingEnabled = true;
            this.cbReportType.Items.AddRange(new object[] {
            "Delivery Dispatches (All/Completed/Cancelled)",
            "Loading Schedules Manifest",
            "Plant & Store Trip Reports",
            "Fleet & Truck Status",
            "Crew & Driver Assignments",
            "Billing & Invoices Log",
            "Collection Receipts Summary",
            "Incident & Damage Reports",
            "Crew Trip Compensation & Pay Slips"});
            this.cbReportType.Margin = new System.Windows.Forms.Padding(0, 0, 16, 6);
            this.cbReportType.Name = "cbReportType";
            this.cbReportType.Size = new System.Drawing.Size(300, 23);
            this.cbReportType.TabIndex = 1;
            this.cbReportType.SelectedIndexChanged += new System.EventHandler(this.CbReportType_SelectedIndexChanged);

            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(51, 17);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Status:";

            this.cbStatusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStatusFilter.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbStatusFilter.FormattingEnabled = true;
            this.cbStatusFilter.Items.AddRange(new object[] {
            "All Statuses",
            "Completed Only",
            "Cancelled Only",
            "Pending / In Transit"});
            this.cbStatusFilter.Margin = new System.Windows.Forms.Padding(0, 0, 16, 6);
            this.cbStatusFilter.Name = "cbStatusFilter";
            this.cbStatusFilter.Size = new System.Drawing.Size(160, 23);
            this.cbStatusFilter.TabIndex = 3;
            this.cbStatusFilter.SelectedIndexChanged += new System.EventHandler(this.CbStatusFilter_SelectedIndexChanged);

            this.lblSrch.AutoSize = true;
            this.lblSrch.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSrch.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblSrch.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);
            this.lblSrch.Name = "lblSrch";
            this.lblSrch.Size = new System.Drawing.Size(54, 17);
            this.lblSrch.TabIndex = 4;
            this.lblSrch.Text = "Search:";

            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Type keywords...";
            this.txtSearch.Size = new System.Drawing.Size(160, 23);
            this.txtSearch.TabIndex = 5;
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);

            this.flowActions.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowActions.AutoSize = true;
            this.flowActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowActions.WrapContents = true;
            this.flowActions.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.flowActions.Name = "flowActions";
            this.flowActions.Controls.Add(this.btnPrintPaySlip);
            this.flowActions.Controls.Add(this.btnPrint);
            this.flowActions.Controls.Add(this.btnExport);

            this.btnPrintPaySlip.BackColor = System.Drawing.Color.FromArgb(30, 58, 138);
            this.btnPrintPaySlip.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrintPaySlip.FlatAppearance.BorderSize = 0;
            this.btnPrintPaySlip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrintPaySlip.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrintPaySlip.ForeColor = System.Drawing.Color.White;
            this.btnPrintPaySlip.Margin = new System.Windows.Forms.Padding(0, 0, 8, 4);
            this.btnPrintPaySlip.Name = "btnPrintPaySlip";
            this.btnPrintPaySlip.Size = new System.Drawing.Size(150, 32);
            this.btnPrintPaySlip.TabIndex = 8;
            this.btnPrintPaySlip.Text = "📄 Print Pay Slip";
            this.btnPrintPaySlip.UseVisualStyleBackColor = false;
            this.btnPrintPaySlip.Click += new System.EventHandler(this.BtnPrintPaySlip_Click);

            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnPrint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrint.FlatAppearance.BorderSize = 0;
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Margin = new System.Windows.Forms.Padding(0, 0, 8, 4);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(140, 32);
            this.btnPrint.TabIndex = 6;
            this.btnPrint.Text = "🖨️ Print Report";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.btnExport.BackColor = System.Drawing.Color.FromArgb(22, 101, 52);
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(140, 32);
            this.btnExport.TabIndex = 7;
            this.btnExport.Text = "📊 Export CSV";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(500, 260);
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(12);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0, 14, 0, 0);
            this.pnlGridCard.Controls.Add(this.gridReport);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.TabIndex = 2;

            this.gridReport.AllowUserToAddRows = false;
            this.gridReport.AllowUserToDeleteRows = false;
            this.gridReport.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridReport.BackgroundColor = System.Drawing.Color.White;
            this.gridReport.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridReport.ColumnHeadersHeight = 42;
            this.gridReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridReport.EnableHeadersVisualStyles = false;
            this.gridReport.Name = "gridReport";
            this.gridReport.ReadOnly = true;
            this.gridReport.RowHeadersVisible = false;
            this.gridReport.RowTemplate.Height = 36;
            this.gridReport.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridReport.TabIndex = 0;

            this.pnlSummary.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummary.Height = 40;
            this.pnlSummary.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlSummary.Controls.Add(this.lblSummary);
            this.pnlSummary.Name = "pnlSummary";
            this.pnlSummary.TabIndex = 1;

            this.lblSummary.AutoSize = true;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSummary.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSummary.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(185, 17);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = "Ready";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.Padding = new System.Windows.Forms.Padding(24, 16, 24, 20);
            this.Controls.Add(this.pnlGridCard);
            this.Controls.Add(this.pnlFilter);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.Name = "ReportsView";
            this.Size = new System.Drawing.Size(1420, 820);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlFilter.ResumeLayout(false);
            this.pnlFilter.PerformLayout();
            this.tblFilterLayout.ResumeLayout(false);
            this.tblFilterLayout.PerformLayout();
            this.flowFilters.ResumeLayout(false);
            this.flowFilters.PerformLayout();
            this.flowActions.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridReport)).EndInit();
            this.pnlSummary.ResumeLayout(false);
            this.pnlSummary.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.TableLayoutPanel tblFilterLayout;
        private System.Windows.Forms.FlowLayoutPanel flowFilters;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cbReportType;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cbStatusFilter;
        private System.Windows.Forms.Label lblSrch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.FlowLayoutPanel flowActions;
        private System.Windows.Forms.Button btnPrintPaySlip;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.DataGridView gridReport;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
