using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12: the Aide boxes are search boxes (name only, status drawn as a chip), and a Driver / Aide that is not found can be added from the box.</summary>
public static partial class Checks
{
    private static int AdSeedCrew(string last, string first, string role)
    {
        var c = new Crew { LastName = last, FirstName = first, MiddleName = "", FullName = NameHelper.FormatFullName(last, first, ""), Role = role, ContactNumber = "", IsAvailable = true, IsActive = true };
        MJdatabase.SaveCrew(c);
        return c.Id;
    }

    [Check]
    public static string AideDriverSearchAdd_AideBoxSearches_NotFoundRowAdds()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.EnsureCapacitySchema();
        Exec("DELETE FROM Trucks"); Exec("DELETE FROM Crews");
        MJdatabase.SaveTruck(new Truck { PlateNumber = "CAO 1116", TruckType = "FORWARD 4 TONS", CapacityCrates = 120, CargoType = "Cold / Chilled", Status = "Active" });
        AdSeedCrew("Santos", "Maria", CrewManagementView.RoleDriver);
        int cruz = AdSeedCrew("Cruz", "Ana", CrewManagementView.RoleAide);
        int reyes = AdSeedCrew("Reyes", "Ben", CrewManagementView.RoleAide);
        AdSeedCrew("Lopez", "Carl", CrewManagementView.RoleAide);
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
            bool Wait(Func<bool> cond) { for (int w = 0; w < 50 && !cond(); w++) Pump(100); return cond(); }
            var driver = (ComboBox)T.GetField("cbDriver", F)!.GetValue(view)!;
            var truck = (ComboBox)T.GetField("cbTruck", F)!.GetValue(view)!;
            var bad = new List<string>();
            view.InitiateDispatchForSchedule(new LoadingSchedule { ClientName = "CLIENT A", Destination = "Route 1", ScheduleDate = DateTime.Today, GCrate = 1, ECrate = 0, TotalCrates = 1, CargoType = MJdatabase.CargoCold });
            if (!Wait(() => truck.Items.Count == 1 && driver.Items.Count == 1)) { host.Close(); return $"lists not loaded (trucks {truck.Items.Count}, drivers {driver.Items.Count})"; }
            Pump(300);
            ComboBox? S1() => T.GetField("cbAideSlot1", F)!.GetValue(view) as ComboBox;
            ComboBox? S2() => T.GetField("cbAideSlot2", F)!.GetValue(view) as ComboBox;
            if (S1() == null || S2() == null) { host.Close(); return "the aide boxes were not created"; }
            var s1 = S1()!; var s2 = S2()!;
            var selIds = (HashSet<int>)T.GetField("selectedAideIds", F)!.GetValue(view)!;
            string Names(ComboBox c) => string.Join("|", c.Items.Cast<object>().Select(o => o.ToString()));

            // 1. aide box: search box, name only, typing filters, picking works, slot 2 does not list slot 1's aide
            if (s1.DropDownStyle != ComboBoxStyle.DropDownList) bad.Add("aide box is not a DropDownList");
            if (s1.DrawMode != DrawMode.OwnerDrawFixed) bad.Add("aide box is not owner-drawn");
            if (s1.Items.Count != 3) bad.Add($"aide 1 lists {s1.Items.Count} aides, expected 3");
            if (Names(s1).Contains("Available") || Names(s1).Contains("·")) bad.Add("item text still carries the status: " + Names(s1));
            s1.Focus(); Pump(100);
            SpType(s1, "ben"); Pump(100);
            var pop = SpPopup(host);
            if (pop == null || TbMatches(pop).Count != 1 || !(s1.Items[TbMatches(pop)[0]]?.ToString() ?? "").Contains("Reyes")) bad.Add("typing 'ben' did not filter to Reyes");
            if (pop != null) { Pump(100); TbShot(pop, "aide_list_open.png"); }
            if (s1.SelectedIndex != -1) bad.Add("typing changed the pick");
            SpDown(s1, Keys.Enter); Pump(300);
            if (!selIds.SetEquals(new[] { reyes })) bad.Add("picking Ben Reyes gave aides: " + string.Join(",", selIds));
            s1 = S1()!; s2 = S2()!;
            if (!s2.Enabled) bad.Add("Aide 2 stayed disabled after Aide 1 was picked");
            if (Names(s2).Contains("Reyes")) bad.Add("slot 2 lists slot 1's aide: " + Names(s2));
            if (s2.Items.Count != 2) bad.Add($"slot 2 lists {s2.Items.Count}, expected 2");
            if (s1.SelectedItem?.ToString() == null || !s1.SelectedItem.ToString()!.Contains("Reyes")) bad.Add("slot 1 does not show the pick");
            TbShot(s1.Parent!, "aide_slots.png");

            // 2a. aide not found: row text, Enter calls the add action with the trimmed text
            s2.Focus(); Pump(100);
            SpType(s2, "Zed Nobody "); Pump(100);
            pop = SpPopup(host);
            if (pop == null || pop.NotFoundMessage != "Aide \"Zed Nobody\" cannot be found." || pop.AddButtonText != "+ Add aide") bad.Add($"aide no-match row: '{pop?.NotFoundMessage}' / '{pop?.AddButtonText}'");
            if (pop != null) { Pump(100); TbShot(pop, "aide_notfound.png"); }
            SpDown(s2, Keys.Escape); Pump(50);

