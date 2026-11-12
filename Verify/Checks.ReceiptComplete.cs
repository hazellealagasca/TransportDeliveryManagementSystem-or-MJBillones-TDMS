using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 10: the receipt is issued only for the amount the person checked; it then marks the invoice, the statement and the trip reports as billed and paid.</summary>
    [Check]
    public static string ReceiptComplete_VerifiedAmount_FinalizesInvoiceStatementAndReports()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int b = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m), (b, 4000m, 200m));
        var inv = MJdatabase.SaveInvoiceForSoa(soaId, "SI-9301");
        foreach (var d in new[] { a, b })
            if (Str("SELECT Status FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", d)) != "Invoiced") return $"the trip report of dispatch {d} did not turn Invoiced with the statement's invoice";

        var target = MJdatabase.GetPayTargets().FirstOrDefault(p => p.SoaId == soaId);
        if (target == null) return "the invoiced statement is not collectable";
        if (target.Net != 10312.50m) return $"overall amount due {target.Net}, expected 10312.50";

        // an amount that was not the one checked is refused and nothing is saved
        if (!Refused(() => MJdatabase.SaveOneReceipt("4417", target, "Cash", "", "", DateTime.Today, false, verifiedAmount: 10000m), out _)) return "a receipt was issued for an amount that was not the one checked";
        if (Convert.ToInt64(Scalar("SELECT COUNT(*) FROM CollectionReceipts WHERE COALESCE(IsVoid,0) = 0") ?? 0) != 0) return "a refused receipt left a row behind";

        var rows = MJdatabase.SaveOneReceipt("4417", target, "Cash", "", "", DateTime.Today, false, verifiedAmount: target.Net);
        if (rows.Count != 1 || rows[0].AmountPaid != 10312.50m) return $"receipt rows {rows.Count}, amount {(rows.Count > 0 ? rows[0].AmountPaid : 0)}";
        if (Str("SELECT Status FROM Invoices WHERE Id = @i", ("@i", inv.Id)) != "Paid") return "the invoice is not Paid after the receipt";
        if (Str("SELECT Status FROM StatementsOfAccount WHERE Id = @s", ("@s", soaId)) != "Paid") return "the statement is not Paid after the receipt";
        foreach (var d in new[] { a, b })
            if (Str("SELECT Status FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", d)) != "Invoiced") return $"the trip report of dispatch {d} is not billed after the receipt";
        if (MJdatabase.GetPayTargets().Any(p => p.SoaId == soaId)) return "a paid statement is still collectable";
        return "";
    }
}
