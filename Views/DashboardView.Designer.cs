namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class DashboardView
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
            pnlTopHeader = new Panel();
            tblTopHeader = new TableLayoutPanel();
            pnlTitleBlock = new Panel();
            lblSubtitle = new Label();
            lblTitle = new Label();
            flowFilterControls = new FlowLayoutPanel();
            lblSystemBadge = new Label();
            pnlKpiContainer = new Panel();
            pnlKpis = new TableLayoutPanel();
            pnlMainTableCard = new Panel();
            gridOperations = new DataGridView();
            pnlTableHeader = new Panel();
            tblTableToolbar = new TableLayoutPanel();
            flowTabs = new FlowLayoutPanel();
            btnTabDispatches = new Button();
            btnTabLoading = new Button();
            flowSearchPrint = new FlowLayoutPanel();
            txtQuickSearch = new TextBox();
            btnPrintTable = new Button();
            pnlTopHeader.SuspendLayout();
            tblTopHeader.SuspendLayout();
            pnlTitleBlock.SuspendLayout();
            pnlKpiContainer.SuspendLayout();
            pnlMainTableCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridOperations).BeginInit();
            pnlTableHeader.SuspendLayout();
            tblTableToolbar.SuspendLayout();
            flowTabs.SuspendLayout();
            flowSearchPrint.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTopHeader
            // 
            pnlTopHeader.AutoSize = true;
            pnlTopHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTopHeader.Controls.Add(tblTopHeader);
            pnlTopHeader.Dock = DockStyle.Top;
            pnlTopHeader.Location = new Point(0, 0);
            pnlTopHeader.Name = "pnlTopHeader";
            pnlTopHeader.Padding = new Padding(24, 14, 24, 8);
            pnlTopHeader.Size = new Size(1420, 86);
            pnlTopHeader.TabIndex = 3;
            // 
            // tblTopHeader
            // 
            tblTopHeader.AutoSize = true;
            tblTopHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tblTopHeader.ColumnCount = 2;
            tblTopHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tblTopHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tblTopHeader.Controls.Add(pnlTitleBlock, 0, 0);
            tblTopHeader.Controls.Add(flowFilterControls, 1, 0);
            tblTopHeader.Dock = DockStyle.Top;
            tblTopHeader.Location = new Point(24, 14);
            tblTopHeader.Name = "tblTopHeader";
            tblTopHeader.RowCount = 1;
            tblTopHeader.RowStyles.Add(new RowStyle());
            tblTopHeader.Size = new Size(1372, 64);
            tblTopHeader.TabIndex = 0;
            // 
            // pnlTitleBlock
            // 
            pnlTitleBlock.AutoSize = true;
            pnlTitleBlock.Controls.Add(lblSubtitle);
            pnlTitleBlock.Controls.Add(lblTitle);
            pnlTitleBlock.Dock = DockStyle.Fill;
            pnlTitleBlock.Location = new Point(3, 3);
            pnlTitleBlock.Name = "pnlTitleBlock";
            pnlTitleBlock.Size = new Size(611, 58);
            pnlTitleBlock.TabIndex = 0;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.Font = new Font("Segoe UI", 9.5F);
            lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitle.Location = new Point(0, 37);
            lblSubtitle.Margin = new Padding(0);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(549, 21);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Executive overview of fleet logistics, dispatch operations, and financial metrics.";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Margin = new Padding(0, 0, 0, 4);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(157, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Dashboard";
            // 
            // flowFilterControls
            // 
            flowFilterControls.AutoSize = true;
            flowFilterControls.Dock = DockStyle.Right;
            flowFilterControls.Location = new Point(1372, 0);
            flowFilterControls.Margin = new Padding(0);
            flowFilterControls.Name = "flowFilterControls";
            flowFilterControls.Size = new Size(0, 64);
            flowFilterControls.TabIndex = 2;
            // 
            // lblSystemBadge
            // 
            lblSystemBadge.BackColor = Color.FromArgb(240, 253, 244);
            lblSystemBadge.BorderStyle = BorderStyle.FixedSingle;
            lblSystemBadge.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblSystemBadge.ForeColor = Color.FromArgb(22, 101, 52);
            lblSystemBadge.Location = new Point(0, 0);
            lblSystemBadge.Margin = new Padding(8, 2, 0, 2);
            lblSystemBadge.Name = "lblSystemBadge";
            lblSystemBadge.Padding = new Padding(8, 4, 8, 4);
            lblSystemBadge.Size = new Size(200, 32);
            lblSystemBadge.TabIndex = 3;
            lblSystemBadge.Text = "● Database: Online (SQLite)";
            lblSystemBadge.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlKpiContainer
            // 
            pnlKpiContainer.Controls.Add(pnlKpis);
            pnlKpiContainer.Dock = DockStyle.Top;
            pnlKpiContainer.Location = new Point(0, 86);
            pnlKpiContainer.Name = "pnlKpiContainer";
            pnlKpiContainer.Padding = new Padding(24, 6, 24, 12);
            pnlKpiContainer.Size = new Size(1420, 145);
            pnlKpiContainer.TabIndex = 1;
            // 
            // pnlKpis
            // 
            pnlKpis.ColumnCount = 4;
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.Dock = DockStyle.Fill;
            pnlKpis.Location = new Point(24, 6);
            pnlKpis.Name = "pnlKpis";
            pnlKpis.RowCount = 1;
            pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlKpis.Size = new Size(1372, 127);
            pnlKpis.TabIndex = 0;
            // 
            // pnlMainTableCard
            // 
            pnlMainTableCard.BackColor = Color.White;
            pnlMainTableCard.Controls.Add(gridOperations);
            pnlMainTableCard.Controls.Add(pnlTableHeader);
            pnlMainTableCard.Dock = DockStyle.Fill;
            pnlMainTableCard.Location = new Point(0, 231);
            pnlMainTableCard.Name = "pnlMainTableCard";
            pnlMainTableCard.Padding = new Padding(24, 16, 24, 20);
            pnlMainTableCard.Size = new Size(1420, 589);
            pnlMainTableCard.TabIndex = 2;
            // 
            // gridOperations
            // 
            gridOperations.AllowUserToAddRows = false;
            gridOperations.AllowUserToDeleteRows = false;
            gridOperations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridOperations.BackgroundColor = Color.White;
            gridOperations.BorderStyle = BorderStyle.None;
            gridOperations.ColumnHeadersHeight = 44;
            gridOperations.Dock = DockStyle.Fill;
            gridOperations.EnableHeadersVisualStyles = false;
            gridOperations.Location = new Point(24, 106);
            gridOperations.Name = "gridOperations";
            gridOperations.ReadOnly = true;
            gridOperations.RowHeadersVisible = false;
            gridOperations.RowHeadersWidth = 51;
            gridOperations.RowTemplate.Height = 42;
            gridOperations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridOperations.Size = new Size(1372, 463);
            gridOperations.TabIndex = 4;
            // 
            // pnlTableHeader
            // 
            pnlTableHeader.AutoSize = true;
            pnlTableHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTableHeader.Controls.Add(tblTableToolbar);
            pnlTableHeader.Dock = DockStyle.Top;
            pnlTableHeader.Location = new Point(24, 16);
            pnlTableHeader.Name = "pnlTableHeader";
            pnlTableHeader.Padding = new Padding(0, 0, 0, 10);
            pnlTableHeader.Size = new Size(1372, 90);
            pnlTableHeader.TabIndex = 5;
            // 
            // tblTableToolbar
            // 
            tblTableToolbar.AutoSize = true;
            tblTableToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tblTableToolbar.ColumnCount = 2;
            tblTableToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblTableToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblTableToolbar.Controls.Add(flowTabs, 0, 0);
            tblTableToolbar.Controls.Add(flowSearchPrint, 1, 0);
            tblTableToolbar.Dock = DockStyle.Top;
            tblTableToolbar.Location = new Point(0, 0);
            tblTableToolbar.Name = "tblTableToolbar";
            tblTableToolbar.RowCount = 1;
            tblTableToolbar.RowStyles.Add(new RowStyle());
            tblTableToolbar.Size = new Size(1372, 40);
            tblTableToolbar.TabIndex = 0;
            // 
            // flowTabs
            // 
            flowTabs.AutoSize = true;
            flowTabs.Controls.Add(btnTabDispatches);
            flowTabs.Controls.Add(btnTabLoading);
            flowTabs.Dock = DockStyle.Fill;
            flowTabs.Location = new Point(0, 0);
            flowTabs.Margin = new Padding(0);
            flowTabs.Name = "flowTabs";
            flowTabs.Size = new Size(686, 40);
            flowTabs.TabIndex = 0;
            // 
            // btnTabDispatches
            // 
            btnTabDispatches.BackColor = Color.FromArgb(15, 23, 42);
            btnTabDispatches.Cursor = Cursors.Hand;
            btnTabDispatches.FlatAppearance.BorderSize = 0;
            btnTabDispatches.FlatStyle = FlatStyle.Flat;
            btnTabDispatches.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnTabDispatches.ForeColor = Color.White;
            btnTabDispatches.Location = new Point(0, 0);
            btnTabDispatches.Margin = new Padding(0, 0, 8, 4);
            btnTabDispatches.Name = "btnTabDispatches";
            btnTabDispatches.Size = new Size(220, 36);
            btnTabDispatches.TabIndex = 0;
            btnTabDispatches.Text = "🚚  Active Dispatches";
            btnTabDispatches.UseVisualStyleBackColor = false;
            // 
            // btnTabLoading
            // 
            btnTabLoading.BackColor = Color.FromArgb(241, 245, 249);
            btnTabLoading.Cursor = Cursors.Hand;
            btnTabLoading.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnTabLoading.FlatStyle = FlatStyle.Flat;
            btnTabLoading.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnTabLoading.ForeColor = Color.FromArgb(71, 85, 105);
            btnTabLoading.Location = new Point(228, 0);
            btnTabLoading.Margin = new Padding(0, 0, 8, 4);
            btnTabLoading.Name = "btnTabLoading";
            btnTabLoading.Size = new Size(220, 36);
            btnTabLoading.TabIndex = 1;
            btnTabLoading.Text = "📝  Daily Loading Orders";
            btnTabLoading.UseVisualStyleBackColor = false;
            // 
            // flowSearchPrint
            // 
            flowSearchPrint.AutoSize = true;
            flowSearchPrint.Controls.Add(txtQuickSearch);
            flowSearchPrint.Controls.Add(btnPrintTable);
            flowSearchPrint.Dock = DockStyle.Right;
            flowSearchPrint.Location = new Point(964, 0);
            flowSearchPrint.Margin = new Padding(0);
            flowSearchPrint.Name = "flowSearchPrint";
            flowSearchPrint.Size = new Size(408, 40);
            flowSearchPrint.TabIndex = 1;
            // 
            // txtQuickSearch
            // 
            txtQuickSearch.Font = new Font("Segoe UI", 9.5F);
            txtQuickSearch.Location = new Point(0, 3);
            txtQuickSearch.Margin = new Padding(0, 3, 8, 4);
            txtQuickSearch.Name = "txtQuickSearch";
            txtQuickSearch.PlaceholderText = "🔍 Quick filter live operations...";
            txtQuickSearch.Size = new Size(260, 29);
            txtQuickSearch.TabIndex = 2;
            // 
            // btnPrintTable
            // 
            btnPrintTable.BackColor = Color.FromArgb(248, 250, 252);
            btnPrintTable.Cursor = Cursors.Hand;
            btnPrintTable.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnPrintTable.FlatStyle = FlatStyle.Flat;
            btnPrintTable.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnPrintTable.ForeColor = Color.FromArgb(71, 85, 105);
            btnPrintTable.Location = new Point(268, 0);
            btnPrintTable.Margin = new Padding(0, 0, 0, 4);
            btnPrintTable.Name = "btnPrintTable";
            btnPrintTable.Size = new Size(140, 32);
            btnPrintTable.TabIndex = 3;
            btnPrintTable.Text = "🖨️ Print Report";
            btnPrintTable.UseVisualStyleBackColor = false;
            // 
            // DashboardView
            // 
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            BackColor = Color.FromArgb(248, 250, 252);
            Controls.Add(pnlMainTableCard);
            Controls.Add(pnlKpiContainer);
            Controls.Add(pnlTopHeader);
            Font = new Font("Segoe UI", 10F);
            Name = "DashboardView";
            Size = new Size(1420, 820);
            pnlTopHeader.ResumeLayout(false);
            pnlTopHeader.PerformLayout();
            tblTopHeader.ResumeLayout(false);
            tblTopHeader.PerformLayout();
            pnlTitleBlock.ResumeLayout(false);
            pnlTitleBlock.PerformLayout();
            pnlKpiContainer.ResumeLayout(false);
            pnlMainTableCard.ResumeLayout(false);
            pnlMainTableCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridOperations).EndInit();
            pnlTableHeader.ResumeLayout(false);
            pnlTableHeader.PerformLayout();
            tblTableToolbar.ResumeLayout(false);
            tblTableToolbar.PerformLayout();
            flowTabs.ResumeLayout(false);
            flowSearchPrint.ResumeLayout(false);
            flowSearchPrint.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlTopHeader;
        private System.Windows.Forms.TableLayoutPanel tblTopHeader;
        private System.Windows.Forms.Panel pnlTitleBlock;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.FlowLayoutPanel flowFilterControls;
        private System.Windows.Forms.Label lblSystemBadge;
        private System.Windows.Forms.Panel pnlKpiContainer;
        private System.Windows.Forms.TableLayoutPanel pnlKpis;
        private System.Windows.Forms.Panel pnlMainTableCard;
        private System.Windows.Forms.Panel pnlTableHeader;
        private System.Windows.Forms.TableLayoutPanel tblTableToolbar;
        private System.Windows.Forms.FlowLayoutPanel flowTabs;
        private System.Windows.Forms.Button btnTabDispatches;
        private System.Windows.Forms.Button btnTabLoading;
        private System.Windows.Forms.FlowLayoutPanel flowSearchPrint;
        private System.Windows.Forms.TextBox txtQuickSearch;
        private System.Windows.Forms.Button btnPrintTable;
        private System.Windows.Forms.DataGridView gridOperations;
    }
}
