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
        private ComboBox cbAuditCategory = new ComboBox();
        private ComboBox cbFilterEvent = new ComboBox();
        private Button btnRefresh = new Button();
        private Button btnExport = new Button();
        private Label lblCount = new Label();
        private List<LoginAudit> rawLoginLogs = new List<LoginAudit>();
        private List<AuditTrailEntry> rawDataAuditLogs = new List<AuditTrailEntry>();

        public SecurityAuditView()
        {
            InitializeComponent();
            SetupGridColumns();
            DataExportHelper.AttachExportContextMenu(gridAudit, "Audit_Logs", "Security and Business Audit Trail");
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

            Label lblCat = new Label
            {
                Text = "Log Type:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbAuditCategory.Size = new Size(240, 26);
            cbAuditCategory.Margin = new Padding(0, 4, 16, 0);
            cbAuditCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAuditCategory.Items.AddRange(new object[]
            {
                "📋 Business Data Changes (Audit Trail)",
                "🛡️ Access & Authentication Logs"
            });
            cbAuditCategory.SelectedIndex = 0;
            cbAuditCategory.SelectedIndexChanged += (s, e) =>
            {
                UpdateEventFilterOptions();
                SetupGridColumns();
                ApplyFilter();
            };

            Label lblSearch = new Label
            {
                Text = "🔍 Search:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtSearch.Size = new Size(180, 26);
            txtSearch.Margin = new Padding(0, 4, 16, 0);
            txtSearch.PlaceholderText = "Search username, ID...";
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            Label lblFilter = new Label
            {
                Text = "Action / Event:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbFilterEvent.Size = new Size(160, 26);
            cbFilterEvent.Margin = new Padding(0, 4, 16, 0);
            cbFilterEvent.DropDownStyle = ComboBoxStyle.DropDownList;
            UpdateEventFilterOptions();
            cbFilterEvent.SelectedIndexChanged += (s, e) => ApplyFilter();

            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.Size = new Size(95, 28);
            btnRefresh.Margin = new Padding(0, 3, 8, 0);
            btnRefresh.BackColor = Color.FromArgb(241, 245, 249);
            btnRefresh.ForeColor = Color.FromArgb(51, 65, 85);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => LoadAuditLogs();

            btnExport.Text = "📊 Export";
            btnExport.Size = new Size(95, 28);
            btnExport.Margin = new Padding(0, 3, 16, 0);
            btnExport.BackColor = Color.FromArgb(15, 23, 42);
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += (s, e) =>
            {
                string title = cbAuditCategory.SelectedIndex == 0 ? "Business Data Change Audit Trail" : "System Access Logs";
                DataExportHelper.ExportToCsv(gridAudit, cbAuditCategory.SelectedIndex == 0 ? "AuditTrail" : "AccessLogs", title);
            };

            lblCount.AutoSize = true;
            lblCount.Margin = new Padding(0, 7, 0, 0);
            lblCount.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblCount.ForeColor = Color.FromArgb(71, 85, 105);
            lblCount.Text = "Logs: 0";

            pnlTop.Controls.AddRange(new Control[] { lblCat, cbAuditCategory, lblSearch, txtSearch, lblFilter, cbFilterEvent, btnRefresh, btnExport, lblCount });

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

        private void UpdateEventFilterOptions()
        {
            cbFilterEvent.Items.Clear();
            if (cbAuditCategory.SelectedIndex == 0)
            {
                cbFilterEvent.Items.AddRange(new object[]
                {
                    "All Actions",
                    "ADD",
                    "UPDATE",
                    "DELETE",
                    "INVOICE_CREATED",
                    "DISPATCH_CREATED",
                    "PAYMENT_COLLECTED",
                    "COMPLETED",
                    "CANCEL",
                    "SECURITY_UPDATE",
                    "PASSWORD_CHANGE"
                });
            }
            else
            {
                cbFilterEvent.Items.AddRange(new object[]
                {
                    "All Events",
                    "LOGIN_SUCCESS",
                    "LOGIN_FAILED",
                    "LOCKED_OUT",
                    "ACCOUNT_REGISTERED",
                    "USER_APPROVED",
                    "USER_REJECTED",
                    "PASSWORD_RESET_SUCCESS",
                    "PASSWORD_RESET_FAILED"
                });
            }
            cbFilterEvent.SelectedIndex = 0;
        }

        private void SetupGridColumns()
        {
            gridAudit.Columns.Clear();
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

            if (cbAuditCategory.SelectedIndex == 0)
            {
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Timestamp", HeaderText = "Timestamp", FillWeight = 14, MinimumWidth = 125, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" } });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "User", FillWeight = 11, MinimumWidth = 90 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ActionType", HeaderText = "Action", FillWeight = 13, MinimumWidth = 100 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TableName", HeaderText = "Module / Table", FillWeight = 12, MinimumWidth = 95 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordIdentifier", HeaderText = "Reference #", FillWeight = 15, MinimumWidth = 115 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FieldName", HeaderText = "Field Changed", FillWeight = 12, MinimumWidth = 95 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NewValue", HeaderText = "New Value", FillWeight = 12, MinimumWidth = 95 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReasonOrNotes", HeaderText = "Activity Notes", FillWeight = 25, MinimumWidth = 150 });
            }
            else
            {
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Timestamp", HeaderText = "Timestamp", FillWeight = 16, MinimumWidth = 120, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" } });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EventType", HeaderText = "Event", FillWeight = 16, MinimumWidth = 110 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", FillWeight = 16, MinimumWidth = 110 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IpOrMachine", HeaderText = "Workstation", FillWeight = 16, MinimumWidth = 110 });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Details", HeaderText = "Details", FillWeight = 36, MinimumWidth = 160 });
            }
        }

        private async void LoadAuditLogs()
        {
            try
            {
                if (cbAuditCategory.SelectedIndex == 0)
                {
                    rawDataAuditLogs = await MJdatabase.GetAuditTrailAsync(500);
                }
                else
                {
                    rawLoginLogs = await MJdatabase.GetSecurityAuditLogsAsync(300);
                }
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
            string filterItem = cbFilterEvent.SelectedItem?.ToString() ?? "";

            if (cbAuditCategory.SelectedIndex == 0)
            {
                var filtered = rawDataAuditLogs.Where(l =>
                {
                    bool matchQuery = string.IsNullOrEmpty(q) ||
                        (l.Username ?? "").ToLowerInvariant().Contains(q) ||
                        (l.TableName ?? "").ToLowerInvariant().Contains(q) ||
                        (l.RecordIdentifier ?? "").ToLowerInvariant().Contains(q) ||
                        (l.FieldName ?? "").ToLowerInvariant().Contains(q) ||
                        (l.NewValue ?? "").ToLowerInvariant().Contains(q) ||
                        (l.ReasonOrNotes ?? "").ToLowerInvariant().Contains(q);

                    bool matchAction = string.IsNullOrEmpty(filterItem) || filterItem == "All Actions" ||
                        (l.ActionType ?? "").Equals(filterItem, StringComparison.OrdinalIgnoreCase);

                    return matchQuery && matchAction;
                }).ToList();

                gridAudit.DataSource = filtered;
                lblCount.Text = $"Audit Records: {filtered.Count} of {rawDataAuditLogs.Count}";
            }
            else
            {
                var filtered = rawLoginLogs.Where(l =>
                {
                    bool matchQuery = string.IsNullOrEmpty(q) ||
                        (l.Username ?? "").ToLowerInvariant().Contains(q) ||
                        (l.IpOrMachine ?? "").ToLowerInvariant().Contains(q) ||
                        (l.Details ?? "").ToLowerInvariant().Contains(q);

                    bool matchEvent = string.IsNullOrEmpty(filterItem) || filterItem == "All Events" ||
                        (l.EventType ?? "").Equals(filterItem, StringComparison.OrdinalIgnoreCase);

                    return matchQuery && matchEvent;
                }).ToList();

                gridAudit.DataSource = filtered;
                lblCount.Text = $"Security Logs: {filtered.Count} of {rawLoginLogs.Count}";
            }
        }
    }
}
