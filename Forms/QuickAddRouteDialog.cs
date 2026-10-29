using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Quick way to set up a delivery route while making a dispatch. Problems are caught while typing:
    /// taken route codes and names, the same origin and destination, duplicate stores, and stores that are already on
    /// another route of the client. Stores, places and route names are suggested as you type.
    /// </summary>
    public class QuickAddRouteDialog : Form
    {
        public DeliveryRoute? CreatedRoute { get; private set; }

        private ComboBox cbClient = null!;
        private TextBox txtRouteCode = null!;
        private TextBox txtOrigin = null!;
        private TextBox txtRouteName = null!;
        private TextBox txtDescription = null!;
        private TextBox txtStore = null!;
        private Button btnAddStore = null!;
        private ListBox lstStores = null!;
        private Button btnRemoveStore = null!, btnUp = null!, btnDown = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private FlowLayoutPanel flowIssues = null!;
        private Label lblSummary = null!;
        private readonly ErrorProvider errors = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private List<DeliveryRoute> allRoutes = new List<DeliveryRoute>();
        private List<ClientStore> clientStores = new List<ClientStore>();
        private bool loading;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public QuickAddRouteDialog(string? initialClient = null)
        {
            loading = true;
            FormClosing += (s, e) => DarkModeKit.StopAutoComplete(this);
            try { allRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: false); } catch { allRoutes = new List<DeliveryRoute>(); }
            InitializeCustomComponent();
            PopulateClients(initialClient);
            txtRouteCode.Text = SafeNextCode();
            txtOrigin.Text = MJdatabase.GetSystemSetting("CompanyAddress", "M.J. Billones Depot, Tarlac City");
            loading = false;
            LoadClientContext();
            UIThemeHelper.ApplyTheme(this);
            CheckAll(false);
        }

        private static string SafeNextCode()
        {
            try { return MJdatabase.GenerateNextRouteCode(); } catch { return ""; }
        }
        // ------------------------------------------------------------------ layout
        private void InitializeCustomComponent()
        {
            Text = "Quick add delivery route";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(D(760), D(640));
            MinimumSize = new Size(D(700), D(600));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;
            var header = new Panel { Dock = DockStyle.Top, Height = D(70), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(12), D(24), D(10)) };
            header.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1); };
            var lblTitle = new Label { Text = "New delivery route", Dock = DockStyle.Top, Height = D(28), Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary };
            var lblSub = new Label { Text = "Pick the client, where the truck starts and where it goes, then the stores it serves. Problems show below as you type.", Dock = DockStyle.Top, Height = D(20), Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true };
            header.Controls.Add(lblSub);
            header.Controls.Add(lblTitle);
            // Body: left column details, right column stores
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(D(20), D(16), D(20), D(8)), BackColor = AppTheme.Canvas };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 12, BackColor = AppTheme.Canvas, Margin = new Padding(0, 0, D(12), 0) };
            for (int i = 0; i < 12; i++) left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cbClient = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, 0, D(10)) };
            cbClient.SelectedIndexChanged += (s, e) => { if (!loading) { LoadClientContext(); CheckAll(false); } };
            txtRouteCode = Box("e.g. R5");
            txtRouteCode.CharacterCasing = CharacterCasing.Upper;
            txtRouteCode.MaxLength = 12;
            txtRouteCode.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != '-') e.Handled = true; };
            var codeRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, D(10)) };
            codeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            codeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            txtRouteCode.Margin = new Padding(0, 0, D(8), 0);
            var btnNextCode = new Button { Text = "Next free code", AutoSize = true, Height = D(30), Cursor = Cursors.Hand, Margin = Padding.Empty };
            UIThemeHelper.SetButtonRole(btnNextCode, "secondary");
            btnNextCode.Click += (s, e) => { txtRouteCode.Text = SafeNextCode(); };
            codeRow.Controls.Add(txtRouteCode, 0, 0);
            codeRow.Controls.Add(btnNextCode, 1, 0);
            txtOrigin = Box("Where the truck loads, e.g. Tarlac City depot");
            txtRouteName = Box("Destination / route name, e.g. La Union – Ilocos Sur");
            txtDescription = Box("Optional notes (roads, cut-off times…)");
            txtDescription.Multiline = true;
            txtDescription.Height = D(54);
            chkIsActive = new CheckBox { Text = "Active — offer this route for new orders", Checked = true, AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(4), 0, 0) };
            left.Controls.Add(Caption("Client company *"));
            left.Controls.Add(cbClient);
            left.Controls.Add(Caption("Route code *"));
            left.Controls.Add(codeRow);
            left.Controls.Add(Caption("Origin (start point) *"));
            left.Controls.Add(txtOrigin);
            left.Controls.Add(Caption("Destination / route name *"));
            left.Controls.Add(txtRouteName);
            left.Controls.Add(Caption("Description"));
            left.Controls.Add(txtDescription);
            left.Controls.Add(chkIsActive);
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = AppTheme.Canvas, Margin = new Padding(D(12), 0, 0, 0) };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var addRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, D(8)) };
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            addRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            txtStore = Box("Type a store (suggestions appear) and press Enter");
            txtStore.Margin = new Padding(0, 0, D(8), 0);
            txtStore.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AddStore(); e.Handled = true; e.SuppressKeyPress = true; } };
            btnAddStore = new Button { Text = "＋ Add", AutoSize = true, Height = D(30), Cursor = Cursors.Hand, Margin = Padding.Empty };
            UIThemeHelper.SetButtonRole(btnAddStore, "secondary");
            btnAddStore.Click += (s, e) => AddStore();
            addRow.Controls.Add(txtStore, 0, 0);
            addRow.Controls.Add(btnAddStore, 1, 0);
            lstStores = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary };
            lstStores.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) RemoveStore(); };
            lstStores.SelectedIndexChanged += (s, e) => UpdateStoreButtons();
            var storeBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, D(6), 0, 0) };
            btnUp = SmallButton("▲ Up", () => MoveStore(-1));
            btnDown = SmallButton("▼ Down", () => MoveStore(1));
            btnRemoveStore = SmallButton("✕ Remove", RemoveStore);
            storeBtns.Controls.AddRange(new Control[] { btnUp, btnDown, btnRemoveStore });
            right.Controls.Add(Caption("Stores served, in delivery order *"), 0, 0);
            right.Controls.Add(addRow, 0, 1);
            right.Controls.Add(lstStores, 0, 2);
            right.Controls.Add(storeBtns, 0, 3);
            body.Controls.Add(left, 0, 0);
            body.Controls.Add(right, 1, 0);
            // Live checks
            var issuesCard = new Panel { Dock = DockStyle.Bottom, Height = D(118), BackColor = AppTheme.Canvas, Padding = new Padding(D(20), 0, D(20), D(8)) };
            var issuesInner = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(14), D(8), D(14), D(8)) };
            issuesInner.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(p, 0, 0, issuesInner.Width - 1, issuesInner.Height - 1); };
            lblSummary = new Label { Dock = DockStyle.Top, Height = D(22), Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, AutoEllipsis = true };
            flowIssues = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AppTheme.Surface };
            issuesInner.Controls.Add(flowIssues);
            issuesInner.Controls.Add(lblSummary);
            issuesCard.Controls.Add(issuesInner);
            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(62), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(12)) };
            footer.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            btnSave = new Button { Text = "Save route", Dock = DockStyle.Right, Width = D(140), Cursor = Cursors.Hand };
            btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = D(100), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnCancel, "secondary");
            btnSave.Click += BtnSave_Click;

            footer.Controls.Add(btnSave);
            footer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = D(10), BackColor = Color.Transparent });
            footer.Controls.Add(btnCancel);
            Controls.Add(body);
            Controls.Add(issuesCard);
            Controls.Add(footer);
            Controls.Add(header);
            CancelButton = btnCancel;
            foreach (var t in new[] { txtRouteCode, txtOrigin, txtRouteName }) t.TextChanged += (s, e) => { if (!loading) CheckAll(false); };
            txtRouteName.Leave += (s, e) => txtRouteName.Text = Tidy(txtRouteName.Text);
            txtOrigin.Leave += (s, e) => txtOrigin.Text = Tidy(txtOrigin.Text);
        }

        private TextBox Box(string cue) => new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), PlaceholderText = cue, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(10)) };
        private Label Caption(string text) => new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, D(3)) };
        private Button SmallButton(string text, Action click)
        {
            var b = new Button { Text = text, AutoSize = true, Height = D(28), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(6), 0) };
            UIThemeHelper.SetButtonRole(b, "secondary");
            b.Click += (s, e) => click();
            return b;
        }

        private static string Tidy(string? s) => Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        // ------------------------------------------------------------------ data
        private void PopulateClients(string? initialClient)
        {
            cbClient.Items.Clear();
            List<string> names;
            try { names = MJdatabase.GetClientNames(); } catch { names = new List<string>(); }
            foreach (var n in names) cbClient.Items.Add(n);
            if (!string.IsNullOrWhiteSpace(initialClient))
            {
                int idx = cbClient.FindStringExact(initialClient.Trim());
                if (idx >= 0) { cbClient.SelectedIndex = idx; return; }
            }
            if (cbClient.Items.Count == 1) cbClient.SelectedIndex = 0;
        }
        /// <summary>Loads the chosen client's stores and refreshes the type-ahead lists.</summary>
        private void LoadClientContext()
        {
            string client = cbClient.Text.Trim();
            try { clientStores = client.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(client); } catch { clientStores = new List<ClientStore>(); }
            var storeSuggest = new AutoCompleteStringCollection();
            foreach (var st in clientStores) storeSuggest.Add(StoreLabel(st.StoreCode, st.StoreName));
            foreach (var r in allRoutes.Where(r => SameClient(r, client))) foreach (var p in r.PlacesList) storeSuggest.Add(p);
            txtStore.AutoCompleteCustomSource = storeSuggest;
            txtStore.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtStore.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var placeSuggest = new AutoCompleteStringCollection();
            foreach (var r in allRoutes) { placeSuggest.Add(r.RouteName); }
            foreach (var r in allRoutes) foreach (var p in r.PlacesList) placeSuggest.Add(DeliveryRoute.SplitStoreCodeAndName(p).StoreName);
            txtRouteName.AutoCompleteCustomSource = placeSuggest;
            txtRouteName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtRouteName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var originSuggest = new AutoCompleteStringCollection();
            originSuggest.Add(MJdatabase.GetSystemSetting("CompanyAddress", ""));
            foreach (var r in allRoutes)
            {
                var m = Regex.Match(r.Description ?? "", @"Origin:\s*(.+?)(\s*→|$)");
                if (m.Success) originSuggest.Add(m.Groups[1].Value.Trim());
            }
            txtOrigin.AutoCompleteCustomSource = originSuggest;
            txtOrigin.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtOrigin.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }
        private static bool SameClient(DeliveryRoute r, string client) => string.Equals((r.ClientCompanyName ?? "").Trim(), client.Trim(), StringComparison.OrdinalIgnoreCase);
        private static string StoreLabel(string? code, string? name) => string.IsNullOrWhiteSpace(code) ? (name ?? "").Trim() : $"{code!.Trim()} - {(name ?? "").Trim()}";
        private static string StoreKey(string raw)
        {
            var (code, name) = DeliveryRoute.SplitStoreCodeAndName(raw);
            return (code.Length > 0 ? code : name).Trim().ToLowerInvariant();
        }
        // ------------------------------------------------------------------ stores list
        private void AddStore()
        {
            string raw = Tidy(txtStore.Text);
            if (raw.Length == 0) return;
            // Typed only a code or only a name that matches a known store: use the full "code - name".
            var known = clientStores.FirstOrDefault(st => string.Equals(st.StoreCode?.Trim(), raw, StringComparison.OrdinalIgnoreCase) || string.Equals(st.StoreName?.Trim(), raw, StringComparison.OrdinalIgnoreCase));
            if (known != null) raw = StoreLabel(known.StoreCode, known.StoreName);
            string key = StoreKey(raw);
            if (lstStores.Items.Cast<object>().Any(o => StoreKey(o.ToString() ?? "") == key))
            {
                ShowInline(txtStore, $"\"{raw}\" is already on this route.");
                txtStore.SelectAll();
                return;
            }
            var (code, name) = DeliveryRoute.SplitStoreCodeAndName(raw);
            if (code.Length > 0)
            {
                var owner = clientStores.FirstOrDefault(st => string.Equals(st.StoreCode?.Trim(), code, StringComparison.OrdinalIgnoreCase));
                if (owner != null && !string.Equals(owner.StoreName.Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    ShowInline(txtStore, $"Store code {code} already belongs to \"{owner.StoreName}\". Use that store or another code.");
                    return;
                }
            }

            errors.SetError(txtStore, "");
            lstStores.Items.Add(raw);
            lstStores.SelectedIndex = lstStores.Items.Count - 1;
            txtStore.Clear();
            txtStore.Focus();
            CheckAll(false);
        }
        private void RemoveStore()
        {
            int i = lstStores.SelectedIndex;
            if (i < 0) return;
            lstStores.Items.RemoveAt(i);
            if (lstStores.Items.Count > 0) lstStores.SelectedIndex = Math.Min(i, lstStores.Items.Count - 1);
            CheckAll(false);
        }
        private void MoveStore(int step)
        {
            int i = lstStores.SelectedIndex, j = i + step;
            if (i < 0 || j < 0 || j >= lstStores.Items.Count) return;
            var item = lstStores.Items[i];
            lstStores.Items.RemoveAt(i);
            lstStores.Items.Insert(j, item);
            lstStores.SelectedIndex = j;
        }
        private void UpdateStoreButtons()
        {
            int i = lstStores.SelectedIndex;
            btnRemoveStore.Enabled = i >= 0;
            btnUp.Enabled = i > 0;
            btnDown.Enabled = i >= 0 && i < lstStores.Items.Count - 1;
        }
        private void ShowInline(Control c, string message)
        {
            errors.SetError(c, message);
            AddIssue(message, error: true);
        }
        // ------------------------------------------------------------------ live validation
        private readonly List<(string Text, bool Error)> issues = new List<(string, bool)>();
        private void AddIssue(string text, bool error)
        {
            if (issues.Any(x => x.Text == text)) return;
            issues.Add((text, error));
            RenderIssues();
        }
        /// <summary>Checks everything; returns true when the route can be saved. Warnings never block.</summary>
        private bool CheckAll(bool showAll)
        {
            issues.Clear();
            foreach (var c in new Control[] { cbClient, txtRouteCode, txtOrigin, txtRouteName, lstStores }) errors.SetError(c, "");
            string client = cbClient.Text.Trim();
            string code = txtRouteCode.Text.Trim();
            string origin = Tidy(txtOrigin.Text);
            string name = Tidy(txtRouteName.Text);
            void Err(Control c, string msg) { errors.SetError(c, msg); issues.Add((msg, true)); }
            void Warn(string msg) => issues.Add((msg, false));
            if (client.Length == 0) { if (showAll) Err(cbClient, "Choose the client this route belongs to."); else issues.Add(("Choose the client this route belongs to.", true)); }
            if (code.Length == 0) Err(txtRouteCode, "Route code is required (e.g. R5).");
            else
            {
                var clash = allRoutes.FirstOrDefault(r => string.Equals(r.RouteCode.Trim(), code, StringComparison.OrdinalIgnoreCase));
                if (clash != null) Err(txtRouteCode, $"Route code {code} is already used by \"{clash.RouteName}\". Click \"Next free code\".");
            }
            if (origin.Length == 0) Err(txtOrigin, "Origin is required — where does the truck start?");
            if (name.Length == 0) { if (showAll) Err(txtRouteName, "Destination / route name is required."); else issues.Add(("Destination / route name is required.", true)); }
            else
            {
                if (name.Length < 3) Err(txtRouteName, "Destination name is too short.");
                if (origin.Length > 0 && string.Equals(origin, name, StringComparison.OrdinalIgnoreCase)) Err(txtRouteName, "Origin and destination can't be the same place.");
                var same = allRoutes.FirstOrDefault(r => SameClient(r, client) && string.Equals(Tidy(r.RouteName), name, StringComparison.OrdinalIgnoreCase));
                if (same != null) Err(txtRouteName, $"{client} already has a route named \"{same.RouteName}\" ({same.RouteCode}){(same.IsActive ? "" : ", currently inactive")}. Use that route instead.");
                else
                {
                    var similar = allRoutes.Where(r => SameClient(r, client) && Similar(r.RouteName, name)).Take(2).ToList();
                    foreach (var r in similar) Warn($"Similar route already exists: {r.RouteCode} - {r.RouteName}. Check it isn't the same trip.");
                }
            }
            if (lstStores.Items.Count == 0) issues.Add(("Add at least one store this route serves.", true));
            else if (client.Length > 0)
            {
                var keys = lstStores.Items.Cast<object>().Select(o => StoreKey(o.ToString() ?? "")).ToHashSet();
                foreach (var r in allRoutes.Where(r => SameClient(r, client) && r.IsActive))
                {
                    var shared = r.PlacesList.Where(p => keys.Contains(StoreKey(p))).ToList();
                    if (shared.Count > 0) Warn($"{shared.Count} of these store(s) are already on {r.RouteCode} - {r.RouteName} ({string.Join(", ", shared.Take(3))}{(shared.Count > 3 ? "…" : "")}).");
                    if (shared.Count > 0 && shared.Count == keys.Count && r.PlacesList.Count == keys.Count) issues.Add(($"{r.RouteCode} - {r.RouteName} already has exactly these stores. Use that route instead.", true));
                }
                int unknown = lstStores.Items.Cast<object>().Count(o => !clientStores.Any(st => StoreKey(StoreLabel(st.StoreCode, st.StoreName)) == StoreKey(o.ToString() ?? "")));
                if (unknown > 0) Warn($"{unknown} new store(s) will be added to {client}'s store list.");
            }
            RenderIssues();
            bool ok = !issues.Any(x => x.Error);
            btnSave.Enabled = ok;
            return ok;
        }

        private static bool Similar(string a, string b)
        {
            string x = Tidy(a).ToLowerInvariant(), y = Tidy(b).ToLowerInvariant();
            if (x.Length < 3 || y.Length < 3) return false;
            if (x.Contains(y) || y.Contains(x)) return true;
            var wa = Regex.Split(x, @"[^a-z0-9]+").Where(w => w.Length > 3).ToHashSet();
            var wb = Regex.Split(y, @"[^a-z0-9]+").Where(w => w.Length > 3).ToHashSet();
            return wa.Count > 0 && wb.Count > 0 && wa.Intersect(wb).Count() >= Math.Min(2, Math.Min(wa.Count, wb.Count));
        }
        private void RenderIssues()
        {
            flowIssues.SuspendLayout();
            flowIssues.Controls.Clear();
            int errs = issues.Count(x => x.Error), warns = issues.Count(x => !x.Error);
            lblSummary.Text = errs == 0 && warns == 0 ? "✓ Everything checks out — ready to save."
                : errs == 0 ? $"✓ Ready to save · {warns} thing(s) to double-check"
                : $"{errs} thing(s) to fix before saving" + (warns > 0 ? $" · {warns} to double-check" : "");
            lblSummary.ForeColor = errs == 0 ? AppTheme.SuccessText : AppTheme.DangerText;
            foreach (var (text, isError) in issues.OrderByDescending(x => x.Error))
            {
                flowIssues.Controls.Add(new Label
                {
                    Text = (isError ? "●  " : "▲  ") + text,
                    AutoSize = true,
                    MaximumSize = new Size(Math.Max(D(300), flowIssues.ClientSize.Width - D(24)), 0),
                    ForeColor = isError ? AppTheme.DangerText : AppTheme.WarningText,
                    Font = new Font("Segoe UI", 9F),
                    Margin = new Padding(0, 0, 0, D(2))
                });
            }
            flowIssues.ResumeLayout();
        }
        // ------------------------------------------------------------------ save
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            txtRouteName.Text = Tidy(txtRouteName.Text);
            txtOrigin.Text = Tidy(txtOrigin.Text);
            if (!CheckAll(true))
            {
                MessageBox.Show(this, "Please fix the items marked in red first.", "Route not ready", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string client = cbClient.Text.Trim();
            string code = txtRouteCode.Text.Trim();
            string name = txtRouteName.Text;
            var stores = lstStores.Items.Cast<object>().Select(o => o.ToString() ?? "").ToList();
            string notes = txtDescription.Text.Trim();
            string description = $"Origin: {txtOrigin.Text} → Destination: {name}" + (notes.Length > 0 ? $" · {notes}" : "");
            var confirm = MessageBox.Show(this,
                $"Are you sure you want to add this route?\n\nClient: {client}\nRoute: {code} - {name}\nFrom: {txtOrigin.Text}\nStores ({stores.Count}): {string.Join(" ➔ ", stores.Take(6))}{(stores.Count > 6 ? " …" : "")}",
                "Add delivery route", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            var newRoute = new DeliveryRoute
            {
                ClientCompanyName = client,
                RouteCode = code,
                RouteName = name,
                Description = description,
                ConnectedPlaces = string.Join("; ", stores),
                IsActive = chkIsActive.Checked
            };

            bool success;
            try { success = MJdatabase.SaveDeliveryRoute(newRoute); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Couldn't save the route: " + ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!success)
            {
                MessageBox.Show(this, "Couldn't save the route. The code or name may have just been taken — refresh and try again.", "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CreatedRoute = newRoute;
            DarkModeKit.StopAutoComplete(this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
