using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class UserProfileDialog : Form
    {
        public Action? OnNavigateToUserManagement;

        public UserProfileDialog(User user)
        {
            this.Text = "👤 User Profile & Account Credentials";
            this.Size = new Size(550, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 16, 24, 16)
            };

            Label lblAvatar = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 36F),
                ForeColor = Color.White,
                Location = new Point(20, 18),
                Size = new Size(80, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };

            string cleanName = (user.FullName ?? user.Username ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            Label lblName = new Label
            {
                Text = cleanName,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(105, 28),
                AutoSize = true
            };

            Label lblRole = new Label
            {
                Text = $"ROLE: {user.Role.ToUpper()}",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 205, 211),
                Location = new Point(105, 58),
                AutoSize = true
            };

            Label lblUsername = new Label
            {
                Text = $"Account: @{user.Username}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(105, 78),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblAvatar);
            pnlHeader.Controls.Add(lblName);
            pnlHeader.Controls.Add(lblRole);
            pnlHeader.Controls.Add(lblUsername);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.White
            };

            TableLayoutPanel tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true,
                Margin = new Padding(0)
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            AddInfoRow(tblInfo, 0, "Account ID:", $"#{user.Id:D4}");
            AddInfoRow(tblInfo, 1, "Account Status:", "Active / Authenticated ✓");
            AddInfoRow(tblInfo, 2, "Access Level:", user.Role == "Admin" || user.Role == "Owner/Admin" ? "Full Administrative & Management Privileges" : "Operational Dispatcher Access");
            AddInfoRow(tblInfo, 3, "Contact Number:", string.IsNullOrWhiteSpace(user.ContactNumber) ? "Not Specified" : user.ContactNumber);
            AddInfoRow(tblInfo, 4, "Account Created:", user.CreatedAt.ToString("MMMM dd, yyyy"));

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 12, 16, 12)
            };

            Button btnClose = new Button
            {
                Text = "CLOSE",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            Button btnSecurity = new Button
            {
                Text = "🔒 SECURITY SETTINGS",
                Dock = DockStyle.Left,
                Width = 175,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSecurity.FlatAppearance.BorderSize = 0;
            btnSecurity.Click += (s, e) =>
            {
                using (var secDlg = new SecuritySettingsDialog(user))
                {
                    secDlg.ShowDialog(this);
                }
            };
            pnlFooter.Controls.Add(btnSecurity);

            if (user.Role == "Admin" || user.Role == "Owner/Admin")
            {
                Button btnManage = new Button
                {
                    Text = "⚙️ USER ACCOUNTS",
                    Dock = DockStyle.Left,
                    Width = 160,
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnManage.FlatAppearance.BorderSize = 0;
                btnManage.Click += (s, e) =>
                {
                    this.Close();
                    OnNavigateToUserManagement?.Invoke();
                };
                pnlFooter.Controls.Add(btnManage);
            }

            pnlFooter.Controls.Add(btnClose);

            pnlBody.Controls.Add(tblInfo);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private void AddInfoRow(TableLayoutPanel tbl, int row, string label, string val)
        {
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            Label lblKey = new Label
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblVal = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            tbl.Controls.Add(lblKey, 0, row);
            tbl.Controls.Add(lblVal, 1, row);
        }
    }
}
