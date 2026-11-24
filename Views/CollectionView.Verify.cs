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
        private ReceiptDetailsCard linkedCard = null!;
        /// <summary>The overall amount the person is asked to verify (the picked statement's or invoice's total due); 0 when none is picked.</summary>
        private decimal verifyAmount;

        private void BuildVerifyBox()
        {
            chkVerifyAmount = new BigCheckBox { Dock = DockStyle.Top, Height = Dpi(60), Margin = new Padding(0, Dpi(8), 0, 0), Text = "I checked the overall amount of the linked invoice (pick the statement or invoice first)", Enabled = false };
            chkVerifyAmount.CheckedChanged += (s, e) => ScheduleCollGuide();
            chkVerifyAmount.Resize += (s, e) => FitCollectionBoxes();
            pnlFormBody.Controls.Add(chkVerifyAmount);
            // the old grey text box becomes a details card; the label stays (hidden) because it still carries the plain text of each state
            lblLinkedInvoiceInfo.Visible = false;
            pnlLinkedInvoice.BorderStyle = BorderStyle.None;
            pnlLinkedInvoice.Padding = Padding.Empty;
            pnlLinkedInvoice.Margin = new Padding(0, 0, 0, Dpi(8));
            linkedCard = new ReceiptDetailsCard { Dock = DockStyle.Fill };
            pnlLinkedInvoice.Controls.Add(linkedCard);
            linkedCard.BringToFront();
            pnlLinkedInvoice.Height = Dpi(60);
            pnlLinkedInvoice.Resize += (s, e) => FitCollectionBoxes();
        }

        /// <summary>One plain line for the states with no statement picked (nothing chosen, or a saved receipt is open).</summary>
        private void SetLinkedInfo(string labelText, string cardText)
        {
            lblLinkedInvoiceInfo.Text = labelText;
            linkedCard?.ShowMessage(cardText);
            FitCollectionBoxes();
        }

        /// <summary>Adds the special-trip reference under the details card (and to the plain label text).</summary>
        private void AddLinkedNote(string note)
        {
            lblLinkedInvoiceInfo.Text += $"  ·  {note}";
            linkedCard?.SetNote(note);
            FitCollectionBoxes();
        }

        /// <summary>Makes the details box and the "I checked the overall amount" line as tall as their text needs, so nothing is cut off.</summary>
        private void FitCollectionBoxes()
        {
            try
            {
                if (linkedCard != null)
                {
                    int w = pnlLinkedInvoice.ClientSize.Width;
                    if (w > 0) pnlLinkedInvoice.Height = linkedCard.MeasureHeight(w);
                }
                if (chkVerifyAmount is BigCheckBox big && big.Width > 0) big.Height = big.MeasureHeight(big.Width);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
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
            FitCollectionBoxes();
        }

        /// <summary>The trip report numbers behind a statement / invoice (for the carried-over details).</summary>
        private static string TripReportsOf(PayTarget t, int max = 6)
        {
            try
            {
                var reports = t.IsSoa ? null : MJdatabase.GetTripReportsByDelivery();
                var nos = t.IsSoa
                    ? MJdatabase.GetStatementOfAccountItems(t.SoaId).Select(i => i.TripReportNumber)
                    : t.Invoices.Select(i => reports != null && reports.TryGetValue(i.DeliveryId, out var tr) ? tr.TripReportNumber : "");
                var list = nos.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                return list.Count == 0 ? "" : string.Join(", ", list.Take(max)) + (list.Count > max ? ", …" : "");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return ""; }
        }
    }
}
