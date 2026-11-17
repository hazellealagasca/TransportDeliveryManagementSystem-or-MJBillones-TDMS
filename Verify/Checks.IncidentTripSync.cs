using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>
    /// Nov 18: step 3 of the trip report follows the dispatch's truck, driver and aides as assigned NOW: the info line and the
    /// "Charge to" list are read fresh from the database, even when the screen's dispatch list is older; a saved incident that charges
    /// someone no longer on the trip is flagged.
    /// </summary>
    [Check]
    public static string IncidentStep_FollowsTheDispatchsCurrentTruckAndCrew()
    {
        Fresh();
        int d = Seed.Delivery(client: "CLIENT A");   // driver 1 Juan Dela Cruz, aide 2 Pedro Santos, truck ABC-123
        var stale = MJdatabase.GetDeliveryById(d)!;   // what the screen's list holds
        // The dispatch is changed afterwards: other driver, two aides, other truck.
        Exec("UPDATE Deliveries SET DriverId = 7, DriverName = 'Reyes, Mario Santos', AideId = 8, AideName = 'Cruz, Ana Lopez', AideIds = '8,9', AideNames = 'Cruz, Ana Lopez, Diaz, Ben Ramos', TruckInfo = 'NEW-777 (8T)' WHERE Id = @d", ("@d", d));
        var old = new IncidentReport { DeliveryId = d, IncidentDate = DateTime.Today, Cause = "Broken crates", DamagedQuantity = 2, EstimatedCost = 300m,
            ChargeableToCrew = true, ChargedCrewIds = "1", ChargedCrewNames = "Juan Dela Cruz", Status = "Pending" };
        MJdatabase.SaveIncidentReport(old, notify: false);

        return RunOnSta(() =>
        {
            var bad = new List<string>();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(800);
            var t = typeof(TripReportView);
            t.GetMethod("LoadIncidentStep", F)!.Invoke(view, new object?[] { stale }); Pump(200);

            var line = FieldOf<Label>(view, "lblIncTrip")?.Text ?? "";
            if (!line.Contains("NEW-777") || !line.Contains("Reyes, Mario Santos") || !line.Contains("Cruz, Ana Lopez") || !line.Contains("Diaz, Ben Ramos"))
                bad.Add("the step 3 line does not show the current truck and crew: " + line);
            if (line.Contains("ABC-123") || line.Contains("Pedro Santos")) bad.Add("the step 3 line still shows the old truck or crew: " + line);

            FieldOf<CheckBox>(view, "chkIncCrew")!.Checked = true; Pump(100);
            var boxes = (List<CheckBox>)t.GetField("incCrewBoxes", F)!.GetValue(view)!;
            var names = boxes.Select(b => b.Text).ToList();
            if (names.Count != 3 || !names[0].StartsWith("Reyes, Mario Santos") || !names.Any(n => n.StartsWith("Cruz, Ana Lopez")) || !names.Any(n => n.StartsWith("Diaz, Ben Ramos")))
                bad.Add("Charge to lists " + string.Join(" | ", names) + " (expected the current driver and two aides)");

            var grid = FieldOf<DataGridView>(view, "gridInc")!;
            string charged = grid.Rows.Count > 0 ? grid.Rows[0].Cells["colIncCrew"].Value?.ToString() ?? "" : "";
            if (!charged.Contains("no longer on this trip")) bad.Add("a saved incident charging someone no longer on the trip is not flagged: " + charged);
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
