using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Several dispatches of one client on one trip report form: same client only, their stores share the table, crates add up,
/// and saving makes one report per dispatch from that dispatch's own rows.
/// </summary>
public static partial class Checks
{
    private const BindingFlags NonPub = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void AddStop(int deliveryId, int seq, string client, string store, int g, int e)
        => Exec(@"INSERT INTO DispatchStops (DeliveryId, StopSequence, ClientName, Destination, GCrate, ECrate, TotalCrates, Status, StoreCode, DrNumbers)
                  VALUES (@d, @s, @c, @st, @g, @e, @t, 'Pending', @code, '')",
            ("@d", deliveryId), ("@s", seq), ("@c", client), ("@st", store), ("@g", g), ("@e", e), ("@t", g + e), ("@code", "S" + deliveryId + seq));

    private static void PickMain(TripReportView view, string dispatchNumber)
    {
        var cb = FieldOf<ComboBox>(view, "cbDelivery")!;
        int idx = cb.Items.Cast<object>().Select(o => o.ToString() ?? "").ToList().FindIndex(t => t.StartsWith(dispatchNumber + " - "));
        if (idx < 0) throw new Exception("dispatch not in the drop-down: " + dispatchNumber);
        cb.SelectedIndex = idx;
        Pump(400);
    }

    private static Delivery DeliveryNo(TripReportView view, string number)
        => ((List<Delivery>)typeof(TripReportView).GetField("allDeliveries", NonPub)!.GetValue(view)!).First(d => d.DispatchNumber == number);

