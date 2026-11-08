using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>The dispatch order's store table: no DR No. (Nov 7), only the store and crate columns show.</summary>
public static partial class Checks
{
    [Check]
    public static string DispatchOrder_StoreTable_NoDrNo()
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
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            typeof(DispatchManagementView).GetMethod("RebuildStoreBreakdownGridColumns", F)!.Invoke(view, null);
            var grid = (DataGridView?)typeof(DispatchManagementView).GetField("gridRouteStores", F)!.GetValue(view);
            if (grid == null) return "store table not created";
            var shown = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).Select(c => c.HeaderText).ToList();
            bool drHidden = grid.Columns.Contains("colDrNumbers") && !grid.Columns["colDrNumbers"]!.Visible; // kept (hidden) so saved DR numbers survive an edit
            bool anyDrShown = shown.Any(h => h.Contains("DR", StringComparison.OrdinalIgnoreCase));
            view.Dispose();
            return drHidden && !anyDrShown ? "" : $"DR column hidden: {drHidden}; visible columns: {string.Join(" | ", shown)}";
        });
    }
}
