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
    /// <summary>
    /// Nov 24: Finance History is the record of finished work. "Completed" lists the paid statements of account, the paid service invoices and the
    /// issued collection receipts (newest completion date first, read-only); "Voided" lists the cancelled / voided ones. The working tabs
    /// (Service Invoices, Collection Receipts) show only open work. "Change log" is the old history view (hidden), "Statement history" is kept but hidden.
    /// </summary>
    public sealed class FinanceHistoryView : UserControl, IRefreshable
    {
        private readonly Panel host;
        private readonly Button btnSettled, btnVoided, btnLog, btnStatements;
        private FinanceStatementHistoryView? statements;
        private FinanceSettledView? settled;
        private FinanceVoidedView? voided;
        private ModuleHistoryView? log;

        /// <summary>Which list shows: "Completed" or "Voided" (the other views are not reachable from the buttons any more).</summary>
        public string CurrentList => host.Controls.Count == 1 && host.Controls[0] is FinanceVoidedView ? "Voided" : "Completed";
        /// <summary>The completed list (created when first shown).</summary>
        public FinanceSettledView? CompletedList => settled;
        /// <summary>The voided list (created when first shown).</summary>
        public FinanceVoidedView? VoidedList => voided;
        /// <summary>Shows the Completed (false) or Voided (true) list.</summary>
        public void ShowList(bool showVoided) => Show(!showVoided, false, showVoided);

        public FinanceHistoryView()
        {
            BackColor = AppTheme.Canvas;
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(44), Padding = new Padding(D(12), D(8), D(12), 0), BackColor = AppTheme.Canvas, WrapContents = false };
            Button Tab(string text) => new Button { Text = text, AutoSize = true, MinimumSize = new Size(D(130), D(30)), Margin = new Padding(0, 0, D(6), 0), Cursor = Cursors.Hand };
            btnSettled = Tab("✅  Completed");
            btnVoided = Tab("🚫  Voided");
            btnLog = Tab("📜  Change log");
            btnStatements = Tab("📑  Statement history");
            btnSettled.Click += (s, e) => Show(true);
            btnVoided.Click += (s, e) => Show(false, false, true);
            btnLog.Click += (s, e) => Show(false);
            btnStatements.Click += (s, e) => Show(false, true);
            // Nov 17: no "Change log" tab here: every change to invoices, statements and receipts is in Settings → Audit Trail (Area filter).
            // Nov 24: Finance History is completed transactions + voided ones; the statement-by-stage history is no longer a tab (its cancelled statements are under Voided).
            bar.Controls.Add(btnSettled); bar.Controls.Add(btnVoided);
            btnLog.Visible = false; btnStatements.Visible = false;
            host = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas };
            Controls.Add(host); Controls.Add(bar);
            Show(true);
        }

        private void Show(bool showSettled, bool showStatements = false, bool showVoided = false)
        {
            UIThemeHelper.SetButtonRole(btnSettled, showSettled ? "primary" : "secondary");
            UIThemeHelper.SetButtonRole(btnVoided, showVoided ? "primary" : "secondary");
            UIThemeHelper.SetButtonRole(btnLog, !showSettled && !showStatements && !showVoided ? "primary" : "secondary");
            UIThemeHelper.SetButtonRole(btnStatements, showStatements ? "primary" : "secondary");
            statements?.RefreshData();
            Control view = showStatements
                ? statements ??= new FinanceStatementHistoryView { Dock = DockStyle.Fill }
                : showVoided
                ? voided ??= new FinanceVoidedView { Dock = DockStyle.Fill }
                : showSettled
                ? settled ??= new FinanceSettledView { Dock = DockStyle.Fill }
                : log ??= new ModuleHistoryView("Billing & collections history", new[] { "Invoices", "CollectionReceipts", "StatementsOfAccount" }, isAdminOnly: true, paymentsSummary: true) { Dock = DockStyle.Fill };
            if (showVoided) voided?.RefreshData();
            if (host.Controls.Count == 1 && host.Controls[0] == view) return;
            host.Controls.Clear();
            host.Controls.Add(view);
        }

        public void RefreshData()
        {
            settled?.RefreshData();
            voided?.RefreshData();
            statements?.RefreshData();
            log?.RefreshData();
        }
    }

    /// <summary>Shared read-only list of Finance History rows (type, number, client, related documents, amount, date).</summary>
    public abstract class FinanceHistoryListBase : UserControl, IRefreshable
    {
        protected readonly DateTimePicker dtFrom, dtTo;
        protected readonly TextBox txtSearch;
        protected readonly DataGridView grid;
        protected readonly Label lblSummary;
        protected List<FinanceHistoryRow> records = new();
        protected int loadId;
        protected readonly bool isVoided;
        /// <summary>The rows currently listed in the grid, in grid order (read by the checks).</summary>
        public IReadOnlyList<FinanceHistoryRow> Shown => shown;
        private List<FinanceHistoryRow> shown = new();
        /// <summary>The grid (read-only; for the checks).</summary>
        public DataGridView Grid => grid;

        protected abstract string DateHeader { get; }
        protected abstract string ExportName { get; }
        protected abstract string EmptyText(DateTime from, DateTime to);
        protected abstract List<FinanceHistoryRow> LoadRows(DateTime from, DateTime to);

        protected FinanceHistoryListBase(bool voided)
        {
            isVoided = voided;
            BackColor = AppTheme.Canvas;
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(46), Padding = new Padding(D(12), D(8), D(12), 0), BackColor = AppTheme.Canvas, WrapContents = false };
            Label L(string t) => new Label { Text = t, AutoSize = true, Margin = new Padding(0, D(6), D(6), 0), ForeColor = AppTheme.TextSecondary };
            dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = voided ? DateTime.Today.AddYears(-5) : DateTime.Today.AddMonths(-3), Margin = new Padding(0, 0, D(12), 0) };
            dtTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = DateTime.Today, Margin = new Padding(0, 0, D(12), 0) };
            txtSearch = new TextBox { Width = D(300), PlaceholderText = "Search client, SOA, SI or receipt no.", Margin = new Padding(0, D(2), 0, 0) };
            filters.Controls.Add(L(voided ? "Voided from" : "Completed from")); filters.Controls.Add(dtFrom); filters.Controls.Add(L("to")); filters.Controls.Add(dtTo); filters.Controls.Add(txtSearch);
            dtFrom.ValueChanged += (s, e) => RefreshData();
            dtTo.ValueChanged += (s, e) => RefreshData();
            txtSearch.TextChanged += (s, e) => Bind();

            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.None
            };
            UIThemeHelper.ApplyGridTheme(grid);
            void Col(string name, string header, int weight, bool money = false) => grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header, FillWeight = weight,
                DefaultCellStyle = money ? new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } : new DataGridViewCellStyle()
            });
            Col("colDate", DateHeader, 70); Col("colType", "Type", 90); Col("colNumber", "Number", 80); Col("colClient", "Client", 120);
            Col("colRelated", voided ? "Related" : "Related documents", 120); Col("colAmount", "Amount", 80, true);
            if (voided) { Col("colReason", "Reason", 160); Col("colBy", "By", 60); }
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenRecord(e.RowIndex); };

            RowMenu.Attach(grid, (m, rows) =>
            {
                if (rows.Count == 0 || rows[0].Tag is not int i || i >= shown.Count) return;
                BuildMenu(shown[i], m, rows[0].Index);
            });

            lblSummary = new Label { Dock = DockStyle.Bottom, Height = D(32), Padding = new Padding(D(12), 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.Canvas };
            Controls.Add(grid); Controls.Add(lblSummary); Controls.Add(filters);
            Load += (s, e) => RefreshData();
        }

        public async void RefreshData()
        {
            int my = ++loadId;
            DateTime from = dtFrom.Value.Date, to = dtTo.Value.Date;
            try
            {
                var list = await Task.Run(() => LoadRows(from, to));
                if (my != loadId || IsDisposed) return;
                records = list;
                Bind();
            }
            catch (Exception ex)
            {
                ErrLog.Error(GetType().Name, ex);
                lblSummary.Text = "Finance history could not be loaded: " + ex.Message;
            }
        }

        /// <summary>Loads the list now (on this thread) and shows it. The checks use it instead of waiting for the background load.</summary>
        public void RefreshNow()
        {
            records = LoadRows(dtFrom.Value.Date, dtTo.Value.Date);
            Bind();
        }

        protected virtual string Note() => "";

        /// <summary>Types <paramref name="text"/> in the search box (the list follows).</summary>
        public void Search(string text) => txtSearch.Text = text ?? "";

        protected void Bind()
        {
            string q = txtSearch.Text.Trim();
            bool Match(string s) => s.Contains(q, StringComparison.OrdinalIgnoreCase);
            grid.Rows.Clear();
            shown = new List<FinanceHistoryRow>();
            decimal total = 0;
            foreach (var r in records)
            {
                if (q.Length > 0 && !(Match(r.Client) || Match(r.Number) || Match(r.Related) || Match(r.Kind) || Match(r.Reason))) continue;
                string date = Fmt.Date(r.Date) + (r.DateIsEstimate ? " *" : "");
                int row = isVoided
                    ? grid.Rows.Add(date, r.Kind, r.Number, r.Client, r.Related, r.Amount, r.Reason, r.By)
                    : grid.Rows.Add(date, r.Kind, r.Number, r.Client, r.Related, r.Amount);
                grid.Rows[row].Tag = shown.Count;
                shown.Add(r);
                total += r.Amount;
            }
            lblSummary.Text = (shown.Count == 0 ? EmptyText(dtFrom.Value, dtTo.Value) : $"{shown.Count:N0} record{(shown.Count == 1 ? "" : "s")} · PHP {total:N2}") + Note();
        }

        /// <summary>
        /// The right-click menu of a row. Everything is read-only; on the Completed list an administrator also gets Void receipt / Cancel invoice /
        /// Cancel statement (the app's existing flows and guards), since completed work is no longer reachable in the working tabs.
        /// </summary>
        public void BuildMenu(FinanceHistoryRow r, ContextMenuStrip m, int rowIndex = -1)
        {
            m.Items.Add(RowMenu.Item("Open (read only)", () => { if (rowIndex >= 0) OpenRecord(rowIndex); else OpenRow(r); }));
            if (!isVoided && SessionContext.IsAdmin)
            {
                m.Items.Add(RowMenu.Sep());
                if (r.Kind == FinanceHistoryRow.Receipt) m.Items.Add(RowMenu.Danger("Void receipt…", () => { if (FinanceHistoryActions.VoidReceipt(this, r)) AfterChange(); }));
                else if (r.Kind == FinanceHistoryRow.Invoice && r.Record is Invoice inv) m.Items.Add(RowMenu.Danger("Cancel invoice…", () => { if (FinanceHistoryActions.CancelInvoice(this, inv)) AfterChange(); }));
                else if (r.Kind == FinanceHistoryRow.Soa && r.Record is StatementOfAccount soa) m.Items.Add(RowMenu.Danger("Cancel statement…", () => { if (FinanceHistoryActions.CancelStatement(this, soa)) AfterChange(); }));
            }
            m.Items.Add(RowMenu.Sep());
            m.Items.Add(RowMenu.Item("📊 Export table to CSV...", () => DataExportHelper.ExportToCsv(grid, ExportName, ExportName)));
        }

        /// <summary>After a void / cancel: both history lists reload and the other Payments tabs (lists, waiting panels, badges) reload.</summary>
        protected void AfterChange()
        {
            for (Control? p = Parent; p != null; p = p.Parent)
                if (p is FinanceHistoryView h) { h.RefreshData(); break; }
            RefreshData();
            FinanceHubView.Of(this)?.RefreshOtherTabs();
        }

        private void OpenRow(FinanceHistoryRow r)
        {
            if (r.Kind == FinanceHistoryRow.Soa) FinanceHubView.Of(this)?.ShowStatements(r.Number);
            else if (r.Record != null) RecordDetailsDialog.ShowDetails(this, r.Record);
        }

        private void OpenRecord(int rowIndex)
        {
            if (grid.Rows[rowIndex].Tag is not int i || i >= shown.Count) return;
            var r = shown[i];
            if (r.Kind == FinanceHistoryRow.Soa) FinanceHubView.Of(this)?.ShowStatements(r.Number);
            else if (r.Record != null) RecordDetailsDialog.ShowDetails(this, r.Record); // read-only details with Print summary
        }
    }

    /// <summary>Completed: paid statements, paid service invoices and issued collection receipts, newest completion date first.</summary>
    public sealed class FinanceSettledView : FinanceHistoryListBase
    {
        private List<DuplicateBilling> billedTwice = new();
        protected override string DateHeader => "Completed on";
        protected override string ExportName => "Completed_Finance_History";
        protected override string EmptyText(DateTime from, DateTime to) => $"Nothing was completed between {Fmt.Date(from)} and {Fmt.Date(to)}.";
        public FinanceSettledView() : base(false) { }

        protected override List<FinanceHistoryRow> LoadRows(DateTime from, DateTime to)
        {
            List<DuplicateBilling> dup = new();
            try { dup = MJdatabase.GetTripsBilledMoreThanOnce(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            billedTwice = dup;
            return MJdatabase.GetCompletedFinanceHistory(from, to);
        }

        protected override string Note()
        {
            lblSummary.ForeColor = billedTwice.Count > 0 ? AppTheme.DangerText : AppTheme.TextSecondary;
            return billedTwice.Count == 0 ? "" : $"   ·   ⚠ {billedTwice.Count} trip{(billedTwice.Count == 1 ? " is" : "s are")} billed more than once: {string.Join("; ", billedTwice.Take(3).Select(d => $"{d.TripLabel} ({string.Join(" + ", d.Invoices)})"))}{(billedTwice.Count > 3 ? "…" : "")}";
        }
    }

    /// <summary>Voided: cancelled statements, cancelled service invoices and voided collection receipts, newest first, with the reason when one was kept.</summary>
    public sealed class FinanceVoidedView : FinanceHistoryListBase
    {
        protected override string DateHeader => "Voided on";
        protected override string ExportName => "Voided_Finance_History";
        protected override string EmptyText(DateTime from, DateTime to) => "Nothing has been cancelled or voided in this period.";
        public FinanceVoidedView() : base(true) { }

        protected override List<FinanceHistoryRow> LoadRows(DateTime from, DateTime to)
            => MJdatabase.GetVoidedFinanceHistory().Where(r => r.Date.Date >= from.Date && r.Date.Date <= to.Date).ToList();
    }

    /// <summary>The existing void / cancel flows (same dialogs, guards and database calls as the old row menus), used by Finance History → Completed.</summary>
    public static class FinanceHistoryActions
    {
        /// <summary>Voids the issued receipt (all its rows) after the same confirmation Collection Receipts used. True when it was voided.</summary>
        public static bool VoidReceipt(Control owner, FinanceHistoryRow r)
        {
            try
            {
                var res = MessageBox.Show(owner.FindForm(), $"Void Collection Receipt {r.Number}?\n\n" + MJdatabase.DeletePolicyNote("CollectionReceipts"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res != DialogResult.Yes) return false;
                var grp = MJdatabase.GetReceiptRows(r.Number);
                if (grp.Count == 0 && r.Record is CollectionReceipt cr) grp.Add(cr);
                bool done = grp.Count > 0;
                foreach (var row in grp) done &= MJdatabase.DeleteRecord("CollectionReceipts", row.Id);
                if (!done) { MessageBox.Show(owner.FindForm(), "Failed to void the collection receipt. It may be locked or referenced by other financial records.", "Void Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
                QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex) { ErrLog.Error("FinanceHistory", ex); MessageBox.Show(owner.FindForm(), $"Void operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
        }

        /// <summary>Cancels the invoice through the existing guards and the cancel dialog (reason, typing mistake or void). True when it was cancelled.</summary>
        public static bool CancelInvoice(Control owner, Invoice inv)
        {
            try
            {
                string? block = MJdatabase.ValidateRecordDeletion("Invoices", inv.Id);
                if (!string.IsNullOrEmpty(block)) { MessageBox.Show(owner.FindForm(), block, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                if (!Forms.CancelInvoiceDialog.Prompt(owner.FindForm(), inv.InvoiceNumber, out bool typingMistake, out string reason)) return false;
                if (!MJdatabase.CancelInvoice(inv.Id, typingMistake, reason)) { MessageBox.Show(owner.FindForm(), "Failed to cancel the Service Invoice. It may be locked or referenced by collections/statements of account.", "Cancel Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
                QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex) { ErrLog.Error("FinanceHistory", ex); MessageBox.Show(owner.FindForm(), $"Cancel operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
        }

        /// <summary>Cancels the statement through the existing guards and dialog; a paid statement says to void its receipt first (here, in Completed).</summary>
        public static bool CancelStatement(Control owner, StatementOfAccount soa)
        {
            string why = MJdatabase.WhySoaCannotBeCancelled(soa);
            if (why.Length > 0) { QuietBox.Show(why, "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
            int invoices = 0;
            try { invoices = MJdatabase.GetTripInvoices().Count(i => i.SoaId == soa.Id && i.Kind.Length > 0); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (!Forms.CancelStatementDialog.Prompt(owner.FindForm(), soa.SoaNumber, soa.ClientName, invoices, soa.TotalAmount, out string reason)) return false;
            try
            {
                MJdatabase.CancelStatementOfAccount(soa.Id, reason);
                QuietBox.Show(MJdatabase.LastDeleteSummary, "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex) { ErrLog.Error("FinanceHistory", ex); MessageBox.Show(owner.FindForm(), $"The statement could not be cancelled: {ex.Message}", "Cancel statement", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
        }
    }
}