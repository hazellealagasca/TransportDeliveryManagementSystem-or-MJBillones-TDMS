using System.Reflection;
using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Phase 5 (payments): "For Collection" instead of Unpaid on screen (the stored word stays), no draft invoices (PHP 0.00 refused, old Draft rows
/// read For Collection or Incomplete), no Trip payments card inside the statement, drops read-only = the trip's store count (also printed),
/// "+ New statement" in the bottom bar, new clients in the statement's client box without a restart, "service invoice" wording and the four
/// Payments tab names in order.
/// </summary>
public static partial class Checks
{
    private const BindingFlags P5F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
    private static T P5Get<T>(object o, string field) where T : class => (T)o.GetType().GetField(field, P5F)!.GetValue(o)!;

    private static (BillingView view, Form host) P5OpenBilling()
    {
        var view = (BillingView)Activator.CreateInstance(typeof(BillingView), nonPublic: true)!;
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
        view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
        Pump(800);
        return (view, host);
    }

    [Check]
    public static string Payments_UnpaidReadsForCollection_StoredValueStays_OldDraftRule()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        if (StatusText.Display("Unpaid") != "For Collection") bad.Add("Display(Unpaid) = " + StatusText.Display("Unpaid"));
        if (StatusText.Display("unpaid") != "For Collection") bad.Add("Display is case sensitive");
        if (StatusText.Display("Paid") != "Paid" || StatusText.Display("Cancelled") != "Cancelled") bad.Add("Paid or Cancelled changed");
        if (StatusText.Billing("Draft", 100m) != "For Collection") bad.Add("Draft with an amount: " + StatusText.Billing("Draft", 100m));
        if (StatusText.Billing("Draft", 0m) != "Incomplete") bad.Add("Draft with no amount: " + StatusText.Billing("Draft", 0m));
        if (StatusText.BillingText("Unpaid · Mixed (old)", 5m) != "For Collection · Mixed (old)") bad.Add("BillingText: " + StatusText.BillingText("Unpaid · Mixed (old)", 5m));
        int d1 = Seed.Delivery(), d2 = Seed.Delivery(), d3 = Seed.Delivery();
        int unpaid = Seed.Invoice(d1, 500m, "Unpaid"), draftAmt = Seed.Invoice(d2, 700m, "Draft"), draftZero = Seed.Invoice(d3, 0m, "Draft");
        var stored = MJdatabase.GetInvoices();
        if (stored.First(i => i.Id == unpaid).Status != "Unpaid") bad.Add("the stored status of the invoice is not Unpaid");
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = P5OpenBilling();
            using var _ = host;
            var grid = P5Get<DataGridView>(view, "gridInvoices");
            view.RefreshData(); Pump(600);
            var st = grid.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "Status");
            string Shown(int id) => grid.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is Invoice i && i.Id == id).Select(r => r.Cells[st.Index].FormattedValue?.ToString() ?? "").FirstOrDefault() ?? "(row missing)";
            var msg = new List<string>();
            if (!Shown(unpaid).StartsWith("For Collection")) msg.Add("unpaid invoice shows '" + Shown(unpaid) + "'");
            if (Shown(draftAmt) != "For Collection") msg.Add("old draft with an amount shows '" + Shown(draftAmt) + "'");
            if (Shown(draftZero) != "Incomplete") msg.Add("old draft with no amount shows '" + Shown(draftZero) + "'");
            string summary = P5Get<Label>(view, "lblSummary").Text;
            if (summary.Contains("draft", StringComparison.OrdinalIgnoreCase)) msg.Add("the summary still counts drafts: " + summary);
            if (!summary.Contains("for collection")) msg.Add("the summary does not say for collection: " + summary);
            host.Close();
            return string.Join("; ", msg);
        }));
        // the list, SOA and search words read the same
        string root = RepoRoot();
        string billing = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Views"), "BillingView*.cs").Select(File.ReadAllText));
        if (Regex.IsMatch(billing, @"=\s*PayStatus\.Draft\b") || billing.Contains("saved as a draft")) bad.Add("Billing still saves a Draft invoice");
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    [Check]
    public static string Payments_StatementList_UnpaidShowsForCollection()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = Seed.Soa(status: "Unpaid", total: 500m);
        Seed.Invoice(d, 500m, "Unpaid", soaId: soa);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = LockOpen();
            using var _ = host;
            var grid = P5Get<DataGridView>(view, "gridSoa");
            view.RefreshData(); Pump(600);
            var st = grid.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "Status");
            var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.DataBoundItem is StatementOfAccount s && s.Id == soa);
            string r = row == null ? "statement row missing" : (row.Cells[st.Index].FormattedValue?.ToString() ?? "") == "For Collection" ? "" : "shows '" + row.Cells[st.Index].FormattedValue + "'";
            if (r.Length == 0 && MJdatabase.GetStatementsOfAccount().First(s => s.Id == soa).Status != "Unpaid") r = "the stored status changed";
            // typing the shown word finds it
            P5Get<TextBox>(view, "txtGridSearch").Text = "for collection"; Pump(300);
            if (r.Length == 0 && !grid.Rows.Cast<DataGridViewRow>().Any(x => x.DataBoundItem is StatementOfAccount s && s.Id == soa)) r = "searching 'for collection' does not find it";
            host.Close();
            return r;
        });
    }

    [Check]
    public static string Payments_ZeroInvoiceRefused_NothingChanges()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int inv = Seed.Invoice(d, 500m, "Unpaid");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = P5OpenBilling();
            using var _ = host;
            view.RefreshData(); Pump(500);
            var saved = MJdatabase.GetInvoices().First(i => i.Id == inv);
            typeof(BillingView).GetField("selectedInvoice", P5F)!.SetValue(view, saved);
            typeof(BillingView).GetMethod("SetFormVisibility", P5F)!.Invoke(view, new object[] { true }); Pump(500);
            typeof(BillingView).GetField("selectedInvoice", P5F)!.SetValue(view, saved);
            P5Get<NumericUpDown>(view, "numTotalAmount").Value = 0m;
            P5Get<NumericUpDown>(view, "numFuel").Value = 0m;
            P5Get<TextBox>(view, "txtInvoiceNumber").Text = saved.InvoiceNumber;
            P5Get<ComboBox>(view, "cbClient").Text = saved.ClientName;
            typeof(BillingView).GetMethod("BtnUpdate_Click", P5F)!.Invoke(view, new object?[] { null, EventArgs.Empty });
            Pump(300);
            host.Close();
            var after = MJdatabase.GetInvoices().First(i => i.Id == inv);
            var bad = new List<string>();
            if (after.NetAmount != 500m) bad.Add("the amount changed to " + after.NetAmount);
            if (after.Status != "Unpaid") bad.Add("the status became " + after.Status);
            if (boxes.Boxes.Count == 0) bad.Add("no message told the user the amount is needed");
            else if (!boxes.Boxes.Any(b => b.Contains("amount", StringComparison.OrdinalIgnoreCase))) bad.Add("message does not mention the amount: " + string.Join(" | ", boxes.Boxes));
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string Payments_NewClientAppearsInStatementClientBox_NoRestart_NoHardCodedNames()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "CLIENT A", TIN = "123-456-789-000", Address = "12 PROFILE ST." });
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = LockOpen();
            using var _ = host;
            var cb = P5Get<ComboBox>(view, "cbClient");
            var filter = P5Get<ComboBox>(view, "cbFilterClient");
            var bad = new List<string>();
            List<string> Items() => cb.Items.Cast<object>().Select(i => i.ToString() ?? "").ToList();
            var active = MJdatabase.GetClients().Where(c => c.IsActive).Select(c => c.CompanyName.Trim()).ToList();
            if (!Items().OrderBy(x => x).SequenceEqual(active.OrderBy(x => x))) bad.Add("the list is not exactly the active clients: " + string.Join("|", Items()) + " vs " + string.Join("|", active));
            if (cb.SelectedIndex != -1 || cb.Text.Trim().Length > 0) bad.Add("a client is pre-selected: " + cb.Text);
            foreach (var hard in new[] { "MORE THAN CAKES, INC.", "Millenium FoodBite Corporation", "BCS Group of Company", "Bestbake Incorporation", "Richbake Incorporation" })
                if (Items().Contains(hard) && !active.Contains(hard, StringComparer.OrdinalIgnoreCase)) bad.Add("hard-coded name listed: " + hard);
            // a client saved on the Clients screen path (SaveClient raises ClientsChanged)
            MJdatabase.SaveClient(new ClientCompany { CompanyName = "BRAND NEW CORP", TIN = "111-222-333-000", Address = "1 NEW ST." });
            Pump(600);
            if (!Items().Contains("BRAND NEW CORP")) bad.Add("a saved client is not in the client box");
            if (!filter.Items.Cast<object>().Any(i => i.ToString() == "BRAND NEW CORP")) bad.Add("a saved client is not in the list filter");
            // an Individual client is listed under its name
            MJdatabase.SaveClient(new ClientCompany { CompanyName = "Juan P. Dela Cruz", ClientType = ClientTypes.Individual, Address = "5 Drop-off Rd., Tarlac", ContactNumber = "09171234567" });
            Pump(600);
            if (!Items().Contains("Juan P. Dela Cruz")) bad.Add("an Individual client is not listed");
            // an archived client drops out
            MJdatabase.SetClientActive("BRAND NEW CORP", false);
            Pump(600);
            if (Items().Contains("BRAND NEW CORP")) bad.Add("an archived client is still listed");
            // RefreshData reads again (a client added behind the event's back)
            Exec("INSERT INTO Clients (CompanyName, Address, TIN, IsActive) VALUES ('LATE ADDED INC.', '2 LATE ST.', '999-888-777-000', 1)");
            if (Items().Contains("LATE ADDED INC.")) bad.Add("test setup: the late client was already listed");
            view.RefreshData(); Pump(300);
            if (!Items().Contains("LATE ADDED INC.")) bad.Add("RefreshData does not reload the clients");
            // the tab becoming visible reads again
            Exec("INSERT INTO Clients (CompanyName, Address, TIN, IsActive) VALUES ('SHOWN LATER INC.', '3 LATER ST.', '999-888-777-111', 1)");
            view.Visible = false; Pump(100); view.Visible = true; Pump(300);
            if (!Items().Contains("SHOWN LATER INC.")) bad.Add("showing the tab does not reload the clients");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string Payments_NoTripPaymentsCard_DropsReadOnly_EqualStoreCount()
    {
        Fresh(); RunBackfill(); LockClient();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = LockOpen();
            using var _ = host;
            var bad = new List<string>();
            if (view.GetType().GetField("soaCardPay", P5F) != null || view.GetType().GetField("gridPay", P5F) != null) bad.Add("the Trip payments card fields still exist");
            if (view.GetType().GetMethod("ShowTripPayments", P5F) != null) bad.Add("ShowTripPayments still exists");
            var q = MJdatabase.GetTripReportsWaitingForStatement().Where(c => c.ClientName == "CLIENT A").ToList();
            var rows = q.Select(c => new ReadyInvoiceRow { InvoiceId = c.DeliveryId, InvoiceNumber = c.TripReportNumber, TripNumber = c.DispatchNumber, ClientName = c.ClientName, TripDate = DateTime.Today }).ToList();
            view.StartSoaFor("CLIENT A", rows);
            Pump(900);
            string Texts(Control c) => c.Text + " " + string.Join(" ", c.Controls.Cast<Control>().Select(Texts));
            if (Texts(view).Contains("Trip payments", StringComparison.OrdinalIgnoreCase)) bad.Add("'Trip payments' is still on the screen");
            var grid = P5Get<DataGridView>(view, "gridItems");
            var drops = grid.Columns["colDrops"]!;
            if (!drops.ReadOnly) bad.Add("colDrops is not read-only");
            var data = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            if (data.Count == 0) { host.Close(); return "no trips were added"; }
            // make the trip a three-store trip: two more rows of the same trip
            string trip = data[0].Cells["colTripNo"].Value?.ToString() ?? "1";
            for (int k = 0; k < 2; k++)
            {
                int i = grid.Rows.Add();
                grid.Rows[i].Cells["colTripNo"].Value = trip;
                grid.Rows[i].Cells["colDest"].Value = "EXTRA STORE " + (k + 1);
                grid.Rows[i].Cells["colDr"].Value = "DR-" + (100000 + k);
            }
            grid.Rows[0].Cells["colDrops"].Value = 99;   // a typed value never sticks
            typeof(StatementOfAccountView).GetMethod("RecalculateTaxes", P5F)!.Invoke(view, new object[] { false });
            Pump(200);
            var tripRows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && (r.Cells["colTripNo"].Value?.ToString() ?? "") == trip).ToList();
            int stores = tripRows.Count(r => !string.IsNullOrWhiteSpace(r.Cells["colDest"].Value?.ToString()) || !string.IsNullOrWhiteSpace(r.Cells["colDr"].Value?.ToString()));
            foreach (var r in tripRows)
                if (r.Cells["colDrops"].Value?.ToString() != stores.ToString()) bad.Add($"drops '{r.Cells["colDrops"].Value}' is not the store count {stores}");
            if (stores < 3) bad.Add("test setup: only " + stores + " stores");
            // typing in the cell is refused
            var beginEdit = new DataGridViewCellCancelEventArgs(drops.Index, 0);
            typeof(DataGridView).GetMethod("OnCellBeginEdit", P5F)!.Invoke(grid, new object[] { beginEdit });
            if (!beginEdit.Cancel) bad.Add("editing the drops cell is allowed");
            // the saved lines carry the store count too
            var items = (List<StatementOfAccountItem>)typeof(StatementOfAccountView).GetMethod("CollectItemsFromGrid", P5F)!.Invoke(view, null)!;
            if (items.Any(i => i.DropCount != stores)) bad.Add("a saved line has drops " + string.Join(",", items.Select(i => i.DropCount)));
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string Payments_SoaPrint_DropsIsTheStoreCount_NotATypedNumber()
    {
        Fresh();
        var soa = new StatementOfAccount { SoaNumber = "SOA-P5-1", ClientName = "CLIENT A", IssueDate = DateTime.Today, PeriodCovered = "Nov 2026", Status = "Unpaid", TotalAmount = 1000m };
        var items = new List<StatementOfAccountItem>();
        for (int k = 0; k < 3; k++)
            items.Add(new StatementOfAccountItem { TripNumber = 1, DrNumbers = "DR-10000" + k, Destination = "STORE " + k, PlateNumber = "PLT", Tonnage = "6T", Zone = "Z", DeliveryType = "REF", DropCount = 9, Amount = k == 0 ? 1000m : 0m, DriverHelper = "DRV" });
        string html = Html(PrintForms.StatementOfAccount(soa, items));
        var bad = new List<string>();
        if (!Regex.IsMatch(html, @"<td class='center' rowspan='3'>3</td>")) bad.Add("the print does not show 3 drops for a three-store trip");
        if (Regex.IsMatch(html, @"<td class='center' rowspan='3'>9</td>")) bad.Add("the print shows the typed 9");
        if (html.Contains(">Unpaid<")) bad.Add("the print shows Unpaid");
        return string.Join("; ", bad);
    }

    [Check]
    public static string Payments_NewStatementButton_IsInTheBottomBar_NextToPrint()
    {
        Fresh();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = LockOpen();
            using var _ = host;
            var toggle = P5Get<Button>(view, "btnToggleForm");
            var print = P5Get<Button>(view, "btnPrintOfficial");
            var top = P5Get<Control>(view, "pnlTopBar");
            var bottom = P5Get<Control>(view, "pnlListActionsBar");
            var bad = new List<string>();
            if (toggle.Text != "＋ New statement") bad.Add("button text is '" + toggle.Text + "'");
            bool InTree(Control root, Control c) { for (Control? p = c; p != null; p = p.Parent) if (p == root) return true; return false; }
            if (InTree(top, toggle)) bad.Add("+ New statement is still in the header");
            if (!InTree(bottom, toggle)) bad.Add("+ New statement is not in the bottom bar");
            if (bottom.Dock != DockStyle.Bottom) bad.Add("the bar is not at the bottom");
            if (toggle.Parent != print.Parent) bad.Add("+ New statement is not next to Print");
            toggle.PerformClick(); Pump(500);
            if (!P5Get<Control>(view, "pnlForm").Visible) bad.Add("+ New statement does not open the form");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string Payments_TabNames_AndOrder_SameInMenuSearchAndPermissions()
    {
        Fresh();
        string[] want = { "Statement of Accounts", "Service Invoices", "Collection Receipts", "Finance History" };
        var bad = new List<string>();
        string Letters(string s) => Regex.Replace(s, @"^[^A-Za-z]+", "").Trim();
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var hub = new FinanceHubView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
            hub.Dock = DockStyle.Fill; host.Controls.Add(hub); host.Show(); Pump(600);
            var tabs = ((Dictionary<string, Button>)typeof(FinanceHubView).GetField("tabButtons", P5F)!.GetValue(hub)!).Values.Select(b => Letters(b.Text)).ToList();
            host.Close();
            // the tab count suffix (e.g. "Service Invoices (6)") is not part of the name
            tabs = tabs.Select(t => Regex.Replace(t, @"\s*\(\d+\)$", "")).ToList();
            return tabs.SequenceEqual(want) ? "" : "hub tabs: " + string.Join(" | ", tabs);
        }));
        var perm = Permissions.Areas.Where(a => a.Module == "Payments").Select(a => a.Label).ToList();
        if (!perm.SequenceEqual(want)) bad.Add("permissions: " + string.Join(" | ", perm));
        string root = RepoRoot();
        string main = File.ReadAllText(Path.Combine(root, "MainForm.cs"));
        int block = main.IndexOf("\"💰  PAYMENTS\"", StringComparison.Ordinal);
        if (block < 0) bad.Add("side menu block not found");
        else
        {
            int last = block;
            foreach (var n in want)
            {
                int at = main.IndexOf(n, last, StringComparison.Ordinal);
                if (at < 0 || at - block > 900) { bad.Add("side menu: '" + n + "' missing or out of order"); break; }
                last = at;
            }
        }
        string search = File.ReadAllText(Path.Combine(root, "Functions", "GlobalSearch.cs"));
        int sl = 0;
        foreach (var n in want)
        {
            int at = search.IndexOf("Page(\"" + n + "\"", sl, StringComparison.Ordinal);
            if (at < 0) { bad.Add("search page '" + n + "' missing or out of order"); break; }
            sl = at;
        }
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    [Check]
    public static string Payments_ServiceInvoiceWording_ButtonAndHeader()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = P5OpenBilling();
            using var _ = host;
            var bad = new List<string>();
            var btn = P5Get<Button>(view, "btnToggleForm");
            if (btn.Text != "＋ New service invoice") bad.Add("button says '" + btn.Text + "'");
            host.Close();
            string root = RepoRoot();
            string soaSrc = File.ReadAllText(Path.Combine(root, "Views", "BillingView.Soa.cs"));
            if (!soaSrc.Contains("$\"New service invoice · {s.SoaNumber}\"")) bad.Add("the statement header does not say New service invoice");
            if (soaSrc.Contains("\"New invoice ·")) bad.Add("the old header wording is left");
            return string.Join("; ", bad);
        });
    }
}
