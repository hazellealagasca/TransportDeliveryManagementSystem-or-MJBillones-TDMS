using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public class RegisterDialog : Form
    {
        private TextBox txtLastName = new TextBox();
        private TextBox txtFirstName = new TextBox();
        private TextBox txtMiddleName = new TextBox();
        private TextBox txtContact = new TextBox();
        private TextBox txtUsername = new TextBox();
        private TextBox txtPassword = new TextBox();
        private TextBox txtConfirmPassword = new TextBox();
        private ComboBox cbSecurityQuestion = new ComboBox();
        private TextBox txtSecurityAnswer = new TextBox();
        private Button btnShowPassword = new Button();
        private Button btnSubmit = new Button();
        private Button btnCancel = new Button();
        private Label lblError = new Label();
        private bool isPasswordVisible = false;

        public RegisterDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Staff Self-Registration - M.J. Billones Transport Service";
            this.ClientSize = new Size(520, 680);
            this.MinimumSize = new Size(460, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(127, 29, 29),
                Padding = new Padding(20, 14, 20, 14)
            };

            Label lblTitle = new Label
            {
                Text = "📝 Staff Account Registration",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Dock = DockStyle.Top
            };

            Label lblSubtitle = new Label
            {
                Text = "Register a new Office Staff account for M.J. Billones Transport Service.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(254, 205, 211),
                AutoSize = true,
                Dock = DockStyle.Bottom
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = true
            };

            TableLayoutPanel tblNames = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };
            tblNames.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tblNames.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            tblNames.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            Label lblLastName = new Label { Text = "Last Name *", Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 4, 4) };
            Label lblFirstName = new Label { Text = "First Name *", Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(4, 0, 4, 4) };
            Label lblMiddleName = new Label { Text = "Middle Name", Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(4, 0, 0, 4) };

            txtLastName.Dock = DockStyle.Fill;
            txtLastName.Font = new Font("Segoe UI", 10F);
            txtLastName.Margin = new Padding(0, 0, 4, 8);

            txtFirstName.Dock = DockStyle.Fill;
            txtFirstName.Font = new Font("Segoe UI", 10F);
            txtFirstName.Margin = new Padding(4, 0, 4, 8);

            txtMiddleName.Dock = DockStyle.Fill;
            txtMiddleName.Font = new Font("Segoe UI", 10F);
            txtMiddleName.Margin = new Padding(4, 0, 0, 8);

            tblNames.Controls.Add(lblLastName, 0, 0);
            tblNames.Controls.Add(lblFirstName, 1, 0);
            tblNames.Controls.Add(lblMiddleName, 2, 0);
            tblNames.Controls.Add(txtLastName, 0, 1);
            tblNames.Controls.Add(txtFirstName, 1, 1);
            tblNames.Controls.Add(txtMiddleName, 2, 1);

            Label lblContact = new Label { Text = "Contact Mobile Number *", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };
            txtContact.Dock = DockStyle.Top;
            txtContact.Font = new Font("Segoe UI", 10F);
            txtContact.PlaceholderText = "e.g. 09171234567";
            txtContact.Margin = new Padding(0, 0, 0, 10);

            Label lblUsername = new Label { Text = "Desired Username *", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };
            txtUsername.Dock = DockStyle.Top;
            txtUsername.Font = new Font("Segoe UI", 10F);
            txtUsername.Margin = new Padding(0, 0, 0, 10);

            Label lblPassword = new Label { Text = "Password * (Minimum 6 characters, alphanumeric)", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };

            TableLayoutPanel tblPassword = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            tblPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));

            txtPassword.Dock = DockStyle.Fill;
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.PasswordChar = '●';
            txtPassword.Margin = new Padding(0, 0, 4, 0);

            btnShowPassword.Text = "👁️";
            btnShowPassword.Dock = DockStyle.Fill;
            btnShowPassword.FlatStyle = FlatStyle.Flat;
            btnShowPassword.Cursor = Cursors.Hand;
            btnShowPassword.Height = 28;
            btnShowPassword.Click += (s, e) =>
            {
                isPasswordVisible = !isPasswordVisible;
                txtPassword.PasswordChar = isPasswordVisible ? '\0' : '●';
                txtConfirmPassword.PasswordChar = isPasswordVisible ? '\0' : '●';
            };

            tblPassword.Controls.Add(txtPassword, 0, 0);
            tblPassword.Controls.Add(btnShowPassword, 1, 0);

            Label lblConfirmPassword = new Label { Text = "Confirm Password *", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };
            txtConfirmPassword.Dock = DockStyle.Top;
            txtConfirmPassword.Font = new Font("Segoe UI", 10F);
            txtConfirmPassword.PasswordChar = '●';
            txtConfirmPassword.Margin = new Padding(0, 0, 0, 10);

            Label lblQuestion = new Label { Text = "Security Question (for offline account recovery) *", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };
            cbSecurityQuestion.Dock = DockStyle.Top;
            cbSecurityQuestion.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSecurityQuestion.Font = new Font("Segoe UI", 10F);
            cbSecurityQuestion.Items.AddRange(new object[]
            {
                "What year was M.J. Billones Transport Service established?",
                "What is your primary delivery destination in Luzon?",
                "What was the name of your first childhood pet?",
                "What is your mother's maiden name?",
                "What was the model or brand of your first vehicle?"
            });
            cbSecurityQuestion.SelectedIndex = 0;
            cbSecurityQuestion.Margin = new Padding(0, 0, 0, 10);

            Label lblAnswer = new Label { Text = "Security Answer *", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Margin = new Padding(0, 0, 0, 4) };
            txtSecurityAnswer.Dock = DockStyle.Top;
            txtSecurityAnswer.Font = new Font("Segoe UI", 10F);
            txtSecurityAnswer.Margin = new Padding(0, 0, 0, 8);

            lblError.Dock = DockStyle.Top;
            lblError.AutoSize = true;
            lblError.ForeColor = Color.FromArgb(220, 38, 38);
            lblError.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblError.Visible = false;
            lblError.Margin = new Padding(0, 4, 0, 10);

            TableLayoutPanel tblActions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 44,
                Margin = new Padding(0, 10, 0, 0)
            };
            tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tblActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            btnSubmit.Text = "REGISTER ACCOUNT";
            btnSubmit.Dock = DockStyle.Fill;
            btnSubmit.BackColor = Color.FromArgb(185, 28, 28);
            btnSubmit.ForeColor = Color.White;
            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnSubmit.Cursor = Cursors.Hand;
            btnSubmit.Margin = new Padding(0, 0, 6, 0);
            btnSubmit.Click += BtnSubmit_Click;

            btnCancel.Text = "CANCEL";
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.BackColor = Color.FromArgb(241, 245, 249);
            btnCancel.ForeColor = Color.FromArgb(51, 65, 85);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Margin = new Padding(6, 0, 0, 0);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            tblActions.Controls.Add(btnSubmit, 0, 0);
            tblActions.Controls.Add(btnCancel, 1, 0);

            pnlBody.Controls.Add(tblActions);
            pnlBody.Controls.Add(lblError);
            pnlBody.Controls.Add(txtSecurityAnswer);
            pnlBody.Controls.Add(lblAnswer);
            pnlBody.Controls.Add(cbSecurityQuestion);
            pnlBody.Controls.Add(lblQuestion);
            pnlBody.Controls.Add(txtConfirmPassword);
            pnlBody.Controls.Add(lblConfirmPassword);
            pnlBody.Controls.Add(tblPassword);
            pnlBody.Controls.Add(lblPassword);
            pnlBody.Controls.Add(txtUsername);
            pnlBody.Controls.Add(lblUsername);
            pnlBody.Controls.Add(txtContact);
            pnlBody.Controls.Add(lblContact);
            pnlBody.Controls.Add(tblNames);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlHeader);
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string middleName = txtMiddleName.Text.Trim();
            string contact = txtContact.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string question = cbSecurityQuestion.SelectedItem?.ToString() ?? "";
            string answer = txtSecurityAnswer.Text.Trim();

            if (string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(contact) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(answer))
            {
                ShowError("Please complete all required fields (Last Name, First Name, Contact, Username, Password, Answer).");
                return;
            }

            if (!SecurityHelper.IsValidName(lastName))
            {
                ShowError("Please enter a valid Last Name (letters, spaces, hyphens, and apostrophes).");
                txtLastName.Focus();
                return;
            }

            if (!SecurityHelper.IsValidName(firstName))
            {
                ShowError("Please enter a valid First Name (letters, spaces, hyphens, and apostrophes).");
                txtFirstName.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(middleName) && !SecurityHelper.IsValidName(middleName))
            {
                ShowError("Please enter a valid Middle Name, or leave it blank.");
                txtMiddleName.Focus();
                return;
            }

            string fullName = NameHelper.FormatFullName(lastName, firstName, middleName);

            if (username.Length < 3)
            {
                ShowError("Username must be at least 3 characters long.");
                return;
            }

            if (password.Length < 6 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            {
                ShowError("Password must be at least 6 characters and contain letters and numbers.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            if (MJdatabase.IsUsernameTaken(username))
            {
                ShowError($"Username '{username}' is already taken. Please choose another.");
                return;
            }

            btnSubmit.Enabled = false;
            btnSubmit.Text = "SUBMITTING...";

            try
            {
                var newUser = new User
                {
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    FullName = fullName,
                    ContactNumber = contact,
                    Username = username,
                    Password = password,
                    Role = "Office Staff",
                    IsActive = false,
                    CreatedAt = DateTime.Now,
                    SecurityQuestion = question,
                    SecurityAnswer = answer
                };

                await MJdatabase.RegisterStaffUserAsync(newUser);

                MessageBox.Show(
                    "Registration submitted successfully!\n\n" +
                    "For company security, your account will be activated once reviewed and approved by the Business Owner (Marie Ann J. Billones).\n\n" +
                    "You may log in after approval.",
                    "Account Pending Approval",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("Registration error: " + ex.Message);
                btnSubmit.Enabled = true;
                btnSubmit.Text = "REGISTER ACCOUNT";
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }
    }
}
