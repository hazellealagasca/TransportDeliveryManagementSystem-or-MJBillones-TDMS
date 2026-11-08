using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

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
}
