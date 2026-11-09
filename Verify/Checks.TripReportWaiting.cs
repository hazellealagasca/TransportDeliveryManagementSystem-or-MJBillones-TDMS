using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Trip reports "Awaiting report": the Create trip report button saves nothing (no empty draft, no number), a real draft is opened to
/// continue, an empty draft is removed, and the list shows one state. Opening a report no longer writes into the search box.
/// </summary>
public static partial class Checks
{
    private static long ReportRowsFor(int deliveryId) => Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0", ("@d", deliveryId));

    private static string WithTripReportView(Func<TripReportView, Form, List<string>, string> body)
    {
        return RunOnSta(() =>
        {
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(2000);
            var problems = new List<string>();
            string extra = body(view, host, problems);
            if (extra.Length > 0) problems.Add(extra);
            lock (boxes.Boxes) if (boxes.Boxes.Count > 0) problems.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
            host.Close();
            return string.Join(" | ", problems);
        });
    }

    /// <summary>
    /// Closes the pop-ups (modal windows) that belong to THIS thread and records their titles. Application.OpenForms is shared by every thread,
    /// so a window left over from an earlier check on another thread must be skipped: touching it throws "Cross-thread operation not valid".
    /// </summary>
    private static void CloseOpenPopups(Form host, List<string> seen)
    {
        foreach (Form f in Application.OpenForms.Cast<Form>().Where(f => f != host && f.Modal).ToList())
        {
            if (f.IsHandleCreated && f.InvokeRequired) continue; // another thread's window
            seen.Add(f.Text);
            f.Close();
        }
    }

    private static void PressCreate(TripReportView view, int deliveryId)
    {
        var row = MJdatabase.GetDispatchesAwaitingReport().First(r => r.DeliveryId == deliveryId);
        typeof(TripReportView).GetMethod("CreateReportFor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { row });
        Pump(2500);
    }

    [Check]
    public static string TripReportWaiting_CreateButton_SavesNothing_OpensBlankForm_SearchBoxUntouched()
    {
        Fresh();
        int id = Seed.Delivery();
        string no = NumberOf(id);
        return WithTripReportView((view, host, problems) =>
        {
            var search = FieldOf<TextBox>(view, "txtGridSearch");
            var pnl = FieldOf<Panel>(view, "pnlForm");
            var cb = FieldOf<ComboBox>(view, "cbDelivery");
            PressCreate(view, id);
            if (ReportRowsFor(id) != 0) problems.Add("Create trip report saved a record");
            if (search != null && search.Text.Length > 0) problems.Add($"the search box was filled in: '{search.Text}'");
            if (pnl == null || !pnl.Visible) problems.Add("the form did not open");
            if (cb?.SelectedItem?.ToString()?.StartsWith(no + " - ") != true) problems.Add("the dispatch was not picked on the form");
            return "";
        });
    }

    [Check]
    public static string TripReportWaiting_EmptyDraft_IsVoided_RealDraft_IsKept()
    {
        Fresh();
        int emptyId = Seed.Delivery(), realId = Seed.Delivery();
        AddReport(NumberOf(emptyId), emptyId, "Draft");
        AddReport(NumberOf(realId), realId, "Draft");
        Exec("UPDATE TripReports SET ArrivalTime = '08:15 AM' WHERE DeliveryId = @d", ("@d", realId));

        return WithTripReportView((view, host, problems) =>
        {
            var search = FieldOf<TextBox>(view, "txtGridSearch");
            var pnl = FieldOf<Panel>(view, "pnlForm");
            PressCreate(view, emptyId);
            if (ReportRowsFor(emptyId) != 0) problems.Add("an empty draft was not removed when Create was pressed");
            if (pnl == null || !pnl.Visible) problems.Add("the form did not open for the empty-draft dispatch");
            // close it, then press Create on the dispatch with a real draft
            typeof(TripReportView).GetMethod("SetFormVisibility", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { false });
            Pump(300);
            PressCreate(view, realId);
            if (ReportRowsFor(realId) != 1) problems.Add("a draft with typing in it was removed or duplicated");
            if (pnl == null || !pnl.Visible) problems.Add("the real draft did not open to continue");
            if (FieldOf<TextBox>(view, "txtArrivalTime")?.Text != "08:15 AM") problems.Add("the real draft's typing was not loaded");
            if (search != null && search.Text.Length > 0) problems.Add($"the search box was filled in: '{search.Text}'");
            return "";
        });
    }

    [Check]
    public static string TripReportWaiting_Cleanup_VoidsOnlyEmptyDrafts_WithBackup()
    {
        Fresh();
        int e1 = Seed.Delivery(), e2 = Seed.Delivery(), real = Seed.Delivery(), done = Seed.Delivery();
        AddReport(NumberOf(e1), e1, "Draft"); AddReport(NumberOf(e2), e2, "Draft");
        AddReport(NumberOf(real), real, "Draft"); Exec("UPDATE TripReports SET Remarks = 'left gate late' WHERE DeliveryId = @d", ("@d", real));
        AddReport(NumberOf(done), done, "Completed");
        var problems = new List<string>();
        var empties = MJdatabase.GetEmptyDraftReports();
        if (empties.Count != 2 || empties.Any(d => d.DeliveryId == real || d.DeliveryId == done)) problems.Add($"expected exactly the 2 empty drafts, found {empties.Count}");
        string dir = Path.Combine(Path.GetDirectoryName(MJdatabase.DbFilePath)!, "Backups");
        int before = Directory.Exists(dir) ? Directory.GetFiles(dir, "before_empty_draft_cleanup_*.db").Length : 0;
        int n = MJdatabase.VoidEmptyDraftReports(empties.Select(d => d.Id), backup: true, reason: "check");
        int after = Directory.Exists(dir) ? Directory.GetFiles(dir, "before_empty_draft_cleanup_*.db").Length : 0;
        if (n != 2) problems.Add($"voided {n}, expected 2");
        if (after != before + 1) problems.Add("no backup was made before clearing; folder " + dir + " exists=" + Directory.Exists(dir) + " files=" + (Directory.Exists(dir) ? string.Join(",", Directory.GetFiles(dir).Select(Path.GetFileName)) : ""));
        if (ReportRowsFor(real) != 1) problems.Add("a draft with typing was voided");
        if (ReportRowsFor(done) != 1) problems.Add("a completed report was voided");
        if (ReportRowsFor(e1) != 0 || ReportRowsFor(e2) != 0) problems.Add("an empty draft is still live");
        return string.Join(" | ", problems);
    }

    [Check]
    public static string TripReportWaiting_AwaitingList_HasNoStateOrReportNumberColumns()
    {
        Fresh();
        int id = Seed.Delivery();
        return WithTripReportView((view, host, problems) =>
        {
            var grid = FieldOf<DataGridView>(view, "gridAwaiting");
            if (grid == null) return "the awaiting list was not found";
            foreach (DataGridViewColumn c in grid.Columns)
                if ((c.DataPropertyName == "ReportState" || c.DataPropertyName == "ReportNumber") && c.Visible) problems.Add($"column '{c.HeaderText}' is still shown");
            return "";
        });
    }

    private static List<string> VisibleColumnOrder(DataGridView g)
        => g.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).Select(c => c.Name.Length > 0 && c.Name.StartsWith("col") || c.Name.StartsWith("x") ? c.Name : c.DataPropertyName).ToList();

