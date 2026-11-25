using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Owner-drawn step indicator: badge + label per step (active = accent, done = green tick, upcoming = soft).
    /// Colours come from <see cref="AppTheme"/> only; repaints on theme change.
    /// </summary>
    public sealed class WizardStepHeader : Control
    {
        private readonly string[] names;
        private readonly Func<int> current;
        private readonly Func<int, bool> canJump;
        private readonly Action<int> jump;
        private readonly EventHandler themed;

        public WizardStepHeader(string[] names, Func<int> current, Func<int, bool> canJump, Action<int> jump)
        {
            this.names = names; this.current = current; this.canJump = canJump; this.jump = jump;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            TabStop = false;
            themed = (s, e) => { try { if (!IsDisposed) Invalidate(); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            ThemeManager.ThemeChanged += themed;
            Disposed += (s, e) => ThemeManager.ThemeChanged -= themed;
        }

        private float S => DeviceDpi / 96f;

        private Rectangle Cell(int i)
        {
            int w = Math.Max(1, Width / names.Length);
            return new Rectangle(i * w, 0, w, Height);
        }

        private int HitTest(Point p)
        {
            for (int i = 0; i < names.Length; i++) if (Cell(i).Contains(p)) return i + 1;
            return 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            Cursor = h > 0 && canJump(h) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int h = HitTest(e.Location);
            if (h > 0 && canJump(h)) jump(h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(AppTheme.Surface);
            Gfx.HighQuality(g);
            using (var pen = new Pen(AppTheme.Border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            int cur = current();
            float zf = UiZoom.Factor; // Nov 26: the step header follows the display size (fonts and badges)
            int d = (int)(30 * S * zf);
            var fl = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            for (int i = 0; i < names.Length; i++)
            {
                int n = i + 1;
                var cell = Cell(i);
                bool active = n == cur, done = n < cur;
                var badge = new Rectangle(cell.X + (int)(18 * S), (Height - d) / 2, d, d);
                Color fill = active ? AppTheme.Accent : done ? AppTheme.Success : AppTheme.SurfaceAlt;
                Color ink = active ? AppTheme.OnAccent : done ? Color.White : AppTheme.TextMuted;
                using (var b = new SolidBrush(fill)) g.FillEllipse(b, badge);
                if (!active && !done) using (var pen = new Pen(AppTheme.Border)) g.DrawEllipse(pen, badge);
                using (var nf = new Font("Segoe UI Semibold", 10F * zf, FontStyle.Bold))
                    TextRenderer.DrawText(g, done ? "✓" : n.ToString(), nf, badge, ink, fl);
                var lbl = new Rectangle(badge.Right + (int)(10 * S), 0, cell.Right - badge.Right - (int)(14 * S), Height);
                using (var lf = new Font("Segoe UI Semibold", 10F * zf, active ? FontStyle.Bold : FontStyle.Regular))
                    TextRenderer.DrawText(g, names[i], lf, lbl, active ? AppTheme.TextPrimary : done ? AppTheme.TextSecondary : AppTheme.TextMuted,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }
    }

    /// <summary>
    /// Shared step-by-step form machinery (header, one visible step panel at a time, Back / Next, first-step and last-step buttons).
    /// Steps are 1-based. A new record can only go back to finished steps; a saved record (edit mode) can jump to any step.
    /// The form keeps its own validation (per-step callback) and its own save buttons.
    /// </summary>
    public sealed class FormWizard
    {
        private readonly Control dpiOwner;
        private readonly string[] stepNames;
        private readonly string?[] nextTexts;
        private readonly Func<bool> isEditMode;
        private readonly Func<int, bool> validateStep;
        private readonly List<(Button Button, Func<bool> Wanted)> lastStepButtons = new();
        private Panel[]? panels;
        private WizardStepHeader? header;

        /// <param name="dpiOwner">Control whose DeviceDpi scales the sizes.</param>
        /// <param name="stepNames">One name per step (2 or more).</param>
        /// <param name="isEditMode">True when a saved record is open (every step clickable).</param>
        /// <param name="validateStep">Called with the current step number when Next is pressed; false blocks the move.</param>
        public FormWizard(Control dpiOwner, string[] stepNames, Func<bool> isEditMode, Func<int, bool> validateStep)
        {
            if (stepNames.Length < 2) throw new ArgumentException("A wizard needs at least 2 steps.", nameof(stepNames));
            this.dpiOwner = dpiOwner; this.stepNames = stepNames; this.isEditMode = isEditMode; this.validateStep = validateStep;
            nextTexts = new string?[stepNames.Length];
            BackButton = new Button { Name = "btnBack", Text = "←  Back", Width = Dpi(130), Visible = false, TabStop = true };
            NextButton = new Button { Name = "btnNext", Text = NextTextFor(1), Width = Dpi(230), Visible = true };
            UIThemeHelper.SetButtonRole(BackButton, "secondary");
            UIThemeHelper.SetButtonRole(NextButton, "primary");
            BackButton.Click += (s, e) => { if (CurrentStep > 1) GoToStep(CurrentStep - 1); };
            NextButton.Click += (s, e) => TryNextStep();
        }

        private int Dpi(int v) => (int)Math.Round(v * dpiOwner.DeviceDpi / 96f);

        public int CurrentStep { get; private set; } = 1;
        public int StepCount => stepNames.Length;
        public bool IsEditMode => isEditMode();
        public Button BackButton { get; }
        public Button NextButton { get; }

        /// <summary>Button shown only on step 1 (e.g. Reset form); Back takes its place on steps 2+.</summary>
        public Button? FirstStepButton { get; set; }

        /// <summary>Panel whose scroll position returns to the top on every step change.</summary>
        public ScrollableControl? ScrollHost { get; set; }

        /// <summary>Called after the step changed, with the new step number.</summary>
        public Action<int>? StepChanged { get; set; }

        /// <summary>The step indicator; created on first use (Dock Top, 64 px).</summary>
        public WizardStepHeader Header => header ?? BuildHeader();

        /// <summary>Creates (or recreates) the step indicator.</summary>
        public WizardStepHeader BuildHeader()
        {
            var old = header;
            var st = new WizardStepHeader(stepNames, () => CurrentStep, CanJump, n => GoToStep(n)) { Name = "pnlStepperHeader", Dock = DockStyle.Top };
            st.Height = Dpi(64);
            st.Margin = new Padding(0);
            header = st;
            if (old != null) { old.Parent?.Controls.Remove(old); old.Dispose(); }
            return st;
        }

        /// <summary>The step panels, one per step, in order. Only the current one is visible.</summary>
        public void SetPanels(params Panel[] stepPanels)
        {
            if (stepPanels.Length != stepNames.Length) throw new ArgumentException("One panel per step is required.", nameof(stepPanels));
            panels = stepPanels;
        }

        /// <summary>A form button (Save / Update / Reschedule ...) shown only on the last step, and only while <paramref name="wanted"/> says the current mode needs it.</summary>
        public void AddLastStepButton(Button button, Func<bool> wanted) => lastStepButtons.Add((button, wanted));

        /// <summary>Overrides the Next button text on <paramref name="step"/> (default: "&lt;next step name&gt;  →"). Null restores the default.</summary>
        public void SetNextText(int step, string? text)
        {
            if (step >= 1 && step < StepCount) nextTexts[step - 1] = text;
            ApplyButtons();
        }

        private string NextTextFor(int step) => nextTexts[step - 1] ?? stepNames[step] + "  →";

        /// <summary>True when the header (or code acting like it) may jump to <paramref name="step"/>.</summary>
        public bool CanJump(int step) => step != CurrentStep && (IsEditMode || step < CurrentStep);

        /// <summary>Shows a step (1..StepCount) with no validation (used by the header in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step)
        {
            CurrentStep = Math.Max(1, Math.Min(StepCount, step));
            ApplyStep();
        }

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep()
        {
            if (CurrentStep >= StepCount) return true;
            if (!validateStep(CurrentStep)) return false;
            GoToStep(CurrentStep + 1);
            return true;
        }

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        public void ShowStepFor(Control? c)
        {
            if (panels == null) return;
            for (var p = c; p != null; p = p.Parent)
            {
                int i = Array.IndexOf(panels, p as Panel);
                if (i >= 0) { if (CurrentStep != i + 1) GoToStep(i + 1); return; }
            }
        }

        /// <summary>ShowStepFor, then focus.</summary>
        public void Focus(Control c)
        {
            ShowStepFor(c);
            c.Focus();
        }

        /// <summary>Re-applies visibility of the navigation and last-step buttons (call after the form changed which save buttons it wants).</summary>
        public void Refresh() => ApplyButtons();

        /// <summary>Puts Back and the first-step button at the left of <paramref name="actionBar"/> (Back first, then the first-step button).</summary>
        public void BuildLeftActions(Control actionBar)
        {
            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = AppTheme.Surface,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            if (FirstStepButton != null) FirstStepButton.Margin = new Padding(0, 0, Dpi(10), 0);
            BackButton.Margin = new Padding(0, 0, Dpi(10), 0);
            left.Controls.Add(BackButton);
            if (FirstStepButton != null) left.Controls.Add(FirstStepButton);
            actionBar.Controls.Add(left);
        }

        private void ApplyStep()
        {
            if (panels == null || panels.Any(p => p == null)) return;
            var host = ScrollHost;
            host?.SuspendLayout();
            for (int i = 0; i < panels.Length; i++) panels[i].Visible = i + 1 == CurrentStep;
            host?.ResumeLayout(true);
            if (host != null)
            {
                try { host.AutoScrollPosition = new Point(0, 0); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            header?.Invalidate();
            ApplyButtons();
            StepChanged?.Invoke(CurrentStep);
        }

        private void ApplyButtons()
        {
            bool last = CurrentStep >= StepCount;
            BackButton.Visible = CurrentStep > 1;
            if (FirstStepButton != null) FirstStepButton.Visible = CurrentStep == 1;
            NextButton.Visible = !last;
            if (!last) NextButton.Text = NextTextFor(CurrentStep);
            foreach (var (button, wanted) in lastStepButtons) button.Visible = last && wanted();
        }
    }
}
