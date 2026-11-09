using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    private static int SaveSoaFor(string client, params (int Del, decimal Amount, decimal Fuel)[] lines)
    {
        var t = SoaTotals.From(lines.Sum(l => l.Amount), lines.Sum(l => l.Fuel));
        var soa = new StatementOfAccount { ClientName = client, PeriodCovered = "2026-11-01 to 2026-11-09", IssueDate = DateTime.Today, DeliveryType = "REF", TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due, SoaType = SoaTypes.Normal };
        return MJdatabase.SaveStatementOfAccount(soa, lines.Select(l => SoaLine(l.Del, l.Amount, l.Fuel)).ToList());
    }

    /// <summary>Nov 10: New invoice bills a whole statement: 10,000 freight + 500 fuel gives 10,321.43 due, one invoice, collectable, billed once.</summary>
    [Check]
    public static string SoaInvoice_OneInvoiceBillsTheWholeStatement()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int b = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m), (b, 4000m, 200m));
        var waiting = MJdatabase.GetSoasWaitingForOwnInvoice();
        var pick = waiting.FirstOrDefault(s => s.SoaId == soaId);
        if (pick == null) return "a saved statement is not offered for invoicing";
        if (pick.Totals.TotalSales != 10000m || pick.Totals.Fuel != 500m || pick.Totals.Due != 10321.43m) return $"statement figures: sales {pick.Totals.TotalSales}, fuel {pick.Totals.Fuel}, due {pick.Totals.Due} (expected 10000 / 500 / 10321.43)";
        if (pick.Trips != 2) return $"trips on the statement: {pick.Trips}";

        var inv = MJdatabase.SaveInvoiceForSoa(soaId, "SI-9201");
        if (inv.SoaId != soaId || inv.DeliveryId != 0) return "the invoice is not the statement's own invoice";
        if (inv.TotalAmount != 10500m || inv.FuelSurcharge != 500m) return $"invoice total {inv.TotalAmount} / fuel {inv.FuelSurcharge}";
        if (inv.NetAmount != 10321.43m) return $"invoice net {inv.NetAmount}, the statement's total due is 10321.43";
        decimal stored = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        if (stored != 10321.43m) return $"stored net {stored}";
        if (!string.Equals(Convert.ToString(Scalar("SELECT SalesInvoiceNo FROM StatementsOfAccount WHERE Id=@s", ("@s", soaId))), "SI-9201", StringComparison.Ordinal)) return "the statement does not show its SI No.";
        if (MJdatabase.GetSoasWaitingForOwnInvoice().Any(s => s.SoaId == soaId)) return "an invoiced statement is still offered";
        if (MJdatabase.GetSoaTripsWithoutInvoice().ContainsKey(soaId)) return "an invoiced statement still shows trips without an invoice";
        if (!MJdatabase.GetPayTargets().Any(p => p.SoaId == soaId)) return "the invoiced statement is not collectable";
        return "";
    }

    /// <summary>A statement is billed once, and its trips can't be billed again one by one.</summary>
    [Check]
    public static string SoaInvoice_BilledOnce_AndItsTripsAreBilled()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 2000m, 0m));
        MJdatabase.SaveInvoiceForSoa(soaId, "SI-9211");
        if (!Refused(() => MJdatabase.SaveInvoiceForSoa(soaId, "SI-9212"), out _)) return "a statement got a second invoice";
        if (!Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-9213", 2000m)), out _)) return "a trip on an invoiced statement was invoiced again one by one";
        if (!Refused(() => MJdatabase.SaveInvoiceForSoa(soaId, "SI-9211"), out _)) return "a repeated SI No. was accepted";
        return "";
    }

    /// <summary>A trip already invoiced one by one keeps its statement from being billed whole; a cancelled invoice frees the statement.</summary>
    [Check]
    public static string SoaInvoice_NotOfferedWhenTripInvoiced_AndFreedWhenCancelled()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 2000m, 0m));
        var perTrip = InvoiceWithNumber(a, "SI-9221", 2000m); MJdatabase.SaveInvoice(perTrip); MJdatabase.ApplyStatementToNewInvoice(perTrip.Id, a);
        if (MJdatabase.WhySoaCannotBeInvoiced(soaId) == null) return "a statement whose trip has its own invoice can still be invoiced whole";
        if (MJdatabase.GetSoasWaitingForOwnInvoice().Any(s => s.SoaId == soaId)) return "it is still offered";
        return "";
    }

    /// <summary>A statement with no amount, a cancelled statement and a paid one are not offered.</summary>
    [Check]
    public static string SoaInvoice_CancelledOrEmptyStatementIsRefused()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 2000m, 0m));
        Scalar("UPDATE StatementsOfAccount SET Status='Cancelled' WHERE Id=@s", ("@s", soaId));
        if (MJdatabase.WhySoaCannotBeInvoiced(soaId) == null) return "a cancelled statement can be invoiced";
        if (!Refused(() => MJdatabase.SaveInvoiceForSoa(soaId, "SI-9231"), out _)) return "a cancelled statement was invoiced";
        if (MJdatabase.GetSoasWaitingForOwnInvoice().Any(s => s.SoaId == soaId)) return "a cancelled statement is offered";
        return "";
    }
}
