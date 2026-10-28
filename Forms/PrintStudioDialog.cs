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
            Font = new Font("Segoe UI", 9.5F);
            BackColor = AppTheme.Canvas;
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Size = new Size((int)(wa.Width * 0.9), (int)(wa.Height * 0.9));
            MinimumSize = new Size(Math.Min(wa.Width, D(980)), Math.Min(wa.Height, D(620)));
            try { ThemeManager.StyleWindowChrome(this); } catch { }
            // Top
            var top = new Panel { Dock = DockStyle.Top, Height = D(64), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(10), D(20), D(8)) };
            top.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1); };
            top.Controls.Add(new Label { Text = "Choose what goes on the page — the preview on the right updates as you go. Untick anything you don't want printed; click a label to rename it.", Dock = DockStyle.Top, Height = D(22), ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), UseMnemonic = false });
            top.Controls.Add(new Label { Text = $"Print {def.Name.ToLowerInvariant()}", Dock = DockStyle.Top, Height = D(26), Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false });
            // Bottom
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = D(62), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(12)) };
            bottom.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, bottom.Width, 0); };
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent };
            right.Controls.Add(Btn("🖨  Print", "primary", (s, e) => Output(true)));
            right.Controls.Add(Btn("Open in browser / PDF", "secondary", (s, e) => Output(false)));
            right.Controls.Add(Btn("Save as file…", "secondary", (s, e) => SaveFile()));
            right.Controls.Add(Btn("Cancel", "secondary", (s, e) => Close()));
            chkRemember = new CheckBox { Text = "Remember my choices for this form", Checked = true, AutoSize = true, Dock = DockStyle.Left, ForeColor = AppTheme.TextSecondary, Padding = new Padding(0, D(6), D(16), 0) };
            var reset = new LinkLabel { Text = "Reset to defaults", AutoSize = true, Dock = DockStyle.Left, LinkColor = AppTheme.AccentText, Padding = new Padding(0, D(8), 0, 0), LinkBehavior = LinkBehavior.HoverUnderline };
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
            optionsHost = new Panel { Dock = DockStyle.Left, Width = D(440), AutoScroll = true, BackColor = AppTheme.Surface, Padding = new Padding(D(18), D(8), D(10), D(18)) };
            optionsHost.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, optionsHost.Width - 1, 0, optionsHost.Width - 1, optionsHost.Height); };
            stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(D(18), D(8)), BackColor = Color.Transparent };
            optionsHost.Controls.Add(stack);
            // Preview
            var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(16)) };
            var paper = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
            paper.Paint += (s, e) => { using var p = new Pen(AppTheme.BorderStrong); e.Graphics.DrawRectangle(p, 0, 0, paper.Width - 1, paper.Height - 1); };
            web = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true, IsWebBrowserContextMenuEnabled = false, AllowWebBrowserDrop = false, WebBrowserShortcutsEnabled = false };
            web.DocumentCompleted += (s, e) =>
            {
                try { var html = web.Document?.GetElementsByTagName("html"); if (html != null && html.Count > 0 && html[0] is HtmlElement el) el.ScrollTop = keepScroll; } catch { }
            };
            paper.Controls.Add(web);
            previewHost.Controls.Add(paper);
            previewHost.Controls.Add(new Label { Text = "Preview", Dock = DockStyle.Top, Height = D(24), ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI Semibold", 9F) });
            Controls.Add(previewHost);
            Controls.Add(optionsHost);
            Controls.Add(bottom);
            Controls.Add(top);
            debounce.Tick += (s, e) => { debounce.Stop(); RenderPreview(); };
            optionsHost.Resize += (s, e) => FitRows();
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
        private Button Btn(string text, string role, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(D(96), D(38)), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F), Margin = new Padding(D(8), 0, 0, 0), Padding = new Padding(D(10), 0, D(10), 0), Cursor = Cursors.Hand, UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, role);
            b.Click += click;
            return b;
        }
        // ------------------------------------------------------------------------------------------ options UI
        private int RowWidth => Math.Max(D(300), optionsHost.ClientSize.Width - optionsHost.Padding.Horizontal - D(8));
        private void FitRows()
        {
            foreach (Control c in stack.Controls) c.Width = RowWidth;
        }
        private void Schedule() { debounce.Stop(); debounce.Start(); }
        private void BuildOptions()
        {
            stack.SuspendLayout();
            foreach (Control c in stack.Controls.Cast<Control>().ToList()) c.Dispose();
            stack.Controls.Clear();
            // Document
            Group("Document");
            Add(LabeledText("Title", o.Title, v => o.Title = v));
            Add(LabeledText("Subtitle / reference", o.Subtitle, v => o.Subtitle = v, multiline: true));
            Add(Pair(LabeledCombo("Copy label", new[] { "", "Original", "Duplicate", "Triplicate", "File copy", "Client copy" }, o.CopyLabel, v => o.CopyLabel = v, editable: true),
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
                var all = new RadioButton { Text = "All rows in the list", Checked = !o.SelectedRowsOnly, AutoSize = true, Margin = new Padding(0, D(4), D(16), 0) };
                var sel = new RadioButton { Text = $"Only the {o.SelectedRowCount} selected row(s)", Checked = o.SelectedRowsOnly, AutoSize = true, Margin = new Padding(0, D(4), 0, 0) };
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
                var num = new NumericUpDown { Minimum = 0, Maximum = 40, Value = Math.Max(0, Math.Min(40, o.BlankRows)), Width = D(80), Margin = new Padding(0, D(2), 0, 0) };
                num.ValueChanged += (s, e) => { o.BlankRows = (int)num.Value; Schedule(); };
                var f = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(6)) };
                f.Controls.Add(new Label { Text = "Table rows (blank lines are added to reach this)", AutoSize = true, Margin = new Padding(0, D(6), D(8), 0), ForeColor = AppTheme.TextSecondary });
                f.Controls.Add(num);
                Add(f);
            }
            // Header
            Group("Company header");
            var logo = new CheckBox { Text = "Show the company logo", Checked = o.ShowLogo, AutoSize = true, Margin = new Padding(0, 0, 0, D(6)) };
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
                    var l = new LinkLabel { Text = t, AutoSize = true, LinkColor = AppTheme.AccentText, Margin = new Padding(0, 0, D(14), 0), LinkBehavior = LinkBehavior.HoverUnderline };
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
                foreach (Control c in sigHost.Controls) c.Width = RowWidth;
            }
            RebuildSigs();
            Add(sigHost);
            var addSig = new LinkLabel { Text = "＋ Add a signature line", AutoSize = true, LinkColor = AppTheme.AccentText, Margin = new Padding(0, D(4), 0, D(6)), LinkBehavior = LinkBehavior.HoverUnderline };
            addSig.LinkClicked += (s, e) => { o.Signatures.Add(new PrintSig("Signed by", "", "")); RebuildSigs(); Schedule(); };
            Add(addSig);
            // Notes
            Group("Notes & footer");
            Add(LabeledText("Note printed under the form", o.Notes, v => o.Notes = v, multiline: true));
            Add(LabeledText("Footer text", o.Footer, v => o.Footer = v, multiline: true));
            stack.ResumeLayout(true);
            FitRows();
        }
        private void Add(Control c) { c.Width = RowWidth; stack.Controls.Add(c); }
        private void Group(string title, string hint = "")
        {
            var p = new Panel { Height = D(hint.Length > 0 ? 58 : 38), BackColor = Color.Transparent, Margin = new Padding(0, D(10), 0, D(2)) };
            var t = new Label { Text = title.ToUpperInvariant(), Dock = DockStyle.Top, Height = D(28), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.AccentText, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, D(30), p.Width, D(30)); };
            p.Controls.Add(t);
            if (hint.Length > 0) { var h = new Label { Text = hint, Dock = DockStyle.Bottom, Height = D(26), Font = new Font("Segoe UI", 8.5F), ForeColor = AppTheme.TextMuted, UseMnemonic = false }; p.Controls.Add(h); }
            stack.Controls.Add(p);
            p.Width = RowWidth;
        }
        private Label Caption(string text) => new Label { Text = text, AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 8.75F), Margin = new Padding(0, 0, 0, D(2)), UseMnemonic = false };
        private Control LabeledText(string caption, string value, Action<string> set, bool multiline = false)
        {
            var p = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(8)) };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var tb = new TextBox { Text = value, Dock = DockStyle.Fill, Multiline = multiline, Height = multiline ? D(52) : D(28), ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
            tb.TextChanged += (s, e) => { set(tb.Text); Schedule(); };
            p.Controls.Add(Caption(caption), 0, 0);
            p.Controls.Add(tb, 0, 1);
            return p;
        }
        private Control LabeledCombo(string caption, string[] items, string value, Action<string> set, bool editable = false)
        {
            var p = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(8)), Dock = DockStyle.Fill };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var cb = new ComboBox { DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
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
            a.Margin = new Padding(0, 0, D(8), D(8));
            b.Margin = new Padding(0, 0, 0, D(8));
            a.Dock = DockStyle.Fill; b.Dock = DockStyle.Fill;
            t.Controls.Add(a, 0, 0);
            t.Controls.Add(b, 1, 0);
            return t;
        }
        /// <summary>[include] [editable label] [value] — one per field.</summary>
        private Control FieldRow(PrintField f)
        {
            var t = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(4)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(26)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(138)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var chk = new CheckBox { Checked = f.On, AutoSize = true, Margin = new Padding(0, D(6), 0, 0) };
            var lbl = new TextBox { Text = f.Label, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9F), Margin = new Padding(0, D(6), D(6), 0) };
            var val = new TextBox { Text = f.Value, Dock = DockStyle.Fill, Multiline = f.Multiline, Height = f.Multiline ? D(48) : D(28), ScrollBars = f.Multiline ? ScrollBars.Vertical : ScrollBars.None, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0) };
            var tips = new ToolTip();
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
        private Control SigRow(PrintSig s, Action remove)
        {
            var t = new TableLayoutPanel { ColumnCount = 5, RowCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(6)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(26)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(26)));
            var chk = new CheckBox { Checked = s.On, AutoSize = true, Margin = new Padding(0, D(22), 0, 0) };
            TextBox Tb(string v, Action<string> set)
            {
                var tb = new TextBox { Text = v, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.InputBack, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, D(6), 0) };
                tb.TextChanged += (x, e) => { set(tb.Text); Schedule(); };
                return tb;
            }
            var cap = Tb(s.Caption, v => s.Caption = v);
            var name = Tb(s.Name, v => s.Name = v);
            var role = Tb(s.Role, v => s.Role = v);
            var del = new LinkLabel { Text = "✕", AutoSize = true, LinkColor = AppTheme.TextMuted, ActiveLinkColor = AppTheme.DangerText, Margin = new Padding(0, D(22), 0, 0), LinkBehavior = LinkBehavior.NeverUnderline };
            del.LinkClicked += (x, e) => remove();
            chk.CheckedChanged += (x, e) => { s.On = chk.Checked; Schedule(); };
            Label H(string text) => new Label { Text = text, AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 8F), Margin = new Padding(0, 0, 0, D(1)) };
            t.Controls.Add(H("Caption"), 1, 0);
            t.Controls.Add(H("Printed name"), 2, 0);
            t.Controls.Add(H("Role / position"), 3, 0);
            t.Controls.Add(chk, 0, 1);
            t.Controls.Add(cap, 1, 1);
            t.Controls.Add(name, 2, 1);
            t.Controls.Add(role, 3, 1);
            t.Controls.Add(del, 4, 1);
            t.SetRowSpan(chk, 1);
            return t;
        }
        private sealed class CheckGridPanel : TableLayoutPanel
        {
            public Action Sync = () => { };
        }
        private CheckGridPanel CheckGrid(List<PrintChoice> choices)
        {
            var t = new CheckGridPanel { ColumnCount = 2, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, D(6)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.RowCount = (choices.Count + 1) / 2;
            var boxes = new List<(CheckBox Box, PrintChoice Choice)>();
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                var cb = new CheckBox { Text = c.Label, Checked = c.On, AutoSize = true, Margin = new Padding(0, D(2), D(8), D(2)), UseMnemonic = false };
                cb.CheckedChanged += (s, e) => { c.On = cb.Checked; Schedule(); };
                boxes.Add((cb, c));
                t.Controls.Add(cb, i % 2, i / 2);
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
            try { web.DocumentText = PrintKit.Page(def, o, autoPrint: false, preview: true); } catch { }
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
            {
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
            {
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
            {
                MessageBox.Show("Unable to open the print window: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static void Show(Func<PrintFormDef> build)
        {
            PrintFormDef def;
            try { def = build(); }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't prepare this printout: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Show(def);
        }
    }
}
