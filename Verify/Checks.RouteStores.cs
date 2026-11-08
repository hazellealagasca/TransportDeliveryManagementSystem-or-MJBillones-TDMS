using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Route store selection, Task 1: one matcher for route places (hyphenated store codes, client-only routes).</summary>
public static partial class Checks
{
    [Check]
    public static string RouteStores_HyphenCodeMatches()
    {
        var st = new ClientStore { StoreCode = "DD-2", StoreName = "Tarlac", ClientCompanyName = "DWARDO" };
        var fails = new List<string>();
        if (!MJdatabase.PlaceIsStore("DD-2 - Tarlac", st)) fails.Add("\"DD-2 - Tarlac\" not matched");
        if (MJdatabase.PlaceIsStore("DD-1 - San Miguel", st)) fails.Add("\"DD-1 - San Miguel\" matched");
        if (MJdatabase.PlaceIsStore("DD-1 - Tarlac", st)) fails.Add("\"DD-1 - Tarlac\" matched (code differs, name must not decide)");
        var (code, name) = DeliveryRoute.SplitStoreCodeAndName("DD-2 - Tarlac");
        if (code != "DD-2" || name != "Tarlac") fails.Add($"split gave ({code}, {name})");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_NameOnlyStillMatches()
    {
        var st = new ClientStore { StoreCode = "DD-2", StoreName = "Tarlac" };
        var fails = new List<string>();
        if (!MJdatabase.PlaceIsStore("Tarlac", st)) fails.Add("\"Tarlac\" not matched");
        if (!MJdatabase.PlaceIsStore("  dd-2  -  tarlac ", st)) fails.Add("canonical text with other case and spacing not matched");
        if (MJdatabase.PlaceIsStore("San Miguel", st)) fails.Add("\"San Miguel\" matched");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_SameNameOtherClientNotMatched()
    {
        var storeA = new ClientStore { StoreCode = "AA-9", StoreName = "Tarlac", ClientCompanyName = "CLIENT A" };
        var storeB = new ClientStore { StoreCode = "BB-1", StoreName = "Tarlac", ClientCompanyName = "CLIENT B" };
        var routeB = new DeliveryRoute { Id = 1, ClientCompanyName = "CLIENT B", RouteCode = "RB1", RouteName = "North", ConnectedPlaces = "Tarlac; San Miguel" };
        var fails = new List<string>();
        if (MJdatabase.StoreIsOnRoute(routeB, storeA)) fails.Add("client A's Tarlac counted on client B's route");
        if (!MJdatabase.StoreIsOnRoute(routeB, storeB)) fails.Add("client B's Tarlac not counted on its own route");
        return string.Join("; ", fails);
    }

    // ---- Task 2: one idempotent writer (SetStoreOnRoute) — run on a fresh database, never the real one.

    private static DeliveryRoute RouteWithPlaces(string places)
    {
        Fresh();
        long id = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DW-R1', 'North', @p, 1)", ("@p", places));
        return new DeliveryRoute { Id = (int)id, ClientCompanyName = "DWARDO", RouteCode = "DW-R1", RouteName = "North", ConnectedPlaces = places };
    }

    private static string SavedPlaces(int routeId) => Str("SELECT ConnectedPlaces FROM DeliveryRoutes WHERE Id = @id", ("@id", routeId));

    private static readonly ClientStore Tarlac = new() { StoreCode = "DD-2", StoreName = "Tarlac", ClientCompanyName = "DWARDO" };

    [Check]
    public static string RouteStores_TwiceAddsOnce()
    {
        var route = RouteWithPlaces("DD-1 - San Miguel");
        var fails = new List<string>();
        if (!MJdatabase.SetStoreOnRoute(route, Tarlac, true)) fails.Add("first include returned false");
        if (MJdatabase.SetStoreOnRoute(route, Tarlac, true)) fails.Add("second include returned true");
        var saved = new DeliveryRoute { ConnectedPlaces = SavedPlaces(route.Id) }.PlacesList;
        int copies = saved.Count(p => MJdatabase.PlaceIsStore(p, Tarlac));
        if (copies != 1) fails.Add($"{copies} entries for the store, expected 1 (saved: \"{string.Join("; ", saved)}\")");
        if (!saved.Contains("DD-2 - Tarlac")) fails.Add("canonical \"DD-2 - Tarlac\" not written");
        if (saved.Count != 2) fails.Add($"{saved.Count} entries in total, expected 2");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_UntickRemovesAllCopies()
    {
        var route = RouteWithPlaces("DD-2 - Tarlac; DD-1 - San Miguel; Tarlac; dd-2 - tarlac");
        var fails = new List<string>();
        if (!MJdatabase.SetStoreOnRoute(route, Tarlac, false)) fails.Add("remove returned false");
        var saved = new DeliveryRoute { ConnectedPlaces = SavedPlaces(route.Id) }.PlacesList;
        int copies = saved.Count(p => MJdatabase.PlaceIsStore(p, Tarlac));
        if (copies != 0) fails.Add($"{copies} copies left after untick, expected 0");
        if (saved.Count != 1 || saved[0] != "DD-1 - San Miguel") fails.Add($"other stores changed (saved: \"{string.Join("; ", saved)}\")");
        if (MJdatabase.SetStoreOnRoute(route, Tarlac, false)) fails.Add("second remove returned true");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_SetOnAlreadyOnReturnsFalse()
    {
        const string before = "DD-2 - Tarlac; DD-1 - San Miguel";
        var route = RouteWithPlaces(before);
        var fails = new List<string>();
        if (MJdatabase.SetStoreOnRoute(route, Tarlac, true)) fails.Add("include on a member returned true");
        string after = SavedPlaces(route.Id);
        if (after != before) fails.Add($"list changed to \"{after}\"");
        return string.Join("; ", fails);
    }

    // ---- Task 3: one-time repair of existing route lists (RepairRoutePlaces) — fresh databases and a copy of the real one.

    /// <summary>A fresh database with only this route's list filled in (the seeded routes are emptied, so the report counts only it).</summary>
    private static DeliveryRoute OnlyRouteWithPlaces(string places)
    {
        var route = RouteWithPlaces(places);
        Exec("UPDATE DeliveryRoutes SET ConnectedPlaces = '' WHERE Id <> @id", ("@id", route.Id));
        return route;
    }

    private static long AddStore(string client, string code, string name) =>
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson) VALUES (0, @cl, @c, @n, '', '')", ("@cl", client), ("@c", code), ("@n", name));

    private static string Counts(MJdatabase.RouteRepairReport r) =>
        $"before {r.EntriesBefore}, after {r.EntriesAfter}, duplicates {r.DuplicatesRemoved}, canonicalised {r.Canonicalised}, unmatched {r.Unmatched.Count}, ambiguous {r.Ambiguous.Count}, kept would-be-ambiguous {r.KeptWouldBeAmbiguous.Count}, orders {r.OrdersToReview.Count}";

    private static string DescribeRepair(MJdatabase.RouteRepairReport r) =>
        $"EntriesBefore: {r.EntriesBefore}\nEntriesAfter: {r.EntriesAfter}\nDuplicatesRemoved: {r.DuplicatesRemoved}\nCanonicalised: {r.Canonicalised}\n" +
        $"Unmatched: {r.Unmatched.Count}\n" + string.Concat(r.Unmatched.Select(u => "  - " + u + "\n")) +
        $"Ambiguous: {r.Ambiguous.Count}\n" + string.Concat(r.Ambiguous.Select(a => "  - " + a + "\n")) +
        $"KeptWouldBeAmbiguous: {r.KeptWouldBeAmbiguous.Count}\n" + string.Concat(r.KeptWouldBeAmbiguous.Select(k => "  - " + k + "\n")) +
        $"OrdersToReview: {r.OrdersToReview.Count}\n" + string.Concat(r.OrdersToReview.Select(o => "  - " + o + "\n"));

    [Check]
    public static string RouteStores_RepairDedupesAndCanonicalises()
    {
        var route = OnlyRouteWithPlaces("Tarlac; DD-1 - San Miguel; DD-2 - Tarlac; dd-2 - tarlac; DD-1 - San Miguel");
        AddStore("DWARDO", "DD-2", "Tarlac");
        AddStore("DWARDO", "DD-1", "San Miguel");
        var fails = new List<string>();
        var dry = MJdatabase.RepairRoutePlaces(apply: false);
        if (SavedPlaces(route.Id) != "Tarlac; DD-1 - San Miguel; DD-2 - Tarlac; dd-2 - tarlac; DD-1 - San Miguel") fails.Add($"dry run wrote \"{SavedPlaces(route.Id)}\"");
        var r = MJdatabase.RepairRoutePlaces(apply: true);
        string saved = SavedPlaces(route.Id);
        if (saved != "DD-2 - Tarlac; DD-1 - San Miguel") fails.Add($"saved \"{saved}\", expected \"DD-2 - Tarlac; DD-1 - San Miguel\"");
        foreach (var (name, rep) in new[] { ("dry run", dry), ("apply", r) })
            if (rep.EntriesBefore != 5 || rep.EntriesAfter != 2 || rep.DuplicatesRemoved != 3 || rep.Canonicalised != 1 || rep.Unmatched.Count != 0 || rep.Ambiguous.Count != 0)
                fails.Add($"{name} report: {Counts(rep)}; expected before 5, after 2, duplicates 3, canonicalised 1, unmatched 0, ambiguous 0");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RepairKeepsUnmatchedAndAmbiguous()
    {
        const string before = "Nowhere; DD-2 - Tarlac; San Miguel; Nowhere";
        var route = OnlyRouteWithPlaces(before);
        AddStore("DWARDO", "DD-2", "Tarlac");
        AddStore("DWARDO", "", "DD-2 - Tarlac");     // a second store that the same text also names
        AddStore("DWARDO", "DD-1", "San Miguel");
        AddStore("OTHER CLIENT", "OC-1", "Nowhere"); // another client's store must not count
        var fails = new List<string>();
        var r = MJdatabase.RepairRoutePlaces(apply: true);
        string saved = SavedPlaces(route.Id);
        if (saved != "Nowhere; DD-2 - Tarlac; DD-1 - San Miguel; Nowhere") fails.Add($"saved \"{saved}\", expected \"Nowhere; DD-2 - Tarlac; DD-1 - San Miguel; Nowhere\"");
        if (r.Unmatched.Count != 2 || !r.Unmatched.All(u => u.Contains("Nowhere"))) fails.Add($"unmatched [{string.Join(" | ", r.Unmatched)}], expected the 2 \"Nowhere\" entries");
        if (r.Ambiguous.Count != 1 || !r.Ambiguous[0].Contains("DD-2 - Tarlac")) fails.Add($"ambiguous [{string.Join(" | ", r.Ambiguous)}], expected 1 \"DD-2 - Tarlac\" entry");
        if (r.EntriesBefore != 4 || r.EntriesAfter != 4 || r.DuplicatesRemoved != 0 || r.Canonicalised != 1) fails.Add($"report: {Counts(r)}; expected before 4, after 4, duplicates 0, canonicalised 1");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RepairKeepsTextWhoseNewTextWouldBeAmbiguous()
    {
        var route = OnlyRouteWithPlaces("Tarlac");
        long coded = AddStore("DWARDO", "DD-2", "Tarlac");
        long noCode = AddStore("DWARDO", "", "DD-2 - Tarlac"); // "DD-2 - Tarlac" would name this store too
        var fails = new List<string>();
        var r = MJdatabase.RepairRoutePlaces(apply: true);
        if (SavedPlaces(route.Id) != "Tarlac") fails.Add($"saved \"{SavedPlaces(route.Id)}\", expected \"Tarlac\"");
        string expected = $"route DW-R1 (DWARDO): \"Tarlac\" kept; \"DD-2 - Tarlac\" would match store #{noCode}, store #{coded}";
        if (r.KeptWouldBeAmbiguous.Count != 1 || r.KeptWouldBeAmbiguous[0] != expected) fails.Add($"kept would-be-ambiguous [{string.Join(" | ", r.KeptWouldBeAmbiguous)}], expected [{expected}]");
        if (r.EntriesBefore != 1 || r.EntriesAfter != 1 || r.DuplicatesRemoved != 0 || r.Canonicalised != 0 || r.Unmatched.Count != 0 || r.Ambiguous.Count != 0)
            fails.Add($"report: {Counts(r)}; expected before 1, after 1, duplicates 0, canonicalised 0, unmatched 0, ambiguous 0");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RepairListsOrdersOfDuplicatedRoutes()
    {
        var dup = OnlyRouteWithPlaces("DD-2 - Tarlac; DD-2 - Tarlac");
        long clean = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DW-R2', 'South', 'DD-2 - Tarlac', 1)");
        AddStore("DWARDO", "DD-2", "Tarlac");
        int onDup = Seed.Delivery("DWARDO");
        int onClean = Seed.Delivery("DWARDO");
        int otherClient = Seed.Delivery("OTHER CLIENT");
        Exec("UPDATE Deliveries SET Destination = 'DW-R1 - North: DD-2 - Tarlac ➔ DD-2 - Tarlac' WHERE Id = @i", ("@i", onDup));
        Exec("UPDATE Deliveries SET Destination = 'DW-R2 - South: DD-2 - Tarlac' WHERE Id = @i", ("@i", onClean));
        Exec("UPDATE Deliveries SET Destination = 'DW-R1 - North: DD-2 - Tarlac' WHERE Id = @i", ("@i", otherClient));
        string dupNo = Str("SELECT DispatchNumber FROM Deliveries WHERE Id = @i", ("@i", onDup));
        string ordersBefore = Str("SELECT group_concat(Id || '|' || Destination || '|' || ClientName, ';') FROM Deliveries");
        var fails = new List<string>();
        var r = MJdatabase.RepairRoutePlaces(apply: true);
        if (r.OrdersToReview.Count != 1 || !r.OrdersToReview[0].Contains(dupNo)) fails.Add($"orders to review [{string.Join(" | ", r.OrdersToReview)}], expected only {dupNo}");
        if (Str("SELECT group_concat(Id || '|' || Destination || '|' || ClientName, ';') FROM Deliveries") != ordersBefore) fails.Add("orders were changed");
        if (SavedPlaces(dup.Id) != "DD-2 - Tarlac") fails.Add($"duplicated route saved \"{SavedPlaces(dup.Id)}\"");
        if (SavedPlaces((int)clean) != "DD-2 - Tarlac") fails.Add($"clean route saved \"{SavedPlaces((int)clean)}\"");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RepairBacksUpRecordsAndRepeatsSafely()
    {
        var route = OnlyRouteWithPlaces("Tarlac; DD-2 - Tarlac");
        AddStore("DWARDO", "DD-2", "Tarlac");
        string backups = Path.Combine(Path.GetDirectoryName(MJdatabase.DbFilePath)!, "Backups");
        int backupsBefore = Directory.Exists(backups) ? Directory.GetFiles(backups, "before_route_places_*.db").Length : 0;
        var fails = new List<string>();
        MJdatabase.RepairRoutePlaces(apply: false);
        if (MJdatabase.GetSystemSetting("Fix.RoutePlaces.v1", "") != "") fails.Add("dry run recorded the fix as done");
        int afterDry = Directory.Exists(backups) ? Directory.GetFiles(backups, "before_route_places_*.db").Length : 0;
        if (afterDry != backupsBefore) fails.Add("dry run made a backup");
        MJdatabase.RepairRoutePlaces(apply: true);
        int afterApply = Directory.Exists(backups) ? Directory.GetFiles(backups, "before_route_places_*.db").Length : 0;
        if (afterApply != backupsBefore + 1) fails.Add($"{afterApply - backupsBefore} backup(s) made by apply, expected 1");
        if (MJdatabase.GetSystemSetting("Fix.RoutePlaces.v1", "") != "done") fails.Add("Fix.RoutePlaces.v1 not recorded as done");
        string first = SavedPlaces(route.Id);
        var again = MJdatabase.RepairRoutePlaces(apply: true);
        if (SavedPlaces(route.Id) != first || first != "DD-2 - Tarlac") fails.Add($"saved \"{first}\" then \"{SavedPlaces(route.Id)}\", expected \"DD-2 - Tarlac\" both times");
        if (again.DuplicatesRemoved != 0 || again.Canonicalised != 0 || again.EntriesAfter != 1) fails.Add($"second run report: {Counts(again)}; expected nothing to change");
        return string.Join("; ", fails);
    }

    /// <summary>Plan Task 3 Step 1 on a copy of the real database: after the repair no route has a store twice and every entry is recognised.</summary>
    [Check]
    public static string RouteStores_RepairRealCopy()
    {
        string? copy = FromCopy();
        if (copy == null) return "no real database given";
        // No InitializeDatabase here: its start-up store sync adds stores to the copy, and the repair must see the data as it is.
        var fails = new List<string>();
        string RoutesNow() => Str("SELECT group_concat(Id || '=' || COALESCE(ConnectedPlaces,''), char(10)) FROM (SELECT Id, ConnectedPlaces FROM DeliveryRoutes ORDER BY Id)");
        string OrdersNow() => Str("SELECT group_concat(Id || '|' || COALESCE(Destination,'') || '|' || COALESCE(ClientName,''), char(10)) FROM (SELECT * FROM Deliveries ORDER BY Id)");
        int Entries() => MJdatabase.GetDeliveryRoutes(activeOnly: false).Sum(rt => rt.PlacesList.Count);
        string routesBefore = RoutesNow(), ordersBefore = OrdersNow();
        int entriesBefore = Entries();

        var dry = MJdatabase.RepairRoutePlaces(apply: false);
        if (RoutesNow() != routesBefore) fails.Add("dry run changed a route");
        if (dry.EntriesBefore != entriesBefore) fails.Add($"dry run counted {dry.EntriesBefore} entries, the routes have {entriesBefore}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(copy)!, "route_repair_dryrun.txt"), DescribeRepair(dry));

        var r = MJdatabase.RepairRoutePlaces(apply: true);
        int entriesAfter = Entries();
        if (r.EntriesAfter != entriesAfter || r.EntriesAfter != r.EntriesBefore - r.DuplicatesRemoved) fails.Add($"apply report: {Counts(r)}; the routes now have {entriesAfter}");
        if (r.EntriesAfter != dry.EntriesAfter || r.DuplicatesRemoved != dry.DuplicatesRemoved || r.Canonicalised != dry.Canonicalised || r.KeptWouldBeAmbiguous.Count != dry.KeptWouldBeAmbiguous.Count) fails.Add($"apply ({Counts(r)}) differs from dry run ({Counts(dry)})");
        if (r.Unmatched.Count != 0) fails.Add($"unmatched: {string.Join(" | ", r.Unmatched)}");
        int ambiguousAfter = 0;
        foreach (var rt in MJdatabase.GetDeliveryRoutes(activeOnly: false))
        {
            var stores = MJdatabase.GetClientStores(rt.ClientCompanyName);
            ambiguousAfter += rt.PlacesList.Count(p => stores.Count(st => MJdatabase.PlaceIsStore(p, st)) > 1);
            foreach (var st in stores)
            {
                int n = rt.PlacesList.Count(p => MJdatabase.PlaceIsStore(p, st));
                if (n > 1) fails.Add($"route {rt.RouteCode} has {MJdatabase.PlaceFor(st)} {n} times");
            }
            foreach (var p in rt.PlacesList.Where(p => !stores.Any(st => MJdatabase.PlaceIsStore(p, st))))
                fails.Add($"route {rt.RouteCode} entry \"{p}\" is not one of {rt.ClientCompanyName}'s stores");
        }
        if (ambiguousAfter != dry.Ambiguous.Count) fails.Add($"{ambiguousAfter} entries match two or more stores after apply, the dry run had {dry.Ambiguous.Count} ambiguous");
        if (OrdersNow() != ordersBefore) fails.Add("orders were changed");
        if (MJdatabase.GetSystemSetting("Fix.RoutePlaces.v1", "") != "done") fails.Add("Fix.RoutePlaces.v1 not recorded as done");
        string backups = Path.Combine(Path.GetDirectoryName(copy)!, "Backups");
        if (!Directory.Exists(backups) || Directory.GetFiles(backups, "before_route_places_*.db").Length == 0) fails.Add("no backup beside the copy");
        return string.Join("; ", fails);
    }

    // ---- Task 4: the panel's model (pure, no UI).

    private static readonly ClientStore[] DwStores =
    {
        new() { StoreCode = "DD-1", StoreName = "San Miguel", ClientCompanyName = "DWARDO" },
        new() { StoreCode = "DD-2", StoreName = "Tarlac", ClientCompanyName = "DWARDO" },
        new() { StoreCode = "DD-3", StoreName = "Capas", ClientCompanyName = "DWARDO" },
        new() { StoreCode = "DD-6", StoreName = "Paniqui", ClientCompanyName = "DWARDO" },
    };

    private static DeliveryRoute ModelRoute(int id, string code, string places) =>
        new() { Id = id, ClientCompanyName = "DWARDO", RouteCode = code, RouteName = code, ConnectedPlaces = places };

    [Check]
    public static string RouteStores_ModelShowsStoresAlreadyOnRoute()
    {
        var dw = ModelRoute(1, "DW", "DD-1 - San Miguel; DD-2 - Tarlac; DD-6 - Paniqui");
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw }, false);
        var fails = new List<string>();
        if (rows.Count != DwStores.Length) fails.Add($"{rows.Count} rows for {DwStores.Length} stores");
        foreach (var code in new[] { "DD-1", "DD-2", "DD-6" })
        {
            var row = rows.FirstOrDefault(r => r.Store.StoreCode == code);
            if (row == null || !row.OnThisRoute) fails.Add($"{code} not shown as on the route");
        }
        var capas = rows.FirstOrDefault(r => r.Store.StoreCode == "DD-3");
        if (capas == null || capas.OnThisRoute) fails.Add("DD-3 shown as on the route");
        if (rows.Count(r => r.OnThisRoute) != 3) fails.Add($"{rows.Count(r => r.OnThisRoute)} on this route, expected 3");
        if (rows.Select(r => r.Store.StoreCode).Distinct().Count() != rows.Count) fails.Add("a store appears twice");
        int firstAvail = rows.FindIndex(r => !r.OnThisRoute);
        if (firstAvail >= 0 && rows.Skip(firstAvail).Any(r => r.OnThisRoute)) fails.Add("stores on the route are not listed first");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ModelCountsDistinctStoresNotEntries()
    {
        var dw = ModelRoute(1, "DW", "DD-2 - Tarlac; Tarlac; dd-2 - tarlac; DD-1 - San Miguel");
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw }, false);
        var fails = new List<string>();
        if (rows.Count(r => r.OnThisRoute) != 2) fails.Add($"{rows.Count(r => r.OnThisRoute)} on this route, expected 2 distinct stores");
        if (rows.Count(r => r.Store.StoreCode == "DD-2") != 1) fails.Add("DD-2 listed more than once");
        if (rows.Count != DwStores.Length) fails.Add($"{rows.Count} rows, expected {DwStores.Length}");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ModelShowsOtherRoutes()
    {
        var dw = ModelRoute(1, "DW", "DD-1 - San Miguel; DD-2 - Tarlac");
        var dw1 = ModelRoute(2, "DW1", "DD-1 - San Miguel; DD-3 - Capas");
        var dw3 = ModelRoute(3, "DW3", "DD-1 - San Miguel; DD-1 - San Miguel");
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw, dw1, dw3 }, false);
        var fails = new List<string>();
        var dd1 = rows.First(r => r.Store.StoreCode == "DD-1");
        if (!dd1.OnThisRoute) fails.Add("DD-1 not on the route");
        if (string.Join(",", dd1.OtherRoutes) != "DW1,DW3") fails.Add($"DD-1 other routes \"{string.Join(",", dd1.OtherRoutes)}\", expected \"DW1,DW3\"");
        var dd2 = rows.First(r => r.Store.StoreCode == "DD-2");
        if (dd2.OtherRoutes.Count != 0) fails.Add("DD-2 reports other routes");
        var dd3 = rows.First(r => r.Store.StoreCode == "DD-3");
        if (string.Join(",", dd3.OtherRoutes) != "DW1") fails.Add($"DD-3 other routes \"{string.Join(",", dd3.OtherRoutes)}\", expected \"DW1\"");
        if (rows.Any(r => r.HiddenByRule)) fails.Add("a row is hidden although the one-route rule is off");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ModelOneRoutePerStoreHidesOthers()
    {
        var dw = ModelRoute(1, "DW", "DD-1 - San Miguel");
        var dw1 = ModelRoute(2, "DW1", "DD-1 - San Miguel; DD-3 - Capas");
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw, dw1 }, true);
        var fails = new List<string>();
        if (rows.First(r => r.Store.StoreCode == "DD-3").HiddenByRule is false) fails.Add("DD-3 (on DW1 only) not hidden");
        if (rows.First(r => r.Store.StoreCode == "DD-1").HiddenByRule) fails.Add("DD-1 is on this route, so it must stay visible");
        if (rows.First(r => r.Store.StoreCode == "DD-2").HiddenByRule) fails.Add("DD-2 (on no route) hidden");
        if (rows.First(r => r.Store.StoreCode == "DD-6").HiddenByRule) fails.Add("DD-6 (on no route) hidden");
        if (rows.Count(r => r.HiddenByRule) != 1) fails.Add($"{rows.Count(r => r.HiddenByRule)} hidden, expected 1");
        if (rows.Count != DwStores.Length) fails.Add("hidden stores must still be returned in the list");
        return string.Join("; ", fails);
    }
    [Check]
    public static string RouteStores_ModelIgnoresArchivedRoutes()
    {
        var dw = ModelRoute(1, "DW", "DD-1 - San Miguel");
        var old = ModelRoute(2, "DW9", "DD-1 - San Miguel; DD-3 - Capas");
        old.IsActive = false;
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw, old }, true);
        var fails = new List<string>();
        var capas = rows.First(r => r.Store.StoreCode == "DD-3");
        if (capas.OtherRoutes.Count != 0) fails.Add($"DD-3 other routes \"{string.Join(",", capas.OtherRoutes)}\", expected none (DW9 is archived)");
        if (capas.HiddenByRule) fails.Add("DD-3 hidden because of an archived route");
        var sm = rows.First(r => r.Store.StoreCode == "DD-1");
        if (sm.OtherRoutes.Count != 0) fails.Add($"DD-1 other routes \"{string.Join(",", sm.OtherRoutes)}\", expected none");
        if (rows.Count(r => r.HiddenByRule) != 0) fails.Add($"{rows.Count(r => r.HiddenByRule)} hidden, expected 0");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ModelCountsIgnoreSearchText()
    {
        var dw = ModelRoute(1, "DW", "DD-1 - San Miguel; DD-2 - Tarlac");
        var dw1 = ModelRoute(2, "DW1", "DD-3 - Capas");
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw, dw1 }, true);
        var (on, hidden) = RouteStoreListModel.Counts(rows);
        var fails = new List<string>();
        if (on != 2) fails.Add($"{on} on this route, expected 2");
        if (hidden != 1) fails.Add($"{hidden} hidden, expected 1");
        var shown = rows.Where(r => RouteStoreListModel.Matches(r, "tarlac")).ToList();
        if (shown.Count != 1 || shown[0].Store.StoreCode != "DD-2") fails.Add($"search \"tarlac\" showed {shown.Count} rows");
        if (RouteStoreListModel.Counts(rows) != (2, 1)) fails.Add("counts changed after searching");
        if (rows.Count(r => RouteStoreListModel.Matches(r, "")) != DwStores.Length) fails.Add("empty search must show every row");
        return string.Join("; ", fails);
    }

