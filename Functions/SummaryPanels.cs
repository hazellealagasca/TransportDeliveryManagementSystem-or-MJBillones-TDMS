using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Colour of a summary card.</summary>
    public enum SummaryTone { Blue, Green, Amber, Red, Purple, Teal, Neutral }



    /// <summary>One figure on a summary card.</summary>
    /// <param name="Strong">Fill the card with its colour (white text), e.g. a total, or a warning that is above zero.</param>
    public sealed record SummaryMetric(string Title, string Value, SummaryTone Tone, string Note = "", bool Strong = false);



    /// <summary>Large-print, high-contrast colours used by the summary cards (the same in light and dark themes).</summary>
    public static class SummaryColors
    {
        public static Color Of(SummaryTone t) => t switch
        {
            SummaryTone.Blue => Color.FromArgb(30, 58, 138),     // deep blue
            SummaryTone.Green => Color.FromArgb(4, 120, 87),     // emerald
            SummaryTone.Amber => Color.FromArgb(180, 83, 9),     // amber / orange
            SummaryTone.Red => Color.FromArgb(185, 28, 28),      // crimson
            SummaryTone.Purple => Color.FromArgb(109, 40, 217),
            SummaryTone.Teal => Color.FromArgb(15, 118, 110),
            _ => Color.FromArgb(51, 65, 85)
        };
        /// <summary>Tinted card background when the card is not filled.</summary>
        public static Color Soft(SummaryTone t) => ThemeManager.Mix(AppTheme.Surface, Of(t), AppTheme.IsDark ? 0.30 : 0.10);
        /// <summary>Title colour on a tinted card (lighter in the dark theme so it stays readable).</summary>
        public static Color Ink(SummaryTone t) => AppTheme.IsDark ? ThemeManager.Mix(Of(t), Color.White, 0.55) : Of(t);
    }



    /// <summary>
    /// A big summary card: bold title (13 pt), large value (22 pt) and a short note, with a thick coloured border.
    /// "Strong" cards are filled with their colour and use white text.
    /// </summary>
    public sealed class BigStatCard : Control
    {
        private SummaryMetric metric = new SummaryMetric("", "", SummaryTone.Neutral);
        public BigStatCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            Margin = new Padding(0);
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public SummaryMetric Metric { get => metric; set { metric = value ?? new SummaryMetric("", "", SummaryTone.Neutral); Invalidate(); AccessibleName = $"{metric.Title}: {metric.Value}"; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var m = metric;
            Color tone = SummaryColors.Of(m.Tone);
            Color back = m.Strong ? tone : SummaryColors.Soft(m.Tone);
            Color title = m.Strong ? Color.White : SummaryColors.Ink(m.Tone);
            Color value = m.Strong ? Color.White : AppTheme.TextPrimary;
            Color note = m.Strong ? Color.FromArgb(235, 255, 255, 255) : AppTheme.TextSecondary;
            using (var b = new SolidBrush(AppTheme.Surface)) g.FillRectangle(b, ClientRectangle);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
            int bw = D(m.Strong ? 2 : 3);
            using (var p = new Pen(m.Strong ? ThemeManager.Mix(tone, Color.Black, 0.25) : tone, bw) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset })
                g.DrawRectangle(p, r);
            int pad = D(14);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            using var tf = new Font("Segoe UI", 13F, FontStyle.Bold);
            using var vf = new Font("Segoe UI", 22F, FontStyle.Bold);
            using var nf = new Font("Segoe UI Semibold", 10.5F);
            int y = D(10);
            int th = TextRenderer.MeasureText("Ag", tf).Height;
            TextRenderer.DrawText(g, m.Title.ToUpperInvariant(), tf, new Rectangle(pad, y, Width - pad * 2, th), title, flags);
            y += th;
            int vh = TextRenderer.MeasureText("0", vf).Height;
            TextRenderer.DrawText(g, m.Value, vf, new Rectangle(pad, y, Width - pad * 2, vh), value, flags);
            y += vh;
            if (!string.IsNullOrWhiteSpace(m.Note))
                TextRenderer.DrawText(g, m.Note, nf, new Rectangle(pad, y, Width - pad * 2, Math.Max(0, Height - y - D(6))), note, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
        /// <summary>Height the card needs at this width.</summary>
        public int PreferredCardHeight()
        {
            using var tf = new Font("Segoe UI", 13F, FontStyle.Bold);
            using var vf = new Font("Segoe UI", 22F, FontStyle.Bold);
            using var nf = new Font("Segoe UI Semibold", 10.5F);
            int h = D(10) + TextRenderer.MeasureText("Ag", tf).Height + TextRenderer.MeasureText("0", vf).Height + D(10);
            if (!string.IsNullOrWhiteSpace(metric.Note))
                h += TextRenderer.MeasureText(metric.Note, nf, new Size(Math.Max(D(60), Width - D(28)), 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
            return h;
        }
    }



    /// <summary>
    /// A grid of <see cref="BigStatCard"/>s that wraps to more rows on narrow windows and sizes its own height.
    /// </summary>
    public sealed class StatCardGrid : Panel
    {
        private readonly List<BigStatCard> cards = new List<BigStatCard>();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        /// <summary>Narrowest a card may get before wrapping.</summary>
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int MinCardWidth { get; set; }
        public StatCardGrid() { BackColor = AppTheme.Surface; MinCardWidth = 0; DoubleBuffered = true; }
        public void SetMetrics(IReadOnlyList<SummaryMetric> metrics)
        {
            SuspendLayout();
            while (cards.Count < metrics.Count) { var c = new BigStatCard(); cards.Add(c); Controls.Add(c); }
            while (cards.Count > metrics.Count) { var c = cards[^1]; cards.RemoveAt(cards.Count - 1); Controls.Remove(c); c.Dispose(); }
            for (int i = 0; i < metrics.Count; i++) cards[i].Metric = metrics[i];
            ResumeLayout(false);
            Arrange();
        }
        protected override void OnResize(EventArgs eventargs) { base.OnResize(eventargs); Arrange(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Arrange(); }
        private bool arranging;
        private void Arrange()
        {
            if (arranging || cards.Count == 0) { if (cards.Count == 0 && Height != 0) Height = 0; return; }
            arranging = true;
            try
            {
                int gap = D(12), w = Math.Max(D(120), ClientSize.Width);
                int min = MinCardWidth > 0 ? MinCardWidth : D(210);
                int cols = Math.Max(1, Math.Min(cards.Count, (w + gap) / (min + gap)));
                int cw = (w - gap * (cols - 1)) / cols;
                int rows = (cards.Count + cols - 1) / cols;
                // Same height for every card in the grid (the tallest one).
                foreach (var c in cards) c.Width = cw;
                int ch = Math.Max(D(96), cards.Max(c => c.PreferredCardHeight()));
                for (int i = 0; i < cards.Count; i++)
                {
                    int r = i / cols, col = i % cols;
                    cards[i].Bounds = new Rectangle(col * (cw + gap), r * (ch + gap), cw, ch);
                }
                int want = rows * ch + (rows - 1) * gap;
                if (Height != want) Height = want;
            }
            finally { arranging = false; }
        }
    }



    /// <summary>Figures for the Trip &amp; Crate Summary Overview under a trip report's store table.</summary>
    public sealed class TripCrateFigures
    {
        public int TotalCrates, Missing, Damaged, Stores, Drops, ReturnedTotal;
        public List<string> Routes = new List<string>();
        public List<string> StoreIssues = new List<string>();
        public List<string> OverReturns = new List<string>();
        public string DamagedNote = "";
        public int Good => Math.Max(0, TotalCrates - Missing - Damaged);
        public int Unaccounted => Missing + Damaged;
    }



    /// <summary>
    /// Trip &amp; Crate Summary Overview (large print, high contrast): Total crates (blue), Intact / good (green),
    /// Missing (amber, thick border when above 0), Damaged (crimson, white text when above 0), then the stores and
    /// drops, routes covered, the stores with problems and whether every crate is accounted for.
    /// </summary>
    public sealed class TripCrateSummaryPanel : Panel
    {
        private readonly Label heading = new Label();
        private readonly StatCardGrid cards = new StatCardGrid();
        private readonly Label details = new Label();
        private readonly Label status = new Label();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public TripCrateSummaryPanel()
        {
            BackColor = AppTheme.Surface;
            Padding = new Padding(0, D(4), 0, D(4));
            heading.Text = "📊  Trip & Crate Summary Overview";
            heading.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            heading.ForeColor = AppTheme.TextPrimary;
            heading.AutoSize = false;
            heading.UseMnemonic = false;
            heading.TextAlign = ContentAlignment.MiddleLeft;
            details.Font = new Font("Segoe UI", 12F);
            details.ForeColor = AppTheme.TextPrimary;
            details.UseMnemonic = false;
            details.AutoSize = false;
            status.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            status.UseMnemonic = false;
            status.AutoSize = false;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.Padding = new Padding(D(12), 0, D(12), 0);
            Controls.Add(heading);
            Controls.Add(cards);
            Controls.Add(status);
            Controls.Add(details);
            cards.Resize += (s, e) => Arrange();
            ShowFigures(new TripCrateFigures());
        }



        private TripCrateFigures last = new TripCrateFigures();
        public void ShowFigures(TripCrateFigures f)
        {
            last = f ?? new TripCrateFigures();
            f = last;
            cards.SetMetrics(new[]
            {
                new SummaryMetric("Total crates", f.TotalCrates.ToString("N0"), SummaryTone.Blue, "delivered on this trip", Strong: true),
                new SummaryMetric("Intact / good crates", f.Good.ToString("N0"), SummaryTone.Green, "total − missing − damaged", Strong: true),
                new SummaryMetric("Missing crates", f.Missing.ToString("N0"), SummaryTone.Amber, f.Missing > 0 ? "not returned by the stores" : "none missing", Strong: f.Missing > 0),
                new SummaryMetric("Damaged crates", f.Damaged.ToString("N0"), SummaryTone.Red, f.Damaged > 0 ? "from incident reports" : "none reported", Strong: f.Damaged > 0),
            });
            var lines = new List<string>
            {
                $"Total stores / drop-off stops:  {f.Stores:N0} store{(f.Stores == 1 ? "" : "s")}" + (f.Drops > 0 ? $"  ·  {f.Drops:N0} drop{(f.Drops == 1 ? "" : "s")}" : ""),
                "Covered routes:  " + (f.Routes.Count > 0 ? string.Join(", ", f.Routes) : "—"),
                $"Crates returned by the stores:  {f.ReturnedTotal:N0} of {f.TotalCrates:N0}",
            };
            if (f.StoreIssues.Count > 0 || f.Damaged > 0 || f.OverReturns.Count > 0)
            {
                lines.Add("Store-by-store issues:");
                lines.AddRange(f.StoreIssues.Select(s => "   • " + s));
                if (f.Damaged > 0) lines.Add("   • " + (f.DamagedNote.Length > 0 ? f.DamagedNote : $"Damaged: {f.Damaged:N0} crates (incident reports for the whole trip)"));
                lines.AddRange(f.OverReturns.Select(s => "   • " + s));
            }
            else lines.Add("Store-by-store issues:  none");
            details.Text = string.Join(Environment.NewLine, lines);
            if (f.TotalCrates == 0 && f.Stores == 0)
            {
                status.Text = "Net crate accountability:  pick a dispatch to see its crates";
                status.BackColor = AppTheme.SurfaceAlt; status.ForeColor = AppTheme.TextSecondary;
            }
            else if (f.Unaccounted == 0)
            {
                status.Text = $"✓  Net crate accountability:  all {f.TotalCrates:N0} crates accounted for";
                status.BackColor = SummaryColors.Of(SummaryTone.Green); status.ForeColor = Color.White;
            }
            else
            {
                status.Text = $"⚠  Net crate accountability:  {f.Unaccounted:N0} crate{(f.Unaccounted == 1 ? "" : "s")} not accounted for ({f.Missing:N0} missing, {f.Damaged:N0} damaged)";
                status.BackColor = SummaryColors.Of(f.Damaged > 0 ? SummaryTone.Red : SummaryTone.Amber); status.ForeColor = Color.White;
            }
            Arrange();
        }



        protected override void OnResize(EventArgs eventargs) { base.OnResize(eventargs); Arrange(); }
        private bool arranging;
        private void Arrange()
        {
            if (arranging) return;
            arranging = true;
            try
            {
                int w = Math.Max(D(200), ClientSize.Width - Padding.Horizontal);
                int x = Padding.Left, y = Padding.Top;
                int hh = TextRenderer.MeasureText("Ag", heading.Font).Height + D(10);
                heading.Bounds = new Rectangle(x, y, w, hh); y += hh + D(4);
                cards.Bounds = new Rectangle(x, y, w, cards.Height); y += cards.Height + D(12);
                int sh = TextRenderer.MeasureText(status.Text, status.Font, new Size(w - D(24), 0), TextFormatFlags.WordBreak).Height + D(16);
                status.Bounds = new Rectangle(x, y, w, Math.Max(D(40), sh)); y += status.Height + D(10);
                int dh = TextRenderer.MeasureText(details.Text, details.Font, new Size(w, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + D(6);
                details.Bounds = new Rectangle(x, y, w, dh); y += dh;
                int want = y + Padding.Bottom;
                if (Height != want) Height = want;
            }
            finally { arranging = false; }
        }
    }



    /// <summary>
    /// The "All-in-One Summary" at the end of a screen's table: a full-width bar
    /// ("▼ Show All-in-One Summary Overview" / "▲ Hide …") with a bold one-line preview even when closed, and
    /// large colour-coded cards when open. Whether it is open is remembered per screen and per user.
    /// </summary>
    public sealed class AllInOneSummaryBar : Panel
    {
        private readonly string key;
        private readonly ToggleBar bar;
        private readonly Label lblContext = new Label();
        private readonly StatCardGrid cards = new StatCardGrid();
        private bool expanded;
        private string preview = "";
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public bool Expanded => expanded;



        public AllInOneSummaryBar(string screenKey)
        {
            key = "AllInOneSummary." + screenKey + "." + (SessionContext.CurrentUsername ?? "user");
            Dock = DockStyle.Bottom;
            BackColor = AppTheme.Surface;
            Padding = new Padding(0, D(8), 0, D(8));
            bar = new ToggleBar(this);
            lblContext.Font = new Font("Segoe UI Semibold", 12F);
            lblContext.ForeColor = AppTheme.TextPrimary;
            lblContext.UseMnemonic = false;
            lblContext.AutoSize = false;
            lblContext.AutoEllipsis = true;
            Controls.Add(bar);
            Controls.Add(lblContext);
            Controls.Add(cards);
            try { expanded = string.Equals(MJdatabase.GetSystemSetting(key, "False"), "True", StringComparison.OrdinalIgnoreCase); }
            catch (Exception ex) { ErrLog.Swallowed(ex); expanded = false; }
            cards.Resize += (s, e) => Arrange();
            Arrange();
            // Settings → Features → "Show All-in-One Summary" (off by default): hidden completely when off, shown live when switched on.
            Features.Bind(this, () => Visible = Features.On(Features.AllInOneSummary), Features.AllInOneSummary);
        }



        /// <summary>Puts new figures on the summary (call after every search, filter, edit or selection).</summary>
        /// <param name="context">Optional line above the cards, e.g. what is selected or how many rows are shown.</param>
        public void SetMetrics(IReadOnlyList<SummaryMetric> metrics, string quickPreview, string context = "")
        {
            preview = quickPreview ?? "";
            lblContext.Text = context ?? "";
            cards.SetMetrics(metrics ?? Array.Empty<SummaryMetric>());
            bar.Invalidate();
            Arrange();
        }



        private void Toggle()
        {
            expanded = !expanded;
            try { MJdatabase.SetSystemSetting(key, expanded ? "True" : "False", "All-in-One Summary open/closed"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            Arrange();
            bar.Invalidate();
        }



        protected override void OnResize(EventArgs eventargs) { base.OnResize(eventargs); Arrange(); }
        private Control? hookedParent;
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (hookedParent != null) hookedParent.Resize -= ParentResized;
            hookedParent = Parent;
            if (hookedParent != null) hookedParent.Resize += ParentResized;
            Arrange();
        }
        private void ParentResized(object? sender, EventArgs e) => Arrange();
        private bool arranging;
        /// <summary>
        /// Lays out the bar and the cards. When open, the summary takes at most half of the space it shares with the
        /// table (it scrolls inside if the cards need more), so the table always keeps room for its rows.
        /// </summary>
        private void Arrange()
        {
            if (arranging) return;
            arranging = true;
            try
            {
                int sb = SystemInformation.VerticalScrollBarWidth;
                int full = Math.Max(D(200), Width - Padding.Horizontal);
                int maxH = Parent != null ? Math.Max(D(150), Parent.ClientSize.Height / 2) : int.MaxValue;
                // First pass at full width to see if it fits; if not, leave room for the scroll bar.
                int natural = PlaceParts(full, 0);
                bool scroll = expanded && natural > maxH;
                if (scroll) natural = PlaceParts(full - sb, AutoScrollPosition.Y);
                AutoScroll = scroll;
                AutoScrollMinSize = scroll ? new Size(0, natural) : Size.Empty;
                int want = scroll ? maxH : natural;
                if (Height != want) Height = want;
                if (!scroll && AutoScrollPosition.Y != 0) { AutoScrollPosition = Point.Empty; PlaceParts(full, 0); }
            }
            finally { arranging = false; }
        }


        /// <summary>Places the parts at <paramref name="w"/> wide (shifted by the scroll offset); returns the height they need.</summary>
        private int PlaceParts(int w, int offsetY)
        {
            int y = Padding.Top;
            int bh = D(54);
            bar.Bounds = new Rectangle(0, y + offsetY, w, bh); y += bh;
            lblContext.Visible = expanded && lblContext.Text.Length > 0;
            cards.Visible = expanded;
            if (expanded)
            {
                y += D(10);
                if (lblContext.Visible) { int lh = TextRenderer.MeasureText("Ag", lblContext.Font).Height + D(4); lblContext.Bounds = new Rectangle(D(2), y + offsetY, w - D(4), lh); y += lh + D(6); }
                cards.Width = w;
                cards.Location = new Point(0, y + offsetY);
                y += cards.Height;
            }
            return y + Padding.Bottom;
        }

        /// <summary>The clickable full-width bar (keyboard: Tab to it, Enter or Space toggles).</summary>
        private sealed class ToggleBar : Control
        {
            private readonly AllInOneSummaryBar owner;
            private bool hover;
            public ToggleBar(AllInOneSummaryBar o)
            {
                owner = o;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                Cursor = Cursors.Hand;
                TabStop = true;
                AccessibleRole = AccessibleRole.PushButton;
            }
            private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnClick(EventArgs e) { base.OnClick(e); owner.Toggle(); }
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode is Keys.Enter or Keys.Space) { owner.Toggle(); e.Handled = true; }
            }
            protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Enter or Keys.Space || base.IsInputKey(keyData);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Color tone = SummaryColors.Of(SummaryTone.Blue);
                Color back = hover ? ThemeManager.Mix(AppTheme.Surface, tone, AppTheme.IsDark ? 0.42 : 0.16) : ThemeManager.Mix(AppTheme.Surface, tone, AppTheme.IsDark ? 0.32 : 0.09);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
                using (var p = new Pen(Focused ? AppTheme.Accent : tone, D(3)) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset }) g.DrawRectangle(p, r);
                string label = owner.expanded ? "▲  Hide All-in-One Summary Overview" : "▼  Show All-in-One Summary Overview";
                using var lf = new Font("Segoe UI", 14F, FontStyle.Bold);
                using var pf = new Font("Segoe UI", 12F, FontStyle.Bold);
                int pad = D(16);
                var ls = TextRenderer.MeasureText(label, lf);
                Color ink = SummaryColors.Ink(SummaryTone.Blue);
                TextRenderer.DrawText(g, label, lf, new Rectangle(pad, 0, ls.Width, Height), ink, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                int px = pad + ls.Width + D(20);
                if (owner.preview.Length > 0 && px < Width - pad)
                    TextRenderer.DrawText(g, owner.preview, pf, new Rectangle(px, 0, Width - px - pad, Height), AppTheme.TextPrimary, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.Right);
            }
        }



        // ------------------------------------------------------------------ placing it under a table
        /// <summary>
        /// Puts a summary bar right under <paramref name="grid"/> (the table keeps the rest of the space). Works when the
        /// table fills its panel, sits in a table-layout cell, or has a fixed size.
        /// </summary>
        public static AllInOneSummaryBar AttachBelow(DataGridView grid, string screenKey)
        {
            var bar = new AllInOneSummaryBar(screenKey);
            var parent = grid.Parent;
            if (parent == null) throw new InvalidOperationException("The table must be on the screen before the summary is attached.");
            if (grid.Dock == DockStyle.Fill && parent is not TableLayoutPanel && parent is not FlowLayoutPanel)
            {
                parent.Controls.Add(bar);
                bar.SendToBack(); // docked first, so it takes the bottom edge and the table fills the rest
                return bar;
            }
            // Otherwise the table and the bar share a holder that takes the table's place.
            var host = new Panel { Dock = grid.Dock, Anchor = grid.Anchor, Margin = grid.Margin, BackColor = parent.BackColor, Bounds = grid.Bounds, Name = grid.Name + "_summaryHost" };
            int tableHeight = grid.Height;
            parent.SuspendLayout();
            if (parent is TableLayoutPanel tlp)
            {
                var pos = tlp.GetPositionFromControl(grid);
                int cs = tlp.GetColumnSpan(grid), rs = tlp.GetRowSpan(grid);
                tlp.Controls.Remove(grid);
                tlp.Controls.Add(host, pos.Column, pos.Row);
                tlp.SetColumnSpan(host, cs); tlp.SetRowSpan(host, rs);
            }
            else
            {
                int idx = parent.Controls.GetChildIndex(grid);
                parent.Controls.Remove(grid);
                parent.Controls.Add(host);
                parent.Controls.SetChildIndex(host, idx);
            }
            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0);
            host.Controls.Add(grid);
            host.Controls.Add(bar);
            bar.SendToBack();
            // A table with its own fixed height keeps it; the holder grows by the summary's height.
            if (host.Dock is DockStyle.Top or DockStyle.Bottom or DockStyle.None)
            {
                void Grow() { host.Height = tableHeight + bar.Height; }
                bar.SizeChanged += (s, e) => Grow();
                Grow();
            }
            parent.ResumeLayout(true);
            return bar;
        }



        // ------------------------------------------------------------------ helpers for the figures
        public static string Money(decimal v) => "₱" + v.ToString("N2");
        public static string Count(int n, string one, string many) => $"{n:N0} {(n == 1 ? one : many)}";
    }
}
