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
    /// <summary>
    /// The "Completed dispatches" picker on the statement form: tick several dispatches to bill them, or type a
    /// dispatch number to add it, plus bulk editing of the statement's rows.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private DataGridView gridPicker = null!;
        private Panel pnlPickerHost = null!;
        private Label lblPickerInfo = null!;
        private Button btnAddSelected = null!;
        private Button btnPickAllReady = null!;
        private TextBox txtAddDispatch = null!;
        private Label lblAddDispatchResult = null!;
        private CheckBox chkPickerAllClients = null!;
        private readonly System.Windows.Forms.Timer pickerRefreshTimer = new System.Windows.Forms.Timer { Interval = 350 };
        private readonly Dictionary<int, decimal> rateCache = new Dictionary<int, decimal>();
        private List<Delivery> deliveryCache = new List<Delivery>();
        private bool pickerReady = false;
        private enum PickState { Ready, OnThisStatement, BilledElsewhere }
        private sealed class PickRow
        {
            public Delivery Delivery = null!;
            public PickState State;
            public string BilledOn = "";
        }
        // ------------------------------------------------------------------ Building
        /// <summary>Controls for the "Completed dispatches" card, top to bottom.</summary>
        private Control[] BuildDispatchPicker()
        {
            // Type-a-number row
            var addRow = new TableLayoutPanel { Height = Dpi(44), ColumnCount = 3, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(360)));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            txtAddDispatch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                PlaceholderText = "Type a dispatch # (e.g. DSP-2026…) and press Enter",
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                Margin = new Padding(0, Dpi(4), Dpi(8), 0)
            };
            txtAddDispatch.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                AddDispatchByNumber();
            };
            var btnAddByNumber = MakeFormButton("＋ Add dispatch", "primary");
            btnAddByNumber.Margin = new Padding(0, Dpi(4), Dpi(12), 0);
            btnAddByNumber.Click += (s, e) => AddDispatchByNumber();
            lblAddDispatchResult = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true, Text = "Fills in the date, stores, DR numbers, truck, crew and rate from that dispatch." };
            addRow.Controls.Add(txtAddDispatch, 0, 0);
            addRow.Controls.Add(btnAddByNumber, 1, 0);
            addRow.Controls.Add(lblAddDispatchResult, 2, 0);
            // Toolbar
            var bar = new TableLayoutPanel { Height = Dpi(46), ColumnCount = 4, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblPickerInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false, Text = "Completed dispatches" };
            chkPickerAllClients = new CheckBox { Text = "All clients", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, Dpi(12), Dpi(12), 0) };
            chkPickerAllClients.CheckedChanged += (s, e) => RefreshPicker();
            btnPickAllReady = MakeFormButton("Tick all ready", "secondary");
            btnPickAllReady.Margin = new Padding(0, Dpi(4), Dpi(8), 0);
            btnPickAllReady.Click += (s, e) => TickAllReady();
            btnAddSelected = MakeFormButton("＋ Add ticked to statement", "primary");
            btnAddSelected.Margin = new Padding(0, Dpi(4), 0, 0);
            btnAddSelected.Click += (s, e) => AddTickedDispatches();
            bar.Controls.Add(lblPickerInfo, 0, 0);
            bar.Controls.Add(chkPickerAllClients, 1, 0);
            bar.Controls.Add(btnPickAllReady, 2, 0);
            bar.Controls.Add(btnAddSelected, 3, 0);
            // Grid
            pnlPickerHost = new Panel { Height = Dpi(240), BackColor = AppTheme.Surface };
            gridPicker = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(36),
                RowTemplate = { Height = Dpi(32) },
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false
            };
            UIThemeHelper.ApplyGridTheme(gridPicker);
            gridPicker.RowTemplate.Height = Dpi(32);
            gridPicker.Columns.Add(new DataGridViewCheckBoxColumn { Name = "pkPick", HeaderText = "", FillWeight = 26, MinimumWidth = Dpi(36) });
            DataGridViewTextBoxColumn Ro(string name, string header, int weight, bool right = false) => new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header, FillWeight = weight, ReadOnly = true,
                DefaultCellStyle = right ? new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } : new DataGridViewCellStyle()
            };
            gridPicker.Columns.Add(Ro("pkDispatch", "Dispatch #", 130));
            gridPicker.Columns.Add(Ro("pkDate", "Date", 80));
            gridPicker.Columns.Add(Ro("pkClient", "Client", 110));
            gridPicker.Columns.Add(Ro("pkRoute", "Route", 130));
            gridPicker.Columns.Add(Ro("pkTruck", "Truck", 90));
            gridPicker.Columns.Add(Ro("pkStores", "Stores", 50, right: true));
            gridPicker.Columns.Add(Ro("pkCrates", "Crates", 55, right: true));
            gridPicker.Columns.Add(Ro("pkRate", "Rate", 80, right: true));
            gridPicker.Columns.Add(Ro("pkState", "Billing", 110));
            gridPicker.CurrentCellDirtyStateChanged += (s, e) => { if (gridPicker.IsCurrentCellDirty) gridPicker.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridPicker.CellValueChanged += (s, e) => UpdatePickerButtons();
            gridPicker.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridPicker.Rows[e.RowIndex].Tag is not PickRow pr) return;
                if (pr.State == PickState.Ready) AddDeliveriesToStatement(new List<Delivery> { pr.Delivery });
            };
            gridPicker.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space && gridPicker.SelectedRows.Count > 1)
                {
                    // Space ticks / unticks every highlighted row (Shift/Ctrl-click to highlight several).
                    bool target = !gridPicker.SelectedRows.Cast<DataGridViewRow>().All(r => r.Cells["pkPick"].Value is true);
                    foreach (DataGridViewRow r in gridPicker.SelectedRows)
                        if (r.Tag is PickRow pr && pr.State == PickState.Ready) r.Cells["pkPick"].Value = target;
                    e.Handled = true;
                    UpdatePickerButtons();
                }
            };
            pnlPickerHost.Controls.Add(gridPicker);
            var hint = new Label
            {
                Height = Dpi(26),
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = true,
                Text = "Tick the dispatches to bill (Shift/Ctrl-click to highlight several, then Space to tick them), or double-click one to add it straight away."
            };
            pickerRefreshTimer.Tick += (s, e) => { pickerRefreshTimer.Stop(); RefreshPicker(); };
            pickerReady = true;
            return new Control[] { addRow, new Panel { Height = Dpi(6), BackColor = AppTheme.Surface }, bar, pnlPickerHost, hint };
        }
        private void SchedulePickerRefresh()
        {
            if (!pickerReady) return;
            pickerRefreshTimer.Stop();
            pickerRefreshTimer.Start();
        }
        // ------------------------------------------------------------------ Data
        private HashSet<int> DeliveryIdsInTable()
        {
            var set = new HashSet<int>();
            if (gridItems == null || !gridItems.Columns.Contains("colDeliveryId")) return set;
            foreach (DataGridViewRow r in gridItems.Rows)
                if (int.TryParse(r.Cells["colDeliveryId"].Value?.ToString(), out int id) && id > 0) set.Add(id);
            return set;
        }
        private decimal RateFor(Delivery d)
        {
            if (rateCache.TryGetValue(d.Id, out var r)) return r;
            try { r = MJdatabase.CalculateDeliveryTariff(d, out _); } catch { r = 0; }
            rateCache[d.Id] = r;
            return r;
        }
        private bool ClientMatches(Delivery d, string client)
        {
            string c = (d.ClientName ?? "").Trim();
            if (client.Length == 0) return true;
            if (c.Length == 0) return false;
            return c.Equals(client, StringComparison.OrdinalIgnoreCase)
                || c.Contains(client, StringComparison.OrdinalIgnoreCase)
                || client.Contains(c, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>Re-lists the completed dispatches for the client and period on the form.</summary>
        private void RefreshPicker()
        {
            if (gridPicker == null || !pnlForm.Visible) return;
            try { deliveryCache = MJdatabase.GetDeliveries(); } catch { deliveryCache = new List<Delivery>(); }
            var src = new AutoCompleteStringCollection();
            src.AddRange(deliveryCache.Select(d => d.DispatchNumber).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray());
            txtAddDispatch.AutoCompleteCustomSource = src;
            string client = chkPickerAllClients.Checked ? "" : cbClient.Text.Trim();
            DateTime from = dtpPeriodFrom.Value.Date, to = dtpPeriodTo.Value.Date;
            var billed = MJdatabase.GetBilledDeliveryIds(isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0);
            var inTable = DeliveryIdsInTable();
            var rows = deliveryCache
                .Where(d => string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                            && d.DispatchDate.Date >= from && d.DispatchDate.Date <= to && ClientMatches(d, client))
                .OrderBy(d => d.DispatchDate).ThenBy(d => d.Id)
                .Select(d => new PickRow
                {
                    Delivery = d,
                    State = inTable.Contains(d.Id) ? PickState.OnThisStatement : billed.ContainsKey(d.Id) ? PickState.BilledElsewhere : PickState.Ready,
                    BilledOn = billed.TryGetValue(d.Id, out var soaNo) ? soaNo : ""
                }).ToList();
            var ticked = new HashSet<int>(gridPicker.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["pkPick"].Value is true && r.Tag is PickRow).Select(r => ((PickRow)r.Tag!).Delivery.Id));
            gridPicker.SuspendLayout();
            gridPicker.Rows.Clear();
            foreach (var pr in rows)
            {
                var d = pr.Delivery;
                int stores = d.TotalStopsCount > 0 ? d.TotalStopsCount : 1;
                decimal rate = RateFor(d);
                string state = pr.State switch
                {
                    PickState.OnThisStatement => "✓ On this statement",
                    PickState.BilledElsewhere => $"Billed on {pr.BilledOn}",
                    _ => "Ready to bill"
                };
                int i = gridPicker.Rows.Add(pr.State == PickState.Ready && ticked.Contains(d.Id), d.DispatchNumber, d.DispatchDate.ToString("MMM d, yyyy"), d.ClientName, d.CleanRouteDisplay, d.TruckInfo, stores,
                    (d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate).ToString("N0"), rate > 0 ? $"₱{rate:N2}" : "no rate", state);
                var row = gridPicker.Rows[i];
                row.Tag = pr;
                if (pr.State != PickState.Ready)
                {
                    row.Cells["pkPick"].ReadOnly = true;
                    row.DefaultCellStyle.ForeColor = AppTheme.TextMuted;
                    row.Cells["pkState"].Style.ForeColor = pr.State == PickState.OnThisStatement ? AppTheme.SuccessText : AppTheme.WarningText;
                }
                else
                {
                    row.Cells["pkState"].Style.ForeColor = AppTheme.InfoText;
                    if (rate <= 0) row.Cells["pkRate"].Style.ForeColor = AppTheme.WarningText;
                }
            }
            gridPicker.ResumeLayout();
            pnlPickerHost.Height = Math.Clamp(Dpi(36) + Math.Max(3, rows.Count) * Dpi(32) + Dpi(4), Dpi(150), Dpi(420));
            int ready = rows.Count(r => r.State == PickState.Ready);
            string who = client.Length == 0 ? "all clients" : client;
            lblPickerInfo.Text = rows.Count == 0
                ? $"No completed dispatches for {who} from {from:MMM d} to {to:MMM d, yyyy}"
                : $"{rows.Count} completed dispatch{(rows.Count == 1 ? "" : "es")} for {who}  ·  {ready} ready to bill";
            UpdatePickerButtons();
        }
        private void UpdatePickerButtons()
        {
            if (btnAddSelected == null) return;
            int n = gridPicker.Rows.Cast<DataGridViewRow>().Count(r => r.Cells["pkPick"].Value is true);
            btnAddSelected.Text = n > 0 ? $"＋ Add {n} ticked to statement" : "＋ Add ticked to statement";
            btnAddSelected.Enabled = n > 0;
            btnPickAllReady.Enabled = gridPicker.Rows.Cast<DataGridViewRow>().Any(r => r.Tag is PickRow pr && pr.State == PickState.Ready);
        }
        private void TickAllReady()
        {
            bool anyUnticked = gridPicker.Rows.Cast<DataGridViewRow>().Any(r => r.Tag is PickRow pr && pr.State == PickState.Ready && r.Cells["pkPick"].Value is not true);
            foreach (DataGridViewRow r in gridPicker.Rows)
                if (r.Tag is PickRow pr && pr.State == PickState.Ready) r.Cells["pkPick"].Value = anyUnticked;
            btnPickAllReady.Text = anyUnticked ? "Untick all" : "Tick all ready";
            UpdatePickerButtons();
        }
        private void AddTickedDispatches()
        {
            var list = gridPicker.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells["pkPick"].Value is true && r.Tag is PickRow pr && pr.State == PickState.Ready)
                .Select(r => ((PickRow)r.Tag!).Delivery).ToList();
            if (list.Count == 0) return;
            AddDeliveriesToStatement(list);
            btnPickAllReady.Text = "Tick all ready";
        }
        // ------------------------------------------------------------------ Adding to the statement
        private int NextTripNumber()
        {
            int max = 0;
            foreach (DataGridViewRow r in gridItems.Rows)
                if (int.TryParse(r.Cells["colTripNo"].Value?.ToString(), out int n) && n > max) max = n;
            return max + 1;
        }
        /// <summary>Adds each dispatch's stores as rows (the first row of a trip carries the rate). Returns how many had no rate.</summary>
        private int AppendDeliveries(List<Delivery> deliveries)
        {
            int unpriced = 0;
            var trucks = MJdatabase.GetTrucks();
            int trip = NextTripNumber();
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                foreach (var del in deliveries.OrderBy(d => d.DispatchDate).ThenBy(d => d.Id))
                {
                    var trk = trucks.FirstOrDefault(t => t.Id == del.TruckId);
                    string plate = trk != null ? trk.PlateNumber : del.TruckInfo;
                    string tonnage = trk != null ? (trk.CapacityCrates >= 400 ? "8T" : (trk.CapacityCrates >= 240 ? "6T" : "4T")) : "6T";
                    string driverHelper = $"{del.DriverName} / {del.AideDisplay}".Trim(' ', '/');
                    var stops = MJdatabase.GetOrBuildTripStopsForDelivery(del);
                    int dropCount = stops.Count > 0 ? stops.Count : (del.TotalStopsCount > 0 ? del.TotalStopsCount : 1);
                    string dateStr = del.DispatchDate.ToString("yyyy-MM-dd");
                    decimal rate = RateFor(del);
                    if (rate <= 0) unpriced++;
                    string did = del.Id.ToString();
                    if (stops.Count > 0)
                    {
                        bool first = true;
                        foreach (var st in stops)
                        {
                            gridItems.Rows.Add(trip, dateStr, "", st.DrNumbers, st.Destination, cbDeliveryType.Text, plate, tonnage, dropCount, first ? rate : 0m, 0m, driverHelper, did);
                            first = false;
                        }
                    }
                    else
                    {
                        gridItems.Rows.Add(trip, dateStr, "", "", del.Destination, cbDeliveryType.Text, plate, tonnage, dropCount, rate, 0m, driverHelper, did);
                    }
                    trip++;
                }
            }
            finally
            {
                isBindingForm = was;
            }
            return unpriced;
        }
        private void AddDeliveriesToStatement(List<Delivery> deliveries)
        {
            var inTable = DeliveryIdsInTable();
            var fresh = deliveries.Where(d => !inTable.Contains(d.Id)).ToList();
            if (fresh.Count == 0)
            {
                ToastHelper.ShowToast(this, "Those dispatches are already on this statement", ToastType.Info);
                return;
            }
            if (string.IsNullOrWhiteSpace(cbClient.Text))
            {
                string first = fresh.Select(d => d.ClientName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";
                if (first.Length > 0) { cbClient.Text = first; AutoFillClientDetails(); }
            }
            int unpriced = AppendDeliveries(fresh);
            RecalculateTaxes(manualOverride: false);
            RefreshPicker();
            string msg = $"Added {fresh.Count} dispatch{(fresh.Count == 1 ? "" : "es")} to the statement";
            if (unpriced > 0) msg += $"; {unpriced} need an amount (no rate found)";
            ToastHelper.ShowToast(this, msg, unpriced > 0 ? ToastType.Warning : ToastType.Success);
        }
        /// <summary>Adds the dispatch typed in the box, checking it's completed, for this client and not billed already.</summary>
        private void AddDispatchByNumber()
        {
            string q = txtAddDispatch.Text.Trim();
            if (q.Length == 0) { txtAddDispatch.Focus(); return; }
            if (deliveryCache.Count == 0) { try { deliveryCache = MJdatabase.GetDeliveries(); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); } }
            var del = deliveryCache.FirstOrDefault(d => string.Equals(d.DispatchNumber, q, StringComparison.OrdinalIgnoreCase));
            if (del == null)
            {
                var partial = deliveryCache.Where(d => (d.DispatchNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                if (partial.Count == 1) del = partial[0];
                else
                {
                    lblAddDispatchResult.Text = partial.Count == 0 ? $"No dispatch matches \"{q}\"." : $"{partial.Count} dispatches match \"{q}\"; type more of the number.";
                    lblAddDispatchResult.ForeColor = AppTheme.WarningText;
                    return;
                }
            }
            if (DeliveryIdsInTable().Contains(del.Id))
            {
                lblAddDispatchResult.Text = $"{del.DispatchNumber} is already on this statement.";
                lblAddDispatchResult.ForeColor = AppTheme.InfoText;
                return;
            }
            var fresh = DispatchGate.Fresh(del);
            string? notDone = DispatchGate.WhyBlocked(fresh, "put it on a statement of account");
            if (notDone != null)
            {
                QuietBox.Show(notDone, "Complete the dispatch first", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblAddDispatchResult.Text = $"{del.DispatchNumber} is not completed yet.";
                lblAddDispatchResult.ForeColor = AppTheme.WarningText;
                return;
            }
            var billed = MJdatabase.GetBilledDeliveryIds(isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0);
            if (billed.TryGetValue(del.Id, out var soaNo))
            {
                lblAddDispatchResult.Text = $"{del.DispatchNumber} is already billed on {soaNo} — it can't be billed twice.";
                lblAddDispatchResult.ForeColor = AppTheme.WarningText;
                return;
            }
            var issues = new List<string>();
            string client = cbClient.Text.Trim();
            if (client.Length > 0 && !ClientMatches(del, client)) issues.Add($"it belongs to {del.ClientName}, not {client}");
            if (del.DispatchDate.Date < dtpPeriodFrom.Value.Date || del.DispatchDate.Date > dtpPeriodTo.Value.Date) issues.Add($"its date ({del.DispatchDate:MMM d}) is outside the period covered");
            if (issues.Count > 0)
            {
                var r = MessageBox.Show($"Add {del.DispatchNumber} anyway?\n\n• " + string.Join("\n• ", issues), "Add dispatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }
            AddDeliveriesToStatement(new List<Delivery> { del });
            lblAddDispatchResult.Text = $"Added {del.DispatchNumber} · {del.CleanRouteDisplay} · {del.DispatchDate:MMM d}";
            lblAddDispatchResult.ForeColor = AppTheme.SuccessText;
            txtAddDispatch.Clear();
            txtAddDispatch.Focus();
        }
        // ------------------------------------------------------------------ Editing several rows at once
        private List<int> SelectedItemRows() => gridItems.SelectedCells.Cast<DataGridViewCell>().Select(c => c.RowIndex)
            .Concat(gridItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index))
            .Where(i => i >= 0).Distinct().OrderBy(i => i).ToList();
        private void RemoveSelectedRows()
        {
            var rows = SelectedItemRows();
            if (rows.Count == 0)
            {
                QuietBox.Show("Click a cell in the rows to remove (Shift/Ctrl-click to pick several).", "Remove rows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (rows.Count > 1 && MessageBox.Show($"Remove {rows.Count} rows from the statement?", "Remove rows", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (int i in rows.OrderByDescending(i => i)) gridItems.Rows.RemoveAt(i);
            RecalculateTaxes(manualOverride: false);
            RefreshPicker();
        }
        /// <summary>Sets one column to the same value on every selected row (e.g. the zone or plate for a whole batch).</summary>
        private void BulkEditSelectedRows()
        {
            gridItems.EndEdit();
            var rows = SelectedItemRows();
            if (rows.Count == 0)
            {
                QuietBox.Show("Select the rows to change first: click a cell, then Shift/Ctrl-click to add more rows.", "Edit selected rows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var editable = gridItems.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && !c.ReadOnly && c.Name != "colTripNo").OrderBy(c => c.DisplayIndex).ToList();
            int startCol = gridItems.CurrentCell != null && editable.Contains(gridItems.Columns[gridItems.CurrentCell.ColumnIndex]) ? editable.IndexOf(gridItems.Columns[gridItems.CurrentCell.ColumnIndex]) : 0;
            using var f = new Form
            {
                Text = "Edit selected rows",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(Dpi(440), Dpi(230)),
                BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F)
            };
            try { ThemeManager.StyleWindowChrome(f); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            var lblTop = new Label { Text = $"Change {rows.Count} selected row{(rows.Count == 1 ? "" : "s")}", Location = new Point(Dpi(18), Dpi(14)), Size = new Size(Dpi(400), Dpi(26)), Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary };
            var lblCol = new Label { Text = "Column", Location = new Point(Dpi(18), Dpi(50)), Size = new Size(Dpi(400), Dpi(20)), ForeColor = AppTheme.TextSecondary };
            var cbCol = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(Dpi(18), Dpi(72)), Size = new Size(Dpi(404), Dpi(28)), Font = new Font("Segoe UI", 10F) };
            foreach (var c in editable) cbCol.Items.Add(c.HeaderText);
            if (cbCol.Items.Count > 0) cbCol.SelectedIndex = Math.Clamp(startCol, 0, cbCol.Items.Count - 1);
            var lblVal = new Label { Text = "New value for all of them", Location = new Point(Dpi(18), Dpi(108)), Size = new Size(Dpi(400), Dpi(20)), ForeColor = AppTheme.TextSecondary };
            var txtVal = new TextBox { Location = new Point(Dpi(18), Dpi(130)), Size = new Size(Dpi(404), Dpi(28)), Font = new Font("Segoe UI", 10.5F) };
            var ok = new Button { Text = "Apply", Size = new Size(Dpi(100), Dpi(36)), Location = new Point(Dpi(322), Dpi(176)), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Size = new Size(Dpi(100), Dpi(36)), Location = new Point(Dpi(214), Dpi(176)), DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            UIThemeHelper.SetButtonRole(ok, "primary");
            UIThemeHelper.SetButtonRole(cancel, "secondary");
            f.Controls.AddRange(new Control[] { lblTop, lblCol, cbCol, lblVal, txtVal, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            if (f.ShowDialog(this) != DialogResult.OK || cbCol.SelectedIndex < 0) return;
            var col = editable[cbCol.SelectedIndex];
            string raw = txtVal.Text.Trim();
            object value = raw;
            if (col.Name is "colAmount" or "colFuel" or "colDrops")
            {
                if (!decimal.TryParse(raw.Replace(",", "").Replace("₱", ""), out var num) || num < 0)
                {
                    MessageBox.Show("Enter a number of 0 or more.", "Edit selected rows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                value = col.Name == "colDrops" ? (object)(int)Math.Round(num) : num;
            }
            bool was = isBindingForm;
            isBindingForm = true;
            foreach (int i in rows) gridItems.Rows[i].Cells[col.Index].Value = value;
            isBindingForm = was;
            RecalculateTaxes(manualOverride: false);
            ToastHelper.ShowToast(this, $"Updated {col.HeaderText} on {rows.Count} row{(rows.Count == 1 ? "" : "s")}", ToastType.Success);
        }
    }
}