    [Check]
    public static string TripReportSeveral_SameClientOnly_CombinedTable_OneReportPerDispatch()
    {
        Fresh();
        int a1 = Seed.Delivery("CLIENT A"), a2 = Seed.Delivery("CLIENT A"), b1 = Seed.Delivery("CLIENT B");
        SetDate("Deliveries", a1, 3); SetDate("Deliveries", a2, 2); SetDate("Deliveries", b1, 1);
        AddStop(a1, 1, "CLIENT A", "Store A1-1", 6, 3); AddStop(a1, 2, "CLIENT A", "Store A1-2", 4, 2);
        AddStop(a2, 1, "CLIENT A", "Store A2-1", 10, 5);
        AddStop(b1, 1, "CLIENT B", "Store B1-1", 10, 5);
        string nA1 = NumberOf(a1), nA2 = NumberOf(a2), nB1 = NumberOf(b1);
        return WithTripReportView((view, host, problems) =>
        {
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            var summary = FieldOf<Label>(view, "lblExtraSummary");
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            PickMain(view, nA1);
            if (grid.Rows.Count != 2) problems.Add($"the main dispatch should show 2 stores, shows {grid.Rows.Count}");

            var refused = (List<Delivery>)typeof(TripReportView).GetMethod("AddExtraDispatches", NonPub)!.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, nA2), DeliveryNo(view, nB1) } })!;
            if (refused.Count != 1 || refused[0].DispatchNumber != nB1) problems.Add("another company's dispatch was not refused (refused: " + string.Join(",", refused.Select(r => r.DispatchNumber)) + ")");
            if (grid.Rows.Count != 3) problems.Add($"expected 3 stores together, found {grid.Rows.Count}");
            if (!grid.Columns["colDisp"]!.Visible) problems.Add("the Dispatch column is not shown with several dispatches");
            var owners = grid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["colDisp"].Value?.ToString()).ToList();
            if (!owners.SequenceEqual(new[] { nA1, nA1, nA2 })) problems.Add("Dispatch column: " + string.Join(",", owners));
            if (summary?.Text != "2 dispatches · 3 stores · 30 crates") problems.Add($"summary '{summary?.Text}'");

            // this dispatch's stops are collected without the other dispatch's rows
            var collect = typeof(TripReportView).GetMethod("CollectStopsFromGrid", NonPub)!;
            int mine = ((List<DispatchStop>)collect.Invoke(view, new object[] { DeliveryNo(view, nA1) })!).Count;
            if (mine != 2) problems.Add($"the main dispatch collected {mine} stops, expected 2");

            // type the stores' times, receivers and DR numbers (each dispatch gets its own)
            int n = 0;
            foreach (DataGridViewRow r in grid.Rows) { n++; r.Cells["colIn"].Value = $"0{n}:00"; r.Cells["colOut"].Value = $"0{n}:30"; r.Cells["colRep"].Value = $"Maria Santos{n}"; r.Cells["colDr"].Value = (5000 + n).ToString(); }
            typeof(TripReportView).GetMethod("SaveSeveralDispatches", NonPub)!.Invoke(view, new object[] { DeliveryNo(view, nA1) });
            Pump(800);

            long Reports(int d) => Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0 AND Status = 'Completed'", ("@d", d));
            if (Reports(a1) != 1 || Reports(a2) != 1) problems.Add($"expected one completed report each for the two dispatches, found {Reports(a1)} and {Reports(a2)}");
            if (Reports(b1) != 0) problems.Add("the other company's dispatch got a report");
            long Crates(int d) => Long("SELECT COALESCE(DeliveredCrates,0) FROM TripReports WHERE DeliveryId = @d", ("@d", d));
            if (Crates(a1) != 15 || Crates(a2) != 15) problems.Add($"each report should carry its own 15 crates, found {Crates(a1)} and {Crates(a2)}");
            if (Long("SELECT COUNT(DISTINCT TripReportNumber) FROM TripReports WHERE DeliveryId IN (@a, @b)", ("@a", a1), ("@b", a2)) != 2) problems.Add("the two reports share a number");
            if (Long("SELECT COUNT(*) FROM DispatchStops WHERE DeliveryId = @d AND ArrivalTime <> ''", ("@d", a1)) != 2) problems.Add("the main dispatch's stops did not keep their typed times");
            if (Long("SELECT COUNT(*) FROM DispatchStops WHERE DeliveryId = @d AND ArrivalTime <> ''", ("@d", a2)) != 1) problems.Add("the added dispatch's stop did not keep its typed time");
            if (Str("SELECT ArrivalTime FROM TripReports WHERE DeliveryId = @d", ("@d", a2)) != "03:00") problems.Add("the added dispatch's report arrival is not its own first store's time");
            return "";
        });
    }

    [Check]
    public static string TripReportSeveral_Remove_NewPick_ClearForm_DraftBlocked()
    {
        Fresh();
        int a1 = Seed.Delivery("CLIENT A"), a2 = Seed.Delivery("CLIENT A"), b1 = Seed.Delivery("CLIENT B");
        SetDate("Deliveries", a1, 3); SetDate("Deliveries", a2, 2); SetDate("Deliveries", b1, 1);
        AddStop(a1, 1, "CLIENT A", "Store A1-1", 10, 5); AddStop(a2, 1, "CLIENT A", "Store A2-1", 10, 5); AddStop(b1, 1, "CLIENT B", "Store B1-1", 10, 5);
        string nA1 = NumberOf(a1), nA2 = NumberOf(a2), nB1 = NumberOf(b1);
        return WithTripReportView((view, host, problems) =>
        {
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            var extras = (List<Delivery>)typeof(TripReportView).GetField("extraDispatches", NonPub)!.GetValue(view)!;
            var add = typeof(TripReportView).GetMethod("AddExtraDispatches", NonPub)!;
            var remove = typeof(TripReportView).GetMethod("RemoveExtra", NonPub)!;
            var block = typeof(TripReportView).GetMethod("ExtraDraftBlockReason", NonPub)!;
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            PickMain(view, nA1);
            if (block.Invoke(view, null) != null) problems.Add("Save draft is blocked with no added dispatches");
            add.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, nA2) } });
            if (block.Invoke(view, null) == null) problems.Add("Save draft is allowed with several dispatches");

            remove.Invoke(view, new object[] { DeliveryNo(view, nA2) });
            if (extras.Count != 0 || grid.Rows.Count != 1) problems.Add($"after removing: {extras.Count} added, {grid.Rows.Count} stores");
            if (grid.Columns["colDisp"]!.Visible) problems.Add("the Dispatch column stays visible with one dispatch");

            add.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, nA2) } });
            PickMain(view, nB1);   // another main dispatch: the added ones no longer apply
            if (extras.Count != 0) problems.Add("added dispatches were kept after picking another main dispatch");
            if (grid.Rows.Count != 1 || grid.Rows[0].Cells["colDispId"].Value?.ToString() != b1.ToString()) problems.Add($"the table does not show only the new main dispatch ({grid.Rows.Count} rows)");

            PickMain(view, nA1);
            add.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, nA2) } });
            typeof(TripReportView).GetMethod("ClearFields", NonPub)!.Invoke(view, null);
            if (extras.Count != 0) problems.Add("Clear form left added dispatches");
            if (grid.Columns["colDisp"]!.Visible) problems.Add("Clear form left the Dispatch column visible");
            return "";
        });
    }
}
