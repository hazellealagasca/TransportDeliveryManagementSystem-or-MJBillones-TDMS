using System.Reflection;
using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 14, phase 4 (trip reports): the waiting list keeps its ticks, the type of delivery is REF and hidden, "+ Add store" is hidden, the drops are the
/// store count (read-only), plant gate and store times are 12-hour (old HHMM still reads), out before in asks "overnight trip?" and never refuses, and
/// the incident report is step 3 of the trip report (saved to IncidentReports, shown in Dispatch details, never read by billing).
/// </summary>
public static partial class Checks
{
    /// <summary>The app's pop-ups are themed forms: this records the text of every one and answers it (Yes by default; "No" when <see cref="Answer"/> is No).</summary>
    private sealed class P4Boxes : IDisposable
    {
        public readonly List<string> Boxes = new();
        public DialogResult Answer = DialogResult.Yes;
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
        public P4Boxes()
        {
            timer.Tick += (s, e) =>
            {
                foreach (var f in Application.OpenForms.Cast<Form>().Where(f => f.Modal && !f.IsDisposed).ToList())
                {
                    var sb = new System.Text.StringBuilder(f.Text).Append("] ");
                    void Walk(Control c) { foreach (Control k in c.Controls) { if (k is Label or Button) sb.Append(k.Text).Append(' '); Walk(k); } }
                    Walk(f);
                    Boxes.Add("[" + sb.ToString().Trim());
                    f.DialogResult = Answer; f.Close();
                }
            };
            timer.Start();
        }
        public void Dispose() { timer.Stop(); timer.Dispose(); }
    }

    private static IEnumerable<Control> P4Walk(Control c)
    {
        yield return c;
        foreach (Control k in c.Controls) foreach (var x in P4Walk(k)) yield return x;
    }

    private static int P4DeliveryWithStores(string client = "CLIENT A")
    {
        int id = Seed.Delivery(client, g: 10, e: 5);
        AddStop(id, 1, client, "Store A-1", 6, 3);
        AddStop(id, 2, client, "Store A-2", 4, 2);
        return id;
    }

    /// <summary>Opens a new report, picks the dispatch, types the store times / receivers / DRs, the gate times and the sign-off names.</summary>
    private static void P4Fill(TripReportView view, string number, string[] ins, string[] outs, string gateIn, string gateOut)
    {
        typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
        Pump(500);
        PickMain(view, number);
        var grid = FieldOf<DataGridView>(view, "gridStores")!;
        int n = 0;
        foreach (DataGridViewRow r in grid.Rows)
        {
            r.Cells["colIn"].Value = ins[n]; r.Cells["colOut"].Value = outs[n];
            r.Cells["colRep"].Value = n == 0 ? "Maria Santos" : "Ana Reyes";
            r.Cells["colDr"].Value = (5001 + n).ToString();
            n++;
        }
        typeof(TripReportView).GetMethod("RecalculateTotalsFromStoreGrid", NonPub)!.Invoke(view, null);
        FieldOf<TextBox>(view, "txtTruckTimeIn")!.Text = gateIn;
        FieldOf<TextBox>(view, "txtTruckTimeOut")!.Text = gateOut;
        foreach (var nm in new[] { "txtDtrApprovedBy", "txtDtrPreparedBy", "txtDrPreparedBy", "txtDrCheckedBy", "txtGuardName" }) FieldOf<TextBox>(view, nm)!.Text = "Maria Santos";
    }

    private static void P4Save(TripReportView view)
    {
        typeof(TripReportView).GetMethod("SaveNewReport", NonPub)!.Invoke(view, new object[] { false });
        Pump(1500);
    }

    // ------------------------------------------------------------------ pure rules

