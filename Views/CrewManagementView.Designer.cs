namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class CrewManagementView
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
            lblTitle = new Label();
            tblMasterContent = new TableLayoutPanel();
            pnlForm = new Panel();
            btnClear = new Button();
            tblUpdateDelete = new TableLayoutPanel();
            btnUpdate = new Button();
            btnDelete = new Button();
            btnSave = new Button();
            tblPhoto = new TableLayoutPanel();
            picPhoto = new PictureBox();
            pnlPhotoButtons = new Panel();
            btnRemovePhoto = new Button();
            btnViewPhoto = new Button();
            btnUploadPhoto = new Button();
            lblPhoto = new Label();
            tblCheckboxes = new TableLayoutPanel();
            chkAvailable = new CheckBox();
            chkIsSeasonal = new CheckBox();
            txtLicense = new TextBox();
            lblLicense = new Label();
            txtContact = new TextBox();
            lblContact = new Label();
            cbRole = new ComboBox();
            lblRole = new Label();
            tblNameFields = new TableLayoutPanel();
            lblLastName = new Label();
            lblFirstName = new Label();
            lblMiddleName = new Label();
            txtLastName = new TextBox();
            txtFirstName = new TextBox();
            txtMiddleName = new TextBox();
            lblFormHeader = new Label();
            pnlGridCard = new Panel();
            gridCrew = new DataGridView();
            pnlGridHeaderBar = new Panel();
            pnlGridActionsFlow = new FlowLayoutPanel();
            txtGridSearch = new TextBox();
            btnClearSearch = new Button();
            btnPrint = new Button();
            lblGridHeader = new Label();
            tblMasterContent.SuspendLayout();
            pnlForm.SuspendLayout();
            tblUpdateDelete.SuspendLayout();
            tblPhoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPhoto).BeginInit();
            pnlPhotoButtons.SuspendLayout();
            tblCheckboxes.SuspendLayout();
            tblNameFields.SuspendLayout();
            pnlGridCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridCrew).BeginInit();
            pnlGridHeaderBar.SuspendLayout();
            pnlGridActionsFlow.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblTitle.Location = new Point(16, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(0, 0, 0, 10);
            lblTitle.Size = new Size(464, 47);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Crew Management (Drivers & Aides)";
            // 
            // tblMasterContent
            // 
            tblMasterContent.ColumnCount = 2;
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480F));
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMasterContent.Controls.Add(pnlForm, 0, 0);
            tblMasterContent.Controls.Add(pnlGridCard, 1, 0);
            tblMasterContent.Dock = DockStyle.Fill;
            tblMasterContent.Location = new Point(16, 63);
            tblMasterContent.Name = "tblMasterContent";
            tblMasterContent.RowCount = 1;
            tblMasterContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblMasterContent.Size = new Size(1388, 741);
            tblMasterContent.TabIndex = 1;
            // 
            // pnlForm
            // 
            pnlForm.AutoScroll = true;
            pnlForm.BackColor = Color.White;
            pnlForm.Controls.Add(btnClear);
            pnlForm.Controls.Add(tblUpdateDelete);
            pnlForm.Controls.Add(btnSave);
            pnlForm.Controls.Add(tblPhoto);
            pnlForm.Controls.Add(lblPhoto);
            pnlForm.Controls.Add(tblCheckboxes);
            pnlForm.Controls.Add(txtLicense);
            pnlForm.Controls.Add(lblLicense);
            pnlForm.Controls.Add(txtContact);
            pnlForm.Controls.Add(lblContact);
            pnlForm.Controls.Add(cbRole);
            pnlForm.Controls.Add(lblRole);
            pnlForm.Controls.Add(tblNameFields);
            pnlForm.Controls.Add(lblFormHeader);
            pnlForm.Dock = DockStyle.Fill;
            pnlForm.Location = new Point(0, 0);
            pnlForm.Margin = new Padding(0, 0, 12, 0);
            pnlForm.Name = "pnlForm";
            pnlForm.Padding = new Padding(16);
            pnlForm.Size = new Size(468, 741);
            pnlForm.TabIndex = 0;
            // 
            // btnClear
            // 
            btnClear.Cursor = Cursors.Hand;
            btnClear.Dock = DockStyle.Top;
            btnClear.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnClear.Location = new Point(16, 528);
            btnClear.Margin = new Padding(0, 4, 0, 6);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(436, 36);
            btnClear.TabIndex = 13;
            btnClear.Text = "\U0001f9f9 Clear Form";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += BtnClear_Click;
            // 
            // tblUpdateDelete
            // 
            tblUpdateDelete.ColumnCount = 2;
            tblUpdateDelete.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblUpdateDelete.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblUpdateDelete.Controls.Add(btnUpdate, 0, 0);
            tblUpdateDelete.Controls.Add(btnDelete, 1, 0);
            tblUpdateDelete.Dock = DockStyle.Top;
            tblUpdateDelete.Location = new Point(16, 488);
            tblUpdateDelete.Margin = new Padding(0, 0, 0, 6);
            tblUpdateDelete.Name = "tblUpdateDelete";
            tblUpdateDelete.RowCount = 1;
            tblUpdateDelete.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblUpdateDelete.Size = new Size(436, 40);
            tblUpdateDelete.TabIndex = 12;
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
            btnUpdate.Text = "✏ Update";
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
            btnDelete.Text = "🗑 Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
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
            btnSave.Location = new Point(16, 448);
            btnSave.Margin = new Padding(0, 8, 0, 8);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(436, 40);
            btnSave.TabIndex = 11;
            btnSave.Text = "Save Crew Member";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += BtnSave_Click;
            // 
            // tblPhoto
            // 
            tblPhoto.ColumnCount = 2;
            tblPhoto.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tblPhoto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblPhoto.Controls.Add(picPhoto, 0, 0);
            tblPhoto.Controls.Add(pnlPhotoButtons, 1, 0);
            tblPhoto.Dock = DockStyle.Top;
            tblPhoto.Location = new Point(16, 330);
            tblPhoto.Margin = new Padding(0, 4, 0, 8);
            tblPhoto.Name = "tblPhoto";
            tblPhoto.RowCount = 1;
            tblPhoto.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblPhoto.Size = new Size(436, 118);
            tblPhoto.TabIndex = 10;
            // 
            // picPhoto
            // 
            picPhoto.BackColor = Color.FromArgb(241, 245, 249);
            picPhoto.BorderStyle = BorderStyle.FixedSingle;
            picPhoto.Dock = DockStyle.Fill;
            picPhoto.Location = new Point(0, 0);
            picPhoto.Margin = new Padding(0, 0, 8, 0);
            picPhoto.Name = "picPhoto";
            picPhoto.Size = new Size(132, 118);
            picPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            picPhoto.TabIndex = 0;
            picPhoto.TabStop = false;
            // 
            // pnlPhotoButtons
            // 
            pnlPhotoButtons.Controls.Add(btnRemovePhoto);
            pnlPhotoButtons.Controls.Add(btnViewPhoto);
            pnlPhotoButtons.Controls.Add(btnUploadPhoto);
            pnlPhotoButtons.Dock = DockStyle.Fill;
            pnlPhotoButtons.Location = new Point(140, 0);
            pnlPhotoButtons.Margin = new Padding(0);
            pnlPhotoButtons.Name = "pnlPhotoButtons";
            pnlPhotoButtons.Size = new Size(296, 118);
            pnlPhotoButtons.TabIndex = 1;
            // 
            // btnRemovePhoto
            // 
            btnRemovePhoto.Dock = DockStyle.Top;
            btnRemovePhoto.Font = new Font("Segoe UI", 9F);
            btnRemovePhoto.Location = new Point(0, 68);
            btnRemovePhoto.Margin = new Padding(0, 4, 0, 0);
            btnRemovePhoto.Name = "btnRemovePhoto";
            btnRemovePhoto.Size = new Size(296, 34);
            btnRemovePhoto.TabIndex = 2;
            btnRemovePhoto.Text = "🗑 Remove Photo";
            btnRemovePhoto.UseVisualStyleBackColor = true;
            btnRemovePhoto.Click += BtnRemovePhoto_Click;
            // 
            // btnViewPhoto
            // 
            btnViewPhoto.Dock = DockStyle.Top;
            btnViewPhoto.Font = new Font("Segoe UI", 9F);
            btnViewPhoto.Location = new Point(0, 34);
            btnViewPhoto.Margin = new Padding(0, 4, 0, 4);
            btnViewPhoto.Name = "btnViewPhoto";
            btnViewPhoto.Size = new Size(296, 34);
            btnViewPhoto.TabIndex = 1;
            btnViewPhoto.Text = "🔍 View Full Photo";
            btnViewPhoto.UseVisualStyleBackColor = true;
            btnViewPhoto.Click += BtnViewPhoto_Click;
            // 
            // btnUploadPhoto
            // 
            btnUploadPhoto.Dock = DockStyle.Top;
            btnUploadPhoto.Font = new Font("Segoe UI", 9F);
            btnUploadPhoto.Location = new Point(0, 0);
            btnUploadPhoto.Margin = new Padding(0, 0, 0, 4);
            btnUploadPhoto.Name = "btnUploadPhoto";
            btnUploadPhoto.Size = new Size(296, 34);
            btnUploadPhoto.TabIndex = 0;
            btnUploadPhoto.Text = "📁 Upload Photo...";
            btnUploadPhoto.UseVisualStyleBackColor = true;
            btnUploadPhoto.Click += BtnUploadPhoto_Click;
            // 
            // lblPhoto
            // 
            lblPhoto.AutoSize = true;
            lblPhoto.Dock = DockStyle.Top;
            lblPhoto.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblPhoto.ForeColor = Color.FromArgb(30, 41, 59);
            lblPhoto.Location = new Point(16, 303);
            lblPhoto.Name = "lblPhoto";
            lblPhoto.Padding = new Padding(0, 4, 0, 2);
            lblPhoto.Size = new Size(193, 27);
            lblPhoto.TabIndex = 9;
            lblPhoto.Text = "Official ID / Driver Photo:";
            // 
            // tblCheckboxes
            // 
            tblCheckboxes.ColumnCount = 2;
            tblCheckboxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCheckboxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCheckboxes.Controls.Add(chkAvailable, 0, 0);
            tblCheckboxes.Controls.Add(chkIsSeasonal, 1, 0);
            tblCheckboxes.Dock = DockStyle.Top;
            tblCheckboxes.Location = new Point(16, 273);
            tblCheckboxes.Margin = new Padding(0, 4, 0, 8);
            tblCheckboxes.Name = "tblCheckboxes";
            tblCheckboxes.RowCount = 1;
            tblCheckboxes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblCheckboxes.Size = new Size(436, 30);
            tblCheckboxes.TabIndex = 8;
            // 
            // chkAvailable
            // 
            chkAvailable.AutoSize = true;
            chkAvailable.Checked = true;
            chkAvailable.CheckState = CheckState.Checked;
            chkAvailable.Dock = DockStyle.Fill;
            chkAvailable.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            chkAvailable.ForeColor = Color.FromArgb(30, 41, 59);
            chkAvailable.Location = new Point(0, 0);
            chkAvailable.Margin = new Padding(0, 0, 4, 0);
            chkAvailable.Name = "chkAvailable";
            chkAvailable.Size = new Size(214, 30);
            chkAvailable.TabIndex = 0;
            chkAvailable.Text = "Is Available for Duty";
            chkAvailable.UseVisualStyleBackColor = true;
            // 
            // chkIsSeasonal
            // 
            chkIsSeasonal.AutoSize = true;
            chkIsSeasonal.Dock = DockStyle.Fill;
            chkIsSeasonal.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            chkIsSeasonal.ForeColor = Color.FromArgb(30, 41, 59);
            chkIsSeasonal.Location = new Point(222, 0);
            chkIsSeasonal.Margin = new Padding(4, 0, 0, 0);
            chkIsSeasonal.Name = "chkIsSeasonal";
            chkIsSeasonal.Size = new Size(214, 30);
            chkIsSeasonal.TabIndex = 1;
            chkIsSeasonal.Text = "Seasonal / Temporary";
            chkIsSeasonal.UseVisualStyleBackColor = true;
            // 
            // txtLicense
            // 
            txtLicense.CharacterCasing = CharacterCasing.Upper;
            txtLicense.Dock = DockStyle.Top;
            txtLicense.Font = new Font("Segoe UI", 9.5F);
            txtLicense.Location = new Point(16, 244);
            txtLicense.Margin = new Padding(0, 0, 0, 8);
            txtLicense.Name = "txtLicense";
            txtLicense.PlaceholderText = "e.g. N01-23-456789";
            txtLicense.Size = new Size(436, 29);
            txtLicense.TabIndex = 7;
            // 
            // lblLicense
            // 
            lblLicense.AutoSize = true;
            lblLicense.Dock = DockStyle.Top;
            lblLicense.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblLicense.ForeColor = Color.FromArgb(30, 41, 59);
            lblLicense.Location = new Point(16, 217);
            lblLicense.Name = "lblLicense";
            lblLicense.Padding = new Padding(0, 4, 0, 2);
            lblLicense.Size = new Size(294, 27);
            lblLicense.TabIndex = 6;
            lblLicense.Text = "Driver's License # (Required for Driver):";
            // 
            // txtContact
            // 
            txtContact.Dock = DockStyle.Top;
            txtContact.Font = new Font("Segoe UI", 9.5F);
            txtContact.Location = new Point(16, 188);
            txtContact.Margin = new Padding(0, 0, 0, 8);
            txtContact.Name = "txtContact";
            txtContact.PlaceholderText = "e.g. 09171234567";
            txtContact.Size = new Size(436, 29);
            txtContact.TabIndex = 5;
            // 
            // lblContact
            // 
            lblContact.AutoSize = true;
            lblContact.Dock = DockStyle.Top;
            lblContact.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblContact.ForeColor = Color.FromArgb(30, 41, 59);
            lblContact.Location = new Point(16, 161);
            lblContact.Name = "lblContact";
            lblContact.Padding = new Padding(0, 4, 0, 2);
            lblContact.Size = new Size(273, 27);
            lblContact.TabIndex = 4;
            lblContact.Text = "Contact Number (e.g. 09171234567):";
            // 
            // cbRole
            // 
            cbRole.Dock = DockStyle.Top;
            cbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRole.Font = new Font("Segoe UI", 9.5F);
            cbRole.FormattingEnabled = true;
            cbRole.Items.AddRange(new object[] { "Driver", "Aide/Helper" });
            cbRole.Location = new Point(16, 132);
            cbRole.Margin = new Padding(0, 0, 0, 8);
            cbRole.Name = "cbRole";
            cbRole.Size = new Size(436, 29);
            cbRole.TabIndex = 3;
            // 
            // lblRole
            // 
            lblRole.AutoSize = true;
            lblRole.Dock = DockStyle.Top;
            lblRole.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblRole.ForeColor = Color.FromArgb(30, 41, 59);
            lblRole.Location = new Point(16, 105);
            lblRole.Name = "lblRole";
            lblRole.Padding = new Padding(0, 4, 0, 2);
            lblRole.Size = new Size(99, 27);
            lblRole.TabIndex = 2;
            lblRole.Text = "Crew Role *:";
            // 
            // tblNameFields
            // 
            tblNameFields.ColumnCount = 3;
            tblNameFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tblNameFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            tblNameFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tblNameFields.Controls.Add(lblLastName, 0, 0);
            tblNameFields.Controls.Add(lblFirstName, 1, 0);
            tblNameFields.Controls.Add(lblMiddleName, 2, 0);
            tblNameFields.Controls.Add(txtLastName, 0, 1);
            tblNameFields.Controls.Add(txtFirstName, 1, 1);
            tblNameFields.Controls.Add(txtMiddleName, 2, 1);
            tblNameFields.Dock = DockStyle.Top;
            tblNameFields.Location = new Point(16, 49);
            tblNameFields.Margin = new Padding(0, 4, 0, 6);
            tblNameFields.Name = "tblNameFields";
            tblNameFields.RowCount = 2;
            tblNameFields.RowStyles.Add(new RowStyle());
            tblNameFields.RowStyles.Add(new RowStyle());
            tblNameFields.Size = new Size(436, 56);
            tblNameFields.TabIndex = 1;
            // 
            // lblLastName
            // 
            lblLastName.AutoSize = true;
            lblLastName.Dock = DockStyle.Fill;
            lblLastName.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblLastName.ForeColor = Color.FromArgb(30, 41, 59);
            lblLastName.Location = new Point(0, 0);
            lblLastName.Margin = new Padding(0, 0, 4, 2);
            lblLastName.Name = "lblLastName";
            lblLastName.Size = new Size(141, 20);
            lblLastName.TabIndex = 0;
            lblLastName.Text = "Last Name *:";
            // 
            // lblFirstName
            // 
            lblFirstName.AutoSize = true;
            lblFirstName.Dock = DockStyle.Fill;
            lblFirstName.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblFirstName.ForeColor = Color.FromArgb(30, 41, 59);
            lblFirstName.Location = new Point(149, 0);
            lblFirstName.Margin = new Padding(4, 0, 4, 2);
            lblFirstName.Name = "lblFirstName";
            lblFirstName.Size = new Size(137, 20);
            lblFirstName.TabIndex = 1;
            lblFirstName.Text = "First Name *:";
            // 
            // lblMiddleName
            // 
            lblMiddleName.AutoSize = true;
            lblMiddleName.Dock = DockStyle.Fill;
            lblMiddleName.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblMiddleName.ForeColor = Color.FromArgb(30, 41, 59);
            lblMiddleName.Location = new Point(294, 0);
            lblMiddleName.Margin = new Padding(4, 0, 0, 2);
            lblMiddleName.Name = "lblMiddleName";
            lblMiddleName.Size = new Size(142, 20);
            lblMiddleName.TabIndex = 2;
            lblMiddleName.Text = "Middle Name:";
            // 
            // txtLastName
            // 
            txtLastName.Dock = DockStyle.Fill;
            txtLastName.Font = new Font("Segoe UI", 9.5F);
            txtLastName.Location = new Point(0, 24);
            txtLastName.Margin = new Padding(0, 2, 4, 6);
            txtLastName.Name = "txtLastName";
            txtLastName.PlaceholderText = "e.g. Santos";
            txtLastName.Size = new Size(141, 29);
            txtLastName.TabIndex = 3;
            // 
            // txtFirstName
            // 
            txtFirstName.Dock = DockStyle.Fill;
            txtFirstName.Font = new Font("Segoe UI", 9.5F);
            txtFirstName.Location = new Point(149, 24);
            txtFirstName.Margin = new Padding(4, 2, 4, 6);
            txtFirstName.Name = "txtFirstName";
            txtFirstName.PlaceholderText = "e.g. Juan";
            txtFirstName.Size = new Size(137, 29);
            txtFirstName.TabIndex = 4;
            // 
            // txtMiddleName
            // 
            txtMiddleName.Dock = DockStyle.Fill;
            txtMiddleName.Font = new Font("Segoe UI", 9.5F);
            txtMiddleName.Location = new Point(294, 24);
            txtMiddleName.Margin = new Padding(4, 2, 0, 6);
            txtMiddleName.Name = "txtMiddleName";
            txtMiddleName.PlaceholderText = "e.g. Cruz";
            txtMiddleName.Size = new Size(142, 29);
            txtMiddleName.TabIndex = 5;
            // 
            // lblFormHeader
            // 
            lblFormHeader.AutoSize = true;
            lblFormHeader.Dock = DockStyle.Top;
            lblFormHeader.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblFormHeader.ForeColor = Color.FromArgb(30, 41, 59);
            lblFormHeader.Location = new Point(16, 16);
            lblFormHeader.Name = "lblFormHeader";
            lblFormHeader.Padding = new Padding(0, 0, 0, 8);
            lblFormHeader.Size = new Size(193, 33);
            lblFormHeader.TabIndex = 0;
            lblFormHeader.Text = "Crew Personnel Form";
            // 
            // pnlGridCard
            // 
            pnlGridCard.BackColor = Color.White;
            pnlGridCard.Controls.Add(gridCrew);
            pnlGridCard.Controls.Add(pnlGridHeaderBar);
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Location = new Point(480, 0);
            pnlGridCard.Margin = new Padding(0);
            pnlGridCard.MinimumSize = new Size(480, 300);
            pnlGridCard.Name = "pnlGridCard";
            pnlGridCard.Padding = new Padding(16);
            pnlGridCard.Size = new Size(908, 741);
            pnlGridCard.TabIndex = 1;
            // 
            // gridCrew
            // 
            gridCrew.AllowUserToAddRows = false;
            gridCrew.AllowUserToDeleteRows = false;
            gridCrew.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCrew.BackgroundColor = Color.FromArgb(248, 250, 252);
            gridCrew.BorderStyle = BorderStyle.None;
            gridCrew.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridCrew.Dock = DockStyle.Fill;
            gridCrew.EditMode = DataGridViewEditMode.EditOnEnter;
            gridCrew.EnableHeadersVisualStyles = false;
            gridCrew.Location = new Point(16, 90);
            gridCrew.MultiSelect = false;
            gridCrew.Name = "gridCrew";
            gridCrew.RowHeadersVisible = false;
            gridCrew.RowHeadersWidth = 51;
            gridCrew.RowTemplate.Height = 34;
            gridCrew.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridCrew.Size = new Size(876, 635);
            gridCrew.TabIndex = 1;
            gridCrew.SelectionChanged += GridCrew_SelectionChanged;
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
            txtGridSearch.Name = "txtGridSearch";
            txtGridSearch.PlaceholderText = "🔍 Search crew, role, contact...";
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
            btnPrint.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnPrint.ForeColor = Color.White;
            btnPrint.Location = new Point(268, 4);
            btnPrint.Margin = new Padding(4, 4, 0, 0);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(160, 28);
            btnPrint.TabIndex = 2;
            btnPrint.Text = "🖨️ Print Roster";
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
            lblGridHeader.Size = new Size(202, 33);
            lblGridHeader.TabIndex = 0;
            lblGridHeader.Text = "Active Crew Personnel";
            // 
            // CrewManagementView
            // 
            AutoScroll = true;
            AutoScrollMinSize = new Size(1000, 620);
            BackColor = Color.FromArgb(241, 245, 249);
            Controls.Add(tblMasterContent);
            Controls.Add(lblTitle);
            Font = new Font("Segoe UI", 10F);
            Name = "CrewManagementView";
            Padding = new Padding(16);
            Size = new Size(1420, 820);
            tblMasterContent.ResumeLayout(false);
            pnlForm.ResumeLayout(false);
            pnlForm.PerformLayout();
            tblUpdateDelete.ResumeLayout(false);
            tblPhoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picPhoto).EndInit();
            pnlPhotoButtons.ResumeLayout(false);
            tblCheckboxes.ResumeLayout(false);
            tblCheckboxes.PerformLayout();
            tblNameFields.ResumeLayout(false);
            tblNameFields.PerformLayout();
            pnlGridCard.ResumeLayout(false);
            pnlGridCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridCrew).EndInit();
            pnlGridHeaderBar.ResumeLayout(false);
            pnlGridHeaderBar.PerformLayout();
            pnlGridActionsFlow.ResumeLayout(false);
            pnlGridActionsFlow.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel tblMasterContent;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.TableLayoutPanel tblNameFields;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.Label lblRole;
        private System.Windows.Forms.ComboBox cbRole;
        private System.Windows.Forms.Label lblContact;
        private System.Windows.Forms.TextBox txtContact;
        private System.Windows.Forms.Label lblLicense;
        private System.Windows.Forms.TextBox txtLicense;
        private System.Windows.Forms.TableLayoutPanel tblCheckboxes;
        private System.Windows.Forms.CheckBox chkAvailable;
        private System.Windows.Forms.CheckBox chkIsSeasonal;
        private System.Windows.Forms.Label lblPhoto;
        private System.Windows.Forms.TableLayoutPanel tblPhoto;
        private System.Windows.Forms.PictureBox picPhoto;
        private System.Windows.Forms.Panel pnlPhotoButtons;
        private System.Windows.Forms.Button btnUploadPhoto;
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
        private System.Windows.Forms.DataGridView gridCrew;
    }
}
