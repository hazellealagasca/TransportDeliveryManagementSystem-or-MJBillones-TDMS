using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Display size for the Payments tabs: A− / A+ at the end of the tab strip, and Ctrl + / Ctrl − / Ctrl 0.</summary>
    public partial class FinanceHubView
    {
        private Button? btnZoomLabel;

        private void InitZoom()
        {
            var host = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0) };
            Button Make(string text, string tip, Action act, int width)
            {
                var b = new Button { Text = text, Width = width, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 0, 0), TabStop = false };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = AppTheme.Border;
                b.BackColor = AppTheme.Surface; b.ForeColor = AppTheme.TextPrimary;
                b.Click += (s, e) => act();
                new ToolTip().SetToolTip(b, tip);
                return b;
            }
            var minus = Make("A−", "Smaller text and controls (Ctrl −)", UiZoom.StepDown, 40);
            btnZoomLabel = Make(UiZoom.Label, "Display size: click to go back to 100 % (Ctrl 0)", UiZoom.Reset, 60);
            var plus = Make("A+", "Bigger text and controls (Ctrl +)", UiZoom.StepUp, 40);
            host.Controls.AddRange(new Control[] { minus, btnZoomLabel, plus });
            pnlTopNav.Controls.Add(host);
            pnlTopNav.Controls.SetChildIndex(host, pnlTopNav.Controls.Count - 1); // docks first, so the tab strip fills what is left
            void Changed()
            {
                if (IsDisposed) return;
                if (btnZoomLabel != null) btnZoomLabel.Text = UiZoom.Label;
                UiZoom.Apply(pnlContentHost);
            }
            UiZoom.Changed += Changed;
            Disposed += (s, e) => UiZoom.Changed -= Changed;
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
