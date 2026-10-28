using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// The top bar: page title with the signed-in user under it (avatar, name, role — click for the profile),
    /// one search box in the middle that finds anything in the system, and the bell / log out on the right.
    /// The scheduled / in transit / unpaid counters are no longer shown here.
    /// </summary>
    public partial class MainForm
    {
        private GlobalSearchBox? globalSearch;
        private UserBadge? userBadge;
        public void NavigateHome() => OpenDashboard();
        public void OpenUserAccounts() => btnNavUsers_Click(this, EventArgs.Empty);
        public void DispatchLoadingOrder(LoadingSchedule ls) => GetOperationsHub("Dispatch")?.DispatchLoadingSchedule(ls);
        private void InitHeaderRedesign()
        {
            // Counters removed from the top bar (they live on the Dashboard).
            foreach (var b in new[] { btnKpiScheduled, btnKpiInTransit, btnKpiUnpaid }) if (b != null) b.Visible = false;
            // User block under the page title
            lblUserInfo.Visible = false;
            userBadge = new UserBadge(currentUser) { Location = new Point(S(58), S(34)) };
            userBadge.Click += (s, e) => { ShowUserProfileDialog(); userBadge.SetUser(currentUser); };
            ttSidebar.SetToolTip(userBadge, "Your profile, password and sign-out");
            pnlHeaderLeft.Controls.Add(userBadge);
            userBadge.BringToFront();
            lblHeaderTitle.Location = new Point(S(60), S(6));
            // Search in the middle
            txtGlobalSearch.Visible = false;
            btnGlobalSearch.Visible = false;
            lstSearchDropdown.Visible = false;
            pnlSearchCenter.Visible = true;
            globalSearch = new GlobalSearchBox();
            pnlSearchCenter.Controls.Add(globalSearch);
            globalSearch.BringToFront();
            pnlSearchCenter.Resize += (s, e) => PlaceGlobalSearch();
            panelHeader.Resize += (s, e) => PlaceGlobalSearch();
            PlaceGlobalSearch();
            panelMainContent.MouseDown += (s, e) => globalSearch?.HideResults();
            panelSidebar.MouseDown += (s, e) => globalSearch?.HideResults();
            this.KeyDown += (s, e) =>
            {
                if ((e.Control && e.KeyCode == Keys.K) || e.KeyCode == Keys.F1)
                {
                    globalSearch?.FocusSearch();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            this.Move += (s, e) => globalSearch?.HideResults();
        }
        /// <summary>Centers the search box on the whole top bar (not just the free space), shrinking it when room is tight.</summary>
        private void PlaceGlobalSearch()
        {
            if (globalSearch == null) return;
            int h = S(40);
            int avail = pnlSearchCenter.ClientSize.Width - S(24);
            int w = Math.Max(S(220), Math.Min(S(560), avail));
            // Try to center on the full header; fall back to the free area if that would overlap either side.
            int headerCenter = panelHeader.ClientSize.Width / 2;
            int xInHeader = headerCenter - w / 2;
            int x = xInHeader - pnlSearchCenter.Left;
            if (x < S(12) || x + w > pnlSearchCenter.ClientSize.Width - S(12)) x = Math.Max(S(12), (pnlSearchCenter.ClientSize.Width - w) / 2);
            int y = Math.Max(0, (panelHeader.Height - h) / 2 - pnlSearchCenter.Top);
            globalSearch.SetBounds(x, y, w, h);
            globalSearch.Visible = avail >= S(200);
        }
    }
    /// <summary>Avatar with initials, the user's name and a role pill. Clickable.</summary>
    public sealed class UserBadge : Control
    {
        private string name = "", role = "", initials = "";
        private bool hover;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public UserBadge(User? u)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => { hover = true; Invalidate(); };
            MouseLeave += (s, e) => { hover = false; Invalidate(); };
            SetUser(u);
        }
        public void SetUser(User? u)
        {
            string full = (u?.FullName ?? u?.Username ?? "User").Trim();
            full = Regex.Replace(full, @"\s*\([^)]*\)\s*$", "").Trim();          // "Hazel Lagasca (Dispatcher)" -> "Hazel Lagasca"
            if (full.Contains(','))                                                  // "LAGASCA, HAZEL" -> "Hazel Lagasca"
            {
                var parts = full.Split(',', 2);
                full = (parts[1].Trim() + " " + parts[0].Trim()).Trim();
            }
            name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(full.ToLowerInvariant());
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            initials = words.Length == 0 ? "?" : (words.Length == 1 ? words[0].Substring(0, 1) : words[0].Substring(0, 1) + words[^1].Substring(0, 1)).ToUpperInvariant();
            string r = (u?.Role ?? "Staff").Trim();
            role = SessionContext.IsAdminRole(r) ? "Administrator" : string.Join(" · ", r.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            FitSize();
            Invalidate();
        }
        private void FitSize()
        {
            using var nf = new Font("Segoe UI Semibold", 9.5F);
            using var rf = new Font("Segoe UI Semibold", 8F);
            int w = S(26) + S(8) + TextRenderer.MeasureText(name, nf).Width + S(8) + TextRenderer.MeasureText(role, rf).Width + S(16) + S(18);
            Size = new Size(w, S(26));
        }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); FitSize(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            if (hover)
            {
                using var hp = Gfx.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
                using var hb = new SolidBrush(AppTheme.SurfaceHover);
                g.FillPath(hb, hp);
            }
            int a = S(22), ay = (Height - a) / 2;
            using (var ab = new SolidBrush(AppTheme.Accent)) g.FillEllipse(ab, S(2), ay, a, a);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            using (var inf = new Font("Segoe UI Semibold", 7.5F))
                TextRenderer.DrawText(g, initials, inf, new Rectangle(S(2), ay, a, a), AppTheme.OnAccent, flags | TextFormatFlags.HorizontalCenter);
            int x = S(2) + a + S(8);
            using var nf = new Font("Segoe UI Semibold", 9.5F);
            var ns = TextRenderer.MeasureText(name, nf);
            TextRenderer.DrawText(g, name, nf, new Rectangle(x, 0, ns.Width + S(2), Height), hover ? AppTheme.AccentText : AppTheme.TextPrimary, flags);
            x += ns.Width + S(6);
            using var rf = new Font("Segoe UI Semibold", 8F);
            var rs = TextRenderer.MeasureText(role, rf);
            var pill = new Rectangle(x, (Height - S(18)) / 2, rs.Width + S(12), S(18));
            using (var pp = Gfx.RoundedRect(pill, S(9)))
            {
                using var pb = new SolidBrush(AppTheme.AccentSoft);
                g.FillPath(pb, pp);
            }
            TextRenderer.DrawText(g, role, rf, pill, AppTheme.AccentText, flags | TextFormatFlags.HorizontalCenter);
            x = pill.Right + S(6);
            using var cf = new Font("Segoe UI", 7.5F);
            TextRenderer.DrawText(g, "▾", cf, new Rectangle(x, 0, S(12), Height), AppTheme.TextMuted, flags);
        }
    }
}
