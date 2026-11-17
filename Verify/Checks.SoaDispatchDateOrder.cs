using System.Drawing;
using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;
using System.Windows.Forms;

namespace TDMS.Verify;

/// <summary>
/// Nov 17: "Date store received" on a statement line is the DISPATCH ORDER's date (not the trip report's completion date, not the day the statement was
/// made), and the trips are listed oldest to latest by it: on screen, when a saved statement is reopened, and on the printed statement.
/// </summary>
public static partial class Checks
{
    private const BindingFlags SdF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A dispatch (completed, with a trip report) whose dispatch date and trip report date are set to the given days from today.</summary>
    private static int SdTrip(int dispatchDaysAgo, int reportDaysAgo, params string[] stores)
    {
        int d = Seed.Delivery(client: "CLIENT A");
        int r = Seed.TripReport(d);
        Exec("UPDATE Deliveries SET DispatchDate=@x WHERE Id=@i", ("@x", DateTime.Today.AddDays(-dispatchDaysAgo).ToString("yyyy-MM-dd")), ("@i", d));
        Exec("UPDATE TripReports SET ReportDate=@x, Status='Completed' WHERE Id=@i", ("@x", DateTime.Today.AddDays(-reportDaysAgo).ToString("yyyy-MM-dd")), ("@i", r));
        for (int k = 0; k < stores.Length; k++)
            Exec("INSERT INTO DispatchStops (DeliveryId, StopSequence, ClientName, Destination, Status, StoreCode, DrNumbers) VALUES (@d, @s, 'CLIENT A', @n, 'Delivered', @c, @dr)",
                ("@d", d), ("@s", k + 1), ("@n", stores[k]), ("@c", "SC" + d + k), ("@dr", "DR-" + (7000 + d * 10 + k)));
        return d;
    }

    private static string SdDate(int daysAgo) => Fmt.Date(DateTime.Today.AddDays(-daysAgo));

