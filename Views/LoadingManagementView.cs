using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
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
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

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
            Button btnExport = new Button
            {
                Text = "📊 Export",
                Size = new Size(88, 30),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => DataExportHelper.ExportToCsv(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules");
            pnlGridActionsFlow.Controls.Add(btnExport);

            Button btnReschedule = new Button
            {
                Text = "📅 Reschedule",
                Size = new Size(118, 30),
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReschedule.FlatAppearance.BorderSize = 0;
            btnReschedule.Click += (s, e) => OpenRescheduleDialog();
            pnlGridActionsFlow.Controls.Add(btnReschedule);

            Button btnDispatchOrder = new Button
            {
                Text = "🚚 Dispatch Now",
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDispatchOrder.FlatAppearance.BorderSize = 0;
            btnDispatchOrder.Click += (s, e) => DispatchSelectedSchedule();
            pnlGridActionsFlow.Controls.Add(btnDispatchOrder);

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
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
            LoadSchedules();
        }

        private void SetupEvents()
        {
            numGCrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            numECrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            dtpScheduleDate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            cbClient.TextChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            btnAddStop.Click += BtnAddStop_Click;
            cbDestination.TextChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            cbDestination.SelectedIndexChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            chkIsPeak.CheckedChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            btnTimePicker.Click += (s, e) =>
            {
                string? pickedTime = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtDepartureTime.Text);
                if (!string.IsNullOrEmpty(pickedTime))
                {
                    txtDepartureTime.Text = pickedTime;
                }
            };
            SetupDraftAutoSave();
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

            var rec = MJdatabase.GetTruckRecommendation(total, dtpScheduleDate.Value);
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
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RescheduleCount", HeaderText = "Resched", FillWeight = 60, MinimumWidth = 65, ReadOnly = true });
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
                var rateDestinations = MJdatabase.GetRateMatrix().Select(r => r.Destination).Where(d => !string.IsNullOrWhiteSpace(d));
                var defaultHubs = new[] { "Batangas / Lipa", "Cavite / Dasmariñas", "Laguna / Calamba", "Pampanga / San Fernando", "Bulacan / Meycauayan", "Metro Manila / NCR", "Tarlac / Capas", "Pangasinan / Urdaneta", "Nueva Ecija / Cabanatuan", "Rizal / Antipolo", "Quezon / Lucena" };

                var allDestList = destinations.Concat(rateDestinations).Concat(defaultHubs).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

                cbDestination.Items.Clear();
                foreach (var d in allDestList) cbDestination.Items.Add(d);

                allSchedules = await MJdatabase.GetLoadingSchedulesAsync();
                foreach (var sch in allSchedules)
                {
                    if (!string.IsNullOrWhiteSpace(sch.Destination) && !cbDestination.Items.Contains(sch.Destination))
                    {
                        cbDestination.Items.Add(sch.Destination);
                    }
                }
                FilterGridData();
                var schedules = allSchedules;
                CalculateCratesAndRecommendation();

                int pendingCount = schedules.Count(s => s.Status == "Pending");
                int transitCount = schedules.Count(s => s.Status == "In Transit");
                int completedCount = schedules.Count(s => s.Status == "Completed");
                int totalCratesSum = schedules.Sum(s => s.TotalCrates);

                lblSummary.Text = $"📊 Total Schedules: {schedules.Count:N0}  |  ⏳ Pending: {pendingCount}  |  🚚 In Transit: {transitCount}  |  ✅ Completed: {completedCount}  |  📦 Total Volume: {totalCratesSum:N0} Crates";
                CheckAndPromptDraft();
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
                SaveNewDestinationIfNeeded(dest);

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
                MJdatabase.ClearFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
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
                SaveNewDestinationIfNeeded(dest);

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

        private void DispatchSelectedSchedule()
        {
            if (selectedSchedule == null)
            {
                if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule rowSched)
                {
                    selectedSchedule = rowSched;
                }
                else
                {
                    MessageBox.Show("Please select a loading schedule to dispatch.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (selectedSchedule.Status == "Completed")
            {
                MessageBox.Show($"Loading Order '{selectedSchedule.ScheduleNumber}' is already completed and delivered.", "Already Delivered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedSchedule.Status == "Cancelled")
            {
                MessageBox.Show($"Loading Order '{selectedSchedule.ScheduleNumber}' is cancelled and cannot be dispatched.", "Order Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OnDispatchScheduleRequested?.Invoke(selectedSchedule);
        }

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
            var itemDispatch = new ToolStripMenuItem("🚚 Create Dispatch for This Order", null, (s, e) => DispatchSelectedSchedule());
            var itemReschedule = new ToolStripMenuItem("📅 Reschedule Loading Order...", null, (s, e) => OpenRescheduleDialog());
            var itemEdit = new ToolStripMenuItem("✏️ Edit Schedule Details", null, (s, e) => {
                SetFormVisibility(true);
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
            var itemExportCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) => {
                DataExportHelper.ExportToCsv(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules");
            });
            var itemExportExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) => {
                DataExportHelper.ExportToExcelXml(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules");
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopySch, new ToolStripSeparator(), itemDispatch, itemReschedule, new ToolStripSeparator(), itemEdit, itemPrint, itemExportCsv, itemExportExcel, new ToolStripSeparator(), itemCancel, new ToolStripSeparator(), itemDelete });

            gridSchedules.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridSchedules.ClearSelection();
                    gridSchedules.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridSchedules, gridSchedules.PointToClient(Cursor.Position));
                }
            };
        }

        private void OpenRescheduleDialog()
        {
            if (selectedSchedule == null)
            {
                if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule ls)
                {
                    selectedSchedule = ls;
                }
            }

            if (selectedSchedule == null)
            {
                MessageBox.Show("Please select a loading schedule order to reschedule.", "Select Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedSchedule.Status.Equals("In Transit", StringComparison.OrdinalIgnoreCase) ||
                selectedSchedule.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Loading Order #{selectedSchedule.ScheduleNumber} is currently '{selectedSchedule.Status}' and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedSchedule.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Loading Order #{selectedSchedule.ScheduleNumber} is Cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new RescheduleScheduleDialog(selectedSchedule))
            {
                if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                {
                    LoadSchedules();
                }
            }
        }

        private void SetupDraftAutoSave()
        {
            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };
            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();
            tmrAutoSaveDraft.Start();
        }

        private void CheckAndPromptDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) pnlForm.Controls.Remove(pnlDraftBanner);
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Loading Schedule", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlForm.Controls.Add(pnlDraftBanner);
                    pnlDraftBanner.BringToFront();
                }
            }
            catch { }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("Client", out var cl) && !string.IsNullOrWhiteSpace(cl))
                {
                    cbClient.Text = cl;
                }
                if (map.TryGetValue("Destination", out var dest) && !string.IsNullOrWhiteSpace(dest))
                {
                    cbDestination.Text = dest;
                }
                if (map.TryGetValue("ScheduleDate", out var sd) && DateTime.TryParse(sd, out var sdate) && sdate >= dtpScheduleDate.MinDate && sdate <= dtpScheduleDate.MaxDate)
                {
                    dtpScheduleDate.Value = sdate;
                }
                if (map.TryGetValue("DepartureTime", out var dt) && !string.IsNullOrWhiteSpace(dt))
                {
                    txtDepartureTime.Text = dt;
                }
                if (map.TryGetValue("GCrate", out var gc) && decimal.TryParse(gc, out var gval))
                {
                    numGCrate.Value = Math.Min(numGCrate.Maximum, Math.Max(numGCrate.Minimum, gval));
                }
                if (map.TryGetValue("ECrate", out var ec) && decimal.TryParse(ec, out var eval))
                {
                    numECrate.Value = Math.Min(numECrate.Maximum, Math.Max(numECrate.Minimum, eval));
                }
                if (map.TryGetValue("CargoType", out var ct) && !string.IsNullOrWhiteSpace(ct))
                {
                    cbCargoType.SelectedItem = ct;
                }
                if (map.TryGetValue("IsPeak", out var ip) && bool.TryParse(ip, out var isPeakVal))
                {
                    chkIsPeak.Checked = isPeakVal;
                }

                isBindingForm = false;
                CalculateCratesAndRecommendation();
                ToastHelper.ShowToast(this, "Loading Schedule draft restored ✓", ToastType.Success);
            }
            catch { isBindingForm = false; }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Loading Schedule draft discarded ✓", ToastType.Info);
        }

        private void SaveNewDestinationIfNeeded(string fullDest)
        {
            if (string.IsNullOrWhiteSpace(fullDest)) return;
            var stops = fullDest.Split(new[] { "➔", "->", " / ", ", " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Distinct();

            var existing = MJdatabase.GetLookupItems("Destination Route");
            foreach (var stop in stops)
            {
                if (!existing.Any(e => e.Equals(stop, StringComparison.OrdinalIgnoreCase)))
                {
                    MJdatabase.SaveLookupItem("Destination Route", stop);
                    if (!cbDestination.Items.Contains(stop))
                    {
                        cbDestination.Items.Add(stop);
                    }
                }
            }
            if (!fullDest.Contains("➔") && !fullDest.Contains("->") && !existing.Any(e => e.Equals(fullDest, StringComparison.OrdinalIgnoreCase)))
            {
                MJdatabase.SaveLookupItem("Destination Route", fullDest);
                if (!cbDestination.Items.Contains(fullDest))
                {
                    cbDestination.Items.Add(fullDest);
                }
            }
        }

        private void BtnAddStop_Click(object? sender, EventArgs e)
        {
            var cms = new ContextMenuStrip();
            var allDestinations = cbDestination.Items.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            var titleItem = new ToolStripMenuItem("➕ Add Next Delivery Stop / Destination Address") { Enabled = false, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
            cms.Items.Add(titleItem);
            cms.Items.Add(new ToolStripSeparator());

            var customInputItem = new ToolStripMenuItem("✏️ Type Custom Stop Address...", null, (s, ev) =>
            {
                using var prompt = new Form
                {
                    Text = "Add Destination Drop / Stop Address",
                    Size = new Size(520, 220),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Font = new Font("Segoe UI", 9.5F)
                };

                var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(30, 41, 59), Padding = new Padding(16, 12, 16, 12) };
                var lblPrompt = new Label { Text = "Enter next destination address, branch, or landmark:", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
                pnlHeader.Controls.Add(lblPrompt);

                var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };
                var txtInput = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F) };
                var btnAdd = new Button { Text = "Add Stop to Route ➔", Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(22, 101, 52), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAdd.FlatAppearance.BorderSize = 0;
                btnAdd.Click += (bs, be) =>
                {
                    string val = txtInput.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        AppendDestinationStop(val);
                        prompt.DialogResult = DialogResult.OK;
                        prompt.Close();
                    }
                };

                pnlContent.Controls.Add(btnAdd);
                pnlContent.Controls.Add(txtInput);
                prompt.Controls.Add(pnlContent);
                prompt.Controls.Add(pnlHeader);
                prompt.AcceptButton = btnAdd;
                prompt.ShowDialog(this.FindForm() ?? this.ParentForm);
            });
            cms.Items.Add(customInputItem);
            cms.Items.Add(new ToolStripSeparator());

            var presetMenu = new ToolStripMenuItem("📋 Select from Preset Routes / Destinations");
            foreach (var d in allDestinations.Take(30))
            {
                string destName = d;
                presetMenu.DropDownItems.Add(new ToolStripMenuItem(destName, null, (s, ev) => AppendDestinationStop(destName)));
            }
            cms.Items.Add(presetMenu);

            var multiBuilderItem = new ToolStripMenuItem("🗺️ Multi-Route Stops Checklist...", null, (s, ev) => ShowMultiRouteBuilderDialog());
            cms.Items.Add(multiBuilderItem);

            if (!string.IsNullOrWhiteSpace(cbDestination.Text))
            {
                cms.Items.Add(new ToolStripSeparator());
                cms.Items.Add(new ToolStripMenuItem("🧹 Clear Destination Address", null, (s, ev) => cbDestination.Text = ""));
            }

            cms.Show(btnAddStop, new Point(0, btnAddStop.Height));
        }

        private void AppendDestinationStop(string nextStop)
        {
            string current = cbDestination.Text.Trim();
            if (string.IsNullOrWhiteSpace(current))
            {
                cbDestination.Text = nextStop;
            }
            else
            {
                cbDestination.Text = $"{current} ➔ {nextStop}";
            }
            if (!cbDestination.Items.Contains(nextStop))
            {
                cbDestination.Items.Add(nextStop);
            }
            cbDestination.Focus();
            cbDestination.SelectionStart = cbDestination.Text.Length;
        }

        private void ShowMultiRouteBuilderDialog()
        {
            using var dlg = new Form
            {
                Text = "Multi-Stop Delivery Route Builder",
                Size = new Size(540, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F)
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(30, 41, 59), Padding = new Padding(16, 12, 16, 12) };
            var lblTitle = new Label { Text = "Check delivery stops in sequence to build the route:", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
            pnlHeader.Controls.Add(lblTitle);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(16, 8, 16, 8) };
            var btnApply = new Button { Text = "Apply Route to Order ✓", Dock = DockStyle.Right, Width = 180, BackColor = Color.FromArgb(22, 101, 52), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnApply.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Left, Width = 100, BackColor = Color.FromArgb(241, 245, 249), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => dlg.Close();
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnApply);

            var chkList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = new Font("Segoe UI", 10F) };
            var allDests = cbDestination.Items.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            foreach (var d in allDests)
            {
                chkList.Items.Add(d);
            }

            btnApply.Click += (s, e) =>
            {
                var selected = chkList.CheckedItems.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (selected.Count > 0)
                {
                    cbDestination.Text = string.Join(" ➔ ", selected);
                }
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(chkList);
            dlg.Controls.Add(pnlBottom);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this.FindForm() ?? this.ParentForm);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedSchedule != null) return;
            if (string.IsNullOrWhiteSpace(cbClient.Text) && string.IsNullOrWhiteSpace(cbDestination.Text) && numGCrate.Value == 0 && numECrate.Value == 0) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["Client"] = cbClient.Text.Trim(),
                    ["Destination"] = cbDestination.Text.Trim(),
                    ["ScheduleDate"] = dtpScheduleDate.Value.ToString("yyyy-MM-dd"),
                    ["DepartureTime"] = txtDepartureTime.Text.Trim(),
                    ["GCrate"] = numGCrate.Value.ToString(),
                    ["ECrate"] = numECrate.Value.ToString(),
                    ["CargoType"] = cbCargoType.SelectedItem?.ToString() ?? cbCargoType.Text,
                    ["IsPeak"] = chkIsPeak.Checked.ToString()
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("LoadingSchedule", SessionContext.CurrentUsername, json);
            }
            catch { }
        }
    }
}
