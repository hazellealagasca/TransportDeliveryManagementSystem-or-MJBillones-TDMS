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
    /// Nov 9, 2026: the two lists on Trip reports.
    /// Awaiting report: ▾ · Dispatch # · Date · Client · [Create trip report] · Destination · Truck · Driver · Crates · Waiting. Search, client, driver and
    /// truck filters apply to it, and the header counts what is shown.
    /// Trip reports: ▾ · Dispatch # · Date · Client · [Create invoice / Invoice no.] · … The first columns are frozen so they never scroll out of view.
    /// ▾ opens the same order summary window as Dispatch.
    /// </summary>
    public partial class TripReportView
    {
        private List<AwaitingReportRow> awaitingAll = new List<AwaitingReportRow>();
        private Dictionary<int, string> invoiceNoByTrip = new Dictionary<int, string>();

        private static string TruckLabelOf(string? truckInfo)
        {
            string s = (truckInfo ?? "").Trim();
            int i = s.IndexOf(' ');
            return i > 0 ? s.Substring(0, i) : s;
        }

        private DataGridViewButtonColumn NewDetailsColumn() => new DataGridViewButtonColumn
        {
            Name = "colDetails",
            HeaderText = "",
            Text = "▾",
            UseColumnTextForButtonValue = true,
            FlatStyle = FlatStyle.Flat,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = Dpi(44),
            MinimumWidth = Dpi(44),
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            ToolTipText = "Order summary"
        };

        // ------------------------------------------------------------------ Awaiting report list

        private void ArrangeAwaitingGrid()
        {
            var g = gridAwaiting;
            if (g == null || g.Columns.Contains("colDetails")) return;
            g.Columns.Insert(0, NewDetailsColumn());
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCrates", DataPropertyName = nameof(AwaitingReportRow.TotalCrates), HeaderText = "Crates", FillWeight = 50, MinimumWidth = Dpi(80), ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWaiting", DataPropertyName = nameof(AwaitingReportRow.Waiting), HeaderText = "Waiting", FillWeight = 60, MinimumWidth = Dpi(100), ReadOnly = true });
            DataGridViewColumn? By(string prop) => g.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.DataPropertyName == prop);
            int i = 0;
            void Put(DataGridViewColumn? c) { if (c != null && c.Visible) c.DisplayIndex = i++; }
            Put(g.Columns["colDetails"]);
            Put(By(nameof(AwaitingReportRow.DispatchNumber)));
            Put(By(nameof(AwaitingReportRow.DispatchDate)));
            Put(By(nameof(AwaitingReportRow.ClientName)));
            Put(g.Columns["colCreate"]);                    // the button sits right beside the dispatch # and date
            Put(By(nameof(AwaitingReportRow.Destination)));
            Put(By(nameof(AwaitingReportRow.TruckInfo)));
            Put(By(nameof(AwaitingReportRow.DriverName)));
            Put(g.Columns["colCrates"]);
            Put(g.Columns["colWaiting"]);
            if (g.Columns["colCreate"] != null) g.Columns["colCreate"]!.Frozen = true;   // freezes everything to its left too
            g.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || g.Columns[e.ColumnIndex].Name != "colDetails") return;
                if (g.Rows[e.RowIndex].DataBoundItem is AwaitingReportRow row) ShowDetailsFor(row.DeliveryId > 0 ? row.DeliveryId : 0, row.DeliveryId < 0 ? -row.DeliveryId : 0);
            };
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || g.Rows[e.RowIndex].DataBoundItem is not AwaitingReportRow row) return;
                if (g.Columns[e.ColumnIndex].Name == "colWaiting" && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = row.WaitingDays >= MJdatabase.WaitingRedDays ? AppTheme.DangerText : row.WaitingDays >= MJdatabase.WaitingAmberDays ? AppTheme.WarningText : AppTheme.TextSecondary;
                    e.CellStyle.Font = new Font(g.Font, row.WaitingDays >= MJdatabase.WaitingAmberDays ? FontStyle.Bold : FontStyle.Regular);
                }
            };
        }

        /// <summary>Completed special trips that still need a report, shown as waiting rows too (Id = −special trip id), so this list matches the drop-down.</summary>
        private List<AwaitingReportRow> SpecialAwaitingRows()
        {
            if (!Features.On(Features.SpecialTrips)) return new List<AwaitingReportRow>();   // Nov 10: special trips are hidden
            var finished = new HashSet<string>(allTripReports.Where(t => !string.Equals(t.Status, TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase)).Select(t => t.DispatchNumber ?? ""), StringComparer.OrdinalIgnoreCase);
            return allDeliveries.Where(d => d.Id < 0 && DispatchGate.IsCompleted(d.Status) && !finished.Contains(d.DispatchNumber ?? ""))
                .Select(d => new AwaitingReportRow
                {
                    DeliveryId = d.Id,
                    DispatchNumber = d.DispatchNumber ?? "",
                    DispatchDate = d.DispatchDate,
                    ClientName = d.ClientName ?? "",
                    Destination = d.LoadingScheduleInfo ?? "",
                    TruckInfo = d.TruckInfo ?? "",
                    DriverName = d.DriverName ?? "",
                    TotalCrates = d.TotalCrates
                }).ToList();
        }

        /// <summary>Applies the search box and the client / driver / truck filters to the waiting list and counts what is shown. (The date period belongs to the reports, so it never hides a waiting trip.)</summary>
        private void ApplyAwaitingView()
        {
            if (gridAwaiting == null) return;
            string q = (txtGridSearch?.Text ?? "").Trim().ToLowerInvariant();
            IEnumerable<AwaitingReportRow> rows = awaitingAll;
            if (q.Length > 0)
                rows = rows.Where(r => $"{r.DispatchNumber} {r.ClientName} {r.Destination} {r.TruckInfo} {r.DriverName}".ToLowerInvariant().Contains(q));
            if (centerReady && cbClientF.SelectedIndex > 0) rows = rows.Where(r => string.Equals(r.ClientName?.Trim(), cbClientF.Text, StringComparison.OrdinalIgnoreCase));
            if (centerReady && cbDriverF.SelectedIndex > 0) rows = rows.Where(r => string.Equals(r.DriverName?.Trim(), cbDriverF.Text, StringComparison.OrdinalIgnoreCase));
            if (centerReady && cbTruckF.SelectedIndex > 0) rows = rows.Where(r => string.Equals(TruckLabelOf(r.TruckInfo), cbTruckF.Text, StringComparison.OrdinalIgnoreCase));
            var list = rows.OrderBy(r => r.DispatchDate).ThenBy(r => r.DispatchNumber, StringComparer.OrdinalIgnoreCase).ToList();   // longest waiting first
            gridAwaiting.DataSource = list;
            if (showAwaiting)
                lblGridHeader.Text = list.Count == awaitingAll.Count
                    ? $"Awaiting report: {awaitingAll.Count} dispatch{(awaitingAll.Count == 1 ? "" : "es")}"
                    : $"Awaiting report: {list.Count} of {awaitingAll.Count}";
        }

        // ------------------------------------------------------------------ Trip reports list (Active / History)

        private void ArrangeReportGrid()
        {
            var g = gridTripReports;
            if (g.Columns.Contains("colDetails")) return;
            g.Columns.Insert(0, NewDetailsColumn());
            var inv = new DataGridViewButtonColumn
            {
                Name = "colInvoice",
                HeaderText = "Invoice",
                FlatStyle = FlatStyle.Flat,
                FillWeight = 120,
                MinimumWidth = Dpi(170),
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            inv.DefaultCellStyle.BackColor = AppTheme.AccentSoft;
            inv.DefaultCellStyle.ForeColor = AppTheme.AccentText;
            inv.DefaultCellStyle.SelectionBackColor = AppTheme.AccentSoft;
            inv.DefaultCellStyle.SelectionForeColor = AppTheme.AccentText;
            inv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.Columns.Add(inv);
            DataGridViewColumn? By(string prop) => g.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.DataPropertyName == prop);
            int i = 0;
            void Put(DataGridViewColumn? c) { if (c != null && c.Visible) c.DisplayIndex = i++; }
            Put(g.Columns["colDetails"]);
            Put(By("DispatchNumber"));
            Put(By("ReportDate"));
            Put(g.Columns["xClient"]);
            Put(g.Columns["colInvoice"]);                   // beside the dispatch # and date
            Put(g.Columns["colStatus"]);
            if (g.Columns["colInvoice"] != null) g.Columns["colInvoice"]!.Frozen = true;
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || g.Columns[e.ColumnIndex].Name != "colInvoice") return;
                if (g.Rows[e.RowIndex].DataBoundItem is not TripReport t) return;
                e.Value = ReportActionText(t);
                e.FormattingApplied = true;
            };
            g.CellContentClick += ReportGridButtonClick;
            // The Create invoice / Finish report cell is drawn as a button; an existing invoice number is drawn as a plain link-like label.
            g.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null || g.Columns[e.ColumnIndex].Name != "colInvoice") return;
                if (g.Rows[e.RowIndex].DataBoundItem is not TripReport t) return;
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                bool done = InvoiceNumberFor(t).Length > 0 || RowMenu.Is(t.Status, TripReportStatus.Invoiced);
                var r = Rectangle.Inflate(e.CellBounds, -Dpi(10), -Dpi(7));
                if (!done)
                {
                    using var fill = new SolidBrush(AppTheme.AccentSoft);
                    using var pen = new Pen(AppTheme.Accent);
                    e.Graphics.FillRectangle(fill, r);
                    e.Graphics.DrawRectangle(pen, r);
                }
                using var f = new Font(g.Font, done ? FontStyle.Regular : FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, ReportActionText(t), f, r, done ? AppTheme.AccentText : AppTheme.AccentText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.Handled = true;
            };
        }

        private string InvoiceNumberFor(TripReport t)
        {
            int key = t.DeliveryId > 0 ? t.DeliveryId : -t.SpecialTripId;
            return key != 0 && invoiceNoByTrip.TryGetValue(key, out var no) ? no : "";
        }

        /// <summary>What the Invoice button says: the invoice number once it is made, "Finish report" for a draft, otherwise Create invoice.</summary>
        private string ReportActionText(TripReport t)
        {
            string no = InvoiceNumberFor(t);
            if (no.Length > 0) return $"Invoice {no} ✓";
            if (RowMenu.Is(t.Status, TripReportStatus.Draft)) return "Finish report";
            if (RowMenu.Is(t.Status, TripReportStatus.Invoiced)) return "Invoiced ✓";
            return "➡ Proceed to SOA";
        }

        private void RefreshInvoiceNumbers()
        {
            try
            {
                invoiceNoByTrip = MJdatabase.GetTripInvoices().Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0)
                    .GroupBy(i => i.DeliveryId > 0 ? i.DeliveryId : -i.SpecialTripId)
                    .ToDictionary(g => g.Key, g => g.First().Number);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private void ReportGridButtonClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var g = gridTripReports;
            if (g.Rows[e.RowIndex].DataBoundItem is not TripReport t) return;
            string col = g.Columns[e.ColumnIndex].Name;
            if (col == "colDetails") { ShowDetailsFor(t.DeliveryId, t.SpecialTripId); return; }
            if (col != "colInvoice") return;
            string no = InvoiceNumberFor(t);
            if (no.Length > 0) { MainForm.Instance?.ShowFinanceRecords("Billing", no); return; }
            if (RowMenu.Is(t.Status, TripReportStatus.Draft)) { OpenEditTripReport(t); return; }
            if (RowMenu.Is(t.Status, TripReportStatus.Invoiced)) { MainForm.Instance?.ShowFinanceRecords("Billing", t.DispatchNumber ?? ""); return; }
            g.ClearSelection();
            g.Rows[e.RowIndex].Selected = true;
            IssueInvoiceForSelected();                      // same checks as "Issue Invoice (Selected)": completed, one client, not invoiced yet
        }

        // ------------------------------------------------------------------ ▾ the order summary window

        /// <summary>Opens the dispatch details window (the one Dispatch uses) for a trip, or the special trip's details.</summary>
        private void ShowDetailsFor(int deliveryId, int specialTripId)
        {
            try
            {
                if (deliveryId > 0)
                {
                    var d = allDeliveries.FirstOrDefault(x => x.Id == deliveryId) ?? MJdatabase.GetDeliveries().FirstOrDefault(x => x.Id == deliveryId);
                    if (d != null) { Forms.DispatchDetailsDialog.ShowFor(this, d); return; }
                }
                else if (specialTripId > 0)
                {
                    Forms.SpecialTripDetailsDialog.ShowFor(this, specialTripId, id => { /* editing a special trip is done in Operations → Special Trip */ }, () => LoadData());
                    return;
                }
                ToastHelper.ShowToast(this, "The order's details could not be found.", ToastType.Info);
            }
            catch (Exception ex) { ErrLog.Error("TripReportView.ShowDetails", ex); }
        }
    }
}
