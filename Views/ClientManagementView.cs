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
    /// Clients: a full-width list of client companies, and a full-page client profile (same tab, back button at the
    /// top left) with the company details, its stores, its trips and its billing.
    /// </summary>
    public class ClientManagementView : UserControl, IRefreshable
    {
        private const int FormContentMaxWidth = 1100;
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        // ------------------------------------------------------------------ State
        private List<ClientCompany> allClients = new List<ClientCompany>();
        private List<ClientRow> allRows = new List<ClientRow>();
        private ClientCompany? selectedClient = null;
        private List<ClientStore> currentStores = new List<ClientStore>();
        private bool isBinding = false;

        private bool isDirty = false;
        private bool isNewClient = false;
        // ------------------------------------------------------------------ List page
        private Panel pnlListPage = null!;
        private TextBox txtSearchClient = null!;
        private Button btnCustomizeList = null!;
        private Button btnAddClient = null!;
        private Label lblListCount = null!;
        private DataGridView gridClients = null!;

        private ComboBox cbStatusFilter = null!;
        private ComboBox cbInactiveAfter = null!;
        private const string InactiveAfterKey = "Clients.InactiveAfterDays";
        private int InactiveAfterDays => cbInactiveAfter?.SelectedIndex == 1 ? 180 : 90;
        // ------------------------------------------------------------------ Profile page
        private Panel pnlProfilePage = null!;
        private Panel pnlProfileHeader = null!;
        private Button btnBack = null!;
        private Label lblProfileTitle = null!;
        private Panel pnlProfileBody = null!;
        private Panel pnlProfileActions = null!;
        private TextBox txtClientName = null!;
        private TextBox txtClientTin = null!;
        private TextBox txtClientAddress = null!;
        private TextBox txtContactPerson = null!;
        private TextBox txtContactNumber = null!;
        private TextBox txtEmail = null!;
        private Button btnSave = null!;
        private Button btnDelete = null!;
        private Label lblSaveState = null!;
        private Panel pnlStoresCard = null!;
        private Label lblStoresTitle = null!;
        private Label lblStoresHint = null!;
        private Button btnAddBranch = null!;
        private Button btnCustomizeTable = null!;
        private Panel pnlBranchesHost = null!;
        private DataGridView gridBranches = null!;

        private Panel pnlTripsCard = null!;
        private FlowLayoutPanel flowTripTiles = null!;
        private DataGridView gridTrips = null!;
        private Panel pnlTripsHost = null!;
        private Panel pnlBillingCard = null!;
        private FlowLayoutPanel flowBillingTiles = null!;
        private DataGridView gridInvoices = null!;
        private Panel pnlInvoicesHost = null!;
        private readonly ToolTip tips = new ToolTip { InitialDelay = 400, ShowAlways = true };
        // ------------------------------------------------------------------ Table layouts
        private const string ClientsListKey = "Clients.List";
        private const string StoresKey = "Clients.Stores";
        private List<GridColumnPref> clientListDefaults = new List<GridColumnPref>();
        private List<GridColumnPref> clientListPrefs = new List<GridColumnPref>();
        private List<GridColumnPref> storeDefaults = new List<GridColumnPref>();
        private List<GridColumnPref> storePrefs = new List<GridColumnPref>();
        private Panel pnlCustomFields = null!;
        private readonly Dictionary<string, TextBox> customFieldBoxes = new Dictionary<string, TextBox>();
        private sealed class ClientRow
        {
            public ClientCompany Client { get; init; } = new ClientCompany();
            public string CompanyName => Client.CompanyName;
            public string TIN => string.IsNullOrWhiteSpace(Client.TIN) ? "—" : Client.TIN;
            public string Address => string.IsNullOrWhiteSpace(Client.Address) ? "—" : Client.Address;
            public string Contact { get; init; } = "";
            public int Stores { get; init; }
            public int CompletedTrips { get; init; }
            public string LastTrip { get; init; } = "—";
            public string Unpaid { get; init; } = "—";
            /// <summary>Active, or Inactive (deactivated, or no activity within the inactivity window).</summary>
            public string Status { get; init; } = "Active";
            public string StatusNote { get; init; } = "";
            public bool IsInactive => Status == "Inactive";
        }
        public ClientManagementView()
        {
            InitializeCustomComponent();
            InitializeTableLayouts();
            LoadClients();
            UIThemeHelper.ApplyTheme(this);
            StyleAfterTheme();
            ShowProfile(false);
        }

        public void RefreshData()
        {
            if (pnlProfilePage.Visible && isDirty) return; // don't overwrite unsaved edits
            LoadClients();
            if (pnlProfilePage.Visible && selectedClient != null && !isNewClient)
            {
                var fresh = allClients.FirstOrDefault(c => c.Id == selectedClient.Id) ?? selectedClient;
                BindClient(fresh);
            }
        }
        /// <summary>Opens a client's profile by name (used by the order forms' "Open client" link).</summary>
        public void OpenClientByName(string companyName)
        {
            if (pnlProfilePage.Visible && !CanLeaveWithoutLosing()) return;
            LoadClients();
            var c = allClients.FirstOrDefault(x => string.Equals(x.CompanyName.Trim(), (companyName ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            if (c != null) OpenClient(c);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (pnlProfilePage.Visible)
            {
                if (keyData == Keys.Escape) { TryLeaveProfile(); return true; }
                if (keyData == (Keys.Control | Keys.S)) { SaveClient(); return true; }
            }
            else if (keyData == Keys.Enter && gridClients.Focused && gridClients.CurrentRow?.DataBoundItem is ClientRow row)
            {
                OpenClient(row.Client);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ------------------------------------------------------------------ Building blocks
        private Button MakeButton(string text, string role, int width = 0)
        {
            var b = new Button
            {
                Text = text,
                Height = Dpi(36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Margin = new Padding(0)
            };
            if (width > 0) b.Width = width;
            else { b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink; b.Padding = new Padding(Dpi(12), 0, Dpi(12), 0); b.MinimumSize = new Size(0, Dpi(36)); }
            b.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }
        private DataGridView MakeGrid(bool readOnly)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = readOnly,
                SelectionMode = readOnly ? DataGridViewSelectionMode.FullRowSelect : DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(38),
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false
            };
            g.RowTemplate.Height = Dpi(36);
            return g;
        }
        private static DataGridViewTextBoxColumn TextCol(string prop, string header, int weight, int min, bool right = false) => new DataGridViewTextBoxColumn
        {
            DataPropertyName = prop,
            Name = prop,
            HeaderText = header,
            FillWeight = weight,
            MinimumWidth = min,
            DefaultCellStyle = right ? new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } : new DataGridViewCellStyle()
        };
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
        private Panel Field(string label, Control input, int inputHeight = 0)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Margin = new Padding(0, 0, Dpi(16), 0) };
            var lbl = new Label { Text = label, Dock = DockStyle.Top, Height = Dpi(24), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft };
            input.Dock = DockStyle.Top;
            if (inputHeight > 0) input.Height = inputHeight;
            pnl.Controls.Add(input);
            pnl.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Dpi(4), BackColor = AppTheme.Surface });
            pnl.Controls.Add(lbl);
            return pnl;
        }
        private Panel Tile(string caption, string value, string sub, Color? valueColor = null)
        {
            var p = new Panel { Width = Dpi(196), Height = Dpi(82), BackColor = AppTheme.Surface, Margin = new Padding(0, 0, Dpi(12), Dpi(12)) };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                using (var path = Gfx.RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), Dpi(8)))
                using (var b = new SolidBrush(AppTheme.SurfaceAlt))
                using (var pen = new Pen(AppTheme.Border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(pen, path);
                }
                int x = Dpi(14);
                var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                using var f1 = new Font("Segoe UI", 8.75F);
                using var f2 = new Font("Segoe UI Semibold", 14F);
                using var f3 = new Font("Segoe UI", 8.5F);
                TextRenderer.DrawText(g, caption, f1, new Rectangle(x, Dpi(9), p.Width - 2 * x, Dpi(18)), AppTheme.TextMuted, flags);
                TextRenderer.DrawText(g, value, f2, new Rectangle(x, Dpi(26), p.Width - 2 * x, Dpi(28)), valueColor ?? AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(g, sub, f3, new Rectangle(x, Dpi(56), p.Width - 2 * x, Dpi(18)), AppTheme.TextMuted, flags);
            };
            tips.SetToolTip(p, $"{caption}: {value}  {sub}");
            return p;
        }
        // ------------------------------------------------------------------ Layout
        private void InitializeCustomComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9F);
            BuildListPage();
            BuildProfilePage();
            Controls.Add(pnlListPage);
            Controls.Add(pnlProfilePage);
        }
        private void BuildListPage()
        {
            pnlListPage = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(Dpi(12)) };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(Dpi(20), Dpi(16), Dpi(20), Dpi(16)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = Dpi(52), ColumnCount = 6, RowCount = 1, BackColor = AppTheme.Surface };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(280)));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(140)));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(200)));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var titleStack = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            var lblTitle = new Label { Text = "Clients", Dock = DockStyle.Top, Height = Dpi(28), Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary };
            lblListCount = new Label { Text = "", Dock = DockStyle.Top, Height = Dpi(20), Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted };
            titleStack.Controls.Add(lblListCount);
            titleStack.Controls.Add(lblTitle);
            txtSearchClient = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Search name, TIN, address or contact…",
                Margin = new Padding(0, Dpi(10), Dpi(12), 0)
            };

            txtSearchClient.TextChanged += (s, e) => FilterClients();
            cbStatusFilter = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, Dpi(10), Dpi(10), 0) };
            cbStatusFilter.Items.AddRange(new object[] { "All statuses", "Active", "Inactive" });
            cbStatusFilter.SelectedIndex = 0;
            cbStatusFilter.SelectedIndexChanged += (s, e) => FilterClients();
            tips.SetToolTip(cbStatusFilter, "Show all clients, only active ones, or only inactive ones. With All statuses, inactive clients are listed last.");
            cbInactiveAfter = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, Dpi(10), Dpi(12), 0) };
            cbInactiveAfter.Items.AddRange(new object[] { "Inactive after 90 days", "Inactive after 180 days" });
            cbInactiveAfter.SelectedIndex = MJdatabase.GetSystemSetting(InactiveAfterKey, "90") == "180" ? 1 : 0;
            cbInactiveAfter.SelectedIndexChanged += (s, e) =>
            {
                try { MJdatabase.SetSystemSetting(InactiveAfterKey, InactiveAfterDays.ToString(), "Days without orders or invoices before a client counts as inactive"); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                LoadClients();
            };
            tips.SetToolTip(cbInactiveAfter, "A client with no dispatch, loading order, special trip or invoice in this many days is shown as Inactive. Deactivated clients are always Inactive.");
            btnAddClient = MakeButton("＋  New client", "primary");
            btnAddClient.Margin = new Padding(0, Dpi(8), 0, 0);
            btnAddClient.Click += (s, e) => OpenNewClient();
            btnCustomizeList = MakeButton("⚙  Customize table", "secondary");
            btnCustomizeList.Margin = new Padding(0, Dpi(8), Dpi(8), 0);
            btnCustomizeList.Click += (s, e) => CustomizeClientList();
            tips.SetToolTip(btnCustomizeList, "Choose, rename, reorder and resize the columns, or add your own (e.g. Region, Payment terms)");
            header.Controls.Add(titleStack, 0, 0);
            header.Controls.Add(txtSearchClient, 1, 0);
            header.Controls.Add(cbStatusFilter, 2, 0);
            header.Controls.Add(cbInactiveAfter, 3, 0);
            header.Controls.Add(btnCustomizeList, 4, 0);
            header.Controls.Add(btnAddClient, 5, 0);
            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(12), BackColor = AppTheme.Surface };
            gridClients = MakeGrid(readOnly: true);
            gridClients.Columns.Add(TextCol("CompanyName", "Company", 170, Dpi(160)));
            gridClients.Columns.Add(TextCol("Status", "Status", 70, Dpi(90)));
            gridClients.Columns.Add(TextCol("TIN", "TIN", 90, Dpi(100)));
            gridClients.Columns.Add(TextCol("Address", "Address", 160, Dpi(120)));
            gridClients.Columns.Add(TextCol("Contact", "Contact", 130, Dpi(110)));
            gridClients.Columns.Add(TextCol("Stores", "Stores", 55, Dpi(60), right: true));
            gridClients.Columns.Add(TextCol("CompletedTrips", "Completed trips", 80, Dpi(90), right: true));
            gridClients.Columns.Add(TextCol("LastTrip", "Last trip", 80, Dpi(90)));
            gridClients.Columns.Add(TextCol("Unpaid", "Outstanding balance", 90, Dpi(100), right: true));
            gridClients.Columns["CompanyName"]!.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            gridClients.Cursor = Cursors.Hand;
            gridClients.DataBindingComplete += (s, e) => FillClientCustomCells();
            gridClients.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && gridClients.Columns[e.ColumnIndex].Name == "Status" && gridClients.Rows[e.RowIndex].DataBoundItem is ClientRow r)
                    e.ToolTipText = r.StatusNote;
            };
            gridClients.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridClients.Rows[e.RowIndex].DataBoundItem is ClientRow r && r.IsInactive)
                    gridClients.Rows[e.RowIndex].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
            };
            gridClients.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridClients.Rows[e.RowIndex].DataBoundItem is ClientRow r) OpenClient(r.Client); };
            RowMenu.Attach(gridClients, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not ClientRow r) return;
                var c = r.Client;
                m.Items.AddRange(RowMenu.Header($"🏢  {c.CompanyName}", $"{r.Stores} store(s) · {r.CompletedTrips} completed trip(s) · last trip {r.LastTrip} · outstanding {r.Unpaid}"));
                m.Items.Add(RowMenu.Item("Open client profile", () => OpenClient(c), "Enter"));
                if (c.IsActive) m.Items.Add(RowMenu.Danger("Deactivate client…", () => SetClientActive(c, false)));
                else m.Items.Add(RowMenu.Good("Reactivate client", () => SetClientActive(c, true)));
                m.Items.Add(RowMenu.Item("Delivery routes", () => MainForm.Instance?.NavigateToMasterData("Routes")));
                m.Items.Add(RowMenu.Sub("📂  Show records",
                    RowMenu.Item("Loading schedules & dispatches", () => MainForm.Instance?.NavigateToOperations("Dispatch")),
                    RowMenu.Item("Invoices", () => MainForm.Instance?.ShowFinanceRecords("Billing", c.CompanyName)),
                    RowMenu.Item("Statements of account", () => MainForm.Instance?.ShowFinanceRecords("StatementOfAccount", c.CompanyName))));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Company name", c.CompanyName), ("TIN", c.TIN), ("Address", c.Address), ("Contact person", c.ContactPerson), ("Contact number", c.ContactNumber), ("Email", c.Email),
                    ("Billing block", string.Join(Environment.NewLine, new[] { c.CompanyName, c.Address, string.IsNullOrWhiteSpace(c.TIN) ? "" : "TIN: " + c.TIN }.Where(x => !string.IsNullOrWhiteSpace(x))))));
                m.Items.Add(RowMenu.Item("Customize table…", () => CustomizeClientList()));
                m.Items.Add(RowMenu.ExportSub(gridClients, "Clients", "Client Companies", () => PrintHelper.PrintGridReport("Client Companies", gridClients, "Registered client companies.")));
            });
            var hint = new Label { Dock = DockStyle.Bottom, Height = Dpi(30), Text = "Double-click a client (or press Enter) to open its profile, stores, trips and billing.", Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.BottomLeft };
            card.Controls.Add(gridClients);
            card.Controls.Add(hint);
            card.Controls.Add(gap);
            card.Controls.Add(header);
            pnlListPage.Controls.Add(card);
        }

        private void BuildProfilePage()
        {
            pnlProfilePage = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Visible = false };
            // Header: back button at the upper left, then the client's name
            pnlProfileHeader = new Panel { Dock = DockStyle.Top, Height = Dpi(64), BackColor = AppTheme.Canvas };
            btnBack = MakeButton("←  Back to clients", "secondary", Dpi(170));
            btnBack.Dock = DockStyle.Left;
            btnBack.Click += (s, e) => TryLeaveProfile();
            var spacer = new Panel { Dock = DockStyle.Left, Width = Dpi(16), BackColor = AppTheme.Canvas };
            lblProfileTitle = new Label { Dock = DockStyle.Fill, Text = "Client", Font = new Font("Segoe UI Semibold", 15F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false };
            pnlProfileHeader.Controls.Add(lblProfileTitle);
            pnlProfileHeader.Controls.Add(spacer);
            pnlProfileHeader.Controls.Add(btnBack);
            pnlProfileBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.Canvas };
            // Bottom action bar
            pnlProfileActions = new Panel { Dock = DockStyle.Bottom, Height = Dpi(66), BackColor = AppTheme.Surface };
            pnlProfileActions.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, pnlProfileActions.Width, 0); };
            var flowActions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = AppTheme.Surface };
            btnSave = MakeButton("💾  Save client", "primary", Dpi(170));
            btnSave.Height = Dpi(40);
            btnSave.Click += (s, e) => SaveClient();
            tips.SetToolTip(btnSave, "Save the company details (Ctrl+S)");
            btnDelete = MakeButton("Deactivate client", "danger", Dpi(170));
            btnDelete.Height = Dpi(40);
            btnDelete.Margin = new Padding(Dpi(10), 0, 0, 0);
            btnDelete.Click += (s, e) => DeleteClient();
            flowActions.Controls.Add(btnSave);
            flowActions.Controls.Add(btnDelete);
            lblSaveState = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, Text = "" };
            pnlProfileActions.Controls.Add(lblSaveState);
            pnlProfileActions.Controls.Add(flowActions);
            // Card 1: company details
            txtClientName = new TextBox { Font = new Font("Segoe UI", 10.5F), PlaceholderText = "Full registered company name" };
            txtClientTin = new TextBox { Font = new Font("Segoe UI", 10.5F), PlaceholderText = "000-000-000-000" };
            txtClientTin.TextChanged += TxtClientTin_TextChanged;
            txtClientAddress = new TextBox { Font = new Font("Segoe UI", 10.5F), Multiline = true, PlaceholderText = "Head office address" };
            txtContactPerson = new TextBox { Font = new Font("Segoe UI", 10.5F), PlaceholderText = "e.g. Dela Cruz, Juan" };
            txtContactNumber = new TextBox { Font = new Font("Segoe UI", 10.5F), PlaceholderText = "e.g. 0917 123 4567" };
            txtEmail = new TextBox { Font = new Font("Segoe UI", 10.5F), PlaceholderText = "billing@company.com" };
            foreach (var t in new[] { txtClientName, txtClientTin, txtClientAddress, txtContactPerson, txtContactNumber, txtEmail })
                t.TextChanged += (s, e) => MarkDirty();
            var tblDetails = new TableLayoutPanel { Height = Dpi(66) * 2 + Dpi(92), ColumnCount = 3, RowCount = 3, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5F));
            tblDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5F));
            tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(66)));
            tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(66)));
            tblDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(92)));
            tblDetails.Controls.Add(Field("Company name", txtClientName), 0, 0);
            tblDetails.Controls.Add(Field("TIN", txtClientTin), 1, 0);
            tblDetails.Controls.Add(Field("Email", txtEmail), 2, 0);
            tblDetails.Controls.Add(Field("Contact person", txtContactPerson), 0, 1);
            tblDetails.Controls.Add(Field("Contact number", txtContactNumber), 1, 1);
            var addr = Field("Address", txtClientAddress, Dpi(56));
            tblDetails.Controls.Add(addr, 0, 2);
            tblDetails.SetColumnSpan(addr, 3);
            pnlCustomFields = new Panel { Height = 0, BackColor = AppTheme.Surface, Visible = false };
            var cardDetails = CreateSectionCard(1, "Company details", "Name, TIN and contact used on statements, invoices and receipts.", tblDetails, pnlCustomFields);
            // Card 2: stores & branches
            var bar = new TableLayoutPanel { Height = Dpi(46), ColumnCount = 3, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblStoresTitle = new Label { Dock = DockStyle.Fill, Text = "🏪 Stores", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft };
            btnCustomizeTable = MakeButton("⚙  Customize table", "secondary");
            btnCustomizeTable.Margin = new Padding(0, 0, Dpi(8), 0);
            btnCustomizeTable.Click += (s, e) => CustomizeStoresTable();
            tips.SetToolTip(btnCustomizeTable, "Choose, rename, reorder and resize the store columns, or add your own (e.g. Store manager, Contact #)");
            btnAddBranch = MakeButton("➕  Add store", "primary");
            btnAddBranch.Click += (s, e) => AddNewBranchRow();

            var btnRoutes = MakeButton("🗺  Delivery routes", "secondary");
            btnRoutes.Margin = new Padding(0, 0, Dpi(8), 0);
            btnRoutes.Click += (s, e) => { if (CanLeaveWithoutLosing()) MainForm.Instance?.NavigateToMasterData("Routes"); };
            tips.SetToolTip(btnRoutes, "Open Master Data → Delivery Routes to add or reorder this client's routes");
            bar.ColumnCount = 4;
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.Controls.Add(lblStoresTitle, 0, 0);
            bar.Controls.Add(btnRoutes, 1, 0);
            bar.Controls.Add(btnCustomizeTable, 2, 0);
            bar.Controls.Add(btnAddBranch, 3, 0);
            lblStoresHint = new Label { Height = Dpi(26), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true };
            pnlBranchesHost = new Panel { Height = Dpi(260), BackColor = AppTheme.Surface };
            gridBranches = MakeGrid(readOnly: false);
            gridBranches.EditMode = DataGridViewEditMode.EditOnEnter;
            gridBranches.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StoreCode", Name = "StoreCode", HeaderText = "Store code", FillWeight = 70, MinimumWidth = Dpi(90) });
            gridBranches.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StoreName", Name = "StoreName", HeaderText = "Store name", FillWeight = 200, MinimumWidth = Dpi(160) });
            gridBranches.Columns.Add(new DataGridViewComboBoxColumn
            {
                DataPropertyName = "DestinationRoute",
                Name = "DestinationRoute",
                HeaderText = "Delivery route",
                FillWeight = 150,
                MinimumWidth = Dpi(150),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            });
            var colActions = new DataGridViewButtonColumn
            {
                Name = "Actions",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                FillWeight = 40,
                MinimumWidth = Dpi(80),
                FlatStyle = FlatStyle.Flat
            };

            colActions.DefaultCellStyle.ForeColor = AppTheme.DangerText;
            colActions.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            gridBranches.Columns.Add(colActions);
            gridBranches.DataError += (s, e) => { e.ThrowException = false; };
            gridBranches.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridBranches.IsCurrentCellDirty && gridBranches.CurrentCell is DataGridViewComboBoxCell)
                    gridBranches.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            gridBranches.CellValueChanged += (s, e) =>
            {
                if (isBinding || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = gridBranches.Columns[e.ColumnIndex].Name;
                if (colName.StartsWith(GridLayoutStore.CustomPrefix, StringComparison.Ordinal) && e.RowIndex < currentStores.Count)
                {
                    var st = currentStores[e.RowIndex];
                    var values = GridLayoutStore.ReadValues(st.ExtraFieldsJson);
                    values[colName] = gridBranches.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    st.ExtraFieldsJson = GridLayoutStore.WriteValues(values);
                }
                SaveStoreRow(e.RowIndex);
            };
            gridBranches.CellValidating += (s, e) =>
            {
                if (isBinding || e.RowIndex < 0 || e.RowIndex >= currentStores.Count) return;
                string col = gridBranches.Columns[e.ColumnIndex].Name;
                if (col != "StoreCode" && col != "StoreName") return;
                string typed = (e.FormattedValue?.ToString() ?? "").Trim();
                gridBranches.Rows[e.RowIndex].ErrorText = "";
                if (typed.Length == 0)
                {
                    gridBranches.Rows[e.RowIndex].ErrorText = col == "StoreCode" ? "Store code is required" : "Store name is required";
                    e.Cancel = true;
                    return;
                }
                if (col == "StoreCode")
                {
                    var other = currentStores.Where((st, i) => i != e.RowIndex).FirstOrDefault(st => string.Equals((st.StoreCode ?? "").Trim(), typed, StringComparison.OrdinalIgnoreCase));
                    if (other != null)
                    {
                        gridBranches.Rows[e.RowIndex].ErrorText = $"Store code {typed} is already used by \"{other.StoreName}\"";
                        e.Cancel = true;
                    }
                }
            };
            gridBranches.CellEndEdit += (s, e) => { if (e.RowIndex >= 0 && e.RowIndex < gridBranches.Rows.Count) gridBranches.Rows[e.RowIndex].ErrorText = ""; };
            gridBranches.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridBranches.Columns[e.ColumnIndex].Name == "Actions") DeleteBranchRow(e.RowIndex);
            };
            pnlBranchesHost.Controls.Add(gridBranches);
            pnlStoresCard = CreateSectionCard(2, "Stores & branches", "The client's stores and the delivery route each one is on.", bar, lblStoresHint, pnlBranchesHost);
            // Card 3: trips
            flowTripTiles = new FlowLayoutPanel { Height = Dpi(94), WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            pnlTripsHost = new Panel { Height = Dpi(240), BackColor = AppTheme.Surface };
            gridTrips = MakeGrid(readOnly: true);
            gridTrips.Columns.Add(TextCol("Date", "Date", 70, Dpi(90)));
            gridTrips.Columns.Add(TextCol("Dispatch", "Dispatch #", 110, Dpi(120)));
            gridTrips.Columns.Add(TextCol("Route", "Route", 150, Dpi(120)));
            gridTrips.Columns.Add(TextCol("Truck", "Truck", 90, Dpi(90)));
            gridTrips.Columns.Add(TextCol("Crates", "Crates", 50, Dpi(60), right: true));
            gridTrips.Columns.Add(TextCol("Status", "Status", 80, Dpi(100)));
            gridTrips.Cursor = Cursors.Hand;
            gridTrips.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridTrips.Rows[e.RowIndex].Tag is Delivery d) RecordDetailsDialog.ShowDetails(this, d);
            };
            RowMenu.Attach(gridTrips, (m, rows) =>
            {
                if (rows[0].Tag is not Delivery d) return;
                m.Items.AddRange(RowMenu.Header($"🚚  {d.DispatchNumber}", $"{d.DispatchDate:MMM d, yyyy} · {d.TotalCrates} crates · {d.TruckInfo} · {d.Status}"));
                m.Items.Add(RowMenu.Item("Open dispatch details", () => Forms.DispatchDetailsDialog.ShowFor(this, d)));
                if (RowMenu.Is(d.Status, "Completed")) m.Items.Add(RowMenu.Item("Trip report", () => { if (CanLeaveWithoutLosing()) MainForm.Instance?.OpenTripReportForDispatch(d); }));
                m.Items.Add(RowMenu.Item("Print delivery dossier", () => PrintHelper.PrintEndToEndDeliveryDossier(d.Id, d.DispatchNumber)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Dispatch number", d.DispatchNumber), ("Summary", Forms.DispatchDetailsDialog.BuildSummaryText(d))));
            });
            pnlTripsHost.Controls.Add(gridTrips);
            var lblTripsHint = new Label { Height = Dpi(28), Text = "Most recent first. Double-click a trip to open its full order details.", Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.BottomLeft };
            pnlTripsCard = CreateSectionCard(3, "Trips", "Every dispatch for this client.", flowTripTiles, pnlTripsHost, lblTripsHint);
            // Card 4: billing
            flowBillingTiles = new FlowLayoutPanel { Height = Dpi(94), WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            pnlInvoicesHost = new Panel { Height = Dpi(220), BackColor = AppTheme.Surface };
            gridInvoices = MakeGrid(readOnly: true);
            gridInvoices.Columns.Add(TextCol("Invoice", "Invoice #", 110, Dpi(110)));
            gridInvoices.Columns.Add(TextCol("Issued", "Issued", 70, Dpi(90)));
            gridInvoices.Columns.Add(TextCol("Due", "Due", 70, Dpi(90)));
            gridInvoices.Columns.Add(TextCol("Dispatch", "Dispatch #", 110, Dpi(110)));
            gridInvoices.Columns.Add(TextCol("Amount", "Net amount", 80, Dpi(100), right: true));
            gridInvoices.Columns.Add(TextCol("Status", "Status", 70, Dpi(90)));
            gridInvoices.Cursor = Cursors.Hand;
            gridInvoices.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridInvoices.Rows[e.RowIndex].Tag is Invoice inv) RecordDetailsDialog.ShowDetails(this, inv);
            };
            RowMenu.Attach(gridInvoices, (m, rows) =>
            {
                if (rows[0].Tag is not Invoice inv) return;
                m.Items.AddRange(RowMenu.Header($"🧾  Invoice {inv.InvoiceNumber}", $"{RowMenu.Money(inv.NetAmount)} · {inv.Status} · due {inv.DueDate:MMM d, yyyy}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, inv)));
                m.Items.Add(RowMenu.Item("Show in Billing", () => { if (CanLeaveWithoutLosing()) MainForm.Instance?.ShowFinanceRecords("Billing", inv.InvoiceNumber); }));
                m.Items.Add(RowMenu.Item($"Open dispatch {inv.DispatchNumber}", () => RowMenu.OpenDispatch(this, inv.DeliveryId, inv.DispatchNumber), "", inv.DeliveryId > 0));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Invoice number", inv.InvoiceNumber), ("Amount", inv.NetAmount.ToString("N2"))));
            });
            pnlInvoicesHost.Controls.Add(gridInvoices);
            var billingBar = new FlowLayoutPanel { Height = Dpi(48), FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = AppTheme.Surface, Padding = new Padding(0, Dpi(10), 0, 0) };
            var btnOpenBilling = MakeButton("Open billing", "secondary");
            btnOpenBilling.Margin = new Padding(0, 0, Dpi(8), 0);
            btnOpenBilling.Click += (s, e) => { if (CanLeaveWithoutLosing()) MainForm.Instance?.NavigateToFinance("Billing"); };
            var btnOpenSoa = MakeButton("Statements of account", "secondary");
            btnOpenSoa.Click += (s, e) => { if (CanLeaveWithoutLosing()) MainForm.Instance?.NavigateToFinance("StatementOfAccount"); };
            billingBar.Controls.Add(btnOpenBilling);
            billingBar.Controls.Add(btnOpenSoa);
            pnlBillingCard = CreateSectionCard(4, "Billing & payments", "What has been billed to this client and what is still outstanding.", flowBillingTiles, pnlInvoicesHost, billingBar);
            pnlProfileBody.Controls.Add(pnlBillingCard);
            pnlProfileBody.Controls.Add(CreateSectionSpacer());
            pnlProfileBody.Controls.Add(pnlTripsCard);
            pnlProfileBody.Controls.Add(CreateSectionSpacer());
            pnlProfileBody.Controls.Add(pnlStoresCard);
            pnlProfileBody.Controls.Add(CreateSectionSpacer());
            pnlProfileBody.Controls.Add(cardDetails);
            pnlProfilePage.Controls.Add(pnlProfileBody);
            pnlProfilePage.Controls.Add(pnlProfileActions);
            pnlProfilePage.Controls.Add(pnlProfileHeader);
            pnlProfilePage.Resize += (s, e) => CenterProfileContent();
        }

        private void StyleAfterTheme()
        {
            UIThemeHelper.SetButtonRole(btnAddClient, "primary");
            UIThemeHelper.SetButtonRole(btnBack, "secondary");
            btnBack.ForeColor = AppTheme.TextPrimary;
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnDelete, "danger");
            UIThemeHelper.SetButtonRole(btnAddBranch, "primary");
            UIThemeHelper.SetButtonRole(btnCustomizeTable, "secondary");
            foreach (var p in new Control[] { pnlListPage, pnlProfilePage, pnlProfileHeader, pnlProfileBody, lblProfileTitle })
                p.BackColor = AppTheme.Canvas;
            lblProfileTitle.ForeColor = AppTheme.TextPrimary;
            CenterProfileContent();
        }
        private void CenterProfileContent()
        {
            if (pnlProfilePage == null || pnlProfilePage.Width <= 0) return;
            int side = Math.Max(Dpi(24), (pnlProfilePage.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);
            pnlProfileHeader.Padding = new Padding(side, Dpi(10), side, Dpi(14));
            pnlProfileBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(16));
            pnlProfileActions.Padding = new Padding(side, Dpi(12), side, Dpi(12));
        }
        private void ShowProfile(bool visible)
        {
            SuspendLayout();
            pnlProfilePage.Visible = visible;
            pnlListPage.Visible = !visible;
            ResumeLayout(true);
            if (visible)
            {
                CenterProfileContent();
                pnlProfileBody.AutoScrollPosition = new Point(0, 0);
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }
        // ------------------------------------------------------------------ List
        private void LoadClients()
        {
            allClients = MJdatabase.GetClients();
            List<Delivery> deliveries;
            List<Invoice> invoices;
            try { deliveries = MJdatabase.GetDeliveries(); } catch { deliveries = new List<Delivery>(); }
            try { invoices = MJdatabase.GetInvoices(); } catch { invoices = new List<Invoice>(); }
            var lastActivity = MJdatabase.GetClientLastActivity();
            int window = InactiveAfterDays;
            bool Same(string? a, string b) => string.Equals((a ?? "").Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
            allRows = allClients.Select(c =>
            {
                var mine = deliveries.Where(d => Same(d.ClientName, c.CompanyName)).ToList();
                var done = mine.Where(d => string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();
                decimal unpaid = invoices.Where(i => Same(i.ClientName, c.CompanyName) && !string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase) && !string.Equals(i.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).Sum(i => i.NetAmount);
                int stores = 0;
                try { stores = MJdatabase.GetClientStores(c.CompanyName).Count; } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
                string contact = string.Join("  ·  ", new[] { c.ContactPerson, c.ContactNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
                string status = "Active", note;
                lastActivity.TryGetValue(c.CompanyName.Trim(), out var lastAct);
                DateTime? since = lastAct == default ? c.CreatedAt : (c.CreatedAt.HasValue && c.CreatedAt > lastAct ? c.CreatedAt : lastAct);
                if (!c.IsActive)
                {
                    status = "Inactive";
                    note = c.DeactivatedAt.HasValue ? $"Deactivated on {c.DeactivatedAt:MMM d, yyyy}" : "Deactivated";
                }
                else if (since.HasValue && (DateTime.Today - since.Value.Date).TotalDays > window)
                {
                    status = "Inactive";
                    note = $"No orders or invoices in {(int)(DateTime.Today - since.Value.Date).TotalDays} days (last activity {since:MMM d, yyyy}; limit {window} days)";
                }
                else note = since.HasValue ? $"Last activity {since:MMM d, yyyy}" : "New client — no activity yet";
                return new ClientRow
                {
                    Client = c,
                    Status = status,
                    StatusNote = note,
                    Contact = contact.Length == 0 ? "—" : contact,
                    Stores = stores,
                    CompletedTrips = done.Count,
                    LastTrip = mine.Count > 0 ? mine.Max(d => d.DispatchDate).ToString("MMM d, yyyy") : "—",
                    Unpaid = unpaid > 0 ? $"₱{unpaid:N2}" : "—"
                };
            }).ToList();
            FilterClients();
        }

        private void FilterClients()
        {
            string q = txtSearchClient.Text.Trim();
            var filtered = string.IsNullOrWhiteSpace(q)
                ? allRows
                : allRows.Where(r =>
                    (r.Client.CompanyName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Client.TIN ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Client.Address ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.Contact.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Client.ExtraFieldsJson ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            int statusPick = cbStatusFilter?.SelectedIndex ?? 0;
            if (statusPick == 1) filtered = filtered.Where(r => !r.IsInactive).ToList();
            else if (statusPick == 2) filtered = filtered.Where(r => r.IsInactive).ToList();
            // Deterministic order: active clients first, inactive ones sink to the bottom, then by name.
            filtered = filtered.OrderBy(r => r.IsInactive ? 1 : 0).ThenBy(r => r.CompanyName, StringComparer.OrdinalIgnoreCase).ToList();
            isBinding = true;
            gridClients.DataSource = null;
            gridClients.DataSource = filtered;
            isBinding = false;

            int inactiveCount = allRows.Count(r => r.IsInactive);
            lblListCount.Text = (filtered.Count == allRows.Count ? $"{allRows.Count} client companies" : $"Showing {filtered.Count} of {allRows.Count}") + $"  ·  {allRows.Count - inactiveCount} active, {inactiveCount} inactive";
            if (selectedClient != null)
            {
                foreach (DataGridViewRow row in gridClients.Rows)
                {
                    if (row.DataBoundItem is ClientRow r && r.Client.Id == selectedClient.Id)
                    {
                        gridClients.CurrentCell = row.Cells[0];
                        row.Selected = true;
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Profile
        private void OpenClient(ClientCompany client)
        {
            isNewClient = false;
            BindClient(client);
            ShowProfile(true);
        }
        private void OpenNewClient()
        {
            isNewClient = true;
            selectedClient = null;
            isBinding = true;
            foreach (var t in new[] { txtClientName, txtClientTin, txtClientAddress, txtContactPerson, txtContactNumber, txtEmail }) t.Text = "";
            foreach (var t in customFieldBoxes.Values) t.Text = "";
            isBinding = false;
            isDirty = false;
            currentStores = new List<ClientStore>();
            gridBranches.DataSource = null;
            LoadActivity(null);
            ApplyProfileMode();
            ShowProfile(true);
            txtClientName.Focus();
        }
        private void BindClient(ClientCompany client)
        {
            selectedClient = client;
            isBinding = true;
            txtClientName.Text = client.CompanyName;
            txtClientTin.Text = client.TIN;
            txtClientAddress.Text = client.Address;

            txtContactPerson.Text = client.ContactPerson;
            txtContactNumber.Text = client.ContactNumber;
            txtEmail.Text = client.Email;
            var extra = GridLayoutStore.ReadValues(client.ExtraFieldsJson);
            foreach (var kv in customFieldBoxes) kv.Value.Text = extra.TryGetValue(kv.Key, out var v) ? v : "";
            isBinding = false;
            isDirty = false;
            LoadClientBranches(client.CompanyName);
            LoadActivity(client.CompanyName);
            ApplyProfileMode();
        }
        private void ApplyProfileMode()
        {
            lblProfileTitle.Text = isNewClient ? "New client" : (selectedClient?.CompanyName ?? "Client") + (selectedClient != null && !selectedClient.IsActive ? "   ·  Deactivated" : "");
            btnDelete.Visible = !isNewClient;
            bool deactivated = selectedClient != null && !selectedClient.IsActive;
            btnDelete.Text = deactivated ? "Reactivate client" : "Deactivate client";
            UIThemeHelper.SetButtonRole(btnDelete, deactivated ? "secondary" : "danger");
            btnSave.Text = isNewClient ? "💾  Create client" : "💾  Save changes";
            bool hasClient = !isNewClient && selectedClient != null;
            btnAddBranch.Enabled = hasClient;
            pnlTripsCard.Visible = hasClient;
            pnlBillingCard.Visible = hasClient;
            lblStoresHint.Text = hasClient
                ? "These are the same stores the delivery routes use: picking a route adds the store to it, and a route's stores show up here. Changes save automatically."
                : "Create the client first, then add its stores here.";
            UpdateSaveState();
        }
        private void MarkDirty()
        {
            if (isBinding) return;
            isDirty = true;
            UpdateSaveState();
        }
        private void UpdateSaveState()
        {
            lblSaveState.Text = isDirty ? "● Unsaved changes" : "";
            lblSaveState.ForeColor = isDirty ? AppTheme.WarningText : AppTheme.TextMuted;
        }
        private bool CanLeaveWithoutLosing()
        {
            if (!isDirty) return true;
            var r = MessageBox.Show("Save the changes to this client before leaving?", "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.Yes) return SaveClient(confirm: false);
            isDirty = false;
            return true;
        }
        private void TryLeaveProfile()
        {
            if (!CanLeaveWithoutLosing()) return;
            ShowProfile(false);
            LoadClients();
        }

        private void LoadClientBranches(string clientName)
        {
            var routes = MJdatabase.GetDeliveryRoutesByClient(clientName, activeOnly: false);
            currentStores = MJdatabase.GetClientStores(clientName);
            if (gridBranches.Columns["DestinationRoute"] is DataGridViewComboBoxColumn colDest)
            {
                colDest.Items.Clear();
                colDest.Items.Add("");
                foreach (var r in routes)
                {
                    string label = $"{r.RouteCode} - {r.RouteName}";
                    if (!colDest.Items.Contains(label)) colDest.Items.Add(label);
                }
                // Keep any value already saved on a store, so existing rows never show an error.
                foreach (var st in currentStores)
                    if (!string.IsNullOrWhiteSpace(st.DestinationRoute) && !colDest.Items.Contains(st.DestinationRoute)) colDest.Items.Add(st.DestinationRoute);
            }

            isBinding = true;
            gridBranches.DataSource = null;
            gridBranches.DataSource = currentStores;
            FillStoreCustomCells();
            isBinding = false;
            SizeStoresGrid();
        }

        private void SizeStoresGrid()
        {
            int rows = currentStores.Count;
            int h = Dpi(38) + Math.Max(3, rows) * Dpi(36) + Dpi(4);
            pnlBranchesHost.Height = Math.Clamp(h, Dpi(180), Dpi(540));
            lblStoresTitle.Text = rows == 0 ? "🏪 No stores yet" : $"🏪 {rows} store{(rows == 1 ? "" : "s")}";
        }

        private void LoadActivity(string? clientName)
        {
            flowTripTiles.Controls.Clear();
            flowBillingTiles.Controls.Clear();
            gridTrips.Rows.Clear();
            gridInvoices.Rows.Clear();
            if (string.IsNullOrWhiteSpace(clientName)) return;
            bool Same(string? a) => string.Equals((a ?? "").Trim(), clientName.Trim(), StringComparison.OrdinalIgnoreCase);
            List<Delivery> trips;
            List<Invoice> invoices;
            try { trips = MJdatabase.GetDeliveries().Where(d => Same(d.ClientName)).OrderByDescending(d => d.DispatchDate).ThenByDescending(d => d.Id).ToList(); } catch { trips = new List<Delivery>(); }
            try { invoices = MJdatabase.GetInvoices().Where(i => Same(i.ClientName)).OrderByDescending(i => i.IssueDate).ToList(); } catch { invoices = new List<Invoice>(); }
            var tripSum = SafeTripSummary(clientName);
            int active = trips.Count(d => !string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) && !string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
            flowTripTiles.Controls.Add(Tile("Completed trips", $"{tripSum.CompletedCount:N0}", string.IsNullOrEmpty(tripSum.LatestCompletedDate) ? "None yet" : $"Latest {tripSum.LatestCompletedDate}", AppTheme.SuccessText));
            flowTripTiles.Controls.Add(Tile("Active orders", $"{active:N0}", "Scheduled or on the road", active > 0 ? AppTheme.InfoText : (Color?)null));
            flowTripTiles.Controls.Add(Tile("Special trips", $"{tripSum.SpecialCount:N0}", string.IsNullOrEmpty(tripSum.LatestSpecialDate) ? "None yet" : $"Latest {tripSum.LatestSpecialDate}"));
            flowTripTiles.Controls.Add(Tile("Cancelled", $"{tripSum.CancelledCount:N0}", "Orders called off", tripSum.CancelledCount > 0 ? AppTheme.DangerText : (Color?)null));
            foreach (var d in trips.Take(100))
            {
                int r = gridTrips.Rows.Add(d.DispatchDate.ToString("MMM d, yyyy"), d.DispatchNumber, d.CleanRouteDisplay, d.TruckInfo, (d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate).ToString("N0"), d.Status);
                gridTrips.Rows[r].Tag = d;
            }
            pnlTripsHost.Height = Math.Clamp(Dpi(38) + Math.Max(3, gridTrips.Rows.Count) * Dpi(36) + Dpi(4), Dpi(150), Dpi(420));
            decimal billed = invoices.Where(i => !string.Equals(i.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).Sum(i => i.NetAmount);
            var openInv = invoices.Where(i => !string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase) && !string.Equals(i.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();
            decimal unpaid = openInv.Sum(i => i.NetAmount);
            int overdue = openInv.Count(i => i.DueDate.Date < DateTime.Today);
            ClientPaymentSummary? pay = null;
            try { pay = MJdatabase.GetClientPaymentSummary(clientName); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            flowBillingTiles.Controls.Add(Tile("Total billed", $"₱{billed:N2}", $"{invoices.Count} invoice{(invoices.Count == 1 ? "" : "s")}"));
            flowBillingTiles.Controls.Add(Tile("Collected", $"₱{(pay?.TotalPaidAmount ?? 0):N2}", string.IsNullOrEmpty(pay?.LatestReceiptNumber) ? "No receipts yet" : $"Latest {pay!.LatestReceiptNumber}", AppTheme.SuccessText));
            flowBillingTiles.Controls.Add(Tile("Unpaid", $"₱{unpaid:N2}", $"{openInv.Count} open invoice{(openInv.Count == 1 ? "" : "s")}", unpaid > 0 ? AppTheme.WarningText : (Color?)null));
            flowBillingTiles.Controls.Add(Tile("Overdue", $"{overdue}", overdue > 0 ? "Past their due date" : "Nothing overdue", overdue > 0 ? AppTheme.DangerText : (Color?)null));
            foreach (var inv in invoices.Take(100))
            {
                int r = gridInvoices.Rows.Add(inv.InvoiceNumber, inv.IssueDate.ToString("MMM d, yyyy"), inv.DueDate.ToString("MMM d, yyyy"), string.IsNullOrWhiteSpace(inv.DispatchNumber) ? "—" : inv.DispatchNumber, $"₱{inv.NetAmount:N2}", inv.Status);
                gridInvoices.Rows[r].Tag = inv;
            }
            pnlInvoicesHost.Height = Math.Clamp(Dpi(38) + Math.Max(3, gridInvoices.Rows.Count) * Dpi(36) + Dpi(4), Dpi(150), Dpi(380));
        }

        private static ClientTripSummary SafeTripSummary(string name)
        {
            try { return MJdatabase.GetClientTripSummary(name); } catch { return new ClientTripSummary(); }
        }
        // ------------------------------------------------------------------ Saving
        private bool SaveClient() => SaveClient(confirm: true);
        private bool SaveClient(bool confirm)
        {
            string name = System.Text.RegularExpressions.Regex.Replace(txtClientName.Text.Trim(), @"\s+", " ");
            // Required fields cannot be empty or only spaces.
            var missing = new List<(string Label, TextBox Box)>();
            if (string.IsNullOrWhiteSpace(txtClientName.Text)) missing.Add(("Company name", txtClientName));
            if (string.IsNullOrWhiteSpace(txtClientAddress.Text)) missing.Add(("Address", txtClientAddress));
            if (string.IsNullOrWhiteSpace(txtContactPerson.Text)) missing.Add(("Contact person", txtContactPerson));
            if (string.IsNullOrWhiteSpace(txtContactNumber.Text)) missing.Add(("Contact number", txtContactNumber));
            foreach (var t in new[] { txtClientName, txtClientAddress, txtContactPerson, txtContactNumber }) t.BackColor = AppTheme.InputBack;
            if (missing.Count > 0)
            {
                foreach (var m in missing) m.Box.BackColor = AppTheme.DangerSoft;
                MessageBox.Show("Please fill in these required fields (blank or spaces-only isn't accepted):\n\n• " + string.Join("\n• ", missing.Select(m => m.Label)), "Client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                missing[0].Box.Focus();
                return false;
            }
            string tinDigits = new string(txtClientTin.Text.Where(char.IsDigit).ToArray());
            if (tinDigits.Length > 0 && tinDigits.Length != 9 && tinDigits.Length != 12)
            {
                MessageBox.Show("The TIN should have 9 digits (or 12 with the branch code), e.g. 006-896-371-000.", "Client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtClientTin.Focus();
                return false;
            }
            string email = txtEmail.Text.Trim();
            if (email.Length > 0 && (!email.Contains('@') || !email.Contains('.')))
            {
                MessageBox.Show("Please check the email address.", "Client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }
            string phone = txtContactNumber.Text.Trim();
            string digits = new string(phone.Where(char.IsDigit).ToArray());
            bool phoneShapeOk = phone.All(ch => char.IsDigit(ch) || " -()+/".IndexOf(ch) >= 0) && digits.Length >= 7 && digits.Length <= 13;
            if (phone.Length > 0 && !phoneShapeOk)
            {
                MessageBox.Show("Please enter a valid contact number (e.g. 09171234567 or (02) 8123-4567).", "Client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContactNumber.Focus();
                return false;
            }
            var clash = allClients.FirstOrDefault(c => string.Equals(System.Text.RegularExpressions.Regex.Replace(c.CompanyName.Trim(), @"\s+", " "), name, StringComparison.OrdinalIgnoreCase) && (isNewClient || selectedClient == null || c.Id != selectedClient.Id));
            if (clash != null)
            {
                MessageBox.Show($"A client named \"{clash.CompanyName}\" already exists{(clash.IsActive ? "" : " (deactivated — reactivate it instead)")}. Open that client instead, or use a different name.", "Duplicate client", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtClientName.Focus();
                return false;
            }
            if (confirm)
            {
                string ask = isNewClient ? $"Are you sure you want to add the new client \"{name}\"?" : $"Are you sure you want to save the changes to \"{name}\"?";
                if (MessageBox.Show(ask, isNewClient ? "Add client" : "Update client", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            }
            try
            {
                if (!isNewClient && selectedClient != null && !string.Equals(selectedClient.CompanyName.Trim(), name, StringComparison.Ordinal))
                {
                    var renameOk = MessageBox.Show(
                        $"Rename \"{selectedClient.CompanyName}\" to \"{name}\"?\n\nIts stores, routes, orders, statements and invoices move to the new name too.",
                        "Rename client", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (renameOk != DialogResult.Yes) return false;
                    MJdatabase.RenameClient(selectedClient.CompanyName, name);
                }
                var client = isNewClient || selectedClient == null ? new ClientCompany() : selectedClient;
                client.CompanyName = name;
                client.TIN = txtClientTin.Text.Trim();
                client.Address = txtClientAddress.Text.Trim();
                client.ContactNumber = txtContactNumber.Text.Trim();
                client.Email = email;
                client.ContactPerson = txtContactPerson.Text.Trim();
                var extraValues = GridLayoutStore.ReadValues(client.ExtraFieldsJson);
                foreach (var kv in customFieldBoxes) extraValues[kv.Key] = kv.Value.Text.Trim();
                client.ExtraFieldsJson = GridLayoutStore.WriteValues(extraValues);
                var sp = NameHelper.SplitFullName(client.ContactPerson);
                client.ContactPersonLastName = sp.LastName;
                client.ContactPersonFirstName = sp.FirstName;
                client.ContactPersonMiddleName = sp.MiddleName;
                MJdatabase.SaveClient(client);
                bool wasNew = isNewClient;
                isDirty = false;
                LoadClients();
                var saved = allClients.FirstOrDefault(c => string.Equals(c.CompanyName.Trim(), name, StringComparison.OrdinalIgnoreCase)) ?? client;
                isNewClient = false;
                BindClient(saved);
                ToastHelper.ShowToast(this, wasNew ? $"Client \"{name}\" created. You can add its stores now." : $"Client \"{name}\" saved", ToastType.Success);
                return true;
            }
            catch (Exception ex)
            { ErrLog.Error("ClientManagementView", ex); 
                MessageBox.Show("Couldn't save the client: " + ex.Message, "Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private void DeleteClient()
        {
            if (selectedClient == null || isNewClient) return;
            SetClientActive(selectedClient, !selectedClient.IsActive);
        }
        /// <summary>Soft delete for clients: deactivating hides them from new orders but keeps every store, order, invoice and payment.</summary>
        private void SetClientActive(ClientCompany client, bool active)
        {
            string name = client.CompanyName;
            string msg = active
                ? $"Are you sure you want to reactivate \"{name}\"?\n\nIt will be offered again for new loading orders and dispatches."
                : $"Are you sure you want to deactivate \"{name}\"?\n\n{MJdatabase.DeletePolicyNote("Clients")}\nYou can reactivate it at any time.";
            if (MessageBox.Show(msg, active ? "Reactivate client" : "Deactivate client", MessageBoxButtons.YesNo, active ? MessageBoxIcon.Question : MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                MJdatabase.SetClientActive(name, active);
                LoadClients();
                var fresh = allClients.FirstOrDefault(c => string.Equals(c.CompanyName, name, StringComparison.OrdinalIgnoreCase));
                if (pnlProfilePage.Visible && fresh != null && selectedClient != null && string.Equals(selectedClient.CompanyName, name, StringComparison.OrdinalIgnoreCase)) BindClient(fresh);
                ToastHelper.ShowToast(this, active ? $"\"{name}\" is active again" : $"\"{name}\" deactivated — its history is kept", active ? ToastType.Success : ToastType.Info);
            }
            catch (Exception ex)
            { ErrLog.Error("ClientManagementView", ex); 
                MessageBox.Show("Couldn't change the client's status: " + ex.Message, "Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ------------------------------------------------------------------ Stores
        private void SaveStoreRow(int rowIndex)
        {
            if (isBinding || rowIndex < 0 || rowIndex >= currentStores.Count || selectedClient == null) return;
            var store = currentStores[rowIndex];
            store.ClientCompanyName = selectedClient.CompanyName;
            store.ClientId = selectedClient.Id;
            store.StoreCode = (store.StoreCode ?? "").Trim();
            store.StoreName = (store.StoreName ?? "").Trim();
            if (store.StoreName.Length == 0 || store.StoreCode.Length == 0)
            {
                lblSaveState.Text = store.StoreCode.Length == 0 ? "Type a store code to save this row" : "Type a store name to save this row";
                lblSaveState.ForeColor = AppTheme.WarningText;
                return;
            }
            var sameCode = store.StoreCode.Length == 0 ? null : currentStores.FirstOrDefault(o => !ReferenceEquals(o, store) && string.Equals((o.StoreCode ?? "").Trim(), store.StoreCode, StringComparison.OrdinalIgnoreCase));
            if (sameCode != null)
            {
                MessageBox.Show($"Store code {store.StoreCode} is already used by \"{sameCode.StoreName}\". Each store needs its own code.", "Stores", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show($"Are you sure you want to save the changes to store \"{store.StoreCode} - {store.StoreName}\"?", "Save store", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                BeginInvoke(new Action(() => LoadClientBranches(selectedClient.CompanyName)));
                lblSaveState.Text = "Store change not saved";
                lblSaveState.ForeColor = AppTheme.TextMuted;
                return;
            }
            try
            {
                MJdatabase.SaveClientStore(store);
                lblSaveState.Text = $"Store saved {DateTime.Now:h:mm tt}";
                lblSaveState.ForeColor = AppTheme.SuccessText;
            }
            catch (Exception ex)
            { ErrLog.Error("ClientManagementView", ex); 
                MessageBox.Show("Couldn't save the store: " + ex.Message, "Stores", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadClientBranches(selectedClient.CompanyName);
            }
        }

        private void AddNewBranchRow()
        {
            if (selectedClient == null || isNewClient)
            {
                QuietBox.Show("Create the client first, then add its stores.", "Stores", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to add a new store to \"{selectedClient.CompanyName}\"?", "Add store", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int count = currentStores.Count + 1;
            string code;
            do { code = $"S{count:D2}"; count++; } while (currentStores.Any(s => string.Equals(s.StoreCode, code, StringComparison.OrdinalIgnoreCase)));
            // New stores start without a route; picking one in the "Delivery route" column adds the store to that route.
            string defaultDest = "";
            var newStore = new ClientStore
            {
                ClientId = selectedClient.Id,
                ClientCompanyName = selectedClient.CompanyName,
                StoreCode = code,
                StoreName = "New store",
                DestinationRoute = defaultDest
            };

            MJdatabase.SaveClientStore(newStore);
            LoadClientBranches(selectedClient.CompanyName);
            int idx = currentStores.FindIndex(s => s.Id == newStore.Id);
            if (idx < 0) idx = gridBranches.Rows.Count - 1;
            if (idx >= 0 && idx < gridBranches.Rows.Count)
            {
                gridBranches.CurrentCell = gridBranches.Rows[idx].Cells["StoreName"];
                gridBranches.BeginEdit(true);
            }
        }

        private void DeleteBranchRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= currentStores.Count || selectedClient == null) return;
            var store = currentStores[rowIndex];

            if (MJdatabase.IsClientStoreInUse(store, out var usedOn))
            {
                QuietBox.Show($"Store \"{store.StoreCode} - {store.StoreName}\" appears on a past {usedOn}, so it can't be removed without breaking that history.\n\nIf the store changed, rename it instead.", "Store is part of history", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var res = MessageBox.Show($"Are you sure you want to delete store \"{store.StoreCode} - {store.StoreName}\"?\n\nIt has never been used on an order, so nothing in the history refers to it.", "Delete store", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes) return;
            try { MJdatabase.DeleteClientStore(store.Id); }
            catch (Exception ex) { ErrLog.Error("ClientManagementView", ex);  MessageBox.Show(ex.Message, "Delete store", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            LoadClientBranches(selectedClient.CompanyName);
        }

        // ------------------------------------------------------------------ Table customization
        private void InitializeTableLayouts()
        {
            clientListDefaults = GridLayoutStore.DefaultsFrom(gridClients, lockedKeys: new[] { "CompanyName" });
            clientListPrefs = GridLayoutStore.Load(ClientsListKey, clientListDefaults);
            GridLayoutStore.Apply(gridClients, clientListPrefs, p => new DataGridViewTextBoxColumn { HeaderText = p.Header, FillWeight = 100, MinimumWidth = Dpi(80), ReadOnly = true });
            storeDefaults = GridLayoutStore.DefaultsFrom(gridBranches, lockedKeys: new[] { "StoreName" }, excludeKeys: new[] { "Actions" });
            storePrefs = GridLayoutStore.Load(StoresKey, storeDefaults);
            GridLayoutStore.Apply(gridBranches, storePrefs, p => new DataGridViewTextBoxColumn
            {
                HeaderText = p.Header,
                FillWeight = 100,
                MinimumWidth = Dpi(90),
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) }
            });
            RebuildCustomClientFields();
        }
        private void CustomizeClientList()
        {
            using var dlg = new TableCustomizeDialog("Customize the clients table",
                "Pick the columns to show, rename or reorder them, or add your own columns. Values for your own columns are filled in on each client's profile.",
                clientListPrefs, clientListDefaults, allowCustom: true);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            clientListPrefs = dlg.Result;
            GridLayoutStore.Save(ClientsListKey, clientListPrefs);
            GridLayoutStore.Apply(gridClients, clientListPrefs, p => new DataGridViewTextBoxColumn { HeaderText = p.Header, FillWeight = 100, MinimumWidth = Dpi(80), ReadOnly = true });
            FillClientCustomCells();
            RebuildCustomClientFields();
            ToastHelper.ShowToast(this, "Clients table layout saved", ToastType.Success);
        }
        private void CustomizeStoresTable()
        {
            using var dlg = new TableCustomizeDialog("Customize the stores table",
                "Pick the columns to show, rename or reorder them, or add your own (e.g. Store manager, Contact #, Opening hours). Type values straight into the table.",
                storePrefs, storeDefaults, allowCustom: true);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            storePrefs = dlg.Result;
            GridLayoutStore.Save(StoresKey, storePrefs);
            isBinding = true;
            GridLayoutStore.Apply(gridBranches, storePrefs, p => new DataGridViewTextBoxColumn
            {
                HeaderText = p.Header,
                FillWeight = 100,
                MinimumWidth = Dpi(90),
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) }
            });
            FillStoreCustomCells();
            isBinding = false;
            ToastHelper.ShowToast(this, "Stores table layout saved", ToastType.Success);
        }
        private void FillClientCustomCells()
        {
            var customCols = gridClients.Columns.Cast<DataGridViewColumn>().Where(c => c.Name.StartsWith(GridLayoutStore.CustomPrefix, StringComparison.Ordinal)).ToList();
            if (customCols.Count == 0) return;
            foreach (DataGridViewRow row in gridClients.Rows)
            {
                if (row.DataBoundItem is not ClientRow cr) continue;
                var values = GridLayoutStore.ReadValues(cr.Client.ExtraFieldsJson);
                foreach (var c in customCols) row.Cells[c.Index].Value = values.TryGetValue(c.Name, out var v) ? v : "";
            }
        }
        private void FillStoreCustomCells()
        {
            var customCols = gridBranches.Columns.Cast<DataGridViewColumn>().Where(c => c.Name.StartsWith(GridLayoutStore.CustomPrefix, StringComparison.Ordinal)).ToList();
            if (customCols.Count == 0) return;
            bool was = isBinding;
            isBinding = true;
            for (int i = 0; i < gridBranches.Rows.Count && i < currentStores.Count; i++)
            {
                var values = GridLayoutStore.ReadValues(currentStores[i].ExtraFieldsJson);
                foreach (var c in customCols) gridBranches.Rows[i].Cells[c.Index].Value = values.TryGetValue(c.Name, out var v) ? v : "";
            }
            isBinding = was;
        }
        /// <summary>The client's own custom fields (from the clients table layout) as inputs on the profile.</summary>
        private void RebuildCustomClientFields()
        {
            var custom = clientListPrefs.Where(p => p.Custom).ToList();
            var current = customFieldBoxes.ToDictionary(kv => kv.Key, kv => kv.Value.Text);
            pnlCustomFields.SuspendLayout();
            foreach (Control c in pnlCustomFields.Controls.Cast<Control>().ToList()) { pnlCustomFields.Controls.Remove(c); c.Dispose(); }
            customFieldBoxes.Clear();
            if (custom.Count == 0)
            {
                pnlCustomFields.Visible = false;
                pnlCustomFields.Height = 0;
                pnlCustomFields.ResumeLayout(true);
                return;
            }
            int cols = 3;
            int rows = (custom.Count + cols - 1) / cols;
            var lbl = new Label { Dock = DockStyle.Top, Height = Dpi(30), Text = "Your custom fields", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.BottomLeft };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Top, Height = rows * Dpi(66), ColumnCount = cols, RowCount = rows, BackColor = AppTheme.Surface };
            for (int c = 0; c < cols; c++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / cols));
            for (int r = 0; r < rows; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(66)));
            for (int i = 0; i < custom.Count; i++)
            {
                var p = custom[i];
                var box = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = current.TryGetValue(p.Key, out var v) ? v : "" };
                box.TextChanged += (s, e) => MarkDirty();
                customFieldBoxes[p.Key] = box;
                tbl.Controls.Add(Field(p.Header, box), i % cols, i / cols);
            }
            pnlCustomFields.Controls.Add(tbl);
            pnlCustomFields.Controls.Add(lbl);
            pnlCustomFields.Height = Dpi(30) + rows * Dpi(66);
            pnlCustomFields.Visible = true;
            pnlCustomFields.ResumeLayout(true);
            if (selectedClient != null && !isNewClient)
            {
                bool was = isBinding;
                isBinding = true;
                var extra = GridLayoutStore.ReadValues(selectedClient.ExtraFieldsJson);
                foreach (var kv in customFieldBoxes) kv.Value.Text = extra.TryGetValue(kv.Key, out var val) ? val : kv.Value.Text;
                isBinding = was;
            }
        }
        // ------------------------------------------------------------------ Helpers
        private void TxtClientTin_TextChanged(object? sender, EventArgs e)
        {
            string raw = new string(txtClientTin.Text.Where(char.IsDigit).ToArray());
            if (raw.Length > 12) raw = raw.Substring(0, 12);

            string formatted = "";
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && i % 3 == 0 && i <= 9) formatted += "-";
                formatted += raw[i];
            }

            if (txtClientTin.Text != formatted)
            {
                int cursor = txtClientTin.SelectionStart;
                txtClientTin.Text = formatted;
                txtClientTin.SelectionStart = Math.Min(cursor + 1, txtClientTin.Text.Length);
            }
        }

        private void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text);
                ToastHelper.ShowToast(this, "Copied", ToastType.Success);
            }
            catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
        }
    }
}
