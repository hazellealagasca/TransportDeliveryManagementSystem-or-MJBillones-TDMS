using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 10: New client, routes tab: a route code is used once (in the list and in every saved client) and a client's route names differ; said up front, never a raw SQLite error.</summary>
    [Check]
    public static string PendingRoute_DuplicateCodeOrNameIsRefusedUpFront()
    {
        Fresh();
        MJdatabase.SaveDeliveryRoute(new DeliveryRoute { ClientCompanyName = "OTHER CLIENT", RouteCode = "R-77", RouteName = "North", Description = "", IsActive = true });
        var list = new List<(string Code, string Name)> { ("0908", "LD-Tarlac"), ("LD1", "Manila") };
        if (MJdatabase.CheckPendingRoute("NEW CLIENT", "LD3", "Cebu", list) != null) return "a free code and name was refused";
        string? a = MJdatabase.CheckPendingRoute("NEW CLIENT", "ld1", "Other", list);
        if (a == null || !a.Contains("already used")) return "the same route code (any capitals) in the list was accepted: " + a;
        string? b = MJdatabase.CheckPendingRoute("NEW CLIENT", "LD2", " ld-tarlac ", list);
        if (b == null || !b.Contains("different route name")) return "the same route name in the list was accepted: " + b;
        string? c = MJdatabase.CheckPendingRoute("NEW CLIENT", "r-77", "South", list);
        if (c == null || !c.Contains("OTHER CLIENT")) return "a route code used by a saved client was accepted: " + c;
        if (MJdatabase.CheckPendingRoute("NEW CLIENT", "", "X", list) == null) return "a blank route code was accepted";
        if (MJdatabase.CheckPendingRoute("NEW CLIENT", "LD9", "", list) == null) return "a blank route name was accepted";
        if (MJdatabase.CheckPendingRoute("NEW CLIENT", "LD9", "", list, partial: true) != null) return "a partly typed route was refused while typing";
        var raw = new Microsoft.Data.Sqlite.SqliteException("SQLite Error 19: 'UNIQUE constraint failed: index 'ux_routes_name_client''.", 19);
        string plain = MJdatabase.PlainDbError(raw);
        if (plain.Contains("SQLite") || plain.Contains("ux_routes")) return "the raw database error reaches the person: " + plain;
        return "";
    }
}
