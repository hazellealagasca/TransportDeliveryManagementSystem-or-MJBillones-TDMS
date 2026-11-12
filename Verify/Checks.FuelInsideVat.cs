using System.Drawing;
using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12: the fuel surcharge is part of Total Sales. VAT and the 2% withholding are on freight + fuel; due = sales - withholding.</summary>
public static partial class Checks
{
    private static readonly string[] FiveRows = { "Total Sales + Fuel surcharge (VAT Inclusive)", "Less: VAT (12%)", "Amount: Net of VAT", "Less: Withholding Tax (2%)" };

    [Check]
    public static string FuelInsideVat_Formula_TenThousandAndFiveHundred()
    {
        var t = SoaTotals.From(10000m, 500m);
        var bad = new List<string>();
        if (t.TotalSales != 10500m) bad.Add($"sales {t.TotalSales}");
        if (t.Vat != 1125m) bad.Add($"vat {t.Vat}");
        if (t.NetOfVat != 9375m) bad.Add($"net {t.NetOfVat}");
        if (t.Withholding != 187.50m) bad.Add($"withholding {t.Withholding}");
        if (t.Due != 10312.50m) bad.Add($"due {t.Due}");
        if (t.Fuel != 500m) bad.Add($"fuel {t.Fuel}");
        if (t.Due != t.NetOfVat + t.Vat - t.Withholding) bad.Add("due is not net + vat - withholding");
        return string.Join("; ", bad);
    }

    [Check]
    public static string FuelInsideVat_Share_ByFreightPlusFuel_AddsUpToTheWithholdingExactly()
    {
        var bad = new List<string>();
        var sets = new[] { new[] { (23500m, 6031m), (23500m, 6031m), (34500m, 8260m) }, new[] { (10000.01m, 333.33m), (3333.33m, 7.77m), (1.05m, 0m) }, new[] { (999.99m, 0.01m) } };
        foreach (var s in sets)
        {
            var t = SoaTotals.From(s.Sum(x => x.Item1), s.Sum(x => x.Item2));
            var shares = SoaTotals.Share(t.Withholding, s.Select(x => x.Item1 + x.Item2).ToArray());
            if (shares.Sum() != t.Withholding) bad.Add($"shares {string.Join("+", shares)} != {t.Withholding}");
            // each invoice's net = its gross - its share, and they add up to the due
            decimal net = 0m; for (int i = 0; i < s.Length; i++) net += s[i].Item1 + s[i].Item2 - shares[i];
            if (net != t.Due) bad.Add($"invoice nets {net} != due {t.Due}");
        }
        return string.Join("; ", bad);
    }

    private static (int Soa, List<(int Id, Invoice Inv)> Trips) FuelSoa(params (decimal Freight, decimal Fuel)[] lines)
    {
        var trips = new List<(int Id, Invoice Inv)>();
        foreach (var (freight, fuel) in lines)
        {
            int d = Seed.Delivery(); Seed.TripReport(d);
            var inv = NewInvoice(d, freight + fuel); inv.FuelSurcharge = fuel;
            MJdatabase.SaveInvoice(inv);
            trips.Add((d, inv));
        }
        StatementOfAccountItem Line(int n, (int Id, Invoice Inv) x) => new() { TripNumber = n, DrNumbers = "DR-" + n, Destination = "Store " + n, PlateNumber = "ABC-123", Tonnage = "6T", DropCount = 1, Amount = x.Inv.Freight, FuelSurcharge = x.Inv.FuelSurcharge, DeliveryId = x.Id };
        var all = SoaTotals.From(trips.Sum(x => x.Inv.Freight), trips.Sum(x => x.Inv.FuelSurcharge));
        var soa = new StatementOfAccount { SoaNumber = "SOA-T-FV", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, PeriodCovered = "Nov 2026", TotalSalesVatInc = all.TotalSales, VatAmount = all.Vat, NetOfVat = all.NetOfVat, FuelSurchargeTotal = all.Fuel, WithholdingTax = all.Withholding, TotalAmount = all.Due, Status = PayStatus.Unpaid };
        int id = MJdatabase.SaveStatementOfAccount(soa, trips.Select((x, i) => Line(i + 1, x)).ToList());
        return (id, trips);
    }

    /// <summary>The stand-in invoice the collection screen builds for a pay target.</summary>
    private static Invoice TargetInvoice(PayTarget t) => new Invoice
    {
        InvoiceNumber = t.Title, SoaId = t.SoaId, ClientName = t.Client, IssueDate = t.IssueDate, DueDate = t.DueDate,
        TotalAmount = t.Gross, FuelSurcharge = t.Fuel, Deductions = t.Deductions, NetAmount = t.Net, Status = PayStatus.Unpaid
    };

