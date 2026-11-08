using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 7: the service invoice has an amount and a fuel surcharge and no deductions; the statement reads both from it.</summary>
public static partial class Checks
{
    [Check]
    public static string Invoice_FuelSaved_NoDeductions_TripReportNumberRead()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var inv = NewInvoice(d, 29531m);
        inv.FuelSurcharge = 6031m;
        inv.Deductions = 500m;       // a caller that still sends a deduction ...
        inv.NetAmount = 29031m;      // ... gets none: the invoice is always the whole amount
        MJdatabase.SaveInvoice(inv);
        var saved = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == inv.Id);
        if (saved == null) return "invoice not found";
        var bad = new List<string>();
        if (saved.TotalAmount != 29531m) bad.Add($"total {saved.TotalAmount}");
        if (saved.FuelSurcharge != 6031m) bad.Add($"fuel {saved.FuelSurcharge}");
        if (saved.Freight != 23500m) bad.Add($"freight {saved.Freight}");
        if (saved.Deductions != 0m) bad.Add($"deductions {saved.Deductions}");
        if (saved.NetAmount != 29531m) bad.Add($"net {saved.NetAmount}");
        if (string.IsNullOrWhiteSpace(saved.TripReportNumber)) bad.Add("trip report number blank");
        var ti = MJdatabase.GetTripInvoices().FirstOrDefault(i => i.Id == inv.Id);
        if (ti == null || ti.Freight != 23500m || ti.Fuel != 6031m) bad.Add("trip invoice freight/fuel");
        return string.Join("; ", bad);
    }

    [Check]
    public static string SoaPrint_AmountAndFuel_AreTheInvoices()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soaId = Seed.Soa();
        var inv = NewInvoice(d, 29531m);
        inv.FuelSurcharge = 6031m; inv.SoaId = soaId;
        MJdatabase.SaveInvoice(inv);
        var soa = new StatementOfAccount { Id = soaId, SoaNumber = "SOA-T-2", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = 29531m, VatAmount = 3164.04m, NetOfVat = 26366.96m, WithholdingTax = 0m, TotalAmount = 29531m };
        // The saved line still holds an old, wrong amount (like SOA-2026-0006's 4,654 against a 45,000 invoice).
        var items = new List<StatementOfAccountItem> { new() { TripNumber = 1, DrNumbers = "DR-1", Destination = "Store One", PlateNumber = "ABC-123", Tonnage = "6T", DropCount = 1, Amount = 4654m, FuelSurcharge = 0m, DeliveryId = d } };
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var bad = new List<string>();
        if (!h.Contains("23,500.00")) bad.Add("amount 23,500.00 missing");
        if (!h.Contains("6,031.00")) bad.Add("fuel 6,031.00 missing");
        if (h.Contains("4,654")) bad.Add("the old line amount still prints");
        return string.Join("; ", bad);
    }

    [Check]
    public static string SoaPrint_ComputationLinesAddUp()
    {
        Fresh();
        // A statement made from invoices: no withholding saved. The printed lines must add up to the total due.
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-3", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = 45000m, VatAmount = 4821.43m, NetOfVat = 40178.57m, WithholdingTax = 0m, TotalAmount = 45000m };
        var (_, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        return !h.Contains("803.57") && h.Contains("45,000.00") ? "" : $"an invented 2% withholding printed: {h.Contains("803.57")}; total 45,000.00 printed: {h.Contains("45,000.00")}";
    }

    [Check]
    public static string BillingScreen_NoDeductions_ListShowsTripReportNo()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (BillingView)Activator.CreateInstance(typeof(BillingView), nonPublic: true)!;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var grid = (DataGridView?)typeof(BillingView).GetField("gridInvoices", F)!.GetValue(view);
            if (grid == null) return "invoice list not created";
            var heads = grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).ToList();
            var bad = new List<string>();
            if (heads.Count < 3 || heads[0] != "Invoice #" || heads[1] != "Trip report #" || heads[2] != "Dispatch #") bad.Add("first columns are " + string.Join(" | ", heads.Take(3)));
            if (heads.Any(h => h.Contains("Schedule", StringComparison.OrdinalIgnoreCase) || h.Contains("Deduction", StringComparison.OrdinalIgnoreCase))) bad.Add("schedule/deduction column still there");
            // No control on the invoice form may talk about deductions.
            var texts = new List<string>();
            void Walk(Control c) { foreach (Control k in c.Controls) { if (k is Label or Button or CheckBox) texts.Add(k.Text); Walk(k); } }
            Walk(view);
            if (texts.Any(t => t.Contains("Deduction", StringComparison.OrdinalIgnoreCase))) bad.Add("a label still says deductions: " + texts.First(t => t.Contains("Deduction", StringComparison.OrdinalIgnoreCase)));
            if (!texts.Any(t => t.StartsWith("Fuel surcharge", StringComparison.OrdinalIgnoreCase))) bad.Add("no fuel surcharge label");
            // Nov 7: the invoice shows only what was typed: no total, net due, VAT or withholding anywhere on its form.
            var computed = texts.FirstOrDefault(t => t.Contains("Total due", StringComparison.OrdinalIgnoreCase) || t.Contains("Net due", StringComparison.OrdinalIgnoreCase) || t.Contains("withholding", StringComparison.OrdinalIgnoreCase) || t.Contains("VAT", StringComparison.Ordinal));
            if (computed != null) bad.Add("the invoice form shows a computed figure: " + computed);
            if (!heads.Contains("Fuel surcharge")) bad.Add("the invoice list has no Fuel surcharge column");
            view.Dispose();
            return string.Join("; ", bad);
        });
    }
}
