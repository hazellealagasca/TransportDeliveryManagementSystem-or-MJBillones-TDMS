using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The statement sheet's totals block, drawn under the trips table exactly like the printed statement: labels on the left,
    /// the values under the Amount column, the fuel surcharge total beside the first line. It follows the table's columns and scrolling.
    /// </summary>
    internal sealed class SoaSheetTotalsPanel : Control
    {
        private readonly DataGridView grid;
        private readonly Func<decimal[]> values;   // total sales, VAT, net of VAT, fuel, VAT, withholding, total due
        private static readonly string[] Labels = { "Total Sales (VAT Inclusive)", "Less: VAT", "Amount: Net of VAT", "Fuel surcharge", "Add: VAT", "Less: Withholding Tax", "TOTAL AMOUNT DUE" };

        public int LineHeight => (int)Math.Round(26 * DeviceDpi / 96f);
        public int TotalHeight => LineHeight * Labels.Length + 2;

        public SoaSheetTotalsPanel(DataGridView grid, Func<decimal[]> values)
        {
            this.grid = grid; this.values = values;
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
            decimal[] v = values();
            using var line = new Pen(grid.GridColor);
            using var normal = new Font("Segoe UI", 8.75F);
            using var strong = new Font("Segoe UI Semibold", 8.75F);
            int h = Math.Max(Math.Min(LineHeight, (Height - 2) / Labels.Length), 16);   // the lines always fit the panel (it may have been sized before the screen scaling was known)
            for (int i = 0; i < Labels.Length; i++)
            {
                bool last = i == Labels.Length - 1;
                int y = i * h;
                var labelCell = new Rectangle(0, y, amt.Left, h);
                var valCell = new Rectangle(amt.Left, y, amt.Width, h);
                g.DrawRectangle(line, labelCell.X, labelCell.Y, labelCell.Width - 1, labelCell.Height);
                g.DrawRectangle(line, valCell.X, valCell.Y, valCell.Width - 1, valCell.Height);
                var f = last ? strong : normal;
                TextRenderer.DrawText(g, Labels[i], f, Rectangle.Inflate(labelCell, -8, 0), AppTheme.TextPrimary, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                string val = i < v.Length ? v[i].ToString("N2", CultureInfo.InvariantCulture) : "";
                TextRenderer.DrawText(g, val, f, Rectangle.Inflate(valCell, -8, 0), AppTheme.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (i == 0 && fuel.Width > 0)
                {
                    var fc = new Rectangle(fuel.Left, y, fuel.Width, h);
                    g.DrawRectangle(line, fc.X, fc.Y, fc.Width - 1, fc.Height);
                    TextRenderer.DrawText(g, v.Length > 3 ? v[3].ToString("N2", CultureInfo.InvariantCulture) : "", normal, Rectangle.Inflate(fc, -8, 0), AppTheme.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }
        }
    }
}
