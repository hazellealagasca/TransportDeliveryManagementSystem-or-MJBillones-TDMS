using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// Clicking the window's ✕ (or Alt+F4) asks first. The default answer signs the user out and returns to the
    /// login screen; closing the whole program is a separate, deliberate choice.
    /// </summary>
    public partial class MainForm
    {
        private enum CloseChoice { Cancel, Logout, Exit }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            bool userAsked = e.CloseReason == CloseReason.UserClosing;
            if (userAsked && !IsLoggingOut && !IsReloading && !exitConfirmed)
            {
                // Closed from the taskbar while minimized: bring the window back first, otherwise the question
                // is hidden together with the minimized window and the person never sees it.
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = restoreStateBeforeMinimize;
                    Show();
                    Activate();
                }
                var choice = AskHowToClose();
                if (choice == CloseChoice.Cancel || !CanLeaveCurrentView())
                {
                    e.Cancel = true;
                    return;
                }
                if (choice == CloseChoice.Logout)
                {
                    try { MJdatabase.LogSecurityEvent(currentUser?.Username ?? "", "LOGOUT", "User logged out (window closed)"); } catch { }
                    IsLoggingOut = true;
                    SessionContext.ActiveUser = null;
                }
                else
                {
                    try { MJdatabase.LogSecurityEvent(currentUser?.Username ?? "", "LOGOUT", "User exited the application"); } catch { }
                    exitConfirmed = true;
                }
            }
            base.OnFormClosing(e);
        }
        private bool exitConfirmed;
        private FormWindowState restoreStateBeforeMinimize = FormWindowState.Maximized;
        protected override void OnResize(EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) restoreStateBeforeMinimize = WindowState;
            base.OnResize(e);
        }
        private CloseChoice AskHowToClose()
        {
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            using var dlg = new Form
            {
                Text = "Leave the system?",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(D(460), D(210)),
                BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F),
                KeyPreview = true,
                TopMost = true
            };
            try { ThemeManager.StyleWindowChrome(dlg); } catch { }
            string name = (currentUser?.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            string who = name.Length == 0 ? "" : $", {name}";
            var title = new Label
            {
                Text = $"Do you want to exit{who}?",
                Location = new Point(D(24), D(20)),
                Size = new Size(D(412), D(30)),
                Font = new Font("Segoe UI Semibold", 13F),
                ForeColor = AppTheme.TextPrimary,
                AutoEllipsis = true,
                UseMnemonic = false
            };
            var body = new Label
            {
                Text = "You'll be signed out and taken back to the login page. Your saved work stays as it is.",
                Location = new Point(D(24), D(56)),
                Size = new Size(D(412), D(44)),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextSecondary
            };
            var result = CloseChoice.Cancel;
            Button Make(string text, string role, int x, int w, CloseChoice c)
            {
                var b = new Button { Text = text, Location = new Point(x, D(150)), Size = new Size(w, D(38)), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F), Cursor = Cursors.Hand, UseMnemonic = false };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                b.Click += (s, e) => { result = c; dlg.Close(); };
                return b;
            }
            var btnLogout = Make("Yes, log out", "primary", D(296), D(140), CloseChoice.Logout);
            var btnCancel = Make("Stay", "secondary", D(188), D(100), CloseChoice.Cancel);
            var lnkExit = new LinkLabel
            {
                Text = "Close the program instead",
                Location = new Point(D(24), D(160)),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                LinkColor = AppTheme.TextMuted,
                ActiveLinkColor = AppTheme.DangerText,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkExit.LinkClicked += (s, e) => { result = CloseChoice.Exit; dlg.Close(); };
            var line = new Panel { Location = new Point(0, D(126)), Size = new Size(D(460), 1), BackColor = AppTheme.Border };
            dlg.Controls.AddRange(new Control[] { title, body, line, lnkExit, btnCancel, btnLogout });
            dlg.AcceptButton = btnLogout;
            dlg.CancelButton = btnCancel;
            dlg.Shown += (s, e) => btnLogout.Focus();
            dlg.ShowDialog(this);
            return result;
        }
    }
}
