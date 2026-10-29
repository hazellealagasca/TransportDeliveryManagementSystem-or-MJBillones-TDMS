using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>A date range used by list filters. From/To are inclusive dates; both null means "all dates".</summary>
    public sealed class DateRange
    {
        public DateTime? From { get; }
        public DateTime? To { get; }
        /// <summary>day, week, month, year, custom or all — decides how ‹ › step the range.</summary>
        public string Kind { get; }
        public DateRange(DateTime? from, DateTime? to, string kind)
        {
            if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date) (from, to) = (to, from);
            From = from?.Date; To = to?.Date; Kind = kind;
        }
        public static DateRange All => new DateRange(null, null, "all");
        public bool IsAll => From == null && To == null;
        public bool Contains(DateTime d) => (From == null || d.Date >= From.Value) && (To == null || d.Date <= To.Value);
        public bool Contains(DateTime? d) => IsAll || (d.HasValue && Contains(d.Value));
        public static DateTime WeekStart(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)); // Monday
        /// <summary>The ranges the menus offer, worked out from today's date.</summary>
        public static DateRange Preset(string name)
        {
            var t = DateTime.Today;
            var m = new DateTime(t.Year, t.Month, 1);
            return name switch
            {
                "Today" => new DateRange(t, t, "day"),
                "Yesterday" => new DateRange(t.AddDays(-1), t.AddDays(-1), "day"),
                "Tomorrow" => new DateRange(t.AddDays(1), t.AddDays(1), "day"),
                "This week" => new DateRange(WeekStart(t), WeekStart(t).AddDays(6), "week"),
                "Last week" => new DateRange(WeekStart(t).AddDays(-7), WeekStart(t).AddDays(-1), "week"),
                "Next 7 days" => new DateRange(t, t.AddDays(6), "custom"),
                "Last 7 days" => new DateRange(t.AddDays(-6), t, "custom"),
                "Last 30 days" => new DateRange(t.AddDays(-29), t, "custom"),
                "This month" => new DateRange(m, m.AddMonths(1).AddDays(-1), "month"),
                "Last month" => new DateRange(m.AddMonths(-1), m.AddDays(-1), "month"),
                "Next month" => new DateRange(m.AddMonths(1), m.AddMonths(2).AddDays(-1), "month"),
                "This year" => new DateRange(new DateTime(t.Year, 1, 1), new DateTime(t.Year, 12, 31), "year"),
                _ => All
            };
        }
        /// <summary>The same-sized period before (-1) or after (+1) this one.</summary>
        public DateRange Step(int dir)
        {
            if (IsAll || From == null || To == null) return this;
            switch (Kind)
            {
                case "day": return new DateRange(From.Value.AddDays(dir), To.Value.AddDays(dir), "day");
                case "week": return new DateRange(From.Value.AddDays(7 * dir), To.Value.AddDays(7 * dir), "week");
                case "month": { var s = new DateTime(From.Value.Year, From.Value.Month, 1).AddMonths(dir); return new DateRange(s, s.AddMonths(1).AddDays(-1), "month"); }
                case "year": { var s = new DateTime(From.Value.Year + dir, 1, 1); return new DateRange(s, new DateTime(s.Year, 12, 31), "year"); }
                default: { int len = (To.Value - From.Value).Days + 1; return new DateRange(From.Value.AddDays(len * dir), To.Value.AddDays(len * dir), "custom"); }
            }
        }
        public bool SameAs(DateRange o) => From == o.From && To == o.To;
        /// <summary>Plain words for the chip: "Today · Wed, Oct 28", "October 2026", "Oct 1 – 15, 2026".</summary>
        public string Describe(IEnumerable<string>? presetNames = null)
        {
            if (IsAll) return "All dates";
            var f = From!.Value; var to = To!.Value; var t = DateTime.Today;
            foreach (var p in presetNames ?? Array.Empty<string>())
            {
                if (p == "All dates") continue;
                var pr = Preset(p);
                if (pr.SameAs(this))
                {
                    if (Kind == "day") return $"{p} · {f:ddd, MMM d}";
                    if (Kind == "month") return $"{p} · {f:MMMM}";
                    return p;
                }
            }
            if (f == to) return f.Year == t.Year ? f.ToString("ddd, MMM d") : f.ToString("ddd, MMM d, yyyy");
            if (Kind == "month") return f.ToString("MMMM yyyy");
            if (Kind == "year") return f.Year.ToString();
            if (f.Year == to.Year && f.Month == to.Month) return $"{f:MMM d} – {to:%d}, {to:yyyy}";
            if (f.Year == to.Year) return $"{f:MMM d} – {to:MMM d}, {to:yyyy}";
            return $"{f:MMM d, yyyy} – {to:MMM d, yyyy}";
        }
    }
    /// <summary>
    /// One rounded "chip" button used by the filter bars. Painted by hand so the theme pass and the
    /// confirmation gate leave it alone; it turns accent-coloured when its filter is not on the default.
    /// </summary>
    public class FilterChip : Control
    {
        private bool hover;
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool Highlighted { get; set; }
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Icon { get; set; } = "";
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool ShowCaret { get; set; } = true;
        protected int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public FilterChip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9.25F);
            Margin = new Padding(0, 2, 8, 2);
            TabStop = true;
        }
        protected override void OnCreateControl() { base.OnCreateControl(); FitWidth(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); FitWidth(); Invalidate(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); FitWidth(); }
        public virtual void FitWidth()
        {
            string full = (Icon.Length > 0 ? Icon + "  " : "") + Text + (ShowCaret ? "   ▾" : "");
            int w = TextRenderer.MeasureText(full, Font).Width + S(26);
            Size = new Size(Math.Max(S(60), w), S(32));
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space or Keys.Down) { OnClick(EventArgs.Empty); e.Handled = true; }
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        /// <summary>The first solid colour behind the chip (parents may be transparent).</summary>
        protected Color BackdropColor()
        {
            for (Control? c = Parent; c != null; c = c.Parent)
                if (c.BackColor.A == 255) return c.BackColor;
            return AppTheme.Surface;
        }
        protected void PaintChip(Graphics g, Rectangle r, string text, bool on, bool hot)
        {
            Color fill = on ? AppTheme.AccentSoft : hot ? AppTheme.SurfaceHover : AppTheme.Surface;
            Color border = on ? AppTheme.Accent : hot || Focused ? AppTheme.BorderStrong : AppTheme.Border;
            Color fore = on ? AppTheme.AccentText : AppTheme.TextPrimary;
            using (var path = Rounded(r, S(8)))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var p = new Pen(border, on ? 1.5f : 1f)) g.DrawPath(p, path);
            }
            TextRenderer.DrawText(g, text, Font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackdropColor());
            string full = (Icon.Length > 0 ? Icon + "  " : "") + Text + (ShowCaret ? "   ▾" : "");
            PaintChip(g, new Rectangle(0, 0, Width - 1, Height - 1), full, Highlighted, hover);
        }
        protected static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var p = new GraphicsPath(); int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
        protected ContextMenuStrip NewMenu()
        {
            var m = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = true, Font = new Font("Segoe UI", 9.5F) };
            try { m.Renderer = new ToolStripProfessionalRenderer(); m.BackColor = AppTheme.Surface; m.ForeColor = AppTheme.TextPrimary; } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            m.Closed += (s, e) => BeginInvoke(new Action(() => m.Dispose()));
            return m;
        }
    }
    /// <summary>"⇅ Sort: Newest first ▾" or "Status: All ▾" — pick one option from a short menu.</summary>
    public sealed class ChoiceChip : FilterChip
    {
        private readonly string prefix;
        private readonly string[] options;
        private int selected;
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public int DefaultIndex { get; set; }
        public event EventHandler? SelectionChanged;
        public ChoiceChip(string prefix, IEnumerable<string> options, string icon = "")
        {
            this.prefix = prefix; this.options = options.ToArray(); Icon = icon;
            UpdateText();
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => selected;
            set { int v = Math.Max(0, Math.Min(options.Length - 1, value)); if (v == selected) return; selected = v; UpdateText(); SelectionChanged?.Invoke(this, EventArgs.Empty); }
        }
        /// <summary>Changes the choice without raising SelectionChanged (to mirror what the list is doing).</summary>
        public void SetQuietly(int index) { selected = Math.Max(0, Math.Min(options.Length - 1, index)); UpdateText(); }
        public string SelectedText => options.Length == 0 ? "" : options[selected];
        private void UpdateText() { Text = (prefix.Length > 0 ? prefix + ": " : "") + SelectedText; Highlighted = selected != DefaultIndex; Invalidate(); }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var m = NewMenu();
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                var it = new ToolStripMenuItem(options[i]) { Checked = i == selected };
                it.Click += (s, a) => SelectedIndex = k;
                m.Items.Add(it);
            }
            m.Show(this, new Point(0, Height + S(2)));
        }
    }
    /// <summary>
    /// "‹  📅 This month · October ▾  ›" — the one date filter used on the dashboard and every list.
    /// Click the middle for quick periods, a specific date or a custom range; ‹ › step to the previous or next period.
    /// </summary>
    public sealed class DateFilterChip : FilterChip
    {
        private readonly string[] presets;
        private readonly DateRange defaultRange;
        private DateRange range;
        private int hot = -1; // 0 = ‹, 1 = middle, 2 = ›
        public event EventHandler? RangeChanged;
        public DateFilterChip(string[] presets, string defaultPreset)
        {
            this.presets = presets;
            defaultRange = DateRange.Preset(defaultPreset);
            range = defaultRange;
            Icon = "📅";
            Text = range.Describe(presets);
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DateRange Range
        {
            get => range;
            set { range = value ?? DateRange.All; Text = range.Describe(presets); Highlighted = !range.SameAs(defaultRange); Invalidate(); RangeChanged?.Invoke(this, EventArgs.Empty); }
        }
        public void SetQuietly(DateRange r) { range = r; Text = range.Describe(presets); Highlighted = !range.SameAs(defaultRange); Invalidate(); }
        public void ResetToDefault() => Range = DateRange.Preset(DefaultPresetName());
        private string DefaultPresetName() => presets.FirstOrDefault(p => DateRange.Preset(p).SameAs(defaultRange)) ?? "All dates";
        private int Arrow => S(30);
        private bool Stepable => !range.IsAll;
        public override void FitWidth()
        {
            int w = TextRenderer.MeasureText(Icon + "  " + Text + "   ▾", Font).Width + S(26);
            Size = new Size(Math.Max(S(120), w) + 2 * Arrow + S(8), S(32));
        }
        private Rectangle LeftBox => new Rectangle(0, 0, Arrow, Height - 1);
        private Rectangle RightBox => new Rectangle(Width - 1 - Arrow, 0, Arrow, Height - 1);
        private Rectangle MidBox => new Rectangle(Arrow + S(4), 0, Width - 1 - 2 * Arrow - S(8), Height - 1);
        private int HitTest(Point p) => LeftBox.Contains(p) ? 0 : RightBox.Contains(p) ? 2 : 1;
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int h = HitTest(e.Location); if (h != hot) { hot = h; Cursor = (h != 1 && !Stepable) ? Cursors.Default : Cursors.Hand; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = -1; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int h = HitTest(e.Location);
            if (h == 0 && Stepable) Range = range.Step(-1);
            else if (h == 2 && Stepable) Range = range.Step(1);
            else if (h == 1) ShowMenu();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left && Stepable) { Range = range.Step(-1); e.Handled = true; return; }
            if (e.KeyCode == Keys.Right && Stepable) { Range = range.Step(1); e.Handled = true; return; }
            if (e.KeyCode is Keys.Enter or Keys.Space or Keys.Down) { ShowMenu(); e.Handled = true; return; }
            base.OnKeyDown(e);
        }
        protected override void OnClick(EventArgs e) { /* handled in OnMouseClick so the arrows don't open the menu */ }
        private void ShowMenu()
        {
            var m = NewMenu();
            foreach (var p in presets)
            {
                var pr = DateRange.Preset(p);
                var it = new ToolStripMenuItem(p) { Checked = pr.SameAs(range) };
                if (!pr.IsAll) it.ShortcutKeyDisplayString = pr.Describe();
                it.Click += (s, a) => Range = DateRange.Preset(p);
                m.Items.Add(it);
            }
            m.Items.Add(new ToolStripSeparator());
            var one = new ToolStripMenuItem("Choose a date…");
            one.Click += (s, a) => { var r = DateRangePicker.Pick(FindForm(), false, range); if (r != null) Range = r; };
            var many = new ToolStripMenuItem("Choose a date range…");
            many.Click += (s, a) => { var r = DateRangePicker.Pick(FindForm(), true, range); if (r != null) Range = r; };
            m.Items.Add(one); m.Items.Add(many);
            m.Show(this, new Point(MidBox.X, Height + S(2)));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackdropColor());
            PaintChip(g, MidBox, Icon + "  " + Text + "   ▾", Highlighted, hot == 1);
            void ArrowBox(Rectangle r, string glyph, bool isHot)
            {
                bool enabled = Stepable;
                Color fill = enabled && isHot ? AppTheme.SurfaceHover : AppTheme.Surface;
                using (var path = Rounded(r, S(8)))
                {
                    using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                    using (var p = new Pen(enabled && isHot ? AppTheme.BorderStrong : AppTheme.Border)) g.DrawPath(p, path);
                }
                using var f = new Font("Segoe UI Semibold", 11F);
                TextRenderer.DrawText(g, glyph, f, r, enabled ? AppTheme.TextPrimary : AppTheme.Border, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            ArrowBox(LeftBox, "‹", hot == 0);
            ArrowBox(RightBox, "›", hot == 2);
        }
    }
    /// <summary>Small window with a calendar for one date, or two calendars (From / To) for a range.</summary>
    public static class DateRangePicker
    {
        public static DateRange? Pick(IWin32Window? owner, bool range, DateRange current)
        {
            int D(int v) => (int)Math.Round(v * (owner is Control c ? c.DeviceDpi : 96) / 96f);
            using var dlg = new Form
            {
                Text = range ? "Choose a date range" : "Choose a date",
                FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F), AutoScaleMode = AutoScaleMode.Dpi, KeyPreview = true
            };
            try { ThemeManager.StyleWindowChrome(dlg); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            DateTime start = current.From ?? DateTime.Today, end = current.To ?? DateTime.Today;
            MonthCalendar Cal(DateTime d) => new MonthCalendar { MaxSelectionCount = 1, SelectionStart = d, SelectionEnd = d, ShowToday = true, ShowTodayCircle = true, Margin = new Padding(D(8)) };
            var calFrom = Cal(start);
            var calTo = Cal(end);
            var lblFrom = new Label { Text = range ? "From" : "Date", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(D(8), D(8), 0, 0) };
            var lblTo = new Label { Text = "To", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(D(8), D(8), 0, 0) };
            var summary = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Margin = new Padding(D(10), D(6), 0, D(6)) };
            void Update()
            {
                var a = calFrom.SelectionStart.Date; var b = range ? calTo.SelectionStart.Date : a;
                if (b < a) (a, b) = (b, a);
                summary.Text = range ? $"{a:ddd, MMM d, yyyy}  →  {b:ddd, MMM d, yyyy}   ({(b - a).Days + 1} day{((b - a).Days == 0 ? "" : "s")})" : a.ToString("dddd, MMMM d, yyyy");
            }
            calFrom.DateChanged += (s, e) => Update();
            calTo.DateChanged += (s, e) => Update();
            var grid = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = range ? 2 : 1, RowCount = 2, Padding = new Padding(D(8), D(4), D(8), 0) };
            grid.Controls.Add(lblFrom, 0, 0); grid.Controls.Add(calFrom, 0, 1);
            if (range) { grid.Controls.Add(lblTo, 1, 0); grid.Controls.Add(calTo, 1, 1); }
            var bar = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(D(8)), Anchor = AnchorStyles.Right, WrapContents = false };
            var ok = new Button { Text = "Show these dates", AutoSize = true, DialogResult = DialogResult.OK, Padding = new Padding(D(10), D(3), D(10), D(3)) };
            var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel, Padding = new Padding(D(10), D(3), D(10), D(3)) };
            try { UIThemeHelper.SetButtonRole(ok, "primary"); UIThemeHelper.SetButtonRole(cancel, "secondary"); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            bar.Controls.Add(ok); bar.Controls.Add(cancel);
            var root = new FlowLayoutPanel { Location = new Point(0, 0), FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = AppTheme.Surface };
            root.Controls.Add(grid); root.Controls.Add(summary); root.Controls.Add(bar);
            dlg.Controls.Add(root);
            dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            dlg.AutoSize = true; dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Update();
            if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
            var f = calFrom.SelectionStart.Date; var t = range ? calTo.SelectionStart.Date : f;
            return new DateRange(f, t, f == t ? "day" : "custom");
        }
    }
    /// <summary>The menus used across the system, so every list offers the same choices.</summary>
    public static class DatePresets
    {
        /// <summary>For lists of past records (completed trips, invoices, receipts, incidents).</summary>
        public static readonly string[] History = { "All dates", "Today", "Yesterday", "This week", "Last week", "Last 7 days", "This month", "Last month", "Last 30 days", "This year" };
        /// <summary>For orders, which can be in the past or scheduled ahead.</summary>
        public static readonly string[] Orders = { "All dates", "Today", "Tomorrow", "Yesterday", "This week", "Next 7 days", "Last 7 days", "This month", "Next month", "Last month", "This year" };
        /// <summary>For the dashboard (always a specific period).</summary>
        public static readonly string[] Dashboard = { "Today", "Tomorrow", "Yesterday", "This week", "Next 7 days", "Last 7 days", "This month", "Last month" };
    }
    /// <summary>
    /// Toolbars that wrap onto a second line must grow taller, or the second line hides behind the list below.
    /// </summary>
    public static class FlowFit
    {
        public static void Grow(FlowLayoutPanel f)
        {
            bool busy = false;
            void Fit()
            {
                if (busy || f.Width <= 0) return;
                busy = true;
                try
                {
                    int h = f.GetPreferredSize(new Size(f.Width, 0)).Height;
                    if (h > 0 && f.Height != h) { f.AutoSize = false; f.Height = h; }
                    var p = f.Parent;
                    if (p != null)
                    {
                        // Auto-sized parents don't always grow for docked children, so make room explicitly.
                        int need = 0;
                        foreach (Control c in p.Controls) if (c.Visible && c.Dock == DockStyle.Top) need += c.Height + c.Margin.Vertical * 0;
                        need += p.Padding.Vertical;
                        if (p.MinimumSize.Height != need) p.MinimumSize = new Size(0, need);
                        if (p.Height < need) p.Height = need;
                        p.Parent?.PerformLayout();
                    }
                }
                finally { busy = false; }
            }
            f.WrapContents = true;
            f.Resize += (s, e) => Fit();
            f.ControlAdded += (s, e) => f.BeginInvokeSafe(Fit);
            f.Layout += (s, e) => f.BeginInvokeSafe(Fit);
            f.HandleCreated += (s, e) => Fit();
        }
        private static void BeginInvokeSafe(this Control c, Action a)
        {
            try { if (c.IsHandleCreated && !c.IsDisposed) c.BeginInvoke(a); } catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
        }
    }
}
