using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// A driver's or delivery aide's profile: classification and license, every dispatch they worked, how each completed
    /// trip went, which trucks they used, and incidents / violations / performance remarks. All lists are newest first.
    /// </summary>
    public sealed class CrewProfileDialog : Form
    {
        private readonly Crew crew;
        private readonly Action<Crew>? onEdit;
        private readonly ProfileHeader header = new ProfileHeader();
        private readonly SegmentTabs tabs = new SegmentTabs("Profile", "Dispatch logs", "Trip performance", "Vehicle history", "Incidents & violations");
        private readonly Panel body = new Panel { Dock = DockStyle.Fill };
        private readonly Panel[] pages = new Panel[5];
        private List<Delivery> trips = new List<Delivery>();
        private Dictionary<int, TripReport> reports = new Dictionary<int, TripReport>();
        private List<IncidentReport> incidents = new List<IncidentReport>();
        private List<CrewRecord> records = new List<CrewRecord>();
        private Dictionary<int, Truck> trucks = new Dictionary<int, Truck>();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public static void ShowFor(IWin32Window? owner, Crew crew, Action<Crew>? onEdit = null)
        {
            var dlg = new CrewProfileDialog(crew, onEdit);
            dlg.FormClosing += (s, e) => DarkModeKit.StopAutoComplete(dlg);
            try { dlg.ShowDialog(owner); }
            finally { DarkModeKit.StopAutoComplete(dlg); DarkModeKit.SafeDispose(dlg); }
        }
        public CrewProfileDialog(Crew crew, Action<Crew>? onEdit = null)
        {
            this.crew = crew;
            this.onEdit = onEdit;
            Text = $"Crew profile — {crew.FullName}";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(D(1080), D(720));
            MinimumSize = new Size(D(880), D(600));
            BackColor = AppTheme.Canvas;
            body.BackColor = AppTheme.Canvas;
            LoadData();
            BuildHeader();
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(60), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(12)) };
            footer.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            var btnClose = new Button { Text = "Close", Dock = DockStyle.Right, Width = D(100), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnClose, "primary");
            var btnEdit = new Button { Text = "Edit crew details", Dock = DockStyle.Left, Width = D(160), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnEdit, "secondary");
            btnEdit.Click += (s, e) => { Close(); onEdit?.Invoke(crew); };
            btnEdit.Visible = onEdit != null;
            var btnPrint = new Button { Text = "Print dossier", Dock = DockStyle.Left, Width = D(130), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnPrint, "secondary");
            btnPrint.Click += (s, e) => PrintHelper.PrintCrewDossierAndHistory(crew, trips);
            footer.Controls.Add(btnClose);
            footer.Controls.Add(btnPrint);
            footer.Controls.Add(new Panel { Dock = DockStyle.Left, Width = D(8), BackColor = Color.Transparent });
            footer.Controls.Add(btnEdit);
            CancelButton = btnClose;
            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(tabs);
            Controls.Add(header);
            UpdateBadges();
            tabs.SelectedChanged += (s, e) => ShowPage(tabs.Selected);
            ShowPage(0);
            KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.Tab) { tabs.Selected = (tabs.Selected + (e.Shift ? 4 : 1)) % 5; e.Handled = true; } };
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
        }
        private bool OnTrip(Delivery d) => d.DriverId == crew.Id || d.AideId == crew.Id || (d.AideIds ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Trim() == crew.Id.ToString());
        private string RoleOn(Delivery d) => d.DriverId == crew.Id ? "Driver" : "Aide";
        private void LoadData()
        {
            try { trips = MJdatabase.GetDeliveries().Where(OnTrip).OrderByDescending(d => d.DispatchDate).ThenByDescending(d => d.Id).ToList(); } catch { trips = new List<Delivery>(); }
            var ids = trips.Select(t => t.Id).ToHashSet();
            try { reports = MJdatabase.GetTripReports().Where(r => ids.Contains(r.DeliveryId)).GroupBy(r => r.DeliveryId).ToDictionary(g => g.Key, g => g.First()); } catch { reports = new Dictionary<int, TripReport>(); }
            try { incidents = MJdatabase.GetIncidentReports().Where(i => ids.Contains(i.DeliveryId)).OrderByDescending(i => i.IncidentDate).ToList(); } catch { incidents = new List<IncidentReport>(); }
            try { records = MJdatabase.GetCrewRecords(crew.Id); } catch { records = new List<CrewRecord>(); }
            try { trucks = MJdatabase.GetTrucks().ToDictionary(t => t.Id); } catch { trucks = new Dictionary<int, Truck>(); }
        }
        private void UpdateBadges()
        {
            tabs.Badges = new[] { 0, trips.Count, trips.Count(t => RowMenu.Is(t.Status, "Completed")), trips.Select(t => t.TruckId).Where(i => i > 0).Distinct().Count(), incidents.Count + records.Count(r => r.Kind == "Violation") };
            tabs.Invalidate();
        }
        private void BuildHeader()
        {
            var parts = (crew.FullName ?? "").Replace(",", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => !p.EndsWith(".")).ToList();
            header.Badge = !string.IsNullOrWhiteSpace(crew.FirstName) && !string.IsNullOrWhiteSpace(crew.LastName) ? $"{crew.FirstName[0]}{crew.LastName[0]}".ToUpperInvariant() : parts.Count > 1 ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant() : "👤";
            header.Title = string.IsNullOrWhiteSpace(crew.FirstName) ? crew.FullName : $"{crew.FirstName} {(string.IsNullOrWhiteSpace(crew.MiddleName) ? "" : crew.MiddleName + " ")}{crew.LastName}";
            header.Subtitle = string.Join("  ·  ", new[] { crew.CrewCode, crew.Role, string.IsNullOrWhiteSpace(crew.ContactNumber) ? "" : crew.ContactNumber }.Where(x => x.Length > 0));
            header.Chips.Clear();
            header.Chips.Add((crew.IsDriverRole ? "Driver" : "Delivery aide / helper", AppTheme.AccentSoft, AppTheme.AccentText));
            if (crew.IsSeasonal) header.Chips.Add(("Seasonal / temporary", AppTheme.InfoSoft, AppTheme.InfoText));
            header.Chips.Add((crew.AvailabilityStatus, crew.IsActive ? (crew.IsAvailable ? AppTheme.SuccessSoft : AppTheme.InfoSoft) : AppTheme.DangerSoft, crew.IsActive ? (crew.IsAvailable ? AppTheme.SuccessText : AppTheme.InfoText) : AppTheme.DangerText));
            bool expired = crew.LicenseExpiredOn(DateTime.Today);
            bool soon = crew.HasLicense && crew.LicenseExpiry.HasValue && !expired && (crew.LicenseExpiry.Value - DateTime.Today).TotalDays <= 60;
            if (crew.HasLicense || crew.IsDriverRole)
                header.Chips.Add(("License: " + crew.LicenseDisplay, expired || (crew.IsDriverRole && !crew.HasLicense) ? AppTheme.DangerSoft : soon ? AppTheme.WarningSoft : AppTheme.SurfaceHover, expired || (crew.IsDriverRole && !crew.HasLicense) ? AppTheme.DangerText : soon ? AppTheme.WarningText : AppTheme.TextSecondary));
            header.Invalidate();
        }
        private void ShowPage(int i)
        {
            if (pages[i] == null)
            {
                pages[i] = i switch { 0 => BuildProfile(), 1 => BuildDispatchLogs(), 2 => BuildPerformance(), 3 => BuildVehicles(), _ => BuildIncidents() };
                pages[i].Dock = DockStyle.Fill;
                pages[i].Visible = false;
                body.Controls.Add(pages[i]);
                if (ThemeManager.IsDark) DarkModeKit.Native(pages[i]);
            }
            foreach (var p in pages) if (p != null) p.Visible = p == pages[i];
        }
        private void Rebuild(params int[] which)
        {
            foreach (var i in which) if (pages[i] != null) { body.Controls.Remove(pages[i]); pages[i].Dispose(); pages[i] = null!; }
            UpdateBadges();
            ShowPage(tabs.Selected);
        }
        private Panel Page() => new Panel { BackColor = AppTheme.Canvas, Padding = new Padding(D(20), D(16), D(20), D(12)) };
        // ------------------------------------------------------------------ profile
        private Panel BuildProfile()
        {
            var page = Page();
            var stats = new StatRow();
            int completed = trips.Count(t => RowMenu.Is(t.Status, "Completed"));
            var last = trips.FirstOrDefault(t => RowMenu.Is(t.Status, "Completed", "In Transit"));
            stats.Add("Trips completed", completed.ToString("N0"), $"{trips.Count(t => t.DriverId == crew.Id)} as driver · {trips.Count(t => t.DriverId != crew.Id)} as aide", AppTheme.Accent);
            stats.Add("Last trip", last == null ? "—" : last.DispatchDate.ToString("MMM d, yyyy"), last == null ? "no trips yet" : last.DispatchNumber, AppTheme.Info);
            stats.Add("Incidents", incidents.Count.ToString("N0"), "on their trips", incidents.Count > 0 ? AppTheme.Warning : AppTheme.Success);
            int viol = records.Count(r => r.Kind == "Violation");
            stats.Add("Violations", viol.ToString("N0"), "on record", viol > 0 ? AppTheme.Danger : AppTheme.Success);
            var list = new ListView { View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.None, BorderStyle = BorderStyle.None, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 10F) };
            list.Columns.Add("k", D(220));
            list.Columns.Add("v", D(560));
            void Row(string k, string v, Color? color = null)
            {
                var it = new ListViewItem(new[] { k, string.IsNullOrWhiteSpace(v) ? "—" : v }) { UseItemStyleForSubItems = false };
                it.SubItems[0].ForeColor = AppTheme.TextMuted;
                it.SubItems[1].ForeColor = color ?? AppTheme.TextPrimary;
                list.Items.Add(it);
            }
            Row("Crew ID", crew.CrewCode);
            Row("Classification", crew.IsDriverRole ? "Driver" : "Delivery aide / helper");
            Row("Employment", crew.IsSeasonal ? "Seasonal / temporary" : "Regular");
            Row("Status", crew.AvailabilityStatus + (crew.IsActive ? (crew.IsAvailable ? " — free for the next dispatch" : " — currently on a trip") : " — not offered for new trips"));
            Row("Contact number", crew.ContactNumber);
            Row("License classification", crew.HasLicense ? crew.LicenseClass : (crew.IsDriverRole ? "None — a driver needs a license!" : "None (not required for aides)"), !crew.HasLicense && crew.IsDriverRole ? AppTheme.DangerText : null);
            if (crew.HasLicense)
            {
                Row("Date issued", crew.LicenseIssued?.ToString("MMMM d, yyyy") ?? "not set");
                string exp = crew.LicenseExpiry?.ToString("MMMM d, yyyy") ?? "not set — please update";
                Color? ec = null;
                if (crew.LicenseExpiry.HasValue)
                {
                    int days = (int)(crew.LicenseExpiry.Value.Date - DateTime.Today).TotalDays;
                    exp += days < 0 ? $"   (EXPIRED {-days} day(s) ago)" : days <= 60 ? $"   (expires in {days} day(s))" : $"   ({days / 365} yr {(days % 365) / 30} mo left)";
                    ec = days < 0 ? AppTheme.DangerText : days <= 60 ? AppTheme.WarningText : AppTheme.SuccessText;
                }
                else ec = AppTheme.WarningText;
                Row("Expiry date", exp, ec);
                if (crew.LicenseIssued.HasValue && crew.LicenseExpiry.HasValue) Row("Validity", $"{(crew.LicenseExpiry.Value - crew.LicenseIssued.Value).TotalDays / 365.25:0.#} years");
            }
            Row("Requirements", crew.IsDriverRole ? "Valid driver's license (Professional for trucks), not expired on the trip date." : "No license required.");
            Row("Pay", "Set per trip after the trip is completed (Trip report → crew pay).");
            var gap = new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = AppTheme.Canvas };
            page.Controls.Add(ProfileKit.Card(list, D(10)));
            page.Controls.Add(ProfileKit.Section("Details"));
            page.Controls.Add(gap);
            page.Controls.Add(stats);
            return page;
        }
        // ------------------------------------------------------------------ dispatch logs
        private Panel BuildDispatchLogs()
        {
            var page = Page();
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 65), ProfileKit.Col("Dispatch", "Dispatch #", 75), ProfileKit.Col("As", "Role on trip", 55), ProfileKit.Col("Client", "Client", 110),
                ProfileKit.Col("Route", "Route", 120), ProfileKit.Col("Truck", "Truck", 70), ProfileKit.Col("Crates", "Crates", 45, true), ProfileKit.Col("Status", "Status", 65));
            var search = ProfileKit.Box("Filter by dispatch #, client, route, truck or status…");
            void Fill()
            {
                string q = search.Text.Trim();
                g.Rows.Clear();
                foreach (var t in trips)
                {
                    string truck = trucks.TryGetValue(t.TruckId, out var tr) ? tr.PlateNumber : t.TruckInfo;
                    if (q.Length > 0 && !new[] { t.DispatchNumber, t.ClientName, t.CleanRouteDisplay, truck, t.Status, RoleOn(t) }.Any(x => (x ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))) continue;
                    int r = g.Rows.Add(t.DispatchDate.ToString("yyyy-MM-dd"), t.DispatchNumber, RoleOn(t), t.ClientName, t.CleanRouteDisplay, truck, (t.TotalCrates > 0 ? t.TotalCrates : t.GCrate + t.ECrate).ToString("N0"), t.Status);
                    g.Rows[r].Tag = t;
                }
            }
            search.TextChanged += (s, e) => Fill();
            Fill();
            g.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is Delivery d) DispatchDetailsDialog.ShowFor(this, d); };
            var top = new Panel { Dock = DockStyle.Top, Height = D(40), BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, 0, D(8)) };
            top.Controls.Add(search);
            page.Controls.Add(ProfileKit.Card(g, 1));
            page.Controls.Add(top);
            page.Controls.Add(ProfileKit.Section("Dispatch logs", $"{trips.Count} dispatch(es), newest first · double-click to open"));
            return page;
        }
        // ------------------------------------------------------------------ trip performance
        private Panel BuildPerformance()
        {
            var page = Page();
            var done = trips.Where(t => RowMenu.Is(t.Status, "Completed")).ToList();
            var rows = done.Select(t =>
            {
                int planned = t.TotalCrates > 0 ? t.TotalCrates : t.GCrate + t.ECrate;
                reports.TryGetValue(t.Id, out var rep);
                int delivered = rep == null ? -1 : (rep.DeliveredCrates > 0 ? rep.DeliveredCrates : rep.DeliveredGCrates + rep.DeliveredECrates);
                int returned = rep == null ? 0 : (rep.ReturnedCrates > 0 ? rep.ReturnedCrates : rep.ReturnedGCrates + rep.ReturnedECrates);
                var inc = incidents.Where(i => i.DeliveryId == t.Id).ToList();
                int damaged = inc.Sum(i => i.DamagedQuantity);
                double pct = delivered < 0 || planned <= 0 ? -1 : Math.Min(100, delivered * 100.0 / planned);
                string rating = delivered < 0 ? "No trip report" : inc.Count == 0 && pct >= 98 ? "Excellent" : damaged == 0 && pct >= 90 ? "Good" : "Needs review";
                return (Trip: t, Planned: planned, Delivered: delivered, Returned: returned, Incidents: inc.Count, Damaged: damaged, Pct: pct, Rating: rating, Report: rep);
            }).ToList();
            var withReport = rows.Where(r => r.Pct >= 0).ToList();
            var stats = new StatRow();
            stats.Add("Completed trips", done.Count.ToString("N0"), $"{withReport.Count} with a trip report", AppTheme.Accent);
            stats.Add("Average delivered", withReport.Count == 0 ? "—" : $"{withReport.Average(r => r.Pct):0.#}%", "of planned crates", AppTheme.Success);
            int clean = rows.Count(r => r.Incidents == 0);
            stats.Add("Incident-free trips", done.Count == 0 ? "—" : $"{clean * 100.0 / done.Count:0}%", $"{clean} of {done.Count}", clean == done.Count ? AppTheme.Success : AppTheme.Warning);
            stats.Add("Rated excellent", rows.Count(r => r.Rating == "Excellent").ToString("N0"), $"{rows.Count(r => r.Rating == "Needs review")} need review", AppTheme.Info);
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas, Padding = new Padding(0, D(14), 0, 0) };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 65), ProfileKit.Col("Dispatch", "Dispatch #", 75), ProfileKit.Col("As", "As", 45), ProfileKit.Col("Planned", "Planned", 50, true),
                ProfileKit.Col("Delivered", "Delivered", 55, true), ProfileKit.Col("Returned", "Returned", 55, true), ProfileKit.Col("Pct", "Delivered %", 60, true), ProfileKit.Col("Inc", "Incidents", 55, true), ProfileKit.Col("Rating", "Rating", 75));
            foreach (var r in rows)
            {
                int i = g.Rows.Add(r.Trip.DispatchDate.ToString("yyyy-MM-dd"), r.Trip.DispatchNumber, RoleOn(r.Trip), r.Planned.ToString("N0"), r.Delivered < 0 ? "—" : r.Delivered.ToString("N0"), r.Delivered < 0 ? "—" : r.Returned.ToString("N0"),
                    r.Pct < 0 ? "—" : $"{r.Pct:0.#}%", r.Incidents == 0 ? "0" : $"{r.Incidents} ({r.Damaged} dmg)", r.Rating);
                g.Rows[i].Tag = r.Trip;
                g.Rows[i].Cells["Rating"].Style.ForeColor = r.Rating == "Excellent" ? AppTheme.SuccessText : r.Rating == "Good" ? AppTheme.InfoText : r.Rating == "Needs review" ? AppTheme.DangerText : AppTheme.TextMuted;
                if (r.Report != null && !string.IsNullOrWhiteSpace(r.Report.Remarks)) g.Rows[i].Cells["Dispatch"].ToolTipText = "Trip report remarks: " + r.Report.Remarks;
            }
            g.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is Delivery d) DispatchDetailsDialog.ShowFor(this, d); };
            var left = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, D(10), 0) };
            left.Controls.Add(ProfileKit.Card(g, 1));
            left.Controls.Add(ProfileKit.Section("Completed trips", "newest first"));
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(10), 0, 0, 0) };
            right.Controls.Add(BuildRecordPanel("Performance remark", "performance"));
            right.Controls.Add(ProfileKit.Section("Performance remarks"));
            split.Controls.Add(left, 0, 0);
            split.Controls.Add(right, 1, 0);
            page.Controls.Add(split);
            page.Controls.Add(stats);
            return page;
        }
        // ------------------------------------------------------------------ vehicles
        private Panel BuildVehicles()
        {
            var page = Page();
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Plate", "Truck", 70), ProfileKit.Col("Type", "Type", 100), ProfileKit.Col("Trips", "Trips", 45, true), ProfileKit.Col("Done", "Completed", 55, true),
                ProfileKit.Col("First", "First used", 65), ProfileKit.Col("Last", "Last used", 65), ProfileKit.Col("Status", "Truck status now", 80));
            var groups = trips.Where(t => t.TruckId > 0).GroupBy(t => t.TruckId).Select(gr => (Id: gr.Key, Trips: gr.ToList(), Last: gr.Max(t => t.DispatchDate), First: gr.Min(t => t.DispatchDate)))
                .OrderByDescending(x => x.Last).ToList();
            foreach (var x in groups)
            {
                trucks.TryGetValue(x.Id, out var tr);
                int r = g.Rows.Add(tr?.PlateNumber ?? x.Trips[0].TruckInfo, tr?.TruckType ?? "—", x.Trips.Count.ToString("N0"), x.Trips.Count(t => RowMenu.Is(t.Status, "Completed")).ToString("N0"),
                    x.First.ToString("yyyy-MM-dd"), x.Last.ToString("yyyy-MM-dd"), tr?.Status ?? "—");
                g.Rows[r].Tag = tr;
            }
            g.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is Truck t) TruckProfileDialog.ShowFor(this, t); };
            page.Controls.Add(ProfileKit.Card(g, 1));
            page.Controls.Add(ProfileKit.Section("Assigned vehicle history", $"{groups.Count} truck(s), most recently used first · double-click for the truck's profile"));
            return page;
        }
        // ------------------------------------------------------------------ incidents & violations
        private Panel BuildIncidents()
        {
            var page = Page();
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 65), ProfileKit.Col("Kind", "Kind", 70), ProfileKit.Col("Type", "Type", 130), ProfileKit.Col("Dispatch", "Dispatch #", 70),
                ProfileKit.Col("Sev", "Severity / cost", 70), ProfileKit.Col("Details", "Details", 150));
            var items = new List<(DateTime When, string Kind, string Type, string Dispatch, string Sev, string Details, object Tag, Color Color)>();
            foreach (var i in incidents)
                items.Add((i.IncidentDate, "Incident", i.Cause, i.DispatchNumber, $"₱{i.EstimatedCost:N0}{(i.ChargeableToCrew ? " · chargeable" : "")}", $"{i.DamagedQuantity} damaged · {i.Status} · {i.Description}", i, AppTheme.WarningText));
            foreach (var r in records.Where(r => r.Kind == "Violation"))
                items.Add((r.RecordDate, "Violation", r.Type, r.DispatchNumber, r.Severity, r.Description, r, r.Severity == "Critical" || r.Severity == "Major" ? AppTheme.DangerText : AppTheme.WarningText));
            foreach (var x in items.OrderByDescending(x => x.When))
            {
                int row = g.Rows.Add(x.When.ToString("yyyy-MM-dd"), x.Kind, x.Type, string.IsNullOrWhiteSpace(x.Dispatch) ? "—" : x.Dispatch, x.Sev, x.Details);
                g.Rows[row].Tag = x.Tag;
                g.Rows[row].Cells["Kind"].Style.ForeColor = x.Color;
            }
            var left = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, D(10), 0) };
            left.Controls.Add(ProfileKit.Card(g, 1));
            left.Controls.Add(ProfileKit.Section("Incident & violation records", $"{incidents.Count} incident(s) on their trips · {records.Count(r => r.Kind == "Violation")} violation(s) · newest first"));
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(10), 0, 0, 0) };
            right.Controls.Add(BuildRecordPanel("Violation", "violation"));
            right.Controls.Add(ProfileKit.Section("Record a violation"));
            split.Controls.Add(left, 0, 0);
            split.Controls.Add(right, 1, 0);
            page.Controls.Add(split);
            return page;
        }
        /// <summary>Entry form for violations or performance remarks, with suggestions and a list of the existing ones.</summary>
        private Control BuildRecordPanel(string kind, string suggestSource)
        {
            bool isViolation = kind == "Violation";
            int fw = D(290);
            var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AppTheme.Surface, Padding = new Padding(D(4)) };
            Control L(string t) => new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(6), 0, D(2)) };
            var dtp = new DateTimePicker { Width = fw, Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 10F), MaxDate = DateTime.Today };
            var cbType = new ComboBox { Width = fw, Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDown };
            ProfileKit.Suggest(cbType, suggestSource);
            cbType.Text = "";
            var cbDispatch = new ComboBox { Width = fw, Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDown };
            cbDispatch.Items.Add("");
            foreach (var t in trips.Take(60)) cbDispatch.Items.Add(t.DispatchNumber);
            cbDispatch.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbDispatch.AutoCompleteSource = AutoCompleteSource.ListItems;
            var cbSev = new ComboBox { Width = fw, Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList };
            cbSev.Items.AddRange(new object[] { "Minor", "Major", "Critical" });
            cbSev.SelectedIndex = 0;
            var txtDesc = new TextBox { Width = fw, Height = D(64), Multiline = true, Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, PlaceholderText = "What happened / what was observed" };
            var lblMsg = new Label { AutoSize = true, MaximumSize = new Size(fw, 0), ForeColor = AppTheme.TextMuted };
            cbDispatch.SelectedIndexChanged += (s, e) =>
            {
                var t = trips.FirstOrDefault(x => x.DispatchNumber == cbDispatch.Text);
                if (t != null && t.DispatchDate.Date <= DateTime.Today) dtp.Value = t.DispatchDate.Date;
            };
            void Save()
            {
                string type = System.Text.RegularExpressions.Regex.Replace(cbType.Text.Trim(), @"\s+", " ");
                string dn = cbDispatch.Text.Trim();
                var errs = new List<string>();
                if (type.Length == 0) errs.Add($"{(isViolation ? "Violation type" : "Remark")} is required (pick a suggestion or type one).");
                if (dn.Length > 0 && !trips.Any(t => string.Equals(t.DispatchNumber, dn, StringComparison.OrdinalIgnoreCase))) errs.Add($"Dispatch {dn} isn't one of {crew.FullName}'s trips.");
                if (isViolation && txtDesc.Text.Trim().Length < 5) errs.Add("Describe the violation briefly (at least a few words).");
                if (errs.Count > 0)
                {
                    lblMsg.ForeColor = AppTheme.DangerText;
                    lblMsg.Text = errs[0];
                    MessageBox.Show(this, "Please check:\n\n• " + string.Join("\n• ", errs), kind, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (records.Any(r => r.Kind == kind && r.RecordDate.Date == dtp.Value.Date && string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase) && string.Equals(r.DispatchNumber, dn, StringComparison.OrdinalIgnoreCase))
                    && MessageBox.Show(this, $"The same {kind.ToLowerInvariant()} is already recorded for that day. Add it again?", "Possible duplicate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                if (MessageBox.Show(this, $"Are you sure you want to add this {kind.ToLowerInvariant()} to {crew.FullName}'s record?\n\n{type}{(dn.Length > 0 ? " · " + dn : "")}", "Add " + kind.ToLowerInvariant(), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                try
                {
                    MJdatabase.SaveCrewRecord(new CrewRecord { CrewId = crew.Id, RecordDate = dtp.Value.Date, Kind = kind, Type = type, Description = txtDesc.Text.Trim(), DispatchNumber = dn, Severity = isViolation ? cbSev.Text : "" });
                    records = MJdatabase.GetCrewRecords(crew.Id);
                    Rebuild(0, 2, 4);
                }
                catch (Exception ex) { ErrLog.Error("CrewProfileDialog", ex);  MessageBox.Show(this, "Couldn't save: " + ex.Message, kind, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            var btns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, D(8), 0, 0) };
            btns.Controls.Add(ProfileKit.Button("Add " + kind.ToLowerInvariant(), "primary", (s, e) => Save()));
            var existing = new ListBox { Width = fw, Height = D(150), Font = new Font("Segoe UI", 9F), BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, IntegralHeight = false };
            foreach (var r in records.Where(r => r.Kind == kind)) existing.Items.Add(new RecordItem(r));
            btns.Controls.Add(ProfileKit.Button("Void selected", "danger", (s, e) =>
            {
                if (existing.SelectedItem is not RecordItem it) { lblMsg.Text = "Select an entry in the list below first."; return; }
                if (MessageBox.Show(this, $"Are you sure you want to void \"{it.Record.Type}\" ({it.Record.RecordDate:MMM d, yyyy})?\n\nIt's hidden from the record but kept in the audit trail.", "Void " + kind.ToLowerInvariant(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                MJdatabase.VoidCrewRecord(it.Record.Id);
                records = MJdatabase.GetCrewRecords(crew.Id);
                Rebuild(0, 2, 4);
            }));
            var controls = new List<Control> { L("Date *"), dtp, L((isViolation ? "Violation type" : "Remark") + " *  (type to see suggestions)"), cbType, L("Related dispatch (optional)"), cbDispatch };
            if (isViolation) { controls.Add(L("Severity")); controls.Add(cbSev); }
            controls.Add(L(isViolation ? "Description *" : "Notes"));
            controls.Add(txtDesc);
            controls.Add(btns);
            controls.Add(lblMsg);
            controls.Add(L($"{kind}s on record (newest first)"));
            controls.Add(existing);
            f.Controls.AddRange(controls.ToArray());
            return ProfileKit.Card(f, D(12));
        }
        private sealed class RecordItem
        {
            public readonly CrewRecord Record;
            public RecordItem(CrewRecord r) { Record = r; }
            public override string ToString() => $"{Record.RecordDate:yyyy-MM-dd}  {Record.Type}{(string.IsNullOrWhiteSpace(Record.Severity) ? "" : $" ({Record.Severity})")}{(string.IsNullOrWhiteSpace(Record.DispatchNumber) ? "" : " · " + Record.DispatchNumber)}";
        }
    }
}
