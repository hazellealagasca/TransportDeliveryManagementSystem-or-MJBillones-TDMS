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
    public partial class StatementOfAccountView : UserControl, IRefreshable
    {
        private List<StatementOfAccount> allSoas = new List<StatementOfAccount>();
        private StatementOfAccount? selectedSoa = null;
        private bool isBindingForm = false;
        private bool isCalculatingTaxes = false;

        private FlowLayoutPanel pnlGridActionsFlow = null!;
        private TextBox txtGridSearch = null!;
        private ComboBox cbFilterClient = null!;
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
        private float currentFormWidth = 420F;

        private TextBox txtSoaNumber = null!;
        private ComboBox cbClient = null!;
        private TextBox txtClientAddress = null!;
        private TextBox txtClientTin = null!;
        private DateTimePicker dtpIssueDate = null!;
        private ComboBox cbDeliveryType = null!;
        private DateTimePicker dtpPeriodFrom = null!;
        private DateTimePicker dtpPeriodTo = null!;
        private Button btnPopulateTrips = null!;

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
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
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
                Height = Dpi(56),
                BackColor = Pal.White,
                Padding = new Padding(12, 8, 12, 4)
            };

            pnlGridActionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
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
                Text = "🖨️ Print Official SOA",
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
                Text = "🧾 View Linked Invoice",
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

            var lblSearch = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(8, 7, 4, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            txtGridSearch = new TextBox { Width = 150, Margin = new Padding(0, 4, 12, 0) };
            txtGridSearch.TextChanged += (s, e) => FilterGridData();

            var lblFilter = new Label { Text = "Client:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            cbFilterClient = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 0, 0) };
            cbFilterClient.SelectedIndexChanged += (s, e) => FilterGridData();

            pnlGridActionsFlow.Controls.Add(lblSearch);
            pnlGridActionsFlow.Controls.Add(txtGridSearch);
            pnlGridActionsFlow.Controls.Add(lblFilter);
            pnlGridActionsFlow.Controls.Add(cbFilterClient);

            pnlTopBar.Controls.Add(pnlGridActionsFlow);
            this.Controls.Add(pnlTopBar);

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
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);

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
        }
        private void CenterFormContent()
        {
            if (pnlForm == null || pnlForm.Width <= 0) return;
            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);
            pnlFormHeaderBar.Padding = new Padding(side, Dpi(6), side, Dpi(12));
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
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) AutoFillClientDetails(); SchedulePickerRefresh(); };
            cbClient.TextChanged += (s, e) => SchedulePickerRefresh();
            txtClientAddress = new TextBox { Font = new Font("Segoe UI", 10F), Text = "BCS CMPD., ALCANTARA ST., VIGAN CITY, ILOCOS SUR" };
            txtClientTin = new TextBox { Font = new Font("Segoe UI", 10F), Text = "006-896-371-000" };
            cbDeliveryType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cbDeliveryType.Items.AddRange(new object[] { "REF", "DRY" });
            cbDeliveryType.SelectedIndex = 0;
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
            void SetPeriod(DateTime from, DateTime to) { dtpPeriodFrom.Value = from; dtpPeriodTo.Value = to; }
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
                Text = "The client's completed dispatches in this period are listed in step 2. Add them all here, or tick the ones you want below.",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var gridClient = CreateFieldGrid(2,
                ("Billed to (client company)", cbClient), ("Client address", txtClientAddress),
                ("Client TIN", txtClientTin), ("Delivery type", cbDeliveryType),
                ("Statement date", dtpIssueDate), ("Period covered", pnlDates),
                ("Quick period", flowPeriodPresets), ("Import trips", btnPopulateTrips));
            // ---------------- Trips & stores grid
            var bar = new TableLayoutPanel { Height = Dpi(46), ColumnCount = 6, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++) bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblItemsTitle = new Label { Dock = DockStyle.Fill, Text = "🧾 Trips & stores on this statement", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            btnAddRow = MakeFormButton("➕ Add row", "secondary");
            btnAddRow.Click += (s, e) => AddBlankGridRow();
            btnDeleteRow = MakeFormButton("Remove selected", "secondary");
            btnDeleteRow.Click += (s, e) => RemoveSelectedRows();
            var btnBulkEdit = MakeFormButton("✎ Edit selected rows", "secondary");
            btnBulkEdit.Click += (s, e) => BulkEditSelectedRows();
            var btnEditSoaTable = MakeFormButton("✎ Edit table", "secondary");
            btnEditSoaTable.Click += (s, e) => OpenSoaTableEditor(lblItemsTitle);
            btnRecalc = MakeFormButton("↻ Recalculate", "secondary");
            btnRecalc.Margin = new Padding(0);
            btnRecalc.Click += (s, e) => RecalculateTaxes(manualOverride: false);
            bar.Controls.Add(lblItemsTitle, 0, 0);
            bar.Controls.Add(btnAddRow, 1, 0);
            bar.Controls.Add(btnBulkEdit, 2, 0);
            bar.Controls.Add(btnDeleteRow, 3, 0);
            bar.Controls.Add(btnEditSoaTable, 4, 0);
            bar.Controls.Add(btnRecalc, 5, 0);
            var lblItemsHint = new Label
            {
                Height = Dpi(24),
                Text = "Yellow cells can be edited. Shift/Ctrl-click to select several rows, then \"Edit selected rows\" to change them all at once.",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true
            };

            pnlItemsGridHost = new Panel { Height = Dpi(300), BackColor = AppTheme.Surface };
            gridItems = new DataGridView
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
                RowTemplate = { Height = Dpi(30) },
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
            gridItems.Columns.Add(Col("colDate", "Date received", 90, editStyle, 80));
            gridItems.Columns.Add(Col("colZone", "Zone", 52, editStyle));
            gridItems.Columns.Add(Col("colDr", "Delivery receipt #", 130, editStyle, 90));
            gridItems.Columns.Add(Col("colDest", "Destination / store", 190, editStyle, 120));
            gridItems.Columns.Add(Col("colType", "Type", 50, editStyle));
            gridItems.Columns.Add(Col("colPlate", "Plate no.", 80, editStyle, 70));
            gridItems.Columns.Add(Col("colTons", "Tons", 48, editStyle));
            gridItems.Columns.Add(Col("colDrops", "Drops", 50, editStyle));
            gridItems.Columns.Add(Col("colAmount", "Amount", 92, moneyStyle, 80));
            gridItems.Columns.Add(Col("colFuel", "Fuel surch.", 86, moneyStyle, 72));
            gridItems.Columns.Add(Col("colDriver", "Driver / helper", 140, editStyle, 100));
            gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeliveryId", HeaderText = "Delivery Id", Visible = false });
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
            pnlItemsGridHost.Controls.Add(gridItems);
            lblItemsTotals = new Label
            {
                Height = Dpi(40),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = AppTheme.TextSecondary,
                Text = "No trips yet. Import completed trips above, or add rows by hand."
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
                    TextAlign = HorizontalAlignment.Right
                };
                n.ValueChanged += (s, e) => { if (!isCalculatingTaxes) RecalculateTaxes(manualOverride: true); };
                return n;
            }

            numTotalSales = MakeNum();
            numVat = MakeNum();
            numNetOfVat = MakeNum();
            numFuelSurchargeTotal = MakeNum();
            numWithholdingTax = MakeNum();
            numTotalAmountDue = MakeNum();
            numTotalAmountDue.Font = new Font("Segoe UI Semibold", 18F);
            numTotalAmountDue.BorderStyle = BorderStyle.None;
            numTotalAmountDue.BackColor = AppTheme.AccentSoft;
            numTotalAmountDue.ForeColor = AppTheme.AccentText;
            var taxFields = CreateFieldGrid(2,
                ("Total sales (VAT inclusive)", numTotalSales), ("Less: VAT (12%)", numVat),
                ("Amount net of VAT", numNetOfVat), ("Fuel surcharge (included in sales)", numFuelSurchargeTotal),
                ("Less: withholding tax (2% EWT)", numWithholdingTax), null);
            taxFields.Dock = DockStyle.Fill;
            var totalBox = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.AccentSoft, Padding = new Padding(Dpi(18), Dpi(14), Dpi(18), Dpi(12)), Margin = new Padding(Dpi(20), 0, 0, Dpi(8)) };
            var lblDueCaption = new Label { Dock = DockStyle.Top, Height = Dpi(24), Text = "TOTAL AMOUNT DUE", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.AccentText };
            var lblDueNote = new Label { Dock = DockStyle.Bottom, Height = Dpi(64), Text = "= Net of VAT + VAT − withholding tax.\nFigures are worked out from the table; change any of them to override.", Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.AccentText };
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
            txtSalesInvoiceNo = new TextBox { Font = new Font("Segoe UI Semibold", 10F), Text = "" };
            txtPreparedBy = new TextBox { Font = new Font("Segoe UI", 10F), Text = "Marie Ann J. Billones" };
            txtReceivedBy1 = new TextBox { Font = new Font("Segoe UI", 10F), Text = "Ma'am Monette Rubonal" };
            txtReceivedBy2 = new TextBox { Font = new Font("Segoe UI", 10F), Text = "Ma'am Francis Pauline Rodillas" };
            var gridSign = CreateFieldGrid(2,
                ("Sales invoice no. (SI no.)", txtSalesInvoiceNo), ("Prepared by (billing officer)", txtPreparedBy),
                ("Received by 1 (client accountant)", txtReceivedBy1), ("Received by 2 (client accountant)", txtReceivedBy2));
            var lblSiHint = new Label
            {
                Height = Dpi(24),
                Text = "Saving with an SI number creates or updates the matching billing invoice.",
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.TopLeft
            };

            // ---------------- Cards
            var card1 = CreateSectionCard(1, "Client & period", "Who the statement is for and which delivery dates it covers.", gridClient, lblPopulateHint);
            var cardPicker = CreateSectionCard(2, "Completed dispatches", "Pick which completed trips go on this statement, or type a dispatch number.", BuildDispatchPicker());
            var card2 = CreateSectionCard(3, "Trips & stores", "Every trip and store billed on this statement.", bar, lblItemsHint, pnlItemsGridHost, lblItemsTotals);
            var card3 = CreateSectionCard(4, "Taxes & total", "VAT, withholding tax and the amount the client pays.", taxLayout);
            var card4 = CreateSectionCard(5, "Sales invoice & sign-off", "The SI number and the people who prepare and receive the statement.", gridSign, lblSiHint);
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
            btnSaveSoa = MakeFormButton("💾  Save & create invoice", "primary", Dpi(210));
            btnSaveSoa.Height = Dpi(40);
            btnSaveSoa.Click += BtnSaveSoa_Click;
            btnPrintDrawer = MakeFormButton("🖨  Print SOA", "secondary", Dpi(150));
            btnPrintDrawer.Height = Dpi(40);
            btnPrintDrawer.Click += (s, e) => PrintCurrentDraftSoa();
            btnDeleteSoa = MakeFormButton("Delete statement", "danger", Dpi(160));
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
            int h = Dpi(36) + Math.Max(4, gridItems.Rows.Count) * Dpi(30) + Dpi(22);
            pnlItemsGridHost.Height = Math.Clamp(h, Dpi(260), Dpi(560));
        }
        private void UpdateItemsSummary()
        {
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
            lblItemsTotals.Text = $"{gridItems.Rows.Count} rows  ·  {trips.Count} trips  ·  Amount ₱{amt:N2}  ·  Fuel ₱{fuel:N2}" +
                                  (unpriced > 0 ? $"   ·   ⚠ {unpriced} trip{(unpriced == 1 ? "" : "s")} without an amount" : "");
            lblItemsTotals.ForeColor = unpriced > 0 ? AppTheme.WarningText : AppTheme.TextSecondary;
        }
        private void OpenNewStatement()
        {
            isEditingExisting = false;
            ClearForm();
            ApplyFormMode();
            SetFormVisibility(true);
            rateCache.Clear();
            RefreshPicker();
            cbClient.Focus();
        }
        private void OpenEditStatement(StatementOfAccount soa)
        {
            selectedSoa = soa;
            BindSoaToDrawer(soa);
            isEditingExisting = true;
            ApplyFormMode();
            SetFormVisibility(true);
            rateCache.Clear();
            RefreshPicker();
        }
        private void ApplyFormMode()
        {
            btnDeleteSoa.Visible = isEditingExisting;
            btnClearForm.Visible = !isEditingExisting;
            btnSaveSoa.Text = isEditingExisting ? "💾  Save changes" : "💾  Save & create invoice";
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
            gridSoa.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalesInvoiceNo", HeaderText = "SI NO.", FillWeight = 80, MinimumWidth = 65, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) } });
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
                m.Items.Add(RowMenu.Danger("Delete statement…", () => { selectedSoa = soa; BtnDeleteSoa_Click(this, EventArgs.Empty); }));
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
                    picked.Count > 1 ? "Change or remove them together" : (did > 0 ? "Filled from a dispatch" : "Typed by hand")));
                m.Items.Add(RowMenu.Item(picked.Count > 1 ? $"Edit a column on {picked.Count} rows…" : "Edit a column…", () => BulkEditSelectedRows()));
                m.Items.Add(RowMenu.Item("Add a blank row", () => AddBlankGridRow()));
                if (did > 0 && picked.Count == 1) m.Items.Add(RowMenu.Item("Open the dispatch", () => RowMenu.OpenDispatch(this, did, null)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger(picked.Count > 1 ? $"Remove {picked.Count} rows" : "Remove this row", () => RemoveSelectedRows()));
            }, allowMultiSelect: true);
        }
        /// <summary>Rows filled from a dispatch: that dispatch must be marked completed and not already on another statement.</summary>
        private bool CheckLinkedDispatchesBeforeSave()
        {
            if (!gridItems.Columns.Contains("colDeliveryId")) return true;
            var ids = new HashSet<int>();
            foreach (DataGridViewRow r in gridItems.Rows)
                if (!r.IsNewRow && int.TryParse(r.Cells["colDeliveryId"].Value?.ToString(), out int id) && id > 0) ids.Add(id);
            if (ids.Count == 0) return true;
            var billed = MJdatabase.GetBilledDeliveryIds(isEditingExisting && selectedSoa != null ? selectedSoa.Id : 0);
            var problems = new List<string>();
            foreach (int id in ids)
            {
                var d = MJdatabase.GetDeliveryById(id);
                if (d == null) continue;
                if (!DispatchGate.IsCompleted(d.Status)) problems.Add($"{d.DispatchNumber} is still \"{d.Status}\" — mark it completed in Dispatch first");
                else if (billed.TryGetValue(id, out var other)) problems.Add($"{d.DispatchNumber} is already billed on {other}");
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

        private void SetFormVisibility(bool visible)
        {
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
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        private void OpenSoaTableEditor(Label bannerLabel)
        {
            if (gridItems == null) return;

            var colSpecs = new List<SpecialTripColumnDef>();
            foreach (DataGridViewColumn col in gridItems.Columns)
            {
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
                            if (gridItems.Columns.Contains(kv.Key))
                            {
                                gRow.Cells[kv.Key].Value = kv.Value;
                            }
                        }
                    }
                }
                finally
                {
                    isBindingForm = false;
                }
                RecalculateTaxes(manualOverride: false);
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
            cbFilterClient.Items.Add("All Clients");

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
                FilterGridData();
            }
            catch (Exception ex)
            { ErrLog.Error("StatementOfAccountView", ex); 
                MessageBox.Show($"Failed to load Statements of Account: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterGridData()
        {
            string q = txtGridSearch?.Text.Trim().ToLower() ?? "";
            string filterClient = cbFilterClient?.SelectedItem?.ToString() ?? "All Clients";

            var filtered = allSoas.Where(soa =>
            {
                if (filterClient != "All Clients" && !soa.ClientName.Equals(filterClient, StringComparison.OrdinalIgnoreCase)) return false;
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
            dtpIssueDate.Value = soa.IssueDate;
            var period = (soa.PeriodCovered ?? "").Split(new[] { " to " }, StringSplitOptions.None);
            if (period.Length == 2 && DateTime.TryParse(period[0].Trim(), out var pFrom) && DateTime.TryParse(period[1].Trim(), out var pTo))
            {
                dtpPeriodFrom.Value = pFrom;
                dtpPeriodTo.Value = pTo;
            }
            cbDeliveryType.SelectedItem = string.IsNullOrEmpty(soa.DeliveryType) ? "REF" : soa.DeliveryType;
            txtSalesInvoiceNo.Text = soa.SalesInvoiceNo;
            txtPreparedBy.Text = string.IsNullOrEmpty(soa.PreparedBy) ? "Marie Ann J. Billones" : soa.PreparedBy;
            txtReceivedBy1.Text = string.IsNullOrEmpty(soa.ReceivedBy1) ? "Ma'am Monette Rubonal" : soa.ReceivedBy1;
            txtReceivedBy2.Text = string.IsNullOrEmpty(soa.ReceivedBy2) ? "Ma'am Francis Pauline Rodillas" : soa.ReceivedBy2;

            numTotalSales.Value = Math.Min(numTotalSales.Maximum, Math.Max(numTotalSales.Minimum, soa.TotalSalesVatInc));
            numVat.Value = Math.Min(numVat.Maximum, Math.Max(numVat.Minimum, soa.VatAmount));
            numNetOfVat.Value = Math.Min(numNetOfVat.Maximum, Math.Max(numNetOfVat.Minimum, soa.NetOfVat));
            numFuelSurchargeTotal.Value = Math.Min(numFuelSurchargeTotal.Maximum, Math.Max(numFuelSurchargeTotal.Minimum, soa.FuelSurchargeTotal));
            numWithholdingTax.Value = Math.Min(numWithholdingTax.Maximum, Math.Max(numWithholdingTax.Minimum, soa.WithholdingTax));
            numTotalAmountDue.Value = Math.Min(numTotalAmountDue.Maximum, Math.Max(numTotalAmountDue.Minimum, soa.TotalAmount));

            var items = MJdatabase.GetStatementOfAccountItems(soa.Id);
            gridItems.Rows.Clear();
            foreach (var it in items)
            {
                gridItems.Rows.Add(it.TripNumber, it.DateReceived, it.Zone, it.DrNumbers, it.Destination, it.DeliveryType, it.PlateNumber, it.Tonnage, it.DropCount, it.Amount, it.FuelSurcharge, it.DriverHelper, it.DeliveryId > 0 ? it.DeliveryId.ToString() : "");
            }

            isBindingForm = false;
            UpdateItemsSummary();
        }

        private void ClearForm()
        {
            selectedSoa = null;
            txtSoaNumber.Text = MJdatabase.GenerateNextSoaNumber();
            cbClient.SelectedItem = "MORE THAN CAKES, INC.";
            AutoFillClientDetails();
            dtpIssueDate.Value = DateTime.Today;
            cbDeliveryType.SelectedIndex = 0;
            gridItems.Rows.Clear();
            txtSalesInvoiceNo.Text = "";
            txtPreparedBy.Text = "Marie Ann J. Billones";
            txtReceivedBy1.Text = "Ma'am Monette Rubonal";
            txtReceivedBy2.Text = "Ma'am Francis Pauline Rodillas";
            numTotalSales.Value = 0;
            numVat.Value = 0;
            numNetOfVat.Value = 0;
            numFuelSurchargeTotal.Value = 0;
            numWithholdingTax.Value = 0;
            numTotalAmountDue.Value = 0;
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
                QuietBox.Show($"No completed trips are waiting to be billed for {(string.IsNullOrWhiteSpace(cbClient.Text) ? "this period" : cbClient.Text.Trim())}.\n\nThe list in step 2 shows which trips are already on this or another statement.", "Add trips", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddDeliveriesToStatement(ready);
        }

        private void AddBlankGridRow()
        {
            int nextTrip = 1;
            if (gridItems.Rows.Count > 0)
            {
                var lastVal = gridItems.Rows[gridItems.Rows.Count - 1].Cells[0].Value?.ToString();
                if (int.TryParse(lastVal, out int lastNum)) nextTrip = lastNum + 1;
            }
            gridItems.Rows.Add(nextTrip, DateTime.Today.ToString("yyyy-MM-dd"), "", "", "", cbDeliveryType.Text, "", "6T", 1, 0m, 0m, "", "");
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
            n.Value = Math.Min(n.Maximum, Math.Max(n.Minimum, Math.Round(v, 2)));
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

                    decimal totalSales = amountSum + fuelSum;
                    decimal vat = totalSales / 1.12m * 0.12m;
                    decimal netOfVat = totalSales / 1.12m;
                    decimal withholdingTax = netOfVat * 0.02m;
                    // totalSales already includes fuel (VAT-inclusive), so fuel is not added a second time.
                    decimal totalDue = netOfVat + vat - withholdingTax;
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
                    decimal totalDue = netOfVat + vat - wtax;
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
                int tripNo = int.TryParse(row.Cells[0].Value?.ToString(), out int t) ? t : 1;
                string dtRecv = row.Cells[1].Value?.ToString() ?? "";
                string zone = row.Cells[2].Value?.ToString() ?? "";
                string dr = row.Cells[3].Value?.ToString() ?? "";
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
                    DeliveryId = deliveryId
                });
            }
            return list;
        }

        private void BtnSaveSoa_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cbClient.Text))
                {
                    MessageBox.Show("Please select or enter the client company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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
                if (numTotalAmountDue.Value <= 0)
                {
                    var zero = MessageBox.Show("The total amount due is 0.00. Some trips may still need an amount.\n\nSave the statement anyway?", "Check amounts", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (zero != DialogResult.Yes) return;
                }
                if (!CheckLinkedDispatchesBeforeSave()) return;
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
                soa.Status = "Issued";

                int savedId = MJdatabase.SaveStatementOfAccount(soa, items);
                soa.Id = savedId;
                selectedSoa = soa;

                QuietBox.Show($"Statement of Account #{soa.SoaNumber} successfully saved and linked to Billing Invoice (SI NO. {soa.SalesInvoiceNo})!", "SOA Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
                SetFormVisibility(false);

                PrintOfficialSoa(soa);
            }
            catch (Exception ex)
            { ErrLog.Error("StatementOfAccountView", ex); 
                MessageBox.Show($"Failed to save Statement of Account: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteSoa_Click(object? sender, EventArgs e)
        {
            if (selectedSoa == null || (pnlForm.Visible && !isEditingExisting))
            {
                MessageBox.Show("Please select a Statement of Account to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to remove Statement of Account #{selectedSoa.SoaNumber}?\n\n" + MJdatabase.DeletePolicyNote("StatementsOfAccount"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    MJdatabase.DeleteStatementOfAccount(selectedSoa.Id);
                    ToastHelper.ShowToast(this, MJdatabase.LastDeleteSummary, ToastType.Info);
                    ClearForm();
                    LoadData();
                    SetFormVisibility(false);
                }
                catch (Exception ex)
                { ErrLog.Error("StatementOfAccountView", ex); 
                    MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
    }
}
