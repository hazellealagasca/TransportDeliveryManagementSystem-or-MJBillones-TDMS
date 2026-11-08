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
}
