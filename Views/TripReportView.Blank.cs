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
            need(txtDtrApprovedBy, "DTR \"Approved by\"");
            need(txtDtrPreparedBy, "DTR \"Prepared by\"");
            need(txtDrPreparedBy, "DR \"Prepared by\"");
            need(txtDrCheckedBy, "DR \"Checked by\"");
            foreach (var (_, field) in list) ValidationHelper.ApplyValidationState(field, false, "Required.");
            return list;
        }

        /// <summary>Shows the missing fields (if any). True when the form may be saved.</summary>
        private bool PassBlankFormChecks()
        {
            var list = BlankFormChecks();
            return list.Count == 0 || ValidationHelper.ShowValidationSummaryIfAny(this, list.Select(x => x.Message).ToList(), list[0].Field);
        }
    }
}
