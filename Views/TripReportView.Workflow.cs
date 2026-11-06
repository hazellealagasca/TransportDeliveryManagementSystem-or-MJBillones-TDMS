using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6 workflow on Trip Reports: a 4-step bar (store times &amp; signatures → gate times → verification → issue invoice),
    /// one text overview of the trip instead of the stacked banners and crate panels, an Active / History switch
    /// (Invoiced reports move to History), and "Issue Invoice (Selected)" for several completed reports at once.
    /// </summary>
    public partial class TripReportView
    {
        private TripStepper? tripStepper;
        private TextBox? txtTripOverview;
        private Button btnSaveDraft = null!;
        private Button btnIssueExisting = null!;
        private bool issueAfterUpdate;
        private bool showHistory;
        private Button? btnViewActive, btnViewHistory, btnIssueSelected;
        private List<Truck>? overviewTrucks;

        // ------------------------------------------------------------------ Form: buttons, step bar, overview

        /// <summary>Adds "Save draft" and "Issue Invoice" (for a saved report) next to the form's existing buttons.</summary>
        private void InitWorkflowButtons(FlowLayoutPanel actions)
        {
            btnSaveDraft = new Button { Text = "💾  Save draft", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSaveDraft.Click += (s, e) => SaveNewReport(asDraft: true);
            btnIssueExisting = new Button { Text = "🧾  Issue Invoice", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = false };
            btnIssueExisting.Click += (s, e) =>
            {
                issueAfterUpdate = true;
                try { BtnUpdate_Click(s, e); }
                finally { issueAfterUpdate = false; }
            };
            foreach (var b in new[] { btnSaveDraft, btnIssueExisting })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI Semibold", 9.5F);
                b.Width = Dpi(180);
            }
            UIThemeHelper.SetButtonRole(btnSaveDraft, "secondary");
            UIThemeHelper.SetButtonRole(btnIssueExisting, "primary");
            actions.Controls.Add(btnIssueExisting);
            actions.Controls.Add(btnSaveDraft);
            actions.Controls.SetChildIndex(btnIssueExisting, 1);
            actions.Controls.SetChildIndex(btnSaveDraft, 3);
        }

        private void ApplyWorkflowFormMode()
        {
            if (btnSaveDraft == null) return;
            btnSaveDraft.Visible = !isEditingExistingReport;
            btnIssueExisting.Visible = isEditingExistingReport && selectedTripReport != null && !RowMenu.Is(selectedTripReport.Status, TripReportStatus.Invoiced);
            if (isEditingExistingReport && selectedTripReport != null)
                lblFormHeader.Text = $"Trip Report  ·  {selectedTripReport.TripReportNumber}  ·  {selectedTripReport.Status}";
            UpdateTripStepper();
        }

        private TripStepper BuildTripStepper()
        {
            tripStepper = new TripStepper { Dock = DockStyle.Top, Height = Dpi(58), Margin = new Padding(0) };
            return tripStepper;
        }

        private TextBox BuildTripOverview()
        {
            txtTripOverview = new TextBox
            {
                Multiline = true, ReadOnly = true, WordWrap = false, ScrollBars = ScrollBars.None, TabStop = false,
                BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.Canvas, ForeColor = AppTheme.TextPrimary,
                Font = new Font("Consolas", 9.5F), Height = Dpi(118), Margin = new Padding(0, Dpi(6), 0, 0),
                Text = "Pick a dispatch order above: its trip and crate overview shows here."
            };
            return txtTripOverview;
        }

        /// <summary>The one text overview of the trip: numbers, route, truck, crew and dispatched crates.</summary>
        private void ShowTripOverview(Delivery d, string refNum, string routeTitle, int storeCount, string incidentNote)
        {
            if (txtTripOverview == null) return;
            try { overviewTrucks ??= MJdatabase.GetTrucks(); } catch (Exception ex) { ErrLog.Swallowed(ex); overviewTrucks = new List<Truck>(); }
            var trk = overviewTrucks.FirstOrDefault(t => t.Id == d.TruckId);
            string plate = trk?.PlateNumber ?? (string.IsNullOrWhiteSpace(d.TruckInfo) ? "—" : d.TruckInfo);
            string tons = trk != null ? MJdatabase.TonnageLabel(trk) : "—";
            string helper = d.AideDisplay;
            var sb = new StringBuilder();
            sb.AppendLine($"Dispatch No. : {d.DispatchNumber,-22} Ref No. : {refNum}");
            sb.AppendLine($"Date         : {d.DispatchDate:MMM d, yyyy}{"",-10} Departure : {d.DepartureTime}");
            sb.AppendLine($"Client       : {d.ClientName}");
            sb.AppendLine($"Route        : {routeTitle}   ·   Destination : {d.Destination}   ·   Stores : {storeCount}");
            sb.AppendLine($"Truck        : {plate} ({tons})   ·   Driver : {d.DriverName}   ·   Helper : {helper}");
            sb.Append($"Crates       : G-Crates {d.GCrate:N0}   ·   E-Crates {d.ECrate:N0}   ·   Total {d.GCrate + d.ECrate:N0} dispatched");
            if (incidentNote.Length > 0) sb.Append(Environment.NewLine + "Incident     : " + incidentNote);
            txtTripOverview.Text = sb.ToString();
            int lines = txtTripOverview.Lines.Length;
            txtTripOverview.Height = Dpi(10) + lines * TextRenderer.MeasureText("X", txtTripOverview.Font).Height;
            txtTripOverview.ForeColor = incidentNote.Length > 0 ? AppTheme.DangerText : AppTheme.TextPrimary;
            UpdateTripStepper();
        }

        // ------------------------------------------------------------------ The four steps

        private static readonly Regex Hhmm = new Regex(@"^([01]?\d|2[0-3]):?[0-5]\d$");
        private static bool Filled(TextBox? t) => t != null && t.Text.Trim().Length > 0;

        /// <summary>Step 1: every store has its time in, time out and who received the delivery.</summary>
        private bool StepStoresDone()
        {
            if (gridStores == null || gridStores.Rows.Count == 0) return false;
            foreach (DataGridViewRow r in gridStores.Rows)
            {
                if (r.IsNewRow) continue;
                string V(string col) => gridStores.Columns.Contains(col) ? (r.Cells[col].Value?.ToString() ?? "").Trim() : "x";
                if (V("colIn").Length == 0 || V("colOut").Length == 0 || V("colRep").Length == 0) return false;
            }
            return true;
        }
        /// <summary>Step 2: the truck's time in and time out at the plant gate (24-hour).</summary>
        private bool StepGateDone() => txtTruckTimeIn != null && txtTruckTimeOut != null
            && Hhmm.IsMatch(txtTruckTimeIn.Text.Trim()) && Hhmm.IsMatch(txtTruckTimeOut.Text.Trim());
        /// <summary>Step 3: the people who checked, prepared and approved the trip signed it.</summary>
        private bool StepVerificationDone() => Filled(txtDtrApprovedBy) && Filled(txtDtrPreparedBy) && Filled(txtDrPreparedBy) && Filled(txtDrCheckedBy) && Filled(txtGuardName);

        private void UpdateTripStepper()
        {
            if (tripStepper == null) return;
            bool invoiced = isEditingExistingReport && selectedTripReport != null && RowMenu.Is(selectedTripReport.Status, TripReportStatus.Invoiced);
            tripStepper.SetSteps(StepStoresDone(), StepGateDone(), StepVerificationDone(), invoiced);
        }

        /// <summary>Re-draws the step bar while the person types (times, names, store cells).</summary>
        private void WireStepperRefresh()
        {
            foreach (var t in new[] { txtTruckTimeIn, txtTruckTimeOut, txtDtrApprovedBy, txtDtrPreparedBy, txtDrPreparedBy, txtDrCheckedBy, txtGuardName, txtReturnCheckedBy })
                if (t != null) t.TextChanged += (s, e) => UpdateTripStepper();
            if (gridStores != null)
            {
                gridStores.CellValueChanged += (s, e) => UpdateTripStepper();
                gridStores.RowsAdded += (s, e) => UpdateTripStepper();
                gridStores.RowsRemoved += (s, e) => UpdateTripStepper();
            }
        }

        // ------------------------------------------------------------------ List: Active / History and Issue Invoice (Selected)

        /// <summary>Active = Draft and Completed reports (the work still to bill); History = Invoiced reports.</summary>
        private IEnumerable<TripReport> ApplyActiveOrHistory(IEnumerable<TripReport> list)
            => showHistory ? list.Where(t => RowMenu.Is(t.Status, TripReportStatus.Invoiced)) : list.Where(t => !RowMenu.Is(t.Status, TripReportStatus.Invoiced));

        private void InitActiveHistoryBar()
        {
            Button Seg(string text, bool active)
            {
                var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(Dpi(96), Dpi(30)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, Dpi(4), Dpi(4), Dpi(4)) };
                UIThemeHelper.SetButtonRole(b, active ? "primary" : "secondary");
                return b;
            }
            btnViewActive = Seg("Active", true);
            btnViewHistory = Seg("History (invoiced)", false);
            btnViewActive.Click += (s, e) => SetHistoryMode(false);
            btnViewHistory.Click += (s, e) => SetHistoryMode(true);
            btnIssueSelected = new Button { Text = "🧾 Issue Invoice (Selected)", AutoSize = true, MinimumSize = new Size(Dpi(150), Dpi(30)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(Dpi(8), Dpi(4), Dpi(4), Dpi(4)) };
            UIThemeHelper.SetButtonRole(btnIssueSelected, "primary");
            btnIssueSelected.Click += (s, e) => IssueInvoiceForSelected();
            pnlGridActionsFlow.Controls.Add(btnViewActive);
            pnlGridActionsFlow.Controls.Add(btnViewHistory);
            pnlGridActionsFlow.Controls.Add(btnIssueSelected);
            pnlGridActionsFlow.Controls.SetChildIndex(btnIssueSelected, 0);
            pnlGridActionsFlow.Controls.SetChildIndex(btnViewHistory, 0);
            pnlGridActionsFlow.Controls.SetChildIndex(btnViewActive, 0);
            gridTripReports.MultiSelect = true;
            gridTripReports.SelectionChanged += (s, e) => UpdateIssueSelectedButton();
            if (!gridTripReports.Columns.Contains("colStatus"))
                gridTripReports.Columns.Insert(Math.Min(1, gridTripReports.Columns.Count), new DataGridViewTextBoxColumn { Name = "colStatus", DataPropertyName = "Status", HeaderText = "Status", FillWeight = 70, MinimumWidth = Dpi(80), ReadOnly = true });
        }

        private void SetHistoryMode(bool history)
        {
            showHistory = history;
            if (btnViewActive != null) UIThemeHelper.SetButtonRole(btnViewActive, history ? "secondary" : "primary");
            if (btnViewHistory != null) UIThemeHelper.SetButtonRole(btnViewHistory, history ? "primary" : "secondary");
            if (btnIssueSelected != null) btnIssueSelected.Visible = !history;
            FilterGridData();
        }

        private List<TripReport> SelectedReports() => gridTripReports.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem).OfType<TripReport>().ToList();

        private void UpdateIssueSelectedButton()
        {
            if (btnIssueSelected == null) return;
            var sel = SelectedReports();
            btnIssueSelected.Text = sel.Count > 1 ? $"🧾 Issue Invoice ({sel.Count} selected)" : "🧾 Issue Invoice (Selected)";
            btnIssueSelected.Enabled = sel.Count > 0;
        }

        /// <summary>
        /// Checks the selected reports (all Completed, all of one client) and opens Billing on each trip in turn, pre-filled
        /// with the dispatch, client, destination and rate. Nothing is created until each invoice is saved.
        /// </summary>
        private void IssueInvoiceForSelected()
        {
            var sel = SelectedReports();
            if (sel.Count == 0) { QuietBox.Show("Select one or more completed trip reports first (Shift/Ctrl-click for several).", "Issue Invoice", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var problems = new List<string>();
            var trips = new List<Delivery>();
            var clients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in sel)
            {
                Delivery? d = t.SpecialTripId > 0
                    ? (MJdatabase.GetSpecialTripById(t.SpecialTripId) is SpecialTrip st ? BillingView.SpecialTripStandIn(st) : null)
                    : DeliveryOf(t);
                if (RowMenu.Is(t.Status, TripReportStatus.Draft)) { problems.Add($"{t.TripReportNumber}: still a Draft — open it and finish the steps first"); continue; }
                if (RowMenu.Is(t.Status, TripReportStatus.Invoiced)) { problems.Add($"{t.TripReportNumber}: already invoiced"); continue; }
                if (d == null) { problems.Add($"{t.TripReportNumber}: its dispatch was not found"); continue; }
                clients.Add((d.ClientName ?? "").Trim());
                trips.Add(d);
            }
            if (clients.Count > 1) problems.Add($"The selected trips are for {clients.Count} clients ({string.Join(", ", clients)}). Select one client's trips at a time.");
            if (problems.Count > 0)
            {
                MessageBox.Show(FindForm(), "These trip reports can't be invoiced together yet:\n\n• " + string.Join("\n• ", problems), "Issue Invoice (Selected)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MainForm.Instance?.OpenBillingFromTripReports(trips);
        }
    }

    /// <summary>The 4-step progress bar across the top of the trip report form.</summary>
    internal sealed class TripStepper : Control
    {
        private static readonly string[] Titles = { "Store timestamps & signatures", "Gate times (time in / out)", "Verification & signatory check", "Issue invoice" };
        private readonly bool[] done = new bool[4];

        public TripStepper()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = AppTheme.Surface;
        }

        public void SetSteps(bool stores, bool gate, bool verification, bool invoiced)
        {
            done[0] = stores; done[1] = gate; done[2] = verification; done[3] = invoiced;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float dpi = DeviceDpi / 96f;
            int n = Titles.Length;
            float w = Width / (float)n;
            int current = Array.FindIndex(done, x => !x);
            using var lineDone = new Pen(AppTheme.Success, 3 * dpi);
            using var lineTodo = new Pen(AppTheme.Border, 3 * dpi);
            using var font = new Font("Segoe UI Semibold", 8.75F);
            using var numFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            float r = 11 * dpi, cy = 16 * dpi;
            for (int i = 0; i < n; i++)
            {
                float cx = w * i + w / 2;
                if (i < n - 1) g.DrawLine(done[i] ? lineDone : lineTodo, cx + r, cy, cx + w - r, cy);
                Color fill = done[i] ? AppTheme.Success : i == current ? AppTheme.Accent : AppTheme.Canvas;
                Color fore = done[i] || i == current ? Color.White : AppTheme.TextMuted;
                using (var b = new SolidBrush(fill)) g.FillEllipse(b, cx - r, cy - r, 2 * r, 2 * r);
                using (var p = new Pen(done[i] ? AppTheme.Success : AppTheme.Border, 1.5f * dpi)) g.DrawEllipse(p, cx - r, cy - r, 2 * r, 2 * r);
                TextRenderer.DrawText(g, done[i] ? "✓" : (i + 1).ToString(), numFont, new Rectangle((int)(cx - r), (int)(cy - r), (int)(2 * r), (int)(2 * r)), fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, $"Step {i + 1}: {Titles[i]}", font, new Rectangle((int)(w * i), (int)(cy + r + 3 * dpi), (int)w, (int)(20 * dpi)),
                    done[i] ? AppTheme.SuccessText : i == current ? AppTheme.TextPrimary : AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
