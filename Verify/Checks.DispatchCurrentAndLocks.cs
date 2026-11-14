using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Phase 3 (operations): "In Transit" reads "Current" on screen (the stored value stays), the dispatch number is generated and read-only,
/// the departure time is never filled in by the system, the stores table has a "Show store addresses" tick, "No. of drops" is the store
/// count, and the Row Type column is hidden while its lock behaviour stays.
/// </summary>
public static partial class Checks
{
    private const BindingFlags DcF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    private static DispatchManagementView DcMakeView(out Form host)
    {
        var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
        host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
        view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
        Pump(800);
        return view;
    }

    private static T DcGet<T>(object view, string field) where T : class => (T)typeof(DispatchManagementView).GetField(field, DcF)!.GetValue(view)!;

    [Check]
    public static string DispatchCurrent_InTransitShowsCurrent_StoredValueUnchanged()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        if (StatusText.Display("In Transit") != "Current") bad.Add("Display(In Transit) = " + StatusText.Display("In Transit"));
        if (StatusText.Display("in transit") != "Current") bad.Add("Display is case sensitive");
        foreach (var s in new[] { "Scheduled", "Completed", "Cancelled", "Delayed", "Pending", "" })
            if (StatusText.Display(s) != s) bad.Add("Display changed '" + s + "'");
        if (StatusText.Display(null) != "") bad.Add("Display(null) is not blank");
        int id = Seed.Delivery("CLIENT A", "In Transit");
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")), ("@i", id));
        if (MJdatabase.GetDeliveries().First(d => d.Id == id).Status != "In Transit") bad.Add("the stored status changed");
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var grid = DcGet<DataGridView>(view, "gridDispatch");
            view.RefreshData(); Pump(600);
            var statusCol = grid.Columns.Cast<DataGridViewColumn>().First(c => c.DataPropertyName == "Status");
            var shown = grid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells[statusCol.Index].FormattedValue?.ToString() ?? "").ToList();
            var msg = new List<string>();
            if (!shown.Contains("Current")) msg.Add("the grid does not show Current: " + string.Join("|", shown));
            if (shown.Contains("In Transit")) msg.Add("the grid still shows In Transit");
            // The status list on the form shows Current, but the chosen item is the stored value.
            var cb = DcGet<ComboBox>(view, "cbStatus");
            int at = cb.Items.IndexOf("In Transit");
            if (at < 0) msg.Add("no In Transit item");
            else
            {
                if (cb.GetItemText(cb.Items[at]) != "Current") msg.Add("status list text is " + cb.GetItemText(cb.Items[at]));
                cb.SelectedIndex = at;
                if (cb.SelectedItem as string != "In Transit") msg.Add("selected item is not the stored value");
            }
            // The status filter offers Current but filters on In Transit.
            var opts = (Array)typeof(DispatchManagementView).GetField("StatusOptions", DcF)!.GetValue(null)!;
            bool found = false;
            foreach (var o in opts) { var t = ((string, string))o; if (t.Item1 == "Current" && t.Item2 == "In Transit") found = true; if (t.Item1 == "In Transit") msg.Add("filter still labelled In Transit"); }
            if (!found) msg.Add("no Current filter on In Transit");
            host.Close();
            return string.Join("; ", msg);
        }));
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    [Check]
    public static string DispatchNumber_IsReadOnly_Generated_OnNewAndEdit()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int id = Seed.Delivery("CLIENT A", "Scheduled");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var box = DcGet<TextBox>(view, "txtDispatchNumber");
            var bad = new List<string>();
            if (!box.ReadOnly) bad.Add("box is not read-only at start");
            if (box.TabStop) bad.Add("box is a tab stop");
            view.OpenNewOrderForm(); Pump(300);
            if (!box.ReadOnly || box.TabStop) bad.Add("box is editable on a new order");
            DcGet<ComboBox>(view, "cbClient").Text = "CLIENT A"; Pump(400);
            if (box.Text.Trim().Length == 0) bad.Add("no number was generated for a new order");
            typeof(DispatchManagementView).GetMethod("BindDeliveryToForm", DcF)!.Invoke(view, new object?[] { MJdatabase.GetDeliveries().First(d => d.Id == id) });
            Pump(300);
            if (!box.ReadOnly) bad.Add("box is editable when an order is opened");
            if (box.Text != MJdatabase.GetDeliveries().First(d => d.Id == id).DispatchNumber) bad.Add("the saved number was not kept");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string DispatchTime_NoSuggestion_RequiredOnSave_RouteClientDateKeepIt()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        if (new Delivery().DepartureTime != "") bad.Add("Delivery default time is '" + new Delivery().DepartureTime + "'");
        if (new LoadingSchedule().DepartureTime != "") bad.Add("LoadingSchedule default time is '" + new LoadingSchedule().DepartureTime + "'");
        if (TimeText.Normalize("", "") != "") bad.Add("blank time was filled in");
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var msg = new List<string>();
            var time = DcGet<ComboBox>(view, "cbDepartureTime");
            if (time.Items.Count != 0) msg.Add("the time list still has preset times: " + string.Join(", ", time.Items.Cast<object>()));
            view.OpenNewOrderForm(); Pump(300);
            if (time.Text.Length != 0) msg.Add("a new order has the time '" + time.Text + "'");
            // Saving without a time is refused with a message.
            int before = boxes.Boxes.Count;
            typeof(DispatchManagementView).GetMethod("BtnSave_Click", DcF)!.Invoke(view, new object?[] { null, EventArgs.Empty });
            Pump(1200);
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Departure Time is required"))) msg.Add("no 'time is required' message: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            if (MJdatabase.GetDeliveries().Count != 0) msg.Add("an order without a time was saved");
            // Route, client and date never change the time.
            time.Text = "09:30 AM";
            DcGet<ComboBox>(view, "cbClient").Text = "CLIENT A"; Pump(300);
            if (!time.Text.Equals("09:30 AM", StringComparison.OrdinalIgnoreCase)) msg.Add("picking the client changed the time to '" + time.Text + "'");
            var dest = DcGet<ComboBox>(view, "cbDestination");
            dest.Text = "Route 1"; Pump(300);
            if (!time.Text.Equals("09:30 AM", StringComparison.OrdinalIgnoreCase)) msg.Add("picking the route changed the time to '" + time.Text + "'");
            DcGet<DateTimePicker>(view, "dtpDispatchDate").Value = DateTime.Today.AddDays(3); Pump(300);
            if (!time.Text.Equals("09:30 AM", StringComparison.OrdinalIgnoreCase)) msg.Add("changing the date changed the time to '" + time.Text + "'");
            host.Close();
            return string.Join("; ", msg);
        }));
        // The Reschedule dialog: a blank time stays blank and is refused.
        bad.Add(RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var d = new Delivery { Id = 1, DispatchNumber = "RS-0001", ClientName = "CLIENT A", Destination = "Route 1", DispatchDate = DateTime.Today.AddDays(1), DepartureTime = "", Status = "Scheduled" };
            using var dlg = new RescheduleDeliveryDialog(d);
            var msg = new List<string>();
            var txt = (TextBox)typeof(RescheduleDeliveryDialog).GetField("txtNewTime", DcF)!.GetValue(dlg)!;
            if (txt.Text.Length != 0) msg.Add("Reschedule time starts as '" + txt.Text + "'");
            int before = boxes.Boxes.Count;
            typeof(RescheduleDeliveryDialog).GetMethod("BtnConfirm_Click", DcF)!.Invoke(dlg, new object?[] { null, EventArgs.Empty });
            Pump(800);
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("valid departure time"))) msg.Add("Reschedule did not refuse a blank time: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            return string.Join("; ", msg);
        }));
        return string.Join("; ", bad.Where(b => b.Length > 0));
    }

    [Check]
    public static string DispatchStores_AddressTick_RowTypeHidden_LockKept_DropsIsStoreCount()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.RegisterCompanyStore("CLIENT A", "A-1", "Store One", "12 Rizal St., Tarlac City");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var bad = new List<string>();
            view.OpenNewOrderForm(); Pump(300);
            var grid = DcGet<DataGridView>(view, "gridRouteStores");
            var tick = DcGet<CheckBox>(view, "chkShowStoreAddresses");
            // Address tick: off by default, the column is there but hidden; ticking shows it, unticking hides it again.
            if (tick.Text != "Show store addresses") bad.Add("tick text is '" + tick.Text + "'");
            if (!grid.Columns.Contains("colStoreAddress")) bad.Add("no address column");
            else
            {
                if (tick.Checked || grid.Columns["colStoreAddress"]!.Visible) bad.Add("the address column is shown by default");
                tick.Checked = true;
                if (!grid.Columns["colStoreAddress"]!.Visible) bad.Add("ticking did not show the address column");
                DcGet<ComboBox>(view, "cbClient").Text = "CLIENT A"; Pump(300);
                int r = grid.Rows.Add();
                grid.Rows[r].Cells["colStoreCode"].Value = "A-1"; grid.Rows[r].Cells["colStoreName"].Value = "Store One"; Pump(200);
                string addr = grid.Rows[r].Cells["colStoreAddress"].FormattedValue?.ToString() ?? "";
                if (addr != "12 Rizal St., Tarlac City") bad.Add("address cell is '" + addr + "'");
                // The choice is remembered: a new screen starts ticked.
                var view2 = DcMakeView(out var host2);
                var tick2 = DcGet<CheckBox>(view2, "chkShowStoreAddresses");
                if (!tick2.Checked || !DcGet<DataGridView>(view2, "gridRouteStores").Columns["colStoreAddress"]!.Visible) bad.Add("the tick was not remembered");
                host2.Close();
                tick.Checked = false;
                if (grid.Columns["colStoreAddress"]!.Visible) bad.Add("unticking did not hide the address column");
            }
            // Row Type is hidden; the lock (code and name read-only on a route store) still works.
            if (!grid.Columns.Contains("colRowType") || grid.Columns["colRowType"]!.Visible) bad.Add("Row Type column is visible");
            if (!grid.Columns.Contains("colBaseline")) bad.Add("the lock flag column is gone");
            else
            {
                int r2 = grid.Rows.Add();
                grid.Rows[r2].Cells["colStoreCode"].Value = "A-1"; grid.Rows[r2].Cells["colStoreName"].Value = "Store One";
                typeof(DispatchManagementView).GetMethod("SetStoreRowType", DcF)!.Invoke(view, new object?[] { grid.Rows[r2], true });
                if (!(bool)typeof(DispatchManagementView).GetMethod("IsRouteStoreRow", DcF)!.Invoke(view, new object?[] { grid.Rows[r2] })!) bad.Add("a route store row is not recognised");
                if (!grid.Rows[r2].Cells["colStoreCode"].ReadOnly || !grid.Rows[r2].Cells["colStoreName"].ReadOnly) bad.Add("a route store row is not locked");
                typeof(DispatchManagementView).GetMethod("SetStoreRowType", DcF)!.Invoke(view, new object?[] { grid.Rows[r2], false });
                if (grid.Rows[r2].Cells["colStoreCode"].ReadOnly) bad.Add("an added store row is locked");
            }
            // No. of drops is the number of stores with a code or a name, and cannot be typed.
            var drops = DcGet<Label>(view, "lblNoOfDrops");
            int stores = grid.Rows.Cast<DataGridViewRow>().Count(x => (x.Cells["colStoreCode"].Value?.ToString() ?? "").Trim().Length > 0 || (x.Cells["colStoreName"].Value?.ToString() ?? "").Trim().Length > 0);
            if (drops.Text != $"No. of drops: {stores}") bad.Add($"drops label is '{drops.Text}', stores are {stores}");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
