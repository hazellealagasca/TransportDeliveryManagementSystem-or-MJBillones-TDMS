using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The signed-in person's own account: who they are, what they can open, their sign-in and security health,
    /// what they changed recently, and a place to correct their own name and contact number.
    /// </summary>
    public class UserProfileDialog : Form
    {
        public Action? OnNavigateToUserManagement;
        public Action? OnLogout;

        /// <summary>True when the person saved changes to their name or contact number.</summary>
        public bool ProfileChanged { get; private set; }
        private readonly User user;
        private readonly ProfileHeader header;
        private readonly SegmentBar tabs;
        private readonly Panel body;
        private readonly Panel[] pages = new Panel[4];
        private List<AuditTrailEntry> myActions = new List<AuditTrailEntry>();
        private List<LoginAudit> mySignIns = new List<LoginAudit>();
        private DateTime? lastPasswordChange;
        private bool hasRecovery;
        private TextBox txtFirst = null!, txtMiddle = null!, txtLast = null!, txtContact = null!;
        private Label lblEditStatus = null!;
        private (string F, string M, string L, string C) original;
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public UserProfileDialog(User user)
        {
            this.user = user;
            Text = "My account";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(S(720), S(640));
            BackColor = AppTheme.Canvas;
            LoadData();
            header = new ProfileHeader(user) { Dock = DockStyle.Top, Height = S(124) };
            header.SignedInAt = mySignIns.FirstOrDefault(a => a.EventType == "LOGIN_SUCCESS")?.Timestamp;
            tabs = new SegmentBar(new[] { "Overview", "Edit profile", "Security & sign-ins", "My activity" }) { Dock = DockStyle.Top, Height = S(46) };
            body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas };
            var footer = BuildFooter();
            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(tabs);
            Controls.Add(header);
            tabs.SelectedChanged += (s, e) => ShowPage(tabs.Selected);
            ShowPage(0);
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.S && tabs.Selected == 1) { SaveProfile(); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.Tab) { tabs.Selected = (tabs.Selected + (e.Shift ? 3 : 1)) % 4; e.Handled = true; }
            };

            try { ThemeManager.StyleWindowChrome(this); } catch { }
        }
        // ---------------------------------------------------------------- data
        private void LoadData()
        {
            string me = (user.Username ?? "").Trim();
            try
            {
                var all = MJdatabase.GetAuditTrail(1500);
                myActions = all.Where(a => string.Equals((a.Username ?? "").Trim(), me, StringComparison.OrdinalIgnoreCase)).OrderByDescending(a => a.Timestamp).ToList();
                lastPasswordChange = all.Where(a => a.ActionType == "PASSWORD_CHANGE" && a.TableName == "Users" && a.RecordId == user.Id).Select(a => (DateTime?)a.Timestamp).Max();
            }
            catch { myActions = new List<AuditTrailEntry>(); }
            try
            {
                mySignIns = MJdatabase.GetSecurityAuditLogs(2000).Where(a => string.Equals((a.Username ?? "").Trim(), me, StringComparison.OrdinalIgnoreCase)).OrderByDescending(a => a.Timestamp).ToList();
                var reset = mySignIns.Where(a => a.EventType == "PASSWORD_RESET_SUCCESS").Select(a => (DateTime?)a.Timestamp).Max();
                if (reset.HasValue && (!lastPasswordChange.HasValue || reset > lastPasswordChange)) lastPasswordChange = reset;
            }
            catch { mySignIns = new List<LoginAudit>(); }
            try { hasRecovery = !string.IsNullOrWhiteSpace(MJdatabase.GetRawUserSecurityQuestion(user.Username ?? "")); } catch { hasRecovery = false; }
        }
        private void ShowPage(int i)
        {
            if (pages[i] == null)
            {
                pages[i] = i switch { 0 => BuildOverview(), 1 => BuildEditPage(), 2 => BuildSecurityPage(), _ => BuildActivityPage() };
                pages[i].Visible = false;
                body.Controls.Add(pages[i]);
            }
            foreach (var p in pages) if (p != null) p.Visible = p == pages[i];
            if (i == 1) BeginInvoke(new Action(() => txtFirst?.Focus()));
        }
        private void RebuildPages()
        {
            int sel = tabs.Selected;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null) continue;
                body.Controls.Remove(pages[i]);
                pages[i].Dispose();
                pages[i] = null!;
            }
            ShowPage(sel);
        }
        // ---------------------------------------------------------------- overview
        private Panel BuildOverview()
        {
            var page = new StackPage(S(20));
            DateTime today = DateTime.Today;
            int todayCount = myActions.Count(a => a.Timestamp.Date == today);
            var logins = mySignIns.Where(a => a.EventType == "LOGIN_SUCCESS").ToList();
            var previous = logins.Count > 1 ? logins[1] : null;
            int failed = mySignIns.Count(a => (a.EventType == "LOGIN_FAILED" || a.EventType == "LOCKED_OUT") && a.Timestamp >= today.AddDays(-7));
            var row = new TileRow(S(10)) { Height = S(92) };
            row.Add(new StatTile("Changes today", AppTheme.Accent, todayCount == 1 ? "record you saved" : "records you saved") { Value = todayCount.ToString("N0"), Clickable = true });
            row.Add(new StatTile("Previous sign-in", AppTheme.Info, previous == null ? "this is your first session" : Ago(previous.Timestamp)) { Value = previous == null ? "—" : previous.Timestamp.ToString("MMM d, h:mm tt"), Clickable = true });
            row.Add(new StatTile("Failed sign-ins", failed > 0 ? AppTheme.Danger : AppTheme.Success, "in the last 7 days") { Value = failed.ToString(), ValueColor = failed > 0 ? AppTheme.DangerText : null, Clickable = true });
            row.Add(new StatTile("Account recovery", hasRecovery ? AppTheme.Success : AppTheme.Warning, hasRecovery ? "security question is set" : "set a security question") { Value = hasRecovery ? "Ready" : "Not set", ValueColor = hasRecovery ? AppTheme.SuccessText : AppTheme.WarningText, Clickable = true });
            row.Tiles[0].Click += (s, e) => tabs.Selected = 3;
            row.Tiles[1].Click += (s, e) => tabs.Selected = 2;
            row.Tiles[2].Click += (s, e) => tabs.Selected = 2;
            row.Tiles[3].Click += (s, e) => tabs.Selected = 2;
            page.AddRow(row, S(18));
            page.AddRow(SectionLabel("Account", "Your details as the system knows them. Fix your name or number under Edit profile."), S(8));
            var info = new InfoGrid();
            info.Add("Full name", DisplayName(user));
            info.Add("Username", "@" + user.Username);
            info.Add("Role", string.IsNullOrWhiteSpace(user.Role) ? "—" : user.Role);
            info.Add("Account ID", $"#{user.Id:D4}");
            info.Add("Contact number", string.IsNullOrWhiteSpace(user.ContactNumber) ? "Not set" : user.ContactNumber, string.IsNullOrWhiteSpace(user.ContactNumber));
            info.Add("Member since", user.CreatedAt == default ? "—" : $"{user.CreatedAt:MMMM d, yyyy}  ·  {MemberFor(user.CreatedAt)}");
            page.AddRow(info, S(18));
            page.AddRow(SectionLabel("What you can open", "Access comes from your role. Ask the owner or an administrator if you need more."), S(8));
            var access = new FeedList { EmptyText = "" };
            void Area(bool ok, string name, string what) => access.Items.Add(new FeedItem(name, what, ok ? "Allowed" : "No access", ok ? AppTheme.Success : AppTheme.BorderStrong, !ok));
            Area(SessionContext.CanAccessOperations, "Operations", "Dispatch, loading orders, special trips, trip and incident reports");
            Area(SessionContext.CanAccessFinance, "Finance", "Billing, collections, statements of account");
            Area(SessionContext.CanAccessResources, "Resources", "Trucks, crew, crew trip and resource history");
            Area(SessionContext.CanAccessMasterData, "Master data", "Clients, delivery routes, rate matrix, categories");
            Area(SessionContext.CanAccessSettings, "Settings & user accounts", "System settings, appearance, users, security audit, backups");
            access.FitHeight();
            page.AddRow(access, 0);
            return page;
        }
        // ---------------------------------------------------------------- edit
        private Panel BuildEditPage()
        {
            var page = new StackPage(S(20));
            page.AddRow(SectionLabel("Your name and contact number", "This is how you appear on dispatches, reports and printouts you prepare."), S(10));
            var (f, m, l) = NameParts(user);
            original = (f, m, l, (user.ContactNumber ?? "").Trim());
            var card = new FormCard();
            txtFirst = card.Field("First name *", f, 0, 0);
            txtMiddle = card.Field("Middle name or initial", m, 1, 0);
            txtLast = card.Field("Last name *", l, 0, 1);
            txtContact = card.Field("Contact number", original.C, 1, 1, "09XXXXXXXXX");
            card.Field("Username", "@" + user.Username, 0, 2, readOnly: true);
            card.Field("Role", user.Role ?? "", 1, 2, readOnly: true);
            txtContact.MaxLength = 13;
            txtContact.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-') e.Handled = true; };
            foreach (var t in new[] { txtFirst, txtMiddle, txtLast }) t.MaxLength = 50;
            card.Height = S(250);
            page.AddRow(card, S(6));
            var note = new Label { Text = "Your username and role can only be changed by the owner or an administrator in User accounts.", ForeColor = AppTheme.TextMuted, AutoSize = false, Height = S(22), Font = new Font("Segoe UI", 8.75F) };
            page.AddRow(note, S(12));
            var bar = new Panel { Height = S(40), BackColor = AppTheme.Canvas };
            var btnSave = new Button { Text = "Save changes", Width = S(140), Height = S(36), Location = new Point(0, 0), Cursor = Cursors.Hand };
            var btnUndo = new Button { Text = "Undo", Width = S(90), Height = S(36), Location = new Point(S(150), 0), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnUndo, "secondary");
            lblEditStatus = new Label { AutoSize = false, Location = new Point(S(252), 0), Size = new Size(S(400), S(36)), TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppTheme.TextMuted, Text = "Ctrl+S saves" };
            btnSave.Click += (s, e) => SaveProfile();
            btnUndo.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Are you sure you want to undo your edits and put back the saved details?", "Confirm reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                txtFirst.Text = original.F; txtMiddle.Text = original.M; txtLast.Text = original.L; txtContact.Text = original.C;
                SetEditStatus("Changes undone.", AppTheme.TextMuted);
            };

            bar.Controls.Add(btnSave);
            bar.Controls.Add(btnUndo);
            bar.Controls.Add(lblEditStatus);
            page.AddRow(bar, 0);
            return page;
        }
        private void SetEditStatus(string text, Color color)
        {
            if (lblEditStatus == null) return;
            lblEditStatus.Text = text;
            lblEditStatus.ForeColor = color;
        }
        private static readonly Regex NameRx = new Regex(@"^[\p{L}][\p{L} .'\-ñÑ]*$");
        private void SaveProfile()
        {
            if (txtFirst == null) return;
            string f = Clean(txtFirst.Text), m = Clean(txtMiddle.Text), l = Clean(txtLast.Text);
            string c = Regex.Replace(txtContact.Text ?? "", @"\D", "");
            var errors = new List<string>();
            Control? firstBad = null;
            void Bad(Control ctl, string msg) { errors.Add(msg); firstBad ??= ctl; ctl.BackColor = AppTheme.DangerSoft; }
            foreach (var t in new[] { txtFirst, txtMiddle, txtLast, txtContact }) t.BackColor = AppTheme.InputBack;
            if (f.Length == 0) Bad(txtFirst, "First name is required.");
            else if (!NameRx.IsMatch(f)) Bad(txtFirst, "First name can only have letters, spaces, periods, apostrophes and hyphens.");
            if (m.Length > 0 && !NameRx.IsMatch(m)) Bad(txtMiddle, "Middle name can only have letters, spaces, periods, apostrophes and hyphens.");
            if (l.Length == 0) Bad(txtLast, "Last name is required.");
            else if (!NameRx.IsMatch(l)) Bad(txtLast, "Last name can only have letters, spaces, periods, apostrophes and hyphens.");
            if (c.Length > 0 && !(c.Length == 11 && c.StartsWith("09"))) Bad(txtContact, "Contact number must be 11 digits starting with 09 (e.g. 09171234567).");
            if (errors.Count > 0)
            {
                SetEditStatus(errors[0], AppTheme.DangerText);
                firstBad?.Focus();
                return;
            }
            bool nameChanged = f != original.F || m != original.M || l != original.L;
            bool contactChanged = c != Regex.Replace(original.C, @"\D", "");
            if (!nameChanged && !contactChanged)
            {
                SetEditStatus("Nothing to save — no changes yet.", AppTheme.TextMuted);
                return;
            }
            if (MessageBox.Show(this, "Are you sure you want to update your profile?", "Confirm update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                SetEditStatus("Not saved.", AppTheme.TextMuted);
                return;
            }
            var updated = new User
            {
                Id = user.Id,
                Username = user.Username,
                Password = "",
                Role = user.Role,
                FirstName = f,
                MiddleName = m,
                LastName = l,
                FullName = nameChanged ? NameHelper.FormatFullName(l, f, m) : user.FullName,
                ContactNumber = c,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            try
            {
                MJdatabase.SaveUser(updated);
                if (nameChanged) MJdatabase.LogAuditTrail("PROFILE_UPDATE", "Users", user.Id, "@" + user.Username, "Name", DisplayName(user), $"{f} {(m.Length > 0 ? m + " " : "")}{l}", "Updated own profile");
                if (contactChanged) MJdatabase.LogAuditTrail("PROFILE_UPDATE", "Users", user.Id, "@" + user.Username, "ContactNumber", user.ContactNumber, c, "Updated own profile");
            }
            catch (Exception ex)
            {
                SetEditStatus("Could not save: " + ex.Message, AppTheme.DangerText);
                return;
            }
            user.FirstName = f; user.MiddleName = m; user.LastName = l; user.FullName = updated.FullName; user.ContactNumber = c;
            if (SessionContext.ActiveUser != null && SessionContext.ActiveUser.Id == user.Id && !ReferenceEquals(SessionContext.ActiveUser, user))
            {
                var a = SessionContext.ActiveUser;
                a.FirstName = f; a.MiddleName = m; a.LastName = l; a.FullName = updated.FullName; a.ContactNumber = c;
            }
            original = (f, m, l, c);
            txtContact.Text = c;
            ProfileChanged = true;
            header.Invalidate();
            LoadData();
            if (pages[0] != null) { body.Controls.Remove(pages[0]); pages[0].Dispose(); pages[0] = null!; }
            if (pages[3] != null) { body.Controls.Remove(pages[3]); pages[3].Dispose(); pages[3] = null!; }
            SetEditStatus("✓ Saved. Your new details show everywhere from now on.", AppTheme.SuccessText);
            try { ToastHelper.ShowToast(this, "Profile updated.", ToastType.Success); } catch { }
        }
        private static string Clean(string? s) => Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        // ---------------------------------------------------------------- security
        private Panel BuildSecurityPage()
        {
            var page = new StackPage(S(20));
            page.AddRow(SectionLabel("Security checkup", "Keep your password fresh and a recovery question set so you can get back in if you forget it."), S(8));
            var check = new FeedList();
            if (lastPasswordChange.HasValue)
            {
                int days = (int)(DateTime.Now - lastPasswordChange.Value).TotalDays;
                bool old = days > 90;
                check.Items.Add(new FeedItem(old ? "Password is getting old" : "Password", $"Last changed {lastPasswordChange.Value:MMMM d, yyyy}" + (old ? " — consider changing it." : ""), Ago(lastPasswordChange.Value), old ? AppTheme.Warning : AppTheme.Success));
            }
            else
                check.Items.Add(new FeedItem("Password never changed", "It is still the one set when this account was created. Changing it is recommended.", "", AppTheme.Warning));
            check.Items.Add(hasRecovery
                ? new FeedItem("Recovery question is set", "You can reset your password from the sign-in screen with your answer.", "Ready", AppTheme.Success)
                : new FeedItem("No recovery question", "If you forget your password you will need an administrator to reset it.", "Set it up", AppTheme.Danger));
            int failed7 = mySignIns.Count(a => (a.EventType == "LOGIN_FAILED" || a.EventType == "LOCKED_OUT") && a.Timestamp >= DateTime.Today.AddDays(-7));
            var lastFail = mySignIns.FirstOrDefault(a => a.EventType == "LOGIN_FAILED" || a.EventType == "LOCKED_OUT");
            check.Items.Add(failed7 == 0
                ? new FeedItem("No failed sign-ins this week", "Nobody has entered a wrong password for your account in the last 7 days.", "", AppTheme.Success)
                : new FeedItem($"{failed7} failed sign-in attempt{(failed7 == 1 ? "" : "s")} this week", $"Latest on {lastFail?.IpOrMachine ?? "this PC"}, {lastFail?.Timestamp:MMM d h:mm tt}. If this wasn't you, change your password.", lastFail == null ? "" : Ago(lastFail.Timestamp), AppTheme.Danger));
            var current = mySignIns.FirstOrDefault(a => a.EventType == "LOGIN_SUCCESS");
            if (current != null)
                check.Items.Add(new FeedItem("Signed in now", $"Since {current.Timestamp:MMMM d, h:mm tt} on {current.IpOrMachine}", Ago(current.Timestamp), AppTheme.Info));
            check.FitHeight();
            page.AddRow(check, S(10));
            var bar = new Panel { Height = S(40), BackColor = AppTheme.Canvas };
            var btn = new Button { Text = "Change password or recovery question…", Width = S(300), Height = S(36), Location = new Point(0, 0), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btn, hasRecovery && lastPasswordChange.HasValue ? "secondary" : "primary");
            btn.Click += (s, e) =>
            {
                using (var dlg = new SecuritySettingsDialog(user)) dlg.ShowDialog(this);
                LoadData();
                RebuildPages();
            };

            bar.Controls.Add(btn);
            page.AddRow(bar, S(18));
            page.AddRow(SectionLabel("Recent sign-in activity", "The last 12 sign-in events for your account, newest first."), S(8));
            var feed = new FeedList { EmptyText = "No sign-in activity recorded yet." };
            foreach (var a in mySignIns.Take(12))
            {
                var (title, color) = LoginEvent(a.EventType);
                string where = string.IsNullOrWhiteSpace(a.IpOrMachine) ? "" : "on " + a.IpOrMachine;
                feed.Items.Add(new FeedItem(title, $"{a.Timestamp:ddd, MMM d yyyy · h:mm tt}  {where}", Ago(a.Timestamp), color));
            }
            feed.FitHeight();
            page.AddRow(feed, 0);
            return page;
        }
        private static (string, Color) LoginEvent(string? type) => (type ?? "") switch
        {
            "LOGIN_SUCCESS" => ("Signed in", AppTheme.Success),
            "LOGOUT" => ("Signed out", AppTheme.BorderStrong),
            "LOGIN_FAILED" => ("Wrong password entered", AppTheme.Danger),
            "LOCKED_OUT" => ("Locked out after failed attempts", AppTheme.Danger),
            "LOGIN_BLOCKED_INACTIVE" => ("Sign-in blocked — account inactive", AppTheme.Danger),
            "LOGIN_BLOCKED_PENDING" => ("Sign-in blocked — awaiting approval", AppTheme.Warning),
            "PASSWORD_RESET_SUCCESS" => ("Password reset with recovery question", AppTheme.Warning),
            "PASSWORD_RESET_FAILED" => ("Failed password reset attempt", AppTheme.Danger),
            "THEME_CHANGED" => ("Changed appearance settings", AppTheme.Info),
            "ACCOUNT_REGISTERED" => ("Account registered", AppTheme.Info),
            "USER_APPROVED" => ("Account approved", AppTheme.Success),
            _ => (Humanize(type), AppTheme.Info)
        };
        // ---------------------------------------------------------------- activity
        private Panel BuildActivityPage()
        {
            var page = new StackPage(S(20));
            DateTime today = DateTime.Today;
            var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var row = new TileRow(S(10)) { Height = S(92) };
            row.Add(new StatTile("Today", AppTheme.Accent, "changes saved") { Value = myActions.Count(a => a.Timestamp.Date == today).ToString("N0") });
            row.Add(new StatTile("This week", AppTheme.Info, "since Monday") { Value = myActions.Count(a => a.Timestamp >= weekStart).ToString("N0") });
            row.Add(new StatTile("This month", AppTheme.Success, today.ToString("MMMM")) { Value = myActions.Count(a => a.Timestamp >= new DateTime(today.Year, today.Month, 1)).ToString("N0") });
            var top = myActions.Where(a => a.Timestamp >= today.AddDays(-30)).GroupBy(a => Friendly(a.TableName)).OrderByDescending(g => g.Count()).FirstOrDefault();
            row.Add(new StatTile("Most worked on", AppTheme.Warning, top == null ? "last 30 days" : $"{top.Count()} changes · 30 days") { Value = top?.Key ?? "—" });
            page.AddRow(row, S(18));
            page.AddRow(SectionLabel("Your recent changes", "The last 30 records you added, edited, completed or removed. Use it to retrace your steps."), S(8));
            var feed = new FeedList { EmptyText = "You haven't changed any records yet." };
            foreach (var a in myActions.Take(30))
            {
                string what = Humanize(a.ActionType) + " · " + Friendly(a.TableName);
                string detail = string.IsNullOrWhiteSpace(a.RecordIdentifier) ? "" : a.RecordIdentifier;
                if (!string.IsNullOrWhiteSpace(a.FieldName) && !string.IsNullOrWhiteSpace(a.NewValue) && a.NewValue != "********")
                    detail += (detail.Length > 0 ? " — " : "") + $"{a.FieldName}: {Short(a.OldValue)} → {Short(a.NewValue)}";
                else if (!string.IsNullOrWhiteSpace(a.ReasonOrNotes))
                    detail += (detail.Length > 0 ? " — " : "") + a.ReasonOrNotes;
                feed.Items.Add(new FeedItem(what, detail.Length == 0 ? a.Timestamp.ToString("MMM d, h:mm tt") : detail, Ago(a.Timestamp), ActionColor(a.ActionType)) { Tip = $"{a.Timestamp:dddd, MMMM d yyyy h:mm:ss tt}\n{detail}" });
            }
            feed.FitHeight();
            page.AddRow(feed, 0);
            return page;
        }
        private static string Short(string? s)
        {
            s = (s ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (s.Length == 0) return "(blank)";
            return s.Length > 32 ? s.Substring(0, 30) + "…" : s;
        }
        private static Color ActionColor(string? t)
        {
            t = (t ?? "").ToUpperInvariant();
            if (t.Contains("DELETE") || t.Contains("CANCEL") || t.Contains("VOID") || t.Contains("REJECT")) return AppTheme.Danger;
            if (t.Contains("ADD") || t.Contains("CREATE") || t.Contains("INSERT") || t.Contains("COMPLETE") || t.Contains("APPROVE") || t.Contains("CLONE")) return AppTheme.Success;
            if (t.Contains("RESCHEDULE") || t.Contains("PASSWORD") || t.Contains("SECURITY") || t.Contains("OVERDUE")) return AppTheme.Warning;
            return AppTheme.Info;
        }
        private static string Friendly(string? table) => (table ?? "") switch
        {
            "Deliveries" => "Dispatch",
            "LoadingSchedules" => "Loading order",
            "SpecialTrips" => "Special trip",
            "Invoices" => "Invoice",
            "CollectionReceipts" => "Collection",
            "StatementOfAccounts" or "StatementsOfAccount" => "Statement of account",
            "Clients" => "Client",
            "Users" => "User account",
            "TripReports" => "Trip report",
            "IncidentReports" => "Incident report",
            "Trucks" => "Truck",
            "Crew" or "CrewMembers" => "Crew",
            "TripCrewPay" => "Crew pay",
            "LookupCategories" => "Category",
            "RateMatrix" or "Rates" => "Rate",
            "DeliveryRoutes" or "Routes" => "Route",
            "" => "Record",
            _ => Regex.Replace(table!, "(?<=[a-z])(?=[A-Z])", " ")
        };
        private static string Humanize(string? code)
        {
            string s = (code ?? "").Replace('_', ' ').Trim().ToLowerInvariant();
            return s.Length == 0 ? "Change" : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
        // ---------------------------------------------------------------- footer
        private Panel BuildFooter()
        {
            var footer = new FooterPanel { Dock = DockStyle.Bottom, Height = S(64), Padding = new Padding(S(20), S(14), S(20), S(14)) };
            var btnClose = new Button { Text = "Close", Dock = DockStyle.Right, Width = S(100), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnClose, "primary");
            btnClose.Click += (s, e) => Close();
            CancelButton = btnClose;
            var btnLogout = new Button { Text = "Sign out", Dock = DockStyle.Left, Width = S(104), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnLogout, "danger");
            btnLogout.Click += (s, e) => { Close(); OnLogout?.Invoke(); };
            footer.Controls.Add(btnClose);
            if (SessionContext.IsAdminRole(user.Role))
            {
                footer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = S(10), BackColor = Color.Transparent });
                var btnUsers = new Button { Text = "Manage user accounts", Dock = DockStyle.Right, Width = S(180), Cursor = Cursors.Hand };
                UIThemeHelper.SetButtonRole(btnUsers, "secondary");
                btnUsers.Click += (s, e) => { Close(); OnNavigateToUserManagement?.Invoke(); };
                footer.Controls.Add(btnUsers);
            }
            footer.Controls.Add(btnLogout);
            return footer;
        }
        // ---------------------------------------------------------------- helpers
        private Label SectionLabel(string title, string sub)
        {
            var l = new SectionTitle(title, sub) { Height = S(44) };
            return l;
        }
        internal static string DisplayName(User u)
        {
            if (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName))
                return Clean(string.Join(" ", new[] { u.FirstName, u.MiddleName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))));
            string n = (u.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            if (n.Contains(','))
            {
                var p = n.Split(new[] { ',' }, 2);
                n = (p[1].Trim() + " " + p[0].Trim()).Trim();
            }
            return n.Length == 0 ? (u.Username ?? "") : n;
        }
        private static (string F, string M, string L) NameParts(User u)
        {
            if (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName))
                return ((u.FirstName ?? "").Trim(), (u.MiddleName ?? "").Trim(), (u.LastName ?? "").Trim());
            string n = (u.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            if (n.Contains(','))
            {
                var sp = NameHelper.SplitFullName(n);
                return (sp.FirstName, sp.MiddleName, sp.LastName);
            }
            var tok = n.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tok.Count == 0) return ("", "", "");
            if (tok.Count == 1) return (tok[0], "", "");
            string last = tok[^1];
            tok.RemoveAt(tok.Count - 1);
            string mid = "";
            if (tok.Count > 1 && (tok[^1].EndsWith(".") || tok[^1].Length <= 2)) { mid = tok[^1]; tok.RemoveAt(tok.Count - 1); }
            return (string.Join(" ", tok), mid, last);
        }
        internal static string Initials(User u)
        {
            var parts = DisplayName(u).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => !p.EndsWith(".")).ToList();
            if (parts.Count == 0) return "?";
            if (parts.Count == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }
        private static string Ago(DateTime t)
        {
            var d = DateTime.Now - t;
            if (d.TotalSeconds < 0) return t.ToString("MMM d");
            if (d.TotalMinutes < 1) return "just now";
            if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} min ago";
            if (d.TotalHours < 24) return $"{(int)d.TotalHours} hr ago";
            if (d.TotalDays < 2) return "yesterday";
            if (d.TotalDays < 7) return $"{(int)d.TotalDays} days ago";
            return t.Year == DateTime.Today.Year ? t.ToString("MMM d") : t.ToString("MMM d, yyyy");
        }
        private static string MemberFor(DateTime created)
        {
            int days = (int)(DateTime.Today - created.Date).TotalDays;
            if (days < 1) return "joined today";
            if (days < 31) return $"{days} day{(days == 1 ? "" : "s")}";
            int months = (DateTime.Today.Year - created.Year) * 12 + DateTime.Today.Month - created.Month;
            if (months < 12) return $"{months} month{(months == 1 ? "" : "s")}";
            int years = months / 12;
            return $"{years} year{(years == 1 ? "" : "s")}";
        }
        // ================================================================ controls
        private sealed class ProfileHeader : Control
        {
            private readonly User u;
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public DateTime? SignedInAt { get; set; }
            public ProfileHeader(User u)
            {
                this.u = u;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                using (var bg = new LinearGradientBrush(ClientRectangle, ThemeManager.Mix(AppTheme.Surface, AppTheme.Accent, AppTheme.IsDark ? 0.22 : 0.10), AppTheme.Surface, LinearGradientMode.Horizontal))
                    g.FillRectangle(bg, ClientRectangle);
                using (var line = new Pen(AppTheme.Border)) g.DrawLine(line, 0, Height - 1, Width, Height - 1);
                int pad = S(24), av = S(72), ay = (Height - av) / 2;
                using (var ring = new SolidBrush(AppTheme.Surface)) g.FillEllipse(ring, pad - S(3), ay - S(3), av + S(6), av + S(6));
                using (var ab = new SolidBrush(AppTheme.Accent)) g.FillEllipse(ab, pad, ay, av, av);
                using (var inf = new Font("Segoe UI Semibold", 20F))
                    TextRenderer.DrawText(g, Initials(u), inf, new Rectangle(pad, ay, av, av), AppTheme.OnAccent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                int x = pad + av + S(18);
                int right = Width - pad;
                // status pill on the right
                string status = u.IsActive ? "● Active" : "● Inactive";
                using var pf = new Font("Segoe UI Semibold", 8.75F);
                var ps = TextRenderer.MeasureText(status, pf);
                var pr = new Rectangle(right - ps.Width - S(16), ay + S(4), ps.Width + S(16), S(24));
                using (var path = Gfx.RoundedRect(pr, pr.Height / 2f))
                using (var pb = new SolidBrush(u.IsActive ? AppTheme.SuccessSoft : AppTheme.DangerSoft))
                    g.FillPath(pb, path);
                TextRenderer.DrawText(g, status, pf, pr, u.IsActive ? AppTheme.SuccessText : AppTheme.DangerText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (SignedInAt.HasValue)
                {
                    using var sf = new Font("Segoe UI", 8.5F);
                    string since = $"Signed in {SignedInAt.Value:h:mm tt}" + (SignedInAt.Value.Date == DateTime.Today ? "" : $", {SignedInAt.Value:MMM d}");
                    TextRenderer.DrawText(g, since, sf, new Rectangle(right - S(220), pr.Bottom + S(6), S(220), S(20)), AppTheme.TextMuted, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
                }
                int textRight = pr.Left - S(16);
                var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                using var nf = new Font("Segoe UI", 16F, FontStyle.Bold);
                int ny = ay - S(2);
                TextRenderer.DrawText(g, DisplayName(u), nf, new Rectangle(x, ny, textRight - x, S(34)), AppTheme.TextPrimary, flags);
                string role = string.IsNullOrWhiteSpace(u.Role) ? "Staff" : u.Role;
                using var rf = new Font("Segoe UI Semibold", 8.5F);
                var rs = TextRenderer.MeasureText(role, rf);
                var rr = new Rectangle(x, ny + S(38), rs.Width + S(16), S(22));
                using (var path = Gfx.RoundedRect(rr, S(6)))
                using (var rb = new SolidBrush(AppTheme.AccentSoftStrong))
                    g.FillPath(rb, path);
                TextRenderer.DrawText(g, role, rf, rr, AppTheme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using var mf = new Font("Segoe UI", 9F);
                TextRenderer.DrawText(g, $"@{u.Username}", mf, new Rectangle(rr.Right + S(10), rr.Top, textRight - rr.Right - S(10), rr.Height), AppTheme.TextMuted, flags | TextFormatFlags.VerticalCenter);
            }
        }
        private sealed class SegmentBar : Control
        {
            private readonly string[] labels;
            private int selected, hover = -1;
            public event EventHandler? SelectedChanged;
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public int Selected { get => selected; set { if (value == selected || value < 0 || value >= labels.Length) return; selected = value; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); } }
            public SegmentBar(string[] labels)
            {
                this.labels = labels;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            private Rectangle[] Slots()
            {
                var r = new Rectangle[labels.Length];
                int x = S(20);
                using var f = new Font("Segoe UI Semibold", 9.5F);
                for (int i = 0; i < labels.Length; i++)
                {
                    int w = TextRenderer.MeasureText(labels[i], f).Width + S(24);
                    r[i] = new Rectangle(x, 0, w, Height);
                    x += w + S(4);
                }
                return r;
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int h = Array.FindIndex(Slots(), r => r.Contains(e.Location));
                if (h != hover) { hover = h; Invalidate(); }
            }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                int h = Array.FindIndex(Slots(), r => r.Contains(e.Location));
                if (h >= 0) Selected = h;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Surface);
                using (var line = new Pen(AppTheme.Border)) g.DrawLine(line, 0, Height - 1, Width, Height - 1);
                var slots = Slots();
                using var f = new Font("Segoe UI Semibold", 9.5F);
                for (int i = 0; i < labels.Length; i++)
                {
                    var r = slots[i];
                    bool on = i == selected;
                    if (i == hover && !on)
                        using (var hb = new SolidBrush(AppTheme.SurfaceHover))
                        using (var p = Gfx.RoundedRect(new Rectangle(r.X, S(7), r.Width, Height - S(14)), S(6)))
                            g.FillPath(hb, p);
                    TextRenderer.DrawText(g, labels[i], f, r, on ? AppTheme.AccentText : AppTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    if (on)
                        using (var ab = new SolidBrush(AppTheme.Accent))
                            g.FillRectangle(ab, r.X + S(8), Height - S(3), r.Width - S(16), S(3));
                }
            }

        }
        private sealed class FooterPanel : Panel
        {
            public FooterPanel() { BackColor = AppTheme.Surface; DoubleBuffered = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using var p = new Pen(AppTheme.Border);
                e.Graphics.DrawLine(p, 0, 0, Width, 0);
            }
        }
        /// <summary>Scrollable page that stacks its rows top to bottom at full width.</summary>
        private sealed class StackPage : Panel
        {
            private readonly int pad;
            private readonly List<(Control C, int Gap)> rows = new List<(Control, int)>();
            private bool laying;
            public StackPage(int pad)
            {
                this.pad = pad;
                Dock = DockStyle.Fill;
                AutoScroll = true;
                BackColor = AppTheme.Canvas;
            }
            public void AddRow(Control c, int gapAfter)
            {
                rows.Add((c, gapAfter));
                Controls.Add(c);
                c.SizeChanged += (s, e) => { if (!laying) PerformLayout(); };
            }
            protected override void OnLayout(LayoutEventArgs levent)
            {
                if (laying) return;
                laying = true;
                try
                {
                    int w = Math.Max(100, ClientSize.Width - pad * 2);
                    int y = pad + AutoScrollPosition.Y;
                    foreach (var (c, gap) in rows)
                    {
                        if (c is FeedList fl) fl.Width = w;
                        c.SetBounds(pad, y, w, c.Height);
                        y += c.Height + gap;
                    }
                    int total = y - AutoScrollPosition.Y + pad;
                    if (AutoScrollMinSize.Height != total) AutoScrollMinSize = new Size(0, total);
                }
                finally { laying = false; }
            }
        }

        private sealed class TileRow : Panel
        {
            private readonly int gap;
            public readonly List<StatTile> Tiles = new List<StatTile>();
            public TileRow(int gap) { this.gap = gap; BackColor = AppTheme.Canvas; }
            public void Add(StatTile t) { t.Dock = DockStyle.None; t.Margin = Padding.Empty; Tiles.Add(t); Controls.Add(t); }
            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                if (Tiles.Count == 0) return;
                int w = (ClientSize.Width - gap * (Tiles.Count - 1)) / Tiles.Count;
                for (int i = 0; i < Tiles.Count; i++) Tiles[i].SetBounds(i * (w + gap), 0, i == Tiles.Count - 1 ? ClientSize.Width - i * (w + gap) : w, ClientSize.Height);
            }
        }
        private sealed class SectionTitle : Label
        {
            private readonly string title, sub;
            public SectionTitle(string title, string sub)
            {
                this.title = title; this.sub = sub;
                AutoSize = false;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(AppTheme.Canvas);
                var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                using var tf = new Font("Segoe UI Semibold", 11F);
                using var sf = new Font("Segoe UI", 8.75F);
                int h = TextRenderer.MeasureText(title, tf).Height;
                TextRenderer.DrawText(e.Graphics, title, tf, new Rectangle(0, 0, Width, h), AppTheme.TextPrimary, flags);
                TextRenderer.DrawText(e.Graphics, sub, sf, new Rectangle(0, h, Width, Height - h), AppTheme.TextMuted, flags);
            }
        }
        private sealed class FeedItem
        {
            public string Title, Sub, Right;
            public Color Dot;
            public bool Dim;
            public string? Tip;
            public FeedItem(string title, string sub, string right, Color dot, bool dim = false) { Title = title; Sub = sub; Right = right; Dot = dot; Dim = dim; }
        }
        /// <summary>A card with one row per item: a coloured dot, a title, a muted second line and a right-aligned note.</summary>
        private sealed class FeedList : Control
        {
            public readonly List<FeedItem> Items = new List<FeedItem>();
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string EmptyText { get; set; } = "Nothing here yet.";
            private readonly ToolTip tip = new ToolTip { InitialDelay = 400 };
            private int hover = -1;
            public FeedList()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            private int RowH => S(54);
            public void FitHeight() => Height = Math.Max(1, Items.Count) * RowH + 2;
            protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); FitHeight(); }
            protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); FitHeight(); }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int h = Items.Count == 0 ? -1 : Math.Min(Items.Count - 1, Math.Max(0, (e.Y - 1) / RowH));
                if (h != hover)
                {
                    hover = h;
                    Invalidate();
                    tip.SetToolTip(this, h >= 0 ? (Items[h].Tip ?? (Items[h].Title + "\n" + Items[h].Sub)) : "");
                }
            }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
            protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Canvas);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = Gfx.RoundedRect(card, S(10)))
                {
                    using var b = new SolidBrush(AppTheme.Surface);
                    g.FillPath(b, path);
                    g.SetClip(path);
                    if (Items.Count == 0)
                    {
                        using var ef = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                        TextRenderer.DrawText(g, EmptyText, ef, card, AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    }
                    using var tf = new Font("Segoe UI Semibold", 9.5F);
                    using var sf = new Font("Segoe UI", 8.75F);
                    using var sep = new Pen(AppTheme.Border);
                    var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var it = Items[i];
                        int y = 1 + i * RowH;
                        if (i == hover)
                            using (var hb = new SolidBrush(AppTheme.SurfaceHover)) g.FillRectangle(hb, 1, y, Width - 2, RowH);
                        if (i > 0) g.DrawLine(sep, S(16), y, Width - S(16), y);
                        int dot = S(10);
                        using (var db = new SolidBrush(it.Dot)) g.FillEllipse(db, S(18), y + (RowH - dot) / 2, dot, dot);
                        int x = S(40);
                        int rw = 0;
                        if (!string.IsNullOrEmpty(it.Right))
                        {
                            rw = TextRenderer.MeasureText(it.Right, sf).Width + S(8);
                            TextRenderer.DrawText(g, it.Right, sf, new Rectangle(Width - S(16) - rw, y, rw, RowH), it.Dim ? AppTheme.TextMuted : AppTheme.TextSecondary, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                        }
                        int tw = Width - x - S(24) - rw;
                        TextRenderer.DrawText(g, it.Title, tf, new Rectangle(x, y + S(8), tw, S(20)), it.Dim ? AppTheme.TextMuted : AppTheme.TextPrimary, flags);
                        TextRenderer.DrawText(g, it.Sub, sf, new Rectangle(x, y + S(28), tw, S(18)), AppTheme.TextMuted, flags);
                    }
                    g.ResetClip();
                    using var pen = new Pen(AppTheme.Border);
                    g.DrawPath(pen, path);
                }
            }
        }
        /// <summary>Two-column card of label / value pairs.</summary>
        private sealed class InfoGrid : Control
        {
            private readonly List<(string K, string V, bool Muted)> pairs = new List<(string, string, bool)>();
            public InfoGrid() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            public void Add(string k, string v, bool muted = false) { pairs.Add((k, v, muted)); Fit(); }
            private void Fit() => Height = (pairs.Count + 1) / 2 * S(56) + S(16);
            protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Fit(); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.Canvas);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = Gfx.RoundedRect(card, S(10));
                using (var b = new SolidBrush(AppTheme.Surface)) g.FillPath(b, path);
                using (var pen = new Pen(AppTheme.Border)) g.DrawPath(pen, path);
                using var kf = new Font("Segoe UI", 8.5F);
                using var vf = new Font("Segoe UI Semibold", 10F);
                var flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                int colW = (Width - S(40)) / 2;
                for (int i = 0; i < pairs.Count; i++)
                {
                    int col = i % 2, row = i / 2;
                    int x = S(20) + col * colW, y = S(8) + row * S(56);
                    TextRenderer.DrawText(g, pairs[i].K.ToUpperInvariant(), kf, new Rectangle(x, y + S(8), colW - S(12), S(18)), AppTheme.TextMuted, flags);
                    TextRenderer.DrawText(g, pairs[i].V, vf, new Rectangle(x, y + S(26), colW - S(12), S(22)), pairs[i].Muted ? AppTheme.TextMuted : AppTheme.TextPrimary, flags);
                }
            }
        }
        /// <summary>Card holding labelled text boxes on a two-column grid.</summary>
        private sealed class FormCard : Panel
        {
            private readonly List<(Label L, TextBox T, int Col, int Row)> fields = new List<(Label, TextBox, int, int)>();
            public FormCard() { BackColor = AppTheme.Canvas; DoubleBuffered = true; ResizeRedraw = true; }
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            public TextBox Field(string label, string value, int col, int row, string? cue = null, bool readOnly = false)
            {
                var l = new Label { Text = label, AutoSize = false, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9F), BackColor = AppTheme.Surface };
                var t = new TextBox { Text = value, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5F), ReadOnly = readOnly, TabStop = !readOnly };
                t.BackColor = readOnly ? AppTheme.SurfaceAlt : AppTheme.InputBack;
                t.ForeColor = readOnly ? AppTheme.TextMuted : AppTheme.TextPrimary;
                if (!string.IsNullOrEmpty(cue)) t.PlaceholderText = cue;
                if (!readOnly) t.TextChanged += (s, e) => t.BackColor = AppTheme.InputBack;
                fields.Add((l, t, col, row));
                Controls.Add(l);
                Controls.Add(t);
                PerformLayout();
                return t;
            }
            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                int pad = S(20), gap = S(16);
                int colW = (ClientSize.Width - pad * 2 - gap) / 2;
                foreach (var (l, t, col, row) in fields)
                {
                    int x = pad + col * (colW + gap), y = S(16) + row * S(74);
                    l.SetBounds(x, y, colW, S(20));
                    t.SetBounds(x, y + S(22), colW, t.PreferredHeight);
                }
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                Gfx.HighQuality(g);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = Gfx.RoundedRect(card, S(10));
                using (var b = new SolidBrush(AppTheme.Surface)) g.FillPath(b, path);
                using (var pen = new Pen(AppTheme.Border)) g.DrawPath(pen, path);
            }
        }
    }
}
