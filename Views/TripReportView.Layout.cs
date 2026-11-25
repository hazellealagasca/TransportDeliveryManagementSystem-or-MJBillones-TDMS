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
            lblRem.Visible = false; txtRemarks.Visible = false; // Nov 26: no QA / store remarks box on the trip report (an old report keeps its saved remarks)
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
            var pickFilterRow = BuildPickFilterRow();
            var card1 = CreateSectionCard("•", "Dispatch order", "The delivery this report closes out. Its route, truck and crew are loaded from the waiting list.",
                lblDel, pickFilterRow, cbDelivery, lblDispatchInfoBanner, BuildExtraDispatchesRow(), BuildTripOverview(), BuildTripTypeRow());
            // Nov 18: no "Dispatch order" caption, search row or drop-down on the form. A report is always started from the waiting list
            // (Create trip report) or from a dispatch, which picks the dispatch in cbDelivery by code; the controls stay (hidden) and keep driving the form.
            // The picked dispatch is shown by lblDispatchInfoBanner and the trip overview.
            lblDel.Visible = false;
            pickFilterRow.Visible = false;
            cbDelivery.Visible = false;
            if (lblDispatchInfoBanner != null) lblDispatchInfoBanner.Visible = GetSelectedDelivery() != null;
            tripCrateSummary = new TripCrateSummaryPanel();
            lblDeliveredCrateTotals = new Label
            {
                AutoSize = false, Height = Dpi(34), Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary,
                BackColor = AppTheme.Surface, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false
            };
            UpdateDeliveredCrateTotals();
            // The delivered-crate totals (lblDeliveredCrateTotals) are the footer above the action bar, on every step (BuildTripCrateFooter).
            var card2 = CreateSectionCard("2", "Stores, DRs & times", "Each store's arrival and departure time, who received it, its DR numbers and the empties that came back.",
                pnlMultiDropInfo, tblTimes, tblManagerNames);
            card2.Name = "pnlStep1Stores";
            // Nov 26: the form is 2 steps: 1 = plant gate card then this stores card; 2 = incident card then attachments card.
            _ = tblCrateTotals; // Nov 6 workflow: no delivered-crate totals or crate cards on the form (the values are still worked out)
            // Nov 6 workflow: crew pay is internal and is recorded in Resources → Crew Trip History, not on the trip report.
            var card4 = CreateSectionCard("1", "Plant gate & sign-off", "Truck times at the plant and the people who signed.",
                pnlTripFooter, lblRem, txtRemarks);
            tripAttachments = new AttachmentPanel("Proof of delivery (POD)") { Height = Dpi(170) }; // Nov 26: was 250 (step 2 fits one screen)
            tripAttachments.AttachmentsChanged += (s, e) => FilterGridData();
            // Nov 26: Attachments sit under the incident card on step 2; the badge is "4" with the incident card and "3" without it (read at paint time).
            var card5 = CreateSectionCard(() => IncidentStepOn ? "4" : "3", "Attachments", "Proof of delivery, receipts, trip sheets and photos for this trip. Drop files in or click Add.", tripAttachments);
            cardIncident = CreateSectionCard("3", "Incident / Damage Report", "Any damaged or missing crates on this trip? Kept for reference only: it never reaches a statement, invoice or receipt.", BuildIncidentStep());
            Features.Bind(this, ApplyIncidentFeature, Features.TripIncidents);
            cardIncident.Name = "pnlIncidentCard";
            card4.Name = "pnlPlantGateCard";
            card5.Name = "pnlAttachmentsCard";
            cardAttachments = card5;
            // Nov 26: step 1 holds the plant gate card, then the stores card; step 2 holds the incident card, then the attachments card (Dock Top: the last one added is the top one).
            pnlStepGateStores = new Panel { Name = "pnlStep1GateAndStores", Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            pnlStepGateStores.Controls.Add(card2);
            pnlStepGateStores.Controls.Add(CreateSectionSpacer());
            pnlStepGateStores.Controls.Add(card4);
            incidentAttachSpacer = CreateSectionSpacer();
            pnlStepIncidentAttach = new Panel { Name = "pnlStep2IncidentAndAttachments", Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            pnlStepIncidentAttach.Controls.Add(card5);
            pnlStepIncidentAttach.Controls.Add(incidentAttachSpacer);
            pnlStepIncidentAttach.Controls.Add(cardIncident);
            // Top to bottom: dispatch card (banner, overview), step header, then the one step shown (FormWizard, see BuildTripWizard):
            // step 1 = Plant gate & sign-off, Stores, DRs & times; step 2 = Incident / Damage Report, Attachments (Nov 26).
            tripHeaderHost = new Panel { Name = "pnlTripStepHeaderHost", Dock = DockStyle.Top, Height = Dpi(64), BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            pnlFormBody.Controls.Add(pnlStepIncidentAttach);
            pnlFormBody.Controls.Add(pnlStepGateStores);
            pnlFormBody.Controls.Add(tripHeaderHost);
            // Nov 26: no spacer between the slim dispatch card and the step header (each step fits one screen)
            pnlFormBody.Controls.Add(card1);
            pnlFormBody.ResumeLayout(true);
            WirePlantGateValidation();
            CompactTripLayout(card1);
        }
        private Panel CreateGap(int h) => new Panel { Height = h, BackColor = AppTheme.Surface };
        private Panel CreateSectionSpacer() => new Panel { Dock = DockStyle.Top, Height = Dpi(8), BackColor = AppTheme.Canvas }; // Nov 26: was 16 (the steps fit one screen)
        private Panel CreateSectionCard(string step, string title, string subtitle, params Control?[] contentTopToBottom) =>
            CreateSectionCard(() => step, title, subtitle, contentTopToBottom);
        private Panel CreateSectionCard(Func<string> step, string title, string subtitle, params Control?[] contentTopToBottom)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface,
                Padding = new Padding(Dpi(24), Dpi(6), Dpi(24), Dpi(8)), // Nov 26: tighter (was 18 / 22)
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
            var header = new Panel { Dock = DockStyle.Top, Height = Dpi(36), BackColor = AppTheme.Surface }; // Nov 26: one line (title, then the description beside it); was 56
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                int titleRight = 0;
                float zf = UiZoom.Factor; // Nov 26: card titles follow the display size
                int d = (int)(Dpi(26) * zf);
                var circle = new Rectangle(0, Dpi(2), d, d);
                using (var b = new SolidBrush(AppTheme.AccentSoft)) g.FillEllipse(b, circle);
                using (var nf = new Font("Segoe UI Semibold", 10F * zf))
                    TextRenderer.DrawText(g, step(), nf, circle, AppTheme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                using (var tf = new Font("Segoe UI Semibold", 12F * zf))
                {
                    TextRenderer.DrawText(g, title, tf, new Rectangle(d + Dpi(10), Dpi(2), header.Width, d), AppTheme.TextPrimary, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
                    titleRight = d + Dpi(10) + TextRenderer.MeasureText(g, title, tf).Width + (int)(Dpi(12) * zf);
                }
                using (var sf = new Font("Segoe UI", 9F * zf))
                    TextRenderer.DrawText(g, subtitle, sf, new Rectangle(titleRight, Dpi(2), Math.Max(0, header.Width - titleRight), d), AppTheme.TextMuted, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                using var pen = new Pen(AppTheme.Border);
                g.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            header.Resize += (s, e) => header.Invalidate();
            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(6), BackColor = AppTheme.Surface };
            card.Controls.Add(gap);
            card.Controls.Add(header);
            return card;
        }
        /// <summary>
        /// Nov 26: each step fits one screen: the green dispatch banner sits in the page header beside the title (no "Dispatch order" card heading),
        /// and the dispatch card keeps only its other rows with a slim padding.
        /// </summary>
        private void CompactTripLayout(Panel dispatchCard)
        {
            // the dispatch card: no heading row, slim padding
            if (dispatchCard.Controls.Count >= 2)
            {
                dispatchCard.Controls[dispatchCard.Controls.Count - 1].Visible = false;   // heading (added last = top)
                dispatchCard.Controls[dispatchCard.Controls.Count - 2].Visible = false;   // the gap under it
            }
            dispatchCard.Padding = new Padding(Dpi(24), 0, Dpi(24), 0);
            if (lblDispatchInfoBanner == null) return;
            tripDispatchCard = dispatchCard;
            // the banner sits beside the title in the page header while it fits there (always at 100 %); at bigger display sizes it moves back
            // into the form above the steps, where it has the full width and wraps (Nov 26)
            lblDispatchInfoBanner.Margin = new Padding(0);
            MoveBannerToHeader();
            lblDispatchInfoBanner.TextChanged += (s, e) => PlaceTripBanner();
            pnlFormHeaderBar.SizeChanged += (s, e) => PlaceTripBanner();
            UiZoom.OnApplied(this, "tripHeaderBanner", PlaceTripBanner);
        }
        private Panel? tripDispatchCard;
        private bool placingTripBanner;
        private void MoveBannerToHeader()
        {
            var b = lblDispatchInfoBanner!;
            b.Parent?.Controls.Remove(b);
            b.Dock = DockStyle.Fill;
            pnlFormHeaderBar.Controls.Add(b);
            ArrangeTripHeader();
        }
        /// <summary>Nov 26: the dispatch banner beside the title when its text fits the header; otherwise (bigger display size, long names) at the top of the form body, wrapped.</summary>
        private void PlaceTripBanner()
        {
            var b = lblDispatchInfoBanner;
            if (b == null || tripDispatchCard == null || placingTripBanner || pnlFormHeaderBar.Width < Dpi(400)) return;
            placingTripBanner = true;
            try
            {
                int avail = pnlFormHeaderBar.ClientSize.Width - pnlFormHeaderBar.Padding.Horizontal - btnCloseForm.Width - (pnlHeaderSpacer?.Width ?? 0) - lblFormHeader.Width;
                var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
                int needInHeader = TextRenderer.MeasureText(b.Text, b.Font, new Size(Math.Max(1, avail - b.Padding.Horizontal), int.MaxValue), flags).Height + b.Padding.Vertical;
                int inner = pnlFormHeaderBar.ClientSize.Height - pnlFormHeaderBar.Padding.Vertical;
                bool fits = avail > Dpi(200) && needInHeader <= inner + Dpi(2);
                if (fits && b.Parent != pnlFormHeaderBar) MoveBannerToHeader();
                else if (!fits)
                {
                    if (b.Parent != tripDispatchCard)
                    {
                        b.Parent?.Controls.Remove(b);
                        b.Dock = DockStyle.Top;
                        tripDispatchCard.Controls.Add(b);
                        tripDispatchCard.Controls.SetChildIndex(b, 0);
                    }
                    int cardW = Math.Max(1, tripDispatchCard.ClientSize.Width - tripDispatchCard.Padding.Horizontal - b.Padding.Horizontal);
                    b.Height = TextRenderer.MeasureText(b.Text, b.Font, new Size(cardW, int.MaxValue), flags).Height + b.Padding.Vertical + Dpi(4);
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            finally { placingTripBanner = false; }
        }
        /// <summary>Nov 26: page header, left to right: Back button, gap, title (its own width), then the dispatch banner filling the rest.</summary>
        private void ArrangeTripHeader()
        {
            if (lblDispatchInfoBanner == null || lblDispatchInfoBanner.Parent != pnlFormHeaderBar) return;
            pnlFormHeaderBar.Height = Dpi(58);
            pnlFormHeaderBar.Padding = new Padding(pnlFormHeaderBar.Padding.Left, Dpi(4), pnlFormHeaderBar.Padding.Right, Dpi(6));
            lblFormHeader.Dock = DockStyle.Left;
            lblFormHeader.AutoSize = true;
            lblFormHeader.Padding = new Padding(0, 0, Dpi(16), 0);
            // docking runs from the highest index down: Back, gap and title take the left, the banner (index 0) fills what is left
            pnlFormHeaderBar.Controls.SetChildIndex(lblDispatchInfoBanner, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 1);
            if (pnlHeaderSpacer != null && pnlFormHeaderBar.Controls.Contains(pnlHeaderSpacer)) pnlFormHeaderBar.Controls.SetChildIndex(pnlHeaderSpacer, 2);
            if (pnlFormHeaderBar.Controls.Contains(btnCloseForm)) pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 3);
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
            ArrangeTripHeader(); // Nov 26: the dispatch banner beside the title, when it has been moved there
            pnlFormBody.Padding = new Padding(0, 0, 0, Dpi(4)); // Nov 26: was 16 (each step fits one screen)
            pnlForm.Resize -= PnlForm_ResizeCenter;
            pnlForm.Resize += PnlForm_ResizeCenter;
            UiZoom.OnApplied(this, "center", CenterFormContent);
            BuildFormActionBar();
            BuildTripCrateFooter();
            BuildTripWizard();
            CenterFormContent();
        }
        private void PnlForm_ResizeCenter(object? sender, EventArgs e) => CenterFormContent();
        private void CenterFormContent()
        {
            if (pnlForm == null || pnlForm.Width <= 0) return;
            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - (int)Math.Round(Dpi(FormContentMaxWidth) * UiZoom.Factor)) / 2);
            pnlFormHeaderBar.Padding = new Padding(side, pnlFormHeaderBar.Padding.Top, side, pnlFormHeaderBar.Padding.Bottom);
            pnlFormBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(4)); // Nov 26: was 16
            pnlFormActionsBar.Padding = new Padding(side, Dpi(12), side, Dpi(12));
            if (pnlTripCrateFooter != null) pnlTripCrateFooter.Padding = new Padding(side, 0, side, 0);
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
                if (!keep.Resuming) wizard?.GoToStep(1);   // a form opens on its first step (a kept draft comes back on the step it was left on)
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
        }
        /// <summary>
        /// Nov 18: the form is useless without a dispatch (the drop-down is hidden). If a new report was opened with none picked,
        /// close it, say how to start one and stay on the waiting list. Returns true when the form was closed.
        /// </summary>
        private bool CloseNewFormIfNoDispatch()
        {
            if (isEditingExistingReport || GetSelectedDelivery() != null) return false;
            SetFormVisibility(false);
            ToastHelper.ShowToast(this, "Start a trip report from the waiting list: click Create trip report on the dispatch.", ToastType.Warning);
            return true;
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
            if (wizard == null)
            {
                btnSave.Visible = !isEditingExistingReport;
                btnUpdate.Visible = isEditingExistingReport;
                btnClear.Visible = !isEditingExistingReport;
            }   // with the wizard, the last step shows Proceed / Save changes (see BuildTripWizard)
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
            try { UpdateIncidentHint(); } catch (Exception ex) { ErrLog.Swallowed(ex); }   // Nov 18: step 3 hint follows the store crates
            if (tripCrateSummary == null || gridStores == null) return;
            try
            {
                var f = new TripCrateFigures();
                var d = TripDelivery() ?? incidentTrip;   // Nov 18: the trip step 3 was loaded for when the drop-down has none
                // Damaged crates come from the trip's incident lines. Nov 18: step 3's own list (saved lines of this trip plus the ones just
                // added, without the ones taken off) when step 3 is on this trip, so the count follows step 3 before saving; otherwise the saved ones.
                int delId = d?.Id ?? 0;
                tripIncidentDeliveryId = delId;
                tripIncidentDamaged = 0; tripIncidentNote = "";
                if (delId > 0)
                {
                    List<IncidentReport> incs = incidentTrip != null && incidentTrip.Id == delId
                        ? ((rbIncYes?.Checked ?? false) ? tripIncidentLines.ToList() : new List<IncidentReport>())
                        : MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == delId && !string.Equals(ir.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
                    tripIncidentDamaged = incs.Sum(i => Math.Max(0, i.DamagedQuantity));
                    if (tripIncidentDamaged > 0)
                        tripIncidentNote = $"Damaged: {tripIncidentDamaged:N0} crate{(tripIncidentDamaged == 1 ? "" : "s")} (incident reports for the whole trip: {string.Join("; ", incs.Where(i => i.DamagedQuantity > 0).Select(i => $"{i.Cause} {i.DamagedQuantity:N0}"))})";
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
