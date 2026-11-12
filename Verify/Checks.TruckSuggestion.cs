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
            var truck = Get<ComboBox>("cbTruck"); var link = Get<Button>("btnUseSuggested");
            if (truck == null || link == null) return "Truck box or the 'Use this truck' button was not created";
            var problems = new List<string>();
            view.InitiateDispatchForSchedule(new LoadingSchedule { ClientName = "CLIENT A", Destination = "Route 1", ScheduleDate = DateTime.Today, GCrate = 1, ECrate = 0, TotalCrates = 1, CargoType = MJdatabase.CargoCold });
            // The truck list and footer can fill a little later on a busy PC: wait up to 5 s instead of a fixed pause.
            for (int w = 0; w < 50 && !link.Visible; w++) Pump(100);
            Pump(100);
            if (truck.SelectedIndex != -1) problems.Add($"a truck was auto-selected (index {truck.SelectedIndex})");
            if (!link.Visible) problems.Add("the 'Use this truck' button is not shown when no truck is picked");
            else if (!link.TabStop || link.Text != "Use this truck") problems.Add($"the button is not keyboard reachable or has the wrong text ('{link.Text}')");
            var main = Get<Label>("lblSuggestMain"); var cap = Get<Label>("lblSuggestCap"); var recLbl = Get<Label>("lblRec");
            if (main == null || !main.Visible || !main.Text.Contains("Total 1 crates") || !main.Text.Contains("4-tonner") || !main.Text.Contains("B-COLD")) problems.Add($"footer main line is '{main?.Text}' (visible {main?.Visible}), expected the tonner and plate of the suggested truck");
            if (cap == null || !cap.Text.Contains("fits 1 of 120 crates")) problems.Add($"footer capacity is '{cap?.Text}', expected 'fits 1 of 120 crates'");
            if (recLbl != null && (!recLbl.Text.Contains("Total 1 crates") || !recLbl.Text.Contains("4-tonner") || !recLbl.Text.Contains("B-COLD"))) problems.Add($"footer text is '{recLbl.Text}'");
            try { var pnl = Get<Panel>("pnlRec"); if (pnl != null && Environment.GetEnvironmentVariable("TDMS_SHOT_DIR") is string dir) { using var bmp = new Bitmap(pnl.Width, pnl.Height); pnl.DrawToBitmap(bmp, new Rectangle(0, 0, pnl.Width, pnl.Height)); bmp.Save(Path.Combine(dir, "footer.png")); } } catch { }
            typeof(DispatchManagementView).GetMethod("UseSuggestedTruck", F)!.Invoke(view, null);
            Pump(200);
            var trucks = (List<Truck>)typeof(DispatchManagementView).GetField("trucks", F)!.GetValue(view)!;
            string picked = truck.SelectedIndex >= 0 && truck.SelectedIndex < trucks.Count ? trucks[truck.SelectedIndex].PlateNumber : "(none)";
            if (picked != "B-COLD") problems.Add($"clicking the button selected {picked}, expected B-COLD");
            if (recLbl != null && (!recLbl.Text.Contains("Total 1 crates") || !recLbl.Text.Contains("1 / 120 crates"))) problems.Add($"picked-truck line is '{recLbl.Text}'");
            view.Dispose();
            return string.Join(" | ", problems);
        });
    }

    [Check]
    public static string TruckSuggestion_TonnerOf_ReadsTheSizeFromTheTruckType()
    {
        var problems = new List<string>();
        foreach (var (type, want) in new[] { ("FORWARD 6 TONS", "6-tonner"), ("FORWARD 8 TONS", "8-tonner"), ("4.5T Closed Van", "4.5-tonner"), ("Refrigerated Van", "Refrigerated Van"), ("L300 Van", "L300 Van"),("", "Truck") })
        {
            string got = CapacityValidator.TonnerOf(new Truck { TruckType = type });
            if (got != want) problems.Add($"'{type}' -> '{got}', expected '{want}'");
        }
        return string.Join(" | ", problems);
    }
}
