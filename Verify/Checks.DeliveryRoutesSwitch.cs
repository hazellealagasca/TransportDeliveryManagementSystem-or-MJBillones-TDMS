using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 19: delivery routes are a switch (Settings → Features &amp; Pop-ups), OFF by default. Off: the Master Data "Delivery Routes" tab, the route
/// box and the route columns are hidden and a dispatch order is built from the client's stores; on: everything is back as before.
/// </summary>
public static partial class Checks
{
    private const BindingFlags DrF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A new database in which the delivery routes switch was never touched (the app's default).</summary>
    private static void DrFreshDefault()
    {
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tdms_verify"));
        MJdatabase.UseDatabaseFile(Path.Combine(Path.GetTempPath(), "tdms_verify", $"fresh_{Guid.NewGuid():N}.db"));
        MJdatabase.InitializeDatabase();
        MJdatabase.EnsureSpecialTripColumns();
        MJdatabase.EnsureReceiptSoaColumn();
        Features.Reload();
    }

    [Check]
    public static string DeliveryRoutesSwitch_OffByDefault_AndListedInSettings()
    {
        DrFreshDefault();
        var bad = new List<string>();
        if (Features.On(Features.DeliveryRoutes)) bad.Add("delivery routes are on by default");
        Features.Set(Features.DeliveryRoutes, true, "Delivery routes");
        if (!Features.On(Features.DeliveryRoutes)) bad.Add("the switch cannot be turned on");
        Features.Reload();
        if (!Features.On(Features.DeliveryRoutes)) bad.Add("the switch is not remembered");
        Features.Set(Features.DeliveryRoutes, false, "Delivery routes");
        Features.Reload();
        if (Features.On(Features.DeliveryRoutes)) bad.Add("the switch cannot be turned off again");
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var view = new FeaturesSettingsView();
            var switches = (System.Collections.IDictionary)typeof(FeaturesSettingsView).GetField("switches", DrF)!.GetValue(view)!;
            return switches.Contains(Features.DeliveryRoutes) ? "" : "Settings → Features & Pop-ups has no Delivery routes row";
        });
        if (ui.Length > 0) bad.Add(ui);
        return string.Join("; ", bad);
    }

    [Check]
    public static string DeliveryRoutesSwitch_MasterDataRoutesTab_HiddenOff_BackOn()
    {
        DrFreshDefault();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var bad = new List<string>();
            var hub = new MasterDataHubView();
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1400, 800) };
            hub.Dock = DockStyle.Fill; host.Controls.Add(hub); host.Show(); Pump(600);
            var tabs = (Dictionary<string, Button>)typeof(MasterDataHubView).GetField("tabButtons", DrF)!.GetValue(hub)!;
            if (tabs["Routes"].Visible) bad.Add("the Delivery Routes tab shows while routes are off");
            if (!tabs["Clients"].Visible || !tabs["Archive"].Visible) bad.Add("another tab disappeared");
            hub.SelectTab("Routes"); Pump(200);
            if (Equals(hub.CurrentTabKey, "Routes")) bad.Add("the hidden Routes tab could be opened");
            Features.Set(Features.DeliveryRoutes, true, "Delivery routes"); Pump(200);
            if (!tabs["Routes"].Visible) bad.Add("the Delivery Routes tab did not come back when routes were turned on");
            hub.SelectTab("Routes"); Pump(300);
            if (!Equals(hub.CurrentTabKey, "Routes")) bad.Add("the Routes tab cannot be opened with routes on");
            Features.Set(Features.DeliveryRoutes, false, "Delivery routes"); Pump(200);
            if (tabs["Routes"].Visible) bad.Add("the tab stayed after routes were turned off again");
            if (Equals(hub.CurrentTabKey, "Routes")) bad.Add("the Routes tab stayed open after routes were turned off");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string DeliveryRoutesSwitch_DispatchSavedFromStores_NoRoutes_AndRouteBoxHidden()
    {
        DrFreshDefault();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.EnsureCapacitySchema();
        Exec("DELETE FROM Trucks"); Exec("DELETE FROM Crews");
        MJdatabase.SaveTruck(new Truck { PlateNumber = "DRS 100", TruckType = "FORWARD 6 TONS", CapacityCrates = 200, CargoType = "Cold / Chilled", Status = "Active" });
        int driver = AdSeedCrew("Santos", "Maria", CrewManagementView.RoleDriver);
        int aide = AdSeedCrew("Cruz", "Ana", CrewManagementView.RoleAide);
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ROUTELESS CO", TIN = "006-896-371-000", Address = "1 Rizal St", ContactPerson = "Juan Dela Cruz", ContactNumber = "09171234567", Email = "a@routeless.co", IsActive = true });
        MJdatabase.RegisterCompanyStore("ROUTELESS CO", "RL-1", "Store One", "12 Rizal St");
        MJdatabase.RegisterCompanyStore("ROUTELESS CO", "RL-2", "Store Two", "34 Mabini St");
        if (MJdatabase.GetDeliveryRoutesByClient("ROUTELESS CO", false).Count != 0) return "the test client has routes";
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var DT = typeof(DispatchManagementView);
            var view = (DispatchManagementView)Activator.CreateInstance(DT, nonPublic: true)!;
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show(); Pump(800);
            var bad = new List<string>();
            TC GetF<TC>(string n) where TC : class => (TC)T_Get(n)!;
            object? T_Get(string n) => typeof(DispatchManagementView).GetField(n, DrF)!.GetValue(view);
            bool Wait(Func<bool> cond) { for (int w = 0; w < 60 && !cond(); w++) Pump(100); return cond(); }
            view.OpenNewOrderForm(); Pump(400);
            var cbTruck = GetF<ComboBox>("cbTruck"); var cbDriver = GetF<ComboBox>("cbDriver");
            if (!Wait(() => cbTruck.Items.Count == 1 && cbDriver.Items.Count == 1)) { host.Close(); return $"lists not loaded (trucks {cbTruck.Items.Count}, drivers {cbDriver.Items.Count})"; }
            var cbDest = GetF<ComboBox>("cbDestination"); var lblDest = GetF<Label>("lblDest");
            if (cbDest.Visible && cbDest.Parent!.Visible && lblDest.Visible) bad.Add("the Delivery Route box shows while routes are off");
            var gridDispatch = GetF<DataGridView>("gridDispatch");
            var routeCol = gridDispatch.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "CleanRouteDisplay");
            if (routeCol.Visible) bad.Add("the dispatch list shows the Route column");
            var grid = GetF<DataGridView>("gridRouteStores");
            foreach (var key in new[] { "colStoreRoute" }) if (grid.Columns.Contains(key) && grid.Columns[key]!.Visible) bad.Add(key + " is shown");

            // the person picks the client and adds the client's stores
            GetF<ComboBox>("cbClient").Text = "ROUTELESS CO"; Pump(500);
            foreach (var (code, name, e, g) in new[] { ("RL-1", "Store One", 3, 4), ("RL-2", "Store Two", 2, 1) })
            {
                int r = grid.Rows.Add(); // programmatic values work on the read-only table
                grid.Rows[r].Cells["colStoreCode"].Value = code; grid.Rows[r].Cells["colStoreName"].Value = name;
                grid.Rows[r].Cells["colECrate"].Value = e; grid.Rows[r].Cells["colGCrate"].Value = g;
            }
            Pump(300);
            cbTruck.SelectedIndex = 0; cbDriver.SelectedIndex = 0;
            ((HashSet<int>)T_Get("selectedAideIds")!).Add(aide);
            ((ComboBox)T_Get("cbTempType")!).SelectedIndex = 1;
            GetF<ComboBox>("cbDepartureTime").Text = "09:30 AM";
            GetF<DateTimePicker>("dtpDispatchDate").Value = DateTime.Today.AddDays(2);
            Pump(300);
            int before; lock (boxes.Boxes) before = boxes.Boxes.Count;
            DT.GetMethod("BtnSave_Click", DrF)!.Invoke(view, new object?[] { null, EventArgs.Empty });
            Pump(1500);
            var saved = MJdatabase.GetDeliveries().Where(d => d.ClientName == "ROUTELESS CO").ToList();
            if (saved.Count != 1)
            {
                string said; lock (boxes.Boxes) said = string.Join(" | ", boxes.Boxes.Skip(before));
                bad.Add($"the dispatch was not saved ({saved.Count} saved) [{said}]");
            }
            else
            {
                var d = saved[0];
                if (string.IsNullOrWhiteSpace(d.Destination)) bad.Add("the saved dispatch has no destination");
                if (d.Destination.Contains("Route", StringComparison.OrdinalIgnoreCase) || d.Destination.Contains(" - ")) bad.Add("the destination looks like a route: " + d.Destination);
                var stops = MJdatabase.GetDispatchStops(d.Id);
                if (stops.Count != 2) bad.Add($"{stops.Count} stores saved, expected 2");
                if (d.TotalCrates != 10) bad.Add("total crates " + d.TotalCrates);
                // opening it again with routes off: the stores are ordinary rows and nothing throws
                DT.GetMethod("BindDeliveryToForm", DrF)!.Invoke(view, new object?[] { d }); Pump(300);
                if (grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow) != 2) bad.Add("the saved dispatch did not reopen with its 2 stores");
                var baseline = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && r.Cells["colBaseline"].Value is true).ToList();
                if (baseline.Count > 0) bad.Add("stores are locked as route stores while routes are off");
                // route lookups for a client without routes must not throw
                foreach (var m in new[] { "ReloadRoutesList" }) { try { view.ReloadRoutesList(); Pump(300); } catch (Exception ex) { bad.Add(m + " threw " + ex.GetType().Name); } }
            }
            // back on: the route box and column come back
            Features.Set(Features.DeliveryRoutes, true, "Delivery routes"); Pump(300);
            view.OpenNewOrderForm(); Pump(400);   // the save closed the form
            if (!lblDest.Visible || !cbDest.Parent!.Visible) bad.Add("the route box did not come back when routes were turned on");
            if (!routeCol.Visible) bad.Add("the Route column did not come back when routes were turned on");
            Features.Set(Features.DeliveryRoutes, false, "Delivery routes"); Pump(200);
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
