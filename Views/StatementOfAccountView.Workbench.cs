using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Ledger Desk layout of the Statement of Account form (60 / 40). Layout only: the section cards, every field and every handler are the same
    /// controls, re-homed. Left: the client and statement details, then a two-tab area (② ready service invoices │ ③ trips and stores) so the page
    /// never scrolls. Right: the live totals tape with the burgundy total slab, the sign-off, and the open problems pinned at the bottom.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private Panel? soaCardClient, soaCardPicker, soaCardLines, soaCardTotals, soaCardSign;
        private TapeCard? soaTape;
        private bool soaWorkbench;
        private Button? btnTabInvoices, btnTabLines;

        private void ApplySoaWorkbench()
        {
            if (soaWorkbench || soaCardClient == null || soaCardPicker == null || soaCardLines == null || soaCardTotals == null || soaCardSign == null) return;
            soaWorkbench = true;
            pnlForm.SuspendLayout(); pnlFormBody.SuspendLayout(); pnlFormHeaderBar.SuspendLayout();
            foreach (Control? h in new Control?[] { soaStepper, soaHelp }) if (h != null) { h.Visible = false; h.Height = 0; }
            soaChecklist.ShowOnlyProblems = true;
            soaChecklist.FocusResolver = c =>
            {
                string t = c.Text;
                if (t.Contains("adjustment reason", StringComparison.OrdinalIgnoreCase) || t.Contains("changed by hand", StringComparison.OrdinalIgnoreCase)) return txtSoaAdjustReason;
                if (t.Contains("client", StringComparison.OrdinalIgnoreCase)) return cbClient;
                if (t.Contains("period", StringComparison.OrdinalIgnoreCase)) return dtpPeriodFrom;
                if (t.Contains("statement date", StringComparison.OrdinalIgnoreCase)) return dtpIssueDate;
                return gridItems; // everything else is about the trip lines or their invoices
            };

            // ---- the client card: 7 fields in 4 columns instead of 2 (saves a third of its height); the period dates take two columns
            lblPopulateHint.Visible = false;
            var fieldGrid = soaCardClient.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (fieldGrid != null) ReflowFieldGrid(fieldGrid, 4, c => c.Controls.Cast<Control>().Any(x => x == pnlDatesHost()));

            // ---- the two tab cards: the tab already says what they are, so their own headers (title + subtitle) are not shown and the padding is tighter
            foreach (var card in new[] { soaCardPicker, soaCardLines })
            {
                int n = card.Controls.Count;
                if (n >= 2) { card.Controls[n - 1].Visible = false; card.Controls[n - 2].Visible = false; } // the painted header and the 10 px spacer under it
                card.Padding = new Padding(Dpi(16), Dpi(10), Dpi(16), Dpi(12));
            }

            // ---- detach the stacked cards, then rebuild as two panes
            var keep = pnlFormBody.Controls.Cast<Control>().Where(c => c is TextBox).ToList(); // the hidden SOA number box
            foreach (Control c in pnlFormBody.Controls.Cast<Control>().ToList()) pnlFormBody.Controls.Remove(c);

            // ONE page, ONE scroll bar (Nov 9): the whole form scrolls together. Three rows that grow with their content:
            //   row 1: client card | live totals     row 2 (full width): the two tabs and the table     row 3: sign-off & notes | what needs attention
            Panel Grow() => new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            Panel Gap() => new Panel { Dock = DockStyle.Top, Height = Dpi(14), BackColor = AppTheme.Canvas };

            var tabs = new Panel { Dock = DockStyle.Top, Height = Dpi(48), BackColor = AppTheme.Canvas, Padding = new Padding(0, Dpi(6), 0, Dpi(6)) };
            btnTabInvoices = MakeFormButton("②  Ready service invoices", "primary", Dpi(250));
            btnTabLines = MakeFormButton("③  Trips and stores on this statement", "secondary", Dpi(310));
            btnTabInvoices.Dock = DockStyle.Left; btnTabLines.Dock = DockStyle.Left;
            btnTabInvoices.Click += (s, e) => SoaShowTab(true);
            btnTabLines.Click += (s, e) => SoaShowTab(false);
            tabs.Controls.Add(btnTabLines);
            tabs.Controls.Add(new Panel { Dock = DockStyle.Left, Width = Dpi(8) });
            tabs.Controls.Add(btnTabInvoices);
            soaCardLines.Dock = DockStyle.Top; soaCardPicker.Dock = DockStyle.Top; soaCardClient.Dock = DockStyle.Top;

            var topLeft = Grow(); topLeft.Margin = new Padding(0, 0, Dpi(8), 0); topLeft.Controls.Add(soaCardClient);
            soaTape = new TapeCard { EmptyText = "Pick the ready service invoices to see the statement add up.", Dock = DockStyle.Top };
            soaTape.Set("The statement adds up to", Array.Empty<(string, string)>(), "Total amount due", "₱0.00", "");
            var topRight = Grow(); topRight.Margin = new Padding(Dpi(8), 0, 0, 0); topRight.Controls.Add(soaTape);

            var middle = Grow();
            middle.Controls.Add(soaCardLines);
            middle.Controls.Add(soaCardPicker);
            middle.Controls.Add(tabs);
            middle.Controls.Add(Gap());

            soaCardSign.Dock = DockStyle.Top; pnlSoaAdjust.Dock = DockStyle.Top;
            BuildSoaHistoryCard(); // shown only when a statement that can't be edited is opened (Nov 8)
            var bottomLeft = Grow(); bottomLeft.Margin = new Padding(0, 0, Dpi(8), 0);
            bottomLeft.Controls.Add(soaCardSign);
            bottomLeft.Controls.Add(Gap());
            bottomLeft.Controls.Add(pnlSoaAdjust);
            bottomLeft.Controls.Add(Gap());
            bottomLeft.Controls.Add(soaHistoryCard!);
            var bottomRight = Grow(); bottomRight.Margin = new Padding(Dpi(8), 0, 0, 0);
            soaChecklist.Dock = DockStyle.Top;
            bottomRight.Controls.Add(soaChecklist);
            var left = topLeft; var right = topRight; var rightScroll = bottomRight;

            var split = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 3, BackColor = AppTheme.Canvas, Padding = new Padding(Dpi(24), Dpi(16), Dpi(24), Dpi(16)) };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            for (int i = 0; i < 3; i++) split.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            split.Controls.Add(topLeft, 0, 0); split.Controls.Add(topRight, 1, 0);
            split.Controls.Add(middle, 0, 1); split.SetColumnSpan(middle, 2);
            split.Controls.Add(bottomLeft, 0, 2); split.Controls.Add(bottomRight, 1, 2);
            pnlFormBody.AutoScroll = true;
            pnlFormBody.Controls.Add(split);
            // the old totals card keeps its boxes (they are read-only and still feed the tape), but is not shown
            soaCardTotals.Visible = false;
            pnlFormBody.Controls.Add(soaCardTotals);
            foreach (var k in keep) pnlFormBody.Controls.Add(k);
            Smooth.Enable(split); Smooth.Enable(left); Smooth.Enable(right); Smooth.Enable(rightScroll); Smooth.Enable(pnlFormBody);

            // one slim header: Back + title + the live figures
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

            btnAddSelected.Click += (s, e) => { if (gridItems.Rows.Count > 0) SoaShowTab(false); };
            SoaShowTab(true);
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

        /// <summary>Shows the ready-invoices card (true) or the trips-and-stores card (false) in the left pane.</summary>
        private void SoaShowTab(bool invoices)
        {
            if (!soaWorkbench || soaCardPicker == null || soaCardLines == null) return;
            pnlFormBody.SuspendLayout();
            soaCardPicker.Visible = invoices;
            soaCardLines.Visible = !invoices;
            if (btnTabInvoices != null) UIThemeHelper.SetButtonRole(btnTabInvoices, invoices ? "primary" : "secondary");
            if (btnTabLines != null) UIThemeHelper.SetButtonRole(btnTabLines, invoices ? "secondary" : "primary");
            pnlFormBody.ResumeLayout(true);
        }

        /// <summary>Feeds the totals tape from the (read-only) total boxes.</summary>
        private void UpdateSoaTape()
        {
            if (soaTape == null) return;
            int invoices = gridPicker == null ? 0 : 0;
            var rows = new (string, string)[]
            {
                ("Total Sales (VAT Inclusive)", numTotalSales.Value.ToString("N2")), ("Less: VAT (12%)", numVat.Value.ToString("N2")),
                ("Amount: Net of VAT", numNetOfVat.Value.ToString("N2")), ("Fuel surcharge (added, no VAT)", numFuelSurchargeTotal.Value.ToString("N2")),
                ("Less: Withholding Tax (2%)", numWithholdingTax.Value.ToString("N2"))
            };
            int lines = gridItems.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
            _ = invoices;
            soaTape.Set("The statement adds up to · from the service invoices", rows, "Total amount due", "₱" + numTotalAmountDue.Value.ToString("N2"),
                lines > 0 ? $"{lines} trip line{(lines == 1 ? "" : "s")} · collected with ONE receipt" : "no trip lines yet");
        }
    }
}
