using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: the dispatch list's Status column follows the order's whole life (Scheduled, Current, For trip report, For Statement of Account,
/// For Service Invoice, For Collection, Completed) and the order stays in the active list until it is paid.</summary>
public static partial class Checks
{
    private static string DsKind(Delivery d, OrderChain? chain, bool onSoa, bool soaInvoice, int soaId, DispatchStageKind kind, string label)
    {
        var s = DispatchStage.For(d, chain, onSoa, soaInvoice, soaId);
        return s.Kind == kind && s.Label == label ? "" : $"{d.Status}/{chain?.TripReportStatus}/soa={onSoa}: {s.Kind} '{s.Label}' (expected {kind} '{label}')";
    }

    [Check]
    public static string DispatchStage_Rules_EveryStage()
    {
        var bad = new List<string>();
        void Add(string m) { if (m.Length > 0) bad.Add(m); }
        Delivery Of(string status) => new Delivery { Id = 5, Status = status, DispatchDate = DateTime.Today };
        OrderChain Chain(string tr, string trStatus = "Completed", LiveTripInvoice? inv = null) => new OrderChain { TripReportNo = tr, TripReportStatus = trStatus, Invoice = inv };
        // Not back from the trip yet: the dispatch's own words.
        Add(DsKind(Of("Scheduled"), null, false, false, 0, DispatchStageKind.Open, "Scheduled"));
        Add(DsKind(Of("Pending"), null, false, false, 0, DispatchStageKind.Open, "Pending"));
        Add(DsKind(Of("In Transit"), null, false, false, 0, DispatchStageKind.Open, "Current"));
        Add(DsKind(Of("Delayed"), Chain("TRIP-1", "Draft"), false, false, 0, DispatchStageKind.Open, "Delayed"));
        // Nov 28: on the road only on its dispatch day; from the next day it waits for its trip report
        { var y = Of("In Transit"); y.DispatchDate = DateTime.Today.AddDays(-1); Add(DsKind(y, null, false, false, 0, DispatchStageKind.ForTripReport, "For trip report")); }
        { var y = Of("Delayed"); y.DispatchDate = DateTime.Today.AddDays(-2); Add(DsKind(y, Chain("TRIP-1", "Draft"), false, false, 0, DispatchStageKind.ForTripReport, "For trip report")); }
        // Back from the trip with no finished trip report (none, a Draft, or a voided one that left no live report).
        Add(DsKind(Of("Completed"), null, false, false, 0, DispatchStageKind.ForTripReport, "For trip report"));
        Add(DsKind(Of("Completed"), Chain(""), false, false, 0, DispatchStageKind.ForTripReport, "For trip report"));
        Add(DsKind(Of("Completed"), Chain("TRIP-1", "Draft"), false, false, 0, DispatchStageKind.ForTripReport, "For trip report"));
        Add(DsKind(Of("Delivered"), null, false, false, 0, DispatchStageKind.ForTripReport, "For trip report"));
        // Trip report finished, not on a live statement (a cancelled statement is not "on" one: the caller passes onLiveSoa = false).
        Add(DsKind(Of("Completed"), Chain("TRIP-1"), false, false, 0, DispatchStageKind.ForSoa, "For Statement of Account"));
        Add(DsKind(Of("Completed"), Chain("TRIP-1", "Invoiced"), false, false, 0, DispatchStageKind.ForSoa, "For Statement of Account"));
        // On a live statement with no invoice yet.
        Add(DsKind(Of("Completed"), Chain("TRIP-1"), true, false, 7, DispatchStageKind.ForInvoice, "For Service Invoice"));
        // Invoiced, not paid (also an invoice of its own that is on no statement).
        var unpaid = new LiveTripInvoice { InvoiceId = 9, Number = "SI-1", Status = "Unpaid", SoaId = 7 };
        Add(DsKind(Of("Completed"), Chain("TRIP-1", "Completed", unpaid), true, true, 7, DispatchStageKind.ForCollection, "For Collection"));
        Add(DsKind(Of("Completed"), Chain("TRIP-1", "Completed", new LiveTripInvoice { InvoiceId = 9, Status = "Unpaid" }), false, false, 0, DispatchStageKind.ForCollection, "For Collection"));
        Add(DsKind(Of("Completed"), Chain("TRIP-1"), true, true, 7, DispatchStageKind.ForCollection, "For Collection"));
        // Paid: finished.
        var paid = new LiveTripInvoice { InvoiceId = 9, Number = "SI-1", Status = "Paid", SoaId = 7 };
        var ps = DispatchStage.For(Of("Completed"), Chain("TRIP-1", "Completed", paid), true, true, 7);
        if (ps.Kind != DispatchStageKind.Paid || !ps.IsFinished || ps.Clickable || ps.Label != "Completed") bad.Add($"paid: {ps.Kind} '{ps.Label}' finished={ps.IsFinished} clickable={ps.Clickable}");
        // Cancelled: finished, even with leftovers.
        var cs = DispatchStage.For(Of("Cancelled"), Chain("TRIP-1", "Completed", unpaid), true, true, 7);
        if (cs.Kind != DispatchStageKind.Cancelled || !cs.IsFinished || cs.Clickable) bad.Add($"cancelled: {cs.Kind} finished={cs.IsFinished}");
        // Only the four For ... stages are clickable, and an unpaid completed trip is not finished.
        foreach (var k in new[] { DispatchStageKind.Open, DispatchStageKind.Paid, DispatchStageKind.Cancelled })
            if (new DispatchStageInfo { Kind = k }.Clickable) bad.Add(k + " is clickable");
        foreach (var k in new[] { DispatchStageKind.ForTripReport, DispatchStageKind.ForSoa, DispatchStageKind.ForInvoice, DispatchStageKind.ForCollection })
            if (!new DispatchStageInfo { Kind = k }.Clickable || new DispatchStageInfo { Kind = k }.IsFinished || new DispatchStageInfo { Kind = k }.Action.Length == 0) bad.Add(k + " is not clickable / is finished / has no action");
        // The stored status is never touched.
        var d = Of("In Transit"); DispatchStage.For(d, null, false, false, 0);
        if (d.Status != "In Transit") bad.Add("the stored status changed");
        return string.Join("; ", bad);
    }

    private const BindingFlags DsF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>One order in each stage, from real rows: the grid shows the stage words, the filter finds each stage, and the paid one is not in the active list.</summary>
    [Check]
    public static string DispatchStage_Grid_ShowsEachStage_PaidLeavesTheActiveList()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); RunBackfill();
        int scheduled = Seed.Delivery("CLIENT A", "Scheduled");
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")), ("@i", scheduled));
        int noReport = Seed.Delivery("CLIENT A");
        int draft = Seed.Delivery("CLIENT A"); int dr = Seed.TripReport(draft); Exec("UPDATE TripReports SET Status='Draft' WHERE Id=@i", ("@i", dr));
        int forSoa = Seed.Delivery("CLIENT A"); Seed.TripReport(forSoa);
        int cancelledSoa = Seed.Delivery("CLIENT A"); Seed.TripReport(cancelledSoa);
        int soaX = SaveSoaFor("CLIENT A", (cancelledSoa, 1000m, 0m));
        Exec("UPDATE StatementsOfAccount SET Status='Cancelled' WHERE Id=@s", ("@s", soaX));
        int forInvoice = Seed.Delivery("CLIENT A"); Seed.TripReport(forInvoice);
        SaveSoaFor("CLIENT A", (forInvoice, 2000m, 0m));
        int forCollection = Seed.Delivery("CLIENT A"); Seed.TripReport(forCollection);
        int soaC = SaveSoaFor("CLIENT A", (forCollection, 3000m, 0m));
        MJdatabase.SaveInvoiceForSoa(soaC, "SI-9701");
        int paid = Seed.Delivery("CLIENT A"); Seed.TripReport(paid);
        int soaP = SaveSoaFor("CLIENT A", (paid, 4000m, 0m));
        MJdatabase.SaveInvoiceForSoa(soaP, "SI-9702");
        MJdatabase.SaveOneReceipt("9703", TargetFor(soaP), "Cash", "", "");
        int cancelled = Seed.Delivery("CLIENT A", "Cancelled");

        var expected = new Dictionary<int, string>
        {
            [scheduled] = "Scheduled", [noReport] = "For trip report", [draft] = "For trip report", [forSoa] = "For Statement of Account",
            [cancelledSoa] = "For Statement of Account", [forInvoice] = "For Service Invoice", [forCollection] = "For Collection"
        };
        var bad = new List<string>();
        // One map for the whole list (the same call the screen makes).
        var map = DispatchStage.BuildMap(MJdatabase.GetDeliveries());
        foreach (var kv in expected) if (!map.TryGetValue(kv.Key, out var m) || m.Label != kv.Value) bad.Add($"map {kv.Key}: {(map.TryGetValue(kv.Key, out var x) ? x.Label : "missing")} (expected {kv.Value})");
        if (map[paid].Kind != DispatchStageKind.Paid) bad.Add("map: the paid order is " + map[paid].Kind);
        if (map[cancelled].Kind != DispatchStageKind.Cancelled) bad.Add("map: the cancelled order is " + map[cancelled].Kind);
        if (map[forInvoice].SoaId <= 0 || map[forCollection].SoaId <= 0 || map[forCollection].InvoiceId <= 0) bad.Add("map: the statement / invoice ids are missing");
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var grid = DcGet<DataGridView>(view, "gridDispatch");
            view.RefreshData(); Pump(900);
            var statusCol = grid.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "Status");
            Dictionary<int, string> Shown() => grid.Rows.Cast<DataGridViewRow>().ToDictionary(r => ((Delivery)r.DataBoundItem!).Id, r => r.Cells[statusCol.Index].FormattedValue?.ToString() ?? "");
            var msg = new List<string>();
            var shown = Shown();
            foreach (var kv in expected) if (!shown.TryGetValue(kv.Key, out var t) || t != kv.Value) msg.Add($"grid {kv.Key}: '{(shown.TryGetValue(kv.Key, out var y) ? y : "not listed")}' (expected '{kv.Value}')");
            if (shown.ContainsKey(paid)) msg.Add("the paid order is in the active list");
            if (shown.ContainsKey(cancelled)) msg.Add("the cancelled order is in the active list");
            var field = typeof(DispatchManagementView);
            var filter = field.GetField("currentStatusFilter", DsF)!;
            var run = field.GetMethod("FilterGridData", DsF)!;
            // The status filter finds each stage.
            foreach (var (key, ids) in new[] { ("stage:ForTripReport", new[] { noReport, draft }), ("stage:ForSoa", new[] { forSoa, cancelledSoa }), ("stage:ForInvoice", new[] { forInvoice }), ("stage:ForCollection", new[] { forCollection }), ("Completed", new[] { paid }) })
            {
                filter.SetValue(view, key); run.Invoke(view, null); Pump(200);
                var ids2 = Shown().Keys.OrderBy(i => i).ToList();
                if (!ids2.SequenceEqual(ids.OrderBy(i => i))) msg.Add($"filter {key}: {string.Join(",", ids2)} (expected {string.Join(",", ids.OrderBy(i => i))})");
            }
            filter.SetValue(view, "");
            // Show finished brings the paid and cancelled ones back; the paid one reads Completed.
            field.GetField("showFinished", DsF)!.SetValue(view, true); run.Invoke(view, null); Pump(200);
            shown = Shown();
            if (!shown.TryGetValue(paid, out var pt) || pt != "Completed") msg.Add("show finished: the paid order reads '" + (shown.TryGetValue(paid, out var pz) ? pz : "not listed") + "'");
            if (!shown.ContainsKey(cancelled)) msg.Add("show finished: the cancelled order is missing");
            // The count: paid and cancelled are the finished ones.
            var btn = DcGet<Button>(view, "btnShowFinished");
            if (!btn.Text.Contains("(2)")) msg.Add("the Show finished count is: " + btn.Text);
            // Sorted by status: the active ones first, the paid and cancelled ones last.
            var ordered = grid.Rows.Cast<DataGridViewRow>().Select(r => ((Delivery)r.DataBoundItem!).Id).ToList();
            if (ordered.IndexOf(paid) < ordered.IndexOf(forCollection) || ordered.IndexOf(cancelled) < ordered.IndexOf(forCollection)) msg.Add("finished orders are not under the active ones");
            host.Close();
            return string.Join("; ", msg);
        }));
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }
}
