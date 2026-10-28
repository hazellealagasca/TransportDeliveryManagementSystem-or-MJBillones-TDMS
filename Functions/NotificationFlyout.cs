using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The bell's drop-down: unread count, filter chips, notifications grouped by day, and one click to open the
    /// record a notification is about (deep link). Hovering a row shows a dismiss button.
    /// </summary>
    public sealed class NotificationFlyout : Panel
    {
        private readonly Action<SystemNotification> open;
        private readonly Label lblCount;
        private readonly FlowLayoutPanel chips;
        private readonly Panel list;
        private string filter = "All";
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private static readonly string[] BaseFilters = { "All", "Unread", "Operations", "Payments", "Trucks", "Alerts" };
        public NotificationFlyout(Action<SystemNotification> open)
        {
            this.open = open;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Size = new Size(D(440), D(580));
            BackColor = AppTheme.Surface;
            Padding = new Padding(1);
            Visible = false;
            var header = new Panel { Dock = DockStyle.Top, Height = D(56), BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(12), D(10), D(6)) };
            var title = new Label { Text = "Notifications", AutoSize = true, Font = new Font("Segoe UI Semibold", 12.5F), ForeColor = AppTheme.TextPrimary, Location = new Point(D(16), D(14)) };
            lblCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = AppTheme.AccentText, BackColor = AppTheme.AccentSoft, Padding = new Padding(D(6), D(2), D(6), D(2)), Location = new Point(D(150), D(18)) };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface };
            Button Link(string text, Action a)
            {
                var b = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 8.75F), ForeColor = AppTheme.AccentText, BackColor = AppTheme.Surface, Cursor = Cursors.Hand, Margin = new Padding(0, D(4), D(2), 0) };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
                b.Click += (s, e) => a();
                return b;
            }
            actions.Controls.Add(Link("Mark all read", () => { EventNotificationService.MarkAllAsRead(SessionContext.IsAdmin); Render(); }));
            actions.Controls.Add(Link("Clear read", () =>
            {
                var read = EventNotificationService.GetRecentNotifications(200, null, false, SessionContext.IsAdmin).Where(n => n.IsRead).ToList();
                if (read.Count == 0) return;
                if (MessageBox.Show(FindForm(), $"Are you sure you want to clear {read.Count} read notification(s)?", "Clear notifications", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                foreach (var n in read) EventNotificationService.DeleteNotification(n.Id);
                Render();
            }));
            header.Controls.Add(actions);
            header.Controls.Add(lblCount);
            header.Controls.Add(title);
            chips = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(42), BackColor = AppTheme.Surface, Padding = new Padding(D(12), D(4), D(8), D(6)), WrapContents = false };
            chips.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, chips.Height - 1, chips.Width, chips.Height - 1); };
            list = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.Canvas, Padding = new Padding(D(8), D(6), D(8), D(8)) };
            Controls.Add(list);
            Controls.Add(chips);
            Controls.Add(header);
            BuildChips();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var p = new Pen(AppTheme.BorderStrong);
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
        private void BuildChips()
        {
            chips.Controls.Clear();
            var filters = BaseFilters.ToList();
            if (SessionContext.IsAdmin) filters.Add("Security");
            foreach (var f in filters)
            {
                bool on = f == filter;
                var b = new Button { Text = f, AutoSize = true, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 8.5F), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(4), 0), Padding = new Padding(D(4), 0, D(4), 0), Height = D(28) };
                b.BackColor = on ? AppTheme.Accent : AppTheme.Surface;
                b.ForeColor = on ? AppTheme.OnAccent : AppTheme.TextSecondary;
                b.FlatAppearance.BorderColor = on ? AppTheme.Accent : AppTheme.Border;
                b.FlatAppearance.MouseOverBackColor = on ? AppTheme.AccentHover : AppTheme.SurfaceHover;
                b.Click += (s, e) => { filter = f; BuildChips(); Render(); };
                chips.Controls.Add(b);
            }
        }
        /// <summary>Re-reads the notifications and redraws the list.</summary>
        public void Render()
        {
            try { RenderCore(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] Render: {ex.Message}");
                list.ResumeLayout();
                list.Controls.Clear();
                list.Controls.Add(new Label { Text = "Notifications couldn't be loaded. Click the bell to try again.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F) });
            }
        }
        private void RenderCore()
        {
            bool unreadOnly = filter == "Unread";
            string? cat = filter is "All" or "Unread" ? null : filter;
            var items = EventNotificationService.GetRecentNotifications(80, cat, unreadOnly, SessionContext.IsAdmin);
            if (filter != "Security") items = items.Where(n => n.IsBusinessProcess).ToList();
            int unread = EventNotificationService.GetBusinessProcessUnreadCount();
            lblCount.Text = unread == 0 ? "all caught up" : $"{unread} unread";
            list.SuspendLayout();
            foreach (Control c in list.Controls) c.Dispose();
            list.Controls.Clear();
            if (items.Count == 0)
            {
                list.Controls.Add(new Label { Text = filter == "Unread" ? "✓ You're all caught up." : "Nothing here yet.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F) });
                list.ResumeLayout();
                return;
            }
            // Docked Top: add in reverse so the newest ends up first.
            var rows = new List<Control>();
            string? lastGroup = null;
            foreach (var n in items.OrderByDescending(x => x.Timestamp))
            {
                string g = n.Timestamp.Date == DateTime.Today ? "Today" : n.Timestamp.Date == DateTime.Today.AddDays(-1) ? "Yesterday" : n.Timestamp.Date >= DateTime.Today.AddDays(-6) ? "This week" : "Earlier";
                if (g != lastGroup)
                {
                    rows.Add(new Label { Text = g.ToUpperInvariant(), Dock = DockStyle.Top, Height = D(26), Font = new Font("Segoe UI Semibold", 8F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(D(4), 0, 0, D(2)) });
                    lastGroup = g;
                }
                rows.Add(new Row(this, n) { Dock = DockStyle.Top });
                rows.Add(new Panel { Dock = DockStyle.Top, Height = D(6), BackColor = AppTheme.Canvas });
            }
            for (int i = rows.Count - 1; i >= 0; i--) list.Controls.Add(rows[i]);
            list.ResumeLayout();
        }
        private void Activate(SystemNotification n)
        {
            EventNotificationService.MarkAsRead(n.Id);
            Visible = false;
            open(n);
        }
        private void Dismiss(SystemNotification n)
        {
            EventNotificationService.DeleteNotification(n.Id);
            Render();
        }
        /// <summary>One notification: type colour, title, two lines of message, time and where clicking goes.</summary>
        private sealed class Row : Control
        {
            private readonly NotificationFlyout owner;
            private readonly SystemNotification n;
            private bool hover, overX;
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            public Row(NotificationFlyout owner, SystemNotification n)
            {
                this.owner = owner; this.n = n;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Height = S(86);
                Cursor = Cursors.Hand;
            }
            private Rectangle XBox => new Rectangle(Width - S(30), S(8), S(22), S(22));
            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; overX = false; Invalidate(); }
            protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); bool x = XBox.Contains(e.Location); if (x != overX) { overX = x; Invalidate(); } }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                if (XBox.Contains(e.Location)) owner.Dismiss(n); else owner.Activate(n);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                try { PaintRow(e.Graphics); }
                catch
                {
                    // Never leave a black, unpainted box: fall back to a plain row.
                    e.Graphics.Clear(AppTheme.Surface);
                    TextRenderer.DrawText(e.Graphics, n.Title ?? "", Font, new Rectangle(S(12), S(8), Width - S(24), Height - S(16)), AppTheme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                }
            }
            private void PaintRow(Graphics g)
            {
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Canvas);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                Color fill = hover ? AppTheme.SurfaceHover : n.IsRead ? AppTheme.Surface : ThemeManager.Mix(AppTheme.Surface, AppTheme.Accent, AppTheme.IsDark ? 0.14 : 0.06);
                using (var p = Gfx.RoundedRect(card, S(8)))
                {
                    using (var b = new SolidBrush(fill)) g.FillPath(b, p);
                    using (var pen = new Pen(hover ? AppTheme.BorderStrong : AppTheme.Border)) g.DrawPath(pen, p);
                }
                Color tone = n.Type switch { ToastType.Success => AppTheme.Success, ToastType.Warning => AppTheme.Warning, ToastType.Error => AppTheme.Danger, _ => AppTheme.Info };
                int d = S(34);
                var circle = new Rectangle(S(12), S(12), d, d);
                using (var b = new SolidBrush(ThemeManager.Mix(AppTheme.Surface, tone, AppTheme.IsDark ? 0.32 : 0.16))) g.FillEllipse(b, circle);
                using (var f = new Font("Segoe UI Emoji", 11F))
                    TextRenderer.DrawText(g, n.IconSymbol, f, circle, tone, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                int x = S(56), right = Width - S(38);
                var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                using var tf = new Font("Segoe UI", 9.5F, n.IsRead ? FontStyle.Regular : FontStyle.Bold);
                using var sf = new Font("Segoe UI", 7.75F);
                string time = n.RelativeTime;
                int tw = TextRenderer.MeasureText(time, sf).Width;
                TextRenderer.DrawText(g, n.Title, tf, new Rectangle(x, S(9), right - x - tw - S(6), S(20)), AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, time, sf, new Rectangle(right - tw, S(11), tw, S(16)), AppTheme.TextMuted, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
                using var mf = new Font("Segoe UI", 8.75F);
                TextRenderer.DrawText(g, n.Message, mf, new Rectangle(x, S(29), right - x, S(34)), AppTheme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
                string link = DeepLinkLabel(n);
                using var lf = new Font("Segoe UI Semibold", 8F);
                TextRenderer.DrawText(g, link, lf, new Rectangle(x, Height - S(22), right - x, S(16)), AppTheme.AccentText, flags);
                if (!n.IsRead) using (var b = new SolidBrush(AppTheme.Accent)) g.FillEllipse(b, S(4), S(26), S(6), S(6));
                if (hover)
                {
                    if (overX) using (var b = new SolidBrush(AppTheme.SurfaceAlt)) g.FillEllipse(b, XBox);
                    using var xf = new Font("Segoe UI", 8F, FontStyle.Bold);
                    TextRenderer.DrawText(g, "✕", xf, XBox, overX ? AppTheme.DangerText : AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }
        /// <summary>"Open dispatch DSP-… ›" style hint for where clicking a notification goes.</summary>
        public static string DeepLinkLabel(SystemNotification n)
        {
            string? id = NotificationLinks.FindRecordId(n);
            string where = (n.TargetModule ?? "").ToLowerInvariant() switch
            {
                "tripreports" => "trip report",
                "dispatch" => "dispatch",
                "loading" => "loading order",
                "billing" or "invoices" => "invoice",
                "collections" => "receipt",
                "incidents" => "incidents",
                "trucks" => "truck",
                "crew" => "crew",
                "settings" => "settings",
                _ => "record"
            };
            return id != null ? $"Open {where} {id}  ›" : (string.IsNullOrWhiteSpace(n.TargetModule) ? "Open  ›" : $"Go to {where}  ›");
        }
    }
    /// <summary>Finds which record a notification is about (stored id, or a number mentioned in its text).</summary>
    public static class NotificationLinks
    {
        private static readonly System.Text.RegularExpressions.Regex IdRx = new System.Text.RegularExpressions.Regex(@"\b(DSP|LS|LOAD|INV|SI|CR|OR|SOA|TRIP|ST|SPT|SP)-[A-Z0-9-]+\b|#([A-Z0-9][A-Z0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        public static string? FindRecordId(SystemNotification n)
        {
            if (!string.IsNullOrWhiteSpace(n.TargetRecordId)) return n.TargetRecordId!.Trim();
            var m = IdRx.Match((n.Title ?? "") + " " + (n.Message ?? ""));
            if (!m.Success) return null;
            return m.Groups[2].Success ? m.Groups[2].Value : m.Value;
        }
    }
}
