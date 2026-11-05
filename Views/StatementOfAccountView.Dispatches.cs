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
    /// The "Completed trip reports" picker on the statement form (the statement's source): tick several trip reports to
    /// bill them, or type a trip report or dispatch number to add it, plus bulk editing of the statement's rows.
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
        private Button btnRefreshFromTrips = null!;
        private readonly System.Windows.Forms.Timer pickerRefreshTimer = new System.Windows.Forms.Timer { Interval = 350 };
        private readonly Dictionary<int, decimal> rateCache = new Dictionary<int, decimal>();
        private List<Delivery> deliveryCache = new List<Delivery>();
        private bool pickerReady = false;
        /// <summary>Completed trips in the period that still have no trip report (not listed; shown in the heading).</summary>
        private int pickerNeedReport;
        private enum PickState { Ready, OnThisStatement, BilledElsewhere }
        private sealed class PickRow
        {
            public Delivery Delivery = null!;
            public TripReport? Report;
            /// <summary>Set when the row is a special trip ticked "With SOA" (Delivery is then a stand-in with Id = −trip Id).</summary>
            public SpecialTrip? Special;
            public PickState State;
            public string BilledOn = "";
        }
        // ------------------------------------------------------------------ Building
        /// <summary>Controls for the "Completed trip reports" card, top to bottom.</summary>
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
                PlaceholderText = "Type a trip report # or dispatch # and press Enter",
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
            var btnAddByNumber = MakeFormButton("＋ Add trip", "primary");
            btnAddByNumber.Margin = new Padding(0, Dpi(4), Dpi(12), 0);
            btnAddByNumber.Click += (s, e) => AddDispatchByNumber();
            lblAddDispatchResult = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true, Text = "Fills in the trip report #, date, stores, DR numbers, total crates, truck, crew and rate from that trip report." };
            addRow.Controls.Add(txtAddDispatch, 0, 0);
            addRow.Controls.Add(btnAddByNumber, 1, 0);
            addRow.Controls.Add(lblAddDispatchResult, 2, 0);
            // Toolbar
            var bar = new TableLayoutPanel { Height = Dpi(46), ColumnCount = 5, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblPickerInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false, Text = "Completed trip reports" };
            chkPickerAllClients = new CheckBox { Text = "All clients", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, Dpi(12), Dpi(12), 0) };
            chkPickerAllClients.CheckedChanged += (s, e) => RefreshPicker();
            btnPickAllReady = MakeFormButton("Tick all ready", "secondary");
            btnPickAllReady.Margin = new Padding(0, Dpi(4), Dpi(8), 0);
            btnPickAllReady.Click += (s, e) => TickAllReady();
            btnAddSelected = MakeFormButton("＋ Add ticked to statement", "primary");
            btnAddSelected.Margin = new Padding(0, Dpi(4), 0, 0);
            btnAddSelected.Click += (s, e) => AddTickedDispatches();
            // Re-reads the stores and DR numbers of the trips already on the statement (after a trip report was corrected).
            btnRefreshFromTrips = MakeFormButton("↻ Refresh lines from trip reports", "secondary");
            btnRefreshFromTrips.Margin = new Padding(0, Dpi(4), Dpi(8), 0);
            btnRefreshFromTrips.Click += (s, e) => RefreshLinesFromTripReports();
            bar.Controls.Add(lblPickerInfo, 0, 0);
            bar.Controls.Add(chkPickerAllClients, 1, 0);
            bar.Controls.Add(btnRefreshFromTrips, 2, 0);
            bar.Controls.Add(btnPickAllReady, 3, 0);
            bar.Controls.Add(btnAddSelected, 4, 0);
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
            gridPicker.Columns.Add(Ro("pkReport", "Trip report #", 170));
            // Wide enough for a whole number such as TRIP-20261105-022839-71078.
            gridPicker.Columns["pkReport"]!.MinimumWidth = TextRenderer.MeasureText("TRIP-20261105-022839-71078", gridPicker.Font).Width + Dpi(16);
            gridPicker.Columns.Add(Ro("pkDate", "Trip date", 80));
            gridPicker.Columns.Add(Ro("pkDispatch", "Dispatch #", 110));
            gridPicker.Columns.Add(Ro("pkClient", "Client", 110));
            gridPicker.Columns.Add(Ro("pkTruck", "Truck", 110));
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
                Text = "Tick the trip reports to bill (Shift/Ctrl-click to highlight several, then Space to tick them), or double-click one to add it straight away."
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
        /// <summary>Special trips already on this statement (by the hidden link column).</summary>
        private HashSet<int> SpecialTripIdsInTable()
        {
            var ids = new HashSet<int>();
            if (!gridItems.Columns.Contains("colSpecialTrip")) return ids;
            foreach (DataGridViewRow r in gridItems.Rows)
                if (!r.IsNewRow && int.TryParse(r.Cells["colSpecialTrip"].Value?.ToString(), out int id) && id > 0) ids.Add(id);
            return ids;
        }
        /// <summary>
        /// Adds each special trip as one statement line: its date, Destination Address, truck, stops, trip charge and crew,
        /// with its trip report # when it has one. The line keeps a link to the trip.
        /// </summary>
        private void AppendSpecialTrips(List<SpecialTrip> trips)
        {
            var trucks = MJdatabase.GetTrucks();
            var links = MJdatabase.GetSpecialTripLinks();
            int trip = NextTripNumber();
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                foreach (var t in trips.OrderBy(x => x.DispatchDate).ThenBy(x => x.Id))
                {
                    if (SpecialTripIdsInTable().Contains(t.Id)) continue;
                    var trk = trucks.FirstOrDefault(k => k.Id == t.TruckId);
                    string plate = trk != null ? trk.PlateNumber : t.TruckPlate;
                    string tonnage = trk != null ? MJdatabase.TonnageLabel(trk) : "";
                    string driverHelper = $"{t.DriverName} / {t.AideName}".Trim(' ', '/');
                    string dest = string.IsNullOrWhiteSpace(t.DestinationAddress) ? t.Address : t.DestinationAddress;
                    int r = gridItems.Rows.Add(trip, t.DispatchDate.ToString("yyyy-MM-dd"), "", "", dest, "SPECIAL", plate, tonnage, Math.Max(1, t.TotalStopsCount), t.TripCharge, 0m, driverHelper, "");
                    gridItems.Rows[r].Cells["colTripReport"].Value = links.TryGetValue(t.Id, out var l) && l.HasReport ? l.TripReportNumber : t.OrderReference;
                    gridItems.Rows[r].Cells["colSpecialTrip"].Value = t.Id.ToString();
                    trip++;
                }
            }
            finally { isBindingForm = was; }
            SizeItemsGrid();
        }
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
            // Strict: a statement only takes trips of exactly its own client company.
            return c.Equals(client, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Re-lists the completed trip reports (saved, not voided) for the client and period on the form: the statement's
        /// source. Completed trips that still have no trip report are counted in the heading, not listed.
        /// </summary>
        private void RefreshPicker()
        {
            if (gridPicker == null || !pnlForm.Visible) return;
            try { deliveryCache = MJdatabase.GetDeliveries(); } catch { deliveryCache = new List<Delivery>(); }
            var reports = MJdatabase.GetTripReportsByDelivery();
            var src = new AutoCompleteStringCollection();
            src.AddRange(reports.Values.Select(t => t.TripReportNumber).Concat(deliveryCache.Select(d => d.DispatchNumber)).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToArray());
            txtAddDispatch.AutoCompleteCustomSource = src;
            string client = chkPickerAllClients.Checked ? "" : cbClient.Text.Trim();
            DateTime from = dtpPeriodFrom.Value.Date, to = dtpPeriodTo.Value.Date;
            var billed = MJdatabase.GetBilledDeliveryIds(isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0);
            foreach (var kv in MJdatabase.GetDirectlyInvoicedDeliveryIds()) billed.TryAdd(kv.Key, "invoice " + kv.Value); // billed directly: can't go on a statement too
            var inTable = DeliveryIdsInTable();
            var completed = deliveryCache
                .Where(d => string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                            && d.DispatchDate.Date >= from && d.DispatchDate.Date <= to && ClientMatches(d, client))
                .OrderBy(d => d.DispatchDate).ThenBy(d => d.Id).ToList();
            int needReport = completed.Count(d => !reports.ContainsKey(d.Id));
            pickerNeedReport = needReport;
            var rows = completed.Where(d => reports.ContainsKey(d.Id))
                .Select(d => new PickRow
                {
                    Delivery = d,
                    Report = reports[d.Id],
                    State = inTable.Contains(d.Id) ? PickState.OnThisStatement : billed.ContainsKey(d.Id) ? PickState.BilledElsewhere : PickState.Ready,
                    BilledOn = billed.TryGetValue(d.Id, out var soaNo) ? soaNo : ""
                }).ToList();
            var ticked = new HashSet<int>(gridPicker.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["pkPick"].Value is true && r.Tag is PickRow).Select(r => ((PickRow)r.Tag!).Delivery.Id));
            gridPicker.SuspendLayout();
            gridPicker.Rows.Clear();
            foreach (var pr in rows)
            {
                var d = pr.Delivery;
                var tr = pr.Report!;
                int stores = BuildTripLines(d, out _).Count;
                if (stores == 0) stores = d.TotalStopsCount > 0 ? d.TotalStopsCount : 1;
                int crates = tr.DeliveredCrates > 0 ? tr.DeliveredCrates : tr.DeliveredGCrates + tr.DeliveredECrates;
                decimal rate = RateFor(d);
                string state = pr.State switch
                {
                    PickState.OnThisStatement => "✓ On this statement",
                    PickState.BilledElsewhere => $"Billed on {pr.BilledOn}",
                    _ => "Ready to bill"
                };
                int i = gridPicker.Rows.Add(pr.State == PickState.Ready && ticked.Contains(d.Id), tr.TripReportNumber, d.DispatchDate.ToString("MMM d, yyyy"), d.DispatchNumber, d.ClientName, d.TruckInfo, stores,
                    crates.ToString("N0"), rate > 0 ? $"₱{rate:N2}" : "no rate", state);
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
            // Special trips ticked "With SOA" for this client and period: one line each (its destination and trip charge).
            int specialCount = 0;
            try
            {
                var links = MJdatabase.GetSpecialTripLinks();
                var inTableSpecial = SpecialTripIdsInTable();
                string thisSoa = isEditingExisting && selectedSoa != null ? selectedSoa.SoaNumber : "";
                var specials = MJdatabase.GetSpecialTrips()
                    .Where(t => t.WithSOA && !RowMenu.Is(t.Status, "Cancelled") && t.DispatchDate.Date >= from && t.DispatchDate.Date <= to
                                && (client.Length == 0 || string.Equals(t.Company?.Trim(), client, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(t => t.DispatchDate).ThenBy(t => t.Id).ToList();
                foreach (var t in specials)
                {
                    var l = links.TryGetValue(t.Id, out var x) ? x : new SpecialTripLinks();
                    bool done = RowMenu.Is(t.Status, "Completed");
                    var pr = new PickRow
                    {
                        Special = t,
                        Delivery = new Delivery { Id = -t.Id, DispatchNumber = t.OrderReference, ClientName = t.Company, DispatchDate = t.DispatchDate },
                        State = inTableSpecial.Contains(t.Id) ? PickState.OnThisStatement
                              : (l.OnSoa && !string.Equals(l.SoaNumber, thisSoa, StringComparison.OrdinalIgnoreCase)) ? PickState.BilledElsewhere : PickState.Ready,
                        BilledOn = l.SoaNumber
                    };
                    bool canTick = pr.State == PickState.Ready && done && t.TripCharge > 0;
                    string state = pr.State switch
                    {
                        PickState.OnThisStatement => "✓ On this statement",
                        PickState.BilledElsewhere => $"Billed on {pr.BilledOn}",
                        _ => !done ? $"Special trip · {t.Status} (not completed yet)" : t.TripCharge <= 0 ? "Special trip · no trip charge" : "Special trip · ready to bill"
                    };
                    int i = gridPicker.Rows.Add(canTick && ticked.Contains(-t.Id), l.HasReport ? l.TripReportNumber : "Special trip", t.DispatchDate.ToString("MMM d, yyyy"), t.OrderReference, t.Company,
                        string.IsNullOrWhiteSpace(t.TruckPlate) ? "—" : t.TruckPlate, Math.Max(1, t.TotalStopsCount), "—", t.TripCharge > 0 ? $"₱{t.TripCharge:N2}" : "no charge", state);
                    var row = gridPicker.Rows[i];
                    row.Tag = pr;
                    row.Cells["pkDispatch"].ToolTipText = "Destination: " + t.DestinationAddress;
                    if (!canTick)
                    {
                        row.Cells["pkPick"].ReadOnly = true;
                        row.DefaultCellStyle.ForeColor = AppTheme.TextMuted;
                        row.Cells["pkState"].Style.ForeColor = pr.State == PickState.OnThisStatement ? AppTheme.SuccessText : AppTheme.WarningText;
                    }
                    else row.Cells["pkState"].Style.ForeColor = AppTheme.InfoText;
                    rows.Add(pr);
                    specialCount++;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            gridPicker.ResumeLayout();
            pnlPickerHost.Height = Math.Clamp(Dpi(36) + Math.Max(3, rows.Count) * Dpi(32) + Dpi(4), Dpi(150), Dpi(420));
            int ready = rows.Count(r => r.State == PickState.Ready && (r.Special == null || (RowMenu.Is(r.Special.Status, "Completed") && r.Special.TripCharge > 0)));
            string who = client.Length == 0 ? "all clients" : client;
            string waiting = needReport > 0 ? $"  ·  {needReport} completed trip{(needReport == 1 ? " still needs its" : "s still need their")} trip report" : "";
            lblPickerInfo.Text = rows.Count == 0
                ? $"No completed trip reports for {who} from {from:MMM d} to {to:MMM d, yyyy}" + waiting
                : $"{rows.Count - specialCount} completed trip report{(rows.Count - specialCount == 1 ? "" : "s")}" + (specialCount > 0 ? $" and {specialCount} special trip{(specialCount == 1 ? "" : "s")}" : "") + $" for {who}  ·  {ready} ready to bill" + waiting;
            UpdatePickerButtons();
            ScheduleSoaGuide();
        }
        private void UpdatePickerButtons()
        {
            if (btnAddSelected == null) return;
            int n = gridPicker.Rows.Cast<DataGridViewRow>().Count(r => r.Cells["pkPick"].Value is true);
            btnAddSelected.Text = n > 0 ? $"＋ Add {n} ticked to statement" : "＋ Add ticked to statement";
            btnAddSelected.Enabled = n > 0;
            btnPickAllReady.Enabled = gridPicker.Rows.Cast<DataGridViewRow>().Any(r => r.Tag is PickRow pr && pr.State == PickState.Ready && !r.Cells["pkPick"].ReadOnly);
            if (btnRefreshFromTrips != null) btnRefreshFromTrips.Enabled = DeliveryIdsInTable().Count > 0;
        }
        private void TickAllReady()
        {
            bool anyUnticked = gridPicker.Rows.Cast<DataGridViewRow>().Any(r => r.Tag is PickRow pr && pr.State == PickState.Ready && !r.Cells["pkPick"].ReadOnly && r.Cells["pkPick"].Value is not true);
            foreach (DataGridViewRow r in gridPicker.Rows)
                if (r.Tag is PickRow pr && pr.State == PickState.Ready && !r.Cells["pkPick"].ReadOnly) r.Cells["pkPick"].Value = anyUnticked;
            btnPickAllReady.Text = anyUnticked ? "Untick all" : "Tick all ready";
            UpdatePickerButtons();
        }
        private void AddTickedDispatches()
        {
            var picked = gridPicker.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells["pkPick"].Value is true && r.Tag is PickRow pr && pr.State == PickState.Ready)
                .Select(r => (PickRow)r.Tag!).ToList();
            var specials = picked.Where(p => p.Special != null).Select(p => p.Special!).ToList();
            var list = picked.Where(p => p.Special == null).Select(p => p.Delivery).ToList();
            if (list.Count == 0 && specials.Count == 0) return;
            if (specials.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(cbClient.Text)) { cbClient.Text = specials[0].Company; AutoFillClientDetails(); }
                string owner = LinesClient();
                if (owner.Length == 0) owner = cbClient.Text.Trim();
                var foreign = specials.Where(t => !string.Equals(t.Company?.Trim(), owner, StringComparison.OrdinalIgnoreCase)).ToList();
                if (foreign.Count > 0)
                {
                    specials = specials.Except(foreign).ToList();
                    MessageBox.Show(this.FindForm(), $"Only trips of {owner} can go on this statement, so these special trips were not added:\n\n"
                        + string.Join("\n", foreign.Take(10).Select(t => $"• {t.OrderReference} — {t.Company}")), "Other client's trips", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (specials.Count > 0) AppendSpecialTrips(specials);
            }
            if (list.Count > 0) AddDeliveriesToStatement(list);
            else if (specials.Count == 0) return;
            else
            {
                RecalculateTaxes(manualOverride: false);
                RefreshPicker();
                ToastHelper.ShowToast(this, $"Added {specials.Count} special trip{(specials.Count == 1 ? "" : "s")} to the statement", ToastType.Success);
            }
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
            var reports = MJdatabase.GetTripReportsByDelivery();
            int trip = NextTripNumber();
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                foreach (var del in deliveries.OrderBy(d => d.DispatchDate).ThenBy(d => d.Id))
                {
                    var trk = trucks.FirstOrDefault(t => t.Id == del.TruckId);
                    string plate = trk != null ? trk.PlateNumber : del.TruckInfo;
                    string tonnage = trk != null ? MJdatabase.TonnageLabel(trk) : "6T";
                    string driverHelper = $"{del.DriverName} / {del.AideDisplay}".Trim(' ', '/');
                    var lines = BuildTripLines(del, out int dropCount);
                    string dateStr = del.DispatchDate.ToString("yyyy-MM-dd");
                    decimal rate = RateFor(del);
                    if (rate <= 0) unpriced++;
                    string did = del.Id.ToString();
                    string trNo = reports.TryGetValue(del.Id, out var rep0) ? rep0.TripReportNumber : "";
                    if (lines.Count > 0)
                    {
                        bool first = true;
                        foreach (var ln in lines)
                        {
                            int r = gridItems.Rows.Add(trip, dateStr, "", DrList.Join(ln.Drs), ln.Name, cbDeliveryType.Text, plate, tonnage, dropCount, first ? rate : 0m, 0m, driverHelper, did);
                            gridItems.Rows[r].Cells["colStoreCode"].Value = ln.Code;
                            gridItems.Rows[r].Cells["colTripReport"].Value = trNo;
                            gridItems.Rows[r].Cells["colCrates"].Value = ln.Crates;
                            first = false;
                        }
                    }
                    else
                    {
                        int r = gridItems.Rows.Add(trip, dateStr, "", "", del.Destination, cbDeliveryType.Text, plate, tonnage, dropCount, rate, 0m, driverHelper, did);
                        gridItems.Rows[r].Cells["colTripReport"].Value = trNo;
                        gridItems.Rows[r].Cells["colCrates"].Value = rep0 != null ? (rep0.DeliveredCrates > 0 ? rep0.DeliveredCrates : rep0.DeliveredGCrates + rep0.DeliveredECrates) : (del.TotalCrates > 0 ? del.TotalCrates : del.GCrate + del.ECrate);
                    }
                    trip++;
                }
            }
            finally
            {
                isBindingForm = was;
            }
            SizeItemsGrid();
            return unpriced;
        }
        /// <summary>One statement line: a store of the trip with all its DR numbers.</summary>
        private sealed class TripLine
        {
            public string Code = "";
            public string Name = "";
            public List<string> Drs = new List<string>();
            /// <summary>Total crates delivered to the store (G + E, every row of the store on the trip added up).</summary>
            public int Crates;
        }
        /// <summary>
        /// The statement lines of a trip, from its trip report's Stores &amp; crates table: one line per store code (a store
        /// listed twice on the trip becomes one line with all its DRs, none dropped), in the trip's order.
        /// <paramref name="drops"/> is the trip's drops: each store's "No. Drops" (1 when blank), added up.
        /// </summary>
        private static List<TripLine> BuildTripLines(Delivery del, out int drops)
        {
            var stops = MJdatabase.GetOrBuildTripStopsForDelivery(del).OrderBy(s => s.StopSequence).ToList();
            var lines = new List<TripLine>();
            foreach (var st in stops)
            {
                string code = (st.StoreCode ?? "").Trim(), name = (st.Destination ?? "").Trim();
                if (code.Length == 0 && name.Length == 0) continue;
                var line = code.Length > 0
                    ? lines.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                    : lines.FirstOrDefault(l => l.Code.Length == 0 && l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (line == null) lines.Add(line = new TripLine { Code = code, Name = name });
                else if (line.Name.Length == 0) line.Name = name;
                line.Drs = DrList.Merge(line.Drs, DrList.Parse(st.DrNumbers));
                int delivered = st.DeliveredGCrates + st.DeliveredECrates;
                line.Crates += delivered > 0 || st.DeliveredCrates > 0 ? Math.Max(delivered, st.DeliveredCrates) : st.GCrate + st.ECrate;
            }
            // Drops: each store's "No. Drops" (1 when blank). A store listed again on the same trip (another DR batch at
            // the same stop) only adds drops when a number was typed for that row.
            drops = 0;
            var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var st in stops)
            {
                string key = !string.IsNullOrWhiteSpace(st.StoreCode) ? "c:" + st.StoreCode.Trim() : "n:" + (st.Destination ?? "").Trim();
                if (counted.Add(key)) drops += MJdatabase.DropsOf(st);
                else if (int.TryParse((st.Remarks ?? "").Trim(), out int extra) && extra > 0 && extra < 1000) drops += extra;
            }
            if (stops.Count == 0) drops = del.TotalStopsCount > 0 ? del.TotalStopsCount : 1;
            return lines;
        }
        private static int RowDeliveryId(DataGridViewRow r) => int.TryParse(r.Cells["colDeliveryId"].Value?.ToString(), out int id) ? id : 0;
        private static bool SameStore(DataGridViewRow r, TripLine ln)
        {
            string code = (r.Cells["colStoreCode"].Value?.ToString() ?? "").Trim();
            string name = (r.Cells["colDest"].Value?.ToString() ?? "").Trim();
            if (code.Length > 0 && ln.Code.Length > 0) return code.Equals(ln.Code, StringComparison.OrdinalIgnoreCase);
            return name.Length > 0 && name.Equals(ln.Name, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// For trips already on the statement: re-reads store codes, names and DR numbers from their trip reports. Typed
        /// amounts, fuel surcharges and zones are kept; a store new on the trip report gets its own line; a line whose
        /// store is no longer on the trip report is kept and listed so the person can remove it. Never adds a trip twice.
        /// </summary>
        private void RefreshLinesFromTripReports()
        {
            gridItems.EndEdit();
            var ids = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(RowDeliveryId).Where(i => i > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                QuietBox.Show("There are no imported trips on this statement yet. Add completed trips first; lines typed by hand are not linked to a trip report.", "Refresh from trip reports", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var reported = MJdatabase.GetTripReportDeliveryIds();
            int changed = 0, added = 0;
            var stale = new List<string>();
            var noReport = new List<string>();
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                foreach (int id in ids)
                {
                    var del = MJdatabase.GetDeliveryById(id);
                    var rows = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && RowDeliveryId(r) == id).ToList();
                    if (del == null || rows.Count == 0) continue;
                    if (!reported.Contains(id)) { noReport.Add(del.DispatchNumber); continue; }
                    var lines = BuildTripLines(del, out int drops);
                    string trNo = MJdatabase.GetTripReportsByDelivery().TryGetValue(id, out var trRep) ? trRep.TripReportNumber : "";
                    var used = new HashSet<DataGridViewRow>();
                    var last = rows[rows.Count - 1];
                    foreach (var ln in lines)
                    {
                        string drText = DrList.Join(ln.Drs);
                        var row = rows.FirstOrDefault(r => !used.Contains(r) && SameStore(r, ln));
                        if (row == null)
                        {
                            var src = rows[0];
                            int at = last.Index + 1;
                            gridItems.Rows.Insert(at, src.Cells["colTripNo"].Value, src.Cells["colDate"].Value, src.Cells["colZone"].Value, drText, ln.Name,
                                src.Cells["colType"].Value, src.Cells["colPlate"].Value, src.Cells["colTons"].Value, drops, 0m, 0m, src.Cells["colDriver"].Value, id.ToString());
                            row = gridItems.Rows[at];
                            row.Cells["colStoreCode"].Value = ln.Code;
                            row.Cells["colTripReport"].Value = trNo;
                            row.Cells["colCrates"].Value = ln.Crates;
                            last = row;
                            added++;
                        }
                        else
                        {
                            bool diff = !string.Equals(DrList.Normalize(row.Cells["colDr"].Value?.ToString()), drText, StringComparison.Ordinal)
                                     || !string.Equals((row.Cells["colDest"].Value?.ToString() ?? "").Trim(), ln.Name, StringComparison.Ordinal)
                                     || !string.Equals((row.Cells["colStoreCode"].Value?.ToString() ?? "").Trim(), ln.Code, StringComparison.Ordinal)
                                     || !string.Equals((row.Cells["colTripReport"].Value?.ToString() ?? "").Trim(), trNo, StringComparison.Ordinal)
                                     || (row.Cells["colCrates"].Value?.ToString() ?? "").Trim() != ln.Crates.ToString();
                            if (diff) changed++;
                            row.Cells["colDr"].Value = drText;
                            row.Cells["colDest"].Value = ln.Name;
                            row.Cells["colStoreCode"].Value = ln.Code;
                            row.Cells["colTripReport"].Value = trNo;
                            row.Cells["colCrates"].Value = ln.Crates;
                        }
                        used.Add(row);
                    }
                    foreach (var r in gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && RowDeliveryId(r) == id))
                    {
                        if (!int.TryParse(r.Cells["colDrops"].Value?.ToString(), out int old) || old != drops) { r.Cells["colDrops"].Value = drops; if (used.Contains(r)) changed++; }
                        if (!used.Contains(r)) stale.Add($"{del.DispatchNumber}: {(r.Cells["colDest"].Value?.ToString() ?? "").Trim()}");
                    }
                }
            }
            finally
            {
                isBindingForm = was;
            }
            RecalculateTaxes(manualOverride: false);
            SizeItemsGrid();
            RefreshPicker();
            var msg = new List<string>();
            msg.Add(changed == 0 && added == 0 ? "The lines already match the trip reports." : $"{changed} line{(changed == 1 ? "" : "s")} updated, {added} new store line{(added == 1 ? "" : "s")} added. Amounts, fuel surcharges and zones were kept.");
            if (stale.Count > 0) msg.Add($"Not on the trip report any more (check and remove if wrong):\n• {string.Join("\n• ", stale.Take(10))}{(stale.Count > 10 ? $"\n• … and {stale.Count - 10} more" : "")}");
            if (noReport.Count > 0) msg.Add($"No trip report found for: {string.Join(", ", noReport)} (left as they are).");
            QuietBox.Show(string.Join("\n\n", msg), "Refresh from trip reports", MessageBoxButtons.OK, stale.Count > 0 || noReport.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
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
            // Relational lock: only trips of the statement's client (and of the client already on its lines).
            string owner = LinesClient();
            if (owner.Length == 0) owner = cbClient.Text.Trim();
            var foreign = fresh.Where(d => !string.Equals(d.ClientName?.Trim(), owner, StringComparison.OrdinalIgnoreCase)).ToList();
            if (foreign.Count > 0)
            {
                fresh = fresh.Except(foreign).ToList();
                MessageBox.Show(this.FindForm(), $"Only trips of {owner} can go on this statement, so {(foreign.Count == 1 ? "this one was" : "these were")} not added:\n\n"
                    + string.Join("\n", foreign.Take(10).Select(d => $"• {d.DispatchNumber} — {d.ClientName}")) + (foreign.Count > 10 ? $"\n• … and {foreign.Count - 10} more" : "")
                    + "\n\nMake a separate statement for each client.", "Other client's trips", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (fresh.Count == 0) return;
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
            // A trip report number or a dispatch number (exact first, then a unique partial match).
            var reportsByDel = MJdatabase.GetTripReportsByDelivery();
            Delivery? ById(int id) => deliveryCache.FirstOrDefault(d => d.Id == id);
            var del = reportsByDel.Where(kv => string.Equals(kv.Value.TripReportNumber, q, StringComparison.OrdinalIgnoreCase)).Select(kv => ById(kv.Key)).FirstOrDefault(d => d != null)
                      ?? deliveryCache.FirstOrDefault(d => string.Equals(d.DispatchNumber, q, StringComparison.OrdinalIgnoreCase));
            if (del == null)
            {
                var partial = reportsByDel.Where(kv => (kv.Value.TripReportNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).Select(kv => ById(kv.Key))
                    .Concat(deliveryCache.Where(d => (d.DispatchNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)))
                    .Where(d => d != null).Select(d => d!).GroupBy(d => d.Id).Select(g => g.First()).ToList();
                if (partial.Count == 1) del = partial[0];
                else
                {
                    lblAddDispatchResult.Text = partial.Count == 0 ? $"No trip report or dispatch matches \"{q}\"." : $"{partial.Count} trips match \"{q}\"; type more of the number.";
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
            foreach (var kv in MJdatabase.GetDirectlyInvoicedDeliveryIds()) billed.TryAdd(kv.Key, "invoice " + kv.Value); // billed directly: can't go on a statement too
            if (billed.TryGetValue(del.Id, out var soaNo))
            {
                lblAddDispatchResult.Text = $"{del.DispatchNumber} is already billed on {soaNo} — it can't be billed twice.";
                lblAddDispatchResult.ForeColor = AppTheme.WarningText;
                return;
            }
            if (!MJdatabase.HasTripReport(del.Id))
            {
                QuietBox.Show($"{del.DispatchNumber} has no trip report yet.\n\nThe statement lines (stores and DR numbers) are made from the trip report, so file it first in Operations → Trip reports, then add the trip here.", "Trip report needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblAddDispatchResult.Text = $"{del.DispatchNumber} needs a trip report first.";
                lblAddDispatchResult.ForeColor = AppTheme.WarningText;
                return;
            }
            var issues = new List<string>();
            string client = cbClient.Text.Trim();
            if (client.Length > 0 && !ClientMatches(del, client))
            {
                // Hard stop: another company's trip can never go on this statement.
                lblAddDispatchResult.Text = $"{del.DispatchNumber} belongs to {del.ClientName}, not {client} — it can't go on this statement.";
                lblAddDispatchResult.ForeColor = AppTheme.DangerText;
                bool canSwitch = DeliveryIdsInTable().Count == 0 && SpecialTripIdsInTable().Count == 0 && IsRegisteredClient(del.ClientName?.Trim() ?? "");
                if (canSwitch)
                {
                    var target = del;
                    LinkedChoicesPopover.ShowUnder(txtAddDispatch, $"{del.DispatchNumber} is a trip of {del.ClientName}. The statement has no trips yet — switch its client?",
                        new[] { del.ClientName!.Trim() }, v => { SetClientQuietly(v); txtAddDispatch.Text = target.DispatchNumber; AddDispatchByNumber(); });
                }
                return;
            }
            if (del.DispatchDate.Date < dtpPeriodFrom.Value.Date || del.DispatchDate.Date > dtpPeriodTo.Value.Date) issues.Add($"its date ({del.DispatchDate:MMM d}) is outside the period covered");
            if (issues.Count > 0)
            {
                var r = MessageBox.Show($"Add {del.DispatchNumber} anyway?\n\n• " + string.Join("\n• ", issues), "Add trip", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }
            AddDeliveriesToStatement(new List<Delivery> { del });
            string trNo = reportsByDel.TryGetValue(del.Id, out var addedReport) ? addedReport.TripReportNumber + " · " : "";
            lblAddDispatchResult.Text = $"Added {trNo}{del.DispatchNumber} · {del.CleanRouteDisplay} · {del.DispatchDate:MMM d}";
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
            var editable = gridItems.Columns.Cast<DataGridViewColumn>().Where(c => c is DataGridViewTextBoxColumn && c.Visible && !c.ReadOnly && c.Name != "colTripNo").OrderBy(c => c.DisplayIndex).ToList();
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
            else if (col.Name == "colDr") value = DrList.Normalize(raw);
            bool was = isBindingForm;
            isBindingForm = true;
            foreach (int i in rows) gridItems.Rows[i].Cells[col.Index].Value = value;
            isBindingForm = was;
            RecalculateTaxes(manualOverride: false);
            SizeItemsGrid();
            ToastHelper.ShowToast(this, $"Updated {col.HeaderText} on {rows.Count} row{(rows.Count == 1 ? "" : "s")}", ToastType.Success);
        }
    }
}
