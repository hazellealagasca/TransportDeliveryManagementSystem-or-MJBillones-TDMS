using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Settings → Features &amp; Pop-ups: switch optional parts of the system and the kinds of pop-ups on or off.</summary>
    public class FeaturesSettingsView : UserControl, IRefreshable
    {
        private sealed record Option(string Key, string Title, string Description);
        private static readonly Option[] Parts =
        {
            new(Features.RateMatrix, "Rate Matrix", "Your price list by route, destination, truck type and season (Master Data → Rate Matrix). When on, loading orders and invoices suggest the rate. When off, the Rate Matrix tab is hidden and amounts are typed in by hand."),
            new(Features.SpecialTrips, "Special trips", "Trips for non-crate or specialised cargo. When off, the Special Trip History tab, the “+ Special Trip” button and the special-cargo choice are hidden. Past special trips stay in Order History."),
            new(Features.Incidents, "Incident & damage logs", "Records of damaged or missing crates. When off, the Incident & Damage Logs tab, the “Log incident” buttons and the question after a short trip report are hidden."),
            new(Features.CrewPay, "Crew pay on trip reports", "The “Driver & aide pay” step (daily wage and meal allowance) in the trip report form. When off, that step is hidden; the usual rates are still recorded with each report."),
        };
        private static readonly Option[] Popups =
        {
            new(Features.QuietNotices, "Quiet notices instead of pop-ups", "Simple confirmations such as “Saved successfully” appear as a small notice in the corner that fades away, instead of a window you have to click OK on. Questions, warnings and errors still use a window."),
            new(Features.ConfirmSaves, "Ask before saving", "Shows “Are you sure?” before Save, Add and Update. Deleting, cancelling, resetting and restoring always ask, whatever this is set to."),
            new(Features.Toasts, "Small notices in the corner", "Short notices at the bottom-right of the window (for example “Logo updated”). When off, none are shown, and quiet notices fall back to a normal window."),
            new(Features.BackupReminder, "Backup reminder", "A Notice item on the dashboard when no backup has been made for 7 days (administrators only)."),
        };
        private readonly System.Collections.Generic.Dictionary<string, ToggleSwitch> switches = new();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public FeaturesSettingsView()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.75F);
            Padding = new Padding(24);
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.Controls.Add(Card("🧩  Parts of the system", "Turn off what M.J. Billones doesn't use. Turning a part off only hides it: no records are deleted or changed, and everything comes back exactly as it was when you turn it on again.", Parts));
            stack.Controls.Add(Card("🔔  Pop-ups and reminders", "Choose which messages need your attention. Fewer pop-ups means fewer interruptions while you work.", Popups));
            Controls.Add(stack);
            if (!SessionContext.IsAdmin)
            {
                foreach (var sw in switches.Values) sw.Enabled = false;
                var note = new Label { Text = "Only the Owner / Administrator can change these settings.", AutoSize = true, Dock = DockStyle.Top, ForeColor = AppTheme.TextMuted, Padding = new Padding(0, 0, 0, 12) };
                Controls.Add(note);
            }
            UIThemeHelper.ApplyTheme(this);
            BackColor = AppTheme.Canvas;
            HookWheel(this);
        }
        /// <summary>The mouse wheel scrolls the page wherever the pointer is (labels and switches don't scroll by themselves).</summary>
        private void HookWheel(Control c)
        {
            if (c != this) c.MouseWheel += (s, e) =>
            {
                int y = Math.Max(0, -AutoScrollPosition.Y - e.Delta);
                AutoScrollPosition = new Point(0, y);
                if (e is HandledMouseEventArgs h) h.Handled = true;
            };
            foreach (Control child in c.Controls) HookWheel(child);
        }
        public void RefreshData()
        {
            Features.Reload();
            foreach (var kv in switches) kv.Value.SetQuietly(Features.On(kv.Key));
        }
        private Control Card(string title, string subtitle, Option[] options)
        {
            var card = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Padding = new Padding(D(22), D(18), D(22), D(10)), Margin = new Padding(0, 0, 0, D(16)) };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(AppTheme.Border);
                using var gp = Gfx.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), D(12));
                e.Graphics.DrawPath(pen, gp);
            };
            card.Resize += (s, e) => card.Invalidate();
            var rows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            int r = 0;
            rows.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 12.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0), UseMnemonic = false }, 0, r);
            rows.SetColumnSpan(rows.GetControlFromPosition(0, r)!, 2); r++;
            var sub = new Label { Text = subtitle, AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, D(4), 0, D(8)), UseMnemonic = false };
            rows.Controls.Add(sub, 0, r); rows.SetColumnSpan(sub, 2); r++;
            var wraps = new System.Collections.Generic.List<Label> { sub };
            foreach (var o in options)
            {
                var text = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, D(12), D(16), D(12)), BackColor = AppTheme.Surface };
                var t = new Label { Text = o.Title, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0), UseMnemonic = false };
                var d = new Label { Text = o.Description, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(3), 0, 0), UseMnemonic = false };
                text.Controls.Add(t); text.Controls.Add(d);
                wraps.Add(d);
                var state = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 8.75F), Margin = new Padding(0, D(4), D(10), 0), Anchor = AnchorStyles.Right, UseMnemonic = false };
                var sw = new ToggleSwitch { Anchor = AnchorStyles.Right, Margin = new Padding(0, D(14), 0, 0) };
                sw.SetQuietly(Features.On(o.Key));
                void ShowState() { state.Text = sw.Checked ? "On" : "Off"; state.ForeColor = sw.Checked ? AppTheme.SuccessText : AppTheme.TextMuted; }
                ShowState();
                var opt = o;
                sw.CheckedChanged += (s, e) =>
                {
                    ShowState();
                    try
                    {
                        Features.Set(opt.Key, sw.Checked, opt.Title);
                        var f = FindForm();
                        if (f != null && opt.Key != Features.Toasts) ToastHelper.ShowToast(f, $"{opt.Title} turned {(sw.Checked ? "on" : "off")}.", ToastType.Info);
                    }
                    catch (Exception ex)
                    {
                        sw.SetQuietly(!sw.Checked); ShowState();
                        AppLog.ShowFailure(FindForm(), "Features", $"“{opt.Title}” could not be changed.", ex);
                    }
                };
                switches[o.Key] = sw;
                var right = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, D(10), 0, 0), BackColor = AppTheme.Surface, Anchor = AnchorStyles.Right | AnchorStyles.Top };
                right.Controls.Add(state); right.Controls.Add(sw);
                rows.Controls.Add(text, 0, r);
                rows.Controls.Add(right, 1, r);
                r++;
                // thin divider between options (not after the last one)
                if (o != options[^1])
                {
                    var line = new Panel { Height = 1, AutoSize = false, Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = AppTheme.Border, Margin = new Padding(0) };
                    rows.Controls.Add(line, 0, r); rows.SetColumnSpan(line, 2); r++;
                }
            }
            void Wrap()
            {
                int w = Math.Max(D(240), rows.ClientSize.Width - D(130));
                foreach (var l in wraps) l.MaximumSize = new Size(l == sub ? rows.ClientSize.Width : w, 0);
            }
            rows.Resize += (s, e) => Wrap();
            card.Controls.Add(rows);
            return card;
        }
    }
}
