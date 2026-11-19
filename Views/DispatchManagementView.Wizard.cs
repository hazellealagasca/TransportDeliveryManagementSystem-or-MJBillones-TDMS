using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The New / Edit Dispatch Order form as a 3-step wizard: 1 Client &amp; stores, 2 Crates, truck &amp; crew,
    /// 3 Schedule &amp; status. One step card is visible at a time; the crate footer and the action bar stay on every step.
    /// All controls, handlers and save validation are the existing ones: this file only regroups and navigates.
    /// </summary>
    public partial class DispatchManagementView
    {
        private static readonly string[] WizardStepNames = { "Client & stores", "Crates, truck & crew", "Schedule & status" };
        private static readonly string[] WizardNextText = { "Crates, truck & crew  →", "Schedule & status  →" };
        private const int WizardLastStep = 3;

        private Panel? pnlStep1ClientStores, pnlStep2LoadCrew, pnlStep3Schedule;
        private Control? pnlStepperHeader;
        private Button? btnBack, btnNext;
        private int wizardStep = 1;
        private bool saveVisWanted, updateVisWanted, rescheduleVisWanted;

        /// <summary>The step (1-3) currently shown.</summary>
        public int CurrentWizardStep => wizardStep;

        private Panel?[] StepPanels => new[] { pnlStep1ClientStores, pnlStep2LoadCrew, pnlStep3Schedule };

        /// <summary>Shows step 1-3 with no validation (used by the stepper in edit mode, Back, Reset and error jumps).</summary>
        public void GoToStep(int step)
        {
            wizardStep = Math.Max(1, Math.Min(WizardLastStep, step));
            ApplyWizardStep();
        }

        /// <summary>Validates the current step and moves to the next one. Returns true when the step was passed.</summary>
        public bool TryNextStep()
        {
            if (wizardStep >= WizardLastStep) return true;
            if (!ValidateWizardStep(wizardStep)) return false;
            GoToStep(wizardStep + 1);
            return true;
        }

        /// <summary>Switches to the step that holds <paramref name="c"/> (so a control we are about to focus is actually visible).</summary>
        private void ShowStepFor(Control? c)
        {
            var panels = StepPanels;
            for (var p = c; p != null; p = p.Parent)
            {
                int i = Array.IndexOf(panels, p as Panel);
                if (i >= 0) { if (wizardStep != i + 1) GoToStep(i + 1); return; }
            }
        }

        private void FocusStep(Control c)
        {
            ShowStepFor(c);
            c.Focus();
        }

        private bool IsWizardEditMode => selectedDelivery != null;

        private bool CanJumpToStep(int step) => step != wizardStep && (IsWizardEditMode || step < wizardStep);

        private bool ValidateWizardStep(int step)
        {
            switch (step)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(txtDispatchNumber.Text))
                    {
                        MessageBox.Show("Please enter the Order Reference / Dispatch #.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtDispatchNumber.Focus();
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(cbClient.Text))
                    {
                        MessageBox.Show("Please select or enter a Client Company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cbClient.Focus();
                        return false;
                    }
                    if (cbTempType != null && cbTempType.SelectedIndex < 0)
                    {
                        MessageBox.Show("Please choose a Temp Type (Dry, Cold or Combi) for this load.", "Temp Type Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cbTempType.Focus();
                        return false;
                    }
                    if (!ResolvePendingEntryRow()) return false;
                    int stores = chkMultiDrop.Checked && currentStops.Count > 0 ? currentStops.Count : GetGridStoreList().Count;
                    if (stores == 0)
                    {
                        MessageBox.Show("Please add at least one store to this order (fill in the store row and click Add store).", "Store required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cbDestination.Visible) cbDestination.Focus(); else txtEntryCode?.Focus();
                        return false;
                    }
                    return true;
                case 2:
                    if ((int)numGCrate.Value + (int)numECrate.Value <= 0)
                    {
                        MessageBox.Show("Please enter at least 1 crate for dispatch.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        numGCrate.Focus();
                        return false;
                    }
                    if (cbTruck.SelectedIndex < 0 || cbDriver.SelectedIndex < 0)
                    {
                        MessageBox.Show("STRICT FLEET ASSIGNMENT REQUIRED:\nPlease select both an assigned Truck Vehicle and Driver before saving this dispatch order.", "Fleet Assignment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (cbTruck.SelectedIndex < 0) cbTruck.Focus(); else cbDriver.Focus();
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private void ApplyWizardStep()
        {
            var panels = StepPanels;
            if (panels.Any(p => p == null)) return;
            pnlFormBody.SuspendLayout();
            for (int i = 0; i < panels.Length; i++) panels[i]!.Visible = i + 1 == wizardStep;
            pnlFormBody.ResumeLayout(true);
            try { pnlFormBody.AutoScrollPosition = new Point(0, 0); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            pnlStepperHeader?.Invalidate();
            ApplyWizardButtons();
        }

        /// <summary>Remembers which of Save / Update / Reschedule the current mode wants (SetButtonStates decides); step 4 shows them.</summary>
        private void RememberSaveButtons()
        {
            ApplyWizardButtons();
        }

        private void ApplyWizardButtons()
        {
            if (btnNext == null || btnBack == null) return;
            bool last = wizardStep >= WizardLastStep;
            btnBack.Visible = wizardStep > 1;
            btnClear.Visible = wizardStep == 1;
            btnNext.Visible = !last;
            if (!last) btnNext.Text = WizardNextText[wizardStep - 1];
            btnSave.Visible = last && saveVisWanted;
            btnUpdate.Visible = last && updateVisWanted;
            btnReschedule.Visible = last && rescheduleVisWanted;
        }

        private void CreateWizardButtons()
        {
            if (btnNext != null) return;
            btnBack = new Button { Name = "btnBack", Text = "←  Back", Width = Dpi(130), Visible = false, TabStop = true };
            btnNext = new Button { Name = "btnNext", Text = WizardNextText[0], Width = Dpi(230), Visible = true };
            UIThemeHelper.SetButtonRole(btnBack, "secondary");
            UIThemeHelper.SetButtonRole(btnNext, "primary");
            btnBack.Click += (s, e) => { if (wizardStep > 1) GoToStep(wizardStep - 1); };
            btnNext.Click += (s, e) => TryNextStep();
        }

        /// <summary>Reset form (step 1) / Back (steps 2-4) sit on the left of the action bar; Next / Save on the right.</summary>
        private void BuildWizardLeftActions()
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
            btnClear.Margin = new Padding(0, 0, Dpi(10), 0);
            btnBack!.Margin = new Padding(0, 0, Dpi(10), 0);
            left.Controls.Add(btnBack);
            left.Controls.Add(btnClear);
            pnlFormActionsBar.Controls.Add(left);
        }

        private void BuildWizardStepper()
        {
            if (pnlStepperHeader != null) { pnlFormBody.Controls.Remove(pnlStepperHeader); }
            var st = new WizardStepper(WizardStepNames, () => wizardStep, CanJumpToStep, n => GoToStep(n)) { Name = "pnlStepperHeader", Dock = DockStyle.Top };
            st.Height = Dpi(64);
            st.Margin = new Padding(0);
            EventHandler themed = (s, e) => { try { if (!st.IsDisposed) st.Invalidate(); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            ThemeManager.ThemeChanged += themed;
            st.Disposed += (s, e) => ThemeManager.ThemeChanged -= themed;
            pnlStepperHeader = st;
        }

        /// <summary>Owner-drawn step indicator: badge + label per step (active = accent, done = green tick, upcoming = soft).</summary>
        private sealed class WizardStepper : Control
        {
            private readonly string[] names;
            private readonly Func<int> current;
            private readonly Func<int, bool> canJump;
            private readonly Action<int> jump;

            public WizardStepper(string[] names, Func<int> current, Func<int, bool> canJump, Action<int> jump)
            {
                this.names = names; this.current = current; this.canJump = canJump; this.jump = jump;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                TabStop = false;
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
                int d = (int)(30 * S);
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
                    using (var nf = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                        TextRenderer.DrawText(g, done ? "✓" : n.ToString(), nf, badge, ink, fl);
                    var lbl = new Rectangle(badge.Right + (int)(10 * S), 0, cell.Right - badge.Right - (int)(14 * S), Height);
                    using (var lf = new Font("Segoe UI Semibold", 10F, active ? FontStyle.Bold : FontStyle.Regular))
                        TextRenderer.DrawText(g, names[i], lf, lbl, active ? AppTheme.TextPrimary : done ? AppTheme.TextSecondary : AppTheme.TextMuted,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                }
            }
        }
    }
}
