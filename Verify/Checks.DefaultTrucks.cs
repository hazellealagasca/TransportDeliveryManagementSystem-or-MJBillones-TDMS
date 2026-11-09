using System;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 10: the load-planning matrix (crates only) is built in as 4T, FWD/6T and 8T; they can't be deleted or renamed; only the administrator changes crates.</summary>
    [Check]
    public static string DefaultTrucks_MatrixIsBuiltIn_Protected_AdminOnlyCrates()
    {
        Fresh();
        MJdatabase.EnsureCapacitySchema();
        var want = new[] { ("4T", 280, 120, 75, 72), ("FWD/6T", 300, 200, 150, 100), ("8T", 500, 280, 250, 140) };
        foreach (var (name, dry, cold, cc, cd) in want)
        {
            var m = MJdatabase.GetCapacityFor(name);
            if (m == null) return $"{name} is not built in";
            if (m.DryMax != dry || m.ColdMax != cold || m.CombiColdMax != cc || m.CombiDryMax != cd) return $"{name}: {m.DryMax}/{m.ColdMax}/{m.CombiColdMax}+{m.CombiDryMax}, expected {dry}/{cold}/{cc}+{cd}";
            if (!MJdatabase.GetTruckCategories().Any(c => c.CategoryName == name)) return $"{name} is not in the truck category list";
        }
        string Why(Action a) { try { a(); return ""; } catch (InvalidOperationException ex) { return ex.Message; } }
        if (!Why(() => MJdatabase.DeleteTruckCategory("4T")).Contains("can't be deleted")) return "a default category was deleted";
        if (MJdatabase.GetCapacityFor("4T") == null) return "the default category is gone after a delete attempt";
        var m4 = MJdatabase.GetCapacityFor("4T")!;
        if (!Why(() => MJdatabase.SaveCapacityMatrix(new TruckCapacity { CategoryName = "FOUR TON", DryMax = m4.DryMax, ColdMax = m4.ColdMax, CombiColdMax = m4.CombiColdMax, CombiDryMax = m4.CombiDryMax }, "4T")).Contains("can't be changed")) return "a default category was renamed";
        // the administrator may change the crates of a default category
        SessionContext.ActiveUser = new User { Username = "admin", Role = "Admin" };
        try
        {
            MJdatabase.SaveCapacityMatrix(new TruckCapacity { CategoryName = "4T", DryMax = 290, ColdMax = 120, CombiColdMax = 75, CombiDryMax = 72 }, "4T");
            if (MJdatabase.GetCapacityFor("4T")!.DryMax != 290) return "the administrator could not change the crates";
            // anybody else may not
            SessionContext.ActiveUser = new User { Username = "staff", Role = "Office Staff" };
            if (!Why(() => MJdatabase.SaveCapacityMatrix(new TruckCapacity { CategoryName = "4T", DryMax = 100, ColdMax = 50, CombiColdMax = 0, CombiDryMax = 0 }, "4T")).Contains("Only the administrator")) return "a non-administrator changed the crates";
            if (MJdatabase.GetCapacityFor("4T")!.DryMax != 290) return "the crates changed for a non-administrator";
        }
        finally { SessionContext.ActiveUser = null; }
        return "";
    }
}
