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
    public partial class LoadingManagementView : UserControl, IRefreshable
    {
        private LoadingSchedule? selectedSchedule = null;
        private bool isBindingForm = false;
        private List<LoadingSchedule> allSchedules = new List<LoadingSchedule>();
        private string currentSortColumn = "ScheduleNumber";
        private bool isSortAscending = true;

        public LoadingManagementView()
        {
            InitializeComponent();
            gridSchedules.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridSchedules.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridSchedules.Rows[e.RowIndex].DataBoundItem is LoadingSchedule item) RecordDetailsDialog.ShowDetails(this, item); };
            gridSchedules.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridSchedules.Columns.Count) return;
                string col = gridSchedules.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            gridSchedules.CurrentCellDirtyStateChanged += (s, e) => { if (gridSchedules.IsCurrentCellDirty && gridSchedules.CurrentCell is DataGridViewCheckBoxCell) gridSchedules.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridSchedules.CellValueChanged += (s, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridSchedules.Rows[e.RowIndex].DataBoundItem is LoadingSchedule ls) {
                    MJdatabase.ToggleLoadingSchedulePeak(ls.Id, ls.IsPeak);
                    if (selectedSchedule != null && selectedSchedule.Id == ls.Id) {
                        chkIsPeak.Checked = ls.IsPeak;
                        CalculateCratesAndRecommendation();
                    }
                }
            };
            gridSchedules.ColumnHeaderMouseClick += (s, e) =>
            {
                if (gridSchedules.DataSource is List<LoadingSchedule> list && list.Count > 0)
                {
                    string colName = gridSchedules.Columns[e.ColumnIndex].DataPropertyName;
                    List<LoadingSchedule> sorted = colName switch
                    {
                        "ScheduleDate" => list.OrderBy(x => x.ScheduleDate).ToList(),
                        "ClientName" => list.OrderBy(x => x.ClientName).ToList(),
                        "Destination" => list.OrderBy(x => x.Destination).ToList(),
                        "TotalCrates" => list.OrderBy(x => x.TotalCrates).ToList(),
                        "Status" => list.OrderBy(x => x.Status).ToList(),
                        _ => list.OrderBy(x => x.ScheduleNumber).ToList()
                    };
                    gridSchedules.DataSource = sorted;
                }
            };
            SetupEvents();
            LoadSchedules();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadSchedules();
        }

        private void SetupEvents()
        {
            numGCrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            numECrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            dtpScheduleDate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            cbClient.TextChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            btnTimePicker.Click += (s, e) =>
            {
                string? pickedTime = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtDepartureTime.Text);
                if (!string.IsNullOrEmpty(pickedTime))
                {
                    txtDepartureTime.Text = pickedTime;
                }
            };
        }

        private void UpdateClientContactHint()
        {
            string cname = cbClient.Text.Trim();
            if (!string.IsNullOrWhiteSpace(cname))
            {
                var cl = MJdatabase.GetClientByCompanyName(cname);
                if (cl != null && !string.IsNullOrEmpty(cl.ContactNumber))
                {
                    lblClient.Text = $"Client: (📞 {cl.ContactPerson} {cl.ContactNumber})";
                    return;
                }
            }
            lblClient.Text = "Client Name:";
        }

        private void CalculateCratesAndRecommendation()
        {
            int g = (int)numGCrate.Value;
            int ec = (int)numECrate.Value;
            int total = g + ec;
            txtTotalCrates.Text = $"{total} Crates ({g} G-Crates + {ec} E-Crates)";

            var rec = MJdatabase.GetFleetRecommendation(total, dtpScheduleDate.Value);
            string dest = cbDestination.Text.Trim();
            var matchedRate = MJdatabase.FindMatchingRate(dest, chkIsPeak.Checked);
            string rateNotice = matchedRate != null
                ? $"  |  🏷️ Tariff: PHP {matchedRate.RateAmount:N2} ({(matchedRate.IsPeakSeason ? "Peak" : "Regular")})"
                : $"  |  🏷️ Est. Tariff: PHP {total * 45.00m:N2}";
            lblRec.Text = rec.RecommendationMessage + rateNotice;
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

        private void SetupGridColumns()
        {
            gridSchedules.ReadOnly = false;
            gridSchedules.EditMode = DataGridViewEditMode.EditOnEnter;
            gridSchedules.AutoGenerateColumns = false;
            gridSchedules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridSchedules.ScrollBars = ScrollBars.Both;
            gridSchedules.AllowUserToResizeColumns = true;
            gridSchedules.ShowCellToolTips = true;
            gridSchedules.Columns.Clear();
            gridSchedules.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsPeak", HeaderText = "Peak", FillWeight = 45, MinimumWidth = 60, ReadOnly = false });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ScheduleNumber", HeaderText = "Schedule #", FillWeight = 115, MinimumWidth = 145, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client Name", FillWeight = 135, MinimumWidth = 150, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Destination", HeaderText = "Destination", FillWeight = 120, MinimumWidth = 140, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CargoType", HeaderText = "Cargo Type", FillWeight = 95, MinimumWidth = 110, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ScheduleDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Departure", FillWeight = 85, MinimumWidth = 95, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GCrate", HeaderText = "G-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ECrate", HeaderText = "E-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Total", FillWeight = 70, MinimumWidth = 75, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
        }

                private void GridSchedules_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule ls)
            {
                isBindingForm = true;
                selectedSchedule = ls;
                cbClient.Text = selectedSchedule.ClientName;
                cbDestination.Text = selectedSchedule.Destination;
                if (selectedSchedule.ScheduleDate >= dtpScheduleDate.MinDate && selectedSchedule.ScheduleDate <= dtpScheduleDate.MaxDate)
                {
                    dtpScheduleDate.Value = selectedSchedule.ScheduleDate;
                }
                txtDepartureTime.Text = string.IsNullOrEmpty(selectedSchedule.DepartureTime) ? "08:00 AM" : selectedSchedule.DepartureTime;
                numGCrate.Value = Math.Max(0, selectedSchedule.GCrate);
                numECrate.Value = Math.Max(0, selectedSchedule.ECrate);
                chkIsPeak.Checked = selectedSchedule.IsPeak;
                cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedSchedule.CargoType) ? "Cold / Chilled" : selectedSchedule.CargoType;
                CalculateCratesAndRecommendation();
                lblFormHeader.Text = "Loading Order Form";
                isBindingForm = false;
            }
        }

        private async void LoadSchedules()
        {
            try
            {
                var clients = MJdatabase.GetLookupItems("Client Company");
                cbClient.Items.Clear();
                foreach (var c in clients) cbClient.Items.Add(c);

                var destinations = MJdatabase.GetLookupItems("Destination Route");
                cbDestination.Items.Clear();
                foreach (var d in destinations) cbDestination.Items.Add(d);

                allSchedules = await MJdatabase.GetLoadingSchedulesAsync();
                FilterGridData();
                var schedules = allSchedules;
                CalculateCratesAndRecommendation();

                int pendingCount = schedules.Count(s => s.Status == "Pending");
                int transitCount = schedules.Count(s => s.Status == "In Transit");
                int completedCount = schedules.Count(s => s.Status == "Completed");
                int totalCratesSum = schedules.Sum(s => s.TotalCrates);

                lblSummary.Text = $"📊 Total Schedules: {schedules.Count:N0}  |  ⏳ Pending: {pendingCount}  |  🚚 In Transit: {transitCount}  |  ✅ Completed: {completedCount}  |  📦 Total Volume: {totalCratesSum:N0} Crates";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load loading schedules: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

                private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cbClient.Text) || string.IsNullOrWhiteSpace(cbDestination.Text))
                {
                    MessageBox.Show("Please enter both Client Name and Destination Branch.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dtpScheduleDate.Value.Date < DateTime.Today.AddDays(-365))
                {
                    MessageBox.Show("Please enter a valid schedule date within the past year or a future date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtpScheduleDate.Focus();
                    return;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                if (g == 0 && ec == 0)
                {
                    MessageBox.Show("Please enter at least 1 crate in either G-Crates or E-Crates.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numGCrate.Focus();
                    return;
                }

                string client = cbClient.Text.Trim();
                string dest = cbDestination.Text.Trim();
                string depTime = txtDepartureTime.Text.Trim();

                if (selectedSchedule != null)
                {
                    var ask = MessageBox.Show($"You currently have schedule '{selectedSchedule.ScheduleNumber}' selected.\n\n• Click YES to UPDATE the selected schedule.\n• Click NO to create a NEW additional order.", "Save or Update?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ask == DialogResult.Cancel) return;
                    if (ask == DialogResult.Yes)
                    {
                        BtnUpdate_Click(sender, e);
                        return;
                    }
                }

                string? duplicateNotice = MJdatabase.ValidateLoadingScheduleConflict(0, client, dest, dtpScheduleDate.Value, depTime);
                if (!string.IsNullOrEmpty(duplicateNotice))
                {
                    var conf = MessageBox.Show(duplicateNotice + "\n\nDo you want to proceed and create an ADDITIONAL batch/trip order for this client?", "Additional Batch Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (conf != DialogResult.Yes) return;
                }

                MJdatabase.SaveLookupItem("Client Company", client);
                MJdatabase.SaveLookupItem("Destination Route", dest);

                LoadingSchedule ls = new LoadingSchedule
                {
                    CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled",
                    ClientName = client,
                    Destination = dest,
                    ScheduleDate = dtpScheduleDate.Value,
                    DepartureTime = depTime,
                    GCrate = g,
                    ECrate = ec,
                    TotalCrates = g + ec,
                    IsPeak = chkIsPeak.Checked,
                    Status = "Pending",
                    Remarks = "Scheduled Delivery Order"
                };

                MJdatabase.SaveLoadingSchedule(ls);
                MessageBox.Show("Loading Schedule created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadSchedules();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    MessageBox.Show("Please select a loading schedule to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string client = cbClient.Text.Trim();
                string dest = cbDestination.Text.Trim();
                string depTime = txtDepartureTime.Text.Trim();

                string? conflict = MJdatabase.ValidateLoadingScheduleConflict(selectedSchedule.Id, client, dest, dtpScheduleDate.Value, depTime);
                if (!string.IsNullOrEmpty(conflict))
                {
                    MessageBox.Show(conflict, "Loading Schedule Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                if (g == 0 && ec == 0)
                {
                    MessageBox.Show("Please enter at least 1 crate in either G-Crates or E-Crates.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numGCrate.Focus();
                    return;
                }

                MJdatabase.SaveLookupItem("Client Company", client);
                MJdatabase.SaveLookupItem("Destination Route", dest);

                selectedSchedule.ClientName = client;
                selectedSchedule.Destination = dest;
                selectedSchedule.ScheduleDate = dtpScheduleDate.Value;
                selectedSchedule.DepartureTime = depTime;
                selectedSchedule.GCrate = g;
                selectedSchedule.ECrate = ec;
                selectedSchedule.TotalCrates = g + ec;
                selectedSchedule.IsPeak = chkIsPeak.Checked;
                selectedSchedule.CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled";
                selectedSchedule.Remarks = "Scheduled Delivery Order";

                MJdatabase.SaveLoadingSchedule(selectedSchedule);
                MessageBox.Show("Loading Schedule updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadSchedules();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    MessageBox.Show("Please select a loading schedule to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("LoadingSchedules", selectedSchedule.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Loading Schedule {selectedSchedule.ScheduleNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("LoadingSchedules", selectedSchedule.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Loading Schedule deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadSchedules();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete loading schedule. It may be locked or referenced by active operations.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClone_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    MessageBox.Show("Please select an existing loading schedule from the table to repeat / clone.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var newSched = new LoadingSchedule
                {
                    ScheduleNumber = $"SCH-{DateTime.Now:yyyyMMddHHmmss}",
                    ClientName = selectedSchedule.ClientName,
                    Destination = selectedSchedule.Destination,
                    CargoType = selectedSchedule.CargoType,
                    ScheduleDate = DateTime.Today,
                    DepartureTime = selectedSchedule.DepartureTime,
                    GCrate = selectedSchedule.GCrate,
                    ECrate = selectedSchedule.ECrate,
                    TotalCrates = selectedSchedule.TotalCrates,
                    IsPeak = selectedSchedule.IsPeak,
                    Status = "Pending"
                };

                MJdatabase.SaveLoadingSchedule(newSched);
                LoadSchedules();
                MessageBox.Show($"New Loading Order '{newSched.ScheduleNumber}' for {newSched.ClientName} ({newSched.Destination}) cloned successfully for today!", "Order Cloned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clone loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ? allSchedules
                : allSchedules.Where(s =>
                    (!string.IsNullOrEmpty(s.ScheduleNumber) && s.ScheduleNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.ClientName) && s.ClientName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Destination) && s.Destination.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.CargoType) && s.CargoType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Status) && s.Status.ToLower().Contains(q))
                ).ToList();

            gridSchedules.DataSource = null;
            gridSchedules.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Schedules List ({allSchedules.Count})"
                : $"Active Schedules List - Showing {filtered.Count} of {allSchedules.Count}";

            if (filtered.Count > 0)
            {
                gridSchedules.ClearSelection();
                gridSchedules.Rows[0].Selected = true;
            }
            else
            {
                isBindingForm = true;
                selectedSchedule = null;
                cbClient.Text = "";
                cbDestination.Text = "";
                numGCrate.Value = 0;
                numECrate.Value = 0;
                txtTotalCrates.Text = "";
                lblRec.Text = "No matching schedules found.";
                lblFormHeader.Text = "Loading Order Form";
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
            PrintHelper.PrintGridReport("Loading Schedules & Cargo Order Report", gridSchedules, "Official client loading schedule register with crate quantities and peak season status.");
        }

        private void ClearFields()
        {
            selectedSchedule = null;
            cbClient.Text = "";
            cbCargoType.SelectedIndex = 0;
            cbDestination.Text = "";
            dtpScheduleDate.Value = DateTime.Now;
            txtDepartureTime.Text = "08:00 AM";
            numGCrate.Value = 150;
            numECrate.Value = 0;
            chkIsPeak.Checked = false;
            CalculateCratesAndRecommendation();
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<LoadingSchedule>? OnDispatchScheduleRequested { get; set; }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedSchedule != null) RecordDetailsDialog.ShowDetails(this, selectedSchedule);
            });
            var itemCopySch = new ToolStripMenuItem("📋 Copy Schedule #", null, (s, e) => {
                if (selectedSchedule != null) { Clipboard.SetText(selectedSchedule.ScheduleNumber); ToastHelper.ShowToast(this, "Schedule # copied ✓"); }
            });
            var itemDispatch = new ToolStripMenuItem("🚚 Create Dispatch for This Order", null, (s, e) => {
                if (selectedSchedule != null) OnDispatchScheduleRequested?.Invoke(selectedSchedule);
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Schedule Details", null, (s, e) => {
                cbClient.Focus();
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Loading Slip / Report", null, BtnPrint_Click);
            var itemCancel = new ToolStripMenuItem("❌ Cancel Loading Schedule", null, (s, e) => {
                if (selectedSchedule != null) {
                    selectedSchedule.Status = "Cancelled";
                    MJdatabase.SaveLoadingSchedule(selectedSchedule);
                    LoadSchedules();
                }
            });
            var itemDelete = new ToolStripMenuItem("🗑️ Delete Schedule", null, BtnDelete_Click);

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopySch, new ToolStripSeparator(), itemDispatch, new ToolStripSeparator(), itemEdit, itemPrint, itemCancel, new ToolStripSeparator(), itemDelete });

            gridSchedules.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridSchedules.ClearSelection();
                    gridSchedules.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridSchedules, gridSchedules.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
