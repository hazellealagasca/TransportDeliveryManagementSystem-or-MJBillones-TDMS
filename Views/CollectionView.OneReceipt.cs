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
            return new Control[] { lblReceiptNo, txtReceiptNo };
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
            Deductions = t.Deductions,
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

        private void ShowTargetInfo(PayTarget t)
        {
            string lines = string.Join("; ", t.Invoices.Select(i => $"SI {i.Number} PHP {i.Net:N2}"));
            lblLinkedInvoiceInfo.Text = t.IsSoa
                ? $"Statement {t.SoaNumber} | {t.Client} | {t.Invoices.Count} invoice(s): {lines} | Total due: PHP {t.Net:N2}"
                : $"Invoice SI {t.Invoices[0].Number} | {t.Client} | Amount due: PHP {t.Net:N2}";
            if (t.Net > numAmountPaid.Maximum) numAmountPaid.Maximum = t.Net * 2;
            numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, t.Net));
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
            if (rWhy != null) { ValidationHelper.ApplyValidationState(txtReceiptNo, false, rWhy); valErrors.Add(rWhy); firstInvalid ??= txtReceiptNo; }
            string method = cbPaymentMethod.Text.Trim();
            if (method.Length == 0) { ValidationHelper.ApplyValidationState(cbPaymentMethod, false, "Payment method is required."); valErrors.Add("Payment method is required."); firstInvalid ??= cbPaymentMethod; }
            string refNum = txtRefNo.Text.Trim();
            if (method.Length > 0 && !IsCash(method) && refNum.Length == 0)
            {
                ValidationHelper.ApplyValidationState(txtRefNo, false, "Reference / cheque no. is required for " + method + ".");
                valErrors.Add($"A reference / cheque number is required for {method} payments.");
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
            var rows = MJdatabase.SaveOneReceipt(rno, target, method, txtRefNo.Text.Trim(), txtNotes.Text.Trim());
            ClearFields();
            LoadData();
            SetFormVisibility(false);
            // Nov 6 workflow: the statement (or invoice) and its invoices are Paid now and move to Finance History.
            string what = target.IsSoa
                ? $"Collection Receipt {rno}: PHP {target.Net:N2} for statement {target.SoaNumber}. The statement and its {rows.Count} service invoice(s) are now PAID ({string.Join(", ", target.Invoices.Select(i => i.Number))})."
                : $"Collection Receipt {rno}: PHP {target.Net:N2} for service invoice {target.Invoices[0].Number}, which is now PAID.";
            var print = MessageBox.Show(FindForm(), what + "\n\nPayment recorded in full. Do you want to print the Official Collection Receipt now?", "Payment Completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (print == DialogResult.Yes && rows.Count > 0) PrintHelper.PrintOfficialCollectionReceipt(rows[0]); // the slip (normal or special trip) is chosen from what was paid
        }

        private void UpdateSelectedReceipt()
        {
            if (selectedCollection == null)
            {
                MessageBox.Show("Please select a collection receipt to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!CheckReceiptForm(editing: true)) return;
            string method = cbPaymentMethod.Text.Trim();
            MJdatabase.SaveLookupItem("Payment Method", method);
            MJdatabase.UpdateReceiptDetails(selectedCollection.ReceiptNumber, txtReceiptNo.Text.Trim(), method, txtRefNo.Text.Trim(), txtNotes.Text.Trim());
            QuietBox.Show($"Collection Receipt {txtReceiptNo.Text.Trim()} is updated. (The amount is the full amount paid and does not change.)", "Receipt updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearFields();
            LoadData();
            SetFormVisibility(false);
        }
    }
}
