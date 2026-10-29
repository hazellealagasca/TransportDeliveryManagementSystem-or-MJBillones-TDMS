using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class SystemCustomizationView : UserControl, IRefreshable
    {
        private User? currentUser;

        private TextBox txtCompanyName = null!;
        private TextBox txtCompanyTagline = null!;
        private TextBox txtCompanyContact = null!;
        private TextBox txtCompanyAddress = null!;
        private TextBox txtCompanyTin = null!;
        private TextBox txtInvoiceFooter = null!;

        private ComboBox cbDefaultDeparture = null!;
        private ComboBox cbRefreshInterval = null!;
        private CheckBox chkAllowMultiDest = null!;
        private CheckBox chkRequireCrates = null!;
        private CheckBox chkAutoTruck = null!;

        private ComboBox cbCurrency = null!;
        private ComboBox cbDateFormat = null!;
        private CheckBox chkEnableToasts = null!;
        private CheckBox chkStartCollapsed = null!;
        private CheckBox chkEnableProactiveAlerts = null!;
        private CheckBox chkNotifyLowTrucks = null!;

        private Button btnSave = null!;
        private Button btnAccountSecurity = null!;
        private Label lblStatus = null!;
        private bool isInitializingSettings = false;

        private PictureBox picLogoPreview = null!;
        private Label lblLogoSource = null!;
        private Button btnChangeLogo = null!;
        private Button btnDefaultLogo = null!;
        private CheckBox chkLightenLogo = null!;
        public SystemCustomizationView(User? user = null)
        {
            currentUser = user;
            InitializeComponent();
            LoadSettings();
            UIThemeHelper.ApplyTheme(this);
            RefreshLogoPreview();
            BrandAssets.LogoChanged += RefreshLogoPreview;
            Disposed += (s, e) => BrandAssets.LogoChanged -= RefreshLogoPreview;
        }

        public void RefreshData()
        {
            LoadSettings();
            RefreshLogoPreview();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Pal.C(248, 250, 252);
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9.5F);
            this.Padding = new Padding(24);
            this.AutoScroll = true;

            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Panel cardBranding = CreateCard("🏢 Business Profile & Invoice Branding", "Customize company information printed on delivery invoices, statements of account, and collection receipts.");
            TableLayoutPanel tblBranding = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                Margin = new Padding(0)
            };
            tblBranding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblBranding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            txtCompanyName = AddFormField(tblBranding, 0, 0, "Business Trading Name *:", "M.J. Billones Transport Service");
            txtCompanyTagline = AddFormField(tblBranding, 0, 1, "Official Tagline / Subtitle:", "Reliable Cold Chain & Cargo Logistics");
            txtCompanyContact = AddFormField(tblBranding, 1, 0, "Contact Telephone / Mobile *:", "0917-123-4567");
            txtCompanyTin = AddFormField(tblBranding, 1, 1, "TIN / Tax Identification #:", "123-456-789-000");
            txtCompanyAddress = AddFormField(tblBranding, 2, 0, "Principal Business Address *:", "Cavite / Batangas Operations Depot", true);
            txtInvoiceFooter = AddFormField(tblBranding, 3, 0, "Standard Terms / Invoice Footer Note:", "Thank you for your business! All deliveries are subject to standard transport terms.", true);

            AddLogoPicker(tblBranding, 8);
            cardBranding.Controls.Add(tblBranding);

            Panel cardOps = CreateCard("🚚 Operations & Dispatch Workflow Preferences", "Configure delivery assignment behaviors, scheduling defaults, and automatic vehicle recommendations.");
            TableLayoutPanel tblOps = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Margin = new Padding(0)
            };
            tblOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Label lblDep = new Label { Text = "Default Schedule Departure Time:", Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Pal.C(51, 65, 85), Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
            cbDefaultDeparture = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 12, 10) };
            cbDefaultDeparture.Items.AddRange(new object[] { "05:00 AM", "06:00 AM", "07:00 AM", "08:00 AM", "09:00 AM", "10:00 AM", "01:00 PM" });
            cbDefaultDeparture.SelectedIndex = 3;

            Label lblRef = new Label { Text = "Automatic Grid Refresh Rate:", Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Pal.C(51, 65, 85), Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
            cbRefreshInterval = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 0, 10) };
            cbRefreshInterval.Items.AddRange(new object[] { "Manual Refresh Only", "Every 30 Seconds", "Every 60 Seconds", "Every 5 Minutes" });
            cbRefreshInterval.SelectedIndex = 0;

            tblOps.Controls.Add(lblDep, 0, 0);
            tblOps.Controls.Add(cbDefaultDeparture, 0, 1);
            tblOps.Controls.Add(lblRef, 1, 0);
            tblOps.Controls.Add(cbRefreshInterval, 1, 1);

            chkAllowMultiDest = new CheckBox
            {
                Text = "Allow Multi-Drop Combined Destinations in 1 Dispatch Trip",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            chkRequireCrates = new CheckBox
            {
                Text = "Require Empty Crates Return Tracking on Trip Reports",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            chkAutoTruck = new CheckBox
            {
                Text = "Automatically Recommend Optimal Vehicle from Active Trucks",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            tblOps.Controls.Add(chkAllowMultiDest, 0, 2);
            tblOps.SetColumnSpan(chkAllowMultiDest, 2);
            tblOps.Controls.Add(chkRequireCrates, 0, 3);
            tblOps.SetColumnSpan(chkRequireCrates, 2);

            cardOps.Controls.Add(chkAutoTruck);
            cardOps.Controls.Add(tblOps);

            Panel cardDisplay = CreateCard("🎨 Display & Notification Preferences", "Configure localization formats, currency indicators, and application notification alerts.");
            TableLayoutPanel tblDisplay = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Margin = new Padding(0)
            };
            tblDisplay.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblDisplay.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Label lblCur = new Label { Text = "Currency Display Symbol:", Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Pal.C(51, 65, 85), Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
            cbCurrency = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 12, 10) };
            cbCurrency.Items.AddRange(new object[] { "PHP (Philippine Peso)", "₱ (Peso Symbol)", "USD ($)" });
            cbCurrency.SelectedIndex = 0;

            Label lblDate = new Label { Text = "Standard Date Display Format:", Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Pal.C(51, 65, 85), Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
            cbDateFormat = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 0, 10) };
            cbDateFormat.Items.AddRange(new object[] { "yyyy-MM-dd (ISO Standard)", "MM/dd/yyyy (US Standard)", "dd/MM/yyyy (UK Standard)", "MMMM dd, yyyy (Long Format)" });
            cbDateFormat.SelectedIndex = 0;

            chkEnableToasts = new CheckBox
            {
                Text = "Enable Floating Notification Toasts for Business Events",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            chkStartCollapsed = new CheckBox
            {
                Text = "Start Application with Navigation Sidebar Menu Collapsed",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            chkEnableProactiveAlerts = new CheckBox
            {
                Text = "Automated Proactive Overdue Invoices & Trip Alerts",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            chkNotifyLowTrucks = new CheckBox
            {
                Text = "Alert When Ready Available Trucks Fall to 1 Vehicle",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 6, 0, 6)
            };

            tblDisplay.Controls.Add(lblCur, 0, 0);
            tblDisplay.Controls.Add(cbCurrency, 0, 1);
            tblDisplay.Controls.Add(lblDate, 1, 0);
            tblDisplay.Controls.Add(cbDateFormat, 1, 1);
            tblDisplay.Controls.Add(chkEnableToasts, 0, 2);
            tblDisplay.Controls.Add(chkStartCollapsed, 1, 2);
            tblDisplay.Controls.Add(chkEnableProactiveAlerts, 0, 3);
            tblDisplay.Controls.Add(chkNotifyLowTrucks, 1, 3);

            cardDisplay.Controls.Add(tblDisplay);

            Panel cardAccount = CreateCard("🔒 My Account Security & Recovery", "Manage secret recovery question and personal password security.");
            FlowLayoutPanel flowAccount = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 4)
            };

            btnAccountSecurity = new Button
            {
                Text = "🔑 Configure Security Question & Password...",
                AutoSize = true,
                Height = 36,
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Padding = new Padding(14, 0, 14, 0)
            };
            btnAccountSecurity.FlatAppearance.BorderSize = 0;
            btnAccountSecurity.Click += (s, e) =>
            {
                if (currentUser != null)
                {
                    using (var secDlg = new SecuritySettingsDialog(currentUser))
                    {
                        secDlg.ShowDialog(this);
                    }
                }
            };
            flowAccount.Controls.Add(btnAccountSecurity);
            cardAccount.Controls.Add(flowAccount);

            Panel cardActions = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 12, 0, 24)
            };

            btnSave = new Button
            {
                Text = "💾 SAVE ALL CUSTOMIZATION PREFERENCES",
                Dock = DockStyle.Left,
                Width = 340,
                Height = 44,
                BackColor = Pal.C(185, 28, 28),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Pal.C(22, 101, 52),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Text = ""
            };

            cardActions.Controls.Add(lblStatus);
            cardActions.Controls.Add(btnSave);

            tblMain.Controls.Add(cardBranding, 0, 0);
            tblMain.Controls.Add(cardOps, 0, 1);
            tblMain.Controls.Add(cardDisplay, 0, 2);
            tblMain.Controls.Add(cardAccount, 0, 3);
            tblMain.Controls.Add(cardActions, 0, 4);
            chkAllowMultiDest.CheckedChanged += (s, e) => AutoSavePreferences();
            chkRequireCrates.CheckedChanged += (s, e) => AutoSavePreferences();
            chkAutoTruck.CheckedChanged += (s, e) => AutoSavePreferences();
            chkEnableToasts.CheckedChanged += (s, e) => AutoSavePreferences();
            chkStartCollapsed.CheckedChanged += (s, e) => AutoSavePreferences();
            chkEnableProactiveAlerts.CheckedChanged += (s, e) => AutoSavePreferences();
            chkNotifyLowTrucks.CheckedChanged += (s, e) => AutoSavePreferences();

            cbDefaultDeparture.SelectedIndexChanged += (s, e) => AutoSavePreferences();
            cbRefreshInterval.SelectedIndexChanged += (s, e) => AutoSavePreferences();
            cbCurrency.SelectedIndexChanged += (s, e) => AutoSavePreferences();
            cbDateFormat.SelectedIndexChanged += (s, e) => AutoSavePreferences();

            txtCompanyName.Leave += (s, e) => AutoSavePreferences();
            txtCompanyTagline.Leave += (s, e) => AutoSavePreferences();
            txtCompanyContact.Leave += (s, e) => AutoSavePreferences();
            txtCompanyTin.Leave += (s, e) => AutoSavePreferences();
            txtCompanyAddress.Leave += (s, e) => AutoSavePreferences();
            txtInvoiceFooter.Leave += (s, e) => AutoSavePreferences();

            this.Controls.Add(tblMain);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>Company logo: preview (as it looks on the sidebar), change, back to default.</summary>
        private void AddLogoPicker(TableLayoutPanel parent, int row)
        {
            Label lbl = new Label
            {
                Text = "Company Logo:",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(51, 65, 85),
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 2)
            };
            var line = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 10) };
            picLogoPreview = new PictureBox { Size = new Size(240, 84), SizeMode = PictureBoxSizeMode.Zoom, Padding = new Padding(14, 10, 14, 10), Margin = new Padding(0, 0, 16, 0), BorderStyle = BorderStyle.None };
            var side = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
            var buttons = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
            btnChangeLogo = new Button { Text = "Change logo…", AutoSize = true, MinimumSize = new Size(130, 34), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
            btnDefaultLogo = new Button { Text = "Use default logo", AutoSize = true, MinimumSize = new Size(140, 34), Cursor = Cursors.Hand, Margin = new Padding(0) };
            UIThemeHelper.SetButtonRole(btnChangeLogo, "primary");
            UIThemeHelper.SetButtonRole(btnDefaultLogo, "secondary");
            btnChangeLogo.Click += (s, e) => ChooseLogo();
            btnDefaultLogo.Click += (s, e) => UseDefaultLogo();
            buttons.Controls.Add(btnChangeLogo);
            buttons.Controls.Add(btnDefaultLogo);
            chkLightenLogo = new CheckBox { Text = "Turn black lettering white on the dark sidebar", AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 4), Font = new Font("Segoe UI", 9F) };
            chkLightenLogo.CheckedChanged += (s, e) =>
            {
                if (isInitializingSettings) return;
                try { BrandAssets.LightenCustomLogoOnDark = chkLightenLogo.Checked; } catch { }
            };
            lblLogoSource = new Label { AutoSize = true, MaximumSize = new Size(460, 0), Font = new Font("Segoe UI", 8.75F), ForeColor = Pal.C(100, 116, 139), Margin = new Padding(0) };
            side.Controls.Add(buttons);
            side.Controls.Add(chkLightenLogo);
            side.Controls.Add(lblLogoSource);
            line.Controls.Add(picLogoPreview);
            line.Controls.Add(side);
            parent.Controls.Add(lbl, 0, row);
            parent.Controls.Add(line, 0, row + 1);
            parent.SetColumnSpan(lbl, 2);
            parent.SetColumnSpan(line, 2);
            if (!SessionContext.IsAdmin)
            {
                btnChangeLogo.Enabled = false;
                btnDefaultLogo.Enabled = false;
                chkLightenLogo.Enabled = false;
            }
        }
        private void RefreshLogoPreview()
        {
            if (IsDisposed || picLogoPreview == null) return;
            if (InvokeRequired) { BeginInvoke(new Action(RefreshLogoPreview)); return; }
            bool custom = BrandAssets.HasCustomLogo;
            picLogoPreview.BackColor = AppTheme.Sidebar;
            picLogoPreview.Image = BrandAssets.LogoFor(AppTheme.Sidebar);
            btnDefaultLogo.Visible = custom;
            isInitializingSettings = true;
            chkLightenLogo.Checked = BrandAssets.LightenCustomLogoOnDark;
            isInitializingSettings = false;
            chkLightenLogo.Visible = custom && AppTheme.SidebarIsDark;
            string where = "Shown on the sign-in screen, the side menu and every printed form (invoices, statements, receipts, trip reports).";
            lblLogoSource.Text = (custom ? "Using your uploaded logo. " : "Using the built-in M.J. Billones logo. ")
                + where + (SessionContext.IsAdmin ? " A PNG with a transparent background looks best." : " Only the administrator can change it.");
        }
        private void ChooseLogo()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choose the company logo",
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            Cursor = Cursors.WaitCursor;
            string? error;
            try { error = BrandAssets.SetCustomLogo(dlg.FileName); }
            finally { Cursor = Cursors.Default; }
            if (error != null)
            {
                MessageBox.Show(FindForm(), error, "Logo not changed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { MJdatabase.LogSecurityEvent(currentUser?.Username ?? "", "SETTINGS", "Company logo changed (" + System.IO.Path.GetFileName(dlg.FileName) + ")"); } catch { }
            EventNotificationService.Publish("Logo updated", "The new company logo is now used on screen and on printed forms.", ToastType.Success, this);
        }
        private void UseDefaultLogo()
        {
            if (MessageBox.Show(FindForm(), "Remove the uploaded logo and go back to the built-in M.J. Billones logo?", "Use default logo",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try { BrandAssets.ResetToDefaultLogo(); }
            catch (Exception ex) { MessageBox.Show(FindForm(), "The logo could not be reset: " + ex.Message, "Logo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { MJdatabase.LogSecurityEvent(currentUser?.Username ?? "", "SETTINGS", "Company logo reset to default"); } catch { }
            EventNotificationService.Publish("Logo updated", "The built-in M.J. Billones logo is back in use.", ToastType.Success, this);
        }
        private Panel CreateCard(string title, string subtitle)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Pal.White,
                Padding = new Padding(20, 16, 20, 16),
                Margin = new Padding(0, 0, 0, 16)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 2)
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Pal.C(100, 116, 139),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 12)
            };

            card.Controls.Add(lblSubtitle);
            card.Controls.Add(lblTitle);
            return card;
        }

        private TextBox AddFormField(TableLayoutPanel parent, int row, int col, string label, string defaultVal, bool spanTwo = false)
        {
            Label lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(51, 65, 85),
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 2)
            };

            TextBox txt = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                Text = defaultVal,
                Margin = new Padding(0, 0, col == 0 && !spanTwo ? 12 : 0, 10),
                MaxLength = 200
            };

            parent.Controls.Add(lbl, col, row * 2);
            parent.Controls.Add(txt, col, (row * 2) + 1);

            if (spanTwo)
            {
                parent.SetColumnSpan(lbl, 2);
                parent.SetColumnSpan(txt, 2);
            }

            return txt;
        }

        private void LoadSettings()
        {
            isInitializingSettings = true;
            try
            {
                txtCompanyName.Text = MJdatabase.GetSystemSetting("CompanyName", "M.J. Billones Transport Service");
                txtCompanyTagline.Text = MJdatabase.GetSystemSetting("CompanyTagline", "Reliable Cold Chain & Cargo Logistics");
                txtCompanyContact.Text = MJdatabase.GetSystemSetting("CompanyContact", "0917-123-4567");
                txtCompanyTin.Text = MJdatabase.GetSystemSetting("CompanyTin", "708-839-875-000");
                txtCompanyAddress.Text = MJdatabase.GetSystemSetting("CompanyAddress", "Jasmin St., San Francisco, Tarlac City");
                txtInvoiceFooter.Text = MJdatabase.GetSystemSetting("InvoiceFooter", "Thank you for your business! All deliveries are subject to standard transport terms.");

                string dep = MJdatabase.GetSystemSetting("DefaultDepartureTime", "08:00 AM");
                int depIdx = cbDefaultDeparture.Items.IndexOf(dep);
                if (depIdx >= 0) cbDefaultDeparture.SelectedIndex = depIdx;

                string refRate = MJdatabase.GetSystemSetting("AutoRefreshRate", "Manual Refresh Only");
                int refIdx = cbRefreshInterval.Items.IndexOf(refRate);
                if (refIdx >= 0) cbRefreshInterval.SelectedIndex = refIdx;

                string multi = MJdatabase.GetSystemSetting("AllowMultiDestDispatch", "True");
                chkAllowMultiDest.Checked = multi.Equals("True", StringComparison.OrdinalIgnoreCase) || multi == "1";

                string crates = MJdatabase.GetSystemSetting("RequireCrateReturnTracking", "True");
                chkRequireCrates.Checked = crates.Equals("True", StringComparison.OrdinalIgnoreCase) || crates == "1";

                string autoTruck = MJdatabase.GetSystemSetting("AutoTruckRecommendation", "True");
                chkAutoTruck.Checked = autoTruck.Equals("True", StringComparison.OrdinalIgnoreCase) || autoTruck == "1";

                string cur = MJdatabase.GetSystemSetting("CurrencySymbol", "PHP (Philippine Peso)");
                int curIdx = cbCurrency.Items.IndexOf(cur);
                if (curIdx >= 0) cbCurrency.SelectedIndex = curIdx;

                string dFmt = MJdatabase.GetSystemSetting("DateFormat", "yyyy-MM-dd (ISO Standard)");
                int dIdx = cbDateFormat.Items.IndexOf(dFmt);
                if (dIdx >= 0) cbDateFormat.SelectedIndex = dIdx;

                string toasts = MJdatabase.GetSystemSetting("EnableToastPopups", "True");
                chkEnableToasts.Checked = toasts.Equals("True", StringComparison.OrdinalIgnoreCase) || toasts == "1";

                string startCol = MJdatabase.GetSystemSetting("StartMenuCollapsed", "True");
                chkStartCollapsed.Checked = startCol.Equals("True", StringComparison.OrdinalIgnoreCase) || startCol == "1";

                string proAlerts = MJdatabase.GetSystemSetting("EnableProactiveAlerts", "True");
                chkEnableProactiveAlerts.Checked = proAlerts.Equals("True", StringComparison.OrdinalIgnoreCase) || proAlerts == "1";

                string lowTrucks = MJdatabase.GetSystemSetting("NotifyLowTrucks", "True");
                chkNotifyLowTrucks.Checked = lowTrucks.Equals("True", StringComparison.OrdinalIgnoreCase) || lowTrucks == "1";
            }
            catch { }
            finally
            {
                isInitializingSettings = false;
            }
        }

        private void AutoSavePreferences(bool notify = false)
        {
            if (isInitializingSettings) return;
            try
            {
                MJdatabase.SetSystemSetting("CompanyName", txtCompanyName.Text.Trim(), "Official business name");
                MJdatabase.SetSystemSetting("CompanyTagline", txtCompanyTagline.Text.Trim(), "Company slogan");
                MJdatabase.SetSystemSetting("CompanyContact", txtCompanyContact.Text.Trim(), "Company contact number");
                MJdatabase.SetSystemSetting("CompanyTin", txtCompanyTin.Text.Trim(), "Tax identification number");
                MJdatabase.SetSystemSetting("CompanyAddress", txtCompanyAddress.Text.Trim(), "Depot and warehouse address");
                MJdatabase.SetSystemSetting("InvoiceFooter", txtInvoiceFooter.Text.Trim(), "Footer note on printouts");

                MJdatabase.SetSystemSetting("DefaultDepartureTime", cbDefaultDeparture.SelectedItem?.ToString() ?? "08:00 AM", "Default dispatch departure time");
                MJdatabase.SetSystemSetting("AutoRefreshRate", cbRefreshInterval.SelectedItem?.ToString() ?? "Manual Refresh Only", "Table grid refresh frequency");
                MJdatabase.SetSystemSetting("AllowMultiDestDispatch", chkAllowMultiDest.Checked ? "True" : "False", "Multi-drop dispatch preference");
                MJdatabase.SetSystemSetting("RequireCrateReturnTracking", chkRequireCrates.Checked ? "True" : "False", "Empty crate return tracking");
                MJdatabase.SetSystemSetting("AutoTruckRecommendation", chkAutoTruck.Checked ? "True" : "False", "Truck recommendation helper");
                MJdatabase.SetSystemSetting("CurrencySymbol", cbCurrency.SelectedItem?.ToString() ?? "PHP (Philippine Peso)", "Default currency representation");
                MJdatabase.SetSystemSetting("DateFormat", cbDateFormat.SelectedItem?.ToString() ?? "yyyy-MM-dd (ISO Standard)", "Standard date format");
                MJdatabase.SetSystemSetting("EnableToastPopups", chkEnableToasts.Checked ? "True" : "False", "Enable notification toast popups");
                MJdatabase.SetSystemSetting("StartMenuCollapsed", chkStartCollapsed.Checked ? "True" : "False", "Start sidebar menu in collapsed state");
                MJdatabase.SetSystemSetting("EnableProactiveAlerts", chkEnableProactiveAlerts.Checked ? "True" : "False", "Automated truck & aging invoice alerts");
                MJdatabase.SetSystemSetting("NotifyLowTrucks", chkNotifyLowTrucks.Checked ? "True" : "False", "Alert when available trucks fall to 1 vehicle");

                lblStatus.Text = "⚡ Auto-saved!";
                lblStatus.ForeColor = Pal.C(22, 101, 52);
                if (notify)
                {
                    EventNotificationService.Publish("Preferences Saved", "System customization preferences updated successfully.", ToastType.Success, this);
                }
            }
            catch { }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                AutoSavePreferences(true);

                lblStatus.Text = "✅ System customization preferences successfully saved!";
                lblStatus.ForeColor = Pal.C(22, 101, 52);
                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 3500 };
                t.Tick += (ts, te) =>
                {
                    lblStatus.Text = "";
                    t.Stop();
                    t.Dispose();
                };
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save preferences: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
