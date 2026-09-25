namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class DispatchManagementView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tmrAutoSaveDraft?.Stop();
                tmrAutoSaveDraft?.Dispose();
                tmrAutoSaveDraft = null;
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            tblMasterContent = new TableLayoutPanel();
            pnlForm = new Panel();
            pnlFormBody = new Panel();
            pnlStatusSpacer = new Panel();
            cbStatus = new ComboBox();
            lblSt = new Label();
            tblDateTime = new TableLayoutPanel();
            lblDd = new Label();
            lblDepartureTime = new Label();
            dtpDispatchDate = new DateTimePicker();
            pnlTimeRow = new Panel();
            cbDepartureTime = new ComboBox();
            btnPickAnalogTime = new Button();
            lblAidesSummary = new Label();
            flowAidesModule = new FlowLayoutPanel();
            pnlAidesSearchRow = new Panel();
            txtSearchAides = new TextBox();
            btnClearAides = new Button();
            lblAi = new Label();
            cbDriver = new ComboBox();
            lblDr = new Label();
            cbTruck = new ComboBox();
            lblTr = new Label();
            pnlRec = new Panel();
            lblRec = new Label();
            txtTotalCrates = new TextBox();
            lblTotalCrates = new Label();
            tblCrates = new TableLayoutPanel();
            lblGCrate = new Label();
            lblECrate = new Label();
            numGCrate = new NumericUpDown();
            numECrate = new NumericUpDown();
            tblCargoPeak = new TableLayoutPanel();
            lblCargoType = new Label();
            lblPeakHeader = new Label();
            cbCargoType = new ComboBox();
            chkIsPeak = new CheckBox();
            lblDestHint = new Label();
            cbDestination = new ComboBox();
            lblDest = new Label();
            cbClient = new ComboBox();
            lblClient = new Label();
            txtDispatchNumber = new TextBox();
            lblDispatchNumber = new Label();
            pnlFormActionsBar = new Panel();
            btnClear = new Button();
            pnlUpdateSpacer = new Panel();
            tblUpdateDelete = new TableLayoutPanel();
            btnUpdate = new Button();
            btnDelete = new Button();
            pnlSaveSpacer = new Panel();
            btnSave = new Button();
            pnlFormHeaderBar = new Panel();
            lblFormHeader = new Label();
            btnCloseForm = new Button();
            pnlGridCard = new Panel();
            gridDispatch = new DataGridView();
            pnlSummary = new Panel();
            lblSummary = new Label();
            pnlGridHeaderBar = new Panel();
            pnlGridActionsFlow = new FlowLayoutPanel();
            txtGridSearch = new TextBox();
            btnClearSearch = new Button();
            btnPrint = new Button();
            lblGridHeader = new Label();
            tblMasterContent.SuspendLayout();
            pnlForm.SuspendLayout();
            pnlFormBody.SuspendLayout();
            tblDateTime.SuspendLayout();
            pnlTimeRow.SuspendLayout();
            pnlAidesSearchRow.SuspendLayout();
            pnlRec.SuspendLayout();
            tblCrates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numGCrate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numECrate).BeginInit();
            tblCargoPeak.SuspendLayout();
            pnlFormActionsBar.SuspendLayout();
            tblUpdateDelete.SuspendLayout();
            pnlFormHeaderBar.SuspendLayout();
            pnlGridCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridDispatch).BeginInit();
            pnlSummary.SuspendLayout();
            pnlGridHeaderBar.SuspendLayout();
            pnlGridActionsFlow.SuspendLayout();
            SuspendLayout();
            // 
            // tblMasterContent
            // 
            tblMasterContent.ColumnCount = 2;
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480F));
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMasterContent.Controls.Add(pnlForm, 0, 0);
            tblMasterContent.Controls.Add(pnlGridCard, 1, 0);
            tblMasterContent.Dock = DockStyle.Fill;
            tblMasterContent.Location = new Point(16, 16);
            tblMasterContent.Name = "tblMasterContent";
            tblMasterContent.RowCount = 1;
            tblMasterContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblMasterContent.Size = new Size(1388, 808);
            tblMasterContent.TabIndex = 1;
            // 
            // pnlForm
            // 
            pnlForm.BackColor = Color.White;
            pnlForm.Controls.Add(pnlFormBody);
            pnlForm.Controls.Add(pnlFormActionsBar);
            pnlForm.Controls.Add(pnlFormHeaderBar);
            pnlForm.Dock = DockStyle.Fill;
            pnlForm.Location = new Point(0, 0);
            pnlForm.Margin = new Padding(0, 0, 12, 0);
            pnlForm.Name = "pnlForm";
            pnlForm.Size = new Size(468, 808);
            pnlForm.TabIndex = 0;
            // 
            // pnlFormBody
            // 
            pnlFormBody.AutoScroll = true;
            pnlFormBody.BackColor = Color.White;
            pnlFormBody.Controls.Add(pnlStatusSpacer);
            pnlFormBody.Controls.Add(cbStatus);
            pnlFormBody.Controls.Add(lblSt);
            pnlFormBody.Controls.Add(tblDateTime);
            pnlFormBody.Controls.Add(lblAidesSummary);
            pnlFormBody.Controls.Add(flowAidesModule);
            pnlFormBody.Controls.Add(pnlAidesSearchRow);
            pnlFormBody.Controls.Add(lblAi);
            pnlFormBody.Controls.Add(cbDriver);
            pnlFormBody.Controls.Add(lblDr);
            pnlFormBody.Controls.Add(cbTruck);
            pnlFormBody.Controls.Add(lblTr);
            pnlFormBody.Controls.Add(pnlRec);
            pnlFormBody.Controls.Add(txtTotalCrates);
            pnlFormBody.Controls.Add(lblTotalCrates);
            pnlFormBody.Controls.Add(tblCrates);
            pnlFormBody.Controls.Add(tblCargoPeak);
            pnlFormBody.Controls.Add(lblDestHint);
            pnlFormBody.Controls.Add(cbDestination);
            pnlFormBody.Controls.Add(lblDest);
            pnlFormBody.Controls.Add(cbClient);
            pnlFormBody.Controls.Add(lblClient);
            pnlFormBody.Controls.Add(txtDispatchNumber);
            pnlFormBody.Controls.Add(lblDispatchNumber);
            pnlFormBody.Dock = DockStyle.Fill;
            pnlFormBody.Location = new Point(0, 38);
            pnlFormBody.Name = "pnlFormBody";
            pnlFormBody.Padding = new Padding(16, 4, 16, 8);
            pnlFormBody.Size = new Size(468, 620);
            pnlFormBody.TabIndex = 1;
            // 
            // pnlStatusSpacer
            // 
            pnlStatusSpacer.Dock = DockStyle.Top;
            pnlStatusSpacer.Location = new Point(16, 822);
            pnlStatusSpacer.Name = "pnlStatusSpacer";
            pnlStatusSpacer.Size = new Size(415, 10);
            pnlStatusSpacer.TabIndex = 18;
            // 
            // cbStatus
            // 
            cbStatus.Dock = DockStyle.Top;
            cbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStatus.Font = new Font("Segoe UI", 9.5F);
            cbStatus.FormattingEnabled = true;
            cbStatus.Items.AddRange(new object[] { "Pending", "In Transit", "Delayed", "Completed", "Cancelled" });
            cbStatus.Location = new Point(16, 793);
            cbStatus.Margin = new Padding(0, 0, 0, 8);
            cbStatus.Name = "cbStatus";
            cbStatus.Size = new Size(415, 29);
            cbStatus.TabIndex = 17;
            // 
            // lblSt
            // 
            lblSt.AutoSize = true;
            lblSt.Dock = DockStyle.Top;
            lblSt.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblSt.ForeColor = Color.FromArgb(30, 41, 59);
            lblSt.Location = new Point(16, 766);
            lblSt.Name = "lblSt";
            lblSt.Padding = new Padding(0, 4, 0, 2);
            lblSt.Size = new Size(126, 27);
            lblSt.TabIndex = 16;
            lblSt.Text = "Dispatch Status:";
            // 
            // tblDateTime
            // 
            tblDateTime.ColumnCount = 2;
            tblDateTime.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblDateTime.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblDateTime.Controls.Add(lblDd, 0, 0);
            tblDateTime.Controls.Add(lblDepartureTime, 1, 0);
            tblDateTime.Controls.Add(dtpDispatchDate, 0, 1);
            tblDateTime.Controls.Add(pnlTimeRow, 1, 1);
            tblDateTime.Dock = DockStyle.Top;
            tblDateTime.Location = new Point(16, 710);
            tblDateTime.Margin = new Padding(0, 4, 0, 4);
            tblDateTime.Name = "tblDateTime";
            tblDateTime.RowCount = 2;
            tblDateTime.RowStyles.Add(new RowStyle());
            tblDateTime.RowStyles.Add(new RowStyle());
            tblDateTime.Size = new Size(415, 56);
            tblDateTime.TabIndex = 15;
            // 
            // lblDd
            // 
            lblDd.AutoSize = true;
            lblDd.Dock = DockStyle.Fill;
            lblDd.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDd.ForeColor = Color.FromArgb(30, 41, 59);
            lblDd.Location = new Point(0, 0);
            lblDd.Margin = new Padding(0, 0, 4, 2);
            lblDd.Name = "lblDd";
            lblDd.Size = new Size(203, 21);
            lblDd.TabIndex = 0;
            lblDd.Text = "Dispatch Date:";
            // 
            // lblDepartureTime
            // 
            lblDepartureTime.AutoSize = true;
            lblDepartureTime.Dock = DockStyle.Fill;
            lblDepartureTime.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDepartureTime.ForeColor = Color.FromArgb(30, 41, 59);
            lblDepartureTime.Location = new Point(211, 0);
            lblDepartureTime.Margin = new Padding(4, 0, 0, 2);
            lblDepartureTime.Name = "lblDepartureTime";
            lblDepartureTime.Size = new Size(204, 21);
            lblDepartureTime.TabIndex = 1;
            lblDepartureTime.Text = "Departure Time:";
            // 
            // dtpDispatchDate
            // 
            dtpDispatchDate.Dock = DockStyle.Fill;
            dtpDispatchDate.Font = new Font("Segoe UI", 9.5F);
            dtpDispatchDate.Format = DateTimePickerFormat.Short;
            dtpDispatchDate.Location = new Point(0, 25);
            dtpDispatchDate.Margin = new Padding(0, 2, 4, 6);
            dtpDispatchDate.Name = "dtpDispatchDate";
            dtpDispatchDate.Size = new Size(203, 29);
            dtpDispatchDate.TabIndex = 2;
            // 
            // pnlTimeRow
            // 
            pnlTimeRow.Controls.Add(cbDepartureTime);
            pnlTimeRow.Controls.Add(btnPickAnalogTime);
            pnlTimeRow.Dock = DockStyle.Fill;
            pnlTimeRow.Location = new Point(211, 25);
            pnlTimeRow.Margin = new Padding(4, 2, 0, 6);
            pnlTimeRow.Name = "pnlTimeRow";
            pnlTimeRow.Size = new Size(204, 29);
            pnlTimeRow.TabIndex = 3;
            // 
            // cbDepartureTime
            // 
            cbDepartureTime.Dock = DockStyle.Fill;
            cbDepartureTime.Font = new Font("Segoe UI", 9.5F);
            cbDepartureTime.FormattingEnabled = true;
            cbDepartureTime.Items.AddRange(new object[] { "06:00 AM", "08:00 AM", "10:00 AM", "01:00 PM", "04:00 PM", "08:00 PM" });
            cbDepartureTime.Location = new Point(0, 0);
            cbDepartureTime.Margin = new Padding(0, 0, 4, 0);
            cbDepartureTime.Name = "cbDepartureTime";
            cbDepartureTime.Size = new Size(168, 29);
            cbDepartureTime.TabIndex = 0;
            cbDepartureTime.Text = "08:00 AM";
            // 
            // btnPickAnalogTime
            // 
            btnPickAnalogTime.Cursor = Cursors.Hand;
            btnPickAnalogTime.Dock = DockStyle.Right;
            btnPickAnalogTime.Font = new Font("Segoe UI", 9F);
            btnPickAnalogTime.Location = new Point(168, 0);
            btnPickAnalogTime.Margin = new Padding(4, 0, 0, 0);
            btnPickAnalogTime.Name = "btnPickAnalogTime";
            btnPickAnalogTime.Size = new Size(36, 29);
            btnPickAnalogTime.TabIndex = 1;
            btnPickAnalogTime.Text = "🕒";
            btnPickAnalogTime.UseVisualStyleBackColor = true;
            // 
            // lblAidesSummary
            // 
            lblAidesSummary.AutoSize = true;
            lblAidesSummary.Dock = DockStyle.Top;
            lblAidesSummary.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblAidesSummary.ForeColor = Color.FromArgb(22, 101, 52);
            lblAidesSummary.Location = new Point(16, 684);
            lblAidesSummary.Margin = new Padding(0, 2, 0, 8);
            lblAidesSummary.Name = "lblAidesSummary";
            lblAidesSummary.Padding = new Padding(0, 2, 0, 4);
            lblAidesSummary.Size = new Size(135, 26);
            lblAidesSummary.TabIndex = 14;
            lblAidesSummary.Text = "✓ 0 Aides Selected";
            // 
            // flowAidesModule
            // 
            flowAidesModule.AutoScroll = true;
            flowAidesModule.BackColor = Color.FromArgb(248, 250, 252);
            flowAidesModule.BorderStyle = BorderStyle.FixedSingle;
            flowAidesModule.Dock = DockStyle.Top;
            flowAidesModule.Location = new Point(16, 592);
            flowAidesModule.Margin = new Padding(0, 0, 0, 4);
            flowAidesModule.Name = "flowAidesModule";
            flowAidesModule.Padding = new Padding(6);
            flowAidesModule.Size = new Size(415, 92);
            flowAidesModule.TabIndex = 13;
            // 
            // pnlAidesSearchRow
            // 
            pnlAidesSearchRow.Controls.Add(txtSearchAides);
            pnlAidesSearchRow.Controls.Add(btnClearAides);
            pnlAidesSearchRow.Dock = DockStyle.Top;
            pnlAidesSearchRow.Location = new Point(16, 564);
            pnlAidesSearchRow.Margin = new Padding(0, 2, 0, 4);
            pnlAidesSearchRow.Name = "pnlAidesSearchRow";
            pnlAidesSearchRow.Size = new Size(415, 28);
            pnlAidesSearchRow.TabIndex = 12;
            // 
            // txtSearchAides
            // 
            txtSearchAides.Dock = DockStyle.Fill;
            txtSearchAides.Font = new Font("Segoe UI", 9F);
            txtSearchAides.Location = new Point(0, 0);
            txtSearchAides.Margin = new Padding(0, 0, 4, 0);
            txtSearchAides.MaxLength = 100;
            txtSearchAides.Name = "txtSearchAides";
            txtSearchAides.PlaceholderText = "🔍 Type aide name to filter...";
            txtSearchAides.Size = new Size(383, 27);
            txtSearchAides.TabIndex = 0;
            // 
            // btnClearAides
            // 
            btnClearAides.BackColor = Color.FromArgb(241, 245, 249);
            btnClearAides.Cursor = Cursors.Hand;
            btnClearAides.Dock = DockStyle.Right;
            btnClearAides.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearAides.FlatStyle = FlatStyle.Flat;
            btnClearAides.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnClearAides.ForeColor = Color.FromArgb(100, 116, 139);
            btnClearAides.Location = new Point(383, 0);
            btnClearAides.Margin = new Padding(4, 0, 0, 0);
            btnClearAides.Name = "btnClearAides";
            btnClearAides.Size = new Size(32, 28);
            btnClearAides.TabIndex = 1;
            btnClearAides.Text = "✖";
            btnClearAides.UseVisualStyleBackColor = false;
            // 
            // lblAi
            // 
            lblAi.AutoSize = true;
            lblAi.Dock = DockStyle.Top;
            lblAi.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblAi.ForeColor = Color.FromArgb(30, 41, 59);
            lblAi.Location = new Point(16, 537);
            lblAi.Name = "lblAi";
            lblAi.Padding = new Padding(0, 4, 0, 2);
            lblAi.Size = new Size(315, 27);
            lblAi.TabIndex = 11;
            lblAi.Text = "Assigned Helper/Aides (Select 1 or more):";
            // 
            // cbDriver
            // 
            cbDriver.Dock = DockStyle.Top;
            cbDriver.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDriver.Font = new Font("Segoe UI", 9.5F);
            cbDriver.FormattingEnabled = true;
            cbDriver.Location = new Point(16, 508);
            cbDriver.Margin = new Padding(0, 0, 0, 8);
            cbDriver.Name = "cbDriver";
            cbDriver.Size = new Size(415, 29);
            cbDriver.TabIndex = 10;
            // 
            // lblDr
            // 
            lblDr.AutoSize = true;
            lblDr.Dock = DockStyle.Top;
            lblDr.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDr.ForeColor = Color.FromArgb(30, 41, 59);
            lblDr.Location = new Point(16, 481);
            lblDr.Name = "lblDr";
            lblDr.Padding = new Padding(0, 4, 0, 2);
            lblDr.Size = new Size(130, 27);
            lblDr.TabIndex = 9;
            lblDr.Text = "Assigned Driver:";
            // 
            // cbTruck
            // 
            cbTruck.Dock = DockStyle.Top;
            cbTruck.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTruck.Font = new Font("Segoe UI", 9.5F);
            cbTruck.FormattingEnabled = true;
            cbTruck.Location = new Point(16, 452);
            cbTruck.Margin = new Padding(0, 0, 0, 8);
            cbTruck.Name = "cbTruck";
            cbTruck.Size = new Size(415, 29);
            cbTruck.TabIndex = 8;
            // 
            // lblTr
            // 
            lblTr.AutoSize = true;
            lblTr.Dock = DockStyle.Top;
            lblTr.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblTr.ForeColor = Color.FromArgb(30, 41, 59);
            lblTr.Location = new Point(16, 425);
            lblTr.Name = "lblTr";
            lblTr.Padding = new Padding(0, 4, 0, 2);
            lblTr.Size = new Size(125, 27);
            lblTr.TabIndex = 7;
            lblTr.Text = "Assigned Truck:";
            // 
            // pnlRec
            // 
            pnlRec.BackColor = Color.FromArgb(240, 253, 244);
            pnlRec.BorderStyle = BorderStyle.FixedSingle;
            pnlRec.Controls.Add(lblRec);
            pnlRec.Dock = DockStyle.Top;
            pnlRec.Location = new Point(16, 367);
            pnlRec.Margin = new Padding(0, 0, 0, 8);
            pnlRec.Name = "pnlRec";
            pnlRec.Padding = new Padding(10, 6, 10, 6);
            pnlRec.Size = new Size(415, 58);
            pnlRec.TabIndex = 6;
            // 
            // lblRec
            // 
            lblRec.AutoEllipsis = true;
            lblRec.Dock = DockStyle.Fill;
            lblRec.Font = new Font("Segoe UI", 9F);
            lblRec.ForeColor = Color.FromArgb(22, 101, 52);
            lblRec.Location = new Point(10, 6);
            lblRec.Name = "lblRec";
            lblRec.Size = new Size(393, 44);
            lblRec.TabIndex = 0;
            lblRec.Text = "Truck Recommendation...";
            // 
            // txtTotalCrates
            // 
            txtTotalCrates.Dock = DockStyle.Top;
            txtTotalCrates.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            txtTotalCrates.Location = new Point(16, 338);
            txtTotalCrates.Margin = new Padding(0, 0, 0, 8);
            txtTotalCrates.Name = "txtTotalCrates";
            txtTotalCrates.ReadOnly = true;
            txtTotalCrates.Size = new Size(415, 29);
            txtTotalCrates.TabIndex = 5;
            txtTotalCrates.Text = "150 Crates";
            // 
            // lblTotalCrates
            // 
            lblTotalCrates.AutoSize = true;
            lblTotalCrates.Dock = DockStyle.Top;
            lblTotalCrates.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblTotalCrates.ForeColor = Color.FromArgb(30, 41, 59);
            lblTotalCrates.Location = new Point(16, 311);
            lblTotalCrates.Name = "lblTotalCrates";
            lblTotalCrates.Padding = new Padding(0, 4, 0, 2);
            lblTotalCrates.Size = new Size(179, 27);
            lblTotalCrates.TabIndex = 4;
            lblTotalCrates.Text = "Total Combined Crates:";
            // 
            // tblCrates
            // 
            tblCrates.ColumnCount = 2;
            tblCrates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrates.Controls.Add(lblGCrate, 0, 0);
            tblCrates.Controls.Add(lblECrate, 1, 0);
            tblCrates.Controls.Add(numGCrate, 0, 1);
            tblCrates.Controls.Add(numECrate, 1, 1);
            tblCrates.Dock = DockStyle.Top;
            tblCrates.Location = new Point(16, 255);
            tblCrates.Margin = new Padding(0, 4, 0, 4);
            tblCrates.Name = "tblCrates";
            tblCrates.RowCount = 2;
            tblCrates.RowStyles.Add(new RowStyle());
            tblCrates.RowStyles.Add(new RowStyle());
            tblCrates.Size = new Size(415, 56);
            tblCrates.TabIndex = 3;
            // 
            // lblGCrate
            // 
            lblGCrate.AutoSize = true;
            lblGCrate.Dock = DockStyle.Fill;
            lblGCrate.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblGCrate.ForeColor = Color.FromArgb(30, 41, 59);
            lblGCrate.Location = new Point(0, 0);
            lblGCrate.Margin = new Padding(0, 0, 4, 2);
            lblGCrate.Name = "lblGCrate";
            lblGCrate.Size = new Size(203, 21);
            lblGCrate.TabIndex = 0;
            lblGCrate.Text = "G-Crates:";
            // 
            // lblECrate
            // 
            lblECrate.AutoSize = true;
            lblECrate.Dock = DockStyle.Fill;
            lblECrate.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblECrate.ForeColor = Color.FromArgb(30, 41, 59);
            lblECrate.Location = new Point(211, 0);
            lblECrate.Margin = new Padding(4, 0, 0, 2);
            lblECrate.Name = "lblECrate";
            lblECrate.Size = new Size(204, 21);
            lblECrate.TabIndex = 1;
            lblECrate.Text = "E-Crates:";
            // 
            // numGCrate
            // 
            numGCrate.Dock = DockStyle.Fill;
            numGCrate.Font = new Font("Segoe UI", 9.5F);
            numGCrate.Location = new Point(0, 25);
            numGCrate.Margin = new Padding(0, 2, 4, 6);
            numGCrate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            numGCrate.Name = "numGCrate";
            numGCrate.Size = new Size(203, 29);
            numGCrate.TabIndex = 2;
            numGCrate.Value = new decimal(new int[] { 150, 0, 0, 0 });
            // 
            // numECrate
            // 
            numECrate.Dock = DockStyle.Fill;
            numECrate.Font = new Font("Segoe UI", 9.5F);
            numECrate.Location = new Point(211, 25);
            numECrate.Margin = new Padding(4, 2, 0, 6);
            numECrate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            numECrate.Name = "numECrate";
            numECrate.Size = new Size(204, 29);
            numECrate.TabIndex = 3;
            // 
            // tblCargoPeak
            // 
            tblCargoPeak.ColumnCount = 2;
            tblCargoPeak.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCargoPeak.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCargoPeak.Controls.Add(lblCargoType, 0, 0);
            tblCargoPeak.Controls.Add(lblPeakHeader, 1, 0);
            tblCargoPeak.Controls.Add(cbCargoType, 0, 1);
            tblCargoPeak.Controls.Add(chkIsPeak, 1, 1);
            tblCargoPeak.Dock = DockStyle.Top;
            tblCargoPeak.Location = new Point(16, 199);
            tblCargoPeak.Margin = new Padding(0, 4, 0, 8);
            tblCargoPeak.Name = "tblCargoPeak";
            tblCargoPeak.RowCount = 2;
            tblCargoPeak.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tblCargoPeak.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblCargoPeak.Size = new Size(415, 56);
            tblCargoPeak.TabIndex = 8;
            // 
            // lblCargoType
            // 
            lblCargoType.AutoSize = true;
            lblCargoType.Dock = DockStyle.Fill;
            lblCargoType.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblCargoType.ForeColor = Color.FromArgb(30, 41, 59);
            lblCargoType.Location = new Point(0, 0);
            lblCargoType.Margin = new Padding(0, 0, 4, 2);
            lblCargoType.Name = "lblCargoType";
            lblCargoType.Size = new Size(203, 20);
            lblCargoType.TabIndex = 0;
            lblCargoType.Text = "Cargo / Temp Type:";
            // 
            // lblPeakHeader
            // 
            lblPeakHeader.AutoSize = true;
            lblPeakHeader.Dock = DockStyle.Fill;
            lblPeakHeader.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblPeakHeader.ForeColor = Color.FromArgb(30, 41, 59);
            lblPeakHeader.Location = new Point(211, 0);
            lblPeakHeader.Margin = new Padding(4, 0, 0, 2);
            lblPeakHeader.Name = "lblPeakHeader";
            lblPeakHeader.Size = new Size(204, 20);
            lblPeakHeader.TabIndex = 1;
            lblPeakHeader.Text = "Tariff Season:";
            // 
            // cbCargoType
            // 
            cbCargoType.Dock = DockStyle.Fill;
            cbCargoType.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCargoType.Font = new Font("Segoe UI", 9.5F);
            cbCargoType.FormattingEnabled = true;
            cbCargoType.Items.AddRange(new object[] { "Cold / Chilled", "Dry Goods", "Dry and Chilled Mix" });
            cbCargoType.Location = new Point(0, 22);
            cbCargoType.Margin = new Padding(0, 0, 4, 0);
            cbCargoType.Name = "cbCargoType";
            cbCargoType.Size = new Size(203, 29);
            cbCargoType.TabIndex = 2;
            // 
            // chkIsPeak
            // 
            chkIsPeak.AutoSize = true;
            chkIsPeak.Dock = DockStyle.Fill;
            chkIsPeak.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            chkIsPeak.ForeColor = Color.FromArgb(30, 41, 59);
            chkIsPeak.Location = new Point(211, 22);
            chkIsPeak.Margin = new Padding(4, 0, 0, 0);
            chkIsPeak.Name = "chkIsPeak";
            chkIsPeak.Size = new Size(204, 34);
            chkIsPeak.TabIndex = 3;
            chkIsPeak.Text = "Is Peak Season Rate";
            chkIsPeak.UseVisualStyleBackColor = true;
            // 
            // lblDestHint
            // 
            lblDestHint.AutoSize = true;
            lblDestHint.Dock = DockStyle.Top;
            lblDestHint.Font = new Font("Segoe UI", 8.25F);
            lblDestHint.ForeColor = Color.FromArgb(100, 116, 139);
            lblDestHint.Location = new Point(16, 172);
            lblDestHint.Name = "lblDestHint";
            lblDestHint.Padding = new Padding(0, 2, 0, 6);
            lblDestHint.Size = new Size(466, 27);
            lblDestHint.TabIndex = 7;
            lblDestHint.Text = "💡 Type or select branch/drop. Check Multi-Drop below for multiple stops.";
            // 
            // cbDestination
            // 
            cbDestination.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbDestination.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbDestination.Dock = DockStyle.Top;
            cbDestination.Font = new Font("Segoe UI", 9.5F);
            cbDestination.FormattingEnabled = true;
            cbDestination.Location = new Point(16, 143);
            cbDestination.Margin = new Padding(0, 0, 0, 2);
            cbDestination.Name = "cbDestination";
            cbDestination.Size = new Size(415, 29);
            cbDestination.TabIndex = 6;
            // 
            // lblDest
            // 
            lblDest.AutoSize = true;
            lblDest.Dock = DockStyle.Top;
            lblDest.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDest.ForeColor = Color.FromArgb(30, 41, 59);
            lblDest.Location = new Point(16, 116);
            lblDest.Name = "lblDest";
            lblDest.Padding = new Padding(0, 4, 0, 2);
            lblDest.Size = new Size(161, 27);
            lblDest.TabIndex = 5;
            lblDest.Text = "Delivery Destination:";
            // 
            // cbClient
            // 
            cbClient.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbClient.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbClient.Dock = DockStyle.Top;
            cbClient.Font = new Font("Segoe UI", 9.5F);
            cbClient.FormattingEnabled = true;
            cbClient.Location = new Point(16, 87);
            cbClient.Margin = new Padding(0, 0, 0, 8);
            cbClient.Name = "cbClient";
            cbClient.Size = new Size(415, 29);
            cbClient.TabIndex = 4;
            // 
            // lblClient
            // 
            lblClient.AutoSize = true;
            lblClient.Dock = DockStyle.Top;
            lblClient.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblClient.ForeColor = Color.FromArgb(30, 41, 59);
            lblClient.Location = new Point(16, 60);
            lblClient.Name = "lblClient";
            lblClient.Padding = new Padding(0, 4, 0, 2);
            lblClient.Size = new Size(129, 27);
            lblClient.TabIndex = 3;
            lblClient.Text = "Client Company:";
            // 
            // txtDispatchNumber
            // 
            txtDispatchNumber.Dock = DockStyle.Top;
            txtDispatchNumber.Font = new Font("Segoe UI", 9.5F);
            txtDispatchNumber.Location = new Point(16, 31);
            txtDispatchNumber.Margin = new Padding(0, 0, 0, 8);
            txtDispatchNumber.MaxLength = 50;
            txtDispatchNumber.Name = "txtDispatchNumber";
            txtDispatchNumber.PlaceholderText = "e.g. REF-2026-001 (Leave empty to auto-generate)";
            txtDispatchNumber.Size = new Size(415, 29);
            txtDispatchNumber.TabIndex = 2;
            // 
            // lblDispatchNumber
            // 
            lblDispatchNumber.AutoSize = true;
            lblDispatchNumber.Dock = DockStyle.Top;
            lblDispatchNumber.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDispatchNumber.ForeColor = Color.FromArgb(30, 41, 59);
            lblDispatchNumber.Location = new Point(16, 4);
            lblDispatchNumber.Name = "lblDispatchNumber";
            lblDispatchNumber.Padding = new Padding(0, 4, 0, 2);
            lblDispatchNumber.Size = new Size(227, 27);
            lblDispatchNumber.TabIndex = 1;
            lblDispatchNumber.Text = "Order Reference / Dispatch #:";
            // 
            // pnlFormActionsBar
            // 
            pnlFormActionsBar.BackColor = Color.White;
            pnlFormActionsBar.Controls.Add(btnClear);
            pnlFormActionsBar.Controls.Add(pnlUpdateSpacer);
            pnlFormActionsBar.Controls.Add(tblUpdateDelete);
            pnlFormActionsBar.Controls.Add(pnlSaveSpacer);
            pnlFormActionsBar.Controls.Add(btnSave);
            pnlFormActionsBar.Dock = DockStyle.Bottom;
            pnlFormActionsBar.Location = new Point(0, 658);
            pnlFormActionsBar.Name = "pnlFormActionsBar";
            pnlFormActionsBar.Padding = new Padding(16, 4, 16, 8);
            pnlFormActionsBar.Size = new Size(468, 150);
            pnlFormActionsBar.TabIndex = 2;
            // 
            // btnClear
            // 
            btnClear.Cursor = Cursors.Hand;
            btnClear.Dock = DockStyle.Top;
            btnClear.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnClear.Location = new Point(16, 102);
            btnClear.Margin = new Padding(0);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(436, 36);
            btnClear.TabIndex = 23;
            btnClear.Text = "Clear Form";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += BtnClear_Click;
            // 
            // pnlUpdateSpacer
            // 
            pnlUpdateSpacer.Dock = DockStyle.Top;
            pnlUpdateSpacer.Location = new Point(16, 94);
            pnlUpdateSpacer.Name = "pnlUpdateSpacer";
            pnlUpdateSpacer.Size = new Size(436, 8);
            pnlUpdateSpacer.TabIndex = 22;
            // 
            // tblUpdateDelete
            // 
            tblUpdateDelete.ColumnCount = 2;
            tblUpdateDelete.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblUpdateDelete.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblUpdateDelete.Controls.Add(btnUpdate, 0, 0);
            tblUpdateDelete.Controls.Add(btnDelete, 1, 0);
            tblUpdateDelete.Dock = DockStyle.Top;
            tblUpdateDelete.Location = new Point(16, 54);
            tblUpdateDelete.Margin = new Padding(0);
            tblUpdateDelete.Name = "tblUpdateDelete";
            tblUpdateDelete.RowCount = 1;
            tblUpdateDelete.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblUpdateDelete.Size = new Size(436, 40);
            tblUpdateDelete.TabIndex = 21;
            // 
            // btnUpdate
            // 
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.Dock = DockStyle.Fill;
            btnUpdate.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnUpdate.Location = new Point(0, 0);
            btnUpdate.Margin = new Padding(0, 0, 4, 0);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(214, 40);
            btnUpdate.TabIndex = 0;
            btnUpdate.Text = "Update";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += BtnUpdate_Click;
            // 
            // btnDelete
            // 
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.Dock = DockStyle.Fill;
            btnDelete.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnDelete.Location = new Point(222, 0);
            btnDelete.Margin = new Padding(4, 0, 0, 0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(214, 40);
            btnDelete.TabIndex = 1;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
            // 
            // pnlSaveSpacer
            // 
            pnlSaveSpacer.Dock = DockStyle.Top;
            pnlSaveSpacer.Location = new Point(16, 46);
            pnlSaveSpacer.Name = "pnlSaveSpacer";
            pnlSaveSpacer.Size = new Size(436, 8);
            pnlSaveSpacer.TabIndex = 20;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(185, 28, 28);
            btnSave.Cursor = Cursors.Hand;
            btnSave.Dock = DockStyle.Top;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(16, 4);
            btnSave.Margin = new Padding(0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(436, 42);
            btnSave.TabIndex = 19;
            btnSave.Text = "Save Dispatch Order";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += BtnSave_Click;
            // 
            // pnlFormHeaderBar
            // 
            pnlFormHeaderBar.BackColor = Color.FromArgb(248, 250, 252);
            pnlFormHeaderBar.Controls.Add(lblFormHeader);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);
            pnlFormHeaderBar.Dock = DockStyle.Top;
            pnlFormHeaderBar.Location = new Point(0, 0);
            pnlFormHeaderBar.Name = "pnlFormHeaderBar";
            pnlFormHeaderBar.Padding = new Padding(12, 6, 12, 6);
            pnlFormHeaderBar.Size = new Size(468, 38);
            pnlFormHeaderBar.TabIndex = 0;
            // 
            // lblFormHeader
            // 
            lblFormHeader.AutoSize = true;
            lblFormHeader.Dock = DockStyle.Left;
            lblFormHeader.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblFormHeader.ForeColor = Color.FromArgb(15, 23, 42);
            lblFormHeader.Location = new Point(12, 6);
            lblFormHeader.Name = "lblFormHeader";
            lblFormHeader.Size = new Size(210, 23);
            lblFormHeader.TabIndex = 0;
            lblFormHeader.Text = "📝 Dispatch Order Details";
            lblFormHeader.UseMnemonic = false;
            // 
            // btnCloseForm
            // 
            btnCloseForm.BackColor = Color.FromArgb(241, 245, 249);
            btnCloseForm.Cursor = Cursors.Hand;
            btnCloseForm.Dock = DockStyle.Right;
            btnCloseForm.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCloseForm.FlatStyle = FlatStyle.Flat;
            btnCloseForm.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnCloseForm.ForeColor = Color.FromArgb(100, 116, 139);
            btnCloseForm.Location = new Point(388, 6);
            btnCloseForm.Name = "btnCloseForm";
            btnCloseForm.Size = new Size(68, 26);
            btnCloseForm.TabIndex = 1;
            btnCloseForm.Text = "✕ Close";
            btnCloseForm.UseVisualStyleBackColor = false;
            // 
            // pnlGridCard
            // 
            pnlGridCard.BackColor = Color.White;
            pnlGridCard.Controls.Add(gridDispatch);
            pnlGridCard.Controls.Add(pnlSummary);
            pnlGridCard.Controls.Add(pnlGridHeaderBar);
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Location = new Point(480, 0);
            pnlGridCard.Margin = new Padding(0);
            pnlGridCard.MinimumSize = new Size(480, 300);
            pnlGridCard.Name = "pnlGridCard";
            pnlGridCard.Padding = new Padding(16);
            pnlGridCard.Size = new Size(908, 808);
            pnlGridCard.TabIndex = 1;
            // 
            // gridDispatch
            // 
            gridDispatch.AllowUserToAddRows = false;
            gridDispatch.AllowUserToDeleteRows = false;
            gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridDispatch.BackgroundColor = Color.White;
            gridDispatch.BorderStyle = BorderStyle.None;
            gridDispatch.ColumnHeadersHeight = 40;
            gridDispatch.Dock = DockStyle.Fill;
            gridDispatch.EnableHeadersVisualStyles = false;
            gridDispatch.Location = new Point(16, 90);
            gridDispatch.MultiSelect = false;
            gridDispatch.Name = "gridDispatch";
            gridDispatch.ReadOnly = true;
            gridDispatch.RowHeadersVisible = false;
            gridDispatch.RowHeadersWidth = 51;
            gridDispatch.RowTemplate.Height = 32;
            gridDispatch.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridDispatch.Size = new Size(876, 664);
            gridDispatch.TabIndex = 1;
            gridDispatch.SelectionChanged += GridDispatch_SelectionChanged;
            // 
            // pnlSummary
            // 
            pnlSummary.BackColor = Color.FromArgb(248, 250, 252);
            pnlSummary.Controls.Add(lblSummary);
            pnlSummary.Dock = DockStyle.Bottom;
            pnlSummary.Location = new Point(16, 754);
            pnlSummary.Name = "pnlSummary";
            pnlSummary.Padding = new Padding(12, 8, 12, 8);
            pnlSummary.Size = new Size(876, 38);
            pnlSummary.TabIndex = 2;
            // 
            // lblSummary
            // 
            lblSummary.AutoSize = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblSummary.ForeColor = Color.FromArgb(51, 65, 85);
            lblSummary.Location = new Point(12, 8);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(55, 21);
            lblSummary.TabIndex = 0;
            lblSummary.Text = "Ready";
            // 
            // pnlGridHeaderBar
            // 
            pnlGridHeaderBar.AutoSize = true;
            pnlGridHeaderBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlGridHeaderBar.Controls.Add(pnlGridActionsFlow);
            pnlGridHeaderBar.Controls.Add(lblGridHeader);
            pnlGridHeaderBar.Dock = DockStyle.Top;
            pnlGridHeaderBar.Location = new Point(16, 16);
            pnlGridHeaderBar.Name = "pnlGridHeaderBar";
            pnlGridHeaderBar.Padding = new Padding(0, 0, 0, 8);
            pnlGridHeaderBar.Size = new Size(876, 74);
            pnlGridHeaderBar.TabIndex = 0;
            // 
            // pnlGridActionsFlow
            // 
            pnlGridActionsFlow.AutoSize = true;
            pnlGridActionsFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlGridActionsFlow.Controls.Add(txtGridSearch);
            pnlGridActionsFlow.Controls.Add(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(btnPrint);
            pnlGridActionsFlow.Dock = DockStyle.Top;
            pnlGridActionsFlow.Location = new Point(0, 33);
            pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            pnlGridActionsFlow.Size = new Size(876, 33);
            pnlGridActionsFlow.TabIndex = 1;
            // 
            // txtGridSearch
            // 
            txtGridSearch.Font = new Font("Segoe UI", 9.5F);
            txtGridSearch.Location = new Point(0, 4);
            txtGridSearch.Margin = new Padding(0, 4, 4, 0);
            txtGridSearch.MaxLength = 100;
            txtGridSearch.Name = "txtGridSearch";
            txtGridSearch.PlaceholderText = "🔍 Search order #, client, destination, driver, truck...";
            txtGridSearch.Size = new Size(220, 29);
            txtGridSearch.TabIndex = 0;
            txtGridSearch.TextChanged += TxtGridSearch_TextChanged;
            // 
            // btnClearSearch
            // 
            btnClearSearch.BackColor = Color.FromArgb(241, 245, 249);
            btnClearSearch.Cursor = Cursors.Hand;
            btnClearSearch.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearSearch.FlatStyle = FlatStyle.Flat;
            btnClearSearch.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnClearSearch.ForeColor = Color.FromArgb(100, 116, 139);
            btnClearSearch.Location = new Point(228, 4);
            btnClearSearch.Margin = new Padding(4, 4, 8, 0);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(28, 26);
            btnClearSearch.TabIndex = 1;
            btnClearSearch.Text = "✖";
            btnClearSearch.UseVisualStyleBackColor = false;
            btnClearSearch.Click += BtnClearSearch_Click;
            // 
            // btnPrint
            // 
            btnPrint.BackColor = Color.FromArgb(71, 85, 105);
            btnPrint.Cursor = Cursors.Hand;
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.FlatStyle = FlatStyle.Flat;
            btnPrint.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnPrint.ForeColor = Color.White;
            btnPrint.Location = new Point(268, 2);
            btnPrint.Margin = new Padding(4, 2, 0, 0);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(160, 30);
            btnPrint.TabIndex = 2;
            btnPrint.Text = "🖨️ Print Report";
            btnPrint.UseVisualStyleBackColor = false;
            btnPrint.Click += BtnPrint_Click;
            // 
            // lblGridHeader
            // 
            lblGridHeader.AutoSize = true;
            lblGridHeader.Dock = DockStyle.Top;
            lblGridHeader.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblGridHeader.ForeColor = Color.FromArgb(30, 41, 59);
            lblGridHeader.Location = new Point(0, 0);
            lblGridHeader.Name = "lblGridHeader";
            lblGridHeader.Padding = new Padding(0, 0, 0, 8);
            lblGridHeader.Size = new Size(279, 33);
            lblGridHeader.TabIndex = 0;
            lblGridHeader.Text = "Delivery & Dispatch Orders List";
            lblGridHeader.UseMnemonic = false;
            // 
            // DispatchManagementView
            // 
            BackColor = Color.FromArgb(241, 245, 249);
            Controls.Add(tblMasterContent);
            Font = new Font("Segoe UI", 10F);
            Name = "DispatchManagementView";
            Padding = new Padding(16);
            Size = new Size(1420, 840);
            tblMasterContent.ResumeLayout(false);
            pnlForm.ResumeLayout(false);
            pnlFormBody.ResumeLayout(false);
            pnlFormBody.PerformLayout();
            tblDateTime.ResumeLayout(false);
            tblDateTime.PerformLayout();
            pnlTimeRow.ResumeLayout(false);
            pnlAidesSearchRow.ResumeLayout(false);
            pnlAidesSearchRow.PerformLayout();
            pnlRec.ResumeLayout(false);
            tblCrates.ResumeLayout(false);
            tblCrates.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numGCrate).EndInit();
            ((System.ComponentModel.ISupportInitialize)numECrate).EndInit();
            tblCargoPeak.ResumeLayout(false);
            tblCargoPeak.PerformLayout();
            pnlFormActionsBar.ResumeLayout(false);
            tblUpdateDelete.ResumeLayout(false);
            pnlFormHeaderBar.ResumeLayout(false);
            pnlFormHeaderBar.PerformLayout();
            pnlGridCard.ResumeLayout(false);
            pnlGridCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridDispatch).EndInit();
            pnlSummary.ResumeLayout(false);
            pnlSummary.PerformLayout();
            pnlGridHeaderBar.ResumeLayout(false);
            pnlGridHeaderBar.PerformLayout();
            pnlGridActionsFlow.ResumeLayout(false);
            pnlGridActionsFlow.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tblMasterContent;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Panel pnlFormHeaderBar;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.Button btnCloseForm;
        private System.Windows.Forms.Panel pnlFormActionsBar;
        private System.Windows.Forms.Panel pnlFormBody;
        private System.Windows.Forms.Label lblDispatchNumber;
        private System.Windows.Forms.TextBox txtDispatchNumber;
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.ComboBox cbClient;
        private System.Windows.Forms.Label lblDest;
        private System.Windows.Forms.ComboBox cbDestination;
        private System.Windows.Forms.Label lblDestHint;
        private System.Windows.Forms.TableLayoutPanel tblCargoPeak;
        private System.Windows.Forms.Label lblCargoType;
        private System.Windows.Forms.Label lblPeakHeader;
        private System.Windows.Forms.ComboBox cbCargoType;
        private System.Windows.Forms.CheckBox chkIsPeak;
        private System.Windows.Forms.TableLayoutPanel tblCrates;
        private System.Windows.Forms.Label lblGCrate;
        private System.Windows.Forms.NumericUpDown numGCrate;
        private System.Windows.Forms.Label lblECrate;
        private System.Windows.Forms.NumericUpDown numECrate;
        private System.Windows.Forms.Label lblTotalCrates;
        private System.Windows.Forms.TextBox txtTotalCrates;
        private System.Windows.Forms.Panel pnlRec;
        private System.Windows.Forms.Label lblRec;
        private System.Windows.Forms.Label lblTr;
        private System.Windows.Forms.ComboBox cbTruck;
        private System.Windows.Forms.Label lblDr;
        private System.Windows.Forms.ComboBox cbDriver;
        private System.Windows.Forms.Label lblAi;
        private System.Windows.Forms.Panel pnlAidesSearchRow;
        private System.Windows.Forms.TextBox txtSearchAides;
        private System.Windows.Forms.Button btnClearAides;
        private System.Windows.Forms.FlowLayoutPanel flowAidesModule;
        private System.Windows.Forms.Label lblAidesSummary;
        private System.Windows.Forms.TableLayoutPanel tblDateTime;
        private System.Windows.Forms.Label lblDd;
        private System.Windows.Forms.Label lblDepartureTime;
        private System.Windows.Forms.DateTimePicker dtpDispatchDate;
        private System.Windows.Forms.Panel pnlTimeRow;
        private System.Windows.Forms.ComboBox cbDepartureTime;
        private System.Windows.Forms.Button btnPickAnalogTime;
        private System.Windows.Forms.Label lblSt;
        private System.Windows.Forms.ComboBox cbStatus;
        private System.Windows.Forms.Panel pnlStatusSpacer;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Panel pnlSaveSpacer;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Panel pnlUpdateSpacer;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeaderBar;
        private System.Windows.Forms.Label lblGridHeader;
        private System.Windows.Forms.FlowLayoutPanel pnlGridActionsFlow;
        private System.Windows.Forms.TextBox txtGridSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.DataGridView gridDispatch;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
