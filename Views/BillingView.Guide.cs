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
        private ReadyBanner billReady = null!;
        private RichLookup? invoiceLookup;
        private readonly System.Windows.Forms.Timer billGuideTimer = new System.Windows.Forms.Timer { Interval = 250 };
        private bool billSaving;
        private HashSet<int> billReported = new HashSet<int>();
        private Dictionary<int, string> billOnSoa = new Dictionary<int, string>();
        private Dictionary<int, TripReport> billReports = new Dictionary<int, TripReport>();
        private readonly Dictionary<int, (decimal Gross, decimal Deductions)> billExpected = new Dictionary<int, (decimal, decimal)>();
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
                "1. Only completed dispatches with a saved trip report appear in the list — the invoice is filled from the trip (client, rate from the Rate Matrix, incident deductions).\n" +
                "2. A trip is billed once: either on a statement of account or on a direct invoice here. Trips already on a statement don't appear.\n" +
                "3. The client is always the trip's client.\n" +
                "4. If you change the amount or the deductions, write the adjustment reason — it is kept with the invoice and in the audit log.\n" +
                "5. All checks must be green before Issue invoice. Payments are then recorded in Collections & Receipts and can't exceed the balance.");
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
            InputGuard.MoneyOnly(numDeductions);
            InputGuard.ReferenceOnly(txtInvoiceNumber, 30);
            billGuideTimer.Tick += (s, e) => { billGuideTimer.Stop(); UpdateBillGuide(); };
            cbDelivery.SelectedIndexChanged += (s, e) => ScheduleBillGuide();
            cbClient.TextChanged += (s, e) => ScheduleBillGuide();
            cbClient.Leave += (s, e) => EnforceBillClientLock();
            cbClient.SelectionChangeCommitted += (s, e) => BeginInvoke(new Action(EnforceBillClientLock));
            numTotalAmount.ValueChanged += (s, e) => ScheduleBillGuide();
            numDeductions.ValueChanged += (s, e) => ScheduleBillGuide();
            txtInvoiceNumber.TextChanged += (s, e) => ScheduleBillGuide();
            invoiceLookup = RichLookup.Attach(txtInvoiceNumber, InvoiceSuggestions, it => ScheduleBillGuide());
            // Ready line above the invoice list.
            billReady = new ReadyBanner();
            pnlGridCard.Controls.Add(billReady);
            pnlGridCard.Controls.SetChildIndex(billReady, pnlGridCard.Controls.GetChildIndex(pnlGridHeaderBar));
            RefreshBillReady();
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
                billOnSoa = MJdatabase.GetBilledDeliveryIds(0);
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
            if (billReady == null) return;
            var items = completedDeliveries.Take(20).Select(d => (
                $"{d.DispatchNumber}  ·  {d.ClientName}  ·  {d.DispatchDate:MMM d}   ▸ Invoice",
                $"Opens a new invoice filled from dispatch {d.DispatchNumber} ({d.CleanRouteDisplay}). Nothing is saved until you issue it.",
                (Action)(() => StartInvoiceFor(d)))).ToList();
            billReady.SetItems($"{completedDeliveries.Count} completed trip{(completedDeliveries.Count == 1 ? "" : "s")} ready to invoice (not on a statement):",
                "No completed trips are waiting for a direct invoice. Trips billed on a statement of account get their invoice from the statement.", items, 4);
        }
        private void StartInvoiceFor(Delivery d)
        {
            OpenNewRecordForm();
            int idx = completedDeliveries.FindIndex(x => x.Id == d.Id);
            if (idx >= 0 && idx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = idx;
            if (string.IsNullOrWhiteSpace(txtInvoiceNumber.Text)) txtInvoiceNumber.Text = MJdatabase.GenerateNextInvoiceNumber();
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
                var kind = RowMenu.Is(inv.Status, "Paid") ? BadgeKind.Good : RowMenu.Is(inv.Status, "Partial") ? BadgeKind.Warn : RowMenu.Is(inv.Status, "Overdue") ? BadgeKind.Bad : BadgeKind.Info;
                yield return new LookupItem(inv.InvoiceNumber, $"Existing · {inv.ClientName} · ₱{inv.NetAmount:N2} · {inv.IssueDate:MMM d, yyyy}{(inv.SoaId > 0 ? " · from a statement" : "")}", inv.Status, kind);
                if (++n >= 20) yield break;
            }
        }
        private (decimal Gross, decimal Deductions) ExpectedFor(Delivery d)
        {
            if (billExpected.TryGetValue(d.Id, out var v)) return v;
            if (IsChargeableCancelled(d)) return billExpected[d.Id] = (chargeFees[d.Id], 0m); // agreed cancellation fee, no incident deductions
            decimal gross = 0, ded = 0;
            try
            {
                gross = MJdatabase.CalculateDeliveryTariff(d, out _);
                ded = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == d.Id && !ir.ChargeableToCrew && ir.Status != "Rejected").Sum(ir => ir.EstimatedCost);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return billExpected[d.Id] = (gross, ded);
        }
        /// <summary>The amount or deductions differ from the rate / incident figures of the trip (so a reason is needed).</summary>
        private bool BillAdjusted(out decimal expectedNet, out decimal enteredNet, out string what)
        {
            expectedNet = 0; enteredNet = numTotalAmount.Value - numDeductions.Value; what = "";
            if (selectedInvoice != null && selectedInvoice.SoaId > 0) return false; // amounts come from the statement
            Delivery? d = SelectedBillDelivery();
            if (selectedInvoice != null) d = selectedInvoice.DeliveryId > 0 ? MJdatabase.GetDeliveryById(selectedInvoice.DeliveryId) : null;
            if (d == null) return false;
            var (g, ded) = ExpectedFor(d);
            if (g <= 0) return false; // no rate to compare with
            expectedNet = g - ded;
            var parts = new List<string>();
            if (Math.Abs(numTotalAmount.Value - g) > 0.005m) parts.Add($"Gross: rate ₱{g:N2} → ₱{numTotalAmount.Value:N2}");
            if (Math.Abs(numDeductions.Value - ded) > 0.005m) parts.Add($"Deductions: incidents ₱{ded:N2} → ₱{numDeductions.Value:N2}");
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
                    var dupInv = allInvoices.FirstOrDefault(i => i.DeliveryId == d.Id);
                    checks.Add(new PaymentCheck(onSoa ? $"{d.DispatchNumber} is already on statement {soaNo}" : dupInv != null ? $"{d.DispatchNumber} is already invoiced ({dupInv.InvoiceNumber})" : "Not billed on a statement or another invoice", !onSoa && dupInv == null));
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
                    bool same = Math.Abs(numTotalAmount.Value - selectedInvoice.TotalAmount) < 0.005m && Math.Abs(numDeductions.Value - selectedInvoice.Deductions) < 0.005m;
                    checks.Add(new PaymentCheck(same ? "Amounts match its statement of account" : "This invoice comes from a statement — change its amounts on the Statement of Account, not here", same));
                }
                else if (selectedInvoice.DeliveryId <= 0 && string.IsNullOrWhiteSpace(selectedInvoice.DispatchNumber)) // special-trip invoices carry their ST- number
                    checks.Add(new PaymentCheck("This invoice bills no trip — a direct invoice needs a completed (or chargeable cancelled) trip", false));
                decimal net0 = numTotalAmount.Value - numDeductions.Value;
                checks.Add(new PaymentCheck(net0 >= billPaid ? $"Net covers the ₱{billPaid:N2} already collected" : $"Net ₱{net0:N2} is less than the ₱{billPaid:N2} already collected", net0 >= billPaid));
            }
            // Invoice number: present, unique.
            string invNum = txtInvoiceNumber.Text.Trim();
            var clash = invNum.Length == 0 ? null : allInvoices.FirstOrDefault(i => (selectedInvoice == null || i.Id != selectedInvoice.Id) && string.Equals(i.InvoiceNumber, invNum, StringComparison.OrdinalIgnoreCase));
            checks.Add(new PaymentCheck(invNum.Length == 0 ? "Enter the invoice number" : clash != null ? $"Invoice # {invNum} is already used ({clash.ClientName})" : $"Invoice # {invNum} is free", invNum.Length > 0 && clash == null));
            // Math
            decimal gross = numTotalAmount.Value, ded = numDeductions.Value;
            checks.Add(new PaymentCheck(gross > 0 ? "Gross amount is more than ₱0.00" : "Gross amount is ₱0.00", gross > 0));
            checks.Add(new PaymentCheck(ded <= gross ? $"Net = gross − deductions = ₱{gross - ded:N2}" : $"Deductions ₱{ded:N2} are more than the gross ₱{gross:N2}", ded <= gross));
            if (BillAdjusted(out var expNet, out var entNet, out _))
            {
                bool hasReason = txtBillAdjustReason.Text.Trim().Length >= StatementOfAccountView.MinAdjustReasonLength;
                checks.Add(new PaymentCheck(hasReason ? "Adjustment reason given" : "Amount changed from the trip's figures — write the adjustment reason", hasReason));
            }
            else if (!editing && SelectedBillDelivery() is Delivery dd && ExpectedFor(dd).Gross <= 0)
                checks.Add(new PaymentCheck("No rate found in the Rate Matrix for this trip — check the gross amount", true, Warning: true));
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
            decimal net = numTotalAmount.Value - numDeductions.Value;
            billStrip.SetTotals(numTotalAmount.Value, numDeductions.Value, billPaid, Math.Max(0, net - billPaid), fails, warns);
            bool editing = selectedInvoice != null;
            var d = SelectedBillDelivery();
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
            FieldMarks.Mark(numDeductions, numDeductions.Value > numTotalAmount.Value ? FieldState.Bad : FieldState.None);
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
