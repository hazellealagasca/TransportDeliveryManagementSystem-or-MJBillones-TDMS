using System.Globalization;
using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 20 (no "Mark complete"): a dispatch that has left shows Current and waits for its trip report; finishing the report completes it in the same save;
/// a Draft never does; a dispatch that has not left is not available; the truck and crew are free 24 hours after departure (decision A) while the
/// dispatch stays Current; and there is no "Mark complete" item in the dispatch menus.
/// </summary>
public static partial class Checks
{
    private sealed class AcTrip
    {
        public int Id; public string Number = ""; public int TruckId, DriverId, AideId, ScheduleId, StopId;
        public string Plate = "";
    }

    private static string AcTime(DateTime t) => t.ToString("hh:mm tt", CultureInfo.InvariantCulture);

    /// <summary>A dispatch with a real truck, driver, aide, loading schedule and pending stop, that left (or leaves) <paramref name="hoursAgo"/> hours before now (negative = in the future).</summary>
    private static AcTrip AcSeed(double hoursAgo, string status = "In Transit", string tag = "1")
    {
        var dep = DateTime.Now.AddHours(-hoursAgo);
        string plate = "AC-" + tag;
        MJdatabase.SaveTruck(new Truck { PlateNumber = plate, TruckType = "4T", CapacityCrates = 120, CargoType = "Cold / Chilled", Status = "Active" });
        int truck = MJdatabase.GetTrucks().First(t => t.PlateNumber == plate).Id;
        int driver = AdSeedCrew("Driver" + tag, "Dan", CrewManagementView.RoleDriver);
        int aide = AdSeedCrew("Aide" + tag, "Abe", CrewManagementView.RoleAide);
        int d = Seed.Delivery("CLIENT A", status);
        int ls = (int)Exec("INSERT INTO LoadingSchedules (ScheduleNumber, ClientName, Destination, ScheduleDate, DepartureTime, TotalCrates, Status) VALUES (@n, 'CLIENT A', 'Route 1', @d, @t, 15, 'Assigned')",
            ("@n", "LS-AC-" + tag), ("@d", dep.ToString("yyyy-MM-dd")), ("@t", AcTime(dep)));
        int stop = (int)Exec("INSERT INTO DispatchStops (DeliveryId, StopSequence, LoadingScheduleId, ClientName, Destination, Status) VALUES (@d, 1, @l, 'CLIENT A', 'Store 1', 'Pending')", ("@d", d), ("@l", ls));
        Exec("UPDATE Deliveries SET TruckId=@t, DriverId=@dr, AideId=@a, AideIds='', LoadingScheduleId=@l, DispatchDate=@dd, DepartureTime=@tm WHERE Id=@i",
            ("@t", truck), ("@dr", driver), ("@a", aide), ("@l", ls), ("@dd", dep.ToString("yyyy-MM-dd")), ("@tm", AcTime(dep)), ("@i", d));
        return new AcTrip { Id = d, Number = Str("SELECT DispatchNumber FROM Deliveries WHERE Id=@i", ("@i", d)), TruckId = truck, DriverId = driver, AideId = aide, ScheduleId = ls, StopId = stop, Plate = plate };
    }

    private static string AcStatus(int id) => Str("SELECT Status FROM Deliveries WHERE Id=@i", ("@i", id));
    private static string AcTruckStatus(AcTrip t) => Str("SELECT Status FROM Trucks WHERE Id=@i", ("@i", t.TruckId));
    private static bool AcCrewFree(int crewId) => Long("SELECT IsAvailable FROM Crews WHERE Id=@i", ("@i", crewId)) == 1;

    private static TripReport AcReport(AcTrip t, string status) => new TripReport
    {
        DeliveryId = t.Id, DispatchNumber = t.Number, Status = status, ReportDate = DateTime.Now,
        StoreManagerLastName = "Santos", StoreManagerFirstName = "Ana", DeliveredGCrates = 10, DeliveredECrates = 5
    };

    private static string AcAuditCount(int deliveryId) => Str("SELECT COUNT(*) FROM AuditTrail WHERE ActionType='ORDER_COMPLETED' AND TableName='Deliveries' AND RecordId=@i AND ReasonOrNotes LIKE '%trip report%'", ("@i", deliveryId));

