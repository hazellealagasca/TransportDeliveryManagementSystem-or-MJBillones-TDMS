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
            pnlWindowControls = new Panel();
            btnMinimize = new Button();
            btnClose = new Button();
            pnlGlassCard = new Panel();
            pnlLogoBadge = new Panel();
            picLogo = new PictureBox();
            lblTitle = new Label();
            lblSubtitle = new Label();
            pnlAlert = new Panel();
            lblError = new Label();
            lblUsernameHeader = new Label();
            pnlUserContainer = new Panel();
            lblUserIcon = new Label();
            txtUsername = new TextBox();
            lblPasswordHeader = new Label();
            pnlPassContainer = new Panel();
            lblPassIcon = new Label();
            txtPassword = new TextBox();
            btnTogglePassword = new Button();
            pnlOptions = new Panel();
            chkRememberUsername = new CheckBox();
            lnkForgotPassword = new LinkLabel();
            btnLogin = new Button();
            lblAuthorized = new Label();
            lblCopyright = new Label();
            pnlWindowControls.SuspendLayout();
            pnlGlassCard.SuspendLayout();
            pnlLogoBadge.SuspendLayout();
            pnlAlert.SuspendLayout();
            pnlUserContainer.SuspendLayout();
            pnlPassContainer.SuspendLayout();
            pnlOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            SuspendLayout();
            pnlWindowControls.BackColor = Color.Transparent;
            pnlWindowControls.Controls.Add(btnMinimize);
            pnlWindowControls.Controls.Add(btnClose);
            pnlWindowControls.Location = new Point(700, 12);
            pnlWindowControls.Name = "pnlWindowControls";
            pnlWindowControls.Size = new Size(88, 36);
            pnlWindowControls.TabIndex = 10;
            btnMinimize.BackColor = Color.FromArgb(140, 15, 23, 42);
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 51, 65, 85);
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnMinimize.ForeColor = Color.White;
            btnMinimize.Location = new Point(4, 2);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(36, 30);
            btnMinimize.TabIndex = 1;
            btnMinimize.Text = "—";
            btnMinimize.UseVisualStyleBackColor = false;
            btnMinimize.Click += btnMinimize_Click;
            btnClose.BackColor = Color.FromArgb(140, 15, 23, 42);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 38, 38);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(46, 2);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(36, 30);
            btnClose.TabIndex = 0;
            btnClose.Text = "✕";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            pnlGlassCard.BackColor = Color.FromArgb(220, 15, 23, 42);
            pnlGlassCard.Controls.Add(pnlLogoBadge);
            pnlGlassCard.Controls.Add(lblTitle);
            pnlGlassCard.Controls.Add(lblSubtitle);
            pnlGlassCard.Controls.Add(pnlAlert);
            pnlGlassCard.Controls.Add(lblUsernameHeader);
            pnlGlassCard.Controls.Add(pnlUserContainer);
            pnlGlassCard.Controls.Add(lblPasswordHeader);
            pnlGlassCard.Controls.Add(pnlPassContainer);
            pnlGlassCard.Controls.Add(pnlOptions);
            pnlGlassCard.Controls.Add(btnLogin);
            pnlGlassCard.Controls.Add(lblAuthorized);
            pnlGlassCard.Controls.Add(lblCopyright);
            pnlGlassCard.Location = new Point(190, 20);
            pnlGlassCard.Name = "pnlGlassCard";
            pnlGlassCard.Size = new Size(410, 545);
            pnlGlassCard.TabIndex = 0;
            pnlLogoBadge.BackColor = Color.White;
            pnlLogoBadge.Controls.Add(picLogo);
            pnlLogoBadge.Location = new Point(90, 20);
            pnlLogoBadge.Name = "pnlLogoBadge";
            pnlLogoBadge.Padding = new Padding(10, 6, 10, 6);
            pnlLogoBadge.Size = new Size(230, 76);
            pnlLogoBadge.TabIndex = 0;
            picLogo.BackColor = Color.Transparent;
            picLogo.Dock = DockStyle.Fill;
            picLogo.Location = new Point(10, 6);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(210, 64);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(36, 104);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(338, 34);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "Login";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 9F);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblSubtitle.Location = new Point(36, 138);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(338, 20);
            lblSubtitle.TabIndex = 2;
            lblSubtitle.Text = "Welcome back! Please login to your account";
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlAlert.BackColor = Color.FromArgb(200, 88, 28, 28);
            pnlAlert.Controls.Add(lblError);
            pnlAlert.Location = new Point(36, 164);
            pnlAlert.Name = "pnlAlert";
            pnlAlert.Size = new Size(338, 30);
            pnlAlert.TabIndex = 3;
            pnlAlert.Visible = false;
            lblError.Dock = DockStyle.Fill;
            lblError.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
            lblError.ForeColor = Color.FromArgb(254, 202, 202);
            lblError.Location = new Point(0, 0);
            lblError.Name = "lblError";
            lblError.Size = new Size(338, 30);
            lblError.TabIndex = 0;
            lblError.Text = "Invalid username or password.";
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            lblUsernameHeader.AutoSize = true;
            lblUsernameHeader.BackColor = Color.Transparent;
            lblUsernameHeader.Font = new Font("Segoe UI", 8.5F);
            lblUsernameHeader.ForeColor = Color.FromArgb(203, 213, 225);
            lblUsernameHeader.Location = new Point(38, 200);
            lblUsernameHeader.Name = "lblUsernameHeader";
            lblUsernameHeader.Size = new Size(60, 15);
            lblUsernameHeader.TabIndex = 4;
            lblUsernameHeader.Text = "Username";
            pnlUserContainer.BackColor = Color.FromArgb(180, 30, 41, 59);
            pnlUserContainer.Controls.Add(lblUserIcon);
            pnlUserContainer.Controls.Add(txtUsername);
            pnlUserContainer.Location = new Point(36, 218);
            pnlUserContainer.Name = "pnlUserContainer";
            pnlUserContainer.Size = new Size(338, 42);
            pnlUserContainer.TabIndex = 5;
            lblUserIcon.BackColor = Color.Transparent;
            lblUserIcon.Font = new Font("Segoe UI", 10.5F);
            lblUserIcon.ForeColor = Color.FromArgb(148, 163, 184);
            lblUserIcon.Location = new Point(10, 10);
            lblUserIcon.Name = "lblUserIcon";
            lblUserIcon.Size = new Size(24, 22);
            lblUserIcon.TabIndex = 0;
            lblUserIcon.Text = "👤";
            lblUserIcon.TextAlign = ContentAlignment.MiddleCenter;
            txtUsername.BackColor = Color.FromArgb(30, 41, 59);
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Font = new Font("Segoe UI", 10F);
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(38, 12);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(286, 18);
            txtUsername.TabIndex = 1;
            lblPasswordHeader.AutoSize = true;
            lblPasswordHeader.BackColor = Color.Transparent;
            lblPasswordHeader.Font = new Font("Segoe UI", 8.5F);
            lblPasswordHeader.ForeColor = Color.FromArgb(203, 213, 225);
            lblPasswordHeader.Location = new Point(38, 268);
            lblPasswordHeader.Name = "lblPasswordHeader";
            lblPasswordHeader.Size = new Size(57, 15);
            lblPasswordHeader.TabIndex = 6;
            lblPasswordHeader.Text = "Password";
            pnlPassContainer.BackColor = Color.FromArgb(180, 30, 41, 59);
            pnlPassContainer.Controls.Add(btnTogglePassword);
            pnlPassContainer.Controls.Add(lblPassIcon);
            pnlPassContainer.Controls.Add(txtPassword);
            pnlPassContainer.Location = new Point(36, 286);
            pnlPassContainer.Name = "pnlPassContainer";
            pnlPassContainer.Size = new Size(338, 42);
            pnlPassContainer.TabIndex = 7;
            lblPassIcon.BackColor = Color.Transparent;
            lblPassIcon.Font = new Font("Segoe UI", 10F);
            lblPassIcon.ForeColor = Color.FromArgb(148, 163, 184);
            lblPassIcon.Location = new Point(10, 10);
            lblPassIcon.Name = "lblPassIcon";
            lblPassIcon.Size = new Size(24, 22);
            lblPassIcon.TabIndex = 0;
            lblPassIcon.Text = "🔒";
            lblPassIcon.TextAlign = ContentAlignment.MiddleCenter;
            txtPassword.BackColor = Color.FromArgb(30, 41, 59);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.ForeColor = Color.White;
            txtPassword.Location = new Point(38, 12);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.Size = new Size(254, 18);
            txtPassword.TabIndex = 1;
            btnTogglePassword.BackColor = Color.Transparent;
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnTogglePassword.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI", 10F);
            btnTogglePassword.ForeColor = Color.FromArgb(148, 163, 184);
            btnTogglePassword.Location = new Point(298, 7);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Size = new Size(30, 26);
            btnTogglePassword.TabIndex = 2;
            btnTogglePassword.Text = "👁️";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += btnTogglePassword_Click;
            pnlOptions.BackColor = Color.Transparent;
            pnlOptions.Controls.Add(lnkForgotPassword);
            pnlOptions.Controls.Add(chkRememberUsername);
            pnlOptions.Location = new Point(36, 336);
            pnlOptions.Name = "pnlOptions";
            pnlOptions.Size = new Size(338, 26);
            pnlOptions.TabIndex = 8;
            chkRememberUsername.AutoSize = true;
            chkRememberUsername.Cursor = Cursors.Hand;
            chkRememberUsername.Dock = DockStyle.Left;
            chkRememberUsername.Font = new Font("Segoe UI", 8.5F);
            chkRememberUsername.ForeColor = Color.FromArgb(203, 213, 225);
            chkRememberUsername.Location = new Point(0, 0);
            chkRememberUsername.Name = "chkRememberUsername";
            chkRememberUsername.Size = new Size(104, 26);
            chkRememberUsername.TabIndex = 0;
            chkRememberUsername.Text = "Remember Me";
            chkRememberUsername.UseVisualStyleBackColor = true;
            lnkForgotPassword.ActiveLinkColor = Color.White;
            lnkForgotPassword.AutoSize = true;
            lnkForgotPassword.Cursor = Cursors.Hand;
            lnkForgotPassword.Dock = DockStyle.Right;
            lnkForgotPassword.Font = new Font("Segoe UI", 8.5F);
            lnkForgotPassword.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkForgotPassword.LinkColor = Color.FromArgb(248, 113, 113);
            lnkForgotPassword.Location = new Point(241, 0);
            lnkForgotPassword.Name = "lnkForgotPassword";
            lnkForgotPassword.Size = new Size(97, 15);
            lnkForgotPassword.TabIndex = 1;
            lnkForgotPassword.TabStop = true;
            lnkForgotPassword.Text = "Forgot Password?";
            lnkForgotPassword.LinkClicked += lnkForgotPassword_LinkClicked;
            btnLogin.BackColor = Color.FromArgb(185, 28, 28);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(36, 372);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(338, 44);
            btnLogin.TabIndex = 9;
            btnLogin.Text = "LOGIN";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            lblAuthorized.BackColor = Color.Transparent;
            lblAuthorized.Font = new Font("Segoe UI", 8.5F);
            lblAuthorized.ForeColor = Color.FromArgb(148, 163, 184);
            lblAuthorized.Location = new Point(36, 432);
            lblAuthorized.Name = "lblAuthorized";
            lblAuthorized.Size = new Size(338, 20);
            lblAuthorized.TabIndex = 11;
            lblAuthorized.Text = "🔒  Authorized Personnel Only";
            lblAuthorized.TextAlign = ContentAlignment.MiddleCenter;
            lblCopyright.BackColor = Color.Transparent;
            lblCopyright.Font = new Font("Segoe UI", 7.5F);
            lblCopyright.ForeColor = Color.FromArgb(100, 116, 139);
            lblCopyright.Location = new Point(36, 456);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(338, 20);
            lblCopyright.TabIndex = 12;
            lblCopyright.Text = "© M.J. Billones Transport Service • All rights reserved.";
            lblCopyright.TextAlign = ContentAlignment.MiddleCenter;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(800, 600);
            Controls.Add(pnlWindowControls);
            Controls.Add(pnlGlassCard);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoginForm";
            StartPosition = FormStartPosition.Manual;
            Text = "M.J. Billones Transport Service - Login";
            pnlWindowControls.ResumeLayout(false);
            pnlGlassCard.ResumeLayout(false);
            pnlGlassCard.PerformLayout();
            pnlLogoBadge.ResumeLayout(false);
            pnlAlert.ResumeLayout(false);
            pnlUserContainer.ResumeLayout(false);
            pnlUserContainer.PerformLayout();
            pnlPassContainer.ResumeLayout(false);
            pnlPassContainer.PerformLayout();
            pnlOptions.ResumeLayout(false);
            pnlOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlWindowControls;
        private Button btnMinimize;
        private Button btnClose;
        private Panel pnlGlassCard;
        private Panel pnlLogoBadge;
        private PictureBox picLogo;
        private Label lblTitle;
        private Label lblSubtitle;
        private Panel pnlAlert;
        private Label lblError;
        private Label lblUsernameHeader;
        private Panel pnlUserContainer;
        private Label lblUserIcon;
        private TextBox txtUsername;
        private Label lblPasswordHeader;
        private Panel pnlPassContainer;
        private Label lblPassIcon;
        private TextBox txtPassword;
        private Button btnTogglePassword;
        private Panel pnlOptions;
        private CheckBox chkRememberUsername;
        private LinkLabel lnkForgotPassword;
        private Button btnLogin;
        private Label lblAuthorized;
        private Label lblCopyright;
    }
}
"""

with open('LoginForm.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(designer_code)
print('LoginForm.Designer.cs written successfully')

login_cs_code = """using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private const string UserPlaceholder = "Enter your username";
        private int failedAttempts = 0;
        private int lockoutSecondsRemaining = 0;
        private System.Windows.Forms.Timer? lockoutTimer;
        private bool isPasswordRevealed = false;

        public LoginForm()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            InitializeComponent();
            MJdatabase.InitializeDatabase();

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            if (Screen.PrimaryScreen != null)
            {
                this.Bounds = Screen.PrimaryScreen.WorkingArea;
            }

            SetupBackgroundAndLogo();
            SetupCustomRendering();
            SetupInputFocusAndPlaceholders();
            LoadRememberedUsername();

            EnableWindowDragging(this);
            EnableWindowDragging(pnlGlassCard);
            EnableWindowDragging(pnlLogoBadge);
            EnableWindowDragging(lblTitle);
            EnableWindowDragging(lblSubtitle);
            EnableWindowDragging(picLogo);

            this.AcceptButton = btnLogin;
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Application.Exit();
                }
            };

            this.Resize += LoginForm_Resize;
            PositionLayout();
        }

        private void SetupBackgroundAndLogo()
        {
            try
            {
                string bgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login_bg.jpg");
                if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "login_bg.jpg");
                if (System.IO.File.Exists(bgPath))
                {
                    this.BackgroundImage = Image.FromFile(bgPath);
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Background init: {ex.Message}");
            }

            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo_transparent.png");

                if (System.IO.File.Exists(logoPath))
                {
                    picLogo.Image = Image.FromFile(logoPath);
                    picLogo.Visible = true;
                    using (Bitmap bmp = new Bitmap(logoPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = (Icon)Icon.FromHandle(hIcon).Clone();
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Logo init: {ex.Message}");
            }
        }

        private void SetupCustomRendering()
        {
            pnlGlassCard.Paint += PnlGlassCard_Paint;
            pnlLogoBadge.Paint += PnlLogoBadge_Paint;
            pnlUserContainer.Paint += (s, e) => DrawContainerBorder(pnlUserContainer, e, txtUsername.Focused);
            pnlPassContainer.Paint += (s, e) => DrawContainerBorder(pnlPassContainer, e, txtPassword.Focused);
            pnlAlert.Paint += PnlAlert_Paint;
            btnLogin.Paint += BtnLogin_Paint;
        }

        private void PnlLogoBadge_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(1, 1, pnlLogoBadge.Width - 3, pnlLogoBadge.Height - 3);
            int radius = 14;
            using (var path = GetRoundedRectPath(rect, radius))
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0), 2f))
                {
                    e.Graphics.DrawPath(shadowPen, path);
                }
                using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private void PnlGlassCard_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(6, 6, pnlGlassCard.Width - 13, pnlGlassCard.Height - 13);
            int radius = 18;
            using (var path = GetRoundedRectPath(rect, radius))
            {
                using (var brush = new SolidBrush(Color.FromArgb(220, 15, 23, 42)))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (var glow1 = new Pen(Color.FromArgb(35, 225, 29, 72), 5.5f))
                {
                    e.Graphics.DrawPath(glow1, path);
                }
                using (var glow2 = new Pen(Color.FromArgb(70, 239, 68, 68), 3.0f))
                {
                    e.Graphics.DrawPath(glow2, path);
                }
                using (var border = new Pen(Color.FromArgb(225, 29, 72), 1.5f))
                {
                    e.Graphics.DrawPath(border, path);
                }
            }
        }

        private void DrawContainerBorder(Panel pnl, PaintEventArgs e, bool hasFocus)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3);
            using (var path = GetRoundedRectPath(rect, 8))
            {
                using (var brush = new SolidBrush(Color.FromArgb(190, 30, 41, 59)))
                {
                    e.Graphics.FillPath(brush, path);
                }
                Color borderClr = hasFocus ? Color.FromArgb(225, 29, 72) : Color.FromArgb(71, 85, 105);
                float width = hasFocus ? 1.5f : 1.0f;
                using (var pen = new Pen(borderClr, width))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private void PnlAlert_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(1, 1, pnlAlert.Width - 3, pnlAlert.Height - 3);
            using (var path = GetRoundedRectPath(rect, 6))
            {
                using (var pen = new Pen(Color.FromArgb(239, 68, 68), 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private void BtnLogin_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, btnLogin.Width, btnLogin.Height);
            using (var path = GetRoundedRectPath(new Rectangle(0, 0, btnLogin.Width - 1, btnLogin.Height - 1), 10))
            {
                using (var brush = new SolidBrush(btnLogin.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, btnLogin.Text, btnLogin.Font, rect, btnLogin.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void SetupInputFocusAndPlaceholders()
        {
            txtUsername.Text = UserPlaceholder;
            txtUsername.ForeColor = Color.FromArgb(148, 163, 184);

            txtUsername.GotFocus += (s, e) =>
            {
                if (txtUsername.Text == UserPlaceholder)
                {
                    txtUsername.Text = "";
                    txtUsername.ForeColor = Color.White;
                }
                pnlUserContainer.Invalidate();
            };

            txtUsername.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    txtUsername.Text = UserPlaceholder;
                    txtUsername.ForeColor = Color.FromArgb(148, 163, 184);
                }
                pnlUserContainer.Invalidate();
            };

            txtPassword.GotFocus += (s, e) => { pnlPassContainer.Invalidate(); };
            txtPassword.LostFocus += (s, e) => { pnlPassContainer.Invalidate(); };
        }

        private void LoginForm_Resize(object? sender, EventArgs e)
        {
            PositionLayout();
        }

        private void PositionLayout()
        {
            if (pnlGlassCard != null)
            {
                pnlGlassCard.Location = new Point(
                    Math.Max(20, (this.ClientSize.Width - pnlGlassCard.Width) / 2),
                    Math.Max(20, (this.ClientSize.Height - pnlGlassCard.Height) / 2)
                );
            }

            if (pnlWindowControls != null)
            {
                pnlWindowControls.Location = new Point(
                    this.ClientSize.Width - pnlWindowControls.Width - 16,
                    14
                );
            }
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
                        txtUsername.ForeColor = Color.White;
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
                pnlAlert.BackColor = Color.FromArgb(200, 6, 78, 59);
                lblError.ForeColor = Color.FromArgb(167, 243, 208);
            }
            else
            {
                pnlAlert.BackColor = Color.FromArgb(200, 88, 28, 28);
                lblError.ForeColor = Color.FromArgb(254, 202, 202);
            }
            pnlAlert.Visible = true;
            pnlAlert.Invalidate();
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
                if (username == UserPlaceholder)
                {
                    username = "";
                }
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
                string user = txtUsername.Text.Trim();
                if (user == UserPlaceholder) user = "";
                using (var dlg = new Forms.ForgotPasswordDialog(user))
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

with open('LoginForm.cs', 'w', encoding='utf-8') as f:
    f.write(login_cs_code)
print('LoginForm.cs written successfully')
