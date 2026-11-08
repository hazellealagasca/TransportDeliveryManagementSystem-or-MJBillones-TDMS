using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// "Keep for later" on the Unsaved-changes pop-up: leaving a dirty form with Keep must hide it without clearing what was typed,
/// and coming back (the add button, or returning to the screen) must show the same typing again. Discard must still clear it.
/// </summary>
public static partial class Checks
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static string KeepRoundTrip(Func<Control> make, string openNew, string? preferredField = null)
    {
        var problems = new List<string>();
        using var boxes = new BoxCloser(GetCurrentThreadId());
        using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
        var view = make(); view.Dock = DockStyle.Fill;
        host.Controls.Add(view); host.Show(); Pump(900);
        var ui = (IUnsavedForm)view;
        var open = view.GetType().GetMethod(openNew, Any) ?? throw new Exception("no " + openNew);
        var pnl = FieldOf<Panel>(view, "pnlForm");
        var toggle = FieldOf<Button>(view, "btnToggleForm");
        if (pnl == null) return "pnlForm not found";

        void Open() { open.Invoke(view, null); Pump(500); }
        Open();
        if (!pnl.Visible) return "the new form did not open";

        // type into the first plain text box that accepts what we give it
        // (not the inner editor of a drop-down or number box: "clear" never reaches those directly)
        Control? box = null; const string typed = "keep-me-123";
        var candidates = Descend(pnl).Where(c => c.Enabled && c.Parent is not (ComboBox or NumericUpDown or DateTimePicker) &&
            ((c is TextBox t && !t.ReadOnly) || (c is ComboBox cb && cb.DropDownStyle != ComboBoxStyle.DropDownList))).ToList();
        var preferred = preferredField == null ? null : FieldOf<Control>(view, preferredField);
        if (preferred != null) candidates.Insert(0, preferred);   // a field the screen's own "clear" is known to reset
        foreach (var c in candidates)
        {
            c.Text = typed;
            if (c.Text == typed) { box = c; break; }
            c.Text = "";
        }
        if (box == null) return "no box took typed text; candidates: " + string.Join(",", candidates.Select(c => c.Name));
        (view.GetType().GetField("formDirty", Any)!.GetValue(view) as FormDirtyTracker)!.MarkDirty();
        if (!ui.FormIsOpenAndDirty) problems.Add("the form with typing is not seen as unsaved");

        // 1. Keep: hidden, typing intact, add button offers Resume
        ui.KeepAndCloseForm(); Pump(400);
        if (pnl.Visible) problems.Add("Keep left the form open");
        if (!ui.HasKeptDraft) problems.Add("Keep did not remember the draft");
        if (box.Text != typed) problems.Add($"Keep cleared the typing (now '{box.Text}')");
        if (toggle != null && toggle.Text != "Resume draft") problems.Add($"the add button says '{toggle.Text}', not 'Resume draft'");

        // 2. Resume with the add button's own routine: same typing, still unsaved, no longer waiting
        Open();
        if (!pnl.Visible) problems.Add("Resume did not reopen the form");
        if (box.Text != typed) problems.Add($"Resume (add button) lost the typing (now '{box.Text}')");
        if (ui.HasKeptDraft) problems.Add("still marked as kept after resuming");
        if (!ui.FormIsOpenAndDirty) problems.Add("a resumed form is no longer seen as unsaved, so leaving it again would not ask");

        // 3. Keep, leave the screen, come back: the form reopens by itself with the typing
        ui.KeepAndCloseForm(); Pump(300);
        view.Visible = false; Pump(200);
        view.Visible = true; Pump(900);
        if (!pnl.Visible) problems.Add("coming back to the screen did not reopen the kept form");
        if (box.Text != typed) problems.Add($"coming back lost the typing (now '{box.Text}')");
        if (toggle != null && toggle.Text == "Resume draft") problems.Add("the add button still says Resume draft after the form came back");

        // 4. Discard really discards
        ui.DiscardAndCloseForm(); Pump(400);
        if (pnl.Visible) problems.Add("Discard left the form open");
        if (box.Text == typed) problems.Add($"Discard did not clear the typing (box {box.Name}/{box.GetType().Name} in {box.Parent?.Name}/{box.Parent?.GetType().Name}, text now '{box.Text}')");
        if (ui.HasKeptDraft) problems.Add("Discard left a kept draft behind");

        // 5. After Discard a new form is blank, not the old draft
        Open();
        if (box.Text == typed) problems.Add($"a new form after Discard still showed the old typing (box {box.Name}/{box.GetType().Name} in {box.Parent?.Name}/{box.Parent?.GetType().Name}, text now '{box.Text}')");

        lock (boxes.Boxes) if (boxes.Boxes.Count > 0) problems.Add("a message box appeared: " + string.Join(" / ", boxes.Boxes));
        host.Close();
        return string.Join(" | ", problems);
    }

    private static IEnumerable<Control> Descend(Control root)
    {
        foreach (Control c in root.Controls) { yield return c; foreach (var d in Descend(c)) yield return d; }
    }

    private static string KeepOn(Func<Control> make, string openNew, string? preferredField = null) { Fresh(); return RunOnSta(() => KeepRoundTrip(make, openNew, preferredField)); }

    [Check] public static string KeepDraft_Crew() => KeepOn(() => new CrewManagementView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_Trucks() => KeepOn(() => new TruckManagementView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_Users() => KeepOn(() => new UserManagementView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_RateMatrix() => KeepOn(() => new RateMatrixView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_Incidents() => KeepOn(() => new IncidentReportView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_Collection() => KeepOn(() => new CollectionView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_Billing() => KeepOn(() => new BillingView(), "OpenNewRecordForm");
    [Check] public static string KeepDraft_TripReports() => KeepOn(() => new TripReportView(), "OpenNewTripReport");
    [Check] public static string KeepDraft_Statements() => KeepOn(() => new StatementOfAccountView(), "OpenNewStatement", "txtPreparedBy");
    [Check] public static string KeepDraft_Dispatch() => KeepOn(() => new DispatchManagementView(), "OpenNewOrderForm");
}
