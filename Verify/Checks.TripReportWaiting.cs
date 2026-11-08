using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
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
}
