using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 8: the dispatch order's store table has tick boxes and "Select all"; several stores are removed together, route stores with a real reason.</summary>
public static partial class Checks
{
    [Check]
    public static string DispatchOrder_StoreTable_PickSeveral_SelectAll_Remove()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            var T = typeof(DispatchManagementView);
            T.GetMethod("RebuildStoreBreakdownGridColumns", F)!.Invoke(view, null);
            var grid = (DataGridView)T.GetField("gridRouteStores", F)!.GetValue(view)!;
            var chkAll = (CheckBox)T.GetField("chkPickAllStores", F)!.GetValue(view)!;
            var remove = (Button)T.GetField("btnRemovePicked", F)!.GetValue(view)!;
            var count = (Label)T.GetField("lblPickCount", F)!.GetValue(view)!;
            var bad = new List<string>();
            // Reason validation: real words only.
            bool Real(string t) => (bool)T.GetMethod("IsRealReason", F)!.Invoke(null, new object[] { t })!;
            if (Real("short") || Real("aaaaaaaaaaaa") || Real("   a b c   ") || !Real("store closed for inventory")) bad.Add("reason validation is wrong");
            // Five stores: two route stores, three added.
            grid.Rows.Clear();
            for (int i = 1; i <= 5; i++)
            {
                int r = grid.Rows.Add();
                grid.Rows[r].Cells["colStoreCode"].Value = "S00" + i;
                grid.Rows[r].Cells["colStoreName"].Value = "Store " + i;
                grid.Rows[r].Cells["colGCrate"].Value = 10; grid.Rows[r].Cells["colECrate"].Value = 5;
                T.GetMethod("SetStoreRowType", F)!.Invoke(view, new object[] { grid.Rows[r], i <= 2 });
            }
            Pump(200);
            if (!grid.Columns.Contains("colPick") || grid.Columns["colPick"]!.DisplayIndex != 0) bad.Add("no tick column first");
            if (remove.Enabled) bad.Add("Remove selected is on with nothing ticked");
            grid.Rows[2].Cells["colPick"].Value = true; grid.Rows[3].Cells["colPick"].Value = true;
            Pump(100);
            if (!remove.Enabled || !remove.Text.Contains("(2)") || !count.Text.StartsWith("2 of 5")) bad.Add($"two ticked: button '{remove.Text}' enabled {remove.Enabled}, count '{count.Text}'");
            if (chkAll.Checked) bad.Add("Select all is ticked with only two ticked");
            chkAll.Checked = true;
            Pump(100);
            if (grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow && !(r.Cells["colPick"].Value is bool b && b)) || !count.Text.StartsWith("5 of 5")) bad.Add($"Select all did not tick every row ('{count.Text}')");
            chkAll.Checked = false;
            Pump(100);
            if (grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow && r.Cells["colPick"].Value is bool b && b) || remove.Enabled) bad.Add("unticking Select all left rows ticked");
            grid.Rows[0].Cells["colPick"].Value = true; grid.Rows[3].Cells["colPick"].Value = true; // one route store and one added store
            Pump(100);
            var picked = ((IEnumerable<DataGridViewRow>)T.GetMethod("PickedStoreRows", F)!.Invoke(view, null)!).ToList();
            if (picked.Count != 2) bad.Add($"{picked.Count} picked, 2 expected");
            T.GetMethod("ApplyStoreRowRemoval", F)!.Invoke(view, new object?[] { picked, "store closed for inventory today" });
            Pump(100);
            var left = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(r => r.Cells["colStoreCode"].Value?.ToString()).ToList();
            if (!left.SequenceEqual(new[] { "S002", "S003", "S005" })) bad.Add("left: " + string.Join(",", left));
            if (remove.Enabled || chkAll.Checked) bad.Add("the bar did not reset after removing");
            if (MJdatabase.GetAuditTrail(null, null, 50).Count(a => a.FieldName == "RouteStoreRemoved") != 1) bad.Add("the route store's removal was not logged once");
            view.Dispose();
            return string.Join("; ", bad);
        });
    }
}
