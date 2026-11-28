using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: the Collection Receipts tab is two pages. List (default): "Completed Collection receipts (N)", toolbar with "New Collection Receipt", grid. Creation: Back, "For Collection Receipt (N)", waiting list. Direct starts open the form and close to the list.</summary>
public static partial class Checks
{
    private static (CollectionView View, Form Host) CtOpen()
    {
        var view = new CollectionView { Dock = DockStyle.Fill };
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
        host.Controls.Add(view); host.Show(); Pump(900);
        return (view, host);
    }

    [Check]
    public static string CollectionTwoPages_ListIsDefault_NewReceiptOpensCreation_BackReturns()
    {
        Fresh(); RunBackfill();
        var (soa, ids, no) = CpPaidStatement("0411");
        var (soa2, ids2) = SoaWithThreeInvoices(); // waiting for payment
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = CtOpen();
            using var _ = host;
            var bad = new List<string>();
            var grid = CpField<DataGridView>(view, "gridCollections"); var title = CpField<Label>(view, "lblGridHeader");
            var card = CpField<Control>(view, "pnlGridCard"); var ready = CpField<Control>(view, "collReady");
            var back = CpField<Button>(view, "btnCollBack"); var toggle = CpField<Button>(view, "btnToggleForm");
            var form = CpField<Control>(view, "pnlForm"); var createTitle = CpField<Label>(view, "lblCollCreateTitle");
            var backBar = CpField<Control>(view, "collBackBar"); var hdr = CpField<Control>(view, "collHeaderHost");
            void List(string w)
            {
                if (!grid.Visible || !title.Visible || !toggle.Visible || ready.Visible || back.Visible || form.Visible) bad.Add($"{w}: not the list page (grid {grid.Visible}, title {title.Visible}, button {toggle.Visible}, waiting {ready.Visible}, back {back.Visible}, form {form.Visible})");
                if (!title.Text.StartsWith("Completed Collection receipts (")) bad.Add($"{w}: list title is '{title.Text}'");
            }
            List("default");
            if (toggle.Text != "＋ New Collection Receipt") bad.Add("button text is '" + toggle.Text + "'");
            bool InTree(Control root, Control c) { for (Control? p = c; p != null; p = p.Parent) if (p == root) return true; return false; }
            if (!InTree(card, toggle)) bad.Add("New Collection Receipt is not in the list toolbar");
            toggle.PerformClick(); Pump(500);
            if (grid.Visible || title.Visible || form.Visible) bad.Add("creation page still shows the list or the form");
            if (!ready.Visible || !back.Visible || !createTitle.Visible) bad.Add($"creation page incomplete (waiting {ready.Visible}, back {back.Visible}, title {createTitle.Visible})");
            if (!createTitle.Text.StartsWith("For Collection Receipt (")) bad.Add("creation title is '" + createTitle.Text + "'");
            var bp = view.PointToClient(back.PointToScreen(Point.Empty));
            int bt = view.PointToClient(backBar.PointToScreen(Point.Empty)).Y, ht = view.PointToClient(hdr.PointToScreen(Point.Empty)).Y, rt = view.PointToClient(ready.PointToScreen(Point.Empty)).Y;
            if (bp.X > 40) bad.Add($"Back is not at the left (at {bp})");
            if (!(bt < ht && ht < rt)) bad.Add($"order wrong: Back bar {bt}, header {ht}, waiting panel {rt}");
            if (ht < bt + backBar.Height) bad.Add("header overlaps the Back bar");
            if (rt < ht + hdr.Height) bad.Add("waiting panel overlaps the header");
            if (ready.Height < 150) bad.Add("the waiting panel is not filling the page (" + ready.Height + ")");
            if (back.Text != "←  Back to collection receipts") bad.Add("Back text: " + back.Text);
            var wg = Descend(ready).OfType<DataGridView>().FirstOrDefault();
            if (wg == null) bad.Add("waiting list grid not found");
            else
            {
                var vis = wg.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                var st = vis.LastOrDefault();
                if (st == null || st.HeaderText != "Status") bad.Add("the last visible column is not Status (" + st?.HeaderText + ")");
                else
                {
                    if (wg.Rows.Count == 0) bad.Add("no waiting rows to check");
                    foreach (DataGridViewRow r in wg.Rows) if ((r.Cells[st.Index].Value as string) != "For Collection") bad.Add("Status cell reads '" + r.Cells[st.Index].Value + "'");
                    using var bf = new Font(wg.Font, FontStyle.Bold);
                    int need = TextRenderer.MeasureText("For Collection", bf).Width;
                    if (st.Width < need) bad.Add($"Status column too narrow ({st.Width} < {need})");
                }
                var due = vis.FirstOrDefault(c => c.HeaderText == "Due date");
                if (due == null) bad.Add("no Due date column");
                else foreach (DataGridViewRow r in wg.Rows) if (!((r.Cells[due.Index].Value as string) ?? "").Contains(" · ")) bad.Add("Due date cell lost the due info: '" + r.Cells[due.Index].Value + "'");
                if (!vis.Any(c => c.HeaderText == "Invoiced")) bad.Add("the Invoiced column is gone");
            }
            back.PerformClick(); Pump(300);
            List("after Back");
            toggle.PerformClick(); Pump(300);
            var args = new object[] { new Message(), Keys.Escape };
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
    public static string CollectionTwoPages_DirectStarts_OpenTheFormAndCloseToTheList()
    {
        Fresh(); RunBackfill();
        var (soa, ids) = SoaWithThreeInvoices();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = CtOpen();
            using var _ = host;
            var bad = new List<string>();
            var grid = CpField<DataGridView>(view, "gridCollections"); var ready = CpField<Control>(view, "collReady");
            var form = CpField<Control>(view, "pnlForm"); var toggle = CpField<Button>(view, "btnToggleForm");
            void Closed(string w) { if (form.Visible || !grid.Visible || ready.Visible) bad.Add($"{w}: not back on the list page (form {form.Visible}, grid {grid.Visible}, waiting {ready.Visible})"); }
            view.PrefillForSoa(soa); Pump(600);
            if (!form.Visible || grid.Visible || ready.Visible) bad.Add("PrefillForSoa did not go straight to the form");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after PrefillForSoa");
            // from the creation page: the form replaces it and closing lands on the list
            toggle.PerformClick(); Pump(300);
            if (!ready.Visible) bad.Add("the creation page did not open");
            view.PrefillForSoa(soa); Pump(600);
            if (!form.Visible || ready.Visible) bad.Add("the form did not replace the creation page");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after the form opened from the creation page");
            host.Close();
            lock (boxes.Boxes) if (boxes.Boxes.Count > 0) bad.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
            return string.Join("; ", bad);
        });
    }
}
