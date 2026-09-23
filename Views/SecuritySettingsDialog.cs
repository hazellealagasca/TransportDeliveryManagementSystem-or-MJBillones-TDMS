using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class SecuritySettingsDialog : Form
    {
        private User currentUser;
        private ComboBox cbQuestion = null!;
        private TextBox txtCustomQuestion = null!;
        private TextBox txtAnswer = null!;
        private TextBox txtNewPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private Label lblStatus = null!;

        public SecuritySettingsDialog(User user)
        {
            currentUser = user;
            this.Text = "🔒 Account Security & Recovery Settings";
            this.Size = new Size(520, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 16, 24, 16)
            };

            Label lblTitle = new Label
            {
                Text = "🔒 Security Question & Password",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 14),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Configure password recovery options for @{currentUser.Username}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 42),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.White,
                AutoScroll = true
            };

            Label lblQHeading = new Label
            {
                Text = "Security Recovery Question *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 4, 0, 4),
                AutoSize = true
            };

            cbQuestion = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 8)
            };
            cbQuestion.Items.AddRange(new object[]
            {
                "What is your mother's maiden name?",
                "What was the name of your first pet?",
                "What elementary school did you attend?",
                "What is your favorite food or dish?",
                "What city or town were you born in?",
                "Write my own custom security question..."
            });
            cbQuestion.SelectedIndex = 0;

            txtCustomQuestion = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Enter your custom security question here...",
                Visible = false,
                Margin = new Padding(0, 6, 0, 8)
            };

            cbQuestion.SelectedIndexChanged += (s, e) =>
            {
                txtCustomQuestion.Visible = cbQuestion.SelectedIndex == cbQuestion.Items.Count - 1;
            };

            Label lblAnsHeading = new Label
            {
                Text = "Security Answer (Case-Insensitive) *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 10, 0, 4),
                AutoSize = true
            };

            txtAnswer = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Enter secret answer used for account recovery...",
                Margin = new Padding(0, 0, 0, 12)
            };

            Label lblPassHeading = new Label
            {
                Text = "Change Password (Leave blank to keep current password)",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 14, 0, 4),
                AutoSize = true
            };

            txtNewPassword = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "New Password (optional)",
                PasswordChar = '●',
                Margin = new Padding(0, 0, 0, 6)
            };

            txtConfirmPassword = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Confirm New Password",
                PasswordChar = '●',
                Margin = new Padding(0, 6, 0, 8)
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 38, 38),
                Padding = new Padding(0, 8, 0, 4),
                AutoSize = true,
                Text = ""
            };

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 12, 16, 12)
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            Button btnSave = new Button
            {
                Text = "Save Security Settings",
                Dock = DockStyle.Right,
                Width = 175,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            pnlBody.Controls.Add(lblStatus);
            pnlBody.Controls.Add(txtConfirmPassword);
            pnlBody.Controls.Add(txtNewPassword);
            pnlBody.Controls.Add(lblPassHeading);
            pnlBody.Controls.Add(txtAnswer);
            pnlBody.Controls.Add(lblAnsHeading);
            pnlBody.Controls.Add(txtCustomQuestion);
            pnlBody.Controls.Add(cbQuestion);
            pnlBody.Controls.Add(lblQHeading);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            string? existingQ = MJdatabase.GetRawUserSecurityQuestion(currentUser.Username) ?? (!string.IsNullOrWhiteSpace(currentUser.SecurityQuestion) ? currentUser.SecurityQuestion : null);
            if (!string.IsNullOrWhiteSpace(existingQ))
            {
                int idx = cbQuestion.Items.IndexOf(existingQ);
                if (idx >= 0)
                {
                    cbQuestion.SelectedIndex = idx;
                }
                else
                {
                    cbQuestion.SelectedIndex = cbQuestion.Items.Count - 1;
                    txtCustomQuestion.Text = existingQ;
                    txtCustomQuestion.Visible = true;
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string selectedQ = cbQuestion.SelectedItem?.ToString() ?? "";
            if (cbQuestion.SelectedIndex == cbQuestion.Items.Count - 1)
            {
                selectedQ = txtCustomQuestion.Text.Trim();
                if (string.IsNullOrWhiteSpace(selectedQ))
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = "Please enter your custom security question.";
                    return;
                }
            }

            string answer = txtAnswer.Text.Trim();
            if (string.IsNullOrWhiteSpace(answer))
            {
                lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                lblStatus.Text = "Please provide an answer to your security question.";
                return;
            }

            string newPass = txtNewPassword.Text.Trim();
            string confirmPass = txtConfirmPassword.Text.Trim();

            if (!string.IsNullOrEmpty(newPass))
            {
                if (newPass.Length < 4)
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = "New password must be at least 4 characters long.";
                    return;
                }
                if (newPass != confirmPass)
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = "New password and confirmation do not match.";
                    return;
                }
            }

            bool qSuccess = MJdatabase.UpdateUserSecurityQuestion(currentUser.Id, selectedQ, answer);
            if (!qSuccess)
            {
                lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                lblStatus.Text = "Failed to update security question in database.";
                return;
            }

            if (!string.IsNullOrEmpty(newPass))
            {
                bool pSuccess = MJdatabase.UpdateUserPassword(currentUser.Id, newPass);
                if (!pSuccess)
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = "Security question updated, but password change failed.";
                    return;
                }
            }

            currentUser.SecurityQuestion = selectedQ;
            currentUser.SecurityAnswer = answer;

            EventNotificationService.Publish("Security Updated", $"Security settings updated for @{currentUser.Username}", ToastType.Success, this);
            MessageBox.Show("Security settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