    [Check]
    public static string TripReportWaiting_AwaitingList_Layout_Filters_Header_Details()
    {
        Fresh();
        int aOld = Seed.Delivery("CLIENT A"), aNew = Seed.Delivery("CLIENT A"), b = Seed.Delivery("CLIENT B", g: 20, e: 10);
        int sp = Seed.SpecialTrip("CLIENT A");
        SetDate("Deliveries", aOld, 4); SetDate("Deliveries", b, 2); SetDate("Deliveries", aNew, 1); SetDate("SpecialTrips", sp, 3);
        Exec("UPDATE SpecialTrips SET ModeOfPaymentWithReport = 1");
        string nOld = NumberOf(aOld), nNew = NumberOf(aNew), nB = NumberOf(b);
        string nSp = MJdatabase.GetSpecialTrips().First(t => t.Id == sp).OrderReference;
        return WithTripReportView((view, host, problems) =>
        {
            var grid = FieldOf<DataGridView>(view, "gridAwaiting");
            var header = FieldOf<Label>(view, "lblGridHeader");
            var search = FieldOf<TextBox>(view, "txtGridSearch");
            if (grid == null || header == null || search == null) return "a control was not found";
            Pump(1500);
            var order = VisibleColumnOrder(grid);
            var expected = new[] { "colDetails", "DispatchNumber", "DispatchDate", "ClientName", "colCreate" };
            if (!order.Take(5).SequenceEqual(expected)) problems.Add("column order starts: " + string.Join(", ", order.Take(5)));
            if (grid.Columns["colCreate"]?.Frozen != true) problems.Add("the left columns are not frozen");
            var numbers = grid.Rows.Cast<DataGridViewRow>().Select(r => ((AwaitingReportRow)r.DataBoundItem!).DispatchNumber).ToList();
            if (!numbers.SequenceEqual(new[] { nOld, nSp, nB, nNew })) problems.Add("rows (oldest first, special trip included): " + string.Join(", ", numbers));
            if (header.Text != "Awaiting report: 4 dispatches") problems.Add($"header '{header.Text}'");
            var rowB = grid.Rows.Cast<DataGridViewRow>().Select(r => (AwaitingReportRow)r.DataBoundItem!).First(r => r.DispatchNumber == nB);
            if (rowB.TotalCrates != 30) problems.Add($"crates {rowB.TotalCrates}, expected 30");
            if (rowB.WaitingDays != 2 || rowB.Waiting != "2 days") problems.Add($"waiting '{rowB.Waiting}'");
            if (grid.Rows.Cast<DataGridViewRow>().Any(r => ((AwaitingReportRow)r.DataBoundItem!).DeliveryId < 0 && ((AwaitingReportRow)r.DataBoundItem!).DispatchNumber != nSp)) problems.Add("unexpected special trip row");

            search.Text = "client b";
            Pump(300);
            if (grid.Rows.Count != 1 || ((AwaitingReportRow)grid.Rows[0].DataBoundItem!).DispatchNumber != nB) problems.Add($"search 'client b' gave {grid.Rows.Count} row(s)");
            if (header.Text != "Awaiting report: 1 of 4") problems.Add($"header with a search '{header.Text}'");
            search.Text = "";
            Pump(300);
            if (grid.Rows.Count != 4) problems.Add($"clearing the search left {grid.Rows.Count} rows");

            // the drop button opens the order summary window
            typeof(TripReportView).GetMethod("ShowDetailsFor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { b, 0 });
            Pump(800);
            var dlg = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "DispatchDetailsDialog");
            if (dlg == null) problems.Add("the details window did not open");
            else dlg.Close();
            return "";
        });
    }

