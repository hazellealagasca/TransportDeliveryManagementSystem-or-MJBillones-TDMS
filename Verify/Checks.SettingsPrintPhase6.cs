using System.Globalization;
using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Phase 6 (settings and printing): one Audit Trail tab in Settings (record changes and sign-ins in one list, filters, sortable, admin only),
/// the final total bold on the statement sheet, its computation box, the invoice list, the receipt and the registers, and the receipt reference
/// printed no larger than the total amount.
/// </summary>
public static partial class Checks
{
    private static void P6Seed()
    {
        Fresh();
        MJdatabase.LogSecurityEvent("auditor1", "LOGIN_SUCCESS", "Signed in to the desktop");
        MJdatabase.LogSecurityEvent("auditor2", "LOGIN_FAILED", "Wrong password");
        // Audit rows can never be updated, so the two changes are inserted with their own date and user.
        string ts(int daysAgo) => DateTime.Now.AddDays(-daysAgo).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        const string ins = "INSERT INTO AuditTrail (Timestamp, Username, UserRole, ActionType, TableName, RecordId, RecordIdentifier, FieldName, OldValue, NewValue, ReasonOrNotes, IpOrMachine) VALUES (@t, @u, 'Admin', @a, @tb, @id, @r, @f, @o, @n, @x, 'PC')";
        Exec(ins, ("@t", ts(0)), ("@u", "admin1"), ("@a", "UPDATE"), ("@tb", "Trucks"), ("@id", 1), ("@r", "TRK-NEW"), ("@f", "Status"), ("@o", "Active"), ("@n", "Idle"), ("@x", "p6 recent change"));
        Exec(ins, ("@t", ts(40)), ("@u", "clerk1"), ("@a", "ADD"), ("@tb", "Clients"), ("@id", 2), ("@r", "CLIENT-OLD"), ("@f", ""), ("@o", ""), ("@n", ""), ("@x", "p6 old change"));
    }

    private static IEnumerable<Control> P6All(Control root)
    {
        foreach (Control c in root.Controls) { yield return c; foreach (var d in P6All(c)) yield return d; }
    }

