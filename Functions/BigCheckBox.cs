using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// A confirmation check box that fills its own bordered panel: a large box with a bold tick and wrapping text.
    /// Amber when it still needs ticking, green when ticked, grey when disabled. Clicking anywhere on it toggles it (Space works too).
    /// </summary>
    public sealed class BigCheckBox : CheckBox
    {
        private readonly Font ownFont = new Font("Segoe UI Semibold", 10.5F);

        public BigCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            UseMnemonic = false;
            Font = ownFont;
            Cursor = Cursors.Hand;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ownFont.Dispose();
            base.Dispose(disposing);
        }

        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private const TextFormatFlags TextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

        private int TextLeft => S(16) + S(24) + S(14);

        /// <summary>The height the control needs for its text at the given width (at least 60 px, DPI-scaled).</summary>
        public int MeasureHeight(int width)
        {
            int tw = Math.Max(S(120), width - TextLeft - S(16));
            var sz = TextRenderer.MeasureText(Text ?? "", Font, new Size(tw, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            return Math.Max(S(60), sz.Height + S(24));
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            base.OnEnabledChanged(e);
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Color back, border, text, boxFill, boxBorder;
            if (!Enabled) { back = AppTheme.SurfaceAlt; border = AppTheme.Border; text = AppTheme.TextMuted; boxFill = AppTheme.SurfaceHover; boxBorder = AppTheme.BorderStrong; }
            else if (Checked) { back = AppTheme.SuccessSoft; border = AppTheme.Success; text = AppTheme.SuccessText; boxFill = AppTheme.Success; boxBorder = AppTheme.Success; }
            else { back = AppTheme.WarningSoft; border = AppTheme.Warning; text = AppTheme.TextPrimary; boxFill = AppTheme.Surface; boxBorder = AppTheme.Warning; }

            g.Clear(Parent?.BackColor ?? AppTheme.Canvas);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = RoundRect(r, S(8)))
            {
                using (var b = new SolidBrush(back)) g.FillPath(b, path);
                using (var p = new Pen(border, Math.Max(1.5f, S(2)))) g.DrawPath(p, path);
            }

            int bs = S(24);
            var box = new Rectangle(S(16), (Height - bs) / 2, bs, bs);
            using (var path = RoundRect(box, S(5)))
            {
                using (var b = new SolidBrush(boxFill)) g.FillPath(b, path);
                using (var p = new Pen(boxBorder, Math.Max(2f, S(2)))) g.DrawPath(p, path);
            }
            if (Checked)
            {
                using var tick = new Pen(Enabled ? Color.White : AppTheme.TextMuted, Math.Max(3f, S(3))) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(tick, new[]
                {
                    new PointF(box.X + bs * 0.24f, box.Y + bs * 0.52f),
                    new PointF(box.X + bs * 0.43f, box.Y + bs * 0.72f),
                    new PointF(box.X + bs * 0.78f, box.Y + bs * 0.28f)
                });
            }
            if (Focused && ShowFocusCues)
            {
                var ring = Rectangle.Inflate(box, S(4), S(4));
                using var path = RoundRect(ring, S(8));
                using var p = new Pen(AppTheme.FocusRing, Math.Max(2f, S(2)));
                g.DrawPath(p, path);
            }
            g.SmoothingMode = SmoothingMode.Default;

            int tl = TextLeft;
            TextRenderer.DrawText(g, Text ?? "", Font, new Rectangle(tl, S(8), Math.Max(S(120), Width - tl - S(16)), Math.Max(1, Height - S(16))), text, TextFlags);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
