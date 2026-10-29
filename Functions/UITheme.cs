using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Typography and legacy colour names. Colours now follow the active theme (see AppTheme / ThemeManager).</summary>
    public static class UITheme
    {
        public static readonly Font TitleFont          = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
        public static readonly Font CardHeaderFont     = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold);
        public static readonly Font FieldLabelFont     = new Font("Segoe UI Semibold", 9F, FontStyle.Regular);
        public static readonly Font InputFont          = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font ButtonPrimaryFont   = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular);
        public static readonly Font ButtonSecondaryFont = new Font("Segoe UI Semibold", 9F, FontStyle.Regular);
        public static readonly Font GridCellFont       = new Font("Segoe UI", 9.25F, FontStyle.Regular);
        public static readonly Font GridHeaderFont     = new Font("Segoe UI Semibold", 8.75F, FontStyle.Regular);
        public static readonly Font HelperSubtextFont  = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public const int CompactRowHeight = 38;
        public const int CompactHeaderHeight = 40;
        public const int DrawerWidth = 480;
        public static Color CanvasBackground   => AppTheme.Canvas;
        public static Color SurfaceBackground  => AppTheme.Surface;
        public static Color TextPrimary        => AppTheme.TextPrimary;
        public static Color TextSecondary      => AppTheme.TextSecondary;
        public static Color TextMuted          => AppTheme.TextMuted;
        public static Color PrimaryAccent      => AppTheme.Accent;
        public static Color PrimaryHover       => AppTheme.AccentHover;
        public static Color PrimaryActive      => AppTheme.AccentPressed;
        public static Color BrandDark          => AppTheme.TextPrimary;
        public static Color SecondarySurface   => AppTheme.SurfaceHover;
        public static Color BorderDefault      => AppTheme.BorderStrong;
        public static Color BorderSubtle       => AppTheme.Border;
        public static Color SuccessGreen       => AppTheme.Success;
        public static Color SuccessHover       => Pal.C(4, 120, 87);
        public static Color DangerRed          => AppTheme.Danger;
        public static Color DangerHover        => AppTheme.DangerHover;
        public static Color SelectionHighlight => AppTheme.GridSelection;
        public static Color SelectionText      => AppTheme.TextPrimary;
    }
    public enum ToastType { Success, Info, Warning, Error }
    /// <summary>
    /// Shared fonts for code that runs on every paint (grid cell formatting, custom painting).
    /// Creating a new Font each time leaks GDI handles and makes scrolling and hovering sluggish.
    /// </summary>
    public static class FontCache
    {
        private static readonly Dictionary<(string, float, FontStyle), Font> fonts = new Dictionary<(string, float, FontStyle), Font>();
        public static Font Get(string family, float size, FontStyle style = FontStyle.Regular)
        {
            var key = (family, size, style);
            lock (fonts)
            {
                if (!fonts.TryGetValue(key, out Font? f))
                {
                    f = new Font(family, size, style);
                    fonts[key] = f;
                }
                return f;
            }
        }
        public static Font Get(Font prototype, FontStyle style) => Get(prototype.FontFamily.Name, prototype.Size, style);
    }
    public static class ToastHelper
    {
        /// <summary>While true (e.g. screens being prepared in the background), toasts are not shown.</summary>
        public static bool Suppressed { get; set; }
        public static void ShowToast(Control parent, string message, ToastType type = ToastType.Success)
        {
            if (Suppressed) return;
            try
            {
                Form? parentForm = parent is Form f ? f : parent.FindForm();
                if (parentForm == null || parentForm.IsDisposed || !parentForm.Visible) return;
                Color accent;
                string icon;
                switch (type)
                {
                    case ToastType.Info: accent = AppTheme.Info; icon = "i"; break;
                    case ToastType.Warning: accent = AppTheme.Warning; icon = "!"; break;
                    case ToastType.Error: accent = AppTheme.Danger; icon = "✕"; break;
                    default: accent = AppTheme.Success; icon = "✓"; break;
                }
                foreach (Control c in parentForm.Controls)
                {
                    if (c is ToastCard old) { old.Top -= old.Height + 10; }
                }
                var toast = new ToastCard(message, icon, accent);
                int targetTop = Math.Max(10, parentForm.ClientSize.Height - toast.Height - 22);
                toast.Location = new Point(Math.Max(10, parentForm.ClientSize.Width - toast.Width - 24), parentForm.ClientSize.Height);
                toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                parentForm.Controls.Add(toast);
                toast.BringToFront();
                var slide = new System.Windows.Forms.Timer { Interval = 15 };
                slide.Tick += (s, e) =>
                {
                    if (toast.IsDisposed) { slide.Stop(); slide.Dispose(); return; }
                    int step = Math.Max(2, (toast.Top - targetTop) / 3);
                    toast.Top = Math.Max(targetTop, toast.Top - step);
                    if (toast.Top <= targetTop) { slide.Stop(); slide.Dispose(); }
                };
                slide.Start();
                var tmr = new System.Windows.Forms.Timer { Interval = 3400 };
                void Dismiss()
                {
                    try
                    {
                        tmr.Stop();
                        tmr.Dispose();
                        if (!parentForm.IsDisposed && parentForm.Controls.Contains(toast))
                        {
                            parentForm.Controls.Remove(toast);
                        }
                        toast.Dispose();
                    }
                    catch { }
                }
                toast.Click += (s, e) => Dismiss();
                tmr.Tick += (s, e) => Dismiss();
                tmr.Start();
            }
            catch { }
        }
        private sealed class ToastCard : Control
        {
            private readonly string message;
            private readonly string icon;
            private readonly Color accent;
            private static readonly Font MsgFont = new Font("Segoe UI Semibold", 9.5F);
            private static readonly Font IconFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            public ToastCard(string message, string icon, Color accent)
            {
                this.message = message;
                this.icon = icon;
                this.accent = accent;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                Size textSize = TextRenderer.MeasureText(message, MsgFont, new Size(380, 0), TextFormatFlags.WordBreak);
                Size = new Size(Math.Min(460, textSize.Width + 82), Math.Max(54, textSize.Height + 30));
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                Color bg = AppTheme.IsDark ? Pal.C(241, 245, 249) : Color.FromArgb(28, 25, 23);
                Color fg = AppTheme.IsDark ? AppTheme.TextPrimary : Color.FromArgb(250, 250, 249);
                g.Clear(Parent?.BackColor ?? AppTheme.Canvas);
                var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
                using (var path = Gfx.RoundedRect(r, 10))
                using (var b = new SolidBrush(bg))
                using (var p = new Pen(Color.FromArgb(40, 0, 0, 0)))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                var circle = new RectangleF(16, (Height - 24) / 2f, 24, 24);
                using (var cb = new SolidBrush(accent)) g.FillEllipse(cb, circle);
                TextRenderer.DrawText(g, icon, IconFont, Rectangle.Round(circle), AppTheme.BestTextOn(accent),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                var textRect = new Rectangle(52, 0, Width - 66, Height);
                TextRenderer.DrawText(g, message, MsgFont, textRect, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
        }
    }
    public static class UIThemeHelper
    {
        private static readonly ConditionalWeakTable<Control, object> styled = new ConditionalWeakTable<Control, object>();
        private static bool MarkStyled(Control c)
        {
            if (styled.TryGetValue(c, out _)) return false;
            styled.Add(c, true);
            return true;
        }
        private static readonly System.Reflection.PropertyInfo? DoubleBufferedProp =
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly ConditionalWeakTable<Control, object> doubleBuffered = new ConditionalWeakTable<Control, object>();
        public static void EnableDoubleBuffering(Control c)
        {
            if (c == null) return;
            if (!doubleBuffered.TryGetValue(c, out _))
            {
                doubleBuffered.Add(c, true);
                // Native edit/list controls paint themselves; double-buffering them only adds work.
                if (!(c is TextBoxBase) && !(c is ComboBox) && !(c is ListBox) && !(c is DateTimePicker) && !(c is UpDownBase))
                {
                    try { DoubleBufferedProp?.SetValue(c, true, null); } catch { }
                }
            }
            foreach (Control child in c.Controls)
            {
                EnableDoubleBuffering(child);
            }
        }
        [ThreadStatic] private static int themeDepth;
        public static void ApplyTheme(Control parent)
        {
            if (parent == null) return;
            if (themeDepth == 0)
            {
                // One pass for the whole tree, with layout suspended so the window is laid out once instead of per change.
                EnableDoubleBuffering(parent);
                parent.SuspendLayout();
                themeDepth++;
                try { ApplyThemeCore(parent); }
                finally
                {
                    themeDepth--;
                    parent.ResumeLayout(true);
                }
                try { ConfirmGate.Watch(parent); } catch { }
                return;
            }
            ApplyThemeCore(parent);
            try { ConfirmGate.Watch(parent); } catch { }
        }
        private static void ApplyThemeCore(Control parent)
        {
            bool first = MarkStyled(parent);
            if (parent is Form form)
            {
                if (first)
                {
                    form.BackColor = AppTheme.Canvas;
                    ThemeManager.StyleWindowChrome(form);
                }
            }
            else if (parent is UserControl uc)
            {
                if (first) uc.BackColor = AppTheme.Canvas;
                uc.AutoScroll = false;
                uc.AutoScrollMinSize = Size.Empty;
            }
            if (first) WatchForNewChildren(parent);
            foreach (Control ctrl in parent.Controls)
            {
                if (MarkStyled(ctrl))
                {
                    ApplyControlStyle(ctrl);
                    if (ctrl is Panel || ctrl is GroupBox || ctrl is TableLayoutPanel || ctrl is FlowLayoutPanel || ctrl is UserControl) WatchForNewChildren(ctrl);
                }
                if (ctrl.HasChildren && !(ctrl is DataGridView) && !(ctrl is NumericUpDown) && !(ctrl is ComboBox))
                {
                    ApplyThemeCore(ctrl);
                }
            }
            FixContrast(parent);
        }
        /// <summary>Controls created later at run time (cards, dialog rows, toolbars) get the modern buttons, inputs and grids too.</summary>
        private static void WatchForNewChildren(Control container)
        {
            container.ControlAdded += (s, e) =>
            {
                try
                {
                    if (e.Control == null) return;
                    StyleDynamicTree(e.Control);
                }
                catch { }
            };
        }
        private static void StyleDynamicTree(Control c)
        {
            if (MarkStyled(c))
            {
                if (c is Button b) StyleButtonChrome(b);
                else if (c is TextBox || c is ComboBox || c is NumericUpDown || c is DateTimePicker) StyleInput(c);
                else if (c is DataGridView g) ApplyGridTheme(g);
                else if (c is CheckBox || c is RadioButton) c.Cursor = Cursors.Hand;
                if (c is Panel || c is GroupBox || c is UserControl) WatchForNewChildren(c);
                if (ThemeManager.IsDark && ((c is ScrollableControl sc && sc.AutoScroll) || c is ListBox)) ThemeManager.StyleNativeControl(c);
            }
            if (c is DataGridView || c is NumericUpDown || c is ComboBox) return;
            foreach (Control child in c.Controls) StyleDynamicTree(child);
        }
        public static Color EffectiveBack(Control? c)
        {
            while (c != null)
            {
                if (c.BackColor.A == 255) return c.BackColor;
                c = c.Parent;
            }
            return AppTheme.Canvas;
        }
        /// <summary>Guarantees readable text: any label/checkbox whose colour is too close to its background is corrected.</summary>
        private static void FixContrast(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if ((c is Label || c is CheckBox || c is RadioButton) && !(c is LinkLabel))
                {
                    Color back = EffectiveBack(c);
                    if (Oklch.Contrast(c.ForeColor, back) < 2.4)
                    {
                        c.ForeColor = Oklch.Contrast(AppTheme.TextPrimary, back) >= 4.5 ? AppTheme.TextPrimary : AppTheme.BestTextOn(back);
                    }
                }
            }
        }
        private static void ApplyControlStyle(Control ctrl)
        {
            if (ctrl is TextBox txt)
            {
                txt.Font = UITheme.InputFont;
                txt.ForeColor = AppTheme.TextPrimary;
                if (!txt.ReadOnly || txt.BackColor == Pal.White) txt.BackColor = AppTheme.InputBack;
                StyleInput(txt);
            }
            else if (ctrl is ComboBox cb)
            {
                cb.Font = UITheme.InputFont;
                cb.ForeColor = AppTheme.TextPrimary;
                cb.BackColor = AppTheme.InputBack;
                if (cb.Name.Contains("Time", StringComparison.OrdinalIgnoreCase) && (cb.Items.Count == 0 || cb.Items.Count <= 7))
                {
                    string currentVal = cb.Text;
                    PopulateTimeSlots(cb, 30, false, string.IsNullOrEmpty(currentVal) ? "06:00 AM" : currentVal);
                }
                StyleInput(cb);
            }
            else if (ctrl is DateTimePicker dtp)
            {
                if (dtp.Name.Contains("Time", StringComparison.OrdinalIgnoreCase))
                {
                    ConfigureTimePicker(dtp, false);
                }
                else
                {
                    dtp.Font = UITheme.InputFont;
                    dtp.CalendarFont = UITheme.InputFont;
                    dtp.CalendarForeColor = AppTheme.TextPrimary;
                    dtp.CalendarMonthBackground = AppTheme.InputBack;
                    dtp.CalendarTitleBackColor = AppTheme.Accent;
                    dtp.CalendarTitleForeColor = AppTheme.OnAccent;
                }
                StyleInput(dtp);
            }
            else if (ctrl is NumericUpDown num)
            {
                num.Font = UITheme.InputFont;
                num.ForeColor = AppTheme.TextPrimary;
                num.BackColor = AppTheme.InputBack;
                num.BorderStyle = BorderStyle.FixedSingle;
                if (num.Name.Contains("Amount") || num.Name.Contains("Paid") || num.Name.Contains("Cost") || num.Name.Contains("Wage") || num.Name.Contains("Allowance") || num.Name.Contains("Rate") || num.Name.Contains("Deduction"))
                {
                    if (num.Maximum < 100000000m) num.Maximum = 100000000m;
                }
                else
                {
                    if (num.Maximum < 1000000m) num.Maximum = 1000000m;
                }
                StyleInput(num);
            }
            else if (ctrl is CheckBox chk)
            {
                chk.Font = UITheme.FieldLabelFont;
                chk.ForeColor = AppTheme.TextSecondary;
                chk.Cursor = Cursors.Hand;
                chk.AutoSize = true;
            }
            else if (ctrl is RadioButton rb)
            {
                rb.Font = UITheme.FieldLabelFont;
                rb.ForeColor = AppTheme.TextSecondary;
                rb.Cursor = Cursors.Hand;
                rb.AutoSize = true;
            }
            else if (ctrl is Button btn)
            {
                ApplyButtonStyle(btn);
            }
            else if (ctrl is LinkLabel link)
            {
                link.LinkColor = AppTheme.AccentText;
                link.ActiveLinkColor = AppTheme.AccentHover;
                link.VisitedLinkColor = AppTheme.AccentText;
                link.LinkBehavior = LinkBehavior.HoverUnderline;
            }
            else if (ctrl is Label lbl)
            {
                ApplyLabelStyle(lbl);
            }
            else if (ctrl is DataGridView grid)
            {
                ApplyGridTheme(grid);
            }
            else if (ctrl is CheckedListBox clb)
            {
                clb.Font = UITheme.InputFont;
                clb.ForeColor = AppTheme.TextPrimary;
                clb.BackColor = AppTheme.InputBack;
                clb.BorderStyle = BorderStyle.FixedSingle;
                if (ThemeManager.IsDark) ThemeManager.StyleNativeControl(clb);
            }
            else if (ctrl is ListBox lb)
            {
                lb.ForeColor = AppTheme.TextPrimary;
                lb.BackColor = AppTheme.InputBack;
                if (ThemeManager.IsDark) ThemeManager.StyleNativeControl(lb);
            }
            else if (ctrl is Panel pnl)
            {
                if (pnl.Name.Equals("pnlFormBody", StringComparison.OrdinalIgnoreCase))
                {
                    pnl.BackColor = AppTheme.Surface;
                    pnl.AutoScroll = true;
                    pnl.AutoScrollMargin = new Size(0, 24);
                }
                else if (pnl.Name.Equals("pnlFormActionsBar", StringComparison.OrdinalIgnoreCase))
                {
                    pnl.BackColor = AppTheme.Surface;
                    pnl.AutoScroll = false;
                    if (pnl.Height < 82) pnl.Height = 82;
                    AddTopRule(pnl);
                }
                else if (pnl.Name.Equals("pnlForm", StringComparison.OrdinalIgnoreCase) ||
                         pnl.Name.Equals("pnlFormHeaderBar", StringComparison.OrdinalIgnoreCase))
                {
                    pnl.BackColor = AppTheme.Surface;
                    pnl.AutoScroll = false;
                    if (pnl.Name.Equals("pnlFormHeaderBar", StringComparison.OrdinalIgnoreCase)) AddBottomRule(pnl);
                }
                else if (pnl.Name.StartsWith("pnlGridCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlDispatchCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlLoadingCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlFilter", StringComparison.OrdinalIgnoreCase))
                {
                    pnl.BackColor = AppTheme.Surface;
                    AddCardBorder(pnl);
                }
                if (ThemeManager.IsDark && pnl.AutoScroll) ThemeManager.StyleNativeControl(pnl);
            }
            else if (ctrl is GroupBox gb)
            {
                gb.Font = UITheme.CardHeaderFont;
                gb.ForeColor = AppTheme.TextPrimary;
                gb.BackColor = AppTheme.Surface;
            }
            else if (ctrl is ScrollableControl scr)
            {
                if (ThemeManager.IsDark && scr.AutoScroll) ThemeManager.StyleNativeControl(ctrl);
            }
        }
        private static void AddCardBorder(Panel pnl)
        {
            if (pnl.BorderStyle != BorderStyle.None) pnl.BorderStyle = BorderStyle.None;
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.ClientSize.Width - 1, pnl.ClientSize.Height - 1);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
        }
        private static void AddTopRule(Panel pnl)
        {
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawLine(pen, 0, 0, pnl.ClientSize.Width, 0);
            };
        }
        private static void AddBottomRule(Panel pnl)
        {
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawLine(pen, 0, pnl.ClientSize.Height - 1, pnl.ClientSize.Width, pnl.ClientSize.Height - 1);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
        }
        private static void ApplyLabelStyle(Label lbl)
        {
            string name = lbl.Name;
            lbl.UseMnemonic = false;
            // Labels built in code (no designer name) already carry deliberate fonts and colours.
            if (string.IsNullOrEmpty(name)) return;
            if (name.Equals("lblFormHeader", StringComparison.OrdinalIgnoreCase))
            {
                lbl.AutoSize = false;
                lbl.AutoEllipsis = true;
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular);
                lbl.ForeColor = AppTheme.TextPrimary;
                return;
            }
            lbl.AutoSize = true;
            if (name.StartsWith("lblTitle", StringComparison.OrdinalIgnoreCase) ||
                (lbl.Font.Size >= 15 && name.Contains("Title", StringComparison.OrdinalIgnoreCase)))
            {
                lbl.Font = UITheme.TitleFont;
                lbl.ForeColor = AppTheme.TextPrimary;
            }
            else if (name.Equals("lblGridHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("lblCardHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("lblHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("lblFormTitle", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.CardHeaderFont;
                lbl.ForeColor = AppTheme.TextPrimary;
            }
            else if (name.StartsWith("lblSub", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Desc", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Note", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Hint", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.HelperSubtextFont;
                lbl.ForeColor = AppTheme.TextMuted;
            }
            else if (name.StartsWith("lblError", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.HelperSubtextFont;
                lbl.ForeColor = AppTheme.Danger;
            }
            else
            {
                lbl.Font = UITheme.FieldLabelFont;
                lbl.ForeColor = AppTheme.TextSecondary;
            }
        }
        // ------------------------------------------------------------------ Buttons
        private enum ButtonRole { Keep, Primary, Strong, Danger, Secondary }
        private static ButtonRole RoleFor(Button btn)
        {
            string text = btn.Text.ToUpperInvariant();
            string name = btn.Name.ToUpperInvariant();
            if (text.Contains("DELETE") || text.Contains("REMOVE") || text.Contains("VOID") || name.Contains("DELETE")) return ButtonRole.Danger;
            if (text.Contains("CLEAR") || text.Contains("CANCEL") || text.Contains("EXIT") || text.Contains("RESET") || name.Contains("CLEAR") || name.Contains("EXIT") || name.Contains("CANCEL")) return ButtonRole.Secondary;
            if (text.Contains("UPDATE") || text.Contains("EDIT") || name.Contains("UPDATE")) return ButtonRole.Strong;
            if (text.Contains("PRINT") || name.Contains("PRINT") || text.Contains("EXPORT")) return ButtonRole.Secondary;
            if (text.Contains("ADD") || text.Contains("SAVE") || text.Contains("FILE") || text.Contains("CREATE") || text.Contains("NEW ") || text.Contains("GENERATE") ||
                text.Contains("RECORD") || text.Contains("SUBMIT") || text.Contains("CONFIRM") || text.Contains("APPLY") || text.Contains("PROCEED") ||
                name.Contains("SAVE") || name.Contains("ADD")) return ButtonRole.Primary;
            return ButtonRole.Keep;
        }
        public static void SetButtonRole(Button btn, string role)
        {
            switch (role.ToLowerInvariant())
            {
                case "primary": ColorButton(btn, ButtonRole.Primary); break;
                case "strong": ColorButton(btn, ButtonRole.Strong); break;
                case "danger": ColorButton(btn, ButtonRole.Danger); break;
                default: ColorButton(btn, ButtonRole.Secondary); break;
            }
            StyleButtonChrome(btn);
            btn.Invalidate();
        }
        private static void ColorButton(Button btn, ButtonRole role)
        {
            btn.FlatStyle = FlatStyle.Flat;
            switch (role)
            {
                case ButtonRole.Primary:
                    btn.BackColor = AppTheme.Accent;
                    btn.ForeColor = AppTheme.OnAccent;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.AccentHover;
                    btn.FlatAppearance.MouseDownBackColor = AppTheme.AccentPressed;
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonRole.Strong:
                    btn.BackColor = AppTheme.TextPrimary;
                    btn.ForeColor = AppTheme.Surface;
                    btn.FlatAppearance.MouseOverBackColor = ThemeManager.Mix(AppTheme.TextPrimary, AppTheme.Surface, 0.14);
                    btn.FlatAppearance.MouseDownBackColor = ThemeManager.Mix(AppTheme.TextPrimary, AppTheme.Surface, 0.24);
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonRole.Danger:
                    btn.BackColor = AppTheme.DangerSoft;
                    btn.ForeColor = AppTheme.DangerText;
                    btn.FlatAppearance.MouseOverBackColor = ThemeManager.Mix(AppTheme.DangerSoft, AppTheme.Danger, 0.16);
                    btn.FlatAppearance.MouseDownBackColor = ThemeManager.Mix(AppTheme.DangerSoft, AppTheme.Danger, 0.28);
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonRole.Secondary:
                    btn.BackColor = AppTheme.Surface;
                    btn.ForeColor = AppTheme.TextSecondary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
                    btn.FlatAppearance.MouseDownBackColor = AppTheme.Border;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = AppTheme.BorderStrong;
                    break;
            }
        }
        private static void ApplyButtonStyle(Button btn)
        {
            btn.UseMnemonic = false;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Cursor = Cursors.Hand;
            if (btn.Name.Contains("Close", StringComparison.OrdinalIgnoreCase) || btn.Text.Contains("Close", StringComparison.OrdinalIgnoreCase))
            {
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Regular);
                btn.AutoSize = true;
                btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                btn.MinimumSize = new Size(68, 28);
                btn.Padding = new Padding(6, 0, 6, 0);
                StyleButtonChrome(btn);
                return;
            }
            if (btn.Name.Contains("Photo", StringComparison.OrdinalIgnoreCase) || btn.Name.Contains("TimePicker", StringComparison.OrdinalIgnoreCase))
            {
                btn.Font = UITheme.ButtonSecondaryFont;
                if (btn.Height < 30) btn.Height = 30;
                StyleButtonChrome(btn);
                return;
            }
            if (btn.Name.Equals("btnClearSearch", StringComparison.OrdinalIgnoreCase))
            {
                btn.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                if (btn.Width < 36) btn.Width = 36;
                if (btn.Height < 30) btn.Height = 30;
                btn.BackColor = AppTheme.SurfaceHover;
                btn.ForeColor = AppTheme.TextMuted;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = AppTheme.Border;
                StyleButtonChrome(btn);
                return;
            }
            btn.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular);
            if (btn.Parent != null && btn.Parent.Name.Equals("pnlFormActionsBar", StringComparison.OrdinalIgnoreCase))
            {
                btn.Height = 34;
            }
            else if (btn.Height < 34 && btn.Dock == DockStyle.None && !btn.AutoSize)
            {
                btn.Height = 34;
            }
            var role = RoleFor(btn);
            if (role != ButtonRole.Keep) ColorButton(btn, role);
            else btn.FlatAppearance.BorderSize = 0;
            StyleButtonChrome(btn);
        }
        private sealed class ButtonVisualState { public bool Hover; public bool Down; }
        private static readonly ConditionalWeakTable<Button, ButtonVisualState> buttonStates = new ConditionalWeakTable<Button, ButtonVisualState>();
        private static readonly ConditionalWeakTable<Control, object> customPaint = new ConditionalWeakTable<Control, object>();
        /// <summary>Opt a control out of the automatic rounded-button painting (for controls that draw themselves).</summary>
        public static void ExcludeFromChrome(Control c)
        {
            if (!customPaint.TryGetValue(c, out _)) customPaint.Add(c, true);
        }
        /// <summary>Rounded, anti-aliased painting with hover / pressed / disabled states for every flat button.</summary>
        public static void StyleButtonChrome(Button btn)
        {
            if (btn == null || customPaint.TryGetValue(btn, out _)) return;
            if (buttonStates.TryGetValue(btn, out _)) return;
            if (HasPaintHandlers(btn)) return;
            if (btn.FlatStyle != FlatStyle.Flat) btn.FlatStyle = FlatStyle.Flat;
            btn.UseMnemonic = false;
            var st = new ButtonVisualState();
            buttonStates.Add(btn, st);
            btn.MouseEnter += (s, e) => { st.Hover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { st.Hover = false; st.Down = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { st.Down = true; btn.Invalidate(); } };
            btn.MouseUp += (s, e) => { st.Down = false; btn.Invalidate(); };
            btn.EnabledChanged += (s, e) => btn.Invalidate();
            btn.Paint += (s, e) => PaintButton(btn, st, e.Graphics);
            FitButtonToText(btn);
            btn.TextChanged += (s, e) => FitButtonToText(btn);
            btn.FontChanged += (s, e) => FitButtonToText(btn);
            btn.HandleCreated += (s, e) => FitButtonToText(btn);
            btn.ParentChanged += (s, e) => FitButtonToText(btn);
        }
        /// <summary>
        /// Buttons laid out in a row (flow panels) with a fixed width written for 100% scaling cut their text off on
        /// larger text sizes ("Print Loading She…"). Widen them to fit their text; never shrink, never touch docked buttons.
        /// </summary>
        public static void FitButtonToText(Button btn)
        {
            try
            {
                if (btn.AutoSize || btn.Dock != DockStyle.None || btn.Parent is not FlowLayoutPanel || string.IsNullOrEmpty(btn.Text)) return;
                float k = btn.DeviceDpi / 96f;
                int need = TextRenderer.MeasureText(btn.Text, btn.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + (int)Math.Round(26 * k)
                           + (btn.Image != null ? btn.Image.Width + (int)Math.Round(6 * k) : 0);
                if (btn.Width < need) btn.Width = need;
                int needH = TextRenderer.MeasureText("Ag", btn.Font).Height + (int)Math.Round(10 * k);
                if (btn.Height < needH) btn.Height = needH;
            }
            catch { }
        }
        private static readonly System.Reflection.PropertyInfo? EventsProp =
            typeof(System.ComponentModel.Component).GetProperty("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly object? PaintEventKey =
            (typeof(Control).GetField("s_paintEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
             ?? typeof(Control).GetField("EventPaint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))?.GetValue(null);
        private static bool HasPaintHandlers(Control c)
        {
            try
            {
                if (EventsProp == null || PaintEventKey == null) return false;
                var list = EventsProp.GetValue(c) as System.ComponentModel.EventHandlerList;
                return list?[PaintEventKey] != null;
            }
            catch { return false; }
        }
        private static void PaintButton(Button btn, ButtonVisualState st, Graphics g)
        {
            Gfx.HighQuality(g);
            Color parentBg = EffectiveBack(btn.Parent);
            g.Clear(parentBg);
            Color bg = btn.BackColor.A == 0 ? parentBg : btn.BackColor;
            var fa = btn.FlatAppearance;
            if (btn.Enabled && st.Down)
                bg = !fa.MouseDownBackColor.IsEmpty && fa.MouseDownBackColor.A > 0 ? fa.MouseDownBackColor : Shift(bg, 0.10);
            else if (btn.Enabled && st.Hover)
                bg = !fa.MouseOverBackColor.IsEmpty && fa.MouseOverBackColor.A > 0 ? fa.MouseOverBackColor : Shift(bg, 0.06);
            Color fg = btn.ForeColor;
            if (Oklch.Contrast(fg, bg) < 2.5) fg = AppTheme.BestTextOn(bg);
            if (!btn.Enabled)
            {
                bg = ThemeManager.Mix(bg, parentBg, 0.45);
                fg = ThemeManager.Mix(fg, bg, 0.5);
            }
            var rect = new RectangleF(0.5f, 0.5f, btn.Width - 1.5f, btn.Height - 1.5f);
            float radius = Math.Min(8f, Math.Min(rect.Height, rect.Width) / 2f);
            using (var path = Gfx.RoundedRect(rect, radius))
            {
                if (bg != parentBg || st.Hover)
                {
                    using var b = new SolidBrush(bg);
                    g.FillPath(b, path);
                }
                Color? border = null;
                if (fa.BorderSize > 0 && !fa.BorderColor.IsEmpty && fa.BorderColor.A > 0) border = fa.BorderColor;
                else if (fa.BorderSize > 0 && Oklch.Contrast(bg, parentBg) < 1.12) border = AppTheme.BorderStrong;
                if (border.HasValue)
                {
                    using var p = new Pen(border.Value, 1f);
                    g.DrawPath(p, path);
                }
            }
            bool leftAligned = btn.TextAlign == ContentAlignment.MiddleLeft || btn.TextAlign == ContentAlignment.TopLeft || btn.TextAlign == ContentAlignment.BottomLeft;
            int inset = leftAligned ? Math.Max(10, btn.Padding.Left) : 6;
            var content = new Rectangle(inset, 0, Math.Max(0, btn.Width - inset - (leftAligned ? 8 : 6)), btn.Height);
            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            switch (btn.TextAlign)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.TopLeft:
                case ContentAlignment.BottomLeft:
                    flags |= TextFormatFlags.Left; break;
                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                case ContentAlignment.BottomRight:
                    flags |= TextFormatFlags.Right; break;
                default:
                    flags |= TextFormatFlags.HorizontalCenter; break;
            }
            if (btn.Image == null && !string.IsNullOrEmpty(btn.Text))
            {
                int need = TextRenderer.MeasureText(g, btn.Text, btn.Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                if (need > content.Width)
                {
                    content = new Rectangle(2, 0, btn.Width - 4, btn.Height);
                    if (need > content.Width) flags &= ~TextFormatFlags.EndEllipsis;
                    flags = (flags & ~(TextFormatFlags.Left | TextFormatFlags.Right)) | TextFormatFlags.HorizontalCenter;
                }
            }
            if (btn.Image != null)
            {
                var img = btn.Image;
                Size textSize = TextRenderer.MeasureText(g, btn.Text, btn.Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                int gap = 5;
                int total = img.Width + (string.IsNullOrEmpty(btn.Text) ? 0 : gap + textSize.Width);
                int x = (flags & TextFormatFlags.Right) != 0 ? content.Right - total : (leftAligned ? content.Left : (btn.Width - total) / 2);
                if (x + total > btn.Width - 4 || x < 4) x = Math.Max(4, (btn.Width - total) / 2);
                int y = (btn.Height - img.Height) / 2;
                if (btn.Enabled) g.DrawImage(img, x, y, img.Width, img.Height);
                else ControlPaint.DrawImageDisabled(g, img, x, y, bg);
                if (!string.IsNullOrEmpty(btn.Text))
                {
                    var tr = new Rectangle(x + img.Width + gap, 0, Math.Max(0, btn.Width - (x + img.Width + gap) - 2), btn.Height);
                    TextRenderer.DrawText(g, btn.Text, btn.Font, tr, fg, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                }
            }
            else
            {
                TextRenderer.DrawText(g, btn.Text, btn.Font, content, fg, flags);
            }
        }
        private static Color Shift(Color c, double amount)
        {
            var (L, C, h) = Oklch.FromColor(c);
            double target = L > 0.6 ? L - amount : L + amount * 0.9;
            return Oklch.ToColor(target, C, h);
        }
        // ------------------------------------------------------------------ Inputs
        private static readonly ConditionalWeakTable<Control, InputFrame> frames = new ConditionalWeakTable<Control, InputFrame>();
        public static void StyleInput(Control c)
        {
            if (c == null || frames.TryGetValue(c, out _)) return;
            if (c.Parent is DataGridView) return;
            if (c is TextBox tb && tb.BorderStyle == BorderStyle.None) return;
            if (c is TextBox tb2 && tb2.BorderStyle == BorderStyle.Fixed3D) tb2.BorderStyle = BorderStyle.FixedSingle;
            if (c is NumericUpDown nud && nud.BorderStyle == BorderStyle.Fixed3D) nud.BorderStyle = BorderStyle.FixedSingle;
            if (c is ComboBox cb)
            {
                if (cb.DrawMode == DrawMode.Normal) cb.FlatStyle = FlatStyle.Flat;
                if (ThemeManager.IsDark) ThemeManager.StyleNativeControl(cb);
            }
            if (c is TextBox t && t.Multiline && ThemeManager.IsDark) ThemeManager.StyleNativeControl(t);
            if (c is TextBox t2 && !t2.ReadOnly && t2.BackColor != AppTheme.InputBack && Oklch.Contrast(t2.BackColor, AppTheme.InputBack) < 1.05) t2.BackColor = AppTheme.InputBack;
            frames.Add(c, new InputFrame(c));
        }
        /// <summary>Repaints the 1px frame of text boxes, combos, number and date fields in theme colours (accent when focused).</summary>
        private sealed class InputFrame : NativeWindow
        {
            private readonly Control owner;
            private bool hover;
            [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
            [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
            [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
            private const int WM_PAINT = 0x000F, WM_NCPAINT = 0x0085, WM_ENABLE = 0x000A;
            public InputFrame(Control owner)
            {
                this.owner = owner;
                if (owner.IsHandleCreated) AssignHandle(owner.Handle);
                owner.HandleCreated += (s, e) => { try { if (Handle != IntPtr.Zero) ReleaseHandle(); AssignHandle(owner.Handle); } catch { } };
                owner.HandleDestroyed += (s, e) => { try { ReleaseHandle(); } catch { } };
                owner.Enter += (s, e) => Redraw();
                owner.Leave += (s, e) => Redraw();
                owner.MouseEnter += (s, e) => { hover = true; Redraw(); };
                owner.MouseLeave += (s, e) => { hover = false; Redraw(); };
                owner.EnabledChanged += (s, e) => Redraw();
            }
            private void Redraw()
            {
                if (!owner.IsHandleCreated) return;
                RedrawWindow(owner.Handle, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0400); // INVALIDATE | FRAME (painted on the next normal paint)
            }
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                bool relevant = owner is TextBoxBase ? (m.Msg == WM_NCPAINT || m.Msg == WM_ENABLE) : (m.Msg == WM_PAINT || m.Msg == WM_NCPAINT || m.Msg == WM_ENABLE);
                if (relevant)
                {
                    try { DrawFrame(); } catch { }
                }
            }
            private void DrawFrame()
            {
                if (Handle == IntPtr.Zero) return;
                IntPtr hdc = GetWindowDC(Handle);
                if (hdc == IntPtr.Zero) return;
                try
                {
                    GetWindowRect(Handle, out RECT wr);
                    int w = wr.Right - wr.Left, h = wr.Bottom - wr.Top;
                    if (w <= 2 || h <= 2) return;
                    using var g = Graphics.FromHdc(hdc);
                    bool focused = owner.ContainsFocus;
                    Color back = owner.BackColor;
                    Color border = focused ? AppTheme.Accent : hover ? AppTheme.TextMuted : AppTheme.BorderStrong;
                    if (!owner.Enabled) border = AppTheme.Border;
                    if (owner is ComboBox cb && cb.DrawMode == DrawMode.Normal && owner.Enabled)
                    {
                        using (var pb = new Pen(back, 2)) g.DrawRectangle(pb, 1, 1, w - 3, h - 3);
                        int arrowW = SystemInformation.VerticalScrollBarWidth + 2;
                        var arrowRect = new Rectangle(w - arrowW - 1, 1, arrowW, h - 2);
                        using (var ab = new SolidBrush(back)) g.FillRectangle(ab, arrowRect);
                        int cx = arrowRect.Left + arrowRect.Width / 2, cy = h / 2;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var ap = new Pen(AppTheme.TextMuted, 1.6f))
                        {
                            g.DrawLines(ap, new[] { new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2) });
                        }
                        g.SmoothingMode = SmoothingMode.None;
                    }
                    using (var p = new Pen(border, 1)) g.DrawRectangle(p, 0, 0, w - 1, h - 1);
                }
                finally { ReleaseDC(Handle, hdc); }
            }
        }
        // ------------------------------------------------------------------ Grids
        private static readonly ConditionalWeakTable<DataGridView, object> themedGrids = new ConditionalWeakTable<DataGridView, object>();
        public static void ApplyGridTheme(DataGridView grid)
        {
            bool firstTime = !themedGrids.TryGetValue(grid, out _);
            if (firstTime) themedGrids.Add(grid, true);
            grid.EnableHeadersVisualStyles = false;
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, grid, new object[] { true });
            }
            catch { }
            grid.BackgroundColor = AppTheme.Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.GridColor = AppTheme.GridLine;
            grid.ShowCellToolTips = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.ScrollBars = ScrollBars.Both;
            grid.AllowUserToResizeColumns = true;
            if (grid.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill)
            {
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                if (grid.Columns.Count > 0)
                {
                    DataGridViewColumn? primaryCol = null;
                    foreach (DataGridViewColumn col in grid.Columns)
                    {
                        string h = col.HeaderText?.ToLowerInvariant() ?? "";
                        string p = col.DataPropertyName?.ToLowerInvariant() ?? "";
                        if (h.Contains("destination") || p.Contains("destination") ||
                            h.Contains("client") || p.Contains("client") ||
                            h.Contains("remarks") || p.Contains("remarks") ||
                            h.Contains("fullname") || p.Contains("fullname") ||
                            (h.Contains("name") && !h.Contains("user")) ||
                            h.Contains("cause") || p.Contains("cause") ||
                            h.Contains("route") || p.Contains("route"))
                        {
                            primaryCol = col;
                            break;
                        }
                    }
                    if (primaryCol != null)
                    {
                        if (primaryCol.MinimumWidth < 150) primaryCol.MinimumWidth = 150;
                        primaryCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                }
            }
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            float dpi = grid.DeviceDpi / 96f;
            int rowH = (int)Math.Round(UITheme.CompactRowHeight * Math.Max(1f, dpi * 0.9f));
            grid.ColumnHeadersHeight = (int)Math.Round(UITheme.CompactHeaderHeight * Math.Max(1f, dpi * 0.9f));
            grid.RowTemplate.Height = rowH;
            if (grid.Rows.Count > 0 && grid.Rows.Count <= 150)
            {
                foreach (DataGridViewRow row in grid.Rows) { try { if (row.Height < rowH) row.Height = rowH; } catch { } }
            }
            grid.ColumnHeadersDefaultCellStyle.Font = UITheme.GridHeaderFont;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.GridHeaderBack;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.GridHeaderText;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.GridHeaderBack;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.GridHeaderText;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 8, 0);
            grid.DefaultCellStyle.Font = UITheme.GridCellFont;
            grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            grid.DefaultCellStyle.BackColor = AppTheme.Surface;
            grid.DefaultCellStyle.SelectionBackColor = AppTheme.GridSelection;
            grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 8, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.IsDark ? ThemeManager.Mix(AppTheme.Surface, Color.White, 0.015) : Color.FromArgb(252, 251, 250);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AppTheme.GridSelection;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(10, 0, 8, 0);
            if (ThemeManager.IsDark)
            {
                ThemeManager.StyleNativeControl(grid);
                foreach (Control sb in grid.Controls)
                {
                    if (sb is ScrollBar) ThemeManager.StyleNativeControl(sb);
                }
            }
            if (!firstTime) return;
            grid.SortCompare += (s, e) =>
            {
                if (e.Column.HeaderText.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                    e.Column.DataPropertyName.Contains("Time", StringComparison.OrdinalIgnoreCase))
                {
                    string? v1 = e.CellValue1?.ToString();
                    string? v2 = e.CellValue2?.ToString();
                    if (DateTime.TryParse(v1, out DateTime dt1) && DateTime.TryParse(v2, out DateTime dt2))
                    {
                        e.SortResult = dt1.TimeOfDay.CompareTo(dt2.TimeOfDay);
                        e.Handled = true;
                    }
                }
            };
            grid.CellPainting += (s, e) =>
            {
                if (e.Handled) return;
                if (e.RowIndex == -1 && e.ColumnIndex >= 0 && e.Graphics != null)
                {
                    var col = grid.Columns[e.ColumnIndex];
                    using (var headerBgBrush = new SolidBrush(AppTheme.GridHeaderBack))
                    {
                        e.Graphics.FillRectangle(headerBgBrush, e.CellBounds);
                    }
                    using (var borderPen = new Pen(AppTheme.Border, 1f))
                    {
                        e.Graphics.DrawLine(borderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    string headerText = col.HeaderText;
                    bool centerHeader = col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleCenter ||
                                        col.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleCenter;
                    bool rightHeader = col.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight;
                    var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                                (centerHeader ? TextFormatFlags.HorizontalCenter : rightHeader ? TextFormatFlags.Right : TextFormatFlags.Left);
                    var textRect = new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y, e.CellBounds.Width - 18, e.CellBounds.Height);
                    if (col.HeaderCell.SortGlyphDirection != SortOrder.None) textRect.Width -= 12;
                    TextRenderer.DrawText(e.Graphics, headerText, UITheme.GridHeaderFont, textRect, AppTheme.GridHeaderText, flags);
                    if (col.HeaderCell.SortGlyphDirection != SortOrder.None)
                    {
                        int cx = e.CellBounds.Right - 14, cy = e.CellBounds.Top + e.CellBounds.Height / 2;
                        bool up = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending;
                        var oldMode = e.Graphics.SmoothingMode;
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using var gp = new Pen(AppTheme.TextMuted, 1.5f);
                        if (up) e.Graphics.DrawLines(gp, new[] { new PointF(cx - 4, cy + 2), new PointF(cx, cy - 2), new PointF(cx + 4, cy + 2) });
                        else e.Graphics.DrawLines(gp, new[] { new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2) });
                        e.Graphics.SmoothingMode = oldMode;
                    }
                    e.Handled = true;
                    return;
                }
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.Value != null && e.Graphics != null)
                {
                    var column = grid.Columns[e.ColumnIndex];
                    if (column is DataGridViewCheckBoxColumn || column is DataGridViewButtonColumn || column is DataGridViewImageColumn) return;
                    string colHeader = column.HeaderText ?? "";
                    string colProp = column.DataPropertyName ?? "";
                    if (colHeader.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
                        colHeader.Contains("Availability", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("Availability", StringComparison.OrdinalIgnoreCase) ||
                        colHeader.Contains("Active", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("IsActive", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = (e.FormattedValue as string) ?? e.Value.ToString() ?? "";
                        if (e.Value is bool b)
                        {
                            val = b ? "Active" : "Inactive";
                            if (colHeader.Contains("Avail", StringComparison.OrdinalIgnoreCase))
                            {
                                val = b ? "Active" : "On Trip";
                            }
                        }
                        if (string.IsNullOrWhiteSpace(val)) return;
                        bool isAction = val.StartsWith("✔");
                        var (bg, fg, dot) = isAction ? (AppTheme.Success, Color.White, Color.White) : StatusColors(val);
                        bool isSel = (e.State & DataGridViewElementStates.Selected) != 0;
                        var g = e.Graphics;
                        Color rowBack = isSel ? grid.DefaultCellStyle.SelectionBackColor
                            : (e.RowIndex % 2 == 1 && !grid.AlternatingRowsDefaultCellStyle.BackColor.IsEmpty ? grid.AlternatingRowsDefaultCellStyle.BackColor : grid.DefaultCellStyle.BackColor);
                        var sharedRow = grid.Rows.SharedRow(e.RowIndex);
                        if (!isSel && sharedRow.HasDefaultCellStyle && !sharedRow.DefaultCellStyle.BackColor.IsEmpty) rowBack = sharedRow.DefaultCellStyle.BackColor;
                        using (var rb = new SolidBrush(rowBack)) g.FillRectangle(rb, e.CellBounds);
                        using (var gl = new Pen(grid.GridColor)) g.DrawLine(gl, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                        var oldMode = g.SmoothingMode;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        var pillFont = FontCache.Get("Segoe UI Semibold", 8.25F);
                        {
                            Size ts = TextRenderer.MeasureText(g, val, pillFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                            int pillHeight = val.StartsWith("✔") ? 26 : 22;
                            int pillWidth = Math.Max(20, Math.Min(e.CellBounds.Width - 12, ts.Width + (val.StartsWith("✔") ? 24 : 30)));
                            var align = column.DefaultCellStyle.Alignment;
                            int pillX = align == DataGridViewContentAlignment.MiddleCenter
                                ? e.CellBounds.X + (e.CellBounds.Width - pillWidth) / 2
                                : e.CellBounds.X + 8;
                            int pillY = e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2;
                            var rect = new RectangleF(pillX, pillY, pillWidth, pillHeight);
                            using (var path = Gfx.RoundedRect(rect, pillHeight / 2f))
                            using (var brush = new SolidBrush(bg))
                            {
                                g.FillPath(brush, path);
                            }
                            if (isAction)
                            {
                                TextRenderer.DrawText(g, val, pillFont, Rectangle.Round(rect), fg, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                            }
                            else
                            {
                                using (var db = new SolidBrush(dot))
                                {
                                    g.FillEllipse(db, pillX + 10, pillY + pillHeight / 2f - 3, 6, 6);
                                }
                                var textRect = new Rectangle(pillX + 21, pillY, pillWidth - 25, pillHeight);
                                TextRenderer.DrawText(g, val, pillFont, textRect, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                            }
                        }
                        g.SmoothingMode = oldMode;
                        e.Handled = true;
                    }
                }
            };
        }
        /// <summary>Soft pill colours for a status word: (background, text, dot).</summary>
        public static (Color bg, Color fg, Color dot) StatusColors(string value)
        {
            string v = (value ?? "").ToUpperInvariant();
            if (v.Contains("FOR COMPLETION") || v.Contains("AWAITING"))
                return (AppTheme.WarningSoft, AppTheme.WarningText, AppTheme.Danger);
            if (v.Contains("MAINTENANCE") || v.Contains("REPAIR") || v.Contains("UNAVAILABLE") || v.Contains("NOT AVAILABLE") || v.Contains("CANCEL") ||
                v.Contains("OVERDUE") || v.Contains("UNPAID") || v == "OPEN" || v.Contains("INACTIVE") || v.Contains("REJECT") || v.Contains("FAILED"))
                return (AppTheme.DangerSoft, AppTheme.DangerText, AppTheme.Danger);
            if (v == "AVAILABLE" || v.Contains("READY") || v.Contains("COMPLETED") || v.Contains("PAID") ||
                v.Contains("RESOLVED") || v == "ACTIVE" || v.Contains("DELIVERED") || v == "TRUE" || v == "YES" || v.Contains("APPROVED"))
                return (AppTheme.SuccessSoft, AppTheme.SuccessText, AppTheme.Success);
            if (v.Contains("PENDING") || v.Contains("PARTIAL") || v.Contains("SCHEDULED") || v.Contains("DRAFT"))
                return (AppTheme.WarningSoft, AppTheme.WarningText, AppTheme.Warning);
            if (v.Contains("ON TRIP") || v.Contains("TRANSIT") || v.Contains("DISPATCH") || v.Contains("ASSIGNED") || v.Contains("IN USE") || v.Contains("IN PROGRESS") || v.Contains("TRIP"))
                return (AppTheme.InfoSoft, AppTheme.InfoText, AppTheme.Info);
            return (AppTheme.SurfaceHover, AppTheme.TextSecondary, AppTheme.TextMuted);
        }
        public static void ConfigureTimePicker(DateTimePicker picker, bool use24Hour = false)
        {
            if (picker == null) return;
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = use24Hour ? "HH:mm" : "hh:mm tt";
            picker.ShowUpDown = true;
            picker.Font = UITheme.InputFont;
            picker.CalendarFont = UITheme.InputFont;
            picker.ForeColor = AppTheme.TextPrimary;
            picker.BackColor = AppTheme.InputBack;
        }
        public static void PopulateTimeSlots(ComboBox combo, int intervalMinutes = 30, bool use24Hour = false, string defaultSelection = "06:00 AM")
        {
            if (combo == null) return;
            if (intervalMinutes <= 0) intervalMinutes = 30;
            combo.Items.Clear();
            combo.Font = UITheme.InputFont;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.ForeColor = AppTheme.TextPrimary;
            combo.BackColor = AppTheme.InputBack;
            combo.FlatStyle = FlatStyle.Flat;
            DateTime start = DateTime.Today;
            DateTime end = start.AddDays(1);
            while (start < end)
            {
                string timeStr = use24Hour ? start.ToString("HH:mm") : start.ToString("hh:mm tt");
                combo.Items.Add(timeStr);
                start = start.AddMinutes(intervalMinutes);
            }
            if (!string.IsNullOrEmpty(defaultSelection))
            {
                int idx = combo.Items.IndexOf(defaultSelection);
                if (idx >= 0) combo.SelectedIndex = idx;
                else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
        }
        public static string? ShowAnalogClockPicker(IWin32Window? owner, string initialTime = "08:00 AM")
        {
            using (var dlg = new Views.AnalogClockTimePickerDialog(initialTime))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.SelectedTimeString;
                }
            }
            return null;
        }
        public static void AttachAnalogClockPicker(Button button, ComboBox targetInput)
        {
            if (button == null || targetInput == null) return;
            button.Click += (s, e) =>
            {
                string currentTime = targetInput.Text;
                string? chosen = ShowAnalogClockPicker(button.FindForm(), string.IsNullOrEmpty(currentTime) ? "08:00 AM" : currentTime);
                if (!string.IsNullOrEmpty(chosen))
                {
                    if (!targetInput.Items.Contains(chosen))
                    {
                        targetInput.Items.Add(chosen);
                    }
                    targetInput.SelectedItem = chosen;
                    targetInput.Text = chosen;
                }
            };
        }
        public static void ApplyLoginTheme(Form loginForm, Panel cardPanel, Button loginBtn)
        {
            loginForm.BackColor = AppTheme.Canvas;
            if (cardPanel != null)
            {
                cardPanel.BackColor = AppTheme.Surface;
            }
            if (loginBtn != null)
            {
                loginBtn.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular);
                loginBtn.Height = 44;
                ColorButton(loginBtn, ButtonRole.Primary);
                StyleButtonChrome(loginBtn);
            }
        }
        public static Button CreateActionsDropdown(ContextMenuStrip menu, string text = "⋮ Actions ▾")
        {
            ApplyContextMenuTheme(menu);
            var btn = new Button
            {
                Text = text,
                Size = new Size(108, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 4, 4)
            };
            ColorButton(btn, ButtonRole.Secondary);
            btn.ForeColor = AppTheme.TextPrimary;
            StyleButtonChrome(btn);
            btn.Click += (s, e) =>
            {
                int x = Math.Min(0, btn.Width - menu.PreferredSize.Width);
                menu.Show(btn, new Point(x, btn.Height + 4));
            };
            return btn;
        }
        public static void ApplyContextMenuTheme(ContextMenuStrip menu)
        {
            if (menu == null) return;
            menu.Font = new Font("Segoe UI", 9.5F);
            menu.BackColor = AppTheme.Surface;
            menu.ForeColor = AppTheme.TextPrimary;
            menu.ShowImageMargin = false;
            menu.DropShadowEnabled = true;
            menu.Renderer = new ThemedMenuRenderer();
            foreach (ToolStripItem item in menu.Items)
            {
                item.Font = new Font("Segoe UI", 9.5F);
                if (item.ForeColor.ToArgb() != AppTheme.Danger.ToArgb() && item.ForeColor.ToArgb() != AppTheme.DangerText.ToArgb())
                    item.ForeColor = AppTheme.TextPrimary;
                item.Padding = new Padding(12, 6, 12, 6);
            }
        }
        public static Panel AttachSideBySideFormResizer(
            TableLayoutPanel tblMasterContent,
            Panel pnlForm,
            Panel pnlFormBody,
            DataGridView? grid,
            float initialWidth,
            Action<float> onWidthChanged)
        {
            Panel grip = new Panel
            {
                Name = "pnlFormResizeGrip",
                Dock = DockStyle.Right,
                Width = 6,
                BackColor = AppTheme.Border,
                Cursor = Cursors.SizeWE
            };
            bool isDragging = false;
            int startScreenX = 0;
            float startWidth = initialWidth;
            grip.MouseEnter += (s, e) => grip.BackColor = AppTheme.AccentSoftStrong;
            grip.MouseLeave += (s, e) => { if (!isDragging) grip.BackColor = AppTheme.Border; };
            grip.Paint += (s, e) =>
            {
                int cx = grip.Width / 2;
                int cy = grip.Height / 2;
                using var brush = new SolidBrush(AppTheme.TextMuted);
                for (int dy = -12; dy <= 12; dy += 6)
                {
                    e.Graphics.FillRectangle(brush, cx - 1, cy + dy, 2, 3);
                }
            };
            grip.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && tblMasterContent.ColumnStyles.Count > 0)
                {
                    isDragging = true;
                    startScreenX = Cursor.Position.X;
                    startWidth = tblMasterContent.ColumnStyles[0].Width;
                    grip.BackColor = AppTheme.Accent;
                }
            };
            grip.MouseMove += (s, e) =>
            {
                if (!isDragging || tblMasterContent.ColumnStyles.Count == 0) return;
                int deltaX = Cursor.Position.X - startScreenX;
                float maxAllowed = Math.Max(480F, tblMasterContent.ClientSize.Width - 420F);
                float nextWidth = Math.Clamp(startWidth + deltaX, 360F, maxAllowed);
                tblMasterContent.ColumnStyles[0].Width = nextWidth;
                onWidthChanged(nextWidth);
                SyncFormBodyControlsWidth(pnlFormBody);
            };
            grip.MouseUp += (s, e) =>
            {
                if (!isDragging) return;
                isDragging = false;
                grip.BackColor = AppTheme.Border;
                SyncFormBodyControlsWidth(pnlFormBody);
                if (grid != null && grid.Columns.Count > 0)
                {
                    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            };
            pnlFormBody.Resize += (s, e) => SyncFormBodyControlsWidth(pnlFormBody);
            pnlForm.Controls.Add(grip);
            pnlForm.Controls.SetChildIndex(grip, pnlForm.Controls.Count - 1);
            return grip;
        }
        public static float GetAdaptiveDrawerWidth(Control control, float laptopWidth = 400F, float desktopWidth = 460F)
        {
            try
            {
                int width = control.FindForm()?.ClientSize.Width ?? Screen.FromControl(control).Bounds.Width;
                return width <= 1400 ? laptopWidth : desktopWidth;
            }
            catch
            {
                return laptopWidth;
            }
        }
        public static void SyncFormBodyControlsWidth(Panel pnlFormBody)
        {
            if (pnlFormBody == null || pnlFormBody.ClientSize.Width <= 40) return;
            foreach (Control c in pnlFormBody.Controls)
            {
                if (c.Dock == DockStyle.None && !(c is Label) && !(c is CheckBox))
                {
                    c.Width = Math.Max(160, pnlFormBody.ClientSize.Width - c.Left - Math.Max(12, pnlFormBody.Padding.Right));
                }
            }
        }
        /// <summary>
        /// ComboBox.EndUpdate re-binds Windows' auto-complete. On some combos Windows rejects the re-bind
        /// ("Value does not fall within the expected range"); when that happens the auto-complete is reset
        /// instead of letting the whole screen fail to load.
        /// </summary>
        public static void SafeEndUpdate(ComboBox cb)
        {
            try
            {
                cb.EndUpdate();
            }
            catch (ArgumentException)
            {
                var mode = cb.AutoCompleteMode;
                var source = cb.AutoCompleteSource;
                try { cb.AutoCompleteMode = AutoCompleteMode.None; } catch { }
                try
                {
                    cb.AutoCompleteSource = source;
                    cb.AutoCompleteMode = mode;
                }
                catch
                {
                    try { cb.AutoCompleteMode = AutoCompleteMode.None; } catch { }
                }
            }
        }
        public static void MakeRounded(Control c, int radius)
        {
            void Apply()
            {
                if (c.Width <= 0 || c.Height <= 0) return;
                using var path = Gfx.RoundedRect(new Rectangle(0, 0, c.Width, c.Height), radius);
                var old = c.Region;
                c.Region = new Region(path);
                old?.Dispose();
            }
            Apply();
            c.Resize += (s, e) => Apply();
        }
    }
    /// <summary>Menus and drop-downs in theme colours with soft rounded highlights.</summary>
    public sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemedMenuRenderer() : base(new ThemedColorTable()) { RoundedEdges = false; }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            var r = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
            if (e.Item.Selected && e.Item.Enabled)
            {
                Gfx.HighQuality(g);
                using var path = Gfx.RoundedRect(r, 6);
                using var b = new SolidBrush(AppTheme.SurfaceHover);
                g.FillPath(b, path);
            }
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled) e.TextColor = AppTheme.TextMuted;
            else if (e.TextColor.ToArgb() == SystemColors.ControlText.ToArgb() || e.TextColor.ToArgb() == SystemColors.MenuText.ToArgb()) e.TextColor = AppTheme.TextPrimary;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var p = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using var p = new Pen(AppTheme.BorderStrong);
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
            else base.OnRenderToolStripBorder(e);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = AppTheme.TextMuted;
            base.OnRenderArrow(e);
        }
    }
    public sealed class ThemedColorTable : ProfessionalColorTable
    {
        public ThemedColorTable() { UseSystemColors = false; }
        public override Color ToolStripDropDownBackground => AppTheme.Surface;
        public override Color ImageMarginGradientBegin => AppTheme.Surface;
        public override Color ImageMarginGradientMiddle => AppTheme.Surface;
        public override Color ImageMarginGradientEnd => AppTheme.Surface;
        public override Color MenuBorder => AppTheme.BorderStrong;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => AppTheme.SurfaceHover;
        public override Color MenuItemSelectedGradientBegin => AppTheme.SurfaceHover;
        public override Color MenuItemSelectedGradientEnd => AppTheme.SurfaceHover;
        public override Color MenuItemPressedGradientBegin => AppTheme.SurfaceHover;
        public override Color MenuItemPressedGradientEnd => AppTheme.SurfaceHover;
        public override Color SeparatorDark => AppTheme.Border;
        public override Color SeparatorLight => AppTheme.Border;
        public override Color ToolStripBorder => AppTheme.Border;
        public override Color ToolStripGradientBegin => AppTheme.Surface;
        public override Color ToolStripGradientMiddle => AppTheme.Surface;
        public override Color ToolStripGradientEnd => AppTheme.Surface;
    }
    public static class UiThemeHelper
    {
        public static void ApplyModernStyling(Control parent) => UIThemeHelper.ApplyTheme(parent);
        public static void ApplyGridStyling(DataGridView grid) => UIThemeHelper.ApplyGridTheme(grid);
        public static void ConfigureTimePicker(DateTimePicker picker, bool use24Hour = false) => UIThemeHelper.ConfigureTimePicker(picker, use24Hour);
        public static void PopulateTimeSlots(ComboBox combo, int intervalMinutes = 30, bool use24Hour = false, string defaultSelection = "06:00 AM")
            => UIThemeHelper.PopulateTimeSlots(combo, intervalMinutes, use24Hour, defaultSelection);
        public static string? ShowAnalogClockPicker(IWin32Window? owner, string initialTime = "08:00 AM")
            => UIThemeHelper.ShowAnalogClockPicker(owner, initialTime);
    }
}
