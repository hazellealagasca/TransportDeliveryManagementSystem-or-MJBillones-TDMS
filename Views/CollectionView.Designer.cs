namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class CollectionView
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
            this.lblInv = new System.Windows.Forms.Label();
            this.cbInvoice = new System.Windows.Forms.ComboBox();
            this.tblPaymentRow = new System.Windows.Forms.TableLayoutPanel();
            this.pnlAmtCol = new System.Windows.Forms.Panel();
            this.lblAmt = new System.Windows.Forms.Label();
            this.numAmountPaid = new System.Windows.Forms.NumericUpDown();
            this.pnlMethodCol = new System.Windows.Forms.Panel();
            this.lblMethod = new System.Windows.Forms.Label();
            this.cbPaymentMethod = new System.Windows.Forms.ComboBox();
            this.lblRef = new System.Windows.Forms.Label();
            this.txtRefNo = new System.Windows.Forms.TextBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.tblUpdateDelete = new System.Windows.Forms.TableLayoutPanel();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.tblClearPrint = new System.Windows.Forms.TableLayoutPanel();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnPrintSlip = new System.Windows.Forms.Button();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlGridHeaderBar = new System.Windows.Forms.Panel();
            this.lblGridHeader = new System.Windows.Forms.Label();
            this.pnlGridActionsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.txtGridSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.gridCollections = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.tblPaymentRow.SuspendLayout();
            this.pnlAmtCol.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmountPaid)).BeginInit();
            this.pnlMethodCol.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.tblClearPrint.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCollections)).BeginInit();
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
            this.lblTitle.Text = "Payment Collections & Receipt Issuance";

            this.tblMasterContent.ColumnCount = 2;
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 480F));
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
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(16);
            this.pnlForm.Size = new System.Drawing.Size(464, 748);
            this.pnlForm.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblFormHeader.Location = new System.Drawing.Point(16, 12);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Size = new System.Drawing.Size(168, 20);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Record Payment Receipt";

            this.lblInv.AutoSize = true;
            this.lblInv.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblInv.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblInv.Location = new System.Drawing.Point(16, 42);
            this.lblInv.Name = "lblInv";
            this.lblInv.Size = new System.Drawing.Size(185, 17);
            this.lblInv.TabIndex = 1;
            this.lblInv.Text = "Select Unpaid Service Invoice:";

            this.cbInvoice.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbInvoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbInvoice.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbInvoice.FormattingEnabled = true;
            this.cbInvoice.Location = new System.Drawing.Point(16, 64);
            this.cbInvoice.Name = "cbInvoice";
            this.cbInvoice.Size = new System.Drawing.Size(432, 25);
            this.cbInvoice.TabIndex = 2;
            this.cbInvoice.SelectedIndexChanged += new System.EventHandler(this.CbInvoice_SelectedIndexChanged);

            this.tblPaymentRow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblPaymentRow.ColumnCount = 2;
            this.tblPaymentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPaymentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPaymentRow.Controls.Add(this.pnlAmtCol, 0, 0);
            this.tblPaymentRow.Controls.Add(this.pnlMethodCol, 1, 0);
            this.tblPaymentRow.Location = new System.Drawing.Point(16, 96);
            this.tblPaymentRow.Margin = new System.Windows.Forms.Padding(0);
            this.tblPaymentRow.Name = "tblPaymentRow";
            this.tblPaymentRow.RowCount = 1;
            this.tblPaymentRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPaymentRow.Size = new System.Drawing.Size(432, 54);
            this.tblPaymentRow.TabIndex = 3;

            this.pnlAmtCol.Controls.Add(this.numAmountPaid);
            this.pnlAmtCol.Controls.Add(this.lblAmt);
            this.pnlAmtCol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAmtCol.Location = new System.Drawing.Point(0, 0);
            this.pnlAmtCol.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.pnlAmtCol.Name = "pnlAmtCol";
            this.pnlAmtCol.Size = new System.Drawing.Size(210, 54);
            this.pnlAmtCol.TabIndex = 0;

            this.lblAmt.AutoSize = true;
            this.lblAmt.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAmt.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblAmt.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblAmt.Location = new System.Drawing.Point(0, 0);
            this.lblAmt.Name = "lblAmt";
            this.lblAmt.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblAmt.Size = new System.Drawing.Size(167, 18);
            this.lblAmt.TabIndex = 0;
            this.lblAmt.Text = "Payment Amount (PHP):";

            this.numAmountPaid.DecimalPlaces = 2;
            this.numAmountPaid.Dock = System.Windows.Forms.DockStyle.Top;
            this.numAmountPaid.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numAmountPaid.Location = new System.Drawing.Point(0, 18);
            this.numAmountPaid.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numAmountPaid.Name = "numAmountPaid";
            this.numAmountPaid.Size = new System.Drawing.Size(210, 24);
            this.numAmountPaid.TabIndex = 1;
            this.numAmountPaid.ThousandsSeparator = true;

            this.pnlMethodCol.Controls.Add(this.cbPaymentMethod);
            this.pnlMethodCol.Controls.Add(this.lblMethod);
            this.pnlMethodCol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMethodCol.Location = new System.Drawing.Point(216, 0);
            this.pnlMethodCol.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.pnlMethodCol.Name = "pnlMethodCol";
            this.pnlMethodCol.Size = new System.Drawing.Size(216, 54);
            this.pnlMethodCol.TabIndex = 1;

            this.lblMethod.AutoSize = true;
            this.lblMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMethod.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblMethod.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblMethod.Location = new System.Drawing.Point(0, 0);
            this.lblMethod.Name = "lblMethod";
            this.lblMethod.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblMethod.Size = new System.Drawing.Size(103, 18);
            this.lblMethod.TabIndex = 0;
            this.lblMethod.Text = "Payment Method:";

            this.cbPaymentMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbPaymentMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cbPaymentMethod.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbPaymentMethod.FormattingEnabled = true;
            this.cbPaymentMethod.Items.AddRange(new object[] {
            "Cash",
            "Cheque",
            "Bank Transfer",
            "Online Deposit"});
            this.cbPaymentMethod.Location = new System.Drawing.Point(0, 18);
            this.cbPaymentMethod.Name = "cbPaymentMethod";
            this.cbPaymentMethod.Size = new System.Drawing.Size(216, 25);
            this.cbPaymentMethod.TabIndex = 1;

            this.lblRef.AutoSize = true;
            this.lblRef.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRef.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblRef.Location = new System.Drawing.Point(16, 156);
            this.lblRef.Name = "lblRef";
            this.lblRef.Size = new System.Drawing.Size(175, 17);
            this.lblRef.TabIndex = 4;
            this.lblRef.Text = "Check / Reference Number:";

            this.txtRefNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRefNo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtRefNo.Location = new System.Drawing.Point(16, 178);
            this.txtRefNo.Name = "txtRefNo";
            this.txtRefNo.PlaceholderText = "e.g. CHK-990214 or BDO-TXN-55410";
            this.txtRefNo.Size = new System.Drawing.Size(432, 24);
            this.txtRefNo.TabIndex = 5;

            this.lblNotes.AutoSize = true;
            this.lblNotes.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNotes.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblNotes.Location = new System.Drawing.Point(16, 212);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.Size = new System.Drawing.Size(174, 17);
            this.lblNotes.TabIndex = 6;
            this.lblNotes.Text = "Collection Notes / Remarks:";

            this.txtNotes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNotes.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtNotes.Location = new System.Drawing.Point(16, 234);
            this.txtNotes.Multiline = true;
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.PlaceholderText = "Payment received in full settlement of invoice...";
            this.txtNotes.Size = new System.Drawing.Size(432, 50);
            this.txtNotes.TabIndex = 7;

            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 294);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(432, 40);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "💵 COLLECT PAYMENT";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 342);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(432, 40);
            this.tblUpdateDelete.TabIndex = 9;

            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.FlatAppearance.BorderSize = 0;
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.ForeColor = System.Drawing.Color.White;
            this.btnUpdate.Location = new System.Drawing.Point(0, 0);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(212, 40);
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
            this.btnDelete.Location = new System.Drawing.Point(220, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(212, 40);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "🗑️ DELETE";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.tblClearPrint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblClearPrint.ColumnCount = 2;
            this.tblClearPrint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblClearPrint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblClearPrint.Controls.Add(this.btnClear, 0, 0);
            this.tblClearPrint.Controls.Add(this.btnPrintSlip, 1, 0);
            this.tblClearPrint.Location = new System.Drawing.Point(16, 390);
            this.tblClearPrint.Margin = new System.Windows.Forms.Padding(0);
            this.tblClearPrint.Name = "tblClearPrint";
            this.tblClearPrint.RowCount = 1;
            this.tblClearPrint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblClearPrint.Size = new System.Drawing.Size(432, 38);
            this.tblClearPrint.TabIndex = 10;

            this.btnClear.BackColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(0, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(212, 38);
            this.btnClear.TabIndex = 0;
            this.btnClear.Text = "🧹 CLEAR";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.btnPrintSlip.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.btnPrintSlip.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrintSlip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrintSlip.FlatAppearance.BorderSize = 0;
            this.btnPrintSlip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrintSlip.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnPrintSlip.ForeColor = System.Drawing.Color.White;
            this.btnPrintSlip.Location = new System.Drawing.Point(220, 0);
            this.btnPrintSlip.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnPrintSlip.Name = "btnPrintSlip";
            this.btnPrintSlip.Size = new System.Drawing.Size(212, 38);
            this.btnPrintSlip.TabIndex = 1;
            this.btnPrintSlip.Text = "🧾 PRINT RECEIPT";
            this.btnPrintSlip.UseVisualStyleBackColor = false;
            this.btnPrintSlip.Click += new System.EventHandler(this.BtnPrintSlip_Click);

            this.pnlForm.Controls.Add(this.tblClearPrint);
            this.pnlForm.Controls.Add(this.tblUpdateDelete);
            this.pnlForm.Controls.Add(this.btnSave);
            this.pnlForm.Controls.Add(this.txtNotes);
            this.pnlForm.Controls.Add(this.lblNotes);
            this.pnlForm.Controls.Add(this.txtRefNo);
            this.pnlForm.Controls.Add(this.lblRef);
            this.pnlForm.Controls.Add(this.tblPaymentRow);
            this.pnlForm.Controls.Add(this.cbInvoice);
            this.pnlForm.Controls.Add(this.lblInv);
            this.pnlForm.Controls.Add(this.lblFormHeader);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridCollections);
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
            this.lblGridHeader.Size = new System.Drawing.Size(248, 26);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Payment Collections Register Log";

            this.pnlGridActionsFlow.AutoSize = true;
            this.pnlGridActionsFlow.Controls.Add(this.txtGridSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnClearSearch);
            this.pnlGridActionsFlow.Controls.Add(this.btnPrint);
            this.pnlGridActionsFlow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridActionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlGridActionsFlow.Location = new System.Drawing.Point(0, 26);
            this.pnlGridActionsFlow.Name = "pnlGridActionsFlow";
            this.pnlGridActionsFlow.Size = new System.Drawing.Size(460, 36);
            this.pnlGridActionsFlow.TabIndex = 1;
            this.pnlGridActionsFlow.WrapContents = true;

            this.txtGridSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGridSearch.Location = new System.Drawing.Point(0, 4);
            this.txtGridSearch.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
            this.txtGridSearch.Name = "txtGridSearch";
            this.txtGridSearch.PlaceholderText = "🔍 Search receipt #, invoice #, ref #...";
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
            this.btnPrint.Size = new System.Drawing.Size(180, 28);
            this.btnPrint.TabIndex = 2;
            this.btnPrint.Text = "🖨️ PRINT LOG REPORT";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridCollections.AllowUserToAddRows = false;
            this.gridCollections.AllowUserToDeleteRows = false;
            this.gridCollections.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridCollections.BackgroundColor = System.Drawing.Color.White;
            this.gridCollections.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridCollections.ColumnHeadersHeight = 36;
            this.gridCollections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridCollections.EnableHeadersVisualStyles = false;
            this.gridCollections.Location = new System.Drawing.Point(16, 60);
            this.gridCollections.MultiSelect = false;
            this.gridCollections.Name = "gridCollections";
            this.gridCollections.ReadOnly = true;
            this.gridCollections.RowHeadersVisible = false;
            this.gridCollections.RowTemplate.Height = 34;
            this.gridCollections.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCollections.Size = new System.Drawing.Size(876, 634);
            this.gridCollections.TabIndex = 1;
            this.gridCollections.SelectionChanged += new System.EventHandler(this.GridCollections_SelectionChanged);

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
            this.Controls.Add(this.lblTitle);
            this.Name = "CollectionView";
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.tblPaymentRow.ResumeLayout(false);
            this.pnlAmtCol.ResumeLayout(false);
            this.pnlAmtCol.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAmountPaid)).EndInit();
            this.pnlMethodCol.ResumeLayout(false);
            this.pnlMethodCol.PerformLayout();
            this.tblUpdateDelete.ResumeLayout(false);
            this.tblClearPrint.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridCard.PerformLayout();
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCollections)).EndInit();
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
        private System.Windows.Forms.Label lblInv;
        private System.Windows.Forms.ComboBox cbInvoice;
        private System.Windows.Forms.TableLayoutPanel tblPaymentRow;
        private System.Windows.Forms.Panel pnlAmtCol;
        private System.Windows.Forms.Label lblAmt;
        private System.Windows.Forms.NumericUpDown numAmountPaid;
        private System.Windows.Forms.Panel pnlMethodCol;
        private System.Windows.Forms.Label lblMethod;
        private System.Windows.Forms.ComboBox cbPaymentMethod;
        private System.Windows.Forms.Label lblRef;
        private System.Windows.Forms.TextBox txtRefNo;
        private System.Windows.Forms.Label lblNotes;
        private System.Windows.Forms.TextBox txtNotes;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TableLayoutPanel tblUpdateDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.TableLayoutPanel tblClearPrint;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnPrintSlip;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeaderBar;
        private System.Windows.Forms.Label lblGridHeader;
        private System.Windows.Forms.FlowLayoutPanel pnlGridActionsFlow;
        private System.Windows.Forms.TextBox txtGridSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.DataGridView gridCollections;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
