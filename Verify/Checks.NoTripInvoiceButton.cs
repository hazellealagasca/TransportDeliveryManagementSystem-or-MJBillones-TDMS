using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

public static partial class Checks
{
    private static void CollectTexts(Control c, List<string> into)
    {
        if ((c is ButtonBase || c is Label) && !string.IsNullOrWhiteSpace(c.Text)) into.Add(c.Text);
        foreach (Control k in c.Controls) CollectTexts(k, into);
    }
    private static void CollectMenu(ToolStripItemCollection items, List<string> into)
    {
        foreach (ToolStripItem i in items)
        {
            into.Add(i.Text ?? "");
            if (i is ToolStripMenuItem mi) CollectMenu(mi.DropDownItems, into);
        }
    }

    /// <summary>Nov 13 (statement first): a completed trip with a trip report and no invoice offers no invoice button and no "Proceed to SOA" button.</summary>
    [Check]
    public static string NoTripInvoiceButton_CompletedReportedTrip_NoInvoiceOrSoaButton()
    {
        Fresh();
        int id = Seed.Delivery(); Seed.TripReport(id);
        var d = MJdatabase.GetDeliveryById(id)!;
        return RunOnSta(() =>
        {
            var problems = new List<string>();
            bool Bad(string t) => t.Contains("Create invoice", StringComparison.OrdinalIgnoreCase) || t.Contains("Proceed to SOA", StringComparison.OrdinalIgnoreCase) || t.Contains("Create billing invoice", StringComparison.OrdinalIgnoreCase);

            var form = ShowForAndGet(p => Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.DispatchDetailsDialog.ShowFor(p, d));
            try
            {
                Pump(800);
                var texts = new List<string>(); CollectTexts(form, texts);
                form.GetType().GetMethod("ShowTab", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, new object[] { "Documents" }); Pump(400);
                CollectTexts(form, texts);
                foreach (var t in texts.Where(Bad)) problems.Add("dialog shows: " + t);
                if (!texts.Any(t => t.Contains("Next: it is billed on a Statement of Account"))) problems.Add("Next bar text missing");
                if (!texts.Any(t => t == "Billed on its Statement of Account.")) problems.Add("Invoice row subtitle missing");
                var more = (ContextMenuStrip)form.GetType().GetMethod("BuildMoreMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!;
                var items = new List<string>(); CollectMenu(more.Items, items);
                foreach (var t in items.Where(Bad)) problems.Add("More menu has: " + t);
                if (items.Count == 0) problems.Add("More menu is empty");
            }
            finally { form.Close(); }

            var view = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.DispatchManagementView();
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show(); Pump(300);
            try
            {
                var menu = new ContextMenuStrip();
                view.GetType().GetMethod("BuildSingleMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { menu, d });
                var items = new List<string>(); CollectMenu(menu.Items, items);
                foreach (var t in items.Where(Bad)) problems.Add("row menu has: " + t);
                if (items.Count == 0) problems.Add("row menu is empty");
            }
            finally { host.Close(); }
            return string.Join("; ", problems);
        });
    }
}
