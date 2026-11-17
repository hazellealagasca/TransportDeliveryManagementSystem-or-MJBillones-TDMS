using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>
    /// Nov 18: incidents saved in step 3 of the trip report are read back everywhere (crew and truck profiles, dossier, counts, search):
    /// GetIncidentReports follows "Incident report in trip reports" instead of the hidden Incident & Damage Logs screen.
    /// </summary>
    [Check]
    public static string IncidentReaders_SeeTripIncidents_AndFollowTheTripSwitch()
    {
        Fresh();
        int d = Seed.Delivery(client: "CLIENT A");
        MJdatabase.SaveIncidentReport(new IncidentReport { DeliveryId = d, IncidentDate = DateTime.Today, Cause = "Broken crates", DamagedQuantity = 4, EstimatedCost = 200m, Status = "Pending" }, notify: false);
        var bad = new List<string>();
        if (Features.On(Features.Incidents)) bad.Add("the old Incident & Damage Logs screen is switched on");
        var all = MJdatabase.GetIncidentReports();
        if (all.Count != 1 || all[0].DeliveryId != d || all[0].DamagedQuantity != 4) bad.Add($"GetIncidentReports returned {all.Count} incident(s) for a saved trip incident");
        try
        {
            Features.Set(Features.TripIncidents, false, "Incident report in trip reports");
            Features.Reload();
            if (MJdatabase.GetIncidentReports().Count != 0) bad.Add("incidents are still read while 'Incident report in trip reports' is off");
        }
        finally { Features.Set(Features.TripIncidents, true, "Incident report in trip reports"); Features.Reload(); }

        // The trip report's damaged-crates count follows step 3 before saving.
        string ui = RunOnSta(() =>
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(800);
            var t = typeof(TripReportView);
            t.GetMethod("LoadIncidentStep", F)!.Invoke(view, new object?[] { MJdatabase.GetDeliveryById(d) }); Pump(200);
            var lines = (System.Collections.IList)t.GetField("tripIncidentLines", F)!.GetValue(view)!;
            if (lines.Count != 1) return "step 3 did not load the saved incident (" + lines.Count + ")";
            lines.Add(new IncidentReport { DeliveryId = d, Cause = "Missing crates", DamagedQuantity = 3, IncidentDate = DateTime.Today, Status = "Pending" });
            t.GetMethod("RefreshIncidentGrid", F)!.Invoke(view, null);
            int damaged = (int)t.GetField("tripIncidentDamaged", F)!.GetValue(view)!;
            host.Close();
            return damaged == 7 ? "" : $"the damaged-crates count is {damaged}, expected 7 (4 saved + 3 just added in step 3)";
        });
        if (ui.Length > 0) bad.Add(ui);
        return string.Join("; ", bad);
    }
}
