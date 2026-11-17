using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 17: Resources → Incident log lists every trip incident with its trip's client, truck and crew; voided lines are left out.</summary>
    [Check]
    public static string IncidentLog_ListsTripIncidents_WithTripDetails_NoVoided()
    {
        Fresh();
        int d = Seed.Delivery(client: "CLIENT A");
        var kept = new IncidentReport { DeliveryId = d, DispatchNumber = "", IncidentDate = DateTime.Today, Cause = "Broken crates", DamagedQuantity = 3, EstimatedCost = 450m,
            ChargeableToCrew = true, ChargedCrewIds = "1,2", ChargedCrewNames = "Juan Dela Cruz; Pedro Santos", Description = "Dropped at store 1", Status = "Pending" };
        MJdatabase.SaveIncidentReport(kept, notify: false);
        var gone = new IncidentReport { DeliveryId = d, IncidentDate = DateTime.Today, Cause = "Wet crates", DamagedQuantity = 1, EstimatedCost = 0m, Status = "Pending" };
        MJdatabase.SaveIncidentReport(gone, notify: false);
        MJdatabase.VoidTripIncident(gone.Id);

        var log = MJdatabase.GetIncidentLog();
        if (log.Count != 1) return $"the log holds {log.Count} lines, expected 1 (the voided one left out)";
        var x = log[0];
        var bad = new System.Collections.Generic.List<string>();
        if (x.ClientName != "CLIENT A" || x.TruckInfo != "ABC-123" || x.DriverName != "Juan Dela Cruz" || x.AideNames != "Pedro Santos") bad.Add($"trip details: {x.ClientName} / {x.TruckInfo} / {x.DriverName} / {x.AideNames}");
        if (string.IsNullOrEmpty(x.DispatchNumber)) bad.Add("the dispatch number is not filled from the trip");
        if (x.ChargedTo != "Juan Dela Cruz; Pedro Santos (PHP 225.00 each)") bad.Add("charged to: " + x.ChargedTo);
        if (x.DamagedQuantity != 3 || x.EstimatedCost != 450m || x.Notes != "Dropped at store 1") bad.Add("figures or notes lost");

        // The screen shows it and filters it.
        string ui = RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1400, 800) };
            var view = new IncidentLogView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(400);
            view.RefreshData(); Pump(200);
            var grid = FieldOf<DataGridView>(view, "grid")!;
            if (grid.Rows.Count != 1) return "the screen shows " + grid.Rows.Count + " lines";
            FieldOf<TextBox>(view, "txtSearch")!.Text = "nothing like this"; Pump(100);
            if (grid.Rows.Count != 0) return "the search did not filter";
            FieldOf<TextBox>(view, "txtSearch")!.Text = "pedro"; Pump(100);
            if (grid.Rows.Count != 1) return "searching a charged crew name found " + grid.Rows.Count;
            host.Close();
            return "";
        });
        if (ui.Length > 0) bad.Add(ui);
        return string.Join("; ", bad);
    }
}
