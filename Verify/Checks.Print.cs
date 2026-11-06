using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 6 / 7: the printed SOA and receipt layouts.</summary>
public static partial class Checks
{
    private static (StatementOfAccount Soa, List<StatementOfAccountItem> Items) PrintSample(string type)
    {
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-1", ClientName = "CLIENT A", SoaType = type, IssueDate = DateTime.Today, TotalSalesVatInc = 1120m, VatAmount = 120m, NetOfVat = 1000m, WithholdingTax = 20m, TotalAmount = 1100m };
        var items = new List<StatementOfAccountItem>
        {
            new() { TripNumber = 1, DrNumbers = "DR-1", Destination = "Store One", PlateNumber = "ABC-123", Tonnage = "6T", DropCount = 2, Amount = 1000m, FuelSurcharge = 120m,
                    DriverHelper = "Juan Dela Cruz / Pedro Santos", Zone = "ZONE-X", TotalCrates = 77, StoreCode = "S001", TripReportNumber = "TRIP-XYZ", DeliveryType = "REF", DateReceived = "2026-11-01",
                    DeliveryId = type == SoaTypes.Special ? 0 : 5, SpecialTripId = type == SoaTypes.Special ? 9 : 0 }
        };
        return (soa, items);
    }
    private static string Html(PrintFormDef def) => def.Body(def.Defaults);

    [Check]
    public static string SoaPrint_Normal_StrictColumns()
    {
        Fresh();
        var (soa, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var must = new[] { "DR no.", "Destination", "Plate no.", "Tons", "Drops", "Freight tariff", "Fuel surcharge", "Driver / helper" };
        var mustNot = new[] { "ZONE-X", "Total crates", "TRIP-XYZ", "S001", ">Zone<", "Trip report #" };
        var missing = must.Where(m => !h.Contains(m)).ToList(); var extra = mustNot.Where(m => h.Contains(m)).ToList();
        return missing.Count == 0 && extra.Count == 0 ? "" : $"missing [{string.Join(", ", missing)}] extra [{string.Join(", ", extra)}]";
    }

    [Check]
    public static string SoaPrint_Special_OwnLayout()
    {
        Fresh();
        var (soa, items) = PrintSample(SoaTypes.Special);
        var def = PrintForms.StatementOfAccount(soa, items);
        string h = Html(def);
        return def.Key == "SOA_Special" && h.Contains("Trip charge") && h.Contains("XYZ") && !h.Contains("DR no.") ? "" : $"key {def.Key}; trip charge {h.Contains("Trip charge")}; ref {h.Contains("XYZ")}; dr {h.Contains("DR no.")}; html: {System.Text.RegularExpressions.Regex.Replace(h.Substring(Math.Max(0, h.IndexOf("<table class='grid'"))), "\\s+", " ").Substring(0, 900)}";
    }

    [Check]
    public static string SoaPrint_ExactlyFiveTotals()
    {
        Fresh();
        string bad = "";
        foreach (var type in new[] { SoaTypes.Normal, SoaTypes.Special })
        {
            var (soa, items) = PrintSample(type);
            string h = Html(PrintForms.StatementOfAccount(soa, items));
            var must = new[] { "Total sales (VAT inclusive)", "VAT (12%)", "Net of VAT", "Expanded withholding tax (2%)", "Total amount due" };
            var mustNot = new[] { "Add: VAT", "Fuel surcharge (included" };
            var missing = must.Where(m => !h.Contains(m)).ToList(); var extra = mustNot.Where(m => h.Contains(m)).ToList();
            if (missing.Count + extra.Count > 0) bad += $"{type}: missing [{string.Join(", ", missing)}] extra [{string.Join(", ", extra)}] ";
        }
        return bad;
    }
}
