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
            UIThemeHelper.ScaleFixedLayout(this); // fixed 100%-scale layout: grow it to the screen's scaling
            InitializeComponent();
            LoadClients();
            LoadInvoices();
        }

        private void InitializeComponent()
        {
            this.Text = "Generate statement of account (SOA)";
            this.Size = new Size(980, 700);
            this.MinimumSize = new Size(860, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Pal.C(153, 27, 27),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Statement of Account (SOA): batch & bill",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Consolidate and issue itemized monthly or weekly statements of account per client company.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(254, 205, 211),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 14),
                BackColor = Pal.C(248, 250, 252)
            };

            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Pal.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 10, 14, 10)
            };

            FlowLayoutPanel flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            // Each filter is a small caption-over-field block that sizes itself.
            TableLayoutPanel Field(string caption, Control input)
            {
                var t = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 16, 4) };
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                t.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 0, 0, 4), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) }, 0, 0);
                input.Margin = new Padding(0);
                t.Controls.Add(input, 0, 1);
                return t;
            }
            cbClient = new ComboBox { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cbClient.SelectedIndexChanged += (s, e) => FilterInvoices();
            dtpStart = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpStart.ValueChanged += (s, e) => FilterInvoices();
            dtpEnd = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpEnd.ValueChanged += (s, e) => FilterInvoices();
            FlowLayoutPanel pnlBtnBlock = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0, 20, 0, 4) };
            Button btnSelectAll = new Button
            {
                Text = "☑ Select all",
                AutoSize = true,
                MinimumSize = new Size(110, 30),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Pal.C(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectAll.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
            btnSelectAll.Click += (s, e) => SetAllChecked(true);

            Button btnUnselectAll = new Button
            {
                Text = "☐ Unselect all",
                AutoSize = true,
                MinimumSize = new Size(110, 30),
                Margin = new Padding(0),
                BackColor = Pal.C(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUnselectAll.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
            btnUnselectAll.Click += (s, e) => SetAllChecked(false);

            pnlBtnBlock.Controls.Add(btnSelectAll);
            pnlBtnBlock.Controls.Add(btnUnselectAll);

            flowFilters.Controls.Add(Field("Client company", cbClient));
            flowFilters.Controls.Add(Field("Period from", dtpStart));
            flowFilters.Controls.Add(Field("Period to", dtpEnd));
            flowFilters.Controls.Add(pnlBtnBlock);
            pnlFilter.Controls.Add(flowFilters);

            Panel pnlGap1 = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Margin = new Padding(0, 12, 0, 12)
            };

            gridInvoices = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
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
            var dateCol = new DataGridViewTextBoxColumn { HeaderText = "Issue date", FillWeight = 13, MinimumWidth = 90, ReadOnly = true, DataPropertyName = "IssueDate", DefaultCellStyle = { Format = "MM/dd/yyyy" } };
            var grossCol = new DataGridViewTextBoxColumn { HeaderText = "Gross hauling", FillWeight = 16, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "TotalAmount", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var dedCol = new DataGridViewTextBoxColumn { HeaderText = "Incident deductions", FillWeight = 17, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "Deductions", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var netCol = new DataGridViewTextBoxColumn { HeaderText = "Net due", FillWeight = 16, MinimumWidth = 110, ReadOnly = true, DataPropertyName = "NetAmount", DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            var statusCol = new DataGridViewTextBoxColumn { HeaderText = "Status", FillWeight = 10, MinimumWidth = 80, ReadOnly = true, DataPropertyName = "Status" };
            var soaCol = new DataGridViewTextBoxColumn { HeaderText = "Billed in SOA", FillWeight = 10, MinimumWidth = 80, ReadOnly = true };
            gridInvoices.Columns.AddRange(chkCol, invNumCol, dateCol, grossCol, dedCol, netCol, statusCol, soaCol);
            gridInvoices.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) CalculateTotals(); };
            gridInvoices.CurrentCellDirtyStateChanged += (s, e) => { if (gridInvoices.IsCurrentCellDirty) gridInvoices.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            pnlGridCard.Controls.Add(gridInvoices);

            Panel pnlGap2 = new Panel { Dock = DockStyle.Bottom, Height = 12, BackColor = Color.Transparent };
            Panel pnlSummary = new Panel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Pal.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16, 10, 16, 10)
            };

            TableLayoutPanel tblSummaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1
            };
            tblSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblSummaryLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            FlowLayoutPanel pnlTotals = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0) };
            lblSelectedCount = new Label { Text = "Selected invoices: 0", AutoSize = true, Margin = new Padding(0, 0, 0, 2), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
            lblTotalGross = new Label { Text = "Gross hauling: PHP 0.00", AutoSize = true, Margin = new Padding(0, 0, 0, 2), Font = new Font("Segoe UI", 8.5F) };
            lblTotalDeductions = new Label { Text = "Total deductions: - PHP 0.00", AutoSize = true, Margin = new Padding(0, 0, 0, 2), Font = new Font("Segoe UI", 8.5F), ForeColor = Pal.C(220, 38, 38) };
            lblNetPayable = new Label { Text = "Total net payable: PHP 0.00", AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Pal.C(153, 27, 27), Margin = new Padding(0, 4, 0, 0) };
            pnlTotals.Controls.Add(lblSelectedCount);
            pnlTotals.Controls.Add(lblTotalGross);
            pnlTotals.Controls.Add(lblTotalDeductions);
            pnlTotals.Controls.Add(lblNetPayable);
            FlowLayoutPanel flowSummaryActions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(12, 0, 0, 0)
            };

            Button MakeBtn(string text, Color back, Color fore, int minW)
            {
                var b = new Button
                {
                    Text = text,
                    AutoSize = true,
                    MinimumSize = new Size(minW, 38),
                    Padding = new Padding(10, 0, 10, 0),
                    Margin = new Padding(8, 0, 0, 0),
                    BackColor = back,
                    ForeColor = fore,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }
            btnClose = MakeBtn("Close", Pal.C(203, 213, 225), Pal.C(30, 41, 59), 100);
            btnClose.Click += (s, e) => this.Close();
            btnPrintPreview = MakeBtn("🖨️ Preview statement", Pal.C(71, 85, 105), Pal.White, 170);
            btnPrintPreview.Click += BtnPrintPreview_Click;
            btnSaveAndPrint = MakeBtn("💾 Save & print official SOA", Pal.C(153, 27, 27), Pal.White, 230);
            btnSaveAndPrint.Click += BtnSaveAndPrint_Click;

            flowSummaryActions.Controls.Add(btnClose);
            flowSummaryActions.Controls.Add(btnPrintPreview);

            flowSummaryActions.Controls.Add(btnSaveAndPrint);
            tblSummaryLayout.Controls.Add(pnlTotals, 0, 0);
            tblSummaryLayout.Controls.Add(flowSummaryActions, 1, 0);
            pnlSummary.Controls.Add(tblSummaryLayout);

            this.CancelButton = btnClose;
            this.Shown += (s, e) =>
            {
                // The theme can restyle Close as a smaller secondary button; keep the three footer buttons the same height.
                btnClose.AutoSize = false;
                btnClose.Size = new Size(Math.Max(btnClose.Width, btnPrintPreview.Height * 2), btnPrintPreview.Height);
                UIThemeHelper.FitColumnHeaders(gridInvoices);
            };
            pnlMainContainer.Controls.Add(pnlGridCard);
            pnlMainContainer.Controls.Add(pnlGap2);
            pnlMainContainer.Controls.Add(pnlSummary);
            pnlMainContainer.Controls.Add(pnlGap1);
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

            lblSelectedCount.Text = $"Selected invoices: {selected.Count} / {gridInvoices.Rows.Count}";
            lblTotalGross.Text = $"Gross hauling: PHP {gross:N2}";
            lblTotalDeductions.Text = $"Total deductions: - PHP {deductions:N2}";
            lblNetPayable.Text = $"Total net payable: PHP {net:N2}";
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

            QuietBox.Show($"Statement of Account #{soa.SoaNumber} successfully generated and saved!", "SOA Issued", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
