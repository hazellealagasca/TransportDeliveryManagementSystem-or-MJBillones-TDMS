using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class ForgotPasswordDialog : Form
    {
        private TextBox txtUsername = null!;
        private Button btnFindUser = null!;
        private Label lblQuestionPrompt = null!;
        private Label lblQuestionText = null!;
        private TextBox txtAnswer = null!;
        private TextBox txtNewPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private Button btnResetPassword = null!;
        private Label lblStatus = null!;
        private Panel pnlResetFields = null!;

        public ForgotPasswordDialog(string initialUsername = "")
        {
            this.Text = "🔑 Password Recovery & Account Reset";
            this.Size = new Size(480, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(24, 16, 24, 16)
            };

            Label lblTitle = new Label
            {
                Text = "🔑 Forgot Password Recovery",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.White,
                Location = new Point(20, 14),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Answer your secret security question to verify identity",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Pal.C(148, 163, 184),
                Location = new Point(20, 42),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Pal.White,
                AutoScroll = true
            };

            Label lblUserPrompt = new Label
            {
                Text = "Enter Your Username *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 4),
                AutoSize = true
            };

            TableLayoutPanel tblUserLookup = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 36,
                Margin = new Padding(0, 0, 0, 12)
            };
            tblUserLookup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tblUserLookup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            txtUsername = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Text = initialUsername,
                MaxLength = 50
            };

            btnFindUser = new Button
            {
                Text = "Verify User",
                Dock = DockStyle.Fill,
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnFindUser.FlatAppearance.BorderSize = 0;
            btnFindUser.Click += BtnFindUser_Click;

            tblUserLookup.Controls.Add(txtUsername, 0, 0);
            tblUserLookup.Controls.Add(btnFindUser, 1, 0);

            pnlResetFields = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Visible = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            lblQuestionPrompt = new Label
            {
                Text = "Security Question:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 4, 0, 2),
                AutoSize = true
            };

            lblQuestionText = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Pal.C(30, 58, 138),
                BackColor = Pal.C(239, 246, 255),
                Dock = DockStyle.Top,
                Padding = new Padding(8, 8, 8, 8),
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = true
            };

            Label lblAnsPrompt = new Label
            {
                Text = "Your Secret Security Answer *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 8, 0, 4),
                AutoSize = true
            };

            txtAnswer = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Enter answer to your security question...",
                Margin = new Padding(0, 0, 0, 8),
                MaxLength = 200
            };

            Label lblNewPassPrompt = new Label
            {
                Text = "New Password *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 8, 0, 4),
                AutoSize = true
            };

            txtNewPassword = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PasswordChar = '●',
                PlaceholderText = "Minimum 6 characters, alphanumeric",
                Margin = new Padding(0, 0, 0, 6),
                MaxLength = 128
            };

            Label lblConfirmPassPrompt = new Label
            {
                Text = "Confirm New Password *",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Padding = new Padding(0, 6, 0, 4),
                AutoSize = true
            };

            txtConfirmPassword = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PasswordChar = '●',
                PlaceholderText = "Re-type new password",
                Margin = new Padding(0, 0, 0, 10),
                MaxLength = 128
            };

            btnResetPassword = new Button
            {
                Text = "RESET PASSWORD",
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Pal.C(185, 28, 28),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 8)
            };
            btnResetPassword.FlatAppearance.BorderSize = 0;
            btnResetPassword.Click += BtnResetPassword_Click;

            pnlResetFields.Controls.Add(btnResetPassword);
            pnlResetFields.Controls.Add(txtConfirmPassword);
            pnlResetFields.Controls.Add(lblConfirmPassPrompt);
            pnlResetFields.Controls.Add(txtNewPassword);
            pnlResetFields.Controls.Add(lblNewPassPrompt);
            pnlResetFields.Controls.Add(txtAnswer);
            pnlResetFields.Controls.Add(lblAnsPrompt);
            pnlResetFields.Controls.Add(lblQuestionText);
            pnlResetFields.Controls.Add(lblQuestionPrompt);

            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Pal.C(220, 38, 38),
                Padding = new Padding(0, 8, 0, 4),
                AutoSize = true,
                Text = ""
            };

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(16, 10, 16, 10)
            };

            Button btnCancel = new Button
            {
                Text = "Close",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Pal.C(226, 232, 240),
                ForeColor = Pal.C(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnCancel);

            pnlBody.Controls.Add(lblStatus);
            pnlBody.Controls.Add(pnlResetFields);
            pnlBody.Controls.Add(tblUserLookup);
            pnlBody.Controls.Add(lblUserPrompt);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnFindUser_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            if (!string.IsNullOrEmpty(initialUsername))
            {
                BtnFindUser_Click(this, EventArgs.Empty);
            }
        }

        private void BtnFindUser_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = "Please enter your username to look up your account.";
                pnlResetFields.Visible = false;
                return;
            }

            string? question = MJdatabase.GetUserSecurityQuestion(username);
            if (question == null)
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = $"No account found matching username '{username}'.";
                pnlResetFields.Visible = false;
                return;
            }

            lblQuestionText.Text = question;
            pnlResetFields.Visible = true;
            lblStatus.ForeColor = Pal.C(22, 101, 52);
            lblStatus.Text = "Account verified. Please answer your security question below.";
            txtAnswer.Focus();
        }

        private void BtnResetPassword_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string answer = txtAnswer.Text.Trim();
            string newPass = txtNewPassword.Text.Trim();
            string confirmPass = txtConfirmPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(answer))
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = "Please enter the answer to your security question.";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6 || !newPass.Any(char.IsLetter) || !newPass.Any(char.IsDigit))
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = "New password must be at least 6 characters and contain letters and numbers.";
                return;
            }

            if (newPass != confirmPass)
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = "Password confirmation does not match.";
                return;
            }

            var result = MJdatabase.VerifySecurityAnswerAndResetPassword(username, answer, newPass);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Password Reset Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.ForeColor = Pal.C(220, 38, 38);
                lblStatus.Text = result.Message;
            }
        }
    }
}
