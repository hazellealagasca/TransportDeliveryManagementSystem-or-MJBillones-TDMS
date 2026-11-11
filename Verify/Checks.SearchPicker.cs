using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Fix 2: the Truck and Driver boxes search as you type (contains, any part of the text); a pick maps to the original index; the boxes keep their items, order and style.</summary>
public static partial class Checks
{
    private static readonly string[] SpTruckItems = { "[ACTIVE] ABC-1234 [6T]", "[ON TRIP] XYZ-9876 [4T]", "[ACTIVE] NBD 4412 [10W]" };
    private static readonly string[] SpDriverItems = { "[ACTIVE] Maria Santos (3 Trips)", "[ACTIVE] Juan Dela Cruz (12 Trips)", "[ON TRIP] Pedro Cruz (1 Trips)" };

    private static void SpKey(Control c, char ch) { typeof(Control).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(c, new object[] { new KeyPressEventArgs(ch) }); }
    private static bool SpKeyHandled(Control c, char ch) { var a = new KeyPressEventArgs(ch); typeof(Control).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(c, new object[] { a }); return a.Handled; }
    private static void SpDown(Control c, Keys k) { typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(c, new object[] { new KeyEventArgs(k) }); }
    private static SearchPicker? SpPopup(Form owner) => owner.OwnedForms.OfType<SearchPicker>().FirstOrDefault(f => f.Visible);
    private static string SpType(Control c, string s) { foreach (char ch in s) SpKey(c, ch); return ""; }

    [Check]
    public static string SearchPicker_Match_ContainsAnyPart_KeepsOrder_OriginalIndexes()
    {
        var bad = new List<string>();
        Func<int, string> trucks = i => i switch { 0 => "ABC-1234 6T", 1 => "XYZ-9876 4T", _ => "NBD 4412 10W" };
        Func<int, string> drivers = i => i switch { 0 => "Maria Santos", 1 => "Juan Dela Cruz", _ => "Pedro Cruz" };
        if (!SearchPicker.Match(3, trucks, "123").SequenceEqual(new[] { 0 })) bad.Add("123 -> " + string.Join(",", SearchPicker.Match(3, trucks, "123")));
        if (!SearchPicker.Match(3, drivers, "cruz").SequenceEqual(new[] { 1, 2 })) bad.Add("cruz -> " + string.Join(",", SearchPicker.Match(3, drivers, "cruz")));
        if (!SearchPicker.Match(3, drivers, "ELA CR").SequenceEqual(new[] { 1 })) bad.Add("middle of a word, any case");
        if (SearchPicker.Match(3, trucks, "   ").Count != 0) bad.Add("blank text matched");
        if (SearchPicker.Match(3, trucks, "nothing-like-this").Count != 0) bad.Add("a non-match matched");
        return string.Join("; ", bad);
    }