    [Check]
    public static string TripReportPhase4_Rules_OutBeforeInIs12HourAware_FeatureFlags()
    {
        var bad = new List<string>();
        if (!DailyRules.GateOutBeforeIn("8:42 PM", "1:30 AM")) bad.Add("1:30 AM before 8:42 PM not flagged");
        if (!DailyRules.GateOutBeforeIn("2042", "0130")) bad.Add("old HHMM 0130 before 2042 not flagged");
        if (!DailyRules.GateOutBeforeIn("08:42 PM", "0130")) bad.Add("mixed 12-hour in and old out not flagged");
        if (DailyRules.GateOutBeforeIn("08:00 AM", "05:00 PM")) bad.Add("a day trip flagged");
        if (DailyRules.GateOutBeforeIn("08:00 AM", "8:00 AM")) bad.Add("equal times flagged");
        if (DailyRules.GateOutBeforeIn("", "1:30 AM") || DailyRules.GateOutBeforeIn("8:42 PM", "") || DailyRules.GateOutBeforeIn("soon", "1:30 AM")) bad.Add("blank / unreadable flagged as earlier");
        string q = DailyRules.OvernightQuestion("8:42 PM", "1:30 AM");
        if (q != "Time out (01:30 AM) is earlier than time in (08:42 PM). Is this an overnight trip?") bad.Add("question wording: " + q);
        if (DailyRules.GateOvernightQuestion("2042", "0130") != q) bad.Add("gate question differs from the store question");
        if (!Features.On(Features.TripIncidents)) bad.Add("Features.TripIncidents is not on by default");
        if (Features.On(Features.Incidents)) bad.Add("Features.Incidents (the full screen) is not off");
        return string.Join("; ", bad);
    }

