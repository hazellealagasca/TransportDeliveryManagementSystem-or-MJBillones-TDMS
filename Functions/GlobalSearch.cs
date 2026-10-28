using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One thing the header search can find: a record, or a page to jump to.</summary>
    public sealed class SearchItem
    {
        public string Group = "";
        public string Icon = "";
        public string Title = "";
        public string Sub = "";
        public string Hay = "";
        public DateTime When = DateTime.MinValue;
        public Action Open = () => { };
    }
    /// <summary>
    /// Builds the list of everything the signed-in user is allowed to see (orders, trucks, crew, clients, invoices,
    /// receipts, statements, trip reports, incidents, routes, special trips, users, pages) and matches typed text
    /// against it. Every word typed has to appear somewhere in the record; title matches rank first, newest next.
    /// </summary>
    public static class SearchIndex
    {
        public static List<SearchItem> Build()
        {
            var list = new List<SearchItem>();
            var mf = MainForm.Instance;
            bool ops = SessionContext.CanAccessOperations, fin = SessionContext.CanAccessFinance,
                 res = SessionContext.CanAccessResources, master = SessionContext.CanAccessMasterData, admin = SessionContext.IsAdmin;
            void Add(string group, string icon, string title, string sub, string hay, DateTime when, Action open)
                => list.Add(new SearchItem { Group = group, Icon = icon, Title = title, Sub = sub, Hay = (title + " " + sub + " " + hay).ToLowerInvariant(), When = when, Open = open });
            static string Peso(decimal v) => $"₱{v:N2}";
            // Pages
            void Page(string title, string words, Action open) => Add("Go to", "↗", title, "Page", words, DateTime.MinValue, open);
            Page("Dashboard", "home overview", () => mf?.NavigateHome());
            if (ops)
            {
                Page("Loading schedule & dispatch", "operations orders dispatch loading", () => mf?.NavigateToOperations("Dispatch"));
                Page("Completed dispatches", "operations done finished", () => mf?.NavigateToOperations("Completed"));
                Page("Trip reports", "operations trip report", () => mf?.NavigateToOperations("TripReports"));
                Page("Incident & damage logs", "operations incidents damage", () => mf?.NavigateToOperations("Incidents"));
                Page("Special trips", "operations special trip", () => mf?.NavigateToOperations("SpecialTrips"));
            }
            if (res)
            {
                Page("Trucks", "resources fleet vehicles", () => mf?.NavigateToResources("Trucks"));
                Page("Crew", "resources drivers aides personnel", () => mf?.NavigateToResources("Crew"));
                Page("Crew trip history", "resources crew history", () => mf?.NavigateToResources("CrewHistory"));
            }
            if (fin)
            {
                Page("Billing invoices", "payments finance billing", () => mf?.NavigateToFinance("Billing"));
                Page("Collections & receipts", "payments finance collections or", () => mf?.NavigateToFinance("Collections"));
                Page("Statements of account", "payments finance soa", () => mf?.NavigateToFinance("StatementOfAccount"));
            }
            if (master)
            {
                Page("Clients", "master data clients customers", () => mf?.NavigateToMasterData("Clients"));
                Page("Delivery routes", "master data routes stores", () => mf?.NavigateToMasterData("Routes"));
            }
            try
            {
                if (ops)
                {
                    foreach (var d in MJdatabase.GetDeliveries())
                        Add("Dispatches", "🚚", $"{d.DispatchNumber} · {d.ClientName}", $"{d.DispatchDate:MMM d, yyyy} · {d.TruckInfo} · {d.DriverName} · {d.Status}",
                            $"{d.Destination} {d.AideNames} {d.AideName} {d.LoadingScheduleInfo} {d.CargoType}", d.DispatchDate, () => Forms.DispatchDetailsDialog.ShowFor(mf!, d));
                    foreach (var ls in MJdatabase.GetLoadingSchedules())
                        Add("Loading schedules", "📦", $"{ls.ScheduleNumber} · {ls.ClientName}", $"{ls.ScheduleDate:MMM d, yyyy} {ls.DepartureTime} · {ls.TotalCrates} crates · {ls.Status}",
                            $"{ls.Destination} {ls.CargoType} {ls.Remarks}", ls.ScheduleDate, () => RecordDetailsDialog.ShowDetails(mf!, ls));
                    foreach (var tr in MJdatabase.GetTripReports())
                        Add("Trip reports", "📝", $"{tr.TripReportNumber}", $"Dispatch {tr.DispatchNumber} · {tr.ReportDate:MMM d, yyyy} · {tr.DeliveredCrates} delivered",
                            $"{tr.DispatchNumber} {tr.StoreManagerName} {tr.Remarks} {tr.GuardName}", tr.ReportDate, () => RecordDetailsDialog.ShowDetails(mf!, tr));
                    foreach (var inc in MJdatabase.GetIncidentReports())
                        Add("Incidents", "⚠️", $"Incident · {inc.DispatchNumber}", $"{inc.IncidentDate:MMM d, yyyy} · {inc.Cause} · {inc.Status}",
                            $"{inc.Description}", inc.IncidentDate, () => RecordDetailsDialog.ShowDetails(mf!, inc));
                    foreach (var st in MJdatabase.GetSpecialTrips())
                        Add("Special trips", "🌟", $"{st.OrderReference} · {st.Company}", $"{st.DispatchDate:MMM d, yyyy} · {st.TruckPlate} · {st.DriverName} · {st.Status}",
                            $"{st.Address} {st.AideName} {st.ServiceType}", st.DispatchDate, () => mf?.NavigateToOperations("SpecialTrips"));
                }
                if (res)
                {
                    foreach (var t in MJdatabase.GetTrucks())
                        Add("Trucks", "🚛", t.PlateNumber, $"{t.TruckType} · {t.CapacityCrates} crates · {t.Status}", $"{t.CargoType} {t.Remarks}", DateTime.MinValue, () => Forms.TruckProfileDialog.ShowFor(mf, t));
                    foreach (var c in MJdatabase.GetCrews())
                        Add("Crew", "👤", c.FullName, $"{c.CrewCode} · {c.Role} · {c.AvailabilityStatus}", $"{c.ContactNumber} {c.LicenseClass}", DateTime.MinValue, () => Forms.CrewProfileDialog.ShowFor(mf, c));
                }
                if (master || ops)
                {
                    foreach (var cl in MJdatabase.GetClients())
                        Add("Clients", "🏢", cl.CompanyName, string.Join(" · ", new[] { cl.ContactPerson, cl.ContactNumber, string.IsNullOrWhiteSpace(cl.TIN) ? "" : "TIN " + cl.TIN }.Where(x => !string.IsNullOrWhiteSpace(x))),
                            $"{cl.Address} {cl.Email}", DateTime.MinValue, () => { if (master) mf?.OpenClientProfile(cl.CompanyName); else RecordDetailsDialog.ShowDetails(mf!, cl); });
                }
                if (master)
                {
                    foreach (var r in MJdatabase.GetDeliveryRoutes())
                        Add("Routes", "🗺️", $"{r.RouteCode} - {r.RouteName}", $"{r.ClientCompanyName} · {r.TotalStops} store(s) · {r.StatusDisplay}", r.ConnectedPlaces, DateTime.MinValue, () => mf?.NavigateToMasterData("Routes"));
                }
                if (fin)
                {
                    foreach (var inv in MJdatabase.GetInvoices())
                        Add("Invoices", "🧾", $"Invoice {inv.InvoiceNumber} · {inv.ClientName}", $"{Peso(inv.NetAmount)} · {inv.Status} · due {inv.DueDate:MMM d, yyyy}",
                            $"{inv.DispatchNumber}", inv.IssueDate, () => mf?.ShowFinanceRecords("Billing", inv.InvoiceNumber));
                    foreach (var cr in MJdatabase.GetCollectionReceipts())
                        Add("Receipts", "💵", $"OR {cr.ReceiptNumber}", $"{Peso(cr.AmountPaid)} · {cr.PaymentMethod} · invoice {cr.InvoiceNumber} · {cr.PaymentDate:MMM d, yyyy}",
                            $"{cr.ReferenceNumber} {cr.Notes}", cr.PaymentDate, () => mf?.ShowFinanceRecords("Collections", cr.ReceiptNumber));
                    foreach (var soa in MJdatabase.GetStatementsOfAccount())
                        Add("Statements of account", "📑", $"{soa.SoaNumber} · {soa.ClientName}", $"{Peso(soa.TotalAmount)} · {soa.PeriodCovered}",
                            $"{soa.SalesInvoiceNo} {soa.ClientTin}", soa.IssueDate, () => mf?.ShowFinanceRecords("StatementOfAccount", soa.SoaNumber));
                }
                if (admin)
                {
                    foreach (var u in MJdatabase.GetUsers())
                        Add("Users", "🔑", u.FullName, $"@{u.Username} · {u.Role}{(u.IsActive ? "" : " · waiting for approval")}", u.ContactNumber, DateTime.MinValue, () => RecordDetailsDialog.ShowDetails(mf!, u));
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SearchIndex] {ex.Message}"); }
            return list;
        }
        /// <summary>Best matches, grouped by kind, at most <paramref name="perGroup"/> per kind.</summary>
        public static List<SearchItem> Find(List<SearchItem> index, string query, int perGroup = 5)
        {
            var words = (query ?? "").ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return new List<SearchItem>();
            string first = words[0];
            return index.Where(i => words.All(w => i.Hay.Contains(w)))
                .Select(i => new { i, title = i.Title.ToLowerInvariant() })
                .OrderBy(x => x.title.StartsWith(first) ? 0 : x.title.Contains(first) ? 1 : 2)
                .ThenByDescending(x => x.i.When)
                .GroupBy(x => x.i.Group)
                .OrderBy(g => g.Min(x => x.title.StartsWith(first) ? 0 : x.title.Contains(first) ? 1 : 2))
                .ThenBy(g => g.Key == "Go to" ? 1 : 0)
                .SelectMany(g => g.Take(perGroup).Select(x => x.i))
                .ToList();
        }
    }
    /// <summary>
    /// The search field in the middle of the top bar: a rounded box with a magnifier and a Ctrl+K hint, and a
    /// results panel that drops down while typing. Arrow keys move, Enter opens, Esc closes.
    /// </summary>
    public sealed class GlobalSearchBox : Control
    {
        private readonly TextBox box;
        private readonly ResultsPanel panel;
        private readonly System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer { Interval = 140 };
        private List<SearchItem>? index;
        private DateTime indexBuilt = DateTime.MinValue;
        private bool focused, hover;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public GlobalSearchBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.IBeam;
            box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5F),
                BackColor = AppTheme.InputBack,
                ForeColor = AppTheme.TextPrimary,
                PlaceholderText = "Search orders, trucks, crew, clients, invoices…"
            };
            panel = new ResultsPanel(this); // before Controls.Add: adding the box triggers a layout pass that uses the panel
            Controls.Add(box);
            box.GotFocus += (s, e) => { focused = true; Invalidate(); EnsureIndex(false); if (box.Text.Trim().Length > 0) RunSearch(); };
            box.LostFocus += (s, e) => { focused = false; Invalidate(); BeginInvoke(new Action(() => { if (!panel.ContainsFocus && !box.Focused) panel.HidePanel(); })); };
            box.TextChanged += (s, e) => { debounce.Stop(); debounce.Start(); Invalidate(); };
            debounce.Tick += (s, e) => { debounce.Stop(); RunSearch(); };
            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down) { panel.Step(1); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Up) { panel.Step(-1); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Enter) { debounce.Stop(); if (!panel.Visible) RunSearch(); panel.OpenSelected(); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { if (panel.Visible) panel.HidePanel(); else box.Clear(); e.Handled = true; e.SuppressKeyPress = true; }
            };
            MouseEnter += (s, e) => { hover = true; Invalidate(); };
            MouseLeave += (s, e) => { hover = false; Invalidate(); };
            MouseDown += (s, e) => box.Focus();
        }
        /// <summary>Puts the cursor in the search box (Ctrl+K / F1).</summary>
        public void FocusSearch() { box.Focus(); box.SelectAll(); }
        /// <summary>Forget cached records so the next search sees new data.</summary>
        public void ResetIndex() => indexBuilt = DateTime.MinValue;
        public void HideResults() => panel.HidePanel();
        private void EnsureIndex(bool force)
        {
            if (!force && index != null && (DateTime.Now - indexBuilt).TotalSeconds < 45) return;
            Cursor.Current = Cursors.WaitCursor;
            index = SearchIndex.Build();
            indexBuilt = DateTime.Now;
            Cursor.Current = Cursors.Default;
        }
        private void RunSearch()
        {
            string q = box.Text.Trim();
            if (q.Length == 0) { panel.HidePanel(); return; }
            EnsureIndex(false);
            var hits = SearchIndex.Find(index!, q);
            panel.ShowResults(q, hits);
        }
        internal void AfterOpen()
        {
            panel.HidePanel();
            box.Clear();
            indexBuilt = DateTime.MinValue;
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int left = S(40), right = S(70);
            int h = box.PreferredHeight;
            box.SetBounds(left, (Height - h) / 2 + 1, Math.Max(10, Width - left - right), h);
            panel?.Reposition();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Gfx.HighQuality(g);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundedRect(r, Height / 2))
            {
                using var b = new SolidBrush(AppTheme.InputBack);
                g.FillPath(b, path);
                using var pen = new Pen(focused ? AppTheme.Accent : (hover ? AppTheme.BorderStrong : AppTheme.Border), focused ? 2f : 1f);
                g.DrawPath(pen, path);
            }
            // magnifier
            int cx = S(20), cy = Height / 2, rad = S(6);
            using (var pen = new Pen(focused ? AppTheme.Accent : AppTheme.TextMuted, Math.Max(1.6f, S(2) * 0.9f)))
            {
                g.DrawEllipse(pen, cx - rad, cy - rad - S(1), rad * 2, rad * 2);
                g.DrawLine(pen, cx + (int)(rad * 0.7), cy + (int)(rad * 0.7) - S(1), cx + rad + S(4), cy + rad + S(3));
            }
            // Ctrl+K hint
            if (box.Text.Length == 0)
            {
                using var f = new Font("Segoe UI Semibold", 8F);
                var sz = TextRenderer.MeasureText("Ctrl K", f);
                var kr = new Rectangle(Width - sz.Width - S(22), (Height - S(20)) / 2, sz.Width + S(8), S(20));
                using (var kp = Gfx.RoundedRect(kr, S(5)))
                {
                    using var kb = new SolidBrush(AppTheme.SurfaceAlt);
                    g.FillPath(kb, kp);
                    using var kpen = new Pen(AppTheme.Border);
                    g.DrawPath(kpen, kp);
                }
                TextRenderer.DrawText(g, "Ctrl K", f, kr, AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { debounce.Dispose(); panel.Dispose(); }
            base.Dispose(disposing);
        }
        /// <summary>The drop-down list of results, hosted on the main window so it can overlap the page.</summary>
        private sealed class ResultsPanel : Panel
        {
            private readonly GlobalSearchBox owner;
            private readonly ListBox list;
            private readonly Label empty;
            private readonly List<object> rows = new List<object>(); // string = group header, SearchItem = result
            private string query = "";
            private int S(int v) => (int)Math.Round(v * owner.DeviceDpi / 96f);
            public ResultsPanel(GlobalSearchBox owner)
            {
                this.owner = owner;
                Visible = false;
                BackColor = AppTheme.Surface;
                Padding = new Padding(1);
                list = new ListBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    DrawMode = DrawMode.OwnerDrawVariable,
                    BackColor = AppTheme.Surface,
                    ForeColor = AppTheme.TextPrimary,
                    IntegralHeight = false
                };
                empty = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = AppTheme.TextMuted,
                    Font = new Font("Segoe UI", 9.5F),
                    Visible = false,
                    UseMnemonic = false
                };
                Controls.Add(list);
                Controls.Add(empty);
                Paint += (s, e) =>
                {
                    using var pen = new Pen(AppTheme.BorderStrong);
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                };
                list.MeasureItem += (s, e) => e.ItemHeight = e.Index >= 0 && e.Index < rows.Count && rows[e.Index] is string ? S(30) : S(50);
                list.DrawItem += DrawRow;
                list.MouseMove += (s, e) =>
                {
                    int i = list.IndexFromPoint(e.Location);
                    if (i >= 0 && i < rows.Count && rows[i] is SearchItem && list.SelectedIndex != i) list.SelectedIndex = i;
                    list.Cursor = i >= 0 && i < rows.Count && rows[i] is SearchItem ? Cursors.Hand : Cursors.Default;
                };
                list.LostFocus += (s, e) => BeginInvoke(new Action(() => { if (!owner.box.Focused && !ContainsFocus) HidePanel(); }));
                list.MouseClick += (s, e) => { int i = list.IndexFromPoint(e.Location); if (i >= 0 && i < rows.Count && rows[i] is SearchItem) { list.SelectedIndex = i; OpenSelected(); } };
                list.SelectedIndexChanged += (s, e) =>
                {
                    if (list.SelectedIndex >= 0 && list.SelectedIndex < rows.Count && rows[list.SelectedIndex] is string)
                    {
                        int next = NextResult(list.SelectedIndex, 1);
                        if (next >= 0) list.SelectedIndex = next;
                    }
                };
            }
            private int NextResult(int from, int step)
            {
                for (int i = from; i >= 0 && i < rows.Count; i += step) if (rows[i] is SearchItem) return i;
                return -1;
            }
            public void Step(int step)
            {
                if (!Visible || rows.Count == 0) return;
                int start = list.SelectedIndex < 0 ? (step > 0 ? 0 : rows.Count - 1) : list.SelectedIndex + step;
                int i = NextResult(Math.Clamp(start, 0, rows.Count - 1), step);
                if (i >= 0) list.SelectedIndex = i;
            }
            public void OpenSelected()
            {
                if (!Visible) return;
                int i = list.SelectedIndex >= 0 ? list.SelectedIndex : NextResult(0, 1);
                if (i < 0 || i >= rows.Count || rows[i] is not SearchItem item) return;
                owner.AfterOpen();
                try { item.Open(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
            public void ShowResults(string q, List<SearchItem> hits)
            {
                query = q;
                rows.Clear();
                list.BeginUpdate();
                list.Items.Clear();
                string? group = null;
                foreach (var h in hits)
                {
                    if (h.Group != group) { group = h.Group; rows.Add(group); list.Items.Add(group); }
                    rows.Add(h);
                    list.Items.Add(h.Title);
                }
                list.EndUpdate();
                empty.Text = $"Nothing matches \"{q}\".\nTry a dispatch #, plate number, name, client or invoice #.";
                empty.Visible = hits.Count == 0;
                list.Visible = hits.Count > 0;
                int listH = 0;
                foreach (var r in rows) listH += r is string ? S(30) : S(50);
                Height = hits.Count == 0 ? S(90) : Math.Min(S(470), listH + S(4));
                if (Parent == null)
                {
                    var host = owner.FindForm();
                    if (host == null) return;
                    host.Controls.Add(this);
                }
                Reposition();
                Visible = true;
                BringToFront();
                int first = NextResult(0, 1);
                if (first >= 0) list.SelectedIndex = first;
            }
            public void HidePanel() { Visible = false; }
            public void Reposition()
            {
                if (Parent == null || !owner.IsHandleCreated) return;
                var pt = Parent.PointToClient(owner.PointToScreen(new Point(0, owner.Height + S(6))));
                int w = Math.Max(owner.Width, S(520));
                int x = pt.X - (w - owner.Width) / 2;
                x = Math.Max(S(8), Math.Min(x, Parent.ClientSize.Width - w - S(8)));
                SetBounds(x, pt.Y, w, Height);
            }
            private void DrawRow(object? sender, DrawItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= rows.Count) return;
                var g = e.Graphics;
                var r = e.Bounds;
                using (var bg = new SolidBrush(AppTheme.Surface)) g.FillRectangle(bg, r);
                var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                if (rows[e.Index] is string header)
                {
                    using var hf = new Font("Segoe UI Semibold", 8.25F);
                    TextRenderer.DrawText(g, header.ToUpperInvariant(), hf, new Rectangle(r.X + S(16), r.Y + S(6), r.Width - S(32), r.Height - S(6)), AppTheme.TextMuted, flags);
                    return;
                }
                var item = (SearchItem)rows[e.Index];
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                if (sel)
                {
                    var hr = new Rectangle(r.X + S(6), r.Y + S(2), r.Width - S(12), r.Height - S(4));
                    using var path = Gfx.RoundedRect(hr, S(8));
                    using var hb = new SolidBrush(AppTheme.AccentSoft);
                    Gfx.HighQuality(g);
                    g.FillPath(hb, path);
                }
                using var iconFont = new Font("Segoe UI Emoji", 12F);
                TextRenderer.DrawText(g, item.Icon, iconFont, new Rectangle(r.X + S(14), r.Y, S(32), r.Height), AppTheme.TextSecondary, flags | TextFormatFlags.HorizontalCenter);
                using var tf = new Font("Segoe UI Semibold", 10F);
                using var sf = new Font("Segoe UI", 8.75F);
                int tx = r.X + S(54);
                TextRenderer.DrawText(g, item.Title, tf, new Rectangle(tx, r.Y + S(5), r.Width - tx - S(70), S(22)), sel ? AppTheme.AccentText : AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, item.Sub, sf, new Rectangle(tx, r.Y + S(25), r.Width - tx - S(20), S(20)), AppTheme.TextMuted, flags);
                if (sel)
                {
                    using var ef = new Font("Segoe UI", 8F);
                    TextRenderer.DrawText(g, "Enter ↵", ef, new Rectangle(r.Right - S(76), r.Y + S(5), S(62), S(22)), AppTheme.AccentText, flags | TextFormatFlags.Right);
                }
            }
        }
    }
}
