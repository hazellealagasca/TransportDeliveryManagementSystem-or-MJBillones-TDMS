using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 12: Finance History lists settled work only (paid statements and paid lone invoices).</summary>
public static partial class Checks
{
    [Check]
    public static string SettledRecords_ListsPaidOnly()
    {
        Fresh(); RunBackfill();
        var (paidSoa, _) = SoaWithThreeInvoices();
        MJdatabase.SaveOneReceipt("0201", TargetFor(paidSoa), "Cash", "", "");
        var (openSoa, _) = SoaWithThreeInvoices();
        int lone = Seed.Invoice(Seed.Delivery(), 450m, PayStatus.Unpaid);
        MJdatabase.SaveOneReceipt("0202", MJdatabase.GetPayTargets().First(t => !t.IsSoa && t.Invoices[0].Id == lone), "Cash", "", "");
        int loneOpen = Seed.Invoice(Seed.Delivery(), 90m, PayStatus.Unpaid);

        var list = MJdatabase.GetSettledRecords(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
        bool hasPaidSoa = list.Any(x => x.Soa?.Id == paidSoa && x.Invoices.Count == 3 && x.ReceiptNo == "0201");
        bool hasLone = list.Any(x => x.Soa == null && x.Invoices.Count == 1 && x.Invoices[0].Id == lone && x.ReceiptNo == "0202");
        bool hasOpen = list.Any(x => x.Soa?.Id == openSoa || x.Invoices.Any(i => i.Id == loneOpen));
        var outside = MJdatabase.GetSettledRecords(DateTime.Today.AddDays(5), DateTime.Today.AddDays(9));
        return hasPaidSoa && hasLone && !hasOpen && list.Count == 2 && outside.Count == 0
            ? "" : $"paid soa {hasPaidSoa}, lone {hasLone}, open listed {hasOpen}, count {list.Count}, outside range {outside.Count}";
    }
}

public static partial class Checks
{
    /// <summary>Final review: after the backfill, no invoice in the real data is left part-paid (a receipt below its net amount
    /// blocks both a new receipt and the SOA picker, so such an invoice could never be settled).</summary>
    [Check]
    public static string RealCopy_NoPartPaidInvoiceLeft()
    {
        if (FromCopy() == null) return "no real database given";
        MJdatabase.InitializeDatabase();
        RunBackfill();
        var stuck = MJdatabase.GetTripInvoices().Where(i => i.Collected > 0 && i.Collected < i.Net && !i.IsStatus(PayStatus.Cancelled)).Select(i => i.Number).ToList();
        return stuck.Count == 0 ? "" : "part-paid invoices: " + string.Join(", ", stuck);
    }
}

public static partial class Checks
{
    /// <summary>
    /// Older statements carry ONE invoice for the whole statement (DeliveryId 0) instead of one per trip. Trips on such a
    /// statement are billed, so their trip reports are Invoiced, after the update and after a later re-sync.
    /// </summary>
    [Check]
    public static string LegacyStatementInvoice_MarksTripReportsInvoiced()
    {
        Fresh();
        int d1 = Seed.Delivery("BCS"), d2 = Seed.Delivery("BCS"), d3 = Seed.Delivery("BCS");
        int r1 = Seed.TripReport(d1), r2 = Seed.TripReport(d2), r3 = Seed.TripReport(d3);
        int soa = Seed.Soa("BCS", "Unpaid", 1000m);
        Seed.SoaItem(soa, d1); Seed.SoaItem(soa, d2);
        Seed.Invoice(0, 1000m, "Unpaid", "BCS", soaId: soa);          // the legacy whole-statement invoice
        int cancelledSoa = Seed.Soa("BCS", "Cancelled", 500m);
        Seed.SoaItem(cancelledSoa, d3);
        Seed.Invoice(0, 500m, "Cancelled", "BCS", soaId: cancelledSoa); // cancelled: d3 stays not invoiced
        RunBackfill();
        string a = TrStatus(r1), b = TrStatus(r2), c = TrStatus(r3);
        // A database that already ran the update gets the same answer from the re-sync.
        Exec("UPDATE TripReports SET Status='Completed'");
        MJdatabase.ResyncTripReportStatuses();
        string a2 = TrStatus(r1), c2 = TrStatus(r3);
        return a == "Invoiced" && b == "Invoiced" && c == "Completed" && a2 == "Invoiced" && c2 == "Completed"
            ? "" : $"after update {a}/{b}/{c}, after re-sync {a2}/{c2}";
    }
}

public static partial class Checks
{
    /// <summary>A cancelled invoice is history: saving over it (e.g. reusing it for another trip) is refused and nothing changes.</summary>
    [Check]
    public static string CancelledInvoice_CannotBeOverwritten()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int cancelled = Seed.Invoice(0, 8500m, PayStatus.Cancelled);
        var inv = MJdatabase.GetInvoices().First(i => i.Id == cancelled);
        inv.DeliveryId = d; inv.TotalAmount = 1000m; inv.NetAmount = 1000m; inv.Status = PayStatus.Unpaid;
        string error = "";
        try { MJdatabase.SaveInvoice(inv); } catch (InvalidOperationException ex) { error = ex.Message; }
        string status = Str("SELECT Status FROM Invoices WHERE Id=@i", ("@i", cancelled));
        long did = Long("SELECT COALESCE(DeliveryId,0) FROM Invoices WHERE Id=@i", ("@i", cancelled));
        return error.Contains("cancelled", StringComparison.OrdinalIgnoreCase) && status == PayStatus.Cancelled && did == 0
            ? "" : $"error '{error}', status {status}, delivery {did}";
    }
}

