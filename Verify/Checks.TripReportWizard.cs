using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// The Trip Report form is on the shared FormWizard (same header, footer, Next and header rules as New Dispatch Order and the Collection Receipt):
/// Nov 26: always 2 steps. 1 Plant gate, sign-off, stores &amp; DRs (plant gate card above the stores card), 2 Incident / Damage Report above Attachments, &amp; save (while the trip-incident switch is off step 2 is Attachments &amp; save with the incident card hidden).
/// The dispatch banner stays above the steps and the crate totals above the action bar on every step.
/// </summary>
public static partial class Checks
{
    private static (TripReportView view, Form host) TwMakeView()
    {
        var view = new TripReportView { Dock = DockStyle.Fill };
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
        host.Controls.Add(view); host.Show(); Pump(2000);
        return (view, host);
    }

    private static Panel TwPanel(Control root, string name) =>
        root.Controls.Find(name, true).OfType<Panel>().FirstOrDefault() ?? throw new InvalidOperationException("panel not found: " + name);

    private static bool TwInside(Control c, Control p) { for (var x = c; x != null; x = x.Parent) if (x == p) return true; return false; }

    [Check]
    public static string TripReportWizard_NewReport_StepsValidateNavigateAndStep2IsRequiredForMissingCrates()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new P4Boxes();
            var (view, host) = TwMakeView();
            var T = typeof(TripReportView);
            var bad = new List<string>();
            T.GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null); Pump(500);
            PickMain(view, no);
            var p1 = TwPanel(view, "pnlStep1GateAndStores"); var p2 = TwPanel(view, "pnlStep2IncidentAndAttachments");
            var pGate = TwPanel(view, "pnlPlantGateCard"); var pStores = TwPanel(view, "pnlStep1Stores"); var pInc = TwPanel(view, "pnlIncidentCard"); var pAtt = TwPanel(view, "pnlAttachmentsCard");
            var panels = new[] { p1, p2 };
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!; var issue = FieldOf<Button>(view, "btnIssueExisting")!;
            var draft = FieldOf<Button>(view, "btnSaveDraft")!; var clear = FieldOf<Button>(view, "btnClear")!;
            var next = view.Controls.Find("btnNext", true).OfType<Button>().First(); var back = view.Controls.Find("btnBack", true).OfType<Button>().First();
            var banner = FieldOf<Label>(view, "lblDispatchInfoBanner")!; var footer = FieldOf<Label>(view, "lblDeliveredCrateTotals")!;
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", NonPub)!.Invoke(view, new object[] { n })!;
            string State() => $"step {view.CurrentWizardStep}, visible [{string.Join(",", panels.Select(p => p.Visible ? "1" : "0"))}]";
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong ({State()})"); break; }
                if (!banner.Visible) bad.Add($"{what}: the dispatch banner is not visible");
                if (!footer.Visible || !footer.Text.StartsWith("Delivered crates")) bad.Add($"{what}: the crate totals footer is not visible ('{footer.Text}', {footer.Visible})");
                if (view.Controls.Find("pnlStepperHeader", true).OfType<WizardStepHeader>().Count() != 1) bad.Add($"{what}: expected exactly one WizardStepHeader");
            }
            int before;
            bool Said(string text) => boxes.Boxes.Skip(before).Any(m => m.Contains(text));
            string AllSaid() => string.Join(" | ", boxes.Boxes.Skip(before));

            // one implementation: the shared header, none of the old custom bar
            if (P4Walk(view).Any(c => c.GetType().Name == "TripStepper")) bad.Add("the old TripStepper bar is still on the form");
            if (view.WizardStepCount != 2) bad.Add("step count " + view.WizardStepCount);
            // Nov 26: step 1 = plant gate card above the stores card; step 2 = incident card above the attachments card
            if (!TwInside(pGate, p1) || !TwInside(pStores, p1)) bad.Add("the plant gate / stores cards are not on step 1");
            if (!TwInside(pInc, p2) || !TwInside(pAtt, p2)) bad.Add("the incident / attachments cards are not on step 2");
            if (pGate.Top >= pStores.Top) bad.Add($"the plant gate card (top {pGate.Top}) is not above the stores card (top {pStores.Top})");
            if (!TwInside(FieldOf<DataGridView>(view, "gridStores")!, pStores)) bad.Add("the stores table is not in the stores card");
            if (!TwInside(FieldOf<TextBox>(view, "txtTruckTimeIn")!, pGate) || !TwInside(FieldOf<TextBox>(view, "txtGuardName")!, pGate) || !TwInside(FieldOf<TextBox>(view, "txtRemarks")!, pGate)) bad.Add("the plant gate / sign-off fields are not in the plant gate card");
            if (!P4Walk(pAtt).Any(c => c.GetType().Name == "AttachmentPanel")) bad.Add("the attachments are not in the attachments card");
            if (!TwInside(FieldOf<RadioButton>(view, "rbIncYes")!, pInc)) bad.Add("the incident question is not in the incident card");
            if (TwInside(banner, p1) || TwInside(banner, p2)) bad.Add("the dispatch banner sits inside a step");
            Expect(1, "new report start");
            if (!clear.Visible || back.Visible || !next.Visible || save.Visible || update.Visible || issue.Visible || !draft.Visible)
                bad.Add($"step 1 buttons wrong (clear {clear.Visible}, back {back.Visible}, next {next.Visible}, save {save.Visible}, update {update.Visible}, issue {issue.Visible}, draft {draft.Visible})");
            if (!next.Text.Contains("Incident / Damage Report")) bad.Add("step 1 Next text: " + next.Text);
            if (CanJump(2)) bad.Add("step 2 is clickable on a new report");

            // Next on step 1 with nothing typed: stays, with the form's own messages for the plant gate AND the stores
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed step 1 with nothing typed");
            Pump(200);
            Expect(1, "Next with step 1 empty");
            if (!Said("Time in is required") || !Said("DR (delivery receipt) number is required")) bad.Add("step 1 Next did not show the time-in / DR messages: " + AllSaid());
            if (!Said("Truck time in (plant gate) is required") || !Said("Guard name & sign is required")) bad.Add("step 1 Next did not show the plant gate / sign-off messages: " + AllSaid());
            FieldOf<TextBox>(view, "txtTruckTimeIn")!.Text = "7:30 PM"; FieldOf<TextBox>(view, "txtTruckTimeOut")!.Text = "8:00 PM";
            foreach (var nm in new[] { "txtDtrApprovedBy", "txtDtrPreparedBy", "txtDrPreparedBy", "txtDrCheckedBy", "txtGuardName" }) FieldOf<TextBox>(view, nm)!.Text = "Maria Santos";
            Pump(100);

            // a store whose time out is before its time in
            int n = 0;
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow) continue;
                r.Cells["colIn"].Value = n == 0 ? "9:00 PM" : "9:10 PM"; r.Cells["colOut"].Value = n == 0 ? "8:30 PM" : "9:30 PM";
                r.Cells["colRep"].Value = n == 0 ? "Maria Santos" : "Ana Reyes";
                r.Cells["colDr"].Value = (5001 + n).ToString();
                n++;
            }
            grid.Rows[0].Cells["colRetG"].Value = Math.Max(0, int.Parse(grid.Rows[0].Cells["colLoadG"].Value?.ToString() ?? "0") - 2).ToString();   // 2 crates not returned: the incident on step 2 will be required
            T.GetMethod("RecalculateTotalsFromStoreGrid", NonPub)!.Invoke(view, null); Pump(200);
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with a store's time out before its time in");
            Pump(200);
            Expect(1, "Next with time out before time in");
            if (!Said("time out can't be earlier than its time in")) bad.Add("no message for time out before time in: " + AllSaid());
            grid.Rows[0].Cells["colOut"].Value = "9:40 PM"; Pump(100);
            if (!view.TryNextStep()) bad.Add("Next refused a valid step 1: " + AllSaid());
            Pump(300);
            Expect(2, "Next from step 1");
            if (!save.Visible || update.Visible || next.Visible || !back.Visible || clear.Visible || !draft.Visible || issue.Visible)
                bad.Add($"step 2 buttons wrong (save {save.Visible}, update {update.Visible}, next {next.Visible}, back {back.Visible}, clear {clear.Visible}, draft {draft.Visible}, issue {issue.Visible})");
            if (!CanJump(1) || CanJump(2)) bad.Add("header clickability wrong at step 2 of a new report");
            if (!pInc.Visible || !pAtt.Visible || pInc.Top >= pAtt.Top) bad.Add($"step 2 cards wrong (incident visible {pInc.Visible} top {pInc.Top}, attachments visible {pAtt.Visible} top {pAtt.Top})");

            // the incident answer is required while crates are missing: Save refuses, nothing is saved
            before = boxes.Boxes.Count;
            T.GetMethod("SaveNewReport", NonPub)!.Invoke(view, new object[] { false }); Pump(500);
            if (!Said("crate(s) are missing")) bad.Add("Save did not require the incident for the missing crates: " + AllSaid());
            if (Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d", ("@d", id)) != 0) bad.Add("a report was saved without the incident answer");
            Expect(2, "Save refused on step 2");

            // Save jumps to the step of the first problem (step 1 comes before step 2)
            var gateIn = FieldOf<TextBox>(view, "txtTruckTimeIn")!;
            gateIn.Text = "";
            before = boxes.Boxes.Count;
            T.GetMethod("SaveNewReport", NonPub)!.Invoke(view, new object[] { false }); Pump(500);
            Expect(1, "Save with a step 1 and a step 2 problem");
            if (!Said("Truck time in (plant gate) is required")) bad.Add("Save did not list the missing gate time: " + AllSaid());
            gateIn.Text = "7:30 PM"; Pump(100);
            // ... a blank DR on step 1 also jumps back from step 2
            grid.Rows[0].Cells["colDr"].Value = ""; Pump(100);
            view.GoToStep(2); Pump(100);
            before = boxes.Boxes.Count;
            T.GetMethod("SaveNewReport", NonPub)!.Invoke(view, new object[] { false }); Pump(500);
            Expect(1, "Save with a step 1 problem");
            grid.Rows[0].Cells["colDr"].Value = "5001"; Pump(100);

            // Back
            view.GoToStep(2); Pump(100);
            back.PerformClick(); Pump(200);
            Expect(1, "Back from step 2");
            if (save.Visible || !next.Visible || !clear.Visible || back.Visible) bad.Add("buttons wrong on step 1 after Back");

            // anything focusing a control jumps to its step first
            T.GetMethod("ShowStepFor", NonPub)!.Invoke(view, new object?[] { FieldOf<RadioButton>(view, "rbIncYes") }); Pump(100);
            Expect(2, "ShowStepFor(incident answer)");
            T.GetMethod("ShowStepFor", NonPub)!.Invoke(view, new object?[] { FieldOf<TextBox>(view, "txtGuardName") }); Pump(100);
            Expect(1, "ShowStepFor(guard name)");
            view.GoToStep(2); Pump(100);

            // answering the incident (step 2) lets Save finish the report (Draft vs finished is unchanged: this is the finished one)
            var yes = FieldOf<RadioButton>(view, "rbIncYes")!;
            yes.Checked = true; Pump(200);
            var add = T.GetMethod("AddIncidentFromFields", NonPub)!;
            FieldOf<TextBox>(view, "txtIncCause")!.Text = "Broken crates"; FieldOf<NumericUpDown>(view, "numIncQty")!.SetSafe(3); FieldOf<NumericUpDown>(view, "numIncCost")!.SetSafe(0);
            if (!(bool)add.Invoke(view, new object[] { false })!) bad.Add("the incident was refused");
            FieldOf<TextBox>(view, "txtIncCause")!.Text = "Missing crates"; FieldOf<NumericUpDown>(view, "numIncQty")!.SetSafe(2);
            if (!(bool)add.Invoke(view, new object[] { false })!) bad.Add("the second incident was refused");
            Pump(200);
            before = boxes.Boxes.Count;
            P4Save(view);
            if (Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId = @d AND Status = 'Completed'", ("@d", id)) != 1) bad.Add("the report was not saved as Completed: " + AllSaid());
            if (Str("SELECT Status FROM Deliveries WHERE Id = @d", ("@d", id)) == "") bad.Add("the dispatch lost its status");
            if (view.CurrentWizardStep != 1) bad.Add("after Save the form is on step " + view.CurrentWizardStep + ", expected 1 (the form closes)");

            // Reset returns to step 1 and clears the form
            T.GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null); Pump(300);
            view.GoToStep(2); Pump(100);
            T.GetMethod("BtnClear_Click", NonPub)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (view.CurrentWizardStep != 1) bad.Add("Reset did not return to step 1");
            if (!clear.Visible || back.Visible) bad.Add("Reset did not restore the step 1 buttons");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string TripReportWizard_SavedReport_HeaderJumpsFreely_ResetReturnsToStep1_FinishedStepsOnly_ForNew()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        Exec(@"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, DeliveredCrates, ReturnedCrates, ReportDate, SpecialTripId, IsVoid, Status)
               VALUES ('TR-WIZ1', @d, @n, 15, 15, @rd, 0, 0, 'Draft')", ("@d", id), ("@n", no), ("@rd", DateTime.Today.ToString("yyyy-MM-dd")));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new P4Boxes();
            var (view, host) = TwMakeView();
            var T = typeof(TripReportView);
            var bad = new List<string>();
            var tr = MJdatabase.GetTripReports().First(t => t.DeliveryId == id);
            T.GetMethod("OpenEditTripReport", NonPub)!.Invoke(view, new object[] { tr }); Pump(1200);
            var panels = new[] { "pnlStep1GateAndStores", "pnlStep2IncidentAndAttachments" }.Select(nm => TwPanel(view, nm)).ToArray();
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!; var issue = FieldOf<Button>(view, "btnIssueExisting")!;
            var draft = FieldOf<Button>(view, "btnSaveDraft")!; var clear = FieldOf<Button>(view, "btnClear")!;
            var next = view.Controls.Find("btnNext", true).OfType<Button>().First();
            var banner = FieldOf<Label>(view, "lblDispatchInfoBanner")!; var footer = FieldOf<Label>(view, "lblDeliveredCrateTotals")!;
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", NonPub)!.Invoke(view, new object[] { n })!;
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong"); break; }
                if (!banner.Visible) bad.Add($"{what}: the dispatch banner is not visible");
                if (!footer.Visible) bad.Add($"{what}: the crate totals footer is not visible");
            }
            Expect(1, "an opened saved report");
            if (!CanJump(2)) bad.Add("step 2 is not clickable on a saved report");
            if (clear.Visible || save.Visible || update.Visible || issue.Visible || !next.Visible) bad.Add($"step 1 of a saved report: clear {clear.Visible}, save {save.Visible}, update {update.Visible}, issue {issue.Visible}, next {next.Visible}");
            view.GoToStep(2); Pump(300);
            Expect(2, "header jump to step 2");
            if (!update.Visible || save.Visible || next.Visible || draft.Visible) bad.Add($"step 2 of a saved report: update {update.Visible}, save {save.Visible}, next {next.Visible}, draft {draft.Visible}");
            if (!issue.Visible) bad.Add("a saved draft does not offer Proceed to SOA on its last step");
            view.GoToStep(1); Pump(200);
            Expect(1, "header jump to step 1");

            // closing and opening a new report starts on step 1 again, with finished-steps-only navigation
            view.GoToStep(2); Pump(100);
            T.GetMethod("SetFormVisibility", NonPub)!.Invoke(view, new object[] { false }); Pump(300);
            T.GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null); Pump(500);
            if (view.CurrentWizardStep != 1) bad.Add("a new report did not open on step 1");
            if (CanJump(2)) bad.Add("a new report has clickable later steps");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string TripReportWizard_IncidentSwitchOff_StillTwoSteps_AttachmentsOnly_BackOn()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string no = NumberOf(id);
        try
        {
            return RunOnSta(() =>
            {
                SignInAsAdmin();
                using var boxes = new P4Boxes();
                var (view, host) = TwMakeView();
                var T = typeof(TripReportView);
                var bad = new List<string>();
                T.GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null); Pump(500);
                PickMain(view, no);
                var pInc = TwPanel(view, "pnlIncidentCard"); var pAtt = TwPanel(view, "pnlAttachmentsCard"); var p2 = TwPanel(view, "pnlStep2IncidentAndAttachments");
                var save = FieldOf<Button>(view, "btnSave")!;
                var next = view.Controls.Find("btnNext", true).OfType<Button>().First();
                var footer = FieldOf<Label>(view, "lblDeliveredCrateTotals")!;
                string StepName(int i) => view.Controls.Find("pnlStepperHeader", true).OfType<WizardStepHeader>().Select(h => (string[])typeof(WizardStepHeader).GetField("names", NonPub)!.GetValue(h)!).First()[i - 1];
                if (StepName(2) != "Incident / Damage Report & save") bad.Add("step 2 is named '" + StepName(2) + "' with the switch on");
                Features.Set(Features.TripIncidents, false, "Incident report in trip reports");
                Pump(300);
                if (view.WizardStepCount != 2) bad.Add("the switch off left " + view.WizardStepCount + " steps");
                if (StepName(1) != "Plant gate, sign-off, stores & DRs" || StepName(2) != "Attachments & save") bad.Add("step names with the switch off: '" + StepName(1) + "' / '" + StepName(2) + "'");
                view.GoToStep(2); Pump(200);
                if (view.CurrentWizardStep != 2) bad.Add("step 2 not reached");
                if (!save.Visible || next.Visible) bad.Add($"step 2 with the switch off: save {save.Visible}, next {next.Visible}");
                if (pInc.Visible) bad.Add("the incident card shows with the switch off");
                if (!pAtt.Visible || !p2.Visible || !TwInside(pAtt, p2)) bad.Add("the attachments card is not shown on step 2 with the switch off");
                if (!footer.Visible) bad.Add("the crate totals footer is hidden");
                view.GoToStep(3); Pump(100);
                if (view.CurrentWizardStep != 2) bad.Add("GoToStep(3) went to step " + view.CurrentWizardStep + " with 2 steps");
                var header = view.Controls.Find("pnlStepperHeader", true).OfType<WizardStepHeader>().ToList();
                if (header.Count != 1) bad.Add("expected one step header, found " + header.Count);
                Features.Set(Features.TripIncidents, true, "Incident report in trip reports");
                Pump(300);
                if (view.WizardStepCount != 2) bad.Add("the switch on left " + view.WizardStepCount + " steps");
                if (StepName(2) != "Incident / Damage Report & save") bad.Add("step 2 is named '" + StepName(2) + "' after the switch came back on");
                next = view.Controls.Find("btnNext", true).OfType<Button>().First();   // the wizard (and its Next button) was rebuilt with the switch
                view.GoToStep(1); Pump(100);
                if (save.Visible || !next.Visible) bad.Add("step 1 buttons wrong after the switch came back on");
                view.GoToStep(2); Pump(200);
                if (!pInc.Visible || !pAtt.Visible || pInc.Top >= pAtt.Top || !save.Visible) bad.Add("the incident card / attachments / Save are not back on step 2 with the switch on");
                host.Close();
                return string.Join("; ", bad);
            });
        }
        finally { Features.Set(Features.TripIncidents, true, "Incident report in trip reports"); Features.Reload(); }
    }

    [Check]
    public static string TripReportWizard_Source_UsesFormWizard_NoOldStepBar()
    {
        string root = RepoRoot();
        var bad = new List<string>();
        string wiz = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Wizard.cs"));
        string wf = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Workflow.cs"));
        string layout = File.ReadAllText(Path.Combine(root, "Views", "TripReportView.Layout.cs"));
        if (!wiz.Contains("new FormWizard(")) bad.Add("the trip report does not use FormWizard");
        if (wf.Contains("class TripStepper") || layout.Contains("BuildTripStepper")) bad.Add("the old TripStepper is still there");
        return string.Join("; ", bad);
    }
}