            // 2b. driver not found: row text, screenshot
            driver.Focus(); Pump(100);
            SpType(driver, "Juan Santos"); Pump(100);
            pop = SpPopup(host);
            if (pop == null || !pop.NotFoundMessage.Contains("cannot be found") || !pop.AddButtonText.Contains("Add driver") || pop.NotFoundMessage != "Driver \"Juan Santos\" cannot be found.") bad.Add($"driver no-match row: '{pop?.NotFoundMessage}' / '{pop?.AddButtonText}'");
            if (pop != null) { Pump(100); TbShot(pop, "driver_notfound.png"); }
            SpDown(driver, Keys.Escape); Pump(50);
            if (driver.SelectedIndex != -1) bad.Add("closing the not-found row changed the driver");

            // Enter on the row calls the callback with the trimmed text (a plain test box, so no window opens)
            var probe = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Parent = host, Left = 10, Top = 10, Width = 200 };
            probe.Items.Add("Maria Santos");
            string? got = null;
            SearchPicker.Attach(probe, i => probe.Items[i]?.ToString() ?? "", noun: "driver", onNotFound: t => got = t);
            probe.Focus(); Pump(100);
            SpType(probe, " Pedro "); Pump(100);
            if (SpPopup(host) == null) bad.Add("no list for the probe box");
            SpDown(probe, Keys.Enter); Pump(100);
            if (got != "Pedro") bad.Add($"Enter on the not-found row gave '{got}', expected 'Pedro'");
            if (SpPopup(host) != null) bad.Add("the list stayed open after the add action");
            // without the option the plain line stays
            var plain = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Parent = host, Left = 10, Top = 50, Width = 200 };
            plain.Items.Add("Maria Santos");
            var pp = SearchPicker.Attach(plain, i => plain.Items[i]?.ToString() ?? "", noun: "truck");
            plain.Focus(); Pump(100); SpType(plain, "zz"); Pump(100);
            if (pp.NotFoundMessage.Length != 0) bad.Add("a box without the option shows the add row");
            SpDown(plain, Keys.Escape); Pump(50);

            // 3. Dialog: SplitTyped, duplicate refused, driver saved and picked
            if (QuickAddCrewDialog.SplitTyped("Juan Santos") != ("Santos", "Juan")) bad.Add("SplitTyped 'Juan Santos'");
            if (QuickAddCrewDialog.SplitTyped("SANTOS, JUAN") != ("SANTOS", "JUAN")) bad.Add("SplitTyped comma");
            if (QuickAddCrewDialog.SplitTyped("Santos") != ("Santos", "")) bad.Add("SplitTyped one word");
            var dup = QuickAddCrewDialog.TrySave(true, "Santos", "Maria", "", "");
            if (dup.Id != 0 || string.IsNullOrEmpty(dup.Problem)) bad.Add("duplicate name was not refused");
            var badName = QuickAddCrewDialog.TrySave(true, "Sa1ntos", "Juan", "", "");
            if (badName.Id != 0 || string.IsNullOrEmpty(badName.Problem)) bad.Add("a name with a digit was not refused");
            var badPhone = QuickAddCrewDialog.TrySave(true, "Santos", "Juan", "", "12");
            if (badPhone.Id != 0 || badPhone.Field != "contact") bad.Add("a bad phone was not refused");

            var saved = QuickAddCrewDialog.TrySave(true, "Santos", "Juan", "", "09171234567");
            if (saved.Id <= 0 || saved.Problem != null) bad.Add("new driver not saved: " + saved.Problem);
            else
            {
                if (Long("SELECT COUNT(*) FROM Crews WHERE Id=@i AND Role='Driver' AND IsActive=1 AND IsAvailable=1", ("@i", saved.Id)) != 1) bad.Add("saved driver row is not an active, available Driver");
                T.GetMethod("AfterCrewAdded", F)!.Invoke(view, new object?[] { saved.Id, true, null });
                Pump(200);
                if (driver.SelectedIndex < 0 || !driver.Items[driver.SelectedIndex]!.ToString()!.Contains("Juan")) bad.Add("new driver not selected in the Driver box: " + driver.Text);
                if (truck.Items.Count != 1) bad.Add("the truck list changed");
            }
            var savedAide = QuickAddCrewDialog.TrySave(false, "Garcia", "Dan", "", "");
            if (savedAide.Id <= 0 || savedAide.Problem != null) bad.Add("new aide not saved: " + savedAide.Problem);
            else
            {
                T.GetMethod("AfterCrewAdded", F)!.Invoke(view, new object?[] { savedAide.Id, false, S2() });
                Pump(300);
                if (!selIds.Contains(savedAide.Id) || !selIds.Contains(reyes) || selIds.Count != 2) bad.Add("aides after add: " + string.Join(",", selIds));
                if (S2()!.SelectedItem == null || !S2()!.SelectedItem!.ToString()!.Contains("Garcia")) bad.Add("new aide not shown in slot 2");
                if (driver.SelectedIndex < 0) bad.Add("the driver pick was lost when an aide was added");
            }
            _ = cruz;
            try { host.Close(); host.Dispose(); Pump(100); } catch (Exception ex) { bad.Add("closing threw: " + ex.Message); }
            return string.Join("; ", bad);
        });
    }
}
