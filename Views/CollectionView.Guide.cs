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
    /// Payments guidance on the receipt form: a payment is always for an issued invoice, can never be more than its
    /// balance (hard cap), needs a reference for non-cash methods, can't reuse a reference, and the checklist gates Save.
    /// </summary>
    public partial class CollectionView
    {
        private PaymentSummaryStrip collStrip = null!;
        private WorkflowStepper collStepper = null!;
        private ValidationChecklist collChecklist = null!;
        private HelpDrawer collHelp = null!;
        private Label lblCollBlocked = null!;
        private Label lblCollCap = null!;
        private ReadyBanner collReady = null!;
        private RichLookup? refLookup;
        private readonly System.Windows.Forms.Timer collGuideTimer = new System.Windows.Forms.Timer { Interval = 250 };
        private bool collSaving;
        private Invoice? collInvoice;          // the invoice this receipt pays
        private decimal collPaidOthers;        // paid on it by other receipts
        private int collGuideKey = int.MinValue;
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        /// <summary>Hint under the amount: the most that can be paid (placed in the Payment section).</summary>
        private Label BuildCollCapLabel()
        {
            lblCollCap = new Label { Height = Dpi(22), Font = new Font("Segoe UI Semibold", 8.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, TextAlign = ContentAlignment.TopLeft };
            return lblCollCap;
        }
        private void InitCollGuide()
        {
            if (formKit == null) return;
            formKit.Compact = true;
            formKit.Restyle();
            collStrip = new PaymentSummaryStrip { Cursor = Cursors.Hand };
            collStrip.Click += (s, e) => formKit?.ScrollTo(collChecklist);
            new ToolTip().SetToolTip(collStrip, "The invoice being paid: amount, paid so far and the balance after this payment. Click to see the checklist.");
            formKit.AddStrip(collStrip);
            collStepper = new WorkflowStepper("Trip report completed", "Invoice issued", "Payment filled", "Review", "Validated & ready");
            collHelp = new HelpDrawer("How this works",
                "1. A payment is always recorded against an issued invoice (from a statement of account or a direct invoice). Only invoices with a balance are listed.\n" +
                "2. The amount is filled with the balance; it can be less (partial payment) but never more than the balance.\n" +
                "3. Cash needs no reference. Cheque, bank transfer, online and other payments need the cheque / transaction number, and the same number can't be recorded twice.\n" +
                "4. The payment date is today, which is always on or after the invoice date.\n" +
                "5. All checks must be green before Save. The invoice turns Partial or Paid by itself.");
            collChecklist = new ValidationChecklist();
            formKit.AddBodyTop(new Panel { Height = Dpi(8), BackColor = AppTheme.Canvas });
            formKit.AddBodyTop(collHelp);
            formKit.AddBodyTop(collStepper);
            formKit.AddBodyBottom(new Panel { Height = Dpi(16), BackColor = AppTheme.Canvas });
            formKit.AddBodyBottom(collChecklist);
            lblCollBlocked = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9.25F), ForeColor = AppTheme.DangerText, Margin = new Padding(0, Dpi(11), Dpi(12), 0), Cursor = Cursors.Hand, UseMnemonic = false };
            lblCollBlocked.Click += (s, e) => formKit?.ScrollTo(collChecklist);
            formKit.Actions?.Controls.Add(lblCollBlocked);
            InputGuard.MoneyOnly(numAmountPaid);
            InputGuard.ReferenceOnly(txtRefNo, 40);
            collGuideTimer.Tick += (s, e) => { collGuideTimer.Stop(); UpdateCollGuide(); };
            cbInvoice.SelectedIndexChanged += (s, e) => { collGuideKey = int.MinValue; ScheduleCollGuide(); };
            numAmountPaid.ValueChanged += (s, e) => ScheduleCollGuide();
            numAmountPaid.KeyUp += (s, e) => ScheduleCollGuide();
            cbPaymentMethod.TextChanged += (s, e) => ScheduleCollGuide();
            txtRefNo.TextChanged += (s, e) => ScheduleCollGuide();
            refLookup = RichLookup.Attach(txtRefNo, ReceiptSuggestions, it => ScheduleCollGuide());
            collReady = new ReadyBanner();
            pnlGridCard.Controls.Add(collReady);
            pnlGridCard.Controls.SetChildIndex(collReady, pnlGridCard.Controls.GetChildIndex(pnlGridHeaderBar));
            RefreshCollReady();
        }
        private void ScheduleCollGuide()
        {
            if (collGuideTimer == null || collChecklist == null || !pnlForm.Visible) return;
            collGuideTimer.Stop();
            collGuideTimer.Start();
        }
        private void OnCollFormShown(bool visible)
        {
            if (collChecklist == null) return;
            if (!visible) { collGuideTimer.Stop(); refLookup?.HideList(); RefreshCollReady(); return; }
            collSaving = false;
            collGuideKey = int.MinValue;
            UpdateCollGuide();
        }
        private void RefreshCollReady()
        {
            if (collReady == null) return;
            var items = new List<(string, string, Action)>();
            try
            {
                foreach (var inv in unpaidInvoices.OrderBy(i => i.DueDate).Take(20))
                {
                    decimal bal = Math.Max(0, inv.NetAmount - MJdatabase.GetTotalCollectedForInvoice(inv.Id));
                    if (bal <= 0) continue;
                    var target = inv;
                    items.Add(($"{inv.InvoiceNumber}  ·  {inv.ClientName}  ·  ₱{bal:N2} due{(inv.DueDate.Date < DateTime.Today ? " (overdue)" : "")}   ▸ Record payment",
                        $"Opens a new receipt for invoice {inv.InvoiceNumber} with the balance filled in. Nothing is saved until you save it.",
                        (Action)(() => PrefillForInvoice(target))));
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            collReady.SetItems($"{items.Count} invoice{(items.Count == 1 ? "" : "s")} with a balance to collect:",
                "No invoices are waiting for payment. Invoices come from statements of account (with an SI no.) and from Billing.", items, 4);
        }
        private IEnumerable<LookupItem> ReceiptSuggestions(string q)
        {
            int n = 0;
            foreach (var r in allReceipts.Where(r => (r.ReferenceNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) || (r.ReceiptNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                bool self = selectedCollection != null && r.Id == selectedCollection.Id;
                string val = string.IsNullOrWhiteSpace(r.ReferenceNumber) ? r.ReceiptNumber : r.ReferenceNumber;
                yield return new LookupItem(val, $"Receipt {r.ReceiptNumber} · invoice {r.InvoiceNumber} · ₱{r.AmountPaid:N2} · {r.PaymentDate:MMM d, yyyy} · {r.PaymentMethod}",
                    self ? "This receipt" : "Already recorded — opens it", self ? BadgeKind.Info : BadgeKind.Warn);
                if (++n >= 15) yield break;
            }
        }
        private static bool IsCash(string method) => string.Equals(method.Trim(), "Cash", StringComparison.OrdinalIgnoreCase);
        /// <summary>The invoice being paid and what other receipts already paid on it (reloaded when the invoice or receipt changes).</summary>
        private void ReloadCollInvoice()
        {
            int key = selectedCollection != null ? -selectedCollection.Id : (cbInvoice.SelectedIndex >= 0 && cbInvoice.SelectedIndex < unpaidInvoices.Count ? unpaidInvoices[cbInvoice.SelectedIndex].Id : 0);
            if (key == collGuideKey) return;
            collGuideKey = key;
            collInvoice = null; collPaidOthers = 0;
            try
            {
                if (selectedCollection != null)
                {
                    collInvoice = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == selectedCollection.InvoiceId);
                    if (collInvoice != null) collPaidOthers = MJdatabase.GetTotalCollectedForInvoice(collInvoice.Id) - selectedCollection.AmountPaid;
                }
                else if (key > 0)
                {
                    collInvoice = unpaidInvoices[cbInvoice.SelectedIndex];
                    collPaidOthers = MJdatabase.GetTotalCollectedForInvoice(collInvoice.Id);
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            // Hard cap: the box can't go above the balance.
            if (collInvoice != null)
            {
                decimal cap = Math.Max(0, collInvoice.NetAmount - collPaidOthers);
                if (cap >= numAmountPaid.Minimum)
                {
                    if (numAmountPaid.Value > cap) numAmountPaid.Value = cap;
                    numAmountPaid.Maximum = cap;
                }
                lblCollCap.Text = $"Most you can record: ₱{cap:N2} (the invoice balance)";
            }
            else
            {
                numAmountPaid.Maximum = 100000000;
                lblCollCap.Text = "";
            }
        }
        private List<PaymentCheck> ComputeCollChecks()
        {
            var checks = new List<PaymentCheck>();
            bool editing = selectedCollection != null;
            var inv = collInvoice;
            checks.Add(new PaymentCheck(inv == null ? (editing ? "The invoice of this receipt was not found" : "Pick the invoice being paid") : $"Invoice {inv.InvoiceNumber} · {inv.ClientName}", inv != null));
            decimal amt = numAmountPaid.Value;
            if (inv != null)
            {
                bool issued = !RowMenu.Is(inv.Status, "Draft") && inv.NetAmount > 0;
                checks.Add(new PaymentCheck(issued ? "Invoice is issued with an amount" : "Invoice is still a draft without an amount — finish it in Billing first", issued));
                decimal balance = Math.Max(0, inv.NetAmount - collPaidOthers);
                checks.Add(new PaymentCheck(amt <= balance ? $"Not more than the balance (₱{balance:N2})" : $"₱{amt:N2} is more than the balance of ₱{balance:N2}", amt <= balance));
                bool dateOk = inv.IssueDate.Date <= DateTime.Today;
                checks.Add(new PaymentCheck(dateOk ? "Payment date (today) is on or after the invoice date" : $"Invoice is dated {inv.IssueDate:MMM d, yyyy}, after today", dateOk));
            }
            checks.Add(new PaymentCheck(amt > 0 ? "Amount is more than ₱0.00" : "Enter the amount paid", amt > 0));
            string method = cbPaymentMethod.Text.Trim();
            checks.Add(new PaymentCheck(method.Length > 0 ? $"Payment method: {method}" : "Choose the payment method", method.Length > 0));
            string refNo = txtRefNo.Text.Trim();
            if (method.Length > 0 && !IsCash(method))
                checks.Add(new PaymentCheck(refNo.Length > 0 ? "Reference / cheque no. entered" : $"Enter the reference / cheque no. for {method}", refNo.Length > 0));
            if (refNo.Length > 0)
            {
                var dup = allReceipts.FirstOrDefault(r => (selectedCollection == null || r.Id != selectedCollection.Id) && string.Equals(r.ReferenceNumber?.Trim(), refNo, StringComparison.OrdinalIgnoreCase));
                checks.Add(new PaymentCheck(dup == null ? "Reference no. not used before" : $"Reference {refNo} is already on receipt {dup.ReceiptNumber} ({dup.PaymentDate:MMM d, yyyy})", dup == null));
            }
            return checks;
        }
        private void UpdateCollGuide()
        {
            if (collChecklist == null || !pnlForm.Visible) return;
            ReloadCollInvoice();
            var checks = ComputeCollChecks();
            collChecklist.SetChecks(checks);
            int fails = collChecklist.Failed, warns = collChecklist.Warnings;
            var inv = collInvoice;
            decimal amt = numAmountPaid.Value;
            if (inv != null) collStrip.SetTotals(inv.TotalAmount, inv.Deductions, collPaidOthers, Math.Max(0, inv.NetAmount - collPaidOthers - amt), fails, warns);
            else collStrip.SetTotals(0, 0, 0, 0, fails, warns);
            bool editing = selectedCollection != null;
            if (editing) collStepper.SetProgress(fails > 0 ? 3 : 4, fails > 0 ? $"Editing receipt {selectedCollection!.ReceiptNumber}: {fails} to fix — see the checklist." : $"Receipt {selectedCollection!.ReceiptNumber} is valid — Update saves the changes.");
            else if (inv == null) collStepper.SetProgress(unpaidInvoices.Count > 0 ? 1 : 0, unpaidInvoices.Count > 0
                ? $"{unpaidInvoices.Count} invoice{(unpaidInvoices.Count == 1 ? " has" : "s have")} a balance — pick the one being paid."
                : "No invoices are waiting for payment.");
            else if (fails > 0) collStepper.SetProgress(2, $"Review: {fails} item{(fails == 1 ? "" : "s")} to fix — see the checklist at the bottom.");
            else collStepper.SetProgress(4, amt < Math.Max(0, inv.NetAmount - collPaidOthers) ? $"✓ Validated — partial payment; ₱{inv.NetAmount - collPaidOthers - amt:N2} will remain." : "✓ Validated — this payment settles the invoice in full.");
            string method = cbPaymentMethod.Text.Trim();
            FieldMarks.Mark(txtRefNo, checks.Any(c => !c.Passed && c.Text.StartsWith("Reference") || !c.Passed && c.Text.StartsWith("Enter the reference")) ? FieldState.Bad : txtRefNo.Text.Trim().Length > 0 ? FieldState.Ok : FieldState.None);
            FieldMarks.Mark(numAmountPaid, checks.Any(c => !c.Passed && (c.Text.Contains("balance") || c.Text.StartsWith("Enter the amount"))) ? FieldState.Bad : amt > 0 ? FieldState.Ok : FieldState.None);
            bool ok = fails == 0 && !collSaving;
            if (btnSave.Visible) { btnSave.Enabled = ok; ApplyButtonVisualState(btnSave, ok, Pal.C(13, 110, 253), Pal.White); }
            if (btnUpdate.Visible) { btnUpdate.Enabled = ok; ApplyButtonVisualState(btnUpdate, ok, Pal.C(25, 135, 84), Pal.White); }
            lblCollBlocked.Text = fails > 0 ? $"✗ {fails} to fix before saving" : "";
            lblCollBlocked.Visible = fails > 0;
        }
        private bool CollGateBeforeSave()
        {
            if (collSaving) return false;
            if (collChecklist == null) return true;
            collGuideKey = int.MinValue; // re-read the balance from the database
            UpdateCollGuide();
            if (collChecklist.Failed > 0)
            {
                MessageBox.Show(FindForm(), "This payment can't be saved yet:\n\n• " + string.Join("\n• ", collChecklist.Checks.Where(c => !c.Passed).Select(c => c.Text)), "Check the payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                formKit?.ScrollTo(collChecklist);
                return false;
            }
            collSaving = true;
            btnSave.Enabled = false;
            btnUpdate.Enabled = false;
            return true;
        }
        private void CollSaveFinished()
        {
            collSaving = false;
            if (pnlForm.Visible) UpdateCollGuide(); else SetButtonStates(selectedCollection != null);
        }
    }
}
