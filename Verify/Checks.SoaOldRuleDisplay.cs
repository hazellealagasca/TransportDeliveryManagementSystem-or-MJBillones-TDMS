using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// A statement saved before Nov 12 (fuel outside the VAT) keeps its saved figures and shows them in its old lines everywhere
/// (top strip, totals block, tape, checklist); a statement under the new rule shows the five new lines and never counts the fuel twice.
/// </summary>
public static partial class Checks
{
    private static readonly string[] OldSevenRows = { "Total Sales (VAT Inclusive)", "Less: VAT", "Amount: Net of VAT", "Fuel surcharge", "Add: VAT", "Less: Withholding Tax", "TOTAL AMOUNT DUE" };

    /// <summary>One trip of 30,000 freight + 1,000 fuel on a statement saved with the given figures.</summary>
    private static int SoaWithFigures(decimal sales, decimal vat, decimal net, decimal fuel, decimal wht, decimal due, string status)
    {
        int d = Seed.Delivery(client: "MORE THAN CAKES, INC."); Seed.TripReport(d);
        var inv = NewInvoice(d, 31000m, "MORE THAN CAKES, INC."); inv.FuelSurcharge = 1000m;
        MJdatabase.SaveInvoice(inv);
        var line = new StatementOfAccountItem { TripNumber = 1, DrNumbers = "DR-1", Destination = "Store 1", PlateNumber = "ABC-123", Tonnage = "6T", DropCount = 1, Amount = 30000m, FuelSurcharge = 1000m, DeliveryId = d };
        var n = SoaTotals.From(30000m, 1000m);   // saved the way the screen would; the figures are then written as the old rule saved them
        var soa = new StatementOfAccount { SoaNumber = "SOA-T-OLD" + d, ClientName = "MORE THAN CAKES, INC.", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, PeriodCovered = "Nov 2026", TotalSalesVatInc = n.TotalSales, VatAmount = n.Vat, NetOfVat = n.NetOfVat, FuelSurchargeTotal = n.Fuel, WithholdingTax = n.Withholding, TotalAmount = n.Due, Status = PayStatus.Unpaid };
        int id = MJdatabase.SaveStatementOfAccount(soa, new List<StatementOfAccountItem> { line });
        Exec("UPDATE StatementsOfAccount SET Status=@s, TotalSalesVatInc=@a, VatAmount=@b, NetOfVat=@c, FuelSurchargeTotal=@d, WithholdingTax=@e, TotalAmount=@f WHERE Id=@i", ("@s", status), ("@a", (double)sales), ("@b", (double)vat), ("@c", (double)net), ("@d", (double)fuel), ("@e", (double)wht), ("@f", (double)due), ("@i", id));
        return id;
    }

    [Check]
    public static string SoaOldRuleDisplay_Helper_TellsTheTwoRulesApart()
    {
        var bad = new List<string>();
        if (!SoaTotals.IsOldRule(30000m, 3214.29m, 26785.71m, 1000m, 535.71m, 30464.29m)) bad.Add("old figures not recognised");
        var n = SoaTotals.From(30000m, 1000m);
        if (SoaTotals.IsOldRule(n.TotalSales, n.Vat, n.NetOfVat, n.Fuel, n.Withholding, n.Due)) bad.Add("new figures called old");
        if (!SoaTotals.IsNewRule(n.TotalSales, n.Withholding, n.Due)) bad.Add("new figures not recognised");
        var o = SoaTotals.OldFrom(30000m, 1000m);
        if (o.Vat != 3214.29m || o.NetOfVat != 26785.71m || o.Withholding != 535.71m || o.Due != 30464.29m) bad.Add($"OldFrom {o}");
        return string.Join("; ", bad);
    }

