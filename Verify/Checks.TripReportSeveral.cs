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
        SetDate("Deliveries", a1, 3); SetDate("Deliveries", a2, 3); SetDate("Deliveries", b1, 1);
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

    [Check]
    public static string EndToEnd_SeveralTripReports_ToInvoice_AndBack()
    {
        Fresh();
        int a1 = Seed.Delivery("CLIENT A"), a2 = Seed.Delivery("CLIENT A"), dr = Seed.Delivery("CLIENT A");
        SetDate("Deliveries", a1, 3); SetDate("Deliveries", a2, 3); SetDate("Deliveries", dr, 1);
        AddStop(a1, 1, "CLIENT A", "Store A1-1", 10, 5); AddStop(a2, 1, "CLIENT A", "Store A2-1", 10, 5);
        string nA1 = NumberOf(a1), nA2 = NumberOf(a2), nDr = NumberOf(dr);
        AddReport(nDr, dr, "Draft");   // a dispatch whose report is only a draft
        return WithTripReportView((view, host, problems) =>
        {
            // 1. file two dispatches together
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            PickMain(view, nA1);
            typeof(TripReportView).GetMethod("AddExtraDispatches", NonPub)!.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, nA2) } });
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            foreach (DataGridViewRow r in grid.Rows) { r.Cells["colIn"].Value = "08:00"; r.Cells["colOut"].Value = "09:00"; r.Cells["colRep"].Value = "Maria Santos"; r.Cells["colDr"].Value = r.Index == 0 ? "7001" : "7002"; }
            typeof(TripReportView).GetMethod("SaveSeveralDispatches", NonPub)!.Invoke(view, new object[] { DeliveryNo(view, nA1) });
            Pump(800);

            // 2. both are now waiting for an invoice, each with its own trip report number; the draft one is not
            var waiting = MJdatabase.GetTripsWaitingForInvoice();
            foreach (var d in new[] { a1, a2 })
            {
                var w = waiting.FirstOrDefault(x => x.DeliveryId == d);
                if (w == null) problems.Add($"dispatch {d} is not waiting for an invoice after its report was filed");
                else if (w.TripReportNumber.Length == 0) problems.Add($"dispatch {d} waits for an invoice but shows no trip report number");
            }
            if (waiting.Any(x => x.DeliveryId == dr)) problems.Add("a dispatch with only a DRAFT report is offered for invoicing");

            // 3. Billing accepts the two filed trips and refuses the draft one (a themed pop-up would wait for a click: close it and record its title)
            var seen = new List<string>();
            using var guard = new System.Windows.Forms.Timer { Interval = 300 };
            guard.Tick += (s, e) => CloseOpenPopups(host, seen);
            guard.Start();
            using var host2 = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var billing = new BillingView { Dock = DockStyle.Fill };
            host2.Controls.Add(billing); host2.Show(); Pump(1500);
            var all = MJdatabase.GetDeliveries();
            bool Open(int id) => billing.PrefillForDelivery(all.First(d => d.Id == id));
            if (!Open(a1)) problems.Add("Billing refused the first filed trip");
            if (!Open(a2)) problems.Add("Billing refused the second filed trip");
            if (Open(dr)) problems.Add("Billing accepted a trip whose report is only a draft");
            Pump(500);

            // 4. the real invoice save: the report turns Invoiced and the trip leaves the waiting list
            int n = 0;
            foreach (var d in new[] { a1, a2 })
            {
                n++;
                MJdatabase.SaveInvoice(new Invoice { InvoiceNumber = "SI-90" + n, DeliveryId = d, ClientName = "CLIENT A", IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(15), TotalAmount = 5000m, NetAmount = 5000m, Status = "Unpaid" });
            }
            foreach (var d in new[] { a1, a2 })
                if (Str("SELECT Status FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", d)) != "Invoiced") problems.Add($"the trip report of dispatch {d} did not turn Invoiced after its invoice was saved");
            waiting = MJdatabase.GetTripsWaitingForInvoice();
            if (waiting.Any(x => x.DeliveryId == a1 || x.DeliveryId == a2)) problems.Add("invoiced trips are still in the waiting-for-invoice list");
            if (Open(a1)) problems.Add("Billing allowed a second invoice for an already invoiced trip");

            // 5. cancelling an invoice sends its trip back (report Completed, waiting again)
            int invId = (int)Long("SELECT Id FROM Invoices WHERE DeliveryId = @d", ("@d", a1));
            MJdatabase.CancelInvoice(invId, true, "typing mistake in the check");
            if (Str("SELECT Status FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", a1)) != "Completed") problems.Add("after cancelling the invoice the report did not go back to Completed");
            if (!MJdatabase.GetTripsWaitingForInvoice().Any(x => x.DeliveryId == a1)) problems.Add("after cancelling the invoice the trip is not waiting again");
            if (Str("SELECT Status FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", a2)) != "Invoiced") problems.Add("cancelling one invoice changed the other trip's report");
            guard.Stop();
            host2.Close();
            if (seen.Count == 0 || !seen.All(t => t.Length > 0)) problems.Add("(expected Billing to explain its refusals) pop-ups seen: " + string.Join(" / ", seen));
            return "";
        });
    }

    [Check]
    public static string TripReportSeveral_Warning_DifferencesBetween_ShownOnForm_AskedBeforeSave()
    {
        var problems = new List<string>();
        // the rule on its own
        var main = new Delivery { DispatchNumber = "M", DispatchDate = new DateTime(2026, 11, 6), TruckId = 1, TruckInfo = "ABC-123 (6T)", DriverId = 1, DriverName = "Juan" };
        var same = new Delivery { DispatchNumber = "S", DispatchDate = new DateTime(2026, 11, 6, 15, 0, 0), TruckId = 1, TruckInfo = "ABC-123 (6T)", DriverId = 1, DriverName = "Juan" };
        var other = new Delivery { DispatchNumber = "O", DispatchDate = new DateTime(2026, 11, 7), TruckId = 2, TruckInfo = "XYZ-999 (6T)", DriverId = 2, DriverName = "Pedro" };
        if (TripReportView.DifferencesBetween(main, same).Count != 0) problems.Add("the same day, truck and driver was reported as different");
        var diff = TripReportView.DifferencesBetween(main, other);
        if (diff.Count != 3 || !diff[0].Contains("date") || !diff[1].Contains("truck") || !diff[2].Contains("driver")) problems.Add("differences: " + string.Join(" | ", diff));
        var noIds = new Delivery { DispatchNumber = "N", DispatchDate = main.DispatchDate, TruckInfo = "XYZ-999 (6T)", DriverName = "Juan" };
        if (TripReportView.DifferencesBetween(main, noIds).Count != 1) problems.Add("a truck that differs only by its plate text was not noticed");

        Fresh();
        int a1 = Seed.Delivery("CLIENT A"), a2 = Seed.Delivery("CLIENT A"), a3 = Seed.Delivery("CLIENT A");
        SetDate("Deliveries", a1, 3); SetDate("Deliveries", a2, 1); SetDate("Deliveries", a3, 3);
        AddStop(a1, 1, "CLIENT A", "Store 1", 10, 5); AddStop(a2, 1, "CLIENT A", "Store 2", 10, 5); AddStop(a3, 1, "CLIENT A", "Store 3", 10, 5);
        string n1 = NumberOf(a1), n2 = NumberOf(a2), n3 = NumberOf(a3);
        string onForm = WithTripReportView((view, host, probs) =>
        {
            var warn = FieldOf<Label>(view, "lblExtraWarning")!;
            var add = typeof(TripReportView).GetMethod("AddExtraDispatches", NonPub)!;
            var remove = typeof(TripReportView).GetMethod("RemoveExtra", NonPub)!;
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            PickMain(view, n1);
            add.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, n3) } });
            if (warn.Visible) probs.Add("a warning is shown for a dispatch of the same day, truck and driver");
            remove.Invoke(view, new object[] { DeliveryNo(view, n3) });
            add.Invoke(view, new object[] { new List<Delivery> { DeliveryNo(view, n2) } });
            if (!warn.Visible || !warn.Text.Contains(n2) || !warn.Text.Contains("different date")) probs.Add($"no standing warning for a different date: visible={warn.Visible} '{warn.Text}'");
            if (!warn.Text.Contains("gate times")) probs.Add("the warning does not say what is copied");

            // saving asks first; closing the question (= No) must save nothing
            var seen = new List<string>();
            using var guard = new System.Windows.Forms.Timer { Interval = 300 };
            guard.Tick += (s, e) => CloseOpenPopups(host, seen);
            guard.Start();
            foreach (DataGridViewRow r in FieldOf<DataGridView>(view, "gridStores")!.Rows) { r.Cells["colIn"].Value = "08:00"; r.Cells["colOut"].Value = "09:00"; r.Cells["colRep"].Value = "Maria Santos"; r.Cells["colDr"].Value = (7000 + r.Index).ToString(); }
            typeof(TripReportView).GetMethod("SaveSeveralDispatches", NonPub)!.Invoke(view, new object[] { DeliveryNo(view, n1) });
            guard.Stop();
            if (!seen.Contains("Check these dispatches")) probs.Add("saving did not ask about the differing dispatch (pop-ups: " + string.Join(", ", seen) + ")");
            if (Long("SELECT COUNT(*) FROM TripReports WHERE COALESCE(IsVoid,0) = 0") != 0) probs.Add("saving went ahead although the question was answered No");
            remove.Invoke(view, new object[] { DeliveryNo(view, n2) });
            if (warn.Visible) probs.Add("the warning stays after the differing dispatch was removed");
            return "";
        });
        if (onForm.Length > 0) problems.Add(onForm);
        return string.Join(" | ", problems);
    }
}
