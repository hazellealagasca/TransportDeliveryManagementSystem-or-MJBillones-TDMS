using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The New / open Trip Report form on the shared <see cref="FormWizard"/> (same header, footer and Next rules as New Dispatch Order and the Collection Receipt):
    /// Nov 26: always 2 steps. 1 Plant gate, sign-off, stores &amp; DRs (the plant gate card, then the stores card), 2 Incident / Damage Report, then Attachments, &amp; save.
    /// While <see cref="Features.TripIncidents"/> is off step 2 is "Attachments &amp; save": the incident card is hidden inside it, Save is still on step 2.
    /// The dispatch banner / overview card stays above the steps and the delivered-crate totals sit above the action bar on every step.
    /// Every field, handler, check and save is the existing one: this file only groups them into steps and navigates.
    /// </summary>
    public partial class TripReportView
    {
        private static readonly string[] TripStepNames3 = { "Plant gate, sign-off, stores & DRs", "Incident / Damage Report & save" };   // incident switch on
        private static readonly string[] TripStepNames2 = { "Plant gate, sign-off, stores & DRs", "Attachments & save" };   // incident switch off

        private FormWizard? wizard;
        private Panel? pnlStepGateStores, pnlStepIncidentAttach, cardAttachments, incidentAttachSpacer, tripHeaderHost, pnlTripCrateFooter;
        private bool? wizardIncidentOn;   // Nov 26: the incident switch state the wizard was built for (the step count is always 2 now)
        private Control? tripWizardLeft;
        private Panel[] tripStepPanels = Array.Empty<Panel>();

        /// <summary>The step (1-2) currently shown.</summary>
        public int CurrentWizardStep => wizard?.CurrentStep ?? 1;

        /// <summary>How many steps the form has (always 2 since Nov 26).</summary>
        public int WizardStepCount => wizard?.StepCount ?? 2;

        /// <summary>Shows a step with no validation (used by the header of a saved report, Back, Reset and error jumps).</summary>
        public void GoToStep(int step) => wizard?.GoToStep(step);

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep() => wizard?.TryNextStep() ?? true;

        private bool CanJumpToStep(int step) => wizard != null && wizard.CanJump(step);

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        private void ShowStepFor(Control? c) => wizard?.ShowStepFor(c);

        /// <summary>The step (1-based) a field belongs to; fields outside the step panels (the hidden manager-name and crate boxes) count as step 1.</summary>
        private int StepOfControl(Control? c)
        {
            for (var p = c; p != null; p = p.Parent)
            {
                int i = Array.IndexOf(tripStepPanels, p as Panel);
                if (i >= 0) return i + 1;
            }
            return 1;
        }

        /// <summary>Of two problem fields, the one on the earlier step (the first one wins a tie), so Save jumps to the first step with a problem.</summary>
        private Control? EarlierStep(Control? a, Control? b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return StepOfControl(b) < StepOfControl(a) ? b : a;
        }

        /// <summary>The one validation pop-up: shows the step of the first problem field first, so the field gets its focus.</summary>
        private bool ShowFormProblems(List<string> messages, Control? first)
        {
            if (messages.Count > 0) ShowStepFor(first);
            return ValidationHelper.ShowValidationSummaryIfAny(this, messages, first);
        }

        /// <summary>Next on step 1: the form's own messages for the fields of that step (the same checks Save runs).</summary>
        private bool ValidateTripStep(int step)
        {
            // Nov 26: problems are listed (and the cursor goes) in on-screen order: the Plant gate & sign-off box comes before the stores on step 1
            var found = new List<(string Message, Control? Field)>();
            void Add(string m, Control? f) => found.Add((m, f));
            ValidationHelper.ClearErrors(pnlFormBody);
            try { gridStores?.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (step == 1 && !isEditingExistingReport)
            {
                // The two checks a new report's Save makes before the shared list.
                if (string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text))
                {
                    ValidationHelper.ApplyValidationState(txtStoreManagerLastName, false, "Store Manager / Representative is required.");
                    Add("Who received the delivery is required: type a name in \"Received by\" for at least one store.", (Control?)gridStores ?? txtStoreManagerLastName);
                }
                bool forSpecialTrip = (GetSelectedDelivery()?.Id ?? 0) < 0;
                if ((int)numDeliveredGCrates.Value + (int)numDeliveredECrates.Value <= 0 && !forSpecialTrip)
                {
                    ValidationHelper.ApplyValidationState(numDeliveredGCrates, false, "Total delivered crates must be greater than zero.");
                    Add("Total delivered crates must be greater than 0.", numDeliveredGCrates);
                }
            }
            foreach (var (message, field) in BlankFormChecks())
            {
                if (StepOfControl(field) == step) Add(message, field);
                else ValidationHelper.ApplyValidationState(field, true, "");   // a later step's field is not marked yet
            }
            var plantGate = pnlFormBody.Controls.Find("pnlPlantGateCard", true).FirstOrDefault();
            bool InPlantGate(Control? c) { for (var p = c; p != null; p = p.Parent) if (p == plantGate) return true; return false; }
            var ordered = found.Select((x, i) => (x, i)).OrderBy(t => InPlantGate(t.x.Field) ? 0 : 1).ThenBy(t => t.i).Select(t => t.x).ToList();
            return ShowFormProblems(ordered.Select(x => x.Message).ToList(), ordered.Select(x => x.Field).FirstOrDefault(c => c != null));
        }

        /// <summary>Re-applies the Save / Update / Proceed buttons and Reset for the current mode (new report or saved report).</summary>
        private void RefreshTripWizard()
        {
            wizard?.Refresh();
            ApplyTripResetVisibility();
            tripHeaderHost?.Invalidate(true);
        }

        /// <summary>"Clear form" belongs to a new report only (a saved report is not cleared).</summary>
        private void ApplyTripResetVisibility()
        {
            if (btnClear != null && isEditingExistingReport) btnClear.Visible = false;
        }

        /// <summary>
        /// Builds (or, when the trip-incident switch changed, rebuilds with the step 2 name for it) the wizard over the two step panels:
        /// header in the body above the steps, Reset / Back on the left of the action bar, Next / Save on the right.
        /// </summary>
        private void BuildTripWizard()
        {
            if (flowFormActions == null || tripHeaderHost == null || pnlStepGateStores == null || pnlStepIncidentAttach == null || cardIncident == null) return;
            bool incOn = IncidentStepOn;
            const int count = 2;
            // Nov 26: inside step 2 the incident card (and the spacer under it) show only while the switch is on; FormWizard only shows / hides the step panel itself.
            cardIncident.Visible = incOn;
            if (incidentAttachSpacer != null) incidentAttachSpacer.Visible = incOn;
            cardAttachments?.Invalidate(true);   // its badge number depends on the switch
            if (wizard != null && wizardIncidentOn == incOn) { RefreshTripWizard(); return; }
            wizardIncidentOn = incOn;
            int keepStep = wizard?.CurrentStep ?? 1;
            if (wizard != null)
            {
                flowFormActions.Controls.Remove(wizard.NextButton);
                if (tripWizardLeft != null) { tripWizardLeft.Controls.Clear(); pnlFormActionsBar.Controls.Remove(tripWizardLeft); tripWizardLeft.Dispose(); tripWizardLeft = null; }
                wizard.BackButton.Dispose();
                wizard.NextButton.Dispose();
                foreach (var old in tripHeaderHost.Controls.Cast<Control>().ToList()) { tripHeaderHost.Controls.Remove(old); old.Dispose(); }
            }
            var w = new FormWizard(this, incOn ? TripStepNames3 : TripStepNames2, () => isEditingExistingReport, ValidateTripStep);
            wizard = w;
            w.FirstStepButton = btnClear;
            w.ScrollHost = pnlFormBody;
            w.AddLastStepButton(btnSave, () => !isEditingExistingReport);
            w.AddLastStepButton(btnUpdate, () => isEditingExistingReport);
            w.AddLastStepButton(btnIssueExisting, () => isEditingExistingReport && selectedTripReport != null && !RowMenu.Is(selectedTripReport.Status, TripReportStatus.Invoiced));
            tripStepPanels = new[] { pnlStepGateStores, pnlStepIncidentAttach };
            w.SetPanels(tripStepPanels);
            w.StepChanged = _ => ApplyTripResetVisibility();
            foreach (var b in new[] { w.BackButton, w.NextButton })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI Semibold", 9.5F);
            }
            w.NextButton.Width = Dpi(320);
            // Right-to-left bar: Next first = far right (Proceed to SOA / Save changes take its place on the last step).
            flowFormActions.Controls.Add(w.NextButton);
            flowFormActions.Controls.SetChildIndex(w.NextButton, 0);
            var before = new HashSet<Control>(pnlFormActionsBar.Controls.Cast<Control>());
            w.BuildLeftActions(pnlFormActionsBar);
            tripWizardLeft = pnlFormActionsBar.Controls.Cast<Control>().FirstOrDefault(c => !before.Contains(c));
            var header = w.BuildHeader();
            tripHeaderHost.Controls.Add(header);
            w.GoToStep(Math.Min(keepStep, count));
            RefreshTripWizard();
        }

        /// <summary>The delivered-crate totals, always visible just above the action bar (like the crate footer of New Dispatch Order).</summary>
        private void BuildTripCrateFooter()
        {
            if (pnlTripCrateFooter != null || lblDeliveredCrateTotals == null) return;
            var lbl = lblDeliveredCrateTotals;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            pnlTripCrateFooter = new Panel { Name = "pnlTripCrateFooter", Dock = DockStyle.Bottom, Height = Dpi(44), BackColor = AppTheme.Surface };
            pnlTripCrateFooter.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, pnlTripCrateFooter!.Width, 0); };
            pnlTripCrateFooter.Controls.Add(lbl);
            pnlForm.Controls.Add(pnlTripCrateFooter);
            pnlForm.Controls.SetChildIndex(pnlTripCrateFooter, pnlForm.Controls.GetChildIndex(pnlFormActionsBar));   // just above the action bar
        }
    }
}