    [Check]
    public static string SoaOldRuleDisplay_SavedOldStatement_ShowsItsOldLinesEverywhere()
    {
        Fresh(); RunBackfill();
        int old = SoaWithFigures(30000m, 3214.29m, 26785.71m, 1000m, 535.71m, 30464.29m, "Paid");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var bad = new List<string>();
            Open(view, old);
            Pump(900);
            bad.AddRange(CheckStrip(view, 31000m, 535.71m, 30464.29m));
            bad.AddRange(CheckPanelAndTape(view, true, new[] { "30,000.00", "3,214.29", "26,785.71", "1,000.00", "3,214.29", "535.71", "30,464.29" }));
            var checks = Checks_(view);
            var math = checks.FirstOrDefault(c => c.Text.StartsWith("Totals add up") || c.Text.StartsWith("Totals don't add up"));
            if (math == null || !math.Passed) bad.Add("Totals add up: " + (math?.Text ?? "missing"));
            foreach (var c in checks.Where(c => !c.Passed)) bad.Add("failing check: " + c.Text);
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SoaOldRuleDisplay_NewRuleStatement_FiveLines_FuelNotCountedTwice()
    {
        Fresh(); RunBackfill();
        var t = SoaTotals.From(30000m, 1000m);
        int id = SoaWithFigures(t.TotalSales, t.Vat, t.NetOfVat, t.Fuel, t.Withholding, t.Due, "Paid");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var bad = new List<string>();
            Open(view, id);
            Pump(900);
            if (t.Due != 30446.43m) bad.Add("formula due " + t.Due);
            bad.AddRange(CheckStrip(view, 31000m, t.Withholding, 30446.43m));
            bad.AddRange(CheckPanelAndTape(view, false, new[] { "31,000.00", t.Vat.ToString("N2"), t.NetOfVat.ToString("N2"), t.Withholding.ToString("N2"), "30,446.43" }));
            foreach (var c in Checks_(view).Where(c => !c.Passed)) bad.Add("failing check: " + c.Text);
            host.Close();
            return string.Join("; ", bad);
        });
    }

    private const BindingFlags SoaF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static void Open(StatementOfAccountView v, int id) => typeof(StatementOfAccountView).GetMethod("OpenEditStatement", SoaF)!.Invoke(v, new object[] { MJdatabase.GetStatementOfAccountById(id)! });
    private static List<PaymentCheck> Checks_(StatementOfAccountView v) => (List<PaymentCheck>)typeof(StatementOfAccountView).GetMethod("ComputeSoaChecks", SoaF)!.Invoke(v, null)!;
    private static string Money(decimal x) => "₱" + x.ToString("N2");

    private static List<string> CheckStrip(StatementOfAccountView v, decimal salesPlusFuel, decimal wht, decimal due)
    {
        var bad = new List<string>();
        var strip = (PaymentSummaryStrip)typeof(StatementOfAccountView).GetField("soaStrip", SoaF)!.GetValue(v)!;
        decimal G(string n) => (decimal)typeof(PaymentSummaryStrip).GetField(n, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(strip)!;
        decimal? ed = (decimal?)typeof(PaymentSummaryStrip).GetField("explicitDue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(strip);
        if (G("gross") != salesPlusFuel) bad.Add($"strip sales+fuel {G("gross")} not {salesPlusFuel}");
        if (G("deductions") != wht) bad.Add($"strip withholding {G("deductions")} not {wht}");
        if (ed != due) bad.Add($"strip total due {ed} not {due}");
        return bad;
    }

    private static List<string> CheckPanelAndTape(StatementOfAccountView v, bool oldRule, string[] values)
    {
        var bad = new List<string>();
        var panel = (SoaSheetTotalsPanel)typeof(StatementOfAccountView).GetField("sheetTotals", SoaF)!.GetValue(v)!;
        bool isOld = (bool)typeof(SoaSheetTotalsPanel).GetProperty("IsOld", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
        var labels = (string[])typeof(SoaSheetTotalsPanel).GetProperty("Labels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
        if (isOld != oldRule) bad.Add($"panel old-rule flag {isOld}");
        if (oldRule && !labels.SequenceEqual(OldSevenRows)) bad.Add("panel labels: " + string.Join(" | ", labels));
        if (!oldRule && !labels.SequenceEqual(FiveRows.Append("TOTAL AMOUNT DUE"))) bad.Add("panel labels: " + string.Join(" | ", labels));
        int lineH = panel.LineHeight;
        int want = lineH * labels.Length + 2 + (oldRule ? lineH : 0);
        if (panel.Height != want) bad.Add($"panel height {panel.Height} not {want}");
        // the panel draws from the same boxes the tape reads: compare the tape's rows with the expected figures
        v.GetType().GetMethod("UpdateSoaTape", SoaF)!.Invoke(v, null);
        var tape = typeof(StatementOfAccountView).GetField("soaTape", SoaF)!.GetValue(v)!;
        var rows = (List<(string Label, string Value)>)typeof(TapeCard).GetField("rows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tape)!;
        string slab = (string)typeof(TapeCard).GetField("slabValue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tape)!;
        var figs = rows.Where(r => r.Value.Length > 0).Select(r => r.Value).ToArray();
        var wantFigs = values.Take(values.Length - 1).ToArray();
        if (!figs.SequenceEqual(wantFigs)) bad.Add("tape figures: " + string.Join(" | ", figs));
        var wantLabels = oldRule ? OldSevenRows.Take(6).ToArray() : FiveRows;
        if (!rows.Where(r => r.Value.Length > 0).Select(r => r.Label).SequenceEqual(wantLabels)) bad.Add("tape labels: " + string.Join(" | ", rows.Select(r => r.Label)));
        if (oldRule && !rows.Any(r => r.Label.StartsWith("Saved before Nov 12"))) bad.Add("tape has no older-computation note");
        if (!oldRule && rows.Any(r => r.Label.StartsWith("Saved before Nov 12"))) bad.Add("tape shows the old note on a new-rule statement");
        if (slab != Money(decimal.Parse(values[^1]))) bad.Add("tape total due " + slab);
        return bad;
    }
}