    [Check]
    public static string FuelInsideVat_SoaTape_HasNoSeparateFuelRow()
    {
        Fresh();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var view = new StatementOfAccountView();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var vt = typeof(StatementOfAccountView);
            void Num(string n, decimal v) => ((NumericUpDown)vt.GetField(n, F)!.GetValue(view)!).Value = v;
            var t = SoaTotals.From(10000m, 500m);
            Num("numTotalSales", t.TotalSales); Num("numVat", t.Vat); Num("numNetOfVat", t.NetOfVat); Num("numFuelSurchargeTotal", t.Fuel); Num("numWithholdingTax", t.Withholding); Num("numTotalAmountDue", t.Due);
            var tape = vt.GetField("soaTape", F)!.GetValue(view);
            if (tape == null) return "the statement tape was not built";
            vt.GetMethod("UpdateSoaTape", F)!.Invoke(view, null);
            var rows = ((List<(string Label, string Value)>)typeof(TapeCard).GetField("rows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tape)!);
            var bad = new List<string>();
            var labels = rows.Select(r => r.Label).ToList();
            if (!labels.SequenceEqual(FiveRows.Take(4))) bad.Add("rows: " + string.Join(" | ", labels));
            if (labels.Any(l => l.Contains("Fuel surcharge (added") || l.Contains("no VAT"))) bad.Add("the old fuel row is still there");
            if (rows.Count > 0 && rows[0].Value != "10,500.00") bad.Add("first row shows " + rows[0].Value);
            string slab = (string)typeof(TapeCard).GetField("slabValue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tape)!;
            if (slab != "₱10,312.50") bad.Add("slab " + slab);
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string FuelInsideVat_SoaPrint_HasTheNewLabels_AndNoAddVat()
    {
        Fresh();
        var t = SoaTotals.From(10000m, 500m);
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-FV2", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due };
        var (_, items) = PrintSample(SoaTypes.Normal);
        string h = Html(PrintForms.StatementOfAccount(soa, items));
        var bad = new List<string>();
        int last = Math.Max(0, h.IndexOf("<tfoot>"));
        foreach (var l in FiveRows.Append("TOTAL AMOUNT DUE")) { int at = h.IndexOf(">" + l + "<", last); if (at < 0) bad.Add("missing or out of order: " + l); else last = at; }
        if (h.Contains("Add: VAT")) bad.Add("'Add: VAT' still prints");
        if (h.Contains(">Less: VAT<")) bad.Add("old 'Less: VAT' label");
        foreach (var n in new[] { "10,500.00", "1,125.00", "9,375.00", "187.50", "10,312.50" }) if (!h.Contains(n)) bad.Add("figure missing: " + n);
        return string.Join("; ", bad);
    }

    [Check]
    public static string FuelInsideVat_ReceiptTape_ShowsFiveRows_AndDueEqualsTheStoredAmount()
    {
        Fresh(); RunBackfill();
        var (soaId, trips) = FuelSoa((23500m, 6031m), (23500m, 6031m), (34500m, 8260m));
        var target = TargetFor(soaId);
        var inv = TargetInvoice(target);
        var rows = CollectionView.ReceiptTapeRows(inv);
        var bad = new List<string>();
        var all = SoaTotals.From(trips.Sum(x => x.Inv.Freight), trips.Sum(x => x.Inv.FuelSurcharge));
        var labels = rows.Select(r => r.Item1).ToList();
        if (!labels.Take(4).SequenceEqual(new[] { "Client", "Statement / invoice", "Invoiced", "Due" })) bad.Add("header rows: " + string.Join(" | ", labels.Take(4)));
        if (!labels.Skip(4).SequenceEqual(FiveRows)) bad.Add("computation rows: " + string.Join(" | ", labels.Skip(4)));
        if (labels.Any(l => l.StartsWith("Saved with the older"))) bad.Add("shows the older-computation note on a statement saved by the new rule");
        if (rows.Length > 4 && rows[4].Item2 != "₱" + all.TotalSales.ToString("N2")) bad.Add("sales row " + rows[4].Item2);
        if (all.Due != target.Net) bad.Add($"computed due {all.Due} != stored amount {target.Net}");
        // an invoice on no statement keeps amount + fuel, no VAT or withholding
        var lone = CollectionView.ReceiptTapeRows(new Invoice { InvoiceNumber = "SI 1", ClientName = "CLIENT A", TotalAmount = 1500m, FuelSurcharge = 500m, NetAmount = 1500m });
        if (lone.Any(r => r.Item1.Contains("VAT"))) bad.Add("a lone invoice shows VAT");
        if (!lone.Any(r => r.Item1 == "Amount") || !lone.Any(r => r.Item1 == "Fuel surcharge")) bad.Add("a lone invoice lost its Amount / Fuel rows");
        // a statement saved under the old rule (stored net is the old due) falls back to the old rows with the note
        var old = SoaTotals.From(trips.Sum(x => x.Inv.Freight), 0m);
        var oldInv = TargetInvoice(target);
        oldInv.NetAmount = old.NetOfVat + old.Vat + trips.Sum(x => x.Inv.FuelSurcharge) - old.Withholding; oldInv.Deductions = old.Withholding;
        var oldRows = CollectionView.ReceiptTapeRows(oldInv).Select(r => r.Item1).ToList();
        if (!oldRows.Contains("Saved with the older computation — open and save the statement to update")) bad.Add("no older-computation note: " + string.Join(" | ", oldRows.Skip(4)));
        if (!oldRows.Contains("Fuel surcharge") || !oldRows.Contains("Amount")) bad.Add("older rows missing");
        return string.Join("; ", bad);
    }

    [Check]
    public static string FuelInsideVat_UnpaidStatementSavedUnderTheOldRule_CanBeSavedAgainAndPaid()
    {
        Fresh(); RunBackfill();
        var (soaId, trips) = FuelSoa((23500m, 6031m), (34500m, 8260m));
        var fuel = trips.Sum(x => x.Inv.FuelSurcharge);
        var neu = SoaTotals.From(trips.Sum(x => x.Inv.Freight), fuel);
        // Put the stored figures back to what the old rule saved: VAT and withholding on freight only, fuel added.
        var o = SoaTotals.From(trips.Sum(x => x.Inv.Freight), 0m);
        decimal oldDue = o.NetOfVat + fuel + o.Vat - o.Withholding;
        Exec("UPDATE StatementsOfAccount SET TotalSalesVatInc=@a, VatAmount=@b, NetOfVat=@c, FuelSurchargeTotal=@f, WithholdingTax=@w, TotalAmount=@d WHERE Id=@i",
            ("@a", (double)o.TotalSales), ("@b", (double)o.Vat), ("@c", (double)o.NetOfVat), ("@f", (double)fuel), ("@w", (double)o.Withholding), ("@d", (double)oldDue), ("@i", soaId));
        var shares = SoaTotals.Share(o.Withholding, trips.Select(x => x.Inv.Freight).ToArray());
        for (int k = 0; k < trips.Count; k++)
            Exec("UPDATE Invoices SET Deductions=@w, NetAmount=TotalAmount-@w WHERE Id=@i", ("@w", (double)shares[k]), ("@i", trips[k].Inv.Id));
        var bad = new List<string>();
        var soa = MJdatabase.GetStatementOfAccountById(soaId)!;
        if (soa.TotalAmount != oldDue) return $"set-up: stored due {soa.TotalAmount} != {oldDue}";
        // Saved again with the new figures (what the screen now sends after recalculating).
        soa.TotalSalesVatInc = neu.TotalSales; soa.VatAmount = neu.Vat; soa.NetOfVat = neu.NetOfVat; soa.FuelSurchargeTotal = neu.Fuel; soa.WithholdingTax = neu.Withholding; soa.TotalAmount = neu.Due;
        var items = MJdatabase.GetStatementOfAccountItems(soaId);
        try { MJdatabase.SaveStatementOfAccount(soa, items); }
        catch (Exception ex) { return "re-saving the old statement was refused: " + ex.Message.Split('\n')[0]; }
        var invs = MJdatabase.GetInvoices().Where(i => trips.Any(x => x.Inv.Id == i.Id)).ToList();
        if (invs.Sum(i => i.Deductions) != neu.Withholding) bad.Add($"shares {invs.Sum(i => i.Deductions)} != {neu.Withholding}");
        if (invs.Sum(i => i.NetAmount) != neu.Due) bad.Add($"invoices' net {invs.Sum(i => i.NetAmount)} != {neu.Due}");
        var saved = MJdatabase.GetStatementOfAccountById(soaId)!;
        if (saved.TotalAmount != neu.Due) bad.Add($"saved due {saved.TotalAmount}");
        // and it can be paid in full
        try { MJdatabase.SaveOneReceipt("0201", TargetFor(soaId), "Cash", "", ""); }
        catch (Exception ex) { bad.Add("paying it was refused: " + ex.Message.Split('\n')[0]); }
        return string.Join("; ", bad);
    }

    [Check]
    public static string FuelInsideVat_PaidStatement_OpensWithItsSavedAmounts()
    {
        Fresh(); RunBackfill();
        var (soaId, _) = FuelSoa((10000m, 500m));
        // Make it look like an old, paid statement: figures saved under the old rule, then paid.
        Exec("UPDATE StatementsOfAccount SET TotalSalesVatInc=10000, VatAmount=1071.43, NetOfVat=8928.57, FuelSurchargeTotal=500, WithholdingTax=178.57, TotalAmount=10321.43, Status='Paid' WHERE Id=@i", ("@i", soaId));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(600);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            typeof(StatementOfAccountView).GetMethod("BindSoaToDrawer", F)!.Invoke(view, new object[] { MJdatabase.GetStatementOfAccountById(soaId)! });
            decimal V(string n) => ((NumericUpDown)typeof(StatementOfAccountView).GetField(n, F)!.GetValue(view)!).Value;
            var bad = new List<string>();
            if (V("numTotalSales") != 10000m || V("numVat") != 1071.43m || V("numTotalAmountDue") != 10321.43m) bad.Add($"a paid statement was recomputed: sales {V("numTotalSales")}, vat {V("numVat")}, due {V("numTotalAmountDue")}");
            return string.Join("; ", bad);
        });
    }
}
