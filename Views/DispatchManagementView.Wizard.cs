using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The New / Edit Dispatch Order form as a 3-step wizard: 1 Client &amp; stores, 2 Crates, truck &amp; crew,
    /// 3 Schedule &amp; status. One step card is visible at a time; the crate footer and the action bar stay on every step.
    /// The step machinery is the shared <see cref="FormWizard"/>; this file keeps the dispatch validation and wiring.
    /// All controls, handlers and save validation are the existing ones: this file only regroups and navigates.
    /// </summary>
    public partial class DispatchManagementView
    {
        private static readonly string[] WizardStepNames = { "Client & stores", "Crates, truck & crew", "Schedule & status" };

        private Panel? pnlStep1ClientStores, pnlStep2LoadCrew, pnlStep3Schedule;
        private Control? pnlStepperHeader;
        private Button? btnBack, btnNext;
        private FormWizard? wizard;
        private bool saveVisWanted, updateVisWanted, rescheduleVisWanted;

        /// <summary>The step (1-3) currently shown.</summary>
        public int CurrentWizardStep => wizard?.CurrentStep ?? 1;

        /// <summary>Shows step 1-3 with no validation (used by the stepper in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step) => wizard?.GoToStep(step);

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep() => wizard?.TryNextStep() ?? true;

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        private void ShowStepFor(Control? c) => wizard?.ShowStepFor(c);

        private void FocusStep(Control c)
        {
            ShowStepFor(c);
            c.Focus();
        }

        private bool IsWizardEditMode => selectedDelivery != null;

        private bool CanJumpToStep(int step) => wizard != null && wizard.CanJump(step);
        private bool ValidateWizardStep(int step)
        {
            switch (step)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(txtDispatchNumber.Text))
                    {
                        MessageBox.Show("Please enter the Order Reference / Dispatch #.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtDispatchNumber.Focus();
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(cbClient.Text))
                    {
                        MessageBox.Show("Please select or enter a Client Company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cbClient.Focus();
                        return false;
                    }
                    if (cbTempType != null && cbTempType.SelectedIndex < 0)
                    {
                        MessageBox.Show("Please choose a Temp Type (Dry, Cold or Combi) for this load.", "Temp Type Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cbTempType.Focus();
                        return false;
                    }
                    if (!ResolvePendingEntryRow()) return false;
                    int stores = chkMultiDrop.Checked && currentStops.Count > 0 ? currentStops.Count : GetGridStoreList().Count;
                    if (stores == 0)
                    {
                        MessageBox.Show("Please add at least one store to this order (fill in the store row and click Add store).", "Store required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cbDestination.Visible) cbDestination.Focus(); else txtEntryCode?.Focus();
                        return false;
                    }
                    return true;
                case 2:
                    if ((int)numGCrate.Value + (int)numECrate.Value <= 0)
                    {
                        MessageBox.Show("Please enter at least 1 crate for dispatch.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        numGCrate.Focus();
                        return false;
                    }
                    if (cbTruck.SelectedIndex < 0 || cbDriver.SelectedIndex < 0)
                    {
                        MessageBox.Show("STRICT FLEET ASSIGNMENT REQUIRED:\nPlease select both an assigned Truck Vehicle and Driver before saving this dispatch order.", "Fleet Assignment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cbTruck.SelectedIndex < 0) cbTruck.Focus(); else cbDriver.Focus();
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        /// <summary>Remembers which of Save / Update / Reschedule the current mode wants (SetButtonStates decides); the last step shows them.</summary>
        private void RememberSaveButtons()
        {
            wizard?.Refresh();
        }

        private void CreateWizardButtons()
        {
            if (wizard != null) return;
            wizard = new FormWizard(this, WizardStepNames, () => IsWizardEditMode, ValidateWizardStep);
            wizard.FirstStepButton = btnClear;
            wizard.ScrollHost = pnlFormBody;
            wizard.AddLastStepButton(btnSave, () => saveVisWanted);
            wizard.AddLastStepButton(btnUpdate, () => updateVisWanted);
            wizard.AddLastStepButton(btnReschedule, () => rescheduleVisWanted);
            btnBack = wizard.BackButton;
            btnNext = wizard.NextButton;
        }

        /// <summary>Reset form (step 1) / Back (steps 2-3) sit on the left of the action bar; Next / Save on the right.</summary>
        private void BuildWizardLeftActions()
        {
            CreateWizardButtons();
            wizard!.BuildLeftActions(pnlFormActionsBar);
        }

        private void BuildWizardStepper()
        {
            if (pnlStepperHeader != null) { pnlFormBody.Controls.Remove(pnlStepperHeader); }
            CreateWizardButtons();   // the form body can be built before the action bar: make the wizard on first use (it only makes it once)
            wizard!.SetPanels(pnlStep1ClientStores!, pnlStep2LoadCrew!, pnlStep3Schedule!);
            pnlStepperHeader = wizard.BuildHeader();
        }
    }
}