public static partial class Checks
{
    /// <summary>Trip Reports opens on Completed dispatches with no finished report (none, or only a Draft).</summary>
    [Check]
    public static string AwaitingReport_ListsCompletedWithoutFinishedReport()
    {
        Fresh(); RunBackfill();
        int none = Seed.Delivery(), draft = Seed.Delivery(), done = Seed.Delivery(), billed = Seed.Delivery(), open = Seed.Delivery(), voided = Seed.Delivery();
        Exec("UPDATE Deliveries SET Status='Completed'"); Exec("UPDATE Deliveries SET Status='In Transit' WHERE Id=@i", ("@i", open));
        int rd = Seed.TripReport(draft); Exec("UPDATE TripReports SET Status='Draft' WHERE Id=@i", ("@i", rd));
        Seed.TripReport(done);
        int rb = Seed.TripReport(billed); Exec("UPDATE TripReports SET Status='Invoiced' WHERE Id=@i", ("@i", rb));
        int rv = Seed.TripReport(voided); Exec("UPDATE TripReports SET IsVoid=1 WHERE Id=@i", ("@i", rv));
        var ids = MJdatabase.GetDispatchesAwaitingReport().Select(r => r.DeliveryId).OrderBy(x => x).ToList();
        var want = new[] { none, draft, voided }.OrderBy(x => x).ToList();
        return ids.SequenceEqual(want) ? "" : $"got [{string.Join(",", ids)}], want [{string.Join(",", want)}]";
    }
}

public static partial class Checks
{
    /// <summary>"Create trip report": a completed dispatch gets a numbered Draft report once; an open dispatch is refused.</summary>
    [Check]
    public static string CreateTripReportForDispatch_NumbersOnceAndNeedsCompleted()
    {
        Fresh(); RunBackfill();
        int done = Seed.Delivery(), open = Seed.Delivery();
        Exec("UPDATE Deliveries SET Status='Completed'"); Exec("UPDATE Deliveries SET Status='In Transit' WHERE Id=@i", ("@i", open));
        string n1 = MJdatabase.CreateTripReportForDispatch(done);
        string n2 = MJdatabase.CreateTripReportForDispatch(done);
        long count = Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId=@d", ("@d", done));
        string status = Str("SELECT Status FROM TripReports WHERE DeliveryId=@d", ("@d", done));
        var row = MJdatabase.GetDispatchesAwaitingReport().FirstOrDefault(r => r.DeliveryId == done);
        string refused = "";
        try { MJdatabase.CreateTripReportForDispatch(open); } catch (InvalidOperationException ex) { refused = ex.Message; }
        return n1.StartsWith("TRIP-") && n1 == n2 && count == 1 && status == TripReportStatus.Draft
               && row != null && row.ReportNumber == n1 && row.ReportState == "Draft saved" && refused.Length > 0
            ? "" : $"n1 '{n1}', n2 '{n2}', rows {count}, status {status}, listed {(row == null ? "no" : row.ReportState + "/" + row.ReportNumber)}, open refused '{refused}'";
    }
}
