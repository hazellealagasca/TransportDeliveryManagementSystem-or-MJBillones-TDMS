using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 10: the receipt form shows what the billing carried over (statement / invoice, client, invoices, trip reports) and the person must
    /// tick that the overall amount was checked before "Complete / Issue Receipt" is allowed. The checked amount is sent with the save, so the
    /// receipt is only issued for exactly the amount that was verified.
    /// </summary>
    public partial class CollectionView
    {
        private CheckBox chkVerifyAmount = null!;
        /// <summary>The overall amount the person is asked to verify (the picked statement's or invoice's total due); 0 when none is picked.</summary>
        private decimal verifyAmount;

        private void BuildVerifyBox()
        {
            chkVerifyAmount = new CheckBox { AutoSize = false, Dock = DockStyle.Top, Height = 44, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = lblLinkedInvoiceInfo.ForeColor, Padding = new Padding(6, 4, 0, 4), Text = "I checked the overall amount of the linked invoice (pick the statement or invoice first)", Enabled = false, UseVisualStyleBackColor = true, UseMnemonic = false };
            chkVerifyAmount.CheckedChanged += (s, e) => ScheduleCollGuide();
            pnlFormBody.Controls.Add(chkVerifyAmount);
            pnlLinkedInvoice.Height = 74; // the carried-over billing details are longer than one line
        }

        /// <summary>The overall amount of the picked statement / invoice is shown and must be ticked as checked.</summary>
        private void ShowVerifyFor(PayTarget? t)
        {
            if (chkVerifyAmount == null) return;
            chkVerifyAmount.Checked = false;
            verifyAmount = t?.Net ?? 0m;
            chkVerifyAmount.Enabled = t != null;
            chkVerifyAmount.Text = t == null ? "I checked the overall amount of the linked invoice (pick the statement or invoice first)"
                : $"I checked the overall amount: PHP {t.Net:N2} for {(t.IsSoa ? "statement " + t.SoaNumber : "invoice SI " + t.Invoices[0].Number)} is correct";
        }

        /// <summary>The trip report numbers behind a statement / invoice (for the carried-over details).</summary>
        private static string TripReportsOf(PayTarget t)
        {
            try
            {
                var reports = t.IsSoa ? null : MJdatabase.GetTripReportsByDelivery();
                var nos = t.IsSoa
                    ? MJdatabase.GetStatementOfAccountItems(t.SoaId).Select(i => i.TripReportNumber)
                    : t.Invoices.Select(i => reports != null && reports.TryGetValue(i.DeliveryId, out var tr) ? tr.TripReportNumber : "");
                var list = nos.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                return list.Count == 0 ? "" : string.Join(", ", list.Take(6)) + (list.Count > 6 ? ", …" : "");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return ""; }
        }
    }
}
