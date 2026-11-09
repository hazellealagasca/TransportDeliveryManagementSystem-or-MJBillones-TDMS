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
    /// Full-page Trip Report form: the form replaces the list (same tab), with a back button at the top left,
    /// numbered section cards, a taller store table and a crew pay section for the driver and aides.
    /// </summary>
    public partial class TripReportView
    {
        private const int FormContentMaxWidth = 1100;
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private bool isEditingExistingReport = false;
        private Panel? pnlHeaderSpacer;
        private FlowLayoutPanel? flowFormActions;
        private Button? btnPrintFromForm;
        // ------------------------------------------------------------------ Page layout
        private void BuildTripReportCards()
        {
            pnlFormBody.SuspendLayout();
            pnlFormBody.Controls.Clear();
            lblDel.AutoSize = false;
            lblDel.Height = Dpi(26);
            lblDel.Font = new Font("Segoe UI Semibold", 9F);
            lblDel.ForeColor = AppTheme.TextSecondary;
            lblDel.TextAlign = ContentAlignment.BottomLeft;
            cbDelivery.Font = new Font("Segoe UI", 10F);
            lblRem.AutoSize = false;
            lblRem.Height = Dpi(30);
            lblRem.Font = new Font("Segoe UI Semibold", 9F);
            lblRem.ForeColor = AppTheme.TextSecondary;
            lblRem.TextAlign = ContentAlignment.BottomLeft;
            txtRemarks.Height = Dpi(64);
            txtRemarks.Font = new Font("Segoe UI", 10F);
            foreach (var l in new[] { lblDelGCrate, lblDelECrate, lblRetGCrate, lblRetECrate })
            {
                l.Font = new Font("Segoe UI Semibold", 9F);
                l.ForeColor = AppTheme.TextSecondary;
            }
            foreach (var n in new[] { numDeliveredGCrates, numDeliveredECrates, numReturnedGCrates, numReturnedECrates })
            {
                n.Font = new Font("Segoe UI", 10F);
                // Totals add up from the stores: they can't be typed or spun here (open a store to change its returned crates).
                n.ReadOnly = true;
                n.InterceptArrowKeys = false;
                n.TabStop = false;
                n.Increment = 0;
                n.MouseWheel += (s, e) => { if (e is HandledMouseEventArgs h) h.Handled = true; };
                // Show the total in a plain read-only box (a locked number box still drew its arrow area).
                var shown = new TextBox { ReadOnly = true, TabStop = false, Font = n.Font, Dock = n.Dock, Anchor = n.Anchor, Margin = n.Margin, Text = n.Value.ToString("N0"), BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary };
                var numBox = n;
                numBox.ValueChanged += (s, e) => shown.Text = numBox.Value.ToString("N0");
                var parent = n.Parent;
                if (parent is TableLayoutPanel tlp)
                {
                    var pos = tlp.GetPositionFromControl(n);
                    tlp.Controls.Remove(n);
                    tlp.Controls.Add(shown, pos.Column, pos.Row);
                }
                else if (parent != null)
                {
                    int idx = parent.Controls.GetChildIndex(n);
                    parent.Controls.Add(shown);
                    parent.Controls.SetChildIndex(shown, idx);
                    n.Visible = false;
                }
            }
            tblDeliveredCrates.Dock = DockStyle.Fill;
            tblReturnedCrates.Dock = DockStyle.Fill;
            tblDeliveredCrates.Margin = new Padding(0, 0, Dpi(12), 0);
            tblReturnedCrates.Margin = new Padding(Dpi(12), 0, 0, 0);
            var tblCrateTotals = new TableLayoutPanel
            {
                Height = Dpi(64),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0)
            };
            tblCrateTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrateTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrateTotals.Controls.Add(tblDeliveredCrates, 0, 0);
            tblCrateTotals.Controls.Add(tblReturnedCrates, 1, 0);
            btnLogIncident.Height = Dpi(40);
            btnLogIncident.Font = new Font("Segoe UI Semibold", 9.5F);
            var card1 = CreateSectionCard(1, "Dispatch order", "Pick the delivery this report closes out. Its route, truck and crew load automatically.",
                lblDel, BuildPickFilterRow(), cbDelivery, BuildExtraDispatchesRow(), BuildTripOverview(), BuildTripTypeRow());
            tripCrateSummary = new TripCrateSummaryPanel();
            lblDeliveredCrateTotals = new Label
            {
                AutoSize = false, Height = Dpi(34), Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary,
                BackColor = AppTheme.Surface, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false
            };
            UpdateDeliveredCrateTotals();
            var card2 = CreateSectionCard(2, "Stores, times & signatures", "Each store's arrival and departure time, who received it, its DR numbers and the empties that came back.",
                pnlMultiDropInfo, lblDeliveredCrateTotals, CreateGap(Dpi(8)), btnLogIncident, tblTimes, tblManagerNames);
            _ = tblCrateTotals; // Nov 6 workflow: no delivered-crate totals or crate cards on the form (the values are still worked out)
            // Nov 6 workflow: crew pay is internal and is recorded in Resources → Crew Trip History, not on the trip report.
            var card4 = CreateSectionCard(4, "Plant gate & sign-off", "Truck times at the plant, the people who signed, and any remarks.",
                pnlTripFooter, lblRem, txtRemarks);
            tripAttachments = new AttachmentPanel("Proof of delivery (POD)") { Height = Dpi(250) };
            tripAttachments.AttachmentsChanged += (s, e) => FilterGridData();
            var card5 = CreateSectionCard(5, "Attachments", "Proof of delivery, receipts, trip sheets and photos for this trip. Drop files in or click Add.", tripAttachments);
            pnlFormBody.Controls.Add(card5);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card4);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card2);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card1);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(BuildTripStepper()); // the 4-step bar across the top
            pnlFormBody.ResumeLayout(true);
            WireStepperRefresh();
        }
        private Panel CreateGap(int h) => new Panel { Height = h, BackColor = AppTheme.Surface };
        private Panel CreateSectionSpacer() => new Panel { Dock = DockStyle.Top, Height = Dpi(16), BackColor = AppTheme.Canvas };
        private Panel CreateSectionCard(int step, string title, string subtitle, params Control?[] contentTopToBottom)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface,
                Padding = new Padding(Dpi(24), Dpi(18), Dpi(24), Dpi(22)),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Resize += (s, e) => card.Invalidate();
            for (int i = contentTopToBottom.Length - 1; i >= 0; i--)
            {
                var c = contentTopToBottom[i];
                if (c == null) continue;
                c.Dock = DockStyle.Top;
                card.Controls.Add(c);
            }
            var header = new Panel { Dock = DockStyle.Top, Height = Dpi(56), BackColor = AppTheme.Surface };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                int d = Dpi(30);
                var circle = new Rectangle(0, Dpi(2), d, d);
                using (var b = new SolidBrush(AppTheme.AccentSoft)) g.FillEllipse(b, circle);
                using (var nf = new Font("Segoe UI Semibold", 10F))
                    TextRenderer.DrawText(g, step.ToString(), nf, circle, AppTheme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                using (var tf = new Font("Segoe UI Semibold", 12F))
                    TextRenderer.DrawText(g, title, tf, new Point(d + Dpi(12), 0), AppTheme.TextPrimary, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                using (var sf = new Font("Segoe UI", 9F))
                    TextRenderer.DrawText(g, subtitle, sf, new Rectangle(d + Dpi(12), Dpi(24), header.Width - d - Dpi(12), Dpi(22)), AppTheme.TextMuted, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                using var pen = new Pen(AppTheme.Border);
                g.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            header.Resize += (s, e) => header.Invalidate();
            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(10), BackColor = AppTheme.Surface };
            card.Controls.Add(gap);
            card.Controls.Add(header);
            return card;
        }
        private void StyleFullPageForm()
        {
            foreach (Control c in pnlForm.Controls)
            {
                if (c.Name == "pnlFormResizeGrip") c.Visible = false;
            }
            pnlForm.BackColor = AppTheme.Canvas;
            pnlFormBody.BackColor = AppTheme.Canvas;
            pnlFormActionsBar.BackColor = AppTheme.Surface;
            pnlFormHeaderBar.BackColor = AppTheme.Canvas;
            pnlFormHeaderBar.Height = Dpi(58);
            pnlFormHeaderBar.Padding = new Padding(0, Dpi(6), 0, Dpi(12));
            btnCloseForm.Text = "←  Back to trip reports";
            btnCloseForm.AutoSize = false;
            btnCloseForm.MinimumSize = Size.Empty;
            btnCloseForm.Padding = new Padding(0);
            btnCloseForm.Dock = DockStyle.Left;
            btnCloseForm.Width = Dpi(190);
            btnCloseForm.Font = new Font("Segoe UI Semibold", 9.5F);
            UIThemeHelper.SetButtonRole(btnCloseForm, "secondary");
            btnCloseForm.ForeColor = AppTheme.TextPrimary;
            pnlHeaderSpacer ??= new Panel { Dock = DockStyle.Left, Width = Dpi(16), BackColor = AppTheme.Canvas };
            if (!pnlFormHeaderBar.Controls.Contains(pnlHeaderSpacer)) pnlFormHeaderBar.Controls.Add(pnlHeaderSpacer);
            lblFormHeader.AutoSize = false;
            lblFormHeader.AutoEllipsis = true;
            lblFormHeader.Dock = DockStyle.Fill;
            lblFormHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblFormHeader.Font = new Font("Segoe UI Semibold", 15F);
            lblFormHeader.ForeColor = AppTheme.TextPrimary;
            lblFormHeader.BackColor = AppTheme.Canvas;
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(pnlHeaderSpacer, 1);
            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 2);
            pnlFormBody.Padding = new Padding(0, 0, 0, Dpi(16));
            pnlForm.Resize -= PnlForm_ResizeCenter;
            pnlForm.Resize += PnlForm_ResizeCenter;
            BuildFormActionBar();
            CenterFormContent();
        }
        private void PnlForm_ResizeCenter(object? sender, EventArgs e) => CenterFormContent();
        private void CenterFormContent()
        {
            if (pnlForm == null || pnlForm.Width <= 0) return;
            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);
            pnlFormHeaderBar.Padding = new Padding(side, pnlFormHeaderBar.Padding.Top, side, pnlFormHeaderBar.Padding.Bottom);
            pnlFormBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(16));
            pnlFormActionsBar.Padding = new Padding(side, Dpi(12), side, Dpi(12));
        }
        private void BuildFormActionBar()
        {
            if (flowFormActions != null) return;
            flowFormActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = AppTheme.Surface,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblUpdateDelete.Controls.Remove(btnSave);
            tblUpdateDelete.Controls.Remove(btnUpdate);
            pnlFormActionsBar.Controls.Remove(btnClear);
            tblUpdateDelete.Visible = false;
            btnPrintFromForm = new Button { Text = "🖨  Print trip report", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPrintFromForm.Click += (s, e) => PrintSelectedTripReport();
            btnSave.Text = "➡  Proceed to SOA";
            btnUpdate.Text = "💾  Save changes";
            btnClear.Text = "Clear form";
            foreach (var b in new[] { btnSave, btnUpdate, btnPrintFromForm, btnClear })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI Semibold", 9.5F);
                b.Width = Dpi(180);
            }
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnUpdate, "primary");
            UIThemeHelper.SetButtonRole(btnPrintFromForm, "secondary");
            UIThemeHelper.SetButtonRole(btnClear, "secondary");
            // Right-to-left: the main action sits at the far right.
            flowFormActions.Controls.Add(btnSave);
            flowFormActions.Controls.Add(btnUpdate);
            flowFormActions.Controls.Add(btnPrintFromForm);
            flowFormActions.Controls.Add(btnClear);
            InitWorkflowButtons(flowFormActions);
            pnlFormActionsBar.Controls.Add(flowFormActions);
            flowFormActions.BringToFront();
            pnlFormActionsBar.Height = Dpi(66);
        }
        // ------------------------------------------------------------------ New / edit modes
        // Nov 6: unsaved-typing tracking for the add / edit form (see FormGuard).
        private FormDirtyTracker? formDirty;
        private readonly KeptDraftState keep = new KeptDraftState();
        public bool FormIsOpenAndDirty => pnlForm.Visible && (formDirty?.IsDirty ?? false);
        public void DiscardAndCloseForm() => SetFormVisibility(false);
        public bool HasKeptDraft => keep.Kept;
        public void KeepAndCloseForm()
        {
            // Nov 9: "Keep for later": hide the form but leave the typing in the fields; it reopens when the person returns.
            keep.Keeping = true;
            try { SetFormVisibility(false); } finally { keep.Keeping = false; }
            keep.MarkKept(this, btnToggleForm, () => keep.Resume(this, SetFormVisibility));
        }
        private void SetFormVisibility(bool visible)
        {
            // Nov 6: a form is blank whenever it is opened or left.
            if (!visible && pnlForm.Visible && IsHandleCreated && !keep.Keeping) ClearFields();
            if (visible) formDirty ??= new FormDirtyTracker(pnlForm);
            if (!keep.Keeping && !(visible && keep.Resuming)) formDirty?.Reset();
            if (visible) keep.Restored(btnToggleForm);
            tblMasterContent.SuspendLayout();
            pnlForm.Visible = visible;
            pnlGridCard.Visible = !visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
                tblMasterContent.ColumnStyles[0].Width = visible ? 100F : 0F;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].SizeType = visible ? SizeType.Absolute : SizeType.Percent;
                tblMasterContent.ColumnStyles[1].Width = visible ? 0F : 100F;
            }
            pnlForm.Margin = new Padding(0);
            tblMasterContent.ResumeLayout(true);
            // Closing the form: the next new report may offer a waiting draft again.
            if (!visible) draftAnswered = false;
            if (visible)
            {
                CenterFormContent();
                pnlFormBody.AutoScrollPosition = new Point(0, 0);
            }
            if (gridTripReports.Columns.Count > 0)
            {
                gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New trip report";
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }
        private void OpenNewTripReport()
        {
            if (keep.Kept) { keep.Resume(this, SetFormVisibility); return; } // a kept draft comes back as it was left, never blank
            isEditingExistingReport = false;
            ClearFields();
            ApplyFormMode();
            SetFormVisibility(true);
            LoadSelectedDeliveryIntoForm();
            cbDelivery.Focus();
        }
        private void OpenEditTripReport(TripReport? tr)
        {
            if (tr == null) return;
            if (selectedTripReport != tr)
            {
                for (int i = 0; i < gridTripReports.Rows.Count; i++)
                {
                    if (gridTripReports.Rows[i].DataBoundItem == tr)
                    {
                        gridTripReports.ClearSelection();
                        gridTripReports.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            if (selectedTripReport != tr)
            {
                // Nov 9: the list's filters (date, client…) may hide this report; open it anyway instead of doing nothing.
                selectedTripReport = tr;
                BindSelectedReportToForm();
            }
            isEditingExistingReport = true;
            ApplyFormMode();
            SetFormVisibility(true);
            SyncAttachmentOwner();
        }
        private void ApplyFormMode()
        {
            btnSave.Visible = !isEditingExistingReport;
            btnUpdate.Visible = isEditingExistingReport;
            btnClear.Visible = !isEditingExistingReport;
            cbDelivery.Enabled = !isEditingExistingReport;
            SetPickControlsEnabled(!isEditingExistingReport);
            RefreshExtraUi();
            lblFormHeader.Text = isEditingExistingReport && selectedTripReport != null
                ? $"Trip Report  ·  {selectedTripReport.TripReportNumber}"
                : "New Trip Report";
            ApplyWorkflowFormMode();
        }
        private Delivery? GetSelectedDelivery()
        {
            if (cbDelivery.SelectedIndex < 0) return null;
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            return allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
        }
        private AttachmentPanel? tripAttachments;
        private TripCrateSummaryPanel? tripCrateSummary;
        private Label? lblDeliveredCrateTotals;
        /// <summary>"Delivered crates: G crates x + E crates y = z total crates", summed from the (read-only) Delivered G / Delivered E columns of the store table.</summary>
        private void UpdateDeliveredCrateTotals()
        {
            if (lblDeliveredCrateTotals == null) return;
            int dg = 0, de = 0;
            if (gridStores != null && gridStores.Columns.Contains("colLoadG") && gridStores.Columns.Contains("colLoadE"))
                foreach (DataGridViewRow r in gridStores.Rows)
                {
                    if (r.IsNewRow) continue;
                    if (int.TryParse(r.Cells["colLoadG"].Value?.ToString(), out int a)) dg += Math.Max(0, a);
                    if (int.TryParse(r.Cells["colLoadE"].Value?.ToString(), out int b)) de += Math.Max(0, b);
                }
            lblDeliveredCrateTotals.Text = $"Delivered crates:  G crates {dg:N0}  +  E crates {de:N0}  =  {dg + de:N0} total crates";
        }
        private int tripIncidentDeliveryId = -1, tripIncidentDamaged;
        private string tripIncidentNote = "";
        private List<ClientStore> tripRouteStores = new List<ClientStore>();
        private string tripRouteStoresClient = "\0";
        /// <summary>The trip's dispatch: the one picked, or the one behind the report being edited.</summary>
        private Delivery? TripDelivery()
        {
            var d = GetSelectedDelivery();
            if (d == null && isEditingExistingReport && selectedTripReport != null)
                d = allDeliveries.FirstOrDefault(x => x.Id == selectedTripReport.DeliveryId || string.Equals(x.DispatchNumber, selectedTripReport.DispatchNumber, StringComparison.OrdinalIgnoreCase));
            return d;
        }
        /// <summary>
        /// Refreshes the Trip &amp; Crate Summary Overview from the store rows: total delivered, missing (crates not
        /// returned, per crate type and store), damaged (incident reports of this trip), intact, the routes covered and
        /// the stores with problems. Called whenever a store row is added, changed or removed.
        /// </summary>
        private void UpdateTripCrateSummary()
        {
            UpdateDeliveredCrateTotals();
            if (tripCrateSummary == null || gridStores == null) return;
            try
            {
                var f = new TripCrateFigures();
                var d = TripDelivery();
                // Damaged crates come from the trip's incident reports (read again only when the trip changes).
                int delId = d?.Id ?? 0;
                if (delId != tripIncidentDeliveryId)
                {
                    tripIncidentDeliveryId = delId;
                    tripIncidentDamaged = 0; tripIncidentNote = "";
                    if (delId > 0)
                    {
                        var incs = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == delId && !string.Equals(ir.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
                        tripIncidentDamaged = incs.Sum(i => Math.Max(0, i.DamagedQuantity));
                        if (tripIncidentDamaged > 0)
                            tripIncidentNote = $"Damaged: {tripIncidentDamaged:N0} crate{(tripIncidentDamaged == 1 ? "" : "s")} (incident reports for the whole trip: {string.Join("; ", incs.Where(i => i.DamagedQuantity > 0).Select(i => $"{i.Cause} {i.DamagedQuantity:N0}"))})";
                    }
                }
                string client = (d?.ClientName ?? "").Trim();
                if (!string.Equals(client, tripRouteStoresClient, StringComparison.OrdinalIgnoreCase))
                {
                    tripRouteStoresClient = client;
                    try { tripRouteStores = client.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(client, includeArchived: true); }
                    catch (Exception ex) { ErrLog.Swallowed(ex); tripRouteStores = new List<ClientStore>(); }
                }
                var routes = new List<string>();
                int N(DataGridViewRow r, string col) => gridStores.Columns.Contains(col) && int.TryParse(r.Cells[col].Value?.ToString(), out int n) ? Math.Max(0, n) : 0;
                string T(DataGridViewRow r, string col) => gridStores.Columns.Contains(col) ? (r.Cells[col].Value?.ToString() ?? "").Trim() : "";
                foreach (DataGridViewRow row in gridStores.Rows)
                {
                    if (row.IsNewRow) continue;
                    string code = T(row, "colCode"), name = T(row, "colStore");
                    if (code.Length == 0 && name.Length == 0) continue;
                    f.Stores++;
                    int le = N(row, "colLoadE"), lg = N(row, "colLoadG"), re = N(row, "colRetE"), rg = N(row, "colRetG");
                    f.TotalCrates += le + lg;
                    f.ReturnedTotal += re + rg;
                    f.Drops += N(row, "colRem");
                    int shortG = Math.Max(0, lg - rg), shortE = Math.Max(0, le - re);
                    string who = code.Length > 0 ? $"{code} – {name}" : name;
                    if (shortG + shortE > 0)
                    {
                        f.Missing += shortG + shortE;
                        f.StoreIssues.Add($"{who}:  {shortG + shortE:N0} missing" + (shortG > 0 && shortE > 0 ? $" (G {shortG:N0}, E {shortE:N0})" : shortG > 0 ? " (G crates)" : " (E crates)"));
                    }
                    int overG = Math.Max(0, rg - lg), overE = Math.Max(0, re - le);
                    if (overG + overE > 0) f.OverReturns.Add($"{who}:  returned {overG + overE:N0} more than delivered");
                    var st = (code.Length > 0 ? tripRouteStores.FirstOrDefault(s => StoreDirectory.Same(s.StoreCode, code)) : null)
                             ?? (name.Length > 0 ? tripRouteStores.FirstOrDefault(s => StoreDirectory.Same(s.StoreName, name)) : null);
                    string route = StoreDirectory.Route(st);
                    if (route.Length > 0 && !routes.Any(x => StoreDirectory.Same(x, route))) routes.Add(route);
                }
                if (routes.Count == 0 && d != null && !string.IsNullOrWhiteSpace(d.CleanRouteDisplay)) routes.Add(d.CleanRouteDisplay.Trim());
                f.Routes = routes;
                f.Damaged = tripIncidentDamaged;
                f.DamagedNote = tripIncidentNote;
                tripCrateSummary.ShowFigures(f);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        /// <summary>Points the attachments box at the trip's dispatch: the picked dispatch, or the report being edited.</summary>
        private void SyncAttachmentOwner()
        {
            if (tripAttachments == null) return;
            var d = GetSelectedDelivery();
            if (d == null && isEditingExistingReport && selectedTripReport != null)
                d = allDeliveries.FirstOrDefault(x => x.Id == selectedTripReport.DeliveryId || string.Equals(x.DispatchNumber, selectedTripReport.DispatchNumber, StringComparison.OrdinalIgnoreCase));
            if (d != null) { tripAttachments.SetOwner("Dispatch", d.Id, d.DispatchNumber ?? ""); return; }
            if (isEditingExistingReport && selectedTripReport != null && selectedTripReport.DeliveryId > 0)
                tripAttachments.SetOwner("Dispatch", selectedTripReport.DeliveryId, selectedTripReport.DispatchNumber ?? "");
            else tripAttachments.SetOwner("Dispatch", 0, "");
        }
        private void LoadSelectedDeliveryIntoForm()
        {
            var d = GetSelectedDelivery();
            SyncAttachmentOwner();
            if (d == null) return;
            bool wasBinding = isBindingForm;
            isBindingForm = false;
            UpdateMultiDropTripInfo(d);
            RecalculateTotalsFromStoreGrid();
            isBindingForm = wasBinding;
        }
    }
}
