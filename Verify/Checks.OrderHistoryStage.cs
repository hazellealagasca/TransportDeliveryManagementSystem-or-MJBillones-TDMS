using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: Order History shows the same stage as the Dispatch list, lists the active orders first (Active, then Completed and cancelled),
/// has an Active tab, and its Completed tab holds only the paid orders.</summary>
public static partial class Checks
{
    private const BindingFlags OhF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The grid pill painter (UIThemeHelper.StatusColors, keyed by the cell text) gives every stage word the shared colours, each stage its own.</summary>
    [Check]
    public static string DispatchStage_GridPillPainter_UsesTheSharedStageColours()
    {
        var bad = new List<string>();
        var seen = new Dictionary<string, string>();
        var stages = new (string Label, DispatchStageKind Kind)[]
        {
            ("For trip report", DispatchStageKind.ForTripReport), ("For Statement of Account", DispatchStageKind.ForSoa), ("For Service Invoice", DispatchStageKind.ForInvoice),
            ("For Collection", DispatchStageKind.ForCollection), ("Completed", DispatchStageKind.Paid), ("Cancelled", DispatchStageKind.Cancelled)
        };
        foreach (var (label, kind) in stages)
        {
            var (bg, fg, _) = UIThemeHelper.StatusColors(label);
            var want = DispatchStage.PillColors(kind)!.Value;
            if (bg != want.Back || fg != want.Fore) bad.Add($"'{label}' is painted {bg}/{fg}, not the shared {want.Back}/{want.Fore}");
            string key = bg.ToArgb() + "/" + fg.ToArgb();
            if (seen.TryGetValue(key, out var other)) bad.Add($"'{label}' has the same colours as '{other}'"); else seen[key] = label;
        }
        foreach (var (label, status) in new[] { ("Current", "In Transit"), ("Scheduled", "Scheduled"), ("Delayed", "Delayed") })
        {
            var (bg, fg, _) = UIThemeHelper.StatusColors(label);
            var want = DispatchStage.OpenPillColors(status);
            if (bg != want.Back || fg != want.Fore) bad.Add($"'{label}' is painted {bg}/{fg}, not the shared {want.Back}/{want.Fore}");
            string key = bg.ToArgb() + "/" + fg.ToArgb();
            if (seen.TryGetValue(key, out var other)) bad.Add($"'{label}' has the same colours as '{other}'"); else seen[key] = label;
        }
        return string.Join("; ", bad);
    }

    [Check]
    public static string OrderHistoryStage_ShowsTheStage_ActiveFirst_ActiveTab_CompletedIsPaid()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations(); RunBackfill();
        int scheduled = Seed.Delivery("CLIENT A", "Scheduled");
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")), ("@i", scheduled));
        int noReport = Seed.Delivery("CLIENT A");
        int forSoa = Seed.Delivery("CLIENT A"); Seed.TripReport(forSoa);
        int forInvoice = Seed.Delivery("CLIENT A"); Seed.TripReport(forInvoice);
        SaveSoaFor("CLIENT A", (forInvoice, 2000m, 0m));
        int forCollection = Seed.Delivery("CLIENT A"); Seed.TripReport(forCollection);
        int soaC = SaveSoaFor("CLIENT A", (forCollection, 3000m, 0m));
        MJdatabase.SaveInvoiceForSoa(soaC, "SI-9801");
        int paid = Seed.Delivery("CLIENT A"); Seed.TripReport(paid);
        int soaP = SaveSoaFor("CLIENT A", (paid, 4000m, 0m));
        MJdatabase.SaveInvoiceForSoa(soaP, "SI-9802");
        MJdatabase.SaveOneReceipt("9803", TargetFor(soaP), "Cash", "", "");
        int cancelled = Seed.Delivery("CLIENT A", "Cancelled");

