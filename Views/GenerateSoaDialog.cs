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
    public class GenerateSoaDialog : Form
    {
        private ComboBox cbClient = null!;
        private DateTimePicker dtpStart = null!;
        private DateTimePicker dtpEnd = null!;
        private DataGridView gridInvoices = null!;
        private Label lblSelectedCount = null!;
        private Label lblTotalGross = null!;
        private Label lblTotalDeductions = null!;
        private Label lblNetPayable = null!;
        private Button btnSaveAndPrint = null!;
        private Button btnPrintPreview = null!;
        private Button btnClose = null!;
        private List<Invoice> allInvoices = new List<Invoice>();

        public GenerateSoaDialog()
        {
            InitializeComponent();
            LoadClients();
            LoadInvoices();
        }

        private void InitializeComponent()
        {
            this.Text = "Statement of Account (SOA) Generator - M.J. Billones Transport Service";
            this.Size = new Size(980, 700);
            this.MinimumSize = new Size(860, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(153, 27, 27),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "STATEMENT OF ACCOUNT (SOA) BATCHING & BILLING",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Consolidate and issue itemized monthly or weekly statements of account per client company.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(254, 205, 211),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 14),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 12)
            };

            FlowLayoutPanel flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            Panel pnlClientBlock = new Panel { AutoSize = true, Margin = new Padding(0, 0, 16, 4) };
            Label lblClient = new Label { Text = "Client Company:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            cbClient = new ComboBox { Dock = DockStyle.Top, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbClient.SelectedIndexChanged += (s, e) => FilterInvoices();
            pnlClientBlock.Controls.Add(cbClient);
            pnlClientBlock.Controls.Add(lblClient);

            Panel pnlStartBlock = new Panel { AutoSize = true, Margin = new Padding(0, 0, 12, 4) };
            Label lblFrom = new Label { Text = "Period From:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            dtpStart = new DateTimePicker { Dock = DockStyle.Top, Width = 130, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpStart.ValueChanged += (s, e) => FilterInvoices();
            pnlStartBlock.Controls.Add(dtpStart);
            pnlStartBlock.Controls.Add(lblFrom);

            Panel pnlEndBlock = new Panel { AutoSize = true, Margin = new Padding(0, 0, 16, 4) };
            Label lblTo = new Label { Text = "Period To:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            dtpEnd = new DateTimePicker { Dock = DockStyle.Top, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpEnd.ValueChanged += (s, e) => FilterInvoices();
            pnlEndBlock.Controls.Add(dtpEnd);
            pnlEndBlock.Controls.Add(lblTo);

            Panel pnlBtnBlock = new Panel { AutoSize = true, Margin = new Padding(0, 16, 0, 4) };
            Button btnSelectAll = new Button
            {
                Text = "☑ Select All",
                Size = new Size(110, 28),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnSelectAll.Click += (s, e) => SetAllChecked(true);

            Button btnUnselectAll = new Button
            {
                Text = "☐ Unselect All",
                Size = new Size(110, 28),
                Location = new Point(118, 0),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUnselectAll.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnUnselectAll.Click += (s, e) => SetAllChecked(false);

            pnlBtnBlock.Controls.Add(btnSelectAll);
            pnlBtnBlock.Controls.Add(btnUnselectAll);

            flowFilters.Controls.Add(pnlClientBlock);
            flowFilters.Controls.Add(pnlStartBlock);
            flowFilters.Controls.Add(pnlEndBlock);
            flowFilters.Controls.Add(pnlBtnBlock);
            pnlFilter.Controls.Add(flowFilters);

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Margin = new Padding(0, 12, 0, 12)
            };

            gridInvoices = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 32 },
                EnableHeadersVisualStyles = false
            };

            var chkCol = new DataGridViewCheckBoxColumn { HeaderText = "Include", Width = 60, Name = "colInclude" };
            var invNumCol = new DataGridViewTextBoxColumn { HeaderText = "Invoice #", FillWeight = 18, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "InvoiceNumber" };
            var dateCol = new DataGridViewTextBoxColumn { HeaderText = "Issue Date", FillWeight = 13, MinimumWidth = 90, ReadOnly = true, DataPropertyName = "IssueDate", DefaultCellStyle = { Format = "MM/dd/yyyy" } };
            var grossCol = new DataGridViewTextBoxColumn { HeaderText = "Gross Hauling", FillWeight = 16, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "TotalAmount", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var dedCol = new DataGridViewTextBoxColumn { HeaderText = "Incident Deductions", FillWeight = 17, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "Deductions", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var netCol = new DataGridViewTextBoxColumn { HeaderText = "Net Due", FillWeight = 16, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "NetAmount", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var statusCol = new DataGridViewTextBoxColumn { HeaderText = "Status", FillWeight = 10, MinimumWidth = 80, ReadOnly = true, DataPropertyName = "Status" };
            var soaCol = new DataGridViewTextBoxColumn { HeaderText = "Billed In SOA", FillWeight = 10, MinimumWidth = 80, ReadOnly = true };

            gridInvoices.Columns.AddRange(chkCol, invNumCol, dateCol, grossCol, dedCol, netCol, statusCol, soaCol);
            gridInvoices.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) CalculateTotals(); };
            gridInvoices.CurrentCellDirtyStateChanged += (s, e) => { if (gridInvoices.IsCurrentCellDirty) gridInvoices.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            pnlGridCard.Controls.Add(gridInvoices);

            Panel pnlSummary = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 105,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16, 10, 16, 10),
                Margin = new Padding(0, 12, 0, 0)
            };

            TableLayoutPanel tblSummaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tblSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblSummaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlTotals = new Panel { Dock = DockStyle.Fill };
            lblSelectedCount = new Label { Text = "Selected Invoices: 0", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
            lblTotalGross = new Label { Text = "Gross Hauling: PHP 0.00", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            lblTotalDeductions = new Label { Text = "Total Deductions: - PHP 0.00", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(220, 38, 38) };
            lblNetPayable = new Label { Text = "TOTAL NET PAYABLE: PHP 0.00", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(153, 27, 27), Margin = new Padding(0, 4, 0, 0) };

            pnlTotals.Controls.Add(lblNetPayable);
            pnlTotals.Controls.Add(lblTotalDeductions);
            pnlTotals.Controls.Add(lblTotalGross);
            pnlTotals.Controls.Add(lblSelectedCount);

            FlowLayoutPanel flowSummaryActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true
            };

            btnSaveAndPrint = new Button
            {
                Text = "💾 SAVE & PRINT OFFICIAL SOA",
                Size = new Size(240, 36),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.FromArgb(153, 27, 27),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveAndPrint.FlatAppearance.BorderSize = 0;
            btnSaveAndPrint.Click += BtnSaveAndPrint_Click;

            btnPrintPreview = new Button
            {
                Text = "🖨️ PREVIEW STATEMENT",
                Size = new Size(160, 32),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrintPreview.FlatAppearance.BorderSize = 0;
            btnPrintPreview.Click += BtnPrintPreview_Click;

            btnClose = new Button
            {
                Text = "CLOSE",
                Size = new Size(90, 32),
                Margin = new Padding(0),
                BackColor = Color.FromArgb(203, 213, 225),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            flowSummaryActions.Controls.Add(btnSaveAndPrint);
            flowSummaryActions.Controls.Add(btnClose);
            flowSummaryActions.Controls.Add(btnPrintPreview);

            tblSummaryLayout.Controls.Add(pnlTotals, 0, 0);
            tblSummaryLayout.Controls.Add(flowSummaryActions, 1, 0);
            pnlSummary.Controls.Add(tblSummaryLayout);

            pnlMainContainer.Controls.Add(pnlGridCard);
            pnlMainContainer.Controls.Add(pnlSummary);
            pnlMainContainer.Controls.Add(pnlFilter);

            this.Controls.Add(pnlMainContainer);
            this.Controls.Add(pnlHeader);
        }

        private void LoadClients()
        {
            var clients = MJdatabase.GetLookupItems("Client Company");
            cbClient.Items.Clear();
            foreach (var c in clients) cbClient.Items.Add(c);
            if (cbClient.Items.Count > 0) cbClient.SelectedIndex = 0;
        }

        private void LoadInvoices()
        {
            allInvoices = MJdatabase.GetInvoices();
            FilterInvoices();
        }

        private void FilterInvoices()
        {
            string selClient = cbClient.SelectedItem?.ToString() ?? "";
            DateTime sDate = dtpStart.Value.Date;
            DateTime eDate = dtpEnd.Value.Date;

            gridInvoices.Rows.Clear();

            var filtered = allInvoices.Where(inv =>
                inv.ClientName.Equals(selClient, StringComparison.OrdinalIgnoreCase) &&
                inv.IssueDate.Date >= sDate && inv.IssueDate.Date <= eDate
            ).ToList();

            foreach (var inv in filtered)
            {
                int rIdx = gridInvoices.Rows.Add();
                var row = gridInvoices.Rows[rIdx];
                row.Cells[0].Value = true;
                row.Cells[1].Value = inv.InvoiceNumber;
                row.Cells[2].Value = inv.IssueDate.ToString("MM/dd/yyyy");
                row.Cells[3].Value = inv.TotalAmount.ToString("N2");
                row.Cells[4].Value = inv.Deductions.ToString("N2");
                row.Cells[5].Value = inv.NetAmount.ToString("N2");
                row.Cells[6].Value = inv.Status;
                row.Cells[7].Value = inv.SoaId > 0 ? $"SOA #{inv.SoaId}" : "Unbundled";
                row.Tag = inv;
            }

            CalculateTotals();
        }

        private void SetAllChecked(bool check)
        {
            foreach (DataGridViewRow r in gridInvoices.Rows)
            {
                r.Cells[0].Value = check;
            }
            CalculateTotals();
        }

        private List<Invoice> GetSelectedInvoices()
        {
            var list = new List<Invoice>();
            foreach (DataGridViewRow r in gridInvoices.Rows)
            {
                bool isChecked = Convert.ToBoolean(r.Cells[0].Value ?? false);
                if (isChecked && r.Tag is Invoice inv)
                {
                    list.Add(inv);
                }
            }
            return list;
        }

        private void CalculateTotals()
        {
            var selected = GetSelectedInvoices();
            decimal gross = selected.Sum(i => i.TotalAmount);
            decimal deductions = selected.Sum(i => i.Deductions);
            decimal net = selected.Sum(i => i.NetAmount);

            lblSelectedCount.Text = $"Selected Invoices: {selected.Count} / {gridInvoices.Rows.Count}";
            lblTotalGross.Text = $"Gross Hauling: PHP {gross:N2}";
            lblTotalDeductions.Text = $"Total Deductions: - PHP {deductions:N2}";
            lblNetPayable.Text = $"TOTAL NET PAYABLE: PHP {net:N2}";
        }

        private void BtnSaveAndPrint_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedInvoices();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least 1 invoice to include in the Statement of Account.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string client = cbClient.SelectedItem?.ToString() ?? "Client";
            string period = $"{dtpStart.Value:MMM dd, yyyy} - {dtpEnd.Value:MMM dd, yyyy}";
            decimal net = selected.Sum(i => i.NetAmount);

            StatementOfAccount soa = new StatementOfAccount
            {
                ClientName = client,
                PeriodCovered = period,
                IssueDate = DateTime.Today,
                TotalAmount = net,
                Status = "Issued"
            };

            int soaId = MJdatabase.SaveStatementOfAccount(soa, selected.Select(i => i.Id).ToList());
            soa.Id = soaId;

            MessageBox.Show($"Statement of Account #{soa.SoaNumber} successfully generated and saved!", "SOA Issued", MessageBoxButtons.OK, MessageBoxIcon.Information);

            PrintHelper.PrintStatementOfAccount(soa, selected);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnPrintPreview_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedInvoices();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least 1 invoice to preview.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string client = cbClient.SelectedItem?.ToString() ?? "Client";
            string period = $"{dtpStart.Value:MMM dd, yyyy} - {dtpEnd.Value:MMM dd, yyyy}";
            decimal net = selected.Sum(i => i.NetAmount);

            StatementOfAccount soa = new StatementOfAccount
            {
                SoaNumber = "SOA-PREVIEW-" + DateTime.Now.ToString("yyyyMMdd"),
                ClientName = client,
                PeriodCovered = period,
                IssueDate = DateTime.Today,
                TotalAmount = net,
                Status = "Draft Preview"
            };

            PrintHelper.PrintStatementOfAccount(soa, selected);
        }
    }
}
