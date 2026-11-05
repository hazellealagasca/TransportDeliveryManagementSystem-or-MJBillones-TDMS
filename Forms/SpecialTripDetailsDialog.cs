using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 6, 2026: the window that opens a saved special trip, laid out like the dispatch details window: where the trip
    /// stands (step circles), what to do next (Mark trip completed, File trip report, Make service invoice, Add to a statement,
    /// Record receipt), number tiles, its details and stops, and Edit / Reschedule / Complete one click away.
    /// Only the steps ticked on the trip are shown.
    /// </summary>
    public sealed class SpecialTripDetailsDialog : Form
    {
        private static SpecialTripDetailsDialog? active;
        /// <summary>Opens (or brings forward) the details window for a trip. <paramref name="onEdit"/> opens the full-screen form.</summary>
        public static void ShowFor(Control parent, int tripId, Action<int> onEdit, Action onChanged)
        {
            if (active != null && !active.IsDisposed)
            {
                active.onEdit = onEdit; active.onChanged = onChanged;
                active.LoadTrip(tripId);
                if (active.WindowState == FormWindowState.Minimized) active.WindowState = FormWindowState.Normal;
                active.Activate();
                return;
            }
            var dlg = new SpecialTripDetailsDialog(parent, onEdit, onChanged);
            active = dlg;
            dlg.LoadTrip(tripId);
            var owner = parent.FindForm();
            if (owner != null && !owner.IsDisposed) dlg.Show(owner); else dlg.Show();
        }

        private Action<int> onEdit;
        private Action onChanged;
        private SpecialTrip trip = new SpecialTrip();
        private SpecialTripFlowState flow = new SpecialTripFlowState();
        private Truck? truck;
        private List<Crew> crews = new();
        private string currentTab = "Overview";
        private Panel pnlBody = null!;
        private FlowLayoutPanel flowTabs = null!;
        private readonly ToolTip tips = new ToolTip { InitialDelay = 350, ShowAlways = true };
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        private SpecialTripDetailsDialog(Control parent, Action<int> onEdit, Action onChanged)
        {
            this.onEdit = onEdit; this.onChanged = onChanged;
            Text = "Special trip details";
            Font = new Font("Segoe UI", 9.5F);
            BackColor = AppTheme.Canvas;
            ForeColor = AppTheme.TextPrimary;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            MinimumSize = new Size(D(900), D(620));
            Size = new Size(D(1180), D(820));
            var wa = Screen.FromControl(parent).WorkingArea;
            if (Width > wa.Width) Width = wa.Width;
            if (Height > wa.Height) Height = wa.Height;
            StartPosition = FormStartPosition.CenterParent;
            if (parent.FindForm() is Form pf)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Math.Max(wa.X, pf.Left + (pf.Width - Width) / 2), Math.Max(wa.Y, pf.Top + (pf.Height - Height) / 2));
                if (pf.Icon != null) Icon = pf.Icon;
            }
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            FormClosed += (s, e) => { if (active == this) active = null; };
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape: Close(); return true;
                case Keys.Control | Keys.E: Edit(); return true;
                case Keys.Control | Keys.D1: ShowTab("Overview"); return true;
                case Keys.Control | Keys.D2: ShowTab("Stops"); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ------------------------------------------------------------------ Data
        private void LoadTrip(int id)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                trip = MJdatabase.GetSpecialTripById(id) ?? trip;
                flow = SpecialTripFlowState.Compute(trip, trip.WithTripReport, trip.WithServiceInvoice, trip.WithSOA, trip.WithCollection);
                truck = Safe(() => MJdatabase.GetTrucks().FirstOrDefault(t => t.Id == trip.TruckId), null);
                crews = Safe(() => MJdatabase.GetCrews(), new List<Crew>());
                BuildUi();
            }
            finally { Cursor = Cursors.Default; }
        }
        private void Reload()
        {
            if (IsDisposed) return;
            int id = trip.Id;
            if (IsHandleCreated) BeginInvoke(new Action(() => { if (!IsDisposed) LoadTrip(id); })); else LoadTrip(id);
        }
        private static T Safe<T>(Func<T> f, T fallback) { try { return f(); } catch { return fallback; } }
        private bool IsCancelled => RowMenu.Is(trip.Status, "Cancelled");
        private bool IsCompleted => RowMenu.Is(trip.Status, "Completed");
        private bool IsActive => !IsCancelled && !IsCompleted;
        private string Dest => string.IsNullOrWhiteSpace(trip.DestinationAddress) ? trip.Address : trip.DestinationAddress;

        // ------------------------------------------------------------------ Layout
        private void BuildUi()
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }
            Text = $"Special trip {trip.OrderReference}";
            var stack = new List<Control> { BuildHeader(), BuildTracker(), BuildNextStep(), BuildTiles(), BuildTabStrip() };
            var footer = BuildFooter();
            pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(16), D(24), D(16)) };
            Controls.Add(pnlBody);
            for (int i = stack.Count - 1; i >= 0; i--) Controls.Add(stack[i]);
            Controls.Add(footer);
            ResumeLayout(true);
            ShowTab(currentTab);
        }
        private (Color back, Color fore) StatusColors()
        {
            if (IsCompleted) return (AppTheme.SuccessSoft, AppTheme.SuccessText);
            if (IsCancelled) return (AppTheme.DangerSoft, AppTheme.DangerText);
            if (RowMenu.Is(trip.Status, "In-Transit")) return (AppTheme.InfoSoft, AppTheme.InfoText);
            return (AppTheme.WarningSoft, AppTheme.WarningText);
        }
        private Panel BuildHeader()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = D(96), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(14), D(18), D(10)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = AppTheme.Surface };
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, D(42)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var titleRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var lblNum = new Label { Text = trip.OrderReference, AutoSize = true, Font = new Font("Segoe UI Semibold", 17F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, D(8), 0), Cursor = Cursors.Hand };
            lblNum.Click += (s, e) => CopyText(trip.OrderReference, "Order reference copied");
            tips.SetToolTip(lblNum, "Click to copy the order reference");
            var (pb, pf) = StatusColors();
            titleRow.Controls.Add(lblNum);
            titleRow.Controls.Add(new DispatchDetailsDialog.Pill(string.IsNullOrWhiteSpace(trip.Status) ? "Pending" : trip.Status, pb, pf) { Margin = new Padding(D(4), D(10), 0, 0) });
            titleRow.Controls.Add(new DispatchDetailsDialog.Pill("Special trip", AppTheme.AccentSoft, AppTheme.AccentText) { Margin = new Padding(D(6), D(10), 0, 0) });
            var lblSub = new Label
            {
                Text = $"{trip.Company}   ·   {Dest}   ·   {trip.DispatchDate:ddd, MMM d, yyyy}  ·  {trip.DepartureTime}",
                Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.TopLeft, Margin = new Padding(D(2), D(2), 0, 0)
            };
            tips.SetToolTip(lblSub, lblSub.Text);
            tbl.Controls.Add(titleRow, 0, 0);
            tbl.Controls.Add(lblSub, 0, 1);
            p.Controls.Add(tbl);
            return p;
        }
        private Control BuildTracker()
        {
            var steps = flow.Steps.Where(s => !s.Skipped).Select(s => new DispatchDetailsDialog.Tracker.Step(s.Title, s.Done ? s.Status : (s.ActionKey != null && !s.Done ? s.Status : ""), s.Done)).ToList();
            var host = new Panel { Dock = DockStyle.Top, Height = D(86), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(8), D(24), D(6)) };
            host.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, host.Height - 1, host.Width, host.Height - 1); };
            host.Controls.Add(new DispatchDetailsDialog.Tracker(steps, IsCancelled) { Dock = DockStyle.Fill });
            return host;
        }
        private Control BuildNextStep()
        {
            var (back, fore) = flow.Tone switch
            {
                "danger" => (AppTheme.DangerSoft, AppTheme.DangerText),
                "success" => (AppTheme.SuccessSoft, AppTheme.SuccessText),
                "warn" => (AppTheme.WarningSoft, AppTheme.WarningText),
                _ => (AppTheme.InfoSoft, AppTheme.InfoText)
            };
            var host = new Panel { Dock = DockStyle.Top, Height = D(64), BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(12), D(24), 0) };
            var band = new Panel { Dock = DockStyle.Fill, BackColor = back, Padding = new Padding(D(16), D(6), D(8), D(6)) };
            string text = flow.Tone is "danger" or "success" ? flow.NextText : "Next:  " + flow.NextText;
            var lbl = new Label { Dock = DockStyle.Fill, Text = text, ForeColor = fore, Font = new Font("Segoe UI Semibold", 10F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, BackColor = back, UseMnemonic = false };
            tips.SetToolTip(lbl, flow.NextText);
            var flowBtns = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = back, Margin = new Padding(0) };
            if (flow.NextActionKey != null && flow.NextActionText != null)
            {
                var key = flow.NextActionKey; var num = flow.NextNumber;
                var b = ActionButton((key == "complete" ? "✔  " : "") + flow.NextActionText, "primary", () => RunStep(key, num));
                b.Height = D(34);
                flowBtns.Controls.Add(b);
            }
            if (IsActive)
            {
                var r = ActionButton("📅  Reschedule", "secondary", Reschedule);
                r.Height = D(34); r.Margin = new Padding(0, 0, D(8), 0);
                flowBtns.Controls.Add(r);
            }
            band.Controls.Add(lbl);
            band.Controls.Add(flowBtns);
            host.Controls.Add(band);
            return host;
        }
        private Control BuildTiles()
        {
            var ti = flow.Invoice;
            int stopCount = Math.Max(1, trip.Stops?.Count(s => !string.IsNullOrWhiteSpace(s.DeliveryDetails) || !string.IsNullOrWhiteSpace(s.StandardId)) ?? trip.TotalStopsCount);
            var tiles = new List<DispatchDetailsDialog.Tile>
            {
                new("Trip charge", trip.TripCharge > 0 ? $"₱{trip.TripCharge:N0}" : "—", string.IsNullOrWhiteSpace(trip.ModeOfPayment) ? "Mode of payment not set" : trip.ModeOfPayment),
                new("Stops", $"{stopCount}", stopCount > 1 ? "Several stops" : "Single stop"),
                new("Truck", string.IsNullOrWhiteSpace(trip.TruckPlate) ? "—" : trip.TruckPlate, string.IsNullOrWhiteSpace(trip.DriverName) ? "No driver yet" : trip.DriverName),
                new("Trip report", flow.Links.HasReport ? "Filed" : "—", flow.Links.HasReport ? flow.Links.TripReportNumber : trip.WithTripReport ? "Not filed yet" : "Not needed (unticked)"),
                new("Billing", ti != null ? $"₱{ti.Net:N0}" : "—", ti != null ? $"SI {ti.Number} · {ti.Status}" + (ti.SoaId > 0 ? $" · {ti.SoaNumber}" : "") : trip.WithServiceInvoice ? "Not billed yet" : "Not needed (unticked)",
                    ti == null ? (Color?)null : ti.Open ? AppTheme.WarningText : AppTheme.SuccessText),
                new("Collected", flow.Receipt != null ? $"₱{flow.Receipt.AmountPaid:N0}" : "—", flow.Receipt != null ? $"Receipt {flow.Receipt.ReceiptNumber} · {flow.Receipt.PaymentMethod}" : trip.WithCollection ? "No receipt yet" : "Not needed (unticked)",
                    flow.Receipt != null ? AppTheme.SuccessText : (Color?)null)
            };
            var host = new Panel { Dock = DockStyle.Top, Height = D(104), BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(12), D(24), 0) };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = tiles.Count, RowCount = 1, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            for (int i = 0; i < tiles.Count; i++)
            {
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / tiles.Count));
                tiles[i].Dock = DockStyle.Fill;
                tiles[i].Margin = new Padding(i == 0 ? 0 : D(6), 0, i == tiles.Count - 1 ? 0 : D(6), 0);
                tbl.Controls.Add(tiles[i], i, 0);
            }
            host.Controls.Add(tbl);
            return host;
        }
        private Control BuildTabStrip()
        {
            var host = new Panel { Dock = DockStyle.Top, Height = D(50), BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(10), D(24), 0) };
            var line = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = AppTheme.Border };
            flowTabs = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            AddTabButton("Overview", "Overview");
            AddTabButton("Stops", $"Stops  {trip.Stops?.Count ?? 0}");
            AddTabButton("Steps", "Steps");
            host.Controls.Add(flowTabs);
            host.Controls.Add(line);
            return host;
        }
        private void AddTabButton(string key, string text)
        {
            var b = new Button { Text = text, Tag = key, AutoSize = true, Height = D(38), Padding = new Padding(D(12), 0, D(12), 0), Margin = new Padding(0, 0, D(4), 0), FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Canvas, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, UseMnemonic = false };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
            b.FlatAppearance.MouseDownBackColor = AppTheme.SurfaceHover;
            b.Paint += (s, e) =>
            {
                if ((string)b.Tag! != currentTab) return;
                using var br = new SolidBrush(AppTheme.Accent);
                e.Graphics.FillRectangle(br, D(8), b.Height - D(3), b.Width - D(16), D(3));
            };
            b.Click += (s, e) => ShowTab(key);
            flowTabs.Controls.Add(b);
        }
        private void ShowTab(string key)
        {
            if (pnlBody == null) return;
            currentTab = key;
            foreach (Control c in flowTabs.Controls) { c.ForeColor = (c.Tag as string) == key ? AppTheme.TextPrimary : AppTheme.TextMuted; c.Invalidate(); }
            pnlBody.SuspendLayout();
            foreach (Control c in pnlBody.Controls.Cast<Control>().ToList()) { pnlBody.Controls.Remove(c); c.Dispose(); }
            Control content = key switch { "Stops" => BuildStopsTab(), "Steps" => BuildStepsTab(), _ => BuildOverviewTab() };
            content.Dock = DockStyle.Fill;
            pnlBody.Controls.Add(content);
            pnlBody.ResumeLayout(true);
        }
        private string Person(int id, string fallback)
        {
            var c = id > 0 ? crews.FirstOrDefault(x => x.Id == id) : null;
            string name = c?.FullName ?? fallback;
            if (string.IsNullOrWhiteSpace(name)) return "—";
            return c != null && !string.IsNullOrWhiteSpace(c.ContactNumber) ? $"{name}  ·  {c.ContactNumber}" : name;
        }
        private Control BuildOverviewTab()
        {
            var scroll = new Panel { AutoScroll = true, BackColor = AppTheme.Canvas };
            var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            var left = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0, 0, D(8), 0) };
            var right = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(D(8), 0, 0, 0) };
            var client = Safe(() => MJdatabase.GetClientByCompanyName(trip.Company), null);
            var order = new List<(string, string, bool)>
            {
                ("Client", string.IsNullOrWhiteSpace(trip.Company) ? "—" : trip.Company, true),
                ("Client address", string.IsNullOrWhiteSpace(trip.Address) ? (client?.Address ?? "—") : trip.Address, true),
                ("Destination", string.IsNullOrWhiteSpace(Dest) ? "—" : Dest, true),
                ("Trip charge", trip.TripCharge > 0 ? $"PHP {trip.TripCharge:N2}" : "—", false),
                ("Mode of payment", string.IsNullOrWhiteSpace(trip.ModeOfPayment) ? "—" : trip.ModeOfPayment, false)
            };
            var crewRows = new List<(string, string, bool)>
            {
                ("Truck", string.IsNullOrWhiteSpace(trip.TruckPlate) ? "—" : trip.TruckPlate, true),
                ("Capacity", truck != null ? $"{truck.CapacityCrates:N0} crates · {truck.CargoType}" : "—", false),
                ("Driver", Person(trip.DriverId, trip.DriverName), true)
            };
            var aideIds = trip.AllAideIds ?? new List<int>();
            if (aideIds.Count == 0) crewRows.Add(("Aides", string.IsNullOrWhiteSpace(trip.AideName) ? "—" : trip.AideName, true));
            else { int n = 1; foreach (var id in aideIds) crewRows.Add((aideIds.Count == 1 ? "Aide" : $"Aide {n++}", Person(id, $"Crew #{id}"), true)); }
            int days = (trip.DispatchDate.Date - DateTime.Today).Days;
            var sched = new List<(string, string, bool)>
            {
                ("Dispatch date", trip.DispatchDate.ToString("dddd, MMMM d, yyyy"), false),
                ("Departure time", string.IsNullOrWhiteSpace(trip.DepartureTime) ? "—" : trip.DepartureTime, false),
                ("Timing", days == 0 ? "Today" : days > 0 ? $"In {days} day{(days == 1 ? "" : "s")}" : $"{-days} day{(days == -1 ? "" : "s")} ago", false),
                ("Status", string.IsNullOrWhiteSpace(trip.Status) ? "Pending" : trip.Status, false),
                ("Completed", trip.CompletedAt.HasValue ? trip.CompletedAt.Value.ToString("MMM d, yyyy h:mm tt") : "—", false)
            };
            var steps = new List<(string, string, bool)>
            {
                ("With Trip Report", trip.WithTripReport ? "Yes" : "No", false),
                ("With Service Invoice", trip.WithServiceInvoice ? "Yes" : "No", false),
                ("With SOA", trip.WithSOA ? "Yes" : "No", false),
                ("With Collection Receipt", trip.WithCollection ? "Yes" : "No", false)
            };
            AddCards(left, InfoCard("Trip & client", order), InfoCard("Schedule", sched));
            AddCards(right, InfoCard("Truck & crew", crewRows), InfoCard("Steps ticked for this trip", steps));
            grid.Controls.Add(left, 0, 0);
            grid.Controls.Add(right, 1, 0);
            scroll.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(30), Text = "Tip: click any underlined value to copy it. Edit the trip to change the ticked steps.", ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 8.75F), TextAlign = ContentAlignment.BottomLeft });
            scroll.Controls.Add(grid);
            return scroll;
        }
        private Control BuildStopsTab()
        {
            var host = new Panel { BackColor = AppTheme.Canvas };
            var stops = (trip.Stops ?? new List<SpecialTripStop>()).Where(s => !string.IsNullOrWhiteSpace(s.DeliveryDetails) || !string.IsNullOrWhiteSpace(s.StandardId)).OrderBy(s => s.SequenceNo).ToList();
            var g = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9.5F) };
            UIThemeHelper.ApplyGridTheme(g);
            g.RowTemplate.Height = D(34);
            g.ColumnHeadersHeight = D(36);
            g.Columns.Add("c1", "#"); g.Columns.Add("c2", "Standard ID"); g.Columns.Add("c3", "Delivery details"); g.Columns.Add("c4", "Driver route"); g.Columns.Add("c5", "Departure");
            int[] w = { 34, 90, 260, 160, 90 };
            for (int i = 0; i < w.Length; i++) g.Columns[i].FillWeight = w[i];
            int n = 1;
            foreach (var s in stops) g.Rows.Add(n++, s.StandardId, s.DeliveryDetails, s.DriverRoute, s.DepartureTime);
            if (stops.Count == 0) g.Rows.Add("", "", string.IsNullOrWhiteSpace(Dest) ? "No stops saved" : "Destination: " + Dest, "", trip.DepartureTime);
            host.Controls.Add(g);
            return host;
        }
        private Control BuildStepsTab()
        {
            var rows = flow.Steps.Select(s => (s.Title, (s.Done ? "✓ " : "") + (string.IsNullOrWhiteSpace(s.Status) ? "—" : s.Status), false)).ToList();
            var scroll = new Panel { AutoScroll = true, BackColor = AppTheme.Canvas };
            var card = InfoCard("Where this trip stands", rows);
            card.Dock = DockStyle.Top;
            scroll.Controls.Add(card);
            return scroll;
        }
        private void AddCards(Panel column, params Control[] cards)
        {
            for (int i = cards.Length - 1; i >= 0; i--)
            {
                cards[i].Dock = DockStyle.Top;
                column.Controls.Add(cards[i]);
                if (i > 0) column.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = AppTheme.Canvas });
            }
        }
        private Control InfoCard(string title, List<(string Label, string Value, bool Copy)> rows)
        {
            int rowH = D(32), pad = D(16);
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(pad, D(12), pad, D(12)) };
            card.Height = D(12) + D(32) + rows.Count * rowH + D(14);
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var lblTitle = new Label { Dock = DockStyle.Top, Height = D(32), Text = title, Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.TopLeft };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Top, Height = rows.Count * rowH, ColumnCount = 2, RowCount = rows.Count, BackColor = AppTheme.Surface };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(170)));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < rows.Count; i++)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
                var (label, value, copy) = rows[i];
                var l = new Label { Text = label, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) };
                var v = new Label { Text = value, Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI Semibold", 9.75F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0), UseMnemonic = false };
                if (copy && value != "—" && !string.IsNullOrWhiteSpace(value))
                {
                    v.Cursor = Cursors.Hand;
                    v.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Underline);
                    v.Click += (s, e) => CopyText(value, $"{label} copied");
                    tips.SetToolTip(v, value + "\n(click to copy)");
                }
                else tips.SetToolTip(v, value);
                tbl.Controls.Add(l, 0, i);
                tbl.Controls.Add(v, 1, i);
            }
            card.Controls.Add(tbl);
            card.Controls.Add(lblTitle);
            return card;
        }
        private Panel BuildFooter()
        {
            var p = new Panel { Dock = DockStyle.Bottom, Height = D(68), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(14), D(24), D(14)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); };
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var btnEdit = MakeButton("✎  Edit trip", "strong", D(124));
            btnEdit.Click += (s, e) => Edit();
            tips.SetToolTip(btnEdit, "Open this trip in the full-screen form (Ctrl+E)");
            left.Controls.Add(btnEdit);
            if (IsActive)
            {
                var btnRes = MakeButton("📅  Reschedule", "secondary", D(136));
                btnRes.Click += (s, e) => Reschedule();
                left.Controls.Add(btnRes);
                var btnComplete = MakeButton("✔  Complete", "primary", D(124));
                btnComplete.Click += (s, e) => RunStep("complete", null);
                tips.SetToolTip(btnComplete, "The truck is back: mark the special trip completed");
                left.Controls.Add(btnComplete);
            }
            else if (IsCompleted && trip.WithTripReport)
            {
                var btnTrip = MakeButton(flow.Links.HasReport ? "📝  Trip report" : "📝  File trip report", "secondary", D(160));
                btnTrip.Click += (s, e) => RunStep("report", null);
                left.Controls.Add(btnTrip);
            }
            foreach (Control c in left.Controls) c.Margin = new Padding(0, 0, D(8), 0);
            var btnClose = MakeButton("Close", "secondary", D(96));
            btnClose.Click += (s, e) => Close();
            var btnCopy = MakeButton("⧉  Copy summary", "secondary", D(150));
            btnCopy.Click += (s, e) => CopyText(SummaryText(), "Trip summary copied, ready to paste");
            foreach (var b in new[] { btnClose, btnCopy }) { b.Margin = new Padding(D(8), 0, 0, 0); right.Controls.Add(b); }
            p.Controls.Add(left);
            p.Controls.Add(right);
            return p;
        }

        // ------------------------------------------------------------------ Actions
        private void Edit()
        {
            int id = trip.Id;
            var go = onEdit;
            Close();
            go(id);
        }
        private void Reschedule()
        {
            if (!IsActive) return;
            using var dlg = new RescheduleDeliveryDialog(trip);
            if (dlg.ShowDialog(this) == DialogResult.OK) { onChanged(); Reload(); }
        }
        /// <summary>A step's button: complete here; the other steps close this window and open their screen with the trip picked.</summary>
        private void RunStep(string key, string? number)
        {
            if (key == "complete")
            {
                if (!IsActive) return;
                if (trip.DispatchDate.Date > DateTime.Today &&
                    MessageBox.Show(this, $"This trip is dated {trip.DispatchDate:MMMM d, yyyy}, which is still ahead. Mark it completed anyway?", "Trip is in the future", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                if (MessageBox.Show(this, $"Mark special trip {trip.OrderReference} for {trip.Company} as completed?\n\nThe truck, driver and aides are freed. The next steps ({NextStepsText()}) open from here.",
                        "Complete trip", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                try
                {
                    if (MJdatabase.MarkSpecialTripCompleted(trip.Id))
                    {
                        ToastHelper.ShowToast(this, $"{trip.OrderReference} marked completed", ToastType.Success);
                        onChanged();
                        Reload();
                    }
                }
                catch (Exception ex) { ErrLog.Error("SpecialTripDetailsDialog.Complete", ex); MessageBox.Show(this, "Couldn't complete the trip: " + ex.Message, "Complete trip", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                return;
            }
            var t = trip;
            Close();
            MainForm.Instance?.GoToSpecialTripStep(key, t, number);
        }
        private string NextStepsText()
        {
            var l = new List<string>();
            if (trip.WithTripReport) l.Add("trip report");
            if (trip.WithServiceInvoice) l.Add("service invoice");
            if (trip.WithSOA) l.Add("SOA");
            if (trip.WithCollection) l.Add("collection receipt");
            return l.Count == 0 ? "none ticked" : string.Join(", ", l);
        }
        private string SummaryText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SPECIAL TRIP {trip.OrderReference}  [{trip.Status}]");
            sb.AppendLine($"Client: {trip.Company}");
            sb.AppendLine($"Destination: {Dest}");
            sb.AppendLine($"Date: {trip.DispatchDate:ddd, MMM d, yyyy} · {trip.DepartureTime}");
            sb.AppendLine($"Truck: {trip.TruckPlate}");
            sb.AppendLine($"Driver: {Person(trip.DriverId, trip.DriverName)}");
            sb.AppendLine($"Aides: {trip.AideName}");
            if (trip.TripCharge > 0) sb.AppendLine($"Trip charge: PHP {trip.TripCharge:N2} ({trip.ModeOfPayment})");
            return sb.ToString().TrimEnd();
        }
        private void CopyText(string text, string toast)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try { Clipboard.SetText(text); ToastHelper.ShowToast(this, toast, ToastType.Success); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        private Button MakeButton(string text, string role, int width)
        {
            var b = new Button { Text = text, Width = width, Height = D(38), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F), Cursor = Cursors.Hand, UseMnemonic = false, Margin = new Padding(0) };
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }
        private Button ActionButton(string text, string role, Action act)
        {
            var b = MakeButton(text, role, 10);
            b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(D(100), D(36));
            b.Padding = new Padding(D(10), 0, D(10), 0);
            b.Click += (s, e) => act();
            return b;
        }
    }
}
