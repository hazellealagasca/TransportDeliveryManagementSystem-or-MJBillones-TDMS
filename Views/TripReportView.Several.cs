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
    /// Nov 9, 2026: several dispatches of ONE client on one trip report form. The stores of all picked dispatches share the "Stores, times &amp; signatures"
    /// table (a Dispatch column tells them apart) and their crates add up. Saving makes ONE trip report per dispatch, each from its own rows, so billing,
    /// statements and report numbers stay per dispatch. Gate times, signatories and remarks typed once are copied to each report.
    /// </summary>
    public partial class TripReportView
    {
        /// <summary>Nov 10 rule: one trip report is for ONE dispatch. The "add dispatches of this client" route is switched off; the code stays so it can be turned back on here.</summary>
        private static bool AllowSeveralDispatches => false;
        private readonly List<Delivery> extraDispatches = new List<Delivery>();
        private int extrasOwnerId;
        private string storesTitleSingle = "";
        private FlowLayoutPanel? pnlExtraDispatches;
        private Button? btnAddDispatches;
        private Label? lblExtraSummary;
        private Label? lblExtraWarning;

        // ------------------------------------------------------------------ do these dispatches look like one ride?

        /// <summary>
        /// How <paramref name="other"/> differs from <paramref name="main"/> in the things that say whether the two really rode together: the date,
        /// the truck and the driver. Empty when they match. Filing dispatches together copies the same gate times, signatories and remarks to each
        /// report, so a difference deserves a second look.
        /// </summary>
        public static List<string> DifferencesBetween(Delivery main, Delivery other)
        {
            var list = new List<string>();
            if (main.DispatchDate.Date != other.DispatchDate.Date) list.Add($"a different date ({other.DispatchDate:MMM d}, not {main.DispatchDate:MMM d})");
            bool truck = main.TruckId > 0 && other.TruckId > 0 ? main.TruckId != other.TruckId
                : !string.Equals(TruckLabelOf(main.TruckInfo), TruckLabelOf(other.TruckInfo), StringComparison.OrdinalIgnoreCase);
            if (truck) list.Add($"a different truck ({TruckLabelOf(other.TruckInfo)}, not {TruckLabelOf(main.TruckInfo)})");
            bool driver = main.DriverId > 0 && other.DriverId > 0 ? main.DriverId != other.DriverId
                : !string.Equals((main.DriverName ?? "").Trim(), (other.DriverName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            if (driver) list.Add($"a different driver ({(other.DriverName ?? "").Trim()}, not {(main.DriverName ?? "").Trim()})");
            return list;
        }

        /// <summary>One line per dispatch that differs from the main one ("D-0002 has a different date (…) and a different truck (…)"); empty when none do.</summary>
        private List<string> RideDifferenceLines(Delivery main, IEnumerable<Delivery> others)
            => others.Select(d => (D: d, Diff: DifferencesBetween(main, d))).Where(x => x.Diff.Count > 0)
                     .Select(x => $"{x.D.DispatchNumber} has {string.Join(" and ", x.Diff)}").ToList();

        private const string RideWarningTail = "Filing them together copies the same gate times, signatories and remarks to every report. Make sure they really rode together.";

        // ------------------------------------------------------------------ the row under the drop-down

        private FlowLayoutPanel BuildExtraDispatchesRow()
        {
            pnlExtraDispatches = new FlowLayoutPanel
            {
                Name = "noDirtyExtraDispatches",     // clicking here is not typing on the form
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0),
                Padding = new Padding(0, Dpi(2), 0, Dpi(6))
            };
            btnAddDispatches = new Button { Text = "＋ Add dispatches of this client", AutoSize = true, MinimumSize = new Size(0, Dpi(30)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, Dpi(8), Dpi(4)), Enabled = false, UseMnemonic = false };
            UIThemeHelper.SetButtonRole(btnAddDispatches, "secondary");
            btnAddDispatches.Click += (s, e) => AddDispatchesClicked();
            lblExtraSummary = new Label { AutoSize = true, Visible = false, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), Margin = new Padding(Dpi(6), Dpi(7), 0, 0), UseMnemonic = false };
            lblExtraWarning = new Label { AutoSize = false, Visible = false, ForeColor = AppTheme.WarningText, Font = new Font("Segoe UI Semibold", 9F), Margin = new Padding(0, 0, 0, Dpi(4)), UseMnemonic = false };
            pnlExtraDispatches.Controls.AddRange(new Control[] { btnAddDispatches, lblExtraSummary, lblExtraWarning });
            cbDelivery.SelectedIndexChanged += (s, e) => RefreshExtraUi();
            return pnlExtraDispatches;
        }

        private void RefreshExtraUi()
        {
            UpdateDeliveredCrateTotals();
            if (pnlExtraDispatches == null || btnAddDispatches == null || lblExtraSummary == null) return;
            var primary = GetSelectedDelivery();
            btnAddDispatches.Visible = AllowSeveralDispatches && !isEditingExistingReport;
            btnAddDispatches.Enabled = primary != null && primary.Id > 0 && !string.IsNullOrWhiteSpace(primary.ClientName);
            foreach (var c in pnlExtraDispatches.Controls.OfType<Button>().Where(b => b.Name == "chipDispatch").ToList()) { pnlExtraDispatches.Controls.Remove(c); c.Dispose(); }
            int at = 1;
            foreach (var d in extraDispatches.ToList())
            {
                var chip = new Button { Name = "chipDispatch", Text = $"{d.DispatchNumber} · {d.DispatchDate:MMM d}  ✕", AutoSize = true, MinimumSize = new Size(0, Dpi(30)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, Dpi(6), Dpi(4)), UseMnemonic = false };
                UIThemeHelper.SetButtonRole(chip, "secondary");
                var captured = d;
                chip.Click += (s, e) => RemoveExtra(captured);
                pnlExtraDispatches.Controls.Add(chip);
                pnlExtraDispatches.Controls.SetChildIndex(chip, at++);
            }
            // a standing amber note while the added dispatches differ from the main one in date, truck or driver
            var diffLines = primary != null && extraDispatches.Count > 0 ? RideDifferenceLines(primary, extraDispatches) : new List<string>();
            if (lblExtraWarning != null)
            {
                if (diffLines.Count > 0)
                {
                    lblExtraWarning.Text = "⚠ Check before filing: " + string.Join("; ", diffLines) + ". " + RideWarningTail;
                    int w = Math.Max(Dpi(320), pnlExtraDispatches.ClientSize.Width - Dpi(8));
                    lblExtraWarning.Width = w;
                    lblExtraWarning.Height = TextRenderer.MeasureText(lblExtraWarning.Text, lblExtraWarning.Font, new Size(w, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + Dpi(4);
                    lblExtraWarning.Visible = true;
                    pnlExtraDispatches.Controls.SetChildIndex(lblExtraWarning, pnlExtraDispatches.Controls.Count - 1);
                }
                else lblExtraWarning.Visible = false;
            }
            if (extraDispatches.Count > 0 && gridStores != null)
            {
                int stores = 0, crates = 0;
                foreach (DataGridViewRow r in gridStores.Rows)
                {
                    if (r.IsNewRow) continue;
                    stores++;
                    crates += RowInt(r, "colLoadE") + RowInt(r, "colLoadG");
                }
                lblExtraSummary.Text = $"{extraDispatches.Count + 1} dispatches · {stores} stores · {crates:N0} crates";
                lblExtraSummary.Visible = true;
                if (lblMultiDropDetails != null) lblMultiDropDetails.Text = $"🏪 Stores for {extraDispatches.Count + 1} dispatches of {(primary?.ClientName ?? "").Trim()} ({stores} stores)";
            }
            else
            {
                lblExtraSummary.Visible = false;
                if (lblMultiDropDetails != null && storesTitleSingle.Length > 0) lblMultiDropDetails.Text = storesTitleSingle;
            }
        }

        private static int RowInt(DataGridViewRow r, string col) => int.TryParse(r.Cells[col].Value?.ToString(), out int n) ? Math.Max(0, n) : 0;

        // ------------------------------------------------------------------ the Dispatch column and row ownership

        private void AddDispatchColumns()
        {
            if (gridStores.Columns.Contains("colDisp")) return;
            var disp = new DataGridViewTextBoxColumn { Name = "colDisp", HeaderText = "Dispatch", ReadOnly = true, FillWeight = 90, MinimumWidth = Dpi(120), Visible = false, SortMode = DataGridViewColumnSortMode.NotSortable };
            if (gridStores.Columns["colCode"] != null) disp.DefaultCellStyle = gridStores.Columns["colCode"]!.DefaultCellStyle.Clone();
            gridStores.Columns.Add(disp);
            gridStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDispId", HeaderText = "", Visible = false });
            disp.DisplayIndex = 0;
        }

        private int RowDispatchId(DataGridViewRow r)
            => gridStores != null && gridStores.Columns.Contains("colDispId") && int.TryParse(r.Cells["colDispId"].Value?.ToString(), out int id) ? id : 0;

        private void StampRow(DataGridViewRow r, Delivery d)
        {
            if (!gridStores.Columns.Contains("colDispId")) return;
            r.Cells["colDispId"].Value = d.Id;
            r.Cells["colDisp"].Value = d.DispatchNumber;
        }

        /// <summary>Marks every row that does not belong to an added dispatch as the main dispatch's.</summary>
        private void StampPrimaryRows(Delivery primary)
        {
            foreach (DataGridViewRow r in gridStores.Rows)
                if (!r.IsNewRow && RowDispatchId(r) == 0) StampRow(r, primary);
        }

        private List<DataGridViewRow> RowsOf(Delivery d)
            => gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && RowDispatchId(r) == d.Id).ToList();

        private void SetDispatchColumnVisible(bool on)
        {
            if (gridStores != null && gridStores.Columns.Contains("colDisp")) gridStores.Columns["colDisp"]!.Visible = on;
        }

        // ------------------------------------------------------------------ adding and removing dispatches

        private void AddDispatchesClicked()
        {
            var primary = GetSelectedDelivery();
            if (primary == null || primary.Id <= 0) return;
            string client = (primary.ClientName ?? "").Trim();
            var candidates = pickRows
                .Where(r => r.D.Id > 0 && !r.Draft && r.D.Id != primary.Id && !extraDispatches.Any(e => e.Id == r.D.Id)
                            && string.Equals(r.Client, client, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Date).ThenBy(r => r.D.DispatchNumber, StringComparer.OrdinalIgnoreCase).ToList();
            if (candidates.Count == 0)
            {
                ToastHelper.ShowToast(this, $"No other dispatch of {client} is waiting for a trip report.", ToastType.Info);
                return;
            }
            var chosen = ShowPickDispatchesDialog(client, candidates.Select(r => (r.D, r.Text)).ToList());
            if (chosen.Count == 0) return;
            var differ = RideDifferenceLines(primary, chosen);
            if (differ.Count > 0)
            {
                string text = "These dispatches may not have ridden with " + primary.DispatchNumber + ":" + Environment.NewLine + Environment.NewLine
                            + string.Join(Environment.NewLine, differ.Select(l => "• " + l)) + Environment.NewLine + Environment.NewLine
                            + RideWarningTail + Environment.NewLine + Environment.NewLine + "Add them anyway?";
                if (MessageBox.Show(FindForm(), text, "Check these dispatches", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            AddExtraDispatches(chosen);
        }

        /// <summary>Adds dispatches to the form; only dispatches of the same client as the picked one are accepted. Returns the ones that were refused.</summary>
        private List<Delivery> AddExtraDispatches(IEnumerable<Delivery> dispatches)
        {
            var refused = new List<Delivery>();
            if (!AllowSeveralDispatches) return dispatches.ToList();
            var primary = GetSelectedDelivery();
            if (primary == null || primary.Id <= 0) return dispatches.ToList();
            string client = (primary.ClientName ?? "").Trim();
            extrasOwnerId = primary.Id;
            StampPrimaryRows(primary);
            foreach (var d in dispatches)
            {
                if (d.Id <= 0 || d.Id == primary.Id || extraDispatches.Any(e => e.Id == d.Id) || !string.Equals((d.ClientName ?? "").Trim(), client, StringComparison.OrdinalIgnoreCase))
                { refused.Add(d); continue; }
                extraDispatches.Add(d);
                AppendRowsFor(d);
            }
            SetDispatchColumnVisible(extraDispatches.Count > 0);
            RecalculateTotalsFromStoreGrid();
            RefreshExtraUi();
            return refused;
        }

        private void AppendRowsFor(Delivery d)
        {
            if (gridStores == null) return;
            bool was = isUpdatingStoreGrid;
            isUpdatingStoreGrid = true;
            try
            {
                foreach (var s in MJdatabase.GetOrBuildTripStopsForDelivery(d))
                {
                    int loadE = (s.DeliveredECrates > 0 || s.DeliveredGCrates > 0) ? s.DeliveredECrates : s.ECrate;
                    int loadG = (s.DeliveredECrates > 0 || s.DeliveredGCrates > 0) ? s.DeliveredGCrates : s.GCrate;
                    int retE = (s.ReturnedECrates > 0 || s.ReturnedGCrates > 0) ? s.ReturnedECrates : loadE;
                    int retG = (s.ReturnedECrates > 0 || s.ReturnedGCrates > 0) ? s.ReturnedGCrates : loadG;
                    int rIdx = gridStores.Rows.Add(s.StoreCode, s.Destination, s.DrNumbers, s.ArrivalTime, s.DepartureTime, s.StoreManagerName, loadE, loadG, retE, retG, DropsFromRemarks(s.Remarks));
                    gridStores.Rows[rIdx].Tag = s;
                    StampRow(gridStores.Rows[rIdx], d);
                }
                SizeStoreTable(gridStores.Rows.Count);
                MarkDuplicateDrs(showToast: false);
            }
            finally { isUpdatingStoreGrid = was; }
        }

        private void RemoveExtra(Delivery d)
        {
            bool was = isUpdatingStoreGrid;
            isUpdatingStoreGrid = true;
            try
            {
                for (int i = gridStores.Rows.Count - 1; i >= 0; i--)
                    if (!gridStores.Rows[i].IsNewRow && RowDispatchId(gridStores.Rows[i]) == d.Id) gridStores.Rows.RemoveAt(i);
                extraDispatches.RemoveAll(e => e.Id == d.Id);
                SizeStoreTable(gridStores.Rows.Count);
            }
            finally { isUpdatingStoreGrid = was; }
            SetDispatchColumnVisible(extraDispatches.Count > 0);
            RecalculateTotalsFromStoreGrid();
            RefreshExtraUi();
        }

        /// <summary>Forgets the added dispatches (their rows are cleared with the table).</summary>
        private void ClearExtras()
        {
            extraDispatches.Clear();
            extrasOwnerId = 0;
            SetDispatchColumnVisible(false);
            RefreshExtraUi();
        }

        /// <summary>Why "Save draft" cannot be used now, or null. A draft belongs to one dispatch.</summary>
        private string? ExtraDraftBlockReason()
            => extraDispatches.Count > 0 ? "Save draft works for one dispatch at a time. Remove the added dispatches (click their ✕) to save a draft, or use Issue Invoice to file all of them." : null;

        private List<Delivery> ShowPickDispatchesDialog(string client, List<(Delivery D, string Text)> items)
        {
            using var f = new Form
            {
                Text = "Add dispatches",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false,
                ClientSize = new Size(Dpi(760), Dpi(430)),
                BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.5F)
            };
            try { ThemeManager.StyleWindowChrome(f); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            var title = new Label { Dock = DockStyle.Top, Height = Dpi(52), Padding = new Padding(Dpi(16), Dpi(12), Dpi(16), 0), Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false,
                Text = $"{client} only. Tick the dispatches to fill in together with this one:" };
            var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.None, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 10F), IntegralHeight = false };
            foreach (var it in items) list.Items.Add(it.Text);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = Dpi(60), BackColor = AppTheme.SurfaceAlt, Padding = new Padding(Dpi(16), Dpi(12), Dpi(16), Dpi(12)) };
            var btnOk = new Button { Text = "Add", DialogResult = DialogResult.OK, Width = Dpi(120), Height = Dpi(34), Dock = DockStyle.Right, Enabled = false, Cursor = Cursors.Hand };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = Dpi(100), Height = Dpi(34), Dock = DockStyle.Right, Cursor = Cursors.Hand };
            var chkAll = new CheckBox { Text = "Select all", AutoSize = true, Dock = DockStyle.Left, ForeColor = AppTheme.TextSecondary, UseMnemonic = false };
            UIThemeHelper.SetButtonRole(btnOk, "primary");
            UIThemeHelper.SetButtonRole(btnCancel, "secondary");
            void Refresh() { int n = list.CheckedIndices.Count; btnOk.Enabled = n > 0; btnOk.Text = n > 1 ? $"Add {n}" : "Add"; }
            list.ItemCheck += (s, e) => f.BeginInvoke(new Action(Refresh));
            chkAll.CheckedChanged += (s, e) => { for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, chkAll.Checked); };
            footer.Controls.AddRange(new Control[] { btnOk, btnCancel, chkAll });
            f.Controls.Add(list); f.Controls.Add(title); f.Controls.Add(footer);
            f.AcceptButton = btnOk; f.CancelButton = btnCancel;
            if (f.ShowDialog(FindForm()) != DialogResult.OK) return new List<Delivery>();
            return list.CheckedIndices.Cast<int>().Select(i => items[i].D).ToList();
        }

        // ------------------------------------------------------------------ saving: one report per dispatch

        /// <summary>
        /// The form's checks already passed. Files one trip report per picked dispatch, each from its own rows (stores, crates, first time in, last time out,
        /// first "received by"), with the gate times, signatories and remarks typed once. Billing then opens with all of the trips queued.
        /// </summary>
        private void SaveSeveralDispatches(Delivery primary)
        {
            var group = new List<Delivery> { primary };
            group.AddRange(extraDispatches);
            string client = (primary.ClientName ?? "").Trim();
            var wrong = group.Where(d => !string.Equals((d.ClientName ?? "").Trim(), client, StringComparison.OrdinalIgnoreCase)).ToList();
            if (wrong.Count > 0)
            {
                MessageBox.Show(FindForm(), $"Only dispatches of {client} can be filed together. Remove: {string.Join(", ", wrong.Select(w => w.DispatchNumber))}.", "Different company", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (var d in group) if (!DispatchGate.RequireCompleted(this, DispatchGate.Fresh(d), "file its trip report")) return;
            // the last chance before anything is saved: dispatches that differ in date, truck or driver get the same gate times and signatories
            var differ = RideDifferenceLines(primary, extraDispatches);
            if (differ.Count > 0)
            {
                string text = "Before filing " + group.Count + " trip reports together:" + Environment.NewLine + Environment.NewLine
                            + string.Join(Environment.NewLine, differ.Select(l => "• " + l)) + Environment.NewLine + Environment.NewLine
                            + RideWarningTail + Environment.NewLine + Environment.NewLine + "File them together anyway?";
                if (MessageBox.Show(FindForm(), text, "Check these dispatches", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            gridStores.EndEdit();

            var plans = new List<(Delivery D, List<DispatchStop> Stops, int Dg, int De, int Rg, int Re, string Arr, string Dep, string Mgr)>();
            foreach (var d in group)
            {
                var rows = RowsOf(d);
                var stops = CollectStopsFromGrid(d);
                int De = rows.Sum(r => RowInt(r, "colLoadE")), Dg = rows.Sum(r => RowInt(r, "colLoadG"));
                int Re = rows.Sum(r => RowInt(r, "colRetE")), Rg = rows.Sum(r => RowInt(r, "colRetG"));
                string arr = rows.Select(r => r.Cells["colIn"].Value?.ToString()?.Trim() ?? "").FirstOrDefault(x => x.Length > 0) ?? txtArrivalTime.Text.Trim();
                string dep = rows.Select(r => r.Cells["colOut"].Value?.ToString()?.Trim() ?? "").LastOrDefault(x => x.Length > 0) ?? txtDepartureTime.Text.Trim();
                string rep = rows.Select(r => r.Cells["colRep"].Value?.ToString()?.Trim() ?? "").FirstOrDefault(x => x.Length > 0) ?? "";
                string mgr;
                if (rep.Length > 0)
                {
                    var sp = NameHelper.SplitFullName(rep);
                    mgr = NameHelper.FormatFullName(string.IsNullOrWhiteSpace(sp.LastName) ? rep : sp.LastName, string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName, sp.MiddleName);
                }
                else
                {
                    string l = txtStoreManagerLastName.Text.Trim(), fn = txtStoreManagerFirstName.Text.Trim();
                    mgr = l.Length == 0 && fn.Length == 0 ? NameHelper.FormatFullName("Store", "Representative", "") : NameHelper.FormatFullName(l, fn, txtStoreManagerMiddleName.Text.Trim());
                }
                plans.Add((d, stops, Dg, De, Rg, Re, arr, dep, mgr));
            }

            // one question for all variances, like the single report's crate variance question
            var variances = plans.Where(p => p.D.Id > 0).Select(p => (p.D, Dispatched: p.D.TotalCrates > 0 ? p.D.TotalCrates : p.D.GCrate + p.D.ECrate, Delivered: p.Dg + p.De))
                                 .Where(v => v.Dispatched > 0 && v.Delivered != v.Dispatched).ToList();
            if (variances.Count > 0)
            {
                string lines = string.Join("\n", variances.Select(v => $"• {v.D.DispatchNumber}: dispatched {v.Dispatched}, delivered {v.Delivered}"));
                var ans = MessageBox.Show(FindForm(), $"Crate variance detected:\n\n{lines}\n\nFile all the trip reports anyway?", "Crate Quantity Reconciliation Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ans != DialogResult.Yes) return;
            }
            if (!EmptiesRecordedOrOk(plans.Sum(p => p.Rg + p.Re), plans.Sum(p => p.Dg + p.De))) { numReturnedGCrates.Focus(); return; }

            var saved = new List<Delivery>();
            try
            {
                foreach (var p in plans)
                {
                    var sp = NameHelper.SplitFullName(p.Mgr);
                    var tr = new TripReport
                    {
                        DeliveryId = p.D.Id,
                        DispatchNumber = p.D.DispatchNumber,
                        ArrivalTime = p.Arr,
                        DepartureTime = p.Dep,
                        DeliveredGCrates = p.Dg, DeliveredECrates = p.De, DeliveredCrates = p.Dg + p.De,
                        ReturnedGCrates = p.Rg, ReturnedECrates = p.Re, ReturnedCrates = p.Rg + p.Re,
                        StoreManagerLastName = sp.LastName, StoreManagerFirstName = sp.FirstName, StoreManagerMiddleName = sp.MiddleName,
                        StoreManagerName = p.Mgr,
                        Remarks = txtRemarks.Text.Trim(),
                        ReportDate = DateTime.Now,
                        TruckTimeIn = txtTruckTimeIn?.Text?.Trim() ?? "",
                        TruckTimeOut = txtTruckTimeOut?.Text?.Trim() ?? "",
                        DtrApprovedBy = txtDtrApprovedBy?.Text?.Trim() ?? "",
                        ReturnCheckedBy = txtReturnCheckedBy?.Text?.Trim() ?? "",
                        DtrPreparedBy = txtDtrPreparedBy?.Text?.Trim() ?? "",
                        DrPreparedBy = txtDrPreparedBy?.Text?.Trim() ?? "",
                        DrCheckedBy = txtDrCheckedBy?.Text?.Trim() ?? "",
                        GuardName = txtGuardName?.Text?.Trim() ?? "",
                        Status = TripReportStatus.Completed
                    };
                    if (p.Stops.Count > 0) MJdatabase.SaveDispatchStops(p.D.Id, p.Stops);
                    MJdatabase.SaveTripReport(tr);
                    SaveTripType(tr.DeliveryId, tr.SpecialTripId);
                    saved.Add(p.D);
                }
            }
            catch (Exception ex)
            {
                ErrLog.Error("TripReportView.SaveSeveral", ex);
                var notSaved = group.Where(d => !saved.Any(s => s.Id == d.Id)).Select(d => d.DispatchNumber).ToList();
                MessageBox.Show(FindForm(), $"Saved: {(saved.Count == 0 ? "none" : string.Join(", ", saved.Select(s => s.DispatchNumber)))}.\nNot saved: {string.Join(", ", notSaved)}.\n\n{ex.Message}\n\nThe ones not saved are back in the waiting list.", "Trip reports", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ClearFields(); LoadData(); SetFormVisibility(false);
                return;
            }

            MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            (Delivery D, int Missing)? incident = null;
            if (Features.On(Features.Incidents))
                foreach (var p in plans)
                {
                    int dispatched = p.D.TotalCrates > 0 ? p.D.TotalCrates : p.D.GCrate + p.D.ECrate;
                    int missing = dispatched - (p.Dg + p.De);
                    if (p.D.Id > 0 && missing > 0 && incident == null)
                    {
                        var ans = MessageBox.Show(FindForm(), $"{p.D.DispatchNumber} was completed with {missing} missing/damaged crate(s).\n\nLog an Incident & Damage Report for it now?", "Log Incident Report", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ans == DialogResult.Yes) incident = (p.D, missing);
                    }
                }
            ClearFields();
            LoadData();
            SetFormVisibility(false);
            ToastHelper.ShowToast(this, $"{saved.Count} trip reports saved: {string.Join(", ", saved.Select(s => s.DispatchNumber))}.", ToastType.Success);
            if (incident != null) { OnLogIncidentRequested?.Invoke(incident.Value.D.Id, incident.Value.D.DispatchNumber, incident.Value.Missing, DateTime.Now); return; }
            MainForm.Instance?.OpenBillingFromTripReports(saved);   // the same queue as "Issue Invoice (n selected)": one invoice per trip, same client
        }
    }
}
