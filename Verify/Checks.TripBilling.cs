using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>A trip is billed once: one live invoice per trip (also when an old statement's invoice bills it), numbers never repeat,
/// a cancelled invoice frees its trip, and an invoice on a statement keeps its amounts.</summary>
public static partial class Checks
{
    private static Invoice InvoiceWithNumber(int deliveryId, string number, decimal gross = 1500m)
    {
        var inv = NewInvoice(deliveryId, gross);
        inv.InvoiceNumber = number;
        return inv;
    }

    private static bool Refused(Action save, out string why)
    {
        why = "";
        try { save(); return false; }
        catch (InvalidOperationException ex) { why = ex.Message; return true; }
    }

    [Check]
    public static string SaveInvoice_RefusesSecondLiveInvoiceForTheSameTrip()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9001"));
        bool refused = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9002")), out var why);
        long live = Long("SELECT COUNT(*) FROM Invoices WHERE DeliveryId=@d AND Status<>'Cancelled'", ("@d", d));
        return refused && live == 1 ? "" : $"second invoice {(refused ? "refused" : "SAVED")}, live invoices {live} ({why})";
    }

    [Check]
    public static string SaveInvoice_AllowsANewInvoiceAfterTheFirstIsCancelled()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var first = InvoiceWithNumber(d, "SI-9101");
        MJdatabase.SaveInvoice(first);
        MJdatabase.DeleteRecord("Invoices", first.Id); // cancels it
        var second = InvoiceWithNumber(d, "SI-9102");
        try { MJdatabase.SaveInvoice(second); }
        catch (InvalidOperationException ex) { return "re-billing refused: " + ex.Message; }
        var live = MJdatabase.GetLiveInvoiceForTrip(d);
        return live != null && live.InvoiceId == second.Id ? "" : "live invoice is " + (live?.Number ?? "none");
    }

    [Check]
    public static string SaveInvoice_RefusesARepeatedNumberIgnoringCaseAndSpaces()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-9201"));
        bool refused = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, " si-9201 ")), out var why);
        return refused ? "" : "a repeated number was saved";
    }

    [Check]
    public static string LiveInvoice_SeesAnOldStatementsInvoice_AndBlocksASecondOne()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        int old = Seed.Invoice(0, 500m, soaId: soa); // an old statement's own invoice: no trip link
        var live = MJdatabase.GetLiveInvoiceForTrip(d);
        if (live == null || live.InvoiceId != old || !live.OnOldStatement) return "the old statement's invoice was not found for the trip";
        bool refused = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9301")), out _);
        return refused ? "" : "a second invoice was saved for a trip billed on an old statement";
    }

    [Check]
    public static string LiveInvoice_IgnoresACancelledStatement()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa);
        Exec("UPDATE StatementsOfAccount SET Status='Cancelled' WHERE Id=@s", ("@s", soa));
        return MJdatabase.GetLiveInvoiceForTrip(d) == null ? "" : "a cancelled statement still bills the trip";
    }

    [Check]
    public static string SaveInvoice_RefusesNewAmountsWhileTheInvoiceIsOnAStatement()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-9401", 1500m);
        MJdatabase.SaveInvoice(inv);
        int soa = Seed.Soa(status: "Unpaid", total: 1500m);
        Exec("UPDATE Invoices SET SoaId=@s WHERE Id=@i", ("@s", soa), ("@i", inv.Id));
        inv.SoaId = soa;
        inv.TotalAmount = 1800m; inv.NetAmount = 1800m;
        bool refused = Refused(() => MJdatabase.SaveInvoice(inv), out _);
        double stored = Convert.ToDouble(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)));
        return refused && stored == 1500.0 ? "" : $"change {(refused ? "refused" : "SAVED")}, stored net {stored}";
    }

    [Check]
    public static string OrderHistory_ShowsTheInvoiceOfATripBilledOnAnOldStatement()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 700m);
        Seed.SoaItem(soa, d);
        Exec("INSERT INTO Invoices (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status) VALUES ('SI-9501', @s, 0, 'CLIENT A', '2026-11-01', '2026-11-16', 700, 0, 700, 'Unpaid')", ("@s", soa));
        var rec = TransactionIndex.LoadAll(true).FirstOrDefault(t => t.Source is Delivery x && x.Id == d);
        return rec != null && rec.InvoiceNo == "SI-9501" && rec.Amount == 700m ? "" : $"Order History shows invoice '{rec?.InvoiceNo}' amount {rec?.Amount}";
    }

    [Check]
    public static string OrderHistory_IgnoresACancelledInvoice()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        Seed.Invoice(d, 900m, status: "Cancelled");
        var rec = TransactionIndex.LoadAll(true).FirstOrDefault(t => t.Source is Delivery x && x.Id == d);
        return rec != null && rec.InvoiceNo.Length == 0 ? "" : $"a cancelled invoice '{rec?.InvoiceNo}' is shown as the trip's invoice";
    }

    [Check]
    public static string DeleteDispatch_IsBlockedWhileAnOldStatementsInvoiceBillsIt()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa);
        string? block = MJdatabase.ValidateRecordDeletion("Deliveries", d);
        return !string.IsNullOrEmpty(block) && block.Contains("invoice") ? "" : "the dispatch could be deleted: " + (block ?? "no block");
    }

    [Check]
    public static string SaveInvoice_RefusesNewAmountsOnceAReceiptExists()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-9601", 1500m);
        MJdatabase.SaveInvoice(inv);
        Seed.Receipt(inv.Id, 1500m, "9601");
        inv.TotalAmount = 1800m; inv.NetAmount = 1800m; // would turn a paid invoice back to Unpaid with a receipt on it
        bool refused = Refused(() => MJdatabase.SaveInvoice(inv), out var why);
        double stored = Convert.ToDouble(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)));
        return refused && stored == 1500.0 ? "" : $"change {(refused ? "refused" : "SAVED")}, stored net {stored} ({why})";
    }

    [Check]
    public static string CancelStatement_SendsAnOldInvoicesTripReportsBackToCompleted()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa); // an old statement's own invoice
        MJdatabase.ResyncTripReportStatuses();
        string before = TrStatus(tr);
        MJdatabase.DeleteRecord("StatementsOfAccount", soa); // cancels the statement and its own invoice
        string after = TrStatus(tr);
        return before == TripReportStatus.Invoiced && after == TripReportStatus.Completed ? "" : $"before {before}, after cancelling the statement {after}";
    }

    [Check]
    public static string CancelStatement_KeepsATripWithItsOwnInvoiceInvoiced()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-9701", 1500m);
        MJdatabase.SaveInvoice(inv);
        int soa = Seed.Soa(status: "Unpaid", total: 1500m);
        Seed.SoaItem(soa, d);
        Exec("UPDATE Invoices SET SoaId=@s WHERE Id=@i", ("@s", soa), ("@i", inv.Id));
        MJdatabase.DeleteRecord("StatementsOfAccount", soa);
        long live = Long("SELECT COUNT(*) FROM Invoices WHERE Id=@i AND Status<>'Cancelled' AND COALESCE(SoaId,0)=0", ("@i", inv.Id));
        return live == 1 && TrStatus(tr) == TripReportStatus.Invoiced ? "" : $"invoice kept and freed: {live}, trip report {TrStatus(tr)}";
    }

    [Check]
    public static string OldStatement_IsFrozen_ButStillCollectable()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa); // its own whole-statement invoice
        var s = MJdatabase.GetStatementOfAccountById(soa)!;
        string why = MJdatabase.WhySoaCannotBeEdited(s);
        bool saveRefused = Refused(() => MJdatabase.SaveStatementOfAccount(s, new List<StatementOfAccountItem>()), out _);
        bool collectable = MJdatabase.GetPayTargets().Any(t => t.SoaId == soa);
        return why.Contains("frozen") && saveRefused && collectable ? "" : $"why '{why}', save {(saveRefused ? "refused" : "ALLOWED")}, collectable {collectable}";
    }

    [Check]
    public static string StatementWithPerTripInvoices_IsNotFrozen()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        int inv = Seed.Invoice(d, 500m, soaId: soa); // the trip's own service invoice
        var s = MJdatabase.GetStatementOfAccountById(soa)!;
        return MJdatabase.WhySoaCannotBeEdited(s).Length == 0 ? "" : "a statement of per-trip invoices is frozen";
    }
}
