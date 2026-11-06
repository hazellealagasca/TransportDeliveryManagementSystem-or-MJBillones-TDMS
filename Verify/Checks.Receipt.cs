using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 7: the Official Collection Receipt slip (normal and special), with no balance.</summary>
public static partial class Checks
{
    [Check]
    public static string ReceiptPrint_Normal_NoBalance()
    {
        Fresh(); RunBackfill();
        var (soa, _) = SoaWithThreeInvoices();
        var rows = MJdatabase.SaveOneReceipt("0101", TargetFor(soa), "Cash", "", "");
        var def = PrintForms.CollectionReceipt(rows[0]);
        string h = Html(def);
        bool balance = h.Contains("Balance", StringComparison.OrdinalIgnoreCase) || h.Contains("Remaining", StringComparison.OrdinalIgnoreCase);
        bool hasBalanceChoice = def.Defaults.Sections.Any(x => x.Key == "balance") || def.Defaults.Columns.Any(x => x.Key == "bal");
        return def.Key == "OfficialReceipt" && !balance && !hasBalanceChoice && h.Contains("600.00") ? "" : $"key {def.Key}, balance text {balance}, balance option {hasBalanceChoice}, amount {h.Contains("600.00")}";
    }

    [Check]
    public static string ReceiptPrint_Special_OwnSlip()
    {
        Fresh(); RunBackfill();
        var (soa, _) = SoaWithThreeInvoices(Seed.SpecialTrip());
        Exec("UPDATE StatementsOfAccount SET SoaType='Special' WHERE Id=@i", ("@i", soa));
        var rows = MJdatabase.SaveOneReceipt("0102", TargetFor(soa), "Cash", "", "");
        var def = PrintForms.CollectionReceipt(rows[0]);
        string h = Html(def);
        return def.Key == "OfficialReceiptSpecial" && h.Contains("Special trip", StringComparison.OrdinalIgnoreCase) && !h.Contains("Balance", StringComparison.OrdinalIgnoreCase) ? "" : $"key {def.Key}, special {h.Contains("Special trip", StringComparison.OrdinalIgnoreCase)}";
    }
}
