using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
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
    public static string TripReportDropdown_SearchClientShowSort_PickedTripStays_ResetOnNewReport()
    {
        Fresh();
        int aWait = Seed.Delivery("CLIENT A"), aDraft = Seed.Delivery("CLIENT A"), bWait = Seed.Delivery("CLIENT B");
        int sp = Seed.SpecialTrip("CLIENT A");
        SetDate("Deliveries", bWait, 5); SetDate("Deliveries", aWait, 3); SetDate("SpecialTrips", sp, 2); SetDate("Deliveries", aDraft, 1);
        Exec("UPDATE SpecialTrips SET ModeOfPaymentWithReport = 1");
        string nAw = NumberOf(aWait), nAd = NumberOf(aDraft), nBw = NumberOf(bWait);
        string nSp = MJdatabase.GetSpecialTrips().First(t => t.Id == sp).OrderReference;
        AddReport(nAd, aDraft, "Draft");

        return RunOnSta(() =>
        {
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(2000);
            var cb = FieldOf<ComboBox>(view, "cbDelivery");
            var search = FieldOf<TextBox>(view, "txtPickSearch"); var client = FieldOf<ComboBox>(view, "cbPickClient");
            var show = FieldOf<ComboBox>(view, "cbPickShow"); var sort = FieldOf<ComboBox>(view, "cbPickSort"); var count = FieldOf<Label>(view, "lblPickCount");
            if (cb == null || search == null || client == null || show == null || sort == null || count == null) return "a filter control was not found";
            typeof(TripReportView).GetMethod("OpenNewTripReport", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);
            Pump(500);
            var problems = new List<string>();
            void Expect(string what, bool ok) { if (!ok) problems.Add(what); }
            List<string> Nos() => cb.Items.Cast<object>().Select(o => (o.ToString() ?? "").Split(" - ")[0]).ToList();
            bool Is(params string[] expected) => Nos().SequenceEqual(expected);
            string Seq() => string.Join(", ", Nos());

            Expect($"default is not oldest first: {Seq()}", Is(nBw, nAw, nSp, nAd));
            Expect($"count text '{count.Text}'", count.Text == "4 waiting");

            search.Text = "client b";
            Expect($"search 'client b': {Seq()}", Is(nBw));
            Expect($"count with a search '{count.Text}'", count.Text == "1 of 4 waiting");
            search.Text = "DRAFT";
            Expect($"search 'draft': {Seq()}", Is(nAd));
            search.Text = "no such thing";
            Expect("a search with no match still lists trips", cb.Items.Count == 0);
            Expect($"no-match text '{count.Text}'", count.Text == "No trip matches.");
            search.Text = "";

            Expect($"client list: {string.Join(", ", client.Items.Cast<object>())}", client.Items.Cast<object>().Select(o => o.ToString()).SequenceEqual(new[] { "All clients", "CLIENT A", "CLIENT B" }));
            client.SelectedIndex = client.FindStringExact("CLIENT B");
            Expect($"client B: {Seq()}", Is(nBw));
            client.SelectedIndex = 0;

            show.SelectedIndex = 2; Expect($"show drafts: {Seq()}", Is(nAd));
            show.SelectedIndex = 1; Expect($"show no report yet: {Seq()}", Is(nBw, nAw, nSp));
            show.SelectedIndex = 3; Expect($"show special trips: {Seq()}", Is(nSp));
            show.SelectedIndex = 0;

            sort.SelectedIndex = 1; Expect($"newest first: {Seq()}", Is(nAd, nSp, nAw, nBw));
            sort.SelectedIndex = 2; Expect($"client A-Z: {Seq()}", Nos().Take(3).All(n => n != nBw) && Nos().Last() == nBw);
            sort.SelectedIndex = 3; Expect($"dispatch #: {Seq()}", Nos().SequenceEqual(Nos().OrderBy(n => n, StringComparer.OrdinalIgnoreCase)));
            sort.SelectedIndex = 0;

            // the picked trip never disappears
            int at = Nos().IndexOf(nAd);
            cb.SelectedIndex = at; Pump(300);
            search.Text = "client b";
            Expect($"the picked trip was dropped by a search: {Seq()}", Nos().Contains(nAd) && Nos().Contains(nBw));
            Expect("the picked trip is no longer selected", cb.SelectedItem?.ToString()?.StartsWith(nAd + " - ") == true);

            // starting a new report resets everything
            typeof(TripReportView).GetMethod("ClearFields", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);
            Expect("search not cleared by a new report", search.Text == "");
            Expect("client/show/sort not reset by a new report", client.SelectedIndex == 0 && show.SelectedIndex == 0 && sort.SelectedIndex == 0);
            Expect($"list not restored by a new report: {Seq()}", Is(nBw, nAw, nSp, nAd));

            lock (boxes.Boxes) if (boxes.Boxes.Count > 0) problems.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
            host.Close();
            return string.Join(" | ", problems);
        });
    }

    [Check]
    public static string UnsavedTracker_IgnoresControlsNamedNoDirty()
    {
        return RunOnSta(() =>
        {
            using var f = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(400, 200), Opacity = 0 };
            var panel = new Panel { Dock = DockStyle.Fill };
            var ignored = new TextBox { Name = "noDirtySearch", Left = 5, Top = 5 };
            var normal = new TextBox { Name = "txtNormal", Left = 5, Top = 40 };
            panel.Controls.AddRange(new Control[] { ignored, normal });
            f.Controls.Add(panel); f.Show(); f.Activate(); Pump(400);
            var tracker = new FormDirtyTracker(panel);
            f.ActiveControl = ignored; ignored.Focus(); Pump(100);
            ignored.Text = "typed in a search box";
            if (tracker.IsDirty) return "typing in a noDirty control counted as unsaved changes";
            f.ActiveControl = normal; normal.Focus(); Pump(100);
            if (!normal.ContainsFocus) return "(test setup) the window could not take focus";
            normal.Text = "typed in a normal box";
            f.Close();
            return tracker.IsDirty ? "" : "typing in a normal box was not noticed";
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
