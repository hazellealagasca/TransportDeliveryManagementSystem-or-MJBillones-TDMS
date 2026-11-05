using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class MainForm
    {
        private bool focusMode;
        /// <summary>True while a full-screen form (the dispatch order form) is open.</summary>
        public bool InFocusMode => focusMode;

        /// <summary>
        /// Full-screen form mode (Nov 6, 2026): hides the left menu and the top bar so an open form gets the whole window.
        /// The page that turned it on turns it off when its form closes (Back, Save, Esc) or the page is left.
        /// </summary>
        public void SetFocusMode(bool on)
        {
            if (focusMode == on) return;
            focusMode = on;
            SuspendLayout();
            try
            {
                if (pnlNavFlyout != null && on) pnlNavFlyout.Visible = false;
                panelSidebar.Visible = !on;
                panelHeader.Visible = !on;
                if (on) globalSearch?.HideResults();
            }
            finally { ResumeLayout(true); }
        }
    }
}
