using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 17: a store's time in / time out is not earlier than the truck's plant gate time in or time out (an overnight gate puts the time out after midnight).</summary>
public static partial class Checks
{
    [Check]
    public static string TripReportTimes_StoreTimeBeforePlantGate_PureRule()
    {
        var bad = new List<string>();
        void expect(string? tin, string? tout, string? store, string? want)
        {
            string? got = DailyRules.StoreTimeBeforePlantGate(tin, tout, store);
            if (got != want) bad.Add($"in [{tin}] out [{tout}] store [{store}]: {got ?? "fine"}, expected {want ?? "fine"}");
        }
        // normal gate 03:00 PM - 03:30 PM
        expect("03:00 PM", "03:30 PM", "02:00 PM", "in");     // earlier than time in
        expect("3:00 PM", "3:30 PM", "02:59 PM", "in");
        expect("03:00 PM", "03:30 PM", "03:15 PM", "out");    // between time in and time out
        expect("03:00 PM", "03:30 PM", "03:29 PM", "out");
        expect("03:00 PM", "03:30 PM", "03:30 PM", null);     // at the time out
        expect("03:00 PM", "03:30 PM", "04:15 PM", null);     // after it
        // overnight gate: in 10:00 PM, out 12:30 AM
        expect("10:00 PM", "12:30 AM", "01:00 AM", null);
        expect("10:00 PM", "12:30 AM", "12:30 AM", null);
        expect("10:00 PM", "12:30 AM", "11:00 PM", "out");
        expect("10:00 PM", "12:30 AM", "12:15 AM", "out");
        expect("10:00 PM", "12:30 AM", "09:00 PM", null);     // earlier than the time in counts as the next day's time (same as 01:00 AM)
        // only one gate time filled / readable: that one is used
        expect("03:00 PM", "", "02:00 PM", "in");
        expect("03:00 PM", "later", "03:10 PM", null);
        expect("", "03:30 PM", "03:15 PM", "out");
        expect("", "03:30 PM", "03:30 PM", null);
        // blank / unreadable ignored
        expect("", "", "02:00 PM", null);
        expect("soon", "later", "02:00 PM", null);
        expect("03:00 PM", "03:30 PM", "", null);
        expect("03:00 PM", "03:30 PM", "later", null);
        expect(null, null, null, null);
        // hint wording
        if (DailyRules.StoreBeforeGateHint("out", "03:00 PM", "3:30 PM") != "Earlier than the truck time out at the plant gate (03:30 PM)") bad.Add("out hint wording");
        if (DailyRules.StoreBeforeGateHint("in", "3:00 PM", "03:30 PM") != "Earlier than the truck time in at the plant gate (03:00 PM)") bad.Add("in hint wording");
        return string.Join("; ", bad);
    }

    [Check]
    public static string TripReportTimes_BlankFormChecks_ListsStoresEarlierThanPlantGate_OvernightGateRespected()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        return WithTripReportView((view, host, problems) =>
        {
            P4Fill(view, no, new[] { "2:00 PM", "3:45 PM" }, new[] { "4:00 PM", "3:15 PM" }, "3:00 PM", "3:30 PM");
            var checks = typeof(TripReportView).GetMethod("BlankFormChecks", NonPub)!;
            string earlier(object? o) => string.Join(" | ", ((List<(string Message, Control Field)>)o!).Select(x => x.Message).Where(m => m.Contains("plant gate times")));
            string msg = earlier(checks.Invoke(view, null));
            string want = "A store's time in and time out can't be earlier than the truck's plant gate times (time in 03:00 PM, time out 03:30 PM). Fix it for: Store A-1 (time in 02:00 PM), Store A-2 (time out 03:15 PM).";
            if (msg != want) problems.Add("message: [" + msg + "]");
            // an overnight gate (in 10:00 PM, out 12:30 AM): 01:00 AM is fine, 11:00 PM is not
            FieldOf<TextBox>(view, "txtTruckTimeIn")!.Text = "10:00 PM";
            FieldOf<TextBox>(view, "txtTruckTimeOut")!.Text = "12:30 AM";
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            grid.Rows[0].Cells["colIn"].Value = "1:00 AM"; grid.Rows[0].Cells["colOut"].Value = "1:30 AM";
            grid.Rows[1].Cells["colIn"].Value = "1:40 AM"; grid.Rows[1].Cells["colOut"].Value = "2:30 AM";
            string over = earlier(checks.Invoke(view, null));
            if (over.Length > 0) problems.Add("overnight gate: after-midnight store times flagged: [" + over + "]");
            grid.Rows[1].Cells["colIn"].Value = "11:00 PM";
            if (!earlier(checks.Invoke(view, null)).Contains("Store A-2 (time in 11:00 PM)")) problems.Add("overnight gate: 11:00 PM not flagged");
            // the live hint marks the cell and clears it when fixed
            var mark = typeof(TripReportView).GetMethod("MarkStoreTimesBeforeTruckIn", NonPub)!;
            mark.Invoke(view, null);
            if (grid.Rows[1].Cells["colIn"].ErrorText != "Earlier than the truck time out at the plant gate (12:30 AM)") problems.Add("cell hint: [" + grid.Rows[1].Cells["colIn"].ErrorText + "]");
            grid.Rows[1].Cells["colIn"].Value = "1:40 AM";
            mark.Invoke(view, null);
            if (grid.Rows[1].Cells["colIn"].ErrorText.Length != 0) problems.Add("cell hint not cleared: [" + grid.Rows[1].Cells["colIn"].ErrorText + "]");
            return "";
        });
    }
}
