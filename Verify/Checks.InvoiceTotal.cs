using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Fix 3: the service invoice shows Amount + Fuel surcharge = Invoice total (the saved figure), and the statement's saved total due when it bills a statement.</summary>
public static partial class Checks
{
    private const BindingFlags InvF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static List<(string Label, string Value)> TapeRows(object tape) => ((List<(string Label, string Value)>)tape.GetType().GetField("rows", InvF)!.GetValue(tape)!).ToList();

    [Check]
    public static string InvoiceTotal_ShownTotalEqualsSavedTotal_NewAndEdited()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        var inv = NewInvoice(d, 0m);
        inv.TotalAmount = InvoiceTotal.Of(45000m, 3500m); inv.FuelSurcharge = 3500m; inv.NetAmount = inv.TotalAmount;
        MJdatabase.SaveInvoice(inv);
        var bad = new List<string>();
        var saved = MJdatabase.GetInvoices().First(i => i.Id == inv.Id);
        var lines = InvoiceTotal.Lines(saved.Freight, saved.FuelSurcharge);
        if (lines[2].Value != "₱" + saved.TotalAmount.ToString("N2") || saved.TotalAmount != 48500m) bad.Add($"new: shown {lines[2].Value}, saved {saved.TotalAmount}");
        // An edited invoice: change the fuel and save again; the shown total follows the stored one.
        decimal freight0 = saved.Freight; saved.FuelSurcharge = 4000m; saved.TotalAmount = InvoiceTotal.Of(freight0, 4000m); saved.NetAmount = saved.TotalAmount;
        MJdatabase.SaveInvoice(saved);
        var again = MJdatabase.GetInvoices().First(i => i.Id == inv.Id);
        var l2 = InvoiceTotal.Lines(again.Freight, again.FuelSurcharge);
        if (l2[2].Value != "₱" + again.TotalAmount.ToString("N2") || again.TotalAmount != 49000m) bad.Add($"edited: shown {l2[2].Value}, saved {again.TotalAmount}");
        if (lines[0].Label != "Amount" || lines[1].Label != "+ Fuel surcharge" || lines[2].Label != "= Invoice total") bad.Add("labels: " + string.Join(" | ", lines.Select(l => l.Label)));
        return string.Join("; ", bad);
    }

    [Check]
    public static string InvoiceTotal_StatementInvoice_SavedTotalIsAmountPlusFuel_StatementLineIsSavedDue()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soaId = Seed.Soa(total: 47632.14m);
        Exec("INSERT INTO StatementOfAccountItems (SoaId, TripNumber, DeliveryId, SpecialTripId, Amount, FuelSurcharge) VALUES (@s, 1, @d, 0, 45000, 3500)", ("@s", soaId), ("@d", d));
        var bad = new List<string>();
        var line = MJdatabase.GetStatementLineForTrip(d);
        if (line == null || line.SoaId != soaId) return "statement line not found";
        var inv = NewInvoice(d, 0m);
        inv.TotalAmount = InvoiceTotal.Of(line.Freight, line.Fuel); inv.FuelSurcharge = line.Fuel; inv.NetAmount = inv.TotalAmount;
        MJdatabase.SaveInvoice(inv);
        var saved = MJdatabase.GetInvoices().First(i => i.Id == inv.Id);
        if (saved.TotalAmount != 48500m) bad.Add($"saved total {saved.TotalAmount}");
        var due = MJdatabase.GetSavedStatementTotalDue(soaId);
        if (due != 47632.14m) bad.Add($"statement due {due}");
        if (MJdatabase.GetSavedStatementTotalDue(0) != null) bad.Add("a statement of 0 should have no line");
        if (InvoiceTotal.StatementLine(47632.14m).Value != "₱47,632.14") bad.Add("statement line text " + InvoiceTotal.StatementLine(47632.14m).Value);
        return string.Join("; ", bad);
    }

    [Check]
    public static string InvoiceTotal_FormTape_ShowsTotalAndStatementLine_ListHasColumn()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soaId = Seed.Soa(total: 47632.14m);
        Exec("INSERT INTO StatementOfAccountItems (SoaId, TripNumber, DeliveryId, SpecialTripId, Amount, FuelSurcharge) VALUES (@s, 1, @d, 0, 45000, 3500)", ("@s", soaId), ("@d", d));
        var inv = NewInvoice(d, 48500m); inv.FuelSurcharge = 3500m; inv.SoaId = soaId;
        MJdatabase.SaveInvoice(inv);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (BillingView)Activator.CreateInstance(typeof(BillingView), nonPublic: true)!;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var bad = new List<string>();
            T? Get<T>(string name) where T : class => typeof(BillingView).GetField(name, InvF)!.GetValue(view) as T;
            var amount = Get<NumericUpDown>("numTotalAmount")!; var fuel = Get<NumericUpDown>("numFuel")!;
            var tape = Get<object>("billNetTape");
            if (tape == null) return "amounts tape not created";
            var upd = typeof(BillingView).GetMethod("UpdateBillNet", InvF)!;
            // A new invoice typed by hand: no statement line.
            amount.Value = 45000m; fuel.Value = 3500m;
            upd.Invoke(view, new object[] { 0m });
            var rows = TapeRows(tape);
            if (!rows.Any(r => r.Label == "= Invoice total" && r.Value == "₱48,500.00")) bad.Add("new: no Invoice total 48,500.00 line: " + string.Join(" | ", rows.Select(r => r.Label + "=" + r.Value)));
            if (rows.Any(r => r.Label.StartsWith("Statement total due"))) bad.Add("new: a statement line shows for an invoice on no statement");
            // The saved invoice that bills the statement: the statement's SAVED total due appears, never recalculated.
            var saved = MJdatabase.GetInvoices().First(i => i.Id == inv.Id);
            typeof(BillingView).GetField("selectedInvoice", InvF)!.SetValue(view, saved);
            upd.Invoke(view, new object[] { 0m });
            rows = TapeRows(tape);
            if (!rows.Any(r => r.Label == "= Invoice total" && r.Value == "₱48,500.00")) bad.Add("saved: no Invoice total line");
            if (!rows.Any(r => r.Label.StartsWith("Statement total due") && r.Value == "₱47,632.14")) bad.Add("saved: statement line missing or not the saved total due: " + string.Join(" | ", rows.Select(r => r.Label + "=" + r.Value)));
            var grid = Get<DataGridView>("gridInvoices")!;
            var col = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "Invoice total");
            if (col == null) bad.Add("the invoice list has no Invoice total column");
            else if (col.DataPropertyName != "TotalAmount") bad.Add("Invoice total column reads " + col.DataPropertyName);
            view.Dispose();
            return string.Join("; ", bad);
        });
    }
}
