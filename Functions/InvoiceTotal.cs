using System.Collections.Generic;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The one formula for a service invoice's total: the amount (freight tariff) plus the fuel surcharge. The save, the screen and the
    /// invoice register all read it from here, so what is shown is always what is stored. It is NOT the amount the client pays: that is
    /// worked out on the Statement of Account (VAT, then withholding, then total due) and is only ever read from the saved statement.
    /// </summary>
    public static class InvoiceTotal
    {
        public static decimal Of(decimal amount, decimal fuel) => amount + fuel;

        /// <summary>The labelled lines under the invoice form: Amount, + Fuel surcharge, = Invoice total.</summary>
        public static List<(string Label, string Value)> Lines(decimal amount, decimal fuel) => new()
        {
            ("Amount", "₱" + amount.ToString("N2")),
            ("+ Fuel surcharge", "₱" + fuel.ToString("N2")),
            ("= Invoice total", "₱" + Of(amount, fuel).ToString("N2"))
        };

        /// <summary>The statement's saved total due, as one line (shown only when the invoice bills a statement).</summary>
        public static (string Label, string Value) StatementLine(decimal savedTotalDue) =>
            ("Statement total due (after VAT and withholding)", "₱" + savedTotalDue.ToString("N2"));
    }
}
