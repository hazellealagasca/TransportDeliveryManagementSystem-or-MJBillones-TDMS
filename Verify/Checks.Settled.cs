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
