using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 3: one collection receipt settles the whole statement in one transaction; voiding it undoes all of it.</summary>
public static partial class Checks
{
    /// <summary>An Unpaid SOA with three Unpaid invoices of 100, 200 and 300.</summary>
    private static (int Soa, int[] Invoices) SoaWithThreeInvoices(int specialTripId = 0)
    {
        int soa = Seed.Soa(status: PayStatus.Unpaid, total: 600m);
        var ids = new[] { 100m, 200m, 300m }.Select(a => specialTripId > 0
            ? Seed.Invoice(0, a, PayStatus.Unpaid, soaId: soa, specialTripId: specialTripId)
            : Seed.Invoice(Seed.Delivery(), a, PayStatus.Unpaid, soaId: soa)).ToArray();
        return (soa, ids);
    }
    private static PayTarget TargetFor(int soaId) => MJdatabase.GetPayTargets().First(t => t.SoaId == soaId);

    [Check]
    public static string Settle_Soa_AllPaidInOneGo()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = SoaWithThreeInvoices();
        var rows = MJdatabase.SaveOneReceipt("0001", TargetFor(soa), "Cash", "", "");
        long completed = Long("SELECT COUNT(*) FROM CollectionReceipts WHERE ReceiptNumber='0001' AND Status='Completed' AND COALESCE(IsVoid,0)=0");
        bool invPaid = ids.All(i => InvStatus(i) == PayStatus.Paid);
        decimal amount = rows.Sum(r => r.AmountPaid);
        return SoaStatus(soa) == PayStatus.Paid && invPaid && completed == 3 && amount == 600m
            ? "" : $"soa {SoaStatus(soa)}, invoices paid {invPaid}, completed rows {completed}, amount {amount}";
    }

    [Check]
    public static string Settle_RollsBackOnChangedInvoice()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = SoaWithThreeInvoices();
        var target = TargetFor(soa);
        Exec("UPDATE Invoices SET NetAmount = 250, TotalAmount = 250 WHERE Id=@i", ("@i", ids[2])); // changed after the screen loaded it
        try { MJdatabase.SaveOneReceipt("0002", target, "Cash", "", ""); return "no error"; }
        catch (InvalidOperationException) { }
        long rows = Long("SELECT COUNT(*) FROM CollectionReceipts");
        return rows == 0 && SoaStatus(soa) == PayStatus.Unpaid && ids.All(i => InvStatus(i) == PayStatus.Unpaid) ? "" : $"rows {rows}, soa {SoaStatus(soa)}";
    }

    [Check]
    public static string Settle_RefusesPaidSoa()
    {
        Fresh(); RunBackfill();
        var (soa, _) = SoaWithThreeInvoices();
        var target = TargetFor(soa);
        Exec("UPDATE StatementsOfAccount SET Status='Paid' WHERE Id=@i", ("@i", soa));
        try { MJdatabase.SaveOneReceipt("0003", target, "Cash", "", ""); return "no error"; }
        catch (InvalidOperationException) { return Long("SELECT COUNT(*) FROM CollectionReceipts") == 0 ? "" : "rows written"; }
    }

    [Check]
    public static string VoidReceipt_RevertsSoaAndInvoices()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = SoaWithThreeInvoices();
        var rows = MJdatabase.SaveOneReceipt("0004", TargetFor(soa), "Cash", "", "");
        MJdatabase.DeleteRecord("CollectionReceipts", rows[0].Id);
        long live = Long("SELECT COUNT(*) FROM CollectionReceipts WHERE ReceiptNumber='0004' AND COALESCE(IsVoid,0)=0");
        bool unpaid = ids.All(i => InvStatus(i) == PayStatus.Unpaid);
        return live == 0 && unpaid && SoaStatus(soa) == PayStatus.Unpaid ? "" : $"live rows {live}, invoices unpaid {unpaid}, soa {SoaStatus(soa)}";
    }

    [Check]
    public static string PayTargetKind_NormalAndSpecial()
    {
        Fresh(); RunBackfill();
        var (normal, _) = SoaWithThreeInvoices();
        var (special, _) = SoaWithThreeInvoices(Seed.SpecialTrip());
        string a = MJdatabase.PayTargetKind(TargetFor(normal)), b = MJdatabase.PayTargetKind(TargetFor(special));
        return a == SoaTypes.Normal && b == SoaTypes.Special ? "" : $"{a}/{b}";
    }
}
