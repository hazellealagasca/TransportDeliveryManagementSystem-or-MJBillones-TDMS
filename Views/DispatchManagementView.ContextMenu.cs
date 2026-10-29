using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Right-click menu for the dispatch list. It is built each time it opens, so it only offers what makes sense
    /// for the order(s) clicked: what to do next for that status, copy/print/find tools, and table exports.
    /// Several orders can be selected (Ctrl/Shift-click) for bulk complete, print and copy.
    /// </summary>
    public partial class DispatchManagementView
    {
        private ContextMenuStrip? rowMenu;
        private void SetupContextMenu()
        {
            gridDispatch.MultiSelect = true;
            gridDispatch.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                var row = gridDispatch.Rows[e.RowIndex];
                if (!row.Selected)
                {
                    gridDispatch.ClearSelection();
                    int col = e.ColumnIndex >= 0 ? e.ColumnIndex : FirstVisibleColumnIndex();
                    if (col >= 0) gridDispatch.CurrentCell = row.Cells[col];
                    row.Selected = true;
                }
                ShowRowMenu(gridDispatch.PointToClient(Cursor.Position));
            };
            gridDispatch.KeyDown += (s, e) =>
            {
                var d = GetGridSelectedDelivery();
                if (d == null) return;
                bool handled = true;
                if (e.KeyCode == Keys.Enter && !e.Control) RecordDetailsDialog.ShowDetails(this, d);
                else if (e.KeyCode == Keys.E && e.Control) EditDeliveryOrder(d);
                else if (e.KeyCode == Keys.C && e.Control && !e.Shift) CopyToClipboard(string.Join(Environment.NewLine, SelectedDeliveries().Select(x => x.DispatchNumber)), "Dispatch # copied");
                else if (e.KeyCode == Keys.C && e.Control && e.Shift) CopyToClipboard(DispatchDetailsDialog.BuildSummaryText(d), "Order summary copied");
                else if (e.KeyCode == Keys.P && e.Control) PrintLoadingSheet(SelectedDeliveries());
                else if (e.KeyCode == Keys.Apps || (e.KeyCode == Keys.F10 && e.Shift))
                {
                    var r = gridDispatch.CurrentRow;
                    var rect = r != null ? gridDispatch.GetRowDisplayRectangle(r.Index, false) : Rectangle.Empty;
                    ShowRowMenu(new Point(rect.Left + 40, rect.Bottom));
                }
                else handled = false;
                if (handled) { e.Handled = true; e.SuppressKeyPress = true; }
            };
        }
        private int FirstVisibleColumnIndex() => gridDispatch.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).Select(c => c.Index).DefaultIfEmpty(-1).First();
        private List<Delivery> SelectedDeliveries() => gridDispatch.SelectedRows.Cast<DataGridViewRow>()
            .OrderBy(r => r.Index).Select(r => r.DataBoundItem as Delivery).Where(d => d != null).Select(d => d!).ToList();
        private static bool IsActiveStatus(Delivery d) => !string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) && !string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
        private static bool IsCompletedStatus(Delivery d) => string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        private static bool IsCancelledStatus(Delivery d) => string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
        // ------------------------------------------------------------------ Building the menu
        private void ShowRowMenu(Point at)
        {
            var selected = SelectedDeliveries();
            if (selected.Count == 0) return;
            rowMenu?.Dispose();
            rowMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F), ShowImageMargin = false, ShowCheckMargin = false };
            if (selected.Count == 1) BuildSingleMenu(rowMenu, selected[0]);
            else BuildMultiMenu(rowMenu, selected);
            rowMenu.Show(gridDispatch, at);
        }
        private static ToolStripMenuItem Item(string text, Action act, string shortcut = "", bool enabled = true, Color? color = null)
        {
            var it = new ToolStripMenuItem(text.Replace("&", "&&")) { ShortcutKeyDisplayString = shortcut, Enabled = enabled };
            if (color.HasValue) it.ForeColor = color.Value;
            it.Click += (s, e) => act();
            return it;
        }
        private static ToolStripItem[] Header(string title, string sub) => new ToolStripItem[]
        {
            new ToolStripLabel(title.Replace("&", "&&")) { Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(8, 6, 8, 0) },
            new ToolStripLabel(sub.Replace("&", "&&")) { Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Margin = new Padding(8, 0, 8, 4) }
        };
        private static ToolStripMenuItem Sub(string text, params ToolStripItem[] items)
        {
            var it = new ToolStripMenuItem(text.Replace("&", "&&"));
            it.DropDown.Font = new Font("Segoe UI", 9.5F);
            if (it.DropDown is ToolStripDropDownMenu ddm) { ddm.ShowImageMargin = false; }
            it.DropDownItems.AddRange(items);
            return it;
        }
        private void BuildSingleMenu(ContextMenuStrip m, Delivery d)
        {
            TripReport? trip = null;
            Invoice? invoice = null;
            int incidents = 0;
            try { trip = MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == d.Id || t.DispatchNumber == d.DispatchNumber); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            try { invoice = MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == d.Id); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            try { incidents = MJdatabase.GetIncidentReports().Count(i => i.DeliveryId == d.Id); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            bool active = IsActiveStatus(d), completed = IsCompletedStatus(d), cancelled = IsCancelledStatus(d);
            string status = MJdatabase.IsAwaitingCompletion(d) ? "Awaiting completion" : d.Status;
            string client = string.IsNullOrWhiteSpace(d.ClientName) ? "No client" : d.ClientName;
            m.Items.AddRange(Header(d.DispatchNumber, $"{client}  ·  {d.DispatchDate:MMM d}  ·  {status}"));
            m.Items.Add(new ToolStripSeparator());
            // Open
            m.Items.Add(Item("🔍  Open order details", () => RecordDetailsDialog.ShowDetails(this, d), "Enter"));
            m.Items.Add(Item("✎  Edit order", () => EditDeliveryOrder(d), "Ctrl+E", enabled: !cancelled));
            m.Items.Add(Item("⧉  Duplicate as new order", () => DuplicateAsNewOrder(d)));
            m.Items.Add(new ToolStripSeparator());
            // What to do next, depending on where the order stands
            if (active)
            {
                if (IsOnTheRoad(d))
                    m.Items.Add(Item("✔  Mark order complete (arrived)", () => CompleteSelectedOrder(d), color: AppTheme.SuccessText));
                else
                {
                    m.Items.Add(Item($"🚚  Start trip now (set to leave {MJdatabase.DepartureDateTime(d):MMM d, h:mm tt})", () => StartTripNow(d), enabled: d.DispatchDate.Date <= DateTime.Today));
                    m.Items.Add(Item("✔  Complete — available once the truck is on the road", () => { }, enabled: false));
                }
                m.Items.Add(Item("📅  Reschedule…", () => RescheduleOrder(d)));
            }
            if (!cancelled)
            {
                m.Items.Add(completed || trip != null
                    ? Item(trip == null ? "📝  File trip report" : $"📝  Open trip report ({trip.TripReportNumber})", () => MainForm.Instance?.OpenTripReportForDispatch(d))
                    : Item("📝  Trip report — mark the order complete first", () => { }, enabled: false));
                if (completed)
                {
                    m.Items.Add(invoice == null
                        ? Item("💰  Create invoice", () => OnCreateInvoiceRequested?.Invoke(d))
                        : Item($"💰  Open invoice ({invoice.InvoiceNumber} · {invoice.Status})", () => MainForm.Instance?.NavigateToFinance("Billing")));
                }
                m.Items.Add(incidents > 0
                    ? Sub($"⚠  Incidents ({incidents})",
                        Item("View incidents for this order", () => MainForm.Instance?.ShowIncidentsForDispatch(d.DispatchNumber)),
                        Item("Log another incident", () => MainForm.Instance?.LogIncidentForDispatch(d)))
                    : Item(completed || DispatchGate.HasDeparted(d.Status) ? "⚠  Log incident / damage" : "⚠  Incidents — available once the truck leaves", () => MainForm.Instance?.LogIncidentForDispatch(d), enabled: completed || DispatchGate.HasDeparted(d.Status)));
                m.Items.Add(new ToolStripSeparator());
            }
            // Tools
            m.Items.Add(Sub("📋  Copy",
                Item("Dispatch number", () => CopyToClipboard(d.DispatchNumber, "Dispatch # copied"), "Ctrl+C"),
                Item("Order summary (for Viber / SMS / email)", () => CopyToClipboard(DispatchDetailsDialog.BuildSummaryText(d), "Order summary copied"), "Ctrl+Shift+C"),
                Item("Driver & aide contacts", () => CopyToClipboard(DispatchDetailsDialog.BuildCrewContactsText(d), "Crew contacts copied")),
                Item("Store list", () => CopyToClipboard(StoreListText(d), "Store list copied"))));
            m.Items.Add(Sub("🖨  Print",
                Item("Dispatch / loading sheet", () => PrintLoadingSheet(new List<Delivery> { d }), "Ctrl+P"),
                Item("Official trip report", () => PrintHelper.PrintOfficialTripReport(d.Id, d.DispatchNumber), enabled: !cancelled),
                Item("Dispatch list (whole table)", () => BtnPrint_Click(this, EventArgs.Empty))));
            string routeCode = RouteCodeOf(d);
            var find = Sub("🔎  Find related",
                Item($"Orders for {Shorten(client, 28)}", () => SearchList(d.ClientName), enabled: !string.IsNullOrWhiteSpace(d.ClientName)),
                Item($"Orders on route {Shorten(string.IsNullOrEmpty(routeCode) ? d.CleanRouteDisplay : routeCode, 24)}", () => SearchList(string.IsNullOrEmpty(routeCode) ? d.CleanRouteDisplay : routeCode)),
                Item($"Orders with truck {Shorten(PlateOf(d), 20)}", () => SearchList(PlateOf(d)), enabled: !string.IsNullOrWhiteSpace(PlateOf(d))),
                Item($"Orders with driver {Shorten(d.DriverName, 24)}", () => SearchList(d.DriverName), enabled: !string.IsNullOrWhiteSpace(d.DriverName)),
                new ToolStripSeparator(),
                Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(d.ClientName), enabled: !string.IsNullOrWhiteSpace(d.ClientName)),
                Item("Open route in Delivery Routes", () => MainForm.Instance?.NavigateToDeliveryRoutes(routeCode: routeCode, isNewRoute: false, returnTarget: "Dispatch",
                    backAction: () => MainForm.Instance?.NavigateToOperations("Dispatch")), enabled: !string.IsNullOrEmpty(routeCode)),
                Item("Clear search", () => SearchList(""), enabled: !string.IsNullOrWhiteSpace(txtGridSearch.Text)));
            m.Items.Add(find);
            m.Items.Add(Sub("📊  Export table",
                Item("To CSV…", () => DataExportHelper.ExportToCsv(gridDispatch, "Dispatch_Orders", "Active and Dispatched Trips")),
                Item("To Excel (.xml)…", () => DataExportHelper.ExportToExcelXml(gridDispatch, "Dispatch_Orders", "Active and Dispatched Trips"))));
            if (active)
            {
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add(Item("✕  Cancel order…", () => CancelOrder(d), color: AppTheme.DangerText));
            }
        }
        private void BuildMultiMenu(ContextMenuStrip m, List<Delivery> list)
        {
            var activeOnes = list.Where(IsActiveStatus).ToList();
            decimal crates = list.Sum(x => x.TotalCrates > 0 ? x.TotalCrates : x.GCrate + x.ECrate);
            m.Items.AddRange(Header($"{list.Count} orders selected", $"{crates:N0} crates  ·  {activeOnes.Count} still active"));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Item(activeOnes.Count > 0 ? $"✔  Mark {activeOnes.Count} active order{(activeOnes.Count == 1 ? "" : "s")} complete" : "✔  Mark complete (none active)",
                () => CompleteMany(activeOnes), enabled: activeOnes.Count > 0, color: activeOnes.Count > 0 ? AppTheme.SuccessText : (Color?)null));
            m.Items.Add(Item($"🖨  Print loading sheet for {list.Count} orders", () => PrintLoadingSheet(list), "Ctrl+P"));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Sub("📋  Copy",
                Item("Dispatch numbers", () => CopyToClipboard(string.Join(Environment.NewLine, list.Select(x => x.DispatchNumber)), $"{list.Count} dispatch numbers copied"), "Ctrl+C"),
                Item("Order summaries", () => CopyToClipboard(string.Join(Environment.NewLine + Environment.NewLine, list.Select(DispatchDetailsDialog.BuildSummaryText)), $"{list.Count} summaries copied"))));
            m.Items.Add(Sub("📊  Export table",
                Item("To CSV…", () => DataExportHelper.ExportToCsv(gridDispatch, "Dispatch_Orders", "Active and Dispatched Trips")),
                Item("To Excel (.xml)…", () => DataExportHelper.ExportToExcelXml(gridDispatch, "Dispatch_Orders", "Active and Dispatched Trips"))));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Item("Clear selection", () => gridDispatch.ClearSelection()));
        }
        // ------------------------------------------------------------------ Actions
        private void CopyToClipboard(string text, string toast)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try { Clipboard.SetText(text); ToastHelper.ShowToast(this, toast, ToastType.Success); } catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
        }
        private static string Shorten(string? s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max - 1) + "…");
        private static string RouteCodeOf(Delivery d)
        {
            var r = MJdatabase.FindRouteByNameOrCode(string.IsNullOrWhiteSpace(d.Destination) ? d.LoadingScheduleInfo : d.Destination);
            return r?.RouteCode ?? "";
        }
        private static string PlateOf(Delivery d)
        {
            string t = (d.TruckInfo ?? "").Trim();
            int cut = t.IndexOfAny(new[] { '[', '(' });
            return (cut > 0 ? t.Substring(0, cut) : t).Trim();
        }
        private static string StoreListText(Delivery d)
        {
            var stops = MJdatabase.GetDispatchStops(d.Id).OrderBy(x => x.StopSequence).ToList();
            if (stops.Count == 0) return string.IsNullOrWhiteSpace(d.Destination) ? d.CleanRouteDisplay : d.Destination;
            return string.Join(Environment.NewLine, stops.Select(st => $"{st.StopSequence}. {(string.IsNullOrWhiteSpace(st.StoreCode) ? "" : st.StoreCode + " - ")}{st.Destination}  (G {st.GCrate}, E {st.ECrate})"));
        }
        private void SearchList(string text)
        {
            txtGridSearch.Text = (text ?? "").Trim();
            txtGridSearch.Focus();
            txtGridSearch.SelectionStart = txtGridSearch.Text.Length;
        }
        private void PrintLoadingSheet(List<Delivery> list)
        {
            if (list.Count == 0) return;
            try { PrintHelper.PrintOfficialLoadingScheduleFromDeliveries(list); }
            catch (Exception ex) { ErrLog.Error("DispatchManagementView.ContextMenu", ex);  MessageBox.Show($"Print failed: {ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void RescheduleOrder(Delivery d)
        {
            if (!IsActiveStatus(d)) return;
            using var dlg = new RescheduleDeliveryDialog(d);
            if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
            {
                LoadDropdownsAndGrid();
                ToastHelper.ShowToast(this, $"{d.DispatchNumber} rescheduled to {dlg.RescheduledDate:MMM d}", ToastType.Success);
            }
        }
        private void CompleteMany(List<Delivery> list)
        {
            if (list.Count == 0) return;
            if (list.Count == 1) { CompleteSelectedOrder(list[0]); return; }
            var confirm = MessageBox.Show($"Mark these {list.Count} orders as completed?\n\n" + string.Join("\n", list.Take(12).Select(x => $"• {x.DispatchNumber}  ({x.ClientName}, {x.DispatchDate:MMM d})")) + (list.Count > 12 ? $"\n• … and {list.Count - 12} more" : "") +
                "\n\nThis frees their trucks, drivers and aides. Trip reports can still be filed afterwards.", "Complete orders", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            int ok = 0;
            var failed = new List<string>();
            foreach (var d in list)
            {
                var (success, message) = MJdatabase.CompleteDelivery(d.Id, SessionContext.CurrentUsername);
                if (success) ok++; else failed.Add($"{d.DispatchNumber}: {message}");
            }
            LoadDropdownsAndGrid();
            try { MainForm.Instance?.UpdateHeaderKpiBadges(); } catch (System.Exception swx5) { ErrLog.Swallowed(swx5); }
            if (failed.Count > 0) MessageBox.Show($"{ok} completed. These could not be completed:\n\n" + string.Join("\n", failed), "Complete orders", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else ToastHelper.ShowToast(this, $"{ok} orders marked complete", ToastType.Success);
        }
        private void CancelOrder(Delivery d)
        {
            try
            {
                if (!IsActiveStatus(d))
                {
                    MessageBox.Show($"Dispatch {d.DispatchNumber} is already {d.Status.ToLowerInvariant()}.", "Cancel order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string? reason = CancellationReasonDialog.PromptCancellationReason(this.FindForm() ?? this.ParentForm!, "Dispatch Order", d.DispatchNumber);
                if (string.IsNullOrEmpty(reason)) return;
                d.Status = "Cancelled";
                d.CancellationReason = reason;
                MJdatabase.SaveDelivery(d);
                LoadDropdownsAndGrid();
                try { MainForm.Instance?.UpdateHeaderKpiBadges(); } catch (System.Exception swx6) { ErrLog.Swallowed(swx6); }
                ToastHelper.ShowToast(this, $"Dispatch {d.DispatchNumber} cancelled", ToastType.Warning);
            }
            catch (Exception ex)
            { ErrLog.Error("DispatchManagementView.ContextMenu", ex); 
                MessageBox.Show($"Failed to cancel dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>Opens the order form pre-filled from an existing order (client, route, stores, crates, truck and crew) as a new order.</summary>
        private void DuplicateAsNewOrder(Delivery source)
        {
            try
            {
                var full = MJdatabase.GetDeliveryById(source.Id) ?? source;
                SetFormVisibility(true);
                BindDeliveryToForm(full);
                selectedDelivery = null;
                isBindingForm = true;
                lastAutoDispatchNumber = GenerateNextDispatchNumber();
                txtDispatchNumber.Text = lastAutoDispatchNumber;
                txtDispatchNumber.Enabled = true;
                DateTime day = DateTime.Today;
                if (day >= dtpDispatchDate.MinDate && day <= dtpDispatchDate.MaxDate) dtpDispatchDate.Value = day;
                if (cbStatus.Items.Contains("Scheduled")) cbStatus.SelectedItem = "Scheduled"; else cbStatus.Text = "Scheduled";
                isBindingForm = false;
                lblFormHeader.Text = $"New Dispatch Order  ·  copy of {full.DispatchNumber}";
                SetButtonStates(false);
                gridDispatch.ClearSelection();
                ToastHelper.ShowToast(this, $"Copied {full.DispatchNumber}. Check the date, truck and crew, then save.", ToastType.Info);
                dtpDispatchDate.Focus();
            }
            catch (Exception ex)
            { ErrLog.Error("DispatchManagementView.ContextMenu", ex); 
                MessageBox.Show($"Couldn't duplicate the order: {ex.Message}", "Duplicate order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
