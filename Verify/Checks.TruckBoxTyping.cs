using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12: typing works while the normal Truck list is open, the typed text is shown, the suggested truck leads the search list and has a line under the Truck box.</summary>
public static partial class Checks
{
    private static List<int> TbMatches(SearchPicker p) => new List<int>((List<int>)typeof(SearchPicker).GetField("matches", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(p)!);

    private static void TbShot(Control c, string name)
    {
        try
        {
            if (Environment.GetEnvironmentVariable("TDMS_SHOT_DIR") is not string dir || c.Width <= 0 || c.Height <= 0) return;
            Directory.CreateDirectory(dir);
            using var bmp = new Bitmap(c.Width, c.Height);
            c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
            bmp.Save(Path.Combine(dir, name));
        }
        catch { }
    }

    [Check]
    public static string TruckBox_TypeWhileListOpen_ShowsQuery_SuggestionLeadsAndHasLineUnderBox()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.EnsureCapacitySchema();
        Exec("DELETE FROM Trucks");
        MJdatabase.SaveTruck(new Truck { PlateNumber = "AAA 900", TruckType = "FORWARD 8 TONS", CapacityCrates = 280, CargoType = "Cold / Chilled", Status = "Active" });
        MJdatabase.SaveTruck(new Truck { PlateNumber = "WNX 383", TruckType = "FORWARD 6 TONS", CapacityCrates = 200, CargoType = "Cold / Chilled", Status = "Active" });
        MJdatabase.SaveTruck(new Truck { PlateNumber = "CAO 1116", TruckType = "FORWARD 4 TONS", CapacityCrates = 120, CargoType = "Cold / Chilled", Status = "Active" });
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var T = typeof(DispatchManagementView);
            T? Get<T>(string n) where T : class => typeof(DispatchManagementView).GetField(n, F)!.GetValue(view) as T;
            bool Wait(Func<bool> cond) { for (int w = 0; w < 50 && !cond(); w++) Pump(100); return cond(); }
            var truck = Get<ComboBox>("cbTruck")!; var driver = Get<ComboBox>("cbDriver")!;
            var line = Get<TableLayoutPanel>("pnlTruckSuggest"); var lineLbl = Get<Label>("lblTruckSuggest"); var lineBtn = Get<Button>("btnTruckSuggest");
            if (line == null || lineLbl == null || lineBtn == null) { host.Close(); return "the suggestion line under the Truck box was not created"; }
            var bad = new List<string>();
            view.InitiateDispatchForSchedule(new LoadingSchedule { ClientName = "CLIENT A", Destination = "Route 1", ScheduleDate = DateTime.Today, GCrate = 1, ECrate = 0, TotalCrates = 1, CargoType = MJdatabase.CargoCold });
            if (!Wait(() => line.Visible && truck.Items.Count == 3)) { host.Close(); return $"no suggestion line / truck list in time (line {line.Visible}, items {truck.Items.Count})"; }
            Pump(200);
            var trucks = (List<Truck>)T.GetField("trucks", F)!.GetValue(view)!;
            int Idx(string plate) => trucks.FindIndex(t => t.PlateNumber == plate);
            int cao = Idx("CAO 1116"), wnx = Idx("WNX 383");
            if (cao < 0 || wnx < 0) { host.Close(); return "seeded trucks missing from the list"; }
            if (truck.SelectedIndex != -1) bad.Add("a truck was auto-selected");

            // (c) the line under the Truck box: tonner + plate, in the left column, clear of the Driver box
            if (!lineLbl.Text.Contains("4-tonner") || !lineLbl.Text.Contains("CAO 1116") || !lineLbl.Text.Contains("fits 1 of 120")) bad.Add($"line text '{lineLbl.Text}'");
            var tr = truck.RectangleToScreen(truck.ClientRectangle); var dr = driver.RectangleToScreen(driver.ClientRectangle); var lr = line.RectangleToScreen(line.ClientRectangle);
            if (lr.Top < tr.Bottom) bad.Add("the line overlaps the Truck box");
            if (lr.IntersectsWith(dr)) bad.Add("the line overlaps the Driver box");
            if (lr.Left < tr.Left - 2 || lr.Right > dr.Left) bad.Add($"the line is not confined to the Truck column (line {lr}, truck {tr}, driver {dr})");
            if (lineBtn.Right > line.ClientSize.Width || lineBtn.Left < lineLbl.Right - 1) bad.Add("the button is clipped or covers the text");
            if (TextRenderer.MeasureText(lineLbl.Text, lineLbl.Font, new Size(lineLbl.Width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height > lineLbl.Height) bad.Add("the line text is cut off");
            TbShot(line.Parent!.Parent ?? line.Parent, "truck_section.png");

            // (a) the normal list is open (as after a click); typing c, a, o closes it and opens the search list with only matching trucks
            truck.Focus(); Pump(100);
            truck.DroppedDown = true; Pump(150);
            if (!truck.DroppedDown) bad.Add("could not open the normal list for the check");
            if (!SpKeyHandled(truck, 'c')) bad.Add("a letter typed with the list open is not taken over");
            SpType(truck, "ao"); Pump(100);
            if (truck.DroppedDown) bad.Add("the normal list is still open after typing");
            var pop = SpPopup(host);
            if (pop == null) bad.Add("no search list after typing with the list open");
            else if (!TbMatches(pop).SequenceEqual(new[] { cao })) bad.Add("'cao' matches: " + string.Join(",", TbMatches(pop)) + $" (expected {cao})");
            if (SearchPicker.QueryFor(truck) != "cao") bad.Add($"query is '{SearchPicker.QueryFor(truck)}'");
            if (truck.SelectedIndex != -1) bad.Add("typing changed the pick");
            SpDown(truck, Keys.Enter); Pump(100);
            if (truck.SelectedIndex != cao) bad.Add($"Enter picked {truck.SelectedIndex}, expected {cao}");
            if (SpPopup(host) != null || SearchPicker.QueryFor(truck).Length != 0) bad.Add("the list or the query stayed after Enter");
            if (!Wait(() => !line.Visible)) bad.Add("the line under the Truck box stayed after a truck was picked");

            // (b) "4 ton" and "cao1116" (no space) find CAO 1116 only
            foreach (var (q, want) in new[] { ("4 ton", new[] { cao }), ("cao1116", new[] { cao }), ("4-tonner", new[] { cao }), ("6 ton", new[] { wnx }) })
            {
                SpType(truck, q); Pump(50);
                var p = SpPopup(host);
                if (p == null || !TbMatches(p).SequenceEqual(want)) bad.Add($"'{q}' matches: " + (p == null ? "(no list)" : string.Join(",", TbMatches(p))) + " expected " + string.Join(",", want));
                SpDown(truck, Keys.Escape); Pump(50);
            }
            if (truck.SelectedIndex != cao) bad.Add("Esc changed the pick");

            // (c) with a suggestion again: the suggested truck is the first row of the search list, chip text, Use this truck selects it
            truck.SelectedIndex = -1;
            if (!Wait(() => line.Visible)) { host.Close(); return string.Join("; ", bad) + "; the line did not come back when the pick was cleared"; }
            Pump(150);
            int other = Enumerable.Range(0, 3).First(i => i != cao); // the first truck in the box's own order that is not the suggestion
            truck.Focus(); truck.DroppedDown = true; Pump(150);
            SpType(truck, "ton"); Pump(100);
            pop = SpPopup(host);
            var all = pop == null ? new List<int>() : TbMatches(pop);
            if (pop == null || all.Count != 3 || all[0] != cao) bad.Add("with 'ton' the suggested truck is not the first row: " + string.Join(",", all));
            else if (!all.Skip(1).SequenceEqual(Enumerable.Range(0, 3).Where(i => i != cao))) bad.Add("the other rows changed order: " + string.Join(",", all));
            if (pop != null) { Pump(100); TbShot(pop, "picker_query.png"); }
            SpDown(truck, Keys.Down); SpDown(truck, Keys.Enter); Pump(100); // second row = the next truck in the box's order, not the suggestion
            if (truck.SelectedIndex != other) bad.Add($"picking a different truck gave index {truck.SelectedIndex}, expected {other}");
            if (!Wait(() => !line.Visible)) bad.Add("the line stayed after picking a different truck");

            truck.SelectedIndex = -1;
            if (!Wait(() => line.Visible)) bad.Add("the line did not return");
            Pump(150);
            lineBtn.PerformClick(); Pump(150);
            if (truck.SelectedIndex != cao) bad.Add($"the line's button selected {truck.SelectedIndex}, expected {cao}");

            // an empty result shows the no-match row and the box shows what was typed
            SpType(truck, "zzq"); Pump(100);
            pop = SpPopup(host);
            if (pop == null || TbMatches(pop).Count != 0) bad.Add("a non-matching query should show an empty list");
            if (pop != null) TbShot(pop, "picker_nomatch.png");
            SpDown(truck, Keys.Escape); Pump(50);

            // Driver: typing while its normal list is open works through the same picker and shows the query
            T.GetField("drivers", F)!.SetValue(view, new List<Crew> { new() { Id = 1, FullName = "Maria Santos" }, new() { Id = 2, FullName = "Juan Dela Cruz" } });
            driver.Items.Clear(); foreach (var s in SpDriverItems.Take(2)) driver.Items.Add(s);
            driver.SelectedIndex = -1;
            driver.Focus(); Pump(100); driver.DroppedDown = true; Pump(150);
            SpType(driver, "jua"); Pump(100);
            pop = SpPopup(host);
            if (driver.DroppedDown || pop == null || !TbMatches(pop).SequenceEqual(new[] { 1 }) || SearchPicker.QueryFor(driver) != "jua") bad.Add("driver: typing with the list open did not open the search list");
            SpDown(driver, Keys.Enter); Pump(100);
            if (driver.SelectedIndex != 1) bad.Add($"driver pick {driver.SelectedIndex}");
            try { host.Close(); host.Dispose(); Pump(100); } catch (Exception ex) { bad.Add("closing threw: " + ex.Message); }
            return string.Join("; ", bad);
        });
    }
}
