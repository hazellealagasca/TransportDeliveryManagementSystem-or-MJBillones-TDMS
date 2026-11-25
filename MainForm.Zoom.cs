using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// Display size for the whole system: A- / 100% / A+ in the top bar (beside Settings) and Ctrl + / Ctrl - / Ctrl 0 on every screen.
    /// The working area (every screen) follows <see cref="UiZoom"/>; the top bar and the sidebar stay at their normal size so they are always usable.
    /// </summary>
    public partial class MainForm
    {
        private Button? btnZoomOut, btnZoomLabel, btnZoomIn;
        /// <summary>The display size each screen was last scaled to (a screen kept in memory while another one was open is scaled when it comes back).</summary>
        private readonly Dictionary<Control, float> zoomedAt = new();

        private void HookUiZoom()
        {
            Action changed = OnUiZoomChanged;
            UiZoom.Changed += changed;
            FormClosed += (s, e) => UiZoom.Changed -= changed;
        }

        private IEnumerable<Button> BuildZoomButtons()
        {
            Button Make(string text, string tip, Action act)
            {
                var b = new Button { Text = text, TabStop = false, Name = "btnZoom" + (text.StartsWith("A") ? (text == "A+" ? "In" : "Out") : "Label") };
                b.Click += (s, e) => act();
                ttSidebar.SetToolTip(b, tip);
                return b;
            }
            btnZoomOut = Make("A−", "Smaller text and controls (Ctrl −)", UiZoom.StepDown);
            btnZoomLabel = Make(UiZoom.Label, "Display size: click to go back to 100 % (Ctrl 0)", UiZoom.Reset);
            btnZoomIn = Make("A+", "Bigger text and controls (Ctrl +)", UiZoom.StepUp);
            return new[] { btnZoomOut, btnZoomLabel, btnZoomIn };
        }

        private void StyleZoomButtons()
        {
            foreach (var b in new[] { btnZoomOut, btnZoomLabel, btnZoomIn })
            {
                if (b == null) continue;
                StyleHeaderChip(b, AppTheme.Surface, AppTheme.TextSecondary, AppTheme.SurfaceHover, outline: true);
                b.Margin = new Padding(0, 0, 2, 0);
            }
            if (btnZoomIn != null) btnZoomIn.Margin = new Padding(0, 0, 8, 0);
        }

        private void OnUiZoomChanged()
        {
            if (IsDisposed) return;
            if (btnZoomLabel != null) btnZoomLabel.Text = UiZoom.Label;
            var current = panelMainContent.Controls.Count > 0 ? panelMainContent.Controls[0] : null;
            if (current != null) ZoomShownView(current, force: true);
        }

        /// <summary>Scales a screen that was just shown (or is showing while the size changes) to the current display size.</summary>
        private void ZoomShownView(Control view, bool force = false)
        {
            float f = UiZoom.Factor;
            if (!force && zoomedAt.TryGetValue(view, out var done) && Math.Abs(done - f) < 0.001f) return;
            if (!zoomedAt.ContainsKey(view) && Math.Abs(f - 1f) < 0.001f && !force) { zoomedAt[view] = f; return; } // a screen first shown at 100 % has nothing to scale (its sizes are remembered when it is first zoomed)
            UiZoom.Apply(view);
            zoomedAt[view] = f;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Oemplus: case Keys.Control | Keys.Add: UiZoom.StepUp(); return true;
                case Keys.Control | Keys.OemMinus: case Keys.Control | Keys.Subtract: UiZoom.StepDown(); return true;
                case Keys.Control | Keys.D0: case Keys.Control | Keys.NumPad0: UiZoom.Reset(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
