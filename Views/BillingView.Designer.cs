namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class BillingView
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
            this.tblMasterContent = new System.Windows.Forms.TableLayoutPanel();
            this.pnlForm = new System.Windows.Forms.Panel();
            this.lblFormHeader = new System.Windows.Forms.Label();
            this.lblDel = new System.Windows.Forms.Label();
            this.cbDelivery = new System.Windows.Forms.ComboBox();
            this.lblLoadingSchedule = new System.Windows.Forms.Label();
            this.txtLoadingSchedule = new System.Windows.Forms.TextBox();
            this.lblClient = new System.Windows.Forms.Label();
            this.cbClient = new System.Windows.Forms.ComboBox();
            this.tblAmounts = new System.Windows.Forms.TableLayoutPanel();
            this.lblGross = new System.Windows.Forms.Label();
            this.lblDed = new System.Windows.Forms.Label();
            this.numTotalAmount = new System.Windows.Forms.NumericUpDown();
            this.numDeductions = new System.Windows.Forms.NumericUpDown();
            this.pnlNetDue = new System.Windows.Forms.Panel();
            this.lblNetAmount = new System.Windows.Forms.Label();
            this.lblDueDate = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.tblUpdateDelete = new System.Windows.Forms.TableLayoutPanel();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlGridHeaderBar = new System.Windows.Forms.Panel();
            this.lblGridHeader = new System.Windows.Forms.Label();
            this.pnlGridActionsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.txtGridSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.btnGenerateSoa = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.gridInvoices = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lblTariffMatch = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.tblAmounts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTotalAmount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDeductions)).BeginInit();
            this.pnlNetDue.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridInvoices)).BeginInit();
            this.pnlSummary.SuspendLayout();
            this.SuspendLayout();

            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1000, 620);
            this.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(1420, 820);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new System.Drawing.Point(16, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.lblTitle.Size = new System.Drawing.Size(430, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Billing & Service Invoicing (15-Day Window)";

            this.tblMasterContent.ColumnCount = 2;
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 580F));
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Controls.Add(this.pnlForm, 0, 0);
            this.tblMasterContent.Controls.Add(this.pnlGridCard, 1, 0);
            this.tblMasterContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMasterContent.Location = new System.Drawing.Point(16, 56);
            this.tblMasterContent.Name = "tblMasterContent";
            this.tblMasterContent.RowCount = 1;
            this.tblMasterContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Size = new System.Drawing.Size(1388, 748);
            this.tblMasterContent.TabIndex = 1;

            this.pnlForm.AutoScroll = true;
            this.pnlForm.BackColor = System.Drawing.Color.White;
            this.pnlForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlForm.Location = new System.Drawing.Point(0, 0);
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(16);
            this.pnlForm.Size = new System.Drawing.Size(468, 748);
            this.pnlForm.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblFormHeader.Location = new System.Drawing.Point(16, 16);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblFormHeader.Size = new System.Drawing.Size(175, 28);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Generate Service Invoice";

            this.lblDel.AutoSize = true;
            this.lblDel.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDel.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDel.Location = new System.Drawing.Point(16, 44);
            this.lblDel.Name = "lblDel";
            this.lblDel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblDel.Size = new System.Drawing.Size(167, 23);
            this.lblDel.TabIndex = 1;
            this.lblDel.Text = "Completed Delivery Trip *:";

            this.cbDelivery.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbDelivery.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelivery.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDelivery.FormattingEnabled = true;
            this.cbDelivery.Location = new System.Drawing.Point(16, 67);
            this.cbDelivery.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbDelivery.Name = "cbDelivery";
            this.cbDelivery.Size = new System.Drawing.Size(436, 25);
            this.cbDelivery.TabIndex = 2;
            this.cbDelivery.SelectedIndexChanged += new System.EventHandler(this.CbDelivery_SelectedIndexChanged);

            this.lblLoadingSchedule.AutoSize = true;
            this.lblLoadingSchedule.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLoadingSchedule.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblLoadingSchedule.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblLoadingSchedule.Location = new System.Drawing.Point(16, 100);
            this.lblLoadingSchedule.Name = "lblLoadingSchedule";
            this.lblLoadingSchedule.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblLoadingSchedule.Size = new System.Drawing.Size(220, 23);
            this.lblLoadingSchedule.TabIndex = 3;
            this.lblLoadingSchedule.Text = "Linked Loading Schedule / Volume:";

            this.txtLoadingSchedule.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtLoadingSchedule.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtLoadingSchedule.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.txtLoadingSchedule.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.txtLoadingSchedule.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLoadingSchedule.ReadOnly = true;
            this.txtLoadingSchedule.Location = new System.Drawing.Point(16, 123);
            this.txtLoadingSchedule.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.txtLoadingSchedule.Name = "txtLoadingSchedule";
            this.txtLoadingSchedule.PlaceholderText = "Linked loading schedule details...";
            this.txtLoadingSchedule.Size = new System.Drawing.Size(436, 25);
            this.txtLoadingSchedule.TabIndex = 4;

            this.lblClient.AutoSize = true;
            this.lblClient.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblClient.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblClient.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblClient.Location = new System.Drawing.Point(16, 156);
            this.lblClient.Name = "lblClient";
            this.lblClient.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblClient.Size = new System.Drawing.Size(183, 23);
            this.lblClient.TabIndex = 5;
            this.lblClient.Text = "Billed Client / Client Name *:";

            this.cbClient.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbClient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cbClient.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbClient.FormattingEnabled = true;
            this.cbClient.Items.AddRange(new object[] {
            "Goldilocks Bakeshop",
            "Red Ribbon Bakeshop",
            "Millenium FoodBite Corp.",
            "BCS Group of Companies",
            "Bestbake Inc.",
            "Richbake Inc."});
            this.cbClient.Location = new System.Drawing.Point(16, 123);
            this.cbClient.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbClient.Name = "cbClient";
            this.cbClient.Size = new System.Drawing.Size(436, 25);
            this.cbClient.TabIndex = 4;

            this.tblAmounts.ColumnCount = 2;
            this.tblAmounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAmounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAmounts.Controls.Add(this.lblGross, 0, 0);
            this.tblAmounts.Controls.Add(this.lblDed, 1, 0);
            this.tblAmounts.Controls.Add(this.numTotalAmount, 0, 1);
            this.tblAmounts.Controls.Add(this.numDeductions, 1, 1);
            this.tblAmounts.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblAmounts.Location = new System.Drawing.Point(16, 156);
            this.tblAmounts.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.tblAmounts.Name = "tblAmounts";
            this.tblAmounts.RowCount = 2;
            this.tblAmounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblAmounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblAmounts.Size = new System.Drawing.Size(436, 56);
            this.tblAmounts.TabIndex = 5;

            this.lblGross.AutoSize = true;
            this.lblGross.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGross.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblGross.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGross.Location = new System.Drawing.Point(0, 0);
            this.lblGross.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblGross.Name = "lblGross";
            this.lblGross.Size = new System.Drawing.Size(214, 17);
            this.lblGross.TabIndex = 0;
            this.lblGross.Text = "Standard Gross (₱) *:";

            this.lblDed.AutoSize = true;
            this.lblDed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDed.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDed.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDed.Location = new System.Drawing.Point(222, 0);
            this.lblDed.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblDed.Name = "lblDed";
            this.lblDed.Size = new System.Drawing.Size(214, 17);
            this.lblDed.TabIndex = 1;
            this.lblDed.Text = "Deductions / Damage (₱):";

            this.numTotalAmount.DecimalPlaces = 2;
            this.numTotalAmount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numTotalAmount.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numTotalAmount.Location = new System.Drawing.Point(0, 21);
            this.numTotalAmount.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.numTotalAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numTotalAmount.Name = "numTotalAmount";
            this.numTotalAmount.Size = new System.Drawing.Size(214, 24);
            this.numTotalAmount.TabIndex = 2;
            this.numTotalAmount.ThousandsSeparator = true;
            this.numTotalAmount.Value = new decimal(new int[] { 8500, 0, 0, 0 });
            this.numTotalAmount.ValueChanged += new System.EventHandler(this.RecalculateNet);

            this.numDeductions.DecimalPlaces = 2;
            this.numDeductions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numDeductions.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numDeductions.Location = new System.Drawing.Point(222, 21);
            this.numDeductions.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.numDeductions.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numDeductions.Name = "numDeductions";
            this.numDeductions.Size = new System.Drawing.Size(214, 24);
            this.numDeductions.TabIndex = 3;
            this.numDeductions.ThousandsSeparator = true;
            this.numDeductions.ValueChanged += new System.EventHandler(this.RecalculateNet);

            this.pnlNetDue.BackColor = System.Drawing.Color.FromArgb(238, 242, 255);
            this.pnlNetDue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlNetDue.Controls.Add(this.lblDueDate);
            this.pnlNetDue.Controls.Add(this.lblNetAmount);
            this.pnlNetDue.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlNetDue.Location = new System.Drawing.Point(16, 218);
            this.pnlNetDue.Margin = new System.Windows.Forms.Padding(0, 4, 0, 8);
            this.pnlNetDue.Name = "pnlNetDue";
            this.pnlNetDue.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.pnlNetDue.Size = new System.Drawing.Size(436, 56);
            this.pnlNetDue.TabIndex = 6;

            this.lblNetAmount.AutoSize = true;
            this.lblNetAmount.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNetAmount.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblNetAmount.ForeColor = System.Drawing.Color.FromArgb(30, 64, 175);
            this.lblNetAmount.Location = new System.Drawing.Point(10, 6);
            this.lblNetAmount.Name = "lblNetAmount";
            this.lblNetAmount.Size = new System.Drawing.Size(216, 19);
            this.lblNetAmount.TabIndex = 0;
            this.lblNetAmount.Text = "Net Due Amount: PHP 8,500.00";

            this.lblDueDate.AutoSize = true;
            this.lblDueDate.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDueDate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblDueDate.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDueDate.Location = new System.Drawing.Point(10, 25);
            this.lblDueDate.Name = "lblDueDate";
            this.lblDueDate.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.lblDueDate.Size = new System.Drawing.Size(232, 17);
            this.lblDueDate.TabIndex = 1;
            this.lblDueDate.Text = "Payment Due Date (15 Days): 08/25/2026";

            this.btnSave.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 282);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(436, 40);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "📄 ISSUE INVOICE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 330);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(436, 40);
            this.tblUpdateDelete.TabIndex = 8;

            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.FlatAppearance.BorderSize = 0;
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.ForeColor = System.Drawing.Color.White;
            this.btnUpdate.Location = new System.Drawing.Point(0, 0);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(214, 40);
            this.btnUpdate.TabIndex = 0;
            this.btnUpdate.Text = "✏️ UPDATE";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.FlatAppearance.BorderSize = 0;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.White;
            this.btnDelete.Location = new System.Drawing.Point(222, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(214, 40);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "🗑️ DELETE";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnClear.Location = new System.Drawing.Point(16, 376);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(436, 36);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "🧹 CLEAR FORM";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.lblTariffMatch.AutoSize = true;
            this.lblTariffMatch.Location = new System.Drawing.Point(0, 0);
            this.lblTariffMatch.Name = "lblTariffMatch";
            this.lblTariffMatch.Size = new System.Drawing.Size(0, 15);
            this.lblTariffMatch.TabIndex = 15;
            this.lblTariffMatch.Visible = false;

            this.pnlForm.Controls.Add(this.lblTariffMatch);
            this.pnlForm.Controls.Add(this.btnClear);
            this.pnlForm.Controls.Add(this.tblUpdateDelete);
            this.pnlForm.Controls.Add(this.btnSave);
            this.pnlForm.Controls.Add(this.pnlNetDue);
            this.pnlForm.Controls.Add(this.tblAmounts);
            this.pnlForm.Controls.Add(this.cbClient);
            this.pnlForm.Controls.Add(this.lblClient);
            this.pnlForm.Controls.Add(this.txtLoadingSchedule);
            this.pnlForm.Controls.Add(this.lblLoadingSchedule);
            this.pnlForm.Controls.Add(this.cbDelivery);
            this.pnlForm.Controls.Add(this.lblDel);
            this.pnlForm.Controls.Add(this.lblFormHeader);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridInvoices);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(580, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(908, 748);
            this.pnlGridCard.TabIndex = 1;

            this.pnlGridHeaderBar.AutoSize = true;
            this.pnlGridHeaderBar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlGridHeaderBar.Controls.Add(this.pnlGridActionsFlow);
            this.pnlGridHeaderBar.Controls.Add(this.lblGridHeader);
            this.pnlGridHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridHeaderBar.Location = new System.Drawing.Point(16, 16);
            this.pnlGridHeaderBar.Name = "pnlGridHeaderBar";
            this.pnlGridHeaderBar.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.pnlGridHeaderBar.Size = new System.Drawing.Size(876, 72);
            this.pnlGridHeaderBar.TabIndex = 0;

            this.lblGridHeader.AutoSize = true;
            this.lblGridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGridHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblGridHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGridHeader.Location = new System.Drawing.Point(0, 0);
            this.lblGridHeader.Name = "lblGridHeader";
            this.lblGridHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblGridHeader.Size = new System.Drawing.Size(227, 28);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Issued Service Invoices Register";

            this.pnlGridActionsFlow.AutoSize = true;
            this.pnlGridActionsFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlGridActionsFlow.Controls.Add(this.txtGridSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnClearSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnGenerateSoa);
            this.pnlGridActionsFlow.Controls.Add(this.btnPrint);
            this.pnlGridActionsFlow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridActionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlGridActionsFlow.Location = new System.Drawing.Point(0, 28);
            this.pnlGridActionsFlow.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            this.pnlGridActionsFlow.Size = new System.Drawing.Size(876, 36);
            this.pnlGridActionsFlow.TabIndex = 1;
            this.pnlGridActionsFlow.WrapContents = true;

            this.txtGridSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGridSearch.Location = new System.Drawing.Point(0, 4);
            this.txtGridSearch.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.txtGridSearch.Name = "txtGridSearch";
            this.txtGridSearch.PlaceholderText = "🔍 Search SOA, invoice, client...";
            this.txtGridSearch.Size = new System.Drawing.Size(190, 24);
            this.txtGridSearch.TabIndex = 0;
            this.txtGridSearch.TextChanged += new System.EventHandler(this.TxtGridSearch_TextChanged);

            this.btnClearSearch.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClearSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearSearch.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnClearSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearSearch.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnClearSearch.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.btnClearSearch.Location = new System.Drawing.Point(198, 4);
            this.btnClearSearch.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);
            this.btnClearSearch.Name = "btnClearSearch";
            this.btnClearSearch.Size = new System.Drawing.Size(28, 26);
            this.btnClearSearch.TabIndex = 1;
            this.btnClearSearch.Text = "✖";
            this.btnClearSearch.UseVisualStyleBackColor = false;
            this.btnClearSearch.Click += new System.EventHandler(this.BtnClearSearch_Click);

            this.btnGenerateSoa.BackColor = System.Drawing.Color.FromArgb(153, 27, 27);
            this.btnGenerateSoa.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGenerateSoa.FlatAppearance.BorderSize = 0;
            this.btnGenerateSoa.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerateSoa.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnGenerateSoa.ForeColor = System.Drawing.Color.White;
            this.btnGenerateSoa.Location = new System.Drawing.Point(238, 4);
            this.btnGenerateSoa.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnGenerateSoa.Name = "btnGenerateSoa";
            this.btnGenerateSoa.Size = new System.Drawing.Size(150, 28);
            this.btnGenerateSoa.TabIndex = 2;
            this.btnGenerateSoa.Text = "📄 GENERATE SOA";
            this.btnGenerateSoa.UseVisualStyleBackColor = false;
            this.btnGenerateSoa.Click += new System.EventHandler(this.BtnGenerateSoa_Click);

            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnPrint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrint.FlatAppearance.BorderSize = 0;
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Location = new System.Drawing.Point(396, 4);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(4, 4, 0, 4);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(130, 28);
            this.btnPrint.TabIndex = 3;
            this.btnPrint.Text = "🖨️ Print Report";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridInvoices.AllowUserToAddRows = false;
            this.gridInvoices.AllowUserToDeleteRows = false;
            this.gridInvoices.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridInvoices.BackgroundColor = System.Drawing.Color.White;
            this.gridInvoices.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridInvoices.ColumnHeadersHeight = 36;
            this.gridInvoices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridInvoices.EnableHeadersVisualStyles = false;
            this.gridInvoices.Location = new System.Drawing.Point(16, 60);
            this.gridInvoices.MultiSelect = false;
            this.gridInvoices.Name = "gridInvoices";
            this.gridInvoices.ReadOnly = true;
            this.gridInvoices.RowHeadersVisible = false;
            this.gridInvoices.RowTemplate.Height = 34;
            this.gridInvoices.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridInvoices.Size = new System.Drawing.Size(876, 626);
            this.gridInvoices.TabIndex = 1;
            this.gridInvoices.SelectionChanged += new System.EventHandler(this.GridInvoices_SelectionChanged);

            this.pnlSummary.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlSummary.Controls.Add(this.lblSummary);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummary.Location = new System.Drawing.Point(16, 694);
            this.pnlSummary.Name = "pnlSummary";
            this.pnlSummary.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlSummary.Size = new System.Drawing.Size(876, 38);
            this.pnlSummary.TabIndex = 2;

            this.lblSummary.AutoSize = true;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSummary.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSummary.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.lblSummary.Location = new System.Drawing.Point(12, 8);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(46, 17);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = "Ready";

            this.Controls.Add(this.tblMasterContent);
            this.Controls.Add(this.lblTitle);
            this.Name = "BillingView";
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.tblAmounts.ResumeLayout(false);
            this.tblAmounts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTotalAmount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDeductions)).EndInit();
            this.pnlNetDue.ResumeLayout(false);
            this.pnlNetDue.PerformLayout();
            this.tblUpdateDelete.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridCard.PerformLayout();
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridInvoices)).EndInit();
            this.pnlSummary.ResumeLayout(false);
            this.pnlSummary.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel tblMasterContent;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.Label lblDel;
        private System.Windows.Forms.ComboBox cbDelivery;
        private System.Windows.Forms.Label lblLoadingSchedule;
        private System.Windows.Forms.TextBox txtLoadingSchedule;
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.ComboBox cbClient;
        private System.Windows.Forms.TableLayoutPanel tblAmounts;
        private System.Windows.Forms.Label lblGross;
        private System.Windows.Forms.Label lblDed;
        private System.Windows.Forms.NumericUpDown numTotalAmount;
        private System.Windows.Forms.NumericUpDown numDeductions;
        private System.Windows.Forms.Panel pnlNetDue;
        private System.Windows.Forms.Label lblNetAmount;
        private System.Windows.Forms.Label lblDueDate;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblTariffMatch;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeaderBar;
        private System.Windows.Forms.Label lblGridHeader;
        private System.Windows.Forms.FlowLayoutPanel pnlGridActionsFlow;
        private System.Windows.Forms.TextBox txtGridSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Button btnGenerateSoa;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.DataGridView gridInvoices;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
