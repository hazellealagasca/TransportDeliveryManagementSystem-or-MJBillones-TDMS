using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: the Service Invoices tab is two pages. List (default): "Service Invoice (N)" title, toolbar with "New Service Invoice", grid. Creation: Back, title, waiting statements. Direct starts open the form and return to the list.</summary>
public static partial class Checks
{
    [Check]
    public static string InvoiceTwoPages_ListIsDefault_NewInvoiceOpensCreation_BackReturns()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = IwMakeView();
            using var _ = host;
            var bad = new List<string>();
            var grid = IwGet<DataGridView>(view, "gridInvoices"); var title = IwGet<Label>(view, "lblGridHeader");
            var bar = IwGet<Control>(view, "pnlGridHeaderBar"); var page = FieldOf<Control>(view, "billCreatePage")!;
            var back = FieldOf<Button>(view, "btnBillBack")!; var toggle = FieldOf<Button>(view, "btnToggleForm")!; var form = IwGet<Control>(view, "pnlForm");
            var backBar = FieldOf<Control>(view, "billBackBar")!; var head = FieldOf<Control>(view, "billPageHeaderHost")!;
            var waiting = FieldOf<Control>(view, "billWaiting")!;
            void List(string w)
            {
                if (!grid.Visible || !bar.Visible || !toggle.Visible || page.Visible || waiting.Visible || back.Visible || form.Visible) bad.Add($"{w}: not the list page (grid {grid.Visible}, bar {bar.Visible}, New {toggle.Visible}, create page {page.Visible}, waiting {waiting.Visible}, back {back.Visible}, form {form.Visible})");
                if (!title.Text.StartsWith("Service Invoice (")) bad.Add($"{w}: title is '{title.Text}'");
            }
            List("default");
            if (toggle.Text != "＋ New Service Invoice") bad.Add("button text: " + toggle.Text);
            if (view.Controls.Find("pnlForCollectionHeading", true).Any(c => c.Visible)) bad.Add("a second For collection heading is still shown");
            toggle.PerformClick(); Pump(500);
            if (grid.Visible || bar.Visible || form.Visible || toggle.Visible) bad.Add("creation page still shows the list or the form");
            if (!page.Visible || !head.Visible || !waiting.Visible || !back.Visible) bad.Add($"creation page incomplete (page {page.Visible}, header {head.Visible}, waiting {waiting.Visible}, back {back.Visible})");
            if (back.Text != "←  Back to service invoices") bad.Add("Back text: " + back.Text);
            var hl = Descend(head).OfType<Label>().Select(l => l.Text).ToList();
            if (!hl.Contains("Creation of Service Invoice")) bad.Add("creation title missing: " + string.Join(" | ", hl));
            if (!hl.Any(t => t.StartsWith("A service invoice bills one Statement of Account"))) bad.Add("description missing");
            var bp = view.PointToClient(back.PointToScreen(Point.Empty)); var hp = view.PointToClient(head.PointToScreen(Point.Empty));
            int bt = view.PointToClient(backBar.PointToScreen(Point.Empty)).Y, rt = view.PointToClient(waiting.PointToScreen(Point.Empty)).Y;
            if (!(bt < hp.Y && hp.Y < rt)) bad.Add($"order wrong: Back bar {bt}, header {hp.Y}, waiting list {rt}");
            if (bp.X > 80 || bp.Y >= hp.Y) bad.Add($"Back is not at the top left (at {bp}, header at {hp})");
            if (waiting.Height < 150) bad.Add("the waiting list is not filling the page (" + waiting.Height + ")");
            var wg = Descend(waiting).OfType<DataGridView>().FirstOrDefault();
            if (wg == null) bad.Add("waiting list grid not found");
            else
            {
                var vis = wg.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                if (vis.Any(c => c.HeaderText == "Invoiced")) bad.Add("an Invoiced column is shown");
                var st = vis.LastOrDefault();
                if (st == null || st.HeaderText != "Status") bad.Add("the last visible column is not Status (" + st?.HeaderText + ")");
                else
                {
                    if (wg.Rows.Count == 0) bad.Add("no waiting rows to check");
                    foreach (DataGridViewRow r in wg.Rows) if ((r.Cells[st.Index].Value as string) != "For Service Invoice") bad.Add("Status cell reads '" + r.Cells[st.Index].Value + "'");
                    using var bf = new Font(wg.Font, FontStyle.Bold);
                    int need = TextRenderer.MeasureText("For Service Invoice", bf).Width;
                    if (st.Width < need) bad.Add($"Status column too narrow ({st.Width} < {need})");
                }
            }
            back.PerformClick(); Pump(300);
            List("after Back");
            toggle.PerformClick(); Pump(300);
            var args = new object[] { new System.Windows.Forms.Message(), Keys.Escape };
            bool handled = (bool)view.GetType().GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, args)!;
            Pump(200);
            if (!handled) bad.Add("Escape on the creation page was not handled");
            List("after Escape");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string InvoiceTwoPages_DirectStart_OpensTheFormAndClosesToTheList()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = IwMakeView();
            using var _ = host;
            var bad = new List<string>();
            var grid = IwGet<DataGridView>(view, "gridInvoices"); var page = FieldOf<Control>(view, "billCreatePage")!; var form = IwGet<Control>(view, "pnlForm");
            var toggle = FieldOf<Button>(view, "btnToggleForm")!;
            void Closed(string w) { if (form.Visible || !grid.Visible || page.Visible) bad.Add($"{w}: not back on the list page (form {form.Visible}, grid {grid.Visible}, create page {page.Visible})"); }
            view.StartForStatement(soaId); Pump(600);
            if (!form.Visible || grid.Visible || page.Visible) bad.Add("StartForStatement did not go straight to the form");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after StartForStatement");
            // from the creation page: the form opens, closing it lands on the list page
            toggle.PerformClick(); Pump(300);
            view.StartForStatement(soaId); Pump(600);
            if (!form.Visible || page.Visible) bad.Add("the form did not replace the creation page");
            view.DiscardAndCloseForm(); Pump(300);
            Closed("after the form opened from the creation page");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
