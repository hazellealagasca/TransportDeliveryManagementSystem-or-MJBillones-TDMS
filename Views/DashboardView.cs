using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The home page. Top to bottom: a greeting with the date and a period switch (today / tomorrow / this week /
    /// this month); five summary cards that open their page; a "Needs attention" list of things to do next (late
    /// departures, trips without a report, deliveries not billed, overdue invoices…); the schedule for the period;
    /// and on the right the fleet at a glance, the last 7 days of deliveries, money owed and the busiest clients.
    /// What each person sees follows their role. Refreshes itself every minute while open.
    /// </summary>
    public partial class DashboardView : UserControl, IRefreshable
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<string>? OnNavigateRequested { get; set; }

        // ---- data
        private sealed class Snapshot
        {
            public List<Delivery> Deliveries = new();
            public List<LoadingSchedule> Schedules = new();
            public List<Truck> Trucks = new();
            public List<Crew> Crews = new();
            public List<TripReport> TripReports = new();
            public List<IncidentReport> Incidents = new();
            public List<Invoice> Invoices = new();
            public List<CollectionReceipt> Receipts = new();
            public int PendingUsers;
        }
        private Snapshot data = new Snapshot();
        private string scope = "Today";
        private string scheduleTab = "Dispatches";
        private bool loading;
        // ---- controls
        private PageHeader header = null!;
        private readonly Dictionary<string, Button> scopeButtons = new();
        private TileStrip tiles = null!;
        private StatTile tDispatch = null!, tCrates = null!, tTrucks = null!, tCrew = null!, tFifth = null!;
        private ActionList attention = null!;
        private Panel attentionCard = null!;
        private Label lblAttentionCount = null!;
        private DataGridView gridOperations = null!;
        private TextBox txtQuickSearch = null!;
        private readonly Dictionary<string, Button> tabButtons = new();
        private Label lblScheduleTitle = null!, lblScheduleFoot = null!;
        private FleetBar fleetBar = null!;
        private BarChart weekChart = null!;
        private Label lblWeekNote = null!;
        private RankList topClients = null!;
        private Panel? moneyCard;
        private Label lblMoneyDue = null!, lblMoneyOverdue = null!, lblMoneyCollected = null!, lblMoneyDraft = null!;
        private Label lblUpdated = null!;
        private System.Windows.Forms.Timer? refreshTimer;
        private readonly List<Action> fitters = new();
        private bool CanOps => SessionContext.CanAccessOperations;
        private bool CanFin => SessionContext.CanAccessFinance;
        private bool CanRes => SessionContext.CanAccessResources;
        private int D(int v) => PageKit.D(this, v);
        public DashboardView()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.5F);
            BuildUi();
            SetupContextMenu();
            refreshTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
            refreshTimer.Tick += (s, e) => { if (Visible && Parent != null && FindForm()?.WindowState != FormWindowState.Minimized) LoadDashboardData(); };
            refreshTimer.Start();
            LoadDashboardData();
        }

        public void RefreshData() => LoadDashboardData();
        // =====================================================================================================
        // Layout
        // =====================================================================================================
        private void BuildUi()
        {
            SuspendLayout();
            Padding = new Padding(D(4), D(6), D(4), D(4));
            var root = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 5, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, D(14)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, D(96)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, D(14)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // Header
            header = PageKit.Header(Greeting(), $"{DateTime.Today:dddd, MMMM d, yyyy}  ·  here's how your deliveries stand");
            header.Dock = DockStyle.Fill;
            if (CanOps) header.AddActions(PageKit.Action("＋  New loading order", "primary", (s, e) => GoOps("Dispatch")));
            header.AddActions(PageKit.Action("↻  Refresh", "secondary", (s, e) => LoadDashboardData()));
            // One date chip: quick periods, a specific date or a range, and ‹ › to step to the previous / next period.
            dateChip = new DateFilterChip(DatePresets.Dashboard, "Today") { Margin = new Padding(D(8), D(2), 0, 0) };
            new ToolTip().SetToolTip(dateChip, "Pick the period to show. Use ‹ › for the previous or next day, week or month.");
            dateChip.RangeChanged += (s, e) => { scope = dateChip.Range.Describe(DatePresets.Dashboard); Render(); };
            header.Actions.Controls.Add(dateChip);
            lblUpdated = new Label { AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = AppTheme.TextMuted, Margin = new Padding(D(8), D(10), 0, 0), UseMnemonic = false };
            header.Actions.Controls.Add(lblUpdated);
            StyleScope();
            root.Controls.Add(header, 0, 0);
            // Summary tiles
            tiles = new TileStrip { Dock = DockStyle.Fill };
            // Only two summary tiles: dispatches in the chosen period, and crew available now (drivers | aides in one box).
            // Crates, trucks-ready and outstanding-balance tiles were removed; the objects stay so the refresh code is unchanged.
            tDispatch = tiles.Add(new StatTile("Dispatches", AppTheme.Info) { NavigateOnClick = CanOps });
            tCrew = tiles.Add(new StatTile("Available crew", AppTheme.Warning) { NavigateOnClick = CanRes, Label1 = "drivers", Label2 = "aides" });
            tCrates = new StatTile("Crates to deliver", AppTheme.Accent);
            tTrucks = new StatTile("Trucks ready", AppTheme.Success);
            tFifth = new StatTile("Outstanding balance", AppTheme.Danger);
            tDispatch.Click += (s, e) => { if (CanOps) GoOps("Dispatch"); };
            tCrates.Click += (s, e) => { if (CanOps) GoOps("Dispatch"); };
            tTrucks.Click += (s, e) => { if (CanRes) MainForm.Instance?.NavigateToResources("Trucks"); };
            tCrew.Click += (s, e) => { if (CanRes) MainForm.Instance?.NavigateToResources("Crew"); };
            tFifth.Click += (s, e) => { if (CanFin) MainForm.Instance?.NavigateToFinance("Billing"); else if (CanOps) GoOps("TripReports"); };
            root.Controls.Add(tiles, 0, 2);
            // Body: left (attention + schedule) / right (insights)
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(14)));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.Controls.Add(BuildLeftColumn(), 0, 0);
            body.Controls.Add(BuildRightColumn(), 2, 0);
            root.Controls.Add(body, 0, 4);
            // The whole page scrolls as one: header, tiles and both columns move together.
            pageRoot = root;
            scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0, 0, D(6), 0) };
            scroller.Controls.Add(root);
            scroller.Resize += (s, e) => FitPage();
            Controls.Add(scroller);
            ResumeLayout(true);
            HandleCreated += (s, e) =>
            {
                root.RowStyles[1].Height = D(14);
                root.RowStyles[2].Height = D(96);
                root.RowStyles[3].Height = D(14);
                body.ColumnStyles[1].Width = D(14);
                FitPage();
            };
        }
        private Panel scroller = null!;
        private TableLayoutPanel pageRoot = null!;
        private Panel rightCol = null!;
        private Control schedCardRef = null!;
        private bool fitting;
        /// <summary>Sizes the page to its content (at least the window height) so one scrollbar moves everything.</summary>
        private void FitPage()
        {
            if (fitting || scroller == null || pageRoot == null || attentionCard == null || rightCol == null) return;
            fitting = true;
            try
            {
                int top = header.Height + D(14) + D(96) + D(14) + pageRoot.Padding.Vertical;
                int left = attentionCard.Height + D(14) + D(460);
                int right = 0;
                foreach (Control c in rightCol.Controls) if (c.Visible) right += c.Height;
                right += rightCol.Padding.Vertical;
                int view = scroller.ClientSize.Height;
                int want = Math.Max(view, top + Math.Max(left, right) + D(6));
                int width = scroller.ClientSize.Width - scroller.Padding.Horizontal;
                if (pageRoot.Height != want) pageRoot.Height = want;
                if (want > view) width = Math.Max(1, scroller.ClientSize.Width - scroller.Padding.Horizontal);
                if (pageRoot.Width != width && width > 0) pageRoot.Width = width;
            }
            finally { fitting = false; }
        }

        private Control BuildLeftColumn()
        {
            var col = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0) };
            // Needs attention
            attention = new ActionList { Dock = DockStyle.Top };
            attentionCard = Card("Needs attention", "What to do next, most urgent first. Click a row to go straight there.", attention, out var attnHead);
            attentionCard.Dock = DockStyle.Top;
            attentionCard.SizeChanged += (s, e) => FitPage();
            lblAttentionCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextMuted, Anchor = AnchorStyles.Right, UseMnemonic = false };
            attnHead.Controls.Add(lblAttentionCount, 1, 0);

            // Schedule
            var bar = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(8)) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(240)));
            var tabsFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Color.Transparent };
            foreach (var key in new[] { "Dispatches", "Loading orders" })
            {
                var b = new Button { Text = key, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(D(90), D(32)), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(4), 0), UseMnemonic = false };
                b.Click += (s, e) => { scheduleTab = key; StyleTabs(); RenderSchedule(); };
                tabButtons[key] = b;
                tabsFlow.Controls.Add(b);
            }
            StyleTabs();
            txtQuickSearch = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, D(2), 0, 0) };
            PageKit.StyleSearch(txtQuickSearch, "Filter this list…");
            txtQuickSearch.TextChanged += (s, e) => RenderSchedule();
            bar.Controls.Add(tabsFlow, 0, 0);
            bar.Controls.Add(new Panel { Width = 1, Height = 1, BackColor = Color.Transparent }, 1, 0);
            bar.Controls.Add(txtQuickSearch, 2, 0);
            gridOperations = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None
            };
            UIThemeHelper.ApplyGridTheme(gridOperations);
            gridOperations.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenRow(gridOperations.Rows[e.RowIndex].Tag); };
            gridOperations.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && gridOperations.CurrentRow != null) { OpenRow(gridOperations.CurrentRow.Tag); e.Handled = true; e.SuppressKeyPress = true; } };
            lblScheduleFoot = new Label { Dock = DockStyle.Bottom, Height = D(28), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            var schedBody = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            schedBody.Controls.Add(gridOperations);
            schedBody.Controls.Add(lblScheduleFoot);
            schedBody.Controls.Add(bar);
            var schedCard = Card("Schedule", "", schedBody, out var schedHead);
            schedCardRef = schedCard;
            lblScheduleTitle = (Label)schedHead.GetControlFromPosition(0, 0)!;
            schedCard.Dock = DockStyle.Fill;
            var gap = new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = Color.Transparent };
            col.Controls.Add(schedCard);
            col.Controls.Add(gap);
            col.Controls.Add(attentionCard);
            HandleCreated += (s, e) => gap.Height = D(14);
            return col;
        }
        private Control BuildRightColumn()
        {
            var col = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, D(2), 0) };
            rightCol = col;
            var stack = new List<Control>();
            // Fleet
            fleetBar = new FleetBar { Dock = DockStyle.Top, Height = D(92) };
            var fleetCard = Card("Trucks available", "Trucks free to dispatch now, and what the rest are doing.", fleetBar, out _, "Trucks ›", CanRes ? () => MainForm.Instance?.NavigateToResources("Trucks") : null);
            stack.Add(fleetCard);
            // Money (finance roles)
            if (CanFin)
            {
                var grid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 2, Height = D(118), BackColor = Color.Transparent };
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                grid.Controls.Add(Figure("Still to collect", out lblMoneyDue), 0, 0);
                grid.Controls.Add(Figure("Overdue", out lblMoneyOverdue), 1, 0);
                grid.Controls.Add(Figure("Collected this month", out lblMoneyCollected), 0, 1);
                grid.Controls.Add(Figure("Draft invoices", out lblMoneyDraft), 1, 1);
                moneyCard = Card("Billing", "Invoices and payments.", grid, out _, "Open ›", () => MainForm.Instance?.NavigateToFinance("Billing"));
                stack.Add(moneyCard);
            }
            // Last 7 days
            weekChart = new BarChart { Dock = DockStyle.Top, Height = D(150) };
            lblWeekNote = new Label { Dock = DockStyle.Top, Height = D(24), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false };
            var weekBody = new Panel { Dock = DockStyle.Top, Height = D(176), BackColor = Color.Transparent };
            lblWeekNote.Dock = DockStyle.Bottom;
            weekChart.Dock = DockStyle.Fill;
            weekBody.Controls.Add(weekChart);
            weekBody.Controls.Add(lblWeekNote);
            // "Last 7 days" chart is no longer shown on the dashboard (kept off-screen so the refresh code stays simple).
            // "Busiest clients this month" is no longer shown on the dashboard; the list is kept off-screen so refresh code stays simple.
            topClients = new RankList { Dock = DockStyle.Top, Height = D(180), Visible = false };
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                stack[i].Dock = DockStyle.Top;
                if (i < stack.Count - 1) col.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = Color.Transparent });
                col.Controls.Add(stack[i]);
                stack[i].SizeChanged += (s, e) => FitPage();
            }
            // Added last-to-first, so the first card (highest index) docks at the top.
            return col;
        }
        /// <summary>A white rounded card: title (+ optional link) and subtitle, then the content.</summary>
        private Panel Card(string title, string subtitle, Control content, out TableLayoutPanel head, string? linkText = null, Action? link = null)
        {
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(D(18), D(14), D(18), D(14)), Margin = new Padding(0) };
            card.Paint += (s, e) =>
            {
                Gfx.HighQuality(e.Graphics);
                using var path = Gfx.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), D(12));
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawPath(pen, path);
            };
            card.Resize += (s, e) => card.Invalidate();
            head = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0, 0, 0, D(10)) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var t = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0), UseMnemonic = false };
            head.Controls.Add(t, 0, 0);
            if (!string.IsNullOrEmpty(subtitle))
                head.Controls.Add(new Label { Text = subtitle, AutoSize = true, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(2), 0, 0), UseMnemonic = false }, 0, 1);
            if (linkText != null && link != null)
            {
                var l = new LinkLabel { Text = linkText, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, LinkBehavior = LinkBehavior.HoverUnderline, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, D(3), 0, 0) };
                l.LinkClicked += (s, e) => link();
                head.Controls.Add(l, 1, 0);
            }
            content.Dock = content.Dock == DockStyle.None ? DockStyle.Top : content.Dock;
            card.Controls.Add(content);
            card.Controls.Add(head);
            if (content.Dock == DockStyle.Top)
            {
                var h = head;
                void Fit()
                {
                    int ph = h.GetPreferredSize(new Size(Math.Max(1, card.ClientSize.Width - card.Padding.Horizontal), 0)).Height;
                    int want = card.Padding.Vertical + ph + content.Height;
                    if (card.Height != want) card.Height = want;
                }
                content.SizeChanged += (s, e) => Fit();
                card.SizeChanged += (s, e) => { if (card.Width > 0) Fit(); };
                card.HandleCreated += (s, e) => Fit();
                fitters.Add(Fit);
            }
            return card;
        }
        private Control Figure(string caption, out Label value)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 0, D(8), D(6)) };
            value = new Label { Dock = DockStyle.Top, Height = D(28), Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary, Text = "—", UseMnemonic = false };
            var cap = new Label { Dock = DockStyle.Top, Height = D(20), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Text = caption, UseMnemonic = false };
            p.Controls.Add(value);
            p.Controls.Add(cap);
            return p;
        }
        private void StyleScope()
        {
        }
        private void StyleTabs()
        {
            foreach (var kv in tabButtons) Seg(kv.Value, kv.Key == scheduleTab);
        }
        private static void Seg(Button b, bool on)
        {
            b.BackColor = on ? AppTheme.Accent : AppTheme.Surface;
            b.ForeColor = on ? AppTheme.OnAccent : AppTheme.TextSecondary;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = on ? AppTheme.Accent : AppTheme.Border;
            b.FlatAppearance.MouseOverBackColor = on ? AppTheme.AccentHover : AppTheme.SurfaceHover;
        }
        private static string Greeting()
        {
            int h = DateTime.Now.Hour;
            string part = h < 12 ? "Good morning" : h < 18 ? "Good afternoon" : "Good evening";
            string full = (SessionContext.ActiveUser?.FullName ?? "").Trim();
            full = System.Text.RegularExpressions.Regex.Replace(full, @"\s*\([^)]*\)\s*$", "");
            if (full.Contains(',')) full = full.Split(',', 2)[1].Trim();
            string first = full.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            first = first.Length == 0 ? "" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(first.ToLowerInvariant());
            return first.Length > 0 ? $"{part}, {first}" : part;
        }
        // =====================================================================================================
        // Data
        // =====================================================================================================
        private async void LoadDashboardData()
        {
            if (loading) return;
            loading = true;
            try
            {
                bool ops = CanOps, fin = CanFin, res = CanRes, admin = SessionContext.IsAdmin;
                var snap = await Task.Run(() =>
                {
                    var s = new Snapshot();
                    T Safe<T>(Func<T> f, T fallback) { try { return f(); } catch { return fallback; } }
                    s.Deliveries = Safe(MJdatabase.GetDeliveries, new List<Delivery>());
                    s.Schedules = Safe(MJdatabase.GetLoadingSchedules, new List<LoadingSchedule>());
                    s.Trucks = Safe(MJdatabase.GetTrucks, new List<Truck>());
                    s.Crews = Safe(MJdatabase.GetCrews, new List<Crew>());
                    if (ops || fin) s.TripReports = Safe(MJdatabase.GetTripReports, new List<TripReport>());
                    if (ops) s.Incidents = Safe(MJdatabase.GetIncidentReports, new List<IncidentReport>());
                    s.Invoices = Safe(MJdatabase.GetInvoices, new List<Invoice>());
                    if (fin) s.Receipts = Safe(MJdatabase.GetCollectionReceipts, new List<CollectionReceipt>());
                    if (admin) s.PendingUsers = Safe(MJdatabase.GetPendingUsersCount, 0);
                    return s;
                });
                if (IsDisposed || !IsHandleCreated) return;
                data = snap;
                header.Title.Text = Greeting();
                Render();
                lblUpdated.Text = $"Updated {DateTime.Now:h:mm tt}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] {ex.Message}");
            }
            finally { loading = false; }
        }
        private DateFilterChip? dateChip;
        private (DateTime from, DateTime to) ScopeRange()
        {
            var r = dateChip?.Range ?? DateRange.Preset("Today");
            return (r.From ?? DateTime.Today, r.To ?? DateTime.Today);
        }
        /// <summary>"today", "this week", "on Oct 30", "Oct 1 – 15" for tile captions.</summary>
        private string WhenText()
        {
            var r = dateChip?.Range ?? DateRange.Preset("Today");
            foreach (var p in DatePresets.Dashboard) if (DateRange.Preset(p).SameAs(r)) return p.ToLowerInvariant();
            string d = r.Describe();
            return r.From == r.To ? "on " + d : d;
        }
        private static bool Active(string? s) => RowMenu.Is(s, "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched", "Delayed");
        private static bool Done(string? s) => DispatchGate.IsCompleted(s);
        private static DateTime Departure(DateTime date, string? time)
        {
            if (DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) || DateTime.TryParse(time, out t))
                return date.Date + t.TimeOfDay;
            return date.Date.AddHours(8);
        }
        private static string Peso(decimal v) => v >= 1_000_000 ? $"₱{v / 1_000_000m:0.##}M" : $"₱{v:N0}";
        private void Render()
        {
            StyleScope();
            StyleTabs();
            var (from, to) = ScopeRange();
            var dels = data.Deliveries;
            var inScope = dels.Where(d => d.DispatchDate.Date >= from && d.DispatchDate.Date <= to && !DispatchGate.IsCancelled(d.Status)).ToList();
            string when = WhenText();
            // Tiles
            tDispatch.Caption = $"Dispatches {when}";
            tDispatch.Value = inScope.Count.ToString("N0");
            tDispatch.Note = $"{inScope.Count(d => RowMenu.Is(d.Status, "In Transit", "Delayed"))} on the road · {inScope.Count(d => Done(d.Status))} delivered · {inScope.Count(d => RowMenu.Is(d.Status, "Scheduled", "Pending", "Rescheduled"))} waiting";
            int crates = inScope.Sum(d => d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate);
            tCrates.Caption = $"Crates {when}";
            tCrates.Value = crates.ToString("N0");
            tCrates.Note = $"G {inScope.Sum(d => d.GCrate):N0}  ·  E {inScope.Sum(d => d.ECrate):N0}";
            var busyTrucks = dels.Where(d => Active(d.Status)).Select(d => d.TruckId).Where(i => i > 0).ToHashSet();
            var busyCrew = new HashSet<int>();
            foreach (var d in dels.Where(d => Active(d.Status))) foreach (var (id, _) in MJdatabase.GetDeliveryCrewIds(d)) busyCrew.Add(id);
            int ready = data.Trucks.Count(t => MJdatabase.IsTruckReadyStatus(t.Status) && !busyTrucks.Contains(t.Id));
            int maint = data.Trucks.Count(t => MJdatabase.IsTruckBlockedStatus(t.Status) && !RowMenu.Is(t.Status, "Inactive"));
            tTrucks.Value = $"{ready} of {data.Trucks.Count}";
            tTrucks.Note = $"{data.Trucks.Count(t => busyTrucks.Contains(t.Id))} on dispatches · {maint} in maintenance";
            tTrucks.ValueColor = ready == 0 && data.Trucks.Count > 0 ? AppTheme.DangerText : null;
            bool IsDriver(Crew c) => (c.Role ?? "").IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
            var drivers = data.Crews.Where(c => c.IsActive && IsDriver(c)).ToList();
            var aides = data.Crews.Where(c => c.IsActive && !IsDriver(c)).ToList();
            int freeDrivers = drivers.Count(c => !busyCrew.Contains(c.Id));
            int freeAides = aides.Count(c => !busyCrew.Contains(c.Id));
            tCrew.Caption = "Available crew";
            tCrew.Value = freeDrivers.ToString("N0");
            tCrew.Note = $"of {drivers.Count} drivers · {drivers.Count - freeDrivers} on a trip";
            tCrew.ValueColor = freeDrivers == 0 && drivers.Count > 0 ? AppTheme.DangerText : null;
            tCrew.Value2 = freeAides.ToString("N0");
            tCrew.Note2 = $"of {aides.Count} aides · {aides.Count - freeAides} on a trip";
            tCrew.ValueColor2 = freeAides == 0 && aides.Count > 0 ? AppTheme.DangerText : null;
            var paidByInvoice = data.Receipts.GroupBy(r => r.InvoiceId).ToDictionary(g => g.Key, g => g.Sum(r => r.AmountPaid));
            decimal Balance(Invoice i) => Math.Max(0, i.NetAmount - (paidByInvoice.TryGetValue(i.Id, out var p) ? p : 0));
            var openInv = data.Invoices.Where(i => RowMenu.Is(i.Status, "Unpaid", "Partial", "Overdue")).ToList();
            var overdue = openInv.Where(i => RowMenu.Is(i.Status, "Overdue") || i.DueDate.Date < DateTime.Today).ToList();
            var reported = new HashSet<int>(data.TripReports.Select(t => t.DeliveryId));
            var reportedNums = new HashSet<string>(data.TripReports.Select(t => t.DispatchNumber ?? ""), StringComparer.OrdinalIgnoreCase);
            var invoicedIds = new HashSet<int>(data.Invoices.Select(i => i.DeliveryId).Where(i => i > 0));
            var needReport = dels.Where(d => Done(d.Status) && !reported.Contains(d.Id) && !reportedNums.Contains(d.DispatchNumber ?? "")).OrderBy(d => d.DispatchDate).ToList();
            if (CanFin)
            {
                decimal due = openInv.Sum(Balance);
                tFifth.Value = Peso(due);
                tFifth.Note = $"{openInv.Count} open invoice(s) · {overdue.Count} overdue";
                tFifth.ValueColor = overdue.Count > 0 ? AppTheme.DangerText : null;
            }
            else
            {
                tFifth.Value = needReport.Count.ToString("N0");
                tFifth.Note = needReport.Count == 0 ? "all delivered trips are reported" : "delivered trips without a report";
                tFifth.ValueColor = needReport.Count > 0 ? AppTheme.WarningText : null;
            }
            // Needs attention
            var items = new List<ActionList.Item>();
            var now = DateTime.Now;
            if (CanOps)
            {
                foreach (var d in dels.Where(d => RowMenu.Is(d.Status, "Scheduled", "Pending", "Rescheduled") && Departure(d.DispatchDate, d.DepartureTime) < now.AddMinutes(-30)).OrderBy(d => Departure(d.DispatchDate, d.DepartureTime)).Take(4))
                {
                    var dd = d;
                    var dep = Departure(d.DispatchDate, d.DepartureTime);
                    items.Add(new ActionList.Item(AppTheme.Danger, $"{d.DispatchNumber} hasn't left yet", $"Was due {dep:MMM d, h:mm tt} · {d.ClientName} · {d.TruckInfo}", "Open", () => Forms.DispatchDetailsDialog.ShowFor(this, dd)));
                }
                foreach (var d in dels.Where(d => RowMenu.Is(d.Status, "Delayed") || (RowMenu.Is(d.Status, "In Transit") && d.DispatchDate.Date < DateTime.Today)).Take(3))
                {
                    var dd = d;
                    items.Add(new ActionList.Item(AppTheme.Warning, $"{d.DispatchNumber} is still on the road", $"Left {d.DispatchDate:MMM d} · {d.ClientName} · mark it completed once delivered", "Open", () => Forms.DispatchDetailsDialog.ShowFor(this, dd)));
                }
                if (needReport.Count == 1)
                {
                    var dd = needReport[0];
                    items.Add(new ActionList.Item(AppTheme.Warning, $"{dd.DispatchNumber} needs its trip report", $"Delivered {dd.DispatchDate:MMM d} · {dd.ClientName}", "File report", () => MainForm.Instance?.OpenTripReportForDispatch(dd)));
                }
                else if (needReport.Count > 1)
                    items.Add(new ActionList.Item(AppTheme.Warning, $"{needReport.Count} delivered trips need a trip report", $"Oldest: {needReport[0].DispatchNumber} from {needReport[0].DispatchDate:MMM d}", "File reports", () => GoOps("TripReports")));
                var dispatchedSched = new HashSet<int>(dels.Where(d => !DispatchGate.IsCancelled(d.Status)).Select(d => d.LoadingScheduleId));
                var undispatched = data.Schedules.Where(s => RowMenu.Is(s.Status, "Pending", "Scheduled", "Rescheduled") && !dispatchedSched.Contains(s.Id) && s.ScheduleDate.Date <= DateTime.Today.AddDays(1)).OrderBy(s => s.ScheduleDate).ToList();
                if (undispatched.Count == 1)
                {
                    var ls = undispatched[0];
                    items.Add(new ActionList.Item(AppTheme.Info, $"{ls.ScheduleNumber} has no dispatch yet", $"{ls.ClientName} · {ls.ScheduleDate:ddd, MMM d} {ls.DepartureTime} · {ls.TotalCrates} crates", "Dispatch", () => MainForm.Instance?.DispatchLoadingOrder(ls)));
                }
                else if (undispatched.Count > 1)
                    items.Add(new ActionList.Item(AppTheme.Info, $"{undispatched.Count} loading orders have no dispatch yet", $"For today and tomorrow · earliest {undispatched[0].ScheduleNumber} ({undispatched[0].ClientName})", "Dispatch", () => GoOps("Dispatch")));
                int openInc = data.Incidents.Count(i => !RowMenu.Is(i.Status, "Resolved", "Closed"));
                if (openInc > 0)
                    items.Add(new ActionList.Item(AppTheme.Info, $"{openInc} incident(s) still open", $"Damaged or missing crates: {data.Incidents.Where(i => !RowMenu.Is(i.Status, "Resolved", "Closed")).Sum(i => i.DamagedQuantity):N0} · est. {Peso(data.Incidents.Where(i => !RowMenu.Is(i.Status, "Resolved", "Closed")).Sum(i => i.EstimatedCost))}", "Review", () => GoOps("Incidents")));
            }
            if (CanFin)
            {
                if (overdue.Count > 0)
                    items.Insert(Math.Min(items.Count, 2), new ActionList.Item(AppTheme.Danger, $"{overdue.Count} invoice(s) overdue", $"{Peso(overdue.Sum(Balance))} past due · oldest {overdue.Min(i => i.DueDate):MMM d} ({overdue.OrderBy(i => i.DueDate).First().ClientName})", "Collect", () => MainForm.Instance?.ShowFinanceRecords("Billing", "Overdue")));
                var unbilled = dels.Where(d => Done(d.Status) && !invoicedIds.Contains(d.Id)).ToList();
                var billedOnSoa = SafeBilled();
                unbilled = unbilled.Where(d => !billedOnSoa.ContainsKey(d.Id)).ToList();
                if (unbilled.Count > 0)
                    items.Add(new ActionList.Item(AppTheme.Warning, $"{unbilled.Count} delivered trip(s) not billed yet", $"{unbilled.Select(d => d.ClientName).Distinct().Count()} client(s) · put them on a statement of account", "Bill", () => MainForm.Instance?.NavigateToFinance("StatementOfAccount")));
                int drafts = data.Invoices.Count(i => RowMenu.Is(i.Status, "Draft"));
                if (drafts > 0)
                    items.Add(new ActionList.Item(AppTheme.Info, $"{drafts} draft invoice(s) to finish", "From completed special trips — add the amount to issue them", "Open", () => MainForm.Instance?.ShowFinanceRecords("Billing", "Draft")));
            }
            if (CanRes && maint > 0)
                items.Add(new ActionList.Item(AppTheme.TextMuted, $"{maint} truck(s) under maintenance", string.Join(", ", data.Trucks.Where(t => MJdatabase.IsTruckBlockedStatus(t.Status) && !RowMenu.Is(t.Status, "Inactive")).Select(t => t.PlateNumber).Take(5)), "Trucks", () => MainForm.Instance?.NavigateToResources("Trucks")));
            if (SessionContext.IsAdmin && data.PendingUsers > 0)
                items.Add(new ActionList.Item(AppTheme.Warning, $"{data.PendingUsers} account(s) waiting for approval", "New staff sign-ups need an administrator", "Review", () => MainForm.Instance?.OpenUserAccounts()));
            attention.SetItems(items, 6);
            foreach (var f in fitters) f();
            FitPage();
            lblAttentionCount.Text = items.Count == 0 ? "" : $"{items.Count} item{(items.Count == 1 ? "" : "s")}";
            // Fleet
            fleetBar.SetSegments(new List<(string, int, Color)>
            {
                ("Available", ready, AppTheme.Success),
                ("On dispatches", data.Trucks.Count(t => busyTrucks.Contains(t.Id)), AppTheme.Info),
                ("Maintenance", maint, AppTheme.Warning),
                ("Inactive", data.Trucks.Count(t => RowMenu.Is(t.Status, "Inactive")), AppTheme.BorderStrong)
            });
            // Last 7 days
            var days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(i - 6)).ToList();
            var perDay = days.Select(day => dels.Count(d => Done(d.Status) && d.DispatchDate.Date == day)).ToList();
            weekChart.SetData(days.Select(d => d == DateTime.Today ? "Today" : d.ToString("ddd")).ToList(), perDay, days.Count - 1);
            int weekCrates = dels.Where(d => Done(d.Status) && d.DispatchDate.Date >= days[0]).Sum(d => d.TotalCrates);
            lblWeekNote.Text = $"{perDay.Sum()} completed · {weekCrates:N0} crates delivered";
            // Top clients
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var top = dels.Where(d => d.DispatchDate.Date >= monthStart && !DispatchGate.IsCancelled(d.Status) && !string.IsNullOrWhiteSpace(d.ClientName))
                .GroupBy(d => d.ClientName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.Key, g.Count(), g.Sum(d => d.TotalCrates)))
                .OrderByDescending(x => x.Item2).Take(5).ToList();
            topClients.SetRows(top.Select(x => (x.Key, x.Item2, $"{x.Item2} trip{(x.Item2 == 1 ? "" : "s")} · {x.Item3:N0} crates")).ToList(),
                name => { if (SessionContext.CanAccessMasterData) MainForm.Instance?.OpenClientProfile(name); });
            // Money
            if (moneyCard != null)
            {
                lblMoneyDue.Text = Peso(openInv.Sum(Balance));
                lblMoneyOverdue.Text = Peso(overdue.Sum(Balance));
                lblMoneyOverdue.ForeColor = overdue.Count > 0 ? AppTheme.DangerText : AppTheme.TextPrimary;
                lblMoneyCollected.Text = Peso(data.Receipts.Where(r => r.PaymentDate >= monthStart).Sum(r => r.AmountPaid));
                lblMoneyCollected.ForeColor = AppTheme.SuccessText;
                lblMoneyDraft.Text = data.Invoices.Count(i => RowMenu.Is(i.Status, "Draft")).ToString("N0");
            }
            RenderSchedule();
        }

        private static Dictionary<int, string> SafeBilled()
        {
            try { return MJdatabase.GetBilledDeliveryIds(0); } catch { return new Dictionary<int, string>(); }
        }

        // =====================================================================================================
        // Schedule table
        // =====================================================================================================
        private void RenderSchedule()
        {
            var (from, to) = ScopeRange();
            string q = txtQuickSearch.Text.Trim().ToLowerInvariant();
            string when = WhenText();
            gridOperations.SuspendLayout();
            gridOperations.Rows.Clear();
            gridOperations.Columns.Clear();
            DataGridViewTextBoxColumn Col(string name, string header, float weight, int min, bool right = false)
            {
                var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight, MinimumWidth = D(min), SortMode = DataGridViewColumnSortMode.Automatic };
                if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                return c;
            }
            int shown;
            if (scheduleTab == "Dispatches")
            {
                gridOperations.Columns.AddRange(Col("When", "When", 11, 90), Col("No", "Dispatch #", 12, 100), Col("Client", "Client", 20, 130), Col("Route", "Route / destination", 18, 120),
                    Col("Truck", "Truck", 11, 85), Col("Driver", "Driver", 14, 110), Col("Crates", "Crates", 7, 60, true), Col("Status", "Status", 11, 105));
                var list = data.Deliveries.Where(d => d.DispatchDate.Date >= from && d.DispatchDate.Date <= to)
                    .OrderBy(d => DispatchGate.IsCancelled(d.Status) ? 1 : 0).ThenBy(d => Departure(d.DispatchDate, d.DepartureTime)).ToList();
                if (q.Length > 0) list = list.Where(d => $"{d.DispatchNumber} {d.ClientName} {d.Destination} {d.TruckInfo} {d.DriverName} {d.AideNames} {d.Status}".ToLowerInvariant().Contains(q)).ToList();
                foreach (var d in list)
                {
                    var dep = Departure(d.DispatchDate, d.DepartureTime);
                    string whenTxt = from == to ? dep.ToString("h:mm tt") : dep.ToString("ddd d · h:mm tt");
                    int i = gridOperations.Rows.Add(whenTxt, d.DispatchNumber, d.ClientName, d.CleanRouteDisplay, d.TruckInfo, d.DriverName, (d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate).ToString("N0"), d.Status);
                    gridOperations.Rows[i].Tag = d;
                    if (DispatchGate.IsCancelled(d.Status)) gridOperations.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
                }
                shown = list.Count;
                lblScheduleTitle.Text = $"Dispatches {when}";
            }
            else
            {
                gridOperations.Columns.AddRange(Col("When", "When", 11, 90), Col("No", "Loading order #", 13, 110), Col("Client", "Client", 22, 130), Col("Route", "Route / destination", 22, 130),
                    Col("Cargo", "Cargo", 11, 90), Col("Crates", "Crates", 8, 60, true), Col("Dispatch", "Dispatch", 11, 90), Col("Status", "Status", 11, 105));
                var dispatchBySched = data.Deliveries.Where(d => !DispatchGate.IsCancelled(d.Status) && d.LoadingScheduleId > 0)
                    .GroupBy(d => d.LoadingScheduleId).ToDictionary(g => g.Key, g => g.First().DispatchNumber);
                var list = data.Schedules.Where(s => s.ScheduleDate.Date >= from && s.ScheduleDate.Date <= to)
                    .OrderBy(s => RowMenu.Is(s.Status, "Cancelled") ? 1 : 0).ThenBy(s => Departure(s.ScheduleDate, s.DepartureTime)).ToList();
                if (q.Length > 0) list = list.Where(s => $"{s.ScheduleNumber} {s.ClientName} {s.Destination} {s.Status} {s.CargoType}".ToLowerInvariant().Contains(q)).ToList();
                foreach (var s in list)
                {
                    var dep = Departure(s.ScheduleDate, s.DepartureTime);
                    string whenTxt = from == to ? dep.ToString("h:mm tt") : dep.ToString("ddd d · h:mm tt");
                    int i = gridOperations.Rows.Add(whenTxt, s.ScheduleNumber, s.ClientName, CleanDest(s.Destination), s.CargoType, s.TotalCrates.ToString("N0"),
                        dispatchBySched.TryGetValue(s.Id, out var dn) ? dn : "—", s.Status);
                    gridOperations.Rows[i].Tag = s;
                    if (RowMenu.Is(s.Status, "Cancelled")) gridOperations.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
                }
                shown = list.Count;
                lblScheduleTitle.Text = $"Loading orders {when}";
            }

            gridOperations.ResumeLayout();
            UIThemeHelper.ApplyGridTheme(gridOperations);
            gridOperations.ClearSelection();
            lblScheduleFoot.Text = shown == 0
                ? (q.Length > 0 ? $"Nothing matches \"{txtQuickSearch.Text.Trim()}\"." : $"Nothing scheduled {when}.")
                : $"{shown} {(scheduleTab == "Dispatches" ? "dispatch" : "loading order")}{(shown == 1 ? "" : "es".Substring(scheduleTab == "Dispatches" ? 0 : 1))}  ·  double-click or Enter to open, right-click for more";
        }

        private static string CleanDest(string? dest)
        {
            if (string.IsNullOrWhiteSpace(dest)) return "";
            int colon = dest.IndexOf(':');
            return colon > 0 ? dest.Substring(0, colon).Trim() : dest.Trim();
        }

        private void OpenRow(object? item)
        {
            if (item is Delivery d) Forms.DispatchDetailsDialog.ShowFor(this, d);
            else if (item != null) RecordDetailsDialog.ShowDetails(this, item);
        }

        private void GoOps(string tab)
        {
            if (MainForm.Instance != null) MainForm.Instance.NavigateToOperations(tab);
            else OnNavigateRequested?.Invoke(tab == "Dispatch" ? "Dispatch" : tab);
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridOperations, (m, rows) =>
            {
                var item = rows[0].Tag;
                if (item is Delivery d)
                {
                    m.Items.AddRange(RowMenu.Header($"🚚  {d.DispatchNumber}", $"{RowMenu.Short(d.ClientName, 26)} · {d.DispatchDate:MMM d} {d.DepartureTime} · {d.TruckInfo} · {d.Status}"));
                    m.Items.Add(RowMenu.Item("Open dispatch details", () => Forms.DispatchDetailsDialog.ShowFor(this, d), "Enter"));
                    m.Items.Add(RowMenu.Item("Edit in Dispatch", () => MainForm.Instance?.EditDeliveryOrderInDispatch(d), "", CanOps && !RowMenu.Is(d.Status, "Completed", "Cancelled")));
                    if (Done(d.Status)) m.Items.Add(RowMenu.Item("Trip report", () => MainForm.Instance?.OpenTripReportForDispatch(d), "", CanOps));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.CopySub(this, ("Dispatch number", d.DispatchNumber), ("Crew contacts", Forms.DispatchDetailsDialog.BuildCrewContactsText(d)), ("Summary", Forms.DispatchDetailsDialog.BuildSummaryText(d))));
                    m.Items.Add(RowMenu.Item("Print delivery dossier", () => PrintHelper.PrintEndToEndDeliveryDossier(d.Id, d.DispatchNumber)));
                }
                else if (item is LoadingSchedule ls)
                {
                    bool hasDispatch = data.Deliveries.Any(x => x.LoadingScheduleId == ls.Id && !DispatchGate.IsCancelled(x.Status));
                    m.Items.AddRange(RowMenu.Header($"📦  {ls.ScheduleNumber}", $"{RowMenu.Short(ls.ClientName, 26)} · {ls.ScheduleDate:MMM d} {ls.DepartureTime} · {ls.TotalCrates} crates · {ls.Status}"));
                    m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, ls), "Enter"));
                    if (!hasDispatch && !RowMenu.Is(ls.Status, "Completed", "Cancelled"))
                        m.Items.Add(RowMenu.Good("Create dispatch for this order", () => MainForm.Instance?.DispatchLoadingOrder(ls), CanOps));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.CopySub(this, ("Loading order number", ls.ScheduleNumber), ("Client", ls.ClientName), ("Destination", CleanDest(ls.Destination))));
                }
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.ExportSub(gridOperations, scheduleTab == "Dispatches" ? "Dispatch_Schedule" : "Loading_Schedule", lblScheduleTitle.Text,
                    () => PrintHelper.PrintGridReport(lblScheduleTitle.Text, gridOperations, $"{scope} · printed {DateTime.Now:MMM d, yyyy h:mm tt}")));
            });

        }
    }
    // =========================================================================================================
    // Small painted controls used by the dashboard
    // =========================================================================================================
    /// <summary>Rows of "do this next" items: coloured dot, title, detail, and an action button-like label.</summary>
    public sealed class ActionList : Control
    {
        public sealed record Item(Color Tone, string Title, string Detail, string ActionText, Action Run);
        private List<Item> items = new();
        private int hover = -1, max = 6;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => S(54);
        public ActionList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = AppTheme.Surface;
        }
        public void SetItems(List<Item> list, int maxRows)
        {
            items = list;
            max = maxRows;
            int rows = Math.Max(1, Math.Min(max, items.Count));
            Height = rows * RowH + (items.Count > max ? S(26) : 0);
            hover = -1;
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = e.Y / RowH;
            int nh = i >= 0 && i < Math.Min(max, items.Count) ? i : -1;
            if (nh != hover) { hover = nh; Invalidate(); }
            Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int i = e.Y / RowH;
            if (i >= 0 && i < Math.Min(max, items.Count))
            {
                try { items[i].Run(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Surface);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            if (items.Count == 0)
            {
                using var ok = new SolidBrush(AppTheme.Success);
                g.FillEllipse(ok, S(4), (RowH - S(22)) / 2, S(22), S(22));
                using var cf = new Font("Segoe UI Semibold", 9F);
                TextRenderer.DrawText(g, "✓", cf, new Rectangle(S(4), (RowH - S(22)) / 2, S(22), S(22)), Color.White, flags | TextFormatFlags.HorizontalCenter);
                using var tf0 = new Font("Segoe UI Semibold", 10.5F);
                TextRenderer.DrawText(g, "All caught up — nothing needs your attention right now.", tf0, new Rectangle(S(38), 0, Width - S(40), RowH), AppTheme.TextPrimary, flags);
                return;
            }
            using var tf = new Font("Segoe UI Semibold", 10F);
            using var sf = new Font("Segoe UI", 8.75F);
            using var af = new Font("Segoe UI Semibold", 9F);
            int n = Math.Min(max, items.Count);
            for (int i = 0; i < n; i++)
            {
                var it = items[i];
                var r = new Rectangle(0, i * RowH, Width, RowH);
                if (i == hover)
                {
                    using var hp = Gfx.RoundedRect(new Rectangle(r.X, r.Y + S(2), r.Width - 1, r.Height - S(4)), S(8));
                    using var hb = new SolidBrush(AppTheme.SurfaceHover);
                    g.FillPath(hb, hp);
                }
                else if (i > 0)
                {
                    using var lp = new Pen(AppTheme.Border);
                    g.DrawLine(lp, S(12), r.Y, r.Right - S(12), r.Y);
                }
                using (var db = new SolidBrush(it.Tone)) g.FillEllipse(db, S(12), r.Y + (RowH - S(10)) / 2, S(10), S(10));
                var asz = TextRenderer.MeasureText(it.ActionText + "  ›", af);
                int textRight = r.Right - asz.Width - S(28);
                TextRenderer.DrawText(g, it.Title, tf, new Rectangle(S(32), r.Y + S(6), textRight - S(32), S(22)), AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, it.Detail, sf, new Rectangle(S(32), r.Y + S(27), textRight - S(32), S(20)), AppTheme.TextMuted, flags);
                var ar = new Rectangle(r.Right - asz.Width - S(20), r.Y + (RowH - S(30)) / 2, asz.Width + S(12), S(30));
                using (var ap = Gfx.RoundedRect(ar, S(8)))
                {
                    using var ab = new SolidBrush(i == hover ? AppTheme.Accent : AppTheme.AccentSoft);
                    g.FillPath(ab, ap);
                }
                TextRenderer.DrawText(g, it.ActionText + "  ›", af, ar, i == hover ? AppTheme.OnAccent : AppTheme.AccentText, flags | TextFormatFlags.HorizontalCenter);
            }
            if (items.Count > max)
            {
                using var mf = new Font("Segoe UI", 8.75F);
                TextRenderer.DrawText(g, $"+ {items.Count - max} more", mf, new Rectangle(S(32), n * RowH, Width - S(32), S(26)), AppTheme.TextMuted, flags);
            }
        }
    }
    /// <summary>One horizontal bar split by category, with a legend underneath.</summary>
    public sealed class FleetBar : Control
    {
        private List<(string Label, int Value, Color Color)> segs = new();
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public FleetBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = AppTheme.Surface;
        }
        public void SetSegments(List<(string, int, Color)> s) { segs = s; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Surface);
            int total = segs.Sum(s => s.Value);
            var bar = new Rectangle(0, S(4), Width - 1, S(16));
            using (var bp = Gfx.RoundedRect(bar, S(8)))
            {
                using var bg = new SolidBrush(AppTheme.SurfaceAlt);
                g.FillPath(bg, bp);
                if (total > 0)
                {
                    var clip = g.Clip;
                    g.SetClip(bp);
                    float x = bar.X;
                    foreach (var s in segs.Where(s => s.Value > 0))
                    {
                        float w = bar.Width * s.Value / (float)total;
                        using var b = new SolidBrush(s.Color);
                        g.FillRectangle(b, x, bar.Y, w + 1, bar.Height);
                        x += w;
                    }
                    g.Clip = clip;
                }
            }
            using var lf = new Font("Segoe UI", 8.75F);
            using var vf = new Font("Segoe UI Semibold", 12F);
            int colW = Math.Max(1, Width / Math.Max(1, segs.Count));
            for (int i = 0; i < segs.Count; i++)
            {
                var s = segs[i];
                int x = i * colW;
                using (var db = new SolidBrush(s.Color)) g.FillEllipse(db, x, S(34), S(9), S(9));
                TextRenderer.DrawText(g, s.Label, lf, new Rectangle(x + S(14), S(28), colW - S(14), S(20)), AppTheme.TextMuted, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, s.Value.ToString("N0"), vf, new Rectangle(x, S(50), colW, S(28)), AppTheme.TextPrimary, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
            }
        }
    }
    /// <summary>Simple vertical bars with the value on top and a label under each; one bar can be highlighted.</summary>
    public sealed class BarChart : Control
    {
        private List<string> labels = new();
        private List<int> values = new();
        private int highlight = -1;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public BarChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = AppTheme.Surface;
        }
        public void SetData(List<string> l, List<int> v, int highlightIndex) { labels = l; values = v; highlight = highlightIndex; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Surface);
            if (values.Count == 0) return;
            int maxV = Math.Max(1, values.Max());
            int labelH = S(20), valueH = S(18);
            int plotTop = valueH, plotBottom = Height - labelH - S(2);
            int slot = Width / values.Count;
            int bw = Math.Min(S(34), (int)(slot * 0.6));
            using var lf = new Font("Segoe UI", 8.5F);
            using var vf = new Font("Segoe UI Semibold", 8.75F);
            using (var axis = new Pen(AppTheme.Border)) g.DrawLine(axis, 0, plotBottom, Width, plotBottom);
            for (int i = 0; i < values.Count; i++)
            {
                int cx = i * slot + slot / 2;
                int h = (int)((plotBottom - plotTop) * values[i] / (float)maxV);
                if (values[i] > 0) h = Math.Max(h, S(4));
                var r = new Rectangle(cx - bw / 2, plotBottom - h, bw, h);
                if (h > 0)
                {
                    using var p = Gfx.RoundedRect(new Rectangle(r.X, r.Y, r.Width, r.Height + S(4)), S(5));
                    using var b = new SolidBrush(i == highlight ? AppTheme.Accent : ThemeManager.Mix(AppTheme.Surface, AppTheme.Accent, 0.38));
                    var clip = g.Clip;
                    g.SetClip(new Rectangle(0, 0, Width, plotBottom));
                    g.FillPath(b, p);
                    g.Clip = clip;
                }
                TextRenderer.DrawText(g, values[i].ToString(), vf, new Rectangle(cx - slot / 2, plotBottom - h - valueH, slot, valueH), values[i] > 0 ? AppTheme.TextPrimary : AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, labels[i], lf, new Rectangle(cx - slot / 2, plotBottom + S(2), slot, labelH), i == highlight ? AppTheme.AccentText : AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
    /// <summary>Ranked rows with a proportional bar; clicking a row calls back with its name.</summary>
    public sealed class RankList : Control
    {
        private List<(string Name, int Value, string Detail)> rows = new();
        private Action<string>? onClick;
        private int hover = -1;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => S(36);
        public RankList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = AppTheme.Surface;
        }
        public void SetRows(List<(string, int, string)> r, Action<string> click)
        {
            rows = r;
            onClick = click;
            Height = Math.Max(1, rows.Count) * RowH;
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = e.Y / RowH;
            int nh = i >= 0 && i < rows.Count ? i : -1;
            if (nh != hover) { hover = nh; Invalidate(); }
            Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int i = e.Y / RowH;
            if (i >= 0 && i < rows.Count) onClick?.Invoke(rows[i].Name);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Surface);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            using var nf = new Font("Segoe UI Semibold", 9.5F);
            using var df = new Font("Segoe UI", 8.5F);
            if (rows.Count == 0)
            {
                TextRenderer.DrawText(g, "No dispatches yet this month.", df, new Rectangle(0, 0, Width, RowH), AppTheme.TextMuted, flags);
                return;
            }
            int maxV = Math.Max(1, rows.Max(r => r.Value));
            for (int i = 0; i < rows.Count; i++)
            {
                var r = new Rectangle(0, i * RowH, Width, RowH);
                int barW = (int)((Width - S(4)) * rows[i].Value / (float)maxV);
                using (var bp = Gfx.RoundedRect(new Rectangle(0, r.Y + S(3), Math.Max(S(10), barW), RowH - S(6)), S(6)))
                {
                    using var bb = new SolidBrush(ThemeManager.Mix(AppTheme.Surface, AppTheme.Accent, i == hover ? 0.22 : 0.11));
                    g.FillPath(bb, bp);
                }
                var dsz = TextRenderer.MeasureText(rows[i].Detail, df);
                TextRenderer.DrawText(g, $"{i + 1}.  {rows[i].Name}", nf, new Rectangle(S(8), r.Y, Width - dsz.Width - S(20), RowH), i == hover ? AppTheme.AccentText : AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, rows[i].Detail, df, new Rectangle(Width - dsz.Width - S(8), r.Y, dsz.Width + S(4), RowH), AppTheme.TextSecondary, flags);
            }
        }
    }
}
