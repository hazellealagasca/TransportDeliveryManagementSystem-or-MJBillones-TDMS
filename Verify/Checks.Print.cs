using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
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
        // Nov 7: the columns of the company's statement sheet.
        var must = new[] { ">Trip<", ">Date store received<", ">Zone<", ">DR no.<", ">Destination<", ">Type of delivery<", ">Plate no.<", ">Tons<", ">No. of drops<", ">Amount<", ">Fuel surcharge<", ">Driver / Aide<" };
        var mustNot = new[] { "Total crates", "TRIP-XYZ", "S001", "Trip report #", "Freight tariff", "Driver / helper" };
        var missing = must.Where(m => !h.Contains(m)).ToList(); var extra = mustNot.Where(m => h.Contains(m)).ToList();
        return missing.Count == 0 && extra.Count == 0 ? "" : $"missing [{string.Join(", ", missing)}] extra [{string.Join(", ", extra)}]";
    }

    [Check]
    public static string SoaPrint_Normal_ColumnsInSheetOrder()
    {
        Fresh();
        var (soa, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var order = new[] { ">Trip<", ">Date store received<", ">Zone<", ">DR no.<", ">Destination<", ">Type of delivery<", ">Plate no.<", ">Tons<", ">No. of drops<", ">Amount<", ">Fuel surcharge<", ">Driver / Aide<" };
        int last = -1; var bad = new List<string>();
        foreach (var o in order) { int at = h.IndexOf(o); if (at < 0 || at < last) bad.Add(o); else last = at; }
        return bad.Count == 0 && h.Contains("class='grid sheet'") && h.Contains("th class='num red'") ? "" : $"out of order or missing: {string.Join(", ", bad)}; sheet class {h.Contains("class='grid sheet'")}; red fuel heading {h.Contains("th class='num red'")}";
    }

    [Check]
    public static string SoaPrint_DateCompleted_IsTripReportDate()
    {
        Fresh();
        string bad = "";
        string Print(string type, int deliveryId, int specialId)
        {
            var (soa, items) = PrintSample(type);
            items[0].DeliveryId = deliveryId; items[0].SpecialTripId = specialId; items[0].DateReceived = "2026-11-01"; // the saved line holds the dispatch date
            return Html(PrintForms.StatementOfAccount(soa, items));
        }
        // A dispatch with a finished trip report prints the report date, not the dispatch date saved on the line.
        int d1 = Seed.Delivery();
        int r1 = Seed.TripReport(d1);
        Exec("UPDATE TripReports SET ReportDate = '2026-11-05', Status = 'Completed' WHERE Id = @i", ("@i", r1));
        string h1 = Print(SoaTypes.Normal, d1, 0);
        if (!h1.Contains("11/05/2026") || h1.Contains("11/01/2026")) bad += "dispatch with report did not print the report date; ";
        // A draft report is not a completion: the saved line date stays.
        int d2 = Seed.Delivery();
        int r2 = Seed.TripReport(d2);
        Exec("UPDATE TripReports SET ReportDate = '2026-11-06', Status = 'Draft' WHERE Id = @i", ("@i", r2));
        string h2 = Print(SoaTypes.Normal, d2, 0);
        if (h2.Contains("11/06/2026") || !h2.Contains("11/01/2026")) bad += "draft report was treated as completed; ";
        // No report at all: the saved line date.
        string h3 = Print(SoaTypes.Normal, Seed.Delivery(), 0);
        if (!h3.Contains("11/01/2026")) bad += "no report: saved date missing; ";
        // A special trip prints its trip report date.
        int s1 = Seed.SpecialTrip();
        int sr = Seed.TripReport(0, s1);
        Exec("UPDATE TripReports SET ReportDate = '2026-11-07', Status = 'Completed' WHERE Id = @i", ("@i", sr));
        string h4 = Print(SoaTypes.Special, 0, s1);
        if (!h4.Contains("11/07/2026") || !h4.Contains(">Date completed<")) bad += "special trip did not print its report date; ";
        // A special trip without a report prints the date it was marked completed.
        int s2 = Seed.SpecialTrip();
        Exec("UPDATE SpecialTrips SET CompletedAt = '2026-11-04 15:30:00' WHERE Id = @i", ("@i", s2));
        string h5 = Print(SoaTypes.Special, 0, s2);
        if (!h5.Contains("11/04/2026")) bad += "special trip without report did not print CompletedAt; ";
        return bad;
    }

    [Check]
    public static string SoaPrint_Special_OwnLayout()
    {
        Fresh();
        var (soa, items) = PrintSample(SoaTypes.Special);
        var def = PrintForms.StatementOfAccount(soa, items);
        string h = Html(def);
        return def.Key == "SOA_Special" && h.Contains("Trip charge") && h.Contains("XYZ") && !h.Contains("DR no.") ? "" : $"key {def.Key}; trip charge {h.Contains("Trip charge")}; ref {h.Contains("XYZ")}; dr {h.Contains("DR no.")}";
    }

    [Check]
    public static string SoaPrint_TotalsBlock_IsTheStatementSheet()
    {
        Fresh();
        // The company's own statement: freight 266,500.00 and fuel surcharge 65,424.00.
        var t = SoaTotals.From(266500m, 65424m);
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-4", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due };
        var (_, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var bad = new List<string>();
        if (t.TotalSales != 331924m || t.Vat != 35563.29m || t.NetOfVat != 296360.71m || t.Withholding != 5927.21m || t.Due != 325996.79m) bad.Add($"formula gave vat {t.Vat}, net {t.NetOfVat}, withholding {t.Withholding}, due {t.Due}");
        var order = new[] { "Total Sales + Fuel surcharge (VAT Inclusive)", "Less: VAT (12%)", "Amount: Net of VAT", "Less: Withholding Tax (2%)", "TOTAL AMOUNT DUE" };
        int last = h.IndexOf("<tfoot>"); if (last < 0) bad.Add("no totals block under the table");
        foreach (var o in order) { int at = h.IndexOf(">" + o + "<", Math.Max(0, last)); if (at < 0 || at < last) bad.Add("line missing or out of order: " + o); else last = at; }
        foreach (var n in new[] { "331,924.00", "35,563.29", "296,360.71", "65,424.00", "5,927.21", "325,996.79" }) if (!h.Contains(n)) bad.Add("figure missing: " + n);
        if (h.Contains("Expanded withholding tax")) bad.Add("the older five-line computation still prints");
        if (h.Contains("Add: VAT")) bad.Add("the old 'Add: VAT' line still prints");
        if (h.IndexOf(">Fuel surcharge<", Math.Max(0, h.IndexOf("<tfoot>")), StringComparison.Ordinal) >= 0 && h.IndexOf("<tfoot>") >= 0 && h.IndexOf(">Fuel surcharge<", h.IndexOf("<tfoot>"), StringComparison.Ordinal) >= 0) bad.Add("a separate Fuel surcharge line prints in the totals block");
        return string.Join("; ", bad);
    }

    [Check]
    public static string SoaPrint_StatementSavedUnderTheOlderRule_KeepsItsSavedFigures()
    {
        Fresh();
        // Saved before Nov 12 (fuel outside the VAT): freight 1,000.00, VAT 107.14, net 892.86, fuel 120.00, withholding 17.86, due 1,102.14. It prints as saved, never recomputed.
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-5", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = 1000m, VatAmount = 107.14m, NetOfVat = 892.86m, FuelSurchargeTotal = 120m, WithholdingTax = 17.86m, TotalAmount = 1102.14m };
        var (_, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var missing = new[] { "Add: VAT", "1,102.14", "892.86", "17.86" }.Where(m => !h.Contains(m)).ToList();
        return missing.Count == 0 ? "" : $"missing [{string.Join(", ", missing)}]";
    }

    [Check]
    public static string SoaTotals_Formula_AndExactSharing()
    {
        var t = SoaTotals.From(266500m, 65424m);
        var bad = new List<string>();
        if (t.Due != t.NetOfVat + t.Vat - t.Withholding) bad.Add("due is not net + vat - withholding");
        if (t.Due != t.TotalSales - t.Withholding || t.TotalSales != 266500m + 65424m) bad.Add("due is not sales (freight + fuel) - withholding");
        // Odd centavos: the shares always add up to the statement's withholding exactly.
        foreach (var f in new[] { new[] { 23500m, 23500m, 34500m }, new[] { 10000.01m, 3333.33m, 7777.77m, 1.05m }, new[] { 999.99m }, new decimal[] { 0m, 0m } })
        {
            var whole = SoaTotals.From(f.Sum(), 0m).Withholding;
            var shares = SoaTotals.Share(whole, f);
            if (shares.Sum() != whole) bad.Add($"shares {string.Join("+", shares)} != {whole}");
            var wf = f.Select(x => x + 111.11m).ToArray(); // freight + fuel weights
            whole = SoaTotals.From(f.Sum(), wf.Sum() - f.Sum()).Withholding;
            shares = SoaTotals.Share(whole, wf);
            if (shares.Sum() != whole) bad.Add($"shares {string.Join("+", shares)} != {whole}");
        }
        return string.Join("; ", bad);
    }

    [Check]
    public static string Soa_Save_SharesWithholding_AndReleasesDroppedTrips()
    {
        Fresh(); RunBackfill();
        var trips = new List<(int Id, Invoice Inv)>();
        foreach (var (freight, fuel) in new[] { (23500m, 6031m), (23500m, 6031m), (34500m, 8260m) })
        {
            int d = Seed.Delivery(); Seed.TripReport(d);
            var inv = NewInvoice(d, freight + fuel); inv.FuelSurcharge = fuel;
            MJdatabase.SaveInvoice(inv);
            trips.Add((d, inv));
        }
        if (trips.Any(x => MJdatabase.GetInvoices().First(i => i.Id == x.Inv.Id).Deductions != 0m)) return "an invoice alone carries a withholding (the computation belongs to the statement)";
        StatementOfAccountItem Line(int n, (int Id, Invoice Inv) x) => new() { TripNumber = n, DrNumbers = "DR-" + n, Destination = "Store " + n, PlateNumber = "ABC-123", Tonnage = "6T", DropCount = 1, Amount = x.Inv.Freight, FuelSurcharge = x.Inv.FuelSurcharge, DeliveryId = x.Id };
        var all = SoaTotals.From(trips.Sum(x => x.Inv.Freight), trips.Sum(x => x.Inv.FuelSurcharge));
        var soa = new StatementOfAccount { SoaNumber = "SOA-T-6", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, PeriodCovered = "Nov 2026", TotalSalesVatInc = all.TotalSales, VatAmount = all.Vat, NetOfVat = all.NetOfVat, FuelSurchargeTotal = all.Fuel, WithholdingTax = all.Withholding, TotalAmount = all.Due, Status = PayStatus.Unpaid };
        int id = MJdatabase.SaveStatementOfAccount(soa, trips.Select((x, i) => Line(i + 1, x)).ToList());
        var invs = MJdatabase.GetInvoices().Where(i => trips.Any(x => x.Inv.Id == i.Id)).ToList();
        var bad = new List<string>();
        var saved = MJdatabase.GetStatementsOfAccount().First(s => s.Id == id);
        if (saved.TotalAmount != all.Due || saved.WithholdingTax != all.Withholding || saved.FuelSurchargeTotal != all.Fuel) bad.Add($"saved due {saved.TotalAmount} withholding {saved.WithholdingTax} fuel {saved.FuelSurchargeTotal}");
        if (invs.Sum(i => i.Deductions) != all.Withholding) bad.Add($"shares {invs.Sum(i => i.Deductions)} != withholding {all.Withholding}");
        if (invs.Sum(i => i.NetAmount) != all.Due) bad.Add($"invoices' net {invs.Sum(i => i.NetAmount)} != statement due {all.Due}");
        // Taking a trip off releases its invoice back to no share.
        soa.Id = id;
        soa.TripRemovalReason = "Client asked to bill it later"; // taking a trip off a saved statement needs a written reason (Nov 9)
        var two = trips.Take(2).ToList();
        var t2 = SoaTotals.From(two.Sum(x => x.Inv.Freight), two.Sum(x => x.Inv.FuelSurcharge));
        soa.TotalSalesVatInc = t2.TotalSales; soa.VatAmount = t2.Vat; soa.NetOfVat = t2.NetOfVat; soa.FuelSurchargeTotal = t2.Fuel; soa.WithholdingTax = t2.Withholding; soa.TotalAmount = t2.Due;
        MJdatabase.SaveStatementOfAccount(soa, two.Select((x, i) => Line(i + 1, x)).ToList());
        var dropped = MJdatabase.GetInvoices().First(i => i.Id == trips[2].Inv.Id);
        if (dropped.SoaId != 0 || dropped.Deductions != 0m || dropped.NetAmount != dropped.TotalAmount) bad.Add($"dropped invoice: soa {dropped.SoaId}, deductions {dropped.Deductions}, net {dropped.NetAmount}");
        var kept = MJdatabase.GetInvoices().Where(i => two.Any(x => x.Inv.Id == i.Id)).ToList();
        if (kept.Sum(i => i.NetAmount) != t2.Due) bad.Add($"kept invoices' net {kept.Sum(i => i.NetAmount)} != {t2.Due}");
        return string.Join("; ", bad);
    }
}
