using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Order History: every loading order, dispatch and special trip in one easy list.
    /// Each record is one readable two-line row (number + client on top; date, route, truck, crew and crates below; status on the right),
    /// so nothing is cut off. Click a row to see everything about it in the panel on the right; double-click to open the full record.
    /// Tabs: All · Dispatches · Loading orders · Special trips · Pinned · Recently viewed. One search box finds anything,
    /// including dates like "oct 5". "Find a transaction" helps when the number is forgotten.
    /// </summary>
    public sealed class OrderHistoryView : UserControl, IRefreshable
    {
        private readonly SegmentTabs tabs = new SegmentTabs("All", "✅ Completed", "Dispatches", "Loading orders", "Special trips", "📌 Pinned", "🕘 Recently viewed");
        private const int TabCompleted = 1, TabPinned = 5, TabRecent = 6;
        private static readonly string[] TabKinds = { "", "", TxRecord.KDispatch, TxRecord.KLoading, TxRecord.KSpecial, "", "" };
        private static readonly string[] StatusOptions = { "All statuses", "Waiting", "On the road", "Completed", "Cancelled" };
        private static readonly string[] GroupOptions = { "By date", "By month", "By client", "By status", "By type", "No groups" };
        private static readonly string[] SortOptions = { "Newest first", "Oldest first", "Client A–Z", "Most crates", "Number" };
        private TextBox txtSearch = null!;
        private DateFilterChip dateChip = null!;
        private ChoiceChip statusChip = null!, groupChip = null!, sortChip = null!;
        private LinkLabel lnkClear = null!;
        private DataGridView grid = null!;
        private Label lblFoot = null!, lblEmpty = null!;
        private Panel preview = null!;
        private Button btnRefresh = null!;
        private List<TxRecord> all = new List<TxRecord>();
        private List<TxRecord> shown = new List<TxRecord>();
        private HashSet<string> pinned = new HashSet<string>();
        private List<string> recent = new List<string>();
        private readonly HashSet<string> collapsed = new HashSet<string>();
        private readonly System.Windows.Forms.Timer searchDelay = new System.Windows.Forms.Timer { Interval = 250 };
        private TxRecord? selected;
        private bool rendering;
        private bool CanFin => SessionContext.CanAccessFinance;
        private string User => SessionContext.CurrentUsername;
        private const int MaxRows = 2000;
        private int D(int v) => PageKit.D(this, v);
        private sealed class GroupTag { public string Key = ""; public string Title = ""; public int Count; public int Crates; }
        public OrderHistoryView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.5F);
            Padding = new Padding(D(4), D(8), D(4), D(4));
            DoubleBuffered = true;
            BuildUi();
            searchDelay.Tick += (s, e) => { searchDelay.Stop(); Render(); };
            Disposed += (s, e) => searchDelay.Dispose();
            LoadData();
        }
        public void RefreshData() => LoadData();
        /// <summary>Completed Dispatches now live here: these hand the work to the other Operations / Finance screens.</summary>
        public Action<Delivery>? OnCreateInvoiceRequested;
        public Action<int, string>? OnIncidentsRequested;
        public Action<int, string>? OnTripReportRequested;
        /// <summary>Opens the "Completed" tab (used where the old Completed Dispatches screen was opened).</summary>
        public void ShowCompleted() { tabs.Selected = TabCompleted; Render(); }
        // =====================================================================================================
        // Layout
        // =====================================================================================================
        private void BuildUi()
        {
            var header = PageKit.Header("Order History", "Every loading order, dispatch and special trip. Search, click a row to see the details, double-click to open it.");
            var btnFind = PageKit.Action("🔎  Find a transaction…", "primary", (s, e) => OpenFinder());
            var more = new ContextMenuStrip();
            more.Items.Add("⬇  Export this list to CSV…", null, (s, e) => ExportCsv());
            more.Items.Add("🕓  Change log (who changed what)", null, (s, e) => OpenChangeLog());
            UIThemeHelper.ApplyContextMenuTheme(more);
            Button? btnMore = null;
            btnMore = PageKit.Action("More  ▾", "secondary", (s, e) => more.Show(btnMore!, new Point(0, btnMore!.Height)));
            btnRefresh = PageKit.Action("↻", "secondary", (s, e) => LoadData());
            new ToolTip().SetToolTip(btnRefresh, "Refresh");
            header.AddActions(btnFind, btnMore, btnRefresh);
            tabs.SelectedChanged += (s, e) => { collapsed.Clear(); Render(); };
            // Search + filters on one line
            var bar = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent, Padding = new Padding(0, D(12), 0, D(10)) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.Controls.Add(BuildSearchBox(), 0, 0);
            var chips = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(D(10), 0, 0, 0), Anchor = AnchorStyles.Right };
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(0, D(2), D(8), 0) };
            dateChip.RangeChanged += (s, e) => { dateChip.FitWidth(); Render(); };
            statusChip = new ChoiceChip("", StatusOptions, "") { Margin = new Padding(0, D(2), D(8), 0) };
            statusChip.SelectionChanged += (s, e) => { statusChip.FitWidth(); Render(); };
            sortChip = new ChoiceChip("", SortOptions, "⇅") { Margin = new Padding(0, D(2), D(8), 0) };
            sortChip.SelectionChanged += (s, e) => { sortChip.FitWidth(); Render(); };
            groupChip = new ChoiceChip("", GroupOptions, "▤") { Margin = new Padding(0, D(2), D(6), 0) };
            groupChip.SelectionChanged += (s, e) => { groupChip.FitWidth(); collapsed.Clear(); Render(); };
            new ToolTip().SetToolTip(statusChip, "Show only one status");
            new ToolTip().SetToolTip(sortChip, "Order of the list");
            new ToolTip().SetToolTip(groupChip, "How the list is grouped");
            lnkClear = new LinkLabel { Text = "Clear", AutoSize = true, Margin = new Padding(D(2), D(9), 0, 0), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, LinkBehavior = LinkBehavior.HoverUnderline, Visible = false };
            lnkClear.LinkClicked += (s, e) => ClearFilters();
            chips.Controls.AddRange(new Control[] { dateChip, statusChip, sortChip, groupChip, lnkClear });
            bar.Controls.Add(chips, 1, 0);
            // Body: list (left) + details preview (right)
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(12)));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(330)));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.Controls.Add(BuildList(), 0, 0);
            preview = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(18), D(16), D(18), D(14)), AutoScroll = true };
            preview.Paint += (s, e) => { Gfx.HighQuality(e.Graphics); using var path = Gfx.RoundedRect(new Rectangle(0, 0, preview.Width - 1, preview.Height - 1), D(10)); using var pen = new Pen(AppTheme.Border); e.Graphics.DrawPath(pen, path); };
            body.Controls.Add(preview, 2, 0);
            Controls.Add(body);
            Controls.Add(bar);
            Controls.Add(tabs);
            Controls.Add(PageKit.Gap(this, 8));
            Controls.Add(header);
            HandleCreated += (s, e) =>
            {
                body.ColumnStyles[1].Width = D(12);
                body.ColumnStyles[2].Width = D(330);
                foreach (var c in new FilterChip[] { dateChip, statusChip, sortChip, groupChip }) c.FitWidth();
                ShowPreview(null);
            };
        }
        private Control BuildSearchBox()
        {
            var box = new Panel { Dock = DockStyle.Fill, Height = D(38), BackColor = AppTheme.InputBack, Margin = new Padding(0, D(2), 0, 0), Padding = new Padding(D(36), D(9), D(30), D(4)) };
            box.Paint += (s, e) =>
            {
                Gfx.HighQuality(e.Graphics);
                using var path = Gfx.RoundedRect(new Rectangle(0, 0, box.Width - 1, box.Height - 1), D(8));
                using var pen = new Pen(txtSearch != null && txtSearch.Focused ? AppTheme.Accent : AppTheme.BorderStrong, txtSearch != null && txtSearch.Focused ? 1.6f : 1f);
                e.Graphics.DrawPath(pen, path);
                using var f = new Font("Segoe UI Symbol", 11F);
                TextRenderer.DrawText(e.Graphics, "🔍", f, new Rectangle(D(8), 0, D(26), box.Height), AppTheme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            txtSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 10.5F), MaxLength = 120,
                PlaceholderText = "Search: number, client, truck, driver, route, crates, amount, or a date like oct 5" };
            var clear = new Label { Text = "✕", Dock = DockStyle.Right, Width = D(22), TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.TextMuted, Cursor = Cursors.Hand, Visible = false, Font = new Font("Segoe UI", 9F) };
            clear.Click += (s, e) => { txtSearch.Clear(); txtSearch.Focus(); };
            txtSearch.TextChanged += (s, e) => { clear.Visible = txtSearch.TextLength > 0; searchDelay.Stop(); searchDelay.Start(); };
            txtSearch.GotFocus += (s, e) => box.Invalidate();
            txtSearch.LostFocus += (s, e) => box.Invalidate();
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Enter) { searchDelay.Stop(); Render(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down && grid.Rows.Count > 0) { grid.Focus(); e.SuppressKeyPress = true; }
            };
            box.Controls.Add(txtSearch);
            box.Controls.Add(clear);
            box.Click += (s, e) => txtSearch.Focus();
            return box;
        }
        private Control BuildList()
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(2), D(2), D(2), 0) };
            card.Paint += (s, e) => { Gfx.HighQuality(e.Graphics); using var path = Gfx.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), D(10)); using var pen = new Pen(AppTheme.Border); e.Graphics.DrawPath(pen, path); };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
                RowHeadersVisible = false, ColumnHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.None,
                ScrollBars = ScrollBars.Vertical, StandardTab = true
            };
            UIThemeHelper.ApplyGridTheme(grid);
            grid.BackgroundColor = AppTheme.Surface;
            grid.DefaultCellStyle.BackColor = AppTheme.Surface;
            grid.RowTemplate.Height = D(60);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pin", FillWeight = 1, MinimumWidth = D(40), Width = D(40), AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "main", FillWeight = 100, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.RowPrePaint += Grid_RowPrePaint;
            grid.CellPainting += Grid_CellPainting;
            grid.SelectionChanged += (s, e) =>
            {
                if (rendering) return;
                var row = grid.CurrentRow;
                if (row?.Tag is GroupTag) return;
                ShowPreview(row?.Tag as TxRecord);
            };
            grid.CellMouseMove += (s, e) => grid.Cursor = e.RowIndex >= 0 && (grid.Rows[e.RowIndex].Tag is GroupTag || e.ColumnIndex == 0) ? Cursors.Hand : Cursors.Default;
            grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = grid.Rows[e.RowIndex];
                if (row.Tag is GroupTag g) { if (!collapsed.Remove(g.Key)) collapsed.Add(g.Key); Render(); return; }
                if (row.Tag is TxRecord r && e.ColumnIndex == 0) TogglePin(r);
            };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex > 0 && grid.Rows[e.RowIndex].Tag is TxRecord r) OpenRecord(r); };
            grid.KeyDown += (s, e) =>
            {
                if (grid.CurrentRow?.Tag is TxRecord r)
                {
                    if (e.KeyCode == Keys.Enter) { OpenRecord(r); e.Handled = e.SuppressKeyPress = true; }
                    else if (e.KeyCode == Keys.P && e.Control) { TogglePin(r); e.Handled = e.SuppressKeyPress = true; }
                }
                else if (grid.CurrentRow?.Tag is GroupTag g && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)) { if (!collapsed.Remove(g.Key)) collapsed.Add(g.Key); Render(); e.Handled = e.SuppressKeyPress = true; }
            };
            grid.CellMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0 || grid.Rows[e.RowIndex].Tag is not TxRecord r) return;
                grid.CurrentCell = grid.Rows[e.RowIndex].Cells[1];
                var m = new ContextMenuStrip();
                m.Items.Add("Open full record", null, (a, b) => OpenRecord(r));
                m.Items.Add(pinned.Contains(r.Key) ? "Unpin" : "📌  Pin to the top", null, (a, b) => TogglePin(r));
                m.Items.Add("Copy number", null, (a, b) => CopyNumber(r));
                if (r.Source is Delivery del && r.StatusGroup == "Completed")
                {
                    m.Items.Add(new ToolStripSeparator());
                    if (CanFin && string.IsNullOrWhiteSpace(r.InvoiceNo)) m.Items.Add("🧾  Create billing invoice", null, (a, b) => OnCreateInvoiceRequested?.Invoke(del));
                    m.Items.Add("📝  View trip report", null, (a, b) => OnTripReportRequested?.Invoke(del.Id, del.DispatchNumber));
                    m.Items.Add("🖨  Print trip report", null, (a, b) => PrintHelper.PrintOfficialTripReport(del.Id, del.DispatchNumber));
                    m.Items.Add("🖨  Print dossier", null, (a, b) => PrintHelper.PrintEndToEndDeliveryDossier(del.Id, del.DispatchNumber));
                    m.Items.Add("⚠  Incident log", null, (a, b) => OnIncidentsRequested?.Invoke(del.Id, del.DispatchNumber));
                }
                m.Items.Add(new ToolStripSeparator());
                foreach (var it in RelatedItems(r)) m.Items.Add(it);
                UIThemeHelper.ApplyContextMenuTheme(m);
                m.Closed += (a, b) => BeginInvoke(new Action(() => m.Dispose()));
                m.Show(grid, grid.PointToClient(Cursor.Position));
            };
            lblEmpty = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10.5F), Visible = false, BackColor = AppTheme.Surface, UseMnemonic = false };
            lblFoot = new Label { Dock = DockStyle.Bottom, Height = D(32), Padding = new Padding(D(12), 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, BackColor = AppTheme.Surface };
            card.Controls.Add(grid);
            card.Controls.Add(lblEmpty);
            card.Controls.Add(lblFoot);
            return card;
        }
        private IEnumerable<ToolStripItem> RelatedItems(TxRecord r)
        {
            if (!string.IsNullOrWhiteSpace(r.Client)) yield return new ToolStripMenuItem($"Everything for {Short(r.Client, 30)}", null, (a, b) => SearchFor(r.Client));
            if (!string.IsNullOrWhiteSpace(r.Truck)) yield return new ToolStripMenuItem($"Everything with truck {Short(r.Truck.Split(' ')[0], 16)}", null, (a, b) => SearchFor(r.Truck.Split(' ')[0]));
            if (!string.IsNullOrWhiteSpace(r.Driver)) yield return new ToolStripMenuItem($"Everything with {Short(r.Driver, 24)}", null, (a, b) => SearchFor(r.Driver));
            yield return new ToolStripMenuItem($"Everything on {r.Date:MMM d, yyyy}", null, (a, b) => { txtSearch.Clear(); searchDelay.Stop(); tabs.Selected = 0; dateChip.Range = new DateRange(r.Date.Date, r.Date.Date, "day"); });
        }
        private void SearchFor(string text) { tabs.Selected = 0; txtSearch.Text = text; searchDelay.Stop(); Render(); }
        // =====================================================================================================
        // Data
        // =====================================================================================================
        private async void LoadData()
        {
            if (btnRefresh != null) btnRefresh.Enabled = false;
            try
            {
                bool fin = CanFin;
                var rows = await Task.Run(() => TransactionIndex.LoadAll(fin));
                if (IsDisposed) return;
                all = rows;
                LoadMarks();
                Render();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[OrderHistory] " + ex.Message); }
            finally { if (btnRefresh != null && !btnRefresh.IsDisposed) btnRefresh.Enabled = true; }
        }
        private void LoadMarks()
        {
            pinned = new HashSet<string>(MJdatabase.GetRecordMarks(User, "pin").Select(m => m.Kind + "|" + m.Number));
            recent = MJdatabase.GetRecordMarks(User, "recent").Select(m => m.Kind + "|" + m.Number).ToList();
        }
        private IEnumerable<TxRecord> BaseFilter()
        {
            var q = all.AsEnumerable();
            var range = dateChip.Range;
            if (!range.IsAll) q = q.Where(r => range.Contains(r.Date));
            if (statusChip.SelectedIndex > 0) { string sg = StatusOptions[statusChip.SelectedIndex]; q = q.Where(r => r.StatusGroup == sg); }
            string text = txtSearch.Text;
            if (!string.IsNullOrWhiteSpace(text)) q = q.Where(r => TransactionIndex.Matches(r, text));
            return q;
        }
        private IEnumerable<TxRecord> Sorted(IEnumerable<TxRecord> q) => sortChip.SelectedIndex switch
        {
            1 => q.OrderBy(r => r.Date).ThenBy(r => r.Number),
            2 => q.OrderBy(r => r.Client, StringComparer.OrdinalIgnoreCase).ThenByDescending(r => r.Date),
            3 => q.OrderByDescending(r => r.Crates).ThenByDescending(r => r.Date),
            4 => q.OrderBy(r => r.Number, StringComparer.OrdinalIgnoreCase),
            _ => q.OrderByDescending(r => r.Date).ThenByDescending(r => r.Number)
        };
        private static string SmartGroup(DateTime d, out int order)
        {
            var t = DateTime.Today; var day = d.Date;
            int dow = ((int)t.DayOfWeek + 6) % 7;
            var weekStart = t.AddDays(-dow);
            if (day > t.AddDays(1)) { order = 0; return "Upcoming"; }
            if (day == t.AddDays(1)) { order = 1; return "Tomorrow"; }
            if (day == t) { order = 2; return "Today"; }
            if (day == t.AddDays(-1)) { order = 3; return "Yesterday"; }
            if (day >= weekStart) { order = 4; return "Earlier this week"; }
            if (day >= weekStart.AddDays(-7)) { order = 5; return "Last week"; }
            if (day.Year == t.Year && day.Month == t.Month) { order = 6; return "Earlier this month"; }
            if (day.Year >= t.Year - 1) { order = 100 + (t.Year * 12 + t.Month) - (day.Year * 12 + day.Month); return day.ToString("MMMM yyyy"); }
            order = 10000 + (t.Year - day.Year); return day.Year.ToString();
        }
        private string GroupOf(TxRecord r, out IComparable order)
        {
            switch (groupChip.SelectedIndex)
            {
                case 1: order = -(r.Date.Year * 12 + r.Date.Month) * (sortChip.SelectedIndex == 1 ? -1 : 1); return r.Date.ToString("MMMM yyyy");
                case 2: order = (r.Client ?? "").ToUpperInvariant(); return string.IsNullOrWhiteSpace(r.Client) ? "(no client)" : r.Client;
                case 3: order = Array.IndexOf(StatusOptions, r.StatusGroup); return r.StatusGroup;
                case 4: order = r.Kind; return r.Kind + "s";
                case 5: order = 0; return "";
                default:
                    string g = SmartGroup(r.Date, out int o);
                    if (sortChip.SelectedIndex == 1) o = -o;
                    order = o; return g;
            }
        }
        private void Render()
        {
            if (grid == null) return;
            var baseAll = BaseFilter().ToList();
            tabs.Badges = new[] { baseAll.Count, baseAll.Count(r => r.StatusGroup == "Completed"), baseAll.Count(r => r.Kind == TxRecord.KDispatch), baseAll.Count(r => r.Kind == TxRecord.KLoading), baseAll.Count(r => r.Kind == TxRecord.KSpecial),
                baseAll.Count(r => pinned.Contains(r.Key)), baseAll.Count(r => recent.Contains(r.Key)) };
            tabs.Invalidate();
            var q = baseAll.AsEnumerable();
            if (tabs.Selected == TabCompleted) q = q.Where(r => r.StatusGroup == "Completed");
            else if (tabs.Selected is >= 2 and <= 4) { string k = TabKinds[tabs.Selected]; q = q.Where(r => r.Kind == k); }
            else if (tabs.Selected == TabPinned) q = q.Where(r => pinned.Contains(r.Key));
            else if (tabs.Selected == TabRecent) q = q.Where(r => recent.Contains(r.Key)).OrderBy(r => recent.IndexOf(r.Key));
            shown = tabs.Selected == TabRecent ? q.ToList() : Sorted(q).ToList();
            bool filtersOn = !string.IsNullOrWhiteSpace(txtSearch.Text) || !dateChip.Range.IsAll || statusChip.SelectedIndex > 0;
            lnkClear.Visible = filtersOn || groupChip.SelectedIndex > 0 || sortChip.SelectedIndex > 0;
            string? keepKey = (grid.CurrentRow?.Tag as TxRecord)?.Key ?? selected?.Key;
            int firstVisible = grid.FirstDisplayedScrollingRowIndex;
            var rows = new List<DataGridViewRow>();
            bool grouped = groupChip.SelectedIndex != 5 && tabs.Selected != TabRecent && tabs.Selected != TabPinned;
            void AddGroup(string key, string title, List<TxRecord> items, bool header)
            {
                if (header)
                {
                    var gr = new DataGridViewRow();
                    gr.CreateCells(grid);
                    gr.Height = D(36);
                    gr.Tag = new GroupTag { Key = key, Title = title, Count = items.Count, Crates = items.Sum(i => i.Crates) };
                    rows.Add(gr);
                    if (collapsed.Contains(key)) return;
                }
                foreach (var r in items)
                {
                    if (rows.Count >= MaxRows) return;
                    var row = new DataGridViewRow();
                    row.CreateCells(grid, "", "");
                    row.Height = D(60);
                    row.Tag = r;
                    rows.Add(row);
                }
            }
            if (tabs.Selected == TabRecent || tabs.Selected == TabPinned) AddGroup("list", "", shown, false);
            else
            {
                var pins = shown.Where(r => pinned.Contains(r.Key)).ToList();
                if (pins.Count > 0) AddGroup("📌", "📌  Pinned", pins, true);
                var rest = shown.Where(r => !pinned.Contains(r.Key)).ToList();
                if (!grouped) AddGroup("all", "", rest, false);
                else
                    foreach (var g in rest.Select(r => (r, g: GroupOf(r, out var o), o)).GroupBy(x => x.g).Select(g => (Key: g.Key, Order: g.First().o, Items: g.Select(x => x.r).ToList())).OrderBy(g => g.Order))
                        AddGroup(g.Key, g.Key, g.Items, true);
            }
            rendering = true;
            grid.SuspendLayout();
            grid.Rows.Clear();
            grid.Rows.AddRange(rows.ToArray());
            grid.ResumeLayout();
            DataGridViewRow? keep = keepKey == null ? null : grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is TxRecord t && t.Key == keepKey);
            keep ??= grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is TxRecord);
            if (keep != null) { grid.CurrentCell = keep.Cells[1]; keep.Selected = true; }
            rendering = false;
            var now = keep?.Tag as TxRecord;
            if (now == null || now.Key != selected?.Key || preview.Controls.Count == 0) ShowPreview(now);
            if (firstVisible >= 0 && firstVisible < grid.Rows.Count) try { grid.FirstDisplayedScrollingRowIndex = firstVisible; } catch { }
            lblEmpty.Visible = shown.Count == 0;
            grid.Visible = shown.Count > 0;
            lblEmpty.Text = all.Count == 0 ? "No transactions yet."
                : tabs.Selected == TabPinned && !filtersOn ? "Nothing pinned yet.\nClick the ☆ at the left of any row to keep it here and at the top of the list."
                : tabs.Selected == TabRecent && !filtersOn ? "Records you open will show here."
                : "Nothing matches.\nTry fewer words, another date, or 🔎 Find a transaction.";
            int realRows = rows.Count(r => r.Tag is TxRecord);
            string cut = realRows < shown.Count && collapsed.Count == 0 ? $"   ·   showing the first {MaxRows:N0}, narrow the search to see the rest" : "";
            lblFoot.Text = shown.Count == 0 ? "" : $"{shown.Count:N0} of {all.Count:N0} transactions   ·   {shown.Sum(r => r.Crates):N0} crates{cut}";
            if (shown.Count == 0) ShowPreview(null);
        }
        // =====================================================================================================
        // Painting
        // =====================================================================================================
        private void Grid_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count || grid.Rows[e.RowIndex].Tag is not GroupTag g) return;
            var b = e.RowBounds;
            using (var bg = new SolidBrush(AppTheme.SurfaceAlt)) e.Graphics.FillRectangle(bg, b);
            using (var line = new Pen(AppTheme.Border)) e.Graphics.DrawLine(line, b.Left, b.Bottom - 1, b.Right, b.Bottom - 1);
            string title = $"{(collapsed.Contains(g.Key) ? "▸" : "▾")}   {g.Title}";
            string count = $"{g.Count:N0}" + (g.Crates > 0 ? $"  ·  {g.Crates:N0} crates" : "");
            using var f1 = new Font("Segoe UI Semibold", 9.75F);
            using var f2 = new Font("Segoe UI", 8.75F);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine;
            int w = TextRenderer.MeasureText(e.Graphics, title, f1).Width;
            TextRenderer.DrawText(e.Graphics, title, f1, new Rectangle(b.Left + D(14), b.Top, w + D(4), b.Height), AppTheme.TextPrimary, flags);
            TextRenderer.DrawText(e.Graphics, count, f2, new Rectangle(b.Left + D(22) + w, b.Top, Math.Max(1, b.Width - w - D(30)), b.Height), AppTheme.TextMuted, flags);
            e.Handled = true;
        }
        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null || grid.Rows[e.RowIndex].Tag is not TxRecord r) return;
            var g = e.Graphics;
            Gfx.HighQuality(g);
            var b = e.CellBounds;
            bool sel = grid.Rows[e.RowIndex].Selected;
            bool hot = grid.Rows[e.RowIndex] == hoverRow;
            Color back = sel ? AppTheme.AccentSoft : hot ? AppTheme.SurfaceHover : AppTheme.Surface;
            using (var bg = new SolidBrush(back)) g.FillRectangle(bg, b);
            using (var line = new Pen(AppTheme.Border)) g.DrawLine(line, b.Left, b.Bottom - 1, b.Right, b.Bottom - 1);
            if (sel && e.ColumnIndex == 0) using (var bar = new SolidBrush(AppTheme.Accent)) g.FillRectangle(bar, b.Left, b.Top, D(3), b.Height - 1);
            var one = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            if (e.ColumnIndex == 0)
            {
                bool p = pinned.Contains(r.Key);
                using var f = new Font("Segoe UI Symbol", 13F);
                TextRenderer.DrawText(g, p ? "★" : "☆", f, b, p ? AppTheme.Warning : AppTheme.BorderStrong, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.Handled = true;
                return;
            }
            int pad = D(4), top = b.Top + D(9), lineH = D(21);
            // right side: status pill + amount/crates
            using var fPill = new Font("Segoe UI Semibold", 8.5F);
            string st = string.IsNullOrWhiteSpace(r.Status) ? r.StatusGroup : r.Status;
            var (pillBg, pillFg) = r.StatusGroup switch
            {
                "Completed" => (AppTheme.SuccessSoft, AppTheme.SuccessText),
                "Cancelled" => (AppTheme.DangerSoft, AppTheme.DangerText),
                "On the road" => (AppTheme.InfoSoft, AppTheme.InfoText),
                _ => (AppTheme.WarningSoft, AppTheme.WarningText)
            };
            int pw = TextRenderer.MeasureText(g, st, fPill).Width + D(14);
            var pill = new Rectangle(b.Right - pw - D(14), top + D(1), pw, D(22));
            using (var path = Gfx.RoundedRect(pill, D(11))) using (var pb = new SolidBrush(pillBg)) g.FillPath(pb, path);
            TextRenderer.DrawText(g, st, fPill, pill, pillFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            using var fBig = new Font("Segoe UI Semibold", 10.5F);
            using var fNorm = new Font("Segoe UI", 10F);
            using var fSmall = new Font("Segoe UI", 8.75F);
            string right2 = r.Amount.HasValue ? "₱" + r.Amount.Value.ToString("N2") : (r.Crates > 0 ? $"{r.Crates:N0} crates" : "");
            int r2w = string.IsNullOrEmpty(right2) ? 0 : TextRenderer.MeasureText(g, right2, fBig).Width + D(4);
            if (r2w > 0) TextRenderer.DrawText(g, right2, fBig, new Rectangle(b.Right - r2w - D(14), top + lineH, r2w, lineH), r.Amount.HasValue ? AppTheme.TextPrimary : AppTheme.TextSecondary, one | TextFormatFlags.Right);
            int rightEdge = b.Right - Math.Max(pw, r2w) - D(28);
            // line 1: number · kind tag · client
            int x = b.Left + pad;
            int nw = TextRenderer.MeasureText(g, r.Number, fBig).Width;
            TextRenderer.DrawText(g, r.Number, fBig, new Rectangle(x, top, Math.Min(nw + D(4), rightEdge - x), lineH), AppTheme.TextPrimary, one);
            x += Math.Min(nw, rightEdge - x) + D(10);
            string kind = r.Kind;
            int kw = TextRenderer.MeasureText(g, kind, fSmall).Width + D(10);
            if (x + kw < rightEdge)
            {
                var kr = new Rectangle(x, top + D(2), kw, D(19));
                using (var path = Gfx.RoundedRect(kr, D(5))) using (var pen = new Pen(AppTheme.BorderStrong)) g.DrawPath(pen, path);
                TextRenderer.DrawText(g, kind, fSmall, kr, AppTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                x += kw + D(10);
            }
            if (x < rightEdge) TextRenderer.DrawText(g, r.Client, fNorm, new Rectangle(x, top, rightEdge - x, lineH), AppTheme.TextPrimary, one);
            // line 2: date · route · truck · crew · crates
            var parts = new List<string> { r.Date.ToString("ddd, MMM d, yyyy") + (string.IsNullOrWhiteSpace(r.Time) ? "" : "  " + r.Time) };
            // Short facts first, the (often long) route last, so truck and crew are never cut off.
            if (!string.IsNullOrWhiteSpace(r.Truck)) parts.Add(r.Truck.Split('(')[0].Trim());
            if (!string.IsNullOrWhiteSpace(r.Driver)) parts.Add(r.Driver + (CountAides(r) > 0 ? $" +{CountAides(r)} aide{(CountAides(r) == 1 ? "" : "s")}" : ""));
            if (!string.IsNullOrWhiteSpace(r.Route)) parts.Add(r.Route);
            if (r.Amount.HasValue && r.Crates > 0) parts.Add($"{r.Crates:N0} crates");
            TextRenderer.DrawText(g, string.Join("   ·   ", parts), fSmall, new Rectangle(b.Left + pad, top + lineH, Math.Max(1, rightEdge - b.Left - pad), lineH), AppTheme.TextMuted, one);
            e.Handled = true;
        }
        private DataGridViewRow? hoverRow;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            grid.CellMouseEnter += (s, a) => { var nr = a.RowIndex >= 0 ? grid.Rows[a.RowIndex] : null; if (nr != hoverRow) { var old = hoverRow; hoverRow = nr; if (old != null && old.Index >= 0) grid.InvalidateRow(old.Index); if (nr != null) grid.InvalidateRow(nr.Index); } };
            grid.MouseLeave += (s, a) => { var old = hoverRow; hoverRow = null; if (old != null && old.Index >= 0 && old.Index < grid.Rows.Count) grid.InvalidateRow(old.Index); };
        }
        private static int CountAides(TxRecord r) => r.AideCount;
        // =====================================================================================================
        // Details panel
        // =====================================================================================================
        private void ShowPreview(TxRecord? r)
        {
            if (preview == null) return;
            selected = r;
            preview.SuspendLayout();
            foreach (Control c in preview.Controls.Cast<Control>().ToList()) c.Dispose();
            preview.Controls.Clear();
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Color.Transparent };
            Label L(string text, Font f, Color c, int bottom = 0) => new Label { Text = text, AutoSize = true, MaximumSize = new Size(D(290), 0), Font = f, ForeColor = c, Margin = new Padding(0, 0, 0, D(bottom)), UseMnemonic = false };
            if (r == null)
            {
                stack.Controls.Add(L("Details", new Font("Segoe UI Semibold", 12F), AppTheme.TextPrimary, 8));
                stack.Controls.Add(L("Click any row to see everything about it here.", new Font("Segoe UI", 9.5F), AppTheme.TextSecondary, 16));
                stack.Controls.Add(L("Tips", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 4));
                foreach (var tip in new[]
                {
                    "Type anything in the search box: a number, client, plate, driver, or a date such as \"oct 5\".",
                    "Use ‹ › beside the date to step day by day or month by month.",
                    "Click ☆ on a row to pin it. Pinned records stay at the top.",
                    "Forgot the number? Use 🔎 Find a transaction and fill in what you remember.",
                    "Click a group heading to fold it. Press Enter to open the selected row."
                }) stack.Controls.Add(L("•  " + tip, new Font("Segoe UI", 9F), AppTheme.TextMuted, 6));
            }
            else
            {
                stack.Controls.Add(L(r.Kind.ToUpperInvariant(), new Font("Segoe UI Semibold", 8.5F), AppTheme.TextMuted, 2));
                stack.Controls.Add(L(r.Number, new Font("Segoe UI Semibold", 14F), AppTheme.TextPrimary, 2));
                stack.Controls.Add(L(r.Client, new Font("Segoe UI", 10.5F), AppTheme.TextSecondary, 10));
                var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(12)) };
                var bOpen = new Button { Text = "Open record", AutoSize = true, MinimumSize = new Size(0, D(32)), Margin = new Padding(0, 0, D(6), D(4)) };
                UIThemeHelper.SetButtonRole(bOpen, "primary");
                bOpen.Click += (s, e) => OpenRecord(r);
                bool p = pinned.Contains(r.Key);
                var bPin = new Button { Text = p ? "★  Pinned" : "☆  Pin", AutoSize = true, MinimumSize = new Size(0, D(32)), Margin = new Padding(0, 0, D(6), D(4)) };
                UIThemeHelper.SetButtonRole(bPin, "secondary");
                bPin.Click += (s, e) => TogglePin(r);
                var bCopy = new Button { Text = "Copy #", AutoSize = true, MinimumSize = new Size(0, D(32)), Margin = new Padding(0, 0, 0, D(4)) };
                UIThemeHelper.SetButtonRole(bCopy, "secondary");
                bCopy.Click += (s, e) => CopyNumber(r);
                actions.Controls.AddRange(new Control[] { bOpen, bPin, bCopy });
                stack.Controls.Add(actions);
                if (r.Source is Delivery del && r.StatusGroup == "Completed")
                {
                    stack.Controls.Add(new Label { Text = "Completed trip", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, D(2), 0, D(6)) });
                    var done = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(D(300), 0), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(12)) };
                    Button Act(string text, string role, Action a)
                    {
                        var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(0, D(32)), Margin = new Padding(0, 0, D(6), D(6)) };
                        UIThemeHelper.SetButtonRole(b, role);
                        b.Click += (s, e) => a();
                        return b;
                    }
                    if (CanFin && string.IsNullOrWhiteSpace(r.InvoiceNo)) done.Controls.Add(Act("🧾  Create invoice", "primary", () => OnCreateInvoiceRequested?.Invoke(del)));
                    done.Controls.Add(Act("📝  Trip report", "secondary", () => OnTripReportRequested?.Invoke(del.Id, del.DispatchNumber)));
                    done.Controls.Add(Act("🖨  Print trip report", "secondary", () => PrintHelper.PrintOfficialTripReport(del.Id, del.DispatchNumber)));
                    done.Controls.Add(Act("🖨  Print dossier", "secondary", () => PrintHelper.PrintEndToEndDeliveryDossier(del.Id, del.DispatchNumber)));
                    done.Controls.Add(Act("⚠  Incidents", "secondary", () => OnIncidentsRequested?.Invoke(del.Id, del.DispatchNumber)));
                    stack.Controls.Add(done);
                }
                var facts = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, BackColor = Color.Transparent, Margin = new Padding(0) };
                facts.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(92)));
                facts.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                void F(string k, string? v)
                {
                    if (string.IsNullOrWhiteSpace(v)) return;
                    facts.Controls.Add(new Label { Text = k, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(3), D(6), D(5)), UseMnemonic = false });
                    facts.Controls.Add(new Label { Text = v, AutoSize = true, MaximumSize = new Size(D(196), 0), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, D(2), 0, D(5)), UseMnemonic = false });
                }
                F("Status", r.Status);
                F("Date", r.Date.ToString("dddd, MMMM d, yyyy"));
                F("Departure", r.Time);
                F("Route", r.Route);
                F("Truck", r.Truck);
                F("Driver", r.Driver);
                F("Aides", r.Aides);
                F("Crates", r.Crates > 0 ? r.Crates.ToString("N0") : "");
                F("Cargo", r.Cargo);
                if (CanFin) { F("Invoice", r.InvoiceNo); F("Amount", r.Amount.HasValue ? "₱" + r.Amount.Value.ToString("N2") : ""); }
                F("Remarks", r.Remarks);
                stack.Controls.Add(facts);
                var rel = new Label { Text = "Find related", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, D(14), 0, D(4)) };
                stack.Controls.Add(rel);
                foreach (var it in RelatedItems(r).OfType<ToolStripMenuItem>())
                {
                    var item = it;
                    var link = new LinkLabel { Text = item.Text, AutoSize = true, MaximumSize = new Size(D(290), 0), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 0, 0, D(5)), UseMnemonic = false };
                    link.LinkClicked += (s, e) => item.PerformClick();
                    stack.Controls.Add(link);
                }
            }
            preview.Controls.Add(stack);
            preview.ResumeLayout(true);
        }
        // =====================================================================================================
        // Actions
        // =====================================================================================================
        private void TogglePin(TxRecord r)
        {
            bool now = !pinned.Contains(r.Key);
            MJdatabase.SetRecordPinned(User, r.Kind, r.Number, now);
            if (now) pinned.Add(r.Key); else pinned.Remove(r.Key);
            Render();
            if (selected?.Key == r.Key) ShowPreview(r);
            ToastHelper.ShowToast(this, now ? $"{r.Number} pinned to the top" : $"{r.Number} unpinned", ToastType.Info);
        }
        private void CopyNumber(TxRecord r)
        {
            try { Clipboard.SetText(r.Number); ToastHelper.ShowToast(this, $"{r.Number} copied", ToastType.Info); } catch { }
        }
        private void OpenRecord(TxRecord r)
        {
            if (r.Source == null) return;
            MJdatabase.TouchRecentRecord(User, r.Kind, r.Number);
            object src = r.Source;
            try
            {
                if (src is Delivery d) src = MJdatabase.GetDeliveryById(d.Id) ?? d;
                else if (src is SpecialTrip st) src = MJdatabase.GetSpecialTripById(st.Id) ?? st;
            }
            catch { }
            RecordDetailsDialog.ShowDetails(this, src);
            recent = MJdatabase.GetRecordMarks(User, "recent").Select(m => m.Kind + "|" + m.Number).ToList();
            if (tabs.Selected == TabRecent) Render();
            else { tabs.Badges[TabRecent] = BaseFilter().Count(x => recent.Contains(x.Key)); tabs.Invalidate(); }
        }
        private void ClearFilters()
        {
            txtSearch.Clear();
            searchDelay.Stop();
            statusChip.SetQuietly(0); groupChip.SetQuietly(0); sortChip.SetQuietly(0);
            dateChip.SetQuietly(DateRange.All);
            foreach (var c in new FilterChip[] { dateChip, statusChip, groupChip, sortChip }) c.FitWidth();
            collapsed.Clear();
            Render();
        }
        private void OpenFinder()
        {
            using var dlg = new FindTransactionDialog(all, CanFin);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Chosen != null)
            {
                var r = dlg.Chosen;
                // Show it in the list too, so the user sees where it is.
                tabs.Selected = 0;
                txtSearch.Text = r.Number;
                searchDelay.Stop();
                Render();
                OpenRecord(r);
            }
        }
        private void OpenChangeLog()
        {
            using var f = new Form
            {
                Text = "Order History — change log (who changed what, and when)",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(D(1200), D(760)),
                MinimumSize = new Size(D(800), D(500)),
                BackColor = AppTheme.Canvas,
                ShowInTaskbar = false
            };
            f.Controls.Add(new ModuleHistoryView("Change log", new[] { "LoadingSchedules", "Deliveries", "SpecialTrips" }, isAdminOnly: false));
            f.ShowDialog(FindForm());
        }
        private void ExportCsv()
        {
            if (shown.Count == 0) { MessageBox.Show(FindForm(), "There is nothing in the list to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var dlg = new SaveFileDialog { Filter = "CSV file|*.csv", FileName = $"Order_History_{DateTime.Now:yyyyMMdd_HHmm}.csv", Title = "Export Order History" };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            static string Q(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", new[] { "Date", "Time", "Type", "Number", "Client", "Route / address", "Truck", "Driver", "Aides", "Crates", "Status" }.Concat(CanFin ? new[] { "Invoice", "Amount" } : Array.Empty<string>())));
            foreach (var r in shown)
            {
                var cells = new List<string> { r.Date.ToString("yyyy-MM-dd"), Q(r.Time), Q(r.Kind), Q(r.Number), Q(r.Client), Q(r.Route), Q(r.Truck), Q(r.Driver), Q(r.Aides), r.Crates.ToString(), Q(r.Status) };
                if (CanFin) { cells.Add(Q(r.InvoiceNo)); cells.Add(r.Amount.HasValue ? r.Amount.Value.ToString("0.00") : ""); }
                sb.AppendLine(string.Join(",", cells));
            }
            try
            {
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                ToastHelper.ShowToast(this, $"{shown.Count:N0} transaction(s) exported", ToastType.Success);
            }
            catch (Exception ex) { MessageBox.Show(FindForm(), "Couldn't save the file: " + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private static string Short(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s ?? "" : s.Substring(0, n - 1) + "…";
    }
}
