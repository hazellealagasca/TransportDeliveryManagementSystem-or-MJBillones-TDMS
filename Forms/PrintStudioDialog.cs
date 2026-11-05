using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Opens before anything is printed. Left: what goes on the page — title, paper, company header, every field
    /// (editable label and value), which parts and table columns to include, signature lines, notes. Right: a live
    /// preview. Choices can be remembered per form.
    /// </summary>
    public sealed class PrintStudioDialog : Form
    {
        private readonly PrintFormDef def;
        private PrintOptions o;
        private readonly Panel optionsHost;
        private readonly FlowLayoutPanel stack;
        private readonly WebBrowser web;
        private readonly CheckBox chkRemember;
        private readonly System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer { Interval = 350 };
        private int keepScroll;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public PrintStudioDialog(PrintFormDef def)
        {
            this.def = def;
            o = PrintStore.Load(def);
            Text = $"Print · {def.Name}";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;
            // Readable everywhere: 10.5 pt base text (captions, groups and buttons sized up from it).
            Font = new Font("Segoe UI", 10.5F);
            BackColor = AppTheme.Canvas;
            // Checkboxes and radio buttons take their text colour from here (they were black on the dark theme).
            ForeColor = AppTheme.TextPrimary;
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Size = new Size((int)(wa.Width * 0.9), (int)(wa.Height * 0.9));
            MinimumSize = new Size(Math.Min(wa.Width, D(980)), Math.Min(wa.Height, D(620)));
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            // Top
            var top = new Panel { Dock = DockStyle.Top, Height = D(80), BackColor = AppTheme.Surface, Padding = new Padding(D(22), D(12), D(22), D(10)) };
            top.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1); };
            top.Controls.Add(new Label { Text = "Choose what goes on the page — the preview on the right updates as you go. Untick anything you don't want printed; click a label to rename it.", Dock = DockStyle.Top, Height = D(26), ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F), AutoEllipsis = true, UseMnemonic = false });
            top.Controls.Add(new Label { Text = "Print " + (def.Name.Length == 0 ? "" : def.Name.Contains(':') ? def.Name : char.ToLowerInvariant(def.Name[0]) + def.Name.Substring(1)), Dock = DockStyle.Top, Height = D(32), Font = new Font("Segoe UI Semibold", 15F), ForeColor = AppTheme.TextPrimary, AutoEllipsis = true, UseMnemonic = false });
            // Bottom
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = D(70), BackColor = AppTheme.Surface, Padding = new Padding(D(22), D(14), D(22), D(14)) };
            bottom.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, bottom.Width, 0); };
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent };
            right.Controls.Add(Btn("🖨  Print", "primary", (s, e) => Output(true)));
            right.Controls.Add(Btn("Open in browser / PDF", "secondary", (s, e) => Output(false)));
            right.Controls.Add(Btn("Save as file…", "secondary", (s, e) => SaveFile()));
            right.Controls.Add(Btn("Cancel", "secondary", (s, e) => Close()));
            chkRemember = new CheckBox { Text = "Remember my choices for this form", Checked = true, AutoSize = true, Dock = DockStyle.Left, ForeColor = AppTheme.TextSecondary, Padding = new Padding(0, D(8), D(18), 0) };
            var reset = new LinkLabel { Text = "Reset to defaults", AutoSize = true, Dock = DockStyle.Left, LinkColor = AppTheme.AccentText, Padding = new Padding(0, D(10), 0, 0), LinkBehavior = LinkBehavior.HoverUnderline };
            reset.LinkClicked += (s, e) =>
            {
                if (MessageBox.Show(this, "Put this form back to its standard layout? Your saved choices for it will be cleared.", "Reset to defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                PrintStore.Reset(def.Key);
                o = def.Defaults.Clone();
                BuildOptions();
                Schedule();
            };
            bottom.Controls.Add(right);
            bottom.Controls.Add(reset);
            bottom.Controls.Add(chkRemember);
            // Left options
            // The options column is about 40% of the window (500–660 px), so labels and boxes have room.
            optionsHost = new Panel { Dock = DockStyle.Left, Width = OptionsWidth(Size.Width), AutoScroll = true, BackColor = AppTheme.Surface, Padding = new Padding(D(22), D(10), D(14), D(22)) };
            optionsHost.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, optionsHost.Width - 1, 0, optionsHost.Width - 1, optionsHost.Height); };
            stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(D(22), D(10)), BackColor = Color.Transparent };
            optionsHost.Controls.Add(stack);
            // Preview
            var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(16)) };
            var paper = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
            paper.Paint += (s, e) => { using var p = new Pen(AppTheme.BorderStrong); e.Graphics.DrawRectangle(p, 0, 0, paper.Width - 1, paper.Height - 1); };
            web = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true, IsWebBrowserContextMenuEnabled = false, AllowWebBrowserDrop = false, WebBrowserShortcutsEnabled = false };
            web.DocumentCompleted += (s, e) =>
            {
                try { var html = web.Document?.GetElementsByTagName("html"); if (html != null && html.Count > 0 && html[0] is HtmlElement el) el.ScrollTop = keepScroll; } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            };
            paper.Controls.Add(web);
            previewHost.Controls.Add(paper);
            previewHost.Controls.Add(new Label { Text = "Preview", Dock = DockStyle.Top, Height = D(28), ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI Semibold", 10.5F) });
            Controls.Add(previewHost);
            Controls.Add(optionsHost);
            Controls.Add(bottom);
            Controls.Add(top);
            debounce.Tick += (s, e) => { debounce.Stop(); RenderPreview(); };
            optionsHost.Resize += (s, e) => FitRows();
            Resize += (s, e) => { int w = OptionsWidth(ClientSize.Width); if (Math.Abs(optionsHost.Width - w) > D(4)) optionsHost.Width = w; };
            Shown += (s, e) => { BuildOptions(); RenderPreview(); };
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.Control && e.KeyCode == Keys.P) { Output(true); e.Handled = true; }
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) debounce.Dispose();
            base.Dispose(disposing);
        }
        private int OptionsWidth(int windowWidth) => Math.Max(D(500), Math.Min(D(660), (int)(windowWidth * 0.40)));
        private Button Btn(string text, string role, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(D(104), D(40)), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10.5F), Margin = new Padding(D(10), 0, 0, 0), Padding = new Padding(D(12), 0, D(12), 0), Cursor = Cursors.Hand, UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, role);
            b.Click += click;
            return b;
        }
        // ------------------------------------------------------------------------------------------ options UI
        private int RowWidth => Math.Max(D(320), optionsHost.ClientSize.Width - optionsHost.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - D(4));
        /// <summary>
        /// Holds a row to the column width. Rows that size themselves (label + box groups) ignored a plain Width, so a box
        /// shrank to its text ("Statement c…"); a fixed minimum and maximum width keeps every box full width.
        /// </summary>
        private void Fit(Control c)
        {
            int w = RowWidth;
            c.MinimumSize = new Size(w, 0);
            c.MaximumSize = new Size(w, 0);
            c.Width = w;
            if (c is GroupPanel g) SizeGroup(g, w);
        }
        private void FitRows()
        {
            stack.SuspendLayout();
            foreach (Control c in stack.Controls)
            {
                Fit(c);
                if (c is FlowLayoutPanel f && f.FlowDirection == FlowDirection.TopDown) foreach (Control k in f.Controls) Fit(k);
            }
            stack.ResumeLayout(true);
        }
        private void Schedule() { debounce.Stop(); debounce.Start(); }
        /// <summary>
        /// The mouse wheel over a closed drop-down or number box scrolls the options instead of changing the value
        /// (scrolling down the list used to switch e.g. the watermark to "Confidential").
        /// </summary>
        private void WheelScrollsOptions(Control c)
        {
            c.MouseWheel += (s, e) =>
            {
                if (c is ComboBox cb && cb.DroppedDown) return;
                if (e is HandledMouseEventArgs h) h.Handled = true;
                int y = -optionsHost.AutoScrollPosition.Y - e.Delta;
                optionsHost.AutoScrollPosition = new Point(0, Math.Max(0, y));
            };
        }
        private void BuildOptions()
        {
            stack.SuspendLayout();
            foreach (Control c in stack.Controls.Cast<Control>().ToList()) c.Dispose();
            stack.Controls.Clear();
            // Document
            Group("Document");
            Add(LabeledText("Title", o.Title, v => o.Title = v));
            Add(LabeledText("Subtitle / reference", o.Subtitle, v => o.Subtitle = v, multiline: true));
            Add(Pair(LabeledCombo("Copy label", new[] { "", "Original", "2nd copy", "3rd copy", "File copy", "Client copy" }, PrintKit.CopyLabelText(o.CopyLabel), v => o.CopyLabel = v, editable: true),
                     LabeledCombo("Watermark", new[] { "", "Draft", "Copy", "Paid", "Cancelled", "Confidential" }, o.Watermark, v => o.Watermark = v, editable: true)));
            Add(Pair(LabeledCombo("Paper", new[] { "Letter", "A4", "Legal", "Long (8.5 x 13)" }, o.Paper, v => o.Paper = v),
                     LabeledCombo("Orientation", new[] { "Portrait", "Landscape" }, o.Orientation, v => o.Orientation = v)));
            var sizes = new Dictionary<string, int> { ["Small"] = 90, ["Normal"] = 100, ["Large"] = 112, ["Extra large"] = 125 };
            string curSize = sizes.OrderBy(kv => Math.Abs(kv.Value - o.FontPct)).First().Key;
            Add(Pair(LabeledCombo("Text size", sizes.Keys.ToArray(), curSize, v => o.FontPct = sizes.TryGetValue(v, out var p) ? p : 100),
                     LabeledCombo("Margins", new[] { "Narrow", "Normal", "Wide" }, o.Margins, v => o.Margins = v)));
            Add(LabeledCombo("Style", new[] { "Black & white", "Color" }, o.Tone, v => o.Tone = v));
            if (o.HasRowScope)
            {
                var all = new RadioButton { Text = "All rows in the list", Checked = !o.SelectedRowsOnly, AutoSize = true, Margin = new Padding(0, D(4), D(18), D(4)) };
                var sel = new RadioButton { Text = o.SelectedRowCount == 1 ? "Only the 1 selected row" : $"Only the {o.SelectedRowCount} selected rows", Checked = o.SelectedRowsOnly, AutoSize = true, Margin = new Padding(0, D(4), 0, D(4)) };
                sel.CheckedChanged += (s, e) => { o.SelectedRowsOnly = sel.Checked; Schedule(); };
                var f = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(6)) };
                f.Controls.Add(Caption("Rows to print"));
                f.SetFlowBreak(f.Controls[0], true);
                f.Controls.Add(all);
                f.Controls.Add(sel);
                Add(f);
            }
            if (o.HasBlankRows)
            {
                var num = new NumericUpDown { Minimum = 0, Maximum = 40, Value = Math.Max(0, Math.Min(40, o.BlankRows)), Width = D(90), Margin = new Padding(0, D(2), 0, 0) };
                WheelScrollsOptions(num);
                num.ValueChanged += (s, e) => { o.BlankRows = (int)num.Value; Schedule(); };
                var f = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(10)) };
                f.Controls.Add(new Label { Text = "Table rows (blank lines are added to reach this)", AutoSize = true, Margin = new Padding(0, D(6), D(10), D(4)), ForeColor = AppTheme.TextSecondary });
                f.Controls.Add(num);
                Add(f);
            }
            // Header
            Group("Company header");
            var logo = new CheckBox { Text = "Show the company logo", Checked = o.ShowLogo, AutoSize = true, Margin = new Padding(0, D(2), 0, D(10)) };
            logo.CheckedChanged += (s, e) => { o.ShowLogo = logo.Checked; Schedule(); };
            Add(logo);
            foreach (var f in o.Header) Add(FieldRow(f));
            // Fields
            if (o.Fields.Count > 0)
            {
                Group("Details on the form", "Tick what to show. Click a label to rename it; values come from the record but can be changed for this printout.");
                foreach (var f in o.Fields) Add(FieldRow(f));
            }
            // Sections
            if (o.Sections.Count > 0)
            {
                Group("Include");
                Add(CheckGrid(o.Sections));
            }
            // Columns
            if (o.Columns.Count > 0)
            {
                Group("Table columns");
                var links = new FlowLayoutPanel { AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(4)) };
                CheckGridPanel? grid = null;
                LinkLabel L(string t, bool on)
                {
                    var l = new LinkLabel { Text = t, AutoSize = true, LinkColor = AppTheme.AccentText, Margin = new Padding(0, 0, D(18), D(4)), LinkBehavior = LinkBehavior.HoverUnderline };
                    l.LinkClicked += (s, e) => { foreach (var c in o.Columns) c.On = on; grid?.Sync(); Schedule(); };
                    return l;
                }
                links.Controls.Add(L("Select all", true));
                links.Controls.Add(L("Clear all", false));
                Add(links);
                grid = CheckGrid(o.Columns);
                Add(grid);
            }
            // Signatures
            Group("Signature lines", "Caption, printed name and role under the line.");
            var sigHost = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            void RebuildSigs()
            {
                foreach (Control c in sigHost.Controls.Cast<Control>().ToList()) c.Dispose();
                sigHost.Controls.Clear();
                foreach (var sig in o.Signatures.ToList()) sigHost.Controls.Add(SigRow(sig, () => { o.Signatures.Remove(sig); RebuildSigs(); Schedule(); }));
                foreach (Control c in sigHost.Controls) Fit(c);
            }
            RebuildSigs();
            Add(sigHost);
            var addSig = new LinkLabel { Text = "＋ Add a signature line", AutoSize = true, LinkColor = AppTheme.AccentText, Margin = new Padding(0, D(6), 0, D(10)), LinkBehavior = LinkBehavior.HoverUnderline };
            addSig.LinkClicked += (s, e) => { o.Signatures.Add(new PrintSig("Signed by", "", "")); RebuildSigs(); Schedule(); };
            Add(addSig);
            // Notes
            Group("Notes & footer");
            Add(LabeledText("Note printed under the form", o.Notes, v => o.Notes = v, multiline: true));
            Add(LabeledText("Footer text", o.Footer, v => o.Footer = v, multiline: true));
            stack.ResumeLayout(true);
            FitRows();
        }
        private void Add(Control c) { Fit(c); stack.Controls.Add(c); }
        /// <summary>A group heading with a line under it, and an optional hint below that wraps onto as many lines as it needs.</summary>
        private sealed class GroupPanel : Panel
        {
            public Label Title = null!;
            public Label? Hint;
        }
        private void Group(string title, string hint = "")
        {
            var p = new GroupPanel { BackColor = Color.Transparent, Margin = new Padding(0, D(16), 0, D(6)) };
            p.Title = new Label { Text = title.ToUpperInvariant(), Dock = DockStyle.Top, Height = D(34), Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.AccentText, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false, Padding = new Padding(0, 0, 0, D(4)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, D(34), p.Width, D(34)); };
            if (hint.Length > 0)
            {
                p.Hint = new Label { Text = hint, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, Padding = new Padding(0, D(6), 0, 0) };
                p.Controls.Add(p.Hint);
            }
            p.Controls.Add(p.Title);
            Fit(p);
            stack.Controls.Add(p);
        }
        /// <summary>Group panels are sized from their text: the title, plus the hint wrapped to the column width.</summary>
        private void SizeGroup(GroupPanel g, int width)
        {
            int h = D(36);
            if (g.Hint != null) h += TextRenderer.MeasureText(g.Hint.Text, g.Hint.Font, new Size(Math.Max(D(100), width), 0), TextFormatFlags.WordBreak).Height + D(10);
            g.Height = h;
        }
        private Label Caption(string text) => new Label { Text = text, AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 10F), Margin = new Padding(0, 0, 0, D(4)), UseMnemonic = false };
        private Control LabeledText(string caption, string value, Action<string> set, bool multiline = false)
        {
            var p = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(12)) };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var tb = new TextBox { Text = value, Dock = DockStyle.Fill, Multiline = multiline, Height = multiline ? D(64) : D(30), ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
            tb.TextChanged += (s, e) => { set(tb.Text); Schedule(); };
            if (!multiline) WheelScrollsOptions(tb);
            p.Controls.Add(Caption(caption), 0, 0);
            p.Controls.Add(tb, 0, 1);
            return p;
        }
        private Control LabeledCombo(string caption, string[] items, string value, Action<string> set, bool editable = false)
        {
            var p = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(12)), Dock = DockStyle.Fill };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var cb = new ComboBox { DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
            WheelScrollsOptions(cb);
            cb.Items.AddRange(items.Select(i => (object)(i.Length == 0 ? "(none)" : i)).ToArray());
            string shown = string.IsNullOrEmpty(value) ? "(none)" : value;
            if (cb.Items.Contains(shown)) cb.SelectedItem = shown; else if (editable) cb.Text = value; else if (cb.Items.Count > 0) cb.SelectedIndex = 0;
            void Push() { string v = cb.Text == "(none)" ? "" : cb.Text; set(v); Schedule(); }
            cb.SelectedIndexChanged += (s, e) => Push();
            if (editable) cb.TextChanged += (s, e) => Push();
            p.Controls.Add(Caption(caption), 0, 0);
            p.Controls.Add(cb, 0, 1);
            return p;
        }
        private Control Pair(Control a, Control b)
        {
            var t = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            a.Margin = new Padding(0, 0, D(12), D(12));
            b.Margin = new Padding(0, 0, 0, D(12));
            a.Dock = DockStyle.Fill; b.Dock = DockStyle.Fill;
            t.Controls.Add(a, 0, 0);
            t.Controls.Add(b, 1, 0);
            return t;
        }
        /// <summary>[include] [editable label] [value] — one per field.</summary>
        private Control FieldRow(PrintField f)
        {
            var t = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(8)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(30)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(180)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var chk = new CheckBox { Checked = f.On, AutoSize = true, Margin = new Padding(0, D(7), 0, 0) };
            var lbl = new TextBox { Text = f.Label, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 10F), Margin = new Padding(0, D(7), D(8), 0) };
            var val = new TextBox { Text = f.Value, Dock = DockStyle.Fill, Multiline = f.Multiline, Height = f.Multiline ? D(60) : D(30), ScrollBars = f.Multiline ? ScrollBars.Vertical : ScrollBars.None, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
            var tips = new ToolTip();
            WheelScrollsOptions(lbl);
            if (!f.Multiline) WheelScrollsOptions(val);
            tips.SetToolTip(lbl, "Click to rename this label");
            if (f.FromData) tips.SetToolTip(val, "Filled from the record — changes here only affect this printout");
            void Dim() { lbl.ForeColor = chk.Checked ? AppTheme.TextSecondary : AppTheme.TextMuted; val.Enabled = chk.Checked; }
            chk.CheckedChanged += (s, e) => { f.On = chk.Checked; Dim(); Schedule(); };
            lbl.TextChanged += (s, e) => { f.Label = lbl.Text; Schedule(); };
            lbl.Enter += (s, e) => lbl.BackColor = AppTheme.InputBack;
            lbl.Leave += (s, e) => lbl.BackColor = AppTheme.Surface;
            val.TextChanged += (s, e) => { f.Value = val.Text; Schedule(); };
            Dim();
            t.Controls.Add(chk, 0, 0);
            t.Controls.Add(lbl, 1, 0);
            t.Controls.Add(val, 2, 0);
            return t;
        }
        /// <summary>
        /// One signature line: [include] Caption | Role on the first line, the printed name across the full width below
        /// (three narrow boxes in one line cut long names off), and ✕ to remove it.
        /// </summary>
        private Control SigRow(PrintSig s, Action remove)
        {
            var t = new TableLayoutPanel { ColumnCount = 4, RowCount = 4, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(14)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(30)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(30)));
            var chk = new CheckBox { Checked = s.On, AutoSize = true, Margin = new Padding(0, D(4), 0, 0) };
            TextBox Tb(string v, Action<string> set)
            {
                var tb = new TextBox { Text = v, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, D(8), D(6)) };
                tb.TextChanged += (x, e) => { set(tb.Text); Schedule(); };
                WheelScrollsOptions(tb);
                return tb;
            }
            var cap = Tb(s.Caption, v => s.Caption = v);
            var role = Tb(s.Role, v => s.Role = v);
            var name = Tb(s.Name, v => s.Name = v);
            var del = new LinkLabel { Text = "✕", AutoSize = true, LinkColor = AppTheme.TextMuted, ActiveLinkColor = AppTheme.DangerText, Margin = new Padding(0, D(4), 0, 0), LinkBehavior = LinkBehavior.NeverUnderline, Font = new Font("Segoe UI Semibold", 11F) };
            new ToolTip().SetToolTip(del, "Remove this signature line");
            del.LinkClicked += (x, e) => remove();
            void Dim() { cap.Enabled = role.Enabled = name.Enabled = chk.Checked; }
            chk.CheckedChanged += (x, e) => { s.On = chk.Checked; Dim(); Schedule(); };
            Label H(string text) => new Label { Text = text, AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 0, D(3)) };
            t.Controls.Add(chk, 0, 0);
            t.Controls.Add(H("Caption"), 1, 0);
            t.Controls.Add(H("Role / position"), 2, 0);
            t.Controls.Add(del, 3, 0);
            t.Controls.Add(cap, 1, 1);
            t.Controls.Add(role, 2, 1);
            t.Controls.Add(H("Printed name"), 1, 2);
            t.SetColumnSpan(t.GetControlFromPosition(1, 2)!, 2);
            t.Controls.Add(name, 1, 3);
            t.SetColumnSpan(name, 2);
            Dim();
            return t;
        }
        private sealed class CheckGridPanel : TableLayoutPanel
        {
            public Action Sync = () => { };
        }
        private CheckGridPanel CheckGrid(List<PrintChoice> choices)
        {
            // Two columns only when every label fits in half the width; otherwise one column, so no label is cut off.
            int widest = choices.Count == 0 ? 0 : choices.Max(c => TextRenderer.MeasureText(c.Label ?? "", Font).Width) + D(34);
            int cols = widest * 2 + D(12) <= RowWidth ? 2 : 1;
            var t = new CheckGridPanel { ColumnCount = cols, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(8)) };
            for (int k = 0; k < cols; k++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / cols));
            t.RowCount = (choices.Count + cols - 1) / cols;
            var boxes = new List<(CheckBox Box, PrintChoice Choice)>();
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                var cb = new CheckBox { Text = c.Label, Checked = c.On, AutoSize = true, Margin = new Padding(0, D(4), D(10), D(4)), UseMnemonic = false };
                cb.CheckedChanged += (s, e) => { c.On = cb.Checked; Schedule(); };
                boxes.Add((cb, c));
                t.Controls.Add(cb, i % cols, i / cols);
            }
            t.Sync = () => { foreach (var (b, c) in boxes) b.Checked = c.On; };
            return t;
        }
        // ------------------------------------------------------------------------------------------ output
        private void RenderPreview()
        {
            try
            {
                var html = web.Document?.GetElementsByTagName("html");
                keepScroll = html != null && html.Count > 0 && html[0] is HtmlElement el ? el.ScrollTop : 0;
            }
            catch { keepScroll = 0; }
            try { web.DocumentText = PrintKit.Page(def, o, autoPrint: false, preview: true); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
        }
        private string WriteTemp(bool autoPrint)
        {
            string safe = string.Concat(def.FileName.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(Path.GetTempPath(), $"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(path, PrintKit.Page(def, o, autoPrint), Encoding.UTF8);
            return path;
        }
        private void Remember()
        {
            if (chkRemember.Checked) PrintStore.Save(o);
        }
        private void Output(bool autoPrint)
        {
            try
            {
                Remember();
                Process.Start(new ProcessStartInfo { FileName = WriteTemp(autoPrint), UseShellExecute = true });
                if (autoPrint) Close();
            }
            catch (Exception ex)
            { ErrLog.Error("PrintStudioDialog", ex); 
                MessageBox.Show(this, "Couldn't open the printout: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void SaveFile()
        {
            using var dlg = new SaveFileDialog { Filter = "Web page (*.html)|*.html", FileName = def.FileName + ".html", Title = "Save printout" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                Remember();
                File.WriteAllText(dlg.FileName, PrintKit.Page(def, o, false), Encoding.UTF8);
                ToastHelper.ShowToast(this, "Saved — open it in a browser to print or save as PDF", ToastType.Success);
            }
            catch (Exception ex)
            { ErrLog.Error("PrintStudioDialog", ex); 
                MessageBox.Show(this, "Couldn't save the file: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class PrintStudio
    {
        /// <summary>Opens the print setup window for a form.</summary>
        public static void Show(PrintFormDef def)
        {
            try
            {
                var owner = Form.ActiveForm ?? (IWin32Window?)MainForm.Instance;
                using var dlg = new Forms.PrintStudioDialog(def);
                if (owner != null) dlg.ShowDialog(owner); else dlg.ShowDialog();
            }
            catch (Exception ex)
            { ErrLog.Error("PrintStudioDialog", ex); 
                MessageBox.Show("Unable to open the print window: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static void Show(Func<PrintFormDef> build)
        {
            PrintFormDef def;
            try { def = build(); }
            catch (Exception ex)
            { ErrLog.Error("PrintStudioDialog", ex); 
                MessageBox.Show("Couldn't prepare this printout: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Show(def);
        }
    }
}
