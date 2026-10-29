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
        private TileStrip tiles = null!;
        private StatTile tAll = null!, tAdd = null!, tUpd = null!, tDel = null!, tOther = null!;
        private int D(int v) => PageKit.D(this, v);
        private static readonly Dictionary<string, string> FriendlyTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Trucks"] = "trucks", ["Crews"] = "crew", ["Invoices"] = "invoices", ["CollectionReceipts"] = "receipts",
            ["StatementsOfAccount"] = "statements of account", ["LookupCategories"] = "categories", ["Clients"] = "clients",
            ["DeliveryRoutes"] = "delivery routes", ["LoadingSchedules"] = "loading orders", ["Deliveries"] = "dispatches"
        };
        private string Scope() => targetTables == null || targetTables.Length == 0 ? "every module"
            : string.Join(", ", targetTables.Select(t => FriendlyTable.TryGetValue(t, out var f) ? f : t));
        private List<AuditTrailEntry> rawLogs = new List<AuditTrailEntry>();

        public ModuleHistoryView(string moduleTitle, string[]? targetTables = null, bool isAdminOnly = false)
        {
            this.moduleTitle = moduleTitle;
            this.targetTables = targetTables;
            this.isAdminOnly = isAdminOnly;

            bool isAdmin = SessionContext.IsAdmin;
            if (isAdminOnly && !isAdmin)
            {
                RenderRestrictedView();
            }
            else
            {
                InitializeComponent();
                SetupGridColumns();
                RowMenu.AttachBasic(gridHistory, $"{moduleTitle.Replace(" ", "_")}_History", $"{moduleTitle} Audit History");
                LoadLogs();
            }

            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            bool isAdmin = SessionContext.IsAdmin;
            if (!isAdminOnly || isAdmin)
            {
                LoadLogs();
            }
        }

        private void RenderRestrictedView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.Canvas;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, BackColor = Color.Transparent };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            var card = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = AppTheme.Surface,
                Padding = new Padding(D(36), D(32), D(36), D(30)),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                Gfx.HighQuality(e.Graphics);
                using var path = Gfx.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), D(14));
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawPath(pen, path);
            };

            var icon = new Label { Text = "🔒", Font = new Font("Segoe UI Emoji", 26F), AutoSize = true, Margin = new Padding(0, 0, 0, D(10)), Anchor = AnchorStyles.None };
            var title = new Label { Text = "Only administrators can see this history", Font = new Font("Segoe UI Semibold", 15F), ForeColor = AppTheme.TextPrimary, AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, D(8)), UseMnemonic = false };
            var body = new Label
            {
                Text = $"{moduleTitle} lists every add, change and delete made to {Scope()}, with who did it and when.\nAsk an administrator if you need to check a change.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(D(520), 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                UseMnemonic = false
            };

            var who = new Label
            {
                Text = $"Signed in as {SessionContext.ActiveUser?.Username} · {SessionContext.ActiveUser?.Role}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, D(16), 0, 0),
                UseMnemonic = false
            };

            card.Controls.Add(icon);
            card.Controls.Add(title);
            card.Controls.Add(body);
            card.Controls.Add(who);
            layout.Controls.Add(card, 1, 1);
            this.Controls.Add(layout);
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.Canvas;
            this.Font = new Font("Segoe UI", 9.5F);

            this.Padding = new Padding(D(4), D(8), D(4), D(4));
            var header = PageKit.Header(moduleTitle, $"Every add, change and delete made to {Scope()} — who did it, when, and what changed. Double-click a row to open the record.");
            btnExport = PageKit.Action("⬇  Export CSV", "secondary", (s, e) => DataExportHelper.ExportToCsv(gridHistory, $"{moduleTitle.Replace(" ", "_")}_History", $"{moduleTitle} Audit History"));
            btnRefresh = PageKit.Action("↻  Refresh", "secondary", (s, e) => LoadLogs());
            header.AddActions(btnExport, btnRefresh);
            tiles = new TileStrip();
            tAll = tiles.Add(new StatTile("All changes", AppTheme.Accent, "in the period") { Clickable = true, Key = "All Actions" });
            tAdd = tiles.Add(new StatTile("Added", AppTheme.Success, "new records") { Clickable = true, Key = "ADD" });
            tUpd = tiles.Add(new StatTile("Updated", AppTheme.Info, "edits and status changes") { Clickable = true, Key = "UPDATE" });
            tDel = tiles.Add(new StatTile("Deleted", AppTheme.Danger, "removed records") { Clickable = true, Key = "DELETE" });
            tOther = tiles.Add(new StatTile("Last change", AppTheme.TextMuted, ""));
            tiles.FilterChanged += t =>
            {
                string key = t?.Key as string ?? "All Actions";
                if (cbActionFilter.Items.Contains(key)) cbActionFilter.SelectedItem = key; else cbActionFilter.SelectedIndex = 0;
            };
            cbActionFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cbActionFilter.Items.AddRange(new object[] { "All Actions", "ADD", "UPDATE", "DELETE", "CANCEL", "RESCHEDULE" });
            cbActionFilter.SelectedIndex = 0;
            cbActionFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            var filterCard = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = AppTheme.Surface,
                Padding = new Padding(D(12), D(10), D(12), D(10)),
                Height = D(58)
            };
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(170)));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(170)));
            filterCard.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, filterCard.Width - 1, filterCard.Height - 1); };
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0, D(1), D(8), 0);
            txtSearch.MaxLength = 100;
            PageKit.StyleSearch(txtSearch, "Search by user, reference #, field or details…");
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbActionFilter.Dock = DockStyle.Fill;
            cbActionFilter.Margin = new Padding(0, 0, D(8), 0);
            cbDateFilter.Dock = DockStyle.Fill;
            cbDateFilter.Margin = new Padding(0);
            cbDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDateFilter.Items.AddRange(new object[] { "All Time", "Today", "Last 7 Days", "Last 30 Days" });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            PageKit.StyleCombo(cbActionFilter);
            PageKit.StyleCombo(cbDateFilter);
            filterCard.Controls.Add(txtSearch, 0, 0);
            filterCard.Controls.Add(cbActionFilter, 1, 0);
            filterCard.Controls.Add(cbDateFilter, 2, 0);
            var pnlGridCard = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(12), D(8), D(12), D(4)) };
            pnlGridCard.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1); };
            gridHistory.Dock = DockStyle.Fill;
            lblCount.Dock = DockStyle.Bottom;
            lblCount.AutoSize = false;
            lblCount.Height = D(30);
            lblCount.TextAlign = ContentAlignment.MiddleLeft;
            lblCount.Font = new Font("Segoe UI", 9F);
            lblCount.ForeColor = AppTheme.TextMuted;
            pnlGridCard.Controls.Add(gridHistory);

            pnlGridCard.Controls.Add(lblCount);
            this.Controls.Add(pnlGridCard);
            this.Controls.Add(PageKit.Gap(this, 12));
            this.Controls.Add(filterCard);
            this.Controls.Add(PageKit.Gap(this, 12));
            this.Controls.Add(tiles);
            this.Controls.Add(PageKit.Gap(this, 14));
            this.Controls.Add(header);
            this.HandleCreated += (s, e) => { tiles.Height = D(96); filterCard.Height = D(58); };
        }

        private void SetupGridColumns()
        {
            gridHistory.Columns.Clear();
            gridHistory.ReadOnly = true;
            gridHistory.AllowUserToAddRows = false;
            gridHistory.AllowUserToDeleteRows = false;
            gridHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHistory.MultiSelect = false;
            gridHistory.BackgroundColor = Pal.White;
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
            gridHistory.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridHistory.Rows[e.RowIndex].DataBoundItem is AuditTrailEntry log)
                {
                    object? targetEntity = null;
                    try
                    {
                        if (log.TableName.Equals("LoadingSchedules", StringComparison.OrdinalIgnoreCase))
                        {
                            targetEntity = MJdatabase.GetLoadingSchedules().FirstOrDefault(x => x.Id == log.RecordId || (!string.IsNullOrEmpty(x.ScheduleNumber) && x.ScheduleNumber == log.RecordIdentifier));
                        }
                        else if (log.TableName.Equals("Deliveries", StringComparison.OrdinalIgnoreCase))
                        {
                            targetEntity = MJdatabase.GetDeliveries().FirstOrDefault(x => x.Id == log.RecordId || (!string.IsNullOrEmpty(x.DispatchNumber) && x.DispatchNumber == log.RecordIdentifier));
                        }
                        else if (log.TableName.Equals("IncidentReports", StringComparison.OrdinalIgnoreCase))
                        {
                            targetEntity = MJdatabase.GetIncidentReports().FirstOrDefault(x => x.Id == log.RecordId || (!string.IsNullOrEmpty(x.DispatchNumber) && x.DispatchNumber == log.RecordIdentifier));
                        }
                        else if (log.TableName.Equals("TripReports", StringComparison.OrdinalIgnoreCase))
                        {
                            targetEntity = MJdatabase.GetTripReports().FirstOrDefault(x => x.Id == log.RecordId || (!string.IsNullOrEmpty(x.TripReportNumber) && x.TripReportNumber == log.RecordIdentifier) || (!string.IsNullOrEmpty(x.DispatchNumber) && x.DispatchNumber == log.RecordIdentifier));
                        }
                    }
                    catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                    RecordDetailsDialog.ShowDetails(this, targetEntity ?? log);
                }
            };
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
                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Pal.C(185, 28, 28);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("CANCEL", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Pal.C(220, 38, 38);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("RESCHEDULE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Pal.C(124, 58, 237);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                else if (act.Equals("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Pal.C(30, 64, 175);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Bold);
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
            catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private static string Ago(DateTime t)
        {
            var d = DateTime.Now - t;
            if (d.TotalMinutes < 1) return "just now";
            if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} min ago";
            if (d.TotalDays < 1) return $"{(int)d.TotalHours} h ago";
            if (d.TotalDays < 30) return $"{(int)d.TotalDays} d ago";
            return t.ToString("MMM d, yyyy");
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
            lblCount.Text = $"Showing {filtered.Count:N0} of {rawLogs.Count:N0} changes  ·  right-click a row to copy or export";
            if (tiles != null)
            {
                var inPeriod = rawLogs.Where(l => !cutoff.HasValue || l.Timestamp >= cutoff.Value).ToList();
                tAll.Value = inPeriod.Count.ToString("N0");
                tAll.Note = selectedDate == "All Time" ? "latest 1,000 kept here" : selectedDate.ToLowerInvariant();
                tAdd.Value = inPeriod.Count(l => l.ActionType.Equals("ADD", StringComparison.OrdinalIgnoreCase) || l.ActionType.Contains("CREATE") || l.ActionType.Equals("INSERT", StringComparison.OrdinalIgnoreCase)).ToString("N0");
                tUpd.Value = inPeriod.Count(l => l.ActionType.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)).ToString("N0");
                tDel.Value = inPeriod.Count(l => l.ActionType.Equals("DELETE", StringComparison.OrdinalIgnoreCase)).ToString("N0");
                var last = rawLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault();
                tOther.Value = last == null ? "—" : Ago(last.Timestamp);
                tOther.Note = last == null ? "no changes yet" : $"{last.Username} · {last.ActionType.ToLowerInvariant()} · {last.RecordIdentifier}";
            }
        }
    }
}
