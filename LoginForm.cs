using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class LoginForm : Form
    {
        private int failedAttempts = 0;
        private int lockoutSecondsRemaining = 0;
        private System.Windows.Forms.Timer? lockoutTimer;

        public LoginForm()
        {
            InitializeComponent();
            MJdatabase.InitializeDatabase();

            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    picLogo.ImageLocation = logoPath;
                    using (Bitmap bmp = new Bitmap(logoPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                    }
                }
            }
            catch { }

            this.AcceptButton = btnLogin;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (lockoutSecondsRemaining > 0)
                return;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter both Username and Password.";
                lblError.Visible = true;
                return;
            }

            var auth = MJdatabase.AuthenticateUserDetailed(username, password);

            if (auth.Result == AuthResult.Success && auth.User != null)
            {
                failedAttempts = 0;
                MJdatabase.LogSecurityEvent(username, "LOGIN_SUCCESS", $"User successfully authenticated ({auth.User.Role})");

                lblError.Visible = false;
                this.Hide();
                MainForm mainForm = new MainForm(auth.User);
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            else if (auth.Result == AuthResult.PendingApproval)
            {
                MJdatabase.LogSecurityEvent(username, "LOGIN_BLOCKED_PENDING", "Pending staff user attempted to login");
                lblError.Text = "Account pending approval by Business Owner (Marie Ann J. Billones).";
                lblError.Visible = true;
            }
            else
            {
                failedAttempts++;
                MJdatabase.LogSecurityEvent(username, "LOGIN_FAILED", $"Invalid login attempt {failedAttempts} of 5");

                if (failedAttempts >= 5)
                {
                    StartLockout(60, username);
                }
                else
                {
                    lblError.Text = $"Invalid Username or Password. (Attempt {failedAttempts} of 5 before lockout)";
                    lblError.Visible = true;
                }
            }
        }

        private void StartLockout(int seconds, string username)
        {
            lockoutSecondsRemaining = seconds;
            btnLogin.Enabled = false;
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;

            MJdatabase.LogSecurityEvent(username, "LOCKED_OUT", "Account locked for 60 seconds due to 5 consecutive failed attempts");

            lblError.Text = $"🔒 Account locked due to multiple failed attempts. Retry in {lockoutSecondsRemaining}s...";
            lblError.Visible = true;

            if (lockoutTimer == null)
            {
                lockoutTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                lockoutTimer.Tick += LockoutTimer_Tick;
            }
            lockoutTimer.Start();
        }

        private void LockoutTimer_Tick(object? sender, EventArgs e)
        {
            lockoutSecondsRemaining--;
            if (lockoutSecondsRemaining <= 0)
            {
                lockoutTimer?.Stop();
                failedAttempts = 0;
                btnLogin.Enabled = true;
                txtUsername.Enabled = true;
                txtPassword.Enabled = true;
                lblError.Text = "Account unlocked. You may now attempt to log in.";
                lblError.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                lblError.ForeColor = Color.FromArgb(220, 38, 38);
                lblError.Text = $"🔒 Account locked due to multiple failed attempts. Retry in {lockoutSecondsRemaining}s...";
            }
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var dlg = new RegisterDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    lblError.ForeColor = Color.FromArgb(22, 101, 52);
                    lblError.Text = "Registration submitted! Awaiting Owner approval.";
                    lblError.Visible = true;
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
