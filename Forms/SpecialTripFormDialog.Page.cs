using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 6, 2026: the special trip form is a full page like the dispatch order form (Back button, numbered section cards,
    /// action bar) and shows the trip's flow: Special trip → Trip report → Service invoice → SOA → Collection receipt,
    /// with a button for the next step. The four "With …" ticks are gone: every special trip follows that flow.
    /// </summary>
    public partial class SpecialTripFormDialog
    {
        private FullPageFormKit? pageKit;
        private Button btnBack = null!;
        private TableLayoutPanel flowBar = null!;
        private FormDirtyTracker? dirtyTracker;
        private bool shownFired;
        /// <summary>Where to go after the form closes (a flow-bar step); run by the page that hosts the form.</summary>
        public Action? PendingNavigation { get; private set; }
        public bool HasUnsavedTyping => !IsSaved && (dirtyTracker?.IsDirty ?? false);
        /// <summary>The main window while this form sits inside a page (a page form can't own popups itself).</summary>
        private IWin32Window OwnerWindow => TopLevel ? this : (IWin32Window?)TopLevelControl ?? this;
        private int Px(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        /// <summary>Puts the form's blocks into section cards (the dispatch layout). Called at the end of building the form.</summary>
        private void BuildFullPage(FlowLayoutPanel actions, params (string Title, string Subtitle, Control Block)[] sections)
        {
            var tblFull = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0) };
            tblFull.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblFull.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            tblFull.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var page = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var header = new Panel { Dock = DockStyle.Top };
            btnBack = new Button { Text = "Back", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => Close();
            header.Controls.Add(btnBack);
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var actionsBar = new Panel { Dock = DockStyle.Bottom };
            actionsBar.Controls.Add(actions);
            page.Controls.Add(body);
            page.Controls.Add(actionsBar);
            page.Controls.Add(header);
            var listDummy = new Panel { Dock = DockStyle.Fill };
            tblFull.Controls.Add(page, 0, 0);
            tblFull.Controls.Add(listDummy, 1, 0);
            foreach (var s in sections) { s.Block.Dock = DockStyle.Top; body.Controls.Add(s.Block); }
            pageKit = new FullPageFormKit(this, tblFull, page, listDummy, header, lblTitle, btnBack, body, actionsBar, "Back to special trips", 980);
            pageKit.Build(sections.Select(s => new FullPageFormKit.Section(s.Title, s.Subtitle, s.Block)), new Control[] { actions });
            flowBar = BuildFlowBar();
            foreach (var cb in new[] { chkPaymentWithReport, chkWithInvoice, chkWithSOA, chkWithCollection }) cb.CheckedChanged += (s, e) => RefreshFlowBar();
            pageKit.AddStrip(flowBar);
            Controls.Add(tblFull);
            tblFull.BringToFront();
            pageKit.Show(true);
        }

        // ------------------------------------------------------------------ Flow bar
        private TableLayoutPanel BuildFlowBar()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 5, RowCount = 1, Height = Px(92), BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return t;
        }

        private sealed record FlowStep(string Title, string Status, bool Done, string? ActionText, Action? Action, bool Next);

        /// <summary>Re-reads what the trip is linked to and redraws its steps (only the ticked ones).</summary>
        private void RefreshFlowBar()
        {
            if (flowBar == null) return;
            lblTitle.Text = currentTripId > 0 ? $"Special trip {txtOrderRef.Text}" : "New special trip";
            var steps = new List<FlowStep>();
            try
            {
                var trip = currentTripId > 0 ? MJdatabase.GetSpecialTripById(currentTripId) : null;
                var flow = SpecialTripFlowState.Compute(trip, chkPaymentWithReport.Checked, chkWithInvoice.Checked, chkWithSOA.Checked, chkWithCollection.Checked);
                var current = flow.Steps.FirstOrDefault(s => !s.Done);
                int n = 1;
                foreach (var s in flow.Steps)
                {
                    if (trip != null && s.Key == "transit") continue; // shown in the details window; the form keeps the bar short
                    Action? act = null;
                    if (s.ActionKey == "complete") act = MarkCompletedHere;
                    else if (s.ActionKey != null && trip != null) { var key = s.ActionKey; var num = s.Number; act = () => { PendingNavigation = () => MainForm.Instance?.GoToSpecialTripStep(key, trip, num); Close(); }; }
                    steps.Add(new FlowStep($"{n++}  {s.Title}", (s.Done ? "✓ " : "") + s.Status, s.Done, act != null ? s.ActionText : null, act, s == current));
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            RenderFlowBar(steps);
        }

        /// <summary>The flow bar's "Mark trip completed": asks to save typing first, then completes the saved trip and stays on the form.</summary>
        private void MarkCompletedHere()
        {
            if (currentTripId <= 0) return;
            if (HasUnsavedTyping)
            {
                MessageBox.Show(OwnerWindow, "Save your changes first (Update), then mark the trip completed.", "Unsaved changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(OwnerWindow, $"Mark special trip {txtOrderRef.Text} as completed?\n\nThe truck, driver and aides are freed. The ticked next steps open from the flow bar.", "Complete trip",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                if (!MJdatabase.MarkSpecialTripCompleted(currentTripId)) return;
                IsSaved = true; // the list reloads when the form closes
                int idx = cbStatus.Items.IndexOf("Completed");
                if (idx >= 0) cbStatus.SelectedIndex = idx;
                if (loadedTrip != null) loadedTrip.Status = "Completed";
                dirtyTracker?.Reset();
                RefreshFlowBar();
                ToastHelper.ShowToast(this, $"{txtOrderRef.Text} marked completed", ToastType.Success);
            }
            catch (Exception ex) { ErrLog.Error("SpecialTripFormDialog.MarkCompleted", ex); MessageBox.Show(OwnerWindow, "Couldn't complete the trip: " + ex.Message, "Complete trip", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void RenderFlowBar(List<FlowStep> steps)
        {
            flowBar.SuspendLayout();
            foreach (Control c in flowBar.Controls.Cast<Control>().ToList()) { flowBar.Controls.Remove(c); c.Dispose(); }
            flowBar.ColumnCount = Math.Max(1, steps.Count);
            flowBar.ColumnStyles.Clear();
            for (int c = 0; c < flowBar.ColumnCount; c++) flowBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / flowBar.ColumnCount));
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                var cell = new Panel { Dock = DockStyle.Fill, Margin = new Padding(i == 0 ? 0 : Px(4), Px(6), i == steps.Count - 1 ? 0 : Px(4), Px(8)), Padding = new Padding(Px(10), Px(6), Px(10), Px(6)),
                    BackColor = s.Done ? AppTheme.SuccessSoft : s.Next ? AppTheme.AccentSoft : AppTheme.Surface };
                var border = s.Done ? AppTheme.Success : s.Next ? AppTheme.Accent : AppTheme.Border;
                cell.Paint += (o, e) => { using var pen = new Pen(border); e.Graphics.DrawRectangle(pen, 0, 0, cell.Width - 1, cell.Height - 1); };
                var title = new Label { Text = s.Title, Dock = DockStyle.Top, Height = Px(20), Font = FontCache.Get("Segoe UI Semibold", 9.5F, FontStyle.Regular), ForeColor = AppTheme.TextPrimary, UseMnemonic = false, AutoEllipsis = true, BackColor = Color.Transparent };
                var status = new Label { Text = s.Status, Dock = DockStyle.Top, Height = Px(32), Font = FontCache.Get("Segoe UI", 8.75F, FontStyle.Regular), ForeColor = s.Done ? AppTheme.SuccessText : AppTheme.TextSecondary, UseMnemonic = false, AutoEllipsis = true, BackColor = Color.Transparent };
                new ToolTip().SetToolTip(status, s.Status);
                if (s.ActionText != null && s.Action != null)
                {
                    var link = new LinkLabel { Text = s.ActionText + "  ›", Dock = DockStyle.Top, Height = Px(20), Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Regular), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentText, UseMnemonic = false, BackColor = Color.Transparent, LinkBehavior = LinkBehavior.HoverUnderline };
                    var act = s.Action;
                    link.LinkClicked += (o, e) => act();
                    cell.Controls.Add(link);
                }
                cell.Controls.Add(status);
                cell.Controls.Add(title);
                flowBar.Controls.Add(cell, i, 0);
            }
            flowBar.ResumeLayout(true);
        }

        // ------------------------------------------------------------------ Closing, Esc, unsaved typing
        protected override void OnShown(EventArgs e)
        {
            shownFired = true;
            base.OnShown(e);
        }
        /// <summary>A form placed inside a page may not raise Shown; runs the "when shown" setup once in that case.</summary>
        public void EnsureShown()
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                if (!shownFired) OnShown(EventArgs.Empty);
                pageKit?.Restyle();
                RefreshFlowBar();
                dirtyTracker ??= new FormDirtyTracker(this);
                dirtyTracker.Reset();
            }));
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            bool userClose = e.CloseReason != CloseReason.ApplicationExitCall && e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.TaskManagerClosing;
            if (userClose && HasUnsavedTyping)
            {
                var owner = (IWin32Window?)TopLevelControl ?? this;
                if (MessageBox.Show(owner, "Discard your unsaved changes?\n\nWhat you typed in this form will be cleared.", "Unsaved changes",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    PendingNavigation = null;
                    return;
                }
            }
            base.OnFormClosing(e);
        }
        /// <summary>Closes without asking (the person already agreed to discard, e.g. when leaving the page).</summary>
        public void CloseWithoutAsking()
        {
            dirtyTracker?.Reset();
            Close();
        }
    }
}