    private static Form P6Host() => new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };

    [Check]
    public static string SettingsPhase6_OneAuditTrailTab_ShowsLoginAndChange()
    {
        P6Seed();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var prev = SessionContext.ActiveUser;
            SessionContext.ActiveUser = new User { Id = 1, Username = "admin", Role = "Admin", FullName = "Admin", IsActive = true };
            var host = P6Host();
            try
            {
                var sv = new SettingsView(SessionContext.ActiveUser, "AuditTrail") { Dock = DockStyle.Fill };
                host.Controls.Add(sv); host.Show(); Pump(500);
                var tabs = P6All(sv).OfType<Button>().Where(b => b.Tag is string).ToList();
                if (tabs.Count(b => b.Text.Contains("Audit Trail")) != 1) bad.Add($"{tabs.Count(b => b.Text.Contains("Audit Trail"))} Audit Trail tabs");
                if (tabs.Any(b => b.Text.Contains("Change History") || b.Text.Contains("Security Audit Logs"))) bad.Add("an old tab (Change History / Security Audit Logs) is still in Settings");
                foreach (var oldTab in new[] { "SystemHistory", "Audit" })
                {
                    sv.SelectTab("Features"); Pump(200);
                    sv.SelectTab(oldTab); Pump(300);
                    if (P6All(sv).OfType<AuditTrailView>().FirstOrDefault() == null) bad.Add($"the old tab key '{oldTab}' no longer opens the Audit Trail");
                }
                var view = P6All(sv).OfType<AuditTrailView>().FirstOrDefault();
                if (view == null) { bad.Add("the Audit Trail tab did not show an AuditTrailView"); return string.Join("; ", bad); }
                var rows = view.ShownRows;
                if (!rows.Any(r => r.IsSecurity && r.Area == "Security" && r.User == "auditor1" && r.Action == "Signed in")) bad.Add("the sign-in entry is not listed");
                var change = rows.FirstOrDefault(r => !r.IsSecurity && r.Record == "TRK-NEW");
                if (change == null || change.Area != "Trucks" || change.Action != "Updated" || !change.Details.Contains("Active") || !change.Details.Contains("Idle")) bad.Add("the record change is not listed as Trucks / Updated with its old and new value");
                if (rows.Zip(rows.Skip(1), (a, b) => a.When >= b.When).Any(x => !x)) bad.Add("not newest first by default");
                var headers = view.Grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).ToArray();
                if (!headers.SequenceEqual(new[] { "Date/time", "User", "Area", "Action", "Record", "Details" })) bad.Add("columns: " + string.Join(", ", headers));
                if (view.Grid.Rows.Count > 0 && view.Grid.Rows[0].Cells[0].FormattedValue?.ToString() != rows[0].When.ToString("MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture)) bad.Add("date/time is not MM/dd/yyyy hh:mm tt");
            }
            finally { host.Close(); SessionContext.ActiveUser = prev; }
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SettingsPhase6_AuditTrail_FiltersAndSorting()
    {
        P6Seed();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var prev = SessionContext.ActiveUser;
            SessionContext.ActiveUser = new User { Id = 1, Username = "admin", Role = "Admin", FullName = "Admin", IsActive = true };
            try
            {
                var v = new AuditTrailView();
                int total = v.ShownRows.Count;
                if (total < 4) bad.Add("only " + total + " entries seeded");
                v.SetFilters(DateTime.Today.AddDays(-7), DateTime.Today);
                if (v.ShownRows.Any(r => r.Record == "CLIENT-OLD") || !v.ShownRows.Any(r => r.Record == "TRK-NEW")) bad.Add("date range did not drop the 40-day-old entry");
                v.SetFilters(null, DateTime.Today.AddDays(-30));
                if (v.ShownRows.Count != 1 || v.ShownRows[0].Record != "CLIENT-OLD") bad.Add("'to' date did not leave only the old entry (" + v.ShownRows.Count + ")");
                v.SetFilters(null, null, user: "auditor2");
                if (v.ShownRows.Count == 0 || v.ShownRows.Any(r => r.User != "auditor2")) bad.Add("user filter");
                v.SetFilters(null, null, area: "Security");
                if (v.ShownRows.Count == 0 || v.ShownRows.Any(r => !r.IsSecurity)) bad.Add("Security area filter");
                v.SetFilters(null, null, area: "Trucks");
                if (v.ShownRows.Count == 0 || v.ShownRows.Any(r => r.Area != "Trucks")) bad.Add("module area filter");
                v.SetFilters(null, null, action: "Sign-in failed");
                if (v.ShownRows.Count == 0 || v.ShownRows.Any(r => r.Action != "Sign-in failed")) bad.Add("action filter");
                v.SetFilters(null, null, text: "wrong password");
                if (v.ShownRows.Count == 0 || v.ShownRows.Any(r => !r.Details.Contains("Wrong password", StringComparison.OrdinalIgnoreCase))) bad.Add("text search");
                v.SetFilters(null, null);
                if (v.ShownRows.Count != total) bad.Add("clearing the filters did not bring every entry back");

                v.SortBy("User");
                var users = v.ShownRows.Select(r => r.User.ToLowerInvariant()).ToList();
                if (!users.SequenceEqual(users.OrderBy(u => u, StringComparer.Ordinal))) bad.Add("User header click did not sort A to Z");
                v.SortBy("User");
                users = v.ShownRows.Select(r => r.User.ToLowerInvariant()).ToList();
                if (!users.SequenceEqual(users.OrderByDescending(u => u, StringComparer.Ordinal))) bad.Add("second click did not reverse");
                v.SortBy("WhenText");
                if (v.ShownRows.Zip(v.ShownRows.Skip(1), (a, b) => a.When >= b.When).Any(x => !x)) bad.Add("Date/time header click did not go back to newest first");
                v.SortBy("WhenText");
                if (v.ShownRows.Zip(v.ShownRows.Skip(1), (a, b) => a.When <= b.When).Any(x => !x)) bad.Add("Date/time second click did not give oldest first");
            }
            finally { SessionContext.ActiveUser = prev; }
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SettingsPhase6_AuditTrail_AdminOnly()
    {
        P6Seed();
        return RunOnSta(() =>
        {
            var prev = SessionContext.ActiveUser;
            SessionContext.ActiveUser = new User { Id = 2, Username = "clerk", Role = "Billing Clerk", FullName = "Clerk", IsActive = true };
            var host = P6Host();
            try
            {
                var sv = new SettingsView(SessionContext.ActiveUser);
                host.Controls.Add(sv); host.Show(); Pump(300);
                bool tab = P6All(sv).OfType<Button>().Any(b => b.Tag is string && b.Text.Contains("Audit Trail"));
                var direct = new AuditTrailView();
                var bad = new List<string>();
                if (tab) bad.Add("a non-admin sees the Audit Trail tab");
                if (direct.ShownRows.Count != 0) bad.Add("a non-admin's AuditTrailView lists entries");
                return string.Join("; ", bad);
            }
            finally { host.Close(); SessionContext.ActiveUser = prev; }
        });
    }

    /// <summary>True when the label is the whole content of a bold element.</summary>
    private static bool P6Bold(string html, string label) => html.Contains("<strong>" + label + "</strong>", StringComparison.Ordinal);

    [Check]
    public static string PrintPhase6_FinalTotalIsBold_SoaSheetBlock_AndComputationBox()
    {
        Fresh();
        var bad = new List<string>();
        var t = SoaTotals.From(266500m, 65424m);
        var soa = new StatementOfAccount { Id = 1, SoaNumber = "SOA-T-6", ClientName = "CLIENT A", SoaType = SoaTypes.Normal, IssueDate = DateTime.Today, TotalSalesVatInc = t.TotalSales, VatAmount = t.Vat, NetOfVat = t.NetOfVat, FuelSurchargeTotal = t.Fuel, WithholdingTax = t.Withholding, TotalAmount = t.Due };
        var (_, items) = PrintSample(SoaTypes.Normal);
        var def = PrintForms.StatementOfAccount(soa, items);
        string h = Html(def);
        if (!P6Bold(h, "TOTAL AMOUNT DUE")) bad.Add("sheet block label TOTAL AMOUNT DUE is not inside a bold element");
        if (!P6Bold(h, "325,996.79")) bad.Add("sheet block total figure is not bold");
        if (P6Bold(h, "Less: VAT (12%)") || P6Bold(h, "296,360.71")) bad.Add("a middle line is bold too");
        // The computation box: its last line is the total.
        string box = PrintKit.Summary(new[] { ("Total Sales + Fuel surcharge (VAT Inclusive)", "331,924.00"), ("Total amount due", "₱325,996.79") }, "Computation");
        if (!P6Bold(box, "Total amount due") || !P6Bold(box, "₱325,996.79")) bad.Add("computation box total is not bold");
        if (P6Bold(box, "Total Sales + Fuel surcharge (VAT Inclusive)")) bad.Add("computation box first line is bold");
        string page = PrintKit.Page(def, def.Defaults, false, true);
        if (!Regex.IsMatch(page, @"strong\s*,\s*b\s*,\s*\.strong\s*\{\s*font-weight:\s*800")) bad.Add("the print CSS has no bold rule for strong");
        return string.Join("; ", bad);
    }

    [Check]
    public static string PrintPhase6_FinalTotalIsBold_InvoiceList_Receipt_Registers()
    {
        Fresh(); RunBackfill();
        var bad = new List<string>();
        var (soaId, _) = SoaWithThreeInvoices();
        var invoices = MJdatabase.GetInvoices().Where(i => i.SoaId == soaId).ToList();
        if (invoices.Count == 0) return "no invoices seeded";
        string inv = Html(PrintForms.StatementOfInvoices(MJdatabase.GetStatementsOfAccount().First(s => s.Id == soaId), invoices));
        if (!P6Bold(inv, "Total payable")) bad.Add("invoice list: Total payable is not bold");
        if (P6Bold(inv, "Total amount")) bad.Add("invoice list: Total amount (not the final line) is bold");

        var rows = MJdatabase.SaveOneReceipt("0601", TargetFor(soaId), "Check", "CHK-123456", "");
        string rc = Html(PrintForms.CollectionReceipt(rows[0]));
        if (!P6Bold(rc, "Amount received")) bad.Add("receipt: Amount received is not bold");   // Nov 17: "(paid in full)" removed from the label
        if (!P6Bold(rc, "Total due")) bad.Add("receipt: Total due is not bold");
        if (P6Bold(rc, "Method / reference")) bad.Add("receipt: the reference line is bold like a total");
        if (!Regex.IsMatch(rc, @"class='amt-total'><strong>[^<]*600\.00")) bad.Add("receipt: the big amount is not inside a bold element");

        // Registers: the total row of a table and the totals strip print their figures inside strong.
        string reg = PrintKit.Table(new PrintOptions(), new[] { ("a", "A", ""), ("b", "B", "num") }, new[] { (IDictionary<string, string>)new Dictionary<string, string> { ["a"] = "x", ["b"] = "1.00" } }, new Dictionary<string, string> { ["a"] = "Total", ["b"] = "1.00" });
        if (!Regex.IsMatch(reg, @"<tfoot>.*<strong>1\.00</strong>.*</tfoot>")) bad.Add("register total row is not bold");
        string stats = PrintKit.Stats(new[] { ("Total billed", "PHP 1.00"), ("Still receivable", "PHP 1.00") });
        if (!stats.Contains("<strong>PHP 1.00</strong>")) bad.Add("register totals strip is not bold");
        return string.Join("; ", bad);
    }

    [Check]
    public static string PrintPhase6_ReceiptReference_NoLargerThanTheTotalAmount()
    {
        Fresh(); RunBackfill();
        var (soaId, _) = SoaWithThreeInvoices();
        var rows = MJdatabase.SaveOneReceipt("0602", TargetFor(soaId), "Check", "CHK-654321", "");
        var def = PrintForms.CollectionReceipt(rows[0]);
        string body = Html(def), page = PrintKit.Page(def, def.Defaults, false, true);
        var bad = new List<string>();
        double Em(string selector)
        {
            var m = Regex.Match(page, selector + @"\s*\{[^}]*?font-size:\s*([0-9.]+)em");
            return m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : -1;
        }
        double total = Em(@"\.amt-total"), refSize = Em(@"td\.ref, table\.summary tr\.ref td");
        if (total <= 0) bad.Add("no font size found for the total amount");
        if (refSize <= 0) bad.Add("no single rule sets the reference font size");
        if (total > 0 && refSize > total) bad.Add($"reference {refSize}em is larger than the total {total}em");
        if (!body.Contains("<td class='v ref'>CHK-654321</td>")) bad.Add("the reference field does not use the ref class");
        if (!Regex.IsMatch(body, @"<tr class='ref'><td>Method / reference</td><td class='num'>[^<]*CHK-654321")) bad.Add("the summary reference line does not use the ref class");
        if (body.Contains("<tr class='grand'><td>Method / reference")) bad.Add("the reference line is still the large 'grand' line");
        return string.Join("; ", bad);
    }
}
