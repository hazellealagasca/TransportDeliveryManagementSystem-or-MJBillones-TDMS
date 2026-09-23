namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class DispatchManagementView
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
            this.lblLs = new System.Windows.Forms.Label();
            this.cbLoadSchedule = new System.Windows.Forms.ComboBox();
            this.tblCrates = new System.Windows.Forms.TableLayoutPanel();
            this.lblGCrate = new System.Windows.Forms.Label();
            this.lblECrate = new System.Windows.Forms.Label();
            this.numGCrate = new System.Windows.Forms.NumericUpDown();
            this.numECrate = new System.Windows.Forms.NumericUpDown();
            this.lblTotalCrates = new System.Windows.Forms.Label();
            this.txtTotalCrates = new System.Windows.Forms.TextBox();
            this.pnlRec = new System.Windows.Forms.Panel();
            this.lblRec = new System.Windows.Forms.Label();
            this.lblTr = new System.Windows.Forms.Label();
            this.cbTruck = new System.Windows.Forms.ComboBox();
            this.lblDr = new System.Windows.Forms.Label();
            this.cbDriver = new System.Windows.Forms.ComboBox();
            this.lblAi = new System.Windows.Forms.Label();
            this.pnlAidesSearchRow = new System.Windows.Forms.Panel();
            this.txtSearchAides = new System.Windows.Forms.TextBox();
            this.btnClearAides = new System.Windows.Forms.Button();
            this.flowAidesModule = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAidesSummary = new System.Windows.Forms.Label();
            this.tblDateTime = new System.Windows.Forms.TableLayoutPanel();
            this.lblDd = new System.Windows.Forms.Label();
            this.lblDepartureTime = new System.Windows.Forms.Label();
            this.dtpDispatchDate = new System.Windows.Forms.DateTimePicker();
            this.pnlTimeRow = new System.Windows.Forms.Panel();
            this.cbDepartureTime = new System.Windows.Forms.ComboBox();
            this.btnPickAnalogTime = new System.Windows.Forms.Button();
            this.lblSt = new System.Windows.Forms.Label();
            this.cbStatus = new System.Windows.Forms.ComboBox();
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
            this.btnPrint = new System.Windows.Forms.Button();
            this.gridDispatch = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.tblCrates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGCrate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numECrate)).BeginInit();
            this.pnlRec.SuspendLayout();
            this.pnlAidesSearchRow.SuspendLayout();
            this.tblDateTime.SuspendLayout();
            this.pnlTimeRow.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDispatch)).BeginInit();
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
            this.lblTitle.Size = new System.Drawing.Size(395, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Delivery & Dispatch Management";

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
            this.pnlForm.Controls.Add(this.btnClear);
            this.pnlForm.Controls.Add(this.tblUpdateDelete);
            this.pnlForm.Controls.Add(this.btnSave);
            this.pnlForm.Controls.Add(this.cbStatus);
            this.pnlForm.Controls.Add(this.lblSt);
            this.pnlForm.Controls.Add(this.tblDateTime);
            this.pnlForm.Controls.Add(this.lblAidesSummary);
            this.pnlForm.Controls.Add(this.flowAidesModule);
            this.pnlForm.Controls.Add(this.pnlAidesSearchRow);
            this.pnlForm.Controls.Add(this.lblAi);
            this.pnlForm.Controls.Add(this.cbDriver);
            this.pnlForm.Controls.Add(this.lblDr);
            this.pnlForm.Controls.Add(this.cbTruck);
            this.pnlForm.Controls.Add(this.lblTr);
            this.pnlForm.Controls.Add(this.pnlRec);
            this.pnlForm.Controls.Add(this.txtTotalCrates);
            this.pnlForm.Controls.Add(this.lblTotalCrates);
            this.pnlForm.Controls.Add(this.tblCrates);
            this.pnlForm.Controls.Add(this.cbLoadSchedule);
            this.pnlForm.Controls.Add(this.lblLs);
            this.pnlForm.Controls.Add(this.lblFormHeader);
            this.pnlForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlForm.Location = new System.Drawing.Point(0, 0);
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(16);
            this.pnlForm.Size = new System.Drawing.Size(468, 768);
            this.pnlForm.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblFormHeader.Location = new System.Drawing.Point(16, 16);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblFormHeader.Size = new System.Drawing.Size(155, 28);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Dispatch Order Form";

            this.lblLs.AutoSize = true;
            this.lblLs.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLs.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblLs.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblLs.Location = new System.Drawing.Point(16, 44);
            this.lblLs.Name = "lblLs";
            this.lblLs.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblLs.Size = new System.Drawing.Size(158, 23);
            this.lblLs.TabIndex = 1;
            this.lblLs.Text = "Select Loading Schedule:";

            this.cbLoadSchedule.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbLoadSchedule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLoadSchedule.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbLoadSchedule.FormattingEnabled = true;
            this.cbLoadSchedule.Location = new System.Drawing.Point(16, 67);
            this.cbLoadSchedule.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbLoadSchedule.Name = "cbLoadSchedule";
            this.cbLoadSchedule.Size = new System.Drawing.Size(436, 25);
            this.cbLoadSchedule.TabIndex = 2;

            this.tblCrates.ColumnCount = 2;
            this.tblCrates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCrates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCrates.Controls.Add(this.lblGCrate, 0, 0);
            this.tblCrates.Controls.Add(this.lblECrate, 1, 0);
            this.tblCrates.Controls.Add(this.numGCrate, 0, 1);
            this.tblCrates.Controls.Add(this.numECrate, 1, 1);
            this.tblCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCrates.Location = new System.Drawing.Point(16, 92);
            this.tblCrates.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.tblCrates.Name = "tblCrates";
            this.tblCrates.RowCount = 2;
            this.tblCrates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCrates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblCrates.Size = new System.Drawing.Size(436, 56);
            this.tblCrates.TabIndex = 3;

            this.lblGCrate.AutoSize = true;
            this.lblGCrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGCrate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblGCrate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblGCrate.Location = new System.Drawing.Point(0, 0);
            this.lblGCrate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblGCrate.Name = "lblGCrate";
            this.lblGCrate.Size = new System.Drawing.Size(214, 17);
            this.lblGCrate.TabIndex = 0;
            this.lblGCrate.Text = "G-Crates:";

            this.lblECrate.AutoSize = true;
            this.lblECrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblECrate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblECrate.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblECrate.Location = new System.Drawing.Point(222, 0);
            this.lblECrate.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblECrate.Name = "lblECrate";
            this.lblECrate.Size = new System.Drawing.Size(214, 17);
            this.lblECrate.TabIndex = 1;
            this.lblECrate.Text = "E-Crates:";

            this.numGCrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numGCrate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numGCrate.Location = new System.Drawing.Point(0, 21);
            this.numGCrate.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.numGCrate.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numGCrate.Name = "numGCrate";
            this.numGCrate.Size = new System.Drawing.Size(214, 24);
            this.numGCrate.TabIndex = 2;
            this.numGCrate.Value = new decimal(new int[] { 150, 0, 0, 0 });

            this.numECrate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numECrate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numECrate.Location = new System.Drawing.Point(222, 21);
            this.numECrate.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.numECrate.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numECrate.Name = "numECrate";
            this.numECrate.Size = new System.Drawing.Size(214, 24);
            this.numECrate.TabIndex = 3;

            this.lblTotalCrates.AutoSize = true;
            this.lblTotalCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTotalCrates.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTotalCrates.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblTotalCrates.Location = new System.Drawing.Point(16, 148);
            this.lblTotalCrates.Name = "lblTotalCrates";
            this.lblTotalCrates.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblTotalCrates.Size = new System.Drawing.Size(147, 23);
            this.lblTotalCrates.TabIndex = 4;
            this.lblTotalCrates.Text = "Total Combined Crates:";

            this.txtTotalCrates.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtTotalCrates.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.txtTotalCrates.Location = new System.Drawing.Point(16, 171);
            this.txtTotalCrates.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.txtTotalCrates.Name = "txtTotalCrates";
            this.txtTotalCrates.ReadOnly = true;
            this.txtTotalCrates.Size = new System.Drawing.Size(436, 24);
            this.txtTotalCrates.TabIndex = 5;
            this.txtTotalCrates.Text = "150 Crates";

            this.pnlRec.BackColor = System.Drawing.Color.FromArgb(240, 253, 244);
            this.pnlRec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlRec.Controls.Add(this.lblRec);
            this.pnlRec.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRec.Location = new System.Drawing.Point(16, 203);
            this.pnlRec.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlRec.Name = "pnlRec";
            this.pnlRec.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.pnlRec.Size = new System.Drawing.Size(436, 48);
            this.pnlRec.TabIndex = 6;

            this.lblRec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRec.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRec.ForeColor = System.Drawing.Color.FromArgb(22, 101, 52);
            this.lblRec.Location = new System.Drawing.Point(10, 6);
            this.lblRec.Name = "lblRec";
            this.lblRec.Size = new System.Drawing.Size(414, 34);
            this.lblRec.TabIndex = 0;
            this.lblRec.Text = "Truck Recommendation...";

            this.lblTr.AutoSize = true;
            this.lblTr.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTr.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTr.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblTr.Location = new System.Drawing.Point(16, 251);
            this.lblTr.Name = "lblTr";
            this.lblTr.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblTr.Size = new System.Drawing.Size(104, 23);
            this.lblTr.TabIndex = 7;
            this.lblTr.Text = "Assigned Truck:";

            this.cbTruck.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbTruck.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTruck.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbTruck.FormattingEnabled = true;
            this.cbTruck.Location = new System.Drawing.Point(16, 274);
            this.cbTruck.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbTruck.Name = "cbTruck";
            this.cbTruck.Size = new System.Drawing.Size(436, 25);
            this.cbTruck.TabIndex = 8;

            this.lblDr.AutoSize = true;
            this.lblDr.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDr.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDr.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDr.Location = new System.Drawing.Point(16, 307);
            this.lblDr.Name = "lblDr";
            this.lblDr.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblDr.Size = new System.Drawing.Size(107, 23);
            this.lblDr.TabIndex = 9;
            this.lblDr.Text = "Assigned Driver:";

            this.cbDriver.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbDriver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDriver.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDriver.FormattingEnabled = true;
            this.cbDriver.Location = new System.Drawing.Point(16, 330);
            this.cbDriver.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbDriver.Name = "cbDriver";
            this.cbDriver.Size = new System.Drawing.Size(436, 25);
            this.cbDriver.TabIndex = 10;

            this.lblAi.AutoSize = true;
            this.lblAi.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAi.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblAi.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblAi.Location = new System.Drawing.Point(16, 363);
            this.lblAi.Name = "lblAi";
            this.lblAi.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblAi.Size = new System.Drawing.Size(262, 23);
            this.lblAi.TabIndex = 11;
            this.lblAi.Text = "Assigned Helper/Aides (Select 1 or more):";

            this.pnlAidesSearchRow.Controls.Add(this.txtSearchAides);
            this.pnlAidesSearchRow.Controls.Add(this.btnClearAides);
            this.pnlAidesSearchRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAidesSearchRow.Location = new System.Drawing.Point(16, 386);
            this.pnlAidesSearchRow.Margin = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.pnlAidesSearchRow.Name = "pnlAidesSearchRow";
            this.pnlAidesSearchRow.Size = new System.Drawing.Size(436, 28);
            this.pnlAidesSearchRow.TabIndex = 12;

            this.txtSearchAides.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearchAides.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSearchAides.Location = new System.Drawing.Point(0, 0);
            this.txtSearchAides.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.txtSearchAides.Name = "txtSearchAides";
            this.txtSearchAides.PlaceholderText = "🔍 Type aide name to filter...";
            this.txtSearchAides.Size = new System.Drawing.Size(400, 26);
            this.txtSearchAides.TabIndex = 0;

            this.btnClearAides.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClearAides.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearAides.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClearAides.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnClearAides.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearAides.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnClearAides.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.btnClearAides.Location = new System.Drawing.Point(404, 0);
            this.btnClearAides.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnClearAides.Name = "btnClearAides";
            this.btnClearAides.Size = new System.Drawing.Size(32, 28);
            this.btnClearAides.TabIndex = 1;
            this.btnClearAides.Text = "✖";
            this.btnClearAides.UseVisualStyleBackColor = false;

            this.flowAidesModule.AutoScroll = true;
            this.flowAidesModule.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.flowAidesModule.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowAidesModule.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowAidesModule.Location = new System.Drawing.Point(16, 414);
            this.flowAidesModule.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.flowAidesModule.Name = "flowAidesModule";
            this.flowAidesModule.Padding = new System.Windows.Forms.Padding(6);
            this.flowAidesModule.Size = new System.Drawing.Size(436, 92);
            this.flowAidesModule.TabIndex = 13;

            this.lblAidesSummary.AutoSize = true;
            this.lblAidesSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAidesSummary.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblAidesSummary.ForeColor = System.Drawing.Color.FromArgb(22, 101, 52);
            this.lblAidesSummary.Location = new System.Drawing.Point(16, 506);
            this.lblAidesSummary.Margin = new System.Windows.Forms.Padding(0, 2, 0, 8);
            this.lblAidesSummary.Name = "lblAidesSummary";
            this.lblAidesSummary.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.lblAidesSummary.Size = new System.Drawing.Size(120, 21);
            this.lblAidesSummary.TabIndex = 14;
            this.lblAidesSummary.Text = "✓ 0 Aides Selected";

            this.tblDateTime.ColumnCount = 2;
            this.tblDateTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblDateTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblDateTime.Controls.Add(this.lblDd, 0, 0);
            this.tblDateTime.Controls.Add(this.lblDepartureTime, 1, 0);
            this.tblDateTime.Controls.Add(this.dtpDispatchDate, 0, 1);
            this.tblDateTime.Controls.Add(this.pnlTimeRow, 1, 1);
            this.tblDateTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblDateTime.Location = new System.Drawing.Point(16, 527);
            this.tblDateTime.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.tblDateTime.Name = "tblDateTime";
            this.tblDateTime.RowCount = 2;
            this.tblDateTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDateTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDateTime.Size = new System.Drawing.Size(436, 56);
            this.tblDateTime.TabIndex = 15;

            this.lblDd.AutoSize = true;
            this.lblDd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDd.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDd.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDd.Location = new System.Drawing.Point(0, 0);
            this.lblDd.Margin = new System.Windows.Forms.Padding(0, 0, 4, 2);
            this.lblDd.Name = "lblDd";
            this.lblDd.Size = new System.Drawing.Size(214, 17);
            this.lblDd.TabIndex = 0;
            this.lblDd.Text = "Dispatch Date:";

            this.lblDepartureTime.AutoSize = true;
            this.lblDepartureTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDepartureTime.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDepartureTime.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblDepartureTime.Location = new System.Drawing.Point(222, 0);
            this.lblDepartureTime.Margin = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblDepartureTime.Name = "lblDepartureTime";
            this.lblDepartureTime.Size = new System.Drawing.Size(214, 17);
            this.lblDepartureTime.TabIndex = 1;
            this.lblDepartureTime.Text = "Departure Time:";

            this.dtpDispatchDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpDispatchDate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpDispatchDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDispatchDate.Location = new System.Drawing.Point(0, 21);
            this.dtpDispatchDate.Margin = new System.Windows.Forms.Padding(0, 2, 4, 6);
            this.dtpDispatchDate.Name = "dtpDispatchDate";
            this.dtpDispatchDate.Size = new System.Drawing.Size(214, 24);
            this.dtpDispatchDate.TabIndex = 2;

            this.pnlTimeRow.Controls.Add(this.cbDepartureTime);
            this.pnlTimeRow.Controls.Add(this.btnPickAnalogTime);
            this.pnlTimeRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTimeRow.Location = new System.Drawing.Point(222, 21);
            this.pnlTimeRow.Margin = new System.Windows.Forms.Padding(4, 2, 0, 6);
            this.pnlTimeRow.Name = "pnlTimeRow";
            this.pnlTimeRow.Size = new System.Drawing.Size(214, 26);
            this.pnlTimeRow.TabIndex = 3;

            this.cbDepartureTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbDepartureTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cbDepartureTime.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDepartureTime.FormattingEnabled = true;
            this.cbDepartureTime.Items.AddRange(new object[] {
            "06:00 AM",
            "08:00 AM",
            "10:00 AM",
            "01:00 PM",
            "04:00 PM",
            "08:00 PM"});
            this.cbDepartureTime.Location = new System.Drawing.Point(0, 0);
            this.cbDepartureTime.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.cbDepartureTime.Name = "cbDepartureTime";
            this.cbDepartureTime.Size = new System.Drawing.Size(174, 25);
            this.cbDepartureTime.TabIndex = 0;
            this.cbDepartureTime.Text = "08:00 AM";

            this.btnPickAnalogTime.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickAnalogTime.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnPickAnalogTime.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPickAnalogTime.Location = new System.Drawing.Point(178, 0);
            this.btnPickAnalogTime.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnPickAnalogTime.Name = "btnPickAnalogTime";
            this.btnPickAnalogTime.Size = new System.Drawing.Size(36, 26);
            this.btnPickAnalogTime.TabIndex = 1;
            this.btnPickAnalogTime.Text = "🕒";
            this.btnPickAnalogTime.UseVisualStyleBackColor = true;

            this.lblSt.AutoSize = true;
            this.lblSt.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSt.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSt.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblSt.Location = new System.Drawing.Point(16, 583);
            this.lblSt.Name = "lblSt";
            this.lblSt.Padding = new System.Windows.Forms.Padding(0, 4, 0, 2);
            this.lblSt.Size = new System.Drawing.Size(104, 23);
            this.lblSt.TabIndex = 16;
            this.lblSt.Text = "Dispatch Status:";

            this.cbStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbStatus.FormattingEnabled = true;
            this.cbStatus.Items.AddRange(new object[] {
            "Pending",
            "In Transit",
            "Completed",
            "Cancelled"});
            this.cbStatus.Location = new System.Drawing.Point(16, 606);
            this.cbStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.cbStatus.Name = "cbStatus";
            this.cbStatus.Size = new System.Drawing.Size(436, 25);
            this.cbStatus.TabIndex = 17;

            this.btnSave.BackColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 641);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 4, 0, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(436, 40);
            this.btnSave.TabIndex = 18;
            this.btnSave.Text = "Save Dispatch Order";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 689);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(436, 42);
            this.tblUpdateDelete.TabIndex = 19;

            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.Location = new System.Drawing.Point(0, 0);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(214, 42);
            this.btnUpdate.TabIndex = 0;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDelete.Location = new System.Drawing.Point(222, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(214, 42);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.Location = new System.Drawing.Point(16, 737);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(436, 36);
            this.btnClear.TabIndex = 20;
            this.btnClear.Text = "Clear Form";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridDispatch);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(580, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(908, 768);
            this.pnlGridCard.TabIndex = 1;

            this.pnlGridHeaderBar.AutoSize = true;
            this.pnlGridHeaderBar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
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
            this.lblGridHeader.Size = new System.Drawing.Size(155, 28);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Active Dispatches List";

            this.pnlGridActionsFlow.AutoSize = true;
            this.pnlGridActionsFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlGridActionsFlow.Controls.Add(this.txtGridSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnClearSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnPrint);
            this.pnlGridActionsFlow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridActionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlGridActionsFlow.Location = new System.Drawing.Point(0, 28);
            this.pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            this.pnlGridActionsFlow.Size = new System.Drawing.Size(876, 36);
            this.pnlGridActionsFlow.TabIndex = 1;
            this.pnlGridActionsFlow.WrapContents = true;

            this.txtGridSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGridSearch.Location = new System.Drawing.Point(0, 4);
            this.txtGridSearch.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
            this.txtGridSearch.Name = "txtGridSearch";
            this.txtGridSearch.PlaceholderText = "🔍 Search dispatch #, driver, truck...";
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
            this.btnPrint.Location = new System.Drawing.Point(268, 2);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(160, 30);
            this.btnPrint.TabIndex = 2;
            this.btnPrint.Text = "🖨️ Print Report";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridDispatch.AllowUserToAddRows = false;
            this.gridDispatch.AllowUserToDeleteRows = false;
            this.gridDispatch.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDispatch.BackgroundColor = System.Drawing.Color.White;
            this.gridDispatch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridDispatch.ColumnHeadersHeight = 40;
            this.gridDispatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDispatch.EnableHeadersVisualStyles = false;
            this.gridDispatch.Location = new System.Drawing.Point(16, 60);
            this.gridDispatch.MultiSelect = false;
            this.gridDispatch.Name = "gridDispatch";
            this.gridDispatch.ReadOnly = true;
            this.gridDispatch.RowHeadersVisible = false;
            this.gridDispatch.RowTemplate.Height = 36;
            this.gridDispatch.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridDispatch.Size = new System.Drawing.Size(876, 654);
            this.gridDispatch.TabIndex = 1;
            this.gridDispatch.SelectionChanged += new System.EventHandler(this.GridDispatch_SelectionChanged);

            this.pnlSummary.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlSummary.Controls.Add(this.lblSummary);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummary.Location = new System.Drawing.Point(16, 714);
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

            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.tblCrates.ResumeLayout(false);
            this.tblCrates.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGCrate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numECrate)).EndInit();
            this.pnlRec.ResumeLayout(false);
            this.pnlAidesSearchRow.ResumeLayout(false);
            this.pnlAidesSearchRow.PerformLayout();
            this.tblDateTime.ResumeLayout(false);
            this.tblDateTime.PerformLayout();
            this.pnlTimeRow.ResumeLayout(false);
            this.tblUpdateDelete.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDispatch)).EndInit();
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
        private System.Windows.Forms.Label lblLs;
        private System.Windows.Forms.ComboBox cbLoadSchedule;
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
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
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
        private System.Windows.Forms.DataGridView gridDispatch;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
