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
    /// Nov 6, 2026: one collection receipt per statement (all its service invoices) or per invoice that is not on a statement,
    /// always for the full amount (no partial payments). The receipt number is typed: 4 to 8 digits, never repeated.
    /// </summary>
    public partial class CollectionView
    {
        private List<PayTarget> payTargets = new();
        private Label lblReceiptNo = null!;
        private TextBox txtReceiptNo = null!;
        /// <summary>Live check of the typed Collection Receipt No.: unique in any spelling, same-digits warning with a confirmation tick (plan 17).</summary>
        private NumberFieldGuard receiptNumberGuard = null!;
        /// <summary>The invoice (or statement total) of the receipt being edited, for the checklist.</summary>
        private Invoice? editTargetInvoice;

        private Control[] BuildReceiptNoField()
        {
            lblReceiptNo = new Label { Text = "Collection Receipt No. *", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, 2) };
            txtReceiptNo = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10.5F), MaxLength = 8, PlaceholderText = "4 to 8 digits from the printed receipt (e.g. 0006)" };
            txtReceiptNo.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtReceiptNo.TextChanged += (s, e) =>
            {
                // Pasted text: keep the digits only.
                string digits = new string(txtReceiptNo.Text.Where(char.IsDigit).ToArray());
                if (digits != txtReceiptNo.Text) { int at = txtReceiptNo.SelectionStart; txtReceiptNo.Text = digits; txtReceiptNo.SelectionStart = Math.Min(at, digits.Length); }
                ScheduleCollGuide();
            };
            receiptNumberGuard = new NumberFieldGuard(txtReceiptNo, NumberKind.CR, null, () => selectedCollection?.ReceiptNumber);
            receiptNumberGuard.Changed += () => ScheduleCollGuide();
            return new Control[] { lblReceiptNo, txtReceiptNo };
        }

        private Label lblPaymentDate = null!;
        private DateTimePicker dtpPaymentDate = null!;

        /// <summary>"Payment date": the day the money was received. Today by default, earlier allowed, never in the future.</summary>
        private Control[] BuildPaymentDateField()
        {
            lblPaymentDate = new Label { Text = "Payment date (day the money was received) *", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, 2) };
            dtpPaymentDate = new DateTimePicker { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10.5F), Format = DateTimePickerFormat.Long };
            dtpPaymentDate.CapAtToday();
            dtpPaymentDate.SetSafe(DateTime.Today);
            dtpPaymentDate.ValueChanged += (s, e) => ScheduleCollGuide();
            return new Control[] { lblPaymentDate, dtpPaymentDate };
        }

        /// <summary>Puts the picker on a day (today at most). Its upper limit moves with the date, so it still works when the screen was left open past midnight.</summary>
        private void SetPaymentDate(DateTime day)
        {
            if (dtpPaymentDate == null) return;
            dtpPaymentDate.SetSafe(day.Date);
        }

        /// <summary>A stand-in invoice for one pay target (a statement adds up its invoices), so the checklist and summary strip can show it.</summary>
        private static Invoice TargetAsInvoice(PayTarget t) => new Invoice
        {
            Id = t.IsSoa ? -t.SoaId : t.Invoices[0].Id,
            InvoiceNumber = t.Title,
            SoaId = t.SoaId,
            DeliveryId = t.IsSoa ? 0 : t.Invoices[0].DeliveryId,
            ClientName = t.Client,
            IssueDate = t.IssueDate,
            DueDate = t.DueDate,
            TotalAmount = t.Gross,
            FuelSurcharge = t.Fuel,
            Deductions = t.Deductions, // the withholding tax the statement shared out to its invoices (0 on an invoice alone)
            NetAmount = t.Net,
            Status = PayStatus.Unpaid
        };

        private void LoadPayTargets()
        {
            payTargets = MJdatabase.GetPayTargets();
            unpaidInvoices = payTargets.Select(TargetAsInvoice).ToList();
            cbInvoice.Items.Clear();
            foreach (var t in payTargets)
            {
                string overdue = t.DueDate.Date < DateTime.Today ? " [Overdue]" : ""; // shown from the due date (never a stored status)
                cbInvoice.Items.Add($"{t.Title} - {t.Client} (PHP {t.Net:N2}){overdue}");
            }
            cbInvoice.SelectedIndex = -1; cbInvoice.Text = "";
        }

        /// <summary>One list row per receipt number: a statement's receipt (one row per invoice in the database) is shown once with its total.</summary>
        private static List<CollectionReceipt> GroupReceipts(List<CollectionReceipt> rows)
        {
            Dictionary<int, string> soaNumbers;
            try { soaNumbers = MJdatabase.GetStatementsOfAccount().GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().SoaNumber); }
            catch (Exception ex) { ErrLog.Swallowed(ex); soaNumbers = new(); }
            var list = new List<CollectionReceipt>();
            foreach (var g in rows.GroupBy(r => (r.ReceiptNumber ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var items = g.OrderBy(r => r.Id).ToList();
                var first = items[0];
                if (items.Count == 1) { list.Add(first); continue; }
                int soaId = items.Select(r => r.SoaId).FirstOrDefault(x => x > 0);
                string soaNo = soaId > 0 && soaNumbers.TryGetValue(soaId, out var n) ? n : "";
                list.Add(new CollectionReceipt
                {
                    Id = first.Id,
                    ReceiptNumber = first.ReceiptNumber,
                    InvoiceId = first.InvoiceId,
                    InvoiceNumber = (soaNo.Length > 0 ? soaNo + ": " : "") + string.Join(", ", items.Select(r => r.InvoiceNumber)),
                    PaymentDate = first.PaymentDate,
                    AmountPaid = items.Sum(r => r.AmountPaid),
                    PaymentMethod = first.PaymentMethod,
                    ReferenceNumber = first.ReferenceNumber,
                    Notes = first.Notes,
                    SoaId = soaId
                });
            }
            return list.OrderByDescending(r => r.Id).ToList();
        }

        /// <summary>The invoice(s) a saved receipt paid, added up, for the checklist while editing.</summary>
        private Invoice? EditInvoiceFor(CollectionReceipt cr)
        {
            try
            {
                var rows = MJdatabase.GetReceiptRows(cr.ReceiptNumber);
                var ids = rows.Select(r => r.InvoiceId).ToHashSet();
                if (ids.Count == 0) ids.Add(cr.InvoiceId);
                var invs = MJdatabase.GetInvoices().Where(i => ids.Contains(i.Id)).ToList();
                if (invs.Count == 0) return null;
                return new Invoice
                {
                    Id = invs[0].Id,
                    InvoiceNumber = invs.Count == 1 ? invs[0].InvoiceNumber : cr.InvoiceNumber,
                    ClientName = invs[0].ClientName,
                    IssueDate = invs.Max(i => i.IssueDate),
                    DueDate = invs.Min(i => i.DueDate),
                    TotalAmount = invs.Sum(i => i.TotalAmount),
                    FuelSurcharge = invs.Sum(i => i.FuelSurcharge),
                    Deductions = invs.Sum(i => i.Deductions),
                    NetAmount = invs.Sum(i => i.NetAmount),
                    Status = invs.All(i => RowMenu.Is(i.Status, "Paid")) ? "Paid" : invs[0].Status
                };
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return null; }
        }

        /// <summary>Opens a new receipt for one listed statement / invoice (from the "ready to collect" banner).</summary>
        private void OpenTarget(Invoice t)
        {
            OpenNewRecordForm();
            int idx = unpaidInvoices.FindIndex(i => i.Id == t.Id);
            if (idx >= 0) cbInvoice.SelectedIndex = idx;
            txtReceiptNo?.Focus();
        }

        /// <summary>"Record payment" from the waiting list: opens a new receipt for that statement (or lone invoice), full amount filled in.</summary>
        private void OpenPayTarget(PayTarget t)
        {
            LoadPayTargets();
            int idx = payTargets.FindIndex(x => t.SoaId > 0 ? x.SoaId == t.SoaId : x.SoaId == 0 && x.Invoices.Count > 0 && t.Invoices.Count > 0 && x.Invoices[0].Id == t.Invoices[0].Id);
            if (idx < 0)
            {
                QuietBox.Show($"{(t.IsSoa ? t.SoaNumber : "SI " + t.Invoices[0].Number)} has nothing left to collect. It may have just been paid.", "Nothing to collect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshCollReady();
                return;
            }
            OpenNewRecordForm();
            cbInvoice.SelectedIndex = idx;
            txtReceiptNo?.Focus();
        }

        private void ShowTargetInfo(PayTarget t)
        {
            string lines = string.Join("; ", t.Invoices.Select(i => $"SI {i.Number} PHP {i.Net:N2}"));
            string trs = TripReportsOf(t);
            string trPart = trs.Length > 0 ? $"{Environment.NewLine}Trip reports: {trs}" : "";
            lblLinkedInvoiceInfo.Text = (t.IsSoa
                ? $"Statement {t.SoaNumber} | {t.Client} | {t.Invoices.Count} invoice(s): {lines}{trPart}{Environment.NewLine}Freight PHP {(t.Gross - t.Fuel):N2} + fuel PHP {t.Fuel:N2} − withholding PHP {t.Deductions:N2} = OVERALL AMOUNT DUE PHP {t.Net:N2}"
                : $"Invoice SI {t.Invoices[0].Number} | {t.Client}{trPart}{Environment.NewLine}Freight PHP {(t.Gross - t.Fuel):N2} + fuel PHP {t.Fuel:N2} − withholding PHP {t.Deductions:N2} = OVERALL AMOUNT DUE PHP {t.Net:N2}");
            ShowVerifyFor(t); // Nov 10: the overall amount must be ticked as checked before the receipt can be issued
            FitCollectionBoxes();
            if (t.Net > numAmountPaid.Maximum) numAmountPaid.Maximum = t.Net * 2;
            numAmountPaid.SetSafe(Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, t.Net)));
        }

        /// <summary>Checks shared by Save and Update. Returns false (after telling the person) when something is missing.</summary>
        private bool CheckReceiptForm(bool editing)
        {
            var valErrors = new List<string>();
            Control? firstInvalid = null;
            ValidationHelper.ClearErrors(pnlForm);
            if (!editing && cbInvoice.SelectedIndex < 0)
            {
                ValidationHelper.ApplyValidationState(cbInvoice, false, "Pick the statement or invoice being paid.");
                valErrors.Add("Pick the statement or invoice being paid.");
                firstInvalid ??= cbInvoice;
            }
            var rWhy = MJdatabase.CheckReceiptNumber(txtReceiptNo.Text, editing ? selectedCollection?.ReceiptNumber : null);
            if (rWhy == null && receiptNumberGuard.TickMissing) rWhy = "Tick the confirmation under the Collection Receipt No. (same digits as another document).";
            if (rWhy != null) { ValidationHelper.ApplyValidationState(txtReceiptNo, false, rWhy); valErrors.Add(rWhy); firstInvalid ??= txtReceiptNo; }
            DateTime issued = editing ? (editTargetInvoice?.IssueDate ?? DateTime.MinValue) : (cbInvoice.SelectedIndex >= 0 && cbInvoice.SelectedIndex < payTargets.Count ? payTargets[cbInvoice.SelectedIndex].IssueDate : DateTime.MinValue);
            var dateWhy = MJdatabase.CheckPaymentDate(dtpPaymentDate.Value.Date, issued);
            if (dateWhy != null) { ValidationHelper.ApplyValidationState(dtpPaymentDate, false, dateWhy); valErrors.Add(dateWhy); firstInvalid ??= dtpPaymentDate; }
            string method = cbPaymentMethod.Text.Trim();
            if (method.Length == 0) { ValidationHelper.ApplyValidationState(cbPaymentMethod, false, "Payment method is required."); valErrors.Add("Payment method is required."); firstInvalid ??= cbPaymentMethod; }
            string refNum = txtRefNo.Text.Trim();
            if (method.Length > 0 && !IsCash(method) && refNum.Length == 0)
            {
                ValidationHelper.ApplyValidationState(txtRefNo, false, "Reference / cheque no. is required for " + method + ".");
                valErrors.Add($"A reference / cheque number is required for {method} payments.");
                firstInvalid ??= txtRefNo;
            }
            string oldRef = editing ? (selectedCollection?.ReferenceNumber ?? "").Trim() : "";
            if (refNum.Length > 0 && !string.Equals(refNum, oldRef, StringComparison.Ordinal) && !DailyRules.PaymentReferenceOk(refNum)) // Nov 12: a reference already on a saved receipt is never refused
            {
                ValidationHelper.ApplyValidationState(txtRefNo, false, DailyRules.PaymentReferenceMessage);
                valErrors.Add(DailyRules.PaymentReferenceMessage);
                firstInvalid ??= txtRefNo;
            }
            if (refNum.Length > 0)
            {
                string self = editing ? (selectedCollection?.ReceiptNumber ?? "").Trim() : "\u0000";
                var dup = MJdatabase.GetCollectionReceipts().FirstOrDefault(r => !string.Equals((r.ReceiptNumber ?? "").Trim(), self, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.ReferenceNumber?.Trim(), refNum, StringComparison.OrdinalIgnoreCase));
                if (dup != null) { valErrors.Add($"Payment reference '{refNum}' is already on receipt #{dup.ReceiptNumber} ({dup.PaymentDate:MM/dd/yyyy}). The same payment can't be recorded twice."); firstInvalid ??= txtRefNo; }
            }
            return ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid);
        }

        private void SaveNewReceipt()
        {
            if (!CheckReceiptForm(editing: false)) return;
            var target = payTargets[cbInvoice.SelectedIndex];
            string method = cbPaymentMethod.Text.Trim();
            MJdatabase.SaveLookupItem("Payment Method", method);
            string rno = txtReceiptNo.Text.Trim();
            if (chkVerifyAmount != null && !chkVerifyAmount.Checked) { MessageBox.Show(FindForm(), "Tick \"I checked the overall amount\" first. The receipt is issued only for the amount you checked.", "Verify the overall amount", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var rows = MJdatabase.SaveOneReceipt(rno, target, method, txtRefNo.Text.Trim(), DailyRules.CollapseSpaces(txtNotes.Text), dtpPaymentDate.Value.Date, receiptNumberGuard.CrossTypeConfirmed, verifiedAmount: verifyAmount);
            ClearFields();
            LoadData();
            SetFormVisibility(false);
            // Nov 6 workflow: the statement (or invoice) and its invoices are Paid now and move to Finance History.
            string what = target.IsSoa
                ? $"Collection Receipt {rno}: PHP {target.Net:N2} for statement {target.SoaNumber}. The statement, its {rows.Count} service invoice(s) ({string.Join(", ", target.Invoices.Select(i => i.Number))}) and its trip reports are now billed and PAID. The billing process is complete."
                : $"Collection Receipt {rno}: PHP {target.Net:N2} for service invoice {target.Invoices[0].Number}. The invoice and its trip report are now billed and PAID. The billing process is complete.";
            var print = MessageBox.Show(FindForm(), what + "\n\nPayment recorded in full. Do you want to print the Official Collection Receipt now?", "Payment Completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (print == DialogResult.Yes && rows.Count > 0) PrintHelper.PrintOfficialCollectionReceipt(rows[0]); // the slip (normal or special trip) is chosen from what was paid
        }

        private void UpdateSelectedReceipt()
        {
            var sel = selectedCollection;
            if (sel == null)
            {
                MessageBox.Show("Please select a collection receipt to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!CheckReceiptForm(editing: true)) return;
            string method = cbPaymentMethod.Text.Trim();
            MJdatabase.SaveLookupItem("Payment Method", method);
            MJdatabase.UpdateReceiptDetails(sel.ReceiptNumber, txtReceiptNo.Text.Trim(), method, txtRefNo.Text.Trim(), DailyRules.CollapseSpaces(txtNotes.Text), dtpPaymentDate.Value.Date, receiptNumberGuard.CrossTypeConfirmed);
            QuietBox.Show($"Collection Receipt {txtReceiptNo.Text.Trim()} is updated. (The amount is the full amount paid and does not change.)", "Receipt updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearFields();
            LoadData();
            SetFormVisibility(false);
        }
    }
}
