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
        private Label lblDupes = null!;
        private readonly HashSet<int> dupIds = new HashSet<int>();
        /// <summary>Rates that share the same route, destination, truck type and season (left over from older versions): shown in a banner and highlighted.</summary>
        private void ShowDuplicates()
        {
            dupIds.Clear();
            var groups = allRates.GroupBy(r => ((r.RouteName ?? "").Trim().ToLowerInvariant(), (r.Destination ?? "").Trim().ToLowerInvariant(), (r.TruckType ?? "").Trim().ToLowerInvariant(), r.IsPeakSeason))
                                 .Where(g => g.Count() > 1).ToList();
            foreach (var g in groups) foreach (var r in g) dupIds.Add(r.Id);
            if (groups.Count == 0) { lblDupes.Visible = false; return; }
            var g0 = groups[0];
            string prices = string.Join(" and ", g0.Select(r => "PHP " + r.RateAmount.ToString("N2")));
            lblDupes.Text = $"⚠  {(groups.Count == 1 ? "One rate is" : groups.Count + " rates are")} entered twice with different prices: {g0.First().RouteName} · {g0.First().TruckType} · {(g0.Key.IsPeakSeason ? "peak" : "regular")} ({prices}). " +
                            "Keep the correct one and delete the other (highlighted below), so invoices always pick the right price.";
            lblDupes.BackColor = AppTheme.WarningSoft;
            lblDupes.ForeColor = AppTheme.WarningText;
            lblDupes.Visible = true;
            gridRates.Invalidate();
        }
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        public RateMatrixView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            lblDupes = new Label { Dock = DockStyle.Top, AutoSize = false, Height = (int)Math.Round(52 * DeviceDpi / 96f), Visible = false, Padding = new Padding(14, 6, 14, 6), Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft };
            gridRates.Parent?.Controls.Add(lblDupes);
            gridRates.BringToFront();
            SetupGridColumns();
            gridRates.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || dupIds.Count == 0) return;
                if (gridRates.Rows[e.RowIndex].DataBoundItem is RateMatrix rr && dupIds.Contains(rr.Id) && e.CellStyle != null)
                {
                    e.CellStyle.BackColor = AppTheme.WarningSoft;
                    e.CellStyle.ForeColor = AppTheme.WarningText;
                }
            };
            gridRates.CurrentCellDirtyStateChanged += (s, e) => { if (gridRates.IsCurrentCellDirty && gridRates.CurrentCell is DataGridViewCheckBoxCell) gridRates.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridRates.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridRates.Rows[e.RowIndex].DataBoundItem is RateMatrix r) {
                    var clash = FindPeakClash(r, r.IsPeakSeason);
                    if (clash != null)
                    {
                        r.IsPeakSeason = !r.IsPeakSeason;
                        int row = e.RowIndex;
                        BeginInvoke(new Action(() => { try { gridRates.InvalidateRow(row); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); } }));
                        MessageBox.Show(clash, "Duplicate Rate Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    MJdatabase.ToggleRateMatrixPeak(r.Id, r.IsPeakSeason);
                    if (selectedRate != null && selectedRate.Id == r.Id) {
                        chkPeak.Checked = r.IsPeakSeason;
                    }
                }
            };
            LoadRates();
            SetupMultiDestinationMode();
            SetupContextMenu();
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);

            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridRates, "Rate_Matrix", "Rate Matrix"));
            menuActions.Items.Add("🖨 Print Rate Matrix", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);
                    InitFullPageForm();
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
                Size = new Size(80, 28),
                Dock = DockStyle.Right,
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
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to rates");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Route & destination", "Where the rate applies.", lblDest, chkMultiDest, cbDestination, pnlMultiDest),
                new FullPageFormKit.Section("Rate", "Truck type, season and the amount per trip.", lblTt, cbTruckType, chkPeak, lblRate, numRate)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New rate";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridRates.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridRates.Rows[e.RowIndex].DataBoundItem is not RateMatrix) return;
                gridRates.ClearSelection();
                gridRates.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New rate";
            SetFormVisibility(true);
        }
        private void SetFormVisibility(bool visible)
        {
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadRates();
        }

        private bool isPopulatingDestinations = false;
        private ToolTip tipRouteStores = new ToolTip { InitialDelay = 100, ReshowDelay = 100, AutoPopDelay = 15000, ShowAlways = true };

        private void UpdateDestinationTooltip()
        {
            if (isBindingForm || !cbDestination.Visible)
            {
                tipRouteStores.Hide(cbDestination);
                return;
            }
            string text = cbDestination.Text.Trim();
            var matchedRoute = MJdatabase.FindRouteByNameOrCode(text);
            if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)
            {
                ShowRouteStoresPopup(matchedRoute);
            }
            else
            {
                tipRouteStores.SetToolTip(cbDestination, "");
                tipRouteStores.Hide(cbDestination);
            }
        }

        private void ShowRouteStoresPopup(DeliveryRoute? route)
        {
            if (route == null || route.PlacesList.Count == 0 || !this.IsHandleCreated || !cbDestination.Visible)
            {
                tipRouteStores.Hide(cbDestination);
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"📍 {route.RouteCode} - {route.RouteName} ({route.PlacesList.Count} Stores):");
            foreach (var store in route.PlacesList)
            {
                sb.AppendLine($"  • {store}");
            }
            string tipText = sb.ToString().TrimEnd();
            tipRouteStores.SetToolTip(cbDestination, tipText);
            if (cbDestination.Focused)
            {
                tipRouteStores.Show(tipText, cbDestination, 0, cbDestination.Height + 2, 8000);
            }
        }

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

            lblDest.Text = "Delivery Route (e.g. R1, R2):";
            cbDestination.SelectedIndexChanged += (s, e) => UpdateDestinationTooltip();
            cbDestination.TextChanged += (s, e) => UpdateDestinationTooltip();
            cbDestination.Leave += (s, e) => tipRouteStores.Hide(cbDestination);
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
                var deliveryRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
                var routeLabels = new List<string>();
                foreach (var dr in deliveryRoutes)
                {
                    string label = $"{dr.RouteCode} - {dr.RouteName}";
                    routeLabels.Add(label);
                    bool shouldCheck = existingChecked.Contains(label) || existingChecked.Contains(dr.RouteCode);
                    clbDestinations.Items.Add(label, shouldCheck);
                }

                foreach (var extra in existingChecked)
                {
                    if (!routeLabels.Contains(extra, StringComparer.OrdinalIgnoreCase) &&
                        !deliveryRoutes.Any(d => d.RouteCode.Equals(extra, StringComparison.OrdinalIgnoreCase)))
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
            gridRates.ShowCellToolTips = false;
            gridRates.Columns.Clear();
            gridRates.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsPeakSeason", HeaderText = "Peak Season", FillWeight = 11, MinimumWidth = 90, ReadOnly = false });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RouteName", HeaderText = "Route / Client", FillWeight = 25, MinimumWidth = 120, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Destination", HeaderText = "Destination", FillWeight = 30, MinimumWidth = 140, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckType", HeaderText = "Truck Type", FillWeight = 18, MinimumWidth = 100, ReadOnly = true });
            gridRates.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RateAmount", HeaderText = "Rate (PHP)", DefaultCellStyle = { Format = "N2" }, FillWeight = 18, MinimumWidth = 100, ReadOnly = true });
        }

        private void GridRates_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridRates.SelectedRows.Count > 0 && gridRates.SelectedRows[0].DataBoundItem is RateMatrix rm)
            {
                if (chkMultiDest.Checked)
                {
                    chkMultiDest.Checked = false;
                }
                isBindingForm = true;
                selectedRate = rm;
                var matchedRoute = MJdatabase.FindRouteByNameOrCode(selectedRate.RouteName) ?? MJdatabase.FindRouteByNameOrCode(selectedRate.Destination);
                cbDestination.Text = matchedRoute != null ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}" : selectedRate.Destination;
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
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (!chkMultiDest.Checked)
                {
                    if (!ValidationHelper.ValidateRequiredCombo(cbDestination, "Destination Route Coverage", valErrors)) firstInvalid ??= cbDestination;
                }
                if (!ValidationHelper.ValidateRequiredCombo(cbTruckType, "Assigned Truck Type", valErrors)) firstInvalid ??= cbTruckType;
                if (!ValidationHelper.ValidatePositiveNumber(numRate, "Base Rate (PHP)", valErrors)) firstInvalid ??= numRate;
                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }
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

                    QuietBox.Show(summary, "Rate Matrix Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);
                string routeTitle = matchedRoute != null ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}" : dest;
                string fullDest = matchedRoute != null && matchedRoute.PlacesList.Count > 0
                    ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {matchedRoute.FormattedPlaces}"
                    : routeTitle;

                var existingRates = MJdatabase.GetRateMatrix();
                var dupRate = existingRates.Find(r => (selectedRate == null || r.Id != selectedRate.Id) && (r.Destination.Equals(fullDest, StringComparison.OrdinalIgnoreCase) || r.RouteName.Equals(routeTitle, StringComparison.OrdinalIgnoreCase)) && r.TruckType.Equals(ttype, StringComparison.OrdinalIgnoreCase) && r.IsPeakSeason == chkPeak.Checked);
                if (dupRate != null)
                {
                    MessageBox.Show($"This rate already exists:\n\n{routeTitle} · {ttype} · {(chkPeak.Checked ? "peak season" : "regular season")} · PHP {dupRate.RateAmount:N2}\n\nEach route, truck type and season can have only one rate, so invoices always pick the right price. Select that rate in the list and change its amount instead.", "Rate already exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MJdatabase.SaveLookupItem("Truck Type / Category", ttype);

                RateMatrix rm = new RateMatrix
                {
                    RouteName = routeTitle,
                    Destination = fullDest,
                    TruckType = ttype,
                    IsPeakSeason = chkPeak.Checked,
                    RateAmount = numRate.Value
                };

                MJdatabase.SaveRateMatrix(rm);
                QuietBox.Show("Rate Matrix entry added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadRates();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("RateMatrixView", ex); 
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

                if (string.IsNullOrWhiteSpace(ttype))
                {
                    MessageBox.Show("Please pick the truck type this rate applies to.", "Truck Type Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbTruckType.Focus();
                    return;
                }
                if (numRate.Value <= 0)
                {
                    MessageBox.Show("The rate per trip must be more than ₱0.00.", "Rate Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numRate.Focus();
                    return;
                }
                var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);
                string routeTitle = matchedRoute != null ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}" : dest;
                string fullDest = matchedRoute != null && matchedRoute.PlacesList.Count > 0
                    ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {matchedRoute.FormattedPlaces}"
                    : routeTitle;

                var existingRates = MJdatabase.GetRateMatrix();
                var dupRate = existingRates.Find(r => r.Id != selectedRate.Id && (r.Destination.Equals(fullDest, StringComparison.OrdinalIgnoreCase) || r.RouteName.Equals(routeTitle, StringComparison.OrdinalIgnoreCase)) && r.TruckType.Equals(ttype, StringComparison.OrdinalIgnoreCase) && r.IsPeakSeason == chkPeak.Checked);
                if (dupRate != null)
                {
                    MessageBox.Show($"This rate already exists:\n\n{routeTitle} · {ttype} · {(chkPeak.Checked ? "peak season" : "regular season")} · PHP {dupRate.RateAmount:N2}\n\nEach route, truck type and season can have only one rate, so invoices always pick the right price. Select that rate in the list and change its amount instead.", "Rate already exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MJdatabase.SaveLookupItem("Truck Type / Category", ttype);

                selectedRate.RouteName = routeTitle;
                selectedRate.Destination = fullDest;
                selectedRate.TruckType = ttype;
                selectedRate.IsPeakSeason = chkPeak.Checked;
                selectedRate.RateAmount = numRate.Value;

                MJdatabase.SaveRateMatrix(selectedRate);
                QuietBox.Show("Rate Matrix entry updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadRates();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("RateMatrixView", ex); 
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
                        QuietBox.Show("Rate Matrix entry deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            { ErrLog.Error("RateMatrixView", ex); 
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

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridRates, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not RateMatrix r) return;
                m.Items.AddRange(RowMenu.Header($"💲  {RowMenu.Short(r.RouteName, 40)}", $"{r.TruckType} · {(r.IsPeakSeason ? "Peak season" : "Regular season")} · {RowMenu.Money(r.RateAmount)} per trip"));
                m.Items.Add(RowMenu.Item("Edit rate", () => { SetFormVisibility(true); numRate.Focus(); }));
                m.Items.Add(RowMenu.Item(r.IsPeakSeason ? "Make it a regular-season rate" : "Make it a peak-season rate", () => TogglePeak(r)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Route", r.RouteName), ("Rate amount", r.RateAmount.ToString("N2")),
                    ("Summary", $"{r.RouteName} — {r.TruckType}, {(r.IsPeakSeason ? "peak" : "regular")}: {RowMenu.Money(r.RateAmount)}")));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other rates for this route", () => RowMenu.Search(txtGridSearch, r.RouteName)),
                    RowMenu.Item($"Other {r.TruckType} rates", () => RowMenu.Search(txtGridSearch, r.TruckType))));
                m.Items.Add(RowMenu.ExportSub(gridRates, "Transport_Rate_Matrix", "Transport and Route Rates Matrix", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete rate…", () => { selectedRate = r; BtnDelete_Click(this, EventArgs.Empty); }));
            });
        }
        /// <summary>Why a rate can't switch season (another rule already covers it), or null.</summary>
        private string? FindPeakClash(RateMatrix r, bool peak)
        {
            var other = MJdatabase.GetRateMatrix().Find(x => x.Id != r.Id && x.IsPeakSeason == peak && x.TruckType.Equals(r.TruckType, StringComparison.OrdinalIgnoreCase)
                && (x.RouteName.Equals(r.RouteName, StringComparison.OrdinalIgnoreCase) || x.Destination.Equals(r.Destination, StringComparison.OrdinalIgnoreCase)));
            return other == null ? null
                : $"There is already a {(peak ? "peak" : "regular")}-season {r.TruckType} rate for {r.RouteName} ({RowMenu.Money(other.RateAmount)}). Edit or delete that one instead.";
        }
        private void TogglePeak(RateMatrix r)
        {
            bool next = !r.IsPeakSeason;
            var clash = FindPeakClash(r, next);
            if (clash != null) { MessageBox.Show(clash, "Duplicate Rate Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop); return; }
            MJdatabase.ToggleRateMatrixPeak(r.Id, next);
            r.IsPeakSeason = next;
            LoadRates();
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

                var deliveryRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
                cbDestination.Items.Clear();
                foreach (var r in deliveryRoutes)
                {
                    string display = $"{r.RouteCode} - {r.RouteName}";
                    if (!cbDestination.Items.Contains(display)) cbDestination.Items.Add(display);
                    if (!cbDestination.Items.Contains(r.RouteCode)) cbDestination.Items.Add(r.RouteCode);
                }

                PopulateMultiDestinationList();
                allRates = MJdatabase.GetRateMatrix();
                FilterGridData();

                ShowDuplicates();
                lblSummary.Text = $"📊 Total Configured Rates: {allRates.Count:N0}  |  📍 Unique Destinations: {allRates.Select(r => r.Destination).Distinct().Count():N0}";
            }
            catch (Exception ex)
            { ErrLog.Error("RateMatrixView", ex); 
                MessageBox.Show($"Failed to load rate matrix: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
