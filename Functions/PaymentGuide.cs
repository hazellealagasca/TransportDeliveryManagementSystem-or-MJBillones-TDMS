using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One check on a Payments form (shown in the checklist above Save).</summary>
    public sealed record PaymentCheck(string Text, bool Passed, bool Warning = false);


    /// <summary>
    /// Slim strip at the top of a Payments form with the live totals: Gross · Deductions / advances · Net due ·
    /// Payment (Unpaid / Paid in full: payments are always in full) · Validation status. Updates as the form is filled in.
    /// </summary>
    public sealed class PaymentSummaryStrip : Control, IZoomAware
    {
        private decimal gross, deductions, paid;
        private decimal? explicitDue;   // when given, the third figure (total due) is this, not gross - deductions
        private int problems, warnings;
        private string status = "";
        /// <summary>Workbench header mode: no outer frame, sits inside the one 56 px header.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public bool Compact { get; set; }
        /// <summary>The "Payment: Unpaid" cell means nothing on a record that is not saved yet; the workbench hides it.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public bool ShowPayment { get; set; } = true;
        /// <summary>Caption of the third figure ("Net due"; "Amount due" on a receipt).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public string NetCaption { get; set; } = "Net due";
        /// <summary>Caption of the first figure ("Gross"; "Amount" on a service invoice).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public string FirstCaption { get; set; } = "Gross";
        /// <summary>Caption of the second figure (default "Deductions"; "Fuel surcharge" on a service invoice).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public string? SecondCaption { get; set; }
        /// <summary>True when the second figure is added to the first (a service invoice: amount + fuel) instead of taken off it.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public bool SecondIsAddition { get; set; }
        /// <summary>False on a service invoice: it shows only the figures typed on it, no total (the first computation is on the statement of account).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public bool ShowNet { get; set; } = true;
        public PaymentSummaryStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Dock = DockStyle.Top;
            Height = D(46);
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f * UiZoom.Factor);
        public void ZoomChanged() { Height = D(Compact ? 40 : 46); if (Compact && Dock == DockStyle.Right) Width = D(620); Invalidate(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Height = D(Compact ? 40 : 46); }
        protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); Invalidate(); }
        public void SetTotals(decimal gross, decimal deductions, decimal paid, decimal balance, int problems, int warnings = 0, string? status = null, decimal? due = null)
        {
            this.gross = gross; this.deductions = deductions; this.paid = paid; _ = balance; // payments are in full: no balance is shown
            this.problems = problems; this.warnings = warnings; this.status = status ?? ""; explicitDue = due;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(AppTheme.Canvas);
            var r = Compact ? new Rectangle(0, 0, Math.Max(D(100), Width) - 1, Height - 1)
                            : new Rectangle(Padding.Left, D(4), Math.Max(D(100), Width - Padding.Horizontal) - 1, Height - D(9));
            if (!Compact)
            {
                using (var b = new SolidBrush(AppTheme.Surface)) g.FillRectangle(b, r);
                using (var p = new Pen(AppTheme.Border)) g.DrawRectangle(p, r);
            }
            string Money(decimal v) => "₱" + v.ToString("N2");
            decimal net = explicitDue ?? (SecondIsAddition ? gross + deductions : gross - deductions);
            var parts = new (string Cap, string Val, Color Ink)[]
            {
                (FirstCaption, Money(gross), AppTheme.TextPrimary),
                (SecondCaption ?? (Compact ? "Deductions" : "Deductions / advances"), Money(deductions), AppTheme.TextPrimary),
                (NetCaption, Money(Math.Max(0m, net)), AppTheme.TextPrimary),
                ("Payment", net > 0 && paid >= net ? "Paid in full" : "Unpaid", net > 0 && paid >= net ? AppTheme.SuccessText : AppTheme.WarningText),
            };
            string vs = status.Length > 0 ? status : problems > 0 ? $"✗ {problems} to fix" : warnings > 0 ? $"⚠ {warnings} to review" : "✓ Ready to save";
            Color vc = problems > 0 ? AppTheme.DangerText : warnings > 0 ? AppTheme.WarningText : AppTheme.SuccessText;
            if (!ShowNet) parts = parts.Where((_, k) => k != 2).ToArray();
            if (!ShowPayment) parts = parts.Take(ShowNet ? 3 : 2).ToArray();
            int n = parts.Length + 1;
            int w = r.Width / n;
            using var cf = new Font("Segoe UI", 8.25F * UiZoom.Factor);
            using var vf = new Font("Segoe UI Semibold", 10.5F * UiZoom.Factor);
            for (int i = 0; i < n; i++)
            {
                int x = r.Left + i * w;
                string cap = i < parts.Length ? parts[i].Cap : (Compact ? "Checks" : "Validation");
                string val = i < parts.Length ? parts[i].Val : vs;
                Color ink = i < parts.Length ? parts[i].Ink : vc;
                if (i > 0) using (var p = new Pen(AppTheme.Border)) g.DrawLine(p, x, r.Top + D(6), x, r.Bottom - D(6));
                var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(g, cap, cf, new Rectangle(x + D(12), r.Top + D(2), w - D(16), r.Height / 2), AppTheme.TextMuted, flags);
                TextRenderer.DrawText(g, val, vf, new Rectangle(x + D(12), r.Top + r.Height / 2 - D(3), w - D(16), r.Height / 2), ink, flags);
            }
        }
    }


    /// <summary>
    /// Compact workflow progress: Dispatch created → Trip report completed → Forms filled → Review → Validated &amp; ready.
    /// Each step is done (✓), current (●) or still to do (○); a short note under the row says what is missing.
    /// </summary>
    public sealed class WorkflowStepper : Control
    {
        private readonly string[] steps;
        private int reached = -1;     // index of the last finished step
        private string note = "";
        public WorkflowStepper(params string[] stepNames)
        {
            steps = stepNames.Length > 0 ? stepNames : new[] { "Dispatch created", "Trip report completed", "Forms filled", "Review & complete", "Validated & ready" };
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Dock = DockStyle.Top;
            Height = D(54);
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Height = D(54); }
        /// <param name="doneThrough">Index of the last finished step (−1: none).</param>
        public void SetProgress(int doneThrough, string whatNext)
        {
            reached = Math.Max(-1, Math.Min(steps.Length - 1, doneThrough));
            note = whatNext ?? "";
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(AppTheme.Canvas);
            int n = steps.Length;
            int w = Width / n;
            int cy = D(14), rad = D(9);
            using var sf = new Font("Segoe UI Semibold", 8.5F);
            using var nf = new Font("Segoe UI", 8.5F);
            for (int i = 0; i < n; i++)
            {
                int cx = i * w + D(14);
                bool done = i <= reached, current = i == reached + 1;
                Color c = done ? AppTheme.SuccessText : current ? AppTheme.Accent : AppTheme.TextMuted;
                if (i < n - 1)
                {
                    // the connector starts after the label so it never runs through the text
                    int labelW = Math.Min(w - rad * 2 - D(8), TextRenderer.MeasureText(steps[i], current ? sf : nf).Width);
                    int x1 = cx + rad + D(4) + labelW + D(6), x2 = (i + 1) * w + D(14) - rad - D(4);
                    if (x2 > x1)
                        using (var p = new Pen(i < reached + 1 ? AppTheme.SuccessText : AppTheme.Border, D(2))) g.DrawLine(p, x1, cy, x2, cy);
                }
                using (var b = new SolidBrush(done ? c : AppTheme.Surface)) g.FillEllipse(b, cx - rad, cy - rad, rad * 2, rad * 2);
                using (var p = new Pen(c, D(2))) g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                string mark = done ? "✓" : (i + 1).ToString();
                TextRenderer.DrawText(g, mark, sf, new Rectangle(cx - rad, cy - rad, rad * 2, rad * 2), done ? AppTheme.BestTextOn(c) : c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, steps[i], current ? sf : nf, new Rectangle(cx + rad + D(4), cy - D(9), w - rad * 2 - D(8), D(18)), done || current ? AppTheme.TextPrimary : AppTheme.TextMuted, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            }
            if (note.Length > 0)
                TextRenderer.DrawText(g, note, nf, new Rectangle(D(4), D(30), Width - D(8), D(20)), reached >= n - 1 ? AppTheme.SuccessText : AppTheme.TextSecondary, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        }
    }


    /// <summary>The live checklist above Save: ✓ passed (green), ⚠ to review (amber), ✗ to fix (red).</summary>
    public sealed class ValidationChecklist : Control, IZoomAware
    {
        private List<PaymentCheck> checks = new List<PaymentCheck>();
        /// <summary>Workbench mode: only the failing (✗) and to-review (⚠) lines are drawn; passing rules collapse into one summary line.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public bool ShowOnlyProblems { get; set; }
        /// <summary>Maps a line to the field it is about, so clicking the line jumps to that field (set by the screen; null = not clickable).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)] public Func<PaymentCheck, Control?>? FocusResolver { get; set; }
        private IReadOnlyList<PaymentCheck> Shown => ShowOnlyProblems ? checks.Where(c => !c.Passed || c.Warning).ToList() : checks;
        public ValidationChecklist()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Dock = DockStyle.Top;
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f * UiZoom.Factor);
        public void ZoomChanged() { if (checks.Count > 0) SetChecks(checks); Invalidate(); }
        public int Failed => checks.Count(c => !c.Passed);
        public int Warnings => checks.Count(c => c.Passed && c.Warning);
        public bool AllPassed => checks.All(c => c.Passed);
        public IReadOnlyList<PaymentCheck> Checks => checks;
        public void SetChecks(IEnumerable<PaymentCheck> list)
        {
            checks = list.ToList();
            int cols = ShowOnlyProblems ? 1 : Width > D(700) ? 2 : 1;
            int rows = (Shown.Count + cols - 1) / cols;
            int want = D(30) + rows * D(22) + D(8);
            if (Height != want) Height = want;
            Invalidate();
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (checks.Count > 0) SetChecks(checks); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (FocusResolver == null || e.Y < D(28)) return;
            int cols = ShowOnlyProblems ? 1 : Width > D(700) ? 2 : 1;
            int cw = (Width - D(20)) / cols;
            int col = Math.Min(cols - 1, Math.Max(0, (e.X - D(10)) / Math.Max(1, cw))), row = (e.Y - D(28)) / D(22);
            int i = row * cols + col;
            var shown = Shown;
            if (i < 0 || i >= shown.Count || shown[i].Passed && !shown[i].Warning) return;
            var target = FocusResolver(shown[i]);
            if (target == null || target.IsDisposed || !target.Visible) return;
            try { target.Focus(); (target as TextBoxBase)?.SelectAll(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = FocusResolver != null && e.Y >= D(28) && Failed + Warnings > 0 ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(AppTheme.Surface);
            using (var p = new Pen(AppTheme.Border)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            using var hf = new Font("Segoe UI Semibold", 9.5F * UiZoom.Factor);
            using var f = new Font("Segoe UI", 9F * UiZoom.Factor);
            int failed = Failed;
            string head = failed == 0 ? $"✓ All {checks.Count} checks passed — ready to save" : ShowOnlyProblems ? $"Needs attention · {failed} to fix  ({checks.Count - failed} of {checks.Count} checks pass)" : $"Validation: {checks.Count - failed} of {checks.Count} passed · {failed} to fix before saving";
            TextRenderer.DrawText(g, head, hf, new Rectangle(D(10), D(4), Width - D(20), D(22)), failed == 0 ? AppTheme.SuccessText : AppTheme.DangerText, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            int cols = ShowOnlyProblems ? 1 : Width > D(700) ? 2 : 1;
            int cw = (Width - D(20)) / cols;
            var shown = Shown;
            for (int i = 0; i < shown.Count; i++)
            {
                var c = shown[i];
                int col = i % cols, row = i / cols;
                string mark = !c.Passed ? "✗" : c.Warning ? "⚠" : "✓";
                Color ink = !c.Passed ? AppTheme.DangerText : c.Warning ? AppTheme.WarningText : AppTheme.SuccessText;
                var rect = new Rectangle(D(10) + col * cw, D(28) + row * D(22), cw - D(8), D(22));
                TextRenderer.DrawText(g, $"{mark}  {c.Text}", f, rect, ink, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            }
        }
    }


    /// <summary>"❓ How this works" toggle with a panel of the rules (closed by default; remembers nothing).</summary>
    public sealed class HelpDrawer : Panel
    {
        private readonly Button toggle;
        private readonly Label body;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public HelpDrawer(string title, string text)
        {
            Dock = DockStyle.Top;
            BackColor = AppTheme.Canvas;
            toggle = new Button
            {
                Text = "❓  " + title + "  ▾",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F),
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Location = new Point(0, D(2))
            };
            UIThemeHelper.SetButtonRole(toggle, "secondary");
            body = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextPrimary,
                BackColor = AppTheme.Surface,
                Padding = new Padding(D(12), D(10), D(12), D(10)),
                UseMnemonic = false,
                Visible = false,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(toggle);
            Controls.Add(body);
            toggle.Click += (s, e) => { body.Visible = !body.Visible; toggle.Text = "❓  " + title + (body.Visible ? "  ▴" : "  ▾"); Arrange(); };
            Arrange();
        }
        protected override void OnResize(EventArgs eventargs) { base.OnResize(eventargs); Arrange(); }
        private void Arrange()
        {
            if (toggle == null || body == null) return;
            int y = toggle.Bottom + D(6);
            if (body.Visible)
            {
                int h = TextRenderer.MeasureText(body.Text, body.Font, new Size(Math.Max(D(200), Width - D(26)), 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + D(24);
                body.Bounds = new Rectangle(0, y, Width, h);
                y += h + D(6);
            }
            if (Height != y) Height = y;
        }
    }


    /// <summary>
    /// Small popover under a field: why the value can't be used, with clickable chips of the values that can.
    /// </summary>
    public sealed class LinkedChoicesPopover : Form
    {
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private LinkedChoicesPopover(string message, IReadOnlyList<string> choices, Action<string> pick)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.BorderStrong;
            Padding = new Padding(1);
            KeyPreview = true;
            var inner = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true, BackColor = AppTheme.Surface, Padding = new Padding(D(10)) };
            var lbl = new Label { Text = "⚠  " + message, AutoSize = true, MaximumSize = new Size(D(460), 0), Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.WarningText, Margin = new Padding(0, 0, 0, D(8)), UseMnemonic = false };
            inner.Controls.Add(lbl);
            inner.SetFlowBreak(lbl, true);
            foreach (var c in choices.Take(12))
            {
                var b = new Button { Text = c, AutoSize = true, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(6), D(6)), UseMnemonic = false };
                UIThemeHelper.SetButtonRole(b, "secondary");
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = AppTheme.Accent;
                b.Padding = new Padding(D(8), D(2), D(8), D(2));
                b.Text = "▸ " + c;
                string v = c;
                b.Click += (s, e) => { Close(); pick(v); };
                inner.Controls.Add(b);
            }
            if (choices.Count == 0) inner.Controls.Add(new Label { Text = "No connected records are ready yet.", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F) });
            Controls.Add(inner);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Deactivate += (s, e) => Close();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }
        /// <summary>Shows the popover under <paramref name="anchor"/>; it closes when a chip is clicked or focus leaves it.</summary>
        public static void ShowUnder(Control anchor, string message, IReadOnlyList<string> choices, Action<string> pick)
        {
            var owner = anchor.FindForm();
            if (owner == null) return;
            var pop = new LinkedChoicesPopover(message, choices, pick);
            var pt = anchor.PointToScreen(new Point(0, anchor.Height + 2));
            pop.Location = pt;
            pop.Show(owner);
            var wa = Screen.FromControl(anchor).WorkingArea;
            if (pop.Right > wa.Right) pop.Left = Math.Max(wa.Left, wa.Right - pop.Width);
            if (pop.Bottom > wa.Bottom) pop.Top = Math.Max(wa.Top, anchor.PointToScreen(Point.Empty).Y - pop.Height - 2);
        }
    }
    public enum BadgeKind { Neutral, Good, Warn, Bad, Info }
    /// <summary>One suggestion in a <see cref="RichLookup"/> list: the value, a short detail line and a coloured badge.</summary>
    public sealed record LookupItem(string Value, string Detail = "", string Badge = "", BadgeKind Kind = BadgeKind.Neutral, bool Enabled = true);


    /// <summary>
    /// Typeahead for a text box or combo box: as you type, a list of matching records (company, dispatch, trip report,
    /// invoice, receipt …) appears under the field with a badge saying whether each can be used ("Ready", "Billed",
    /// "Paid" …). Up/Down + Enter or a click picks one; records that can't be used are shown greyed and can't be picked.
    /// </summary>
    public sealed class RichLookup : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x00000080, WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 3;
        private const int MaxRows = 8;
        private readonly Control field;
        private readonly Func<string, IEnumerable<LookupItem>> source;
        private readonly Action<LookupItem> picked;
        private readonly RowsView view;
        private List<LookupItem> items = new List<LookupItem>();
        private int selected = -1, extra;
        private bool suppress;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => D(34);
        /// <summary>Set false to stop suggestions (e.g. while the form fills itself in).</summary>
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Active { get; set; } = true;
        private RichLookup(Control field, Func<string, IEnumerable<LookupItem>> source, Action<LookupItem> picked)
        {
            this.field = field; this.source = source; this.picked = picked;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.BorderStrong;
            Padding = new Padding(1);
            view = new RowsView(this) { Dock = DockStyle.Fill };
            Controls.Add(view);
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW; return cp; } }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
            base.WndProc(ref m);
        }
        /// <summary>Attaches suggestions to <paramref name="field"/> (TextBox or ComboBox).</summary>
        public static RichLookup Attach(Control field, Func<string, IEnumerable<LookupItem>> source, Action<LookupItem> picked)
        {
            var l = new RichLookup(field, source, picked);
            field.TextChanged += (s, e) => l.OnTyped();
            field.KeyDown += (s, e) => l.OnKey(e);
            field.Leave += (s, e) => l.HideList();
            field.Disposed += (s, e) => { try { l.Close(); l.Dispose(); } catch { } };
            if (field is ComboBox cb) cb.DropDown += (s, e) => l.HideList();
            field.ParentChanged += (s, e) => l.HideList();
            return l;
        }
        /// <summary>Changes the field's text without opening the list.</summary>
        public void SetTextQuietly(string text)
        {
            suppress = true;
            try { field.Text = text; if (field is TextBox tb) tb.SelectionStart = tb.TextLength; }
            finally { suppress = false; }
            HideList();
        }
        private void OnTyped()
        {
            if (suppress || !Active || !field.Focused && !(field is ComboBox c && c.ContainsFocus)) return;
            if (field is ComboBox cb && cb.DroppedDown) return;
            string q = field.Text.Trim();
            if (q.Length == 0) { HideList(); return; }
            List<LookupItem> all;
            try { all = source(q).ToList(); } catch (Exception ex) { ErrLog.Swallowed(ex); all = new List<LookupItem>(); }
            if (all.Count == 0) { HideList(); return; }
            items = all.Take(MaxRows).ToList();
            extra = Math.Max(0, all.Count - items.Count);
            selected = items.FindIndex(i => i.Enabled);
            var owner = field.FindForm();
            if (owner == null) return;
            var r = field.RectangleToScreen(field.ClientRectangle);
            int rows = items.Count + (extra > 0 ? 1 : 0);
            var size = new Size(Math.Max(D(420), r.Width), rows * RowH + 2);
            var wa = Screen.FromRectangle(r).WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(r.Left, wa.Right - size.Width));
            int y = r.Bottom + 1;
            if (y + size.Height > wa.Bottom) y = Math.Max(wa.Top, r.Top - size.Height - 1);
            Bounds = new Rectangle(new Point(x, y), size);
            if (Owner != owner) { if (Visible) Hide(); Owner = owner; }
            if (!Visible) Show(owner);
            view.Invalidate();
        }
        private void OnKey(KeyEventArgs e)
        {
            if (!Visible) return;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                int d = e.KeyCode == Keys.Down ? 1 : -1;
                for (int n = 0; n < items.Count; n++)
                {
                    selected = (selected + d + items.Count) % items.Count;
                    if (items[selected].Enabled) break;
                }
                view.Invalidate();
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab && selected >= 0)
            {
                if (selected >= 0 && selected < items.Count && items[selected].Enabled) { Pick(items[selected]); if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; } }
            }
            else if (e.KeyCode == Keys.Escape) { HideList(); e.Handled = true; e.SuppressKeyPress = true; }
        }
        private void Pick(LookupItem it)
        {
            HideList();
            SetTextQuietly(it.Value);
            picked(it);
        }
        public void HideList() { if (Visible) Hide(); }
        private static (Color Back, Color Fore) BadgeColors(BadgeKind k) => k switch
        {
            BadgeKind.Good => (AppTheme.SuccessSoft, AppTheme.SuccessText),
            BadgeKind.Warn => (AppTheme.WarningSoft, AppTheme.WarningText),
            BadgeKind.Bad => (AppTheme.DangerSoft, AppTheme.DangerText),
            BadgeKind.Info => (AppTheme.AccentSoft, AppTheme.AccentText),
            _ => (AppTheme.SurfaceAlt, AppTheme.TextSecondary)
        };
        private sealed class RowsView : Control
        {
            private readonly RichLookup p;
            public RowsView(RichLookup popup)
            {
                p = popup;
                SetStyle(ControlStyles.Selectable, false);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }
            private int Hit(Point pt) { int i = pt.Y / p.RowH; return i >= 0 && i < p.items.Count ? i : -1; }
            protected override void OnMouseMove(MouseEventArgs e) { int i = Hit(e.Location); if (i >= 0 && i != p.selected && p.items[i].Enabled) { p.selected = i; Invalidate(); } base.OnMouseMove(e); }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;
                int i = Hit(e.Location);
                if (i >= 0 && p.items[i].Enabled) p.Pick(p.items[i]);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(AppTheme.Surface);
                int h = p.RowH, w = Width, pad = p.D(10);
                using var bold = new Font("Segoe UI Semibold", 9.5F);
                using var small = new Font("Segoe UI", 8.5F);
                using var badgeFont = new Font("Segoe UI Semibold", 8F);
                var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
                for (int i = 0; i < p.items.Count; i++)
                {
                    var it = p.items[i];
                    int y = i * h;
                    if (i == p.selected && it.Enabled) using (var sb = new SolidBrush(AppTheme.AccentSoftStrong)) g.FillRectangle(sb, 0, y, w, h);
                    int badgeW = 0;
                    if (it.Badge.Length > 0)
                    {
                        var (bk, fk) = BadgeColors(it.Kind);
                        badgeW = TextRenderer.MeasureText(it.Badge, badgeFont).Width + p.D(10);
                        var br = new Rectangle(w - pad - badgeW, y + (h - p.D(20)) / 2, badgeW, p.D(20));
                        using (var b = new SolidBrush(bk)) g.FillRectangle(b, br);
                        TextRenderer.DrawText(g, it.Badge, badgeFont, br, fk, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                        badgeW += p.D(8);
                    }
                    Color ink = it.Enabled ? AppTheme.TextPrimary : AppTheme.TextMuted;
                    int tw = w - pad * 2 - badgeW;
                    if (it.Detail.Length > 0)
                    {
                        TextRenderer.DrawText(g, it.Value, bold, new Rectangle(pad, y + p.D(1), tw, h / 2), ink, flags);
                        TextRenderer.DrawText(g, it.Detail, small, new Rectangle(pad, y + h / 2 - p.D(2), tw, h / 2), AppTheme.TextMuted, flags);
                    }
                    else TextRenderer.DrawText(g, it.Value, bold, new Rectangle(pad, y, tw, h), ink, flags);
                    using var lp = new Pen(AppTheme.Border);
                    g.DrawLine(lp, 0, y + h - 1, w, y + h - 1);
                }
                if (p.extra > 0)
                    TextRenderer.DrawText(g, $"+{p.extra} more — keep typing to narrow the list", small, new Rectangle(pad, p.items.Count * h, w - pad * 2, h), AppTheme.TextMuted, flags);
            }
        }
    }


    /// <summary>Field colouring used on the Payments forms: green = checked, amber = review, red = must fix.</summary>
    public enum FieldState { None, Ok, Warn, Bad }
    public static class FieldMarks
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, StrongBox> original = new();
        private sealed class StrongBox { public Color Back; public Color Tint; }
        private static bool hooked;
        /// <summary>After a theme switch the remembered colours belong to the old theme: put tinted fields back on the new input colour and forget them, so the next Mark captures fresh originals.</summary>
        private static void Hook()
        {
            if (hooked) return;
            hooked = true;
            ThemeManager.ThemeChanged += (s, e) =>
            {
                var stale = new List<KeyValuePair<Control, StrongBox>>();
                foreach (var kv in original) stale.Add(kv);
                foreach (var kv in stale)
                {
                    var c = kv.Key;
                    if (c != null && !c.IsDisposed && c.BackColor == kv.Value.Tint) c.BackColor = AppTheme.InputBack;
                    original.Remove(c!);
                }
            };
        }
        /// <summary>Tints the field's background by its state (keeps its own colour for <see cref="FieldState.None"/>).</summary>
        public static void Mark(Control c, FieldState state)
        {
            if (c == null || c.IsDisposed) return;
            Hook();
            if (!original.TryGetValue(c, out var box)) { box = new StrongBox { Back = c.BackColor }; original.Add(c, box); }
            Color want = state switch
            {
                FieldState.Ok => AppTheme.SuccessSoft,
                FieldState.Warn => AppTheme.WarningSoft,
                FieldState.Bad => AppTheme.DangerSoft,
                _ => box.Back
            };
            box.Tint = want;
            if (c.BackColor != want) c.BackColor = want;
        }
        /// <summary>Forgets the remembered colour (call after a theme change, before marking again).</summary>
        public static void Reset(Control c) { if (original.TryGetValue(c, out var box)) { c.BackColor = box.Back; original.Remove(c); } }
    }


    /// <summary>
    /// Keystroke and paste guard for money boxes: digits and one decimal point, at most 2 decimals, no minus sign.
    /// </summary>
    public static class InputGuard
    {
        public static void MoneyOnly(TextBox tb)
        {
            tb.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.') { e.Handled = true; System.Media.SystemSounds.Beep.Play(); return; }
                string next = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.KeyChar.ToString());
                if (!IsMoney(next)) { e.Handled = true; System.Media.SystemSounds.Beep.Play(); }
            };
            tb.TextChanged += (s, e) =>
            {
                // A paste that isn't a valid amount is taken out again.
                string t = tb.Text.Replace(",", "").Replace("₱", "").Trim();
                if (t != tb.Text && IsMoney(t)) { int c = tb.SelectionStart; tb.Text = t; tb.SelectionStart = Math.Min(c, t.Length); return; }
                if (!IsMoney(tb.Text)) { tb.Text = new string(tb.Text.Where(ch => char.IsDigit(ch) || ch == '.').ToArray()); if (!IsMoney(tb.Text)) tb.Text = ""; }
            };
        }
        public static bool IsMoney(string s)
        {
            if (s.Length == 0) return true;
            int dot = s.IndexOf('.');
            if (dot != s.LastIndexOf('.')) return false;
            if (s.Any(ch => !char.IsDigit(ch) && ch != '.')) return false;
            return dot < 0 || s.Length - dot - 1 <= 2;
        }
        /// <summary>No minus sign and at most 2 decimals typed into a NumericUpDown.</summary>
        public static void MoneyOnly(NumericUpDown n)
        {
            n.Minimum = Math.Max(0, n.Minimum);
            n.DecimalPlaces = 2;
            n.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (e.KeyChar == '-' ) { e.Handled = true; System.Media.SystemSounds.Beep.Play(); return; }
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',') { e.Handled = true; System.Media.SystemSounds.Beep.Play(); return; }
                // Inside the box's own text field: refuse a 3rd decimal.
                var inner = n.Controls.OfType<TextBox>().FirstOrDefault();
                if (inner == null || !char.IsDigit(e.KeyChar)) return;
                string next = inner.Text.Remove(inner.SelectionStart, inner.SelectionLength).Insert(inner.SelectionStart, e.KeyChar.ToString()).Replace(",", "");
                int dot = next.IndexOf('.');
                if (dot >= 0 && next.Length - dot - 1 > 2) { e.Handled = true; System.Media.SystemSounds.Beep.Play(); }
            };
        }
        /// <summary>Nov 12: a payment reference takes letters, digits and "-" only (the other symbols are not typed).</summary>
        public static void ReferenceDashOnly(TextBox tb, int maxLength = 30)
        {
            tb.MaxLength = maxLength;
            static bool Ok(char ch) => (ch < 128 && char.IsLetterOrDigit(ch)) || ch == '-';
            tb.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !Ok(e.KeyChar)) { e.Handled = true; System.Media.SystemSounds.Beep.Play(); } };
            tb.TextChanged += (s, e) =>
            {
                if (tb.Text.All(Ok)) return;
                int c = tb.SelectionStart;
                tb.Text = new string(tb.Text.Where(Ok).ToArray());
                tb.SelectionStart = Math.Min(c, tb.TextLength);
            };
        }
        /// <summary>Reference / receipt / invoice numbers: letters, digits, dash, slash, dot, space — no other symbols.</summary>
        public static void ReferenceOnly(TextBox tb, int maxLength = 40)
        {
            tb.MaxLength = maxLength;
            static bool Ok(char ch) => char.IsLetterOrDigit(ch) || ch is '-' or '/' or '.' or ' ' or '#' or '_';
            tb.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !Ok(e.KeyChar)) { e.Handled = true; System.Media.SystemSounds.Beep.Play(); } };
            tb.TextChanged += (s, e) =>
            {
                if (tb.Text.All(Ok)) return;
                int c = tb.SelectionStart;
                tb.Text = new string(tb.Text.Where(Ok).ToArray());
                tb.SelectionStart = Math.Min(c, tb.TextLength);
            };
        }
    }
    /// <summary>
    /// Slim "ready" line above a Payments list: a lead text and one button per record that can be processed next
    /// (one click opens a filled form; nothing is saved until Save).
    /// </summary>
    public sealed class ReadyBanner : FlowLayoutPanel
    {
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public ReadyBanner()
        {
            Dock = DockStyle.Top;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = true;
            BackColor = Color.Transparent;
            Padding = new Padding(0, D(2), 0, D(6));
        }
        public void SetItems(string lead, string emptyText, IReadOnlyList<(string Text, string Tip, Action Click)> items, int maxShown = 6)
        {
            SuspendLayout();
            try
            {
                foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }
                Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.25F),
                    ForeColor = items.Count > 0 ? AppTheme.SuccessText : AppTheme.TextMuted,
                    Margin = new Padding(0, D(7), D(8), 0),
                    UseMnemonic = false,
                    MaximumSize = new Size(Math.Max(D(300), Width - D(10)), 0),
                    Text = items.Count > 0 ? "● " + lead : emptyText
                });
                var tip = new ToolTip();
                foreach (var it in items.Take(maxShown))
                {
                    var b = new Button
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI Semibold", 9F),
                        Cursor = Cursors.Hand,
                        UseMnemonic = false,
                        Padding = new Padding(D(6), 0, D(6), 0),
                        MinimumSize = new Size(0, D(30)),
                        Margin = new Padding(0, D(2), D(6), D(2)),
                        Text = it.Text
                    };
                    UIThemeHelper.SetButtonRole(b, "secondary");
                    var click = it.Click;
                    b.Click += (s, e) => click();
                    if (it.Tip.Length > 0) tip.SetToolTip(b, it.Tip);
                    Controls.Add(b);
                }
                if (items.Count > maxShown)
                    Controls.Add(new Label { AutoSize = true, Text = $"+{items.Count - maxShown} more", ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(8), 0, 0), Font = new Font("Segoe UI", 9F) });
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            finally { ResumeLayout(true); }
        }
    }
}
