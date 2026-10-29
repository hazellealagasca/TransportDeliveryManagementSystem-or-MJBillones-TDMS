using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class TruckManagementView : UserControl, IRefreshable
    {
        private Button btnSubTabFleet = null!;
        private Button btnSubTabCategories = null!;
        private Panel pnlSubNavTabs = null!;
        private Panel pnlCategoriesContainer = null!;
        private DataGridView gridTruckCategories = null!;
        private TextBox txtCatName = null!;
        private NumericUpDown numCatCapacity = null!;
        private Button btnAddCat = null!;
        private Button btnUpdateCat = null!;
        private Button btnDeleteCat = null!;
        private Button btnClearCat = null!;
        private TextBox txtCatSearch = null!;
        private Label lblCatSummary = null!;
        private List<TruckCategoryItem> allTruckCategories = new List<TruckCategoryItem>();
        private TruckCategoryItem? selectedTruckCategory = null;

        private Truck? selectedTruck = null;
        private bool isBindingForm = false;
        private List<Truck> allTrucks = new List<Truck>();
        private string currentSortColumn = "PlateNumber";
        private bool isSortAscending = true;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private Action? _categoriesHandler;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Truck>? OnDispatchTruckRequested { get; set; }

        private void SetupEvents()
        {
            cbType.SelectedIndexChanged += (s, e) =>
            {
                if (!isBindingForm && !string.IsNullOrWhiteSpace(cbType.Text))
                {
                    int cap = MJdatabase.GetTruckTypeCapacity(cbType.Text.Trim());
                    if (cap > 0 && (selectedTruck == null || selectedTruck.Id == 0))
                    {
                        numCapacity.Value = cap;
                    }
                }
            };
        }

        public TruckManagementView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            InitializeSubNavAndCategories();
            gridTrucks.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridTrucks.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridTrucks.Columns.Count) return;
                string col = gridTrucks.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            SetupEvents();
            LoadTrucks();
            _categoriesHandler = () => { if (!this.IsDisposed && this.IsHandleCreated) this.BeginInvoke(new Action(LoadTrucks)); };
            MJdatabase.OnCategoriesChanged += _categoriesHandler;

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F);
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 1);
            txtPlate.Leave += (s, e) => txtPlate.Text = txtPlate.Text.Trim().ToUpper();
            txtPlate.TextChanged += (s, e) => { if (!isBindingForm) HandlePlateInput(); };
            SetButtonStates(false);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => {
                try { DataExportHelper.ExportToCsv(gridTrucks, "Truck_Vehicles", "Registered Trucks"); }
                catch (Exception ex) { ErrLog.Error("TruckManagementView", ex);  MessageBox.Show($"Export error: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            });
            menuActions.Items.Add("🖨 Print Truck Report", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);
                    InitInlineCategories();
                    InitFullPageForm();
            InitListPage();
        }

        private float currentFormWidth = 400F;

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_categoriesHandler != null)
            {
                MJdatabase.OnCategoriesChanged -= _categoriesHandler;
            }
            base.OnHandleDestroyed(e);
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
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to trucks");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Vehicle", "Plate number, type, cargo zone and status.", tblPlateType, tblCargoStatus),
                new FullPageFormKit.Section("Capacity", "Fills in from the truck category above; change it for this truck if needed. A new category typed above is added when you save.", lblCap, numCapacity, lblCatHint, chkSaveCatDefault, lnkManageCats),
                new FullPageFormKit.Section("Remarks", "Anything to note about this truck.", lblRemarks, txtRemarks, pnlRemarksSpacer)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New truck";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridTrucks.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridTrucks.Rows[e.RowIndex].DataBoundItem is not Truck t) return;
                gridTrucks.ClearSelection();
                gridTrucks.Rows[e.RowIndex].Selected = true;
                OpenTruckProfile(t);
            };
            SetFormVisibility(false);
            ProfileKit.Suggest(txtRemarks, "truck-remarks");
        }
        /// <summary>Specs, dispatch history, maintenance log and incidents for one truck.</summary>
        private void OpenTruckProfile(Truck t)
        {
            Truck? toEdit = null;
            Forms.TruckProfileDialog.ShowFor(FindForm(), t, SessionContext.IsAdmin ? (Action<Truck>)(tr => toEdit = tr) : null);
            if (toEdit != null)
            {
                BindTruckToForm(toEdit);
                SetFormVisibility(true);
                txtPlate.Focus();
            }
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New truck";
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
            LoadTrucks();
            LoadTruckCategories();
        }

        private void SetupGridColumns()
        {
            gridTrucks.AutoGenerateColumns = false;
            gridTrucks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridTrucks.ScrollBars = ScrollBars.Both;
            gridTrucks.AllowUserToResizeColumns = true;
            gridTrucks.ShowCellToolTips = false;
            gridTrucks.Columns.Clear();
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PlateNumber", HeaderText = "Plate Number", FillWeight = 14F, MinimumWidth = 110, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckType", HeaderText = "Truck Category", FillWeight = 18F, MinimumWidth = 125, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CargoType", HeaderText = "Cargo / Temp Type", FillWeight = 16F, MinimumWidth = 120, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CapacityCrates", HeaderText = "Capacity", FillWeight = 12F, MinimumWidth = 85, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 12F, MinimumWidth = 85, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Remarks", HeaderText = "Condition Remarks", FillWeight = 28F, MinimumWidth = 135, ReadOnly = true });
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
            string plate = txtPlate.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(plate)
                ? allTrucks.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.PlateNumber) && t.PlateNumber.Equals(plate, StringComparison.OrdinalIgnoreCase))
                : null;
            bool exists = (selectedTruck != null || matched != null) && !string.IsNullOrWhiteSpace(plate);

            btnAdd.Enabled = !exists;
            btnAdd.Visible = !exists;
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            ApplyButtonVisualState(btnAdd, !exists, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, exists, Pal.C(25, 135, 84), Pal.White);
        }

        private void GridTrucks_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridTrucks.SelectedRows.Count > 0 && gridTrucks.SelectedRows[0].DataBoundItem is Truck t)
            {
                BindTruckToForm(t);
            }
        }

        private void HandlePlateInput()
        {
            string plate = txtPlate.Text.Trim();
            if (string.IsNullOrWhiteSpace(plate))
            {
                selectedTruck = null;
                lblFormHeader.Text = "Truck Vehicle Details";
                SetButtonStates(false);
                return;
            }

            var matched = allTrucks.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.PlateNumber) && t.PlateNumber.Equals(plate, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                BindTruckToForm(matched);
                for (int i = 0; i < gridTrucks.Rows.Count; i++)
                {
                    if (gridTrucks.Rows[i].DataBoundItem is Truck rowT && rowT.Id == matched.Id)
                    {
                        gridTrucks.ClearSelection();
                        gridTrucks.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedTruck = null;
                lblFormHeader.Text = $"Truck: {plate} (New)";
                SetButtonStates(false);
            }
        }

        private void BindTruckToForm(Truck t)
        {
            isBindingForm = true;
            try
            {
                selectedTruck = t;
                txtPlate.Text = selectedTruck.PlateNumber;
                cbType.Text = selectedTruck.TruckType;
                cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedTruck.CargoType) ? "Cold / Chilled" : selectedTruck.CargoType;
                numCapacity.Value = selectedTruck.CapacityCrates;
                string curStatus = selectedTruck.Status;
                if (curStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)) curStatus = "Active";
                cbStatus.SelectedItem = curStatus;
                txtRemarks.Text = selectedTruck.Remarks;
                lblFormHeader.Text = $"Truck: {selectedTruck.PlateNumber}";
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
            ResetCategoryHint();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (!ValidationHelper.ValidateRequiredText(txtPlate, "Plate Number", valErrors)) firstInvalid ??= txtPlate;
                if (string.IsNullOrWhiteSpace(cbType.Text)) { valErrors.Add("Truck category is required (pick one or type a new one)."); firstInvalid ??= cbType; }
                if (!ValidationHelper.ValidatePositiveNumber(numCapacity, "Crate Capacity", valErrors)) firstInvalid ??= numCapacity;

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (!SessionContext.IsAdmin)
                {
                    MessageBox.Show("Administrator privileges are required to add new vehicles.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string plateNumber = txtPlate.Text.Trim();
                if (string.IsNullOrWhiteSpace(plateNumber))
                {
                    MessageBox.Show("Please enter a valid Plate Number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPlate.Focus();
                    return;
                }

                if (!SecurityHelper.IsValidPlateNumber(plateNumber))
                {
                    MessageBox.Show("Invalid Philippine Plate Number format.\n\nAccepted Philippine LTO standard formats:\n• 3 Letters + 4 Digits (e.g. ABC 1234 or ABC-1234)\n• 3 Letters + 3 Digits (e.g. ABC 123 or ABC-123)\n• 2 Letters + 4 Digits (e.g. NC 9012 or NC-9012)\n• Trailer Format: 4 Digits + 2 Letters (e.g. 1234-AB)", "Invalid Plate Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPlate.Focus();
                    return;
                }

                if (MJdatabase.IsPlateNumberExists(plateNumber, 0))
                {
                    MessageBox.Show($"DUPLICATE VEHICLE PLATE NUMBER BLOCKED:\nA truck with Plate Number '{plateNumber}' is already registered in the system. Duplicate plate numbers are strictly prohibited.", "Duplicate Plate Number Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    txtPlate.Focus();
                    return;
                }

                int capacity = (int)numCapacity.Value;
                if (capacity <= 0)
                {
                    MessageBox.Show("Please specify a valid crate capacity greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numCapacity.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cbType.Text))
                {
                    MessageBox.Show("Choose a truck category, or type a new one.", "Truck category required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbType.Focus();
                    return;
                }
                string? truckType = CommitCategoryFromForm(capacity);
                if (truckType == null) return;
                Truck truck = new Truck
                {
                    CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled",
                    PlateNumber = plateNumber,
                    TruckType = truckType,
                    CapacityCrates = capacity,
                    Status = cbStatus.SelectedItem?.ToString() ?? "Active",
                    Remarks = txtRemarks.Text.Trim()
                };

                MJdatabase.SaveTruck(truck);
                QuietBox.Show($"Truck '{plateNumber}' ({truckType} - {capacity} crates) added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadTrucks();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                MessageBox.Show(ex.Message, "Truck Management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!SessionContext.IsAdmin)
                {
                    MessageBox.Show("Administrator privileges are required to update vehicle records.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedTruck == null)
                {
                    MessageBox.Show("Please select a truck from the list on the right before clicking Update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string plateNumber = txtPlate.Text.Trim();
                if (string.IsNullOrWhiteSpace(plateNumber))
                {
                    MessageBox.Show("Please enter a valid Plate Number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPlate.Focus();
                    return;
                }

                if (!SecurityHelper.IsValidPlateNumber(plateNumber))
                {
                    MessageBox.Show("Invalid Philippine Plate Number format.\n\nAccepted Philippine LTO standard formats:\n• 3 Letters + 4 Digits (e.g. ABC 1234 or ABC-1234)\n• 3 Letters + 3 Digits (e.g. ABC 123 or ABC-123)\n• 2 Letters + 4 Digits (e.g. NC 9012 or NC-9012)\n• Trailer Format: 4 Digits + 2 Letters (e.g. 1234-AB)", "Invalid Plate Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPlate.Focus();
                    return;
                }

                if (MJdatabase.IsPlateNumberExists(plateNumber, selectedTruck.Id))
                {
                    MessageBox.Show($"DUPLICATE VEHICLE PLATE NUMBER BLOCKED:\nA different truck with Plate Number '{plateNumber}' already exists in the system. Duplicate plate numbers are strictly prohibited.", "Duplicate Plate Number Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    txtPlate.Focus();
                    return;
                }

                int capacity = (int)numCapacity.Value;
                if (capacity <= 0)
                {
                    MessageBox.Show("Please specify a valid crate capacity greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numCapacity.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cbType.Text))
                {
                    MessageBox.Show("Choose a truck category, or type a new one.", "Truck category required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbType.Focus();
                    return;
                }
                string newStatus = cbStatus.SelectedItem?.ToString() ?? "Active";
                if (MJdatabase.IsTruckBlockedStatus(newStatus) && !MJdatabase.IsTruckBlockedStatus(selectedTruck.Status) && MJdatabase.IsTruckOnActiveTrip(selectedTruck.Id))
                {
                    MessageBox.Show($"Truck {selectedTruck.PlateNumber} is assigned to an open dispatch. Complete or reassign that trip before setting it to '{newStatus}'.", "Truck on a trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbStatus.Focus();
                    return;
                }
                string? truckType = CommitCategoryFromForm(capacity);
                if (truckType == null) return;
                selectedTruck.PlateNumber = plateNumber;
                selectedTruck.TruckType = truckType;
                selectedTruck.CapacityCrates = capacity;
                selectedTruck.Status = cbStatus.SelectedItem?.ToString() ?? "Active";
                selectedTruck.Remarks = txtRemarks.Text.Trim();
                selectedTruck.CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled";

                MJdatabase.SaveTruck(selectedTruck);
                QuietBox.Show($"Truck '{plateNumber}' updated successfully! New capacity: {capacity} crates.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadTrucks();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                MessageBox.Show(ex.Message, "Truck Management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!SessionContext.IsAdmin)
                {
                    MessageBox.Show("Administrator privileges are required to delete vehicle records.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedTruck == null)
                {
                    MessageBox.Show("Please select a truck to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Trucks", selectedTruck.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete truck {selectedTruck.PlateNumber}?\n\n" + MJdatabase.DeletePolicyNote("Trucks"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Trucks", selectedTruck.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadTrucks();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete truck record. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                MessageBox.Show($"Failed to delete truck record: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ? allTrucks
                : allTrucks.Where(t =>
                    (!string.IsNullOrEmpty(t.PlateNumber) && t.PlateNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.TruckType) && t.TruckType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.CargoType) && t.CargoType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.Status) && t.Status.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.Remarks) && t.Remarks.ToLower().Contains(q))
                ).ToList();

            filtered = filtered.Where(TruckPassesTile).ToList();
            UpdateTruckTiles();
            switch (currentSortColumn)
            {
                case "PlateNumber":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.PlateNumber).ToList() : filtered.OrderByDescending(t => t.PlateNumber).ToList();
                    break;
                case "TruckType":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.TruckType).ThenBy(t => t.PlateNumber).ToList() : filtered.OrderByDescending(t => t.TruckType).ThenBy(t => t.PlateNumber).ToList();
                    break;
                case "CargoType":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.CargoType).ToList() : filtered.OrderByDescending(t => t.CargoType).ToList();
                    break;
                case "CapacityCrates":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.CapacityCrates).ToList() : filtered.OrderByDescending(t => t.CapacityCrates).ToList();
                    break;
                case "Status":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.Status).ToList() : filtered.OrderByDescending(t => t.Status).ToList();
                    break;
                case "Remarks":
                    filtered = isSortAscending ? filtered.OrderBy(t => t.Remarks).ToList() : filtered.OrderByDescending(t => t.Remarks).ToList();
                    break;
            }

            foreach (DataGridViewColumn col in gridTrucks.Columns)
            {
                string baseText = col.HeaderText.Replace(" ▲", "").Replace(" ▼", "");
                col.HeaderText = col.DataPropertyName == currentSortColumn ? baseText + (isSortAscending ? " ▲" : " ▼") : baseText;
            }

            gridTrucks.SelectionChanged -= GridTrucks_SelectionChanged;
            gridTrucks.DataSource = null;
            gridTrucks.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q) && truckTileFilter == null
                ? $"All trucks · {allTrucks.Count}"
                : $"Showing {filtered.Count} of {allTrucks.Count} trucks";
            if (filtered.Count > 0)
            {
                gridTrucks.ClearSelection();
                gridTrucks.Rows[0].Selected = true;
                if (!pnlForm.Visible || selectedTruck == null)
                {
                    isBindingForm = true;
                    selectedTruck = filtered[0];
                    txtPlate.Text = selectedTruck.PlateNumber;
                    cbType.Text = selectedTruck.TruckType;
                    cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedTruck.CargoType) ? "Cold / Chilled" : selectedTruck.CargoType;
                    numCapacity.Value = selectedTruck.CapacityCrates;
                    cbStatus.SelectedItem = selectedTruck.Status;
                    txtRemarks.Text = selectedTruck.Remarks;
                    lblFormHeader.Text = "Truck Vehicle Details";
                    isBindingForm = false;
                }
            }
            else
            {
                if (!pnlForm.Visible || selectedTruck == null)
                {
                    isBindingForm = true;
                    selectedTruck = null;
                    txtPlate.Text = "";
                    txtRemarks.Text = "";
                    lblFormHeader.Text = "Truck Vehicle Details";
                    isBindingForm = false;
                }
            }
            gridTrucks.SelectionChanged += GridTrucks_SelectionChanged;
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
                PrintHelper.PrintGridReport("Truck & Vehicle Inventory Report", gridTrucks, "Official truck inventory statement including plate numbers, crate capacities, and operational statuses.");
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            selectedTruck = null;
            txtPlate.Text = "";
            cbType.SelectedIndex = -1;
            cbType.Text = "";
            numCapacity.Value = 300;
            cbStatus.SelectedIndex = 0;
            cbCargoType.SelectedIndex = 0;
            txtRemarks.Text = "";
            lblFormHeader.Text = "Truck Vehicle Details";
            SetButtonStates(false);
            gridTrucks.ClearSelection();
            ResetCategoryHint();
        }

        private async void LoadTrucks()
        {
            try
            {
                var categories = MJdatabase.GetTruckCategories();
                allTruckCategories = categories;
                string currentType = cbType.Text;
                cbType.Items.Clear();
                foreach (var c in categories) cbType.Items.Add(c.CategoryName);
                if (!string.IsNullOrWhiteSpace(currentType) && !cbType.Items.Contains(currentType))
                {
                    cbType.Text = currentType;   // a new category typed in the form stays until it is saved
                }
                else if (cbType.Items.Count > 0)
                {
                    if (!string.IsNullOrEmpty(currentType) && cbType.Items.Contains(currentType))
                        cbType.SelectedItem = currentType;
                    else if (cbType.SelectedIndex < 0)
                        cbType.SelectedIndex = 0;
                }

                if (cbCargoType.Items.Count == 0)
                {
                    cbCargoType.Items.AddRange(new object[] { "Cold / Chilled", "Dry Goods", "Dry and Chilled Mix" });
                }
                if (cbCargoType.SelectedIndex < 0) cbCargoType.SelectedIndex = 0;

                if (cbStatus.Items.Count == 0)
                {
                    cbStatus.Items.AddRange(new object[] { "Active", "In Use", "Under Maintenance", "Inactive" });
                }
                if (cbStatus.SelectedIndex < 0) cbStatus.SelectedIndex = 0;

                allTrucks = await MJdatabase.GetTrucksAsync();
                RefreshBusyTrucks();
                if (!this.IsDisposed)
                {
                    FilterGridData();
                }
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                if (!this.IsDisposed)
                {
                    MessageBox.Show($"Failed to load truck data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDispatchTruck_Click(object? sender, EventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Please select a vehicle from the trucks table first.", "Select Vehicle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedTruck.Status.Equals("Under Maintenance", StringComparison.OrdinalIgnoreCase) ||
                selectedTruck.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Cannot dispatch vehicle '{selectedTruck.PlateNumber}' because its current status is '{selectedTruck.Status}'.", "Vehicle Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OnDispatchTruckRequested?.Invoke(selectedTruck);
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridTrucks, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not Truck t) return;
                selectedTruck = t;
                string status = string.IsNullOrWhiteSpace(t.Status) ? "Active" : t.Status;
                bool blocked = MJdatabase.IsTruckBlockedStatus(status);
                bool onTrip = false;
                try { onTrip = MJdatabase.IsTruckOnActiveTrip(t.Id); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                m.Items.AddRange(RowMenu.Header($"🚚  {t.PlateNumber}", $"{t.TruckType} · {t.CapacityCrates} crates · {status}{(onTrip ? " · assigned to a dispatch" : "")}"));
                m.Items.Add(RowMenu.Item("Open truck profile", () => OpenTruckProfile(t), "Enter"));
                m.Items.Add(RowMenu.Item("Quick details", () => RecordDetailsDialog.ShowDetails(this, t)));
                m.Items.Add(RowMenu.Item("Edit truck", () => { SetFormVisibility(true); txtPlate.Focus(); }, "", SessionContext.IsAdmin));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Good("Dispatch this truck", () => BtnDispatchTruck_Click(this, EventArgs.Empty), !blocked && !onTrip));
                m.Items.Add(RowMenu.Sub("Set status",
                    RowMenu.Item("🟢  Active", () => SetTruckStatus(t, "Active"), "", !status.Equals("Active", StringComparison.OrdinalIgnoreCase)),
                    RowMenu.Item("🔧  Under maintenance", () => SetTruckStatus(t, "Under Maintenance"), "", !status.Equals("Under Maintenance", StringComparison.OrdinalIgnoreCase)),
                    RowMenu.Item("⚪  Inactive", () => SetTruckStatus(t, "Inactive"), "", !status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Plate number", t.PlateNumber), ("Truck type", t.TruckType),
                    ("Summary", $"{t.PlateNumber} — {t.TruckType}, {t.CapacityCrates} crates, {t.CargoType}, {status}")));
                m.Items.Add(RowMenu.Item("Find trucks of this type", () => RowMenu.Search(txtGridSearch, t.TruckType)));
                m.Items.Add(RowMenu.ExportSub(gridTrucks, "Truck_Vehicles", "Registered Trucks", () => BtnPrint_Click(this, EventArgs.Empty)));
                if (SessionContext.IsAdmin)
                {
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.Danger("Delete truck…", () => { selectedTruck = t; BtnDelete_Click(this, EventArgs.Empty); }, !onTrip));
                }
            });
        }
        /// <summary>Changes a truck's status from the list, with the same rules as the form.</summary>
        private void SetTruckStatus(Truck t, string status)
        {
            if (!SessionContext.IsAdmin)
            {
                MessageBox.Show("Administrator privileges are required to change a vehicle's status.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MJdatabase.IsTruckBlockedStatus(status) && MJdatabase.IsTruckOnActiveTrip(t.Id))
            {
                MessageBox.Show($"Truck {t.PlateNumber} is assigned to an open dispatch. Complete or reassign that trip before setting it to '{status}'.", "Truck on a trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string old = t.Status;
            t.Status = status;
            try
            {
                MJdatabase.SaveTruck(t);
                ToastHelper.ShowToast(this, $"{t.PlateNumber} is now {status}", ToastType.Success);
            }
            catch (Exception ex)
            { ErrLog.Error("TruckManagementView", ex); 
                t.Status = old;
                MessageBox.Show($"Failed to update truck status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            LoadTrucks();
        }
    
        private void InitializeSubNavAndCategories()
        {
            pnlSubNavTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Pal.C(248, 250, 252),
                Padding = new Padding(16, 6, 16, 6)
            };

            FlowLayoutPanel flowTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnSubTabFleet = new Button
            {
                Text = "🚚  Registered Trucks",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 32),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                Cursor = Cursors.Hand
            };
            btnSubTabFleet.FlatAppearance.BorderSize = 1;
            btnSubTabFleet.FlatAppearance.BorderColor = Pal.C(15, 23, 42);
            btnSubTabFleet.Click += (s, e) => SwitchSubTab("Fleet");

            btnSubTabCategories = new Button
            {
                Text = "🏷️  Truck Categories & Capacities",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(190, 32),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Pal.White,
                ForeColor = Pal.C(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btnSubTabCategories.FlatAppearance.BorderSize = 1;
            btnSubTabCategories.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
            btnSubTabCategories.Click += (s, e) => SwitchSubTab("Categories");

            flowTabs.Controls.Add(btnSubTabFleet);
            flowTabs.Controls.Add(btnSubTabCategories);
            pnlSubNavTabs.Controls.Add(flowTabs);

            pnlCategoriesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(16)
            };

            TableLayoutPanel tblCatLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Pal.C(241, 245, 249)
            };
            tblCatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            tblCatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblCatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlCatForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(16)
            };

            Label lblCatHeader = new Label
            {
                Text = "Truck Category & Capacity Details",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 30
            };

            Label lblCatName = new Label
            {
                Text = "Truck Category / Classification Name:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Height = 22
            };

            txtCatName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                MaxLength = 100
            };

            Panel pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = 10 };

            Label lblCatCap = new Label
            {
                Text = "Default Crate Capacity (crates):",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Height = 22
            };

            numCatCapacity = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 1000000,
                Value = 300
            };

            Panel pnlSpacer2 = new Panel { Dock = DockStyle.Top, Height = 16 };

            FlowLayoutPanel flowCatBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            btnAddCat = new Button
            {
                Text = "+ Add Category",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddCat.FlatAppearance.BorderSize = 0;
            btnAddCat.Click += BtnAddCat_Click;

            btnUpdateCat = new Button
            {
                Text = "Update",
                Size = new Size(80, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Pal.C(22, 163, 74),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdateCat.FlatAppearance.BorderSize = 0;
            btnUpdateCat.Click += BtnUpdateCat_Click;

            btnDeleteCat = new Button
            {
                Text = "Delete",
                Size = new Size(80, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Pal.C(220, 38, 38),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteCat.FlatAppearance.BorderSize = 0;
            btnDeleteCat.Click += BtnDeleteCat_Click;

            btnClearCat = new Button
            {
                Text = "Clear",
                Size = new Size(70, 32),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearCat.FlatAppearance.BorderSize = 1;
            btnClearCat.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClearCat.Click += (s, e) => ClearCatInputs();

            flowCatBtns.Controls.Add(btnAddCat);
            flowCatBtns.Controls.Add(btnUpdateCat);
            flowCatBtns.Controls.Add(btnDeleteCat);
            flowCatBtns.Controls.Add(btnClearCat);

            pnlCatForm.Controls.Add(flowCatBtns);
            pnlCatForm.Controls.Add(pnlSpacer2);
            pnlCatForm.Controls.Add(numCatCapacity);
            pnlCatForm.Controls.Add(lblCatCap);
            pnlCatForm.Controls.Add(pnlSpacer1);
            pnlCatForm.Controls.Add(txtCatName);
            pnlCatForm.Controls.Add(lblCatName);
            pnlCatForm.Controls.Add(lblCatHeader);

            Panel pnlCatGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(0)
            };

            Panel pnlCatToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 6, 12, 6),
                BackColor = Pal.White
            };

            lblCatSummary = new Label
            {
                Text = "📊 Total Truck Classifications: 0",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            Panel pnlSearchWrap = new Panel { Dock = DockStyle.Right, Width = 220 };
            txtCatSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Search categories..."
            };
            txtCatSearch.TextChanged += (s, e) => FilterTruckCategories();
            pnlSearchWrap.Controls.Add(txtCatSearch);

            pnlCatToolbar.Controls.Add(pnlSearchWrap);
            pnlCatToolbar.Controls.Add(lblCatSummary);

            gridTruckCategories = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                ShowCellToolTips = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category / Model Classification", FillWeight = 50, MinimumWidth = 160 });
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Default Crate Capacity", FillWeight = 25, MinimumWidth = 120 });
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Active Fleet Assigned", FillWeight = 25, MinimumWidth = 120 });
            gridTruckCategories.SelectionChanged += GridTruckCategories_SelectionChanged;

            pnlCatGridCard.Controls.Add(gridTruckCategories);
            pnlCatGridCard.Controls.Add(pnlCatToolbar);

            tblCatLayout.Controls.Add(pnlCatForm, 0, 0);
            tblCatLayout.Controls.Add(pnlCatGridCard, 1, 0);
            pnlCategoriesContainer.Controls.Add(tblCatLayout);

            this.Controls.Add(pnlCategoriesContainer);
            this.Controls.Add(pnlSubNavTabs);

            pnlSubNavTabs.BringToFront();
            tblMasterContent.BringToFront();
        }

        private void SwitchSubTab(string tab)
        {
            if (tab == "Fleet")
            {
                btnSubTabFleet.BackColor = Pal.C(15, 23, 42);
                btnSubTabFleet.ForeColor = Pal.White;
                btnSubTabFleet.FlatAppearance.BorderColor = Pal.C(15, 23, 42);
                btnSubTabCategories.BackColor = Pal.White;
                btnSubTabCategories.ForeColor = Pal.C(71, 85, 105);
                btnSubTabCategories.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
                pnlCategoriesContainer.Visible = false;
                tblMasterContent.Visible = true;
                tblMasterContent.BringToFront();
            }
            else
            {
                btnSubTabCategories.BackColor = Pal.C(15, 23, 42);
                btnSubTabCategories.ForeColor = Pal.White;
                btnSubTabCategories.FlatAppearance.BorderColor = Pal.C(15, 23, 42);
                btnSubTabFleet.BackColor = Pal.White;
                btnSubTabFleet.ForeColor = Pal.C(71, 85, 105);
                btnSubTabFleet.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
                tblMasterContent.Visible = false;
                pnlCategoriesContainer.Visible = true;
                pnlCategoriesContainer.BringToFront();
                LoadTruckCategories();
            }
        }

        private void LoadTruckCategories()
        {
            try
            {
                allTruckCategories = MJdatabase.GetTruckCategories();
                FilterTruckCategories();
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to load truck categories: {ex.Message}"); ErrLog.Swallowed(ex); }
        }

        private void FilterTruckCategories()
        {
            gridTruckCategories.SelectionChanged -= GridTruckCategories_SelectionChanged;
            gridTruckCategories.Rows.Clear();

            string q = txtCatSearch.Text.Trim().ToLowerInvariant();
            var list = string.IsNullOrEmpty(q)
                ? allTruckCategories
                : allTruckCategories.Where(c => c.CategoryName.ToLower().Contains(q)).ToList();

            foreach (var c in list)
            {
                gridTruckCategories.Rows.Add(c.CategoryName, $"{c.CrateCapacity} crates", $"{c.AssignedTrucksCount} trucks");
            }
            lblCatSummary.Text = $"📊 Total Truck Classifications: {list.Count} of {allTruckCategories.Count}";

            if (gridTruckCategories.Rows.Count > 0)
            {
                gridTruckCategories.ClearSelection();
                gridTruckCategories.Rows[0].Selected = true;
                PopulateCategoryFromRow(gridTruckCategories.Rows[0]);
            }
            else
            {
                ClearCatInputs();
            }

            gridTruckCategories.SelectionChanged += GridTruckCategories_SelectionChanged;
        }

        private void GridTruckCategories_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridTruckCategories.SelectedRows.Count > 0)
            {
                PopulateCategoryFromRow(gridTruckCategories.SelectedRows[0]);
            }
        }

        private void PopulateCategoryFromRow(DataGridViewRow row)
        {
            string catName = row.Cells[0].Value?.ToString() ?? "";
            selectedTruckCategory = allTruckCategories.FirstOrDefault(c => string.Equals(c.CategoryName, catName, StringComparison.OrdinalIgnoreCase));
            if (selectedTruckCategory != null)
            {
                txtCatName.Text = selectedTruckCategory.CategoryName;
                numCatCapacity.Value = selectedTruckCategory.CrateCapacity;
            }
        }

        private void BtnAddCat_Click(object? sender, EventArgs e)
        {
            string name = txtCatName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCatName.Focus();
                return;
            }

            int cap = (int)numCatCapacity.Value;
            bool success = MJdatabase.SaveTruckCategory(name, cap);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                QuietBox.Show($"Truck category '{name}' with {cap} crate capacity saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save truck category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateCat_Click(object? sender, EventArgs e)
        {
            if (selectedTruckCategory == null)
            {
                MessageBox.Show("Please select a category to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtCatName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCatName.Focus();
                return;
            }

            int cap = (int)numCatCapacity.Value;
            string oldName = selectedTruckCategory.CategoryName;
            bool success = MJdatabase.SaveTruckCategory(name, cap, oldName);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                QuietBox.Show($"Truck category updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update truck category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteCat_Click(object? sender, EventArgs e)
        {
            if (selectedTruckCategory == null)
            {
                MessageBox.Show("Please select a category to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = selectedTruckCategory.CategoryName;
            var confirm = MessageBox.Show($"Are you sure you want to delete category '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                MJdatabase.DeleteTruckCategory(name);
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                ClearCatInputs();
                QuietBox.Show($"Category '{name}' deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearCatInputs()
        {
            selectedTruckCategory = null;
            txtCatName.Clear();
            numCatCapacity.Value = 300;
        }
}
}
