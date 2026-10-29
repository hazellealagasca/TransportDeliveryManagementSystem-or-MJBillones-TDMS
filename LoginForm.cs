using System;
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
            Shown += (s, e) =>
            {
                if (AppPaths.MigrationMessage.Length == 0) return;
                BeginInvoke(new Action(() => AppDialog.Show(this, "Data folder moved", DialogTone.Success,
                    "Your data has a new, safer home",
                    "The database, photos, documents, backups and logs are now kept in this folder. Rebuilding, updating or reinstalling the app won't touch it.",
                    path: AppPaths.DataRoot,
                    note: "The old copy in the program folder was left as it was, as an extra backup. Back up this folder (Settings → Backup & Restore) and keep a copy on a USB drive.",
                    okText: "Got it")));
            };
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
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Login] Background init: {ex.Message}"); ErrLog.Swallowed(ex); }
            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_clean.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo_clean.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo_transparent.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (!System.IO.File.Exists(logoPath)) logoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "logo.png");

                var customLogo = BrandAssets.CustomLogoCopy();
                if (customLogo != null)
                {
                    cleanLogoImage = customLogo;
                }
                else if (System.IO.File.Exists(logoPath))
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
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Login] Logo init: {ex.Message}"); ErrLog.Swallowed(ex); }
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
            int w = Math.Max(900, this.ClientSize.Width);
            int h = Math.Max(680, this.ClientSize.Height);

            int cardW = 520;
            int cardH = 650;
            cardRect = new Rectangle((w - cardW) / 2, Math.Max(14, (h - cardH) / 2), cardW, cardH);

            int contentX = cardRect.X + 50;
            int contentW = 420;

            logoBadgeRect = new Rectangle(cardRect.X + (cardW - 340) / 2, cardRect.Y + 20, 340, 125);
            alertRect = new Rectangle(contentX, cardRect.Y + 234, contentW, 36);
            userRect = new Rectangle(contentX, cardRect.Y + 302, contentW, 48);
            passRect = new Rectangle(contentX, cardRect.Y + 386, contentW, 48);
            eyeRect = new Rectangle(passRect.Right - 44, passRect.Y + 6, 36, 36);

            chkBoxRect = new Rectangle(contentX + 2, cardRect.Y + 450, 20, 20);
            chkHitRect = new Rectangle(contentX, cardRect.Y + 446, 190, 28);
            forgotRect = new Rectangle(cardRect.Right - 50 - 210, cardRect.Y + 446, 210, 28);

            loginBtnRect = new Rectangle(contentX, cardRect.Y + 488, contentW, 52);

            closeBtnRect = new Rectangle(w - 56, 14, 42, 34);
            minBtnRect = new Rectangle(w - 104, 14, 42, 34);

            if (txtUsername != null)
            {
                int prefH = Math.Max(20, txtUsername.PreferredHeight);
                txtUsername.Location = new Point(userRect.X + 46, userRect.Y + (userRect.Height - prefH) / 2);
                txtUsername.Width = userRect.Width - 60;
            }

            if (txtPassword != null)
            {
                int prefH = Math.Max(20, txtPassword.PreferredHeight);
                txtPassword.Location = new Point(passRect.X + 46, passRect.Y + (passRect.Height - prefH) / 2);
                txtPassword.Width = passRect.Width - 96;
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
            int w = Math.Max(900, this.ClientSize.Width);
            int h = Math.Max(680, this.ClientSize.Height);

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

                    for (int i = 7; i >= 1; i--)
                    {
                        int expand = i * 2;
                        Rectangle glowRect = new Rectangle(cardRect.X - expand, cardRect.Y - expand, cardRect.Width + expand * 2, cardRect.Height + expand * 2);
                        using (GraphicsPath glowPath = GetRoundedRectPath(glowRect, 24 + expand))
                        using (Pen glowPen = new Pen(Color.FromArgb(12 + (7 - i) * 7, 239, 68, 68), 3.4f))
                        {
                            g.DrawPath(glowPen, glowPath);
                        }
                    }

                    using (Bitmap blurredCard = CreateBlurredRegion(bgFull, cardRect, 6))
                    using (GraphicsPath cardPath = GetRoundedRectPath(cardRect, 24))
                    {
                        GraphicsState state = g.Save();
                        g.SetClip(cardPath);
                        g.DrawImageUnscaled(blurredCard, cardRect.X, cardRect.Y);

                        using (LinearGradientBrush glassTint = new LinearGradientBrush(
                            cardRect,
                            Color.FromArgb(172, 13, 21, 38),
                            Color.FromArgb(206, 10, 15, 28),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(glassTint, cardRect);
                        }

                        Rectangle topSheenRect = new Rectangle(cardRect.X, cardRect.Y, cardRect.Width, 190);
                        using (LinearGradientBrush topSheen = new LinearGradientBrush(
                            topSheenRect,
                            Color.FromArgb(40, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(topSheen, topSheenRect);
                        }

                        g.Restore(state);

                        Rectangle innerSpecRect = new Rectangle(cardRect.X + 1, cardRect.Y + 1, cardRect.Width - 2, cardRect.Height - 2);
                        using (GraphicsPath innerPath = GetRoundedRectPath(innerSpecRect, 23))
                        using (Pen innerPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1.1f))
                        {
                            g.DrawPath(innerPen, innerPath);
                        }

                        using (Pen neonBorder = new Pen(Color.FromArgb(240, 225, 29, 72), 2.0f))
                        {
                            g.DrawPath(neonBorder, cardPath);
                        }
                    }

                    DrawFrostedLogoBadge(g, bgFull, logoBadgeRect, 28, 152, 122, false);
                }
            }
        }

        private void DrawFrostedLogoBadge(Graphics g, Bitmap bgFull, Rectangle badgeRect, int radius, int topAlpha, int bottomAlpha, bool hasCrimsonOuterGlow)
        {
            if (hasCrimsonOuterGlow)
            {
                for (int i = 6; i >= 1; i--)
                {
                    int exp = i * 2;
                    Rectangle gr = new Rectangle(badgeRect.X - exp, badgeRect.Y - exp, badgeRect.Width + exp * 2, badgeRect.Height + exp * 2);
                    using (GraphicsPath gp = GetRoundedRectPath(gr, radius + exp))
                    using (Pen gPen = new Pen(Color.FromArgb(14 + (6 - i) * 8, 239, 68, 68), 3.0f))
                    {
                        g.DrawPath(gPen, gp);
                    }
                }
            }
            else
            {
                Rectangle shadowRect = new Rectangle(badgeRect.X - 2, badgeRect.Y - 1, badgeRect.Width + 4, badgeRect.Height + 5);
                using (GraphicsPath sp = GetRoundedRectPath(shadowRect, radius + 2))
                using (Pen sPen = new Pen(Color.FromArgb(50, 0, 0, 0), 4.0f))
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

                using (Pen rimPen = new Pen(Color.FromArgb(225, 255, 255, 255), 1.8f))
                {
                    g.DrawPath(rimPen, badgePath);
                }
            }
        }
        /// <summary>Returns to the login screen on logout; follows the window across theme reloads.</summary>
        private void AttachMainForm(MainForm mainForm)
        {
            mainForm.FormClosed += (s, args) =>
            {
                if (mainForm.IsReloading && mainForm.Replacement != null)
                {
                    AttachMainForm(mainForm.Replacement);
                    return;
                }
                if (mainForm.IsLoggingOut && !this.IsDisposed)
                {
                    ResetForReLogin();
                    this.Show();
                    this.Activate();
                }
                else
                {
                    this.Close();
                }
            };
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

            Rectangle titleRect = new Rectangle(cardRect.X + 20, cardRect.Y + 150, cardRect.Width - 40, 52);
            using (Font titleFont = new Font("Segoe UI", 22F, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "Login", titleFont, titleRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            Rectangle subRect = new Rectangle(cardRect.X + 20, cardRect.Y + 202, cardRect.Width - 40, 28);
            using (Font subFont = new Font("Segoe UI", 10F))
            {
                TextRenderer.DrawText(g, "Welcome back! Please login to your account", subFont, subRect, Color.FromArgb(195, 208, 225), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            if (pnlAlert.Visible && !string.IsNullOrEmpty(lblError.Text))
            {
                using (GraphicsPath alertPath = GetRoundedRectPath(alertRect, 9))
                {
                    Color fillClr = isAlertSuccess ? Color.FromArgb(195, 6, 78, 59) : Color.FromArgb(200, 88, 28, 28);
                    Color borderClr = isAlertSuccess ? Color.FromArgb(52, 211, 153) : Color.FromArgb(239, 68, 68);
                    Color textClr = isAlertSuccess ? Color.FromArgb(167, 243, 208) : Color.FromArgb(254, 202, 202);
                    using (SolidBrush br = new SolidBrush(fillClr))
                    {
                        g.FillPath(br, alertPath);
                    }
                    using (Pen p = new Pen(borderClr, 1.3f))
                    {
                        g.DrawPath(p, alertPath);
                    }
                    using (Font errFont = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(g, lblError.Text, errFont, alertRect, textClr, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                    }
                }
            }

            using (Font lblFont = new Font("Segoe UI", 9.5F, FontStyle.Regular))
            {
                Rectangle userLblRect = new Rectangle(userRect.X + 2, userRect.Y - 26, 300, 24);
                Rectangle passLblRect = new Rectangle(passRect.X + 2, passRect.Y - 26, 300, 24);
                TextRenderer.DrawText(g, "Username", lblFont, userLblRect, Color.FromArgb(215, 225, 238), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                TextRenderer.DrawText(g, "Password", lblFont, passLblRect, Color.FromArgb(215, 225, 238), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            DrawInputPill(g, userRect, txtUsername != null && txtUsername.Focused, true);
            DrawInputPill(g, passRect, txtPassword != null && txtPassword.Focused, false);

            DrawEyeVectorIcon(g, eyeRect, isPasswordRevealed, isHoverEye);

            using (GraphicsPath chkPath = GetRoundedRectPath(chkBoxRect, 5))
            {
                if (chkRememberUsername.Checked)
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillPath(br, chkPath);
                    }
                    using (Pen p = new Pen(Color.FromArgb(248, 113, 113), 1.3f))
                    {
                        g.DrawPath(p, chkPath);
                    }
                    using (Pen checkPen = new Pen(Color.White, 2.2f))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        g.DrawLines(checkPen, new Point[]
                        {
                            new Point(chkBoxRect.X + 4, chkBoxRect.Y + 10),
                            new Point(chkBoxRect.X + 8, chkBoxRect.Y + 14),
                            new Point(chkBoxRect.X + 15, chkBoxRect.Y + 6)
                        });
                    }
                }
                else
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(175, 22, 31, 48)))
                    {
                        g.FillPath(br, chkPath);
                    }
                    using (Pen p = new Pen(Color.FromArgb(115, 132, 155), 1.3f))
                    {
                        g.DrawPath(p, chkPath);
                    }
                }
            }

            using (Font optFont = new Font("Segoe UI", 9.5F))
            {
                Rectangle remTextRect = new Rectangle(chkBoxRect.Right + 9, chkHitRect.Y, 175, chkHitRect.Height);
                TextRenderer.DrawText(g, "Remember Me", optFont, remTextRect, Color.FromArgb(215, 225, 238), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            using (Font linkFont = new Font("Segoe UI", 9.5F, isHoverForgot ? FontStyle.Underline : FontStyle.Regular))
            {
                Color linkClr = isHoverForgot ? Color.White : Color.FromArgb(250, 120, 120);
                TextRenderer.DrawText(g, "Forgot Password?", linkFont, forgotRect, linkClr, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            using (GraphicsPath btnPath = GetRoundedRectPath(loginBtnRect, 14))
            {
                Color cTop = !btnLogin.Enabled ? Color.FromArgb(100, 116, 139) : (isHoverLogin ? Color.FromArgb(239, 68, 68) : Color.FromArgb(215, 34, 34));
                Color cBot = !btnLogin.Enabled ? Color.FromArgb(71, 85, 105) : (isHoverLogin ? Color.FromArgb(185, 28, 28) : Color.FromArgb(153, 27, 27));
                using (LinearGradientBrush btnBr = new LinearGradientBrush(loginBtnRect, cTop, cBot, LinearGradientMode.Vertical))
                {
                    g.FillPath(btnBr, btnPath);
                }
                using (Pen btnRim = new Pen(Color.FromArgb(105, 255, 255, 255), 1.2f))
                {
                    g.DrawPath(btnRim, btnPath);
                }
                using (Font btnFont = new Font("Segoe UI", 12F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "LOGIN", btnFont, loginBtnRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                }
            }

            using (Font authFont = new Font("Segoe UI", 9.5F))
            {
                string authText = "Authorized Personnel Only";
                Size textSz = TextRenderer.MeasureText(g, authText, authFont);
                int totalAuthW = 20 + textSz.Width;
                int startX = cardRect.X + (cardRect.Width - totalAuthW) / 2;
                int authY = cardRect.Y + 562;
                DrawSmallLockVectorIcon(g, new Rectangle(startX, authY + 5, 15, 17), Color.FromArgb(175, 190, 210));
                Rectangle authTextRect = new Rectangle(startX + 20, authY, textSz.Width + 20, 28);
                TextRenderer.DrawText(g, authText, authFont, authTextRect, Color.FromArgb(175, 190, 210), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            Rectangle copyRect = new Rectangle(cardRect.X + 20, cardRect.Y + 596, cardRect.Width - 40, 26);
            using (Font copyFont = new Font("Segoe UI", 8.5F))
            {
                TextRenderer.DrawText(g, "© M.J. Billones Transport Service • All rights reserved.", copyFont, copyRect, Color.FromArgb(135, 150, 172), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            DrawWindowControlButton(g, minBtnRect, isHoverMin, false);
            DrawWindowControlButton(g, closeBtnRect, isHoverClose, true);
        }

        private void DrawInputPill(Graphics g, Rectangle rect, bool isFocused, bool isUserIcon)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, 12))
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(22, 31, 48)))
                {
                    g.FillPath(br, path);
                }

                if (isFocused)
                {
                    Rectangle glowRect = new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                    using (GraphicsPath gp = GetRoundedRectPath(glowRect, 14))
                    using (Pen glowPen = new Pen(Color.FromArgb(75, 239, 68, 68), 2.8f))
                    {
                        g.DrawPath(glowPen, gp);
                    }
                }

                Color borderClr = isFocused ? Color.FromArgb(239, 68, 68) : Color.FromArgb(82, 98, 120);
                using (Pen p = new Pen(borderClr, isFocused ? 1.6f : 1.2f))
                {
                    g.DrawPath(p, path);
                }
            }

            Rectangle iconBox = new Rectangle(rect.X + 14, rect.Y + 13, 22, 22);
            Color iconClr = isFocused ? Color.FromArgb(248, 113, 113) : Color.FromArgb(158, 174, 196);
            if (isUserIcon)
            {
                DrawUserVectorIcon(g, iconBox, iconClr);
            }
            else
            {
                DrawLockVectorIcon(g, iconBox, iconClr);
            }
        }

        private void DrawUserVectorIcon(Graphics g, Rectangle r, Color c)
        {
            using (Pen p = new Pen(c, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                Rectangle head = new Rectangle(r.X + 6, r.Y + 2, 10, 10);
                g.DrawEllipse(p, head);
                Rectangle bodyArc = new Rectangle(r.X + 2, r.Y + 13, 18, 12);
                g.DrawArc(p, bodyArc, 180, 180);
            }
        }

        private void DrawLockVectorIcon(Graphics g, Rectangle r, Color c)
        {
            using (Pen p = new Pen(c, 1.7f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                Rectangle shackle = new Rectangle(r.X + 6, r.Y + 2, 10, 10);
                g.DrawArc(p, shackle, 180, 180);
                g.DrawLine(p, r.X + 6, r.Y + 7, r.X + 6, r.Y + 10);
                g.DrawLine(p, r.X + 16, r.Y + 7, r.X + 16, r.Y + 10);
                Rectangle body = new Rectangle(r.X + 3, r.Y + 10, 16, 11);
                using (GraphicsPath bp = GetRoundedRectPath(body, 3))
                {
                    g.DrawPath(p, bp);
                }
                using (SolidBrush br = new SolidBrush(c))
                {
                    g.FillEllipse(br, r.X + 9.5f, r.Y + 14f, 3f, 3f);
                }
            }
        }

        private void DrawSmallLockVectorIcon(Graphics g, Rectangle r, Color c)
        {
            using (Pen p = new Pen(c, 1.5f))
            {
                Rectangle shackle = new Rectangle(r.X + 3, r.Y + 1, 9, 8);
                g.DrawArc(p, shackle, 180, 180);
                Rectangle body = new Rectangle(r.X + 1, r.Y + 7, 13, 9);
                using (GraphicsPath bp = GetRoundedRectPath(body, 2))
                {
                    g.DrawPath(p, bp);
                }
            }
        }

        private void DrawEyeVectorIcon(Graphics g, Rectangle r, bool isRevealed, bool isHover)
        {
            Color c = isHover ? Color.White : Color.FromArgb(158, 174, 196);
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            using (Pen p = new Pen(c, 1.7f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                using (GraphicsPath eyePath = new GraphicsPath())
                {
                    eyePath.AddBezier(cx - 11, cy, cx - 5, cy - 8, cx + 5, cy - 8, cx + 11, cy);
                    eyePath.AddBezier(cx + 11, cy, cx + 5, cy + 8, cx - 5, cy + 8, cx - 11, cy);
                    g.DrawPath(p, eyePath);
                }
                g.DrawEllipse(p, cx - 3.5f, cy - 3.5f, 7f, 7f);
                if (!isRevealed)
                {
                    g.DrawLine(p, cx - 9, cy + 8, cx + 9, cy - 8);
                }
            }
        }

        private void DrawWindowControlButton(Graphics g, Rectangle rect, bool isHover, bool isClose)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, 9))
            {
                Color bg = isHover
                    ? (isClose ? Color.FromArgb(230, 220, 38, 38) : Color.FromArgb(210, 51, 65, 85))
                    : Color.FromArgb(165, 15, 23, 42);
                using (SolidBrush br = new SolidBrush(bg))
                {
                    g.FillPath(br, path);
                }
                using (Pen p = new Pen(Color.FromArgb(110, 255, 255, 255), 1.2f))
                {
                    g.DrawPath(p, path);
                }

                int cx = rect.X + rect.Width / 2;
                int cy = rect.Y + rect.Height / 2;
                using (Pen iconPen = new Pen(Color.White, 2.0f))
                {
                    iconPen.StartCap = LineCap.Round;
                    iconPen.EndCap = LineCap.Round;
                    if (isClose)
                    {
                        g.DrawLine(iconPen, cx - 5, cy - 5, cx + 5, cy + 5);
                        g.DrawLine(iconPen, cx + 5, cy - 5, cx - 5, cy + 5);
                    }
                    else
                    {
                        g.DrawLine(iconPen, cx - 6, cy, cx + 6, cy);
                    }
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
            catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
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
            catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
        }

        private string GetUserPrefPath()
        {
            // Kept in the data folder so a rebuild or reinstall doesn't forget it; an older copy in Assets is still read once.
            string p = System.IO.Path.Combine(AppPaths.DataRoot, "login_pref.dat");
            string old = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login_pref.dat");
            try { if (!System.IO.File.Exists(p) && System.IO.File.Exists(old)) System.IO.File.Copy(old, p); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            return p;
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
            txtPassword.PasswordChar = isPasswordRevealed ? '\0' : '●';
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
                string password = txtPassword.Text;
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("⚠️ Please enter both Username and Password.");
                    return;
                }

                int lockLeft = MJdatabase.GetLoginLockoutSecondsRemaining(username);
                if (lockLeft > 0)
                {
                    StartLockout(lockLeft, username, logEvent: false);
                    return;
                }
                var auth = MJdatabase.AuthenticateUserDetailed(username, password);

                if (auth.Result == AuthResult.InvalidCredentials && password != password.Trim())
                {
                    // Passwords are trimmed when they are set, so also accept the trimmed form of what was typed.
                    auth = MJdatabase.AuthenticateUserDetailed(username, password.Trim());
                }
                if (auth.Result == AuthResult.Success && auth.User != null)
                {
                    failedAttempts = 0;
                    SaveRememberedUsername(username);
                    MJdatabase.LogSecurityEvent(username, "LOGIN_SUCCESS", $"User successfully authenticated ({auth.User.Role})");

                    HideError();
                    this.Hide();
                    MainForm mainForm = new MainForm(auth.User);
                    AttachMainForm(mainForm);
                    mainForm.Show();
                }
                else if (auth.Result == AuthResult.Inactive)
                {
                    MJdatabase.LogSecurityEvent(username, "LOGIN_BLOCKED_INACTIVE", "Deactivated user attempted to login");
                    ShowError("⚠️ This account has been deactivated. Please contact the administrator.");
                }
                else if (auth.Result == AuthResult.PendingApproval)
                {
                    MJdatabase.LogSecurityEvent(username, "LOGIN_BLOCKED_PENDING", "Pending staff user attempted to login");
                    ShowError("⚠️ Account pending approval by Business Owner (Marie Ann J. Billones).");
                }
                else
                {
                    MJdatabase.LogSecurityEvent(username, "LOGIN_FAILED", "Invalid login attempt");
                    failedAttempts = Math.Max(failedAttempts + 1, MJdatabase.GetFailedLoginCountSinceReset(username));
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

        /// <summary>Picks up a logo changed in Settings while someone was signed in (the sign-in window is reused after log out).</summary>
        private void ReloadLogoIfChanged()
        {
            try
            {
                var custom = BrandAssets.CustomLogoCopy();
                Image? next = custom;
                if (next == null)
                {
                    foreach (var name in new[] { "logo_clean.png", "logo_transparent.png", "logo.png" })
                    {
                        string p = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", name);
                        if (System.IO.File.Exists(p)) { using var fs = new System.IO.FileStream(p, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite); using var img = Image.FromStream(fs); next = new Bitmap(img); break; }
                    }
                }
                if (next == null) return;
                var old = cleanLogoImage;
                cleanLogoImage = next;
                old?.Dispose();
                RebuildSceneBitmap();
                Invalidate(true);
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Login] Logo reload: {ex.Message}"); ErrLog.Swallowed(ex); }
        }
        private void ResetForReLogin()
        {
            ReloadLogoIfChanged();
            failedAttempts = 0;
            txtPassword.Clear();
            HideError();
            btnLogin.Enabled = true;
            txtUsername.Enabled = true;
            txtPassword.Enabled = true;
            this.ActiveControl = txtPassword;
        }
        private void StartLockout(int seconds, string username, bool logEvent = true)
        {
            lockoutSecondsRemaining = seconds;
            btnLogin.Enabled = false;
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;

            if (logEvent)
            {
                MJdatabase.LogSecurityEvent(username, "LOCKED_OUT", "Account locked for 60 seconds due to 5 consecutive failed attempts");
            }
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
            { ErrLog.Error("LoginForm", ex); 
                MessageBox.Show($"Unable to open password recovery dialog: {ex.Message}", "Recovery Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
