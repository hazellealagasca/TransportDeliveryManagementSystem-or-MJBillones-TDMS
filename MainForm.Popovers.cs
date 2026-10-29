using System;
using System.Drawing;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// Keeps the notifications panel and the search results from being left open where nobody can see them.
    /// Any click outside them, Esc, moving, resizing, maximising or leaving the window closes them, so they can
    /// never sit over the page and swallow clicks.
    /// </summary>
    public partial class MainForm
    {
        private OutsideClickCloser? popoverCloser;
        private void InstallPopoverGuards()
        {
            popoverCloser = new OutsideClickCloser(this);
            Application.AddMessageFilter(popoverCloser);
            this.FormClosed += (s, e) => { if (popoverCloser != null) Application.RemoveMessageFilter(popoverCloser); };
            this.Deactivate += (s, e) => CloseNotificationsPanel();
            this.Resize += (s, e) => CloseAllPopovers();
            this.LocationChanged += (s, e) => CloseAllPopovers();
        }
        internal void CloseNotificationsPanel()
        {
            if (pnlNotificationMenu != null && pnlNotificationMenu.Visible) pnlNotificationMenu.Visible = false;
        }
        internal void CloseAllPopovers()
        {
            CloseNotificationsPanel();
            if (lstSearchDropdown != null && lstSearchDropdown.Visible) lstSearchDropdown.Visible = false;
        }
        private sealed class OutsideClickCloser : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x0201, WM_RBUTTONDOWN = 0x0204, WM_NCLBUTTONDOWN = 0x00A1, WM_KEYDOWN = 0x0100;
            private readonly MainForm form;
            public OutsideClickCloser(MainForm form) { this.form = form; }
            public bool PreFilterMessage(ref Message m)
            {
                try
                {
                    if (form.IsDisposed) return false;
                    var panel = form.pnlNotificationMenu;
                    if (panel == null || !panel.Visible) return false;
                    if (m.Msg == WM_KEYDOWN && (Keys)(int)m.WParam == Keys.Escape) { panel.Visible = false; return false; }
                    if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN && m.Msg != WM_NCLBUTTONDOWN) return false;
                    Point screen = Control.MousePosition;
                    bool insidePanel = panel.RectangleToScreen(panel.ClientRectangle).Contains(screen);
                    var bell = form.btnNotificationBell;
                    bool onBell = bell != null && bell.Visible && bell.RectangleToScreen(bell.ClientRectangle).Contains(screen);
                    // The bell toggles the panel itself; anything else outside the panel closes it.
                    if (!insidePanel && !onBell) panel.Visible = false;
                }
                catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                return false;
            }
        }
    }
}
