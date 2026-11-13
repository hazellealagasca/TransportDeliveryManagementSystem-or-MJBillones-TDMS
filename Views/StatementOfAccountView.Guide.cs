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
    /// Payments guidance on the statement form: live totals strip, workflow steps, the validation checklist that
    /// gates Save, the client lock (lines must all belong to the statement's client), the adjustment reason for
    /// totals changed by hand, typeahead with badges and the "How this works" panel.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private Panel soaPageHeaderHost = null!;
        private PaymentSummaryStrip soaStrip = null!;
        private WorkflowStepper soaStepper = null!;
        private ValidationChecklist soaChecklist = null!;
        private HelpDrawer soaHelp = null!;
        private Label lblSaveBlocked = null!;
        private Panel pnlSoaAdjust = null!;
        private Label lblSoaVariance = null!;
        private TextBox txtSoaAdjustReason = null!;
        private RichLookup? clientLookup, addDispatchLookup;
        private readonly System.Windows.Forms.Timer soaGuideTimer = new System.Windows.Forms.Timer { Interval = 250 };
        private bool soaSaving;
        private string lastGoodClient = "";
        // Cached for the checks (reloaded when the form opens and when trips are added).
        private Dictionary<int, Delivery> guideDeliveries = new Dictionary<int, Delivery>();
        private Dictionary<int, TripReport> guideReports = new Dictionary<int, TripReport>();
        private Dictionary<int, string> guideBilled = new Dictionary<int, string>();
        private HashSet<int> guideReported = new HashSet<int>();
        private Dictionary<int, string> guideDirect = new Dictionary<int, string>();
        private List<TripInvoice> guideTripInvoices = new List<TripInvoice>();
        private List<Invoice> guideInvoices = new List<Invoice>();
        private Dictionary<int, SpecialTrip> guideSpecials = new Dictionary<int, SpecialTrip>();
        private decimal guidePaid;
        public const int MinAdjustReasonLength = 10;

        /// <summary>Builds the reason box shown under the totals when they no longer match the trip lines.</summary>
        private Panel BuildSoaAdjustPanel()
        {
            pnlSoaAdjust = new Panel { Height = Dpi(96), BackColor = AppTheme.Surface, Visible = false, Padding = new Padding(0, Dpi(6), 0, 0) };
            lblSoaVariance = new Label { Dock = DockStyle.Top, Height = Dpi(40), Font = new Font("Segoe UI Semibold", 9.25F), ForeColor = AppTheme.WarningText, UseMnemonic = false };
            txtSoaAdjustReason = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F), MaxLength = 300, PlaceholderText = $"Adjustment reason (required, at least {MinAdjustReasonLength} characters) — e.g. agreed discount for late delivery" };
            txtSoaAdjustReason.TextChanged += (s, e) => ScheduleSoaGuide();
            pnlSoaAdjust.Controls.Add(txtSoaAdjustReason);
            pnlSoaAdjust.Controls.Add(lblSoaVariance);
            return pnlSoaAdjust;
        }

        /// <summary>Called once after the form controls exist.</summary>
        private void InitSoaGuide()
        {
            // Compact page header: smaller title row, back button and title on one line.
            pnlFormHeaderBar.Height = Dpi(44);
            lblFormHeader.Font = new Font("Segoe UI Semibold", 13F);
            btnCloseForm.Width = Dpi(176);
            btnCloseForm.Font = new Font("Segoe UI Semibold", 9F);
            // Sticky totals strip right under the header (outside the scrolling body).
            soaStrip = new PaymentSummaryStrip { Cursor = Cursors.Hand };
            soaStrip.Click += (s, e) => { try { pnlFormBody.ScrollControlIntoView(soaChecklist); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            new ToolTip().SetToolTip(soaStrip, "Live totals of this statement. Click to see the validation checklist.");
            pnlForm.SuspendLayout();
            pnlForm.Controls.Add(soaStrip);
            // dock order: header first (top), then the strip under it, then the body
            pnlForm.Controls.SetChildIndex(pnlFormBody, 0);
            pnlForm.Controls.SetChildIndex(soaStrip, pnlForm.Controls.GetChildIndex(pnlFormHeaderBar));
            pnlForm.ResumeLayout(true);
            // Steps + help at the top of the body, checklist at the bottom (just above the Save bar).
            soaStepper = new WorkflowStepper("Trip delivered", "Service invoice made", "Invoices picked", "Review", "Statement ready");
            soaHelp = new HelpDrawer("How this works",
                "1. Complete the dispatch, then file its trip report (Operations → Trip reports).\n" +
                "2. Pick the client and period: the completed trip reports for that client appear in step 2. ⚡ Add all ready trips fills the lines, amounts and DR numbers for you.\n" +
                "3. Only trips of the statement's client can be added, and a trip can be billed once (on one statement or one direct invoice).\n" +
                "4. The totals are worked out from the lines. If you change one by hand, say why in Adjustment reason — it is kept with the statement and in the audit log.\n" +
                "5. The checklist at the bottom must be all green before Save. The saved statement is Unpaid until \u201cProceed to Collection Receipt\u201d records its full payment in one receipt (no part payments).");
            soaChecklist = new ValidationChecklist();
            pnlFormBody.SuspendLayout();
            var spacerTop = CreateSectionSpacer();
            spacerTop.Height = Dpi(8);
            pnlFormBody.Controls.Add(spacerTop);
            pnlFormBody.Controls.Add(soaHelp);
            pnlFormBody.Controls.Add(soaStepper);
            var spacerBottom = CreateSectionSpacer();
            pnlFormBody.Controls.Add(soaChecklist);
            pnlFormBody.Controls.Add(spacerBottom);
            pnlFormBody.Controls.SetChildIndex(soaChecklist, 0);
            pnlFormBody.Controls.SetChildIndex(spacerBottom, 1);
            pnlFormBody.ResumeLayout(true);
            // "n to fix" note left of the buttons in the Save bar.
            lblSaveBlocked = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9.25F), ForeColor = AppTheme.DangerText, Margin = new Padding(0, Dpi(11), Dpi(12), 0), Cursor = Cursors.Hand, UseMnemonic = false };
            lblSaveBlocked.Click += (s, e) => { try { pnlFormBody.ScrollControlIntoView(soaChecklist); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            flowFormActions.Controls.Add(lblSaveBlocked);
            // Money boxes: no minus sign, 2 decimals.
            foreach (var n in new[] { numTotalSales, numVat, numNetOfVat, numFuelSurchargeTotal, numWithholdingTax, numTotalAmountDue }) InputGuard.MoneyOnly(n);
            InputGuard.ReferenceOnly(txtSalesInvoiceNo, 30);
            // Re-check as the form changes.
            soaGuideTimer.Tick += (s, e) => { soaGuideTimer.Stop(); UpdateSoaGuide(); };
            cbClient.TextChanged += (s, e) => ScheduleSoaGuide();
            cbClient.Leave += (s, e) => EnforceClientLock();
            cbClient.SelectionChangeCommitted += (s, e) => BeginInvoke(new Action(EnforceClientLock));
            foreach (var d in new[] { dtpPeriodFrom, dtpPeriodTo, dtpIssueDate }) d.ValueChanged += (s, e) => ScheduleSoaGuide();
            gridItems.CellValueChanged += (s, e) => ScheduleSoaGuide();
            gridItems.RowsAdded += (s, e) => ScheduleSoaGuide();
            gridItems.RowsRemoved += (s, e) => ScheduleSoaGuide();
            foreach (var n in new[] { numTotalSales, numVat, numNetOfVat, numFuelSurchargeTotal, numWithholdingTax, numTotalAmountDue }) n.ValueChanged += (s, e) => ScheduleSoaGuide();
            txtSalesInvoiceNo.TextChanged += (s, e) => ScheduleSoaGuide();
            // Typeahead with badges.
            clientLookup = RichLookup.Attach(cbClient, ClientSuggestions, it => { AutoFillClientDetails(); EnforceClientLock(); SchedulePickerRefresh(); ScheduleSoaGuide(); });
            addDispatchLookup = RichLookup.Attach(txtAddDispatch, TripSuggestions, it => AddDispatchByNumber());
        }
        private void ScheduleSoaGuide()
        {
            if (soaGuideTimer == null || pnlForm == null || !pnlForm.Visible || soaViewOnly) return;
            soaGuideTimer.Stop();
            soaGuideTimer.Start();
        }

        /// <summary>Form opened: reload what the checks need and show the guide.</summary>
        private void OnSoaFormOpened()
        {
            if (soaPageHeaderHost != null) soaPageHeaderHost.Visible = false;
            if (soaReadyBanner != null) soaReadyBanner.Visible = false;
            soaSaving = false;
            btnSaveSoa.Enabled = true;
            if (!keep.Resuming) txtSoaAdjustReason.Text = isEditingExisting && selectedSoa != null ? MJdatabase.GetAdjustmentReason("StatementsOfAccount", selectedSoa.Id) : "";
            LoadSoaGuideCache();
            lastGoodClient = cbClient.Text.Trim();
            UpdateSoaGuide();
        }
        private void OnSoaFormClosed()
        {
            soaGuideTimer.Stop();
            if (soaPageHeaderHost != null) soaPageHeaderHost.Visible = true;
            if (soaReadyBanner != null) { soaReadyBanner.Visible = true; RefreshSoaReadyBanner(); }
            clientLookup?.HideList();
            addDispatchLookup?.HideList();
        }
        private void LoadSoaGuideCache()
        {
            try
            {
                guideDeliveries = MJdatabase.GetDeliveries().GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
                guideReports = MJdatabase.GetTripReportsByDelivery();
                guideReported = MJdatabase.GetTripReportDeliveryIds();
                guideBilled = MJdatabase.GetBilledDeliveryIds(CurrentSoaId);
                guideDirect = new Dictionary<int, string>(); // Nov 6: a trip's own invoice is required, not a block
                guideTripInvoices = MJdatabase.GetTripInvoices();
                guideInvoices = MJdatabase.GetInvoices();
                guideSpecials = MJdatabase.GetSpecialTrips().GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());
                guideReadyClients = ReadyClients();
                guidePaid = 0;
                if (CurrentSoaId > 0)
                    foreach (var inv in guideInvoices.Where(i => i.SoaId == CurrentSoaId)) guidePaid += MJdatabase.GetTotalPaidForInvoice(inv.Id);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        private bool IsRegisteredClient(string name) => name.Length > 0 && cbClient.Items.Cast<object>().Any(o => string.Equals(o?.ToString()?.Trim(), name, StringComparison.OrdinalIgnoreCase));
        /// <summary>The client every linked line on the statement belongs to ("" when there are none, or they disagree).</summary>
        private string LinesClient()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow r in gridItems.Rows)
            {
                if (r.IsNewRow) continue;
                int did = RowDeliveryId(r);
                if (did > 0 && guideDeliveries.TryGetValue(did, out var d) && !string.IsNullOrWhiteSpace(d.ClientName)) names.Add(d.ClientName.Trim());
                int sid = gridItems.Columns.Contains("colSpecialTrip") && int.TryParse(r.Cells["colSpecialTrip"].Value?.ToString(), out int x) ? x : 0;
                if (sid > 0 && guideSpecials.TryGetValue(sid, out var t) && !string.IsNullOrWhiteSpace(t.Company)) names.Add(t.Company.Trim());
            }
            return names.Count == 1 ? names.First() : "";
        }
        /// <summary>
        /// Relational lock: once the statement has trips, its client is the trips' client. Typing another company is
        /// put back, and a company that isn't registered gets a popover with the companies that can be used.
        /// </summary>
        private void EnforceClientLock()
        {
            if (isBindingForm || !pnlForm.Visible) return;
            string typed = cbClient.Text.Trim();
            string locked = LinesClient();
            if (locked.Length > 0 && !string.Equals(typed, locked, StringComparison.OrdinalIgnoreCase))
            {
                SetClientQuietly(locked);
                LinkedChoicesPopover.ShowUnder(cbClient, $"This statement already has trips of {locked}, so its client stays {locked}. Remove those lines first to bill another company.",
                    new[] { locked }, v => { SetClientQuietly(v); ScheduleSoaGuide(); });
                return;
            }
            if (typed.Length > 0 && !IsRegisteredClient(typed))
            {
                var ready = guideReadyClients;
                var choices = ready.Select(r => r.Client).Concat(cbClient.Items.Cast<object>().Select(o => o.ToString() ?? "")
                        .Where(n => n.Contains(typed, StringComparison.OrdinalIgnoreCase) || typed.Contains(n, StringComparison.OrdinalIgnoreCase)))
                    .Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
                LinkedChoicesPopover.ShowUnder(cbClient, $"\"{typed}\" is not a registered client company. Pick one of these (companies with ready trip reports first):",
                    choices, v => { SetClientQuietly(v); AutoFillClientDetails(); lastGoodClient = v; SchedulePickerRefresh(); ScheduleSoaGuide(); });
                return;
            }
            lastGoodClient = typed;
        }
        private void SetClientQuietly(string name)
        {
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                if (clientLookup != null) clientLookup.SetTextQuietly(name); else cbClient.Text = name;
                int i = cbClient.FindStringExact(name);
                if (i >= 0) cbClient.SelectedIndex = i;
            }
            finally { isBindingForm = was; }
            AutoFillClientDetails();
            SchedulePickerRefresh();
        }
        private List<(string Client, int Trips, DateTime From, DateTime To)> guideReadyClients = new();
        /// <summary>Clients with completed, saved trip reports not on any statement or invoice yet (read from the database).</summary>
        internal static List<(string Client, int Trips, DateTime From, DateTime To)> ReadyClients()
        {
            var list = new List<(string, int, DateTime, DateTime)>();
            try
            {
                var dels = MJdatabase.GetDeliveries();
                var reported = MJdatabase.GetTripReportDeliveryIds();
                var billed = MJdatabase.GetBilledDeliveryIds(0);
                var openInvoiceTrips = new HashSet<int>(MJdatabase.GetTripInvoices().Where(i => i.Open && i.SoaId == 0 && i.DeliveryId > 0).Select(i => i.DeliveryId)); // Nov 6: needs its service invoice
                foreach (var g in dels.Where(d => DispatchGate.IsCompleted(d.Status) && reported.Contains(d.Id) && !billed.ContainsKey(d.Id) && openInvoiceTrips.Contains(d.Id) && !string.IsNullOrWhiteSpace(d.ClientName))
                                      .GroupBy(d => d.ClientName.Trim(), StringComparer.OrdinalIgnoreCase))
                    list.Add((g.Key, g.Count(), g.Min(d => d.DispatchDate.Date), g.Max(d => d.DispatchDate.Date)));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return list.OrderByDescending(x => x.Item2).ToList();
        }
        private IEnumerable<LookupItem> ClientSuggestions(string q)
        {
            var ready = guideReadyClients.ToDictionary(r => r.Client, r => r, StringComparer.OrdinalIgnoreCase);
            string locked = LinesClient();
            foreach (var o in cbClient.Items.Cast<object>())
            {
                string n = o?.ToString() ?? "";
                if (!n.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                if (locked.Length > 0 && !string.Equals(n, locked, StringComparison.OrdinalIgnoreCase))
                    yield return new LookupItem(n, $"Locked: this statement has trips of {locked}", "Other client", BadgeKind.Bad, Enabled: false);
                else if (ready.TryGetValue(n, out var r))
                    yield return new LookupItem(n, $"{r.Trips} service invoice{(r.Trips == 1 ? "" : "s")} ready · {r.From:MMM d} – {r.To:MMM d, yyyy}", $"{r.Trips} ready", BadgeKind.Good);
                else yield return new LookupItem(n, "No service invoices ready for a statement", "Nothing ready", BadgeKind.Neutral);
            }
        }
        private IEnumerable<LookupItem> TripSuggestions(string q)
        {
            string client = cbClient.Text.Trim();
            var inTable = DeliveryIdsInTable();
            int shown = 0;
            foreach (var kv in guideReports)
            {
                if (!guideDeliveries.TryGetValue(kv.Key, out var d)) continue;
                string tr = kv.Value.TripReportNumber ?? "";
                if (!tr.Contains(q, StringComparison.OrdinalIgnoreCase) && !(d.DispatchNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                string detail = $"{d.DispatchNumber} · {d.ClientName} · {d.DispatchDate:MMM d, yyyy} · {d.TruckInfo}";
                LookupItem it;
                if (inTable.Contains(d.Id)) it = new LookupItem(tr, detail, "On this statement", BadgeKind.Info, false);
                else if (!guideReported.Contains(d.Id)) it = new LookupItem(tr, detail, "Report voided", BadgeKind.Bad, false);
                else if (!DispatchGate.IsCompleted(d.Status)) it = new LookupItem(tr, detail, d.Status, BadgeKind.Warn, false);
                else if (guideBilled.TryGetValue(d.Id, out var soaNo)) it = new LookupItem(tr, detail, "Billed · " + soaNo, BadgeKind.Bad, false);
                else if (guideDirect.ContainsKey(d.Id)) it = new LookupItem(tr, detail, "Invoiced directly", BadgeKind.Bad, false);
                else if (client.Length > 0 && !string.Equals(d.ClientName?.Trim(), client, StringComparison.OrdinalIgnoreCase)) it = new LookupItem(tr, detail, "Other client", BadgeKind.Bad, false);
                else it = new LookupItem(tr, detail, "Ready", BadgeKind.Good);
                yield return it;
                if (++shown >= 30) yield break;
            }
        }

        private (decimal ExpectedDue, decimal ExpectedSales) ExpectedSoaTotals()
        {
            var inv = InvoiceTotalsOfLines();
            bool oldRule = SoaShowsOldRule();   // a statement saved before Nov 12 is judged by the rule it was saved under (its amounts stay as saved)
            if (inv != null) { var it = oldRule ? SoaTotals.OldFrom(inv.Value.Freight, inv.Value.Fuel) : SoaTotals.From(inv.Value.Freight, inv.Value.Fuel); return (it.Due, it.TotalSales); } // follows the service invoices
            decimal amt = 0, fuel = 0;
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if (row.IsNewRow) continue;
                decimal.TryParse(row.Cells["colAmount"].Value?.ToString(), out var a);
                decimal.TryParse(row.Cells["colFuel"].Value?.ToString(), out var f);
                amt += a; fuel += f;
            }
            var lt = oldRule ? SoaTotals.OldFrom(amt, fuel) : SoaTotals.From(amt, fuel);
            return (lt.Due, lt.TotalSales);
        }
        /// <summary>True when the totals no longer match what the trip lines work out to (so a reason is needed).</summary>
        private bool SoaTotalsAdjusted(out decimal expected, out decimal entered)
        {
            var (due, _) = ExpectedSoaTotals();
            expected = due;
            entered = numTotalAmountDue.Value;
            return gridItems.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow) && Math.Abs(entered - expected) > 0.05m;
        }

        internal List<PaymentCheck> ComputeSoaChecks()
        {
            var checks = new List<PaymentCheck>();
            string client = cbClient.Text.Trim();
            var rows = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            checks.Add(new PaymentCheck(client.Length == 0 ? "Choose the client company" : IsRegisteredClient(client) ? $"Client {client} is registered" : $"\"{client}\" is not a registered client company", IsRegisteredClient(client)));
            checks.Add(new PaymentCheck(rows.Count == 0 ? "Add at least one completed trip report" : $"{rows.Select(r => r.Cells["colTripNo"].Value?.ToString()).Distinct().Count()} trip(s) on the statement", rows.Count > 0));
            checks.Add(new PaymentCheck(dtpPeriodTo.Value.Date >= dtpPeriodFrom.Value.Date ? "Period dates are in order" : "Period ends before it starts", dtpPeriodTo.Value.Date >= dtpPeriodFrom.Value.Date));
            bool issueOk = dtpIssueDate.Value.Date <= DateTime.Today;
            checks.Add(new PaymentCheck(issueOk ? "Statement date is not in the future" : $"Statement date {dtpIssueDate.Value:MMM d, yyyy} is in the future", issueOk));
            // Every line comes from a completed trip report of this client, not billed anywhere else.
            int unlinked = 0, notDone = 0, otherClient = 0, billedElsewhere = 0, dateOrder = 0, reportBeforeDispatch = 0;
            var otherNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                int did = RowDeliveryId(r);
                int sid = gridItems.Columns.Contains("colSpecialTrip") && int.TryParse(r.Cells["colSpecialTrip"].Value?.ToString(), out int x) ? x : 0;
                if (did <= 0 && sid <= 0) { unlinked++; continue; }
                if (did > 0)
                {
                    if (!guideDeliveries.TryGetValue(did, out var d)) { unlinked++; continue; }
                    if (!DispatchGate.IsCompleted(d.Status) || !guideReported.Contains(did)) notDone++;
                    if (client.Length > 0 && !string.Equals(d.ClientName?.Trim(), client, StringComparison.OrdinalIgnoreCase)) { otherClient++; otherNames.Add(d.ClientName ?? ""); }
                    if (guideBilled.ContainsKey(did)) billedElsewhere++;
                    if (d.DispatchDate.Date > dtpIssueDate.Value.Date) dateOrder++;
                    if (guideReports.TryGetValue(did, out var tr))
                    {
                        if (tr.ReportDate.Date > dtpIssueDate.Value.Date) dateOrder++;
                        if (tr.ReportDate.Date < d.DispatchDate.Date) reportBeforeDispatch++;
                    }
                }
                else if (guideSpecials.TryGetValue(sid, out var t))
                {
                    if (client.Length > 0 && !string.Equals(t.Company?.Trim(), client, StringComparison.OrdinalIgnoreCase)) { otherClient++; otherNames.Add(t.Company ?? ""); }
                    if (t.DispatchDate.Date > dtpIssueDate.Value.Date) dateOrder++;
                }
            }
            if (rows.Count > 0)
            {
                bool unlinkedBlocks = !isEditingExisting;
                checks.Add(unlinked == 0 ? new PaymentCheck("Every line comes from a trip report", true)
                    : new PaymentCheck($"{unlinked} line(s) not linked to a trip report" + (unlinkedBlocks ? " — add trips from step 2 instead of typing them" : " (older lines, saved before linking)"), !unlinkedBlocks, Warning: !unlinkedBlocks));
                checks.Add(new PaymentCheck(notDone == 0 ? "All trips completed with a saved trip report" : $"{notDone} line(s) whose trip is not completed or whose report was voided", notDone == 0));
                checks.Add(new PaymentCheck(otherClient == 0 ? $"All trips belong to {(client.Length > 0 ? client : "the client")}" : $"{otherClient} line(s) belong to {string.Join(", ", otherNames)} — not {client}", otherClient == 0));
                checks.Add(new PaymentCheck(billedElsewhere == 0 ? "No trip is on another statement" : $"{billedElsewhere} line(s) already billed on another statement or invoice", billedElsewhere == 0));
                checks.Add(new PaymentCheck(dateOrder == 0 ? "Dates in order: dispatch ≤ trip report ≤ statement date" : $"{dateOrder} trip date(s) are after the statement date ({dtpIssueDate.Value:MMM d, yyyy})", dateOrder == 0));
                if (reportBeforeDispatch > 0) checks.Add(new PaymentCheck($"{reportBeforeDispatch} trip report(s) dated before their dispatch — check the report dates", true, Warning: true));
                // Each trip priced; DRs not repeated on the statement.
                var tripAmt = new Dictionary<string, decimal>();
                var drSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int repeatedDr = 0;
                foreach (var r in rows)
                {
                    string trip = r.Cells["colTripNo"].Value?.ToString() ?? "";
                    decimal.TryParse(r.Cells["colAmount"].Value?.ToString(), out var a);
                    tripAmt[trip] = (tripAmt.TryGetValue(trip, out var v) ? v : 0) + a;
                    foreach (var dr in DrList.Parse(r.Cells["colDr"].Value?.ToString())) if (!drSeen.Add(dr)) repeatedDr++;
                }
                int unpriced = tripAmt.Count(kv => kv.Value <= 0);
                checks.Add(new PaymentCheck(unpriced == 0 ? "Every trip has an amount" : $"{unpriced} trip(s) without an amount", unpriced == 0));
                checks.Add(new PaymentCheck(repeatedDr == 0 ? "No DR number repeated" : $"{repeatedDr} DR number(s) repeated on this statement", repeatedDr == 0));
            }
            // Math (Nov 12): the fuel surcharge is inside Total Sales, so total sales − withholding tax = total due (= net of VAT + VAT − withholding tax).
            // A statement saved before Nov 12 (fuel outside the VAT) adds up the old way: net + fuel + VAT - withholding = due. Its saved amounts are never changed.
            bool oldMath = SoaShowsOldRule();
            decimal calc = oldMath ? numNetOfVat.Value + numFuelSurchargeTotal.Value + numVat.Value - numWithholdingTax.Value : numTotalSales.Value - numWithholdingTax.Value;
            bool mathOk = Math.Abs(calc - numTotalAmountDue.Value) <= 0.01m;
            string mathLabel = oldMath ? "net of VAT + fuel + VAT − withholding tax" : "total sales − withholding tax";
            checks.Add(new PaymentCheck(mathOk ? $"Totals add up ({mathLabel} = total due)" : $"Totals don't add up: {mathLabel} = ₱{calc:N2}, not ₱{numTotalAmountDue.Value:N2}", mathOk));
            checks.Add(new PaymentCheck(numTotalAmountDue.Value > 0 ? "Total amount due is more than ₱0.00" : "Total amount due is ₱0.00", numTotalAmountDue.Value > 0));
            if (SoaTotalsAdjusted(out var expected, out var entered))
            {
                bool hasReason = txtSoaAdjustReason.Text.Trim().Length >= MinAdjustReasonLength;
                checks.Add(new PaymentCheck(hasReason ? $"Adjustment reason given (₱{entered - expected:+#,##0.00;-#,##0.00})" : "Totals changed by hand — write the adjustment reason", hasReason));
            }
            if (!IsLegacySoa()) { AddInvoiceChecks(checks, rows); return checks; } // Nov 6: the statement groups its trips' service invoices
            // Statements saved before Nov 6 (their own SI invoice): the old checks.
            string si = txtSalesInvoiceNo.Text.Trim();
            if (si.Length == 0) checks.Add(new PaymentCheck("No SI no. yet — no billing invoice will be made until it is added", true, Warning: true));
            else
            {
                string invNum = si.StartsWith("SI", StringComparison.OrdinalIgnoreCase) ? si : $"SI-{si}";
                var clash = guideInvoices.FirstOrDefault(i => i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase) && (CurrentSoaId == 0 || i.SoaId != CurrentSoaId));
                checks.Add(new PaymentCheck(clash == null ? $"SI no. {si} is free" : $"SI no. {si} is already used by invoice {clash.InvoiceNumber} ({clash.ClientName})", clash == null));
            }
            if (guidePaid > 0)
            {
                bool ok = numTotalAmountDue.Value >= guidePaid;
                checks.Add(new PaymentCheck(ok ? $"Total covers the ₱{guidePaid:N2} already collected" : $"Total ₱{numTotalAmountDue.Value:N2} is less than the ₱{guidePaid:N2} already collected", ok));
            }
            return checks;
        }

        private void UpdateSoaGuide()
        {
            if (soaChecklist == null || !pnlForm.Visible) return;
            // Cache misses (a trip just added): reload once.
            if (DeliveryIdsInTable().Any(id => id > 0 && !guideDeliveries.ContainsKey(id))) LoadSoaGuideCache();
            var checks = ComputeSoaChecks();
            soaChecklist.SetChecks(checks);
            int fails = soaChecklist.Failed, warns = soaChecklist.Warnings;
            decimal due = numTotalAmountDue.Value;
            // Sales + fuel: the new rule already has the fuel inside Total Sales; a statement saved under the old rule has it outside, so it is added. Total due is the statement's own box.
            bool oldRule = SoaShowsOldRule();
            soaStrip.SetTotals(numTotalSales.Value + (oldRule ? numFuelSurchargeTotal.Value : 0m), numWithholdingTax.Value, guidePaid, Math.Max(0, due - guidePaid), fails, warns, soaViewOnly ? "View only" : null, due);
            UpdateSoaTape();
            if (soaViewOnly) { sheetTotals?.Relayout(); return; }   // a view-only statement shows its figures; the steps and the adjustment box are for editing
            // Steps
            int rows = gridItems.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
            int ready = gridPicker == null ? 0 : gridPicker.Rows.Cast<DataGridViewRow>().Count(r => r.Tag is PickRow pr && pr.State == PickState.Ready && !r.Cells["pkPick"].ReadOnly);
            if (rows == 0)
                soaStepper.SetProgress(ready > 0 ? 1 : 0, ready > 0
                    ? $"{ready} service invoice{(ready == 1 ? " is" : "s are")} ready for {cbClient.Text.Trim()} — tick them in step 2 or use ⚡ Add all ready trips."
                    : string.IsNullOrWhiteSpace(cbClient.Text) ? "Choose the client company and the period to see its service invoices that are ready."
                    : $"No service invoices are ready for {cbClient.Text.Trim()} in this period. Make the trip's service invoice first (Billing), or widen the period.");
            else if (fails > 0) soaStepper.SetProgress(2, $"Review: {fails} item{(fails == 1 ? "" : "s")} to fix — see the checklist at the bottom.");
            else soaStepper.SetProgress(4, warns > 0 ? $"✓ Validated — {warns} note{(warns == 1 ? "" : "s")} to review, then save." : "✓ Validated — Save makes the statement of these service invoices. Collect it with one receipt.");
            // Variance hint + reason box
            bool adjusted = SoaTotalsAdjusted(out var expected, out var entered);
            if (pnlSoaAdjust.Visible != adjusted) pnlSoaAdjust.Visible = adjusted;
            if (adjusted)
            {
                decimal diff = entered - expected;
                lblSoaVariance.Text = $"Trip-report total: ₱{expected:N2}  →  Current total due: ₱{entered:N2}  ({(diff >= 0 ? "+" : "−")}₱{Math.Abs(diff):N2}).  An adjustment reason is required below.";
                FieldMarks.Mark(txtSoaAdjustReason, txtSoaAdjustReason.Text.Trim().Length >= MinAdjustReasonLength ? FieldState.Ok : FieldState.Bad);
            }
            // Field colours
            string client = cbClient.Text.Trim();
            FieldMarks.Mark(cbClient, client.Length == 0 ? FieldState.None : checks.Any(c => !c.Passed && (c.Text.Contains("registered") || c.Text.Contains("belong to"))) ? FieldState.Bad : FieldState.Ok);
            FieldMarks.Mark(txtSalesInvoiceNo, txtSalesInvoiceNo.Text.Trim().Length == 0 ? FieldState.Warn : checks.Any(c => !c.Passed && c.Text.StartsWith("SI no.")) ? FieldState.Bad : FieldState.Ok);
            FieldMarks.Mark(numTotalAmountDue, checks.Any(c => !c.Passed && (c.Text.StartsWith("Totals") || c.Text.StartsWith("Total amount"))) ? FieldState.Bad : FieldState.None);
            // Save gate
            btnSaveSoa.Enabled = fails == 0 && !soaSaving;
            lblSaveBlocked.Text = fails > 0 ? $"✗ {fails} to fix before saving" : "";
            lblSaveBlocked.Visible = fails > 0;
        }
            // ------------------------------------------------------------------ Ready for a statement (list page)
        private StatementReadyPanel soaReadyBanner = null!;
        /// <summary>The list above the statements: every service invoice that is ready for a statement. Tick several (or Select all) of one client to make one statement.</summary>
        private void BuildSoaReadyBanner()
        {
            soaReadyBanner = new StatementReadyPanel { Height = Dpi(190), Padding = new Padding(Dpi(16), Dpi(2), Dpi(16), Dpi(8)) };
            soaReadyBanner.StatementRequested += (client, rows) => StartSoaFor(client, rows);
            Controls.Add(soaReadyBanner);
            Controls.SetChildIndex(soaReadyBanner, Controls.GetChildIndex(soaPageHeaderHost));
        }
        private void RefreshSoaReadyBanner() => soaReadyBanner?.RefreshData();

        /// <summary>"Make statement" from the ready list: the chosen invoices of one client, with the period of their trips.</summary>
        public void StartSoaFor(string client, List<ReadyInvoiceRow> rows)
        {
            if (rows.Count == 0) return;
            StartSoaFor(client, rows.Min(r => r.TripDate).Date, rows.Max(r => r.TripDate).Date, new HashSet<int>(rows.Select(r => r.InvoiceId)));
        }

        /// <summary>
        /// Opens a new statement for the client with the period of its waiting invoices and those invoices already added (amounts, stores and DR
        /// numbers from the trip reports). <paramref name="onlyInvoiceIds"/> limits it to the ticked invoices. Nothing is saved until Save.
        /// </summary>
        public void StartSoaFor(string client, DateTime from, DateTime to, HashSet<int>? onlyInvoiceIds = null)
        {
            if (!Permissions.Can("Payments", "StatementOfAccount")) { Permissions.TellDenied(this, "Payments", "StatementOfAccount"); return; }
            OpenNewStatement();
            if (chkPickerAllClients != null) chkPickerAllClients.Checked = false;
            SetClientQuietly(client);
            dtpPeriodFrom.SetSafe(from.Date);
            dtpPeriodTo.SetSafe(to.Date);
            pickerRefreshTimer.Stop();
            RefreshPicker();
            int ticked = 0;
            foreach (DataGridViewRow r in gridPicker.Rows)
                if (r.Tag is PickRow pr && pr.State == PickState.Ready && !r.Cells["pkPick"].ReadOnly
                    && (onlyInvoiceIds == null || onlyInvoiceIds.Contains(pr.Invoice != null ? pr.Invoice.Id : pr.Delivery.Id))) { r.Cells["pkPick"].Value = true; ticked++; } // trip-report rows (Nov 9) are keyed by their dispatch id
            if (ticked > 0) { AddTickedDispatches(); SoaShowTab(false); } // show the trip table (type Amount and Fuel surcharge there)
            LoadSoaGuideCache();
            lastGoodClient = cbClient.Text.Trim();
            UpdateSoaGuide();
            string note = onlyInvoiceIds != null && ticked < onlyInvoiceIds.Count ? $" ({onlyInvoiceIds.Count - ticked} chosen invoice(s) could not be added: see the picker)" : "";
            ToastHelper.ShowToast(this, ticked > 0 ? $"Filled from {ticked} service invoice{(ticked == 1 ? "" : "s")} of {client}{note} — review, then Save" : $"No ready service invoices found for {client} in that period", ticked > 0 ? ToastType.Success : ToastType.Info);
        }
    }
}
