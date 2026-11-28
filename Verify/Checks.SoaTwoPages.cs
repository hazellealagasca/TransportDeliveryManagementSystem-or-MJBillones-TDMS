using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: the Statement of Account tab is two pages. List (default): title, toolbar with "New Statement", grid. Creation: Back, header, waiting trip reports. Direct starts open the form and return to the list.</summary>
public static partial class Checks
{
    [Check]
    public static string SoaTwoPages_ListIsDefault_NewStatementOpensCreation_BackReturns()
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
            var grid = P5Get<Control>(view, "gridSoa"); var title = P5Get<Control>(view, "lblForCollectionTitle");
            var bar = P5Get<Control>(view, "pnlTopBar"); var header = P5Get<Control>(view, "soaPageHeaderHost");
            var ready = P5Get<Control>(view, "soaReadyBanner"); var back = P5Get<Button>(view, "btnSoaBack");
            var toggle = P5Get<Button>(view, "btnToggleForm"); var form = P5Get<Control>(view, "pnlForm");
            void List(string w) { if (!grid.Visible || !title.Visible || !bar.Visible || header.Visible || ready.Visible || back.Visible || form.Visible) bad.Add($"{w}: not the list page (grid {grid.Visible}, title {title.Visible}, bar {bar.Visible}, header {header.Visible}, waiting {ready.Visible}, back {back.Visible}, form {form.Visible})"); }
            List("default");
            toggle.PerformClick(); Pump(500);
            if (grid.Visible || title.Visible || bar.Visible || form.Visible) bad.Add("creation page still shows the list or the form");
            if (!header.Visible || !ready.Visible || !back.Visible) bad.Add($"creation page incomplete (header {header.Visible}, waiting {ready.Visible}, back {back.Visible})");
            var bp = view.PointToClient(back.PointToScreen(Point.Empty)); var hp = view.PointToClient(header.PointToScreen(Point.Empty));
            if (bp.X > 40 || bp.Y >= hp.Y) bad.Add($"Back is not at the top left (at {bp}, header at {hp})");
            // header + description sit between the Back bar and the waiting panel, and have a real height
            var backBar = P5Get<Control>(view, "soaBackBar");
            int bt = view.PointToClient(backBar.PointToScreen(Point.Empty)).Y, ht = hp.Y, rt = view.PointToClient(ready.PointToScreen(Point.Empty)).Y;
            if (!(bt < ht && ht < rt)) bad.Add("z-order: " + string.Join(",", P5Get<Control>(view, "soaCreatePage").Controls.Cast<Control>().Select(c => c.Name + ":" + c.GetType().Name + ":" + c.Dock + ":" + c.Visible + ":" + c.Top + "+" + c.Height)));
            if (header.Height < 30) bad.Add("the header has no height (" + header.Height + ")");
            if (!(bt < ht && ht < rt)) bad.Add($"order wrong: Back bar {bt}, header {ht}, waiting panel {rt}");
            if (ht < bt + backBar.Height) bad.Add($"header overlaps the Back bar (bar {bt}+{backBar.Height}, header {ht})");
            if (rt < ht + header.Height) bad.Add($"waiting panel overlaps the header (header {ht}+{header.Height}, panel {rt})");
            // waiting list columns: no visible "Invoiced", a visible last "Status" reading the shared stage label, wide enough for it
            var wg = Descend(ready).OfType<DataGridView>().FirstOrDefault();
            if (wg == null) bad.Add("waiting list grid not found");
            else
            {
                var vis = wg.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                if (vis.Any(c => c.HeaderText == "Invoiced")) bad.Add("the Invoiced column is still shown");
                var st = vis.LastOrDefault();
                if (st == null || st.HeaderText != "Status") bad.Add("the last visible column is not Status (" + st?.HeaderText + ")");
                else
                {
                    if (wg.Rows.Count == 0) bad.Add("no waiting rows to check");
                    foreach (DataGridViewRow r in wg.Rows) if ((r.Cells[st.Index].Value as string) != "For Statement of Account") bad.Add("Status cell reads '" + r.Cells[st.Index].Value + "'");
                    using var bf = new Font(wg.Font, FontStyle.Bold);
                    int need = TextRenderer.MeasureText("For Statement of Account", bf).Width;
                    if (st.Width < need) bad.Add($"Status column too narrow ({st.Width} < {need})");
                }
            }
            if (back.Text !="←  Back to statements") bad.Add("Back text: " + back.Text);
            if (ready.Height < 150) bad.Add("the waiting panel is not filling the page (" + ready.Height + ")");
            back.PerformClick(); Pump(300);
            List("after Back");
            toggle.PerformClick(); Pump(300);
            var args = new object[] { new System.Windows.Forms.Message(), Keys.Escape };
            bool handled = (bool)view.GetType().GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, args)!;
            Pump(200);
            if (!handled) bad.Add("Escape on the creation page was not handled");
            List("after Escape");
            host.Close();
            lock (boxes.Boxes) if (boxes.Boxes.Count > 0) bad.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SoaTwoPages_DirectStarts_OpenTheFormAndCloseToTheList()
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
            var grid = P5Get<Control>(view, "gridSoa"); var ready = P5Get<Control>(view, "soaReadyBanner"); var form = P5Get<Control>(view, "pnlForm");
            var toggle = P5Get<Button>(view, "btnToggleForm");
            void Closed(string w) { if (form.Visible || !grid.Visible || ready.Visible) bad.Add($"{w}: not back on the list page (form {form.Visible}, grid {grid.Visible}, waiting {ready.Visible})"); }
            var d = MJdatabase.GetDeliveries().First(x => x.Id == a);
            view.StartSoaForTrip(d); Pump(600);
            if (!form.Visible || grid.Visible || ready.Visible) bad.Add("StartSoaForTrip did not go straight to the form");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after StartSoaForTrip");
            view.StartSoaFor("CLIENT A", DateTime.Today.AddDays(-90), DateTime.Today); Pump(600);
            if (!form.Visible) bad.Add("StartSoaFor did not open the form");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after StartSoaFor");
            // from the creation page: the form opens, closing it lands on the list page
            toggle.PerformClick(); Pump(300);
            view.StartSoaFor("CLIENT A", DateTime.Today.AddDays(-90), DateTime.Today); Pump(600);
            if (!form.Visible || ready.Visible) bad.Add("the form did not replace the creation page");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after the form opened from the creation page");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
