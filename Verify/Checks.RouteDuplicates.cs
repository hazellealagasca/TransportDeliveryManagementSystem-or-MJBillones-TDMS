using System;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 10: the one route writer refuses a repeated route code or name in plain words, on every screen, and still allows a normal save and an edit.</summary>
    [Check]
    public static string RouteDuplicates_SaveDeliveryRouteRefusesInPlainWords()
    {
        Fresh();
        DeliveryRoute R(string client, string code, string name) => new DeliveryRoute { ClientCompanyName = client, RouteCode = code, RouteName = name, Description = "", IsActive = true };
        if (!MJdatabase.SaveDeliveryRoute(R("CLIENT A", "LD1", "Manila"))) return "a normal route was not saved";
        if (!MJdatabase.SaveDeliveryRoute(R("CLIENT A", "LD2", "Cebu"))) return "a second route with its own code and name was not saved";
        string Why(Func<bool> f) { try { f(); return ""; } catch (InvalidOperationException ex) { return ex.Message; } catch (Exception ex) { return "RAW: " + ex.Message; } }
        string a = Why(() => MJdatabase.SaveDeliveryRoute(R("CLIENT A", "LD3", " manila ")));
        if (!a.Contains("different route name")) return "the same route name for one client was not refused in plain words: " + a;
        string b = Why(() => MJdatabase.SaveDeliveryRoute(R("CLIENT B", "ld1", "Other")));
        if (!b.Contains("already used")) return "a route code used by another client (other capitals) was not refused: " + b;
        if (!MJdatabase.SaveDeliveryRoute(R("CLIENT B", "LD9", "Manila"))) return "another client's route with the same name as client A's was refused";
        var two = MJdatabase.GetDeliveryRoutesByClient("CLIENT A", activeOnly: false).Find(x => x.RouteCode == "LD2")!;
        two.RouteName = "manila";
        string c = Why(() => MJdatabase.SaveDeliveryRoute(two));
        if (!c.Contains("different route name")) return "renaming a route to another route's name was not refused: " + c;
        two.RouteName = "Cebu City";
        if (!MJdatabase.SaveDeliveryRoute(two)) return "a normal edit of a route was refused";
        return "";
    }
}
