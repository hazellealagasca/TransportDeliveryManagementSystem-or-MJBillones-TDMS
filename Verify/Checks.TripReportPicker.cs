using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// The Trip report "Dispatch order" drop-down lists only the trips that still wait for a report, as short lines, oldest first.
/// A trip whose report is finished is not listed (it cannot get a second one), but opening its report still shows it.
/// </summary>
public static partial class Checks
{
    private static string NumberOf(int deliveryId) => MJdatabase.GetDeliveries().First(d => d.Id == deliveryId).DispatchNumber;

    private static void SetDate(string table, int id, int daysAgo)
        => Exec($"UPDATE {table} SET DispatchDate = @d WHERE Id = @id", ("@d", DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd")), ("@id", id));

    private static void AddReport(string dispatchNumber, int deliveryId, string status)
        => Exec(@"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, DeliveredCrates, ReturnedCrates, ReportDate, SpecialTripId, IsVoid, Status)
                  VALUES (@no, @d, @dn, 0, 0, @rd, 0, 0, @st)",
            ("@no", "TR-" + dispatchNumber), ("@d", deliveryId), ("@dn", dispatchNumber), ("@rd", DateTime.Today.ToString("yyyy-MM-dd")), ("@st", status));

    [Check]
    public static string TripReportDropdown_ListsOnlyTripsWaitingForAReport_ShortLines_OldestFirst()
    {
        Fresh();
        int waiting = Seed.Delivery(), done = Seed.Delivery(), draft = Seed.Delivery(), planned = Seed.Delivery(status: "Scheduled");
        int spWaiting = Seed.SpecialTrip(), spPlanned = Seed.SpecialTrip(status: "Scheduled");
        SetDate("Deliveries", waiting, 3); SetDate("Deliveries", done, 6); SetDate("Deliveries", draft, 1); SetDate("Deliveries", planned, 4);
        SetDate("SpecialTrips", spWaiting, 2); SetDate("SpecialTrips", spPlanned, 5);
        Exec("UPDATE SpecialTrips SET ModeOfPaymentWithReport = 1");
        string nWaiting = NumberOf(waiting), nDone = NumberOf(done), nDraft = NumberOf(draft), nPlanned = NumberOf(planned);
        AddReport(nDone, done, "Completed");
        AddReport(nDraft, draft, "Draft");
        string sWaiting = MJdatabase.GetSpecialTrips().First(t => t.Id == spWaiting).OrderReference;
        string sPlanned = MJdatabase.GetSpecialTrips().First(t => t.Id == spPlanned).OrderReference;

        return RunOnSta(() =>
        {
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(2000);
            var cb = FieldOf<ComboBox>(view, "cbDelivery");
            if (cb == null) return "the Dispatch order drop-down was not found";
            typeof(TripReportView).GetMethod("OpenNewTripReport", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);   // like clicking "+ New trip report"
            Pump(500);
            var items = cb.Items.Cast<object>().Select(o => o.ToString() ?? "").ToList();
            var problems = new List<string>();
            void Expect(string what, bool ok) { if (!ok) problems.Add(what); }

            bool Has(string no) => items.Any(i => i.StartsWith(no + " - "));
            Expect("a completed trip with no report is missing", Has(nWaiting));
            Expect("a trip with a draft report is missing", Has(nDraft));
            Expect("a completed special trip that needs a report is missing", Has(sWaiting));
            Expect("a trip whose report is already finished is still listed", !Has(nDone));
            Expect("a trip that is not completed is listed", !Has(nPlanned));
            Expect("a special trip that is not completed is listed", !Has(sPlanned));
            Expect($"expected 3 lines, found {items.Count}", items.Count == 3);

            // oldest first: waiting (3 days ago), special (2), draft (1)
            var order = items.Select(i => i.Split(" - ")[0]).ToList();
            Expect($"not oldest first: {string.Join(", ", order)}", order.SequenceEqual(new[] { nWaiting, sWaiting, nDraft }));
            Expect("the draft line is not tagged", items.FirstOrDefault(i => i.StartsWith(nDraft + " - "))?.Contains("draft saved") == true);
            foreach (var i in items) Expect($"line too long ({i.Length}): {i}", i.Length <= 140);

            // opening a finished report still shows its trip (the list adds it when it is opened)
            var find = typeof(TripReportView).GetMethod("FindDeliveryItemIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
            int idx = (int)find.Invoke(view, new object[] { nDone })!;
            Expect("opening a finished report did not bring its trip into the list", idx >= 0 && (cb.Items[idx]?.ToString() ?? "").StartsWith(nDone + " - "));
            int missing = (int)find.Invoke(view, new object[] { "NO-SUCH-DISPATCH" })!;
            Expect("an unknown dispatch number should give -1", missing == -1);
            // starting a new report takes the added trip out again
            typeof(TripReportView).GetMethod("ClearFields", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);
            Expect("a finished report's trip stayed in the list after a new report was started", !cb.Items.Cast<object>().Any(o => (o.ToString() ?? "").StartsWith(nDone + " - ")));
            Expect($"expected 3 lines again after clearing, found {cb.Items.Count}", cb.Items.Count == 3);

            lock (boxes.Boxes) if (boxes.Boxes.Count > 0) problems.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
            host.Close();
            return string.Join(" | ", problems);
        });
    }

    [Check]
    public static string TripReportDropdown_EmptyMessage_WhenEveryCompletedTripHasAReport()
    {
        Fresh();
        int done = Seed.Delivery();
        AddReport(NumberOf(done), done, "Completed");
        return RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(2000);
            var cb = FieldOf<ComboBox>(view, "cbDelivery");
            if (cb == null) return "the Dispatch order drop-down was not found";
            typeof(TripReportView).GetMethod("OpenNewTripReport", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);   // like clicking "+ New trip report"
            Pump(500);
            host.Close();
            return cb.Items.Count == 0 ? "" : $"the list should be empty but has {cb.Items.Count} line(s)";
        });
    }
}
