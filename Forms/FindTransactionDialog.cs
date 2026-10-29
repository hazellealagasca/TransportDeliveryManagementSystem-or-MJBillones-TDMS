using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// "I don't remember the number" helper: the user fills in whatever they remember (client, about when, truck,
    /// driver or aide, roughly how many crates or how much) and the closest transactions are listed best match first.
    /// </summary>
    public sealed class FindTransactionDialog : Form
    {
        private readonly List<TxRecord> all;
        private readonly bool showAmount;
        private readonly ComboBox cbClient = new ComboBox(), cbType = new ComboBox(), cbTruck = new ComboBox(), cbCrew = new ComboBox(), cbStatus = new ComboBox();
        private readonly NumericUpDown numCrates = new NumericUpDown(), numAmount = new NumericUpDown();
        private readonly TextBox txtWords = new TextBox();
        private readonly DateFilterChip dateChip = new DateFilterChip(DatePresets.History, "All dates");
        private readonly DataGridView grid = new DataGridView();
        private readonly Label lblResult = new Label();
        private readonly Button btnOpen = new Button(), btnClose = new Button(), btnReset = new Button();
        private readonly System.Windows.Forms.Timer delay = new System.Windows.Forms.Timer { Interval = 250 };
        private List<(TxRecord R, int Score, int Clues)> results = new List<(TxRecord, int, int)>();
        public TxRecord? Chosen { get; private set; }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public FindTransactionDialog(List<TxRecord> records, bool showAmount)
        {
            all = records ?? new List<TxRecord>();
            this.showAmount = showAmount;
            AutoScaleMode = AutoScaleMode.None;
            Text = "Find a transaction";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(D(1180), D(680));
            MinimumSize = new Size(D(900), D(520));
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.5F);
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;
            BuildUi();
            delay.Tick += (s, e) => { delay.Stop(); Search(); };
            Disposed += (s, e) => delay.Dispose();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
            Shown += (s, e) => { cbClient.Focus(); Search(); };
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
        }
        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(D(16), D(14), D(16), D(8)), BackColor = AppTheme.Canvas };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            // ---- clues
            var clues = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoScroll = true, GrowStyle = TableLayoutPanelGrowStyle.AddRows, BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(12), D(16), D(12)), Margin = new Padding(D(0), D(0), D(14), D(0)) };
            clues.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            clues.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, clues.Width - 1, clues.Height - 1); };
            void Head(string t, string sub)
            {
                clues.Controls.Add(new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(D(0), D(0), D(0), D(2)), UseMnemonic = false });
                clues.Controls.Add(new Label { Text = sub, AutoSize = true, MaximumSize = new Size(D(300), 0), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Margin = new Padding(D(0), D(0), D(0), D(10)), UseMnemonic = false });
            }
            void Field(string label, Control c)
            {
                clues.Controls.Add(new Label { Text = label, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(D(0), D(6), D(0), D(3)), UseMnemonic = false });
                if (c is FilterChip) c.Anchor = AnchorStyles.Left; else c.Dock = DockStyle.Fill; c.Margin = new Padding(D(0), D(0), D(0), D(4));
                clues.Controls.Add(c);
            }
            Head("What do you remember?", "Fill in only what you know — even one clue helps. The best matches appear first.");
            Combo(cbClient, true, new[] { "" }.Concat(Distinct(all.Select(r => r.Client))));
            Field("Client / company", cbClient);
            Field("About when", dateChip);
            Combo(cbType, false, new[] { "Any type", TxRecord.KDispatch, TxRecord.KLoading, TxRecord.KSpecial });
            Field("Type", cbType);
            Combo(cbTruck, true, new[] { "" }.Concat(Distinct(all.Select(r => r.Truck))));
            Field("Truck (plate)", cbTruck);
            var crewNames = all.SelectMany(r => new[] { r.Driver }.Concat((r.Aides ?? "").Split(',')).Select(n => n.Trim()));
            Combo(cbCrew, true, new[] { "" }.Concat(Distinct(crewNames)));
            Field("Driver or aide", cbCrew);
            numCrates.Maximum = 100000; numCrates.ThousandsSeparator = true;
            Field("About how many crates  (0 = not sure)", numCrates);
            if (showAmount)
            {
                numAmount.Maximum = 100000000; numAmount.DecimalPlaces = 2; numAmount.ThousandsSeparator = true;
                Field("About how much ₱  (0 = not sure)", numAmount);
            }
            Combo(cbStatus, false, new[] { "Any status", "Waiting", "On the road", "Completed", "Cancelled" });
            Field("Status", cbStatus);
            txtWords.PlaceholderText = "route, store, remark, part of the number…";
            Field("Anything else you remember", txtWords);
            btnReset.Text = "Clear all clues"; btnReset.AutoSize = true; btnReset.Margin = new Padding(D(0), D(12), D(0), D(0));
            UIThemeHelper.SetButtonRole(btnReset, "secondary");
            btnReset.Click += (s, e) => ResetClues();
            clues.Controls.Add(btnReset);
            dateChip.FitWidth();
            foreach (Control c in new Control[] { cbClient, cbTruck, cbCrew, txtWords }) c.TextChanged += (s, e) => Kick();
            foreach (var cb in new[] { cbClient, cbType, cbTruck, cbCrew, cbStatus }) cb.SelectedIndexChanged += (s, e) => Kick();
            numCrates.ValueChanged += (s, e) => Kick();
            numAmount.ValueChanged += (s, e) => Kick();
            dateChip.RangeChanged += (s, e) => { dateChip.FitWidth(); Search(); };
            // ---- results
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(10), D(8), D(10), D(6)) };
            right.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, right.Width - 1, right.Height - 1); };
            lblResult.Dock = DockStyle.Top; lblResult.Height = D(30); lblResult.Font = new Font("Segoe UI Semibold", 10F); lblResult.ForeColor = AppTheme.TextPrimary; lblResult.TextAlign = ContentAlignment.MiddleLeft; lblResult.UseMnemonic = false;
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.BorderStyle = BorderStyle.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            UIThemeHelper.ApplyGridTheme(grid);
            grid.RowTemplate.Height = D(32);
            void C(string n, string h, int w) => grid.Columns.Add(new DataGridViewTextBoxColumn { Name = n, HeaderText = h, FillWeight = w, SortMode = DataGridViewColumnSortMode.NotSortable });
            C("match", "Match", 36); C("date", "Date", 46); C("kind", "Type", 42); C("num", "Number", 52); C("client", "Client", 70); C("route", "Route", 60);
            C("truck", "Truck", 40); C("crew", "Driver · aides", 64); C("crates", "Crates", 30); if (showAmount) C("amount", "Amount", 40); C("status", "Status", 40);
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.CellStyle == null || grid.Rows[e.RowIndex].Tag is not ValueTuple<TxRecord, int, int> t) return;
                if (grid.Columns[e.ColumnIndex].Name == "match")
                {
                    bool full = t.Item2 == t.Item3;
                    e.CellStyle.ForeColor = full ? AppTheme.SuccessText : AppTheme.WarningText;
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Regular);
                }
            };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Choose(); };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Choose(); e.SuppressKeyPress = true; } };
            right.Controls.Add(grid);
            right.Controls.Add(lblResult);
            root.Controls.Add(clues, 0, 0);
            root.Controls.Add(right, 1, 0);
            // ---- footer
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = D(54), Padding = new Padding(D(16), D(8), D(16), D(8)), BackColor = AppTheme.Canvas };
            btnOpen.Text = "Open selected"; btnOpen.AutoSize = true; btnOpen.MinimumSize = new Size(D(140), D(36));
            UIThemeHelper.SetButtonRole(btnOpen, "primary");
            btnOpen.Click += (s, e) => Choose();
            btnClose.Text = "Close"; btnClose.AutoSize = true; btnClose.MinimumSize = new Size(D(100), D(36)); btnClose.DialogResult = DialogResult.Cancel;
            UIThemeHelper.SetButtonRole(btnClose, "secondary");
            foot.Controls.Add(btnOpen);
            foot.Controls.Add(btnClose);
            CancelButton = btnClose;
            Controls.Add(root);
            Controls.Add(foot);
        }
        private static IEnumerable<string> Distinct(IEnumerable<string?> xs) => xs.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        private static void Combo(ComboBox cb, bool editable, IEnumerable<string> items)
        {
            cb.DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            cb.Items.AddRange(items.Cast<object>().ToArray());
            cb.MaxDropDownItems = 14;
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }
        private void Kick() { delay.Stop(); delay.Start(); }
        private void ResetClues()
        {
            foreach (var cb in new[] { cbClient, cbType, cbTruck, cbCrew, cbStatus }) { cb.Text = ""; if (cb.Items.Count > 0) cb.SelectedIndex = 0; }
            numCrates.Value = 0; numAmount.Value = 0; txtWords.Clear();
            dateChip.SetQuietly(DateRange.All); dateChip.FitWidth();
            Search();
        }
        private static bool Has(string? hay, string needle) => !string.IsNullOrWhiteSpace(hay) && hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        private void Search()
        {
            string client = cbClient.Text.Trim(), truck = cbTruck.Text.Trim(), crew = cbCrew.Text.Trim(), words = txtWords.Text.Trim();
            string type = cbType.SelectedIndex > 0 ? cbType.Text : "", status = cbStatus.SelectedIndex > 0 ? cbStatus.Text : "";
            int crates = (int)numCrates.Value; decimal amount = numAmount.Value;
            var range = dateChip.Range;
            int clues = 0;
            if (client.Length > 0) clues++;
            if (truck.Length > 0) clues++;
            if (crew.Length > 0) clues++;
            if (words.Length > 0) clues++;
            if (type.Length > 0) clues++;
            if (status.Length > 0) clues++;
            if (crates > 0) clues++;
            if (showAmount && amount > 0) clues++;
            if (!range.IsAll) clues++;
            results.Clear();
            if (clues > 0)
            {
                foreach (var r in all)
                {
                    int s = 0;
                    if (client.Length > 0 && (Has(r.Client, client) || (r.Client.Length > 2 && Has(client, r.Client)))) s++;
                    if (truck.Length > 0 && Has(r.Truck.Replace(" ", "").Replace("-", ""), truck.Replace(" ", "").Replace("-", ""))) s++;
                    if (crew.Length > 0 && (Has(r.Driver, crew) || Has(r.Aides, crew))) s++;
                    if (words.Length > 0 && TransactionIndex.Matches(r, words)) s++;
                    if (type.Length > 0 && r.Kind == type) s++;
                    if (status.Length > 0 && r.StatusGroup == status) s++;
                    if (crates > 0 && r.Crates > 0 && Math.Abs(r.Crates - crates) <= Math.Max(5, crates * 0.15)) s++;
                    if (showAmount && amount > 0 && r.Amount.HasValue && Math.Abs(r.Amount.Value - amount) <= Math.Max(100m, amount * 0.10m)) s++;
                    if (!range.IsAll && range.Contains(r.Date)) s++;
                    if (s > 0) results.Add((r, s, clues));
                }
            }
            results = results.OrderByDescending(x => x.Score).ThenByDescending(x => x.R.Date).Take(300).ToList();
            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (var x in results)
            {
                var r = x.R;
                var cells = new List<object> { x.Score == clues ? "✔ all clues" : $"{x.Score} of {clues}", r.Date.ToString("ddd, MMM d, yyyy"), r.Kind, r.Number, r.Client, r.Route, r.Truck, r.Crew, r.Crates > 0 ? r.Crates.ToString("N0") : "" };
                if (showAmount) cells.Add(r.Amount.HasValue ? "₱" + r.Amount.Value.ToString("N2") : "");
                cells.Add(r.Status);
                int i = grid.Rows.Add(cells.ToArray());
                grid.Rows[i].Tag = x;
            }
            grid.ResumeLayout();
            int full = results.Count(x => x.Score == clues);
            lblResult.Text = clues == 0 ? "Add a clue on the left to start." :
                results.Count == 0 ? "No match. Try fewer clues or a wider date." :
                full > 0 ? $"{full:N0} match{(full == 1 ? "" : "es")} with every clue" + (results.Count > full ? $"  ·  {results.Count - full:N0} close match(es) below" : "") :
                $"No exact match — {results.Count:N0} close match(es), best first";
            btnOpen.Enabled = results.Count > 0;
            if (grid.Rows.Count > 0) { grid.ClearSelection(); grid.Rows[0].Selected = true; grid.CurrentCell = grid.Rows[0].Cells[1]; }
        }
        private void Choose()
        {
            if (grid.CurrentRow?.Tag is not ValueTuple<TxRecord, int, int> t) return;
            Chosen = t.Item1;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
