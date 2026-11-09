using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>
/// Findings F1 and F3 of Docs/2026-11-09-order-to-receipt-process-and-findings.md:
/// F1 - cancelling a statement must give its invoices back their full amount (no leftover withholding share);
/// F3 - a trip report whose trip has a live invoice can be neither voided nor edited, whichever screen asks.
/// </summary>
public static partial class Checks
{
    // ------------------------------------------------------------------ F1

    /// <summary>A real statement of <paramref name="freights"/> (one trip and invoice each), saved the normal way so the withholding is shared out.</summary>
    private static (int SoaId, List<int> InvoiceIds) RealStatement(params decimal[] freights)
    {
        var invoiceIds = new List<int>();
        var lines = new List<StatementOfAccountItem>();
        int n = 0;
        foreach (var freight in freights)
        {
            int d = Seed.Delivery(); Seed.TripReport(d);
            var inv = NewInvoice(d, freight);
            MJdatabase.SaveInvoice(inv);
            invoiceIds.Add(inv.Id);
            lines.Add(new StatementOfAccountItem { TripNumber = ++n, DrNumbers = "DR-" + n, Destination = "Store " + n, DeliveryId = d, Amount = freight });
        }
        var t = SoaTotals.From(freights.Sum(), 0m);
        var soa = new StatementOfAccount
        {
            SoaNumber = "SOA-T-F1", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, PeriodCovered = "Nov 2026",
            TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due, Status = PayStatus.Unpaid
        };
        int id = MJdatabase.SaveStatementOfAccount(soa, lines);
        return (id, invoiceIds);
    }

    [Check]
    public static string CancelStatement_GivesItsInvoicesBackTheirFullAmount()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = RealStatement(10000m, 20000m);
        var bad = new List<string>();
        foreach (int i in ids)
            if (Convert.ToDecimal(Scalar("SELECT Deductions FROM Invoices WHERE Id=@i", ("@i", i)) ?? 0) <= 0m) bad.Add($"setup: invoice {i} has no withholding share on the statement");
        if (bad.Count > 0) return string.Join("; ", bad);

        MJdatabase.CancelStatementOfAccount(soa, "Wrong client was picked");

