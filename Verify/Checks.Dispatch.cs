using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 10: with the cargo dropdown hidden, a dispatch is checked as total crates against the truck's maximum.</summary>
public static partial class Checks
{
    private static Truck TestTruck(string cargo) => new Truck { Id = 1, PlateNumber = "TST-100", TruckType = "No such category", CapacityCrates = 300, CargoType = cargo };

    [Check]
    public static string ValidateTotal_DryTruckAcceptsCrates()
    {
        Fresh();
        var r = CapacityValidator.ValidateTotal(TestTruck("Dry Goods"), 100);
        return r.State == CapacityState.Allow && r.Limit == 300 ? "" : $"{r.State} limit {r.Limit}: {r.Message}";
    }

    [Check]
    public static string ValidateTotal_OverMaxBlocks()
    {
        Fresh();
        var r = CapacityValidator.ValidateTotal(TestTruck("Cold / Chilled"), 301);
        var zero = CapacityValidator.ValidateTotal(TestTruck("Cold / Chilled"), 0);
        return r.State != CapacityState.Allow && r.Over == 1 && zero.State == CapacityState.NoCrates ? "" : $"{r.State} over {r.Over}; zero {zero.State}";
    }
}
