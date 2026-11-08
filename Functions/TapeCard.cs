using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The "adding-machine tape" of the Ledger Desk: label ········ figure rows with dotted leaders, and one solid brand-colour slab with the
    /// amount that matters. Read-only and painted (no input boxes), so it can never be typed over or scrolled into a different number.
    /// </summary>
    public sealed class TapeCard : Control, IZoomAware
    {
        private string title = "";
        private List<(string Label, string Value)> rows = new();
        private string slabLabel = "", slabValue = "", slabNote = "";
        /// <summary>Shown when there are no rows yet.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)]
        public string EmptyText { get; set; } = "Pick a statement or invoice to see what it adds up to.";
        public TapeCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Dock = DockStyle.Top;
            TabStop = false;
            Height = D(120);
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f * UiZoomFactor);
        private static float UiZoomFactor => UiZoom.Factor;

        /// <summary>Sets what the card shows; pass no rows for the empty state.</summary>
        public void Set(string heading, IEnumerable<(string Label, string Value)> lines, string slabCaption, string slabAmount, string slabSub)
        {
            title = heading; rows = lines.ToList(); slabLabel = slabCaption; slabValue = slabAmount; slabNote = slabSub;
            int want = D(46) + Math.Max(1, rows.Count) * D(30) + D(12) + (slabValue.Length > 0 ? D(98) : 0) + D(14);
            if (Height != want) Height = want;
            Invalidate();
        }

        public void ZoomChanged() => Set(title, rows, slabLabel, slabValue, slabNote);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(AppTheme.Surface);
            using (var pen = new Pen(AppTheme.Border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            const TextFormatFlags f = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            int pad = D(24);
            using var cap = new Font("Segoe UI Semibold", 8.75F * UiZoomFactor);
            TextRenderer.DrawText(g, title.ToUpperInvariant(), cap, new Rectangle(pad, D(14), Width - pad * 2, D(22)), AppTheme.TextMuted, f);
            using (var pen = new Pen(AppTheme.Border)) g.DrawLine(pen, pad, D(42), Width - pad, D(42));
            int y = D(48);
            using var lab = new Font("Segoe UI", 10F * UiZoomFactor);
            using var val = new Font("Segoe UI Semibold", 10.5F * UiZoomFactor);
            if (rows.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, lab, new Rectangle(pad, y, Width - pad * 2, D(30)), AppTheme.TextMuted, f);
                y += D(30);
            }
            foreach (var (l, v) in rows)
            {
                var lsz = TextRenderer.MeasureText(g, l, lab, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                var vsz = TextRenderer.MeasureText(g, v, val, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                int vw = Math.Min(vsz.Width + D(4), Width - pad * 2 - lsz.Width - D(24));
                TextRenderer.DrawText(g, l, lab, new Rectangle(pad, y, lsz.Width + D(4), D(30)), AppTheme.TextSecondary, f);
                TextRenderer.DrawText(g, v, val, new Rectangle(Width - pad - vw, y, vw, D(30)), AppTheme.TextPrimary, f | TextFormatFlags.Right);
                int x1 = pad + lsz.Width + D(10), x2 = Width - pad - vw - D(8);
                if (x2 > x1)
                    using (var dot = new Pen(AppTheme.BorderStrong, 1.5f) { DashStyle = DashStyle.Dot }) g.DrawLine(dot, x1, y + D(22), x2, y + D(22));
                y += D(30);
            }
            if (slabValue.Length == 0) return;
            y += D(12);
            var slab = new Rectangle(pad, y, Width - pad * 2, D(84));
            using (var path = RoundRect(slab, D(12)))
            using (var br = new LinearGradientBrush(slab, AppTheme.AccentPressed, AppTheme.Accent, 35f))
                g.FillPath(br, path);
            Color ink = AppTheme.OnAccent;
            using var sc = new Font("Segoe UI Semibold", 8.25F * UiZoomFactor);
            using var sv = new Font("Segoe UI Semibold", 24F * UiZoomFactor);
            using var sn = new Font("Segoe UI", 9F * UiZoomFactor);
            TextRenderer.DrawText(g, slabLabel.ToUpperInvariant(), sc, new Rectangle(slab.Left + D(18), slab.Top + D(10), slab.Width - D(36), D(18)), ink, f);
            TextRenderer.DrawText(g, slabValue, sv, new Rectangle(slab.Left + D(18), slab.Top + D(26), slab.Width - D(36), D(38)), ink, f | TextFormatFlags.Right);
            TextRenderer.DrawText(g, slabNote, sn, new Rectangle(slab.Left + D(18), slab.Top + D(62), slab.Width - D(36), D(18)), ink, f | TextFormatFlags.Right);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
