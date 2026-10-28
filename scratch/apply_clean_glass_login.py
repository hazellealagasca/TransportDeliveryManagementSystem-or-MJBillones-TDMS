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
            pnlTopRightBranding = new Panel();
            picTopRightLogo = new PictureBox();
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
            SuspendLayout();
            txtUsername.BackColor = Color.FromArgb(22, 31, 48);
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Font = new Font("Segoe UI", 10.5F);
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(270, 290);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(280, 20);
            txtUsername.TabIndex = 1;
            txtPassword.BackColor = Color.FromArgb(22, 31, 48);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 10.5F);
            txtPassword.ForeColor = Color.White;
            txtPassword.Location = new Point(270, 360);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.Size = new Size(248, 20);
            txtPassword.TabIndex = 2;
            pnlAlert.Visible = false;
            lblError.Text = "";
            chkRememberUsername.Checked = false;
            btnLogin.Enabled = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(1280, 720);
            Controls.Add(txtUsername);
            Controls.Add(txtPassword);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoginForm";
            StartPosition = FormStartPosition.Manual;
            Text = "M.J. Billones Transport Service - Login";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlWindowControls;
        private Button btnMinimize;
        private Button btnClose;
        private Panel pnlTopRightBranding;
        private PictureBox picTopRightLogo;
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

        private Image? rawBackgroundImage;
        private Image? cleanLogoImage;
        private Bitmap? cachedSceneBitmap;

        private Rectangle cardRect;
        private Rectangle logoBadgeRect;
        private Rectangle topRightBadgeRect;
        private Rectangle alertRect;
        private Rectangle userRect;
        private Rectangle passRect;
        private Rectangle eyeRect;
        private Rectangle chkBoxRect;
        private Rectangle chkHitRect;
        private Rectangle forgotRect;
        private Rectangle loginBtnRect;
        private Rectangle minBtnRect;
        private Rectangle closeBtnRect;

        private bool isHoverLogin = false;
        private bool isHoverEye = false;
        private bool isHoverForgot = false;
        private bool isHoverMin = false;
        private bool isHoverClose = false;
        private bool isAlertSuccess = false;

        public LoginForm()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.DoubleBuffered = true;

            InitializeComponent();
            MJdatabase.InitializeDatabase();

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            if (Screen.PrimaryScreen != null)
            {
                this.Bounds = Screen.PrimaryScreen.WorkingArea;
            }

            SetupBackgroundAndLogo();
            SetupInputFocusAndPlaceholders();
            LoadRememberedUsername();

            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
            txtUsername.KeyDown += Input_KeyDown;
            txtPassword.KeyDown += Input_KeyDown;

            this.Resize += LoginForm_Resize;
            PositionLayout();
        }

        private void LoginForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
            else if (e.KeyCode == Keys.Enter && btnLogin.Enabled)
            {
                e.SuppressKeyPress = true;
                btnLogin_Click(this, EventArgs.Empty);
            }
        }

        private void Input_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnLogin.Enabled)
            {
                e.SuppressKeyPress = true;
                btnLogin_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
        }

        private void SetupBackgroundAndLogo()
        {
            try
            {
                string bgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login_bg.jpg");
                if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "login_bg.jpg");
                if (System.IO.File.Exists(bgPath))
                {
                    rawBackgroundImage = Image.FromFile(bgPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Background init: {ex.Message}");
            }

            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_clean.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo_clean.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo_transparent.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo.png");

                if (System.IO.File.Exists(logoPath))
                {
                    cleanLogoImage = Image.FromFile(logoPath);
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

        private void SetupInputFocusAndPlaceholders()
        {
            Color inputInnerBg = Color.FromArgb(22, 31, 48);
            txtUsername.BackColor = inputInnerBg;
            txtPassword.BackColor = inputInnerBg;

            txtUsername.Text = UserPlaceholder;
            txtUsername.ForeColor = Color.FromArgb(148, 163, 184);

            txtUsername.GotFocus += (s, e) =>
            {
                if (txtUsername.Text == UserPlaceholder)
                {
                    txtUsername.Text = "";
                    txtUsername.ForeColor = Color.White;
                }
                this.Invalidate();
            };

            txtUsername.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    txtUsername.Text = UserPlaceholder;
                    txtUsername.ForeColor = Color.FromArgb(148, 163, 184);
                }
                this.Invalidate();
            };

            txtPassword.GotFocus += (s, e) => { this.Invalidate(); };
            txtPassword.LostFocus += (s, e) => { this.Invalidate(); };
        }

        private void LoginForm_Resize(object? sender, EventArgs e)
        {
            PositionLayout();
        }

        private void PositionLayout()
        {
            int w = Math.Max(800, this.ClientSize.Width);
            int h = Math.Max(600, this.ClientSize.Height);

            int cardW = 420;
            int cardH = 550;
            cardRect = new Rectangle((w - cardW) / 2, (h - cardH) / 2, cardW, cardH);

            logoBadgeRect = new Rectangle(cardRect.X + (cardW - 260) / 2, cardRect.Y + 20, 260, 96);
            alertRect = new Rectangle(cardRect.X + 38, cardRect.Y + 188, cardW - 76, 30);
            userRect = new Rectangle(cardRect.X + 38, cardRect.Y + 242, cardW - 76, 42);
            passRect = new Rectangle(cardRect.X + 38, cardRect.Y + 314, cardW - 76, 42);
            eyeRect = new Rectangle(passRect.Right - 38, passRect.Y + 6, 32, 30);

            chkBoxRect = new Rectangle(cardRect.X + 40, cardRect.Y + 368, 18, 18);
            chkHitRect = new Rectangle(cardRect.X + 38, cardRect.Y + 364, 135, 26);
            forgotRect = new Rectangle(cardRect.Right - 38 - 118, cardRect.Y + 364, 118, 26);

            loginBtnRect = new Rectangle(cardRect.X + 38, cardRect.Y + 402, cardW - 76, 44);

            closeBtnRect = new Rectangle(w - 50, 14, 36, 30);
            minBtnRect = new Rectangle(w - 92, 14, 36, 30);
            topRightBadgeRect = new Rectangle(w - 92 - 14 - 260, 14, 260, 92);

            if (txtUsername != null)
            {
                txtUsername.Location = new Point(userRect.X + 40, userRect.Y + (userRect.Height - txtUsername.Height) / 2);
                txtUsername.Width = userRect.Width - 52;
            }

            if (txtPassword != null)
            {
                txtPassword.Location = new Point(passRect.X + 40, passRect.Y + (passRect.Height - txtPassword.Height) / 2);
                txtPassword.Width = passRect.Width - 84;
            }

            RebuildSceneBitmap();
            this.Invalidate();
        }

        private Bitmap CreateBlurredRegion(Bitmap source, Rectangle region, int downsampleFactor)
        {
            Rectangle safeRect = Rectangle.Intersect(region, new Rectangle(0, 0, source.Width, source.Height));
            if (safeRect.Width <= 4 || safeRect.Height <= 4)
            {
                return new Bitmap(Math.Max(1, region.Width), Math.Max(1, region.Height));
            }

            int smallW = Math.Max(4, safeRect.Width / downsampleFactor);
            int smallH = Math.Max(4, safeRect.Height / downsampleFactor);

            using (Bitmap pass1 = new Bitmap(smallW, smallH))
            using (Bitmap pass2 = new Bitmap(Math.Max(2, smallW / 2), Math.Max(2, smallH / 2)))
            {
                using (Graphics g1 = Graphics.FromImage(pass1))
                {
                    g1.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g1.DrawImage(source, new Rectangle(0, 0, smallW, smallH), safeRect, GraphicsUnit.Pixel);
                }
                using (Graphics g2 = Graphics.FromImage(pass2))
                {
                    g2.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g2.DrawImage(pass1, new Rectangle(0, 0, pass2.Width, pass2.Height), new Rectangle(0, 0, smallW, smallH), GraphicsUnit.Pixel);
                }

                Bitmap blurred = new Bitmap(region.Width, region.Height);
                using (Graphics gOut = Graphics.FromImage(blurred))
                {
                    gOut.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gOut.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gOut.DrawImage(pass2, new Rectangle(0, 0, region.Width, region.Height), new Rectangle(0, 0, pass2.Width, pass2.Height), GraphicsUnit.Pixel);
                }
                return blurred;
            }
        }

        private void RebuildSceneBitmap()
        {
            int w = Math.Max(800, this.ClientSize.Width);
            int h = Math.Max(600, this.ClientSize.Height);

            cachedSceneBitmap?.Dispose();
            cachedSceneBitmap = new Bitmap(w, h);

            using (Bitmap bgFull = new Bitmap(w, h))
            {
                using (Graphics gBg = Graphics.FromImage(bgFull))
                {
                    gBg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    if (rawBackgroundImage != null)
                    {
                        gBg.DrawImage(rawBackgroundImage, new Rectangle(0, 0, w, h));
                    }
                    else
                    {
                        using (var br = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 45f))
                        {
                            gBg.FillRectangle(br, 0, 0, w, h);
                        }
                    }
                }

                using (Graphics g = Graphics.FromImage(cachedSceneBitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    g.DrawImageUnscaled(bgFull, 0, 0);

                    for (int i = 6; i >= 1; i--)
                    {
                        int expand = i * 2;
                        Rectangle glowRect = new Rectangle(cardRect.X - expand, cardRect.Y - expand, cardRect.Width + expand * 2, cardRect.Height + expand * 2);
                        using (GraphicsPath glowPath = GetRoundedRectPath(glowRect, 22 + expand))
                        using (Pen glowPen = new Pen(Color.FromArgb(14 + (6 - i) * 6, 239, 68, 68), 3.2f))
                        {
                            g.DrawPath(glowPen, glowPath);
                        }
                    }

                    using (Bitmap blurredCard = CreateBlurredRegion(bgFull, cardRect, 6))
                    using (GraphicsPath cardPath = GetRoundedRectPath(cardRect, 22))
                    {
                        GraphicsState state = g.Save();
                        g.SetClip(cardPath);
                        g.DrawImageUnscaled(blurredCard, cardRect.X, cardRect.Y);

                        using (LinearGradientBrush glassTint = new LinearGradientBrush(
                            cardRect,
                            Color.FromArgb(175, 13, 21, 38),
                            Color.FromArgb(208, 10, 15, 28),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(glassTint, cardRect);
                        }

                        Rectangle topSheenRect = new Rectangle(cardRect.X, cardRect.Y, cardRect.Width, 160);
                        using (LinearGradientBrush topSheen = new LinearGradientBrush(
                            topSheenRect,
                            Color.FromArgb(38, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(topSheen, topSheenRect);
                        }

                        g.Restore(state);

                        Rectangle innerSpecRect = new Rectangle(cardRect.X + 1, cardRect.Y + 1, cardRect.Width - 2, cardRect.Height - 2);
                        using (GraphicsPath innerPath = GetRoundedRectPath(innerSpecRect, 21))
                        using (Pen innerPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1.0f))
                        {
                            g.DrawPath(innerPen, innerPath);
                        }

                        using (Pen neonBorder = new Pen(Color.FromArgb(235, 225, 29, 72), 1.8f))
                        {
                            g.DrawPath(neonBorder, cardPath);
                        }
                    }

                    DrawFrostedLogoBadge(g, bgFull, logoBadgeRect, 24, 150, 120, false);

                    DrawFrostedLogoBadge(g, bgFull, topRightBadgeRect, 22, 165, 135, true);
                }
            }
        }

        private void DrawFrostedLogoBadge(Graphics g, Bitmap bgFull, Rectangle badgeRect, int radius, int topAlpha, int bottomAlpha, bool hasCrimsonOuterGlow)
        {
            if (hasCrimsonOuterGlow)
            {
                for (int i = 4; i >= 1; i--)
                {
                    int exp = i * 2;
                    Rectangle gr = new Rectangle(badgeRect.X - exp, badgeRect.Y - exp, badgeRect.Width + exp * 2, badgeRect.Height + exp * 2);
                    using (GraphicsPath gp = GetRoundedRectPath(gr, radius + exp))
                    using (Pen gPen = new Pen(Color.FromArgb(16 + (4 - i) * 8, 239, 68, 68), 2.8f))
                    {
                        g.DrawPath(gPen, gp);
                    }
                }
            }
            else
            {
                Rectangle shadowRect = new Rectangle(badgeRect.X - 2, badgeRect.Y - 1, badgeRect.Width + 4, badgeRect.Height + 4);
                using (GraphicsPath sp = GetRoundedRectPath(shadowRect, radius + 2))
                using (Pen sPen = new Pen(Color.FromArgb(45, 0, 0, 0), 3.5f))
                {
                    g.DrawPath(sPen, sp);
                }
            }

            using (Bitmap blurredBadge = CreateBlurredRegion(bgFull, badgeRect, 5))
            using (GraphicsPath badgePath = GetRoundedRectPath(badgeRect, radius))
            {
                GraphicsState st = g.Save();
                g.SetClip(badgePath);
                g.DrawImageUnscaled(blurredBadge, badgeRect.X, badgeRect.Y);

                using (LinearGradientBrush whiteFrost = new LinearGradientBrush(
                    badgeRect,
                    Color.FromArgb(topAlpha, 255, 255, 255),
                    Color.FromArgb(bottomAlpha, 241, 245, 249),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(whiteFrost, badgeRect);
                }

                if (cleanLogoImage != null)
                {
                    int padX = 12;
                    int padY = 6;
                    Rectangle avail = new Rectangle(badgeRect.X + padX, badgeRect.Y + padY, badgeRect.Width - padX * 2, badgeRect.Height - padY * 2);
                    float imgRatio = (float)cleanLogoImage.Width / cleanLogoImage.Height;
                    float boxRatio = (float)avail.Width / avail.Height;
                    int drawW, drawH;
                    if (imgRatio > boxRatio)
                    {
                        drawW = avail.Width;
                        drawH = (int)(drawW / imgRatio);
                    }
                    else
                    {
                        drawH = avail.Height;
                        drawW = (int)(drawH * imgRatio);
                    }
                    int drawX = avail.X + (avail.Width - drawW) / 2;
                    int drawY = avail.Y + (avail.Height - drawH) / 2;
                    g.DrawImage(cleanLogoImage, new Rectangle(drawX, drawY, drawW, drawH));
                }

                g.Restore(st);

                using (Pen rimPen = new Pen(Color.FromArgb(215, 255, 255, 255), 1.5f))
                {
                    g.DrawPath(rimPen, badgePath);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (cachedSceneBitmap == null || cachedSceneBitmap.Width != this.ClientSize.Width || cachedSceneBitmap.Height != this.ClientSize.Height)
            {
                RebuildSceneBitmap();
            }

            if (cachedSceneBitmap != null)
            {
                g.DrawImageUnscaled(cachedSceneBitmap, 0, 0);
            }

            Rectangle titleRect = new Rectangle(cardRect.X + 20, cardRect.Y + 124, cardRect.Width - 40, 36);
            using (Font titleFont = new Font("Segoe UI", 20F, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "Login", titleFont, titleRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            Rectangle subRect = new Rectangle(cardRect.X + 20, cardRect.Y + 160, cardRect.Width - 40, 22);
            using (Font subFont = new Font("Segoe UI", 9.25F))
            {
                TextRenderer.DrawText(g, "Welcome back! Please login to your account", subFont, subRect, Color.FromArgb(186, 200, 218), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (pnlAlert.Visible && !string.IsNullOrEmpty(lblError.Text))
            {
                using (GraphicsPath alertPath = GetRoundedRectPath(alertRect, 8))
                {
                    Color fillClr = isAlertSuccess ? Color.FromArgb(190, 6, 78, 59) : Color.FromArgb(195, 88, 28, 28);
                    Color borderClr = isAlertSuccess ? Color.FromArgb(52, 211, 153) : Color.FromArgb(239, 68, 68);
                    Color textClr = isAlertSuccess ? Color.FromArgb(167, 243, 208) : Color.FromArgb(254, 202, 202);
                    using (SolidBrush br = new SolidBrush(fillClr))
                    {
                        g.FillPath(br, alertPath);
                    }
                    using (Pen p = new Pen(borderClr, 1.2f))
                    {
                        g.DrawPath(p, alertPath);
                    }
                    using (Font errFont = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, lblError.Text, errFont, alertRect, textClr, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                }
            }

            using (Font lblFont = new Font("Segoe UI", 8.75F))
            {
                TextRenderer.DrawText(g, "Username", lblFont, new Point(userRect.X + 2, userRect.Y - 20), Color.FromArgb(203, 213, 225));
                TextRenderer.DrawText(g, "Password", lblFont, new Point(passRect.X + 2, passRect.Y - 20), Color.FromArgb(203, 213, 225));
            }

            DrawInputPill(g, userRect, txtUsername != null && txtUsername.Focused, "👤");
            DrawInputPill(g, passRect, txtPassword != null && txtPassword.Focused, "🔒");

            using (Font eyeFont = new Font("Segoe UI", 10.5F))
            {
                Color eyeClr = isHoverEye ? Color.White : Color.FromArgb(148, 163, 184);
                string eyeGlyph = isPasswordRevealed ? "🔒" : "👁";
                TextRenderer.DrawText(g, eyeGlyph, eyeFont, eyeRect, eyeClr, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            using (GraphicsPath chkPath = GetRoundedRectPath(chkBoxRect, 5))
            {
                if (chkRememberUsername.Checked)
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillPath(br, chkPath);
                    }
                    using (Pen p = new Pen(Color.FromArgb(248, 113, 113), 1.2f))
                    {
                        g.DrawPath(p, chkPath);
                    }
                    using (Pen checkPen = new Pen(Color.White, 2.0f))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        g.DrawLines(checkPen, new Point[]
                        {
                            new Point(chkBoxRect.X + 4, chkBoxRect.Y + 9),
                            new Point(chkBoxRect.X + 8, chkBoxRect.Y + 13),
                            new Point(chkBoxRect.X + 14, chkBoxRect.Y + 5)
                        });
                    }
                }
                else
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(160, 22, 31, 48)))
                    {
                        g.FillPath(br, chkPath);
                    }
                    using (Pen p = new Pen(Color.FromArgb(100, 116, 139), 1.2f))
                    {
                        g.DrawPath(p, chkPath);
                    }
                }
            }

            using (Font optFont = new Font("Segoe UI", 8.75F))
            {
                TextRenderer.DrawText(g, "Remember Me", optFont, new Point(chkBoxRect.Right + 7, chkBoxRect.Y), Color.FromArgb(203, 213, 225));
            }

            using (Font linkFont = new Font("Segoe UI", 8.75F, isHoverForgot ? FontStyle.Underline : FontStyle.Regular))
            {
                Color linkClr = isHoverForgot ? Color.White : Color.FromArgb(248, 113, 113);
                TextRenderer.DrawText(g, "Forgot Password?", linkFont, forgotRect, linkClr, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            using (GraphicsPath btnPath = GetRoundedRectPath(loginBtnRect, 11))
            {
                Color cTop = !btnLogin.Enabled ? Color.FromArgb(100, 116, 139) : (isHoverLogin ? Color.FromArgb(239, 68, 68) : Color.FromArgb(210, 32, 32));
                Color cBot = !btnLogin.Enabled ? Color.FromArgb(71, 85, 105) : (isHoverLogin ? Color.FromArgb(185, 28, 28) : Color.FromArgb(153, 27, 27));
                using (LinearGradientBrush btnBr = new LinearGradientBrush(loginBtnRect, cTop, cBot, LinearGradientMode.Vertical))
                {
                    g.FillPath(btnBr, btnPath);
                }
                using (Pen btnRim = new Pen(Color.FromArgb(90, 255, 255, 255), 1.0f))
                {
                    g.DrawPath(btnRim, btnPath);
                }
                using (Font btnFont = new Font("Segoe UI", 11F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "LOGIN", btnFont, loginBtnRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            Rectangle authRect = new Rectangle(cardRect.X + 20, cardRect.Y + 468, cardRect.Width - 40, 22);
            using (Font authFont = new Font("Segoe UI", 8.5F))
            {
                TextRenderer.DrawText(g, "🔒  Authorized Personnel Only", authFont, authRect, Color.FromArgb(165, 180, 200), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            Rectangle copyRect = new Rectangle(cardRect.X + 20, cardRect.Y + 494, cardRect.Width - 40, 20);
            using (Font copyFont = new Font("Segoe UI", 7.75F))
            {
                TextRenderer.DrawText(g, "© M.J. Billones Transport Service • All rights reserved.", copyFont, copyRect, Color.FromArgb(120, 136, 158), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            DrawWindowControlButton(g, minBtnRect, "—", isHoverMin, false);
            DrawWindowControlButton(g, closeBtnRect, "✕", isHoverClose, true);
        }

        private void DrawInputPill(Graphics g, Rectangle rect, bool isFocused, string iconGlyph)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, 10))
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(22, 31, 48)))
                {
                    g.FillPath(br, path);
                }

                if (isFocused)
                {
                    Rectangle glowRect = new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                    using (GraphicsPath gp = GetRoundedRectPath(glowRect, 12))
                    using (Pen glowPen = new Pen(Color.FromArgb(65, 239, 68, 68), 2.5f))
                    {
                        g.DrawPath(glowPen, gp);
                    }
                }

                Color borderClr = isFocused ? Color.FromArgb(239, 68, 68) : Color.FromArgb(71, 85, 105);
                using (Pen p = new Pen(borderClr, isFocused ? 1.5f : 1.1f))
                {
                    g.DrawPath(p, path);
                }
            }

            Rectangle iconRect = new Rectangle(rect.X + 10, rect.Y + 8, 26, 26);
            using (Font iconFont = new Font("Segoe UI", 10F))
            {
                TextRenderer.DrawText(g, iconGlyph, iconFont, iconRect, Color.FromArgb(148, 163, 184), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawWindowControlButton(Graphics g, Rectangle rect, string glyph, bool isHover, bool isClose)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, 8))
            {
                Color bg = isHover
                    ? (isClose ? Color.FromArgb(220, 220, 38, 38) : Color.FromArgb(200, 51, 65, 85))
                    : Color.FromArgb(150, 15, 23, 42);
                using (SolidBrush br = new SolidBrush(bg))
                {
                    g.FillPath(br, path);
                }
                using (Pen p = new Pen(Color.FromArgb(80, 255, 255, 255), 1.0f))
                {
                    g.DrawPath(p, path);
                }
                using (Font f = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, glyph, f, rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hLogin = loginBtnRect.Contains(e.Location) && btnLogin.Enabled;
            bool hEye = eyeRect.Contains(e.Location);
            bool hForgot = forgotRect.Contains(e.Location);
            bool hMin = minBtnRect.Contains(e.Location);
            bool hClose = closeBtnRect.Contains(e.Location);
            bool hChk = chkHitRect.Contains(e.Location);
            bool hUser = userRect.Contains(e.Location);
            bool hPass = passRect.Contains(e.Location);

            if (hLogin != isHoverLogin || hEye != isHoverEye || hForgot != isHoverForgot || hMin != isHoverMin || hClose != isHoverClose)
            {
                isHoverLogin = hLogin;
                isHoverEye = hEye;
                isHoverForgot = hForgot;
                isHoverMin = hMin;
                isHoverClose = hClose;
                this.Invalidate();
            }

            if (hLogin || hEye || hForgot || hMin || hClose || hChk)
            {
                this.Cursor = Cursors.Hand;
            }
            else if (hUser || hPass)
            {
                this.Cursor = Cursors.IBeam;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (loginBtnRect.Contains(e.Location))
            {
                if (btnLogin.Enabled)
                {
                    btnLogin_Click(this, EventArgs.Empty);
                }
                return;
            }

            if (eyeRect.Contains(e.Location))
            {
                btnTogglePassword_Click(this, EventArgs.Empty);
                return;
            }

            if (chkHitRect.Contains(e.Location))
            {
                chkRememberUsername.Checked = !chkRememberUsername.Checked;
                this.Invalidate();
                return;
            }

            if (forgotRect.Contains(e.Location))
            {
                lnkForgotPassword_LinkClicked(this, null);
                return;
            }

            if (minBtnRect.Contains(e.Location))
            {
                btnMinimize_Click(this, EventArgs.Empty);
                return;
            }

            if (closeBtnRect.Contains(e.Location))
            {
                btnClose_Click(this, EventArgs.Empty);
                return;
            }

            if (userRect.Contains(e.Location))
            {
                txtUsername.Focus();
                return;
            }

            if (passRect.Contains(e.Location))
            {
                txtPassword.Focus();
                return;
            }

            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
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
            isAlertSuccess = isSuccess;
            pnlAlert.Visible = true;
            this.Invalidate();
        }

        private void HideError()
        {
            pnlAlert.Visible = false;
            this.Invalidate();
        }

        private void btnTogglePassword_Click(object? sender, EventArgs e)
        {
            isPasswordRevealed = !isPasswordRevealed;
            txtPassword.PasswordChar = isPasswordRevealed ? '\\0' : '●';
            this.Invalidate();
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
            this.Invalidate();
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
            this.Invalidate();
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