    // (a) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_DepartedDispatch_ShowsCurrentToday_ForTripReportFromNextDay_IsWaitingForReport_AndCanGetOne()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var young = AcSeed(2, tag: "1");     // left 2 hours ago
        var old = AcSeed(30, tag: "2");      // left 30 hours ago: past the 24 hours (an earlier day)
        var bad = new List<string>();
        foreach (var t in new[] { young, old })
        {
            var del = MJdatabase.GetDeliveries().First(x => x.Id == t.Id);
            if (del.Status != "In Transit") bad.Add($"{t.Number} stored status is '{del.Status}'");
            if (StatusText.Display(del.Status) != "Current") bad.Add($"{t.Number} shows '{StatusText.Display(del.Status)}'");
            if (!DispatchGate.CanFileTripReport(del) || DispatchGate.WhyTripReportBlocked(del) != null) bad.Add($"{t.Number} is not available for a trip report");
            if (DispatchGate.IsCompleted(del)) bad.Add($"{t.Number} counts as completed");
            var row = MJdatabase.GetDispatchesAwaitingReport().FirstOrDefault(r => r.DeliveryId == t.Id);
            if (row == null || row.ReportState != "No trip report yet") bad.Add($"{t.Number} is not in the waiting list ({row?.ReportState})");
            if (!MJdatabase.GetTripsWaitingForTripReport().Any(r => r.DeliveryId == t.Id)) bad.Add($"{t.Number} is not in GetTripsWaitingForTripReport");
        }
        // The dispatch screen shows Current (today's dispatch) or For trip report (an earlier day), and no "Awaiting completion" / "Complete" label.
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var grid = DcGet<DataGridView>(view, "gridDispatch");
            view.RefreshData(); Pump(600);
            var statusCol = grid.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "Status");
            var shown = grid.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is Delivery).ToDictionary(r => ((Delivery)r.DataBoundItem!).Id, r => (Date: ((Delivery)r.DataBoundItem!).DispatchDate.Date, Text: r.Cells[statusCol.Index].FormattedValue?.ToString() ?? ""));
            host.Close();
            // Nov 28: Current only on the dispatch day; from the next day, with no finished trip report, it reads For trip report.
            var wrong = new List<string>();
            foreach (var t in new[] { young, old })
            {
                if (!shown.TryGetValue(t.Id, out var s)) { wrong.Add(t.Number + " not listed"); continue; }
                string want = s.Date < DateTime.Today ? "For trip report" : "Current";
                if (s.Text != want) wrong.Add($"{t.Number} (dispatch day {s.Date:yyyy-MM-dd}) shows '{s.Text}', expected '{want}'");
            }
            return shown.Count == 2 ? string.Join("; ", wrong) : "the status column shows: " + string.Join("|", shown.Values.Select(v => v.Text));
        }));
        // A trip report can be started for each: it is numbered once and the dispatch stays On the road.
        foreach (var t in new[] { young, old })
        {
            string n1 = MJdatabase.CreateTripReportForDispatch(t.Id);
            string n2 = MJdatabase.CreateTripReportForDispatch(t.Id);
            if (!n1.StartsWith("TRIP-") || n1 != n2) bad.Add($"{t.Number}: report numbers '{n1}' / '{n2}'");
            var row = MJdatabase.GetDispatchesAwaitingReport().FirstOrDefault(r => r.DeliveryId == t.Id);
            if (row == null || row.ReportState != "Draft saved" || row.ReportNumber != n1) bad.Add($"{t.Number}: after the draft the row is {(row == null ? "gone" : row.ReportState)}");
        }
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    // (b) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_DraftReport_LeavesTheDispatchInTransit()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var t = AcSeed(2);
        var bad = new List<string>();
        MJdatabase.SaveTripReport(AcReport(t, TripReportStatus.Draft));
        int trId = (int)Long("SELECT Id FROM TripReports WHERE DeliveryId=@d", ("@d", t.Id));
        void Untouched(string when)
        {
            if (AcStatus(t.Id) != "In Transit") bad.Add($"{when}: status is {AcStatus(t.Id)}");
            if (AcTruckStatus(t) != "In Use") bad.Add($"{when}: truck is {AcTruckStatus(t)}");
            if (AcCrewFree(t.DriverId) || AcCrewFree(t.AideId)) bad.Add($"{when}: the crew was freed");
            if (Str("SELECT Status FROM DispatchStops WHERE Id=@i", ("@i", t.StopId)) != "Pending") bad.Add($"{when}: the stop changed");
            if (Str("SELECT Status FROM LoadingSchedules WHERE Id=@i", ("@i", t.ScheduleId)) == "Completed") bad.Add($"{when}: the loading schedule was completed");
            if (AcAuditCount(t.Id) != "0") bad.Add($"{when}: a completion was logged");
        }
        Untouched("new draft");
        if (TrStatus(trId) != TripReportStatus.Draft) bad.Add("the report is not a Draft");
        var again = MJdatabase.GetTripReports().First(x => x.Id == trId);
        again.Remarks = "still typing"; again.Status = TripReportStatus.Draft;
        MJdatabase.SaveTripReport(again);
        Untouched("updated draft");
        if (!MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == t.Id && r.ReportState == "Draft saved")) bad.Add("the draft dispatch left the waiting list");
        // An empty draft started from the waiting list (Create trip report) does not complete it either.
        var t2 = AcSeed(3, tag: "2");
        MJdatabase.CreateTripReportForDispatch(t2.Id);
        if (AcStatus(t2.Id) != "In Transit") bad.Add("Create trip report completed the dispatch: " + AcStatus(t2.Id));
        return string.Join("; ", bad);
    }

    // (c) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_FinishingTheReport_CompletesTheDispatch_InTheSameSave()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var viaDraft = AcSeed(5, tag: "1");
        var direct = AcSeed(5, tag: "2");
        var bad = new List<string>();
        MJdatabase.SaveTripReport(AcReport(viaDraft, TripReportStatus.Draft));
        int trId = (int)Long("SELECT Id FROM TripReports WHERE DeliveryId=@d", ("@d", viaDraft.Id));
        // The list before: both are on it.
        List<int> OnList() => RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var grid = DcGet<DataGridView>(view, "gridDispatch");
            view.RefreshData(); Pump(600);
            var ids = grid.Rows.Cast<DataGridViewRow>().Select(r => (r.DataBoundItem as Delivery)?.Id ?? 0).ToList();
            host.Close();
            return string.Join(",", ids);
        }).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        var before = OnList();
        if (!before.Contains(viaDraft.Id) || !before.Contains(direct.Id)) bad.Add("before: the open list is " + string.Join(",", before));
        // Finishing the Draft (Status Completed) completes its dispatch; a brand-new finished report does too.
        var fin = MJdatabase.GetTripReports().First(x => x.Id == trId);
        fin.Status = TripReportStatus.Completed;
        MJdatabase.SaveTripReport(fin);
        MJdatabase.SaveTripReport(AcReport(direct, TripReportStatus.Completed));
        foreach (var t in new[] { viaDraft, direct })
        {
            if (AcStatus(t.Id) != "Completed") bad.Add($"{t.Number}: status is {AcStatus(t.Id)}");
            if (AcTruckStatus(t) != "Active") bad.Add($"{t.Number}: truck is {AcTruckStatus(t)}");
            if (!AcCrewFree(t.DriverId) || !AcCrewFree(t.AideId)) bad.Add($"{t.Number}: the driver / aide were not freed");
            if (Str("SELECT Status FROM DispatchStops WHERE Id=@i", ("@i", t.StopId)) != "Delivered") bad.Add($"{t.Number}: the stop is {Str("SELECT Status FROM DispatchStops WHERE Id=@i", ("@i", t.StopId))}");
            if (Str("SELECT Status FROM LoadingSchedules WHERE Id=@i", ("@i", t.ScheduleId)) != "Completed") bad.Add($"{t.Number}: the loading schedule is {Str("SELECT Status FROM LoadingSchedules WHERE Id=@i", ("@i", t.ScheduleId))}");
            if (AcAuditCount(t.Id) != "1") bad.Add($"{t.Number}: completion logged {AcAuditCount(t.Id)} time(s)");
            if (MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == t.Id)) bad.Add($"{t.Number} is still on the waiting list");
            if (!DispatchGate.IsCompleted(MJdatabase.GetDeliveries().First(x => x.Id == t.Id))) bad.Add($"{t.Number} is not Completed for the invoice / statement gates");
        }
        if (Str("SELECT ReasonOrNotes FROM AuditTrail WHERE ActionType='ORDER_COMPLETED' AND RecordId=@i", ("@i", viaDraft.Id)).Contains(Str("SELECT TripReportNumber FROM TripReports WHERE Id=@i", ("@i", trId))) == false) bad.Add("the audit note does not name the trip report");
        var after = OnList();
        // Nov 28: a completed dispatch stays in the active list until it is paid (it is now "For Statement of Account"); only paid or cancelled ones leave.
        if (!after.Contains(viaDraft.Id) || !after.Contains(direct.Id)) bad.Add("after: a completed, unpaid dispatch left the active list: " + string.Join(",", after));
        // Completing again is harmless: saving the finished report once more changes nothing.
        var once = MJdatabase.GetTripReports().First(x => x.Id == trId); once.Remarks = "edited later";
        MJdatabase.SaveTripReport(once);
        if (AcStatus(viaDraft.Id) != "Completed" || AcAuditCount(viaDraft.Id) != "1") bad.Add("saving the finished report again changed the dispatch");
        // Voiding the finished report does not bring the dispatch back to In Transit: it stays Completed and is waiting for a report again.
        Exec("UPDATE TripReports SET IsVoid=1 WHERE Id=@i", ("@i", trId));
        if (AcStatus(viaDraft.Id) != "Completed") bad.Add("voiding the report changed the dispatch to " + AcStatus(viaDraft.Id));
        if (!MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == viaDraft.Id)) bad.Add("a voided report did not put the dispatch back on the waiting list");
        return string.Join("; ", bad);
    }

    [Check]
    public static string AutoComplete_FinishingTheReport_AllOrNothing_AndFreedTruckGivenToANewDispatchStaysBusy()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var bad = new List<string>();
        // A cancelled dispatch can't get a finished report: nothing is written (no report row, status unchanged).
        var cancelled = AcSeed(5, "Cancelled", "1");
        string why = ""; try { MJdatabase.SaveTripReport(AcReport(cancelled, TripReportStatus.Completed)); } catch (InvalidOperationException ex) { why = ex.Message; }
        if (why.Length == 0 || Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId=@d", ("@d", cancelled.Id)) != 0 || AcStatus(cancelled.Id) != "Cancelled") bad.Add($"cancelled: refused '{why}'");
        // The old trip passed 24 hours, its truck and crew went to a new dispatch; finishing the old report must not free them.
        var old = AcSeed(30, tag: "2");
        MJdatabase.ReleaseResourcesAfterWait();
        Exec("INSERT INTO Deliveries (DispatchNumber, ClientName, Destination, TruckId, TruckInfo, DriverId, DriverName, AideId, AideNames, GCrate, ECrate, TotalCrates, DispatchDate, DepartureTime, Status) VALUES ('AC-NEW','CLIENT B','Route 2',@t,'x',@d,'x',@a,'x',1,0,1,@dd,'11:59 PM','Scheduled')",
            ("@t", old.TruckId), ("@d", old.DriverId), ("@a", old.AideId), ("@dd", DateTime.Today.ToString("yyyy-MM-dd")));
        MJdatabase.ReleaseResourcesAfterWait();   // re-locks them for the new dispatch (it is dated today)
        MJdatabase.SaveTripReport(AcReport(old, TripReportStatus.Completed));
        if (AcStatus(old.Id) != "Completed") bad.Add("old dispatch is " + AcStatus(old.Id));
        if (AcTruckStatus(old) != "In Use") bad.Add("the truck of the new dispatch was freed: " + AcTruckStatus(old));
        if (AcCrewFree(old.DriverId) || AcCrewFree(old.AideId)) bad.Add("the crew of the new dispatch was freed");
        return string.Join("; ", bad);
    }

    // (d) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_NotYetDeparted_OrCancelled_IsNotAvailableForATripReport()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var scheduled = AcSeed(-3, "Scheduled", "1");      // leaves in 3 hours
        var early = AcSeed(-3, "In Transit", "2");         // "Start trip now" before its set time: it HAS left, so its trip report can be filed
        var cancelled = AcSeed(2, "Cancelled", "3");
        var bad = new List<string>();
        {
            var del = MJdatabase.GetDeliveries().First(x => x.Id == early.Id);
            if (!DispatchGate.CanFileTripReport(del) || DispatchGate.WhyTripReportBlocked(del) != null) bad.Add($"{early.Number} (started early) is not available for a trip report");
            if (!MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == early.Id)) bad.Add($"{early.Number} (started early) is not on the waiting list");
        }
        foreach (var t in new[] { scheduled, cancelled })
        {
            var del = MJdatabase.GetDeliveries().First(x => x.Id == t.Id);
            if (DispatchGate.CanFileTripReport(del) || DispatchGate.WhyTripReportBlocked(del) == null) bad.Add($"{t.Number} ({del.Status}) is available");
            if (MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == t.Id)) bad.Add($"{t.Number} is on the waiting list");
            if (MJdatabase.GetTripsWaitingForTripReport().Any(r => r.DeliveryId == t.Id)) bad.Add($"{t.Number} is on GetTripsWaitingForTripReport");
            string refusedDraft = "", refusedFinal = "", refusedCreate = "";
            try { MJdatabase.SaveTripReport(AcReport(t, TripReportStatus.Draft)); } catch (InvalidOperationException ex) { refusedDraft = ex.Message; }
            try { MJdatabase.SaveTripReport(AcReport(t, TripReportStatus.Completed)); } catch (InvalidOperationException ex) { refusedFinal = ex.Message; }
            try { MJdatabase.CreateTripReportForDispatch(t.Id); } catch (InvalidOperationException ex) { refusedCreate = ex.Message; }
            if (refusedDraft.Length == 0 || refusedFinal.Length == 0 || refusedCreate.Length == 0) bad.Add($"{t.Number} was not refused (draft '{refusedDraft}', final '{refusedFinal}', create '{refusedCreate}')");
            if (Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId=@d", ("@d", t.Id)) != 0) bad.Add($"{t.Number} got a report row");
        }
        if (AcStatus(scheduled.Id) != "Scheduled" || AcStatus(cancelled.Id) != "Cancelled") bad.Add("a refused save changed a status");
        // Old data is unchanged: a Completed dispatch with no report is available.
        var done = AcSeed(40, "Completed", "4");
        if (!MJdatabase.GetDispatchesAwaitingReport().Any(r => r.DeliveryId == done.Id)) bad.Add("a Completed dispatch is not on the waiting list");
        // Invoice and statement still need Completed.
        var onRoad = MJdatabase.GetDeliveries().First(x => x.Id == scheduled.Id);
        if (DispatchGate.WhyBlocked(onRoad, "bill it") == null) bad.Add("a Scheduled dispatch passes the Completed gate");
        return string.Join("; ", bad);
    }

    // (e) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_AfterTwentyFourHours_TruckAndCrewAreFree_UnderTwentyFourTheyAreBusy()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        Exec("DELETE FROM Trucks"); Exec("DELETE FROM Crews");
        var young = AcSeed(5, tag: "1");     // left 5 hours ago: busy
        var old = AcSeed(30, tag: "2");      // left 30 hours ago: free again
        var bad = new List<string>();
        MJdatabase.ReleaseResourcesAfterWait();
        MJdatabase.ReleaseResourcesAfterWait();   // running it twice changes nothing
        MJdatabase.GetDeliveries(); MJdatabase.ReconcileResourceStatuses();   // nothing re-locks them
        if (AcStatus(old.Id) != "In Transit") bad.Add("the old dispatch is " + AcStatus(old.Id) + " (it must stay In Transit / Current)");
        if (AcTruckStatus(old) != "Active" || !AcCrewFree(old.DriverId) || !AcCrewFree(old.AideId)) bad.Add($"30 h: truck {AcTruckStatus(old)}, driver free {AcCrewFree(old.DriverId)}, aide free {AcCrewFree(old.AideId)}");
        if (AcTruckStatus(young) != "In Use" || AcCrewFree(young.DriverId) || AcCrewFree(young.AideId)) bad.Add($"5 h: truck {AcTruckStatus(young)}, driver free {AcCrewFree(young.DriverId)}, aide free {AcCrewFree(young.AideId)}");
        var oldDate = DateTime.Parse(Str("SELECT DispatchDate FROM Deliveries WHERE Id=@i", ("@i", old.Id))).Date;
        var youngDate = DateTime.Parse(Str("SELECT DispatchDate FROM Deliveries WHERE Id=@i", ("@i", young.Id))).Date;
        // The helper
        var oldD = MJdatabase.GetDeliveries().First(x => x.Id == old.Id); var youngD = MJdatabase.GetDeliveries().First(x => x.Id == young.Id);
        if (MJdatabase.HoldsResources(oldD, DateTime.Now) || !MJdatabase.HoldsResources(youngD, DateTime.Now)) bad.Add("HoldsResources is wrong");
        if (!MJdatabase.HoldsResources(oldD, DateTime.Now.AddHours(-10))) bad.Add("HoldsResources frees it before 24 hours");
        // Availability checks used when a dispatch is saved.
        if (MJdatabase.IsTruckOnActiveTrip(old.TruckId) || MJdatabase.IsDriverOnActiveTrip(old.DriverId)) bad.Add("IsTruckOnActiveTrip / IsDriverOnActiveTrip still hold the 30 h trip");
        if (!MJdatabase.IsTruckOnActiveTrip(young.TruckId) || !MJdatabase.IsDriverOnActiveTrip(young.DriverId)) bad.Add("IsTruckOnActiveTrip / IsDriverOnActiveTrip free the 5 h trip");
        string? okToday = MJdatabase.ValidateResourceConflict(0, 0, old.TruckId, old.DriverId, new List<int> { old.AideId }, DateTime.Today);
        if (okToday != null) bad.Add("a new dispatch today with the 30 h truck and crew is refused: " + okToday.Replace("\n", " "));
        string? okOnDate = MJdatabase.ValidateResourceConflict(0, 0, old.TruckId, old.DriverId, new List<int> { old.AideId }, oldDate);
        if (okOnDate != null) bad.Add("a new dispatch on the old dispatch's own date with the 30 h truck and crew is refused: " + okOnDate.Replace("\n", " "));
        string? busyToday = MJdatabase.ValidateResourceConflict(0, 0, young.TruckId, young.DriverId, new List<int> { young.AideId }, DateTime.Today);
        if (busyToday == null) bad.Add("a new dispatch today with the 5 h truck and crew is allowed");
        string? busyOnDate = MJdatabase.ValidateResourceConflict(0, 0, young.TruckId, young.DriverId, new List<int> { young.AideId }, youngDate);
        if (busyOnDate == null) bad.Add("a new dispatch on the 5 h dispatch's date with its truck and crew is allowed");
        // The suggestion on the old dispatch's date offers the 30 h truck and not the 5 h truck.
        var rec = MJdatabase.GetTruckRecommendation(1, oldDate, "Cold / Chilled");
        if (oldDate != youngDate && rec.RecommendedTruck?.Id != old.TruckId) bad.Add("the truck suggestion on " + oldDate.ToString("yyyy-MM-dd") + " is " + (rec.RecommendedTruck?.PlateNumber ?? "(none)"));
        // The dispatch screen's pickers.
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            view.RefreshData(); Pump(600);
            var msg = new List<string>();
            bool Free(string method, object arg) => (bool)typeof(DispatchManagementView).GetMethod(method, DcF)!.Invoke(view, new[] { arg })!;
            var trucks = MJdatabase.GetTrucks(); var crews = MJdatabase.GetCrews();
            DcGet<DateTimePicker>(view, "dtpDispatchDate").Value = DateTime.Today; Pump(300);
            if (!Free("TruckFree", trucks.First(x => x.Id == old.TruckId))) msg.Add("the 30 h truck is not selectable");
            if (!Free("CrewFree", crews.First(x => x.Id == old.DriverId)) || !Free("CrewFree", crews.First(x => x.Id == old.AideId))) msg.Add("the 30 h driver / aide are not selectable");
            if (Free("TruckFree", trucks.First(x => x.Id == young.TruckId))) msg.Add("the 5 h truck is selectable");
            if (Free("CrewFree", crews.First(x => x.Id == young.DriverId)) || Free("CrewFree", crews.First(x => x.Id == young.AideId))) msg.Add("the 5 h driver / aide are selectable");
            host.Close();
            return string.Join("; ", msg);
        }));
        // The nightly timer's date: the next moment a trip passes its 24 hours (the 5 h trip, 19 hours from now).
        var next = MJdatabase.NextCompletionDue();
        if (next == null || Math.Abs((next.Value - DateTime.Now.AddHours(19)).TotalMinutes) > 5) bad.Add("NextCompletionDue is " + next);
        // Finishing the old trip's report later frees again, harmlessly.
        MJdatabase.SaveTripReport(AcReport(old, TripReportStatus.Completed));
        if (AcStatus(old.Id) != "Completed" || AcTruckStatus(old) != "Active" || !AcCrewFree(old.DriverId)) bad.Add("finishing the report after the release broke the state");
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    // (f) -------------------------------------------------------------------------------------------------------------------------------
    [Check]
    public static string AutoComplete_NoMarkCompleteAnywhere_InTheMenus_OrTheCode()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); MJdatabase.EnsureCapacitySchema(); RunBackfill();
        var young = AcSeed(2, tag: "1"); var old = AcSeed(30, tag: "2"); var later = AcSeed(-3, "Scheduled", "3");
        var bad = new List<string>();
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            view.RefreshData(); Pump(600);
            var msg = new List<string>();
            var all = MJdatabase.GetDeliveries();
            static IEnumerable<string> Texts(ToolStripItemCollection items)
            {
                foreach (ToolStripItem it in items)
                {
                    yield return it.Text ?? "";
                    if (it is ToolStripMenuItem mi) foreach (var s in Texts(mi.DropDownItems)) yield return s;
                }
            }
            foreach (var t in new[] { young, old, later })
            {
                var m = new ContextMenuStrip();
                typeof(DispatchManagementView).GetMethod("BuildSingleMenu", DcF)!.Invoke(view, new object[] { m, all.First(x => x.Id == t.Id) });
                var texts = Texts(m.Items).ToList();
                if (texts.Count < 5) msg.Add($"{t.Number}: the menu has {texts.Count} items");
                foreach (var s in texts.Where(s => s.Contains("omplete", StringComparison.OrdinalIgnoreCase) || s.Contains("arrived", StringComparison.OrdinalIgnoreCase))) msg.Add($"{t.Number}: menu item '{s}'");
                if (!texts.Any(s => s.Contains("Start trip now") ) == (t.Id == later.Id)) msg.Add($"{t.Number}: 'Start trip now' is {(t.Id == later.Id ? "missing" : "shown for a trip that already left")}");
                if (t.Id != later.Id && !texts.Any(s => s.Contains("File trip report"))) msg.Add($"{t.Number}: no 'File trip report' item for a trip that left");
                if (t.Id == later.Id && !texts.Any(s => s.Contains("available once the truck leaves"))) msg.Add($"{t.Number}: the not-yet-departed trip report item is not disabled with a hint");
            }
            var multi = new ContextMenuStrip();
            typeof(DispatchManagementView).GetMethod("BuildMultiMenu", DcF)!.Invoke(view, new object[] { multi, all });
            foreach (var s in Texts(multi.Items).Where(s => s.Contains("omplete", StringComparison.OrdinalIgnoreCase))) msg.Add($"multi-select menu item '{s}'");
            host.Close();
            return string.Join("; ", msg);
        }));
        // The code: no screen calls CompleteDelivery any more, and the old buttons / labels are gone.
        string root = SourceRoot();
        foreach (var dir in new[] { "Views", "Forms", "Functions" })
            foreach (var file in Directory.GetFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                string src = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                if (src.Contains("CompleteDelivery(")) bad.Add(name + " still calls CompleteDelivery");
                if (src.Contains("Mark order complete") && !name.StartsWith("DispatchGate")) bad.Add(name + " still offers 'Mark order complete'");
                if (src.Contains("Awaiting completion") || src.Contains("IsAwaitingCompletion")) bad.Add(name + " still has the Awaiting completion label");
            }
        string detail = File.ReadAllText(Path.Combine(root, "Forms", "DispatchDetailsDialog.cs"));
        if (detail.Contains("CompleteOrder") || detail.Contains("\"✔  Complete\"")) bad.Add("the Dispatch details dialog still has a Complete button");
        string main = File.ReadAllText(Path.Combine(root, "MainForm.cs"));
        if (!main.Contains("ReleaseResourcesAfterWait")) bad.Add("the main window timer does not release trucks and crew");
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }
}
