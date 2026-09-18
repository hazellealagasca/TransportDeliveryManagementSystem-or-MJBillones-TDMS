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

        public UserManagementView()
        {
            InitializeComponent();
            gridUsers.BringToFront();
            SetupGridColumns();
            gridUsers.CurrentCellDirtyStateChanged += (s, e) => { if (gridUsers.IsCurrentCellDirty && gridUsers.CurrentCell is DataGridViewCheckBoxCell) gridUsers.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridUsers.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridUsers.Rows[e.RowIndex].DataBoundItem is User u) {
                    MJdatabase.ToggleUserActive(u.Id, u.IsActive);
                    if (selectedUser != null && selectedUser.Id == u.Id) {
                        chkActive.Checked = u.IsActive;
                    }
                }
            };
            SetupEvents();
            LoadUsers();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadUsers();
        }

        private void SetupEvents()
        {
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
            };
            chkActive.CheckedChanged += (s, e) => {
                if (isBindingForm) return;
                if (selectedUser != null && selectedUser.IsActive != chkActive.Checked)
                {
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
            gridUsers.ShowCellToolTips = true;
            gridUsers.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridUsers.Rows[e.RowIndex].DataBoundItem is User u)
                {
                    if (!u.IsActive)
                    {
                        gridUsers.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                        gridUsers.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.FromArgb(253, 230, 138);
                        gridUsers.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 53, 15);
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
                    lblFormHeader.ForeColor = Color.FromArgb(180, 83, 9);
                    btnSave.Location = new Point(20, 460);
                    btnUpdate.Location = new Point(20, 505);
                    btnDelete.Location = new Point(245, 505);
                    btnClear.Location = new Point(20, 548);
                }
                else
                {
                    lblFormHeader.Text = "User Credentials Form";
                    lblFormHeader.ForeColor = Color.FromArgb(15, 23, 42);
                    btnSave.Location = new Point(20, 415);
                    btnUpdate.Location = new Point(20, 465);
                    btnDelete.Location = new Point(245, 465);
                    btnClear.Location = new Point(20, 512);
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

                User u = new User
                {
                    Username = username,
                    Password = password.Trim(),
                    Role = string.IsNullOrWhiteSpace(cbRole.Text) ? "Office Staff" : cbRole.Text.Trim(),
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    FullName = fullName,
                    ContactNumber = txtContact.Text.Trim(),
                    IsActive = chkActive.Checked
                };

                MJdatabase.SaveUser(u);
                MessageBox.Show("User account created successfully (password securely hashed with SHA-256)!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadUsers();
            }
            catch (Exception ex)
            {
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

                        bool isTargetAdmin = selectedUser.Role.Contains("Admin", StringComparison.OrdinalIgnoreCase) || selectedUser.Role.Contains("Owner", StringComparison.OrdinalIgnoreCase);
                bool willBeActive = chkActive.Checked;
                bool willBeAdmin = cbRole.Text.Trim().Contains("Admin", StringComparison.OrdinalIgnoreCase) || cbRole.Text.Trim().Contains("Owner", StringComparison.OrdinalIgnoreCase);

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

                selectedUser.Username = username;
                selectedUser.Role = string.IsNullOrWhiteSpace(cbRole.Text) ? "Office Staff" : cbRole.Text.Trim();
                selectedUser.LastName = lastName;
                selectedUser.FirstName = firstName;
                selectedUser.MiddleName = middleName;
                selectedUser.FullName = fullName;
                selectedUser.ContactNumber = txtContact.Text.Trim();
                selectedUser.IsActive = chkActive.Checked;

                MJdatabase.SaveUser(selectedUser);
                MessageBox.Show("User account updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadUsers();
            }
            catch (Exception ex)
            {
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

                if (selectedUser.Role.Contains("Admin", StringComparison.OrdinalIgnoreCase) || selectedUser.Role.Contains("Owner", StringComparison.OrdinalIgnoreCase))
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

                var res = MessageBox.Show($"Are you sure you want to delete user account '{selectedUser.Username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Users", selectedUser.Id);
                    if (deleted)
                    {
                        MessageBox.Show("User account deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            {
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
                    MessageBox.Show($"Staff account '{selectedUser.FullName}' has been successfully approved and activated!", "Account Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to approve user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            var itemApprove = new ToolStripMenuItem("✅ Approve Staff Account", null, (s, e) =>
            {
                try
                {
                    if (gridUsers.SelectedRows.Count > 0 && gridUsers.SelectedRows[0].DataBoundItem is User u && !u.IsActive)
                    {
                        MJdatabase.ApproveUser(u.Id);
                        MessageBox.Show($"Staff account '{u.FullName}' has been approved!", "Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to approve user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            var itemReject = new ToolStripMenuItem("❌ Reject / Delete Account", null, (s, e) =>
            {
                try
                {
                    if (gridUsers.SelectedRows.Count > 0 && gridUsers.SelectedRows[0].DataBoundItem is User u)
                    {
                        if (u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("Primary administrator account cannot be deleted.", "Protected", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }
                        var conf = MessageBox.Show($"Delete user account '{u.Username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (conf == DialogResult.Yes)
                        {
                            bool deleted = MJdatabase.DeleteRecord("Users", u.Id);
                            if (deleted)
                            {
                                LoadUsers();
                            }
                            else
                            {
                                MessageBox.Show("Failed to delete user account. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete user account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            var itemInspect = new ToolStripMenuItem("🔍 Inspect Account Details", null, (s, e) =>
            {
                if (gridUsers.SelectedRows.Count > 0 && gridUsers.SelectedRows[0].DataBoundItem is User u)
                {
                    RecordDetailsDialog.ShowDetails(this, u);
                }
            });

            menu.Opening += (s, e) =>
            {
                if (gridUsers.SelectedRows.Count > 0 && gridUsers.SelectedRows[0].DataBoundItem is User u)
                {
                    itemApprove.Visible = !u.IsActive;
                }
            };

            menu.Items.AddRange(new ToolStripItem[] { itemApprove, itemReject, new ToolStripSeparator(), itemInspect });
            gridUsers.ContextMenuStrip = menu;

            gridUsers.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    gridUsers.ClearSelection();
                    gridUsers.Rows[e.RowIndex].Selected = true;
                    if (e.ColumnIndex >= 0) gridUsers.CurrentCell = gridUsers.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
            };
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
            {
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
            lblFormHeader.ForeColor = Color.FromArgb(15, 23, 42);
            btnSave.Location = new Point(20, 415);
            btnUpdate.Location = new Point(20, 465);
            btnDelete.Location = new Point(245, 465);
            btnClear.Location = new Point(20, 512);
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
            {
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
                lblFormHeader.ForeColor = Color.FromArgb(15, 23, 42);
                btnSave.Location = new Point(20, 415);
                btnUpdate.Location = new Point(20, 465);
                btnDelete.Location = new Point(245, 465);
                btnClear.Location = new Point(20, 512);
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
