using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Layout of the Statement of Account form: three step panels, one visible at a time (the wizard itself is in StatementOfAccountView.Wizard.cs).
    /// Layout only: the section cards, every field and every handler are the same controls, re-homed.
    ///   Step 1 "Client &amp; trips": the client card, then the read-only list of the client's trips.
    ///   Step 2 "Trips &amp; totals": the trips table (with the sheet's totals block), the live totals tape with the total amount due, the adjustment reason.
    ///   Step 3 "Sign-off &amp; save": the sign-off, the open problems (checklist) and, for a statement that can't be edited, its history.
    /// The totals strip in the header (Sales + fuel, Withholding, Total due) stays on every step.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private Panel? soaCardClient, soaCardPicker, soaCardLines, soaCardTotals, soaCardSign;
        private TapeCard? soaTape;
        private bool soaWorkbench;

        private void ApplySoaWizard()
        {
            if (soaWorkbench || soaCardClient == null || soaCardPicker == null || soaCardLines == null || soaCardTotals == null || soaCardSign == null) return;
            soaWorkbench = true;
            pnlForm.SuspendLayout(); pnlFormBody.SuspendLayout(); pnlFormHeaderBar.SuspendLayout();
            foreach (Control? h in new Control?[] { soaStepper, soaHelp }) if (h != null) { h.Visible = false; h.Height = 0; }
            soaChecklist.ShowOnlyProblems = true;
            soaChecklist.FocusResolver = c =>
            {
                string t = c.Text;
                Control target = gridItems; // everything else is about the trip lines or their invoices
                if (t.Contains("adjustment reason", StringComparison.OrdinalIgnoreCase) || t.Contains("changed by hand", StringComparison.OrdinalIgnoreCase)) target = txtSoaAdjustReason;
                else if (t.Contains("client", StringComparison.OrdinalIgnoreCase)) target = cbClient;
                else if (t.Contains("period", StringComparison.OrdinalIgnoreCase)) target = dtpPeriodFrom;
                else if (t.Contains("statement date", StringComparison.OrdinalIgnoreCase)) target = dtpIssueDate;
                ShowStepFor(target); // the control must be on the step that is showing before it is focused
                return target;
            };

            // ---- the client card: 7 fields in 4 columns instead of 2 (saves a third of its height); the period dates take two columns
            lblPopulateHint.Visible = false;
            var fieldGrid = soaCardClient.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (fieldGrid != null) ReflowFieldGrid(fieldGrid, 4, c => c.Controls.Cast<Control>().Any(x => x == pnlDatesHost()));

            // ---- the trips card sits directly under the client card with its number badge and title only (no subtitle line, no divider)
            { int n = soaCardPicker.Controls.Count; if (n >= 2) { soaCardPicker.Controls[n - 1].Tag = "titleOnly"; soaCardPicker.Controls[n - 1].Height = Dpi(38); soaCardPicker.Controls[n - 2].Height = Dpi(4); } }

            // ---- detach the stacked cards, then rebuild as three step panels
            var keepBoxes = pnlFormBody.Controls.Cast<Control>().Where(c => c is TextBox).ToList(); // the hidden SOA number box
            foreach (Control c in pnlFormBody.Controls.Cast<Control>().ToList()) pnlFormBody.Controls.Remove(c);

            Panel Step(string name) => new Panel { Name = name, Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0), Padding = new Padding(Dpi(24), Dpi(16), Dpi(24), Dpi(16)), Visible = false };
            Panel Gap() => new Panel { Dock = DockStyle.Top, Height = Dpi(14), BackColor = AppTheme.Canvas };
            // cards dock to the top: the last one added sits highest
            soaCardClient.Dock = DockStyle.Top; soaCardPicker.Dock = DockStyle.Top; soaCardLines.Dock = DockStyle.Top; soaCardSign.Dock = DockStyle.Top;
            pnlSoaAdjust.Dock = DockStyle.Top;

            soaTape = new TapeCard { EmptyText = "Pick the ready service invoices to see the statement add up.", Dock = DockStyle.Top };
            soaTape.Set("The statement adds up to", Array.Empty<(string, string)>(), "Total amount due", "₱0.00", "");
            var tapeHost = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            tapeHost.Controls.Add(soaTape);

            BuildSoaHistoryCard(); // shown only when a statement that can't be edited is opened (Nov 8)
            soaChecklist.Dock = DockStyle.Top;

            var step1 = Step("pnlStep1ClientInvoices");
            step1.Controls.Add(soaCardPicker);
            step1.Controls.Add(Gap());
            step1.Controls.Add(soaCardClient);

            var step2 = Step("pnlStep2TripsTotals");
            step2.Controls.Add(pnlSoaAdjust);
            step2.Controls.Add(Gap());
            step2.Controls.Add(tapeHost);
            step2.Controls.Add(Gap());
            step2.Controls.Add(soaCardLines);

            var step3 = Step("pnlStep3SignSave");
            step3.Controls.Add(soaChecklist);
            step3.Controls.Add(Gap());
            step3.Controls.Add(soaHistoryCard!);
            step3.Controls.Add(Gap());
            step3.Controls.Add(soaCardSign);

            pnlFormBody.AutoScroll = true;
            pnlFormBody.Controls.Add(step3);
            pnlFormBody.Controls.Add(step2);
            pnlFormBody.Controls.Add(step1);
            // the old totals card keeps its boxes (they are read-only and still feed the tape), but is not shown
            soaCardTotals.Visible = false;
            pnlFormBody.Controls.Add(soaCardTotals);
            foreach (var k in keepBoxes) pnlFormBody.Controls.Add(k);
            Smooth.Enable(step1); Smooth.Enable(step2); Smooth.Enable(step3); Smooth.Enable(pnlFormBody);

            // one slim header: Back + title + the live figures (on every step)
            soaStrip.Compact = true;
            soaStrip.ShowPayment = false;
            soaStrip.FirstCaption = "Sales + fuel"; soaStrip.SecondCaption = "Withholding tax"; soaStrip.NetCaption = "Total due"; // Nov 7: the sheet's total
            soaStrip.Padding = new Padding(0);
            pnlForm.Controls.Remove(soaStrip);
            soaStrip.Dock = DockStyle.Right;
            soaStrip.Width = Dpi(620);
            soaStrip.Height = Dpi(40);
            pnlFormHeaderBar.Controls.Add(soaStrip); // added last: docks first, the title fills what is left
            pnlFormHeaderBar.Height = Dpi(56);

            BuildSoaWizard(step1, step2, step3);
            pnlFormHeaderBar.ResumeLayout(true); pnlFormBody.ResumeLayout(true); pnlForm.ResumeLayout(true);
            CenterFormContent();
        }

        private Control? pnlDatesHostCache;
        /// <summary>The panel that holds the two period date pickers (found through the picker, so no field has to be renamed).</summary>
        private Control pnlDatesHost() => pnlDatesHostCache ??= dtpPeriodFrom.Parent ?? dtpPeriodFrom;

        /// <summary>Moves the field cells of a 2-column grid into <paramref name="columns"/> columns; cells for which <paramref name="wide"/> is true span two columns.</summary>
        private void ReflowFieldGrid(TableLayoutPanel grid, int columns, Func<Control, bool> wide)
        {
            var cells = grid.Controls.Cast<Control>().OrderBy(c => grid.GetRow(c)).ThenBy(c => grid.GetColumn(c)).ToList();
            grid.SuspendLayout();
            grid.ColumnStyles.Clear(); grid.RowStyles.Clear();
            for (int i = 0; i < columns; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            grid.ColumnCount = columns;
            int col = 0, row = 0;
            foreach (var cell in cells)
            {
                int span = wide(cell) ? 2 : 1;
                if (col + span > columns) { col = 0; row++; }
                grid.SetCellPosition(cell, new TableLayoutPanelCellPosition(col, row));
                grid.SetColumnSpan(cell, span);
                cell.Margin = new Padding(0, 0, col + span < columns ? Dpi(16) : 0, 0);
                col += span;
                if (col >= columns) { col = 0; row++; }
            }
            int rows = col == 0 ? row : row + 1;
            grid.RowCount = Math.Max(1, rows);
            for (int r = 0; r < grid.RowCount; r++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(66)));
            grid.Height = grid.RowCount * Dpi(66);
            grid.ResumeLayout(true);
        }

        /// <summary>Feeds the totals tape from the (read-only) total boxes.</summary>
        private void UpdateSoaTape()
        {
            if (soaTape == null) return;
            int invoices = gridPicker == null ? 0 : 0;
            var rows = new (string, string)[]
            {
                ("Total Sales + Fuel surcharge (VAT Inclusive)", numTotalSales.Value.ToString("N2")), ("Less: VAT (12%)", numVat.Value.ToString("N2")),
                ("Amount: Net of VAT", numNetOfVat.Value.ToString("N2")),
                ("Less: Withholding Tax (2%)", numWithholdingTax.Value.ToString("N2"))
            };
            if (SoaShowsOldRule())   // saved before Nov 12: the same seven lines as the print, then the note
            {
                string F(decimal v) => v.ToString("N2");
                var ol = SoaSheetTotalsPanel.OldLabels;
                rows = new (string, string)[]
                {
                    (ol[0], F(numTotalSales.Value)), (ol[1], F(numVat.Value)), (ol[2], F(numNetOfVat.Value)), (ol[3], F(numFuelSurchargeTotal.Value)),
                    (ol[4], F(numVat.Value)), (ol[5], F(numWithholdingTax.Value)), (SoaSheetTotalsPanel.OldRuleNote, "")
                };
            }
            int lines = gridItems.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
            _ = invoices;
            soaTape.Set("The statement adds up to · from the service invoices", rows, "Total amount due", "₱" + numTotalAmountDue.Value.ToString("N2"),
                lines > 0 ? $"{lines} trip line{(lines == 1 ? "" : "s")} · collected with ONE receipt" : "no trip lines yet");
        }
    }
}
