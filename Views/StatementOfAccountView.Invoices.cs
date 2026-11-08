using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6, 2026: a statement groups the service invoices of its trips (one per trip, typed SI No. in Billing). Its total is the
    /// invoices' total, and it is collected with one receipt. Statements saved before (with their own SI invoice) keep the old rules.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private int legacyCacheId = -1;
        private bool legacyCacheValue;

        /// <summary>True for a statement saved before Nov 6 that made its own SI invoice.</summary>
        private bool IsLegacySoa()
        {
            int id = CurrentSoaId;
            if (id <= 0) return false;
            if (legacyCacheId != id)
            {
                try { legacyCacheValue = MJdatabase.SoaHasOwnInvoice(id); } catch (Exception ex) { ErrLog.Swallowed(ex); legacyCacheValue = false; }
                legacyCacheId = id;
            }
            return legacyCacheValue;
        }
        private static bool IsLegacySoa(int soaId)
        {
            try { return soaId > 0 && MJdatabase.SoaHasOwnInvoice(soaId); } catch (Exception ex) { ErrLog.Swallowed(ex); return false; }
        }

        private static int RowSpecialTripId(DataGridViewRow r)
            => r.DataGridView != null && r.DataGridView.Columns.Contains("colSpecialTrip") && int.TryParse(r.Cells["colSpecialTrip"].Value?.ToString(), out int x) ? x : 0;

        /// <summary>The service invoice of each trip on the statement, and the trips that can't go on a statement yet (with why).</summary>
        private (List<TripInvoice> Invoices, List<string> Missing) LineInvoices(List<TripInvoice> all, List<DataGridViewRow> rows)
        {
            var invs = new List<TripInvoice>();
            var missing = new List<string>();
            var keys = rows.Select(r => (Del: RowDeliveryId(r), Sp: RowSpecialTripId(r))).Where(k => k.Del > 0 || k.Sp > 0).Distinct().ToList();
            foreach (var (d, s) in keys)
            {
                var ti = MJdatabase.TripInvoiceFor(all, d, d > 0 ? 0 : s);
                string label = d > 0 ? (guideDeliveries.TryGetValue(d, out var del) ? del.DispatchNumber : $"dispatch #{d}")
                                     : (guideSpecials.TryGetValue(s, out var sp) ? sp.OrderReference : $"special trip #{s}");
                var why = MJdatabase.WhyTripNotReadyForSoa(ti, CurrentSoaId);
                if (why != null) missing.Add($"{label}: {why}");
                else if (!invs.Any(i => i.Id == ti!.Id)) invs.Add(ti!);
            }
            return (invs, missing);
        }

        /// <summary>
        /// The totals of the service invoices behind the statement's lines: a statement's total IS its invoices' total (amounts are corrected on the
        /// invoice in Billing, never typed here). Null while there are no lines, a line has no usable invoice, or it is an old statement.
        /// </summary>
        private (decimal Freight, decimal Fuel)? InvoiceTotalsOfLines()
        {
            if (IsLegacySoa() || guideTripInvoices.Count == 0) return null;
            var rows = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            if (rows.Count == 0) return null;
            var (invs, missing) = LineInvoices(guideTripInvoices, rows);
            if (missing.Count > 0 || invs.Count == 0) return null;
            return (invs.Sum(i => i.Freight), invs.Sum(i => i.Fuel));
        }

        /// <summary>Checklist lines for the statement's service invoices (replaces the old "SI no." checks).</summary>
        private void AddInvoiceChecks(List<PaymentCheck> checks, List<DataGridViewRow> rows)
        {
            var (invs, missing) = LineInvoices(guideTripInvoices, rows);
            string list = string.Join(", ", invs.Select(i => i.Number));
            if (txtSalesInvoiceNo != null && txtSalesInvoiceNo.Text != list) txtSalesInvoiceNo.Text = list;
            if (rows.Count == 0) return;
            checks.Add(new PaymentCheck(missing.Count == 0
                    ? $"Every trip has its service invoice ({invs.Count}: {list})"
                    : $"{missing.Count} trip(s) can't go on a statement yet: {string.Join("; ", missing.Take(3))}{(missing.Count > 3 ? " …" : "")}. Make each trip's service invoice in Billing first (type its SI No.).",
                missing.Count == 0));
            if (invs.Count > 0)
            {
                decimal net = SoaTotals.From(invs.Sum(i => i.Freight), invs.Sum(i => i.Fuel)).Due;
                bool same = Math.Abs(net - numTotalAmountDue.Value) <= 0.05m;
                checks.Add(new PaymentCheck(same ? $"Total due matches its service invoices (PHP {net:N2})"
                    : $"Total due PHP {numTotalAmountDue.Value:N2} is not its service invoices' total (PHP {net:N2}); Save uses the invoices' totals", true, Warning: !same));
            }
        }

        /// <summary>
        /// Before saving: every trip must have its service invoice, and the statement's total becomes the invoices' total
        /// (asks first when the lines worked out to something else). False = don't save.
        /// </summary>
        private bool UseInvoiceTotals()
        {
            var rows = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            var (invs, missing) = LineInvoices(MJdatabase.GetTripInvoices(), rows);
            if (missing.Count > 0 || invs.Count == 0)
            {
                MessageBox.Show(FindForm(), "This statement can't be saved yet:\n\n• " + (missing.Count > 0 ? string.Join("\n• ", missing) : "None of its trips has a service invoice.") +
                    "\n\nEach trip needs its service invoice first: Billing → pick the trip → type the SI No.", "Service invoice needed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var t = SoaTotals.From(invs.Sum(i => i.Freight), invs.Sum(i => i.Fuel));
            decimal net = t.Due;
            if (Math.Abs(net - numTotalAmountDue.Value) <= 0.05m) return true;
            var ans = MessageBox.Show(FindForm(),
                $"The service invoices on this statement ({string.Join(", ", invs.Select(i => i.Number))}) work out to PHP {net:N2} (freight PHP {t.TotalSales:N2}, fuel surcharge PHP {t.Fuel:N2}, less withholding tax PHP {t.Withholding:N2}).\n" +
                $"The statement shows PHP {numTotalAmountDue.Value:N2}.\n\nA statement's total is worked out from its service invoices. Use the invoices' figures and save?",
                "Statement total", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (ans != DialogResult.Yes) return false;
            isCalculatingTaxes = true;
            try
            {
                SetNum(numTotalSales, t.TotalSales);
                SetNum(numNetOfVat, t.NetOfVat);
                SetNum(numVat, t.Vat);
                SetNum(numFuelSurchargeTotal, t.Fuel);
                SetNum(numWithholdingTax, t.Withholding);
                SetNum(numTotalAmountDue, t.Due);
            }
            finally { isCalculatingTaxes = false; }
            if (SoaTotalsAdjusted(out _, out _) && txtSoaAdjustReason.Text.Trim().Length < MinAdjustReasonLength)
                txtSoaAdjustReason.Text = $"Totals follow the service invoices {string.Join(", ", invs.Select(i => i.Number))}.";
            UpdateItemsSummary();
            return true;
        }
    }
}
