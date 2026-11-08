using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using WinFormsMessageBox = System.Windows.Forms.MessageBox;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// The app's own pop-up, used everywhere the code says MessageBox.Show(...). Same calls and results as the Windows
    /// message box, but drawn in the app's theme (light and dark), sized for the screen's scaling, with shouting titles
    /// and ALL-CAPS lines written in normal case. Falls back to the Windows box if anything goes wrong.
    /// </summary>
    public static class MessageBox
    {
        public static DialogResult Show(string text) => Core(null, text, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption) => Core(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) => Core(null, text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => Core(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => Core(null, text, caption, buttons, icon, defaultButton);
        public static DialogResult Show(IWin32Window? owner, string text) => Core(owner, text, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(IWin32Window? owner, string text, string caption) => Core(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons) => Core(owner, text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => Core(owner, text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => Core(owner, text, caption, buttons, icon, defaultButton);








        /// <summary>Same themed pop-up with your own button labels. The first button is the main one unless its result is Cancel/No;
        /// Esc and the title-bar X give the button whose result is Cancel.</summary>
        public static DialogResult Ask(IWin32Window? owner, string text, string caption, MessageBoxIcon icon, (string Text, DialogResult Result)[] buttons, int defaultIndex = 0)
            => Core(owner, text, caption, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, buttons, defaultIndex);

        private static DialogResult Core(IWin32Window? owner, string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton,
            (string Text, DialogResult Result)[]? custom = null, int customDefault = 0)
        {
            text ??= ""; caption ??= "";
            try
            {
                // Always show on the UI thread (a background task asking would otherwise crash the window).
                var ui = Application.OpenForms.Cast<Form>().FirstOrDefault(f => !f.IsDisposed && f.IsHandleCreated);
                if (ui != null && ui.InvokeRequired)
                    return (DialogResult)ui.Invoke(new Func<DialogResult>(() => Core(owner, text, caption, buttons, icon, defaultButton, custom, customDefault)));
                return ShowThemed(owner, text, caption, buttons, icon, defaultButton, custom, customDefault);
            }
            catch (Exception ex)
            {
                try { ErrLog.Swallowed(ex); } catch { }
                if (custom != null) return custom.Any(c => c.Result == DialogResult.Cancel) ? DialogResult.Cancel : custom[^1].Result; // if the pop-up cannot be drawn, do the safe thing (stay)
                return owner == null
                    ? WinFormsMessageBox.Show(text, caption, buttons, icon, defaultButton)
                    : WinFormsMessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
            }
        }








        /// <summary>"DISPATCH NUMBER ALREADY EXISTS:" → "Dispatch number already exists:". Lines with numbers or codes are left alone.</summary>
        internal static string Calm(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var lines = s.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                int letters = l.Count(char.IsLetter);
                if (letters < 6 || l.Any(char.IsLower) || l.Any(char.IsDigit)) continue;
                if (Regex.Matches(l, @"\p{L}{2,}").Count < 2) continue;
                string lower = l.ToLowerInvariant();
                int first = lower.IndexOf(lower.FirstOrDefault(char.IsLetter));
                lines[i] = first < 0 ? lower : lower.Substring(0, first) + char.ToUpperInvariant(lower[first]) + lower.Substring(first + 1);
                // Keep well-known short codes in capitals.
                lines[i] = Regex.Replace(lines[i], @"\b(soa|pdf|csv|tin|pod|lto|id|ok|dtr|sku|php)\b", m => m.Value.ToUpperInvariant());
            }
            return string.Join(Environment.NewLine, lines);
        }








        private static DialogResult ShowThemed(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton,
            (string Text, DialogResult Result)[]? custom = null, int customDefault = 0)
        {
            Form? host = owner as Form ?? (owner as Control)?.FindForm() ?? Form.ActiveForm
                         ?? Application.OpenForms.Cast<Form>().LastOrDefault(f => f.Visible && !f.IsDisposed);
            if (host != null && (host.IsDisposed || !host.Visible)) host = null;
            string title = Calm(string.IsNullOrWhiteSpace(caption) ? "M.J. Billones" : caption.Trim());
            string body = Calm(text.Trim());
            using var dlg = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = host == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowIcon = false,
                ShowInTaskbar = host == null, TopMost = host == null,
                AutoScaleMode = AutoScaleMode.None,
                BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.75F),
                KeyPreview = true
            };
            int D(int v) => (int)Math.Round(v * dlg.DeviceDpi / 96f);
            try { ThemeManager.StyleWindowChrome(dlg); } catch (Exception swx) { ErrLog.Swallowed(swx); }
            // Tone from the icon (questions are information-blue, like the rest of the app).
            Color tint, soft; string glyph;
            switch (icon)
            {
                case MessageBoxIcon.Error: tint = AppTheme.Danger; soft = AppTheme.DangerSoft; glyph = "!"; break;
                case MessageBoxIcon.Warning: tint = AppTheme.Warning; soft = AppTheme.WarningSoft; glyph = "!"; break;
                case MessageBoxIcon.Question: tint = AppTheme.Info; soft = AppTheme.InfoSoft; glyph = "?"; break;
                case MessageBoxIcon.Information: tint = AppTheme.Info; soft = AppTheme.InfoSoft; glyph = "i"; break;
                default: tint = AppTheme.Accent; soft = AppTheme.AccentSoft; glyph = "i"; break;
            }
            int textW = D(440);
            var content = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = new Padding(0), Padding = new Padding(D(22), D(20), D(26), D(16)), BackColor = AppTheme.Surface };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var badge = new Panel { Size = new Size(D(42), D(42)), Margin = new Padding(0, D(2), D(14), 0), BackColor = AppTheme.Surface };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(AppTheme.Surface);
                using (var b = new SolidBrush(soft)) g.FillEllipse(b, 0, 0, badge.Width - 1, badge.Height - 1);
                int inset = D(8);
                using (var b = new SolidBrush(tint)) g.FillEllipse(b, inset, inset, badge.Width - 1 - inset * 2, badge.Height - 1 - inset * 2);
                using var f = new Font("Segoe UI", 11F, FontStyle.Bold);
                TextRenderer.DrawText(g, glyph, f, new Rectangle(0, 0, badge.Width, badge.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            var stack = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = AppTheme.Surface };
            stack.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 12F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, AutoSize = true, MaximumSize = new Size(textW, 0), Margin = new Padding(0, D(2), 0, 0), UseMnemonic = false });
            if (body.Length > 0)
            {
                var bodyFont = new Font("Segoe UI", 9.75F);
                int maxH = Math.Max(D(200), (int)(Screen.FromPoint(Cursor.Position).WorkingArea.Height * 0.55));
                var size = TextRenderer.MeasureText(body, bodyFont, new Size(textW, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                if (size.Height <= maxH)
                {
                    stack.Controls.Add(new Label { Text = body, Font = bodyFont, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.Surface, AutoSize = true, MaximumSize = new Size(textW, 0), Margin = new Padding(0, D(8), 0, 0), UseMnemonic = false });
                }
                else
                {
                    // Very long messages (lists of records) scroll instead of running off the screen.
                    var box = new TextBox { Text = body.Replace("\n", Environment.NewLine).Replace("\r\r", "\r"), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, Font = bodyFont, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.Surface, Width = textW, Height = maxH, Margin = new Padding(0, D(8), 0, 0), TabStop = false };
                    stack.Controls.Add(box);
                }
            }
            content.Controls.Add(badge, 0, 0);
            content.Controls.Add(stack, 1, 0);
            // Buttons on a slightly darker band, right-aligned; Windows' own order (Yes, No, Cancel / OK, Cancel).
            var footer = new FlowLayoutPanel { Height = D(60), FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0), Padding = new Padding(D(16), D(13), D(20), D(13)), BackColor = AppTheme.SurfaceAlt };
            footer.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
            (string Text, DialogResult Result)[] defs = custom != null && custom.Length > 0 ? custom : buttons switch
            {
                MessageBoxButtons.OKCancel => new[] { ("OK", DialogResult.OK), ("Cancel", DialogResult.Cancel) },
                MessageBoxButtons.YesNo => new[] { ("Yes", DialogResult.Yes), ("No", DialogResult.No) },
                MessageBoxButtons.YesNoCancel => new[] { ("Yes", DialogResult.Yes), ("No", DialogResult.No), ("Cancel", DialogResult.Cancel) },
                MessageBoxButtons.RetryCancel => new[] { ("Try again", DialogResult.Retry), ("Cancel", DialogResult.Cancel) },
                MessageBoxButtons.AbortRetryIgnore => new[] { ("Stop", DialogResult.Abort), ("Try again", DialogResult.Retry), ("Ignore", DialogResult.Ignore) },
                MessageBoxButtons.CancelTryContinue => new[] { ("Cancel", DialogResult.Cancel), ("Try again", DialogResult.TryAgain), ("Continue", DialogResult.Continue) },
                _ => new[] { ("OK", DialogResult.OK) }
            };
            int defIndex = custom != null ? customDefault : defaultButton switch { MessageBoxDefaultButton.Button2 => 1, MessageBoxDefaultButton.Button3 => 2, _ => 0 };
            if (defIndex >= defs.Length) defIndex = 0;
            var made = new Button[defs.Length];
            for (int i = defs.Length - 1; i >= 0; i--)
            {
                var b = new Button { Text = defs[i].Text, DialogResult = defs[i].Result, AutoSize = true, MinimumSize = new Size(D(92), D(34)), Padding = new Padding(D(8), 0, D(8), 0), Margin = new Padding(D(8), 0, 0, 0), Cursor = Cursors.Hand, UseMnemonic = false };
                bool positive = defs[i].Result is DialogResult.OK or DialogResult.Yes or DialogResult.Retry or DialogResult.TryAgain or DialogResult.Continue;
                UIThemeHelper.SetButtonRole(b, i == 0 && positive ? "primary" : "secondary");
                footer.Controls.Add(b);
                made[i] = b;
            }
            var cancel = defs.Select((d, i) => (d, i)).FirstOrDefault(x => x.d.Result == DialogResult.Cancel);
            if (cancel.d.Text != null) dlg.CancelButton = made[cancel.i];
            else if (defs.Length == 1) dlg.CancelButton = made[0];
            else if (custom == null && buttons == MessageBoxButtons.YesNo) dlg.CancelButton = made[1];
            dlg.AcceptButton = made[defIndex];
            // Ctrl+C copies the message, like the Windows box.
            dlg.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    try { Clipboard.SetText(title + Environment.NewLine + Environment.NewLine + body); } catch (Exception swx) { ErrLog.Swallowed(swx); }
                    e.Handled = true;
                }
            };
            var pref = content.GetPreferredSize(Size.Empty);
            int needButtons = made.Sum(b => Math.Max(b.MinimumSize.Width, b.GetPreferredSize(Size.Empty).Width) + D(8)) + footer.Padding.Horizontal;
            dlg.ClientSize = new Size(Math.Max(Math.Max(pref.Width, needButtons), D(400)), pref.Height + footer.Height);
            content.AutoSize = false; content.Dock = DockStyle.Fill;
            footer.Dock = DockStyle.Bottom;
            dlg.Controls.Add(content);
            dlg.Controls.Add(footer);
            dlg.Shown += (s, e) =>
            {
                UIThemeHelper.KeepOnScreen(dlg);
                made[defIndex].Focus();
                if (dlg.TopMost) dlg.Activate();
                try
                {
                    switch (icon)
                    {
                        case MessageBoxIcon.Error: SystemSounds.Hand.Play(); break;
                        case MessageBoxIcon.Warning: SystemSounds.Exclamation.Play(); break;
                        case MessageBoxIcon.Question: SystemSounds.Question.Play(); break;
                        case MessageBoxIcon.Information: SystemSounds.Asterisk.Play(); break;
                    }
                }
                catch (Exception swx) { ErrLog.Swallowed(swx); }
            };
            var result = host != null ? dlg.ShowDialog(host) : dlg.ShowDialog();
            // Closing with the title-bar X: behave like the Windows box (Cancel / No / OK).
            if (result == DialogResult.Cancel && dlg.CancelButton is Button cb && cb.DialogResult != DialogResult.Cancel) return cb.DialogResult;
            return result;
        }
    }
}
