import os

designer_code = """namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            pnlBorder = new Panel();
            pnlRightLogin = new Panel();
            pnlFormContent = new Panel();
            lblCopyright = new Label();
            btnLogin = new Button();
            pnlOptions = new Panel();
            lnkForgotPassword = new LinkLabel();
            chkRememberUsername = new CheckBox();
            pnlPassContainer = new Panel();
            txtPassword = new TextBox();
            btnTogglePassword = new Button();
            lblPassIcon = new Label();
            lblPassword = new Label();
            pnlUserContainer = new Panel();
            txtUsername = new TextBox();
            lblUserIcon = new Label();
            lblUsername = new Label();
            pnlAlert = new Panel();
            lblError = new Label();
            lblInstruction = new Label();
            lblWelcome = new Label();
            pnlWindowControls = new Panel();
            btnMinimize = new Button();
            btnClose = new Button();
            pnlLeftBrand = new Panel();
            lblSecurityBadge = new Label();
            pnlFeatures = new Panel();
            lblFeature3 = new Label();
            lblFeature2 = new Label();
            lblFeature1 = new Label();
            pnlBrandDivider = new Panel();
            lblBrandSubtitle = new Label();
            lblBrandTitle = new Label();
            picLogoCard = new Panel();
            picLogo = new PictureBox();
            pnlBorder.SuspendLayout();
            pnlRightLogin.SuspendLayout();
            pnlFormContent.SuspendLayout();
            pnlOptions.SuspendLayout();
            pnlPassContainer.SuspendLayout();
            pnlUserContainer.SuspendLayout();
            pnlAlert.SuspendLayout();
            pnlWindowControls.SuspendLayout();
            pnlLeftBrand.SuspendLayout();
            pnlFeatures.SuspendLayout();
            picLogoCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            SuspendLayout();
            pnlBorder.BackColor = Color.White;
            pnlBorder.Controls.Add(pnlRightLogin);
            pnlBorder.Controls.Add(pnlLeftBrand);
            pnlBorder.Dock = DockStyle.Fill;
            pnlBorder.Location = new Point(1, 1);
            pnlBorder.Name = "pnlBorder";
            pnlBorder.Size = new Size(758, 478);
            pnlBorder.TabIndex = 0;
            pnlRightLogin.BackColor = Color.White;
            pnlRightLogin.Controls.Add(pnlFormContent);
            pnlRightLogin.Controls.Add(pnlWindowControls);
            pnlRightLogin.Dock = DockStyle.Fill;
            pnlRightLogin.Location = new Point(310, 0);
            pnlRightLogin.Name = "pnlRightLogin";
            pnlRightLogin.Size = new Size(448, 478);
            pnlRightLogin.TabIndex = 1;
            pnlFormContent.Controls.Add(lblCopyright);
            pnlFormContent.Controls.Add(btnLogin);
            pnlFormContent.Controls.Add(pnlOptions);
            pnlFormContent.Controls.Add(pnlPassContainer);
            pnlFormContent.Controls.Add(lblPassword);
            pnlFormContent.Controls.Add(pnlUserContainer);
            pnlFormContent.Controls.Add(lblUsername);
            pnlFormContent.Controls.Add(pnlAlert);
            pnlFormContent.Controls.Add(lblInstruction);
            pnlFormContent.Controls.Add(lblWelcome);
            pnlFormContent.Dock = DockStyle.Fill;
            pnlFormContent.Location = new Point(0, 36);
            pnlFormContent.Name = "pnlFormContent";
            pnlFormContent.Padding = new Padding(36, 6, 36, 16);
            pnlFormContent.Size = new Size(448, 442);
            pnlFormContent.TabIndex = 1;
            lblCopyright.Dock = DockStyle.Bottom;
            lblCopyright.Font = new Font("Segoe UI", 8F);
            lblCopyright.ForeColor = Color.FromArgb(148, 163, 184);
            lblCopyright.Location = new Point(36, 404);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(376, 22);
            lblCopyright.TabIndex = 9;
            lblCopyright.Text = "© M.J. Billones Transport Service • All rights reserved.";
            lblCopyright.TextAlign = ContentAlignment.MiddleCenter;
            btnLogin.BackColor = Color.FromArgb(185, 28, 28);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Dock = DockStyle.Top;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(36, 332);
            btnLogin.Margin = new Padding(0, 10, 0, 0);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(376, 42);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "LOG IN";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            pnlOptions.Controls.Add(lnkForgotPassword);
            pnlOptions.Controls.Add(chkRememberUsername);
            pnlOptions.Dock = DockStyle.Top;
            pnlOptions.Location = new Point(36, 296);
            pnlOptions.Name = "pnlOptions";
            pnlOptions.Padding = new Padding(0, 6, 0, 0);
            pnlOptions.Size = new Size(376, 36);
            pnlOptions.TabIndex = 6;
            lnkForgotPassword.ActiveLinkColor = Color.FromArgb(185, 28, 28);
            lnkForgotPassword.AutoSize = true;
            lnkForgotPassword.Cursor = Cursors.Hand;
            lnkForgotPassword.Dock = DockStyle.Right;
            lnkForgotPassword.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lnkForgotPassword.LinkColor = Color.FromArgb(37, 99, 235);
            lnkForgotPassword.Location = new Point(275, 6);
            lnkForgotPassword.Name = "lnkForgotPassword";
            lnkForgotPassword.Size = new Size(101, 15);
            lnkForgotPassword.TabIndex = 1;
            lnkForgotPassword.TabStop = true;
            lnkForgotPassword.Text = "Forgot Password?";
            lnkForgotPassword.LinkClicked += lnkForgotPassword_LinkClicked;
            chkRememberUsername.AutoSize = true;
            chkRememberUsername.Cursor = Cursors.Hand;
            chkRememberUsername.Dock = DockStyle.Left;
            chkRememberUsername.Font = new Font("Segoe UI", 9F);
            chkRememberUsername.ForeColor = Color.FromArgb(71, 85, 105);
            chkRememberUsername.Location = new Point(0, 6);
            chkRememberUsername.Name = "chkRememberUsername";
            chkRememberUsername.Size = new Size(141, 30);
            chkRememberUsername.TabIndex = 0;
            chkRememberUsername.Text = "Remember Username";
            chkRememberUsername.UseVisualStyleBackColor = true;
            pnlPassContainer.BackColor = Color.White;
            pnlPassContainer.BorderStyle = BorderStyle.FixedSingle;
            pnlPassContainer.Controls.Add(txtPassword);
            pnlPassContainer.Controls.Add(btnTogglePassword);
            pnlPassContainer.Controls.Add(lblPassIcon);
            pnlPassContainer.Dock = DockStyle.Top;
            pnlPassContainer.Location = new Point(36, 260);
            pnlPassContainer.Name = "pnlPassContainer";
            pnlPassContainer.Padding = new Padding(8, 7, 6, 6);
            pnlPassContainer.Size = new Size(376, 36);
            pnlPassContainer.TabIndex = 5;
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.Font = new Font("Segoe UI", 10.5F);
            txtPassword.ForeColor = Color.FromArgb(15, 23, 42);
            txtPassword.Location = new Point(34, 7);
            txtPassword.MaxLength = 128;
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.PlaceholderText = "Enter your password";
            txtPassword.Size = new Size(304, 19);
            txtPassword.TabIndex = 0;
            btnTogglePassword.BackColor = Color.Transparent;
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.Dock = DockStyle.Right;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatAppearance.MouseDownBackColor = Color.FromArgb(241, 245, 249);
            btnTogglePassword.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI", 9F);
            btnTogglePassword.ForeColor = Color.FromArgb(100, 116, 139);
            btnTogglePassword.Location = new Point(338, 7);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Size = new Size(30, 21);
            btnTogglePassword.TabIndex = 2;
            btnTogglePassword.Text = "👁️";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += btnTogglePassword_Click;
            lblPassIcon.Dock = DockStyle.Left;
            lblPassIcon.Font = new Font("Segoe UI", 9.5F);
            lblPassIcon.ForeColor = Color.FromArgb(100, 116, 139);
            lblPassIcon.Location = new Point(8, 7);
            lblPassIcon.Name = "lblPassIcon";
            lblPassIcon.Size = new Size(26, 21);
            lblPassIcon.TabIndex = 1;
            lblPassIcon.Text = "🔒";
            lblPassIcon.TextAlign = ContentAlignment.MiddleLeft;
            lblPassword.Dock = DockStyle.Top;
            lblPassword.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblPassword.ForeColor = Color.FromArgb(51, 65, 85);
            lblPassword.Location = new Point(36, 232);
            lblPassword.Name = "lblPassword";
            lblPassword.Padding = new Padding(0, 8, 0, 2);
            lblPassword.Size = new Size(376, 28);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Password";
            pnlUserContainer.BackColor = Color.White;
            pnlUserContainer.BorderStyle = BorderStyle.FixedSingle;
            pnlUserContainer.Controls.Add(txtUsername);
            pnlUserContainer.Controls.Add(lblUserIcon);
            pnlUserContainer.Dock = DockStyle.Top;
            pnlUserContainer.Location = new Point(36, 196);
            pnlUserContainer.Name = "pnlUserContainer";
            pnlUserContainer.Padding = new Padding(8, 7, 8, 6);
            pnlUserContainer.Size = new Size(376, 36);
            pnlUserContainer.TabIndex = 3;
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.Font = new Font("Segoe UI", 10.5F);
            txtUsername.ForeColor = Color.FromArgb(15, 23, 42);
            txtUsername.Location = new Point(34, 7);
            txtUsername.MaxLength = 50;
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "Enter your username";
            txtUsername.Size = new Size(332, 19);
            txtUsername.TabIndex = 0;
            lblUserIcon.Dock = DockStyle.Left;
            lblUserIcon.Font = new Font("Segoe UI", 9.5F);
            lblUserIcon.ForeColor = Color.FromArgb(100, 116, 139);
            lblUserIcon.Location = new Point(8, 7);
            lblUserIcon.Name = "lblUserIcon";
            lblUserIcon.Size = new Size(26, 21);
            lblUserIcon.TabIndex = 1;
            lblUserIcon.Text = "👤";
            lblUserIcon.TextAlign = ContentAlignment.MiddleLeft;
            lblUsername.Dock = DockStyle.Top;
            lblUsername.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblUsername.ForeColor = Color.FromArgb(51, 65, 85);
            lblUsername.Location = new Point(36, 170);
            lblUsername.Name = "lblUsername";
            lblUsername.Padding = new Padding(0, 6, 0, 2);
            lblUsername.Size = new Size(376, 26);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Username";
            pnlAlert.BackColor = Color.FromArgb(254, 242, 242);
            pnlAlert.BorderStyle = BorderStyle.FixedSingle;
            pnlAlert.Controls.Add(lblError);
            pnlAlert.Dock = DockStyle.Top;
            pnlAlert.Location = new Point(36, 60);
            pnlAlert.Margin = new Padding(0, 4, 0, 8);
            pnlAlert.Name = "pnlAlert";
            pnlAlert.Padding = new Padding(6, 4, 6, 4);
            pnlAlert.Size = new Size(376, 110);
            pnlAlert.TabIndex = 8;
            pnlAlert.Visible = false;
            lblError.Dock = DockStyle.Fill;
            lblError.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblError.ForeColor = Color.FromArgb(185, 28, 28);
            lblError.Location = new Point(6, 4);
            lblError.Name = "lblError";
            lblError.Size = new Size(362, 100);
            lblError.TabIndex = 0;
            lblError.Text = "Invalid Username or Password";
            lblError.TextAlign = ContentAlignment.MiddleLeft;
            lblInstruction.Dock = DockStyle.Top;
            lblInstruction.Font = new Font("Segoe UI", 9F);
            lblInstruction.ForeColor = Color.FromArgb(100, 116, 139);
            lblInstruction.Location = new Point(36, 36);
            lblInstruction.Name = "lblInstruction";
            lblInstruction.Size = new Size(376, 24);
            lblInstruction.TabIndex = 1;
            lblInstruction.Text = "Please enter your credentials to access the system.";
            lblWelcome.Dock = DockStyle.Top;
            lblWelcome.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblWelcome.ForeColor = Color.FromArgb(15, 23, 42);
            lblWelcome.Location = new Point(36, 6);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(376, 30);
            lblWelcome.TabIndex = 0;
            lblWelcome.Text = "Welcome Back";
            pnlWindowControls.BackColor = Color.White;
            pnlWindowControls.Controls.Add(btnMinimize);
            pnlWindowControls.Controls.Add(btnClose);
            pnlWindowControls.Dock = DockStyle.Top;
            pnlWindowControls.Location = new Point(0, 0);
            pnlWindowControls.Name = "pnlWindowControls";
            pnlWindowControls.Size = new Size(448, 36);
            pnlWindowControls.TabIndex = 0;
            btnMinimize.BackColor = Color.Transparent;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Dock = DockStyle.Right;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMinimize.ForeColor = Color.FromArgb(100, 116, 139);
            btnMinimize.Location = new Point(372, 0);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(38, 36);
            btnMinimize.TabIndex = 1;
            btnMinimize.Text = "—";
            btnMinimize.UseVisualStyleBackColor = false;
            btnMinimize.Click += btnMinimize_Click;
            btnClose.BackColor = Color.Transparent;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Dock = DockStyle.Right;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Location = new Point(410, 0);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(38, 36);
            btnClose.TabIndex = 0;
            btnClose.Text = "✕";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            pnlLeftBrand.BackColor = Color.FromArgb(15, 23, 42);
            pnlLeftBrand.Controls.Add(lblSecurityBadge);
            pnlLeftBrand.Controls.Add(pnlFeatures);
            pnlLeftBrand.Controls.Add(pnlBrandDivider);
            pnlLeftBrand.Controls.Add(lblBrandSubtitle);
            pnlLeftBrand.Controls.Add(lblBrandTitle);
            pnlLeftBrand.Controls.Add(picLogoCard);
            pnlLeftBrand.Dock = DockStyle.Left;
            pnlLeftBrand.Location = new Point(0, 0);
            pnlLeftBrand.Name = "pnlLeftBrand";
            pnlLeftBrand.Padding = new Padding(24, 32, 24, 20);
            pnlLeftBrand.Size = new Size(310, 478);
            pnlLeftBrand.TabIndex = 0;
            lblSecurityBadge.Dock = DockStyle.Bottom;
            lblSecurityBadge.Font = new Font("Segoe UI", 8F);
            lblSecurityBadge.ForeColor = Color.FromArgb(100, 116, 139);
            lblSecurityBadge.Location = new Point(24, 418);
            lblSecurityBadge.Name = "lblSecurityBadge";
            lblSecurityBadge.Size = new Size(262, 40);
            lblSecurityBadge.TabIndex = 5;
            lblSecurityBadge.Text = "🔒 256-Bit Encrypted Portal\\nAuthorized Personnel Only";
            lblSecurityBadge.TextAlign = ContentAlignment.MiddleCenter;
            pnlFeatures.Controls.Add(lblFeature3);
            pnlFeatures.Controls.Add(lblFeature2);
            pnlFeatures.Controls.Add(lblFeature1);
            pnlFeatures.Dock = DockStyle.Top;
            pnlFeatures.Location = new Point(24, 226);
            pnlFeatures.Name = "pnlFeatures";
            pnlFeatures.Padding = new Padding(4, 12, 4, 0);
            pnlFeatures.Size = new Size(262, 120);
            pnlFeatures.TabIndex = 4;
            lblFeature3.Dock = DockStyle.Top;
            lblFeature3.Font = new Font("Segoe UI", 8.5F);
            lblFeature3.ForeColor = Color.FromArgb(203, 213, 225);
            lblFeature3.Location = new Point(4, 76);
            lblFeature3.Name = "lblFeature3";
            lblFeature3.Size = new Size(254, 32);
            lblFeature3.TabIndex = 2;
            lblFeature3.Text = "✦  End-to-End Billing & Trip Dossier";
            lblFeature3.TextAlign = ContentAlignment.MiddleLeft;
            lblFeature2.Dock = DockStyle.Top;
            lblFeature2.Font = new Font("Segoe UI", 8.5F);
            lblFeature2.ForeColor = Color.FromArgb(203, 213, 225);
            lblFeature2.Location = new Point(4, 44);
            lblFeature2.Name = "lblFeature2";
            lblFeature2.Size = new Size(254, 32);
            lblFeature2.TabIndex = 1;
            lblFeature2.Text = "✦  Store Crate Breakdown & Audit Log";
            lblFeature2.TextAlign = ContentAlignment.MiddleLeft;
            lblFeature1.Dock = DockStyle.Top;
            lblFeature1.Font = new Font("Segoe UI", 8.5F);
            lblFeature1.ForeColor = Color.FromArgb(203, 213, 225);
            lblFeature1.Location = new Point(4, 12);
            lblFeature1.Name = "lblFeature1";
            lblFeature1.Size = new Size(254, 32);
            lblFeature1.TabIndex = 0;
            lblFeature1.Text = "✦  Automated Dispatch & Route Schedules";
            lblFeature1.TextAlign = ContentAlignment.MiddleLeft;
            pnlBrandDivider.BackColor = Color.FromArgb(51, 65, 85);
            pnlBrandDivider.Dock = DockStyle.Top;
            pnlBrandDivider.Location = new Point(24, 210);
            pnlBrandDivider.Margin = new Padding(0, 10, 0, 14);
            pnlBrandDivider.Name = "pnlBrandDivider";
            pnlBrandDivider.Size = new Size(262, 16);
            pnlBrandDivider.TabIndex = 3;
            lblBrandSubtitle.Dock = DockStyle.Top;
            lblBrandSubtitle.Font = new Font("Segoe UI", 8.5F);
            lblBrandSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblBrandSubtitle.Location = new Point(24, 186);
            lblBrandSubtitle.Name = "lblBrandSubtitle";
            lblBrandSubtitle.Size = new Size(262, 24);
            lblBrandSubtitle.TabIndex = 2;
            lblBrandSubtitle.Text = "Transport Delivery Management System";
            lblBrandSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            lblBrandTitle.Dock = DockStyle.Top;
            lblBrandTitle.Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold);
            lblBrandTitle.ForeColor = Color.White;
            lblBrandTitle.Location = new Point(24, 154);
            lblBrandTitle.Name = "lblBrandTitle";
            lblBrandTitle.Size = new Size(262, 32);
            lblBrandTitle.TabIndex = 1;
            lblBrandTitle.Text = "M.J. Billones Transport Service";
            lblBrandTitle.TextAlign = ContentAlignment.MiddleCenter;
            picLogoCard.BackColor = Color.White;
            picLogoCard.Controls.Add(picLogo);
            picLogoCard.Dock = DockStyle.Top;
            picLogoCard.Location = new Point(24, 32);
            picLogoCard.Name = "picLogoCard";
            picLogoCard.Padding = new Padding(8);
            picLogoCard.Size = new Size(262, 122);
            picLogoCard.TabIndex = 0;
            picLogo.Dock = DockStyle.Fill;
            picLogo.Image = Properties.Resources.logo;
            picLogo.Location = new Point(8, 8);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(246, 106);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(203, 213, 225);
            ClientSize = new Size(760, 480);
            Controls.Add(pnlBorder);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoginForm";
            Padding = new Padding(1);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "M.J. Billones Transport Service - System Login";
            pnlBorder.ResumeLayout(false);
            pnlRightLogin.ResumeLayout(false);
            pnlFormContent.ResumeLayout(false);
            pnlFormContent.PerformLayout();
            pnlOptions.ResumeLayout(false);
            pnlOptions.PerformLayout();
            pnlPassContainer.ResumeLayout(false);
            pnlPassContainer.PerformLayout();
            pnlUserContainer.ResumeLayout(false);
            pnlUserContainer.PerformLayout();
            pnlAlert.ResumeLayout(false);
            pnlWindowControls.ResumeLayout(false);
            pnlLeftBrand.ResumeLayout(false);
            pnlFeatures.ResumeLayout(false);
            picLogoCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlBorder;
        private Panel pnlLeftBrand;
        private Panel picLogoCard;
        private PictureBox picLogo;
        private Label lblBrandTitle;
        private Label lblBrandSubtitle;
        private Panel pnlBrandDivider;
        private Panel pnlFeatures;
        private Label lblFeature1;
        private Label lblFeature2;
        private Label lblFeature3;
        private Label lblSecurityBadge;
        private Panel pnlRightLogin;
        private Panel pnlWindowControls;
        private Button btnMinimize;
        private Button btnClose;
        private Panel pnlFormContent;
        private Label lblWelcome;
        private Label lblInstruction;
        private Panel pnlAlert;
        private Label lblError;
        private Label lblUsername;
        private Panel pnlUserContainer;
        private Label lblUserIcon;
        private TextBox txtUsername;
        private Label lblPassword;
        private Panel pnlPassContainer;
        private Label lblPassIcon;
        private TextBox txtPassword;
        private Button btnTogglePassword;
        private Panel pnlOptions;
        private CheckBox chkRememberUsername;
        private LinkLabel lnkForgotPassword;
        private Button btnLogin;
        private Label lblCopyright;
    }
}
"""