    [Check]
    public static string TripReportPhase4_TimeCells_TypeAndNormalise_UnreadableGetsAnErrorText()
    {
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var g = new DataGridView { AllowUserToAddRows = false };
            g.Columns.Add("colIn", "In"); g.Columns.Add("colRep", "Rep");
            g.Rows.Add("", "");
            Fmt.ApplyTimeColumns(g, "colIn");
            var end = typeof(DataGridView).GetMethod("OnCellEndEdit", BindingFlags.Instance | BindingFlags.NonPublic)!;
            void Leave(string text, int col)
            {
                g.Rows[0].Cells[col].Value = text;
                end.Invoke(g, new object[] { new DataGridViewCellEventArgs(col, 0) });
            }
            Leave("8:42 PM", 0); if ((g.Rows[0].Cells[0].Value?.ToString() ?? "") != "08:42 PM") bad.Add("8:42 PM became " + g.Rows[0].Cells[0].Value);
            Leave("2042", 0); if ((g.Rows[0].Cells[0].Value?.ToString() ?? "") != "08:42 PM") bad.Add("old 2042 became " + g.Rows[0].Cells[0].Value);
            Leave("842p", 0); if ((g.Rows[0].Cells[0].Value?.ToString() ?? "") != "08:42 PM") bad.Add("842p became " + g.Rows[0].Cells[0].Value);
            if (g.Rows[0].Cells[0].ErrorText.Length != 0) bad.Add("a readable time kept an error text");
            Leave("99:99", 0); if (g.Rows[0].Cells[0].ErrorText.Length == 0) bad.Add("an unreadable time got no error text");
            Leave("", 0); if (g.Rows[0].Cells[0].ErrorText.Length != 0) bad.Add("a blank time kept an error text");
            Leave("some name", 1); if ((g.Rows[0].Cells[1].Value?.ToString() ?? "") != "some name" || g.Rows[0].Cells[1].ErrorText.Length != 0) bad.Add("another column was touched");
            return string.Join("; ", bad);
        });
    }

    // ------------------------------------------------------------------ ticks

    [Check]
    public static string TripReportPhase4_PickTicks_SurviveFilterSortRefresh_DroppedWhenNoLongerWaiting_OldestFirstDefault()
    {
        Fresh();
        int a1 = Seed.Delivery("CLIENT A"), a2 = Seed.Delivery("CLIENT A"), b1 = Seed.Delivery("CLIENT B"), a3 = Seed.Delivery("CLIENT A");
        SetDate("Deliveries", a1, 5); SetDate("Deliveries", a2, 4); SetDate("Deliveries", b1, 3); SetDate("Deliveries", a3, 1);
        string nA1 = NumberOf(a1), nA2 = NumberOf(a2), nB1 = NumberOf(b1), nA3 = NumberOf(a3);
        return WithTripReportView((view, host, problems) =>
        {
            var cb = FieldOf<ComboBox>(view, "cbDelivery")!; var search = FieldOf<TextBox>(view, "txtPickSearch")!;
            var client = FieldOf<ComboBox>(view, "cbPickClient")!; var show = FieldOf<ComboBox>(view, "cbPickShow")!; var sort = FieldOf<ComboBox>(view, "cbPickSort")!;
            var count = FieldOf<Label>(view, "lblPickCount")!;
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            List<string> Nos() => cb.Items.Cast<object>().Select(o => (o.ToString() ?? "").Split(" - ")[0]).ToList();
            if (sort.SelectedIndex != 0 || sort.Text != "Oldest first") problems.Add("the default sort is not Oldest first: " + sort.Text);
            if (!Nos().SequenceEqual(new[] { nA1, nA2, nB1, nA3 })) problems.Add("not oldest first: " + string.Join(", ", Nos()));

            if (!view.TickPick(a1, true) || !view.TickPick(b1, true)) problems.Add("a waiting dispatch could not be ticked");
            if (view.TickPick(987654, true)) problems.Add("a dispatch that is not waiting could be ticked");
            bool Same(params int[] ids) => view.TickedPicks.OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x));
            if (!Same(a1, b1)) problems.Add("ticks after ticking: " + string.Join(",", view.TickedPicks));

            client.SelectedIndex = client.FindStringExact("CLIENT A"); Pump(200);     // CLIENT B's dispatch leaves the list, its tick stays
            if (Nos().Contains(nB1)) problems.Add("the client filter did not hide CLIENT B");
            if (!Same(a1, b1)) problems.Add("ticks after the client filter: " + string.Join(",", view.TickedPicks));
            sort.SelectedIndex = 1; Pump(200);
            if (!Same(a1, b1)) problems.Add("ticks after a sort change: " + string.Join(",", view.TickedPicks));
            search.Text = "zzz-nothing"; Pump(200);
            if (!Same(a1, b1)) problems.Add("ticks after a search that matches nothing: " + string.Join(",", view.TickedPicks));
            search.Text = ""; show.SelectedIndex = 1; Pump(200);
            if (!Same(a1, b1)) problems.Add("ticks after the Show filter: " + string.Join(",", view.TickedPicks));
            client.SelectedIndex = 0; show.SelectedIndex = 0; sort.SelectedIndex = 0; Pump(200);

            var load = typeof(TripReportView).GetMethod("LoadData", NonPub)!;
            load.Invoke(view, null); Pump(2500);                                       // a refresh
            if (!Same(a1, b1)) problems.Add("ticks after a refresh: " + string.Join(",", view.TickedPicks));
            if (!count.Text.Contains("2 ticked")) problems.Add("the count text does not say how many are ticked: " + count.Text);

            AddReport(nA1, a1, "Completed");                                           // a1 is no longer waiting
            load.Invoke(view, null); Pump(2500);
            if (!Same(b1)) problems.Add("a dispatch that no longer waits kept its tick: " + string.Join(",", view.TickedPicks));
            view.TickPick(b1, false);
            if (view.TickedPicks.Count != 0) problems.Add("unticking did not clear the tick");
            if (sort.SelectedIndex != 0) problems.Add("the sort did not return to Oldest first");
            return "";
        });
    }

    // ------------------------------------------------------------------ the form: REF hidden, no Add store, drops, no Log incident, saved REF and 12-hour times

    [Check]
    public static string TripReportPhase4_Form_TypeRefHidden_NoAddStore_DropsReadOnly_NoLogIncidentButton_SavedAsRef()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        return WithTripReportView((view, host, problems) =>
        {
            using var boxes = new P4Boxes();
            P4Fill(view, no, new[] { "8:42 PM", "9:10 PM" }, new[] { "9:00 PM", "9:30 PM" }, "7:30 PM", "8:00 PM");   // Nov 17: the gate times come before the store times
            var typeBox = FieldOf<ComboBox>(view, "cbTripType")!;
            if (typeBox.Parent!.Visible || typeBox.Visible) problems.Add("the type of delivery is still shown");
            if (typeBox.Text != "REF") problems.Add("the type of delivery is " + typeBox.Text + ", not REF");
            if (P4Walk(view).OfType<Label>().Any(l => l.Text.StartsWith("Type of delivery") && l.Visible)) problems.Add("the 'Type of delivery' label is visible");
            if (P4Walk(view).OfType<Button>().Any(b => b.Text.Contains("Add store") && b.Visible)) problems.Add("the '+ Add store' button is visible");
            if (P4Walk(view).OfType<Button>().Any(b => b.Text.Contains("Log incident", StringComparison.OrdinalIgnoreCase) || b.Text.Contains("Log crate incident", StringComparison.OrdinalIgnoreCase))) problems.Add("an old Log incident button is still on the form");
            if (FieldOf<Button>(view, "btnLogIncident") != null) problems.Add("btnLogIncident still exists");

            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            if (!grid.Columns["colRem"]!.ReadOnly) problems.Add("No. Drops is editable");
            var drops = grid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["colRem"].Value?.ToString()).ToList();
            if (drops.Count != 2 || drops.Any(d => d != "1")) problems.Add("drops per store: " + string.Join(",", drops) + " (expected 1 for each of the 2 stores)");
            if (grid.Rows.Count != 2) problems.Add("expected 2 stores, found " + grid.Rows.Count);

            P4Save(view);
            long reports = Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND Status = 'Completed'", ("@d", id));
            if (reports != 1) problems.Add("the report was not saved (" + reports + "): " + string.Join(" | ", boxes.Boxes));
            else
            {
                int trId = (int)Long("SELECT Id FROM TripReports WHERE DeliveryId = @d", ("@d", id));
                if (MJdatabase.GetTripReportDeliveryType(trId) != "REF") problems.Add("saved type of delivery: " + MJdatabase.GetTripReportDeliveryType(trId));
                if (Str("SELECT TruckTimeIn FROM TripReports WHERE DeliveryId = @d", ("@d", id)) != "07:30 PM") problems.Add("gate time in saved as " + Str("SELECT TruckTimeIn FROM TripReports WHERE DeliveryId = @d", ("@d", id)));
                if (Str("SELECT TruckTimeOut FROM TripReports WHERE DeliveryId = @d", ("@d", id)) != "08:00 PM") problems.Add("gate time out saved as " + Str("SELECT TruckTimeOut FROM TripReports WHERE DeliveryId = @d", ("@d", id)));
                if (Str("SELECT ArrivalTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 1", ("@d", id)) != "08:42 PM") problems.Add("store 1 time in saved as " + Str("SELECT ArrivalTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 1", ("@d", id)));
                if (Str("SELECT DepartureTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 2", ("@d", id)) != "09:30 PM") problems.Add("store 2 time out saved as " + Str("SELECT DepartureTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 2", ("@d", id)));
                if (boxes.Boxes.Any(b => b.Contains("overnight"))) problems.Add("a normal day trip asked the overnight question");
            }
            return "";
        });
    }

    // ------------------------------------------------------------------ times

    [Check]
    public static string TripReportPhase4_Times_OldHhmmShowAs12Hour_UnreadableRefused_OutBeforeInAsksOvernight()
    {
        Fresh();
        int old = P4DeliveryWithStores(), over = P4DeliveryWithStores();
        string nOld = NumberOf(old), nOver = NumberOf(over);
        // an old report saved with 24-hour HHMM text
        Exec("UPDATE DispatchStops SET ArrivalTime = '0830', DepartureTime = '0915' WHERE DeliveryId = @d", ("@d", old));
        Exec(@"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, DeliveredCrates, ReturnedCrates, ReportDate, SpecialTripId, IsVoid, Status, TruckTimeIn, TruckTimeOut, ArrivalTime, DepartureTime)
               VALUES ('TR-OLD1', @d, @n, 15, 15, @rd, 0, 0, 'Completed', '2042', '0139', '0830', '0915')", ("@d", old), ("@n", nOld), ("@rd", DateTime.Today.ToString("yyyy-MM-dd")));
        return WithTripReportView((view, host, problems) =>
        {
            using var boxes = new P4Boxes();
            var tin = FieldOf<TextBox>(view, "txtTruckTimeIn")!; var tout = FieldOf<TextBox>(view, "txtTruckTimeOut")!;
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            // old values load as 12-hour
            var tr = MJdatabase.GetTripReports().First(t => t.DeliveryId == old);
            typeof(TripReportView).GetMethod("OpenEditTripReport", NonPub)!.Invoke(view, new object[] { tr });
            Pump(1200);
            if (tin.Text != "08:42 PM" || tout.Text != "01:39 AM") problems.Add($"old gate times show as '{tin.Text}' / '{tout.Text}'");
            var inCells = grid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["colIn"].Value?.ToString()).ToList();
            if (inCells.Count == 0 || inCells[0] != "08:30 AM") problems.Add("an old store time in shows as " + string.Join(",", inCells));
            if (grid.Rows.Cast<DataGridViewRow>().Any(r => r.Cells["colOut"].Value?.ToString() != "09:15 AM")) problems.Add("an old store time out is not shown as 09:15 AM");
            // out before in at the gate asks the question (and "No" does not let the save go on)
            boxes.Answer = DialogResult.Cancel;
            int before = boxes.Boxes.Count;
            bool go = (bool)typeof(TripReportView).GetMethod("ConfirmGateOrder", NonPub)!.Invoke(view, null)!;
            Pump(300);
            if (go) problems.Add("answering No to the gate question still let the save go on");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Time out (01:39 AM) is earlier than time in (08:42 PM). Is this an overnight trip?"))) problems.Add("gate question missing or reworded: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            boxes.Answer = DialogResult.Yes;
            if (!(bool)typeof(TripReportView).GetMethod("ConfirmGateOrder", NonPub)!.Invoke(view, null)!) problems.Add("answering Yes to the gate question did not let the save go on");

            // a new report: unreadable times are refused (listed), out before in is not refused
            P4Fill(view, nOver, new[] { "12:20 AM", "1:40 AM" }, new[] { "1:30 AM", "3:00 AM" }, "7:30 PM", "12:10 AM");   // Nov 17: overnight gate; store times are after the gate time out
            var blank = typeof(TripReportView).GetMethod("BlankFormChecks", NonPub)!;
            string Listed() => string.Join(" | ", ((System.Collections.IEnumerable)blank.Invoke(view, null)!).Cast<object>().Select(o => o.ToString() ?? ""));
            if (Listed().Contains("must be a time like 08:42 PM")) problems.Add("valid 12-hour times were refused: " + Listed());
            if (Listed().Contains("overnight")) problems.Add("the overnight question is in the refusal list");
            tin.Text = "99:99";
            if (!Listed().Contains("Truck time in must be a time like 08:42 PM")) problems.Add("an unreadable gate time in was not refused: " + Listed());
            tin.Text = "7:30 PM";
            grid.Rows[0].Cells["colOut"].Value = "soon";
            if (!Listed().Contains("Time out must be a time like 08:42 PM. Fix it for: Store A-1")) problems.Add("an unreadable store time out was not refused: " + Listed());
            grid.Rows[0].Cells["colOut"].Value = "1:30 AM";

            // saving with an overnight store and an overnight gate: asks for the store and for the gate, then saves both as 12-hour
            boxes.Answer = DialogResult.Yes;
            before = boxes.Boxes.Count;
            P4Save(view);
            var asked = boxes.Boxes.Skip(before).Where(b => b.Contains("overnight trip")).ToList();
            if (asked.Count != 1) problems.Add($"expected 1 overnight question (the plant gate; Nov 17: a store out-before-in cannot pass the new store-time rule), saw {asked.Count}: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            if (!asked.Any(b => b.Contains("Time out (12:10 AM) is earlier than time in (07:30 PM)"))) problems.Add("the gate question is missing: " + string.Join(" | ", asked));
            if (Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND Status = 'Completed'", ("@d", over)) != 1) problems.Add("the overnight report was not saved: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            else
            {
                if (Str("SELECT TruckTimeOut FROM TripReports WHERE DeliveryId = @d", ("@d", over)) != "12:10 AM") problems.Add("overnight gate time out saved as " + Str("SELECT TruckTimeOut FROM TripReports WHERE DeliveryId = @d", ("@d", over)));
                if (Str("SELECT DepartureTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 1", ("@d", over)) != "01:30 AM") problems.Add("overnight store time out saved as " + Str("SELECT DepartureTime FROM DispatchStops WHERE DeliveryId = @d AND StopSequence = 1", ("@d", over)));
            }
            return "";
        });
    }

    // ------------------------------------------------------------------ incident report = step 3

    [Check]
    public static string TripReportPhase4_Incident_Step3_SavedWithDeliveryId_ShownInDispatchDetails_FullScreenStaysHidden()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        string result = WithTripReportView((view, host, problems) =>
        {
            using var boxes = new P4Boxes();
            P4Fill(view, no, new[] { "8:42 PM", "9:10 PM" }, new[] { "9:00 PM", "9:30 PM" }, "7:30 PM", "8:00 PM");   // Nov 17: the gate times come before the store times
            var no_ = FieldOf<RadioButton>(view, "rbIncNo")!; var yes = FieldOf<RadioButton>(view, "rbIncYes")!;
            var fields = FieldOf<TableLayoutPanel>(view, "pnlIncFields")!; var card = FieldOf<Panel>(view, "cardIncident")!;
            var cause = FieldOf<TextBox>(view, "txtIncCause")!; var notes = FieldOf<TextBox>(view, "txtIncNotes")!;
            var qty = FieldOf<NumericUpDown>(view, "numIncQty")!; var cost = FieldOf<NumericUpDown>(view, "numIncCost")!; var crew = FieldOf<CheckBox>(view, "chkIncCrew")!;
            if (card.Visible) problems.Add("the incident card shows on step 1 (it is step 3 of the wizard)");
            view.GoToStep(3); Pump(200);   // the wizard shows one step at a time: the incident question is on step 3
            if (!card.Visible) problems.Add("the incident card is not shown on step 3");
            if (!no_.Checked || yes.Checked) problems.Add("the answer is not No by default");
            if (fields.Visible) problems.Add("the incident fields show while the answer is No");
            if (P4Walk(view).OfType<Label>().All(l => l.Text != "Any damaged or missing crates?")) problems.Add("the question 'Any damaged or missing crates?' is missing");

            var blank = typeof(TripReportView).GetMethod("BlankFormChecks", NonPub)!;
            string Listed() => string.Join(" | ", ((System.Collections.IEnumerable)blank.Invoke(view, null)!).Cast<object>().Select(o => o.ToString() ?? ""));
            if (Listed().Contains("Incident report")) problems.Add("No still asks for an incident: " + Listed());
            yes.Checked = true; Pump(200);
            if (!fields.Visible) problems.Add("Yes did not show the incident fields");
            if (!Listed().Contains("Incident report: add at least one incident")) problems.Add("Yes with no incident was not refused: " + Listed());

            var add = typeof(TripReportView).GetMethod("AddIncidentFromFields", NonPub)!;
            bool Add(string c, int q, decimal k, bool ch, string n)
            {
                cause.Text = c; qty.SetSafe(q); cost.SetSafe(k); crew.Checked = ch; notes.Text = n;
                // Nov 17: a chargeable incident names who is charged: tick the trip's first crew member (the driver).
                if (ch && typeof(TripReportView).GetField("incCrewBoxes", NonPub)!.GetValue(view) is List<CheckBox> who && who.Count > 0) who[0].Checked = true;
                return (bool)add.Invoke(view, new object[] { false })!;
            }
            if (Add("", 3, 100, false, "")) problems.Add("an incident with no cause was added");
            if (Add("Broken crates", 0, 100, false, "")) problems.Add("an incident with no quantity was added");
            if (Add("Broken crates", 99, 100, false, "")) problems.Add("a quantity above the loaded crates was added");
            if (Add("Broken crates", 3, 0, true, "")) problems.Add("a chargeable incident with no cost was added");
            if (!Add("Broken crates", 3, 450, true, "Dropped at store 1")) problems.Add("a valid incident was refused");
            if (!Add("Missing crates", 2, 0, false, "")) problems.Add("a second incident on the same trip was refused");
            if (Add("broken CRATES", 1, 10, false, "")) problems.Add("the same cause was added twice");
            cause.Text = ""; qty.SetSafe(0); cost.SetSafe(0); crew.Checked = false; notes.Text = "";
            var lines = (System.Collections.IList)typeof(TripReportView).GetField("tripIncidentLines", NonPub)!.GetValue(view)!;
            if (lines.Count != 2) problems.Add("the list holds " + lines.Count + " incidents, expected 2");
            if (Listed().Contains("Incident report")) problems.Add("two valid incidents are still refused: " + Listed());

            P4Save(view);
            if (Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND Status = 'Completed'", ("@d", id)) != 1) problems.Add("the trip report was not saved: " + string.Join(" | ", boxes.Boxes));
            if (boxes.Boxes.Any(b => b.Contains("Incident") && b.Contains("now?"))) problems.Add("the old 'log an incident now?' question appeared: " + string.Join(" | ", boxes.Boxes));
            if (Long("SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @d AND COALESCE(Status,'') <> 'Voided'", ("@d", id)) != 2) problems.Add("IncidentReports rows for the trip: " + Long("SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @d", ("@d", id)));
            if (Str("SELECT DispatchNumber FROM IncidentReports WHERE DeliveryId = @d ORDER BY Id LIMIT 1", ("@d", id)) != no) problems.Add("the incident has the wrong dispatch number");
            if (Long("SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @d AND Cause = 'Broken crates' AND DamagedQuantity = 3 AND EstimatedCost = 450 AND ChargeableToCrew = 1 AND Description = 'Dropped at store 1'", ("@d", id)) != 1) problems.Add("the first incident was not saved with its quantity, cost, crew flag and notes");
            if (MJdatabase.GetTripIncidents(id).Count != 2) problems.Add("GetTripIncidents found " + MJdatabase.GetTripIncidents(id).Count);
            // Nov 18: the trip incidents are read back everywhere (profiles, dossier, counts); only the old full screen stays hidden.
            if (Features.On(Features.Incidents)) problems.Add("the full Incident & Damage Logs screen is switched on");
            if (MJdatabase.GetIncidentReports().Count(i => i.DeliveryId == id) != 2) problems.Add("the saved trip incidents are not read back (" + MJdatabase.GetIncidentReports().Count(i => i.DeliveryId == id) + ")");
            return "";
        });
        if (result.Length > 0) return result;

        // Dispatch details lists them read-only, and only while Features.TripIncidents is on
        var d = MJdatabase.GetDeliveryById(id)!;
        return RunOnSta(() =>
        {
            var problems = new List<string>();
            List<string> DetailsTexts()
            {
                var form = ShowForAndGet(p => Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.DispatchDetailsDialog.ShowFor(p, d));
                try
                {
                    Pump(800);
                    form.GetType().GetMethod("ShowTab", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, new object[] { "Documents" }); Pump(400);
                    var texts = new List<string>(); CollectTexts(form, texts);
                    return texts;
                }
                finally { form.Close(); }
            }
            var t1 = DetailsTexts();
            if (!t1.Any(t => t == "Incident · Broken crates")) problems.Add("Dispatch details does not list the Broken crates incident: " + string.Join(" | ", t1.Where(t => t.Contains("ncident"))));
            if (!t1.Any(t => t == "Incident · Missing crates")) problems.Add("Dispatch details does not list the Missing crates incident");
            if (!t1.Any(t => t.Contains("3 crates") && t.Contains("PHP 450.00") && t.Contains("charged to ") && t.Contains("Dropped at store 1"))) problems.Add("the incident line lacks its quantity, cost, crew flag or notes");   // Nov 17: "charged to <name>"
            if (t1.Any(t => t.Contains("Log incident", StringComparison.OrdinalIgnoreCase))) problems.Add("Dispatch details offers 'Log incident'");
            try
            {
                Features.Set(Features.TripIncidents, false, "Incident report in trip reports");
                if (MJdatabase.GetTripIncidents(id).Count != 0) problems.Add("GetTripIncidents is not empty while the switch is off");
                var t2 = DetailsTexts();
                if (t2.Any(t => t.StartsWith("Incident ·"))) problems.Add("Dispatch details shows incidents while the switch is off");
            }
            finally { Features.Set(Features.TripIncidents, true, "Incident report in trip reports"); Features.Reload(); }
            return string.Join("; ", problems);
        });
    }

    [Check]
    public static string TripReportPhase4_Incident_SwitchOff_HidesStep_EditShowsSavedLines_NoOnRemoves()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        Exec(@"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, DeliveredCrates, ReturnedCrates, ReportDate, SpecialTripId, IsVoid, Status)
               VALUES ('TR-INC1', @d, @n, 15, 15, @rd, 0, 0, 'Draft')", ("@d", id), ("@n", no), ("@rd", DateTime.Today.ToString("yyyy-MM-dd")));
        MJdatabase.SaveIncidentReport(new IncidentReport { DeliveryId = id, DispatchNumber = no, IncidentDate = DateTime.Today, Cause = "Wet crates", DamagedQuantity = 4, EstimatedCost = 200m, Status = "Pending" }, notify: false);
        try
        {
            return WithTripReportView((view, host, problems) =>
            {
                using var boxes = new P4Boxes();
                var yes = FieldOf<RadioButton>(view, "rbIncYes")!; var no_ = FieldOf<RadioButton>(view, "rbIncNo")!;
                var card = FieldOf<Panel>(view, "cardIncident")!; var fields = FieldOf<TableLayoutPanel>(view, "pnlIncFields")!;
                var grid = FieldOf<DataGridView>(view, "gridInc")!;
                var tr = MJdatabase.GetTripReports().First(t => t.DeliveryId == id);
                typeof(TripReportView).GetMethod("OpenEditTripReport", NonPub)!.Invoke(view, new object[] { tr });
                Pump(1200);
                view.GoToStep(3); Pump(200);   // a saved report's header opens any step: the incident lines are on step 3
                if (!yes.Checked || !fields.Visible) problems.Add("a trip with a saved incident does not open on Yes");
                if (grid.Rows.Count != 1 || grid.Rows[0].Cells["colIncCause"].Value?.ToString() != "Wet crates") problems.Add("the saved incident is not listed in step 3");
                // choosing No takes it off the trip (asks first; the answer here is Yes)
                boxes.Answer = DialogResult.Yes;
                no_.Checked = true; Pump(300);
                if (fields.Visible || grid.Rows.Count != 0) problems.Add("No did not clear the list");
                typeof(TripReportView).GetMethod("PersistTripIncidents", NonPub)!.Invoke(view, new object[] { MJdatabase.GetDeliveryById(id)! });
                if (MJdatabase.GetTripIncidents(id).Count != 0) problems.Add("the incident was not taken off the trip");
                if (Long("SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @d AND Status = 'Voided'", ("@d", id)) != 1) problems.Add("the removed incident was erased instead of voided");
                // the switch off hides the whole card
                Features.Set(Features.TripIncidents, false, "Incident report in trip reports");
                Pump(200);
                if (card.Visible) problems.Add("the incident card is still shown with the switch off");
                return "";
            });
        }
        finally { Features.Set(Features.TripIncidents, true, "Incident report in trip reports"); Features.Reload(); }
    }

    // ------------------------------------------------------------------ billing isolation

    [Check]
    public static string TripReportPhase4_BillingIsolation_NoIncidentInSoaInvoiceReceiptTotalsOrPrints()
    {
        string root = RepoRoot();
        var hits = new List<string>();
        void Scan(string label, string text)
        {
            string code = Regex.Replace(text, @"//[^\n]*|/\*.*?\*/", "", RegexOptions.Singleline);   // a comment may explain the rule; only code counts
            if (code.Contains("Incident", StringComparison.Ordinal)) hits.Add(label);
        }
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(Path.Combine(root, "Functions"), "SoaTotals*.cs"));
        foreach (var pattern in new[] { "StatementOfAccountView*.cs", "BillingView*.cs", "CollectionView*.cs" }) files.AddRange(Directory.GetFiles(Path.Combine(root, "Views"), pattern));
        if (files.Count < 10) hits.Add("only " + files.Count + " billing files were found to scan");
        foreach (var f in files) Scan(Path.GetFileName(f), File.ReadAllText(f));
        string print = File.ReadAllText(Path.Combine(root, "Functions", "PrintForms.cs"));
        foreach (var m in new[] { "CollectionReceipt", "StatementOfAccount", "SpecialTripStatementOfAccount", "StatementOfInvoices" })
        {
            string body = MethodText(print, m);
            if (body.Length == 0) hits.Add("PrintForms." + m + " not found");
            Scan("PrintForms." + m, body);
        }
        // the data layer's billing code does not read the incident table either
        string soaTotals = File.ReadAllText(Path.Combine(root, "Functions", "SoaTotals.cs"));
        if (soaTotals.Contains("IncidentReports")) hits.Add("SoaTotals reads IncidentReports");
        return hits.Count == 0 ? "" : "billing code uses incidents: " + string.Join(", ", hits);
    }

    [Check]
    public static string TripReportPhase4_Source_StepOneTwoThree_NoOldLogIncidentPopup_Add_Store_Hidden()
    {
        string root = RepoRoot();
        var bad = new List<string>();
        string cs = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.cs"));
        string several = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Several.cs"));
        string layout = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Layout.cs"));
        foreach (var (label, text) in new[] { ("TripReportView.cs", cs), ("TripReportView.Several.cs", several) })
        {
            if (text.Contains("Log Incident Report")) bad.Add(label + " still asks 'Log Incident Report?'");
            if (text.Contains("log an Incident & Damage Report")) bad.Add(label + " still has the 'log an incident now' text");
        }
        if (cs.Contains("btnLogIncident") || layout.Contains("btnLogIncident")) bad.Add("btnLogIncident is still referenced");
        if (!Regex.IsMatch(cs, @"btnAddTripStoreRow\.Visible\s*=\s*false")) bad.Add("the Add store button is not hidden");
        if (!File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Blank.cs")).Contains("Fmt.ApplyTimeBox")) bad.Add("the plant gate boxes do not use Fmt.ApplyTimeBox");
        if (File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Blank.cs")).Contains("24-hour")) bad.Add("a 24-hour message is left in the trip report checks");
        return string.Join("; ", bad);
    }
}
