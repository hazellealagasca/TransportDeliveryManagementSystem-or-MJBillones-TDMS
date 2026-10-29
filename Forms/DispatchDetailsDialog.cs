using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// The window that opens when a dispatch order is double-clicked: where the order stands, what to do next,
    /// its stores, linked documents, activity and notes, with the common actions one click away.
    /// Layout preferences (which panels show, the opening tab, density, window size) are remembered per user.
    /// </summary>
    public sealed class DispatchDetailsDialog : Form
    {
        // ------------------------------------------------------------------ Opening
        private static DispatchDetailsDialog? active;
        public static void ShowFor(Control parent, Delivery delivery)
        {
            if (delivery == null) return;
            var siblings = FindSiblingDeliveries(parent);
            if (active != null && !active.IsDisposed)
            {
                active.parentView = parent;
                active.siblings = siblings;
                active.LoadDelivery(delivery);
                if (active.WindowState == FormWindowState.Minimized) active.WindowState = FormWindowState.Normal;
                active.Activate();
                return;
            }
            var dlg = new DispatchDetailsDialog(parent, siblings);
            active = dlg;
            dlg.LoadDelivery(delivery);
            var owner = parent.FindForm();
            if (owner != null && !owner.IsDisposed) dlg.Show(owner); else dlg.Show();
        }
        /// <summary>The orders listed in the table the user double-clicked, in on-screen order, for Previous / Next.</summary>
        private static List<Delivery> FindSiblingDeliveries(Control parent)
        {
            var queue = new Queue<Control>();
            queue.Enqueue(parent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c is DataGridView g && g.Visible)
                {
                    var list = new List<Delivery>();
                    foreach (DataGridViewRow r in g.Rows)
                        if (r.DataBoundItem is Delivery d) list.Add(d);
                    if (list.Count > 0) return list;
                }
                foreach (Control child in c.Controls) queue.Enqueue(child);
            }
            return new List<Delivery>();
        }
        /// <summary>A plain-text summary of an order, ready to paste into Viber, SMS or email.</summary>
        public static string BuildSummaryText(Delivery d)
        {
            var crews = SafeGet(() => MJdatabase.GetCrews(), new List<Crew>());
            var stops = SafeGet(() => MJdatabase.GetDispatchStops(d.Id), new List<DispatchStop>());
            string Person(int id, string fallback)
            {
                var c = id > 0 ? crews.FirstOrDefault(x => x.Id == id) : null;
                string name = c?.FullName ?? fallback;
                return c != null && !string.IsNullOrWhiteSpace(c.ContactNumber) ? $"{name} ({c.ContactNumber})" : name;
            }
            var sb = new StringBuilder();
            sb.AppendLine($"DISPATCH {d.DispatchNumber}  [{d.Status}]");
            if (!string.IsNullOrWhiteSpace(d.ClientName)) sb.AppendLine($"Client: {d.ClientName}");
            sb.AppendLine($"Route: {(string.IsNullOrWhiteSpace(d.Destination) ? d.CleanRouteDisplay : d.Destination)}");
            sb.AppendLine($"Date: {d.DispatchDate:ddd, MMM d, yyyy} · {d.DepartureTime}");
            sb.AppendLine($"Truck: {d.TruckInfo}");
            sb.AppendLine($"Driver: {Person(d.DriverId, d.DriverName)}");
            var aides = MJdatabase.GetDeliveryCrewIds(d).Where(x => x.Role == "Aide").Select(x => Person(x.CrewId, "")).Where(x => x.Length > 0).ToList();
            sb.AppendLine($"Aides: {(aides.Count > 0 ? string.Join("; ", aides) : d.AideDisplay)}");
            sb.AppendLine($"Crates: {d.TotalCrates:N0} (G {d.GCrate:N0} · E {d.ECrate:N0})");
            if (stops.Count > 0)
            {
                sb.AppendLine($"Stores ({stops.Count}):");
                foreach (var st in stops.OrderBy(x => x.StopSequence))
                    sb.AppendLine($"{st.StopSequence}. {(string.IsNullOrWhiteSpace(st.StoreCode) ? "" : st.StoreCode + " - ")}{st.Destination}  (G {st.GCrate}, E {st.ECrate})");
            }
            if (!string.IsNullOrWhiteSpace(d.Remarks)) sb.AppendLine($"Notes: {d.Remarks}");
            return sb.ToString().TrimEnd();
        }
        /// <summary>Driver and aides with phone numbers, one per line.</summary>
        public static string BuildCrewContactsText(Delivery d)
        {
            var crews = SafeGet(() => MJdatabase.GetCrews(), new List<Crew>());
            var lines = new List<string>();
            foreach (var (id, role) in MJdatabase.GetDeliveryCrewIds(d))
            {
                var c = crews.FirstOrDefault(x => x.Id == id);
                if (c == null) continue;
                lines.Add($"{role}: {c.FullName}{(string.IsNullOrWhiteSpace(c.ContactNumber) ? "" : " · " + c.ContactNumber)}");
            }
            if (lines.Count == 0) lines.Add($"Driver: {d.DriverName}");
            return string.Join(Environment.NewLine, lines);
        }
        // ------------------------------------------------------------------ State
        private Control parentView;
        private List<Delivery> siblings;
        private Delivery del = new Delivery();
        private List<DispatchStop> stops = new List<DispatchStop>();
        private LoadingSchedule? schedule;
        private ClientCompany? client;
        private TripReport? trip;
        private Invoice? invoice;
        private List<IncidentReport> incidents = new List<IncidentReport>();
        private List<AuditTrailEntry> activity = new List<AuditTrailEntry>();
        private List<TripCrewPay> crewPay = new List<TripCrewPay>();
        private List<Crew> crews = new List<Crew>();
        private Truck? truck;
        private string clientName = "";
        private string routeText = "";
        private string currentTab = "Overview";
        private readonly ToolTip tips = new ToolTip { InitialDelay = 350, ShowAlways = true };
        private Panel pnlBody = null!;
        private FlowLayoutPanel flowTabs = null!;
        private TextBox? txtNotes;
        private Prefs prefs;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private DispatchDetailsDialog(Control parent, List<Delivery> siblings)
        {
            parentView = parent;
            this.siblings = siblings;
            prefs = Prefs.Load();
            Text = "Dispatch details";
            Font = new Font("Segoe UI", 9.5F);
            BackColor = AppTheme.Canvas;
            ForeColor = AppTheme.TextPrimary;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(D(900), D(620));
            Size = prefs.RememberSize && prefs.Width > 0 ? new Size(Math.Max(D(900), prefs.Width), Math.Max(D(620), prefs.Height)) : new Size(D(1180), D(820));
            var wa = Screen.FromControl(parent).WorkingArea;
            if (Width > wa.Width) Width = wa.Width;
            if (Height > wa.Height) Height = wa.Height;
            if (parent.FindForm() is Form pf)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Math.Max(wa.X, pf.Left + (pf.Width - Width) / 2), Math.Max(wa.Y, pf.Top + (pf.Height - Height) / 2));
                if (pf.Icon != null) Icon = pf.Icon;
            }
            if (prefs.RememberSize && prefs.Maximized) WindowState = FormWindowState.Maximized;
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            FormClosed += (s, e) => { if (active == this) active = null; };
            FormClosing += (s, e) =>
            {
                if (!prefs.RememberSize) return;
                prefs.Maximized = WindowState == FormWindowState.Maximized;
                var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                prefs.Width = b.Width;
                prefs.Height = b.Height;
                prefs.Save();
            };
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape: Close(); return true;
                case Keys.Alt | Keys.Left: Navigate(-1); return true;
                case Keys.Alt | Keys.Right: Navigate(1); return true;
                case Keys.Control | Keys.E: EditOrder(); return true;
                case Keys.Control | Keys.P: PrintDispatchSheet(); return true;
                case Keys.Control | Keys.Shift | Keys.C: CopySummary(); return true;
                case Keys.Control | Keys.D1: ShowTab("Overview"); return true;
                case Keys.Control | Keys.D2: ShowTab("Stores"); return true;
                case Keys.Control | Keys.D3: ShowTab("Documents"); return true;
                case Keys.Control | Keys.D4: ShowTab("Activity"); return true;
                case Keys.Control | Keys.D5: ShowTab("Notes"); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ------------------------------------------------------------------ Data
        private void LoadDelivery(Delivery d, bool keepTab = false)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                del = MJdatabase.GetDeliveryById(d.Id) ?? d;
                stops = SafeGet(() => MJdatabase.GetDispatchStops(del.Id), new List<DispatchStop>());
                if (stops.Count == 0 && del.Stops != null) stops = del.Stops;
                schedule = del.LoadingScheduleId > 0 ? SafeGet(() => MJdatabase.GetLoadingSchedules().FirstOrDefault(s => s.Id == del.LoadingScheduleId), null) : null;
                clientName = !string.IsNullOrWhiteSpace(del.ClientName) ? del.ClientName : (schedule?.ClientName ?? "");
                if (string.IsNullOrWhiteSpace(clientName))
                    clientName = string.Join(", ", stops.Select(s => s.ClientName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct());
                if (string.IsNullOrWhiteSpace(clientName)) clientName = "Direct client";
                client = SafeGet(() => MJdatabase.GetClientByCompanyName(clientName), null);
                trip = SafeGet(() => MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == del.Id || (!string.IsNullOrEmpty(t.DispatchNumber) && t.DispatchNumber == del.DispatchNumber)), null);
                invoice = SafeGet(() => MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == del.Id), null);
                incidents = SafeGet(() => MJdatabase.GetIncidentReports().Where(i => i.DeliveryId == del.Id).ToList(), new List<IncidentReport>());
                crewPay = SafeGet(() => MJdatabase.GetTripCrewPay(del.Id), new List<TripCrewPay>());
                crews = SafeGet(() => MJdatabase.GetCrews(), new List<Crew>());
                truck = SafeGet(() => MJdatabase.GetTrucks().FirstOrDefault(t => t.Id == del.TruckId), null);
                string num = del.DispatchNumber ?? "";
                activity = SafeGet(() => MJdatabase.GetAuditTrail(null, null, 3000).Where(a =>
                        (a.TableName == "Deliveries" && a.RecordId == del.Id) ||
                        (a.TableName == "TripCrewPay" && a.RecordId == del.Id) ||
                        (num.Length > 0 && ((a.RecordIdentifier ?? "").Contains(num) || (a.ReasonOrNotes ?? "").Contains(num) || (a.NewValue ?? "").Contains(num))))
                    .OrderByDescending(a => a.Timestamp).ToList(), new List<AuditTrailEntry>());
                routeText = !string.IsNullOrWhiteSpace(del.Destination) ? del.Destination.Trim() : (schedule?.Destination ?? del.CleanRouteDisplay);
                if (!keepTab) currentTab = prefs.DefaultTab;
                BuildUi();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        /// <summary>Rebuilds after the current click has finished, so the clicked button isn't disposed mid-click.</summary>
        private void Reload(Delivery d, bool keepTab = true)
        {
            if (IsDisposed) return;
            if (IsHandleCreated) BeginInvoke(new Action(() => { if (!IsDisposed) LoadDelivery(d, keepTab); }));
            else LoadDelivery(d, keepTab);
        }
        private static T SafeGet<T>(Func<T> f, T fallback)
        {
            try { return f(); } catch { return fallback; }
        }
        private bool IsCancelled => string.Equals(del.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
        private bool IsCompleted => string.Equals(del.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        private bool IsInTransit => string.Equals(del.Status, "In Transit", StringComparison.OrdinalIgnoreCase);
        private bool IsActiveOrder => !IsCancelled && !IsCompleted;
        private bool IsPaid => invoice != null && string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase);
        private Crew? FindCrew(int id) => id > 0 ? crews.FirstOrDefault(c => c.Id == id) : null;
        private string StatusLabel()
        {
            if (IsInTransit && MJdatabase.IsAwaitingCompletion(del)) return "Awaiting completion";
            return string.IsNullOrWhiteSpace(del.Status) ? "Scheduled" : del.Status;
        }
        private (Color back, Color fore) StatusColors()
        {
            if (IsCompleted) return (AppTheme.SuccessSoft, AppTheme.SuccessText);
            if (IsCancelled) return (AppTheme.DangerSoft, AppTheme.DangerText);
            if (IsInTransit) return MJdatabase.IsAwaitingCompletion(del) ? (AppTheme.WarningSoft, AppTheme.WarningText) : (AppTheme.InfoSoft, AppTheme.InfoText);
            if (string.Equals(del.Status, "Rescheduled", StringComparison.OrdinalIgnoreCase)) return (AppTheme.AccentSoft, AppTheme.AccentText);
            return (AppTheme.WarningSoft, AppTheme.WarningText);
        }
        // ------------------------------------------------------------------ Layout
        private void BuildUi()
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }
            txtNotes = null;
            Text = $"Dispatch {del.DispatchNumber}";
            var header = BuildHeader();
            var footer = BuildFooter();
            pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(16), D(24), D(16)) };
            var tabs = BuildTabStrip();
            var stack = new List<Control> { header };
            if (prefs.ShowTracker) stack.Add(BuildTracker());
            if (prefs.ShowNextStep) stack.Add(BuildNextStep());
            if (prefs.ShowTiles) stack.Add(BuildTiles());
            stack.Add(tabs);
            Controls.Add(pnlBody);
            for (int i = stack.Count - 1; i >= 0; i--) Controls.Add(stack[i]);
            Controls.Add(footer);
            ResumeLayout(true);
            ShowTab(currentTab);
        }
        private Panel BuildHeader()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = D(96), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(14), D(18), D(10)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = AppTheme.Surface };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, D(42)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var titleRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var lblNum = new Label
            {
                Text = del.DispatchNumber,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 17F),
                ForeColor = AppTheme.TextPrimary,
                Margin = new Padding(0, 0, D(8), 0),
                Cursor = Cursors.Hand
            };
            lblNum.Click += (s, e) => CopyText(del.DispatchNumber, "Dispatch number copied");
            tips.SetToolTip(lblNum, "Click to copy the dispatch number");
            var (pb, pf) = StatusColors();
            var pill = new Pill(StatusLabel(), pb, pf) { Margin = new Padding(D(4), D(10), 0, 0) };
            titleRow.Controls.Add(lblNum);
            titleRow.Controls.Add(pill);
            if (del.IsPeak) titleRow.Controls.Add(new Pill("Peak season rate", AppTheme.AccentSoft, AppTheme.AccentText) { Margin = new Padding(D(6), D(10), 0, 0) });
            string when = $"{del.DispatchDate:ddd, MMM d, yyyy}  ·  {del.DepartureTime}";
            var lblSub = new Label
            {
                Text = $"{clientName}   ·   {routeText}   ·   {when}",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppTheme.TextSecondary,
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(D(2), D(2), 0, 0)
            };
            tips.SetToolTip(lblSub, lblSub.Text);
            var right = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0), Anchor = AnchorStyles.Right | AnchorStyles.Top };
            int idx = siblings.FindIndex(x => x.Id == del.Id);
            var btnPrev = MakeButton("‹  Previous", "secondary", D(110));
            var btnNext = MakeButton("Next  ›", "secondary", D(96));
            btnPrev.Enabled = idx > 0;
            btnNext.Enabled = idx >= 0 && idx < siblings.Count - 1;
            btnPrev.Click += (s, e) => Navigate(-1);
            btnNext.Click += (s, e) => Navigate(1);
            tips.SetToolTip(btnPrev, "Previous order in the list (Alt+←)");
            tips.SetToolTip(btnNext, "Next order in the list (Alt+→)");
            var lblPos = new Label
            {
                Text = idx >= 0 ? $"{idx + 1} of {siblings.Count}" : "",
                AutoSize = true,
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(D(8), D(10), D(8), 0)
            };
            var btnCustomize = MakeButton("⚙  Customize", "secondary", D(124));
            btnCustomize.Margin = new Padding(D(14), 0, 0, 0);
            btnCustomize.Click += (s, e) => BuildCustomizeMenu().Show(btnCustomize, new Point(0, btnCustomize.Height));
            right.Controls.Add(btnPrev);
            right.Controls.Add(lblPos);
            right.Controls.Add(btnNext);
            right.Controls.Add(btnCustomize);
            tbl.Controls.Add(titleRow, 0, 0);
            tbl.Controls.Add(right, 1, 0);
            tbl.Controls.Add(lblSub, 0, 1);
            tbl.SetColumnSpan(lblSub, 2);
            p.Controls.Add(tbl);
            return p;
        }
        private Control BuildTracker()
        {
            var steps = new List<Tracker.Step>
            {
                new Tracker.Step("Scheduled", del.DispatchDate.ToString("MMM d"), true),
                new Tracker.Step("In transit", IsInTransit ? "On the road" : "", IsInTransit || IsCompleted || trip != null),
                new Tracker.Step("Delivered", IsCompleted ? "Order complete" : "", IsCompleted),
                new Tracker.Step("Trip report", trip != null ? trip.ReportDate.ToString("MMM d") : "", trip != null),
                new Tracker.Step("Invoiced", invoice != null ? invoice.IssueDate.ToString("MMM d") : "", invoice != null),
                new Tracker.Step("Paid", IsPaid ? "Settled" : "", IsPaid)
            };
            var host = new Panel { Dock = DockStyle.Top, Height = D(86), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(8), D(24), D(6)) };
            host.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, host.Height - 1, host.Width, host.Height - 1); };
            host.Controls.Add(new Tracker(steps, IsCancelled) { Dock = DockStyle.Fill });
            return host;
        }
        private Control BuildNextStep()
        {
            string text;
            Color back, fore;
            var actions = new List<Button>();
            int daysAgo = (DateTime.Today - del.DispatchDate.Date).Days;
            if (IsCancelled)
            {
                text = "This order was cancelled." + (string.IsNullOrWhiteSpace(del.CancellationReason) ? "" : $"  Reason: {del.CancellationReason}");
                back = AppTheme.DangerSoft; fore = AppTheme.DangerText;
            }
            else if (IsInTransit)
            {
                text = MJdatabase.IsAwaitingCompletion(del)
                    ? $"On the road since {del.DispatchDate:MMM d} ({daysAgo} day{(daysAgo == 1 ? "" : "s")}). Mark it complete once the truck is back."
                    : "The truck is on the road. Mark the order complete when it returns.";
                back = MJdatabase.IsAwaitingCompletion(del) ? AppTheme.WarningSoft : AppTheme.InfoSoft;
                fore = MJdatabase.IsAwaitingCompletion(del) ? AppTheme.WarningText : AppTheme.InfoText;
                actions.Add(ActionButton("✔  Mark order complete", "primary", CompleteOrder));
            }
            else if (IsActiveOrder)
            {
                text = daysAgo < 0
                    ? $"Departs in {-daysAgo} day{(daysAgo == -1 ? "" : "s")}, on {del.DispatchDate:ddd, MMM d} at {del.DepartureTime}."
                    : daysAgo == 0 ? $"Departs today at {del.DepartureTime}." : $"Was due to leave {daysAgo} day{(daysAgo == 1 ? "" : "s")} ago and hasn't been marked in transit or complete.";
                back = AppTheme.AccentSoft; fore = AppTheme.AccentText;
                actions.Add(ActionButton("📅  Reschedule", "secondary", RescheduleOrder));
                actions.Add(ActionButton("✎  Edit order", "primary", EditOrder));
            }
            else if (trip == null)
            {
                text = "Delivered. File the trip report to record store sign-offs, returned crates and crew pay.";
                back = AppTheme.InfoSoft; fore = AppTheme.InfoText;
                actions.Add(ActionButton("📝  File trip report", "primary", OpenTripReport));
            }
            else if (invoice == null)
            {
                text = "Trip report filed. This order is ready to bill.";
                back = AppTheme.InfoSoft; fore = AppTheme.InfoText;
                actions.Add(ActionButton("💰  Create invoice", "primary", CreateInvoice));
            }
            else if (!IsPaid)
            {
                text = $"Invoice {invoice.InvoiceNumber} for PHP {invoice.NetAmount:N2} is {invoice.Status.ToLowerInvariant()}" + (invoice.DueDate.Date < DateTime.Today ? $" and was due {invoice.DueDate:MMM d}." : $", due {invoice.DueDate:MMM d}.");
                back = invoice.DueDate.Date < DateTime.Today ? AppTheme.WarningSoft : AppTheme.InfoSoft;
                fore = invoice.DueDate.Date < DateTime.Today ? AppTheme.WarningText : AppTheme.InfoText;
                actions.Add(ActionButton("Open billing", "primary", () => { MainForm.Instance?.NavigateToFinance("Billing"); Close(); }));
            }
            else
            {
                text = "All done: delivered, reported, billed and paid.";
                back = AppTheme.SuccessSoft; fore = AppTheme.SuccessText;
            }
            var host = new Panel { Dock = DockStyle.Top, Height = D(64), BackColor = AppTheme.Canvas, Padding = new Padding(D(24), D(12), D(24), 0) };
            var band = new Panel { Dock = DockStyle.Fill, BackColor = back, Padding = new Padding(D(16), D(6), D(8), D(6)) };
            var lbl = new Label { Dock = DockStyle.Fill, Text = "Next:  " + text, ForeColor = fore, Font = new Font("Segoe UI Semibold", 10F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, BackColor = back };
            if (IsCancelled || (IsCompleted && IsPaid)) lbl.Text = text;
            tips.SetToolTip(lbl, text);
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = back, Margin = new Padding(0) };
            foreach (var b in actions.AsEnumerable().Reverse()) { b.Height = D(34); b.Margin = new Padding(D(8), 0, 0, 0); flow.Controls.Add(b); }
            band.Controls.Add(lbl);
            band.Controls.Add(flow);
            host.Controls.Add(band);
            return host;
        }
        private Control BuildTiles()
        {
            int total = del.TotalCrates > 0 ? del.TotalCrates : del.GCrate + del.ECrate;
            int storeCount = stops.Count > 0 ? stops.Count : Math.Max(1, del.TotalStopsCount);
            int cap = truck?.CapacityCrates ?? 0;
            int pct = cap > 0 ? (int)Math.Round(total * 100.0 / cap) : 0;
            decimal payRecorded = crewPay.Sum(p => p.NetPay);
            decimal payEstimate = 0;
            foreach (var (id, _) in MJdatabase.GetDeliveryCrewIds(del))
            {
                var c = FindCrew(id);
                if (c != null) payEstimate += c.DailyWage + c.FoodAllowance;
            }
            var tiles = new List<Tile>
            {
                new Tile("Crates", $"{total:N0}", $"G {del.GCrate:N0}  ·  E {del.ECrate:N0}"),
                new Tile("Stores", $"{storeCount}", storeCount > 1 ? "Multi-drop route" : "Single drop"),
                new Tile("Truck load", cap > 0 ? $"{pct}%" : "—", cap > 0 ? $"{total:N0} of {cap:N0} crates" : "Truck capacity not set", pct > 100 ? AppTheme.DangerText : (Color?)null),
                new Tile("Delivered / returned", trip != null ? $"{trip.DeliveredCrates:N0} / {trip.ReturnedCrates:N0}" : "—", trip != null ? $"Signed by {Shorten(trip.StoreManagerName, 22)}" : "No trip report yet"),
                new Tile("Billing", invoice != null ? $"₱{invoice.NetAmount:N0}" : "—", invoice != null ? $"{invoice.InvoiceNumber} · {invoice.Status}" : "Not billed yet",
                    invoice == null ? (Color?)null : IsPaid ? AppTheme.SuccessText : AppTheme.WarningText),
                new Tile("Crew pay", crewPay.Count > 0 ? $"₱{payRecorded:N0}" : (payEstimate > 0 ? $"≈ ₱{payEstimate:N0}" : "—"),
                    crewPay.Count > 0 ? "Recorded on trip report" : "Estimate from standard rates")
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
            AddTabButton("Stores", $"Stores  {Math.Max(stops.Count, 0)}");
            AddTabButton("Documents", $"Documents  {(schedule != null ? 1 : 0) + (trip != null ? 1 : 0) + (invoice != null ? 1 : 0) + incidents.Count}");
            AddTabButton("Activity", $"Activity  {activity.Count}");
            AddTabButton("Notes", string.IsNullOrWhiteSpace(del.Remarks) ? "Notes" : "Notes  •");
            host.Controls.Add(flowTabs);
            host.Controls.Add(line);
            return host;
        }
        private void AddTabButton(string key, string text)
        {
            var b = new Button
            {
                Text = text,
                Tag = key,
                AutoSize = true,
                Height = D(38),
                Padding = new Padding(D(12), 0, D(12), 0),
                Margin = new Padding(0, 0, D(4), 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Canvas,
                Font = new Font("Segoe UI Semibold", 10F),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
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
            foreach (Control c in flowTabs.Controls)
            {
                bool sel = (c.Tag as string) == key;
                c.ForeColor = sel ? AppTheme.TextPrimary : AppTheme.TextMuted;
                c.Invalidate();
            }
            pnlBody.SuspendLayout();
            foreach (Control c in pnlBody.Controls.Cast<Control>().ToList()) { pnlBody.Controls.Remove(c); c.Dispose(); }
            txtNotes = null;
            Control content = key switch
            {
                "Stores" => BuildStoresTab(),
                "Documents" => BuildDocumentsTab(),
                "Activity" => BuildActivityTab(),
                "Notes" => BuildNotesTab(),
                _ => BuildOverviewTab()
            };
            content.Dock = DockStyle.Fill;
            pnlBody.Controls.Add(content);
            pnlBody.ResumeLayout(true);
        }
        // ------------------------------------------------------------------ Tabs
        private int RowH => prefs.Compact ? D(26) : D(32);
        private Control BuildOverviewTab()
        {
            var scroll = new Panel { AutoScroll = true, BackColor = AppTheme.Canvas };
            var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            var left = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0, 0, D(8), 0) };
            var right = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(D(8), 0, 0, 0) };
            // Order & client
            var order = new List<(string, string, bool)>
            {
                ("Client", clientName, true),
                ("Contact person", string.IsNullOrWhiteSpace(client?.ContactPerson) ? "—" : client!.ContactPerson, true),
                ("Contact number", string.IsNullOrWhiteSpace(client?.ContactNumber) ? "—" : client!.ContactNumber, true),
                ("Route / destination", routeText, true),
                ("Order type", stops.Count > 1 || del.IsMultiDrop ? $"Multi-drop · {Math.Max(stops.Count, del.TotalStopsCount)} stores" : "Single drop", false),
                ("Cargo", string.IsNullOrWhiteSpace(del.CargoType) ? "—" : del.CargoType, false),
                ("Peak Season", del.IsPeak ? "Yes — peak season rate" : "No — regular rate", false),
                ("Loading schedule", schedule != null ? $"{schedule.ScheduleNumber} ({schedule.Status})" : (del.LoadingScheduleId > 0 ? $"#{del.LoadingScheduleId}" : "—"), true)
            };
            if (!string.IsNullOrWhiteSpace(client?.Address)) order.Insert(3, ("Client address", client!.Address, true));
            // Truck & crew
            var crewRows = new List<(string, string, bool)>
            {
                ("Truck", string.IsNullOrWhiteSpace(del.TruckInfo) ? "—" : del.TruckInfo, true),
                ("Capacity", truck != null ? $"{truck.CapacityCrates:N0} crates · {truck.CargoType}" : "—", false)
            };
            var driver = FindCrew(del.DriverId);
            crewRows.Add(("Driver", PersonLine(del.DriverName, driver), true));
            if (driver != null) crewRows.Add(("License", driver.LicenseDisplay, true));
            var aideIds = MJdatabase.GetDeliveryCrewIds(del).Where(x => x.Role == "Aide").Select(x => x.CrewId).ToList();
            if (aideIds.Count == 0)
            {
                crewRows.Add(("Aides", del.AideDisplay, true));
            }
            else
            {
                int n = 1;
                foreach (var id in aideIds)
                {
                    var c = FindCrew(id);
                    crewRows.Add((aideIds.Count == 1 ? "Aide" : $"Aide {n++}", PersonLine(c?.FullName ?? $"Crew #{id}", c), true));
                }
            }
            // Schedule
            int days = (del.DispatchDate.Date - DateTime.Today).Days;
            string timing = days == 0 ? "Today" : days > 0 ? $"In {days} day{(days == 1 ? "" : "s")}" : $"{-days} day{(days == -1 ? "" : "s")} ago";
            var sched = new List<(string, string, bool)>
            {
                ("Dispatch date", del.DispatchDate.ToString("dddd, MMMM d, yyyy"), false),
                ("Departure time", string.IsNullOrWhiteSpace(del.DepartureTime) ? "—" : del.DepartureTime, false),
                ("Timing", timing, false),
                ("Status", StatusLabel(), false)
            };
            if (schedule != null && schedule.RescheduleCount > 0)
            {
                sched.Add(("Rescheduled", $"{schedule.RescheduleCount} time(s)" + (schedule.OriginalScheduleDate.HasValue ? $" · originally {schedule.OriginalScheduleDate.Value:MMM d, yyyy}" : ""), false));
                if (!string.IsNullOrWhiteSpace(schedule.RescheduleReason)) sched.Add(("Reschedule reason", schedule.RescheduleReason, false));
            }
            if (IsCancelled && !string.IsNullOrWhiteSpace(del.CancellationReason)) sched.Add(("Cancellation reason", del.CancellationReason, false));
            // Remarks
            var notes = new List<(string, string, bool)>
            {
                ("Remarks", string.IsNullOrWhiteSpace(del.Remarks) ? "No remarks yet. Add some in the Notes tab." : del.Remarks.Replace("\r", " ").Replace("\n", " "), false)
            };
            AddCards(left, InfoCard("Order & client", order), InfoCard("Schedule", sched));
            AddCards(right, InfoCard("Truck & crew", crewRows), InfoCard("Remarks", notes));
            grid.Controls.Add(left, 0, 0);
            grid.Controls.Add(right, 1, 0);
            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = D(30),
                Text = "Tip: click any underlined value to copy it.",
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.BottomLeft
            };
            scroll.Controls.Add(hint);
            scroll.Controls.Add(grid);
            return scroll;
        }
        private static string PersonLine(string name, Crew? c)
        {
            if (string.IsNullOrWhiteSpace(name)) return "—";
            return c != null && !string.IsNullOrWhiteSpace(c.ContactNumber) ? $"{name}  ·  {c.ContactNumber}" : name;
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
            int pad = D(16);
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(pad, D(12), pad, D(12)) };
            card.Height = D(12) + D(32) + rows.Count * RowH + D(14);
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var lblTitle = new Label { Dock = DockStyle.Top, Height = D(32), Text = title, Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.TopLeft };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Top, Height = rows.Count * RowH, ColumnCount = 2, RowCount = rows.Count, BackColor = AppTheme.Surface };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(150)));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < rows.Count; i++)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, RowH));
                var (label, value, copy) = rows[i];
                var l = new Label { Text = label, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) };
                var v = new Label { Text = value, Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI Semibold", 9.75F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0), UseMnemonic = false };
                bool canCopy = copy && value != "—" && !string.IsNullOrWhiteSpace(value);
                if (canCopy)
                {
                    v.Cursor = Cursors.Hand;
                    v.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Underline);
                    v.Click += (s, e) => CopyText(value, $"{label} copied");
                    tips.SetToolTip(v, value + "\n(click to copy)");
                }
                else
                {
                    tips.SetToolTip(v, value);
                }
                tbl.Controls.Add(l, 0, i);
                tbl.Controls.Add(v, 1, i);
            }
            card.Controls.Add(tbl);
            card.Controls.Add(lblTitle);
            return card;
        }
        private DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5F),
                MultiSelect = true
            };
            UIThemeHelper.ApplyGridTheme(g);
            g.RowTemplate.Height = prefs.Compact ? D(28) : D(34);
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = D(36);
            g.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            return g;
        }
        private Control BuildStoresTab()
        {
            var host = new Panel { BackColor = AppTheme.Canvas };
            if (stops.Count == 0)
            {
                host.Controls.Add(EmptyState("No store breakdown saved for this order.", "Edit the order to add its stores and crates."));
                return host;
            }
            var g = MakeGrid();
            g.Dock = DockStyle.Fill;
            g.Columns.Add("c1", "#");
            g.Columns.Add("c2", "Code");
            g.Columns.Add("c3", "Store");
            g.Columns.Add("c4", "G-crates");
            g.Columns.Add("c5", "E-crates");
            g.Columns.Add("c6", "Total");
            g.Columns.Add("c7", "DR / Doc #");
            g.Columns.Add("c8", "Delivered");
            g.Columns.Add("c9", "Returned");
            g.Columns.Add("c10", "Status");
            int[] weights = { 34, 70, 200, 70, 70, 64, 110, 76, 76, 84 };
            for (int i = 0; i < weights.Length; i++) g.Columns[i].FillWeight = weights[i];
            foreach (var i in new[] { 0, 3, 4, 5, 7, 8 }) g.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            g.Columns[5].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            foreach (var s in stops.OrderBy(x => x.StopSequence))
            {
                int total = s.TotalCrates > 0 ? s.TotalCrates : s.GCrate + s.ECrate;
                g.Rows.Add(s.StopSequence, s.StoreCode, s.Destination, s.GCrate, s.ECrate, total, s.DrNumbers,
                    s.DeliveredCrates > 0 ? s.DeliveredCrates.ToString("N0") : "—",
                    s.ReturnedCrates > 0 ? s.ReturnedCrates.ToString("N0") : "—",
                    string.IsNullOrWhiteSpace(s.Status) ? "Pending" : s.Status);
            }
            var foot = new Label
            {
                Dock = DockStyle.Bottom,
                Height = D(40),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = AppTheme.TextSecondary,
                Text = $"{stops.Count} stores   ·   G {stops.Sum(x => x.GCrate):N0}   ·   E {stops.Sum(x => x.ECrate):N0}   ·   Total {stops.Sum(x => x.TotalCrates > 0 ? x.TotalCrates : x.GCrate + x.ECrate):N0} crates" +
                       (stops.Any(x => x.DeliveredCrates > 0) ? $"   ·   Delivered {stops.Sum(x => x.DeliveredCrates):N0}   ·   Returned {stops.Sum(x => x.ReturnedCrates):N0}" : "") +
                       "        (select rows and press Ctrl+C to copy)"
            };
            host.Controls.Add(g);
            host.Controls.Add(foot);
            return host;
        }
        private Control BuildDocumentsTab()
        {
            var scroll = new Panel { AutoScroll = true, BackColor = AppTheme.Canvas };
            var items = new List<Control>();
            // Loading schedule
            if (schedule != null)
            {
                items.Add(DocRow("📅", $"Loading schedule  {schedule.ScheduleNumber}", $"{schedule.ScheduleDate:MMM d, yyyy} · {schedule.TotalCrates:N0} crates · {schedule.Status}",
                    ("View", "secondary", () => RecordDetailsDialog.ShowDetails(this, schedule))));
            }
            // Trip report
            if (trip != null)
            {
                items.Add(DocRow("📝", $"Trip report  {trip.TripReportNumber}", $"Filed {trip.ReportDate:MMM d, yyyy} · delivered {trip.DeliveredCrates:N0}, returned {trip.ReturnedCrates:N0} · signed by {trip.StoreManagerName}",
                    ("Print", "secondary", () => PrintHelper.PrintOfficialTripReport(del.Id, del.DispatchNumber)),
                    ("Open", "primary", OpenTripReport)));
            }
            else
            {
                items.Add(DocRow("📝", "Trip report", IsCompleted ? "Not filed yet. File it to record store sign-offs, returned crates and crew pay." : "Filed after the trip is delivered.",
                    IsCompleted ? ("File trip report", "primary", OpenTripReport) : default));
            }
            // Invoice
            if (invoice != null)
            {
                items.Add(DocRow("💰", $"Invoice  {invoice.InvoiceNumber}", $"PHP {invoice.NetAmount:N2} · {invoice.Status} · issued {invoice.IssueDate:MMM d}, due {invoice.DueDate:MMM d, yyyy}",
                    ("Open billing", "primary", () => { MainForm.Instance?.NavigateToFinance("Billing"); Close(); })));
            }
            else
            {
                items.Add(DocRow("💰", "Invoice", IsCompleted ? "Not billed yet." : "Created once the order is delivered.",
                    IsCompleted ? ("Create invoice", trip != null ? "primary" : "secondary", CreateInvoice) : default));
            }
            // Incidents
            if (incidents.Count == 0)
            {
                items.Add(DocRow("⚠️", "Incidents & damage", "None reported for this order.",
                    IsCancelled ? default : ("Log incident", "secondary", LogIncident)));
            }
            else
            {
                foreach (var inc in incidents)
                {
                    items.Add(DocRow("⚠️", $"Incident · {inc.Cause}", $"{inc.IncidentDate:MMM d, yyyy} · {inc.DamagedQuantity:N0} crates · PHP {inc.EstimatedCost:N2} · {inc.Status}" + (inc.ChargeableToCrew ? " · charged to crew" : ""),
                        ("Open", "secondary", OpenIncidents)));
                }
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                items[i].Dock = DockStyle.Top;
                scroll.Controls.Add(items[i]);
                if (i > 0) scroll.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(10), BackColor = AppTheme.Canvas });
            }
            return scroll;
        }
        private Control DocRow(string icon, string title, string sub, params (string Text, string Role, Action Act)[] actions)
        {
            var row = new Panel { Height = prefs.Compact ? D(62) : D(72), BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(10), D(14), D(10)) };
            row.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1); };
            row.Resize += (s, e) => row.Invalidate();
            var lblIcon = new Label { Dock = DockStyle.Left, Width = D(40), Text = icon, Font = new Font("Segoe UI Emoji", 16F), TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppTheme.TextSecondary };
            var text = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            var t1 = new Label { Dock = DockStyle.Top, Height = D(26), Text = title, Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, AutoEllipsis = true, UseMnemonic = false };
            var t2 = new Label { Dock = DockStyle.Fill, Text = sub, Font = new Font("Segoe UI", 9.25F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true, UseMnemonic = false };
            tips.SetToolTip(t2, sub);
            text.Controls.Add(t2);
            text.Controls.Add(t1);
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.Surface, Padding = new Padding(0, D(4), 0, 0) };
            foreach (var a in actions.Reverse())
            {
                if (a.Act == null) continue;
                var b = ActionButton(a.Text, a.Role, a.Act);
                b.Margin = new Padding(D(8), 0, 0, 0);
                flow.Controls.Add(b);
            }
            row.Controls.Add(text);
            row.Controls.Add(flow);
            row.Controls.Add(lblIcon);
            return row;
        }
        private Control BuildActivityTab()
        {
            var host = new Panel { BackColor = AppTheme.Canvas };
            if (activity.Count == 0)
            {
                host.Controls.Add(EmptyState("No recorded activity for this order yet.", "Changes, reschedules, completions and payments will show up here."));
                return host;
            }
            var g = MakeGrid();
            g.Dock = DockStyle.Fill;
            g.Columns.Add("a1", "When");
            g.Columns.Add("a2", "Who");
            g.Columns.Add("a3", "What");
            g.Columns.Add("a4", "Details");
            g.Columns[0].FillWeight = 26; g.Columns[1].FillWeight = 18; g.Columns[2].FillWeight = 24; g.Columns[3].FillWeight = 80;
            foreach (var a in activity)
            {
                string what = $"{Friendly(a.ActionType)} · {FriendlyTable(a.TableName)}";
                var detail = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(a.FieldName) && (!string.IsNullOrWhiteSpace(a.OldValue) || !string.IsNullOrWhiteSpace(a.NewValue)))
                    detail.Append($"{a.FieldName}: {(string.IsNullOrWhiteSpace(a.OldValue) ? "" : a.OldValue + " → ")}{a.NewValue}");
                if (!string.IsNullOrWhiteSpace(a.ReasonOrNotes)) detail.Append((detail.Length > 0 ? "  ·  " : "") + a.ReasonOrNotes);
                int r = g.Rows.Add(a.Timestamp.ToString("MMM d, yyyy  h:mm tt"), string.IsNullOrWhiteSpace(a.Username) ? "System" : a.Username, what, detail.ToString());
                g.Rows[r].Cells[3].ToolTipText = detail.ToString();
            }
            host.Controls.Add(g);
            return host;
        }
        private static string Friendly(string action) => (action ?? "").ToUpperInvariant() switch
        {
            "INSERT" => "Created",
            "UPDATE" => "Updated",
            "DELETE" => "Deleted",
            "ORDER_COMPLETED" => "Completed",
            "RESCHEDULE" => "Rescheduled",
            "CANCEL" => "Cancelled",
            _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((action ?? "").Replace('_', ' ').ToLowerInvariant())
        };
        private static string FriendlyTable(string table) => table switch
        {
            "Deliveries" => "Order",
            "TripReports" => "Trip report",
            "Invoices" => "Invoice",
            "IncidentReports" => "Incident",
            "LoadingSchedules" => "Loading schedule",
            "TripCrewPay" => "Crew pay",
            "CollectionReceipts" => "Payment",
            _ => table
        };
        private Control BuildNotesTab()
        {
            var host = new Panel { BackColor = AppTheme.Canvas };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(12), D(16), D(12)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var title = new Label { Dock = DockStyle.Top, Height = D(30), Text = "Order notes", Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary };
            var sub = new Label { Dock = DockStyle.Top, Height = D(26), Text = "Anything the team should know about this order: instructions, gate passes, problems on the road.", Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true };
            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                Font = new Font("Segoe UI", 10.5F),
                Text = del.Remarks ?? "",
                BackColor = AppTheme.InputBack,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            var bar = new Panel { Dock = DockStyle.Bottom, Height = D(54), BackColor = AppTheme.Surface, Padding = new Padding(0, D(12), 0, 0) };
            var lblState = new Label { Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft, Text = "" };
            var btnSave = MakeButton("💾  Save notes", "primary", D(150));
            btnSave.Dock = DockStyle.Right;
            btnSave.Enabled = false;
            var btnStamp = MakeButton("🕒  Add timestamp", "secondary", D(160));
            btnStamp.Dock = DockStyle.Right;
            var gap = new Panel { Dock = DockStyle.Right, Width = D(8), BackColor = AppTheme.Surface };
            txtNotes.TextChanged += (s, e) => { btnSave.Enabled = (txtNotes.Text ?? "") != (del.Remarks ?? ""); lblState.Text = btnSave.Enabled ? "Unsaved changes" : ""; };
            btnStamp.Click += (s, e) =>
            {
                if (txtNotes == null) return;
                string stamp = $"[{DateTime.Now:MMM d, h:mm tt} · {SessionContext.CurrentUsername}] ";
                txtNotes.AppendText((txtNotes.TextLength > 0 && !txtNotes.Text.EndsWith("\n") ? Environment.NewLine : "") + stamp);
                txtNotes.Focus();
            };
            btnSave.Click += (s, e) =>
            {
                if (txtNotes == null) return;
                try
                {
                    MJdatabase.UpdateDeliveryRemarks(del.Id, txtNotes.Text.Trim());
                    del.Remarks = txtNotes.Text.Trim();
                    btnSave.Enabled = false;
                    lblState.Text = $"Saved {DateTime.Now:h:mm tt}";
                    RefreshParent();
                    ToastHelper.ShowToast(this, "Notes saved", ToastType.Success);
                }
                catch (Exception ex)
                { ErrLog.Error("DispatchDetailsDialog", ex); 
                    MessageBox.Show(this, "Couldn't save the notes: " + ex.Message, "Notes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            bar.Controls.Add(lblState);
            bar.Controls.Add(btnStamp);
            bar.Controls.Add(gap);
            bar.Controls.Add(btnSave);
            card.Controls.Add(txtNotes);
            card.Controls.Add(bar);
            card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(6), BackColor = AppTheme.Surface });
            card.Controls.Add(sub);
            card.Controls.Add(title);
            host.Controls.Add(card);
            return host;
        }
        private Control EmptyState(string title, string sub)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                var r = new Rectangle(0, p.Height / 2 - D(30), p.Width, D(28));
                using var f1 = new Font("Segoe UI Semibold", 11F);
                using var f2 = new Font("Segoe UI", 9.5F);
                TextRenderer.DrawText(e.Graphics, title, f1, r, AppTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                r.Offset(0, D(28));
                TextRenderer.DrawText(e.Graphics, sub, f2, r, AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            p.Resize += (s, e) => p.Invalidate();
            return p;
        }
        // ------------------------------------------------------------------ Footer & actions
        private Panel BuildFooter()
        {
            var p = new Panel { Dock = DockStyle.Bottom, Height = D(68), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(14), D(24), D(14)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); };
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var btnEdit = MakeButton("✎  Edit order", "strong", D(132));
            btnEdit.Click += (s, e) => EditOrder();
            tips.SetToolTip(btnEdit, "Open this order in the dispatch form (Ctrl+E)");
            left.Controls.Add(btnEdit);
            if (IsActiveOrder)
            {
                var btnRes = MakeButton("📅  Reschedule", "secondary", D(136));
                btnRes.Click += (s, e) => RescheduleOrder();
                left.Controls.Add(btnRes);
                if (Views.DispatchManagementView.IsOnTheRoad(del))
                {
                    var btnComplete = MakeButton("✔  Complete", "primary", D(124));
                    btnComplete.Click += (s, e) => CompleteOrder();
                    tips.SetToolTip(btnComplete, "The truck arrived (even if early): mark the order delivered and free the truck and crew");
                    left.Controls.Add(btnComplete);
                }
                else
                {
                    var btnStart = MakeButton("🚚  Start trip now", "secondary", D(150));
                    btnStart.Enabled = del.DispatchDate.Date <= DateTime.Today;
                    btnStart.Click += (s, e) => StartTrip();
                    tips.SetToolTip(btnStart, $"Goes in transit automatically at {MJdatabase.DepartureDateTime(del):MMM d, h:mm tt}. Use this if the truck left early.");
                    left.Controls.Add(btnStart);
                }
            }
            else if (IsCompleted)
            {
                var btnTrip = MakeButton(trip == null ? "📝  File trip report" : "📝  Trip report", "secondary", D(160));
                btnTrip.Click += (s, e) => OpenTripReport();
                left.Controls.Add(btnTrip);
            }
            foreach (Control c in left.Controls) c.Margin = new Padding(0, 0, D(8), 0);
            var btnClose = MakeButton("Close", "secondary", D(96));
            btnClose.Click += (s, e) => Close();
            var btnMore = MakeButton("More  ▾", "secondary", D(104));
            btnMore.Click += (s, e) => BuildMoreMenu().Show(btnMore, new Point(0, btnMore.Height));
            var btnCopy = MakeButton("⧉  Copy summary", "secondary", D(150));
            btnCopy.Click += (s, e) => CopySummary();
            tips.SetToolTip(btnCopy, "Copy the order details as text, ready to paste in Viber, SMS or email (Ctrl+Shift+C)");
            var btnPrint = MakeButton("🖨  Print  ▾", "secondary", D(110));
            btnPrint.Click += (s, e) => BuildPrintMenu().Show(btnPrint, new Point(0, btnPrint.Height));
            foreach (var b in new[] { btnClose, btnMore, btnCopy, btnPrint })
            {
                b.Margin = new Padding(D(8), 0, 0, 0);
                right.Controls.Add(b);
            }
            p.Controls.Add(left);
            p.Controls.Add(right);
            return p;
        }
        private ContextMenuStrip BuildPrintMenu()
        {
            var m = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F) };
            m.Items.Add("Dispatch / loading sheet", null, (s, e) => PrintDispatchSheet());
            m.Items.Add("Official trip report", null, (s, e) => PrintHelper.PrintOfficialTripReport(del.Id, del.DispatchNumber));
            return m;
        }
        private ContextMenuStrip BuildMoreMenu()
        {
            var m = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F) };
            m.Items.Add("Copy dispatch number", null, (s, e) => CopyText(del.DispatchNumber, "Dispatch number copied"));
            m.Items.Add("Copy store list", null, (s, e) => CopyText(StoreListText(), "Store list copied"));
            m.Items.Add(new ToolStripSeparator());
            if (!IsCancelled)
            {
                m.Items.Add(trip == null ? "File trip report" : "Open trip report", null, (s, e) => OpenTripReport());
                m.Items.Add("Log incident / damage", null, (s, e) => LogIncident());
            }
            if (IsCompleted && invoice == null) m.Items.Add("Create invoice", null, (s, e) => CreateInvoice());
            m.Items.Add("Show all trip reports for this order", null, (s, e) => { MainForm.Instance?.ShowTripReportsForDispatch(del.DispatchNumber); Close(); });
            if (IsActiveOrder)
            {
                m.Items.Add(new ToolStripSeparator());
                var cancel = new ToolStripMenuItem("Cancel order…", null, (s, e) => CancelOrder()) { ForeColor = AppTheme.DangerText };
                m.Items.Add(cancel);
            }
            return m;
        }
        private ContextMenuStrip BuildCustomizeMenu()
        {
            var m = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F) };
            ToolStripMenuItem Toggle(string text, bool value, Action<bool> set)
            {
                var it = new ToolStripMenuItem(text) { Checked = value, CheckOnClick = true };
                it.Click += (s, e) => { set(it.Checked); prefs.Save(); Reload(del); };
                return it;
            }
            m.Items.Add(new ToolStripLabel("Show on this window") { ForeColor = AppTheme.TextMuted });
            m.Items.Add(Toggle("Progress tracker", prefs.ShowTracker, v => prefs.ShowTracker = v));
            m.Items.Add(Toggle("Next-step banner", prefs.ShowNextStep, v => prefs.ShowNextStep = v));
            m.Items.Add(Toggle("Summary tiles", prefs.ShowTiles, v => prefs.ShowTiles = v));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Toggle("Compact rows", prefs.Compact, v => prefs.Compact = v));
            m.Items.Add(Toggle("Remember window size", prefs.RememberSize, v => prefs.RememberSize = v));
            var open = new ToolStripMenuItem("Open on tab");
            foreach (var t in new[] { "Overview", "Stores", "Documents", "Activity", "Notes" })
            {
                var it = new ToolStripMenuItem(t) { Checked = prefs.DefaultTab == t };
                it.Click += (s, e) => { prefs.DefaultTab = t; prefs.Save(); ToastHelper.ShowToast(this, $"Orders will open on the {t} tab", ToastType.Info); };
                open.DropDownItems.Add(it);
            }
            m.Items.Add(open);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Reset to default layout", null, (s, e) => { prefs = new Prefs(); prefs.Save(); Reload(del); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(new ToolStripLabel("Shortcuts: Alt+←/→ previous/next · Ctrl+1–5 tabs · Ctrl+E edit · Ctrl+P print") { ForeColor = AppTheme.TextMuted });
            return m;
        }
        private void Navigate(int step)
        {
            int idx = siblings.FindIndex(x => x.Id == del.Id);
            if (idx < 0) return;
            int next = idx + step;
            if (next < 0 || next >= siblings.Count) return;
            Reload(siblings[next]);
        }
        private void EditOrder()
        {
            var d = del;
            Close();
            if (parentView is DispatchManagementView dv) dv.EditDeliveryOrder(d);
            else MainForm.Instance?.EditDeliveryOrderInDispatch(d);
        }
        private void RescheduleOrder()
        {
            if (!IsActiveOrder)
            {
                MessageBox.Show(this, $"Dispatch {del.DispatchNumber} is {del.Status.ToLowerInvariant()} and can't be rescheduled.", "Reschedule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new RescheduleDeliveryDialog(del);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshParent();
                Reload(del);
                ToastHelper.ShowToast(this, $"Rescheduled to {dlg.RescheduledDate:MMM d}", ToastType.Success);
            }
        }
        private void StartTrip()
        {
            if (MessageBox.Show(this, $"Start dispatch {del.DispatchNumber} now? It is set to leave at {MJdatabase.DepartureDateTime(del):MMM d, h:mm tt}.", "Start trip now", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var (ok, message) = MJdatabase.StartTripNow(del.Id, SessionContext.CurrentUsername);
            if (!ok) { MessageBox.Show(this, message, "Start trip now", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            RefreshParent();
            Reload(del);
            ToastHelper.ShowToast(this, message, ToastType.Success);
        }
        private void CompleteOrder()
        {
            if (!IsActiveOrder) return;
            var confirm = MessageBox.Show(this,
                $"Mark dispatch {del.DispatchNumber} as completed?\n\nThis frees the truck, driver and aides and closes the linked loading schedule. You can still file the trip report afterwards.",
                "Complete order", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            var (ok, message) = MJdatabase.CompleteDelivery(del.Id, SessionContext.CurrentUsername);
            if (!ok)
            {
                MessageBox.Show(this, message, "Complete order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RefreshParent();
            Reload(del);
            ToastHelper.ShowToast(this, message, ToastType.Success);
        }
        private void CancelOrder()
        {
            if (!IsActiveOrder) return;
            string? reason = CancellationReasonDialog.PromptCancellationReason(this, "Dispatch Order", del.DispatchNumber);
            if (string.IsNullOrEmpty(reason)) return;
            try
            {
                del.Status = "Cancelled";
                del.CancellationReason = reason;
                MJdatabase.SaveDelivery(del);
                RefreshParent();
                Reload(del);
                ToastHelper.ShowToast(this, $"Dispatch {del.DispatchNumber} cancelled", ToastType.Warning);
            }
            catch (Exception ex)
            { ErrLog.Error("DispatchDetailsDialog", ex); 
                MessageBox.Show(this, "Couldn't cancel the order: " + ex.Message, "Cancel order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void OpenTripReport()
        {
            var d = del;
            Close();
            MainForm.Instance?.OpenTripReportForDispatch(d);
        }
        private void CreateInvoice()
        {
            var d = del;
            Close();
            MainForm.Instance?.CreateInvoiceForDelivery(d);
        }
        private void LogIncident()
        {
            var d = del;
            Close();
            MainForm.Instance?.LogIncidentForDispatch(d);
        }
        private void OpenIncidents()
        {
            var num = del.DispatchNumber;
            Close();
            MainForm.Instance?.ShowIncidentsForDispatch(num);
        }
        private void PrintDispatchSheet()
        {
            try { PrintHelper.PrintOfficialLoadingScheduleFromDeliveries(new List<Delivery> { del }); }
            catch (Exception ex) { ErrLog.Error("DispatchDetailsDialog", ex);  MessageBox.Show(this, "Print failed: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void RefreshParent()
        {
            try { if (parentView is IRefreshable r && !parentView.IsDisposed) r.RefreshData(); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            try { MainForm.Instance?.UpdateHeaderKpiBadges(); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
        }
        private string StoreListText()
        {
            var sb = new StringBuilder();
            foreach (var s in stops.OrderBy(x => x.StopSequence))
            {
                string code = string.IsNullOrWhiteSpace(s.StoreCode) ? "" : s.StoreCode + " - ";
                sb.AppendLine($"{s.StopSequence}. {code}{s.Destination}  (G {s.GCrate}, E {s.ECrate})" + (string.IsNullOrWhiteSpace(s.DrNumbers) ? "" : $"  DR {s.DrNumbers}"));
            }
            return sb.ToString().TrimEnd();
        }
        private void CopySummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"DISPATCH {del.DispatchNumber}  [{StatusLabel()}]");
            sb.AppendLine($"Client: {clientName}");
            sb.AppendLine($"Route: {routeText}");
            sb.AppendLine($"Date: {del.DispatchDate:ddd, MMM d, yyyy} · {del.DepartureTime}");
            sb.AppendLine($"Truck: {del.TruckInfo}");
            sb.AppendLine($"Driver: {PersonLine(del.DriverName, FindCrew(del.DriverId))}");
            var aides = MJdatabase.GetDeliveryCrewIds(del).Where(x => x.Role == "Aide").Select(x => PersonLine(FindCrew(x.CrewId)?.FullName ?? "", FindCrew(x.CrewId))).Where(x => x != "—").ToList();
            sb.AppendLine($"Aides: {(aides.Count > 0 ? string.Join("; ", aides) : del.AideDisplay)}");
            sb.AppendLine($"Crates: {del.TotalCrates:N0} (G {del.GCrate:N0} · E {del.ECrate:N0})");
            if (stops.Count > 0)
            {
                sb.AppendLine($"Stores ({stops.Count}):");
                sb.AppendLine(StoreListText());
            }
            if (!string.IsNullOrWhiteSpace(del.Remarks)) sb.AppendLine($"Notes: {del.Remarks}");
            CopyText(sb.ToString().TrimEnd(), "Order summary copied, ready to paste");
        }
        private void CopyText(string text, string toast)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                Clipboard.SetText(text);
                ToastHelper.ShowToast(this, toast, ToastType.Success);
            }
            catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
        }
        private static string Shorten(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? "") : s.Substring(0, max - 1) + "…";
        private Button MakeButton(string text, string role, int width)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = D(38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Margin = new Padding(0)
            };
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }
        private Button ActionButton(string text, string role, Action act)
        {
            var b = MakeButton(text, role, 10);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(D(100), D(36));
            b.Padding = new Padding(D(10), 0, D(10), 0);
            b.Click += (s, e) => act();
            return b;
        }
        // ------------------------------------------------------------------ Preferences
        private sealed class Prefs
        {
            public bool ShowTracker = true, ShowNextStep = true, ShowTiles = true, Compact = false, RememberSize = true, Maximized = false;
            public string DefaultTab = "Overview";
            public int Width, Height;
            private static string Key => $"DispatchDetails.Prefs.{SessionContext.CurrentUsername}";
            public static Prefs Load()
            {
                var p = new Prefs();
                try
                {
                    string raw = MJdatabase.GetSystemSetting(Key, "");
                    foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = part.Split('=', 2);
                        if (kv.Length != 2) continue;
                        string k = kv[0].Trim(), v = kv[1].Trim();
                        bool b = v == "1";
                        switch (k)
                        {
                            case "tracker": p.ShowTracker = b; break;
                            case "next": p.ShowNextStep = b; break;
                            case "tiles": p.ShowTiles = b; break;
                            case "compact": p.Compact = b; break;
                            case "remember": p.RememberSize = b; break;
                            case "max": p.Maximized = b; break;
                            case "tab": if (v is "Overview" or "Stores" or "Documents" or "Activity" or "Notes") p.DefaultTab = v; break;
                            case "w": int.TryParse(v, out p.Width); break;
                            case "h": int.TryParse(v, out p.Height); break;
                        }
                    }
                }
                catch (System.Exception swx5) { ErrLog.Swallowed(swx5); }
                return p;
            }
            public void Save()
            {
                try
                {
                    string B(bool x) => x ? "1" : "0";
                    MJdatabase.SetSystemSetting(Key, $"tracker={B(ShowTracker)};next={B(ShowNextStep)};tiles={B(ShowTiles)};compact={B(Compact)};remember={B(RememberSize)};max={B(Maximized)};tab={DefaultTab};w={Width};h={Height}", "Dispatch details window layout");
                }
                catch (System.Exception swx6) { ErrLog.Swallowed(swx6); }
            }
        }
        // ------------------------------------------------------------------ Painted pieces
        private sealed class Pill : Control
        {
            private readonly Color back, fore;
            public Pill(string text, Color back, Color fore)
            {
                Text = text;
                this.back = back;
                this.fore = fore;
                Font = new Font("Segoe UI Semibold", 9F);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                var sz = TextRenderer.MeasureText(text, Font);
                Size = new Size(sz.Width + 24 * DeviceDpi / 96, sz.Height + 8 * DeviceDpi / 96);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Gfx.HighQuality(e.Graphics);
                using var path = Gfx.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
                using var b = new SolidBrush(back);
                e.Graphics.FillPath(b, path);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }
        private sealed class Tile : Control
        {
            private readonly string caption, value, sub;
            private readonly Color? valueColor;
            public Tile(string caption, string value, string sub, Color? valueColor = null)
            {
                this.caption = caption; this.value = value; this.sub = sub; this.valueColor = valueColor;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                BackColor = AppTheme.Canvas;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                float s = DeviceDpi / 96f;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = Gfx.RoundedRect(r, (int)(8 * s)))
                using (var b = new SolidBrush(AppTheme.Surface))
                using (var pen = new Pen(AppTheme.Border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(pen, path);
                }
                int x = (int)(14 * s);
                using var f1 = new Font("Segoe UI", 8.75F);
                using var f2 = new Font("Segoe UI Semibold", 15F);
                using var f3 = new Font("Segoe UI", 8.5F);
                var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(g, caption, f1, new Rectangle(x, (int)(10 * s), Width - 2 * x, (int)(18 * s)), AppTheme.TextMuted, flags);
                TextRenderer.DrawText(g, value, f2, new Rectangle(x, (int)(28 * s), Width - 2 * x, (int)(30 * s)), valueColor ?? AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, sub, f3, new Rectangle(x, (int)(60 * s), Width - 2 * x, (int)(18 * s)), AppTheme.TextMuted, flags);
            }
        }
        private sealed class Tracker : Control
        {
            public sealed record Step(string Title, string Sub, bool Done);
            private readonly List<Step> steps;
            private readonly bool cancelled;
            public Tracker(List<Step> steps, bool cancelled)
            {
                this.steps = steps;
                this.cancelled = cancelled;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                BackColor = AppTheme.Surface;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                float s = DeviceDpi / 96f;
                int n = steps.Count;
                if (n == 0) return;
                int d = (int)(24 * s);
                int cy = (int)(6 * s) + d / 2;
                float slot = Width / (float)n;
                int current = cancelled ? -1 : steps.FindIndex(x => !x.Done);
                using var fT = new Font("Segoe UI Semibold", 9F);
                using var fS = new Font("Segoe UI", 8.25F);
                using var fC = new Font("Segoe UI Semibold", 9F);
                for (int i = 0; i < n - 1; i++)
                {
                    float x1 = slot * i + slot / 2 + d / 2 + 4 * s;
                    float x2 = slot * (i + 1) + slot / 2 - d / 2 - 4 * s;
                    bool done = !cancelled && steps[i + 1].Done;
                    using var pen = new Pen(done ? AppTheme.Success : AppTheme.Border, 2.5f * s);
                    g.DrawLine(pen, x1, cy, x2, cy);
                }
                for (int i = 0; i < n; i++)
                {
                    var st = steps[i];
                    float cx = slot * i + slot / 2;
                    var circle = new RectangleF(cx - d / 2f, cy - d / 2f, d, d);
                    Color fill, ring, txt;
                    if (cancelled) { fill = AppTheme.SurfaceAlt; ring = AppTheme.Border; txt = AppTheme.TextMuted; }
                    else if (st.Done) { fill = AppTheme.Success; ring = AppTheme.Success; txt = AppTheme.TextPrimary; }
                    else if (i == current) { fill = AppTheme.Surface; ring = AppTheme.Accent; txt = AppTheme.TextPrimary; }
                    else { fill = AppTheme.Surface; ring = AppTheme.Border; txt = AppTheme.TextMuted; }
                    using (var b = new SolidBrush(fill)) g.FillEllipse(b, circle);
                    using (var pen = new Pen(ring, (i == current ? 2.5f : 1.5f) * s)) g.DrawEllipse(pen, circle);
                    if (!cancelled && st.Done)
                    {
                        using var pen = new Pen(Color.White, 2.2f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                        g.DrawLines(pen, new[] { new PointF(cx - 5 * s, cy), new PointF(cx - 1.5f * s, cy + 3.5f * s), new PointF(cx + 5.5f * s, cy - 4 * s) });
                    }
                    else
                    {
                        TextRenderer.DrawText(g, (i + 1).ToString(), fC, Rectangle.Round(circle), i == current ? AppTheme.Accent : AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    }
                    var tr = new Rectangle((int)(slot * i), (int)(cy + d / 2 + 6 * s), (int)slot, (int)(18 * s));
                    TextRenderer.DrawText(g, st.Title, fT, tr, txt, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    if (!string.IsNullOrEmpty(st.Sub))
                    {
                        tr.Offset(0, (int)(18 * s));
                        TextRenderer.DrawText(g, st.Sub, fS, tr, AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    }
                }
            }
        }
    }
}
