namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class IncidentReportView
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.tblMasterContent = new System.Windows.Forms.TableLayoutPanel();
            this.pnlForm = new System.Windows.Forms.Panel();
            this.pnlFormHeaderBar = new System.Windows.Forms.Panel();
            this.pnlFormActionsBar = new System.Windows.Forms.Panel();
            this.pnlFormBody = new System.Windows.Forms.Panel();
            this.lblFormHeader = new System.Windows.Forms.Label();
            this.lblDel = new System.Windows.Forms.Label();
            this.cbDelivery = new System.Windows.Forms.ComboBox();
            this.tblQtyCost = new System.Windows.Forms.TableLayoutPanel();
            this.pnlQty = new System.Windows.Forms.Panel();
            this.lblQty = new System.Windows.Forms.Label();
            this.numDamagedQty = new System.Windows.Forms.NumericUpDown();
            this.pnlCost = new System.Windows.Forms.Panel();
            this.lblCost = new System.Windows.Forms.Label();
            this.numEstimatedCost = new System.Windows.Forms.NumericUpDown();
            this.lblCause = new System.Windows.Forms.Label();
            this.txtCause = new System.Windows.Forms.TextBox();
            this.chkChargeable = new System.Windows.Forms.CheckBox();
            this.lblDesc = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.lblPhoto = new System.Windows.Forms.Label();
            this.tblPhotoModule = new System.Windows.Forms.TableLayoutPanel();
            this.picPhoto = new System.Windows.Forms.PictureBox();
            this.tblPhotoActions = new System.Windows.Forms.TableLayoutPanel();
            this.btnUploadPhoto = new System.Windows.Forms.Button();
            this.tblViewRemovePhoto = new System.Windows.Forms.TableLayoutPanel();
            this.btnViewPhoto = new System.Windows.Forms.Button();
            this.btnRemovePhoto = new System.Windows.Forms.Button();
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
            this.gridIncidents = new System.Windows.Forms.DataGridView();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.pnlFormHeaderBar.SuspendLayout();
            this.pnlFormActionsBar.SuspendLayout();
            this.pnlFormBody.SuspendLayout();
            this.tblQtyCost.SuspendLayout();
            this.pnlQty.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDamagedQty)).BeginInit();
            this.pnlCost.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEstimatedCost)).BeginInit();
            this.tblPhotoModule.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPhoto)).BeginInit();
            this.tblPhotoActions.SuspendLayout();
            this.tblViewRemovePhoto.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridIncidents)).BeginInit();
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
            this.tblMasterContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 480F));
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
            this.pnlForm.Size = new System.Drawing.Size(480, 788);
            this.pnlForm.TabIndex = 0;

            this.pnlFormHeaderBar.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFormHeaderBar.Controls.Add(this.lblFormHeader);
            this.pnlFormHeaderBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFormHeaderBar.Location = new System.Drawing.Point(0, 0);
            this.pnlFormHeaderBar.Name = "pnlFormHeaderBar";
            this.pnlFormHeaderBar.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlFormHeaderBar.Size = new System.Drawing.Size(480, 38);
            this.pnlFormHeaderBar.TabIndex = 0;

            this.lblFormHeader.AutoSize = true;
            this.lblFormHeader.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblFormHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblFormHeader.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblFormHeader.Location = new System.Drawing.Point(12, 6);
            this.lblFormHeader.Name = "lblFormHeader";
            this.lblFormHeader.Size = new System.Drawing.Size(185, 21);
            this.lblFormHeader.TabIndex = 0;
            this.lblFormHeader.Text = "File New Incident Report";

            this.lblDel.AutoSize = true;
            this.lblDel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDel.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.lblDel.Location = new System.Drawing.Point(16, 60);
            this.lblDel.Name = "lblDel";
            this.lblDel.Size = new System.Drawing.Size(160, 17);
            this.lblDel.TabIndex = 1;
            this.lblDel.Text = "Associated Delivery Trip:";

            this.cbDelivery.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbDelivery.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelivery.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbDelivery.FormattingEnabled = true;
            this.cbDelivery.Location = new System.Drawing.Point(16, 82);
            this.cbDelivery.Name = "cbDelivery";
            this.cbDelivery.Size = new System.Drawing.Size(536, 25);
            this.cbDelivery.TabIndex = 2;

            this.tblQtyCost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblQtyCost.ColumnCount = 2;
            this.tblQtyCost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblQtyCost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblQtyCost.Controls.Add(this.pnlQty, 0, 0);
            this.tblQtyCost.Controls.Add(this.pnlCost, 1, 0);
            this.tblQtyCost.Location = new System.Drawing.Point(16, 118);
            this.tblQtyCost.Margin = new System.Windows.Forms.Padding(0);
            this.tblQtyCost.Name = "tblQtyCost";
            this.tblQtyCost.RowCount = 1;
            this.tblQtyCost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblQtyCost.Size = new System.Drawing.Size(536, 54);
            this.tblQtyCost.TabIndex = 3;

            this.pnlQty.Controls.Add(this.numDamagedQty);
            this.pnlQty.Controls.Add(this.lblQty);
            this.pnlQty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlQty.Location = new System.Drawing.Point(0, 0);
            this.pnlQty.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.pnlQty.Name = "pnlQty";
            this.pnlQty.Size = new System.Drawing.Size(262, 54);
            this.pnlQty.TabIndex = 0;

            this.lblQty.AutoSize = true;
            this.lblQty.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblQty.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblQty.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblQty.Location = new System.Drawing.Point(0, 0);
            this.lblQty.Name = "lblQty";
            this.lblQty.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblQty.Size = new System.Drawing.Size(147, 18);
            this.lblQty.TabIndex = 0;
            this.lblQty.Text = "Damaged Quantity (Qty):";

            this.numDamagedQty.Dock = System.Windows.Forms.DockStyle.Top;
            this.numDamagedQty.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numDamagedQty.Location = new System.Drawing.Point(0, 18);
            this.numDamagedQty.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numDamagedQty.Name = "numDamagedQty";
            this.numDamagedQty.Size = new System.Drawing.Size(262, 24);
            this.numDamagedQty.TabIndex = 1;
            this.numDamagedQty.Value = new decimal(new int[] { 2, 0, 0, 0 });

            this.pnlCost.Controls.Add(this.numEstimatedCost);
            this.pnlCost.Controls.Add(this.lblCost);
            this.pnlCost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCost.Location = new System.Drawing.Point(268, 0);
            this.pnlCost.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.pnlCost.Name = "pnlCost";
            this.pnlCost.Size = new System.Drawing.Size(268, 54);
            this.pnlCost.TabIndex = 1;

            this.lblCost.AutoSize = true;
            this.lblCost.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCost.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblCost.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblCost.Location = new System.Drawing.Point(0, 0);
            this.lblCost.Name = "lblCost";
            this.lblCost.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblCost.Size = new System.Drawing.Size(164, 18);
            this.lblCost.TabIndex = 0;
            this.lblCost.Text = "Estimated Damage Cost (₱):";

            this.numEstimatedCost.DecimalPlaces = 2;
            this.numEstimatedCost.Dock = System.Windows.Forms.DockStyle.Top;
            this.numEstimatedCost.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.numEstimatedCost.Location = new System.Drawing.Point(0, 18);
            this.numEstimatedCost.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numEstimatedCost.Name = "numEstimatedCost";
            this.numEstimatedCost.Size = new System.Drawing.Size(268, 24);
            this.numEstimatedCost.TabIndex = 1;
            this.numEstimatedCost.ThousandsSeparator = true;
            this.numEstimatedCost.Value = new decimal(new int[] { 1500, 0, 0, 0 });

            this.lblCause.AutoSize = true;
            this.lblCause.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCause.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.lblCause.Location = new System.Drawing.Point(16, 184);
            this.lblCause.Name = "lblCause";
            this.lblCause.Size = new System.Drawing.Size(163, 17);
            this.lblCause.TabIndex = 4;
            this.lblCause.Text = "Primary Cause of Incident:";

            this.txtCause.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCause.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtCause.Location = new System.Drawing.Point(16, 206);
            this.txtCause.MaxLength = 200;
            this.txtCause.Name = "txtCause";
            this.txtCause.PlaceholderText = "e.g. Broken refrigeration latch / rough road transit...";
            this.txtCause.Size = new System.Drawing.Size(536, 24);
            this.txtCause.TabIndex = 5;

            this.chkChargeable.AutoSize = true;
            this.chkChargeable.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.chkChargeable.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
            this.chkChargeable.Location = new System.Drawing.Point(16, 242);
            this.chkChargeable.Name = "chkChargeable";
            this.chkChargeable.Size = new System.Drawing.Size(262, 21);
            this.chkChargeable.TabIndex = 6;
            this.chkChargeable.Text = "Damage Chargeable to Crew Negligence";

            this.lblDesc.AutoSize = true;
            this.lblDesc.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblDesc.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.lblDesc.Location = new System.Drawing.Point(16, 274);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(169, 17);
            this.lblDesc.TabIndex = 7;
            this.lblDesc.Text = "Detailed Incident Remarks:";

            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtDescription.Location = new System.Drawing.Point(16, 296);
            this.txtDescription.MaxLength = 500;
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.PlaceholderText = "Describe damage extent and store acceptance status...";
            this.txtDescription.Size = new System.Drawing.Size(536, 50);
            this.txtDescription.TabIndex = 8;

            this.lblPhoto.AutoSize = true;
            this.lblPhoto.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPhoto.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblPhoto.Location = new System.Drawing.Point(16, 358);
            this.lblPhoto.Name = "lblPhoto";
            this.lblPhoto.Size = new System.Drawing.Size(206, 17);
            this.lblPhoto.TabIndex = 9;
            this.lblPhoto.Text = "Damage Photo / Proof Reference:";

            this.tblPhotoModule.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblPhotoModule.ColumnCount = 2;
            this.tblPhotoModule.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tblPhotoModule.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPhotoModule.Controls.Add(this.picPhoto, 0, 0);
            this.tblPhotoModule.Controls.Add(this.tblPhotoActions, 1, 0);
            this.tblPhotoModule.Location = new System.Drawing.Point(16, 380);
            this.tblPhotoModule.Margin = new System.Windows.Forms.Padding(0);
            this.tblPhotoModule.Name = "tblPhotoModule";
            this.tblPhotoModule.RowCount = 1;
            this.tblPhotoModule.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPhotoModule.Size = new System.Drawing.Size(536, 82);
            this.tblPhotoModule.TabIndex = 10;

            this.picPhoto.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.picPhoto.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPhoto.Location = new System.Drawing.Point(0, 0);
            this.picPhoto.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.picPhoto.Name = "picPhoto";
            this.picPhoto.Size = new System.Drawing.Size(122, 82);
            this.picPhoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPhoto.TabIndex = 0;
            this.picPhoto.TabStop = false;

            this.tblPhotoActions.ColumnCount = 1;
            this.tblPhotoActions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPhotoActions.Controls.Add(this.btnUploadPhoto, 0, 0);
            this.tblPhotoActions.Controls.Add(this.tblViewRemovePhoto, 0, 1);
            this.tblPhotoActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblPhotoActions.Location = new System.Drawing.Point(130, 0);
            this.tblPhotoActions.Margin = new System.Windows.Forms.Padding(0);
            this.tblPhotoActions.Name = "tblPhotoActions";
            this.tblPhotoActions.RowCount = 2;
            this.tblPhotoActions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPhotoActions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblPhotoActions.Size = new System.Drawing.Size(406, 82);
            this.tblPhotoActions.TabIndex = 1;

            this.btnUploadPhoto.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnUploadPhoto.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUploadPhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUploadPhoto.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnUploadPhoto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUploadPhoto.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnUploadPhoto.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.btnUploadPhoto.Location = new System.Drawing.Point(0, 0);
            this.btnUploadPhoto.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnUploadPhoto.Name = "btnUploadPhoto";
            this.btnUploadPhoto.Size = new System.Drawing.Size(406, 37);
            this.btnUploadPhoto.TabIndex = 0;
            this.btnUploadPhoto.Text = "🖼️ Upload Photo Evidence";
            this.btnUploadPhoto.UseVisualStyleBackColor = false;
            this.btnUploadPhoto.Click += new System.EventHandler(this.BtnUploadPhoto_Click);

            this.tblViewRemovePhoto.ColumnCount = 2;
            this.tblViewRemovePhoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblViewRemovePhoto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblViewRemovePhoto.Controls.Add(this.btnViewPhoto, 0, 0);
            this.tblViewRemovePhoto.Controls.Add(this.btnRemovePhoto, 1, 0);
            this.tblViewRemovePhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblViewRemovePhoto.Location = new System.Drawing.Point(0, 41);
            this.tblViewRemovePhoto.Margin = new System.Windows.Forms.Padding(0);
            this.tblViewRemovePhoto.Name = "tblViewRemovePhoto";
            this.tblViewRemovePhoto.RowCount = 1;
            this.tblViewRemovePhoto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblViewRemovePhoto.Size = new System.Drawing.Size(406, 41);
            this.tblViewRemovePhoto.TabIndex = 1;

            this.btnViewPhoto.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnViewPhoto.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnViewPhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnViewPhoto.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnViewPhoto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewPhoto.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnViewPhoto.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.btnViewPhoto.Location = new System.Drawing.Point(0, 4);
            this.btnViewPhoto.Margin = new System.Windows.Forms.Padding(0, 4, 3, 0);
            this.btnViewPhoto.Name = "btnViewPhoto";
            this.btnViewPhoto.Size = new System.Drawing.Size(200, 37);
            this.btnViewPhoto.TabIndex = 0;
            this.btnViewPhoto.Text = "👁️ View Full";
            this.btnViewPhoto.UseVisualStyleBackColor = false;
            this.btnViewPhoto.Click += new System.EventHandler(this.BtnViewPhoto_Click);

            this.btnRemovePhoto.BackColor = System.Drawing.Color.FromArgb(254, 226, 226);
            this.btnRemovePhoto.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRemovePhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRemovePhoto.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(252, 165, 165);
            this.btnRemovePhoto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemovePhoto.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnRemovePhoto.ForeColor = System.Drawing.Color.FromArgb(153, 27, 27);
            this.btnRemovePhoto.Location = new System.Drawing.Point(206, 4);
            this.btnRemovePhoto.Margin = new System.Windows.Forms.Padding(3, 4, 0, 0);
            this.btnRemovePhoto.Name = "btnRemovePhoto";
            this.btnRemovePhoto.Size = new System.Drawing.Size(200, 37);
            this.btnRemovePhoto.TabIndex = 1;
            this.btnRemovePhoto.Text = "🗑️ Remove";
            this.btnRemovePhoto.UseVisualStyleBackColor = false;
            this.btnRemovePhoto.Click += new System.EventHandler(this.BtnRemovePhoto_Click);

            this.btnSave.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 4);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(448, 40);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "🚨 FILE DAMAGE REPORT";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 50);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(448, 38);
            this.tblUpdateDelete.TabIndex = 1;

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
            this.btnUpdate.Size = new System.Drawing.Size(220, 38);
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
            this.btnDelete.Location = new System.Drawing.Point(228, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(220, 38);
            this.btnDelete.TabIndex = 1;
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
            this.btnClear.Margin = new System.Windows.Forms.Padding(0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(448, 36);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "🧹 CLEAR";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.pnlFormActionsBar.BackColor = System.Drawing.Color.White;
            this.pnlFormActionsBar.Controls.Add(this.btnClear);
            this.pnlFormActionsBar.Controls.Add(this.tblUpdateDelete);
            this.pnlFormActionsBar.Controls.Add(this.btnSave);
            this.pnlFormActionsBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFormActionsBar.Location = new System.Drawing.Point(0, 650);
            this.pnlFormActionsBar.Name = "pnlFormActionsBar";
            this.pnlFormActionsBar.Padding = new System.Windows.Forms.Padding(16, 4, 16, 8);
            this.pnlFormActionsBar.Size = new System.Drawing.Size(480, 138);
            this.pnlFormActionsBar.TabIndex = 2;

            this.pnlFormBody.AutoScroll = true;
            this.pnlFormBody.BackColor = System.Drawing.Color.White;
            this.pnlFormBody.Controls.Add(this.tblPhotoModule);
            this.pnlFormBody.Controls.Add(this.lblPhoto);
            this.pnlFormBody.Controls.Add(this.txtDescription);
            this.pnlFormBody.Controls.Add(this.lblDesc);
            this.pnlFormBody.Controls.Add(this.chkChargeable);
            this.pnlFormBody.Controls.Add(this.txtCause);
            this.pnlFormBody.Controls.Add(this.lblCause);
            this.pnlFormBody.Controls.Add(this.tblQtyCost);
            this.pnlFormBody.Controls.Add(this.cbDelivery);
            this.pnlFormBody.Controls.Add(this.lblDel);
            this.pnlFormBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFormBody.Location = new System.Drawing.Point(0, 38);
            this.pnlFormBody.Name = "pnlFormBody";
            this.pnlFormBody.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.pnlFormBody.Size = new System.Drawing.Size(480, 612);
            this.pnlFormBody.TabIndex = 1;

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridIncidents);
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
            this.lblGridHeader.Size = new System.Drawing.Size(252, 26);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Incident & Damage Reports Register";

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
            this.txtGridSearch.PlaceholderText = "🔍 Search incident, dispatch #, cause...";
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

            this.gridIncidents.AllowUserToAddRows = false;
            this.gridIncidents.AllowUserToDeleteRows = false;
            this.gridIncidents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridIncidents.BackgroundColor = System.Drawing.Color.White;
            this.gridIncidents.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridIncidents.ColumnHeadersHeight = 36;
            this.gridIncidents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridIncidents.EnableHeadersVisualStyles = false;
            this.gridIncidents.Location = new System.Drawing.Point(16, 60);
            this.gridIncidents.MultiSelect = false;
            this.gridIncidents.Name = "gridIncidents";
            this.gridIncidents.ReadOnly = true;
            this.gridIncidents.RowHeadersVisible = false;
            this.gridIncidents.RowTemplate.Height = 32;
            this.gridIncidents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridIncidents.Size = new System.Drawing.Size(876, 634);
            this.gridIncidents.TabIndex = 1;
            this.gridIncidents.SelectionChanged += new System.EventHandler(this.GridIncidents_SelectionChanged);

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
            this.Name = "IncidentReportView";
            this.pnlFormActionsBar.ResumeLayout(false);
            this.pnlFormBody.ResumeLayout(false);
            this.pnlFormBody.PerformLayout();
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.pnlFormHeaderBar.ResumeLayout(false);
            this.pnlFormHeaderBar.PerformLayout();
            this.tblQtyCost.ResumeLayout(false);
            this.pnlQty.ResumeLayout(false);
            this.pnlQty.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDamagedQty)).EndInit();
            this.pnlCost.ResumeLayout(false);
            this.pnlCost.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEstimatedCost)).EndInit();
            this.tblPhotoModule.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPhoto)).EndInit();
            this.tblPhotoActions.ResumeLayout(false);
            this.tblViewRemovePhoto.ResumeLayout(false);
            this.tblUpdateDelete.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridCard.PerformLayout();
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridIncidents)).EndInit();
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
        private System.Windows.Forms.Label lblDel;
        private System.Windows.Forms.ComboBox cbDelivery;
        private System.Windows.Forms.TableLayoutPanel tblQtyCost;
        private System.Windows.Forms.Panel pnlQty;
        private System.Windows.Forms.Label lblQty;
        private System.Windows.Forms.NumericUpDown numDamagedQty;
        private System.Windows.Forms.Panel pnlCost;
        private System.Windows.Forms.Label lblCost;
        private System.Windows.Forms.NumericUpDown numEstimatedCost;
        private System.Windows.Forms.Label lblCause;
        private System.Windows.Forms.TextBox txtCause;
        private System.Windows.Forms.CheckBox chkChargeable;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Label lblPhoto;
        private System.Windows.Forms.TableLayoutPanel tblPhotoModule;
        private System.Windows.Forms.PictureBox picPhoto;
        private System.Windows.Forms.TableLayoutPanel tblPhotoActions;
        private System.Windows.Forms.Button btnUploadPhoto;
        private System.Windows.Forms.TableLayoutPanel tblViewRemovePhoto;
        private System.Windows.Forms.Button btnViewPhoto;
        private System.Windows.Forms.Button btnRemovePhoto;
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
        private System.Windows.Forms.DataGridView gridIncidents;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