        var expected = new Dictionary<int, string>
        {
            [scheduled] = "Scheduled", [noReport] = "For trip report", [forSoa] = "For Statement of Account", [forInvoice] = "For Service Invoice",
            [forCollection] = "For Collection", [paid] = "Completed", [cancelled] = "Cancelled"
        };
        var activeIds = new HashSet<int> { scheduled, noReport, forSoa, forInvoice, forCollection };
        var bad = new List<string>();
        // The index: every dispatch row carries the stage of the Dispatch list.
        var recs = TransactionIndex.LoadAll(true);
        var byId = recs.Where(r => r.Source is Delivery).ToDictionary(r => ((Delivery)r.Source!).Id);
        foreach (var kv in expected)
            if (!byId.TryGetValue(kv.Key, out var rec) || rec.StageLabel != kv.Value) bad.Add($"index {kv.Key}: '{(byId.TryGetValue(kv.Key, out var x) ? x.StageLabel : "missing")}' (expected '{kv.Value}')");
        bad.AddRange(byId.Values.Where(r => activeIds.Contains(((Delivery)r.Source!).Id) != r.IsActive).Select(r => "IsActive wrong for " + r.Number));
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = new OrderHistoryView();
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(1500);
            var t = typeof(OrderHistoryView);
            var tabs = (SegmentTabs)t.GetField("tabs", OhF)!.GetValue(view)!;
            var grid = (DataGridView)t.GetField("grid", OhF)!.GetValue(view)!;
            var sort = t.GetField("sortChip", OhF)!.GetValue(view)!;
            var shownF = t.GetField("shown", OhF)!;
            var msg = new List<string>();
            List<TxRecord> Shown() => (List<TxRecord>)shownF.GetValue(view)!;
            HashSet<int> Ids(IEnumerable<TxRecord> l) => l.Where(r => r.Source is Delivery).Select(r => ((Delivery)r.Source!).Id).ToHashSet();
            // The tab names and counts.
            var labels = (string[])typeof(SegmentTabs).GetField("labels", OhF)!.GetValue(tabs)!;
            if (labels.Length < 3 || !labels[1].Contains("Active") || !labels[2].Contains("Completed")) msg.Add("tabs: " + string.Join(" | ", labels));
            // All tab: Active part first, then Completed & cancelled; the rows read their stage words.
            tabs.Selected = 0; Pump(300);
            var gridRows = grid.Rows.Cast<DataGridViewRow>().ToList();
            var heads = gridRows.Where(r => r.Tag != null && r.Tag.GetType().Name == "GroupTag").Select(r => (string)r.Tag!.GetType().GetField("Title")!.GetValue(r.Tag)!).ToList();
            if (heads.Count == 0 || heads[0] != $"Active ({activeIds.Count})") msg.Add("first heading: " + (heads.Count == 0 ? "none" : heads[0]));
            if (!heads.Any(h => h == "Completed & cancelled (2)")) msg.Add("headings: " + string.Join(" | ", heads));
            void ActiveBeforeDone(string when)
            {
                var order = grid.Rows.Cast<DataGridViewRow>().Where(r => r.Tag is TxRecord).Select(r => (TxRecord)r.Tag!).ToList();
                int lastActive = order.FindLastIndex(r => r.IsActive), firstDone = order.FindIndex(r => !r.IsActive);
                if (firstDone >= 0 && lastActive > firstDone) msg.Add("active rows are not all before the paid / cancelled ones (" + when + ")");
                var done = order.Where(r => r.Source is Delivery d && (d.Id == paid || d.Id == cancelled)).ToList();
                if (done.Count != 2) msg.Add("completed & cancelled rows listed: " + done.Count + " (" + when + ")");
            }
            ActiveBeforeDone("newest first");
            foreach (var r in Shown().Where(r => r.Source is Delivery d && expected.ContainsKey(d.Id)))
                if (r.StageLabel != expected[((Delivery)r.Source!).Id]) msg.Add($"row {r.Number} reads '{r.StageLabel}'");
            // The sort chip still works inside the parts.
            sort.GetType().GetProperty("SelectedIndex")!.SetValue(sort, 1); Pump(300);
            ActiveBeforeDone("oldest first");
            sort.GetType().GetProperty("SelectedIndex")!.SetValue(sort, 0); Pump(300);
            // Tab counts (badges) and contents.
            tabs.Selected = 0; Pump(200);
            int Badge(int i) => tabs.Badges.Length > i ? tabs.Badges[i] : -1;
            tabs.Selected = 1; Pump(300);   // Active
            if (!Ids(Shown()).SetEquals(activeIds) || Shown().Any(r => !r.IsActive)) msg.Add("Active tab: " + string.Join(",", Ids(Shown()).OrderBy(i => i)));
            if (Badge(1) != Shown().Count) msg.Add($"Active badge {Badge(1)} vs {Shown().Count} rows");
            tabs.Selected = 2; Pump(300);   // Completed = paid only
            if (!Ids(Shown()).SetEquals(new[] { paid }) || Shown().Any(r => r.StageLabel != "Completed")) msg.Add("Completed tab: " + string.Join(",", Shown().Select(r => r.Number + ":" + r.StageLabel)));
            if (Badge(2) != Shown().Count) msg.Add($"Completed badge {Badge(2)} vs {Shown().Count} rows");
            tabs.Selected = 3; Pump(300);   // Cancelled
            if (!Ids(Shown()).SetEquals(new[] { cancelled })) msg.Add("Cancelled tab: " + string.Join(",", Ids(Shown())));
            tabs.Selected = 4; Pump(300);   // Dispatches: still both parts
            if (!Ids(Shown()).SetEquals(expected.Keys)) msg.Add("Dispatches tab lists " + Ids(Shown()).Count + " of " + expected.Count);
            host.Close();
            return string.Join("; ", msg);
        }));
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }
}
