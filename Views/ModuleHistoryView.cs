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
    public class ModuleHistoryView : UserControl, IRefreshable
    {
        private readonly string moduleTitle;
        private readonly string[]? targetTables;
        private readonly bool isAdminOnly;

        private DataGridView gridHistory = new DataGridView();
        private TextBox txtSearch = new TextBox();
        private ComboBox cbActionFilter = new ComboBox();
        private ComboBox cbDateFilter = new ComboBox();
        private Button btnRefresh = new Button();
        private Button btnExport = new Button();
        private Label lblCount = new Label();
        private List<AuditTrailEntry> rawLogs = new List<AuditTrailEntry>();

        public ModuleHistoryView(string moduleTitle, string[]? targetTables = null, bool isAdminOnly = false)
        {
            this.moduleTitle = moduleTitle;
            this.targetTables = targetTables;
            this.isAdminOnly = isAdminOnly;

            bool isAdmin = SessionContext.ActiveUser != null && 
                (SessionContext.ActiveUser.Role == "Admin" || SessionContext.ActiveUser.Role == "Owner/Admin");

            if (isAdminOnly && !isAdmin)
            {
                RenderRestrictedView();
            }
            else
            {
                InitializeComponent();
                SetupGridColumns();
                DataExportHelper.AttachExportContextMenu(gridHistory, $"{moduleTitle.Replace(" ", "_")}_History", $"{moduleTitle} Audit History");
                LoadLogs();
            }

            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            bool isAdmin = SessionContext.ActiveUser != null && 
                (SessionContext.ActiveUser.Role == "Admin" || SessionContext.ActiveUser.Role == "Owner/Admin");

            if (!isAdminOnly || isAdmin)
            {
                LoadLogs();
            }
        }

        private void RenderRestrictedView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            Panel card = new Panel
            {
                Size = new Size(540, 260),
                BackColor = Color.White,
                Padding = new Padding(32),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                using var b = new SolidBrush(Color.FromArgb(190, 18, 60));
                e.Graphics.FillRectangle(b, 0, 0, 6, card.Height);
            };

            Label lblIcon = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 32F),
                AutoSize = true,
                Location = new Point(32, 28)
            };

            Label lblTitle = new Label
            {
                Text = "Administrator Access Required",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(32, 88),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = $"The change tracking and audit history for {moduleTitle} is restricted to system administrators only.\n\nOnly administrative accounts have permission to review complete update, add, and delete records for this module.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(32, 126),
                Size = new Size(470, 80)
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblDesc });
            layout.Controls.Add(card, 1, 1);
            this.Controls.Add(layout);
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 10)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            Label lblModuleTitle = new Label
            {
                Text = $"📜 {moduleTitle}",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(20, 12)
            };

            Label lblModuleSubtitle = new Label
            {
                Text = targetTables != null && targetTables.Length > 0
                    ? $"Tracked Modules: {string.Join(", ", targetTables)} • Complete Add, Update, and Delete audit log"
                    : "Master System Audit Log • Complete Add, Update, and Delete tracking across all modules",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(20, 38)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblModuleTitle, lblModuleSubtitle });

            FlowLayoutPanel pnlFilterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = Color.FromArgb(248, 250, 252)
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
            txtSearch.PlaceholderText = "Search user, ref #, field...";
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            Label lblAction = new Label
            {
                Text = "Action:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbActionFilter.Size = new Size(140, 26);
            cbActionFilter.Margin = new Padding(0, 4, 16, 0);
            cbActionFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cbActionFilter.Items.AddRange(new object[]
            {
                "All Actions",
                "ADD",
                "UPDATE",
                "DELETE",
                "CANCEL",
                "RESCHEDULE"
            });
            cbActionFilter.SelectedIndex = 0;
            cbActionFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            Label lblDate = new Label
            {
                Text = "Date Range:",
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            cbDateFilter.Size = new Size(130, 26);
            cbDateFilter.Margin = new Padding(0, 4, 16, 0);
            cbDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDateFilter.Items.AddRange(new object[]
            {
                "All Time",
                "Today",
                "Last 7 Days",
                "Last 30 Days"
            });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.Size = new Size(95, 28);
            btnRefresh.Margin = new Padding(0, 3, 8, 0);
            btnRefresh.BackColor = Color.FromArgb(241, 245, 249);
            btnRefresh.ForeColor = Color.FromArgb(51, 65, 85);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => LoadLogs();

            btnExport.Text = "📊 Export CSV";
            btnExport.Size = new Size(110, 28);
            btnExport.Margin = new Padding(0, 3, 16, 0);
            btnExport.BackColor = Color.FromArgb(15, 23, 42);
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += (s, e) =>
            {
                DataExportHelper.ExportToCsv(gridHistory, $"{moduleTitle.Replace(" ", "_")}_History", $"{moduleTitle} Audit History");
            };

            lblCount.AutoSize = true;
            lblCount.Margin = new Padding(0, 7, 0, 0);
            lblCount.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblCount.ForeColor = Color.FromArgb(71, 85, 105);
            lblCount.Text = "Records: 0";

            pnlFilterBar.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch,
                lblAction, cbActionFilter,
                lblDate, cbDateFilter,
                btnRefresh, btnExport,
                lblCount
            });

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 16, 16),
                BackColor = Color.Transparent
            };

            gridHistory.Dock = DockStyle.Fill;
            pnlGridCard.Controls.Add(gridHistory);

            this.Controls.Add(pnlGridCard);
            this.Controls.Add(pnlFilterBar);
            this.Controls.Add(pnlHeader);
        }

        private void SetupGridColumns()
        {
            gridHistory.Columns.Clear();
            gridHistory.ReadOnly = true;
            gridHistory.AllowUserToAddRows = false;
            gridHistory.AllowUserToDeleteRows = false;
            gridHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHistory.MultiSelect = false;
            gridHistory.BackgroundColor = Color.White;
            gridHistory.BorderStyle = BorderStyle.None;
            gridHistory.RowHeadersVisible = false;
            gridHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridHistory.RowTemplate.Height = 32;

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Timestamp",
                HeaderText = "Timestamp",
                FillWeight = 14,
                MinimumWidth = 125,
                DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Username",
                HeaderText = "User",
                FillWeight = 11,
                MinimumWidth = 90
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "UserRole",
                HeaderText = "Role",
                FillWeight = 10,
                MinimumWidth = 85
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ActionType",
                HeaderText = "Action",
                FillWeight = 11,
                MinimumWidth = 90
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TableName",
                HeaderText = "Module / Table",
                FillWeight = 13,
                MinimumWidth = 100
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "RecordIdentifier",
                HeaderText = "Reference #",
                FillWeight = 14,
                MinimumWidth = 110
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FieldName",
                HeaderText = "Field Changed",
                FillWeight = 12,
                MinimumWidth = 95
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "NewValue",
                HeaderText = "New Value",
                FillWeight = 12,
                MinimumWidth = 95
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ReasonOrNotes",
                HeaderText = "Activity Details",
                FillWeight = 26,
                MinimumWidth = 160
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IpOrMachine",
                HeaderText = "Workstation",
                FillWeight = 10,
                MinimumWidth = 85
            });

            gridHistory.CellFormatting += GridHistory_CellFormatting;
        }

        private void GridHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= gridHistory.Rows.Count) return;
            var col = gridHistory.Columns[e.ColumnIndex];
            if (col.DataPropertyName == "ActionType" && e.Value != null)
            {
                string act = e.Value.ToString() ?? "";
                if (act.Equals("ADD", StringComparison.OrdinalIgnoreCase) || act.Contains("CREATE"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("CANCEL", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("RESCHEDULE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(124, 58, 237);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(30, 64, 175);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
            }
        }

        private async void LoadLogs()
        {
            try
            {
                btnRefresh.Enabled = false;
                rawLogs = await MJdatabase.GetAuditTrailAsync(targetTables, null, 1000);
                ApplyFilter();
            }
            catch { }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private void ApplyFilter()
        {
            string q = (txtSearch.Text ?? "").Trim().ToLowerInvariant();
            string selectedAction = cbActionFilter.SelectedItem?.ToString() ?? "All Actions";
            string selectedDate = cbDateFilter.SelectedItem?.ToString() ?? "All Time";

            DateTime now = DateTime.Now;
            DateTime? cutoff = selectedDate switch
            {
                "Today" => DateTime.Today,
                "Last 7 Days" => now.AddDays(-7),
                "Last 30 Days" => now.AddDays(-30),
                _ => null
            };

            var filtered = rawLogs.Where(log =>
            {
                if (cutoff.HasValue && log.Timestamp < cutoff.Value) return false;

                if (!selectedAction.Equals("All Actions", StringComparison.OrdinalIgnoreCase))
                {
                    if (selectedAction == "ADD" && !(log.ActionType.Equals("ADD", StringComparison.OrdinalIgnoreCase) || log.ActionType.Contains("CREATE") || log.ActionType.Equals("INSERT", StringComparison.OrdinalIgnoreCase)))
                        return false;
                    else if (selectedAction == "UPDATE" && !(log.ActionType.Equals("UPDATE", StringComparison.OrdinalIgnoreCase) || log.ActionType.Contains("UPDATE")))
                        return false;
                    else if (selectedAction == "DELETE" && !log.ActionType.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                        return false;
                    else if (selectedAction == "CANCEL" && !log.ActionType.Equals("CANCEL", StringComparison.OrdinalIgnoreCase))
                        return false;
                    else if (selectedAction == "RESCHEDULE" && !log.ActionType.Equals("RESCHEDULE", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (!string.IsNullOrEmpty(q))
                {
                    bool match = (log.Username?.ToLowerInvariant().Contains(q) ?? false) ||
                                 (log.RecordIdentifier?.ToLowerInvariant().Contains(q) ?? false) ||
                                 (log.FieldName?.ToLowerInvariant().Contains(q) ?? false) ||
                                 (log.NewValue?.ToLowerInvariant().Contains(q) ?? false) ||
                                 (log.ReasonOrNotes?.ToLowerInvariant().Contains(q) ?? false) ||
                                 (log.TableName?.ToLowerInvariant().Contains(q) ?? false);
                    if (!match) return false;
                }

                return true;
            }).ToList();

            gridHistory.DataSource = null;
            gridHistory.DataSource = filtered;
            lblCount.Text = $"Records: {filtered.Count} / {rawLogs.Count}";
        }
    }
}