    [Check]
    public static string TripReportWaiting_ReportsList_InvoiceButton_Layout_ReadyToInvoiceTile()
    {
        Fresh();
        int ready = Seed.Delivery(), billed = Seed.Delivery(), draft = Seed.Delivery();
        AddReport(NumberOf(ready), ready, "Completed");
        AddReport(NumberOf(billed), billed, "Completed");
        AddReport(NumberOf(draft), draft, "Draft"); Exec("UPDATE TripReports SET Remarks = 'typed' WHERE DeliveryId = @d", ("@d", draft));
        Seed.Invoice(billed, 5000m);
        string invNo = Str("SELECT InvoiceNumber FROM Invoices WHERE DeliveryId = @d", ("@d", billed));
        return WithTripReportView((view, host, problems) =>
        {
            var grid = FieldOf<DataGridView>(view, "gridTripReports");
            if (grid == null) return "the reports list was not found";
            typeof(TripReportView).GetMethod("SetHistoryMode", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { false });
            Pump(1200);
            var order = VisibleColumnOrder(grid);
            if (!order.Take(5).SequenceEqual(new[] { "colDetails", "DispatchNumber", "ReportDate", "xClient", "colInvoice" })) problems.Add("column order starts: " + string.Join(", ", order.Take(5)));
            if (grid.Columns["colInvoice"]?.Frozen != true) problems.Add("the left columns are not frozen");
            var text = typeof(TripReportView).GetMethod("ReportActionText", BindingFlags.Instance | BindingFlags.NonPublic)!;
            string Say(int deliveryId)
            {
                var all = (List<TripReport>)typeof(TripReportView).GetField("allTripReports", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
                return (string)text.Invoke(view, new object[] { all.First(t => t.DeliveryId == deliveryId) })!;
            }
            if (Say(ready) != "🧾 Create invoice") problems.Add($"completed, not invoiced says '{Say(ready)}'");
            if (!Say(billed).StartsWith("Invoice " + invNo)) problems.Add($"invoiced says '{Say(billed)}' (expected the invoice number {invNo})");
            if (Say(draft) != "Finish report") problems.Add($"draft says '{Say(draft)}'");
            var tile = typeof(TripReportView).GetField("tShort", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view);
            string tileText = tile?.GetType().GetProperty("Value")?.GetValue(tile)?.ToString() ?? "?";
            if (tileText != "1") problems.Add($"Ready to invoice tile shows '{tileText}', expected 1");
            return "";
        });
    }
}
