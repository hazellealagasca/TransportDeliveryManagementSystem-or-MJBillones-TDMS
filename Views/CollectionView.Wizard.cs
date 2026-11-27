using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The New / open Collection Receipt form as a 2-step wizard: 1 What is being paid (the statement or invoice), 2 Payment &amp; save.
    /// One section card is visible at a time; the figures strip above and the action bar below stay on every step.
    /// The step machinery is the shared <see cref="FormWizard"/> (same as New Dispatch Order); this file keeps the receipt validation and wiring.
    /// Every control, handler, lock and save rule is the existing one: this file only regroups and navigates.
    /// </summary>
    public partial class CollectionView
    {
        private static readonly string[] WizardStepNames = { "What is being paid", "Payment & save" };

        private FormWizard? wizard;
        private bool saveVisWanted = true, updateVisWanted;
        private Control? wizardHeader;

        /// <summary>The step (1-2) currently shown.</summary>
        public int CurrentWizardStep => wizard?.CurrentStep ?? 1;

        /// <summary>Shows step 1-2 with no validation (used by the header in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step) => wizard?.GoToStep(step);

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep() => wizard?.TryNextStep() ?? true;

        /// <summary>True on the last step, where Save / Update and the "to fix" note are shown.</summary>
        private bool OnLastWizardStep => wizard == null || wizard.CurrentStep >= wizard.StepCount;

        private bool CanJumpToStep(int step) => wizard != null && wizard.CanJump(step);

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        private void ShowStepFor(Control? c) => wizard?.ShowStepFor(c);

        /// <summary>Shows the step of <paramref name="c"/>, then focuses it.</summary>
        private void FocusStep(Control? c)
        {
            if (c == null) return;
            if (wizard != null) wizard.Focus(c); else c.Focus();
        }

        /// <summary>The problem list sits under the payment fields (last step); a new receipt can't jump ahead to it.</summary>
        private void ShowCollChecklist()
        {
            if (wizard != null && wizard.CurrentStep < wizard.StepCount && wizard.CanJump(wizard.StepCount)) wizard.GoToStep(wizard.StepCount);
            formKit?.ScrollTo(collChecklist);
        }

        /// <summary>Step 1 on Next: a statement or invoice must be picked (the same message Save shows). A saved receipt already has its statement.</summary>
        private bool ValidateWizardStep(int step)
        {
            if (step != 1) return true;
            if (selectedCollection != null) return true;
            if (cbInvoice.SelectedIndex >= 0)
            {
                // A new receipt: the overall amount must be ticked as checked here, on step 1, before going to Payment & save.
                if (chkVerifyAmount == null || chkVerifyAmount.Checked || !chkVerifyAmount.Enabled) return true;
                FocusStep(chkVerifyAmount);
                MessageBox.Show(FindForm(), "Tick \"I checked the overall amount\" under the linked invoice first. The receipt is issued only for the amount you checked.", "Verify the overall amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            const string why = "Pick the statement or invoice being paid.";
            ValidationHelper.ClearErrors(pnlForm);
            ValidationHelper.ApplyValidationState(cbInvoice, false, why);
            return ValidationHelper.ShowValidationSummaryIfAny(this, new List<string> { why }, cbInvoice);
        }

        /// <summary>Builds the wizard over the two section cards the form kit made: header on top of the body, Back / Reset left, Next / Save right.</summary>
        private void BuildCollWizard()
        {
            if (wizard != null || formKit == null || formKit.SectionCards.Count != WizardStepNames.Length || formKit.Actions == null) return;
            var cards = formKit.SectionCards.Select(c => (Panel)c).ToArray();
            wizard = new FormWizard(this, WizardStepNames, () => selectedCollection != null, ValidateWizardStep);
            wizard.FirstStepButton = btnClear;
            wizard.ScrollHost = pnlFormBody;
            wizard.AddLastStepButton(btnSave, () => saveVisWanted);
            wizard.AddLastStepButton(btnUpdate, () => updateVisWanted);
            wizard.SetPanels(cards);
            cards[0].Name = "pnlStep1WhatIsPaid"; cards[1].Name = "pnlStep2PaymentSave";
            foreach (var gap in formKit.SectionSpacers) gap.Visible = false; // only one card shows at a time
            wizard.StepChanged = n =>
            {
                bool last = n >= wizard.StepCount;
                if (collChecklist != null) collChecklist.Visible = last;   // the problem list belongs to the payment step
                if (collBottomSpacer != null) collBottomSpacer.Visible = last;
                ScheduleCollGuide();
            };
            foreach (var b in new[] { wizard.BackButton, wizard.NextButton })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); // Nov 28: Back and Next in bold
            }
            // Right-to-left bar: Next first = far right (Save / Update take its place on the last step).
            formKit.Actions.Controls.Add(wizard.NextButton);
            formKit.Actions.Controls.SetChildIndex(wizard.NextButton, 0);
            wizard.BuildLeftActions(pnlFormActionsBar);
            wizardHeader = wizard.BuildHeader();
            pnlFormBody.Controls.Add(wizardHeader);   // docked top: the last control added sits highest
            wizard.GoToStep(1);
        }
    }
}
