using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class StatementOfAccountView : UserControl, IRefreshable, IUnsavedForm
    {
        private List<StatementOfAccount> allSoas = new List<StatementOfAccount>();
        private StatementOfAccount? selectedSoa = null;
        private bool isBindingForm = false;
        private bool isCalculatingTaxes = false;

        private FlowLayoutPanel pnlGridActionsFlow = null!;
        private TextBox txtGridSearch = null!;
        private ComboBox cbFilterClient = null!;
        private ChoiceChip? paidChip, cancelledChip;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private Button btnPrintOfficial = null!;
        private Button btnExportCsv = null!;
        private Button btnViewInvoice = null!;

        private TableLayoutPanel tblMasterContent = null!;
        private DataGridView gridSoa = null!;
        private Panel pnlForm = null!;
        private Panel pnlFormBody = null!;
        private Panel pnlFormHeaderBar = null!;
        private Label lblFormHeader = null!;

        private TextBox txtSoaNumber = null!;
        private ComboBox cbClient = null!;
        private TextBox txtClientAddress = null!;
        private TextBox txtClientTin = null!;
        private DateTimePicker dtpIssueDate = null!;
        private ComboBox cbDeliveryType = null!;
        private DateTimePicker dtpPeriodFrom = null!;
        private DateTimePicker dtpPeriodTo = null!;
        private Button btnPopulateTrips = null!;
        /// <summary>Nov 6 workflow: "Normal Operations SOA" (dispatches) or "Special Trips SOA". Never both on one statement.</summary>
        private ComboBox cbSoaType = null!;
        private string CurrentSoaType => cbSoaType != null && cbSoaType.SelectedIndex == 1 ? SoaTypes.Special : SoaTypes.Normal;

        private DataGridView gridItems = null!;
        private Button btnAddRow = null!;
        private Button btnDeleteRow = null!;
        private Button btnRecalc = null!;

        private NumericUpDown numTotalSales = null!;
        private NumericUpDown numVat = null!;
        private NumericUpDown numNetOfVat = null!;
        private NumericUpDown numFuelSurchargeTotal = null!;
        private NumericUpDown numWithholdingTax = null!;
        private NumericUpDown numTotalAmountDue = null!;
        private TextBox txtSalesInvoiceNo = null!;
        private TextBox txtPreparedBy = null!;
        private TextBox txtReceivedBy1 = null!;
        private TextBox txtReceivedBy2 = null!;

        private Button btnSaveSoa = null!;
        private Button btnPrintDrawer = null!;
        private Button btnDeleteSoa = null!;
        private Button btnClearForm = null!;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<string>? OnNavigateToInvoice { get; set; }

        // ------------------------------------------------------------------ Full-page form
        private const int FormContentMaxWidth = 1100;
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private Panel pnlTopBar = null!;
        private Panel pnlFormActionsBar = null!;
        private FlowLayoutPanel flowFormActions = null!;
        private Panel pnlHeaderSpacer = null!;
        private Panel pnlItemsGridHost = null!;
        private Label lblItemsTitle = null!;
        private Label lblItemsTotals = null!;
        private Label lblPopulateHint = null!;
        private bool isEditingExisting = false;
        public StatementOfAccountView()
        {
            InitializeComponent();
            SetupGridColumns();
            SetupContextMenu();
            LoadClients();
            LoadData();
            UIThemeHelper.ApplyTheme(this);
            StyleFullPageForm();
            FitItemsGridColumns();
            ThemeManager.ThemeChanged += (s, e) => { try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(FitItemsGridColumns)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
        }
        /// <summary>
        /// Every column of the trip lines is at least as wide as its header (and the trip report number), so nothing is cut
        /// off; when they don't all fit, the table scrolls sideways instead of squeezing the text.
        /// </summary>
        private void FitItemsGridColumns()
        {
            if (gridItems == null) return;
            try
            {
                var headFont = gridItems.ColumnHeadersDefaultCellStyle.Font ?? gridItems.Font;
                foreach (DataGridViewColumn c in gridItems.Columns)
                {
                    if (!c.Visible || c is DataGridViewButtonColumn) continue;
                    int need = TextRenderer.MeasureText(c.HeaderText ?? "", headFont).Width + Dpi(30);
                    if (c.Name == "colTripReport") need = Math.Max(need, TextRenderer.MeasureText("TRIP-20261105-022839-71078", gridItems.Font).Width + Dpi(16));
                    if (c.Name == "colDate") need = Math.Max(need, TextRenderer.MeasureText("2026-11-05", gridItems.Font).Width + Dpi(16));
                    if (c.Name == "colPlate") need = Math.Max(need, TextRenderer.MeasureText("CCE 9458", gridItems.Font).Width + Dpi(16));
                    if (c.Name == "colAmount") need = Math.Max(need, TextRenderer.MeasureText("999,999.00", gridItems.Font).Width + Dpi(16));
                    if (c.Name == "colDr") need = Math.Max(need, TextRenderer.MeasureText("DR-000000", gridItems.Font).Width + Dpi(16));
                    if (c.MinimumWidth < need) c.MinimumWidth = need;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible && EscapeGuard.AllowClose(this))
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, Dpi(56)),
                BackColor = Pal.White,
                Padding = new Padding(12, 8, 12, 4)
            };

            // The buttons wrap onto a second line on narrow windows instead of hiding behind a scrollbar.
            pnlGridActionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };

            btnToggleForm = new Button
            {
                Text = "＋ New statement",
                Size = new Size(170, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => OpenNewStatement();
            pnlGridActionsFlow.Controls.Add(btnToggleForm);

            btnPrintOfficial = new Button
            {
                Text = "🖨️ Print official SOA",
                Size = new Size(160, 32),
                BackColor = Pal.C(30, 58, 138),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0)
            };
            btnPrintOfficial.FlatAppearance.BorderSize = 0;
            btnPrintOfficial.Click += (s, e) => PrintSelectedSoa();
            pnlGridActionsFlow.Controls.Add(btnPrintOfficial);

            btnExportCsv = new Button
            {
                Text = "📊 Export to CSV",
                Size = new Size(130, 32),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0)
            };
            btnExportCsv.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnExportCsv.Click += (s, e) => DataExportHelper.ExportToCsv(gridSoa, "Statements_of_Account", "Issued Statements of Account");
            pnlGridActionsFlow.Controls.Add(btnExportCsv);

            btnViewInvoice = new Button
            {
                Text = "🧾 View linked invoice",
                Size = new Size(160, 32),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 12, 0)
            };
            btnViewInvoice.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnViewInvoice.Click += (s, e) => OpenLinkedInvoice();
            pnlGridActionsFlow.Controls.Add(btnViewInvoice);

            // Fix a saved statement (wrong amounts, forgotten trips); each line can open its dispatch.
            var btnEditSoa = new Button
            {
                Text = "✏️ Edit statement",
                Size = new Size(140, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 12, 0)
            };
            UIThemeHelper.SetButtonRole(btnEditSoa, "secondary");
            btnEditSoa.Click += (s, e) =>
            {
                var soa = gridSoa.CurrentRow?.DataBoundItem as StatementOfAccount ?? selectedSoa;
                if (soa == null) { QuietBox.Show("Select a statement in the list first.", "Edit statement", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                OpenEditStatement(soa);
            };
            pnlGridActionsFlow.Controls.Add(btnEditSoa);
            // Cancel (void) a statement: the reason is required; its service invoices go back to "Ready for a statement".
            var btnCancelSoa = new Button
            {
                Text = "⛔ Cancel statement",
                Size = new Size(160, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 12, 0)
            };
            UIThemeHelper.SetButtonRole(btnCancelSoa, "danger");
            btnCancelSoa.Click += (s, e) =>
            {
                var soa = gridSoa.CurrentRow?.DataBoundItem as StatementOfAccount ?? selectedSoa;
                if (soa == null) { QuietBox.Show("Select a statement in the list first.", "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                CancelStatement(soa);
            };
            pnlGridActionsFlow.Controls.Add(btnCancelSoa);
            txtGridSearch = new TextBox { Width = Dpi(240), Margin = new Padding(Dpi(8), Dpi(3), Dpi(8), 0) };
            PageKit.StyleSearch(txtGridSearch, "🔍 Search statement #, client…");
            txtGridSearch.TextChanged += (s, e) => FilterGridData();

            cbFilterClient = new ComboBox { Width = Dpi(200), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, Dpi(3), 0, 0) };
            PageKit.StyleCombo(cbFilterClient);
            cbFilterClient.SelectedIndexChanged += (s, e) => FilterGridData();

            new ToolTip().SetToolTip(cbFilterClient, "Show only one client's statements");
            pnlGridActionsFlow.Controls.Add(txtGridSearch);
            pnlGridActionsFlow.Controls.Add(cbFilterClient);
            paidChip = new ChoiceChip("Paid", new[] { "Hidden", "Shown" }, "✓") { Margin = new Padding(Dpi(6), Dpi(4), 0, 0) };
            paidChip.SelectionChanged += (s, e) => FilterGridData();
            new ToolTip().SetToolTip(paidChip, "Paid statements are kept in Finance History → Settled records. Searching always finds them.");
            pnlGridActionsFlow.Controls.Add(paidChip);
            cancelledChip = new ChoiceChip("Cancelled", new[] { "Hidden", "Shown" }, "✓") { Margin = new Padding(Dpi(6), Dpi(4), 0, 0) };
            cancelledChip.SelectionChanged += (s, e) => FilterGridData();
            new ToolTip().SetToolTip(cancelledChip, "Cancelled statements stay in the audit log. Searching always finds them.");
            pnlGridActionsFlow.Controls.Add(cancelledChip);

            pnlTopBar.Controls.Add(pnlGridActionsFlow);
            // H3: the toolbar wraps onto a second row (search, client filter, Paid / Cancelled chips). An AutoSize panel measured it one row too
            // short and cut the second row off, so the bar now takes exactly the height of its wrapped rows.
            pnlTopBar.AutoSize = false;
            void FitTopBar() { int hh = pnlGridActionsFlow.Height + pnlTopBar.Padding.Vertical; if (pnlTopBar.Height != hh) pnlTopBar.Height = hh; }
            pnlGridActionsFlow.SizeChanged += (s, e) => FitTopBar();
            pnlTopBar.Resize += (s, e) => FitTopBar();
            this.Controls.Add(pnlTopBar);

            // Page title like the other list pages (Trucks, Crew, Order History).
            var soaHeader = PageKit.Header("Statements of Account", "A statement groups the service invoices of one client into one bill. Pick the invoices that are ready, make the statement, then collect one payment in Collections.");
            var soaHeaderHost = soaPageHeaderHost = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(Dpi(16), Dpi(8), Dpi(16), Dpi(4)), BackColor = Color.Transparent };
            soaHeader.MakeCompact();
            soaHeaderHost.Controls.Add(soaHeader);
            this.Controls.Add(soaHeaderHost);
            BuildSoaReadyBanner();
            tblMasterContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = Pal.C(248, 250, 252)
            };
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMasterContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            InitializeFormDrawer();
            tblMasterContent.Controls.Add(pnlForm, 0, 0);

            gridSoa = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 28 },
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9F)
            };
            gridSoa.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridSoa.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 41, 59);
            gridSoa.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);

            gridSoa.SelectionChanged += (s, e) => GridSoa_SelectionChanged();
            gridSoa.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridSoa.Rows[e.RowIndex].DataBoundItem is StatementOfAccount soa) OpenEditStatement(soa); };
            tblMasterContent.Controls.Add(gridSoa, 1, 0);

            this.Controls.Add(tblMasterContent);
            // Fill must dock after the top bar, or the bar covers the list's column headers.
            tblMasterContent.BringToFront();
        }

        private void InitializeFormDrawer()
        {
            pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Visible = false,
                Margin = new Padding(0)
            };

            pnlFormHeaderBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = Dpi(58),
                BackColor = AppTheme.Canvas,
                Padding = new Padding(0, Dpi(6), 0, Dpi(12))
            };

            lblFormHeader = new Label
            {
                Text = "New Statement of Account",
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Font = new Font("Segoe UI Semibold", 15F),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnCloseForm = new Button
            {
                Text = "←  Back to statements",
                Dock = DockStyle.Left,
                Width = Dpi(190),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCloseForm.Click += (s, e) => { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); };

            pnlHeaderSpacer = new Panel { Dock = DockStyle.Left, Width = Dpi(16), BackColor = AppTheme.Canvas };
            pnlFormHeaderBar.Controls.Add(lblFormHeader);
            pnlFormHeaderBar.Controls.Add(pnlHeaderSpacer);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);
            pnlFormBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = AppTheme.Canvas
            };
            pnlFormActionsBar = new Panel { Dock = DockStyle.Bottom, Height = Dpi(66), BackColor = AppTheme.Surface };
            pnlFormActionsBar.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, pnlFormActionsBar.Width, 0); };
            flowFormActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = AppTheme.Surface };
            pnlFormActionsBar.Controls.Add(flowFormActions);
            pnlForm.Controls.Add(pnlFormBody);

            pnlForm.Controls.Add(pnlFormActionsBar);
            pnlForm.Controls.Add(pnlFormHeaderBar);
            pnlForm.Resize += (s, e) => CenterFormContent();
            BuildFormControls();
            InitSoaGuide();
            if (Features.On(Features.PaymentsWorkbench)) ApplySoaWorkbench(); // Ledger Desk layout (Settings → Features can switch it off)
        }
        private void CenterFormContent()
        {
            if (pnlForm == null || pnlForm.Width <= 0) return;
            if (soaWorkbench)
            {
                pnlFormHeaderBar.Padding = new Padding(Dpi(24), Dpi(8), Dpi(24), Dpi(8));
                pnlFormBody.Padding = new Padding(0);
                pnlFormActionsBar.Padding = new Padding(Dpi(24), Dpi(12), Dpi(24), Dpi(12));
                return;
            }
            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);
            pnlFormHeaderBar.Padding = new Padding(side, Dpi(4), side, Dpi(6));
            if (soaStrip != null) soaStrip.Padding = new Padding(side, 0, side, 0);
            pnlFormBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(16));
            pnlFormActionsBar.Padding = new Padding(side, Dpi(12), side, Dpi(12));
        }
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
            card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Dpi(10), BackColor = AppTheme.Surface });
            card.Controls.Add(header);
            return card;
        }
        /// <summary>A grid of labelled fields: each cell is (label, input) or null for an empty cell.</summary>
        private TableLayoutPanel CreateFieldGrid(int columns, params (string Label, Control Input)?[] cells)
        {
            int rows = (cells.Length + columns - 1) / columns;
            var tbl = new TableLayoutPanel { Height = rows * Dpi(66), ColumnCount = columns, RowCount = rows, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            for (int c = 0; c < columns; c++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            for (int r = 0; r < rows; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(66)));
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] is not (string label, Control input)) continue;
                var pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, (i % columns) < columns - 1 ? Dpi(16) : 0, 0), BackColor = AppTheme.Surface };
                var lbl = new Label { Text = label, Dock = DockStyle.Top, Height = Dpi(24), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true };
                input.Dock = DockStyle.Top;
                pnl.Controls.Add(input);
                pnl.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Dpi(4), BackColor = AppTheme.Surface });
                pnl.Controls.Add(lbl);
                tbl.Controls.Add(pnl, i % columns, i / columns);
            }
            return tbl;
        }
        private Button MakeFormButton(string text, string role, int width = 0)
        {
            var b = new Button
            {
                Text = text,
                Height = Dpi(36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, Dpi(8), 0),
                UseMnemonic = false
            };
            if (width > 0) b.Width = width;
            else { b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink; b.Padding = new Padding(Dpi(10), 0, Dpi(10), 0); b.MinimumSize = new Size(0, Dpi(36)); }
            b.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }

        private void BuildFormControls()
        {
            pnlFormBody.SuspendLayout();

            // ---------------- Inputs
            cbClient = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 10F) };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) { AutoFillClientDetails(); AutoFillClientTrips(); } SchedulePickerRefresh(); };
            cbClient.TextChanged += (s, e) => SchedulePickerRefresh();
            txtClientAddress = new TextBox { Font = new Font("Segoe UI", 10F), Text = "" };
            txtClientTin = new TextBox { Font = new Font("Segoe UI", 10F), Text = "" };
            cbDeliveryType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cbDeliveryType.Items.AddRange(new object[] { "REF", "DRY" });
            cbDeliveryType.SelectedIndex = -1;
            cbSoaType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI Semibold", 10F) };
            cbSoaType.Items.AddRange(Features.On(Features.SpecialTrips) ? new object[] { "Normal Operations SOA", "Special Trips SOA" } : new object[] { "Normal Operations SOA" });
            cbSoaType.SelectedIndex = 0;
            cbSoaType.SelectedIndexChanged += (s, e) => { if (!isBindingForm) SchedulePickerRefresh(); };
            dtpIssueDate = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 10F) };
            var pnlDates = new TableLayoutPanel { Height = Dpi(30), ColumnCount = 3, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            pnlDates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlDates.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(34)));
            pnlDates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            dtpPeriodFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            var lblToWord = new Label { Text = "to", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted };
            dtpPeriodTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Value = DateTime.Today, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), Margin = new Padding(0) };
            dtpPeriodFrom.ValueChanged += (s, e) => SchedulePickerRefresh();
            dtpPeriodTo.ValueChanged += (s, e) => SchedulePickerRefresh();
            pnlDates.Controls.Add(dtpPeriodFrom, 0, 0);
            pnlDates.Controls.Add(lblToWord, 1, 0);
            pnlDates.Controls.Add(dtpPeriodTo, 2, 0);
            var flowPeriodPresets = new FlowLayoutPanel { Height = Dpi(36), WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            void SetPeriod(DateTime from, DateTime to) { dtpPeriodFrom.SetSafe(from); dtpPeriodTo.SetSafe(to); }
            var btnFirstHalf = MakeFormButton("1st – 15th", "secondary");
            btnFirstHalf.Click += (s, e) => { var r = dtpPeriodFrom.Value; SetPeriod(new DateTime(r.Year, r.Month, 1), new DateTime(r.Year, r.Month, 15)); };
            var btnSecondHalf = MakeFormButton("16th – end", "secondary");
            btnSecondHalf.Click += (s, e) => { var r = dtpPeriodFrom.Value; SetPeriod(new DateTime(r.Year, r.Month, 16), new DateTime(r.Year, r.Month, DateTime.DaysInMonth(r.Year, r.Month))); };
            var btnThisMonth = MakeFormButton("This month", "secondary");
            btnThisMonth.Click += (s, e) => { var t = DateTime.Today; SetPeriod(new DateTime(t.Year, t.Month, 1), new DateTime(t.Year, t.Month, DateTime.DaysInMonth(t.Year, t.Month))); };
            var btnLastMonth = MakeFormButton("Last month", "secondary");
            btnLastMonth.Click += (s, e) => { var t = DateTime.Today.AddMonths(-1); SetPeriod(new DateTime(t.Year, t.Month, 1), new DateTime(t.Year, t.Month, DateTime.DaysInMonth(t.Year, t.Month))); };
            foreach (var b in new[] { btnFirstHalf, btnSecondHalf, btnThisMonth, btnLastMonth }) flowPeriodPresets.Controls.Add(b);
            btnPopulateTrips = MakeFormButton("⚡  Add all ready trips", "primary");
            btnPopulateTrips.AutoSize = false;
            btnPopulateTrips.Height = Dpi(36);
            btnPopulateTrips.Click += (s, e) => PopulateTripsFromCompletedDeliveries();
            txtSoaNumber = new TextBox { Text = MJdatabase.GenerateNextSoaNumber(), Visible = false };
            lblPopulateHint = new Label
            {
                Height = Dpi(26),
                Text = "The client's unpaid service invoices of this SOA type are listed in step 2. Tick the ones to put on this statement (for example the whole week or month).",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            // Nov 6 workflow: the statement is filled from the client's service invoices, so the period is taken from them
            // (it can still be changed for the print). The old "import trips by period" buttons are no longer shown.
            _ = flowPeriodPresets;
            var gridClient = CreateFieldGrid(2,
                ("Billed to (client company)", cbClient), ("SOA type", cbSoaType),
                ("Client address", txtClientAddress), ("Client TIN", txtClientTin),
                ("Delivery type", cbDeliveryType), ("Statement date", dtpIssueDate),
                ("Period covered (from the invoices)", pnlDates), null);
            // ---------------- Trips & stores grid
            var bar = new TableLayoutPanel { Height = Dpi(46), ColumnCount = 8, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 7; i++) bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblItemsTitle = new Label { Dock = DockStyle.Fill, Text = "🧾 Trips & stores on this statement", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            btnAddRow = MakeFormButton("➕ Add row", "secondary");
            btnAddRow.Click += (s, e) => AddBlankGridRow();
            btnDeleteRow = MakeFormButton("Remove selected", "secondary");
            btnDeleteRow.Click += (s, e) => RemoveSelectedRows();
            var btnBulkEdit = MakeFormButton("✎ Edit selected rows", "secondary");
            btnBulkEdit.Click += (s, e) => BulkEditSelectedRows();
            var btnEditSoaTable = MakeFormButton("✎ Edit table", "secondary");
            btnEditSoaTable.Click += (s, e) => OpenSoaTableEditor(lblItemsTitle);
            Features.Bind(btnEditSoaTable, () => btnEditSoaTable.Visible = Features.On(Features.StoreTableEditHeader), Features.StoreTableEditHeader);
            btnRecalc = MakeFormButton("↻ Recalculate", "secondary");
            btnRecalc.Margin = new Padding(0);
            btnRecalc.Click += (s, e) => RecalculateTaxes(manualOverride: false);
            bar.Controls.Add(lblItemsTitle, 0, 0);
            bar.Controls.Add(btnAddRow, 1, 0);
            bar.Controls.Add(btnBulkEdit, 2, 0);
            bar.Controls.Add(btnDeleteRow, 3, 0);
            bar.Controls.Add(btnEditSoaTable, 4, 0);
            var btnTripPay = MakeFormButton("₱ Trip payments", "primary");
            btnTripPay.Click += (s, e) => ShowTripPayments(auto: false);
            bar.Controls.Add(btnTripPay, 7, 0);
            bar.Controls.Add(btnRecalc, 6, 0);
            // Go back to the dispatch behind the selected line (to check a forgotten store or a wrong amount).
            var btnOpenDispatch = MakeFormButton("🔗 Open dispatch", "secondary");
            btnOpenDispatch.Click += (s, e) =>
            {
                var row = gridItems.CurrentRow;
                int did = row != null && gridItems.Columns.Contains("colDeliveryId") && int.TryParse(row.Cells["colDeliveryId"].Value?.ToString(), out int d) ? d : 0;
                if (did <= 0) { QuietBox.Show("Select a line that came from a dispatch (imported trips), then click Open dispatch.", "Open dispatch", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                RowMenu.OpenDispatch(this, did, null);
            };
            bar.Controls.Add(btnOpenDispatch, 5, 0);
            var lblItemsHint = new Label
            {
                Height = Dpi(24),
                Text = "Type only the number of drops, the Amount and the Fuel surcharge for each trip (or use Trip payments). Everything else comes from the trip report. The totals on the right follow as you type.",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true
            };

            pnlItemsGridHost = new Panel { Height = Dpi(300), BackColor = AppTheme.Surface };
            gridItems = new MultiValueGrid
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = true,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ScrollBars = ScrollBars.Both,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(36),
                RowTemplate = { Height = Dpi(30), MinimumHeight = Dpi(30) },
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false
            };
            gridItems.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridItems.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.75F);
            var roStyle = new DataGridViewCellStyle { BackColor = Pal.C(248, 250, 252) };
            var editStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232) };
            var moneyStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" };
            DataGridViewTextBoxColumn Col(string name, string header, int weight, DataGridViewCellStyle style, int min = 44) =>
                new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight, MinimumWidth = Dpi(min), DefaultCellStyle = new DataGridViewCellStyle(style) };
            gridItems.Columns.Add(Col("colTripNo", "Trip", 38, roStyle, 38));
            gridItems.Columns.Add(Col("colDate", "Date store received", 90, editStyle, 80));
            gridItems.Columns.Add(Col("colZone", "Zone", 52, editStyle));
            gridItems.Columns.Add(Col("colDr", "DR numbers", 130, editStyle, 96));
            gridItems.Columns.Add(Col("colDest", "Destination / store", 190, editStyle, 120));
            gridItems.Columns.Add(Col("colType", "Type", 50, editStyle));
            gridItems.Columns.Add(Col("colPlate", "Plate no.", 80, editStyle, 70));
            gridItems.Columns.Add(Col("colTons", "Tons", 48, editStyle));
            gridItems.Columns.Add(Col("colDrops", "Drops", 50, editStyle));
            gridItems.Columns.Add(Col("colAmount", "Amount", 92, moneyStyle, 80));
            gridItems.Columns.Add(Col("colFuel", "Fuel surch.", 86, moneyStyle, 72));
            gridItems.Columns.Add(Col("colDriver", "Driver / helper", 140, editStyle, 100));
            gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeliveryId", HeaderText = "Delivery Id", Visible = false });
            // Added last so every column above keeps its position (the totals read columns by position);
            // shown next to the DR cell and the destination.
            gridItems.Columns.Add(Col("colStoreCode", "Store code", 70, editStyle, 70));
            gridItems.Columns.Add(DrCellSupport.MakeButtonColumn("colDrBtn", Dpi(34)));
            // Trip report # (where the line came from; read-only) and the store's Total crates (G + E from the trip report:
            // only the total is billed, the crate types stay on the trip report). Also added last, shown in place.
            var trStyle = new DataGridViewCellStyle(roStyle) { Font = new Font("Segoe UI Semibold", 8.75F) };
            var crStyle = new DataGridViewCellStyle(editStyle) { Alignment = DataGridViewContentAlignment.MiddleRight };
            var colTr = Col("colTripReport", "Trip report #", 120, trStyle, 110);
            colTr.ReadOnly = true;
            gridItems.Columns.Add(colTr);
            gridItems.Columns.Add(Col("colCrates", "Total crates", 64, crStyle, 70));
            // The special trip a line bills (hidden; links the line back to the trip).
            gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSpecialTrip", HeaderText = "Special trip Id", Visible = false });
            gridItems.Columns["colStoreCode"]!.DisplayIndex = gridItems.Columns["colDest"]!.DisplayIndex;
            gridItems.Columns["colDrBtn"]!.DisplayIndex = gridItems.Columns["colDr"]!.DisplayIndex + 1;
            gridItems.Columns["colTripReport"]!.DisplayIndex = gridItems.Columns["colTripNo"]!.DisplayIndex + 1;
            gridItems.Columns["colCrates"]!.DisplayIndex = gridItems.Columns["colDrops"]!.DisplayIndex + 1;
            InputGuards.DigitsOnlyColumns(gridItems, "colCrates");
            // Nov 6 workflow: the statement shows only the billed line items (DR no., store, plate, tons, drops, freight tariff,
            // fuel surcharge) and the crew names. The other columns stay in the table (hidden) so the saved lines keep their data.
            // Nov 7: the table follows the statement sheet: Trip, Date store received, Zone, DR no., Destination, Type of delivery, Plate, Tons, Drops, Amount, Fuel surcharge, Driver / Aide.
            foreach (var hide in new[] { "colTripReport", "colCrates", "colStoreCode", "colDrBtn" }) gridItems.Columns[hide]!.Visible = false;
            gridItems.Columns["colTripNo"]!.HeaderText = "Trip";
            gridItems.Columns["colDr"]!.HeaderText = "DR no.";
            gridItems.Columns["colDest"]!.HeaderText = "Destination";
            gridItems.Columns["colType"]!.HeaderText = "Type of delivery";
            gridItems.Columns["colDrops"]!.HeaderText = "No. of drops";
            gridItems.Columns["colAmount"]!.HeaderText = "Amount";
            gridItems.Columns["colFuel"]!.HeaderText = "Fuel surcharge";
            gridItems.Columns["colDriver"]!.HeaderText = "Driver / Aide";
            // Only Amount and Fuel surcharge look editable; the other columns are grey (they are read-only on trip lines).
            foreach (var nm in new[] { "colDate", "colZone", "colDr", "colDest", "colType", "colPlate", "colTons", "colDrops", "colDriver" }) gridItems.Columns[nm]!.DefaultCellStyle.BackColor = Pal.C(248, 250, 252);
            // Live totals: every keystroke in Amount / Fuel surcharge is committed so the statement's computation updates while typing.
            gridItems.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb && gridItems.CurrentCell?.OwningColumn?.Name is "colAmount" or "colFuel")
                {
                    tb.TextChanged -= LiveCommitAmount; tb.TextChanged += LiveCommitAmount;
                }
            };
            // A line that bills a trip is read-only: the trip report and the service invoice give every figure (fix them there).
            gridItems.CellBeginEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= gridItems.Rows.Count) return;
                var row = gridItems.Rows[e.RowIndex];
                if (row.IsNewRow) return;
                // Nov 10: drops, Amount and Fuel surcharge are typed once per trip, on the trip's first row.
                if (TripFigureCols.Contains(gridItems.Columns[e.ColumnIndex].Name) && !IsFirstRowOfTrip(e.RowIndex)) { e.Cancel = true; return; }
                bool linked = int.TryParse(row.Cells["colDeliveryId"].Value?.ToString(), out int did) && did > 0
                              || int.TryParse(row.Cells["colSpecialTrip"].Value?.ToString(), out int sid) && sid > 0;
                if (!linked) return;
                // Nov 9 (statement first): on a trip that has no service invoice yet, ONLY Amount and Fuel surcharge can be typed (everything else comes from the trip report).
                string colName = gridItems.Columns[e.ColumnIndex].Name;
                if (colName == "colDrops") return;   // Nov 10: the number of drops can be changed on the statement
                if (colName is "colAmount" or "colFuel")
                {
                    try
                    {
                        int d0 = RowDeliveryId(row), s0 = RowSpecialTripId(row);
                        if (!IsLegacySoa() && (MJdatabase.GetLiveInvoiceForTrip(d0 > 0 ? d0 : 0, d0 > 0 ? 0 : s0) == null || MJdatabase.IsPreStatementInvoice(d0 > 0 ? d0 : 0, d0 > 0 ? 0 : s0))) return; // editable
                    }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }
                e.Cancel = true;
            };
            DrCellSupport.Attach((MultiValueGrid)gridItems, "colDr", "colDrBtn",
                row => (row.Cells["colDest"].Value?.ToString() ?? "").Trim(),
                row => { RejectDuplicateSoaDrs(row); SizeItemsGrid(); });
            gridItems.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string col = gridItems.Columns[e.ColumnIndex].Name;
                if (col is not ("colAmount" or "colFuel" or "colDrops")) return;
                string txt = (e.FormattedValue?.ToString() ?? "").Replace(",", "").Replace("₱", "").Trim();
                if (txt.Length == 0) return;
                if (!decimal.TryParse(txt, out var v) || v < 0)
                {
                    gridItems.Rows[e.RowIndex].ErrorText = col == "colDrops" ? "Drops must be a whole number of 0 or more." : "Enter an amount of 0 or more, e.g. 8500 or 8500.50";
                    e.Cancel = true;
                }
                else gridItems.Rows[e.RowIndex].ErrorText = "";
            };
            gridItems.CellEndEdit += (s, e) => { if (e.RowIndex >= 0) gridItems.Rows[e.RowIndex].ErrorText = ""; };
            gridItems.CellValueChanged += (s, e) =>
            {
                if (isBindingForm) return;
                RecalculateTaxes(manualOverride: false);
            };
            gridItems.RowsAdded += (s, e) => UpdateItemsSummary();
            gridItems.RowsRemoved += (s, e) => UpdateItemsSummary();
            SetupTripMerge();   // Nov 10: a trip's cells are one tall cell, like the printed statement
            pnlItemsGridHost.Controls.Add(gridItems);
            // Nov 10: the statement sheet's totals block sits right under the trips table, like the print.
            sheetTotals = new SoaSheetTotalsPanel(gridItems, () => new[] { numTotalSales.Value, numVat.Value, numNetOfVat.Value, numFuelSurchargeTotal.Value, numVat.Value, numWithholdingTax.Value, numTotalAmountDue.Value });
            pnlItemsGridHost.Controls.Add(sheetTotals);
            gridItems.BringToFront();
            lblItemsTotals = new Label
            {
                Height = Dpi(40),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = AppTheme.TextSecondary,
                Text = "No invoices picked yet. Tick the ready service invoices in step 2."
            };
            // ---------------- Taxes
            NumericUpDown MakeNum()
            {
                var n = new NumericUpDown
                {
                    Maximum = 100000000,
                    Minimum = 0,
                    DecimalPlaces = 2,
                    ThousandsSeparator = true,
                    Font = new Font("Segoe UI Semibold", 10.5F),
                    TextAlign = HorizontalAlignment.Right,
                    InterceptArrowKeys = false,
                    ReadOnly = true,          // calculated from the service invoices: to change an amount, correct the invoice in Billing
                    TabStop = false,
                    BackColor = AppTheme.Canvas
                };
                if (n.Controls.Count > 0) n.Controls[0].Visible = false; // no up/down buttons on a calculated figure
                n.ValueChanged += (s, e) => { if (!isCalculatingTaxes) RecalculateTaxes(manualOverride: true); };
                return n;
            }

            numTotalSales = MakeNum();
            numVat = MakeNum();
            numNetOfVat = MakeNum();
            numFuelSurchargeTotal = MakeNum();
            numWithholdingTax = MakeNum();
            numTotalAmountDue = MakeNum();
            foreach (var tn in new[] { numTotalSales, numVat, numNetOfVat, numFuelSurchargeTotal, numWithholdingTax, numTotalAmountDue }) tn.ValueChanged += (s2, e2) => sheetTotals?.Invalidate();
            numTotalAmountDue.Font = new Font("Segoe UI Semibold", 18F);
            numTotalAmountDue.BorderStyle = BorderStyle.None;
            numTotalAmountDue.BackColor = AppTheme.AccentSoft;
            numTotalAmountDue.ForeColor = AppTheme.AccentText;
            var taxFields = CreateFieldGrid(2,
                ("Total Sales (VAT Inclusive)", numTotalSales), ("Less: VAT (12%)", numVat),
                ("Amount: Net of VAT", numNetOfVat), ("Fuel surcharge (added, no VAT)", numFuelSurchargeTotal),
                ("Less: Withholding Tax (2% of net of VAT)", numWithholdingTax), null);
            taxFields.Dock = DockStyle.Fill;
            var totalBox = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.AccentSoft, Padding = new Padding(Dpi(18), Dpi(14), Dpi(18), Dpi(12)), Margin = new Padding(Dpi(20), 0, 0, Dpi(8)) };
            var lblDueCaption = new Label { Dock = DockStyle.Top, Height = Dpi(24), Text = "TOTAL AMOUNT DUE", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.AccentText };
            var lblDueNote = new Label { Dock = DockStyle.Bottom, Height = Dpi(64), Text = "= Net of VAT + fuel surcharge + VAT − withholding tax.\nVAT and the withholding tax are on the freight only. These come from the service invoices on this statement: to change an amount, correct the invoice in Billing.", Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.AccentText };
            numTotalAmountDue.Dock = DockStyle.Top;
            totalBox.Controls.Add(numTotalAmountDue);
            totalBox.Controls.Add(lblDueCaption);
            totalBox.Controls.Add(lblDueNote);
            var taxLayout = new TableLayoutPanel { Height = 3 * Dpi(66), ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            taxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            taxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            taxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            taxLayout.Controls.Add(taxFields, 0, 0);
            taxLayout.Controls.Add(totalBox, 1, 0);
            // ---------------- Sign-off
            txtSalesInvoiceNo = new TextBox { Font = new Font("Segoe UI Semibold", 10F), Text = "", ReadOnly = true, PlaceholderText = "Filled from the trips' service invoices" }; // Nov 6: not typed here any more
            txtPreparedBy = new TextBox { Font = new Font("Segoe UI", 10F), Text = "" };
            txtReceivedBy1 = new TextBox { Font = new Font("Segoe UI", 10F), Text = "" };
            txtReceivedBy2 = new TextBox { Font = new Font("Segoe UI", 10F), Text = "" };
            var gridSign = CreateFieldGrid(2,
                ("Service invoices on this statement (from Billing)", txtSalesInvoiceNo), ("Prepared by (billing officer)", txtPreparedBy),
                ("Received by 1 (client accountant)", txtReceivedBy1), ("Received by 2 (client accountant)", txtReceivedBy2));
            var lblSiHint = new Label
            {
                Height = Dpi(24),
                Text = "Service invoices are made in Billing and listed here automatically.",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.TopLeft
            };

            // ---------------- Cards
            var card1 = CreateSectionCard(1, "Client & SOA type", "Who the statement is for, and whether it bills normal dispatches or special trips (never both).", gridClient, lblPopulateHint);
            var cardPicker = CreateSectionCard(2, "Ready service invoices", "Tick the client's service invoices to put on this statement, or type a trip report or dispatch number.", BuildDispatchPicker());
            var card2 = CreateSectionCard(3, "Trips & stores", "Every trip and store billed on this statement.", bar, lblItemsHint, pnlItemsGridHost, lblItemsTotals);
            var card3 = CreateSectionCard(4, "Totals", "VAT, withholding tax and the amount the client pays. They follow the service invoices and can't be typed.", taxLayout, BuildSoaAdjustPanel());
            var card4 = CreateSectionCard(5, "Sign-off", "The service invoices on this statement and the people who prepare and receive it.", gridSign, lblSiHint);
            soaCardClient = card1; soaCardPicker = cardPicker; soaCardLines = card2; soaCardTotals = card3; soaCardSign = card4;
            pnlFormBody.Controls.Add(card4);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card3);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card2);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(cardPicker);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card1);
            pnlFormBody.Controls.Add(txtSoaNumber);
            pnlFormBody.ResumeLayout(true);
            // ---------------- Action bar (right-to-left: main action at the far right)
            btnSaveSoa = MakeFormButton("💾  Save statement", "primary", Dpi(210));
            btnSaveSoa.Height = Dpi(40);
            btnSaveSoa.Click += BtnSaveSoa_Click;
            btnPrintDrawer = MakeFormButton("🖨  Print SOA", "secondary", Dpi(150));
            btnPrintDrawer.Height = Dpi(40);
            btnPrintDrawer.Click += (s, e) => PrintCurrentDraftSoa();
            btnDeleteSoa = MakeFormButton("Cancel statement", "danger", Dpi(160));
            btnDeleteSoa.Height = Dpi(40);
            btnDeleteSoa.Click += BtnDeleteSoa_Click;
            btnClearForm = MakeFormButton("Clear form", "secondary", Dpi(130));
            btnClearForm.Height = Dpi(40);
            btnClearForm.Click += (s, e) => ClearForm();
            foreach (var b in new[] { btnSaveSoa, btnPrintDrawer, btnDeleteSoa, btnClearForm })
            {
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                flowFormActions.Controls.Add(b);
            }
        }
        private void StyleFullPageForm()
        {
            UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            UIThemeHelper.SetButtonRole(btnCloseForm, "secondary");
            btnCloseForm.ForeColor = AppTheme.TextPrimary;
            pnlForm.BackColor = AppTheme.Canvas;
            pnlFormBody.BackColor = AppTheme.Canvas;
            pnlFormHeaderBar.BackColor = AppTheme.Canvas;
            lblFormHeader.BackColor = AppTheme.Canvas;
            lblFormHeader.ForeColor = AppTheme.TextPrimary;
            numTotalAmountDue.BackColor = AppTheme.AccentSoft;
            numTotalAmountDue.ForeColor = AppTheme.AccentText;
            gridItems.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridItems.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridItems.ColumnHeadersHeight = Dpi(36);
            CenterFormContent();
        }
        private void SizeItemsGrid()
        {
            if (pnlItemsGridHost == null || gridItems == null) return;
            // Rows grow with their DR lists, so the table is sized from the real row heights.
            try { gridItems.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            int rowsH = gridItems.Rows.Cast<DataGridViewRow>().Sum(r => r.Height);
            int h = Math.Max(gridItems.ColumnHeadersHeight, Dpi(36)) + Math.Max(rowsH, 4 * Dpi(30)) + Dpi(22) + (sheetTotals?.TotalHeight ?? 0);
            pnlItemsGridHost.Height = Math.Max(h, Dpi(260)); // no inner scroll bar: the page grows and scrolls as one
        }
        private void UpdateItemsSummary()
        {
            UpdateSoaTypeLock();
            if (lblItemsTotals == null || gridItems == null) return;
            SizeItemsGrid();
            if (gridItems.Rows.Count == 0)
            {
                lblItemsTotals.Text = "No trips yet. Import completed trips above, or add rows by hand.";
                return;
            }
            decimal amt = 0, fuel = 0;
            var trips = new HashSet<string>();
            int unpriced = 0;
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if (row.IsNewRow) continue;
                decimal.TryParse(row.Cells["colAmount"].Value?.ToString(), out var a);
                decimal.TryParse(row.Cells["colFuel"].Value?.ToString(), out var f);
                amt += a; fuel += f;
                string trip = row.Cells["colTripNo"].Value?.ToString() ?? "";
                if (trips.Add(trip) && a <= 0)
                {
                    // the first row of each trip carries its price
                    unpriced++;
                }
            }
            lblItemsTotals.Text = $"{gridItems.Rows.Count} rows  ·  {trips.Count} trips  ·  Total Sales ₱{amt:N2} + Fuel Surcharge ₱{fuel:N2} = ₱{amt + fuel:N2} (VAT Inclusive)" +
                                  (unpriced > 0 ? $"   ·   ⚠ {unpriced} trip{(unpriced == 1 ? "" : "s")} without an amount" : "");
            lblItemsTotals.ForeColor = unpriced > 0 ? AppTheme.WarningText : AppTheme.TextSecondary;
        }
        private void OpenNewStatement()
        {
            if (keep.Kept) { keep.Resume(this, SetFormVisibility); return; } // a kept draft comes back as it was left, never blank
            isEditingExisting = false;
            ClearForm();
            ApplyFormMode();
            SetFormVisibility(true);
            RefreshPicker();
            cbClient.Focus();
        }
        private void OpenEditStatement(StatementOfAccount soa)
        {
            string why = MJdatabase.WhySoaCannotBeEdited(soa);
            if (why.Length > 0)
            {
                // Nov 8: it still opens, read-only, with its history (the reason is shown at the top of the page).
                var ci = MJdatabase.GetSoaCancelInfo(soa.Id);
                if (ci.Any) why += $"\n\nCancelled{(ci.By.Length > 0 ? " by " + ci.By : "")}{(ci.At.HasValue ? " on " + ci.At.Value.ToString("MMM d, yyyy h:mm tt") : "")}." + (ci.Reason.Length > 0 ? $"\nReason: {ci.Reason}" : "");
                selectedSoa = soa;
                BindSoaToDrawer(soa);
                isEditingExisting = true;
                ApplyFormMode();
                SetFormVisibility(true);
                EnterSoaViewOnly(soa, why);
                return;
            }
            selectedSoa = soa;
            BindSoaToDrawer(soa);
            isEditingExisting = true;
            ApplyFormMode();
            SetFormVisibility(true);
            RefreshPicker();
        }
        private void ApplyFormMode()
        {
            btnDeleteSoa.Visible = isEditingExisting;
            btnClearForm.Visible = !isEditingExisting;
            btnSaveSoa.Text = isEditingExisting ? "💾  Save changes" : "💾  Save statement";
            lblFormHeader.Text = isEditingExisting && selectedSoa != null
                ? $"Statement  ·  {selectedSoa.SoaNumber}" + (string.IsNullOrWhiteSpace(selectedSoa.SalesInvoiceNo) ? "" : $"  (SI {selectedSoa.SalesInvoiceNo})")
                : $"New Statement of Account  ·  {txtSoaNumber.Text}";
        }

        private void SetupGridColumns()
        {
            gridSoa.Columns.Clear();
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SoaNumber", HeaderText = "SOA Ref #", FillWeight = 110, MinimumWidth = 90, ReadOnly = true });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IssueDate", HeaderText = "Issue Date", FillWeight = 85, MinimumWidth = 75, ReadOnly = true, DefaultCellStyle = { Format = "yyyy-MM-dd" } });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client Company", FillWeight = 145, MinimumWidth = 110, ReadOnly = true });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PeriodCovered", HeaderText = "Period Covered", FillWeight = 120, MinimumWidth = 95, ReadOnly = true });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveryType", HeaderText = "Type", FillWeight = 50, MinimumWidth = 40, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalSalesVatInc", HeaderText = "Gross Sales", FillWeight = 95, MinimumWidth = 80, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "Total Due (PHP)", FillWeight = 105, MinimumWidth = 85, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) } });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalesInvoiceNo", HeaderText = "Service invoices", FillWeight = 80, MinimumWidth = 65, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) } });
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 75, MinimumWidth = 60, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridSoa, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not StatementOfAccount soa) return;
                selectedSoa = soa;
                m.Items.AddRange(RowMenu.Header($"📑  Statement {soa.SoaNumber}",
                    $"{RowMenu.Short(soa.ClientName, 28)} · {RowMenu.Money(soa.TotalAmount)} · {soa.PeriodCovered} · {soa.Status}"));
                m.Items.Add(RowMenu.Item("Edit statement", () => OpenEditStatement(soa), "Enter"));
                m.Items.Add(RowMenu.Good("Print official statement", () => PrintOfficialSoa(soa)));
                m.Items.Add(RowMenu.Item("Mark as sent to client", () =>
                {
                    MJdatabase.LogStatementIssued(soa.Id, soa.SoaNumber, soa.TotalAmount, "Sent");
                    ToastHelper.ShowToast(this, $"{soa.SoaNumber} marked as sent (PHP {soa.TotalAmount:N2}). If it changes later you will be told to resend it.", ToastType.Success);
                }, "", !string.Equals(soa.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)));
                var stale = MJdatabase.NeedsReissue(MJdatabase.DocSoa, soa.Id, soa.TotalAmount);
                if (stale != null) m.Items.Add(RowMenu.Item($"⚠ Changed since it was {stale.Kind.ToLowerInvariant()} ({stale.At:MMM d}, PHP {stale.Amount:N2}): reprint and resend", () => PrintOfficialSoa(soa)));
                m.Items.Add(RowMenu.Item(string.IsNullOrWhiteSpace(soa.SalesInvoiceNo) ? "No sales invoice linked" : $"Show sales invoice {soa.SalesInvoiceNo}", () => OpenLinkedInvoice(), "", !string.IsNullOrWhiteSpace(soa.SalesInvoiceNo)));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(soa.ClientName), "", !string.IsNullOrWhiteSpace(soa.ClientName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Statement number", soa.SoaNumber), ("Client", soa.ClientName), ("TIN", soa.ClientTin), ("Period covered", soa.PeriodCovered),
                    ("Sales invoice number", soa.SalesInvoiceNo), ("Total amount due", soa.TotalAmount.ToString("N2")),
                    ("Summary", $"SOA {soa.SoaNumber} — {soa.ClientName}, {soa.PeriodCovered}: {RowMenu.Money(soa.TotalAmount)} (VAT {RowMenu.Money(soa.VatAmount)}, EWT {RowMenu.Money(soa.WithholdingTax)})")));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other statements for this client", () => RowMenu.Search(txtGridSearch, soa.ClientName)),
                    RowMenu.Item("This client's invoices", () => FinanceHubView.Of(this)?.ShowInvoices(soa.ClientName))));
                m.Items.Add(RowMenu.ExportSub(gridSoa, "Statements_Of_Account", "Statements of Account", () => PrintHelper.PrintGridReport("Statements of Account", gridSoa, "Issued statements of account.")));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Cancel statement…", () => { selectedSoa = soa; CancelStatement(soa); }));
            });
            gridSoa.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && gridSoa.CurrentRow?.DataBoundItem is StatementOfAccount soa) { OpenEditStatement(soa); e.Handled = true; e.SuppressKeyPress = true; }
            };
            SetupItemsMenu();
        }
        /// <summary>Right-click on the trips table inside the statement form.</summary>
        private void SetupItemsMenu()
        {
            RowMenu.Attach(gridItems, (m, rows) =>
            {
                var picked = rows.Where(r => !r.IsNewRow).ToList();
                if (picked.Count == 0) return;
                var first = picked[0];
                string dest = first.Cells["colDest"].Value?.ToString() ?? "";
                int did = 0;
                int.TryParse(first.Cells["colDeliveryId"].Value?.ToString(), out did);
                m.Items.AddRange(RowMenu.Header(picked.Count > 1 ? $"{picked.Count} trip rows" : $"Trip {first.Cells["colTripNo"].Value} · {RowMenu.Short(dest, 34)}",
                    picked.Count > 1 ? "Change or remove them together" : (did > 0 ? "Filled from a trip report" : "Typed by hand")));
                m.Items.Add(RowMenu.Item(picked.Count > 1 ? $"Edit a column on {picked.Count} rows…" : "Edit a column…", () => BulkEditSelectedRows()));
                m.Items.Add(RowMenu.Item("Add a blank row", () => AddBlankGridRow()));
                if (did > 0) m.Items.Add(RowMenu.Item("Refresh lines from trip reports", () => RefreshLinesFromTripReports()));
                if (did > 0 && picked.Count == 1) m.Items.Add(RowMenu.Item("Open the dispatch", () => RowMenu.OpenDispatch(this, did, null)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger(picked.Count > 1 ? $"Remove {picked.Count} rows" : "Remove this row", () => RemoveSelectedRows()));
            }, allowMultiSelect: true);
        }
        /// <summary>Rows filled from a trip report: its dispatch must be completed, still have a saved trip report and not be on another statement.</summary>
        /// <summary>A special trip can be billed on one statement only.</summary>
        private bool CheckSpecialTripsBeforeSave()
        {
            if (!gridItems.Columns.Contains("colSpecialTrip")) return true;
            var ids = new HashSet<int>();
            foreach (DataGridViewRow r in gridItems.Rows)
                if (!r.IsNewRow && int.TryParse(r.Cells["colSpecialTrip"].Value?.ToString(), out int id) && id > 0)
                {
                    if (!ids.Add(id)) { MessageBox.Show(this, "The same special trip is on this statement twice. Remove one of its lines.", "Special Trip Already Added", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                }
            if (ids.Count == 0) return true;
            string thisSoa = isEditingExisting && selectedSoa != null ? selectedSoa.SoaNumber : "";
            var links = MJdatabase.GetSpecialTripLinks();
            var problems = ids.Where(id => links.TryGetValue(id, out var l) && l.OnSoa && !string.Equals(l.SoaNumber, thisSoa, StringComparison.OrdinalIgnoreCase))
                              .Select(id => { var t = MJdatabase.GetSpecialTripById(id); return $"• {t?.OrderReference ?? "#" + id} is already billed on {links[id].SoaNumber}"; }).ToList();
            if (problems.Count == 0) return true;
            MessageBox.Show(this, "These special trips are already on another statement:\n\n" + string.Join("\n", problems) + "\n\nRemove their lines from this statement.", "Special Trip Already Billed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        private bool CheckLinkedDispatchesBeforeSave()
        {
            if (!gridItems.Columns.Contains("colDeliveryId")) return true;
            var ids = new HashSet<int>();
            foreach (DataGridViewRow r in gridItems.Rows)
                if (!r.IsNewRow && int.TryParse(r.Cells["colDeliveryId"].Value?.ToString(), out int id) && id > 0) ids.Add(id);
            if (ids.Count == 0) return true;
            var billed = MJdatabase.GetBilledDeliveryIds(isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0);
            // Nov 6: a trip's own service invoice is now required before it goes on a statement (it is no longer "billed directly").
            var reported = MJdatabase.GetTripReportDeliveryIds();
            var problems = new List<string>();
            foreach (int id in ids)
            {
                var d = MJdatabase.GetDeliveryById(id);
                if (d == null) continue;
                if (!DispatchGate.IsCompleted(d.Status)) problems.Add($"{d.DispatchNumber} is still \"{d.Status}\" — mark it completed in Dispatch first");
                else if (billed.TryGetValue(id, out var other)) problems.Add($"{d.DispatchNumber} is already billed on {other}");
                // The statement is made from completed trip reports: a report voided after the trip was added can't be billed.
                else if (!reported.Contains(id)) problems.Add($"{d.DispatchNumber} has no saved trip report any more (it may have been voided) — file it again or remove these rows");
            }
            if (problems.Count == 0) return true;
            MessageBox.Show("This statement can't be saved yet:\n\n• " + string.Join("\n• ", problems) + "\n\nRemove those rows or fix the dispatches, then save again.", "Check the dispatches", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
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
            ExitSoaViewOnly(); // a read-only view never carries over to the next statement
            // Nov 6: a form is blank whenever it is opened or left.
            if (!visible && pnlForm.Visible && IsHandleCreated && !keep.Keeping) ClearForm();
            if (visible) formDirty ??= new FormDirtyTracker(pnlForm);
            if (!keep.Keeping && !(visible && keep.Resuming)) formDirty?.Reset();
            if (visible) keep.Restored(btnToggleForm);
            tblMasterContent.SuspendLayout();
            // The statement form is a full page of its own: it replaces the list instead of sitting beside it.
            pnlForm.Visible = visible;
            gridSoa.Visible = !visible;
            pnlTopBar.Visible = !visible;
            tblMasterContent.ColumnStyles[0].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
            tblMasterContent.ColumnStyles[0].Width = visible ? 100F : 0F;
            tblMasterContent.ColumnStyles[1].SizeType = visible ? SizeType.Absolute : SizeType.Percent;
            tblMasterContent.ColumnStyles[1].Width = visible ? 0F : 100F;
            tblMasterContent.Padding = visible ? new Padding(0) : new Padding(12);
            tblMasterContent.ResumeLayout(true);
            if (visible)
            {
                CenterFormContent();
                pnlFormBody.AutoScrollPosition = new Point(0, 0);
                UpdateItemsSummary();
                OnSoaFormOpened();
            }
            else OnSoaFormClosed();
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        private void OpenSoaTableEditor(Label bannerLabel)
        {
            if (gridItems == null || !Features.On(Features.StoreTableEditHeader)) return;
            var colSpecs = new List<SpecialTripColumnDef>();
            foreach (DataGridViewColumn col in gridItems.Columns)
            {
                if (col is DataGridViewButtonColumn) continue; // the "…" DR button is not data
                colSpecs.Add(new SpecialTripColumnDef
                {
                    Key = col.Name,
                    HeaderText = col.HeaderText,
                    Width = col.Width,
                    IsFill = false
                });
            }

            var rowDicts = new List<Dictionary<string, string>>();
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if (row.IsNewRow) continue;
                var dict = new Dictionary<string, string>();
                foreach (DataGridViewColumn col in gridItems.Columns)
                {
                    if (col is DataGridViewButtonColumn) continue;
                    dict[col.Name] = row.Cells[col.Name].Value?.ToString() ?? "";
                }
                rowDicts.Add(dict);
            }

            using var dlg = new SpecialTripTableEditorDialog(
                bannerLabel.Text,
                "Customize Statement of Account table columns, headers, delivery receipts, and billing rows.",
                colSpecs,
                rowDicts);

            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                bannerLabel.Text = dlg.EditedTableTitle;
                isBindingForm = true;
                try
                {
                    foreach (var spec in dlg.EditedColumns)
                    {
                        if (gridItems.Columns.Contains(spec.Key) && gridItems.Columns[spec.Key] is DataGridViewColumn existingCol)
                        {
                            existingCol.HeaderText = spec.HeaderText;
                        }
                        else
                        {
                            gridItems.Columns.Add(new DataGridViewTextBoxColumn
                            {
                                Name = spec.Key,
                                HeaderText = spec.HeaderText,
                                Width = Math.Max(70, spec.Width),
                                ReadOnly = false
                            });
                        }
                    }

                    gridItems.Rows.Clear();
                    foreach (var rDict in dlg.EditedRows)
                    {
                        int rIdx = gridItems.Rows.Add();
                        var gRow = gridItems.Rows[rIdx];
                        foreach (var kv in rDict)
                        {
                            if (gridItems.Columns.Contains(kv.Key) && gridItems.Columns[kv.Key] is not DataGridViewButtonColumn)
                            {
                                gRow.Cells[kv.Key].Value = kv.Key == "colDr" ? DrList.Normalize(kv.Value) : kv.Value;
                            }
                        }
                    }
                }
                finally
                {
                    isBindingForm = false;
                }
                RecalculateTaxes(manualOverride: false);
                SizeItemsGrid();
            }
        }

        private void LoadClients()
        {
            var clients = MJdatabase.GetLookupItems("Client Company");
            var clientSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "MORE THAN CAKES, INC.",
                "Millenium FoodBite Corporation",
                "BCS Group of Company",
                "Bestbake Incorporation",
                "Richbake Incorporation"
            };
            foreach (var c in clients) clientSet.Add(c);

            cbClient.Items.Clear();
            cbFilterClient.Items.Clear();
            cbFilterClient.Items.Add("All clients");
            foreach (var cl in clientSet.OrderBy(x => x))
            {
                cbClient.Items.Add(cl);
                cbFilterClient.Items.Add(cl);
            }

            cbClient.SelectedItem = "MORE THAN CAKES, INC.";
            if (cbClient.SelectedIndex < 0 && cbClient.Items.Count > 0) cbClient.SelectedIndex = 0;
            cbFilterClient.SelectedIndex = 0;
        }

        private void AutoFillClientDetails()
        {
            string cname = cbClient.Text.Trim();
            if (cname.Equals("MORE THAN CAKES, INC.", StringComparison.OrdinalIgnoreCase))
            {
                txtClientAddress.Text = "BCS CMPD., ALCANTARA ST., VIGAN CITY, ILOCOS SUR";
                txtClientTin.Text = "006-896-371-000";
            }
            else
            {
                var cl = MJdatabase.GetClientByCompanyName(cname);
                if (cl != null)
                {
                    txtClientAddress.Text = cl.Address;
                    txtClientTin.Text = string.IsNullOrWhiteSpace(cl.TIN) ? "" : cl.TIN;
                }
            }
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                allSoas = MJdatabase.GetStatementsOfAccount();
                ShowPaymentStateInList(allSoas);
                FilterGridData();
                if (pnlForm == null || !pnlForm.Visible) RefreshSoaReadyBanner();
            }
            catch (Exception ex)
            { ErrLog.Error("StatementOfAccountView", ex); 
                MessageBox.Show($"Failed to load Statements of Account: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Nov 6: a statement is Unpaid until its one collection receipt makes it Paid (no part payments). For the list only,
        /// a statement with no SI no. yet shows "No invoice". A paid statement can't be edited.
        /// </summary>
        private static void ShowPaymentStateInList(List<StatementOfAccount> list)
        {
            // Nov 6 workflow: a statement's status is stored (Unpaid until its receipt makes it Paid). The list only marks
            // statements that have no service invoice yet, and old statements that hold both normal and special trips.
            try
            {
                var withInvoices = MJdatabase.GetInvoices().Where(i => i.SoaId > 0).Select(i => i.SoaId).ToHashSet();
                foreach (var soa in list)
                {
                    if (soa.Status.Equals(PayStatus.Unpaid, StringComparison.OrdinalIgnoreCase) && !withInvoices.Contains(soa.Id) && string.IsNullOrWhiteSpace(soa.SalesInvoiceNo)) soa.Status = "No invoice";
                    if (soa.SoaType == SoaTypes.Mixed) soa.Status += " · Mixed (old)";
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        private void FilterGridData()
        {
            string q = txtGridSearch?.Text.Trim().ToLower() ?? "";
            string filterClient = cbFilterClient?.SelectedItem?.ToString() ?? "All clients";
            var filtered = allSoas.Where(soa =>
            {
                if (filterClient != "All clients" && !soa.ClientName.Equals(filterClient, StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrEmpty(q) && (paidChip?.SelectedIndex ?? 0) == 0 && string.Equals(soa.Status, PayStatus.Paid, StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrEmpty(q) && (cancelledChip?.SelectedIndex ?? 0) == 0 && (soa.Status ?? "").StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrEmpty(q))
                {
                    bool match = (soa.SoaNumber?.ToLower().Contains(q) ?? false) ||
                                 (soa.ClientName?.ToLower().Contains(q) ?? false) ||
                                 (soa.SalesInvoiceNo?.ToLower().Contains(q) ?? false) ||
                                 (soa.PeriodCovered?.ToLower().Contains(q) ?? false);
                    if (!match) return false;
                }
                return true;
            }).ToList();

            gridSoa.DataSource = null;
            gridSoa.DataSource = filtered;
            UpdateSoaSummary(filtered.ToList());
        }

        private void GridSoa_SelectionChanged()
        {
            // The form is filled when a statement is opened for editing, not on every click in the list.
            if (pnlForm.Visible) return;
            if (gridSoa.SelectedRows.Count > 0 && gridSoa.SelectedRows[0].DataBoundItem is StatementOfAccount soa)
            {
                selectedSoa = soa;
            }
        }

        private void BindSoaToDrawer(StatementOfAccount soa)
        {
            isBindingForm = true;
            txtSoaNumber.Text = soa.SoaNumber;
            cbClient.Text = soa.ClientName;
            txtClientAddress.Text = soa.ClientAddress;
            txtClientTin.Text = soa.ClientTin;
            dtpIssueDate.SetSafe(soa.IssueDate);
            var period = (soa.PeriodCovered ?? "").Split(new[] { " to " }, StringSplitOptions.None);
            if (period.Length == 2 && DateTime.TryParse(period[0].Trim(), out var pFrom) && DateTime.TryParse(period[1].Trim(), out var pTo))
            {
                dtpPeriodFrom.SetSafe(pFrom);
                dtpPeriodTo.SetSafe(pTo);
            }
            cbDeliveryType.SelectedItem = string.IsNullOrEmpty(soa.DeliveryType) ? "REF" : soa.DeliveryType;
            cbSoaType.SelectedIndex = soa.SoaType == SoaTypes.Special ? 1 : 0;
            txtSalesInvoiceNo.Text = soa.SalesInvoiceNo;
            txtPreparedBy.Text = soa.PreparedBy ?? "";
            txtReceivedBy1.Text = soa.ReceivedBy1 ?? "";
            txtReceivedBy2.Text = soa.ReceivedBy2 ?? "";

            numTotalSales.SetSafe(Math.Min(numTotalSales.Maximum, Math.Max(numTotalSales.Minimum, soa.TotalSalesVatInc)));
            numVat.SetSafe(Math.Min(numVat.Maximum, Math.Max(numVat.Minimum, soa.VatAmount)));
            numNetOfVat.SetSafe(Math.Min(numNetOfVat.Maximum, Math.Max(numNetOfVat.Minimum, soa.NetOfVat)));
            numFuelSurchargeTotal.SetSafe(Math.Min(numFuelSurchargeTotal.Maximum, Math.Max(numFuelSurchargeTotal.Minimum, soa.FuelSurchargeTotal)));
            numWithholdingTax.SetSafe(Math.Min(numWithholdingTax.Maximum, Math.Max(numWithholdingTax.Minimum, soa.WithholdingTax)));
            numTotalAmountDue.SetSafe(Math.Min(numTotalAmountDue.Maximum, Math.Max(numTotalAmountDue.Minimum, soa.TotalAmount)));

            var items = MJdatabase.GetStatementOfAccountItems(soa.Id);
            gridItems.Rows.Clear();
            foreach (var it in items)
            {
                int r = gridItems.Rows.Add(it.TripNumber, it.DateReceived, it.Zone, it.DrNumbers, it.Destination, it.DeliveryType, it.PlateNumber, it.Tonnage, it.DropCount, it.Amount, it.FuelSurcharge, it.DriverHelper, it.DeliveryId > 0 ? it.DeliveryId.ToString() : "");
                gridItems.Rows[r].Cells["colStoreCode"].Value = it.StoreCode;
                gridItems.Rows[r].Cells["colTripReport"].Value = it.TripReportNumber;
                gridItems.Rows[r].Cells["colCrates"].Value = it.TotalCrates;
                gridItems.Rows[r].Cells["colSpecialTrip"].Value = it.SpecialTripId > 0 ? it.SpecialTripId.ToString() : "";
            }

            isBindingForm = false;
            UpdateItemsSummary();
        }

        private void ClearForm()
        {
            selectedSoa = null;
            txtSoaNumber.Text = MJdatabase.GenerateNextSoaNumber();
            cbClient.SelectedIndex = -1; cbClient.Text = ""; txtClientAddress.Text = ""; txtClientTin.Text = ""; // Nov 6: blank until a client is picked
            dtpIssueDate.SetSafe(DateTime.Today);
            cbDeliveryType.SelectedIndex = -1;
            cbSoaType.SelectedIndex = 0;
            gridItems.Rows.Clear();
            txtSalesInvoiceNo.Text = "";
            txtPreparedBy.Text = "";
            txtReceivedBy1.Text = "";
            txtReceivedBy2.Text = "";
            numTotalSales.SetSafe(0);
            numVat.SetSafe(0);
            numNetOfVat.SetSafe(0);
            numFuelSurchargeTotal.SetSafe(0);
            numWithholdingTax.SetSafe(0);
            numTotalAmountDue.SetSafe(0);
            UpdateItemsSummary();
            if (pnlForm.Visible) ApplyFormMode();
        }

        /// <summary>Adds every completed, not-yet-billed dispatch for the client and period to the statement.</summary>
        private void PopulateTripsFromCompletedDeliveries()
        {
            if (dtpPeriodTo.Value.Date < dtpPeriodFrom.Value.Date)
            {
                MessageBox.Show("The period's end date is before its start date.", "Add trips", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(cbClient.Text) && !chkPickerAllClients.Checked)
            {
                QuietBox.Show("Pick the client first, so only their trips are added.", "Add trips", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cbClient.Focus();
                return;
            }
            RefreshPicker();
            var ready = gridPicker.Rows.Cast<DataGridViewRow>().Where(r => r.Tag is PickRow pr && pr.State == PickState.Ready).Select(r => ((PickRow)r.Tag!).Delivery).ToList();
            if (ready.Count == 0)
            {
                int needReport = pickerNeedReport;
                QuietBox.Show($"No completed trip reports are waiting to be billed for {(string.IsNullOrWhiteSpace(cbClient.Text) ? "this period" : cbClient.Text.Trim())}.\n\n"
                    + (needReport > 0 ? $"{needReport} completed trip{(needReport == 1 ? " still needs its" : "s still need their")} trip report (Operations → Trip reports) before {(needReport == 1 ? "it" : "they")} can be billed.\n\n" : "")
                    + "The list in step 2 shows which trips are already on this or another statement.", "Add trips", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddDeliveriesToStatement(ready);
        }

        private void AddBlankGridRow()
        {
            // A new line starts completely blank (system rule: no pre-filled rows).
            int r = gridItems.Rows.Add();
            if (r >= 0 && gridItems.Columns.Count > 0) { gridItems.CurrentCell = gridItems.Rows[r].Cells[0]; }
            RecalculateTaxes(manualOverride: false);
        }

        private void RemoveSelectedGridRow()
        {
            if (gridItems.SelectedCells.Count > 0)
            {
                int rIdx = gridItems.SelectedCells[0].RowIndex;
                if (rIdx >= 0 && rIdx < gridItems.Rows.Count)
                {
                    gridItems.Rows.RemoveAt(rIdx);
                    RecalculateTaxes(manualOverride: false);
                }
            }
        }
        private static void SetNum(NumericUpDown n, decimal v)
        {
            n.SetSafe(Math.Round(v, 2));
        }

        private void LiveCommitAmount(object? sender, EventArgs e)
        {
            try { if (!isBindingForm && gridItems.IsCurrentCellInEditMode && gridItems.IsCurrentCellDirty) gridItems.CommitEdit(DataGridViewDataErrorContexts.Commit); }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private void RecalculateTaxes(bool manualOverride)
        {
            if (isCalculatingTaxes) return;
            isCalculatingTaxes = true;
            try
            {
                if (!manualOverride)
                {
                    decimal amountSum = 0;
                    decimal fuelSum = 0;

                    foreach (DataGridViewRow row in gridItems.Rows)
                    {
                        if (row.IsNewRow) continue;
                        decimal rAmt = 0;
                        decimal rFuel = 0;
                        decimal.TryParse(row.Cells[9].Value?.ToString(), out rAmt);
                        decimal.TryParse(row.Cells[10].Value?.ToString(), out rFuel);

                        amountSum += rAmt;
                        fuelSum += rFuel;
                    }

                    // Nov 7: one formula (SoaTotals): VAT and the 2% withholding tax are on the freight only; the fuel surcharge is added outside both.
                    // The statement follows its trips' service invoices; an old statement that made its own invoice keeps the old rule.
                    decimal totalSales, vat, netOfVat, withholdingTax, totalDue;
                    var invTotals = InvoiceTotalsOfLines();
                    if (invTotals != null)
                    {
                        var t = SoaTotals.From(invTotals.Value.Freight, invTotals.Value.Fuel);
                        totalSales = t.TotalSales; vat = t.Vat; netOfVat = t.NetOfVat; withholdingTax = t.Withholding; totalDue = t.Due; fuelSum = t.Fuel;
                    }
                    else if (IsLegacySoa())
                    {
                        totalSales = amountSum + fuelSum;
                        vat = totalSales / 1.12m * 0.12m;
                        netOfVat = totalSales / 1.12m;
                        withholdingTax = netOfVat * 0.02m;
                        totalDue = netOfVat + vat - withholdingTax; // old rule: fuel inside the sales
                    }
                    else
                    {
                        var t = SoaTotals.From(amountSum, fuelSum);
                        totalSales = t.TotalSales; vat = t.Vat; netOfVat = t.NetOfVat; withholdingTax = t.Withholding; totalDue = t.Due; fuelSum = t.Fuel;
                    }
                    SetNum(numTotalSales, totalSales);
                    SetNum(numVat, vat);
                    SetNum(numNetOfVat, netOfVat);
                    SetNum(numFuelSurchargeTotal, fuelSum);
                    SetNum(numWithholdingTax, withholdingTax);
                    SetNum(numTotalAmountDue, totalDue);
                }
                else
                {
                    decimal totalSales = numTotalSales.Value;
                    decimal vat = numVat.Value;
                    decimal netOfVat = numNetOfVat.Value;
                    decimal fuel = numFuelSurchargeTotal.Value;
                    decimal wtax = numWithholdingTax.Value;
                    decimal totalDue = netOfVat + fuel + vat - wtax;
                    SetNum(numTotalAmountDue, totalDue);
                }
            }
            finally
            {
                isCalculatingTaxes = false;
            }
            UpdateItemsSummary();
        }

        private List<StatementOfAccountItem> CollectItemsFromGrid()
        {
            var list = new List<StatementOfAccountItem>();
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if (row.IsNewRow) continue;
                // A line left completely blank is ignored.
                bool blank = string.IsNullOrWhiteSpace(row.Cells["colStoreCode"].Value?.ToString()) && string.IsNullOrWhiteSpace(row.Cells["colTripReport"].Value?.ToString())
                             && ((row.Cells["colCrates"].Value?.ToString() ?? "").Trim() is "" or "0");
                for (int ci = 0; blank && ci <= Math.Min(11, row.Cells.Count - 1); ci++)
                    if (!string.IsNullOrWhiteSpace(row.Cells[ci].Value?.ToString())) blank = false;
                if (blank) continue;
                int tripNo = int.TryParse(row.Cells[0].Value?.ToString(), out int t) ? t : list.Count + 1;
                string dtRecv = row.Cells[1].Value?.ToString() ?? "";
                string zone = row.Cells[2].Value?.ToString() ?? "";
                string dr = DrList.Normalize(row.Cells[3].Value?.ToString());
                string dest = row.Cells[4].Value?.ToString() ?? "";
                string dtype = row.Cells[5].Value?.ToString() ?? "REF";
                string plate = row.Cells[6].Value?.ToString() ?? "";
                string tons = row.Cells[7].Value?.ToString() ?? "6T";
                int drops = int.TryParse(row.Cells[8].Value?.ToString(), out int dp) ? dp : 1;
                decimal amt = decimal.TryParse(row.Cells[9].Value?.ToString(), out decimal a) ? a : 0;
                decimal fuel = decimal.TryParse(row.Cells[10].Value?.ToString(), out decimal fl) ? fl : 0;
                string dh = row.Cells[11].Value?.ToString() ?? "";

                int deliveryId = gridItems.Columns.Contains("colDeliveryId") && int.TryParse(row.Cells["colDeliveryId"].Value?.ToString(), out int dId) ? dId : 0;
                list.Add(new StatementOfAccountItem
                {
                    TripNumber = tripNo,
                    DateReceived = dtRecv,
                    Zone = zone,
                    DrNumbers = dr,
                    Destination = dest,
                    DeliveryType = dtype,
                    PlateNumber = plate,
                    Tonnage = tons,
                    DropCount = drops,
                    Amount = amt,
                    FuelSurcharge = fuel,
                    DriverHelper = dh,
                    DeliveryId = deliveryId,
                    StoreCode = (row.Cells["colStoreCode"].Value?.ToString() ?? "").Trim(),
                    TripReportNumber = (row.Cells["colTripReport"].Value?.ToString() ?? "").Trim(),
                    TotalCrates = int.TryParse((row.Cells["colCrates"].Value?.ToString() ?? "").Replace(",", "").Trim(), out int tc) && tc > 0 ? tc : 0,
                    SpecialTripId = gridItems.Columns.Contains("colSpecialTrip") && int.TryParse(row.Cells["colSpecialTrip"].Value?.ToString(), out int stid) && stid > 0 ? stid : 0
                    // Crew pay never goes on a statement: it is internal and stays in Crew Management.
                });
            }
            return list;
        }

        private void BtnSaveSoa_Click(object? sender, EventArgs e)
        {
            if (soaSaving) return;
            // The checklist must be all green (the button is disabled otherwise; this also covers Enter / shortcuts).
            gridItems.EndEdit();
            UpdateSoaGuide();
            if (soaChecklist != null && soaChecklist.Failed > 0)
            {
                MessageBox.Show(this.FindForm(), "This statement can't be saved yet:\n\n• " + string.Join("\n• ", soaChecklist.Checks.Where(c => !c.Passed).Select(c => c.Text)),
                    "Check the statement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try { pnlFormBody.ScrollControlIntoView(soaChecklist); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                return;
            }
            soaSaving = true;
            btnSaveSoa.Enabled = false;
            try
            {
                if (string.IsNullOrWhiteSpace(cbClient.Text))
                {
                    MessageBox.Show("Please select or enter the client company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cbDeliveryType.SelectedIndex < 0) { MessageBox.Show("Please choose the delivery type (REF or DRY).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); cbDeliveryType.Focus(); return; } // Nov 6
                if (string.IsNullOrWhiteSpace(txtPreparedBy.Text)) { MessageBox.Show("Please type who prepared this statement (Prepared by).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtPreparedBy.Focus(); return; }

                if (gridItems.Rows.Count == 0)
                {
                    MessageBox.Show("Please add at least one trip or store line to the Statement of Account.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                gridItems.EndEdit();
                if (dtpPeriodTo.Value.Date < dtpPeriodFrom.Value.Date)
                {
                    MessageBox.Show("The period's end date is before its start date.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // A statement creates (or updates) the client's invoice, so it must be for a real amount.
                if (numTotalSales.Value < 0 || numTotalAmountDue.Value <= 0)
                {
                    MessageBox.Show(numTotalSales.Value < 0
                        ? $"Total sales is negative (PHP {numTotalSales.Value:N2}). Check the amounts and deductions on the trip lines."
                        : "The total amount due is PHP 0.00. Enter the amount for each trip line before saving.\n\nSaving creates an invoice for the client, so it must be more than PHP 0.00.",
                        "Check amounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                {
                    string tin = new string(txtClientTin.Text.Where(char.IsDigit).ToArray());
                    if (tin.Length != 9 && tin.Length != 12)
                    {
                        MessageBox.Show(FindForm(), tin.Length == 0 ? "The client's TIN is required on a statement. Type it in \"Client TIN\" (or add it in Master Data > Clients)." : "The TIN should have 9 digits (or 12 with the branch code), e.g. 006-896-371-000.", "Client TIN required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtClientTin.Focus();
                        return;
                    }
                    var noDr = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && DrList.Parse(r.Cells["colDr"].Value?.ToString()).Count == 0).Select(SoaLineLabel).ToList();
                    if (noDr.Count > 0)
                    {
                        MessageBox.Show(FindForm(), "A Delivery Receipt (DR) number is required on every trip line. Missing on:\n\n• " + string.Join("\n• ", noDr.Take(10)) + (noDr.Count > 10 ? $"\n• … and {noDr.Count - 10} more" : "") + "\n\nFill the DR numbers in the Trip Report (stores table), then Refresh lines from trip reports.", "DR number required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        gridItems.Focus();
                        return;
                    }
                    // Nov 10: a DR number is 4 to 8 digits.
                    var badDr = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).SelectMany(r => DrList.Parse(r.Cells["colDr"].Value?.ToString())).Where(d => !System.Text.RegularExpressions.Regex.IsMatch(d, @"^\d{4,8}$")).Distinct().ToList();
                    if (badDr.Count > 0)
                    {
                        MessageBox.Show(FindForm(), "A DR number must be 4 to 8 digits (numbers only). Fix:

• " + string.Join("
• ", badDr.Take(10)), "DR number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        gridItems.Focus();
                        return;
                    }
                }
                if (!CheckLinkedDispatchesBeforeSave()) return;
                if (!CheckSpecialTripsBeforeSave()) return;
                if (!CheckSalesInvoiceNoBeforeSave()) return;
                if (!CheckRepeatedDrsBeforeSave()) return;
                if (!IsLegacySoa() && !UseInvoiceTotals()) return; // Nov 6: the statement's total is its invoices' total
                var items = CollectItemsFromGrid();
                string period = $"{dtpPeriodFrom.Value:yyyy-MM-dd} to {dtpPeriodTo.Value:yyyy-MM-dd}";

                StatementOfAccount soa = isEditingExisting && selectedSoa != null ? selectedSoa : new StatementOfAccount();
                soa.SoaNumber = string.IsNullOrWhiteSpace(txtSoaNumber.Text) ? MJdatabase.GenerateNextSoaNumber() : txtSoaNumber.Text.Trim();
                soa.ClientName = cbClient.Text.Trim();
                soa.ClientAddress = txtClientAddress.Text.Trim();
                soa.ClientTin = txtClientTin.Text.Trim();
                soa.PeriodCovered = period;
                soa.IssueDate = dtpIssueDate.Value.Date;
                soa.DeliveryType = cbDeliveryType.Text.Trim();
                soa.TotalSalesVatInc = numTotalSales.Value;
                soa.VatAmount = numVat.Value;
                soa.NetOfVat = numNetOfVat.Value;
                soa.FuelSurchargeTotal = numFuelSurchargeTotal.Value;
                soa.WithholdingTax = numWithholdingTax.Value;
                soa.TotalAmount = numTotalAmountDue.Value;
                soa.SalesInvoiceNo = txtSalesInvoiceNo.Text.Trim();
                soa.PreparedBy = txtPreparedBy.Text.Trim();
                soa.ReceivedBy1 = txtReceivedBy1.Text.Trim();
                soa.ReceivedBy2 = txtReceivedBy2.Text.Trim();
                if (!isEditingExisting) { soa.Status = PayStatus.Unpaid; soa.SoaType = CurrentSoaType; }
                soa.TripRemovalReason = "";
                if (isEditingExisting && soa.Id > 0 && !IsLegacySoa(soa.Id) && !AskWhyTripsRemoved(soa, items, out string removalReason)) return;
                else if (isEditingExisting) soa.TripRemovalReason = removalReasonCache;

                int savedId = MJdatabase.SaveStatementOfAccount(soa, items);
                soa.Id = savedId;
                try
                {
                    bool adjusted = SoaTotalsAdjusted(out var expectedDue, out var enteredDue);
                    MJdatabase.SaveAdjustmentReason("StatementsOfAccount", savedId, soa.SoaNumber, adjusted ? txtSoaAdjustReason.Text.Trim() : "", expectedDue, enteredDue);
                }
                catch (Exception ex) { ErrLog.Error("StatementOfAccountView.AdjustmentReason", ex); }
                selectedSoa = soa;

                string siNo = soa.SalesInvoiceNo.Trim();
                string siInv = siNo.StartsWith("SI", StringComparison.OrdinalIgnoreCase) ? siNo : $"SI-{siNo}";
                if (IsLegacySoa(savedId))
                {
                    QuietBox.Show(IsLegacySoa(savedId)
                            ? (siNo.Length > 0 ? $"Statement of Account #{soa.SoaNumber} is saved.\n\nBilling invoice {siInv} for PHP {soa.TotalAmount:N2} is linked to it (see Billing Invoices)." : $"Statement of Account #{soa.SoaNumber} is saved.")
                            : $"Statement of Account #{soa.SoaNumber} is saved with its service invoice(s): {soa.SalesInvoiceNo}.\n\nCollect it in Collections with ONE receipt for PHP {soa.TotalAmount:N2}.",
                        "SOA saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                    SetFormVisibility(false);
                    PrintOfficialSoa(soa);
                }
                else
                {
                    LoadData();
                    SetFormVisibility(false);
                    // Nov 9 (statement first): straight on to the Service Invoice list, where each trip's invoice number is TYPED and its amount is locked to this statement.
                    // When every trip already has its invoice, straight on to the Collection Receipt as before.
                    bool needInvoices = MJdatabase.GetSoaTripsWithoutInvoice().ContainsKey(savedId);
                    int next = needInvoices
                        ? ChoiceDialog.Show(FindForm(), "SOA saved", $"Statement of Account {soa.SoaNumber} is saved",
                            $"{(soa.SoaType == SoaTypes.Special ? "Special Trips SOA" : "Normal Operations SOA")} for {soa.ClientName}.\n\nTotal amount due: PHP {soa.TotalAmount:N2}.\n\nNext: Service Invoice. Type each trip's invoice number; the amount is taken from this statement and can't be changed.",
                            "➜  Issue Invoice", "🖨  Print SOA", "Close")
                        : ChoiceDialog.Show(FindForm(), "SOA saved", $"Statement of Account {soa.SoaNumber} is saved",
                            $"{(soa.SoaType == SoaTypes.Special ? "Special Trips SOA" : "Normal Operations SOA")} for {soa.ClientName} with service invoice(s) {soa.SalesInvoiceNo}.\n\nTotal amount due: PHP {soa.TotalAmount:N2}. It is collected with ONE receipt for the full amount.",
                            "➜  Proceed to Collection Receipt", "🖨  Print SOA", "Close");
                    if (next == 0) { if (needInvoices) FinanceHubView.Of(this)?.InvoiceStatement(savedId); else FinanceHubView.Of(this)?.ProceedToCollection(savedId); }
                    else if (next == 1) PrintOfficialSoa(soa);
                }
            }
            catch (Exception ex)
            { ErrLog.Error("StatementOfAccountView", ex); 
                MessageBox.Show($"Failed to save Statement of Account: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                soaSaving = false;
                if (pnlForm.Visible) UpdateSoaGuide(); else btnSaveSoa.Enabled = true;
            }
        }
        /// <summary>
        /// The invoice is made from the sales invoice no. Blank: ask before saving without an invoice.
        /// Already used by another statement's invoice: block, so that invoice is not moved onto this statement.
        /// </summary>
        private bool CheckSalesInvoiceNoBeforeSave()
        {
            if (!IsLegacySoa()) return true; // Nov 6: the statement uses its trips' service invoices (typed in Billing)
            string raw = txtSalesInvoiceNo.Text.Trim();
            if (raw.Length == 0)
            {
                var ans = MessageBox.Show("The sales invoice no. (SI no.) is blank.\n\nThe statement can be saved, but no billing invoice is created until you add the SI no. (Edit statement), so it can't be collected yet.\n\nSave without an invoice?",
                    "No sales invoice no.", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (ans != DialogResult.Yes) { try { txtSalesInvoiceNo.Focus(); } catch { } return false; }
                return true;
            }
            string invNum = raw.StartsWith("SI", StringComparison.OrdinalIgnoreCase) ? raw : $"SI-{raw}";
            int selfId = isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0;
            try
            {
                var clash = MJdatabase.GetInvoices().FirstOrDefault(i => i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase) && (selfId == 0 || i.SoaId != selfId));
                if (clash != null)
                {
                    string owner = clash.SoaId > 0 ? (MJdatabase.GetStatementsOfAccount().FirstOrDefault(x => x.Id == clash.SoaId)?.SoaNumber ?? "another statement") : "another record";
                    MessageBox.Show($"Sales invoice no. {raw} is already used by invoice {clash.InvoiceNumber} ({clash.ClientName}, {owner}, {clash.Status}).\n\nEach statement needs its own SI no. Check the number on the printed sales invoice.",
                        "SI no. already used", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try { txtSalesInvoiceNo.Focus(); txtSalesInvoiceNo.SelectAll(); } catch { }
                    return false;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return true;
        }
        private int CurrentSoaId => isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0;
        private static string SoaLineLabel(DataGridViewRow r) => $"trip {r.Cells["colTripNo"].Value} {(r.Cells["colDest"].Value?.ToString() ?? "").Trim()}".Trim();
        /// <summary>
        /// A DR number can be billed only once. DRs just entered on <paramref name="row"/> that are already on another
        /// line of this statement, or on another (not cancelled) statement, are taken out again and the person is told where.
        /// </summary>
        private void RejectDuplicateSoaDrs(DataGridViewRow row)
        {
            if (gridItems == null || row == null || row.Index < 0 || row.DataGridView != gridItems) return;
            var drs = DrList.Parse(row.Cells["colDr"].Value?.ToString());
            if (drs.Count == 0) return;
            var here = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow other in gridItems.Rows)
            {
                if (other.IsNewRow || other == row) continue;
                foreach (var dr in DrList.Parse(other.Cells["colDr"].Value?.ToString()))
                    if (!here.ContainsKey(dr)) here[dr] = SoaLineLabel(other);
            }
            var billed = MJdatabase.FindDrOwnersOnOtherStatements(CurrentSoaId, drs.Where(d => !here.ContainsKey(d)));
            var keep = new List<string>();
            var refused = new List<string>();
            foreach (var dr in drs)
            {
                if (here.TryGetValue(dr, out var line)) refused.Add($"{dr} is already on {line} of this statement");
                else if (billed.TryGetValue(dr, out var soa)) refused.Add($"{dr} is already billed on {soa}");
                else keep.Add(dr);
            }
            if (refused.Count == 0) return;
            bool was = isBindingForm;
            isBindingForm = true;
            try { row.Cells["colDr"].Value = DrList.Join(keep); }
            finally { isBindingForm = was; }
            MessageBox.Show(this.FindForm(), $"A DR number can only be billed once, so {(refused.Count == 1 ? "this one was" : "these were")} not added:\n\n"
                + string.Join("\n", refused.Take(10).Select(x => "• " + x)) + (refused.Count > 10 ? $"\n• … and {refused.Count - 10} more" : ""),
                "DR number already used", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        /// <summary>
        /// Before saving: DR numbers must not repeat, neither on two lines of this statement nor on another (not cancelled)
        /// statement. The save stops until they are corrected (no "save anyway").
        /// </summary>
        private bool CheckRepeatedDrsBeforeSave()
        {
            var where = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if (row.IsNewRow) continue;
                foreach (var dr in DrList.Parse(row.Cells["colDr"].Value?.ToString()))
                {
                    if (!where.TryGetValue(dr, out var l)) where[dr] = l = new List<string>();
                    l.Add(SoaLineLabel(row));
                }
            }
            var dups = where.Where(k => k.Value.Count > 1).Select(k => $"{k.Key} is on {string.Join(" and ", k.Value)}").ToList();
            var billed = MJdatabase.FindDrOwnersOnOtherStatements(CurrentSoaId, where.Keys).Select(kv => $"{kv.Key} is already billed on {kv.Value}").ToList();
            if (dups.Count == 0 && billed.Count == 0) return true;
            string L(List<string> l) => string.Join("\n", l.Take(8).Select(x => "• " + x)) + (l.Count > 8 ? $"\n• … and {l.Count - 8} more" : "");
            MessageBox.Show((dups.Count > 0 ? $"The same DR number is on more than one line:\n{L(dups)}\n\n" : "")
                + (billed.Count > 0 ? $"These DR numbers are already on another statement:\n{L(billed)}\n\n" : "")
                + "A DR number can only be billed once. Correct the DR numbers, then save again.",
                "DR numbers repeated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void BtnDeleteSoa_Click(object? sender, EventArgs e)
        {
            if (selectedSoa == null || (pnlForm.Visible && !isEditingExisting))
            {
                MessageBox.Show("Please select a Statement of Account to cancel.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            CancelStatement(selectedSoa);
        }

        /// <summary>
        /// Cancels a statement: asks why, says what happens (its service invoices return to "Ready for a statement"), and keeps the reason.
        /// A paid statement can't be cancelled until its collection receipt is voided (offers to open Collections).
        /// </summary>
        private string removalReasonCache = "";

        /// <summary>
        /// Saving a SAVED statement whose lines no longer include a trip that had a service invoice: say what happens, ask why, and refuse to
        /// leave the statement with no trips (cancel it instead). Returns false = don't save.
        /// </summary>
        private bool AskWhyTripsRemoved(StatementOfAccount soa, List<StatementOfAccountItem> items, out string reason)
        {
            reason = ""; removalReasonCache = "";
            try
            {
                var keep = new HashSet<(int, int)>(items.Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).Select(i => (i.DeliveryId > 0 ? i.DeliveryId : 0, i.DeliveryId > 0 ? 0 : i.SpecialTripId)));
                var onSoa = MJdatabase.GetTripInvoices().Where(i => i.SoaId == soa.Id && (i.DeliveryId > 0 || i.SpecialTripId > 0)).ToList();
                var dropped = onSoa.Where(i => !keep.Contains((i.DeliveryId > 0 ? i.DeliveryId : 0, i.DeliveryId > 0 ? 0 : i.SpecialTripId))).ToList();
                if (dropped.Count == 0) return true;
                if (keep.Count == 0) { QuietBox.Show("A statement needs at least one trip. To drop them all, cancel the statement instead.", "Take trips off", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
                var stay = onSoa.Except(dropped).ToList();
                // Which of the invoices that stay were already printed or sent (the statement's own print counts for all of its invoices), with their new net.
                var issuedLines = new List<string>();
                if (stay.Count > 0)
                {
                    var tt = SoaTotals.From(stay.Sum(i => i.Freight), stay.Sum(i => i.Fuel));
                    var sh = SoaTotals.Share(tt.Withholding, stay.Select(i => i.Freight).ToArray());
                    for (int k = 0; k < stay.Count; k++)
                    {
                        var inv = stay[k];
                        var was = MJdatabase.GetIssuedInfo(MJdatabase.DocInvoice, inv.Id);
                        decimal newNet = inv.Gross - sh[k];
                        if (was != null && Math.Abs(was.Amount - newNet) > 0.004m)
                            issuedLines.Add($"SI {inv.Number} was {was.Kind.ToLowerInvariant()} on {was.At:MMM d} at PHP {was.Amount:N2}; its new net is PHP {newNet:N2}");
                    }
                }
                string msg = $"You are taking {dropped.Count} trip(s) off {soa.SoaNumber}:\n\n• " + string.Join("\n• ", dropped.Take(10).Select(i => $"SI {i.Number} (PHP {i.Gross:N2})"))
                    + "\n\nEach goes back to a lone unpaid invoice with the amount it had before the statement, and its trip report returns to the statement queue."
                    + (stay.Count > 0 ? $"\n\nThe 2% withholding is shared out again, so the net amount of the {stay.Count} invoice(s) that stay changes too." : "")
                    + (issuedLines.Count > 0 ? "\n\nAlready printed or sent, so reprint and resend:\n• " + string.Join("\n• ", issuedLines.Take(8)) : "")
                    + "\n\nWhy are they taken off?";
                if (!AskReason("Take trips off the statement", msg, out reason)) return false;
                removalReasonCache = reason;
                return true;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return true; }
        }

        private bool AskReason(string title, string message, out string reason)
        {
            reason = "";
            using var f = new Form { Text = title, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(Dpi(560), Dpi(380)), BackColor = AppTheme.Surface };
            var lbl = new Label { Dock = DockStyle.Top, Height = Dpi(250), Padding = new Padding(Dpi(16), Dpi(14), Dpi(16), 0), Text = message, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextPrimary };
            var tb = new TextBox { Dock = DockStyle.Top, Multiline = true, Height = Dpi(56), Font = new Font("Segoe UI", 10F), Margin = new Padding(Dpi(16)) };
            var host = new Panel { Dock = DockStyle.Top, Height = Dpi(70), Padding = new Padding(Dpi(16), Dpi(6), Dpi(16), Dpi(6)) };
            host.Controls.Add(tb);
            var ok = new Button { Text = "Take them off and save", DialogResult = DialogResult.None, Width = Dpi(200), Height = Dpi(36), Dock = DockStyle.Right };
            var no = new Button { Text = "Keep the trips", DialogResult = DialogResult.Cancel, Width = Dpi(140), Height = Dpi(36), Dock = DockStyle.Right };
            var bar = new Panel { Dock = DockStyle.Bottom, Height = Dpi(52), Padding = new Padding(Dpi(16), Dpi(8), Dpi(16), Dpi(8)) };
            bar.Controls.Add(ok); bar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = Dpi(8) }); bar.Controls.Add(no);
            ok.Click += (s, e) =>
            {
                if (tb.Text.Trim().Length < MJdatabase.MinCancelReasonLength) { MessageBox.Show(f, $"Write why (at least {MJdatabase.MinCancelReasonLength} characters).", title, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                f.DialogResult = DialogResult.OK;
            };
            f.Controls.Add(bar); f.Controls.Add(host); f.Controls.Add(lbl);
            f.CancelButton = no;
            if (f.ShowDialog(FindForm()) != DialogResult.OK) return false;
            reason = tb.Text.Trim();
            return true;
        }

        private void CancelStatement(StatementOfAccount soa)
        {
            string why = MJdatabase.WhySoaCannotBeCancelled(soa);
            if (why.Length > 0)
            {
                if (MJdatabase.LiveReceiptCountForSoa(soa.Id) > 0)
                {
                    var go = MessageBox.Show(FindForm(), why + "\n\nOpen Collections & Receipts now?", "Cancel statement", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (go == DialogResult.Yes) FinanceHubView.Of(this)?.ShowCollections(soa.SoaNumber);
                }
                else QuietBox.Show(why, "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int invoices = 0;
            try { invoices = MJdatabase.GetTripInvoices().Count(i => i.SoaId == soa.Id && i.Kind.Length > 0); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (!Forms.CancelStatementDialog.Prompt(FindForm(), soa.SoaNumber, soa.ClientName, invoices, soa.TotalAmount, out string reason)) return;
            try
            {
                MJdatabase.CancelStatementOfAccount(soa.Id, reason);
                ToastHelper.ShowToast(this, MJdatabase.LastDeleteSummary, ToastType.Info);
                ClearForm();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                ErrLog.Error("StatementOfAccountView", ex);
                MessageBox.Show($"The statement could not be cancelled: {ex.Message}", "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintSelectedSoa()
        {
            if (selectedSoa == null && gridSoa.SelectedRows.Count > 0 && gridSoa.SelectedRows[0].DataBoundItem is StatementOfAccount item)
            {
                selectedSoa = item;
            }

            if (selectedSoa != null)
            {
                PrintOfficialSoa(selectedSoa);
            }
            else
            {
                QuietBox.Show("Please select a Statement of Account from the table to print.", "Select SOA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PrintCurrentDraftSoa()
        {
            var items = CollectItemsFromGrid();
            var soa = new StatementOfAccount
            {
                SoaNumber = string.IsNullOrWhiteSpace(txtSoaNumber.Text) ? "SOA-DRAFT" : txtSoaNumber.Text.Trim(),
                ClientName = cbClient.Text.Trim(),
                ClientAddress = txtClientAddress.Text.Trim(),
                ClientTin = txtClientTin.Text.Trim(),
                PeriodCovered = $"{dtpPeriodFrom.Value:yyyy-MM-dd} to {dtpPeriodTo.Value:yyyy-MM-dd}",
                IssueDate = dtpIssueDate.Value.Date,
                DeliveryType = cbDeliveryType.Text.Trim(),
                TotalSalesVatInc = numTotalSales.Value,
                VatAmount = numVat.Value,
                NetOfVat = numNetOfVat.Value,
                FuelSurchargeTotal = numFuelSurchargeTotal.Value,
                WithholdingTax = numWithholdingTax.Value,
                TotalAmount = numTotalAmountDue.Value,
                SalesInvoiceNo = txtSalesInvoiceNo.Text.Trim(),
                PreparedBy = txtPreparedBy.Text.Trim(),
                ReceivedBy1 = txtReceivedBy1.Text.Trim(),
                ReceivedBy2 = txtReceivedBy2.Text.Trim()
            };
            PrintHelper.PrintOfficialStatementOfAccount(soa, items);
        }

        private void PrintOfficialSoa(StatementOfAccount soa)
        {
            var items = MJdatabase.GetStatementOfAccountItems(soa.Id);
            PrintHelper.PrintOfficialStatementOfAccount(soa, items);
            MJdatabase.LogStatementIssued(soa.Id, soa.SoaNumber, soa.TotalAmount, "Printed"); // so a later change can warn that the client's copy is out of date
        }

        private void OpenLinkedInvoice()
        {
            if (selectedSoa == null && gridSoa.SelectedRows.Count > 0 && gridSoa.SelectedRows[0].DataBoundItem is StatementOfAccount item)
            {
                selectedSoa = item;
            }

            if (selectedSoa != null && !string.IsNullOrWhiteSpace(selectedSoa.SalesInvoiceNo))
            {
                for (Control? p = this.Parent; p != null; p = p.Parent)
                {
                    if (p is FinanceHubView hub)
                    {
                        hub.ShowInvoices(selectedSoa.SalesInvoiceNo);
                        return;
                    }
                }
                QuietBox.Show($"This Statement of Account is linked to Billing Invoice #{selectedSoa.SalesInvoiceNo}.\nPlease open the Billing Invoices tab to view it.", "Linked Invoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                QuietBox.Show("Please select an issued Statement of Account with a linked Sales Invoice.", "Select SOA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) DarkModeKit.StopAutoComplete(this);
            DarkModeKit.RunWithoutFalseCrossThreadCheck(() => base.Dispose(disposing), nameof(StatementOfAccountView));
        }
    }
}
