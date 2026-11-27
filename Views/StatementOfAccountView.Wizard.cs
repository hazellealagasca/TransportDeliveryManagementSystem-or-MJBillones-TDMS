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
    /// The New / open Statement of Account form as a 3-step wizard: 1 Client &amp; trips, 2 Trips &amp; totals, 3 Sign-off &amp; save.
    /// One step is visible at a time; the totals strip in the header and the action bar below stay on every step.
    /// The step machinery is the shared <see cref="FormWizard"/> (same as New Dispatch Order and the Collection Receipt); this file keeps the
    /// per-step validation (the save's own messages) and the wiring. Every control, calculation, number, lock and the save itself is the existing one.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private static readonly string[] SoaStepNames = { "Client & trips", "Trips & totals", "Sign-off & save" };

        private FormWizard? wizard;
        private Control? wizardHeader;

        /// <summary>The step (1-3) currently shown.</summary>
        public int CurrentWizardStep => wizard?.CurrentStep ?? 1;

        /// <summary>Shows step 1-3 with no validation (header in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step) => wizard?.GoToStep(step);

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep() => wizard?.TryNextStep() ?? true;

        /// <summary>True on the last step, where Save and the "to fix" note are shown.</summary>
        private bool OnLastSoaStep => wizard == null || wizard.CurrentStep >= wizard.StepCount;

        private bool CanJumpToStep(int step) => wizard != null && wizard.CanJump(step);

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        private void ShowStepFor(Control? c) => wizard?.ShowStepFor(c);

        /// <summary>Shows the step of <paramref name="c"/>, then focuses it.</summary>
        private void FocusStep(Control? c)
        {
            if (c == null) return;
            if (wizard != null) wizard.Focus(c); else c.Focus();
        }

        /// <summary>The problem list sits on the last step; a new statement can't jump ahead to it.</summary>
        private void ShowSoaChecklist()
        {
            if (wizard != null && wizard.CurrentStep < wizard.StepCount && wizard.CanJump(wizard.StepCount)) wizard.GoToStep(wizard.StepCount);
            try { pnlFormBody.ScrollControlIntoView(soaChecklist); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Re-applies which buttons the current step and mode show (Reset only on step 1 of a new statement; Save on the last step of an editable one).</summary>
        private void SyncSoaWizardButtons()
        {
            wizard?.Refresh();
            if (btnClearForm != null && (isEditingExisting || soaViewOnly)) btnClearForm.Visible = false;
            wizardHeader?.Invalidate();
        }

        private bool SoaWarn(string text, string caption, Control? focus)
        {
            // Nov 25: missing input is tinted light red, the step is shown, the box scrolled to and focused after OK
            if (caption == "Missing input" && focus != null) { MissingInput.Show(focus, text, FindForm(), FocusStep, caption); return false; }
            if (focus != null) { ShowStepFor(focus); MissingInput.Mark(focus); }
            MessageBox.Show(FindForm(), text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            FocusStep(focus);
            return false;
        }

        /// <summary>
        /// Next: step 1 needs the client, delivery type, a valid period, the client's TIN and at least one trip line; step 2 needs real amounts and the DR numbers.
        /// Same messages as Save. A statement opened view-only is never validated.
        /// </summary>
        private bool ValidateSoaStep(int step)
        {
            if (soaViewOnly) return true;
            try { gridItems.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (step == 1)
            {
                if (string.IsNullOrWhiteSpace(cbClient.Text)) return SoaWarn("Please select or enter the client company name.", "Missing input", cbClient);
                if (cbDeliveryType.SelectedIndex < 0) return SoaWarn("Please choose the delivery type (REF or DRY).", "Missing input", cbDeliveryType);
                if (dtpPeriodTo.Value.Date < dtpPeriodFrom.Value.Date) return SoaWarn("The period's end date is before its start date.", "Check the dates", dtpPeriodFrom);
                var profile = ClientProfileDetails(cbClient.Text);
                string tin = new string((profile.Found ? profile.Tin : txtClientTin.Text).Where(char.IsDigit).ToArray());
                if (!MJdatabase.IsIndividualClient(cbClient.Text) && tin.Length != 9 && tin.Length != 12)
                    return SoaWarn(tin.Length == 0 ? "The client's TIN is required on a statement. It comes from the client's profile: add it in Master Data › Clients (edit the client), then pick the client again." : "The TIN should have 9 digits (or 12 with the branch code), e.g. 006-896-371-000.", "Client TIN required", txtClientTin);
                if (!gridItems.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow))
                    return SoaWarn("Please add at least one trip or store line to the Statement of Account. The client's trips are put on it when the client is picked.", "Missing input", gridPicker);
                return true;
            }
            if (step == 2)
            {
                if (!gridItems.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow))
                    return SoaWarn("Please add at least one trip or store line to the Statement of Account.", "Missing input", gridItems);
                if (numTotalSales.Value < 0 || numTotalAmountDue.Value <= 0)
                    return SoaWarn(numTotalSales.Value < 0
                        ? $"Total sales is negative (PHP {numTotalSales.Value:N2}). Check the amounts and deductions on the trip lines."
                        : "The total amount due is PHP 0.00. Enter the amount for each trip line before saving.\n\nSaving creates an invoice for the client, so it must be more than PHP 0.00.",
                        "Check amounts", gridItems);
                var noDr = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && DrList.Parse(r.Cells["colDr"].Value?.ToString()).Count == 0).Select(SoaLineLabel).ToList();
                if (noDr.Count > 0)
                    return SoaWarn("A Delivery Receipt (DR) number is required on every trip line. Missing on:\n\n• " + string.Join("\n• ", noDr.Take(10)) + (noDr.Count > 10 ? $"\n• … and {noDr.Count - 10} more" : "") + "\n\nFill the DR numbers in the Trip Report (stores table), then Refresh lines from trip reports.", "DR number required", gridItems);
                var badDr = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).SelectMany(r => DrList.Parse(r.Cells["colDr"].Value?.ToString())).Where(d => !System.Text.RegularExpressions.Regex.IsMatch(d, @"^\d{4,8}$")).Distinct().ToList();
                if (badDr.Count > 0)
                    return SoaWarn("A DR number must be 4 to 8 digits (numbers only). Fix:\n\n• " + string.Join("\n• ", badDr.Take(10)), "DR number", gridItems);
            }
            return true;
        }

        /// <summary>Builds the wizard over the three step panels: header on top of the body, Back / Reset left, Next / Save right.</summary>
        private void BuildSoaWizard(Panel step1, Panel step2, Panel step3)
        {
            if (wizard != null) return;
            wizard = new FormWizard(this, SoaStepNames, () => isEditingExisting && selectedSoa != null, ValidateSoaStep);
            wizard.FirstStepButton = btnClearForm;
            wizard.ScrollHost = pnlFormBody;
            wizard.AddLastStepButton(btnSaveSoa, () => !soaViewOnly);
            wizard.SetPanels(step1, step2, step3);
            wizard.StepChanged = n =>
            {
                bool last = n >= wizard.StepCount;
                if (btnClearForm != null && (isEditingExisting || soaViewOnly)) btnClearForm.Visible = false;
                if (lblSaveBlocked != null) lblSaveBlocked.Visible = last && !soaViewOnly && lblSaveBlocked.Text.Length > 0;
                if (n == 2) { SizeItemsGrid(); sheetTotals?.Relayout(); }
                try { gridItems.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            };
            foreach (var b in new[] { wizard.BackButton, wizard.NextButton })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); // Nov 28: Back and Next in bold
            }
            // Right-to-left bar: Next first = far right (Save takes its place on the last step).
            flowFormActions.Controls.Add(wizard.NextButton);
            flowFormActions.Controls.SetChildIndex(wizard.NextButton, 0);
            wizard.BuildLeftActions(pnlFormActionsBar);
            wizardHeader = wizard.BuildHeader();
            pnlFormBody.Controls.Add(wizardHeader);   // docked top: the last control added sits highest
            wizard.GoToStep(1);
        }
    }
}
