namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class RateMatrixView
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
            this.tblMasterContent = new System.Windows.Forms.TableLayoutPanel();
            this.pnlForm = new System.Windows.Forms.Panel();
            this.pnlFormActionsBar = new System.Windows.Forms.Panel();
            this.pnlSaveSpacer = new System.Windows.Forms.Panel();
            this.tblUpdateDelete = new System.Windows.Forms.TableLayoutPanel();
            this.pnlUpdateSpacer = new System.Windows.Forms.Panel();
            this.pnlFormBody = new System.Windows.Forms.Panel();
            this.pnlFormHeaderBar = new System.Windows.Forms.Panel();
            this.lblFormHeader = new System.Windows.Forms.Label();
            this.lblDest = new System.Windows.Forms.Label();
            this.cbDestination = new System.Windows.Forms.ComboBox();
            this.chkMultiDest = new System.Windows.Forms.CheckBox();
            this.pnlMultiDest = new System.Windows.Forms.Panel();
            this.lnkSelectAllDest = new System.Windows.Forms.LinkLabel();
            this.lnkClearAllDest = new System.Windows.Forms.LinkLabel();
            this.lblMultiCount = new System.Windows.Forms.Label();
            this.clbDestinations = new System.Windows.Forms.CheckedListBox();
            this.lblQuickAdd = new System.Windows.Forms.Label();
            this.txtQuickAddDest = new System.Windows.Forms.TextBox();
            this.btnAddQuickDest = new System.Windows.Forms.Button();
            this.rbSeparateRates = new System.Windows.Forms.RadioButton();
            this.rbCombinedRoute = new System.Windows.Forms.RadioButton();
            this.lblTt = new System.Windows.Forms.Label();
            this.cbTruckType = new System.Windows.Forms.ComboBox();
            this.chkPeak = new System.Windows.Forms.CheckBox();
            this.lblRate = new System.Windows.Forms.Label();
            this.numRate = new System.Windows.Forms.NumericUpDown();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlGridHeaderBar = new System.Windows.Forms.Panel();
            this.lblGridHeader = new System.Windows.Forms.Label();
            this.pnlGridActionsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.txtGridSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.gridRates = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.pnlFormActionsBar.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlFormBody.SuspendLayout();
            this.pnlFormHeaderBar.SuspendLayout();
            this.pnlMultiDest.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRate)).BeginInit();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRates)).BeginInit();
            this.pnlSummary.SuspendLayout();
            this.SuspendLayout();

            this.AutoScroll = false;
            this.AutoScrollMinSize = System.Drawing.Size.Empty;
            this.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(1420, 820);

            this.tblMasterContent.ColumnCount = 2;
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 480F));
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Controls.Add(this.pnlForm, 0, 0);
            this.tblMasterContent.Controls.Add(this.pnlGridCard, 1, 0);
            this.tblMasterContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMasterContent.Location = new System.Drawing.Point(16, 16);
            this.tblMasterContent.Name = "tblMasterContent";
            this.tblMasterContent.RowCount = 1;
            this.tblMasterContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Size = new System.Drawing.Size(1388, 748);
            this.tblMasterContent.TabIndex = 1;

            this.pnlForm.AutoScroll = false;
            this.pnlForm.BackColor = System.Drawing.Color.White;
            this.pnlForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlForm.Location = new System.Drawing.Point(0, 0);
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(0);
            this.pnlForm.Size = new System.Drawing.Size(464, 748);
            this.pnlForm.TabIndex = 0;

            this.pnlFormHeaderBar.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFormHeaderBar.Controls.Add(this.lblFormHeader);
            this.pnlFormHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFormHeaderBar.Location = new System.Drawing.Point(0, 0);
            this.pnlFormHeaderBar.Margin = new System.Windows.Forms.Padding(0);
            this.pnlFormHeaderBar.Name = "pnlFormHeaderBar";
            this.pnlFormHeaderBar.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlFormHeaderBar.Size = new System.Drawing.Size(480, 38);
            this.pnlFormHeaderBar.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblFormHeader.Location = new System.Drawing.Point(0, 6);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Size = new System.Drawing.Size(185, 21);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Standard Pricing Entry";

            this.lblDest.AutoSize = true;
            this.lblDest.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDest.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDest.Location = new System.Drawing.Point(16, 60);
            this.lblDest.Name = "lblDest";
            this.lblDest.Size = new System.Drawing.Size(144, 17);
            this.lblDest.TabIndex = 1;
            this.lblDest.Text = "Destination Coverage:";

            this.chkMultiDest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkMultiDest.AutoSize = true;
            this.chkMultiDest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkMultiDest.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.chkMultiDest.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.chkMultiDest.Location = new System.Drawing.Point(220, 59);
            this.chkMultiDest.Name = "chkMultiDest";
            this.chkMultiDest.Size = new System.Drawing.Size(225, 19);
            this.chkMultiDest.TabIndex = 2;
            this.chkMultiDest.Text = "⚡ Apply to Multiple Destinations";
            this.chkMultiDest.UseVisualStyleBackColor = true;

            this.cbDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbDestination.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDestination.FormattingEnabled = true;
            this.cbDestination.Location = new System.Drawing.Point(16, 82);
            this.cbDestination.Name = "cbDestination";
            this.cbDestination.Size = new System.Drawing.Size(432, 25);
            this.cbDestination.TabIndex = 3;

            this.pnlMultiDest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlMultiDest.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlMultiDest.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMultiDest.Controls.Add(this.lnkSelectAllDest);
            this.pnlMultiDest.Controls.Add(this.lnkClearAllDest);
            this.pnlMultiDest.Controls.Add(this.lblMultiCount);
            this.pnlMultiDest.Controls.Add(this.clbDestinations);
            this.pnlMultiDest.Controls.Add(this.lblQuickAdd);
            this.pnlMultiDest.Controls.Add(this.txtQuickAddDest);
            this.pnlMultiDest.Controls.Add(this.btnAddQuickDest);
            this.pnlMultiDest.Controls.Add(this.rbSeparateRates);
            this.pnlMultiDest.Controls.Add(this.rbCombinedRoute);
            this.pnlMultiDest.Location = new System.Drawing.Point(16, 82);
            this.pnlMultiDest.Name = "pnlMultiDest";
            this.pnlMultiDest.Size = new System.Drawing.Size(432, 256);
            this.pnlMultiDest.TabIndex = 4;
            this.pnlMultiDest.Visible = false;

            this.lnkSelectAllDest.AutoSize = true;
            this.lnkSelectAllDest.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lnkSelectAllDest.LinkColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.lnkSelectAllDest.Location = new System.Drawing.Point(8, 6);
            this.lnkSelectAllDest.Name = "lnkSelectAllDest";
            this.lnkSelectAllDest.Size = new System.Drawing.Size(55, 15);
            this.lnkSelectAllDest.TabIndex = 0;
            this.lnkSelectAllDest.Text = "Select All";

            this.lnkClearAllDest.AutoSize = true;
            this.lnkClearAllDest.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lnkClearAllDest.LinkColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lnkClearAllDest.Location = new System.Drawing.Point(82, 6);
            this.lnkClearAllDest.Name = "lnkClearAllDest";
            this.lnkClearAllDest.Size = new System.Drawing.Size(51, 15);
            this.lnkClearAllDest.TabIndex = 1;
            this.lnkClearAllDest.Text = "Clear All";

            this.lblMultiCount.AutoSize = true;
            this.lblMultiCount.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMultiCount.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblMultiCount.Location = new System.Drawing.Point(155, 6);
            this.lblMultiCount.Name = "lblMultiCount";
            this.lblMultiCount.Size = new System.Drawing.Size(125, 15);
            this.lblMultiCount.TabIndex = 2;
            this.lblMultiCount.Text = "Selected: 0 destinations";

            this.clbDestinations.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbDestinations.CheckOnClick = true;
            this.clbDestinations.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbDestinations.FormattingEnabled = true;
            this.clbDestinations.Location = new System.Drawing.Point(6, 26);
            this.clbDestinations.Name = "clbDestinations";
            this.clbDestinations.Size = new System.Drawing.Size(418, 108);
            this.clbDestinations.TabIndex = 3;

            this.lblQuickAdd.AutoSize = true;
            this.lblQuickAdd.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblQuickAdd.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblQuickAdd.Location = new System.Drawing.Point(6, 140);
            this.lblQuickAdd.Name = "lblQuickAdd";
            this.lblQuickAdd.Size = new System.Drawing.Size(262, 13);
            this.lblQuickAdd.TabIndex = 4;
            this.lblQuickAdd.Text = "➕ Type new destination(s), comma-separated:";

            this.txtQuickAddDest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtQuickAddDest.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtQuickAddDest.Location = new System.Drawing.Point(6, 158);
            this.txtQuickAddDest.MaxLength = 200;
            this.txtQuickAddDest.Name = "txtQuickAddDest";
            this.txtQuickAddDest.PlaceholderText = "e.g. Quezon, Lucena, Rizal, Tarlac";
            this.txtQuickAddDest.Size = new System.Drawing.Size(333, 23);
            this.txtQuickAddDest.TabIndex = 5;

            this.btnAddQuickDest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddQuickDest.BackColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnAddQuickDest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddQuickDest.FlatAppearance.BorderSize = 0;
            this.btnAddQuickDest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddQuickDest.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnAddQuickDest.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.btnAddQuickDest.Location = new System.Drawing.Point(345, 157);
            this.btnAddQuickDest.Name = "btnAddQuickDest";
            this.btnAddQuickDest.Size = new System.Drawing.Size(79, 25);
            this.btnAddQuickDest.TabIndex = 6;
            this.btnAddQuickDest.Text = "+ Add";
            this.btnAddQuickDest.UseVisualStyleBackColor = false;

            this.rbSeparateRates.AutoSize = true;
            this.rbSeparateRates.Checked = true;
            this.rbSeparateRates.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.rbSeparateRates.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.rbSeparateRates.Location = new System.Drawing.Point(6, 192);
            this.rbSeparateRates.Name = "rbSeparateRates";
            this.rbSeparateRates.Size = new System.Drawing.Size(287, 19);
            this.rbSeparateRates.TabIndex = 7;
            this.rbSeparateRates.TabStop = true;
            this.rbSeparateRates.Text = "Create separate rate entry for each destination";
            this.rbSeparateRates.UseVisualStyleBackColor = true;

            this.rbCombinedRoute.AutoSize = true;
            this.rbCombinedRoute.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.rbCombinedRoute.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.rbCombinedRoute.Location = new System.Drawing.Point(6, 220);
            this.rbCombinedRoute.Name = "rbCombinedRoute";
            this.rbCombinedRoute.Size = new System.Drawing.Size(265, 19);
            this.rbCombinedRoute.TabIndex = 8;
            this.rbCombinedRoute.Text = "Combine into 1 multi-stop route entry";
            this.rbCombinedRoute.UseVisualStyleBackColor = true;

            this.lblTt.AutoSize = true;
            this.lblTt.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTt.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblTt.Location = new System.Drawing.Point(16, 118);
            this.lblTt.Name = "lblTt";
            this.lblTt.Size = new System.Drawing.Size(141, 17);
            this.lblTt.TabIndex = 5;
            this.lblTt.Text = "Assigned Truck Type:";

            this.cbTruckType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTruckType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTruckType.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbTruckType.FormattingEnabled = true;
            this.cbTruckType.Location = new System.Drawing.Point(16, 140);
            this.cbTruckType.Name = "cbTruckType";
            this.cbTruckType.Size = new System.Drawing.Size(432, 25);
            this.cbTruckType.TabIndex = 6;

            this.chkPeak.AutoSize = true;
            this.chkPeak.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.chkPeak.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.chkPeak.Location = new System.Drawing.Point(16, 180);
            this.chkPeak.Name = "chkPeak";
            this.chkPeak.Size = new System.Drawing.Size(193, 21);
            this.chkPeak.TabIndex = 7;
            this.chkPeak.Text = "Peak Season Rate Premium";

            this.lblRate.AutoSize = true;
            this.lblRate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblRate.Location = new System.Drawing.Point(16, 212);
            this.lblRate.Name = "lblRate";
            this.lblRate.Size = new System.Drawing.Size(176, 17);
            this.lblRate.TabIndex = 8;
            this.lblRate.Text = "Standard Hauling Price (₱):";

            this.numRate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numRate.DecimalPlaces = 2;
            this.numRate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numRate.Location = new System.Drawing.Point(16, 234);
            this.numRate.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numRate.Name = "numRate";
            this.numRate.Size = new System.Drawing.Size(432, 24);
            this.numRate.TabIndex = 9;
            this.numRate.ThousandsSeparator = true;
            this.numRate.Value = new decimal(new int[] { 8500, 0, 0, 0 });

            this.btnSave.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(448, 42);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "➕ ADD RATE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

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
            this.btnUpdate.Size = new System.Drawing.Size(220, 40);
            this.btnUpdate.TabIndex = 11;
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
            this.btnDelete.Location = new System.Drawing.Point(228, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(220, 40);
            this.btnDelete.TabIndex = 12;
            this.btnDelete.Text = "🗑️ DELETE";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.BackColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(16, 94);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(448, 36);
            this.btnClear.TabIndex = 13;
            this.btnClear.Text = "🧹 CLEAR";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.pnlForm.Controls.Add(this.pnlFormBody);
            this.pnlForm.Controls.Add(this.pnlFormActionsBar);
            this.pnlForm.Controls.Add(this.pnlFormHeaderBar);
            
            this.pnlSaveSpacer.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSaveSpacer.Size = new System.Drawing.Size(448, 8);
            
            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Height = 40;
            
            this.pnlUpdateSpacer.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlUpdateSpacer.Size = new System.Drawing.Size(448, 8);
            
            this.pnlFormActionsBar.Controls.Add(this.btnClear);
            this.pnlFormActionsBar.Controls.Add(this.pnlUpdateSpacer);
            this.pnlFormActionsBar.Controls.Add(this.tblUpdateDelete);
            this.pnlFormActionsBar.Controls.Add(this.pnlSaveSpacer);
            this.pnlFormActionsBar.Controls.Add(this.btnSave);
            this.pnlFormActionsBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFormActionsBar.Padding = new System.Windows.Forms.Padding(16, 4, 16, 8);
            this.pnlFormActionsBar.Size = new System.Drawing.Size(480, 140);
            
            this.pnlFormBody.AutoScroll = true;
            this.pnlFormBody.Controls.Add(this.numRate);
            this.pnlFormBody.Controls.Add(this.lblRate);
            this.pnlFormBody.Controls.Add(this.chkPeak);
            this.pnlFormBody.Controls.Add(this.cbTruckType);
            this.pnlFormBody.Controls.Add(this.lblTt);
            this.pnlFormBody.Controls.Add(this.pnlMultiDest);
            this.pnlFormBody.Controls.Add(this.cbDestination);
            this.pnlFormBody.Controls.Add(this.chkMultiDest);
            this.pnlFormBody.Controls.Add(this.lblDest);
            this.pnlFormBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFormBody.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridRates);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(480, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(908, 748);
            this.pnlGridCard.TabIndex = 1;

            this.pnlGridHeaderBar.AutoSize = true;
            this.pnlGridHeaderBar.Controls.Add(this.pnlGridActionsFlow);
            this.pnlGridHeaderBar.Controls.Add(this.lblGridHeader);
            this.pnlGridHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridHeaderBar.Location = new System.Drawing.Point(16, 16);
            this.pnlGridHeaderBar.Name = "pnlGridHeaderBar";
            this.pnlGridHeaderBar.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlGridHeaderBar.Size = new System.Drawing.Size(876, 44);
            this.pnlGridHeaderBar.TabIndex = 0;

            this.lblGridHeader.AutoSize = true;
            this.lblGridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGridHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblGridHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGridHeader.Location = new System.Drawing.Point(0, 0);
            this.lblGridHeader.Name = "lblGridHeader";
            this.lblGridHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblGridHeader.Size = new System.Drawing.Size(217, 26);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Standard Rate Matrix Register";

            this.pnlGridActionsFlow.AutoSize = true;
            this.pnlGridActionsFlow.Controls.Add(this.txtGridSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnClearSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnPrint);
            this.pnlGridActionsFlow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridActionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlGridActionsFlow.Location = new System.Drawing.Point(0, 26);
            this.pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            this.pnlGridActionsFlow.Size = new System.Drawing.Size(450, 36);
            this.pnlGridActionsFlow.TabIndex = 1;
            this.pnlGridActionsFlow.WrapContents = true;

            this.txtGridSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGridSearch.Location = new System.Drawing.Point(0, 4);
            this.txtGridSearch.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
            this.txtGridSearch.MaxLength = 100;
            this.txtGridSearch.Name = "txtGridSearch";
            this.txtGridSearch.PlaceholderText = "🔍 Search origin, destination, vehicle...";
            this.txtGridSearch.Size = new System.Drawing.Size(220, 24);
            this.txtGridSearch.TabIndex = 0;
            this.txtGridSearch.TextChanged += new System.EventHandler(this.TxtGridSearch_TextChanged);

            this.btnClearSearch.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClearSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearSearch.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnClearSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearSearch.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnClearSearch.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.btnClearSearch.Location = new System.Drawing.Point(228, 4);
            this.btnClearSearch.Margin = new System.Windows.Forms.Padding(4, 4, 8, 0);
            this.btnClearSearch.Name = "btnClearSearch";
            this.btnClearSearch.Size = new System.Drawing.Size(28, 26);
            this.btnClearSearch.TabIndex = 1;
            this.btnClearSearch.Text = "✖";
            this.btnClearSearch.UseVisualStyleBackColor = false;
            this.btnClearSearch.Click += new System.EventHandler(this.BtnClearSearch_Click);

            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnPrint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrint.FlatAppearance.BorderSize = 0;
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Location = new System.Drawing.Point(268, 4);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(170, 28);
            this.btnPrint.TabIndex = 2;
            this.btnPrint.Text = "🖨️ Print Report";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridRates.AllowUserToAddRows = false;
            this.gridRates.AllowUserToDeleteRows = false;
            this.gridRates.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRates.BackgroundColor = System.Drawing.Color.White;
            this.gridRates.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridRates.ColumnHeadersHeight = 36;
            this.gridRates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridRates.EnableHeadersVisualStyles = false;
            this.gridRates.Location = new System.Drawing.Point(16, 60);
            this.gridRates.Name = "gridRates";
            this.gridRates.RowHeadersVisible = false;
            this.gridRates.RowTemplate.Height = 32;
            this.gridRates.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRates.Size = new System.Drawing.Size(876, 634);
            this.gridRates.TabIndex = 1;
            this.gridRates.SelectionChanged += new System.EventHandler(this.GridRates_SelectionChanged);

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

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tblMasterContent);
            this.Name = "RateMatrixView";
            this.pnlFormActionsBar.ResumeLayout(false);
            this.pnlFormBody.ResumeLayout(false);
            this.pnlFormBody.PerformLayout();
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.pnlFormHeaderBar.ResumeLayout(false);
            this.pnlFormHeaderBar.PerformLayout();
            this.pnlMultiDest.ResumeLayout(false);
            this.pnlMultiDest.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRate)).EndInit();
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridCard.PerformLayout();
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRates)).EndInit();
            this.pnlSummary.ResumeLayout(false);
            this.pnlSummary.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tblMasterContent;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Panel pnlFormActionsBar;
        private System.Windows.Forms.Panel pnlSaveSpacer;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
        private System.Windows.Forms.Panel pnlUpdateSpacer;
        private System.Windows.Forms.Panel pnlFormBody;
        private System.Windows.Forms.Panel pnlFormHeaderBar;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.Label lblDest;
        private System.Windows.Forms.ComboBox cbDestination;
        private System.Windows.Forms.CheckBox chkMultiDest;
        private System.Windows.Forms.Panel pnlMultiDest;
        private System.Windows.Forms.LinkLabel lnkSelectAllDest;
        private System.Windows.Forms.LinkLabel lnkClearAllDest;
        private System.Windows.Forms.Label lblMultiCount;
        private System.Windows.Forms.CheckedListBox clbDestinations;
        private System.Windows.Forms.Label lblQuickAdd;
        private System.Windows.Forms.TextBox txtQuickAddDest;
        private System.Windows.Forms.Button btnAddQuickDest;
        private System.Windows.Forms.RadioButton rbSeparateRates;
        private System.Windows.Forms.RadioButton rbCombinedRoute;
        private System.Windows.Forms.Label lblTt;
        private System.Windows.Forms.ComboBox cbTruckType;
        private System.Windows.Forms.CheckBox chkPeak;
        private System.Windows.Forms.Label lblRate;
        private System.Windows.Forms.NumericUpDown numRate;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeaderBar;
        private System.Windows.Forms.Label lblGridHeader;
        private System.Windows.Forms.FlowLayoutPanel pnlGridActionsFlow;
        private System.Windows.Forms.TextBox txtGridSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.DataGridView gridRates;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
