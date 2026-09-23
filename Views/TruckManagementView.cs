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
        private Truck? selectedTruck = null;
        private bool isBindingForm = false;
        private List<Truck> allTrucks = new List<Truck>();
        private string currentSortColumn = "PlateNumber";
        private bool isSortAscending = true;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
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
            gridTrucks.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridTrucks.Rows[e.RowIndex].DataBoundItem is Truck item) RecordDetailsDialog.ShowDetails(this, item); };
            SetupEvents();
            LoadTrucks();
            MJdatabase.OnCategoriesChanged += () => { if (!this.IsDisposed && this.IsHandleCreated) this.Invoke(new Action(LoadTrucks)); };

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            txtPlate.Leave += (s, e) => txtPlate.Text = txtPlate.Text.Trim().ToUpper();
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 Input Form ▾",
                Size = new Size(130, 30),
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
                Text = "✖ Hide Form",
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 8)
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlForm.Controls.Add(btnCloseForm);
            pnlForm.Controls.SetChildIndex(btnCloseForm, 0);

            SetFormVisibility(false);
        }

        private void SetFormVisibility(bool visible)
        {
            pnlForm.Visible = visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].Width = visible ? 580F : 0F;
                tblMasterContent.ColumnStyles[0].SizeType = SizeType.Absolute;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].Width = 100F;
                tblMasterContent.ColumnStyles[1].SizeType = SizeType.Percent;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "📝 Hide Form ▴" : "📝 Input Form ▾";
                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;
            }
        }

        public void RefreshData()
        {
            LoadTrucks();
        }

        private void SetupGridColumns()
        {
            gridTrucks.AutoGenerateColumns = false;
            gridTrucks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridTrucks.ScrollBars = ScrollBars.Both;
            gridTrucks.AllowUserToResizeColumns = true;
            gridTrucks.ShowCellToolTips = true;
            gridTrucks.Columns.Clear();
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PlateNumber", HeaderText = "Plate Number", FillWeight = 14F, MinimumWidth = 110, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckType", HeaderText = "Truck Category", FillWeight = 18F, MinimumWidth = 125, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CargoType", HeaderText = "Cargo / Temp Type", FillWeight = 16F, MinimumWidth = 120, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CapacityCrates", HeaderText = "Capacity", FillWeight = 12F, MinimumWidth = 85, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 12F, MinimumWidth = 85, ReadOnly = true });
            gridTrucks.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Remarks", HeaderText = "Condition Remarks", FillWeight = 28F, MinimumWidth = 135, ReadOnly = true });
        }

                private void GridTrucks_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridTrucks.SelectedRows.Count > 0 && gridTrucks.SelectedRows[0].DataBoundItem is Truck t)
            {
                isBindingForm = true;
                selectedTruck = t;
                txtPlate.Text = selectedTruck.PlateNumber;
                cbType.SelectedItem = selectedTruck.TruckType;
                cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedTruck.CargoType) ? "Cold / Chilled" : selectedTruck.CargoType;
                numCapacity.Value = selectedTruck.CapacityCrates;
                string curStatus = selectedTruck.Status;
                if (curStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)) curStatus = "Active";
                cbStatus.SelectedItem = curStatus;
                txtRemarks.Text = selectedTruck.Remarks;
                lblFormHeader.Text = "Truck Vehicle Details";
                isBindingForm = false;
            }
        }

                private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
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

                string truckType = cbType.Text.Trim();
                if (string.IsNullOrWhiteSpace(truckType))
                {
                    MessageBox.Show("Please select a Truck Category from the dropdown. (You can add or edit Truck Categories and their Capacities via the Categories menu on the top bar).", "Truck Type Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbType.Focus();
                    return;
                }
                MJdatabase.SaveLookupItem("Truck Type / Category", truckType);

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
                MessageBox.Show($"Truck '{plateNumber}' ({truckType} - {capacity} crates) added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadTrucks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Truck Management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

                private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
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

                string truckType = cbType.Text.Trim();
                if (string.IsNullOrWhiteSpace(truckType))
                {
                    MessageBox.Show("Please select a Truck Category from the dropdown. (You can add or edit Truck Categories and their Capacities via the Categories menu on the top bar).", "Truck Type Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbType.Focus();
                    return;
                }
                MJdatabase.SaveLookupItem("Truck Type / Category", truckType);

                selectedTruck.PlateNumber = plateNumber;
                selectedTruck.TruckType = truckType;
                selectedTruck.CapacityCrates = capacity;
                selectedTruck.Status = cbStatus.SelectedItem?.ToString() ?? "Active";
                selectedTruck.Remarks = txtRemarks.Text.Trim();
                selectedTruck.CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled";

                MJdatabase.SaveTruck(selectedTruck);
                MessageBox.Show($"Truck '{plateNumber}' updated successfully! New capacity: {capacity} crates.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadTrucks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Truck Management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
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

                var res = MessageBox.Show($"Are you sure you want to delete truck {selectedTruck.PlateNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Trucks", selectedTruck.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Truck record deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            {
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
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Registered Trucks ({allTrucks.Count})"
                : $"Registered Trucks - Showing {filtered.Count} of {allTrucks.Count}";

            if (filtered.Count > 0)
            {
                gridTrucks.ClearSelection();
                gridTrucks.Rows[0].Selected = true;
                isBindingForm = true;
                selectedTruck = filtered[0];
                txtPlate.Text = selectedTruck.PlateNumber;
                cbType.SelectedItem = selectedTruck.TruckType;
                cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedTruck.CargoType) ? "Cold / Chilled" : selectedTruck.CargoType;
                numCapacity.Value = selectedTruck.CapacityCrates;
                cbStatus.SelectedItem = selectedTruck.Status;
                txtRemarks.Text = selectedTruck.Remarks;
                lblFormHeader.Text = "Truck Vehicle Details";
                isBindingForm = false;
            }
            else
            {
                isBindingForm = true;
                selectedTruck = null;
                txtPlate.Text = "";
                txtRemarks.Text = "";
                lblFormHeader.Text = "Truck Vehicle Details";
                isBindingForm = false;
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
            {
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            selectedTruck = null;
            txtPlate.Text = "";
            cbType.Text = "";
            numCapacity.Value = 300;
            cbStatus.SelectedIndex = 0;
            cbCargoType.SelectedIndex = 0;
            txtRemarks.Text = "";
        }

        private void LoadTrucks()
        {
            try
            {
                var types = MJdatabase.GetLookupItems("Truck Type / Category");
                string currentType = cbType.Text;
                cbType.Items.Clear();
                foreach (var t in types) cbType.Items.Add(t);
                if (cbType.Items.Count > 0)
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

                allTrucks = MJdatabase.GetTrucks();
                FilterGridData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load truck data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDispatchTruck_Click(object? sender, EventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Please select a vehicle from the trucks table first.", "Select Vehicle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OnDispatchTruckRequested?.Invoke(selectedTruck);
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedTruck != null) RecordDetailsDialog.ShowDetails(this, selectedTruck);
            });
            var itemCopyPlate = new ToolStripMenuItem("📋 Copy Plate Number", null, (s, e) => {
                if (selectedTruck != null) { Clipboard.SetText(selectedTruck.PlateNumber); ToastHelper.ShowToast(this, "Plate number copied ✓"); }
            });
            var itemDispatch = new ToolStripMenuItem("🚚 Dispatch This Vehicle", null, (s, e) => {
                if (selectedTruck != null) OnDispatchTruckRequested?.Invoke(selectedTruck);
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Vehicle Details", null, (s, e) => {
                SetFormVisibility(true);
                txtPlate.Focus();
            });
            var itemAvail = new ToolStripMenuItem("🟢 Set Status: Active", null, (s, e) => {
                try
                {
                    if (selectedTruck != null) {
                        selectedTruck.Status = "Active";
                        MJdatabase.SaveTruck(selectedTruck);
                        LoadTrucks();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update truck status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var itemInactive = new ToolStripMenuItem("⚪ Set Status: Inactive", null, (s, e) => {
                try
                {
                    if (selectedTruck != null) {
                        selectedTruck.Status = "Inactive";
                        MJdatabase.SaveTruck(selectedTruck);
                        LoadTrucks();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update truck status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var itemMaint = new ToolStripMenuItem("🔧 Set Status: Under Maintenance", null, (s, e) => {
                try
                {
                    if (selectedTruck != null) {
                        selectedTruck.Status = "Under Maintenance";
                        MJdatabase.SaveTruck(selectedTruck);
                        LoadTrucks();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update truck status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var itemDelete = new ToolStripMenuItem("🗑️ Delete Vehicle Record", null, BtnDelete_Click);
            var itemExportCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) => {
                DataExportHelper.ExportToCsv(gridTrucks, "Truck_Vehicles", "Registered Trucks");
            });
            var itemExportExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) => {
                DataExportHelper.ExportToExcelXml(gridTrucks, "Truck_Vehicles", "Registered Trucks");
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyPlate, new ToolStripSeparator(), itemDispatch, new ToolStripSeparator(), itemEdit, itemAvail, itemInactive, itemMaint, new ToolStripSeparator(), itemExportCsv, itemExportExcel, new ToolStripSeparator(), itemDelete });

            gridTrucks.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridTrucks.ClearSelection();
                    gridTrucks.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridTrucks, gridTrucks.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
