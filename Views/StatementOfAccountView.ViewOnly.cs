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
    /// Nov 8: a statement that can't be edited (paid in full, cancelled, or frozen) still opens, so its trips, figures and history can be seen.
    /// The same page opens read-only: every field and button that would change it is locked, nothing can be saved, and a history card lists
    /// what happened to it (saved, changed, each payment received, cancelled).
    /// </summary>
    public partial class StatementOfAccountView
    {
        private bool soaViewOnly;
        private Panel? soaHistoryCard;
        private DataGridView? gridSoaHistory;
        private Label? lblSoaViewOnly;
        private readonly List<Action> soaViewOnlyRestore = new List<Action>();

        /// <summary>The history card (hidden until a statement is opened view-only); built with the form's right-hand column.</summary>
        private void BuildSoaHistoryCard()
        {
            soaHistoryCard = new Panel { Dock = DockStyle.Top, Height = Dpi(360), Visible = false, BackColor = AppTheme.Surface, Padding = new Padding(Dpi(16), Dpi(12), Dpi(16), Dpi(12)) };
            var title = new Label { Dock = DockStyle.Top, Height = Dpi(26), Text = "STATEMENT HISTORY  ·  VIEW ONLY", Font = new Font("Segoe UI Semibold", 8.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false };
            lblSoaViewOnly = new Label { Dock = DockStyle.Top, Height = Dpi(64), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextSecondary, UseMnemonic = false };
            gridSoaHistory = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BorderStyle = BorderStyle.None, BackgroundColor = AppTheme.Surface,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 9.25F)
            };
            gridSoaHistory.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
            gridSoaHistory.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            gridSoaHistory.DefaultCellStyle.BackColor = AppTheme.Surface;
            gridSoaHistory.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            gridSoaHistory.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            gridSoaHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "hWhen", HeaderText = "When", FillWeight = 24, MinimumWidth = 90 });
            gridSoaHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "hEvent", HeaderText = "What happened", FillWeight = 30, MinimumWidth = 110 });
            gridSoaHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "hDetail", HeaderText = "Details", FillWeight = 34, MinimumWidth = 110 });
            gridSoaHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "hAmount", HeaderText = "Amount", FillWeight = 20, MinimumWidth = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            soaHistoryCard.Controls.Add(gridSoaHistory);
            soaHistoryCard.Controls.Add(lblSoaViewOnly);
            soaHistoryCard.Controls.Add(title);
        }

        /// <summary>Fills the history card: when the statement was saved or changed, each payment received, and its cancellation.</summary>
        private void FillSoaHistory(StatementOfAccount soa, string why)
        {
            if (gridSoaHistory == null || lblSoaViewOnly == null) return;
            var rows = new List<(DateTime When, string Event, string Detail, string Amount)>();
            decimal paid = 0m;
            try
            {
                foreach (var a in MJdatabase.GetAuditTrail(new[] { "StatementsOfAccount" }, null, 2000).Where(a => a.RecordId == soa.Id))
                {
                    string ev = string.Equals(a.ActionType, "ADD", StringComparison.OrdinalIgnoreCase) ? "Statement saved" : string.Equals(a.ActionType, "UPDATE", StringComparison.OrdinalIgnoreCase) ? "Statement changed" : a.ActionType;
                    string note = (a.ReasonOrNotes ?? "").Trim();
                    rows.Add((a.Timestamp, ev, $"by {a.Username}" + (note.Length > 0 ? " · " + (note.Length > 110 ? note.Substring(0, 110) + "…" : note) : ""), ""));
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            try
            {
                var invIds = MJdatabase.GetInvoicesForSoa(soa.Id).Select(i => i.Id).ToHashSet();
                foreach (var g in MJdatabase.GetCollectionReceipts().Where(r => r.SoaId == soa.Id || invIds.Contains(r.InvoiceId)).GroupBy(r => r.ReceiptNumber))
                {
                    var first = g.OrderBy(r => r.Id).First();
                    decimal sum = g.Sum(r => r.AmountPaid);
                    paid += sum;
                    string by = (first.PaymentMethod ?? "").Trim() + (string.IsNullOrWhiteSpace(first.ReferenceNumber) ? "" : " · ref " + first.ReferenceNumber.Trim());
                    rows.Add((g.Min(r => r.PaymentDate), $"Payment received · receipt {g.Key}", by, "₱" + sum.ToString("N2")));
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            try
            {
                var ci = MJdatabase.GetSoaCancelInfo(soa.Id);
                if (ci.Any) rows.Add((ci.At ?? DateTime.MinValue, "Statement cancelled", (ci.By.Length > 0 ? "by " + ci.By : "") + (ci.Reason.Length > 0 ? " · " + ci.Reason : ""), ""));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (!rows.Any(r => r.Event == "Statement saved")) rows.Add((soa.IssueDate, "Statement dated", $"{soa.SoaNumber} for {soa.ClientName}", "₱" + soa.TotalAmount.ToString("N2")));
            gridSoaHistory.Rows.Clear();
            foreach (var r in rows.OrderBy(r => r.When))
                gridSoaHistory.Rows.Add(r.When == DateTime.MinValue ? "" : (r.When.TimeOfDay == TimeSpan.Zero ? r.When.ToString("dd-MMM-yyyy") : r.When.ToString("dd-MMM-yyyy h:mm tt")), r.Event, r.Detail, r.Amount);
            lblSoaViewOnly.Text = why + (paid > 0 ? $"\nTotal due ₱{soa.TotalAmount:N2}  ·  received ₱{paid:N2}" : "");
        }

        /// <summary>Locks the page: no field or button that changes the statement works, nothing can be saved. Everything is put back by <see cref="ExitSoaViewOnly"/>.</summary>
        private void EnterSoaViewOnly(StatementOfAccount soa, string why)
        {
            ExitSoaViewOnly();
            soaViewOnly = true;
            foreach (var card in new Control?[] { soaCardClient, soaCardPay, soaCardPicker, soaCardLines, soaCardSign }) if (card != null) LockSoaControls(card);
            gridItems.ReadOnly = true; gridItems.AllowUserToDeleteRows = false; gridItems.AllowUserToAddRows = false;
            soaViewOnlyRestore.Add(() => { gridItems.ReadOnly = false; gridItems.AllowUserToDeleteRows = true; });
            btnSaveSoa.Visible = false; btnDeleteSoa.Visible = false; btnClearForm.Visible = false;
            soaChecklist.Visible = false;
            if (pnlSoaAdjust != null) pnlSoaAdjust.Visible = false;
            if (btnTabInvoices != null) btnTabInvoices.Visible = false;
            if (btnTabLines != null) SoaShowTab(false);
            lblFormHeader.Text = $"Statement  ·  {soa.SoaNumber}  ·  view only";
            FillSoaHistory(soa, why);
            if (soaHistoryCard != null) soaHistoryCard.Visible = true;
            try { UpdateSoaGuide(); } catch (Exception ex) { ErrLog.Swallowed(ex); }   // the strip, the totals block and the tape show the saved figures
        }

        private void ExitSoaViewOnly()
        {
            if (!soaViewOnly && soaViewOnlyRestore.Count == 0) return;
            foreach (var undo in soaViewOnlyRestore) { try { undo(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
            soaViewOnlyRestore.Clear();
            soaViewOnly = false;
            btnSaveSoa.Visible = true;
            soaChecklist.Visible = true;
            if (btnTabInvoices != null) btnTabInvoices.Visible = true;
            if (soaHistoryCard != null) soaHistoryCard.Visible = false;
        }

        private void LockSoaControls(Control parent)
        {
            foreach (Control k in parent.Controls)
            {
                switch (k)
                {
                    case TextBox t:
                        if (!t.ReadOnly) { t.ReadOnly = true; soaViewOnlyRestore.Add(() => t.ReadOnly = false); }
                        break;
                    case DataGridView g:
                        if (!g.ReadOnly) { g.ReadOnly = true; soaViewOnlyRestore.Add(() => g.ReadOnly = false); }
                        break;
                    case ComboBox or DateTimePicker or CheckBox or Button or NumericUpDown:
                        if (k.Enabled) { k.Enabled = false; soaViewOnlyRestore.Add(() => k.Enabled = true); }
                        break;
                    default:
                        LockSoaControls(k);
                        break;
                }
            }
        }
    }
}
