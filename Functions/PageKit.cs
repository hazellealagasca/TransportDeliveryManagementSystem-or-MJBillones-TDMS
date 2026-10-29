using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Building blocks for list pages (Trucks, Crew, Crew trip history, history logs): a page header with a title,
    /// one-line description and action buttons on the right, a strip of summary tiles that double as quick filters,
    /// and a filter bar. Everything is DPI-aware and uses the app theme tokens.
    /// </summary>
    public static class PageKit
    {
        public static int D(Control c, int v) => (int)Math.Round(v * (c.IsHandleCreated ? c.DeviceDpi : DpiOf(c)) / 96f);
        private static int DpiOf(Control c)
        {
            try { using var g = Graphics.FromHwnd(IntPtr.Zero); return (int)g.DpiX; } catch { return 96; }
        }
        /// <summary>Title + description on the left, actions on the right, on one line that never overlaps.</summary>
        public static PageHeader Header(string title, string subtitle) => new PageHeader(title, subtitle);
        public static Button Action(string text, string role, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(8, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = role == "secondary" ? 1 : 0;
            UIThemeHelper.SetButtonRole(b, role);
            b.Click += onClick;
            return b;
        }
        /// <summary>Styles a text box as a search field with a placeholder.</summary>
        public static void StyleSearch(TextBox box, string placeholder)
        {
            box.PlaceholderText = placeholder;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = new Font("Segoe UI", 10F);
            box.BackColor = AppTheme.InputBack;
            box.ForeColor = AppTheme.TextPrimary;
        }
        public static void StyleCombo(ComboBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.Font = new Font("Segoe UI", 10F);
            cb.BackColor = AppTheme.InputBack;
            cb.ForeColor = AppTheme.TextPrimary;
        }
        /// <summary>Docks <paramref name="newTop"/> directly under <paramref name="anchor"/> inside the same parent (both Dock = Top).</summary>
        public static void InsertBelow(Control anchor, Control newTop)
        {
            var parent = anchor.Parent;
            if (parent == null) return;
            newTop.Dock = DockStyle.Top;
            parent.Controls.Add(newTop);
            parent.Controls.SetChildIndex(newTop, parent.Controls.GetChildIndex(anchor));
        }
        /// <summary>Docks <paramref name="newTop"/> above everything else inside <paramref name="parent"/>.</summary>
        public static void InsertAtTop(Control parent, Control newTop)
        {
            newTop.Dock = DockStyle.Top;
            parent.Controls.Add(newTop);
            parent.Controls.SetChildIndex(newTop, parent.Controls.Count - 1);
        }
        /// <summary>A transparent spacer of a given height, docked top.</summary>
        public static Panel Gap(Control c, int height) => new Panel { Dock = DockStyle.Top, Height = D(c, height), BackColor = Color.Transparent };
    }
    /// <summary>Page title, one-line description, and right-aligned actions.</summary>
    public sealed class PageHeader : TableLayoutPanel
    {
        public Label Title { get; }
        public Label Subtitle { get; }
        public FlowLayoutPanel Actions { get; }
        public PageHeader(string title, string subtitle)
        {
            Dock = DockStyle.Top;
            ColumnCount = 2;
            RowCount = 2;
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Title = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = AppTheme.TextPrimary,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 2)
            };
            Subtitle = new Label
            {
                Text = subtitle,
                AutoSize = true,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextMuted,
                UseMnemonic = false,
                Margin = new Padding(1, 0, 0, 0)
            };
            Actions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(12, 2, 0, 0),
                BackColor = Color.Transparent
            };
            Controls.Add(Title, 0, 0);
            Controls.Add(Subtitle, 0, 1);
            Controls.Add(Actions, 1, 0);
            SetRowSpan(Actions, 2);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
        /// <summary>Adds buttons right-to-left: the first one ends up right-most (make it the primary action).</summary>
        public void AddActions(params Control[] buttons)
        {
            foreach (var b in buttons)
            {
                if (b is Button btn) btn.MinimumSize = new Size(0, (int)Math.Round(36 * DeviceDpi / 96f));
                Actions.Controls.Add(b);
            }
        }
    }
    /// <summary>
    /// A summary card: caption, big number and a short note. When <see cref="Clickable"/> it works as a quick filter
    /// (click to select, click again to clear) and shows a highlighted border while selected.
    /// </summary>
    public sealed class StatTile : Control
    {
        private string caption = "", value = "", note = "";
        private bool selected, hover;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Color Accent { get; set; } = AppTheme.Accent;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Color? ValueColor { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool Clickable { get; set; }
        /// <summary>Hand cursor and hover, but no selected state (the tile opens a page instead of filtering).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool NavigateOnClick { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Caption { get => caption; set { caption = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Value { get => value; set { this.value = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Note { get => note; set { note = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool Selected { get => selected; set { selected = value; Invalidate(); } }
        private string value2 = "", note2 = "", label1 = "", label2 = "";
        /// <summary>Optional second figure shown beside the first one (e.g. drivers | aides). Empty = single figure.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Value2 { get => value2; set { value2 = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Note2 { get => note2; set { note2 = value ?? ""; Invalidate(); } }
        /// <summary>Small labels drawn before each figure when the tile is split (e.g. "Drivers", "Aides").</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Label1 { get => label1; set { label1 = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string Label2 { get => label2; set { label2 = value ?? ""; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Color? ValueColor2 { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public object? Key { get; set; }
        public StatTile(string caption, Color accent, string note = "")
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.caption = caption;
            this.note = note;
            Accent = accent;
            value = "—";
            BackColor = Color.Transparent;
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 0, 10, 0);
            MouseEnter += (s, e) => { hover = true; Invalidate(); };
            MouseLeave += (s, e) => { hover = false; Invalidate(); };
        }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Clickable || NavigateOnClick ? Cursors.Hand : Cursors.Default;
        }
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = selected ? ThemeManager.Mix(AppTheme.Surface, Accent, 0.07) : (hover && (Clickable || NavigateOnClick) ? AppTheme.SurfaceHover : AppTheme.Surface);
            using (var path = Gfx.RoundedRect(r, S(10)))
            {
                using var b = new SolidBrush(fill);
                g.FillPath(b, path);
                using var pen = new Pen(selected ? Accent : AppTheme.Border, selected ? 2f : 1f);
                g.DrawPath(pen, path);
            }
            int pad = S(16);
            // accent dot + caption
            int dot = S(8);
            using (var db = new SolidBrush(Accent)) g.FillEllipse(db, pad, S(17), dot, dot);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            using var capFont = new Font("Segoe UI Semibold", 9F);
            TextRenderer.DrawText(g, caption, capFont, new Rectangle(pad + dot + S(8), S(10), Width - pad * 2 - dot, S(22)), AppTheme.TextSecondary, flags);
            using var valFont = new Font("Segoe UI Semibold", 19F);
            if (!string.IsNullOrEmpty(value2))
            {
                // Two figures side by side with a thin divider: [Label1 value / note] | [Label2 value2 / note2]
                int half = (Width - pad * 2) / 2;
                using var lblFont = new Font("Segoe UI Semibold", 9F);
                using var noteF = new Font("Segoe UI", 8.5F);
                void Half(int x, int w, string lbl, string v, string n, Color? vc)
                {
                    int lw = string.IsNullOrEmpty(lbl) ? 0 : TextRenderer.MeasureText(g, v, valFont, Size.Empty, TextFormatFlags.NoPadding).Width + S(6);
                    TextRenderer.DrawText(g, v, valFont, new Rectangle(x - S(2), S(32), w, S(36)), vc ?? AppTheme.TextPrimary, flags);
                    if (lw > 0) TextRenderer.DrawText(g, lbl, lblFont, new Rectangle(x + lw, S(42), Math.Max(1, w - lw), S(22)), AppTheme.TextSecondary, flags);
                    if (!string.IsNullOrEmpty(n)) TextRenderer.DrawText(g, n, noteF, new Rectangle(x, S(68), w - S(6), S(20)), AppTheme.TextMuted, flags);
                }
                Half(pad, half - S(8), label1, value, note, ValueColor);
                using (var dp = new Pen(AppTheme.Border)) g.DrawLine(dp, pad + half, S(38), pad + half, Height - S(14));
                Half(pad + half + S(14), half - S(14), label2, value2, note2, ValueColor2);
                return;
            }
            TextRenderer.DrawText(g, value, valFont, new Rectangle(pad - S(2), S(32), Width - pad * 2, S(36)), ValueColor ?? AppTheme.TextPrimary, flags);
            if (!string.IsNullOrEmpty(note))
            {
                using var noteFont = new Font("Segoe UI", 8.5F);
                TextRenderer.DrawText(g, note, noteFont, new Rectangle(pad, S(68), Width - pad * 2, S(20)), selected ? Accent : AppTheme.TextMuted, flags);
            }
        }
    }
    /// <summary>A row of equally wide <see cref="StatTile"/>s. Clickable tiles act as radio-style quick filters.</summary>
    public sealed class TileStrip : TableLayoutPanel
    {
        private readonly List<StatTile> tiles = new List<StatTile>();
        public event Action<StatTile?>? FilterChanged;
        public StatTile? SelectedTile => tiles.FirstOrDefault(t => t.Selected);
        public TileStrip()
        {
            Dock = DockStyle.Top;
            RowCount = 1;
            ColumnCount = 0;
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Height = (int)Math.Round(96 * DeviceDpi / 96f);
        }
        public StatTile Add(StatTile t)
        {
            tiles.Add(t);
            ColumnCount = tiles.Count;
            ColumnStyles.Clear();
            for (int i = 0; i < tiles.Count; i++) ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / tiles.Count));
            Controls.Add(t, tiles.Count - 1, 0);
            for (int i = 0; i < tiles.Count; i++) tiles[i].Margin = new Padding(0, 0, i == tiles.Count - 1 ? 0 : (int)Math.Round(10 * DeviceDpi / 96f), 0);
            if (t.Clickable)
            {
                t.Click += (s, e) =>
                {
                    bool turnOn = !t.Selected;
                    foreach (var x in tiles) x.Selected = false;
                    t.Selected = turnOn;
                    FilterChanged?.Invoke(turnOn ? t : null);
                };
            }
            return t;
        }
        public void ClearSelection()
        {
            if (tiles.All(t => !t.Selected)) return;
            foreach (var x in tiles) x.Selected = false;
            FilterChanged?.Invoke(null);
        }
    }
}