with open("LoginForm.Designer.cs", "w", encoding="utf-8") as f:
    f.write(designer_code)
print("LoginForm.Designer.cs written successfully.")
import os

login_form_cs = """using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class LoginForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private int failedAttempts = 0;
        private int lockoutSecondsRemaining = 0;
        private System.Windows.Forms.Timer? lockoutTimer;
        private bool isPasswordRevealed = false;

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
                    picLogo.Visible = true;
                    using (Bitmap bmp = new Bitmap(logoPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = (Icon)Icon.FromHandle(hIcon).Clone();
                        DestroyIcon(hIcon);
                    }
                }
                else if (picLogo.Image != null)
                {
                    picLogo.Visible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Logo init: {ex.Message}");
            }

            EnableWindowDragging(pnlWindowControls);
            EnableWindowDragging(pnlLeftBrand);
            EnableWindowDragging(lblWelcome);
            EnableWindowDragging(lblInstruction);

            SetupInputFocusEffects();
            LoadRememberedUsername();

            this.AcceptButton = btnLogin;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Application.Exit();
                }
            };
        }

        private void EnableWindowDragging(Control control)
        {
            control.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
        }

        private void SetupInputFocusEffects()
        {
            txtUsername.GotFocus += (s, e) => { pnlUserContainer.BackColor = Color.FromArgb(248, 250, 252); };
            txtUsername.LostFocus += (s, e) => { pnlUserContainer.BackColor = Color.White; };
            txtPassword.GotFocus += (s, e) => { pnlPassContainer.BackColor = Color.FromArgb(248, 250, 252); };
            txtPassword.LostFocus += (s, e) => { pnlPassContainer.BackColor = Color.White; };
        }

        private void LoadRememberedUsername()
        {
            try
            {
                string prefPath = GetUserPrefPath();
                if (System.IO.File.Exists(prefPath))
                {
                    string savedUser = System.IO.File.ReadAllText(prefPath).Trim();
                    if (!string.IsNullOrWhiteSpace(savedUser))
                    {
                        txtUsername.Text = savedUser;
                        chkRememberUsername.Checked = true;
                        this.ActiveControl = txtPassword;
                    }
                }
            }
            catch { }
        }

        private void SaveRememberedUsername(string username)
        {
            try
            {
                string prefPath = GetUserPrefPath();
                if (chkRememberUsername.Checked)
                {
                    string? dir = System.IO.Path.GetDirectoryName(prefPath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    System.IO.File.WriteAllText(prefPath, username);
                }
                else if (System.IO.File.Exists(prefPath))
                {
                    System.IO.File.Delete(prefPath);
                }
            }
            catch { }
        }

        private string GetUserPrefPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login_pref.dat");
        }

        private void ShowError(string message, bool isSuccess = false)
        {
            lblError.Text = message;
            if (isSuccess)
            {
                pnlAlert.BackColor = Color.FromArgb(240, 253, 244);
                lblError.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                pnlAlert.BackColor = Color.FromArgb(254, 242, 242);
                lblError.ForeColor = Color.FromArgb(185, 28, 28);
            }
            pnlAlert.Visible = true;
        }

        private void HideError()
        {
            pnlAlert.Visible = false;
        }

        private void btnTogglePassword_Click(object? sender, EventArgs e)
        {
            isPasswordRevealed = !isPasswordRevealed;
            txtPassword.PasswordChar = isPasswordRevealed ? '\\0' : '●';
            btnTogglePassword.Text = isPasswordRevealed ? "🔒" : "👁️";
        }

        private void btnMinimize_Click(object? sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnClose_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (lockoutSecondsRemaining > 0)
                    return;

                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("⚠️ Please enter both Username and Password.");
                    return;
                }

                var auth = MJdatabase.AuthenticateUserDetailed(username, password);

                if (auth.Result == AuthResult.Success && auth.User != null)
                {
                    failedAttempts = 0;
                    SaveRememberedUsername(username);
                    MJdatabase.LogSecurityEvent(username, "LOGIN_SUCCESS", $"User successfully authenticated ({auth.User.Role})");

                    HideError();
                    this.Hide();
                    MainForm mainForm = new MainForm(auth.User);
                    mainForm.FormClosed += (s, args) => this.Close();
                    mainForm.Show();
                }
                else if (auth.Result == AuthResult.PendingApproval)
                {
                    MJdatabase.LogSecurityEvent(username, "LOGIN_BLOCKED_PENDING", "Pending staff user attempted to login");
                    ShowError("⚠️ Account pending approval by Business Owner (Marie Ann J. Billones).");
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
                        ShowError($"⚠️ Invalid Username or Password. (Attempt {failedAttempts} of 5 before lockout)");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"⚠️ System/Database Error: {ex.Message}");
            }
        }

        private void StartLockout(int seconds, string username)
        {
            lockoutSecondsRemaining = seconds;
            btnLogin.Enabled = false;
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;

            MJdatabase.LogSecurityEvent(username, "LOCKED_OUT", "Account locked for 60 seconds due to 5 consecutive failed attempts");

            ShowError($"🔒 Account locked due to multiple failed attempts. Retry in {lockoutSecondsRemaining}s...");

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
                ShowError("Account unlocked. You may now attempt to log in.", isSuccess: true);
            }
            else
            {
                ShowError($"🔒 Account locked due to multiple failed attempts. Retry in {lockoutSecondsRemaining}s...");
            }
        }

        private void lnkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs? e)
        {
            try
            {
                using (var dlg = new Forms.ForgotPasswordDialog(txtUsername.Text.Trim()))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ShowError("Password reset successfully. Please enter your new password.", isSuccess: true);
                        txtPassword.Clear();
                        txtPassword.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open password recovery dialog: {ex.Message}", "Recovery Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
"""

with open("LoginForm.cs", "w", encoding="utf-8") as f:
    f.write(login_form_cs)
print("LoginForm.cs written successfully.")
