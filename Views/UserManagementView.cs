using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class UserManagementView : UserControl, IRefreshable
    {
        private User? selectedUser = null;
        private bool isBindingForm = false;
        private List<User> allUsers = new List<User>();
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        public UserManagementView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridUsers.BringToFront();
            SetupGridColumns();
            gridUsers.CurrentCellDirtyStateChanged += (s, e) => { if (gridUsers.IsCurrentCellDirty && gridUsers.CurrentCell is DataGridViewCheckBoxCell) gridUsers.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridUsers.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && gridUsers.Columns[e.ColumnIndex].DataPropertyName == "IsActive" && gridUsers.Rows[e.RowIndex].DataBoundItem is User u) {
                    string? block = u.IsActive ? null : CheckDeactivation(u);
                    if (block != null)
                    {
                        u.IsActive = true;
                        int row = e.RowIndex;
                        BeginInvoke(new Action(() => { try { gridUsers.InvalidateRow(row); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); } }));
                        MessageBox.Show(block, "Can't deactivate", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    MJdatabase.ToggleUserActive(u.Id, u.IsActive);
                    if (selectedUser != null && selectedUser.Id == u.Id) {
                        chkActive.Checked = u.IsActive;
                    }
                }
            };
            SetupEvents();
            LoadUsers();
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridUsers, "User_Accounts", "User Accounts"));
            menuActions.Items.Add("🖨 Print User Directory", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            var btnRegisterStaff = new Button
            {
                Text = "👤 Register Staff Account",
                Size = new Size(185, 30),
                BackColor = Pal.C(185, 28, 28),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnRegisterStaff.FlatAppearance.BorderSize = 0;
            btnRegisterStaff.Click += (s, e) =>
            {
                using (var dlg = new RegisterDialog(isAdminMode: true))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadUsers();
                        ToastHelper.ShowToast(this, "New staff account registered and activated ✓", ToastType.Success);
                    }
                }
            };
            pnlGridActionsFlow.Controls.Add(btnRegisterStaff);
            pnlGridActionsFlow.Controls.SetChildIndex(btnRegisterStaff, 1);
            // Per-account access and sign-in help (the User Accounts tab is shown to administrators only).
            Button Tool(string text, Action<User> act)
            {
                var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(150, 30), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(0, 4, 8, 4), UseMnemonic = false };
                UIThemeHelper.SetButtonRole(b, "secondary");
                b.Click += (s, e) =>
                {
                    var u = gridUsers.CurrentRow?.DataBoundItem as User ?? selectedUser;
                    if (u == null) { QuietBox.Show(this, "Select an account in the list first.", "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                    act(u);
                };
                return b;
            }
            var btnAccess = Tool("🔐 Access & permissions", OpenAccess);
            var btnSignIn = Tool("🔑 Sign-in details", OpenSignInDetails);
            pnlGridActionsFlow.Controls.Add(btnAccess);
            pnlGridActionsFlow.Controls.Add(btnSignIn);
            pnlGridActionsFlow.Controls.SetChildIndex(btnAccess, 2);
            pnlGridActionsFlow.Controls.SetChildIndex(btnSignIn, 3);
                    InitFullPageForm();
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 Input Form ▾",
                Size = new Size(130, 30),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) SetFormVisibility(false); else OpenNewRecordForm(); };
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Name = "btnCloseForm",
                Text = "✕ Close",
                Size = new Size(80, 28),
                Dock = DockStyle.Right,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }


        // ------------------------------------------------------------------ Full-page form (same tab, back button, section cards)
        private FullPageFormKit? formKit;
        private void InitFullPageForm()
        {
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to users");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Sign-in", "Username, password and role.", lblU, txtUsername, lblP, tblPasswordRow, lblPassHint, lblR, cbRole),
                new FullPageFormKit.Section("Person", "Name, contact number and whether the account is active.", tblNameRow, lblC, txtContact, chkActive)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New user";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridUsers.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridUsers.Rows[e.RowIndex].DataBoundItem is not User) return;
                gridUsers.ClearSelection();
                gridUsers.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New user account";
            SetFormVisibility(true);
        }
        private void SetFormVisibility(bool visible)
        {
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void RefreshData()
        {
            LoadUsers();
        }

        private void SetupEvents()
        {
            txtContact.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-')
                {
                    e.Handled = true;
                }
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
            };
            chkActive.CheckedChanged += (s, e) => {
                if (isBindingForm) return;
                if (selectedUser != null && selectedUser.IsActive != chkActive.Checked)
                {
                    string? block = chkActive.Checked ? null : CheckDeactivation(selectedUser);
                    if (block != null)
                    {
                        MessageBox.Show(block, "Can't deactivate", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        isBindingForm = true;
                        try { chkActive.Checked = true; } finally { isBindingForm = false; }
                        return;
                    }
                    selectedUser.IsActive = chkActive.Checked;
                    MJdatabase.ToggleUserActive(selectedUser.Id, chkActive.Checked);
                    gridUsers.Refresh();
                }
            };
        }

        private void SetupGridColumns()
        {
            gridUsers.ReadOnly = false;
            gridUsers.EditMode = DataGridViewEditMode.EditOnEnter;
            gridUsers.AutoGenerateColumns = false;
            gridUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridUsers.ScrollBars = ScrollBars.Both;
            gridUsers.AllowUserToResizeColumns = true;
            gridUsers.ShowCellToolTips = false;
            gridUsers.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridUsers.Rows[e.RowIndex].DataBoundItem is User u)
                {
                    if (!u.IsActive)
                    {
                        e.CellStyle.BackColor = Pal.C(254, 243, 199);
                        e.CellStyle.SelectionBackColor = Pal.C(253, 230, 138);
                        e.CellStyle.SelectionForeColor = Pal.C(120, 53, 15);
                    }
                }
            };
            SetupContextMenu();
            gridUsers.Columns.Clear();
            gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Active", FillWeight = 9, MinimumWidth = 65, ReadOnly = false });
            gridUsers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", FillWeight = 18, MinimumWidth = 110, ReadOnly = true });
            gridUsers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "Role", FillWeight = 14, MinimumWidth = 90, ReadOnly = true });
            gridUsers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "Full Name", FillWeight = 28, MinimumWidth = 130, ReadOnly = true });
            gridUsers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ContactNumber", HeaderText = "Contact #", FillWeight = 16, MinimumWidth = 100, ReadOnly = true });
            gridUsers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedAt", HeaderText = "Created", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 15, MinimumWidth = 85, ReadOnly = true });
        }

        private void GridUsers_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridUsers.SelectedRows.Count > 0 && gridUsers.SelectedRows[0].DataBoundItem is User u)
            {
                isBindingForm = true;
                selectedUser = u;
                txtUsername.Text = selectedUser.Username;
                txtPassword.Text = "";
                cbRole.Text = selectedUser.Role;
                txtLastName.Text = selectedUser.LastName ?? "";
                txtFirstName.Text = selectedUser.FirstName ?? "";
                txtMiddleName.Text = selectedUser.MiddleName ?? "";
                txtContact.Text = selectedUser.ContactNumber;
                chkActive.Checked = selectedUser.IsActive;
                btnApprove.Visible = !selectedUser.IsActive;
                if (!selectedUser.IsActive)
                {
                    lblFormHeader.Text = "⚠️ Pending Staff Account (Awaiting Owner Approval)";
                    lblFormHeader.ForeColor = Pal.C(180, 83, 9);
                    btnApprove.Location = new Point(16, 404);
                    btnSave.Location = new Point(16, 452);
                    tblUpdateDelete.Location = new Point(16, 502);
                    btnClear.Location = new Point(16, 550);
                }
                else
                {
                    lblFormHeader.Text = "User Credentials Form";
                    lblFormHeader.ForeColor = Pal.C(15, 23, 42);
                    btnSave.Location = new Point(16, 404);
                    tblUpdateDelete.Location = new Point(16, 454);
                    btnClear.Location = new Point(16, 502);
                }
                isBindingForm = false;
            }
        }

        private string? ValidatePasswordInput(string password, bool isNewUser)
        {
            if (isNewUser && string.IsNullOrWhiteSpace(password))
            {
                return "Password cannot be empty.";
            }

            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                {
                    return "Password must be at least 6 characters long.";
                }

                bool hasLetter = password.Any(char.IsLetter);
                bool hasDigit = password.Any(char.IsDigit);

                if (!hasLetter || !hasDigit)
                {
                    return "Password must contain both letters and numbers (e.g. Pass123).";
                }
            }

            return null;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Username cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (username.Length < 3)
                {
                    MessageBox.Show("Username must be at least 3 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                string? passError = ValidatePasswordInput(password, true);
                if (!string.IsNullOrEmpty(passError))
                {
                    MessageBox.Show(passError, "Password Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }

                var allUsers = MJdatabase.GetUsers();
                if (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"DUPLICATE USERNAME BLOCKED:\nThe username '{username}' is already in use. Duplicate usernames are not permitted.", "Duplicate Username Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    txtUsername.Focus();
                    return;
                }

                string lastName = txtLastName.Text.Trim();
                string firstName = txtFirstName.Text.Trim();
                string middleName = txtMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(lastName) || !SecurityHelper.IsValidName(lastName))
                {
                    MessageBox.Show("Please enter a valid last name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(firstName) || !SecurityHelper.IsValidName(firstName))
                {
                    MessageBox.Show("Please enter a valid first name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(middleName) && !SecurityHelper.IsValidName(middleName))
                {
                    MessageBox.Show("Please enter a valid middle name containing only letters, spaces, hyphens, and apostrophes, or leave it blank.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMiddleName.Focus();
                    return;
                }

                string fullName = NameHelper.FormatFullName(lastName, firstName, middleName);

                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrEmpty(contact) && !SecurityHelper.IsValidPhone(contact))
                {
                    MessageBox.Show("Please enter a valid Philippine contact number (e.g. 09123456789 or +639123456789).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtContact.Focus();
                    return;
                }

                User u = new User
                {
                    Username = username,
                    Password = password.Trim(),
                    Role = string.IsNullOrWhiteSpace(cbRole.Text) ? "Office Staff" : cbRole.Text.Trim(),
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    FullName = fullName,
                    ContactNumber = contact,
                    IsActive = chkActive.Checked
                };

                MJdatabase.SaveUser(u);
                QuietBox.Show("User account created successfully (password securely hashed with SHA-256)!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadUsers();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to save user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedUser == null)
                {
                    MessageBox.Show("Please select a user account to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string username = txtUsername.Text.Trim();
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                {
                    MessageBox.Show("Username must be at least 3 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                var allUsers = MJdatabase.GetUsers();
                if (allUsers.Any(u => u.Id != selectedUser.Id && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"The username '{username}' is already in use by another account.", "Duplicate Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                string password = txtPassword.Text;
                if (!string.IsNullOrWhiteSpace(password))
                {
                    string? passError = ValidatePasswordInput(password, false);
                    if (!string.IsNullOrEmpty(passError))
                    {
                        MessageBox.Show(passError, "Password Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPassword.Focus();
                        return;
                    }
                    selectedUser.Password = password.Trim();
                }

                        bool isTargetAdmin = SessionContext.IsAdminRole(selectedUser.Role);
                bool willBeActive = chkActive.Checked;
                bool willBeAdmin = SessionContext.IsAdminRole(cbRole.Text);
                if (IsCurrentUser(selectedUser) && !willBeActive)
                {
                    MessageBox.Show("You can't deactivate the account you are signed in with.", "Can't deactivate", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                if (IsCurrentUser(selectedUser) && isTargetAdmin && !willBeAdmin)
                {
                    var ans = MessageBox.Show("You are removing administrator rights from your own account. You will lose access to this screen after signing out.\n\nContinue?", "Change your own role", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (ans != DialogResult.Yes) return;
                }
                if (isTargetAdmin && (!willBeActive || !willBeAdmin))
                {
                    if (!MJdatabase.CanDeactivateOrDeleteAdmin(selectedUser.Id))
                    {
                        MessageBox.Show("Cannot deactivate or change the role of the only remaining active Administrator account. Please ensure at least one other active Administrator account exists first.", "Administrator Protection", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }

                string lastName = txtLastName.Text.Trim();
                string firstName = txtFirstName.Text.Trim();
                string middleName = txtMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(lastName) || !SecurityHelper.IsValidName(lastName))
                {
                    MessageBox.Show("Please enter a valid last name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(firstName) || !SecurityHelper.IsValidName(firstName))
                {
                    MessageBox.Show("Please enter a valid first name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(middleName) && !SecurityHelper.IsValidName(middleName))
                {
                    MessageBox.Show("Please enter a valid middle name containing only letters, spaces, hyphens, and apostrophes, or leave it blank.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMiddleName.Focus();
                    return;
                }

                string fullName = NameHelper.FormatFullName(lastName, firstName, middleName);

                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrEmpty(contact) && !SecurityHelper.IsValidPhone(contact))
                {
                    MessageBox.Show("Please enter a valid Philippine contact number (e.g. 09123456789 or +639123456789).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtContact.Focus();
                    return;
                }

                selectedUser.Username = username;
                selectedUser.Role = string.IsNullOrWhiteSpace(cbRole.Text) ? "Office Staff" : cbRole.Text.Trim();
                selectedUser.LastName = lastName;
                selectedUser.FirstName = firstName;
                selectedUser.MiddleName = middleName;
                selectedUser.FullName = fullName;
                selectedUser.ContactNumber = contact;
                selectedUser.IsActive = chkActive.Checked;

                MJdatabase.SaveUser(selectedUser);
                QuietBox.Show("User account updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadUsers();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to update user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedUser == null)
                {
                    MessageBox.Show("Please select a user account to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IsCurrentUser(selectedUser))
                {
                    MessageBox.Show("You can't delete the account you are signed in with.", "Protection Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                if (SessionContext.IsAdminRole(selectedUser.Role))
                {
                    if (!MJdatabase.CanDeactivateOrDeleteAdmin(selectedUser.Id))
                    {
                        MessageBox.Show("Cannot delete the only remaining active Administrator account. Please promote or create another active Administrator account first.", "Administrator Protection", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }

                if (selectedUser.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("The primary admin account cannot be deleted.", "Protection Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete user account '{selectedUser.Username}'?\n\n" + MJdatabase.DeletePolicyNote("Users"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Users", selectedUser.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadUsers();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete user account. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to delete user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnApprove_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedUser == null) return;
                var confirm = MessageBox.Show($"Approve and activate staff account for '{selectedUser.FullName}' ({selectedUser.Username})?", "Confirm Approval", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    await MJdatabase.ApproveUserAsync(selectedUser.Id);
                    QuietBox.Show($"Staff account '{selectedUser.FullName}' has been successfully approved and activated!", "Account Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsers();
                }
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to approve user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAccess(User u)
        {
            if (!SessionContext.IsAdmin) { MessageBox.Show(this, "Only the Owner / Administrator can change what an account can use.", "Access & permissions", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new UserAccessDialog(u);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) ToastHelper.ShowToast(this, $"Access for {u.Username} saved. It applies the next time they sign in.", ToastType.Success);
        }
        private void OpenSignInDetails(User u)
        {
            if (!SessionContext.IsAdmin) { MessageBox.Show(this, "Only the Owner / Administrator can see sign-in details.", "Sign-in details", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var dlg = new SignInDetailsDialog(u);
            dlg.ShowDialog(FindForm());
        }
        private void SetupContextMenu()
        {
            RowMenu.Attach(gridUsers, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not User u) return;
                selectedUser = u;
                bool isSelf = IsCurrentUser(u);
                bool isPrimary = u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase);
                m.Items.AddRange(RowMenu.Header($"👤  {(string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)}",
                    $"@{u.Username} · {u.Role} · {(u.IsActive ? "Active" : "Waiting for approval / inactive")}{(isSelf ? " · you" : "")}"));
                if (!u.IsActive) m.Items.Add(RowMenu.Good("Approve account", () => ApproveUser(u)));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, u)));
                m.Items.Add(RowMenu.Item("Edit account", () => { SetFormVisibility(true); txtUsername.Focus(); }));
                m.Items.Add(RowMenu.Item("Set / reset security question…", () =>
                {
                    using var secDlg = new SecuritySettingsDialog(u);
                    if (secDlg.ShowDialog(this) == DialogResult.OK) LoadUsers();
                }));
                m.Items.Add(RowMenu.Item("Access & permissions…", () => OpenAccess(u)));
                m.Items.Add(RowMenu.Item("Sign-in details (username / new password)…", () => OpenSignInDetails(u)));
                if (u.IsActive) m.Items.Add(RowMenu.Item("Deactivate account", () => DeactivateUser(u), "", !isSelf && !isPrimary));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Username", u.Username), ("Full name", u.FullName), ("Contact number", u.ContactNumber)));
                m.Items.Add(RowMenu.Item($"Find other {u.Role} accounts", () => RowMenu.Search(txtGridSearch, u.Role)));
                m.Items.Add(RowMenu.ExportSub(gridUsers, "User_Accounts", "System User Accounts", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger(u.IsActive ? "Delete account…" : "Reject / delete account…", () => { selectedUser = u; BtnDelete_Click(this, EventArgs.Empty); }, !isSelf && !isPrimary));
            });

        }
        private static bool IsCurrentUser(User u) =>
            SessionContext.ActiveUser != null && (SessionContext.ActiveUser.Id == u.Id || SessionContext.ActiveUser.Username.Equals(u.Username, StringComparison.OrdinalIgnoreCase));
        private void ApproveUser(User u)
        {
            try
            {
                MJdatabase.ApproveUser(u.Id);
                ToastHelper.ShowToast(this, $"{u.FullName} can now sign in", ToastType.Success);
                LoadUsers();
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to approve user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DeactivateUser(User u)
        {
            string? block = CheckDeactivation(u);
            if (block != null)
            {
                MessageBox.Show(block, "Can't deactivate", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            if (MessageBox.Show($"Deactivate {u.Username}? They won't be able to sign in until the account is approved again.", "Deactivate account", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            MJdatabase.ToggleUserActive(u.Id, false);
            LoadUsers();
        }
        /// <summary>Why an account can't be switched off, or null when it can.</summary>
        private static string? CheckDeactivation(User u)
        {
            if (IsCurrentUser(u)) return "You can't deactivate the account you are signed in with.";
            if (u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return "The primary admin account can't be deactivated.";
            if (SessionContext.IsAdminRole(u.Role) && !MJdatabase.CanDeactivateOrDeleteAdmin(u.Id))
                return "This is the only active administrator. Create or approve another administrator first.";
            return null;
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearFields();
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintHelper.PrintGridReport("User Accounts Access & Credentials Audit Register", gridUsers, "Official user access roster detailing usernames, assigned system roles, active status, and encrypted password security.");
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            selectedUser = null;
            txtUsername.Text = "";
            txtPassword.Text = "";
            chkShowPassword.Checked = false;
            cbRole.SelectedIndex = 0;
            txtLastName.Text = "";
            txtFirstName.Text = "";
            txtMiddleName.Text = "";
            txtContact.Text = "";
            chkActive.Checked = true;
            btnApprove.Visible = false;
            lblFormHeader.Text = "User Credentials Form";
            lblFormHeader.ForeColor = Pal.C(15, 23, 42);
            btnSave.Location = new Point(16, 404);
            tblUpdateDelete.Location = new Point(16, 454);
            btnClear.Location = new Point(16, 502);
            if (gridUsers.SelectedRows.Count > 0) gridUsers.ClearSelection();
        }

        private void LoadUsers()
        {
            try
            {
                allUsers = MJdatabase.GetUsers();
                FilterGridData();
                if (cbRole.Items.Count > 0 && cbRole.SelectedIndex < 0) cbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            { ErrLog.Error("UserManagementView", ex); 
                MessageBox.Show($"Failed to load users: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allUsers
                : allUsers.Where(u =>
                    (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(u.LastName) && u.LastName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(u.MiddleName) && u.MiddleName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(u.Role) && u.Role.ToLower().Contains(q))
                ).ToList();

            gridUsers.DataSource = null;
            gridUsers.DataSource = filtered;
            int pendingCount = allUsers.Count(u => !u.IsActive);
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? (pendingCount > 0 ? $"System User Accounts ({allUsers.Count})  |  ⚠️ {pendingCount} Pending Approval" : $"System User Accounts ({allUsers.Count})")
                : $"System User Accounts - Showing {filtered.Count} of {allUsers.Count}";

            if (filtered.Count > 0)
            {
                gridUsers.ClearSelection();
                gridUsers.Rows[0].Selected = true;
            }
            else
            {
                isBindingForm = true;
                selectedUser = null;
                txtUsername.Text = "";
                txtLastName.Text = "";
                txtFirstName.Text = "";
                txtMiddleName.Text = "";
                txtContact.Text = "";
                btnApprove.Visible = false;
                lblFormHeader.Text = "User Credentials Form";
                lblFormHeader.ForeColor = Pal.C(15, 23, 42);
                btnSave.Location = new Point(16, 404);
                tblUpdateDelete.Location = new Point(16, 454);
                btnClear.Location = new Point(16, 502);
                isBindingForm = false;
            }
        }

        private void TxtGridSearch_TextChanged(object? sender, EventArgs e)
        {
            FilterGridData();
        }

        private void BtnClearSearch_Click(object? sender, EventArgs e)
        {
            txtGridSearch.Text = "";
            txtGridSearch.Focus();
        }
    }
}
