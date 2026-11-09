using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>New Dispatch Order: the required Temp Type (Dry / Cold / Combi), the zone-aware crate check and the crate footer.</summary>
public static partial class Checks
{
    [Check]
    public static string DispatchOrder_TempType_DecidesTheCrateLimit_AndFooterShowsTheCount()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
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
            var temp = Get<ComboBox>("cbTempType");
            var footer = Get<Label>("lblCrateFooter");
            var cargo = Get<ComboBox>("cbCargoType");
            var g = Get<NumericUpDown>("numGCrate"); var e = Get<NumericUpDown>("numECrate");
            if (temp == null || footer == null || cargo == null || g == null || e == null) return "Temp Type box or footer not created";
            var problems = new List<string>();
            if (!temp.Items.Cast<object>().Select(x => x.ToString()).SequenceEqual(new[] { "Dry", "Cold", "Combi" })) problems.Add("Temp Type items are not Dry, Cold, Combi");
            g.Value = 120; e.Value = 150;
            if (!footer.Text.Contains("G crates 120") || !footer.Text.Contains("E crates 150") || !footer.Text.Contains("270 total")) problems.Add("footer text: " + footer.Text);
            temp.SelectedIndex = 1; // Cold
            if (cargo.Text != MJdatabase.CargoCold) problems.Add($"Cold did not set the crate type ('{cargo.Text}')");
            temp.SelectedIndex = 2;
            if (cargo.Text != MJdatabase.CargoCombi) problems.Add($"Combi did not set the crate type ('{cargo.Text}')");
            cargo.Text = MJdatabase.CargoDry;
            if (temp.SelectedIndex != 0) problems.Add("the Temp Type did not follow the crate type");
            // The limit follows the zone: Cold 120 vs Dry 280 on a 4-ton truck.
            var truck = new Truck { PlateNumber = "T-1", TruckType = "FORWARD 4 TONS", CargoType = MJdatabase.CargoCold, CapacityCrates = 120 };
            var m = CapacityValidator.MatrixFor(truck);
            var cold = CapacityValidator.Validate(truck, MJdatabase.CargoCold, 270, 0);
            var dry = CapacityValidator.Validate(truck, MJdatabase.CargoDry, 0, 270);
            if (m.ColdMax > 0 && m.DryMax > 0 && m.ColdMax < m.DryMax && !(cold.State == CapacityState.BlockPure && dry.State == CapacityState.Allow)) problems.Add($"270 crates: cold {cold.State}, dry {dry.State}");
            view.Dispose();
            return string.Join(" | ", problems);
        });
    }
}
