using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>One line of the Audit Trail: a record change (table AuditTrail) or a sign-in event (table LoginAudit).</summary>
    public sealed class AuditTrailRow
    {
        public DateTime When { get; set; }
        public string WhenText => Fmt.DateTimeText(When);
        public string User { get; set; } = "";
        public string Area { get; set; } = "";
        public string Action { get; set; } = "";
        public string Record { get; set; } = "";
        public string Details { get; set; } = "";
        public bool IsSecurity { get; set; }
    }

    /// <summary>
    /// Settings > Audit Trail (administrators only): every record change and every sign-in event in one list, newest first.
    /// Replaces the two separate tabs "Change History" (<see cref="ModuleHistoryView"/>) and "Security Audit Logs" (<see cref="SecurityAuditView"/>), which stay in the code base for reuse.
    /// </summary>
    public class AuditTrailView : UserControl, IRefreshable
    {
        public const string SecurityArea = "Security";
        public const string AllAreas = "All areas", AllUsers = "All users", AllActions = "All actions";
        private const int RowLimit = 5000;

        private static readonly Dictionary<string, string> ModuleNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Trucks"] = "Trucks", ["Crews"] = "Crew", ["CrewRecords"] = "Crew records", ["TruckMaintenance"] = "Truck maintenance", ["LookupCategories"] = "Categories",
            ["Clients"] = "Clients", ["ClientStores"] = "Client stores", ["DeliveryRoutes"] = "Delivery routes", ["RateMatrix"] = "Rates",
            ["LoadingSchedules"] = "Loading orders", ["Deliveries"] = "Dispatches", ["SpecialTrips"] = "Special trips", ["TripReports"] = "Trip reports", ["IncidentReports"] = "Incident reports",
            ["Invoices"] = "Invoices", ["StatementsOfAccount"] = "Statements of account", ["CollectionReceipts"] = "Collection receipts", ["Users"] = "Users"
        };
        private static readonly Dictionary<string, string> ActionNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADD"] = "Added", ["UPDATE"] = "Updated", ["DELETE"] = "Deleted", ["CANCEL"] = "Cancelled", ["COMPLETED"] = "Completed",
            ["INVOICE_CREATED"] = "Invoice created", ["DISPATCH_CREATED"] = "Dispatch created", ["PAYMENT_COLLECTED"] = "Payment collected",
            ["SECURITY_UPDATE"] = "Security update", ["PASSWORD_CHANGE"] = "Password change",
            ["LOGIN_SUCCESS"] = "Signed in", ["LOGIN_FAILED"] = "Sign-in failed", ["LOCKED_OUT"] = "Locked out", ["ACCOUNT_REGISTERED"] = "Account registered",
            ["USER_APPROVED"] = "User approved", ["USER_REJECTED"] = "User rejected", ["PASSWORD_RESET_SUCCESS"] = "Password reset", ["PASSWORD_RESET_FAILED"] = "Password reset failed"
        };

        /// <summary>The module name shown in the Area column for a table of the AuditTrail log.</summary>
        public static string AreaOf(string tableName)
            => ModuleNames.TryGetValue(tableName ?? "", out var n) ? n : (string.IsNullOrWhiteSpace(tableName) ? "Other" : tableName.Trim());
        /// <summary>The action as shown (Added, Signed in, ...); an unknown code is shown in words.</summary>
        public static string ActionText(string code)
        {
            if (ActionNames.TryGetValue(code ?? "", out var n)) return n;
            string t = (code ?? "").Replace('_', ' ').Trim().ToLowerInvariant();
            return t.Length == 0 ? "" : char.ToUpperInvariant(t[0]) + t.Substring(1);
        }

        /// <summary>Both logs as one list of rows, newest first.</summary>
        public static List<AuditTrailRow> Merge(IEnumerable<AuditTrailEntry> changes, IEnumerable<LoginAudit> signIns)
        {
            var rows = new List<AuditTrailRow>();
            foreach (var c in changes)
            {
                string detail = string.IsNullOrWhiteSpace(c.FieldName) ? "" : $"{c.FieldName}: {(string.IsNullOrEmpty(c.OldValue) ? "(blank)" : c.OldValue)} → {(string.IsNullOrEmpty(c.NewValue) ? "(blank)" : c.NewValue)}";
                if (!string.IsNullOrWhiteSpace(c.ReasonOrNotes)) detail = detail.Length == 0 ? c.ReasonOrNotes : detail + " · " + c.ReasonOrNotes;
                rows.Add(new AuditTrailRow { When = c.Timestamp, User = c.Username ?? "", Area = AreaOf(c.TableName), Action = ActionText(c.ActionType), Record = c.RecordIdentifier ?? "", Details = detail });
            }
            foreach (var l in signIns)
            {
                string detail = l.Details ?? "";
                if (!string.IsNullOrWhiteSpace(l.IpOrMachine)) detail = detail.Length == 0 ? "Computer: " + l.IpOrMachine : detail + " · Computer: " + l.IpOrMachine;
                rows.Add(new AuditTrailRow { When = l.Timestamp, User = l.Username ?? "", Area = SecurityArea, Action = ActionText(l.EventType), Record = "", Details = detail, IsSecurity = true });
            }
            return rows.OrderByDescending(r => r.When).ToList();
        }

        /// <summary>The rows that pass every filter. A blank / "All ..." filter lets everything through; the dates are whole days, both included.</summary>
        public static List<AuditTrailRow> Filter(IEnumerable<AuditTrailRow> rows, DateTime? from, DateTime? to, string? user, string? area, string? action, string? text)
        {
            string q = (text ?? "").Trim();
            bool anyUser = string.IsNullOrWhiteSpace(user) || user == AllUsers, anyArea = string.IsNullOrWhiteSpace(area) || area == AllAreas, anyAction = string.IsNullOrWhiteSpace(action) || action == AllActions;
            return rows.Where(r =>
                (!from.HasValue || r.When >= from.Value.Date) &&
                (!to.HasValue || r.When < to.Value.Date.AddDays(1)) &&
                (anyUser || r.User.Equals(user, StringComparison.OrdinalIgnoreCase)) &&
                (anyArea || r.Area.Equals(area, StringComparison.OrdinalIgnoreCase)) &&
                (anyAction || r.Action.Equals(action, StringComparison.OrdinalIgnoreCase)) &&
                (q.Length == 0 || new[] { r.WhenText, r.User, r.Area, r.Action, r.Record, r.Details }.Any(v => v.Contains(q, StringComparison.OrdinalIgnoreCase)))).ToList();
        }

        private readonly DataGridView grid = new DataGridView();
        private readonly DateTimePicker dtFrom = new DateTimePicker { ShowCheckBox = true, Checked = false, Width = 130 };
        private readonly DateTimePicker dtTo = new DateTimePicker { ShowCheckBox = true, Checked = false, Width = 130 };
        private readonly ComboBox cbUser = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly ComboBox cbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
        private readonly ComboBox cbAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        private readonly TextBox txtSearch = new TextBox();
        private readonly Label lblCount = new Label();
        private List<AuditTrailRow> all = new List<AuditTrailRow>();
        private List<AuditTrailRow> shown = new List<AuditTrailRow>();
        private string sortProperty = nameof(AuditTrailRow.When);
        private bool sortAscending;
        private bool loading;
        private int D(int v) => PageKit.D(this, v);

        /// <summary>The rows the list shows now (after the filters and the sort).</summary>
        public IReadOnlyList<AuditTrailRow> ShownRows => shown;
        public DataGridView Grid => grid;

        public AuditTrailView()
        {
            if (!SessionContext.IsAdmin) { RenderRestricted(); UIThemeHelper.ApplyTheme(this); return; }
            Build();
            RowMenu.AttachBasic(grid, "Audit_Trail", "Audit Trail");
            LoadNow();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            if (SessionContext.IsAdmin && grid.Parent != null) LoadNow();
        }

        private void RenderRestricted()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppTheme.TextSecondary,
                UseMnemonic = false,
                Text = "Only administrators can see the audit trail.\nAsk an administrator if you need to check a change."
            });
        }

        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.5F);

            var header = PageKit.Header("Audit trail", "Every change to your records and every sign-in, newest first. Entries can never be changed or deleted.");
            header.Dock = DockStyle.Top;
            header.Padding = new Padding(D(12), header.Padding.Top, D(16), header.Padding.Bottom);

            Fmt.ApplyDatePicker(dtFrom); Fmt.ApplyDatePicker(dtTo);
            foreach (var cb in new[] { cbUser, cbArea, cbAction }) PageKit.StyleCombo(cb);
            PageKit.StyleSearch(txtSearch, "🔍 Search user, record or details…");
            txtSearch.Width = D(240);
            txtSearch.MaxLength = 100;
            dtFrom.ValueChanged += (s, e) => ApplyFilters();
            dtTo.ValueChanged += (s, e) => ApplyFilters();
            cbUser.SelectedIndexChanged += (s, e) => ApplyFilters();
            cbArea.SelectedIndexChanged += (s, e) => ApplyFilters();
            cbAction.SelectedIndexChanged += (s, e) => ApplyFilters();
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            var btnRefresh = PageKit.Action("🔄 Refresh", "secondary", (s, e) => LoadNow());
            var btnExport = PageKit.Action("📊 Export", "secondary", (s, e) => DataExportHelper.ExportToCsv(grid, "AuditTrail", "Audit trail"));
            var btnClear = PageKit.Action("Clear filters", "secondary", (s, e) => ClearFilters());
            lblCount.AutoSize = true;
            lblCount.ForeColor = AppTheme.TextMuted;
            lblCount.Margin = new Padding(D(8), D(8), 0, D(4));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(D(16), D(4), D(16), D(8)),
                BackColor = Color.Transparent
            };
            Label Cap(string t) => new Label { Text = t, AutoSize = true, ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(8), D(4), D(4)), UseMnemonic = false };
            foreach (var c in new Control[] { dtFrom, dtTo, cbUser, cbArea, cbAction, txtSearch }) c.Margin = new Padding(0, D(4), D(10), D(4));
            toolbar.Controls.AddRange(new Control[] { Cap("From"), dtFrom, Cap("To"), dtTo, Cap("User"), cbUser, Cap("Area"), cbArea, Cap("Action"), cbAction, txtSearch, btnClear, btnRefresh, btnExport, lblCount });

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = Pal.White;
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowTemplate.Height = 32;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = D(36);
            void Col(string prop, string head, int weight, int min)
                => grid.Columns.Add(new DataGridViewTextBoxColumn { Name = prop, DataPropertyName = prop, HeaderText = head, FillWeight = weight, MinimumWidth = D(min), SortMode = DataGridViewColumnSortMode.Programmatic, ToolTipText = "Click to sort by " + head.ToLowerInvariant() });
            Col(nameof(AuditTrailRow.WhenText), "Date/time", 15, 140);
            Col(nameof(AuditTrailRow.User), "User", 10, 80);
            Col(nameof(AuditTrailRow.Area), "Area", 12, 100);
            Col(nameof(AuditTrailRow.Action), "Action", 12, 100);
            Col(nameof(AuditTrailRow.Record), "Record", 14, 100);
            Col(nameof(AuditTrailRow.Details), "Details", 37, 180);
            grid.ColumnHeaderMouseClick += (s, e) => SortBy(grid.Columns[e.ColumnIndex].DataPropertyName);

            var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(16), D(4), D(16), D(16)), BackColor = Color.Transparent };
            card.Controls.Add(grid);
            Controls.Add(card);
            Controls.Add(toolbar);
            Controls.Add(header);
        }

        /// <summary>Reads both logs again and keeps the filters and the sort.</summary>
        public void LoadNow()
        {
            if (!SessionContext.IsAdmin) return;
            try
            {
                all = Merge(MJdatabase.GetAuditTrail(RowLimit), MJdatabase.GetSecurityAuditLogs(RowLimit));
            }
            catch (Exception ex)
            {
                ErrLog.Error("AuditTrailView", ex);
                all = new List<AuditTrailRow>();
            }
            loading = true;
            try
            {
                Refill(cbUser, AllUsers, all.Select(r => r.User).Where(u => u.Length > 0));
                var areas = ModuleNames.Values.Concat(all.Where(r => !r.IsSecurity).Select(r => r.Area)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a, StringComparer.OrdinalIgnoreCase).Append(SecurityArea);
                Refill(cbArea, AllAreas, areas, keepOrder: true);
                Refill(cbAction, AllActions, all.Select(r => r.Action).Where(a => a.Length > 0));
            }
            finally { loading = false; }
            ApplyFilters();
        }

        private static void Refill(ComboBox cb, string allText, IEnumerable<string> values, bool keepOrder = false)
        {
            string keep = cb.SelectedItem?.ToString() ?? allText;
            var list = keepOrder ? values.Distinct(StringComparer.OrdinalIgnoreCase).ToList() : values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
            cb.Items.Clear();
            cb.Items.Add(allText);
            cb.Items.AddRange(list.Cast<object>().ToArray());
            int at = cb.Items.IndexOf(keep);
            cb.SelectedIndex = at >= 0 ? at : 0;
        }

        /// <summary>Sets the filters (blank / null = no filter) and refreshes the list.</summary>
        public void SetFilters(DateTime? from, DateTime? to, string? user = null, string? area = null, string? action = null, string? text = null)
        {
            loading = true;
            try
            {
                dtFrom.Checked = from.HasValue; if (from.HasValue) dtFrom.Value = from.Value;
                dtTo.Checked = to.HasValue; if (to.HasValue) dtTo.Value = to.Value;
                Select(cbUser, user, AllUsers); Select(cbArea, area, AllAreas); Select(cbAction, action, AllActions);
                txtSearch.Text = text ?? "";
            }
            finally { loading = false; }
            ApplyFilters();
        }
        private static void Select(ComboBox cb, string? v, string allText)
        {
            int at = string.IsNullOrWhiteSpace(v) ? 0 : cb.Items.IndexOf(v);
            cb.SelectedIndex = at >= 0 ? at : 0;
        }
        private void ClearFilters() => SetFilters(null, null);

        /// <summary>Sorts by a column (the property it shows); the same column again flips the direction. The first click on a column sorts newest / A to Z.</summary>
        public void SortBy(string property)
        {
            if (property == nameof(AuditTrailRow.WhenText)) property = nameof(AuditTrailRow.When);
            if (sortProperty == property) sortAscending = !sortAscending;
            else { sortProperty = property; sortAscending = property != nameof(AuditTrailRow.When); }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (loading) return;
            shown = Filter(all, dtFrom.Checked ? dtFrom.Value : null, dtTo.Checked ? dtTo.Value : null,
                cbUser.SelectedItem?.ToString(), cbArea.SelectedItem?.ToString(), cbAction.SelectedItem?.ToString(), txtSearch.Text);
            Func<AuditTrailRow, object> key = sortProperty switch
            {
                nameof(AuditTrailRow.User) => r => r.User.ToLowerInvariant(),
                nameof(AuditTrailRow.Area) => r => r.Area.ToLowerInvariant(),
                nameof(AuditTrailRow.Action) => r => r.Action.ToLowerInvariant(),
                nameof(AuditTrailRow.Record) => r => r.Record.ToLowerInvariant(),
                nameof(AuditTrailRow.Details) => r => r.Details.ToLowerInvariant(),
                _ => r => r.When
            };
            shown = (sortAscending ? shown.OrderBy(key) : shown.OrderByDescending(key)).ThenByDescending(r => r.When).ToList();
            grid.DataSource = shown;
            foreach (DataGridViewColumn c in grid.Columns)
            {
                string p = c.DataPropertyName == nameof(AuditTrailRow.WhenText) ? nameof(AuditTrailRow.When) : c.DataPropertyName;
                c.HeaderCell.SortGlyphDirection = p == sortProperty ? (sortAscending ? SortOrder.Ascending : SortOrder.Descending) : SortOrder.None;
            }
            lblCount.Text = shown.Count == all.Count ? $"{shown.Count} entries" : $"{shown.Count} of {all.Count} entries";
        }
    }
}
