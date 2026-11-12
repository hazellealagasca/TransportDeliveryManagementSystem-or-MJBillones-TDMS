using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12: the system suggests the smallest free truck that fits (by the category matrix); the user always picks.</summary>
public static partial class Checks
{
    private static void SeedSuggestionTrucks()
    {
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.EnsureCapacitySchema();
        Exec("DELETE FROM Trucks");
        MJdatabase.SaveTruck(new Truck { PlateNumber = "A-DRY", TruckType = "4T", CapacityCrates = 280, CargoType = "Dry Goods", Status = "Active" });
        MJdatabase.SaveTruck(new Truck { PlateNumber = "B-COLD", TruckType = "4T", CapacityCrates = 120, CargoType = "Cold / Chilled", Status = "Active" });
        MJdatabase.SaveTruck(new Truck { PlateNumber = "C-FWD", TruckType = "FWD/6T", CapacityCrates = 200, CargoType = "Cold / Chilled", Status = "Active" });
        MJdatabase.SaveTruck(new Truck { PlateNumber = "D-8T", TruckType = "8T", CapacityCrates = 280, CargoType = "Cold / Chilled", Status = "Active" });
    }

    [Check]
    public static string TruckSuggestion_SmallestFreeFittingTruck_ByMatrix_SkipsBusyAndDryForCold()
    {
        Fresh();
        SeedSuggestionTrucks();
        var today = DateTime.Today;
        var problems = new List<string>();
        string Plate(TruckRecommendation r) => r.RecommendedTruck?.PlateNumber ?? "(none)";
        var dry1 = MJdatabase.GetTruckRecommendation(1, today, "Dry Goods");
        if (Plate(dry1) != "A-DRY" || dry1.IsMultiTruckNeeded) problems.Add($"1 dry crate -> {Plate(dry1)}");
        var cold1 = MJdatabase.GetTruckRecommendation(1, today, "Cold / Chilled");
        if (Plate(cold1) != "B-COLD") problems.Add($"1 cold crate -> {Plate(cold1)} (a Dry Goods truck must be skipped)");
        var none = MJdatabase.GetTruckRecommendation(1, today, "");
        if (Plate(none) != "A-DRY" && Plate(none) != "B-COLD") problems.Add($"1 crate, no temp -> {Plate(none)}");
        // matrix sizing: 150 cold crates is above the 4T cold limit (120), so the next size up
        var cold150 = MJdatabase.GetTruckRecommendation(150, today, "Cold / Chilled");
        if (Plate(cold150) != "C-FWD") problems.Add($"150 cold crates -> {Plate(cold150)}");
        // Combi follows the same check as the capacity validator
        var combi = MJdatabase.GetTruckRecommendation(100, today, "Combi", 40, 60);
        var chosen = combi.RecommendedTruck;
        if (chosen == null || CapacityValidator.Validate(chosen, "Combi", 40, 60).State != CapacityState.Allow) problems.Add($"combi 40+60 -> {Plate(combi)} does not pass the capacity check");
        // a busy truck is skipped
        Exec("INSERT INTO Deliveries (DispatchNumber, ClientName, Destination, TruckId, TruckInfo, DriverId, DriverName, AideId, AideNames, GCrate, ECrate, TotalCrates, DispatchDate, DepartureTime, Status) " +
             "SELECT 'TS-0001','CLIENT A','Route 1', Id,'B-COLD',1,'Juan',2,'Pedro',1,0,1,@d,'08:00 AM','Scheduled' FROM Trucks WHERE PlateNumber='B-COLD'", ("@d", today.ToString("yyyy-MM-dd")));
        var busy = MJdatabase.GetTruckRecommendation(1, today, "Cold / Chilled");
        if (Plate(busy) != "C-FWD") problems.Add($"B-COLD busy, 1 cold crate -> {Plate(busy)}");
        // nothing fits: multi-truck message
        var huge = MJdatabase.GetTruckRecommendation(2000, today, "Cold / Chilled");
        if (!huge.IsMultiTruckNeeded || huge.RequiredTruckCount < 2) problems.Add("2000 crates did not ask for several trucks");
        return string.Join(" | ", problems);
    }

    [Check]
    public static string TruckSuggestion_BiggerTruckThanNeeded_IsAllowed_NotBlocked()
    {
        Fresh();
        SeedSuggestionTrucks();
        var big = MJdatabase.GetTrucks().First(t => t.PlateNumber == "D-8T");
        var problems = new List<string>();
        foreach (var cargo in new[] { "Dry Goods", "Cold / Chilled" })
        {
            int cold = cargo == "Dry Goods" ? 0 : 1, dry = cargo == "Dry Goods" ? 1 : 0;
            var r = CapacityValidator.Validate(big, cargo, cold, dry);
            if (r.State != CapacityState.Allow || r.IsBlocked) problems.Add($"1 crate on the 8T truck ({cargo}) -> {r.State}");
        }
        var t = CapacityValidator.ValidateTotal(big, 1);
        if (t.State != CapacityState.Allow) problems.Add($"ValidateTotal -> {t.State}");
        return string.Join(" | ", problems);
    }

    [Check]
    public static string TruckSuggestion_LoadFromSchedule_LeavesTruckUnselected_AndLinkSelectsOnClick()
    {
        Fresh();
        SeedSuggestionTrucks();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            T? Get<T>(string n) where T : class => typeof(DispatchManagementView).GetField(n, F)!.GetValue(view) as T;
            var truck = Get<ComboBox>("cbTruck"); var link = Get<LinkLabel>("lnkUseSuggested");
            if (truck == null || link == null) return "Truck box or the 'Use suggested truck' link was not created";
            var problems = new List<string>();
            view.InitiateDispatchForSchedule(new LoadingSchedule { ClientName = "CLIENT A", Destination = "Route 1", ScheduleDate = DateTime.Today, GCrate = 1, ECrate = 0, TotalCrates = 1, CargoType = MJdatabase.CargoCold });
            Pump(300);
            if (truck.SelectedIndex != -1) problems.Add($"a truck was auto-selected (index {truck.SelectedIndex})");
            if (!link.Visible) problems.Add("the 'Use suggested truck' link is not shown when no truck is picked");
            typeof(DispatchManagementView).GetMethod("UseSuggestedTruck", F)!.Invoke(view, null);
            Pump(200);
            var trucks = (List<Truck>)typeof(DispatchManagementView).GetField("trucks", F)!.GetValue(view)!;
            string picked = truck.SelectedIndex >= 0 && truck.SelectedIndex < trucks.Count ? trucks[truck.SelectedIndex].PlateNumber : "(none)";
            if (picked != "B-COLD") problems.Add($"clicking the link selected {picked}, expected B-COLD");
            view.Dispose();
            return string.Join(" | ", problems);
        });
    }
}