    /// <summary>Shows the statement screen in a hidden window and runs <paramref name="body"/> with it and its trips table.</summary>
    private static string SdView(Func<StatementOfAccountView, DataGridView, string> body)
        => RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(600);
            var grid = FieldOf<DataGridView>(view, "gridItems")!;
            // the client is on the form first, without the screen filling the table with all of the client's waiting trips (that is checked on its own)
            var bind = typeof(StatementOfAccountView).GetField("isBindingForm", SdF)!;
            bind.SetValue(view, true); SdCall(view, "ShowClientName", "CLIENT A"); bind.SetValue(view, false);
            if (SdRows(grid).Count != 0) return "set-up: the table is not empty";
            return body(view, grid);
        });

    private static object? SdCall(StatementOfAccountView view, string method, params object[] args) => typeof(StatementOfAccountView).GetMethod(method, SdF)!.Invoke(view, args);
    private static void SdAdd(StatementOfAccountView view, params int[] ids) => SdCall(view, "AddDeliveriesToStatement", ids.Select(i => MJdatabase.GetDeliveryById(i)!).ToList());
    private static List<DataGridViewRow> SdRows(DataGridView g) => g.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
    private static string SdCell(DataGridViewRow r, string col) => r.Cells[col].Value?.ToString() ?? "";
    private static string SdShape(DataGridView g) => string.Join(" | ", SdRows(g).Select(r => $"T{SdCell(r, "colTripNo")} {SdCell(r, "colDate")} {SdCell(r, "colDest")} [{SdCell(r, "colAmount")}/{SdCell(r, "colFuel")}]"));
    private static decimal SdSum(DataGridView g, string col) => SdRows(g).Sum(r => decimal.TryParse(SdCell(r, col), out var v) ? v : 0m);
    /// <summary>Types the trip's Amount and Fuel surcharge on its first row (as the person does).</summary>
    private static void SdMoney(DataGridView g, int deliveryId, decimal amount, decimal fuel)
    {
        var first = SdRows(g).First(r => SdCell(r, "colDeliveryId") == deliveryId.ToString());
        first.Cells["colAmount"].Value = amount; first.Cells["colFuel"].Value = fuel;
    }
    private static decimal SdBox(StatementOfAccountView v, string name) => ((NumericUpDown)typeof(StatementOfAccountView).GetField(name, SdF)!.GetValue(v)!).Value;

    [Check]
    public static string SoaDispatchDate_LineShowsTheDispatchDate_NotTheReportDateOrToday()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(dispatchDaysAgo: 9, reportDaysAgo: 2, "Store One");
        return SdView((view, grid) =>
        {
            SdAdd(view, a);
            var rows = SdRows(grid);
            if (rows.Count != 1) return $"{rows.Count} rows, expected 1: {SdShape(grid)}";
            string got = SdCell(rows[0], "colDate");
            if (got != SdDate(9)) return $"Date store received is {got}; the dispatch order's date is {SdDate(9)} (trip report {SdDate(2)}, today {SdDate(0)})";
            return "";
        });
    }

    [Check]
    public static string SoaDispatchDate_AddedOutOfOrder_SortedOldestFirst_MultiStoreTripStaysTogether()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(3, 1, "Store A");
        int b = SdTrip(10, 4, "Store B1", "Store B2");     // a trip with two stores
        int c = SdTrip(6, 2, "Store C");
        return SdView((view, grid) =>
        {
            SdAdd(view, a); SdMoney(grid, a, 1000m, 100m);
            SdAdd(view, b); SdMoney(grid, b, 2000m, 200m);
            string s2 = SdShape(grid);
            var r2 = SdRows(grid);
            if (r2.Count != 3 || SdCell(r2[0], "colDate") != SdDate(10) || SdCell(r2[2], "colDate") != SdDate(3)) return "after adding the later trip first and the older one second the table is: " + s2;
            SdAdd(view, c); SdMoney(grid, c, 3000m, 0m);
            var rows = SdRows(grid);
            string shape = SdShape(grid);
            if (rows.Count != 4) return $"{rows.Count} rows, expected 4: {shape}";
            var dates = rows.Select(r => SdCell(r, "colDate")).ToList();
            if (!dates.SequenceEqual(new[] { SdDate(10), SdDate(10), SdDate(6), SdDate(3) })) return "dates are not oldest to latest: " + shape;
            if (!rows.Select(r => SdCell(r, "colTripNo")).SequenceEqual(new[] { "1", "1", "2", "3" })) return "Trip numbers are not 1,1,2,3: " + shape;
            if (!rows.Select(r => SdCell(r, "colDeliveryId")).SequenceEqual(new[] { b.ToString(), b.ToString(), c.ToString(), a.ToString() })) return "trips are in the wrong order: " + shape;
            if (SdCell(rows[0], "colDest") != "Store B1" || SdCell(rows[1], "colDest") != "Store B2") return "the multi-store trip's stores are not in their order: " + shape;
            if (SdCell(rows[0], "colAmount") != "2000" || SdCell(rows[0], "colFuel") != "200") return "the multi-store trip's amount is not on its first row: " + shape;
            if (SdCell(rows[1], "colAmount").Length > 0 && SdCell(rows[1], "colAmount") != "0" || SdCell(rows[1], "colFuel").Length > 0 && SdCell(rows[1], "colFuel") != "0") return "the multi-store trip's second row holds money: " + shape;
            if (SdCell(rows[2], "colAmount") != "3000" || SdCell(rows[3], "colAmount") != "1000" || SdCell(rows[3], "colFuel") != "100") return "amounts did not stay with their trips: " + shape;
            if (SdCell(rows[0], "colDrops") != "2" || SdCell(rows[1], "colDrops") != "2" || SdCell(rows[2], "colDrops") != "1") return "No. of drops is wrong after sorting: " + shape;
            // the merged trip cell: the two rows of the trip are one trip, the others are single
            var tb = typeof(StatementOfAccountView).GetMethod("TripBounds", SdF)!;
            object[] p = { 1, 0, 0 }; tb.Invoke(view, p);
            if ((int)p[1] != 0 || (int)p[2] != 1) return $"the multi-store trip's rows are {p[1]}..{p[2]}, expected 0..1";
            p = new object[] { 2, 0, 0 }; tb.Invoke(view, p);
            if ((int)p[1] != 2 || (int)p[2] != 2) return $"the next trip's rows are {p[1]}..{p[2]}, expected 2..2";
            return "";
        });
    }

    [Check]
    public static string SoaDispatchDate_PickingTheClient_FillsTheTableOldestFirst()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(3, 1, "Store A");
        int b = SdTrip(10, 4, "Store B1", "Store B2");
        int c = SdTrip(6, 2, "Store C");
        return SdView((view, grid) =>
        {
            // choosing the client in the box fills the table with its waiting trips
            var cb = FieldOf<ComboBox>(view, "cbClient")!;
            if (cb.FindStringExact("CLIENT A") < 0) cb.Items.Add("CLIENT A");
            cb.SelectedIndex = -1; cb.SelectedIndex = cb.FindStringExact("CLIENT A");
            Pump(300);
            var rows = SdRows(grid);
            if (!rows.Select(r => SdCell(r, "colDeliveryId")).SequenceEqual(new[] { b.ToString(), b.ToString(), c.ToString(), a.ToString() })) return "picking the client did not list the trips oldest to latest: " + SdShape(grid);
            if (!rows.Select(r => SdCell(r, "colDate")).SequenceEqual(new[] { SdDate(10), SdDate(10), SdDate(6), SdDate(3) })) return "dates: " + SdShape(grid);
            if (!rows.Select(r => SdCell(r, "colTripNo")).SequenceEqual(new[] { "1", "1", "2", "3" })) return "Trip numbers: " + SdShape(grid);
            return "";
        });
    }

    [Check]
    public static string SoaDispatchDate_Sorting_DoesNotChangeTheMoney()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(3, 1, "Store A");
        int b = SdTrip(10, 4, "Store B1", "Store B2");
        int c = SdTrip(6, 2, "Store C");
        return SdView((view, grid) =>
        {
            SdAdd(view, a, b, c);
            SdMoney(grid, a, 1000m, 100m); SdMoney(grid, b, 2000m, 200m); SdMoney(grid, c, 3000m, 50m);
            // make the table out of order: the latest trip's date becomes the oldest
            foreach (var r in SdRows(grid).Where(r => SdCell(r, "colDeliveryId") == a.ToString())) r.Cells["colDate"].Value = SdDate(30);
            decimal amt = SdSum(grid, "colAmount"), fuel = SdSum(grid, "colFuel");
            decimal sales = SdBox(view, "numTotalSales"), vat = SdBox(view, "numVat"), net = SdBox(view, "numNetOfVat"), wht = SdBox(view, "numWithholdingTax"), due = SdBox(view, "numTotalAmountDue"), fuelBox = SdBox(view, "numFuelSurchargeTotal");
            var perTrip = SdRows(grid).GroupBy(r => SdCell(r, "colDeliveryId")).ToDictionary(g => g.Key, g => (g.Sum(r => decimal.TryParse(SdCell(r, "colAmount"), out var v) ? v : 0m), g.Sum(r => decimal.TryParse(SdCell(r, "colFuel"), out var v) ? v : 0m)));
            string before = SdShape(grid);
            SdCall(view, "SortTripsByDateStoreReceived");
            string after = SdShape(grid);
            var rows = SdRows(grid);
            if (!rows.Select(r => SdCell(r, "colDeliveryId")).SequenceEqual(new[] { a.ToString(), b.ToString(), b.ToString(), c.ToString() })) return $"not re-ordered: before {before} / after {after}";
            if (!rows.Select(r => SdCell(r, "colTripNo")).SequenceEqual(new[] { "1", "2", "2", "3" })) return "Trip not renumbered 1..n: " + after;
            if (SdSum(grid, "colAmount") != amt || SdSum(grid, "colFuel") != fuel) return $"the Amount / Fuel column sums changed: {amt}/{fuel} -> {SdSum(grid, "colAmount")}/{SdSum(grid, "colFuel")}";
            var perAfter = rows.GroupBy(r => SdCell(r, "colDeliveryId")).ToDictionary(g => g.Key, g => (g.Sum(r => decimal.TryParse(SdCell(r, "colAmount"), out var v) ? v : 0m), g.Sum(r => decimal.TryParse(SdCell(r, "colFuel"), out var v) ? v : 0m)));
            if (perTrip.Any(kv => perAfter[kv.Key] != kv.Value)) return "a trip's amount or fuel changed";
            if (SdBox(view, "numTotalSales") != sales || SdBox(view, "numVat") != vat || SdBox(view, "numNetOfVat") != net || SdBox(view, "numWithholdingTax") != wht || SdBox(view, "numTotalAmountDue") != due || SdBox(view, "numFuelSurchargeTotal") != fuelBox)
                return "the statement's totals changed after sorting";
            var t = SoaTotals.From(6000m, 350m);
            if (due != t.Due || sales != t.TotalSales) return $"set-up: totals {sales}/{due} are not what 6000 + 350 fuel gives ({t.TotalSales}/{t.Due})";
            // sorting again changes nothing
            SdCall(view, "SortTripsByDateStoreReceived");
            if (SdShape(grid) != after) return "sorting twice moved something";
            return "";
        });
    }

    [Check]
    public static string SoaDispatchDate_OrderSurvivesSaveAndReopen_AndPrintsOldestFirst()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(3, 1, "Store A");
        int b = SdTrip(10, 4, "Store B1", "Store B2");
        int c = SdTrip(6, 2, "Store C");
        int soaId = 0; string err = "";
        err = SdView((view, grid) =>
        {
            SdAdd(view, a); SdMoney(grid, a, 1000m, 100m);
            SdAdd(view, c); SdMoney(grid, c, 3000m, 0m);
            SdAdd(view, b); SdMoney(grid, b, 2000m, 200m);
            // what the Save button does: sort, then collect the lines
            SdCall(view, "SortTripsByDateStoreReceived");
            var items = (List<StatementOfAccountItem>)SdCall(view, "CollectItemsFromGrid")!;
            var t = SoaTotals.From(items.Sum(i => i.Amount), items.Sum(i => i.FuelSurcharge));
            var soa = new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "period", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due, SoaType = SoaTypes.Normal };
            soaId = MJdatabase.SaveStatementOfAccount(soa, items);
            return "";
        });
        if (err.Length > 0) return err;
        var saved = MJdatabase.GetStatementOfAccountItems(soaId);
        string stored = string.Join(" | ", saved.Select(i => $"T{i.TripNumber} {i.DateReceived} {i.Destination}"));
        string want = string.Join(" | ", new[] { (1, 10, "Store B1"), (1, 10, "Store B2"), (2, 6, "Store C"), (3, 3, "Store A") }.Select(x => $"T{x.Item1} {DateTime.Today.AddDays(-x.Item2):yyyy-MM-dd} {x.Item3}"));
        if (stored != want) return $"saved lines are '{stored}', expected '{want}'";
        if (saved.Sum(i => i.Amount) != 6000m || saved.Sum(i => i.FuelSurcharge) != 300m) return "the saved amounts changed";
        if (saved[0].Amount != 2000m || saved[1].Amount != 0m) return "the multi-store trip's amount is not on its first saved row";
        // reopened: same order
        string reopened = SdView((view, grid) =>
        {
            SdCall(view, "BindSoaToDrawer", MJdatabase.GetStatementOfAccountById(soaId)!);
            string shape = SdShape(grid);
            var rows = SdRows(grid);
            if (!rows.Select(r => SdCell(r, "colDate")).SequenceEqual(new[] { SdDate(10), SdDate(10), SdDate(6), SdDate(3) })) return "reopened statement is not oldest to latest: " + shape;
            if (!rows.Select(r => SdCell(r, "colTripNo")).SequenceEqual(new[] { "1", "1", "2", "3" })) return "reopened Trip numbers: " + shape;
            return "";
        });
        if (reopened.Length > 0) return reopened;
        // printed: oldest first, Trip 1..3
        var soaRow = MJdatabase.GetStatementOfAccountById(soaId)!;
        string h = Html(PrintForms.StatementOfAccount(soaRow, saved));
        int i10 = h.IndexOf(SdDate(10), StringComparison.Ordinal), i6 = h.IndexOf(SdDate(6), StringComparison.Ordinal), i3 = h.IndexOf(SdDate(3), StringComparison.Ordinal);
        if (i10 < 0 || i6 < 0 || i3 < 0 || !(i10 < i6 && i6 < i3)) return $"the printed statement is not oldest first (positions {i10}, {i6}, {i3})";
        // a statement saved in the wrong order prints oldest first too (the printout sorts by the dispatch date itself)
        var jumbled = MJdatabase.GetStatementOfAccountItems(soaId);
        foreach (var it in jumbled) it.TripNumber = it.DeliveryId == a ? 1 : it.DeliveryId == c ? 2 : 3;   // the latest trip first, the oldest last
        string h2 = Html(PrintForms.StatementOfAccount(soaRow, jumbled));
        int j10 = h2.IndexOf(SdDate(10), StringComparison.Ordinal), j6 = h2.IndexOf(SdDate(6), StringComparison.Ordinal), j3 = h2.IndexOf(SdDate(3), StringComparison.Ordinal);
        if (!(j10 < j6 && j6 < j3 && j10 >= 0)) return $"a statement saved in another order does not print oldest first (positions {j10}, {j6}, {j3})";
        return "";
    }

    [Check]
    public static string SoaDispatchDate_OldStatement_ShowsDispatchDates_AndIsNotRewrittenByOpening()
    {
        Fresh(); RunBackfill();
        int a = SdTrip(3, 1, "Store A");
        int b = SdTrip(10, 4, "Store B");
        // A statement saved before this fix: the lines hold the trip report's date, the trips are in the order they were added.
        StatementOfAccountItem L(int trip, int del, string date, decimal amt, string dest) => new StatementOfAccountItem { TripNumber = trip, DateReceived = date, Destination = dest, DrNumbers = "DR-" + (7000 + trip), DropCount = 1, Amount = amt, FuelSurcharge = 0m, DeliveryId = del };
        string rep(int daysAgo) => DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");
        var lines = new List<StatementOfAccountItem> { L(1, a, rep(1), 1000m, "Store A"), L(2, b, rep(4), 2000m, "Store B") };
        var tt = SoaTotals.From(3000m, 0m);
        var soa = new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "period", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = tt.TotalSales, VatAmount = tt.Vat, NetOfVat = tt.NetOfVat, FuelSurchargeTotal = tt.Fuel, WithholdingTax = tt.Withholding, TotalAmount = tt.Due, SoaType = SoaTypes.Normal };
        int soaId = MJdatabase.SaveStatementOfAccount(soa, lines);
        string Db() => string.Join(" | ", MJdatabase.GetStatementOfAccountItems(soaId).Select(i => $"{i.TripNumber} {i.DateReceived} {i.Destination} {i.Amount}"));
        string dbBefore = Db();
        string ui = SdView((view, grid) =>
        {
            SdCall(view, "BindSoaToDrawer", MJdatabase.GetStatementOfAccountById(soaId)!);
            var rows = SdRows(grid);
            string shape = SdShape(grid);
            if (!rows.Select(r => SdCell(r, "colDate")).SequenceEqual(new[] { SdDate(10), SdDate(3) })) return "dates after opening: " + shape + $" (expected the dispatch dates {SdDate(10)}, {SdDate(3)}; the report dates were {SdDate(4)}, {SdDate(1)})";
            if (!rows.Select(r => SdCell(r, "colDest")).SequenceEqual(new[] { "Store B", "Store A" })) return "order after opening: " + shape;
            if (!rows.Select(r => SdCell(r, "colTripNo")).SequenceEqual(new[] { "1", "2" })) return "Trip numbers after opening: " + shape;
            if (SdSum(grid, "colAmount") != 3000m) return "amounts changed on opening: " + shape;
            // a line with no dispatch (typed by hand) keeps the date it has
            grid.Rows.Add(9, "10/01/2026", "", "DR-1234", "Typed store", "REF", "PLT", "6T", 1, 500m, 0m, "DRV");
            SdCall(view, "ApplyDispatchDatesToRows");
            var typed = SdRows(grid).Last();
            if (SdCell(typed, "colDate") != "10/01/2026") return "a line with no dispatch had its date changed to " + SdCell(typed, "colDate");
            grid.Rows.RemoveAt(typed.Index);
            // view-only opens the same way
            SdCall(view, "EnterSoaViewOnly", MJdatabase.GetStatementOfAccountById(soaId)!, "view only");
            if (!grid.ReadOnly) return "set-up: the view-only grid is not read-only";
            if (!SdRows(grid).Select(r => SdCell(r, "colDate")).SequenceEqual(new[] { SdDate(10), SdDate(3) })) return "view-only dates: " + SdShape(grid);
            return "";
        });
        if (ui.Length > 0) return ui;
        string dbAfter = Db();
        if (dbAfter != dbBefore) return $"opening rewrote the saved statement: '{dbBefore}' -> '{dbAfter}'";
        return "";
    }
}
