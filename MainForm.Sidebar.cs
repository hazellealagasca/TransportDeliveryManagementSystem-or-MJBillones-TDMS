using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// Auto-hiding navigation menu.
    /// The menu opens fully when the app starts. When the mouse leaves it, it tucks away to a slim icon rail;
    /// hovering the rail slides the full menu back out over the page (the page itself does not jump around).
    /// The ☰ button pins the menu open (or un-pins it) for people who prefer it always visible.
    /// </summary>
    public partial class MainForm
    {
        private static bool sidebarPinned = false;
        private Panel? pnlSidebarSpacer;
        private System.Windows.Forms.Timer? tmrSidebarHover;
        private DateTime lastCursorInsideSidebar = DateTime.Now;
        private DateTime cursorEnteredRailAt = DateTime.MinValue;
        private bool startupHold = true;
        private const int HoverOpenDelayMs = 180;
        private const int LeaveCloseDelayMs = 450;
        private void SetupAutoHideSidebar()
        {
            // A spacer keeps the page's left margin; the real menu floats above the page so it can slide in and out.
            pnlSidebarSpacer = new Panel
            {
                Dock = DockStyle.Left,
                Width = panelSidebar.Width,
                BackColor = AppTheme.Sidebar
            };
            int idx = this.Controls.GetChildIndex(panelSidebar);
            this.Controls.Add(pnlSidebarSpacer);
            this.Controls.SetChildIndex(pnlSidebarSpacer, idx);
            panelSidebar.Dock = DockStyle.None;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Height = this.ClientSize.Height;
            panelSidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panelSidebar.BringToFront();
            this.Resize += (s, e) =>
            {
                panelSidebar.Height = this.ClientSize.Height;
                panelSidebar.Location = new Point(0, 0);
            };
            if (sidebarPinned)
            {
                startupHold = false;
                if (isSidebarCollapsed) SetSidebarExpandedImmediately();
                SyncSidebarSpacer();
            }
            UpdatePinTooltip();
            tmrSidebarHover = new System.Windows.Forms.Timer { Interval = 90 };
            tmrSidebarHover.Tick += (s, e) => CheckSidebarHover();
            tmrSidebarHover.Start();
            this.FormClosed += (s, e) => { tmrSidebarHover?.Stop(); tmrSidebarHover?.Dispose(); };
            // Keep the menu open briefly after start-up so people see it, then let it auto-hide.
            this.Shown += (s, e) =>
            {
                var hold = new System.Windows.Forms.Timer { Interval = 1500 };
                hold.Tick += (s2, e2) => { hold.Stop(); hold.Dispose(); startupHold = false; lastCursorInsideSidebar = DateTime.Now; };
                hold.Start();
            };
        }
        private void CheckSidebarHover()
        {
            if (sidebarPinned || startupHold || this.IsDisposed || !this.Visible) return;
            if (this.WindowState == FormWindowState.Minimized) return;
            Point p = Cursor.Position;
            Rectangle sb = panelSidebar.RectangleToScreen(panelSidebar.ClientRectangle);
            bool inside = sb.Contains(p);
            if (!inside && pnlNavFlyout != null && pnlNavFlyout.Visible)
            {
                inside = pnlNavFlyout.RectangleToScreen(pnlNavFlyout.ClientRectangle).Contains(p);
            }
            DateTime now = DateTime.Now;
            if (inside)
            {
                lastCursorInsideSidebar = now;
                if (isSidebarCollapsed && !tmrSidebarAnimation.Enabled)
                {
                    if (cursorEnteredRailAt == DateTime.MinValue) cursorEnteredRailAt = now;
                    if ((now - cursorEnteredRailAt).TotalMilliseconds >= HoverOpenDelayMs)
                    {
                        cursorEnteredRailAt = DateTime.MinValue;
                        panelSidebar.BringToFront();
                        SetSidebarCollapsed(false);
                    }
                }
            }
            else
            {
                cursorEnteredRailAt = DateTime.MinValue;
                if (!isSidebarCollapsed && (now - lastCursorInsideSidebar).TotalMilliseconds >= LeaveCloseDelayMs)
                {
                    // Don't snap shut while one of the app's own pop-ups (menu, dialog) is in use.
                    if (Form.ActiveForm != null && Form.ActiveForm != this) return;
                    SetSidebarCollapsed(true);
                }
            }
        }
        /// <summary>The page margin follows the menu only when it is pinned; otherwise it stays at the rail width.</summary>
        private void SyncSidebarSpacer()
        {
            if (pnlSidebarSpacer == null) return;
            int target = sidebarPinned ? S(SidebarExpandedWidth) : (startupHold && !isSidebarCollapsed ? S(SidebarExpandedWidth) : S(SidebarCollapsedWidth));
            if (!sidebarPinned && isSidebarCollapsed) target = S(SidebarCollapsedWidth);
            if (pnlSidebarSpacer.Width != target) pnlSidebarSpacer.Width = target;
            panelSidebar.BringToFront();
        }
        private void TogglePinSidebar()
        {
            sidebarPinned = !sidebarPinned;
            startupHold = false;
            if (sidebarPinned)
            {
                if (isSidebarCollapsed) SetSidebarCollapsed(false);
                SyncSidebarSpacer();
                ToastHelper.ShowToast(this, "Menu pinned open. Click ☰ again to auto-hide it.", ToastType.Info);
            }
            else
            {
                lastCursorInsideSidebar = DateTime.Now.AddMilliseconds(-LeaveCloseDelayMs);
                if (!isSidebarCollapsed) SetSidebarCollapsed(true);
                ToastHelper.ShowToast(this, "Menu set to auto-hide. Hover the left edge to open it.", ToastType.Info);
            }
            UpdatePinTooltip();
        }
        private void UpdatePinTooltip()
        {
            ttSidebar.SetToolTip(btnToggleSidebar, sidebarPinned ? "Menu is pinned open — click to auto-hide" : "Click to keep the menu open");
        }
    }
}
