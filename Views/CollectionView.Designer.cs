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
            this.pnlFormHeaderBar = new System.Windows.Forms.Panel();
            this.pnlFormActionsBar = new System.Windows.Forms.Panel();
            this.pnlFormBody = new System.Windows.Forms.Panel();
            this.lblFormHeader = new System.Windows.Forms.Label();
            this.lblInv = new System.Windows.Forms.Label();
            this.cbInvoice = new System.Windows.Forms.ComboBox();
            this.pnlLinkedInvoice = new System.Windows.Forms.Panel();
            this.lblLinkedInvoiceInfo = new System.Windows.Forms.Label();
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
            this.pnlFormHeaderBar.SuspendLayout();
            this.pnlFormActionsBar.SuspendLayout();
            this.pnlFormBody.SuspendLayout();
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

            this.AutoScroll = false;
            this.AutoScrollMinSize = System.Drawing.Size.Empty;
            this.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(1420, 820);

            this.lblTitle.AutoSize = false;
            this.lblTitle.Size = System.Drawing.Size.Empty;
            this.lblTitle.Visible = false;

            this.tblMasterContent.ColumnCount = 2;
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 600F));
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Controls.Add(this.pnlForm, 0, 0);
            this.tblMasterContent.Controls.Add(this.pnlGridCard, 1, 0);
            this.tblMasterContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMasterContent.Location = new System.Drawing.Point(16, 16);
            this.tblMasterContent.Name = "tblMasterContent";
            this.tblMasterContent.RowCount = 1;
            this.tblMasterContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMasterContent.Size = new System.Drawing.Size(1388, 788);
            this.tblMasterContent.TabIndex = 1;

            this.pnlForm.AutoScroll = false;
            this.pnlForm.BackColor = System.Drawing.Color.White;
            this.pnlForm.Controls.Add(this.pnlFormBody);
            this.pnlForm.Controls.Add(this.pnlFormActionsBar);
            this.pnlForm.Controls.Add(this.pnlFormHeaderBar);
            this.pnlForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlForm.Location = new System.Drawing.Point(0, 0);
            this.pnlForm.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(0);
            this.pnlForm.Size = new System.Drawing.Size(600, 788);
            this.pnlForm.TabIndex = 0;

            this.pnlFormHeaderBar.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFormHeaderBar.Controls.Add(this.lblFormHeader);
            this.pnlFormHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFormHeaderBar.Location = new System.Drawing.Point(0, 0);
            this.pnlFormHeaderBar.Name = "pnlFormHeaderBar";
            this.pnlFormHeaderBar.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlFormHeaderBar.Size = new System.Drawing.Size(600, 38);
            this.pnlFormHeaderBar.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblFormHeader.Location = new System.Drawing.Point(12, 6);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Size = new System.Drawing.Size(185, 21);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "Record Payment Receipt";

            this.lblInv.AutoSize = true;
            this.lblInv.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblInv.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblInv.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblInv.Location = new System.Drawing.Point(16, 16);
            this.lblInv.Name = "lblInv";
            this.lblInv.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.lblInv.Size = new System.Drawing.Size(185, 23);
            this.lblInv.TabIndex = 0;
            this.lblInv.Text = "Select Invoice with Outstanding Balance:";
            this.cbInvoice.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbInvoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbInvoice.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbInvoice.FormattingEnabled = true;
            this.cbInvoice.Location = new System.Drawing.Point(16, 39);
            this.cbInvoice.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cbInvoice.Name = "cbInvoice";
            this.cbInvoice.Size = new System.Drawing.Size(448, 25);
            this.cbInvoice.TabIndex = 1;
            this.cbInvoice.SelectedIndexChanged += new System.EventHandler(this.CbInvoice_SelectedIndexChanged);

            this.pnlLinkedInvoice.BackColor = System.Drawing.Color.FromArgb(240, 249, 255);
            this.pnlLinkedInvoice.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLinkedInvoice.Controls.Add(this.lblLinkedInvoiceInfo);
            this.pnlLinkedInvoice.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLinkedInvoice.Location = new System.Drawing.Point(16, 68);
            this.pnlLinkedInvoice.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlLinkedInvoice.Name = "pnlLinkedInvoice";
            this.pnlLinkedInvoice.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.pnlLinkedInvoice.Size = new System.Drawing.Size(448, 34);
            this.pnlLinkedInvoice.TabIndex = 2;

            this.lblLinkedInvoiceInfo.AutoEllipsis = true;
            this.lblLinkedInvoiceInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLinkedInvoiceInfo.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLinkedInvoiceInfo.ForeColor = System.Drawing.Color.FromArgb(3, 105, 161);
            this.lblLinkedInvoiceInfo.Location = new System.Drawing.Point(6, 4);
            this.lblLinkedInvoiceInfo.Name = "lblLinkedInvoiceInfo";
            this.lblLinkedInvoiceInfo.Size = new System.Drawing.Size(434, 18);
            this.lblLinkedInvoiceInfo.TabIndex = 0;
            this.lblLinkedInvoiceInfo.Text = "📄 Linked Invoice: (None Selected)";
            this.lblLinkedInvoiceInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.tblPaymentRow.ColumnCount = 2;
            this.tblPaymentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPaymentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPaymentRow.Controls.Add(this.pnlAmtCol, 0, 0);
            this.tblPaymentRow.Controls.Add(this.pnlMethodCol, 1, 0);
            this.tblPaymentRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblPaymentRow.Location = new System.Drawing.Point(16, 64);
            this.tblPaymentRow.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.tblPaymentRow.Name = "tblPaymentRow";
            this.tblPaymentRow.RowCount = 1;
            this.tblPaymentRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPaymentRow.Size = new System.Drawing.Size(448, 68);
            this.tblPaymentRow.TabIndex = 2;

            this.pnlAmtCol.Controls.Add(this.numAmountPaid);
            this.pnlAmtCol.Controls.Add(this.lblAmt);
            this.pnlAmtCol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAmtCol.Location = new System.Drawing.Point(0, 0);
            this.pnlAmtCol.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.pnlAmtCol.Name = "pnlAmtCol";
            this.pnlAmtCol.Size = new System.Drawing.Size(218, 68);
            this.pnlAmtCol.TabIndex = 0;

            this.lblAmt.AutoSize = true;
            this.lblAmt.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAmt.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblAmt.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblAmt.Location = new System.Drawing.Point(0, 0);
            this.lblAmt.Name = "lblAmt";
            this.lblAmt.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.lblAmt.Size = new System.Drawing.Size(167, 18);
            this.lblAmt.TabIndex = 0;
            this.lblAmt.Text = "Payment Amount (PHP):";

            this.numAmountPaid.DecimalPlaces = 2;
            this.numAmountPaid.Dock = System.Windows.Forms.DockStyle.Top;
            this.numAmountPaid.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numAmountPaid.Location = new System.Drawing.Point(0, 18);
            this.numAmountPaid.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numAmountPaid.Name = "numAmountPaid";
            this.numAmountPaid.Size = new System.Drawing.Size(218, 24);
            this.numAmountPaid.TabIndex = 1;
            this.numAmountPaid.ThousandsSeparator = true;

            this.pnlMethodCol.Controls.Add(this.cbPaymentMethod);
            this.pnlMethodCol.Controls.Add(this.lblMethod);
            this.pnlMethodCol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMethodCol.Location = new System.Drawing.Point(224, 0);
            this.pnlMethodCol.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.pnlMethodCol.Name = "pnlMethodCol";
            this.pnlMethodCol.Size = new System.Drawing.Size(224, 68);
            this.pnlMethodCol.TabIndex = 1;

            this.lblMethod.AutoSize = true;
            this.lblMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMethod.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblMethod.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblMethod.Location = new System.Drawing.Point(0, 0);
            this.lblMethod.Name = "lblMethod";
            this.lblMethod.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
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
            this.cbPaymentMethod.Size = new System.Drawing.Size(224, 25);
            this.cbPaymentMethod.TabIndex = 1;

            this.lblRef.AutoSize = true;
            this.lblRef.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRef.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRef.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblRef.Location = new System.Drawing.Point(16, 118);
            this.lblRef.Name = "lblRef";
            this.lblRef.Padding = new System.Windows.Forms.Padding(0, 8, 0, 2);
            this.lblRef.Size = new System.Drawing.Size(175, 27);
            this.lblRef.TabIndex = 3;
            this.lblRef.Text = "Payment / Transaction Reference # *:";

            this.txtRefNo.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtRefNo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtRefNo.Location = new System.Drawing.Point(16, 145);
            this.txtRefNo.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.txtRefNo.Name = "txtRefNo";
            this.txtRefNo.PlaceholderText = "e.g. CHK-990214, BDO-TXN-55410, or REF-001";
            this.txtRefNo.Size = new System.Drawing.Size(448, 24);
            this.txtRefNo.TabIndex = 4;

            this.lblNotes.AutoSize = true;
            this.lblNotes.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNotes.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNotes.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblNotes.Location = new System.Drawing.Point(16, 169);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.Padding = new System.Windows.Forms.Padding(0, 8, 0, 2);
            this.lblNotes.Size = new System.Drawing.Size(174, 27);
            this.lblNotes.TabIndex = 5;
            this.lblNotes.Text = "Collection Notes / Remarks:";

            this.txtNotes.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtNotes.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtNotes.Location = new System.Drawing.Point(16, 196);
            this.txtNotes.Multiline = true;
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.PlaceholderText = "Payment received in full settlement of invoice...";
            this.txtNotes.Size = new System.Drawing.Size(448, 64);
            this.txtNotes.TabIndex = 6;

            this.btnSave.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(0, 0);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(190, 32);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "💵 RECORD PAYMENT";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnSave, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Location = new System.Drawing.Point(12, 5);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(386, 32);
            this.tblUpdateDelete.TabIndex = 0;

            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUpdate.FlatAppearance.BorderSize = 0;
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.ForeColor = System.Drawing.Color.White;
            this.btnUpdate.Location = new System.Drawing.Point(196, 0);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(190, 32);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "✏️ UPDATE RECEIPT";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.Location = new System.Drawing.Point(0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(0, 0);
            this.btnDelete.TabIndex = 99;
            this.btnDelete.Visible = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.tblClearPrint.ColumnCount = 2;
            this.tblClearPrint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblClearPrint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblClearPrint.Controls.Add(this.btnClear, 0, 0);
            this.tblClearPrint.Controls.Add(this.btnPrintSlip, 1, 0);
            this.tblClearPrint.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tblClearPrint.Location = new System.Drawing.Point(12, 39);
            this.tblClearPrint.Margin = new System.Windows.Forms.Padding(0);
            this.tblClearPrint.Name = "tblClearPrint";
            this.tblClearPrint.RowCount = 1;
            this.tblClearPrint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tblClearPrint.Size = new System.Drawing.Size(386, 28);
            this.tblClearPrint.TabIndex = 1;

            this.btnClear.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClear.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnClear.FlatAppearance.BorderSize = 1;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnClear.Location = new System.Drawing.Point(0, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(190, 28);
            this.btnClear.TabIndex = 0;
            this.btnClear.Text = "🧹 CLEAR FORM";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.btnPrintSlip.BackColor = System.Drawing.Color.FromArgb(14, 165, 233);
            this.btnPrintSlip.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrintSlip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrintSlip.FlatAppearance.BorderSize = 0;
            this.btnPrintSlip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrintSlip.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrintSlip.ForeColor = System.Drawing.Color.White;
            this.btnPrintSlip.Location = new System.Drawing.Point(196, 0);
            this.btnPrintSlip.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnPrintSlip.Name = "btnPrintSlip";
            this.btnPrintSlip.Size = new System.Drawing.Size(190, 28);
            this.btnPrintSlip.TabIndex = 1;
            this.btnPrintSlip.Text = "🧾 PRINT RECEIPT";
            this.btnPrintSlip.UseVisualStyleBackColor = false;
            this.btnPrintSlip.Click += new System.EventHandler(this.BtnPrintSlip_Click);

            this.pnlFormActionsBar.BackColor = System.Drawing.Color.White;
            this.pnlFormActionsBar.Controls.Add(this.tblClearPrint);
            this.pnlFormActionsBar.Controls.Add(this.tblUpdateDelete);
            this.pnlFormActionsBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFormActionsBar.Location = new System.Drawing.Point(0, 714);
            this.pnlFormActionsBar.Name = "pnlFormActionsBar";
            this.pnlFormActionsBar.Padding = new System.Windows.Forms.Padding(12, 5, 12, 7);
            this.pnlFormActionsBar.Size = new System.Drawing.Size(600, 74);
            this.pnlFormActionsBar.TabIndex = 2;

            this.pnlFormBody.AutoScroll = true;
            this.pnlFormBody.BackColor = System.Drawing.Color.White;
            this.pnlFormBody.Controls.Add(this.txtNotes);
            this.pnlFormBody.Controls.Add(this.lblNotes);
            this.pnlFormBody.Controls.Add(this.txtRefNo);
            this.pnlFormBody.Controls.Add(this.lblRef);
            this.pnlFormBody.Controls.Add(this.tblPaymentRow);
            this.pnlFormBody.Controls.Add(this.pnlLinkedInvoice);
            this.pnlFormBody.Controls.Add(this.cbInvoice);
            this.pnlFormBody.Controls.Add(this.lblInv);
            this.pnlFormBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFormBody.Location = new System.Drawing.Point(0, 38);
            this.pnlFormBody.Name = "pnlFormBody";
            this.pnlFormBody.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.pnlFormBody.Size = new System.Drawing.Size(600, 676);
            this.pnlFormBody.TabIndex = 1;

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridCollections);
            this.pnlGridCard.Controls.Add(this.pnlSummary);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(410, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(978, 788);
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
            this.btnClearSearch.Size = new System.Drawing.Size(34, 28);
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
            this.gridCollections.RowTemplate.Height = 32;
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
            this.pnlFormActionsBar.ResumeLayout(false);
            this.pnlFormBody.ResumeLayout(false);
            this.pnlFormBody.PerformLayout();
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.pnlFormHeaderBar.ResumeLayout(false);
            this.pnlFormHeaderBar.PerformLayout();
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
        private System.Windows.Forms.Panel pnlFormHeaderBar;
        private System.Windows.Forms.Panel pnlFormActionsBar;
        private System.Windows.Forms.Panel pnlFormBody;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.Label lblInv;
        private System.Windows.Forms.ComboBox cbInvoice;
        private System.Windows.Forms.Panel pnlLinkedInvoice;
        private System.Windows.Forms.Label lblLinkedInvoiceInfo;
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
