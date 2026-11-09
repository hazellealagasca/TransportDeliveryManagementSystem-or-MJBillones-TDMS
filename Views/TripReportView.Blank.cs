using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class TripReportView
    {
        /// <summary>Nov 6: the form starts blank, so the times and the sign-off names must be typed before saving.</summary>
        private List<(string Message, Control Field)> BlankFormChecks()
        {
            var list = new List<(string, Control)>();
            Control timeField(Control box) => box.Visible ? box : (Control?)gridStores ?? box;
            if (string.IsNullOrWhiteSpace(txtArrivalTime.Text))
                list.Add(("Store arrival time is required: type the In time for the stores.", timeField(txtArrivalTime)));
            if (string.IsNullOrWhiteSpace(txtDepartureTime.Text))
                list.Add(("Store departure time is required: type the Out time for the stores.", timeField(txtDepartureTime)));
            void need(TextBox? t, string name)
            {
                if (t != null && t.Visible && string.IsNullOrWhiteSpace(t.Text)) list.Add(($"{name} is required.", t));
            }
            if (gridStores != null)
            {
                gridStores.EndEdit();
                var noDr = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && DrList.Parse(r.Cells["colDr"].Value?.ToString()).Count == 0)
                    .Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                if (noDr.Count > 0) list.Add(($"A DR (delivery receipt) number is required for every store. Missing for: {string.Join(", ", noDr.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noDr.Count > 6 ? ", …" : "")}.", gridStores));
            }
            if (gridStores != null)
            {
                // Nov 10: a DR number is exactly 8 digits.
                var badDr = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow)
                    .SelectMany(r => DrList.Parse(r.Cells["colDr"].Value?.ToString())).Where(d => !System.Text.RegularExpressions.Regex.IsMatch(d, @"^\d{8}$")).Distinct().ToList();
                if (badDr.Count > 0) list.Add(($"A DR number must be exactly 8 digits (numbers only). Fix: {string.Join(", ", badDr.Take(6))}{(badDr.Count > 6 ? ", …" : "")}.", gridStores));
                // Nov 10: every store needs its time in and time out.
                var noIn = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && string.IsNullOrWhiteSpace(r.Cells["colIn"].Value?.ToString())).Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                var noOut = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && string.IsNullOrWhiteSpace(r.Cells["colOut"].Value?.ToString())).Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                if (noIn.Count > 0) list.Add(($"Time in is required for every store. Missing for: {string.Join(", ", noIn.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noIn.Count > 6 ? ", …" : "")}.", gridStores));
                if (noOut.Count > 0) list.Add(($"Time out is required for every store. Missing for: {string.Join(", ", noOut.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noOut.Count > 6 ? ", …" : "")}.", gridStores));
            }
            if (txtTruckTimeIn != null && string.IsNullOrWhiteSpace(txtTruckTimeIn.Text)) list.Add(("Truck time in (plant gate) is required.", txtTruckTimeIn));
            if (txtTruckTimeOut != null && string.IsNullOrWhiteSpace(txtTruckTimeOut.Text)) list.Add(("Truck time out (plant gate) is required.", txtTruckTimeOut));
            // Nov 10: plant gate times are 24-hour HHMM (e.g. 2042); the sign-off names are real names.
            foreach (var (tb, nm) in new[] { (txtTruckTimeIn, "Truck time in"), (txtTruckTimeOut, "Truck time out") })
                if (tb != null && !string.IsNullOrWhiteSpace(tb.Text) && !GateTimeOk(tb.Text)) list.Add(($"{nm} must be a 24-hour time of 4 digits, like 2042 or 0139.", tb));
            foreach (var (tb, nm) in SignOffBoxes())
                if (tb != null && !string.IsNullOrWhiteSpace(tb.Text) && !SecurityHelper.IsValidName(tb.Text.Trim())) list.Add(($"{nm} must be a name (letters only).", tb));
            need(txtGuardName, "Guard name & sign");
            need(txtDtrApprovedBy, "DTR \"Approved by\"");
            need(txtDtrPreparedBy, "DTR \"Prepared by\"");
            need(txtDrPreparedBy, "DR \"Prepared by\"");
            need(txtDrCheckedBy, "DR \"Checked by\"");
            foreach (var (_, field) in list) ValidationHelper.ApplyValidationState(field, false, "Required.");
            return list;
        }

        private static bool GateTimeOk(string t) => System.Text.RegularExpressions.Regex.IsMatch((t ?? "").Trim(), @"^([01]\d|2[0-3])[0-5]\d$");

        private IEnumerable<(TextBox? Box, string Name)> SignOffBoxes() => new (TextBox?, string)[]
        {
            (txtDtrApprovedBy, "DTR \"Approved by\""), (txtReturnCheckedBy, "Return checked by"), (txtDtrPreparedBy, "DTR \"Prepared by\""),
            (txtDrPreparedBy, "DR \"Prepared by\""), (txtDrCheckedBy, "DR \"Checked by\""), (txtGuardName, "Guard name & sign")
        };

        /// <summary>Plant gate &amp; sign-off boxes check themselves when you leave them; the times take digits only.</summary>
        private void WirePlantGateValidation()
        {
            foreach (var tb in new[] { txtTruckTimeIn, txtTruckTimeOut })
            {
                if (tb == null) continue;
                tb.MaxLength = 4;
                tb.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
                tb.Leave += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text)) { ValidationHelper.ClearErrors(tb); return; }
                    ValidationHelper.ApplyValidationState(tb, GateTimeOk(tb.Text), "Type 4 digits in 24-hour time, like 2042 or 0139.");
                };
            }
            foreach (var (tb, _) in SignOffBoxes())
            {
                if (tb == null) continue;
                var box = tb;
                box.Leave += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(box.Text)) { ValidationHelper.ClearErrors(box); return; }
                    ValidationHelper.ApplyValidationState(box, SecurityHelper.IsValidName(box.Text.Trim()), "Type a name (letters only).");
                };
            }
        }

        /// <summary>Shows the missing fields (if any). True when the form may be saved.</summary>
        private bool PassBlankFormChecks()
        {
            var list = BlankFormChecks();
            return list.Count == 0 || ValidationHelper.ShowValidationSummaryIfAny(this, list.Select(x => x.Message).ToList(), list[0].Field);
        }
    }
}
