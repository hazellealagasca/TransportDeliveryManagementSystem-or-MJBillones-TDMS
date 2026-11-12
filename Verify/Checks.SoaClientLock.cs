using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 13: the statement form has no SOA type box (special trips are off), and its client details (address, TIN) come from the client's profile and are read-only.</summary>
public static partial class Checks
{
    private const string LockTin = "123-456-789-000", LockAddress = "12 PROFILE ST., TARLAC CITY";

    private static void LockClient(string name = "CLIENT A") =>
        MJdatabase.SaveClient(new ClientCompany { CompanyName = name, TIN = LockTin, Address = LockAddress });

    private static (StatementOfAccountView view, Form host) LockOpen()
    {
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1400, 900), Opacity = 0 };
        var view = new StatementOfAccountView { Dock = DockStyle.Fill };
        host.Controls.Add(view); host.Show();
        Pump(600);
        return (view, host);
    }

    private static T? LockMember<T>(object o, string name) where T : class
    {
        var f = o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return f?.GetValue(o) as T;
    }


    [Check]
    public static string SoaClientLock_SoaTypeBoxHidden_AndStartSoaForFillsReadOnlyClientDetails()
    {
        Fresh(); RunBackfill(); LockClient();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int b = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b);
        return RunOnSta(() =>
        {
            var (view, host) = LockOpen();
            using var _ = host;
            var cbType = LockMember<ComboBox>(view, "cbSoaType");
            var cbClient = LockMember<ComboBox>(view, "cbClient");
            var addr = LockMember<TextBox>(view, "txtClientAddress");
            var tin = LockMember<TextBox>(view, "txtClientTin");
            if (cbType == null || cbClient == null || addr == null || tin == null) return "form fields not found";
            if (cbType.Visible || cbType.Parent != null) return $"SOA type box is on the form (visible {cbType.Visible}, parent {cbType.Parent?.GetType().Name})";
            var fromBoxes = view.GetType().GetProperty("CurrentSoaType", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(view) as string;
            if (fromBoxes != SoaTypes.Normal) return $"the form's SOA type is '{fromBoxes}', not Normal";
            if (!addr.ReadOnly || !tin.ReadOnly || addr.TabStop || tin.TabStop) return $"address read-only {addr.ReadOnly}/tabstop {addr.TabStop}, TIN read-only {tin.ReadOnly}/tabstop {tin.TabStop}";
            if (addr.BackColor != tin.BackColor) return "address and TIN boxes are not the same grey";

            var q = MJdatabase.GetTripReportsWaitingForStatement().Where(c => c.ClientName == "CLIENT A").ToList();
            var rows = q.Select(c => new ReadyInvoiceRow { InvoiceId = c.DeliveryId, InvoiceNumber = c.TripReportNumber, TripNumber = c.DispatchNumber, ClientName = c.ClientName, TripDate = DateTime.Today }).ToList();
            view.StartSoaFor("CLIENT A", rows);
            Pump(900);
            if (cbClient.Text.Trim() != "CLIENT A") return $"client is '{cbClient.Text}'";
            if (addr.Text != LockAddress) return $"address '{addr.Text}'";
            if (tin.Text != LockTin) return $"TIN '{tin.Text}'";
            var grid = LockMember<DataGridView>(view, "gridItems");
            if (grid == null || grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow) == 0) return "no trips were added";
            if (cbClient.Enabled) return "the client can still be changed while trips are on the statement";
            host.Close();
            return "";
        });
    }

    [Check]
    public static string SoaClientLock_PickingTheClientAndAddingAllReadyTrips_FillsTheDetails()
    {
        Fresh(); RunBackfill(); LockClient();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        return RunOnSta(() =>
        {
            var (view, host) = LockOpen();
            using var _ = host;
            var cbClient = LockMember<ComboBox>(view, "cbClient")!;
            var addr = LockMember<TextBox>(view, "txtClientAddress")!;
            var tin = LockMember<TextBox>(view, "txtClientTin")!;
            view.GetType().GetMethod("OpenNewStatement", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(view, null);
            Pump(300);
            // typing the client text (no list pick) and adding the ready trips must still fill the details from the profile
            var binding = view.GetType().GetField("isBindingForm", BindingFlags.Instance | BindingFlags.NonPublic)!;
            binding.SetValue(view, true); cbClient.Text = "CLIENT A"; binding.SetValue(view, false);   // set the text without the pick event
            addr.Text = ""; tin.Text = "";
            view.GetType().GetMethod("PopulateTripsFromCompletedDeliveries", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null);
            Pump(900);
            string r = addr.Text == LockAddress && tin.Text == LockTin ? "" : $"after Add all ready trips: address '{addr.Text}', TIN '{tin.Text}'";
            host.Close();
            return r;
        });
    }

    [Check]
    public static string SoaClientLock_Save_TakesAddressAndTinFromTheProfile_NotTheBoxes()
    {
        Fresh(); RunBackfill(); LockClient();
        return RunOnSta(() =>
        {
            var (view, host) = LockOpen();
            using var _ = host;
            var addr = LockMember<TextBox>(view, "txtClientAddress")!;
            var tin = LockMember<TextBox>(view, "txtClientTin")!;
            var apply = view.GetType().GetMethod("ApplyClientProfile", BindingFlags.Instance | BindingFlags.NonPublic)!;   // the step Save runs on the statement it builds
            addr.Text = "TYPED IN THE BOX"; tin.Text = "999-999-999-000";   // a stale or edited box must not reach the saved statement
            var soa = new StatementOfAccount { ClientName = "CLIENT A" };
            apply.Invoke(view, new object[] { soa });
            if (soa.ClientAddress != LockAddress || soa.ClientTin != LockTin) return $"saved address '{soa.ClientAddress}', TIN '{soa.ClientTin}' (expected the profile's)";
            var none = new StatementOfAccount { ClientName = "NO SUCH CLIENT" };   // no profile: the boxes are the fallback
            apply.Invoke(view, new object[] { none });
            if (none.ClientAddress != "TYPED IN THE BOX") return $"no-profile client: address '{none.ClientAddress}'";
            // a saved statement opens with its own saved details, read-only, and its type is Normal
            var saved = new StatementOfAccount { ClientName = "CLIENT A", ClientAddress = "SAVED ADDRESS", ClientTin = "111-222-333-000", PeriodCovered = "2026-11-01 to 2026-11-09", IssueDate = DateTime.Today, DeliveryType = "REF", SoaType = SoaTypes.Normal };
            view.GetType().GetMethod("BindSoaToDrawer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(view, new object[] { saved });
            if (addr.Text != "SAVED ADDRESS" || tin.Text != "111-222-333-000" || !addr.ReadOnly || !tin.ReadOnly) return $"saved statement shows address '{addr.Text}', TIN '{tin.Text}'";
            if ((view.GetType().GetProperty("CurrentSoaType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view) as string) != SoaTypes.Normal) return "a saved statement is not Normal";
            host.Close();
            return "";
        });
    }

    [Check]
    public static string SoaClientLock_Step1_LaysOutCleanly_AtNormalAndLargePrint()
    {
        Fresh(); RunBackfill(); LockClient();
        return RunOnSta(() =>
        {
            UiZoom.PersistToDisk = false;
            try
            {
                UiZoom.Set(1f);
                var (view, host) = LockOpen();
                using var _ = host;
                var addr = LockMember<TextBox>(view, "txtClientAddress")!;
                view.GetType().GetMethod("OpenNewStatement", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(view, null);
                Pump(700);
                string Check(string when)
                {
                    var table = addr.Parent!.Parent as TableLayoutPanel;
                    if (table == null) return when + ": field grid not found";
                    var cells = table.Controls.Cast<Control>().ToList();
                    if (cells.Count != 6) return $"{when}: {cells.Count} field cells (6 expected, no hole)";
                    for (int i = 0; i < cells.Count; i++)
                    {
                        var c = cells[i];
                        for (int j = i + 1; j < cells.Count; j++) if (c.Bounds.IntersectsWith(cells[j].Bounds)) return $"{when}: cells {i} {c.Bounds} and {j} {cells[j].Bounds} overlap (table {table.Size})";
                        if (c.Bottom > table.Height + 1) return $"{when}: a cell runs past the grid ({c.Bottom} > {table.Height})";
                        var kids = c.Controls.Cast<Control>().Where(k => k.Visible).OrderBy(k => k.Top).ToList();
                        for (int k = 0; k + 1 < kids.Count; k++) if (kids[k].Bounds.IntersectsWith(kids[k + 1].Bounds)) return $"{when}: label and box overlap in cell {i}";
                        if (kids.Any(k => k.Bottom > c.Height + 1)) return $"{when}: a box is cut off by its cell {i}";
                    }
                    return "";
                }
                string r = Check("normal print");
                if (r.Length > 0) return r;
                UiZoom.Set(1.5f); UiZoom.Apply(view); Pump(500);
                r = Check("large print");
                UiZoom.Set(1f); UiZoom.Apply(view); Pump(300);
                host.Close();
                return r;
            }
            finally { UiZoom.Set(1f); UiZoom.PersistToDisk = true; }
        });
    }
}
