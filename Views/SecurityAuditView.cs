using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class SecurityAuditView : UserControl, IRefreshable
    {
        private DataGridView gridAudit = new DataGridView();
        private TextBox txtSearch = new TextBox();
        private ComboBox cbFilterEvent = new ComboBox();
        private Button btnRefresh = new Button();
        private Label lblCount = new Label();
        private List<LoginAudit> rawLogs = new List<LoginAudit>();

        public SecurityAuditView()
        {
            InitializeComponent();
            SetupGrid();
            LoadAuditLogs();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadAuditLogs();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F);

            FlowLayoutPanel pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };

            Label lblSearch = new Label
            {
                Text = "🔍 Search:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtSearch.Size = new Size(200, 26);
            txtSearch.Margin = new Padding(0, 4, 16, 0);
            txtSearch.PlaceholderText = "Username or machine...";
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            Label lblFilter = new Label
            {
                Text = "Event Type:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbFilterEvent.Size = new Size(180, 26);
            cbFilterEvent.Margin = new Padding(0, 4, 16, 0);
            cbFilterEvent.DropDownStyle = ComboBoxStyle.DropDownList;
            cbFilterEvent.Items.AddRange(new object[] { "All Events", "LOGIN_SUCCESS", "LOGIN_FAILED", "LOCKED_OUT", "ACCOUNT_REGISTERED", "USER_APPROVED", "USER_REJECTED" });
            cbFilterEvent.SelectedIndex = 0;
            cbFilterEvent.SelectedIndexChanged += (s, e) => ApplyFilter();

            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.Size = new Size(95, 28);
            btnRefresh.Margin = new Padding(0, 3, 16, 0);
            btnRefresh.BackColor = Color.FromArgb(241, 245, 249);
            btnRefresh.ForeColor = Color.FromArgb(51, 65, 85);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => LoadAuditLogs();

            lblCount.AutoSize = true;
            lblCount.Margin = new Padding(0, 7, 0, 0);
            lblCount.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblCount.ForeColor = Color.FromArgb(71, 85, 105);
            lblCount.Text = "Logs: 0";

            pnlTop.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblFilter, cbFilterEvent, btnRefresh, lblCount });

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };

            gridAudit.Dock = DockStyle.Fill;
            pnlGridCard.Controls.Add(gridAudit);

            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlGridCard);
            pnlGridCard.SendToBack();
        }

        private void SetupGrid()
        {
            gridAudit.ReadOnly = true;
            gridAudit.AllowUserToAddRows = false;
            gridAudit.AllowUserToDeleteRows = false;
            gridAudit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAudit.MultiSelect = false;
            gridAudit.BackgroundColor = Color.White;
            gridAudit.BorderStyle = BorderStyle.None;
            gridAudit.RowHeadersVisible = false;
            gridAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAudit.RowTemplate.Height = 32;

            gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Timestamp", HeaderText = "Timestamp", FillWeight = 16, MinimumWidth = 120, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" } });
            gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EventType", HeaderText = "Event", FillWeight = 16, MinimumWidth = 110 });
            gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", FillWeight = 16, MinimumWidth = 110 });
            gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IpOrMachine", HeaderText = "Workstation", FillWeight = 16, MinimumWidth = 110 });
            gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Details", HeaderText = "Details", FillWeight = 36, MinimumWidth = 160 });

            gridAudit.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridAudit.Columns[e.ColumnIndex].HeaderText == "Security Event" && e.Value != null)
                {
                    string ev = e.Value.ToString() ?? "";
                    if (ev == "LOGIN_SUCCESS" || ev == "USER_APPROVED")
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                        e.CellStyle.Font = new Font(gridAudit.Font, FontStyle.Bold);
                    }
                    else if (ev == "LOGIN_FAILED" || ev == "LOCKED_OUT" || ev == "USER_REJECTED")
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                        e.CellStyle.Font = new Font(gridAudit.Font, FontStyle.Bold);
                    }
                    else if (ev == "ACCOUNT_REGISTERED")
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9);
                        e.CellStyle.Font = new Font(gridAudit.Font, FontStyle.Bold);
                    }
                }
            };
        }

        private async void LoadAuditLogs()
        {
            try
            {
                rawLogs = await MJdatabase.GetSecurityAuditLogsAsync(300);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load audit logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            string q = txtSearch.Text.Trim().ToLowerInvariant();
            string filterEvent = cbFilterEvent.SelectedItem?.ToString() ?? "All Events";

            var filtered = rawLogs.Where(l =>
            {
                bool matchQuery = string.IsNullOrEmpty(q) ||
                    l.Username.ToLowerInvariant().Contains(q) ||
                    l.IpOrMachine.ToLowerInvariant().Contains(q) ||
                    l.Details.ToLowerInvariant().Contains(q);

                bool matchEvent = filterEvent == "All Events" || l.EventType.Equals(filterEvent, StringComparison.OrdinalIgnoreCase);

                return matchQuery && matchEvent;
            }).ToList();

            gridAudit.DataSource = filtered;
            lblCount.Text = $"Total Security Logs: {filtered.Count} of {rawLogs.Count}";
        }
    }
}
