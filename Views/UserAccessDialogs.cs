using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Base look for the two account dialogs: header, scrollable body, button band.</summary>
    internal abstract class AccountDialogBase : Form
    {
        protected readonly Panel Body;
        protected readonly FlowLayoutPanel Footer;
        protected int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        protected AccountDialogBase(string title, string heading, string subtitle)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false; ShowIcon = false;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary;
            Font = new Font("Segoe UI", 9.75F);
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            var head = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(D(24), D(16), D(24), D(10)), BackColor = AppTheme.Surface };
            var h1 = new Label { Text = heading, Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            var h2 = new Label { Text = subtitle, Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, Padding = new Padding(0, D(4), 0, 0) };
            head.Controls.Add(h2); head.Controls.Add(h1);
            // Wrap the explanation to the window width.
            void WrapHead() { int w = ClientSize.Width - head.Padding.Horizontal; if (w > 100) { h2.MaximumSize = new Size(w, 0); h1.MaximumSize = new Size(w, 0); } }
            Resize += (s, e) => WrapHead();
            Load += (s, e) => WrapHead();
            Body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(D(24), D(4), D(24), D(12)), BackColor = AppTheme.Surface };
            Footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(60), FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(D(16), D(13), D(20), D(13)), BackColor = AppTheme.SurfaceAlt };
            Footer.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, Footer.Width, 0); };
            Controls.Add(Body); Controls.Add(Footer); Controls.Add(head);
        }
        protected Button AddButton(string text, string role, int minW, Action? onClick = null)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(D(minW), D(34)), Margin = new Padding(D(8), 0, 0, 0), Cursor = Cursors.Hand, UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, role);
            if (onClick != null) b.Click += (s, e) => onClick();
            Footer.Controls.Add(b);
            return b;
        }
        protected Label Text2(string t, Font f, Color c, int top = 0) => new Label { Text = t, AutoSize = true, Font = f, ForeColor = c, Margin = new Padding(0, top, 0, 0), UseMnemonic = false, BackColor = Color.Transparent };
    }
    /// <summary>Settings → User Accounts → "Access &amp; permissions…": what one staff account can open.</summary>
    internal sealed class UserAccessDialog : AccountDialogBase
    {
        private readonly User user;
        private readonly Dictionary<string, ToggleSwitch> switches = new();
        public UserAccessDialog(User u)
            : base("Access & permissions", $"What {(string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)} can use",
                   "Switch off the screens this account should not see. Hidden screens disappear from the menu, the tabs, search and the dashboard. Changes apply the next time this person signs in.")
        {
            user = u;
            ClientSize = new Size(D(640), D(620));
            bool isAdminAccount = SessionContext.IsAdminRole(u.Role);
            var denied = Permissions.DeniedFor(u.Username);
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = AppTheme.Surface };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            if (isAdminAccount)
            {
                var note = Text2("This is an Owner / Administrator account. Administrators can always use every screen, so there is nothing to switch off here.", new Font("Segoe UI Semibold", 9.5F), AppTheme.InfoText, D(6));
                note.MaximumSize = new Size(D(580), 0);
                stack.Controls.Add(note); stack.SetColumnSpan(note, 2);
            }
            foreach (var (module, label) in Permissions.Modules)
            {
                var modHead = Text2(label.ToUpperInvariant(), new Font("Segoe UI Semibold", 8.5F), AppTheme.TextMuted, D(16));
                var links = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, D(14), 0, 0), BackColor = Color.Transparent, Anchor = AnchorStyles.Right };
                LinkLabel Link(string t, bool on)
                {
                    var l = new LinkLabel { Text = t, AutoSize = true, LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(D(10), 0, 0, 0), Enabled = !isAdminAccount };
                    string m = module;
                    l.LinkClicked += (s, e) => { foreach (var a in Permissions.Areas.Where(a => a.Module == m)) switches[a.Key].Checked = on; };
                    return l;
                }
                links.Controls.Add(Link("Allow all", true)); links.Controls.Add(Link("Hide all", false));
                stack.Controls.Add(modHead); stack.Controls.Add(links);
                foreach (var a in Permissions.Areas.Where(a => a.Module == module))
                {
                    var text = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, D(8), D(12), D(6)), BackColor = Color.Transparent };
                    text.Controls.Add(Text2(a.Label, new Font("Segoe UI Semibold", 10F), AppTheme.TextPrimary));
                    text.Controls.Add(Text2(a.Description, new Font("Segoe UI", 8.75F), AppTheme.TextSecondary, D(2)));
                    var sw = new ToggleSwitch { Anchor = AnchorStyles.Right, Margin = new Padding(0, D(12), 0, 0), Enabled = !isAdminAccount };
                    sw.SetQuietly(isAdminAccount || !denied.Contains(a.Key));
                    switches[a.Key] = sw;
                    stack.Controls.Add(text); stack.Controls.Add(sw);
                }
            }
            Body.Controls.Add(stack);
            var save = AddButton(isAdminAccount ? "Close" : "Save access", "primary", 110, () =>
            {
                if (isAdminAccount) { DialogResult = DialogResult.Cancel; return; }
                try
                {
                    var hidden = switches.Where(kv => !kv.Value.Checked).Select(kv => kv.Key).ToList();
                    Permissions.SetDenied(user.Username, hidden);
                    DialogResult = DialogResult.OK;
                }
                catch (Exception ex) { AppLog.ShowFailure(this, "Access & permissions", "The access settings could not be saved.", ex); }
            });
            if (!isAdminAccount) AddButton("Cancel", "secondary", 90, () => DialogResult = DialogResult.Cancel);
            AcceptButton = save;
        }
    }
    /// <summary>
    /// Settings → User Accounts → "Sign-in details…". Passwords are stored scrambled (hashed), so nobody — not even
    /// the system — can read an existing password back. After the administrator confirms their own password, this
    /// shows the account's username and can set a new password that is shown once.
    /// </summary>
    internal sealed class SignInDetailsDialog : AccountDialogBase
    {
        private readonly User user;
        private readonly TextBox txtAdminPass;
        private readonly Panel details;
        private readonly Label lblNewPass;
        private readonly Button btnReveal;
        public SignInDetailsDialog(User u)
            : base("Sign-in details", $"Sign-in details for {(string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)}",
                   "For your protection, confirm your own administrator password first.")
        {
            user = u;
            ClientSize = new Size(D(560), D(430));
            var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = AppTheme.Surface };
            stack.Controls.Add(Text2("Your password (" + SessionContext.CurrentUsername + ")", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, D(8)));
            var passRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, D(4), 0, 0), BackColor = Color.Transparent };
            txtAdminPass = new TextBox { Width = D(300), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10.5F) };
            btnReveal = new Button { Text = "Show details", AutoSize = true, MinimumSize = new Size(D(120), D(30)), Margin = new Padding(D(10), 0, 0, 0), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnReveal, "primary");
            passRow.Controls.Add(txtAdminPass); passRow.Controls.Add(btnReveal);
            stack.Controls.Add(passRow);
            details = new Panel { AutoSize = true, Visible = false, Margin = new Padding(0, D(18), 0, 0), BackColor = Color.Transparent };
            var inner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, BackColor = Color.Transparent };
            inner.Controls.Add(Text2("USERNAME", new Font("Segoe UI Semibold", 8.5F), AppTheme.TextMuted));
            var userRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
            userRow.Controls.Add(Text2(u.Username, new Font("Consolas", 13F, FontStyle.Bold), AppTheme.TextPrimary, D(2)));
            var copyUser = new LinkLabel { Text = "Copy", AutoSize = true, LinkColor = AppTheme.AccentText, Margin = new Padding(D(12), D(8), 0, 0), LinkBehavior = LinkBehavior.HoverUnderline };
            copyUser.LinkClicked += (s, e) => { try { Clipboard.SetText(u.Username); copyUser.Text = "Copied ✓"; } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            userRow.Controls.Add(copyUser);
            inner.Controls.Add(userRow);
            inner.Controls.Add(Text2("PASSWORD", new Font("Segoe UI Semibold", 8.5F), AppTheme.TextMuted, D(14)));
            var why = Text2("The current password can't be shown: it is stored scrambled so that nobody can read it, not even the system. If it is forgotten, set a new one below and give it to the person; they can change it later in their profile.", new Font("Segoe UI", 9F), AppTheme.TextSecondary, D(2));
            why.MaximumSize = new Size(D(500), 0);
            inner.Controls.Add(why);
            var setRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, D(10), 0, 0), BackColor = Color.Transparent };
            var btnSet = new Button { Text = "Set a new password", AutoSize = true, MinimumSize = new Size(D(160), D(32)), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnSet, "secondary");
            lblNewPass = Text2("", new Font("Consolas", 13F, FontStyle.Bold), AppTheme.SuccessText, D(6));
            lblNewPass.Margin = new Padding(D(12), D(6), 0, 0);
            var copyPass = new LinkLabel { Text = "Copy", AutoSize = true, Visible = false, LinkColor = AppTheme.AccentText, Margin = new Padding(D(10), D(10), 0, 0), LinkBehavior = LinkBehavior.HoverUnderline };
            copyPass.LinkClicked += (s, e) => { try { Clipboard.SetText(lblNewPass.Text); copyPass.Text = "Copied ✓"; } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            setRow.Controls.Add(btnSet); setRow.Controls.Add(lblNewPass); setRow.Controls.Add(copyPass);
            inner.Controls.Add(setRow);
            details.Controls.Add(inner);
            stack.Controls.Add(details);
            Body.Controls.Add(stack);
            btnReveal.Click += (s, e) =>
            {
                if (!ConfirmAdmin()) return;
                details.Visible = true;
                txtAdminPass.Enabled = false; btnReveal.Enabled = false;
                btnReveal.Text = "Confirmed ✓";
            };
            btnSet.Click += (s, e) =>
            {
                if (MessageBox.Show(this, $"Give {u.Username} a new password?\n\nTheir old password will stop working straight away.", "Set a new password", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                string pw = NewPassword();
                if (!MJdatabase.UpdateUserPassword(u.Id, pw)) { MessageBox.Show(this, "The new password could not be saved. Nothing was changed.", "Set a new password", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                try { MJdatabase.LogSecurityEvent(SessionContext.CurrentUsername, "PASSWORD_RESET", $"Administrator set a new password for '{u.Username}'"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                lblNewPass.Text = pw; copyPass.Visible = true; btnSet.Text = "Set another";
            };
            AcceptButton = btnReveal;
            AddButton("Close", "secondary", 90, () => DialogResult = DialogResult.Cancel);
            Shown += (s, e) => txtAdminPass.Focus();
        }
        private bool ConfirmAdmin()
        {
            try
            {
                if (!SessionContext.IsAdmin) { MessageBox.Show(this, "Only the Owner / Administrator can see sign-in details.", "Sign-in details", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                var me = MJdatabase.GetUsers().FirstOrDefault(x => x.Username.Equals(SessionContext.CurrentUsername, StringComparison.OrdinalIgnoreCase));
                if (me != null && MJdatabase.VerifyPassword(txtAdminPass.Text, me.Password, out _)) return true;
                try { MJdatabase.LogSecurityEvent(SessionContext.CurrentUsername, "SIGNIN_DETAILS_DENIED", $"Wrong password when opening sign-in details of '{user.Username}'"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                MessageBox.Show(this, "That password is not correct.", "Sign-in details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAdminPass.SelectAll(); txtAdminPass.Focus();
                return false;
            }
            catch (Exception ex) { AppLog.ShowFailure(this, "Sign-in details", "Your password could not be checked.", ex); return false; }
        }
        /// <summary>Easy to read aloud and type: no 0/O or 1/l, letters and numbers mixed.</summary>
        private static string NewPassword()
        {
            const string letters = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            Span<char> c = stackalloc char[9];
            for (int i = 0; i < 4; i++) c[i] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
            c[4] = '-';
            for (int i = 5; i < 9; i++) c[i] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            return new string(c);
        }
    }
}