        foreach (int i in ids)
        {
            decimal ded = Convert.ToDecimal(Scalar("SELECT Deductions FROM Invoices WHERE Id=@i", ("@i", i)) ?? 0);
            decimal total = Convert.ToDecimal(Scalar("SELECT TotalAmount FROM Invoices WHERE Id=@i", ("@i", i)) ?? 0);
            decimal net = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", i)) ?? 0);
            long onSoa = Long("SELECT COALESCE(SoaId,0) FROM Invoices WHERE Id=@i", ("@i", i));
            if (onSoa != 0 || ded != 0m || net != total) bad.Add($"invoice {i}: soa {onSoa}, deductions {ded}, net {net}, total {total}");
        }
        // What Collections would ask for on each freed invoice on its own.
        var targets = MJdatabase.GetPayTargets().Where(t => !t.IsSoa).ToList();
        if (targets.Sum(t => t.Net) != 30000m) bad.Add($"Collections would ask {targets.Sum(t => t.Net)} for 30000 of freight");
        return string.Join("; ", bad);
    }

    [Check]
    public static string DeleteStatement_GivesItsInvoicesBackTheirFullAmount()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = RealStatement(15000m);
        MJdatabase.DeleteRecord("StatementsOfAccount", soa); // the other way to cancel a statement
        decimal ded = Convert.ToDecimal(Scalar("SELECT Deductions FROM Invoices WHERE Id=@i", ("@i", ids[0])) ?? 0);
        decimal net = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", ids[0])) ?? 0);
        return ded == 0m && net == 15000m ? "" : $"deductions {ded}, net {net} (expected 0 and 15000)";
    }

    // ------------------------------------------------------------------ F3: voiding

    [Check]
    public static string VoidTripReport_IsRefusedWhileTheDispatchHasALiveInvoice()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9801", 1500m));
        bool refused = Refused(() => MJdatabase.DeleteRecord("TripReports", tr), out var why);
        long live = Long("SELECT COUNT(*) FROM TripReports WHERE Id=@i AND COALESCE(IsVoid,0)=0", ("@i", tr));
        return refused && live == 1 && why.Contains("billed") ? "" : $"void {(refused ? "refused" : "ALLOWED")}, live reports {live}, message '{why}'";
    }

    [Check]
    public static string VoidTripReport_IsRefusedWhileTheSpecialTripHasALiveInvoice()
    {
        Fresh(); RunBackfill();
        int st = Seed.SpecialTrip(); int tr = Seed.TripReport(0, specialTripId: st);
        int invId = Seed.Invoice(0, 5000m, specialTripId: st);
        Exec("UPDATE Invoices SET DeliveryId = NULL WHERE Id=@i", ("@i", invId)); // as SaveInvoice stores a special trip's invoice: no dispatch link
        string? block = MJdatabase.ValidateRecordDeletion("TripReports", tr);
        bool refused = Refused(() => MJdatabase.DeleteRecord("TripReports", tr), out _);
        long live = Long("SELECT COUNT(*) FROM TripReports WHERE Id=@i AND COALESCE(IsVoid,0)=0", ("@i", tr));
        return !string.IsNullOrEmpty(block) && refused && live == 1 ? "" : $"screen check '{block}', void {(refused ? "refused" : "ALLOWED")}, live reports {live}";
    }

    [Check]
    public static string VoidTripReport_IsRefusedWhileAnOldStatementsInvoiceBillsTheTrip()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.SoaItem(soa, d);
        Seed.Invoice(0, 500m, soaId: soa); // an old statement's own invoice bills the trip through the statement's lines
        bool refused = Refused(() => MJdatabase.DeleteRecord("TripReports", tr), out _);
        return refused ? "" : "the report was voided although an old statement's invoice bills its trip";
    }

    [Check]
    public static string VoidTripReport_StillWorksWhenTheTripIsNotBilled()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        bool ok = MJdatabase.DeleteRecord("TripReports", tr);
        long voided = Long("SELECT COUNT(*) FROM TripReports WHERE Id=@i AND COALESCE(IsVoid,0)=1", ("@i", tr));
        return ok && voided == 1 ? "" : $"returned {ok}, voided rows {voided}";
    }

    [Check]
    public static string VoidTripReport_WorksAgainOnceTheInvoiceIsCancelled()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-9803", 1500m);
        MJdatabase.SaveInvoice(inv);
        MJdatabase.DeleteRecord("Invoices", inv.Id); // cancels the invoice and frees the trip
        bool ok = MJdatabase.DeleteRecord("TripReports", tr);
        long voided = Long("SELECT COUNT(*) FROM TripReports WHERE Id=@i AND COALESCE(IsVoid,0)=1", ("@i", tr));
        return ok && voided == 1 ? "" : $"returned {ok}, voided rows {voided}";
    }

    // ------------------------------------------------------------------ F3: editing

    [Check]
    public static string EditTripReport_IsRefusedWhileTheDispatchHasALiveInvoice()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int trId = Seed.TripReport(d);
        MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9804", 1500m));
        var rep = MJdatabase.GetTripReports().First(t => t.Id == trId);
        rep.Remarks = "edited after billing"; rep.DeliveredGCrates = 1;
        bool refused = Refused(() => MJdatabase.SaveTripReport(rep), out var why);
        string remarks = Str("SELECT COALESCE(Remarks,'') FROM TripReports WHERE Id=@i", ("@i", trId));
        return refused && remarks != "edited after billing" && why.Contains("billed") ? "" : $"edit {(refused ? "refused" : "SAVED")}, remarks now '{remarks}', message '{why}'";
    }

    [Check]
    public static string EditTripReport_IsRefusedWhileTheSpecialTripHasALiveInvoice()
    {
        Fresh(); RunBackfill();
        int st = Seed.SpecialTrip(); int trId = Seed.TripReport(0, specialTripId: st);
        Seed.Invoice(0, 5000m, specialTripId: st);
        var rep = MJdatabase.GetTripReports().First(t => t.Id == trId);
        rep.Remarks = "edited after billing";
        bool refused = Refused(() => MJdatabase.SaveTripReport(rep), out _);
        string remarks = Str("SELECT COALESCE(Remarks,'') FROM TripReports WHERE Id=@i", ("@i", trId));
        return refused && remarks != "edited after billing" ? "" : $"edit {(refused ? "refused" : "SAVED")}, remarks now '{remarks}'";
    }

    [Check]
    public static string EditTripReport_StillWorksWhenTheTripIsNotBilled()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int trId = Seed.TripReport(d);
        var rep = MJdatabase.GetTripReports().First(t => t.Id == trId);
        rep.Remarks = "fixed a typo";
        MJdatabase.SaveTripReport(rep);
        string remarks = Str("SELECT COALESCE(Remarks,'') FROM TripReports WHERE Id=@i", ("@i", trId));
        return remarks == "fixed a typo" ? "" : $"remarks '{remarks}'";
    }

    [Check]
    public static string EditTripReport_WorksAgainOnceTheInvoiceIsCancelled()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int trId = Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-9805", 1500m);
        MJdatabase.SaveInvoice(inv);
        MJdatabase.DeleteRecord("Invoices", inv.Id);
        var rep = MJdatabase.GetTripReports().First(t => t.Id == trId);
        rep.Remarks = "corrected after the invoice was cancelled";
        MJdatabase.SaveTripReport(rep);
        string remarks = Str("SELECT COALESCE(Remarks,'') FROM TripReports WHERE Id=@i", ("@i", trId));
        return remarks == "corrected after the invoice was cancelled" ? "" : $"remarks '{remarks}'";
    }

    [Check]
    public static string WhyTripReportIsLocked_NamesTheInvoice_AndIsEmptyOtherwise()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); int ta = Seed.TripReport(a);
        int b = Seed.Delivery(); int tb = Seed.TripReport(b);
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-9806", 1500m));
        var billed = MJdatabase.GetTripReports().First(t => t.Id == ta);
        var free = MJdatabase.GetTripReports().First(t => t.Id == tb);
        string whyBilled = MJdatabase.WhyTripReportIsLocked(billed);
        string whyFree = MJdatabase.WhyTripReportIsLocked(free);
        string whyNew = MJdatabase.WhyTripReportIsLocked(new TripReport { DeliveryId = b });
        return whyBilled.Contains("SI-9806") && whyFree.Length == 0 && whyNew.Length == 0 ? "" : $"billed '{whyBilled}', unbilled '{whyFree}', new '{whyNew}'";
    }
}
