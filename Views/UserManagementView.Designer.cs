namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class UserManagementView
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
            this.pnlFormActionsBar = new System.Windows.Forms.Panel();
            this.pnlFormBody = new System.Windows.Forms.Panel();
            this.pnlFormHeaderBar = new System.Windows.Forms.Panel();
            this.lblFormHeader = new System.Windows.Forms.Label();
            this.lblU = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblP = new System.Windows.Forms.Label();
            this.tblPasswordRow = new System.Windows.Forms.TableLayoutPanel();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkShowPassword = new System.Windows.Forms.CheckBox();
            this.lblPassHint = new System.Windows.Forms.Label();
            this.lblR = new System.Windows.Forms.Label();
            this.cbRole = new System.Windows.Forms.ComboBox();
            this.tblNameRow = new System.Windows.Forms.TableLayoutPanel();
            this.pnlLastName = new System.Windows.Forms.Panel();
            this.lblLastName = new System.Windows.Forms.Label();
            this.txtLastName = new System.Windows.Forms.TextBox();
            this.pnlFirstName = new System.Windows.Forms.Panel();
            this.lblFirstName = new System.Windows.Forms.Label();
            this.txtFirstName = new System.Windows.Forms.TextBox();
            this.pnlMiddleName = new System.Windows.Forms.Panel();
            this.lblMiddleName = new System.Windows.Forms.Label();
            this.txtMiddleName = new System.Windows.Forms.TextBox();
            this.lblC = new System.Windows.Forms.Label();
            this.txtContact = new System.Windows.Forms.TextBox();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.btnApprove = new System.Windows.Forms.Button();
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
            this.gridUsers = new System.Windows.Forms.DataGridView();
            this.tblMasterContent.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.pnlFormActionsBar.SuspendLayout();
            this.pnlFormBody.SuspendLayout();
            this.pnlFormHeaderBar.SuspendLayout();
            this.tblPasswordRow.SuspendLayout();
            this.tblNameRow.SuspendLayout();
            this.pnlLastName.SuspendLayout();
            this.pnlFirstName.SuspendLayout();
            this.pnlMiddleName.SuspendLayout();
            this.tblUpdateDelete.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeaderBar.SuspendLayout();
            this.pnlGridActionsFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).BeginInit();
            this.SuspendLayout();

            this.AutoScroll = false;
            this.AutoScrollMinSize = System.Drawing.Size.Empty;
            this.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(1420, 820);

            this.lblTitle.AutoSize = false;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new System.Drawing.Point(16, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.lblTitle.Size = System.Drawing.Size.Empty;
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "User Accounts & Security";
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
            this.lblFormHeader.Text = "User Credentials Form";

            this.lblU.AutoSize = true;
            this.lblU.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblU.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblU.Location = new System.Drawing.Point(16, 60);
            this.lblU.Name = "lblU";
            this.lblU.Size = new System.Drawing.Size(72, 17);
            this.lblU.TabIndex = 1;
            this.lblU.Text = "Username:";

            this.txtUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUsername.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtUsername.Location = new System.Drawing.Point(16, 82);
            this.txtUsername.MaxLength = 50;
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(432, 24);
            this.txtUsername.TabIndex = 2;

            this.lblP.AutoSize = true;
            this.lblP.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblP.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblP.Location = new System.Drawing.Point(16, 116);
            this.lblP.Name = "lblP";
            this.lblP.Size = new System.Drawing.Size(69, 17);
            this.lblP.TabIndex = 3;
            this.lblP.Text = "Password:";

            this.tblPasswordRow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblPasswordRow.ColumnCount = 2;
            this.tblPasswordRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tblPasswordRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblPasswordRow.Controls.Add(this.txtPassword, 0, 0);
            this.tblPasswordRow.Controls.Add(this.chkShowPassword, 1, 0);
            this.tblPasswordRow.Location = new System.Drawing.Point(16, 138);
            this.tblPasswordRow.Margin = new System.Windows.Forms.Padding(0);
            this.tblPasswordRow.Name = "tblPasswordRow";
            this.tblPasswordRow.RowCount = 1;
            this.tblPasswordRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPasswordRow.Size = new System.Drawing.Size(432, 28);
            this.tblPasswordRow.TabIndex = 4;

            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtPassword.Location = new System.Drawing.Point(0, 0);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.txtPassword.MaxLength = 128;
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.Size = new System.Drawing.Size(272, 24);
            this.txtPassword.TabIndex = 0;

            this.chkShowPassword.AutoSize = true;
            this.chkShowPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkShowPassword.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.chkShowPassword.Location = new System.Drawing.Point(280, 0);
            this.chkShowPassword.Margin = new System.Windows.Forms.Padding(0);
            this.chkShowPassword.Name = "chkShowPassword";
            this.chkShowPassword.Size = new System.Drawing.Size(152, 28);
            this.chkShowPassword.TabIndex = 1;
            this.chkShowPassword.Text = "Show Password";
            this.chkShowPassword.UseVisualStyleBackColor = true;

            this.lblPassHint.AutoSize = true;
            this.lblPassHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPassHint.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblPassHint.Location = new System.Drawing.Point(16, 170);
            this.lblPassHint.Name = "lblPassHint";
            this.lblPassHint.Size = new System.Drawing.Size(340, 15);
            this.lblPassHint.TabIndex = 5;
            this.lblPassHint.Text = "Min 6 characters with both letters and numbers (e.g. admin123)";

            this.lblR.AutoSize = true;
            this.lblR.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblR.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblR.Location = new System.Drawing.Point(16, 194);
            this.lblR.Name = "lblR";
            this.lblR.Size = new System.Drawing.Size(68, 17);
            this.lblR.TabIndex = 6;
            this.lblR.Text = "User Role:";

            this.cbRole.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRole.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cbRole.FormattingEnabled = true;
            this.cbRole.Items.AddRange(new object[] {
            "Owner/Admin",
            "Dispatcher",
            "Billing Clerk",
            "Office Staff"});
            this.cbRole.Location = new System.Drawing.Point(16, 216);
            this.cbRole.Name = "cbRole";
            this.cbRole.Size = new System.Drawing.Size(432, 25);
            this.cbRole.TabIndex = 7;

            this.tblNameRow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblNameRow.ColumnCount = 3;
            this.tblNameRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tblNameRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.tblNameRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tblNameRow.Controls.Add(this.pnlLastName, 0, 0);
            this.tblNameRow.Controls.Add(this.pnlFirstName, 1, 0);
            this.tblNameRow.Controls.Add(this.pnlMiddleName, 2, 0);
            this.tblNameRow.Location = new System.Drawing.Point(16, 252);
            this.tblNameRow.Margin = new System.Windows.Forms.Padding(0);
            this.tblNameRow.Name = "tblNameRow";
            this.tblNameRow.RowCount = 1;
            this.tblNameRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblNameRow.Size = new System.Drawing.Size(432, 54);
            this.tblNameRow.TabIndex = 8;

            this.pnlLastName.Controls.Add(this.txtLastName);
            this.pnlLastName.Controls.Add(this.lblLastName);
            this.pnlLastName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLastName.Location = new System.Drawing.Point(0, 0);
            this.pnlLastName.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.pnlLastName.Name = "pnlLastName";
            this.pnlLastName.Size = new System.Drawing.Size(139, 54);
            this.pnlLastName.TabIndex = 0;

            this.lblLastName.AutoSize = true;
            this.lblLastName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLastName.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblLastName.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblLastName.Location = new System.Drawing.Point(0, 0);
            this.lblLastName.Name = "lblLastName";
            this.lblLastName.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblLastName.Size = new System.Drawing.Size(76, 18);
            this.lblLastName.TabIndex = 0;
            this.lblLastName.Text = "Last Name *:";

            this.txtLastName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtLastName.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtLastName.Location = new System.Drawing.Point(0, 18);
            this.txtLastName.MaxLength = 100;
            this.txtLastName.Name = "txtLastName";
            this.txtLastName.Size = new System.Drawing.Size(139, 24);
            this.txtLastName.TabIndex = 1;

            this.pnlFirstName.Controls.Add(this.txtFirstName);
            this.pnlFirstName.Controls.Add(this.lblFirstName);
            this.pnlFirstName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFirstName.Location = new System.Drawing.Point(143, 0);
            this.pnlFirstName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.pnlFirstName.Name = "pnlFirstName";
            this.pnlFirstName.Size = new System.Drawing.Size(145, 54);
            this.pnlFirstName.TabIndex = 1;

            this.lblFirstName.AutoSize = true;
            this.lblFirstName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFirstName.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblFirstName.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblFirstName.Location = new System.Drawing.Point(0, 0);
            this.lblFirstName.Name = "lblFirstName";
            this.lblFirstName.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblFirstName.Size = new System.Drawing.Size(77, 18);
            this.lblFirstName.TabIndex = 0;
            this.lblFirstName.Text = "First Name *:";

            this.txtFirstName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtFirstName.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtFirstName.Location = new System.Drawing.Point(0, 18);
            this.txtFirstName.MaxLength = 100;
            this.txtFirstName.Name = "txtFirstName";
            this.txtFirstName.Size = new System.Drawing.Size(145, 24);
            this.txtFirstName.TabIndex = 1;

            this.pnlMiddleName.Controls.Add(this.txtMiddleName);
            this.pnlMiddleName.Controls.Add(this.lblMiddleName);
            this.pnlMiddleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMiddleName.Location = new System.Drawing.Point(292, 0);
            this.pnlMiddleName.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.pnlMiddleName.Name = "pnlMiddleName";
            this.pnlMiddleName.Size = new System.Drawing.Size(140, 54);
            this.pnlMiddleName.TabIndex = 2;

            this.lblMiddleName.AutoSize = true;
            this.lblMiddleName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMiddleName.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblMiddleName.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblMiddleName.Location = new System.Drawing.Point(0, 0);
            this.lblMiddleName.Name = "lblMiddleName";
            this.lblMiddleName.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lblMiddleName.Size = new System.Drawing.Size(84, 18);
            this.lblMiddleName.TabIndex = 0;
            this.lblMiddleName.Text = "Middle Name:";

            this.txtMiddleName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtMiddleName.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtMiddleName.Location = new System.Drawing.Point(0, 18);
            this.txtMiddleName.MaxLength = 100;
            this.txtMiddleName.Name = "txtMiddleName";
            this.txtMiddleName.Size = new System.Drawing.Size(140, 24);
            this.txtMiddleName.TabIndex = 1;

            this.lblC.AutoSize = true;
            this.lblC.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblC.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblC.Location = new System.Drawing.Point(16, 314);
            this.lblC.Name = "lblC";
            this.lblC.Size = new System.Drawing.Size(111, 17);
            this.lblC.TabIndex = 9;
            this.lblC.Text = "Contact Number:";

            this.txtContact.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtContact.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtContact.Location = new System.Drawing.Point(16, 336);
            this.txtContact.MaxLength = 20;
            this.txtContact.Name = "txtContact";
            this.txtContact.Size = new System.Drawing.Size(432, 24);
            this.txtContact.TabIndex = 10;

            this.chkActive.AutoSize = true;
            this.chkActive.Checked = true;
            this.chkActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkActive.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.chkActive.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.chkActive.Location = new System.Drawing.Point(16, 370);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(154, 21);
            this.chkActive.TabIndex = 11;
            this.chkActive.Text = "Active User Account";
            this.chkActive.UseVisualStyleBackColor = true;

            this.btnApprove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApprove.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnApprove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApprove.FlatAppearance.BorderSize = 0;
            this.btnApprove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApprove.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnApprove.ForeColor = System.Drawing.Color.White;
            this.btnApprove.Location = new System.Drawing.Point(16, 404);
            this.btnApprove.Name = "btnApprove";
            this.btnApprove.Size = new System.Drawing.Size(432, 40);
            this.btnApprove.TabIndex = 12;
            this.btnApprove.Text = "✅  APPROVE STAFF ACCOUNT";
            this.btnApprove.UseVisualStyleBackColor = false;
            this.btnApprove.Visible = false;
            this.btnApprove.Click += new System.EventHandler(this.BtnApprove_Click);

            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 404);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(432, 42);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save User Account";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.tblUpdateDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblUpdateDelete.ColumnCount = 2;
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUpdateDelete.Controls.Add(this.btnUpdate, 0, 0);
            this.tblUpdateDelete.Controls.Add(this.btnDelete, 1, 0);
            this.tblUpdateDelete.Location = new System.Drawing.Point(16, 454);
            this.tblUpdateDelete.Margin = new System.Windows.Forms.Padding(0);
            this.tblUpdateDelete.Name = "tblUpdateDelete";
            this.tblUpdateDelete.RowCount = 1;
            this.tblUpdateDelete.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUpdateDelete.Size = new System.Drawing.Size(432, 40);
            this.tblUpdateDelete.TabIndex = 14;

            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(5, 150, 105);
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
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(220, 38, 38);
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
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
            this.btnClear.Location = new System.Drawing.Point(16, 502);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(432, 36);
            this.btnClear.TabIndex = 15;
            this.btnClear.Text = "Clear Form";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.btnApprove.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblUpdateDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Top;
            
            System.Windows.Forms.Panel pnlApproveSpacer = new System.Windows.Forms.Panel();
            pnlApproveSpacer.Height = 8;
            pnlApproveSpacer.Dock = System.Windows.Forms.DockStyle.Top;
            System.Windows.Forms.Panel pnlSaveSpacer = new System.Windows.Forms.Panel();
            pnlSaveSpacer.Height = 8;
            pnlSaveSpacer.Dock = System.Windows.Forms.DockStyle.Top;
            System.Windows.Forms.Panel pnlUpdateSpacer = new System.Windows.Forms.Panel();
            pnlUpdateSpacer.Height = 8;
            pnlUpdateSpacer.Dock = System.Windows.Forms.DockStyle.Top;

            this.pnlFormActionsBar.Controls.Add(this.btnClear);
            this.pnlFormActionsBar.Controls.Add(pnlUpdateSpacer);
            this.pnlFormActionsBar.Controls.Add(this.tblUpdateDelete);
            this.pnlFormActionsBar.Controls.Add(pnlSaveSpacer);
            this.pnlFormActionsBar.Controls.Add(this.btnSave);
            this.pnlFormActionsBar.Controls.Add(pnlApproveSpacer);
            this.pnlFormActionsBar.Controls.Add(this.btnApprove);
            this.pnlFormActionsBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFormActionsBar.Location = new System.Drawing.Point(0, 560);
            this.pnlFormActionsBar.Name = "pnlFormActionsBar";
            this.pnlFormActionsBar.Padding = new System.Windows.Forms.Padding(16, 4, 16, 8);
            this.pnlFormActionsBar.Size = new System.Drawing.Size(480, 188);
            this.pnlFormActionsBar.TabIndex = 2;

            this.pnlFormBody.AutoScroll = true;
            this.pnlFormBody.Controls.Add(this.chkActive);
            this.pnlFormBody.Controls.Add(this.txtContact);
            this.pnlFormBody.Controls.Add(this.lblC);
            this.pnlFormBody.Controls.Add(this.tblNameRow);
            this.pnlFormBody.Controls.Add(this.cbRole);
            this.pnlFormBody.Controls.Add(this.lblR);
            this.pnlFormBody.Controls.Add(this.lblPassHint);
            this.pnlFormBody.Controls.Add(this.tblPasswordRow);
            this.pnlFormBody.Controls.Add(this.lblP);
            this.pnlFormBody.Controls.Add(this.txtUsername);
            this.pnlFormBody.Controls.Add(this.lblU);
            this.pnlFormBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFormBody.Location = new System.Drawing.Point(0, 38);
            this.pnlFormBody.Name = "pnlFormBody";
            this.pnlFormBody.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.pnlFormBody.Size = new System.Drawing.Size(480, 522);
            this.pnlFormBody.TabIndex = 1;

            this.pnlForm.Controls.Add(this.pnlFormBody);
            this.pnlForm.Controls.Add(this.pnlFormActionsBar);
            this.pnlForm.Controls.Add(this.pnlFormHeaderBar);

            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridUsers);
            this.pnlGridCard.Controls.Add(this.pnlGridHeaderBar);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(480, 0);
            this.pnlGridCard.Margin = new System.Windows.Forms.Padding(0);
            this.pnlGridCard.MinimumSize = new System.Drawing.Size(480, 300);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(808, 748);
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
            this.lblGridHeader.Size = new System.Drawing.Size(160, 26);
            this.lblGridHeader.TabIndex = 0;
            this.lblGridHeader.Text = "Active User Accounts";

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
            this.txtGridSearch.PlaceholderText = "🔍 Search username, role...";
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
            this.btnPrint.Size = new System.Drawing.Size(170, 28);
            this.btnPrint.TabIndex = 2;
            this.btnPrint.Text = "🖨️ Print Report";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);

            this.gridUsers.AllowUserToAddRows = false;
            this.gridUsers.AllowUserToDeleteRows = false;
            this.gridUsers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridUsers.BackgroundColor = System.Drawing.Color.White;
            this.gridUsers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridUsers.ColumnHeadersHeight = 32;
            this.gridUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridUsers.EnableHeadersVisualStyles = false;
            this.gridUsers.Location = new System.Drawing.Point(16, 60);
            this.gridUsers.MultiSelect = false;
            this.gridUsers.Name = "gridUsers";
            this.gridUsers.ReadOnly = true;
            this.gridUsers.RowHeadersVisible = false;
            this.gridUsers.RowTemplate.Height = 32;
            this.gridUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridUsers.Size = new System.Drawing.Size(876, 672);
            this.gridUsers.TabIndex = 1;
            this.gridUsers.SelectionChanged += new System.EventHandler(this.GridUsers_SelectionChanged);

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tblMasterContent);
            this.Controls.Add(this.lblTitle);
            this.Name = "UserManagementView";
            this.pnlFormActionsBar.ResumeLayout(false);
            this.pnlFormBody.ResumeLayout(false);
            this.pnlFormBody.PerformLayout();
            this.tblMasterContent.ResumeLayout(false);
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.pnlFormHeaderBar.ResumeLayout(false);
            this.pnlFormHeaderBar.PerformLayout();
            this.tblPasswordRow.ResumeLayout(false);
            this.tblPasswordRow.PerformLayout();
            this.tblNameRow.ResumeLayout(false);
            this.pnlLastName.ResumeLayout(false);
            this.pnlLastName.PerformLayout();
            this.pnlFirstName.ResumeLayout(false);
            this.pnlFirstName.PerformLayout();
            this.pnlMiddleName.ResumeLayout(false);
            this.pnlMiddleName.PerformLayout();
            this.tblUpdateDelete.ResumeLayout(false);
            this.pnlGridCard.ResumeLayout(false);
            this.pnlGridCard.PerformLayout();
            this.pnlGridHeaderBar.ResumeLayout(false);
            this.pnlGridHeaderBar.PerformLayout();
            this.pnlGridActionsFlow.ResumeLayout(false);
            this.pnlGridActionsFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel tblMasterContent;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Panel pnlFormActionsBar;
        private System.Windows.Forms.Panel pnlFormBody;
        private System.Windows.Forms.Panel pnlFormHeaderBar;
        private System.Windows.Forms.Label lblFormHeader;
        private System.Windows.Forms.Label lblU;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblP;
        private System.Windows.Forms.TableLayoutPanel tblPasswordRow;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkShowPassword;
        private System.Windows.Forms.Label lblPassHint;
        private System.Windows.Forms.Label lblR;
        private System.Windows.Forms.ComboBox cbRole;
        private System.Windows.Forms.TableLayoutPanel tblNameRow;
        private System.Windows.Forms.Panel pnlLastName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.Panel pnlFirstName;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.Panel pnlMiddleName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.Label lblC;
        private System.Windows.Forms.TextBox txtContact;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.Button btnApprove;
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
        private System.Windows.Forms.DataGridView gridUsers;
    }
}
