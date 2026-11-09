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
    /// Payments guidance on the invoice form: only completed trips with a saved trip report that are not on a statement
    /// (or another invoice) can be invoiced; the client is the trip's client; an amount changed from the rate / incident
    /// figures needs a reason; the checklist gates Issue / Update.
    /// </summary>
    public partial class BillingView
    {
        private PaymentSummaryStrip billStrip = null!;
        private WorkflowStepper billStepper = null!;
        private ValidationChecklist billChecklist = null!;
        private HelpDrawer billHelp = null!;
        private Label lblBillBlocked = null!;
        private Panel pnlBillAdjust = null!;
        private Label lblBillVariance = null!;
        private TextBox txtBillAdjustReason = null!;
        private WaitingListPanel billWaiting = null!;
        private TapeCard? billTape, billNetTape;
        private RichLookup? invoiceLookup;
        private readonly System.Windows.Forms.Timer billGuideTimer = new System.Windows.Forms.Timer { Interval = 250 };
        private bool billSaving;
        private HashSet<int> billReported = new HashSet<int>();
        private Dictionary<int, string> billOnSoa = new Dictionary<int, string>();
        /// <summary>Dispatch id → the live invoice that already bills it (a cancelled invoice does not count).</summary>
        private Dictionary<int, LiveTripInvoice> billLive = new Dictionary<int, LiveTripInvoice>();
        private Dictionary<int, TripReport> billReports = new Dictionary<int, TripReport>();
        /// <summary>Dispatch id → the amount (freight tariff) the Rate Matrix, special-trip charge or agreed cancellation fee gives for it.</summary>
        private readonly Dictionary<int, decimal> billExpected = new Dictionary<int, decimal>();
        private decimal billPaid;

        private Panel BuildBillAdjustPanel()
        {
            pnlBillAdjust = new Panel { Height = Dpi(96), Visible = false, Padding = new Padding(0, Dpi(6), 0, 0) };
            lblBillVariance = new Label { Dock = DockStyle.Top, Height = Dpi(40), Font = new Font("Segoe UI Semibold", 9.25F), ForeColor = AppTheme.WarningText, UseMnemonic = false };
            txtBillAdjustReason = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F), MaxLength = 300, PlaceholderText = $"Adjustment reason (required, at least {StatementOfAccountView.MinAdjustReasonLength} characters)" };
            txtBillAdjustReason.TextChanged += (s, e) => ScheduleBillGuide();
            pnlBillAdjust.Controls.Add(txtBillAdjustReason);
            pnlBillAdjust.Controls.Add(lblBillVariance);
            return pnlBillAdjust;
        }
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        private void InitBillGuide()
        {
            if (formKit == null) return;
            formKit.Compact = true;
            formKit.Restyle();
            billStrip = new PaymentSummaryStrip { Cursor = Cursors.Hand };
            billStrip.Click += (s, e) => formKit?.ScrollTo(billChecklist);
            new ToolTip().SetToolTip(billStrip, "Live totals of this invoice. Click to see the validation checklist.");
            formKit.AddStrip(billStrip);
            billStepper = new WorkflowStepper("Dispatch created", "Trip report completed", "Invoice filled from trip", "Review", "Validated & ready");
            billHelp = new HelpDrawer("How this works",
                "1. Only completed dispatches with a saved trip report appear in the list — the invoice is filled from the trip (client, trip report, amount from the Rate Matrix). Once the trip is picked it is fixed; Clear form frees it.\n" +
                "2. A trip is billed once: either on a statement of account or on a direct invoice here. Trips already on a statement don't appear.\n" +
                "3. The client is always the trip's client. The statement of account reads this invoice's amount and fuel surcharge, so type them here, once.\n" +
                "4. If you change the amount from the Rate Matrix's figure, write the adjustment reason — it is kept with the invoice and in the audit log. There are no deductions on an invoice.\n" +
                "5. All checks must be green before Issue invoice. Payments are then recorded in full in Collections & Receipts (no part payments).");
            billChecklist = new ValidationChecklist();
            formKit.AddBodyTop(new Panel { Height = Dpi(8), BackColor = AppTheme.Canvas });
            formKit.AddBodyTop(billHelp);
            formKit.AddBodyTop(billStepper);
            formKit.AddBodyBottom(new Panel { Height = Dpi(16), BackColor = AppTheme.Canvas });
            formKit.AddBodyBottom(billChecklist);
            lblBillBlocked = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9.25F), ForeColor = AppTheme.DangerText, Margin = new Padding(0, Dpi(11), Dpi(12), 0), Cursor = Cursors.Hand, UseMnemonic = false };
            lblBillBlocked.Click += (s, e) => formKit?.ScrollTo(billChecklist);
            formKit.Actions?.Controls.Add(lblBillBlocked);
            InputGuard.MoneyOnly(numTotalAmount);
            InputGuard.MoneyOnly(numFuel);
            InputGuard.ReferenceOnly(txtInvoiceNumber, 30);
            billGuideTimer.Tick += (s, e) => { billGuideTimer.Stop(); UpdateBillGuide(); };
            cbDelivery.SelectedIndexChanged += (s, e) => ScheduleBillGuide();
            cbClient.TextChanged += (s, e) => ScheduleBillGuide();
            cbClient.Leave += (s, e) => EnforceBillClientLock();
            cbClient.SelectionChangeCommitted += (s, e) => BeginInvoke(new Action(EnforceBillClientLock));
            numTotalAmount.ValueChanged += (s, e) => ScheduleBillGuide();
            numFuel.ValueChanged += (s, e) => ScheduleBillGuide();
            txtInvoiceNumber.TextChanged += (s, e) => ScheduleBillGuide();
            invoiceLookup = RichLookup.Attach(txtInvoiceNumber, InvoiceSuggestions, it => ScheduleBillGuide());
            // Ready line above the invoice list.
            billWaiting = new WaitingListPanel();
            billWaiting.InvoiceRequested += StartInvoiceForWaiting;
            billWaiting.CountChanged += n => FinanceHubView.Of(this)?.SetTabBadge("Billing", n);
            pnlGridCard.Controls.Add(billWaiting);
            pnlGridCard.Controls.SetChildIndex(billWaiting, pnlGridCard.Controls.GetChildIndex(pnlGridHeaderBar));
            RefreshBillReady();
        }
        /// <summary>
        /// Ledger Desk layout of the invoice form (55 / 45): the invoice and the trip on the left, the amounts and the open problems on the right, one slim
        /// header with the live figures. Layout only: every field, handler and rule is the same.
        /// </summary>
        private void ApplyBillWorkbench()
        {
            if (formKit == null || billChecklist == null) return;
            billChecklist.ShowOnlyProblems = true;
            billChecklist.FocusResolver = c =>
            {
                string t = c.Text;
                if (t.Contains("adjustment reason", StringComparison.OrdinalIgnoreCase)) return txtBillAdjustReason;
                if (t.Contains("Service Invoice No", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Invoice #", StringComparison.OrdinalIgnoreCase)) return txtInvoiceNumber;
                if (t.Contains("trip", StringComparison.OrdinalIgnoreCase) || t.Contains("dispatch", StringComparison.OrdinalIgnoreCase)) return cbDelivery;
                if (t.Contains("client", StringComparison.OrdinalIgnoreCase)) return cbClient;
                if (t.Contains("fuel", StringComparison.OrdinalIgnoreCase)) return numFuel;
                if (t.Contains("amount", StringComparison.OrdinalIgnoreCase) || t.Contains("Rate Matrix", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) return numTotalAmount;
                return null;
            };
            billStrip.ShowPayment = false; // "Payment: Unpaid" means nothing on an invoice that is not issued yet
            billStrip.FirstCaption = "Amount"; billStrip.SecondCaption = "Fuel surcharge"; billStrip.ShowNet = false; // Nov 7: an invoice shows only what was typed (amount, fuel surcharge): no total, tax or withholding; the first computation is on the statement
            billTape = new TapeCard { EmptyText = "Pick the completed trip to see its dispatch, route, date and crates here." };
            billTape.Set("The trip being billed", Array.Empty<(string, string)>(), "", "", "");
            billNetTape = new TapeCard();
            billNetTape.Set("What was typed on this invoice", Array.Empty<(string, string)>(), "", "", "");
            pnlNetDue.Visible = false; // replaced by the tape on the right (not removed: its labels are still updated by the form)
            formKit.ApplyWorkbench(55f, new[] { 0 }, new[] { 1 }, billChecklist, new Control?[] { billStepper, billHelp }, billStrip, 620, new Control[] { billTape }, new Control[] { billNetTape });
        }
        /// <summary>The amounts tape on the right: the amount and the fuel surcharge exactly as typed, and the payment due date. No total, VAT or withholding on an invoice.</summary>
        private void UpdateBillNet(decimal net)
        {
            if (billNetTape == null) return;
            _ = net; // not shown: an invoice carries no computation
            DateTime due = selectedInvoice != null && selectedInvoice.DueDate > DateTime.MinValue ? selectedInvoice.DueDate : DateTime.Today.AddDays(15);
            decimal amount = numTotalAmount.Value, fuel = numFuel.Value;
            billNetTape.Set("What was typed on this invoice", new (string, string)[] { ("Amount (freight tariff)", "₱" + amount.ToString("N2")), ("Fuel surcharge", "₱" + fuel.ToString("N2")), ("Payment due", $"{due:MMM d, yyyy} · 15 days") },
                "", "", "");
        }
        /// <summary>Fills the trip card on the left from the picked trip (read-only; the picked trip itself is still the Completed Delivery Trip box).</summary>
        private void UpdateBillTrip(Delivery? d)
        {
            if (billTape == null) return;
            if (d == null) { billTape.Set("The trip being billed", Array.Empty<(string, string)>(), "", "", ""); return; }
            billReports.TryGetValue(d.Id, out var tr);
            var rows = new List<(string, string)>
            {
                (d.Id < 0 ? "Special trip" : "Dispatch", d.DispatchNumber ?? ""), ("Client", d.ClientName ?? ""), ("Route", d.CleanRouteDisplay ?? ""),
                ("Date", d.DispatchDate.ToString("MMM d, yyyy")), ("Crates", d.TotalCrates.ToString("N0")), ("Truck", d.TruckInfo ?? "")
            };
            if (tr != null && !string.IsNullOrWhiteSpace(tr.TripReportNumber))
            {
                rows.Add(("Trip report", tr.TripReportNumber));
                rows.Add(("Date completed", tr.ReportDate.ToString("dd-MMM-yyyy")));
                rows.Add(("Delivered / returned", $"{tr.DeliveredCrates:N0} / {tr.ReturnedCrates:N0} crates"));
                if (!string.IsNullOrWhiteSpace(tr.StoreManagerName)) rows.Add(("Received by", tr.StoreManagerName.Trim()));
            }
            if (billExpected.TryGetValue(d.Id, out var exp) && exp > 0)
                billTape.Set("The trip being billed", rows, "Amount from the Rate Matrix", "₱" + exp.ToString("N2"), "the fuel surcharge is typed on the invoice");
            else billTape.Set("The trip being billed", rows, "", "", "");
        }
        private void ScheduleBillGuide()
        {
            if (billGuideTimer == null || !pnlForm.Visible || billChecklist == null) return;
            billGuideTimer.Stop();
            billGuideTimer.Start();
        }
        private void LoadBillGuideCache()
        {
            try
            {
                billReported = MJdatabase.GetTripReportDeliveryIds();
                foreach (var kv in MJdatabase.GetSpecialTripLinks()) if (kv.Value.HasReport) billReported.Add(-kv.Key); // Nov 6: special trips
                billOnSoa = MJdatabase.GetBilledDeliveryIds(0);
                billLive = MJdatabase.GetLiveInvoicesByDelivery();
                billReports = MJdatabase.GetTripReportsByDelivery();
                billPaid = selectedInvoice != null ? MJdatabase.GetTotalCollectedForInvoice(selectedInvoice.Id) : 0;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        private void OnBillFormShown(bool visible)
        {
            if (billChecklist == null) return;
            if (!visible) { billGuideTimer.Stop(); invoiceLookup?.HideList(); RefreshBillReady(); return; }
            billSaving = false;
            billGuideInvoiceId = -1; // reload on the first update
            UpdateBillGuide();
        }
        /// <summary>Completed dispatches with a saved trip report that are on no statement and no invoice.</summary>
        private void RefreshBillReady()
        {
            billWaiting?.RefreshData();
        }
        /// <summary>The worklist asked to invoice a trip: open the new-invoice form on it.</summary>
        private void StartInvoiceForWaiting(WaitingTripRow row)
        {
            int id = row.SpecialTripId > 0 ? -row.SpecialTripId : row.DeliveryId;
            var d = completedDeliveries.Find(x => x.Id == id);
            if (d == null) { LoadData(); d = completedDeliveries.Find(x => x.Id == id); }
            if (d == null) { QuietBox.Show($"{row.Number} can no longer be invoiced from here. It may have just been billed.", "Trip not available", MessageBoxButtons.OK, MessageBoxIcon.Information); billWaiting?.RefreshData(); return; }
            StartInvoiceFor(d);
        }
        private void StartInvoiceFor(Delivery d)
        {
            OpenNewRecordForm();
            int idx = completedDeliveries.FindIndex(x => x.Id == d.Id);
            if (idx >= 0 && idx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = idx;
            UpdateBillGuide();
        }
        private Delivery? SelectedBillDelivery() => cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count ? completedDeliveries[cbDelivery.SelectedIndex] : null;
        /// <summary>The client this invoice must be for: the trip's client (new invoice) or the client it was issued to (linked invoice).</summary>
        private string BillLockedClient()
        {
            if (selectedInvoice != null) return selectedInvoice.DeliveryId > 0 || selectedInvoice.SoaId > 0 ? (selectedInvoice.ClientName ?? "").Trim() : "";
            var d = SelectedBillDelivery();
            return d == null ? "" : (d.ClientName ?? "").Trim();
        }
        private void EnforceBillClientLock()
        {
            if (isBindingForm || !pnlForm.Visible) return;
            string locked = BillLockedClient();
            string typed = cbClient.Text.Trim();
            if (locked.Length == 0 || string.Equals(locked, typed, StringComparison.OrdinalIgnoreCase)) return;
            isBindingForm = true;
            try { cbClient.Text = locked; }
            finally { isBindingForm = false; }
            LinkedChoicesPopover.ShowUnder(cbClient, $"This invoice bills a trip of {locked}, so the client stays {locked}.", new[] { locked }, v => { cbClient.Text = v; ScheduleBillGuide(); });
            ScheduleBillGuide();
        }
        private IEnumerable<LookupItem> InvoiceSuggestions(string q)
        {
            int n = 0;
            foreach (var inv in allInvoices.Where(i => (i.InvoiceNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                var kind = RowMenu.Is(inv.Status, "Paid") ? BadgeKind.Good : RowMenu.Is(inv.Status, "Draft") ? BadgeKind.Warn : inv.DueDate.Date < DateTime.Today ? BadgeKind.Bad : BadgeKind.Info;
                yield return new LookupItem(inv.InvoiceNumber, $"Existing · {inv.ClientName} · ₱{inv.NetAmount:N2} · {inv.IssueDate:MMM d, yyyy}{(inv.SoaId > 0 ? " · from a statement" : "")}", inv.Status, kind);
                if (++n >= 20) yield break;
            }
        }
        private decimal ExpectedFor(Delivery d)
        {
            if (billExpected.TryGetValue(d.Id, out var v)) return v;
            if (d.Id < 0) return billExpected[d.Id] = SpecialCharge(d); // Nov 6: special trip charge
            if (IsChargeableCancelled(d)) return billExpected[d.Id] = chargeFees[d.Id]; // agreed cancellation fee
            decimal gross = 0;
            try { gross = MJdatabase.CalculateDeliveryTariff(d, out _); }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return billExpected[d.Id] = gross;
        }
        /// <summary>The amount differs from the trip's rate (so a reason is needed). Nov 7: no deductions; the fuel surcharge is always typed, so it is not compared.</summary>
        private bool BillAdjusted(out decimal expectedNet, out decimal enteredNet, out string what)
        {
            expectedNet = 0; enteredNet = numTotalAmount.Value; what = "";
            if (selectedInvoice != null && selectedInvoice.SoaId > 0) return false; // amounts come from the statement
            Delivery? d = SelectedBillDelivery();
            if (selectedInvoice != null) d = selectedInvoice.DeliveryId > 0 ? MJdatabase.GetDeliveryById(selectedInvoice.DeliveryId) : null;
            if (d == null) return false;
            decimal g = ExpectedFor(d);
            if (g <= 0) return false; // no rate to compare with
            expectedNet = g;
            var parts = new List<string>();
            if (Math.Abs(numTotalAmount.Value - g) > 0.005m) parts.Add($"Amount: rate ₱{g:N2} → ₱{numTotalAmount.Value:N2}");
            what = string.Join("   ·   ", parts);
            return parts.Count > 0;
        }

        private List<PaymentCheck> ComputeBillChecks()
        {
            var checks = new List<PaymentCheck>();
            bool editing = selectedInvoice != null;
            string client = cbClient.Text.Trim();
            if (!editing)
            {
                var d = SelectedBillDelivery();
                bool chargeable = d != null && IsChargeableCancelled(d);
                if (d == null) checks.Add(new PaymentCheck("Pick a completed trip to invoice (Completed Delivery Trip is required)", false));
                else if (chargeable) checks.Add(new PaymentCheck($"Dispatch {d.DispatchNumber} was cancelled but is chargeable (agreed ₱{chargeFees[d.Id]:N2})", true));
                else if (string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) checks.Add(new PaymentCheck($"Dispatch {d.DispatchNumber} was cancelled with no charge — it can't be billed", false));
                else checks.Add(new PaymentCheck($"Dispatch {d.DispatchNumber} is completed", DispatchGate.IsCompleted(d.Status)));
                if (d != null)
                {
                    if (!chargeable) // a cancelled trip has no trip report
                        checks.Add(new PaymentCheck(billReported.Contains(d.Id) ? "Its trip report is saved" : $"{d.DispatchNumber} has no saved trip report — file it first", billReported.Contains(d.Id)));
                    bool onSoa = billOnSoa.TryGetValue(d.Id, out var soaNo);
                    billLive.TryGetValue(d.Id, out var dupInv); // a cancelled invoice is not a duplicate; an old statement's invoice is
                    checks.Add(new PaymentCheck(dupInv != null ? $"{d.DispatchNumber} is already invoiced: {dupInv.Describe()}" : onSoa ? $"{d.DispatchNumber} is on statement {soaNo}: the amount and fuel surcharge come from it (locked)" : "Not billed yet: no live invoice or statement for this trip", dupInv == null)); // Nov 9: a trip on a statement is invoiced here
                    bool clientOk = string.Equals((d.ClientName ?? "").Trim(), client, StringComparison.OrdinalIgnoreCase);
                    checks.Add(new PaymentCheck(clientOk ? $"Client is the trip's client ({d.ClientName})" : $"Client must be {d.ClientName} (the trip's client)", clientOk));
                    bool datesOk = d.DispatchDate.Date <= DateTime.Today && (!billReports.TryGetValue(d.Id, out var tr) || (tr.ReportDate.Date <= DateTime.Today));
                    checks.Add(new PaymentCheck(datesOk ? "Dates in order: dispatch ≤ trip report ≤ invoice date (today)" : "The dispatch or trip report is dated after today", datesOk));
                    if (billReports.TryGetValue(d.Id, out var tr2) && tr2.ReportDate.Date < d.DispatchDate.Date)
                        checks.Add(new PaymentCheck("Trip report is dated before the dispatch — check the report date", true, Warning: true));
                }
            }
            else
            {
                string locked = BillLockedClient();
                bool clientOk = locked.Length == 0 ? client.Length > 0 : string.Equals(locked, client, StringComparison.OrdinalIgnoreCase);
                checks.Add(new PaymentCheck(clientOk ? $"Client {client}" : locked.Length > 0 ? $"Client must stay {locked}" : "Choose the client", clientOk));
                if (selectedInvoice!.SoaId > 0)
                {
                    // A statement's invoice must match the statement; change the amounts on the SOA instead.
                    bool same = Math.Abs(numTotalAmount.Value - selectedInvoice.Freight) < 0.005m && Math.Abs(numFuel.Value - selectedInvoice.FuelSurcharge) < 0.005m;
                    checks.Add(new PaymentCheck(same ? "Amounts match its statement of account" : "This invoice is on a statement - its amounts can't change while it is on one (take its trip off the Statement of Account first)", same));
                }
                else if (selectedInvoice.DeliveryId <= 0 && string.IsNullOrWhiteSpace(selectedInvoice.DispatchNumber)) // special-trip invoices carry their ST- number
                    checks.Add(new PaymentCheck("This invoice bills no trip — a direct invoice needs a completed (or chargeable cancelled) trip", false));
                if (billPaid > 0)
                {
                    // Payments are in full: once a receipt exists the amounts stay as paid (void the receipt first to change them).
                    bool unchanged = Math.Abs(numTotalAmount.Value - selectedInvoice.Freight) < 0.005m && Math.Abs(numFuel.Value - selectedInvoice.FuelSurcharge) < 0.005m;
                    checks.Add(new PaymentCheck(unchanged ? $"Amounts stay as paid (₱{billPaid:N2} collected)" : $"₱{billPaid:N2} is already collected, so the amounts can't change — void the receipt in Collections & Receipts first", unchanged));
                }
            }
            // Invoice number: present, unique.
            string invNum = txtInvoiceNumber.Text.Trim();
            billNumberGuard?.Recheck();
            var gn = billNumberGuard?.Current;
            bool numberBlocked = gn != null && gn.Blocked;
            bool tickMissing = billNumberGuard != null && billNumberGuard.TickMissing;
            bool renameDenied = selectedInvoice != null && invNum.Length > 0 && !string.Equals((selectedInvoice.InvoiceNumber ?? "").Trim(), invNum, StringComparison.OrdinalIgnoreCase) && !SessionContext.IsAdmin;
            checks.Add(new PaymentCheck(invNum.Length == 0 ? "Enter the Service Invoice No." : numberBlocked ? gn!.Why()
                : renameDenied ? "Only the Administrator can change the Service Invoice No. of a saved invoice"
                : tickMissing ? "Tick the confirmation under the Service Invoice No. (same digits as another document)" : $"Service Invoice No. {invNum} is free",
                invNum.Length > 0 && !numberBlocked && !renameDenied && !tickMissing));
            // Math
            decimal amount = numTotalAmount.Value, fuel = numFuel.Value;
            checks.Add(new PaymentCheck(amount > 0 ? "Amount is more than ₱0.00" : "Amount is ₱0.00", amount > 0));
            if (BillAdjusted(out var expNet, out var entNet, out _))
            {
                bool hasReason = txtBillAdjustReason.Text.Trim().Length >= StatementOfAccountView.MinAdjustReasonLength;
                checks.Add(new PaymentCheck(hasReason ? "Adjustment reason given" : "Amount changed from the trip's figures — write the adjustment reason", hasReason));
            }
            else if (!editing && Functions.Features.On(Functions.Features.RateMatrix) && SelectedBillDelivery() is Delivery dd && ExpectedFor(dd) <= 0)
                checks.Add(new PaymentCheck("No rate found in the Rate Matrix for this trip — check the amount", true, Warning: true));
            return checks;
        }

        private int billGuideInvoiceId = -1;
        private void UpdateBillGuide()
        {
            if (billChecklist == null || !pnlForm.Visible) return;
            int curId = selectedInvoice?.Id ?? 0;
            if (curId != billGuideInvoiceId)
            {
                // Another invoice was opened in the form (e.g. its number typed): reload what it has been paid and its reason.
                billGuideInvoiceId = curId;
                LoadBillGuideCache();
                txtBillAdjustReason.Text = selectedInvoice != null ? MJdatabase.GetAdjustmentReason("Invoices", selectedInvoice.Id) : "";
            }
            var checks = ComputeBillChecks();
            billChecklist.SetChecks(checks);
            int fails = billChecklist.Failed, warns = billChecklist.Warnings;
            decimal net = numTotalAmount.Value + numFuel.Value;
            billStrip.SetTotals(numTotalAmount.Value, numFuel.Value, billPaid, Math.Max(0, net - billPaid), fails, warns);
            bool editing = selectedInvoice != null;
            var d = SelectedBillDelivery();
            UpdateBillTrip(d);
            UpdateBillNet(net);
            if (editing) billStepper.SetProgress(fails > 0 ? 3 : 4, fails > 0 ? $"Editing invoice {selectedInvoice!.InvoiceNumber}: {fails} to fix — see the checklist." : $"Invoice {selectedInvoice!.InvoiceNumber} is valid — Update invoice saves the changes.");
            else if (d == null) billStepper.SetProgress(completedDeliveries.Count > 0 ? 1 : 0, completedDeliveries.Count > 0
                ? $"{completedDeliveries.Count} completed trip{(completedDeliveries.Count == 1 ? " is" : "s are")} ready — pick one under Invoice & order."
                : "No completed trips with a trip report are waiting for a direct invoice.");
            else if (fails > 0) billStepper.SetProgress(2, $"Review: {fails} item{(fails == 1 ? "" : "s")} to fix — see the checklist at the bottom.");
            else billStepper.SetProgress(4, warns > 0 ? $"✓ Validated — {warns} note{(warns == 1 ? "" : "s")} to review, then Issue invoice." : "✓ Validated — Issue invoice saves it with 15-day terms.");
            bool adjusted = BillAdjusted(out _, out _, out var what);
            if (pnlBillAdjust.Visible != adjusted) pnlBillAdjust.Visible = adjusted;
            if (adjusted)
            {
                lblBillVariance.Text = $"Trip figures changed — {what}.  An adjustment reason is required below.";
                FieldMarks.Mark(txtBillAdjustReason, txtBillAdjustReason.Text.Trim().Length >= StatementOfAccountView.MinAdjustReasonLength ? FieldState.Ok : FieldState.Bad);
            }
            FieldMarks.Mark(cbClient, cbClient.Text.Trim().Length == 0 ? FieldState.None : checks.Any(c => !c.Passed && c.Text.StartsWith("Client")) ? FieldState.Bad : FieldState.Ok);
            FieldMarks.Mark(txtInvoiceNumber, txtInvoiceNumber.Text.Trim().Length == 0 ? FieldState.None : checks.Any(c => !c.Passed && c.Text.StartsWith("Invoice #")) ? FieldState.Bad : FieldState.Ok);
            // Save gate (Issue for a new invoice, Update for an existing one).
            bool ok = fails == 0 && !billSaving;
            if (btnSave.Visible) { btnSave.Enabled = ok; ApplyButtonVisualState(btnSave, ok, Pal.C(13, 110, 253), Pal.White); }
            if (btnUpdate.Visible) { btnUpdate.Enabled = ok; ApplyButtonVisualState(btnUpdate, ok, Pal.C(25, 135, 84), Pal.White); }
            lblBillBlocked.Text = fails > 0 ? $"✗ {fails} to fix before saving" : "";
            lblBillBlocked.Visible = fails > 0;
        }
        /// <summary>Runs before Issue / Update: the checklist must be all green, and only one save at a time.</summary>
        private bool BillGateBeforeSave()
        {
            if (billSaving) return false;
            if (billChecklist == null) return true;
            UpdateBillGuide();
            if (billChecklist.Failed > 0)
            {
                MessageBox.Show(FindForm(), "This invoice can't be saved yet:\n\n• " + string.Join("\n• ", billChecklist.Checks.Where(c => !c.Passed).Select(c => c.Text)), "Check the invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                formKit?.ScrollTo(billChecklist);
                return false;
            }
            billSaving = true;
            btnSave.Enabled = false;
            btnUpdate.Enabled = false;
            return true;
        }
        private void BillAfterSave(int invoiceId, string invoiceNumber)
        {
            try
            {
                bool adjusted = BillAdjusted(out var expNet, out var entNet, out _);
                MJdatabase.SaveAdjustmentReason("Invoices", invoiceId, invoiceNumber, adjusted ? txtBillAdjustReason.Text.Trim() : "", expNet, entNet);
            }
            catch (Exception ex) { ErrLog.Error("BillingView.AdjustmentReason", ex); }
        }
        private void BillSaveFinished()
        {
            billSaving = false;
            if (pnlForm.Visible) UpdateBillGuide(); else SetButtonStates(selectedInvoice != null);
        }
    }
}