    // ---- Task 5: delivery order (MoveStoreOnRoute) — fresh databases only.

    private static ClientStore DwStore(string code, string name) => new() { StoreCode = code, StoreName = name, ClientCompanyName = "DWARDO" };
    private static readonly ClientStore Capas = DwStore("DD-3", "Capas");
    private static readonly ClientStore SanMiguel = DwStore("DD-1", "San Miguel");
    private static readonly ClientStore Paniqui = DwStore("DD-6", "Paniqui");

    [Check]
    public static string RouteStores_MoveUpAndDownKeepOtherOrder()
    {
        var route = RouteWithPlaces("DD-1 - San Miguel; DD-2 - Tarlac; DD-3 - Capas; DD-6 - Paniqui");
        var fails = new List<string>();
        if (!MJdatabase.MoveStoreOnRoute(route, Capas, -1)) fails.Add("move up returned false");
        string up = SavedPlaces(route.Id);
        if (up != "DD-1 - San Miguel; DD-3 - Capas; DD-2 - Tarlac; DD-6 - Paniqui") fails.Add($"after move up: \"{up}\"");
        if (!MJdatabase.MoveStoreOnRoute(route, SanMiguel, 1)) fails.Add("move down returned false");
        string down = SavedPlaces(route.Id);
        if (down != "DD-3 - Capas; DD-1 - San Miguel; DD-2 - Tarlac; DD-6 - Paniqui") fails.Add($"after move down: \"{down}\"");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_MoveAtTheEndsChangesNothing()
    {
        const string before = "DD-1 - San Miguel; DD-2 - Tarlac; DD-6 - Paniqui";
        var route = RouteWithPlaces(before);
        var fails = new List<string>();
        if (MJdatabase.MoveStoreOnRoute(route, SanMiguel, -1)) fails.Add("first store up returned true");
        if (MJdatabase.MoveStoreOnRoute(route, Paniqui, 1)) fails.Add("last store down returned true");
        if (MJdatabase.MoveStoreOnRoute(route, Capas, -1)) fails.Add("a store not on the route moved");
        if (MJdatabase.MoveStoreOnRoute(route, Tarlac, 0)) fails.Add("delta 0 returned true");
        string after = SavedPlaces(route.Id);
        if (after != before) fails.Add($"list changed to \"{after}\"");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_MoveUsesFirstEntryAndKeepsTheRest()
    {
        var route = RouteWithPlaces("DD-1 - San Miguel; Tarlac; DD-3 - Capas; dd-2 - tarlac");
        var fails = new List<string>();
        if (!MJdatabase.MoveStoreOnRoute(route, Tarlac, 1)) fails.Add("move down returned false");
        string after = SavedPlaces(route.Id);
        if (after != "DD-1 - San Miguel; DD-3 - Capas; Tarlac; dd-2 - tarlac") fails.Add($"after move: \"{after}\"");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_DispatchStopsFollowMovedOrder()
    {
        // MJdatabase builds dispatch stops by reading route.PlacesList[i] in order (stop i+1), so PlacesList order is stop order.
        var route = RouteWithPlaces("DD-1 - San Miguel; DD-2 - Tarlac; DD-3 - Capas");
        var fails = new List<string>();
        MJdatabase.MoveStoreOnRoute(route, Capas, -1);
        MJdatabase.MoveStoreOnRoute(route, Capas, -1);
        var codes = new DeliveryRoute { ConnectedPlaces = SavedPlaces(route.Id) }.PlacesList
            .Select(p => DeliveryRoute.SplitStoreCodeAndName(p).Item1).ToList();
        if (string.Join(",", codes) != "DD-3,DD-1,DD-2") fails.Add($"stop order \"{string.Join(",", codes)}\", expected DD-3,DD-1,DD-2");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ModelListsOnRouteStoresInStopOrder()
    {
        var dw = ModelRoute(1, "DW", "DD-2 - Tarlac; DD-1 - San Miguel; DD-6 - Paniqui"); // not name order (Paniqui, San Miguel, Tarlac)
        var rows = RouteStoreListModel.Build(dw, DwStores, new[] { dw }, false);
        var fails = new List<string>();
        string on = string.Join(",", rows.Where(r => r.OnThisRoute).Select(r => r.Store.StoreCode));
        if (on != "DD-2,DD-1,DD-6") fails.Add($"on-route order \"{on}\", expected DD-2,DD-1,DD-6");
        string avail = string.Join(",", rows.Where(r => !r.OnThisRoute).Select(r => r.Store.StoreCode));
        if (avail != "DD-3") fails.Add($"available \"{avail}\"");
        return string.Join("; ", fails);
    }

    // ---- Task 6: checked entry of store codes, names and route codes, and the paste of many stores — fresh databases only.

    private static string StatusList(IEnumerable<PasteRow> rows) => string.Join(",", rows.Select(r => r.Status));

    [Check]
    public static string RouteStores_CodePreviewShowsWhatWillBeSaved()
    {
        var fails = new List<string>();
        string p = StorePasteModel.CodePreview("dd 1");
        if (p != "Will save as DD1") fails.Add($"\"dd 1\" previewed as \"{p}\"");
        if (StorePasteModel.CodePreview("dd--1-") != "Will save as DD-1") fails.Add($"\"dd--1-\" previewed as \"{StorePasteModel.CodePreview("dd--1-")}\"");
        if (StorePasteModel.CodePreview("DD1") != "") fails.Add("a code that is already clean must show no preview");
        if (StorePasteModel.CodePreview("   ") != "") fails.Add("empty text must show no preview");
        if (!StorePasteModel.CodePreview("!!").Contains("letters, numbers")) fails.Add("a code with nothing usable must say what is allowed");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_StoreCodeOfAnotherClientNamesThatClient()
    {
        Fresh();
        AddStore("CLIENT A", "DD1", "Tarlac");
        var fails = new List<string>();
        string? msg = MJdatabase.CheckCompanyStore("DWARDO", "dd 1", "Capas");
        if (msg == null || !msg.Contains("CLIENT A") || !msg.Contains("DD1")) fails.Add($"other client's code gave \"{msg}\"");
        if (MJdatabase.CheckCompanyStore("DWARDO", "DD2", "Capas") != null) fails.Add("a free code and name gave a message");
        AddStore("DWARDO", "DD5", "San Miguel");
        string? same = MJdatabase.CheckCompanyStore("DWARDO", "DD6", "san miguel");
        if (same == null || !same.Contains("DD5")) fails.Add($"same name gave \"{same}\"");
        if (MJdatabase.CheckCompanyStore("DWARDO", "", "Capas") == null) fails.Add("empty code gave no message");
        if (MJdatabase.CheckCompanyStore("DWARDO", "DD2", "") == null) fails.Add("empty name gave no message");
        if (MJdatabase.CheckCompanyStore("", "DD2", "Capas") == null) fails.Add("empty client gave no message");
        try { MJdatabase.RegisterCompanyStore("DWARDO", "dd 1", "Capas"); fails.Add("RegisterCompanyStore did not refuse"); }
        catch (InvalidOperationException ex) { if (ex.Message != msg) fails.Add($"register said \"{ex.Message}\", check said \"{msg}\""); }
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE ClientCompanyName = 'DWARDO' AND StoreName = 'Capas'") != 0) fails.Add("a refused store was saved");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_PasteParsesExcelText()
    {
        var rows = StorePasteModel.ParseRows("Code\tName\r\nDD1\tTarlac\r\n\r\n dd 2 \t Capas \r\nDD3\r\nDD4\tSan\tMiguel\textra\n");
        var fails = new List<string>();
        if (rows.Count != 4) { fails.Add($"{rows.Count} rows, expected 4 (header and blank line skipped)"); return string.Join("; ", fails); }
        if (rows[0].Code != "DD1" || rows[0].Name != "Tarlac") fails.Add($"row 1 = {rows[0].Code}|{rows[0].Name}");
        if (rows[1].Code != "dd 2" || rows[1].Name != "Capas") fails.Add($"row 2 = {rows[1].Code}|{rows[1].Name}");
        if (rows[2].Code != "DD3" || rows[2].Name != "") fails.Add($"row 3 = {rows[2].Code}|{rows[2].Name}");
        if (rows[3].Code != "DD4" || rows[3].Name != "San") fails.Add($"row 4 = {rows[3].Code}|{rows[3].Name} (only the first two columns count)");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_PasteThreeRowsOneDuplicateAddsTwo()
    {
        Fresh();
        var rows = StorePasteModel.Classify("DWARDO", StorePasteModel.ParseRows("DD1\tTarlac\r\nDD2\tCapas\r\ndd 1\tTarlac again"));
        var fails = new List<string>();
        if (StatusList(rows) != "Ok,Ok,Duplicate") fails.Add($"statuses {StatusList(rows)}, expected Ok,Ok,Duplicate");
        var res = StorePasteModel.AddValid("DWARDO", rows);
        if (res.Added != 2) fails.Add($"{res.Added} added, expected 2");
        if (res.Skipped != 1) fails.Add($"{res.Skipped} skipped, expected 1");
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE ClientCompanyName = 'DWARDO'") != 2) fails.Add("DWARDO does not have exactly 2 stores");
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE ClientCompanyName = 'DWARDO' AND StoreCode = 'DD1' AND StoreName = 'Tarlac'") != 1) fails.Add("DD1 Tarlac not saved once");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_PasteStatusesNameTheOwnerAndSkipInvalid()
    {
        Fresh();
        AddStore("CLIENT A", "DD1", "Alpha");
        AddStore("DWARDO", "DD7", "Paniqui");
        long routesBefore = Long("SELECT COUNT(*) FROM DeliveryRoutes");
        string placesBefore = Str("SELECT COALESCE(group_concat(ConnectedPlaces, '|'), '') FROM DeliveryRoutes");
        var rows = StorePasteModel.Classify("DWARDO", StorePasteModel.ParseRows("DD1\tTarlac\r\n!!\tNothing\r\nDD9\t\r\nDD3\tCapas\r\nDD8\tpaniqui"));
        var fails = new List<string>();
        if (StatusList(rows) != "AlreadyRegistered,Invalid,Invalid,Ok,AlreadyRegistered") fails.Add($"statuses {StatusList(rows)}");
        if (!rows[0].Message.Contains("CLIENT A")) fails.Add($"row 1 message \"{rows[0].Message}\" does not name CLIENT A");
        if (rows[1].Message.Length == 0 || rows[2].Message.Length == 0) fails.Add("an invalid row has no message");
        var res = StorePasteModel.AddValid("DWARDO", rows);
        if (res.Added != 1 || res.Skipped != 4) fails.Add($"added {res.Added}, skipped {res.Skipped}; expected 1 and 4");
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE StoreCode = 'DD3' AND ClientCompanyName = 'DWARDO'") != 1) fails.Add("DD3 not registered");
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE StoreCode = 'DD1' AND ClientCompanyName = 'DWARDO'") != 0) fails.Add("DD1 of CLIENT A was registered for DWARDO");
        if (Str("SELECT DestinationRoute FROM ClientStores WHERE StoreCode = 'DD3'") != "") fails.Add("a pasted store was given a route");
        if (Long("SELECT COUNT(*) FROM DeliveryRoutes") != routesBefore || Str("SELECT COALESCE(group_concat(ConnectedPlaces, '|'), '') FROM DeliveryRoutes") != placesBefore) fails.Add("the paste changed routes");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RouteCodeOfAnotherClientRefusedAndNothingSaved()
    {
        Fresh();
        Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('CLIENT A', 'RC1', 'Alpha', '', 1)");
        Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'RC9', 'Old', '', 0)");
        long before = Long("SELECT COUNT(*) FROM DeliveryRoutes");
        var fails = new List<string>();
        string? msg = MJdatabase.CheckRouteCode("DWARDO", "rc1", "Beta");
        if (msg == null || !msg.Contains("RC1") || !msg.Contains("CLIENT A") || !msg.Contains("different route code")) fails.Add($"other client's route code gave \"{msg}\"");
        string? own = MJdatabase.CheckRouteCode("DWARDO", "RC9", "Another");
        if (own == null || !own.Contains("RC9")) fails.Add($"own archived route's code gave \"{own}\"");
        string? sameName = MJdatabase.CheckRouteCode("DWARDO", "RC2", "old");
        if (sameName == null || !sameName.Contains("Old")) fails.Add($"same route name gave \"{sameName}\"");
        if (MJdatabase.CheckRouteCode("DWARDO", "RC2", "Beta") != null) fails.Add("a free route code and name gave a message");
        if (MJdatabase.CheckRouteCode("DWARDO", "", "Beta") == null) fails.Add("empty route code gave no message");
        if (MJdatabase.CheckRouteCode("DWARDO", "RC2", "") == null) fails.Add("empty route name gave no message");
        if (MJdatabase.CheckRouteCode("", "RC2", "Beta") == null) fails.Add("empty client gave no message");
        try { MJdatabase.SaveDeliveryRoute(new DeliveryRoute { ClientCompanyName = "DWARDO", RouteCode = "RC1", RouteName = "Beta", ConnectedPlaces = "", IsActive = true }); fails.Add("SaveDeliveryRoute did not refuse"); }
        catch (InvalidOperationException ex) { if (msg != null && ex.Message != msg) fails.Add($"save said \"{ex.Message}\", check said \"{msg}\""); }
        if (Long("SELECT COUNT(*) FROM DeliveryRoutes") != before) fails.Add("a route was saved or lost");
        if (Str("SELECT ClientCompanyName FROM DeliveryRoutes WHERE RouteCode = 'RC1'") != "CLIENT A") fails.Add("RC1 no longer belongs to CLIENT A");
        return string.Join("; ", fails);
    }
    // ---- Task 7: optional one-route-per-store rule per client (default off).

    private static (DeliveryRoute dw, DeliveryRoute dx, ClientStore dd1) TwoRoutesOneStore(string dwPlaces, string dxPlaces)
    {
        Fresh();
        long a = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DW', 'Dw North', @p, 1)", ("@p", dwPlaces));
        long b = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DX', 'Dx South', @p, 1)", ("@p", dxPlaces));
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson) VALUES (0, 'DWARDO', 'DD-1', 'San Miguel', '', '')");
        var dd1 = new ClientStore { StoreCode = "DD-1", StoreName = "San Miguel", ClientCompanyName = "DWARDO" };
        return (new DeliveryRoute { Id = (int)a, ClientCompanyName = "DWARDO", RouteCode = "DW", RouteName = "Dw North", ConnectedPlaces = dwPlaces },
                new DeliveryRoute { Id = (int)b, ClientCompanyName = "DWARDO", RouteCode = "DX", RouteName = "Dx South", ConnectedPlaces = dxPlaces }, dd1);
    }

    [Check]
    public static string RouteStores_OneRouteSwitchRefusedWithMultiRouteStore()
    {
        var (dw, dx, dd1) = TwoRoutesOneStore("DD-1 - San Miguel", "DD-1 - San Miguel");
        var fails = new List<string>();
        if (MJdatabase.GetOneRoutePerStore("DWARDO")) fails.Add("switch is on by default");
        string? msg = MJdatabase.SetOneRoutePerStore("DWARDO", true);
        if (msg == null) fails.Add("switch on was not refused");
        else if (!msg.Contains("DD-1") || !msg.Contains("DW") || !msg.Contains("DX")) fails.Add($"refusal does not name the store and its routes: \"{msg}\"");
        if (MJdatabase.GetOneRoutePerStore("DWARDO")) fails.Add("switch is on after a refusal");
        var multi = MJdatabase.StoresOnSeveralRoutes("DWARDO");
        if (multi.Count != 1 || multi[0].Store.StoreCode != "DD-1" || multi[0].RouteCodes.Count != 2) fails.Add($"multi-route list has {multi.Count} entries");
        MJdatabase.SetStoreOnRoute(dx, dd1, false); // fixed: now on DW only
        if (MJdatabase.SetOneRoutePerStore("DWARDO", true) != null) fails.Add("switch on refused after the store was taken off one route");
        if (!MJdatabase.GetOneRoutePerStore("DWARDO")) fails.Add("switch is not on after it was accepted");
        if (MJdatabase.GetOneRoutePerStore("OTHER CLIENT")) fails.Add("another client's switch turned on");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_OneRuleRefusesSecondRouteAdd()
    {
        var (dw, dx, dd1) = TwoRoutesOneStore("DD-1 - San Miguel", "");
        var fails = new List<string>();
        if (MJdatabase.SetOneRoutePerStore("DWARDO", true) != null) fails.Add("switch on refused");
        string? why = MJdatabase.OneRouteRefusal(dx, dd1);
        if (why != "DD-1 is already on route DW. Take it off there first.") fails.Add($"refusal text \"{why}\"");
        if (MJdatabase.SetStoreOnRoute(dx, dd1, true)) fails.Add("second-route add returned true");
        if (SavedPlaces(dx.Id) != "") fails.Add($"DX was written: \"{SavedPlaces(dx.Id)}\"");
        if (MJdatabase.OneRouteRefusal(dw, dd1) != null) fails.Add("refusal on the route the store is already on");
        var other = new ClientStore { StoreCode = "DD-9", StoreName = "Paniqui", ClientCompanyName = "DWARDO" };
        if (!MJdatabase.SetStoreOnRoute(dx, other, true)) fails.Add("a store on no route could not be added");
        if (!MJdatabase.SetStoreOnRoute(dw, dd1, false)) fails.Add("remove under the rule returned false");
        if (!MJdatabase.SetStoreOnRoute(dx, dd1, true)) fails.Add("add after taking it off DW returned false");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_OneRuleOffAllowsSecondRouteAgain()
    {
        var (dw, dx, dd1) = TwoRoutesOneStore("DD-1 - San Miguel", "");
        var fails = new List<string>();
        MJdatabase.SetOneRoutePerStore("DWARDO", true);
        if (MJdatabase.SetStoreOnRoute(dx, dd1, true)) fails.Add("add was not refused while on");
        if (MJdatabase.SetOneRoutePerStore("DWARDO", false) != null) fails.Add("switch off refused");
        if (MJdatabase.GetOneRoutePerStore("DWARDO")) fails.Add("switch still on");
        if (MJdatabase.OneRouteRefusal(dx, dd1) != null) fails.Add("refusal still given when off");
        if (!MJdatabase.SetStoreOnRoute(dx, dd1, true)) fails.Add("add refused when off");
        if (!SavedPlaces(dx.Id).Contains("DD-1")) fails.Add($"DX not written: \"{SavedPlaces(dx.Id)}\"");
        if (MJdatabase.SetOneRoutePerStore("DWARDO", false) != null) fails.Add("switch off refused with a multi-route store");
        return string.Join("; ", fails);
    }
    [Check]
    public static string RouteStores_OneRuleRefusalCapsAtFiveStores()
    {
        Fresh();
        string places = string.Join("; ", Enumerable.Range(1, 7).Select(n => $"DD-{n} - Store {n}"));
        Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DW', 'Dw', @p, 1)", ("@p", places));
        Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', 'DX', 'Dx', @p, 1)", ("@p", places));
        for (int n = 1; n <= 7; n++)
            Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson) VALUES (0, 'DWARDO', @c, @n, '', '')", ("@c", $"DD-{n}"), ("@n", $"Store {n}"));
        var fails = new List<string>();
        string? msg = MJdatabase.SetOneRoutePerStore("DWARDO", true);
        if (msg == null) return "switch on was not refused";
        int named = Enumerable.Range(1, 7).Count(n => msg.Contains($"DD-{n} (DW, DX)"));
        if (named != 5) fails.Add($"{named} stores named, expected 5: \"{msg}\"");
        if (!msg.Contains("and 2 more")) fails.Add("no \"and 2 more\"");
        if (!msg.EndsWith("They are marked 'Also on ...' in the store list.")) fails.Add("last sentence missing");
        if (msg.Length > 260) fails.Add($"message is {msg.Length} characters, too long for the label");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_OneRuleSingleStoreRefusalExactText()
    {
        TwoRoutesOneStore("DD-1 - San Miguel", "DD-1 - San Miguel");
        string? msg = MJdatabase.SetOneRoutePerStore("DWARDO", true);
        const string want = "Cannot turn this on yet. Take these stores off all but one route first: DD-1 (DW, DX). They are marked 'Also on ...' in the store list.";
        return msg == want ? "" : $"refusal was \"{msg}\"";
    }

    [Check]
    public static string RouteStores_OneRuleKeyIgnoresClientCase()
    {
        Fresh();
        var fails = new List<string>();
        if (MJdatabase.SetOneRoutePerStore("dwardo", true) != null) fails.Add("switch on refused");
        if (!MJdatabase.GetOneRoutePerStore("DWARDO")) fails.Add("DWARDO not on after setting dwardo");
        if (!MJdatabase.GetOneRoutePerStore("Dwardo")) fails.Add("Dwardo not on after setting dwardo");
        if (Long("SELECT COUNT(*) FROM SystemSettings WHERE SettingKey LIKE 'Routes.OneRoutePerStore.%'") != 1) fails.Add("more than one key saved");
        if (Str("SELECT SettingKey FROM SystemSettings WHERE SettingKey LIKE 'Routes.OneRoutePerStore.%'") != "Routes.OneRoutePerStore.DWARDO") fails.Add("key is not upper case");
        return string.Join("; ", fails);
    }

    // ---- Task 8: the new-client path (pending stores and routes) follows the same rules.

    [Check]
    public static string RouteStores_PendingMembershipListsEveryRoute()
    {
        var routes = new List<(string, IEnumerable<string>)> { ("DW", new[] { "DD-1", "DD-2" }), ("DW1", new[] { "dd-2" }), ("DX", new[] { "DD-3" }) };
        var fails = new List<string>();
        string two = string.Join(",", PendingRouteModel.RoutesHoldingStore(routes, "DD-2"));
        if (two != "DW,DW1") fails.Add($"DD-2 on \"{two}\", expected DW,DW1");
        string one = string.Join(",", PendingRouteModel.RoutesHoldingStore(routes, "dd-1"));
        if (one != "DW") fails.Add($"dd-1 on \"{one}\", expected DW");
        if (PendingRouteModel.RoutesHoldingStore(routes, "DD-9").Count != 0) fails.Add("DD-9 found on a route");
        string text = PendingRouteModel.StateText(PendingRouteModel.RoutesHoldingStore(routes, "DD-2"));
        if (text != "On DW, DW1 · not saved yet") fails.Add($"state text \"{text}\"");
        if (PendingRouteModel.StateText(new List<string>()) != "Not saved yet") fails.Add("empty state text wrong");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_PendingStoreTickedTwiceSavesOnce()
    {
        var typed = new List<(string Code, string Name)> { ("DD-1", "San Miguel"), ("DD-2", "Tarlac"), ("DD-3", "Capas") };
        var fails = new List<string>();
        var write = PendingRouteModel.StoresToWrite(new[] { "dd-2", "DD-2", " DD-2 ", "DD-1", "ZZ-9" }, typed);
        string got = string.Join(",", write.Select(w => w.Code));
        if (got != "DD-1,DD-2") fails.Add($"stores to write \"{got}\", expected DD-1,DD-2 (typed order, once each, untyped dropped)");
        // and through the real writer on a fresh database: ticking the same store twice leaves one entry
        var route = RouteWithPlaces("");
        var errors = new List<string>();
        MJdatabase.RegisterCompanyStore("DWARDO", "DD-1", "San Miguel");
        MJdatabase.RegisterCompanyStore("DWARDO", "DD-2", "Tarlac");
        var savedStores = MJdatabase.GetClientStores("DWARDO");
        int first = PendingRouteModel.SaveRouteStores(route, savedStores, write, errors);
        int again = PendingRouteModel.SaveRouteStores(route, savedStores, write, errors);
        var places = new DeliveryRoute { ConnectedPlaces = SavedPlaces(route.Id) }.PlacesList;
        if (first != 2) fails.Add($"first save added {first}, expected 2");
        if (again != 0) fails.Add($"second save added {again}, expected 0");
        if (places.Count != 2 || places[0] != "DD-1 - San Miguel" || places[1] != "DD-2 - Tarlac") fails.Add($"saved \"{string.Join("; ", places)}\", expected \"DD-1 - San Miguel; DD-2 - Tarlac\"");
        if (errors.Count != 0) fails.Add("errors: " + string.Join(" | ", errors));
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_PendingCreateSavesWhatWasTyped()
    {
        Fresh();
        var typed = new List<(string Code, string Name)> { ("DD-1", "San Miguel"), ("DD-2", "Tarlac"), ("DD-3", "Capas") };
        var pendingRoutes = new List<(string Code, string Name, string[] Ticked)> { ("PR-A", "North", new[] { "DD-2", "DD-1" }), ("PR-B", "South", new[] { "dd-2", "DD-3" }) };
        foreach (var (c, n) in typed) MJdatabase.RegisterCompanyStore("DWARDO", c, n);
        var savedStores = MJdatabase.GetClientStores("DWARDO");
        var errors = new List<string>();
        foreach (var pr in pendingRoutes)
        {
            if (!MJdatabase.SaveDeliveryRoute(new DeliveryRoute { ClientCompanyName = "DWARDO", RouteCode = pr.Code, RouteName = pr.Name, Description = "", ConnectedPlaces = "", IsActive = true })) errors.Add("route " + pr.Code + " not saved");
            var fresh = MJdatabase.GetDeliveryRoutesByClient("DWARDO", activeOnly: false).First(x => x.RouteCode == pr.Code);
            PendingRouteModel.SaveRouteStores(fresh, savedStores, PendingRouteModel.StoresToWrite(pr.Ticked, typed), errors);
        }
        var fails = new List<string>(errors);
        string Saved(string code) => string.Join("; ", MJdatabase.GetDeliveryRoutesByClient("DWARDO", activeOnly: false).First(x => x.RouteCode == code).PlacesList);
        if (Saved("PR-A") != "DD-1 - San Miguel; DD-2 - Tarlac") fails.Add($"PR-A saved \"{Saved("PR-A")}\"");
        if (Saved("PR-B") != "DD-2 - Tarlac; DD-3 - Capas") fails.Add($"PR-B saved \"{Saved("PR-B")}\"");
        return string.Join("; ", fails);
    }

    // ---- Task 9: every writer of route store text writes the canonical PlaceFor text, and a source scan keeps it that way.

    private static long DwRoute(string code, string name, string places) =>
        Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('DWARDO', @c, @n, @p, 1)", ("@c", code), ("@n", name), ("@p", places));

    private static long DwStoreOn(string code, string name, string routeLabel) =>
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson) VALUES (0, 'DWARDO', @c, @n, @r, '')", ("@c", code), ("@n", name), ("@r", routeLabel));

    private static ClientStore DwSaved(long id) => MJdatabase.GetClientStores("DWARDO", includeArchived: true).First(s => s.Id == id);

    /// <summary>Adds a failure when the route's saved text is not <paramref name="want"/>, or when an entry of the store is not its canonical text.</summary>
    private static void ExpectPlaces(List<string> fails, string when, long routeId, string want, ClientStore? st = null)
    {
        string saved = SavedPlaces((int)routeId);
        if (saved != want) fails.Add($"{when}: route #{routeId} saved \"{saved}\", expected \"{want}\"");
        if (st == null) return;
        var entries = new DeliveryRoute { ConnectedPlaces = saved }.PlacesList.Where(p => MJdatabase.PlaceIsStore(p, st)).ToList();
        if (entries.Count != 1 || entries[0] != MJdatabase.PlaceFor(st)) fails.Add($"{when}: route #{routeId} entries for the store [{string.Join(" | ", entries)}], expected exactly \"{MJdatabase.PlaceFor(st)}\"");
    }

    [Check]
    public static string RouteStores_RenameCodedStoreStaysOnRoute()
    {
        Fresh();
        long r1 = DwRoute("DW-R1", "North", "DD-1 - San Miguel; DD-2 - Tarlac");
        long r2 = DwRoute("DW-R2", "South", "DD-2 - Tarlac; DD-3 - Capas"); // rule 16c: the same store on a second route
        AddStore("DWARDO", "DD-1", "San Miguel");
        AddStore("DWARDO", "DD-3", "Capas");
        long id = DwStoreOn("DD-2", "Tarlac", "DW-R1 - North");
        var fails = new List<string>();
        var st = DwSaved(id);
        st.StoreName = "Tarlac City";
        MJdatabase.SaveClientStore(st);
        ExpectPlaces(fails, "renamed", r1, "DD-1 - San Miguel; DD-2 - Tarlac City", st);
        ExpectPlaces(fails, "renamed", r2, "DD-2 - Tarlac City; DD-3 - Capas", st);
        st = DwSaved(id);
        st.StoreCode = "DD-5"; // re-coded after it was put on routes (Review Focus 4)
        MJdatabase.SaveClientStore(st);
        ExpectPlaces(fails, "re-coded", r1, "DD-1 - San Miguel; DD-5 - Tarlac City", st);
        ExpectPlaces(fails, "re-coded", r2, "DD-5 - Tarlac City; DD-3 - Capas", st);
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RenameNameOnlyStoreUpdatesRouteText()
    {
        Fresh();
        long r1 = DwRoute("DW-R1", "North", "DD-1 - San Miguel; Tarlac");
        long r2 = DwRoute("DW-R2", "South", "Tarlac; DD-3 - Capas");
        AddStore("DWARDO", "DD-1", "San Miguel");
        AddStore("DWARDO", "DD-3", "Capas");
        long id = DwStoreOn("", "Tarlac", "DW-R1 - North");
        var fails = new List<string>();
        var st = DwSaved(id);
        st.StoreName = "Tarlac City";
        MJdatabase.SaveClientStore(st);
        ExpectPlaces(fails, "renamed", r1, "DD-1 - San Miguel; Tarlac City", st);
        ExpectPlaces(fails, "renamed", r2, "Tarlac City; DD-3 - Capas", st);
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_AutoCodeCleanUpKeepsStoresOnRoutes()
    {
        Fresh();
        Exec("DELETE FROM SystemSettings WHERE SettingKey = 'Fix.AutoStoreCodes.v1'");
        long route = DwRoute("DW-R1", "North", "203077 - RR Laoag; R1-S02 - Paniqui; Elsewhere");
        long laoag = DwStoreOn("R1-S01", "203077 - RR Laoag", ""); // the real code moves out of the name
        long paniqui = DwStoreOn("R1-S02", "Paniqui", "");         // no real code: the made-up one is cleared
        var fails = new List<string>();
        MJdatabase.MigrateAutoStoreCodes();
        var l = DwSaved(laoag);
        var p = DwSaved(paniqui);
        if (l.StoreCode != "203077" || l.StoreName != "RR Laoag") fails.Add($"Laoag saved as {l.StoreCode}|{l.StoreName}");
        if (p.StoreCode != "" || p.StoreName != "Paniqui") fails.Add($"Paniqui saved as {p.StoreCode}|{p.StoreName}");
        ExpectPlaces(fails, "after clean-up", route, "203077 - RR Laoag; Paniqui; Elsewhere", l);
        ExpectPlaces(fails, "after clean-up", route, "203077 - RR Laoag; Paniqui; Elsewhere", p);
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RegisterClientStoreWritesCanonicalPlace()
    {
        Fresh();
        long route = DwRoute("DW-R1", "North", "DD-1 - San Miguel");
        AddStore("DWARDO", "DD-1", "San Miguel");
        var fails = new List<string>();
        var st = MJdatabase.RegisterClientStore("DWARDO", "dd-2", " Tarlac ", "DW-R1 - North");
        ExpectPlaces(fails, "registered", route, "DD-1 - San Miguel; DD-2 - Tarlac", st);
        if (DwSaved(st.Id).DestinationRoute != "DW-R1 - North") fails.Add($"store route label \"{DwSaved(st.Id).DestinationRoute}\"");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_ArchiveRestoreDeleteUseTheStoreEntry()
    {
        Fresh();
        long route = DwRoute("DW-R1", "North", "DD-1 - San Miguel; DD-2 - Tarlac; DD-3 - Capas");
        AddStore("DWARDO", "DD-1", "San Miguel");
        long tarlac = DwStoreOn("DD-2", "Tarlac", "DW-R1 - North");
        long capas = DwStoreOn("DD-3", "Capas", "DW-R1 - North");
        var fails = new List<string>();
        MJdatabase.ArchiveClientStore((int)tarlac);
        ExpectPlaces(fails, "archived", route, "DD-1 - San Miguel; DD-3 - Capas");
        MJdatabase.RestoreClientStore((int)tarlac);
        ExpectPlaces(fails, "restored", route, "DD-1 - San Miguel; DD-3 - Capas; DD-2 - Tarlac", DwSaved(tarlac));
        MJdatabase.DeleteClientStore((int)capas);
        ExpectPlaces(fails, "deleted", route, "DD-1 - San Miguel; DD-2 - Tarlac");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_RenameToAnotherStoresNameLeavesItsEntry()
    {
        Fresh();
        long route = DwRoute("DW-R1", "North", "DD-2 - Tarlac; Capas"); // "Capas" is DD-3's legacy entry
        long dd2 = DwStoreOn("DD-2", "Tarlac", "DW-R1 - North");
        long dd3 = DwStoreOn("DD-3", "Capas", "DW-R1 - North");
        var fails = new List<string>();
        var st = DwSaved(dd2);
        st.StoreName = "Capas"; // names are not unique per client: only the code is checked
        MJdatabase.SaveClientStore(st);
        ExpectPlaces(fails, "renamed to Capas", route, "DD-2 - Capas; Capas");
        var r = MJdatabase.GetDeliveryRoutes(activeOnly: false).First(x => x.Id == route);
        if (!MJdatabase.StoreIsOnRoute(r, DwSaved(dd3))) fails.Add("DD-3 dropped off the route");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_AutoCodeCleanUpLeavesAnotherStoresEntry()
    {
        Fresh();
        Exec("DELETE FROM SystemSettings WHERE SettingKey = 'Fix.AutoStoreCodes.v1'");
        long route = DwRoute("DW-R1", "North", "203099 - Paniqui; R1-S02 - Paniqui; Elsewhere");
        long real = DwStoreOn("203099", "Paniqui", "");  // a real store with the same name
        long auto = DwStoreOn("R1-S02", "Paniqui", ""); // made-up code, cleared by the clean-up ("" , "Paniqui")
        var fails = new List<string>();
        MJdatabase.MigrateAutoStoreCodes();
        var a = DwSaved(auto);
        if (a.StoreCode != "" || a.StoreName != "Paniqui") fails.Add($"made-up store saved as {a.StoreCode}|{a.StoreName}");
        // "203099 - Paniqui" is the other store's own entry; "Paniqui" would name both stores, so "R1-S02 - Paniqui" is left too
        ExpectPlaces(fails, "after clean-up", route, "203099 - Paniqui; R1-S02 - Paniqui; Elsewhere", DwSaved(real));
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_DeleteStoreClearsEveryRoute()
    {
        Fresh();
        long r1 = DwRoute("DW-R1", "North", "DD-1 - San Miguel; DD-3 - Capas");
        long r2 = DwRoute("DW-R2", "South", "DD-3 - Capas; DD-2 - Tarlac");
        long other = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES ('OTHER CLIENT', 'OC-R1', 'East', 'DD-3 - Capas', 1)");
        AddStore("DWARDO", "DD-1", "San Miguel");
        AddStore("DWARDO", "DD-2", "Tarlac");
        long capas = DwStoreOn("DD-3", "Capas", "DW-R1 - North");
        var fails = new List<string>();
        MJdatabase.DeleteClientStore((int)capas);
        ExpectPlaces(fails, "deleted", r1, "DD-1 - San Miguel");
        ExpectPlaces(fails, "deleted", r2, "DD-2 - Tarlac");
        ExpectPlaces(fails, "deleted (other client's route)", other, "DD-3 - Capas");
        if (Long("SELECT COUNT(*) FROM ClientStores WHERE Id = @id", ("@id", capas)) != 0) fails.Add("store row not deleted");
        return string.Join("; ", fails);
    }

    [Check]
    public static string RouteStores_SetRoutePlacesWritesCanonicalText()
    {
        Fresh();
        AddStore("DWARDO", "DD-1", "San Miguel");
        AddStore("DWARDO", "DD-2", "Tarlac");
        AddStore("OTHER CLIENT", "OC-1", "Capas"); // another client's store is not one of DWARDO's
        var route = new DeliveryRoute { ClientCompanyName = "DWARDO", RouteCode = "DW-R1", RouteName = "North", IsActive = true };
        var fails = new List<string>();
        // what the route screens hand over: a bare name, other case and spacing, a repeat of the same store, unknown text
        MJdatabase.SetRoutePlaces(route, new[] { "Tarlac", " dd-1  -  san miguel ", "DD-2 - Tarlac", "  Capas ", "", "Nowhere   Store" });
        const string want = "DD-2 - Tarlac; DD-1 - San Miguel; Capas; Nowhere Store";
        if (route.ConnectedPlaces != want) fails.Add($"set \"{route.ConnectedPlaces}\", expected \"{want}\"");
        var stores = MJdatabase.GetClientStores("DWARDO");
        foreach (var st in stores.Where(s => s.StoreCode.StartsWith("DD-")))
        {
            var entries = route.PlacesList.Where(p => MJdatabase.PlaceIsStore(p, st)).ToList();
            if (entries.Count != 1 || entries[0] != MJdatabase.PlaceFor(st)) fails.Add($"entries for {st.StoreCode} [{string.Join(" | ", entries)}]");
        }
        if (!MJdatabase.SaveDeliveryRoute(route)) fails.Add("SaveDeliveryRoute returned false");
        string saved = Str("SELECT ConnectedPlaces FROM DeliveryRoutes WHERE RouteCode = 'DW-R1'");
        if (saved != want) fails.Add($"saved \"{saved}\", expected \"{want}\"");
        return string.Join("; ", fails);
    }

    // The source scan. Allowed to write DeliveryRoutes.ConnectedPlaces: the two files below (the one writer SetStoreOnRoute, the list
    // writers WriteRoutePlaces and SetRoutePlaces, the reorder, the one-time repair), and two methods of MJdatabase.cs.
    private static readonly string[] RouteTextWriterFiles = { "MJdatabase.RouteBuilder.cs", "MJdatabase.RouteRepair.cs" };

    // In MJdatabase.cs only: EnsureDefaultDeliveryRoutes (the seed of the default routes) and SaveDeliveryRouteCore (the whole-route
    // save behind SaveDeliveryRoute). Every other method of MJdatabase.cs that writes the text fails the scan.
    private static readonly string[] RouteTextWriterMethodsInMJdatabase = { "EnsureDefaultDeliveryRoutes", "SaveDeliveryRouteCore" };

    // A write: "ConnectedPlaces =" or "+=" in C# or SQL (not "==", and not "= reader.…", which only fills the model from a row it read),
    // or an SQL INSERT INTO DeliveryRoutes that names the column.
    private static readonly Regex PlacesAssign = new(@"\bConnectedPlaces\s*\+?=(?!=)(?!\s*reader\.)");
    private static readonly Regex PlacesInsert = new(@"INSERT\s+(?:OR\s+\w+\s+)?INTO\s+DeliveryRoutes\b.*\bConnectedPlaces\b", RegexOptions.IgnoreCase);

    /// <summary>Line ranges (0-based, inclusive) of the named methods: from the signature to its closing brace.</summary>
    private static List<(int From, int To)> MethodSpans(string[] lines, IEnumerable<string> names)
    {
        var spans = new List<(int, int)>();
        foreach (var n in names)
        {
            var head = new Regex(@"^\s*(?:public|private|internal|protected)\s[\w<>\[\],?\s]*\s" + Regex.Escape(n) + @"\s*\(");
            int start = Array.FindIndex(lines, l => head.IsMatch(l));
            if (start < 0) continue; // not found: nothing is allowed in its name
            int depth = 0;
            bool opened = false;
            for (int i = start; i < lines.Length; i++)
            {
                foreach (char c in lines[i]) { if (c == '{') { depth++; opened = true; } else if (c == '}') depth--; }
                if (opened && depth == 0) { spans.Add((start, i)); break; }
            }
        }
        return spans;
    }

    /// <summary>Every source line under <paramref name="root"/> that writes a route's store text outside the allowlist, as "path:line: text".</summary>
    private static List<string> RouteTextWritersOutsideAllowlist(string root)
    {
        string[] skipFolders = { "bin", "obj", "Verify", ".claude", ".git" };
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.Split('/').Any(seg => skipFolders.Contains(seg, StringComparer.OrdinalIgnoreCase))) continue;
            string name = Path.GetFileName(file);
            if (RouteTextWriterFiles.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            var lines = File.ReadAllLines(file);
            var allowed = name.Equals("MJdatabase.cs", StringComparison.OrdinalIgnoreCase) ? MethodSpans(lines, RouteTextWriterMethodsInMJdatabase) : new List<(int From, int To)>();
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//") || t.StartsWith("*")) continue;
                if (!PlacesAssign.IsMatch(lines[i]) && !PlacesInsert.IsMatch(lines[i])) continue;
                if (allowed.Any(s => i >= s.From && i <= s.To)) continue;
                found.Add($"{rel}:{i + 1}: {t}");
            }
        }
        return found;
    }

    /// <summary>The app's source folder: walks up from where Verify runs to the folder that holds Database/MJdatabase.RouteBuilder.cs.</summary>
    private static string? SourceRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "Database", "MJdatabase.RouteBuilder.cs"))) return d.FullName;
        return null;
    }

    [Check]
    public static string RouteStores_AllWritersCanonical()
    {
        string? root = SourceRoot();
        if (root == null) return $"source folder not found above {AppContext.BaseDirectory}";
        if (!File.Exists(Path.Combine(root, "Database", "MJdatabase.cs"))) return $"Database/MJdatabase.cs not found in {root}";
        var found = RouteTextWritersOutsideAllowlist(root);
        return found.Count == 0 ? "" : $"{found.Count} write(s) of route store text outside the one writer: " + string.Join(" | ", found);
    }

    [Check]
    public static string RouteStores_SourceScanCatchesNewWriter()
    {
        string root = Path.Combine(Root, $"scan_{Guid.NewGuid():N}");
        void Put(string rel, params string[] lines)
        {
            string p = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllLines(p, lines);
        }
        Put("Database/MJdatabase.RouteBuilder.cs", "route.ConnectedPlaces = string.Join(\"; \", places);");
        Put("Database/MJdatabase.cs",
            "public static partial class MJdatabase",
            "{",
            "    private static bool SaveDeliveryRouteCore(DeliveryRoute r)",
            "    {",
            "        cmd.CommandText = \"UPDATE DeliveryRoutes SET ConnectedPlaces = @p WHERE Id = @id\";",
            "    }",
            "    public static void SneakyWriter(DeliveryRoute r)",
            "    {",
            "        r.ConnectedPlaces = \"Tarlac\";",
            "    }",
            "    public static List<DeliveryRoute> Load()",
            "    {",
            "        list.Add(new DeliveryRoute { ConnectedPlaces = reader.GetString(4) });",
            "    }",
            "}");
        Put("Views/NewPanel.cs",
            "// route.ConnectedPlaces = \"only a comment\";",
            "if (route.ConnectedPlaces == \"\") return;",
            "route.ConnectedPlaces = string.Join(\"; \", names);",
            "cmd.CommandText = \"INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, ConnectedPlaces) VALUES (@c, @r, @p)\";",
            "route.ConnectedPlaces += \"; Tarlac\";");
        Put("Verify/Checks.Other.cs", "r.ConnectedPlaces = \"test data\";");
        Put("obj/Generated.cs", "r.ConnectedPlaces = \"generated\";");
        try
        {
            var found = RouteTextWritersOutsideAllowlist(root).Select(f => f.Substring(0, f.IndexOf(": ", StringComparison.Ordinal))).ToList();
            string want = "Database/MJdatabase.cs:9, Views/NewPanel.cs:3, Views/NewPanel.cs:4, Views/NewPanel.cs:5";
            return string.Join(", ", found) == want ? "" : $"scan found [{string.Join(", ", found)}], expected [{want}]";
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }
}
