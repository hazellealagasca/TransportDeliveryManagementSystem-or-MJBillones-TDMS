using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class TripReportView : UserControl, IRefreshable
    {
        public Action<int, string, int, DateTime>? OnLogIncidentRequested;
        private TripReport? selectedTripReport = null;
        private bool isBindingForm = false;
        private List<TripReport> allTripReports = new List<TripReport>();
        private string currentSortColumn = "TripReportNumber";
        private bool isSortAscending = true;
        private List<Delivery> allDeliveries = new List<Delivery>();
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private Panel pnlMultiDropInfo = null!;
        private Label lblMultiDropDetails = null!;

        public TripReportView()
        {
            InitializeComponent();
            gridTripReports.BringToFront();
            InitializeMultiDropTripPanel();
            SetupGridColumns();
            SetupContextMenu();
            gridTripReports.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridTripReports.Rows[e.RowIndex].DataBoundItem is TripReport item) RecordDetailsDialog.ShowDetails(this, item); };
            gridTripReports.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridTripReports.Columns.Count) return;
                string col = gridTripReports.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            SetupEvents();
            LoadData();
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
            btnExport.Click += (s, e) => DataExportHelper.ExportToCsv(gridTripReports, "Trip_Reports", "Completed Trip Reports");
            pnlGridActionsFlow.Controls.Add(btnExport);

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
            LoadData();
        }

        private void InitializeMultiDropTripPanel()
        {
            pnlMultiDropInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Visible = false,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 8)
            };
            lblMultiDropDetails = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(22, 101, 52),
                Text = ""
            };
            pnlMultiDropInfo.Controls.Add(lblMultiDropDetails);
            pnlForm.Controls.Add(pnlMultiDropInfo);
            int dIdx = pnlForm.Controls.IndexOf(cbDelivery);
            if (dIdx >= 0)
            {
                pnlForm.Controls.SetChildIndex(pnlMultiDropInfo, dIdx + 1);
            }
        }

        private void UpdateMultiDropTripInfo(Delivery d)
        {
            if (d.IsMultiDrop)
            {
                var stops = MJdatabase.GetDispatchStops(d.Id);
                string stopsSummary = stops.Count > 0
                    ? string.Join(" ➔ ", stops.Select(s => $"{s.StopSequence}. {s.ClientName} ({s.TotalCrates} cr)"))
                    : d.LoadingScheduleInfo;
                lblMultiDropDetails.Text = $"🗺️ Consolidated Multi-Drop Route ({d.TotalStopsCount} stops):\n{stopsSummary}";
                pnlMultiDropInfo.Visible = true;
            }
            else
            {
                pnlMultiDropInfo.Visible = false;
            }
        }

        private void SetupEvents()
        {
            cbDelivery.SelectedIndexChanged += (s, e) =>
            {
                if (isBindingForm) return;
                if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < allDeliveries.Count)
                {
                    var d = allDeliveries[cbDelivery.SelectedIndex];
                    numDeliveredGCrates.Value = Math.Max(0, d.GCrate);
                    numDeliveredECrates.Value = Math.Max(0, d.ECrate);
                    numReturnedGCrates.Value = 0;
                    numReturnedECrates.Value = 0;
                    CheckCrateVariance();
                    UpdateMultiDropTripInfo(d);
                }
            };

            numDeliveredGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numDeliveredECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            SetupDraftAutoSave();
        }

        private void CheckCrateVariance()
        {
            if (cbDelivery.SelectedIndex < 0)
            {
                btnLogIncident.Visible = false;
                return;
            }
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber));
            if (selDel == null)
            {
                btnLogIncident.Visible = false;
                return;
            }

            int dgc = (int)numDeliveredGCrates.Value;
            int dec = (int)numDeliveredECrates.Value;
            int totalDel = dgc + dec;
            int totalDisp = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);

            if (totalDisp > totalDel)
            {
                int missing = totalDisp - totalDel;
                btnLogIncident.Text = $"⚠️ LOG INCIDENT ({missing} MISSING CRATES)";
                btnLogIncident.Visible = true;
            }
            else
            {
                btnLogIncident.Visible = false;
            }
        }

        private void BtnLogIncident_Click(object? sender, EventArgs e)
        {
            if (cbDelivery.SelectedIndex < 0) return;
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber));
            if (selDel == null) return;

            int dgc = (int)numDeliveredGCrates.Value;
            int dec = (int)numDeliveredECrates.Value;
            int totalDel = dgc + dec;
            int totalDisp = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);
            int missing = Math.Max(1, totalDisp - totalDel);

            OnLogIncidentRequested?.Invoke(selDel.Id, selDel.DispatchNumber, missing, DateTime.Now);
        }

        private void SetupGridColumns()
        {
            gridTripReports.AutoGenerateColumns = false;
            gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridTripReports.ScrollBars = ScrollBars.Both;
            gridTripReports.AllowUserToResizeColumns = true;
            gridTripReports.ShowCellToolTips = true;
            gridTripReports.Columns.Clear();
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TripReportNumber", HeaderText = "Report #", FillWeight = 110, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 110, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReportDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ArrivalTime", HeaderText = "Arrival", FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Departure", FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredGCrates", HeaderText = "Del. G", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredECrates", HeaderText = "Del. E", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredCrates", HeaderText = "Total Del.", FillWeight = 75, MinimumWidth = 75, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedGCrates", HeaderText = "Ret. G", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedECrates", HeaderText = "Ret. E", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedCrates", HeaderText = "Total Ret.", FillWeight = 75, MinimumWidth = 75, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StoreManagerName", HeaderText = "Manager Signee", FillWeight = 120, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Remarks", HeaderText = "Remarks", FillWeight = 130, MinimumWidth = 130, ReadOnly = true });
        }

                        private void GridTripReports_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridTripReports.SelectedRows.Count > 0 && gridTripReports.SelectedRows[0].DataBoundItem is TripReport tr)
            {
                isBindingForm = true;
                selectedTripReport = tr;

                int dIdx = allDeliveries.FindIndex(d => d.Id == selectedTripReport.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;

                txtArrivalTime.Text = string.IsNullOrEmpty(selectedTripReport.ArrivalTime) ? "10:00 AM" : selectedTripReport.ArrivalTime;
                txtDepartureTime.Text = string.IsNullOrEmpty(selectedTripReport.DepartureTime) ? "11:30 AM" : selectedTripReport.DepartureTime;
                numDeliveredGCrates.Value = Math.Max(0, selectedTripReport.DeliveredGCrates);
                numDeliveredECrates.Value = Math.Max(0, selectedTripReport.DeliveredECrates);
                numReturnedGCrates.Value = Math.Max(0, selectedTripReport.ReturnedGCrates);
                numReturnedECrates.Value = Math.Max(0, selectedTripReport.ReturnedECrates);
                txtStoreManagerLastName.Text = selectedTripReport.StoreManagerLastName ?? "";
                txtStoreManagerFirstName.Text = selectedTripReport.StoreManagerFirstName ?? "";
                txtStoreManagerMiddleName.Text = selectedTripReport.StoreManagerMiddleName ?? "";
                txtRemarks.Text = selectedTripReport.Remarks;
                var del = allDeliveries.FirstOrDefault(d => d.Id == selectedTripReport.DeliveryId);
                if (del != null) UpdateMultiDropTripInfo(del);
                lblFormHeader.Text = "Trip Completion Record";
                isBindingForm = false;
            }
        }

        private async void LoadData()
        {
            try
            {
                allDeliveries = await MJdatabase.GetDeliveriesAsync();
                var pendingOrTransit = allDeliveries.FindAll(d => d.Status == "Pending" || d.Status == "In Transit");

                cbDelivery.Items.Clear();
                if (allDeliveries.Count == 0)
                {
                    cbDelivery.Text = "(No dispatch orders created yet)";
                }
                else if (pendingOrTransit.Count == 0)
                {
                    foreach (var d in allDeliveries)
                    {
                        cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo} [{d.Status}] ({d.DriverName})");
                    }
                    if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
                }
                else
                {
                    foreach (var d in pendingOrTransit)
                    {
                        cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo} [{d.Status}] ({d.DriverName})");
                    }
                    foreach (var d in allDeliveries.Where(d => d.Status != "Pending" && d.Status != "In Transit"))
                    {
                        cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo} [{d.Status}] ({d.DriverName})");
                    }
                    if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
                }

                allTripReports = await MJdatabase.GetTripReportsAsync();
                FilterGridData();
                var reports = allTripReports;

                int totalDel = reports.Sum(r => r.DeliveredCrates);
                int totalRet = reports.Sum(r => r.ReturnedCrates);
                lblSummary.Text = $"📊 Total Completed Trips: {reports.Count:N0}  |  📥 Delivered: {totalDel:N0} Crates  |  🔄 Returned Empties: {totalRet:N0} Crates";
                CheckAndPromptDraft();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load trip completion records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbDelivery.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a dispatch order to complete.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber));
                if (selDel == null)
                {
                    MessageBox.Show("Selected dispatch order not found. Please refresh and try again.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string smLastName = txtStoreManagerLastName.Text.Trim();
                string smFirstName = txtStoreManagerFirstName.Text.Trim();
                string smMiddleName = txtStoreManagerMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(smLastName) || !SecurityHelper.IsValidName(smLastName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel Last Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(smFirstName) || !SecurityHelper.IsValidName(smFirstName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel First Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(smMiddleName) && !SecurityHelper.IsValidName(smMiddleName))
                {
                    MessageBox.Show("Please enter a valid middle name, or leave it blank.", "Manager Sign-Off Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerMiddleName.Focus();
                    return;
                }

                string storeManagerName = NameHelper.FormatFullName(smLastName, smFirstName, smMiddleName);

                if (DateTime.TryParse(txtArrivalTime.Text.Trim(), out var arrTime) && DateTime.TryParse(txtDepartureTime.Text.Trim(), out var depTime))
                {
                    if (depTime < arrTime)
                    {
                        var timePrompt = MessageBox.Show($"Store Departure Time ({txtDepartureTime.Text.Trim()}) is earlier than Store Arrival Time ({txtArrivalTime.Text.Trim()}).\n\nAre you sure this transit chronology is accurate?", "Transit Time Chronology Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (timePrompt != DialogResult.Yes)
                        {
                            txtDepartureTime.Focus();
                            return;
                        }
                    }
                }

                int dgc = (int)numDeliveredGCrates.Value;
                int dec = (int)numDeliveredECrates.Value;
                int rgc = (int)numReturnedGCrates.Value;
                int rec = (int)numReturnedECrates.Value;

                int totalDispatched = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);
                int totalDelivered = dgc + dec;
                if (totalDispatched > 0 && totalDelivered != totalDispatched)
                {
                    var auditPrompt = MessageBox.Show(
                        $"Crate Variance Detected!\n\nDispatched Quantity: {totalDispatched} Crates\nDelivered Quantity: {totalDelivered} Crates\nDifference: {Math.Abs(totalDispatched - totalDelivered)} Crates\n\nDo you wish to proceed with completing this trip report?",
                        "Crate Quantity Reconciliation Notice",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );
                    if (auditPrompt != DialogResult.Yes) return;
                }

                TripReport tr = new TripReport
                {
                    DeliveryId = selDel.Id,
                    DispatchNumber = selDel.DispatchNumber,
                    ArrivalTime = txtArrivalTime.Text.Trim(),
                    DepartureTime = txtDepartureTime.Text.Trim(),
                    DeliveredGCrates = dgc,
                    DeliveredECrates = dec,
                    DeliveredCrates = dgc + dec,
                    ReturnedGCrates = rgc,
                    ReturnedECrates = rec,
                    ReturnedCrates = rgc + rec,
                    StoreManagerLastName = smLastName,
                    StoreManagerFirstName = smFirstName,
                    StoreManagerMiddleName = smMiddleName,
                    StoreManagerName = storeManagerName,
                    Remarks = txtRemarks.Text.Trim(),
                    ReportDate = DateTime.Now
                };

                MJdatabase.SaveTripReport(tr);
                MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
                MessageBox.Show("Trip Report saved and Delivery marked Completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (totalDispatched > totalDelivered)
                {
                    int missing = totalDispatched - totalDelivered;
                    var promptLog = MessageBox.Show($"Trip Report completed with {missing} missing/damaged crate(s).\n\nWould you like to log an Incident & Damage Report for this delivery now?", "Log Incident Report?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (promptLog == DialogResult.Yes)
                    {
                        OnLogIncidentRequested?.Invoke(selDel.Id, selDel.DispatchNumber, missing, DateTime.Now);
                        return;
                    }
                }

                ClearFields();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save trip completion report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedTripReport == null)
                {
                    MessageBox.Show("Please select a trip report to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string smLastName = txtStoreManagerLastName.Text.Trim();
                string smFirstName = txtStoreManagerFirstName.Text.Trim();
                string smMiddleName = txtStoreManagerMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(smLastName) || !SecurityHelper.IsValidName(smLastName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel Last Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(smFirstName) || !SecurityHelper.IsValidName(smFirstName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel First Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(smMiddleName) && !SecurityHelper.IsValidName(smMiddleName))
                {
                    MessageBox.Show("Please enter a valid middle name, or leave it blank.", "Manager Sign-Off Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerMiddleName.Focus();
                    return;
                }

                string storeManagerName = NameHelper.FormatFullName(smLastName, smFirstName, smMiddleName);

                if (DateTime.TryParse(txtArrivalTime.Text.Trim(), out var arrTime) && DateTime.TryParse(txtDepartureTime.Text.Trim(), out var depTime))
                {
                    if (depTime < arrTime)
                    {
                        var timePrompt = MessageBox.Show($"Store Departure Time ({txtDepartureTime.Text.Trim()}) is earlier than Store Arrival Time ({txtArrivalTime.Text.Trim()}).\n\nAre you sure this transit chronology is accurate?", "Transit Time Chronology Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (timePrompt != DialogResult.Yes)
                        {
                            txtDepartureTime.Focus();
                            return;
                        }
                    }
                }

                int dgc = (int)numDeliveredGCrates.Value;
                int dec = (int)numDeliveredECrates.Value;
                int rgc = (int)numReturnedGCrates.Value;
                int rec = (int)numReturnedECrates.Value;

                selectedTripReport.ArrivalTime = txtArrivalTime.Text.Trim();
                selectedTripReport.DepartureTime = txtDepartureTime.Text.Trim();
                selectedTripReport.DeliveredGCrates = dgc;
                selectedTripReport.DeliveredECrates = dec;
                selectedTripReport.DeliveredCrates = dgc + dec;
                selectedTripReport.ReturnedGCrates = rgc;
                selectedTripReport.ReturnedECrates = rec;
                selectedTripReport.ReturnedCrates = rgc + rec;
                selectedTripReport.StoreManagerLastName = smLastName;
                selectedTripReport.StoreManagerFirstName = smFirstName;
                selectedTripReport.StoreManagerMiddleName = smMiddleName;
                selectedTripReport.StoreManagerName = storeManagerName;
                selectedTripReport.Remarks = txtRemarks.Text.Trim();

                MJdatabase.SaveTripReport(selectedTripReport);
                MessageBox.Show("Trip Report updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update trip completion report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedTripReport == null)
                {
                    MessageBox.Show("Please select a trip report to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Trip Report {selectedTripReport.TripReportNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("TripReports", selectedTripReport.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Trip Report deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Trip Report. It may be locked or referenced by billing invoices.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ? allTripReports
                : allTripReports.Where(t =>
                    (!string.IsNullOrEmpty(t.TripReportNumber) && t.TripReportNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.DispatchNumber) && t.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerName) && t.StoreManagerName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerLastName) && t.StoreManagerLastName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerFirstName) && t.StoreManagerFirstName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerMiddleName) && t.StoreManagerMiddleName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.Remarks) && t.Remarks.ToLower().Contains(q))
                ).ToList();

            gridTripReports.DataSource = null;
            gridTripReports.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Completed Trips Log ({allTripReports.Count})"
                : $"Completed Trips Log - Showing {filtered.Count} of {allTripReports.Count}";

            if (filtered.Count > 0)
            {
                gridTripReports.ClearSelection();
                gridTripReports.Rows[0].Selected = true;
            }
            else
            {
                isBindingForm = true;
                selectedTripReport = null;
                txtStoreManagerLastName.Text = "";
                txtStoreManagerFirstName.Text = "";
                txtStoreManagerMiddleName.Text = "";
                txtRemarks.Text = "";
                lblFormHeader.Text = "Trip Completion Record";
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
            PrintHelper.PrintGridReport("Plant & Store Trip Documentation Report", gridTripReports, "Official trip delivery completion register with G/E crate receipts and store signees.");
        }

                private void BtnPickArrival_Click(object? sender, EventArgs e)
        {
            string? picked = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtArrivalTime.Text);
            if (!string.IsNullOrEmpty(picked))
            {
                txtArrivalTime.Text = picked;
            }
        }

        private void BtnPickDeparture_Click(object? sender, EventArgs e)
        {
            string? picked = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtDepartureTime.Text);
            if (!string.IsNullOrEmpty(picked))
            {
                txtDepartureTime.Text = picked;
            }
        }

        private void ClearFields()
        {
            selectedTripReport = null;
            txtArrivalTime.Text = "10:30 AM";
            txtDepartureTime.Text = "11:45 AM";
            numDeliveredGCrates.Value = 150;
            numDeliveredECrates.Value = 0;
            numReturnedGCrates.Value = 150;
            numReturnedECrates.Value = 0;
            txtStoreManagerLastName.Text = "";
            txtStoreManagerFirstName.Text = "";
            txtStoreManagerMiddleName.Text = "";
            txtRemarks.Text = "";
            if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
            if (pnlMultiDropInfo != null) pnlMultiDropInfo.Visible = false;
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<TripReport>? OnCreateInvoiceRequested { get; set; }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedTripReport != null) RecordDetailsDialog.ShowDetails(this, selectedTripReport);
            });
            var itemCopyTrip = new ToolStripMenuItem("📋 Copy Trip Report #", null, (s, e) => {
                if (selectedTripReport != null) { Clipboard.SetText(selectedTripReport.TripReportNumber); ToastHelper.ShowToast(this, "Trip Report # copied ✓"); }
            });
            var itemInvoice = new ToolStripMenuItem("💰 Issue Billing Invoice for Trip", null, (s, e) => {
                if (selectedTripReport != null) OnCreateInvoiceRequested?.Invoke(selectedTripReport);
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Trip Report", null, (s, e) => {
                SetFormVisibility(true);
                cbDelivery.Focus();
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Trip Summary Report", null, BtnPrint_Click);
            var itemDelete = new ToolStripMenuItem("🗑️ Delete Trip Report", null, BtnDelete_Click);
            var itemExportCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) => {
                DataExportHelper.ExportToCsv(gridTripReports, "Trip_Reports", "Completed Trip Reports");
            });
            var itemExportExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) => {
                DataExportHelper.ExportToExcelXml(gridTripReports, "Trip_Reports", "Completed Trip Reports");
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyTrip, new ToolStripSeparator(), itemEdit, itemInvoice, new ToolStripSeparator(), itemPrint, itemExportCsv, itemExportExcel, new ToolStripSeparator(), itemDelete });

            gridTripReports.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridTripReports.ClearSelection();
                    gridTripReports.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridTripReports, gridTripReports.PointToClient(Cursor.Position));
                }
            };
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
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) pnlForm.Controls.Remove(pnlDraftBanner);
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Trip Report", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
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
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("DeliveryIndex", out var dIdx) && int.TryParse(dIdx, out int idx) && idx >= 0 && idx < cbDelivery.Items.Count)
                {
                    cbDelivery.SelectedIndex = idx;
                }
                if (map.TryGetValue("ArrivalTime", out var at) && !string.IsNullOrWhiteSpace(at))
                {
                    txtArrivalTime.Text = at;
                }
                if (map.TryGetValue("DepartureTime", out var dt) && !string.IsNullOrWhiteSpace(dt))
                {
                    txtDepartureTime.Text = dt;
                }
                if (map.TryGetValue("DeliveredGCrates", out var dgc) && decimal.TryParse(dgc, out var dgcVal))
                {
                    numDeliveredGCrates.Value = Math.Min(numDeliveredGCrates.Maximum, Math.Max(numDeliveredGCrates.Minimum, dgcVal));
                }
                if (map.TryGetValue("DeliveredECrates", out var dec) && decimal.TryParse(dec, out var decVal))
                {
                    numDeliveredECrates.Value = Math.Min(numDeliveredECrates.Maximum, Math.Max(numDeliveredECrates.Minimum, decVal));
                }
                if (map.TryGetValue("ReturnedGCrates", out var rgc) && decimal.TryParse(rgc, out var rgcVal))
                {
                    numReturnedGCrates.Value = Math.Min(numReturnedGCrates.Maximum, Math.Max(numReturnedGCrates.Minimum, rgcVal));
                }
                if (map.TryGetValue("ReturnedECrates", out var rec) && decimal.TryParse(rec, out var recVal))
                {
                    numReturnedECrates.Value = Math.Min(numReturnedECrates.Maximum, Math.Max(numReturnedECrates.Minimum, recVal));
                }
                if (map.TryGetValue("StoreManagerLastName", out var sml) && !string.IsNullOrWhiteSpace(sml))
                {
                    txtStoreManagerLastName.Text = sml;
                }
                if (map.TryGetValue("StoreManagerFirstName", out var smf) && !string.IsNullOrWhiteSpace(smf))
                {
                    txtStoreManagerFirstName.Text = smf;
                }
                if (map.TryGetValue("StoreManagerMiddleName", out var smm) && !string.IsNullOrWhiteSpace(smm))
                {
                    txtStoreManagerMiddleName.Text = smm;
                }
                if (map.TryGetValue("Remarks", out var rem) && !string.IsNullOrWhiteSpace(rem))
                {
                    txtRemarks.Text = rem;
                }

                isBindingForm = false;
                CheckCrateVariance();
                ToastHelper.ShowToast(this, "Trip Report draft restored ✓", ToastType.Success);
            }
            catch { isBindingForm = false; }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Trip Report draft discarded ✓", ToastType.Info);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedTripReport != null) return;
            if (string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text) && string.IsNullOrWhiteSpace(txtRemarks.Text) && numDeliveredGCrates.Value == 0 && numDeliveredECrates.Value == 0) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["DeliveryIndex"] = cbDelivery.SelectedIndex.ToString(),
                    ["ArrivalTime"] = txtArrivalTime.Text.Trim(),
                    ["DepartureTime"] = txtDepartureTime.Text.Trim(),
                    ["DeliveredGCrates"] = numDeliveredGCrates.Value.ToString(),
                    ["DeliveredECrates"] = numDeliveredECrates.Value.ToString(),
                    ["ReturnedGCrates"] = numReturnedGCrates.Value.ToString(),
                    ["ReturnedECrates"] = numReturnedECrates.Value.ToString(),
                    ["StoreManagerLastName"] = txtStoreManagerLastName.Text.Trim(),
                    ["StoreManagerFirstName"] = txtStoreManagerFirstName.Text.Trim(),
                    ["StoreManagerMiddleName"] = txtStoreManagerMiddleName.Text.Trim(),
                    ["Remarks"] = txtRemarks.Text.Trim()
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("TripReport", SessionContext.CurrentUsername, json);
            }
            catch { }
        }
    }
}
