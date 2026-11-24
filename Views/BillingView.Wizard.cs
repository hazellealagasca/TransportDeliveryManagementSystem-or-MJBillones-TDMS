using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The New / open Service Invoice form as a 2-step wizard: 1 Invoice &amp; statement (invoice no., the statement of account and its client),
    /// 2 Amounts &amp; save (amount, fuel surcharge, what the invoice adds up to, the checklist, Issue / Update).
    /// One section card is visible at a time; the figures strip above and the action bar below stay on every step.
    /// The step machinery is the shared <see cref="FormWizard"/> (same as New Dispatch Order and the Collection Receipt); this file keeps the invoice validation and wiring.
    /// Every control, handler, lock and save rule is the existing one: this file only regroups and navigates.
    /// </summary>
    public partial class BillingView
    {
        private static readonly string[] WizardStepNames = { "Invoice & statement", "Amounts & save" };

        private FormWizard? wizard;
        private bool saveVisWanted = true, updateVisWanted;
        private Control? wizardHeader;
        private Panel? billBottomSpacer;

        /// <summary>The step (1-2) currently shown.</summary>
        public int CurrentWizardStep => wizard?.CurrentStep ?? 1;

        /// <summary>Shows step 1-2 with no validation (used by the header in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step) => wizard?.GoToStep(step);

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep() => wizard?.TryNextStep() ?? true;

        /// <summary>True on the last step, where Issue / Update and the "to fix" note are shown.</summary>
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

        /// <summary>The problem list sits under the amounts (last step); a new invoice can't jump ahead to it.</summary>
        private void ShowBillChecklist()
        {
            if (wizard != null && wizard.CurrentStep < wizard.StepCount && wizard.CanJump(wizard.StepCount)) wizard.GoToStep(wizard.StepCount);
            formKit?.ScrollTo(billChecklist);
        }

        /// <summary>
        /// Step 1 on Next: the Service Invoice No. is entered and free (the same checks and messages Issue / Update use), and a statement of account
        /// (or, for a direct invoice opened from another screen, a trip) is picked. A saved invoice already has its statement; a Paid one is read-only.
        /// </summary>
        private bool ValidateWizardStep(int step)
        {
            if (step != 1) return true;
            if (selectedInvoice != null && IsInvoiceLocked(selectedInvoice)) return true;
            ValidationHelper.ClearErrors(pnlForm);
            string invNum = txtInvoiceNumber.Text.Trim();
            billNumberGuard?.Recheck();
            string? numWhy = MJdatabase.CheckSiNumber(invNum, selectedInvoice?.Id ?? 0);
            if (numWhy == null && billNumberGuard != null && billNumberGuard.TickMissing)
                numWhy = "Tick the confirmation under the Service Invoice No. (same digits as another document)";
            if (numWhy != null)
            {
                ValidationHelper.ApplyValidationState(txtInvoiceNumber, false, numWhy);
                FocusStep(txtInvoiceNumber);
                MessageBox.Show(FindForm(), numWhy, "Service Invoice No.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (selectedInvoice != null) return true;
            if (SoaModeOn)
            {
                if (SelectedSoa() != null) return true;
                const string why = "Pick a Statement of Account to invoice.";
                ValidationHelper.ApplyValidationState(cbSoa, false, why);
                FocusStep(cbSoa);
                MessageBox.Show(FindForm(), why, "Statement of Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (SelectedBillDelivery() != null) return true;
            const string whyTrip = "Please select a completed trip to invoice.";
            FocusStep(cbDelivery);
            MessageBox.Show(FindForm(), whyTrip, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>Builds the wizard over the two section cards the form kit made: header on top of the body, Back / Clear form left, Next / Issue / Update right.</summary>
        private void BuildBillWizard()
        {
            if (wizard != null || formKit == null || formKit.SectionCards.Count != WizardStepNames.Length || formKit.Actions == null) return;
            var cards = formKit.SectionCards.Select(c => (Panel)c).ToArray();
            wizard = new FormWizard(this, WizardStepNames, () => selectedInvoice != null, ValidateWizardStep);
            wizard.FirstStepButton = btnClear;
            wizard.ScrollHost = pnlFormBody;
            wizard.AddLastStepButton(btnSave, () => saveVisWanted);
            wizard.AddLastStepButton(btnUpdate, () => updateVisWanted);
            wizard.SetPanels(cards);
            cards[0].Name = "pnlStep1InvoiceStatement"; cards[1].Name = "pnlStep2AmountsSave";
            foreach (var gap in formKit.SectionSpacers) gap.Visible = false; // only one card shows at a time
            wizard.StepChanged = n =>
            {
                bool last = n >= wizard.StepCount;
                if (billChecklist != null) billChecklist.Visible = last;   // the problem list belongs to the amounts step
                if (billBottomSpacer != null) billBottomSpacer.Visible = last;
                ScheduleBillGuide();
            };
            foreach (var b in new[] { wizard.BackButton, wizard.NextButton })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI Semibold", 9.5F);
            }
            // Right-to-left bar: Next first = far right (Issue / Update take its place on the last step).
            formKit.Actions.Controls.Add(wizard.NextButton);
            formKit.Actions.Controls.SetChildIndex(wizard.NextButton, 0);
            wizard.BuildLeftActions(pnlFormActionsBar);
            wizardHeader = wizard.BuildHeader();
            pnlFormBody.Controls.Add(wizardHeader);   // docked top: the last control added sits highest
            wizard.GoToStep(1);
        }
    }
}
