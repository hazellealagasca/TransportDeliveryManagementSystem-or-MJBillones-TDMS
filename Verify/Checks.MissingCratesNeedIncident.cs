using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 18: missing crates (a store returned fewer than delivered) must be answered in step 3: Yes with an incident. Blocks the final save, shows a hint, and step 3 is not done until then.</summary>
    [Check]
    public static string MissingCrates_MustBeAnsweredInIncidentStep()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        return WithTripReportView((view, host, problems) =>
        {
            using var boxes = new P4Boxes();
            P4Fill(view, no, new[] { "8:42 PM", "9:10 PM" }, new[] { "9:00 PM", "9:30 PM" }, "7:30 PM", "8:00 PM");
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            var row = grid.Rows.Cast<DataGridViewRow>().First(r => !r.IsNewRow);
            int loadG = int.Parse(row.Cells["colLoadG"].Value?.ToString() ?? "0");
            row.Cells["colRetG"].Value = Math.Max(0, loadG - 2).ToString();
            typeof(TripReportView).GetMethod("RecalculateTotalsFromStoreGrid", NonPub)!.Invoke(view, null);
            typeof(TripReportView).GetMethod("UpdateTripCrateSummary", NonPub)!.Invoke(view, null);
            Pump(200);

            var blank = typeof(TripReportView).GetMethod("BlankFormChecks", NonPub)!;
            string Listed() => string.Join(" | ", ((System.Collections.IEnumerable)blank.Invoke(view, null)!).Cast<object>().Select(o => o.ToString() ?? ""));
            var hint = FieldOf<Label>(view, "lblIncHint")!;
            var yes = FieldOf<RadioButton>(view, "rbIncYes")!;
            if (!Listed().Contains("crate(s) are missing")) problems.Add("missing crates with step 3 = No were not refused: " + Listed());
            if (!hint.Visible || !hint.Text.StartsWith("Missing crates on this trip")) problems.Add("the live hint is not shown: '" + hint.Text + "'");
            var done = typeof(TripReportView).GetMethod("StepIncidentDone", NonPub)!;
            if ((bool)done.Invoke(view, null)!) problems.Add("step 3 shows done while crates are missing and no incident is added");

            yes.Checked = true; Pump(200);
            if (!Listed().Contains("crate(s) are missing")) problems.Add("Yes with no incident line was not refused: " + Listed());

            var cause = FieldOf<TextBox>(view, "txtIncCause")!; var qty = FieldOf<NumericUpDown>(view, "numIncQty")!; var cost = FieldOf<NumericUpDown>(view, "numIncCost")!;
            cause.Text = "Missing crates"; qty.SetSafe(2); cost.SetSafe(0);
            bool added = (bool)typeof(TripReportView).GetMethod("AddIncidentFromFields", NonPub)!.Invoke(view, new object[] { false })!;
            Pump(200);
            if (!added) problems.Add("the incident line was not added");
            if (Listed().Contains("crate(s) are missing")) problems.Add("the message stays after an incident was added: " + Listed());
            if (hint.Text.StartsWith("Missing crates on this trip")) problems.Add("the hint stays after an incident was added");
            if (!(bool)done.Invoke(view, null)!) problems.Add("step 3 is not done after the incident was added");
            return "";
        });
    }
}
