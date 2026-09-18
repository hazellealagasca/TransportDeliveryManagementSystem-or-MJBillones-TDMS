using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class DispatchManagementView : UserControl, IRefreshable
    {
        private Delivery? selectedDelivery = null;
        private bool isBindingForm = false;
        private List<Delivery> allDispatches = new List<Delivery>();
        private string currentSortColumn = "DispatchNumber";
        private bool isSortAscending = true;

        private List<LoadingSchedule> loadingSchedules = new List<LoadingSchedule>();
        private List<Truck> trucks = new List<Truck>();
        private List<Crew> drivers = new List<Crew>();
        private List<Crew> aides = new List<Crew>();

        public DispatchManagementView()
        {
            InitializeComponent();
            gridDispatch.BringToFront();
            SetupAideModuleEvents();
            SetupGridColumns();
            SetupContextMenu();
            gridDispatch.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridDispatch.Rows[e.RowIndex].DataBoundItem is Delivery item) RecordDetailsDialog.ShowDetails(this, item); };
            gridDispatch.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridDispatch.Columns.Count) return;
                string col = gridDispatch.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            cbTruck.DrawMode = DrawMode.OwnerDrawFixed;
            cbTruck.ItemHeight = 28;
            cbTruck.DrawItem += (s, e) => DrawStatusComboBoxItem(cbTruck, e);

            cbDriver.DrawMode = DrawMode.OwnerDrawFixed;
            cbDriver.ItemHeight = 28;
            cbDriver.DrawItem += (s, e) => DrawStatusComboBoxItem(cbDriver, e);
            SetupEvents();
            LoadDropdownsAndGrid();
            UIThemeHelper.ApplyTheme(this);
        }

        public void PreSelectTruck(int truckId)
        {
            try
            {
                if (trucks == null || trucks.Count == 0)
                {
                    trucks = MJdatabase.GetTrucks();
                }
                int idx = trucks.FindIndex(t => t.Id == truckId);
                if (idx >= 0 && idx < cbTruck.Items.Count)
                {
                    cbTruck.SelectedIndex = idx;
                    CalculateCratesAndRecommendation();
                }
            }
            catch { }
        }

        public void RefreshData()
        {
            LoadDropdownsAndGrid();
        }

        private void SetupEvents()
        {
            btnPickAnalogTime.Click += (s, e) => OpenAnalogClockPicker();
            cbDepartureTime.Click += (s, e) => OpenAnalogClockPicker();

            cbLoadSchedule.SelectedIndexChanged += (s, e) =>
            {
                if (isBindingForm) return;
                if (cbLoadSchedule.SelectedIndex >= 0 && cbLoadSchedule.SelectedIndex < loadingSchedules.Count)
                {
                    var ls = loadingSchedules[cbLoadSchedule.SelectedIndex];
                    numGCrate.Value = Math.Max(0, ls.GCrate);
                    numECrate.Value = Math.Max(0, ls.ECrate);
                    if (ls.ScheduleDate >= dtpDispatchDate.MinDate && ls.ScheduleDate <= dtpDispatchDate.MaxDate)
                    {
                        dtpDispatchDate.Value = ls.ScheduleDate;
                    }
                    if (!string.IsNullOrEmpty(ls.DepartureTime))
                    {
                        cbDepartureTime.Text = ls.DepartureTime;
                    }
                    CalculateCratesAndRecommendation();
                }
            };

            numGCrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            numECrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            dtpDispatchDate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            cbTruck.SelectedIndexChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
        }

        private void OpenAnalogClockPicker()
        {
            string cur = cbDepartureTime.Text;
            if (string.IsNullOrWhiteSpace(cur)) cur = "08:00 AM";

            var dlg = new AnalogClockTimePickerDialog(cur);
            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                string sel = dlg.SelectedTimeString;
                if (!cbDepartureTime.Items.Contains(sel))
                {
                    cbDepartureTime.Items.Add(sel);
                }
                cbDepartureTime.SelectedItem = sel;
                cbDepartureTime.Text = sel;
            }
        }

        private void CalculateCratesAndRecommendation()
        {
            int g = (int)numGCrate.Value;
            int ec = (int)numECrate.Value;
            int total = g + ec;
            txtTotalCrates.Text = $"{total} Crates ({g} G-Crates + {ec} E-Crates)";

            if (total == 0)
            {
                pnlRec.BackColor = Color.FromArgb(241, 245, 249);
                lblRec.ForeColor = Color.FromArgb(71, 85, 105);
                lblRec.Text = "ℹ️ Enter crate quantity to verify vehicle load capacity.";
                return;
            }

            Truck? selTruck = (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count)
                ? trucks[cbTruck.SelectedIndex]
                : null;

            LoadingSchedule? selSchedule = (cbLoadSchedule.SelectedIndex >= 0 && cbLoadSchedule.SelectedIndex < loadingSchedules.Count)
                ? loadingSchedules[cbLoadSchedule.SelectedIndex]
                : null;

            if (selTruck != null)
            {
                string reqTemp = selSchedule != null && !string.IsNullOrEmpty(selSchedule.CargoType) ? selSchedule.CargoType : "Cold / Chilled";
                string truckTemp = !string.IsNullOrEmpty(selTruck.CargoType) ? selTruck.CargoType : "Cold / Chilled";
                bool tempMismatch = (reqTemp.Contains("Cold", StringComparison.OrdinalIgnoreCase) || reqTemp.Contains("Mix", StringComparison.OrdinalIgnoreCase)) && truckTemp.Equals("Dry Goods", StringComparison.OrdinalIgnoreCase);

                if (tempMismatch)
                {
                    pnlRec.BackColor = Color.FromArgb(254, 242, 242);
                    lblRec.ForeColor = Color.FromArgb(185, 28, 28);
                    lblRec.Text = $"⚠️ Temperature Mismatch: Order requires '{reqTemp}', but {selTruck.PlateNumber} is configured for '{truckTemp}'! Use a refrigerated vehicle.";
                }
                else if (selTruck.CapacityCrates >= total)
                {
                    double utilization = Math.Round(((double)total / selTruck.CapacityCrates) * 100.0, 1);
                    pnlRec.BackColor = Color.FromArgb(240, 253, 244);
                    lblRec.ForeColor = Color.FromArgb(22, 101, 52);
                    lblRec.Text = $"✓ Optimal Match: {selTruck.PlateNumber} ({truckTemp} - {selTruck.CapacityCrates} crates) handles {total} crates ({utilization}% utilized).";
                }
                else
                {
                    int deficit = total - selTruck.CapacityCrates;
                    pnlRec.BackColor = Color.FromArgb(254, 242, 242);
                    lblRec.ForeColor = Color.FromArgb(185, 28, 28);
                    lblRec.Text = $"⚠️ Overload Warning: Selected truck {selTruck.PlateNumber} (max {selTruck.CapacityCrates} crates) is exceeded by {deficit} crates. Choose a larger vehicle.";
                }
            }
            else
            {
                var rec = MJdatabase.GetFleetRecommendation(total, dtpDispatchDate.Value);
                lblRec.Text = rec.RecommendationMessage;

                if (rec.IsMultiTruckNeeded)
                {
                    pnlRec.BackColor = Color.FromArgb(254, 242, 242);
                    lblRec.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else
                {
                    pnlRec.BackColor = Color.FromArgb(240, 253, 244);
                    lblRec.ForeColor = Color.FromArgb(22, 101, 52);
                }
            }
        }

        private void SetupGridColumns()
        {
            gridDispatch.AutoGenerateColumns = false;
            gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridDispatch.ScrollBars = ScrollBars.Both;
            gridDispatch.AllowUserToResizeColumns = true;
            gridDispatch.ShowCellToolTips = true;
            gridDispatch.Columns.Clear();
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 115, MinimumWidth = 135, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoadingScheduleInfo", HeaderText = "Order / Client", FillWeight = 145, MinimumWidth = 150, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckInfo", HeaderText = "Vehicle", FillWeight = 105, MinimumWidth = 115, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DriverName", HeaderText = "Driver", FillWeight = 105, MinimumWidth = 115, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AideNames", HeaderText = "Aide(s)", FillWeight = 115, MinimumWidth = 120, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GCrate", HeaderText = "G-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ECrate", HeaderText = "E-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Total", FillWeight = 70, MinimumWidth = 75, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Departure", FillWeight = 85, MinimumWidth = 95, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
        }

        private async void LoadDropdownsAndGrid()
        {
            try
            {
                loadingSchedules = await MJdatabase.GetLoadingSchedulesAsync();
                cbLoadSchedule.Items.Clear();
                foreach (var ls in loadingSchedules)
                {
                    cbLoadSchedule.Items.Add($"{ls.ScheduleNumber} - {ls.ClientName} ({ls.TotalCrates} Crates)");
                }
                cbLoadSchedule.SelectedIndex = -1;

                trucks = await MJdatabase.GetTrucksAsync();
                cbTruck.Items.Clear();
                foreach (var t in trucks)
                {
                    string tBadge = t.Status == "Available" ? "[AVAILABLE]" : (t.Status == "In Use" || t.Status == "On Trip" ? "[ON TRIP]" : "[MAINTENANCE]");
                    cbTruck.Items.Add($"{tBadge} {t.PlateNumber} [{t.TruckType}]");
                }
                cbTruck.SelectedIndex = -1;

                var crews = await MJdatabase.GetCrewsAsync();
                drivers = crews.FindAll(c => c.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0 && c.IsActive);
                cbDriver.Items.Clear();
                foreach (var d in drivers)
                {
                    string dBadge = d.IsAvailable ? "[AVAILABLE]" : "[ON TRIP]";
                    cbDriver.Items.Add($"{dBadge} {d.FullName}");
                }
                cbDriver.SelectedIndex = -1;

                aides = crews.FindAll(c => (c.Role.IndexOf("Aide", StringComparison.OrdinalIgnoreCase) >= 0 || c.Role.IndexOf("Helper", StringComparison.OrdinalIgnoreCase) >= 0 || c.Role.IndexOf("Crew", StringComparison.OrdinalIgnoreCase) >= 0) && c.IsActive);
                if (aides.Count == 0)
                {
                    lblAi.Text = "Assigned Helper/Aides: (No Aides registered yet - add in Crew Management)";
                    lblAi.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else
                {
                    lblAi.Text = "Assigned Helper/Aides (Select 1 or more):";
                    lblAi.ForeColor = Color.FromArgb(30, 41, 59);
                }
                RenderAidesModule();

                allDispatches = await MJdatabase.GetDeliveriesAsync();
                FilterGridData();
                var deliveries = allDispatches;

                int pendingCount = deliveries.Count(d => d.Status == "Pending");
                int transitCount = deliveries.Count(d => d.Status == "In Transit");
                int completedCount = deliveries.Count(d => d.Status == "Completed");
                int totalCrates = deliveries.Sum(d => d.TotalCrates);

                lblSummary.Text = $"📊 Total Dispatches: {deliveries.Count:N0}  |  ⏳ Pending: {pendingCount}  |  🚚 In Transit: {transitCount}  |  ✅ Completed: {completedCount}  |  📦 Total Volume: {totalCrates:N0} Crates";

                CalculateCratesAndRecommendation();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dispatch data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

                        private void GridDispatch_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridDispatch.SelectedRows.Count > 0 && gridDispatch.SelectedRows[0].DataBoundItem is Delivery d)
            {
                isBindingForm = true;
                selectedDelivery = d;

                int schIdx = loadingSchedules.FindIndex(s => s.Id == selectedDelivery.LoadingScheduleId);
                if (schIdx >= 0 && schIdx < cbLoadSchedule.Items.Count) cbLoadSchedule.SelectedIndex = schIdx;

                int trkIdx = trucks.FindIndex(t => t.Id == selectedDelivery.TruckId);
                if (trkIdx >= 0 && trkIdx < cbTruck.Items.Count) cbTruck.SelectedIndex = trkIdx;

                int drvIdx = drivers.FindIndex(drv => drv.Id == selectedDelivery.DriverId);
                if (drvIdx >= 0 && drvIdx < cbDriver.Items.Count) cbDriver.SelectedIndex = drvIdx;

                selectedAideIds.Clear();
                if (!string.IsNullOrEmpty(selectedDelivery.AideIds))
                {
                    var idTokens = selectedDelivery.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    foreach (var idStr in idTokens)
                    {
                        if (int.TryParse(idStr, out int aId))
                            selectedAideIds.Add(aId);
                    }
                }
                RenderAidesModule();

                if (selectedDelivery.DispatchDate >= dtpDispatchDate.MinDate && selectedDelivery.DispatchDate <= dtpDispatchDate.MaxDate)
                {
                    dtpDispatchDate.Value = selectedDelivery.DispatchDate;
                }
                cbDepartureTime.Text = string.IsNullOrEmpty(selectedDelivery.DepartureTime) ? "08:00 AM" : selectedDelivery.DepartureTime;
                cbStatus.Text = string.IsNullOrEmpty(selectedDelivery.Status) ? "Pending" : selectedDelivery.Status;
                numGCrate.Value = Math.Max(0, selectedDelivery.GCrate);
                numECrate.Value = Math.Max(0, selectedDelivery.ECrate);
                CalculateCratesAndRecommendation();
                lblFormHeader.Text = "Delivery Dispatch Order Form";
                isBindingForm = false;
            }
        }

        private (List<int> Ids, List<string> Names) GetSelectedAides()
        {
            var sel = aides.Where(a => selectedAideIds.Contains(a.Id)).ToList();
            var ids = sel.Select(a => a.Id).ToList();
            var names = sel.Select(a => a.FullName).ToList();
            return (ids, names);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbLoadSchedule.SelectedIndex < 0 || cbTruck.SelectedIndex < 0 || cbDriver.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a Loading Schedule, Truck Vehicle, and Driver.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selLs = loadingSchedules[cbLoadSchedule.SelectedIndex];
                var selTr = trucks[cbTruck.SelectedIndex];
                var selDr = drivers[cbDriver.SelectedIndex];
                var (selectedAideIds, selectedAideNames) = GetSelectedAides();

                int primaryAideId = selectedAideIds.Count > 0 ? selectedAideIds[0] : 0;
                string primaryAideName = selectedAideNames.Count > 0 ? selectedAideNames[0] : "None";

                var existingDeliveries = MJdatabase.GetDeliveries();
                var duplicateDispatch = existingDeliveries.Find(d => (selectedDelivery == null || d.Id != selectedDelivery.Id) && d.LoadingScheduleId == selLs.Id && d.Status != "Cancelled");
                if (duplicateDispatch != null)
                {
                    bool allowMultiDest = MJdatabase.GetSystemSetting("AllowMultiDestDispatch", "True").Equals("True", StringComparison.OrdinalIgnoreCase);
                    if (!allowMultiDest)
                    {
                        MessageBox.Show($"DUPLICATE DISPATCH BLOCKED:\nLoading Schedule #{selLs.ScheduleNumber} is already assigned to active Dispatch #{duplicateDispatch.DispatchNumber} (Status: {duplicateDispatch.Status}). Re-dispatching an assigned schedule is strictly prohibited.", "Duplicate Dispatch Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    else
                    {
                        var confirmCombined = MessageBox.Show($"MULTI-DESTINATION TRIP COMBINATION:\nLoading Schedule #{selLs.ScheduleNumber} is already assigned to Dispatch #{duplicateDispatch.DispatchNumber}.\n\nDo you want to proceed with combining this destination order into this trip under Multi-Destination Dispatch routing?", "Combined Destination Trip", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirmCombined != DialogResult.Yes) return;
                    }
                }

                if (!selDr.IsAvailable)
                {
                    MessageBox.Show($"DRIVER ON TRIP:\nDriver '{selDr.FullName}' is currently marked as 'On Trip' and cannot be dispatched.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                if (!selTr.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"TRUCK UNAVAILABLE:\nVehicle '{selTr.PlateNumber}' is currently marked as '{selTr.Status}' and cannot be dispatched.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                foreach (int aideId in selectedAideIds)
                {
                    var aide = aides.Find(a => a.Id == aideId);
                    if (aide != null && !aide.IsAvailable)
                    {
                        MessageBox.Show($"AIDE/HELPER ON TRIP:\nHelper '{aide.FullName}' is currently marked as 'On Trip' and cannot be dispatched.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }

                string? conflict = MJdatabase.ValidateDispatchConflict(0, selTr.Id, selDr.Id, selectedAideIds, dtpDispatchDate.Value);
                if (!string.IsNullOrEmpty(conflict))
                {
                    MessageBox.Show(conflict, "Scheduling & Delivery Conflict", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                int total = g + ec;
                if (total == 0)
                {
                    MessageBox.Show("Please enter at least 1 crate for dispatch.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numGCrate.Focus();
                    return;
                }

                string status = cbStatus.SelectedItem?.ToString() ?? "Pending";
                string cancellationReason = "";
                if (status == "Cancelled")
                {
                    string? reason = CancellationReasonDialog.PromptCancellationReason(this.FindForm() ?? this.ParentForm!, "Dispatch Order");
                    if (string.IsNullOrEmpty(reason)) return;
                    cancellationReason = reason;
                }

                Delivery d = new Delivery
                {
                    LoadingScheduleId = selLs.Id,
                    LoadingScheduleInfo = $"{selLs.ClientName} ({selLs.Destination})",
                    TruckId = selTr.Id,
                    TruckInfo = $"{selTr.PlateNumber} ({selTr.TruckType})",
                    DriverId = selDr.Id,
                    DriverName = selDr.FullName,
                    AideId = primaryAideId,
                    AideName = primaryAideName,
                    AideIds = string.Join(",", selectedAideIds),
                    AideNames = selectedAideNames.Count > 0 ? string.Join(", ", selectedAideNames) : "None",
                    GCrate = g,
                    ECrate = ec,
                    TotalCrates = total,
                    DispatchDate = dtpDispatchDate.Value,
                    DepartureTime = cbDepartureTime.Text.Trim(),
                    Status = status,
                    Remarks = "Dispatched Delivery Order",
                    CancellationReason = cancellationReason
                };

                MJdatabase.SaveDelivery(d);
                MessageBox.Show("Dispatch Order created and crew assigned successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadDropdownsAndGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedDelivery == null)
                {
                    MessageBox.Show("Please select a dispatch order to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var (selectedAideIds, selectedAideNames) = GetSelectedAides();
                int primaryAideId = selectedAideIds.Count > 0 ? selectedAideIds[0] : 0;
                string primaryAideName = selectedAideNames.Count > 0 ? selectedAideNames[0] : "None";

                string? conflict = MJdatabase.ValidateDispatchConflict(selectedDelivery.Id, selectedDelivery.TruckId, selectedDelivery.DriverId, selectedAideIds, dtpDispatchDate.Value);
                if (!string.IsNullOrEmpty(conflict))
                {
                    MessageBox.Show(conflict, "Scheduling & Delivery Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string newStatus = cbStatus.SelectedItem?.ToString() ?? "Pending";
                string cancellationReason = selectedDelivery.CancellationReason;

                if (newStatus == "Cancelled" && selectedDelivery.Status != "Cancelled")
                {
                    string? reason = CancellationReasonDialog.PromptCancellationReason(this.FindForm() ?? this.ParentForm!, "Dispatch Order", selectedDelivery.DispatchNumber);
                    if (string.IsNullOrEmpty(reason)) return;
                    cancellationReason = reason;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                int total = g + ec;

                selectedDelivery.DispatchDate = dtpDispatchDate.Value;
                selectedDelivery.DepartureTime = cbDepartureTime.Text.Trim();
                selectedDelivery.Status = newStatus;
                selectedDelivery.GCrate = g;
                selectedDelivery.ECrate = ec;
                selectedDelivery.TotalCrates = total;
                selectedDelivery.AideId = primaryAideId;
                selectedDelivery.AideName = primaryAideName;
                selectedDelivery.AideIds = string.Join(",", selectedAideIds);
                selectedDelivery.AideNames = selectedAideNames.Count > 0 ? string.Join(", ", selectedAideNames) : "None";
                selectedDelivery.CancellationReason = cancellationReason;

                MJdatabase.SaveDelivery(selectedDelivery);
                MessageBox.Show("Dispatch Order updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadDropdownsAndGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedDelivery == null)
                {
                    MessageBox.Show("Please select a dispatch order to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Deliveries", selectedDelivery.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Dispatch Order {selectedDelivery.DispatchNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Deliveries", selectedDelivery.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Dispatch Order deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadDropdownsAndGrid();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete dispatch order. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ? allDispatches
                : allDispatches.Where(d =>
                    (!string.IsNullOrEmpty(d.DispatchNumber) && d.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.LoadingScheduleInfo) && d.LoadingScheduleInfo.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.TruckInfo) && d.TruckInfo.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.DriverName) && d.DriverName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.AideNames) && d.AideNames.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.CargoType) && d.CargoType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.Status) && d.Status.ToLower().Contains(q))
                ).ToList();

            gridDispatch.DataSource = null;
            gridDispatch.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Delivery Dispatches & Trips ({allDispatches.Count})"
                : $"Delivery Dispatches & Trips - Showing {filtered.Count} of {allDispatches.Count}";

            if (filtered.Count > 0)
            {
                gridDispatch.ClearSelection();
                gridDispatch.Rows[0].Selected = true;
            }
            else
            {
                isBindingForm = true;
                selectedDelivery = null;
                numGCrate.Value = 0;
                numECrate.Value = 0;
                txtTotalCrates.Text = "";
                lblRec.Text = "No matching dispatch orders found.";
                lblFormHeader.Text = "Delivery Dispatch Order Form";
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

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintHelper.PrintGridReport("Delivery Dispatch Manifest Report", gridDispatch, "Official transport dispatch order register with complete vehicle, crew, and crate details.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            selectedDelivery = null;
            gridDispatch.ClearSelection();
            cbLoadSchedule.SelectedIndex = -1;
            cbTruck.SelectedIndex = -1;
            cbDriver.SelectedIndex = -1;
            dtpDispatchDate.Value = DateTime.Now;
            cbDepartureTime.Text = "08:00 AM";
            if (cbStatus.Items.Count > 0) cbStatus.SelectedIndex = 0;
            numGCrate.Value = 150;
            numECrate.Value = 0;
            selectedAideIds.Clear();
            txtSearchAides.Clear();
            RenderAidesModule();

            btnSave.Enabled = true;
            btnUpdate.Enabled = true;
            cbLoadSchedule.Enabled = true;
            cbTruck.Enabled = true;
            cbDriver.Enabled = true;
            flowAidesModule.Enabled = true;
            txtSearchAides.Enabled = true;

            btnClearAides.Enabled = true;
            numGCrate.Enabled = true;
            numECrate.Enabled = true;
            dtpDispatchDate.Enabled = true;
            cbDepartureTime.Enabled = true;
            btnPickAnalogTime.Enabled = true;

            lblRec.Text = "💡 Select a loading order, truck vehicle, and driver to view capacity match.";
            pnlRec.BackColor = Color.FromArgb(248, 250, 252);
            lblRec.ForeColor = Color.FromArgb(71, 85, 105);
        }

        private HashSet<int> selectedAideIds = new HashSet<int>();

        private void SetupAideModuleEvents()
        {
            txtSearchAides.TextChanged += (s, e) => RenderAidesModule();
            btnClearAides.Click += (s, e) =>
            {
                selectedAideIds.Clear();
                txtSearchAides.Clear();
                RenderAidesModule();
            };
        }

        private void RenderAidesModule()
        {
            flowAidesModule.SuspendLayout();
            flowAidesModule.Controls.Clear();
            string q = txtSearchAides.Text.Trim().ToLowerInvariant();

            var visibleAides = aides.Where(a =>
                string.IsNullOrEmpty(q) ||
                a.FullName.ToLowerInvariant().Contains(q)
            ).ToList();

            foreach (var a in visibleAides)
            {
                bool isSelected = selectedAideIds.Contains(a.Id);
                string prefix = isSelected ? "✓ " : (a.IsAvailable ? "+ 🟢 " : "+ 🟠 ");
                string suffix = (!isSelected && !a.IsAvailable) ? " (On Trip)" : "";

                Button pill = new Button
                {
                    Text = prefix + a.FullName + suffix,
                    AutoSize = true,
                    Height = 28,
                    Margin = new Padding(3),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 8.5F, isSelected ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand
                };

                if (isSelected)
                {
                    pill.BackColor = Color.FromArgb(15, 23, 42);
                    pill.ForeColor = Color.White;
                    pill.FlatAppearance.BorderSize = 0;
                }
                else if (a.IsAvailable)
                {
                    pill.BackColor = Color.FromArgb(240, 253, 244);
                    pill.ForeColor = Color.FromArgb(22, 101, 52);
                    pill.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
                }
                else
                {
                    pill.BackColor = Color.FromArgb(254, 243, 199);
                    pill.ForeColor = Color.FromArgb(180, 83, 9);
                    pill.FlatAppearance.BorderColor = Color.FromArgb(253, 230, 138);
                }

                pill.Click += (s, e) =>
                {
                    if (selectedAideIds.Contains(a.Id))
                        selectedAideIds.Remove(a.Id);
                    else
                        selectedAideIds.Add(a.Id);

                    RenderAidesModule();
                };

                flowAidesModule.Controls.Add(pill);
            }

            flowAidesModule.ResumeLayout();
            UpdateAidesSummary();
        }

        private void UpdateAidesSummary()
        {
            var selNames = aides.Where(a => selectedAideIds.Contains(a.Id)).Select(a => a.FullName).ToList();
            if (selNames.Count == 0)
            {
                lblAidesSummary.Text = "✓ 0 Aides Selected (Click pills above to select)";
                lblAidesSummary.ForeColor = Color.FromArgb(100, 116, 139);
            }
            else
            {
                lblAidesSummary.Text = $"✓ {selNames.Count} Aide(s) Selected: {string.Join(", ", selNames)}";
                lblAidesSummary.ForeColor = Color.FromArgb(22, 101, 52);
            }
        }

        private void DrawStatusComboBoxItem(ComboBox cb, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= cb.Items.Count) return;

            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            string text = cb.Items[e.Index]?.ToString() ?? "";

            using var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool isAvail = text.Contains("[AVAILABLE]", StringComparison.OrdinalIgnoreCase);
            bool isOnTrip = text.Contains("[ON TRIP]", StringComparison.OrdinalIgnoreCase);

            Color dotColor = isAvail ? Color.FromArgb(16, 185, 129) : (isOnTrip ? Color.FromArgb(249, 115, 22) : Color.FromArgb(239, 68, 68));
            Color badgeBg = isAvail ? Color.FromArgb(220, 252, 231) : (isOnTrip ? Color.FromArgb(254, 243, 199) : Color.FromArgb(254, 226, 226));
            Color badgeFg = isAvail ? Color.FromArgb(22, 101, 52) : (isOnTrip ? Color.FromArgb(180, 83, 9) : Color.FromArgb(185, 28, 28));

            int dotSize = 8;
            int dotY = e.Bounds.Y + (e.Bounds.Height - dotSize) / 2;
            using (var brush = new SolidBrush(dotColor))
            {
                g.FillEllipse(brush, e.Bounds.X + 8, dotY, dotSize, dotSize);
            }

            string statusTag = isAvail ? "AVAILABLE" : (isOnTrip ? "ON TRIP" : "BUSY");
            Rectangle tagRect = new Rectangle(e.Bounds.X + 22, e.Bounds.Y + 4, 75, e.Bounds.Height - 8);
            using (var tagBrush = new SolidBrush(isSelected ? Color.White : badgeBg))
            {
                g.FillRectangle(tagBrush, tagRect);
            }
            TextRenderer.DrawText(g, statusTag, new Font("Segoe UI", 7.5F, FontStyle.Bold), tagRect, isSelected ? Color.FromArgb(15, 23, 42) : badgeFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            string cleanText = text.Replace("[AVAILABLE]", "").Replace("[ON TRIP]", "").Replace("[MAINTENANCE]", "").Trim();

            Color textColor = isSelected ? Color.White : Color.FromArgb(30, 41, 59);
            Rectangle textRect = new Rectangle(e.Bounds.X + 104, e.Bounds.Y, e.Bounds.Width - 108, e.Bounds.Height);
            TextRenderer.DrawText(g, cleanText, new Font("Segoe UI Semibold", 9F, FontStyle.Bold), textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Delivery>? OnRecordTripRequested { get; set; }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Delivery>? OnCreateInvoiceRequested { get; set; }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedDelivery != null) RecordDetailsDialog.ShowDetails(this, selectedDelivery);
            });
            var itemCopyDsp = new ToolStripMenuItem("📋 Copy Dispatch #", null, (s, e) => {
                if (selectedDelivery != null) { Clipboard.SetText(selectedDelivery.DispatchNumber); ToastHelper.ShowToast(this, "Dispatch # copied ✓"); }
            });
            var itemTrip = new ToolStripMenuItem("📝 Record Trip Report / Completion", null, (s, e) => {
                if (selectedDelivery != null) OnRecordTripRequested?.Invoke(selectedDelivery);
            });
            var itemInvoice = new ToolStripMenuItem("💰 Issue Billing Invoice", null, (s, e) => {
                if (selectedDelivery != null) OnCreateInvoiceRequested?.Invoke(selectedDelivery);
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Dispatch Manifest", null, BtnPrint_Click);
            var itemCancel = new ToolStripMenuItem("❌ Cancel Dispatch Order", null, (s, e) => {
                try
                {
                    if (selectedDelivery != null) {
                        selectedDelivery.Status = "Cancelled";
                        MJdatabase.SaveDelivery(selectedDelivery);
                        LoadDropdownsAndGrid();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to cancel dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyDsp, new ToolStripSeparator(), itemTrip, itemInvoice, new ToolStripSeparator(), itemPrint, itemCancel });

            gridDispatch.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridDispatch.ClearSelection();
                    gridDispatch.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridDispatch, gridDispatch.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
