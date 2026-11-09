using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    private static StatementOfAccountItem SoaLine(int deliveryId, decimal amount, decimal fuel) => new StatementOfAccountItem
    { TripNumber = 1, DateReceived = DateTime.Today.ToString("yyyy-MM-dd"), Destination = "Store", DropCount = 1, Amount = amount, FuelSurcharge = fuel, DeliveryId = deliveryId };

    /// <summary>Nov 9 (statement first): trip report -> statement (no invoice yet) -> invoice with the statement's locked amount -> collectable.</summary>
    [Check]
    public static string StatementFirst_TripReportToStatementToInvoiceToCollection()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int b = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b);
        int x = Seed.Delivery(client: "CLIENT B"); Seed.TripReport(x);
        var q = MJdatabase.GetTripReportsWaitingForStatement();
        if (q.Count != 3) return $"queue should list the 3 completed trip reports, has {q.Count}";
        if (MJdatabase.WhySelectionNotOneClient(q) == null) return "ticking two clients was NOT refused";
        if (MJdatabase.WhySelectionNotOneClient(q.Where(c => c.ClientName == "CLIENT A").ToList()) != null) return "one client was refused";

        // a statement for CLIENT A with its two trips, no invoice yet
        var t = SoaTotals.From(3000m, 100m);
        var soa = new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "2026-11-01 to 2026-11-09", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due, SoaType = SoaTypes.Normal };
        int soaId = MJdatabase.SaveStatementOfAccount(soa, new List<StatementOfAccountItem> { SoaLine(a, 2000m, 100m), SoaLine(b, 1000m, 0m) });
        if (MJdatabase.GetTripReportsWaitingForStatement().Any(c => c.DeliveryId == a || c.DeliveryId == b)) return "the trips are still in the queue after the statement was saved";
        if (MJdatabase.GetTripReportsWaitingForStatement().Count != 1) return "the other client's trip report left the queue";
        if (!MJdatabase.GetSoaTripsWithoutInvoice().TryGetValue(soaId, out int missing) || missing != 2) return "the statement should have 2 trips without an invoice";

        // a trip of another client can't be added
        bool wrongClient = Refused(() => MJdatabase.SaveStatementOfAccount(new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "x", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = 1000, TotalAmount = 1000, SoaType = SoaTypes.Normal },
            new List<StatementOfAccountItem> { SoaLine(x, 1000m, 0m) }), out _);
        if (!wrongClient) return "another client's trip was accepted on a statement";

        // not collectable while a trip has no invoice
        if (MJdatabase.GetPayTargets().Any(p => p.SoaId == soaId)) return "a half-invoiced statement is collectable";

        // the invoice must equal the statement line
        var line = MJdatabase.GetStatementLineForTrip(a);
        if (line == null || line.Freight != 2000m || line.Fuel != 100m) return $"statement line for trip A: {(line == null ? "none" : line.Freight + "/" + line.Fuel)}";
        var wrong = InvoiceWithNumber(a, "SI-9101", 2500m); wrong.FuelSurcharge = 100m; MJdatabase.SaveInvoice(wrong);
        bool lockedRefused = Refused(() => MJdatabase.ApplyStatementToNewInvoice(wrong.Id, a), out _);
        if (!lockedRefused) return "an invoice with a different amount was put on the statement";
        MJdatabase.DeleteRecord("Invoices", wrong.Id);

        var inv1 = InvoiceWithNumber(a, "SI-9102", 2100m); inv1.FuelSurcharge = 100m; MJdatabase.SaveInvoice(inv1); MJdatabase.ApplyStatementToNewInvoice(inv1.Id, a);
        var inv2 = InvoiceWithNumber(b, "SI-9103", 1000m); inv2.FuelSurcharge = 0m; MJdatabase.SaveInvoice(inv2); MJdatabase.ApplyStatementToNewInvoice(inv2.Id, b);
        decimal net = Convert.ToDecimal(Scalar("SELECT SUM(NetAmount) FROM Invoices WHERE SoaId=@s AND Status<>'Cancelled'", ("@s", soaId)) ?? 0);
        if (net != t.Due) return $"the invoices' net amounts add up to {net}, the statement's total due is {t.Due}";
        if (MJdatabase.GetSoaTripsWithoutInvoice().ContainsKey(soaId)) return "the statement still shows trips without an invoice";
        if (!MJdatabase.GetPayTargets().Any(p => p.SoaId == soaId && p.Invoices.Count == 2)) return "the fully invoiced statement is not collectable";
        return "";
    }

    /// <summary>An unpaid invoice made before the flow (on no statement): the statement line's amounts replace it and it joins the statement.</summary>
    [Check]
    public static string StatementFirst_OldUnpaidInvoice_TakesTheStatementLinesAmounts()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        var inv = InvoiceWithNumber(a, "SI-9201", 5000m); inv.FuelSurcharge = 500m; MJdatabase.SaveInvoice(inv);
        if (!MJdatabase.IsPreStatementInvoice(a)) return "the old unpaid invoice is not seen as pre-statement";
        var t = SoaTotals.From(3000m, 200m);
        var soa = new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "x", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due, SoaType = SoaTypes.Normal };
        int soaId = MJdatabase.SaveStatementOfAccount(soa, new List<StatementOfAccountItem> { SoaLine(a, 3000m, 200m) });
        decimal total = Convert.ToDecimal(Scalar("SELECT TotalAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        decimal net = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        long onSoa = Convert.ToInt64(Scalar("SELECT COALESCE(SoaId,0) FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        if (total != 3200m || net != t.Due || onSoa != soaId) return $"invoice total {total}, net {net} (due {t.Due}), soa {onSoa} (expected {soaId})";
        // the old amounts are kept and audited
        long hist = Convert.ToInt64(Scalar("SELECT COUNT(*) FROM InvoiceAmountHistory WHERE InvoiceId=@i AND OldTotal=5000 AND OldFuel=500 AND RevertedAt IS NULL", ("@i", inv.Id)) ?? 0);
        long audit = Convert.ToInt64(Scalar("SELECT COUNT(*) FROM AuditTrail WHERE TableName='Invoices' AND RecordId=@i", ("@i", inv.Id)) ?? 0);
        if (hist != 1 || audit < 1) return $"history rows {hist}, audit lines {audit} (1 and 1+ expected)";
        // cancelling the statement puts the old amounts back
        MJdatabase.CancelStatementOfAccount(soaId, "Client did not agree the amounts");
        decimal back = Convert.ToDecimal(Scalar("SELECT TotalAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        decimal backFuel = Convert.ToDecimal(Scalar("SELECT FuelSurcharge FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        decimal backNet = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        if (back != 5000m || backFuel != 500m || backNet != 5000m) return $"after cancel the invoice is {back} / fuel {backFuel} / net {backNet} (5000 / 500 / 5000 expected)";
        // a typo amount is refused
        int b2 = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b2);
        bool tiny = Refused(() => MJdatabase.SaveStatementOfAccount(new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "x", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = 0.01m, TotalAmount = 0.01m, SoaType = SoaTypes.Normal },
            new List<StatementOfAccountItem> { SoaLine(b2, 0.01m, 0m) }), out _);
        if (!tiny) return "an amount of 0.01 was accepted";
        return "";
    }

    /// <summary>Taking a trip off a saved statement: reason required, its invoice goes back, the rest re-share the withholding and still add up.</summary>
    [Check]
    public static string StatementFirst_TakeATripOffASavedStatement()
    {
        Fresh(); RunBackfill();
        var d = new int[3]; var inv = new Invoice[3];
        for (int i = 0; i < 3; i++) { d[i] = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d[i]); }
        // old unpaid invoices made before the flow
        for (int i = 0; i < 3; i++) { inv[i] = InvoiceWithNumber(d[i], "SI-97" + i, 4000m); MJdatabase.SaveInvoice(inv[i]); }
        var t3 = SoaTotals.From(3000m, 0m);
        var soa = new StatementOfAccount { ClientName = "CLIENT A", PeriodCovered = "x", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = t3.TotalSales, VatAmount = t3.Vat, NetOfVat = t3.NetOfVat, FuelSurchargeTotal = t3.Fuel, WithholdingTax = t3.Withholding, TotalAmount = t3.Due, SoaType = SoaTypes.Normal };
        int soaId = MJdatabase.SaveStatementOfAccount(soa, new List<StatementOfAccountItem> { SoaLine(d[0], 1000m, 0m), SoaLine(d[1], 1000m, 0m), SoaLine(d[2], 1000m, 0m) });
        soa.Id = soaId;
        var t2 = SoaTotals.From(2000m, 0m);
        soa.TotalSalesVatInc = t2.TotalSales; soa.VatAmount = t2.Vat; soa.NetOfVat = t2.NetOfVat; soa.FuelSurchargeTotal = t2.Fuel; soa.WithholdingTax = t2.Withholding; soa.TotalAmount = t2.Due;
        var two = new List<StatementOfAccountItem> { SoaLine(d[0], 1000m, 0m), SoaLine(d[1], 1000m, 0m) };
        soa.TripRemovalReason = "";
        if (!Refused(() => MJdatabase.SaveStatementOfAccount(soa, two), out _)) return "a trip was taken off without a reason";
        soa.TripRemovalReason = "Client asked to bill it later";
        MJdatabase.SaveStatementOfAccount(soa, two);
        decimal net = Convert.ToDecimal(Scalar("SELECT SUM(NetAmount) FROM Invoices WHERE SoaId=@s AND Status<>'Cancelled'", ("@s", soaId)) ?? 0);
        if (net != t2.Due) return $"the remaining invoices add up to {net}, the statement's total due is {t2.Due}";
        decimal back = Convert.ToDecimal(Scalar("SELECT TotalAmount FROM Invoices WHERE Id=@i", ("@i", inv[2].Id)) ?? 0);
        long onSoa = Convert.ToInt64(Scalar("SELECT COALESCE(SoaId,0) FROM Invoices WHERE Id=@i", ("@i", inv[2].Id)) ?? 0);
        decimal backNet = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv[2].Id)) ?? 0);
        if (back != 4000m || onSoa != 0 || backNet != 4000m) return $"the dropped invoice is {back} / net {backNet} / on statement {onSoa} (4000 / 4000 / 0 expected)";
        if (!MJdatabase.GetTripReportsWaitingForStatement().Any(c => c.DeliveryId == d[2])) return "the dropped trip's report did not return to the queue";
        long audit = Convert.ToInt64(Scalar("SELECT COUNT(*) FROM AuditTrail WHERE TableName='Invoices' AND RecordId=@i AND FieldName='Taken off statement'", ("@i", inv[2].Id)) ?? 0);
        if (audit != 1) return $"audit lines for the removal: {audit}";
        return "";
    }
}
