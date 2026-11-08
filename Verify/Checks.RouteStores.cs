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
}
