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
        private Button btnManageRoles = null!;

        private Crew? selectedCrew = null;
        private List<Crew> allCrews = new List<Crew>();
        private string currentSortColumn = "FullName";
        private bool isSortAscending = true;
        private string currentPhotoPath = string.Empty;
        private bool isBindingForm = false;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        public Action<int>? OnViewCrewHistoryRequested;
        public CrewManagementView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridCrew.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
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
            txtLastName.TextChanged += (s, e) => { if (!isBindingForm) HandleCrewInput(); };
            txtFirstName.TextChanged += (s, e) => { if (!isBindingForm) HandleCrewInput(); };
            txtLicense.TextChanged += (s, e) => { if (!isBindingForm && !isFormattingLicense) HandleCrewInput(); };
            txtLastName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtFirstName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtMiddleName.KeyPress += (s, e) => { if (char.IsDigit(e.KeyChar)) e.Handled = true; };
            LoadCrew();

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F);
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 1);
            SetButtonStates(false);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridCrew, "Crew_Personnel", "Crew Personnel"));
            menuActions.Items.Add("🖨️ Print Personnel Roster", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);
                    InitFullPageForm();
            InitListPage();
        }

        private float currentFormWidth = 400F;

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
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(68, 26),
                Padding = new Padding(6, 0, 6, 0),
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
            CreatePayFields();
            InitClassification();
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to crew");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Employment type", "Regular crew, or seasonal / temporary.", pnlEmployment),
                new FullPageFormKit.Section("Role & personal details", "Driver or delivery aide / helper, name and contact number.", lblRole, cbRole, lblRoleRequirements, tblNameFields, lblContact, txtContact),
                new FullPageFormKit.Section("Driver's license", "Classification and validity only — the license number is not stored.", pnlLicense),
                new FullPageFormKit.Section("Photo", "ID photo of the crew member.", lblPhoto, tblPhoto, pnlPhotoSpacer)
            });
            // Pay is set per trip after the trip is completed, and availability follows the trips — neither is edited here.
            tblPayFields.Visible = false;
            if (tblPayFields.Parent != null) tblPayFields.Parent.Controls.Remove(tblPayFields);
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New crew member";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridCrew.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridCrew.Rows[e.RowIndex].DataBoundItem is not Crew dc) return;
                gridCrew.ClearSelection();
                gridCrew.Rows[e.RowIndex].Selected = true;
                OpenCrewProfile(dc);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New crew member";
            SetFormVisibility(true);
        }
        private void SetFormVisibility(bool visible)
        {
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
            ShowListChrome(!visible);
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

        }

        private void SetupGridColumns()
        {
            gridCrew.ReadOnly = false;
            gridCrew.EditMode = DataGridViewEditMode.EditOnEnter;
            gridCrew.AutoGenerateColumns = false;
            gridCrew.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCrew.ScrollBars = ScrollBars.Both;
            gridCrew.AllowUserToResizeColumns = true;
            gridCrew.ShowCellToolTips = false;
            gridCrew.Columns.Clear();
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CrewCode", HeaderText = "Crew ID", FillWeight = 11F, MinimumWidth = 85, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "Full Name", FillWeight = 26F, MinimumWidth = 145, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "Role", FillWeight = 16F, MinimumWidth = 110, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ContactNumber", HeaderText = "Contact #", FillWeight = 16F, MinimumWidth = 110, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LicenseDisplay", HeaderText = "Driver's license", FillWeight = 22F, MinimumWidth = 150, ReadOnly = true });
            gridCrew.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AvailabilityStatus", HeaderText = "Status", FillWeight = 15F, MinimumWidth = 95, ReadOnly = true });
        }

        private static void ApplyButtonVisualState(Button btn, bool isEnabled, Color activeBgColor, Color activeFgColor)
        {
            btn.Enabled = isEnabled;
            if (isEnabled)
            {
                btn.BackColor = activeBgColor;
                btn.ForeColor = activeFgColor;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Pal.C(226, 232, 240);
                btn.ForeColor = Pal.C(148, 163, 184);
                btn.Cursor = Cursors.Default;
            }
        }

        private void SetButtonStates(bool isEditing)
        {
            string lic = txtLicense.Text.Trim();
            string ln = txtLastName.Text.Trim();
            string fn = txtFirstName.Text.Trim();
            Crew? matched = null;
            if (!string.IsNullOrWhiteSpace(lic) && !lic.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !lic.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                matched = allCrews.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.LicenseNumber) && c.LicenseNumber.Equals(lic, StringComparison.OrdinalIgnoreCase));
            }
            if (matched == null && !string.IsNullOrWhiteSpace(ln) && !string.IsNullOrWhiteSpace(fn))
            {
                matched = allCrews.FirstOrDefault(c => c.LastName.Equals(ln, StringComparison.OrdinalIgnoreCase) && c.FirstName.Equals(fn, StringComparison.OrdinalIgnoreCase));
            }

            bool exists = (selectedCrew != null || matched != null) && (!string.IsNullOrWhiteSpace(lic) || (!string.IsNullOrWhiteSpace(ln) && !string.IsNullOrWhiteSpace(fn)));

            btnSave.Enabled = !exists;
            btnSave.Visible = !exists;
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            ApplyButtonVisualState(btnSave, !exists, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, exists, Pal.C(25, 135, 84), Pal.White);
        }

        private void GridCrew_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridCrew.SelectedRows.Count > 0 && gridCrew.SelectedRows[0].DataBoundItem is Crew c)
            {
                BindCrewToForm(c);
            }
        }

        private void HandleCrewInput()
        {
            string lic = txtLicense.Text.Trim();
            string ln = txtLastName.Text.Trim();
            string fn = txtFirstName.Text.Trim();
            Crew? matched = null;
            if (!string.IsNullOrWhiteSpace(lic) && !lic.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !lic.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                matched = allCrews.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.LicenseNumber) && c.LicenseNumber.Equals(lic, StringComparison.OrdinalIgnoreCase));
            }
            if (matched == null && !string.IsNullOrWhiteSpace(ln) && !string.IsNullOrWhiteSpace(fn))
            {
                matched = allCrews.FirstOrDefault(c => c.LastName.Equals(ln, StringComparison.OrdinalIgnoreCase) && c.FirstName.Equals(fn, StringComparison.OrdinalIgnoreCase));
            }

            if (matched != null)
            {
                BindCrewToForm(matched);
                for (int i = 0; i < gridCrew.Rows.Count; i++)
                {
                    if (gridCrew.Rows[i].DataBoundItem is Crew rowC && rowC.Id == matched.Id)
                    {
                        gridCrew.ClearSelection();
                        gridCrew.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedCrew = null;
                SetButtonStates(false);
            }
        }

        // ------------------------------------------------------------------ Standard pay per trip
        private NumericUpDown numDailyWage = null!;
        private NumericUpDown numFoodAllowance = null!;
        private TableLayoutPanel tblPayFields = null!;
        private void CreatePayFields()
        {
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            NumericUpDown Money() => new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000, DecimalPlaces = 2, ThousandsSeparator = true, Increment = 50, Font = new Font("Segoe UI", 10F), TextAlign = HorizontalAlignment.Right, Margin = new Padding(0, 0, D(16), 0) };
            numDailyWage = Money();
            numFoodAllowance = Money();
            tblPayFields = new TableLayoutPanel { Height = D(62), ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 0, 0, D(8)) };
            tblPayFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblPayFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblPayFields.RowStyles.Add(new RowStyle(SizeType.Absolute, D(26)));
            tblPayFields.RowStyles.Add(new RowStyle(SizeType.Absolute, D(34)));
            Label L(string t) => new Label { Text = t, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary };
            tblPayFields.Controls.Add(L("Daily wage per trip (PHP)"), 0, 0);
            tblPayFields.Controls.Add(L("Meal / food allowance per trip (PHP)"), 1, 0);
            tblPayFields.Controls.Add(numDailyWage, 0, 1);
            tblPayFields.Controls.Add(numFoodAllowance, 1, 1);
        }
        private static decimal ClampMoney(NumericUpDown n, decimal v) => Math.Min(n.Maximum, Math.Max(n.Minimum, v));
        private void BindCrewToForm(Crew c)
        {
            isBindingForm = true;
            try
            {
                selectedCrew = c;
                txtLastName.Text = selectedCrew.LastName;
                txtFirstName.Text = selectedCrew.FirstName;
                txtMiddleName.Text = selectedCrew.MiddleName;
                txtContact.Text = selectedCrew.ContactNumber;
                txtLicense.Text = "";
                chkAvailable.Checked = selectedCrew.IsAvailable;
                BindClassification(selectedCrew);
                if (numDailyWage != null) numDailyWage.Value = ClampMoney(numDailyWage, selectedCrew.DailyWage);
                if (numFoodAllowance != null) numFoodAllowance.Value = ClampMoney(numFoodAllowance, selectedCrew.FoodAllowance);
                currentPhotoPath = selectedCrew.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = $"Personnel: {selectedCrew.FullName} ({selectedCrew.CrewCode})";
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
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
                string currentRole = cbRole.Text;
                FillRoleChoices(currentRole);
                if (!string.IsNullOrEmpty(currentRole))
                {
                    var exactMatch = cbRole.Items.Cast<string>().FirstOrDefault(i => i.Equals(currentRole.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (exactMatch != null)
                        cbRole.SelectedItem = exactMatch;
                    else if (currentRole.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0)
                        cbRole.SelectedItem = "Driver";
                    else if (currentRole.IndexOf("Mechanic", StringComparison.OrdinalIgnoreCase) >= 0)
                        cbRole.SelectedItem = "Mechanic";
                    else if (currentRole.IndexOf("Fabricator", StringComparison.OrdinalIgnoreCase) >= 0)
                        cbRole.SelectedItem = "Fabricator";
                    else
                        cbRole.SelectedItem = "Aide/Helper";
                }
                else if (cbRole.SelectedIndex < 0 && cbRole.Items.Count > 0)
                {
                    cbRole.SelectedIndex = 0;
                }

                allCrews = await MJdatabase.GetCrewsAsync();
                RefreshBusyCrew();
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
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (!ValidationHelper.ValidateRequiredText(txtLastName, "Last Name", valErrors)) firstInvalid ??= txtLastName;
                if (!ValidationHelper.ValidateRequiredText(txtFirstName, "First Name", valErrors)) firstInvalid ??= txtFirstName;
                if (!ValidationHelper.ValidateRequiredCombo(cbRole, "Crew Role", valErrors)) firstInvalid ??= cbRole;
                if (!ValidationHelper.ValidatePhoneNumber(txtContact, "Contact Number", valErrors)) firstInvalid ??= txtContact;
                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

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

                if (!string.IsNullOrWhiteSpace(middleName) && !SecurityHelper.IsValidName(middleName))
                {
                    MessageBox.Show("Please enter a valid middle name containing only letters, spaces, hyphens, and apostrophes, or leave it blank.", "Invalid Middle Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMiddleName.Focus();
                    return;
                }
                string contact = txtContact.Text.Trim();
                if (!string.IsNullOrEmpty(contact) && !SecurityHelper.IsValidPhone(contact))
                {
                    MessageBox.Show("Invalid contact number format. Please enter a valid mobile number (e.g. 09171234567).", "Invalid Contact #", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtContact.Focus();
                    return;
                }

                if (!ValidateClassification()) return;
                string role = cbRole.Text.Trim();
                string? duplicate = MJdatabase.ValidateDuplicateCrew(0, name, contact, null, lastName, firstName);
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
                    DailyWage = 0,
                    FoodAllowance = 0,
                    IsAvailable = true,
                    IsActive = true,
                    PhotoPath = currentPhotoPath
                };

                ApplyClassification(crew);
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

                if (!ValidateClassification()) return;
                string role = cbRole.Text.Trim();
                bool isDriver = role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
                bool wasDriver = selectedCrew.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
                if (wasDriver && !isDriver && MJdatabase.IsDriverOnActiveTrip(selectedCrew.Id))
                {
                    MessageBox.Show($"{selectedCrew.FullName} is the driver on an open dispatch. Complete or reassign that trip before changing their role.", "Driver on a trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbRole.Focus();
                    return;
                }

                string? duplicate = MJdatabase.ValidateDuplicateCrew(selectedCrew.Id, name, contact, null, lastName, firstName);
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
                selectedCrew.ContactNumber = contact;
                ApplyClassification(selectedCrew);
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

                var res = MessageBox.Show($"Are you sure you want to deactivate/delete crew record for '{selectedCrew.FullName}'?\n\n" + MJdatabase.DeletePolicyNote("Crews"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Crews", selectedCrew.Id);
                    if (deleted)
                    {
                        MessageBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    (!string.IsNullOrEmpty(c.CrewCode) && c.CrewCode.ToLower().Contains(q)) ||
                    c.Id.ToString() == q ||
                    (!string.IsNullOrEmpty(c.FullName) && c.FullName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.LastName) && c.LastName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.FirstName) && c.FirstName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.MiddleName) && c.MiddleName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Role) && c.Role.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.ContactNumber) && c.ContactNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.LicenseNumber) && c.LicenseNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.AvailabilityStatus) && c.AvailabilityStatus.ToLower().Contains(q))
                ).ToList();

            filtered = filtered.Where(CrewPassesTile).ToList();
            UpdateCrewTiles();
            switch (currentSortColumn)
            {
                case "CrewCode":
                    filtered = isSortAscending ? filtered.OrderBy(c => c.Id).ToList() : filtered.OrderByDescending(c => c.Id).ToList();
                    break;
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
            lblGridHeader.Text = string.IsNullOrEmpty(q) && crewTileFilter == null
                ? $"All crew · {allCrews.Count}"
                : $"Showing {filtered.Count} of {allCrews.Count} crew";
            if (pnlForm.Visible)
            {
                // The form is open (new or edit): leave what is being typed alone.
            }
            else if (filtered.Count > 0)
            {
                gridCrew.ClearSelection();
                gridCrew.Rows[0].Selected = true;
                isBindingForm = true;
                selectedCrew = filtered[0];
                txtLastName.Text = selectedCrew.LastName ?? "";
                txtFirstName.Text = selectedCrew.FirstName ?? "";
                txtMiddleName.Text = selectedCrew.MiddleName ?? "";
                txtContact.Text = selectedCrew.ContactNumber;
                txtLicense.Text = "";
                chkAvailable.Checked = selectedCrew.IsAvailable;
                BindClassification(selectedCrew);
                if (numDailyWage != null) numDailyWage.Value = ClampMoney(numDailyWage, selectedCrew.DailyWage);
                if (numFoodAllowance != null) numFoodAllowance.Value = ClampMoney(numFoodAllowance, selectedCrew.FoodAllowance);
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
            ClearClassification();
            if (numDailyWage != null) numDailyWage.Value = 0;
            if (numFoodAllowance != null) numFoodAllowance.Value = 0;
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(string.Empty);
            lblFormHeader.Text = "Personnel Master Form";
            SetButtonStates(false);
            gridCrew.ClearSelection();
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridCrew, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not Crew c) return;
                selectedCrew = c;
                bool onTrip = false;
                try { onTrip = c.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0 && MJdatabase.IsDriverOnActiveTrip(c.Id); } catch { }
                m.Items.AddRange(RowMenu.Header($"👤  {c.FullName}", $"{c.CrewCode} · {c.Role}{(c.IsSeasonal ? " (seasonal)" : "")} · {c.AvailabilityStatus} · license: {c.LicenseDisplay}"));
                m.Items.Add(RowMenu.Item("Open crew profile", () => OpenCrewProfile(c), "Enter"));
                m.Items.Add(RowMenu.Item("Quick details", () => RecordDetailsDialog.ShowDetails(this, c)));
                m.Items.Add(RowMenu.Item("Edit crew member", () => { SetFormVisibility(true); txtLastName.Focus(); }));
                m.Items.Add(RowMenu.Item("Trip & schedule history", () => OnViewCrewHistoryRequested?.Invoke(c.Id)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("Set status",
                    RowMenu.Item("🟢  Active (ready for duty)", () => SetCrewStatus(c, true), "", !(c.IsActive && c.IsAvailable)),
                    RowMenu.Item("⚪  Inactive", () => SetCrewStatus(c, false), "", c.IsActive)));
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item($"Pay slip — {thisMonth:MMMM yyyy}", () => PrintPaySlip(c, thisMonth, today)),
                    RowMenu.Item($"Pay slip — {thisMonth.AddMonths(-1):MMMM yyyy}", () => PrintPaySlip(c, thisMonth.AddMonths(-1), thisMonth.AddDays(-1))),
                    RowMenu.Item("Crew dossier & trip history", () => PrintHelper.PrintCrewDossierAndHistory(c, MJdatabase.GetCrewHistory(c.Id))),
                    RowMenu.Sep(),
                    RowMenu.Item("Personnel register (whole table)", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Full name", c.FullName), ("Crew code", c.CrewCode), ("Contact number", c.ContactNumber), ("Driver's license", c.LicenseDisplay),
                    ("Contact card", $"{c.FullName} ({c.Role}){(string.IsNullOrWhiteSpace(c.ContactNumber) ? "" : " — " + c.ContactNumber)}")));
                m.Items.Add(RowMenu.Item($"Find other {c.Role}s", () => RowMenu.Search(txtGridSearch, c.Role)));
                m.Items.Add(RowMenu.ExportSub(gridCrew, "Crew_Personnel", "Registered Drivers and Aides", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete crew member…", () => { selectedCrew = c; BtnDelete_Click(this, EventArgs.Empty); }, !onTrip));
            });
        }
        private void SetCrewStatus(Crew c, bool active)
        {
            if (!active && c.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0 && MJdatabase.IsDriverOnActiveTrip(c.Id))
            {
                MessageBox.Show($"{c.FullName} is assigned to an open dispatch. Complete or reassign that trip before setting them to inactive.", "Crew on a trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool wasActive = c.IsActive, wasAvail = c.IsAvailable;
            c.IsActive = active;
            c.IsAvailable = active;
            try
            {
                MJdatabase.SaveCrew(c);
                ToastHelper.ShowToast(this, $"{c.FullName} is now {(active ? "active" : "inactive")}", ToastType.Success);
            }
            catch (Exception ex)
            {
                c.IsActive = wasActive; c.IsAvailable = wasAvail;
                MessageBox.Show($"Failed to update crew status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            LoadCrew();
        }
        /// <summary>Pay per trip must be set (it is what Trip Reports and payroll use), and the meal allowance can't exceed it.</summary>
        private bool ValidateCrewPay()
        {
            if (numDailyWage == null) return true;
            if (numDailyWage.Value <= 0)
            {
                var ans = MessageBox.Show("No pay per trip is set for this crew member, so their trip pay records will show ₱0.00.\n\nSave anyway?", "Pay per trip is ₱0", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (ans != DialogResult.Yes) { numDailyWage.Focus(); return false; }
            }
            if (numFoodAllowance != null && numDailyWage.Value > 0 && numFoodAllowance.Value > numDailyWage.Value)
            {
                var ans = MessageBox.Show("The meal / food allowance is higher than the pay per trip. Is that correct?", "Check the allowance", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (ans != DialogResult.Yes) { numFoodAllowance.Focus(); return false; }
            }
            return true;
        }
        private void PrintPaySlip(Crew c, DateTime from, DateTime to)
        {
            var trips = MJdatabase.GetDeliveries().Where(d =>
                string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                d.DispatchDate.Date >= from.Date && d.DispatchDate.Date <= to.Date &&
                MJdatabase.GetDeliveryCrewIds(d).Any(x => x.CrewId == c.Id)).ToList();
            if (trips.Count == 0)
            {
                MessageBox.Show($"{c.FullName} has no completed trips from {from:MMM d} to {to:MMM d, yyyy}.", "No trips to pay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintCrewPaySlip(c, trips, from, to);
        }
    
        private void SetupRoleFieldWithManageButton()
        {
            int roleIdx = pnlFormBody.Controls.IndexOf(cbRole);
            if (roleIdx >= 0)
            {
                pnlFormBody.Controls.Remove(cbRole);

                TableLayoutPanel tblRoleWrap = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 32,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(0)
                };
                tblRoleWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                tblRoleWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
                tblRoleWrap.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                btnManageRoles = new Button
                {
                    Dock = DockStyle.Fill,
                    Text = "⚙️ Roles",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Pal.C(241, 245, 249),
                    ForeColor = Pal.C(30, 41, 59),
                    Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0)
                };
                btnManageRoles.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
                btnManageRoles.FlatAppearance.BorderSize = 1;
                btnManageRoles.Click += BtnManageRoles_Click;

                cbRole.Dock = DockStyle.Fill;
                cbRole.Margin = new Padding(0, 0, 6, 0);

                tblRoleWrap.Controls.Add(cbRole, 0, 0);
                tblRoleWrap.Controls.Add(btnManageRoles, 1, 0);

                pnlFormBody.Controls.Add(tblRoleWrap);
                pnlFormBody.Controls.SetChildIndex(tblRoleWrap, roleIdx);
            }
        }

        private void BtnManageRoles_Click(object? sender, EventArgs e)
        {
            string currentRole = cbRole.Text;
            using (var dlg = new Forms.CrewRoleManageDialog(currentRole))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK || !string.IsNullOrWhiteSpace(dlg.SelectedRoleName))
                {
                    LoadCrew();
                    if (!string.IsNullOrWhiteSpace(dlg.SelectedRoleName))
                    {
                        var match = cbRole.Items.Cast<string>().FirstOrDefault(i => i.Equals(dlg.SelectedRoleName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            cbRole.SelectedItem = match;
                        }
                        else
                        {
                            cbRole.Items.Add(dlg.SelectedRoleName.Trim());
                            cbRole.SelectedItem = dlg.SelectedRoleName.Trim();
                        }
                    }
                }
            }
        }
    }
}
