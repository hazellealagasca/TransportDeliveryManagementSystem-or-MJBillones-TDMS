using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class DispatchRulesSettingsView : UserControl, IRefreshable
    {
        private CheckBox chkAllowMultiDest = null!;
        private CheckBox chkRequireCrateReturn = null!;
        private CheckBox chkAutoFillCrates = null!;
        private Label lblStatusToast = null!;
        private Button btnSave = null!;

        public DispatchRulesSettingsView()
        {
            InitializeComponent();
            LoadSettings();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 10F);
            this.Padding = new Padding(24);
            this.AutoScroll = true;

            Panel pnlCard = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 0, 16)
            };

            TableLayoutPanel tblLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 8
            };
            tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label lblCardTitle = new Label
            {
                Text = "🚚 Operations & Dispatch Routing Rules",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblCardSubtitle = new Label
            {
                Text = "Configure multi-destination delivery trip parameters, loading schedule flexibility, and dispatch assignment behaviors.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };

            chkAllowMultiDest = new CheckBox
            {
                Text = "Allow Multiple Destinations in 1 Dispatch Trip (Multi-Drop Combined Delivery)",
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6)
            };

            Panel pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding = new Padding(14, 12, 14, 12),
                Margin = new Padding(0, 0, 0, 16)
            };

            Label lblInfo = new Label
            {
                Text = "ℹ️ When enabled: Loading Schedule Management accepts multi-drop destinations (e.g., 'TRECE / DASMARINAS / TAGAYTAY') and Delivery Dispatch allows combining multiple destination loading orders into 1 single truck dispatch trip without duplicate assignment lockouts.\n\nWhen disabled: Every dispatch trip is strictly constrained to a single destination and 1:1 loading schedule.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138),
                Dock = DockStyle.Top,
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblInfo);

            chkRequireCrateReturn = new CheckBox
            {
                Text = "Require Empty Crates Return Tracking on Trip Documentation",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6)
            };

            chkAutoFillCrates = new CheckBox
            {
                Text = "Automatically Recommend Optimal Truck based on Total Volume (G-Crates + E-Crates)",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 18)
            };

            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            btnSave = new Button
            {
                Text = "💾  Save Dispatch Preferences",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 40,
                Width = 240,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            lblStatusToast = new Label
            {
                Text = "",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            flowButtons.Controls.Add(btnSave);
            flowButtons.Controls.Add(lblStatusToast);

            tblLayout.Controls.Add(lblCardTitle, 0, 0);
            tblLayout.Controls.Add(lblCardSubtitle, 0, 1);
            tblLayout.Controls.Add(chkAllowMultiDest, 0, 2);
            tblLayout.Controls.Add(pnlInfo, 0, 3);
            tblLayout.Controls.Add(chkRequireCrateReturn, 0, 4);
            tblLayout.Controls.Add(chkAutoFillCrates, 0, 5);
            tblLayout.Controls.Add(flowButtons, 0, 6);

            pnlCard.Controls.Add(tblLayout);
            this.Controls.Add(pnlCard);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadSettings()
        {
            try
            {
                string multiDest = MJdatabase.GetSystemSetting("AllowMultiDestDispatch", "True");
                chkAllowMultiDest.Checked = multiDest.Equals("True", StringComparison.OrdinalIgnoreCase) || multiDest == "1";

                string reqCrates = MJdatabase.GetSystemSetting("RequireCrateReturnTracking", "True");
                chkRequireCrateReturn.Checked = reqCrates.Equals("True", StringComparison.OrdinalIgnoreCase) || reqCrates == "1";

                string autoRec = MJdatabase.GetSystemSetting("AutoFleetRecommendation", "True");
                chkAutoFillCrates.Checked = autoRec.Equals("True", StringComparison.OrdinalIgnoreCase) || autoRec == "1";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load system settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                MJdatabase.SetSystemSetting("AllowMultiDestDispatch", chkAllowMultiDest.Checked ? "True" : "False", "Allow multiple destinations in 1 dispatch trip");
                MJdatabase.SetSystemSetting("RequireCrateReturnTracking", chkRequireCrateReturn.Checked ? "True" : "False", "Require empty crates return tracking");
                MJdatabase.SetSystemSetting("AutoFleetRecommendation", chkAutoFillCrates.Checked ? "True" : "False", "Automatically recommend optimal truck");

                EventNotificationService.Publish("Dispatch Rules Saved", "Dispatch routing preferences updated successfully.", ToastType.Success, this);

                lblStatusToast.Text = "✅ Dispatch routing preferences successfully saved!";
                lblStatusToast.ForeColor = Color.FromArgb(22, 101, 52);

                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 3500 };
                t.Tick += (ts, te) =>
                {
                    lblStatusToast.Text = "";
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
