using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class CrewManagementView : UserControl, IRefreshable
    {
        private Crew? selectedCrew = null;
        private List<Crew> allCrews = new List<Crew>();
        private string currentSortColumn = "FullName";
        private bool isSortAscending = true;
        private string currentPhotoPath = string.Empty;
        private bool isBindingForm = false;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        public CrewManagementView()
        {
            InitializeComponent();
            gridCrew.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridCrew.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridCrew.Rows[e.RowIndex].DataBoundItem is Crew item) RecordDetailsDialog.ShowDetails(this, item); };
            gridCrew.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridCrew.Columns.Count) return;
                string col = gridCrew.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
                        gridCrew.CurrentCellDirtyStateChanged += (s, e) => { if (gridCrew.IsCurrentCellDirty && gridCrew.CurrentCell is DataGridViewCheckBoxCell) gridCrew.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridCrew.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && gridCrew.Columns[e.ColumnIndex].DataPropertyName == "IsAvailable" && gridCrew.Rows[e.RowIndex].DataBoundItem is Crew c) {
                    try
                    {
                        bool success = MJdatabase.ToggleCrewAvailable(c.Id, c.IsAvailable);
                        if (!success)
                        {
                            c.IsAvailable = !c.IsAvailable;
                            gridCrew.InvalidateRow(e.RowIndex);
                            MessageBox.Show("Failed to update crew availability status.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        if (selectedCrew != null && selectedCrew.Id == c.Id) {
                            chkAvailable.Checked = c.IsAvailable;
                        }
                        gridCrew.InvalidateRow(e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        c.IsAvailable = !c.IsAvailable;
                        gridCrew.InvalidateRow(e.RowIndex);
                        MessageBox.Show($"Failed to update crew availability: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            SetupEvents();
            txtLastName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtFirstName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtMiddleName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            LoadCrew();

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridCrew, "Crew_Personnel", "Crew Personnel"));
            menuActions.Items.Add("🖨️ Print Personnel Roster", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);
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

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 + New / Form ▾",
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Width = 72,
                Height = 30,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }

        private void SetFormVisibility(bool visible)
        {
            pnlForm.Visible = visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].Width = visible ? 480F : 0F;
                tblMasterContent.ColumnStyles[0].SizeType = SizeType.Absolute;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].Width = 100F;
                tblMasterContent.ColumnStyles[1].SizeType = SizeType.Percent;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "✕ Hide Form ▴" : "📝 + New / Form ▾";
                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadCrew();
        }

        private bool isFormattingLicense = false;

        private void SetupLicenseAutoHyphen()
        {
            txtLicense.TextChanged += (s, e) => FormatLicenseNumber();
            txtLicense.Leave += (s, e) =>
            {
                string raw = txtLicense.Text.Trim().ToUpper();
                if (raw == "NA") txtLicense.Text = "N/A";
                else txtLicense.Text = raw;
            };
        }

        private void FormatLicenseNumber()
        {
            if (isFormattingLicense) return;

            string raw = txtLicense.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(raw) || raw == "N/A" || raw == "NONE") return;

            string clean = new string(raw.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (clean.Length == 0) return;

            string formatted;
            if (clean.Length <= 3)
            {
                formatted = clean;
            }
            else if (clean.Length <= 5)
            {
                formatted = $"{clean.Substring(0, 3)}-{clean.Substring(3)}";
            }
            else if (clean.Length <= 11)
            {
                formatted = $"{clean.Substring(0, 3)}-{clean.Substring(3, 2)}-{clean.Substring(5)}";
            }
            else
            {
                clean = clean.Substring(0, 11);
                formatted = $"{clean.Substring(0, 3)}-{clean.Substring(3, 2)}-{clean.Substring(5)}";
            }

            if (formatted != txtLicense.Text)
            {
                isFormattingLicense = true;
                txtLicense.Text = formatted;
                txtLicense.SelectionStart = formatted.Length;
                isFormattingLicense = false;
            }
        }

        private void SetupEvents()
        {
            txtContact.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-')
                {
                    e.Handled = true;
                }
            };
            txtLicense.CharacterCasing = CharacterCasing.Upper;
            SetupLicenseAutoHyphen();
            cbRole.SelectedIndexChanged += (s, e) =>
            {
                bool isDriver = cbRole.Text.Trim().IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isDriver)
                {
                    lblLicense.Text = "Driver's License # (Required for Drivers):";
                    txtLicense.BackColor = Color.White;
                }
                else
                {
                    lblLicense.Text = "Driver's License # (Optional for Aides):";
                    txtLicense.BackColor = Color.FromArgb(248, 250, 252);
                }
            };
        }

        private void SetupGridColumns()
        {
            gridCrew.ReadOnly = false;
            gridCrew.EditMode = DataGridViewEditMode.EditOnEnter;
            gridCrew.AutoGenerateColumns = false;
            gridCrew.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCrew.ScrollBars = ScrollBars.Both;
            gridCrew.AllowUserToResizeColumns = true;
            gridCrew.ShowCellToolTips = true;
            gridCrew.Columns.Clear();
            gridCrew.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsAvailable", HeaderText = "Active", FillWeight = 9F, MinimumWidth = 75, ReadOnly = false });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "Full Name", FillWeight = 26F, MinimumWidth = 145, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "Role", FillWeight = 16F, MinimumWidth = 110, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ContactNumber", HeaderText = "Contact #", FillWeight = 16F, MinimumWidth = 110, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LicenseNumber", HeaderText = "License #", FillWeight = 18F, MinimumWidth = 120, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AvailabilityStatus", HeaderText = "Status", FillWeight = 15F, MinimumWidth = 95, ReadOnly = true });
        }

        private void GridCrew_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridCrew.SelectedRows.Count > 0 && gridCrew.SelectedRows[0].DataBoundItem is Crew c)
            {
                isBindingForm = true;
                selectedCrew = c;
                txtLastName.Text = selectedCrew.LastName;
                txtFirstName.Text = selectedCrew.FirstName;
                txtMiddleName.Text = selectedCrew.MiddleName;
                cbRole.Text = selectedCrew.Role;
                txtContact.Text = selectedCrew.ContactNumber;
                txtLicense.Text = selectedCrew.LicenseNumber;

                chkAvailable.Checked = selectedCrew.IsAvailable;
                chkIsSeasonal.Checked = selectedCrew.IsSeasonal;

                currentPhotoPath = selectedCrew.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = "Personnel Master Form";
                isBindingForm = false;
            }
        }

        private void DisplayPhotoPreview(string photoPath)
        {
            try
            {
                if (picPhoto.Image != null)
                {
                    var oldImg = picPhoto.Image;
                    picPhoto.Image = null;
                    oldImg.Dispose();
                }

                if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
                {
                    picPhoto.Image = SecurityHelper.LoadDecoupledBitmap(photoPath);
                    btnViewPhoto.Enabled = true;
                    btnRemovePhoto.Enabled = true;
                }
                else
                {
                    picPhoto.Image = null;
                    btnViewPhoto.Enabled = false;
                    btnRemovePhoto.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrewManagementView] DisplayPhotoPreview: {ex.Message}");
                picPhoto.Image = null;
                btnViewPhoto.Enabled = false;
                btnRemovePhoto.Enabled = false;
            }
        }

        private async void LoadCrew()
        {
            try
            {
                var crewRoles = MJdatabase.GetLookupItems("Crew Role");
                string currentRole = cbRole.Text;
                cbRole.Items.Clear();
                cbRole.Items.Add("Driver");
                cbRole.Items.Add("Aide/Helper");

                if (!string.IsNullOrEmpty(currentRole))
                {
                    if (currentRole.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0)
                        cbRole.SelectedItem = "Driver";
                    else
                        cbRole.SelectedItem = "Aide/Helper";
                }
                else if (cbRole.SelectedIndex < 0)
                {
                    cbRole.SelectedIndex = 0;
                }

                allCrews = await MJdatabase.GetCrewsAsync();
                FilterGridData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load crew records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUploadPhoto_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Crew ID / Driver Photo";
                ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string ext = Path.GetExtension(ofd.FileName).ToLowerInvariant();
                        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp")
                        {
                            MessageBox.Show("Invalid image file format. Only JPG, PNG, and BMP are supported.", "Invalid Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        FileInfo fi = new FileInfo(ofd.FileName);
                        if (fi.Length > 10 * 1024 * 1024)
                        {
                            MessageBox.Show("Selected image exceeds 10MB limit. Please choose a smaller photo.", "File Too Large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string uploadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", "CrewPhotos");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        string uniqueFileName = $"Crew_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                        string destPath = Path.Combine(uploadDir, uniqueFileName);

                        File.Copy(ofd.FileName, destPath, true);
                        currentPhotoPath = destPath;
                        DisplayPhotoPreview(currentPhotoPath);
                        MessageBox.Show("Photo attached successfully!", "Photo Uploaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save photo: {ex.Message}", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnViewPhoto_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPhotoPath) && File.Exists(currentPhotoPath))
            {
                string crewName = NameHelper.FormatFullName(txtLastName.Text.Trim(), txtFirstName.Text.Trim(), txtMiddleName.Text.Trim());
                string crewTitle = $"{crewName} - Photo ID";
                using (var viewer = new PhotoViewerDialog(currentPhotoPath, crewTitle))
                {
                    viewer.ShowDialog(this.FindForm());
                }
            }
            else
            {
                MessageBox.Show("No valid photo file found to display.", "Photo Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnRemovePhoto_Click(object? sender, EventArgs e)
        {
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(string.Empty);
            MessageBox.Show("Photo reference removed. Click 'Save' or 'Update' to persist changes.", "Photo Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(cbRole.Text))
                {
                    MessageBox.Show("Last Name, First Name, and Crew Role are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string lastName = txtLastName.Text.Trim();
                string firstName = txtFirstName.Text.Trim();
                string middleName = txtMiddleName.Text.Trim();
                string name = NameHelper.FormatFullName(lastName, firstName, middleName);

                if (!SecurityHelper.IsValidName(lastName) || !SecurityHelper.IsValidName(firstName))
                {
                    MessageBox.Show("Please enter valid Last Name and First Name containing only letters, spaces, hyphens, and apostrophes.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrEmpty(contact) && !SecurityHelper.IsValidPhone(contact))
                {
                    MessageBox.Show("Invalid contact number format. Please enter a valid mobile number (e.g. 09171234567).", "Invalid Contact #", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtContact.Focus();
                    return;
                }

                string role = cbRole.Text.Trim();
                bool isDriver = role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
                string license = txtLicense.Text.Trim().ToUpper();

                if (isDriver)
                {
                    if (string.IsNullOrWhiteSpace(license) || !SecurityHelper.IsValidLicenseNumber(license, true))
                    {
                        MessageBox.Show("A valid Philippine driver's license number (e.g. N01-23-456789 or D02-14-123456) is mandatory for Driver personnel.", "Driver License Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtLicense.Focus();
                        return;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(license) && !license.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !license.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!SecurityHelper.IsValidLicenseNumber(license, false))
                        {
                            MessageBox.Show("Invalid driver's license format. If provided for non-driver crew, it must be a valid Philippine Driver's License format (e.g. N01-23-456789 or D02-14-123456), or left blank / N/A.", "Invalid License Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtLicense.Focus();
                            return;
                        }
                    }
                }

                string? duplicate = MJdatabase.ValidateDuplicateCrew(0, name, contact, license, lastName, firstName);
                if (!string.IsNullOrEmpty(duplicate))
                {
                    MessageBox.Show(duplicate, "Duplicate Crew Information Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                MJdatabase.SaveLookupItem("Crew Role", role);

                Crew crew = new Crew
                {
                    FullName = name,
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    Role = role,
                    ContactNumber = contact,
                    LicenseNumber = isDriver ? license : (string.IsNullOrWhiteSpace(license) || license == "N/A" || license == "NONE" ? string.Empty : license),
                    DailyWage = 0,
                    FoodAllowance = 0,
                    IsAvailable = chkAvailable.Checked,
                    IsActive = true,
                    IsSeasonal = chkIsSeasonal.Checked,
                    PhotoPath = currentPhotoPath
                };

                MJdatabase.SaveCrew(crew);
                MessageBox.Show("Crew member registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadCrew();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register crew member: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedCrew == null)
                {
                    MessageBox.Show("Please select a crew member to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string lastName = txtLastName.Text.Trim();
                string firstName = txtFirstName.Text.Trim();
                string middleName = txtMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(lastName) || !SecurityHelper.IsValidName(lastName))
                {
                    MessageBox.Show("Please enter a valid last name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Invalid Last Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(firstName) || !SecurityHelper.IsValidName(firstName))
                {
                    MessageBox.Show("Please enter a valid first name containing only letters, spaces, hyphens, and apostrophes (2 to 60 characters).", "Invalid First Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(middleName) && !SecurityHelper.IsValidName(middleName))
                {
                    MessageBox.Show("Please enter a valid middle name containing only letters, spaces, hyphens, and apostrophes, or leave it blank.", "Invalid Middle Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMiddleName.Focus();
                    return;
                }

                string name = NameHelper.FormatFullName(lastName, firstName, middleName);

                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrEmpty(contact) && !SecurityHelper.IsValidPhone(contact))
                {
                    MessageBox.Show("Invalid contact number format. Please enter a valid mobile number (e.g. 09171234567).", "Invalid Contact #", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtContact.Focus();
                    return;
                }

                string role = cbRole.Text.Trim();
                bool isDriver = role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
                string license = txtLicense.Text.Trim().ToUpper();

                if (isDriver)
                {
                    if (string.IsNullOrWhiteSpace(license) || !SecurityHelper.IsValidLicenseNumber(license, true))
                    {
                        MessageBox.Show("A valid Philippine driver's license number (e.g. N01-23-456789 or D02-14-123456) is mandatory for Driver personnel.", "Driver License Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtLicense.Focus();
                        return;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(license) && !license.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !license.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!SecurityHelper.IsValidLicenseNumber(license, false))
                        {
                            MessageBox.Show("Invalid driver's license format. If provided for non-driver crew, it must be a valid Philippine Driver's License format (e.g. N01-23-456789 or D02-14-123456), or left blank / N/A.", "Invalid License Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtLicense.Focus();
                            return;
                        }
                    }
                }

                string? duplicate = MJdatabase.ValidateDuplicateCrew(selectedCrew.Id, name, contact, license, lastName, firstName);
                if (!string.IsNullOrEmpty(duplicate))
                {
                    MessageBox.Show(duplicate, "Duplicate Crew Information Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                MJdatabase.SaveLookupItem("Crew Role", role);

                selectedCrew.LastName = lastName;
                selectedCrew.FirstName = firstName;
                selectedCrew.MiddleName = middleName;
                selectedCrew.FullName = name;
                selectedCrew.Role = role;
                selectedCrew.ContactNumber = contact;
                selectedCrew.LicenseNumber = license;

                selectedCrew.IsAvailable = chkAvailable.Checked;
                selectedCrew.IsSeasonal = chkIsSeasonal.Checked;
                selectedCrew.PhotoPath = currentPhotoPath;

                MJdatabase.SaveCrew(selectedCrew);
                MessageBox.Show("Crew member updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadCrew();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update crew member: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedCrew == null)
                {
                    MessageBox.Show("Please select a crew member to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Crews", selectedCrew.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to deactivate/delete crew record for '{selectedCrew.FullName}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Crews", selectedCrew.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Crew record deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadCrew();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete crew record. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete crew record: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearFields();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allCrews
                : allCrews.Where(c =>
                    (!string.IsNullOrEmpty(c.FullName) && c.FullName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.LastName) && c.LastName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.FirstName) && c.FirstName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.MiddleName) && c.MiddleName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Role) && c.Role.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.ContactNumber) && c.ContactNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.LicenseNumber) && c.LicenseNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.AvailabilityStatus) && c.AvailabilityStatus.ToLower().Contains(q))
                ).ToList();

            switch (currentSortColumn)
            {
                case "FullName":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.FullName).ToList() : filtered.OrderByDescending(c => c.FullName).ToList();
                    break;
                case "Role":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.Role).ThenBy(c => c.FullName).ToList() : filtered.OrderByDescending(c => c.Role).ThenBy(c => c.FullName).ToList();
                    break;
                case "ContactNumber":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.ContactNumber).ToList() : filtered.OrderByDescending(c => c.ContactNumber).ToList();
                    break;
                case "LicenseNumber":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.LicenseNumber).ToList() : filtered.OrderByDescending(c => c.LicenseNumber).ToList();
                    break;
                case "IsAvailable":
                    filtered = isSortAscending ? filtered.OrderByDescending(c => c.IsAvailable).ThenBy(c => c.FullName).ToList() : filtered.OrderBy(c => c.IsAvailable).ThenBy(c => c.FullName).ToList();
                    break;
                case "AvailabilityStatus":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.AvailabilityStatus).ToList() : filtered.OrderByDescending(c => c.AvailabilityStatus).ToList();
                    break;
            }

            foreach (DataGridViewColumn col in gridCrew.Columns)
            {
                string baseText = col.HeaderText.Replace(" ▲", "").Replace(" ▼", "");
                col.HeaderText = col.DataPropertyName == currentSortColumn ? baseText + (isSortAscending ? " ▲" : " ▼") : baseText;
            }

            gridCrew.SelectionChanged -= GridCrew_SelectionChanged;
            gridCrew.DataSource = null;
            gridCrew.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Personnel Directory ({allCrews.Count})"
                : $"Active Personnel Directory - Showing {filtered.Count} of {allCrews.Count}";

            if (filtered.Count > 0)
            {
                gridCrew.ClearSelection();
                gridCrew.Rows[0].Selected = true;
                isBindingForm = true;
                selectedCrew = filtered[0];
                txtLastName.Text = selectedCrew.LastName ?? "";
                txtFirstName.Text = selectedCrew.FirstName ?? "";
                txtMiddleName.Text = selectedCrew.MiddleName ?? "";
                cbRole.Text = selectedCrew.Role;
                txtContact.Text = selectedCrew.ContactNumber;
                txtLicense.Text = selectedCrew.LicenseNumber;

                chkAvailable.Checked = selectedCrew.IsAvailable;
                chkIsSeasonal.Checked = selectedCrew.IsSeasonal;
                currentPhotoPath = selectedCrew.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = "Personnel Master Form";
                isBindingForm = false;
            }
            else
            {
                isBindingForm = true;
                selectedCrew = null;
                txtLastName.Text = "";
                txtFirstName.Text = "";
                txtMiddleName.Text = "";
                txtContact.Text = "";
                txtLicense.Text = "";
                lblFormHeader.Text = "Personnel Master Form";
                isBindingForm = false;
            }
            gridCrew.SelectionChanged += GridCrew_SelectionChanged;
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

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintHelper.PrintGridReport("Crew Personnel Roster & Wage Report", gridCrew, "Official driver and aide personnel directory with wage structure and duty availability.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            selectedCrew = null;
            txtLastName.Text = "";
            txtFirstName.Text = "";
            txtMiddleName.Text = "";
            txtContact.Text = "";
            txtLicense.Text = "";

            chkAvailable.Checked = true;
            chkIsSeasonal.Checked = false;
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(string.Empty);
            if (cbRole.Items.Count > 0) cbRole.SelectedIndex = 0;
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedCrew != null) RecordDetailsDialog.ShowDetails(this, selectedCrew);
            });
            var itemCopyName = new ToolStripMenuItem("📋 Copy Crew Full Name", null, (s, e) => {
                if (selectedCrew != null) { Clipboard.SetText(selectedCrew.FullName); ToastHelper.ShowToast(this, "Crew name copied ✓"); }
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Crew Details", null, (s, e) => {
                SetFormVisibility(true);
                txtLastName.Focus();
            });
            var itemAvail = new ToolStripMenuItem("🟢 Set Status: Active (Duty Ready)", null, (s, e) => {
                try
                {
                    if (selectedCrew != null) {
                        selectedCrew.IsAvailable = true;
                        selectedCrew.IsActive = true;
                        MJdatabase.SaveCrew(selectedCrew);
                        LoadCrew();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update crew availability: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var itemInactive = new ToolStripMenuItem("⚪ Set Status: Inactive", null, (s, e) => {
                try
                {
                    if (selectedCrew != null) {
                        selectedCrew.IsActive = false;
                        selectedCrew.IsAvailable = false;
                        MJdatabase.SaveCrew(selectedCrew);
                        LoadCrew();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update crew status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Personnel Register", null, BtnPrint_Click);
            var itemDelete = new ToolStripMenuItem("🗑️ Delete Personnel Record", null, BtnDelete_Click);
            var itemExportCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) => {
                DataExportHelper.ExportToCsv(gridCrew, "Crew_Personnel", "Registered Drivers and Aides");
            });
            var itemExportExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) => {
                DataExportHelper.ExportToExcelXml(gridCrew, "Crew_Personnel", "Registered Drivers and Aides");
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyName, new ToolStripSeparator(), itemEdit, itemAvail, itemInactive, itemPrint, new ToolStripSeparator(), itemExportCsv, itemExportExcel, new ToolStripSeparator(), itemDelete });

            gridCrew.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridCrew.ClearSelection();
                    gridCrew.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridCrew, gridCrew.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
