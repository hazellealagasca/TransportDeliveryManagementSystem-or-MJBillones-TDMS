using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Building blocks shared by the truck and crew profile windows: tabs, header, themed grids, stat rows, type-ahead.</summary>
    public static class ProfileKit
    {
        public static int D(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96f);
        /// <summary>Read-only, latest-first history grid in the app's theme.</summary>
        public static DataGridView Grid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                BackgroundColor = AppTheme.Surface,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34,
                Font = new Font("Segoe UI", 9.5F)
            };
            g.RowTemplate.Height = 32;
            try { UIThemeHelper.ApplyGridTheme(g); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            return g;
        }
        public static DataGridViewTextBoxColumn Col(string name, string header, float weight, bool right = false)
        {
            var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight, SortMode = DataGridViewColumnSortMode.Automatic };
            if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }
        /// <summary>A white card with a thin border holding one control.</summary>
        public static Panel Card(Control content, int pad = 12)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(pad) };
            card.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            content.Dock = DockStyle.Fill;
            card.Controls.Add(content);
            return card;
        }
        public static Label Section(string title, string sub = "")
        {
            return new Label
            {
                Text = sub.Length == 0 ? title : title + "   ·   " + sub,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI Semibold", 10.5F),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }
        public static Label Caption(string text) => new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 6, 0, 2), UseMnemonic = false };
        public static TextBox Box(string cue = "") => new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F), PlaceholderText = cue, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary };
        public static Button Button(string text, string role, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(0, 34), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10, 0, 10, 0), UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, role);
            b.Click += click;
            return b;
        }
        /// <summary>Type-ahead from values used before plus sensible defaults (see MJdatabase.GetSuggestions).</summary>
        public static void Suggest(Control box, string source)
        {
            var src = new AutoCompleteStringCollection();
            src.AddRange(MJdatabase.GetSuggestions(source).ToArray());
            if (box is TextBox t)
            {
                t.AutoCompleteCustomSource = src;
                t.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                t.AutoCompleteSource = AutoCompleteSource.CustomSource;
            }
            else if (box is ComboBox cb)
            {
                string keep = cb.Text;
                cb.Items.Clear();
                foreach (string s in src) cb.Items.Add(s);
                cb.AutoCompleteCustomSource = src;
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                cb.Text = keep;
            }
        }
        public static string Ago(DateTime t)
        {
            var d = DateTime.Today - t.Date;
            if (d.TotalDays < 0) return "in " + (-(int)d.TotalDays) + " day(s)";
            if (d.TotalDays < 1) return "today";
            if (d.TotalDays < 2) return "yesterday";
            if (d.TotalDays < 31) return $"{(int)d.TotalDays} days ago";
            return t.ToString("MMM d, yyyy");
        }
    }
    /// <summary>Underlined tab strip used at the top of profile windows.</summary>
    public sealed class SegmentTabs : Control
    {
        private readonly string[] labels;
        private int selected, hover = -1;
        public event EventHandler? SelectedChanged;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Selected { get => selected; set { if (value == selected || value < 0 || value >= labels.Length) return; selected = value; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int[] Badges { get; set; } = Array.Empty<int>();
        public SegmentTabs(params string[] labels)
        {
            this.labels = labels;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Dock = DockStyle.Top;
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = S(46); }
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private string Label(int i) => i < Badges.Length && Badges[i] > 0 ? $"{labels[i]}  {Badges[i]}" : labels[i];
        private Rectangle[] Slots()
        {
            var r = new Rectangle[labels.Length];
            int x = S(20);
            using var f = new Font("Segoe UI Semibold", 9.5F);
            for (int i = 0; i < labels.Length; i++)
            {
                int w = TextRenderer.MeasureText(Label(i), f).Width + S(24);
                r[i] = new Rectangle(x, 0, w, Height);
                x += w + S(4);
            }
            return r;
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int h = Array.FindIndex(Slots(), r => r.Contains(e.Location)); if (h != hover) { hover = h; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); int h = Array.FindIndex(Slots(), r => r.Contains(e.Location)); if (h >= 0) Selected = h; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Surface);
            using (var line = new Pen(AppTheme.Border)) g.DrawLine(line, 0, Height - 1, Width, Height - 1);
            var slots = Slots();
            using var f = new Font("Segoe UI Semibold", 9.5F);
            for (int i = 0; i < labels.Length; i++)
            {
                var r = slots[i];
                bool on = i == selected;
                if (i == hover && !on)
                    using (var hb = new SolidBrush(AppTheme.SurfaceHover))
                    using (var p = Gfx.RoundedRect(new Rectangle(r.X, S(7), r.Width, Height - S(14)), S(6)))
                        g.FillPath(hb, p);
                TextRenderer.DrawText(g, Label(i), f, r, on ? AppTheme.AccentText : AppTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (on) using (var ab = new SolidBrush(AppTheme.Accent)) g.FillRectangle(ab, r.X + S(8), Height - S(3), r.Width - S(16), S(3));
            }
        }
    }
    /// <summary>Profile header: a round badge with initials or an icon, a title, a line of facts and coloured chips.</summary>
    public sealed class ProfileHeader : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string Badge { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string Title { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string Subtitle { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public List<(string Text, Color Back, Color Fore)> Chips { get; } = new List<(string, Color, Color)>();
        public ProfileHeader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = S(112); }
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            using (var bg = new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle, ThemeManager.Mix(AppTheme.Surface, AppTheme.Accent, AppTheme.IsDark ? 0.22 : 0.09), AppTheme.Surface, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                g.FillRectangle(bg, ClientRectangle);
            using (var line = new Pen(AppTheme.Border)) g.DrawLine(line, 0, Height - 1, Width, Height - 1);
            int pad = S(24), av = S(64), ay = (Height - av) / 2;
            using (var ab = new SolidBrush(AppTheme.Accent)) g.FillEllipse(ab, pad, ay, av, av);
            using (var bf = new Font("Segoe UI Semibold", Badge.Length > 2 ? 15F : 18F))
                TextRenderer.DrawText(g, Badge, bf, new Rectangle(pad, ay, av, av), AppTheme.OnAccent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            int x = pad + av + S(18), w = Width - x - pad;
            var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            using var tf = new Font("Segoe UI", 16F, FontStyle.Bold);
            TextRenderer.DrawText(g, Title, tf, new Rectangle(x, ay - S(4), w, S(32)), AppTheme.TextPrimary, flags);
            using var sf = new Font("Segoe UI", 9.5F);
            TextRenderer.DrawText(g, Subtitle, sf, new Rectangle(x, ay + S(28), w, S(20)), AppTheme.TextMuted, flags);
            using var cf = new Font("Segoe UI Semibold", 8.5F);
            int cx = x, cy = ay + S(50);
            foreach (var (text, back, fore) in Chips)
            {
                var sz = TextRenderer.MeasureText(text, cf);
                var r = new Rectangle(cx, cy, sz.Width + S(14), S(22));
                if (r.Right > Width - pad) break;
                using (var p = Gfx.RoundedRect(r, S(11))) using (var b = new SolidBrush(back)) g.FillPath(b, p);
                TextRenderer.DrawText(g, text, cf, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                cx = r.Right + S(6);
            }
        }
    }
    /// <summary>A row of equal-width stat tiles.</summary>
    public sealed class StatRow : Panel
    {
        public readonly List<StatTile> Tiles = new List<StatTile>();
        public StatRow() { Dock = DockStyle.Top; BackColor = AppTheme.Canvas; }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)Math.Round(92 * DeviceDpi / 96f); }
        public StatTile Add(string caption, string value, string note, Color accent, Color? valueColor = null)
        {
            var t = new StatTile(caption, accent, note) { Value = value, ValueColor = valueColor, Dock = DockStyle.None, Margin = Padding.Empty };
            Tiles.Add(t);
            Controls.Add(t);
            PerformLayout();
            return t;
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Tiles.Count == 0) return;
            int gap = (int)Math.Round(10 * DeviceDpi / 96f);
            int w = (ClientSize.Width - gap * (Tiles.Count - 1)) / Tiles.Count;
            for (int i = 0; i < Tiles.Count; i++) Tiles[i].SetBounds(i * (w + gap), 0, i == Tiles.Count - 1 ? ClientSize.Width - i * (w + gap) : w, ClientSize.Height);
        }
    }
}
