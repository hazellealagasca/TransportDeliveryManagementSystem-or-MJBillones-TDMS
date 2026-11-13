using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The statement sheet's totals block, drawn under the trips table exactly like the printed statement: labels on the left,
    /// the values under the Amount column, the fuel surcharge total beside the first line (it is already inside Total Sales). It follows the table's columns and scrolling.
    /// </summary>
    public sealed class SoaSheetTotalsPanel : Control
    {
        private readonly DataGridView grid;
        private readonly Func<decimal[]> values;   // total sales (fuel included), VAT, net of VAT, withholding, total due, then the fuel total (beside the first line)
        private static readonly string[] NewLabels = { "Total Sales + Fuel surcharge (VAT Inclusive)", "Less: VAT (12%)", "Amount: Net of VAT", "Less: Withholding Tax (2%)", "TOTAL AMOUNT DUE" };
        /// <summary>The lines of a statement saved before Nov 12 (fuel outside the VAT), as the print shows them.</summary>
        internal static readonly string[] OldLabels = { "Total Sales (VAT Inclusive)", "Less: VAT", "Amount: Net of VAT", "Fuel surcharge", "Add: VAT", "Less: Withholding Tax", "TOTAL AMOUNT DUE" };
        internal const string OldRuleNote = "Saved before Nov 12 with the older computation (fuel outside VAT). Its amounts stay as saved.";
        private readonly Func<bool>? oldRule;   // true: the statement was saved under the old rule, show its seven old lines + a note
        private bool IsOld => oldRule?.Invoke() ?? false;
        private string[] Labels => IsOld ? OldLabels : NewLabels;

        public int LineHeight => (int)Math.Round(26 * DeviceDpi / 96f);
        public int TotalHeight => LineHeight * Labels.Length + 2 + (IsOld ? LineHeight : 0);
        /// <summary>Re-reads the rule and fits the panel to its lines.</summary>
        public void Relayout() { int h = TotalHeight; if (Height != h) { Height = h; AfterResize?.Invoke(); } Invalidate(); }

        public SoaSheetTotalsPanel(DataGridView grid, Func<decimal[]> values, Func<bool>? oldRule = null)
        {
            this.grid = grid; this.values = values; this.oldRule = oldRule;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Dock = DockStyle.Bottom;
            Height = TotalHeight;
            BackColor = AppTheme.Surface;
            grid.Scroll += (s, e) => Invalidate();
            grid.ColumnWidthChanged += (s, e) => Invalidate();
            grid.SizeChanged += (s, e) => Invalidate();
            grid.ColumnDisplayIndexChanged += (s, e) => Invalidate();
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action? AfterResize { get; set; }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = TotalHeight; AfterResize?.Invoke(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Height = TotalHeight; AfterResize?.Invoke(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            if (!grid.Columns.Contains("colAmount") || !grid.Columns.Contains("colFuel")) return;
            var amt = grid.GetColumnDisplayRectangle(grid.Columns["colAmount"]!.Index, true);
            var fuel = grid.GetColumnDisplayRectangle(grid.Columns["colFuel"]!.Index, true);
            if (amt.Width <= 0) return;
            decimal[] v0 = values();
            bool old = IsOld;
            string[] lines = old ? OldLabels : NewLabels;
            decimal[] v = old && v0.Length > 5 ? new[] { v0[0], v0[1], v0[2], v0[5], v0[1], v0[3], v0[4] } : v0;   // values arrive as sales, VAT, net, withholding, due, fuel
            using var line = new Pen(grid.GridColor);
            using var normal = new Font("Segoe UI", 8.75F);
            using var strong = new Font("Segoe UI Semibold", 8.75F);
            int h = Math.Max(Math.Min(LineHeight, (Height - 2) / (lines.Length + (old ? 1 : 0))), 16);   // the lines always fit the panel (it may have been sized before the screen scaling was known)
            for (int i = 0; i < lines.Length; i++)
            {
                bool last = i == lines.Length - 1;
                int y = i * h;
                var labelCell = new Rectangle(0, y, amt.Left, h);
                var valCell = new Rectangle(amt.Left, y, amt.Width, h);
                g.DrawRectangle(line, labelCell.X, labelCell.Y, labelCell.Width - 1, labelCell.Height);
                g.DrawRectangle(line, valCell.X, valCell.Y, valCell.Width - 1, valCell.Height);
                var f = last ? strong : normal;
                TextRenderer.DrawText(g, lines[i], f, Rectangle.Inflate(labelCell, -8, 0), AppTheme.TextPrimary, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                string val = i < v.Length ? v[i].ToString("N2", CultureInfo.InvariantCulture) : "";
                TextRenderer.DrawText(g, val, f, Rectangle.Inflate(valCell, -8, 0), AppTheme.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (i == 0 && fuel.Width > 0)   // the fuel total sits beside the first line (as on the print)
                {
                    var fc = new Rectangle(fuel.Left, y, fuel.Width, h);
                    g.DrawRectangle(line, fc.X, fc.Y, fc.Width - 1, fc.Height);
                    TextRenderer.DrawText(g, v0.Length > 5 ? v0[5].ToString("N2", CultureInfo.InvariantCulture) : "", normal, Rectangle.Inflate(fc, -8, 0), AppTheme.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }
            if (old)
            {
                using var small = new Font("Segoe UI", 8.25F, FontStyle.Italic);
                TextRenderer.DrawText(g, OldRuleNote, small, new Rectangle(0, lines.Length * h, Width, h), AppTheme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
