using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Settings → Appearance: light / dark / match-Windows mode, accent colour (presets or any custom colour)
    /// and sidebar style, with a live miniature preview of the whole system before applying.
    /// </summary>
    public class AppearanceSettingsView : UserControl, IRefreshable
    {
        private ThemeMode pendingMode;
        private Color pendingAccent;
        private SidebarStyle pendingSidebar;
        private readonly List<OptionCard> modeCards = new List<OptionCard>();
        private readonly List<OptionCard> sidebarCards = new List<OptionCard>();
        private readonly List<Swatch> swatches = new List<Swatch>();
        private PreviewCanvas preview = null!;
        private TextBox txtHex = null!;
        private Panel pnlCustomChip = null!;
        private Label lblStatus = null!;
        private bool updatingHex;
        private static float F => Math.Max(1f, Screen.PrimaryScreen != null ? GetDpiScale() : 1f);
        private static float GetDpiScale()
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return g.DpiX / 96f;
        }
        private static int S(int v) => (int)Math.Round(v * F);
        public AppearanceSettingsView()
        {
            pendingMode = ThemeManager.Mode;
            pendingAccent = ThemeManager.AccentColor;
            pendingSidebar = ThemeManager.Sidebar;
            BuildUi();
            UIThemeHelper.ApplyTheme(this);
            SyncSelection();
        }
        public void RefreshData()
        {
            pendingMode = ThemeManager.Mode;
            pendingAccent = ThemeManager.AccentColor;
            pendingSidebar = ThemeManager.Sidebar;
            SyncSelection();
        }
        // ------------------------------------------------------------------ layout
        private void BuildUi()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9F);
            var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.Canvas, Padding = new Padding(S(24), S(20), S(24), S(24)) };
            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = AppTheme.Canvas,
                Margin = new Padding(S(0))
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            // Left: the choices
            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Canvas,
                Margin = new Padding(S(0), S(0), S(20), S(0))
            };
            left.Controls.Add(BuildModeSection());
            left.Controls.Add(BuildAccentSection());
            left.Controls.Add(BuildSidebarSection());
            // Right: live preview + actions
            var right = new Card { Dock = DockStyle.Top, Height = S(470), Padding = new Padding(S(20)), Margin = new Padding(S(0)) };
            var lblPrev = SectionTitle("Live preview", "This is how the system will look. Nothing changes until you apply.");
            lblPrev.Dock = DockStyle.Top;
            preview = new PreviewCanvas { Dock = DockStyle.Top, Height = S(300), Margin = new Padding(S(0)) };
            var spacer = new Panel { Dock = DockStyle.Top, Height = S(14), BackColor = AppTheme.Surface };
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = S(48),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = AppTheme.Surface,
                Padding = new Padding(S(0), S(6), S(0), S(0))
            };
            var btnApply = new Button { Name = "btnApplyTheme", Text = "Apply theme", Size = new Size(S(140), S(38)), Margin = new Padding(S(0), S(0), S(10), S(0)) };
            UIThemeHelper.SetButtonRole(btnApply, "primary");
            btnApply.Font = new Font("Segoe UI Semibold", 9.5F);
            btnApply.Click += (s, e) => ApplyChoice();
            var btnReset = new Button { Name = "btnResetTheme", Text = "Reset to default", Size = new Size(S(150), S(38)), Margin = new Padding(S(0)) };
            UIThemeHelper.SetButtonRole(btnReset, "secondary");
            btnReset.Font = new Font("Segoe UI Semibold", 9.5F);
            btnReset.Click += (s, e) =>
            {
                pendingMode = ThemeMode.Light;
                pendingAccent = ThemeManager.DefaultAccent;
                pendingSidebar = SidebarStyle.Charcoal;
                SyncSelection();
            };
            actions.Controls.Add(btnApply);
            actions.Controls.Add(btnReset);
            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = S(40),
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                Text = "Applies to everyone who uses this computer. The window refreshes for a moment — you stay signed in.",
                Padding = new Padding(S(0), S(6), S(0), S(0))
            };
            right.Controls.Add(lblStatus);
            right.Controls.Add(actions);
            right.Controls.Add(spacer);
            right.Controls.Add(preview);
            right.Controls.Add(lblPrev);
            var rightHost = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Height = S(470), Margin = new Padding(S(0)) };
            rightHost.Controls.Add(right);
            columns.Controls.Add(left, 0, 0);
            columns.Controls.Add(rightHost, 1, 0);
            scroller.Controls.Add(columns);
            scroller.Resize += (s, e) => columns.Width = scroller.ClientSize.Width - scroller.Padding.Horizontal;
            left.SizeChanged += (s, e) => { foreach (Control c in left.Controls) c.Width = Math.Max(420, left.ClientSize.Width); };
            columns.SizeChanged += (s, e) =>
            {
                int w = (int)(columns.ClientSize.Width * 0.56) - 20;
                foreach (Control c in left.Controls) c.Width = Math.Max(420, w);
            };
            Controls.Add(scroller);
            ResumeLayout(true);
        }
        private Card BuildModeSection()
        {
            var card = new Card { Height = S(222), Padding = new Padding(S(20), S(18), S(20), S(18)), Margin = new Padding(S(0), S(0), S(0), S(16)) };
            var title = SectionTitle("Theme mode", "Light is easiest on the eyes in bright offices. Dark reduces glare at night. Match Windows follows your PC setting.");
            title.Dock = DockStyle.Top;
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, WrapContents = false, Padding = new Padding(S(0), S(6), S(0), S(0)) };
            foreach (var (mode, label) in new[] { (ThemeMode.Light, "Light"), (ThemeMode.Dark, "Dark"), (ThemeMode.System, "Match Windows") })
            {
                var oc = new OptionCard(label, g => DrawModeThumb(g, mode)) { Tag = mode, Size = new Size(S(150), S(118)), Margin = new Padding(S(0), S(0), S(12), S(0)) };
                oc.Click += (s, e) => { pendingMode = mode; SyncSelection(); };
                modeCards.Add(oc);
                row.Controls.Add(oc);
            }
            card.Controls.Add(row);
            card.Controls.Add(title);
            return card;
        }
        private Card BuildAccentSection()
        {
            var card = new Card { Height = S(218), Padding = new Padding(S(20), S(18), S(20), S(18)), Margin = new Padding(S(0), S(0), S(0), S(16)) };
            var title = SectionTitle("Accent colour", "Used for primary buttons, selections, links and highlights across every screen.");
            title.Dock = DockStyle.Top;
            var swatchRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(58), BackColor = AppTheme.Surface, WrapContents = false, Padding = new Padding(S(0), S(10), S(0), S(0)) };
            foreach (var p in ThemeManager.Presets)
            {
                var sw = new Swatch(p.Color, p.Name) { Margin = new Padding(S(0), S(0), S(8), S(0)) };
                sw.Click += (s, e) => { pendingAccent = p.Color; SyncSelection(); };
                swatches.Add(sw);
                swatchRow.Controls.Add(sw);
            }
            var customRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(46), BackColor = AppTheme.Surface, WrapContents = false, Padding = new Padding(S(0), S(6), S(0), S(0)) };
            var lblCustom = new Label { Text = "Custom", AutoSize = true, Margin = new Padding(S(0), S(9), S(10), S(0)), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary };
            pnlCustomChip = new Panel { Size = new Size(S(30), S(30)), Margin = new Padding(S(0), S(2), S(8), S(0)), BackColor = AppTheme.Surface, Cursor = Cursors.Hand };
            pnlCustomChip.Paint += (s, e) =>
            {
                Gfx.HighQuality(e.Graphics);
                using var b = new SolidBrush(pendingAccent);
                using var path = Gfx.RoundedRect(new RectangleF(1, 1, pnlCustomChip.Width - 3, pnlCustomChip.Height - 3), S(7));
                e.Graphics.FillPath(b, path);
                using var p = new Pen(AppTheme.BorderStrong);
                e.Graphics.DrawPath(p, path);
            };
            pnlCustomChip.Click += (s, e) => PickCustomColor();
            txtHex = new TextBox { Width = S(96), Margin = new Padding(S(0), S(4), S(8), S(0)), CharacterCasing = CharacterCasing.Upper, MaxLength = 7 };
            txtHex.TextChanged += (s, e) =>
            {
                if (updatingHex) return;
                if (ThemeManager.TryParseHex(txtHex.Text, out Color c)) { pendingAccent = c; SyncSelection(fromHex: true); }
            };
            var btnPick = new Button { Text = "Pick any colour…", Size = new Size(S(140), S(32)), Margin = new Padding(S(0), S(1), S(0), S(0)) };
            UIThemeHelper.SetButtonRole(btnPick, "secondary");
            btnPick.Click += (s, e) => PickCustomColor();
            customRow.Controls.Add(lblCustom);
            customRow.Controls.Add(pnlCustomChip);
            customRow.Controls.Add(txtHex);
            customRow.Controls.Add(btnPick);
            card.Controls.Add(customRow);
            card.Controls.Add(swatchRow);
            card.Controls.Add(title);
            return card;
        }
        private Card BuildSidebarSection()
        {
            var card = new Card { Height = S(222), Padding = new Padding(S(20), S(18), S(20), S(18)), Margin = new Padding(S(0), S(0), S(0), S(16)) };
            var title = SectionTitle("Sidebar", "Choose how the navigation menu on the left looks.");
            title.Dock = DockStyle.Top;
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, WrapContents = false, Padding = new Padding(S(0), S(6), S(0), S(0)) };
            foreach (var (style, label) in new[] { (SidebarStyle.Charcoal, "Charcoal"), (SidebarStyle.Light, "Light"), (SidebarStyle.Accent, "Accent") })
            {
                var oc = new OptionCard(label, g => DrawSidebarThumb(g, style)) { Tag = style, Size = new Size(S(150), S(118)), Margin = new Padding(S(0), S(0), S(12), S(0)) };
                oc.Click += (s, e) => { pendingSidebar = style; SyncSelection(); };
                sidebarCards.Add(oc);
                row.Controls.Add(oc);
            }
            card.Controls.Add(row);
            card.Controls.Add(title);
            return card;
        }
        private static Control SectionTitle(string title, string subtitle)
        {
            var p = new Panel { Height = S(52), BackColor = AppTheme.Surface };
            var t = new Label { Text = title, AutoSize = true, Location = new Point(S(0), S(0)), Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary };
            var st = new Label { Text = subtitle, AutoSize = false, Location = new Point(S(1), S(24)), Height = S(24), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            p.Controls.Add(t);
            p.Controls.Add(st);
            p.Resize += (s, e) => st.Width = Math.Max(100, p.Width - 2);
            return p;
        }
        // ------------------------------------------------------------------ behaviour
        private void PickCustomColor()
        {
            using var dlg = new ColorDialog { Color = pendingAccent, FullOpen = true, AnyColor = true };
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                pendingAccent = dlg.Color;
                SyncSelection();
            }
        }
        private void SyncSelection(bool fromHex = false)
        {
            foreach (var c in modeCards) c.Selected = (ThemeMode)c.Tag! == pendingMode;
            foreach (var c in sidebarCards) c.Selected = (SidebarStyle)c.Tag! == pendingSidebar;
            foreach (var s in swatches) s.Selected = s.Color.ToArgb() == pendingAccent.ToArgb();
            if (!fromHex && txtHex != null)
            {
                updatingHex = true;
                txtHex.Text = ThemeManager.ToHex(pendingAccent);
                updatingHex = false;
            }
            pnlCustomChip?.Invalidate();
            if (preview != null)
            {
                preview.Snapshot = PreviewSnapshot.For(pendingMode, pendingAccent, pendingSidebar);
                preview.Invalidate();
            }
            foreach (var c in modeCards) c.Invalidate();
            foreach (var c in sidebarCards) c.Invalidate();
        }
        private void ApplyChoice()
        {
            bool changed = pendingMode != ThemeManager.Mode || pendingAccent.ToArgb() != ThemeManager.AccentColor.ToArgb() || pendingSidebar != ThemeManager.Sidebar;
            if (!changed)
            {
                ToastHelper.ShowToast(this, "This theme is already in use.", ToastType.Info);
                return;
            }
            ThemeManager.Apply(pendingMode, pendingAccent, pendingSidebar);
            try { Database.MJdatabase.LogSecurityEvent(SessionContext.ActiveUser?.Username ?? "", "THEME_CHANGED", $"Appearance set to {pendingMode}, accent {ThemeManager.ToHex(pendingAccent)}, sidebar {pendingSidebar}"); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            var main = MainForm.Instance;
            if (main != null) main.ReloadForTheme("Appearance");
        }
        // ------------------------------------------------------------------ thumbnails
        private void DrawModeThumb(Graphics g, ThemeMode mode)
        {
            var snap = PreviewSnapshot.For(mode, pendingAccent, pendingSidebar);
            DrawMiniApp(g, new Rectangle(0, 0, 134, 72), snap, simple: true);
        }
        private void DrawSidebarThumb(Graphics g, SidebarStyle style)
        {
            var snap = PreviewSnapshot.For(pendingMode, pendingAccent, style);
            DrawMiniApp(g, new Rectangle(0, 0, 134, 72), snap, simple: true);
        }
        internal static void DrawMiniApp(Graphics g, Rectangle r, PreviewSnapshot c, bool simple)
        {
            Gfx.HighQuality(g);
            using (var path = Gfx.RoundedRect(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), simple ? 6 : 10))
            {
                using var bg = new SolidBrush(c.Canvas);
                g.FillPath(bg, path);
                var state = g.Save();
                g.SetClip(path);
                int sbw = simple ? r.Width / 5 : r.Width / 5;
                using (var sb = new SolidBrush(c.Sidebar)) g.FillRectangle(sb, r.X, r.Y, sbw, r.Height);
                int hh = simple ? 12 : 34;
                using (var hb = new SolidBrush(c.Surface)) g.FillRectangle(hb, r.X + sbw, r.Y, r.Width - sbw, hh);
                using (var lp = new Pen(c.Border)) g.DrawLine(lp, r.X + sbw, r.Y + hh, r.Right, r.Y + hh);
                // nav items
                int ny = r.Y + (simple ? 14 : 52);
                for (int i = 0; i < 4; i++)
                {
                    var nr = new RectangleF(r.X + 4, ny + i * (simple ? 11 : 30), sbw - 8, simple ? 7 : 22);
                    if (i == 0)
                    {
                        using var ab = new SolidBrush(c.SidebarActive);
                        using var ap = Gfx.RoundedRect(nr, simple ? 2 : 6);
                        g.FillPath(ab, ap);
                        using var ib = new SolidBrush(c.SidebarIndicator);
                        g.FillRectangle(ib, nr.X - 2, nr.Y + 1, 2, nr.Height - 2);
                    }
                    using var tb = new SolidBrush(i == 0 ? c.SidebarText : Color.FromArgb(150, c.SidebarText));
                    g.FillRectangle(tb, nr.X + (simple ? 3 : 10), nr.Y + nr.Height / 2 - (simple ? 1 : 2), nr.Width * (simple ? 0.55f : 0.6f), simple ? 2 : 4);
                }
                // content card
                int pad = simple ? 6 : 16;
                var card = new RectangleF(r.X + sbw + pad, r.Y + hh + pad, r.Width - sbw - pad * 2, r.Height - hh - pad * 2);
                using (var cp = Gfx.RoundedRect(card, simple ? 3 : 8))
                using (var cb = new SolidBrush(c.Surface))
                using (var cbp = new Pen(c.Border))
                {
                    g.FillPath(cb, cp);
                    g.DrawPath(cbp, cp);
                }
                if (simple)
                {
                    using var t1 = new SolidBrush(c.Text);
                    g.FillRectangle(t1, card.X + 5, card.Y + 6, card.Width * 0.4f, 3);
                    using var t2 = new SolidBrush(c.Muted);
                    g.FillRectangle(t2, card.X + 5, card.Y + 13, card.Width * 0.6f, 2);
                    using var btn = new SolidBrush(c.Accent);
                    using var bp = Gfx.RoundedRect(new RectangleF(card.Right - 26, card.Bottom - 12, 21, 7), 2);
                    g.FillPath(btn, bp);
                }
                else
                {
                    using var titleFont = new Font("Segoe UI Semibold", 13F, GraphicsUnit.Pixel);
                    using var bodyFont = new Font("Segoe UI", 11F, GraphicsUnit.Pixel);
                    using var smallFont = new Font("Segoe UI Semibold", 10.5F, GraphicsUnit.Pixel);
                    DrawStr(g, "Dashboard", titleFont, new PointF((int)(r.X + sbw + 14), r.Y + 8), c.Text);
                    // chips in header
                    DrawChip(g, new RectangleF(r.Right - 150, r.Y + 8, 62, 18), c.WarningSoft, c.WarningText, "3 sched.", smallFont);
                    DrawChip(g, new RectangleF(r.Right - 82, r.Y + 8, 68, 18), c.InfoSoft, c.InfoText, "2 transit", smallFont);
                    DrawStr(g, "Today's deliveries", titleFont, new PointF((int)card.X + 12, (int)card.Y + 10), c.Text);
                    DrawStr(g, "Live overview of trucks and crew on the road.", bodyFont, new PointF((int)card.X + 12, (int)card.Y + 30), c.Muted);
                    // table rows
                    float ty = card.Y + 56;
                    using (var hb = new SolidBrush(c.Canvas)) g.FillRectangle(hb, card.X + 1, ty, card.Width - 2, 20);
                    DrawStr(g, "Dispatch #", smallFont, new PointF((int)card.X + 12, (int)ty + 3), c.Muted);
                    DrawStr(g, "Status", smallFont, new PointF((int)(card.Right - 110), (int)ty + 3), c.Muted);
                    string[] ids = { "DSP-1042", "DSP-1043", "DSP-1044" };
                    for (int i = 0; i < 3; i++)
                    {
                        float ry = ty + 22 + i * 26;
                        if (i == 1)
                        {
                            using var sel = new SolidBrush(c.Selection);
                            g.FillRectangle(sel, card.X + 1, ry, card.Width - 2, 26);
                        }
                        DrawStr(g, ids[i], bodyFont, new PointF((int)card.X + 12, (int)ry + 6), c.Text);
                        if (i == 0) DrawChip(g, new RectangleF(card.Right - 112, ry + 4, 78, 18), c.SuccessSoft, c.SuccessText, "Delivered", smallFont);
                        if (i == 1) DrawChip(g, new RectangleF(card.Right - 112, ry + 4, 78, 18), c.InfoSoft, c.InfoText, "In transit", smallFont);
                        if (i == 2) DrawChip(g, new RectangleF(card.Right - 112, ry + 4, 78, 18), c.WarningSoft, c.WarningText, "Pending", smallFont);
                        using var lp = new Pen(c.Border);
                        g.DrawLine(lp, card.X + 1, ry + 26, card.Right - 1, ry + 26);
                    }
                    // buttons
                    var b1 = new RectangleF(card.Right - 118, card.Bottom - 40, 104, 28);
                    using (var bp = Gfx.RoundedRect(b1, 7))
                    using (var bb = new SolidBrush(c.Accent)) g.FillPath(bb, bp);
                    DrawCentered(g, "+ New dispatch", smallFont, b1, c.OnAccent);
                    var b2 = new RectangleF(card.Right - 200, card.Bottom - 40, 74, 28);
                    using (var bp2 = Gfx.RoundedRect(b2, 7))
                    using (var bb2 = new SolidBrush(c.Surface))
                    using (var bpen = new Pen(c.BorderStrong))
                    {
                        g.FillPath(bb2, bp2);
                        g.DrawPath(bpen, bp2);
                    }
                    DrawCentered(g, "Export", smallFont, b2, c.Text);
                }
                g.Restore(state);
                using var outline = new Pen(c.Border);
                g.DrawPath(outline, path);
            }
        }
        private static void DrawChip(Graphics g, RectangleF r, Color bg, Color fg, string text, Font f)
        {
            using var p = Gfx.RoundedRect(r, r.Height / 2);
            using var b = new SolidBrush(bg);
            g.FillPath(b, p);
            DrawCentered(g, text, f, r, fg);
        }
        private static void DrawStr(Graphics g, string text, Font f, PointF p, Color c)
        {
            using var b = new SolidBrush(c);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(text, f, b, p);
        }
        private static void DrawCentered(Graphics g, string text, Font f, RectangleF r, Color c)
        {
            using var b = new SolidBrush(c);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(text, f, b, r, sf);
        }
        // ------------------------------------------------------------------ small controls
        internal sealed class PreviewSnapshot
        {
            public Color Canvas, Surface, Border, BorderStrong, Text, Muted, Accent, OnAccent, Selection, Sidebar, SidebarText, SidebarActive, SidebarIndicator;
            public Color SuccessSoft, SuccessText, InfoSoft, InfoText, WarningSoft, WarningText;
            public static PreviewSnapshot For(ThemeMode mode, Color accent, SidebarStyle sidebar)
            {
                var oldMode = ThemeManager.Mode;
                var oldAccent = ThemeManager.AccentColor;
                var oldSidebar = ThemeManager.Sidebar;
                ThemeManager.Apply(mode, accent, sidebar, persist: false, quiet: true);
                var s = new PreviewSnapshot
                {
                    Canvas = AppTheme.Canvas, Surface = AppTheme.Surface, Border = AppTheme.Border, BorderStrong = AppTheme.BorderStrong,
                    Text = AppTheme.TextPrimary, Muted = AppTheme.TextMuted, Accent = AppTheme.Accent, OnAccent = AppTheme.OnAccent,
                    Selection = AppTheme.GridSelection, Sidebar = AppTheme.Sidebar, SidebarText = AppTheme.SidebarText,
                    SidebarActive = AppTheme.SidebarActive, SidebarIndicator = AppTheme.SidebarIndicator,
                    SuccessSoft = AppTheme.SuccessSoft, SuccessText = AppTheme.SuccessText, InfoSoft = AppTheme.InfoSoft, InfoText = AppTheme.InfoText,
                    WarningSoft = AppTheme.WarningSoft, WarningText = AppTheme.WarningText
                };
                ThemeManager.Apply(oldMode, oldAccent, oldSidebar, persist: false, quiet: true);
                return s;
            }
        }
        private sealed class PreviewCanvas : Control
        {
            public PreviewSnapshot? Snapshot;
            public PreviewCanvas()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(AppTheme.Surface);
                if (Snapshot == null) return;
                e.Graphics.ScaleTransform(F, F);
                DrawMiniApp(e.Graphics, new Rectangle(0, 0, (int)((Width - 1) / F), (int)((Height - 1) / F)), Snapshot, simple: false);
            }
        }
        private sealed class Card : Panel
        {
            public Card()
            {
                BackColor = AppTheme.Surface;
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
        private sealed class OptionCard : Control
        {
            private readonly string label;
            private readonly Action<Graphics> drawThumb;
            private bool hover;
            private bool selected;
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool Selected { get => selected; set { if (selected != value) { selected = value; Invalidate(); } } }
            public OptionCard(string label, Action<Graphics> drawThumb)
            {
                this.label = label;
                this.drawThumb = drawThumb;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }
            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Surface);
                var r = new RectangleF(1, 1, Width - 3, Height - 3);
                using (var path = Gfx.RoundedRect(r, 10))
                {
                    if (selected)
                    {
                        using var fill = new SolidBrush(AppTheme.AccentSoft);
                        g.FillPath(fill, path);
                    }
                    else if (hover)
                    {
                        using var fill = new SolidBrush(AppTheme.SurfaceHover);
                        g.FillPath(fill, path);
                    }
                    using var pen = new Pen(selected ? AppTheme.Accent : AppTheme.Border, selected ? 2f : 1f);
                    g.DrawPath(pen, path);
                }
                var state = g.Save();
                g.TranslateTransform(S(8), S(8));
                g.ScaleTransform(F, F);
                drawThumb(g);
                g.Restore(state);
                using var f = new Font("Segoe UI Semibold", 9F);
                TextRenderer.DrawText(g, label, f, new Rectangle(S(10), Height - S(34), Width - S(40), S(26)), selected ? AppTheme.AccentText : AppTheme.TextPrimary, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                if (selected)
                {
                    var dot = new RectangleF(Width - S(28), Height - S(29), S(16), S(16));
                    using var db = new SolidBrush(AppTheme.Accent);
                    g.FillEllipse(db, dot);
                    using var cp = new Pen(AppTheme.OnAccent, 1.8f);
                    g.DrawLines(cp, new[] { new PointF(dot.X + 4.2f * F, dot.Y + 8.3f * F), new PointF(dot.X + 7f * F, dot.Y + 11f * F), new PointF(dot.X + 12f * F, dot.Y + 5.5f * F) });
                }
            }
        }
        private sealed class Swatch : Control
        {
            public Color Color { get; }
            private bool hover;
            private bool selected;
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool Selected { get => selected; set { if (selected != value) { selected = value; Invalidate(); } } }
            private readonly ToolTip tip = new ToolTip();
            public Swatch(Color color, string name)
            {
                Color = color;
                Size = new Size(S(38), S(38));
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                tip.SetToolTip(this, name);
            }
            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Surface);
                if (selected || hover)
                {
                    using var ring = new Pen(selected ? Color : AppTheme.BorderStrong, 2f);
                    g.DrawEllipse(ring, 2, 2, Width - 5, Height - 5);
                }
                using var b = new SolidBrush(Color);
                g.FillEllipse(b, S(7), S(7), Width - S(15), Height - S(15));
                if (selected)
                {
                    using var cp = new Pen(AppTheme.BestTextOn(Color), 2f);
                    float cx = Width / 2f, cy = Height / 2f;
                    g.DrawLines(cp, new[] { new PointF(cx - 5 * F, cy), new PointF(cx - 1.5f * F, cy + 3.5f * F), new PointF(cx + 5 * F, cy - 3.5f * F) });
                }
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) tip.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
