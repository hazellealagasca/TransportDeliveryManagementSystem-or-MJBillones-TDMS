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
    public partial class RateMatrixView : UserControl, IRefreshable
    {
        private RateMatrix? selectedRate = null;
        private bool isBindingForm = false;
        private List<RateMatrix> allRates = new List<RateMatrix>();
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        public RateMatrixView()
        {
            InitializeComponent();
            gridRates.BringToFront();
            SetupGridColumns();
            gridRates.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridRates.Rows[e.RowIndex].DataBoundItem is RateMatrix item) RecordDetailsDialog.ShowDetails(this, item); };
            gridRates.CurrentCellDirtyStateChanged += (s, e) => { if (gridRates.IsCurrentCellDirty && gridRates.CurrentCell is DataGridViewCheckBoxCell) gridRates.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridRates.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridRates.Rows[e.RowIndex].DataBoundItem is RateMatrix r) {
                    MJdatabase.ToggleRateMatrixPeak(r.Id, r.IsPeakSeason);
                    if (selectedRate != null && selectedRate.Id == r.Id) {
                        chkPeak.Checked = r.IsPeakSeason;
                    }
                }
            };
            LoadRates();
            SetupMultiDestinationMode();
            DataExportHelper.AttachExportContextMenu(gridRates, "Transport_Rate_Matrix", "Transport and Route Rates Matrix");

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridRates, "Rate_Matrix", "Rate Matrix"));
            menuActions.Items.Add("🖨 Print Rate Matrix", null, BtnPrint_Click);
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
                Name = "btnCloseForm",
                Text = "✕ Close",
                Size = new Size(80, 28),
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
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
            LoadRates();
        }

        private bool isPopulatingDestinations = false;

        private void SetupMultiDestinationMode()
        {
            chkMultiDest.CheckedChanged += (s, e) =>
            {
                ToggleMultiDestMode(chkMultiDest.Checked);
            };

            lnkSelectAllDest.LinkClicked += (s, e) =>
            {
                for (int i = 0; i < clbDestinations.Items.Count; i++)
                {
                    clbDestinations.SetItemChecked(i, true);
                }
                UpdateMultiCount();
            };

            lnkClearAllDest.LinkClicked += (s, e) =>
            {
                for (int i = 0; i < clbDestinations.Items.Count; i++)
                {
                    clbDestinations.SetItemChecked(i, false);
                }
                UpdateMultiCount();
            };

            clbDestinations.ItemCheck += (s, e) =>
            {
                if (isPopulatingDestinations) return;
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke((MethodInvoker)delegate { UpdateMultiCount(); });
                }
                else
                {
                    UpdateMultiCount();
                }
            };

            btnAddQuickDest.Click += (s, e) => AddQuickDestinations();
            txtQuickAddDest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddQuickDestinations();
                }
            };
        }

        private void AddQuickDestinations()
        {
            string raw = txtQuickAddDest.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            var parts = raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var p in parts)
            {
                string clean = p.Trim();
                if (string.IsNullOrEmpty(clean)) continue;

                int existingIdx = -1;
                for (int i = 0; i < clbDestinations.Items.Count; i++)
                {
                    if (string.Equals(clbDestinations.Items[i]?.ToString(), clean, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIdx = i;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    clbDestinations.SetItemChecked(existingIdx, true);
                }
                else
                {
                    int newIdx = clbDestinations.Items.Add(clean, true);
                    if (!cbDestination.Items.Contains(clean))
                    {
                        cbDestination.Items.Add(clean);
                    }
                    addedCount++;
                }
            }

            txtQuickAddDest.Clear();
            UpdateMultiCount();
        }

        private void ToggleMultiDestMode(bool isMulti)
        {
            pnlForm.SuspendLayout();
            pnlForm.AutoScrollPosition = new Point(0, 0);

            pnlMultiDest.Visible = isMulti;
            cbDestination.Visible = !isMulti;

            if (isMulti)
            {
                pnlMultiDest.Location = new Point(16, 82);
                int startY = pnlMultiDest.Bottom + 12;

                lblTt.Location = new Point(16, startY);
                cbTruckType.Location = new Point(16, startY + 22);
                chkPeak.Location = new Point(16, startY + 58);
                lblRate.Location = new Point(16, startY + 90);
                numRate.Location = new Point(16, startY + 114);
                btnSave.Location = new Point(16, startY + 154);
                btnSave.Size = new Size(432, 42);
                btnUpdate.Visible = false;
                btnDelete.Visible = false;
                btnClear.Location = new Point(16, startY + 204);
                btnClear.Size = new Size(432, 36);

                PopulateMultiDestinationList();
                UpdateMultiCount();
            }
            else
            {
                lblDest.Location = new Point(16, 60);
                cbDestination.Location = new Point(16, 82);
                lblTt.Location = new Point(16, 118);
                cbTruckType.Location = new Point(16, 140);
                chkPeak.Location = new Point(16, 180);
                lblRate.Location = new Point(16, 212);
                numRate.Location = new Point(16, 234);
                btnSave.Location = new Point(16, 280);
                btnSave.Size = new Size(432, 42);
                btnSave.Text = "➕ ADD RATE";
                btnUpdate.Location = new Point(16, 330);
                btnUpdate.Size = new Size(212, 40);
                btnUpdate.Visible = true;
                btnDelete.Location = new Point(236, 330);
                btnDelete.Size = new Size(212, 40);
                btnDelete.Visible = true;
                btnClear.Location = new Point(16, 378);
                btnClear.Size = new Size(432, 36);
            }

            pnlForm.ResumeLayout(true);
        }

        private void PopulateMultiDestinationList()
        {
            isPopulatingDestinations = true;
            try
            {
                string currentSingle = cbDestination.Text.Trim();
                var existingChecked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < clbDestinations.Items.Count; i++)
                {
                    if (clbDestinations.GetItemChecked(i))
                    {
                        existingChecked.Add(clbDestinations.Items[i].ToString() ?? "");
                    }
                }

                if (!string.IsNullOrEmpty(currentSingle))
                {
                    existingChecked.Add(currentSingle);
                }

                clbDestinations.Items.Clear();
                var routes = MJdatabase.GetLookupItems("Destination Route");
                if (routes.Count == 0)
                {
                    routes = new List<string> { "Batangas / Lipa", "Cavite / Dasmariñas", "Laguna / Calamba", "Pampanga / San Fernando", "Bulacan / Meycauayan", "Metro Manila / NCR" };
                }

                foreach (var r in routes)
                {
                    bool shouldCheck = existingChecked.Contains(r);
                    clbDestinations.Items.Add(r, shouldCheck);
                }

                foreach (var extra in existingChecked)
                {
                    if (!routes.Contains(extra, StringComparer.OrdinalIgnoreCase))
                    {
                        clbDestinations.Items.Add(extra, true);
                    }
                }
            }
            finally
            {
                isPopulatingDestinations = false;
                UpdateMultiCount();
            }
        }

        private void UpdateMultiCount()
        {
            int checkedCount = 0;
            for (int i = 0; i < clbDestinations.Items.Count; i++)
            {
                if (clbDestinations.GetItemChecked(i)) checkedCount++;
            }

            lblMultiCount.Text = $"Selected: {checkedCount} destination(s)";
            if (chkMultiDest.Checked)
            {
                btnSave.Text = checkedCount > 1
                    ? $"⚡ APPLY RATE TO ({checkedCount}) DESTINATIONS"
                    : (checkedCount == 1 ? "➕ ADD RATE (1 DESTINATION)" : "➕ ADD RATE");
            }
        }

        private void SetupGridColumns()
        {
            gridRates.ReadOnly = false;
            gridRates.EditMode = DataGridViewEditMode.EditOnEnter;
            gridRates.AutoGenerateColumns = false;
            gridRates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridRates.ScrollBars = ScrollBars.Both;
            gridRates.AllowUserToResizeColumns = true;
            gridRates.ShowCellToolTips = true;
            gridRates.Columns.Clear();
            gridRates.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsPeakSeason", HeaderText = "Peak", FillWeight = 9, MinimumWidth = 65, ReadOnly = false });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RouteName", HeaderText = "Route / Client", FillWeight = 25, MinimumWidth = 120, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Destination", HeaderText = "Destination", FillWeight = 30, MinimumWidth = 140, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckType", HeaderText = "Truck Type", FillWeight = 18, MinimumWidth = 100, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RateAmount", HeaderText = "Rate (PHP)", DefaultCellStyle = { Format = "N2" }, FillWeight = 18, MinimumWidth = 100, ReadOnly = true });
        }

        private void GridRates_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridRates.SelectedRows.Count > 0 && gridRates.SelectedRows[0].DataBoundItem is RateMatrix rm)
            {
                if (chkMultiDest.Checked)
                {
                    chkMultiDest.Checked = false;
                }
                isBindingForm = true;
                selectedRate = rm;
                cbDestination.Text = selectedRate.Destination;
                cbTruckType.SelectedItem = selectedRate.TruckType;
                chkPeak.Checked = selectedRate.IsPeakSeason;
                numRate.Value = selectedRate.RateAmount;
                lblFormHeader.Text = "Transport Rate Details";
                isBindingForm = false;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (chkMultiDest.Checked)
                {
                    var selectedDests = new List<string>();
                    for (int i = 0; i < clbDestinations.Items.Count; i++)
                    {
                        if (clbDestinations.GetItemChecked(i))
                        {
                            string item = clbDestinations.Items[i].ToString() ?? "";
                            if (!string.IsNullOrWhiteSpace(item)) selectedDests.Add(item.Trim());
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(txtQuickAddDest.Text))
                    {
                        var extra = txtQuickAddDest.Text.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                        foreach (var ex in extra)
                        {
                            if (!selectedDests.Contains(ex, StringComparer.OrdinalIgnoreCase))
                                selectedDests.Add(ex);
                        }
                    }

                    if (selectedDests.Count == 0)
                    {
                        MessageBox.Show("Please select or type at least one destination coverage.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string ttypeMulti = cbTruckType.Text.Trim();
                    if (string.IsNullOrWhiteSpace(ttypeMulti))
                    {
                        MessageBox.Show("Please select assigned truck type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cbTruckType.Focus();
                        return;
                    }

                    bool isCombined = rbCombinedRoute.Checked;
                    var (added, updated, saved) = MJdatabase.SaveMultipleRateMatrix(selectedDests, ttypeMulti, chkPeak.Checked, numRate.Value, isCombined);

                    string summary = isCombined
                        ? $"Successfully configured combined multi-stop route rate for {saved.Count} destinations!\n\n• Route: {saved[0]}\n• Rate: PHP {numRate.Value:N2} ({ttypeMulti})"
                        : $"Successfully applied rate rule (PHP {numRate.Value:N2} for {ttypeMulti}, Peak={chkPeak.Checked}) across {saved.Count} destination(s)!\n\n• New Rates Added: {added}\n• Existing Rates Updated: {updated}\n\nDestinations applied:\n• " + string.Join("\n• ", saved);

                    MessageBox.Show(summary, "Rate Matrix Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadRates();
                    return;
                }

                string dest = cbDestination.Text.Trim();
                string ttype = cbTruckType.Text.Trim();

                if (string.IsNullOrWhiteSpace(dest))
                {
                    MessageBox.Show("Please select or enter destination coverage.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbDestination.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(ttype))
                {
                    MessageBox.Show("Please select assigned truck type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbTruckType.Focus();
                    return;
                }

                var existingRates = MJdatabase.GetRateMatrix();
                var dupRate = existingRates.Find(r => (selectedRate == null || r.Id != selectedRate.Id) && r.Destination.Equals(dest, StringComparison.OrdinalIgnoreCase) && r.TruckType.Equals(ttype, StringComparison.OrdinalIgnoreCase) && r.IsPeakSeason == chkPeak.Checked);
                if (dupRate != null)
                {
                    MessageBox.Show($"DUPLICATE RATE RULE BLOCKED:\nA rate entry for Destination '{dest}', Truck Type '{ttype}', and Peak Season={chkPeak.Checked} already exists (Current Rate: PHP {dupRate.RateAmount:N2}). Duplicate rate rules are strictly prohibited.", "Duplicate Rate Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                MJdatabase.SaveLookupItem("Destination Route", dest);
                MJdatabase.SaveLookupItem("Truck Type / Category", ttype);

                RateMatrix rm = new RateMatrix
                {
                    RouteName = dest,
                    Destination = dest,
                    TruckType = ttype,
                    IsPeakSeason = chkPeak.Checked,
                    RateAmount = numRate.Value
                };

                MJdatabase.SaveRateMatrix(rm);
                MessageBox.Show("Rate Matrix entry added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadRates();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save rate rule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedRate == null)
                {
                    MessageBox.Show("Please select a rate entry to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string dest = cbDestination.Text.Trim();
                string ttype = cbTruckType.Text.Trim();

                if (string.IsNullOrWhiteSpace(dest))
                {
                    MessageBox.Show("Please select or enter destination coverage.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbDestination.Focus();
                    return;
                }

                var existingRates = MJdatabase.GetRateMatrix();
                var dupRate = existingRates.Find(r => r.Id != selectedRate.Id && r.Destination.Equals(dest, StringComparison.OrdinalIgnoreCase) && r.TruckType.Equals(ttype, StringComparison.OrdinalIgnoreCase) && r.IsPeakSeason == chkPeak.Checked);
                if (dupRate != null)
                {
                    MessageBox.Show($"DUPLICATE RATE RULE BLOCKED:\nA rate entry for Destination '{dest}', Truck Type '{ttype}', and Peak Season={chkPeak.Checked} already exists (Current Rate: PHP {dupRate.RateAmount:N2}). Duplicate rate rules are strictly prohibited.", "Duplicate Rate Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                MJdatabase.SaveLookupItem("Destination Route", dest);
                MJdatabase.SaveLookupItem("Truck Type / Category", ttype);

                selectedRate.RouteName = dest;
                selectedRate.Destination = dest;
                selectedRate.TruckType = ttype;
                selectedRate.IsPeakSeason = chkPeak.Checked;
                selectedRate.RateAmount = numRate.Value;

                MJdatabase.SaveRateMatrix(selectedRate);
                MessageBox.Show("Rate Matrix entry updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadRates();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update rate rule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedRate == null)
                {
                    MessageBox.Show("Please select a rate entry to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete rate entry for '{selectedRate.Destination} ({selectedRate.TruckType})'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("RateMatrix", selectedRate.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Rate Matrix entry deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadRates();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete rate rule. It may be locked or referenced by billing calculations.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ? allRates
                : allRates.Where(r =>
                    (!string.IsNullOrEmpty(r.RouteName) && r.RouteName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.Destination) && r.Destination.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.TruckType) && r.TruckType.ToLower().Contains(q))
                ).ToList();

            gridRates.DataSource = null;
            gridRates.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Transport Pricing Rates ({allRates.Count})"
                : $"Active Transport Pricing Rates - Showing {filtered.Count} of {allRates.Count}";

            if (filtered.Count > 0)
            {
                gridRates.ClearSelection();
                gridRates.Rows[0].Selected = true;
            }
            else
            {
                isBindingForm = true;
                selectedRate = null;
                cbDestination.Text = "";
                numRate.Value = 0;
                lblFormHeader.Text = "Transport Rate Details";
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
            PrintHelper.PrintGridReport("Standard Transport Rate Matrix Schedule", gridRates, "Official haulage rate schedule organized by destination coverage, vehicle type, and peak season rates.");
        }

        private void ClearFields()
        {
            selectedRate = null;
            cbDestination.Text = "";
            txtQuickAddDest.Clear();
            for (int i = 0; i < clbDestinations.Items.Count; i++)
            {
                clbDestinations.SetItemChecked(i, false);
            }
            UpdateMultiCount();
            cbTruckType.Text = "Refrigerated Van";
            chkPeak.Checked = false;
            numRate.Value = 8500;
        }

        private void LoadRates()
        {
            try
            {
                var types = MJdatabase.GetLookupItems("Truck Type / Category");
                cbTruckType.Items.Clear();
                if (types.Count == 0)
                {
                    cbTruckType.Items.AddRange(new object[] { "Refrigerated Van", "4-Wheeler Van", "6-Wheeler Truck", "10-Wheeler Heavy Truck" });
                }
                else
                {
                    foreach (var t in types) cbTruckType.Items.Add(t);
                }
                if (cbTruckType.Items.Count > 0 && cbTruckType.SelectedIndex < 0) cbTruckType.SelectedIndex = 0;

                var routes = MJdatabase.GetLookupItems("Destination Route");
                cbDestination.Items.Clear();
                if (routes.Count == 0)
                {
                    cbDestination.Items.AddRange(new object[] { "Batangas / Lipa", "Cavite / Dasmariñas", "Laguna / Calamba", "Pampanga / San Fernando", "Bulacan / Meycauayan", "Metro Manila / NCR" });
                }
                else
                {
                    foreach (var r in routes) cbDestination.Items.Add(r);
                }

                PopulateMultiDestinationList();
                allRates = MJdatabase.GetRateMatrix();
                FilterGridData();

                lblSummary.Text = $"📊 Total Configured Rates: {allRates.Count:N0}  |  📍 Unique Destinations: {allRates.Select(r => r.Destination).Distinct().Count():N0}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load rate matrix: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
