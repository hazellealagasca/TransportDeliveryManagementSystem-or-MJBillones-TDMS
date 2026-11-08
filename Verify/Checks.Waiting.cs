using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace TDMS.Verify;

/// <summary>The "waiting for service invoice" and "waiting for trip report" lists: every trip is listed, none are hidden, and one function feeds every count.</summary>
public static partial class Checks
{
    [Check]
    public static string Waiting_ListsEveryFinishedTripWithoutALiveInvoice()
    {
        Fresh(); RunBackfill();
        var ids = new List<int>();
        for (int i = 0; i < 7; i++) { int d = Seed.Delivery(); Seed.TripReport(d); ids.Add(d); } // the old banner showed only 4 of these
        var rows = MJdatabase.GetTripsWaitingForInvoice();
        bool all = ids.All(id => rows.Any(r => r.DeliveryId == id));
        return rows.Count == 7 && all && MJdatabase.CountTripsWaitingForInvoice() == 7 ? "" : $"listed {rows.Count} of 7";
    }

    [Check]
    public static string Waiting_LeavesOutDraftReportsAndBilledTrips()
    {
        Fresh(); RunBackfill();
        int noReport = Seed.Delivery();
        int draft = Seed.Delivery(); int draftTr = Seed.TripReport(draft);
        Exec("UPDATE TripReports SET Status='Draft' WHERE Id=@i", ("@i", draftTr));
        int billed = Seed.Delivery(); Seed.TripReport(billed);
        MJdatabase.SaveInvoice(InvoiceWithNumber(billed, "SI-7301"));
        int ready = Seed.Delivery(); Seed.TripReport(ready);
        var rows = MJdatabase.GetTripsWaitingForInvoice();
        var blocked = MJdatabase.GetTripsWaitingForTripReport();
        bool onlyReady = rows.Count == 1 && rows[0].DeliveryId == ready;
        bool blockedOk = blocked.Any(b => b.DeliveryId == noReport) && blocked.Any(b => b.DeliveryId == draft) && blocked.Count == 2;
        return onlyReady && blockedOk ? "" : $"waiting for invoice {rows.Count}, waiting for report {blocked.Count}";
    }

    [Check]
    public static string Waiting_ACancelledInvoiceBringsTheTripBack()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-7401");
        MJdatabase.SaveInvoice(inv);
        bool gone = MJdatabase.GetTripsWaitingForInvoice().Count == 0;
        MJdatabase.DeleteRecord("Invoices", inv.Id);
        bool back = MJdatabase.GetTripsWaitingForInvoice().Any(r => r.DeliveryId == d);
        return gone && back ? "" : $"after saving {(gone ? "gone" : "STILL LISTED")}, after cancelling {(back ? "back" : "MISSING")}";
    }

    [Check]
    public static string Waiting_AgeColoursFollowTheLimits()
    {
        bool ok = MJdatabase.WaitingAge(0) == "Normal" && MJdatabase.WaitingAge(1) == "Normal"
               && MJdatabase.WaitingAge(2) == "Amber" && MJdatabase.WaitingAge(3) == "Red" && MJdatabase.WaitingAge(30) == "Red";
        return ok ? "" : "grey 0-1, amber 2, red 3+ expected";
    }

    [Check]
    public static string Waiting_RealCopy_ListAndCountAgree()
    {
        string? p = FromCopy();
        if (p == null) return "";
        var rows = MJdatabase.GetTripsWaitingForInvoice();
        var blocked = MJdatabase.GetTripsWaitingForTripReport();
        bool noDup = rows.Select(r => (r.DeliveryId, r.SpecialTripId)).Distinct().Count() == rows.Count;
        Console.WriteLine($"     (real copy: {rows.Count} waiting for invoice, {blocked.Count} waiting for trip report)");
        return noDup && rows.Count == MJdatabase.CountTripsWaitingForInvoice() ? "" : "duplicate rows or count mismatch";
    }
}