    [Check]
    public static string SearchPicker_DispatchBoxes_PickIsOriginalIndex_EscKeeps_ItemsUntouched_CloseWhileOpenIsQuiet()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
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
            var cbT = (ComboBox)T.GetField("cbTruck", F)!.GetValue(view)!; var cbD = (ComboBox)T.GetField("cbDriver", F)!.GetValue(view)!;
            // Fill the boxes the way the screen does (a list + one item per entry, same order), then search.
            T.GetField("trucks", F)!.SetValue(view, new List<Truck> { new() { Id = 1, PlateNumber = "ABC-1234", TruckType = "6T" }, new() { Id = 2, PlateNumber = "XYZ-9876", TruckType = "4T" }, new() { Id = 3, PlateNumber = "NBD 4412", TruckType = "10W" } });
            T.GetField("drivers", F)!.SetValue(view, new List<Crew> { new() { Id = 1, FullName = "Maria Santos" }, new() { Id = 2, FullName = "Juan Dela Cruz" }, new() { Id = 3, FullName = "Pedro Cruz" } });
            cbT.Items.Clear(); foreach (var s in SpTruckItems) cbT.Items.Add(s);
            cbD.Items.Clear(); foreach (var s in SpDriverItems) cbD.Items.Add(s);
            var bad = new List<string>();
            if (cbT.DropDownStyle != ComboBoxStyle.DropDownList || cbD.DropDownStyle != ComboBoxStyle.DropDownList) bad.Add("a box is no longer DropDownList");
            if (cbT.AutoCompleteMode != AutoCompleteMode.None || cbD.AutoCompleteMode != AutoCompleteMode.None) bad.Add("Windows AutoComplete is on");
            int changed = 0; cbT.SelectedIndexChanged += (s, e) => changed++;
            cbT.Focus(); Pump(100);
            // Typing "123" lists ABC-1234 and picking it selects item 0; the key does not jump the box on its own.
            if (!SpKeyHandled(cbT, '1')) bad.Add("a typed key is not swallowed (the box would jump to an item)");
            SpType(cbT, "23"); Pump(50);
            if (SpPopup(host) == null) bad.Add("no suggestion list after typing");
            if (cbT.SelectedIndex != -1) bad.Add("typing changed the pick");
            SpDown(cbT, Keys.Enter); Pump(50);
            if (cbT.SelectedIndex != 0 || changed != 1) bad.Add($"picked index {cbT.SelectedIndex}, SelectedIndexChanged {changed}x");
            // A later item: "nbd" picks index 2 (the original position), not 0.
            SpType(cbT, "nbd"); SpDown(cbT, Keys.Enter); Pump(50);
            if (cbT.SelectedIndex != 2) bad.Add($"nbd picked {cbT.SelectedIndex}");
            // Esc keeps the previous pick.
            SpType(cbT, "xyz"); SpDown(cbT, Keys.Escape); Pump(50);
            if (cbT.SelectedIndex != 2 || SpPopup(host) != null) bad.Add("Esc changed the pick or left the list open");
            // Leaving the box closes the list without changing the pick.
            SpType(cbT, "xyz"); typeof(Control).GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cbT, new object[] { EventArgs.Empty }); Pump(100); // the box lost focus
            if (cbT.SelectedIndex != 2 || SpPopup(host) != null) bad.Add("leaving the box changed the pick or left the list open");
            // Driver: "cruz" lists both; Down then Enter picks the second (original index 2).
            cbD.Focus(); Pump(50); SpType(cbD, "cruz"); SpDown(cbD, Keys.Down); SpDown(cbD, Keys.Enter); Pump(50);
            if (cbD.SelectedIndex != 2) bad.Add($"driver pick {cbD.SelectedIndex}");
            // Items and their order are untouched.
            if (!cbT.Items.Cast<object>().Select(x => x.ToString()).SequenceEqual(SpTruckItems) || !cbD.Items.Cast<object>().Select(x => x.ToString()).SequenceEqual(SpDriverItems)) bad.Add("items or their order changed");
            // Closing the window while the list is open raises no error.
            cbD.Focus(); SpType(cbD, "ju"); Pump(50);
            try { host.Close(); host.Dispose(); Pump(100); } catch (Exception ex) { bad.Add("closing with the list open threw: " + ex.Message); }
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SearchPicker_RescheduleBoxes_SameBehaviour()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int d = Seed.Delivery();
        var del = MJdatabase.GetDeliveryById(d)!;
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var dlg = new RescheduleDeliveryDialog(del) { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) };
            dlg.Show(); Pump(500);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var T = typeof(RescheduleDeliveryDialog);
            var cbT = (ComboBox)T.GetField("cbTruck", F)!.GetValue(dlg)!; var cbD = (ComboBox)T.GetField("cbDriver", F)!.GetValue(dlg)!;
            T.GetField("availableTrucks", F)!.SetValue(dlg, new List<Truck> { new() { Id = 1, PlateNumber = "ABC-1234", TruckType = "6T" }, new() { Id = 2, PlateNumber = "NBD 4412", TruckType = "10W" } });
            T.GetField("availableDrivers", F)!.SetValue(dlg, new List<Crew> { new() { Id = 1, FullName = "Maria Santos" }, new() { Id = 2, FullName = "Juan Dela Cruz" } });
            cbT.Items.Clear(); cbT.Items.Add("Keep current"); cbT.Items.Add("ABC-1234 [6T]"); cbT.Items.Add("NBD 4412 [10W]"); cbT.SelectedIndex = 0;
            cbD.Items.Clear(); cbD.Items.Add("Keep current"); cbD.Items.Add("Maria Santos"); cbD.Items.Add("Juan Dela Cruz"); cbD.SelectedIndex = 0;
            var bad = new List<string>();
            if (cbT.DropDownStyle != ComboBoxStyle.DropDownList || cbD.DropDownStyle != ComboBoxStyle.DropDownList) bad.Add("a box is no longer DropDownList");
            cbT.Focus(); Pump(100);
            SpType(cbT, "123"); SpDown(cbT, Keys.Enter); Pump(50);
            if (cbT.SelectedIndex != 1) bad.Add($"truck pick {cbT.SelectedIndex}");
            cbD.Focus(); Pump(50); SpType(cbD, "cruz"); SpDown(cbD, Keys.Enter); Pump(50);
            if (cbD.SelectedIndex != 2) bad.Add($"driver pick {cbD.SelectedIndex}");
            SpType(cbD, "mari"); SpDown(cbD, Keys.Escape); Pump(50);
            if (cbD.SelectedIndex != 2) bad.Add("Esc changed the driver pick");
            SpType(cbD, "mari");
            try { dlg.Close(); dlg.Dispose(); Pump(100); } catch (Exception ex) { bad.Add("closing with the list open threw: " + ex.Message); }
            return string.Join("; ", bad);
        });
    }
}
