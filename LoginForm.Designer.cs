namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
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
            txtUsername.Font = new Font("Segoe UI", 11F);
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(270, 315);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(356, 22);
            txtUsername.TabIndex = 1;
            txtPassword.BackColor = Color.FromArgb(22, 31, 48);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.ForeColor = Color.White;
            txtPassword.Location = new Point(270, 399);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.Size = new Size(318, 22);
            txtPassword.TabIndex = 2;
            pnlAlert.Visible = false;
            lblError.Text = "";
            chkRememberUsername.Checked = false;
            btnLogin.Enabled = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(1366, 768);
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
