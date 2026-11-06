using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>The history of an order tells one story (trip report → invoice → statement → receipt), a trip billed twice is found,
/// and a receipt carries the day the money came in (today or earlier, never in the future or before the invoice).</summary>
public static partial class Checks
{
    [Check]
    public static string OrderChain_ShowsEachStageOfATrip()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery();
        string s0 = MJdatabase.GetOrderChainForTrip(d).Stage;
        int tr = Seed.TripReport(d);
        Exec("UPDATE TripReports SET Status='Completed' WHERE Id=@i", ("@i", tr));
        string s1 = MJdatabase.GetOrderChainForTrip(d).Stage;
        int inv = Seed.Invoice(d, 500m);
        string s2 = MJdatabase.GetOrderChainForTrip(d).Stage;
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Exec("UPDATE Invoices SET SoaId=@s WHERE Id=@i", ("@s", soa), ("@i", inv));
        string s3 = MJdatabase.GetOrderChainForTrip(d).Stage;
        Exec("UPDATE Invoices SET Status='Paid' WHERE Id=@i", ("@i", inv));
        Exec("UPDATE StatementsOfAccount SET Status='Paid' WHERE Id=@s", ("@s", soa));
        Seed.Receipt(inv, 500m, "9801", soa);
        var c = MJdatabase.GetOrderChainForTrip(d);
        bool ok = s0 == "Waiting for a trip report" && s1 == "Ready to invoice" && s2 == "Invoiced, waiting for payment"
            && s3 == "On a statement, waiting for payment" && c.Stage == "Paid"
            && c.TripReportNo.Length > 0 && c.SoaNumber.Length > 0 && c.ReceiptNo == "9801" && c.PaidOn.HasValue;
        return ok ? "" : $"stages: {s0} | {s1} | {s2} | {s3} | {c.Stage}; line: {c.Line()}";
    }

    [Check]
    public static string OrderChain_FollowsAnOldStatementsInvoice()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa);
        var c = MJdatabase.GetOrderChainForTrip(d);
        return c.Invoice != null && c.Invoice.OnOldStatement && c.SoaNumber.Length > 0 && c.Stage == "On a statement, waiting for payment" ? "" : "stage: " + c.Stage;
    }

    [Check]
    public static string BilledMoreThanOnce_FindsAnOwnInvoicePlusAnOldStatement_AndNothingElse()
    {
        Fresh(); RunBackfill();
        int twice = Seed.Delivery(); Seed.TripReport(twice);
        int once = Seed.Delivery(); Seed.TripReport(once);
        Seed.Invoice(twice, 100m); // its own invoice
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, twice);
        Seed.Invoice(0, 500m, soaId: soa); // and an old statement's invoice that bills it too
        Seed.Invoice(once, 200m);
        Seed.Invoice(once, 300m, status: "Cancelled"); // a cancelled one does not count
        var found = MJdatabase.GetTripsBilledMoreThanOnce();
        string no = Str("SELECT DispatchNumber FROM Deliveries WHERE Id=@d", ("@d", twice));
        return found.Count == 1 && found[0].TripLabel == no && found[0].Invoices.Count == 2 ? "" : "found: " + string.Join(", ", found.Select(f => f.TripLabel + "(" + f.Invoices.Count + ")"));
    }

    [Check]
    public static string SettledRecords_ShowTheDispatchAndTripReportBehindThem()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        int soa = Seed.Soa(status: "Paid", total: 500m);
        Seed.SoaItem(soa, d);
        int inv = Seed.Invoice(d, 500m, status: "Paid", soaId: soa);
        Seed.Receipt(inv, 500m, "9802", soa);
        var recs = MJdatabase.GetSettledRecords(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
        var trips = MJdatabase.ResolveSettledTrips(recs);
        string dispatch = Str("SELECT DispatchNumber FROM Deliveries WHERE Id=@d", ("@d", d));
        string report = Str("SELECT TripReportNumber FROM TripReports WHERE Id=@t", ("@t", tr));
        return recs.Count == 1 && trips.Count == 1 && trips[0].Count == 1 && trips[0][0].Number == dispatch && trips[0][0].TripReportNo == report && trips[0][0].DeliveryId == d
            ? "" : $"records {recs.Count}, trips {string.Join("|", trips.Select(t => string.Join(",", t.Select(x => x.Number + "/" + x.TripReportNo))))}";
    }

    private static PayTarget TargetOf(int deliveryId)
    {
        var inv = InvoiceWithNumber(deliveryId, "SI-" + (7000 + new Random().Next(1, 99999)), 1000m);
        inv.IssueDate = DateTime.Today.AddDays(-10);
        MJdatabase.SaveInvoice(inv);
        return MJdatabase.GetPayTargets().First(t => t.Invoices.Any(i => i.Id == inv.Id));
    }

    [Check]
    public static string Receipt_CarriesAnEarlierPaymentDate()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var target = TargetOf(d);
        DateTime day = DateTime.Today.AddDays(-3);
        MJdatabase.SaveOneReceipt("9901", target, "Cash", "", "", day);
        string stored = Str("SELECT PaymentDate FROM CollectionReceipts WHERE ReceiptNumber='9901'");
        // and the day can be corrected later, still not into the future
        MJdatabase.UpdateReceiptDetails("9901", "9901", "Cash", "", "", DateTime.Today.AddDays(-2));
        string corrected = Str("SELECT PaymentDate FROM CollectionReceipts WHERE ReceiptNumber='9901'");
        bool futureRefused = Refused(() => MJdatabase.UpdateReceiptDetails("9901", "9901", "Cash", "", "", DateTime.Today.AddDays(1)), out _);
        return stored.StartsWith(day.ToString("yyyy-MM-dd")) && corrected.StartsWith(DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd")) && futureRefused
            ? "" : $"saved {stored}, corrected {corrected}, future {(futureRefused ? "refused" : "ACCEPTED")}";
    }

    [Check]
    public static string Receipt_DefaultsToToday_AndRefusesAFutureDayOrOneBeforeTheInvoice()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        int c = Seed.Delivery(); Seed.TripReport(c);
        var ta = TargetOf(a); var tb = TargetOf(b); var tc = TargetOf(c);
        MJdatabase.SaveOneReceipt("9911", ta, "Cash", "", ""); // no date given
        string today = Str("SELECT PaymentDate FROM CollectionReceipts WHERE ReceiptNumber='9911'");
        bool future = Refused(() => MJdatabase.SaveOneReceipt("9912", tb, "Cash", "", "", DateTime.Today.AddDays(1)), out _);
        bool beforeInvoice = Refused(() => MJdatabase.SaveOneReceipt("9913", tc, "Cash", "", "", DateTime.Today.AddDays(-11)), out _); // invoice is dated 10 days ago
        long saved = Long("SELECT COUNT(*) FROM CollectionReceipts WHERE ReceiptNumber IN ('9912','9913')");
        return today.StartsWith(DateTime.Today.ToString("yyyy-MM-dd")) && future && beforeInvoice && saved == 0 ? ""
            : $"default {today}, future {(future ? "refused" : "ACCEPTED")}, before invoice {(beforeInvoice ? "refused" : "ACCEPTED")}, saved {saved}";
    }
}
