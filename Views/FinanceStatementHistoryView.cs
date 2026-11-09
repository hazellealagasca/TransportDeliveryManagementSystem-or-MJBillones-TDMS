using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Finance History > Statement history: every statement of account that was saved, newest first, with where it stands
    /// (waiting for invoice, invoiced, collected, cancelled). It reads the saved statements, so it fills itself when a statement is saved.
    /// </summary>
    public sealed class FinanceStatementHistoryView : UserControl, IRefreshable
    {
        private readonly DataGridView grid = new();
        private readonly Label lblTitle = new() { Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 11F), Padding = new Padding(12, 10, 0, 0), UseMnemonic = false };
        private List<StatementHistoryRow> rows = new();

        public FinanceStatementHistoryView()
        {
            BackColor = AppTheme.Canvas;
            lblTitle.ForeColor = AppTheme.TextPrimary;
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = AppTheme.Surface; grid.GridColor = AppTheme.Border; grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Canvas; grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            grid.DefaultCellStyle.BackColor = AppTheme.Surface; grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            grid.RowTemplate.Height = 32; grid.ColumnHeadersHeight = 34; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            void Col(string h, float w) => grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = h, FillWeight = w, MinimumWidth = 70 });
            Col("SOA #", 12); Col("Statement date", 11); Col("Client", 20); Col("Period covered", 18); Col("Trips", 6); Col("Total due", 11); Col("Service invoices", 16); Col("Stage", 13);
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 7) return;
                string v = e.Value?.ToString() ?? "";
                e.CellStyle.Font = new Font("Segoe UI Semibold", 9F);
                e.CellStyle.ForeColor = v == "Cancelled" ? AppTheme.TextMuted : v == "Collected" ? AppTheme.SuccessText : v == "Waiting for invoice" ? AppTheme.WarningText : AppTheme.InfoText;
            };
            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12), BackColor = AppTheme.Canvas };
            host.Controls.Add(grid);
            Controls.Add(host); Controls.Add(lblTitle);
            RefreshData();
        }

        public void RefreshData()
        {
            try { rows = MJdatabase.GetStatementHistory(); } catch (Exception ex) { ErrLog.Swallowed(ex); rows = new(); }
            grid.Rows.Clear();
            foreach (var r in rows)
                grid.Rows.Add(r.Soa.SoaNumber, r.Soa.IssueDate.ToString("MMM d, yyyy"), r.Soa.ClientName, r.Soa.PeriodCovered, r.Trips, $"₱{r.Soa.TotalAmount:N2}",
                    r.Invoices.Length > 0 ? r.Invoices + (r.TripsWithoutInvoice > 0 ? $"  (+{r.TripsWithoutInvoice} to invoice)" : "") : (r.TripsWithoutInvoice > 0 ? $"{r.TripsWithoutInvoice} to invoice" : ""), r.Stage);
            lblTitle.Text = $"Statement history · {rows.Count} statement{(rows.Count == 1 ? "" : "s")} (saved automatically with each statement)";
        }
    }
}
