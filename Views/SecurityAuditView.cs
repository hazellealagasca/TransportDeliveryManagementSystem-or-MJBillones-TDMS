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
            RowMenu.AttachBasic(gridAudit, "Audit_Logs", "Security and Business Audit Trail");
            LoadAuditLogs();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadAuditLogs();
        }

        private PageHeader header = null!;
        private ChoiceChip chipCategory = null!, chipDataAction = null!, chipAuthEvent = null!;
        private static readonly string[] DataActionLabels = { "All actions", "Added", "Updated", "Deleted", "Invoice created", "Dispatch created", "Payment collected", "Completed", "Cancelled", "Security update", "Password change" };
        private static readonly string[] AuthEventLabels = { "All events", "Signed in", "Sign-in failed", "Locked out", "Account registered", "User approved", "User rejected", "Password reset", "Password reset failed" };
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.Canvas;
            this.Font = new Font("Segoe UI", 9.5F);

            // The two lists still run on these (not shown): the chips below pick their items.
            cbAuditCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAuditCategory.Items.AddRange(new object[] { "Business data changes", "Access & sign-in logs" });
            cbAuditCategory.SelectedIndex = 0;
            cbAuditCategory.SelectedIndexChanged += (s, e) =>
            {
                UpdateEventFilterOptions();
                SetupGridColumns();
                chipDataAction.Visible = cbAuditCategory.SelectedIndex == 0;
                chipAuthEvent.Visible = cbAuditCategory.SelectedIndex == 1;
                chipDataAction.SetQuietly(0);
                chipAuthEvent.SetQuietly(0);
                LoadAuditLogs(); // the other log is loaded when you switch to it
            };

            cbFilterEvent.DropDownStyle = ComboBoxStyle.DropDownList;
            header = PageKit.Header("Audit logs", "Every change to your records and every sign-in. Entries can never be changed or deleted.");
            header.Dock = DockStyle.Top;
            header.Padding = new Padding(D(12), header.Padding.Top, D(16), header.Padding.Bottom); // line up with the toolbar and list
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(D(16), D(4), D(16), D(8)),
                BackColor = Color.Transparent
            };

            chipCategory = new ChoiceChip("Log", new[] { "Data changes", "Sign-ins" }) { Margin = new Padding(0, 0, D(8), D(4)) };
            chipCategory.SelectionChanged += (s, e) => cbAuditCategory.SelectedIndex = chipCategory.SelectedIndex;
            PageKit.StyleSearch(txtSearch, "🔍 Search user, record or notes…");
            txtSearch.Width = D(260);
            txtSearch.MaxLength = 100;
            txtSearch.Margin = new Padding(0, D(4), D(8), D(4));
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            chipDataAction = new ChoiceChip("Action", DataActionLabels) { Margin = new Padding(0, 0, D(8), D(4)) };
            chipDataAction.SelectionChanged += (s, e) => { if (cbFilterEvent.Items.Count > chipDataAction.SelectedIndex) cbFilterEvent.SelectedIndex = chipDataAction.SelectedIndex; };
            chipAuthEvent = new ChoiceChip("Event", AuthEventLabels) { Margin = new Padding(0, 0, D(8), D(4)), Visible = false };
            chipAuthEvent.SelectionChanged += (s, e) => { if (cbFilterEvent.Items.Count > chipAuthEvent.SelectedIndex) cbFilterEvent.SelectedIndex = chipAuthEvent.SelectedIndex; };
            UpdateEventFilterOptions();
            cbFilterEvent.SelectedIndexChanged += (s, e) => ApplyFilter();

            btnRefresh = PageKit.Action("🔄 Refresh", "secondary", (s, e) => LoadAuditLogs());
            btnRefresh.MinimumSize = new Size(0, D(32));
            btnRefresh.Margin = new Padding(0, 0, D(8), D(4));
            btnExport = PageKit.Action("📊 Export", "secondary", (s, e) =>
            {
                string title = cbAuditCategory.SelectedIndex == 0 ? "Business Data Change Audit Trail" : "System Access Logs";
                DataExportHelper.ExportToCsv(gridAudit, cbAuditCategory.SelectedIndex == 0 ? "AuditTrail" : "AccessLogs", title);
            });
            btnExport.MinimumSize = new Size(0, D(32));
            btnExport.Margin = new Padding(0, 0, D(12), D(4));
            lblCount.AutoSize = true;
            lblCount.Margin = new Padding(0, D(8), 0, D(4));
            lblCount.Font = new Font("Segoe UI", 9.5F);
            lblCount.ForeColor = AppTheme.TextMuted;
            lblCount.Text = "";
            toolbar.Controls.AddRange(new Control[] { chipCategory, txtSearch, chipDataAction, chipAuthEvent, btnRefresh, btnExport, lblCount });
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(D(16), D(4), D(16), D(16)),
                BackColor = Color.Transparent
            };

            gridAudit.Dock = DockStyle.Fill;
            pnlGridCard.Controls.Add(gridAudit);

            this.Controls.Add(pnlGridCard);
            this.Controls.Add(toolbar);
            this.Controls.Add(header);
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
            gridAudit.BackgroundColor = Pal.White;
            gridAudit.BorderStyle = BorderStyle.None;
            gridAudit.RowHeadersVisible = false;
            gridAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAudit.RowTemplate.Height = 32;

            gridAudit.AutoGenerateColumns = false;
            gridAudit.ColumnHeadersVisible = true;
            gridAudit.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridAudit.ColumnHeadersHeight = D(36);
            if (cbAuditCategory.SelectedIndex == 0)
            {
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Timestamp", HeaderText = "Date & time", FillWeight = 14, MinimumWidth = D(130), DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" } });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "User", FillWeight = 9, MinimumWidth = D(70) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ActionType", HeaderText = "Action", FillWeight = 10, MinimumWidth = D(80) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TableName", HeaderText = "Module", FillWeight = 11, MinimumWidth = D(80) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordIdentifier", HeaderText = "Record", FillWeight = 13, MinimumWidth = D(90) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FieldName", HeaderText = "Field", FillWeight = 9, MinimumWidth = D(70) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OldValue", HeaderText = "Old value", FillWeight = 10, MinimumWidth = D(80) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NewValue", HeaderText = "New value", FillWeight = 10, MinimumWidth = D(80) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReasonOrNotes", HeaderText = "Notes", FillWeight = 24, MinimumWidth = D(140) });
            }
            else
            {
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Timestamp", HeaderText = "Date & time", FillWeight = 16, MinimumWidth = D(130), DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" } });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EventType", HeaderText = "Event", FillWeight = 16, MinimumWidth = D(110) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", FillWeight = 14, MinimumWidth = D(90) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IpOrMachine", HeaderText = "Computer", FillWeight = 14, MinimumWidth = D(90) });
                gridAudit.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Details", HeaderText = "Details", FillWeight = 40, MinimumWidth = D(160) });
            }
            foreach (DataGridViewColumn c in gridAudit.Columns) c.ToolTipText = c.HeaderText;
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
            { ErrLog.Error("SecurityAuditView", ex); 
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
                lblCount.Text = filtered.Count == rawDataAuditLogs.Count ? $"{filtered.Count} entries" : $"{filtered.Count} of {rawDataAuditLogs.Count} entries";
                header.Title.Text = $"Audit logs · data changes ({rawDataAuditLogs.Count})";
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
                lblCount.Text = filtered.Count == rawLoginLogs.Count ? $"{filtered.Count} entries" : $"{filtered.Count} of {rawLoginLogs.Count} entries";
                header.Title.Text = $"Audit logs · sign-ins ({rawLoginLogs.Count})";
            }
        }
    }
}
