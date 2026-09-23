namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class LoadingManagementView
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
            this.lblClient = new System.Windows.Forms.Label();
            this.cbClient = new System.Windows.Forms.ComboBox();
            this.pnlDestHeader = new System.Windows.Forms.Panel();
            this.lblDest = new System.Windows.Forms.Label();
            this.btnAddStop = new System.Windows.Forms.Button();
            this.cbDestination = new System.Windows.Forms.ComboBox();
            this.lblDestHint = new System.Windows.Forms.Label();
            this.tblCargoPeak = new System.Windows.Forms.TableLayoutPanel();
            this.lblCargoType = new System.Windows.Forms.Label();
            this.lblPeakHeader = new System.Windows.Forms.Label();
            this.cbCargoType = new System.Windows.Forms.ComboBox();
            this.tblDateTime = new System.Windows.Forms.TableLayoutPanel();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpScheduleDate = new System.Windows.Forms.DateTimePicker();
            this.lblTime = new System.Windows.Forms.Label();
            this.pnlTimeRow = new System.Windows.Forms.Panel();
            this.txtDepartureTime = new System.Windows.Forms.TextBox();
            this.btnTimePicker = new System.Windows.Forms.Button();
            this.tblCrates = new System.Windows.Forms.TableLayoutPanel();
            this.lblGCrate = new System.Windows.Forms.Label();
            this.numGCrate = new System.Windows.Forms.NumericUpDown();
            this.lblECrate = new System.Windows.Forms.Label();
            this.numECrate = new System.Windows.Forms.NumericUpDown();
            this.lblTotalCrates = new System.Windows.Forms.Label();
            this.txtTotalCrates = new System.Windows.Forms.TextBox();
            this.pnlRec = new System.Windows.Forms.Panel();
            this.lblRec = new System.Windows.Forms.Label();
            this.chkIsPeak = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.tblUpdateDelete = new System.Windows.Forms.TableLayoutPanel();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnClone = new System.Windows.Forms.Button();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlGridHeaderBar = new System.Windows.Forms.Panel();
            this.lblGridHeader = new System.Windows.Forms.Label();
            this.pnlGridActionsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.txtGridSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.gridSchedules = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.pnlDestHeader.SuspendLayout();
            this.tblCargoPeak.SuspendLayout();
            this.tblDateTime.SuspendLayout();
            this.pnlTimeRow.SuspendLayout();
            this.tblCrates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGCrate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numECrate)).BeginInit();
            this.pnlRec.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchedules)).BeginInit();
            this.pnlSummary.SuspendLayout();
            this.SuspendLayout();

            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1000, 620);
            this.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(1420, 840);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new System.Drawing.Point(16, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.lblTitle.Size = new System.Drawing.Size(350, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Loading Schedule Management";

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
            this.tblMasterContent.Size = new System.Drawing.Size(1388, 768);
            this.tblMasterContent.TabIndex = 1;

            this.pnlForm.AutoScroll = true;
            this.pnlForm.BackColor = System.Drawing.Color.White;
            this.pnlForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlForm.Location = new System.Drawing.Point(0, 0);
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(16);
            this.pnlForm.Size = new System.Drawing.Size(448, 768);
            this.pnlForm.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblFormHeader.Location = new System.Drawing.Point(16, 16);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblFormHeader.Size = new System.Drawing.Size(152, 28);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Loading Order Form";

            this.lblClient.AutoSize = true;
            this.lblClient.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblClient.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblClient.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblClient.Location = new System.Drawing.Point(16, 44);
            this.lblClient.Name = "lblClient";
            this.lblClient.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblClient.Size = new System.Drawing.Size(86, 23);
            this.lblClient.TabIndex = 1;
            this.lblClient.Text = "Client Name:";

            this.cbClient.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbClient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cbClient.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbClient.FormattingEnabled = true;
            this.cbClient.Location = new System.Drawing.Point(16, 67);
            this.cbClient.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbClient.Name = "cbClient";
            this.cbClient.Size = new System.Drawing.Size(416, 25);
            this.cbClient.TabIndex = 2;

            this.pnlDestHeader.Controls.Add(this.btnAddStop);
            this.pnlDestHeader.Controls.Add(this.lblDest);
            this.pnlDestHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlDestHeader.Location = new System.Drawing.Point(16, 92);
            this.pnlDestHeader.Margin = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.pnlDestHeader.Name = "pnlDestHeader";
            this.pnlDestHeader.Size = new System.Drawing.Size(536, 28);
            this.pnlDestHeader.TabIndex = 3;

            this.lblDest.AutoSize = true;
            this.lblDest.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblDest.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDest.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDest.Location = new System.Drawing.Point(0, 0);
            this.lblDest.Name = "lblDest";
            this.lblDest.Size = new System.Drawing.Size(255, 17);
            this.lblDest.TabIndex = 0;
            this.lblDest.Text = "Destination Address(es) / Route Stops:";

            this.btnAddStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddStop.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAddStop.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnAddStop.Location = new System.Drawing.Point(376, 0);
            this.btnAddStop.Name = "btnAddStop";
            this.btnAddStop.Size = new System.Drawing.Size(160, 28);
            this.btnAddStop.TabIndex = 1;
            this.btnAddStop.Text = "➕ Add Stop / Address";
            this.btnAddStop.UseVisualStyleBackColor = true;

            this.cbDestination.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbDestination.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbDestination.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbDestination.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cbDestination.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDestination.FormattingEnabled = true;
            this.cbDestination.Location = new System.Drawing.Point(16, 120);
            this.cbDestination.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.cbDestination.Name = "cbDestination";
            this.cbDestination.Size = new System.Drawing.Size(536, 25);
            this.cbDestination.TabIndex = 4;

            this.lblDestHint.AutoSize = true;
            this.lblDestHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDestHint.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblDestHint.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblDestHint.Location = new System.Drawing.Point(16, 145);
            this.lblDestHint.Name = "lblDestHint";
            this.lblDestHint.Padding = new System.Windows.Forms.Padding(0, 2, 0, 6);
            this.lblDestHint.Size = new System.Drawing.Size(430, 21);
            this.lblDestHint.TabIndex = 5;
            this.lblDestHint.Text = "💡 Type any destination address or select from dropdown. Click [➕ Add Stop] for multi-drop routes.";

            this.tblCargoPeak.ColumnCount = 2;
            this.tblCargoPeak.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCargoPeak.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCargoPeak.Controls.Add(this.lblCargoType, 0, 0);
            this.tblCargoPeak.Controls.Add(this.lblPeakHeader, 1, 0);
            this.tblCargoPeak.Controls.Add(this.cbCargoType, 0, 1);
            this.tblCargoPeak.Controls.Add(this.chkIsPeak, 1, 1);
            this.tblCargoPeak.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCargoPeak.Location = new System.Drawing.Point(16, 166);
            this.tblCargoPeak.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.tblCargoPeak.Name = "tblCargoPeak";
            this.tblCargoPeak.RowCount = 2;
            this.tblCargoPeak.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCargoPeak.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCargoPeak.Size = new System.Drawing.Size(536, 56);
            this.tblCargoPeak.TabIndex = 6;

            this.lblCargoType.AutoSize = true;
            this.lblCargoType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCargoType.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCargoType.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblCargoType.Location = new System.Drawing.Point(0, 0);
            this.lblCargoType.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblCargoType.Name = "lblCargoType";
            this.lblCargoType.Size = new System.Drawing.Size(264, 17);
            this.lblCargoType.TabIndex = 0;
            this.lblCargoType.Text = "Cargo / Temp Type:";

            this.lblPeakHeader.AutoSize = true;
            this.lblPeakHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPeakHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPeakHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblPeakHeader.Location = new System.Drawing.Point(272, 0);
            this.lblPeakHeader.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblPeakHeader.Name = "lblPeakHeader";
            this.lblPeakHeader.Size = new System.Drawing.Size(264, 17);
            this.lblPeakHeader.TabIndex = 1;
            this.lblPeakHeader.Text = "Pricing Tariff Season:";

            this.cbCargoType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbCargoType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCargoType.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbCargoType.FormattingEnabled = true;
            this.cbCargoType.Items.AddRange(new object[] {
            "Cold / Chilled",
            "Dry Goods",
            "Dry and Chilled Mix"});
            this.cbCargoType.Location = new System.Drawing.Point(0, 21);
            this.cbCargoType.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.cbCargoType.Name = "cbCargoType";
            this.cbCargoType.Size = new System.Drawing.Size(264, 25);
            this.cbCargoType.TabIndex = 2;

            this.chkIsPeak.AutoSize = true;
            this.chkIsPeak.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkIsPeak.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.chkIsPeak.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.chkIsPeak.Location = new System.Drawing.Point(272, 21);
            this.chkIsPeak.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.chkIsPeak.Name = "chkIsPeak";
            this.chkIsPeak.Size = new System.Drawing.Size(264, 25);
            this.chkIsPeak.TabIndex = 3;
            this.chkIsPeak.Text = "Is Peak Season Rate";
            this.chkIsPeak.UseVisualStyleBackColor = true;

            this.tblDateTime.ColumnCount = 2;
            this.tblDateTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblDateTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblDateTime.Controls.Add(this.lblDate, 0, 0);
            this.tblDateTime.Controls.Add(this.lblTime, 1, 0);
            this.tblDateTime.Controls.Add(this.dtpScheduleDate, 0, 1);
            this.tblDateTime.Controls.Add(this.pnlTimeRow, 1, 1);
            this.tblDateTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblDateTime.Location = new System.Drawing.Point(16, 148);
            this.tblDateTime.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.tblDateTime.Name = "tblDateTime";
            this.tblDateTime.RowCount = 2;
            this.tblDateTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDateTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDateTime.Size = new System.Drawing.Size(416, 56);
            this.tblDateTime.TabIndex = 4;

            this.lblDate.AutoSize = true;
            this.lblDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDate.Location = new System.Drawing.Point(0, 0);
            this.lblDate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(204, 17);
            this.lblDate.TabIndex = 0;
            this.lblDate.Text = "Schedule Date:";

            this.lblTime.AutoSize = true;
            this.lblTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTime.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTime.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblTime.Location = new System.Drawing.Point(212, 0);
            this.lblTime.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(204, 17);
            this.lblTime.TabIndex = 1;
            this.lblTime.Text = "Departure Time:";

            this.dtpScheduleDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpScheduleDate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpScheduleDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpScheduleDate.Location = new System.Drawing.Point(0, 21);
            this.dtpScheduleDate.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.dtpScheduleDate.Name = "dtpScheduleDate";
            this.dtpScheduleDate.Size = new System.Drawing.Size(204, 24);
            this.dtpScheduleDate.TabIndex = 2;

            this.pnlTimeRow.Controls.Add(this.btnTimePicker);
            this.pnlTimeRow.Controls.Add(this.txtDepartureTime);
            this.pnlTimeRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTimeRow.Location = new System.Drawing.Point(212, 21);
            this.pnlTimeRow.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.pnlTimeRow.Name = "pnlTimeRow";
            this.pnlTimeRow.Size = new System.Drawing.Size(204, 28);
            this.pnlTimeRow.TabIndex = 3;

            this.txtDepartureTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDepartureTime.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtDepartureTime.Location = new System.Drawing.Point(0, 0);
            this.txtDepartureTime.Name = "txtDepartureTime";
            this.txtDepartureTime.ReadOnly = true;
            this.txtDepartureTime.Size = new System.Drawing.Size(162, 24);
            this.txtDepartureTime.TabIndex = 0;
            this.txtDepartureTime.Text = "08:00 AM";

            this.btnTimePicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTimePicker.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnTimePicker.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTimePicker.Location = new System.Drawing.Point(162, 0);
            this.btnTimePicker.Name = "btnTimePicker";
            this.btnTimePicker.Size = new System.Drawing.Size(42, 28);
            this.btnTimePicker.TabIndex = 1;
            this.btnTimePicker.Text = "🕒";
            this.btnTimePicker.UseVisualStyleBackColor = true;

            this.tblCrates.ColumnCount = 2;
            this.tblCrates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCrates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCrates.Controls.Add(this.lblGCrate, 0, 0);
            this.tblCrates.Controls.Add(this.lblECrate, 1, 0);
            this.tblCrates.Controls.Add(this.numGCrate, 0, 1);
            this.tblCrates.Controls.Add(this.numECrate, 1, 1);
            this.tblCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCrates.Location = new System.Drawing.Point(16, 204);
            this.tblCrates.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.tblCrates.Name = "tblCrates";
            this.tblCrates.RowCount = 2;
            this.tblCrates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCrates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCrates.Size = new System.Drawing.Size(416, 56);
            this.tblCrates.TabIndex = 5;

            this.lblGCrate.AutoSize = true;
            this.lblGCrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGCrate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblGCrate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGCrate.Location = new System.Drawing.Point(0, 0);
            this.lblGCrate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblGCrate.Name = "lblGCrate";
            this.lblGCrate.Size = new System.Drawing.Size(204, 17);
            this.lblGCrate.TabIndex = 0;
            this.lblGCrate.Text = "G-Crates:";

            this.lblECrate.AutoSize = true;
            this.lblECrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblECrate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblECrate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblECrate.Location = new System.Drawing.Point(212, 0);
            this.lblECrate.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblECrate.Name = "lblECrate";
            this.lblECrate.Size = new System.Drawing.Size(204, 17);
            this.lblECrate.TabIndex = 1;
            this.lblECrate.Text = "E-Crates:";

            this.numGCrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numGCrate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numGCrate.Location = new System.Drawing.Point(0, 21);
            this.numGCrate.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.numGCrate.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numGCrate.Name = "numGCrate";
            this.numGCrate.Size = new System.Drawing.Size(204, 24);
            this.numGCrate.TabIndex = 2;
            this.numGCrate.Value = new decimal(new int[] { 150, 0, 0, 0 });

            this.numECrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numECrate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numECrate.Location = new System.Drawing.Point(212, 21);
            this.numECrate.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.numECrate.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numECrate.Name = "numECrate";
            this.numECrate.Size = new System.Drawing.Size(204, 24);
            this.numECrate.TabIndex = 3;

            this.lblTotalCrates.AutoSize = true;
            this.lblTotalCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTotalCrates.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTotalCrates.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblTotalCrates.Location = new System.Drawing.Point(16, 260);
            this.lblTotalCrates.Name = "lblTotalCrates";
            this.lblTotalCrates.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblTotalCrates.Size = new System.Drawing.Size(147, 23);
            this.lblTotalCrates.TabIndex = 6;
            this.lblTotalCrates.Text = "Total Combined Crates:";

            this.txtTotalCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtTotalCrates.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.txtTotalCrates.Location = new System.Drawing.Point(16, 283);
            this.txtTotalCrates.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.txtTotalCrates.Name = "txtTotalCrates";
            this.txtTotalCrates.ReadOnly = true;
            this.txtTotalCrates.Size = new System.Drawing.Size(416, 24);
            this.txtTotalCrates.TabIndex = 7;
            this.txtTotalCrates.Text = "150 Crates";

            this.pnlRec.BackColor = System.Drawing.Color.FromArgb(240, 253, 244);
            this.pnlRec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlRec.Controls.Add(this.lblRec);
            this.pnlRec.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRec.Location = new System.Drawing.Point(16, 315);
            this.pnlRec.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlRec.Name = "pnlRec";
            this.pnlRec.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.pnlRec.Size = new System.Drawing.Size(416, 46);
            this.pnlRec.TabIndex = 8;

            this.lblRec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRec.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRec.ForeColor = System.Drawing.Color.FromArgb(22, 101, 52);
            this.lblRec.Location = new System.Drawing.Point(10, 6);
            this.lblRec.Name = "lblRec";
            this.lblRec.Size = new System.Drawing.Size(394, 32);
            this.lblRec.TabIndex = 0;
            this.lblRec.Text = "Truck Recommendation...";


            this.btnSave.BackColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 404);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 4, 0, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(416, 40);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save Loading Schedule";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 452);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(416, 42);
            this.tblUpdateDelete.TabIndex = 11;

            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.Location = new System.Drawing.Point(0, 0);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(204, 42);
            this.btnUpdate.TabIndex = 0;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDelete.Location = new System.Drawing.Point(212, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(204, 42);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.Location = new System.Drawing.Point(16, 500);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(416, 36);
            this.btnClear.TabIndex = 12;
            this.btnClear.Text = "Clear Form";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.btnClone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClone.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClone.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClone.Location = new System.Drawing.Point(16, 542);
            this.btnClone.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.btnClone.Name = "btnClone";
            this.btnClone.Size = new System.Drawing.Size(416, 36);
            this.btnClone.TabIndex = 13;
            this.btnClone.Text = "📑 Repeat / Clone Selected Order";
            this.btnClone.UseVisualStyleBackColor = true;
            this.btnClone.Click += new System.EventHandler(this.BtnClone_Click);

            this.pnlForm.Controls.Add(this.btnClone);
            this.pnlForm.Controls.Add(this.btnClear);
            this.pnlForm.Controls.Add(this.tblUpdateDelete);
            this.pnlForm.Controls.Add(this.btnSave);
            this.pnlForm.Controls.Add(this.pnlRec);
            this.pnlForm.Controls.Add(this.txtTotalCrates);
            this.pnlForm.Controls.Add(this.lblTotalCrates);
            this.pnlForm.Controls.Add(this.tblCrates);
            this.pnlForm.Controls.Add(this.tblDateTime);
            this.pnlForm.Controls.Add(this.tblCargoPeak);
            this.pnlForm.Controls.Add(this.lblDestHint);
            this.pnlForm.Controls.Add(this.cbDestination);
            this.pnlForm.Controls.Add(this.pnlDestHeader);
            this.pnlForm.Controls.Add(this.cbClient);
            this.pnlForm.Controls.Add(this.lblClient);
            this.pnlForm.Controls.Add(this.lblFormHeader);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridSchedules);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(580, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(928, 768);
            this.pnlGridCard.TabIndex = 1;

            this.pnlGridHeaderBar.AutoSize = true;
            this.pnlGridHeaderBar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlGridHeaderBar.Controls.Add(this.pnlGridActionsFlow);
            this.pnlGridHeaderBar.Controls.Add(this.lblGridHeader);
            this.pnlGridHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridHeaderBar.Location = new System.Drawing.Point(16, 16);
            this.pnlGridHeaderBar.Name = "pnlGridHeaderBar";
            this.pnlGridHeaderBar.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlGridHeaderBar.Size = new System.Drawing.Size(896, 44);
            this.pnlGridHeaderBar.TabIndex = 0;

            this.lblGridHeader.AutoSize = true;
            this.lblGridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGridHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblGridHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGridHeader.Location = new System.Drawing.Point(0, 0);
            this.lblGridHeader.Name = "lblGridHeader";
            this.lblGridHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblGridHeader.Size = new System.Drawing.Size(155, 28);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Active Schedules List";

            this.pnlGridActionsFlow.AutoSize = true;
            this.pnlGridActionsFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlGridActionsFlow.Controls.Add(this.txtGridSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnClearSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnPrint);
            this.pnlGridActionsFlow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridActionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlGridActionsFlow.Location = new System.Drawing.Point(0, 28);
            this.pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            this.pnlGridActionsFlow.Size = new System.Drawing.Size(896, 36);
            this.pnlGridActionsFlow.TabIndex = 1;
            this.pnlGridActionsFlow.WrapContents = true;

            this.txtGridSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGridSearch.Location = new System.Drawing.Point(0, 4);
            this.txtGridSearch.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
            this.txtGridSearch.Name = "txtGridSearch";
            this.txtGridSearch.PlaceholderText = "🔍 Search schedule, client, route...";
            this.txtGridSearch.Size = new System.Drawing.Size(190, 24);
            this.txtGridSearch.TabIndex = 0;
            this.txtGridSearch.TextChanged += new System.EventHandler(this.TxtGridSearch_TextChanged);

            this.btnClearSearch.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClearSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearSearch.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnClearSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearSearch.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnClearSearch.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.btnClearSearch.Location = new System.Drawing.Point(248, 4);
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
            this.btnPrint.Location = new System.Drawing.Point(288, 2);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(160, 30);
            this.btnPrint.TabIndex = 2;
            this.btnPrint.Text = "🖨️ Print Schedule";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridSchedules.AllowUserToAddRows = false;
            this.gridSchedules.AllowUserToDeleteRows = false;
            this.gridSchedules.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridSchedules.BackgroundColor = System.Drawing.Color.White;
            this.gridSchedules.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridSchedules.ColumnHeadersHeight = 40;
            this.gridSchedules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSchedules.EnableHeadersVisualStyles = false;
            this.gridSchedules.Location = new System.Drawing.Point(16, 60);
            this.gridSchedules.MultiSelect = false;
            this.gridSchedules.Name = "gridSchedules";
            this.gridSchedules.ReadOnly = true;
            this.gridSchedules.RowHeadersVisible = false;
            this.gridSchedules.RowTemplate.Height = 36;
            this.gridSchedules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSchedules.Size = new System.Drawing.Size(896, 654);
            this.gridSchedules.TabIndex = 1;
            this.gridSchedules.SelectionChanged += new System.EventHandler(this.GridSchedules_SelectionChanged);

            this.pnlSummary.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlSummary.Controls.Add(this.lblSummary);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummary.Location = new System.Drawing.Point(16, 714);
            this.pnlSummary.Name = "pnlSummary";
            this.pnlSummary.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlSummary.Size = new System.Drawing.Size(896, 38);
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

            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.pnlDestHeader.ResumeLayout(false);
            this.pnlDestHeader.PerformLayout();
            this.tblCargoPeak.ResumeLayout(false);
            this.tblCargoPeak.PerformLayout();
            this.tblDateTime.ResumeLayout(false);
            this.tblDateTime.PerformLayout();
            this.pnlTimeRow.ResumeLayout(false);
            this.pnlTimeRow.PerformLayout();
            this.tblCrates.ResumeLayout(false);
            this.tblCrates.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGCrate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numECrate)).EndInit();
            this.pnlRec.ResumeLayout(false);
            this.tblUpdateDelete.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchedules)).EndInit();
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
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.ComboBox cbClient;
        private System.Windows.Forms.Panel pnlDestHeader;
        private System.Windows.Forms.Label lblDest;
        private System.Windows.Forms.Button btnAddStop;
        private System.Windows.Forms.ComboBox cbDestination;
        private System.Windows.Forms.Label lblDestHint;
        private System.Windows.Forms.TableLayoutPanel tblCargoPeak;
        private System.Windows.Forms.Label lblCargoType;
        private System.Windows.Forms.Label lblPeakHeader;
        private System.Windows.Forms.ComboBox cbCargoType;
        private System.Windows.Forms.TableLayoutPanel tblDateTime;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.DateTimePicker dtpScheduleDate;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Panel pnlTimeRow;
        private System.Windows.Forms.TextBox txtDepartureTime;
        private System.Windows.Forms.Button btnTimePicker;
        private System.Windows.Forms.TableLayoutPanel tblCrates;
        private System.Windows.Forms.Label lblGCrate;
        private System.Windows.Forms.NumericUpDown numGCrate;
        private System.Windows.Forms.Label lblECrate;
        private System.Windows.Forms.NumericUpDown numECrate;
        private System.Windows.Forms.Label lblTotalCrates;
        private System.Windows.Forms.TextBox txtTotalCrates;
        private System.Windows.Forms.Panel pnlRec;
        private System.Windows.Forms.Label lblRec;
        private System.Windows.Forms.CheckBox chkIsPeak;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClone;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeaderBar;
        private System.Windows.Forms.Label lblGridHeader;
        private System.Windows.Forms.FlowLayoutPanel pnlGridActionsFlow;
        private System.Windows.Forms.TextBox txtGridSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.DataGridView gridSchedules;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
