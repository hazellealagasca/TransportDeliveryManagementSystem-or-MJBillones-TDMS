using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public enum DialogTone { Info, Success, Warning, Error }
    /// <summary>
    /// The app's own message window: a coloured badge, a short heading, a plain-words explanation, and optionally a
    /// folder path (with Open folder / Copy) or error details (with Copy error details). Sized for the screen's DPI.
    /// </summary>
    public static class AppDialog
    {
        public static void Show(IWin32Window? owner, string windowTitle, DialogTone tone, string heading, string body,
            string? path = null, string? note = null, string? details = null, string okText = "OK")
        {
            using var dlg = new Form
            {
                Text = windowTitle,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = owner == null, ShowIcon = false,
                AutoScaleMode = AutoScaleMode.None,
                BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.75F),
                TopMost = owner == null
            };
            int D(int v) => (int)Math.Round(v * dlg.DeviceDpi / 96f);
            try { ThemeManager.StyleWindowChrome(dlg); } catch (Exception swx) { ErrLog.Swallowed(swx); }
            Color tint = tone switch { DialogTone.Success => AppTheme.Success, DialogTone.Warning => AppTheme.Warning, DialogTone.Error => AppTheme.Danger, _ => AppTheme.Info };
            Color soft = tone switch { DialogTone.Success => AppTheme.SuccessSoft, DialogTone.Warning => AppTheme.WarningSoft, DialogTone.Error => AppTheme.DangerSoft, _ => AppTheme.InfoSoft };
            string glyph = tone switch { DialogTone.Success => "✓", DialogTone.Warning => "!", DialogTone.Error => "!", _ => "i" };
            int textW = D(430);
            // ---- content: badge + text
            var content = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = new Padding(0), Padding = new Padding(D(24), D(22), D(28), D(18)), BackColor = AppTheme.Surface };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var badge = new Panel { Size = new Size(D(44), D(44)), Margin = new Padding(0, D(2), D(16), 0), BackColor = AppTheme.Surface };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(AppTheme.Surface);
                using (var b = new SolidBrush(soft)) g.FillEllipse(b, 0, 0, badge.Width - 1, badge.Height - 1);
                int inset = D(8);
                using (var b = new SolidBrush(tint)) g.FillEllipse(b, inset, inset, badge.Width - 1 - inset * 2, badge.Height - 1 - inset * 2);
                using var f = new Font("Segoe UI", 11F, FontStyle.Bold);
                TextRenderer.DrawText(g, glyph, f, new Rectangle(0, 0, badge.Width, badge.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            var text = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = AppTheme.Surface };
            Label L(string t, Font f, Color c, int top) => new Label { Text = t, Font = f, ForeColor = c, AutoSize = true, MaximumSize = new Size(textW, 0), Margin = new Padding(0, top, 0, 0), UseMnemonic = false, BackColor = AppTheme.Surface };
            text.Controls.Add(L(heading, new Font("Segoe UI Semibold", 12.5F), AppTheme.TextPrimary, 0));
            if (!string.IsNullOrWhiteSpace(body)) text.Controls.Add(L(body, new Font("Segoe UI", 9.75F), AppTheme.TextSecondary, D(6)));
            if (!string.IsNullOrWhiteSpace(path))
            {
                var box = new Panel { Width = textW, Height = D(40), Margin = new Padding(0, D(14), 0, 0), BackColor = AppTheme.SurfaceAlt, Padding = new Padding(D(12), 0, D(8), 0) };
                box.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(AppTheme.Border);
                    using var gp = Gfx.RoundedRect(new Rectangle(0, 0, box.Width - 1, box.Height - 1), D(8));
                    e.Graphics.DrawPath(pen, gp);
                };
                var copy = new LinkLabel { Text = "Copy", AutoSize = true, Dock = DockStyle.Right, Padding = new Padding(0, D(11), D(4), 0), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, LinkBehavior = LinkBehavior.HoverUnderline, BackColor = AppTheme.SurfaceAlt };
                var pathLbl = new Label { Text = "📁  " + path, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Font = new Font("Consolas", 9.5F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false, BackColor = AppTheme.SurfaceAlt };
                copy.LinkClicked += (s, e) => { try { Clipboard.SetText(path); copy.Text = "Copied ✓"; } catch (Exception swx) { ErrLog.Swallowed(swx); } };
                box.Controls.Add(pathLbl);
                box.Controls.Add(copy);
                text.Controls.Add(box);
            }
            if (!string.IsNullOrWhiteSpace(note)) text.Controls.Add(L(note, new Font("Segoe UI", 8.75F), AppTheme.TextMuted, D(12)));
            content.Controls.Add(badge, 0, 0);
            content.Controls.Add(text, 1, 0);
            // ---- footer: buttons on a slightly darker band
            var footer = new FlowLayoutPanel { AutoSize = false, Height = D(60), FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0), Padding = new Padding(D(16), D(13), D(20), D(13)), BackColor = AppTheme.SurfaceAlt };
            footer.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
            Button B(string t, string role, int minW)
            {
                var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(D(minW), D(34)), Margin = new Padding(D(8), 0, 0, 0), Cursor = Cursors.Hand, UseMnemonic = false };
                UIThemeHelper.SetButtonRole(b, role);
                return b;
            }
            var ok = B(okText, "primary", 96);
            ok.DialogResult = DialogResult.OK;
            footer.Controls.Add(ok);
            if (!string.IsNullOrWhiteSpace(path))
            {
                var open = B("Open folder", "secondary", 110);
                open.Click += (s, e) =>
                {
                    try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true }); }
                    catch (Exception ex) { AppLog.Error("AppDialog.OpenFolder", ex, path); }
                };
                footer.Controls.Add(open);
            }
            if (!string.IsNullOrWhiteSpace(details))
            {
                var copyDetails = B("Copy error details", "secondary", 140);
                copyDetails.Click += (s, e) => { try { Clipboard.SetText(details); copyDetails.Text = "Copied ✓"; } catch (Exception swx) { ErrLog.Swallowed(swx); } };
                footer.Controls.Add(copyDetails);
            }
            // Size the window to the text, then dock: content on top, buttons in a band along the bottom.
            var pref = content.GetPreferredSize(Size.Empty);
            dlg.ClientSize = new Size(Math.Max(pref.Width, D(420)), pref.Height + footer.Height);
            content.AutoSize = false;
            content.Dock = DockStyle.Fill;
            footer.Dock = DockStyle.Bottom;
            dlg.Controls.Add(content);
            dlg.Controls.Add(footer);
            dlg.AcceptButton = ok;
            dlg.CancelButton = ok;
            dlg.Shown += (s, e) => { ok.Focus(); if (dlg.TopMost) dlg.Activate(); };
            if (owner != null) dlg.ShowDialog(owner); else dlg.ShowDialog();
        }
    }
}
