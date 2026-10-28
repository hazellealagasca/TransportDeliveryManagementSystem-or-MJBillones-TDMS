using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Trip report centre: summary tiles that point at what needs action (short deliveries, missing PODs, completed
    /// trips still without a report), a filter bar (period, client, driver, truck, problem type) with sorting, and extra
    /// columns joined from the dispatch (client, truck, driver, planned crates, variance, return rate, files).
    /// The chosen filters are remembered per computer.
    /// </summary>
    public partial class TripReportView
    {
        private const string ViewSettingKey = "TripReports.View";
        private TileStrip? trTiles;
        private StatTile? tReports, tDelivered, tReturnRate, tShort, tNoPod, tAwaiting;
        private FlowLayoutPanel? filterBar;
        private ComboBox cbPeriod = null!, cbClientF = null!, cbDriverF = null!, cbTruckF = null!, cbShowF = null!, cbSortF = null!;
        private DateTimePicker dtpFromF = null!, dtpToF = null!;
        private bool centerReady, applyingView;
        private Dictionary<string, Delivery> deliveryByNumber = new Dictionary<string, Delivery>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<int, (int Total, int Pods)> fileCounts = new Dictionary<int, (int, int)>();
        private Dictionary<int, int> incidentCounts = new Dictionary<int, int>();
        private List<Delivery> awaitingReport = new List<Delivery>();
        private int Dc(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private void InitReportCenter()
        {
            trTiles = new TileStrip();
            tReports = trTiles.Add(new StatTile("Trip reports", AppTheme.Accent, "in the selected period") { Clickable = true, Key = "all" });
            tDelivered = trTiles.Add(new StatTile("Crates delivered", AppTheme.Success, ""));
            tReturnRate = trTiles.Add(new StatTile("Return rate", AppTheme.Info, "returned ÷ delivered"));
            tShort = trTiles.Add(new StatTile("Short deliveries", AppTheme.Danger, "delivered less than planned") { Clickable = true, Key = "short" });
            tNoPod = trTiles.Add(new StatTile("Missing POD", AppTheme.Warning, "no proof-of-delivery file") { Clickable = true, Key = "nopod" });
            tAwaiting = trTiles.Add(new StatTile("Awaiting a report", AppTheme.Warning, "completed, no report yet") { Clickable = true, Key = "awaiting" });
            trTiles.FilterChanged += t =>
            {
                string? key = t?.Key as string;
                if (key == "awaiting") { trTiles.ClearSelection(); ShowAwaitingMenu(); return; }
                if (applyingView) return;
                applyingView = true;
                try { cbShowF.SelectedIndex = key == "short" ? 1 : key == "nopod" ? 4 : 0; }
                finally { applyingView = false; }
                FilterGridData();
            };
            filterBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = Dc(44), WrapContents = false, AutoScroll = false, BackColor = pnlGridCard.BackColor, Padding = new Padding(0, Dc(6), 0, 0) };
            ComboBox Combo(int w, params string[] items)
            {
                var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dc(w), Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, Dc(8), 0) };
                c.Items.AddRange(items);
                if (c.Items.Count > 0) c.SelectedIndex = 0;
                c.SelectedIndexChanged += (s, e) => { if (!applyingView) { UpdateCustomRange(); SaveView(); FilterGridData(); } };
                return c;
            }
            cbPeriod = Combo(130, "All time", "Today", "This week", "This month", "Last 30 days", "This year", "Custom range");
            dtpFromF = new DateTimePicker { Width = Dc(118), Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Margin = new Padding(0, 0, Dc(4), 0), Visible = false, Value = DateTime.Today.AddDays(-30) };
            dtpToF = new DateTimePicker { Width = Dc(118), Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Margin = new Padding(0, 0, Dc(8), 0), Visible = false };
            dtpFromF.ValueChanged += (s, e) => { if (!applyingView) { if (dtpFromF.Value.Date > dtpToF.Value.Date) dtpToF.Value = dtpFromF.Value; SaveView(); FilterGridData(); } };
            dtpToF.ValueChanged += (s, e) => { if (!applyingView) { if (dtpToF.Value.Date < dtpFromF.Value.Date) dtpFromF.Value = dtpToF.Value; SaveView(); FilterGridData(); } };
            cbClientF = Combo(170, "All clients");
            cbDriverF = Combo(150, "All drivers");
            cbTruckF = Combo(120, "All trucks");
            cbShowF = Combo(180, "All reports", "Short deliveries", "With returned crates", "With incidents", "Missing POD file", "On target (no issues)");
            cbSortF = Combo(170, "Newest first", "Oldest first", "Most crates returned", "Biggest shortfall", "Client A–Z", "Report number");
            cbSortF.SelectedIndexChanged += (s, e) => { if (!applyingView && currentSortColumn.Length > 0) { currentSortColumn = ""; FilterGridData(); } };
            var btnReset = new Button { Text = "Reset filters", AutoSize = true, MinimumSize = new Size(0, Dc(28)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, Dc(8), 0) };
            UIThemeHelper.SetButtonRole(btnReset, "secondary");
            btnReset.Name = "tripFilterReset";
            btnReset.Click += (s, e) => ResetView();
            filterBar.Controls.AddRange(new Control[] { cbPeriod, dtpFromF, dtpToF, cbClientF, cbDriverF, cbTruckF, cbShowF, cbSortF, btnReset });
            var tilesGap = new Panel { Dock = DockStyle.Top, Height = Dc(10), BackColor = pnlGridCard.BackColor };
            PageKit.InsertBelow(pnlGridHeaderBar, trTiles);
            PageKit.InsertBelow(trTiles, tilesGap);
            PageKit.InsertBelow(tilesGap, filterBar);
            if (pnlSummary != null) pnlSummary.Visible = false;
            // Extra columns joined from the dispatch, filled while painting.
            void Extra(string name, string header, float w, bool right = false)
            {
                var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = w, MinimumWidth = Dc(70), ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
                if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                gridTripReports.Columns.Insert(Math.Min(3, gridTripReports.Columns.Count), c);
            }
            Extra("xFiles", "Files", 50, true);
            Extra("xVariance", "Variance", 65, true);
            Extra("xPlanned", "Planned", 60, true);
            Extra("xDriver", "Driver", 110);
            Extra("xTruck", "Truck", 80);
            Extra("xClient", "Client", 130);
            gridTripReports.CellFormatting += GridTripReports_CenterFormatting;
            currentSortColumn = "";
            centerReady = true;
            LoadView();
        }
        private void UpdateCustomRange()
        {
            bool custom = cbPeriod.SelectedIndex == 6;
            dtpFromF.Visible = custom;
            dtpToF.Visible = custom;
        }
        /// <summary>Refreshes the joined data and the dropdown choices after the reports are loaded.</summary>
        private void RefreshCenterData()
        {
            if (!centerReady) return;
            deliveryByNumber = allDeliveries.GroupBy(d => d.DispatchNumber ?? "").ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            fileCounts = MJdatabase.GetAttachmentCounts("Dispatch");
            try { incidentCounts = MJdatabase.GetIncidentReports().GroupBy(i => i.DeliveryId).ToDictionary(g => g.Key, g => g.Count()); } catch { incidentCounts = new Dictionary<int, int>(); }
            var reported = new HashSet<string>(allTripReports.Select(t => t.DispatchNumber ?? ""), StringComparer.OrdinalIgnoreCase);
            awaitingReport = allDeliveries.Where(d => DispatchGate.IsCompleted(d.Status) && !reported.Contains(d.DispatchNumber ?? "")).OrderByDescending(d => d.DispatchDate).ToList();
            var linked = allTripReports.Select(DeliveryOf).Where(d => d != null).Select(d => d!).ToList();
            Refill(cbClientF, "All clients", linked.Select(d => d.ClientName));
            Refill(cbDriverF, "All drivers", linked.Select(d => d.DriverName));
            Refill(cbTruckF, "All trucks", linked.Select(TruckLabel));
        }
        private void Refill(ComboBox cb, string all, IEnumerable<string?> values)
        {
            string keep = cb.Text;
            applyingView = true;
            try
            {
                cb.Items.Clear();
                cb.Items.Add(all);
                foreach (var v in values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v)) cb.Items.Add(v);
                int i = cb.FindStringExact(keep);
                cb.SelectedIndex = i >= 0 ? i : 0;
            }
            finally { applyingView = false; }
        }
        private Delivery? DeliveryOf(TripReport t) => deliveryByNumber.TryGetValue(t.DispatchNumber ?? "", out var d) ? d : allDeliveries.FirstOrDefault(x => x.Id == t.DeliveryId);
        private static string TruckLabel(Delivery d)
        {
            string s = (d.TruckInfo ?? "").Trim();
            int i = s.IndexOf(' ');
            return i > 0 ? s.Substring(0, i) : s;
        }
        private static int Planned(Delivery? d) => d == null ? 0 : (d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate);
        private static int Delivered(TripReport t) => t.DeliveredCrates > 0 ? t.DeliveredCrates : t.DeliveredGCrates + t.DeliveredECrates;
        private static int Returned(TripReport t) => t.ReturnedCrates > 0 ? t.ReturnedCrates : t.ReturnedGCrates + t.ReturnedECrates;
        private int Variance(TripReport t) { var d = DeliveryOf(t); return d == null ? 0 : Delivered(t) - Planned(d); }
        private bool HasPod(TripReport t) { var d = DeliveryOf(t); return d != null && fileCounts.TryGetValue(d.Id, out var c) && c.Pods > 0; }
        private bool HasIncident(TripReport t) { var d = DeliveryOf(t); return d != null && incidentCounts.ContainsKey(d.Id); }
        /// <summary>Applies the period / client / driver / truck / problem filters and the chosen sort order.</summary>
        private IEnumerable<TripReport> ApplyCenter(IEnumerable<TripReport> src)
        {
            if (!centerReady) return src;
            DateTime today = DateTime.Today;
            DateTime? from = null, to = null;
            switch (cbPeriod.SelectedIndex)
            {
                case 1: from = today; to = today; break;
                case 2: from = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); to = today; break;
                case 3: from = new DateTime(today.Year, today.Month, 1); to = today; break;
                case 4: from = today.AddDays(-29); to = today; break;
                case 5: from = new DateTime(today.Year, 1, 1); to = today; break;
                case 6: from = dtpFromF.Value.Date; to = dtpToF.Value.Date; break;
            }
            var q = src;
            if (from.HasValue) q = q.Where(t => t.ReportDate.Date >= from.Value && t.ReportDate.Date <= to!.Value);
            if (cbClientF.SelectedIndex > 0) q = q.Where(t => string.Equals(DeliveryOf(t)?.ClientName?.Trim(), cbClientF.Text, StringComparison.OrdinalIgnoreCase));
            if (cbDriverF.SelectedIndex > 0) q = q.Where(t => string.Equals(DeliveryOf(t)?.DriverName?.Trim(), cbDriverF.Text, StringComparison.OrdinalIgnoreCase));
            if (cbTruckF.SelectedIndex > 0) q = q.Where(t => { var d = DeliveryOf(t); return d != null && string.Equals(TruckLabel(d), cbTruckF.Text, StringComparison.OrdinalIgnoreCase); });
            var periodSet = q.ToList();
            UpdateCenterTiles(periodSet);
            q = cbShowF.SelectedIndex switch
            {
                1 => periodSet.Where(t => Variance(t) < 0),
                2 => periodSet.Where(t => Returned(t) > 0),
                3 => periodSet.Where(HasIncident),
                4 => periodSet.Where(t => !HasPod(t)),
                5 => periodSet.Where(t => Variance(t) >= 0 && !HasIncident(t)),
                _ => periodSet
            };
            q = cbSortF.SelectedIndex switch
            {
                1 => q.OrderBy(t => t.ReportDate).ThenBy(t => t.Id),
                2 => q.OrderByDescending(Returned).ThenByDescending(t => t.ReportDate),
                3 => q.OrderBy(Variance).ThenByDescending(t => t.ReportDate),
                4 => q.OrderBy(t => DeliveryOf(t)?.ClientName ?? "", StringComparer.OrdinalIgnoreCase).ThenByDescending(t => t.ReportDate),
                5 => q.OrderBy(t => t.TripReportNumber, StringComparer.OrdinalIgnoreCase),
                _ => q.OrderByDescending(t => t.ReportDate).ThenByDescending(t => t.Id)
            };
            return q;
        }
        private void UpdateCenterTiles(List<TripReport> set)
        {
            if (tReports == null) return;
            int del = set.Sum(Delivered), ret = set.Sum(Returned);
            tReports.Value = set.Count.ToString("N0");
            tDelivered!.Value = del.ToString("N0");
            tDelivered.Note = $"{set.Sum(t => Planned(DeliveryOf(t))):N0} planned";
            tReturnRate!.Value = del == 0 ? "—" : $"{ret * 100.0 / del:0.#}%";
            tReturnRate.Note = $"{ret:N0} crate(s) returned";
            int shortCount = set.Count(t => Variance(t) < 0);
            tShort!.Value = shortCount.ToString("N0");
            tShort.ValueColor = shortCount > 0 ? AppTheme.DangerText : null;
            int noPod = set.Count(t => !HasPod(t));
            tNoPod!.Value = noPod.ToString("N0");
            tNoPod.ValueColor = noPod > 0 ? AppTheme.WarningText : null;
            tAwaiting!.Value = awaitingReport.Count.ToString("N0");
            tAwaiting.Note = awaitingReport.Count == 0 ? "all completed trips are reported" : "click to start one";
            tAwaiting.ValueColor = awaitingReport.Count > 0 ? AppTheme.WarningText : null;
        }
        private void ShowAwaitingMenu()
        {
            if (awaitingReport.Count == 0) { ToastHelper.ShowToast(this, "Every completed trip already has a report", ToastType.Info); return; }
            var m = new ContextMenuStrip();
            m.Items.Add(new ToolStripMenuItem("Completed trips without a report — pick one to start") { Enabled = false });
            foreach (var d in awaitingReport.Take(25))
                m.Items.Add($"{d.DispatchNumber}  ·  {d.DispatchDate:MMM d}  ·  {d.ClientName}  ·  {d.DriverName}", null, (s, e) => OpenForDispatch(d.DispatchNumber));
            if (awaitingReport.Count > 25) m.Items.Add(new ToolStripMenuItem($"… and {awaitingReport.Count - 25} more") { Enabled = false });
            m.Show(tAwaiting!, new Point(0, tAwaiting!.Height));
        }
        private void GridTripReports_CenterFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || gridTripReports.Rows[e.RowIndex].DataBoundItem is not TripReport t) return;
            string col = gridTripReports.Columns[e.ColumnIndex].Name;
            if (!col.StartsWith("x")) return;
            var d = DeliveryOf(t);
            switch (col)
            {
                case "xClient": e.Value = d?.ClientName ?? "—"; break;
                case "xTruck": e.Value = d == null ? "—" : TruckLabel(d); break;
                case "xDriver": e.Value = d?.DriverName ?? "—"; break;
                case "xPlanned": e.Value = d == null ? "—" : Planned(d).ToString("N0"); break;
                case "xVariance":
                    int v = d == null ? 0 : Delivered(t) - Planned(d);
                    e.Value = d == null ? "—" : v == 0 ? "0" : v.ToString("+#,0;-#,0");
                    if (v < 0) e.CellStyle!.ForeColor = AppTheme.DangerText;
                    else if (v > 0) e.CellStyle!.ForeColor = AppTheme.WarningText;
                    break;
                case "xFiles":
                    var c = d != null && fileCounts.TryGetValue(d.Id, out var fc) ? fc : (0, 0);
                    e.Value = c.Item1 == 0 ? "none" : c.Item2 > 0 ? $"📎 {c.Item1} · POD" : $"📎 {c.Item1}";
                    e.CellStyle!.ForeColor = c.Item2 > 0 ? AppTheme.SuccessText : AppTheme.WarningText;
                    break;
            }
            e.FormattingApplied = true;
        }
        // ------------------------------------------------------------------ remembered parameters
        private void SaveView()
        {
            if (applyingView || !centerReady) return;
            try
            {
                string v = string.Join("|", cbPeriod.SelectedIndex, cbShowF.SelectedIndex, cbSortF.SelectedIndex, cbClientF.Text, cbDriverF.Text, cbTruckF.Text, dtpFromF.Value.ToString("yyyy-MM-dd"), dtpToF.Value.ToString("yyyy-MM-dd"));
                MJdatabase.SetSystemSetting(ViewSettingKey + "." + SessionContext.CurrentUsername, v, "Trip report filters");
            }
            catch { }
        }
        private void LoadView()
        {
            string v;
            try { v = MJdatabase.GetSystemSetting(ViewSettingKey + "." + SessionContext.CurrentUsername, ""); } catch { v = ""; }
            var p = v.Split('|');
            if (p.Length < 8) return;
            applyingView = true;
            try
            {
                int I(string s, ComboBox cb) => int.TryParse(s, out var i) && i >= 0 && i < cb.Items.Count ? i : 0;
                cbPeriod.SelectedIndex = I(p[0], cbPeriod);
                cbShowF.SelectedIndex = I(p[1], cbShowF);
                cbSortF.SelectedIndex = I(p[2], cbSortF);
                pendingClient = p[3]; pendingDriver = p[4]; pendingTruck = p[5];
                if (DateTime.TryParse(p[6], out var f)) dtpFromF.Value = f;
                if (DateTime.TryParse(p[7], out var t)) dtpToF.Value = t;
                UpdateCustomRange();
            }
            finally { applyingView = false; }
        }
        private string pendingClient = "", pendingDriver = "", pendingTruck = "";
        private void ApplyPendingPicks()
        {
            applyingView = true;
            try
            {
                void Pick(ComboBox cb, ref string val) { if (val.Length == 0) return; int i = cb.FindStringExact(val); if (i >= 0) cb.SelectedIndex = i; val = ""; }
                Pick(cbClientF, ref pendingClient);
                Pick(cbDriverF, ref pendingDriver);
                Pick(cbTruckF, ref pendingTruck);
            }
            finally { applyingView = false; }
        }
        private void ResetView()
        {
            if (MessageBox.Show(FindForm(), "Are you sure you want to reset the trip report filters and sorting to their defaults?", "Reset filters", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            applyingView = true;
            try
            {
                foreach (var cb in new[] { cbPeriod, cbClientF, cbDriverF, cbTruckF, cbShowF, cbSortF }) cb.SelectedIndex = 0;
                dtpFromF.Value = DateTime.Today.AddDays(-30);
                dtpToF.Value = DateTime.Today;
                txtGridSearch.Text = "";
                currentSortColumn = "";
                UpdateCustomRange();
                trTiles?.ClearSelection();
            }
            finally { applyingView = false; }
            SaveView();
            FilterGridData();
        }
    }
}
