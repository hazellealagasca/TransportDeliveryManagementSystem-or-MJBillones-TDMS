using System;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>The figures at the bottom of a statement of account.</summary>
    public readonly record struct SoaTotalsResult(decimal TotalSales, decimal Vat, decimal NetOfVat, decimal Fuel, decimal Withholding, decimal Due);

    /// <summary>
    /// The one formula for a statement's totals (Nov 12: the fuel surcharge is part of Total Sales). Total sales (VAT inclusive) =
    /// freight + fuel surcharge; Less VAT = sales ÷ 1.12 × 0.12; Net of VAT = sales − VAT; Withholding = Net of VAT × rate;
    /// Total amount due = sales − Withholding (= Net of VAT + VAT − Withholding). The Fuel field reports the fuel part, but TotalSales includes it.
    /// The screen, the save, the print, the invoices and the receipt all use this, so they cannot disagree.
    /// </summary>
    public static class SoaTotals
    {
        /// <summary>The expanded withholding tax rate clients take off a payment (2%).</summary>
        public const decimal DefaultWithholdingRate = 0.02m;

        private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        public static SoaTotalsResult From(decimal freight, decimal fuel, decimal withholdingRate = DefaultWithholdingRate)
        {
            decimal fuelTotal = R(fuel);
            decimal sales = R(freight) + fuelTotal; // the fuel surcharge is part of Total Sales
            decimal vat = R(sales / 1.12m * 0.12m);
            decimal net = sales - vat;
            decimal wht = R(net * withholdingRate);
            return new SoaTotalsResult(sales, vat, net, fuelTotal, wht, sales - wht);
        }

        /// <summary>
        /// Shares a statement's withholding out over its invoices (in proportion to each one's freight plus fuel surcharge) so the shares add up to it exactly.
        /// The last invoice takes the leftover centavos.
        /// </summary>
        public static decimal[] Share(decimal totalWithholding, decimal[] freights)
        {
            var shares = new decimal[freights.Length];
            decimal sum = 0m; foreach (var f in freights) sum += f;
            if (freights.Length == 0) return shares;
            if (sum <= 0m) { shares[freights.Length - 1] = R(totalWithholding); return shares; }
            decimal given = 0m;
            for (int i = 0; i < freights.Length - 1; i++) { shares[i] = R(totalWithholding * freights[i] / sum); given += shares[i]; }
            shares[freights.Length - 1] = R(totalWithholding) - given;
            return shares;
        }
    }
}
