using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class BillingView : UserControl, IRefreshable
    {
        private Invoice? selectedInvoice = null;
        private List<Invoice> allInvoices = new List<Invoice>();
        private string currentSortColumn = "InvoiceNumber";
        private bool isSortAscending = true;
        private List<Delivery> completedDeliveries = new List<Delivery>();
        private bool isBindingForm = false;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        public BillingView()
        {
            InitializeComponent();
            gridInvoices.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridInvoices.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridInvoices.Rows[e.RowIndex].DataBoundItem is Invoice item) RecordDetailsDialog.ShowDetails(this, item); };
            gridInvoices.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridInvoices.Columns.Count) return;
                string col = gridInvoices.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            LoadData();

            btnGenerateSoa.Visible = false;
            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("🧾 Generate Statement of Account (SOA)", null, (s, e) => btnGenerateSoa.PerformClick());
            menuActions.Items.Add("📊 Export Invoices to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable"));
            menuActions.Items.Add("🖨️ Print Invoice Register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 + New / Form ▾",
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Width = 72,
                Height = 30,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }

        private void SetFormVisibility(bool visible)
        {
            pnlForm.Visible = visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].Width = visible ? 480F : 0F;
                tblMasterContent.ColumnStyles[0].SizeType = SizeType.Absolute;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].Width = 100F;
                tblMasterContent.ColumnStyles[1].SizeType = SizeType.Percent;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "✕ Hide Form ▴" : "📝 + New / Form ▾";
                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void SetupGridColumns()
        {
            gridInvoices.AutoGenerateColumns = false;
            gridInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridInvoices.ScrollBars = ScrollBars.Both;
            gridInvoices.AllowUserToResizeColumns = true;
            gridInvoices.ShowCellToolTips = true;
            gridInvoices.Columns.Clear();
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNumber", HeaderText = "Invoice #", FillWeight = 12F, MinimumWidth = 100, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoadingScheduleNumber", HeaderText = "Schedule #", FillWeight = 12F, MinimumWidth = 100, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 12F, MinimumWidth = 100, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client", FillWeight = 18F, MinimumWidth = 120, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 10F, MinimumWidth = 85, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GrossAmount", HeaderText = "Gross", DefaultCellStyle = { Format = "N2" }, FillWeight = 12F, MinimumWidth = 90, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Deductions", HeaderText = "Deductions", DefaultCellStyle = { Format = "N2" }, FillWeight = 11F, MinimumWidth = 85, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NetAmount", HeaderText = "Net Due", DefaultCellStyle = { Format = "N2" }, FillWeight = 12F, MinimumWidth = 90, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 10F, MinimumWidth = 80, ReadOnly = true });
            gridInvoices.CellFormatting -= GridInvoices_CellFormatting;
            gridInvoices.CellFormatting += GridInvoices_CellFormatting;
        }

        private void GridInvoices_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridInvoices.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.FromArgb(220, 252, 231);
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    e.CellStyle.Font = new Font(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Partial", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9);
                    e.CellStyle.Font = new Font(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.FromArgb(241, 245, 249);
                    e.CellStyle.ForeColor = Color.FromArgb(51, 65, 85);
                }
            }
        }

        private void CbDelivery_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isBindingForm) return;
            if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
            {
                var del = completedDeliveries[cbDelivery.SelectedIndex];
                var schedules = MJdatabase.GetLoadingSchedules();
                var ls = schedules.FirstOrDefault(s => s.Id == del.LoadingScheduleId);

                if (ls != null)
                {
                    txtLoadingSchedule.Text = $"{ls.ScheduleNumber} | {ls.TotalCrates:N0} Crates ({ls.GCrate}G + {ls.ECrate}E) | {ls.Destination}";
                }
                else if (!string.IsNullOrWhiteSpace(del.LoadingScheduleInfo))
                {
                    txtLoadingSchedule.Text = $"Loading Info: {del.LoadingScheduleInfo}";
                }
                else
                {
                    txtLoadingSchedule.Text = "(No Loading Schedule Linked)";
                }

                string clientName = "";
                if (ls != null && !string.IsNullOrWhiteSpace(ls.ClientName))
                {
                    clientName = ls.ClientName.Trim();
                }
                else if (!string.IsNullOrWhiteSpace(del.LoadingScheduleInfo))
                {
                    string raw = del.LoadingScheduleInfo.Trim();
                    if (raw.Contains("-"))
                    {
                        var parts = raw.Split(new[] { '-' }, 2);
                        raw = parts.Length > 1 ? parts[1].Trim() : raw;
                    }
                    int pIdx = raw.LastIndexOf('(');
                    clientName = pIdx > 0 ? raw.Substring(0, pIdx).Trim() : raw.Trim();
                }

                if (!string.IsNullOrWhiteSpace(clientName))
                {
                    if (!cbClient.Items.Contains(clientName))
                    {
                        cbClient.Items.Add(clientName);
                    }
                    cbClient.SelectedItem = clientName;
                    cbClient.Text = clientName;
                }

                string tariffDesc;
                decimal calculatedAmount = MJdatabase.CalculateDeliveryTariff(del, out tariffDesc);
                lblTariffMatch.Text = $"✓ {tariffDesc} -> PHP {calculatedAmount:N2}";
                lblTariffMatch.Visible = true;

                if (calculatedAmount > numTotalAmount.Maximum) numTotalAmount.Maximum = calculatedAmount * 2;
                numTotalAmount.Value = Math.Max(numTotalAmount.Minimum, Math.Min(numTotalAmount.Maximum, calculatedAmount));

                var incidents = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == del.Id && !ir.ChargeableToCrew && ir.Status != "Rejected");
                decimal totalDeductions = incidents.Sum(ir => ir.EstimatedCost);
                if (totalDeductions > numDeductions.Maximum) numDeductions.Maximum = totalDeductions * 2;
                numDeductions.Value = Math.Max(numDeductions.Minimum, Math.Min(numDeductions.Maximum, totalDeductions));

                RecalculateNet(sender, e);
            }
        }

        private void UpdateClientContactHint()
        {
            string cname = cbClient.Text.Trim();
            if (!string.IsNullOrWhiteSpace(cname))
            {
                var cl = MJdatabase.GetClientByCompanyName(cname);
                if (cl != null && !string.IsNullOrEmpty(cl.ContactNumber))
                {
                    lblClient.Text = $"Client: (📞 {cl.ContactPerson} {cl.ContactNumber})";
                    return;
                }
            }
            lblClient.Text = "Client Company:";
        }

        private void RecalculateNet(object? sender, EventArgs e)
        {
            if (isBindingForm) return;
            decimal net = numTotalAmount.Value - numDeductions.Value;
            lblNetAmount.Text = $"Net Due Amount: PHP {net:N2}";
            DateTime baseDate = selectedInvoice != null ? selectedInvoice.IssueDate : DateTime.Today;
            lblDueDate.Text = $"Payment Due Date (15 Days): {(selectedInvoice != null ? selectedInvoice.DueDate : baseDate.AddDays(15)):MM/dd/yyyy}";
        }

        private void GridInvoices_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridInvoices.SelectedRows.Count > 0 && gridInvoices.SelectedRows[0].DataBoundItem is Invoice inv)
            {
                isBindingForm = true;
                selectedInvoice = inv;

                int dIdx = completedDeliveries.FindIndex(d => d.Id == selectedInvoice.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;

                if (!string.IsNullOrEmpty(selectedInvoice.LoadingScheduleNumber))
                {
                    txtLoadingSchedule.Text = $"Schedule #{selectedInvoice.LoadingScheduleNumber} | Dispatch #{selectedInvoice.DispatchNumber}";
                }
                else
                {
                    var allDel = MJdatabase.GetDeliveries();
                    var del = allDel.FirstOrDefault(d => d.Id == selectedInvoice.DeliveryId);
                    if (del != null)
                    {
                        var schedules = MJdatabase.GetLoadingSchedules();
                        var ls = schedules.FirstOrDefault(s => s.Id == del.LoadingScheduleId);
                        if (ls != null)
                        {
                            txtLoadingSchedule.Text = $"{ls.ScheduleNumber} | {ls.TotalCrates:N0} Crates ({ls.GCrate}G + {ls.ECrate}E) | {ls.Destination}";
                        }
                        else
                        {
                            txtLoadingSchedule.Text = !string.IsNullOrEmpty(del.LoadingScheduleInfo) ? del.LoadingScheduleInfo : "(No Loading Schedule)";
                        }
                    }
                    else
                    {
                        txtLoadingSchedule.Text = "(Delivery Reference Recorded)";
                    }
                }

                cbClient.Text = selectedInvoice.ClientName;
                numTotalAmount.Value = selectedInvoice.TotalAmount;
                numDeductions.Value = selectedInvoice.Deductions;
                RecalculateNet(sender, e);
                lblDueDate.Text = $"Payment Due Date (15 Days): {selectedInvoice.DueDate:MM/dd/yyyy}";
                lblFormHeader.Text = "Invoice Billing Form";
                isBindingForm = false;
            }
        }

        private void LoadData()
        {
            try
            {
                allInvoices = MJdatabase.GetInvoices();
                var invoicedDeliveryIds = new HashSet<int>(allInvoices.Select(inv => inv.DeliveryId));

                var clients = MJdatabase.GetLookupItems("Client Company");
                var allSchedules = MJdatabase.GetLoadingSchedules();
                var clientSet = new HashSet<string>(clients, StringComparer.OrdinalIgnoreCase);
                foreach (var s in allSchedules)
                {
                    if (!string.IsNullOrWhiteSpace(s.ClientName)) clientSet.Add(s.ClientName.Trim());
                }

                cbClient.Items.Clear();
                foreach (var c in clientSet.OrderBy(x => x))
                {
                    cbClient.Items.Add(c);
                }

                completedDeliveries = MJdatabase.GetDeliveries().FindAll(d => d.Status == "Completed" && !invoicedDeliveryIds.Contains(d.Id));
                var schedulesList = MJdatabase.GetLoadingSchedules();
                var schedDict = schedulesList.ToDictionary(s => s.Id, s => s);

                cbDelivery.Items.Clear();
                foreach (var d in completedDeliveries)
                {
                    string sNum = schedDict.TryGetValue(d.LoadingScheduleId, out var s) ? s.ScheduleNumber : "";
                    if (!string.IsNullOrEmpty(sNum))
                    {
                        cbDelivery.Items.Add($"{d.DispatchNumber} • {sNum} - {d.LoadingScheduleInfo}");
                    }
                    else
                    {
                        cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo}");
                    }
                }
                if (cbDelivery.Items.Count > 0)
                {
                    cbDelivery.SelectedIndex = 0;
                }
                else
                {
                    cbDelivery.Text = "(All completed dispatches are invoiced)";
                    txtLoadingSchedule.Text = "";
                }

                FilterGridData();

                int unpaid = allInvoices.Count(i => i.Status == "Unpaid");
                int partial = allInvoices.Count(i => i.Status == "Partial");
                int overdue = allInvoices.Count(i => i.Status == "Overdue");
                int paid = allInvoices.Count(i => i.Status == "Paid");
                decimal totalNet = allInvoices.Sum(i => i.NetAmount);

                lblSummary.Text = $"📊 Total Invoices: {allInvoices.Count:N0}  |  ⏳ Unpaid: {unpaid}  |  🟡 Partial: {partial}  |  🚨 Overdue: {overdue}  |  🟢 Paid: {paid}  |  💰 Total Billed: PHP {totalNet:N2}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BillingView] LoadData: {ex}");
                MessageBox.Show($"Failed to load invoice billing records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbDelivery.SelectedIndex < 0 || completedDeliveries.Count <= cbDelivery.SelectedIndex)
                {
                    MessageBox.Show("Please select a completed trip to invoice.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = completedDeliveries[cbDelivery.SelectedIndex];
                var existingInvoices = MJdatabase.GetInvoices();
                var duplicateInvoice = existingInvoices.Find(i => (selectedInvoice == null || i.Id != selectedInvoice.Id) && i.DeliveryId == selDel.Id);
                if (duplicateInvoice != null)
                {
                    MessageBox.Show($"DUPLICATE INVOICE BLOCKED:\nDelivery #{selDel.DispatchNumber} has already been invoiced under Invoice #{duplicateInvoice.InvoiceNumber} (Date: {duplicateInvoice.IssueDate:MMM dd, yyyy}). Duplicate invoices for the same delivery trip are strictly prohibited.", "Duplicate Invoice Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                decimal total = numTotalAmount.Value;
                decimal ded = numDeductions.Value;

                if (total <= 0)
                {
                    MessageBox.Show("Invoice gross amount must be greater than PHP 0.00. Please verify the crate count or rate matrix.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (ded > total)
                {
                    MessageBox.Show($"Deductions (PHP {ded:N2}) cannot exceed the gross invoice amount (PHP {total:N2}).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal net = total - ded;

                Invoice inv = new Invoice
                {
                    SoaId = 0,
                    DeliveryId = selDel.Id,
                    ClientName = cbClient.Text.Trim(),
                    IssueDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(15),
                    TotalAmount = total,
                    Deductions = ded,
                    NetAmount = net,
                    Status = "Unpaid"
                };

                MJdatabase.SaveInvoice(inv);
                MessageBox.Show("Service Invoice generated and issued with 15-day credit terms!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnSave_Click: {ex}");
                MessageBox.Show($"Failed to save invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedInvoice == null)
                {
                    MessageBox.Show("Please select an invoice to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal total = numTotalAmount.Value;
                decimal ded = numDeductions.Value;

                if (total <= 0)
                {
                    MessageBox.Show("Invoice gross amount must be greater than PHP 0.00.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (ded > total)
                {
                    MessageBox.Show($"Deductions (PHP {ded:N2}) cannot exceed the gross invoice amount (PHP {total:N2}).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal net = total - ded;
                decimal alreadyCollected = MJdatabase.GetTotalCollectedForInvoice(selectedInvoice.Id);
                if (net < alreadyCollected)
                {
                    MessageBox.Show($"The updated Net Amount (PHP {net:N2}) cannot be less than the payments already collected for this invoice (PHP {alreadyCollected:N2}).", "Invoice Update Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedInvoice.ClientName = cbClient.Text.Trim();
                selectedInvoice.TotalAmount = total;
                selectedInvoice.Deductions = ded;
                selectedInvoice.NetAmount = net;

                MJdatabase.SaveInvoice(selectedInvoice);
                MessageBox.Show("Invoice updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnUpdate_Click: {ex}");
                MessageBox.Show($"Failed to update invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedInvoice == null)
                {
                    MessageBox.Show("Please select an invoice to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Invoices", selectedInvoice.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Invoice {selectedInvoice.InvoiceNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Invoices", selectedInvoice.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Service Invoice deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Service Invoice. It may be locked or referenced by collections/statements of account.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnDelete_Click: {ex}");
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearFields();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allInvoices
                : allInvoices.Where(i =>
                    (!string.IsNullOrEmpty(i.InvoiceNumber) && i.InvoiceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.LoadingScheduleNumber) && i.LoadingScheduleNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.DispatchNumber) && i.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.ClientName) && i.ClientName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Status) && i.Status.ToLower().Contains(q))
                ).ToList();

            gridInvoices.DataSource = null;
            gridInvoices.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Issued Invoices ({allInvoices.Count})"
                : $"Active Issued Invoices ({filtered.Count} of {allInvoices.Count})";

            if (filtered.Count > 0)
            {
                gridInvoices.ClearSelection();
                gridInvoices.Rows[0].Selected = true;
            }
            else
            {
                selectedInvoice = null;
                if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
                {
                    CbDelivery_SelectedIndexChanged(cbDelivery, EventArgs.Empty);
                }
                else
                {
                    isBindingForm = true;
                    cbClient.Text = "";
                    txtLoadingSchedule.Text = "";
                    numTotalAmount.Value = 0;
                    numDeductions.Value = 0;
                    lblNetAmount.Text = "Net Due Amount: PHP 0.00";
                    lblFormHeader.Text = "Invoice Billing Form";
                    isBindingForm = false;
                }
            }
        }

        private void TxtGridSearch_TextChanged(object? sender, EventArgs e)
        {
            FilterGridData();
        }

        private void BtnClearSearch_Click(object? sender, EventArgs e)
        {
            txtGridSearch.Text = "";
            txtGridSearch.Focus();
        }

        private void BtnGenerateSoa_Click(object? sender, EventArgs e)
        {
            GenerateSoaDialog dialog = new GenerateSoaDialog();
            if (dialog.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
            {
                LoadData();
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            PrintHelper.PrintGridReport("Client Billing & Accounts Receivable Statement", gridInvoices, "Official client billing invoice statement with gross totals, damage deductions, net payables, and 15-day due terms.");
        }

        private void ClearFields()
        {
            selectedInvoice = null;
            txtLoadingSchedule.Text = "";
            cbClient.Text = "";
            numTotalAmount.Value = 8500;
            numDeductions.Value = 0;
            lblTariffMatch.Visible = false;
            lblDueDate.Text = $"Payment Due Date (15 Days): {DateTime.Today.AddDays(15):MM/dd/yyyy}";
            RecalculateNet(this, EventArgs.Empty);
            if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
            {
                CbDelivery_SelectedIndexChanged(cbDelivery, EventArgs.Empty);
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Invoice>? OnRecordPaymentRequested { get; set; }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedInvoice != null) RecordDetailsDialog.ShowDetails(this, selectedInvoice);
            });
            var itemCopyInv = new ToolStripMenuItem("📋 Copy Invoice #", null, (s, e) => {
                if (selectedInvoice != null) { Clipboard.SetText(selectedInvoice.InvoiceNumber); ToastHelper.ShowToast(this, "Invoice # copied ✓"); }
            });
            var itemPay = new ToolStripMenuItem("💵 Record Payment Receipt", null, (s, e) => {
                if (selectedInvoice != null) OnRecordPaymentRequested?.Invoke(selectedInvoice);
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Invoice Details", null, (s, e) => {
                SetFormVisibility(true);
                cbDelivery.Focus();
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Official Invoice", null, BtnPrint_Click);
            var itemSoa = new ToolStripMenuItem("📑 Add / Generate Statement of Account (SOA)", null, (s, e) => {
                BtnGenerateSoa_Click(this, EventArgs.Empty);
            });
            var itemDelete = new ToolStripMenuItem("🗑️ Delete / Void Invoice", null, BtnDelete_Click);
            var itemExportCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) => {
                DataExportHelper.ExportToCsv(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable");
            });
            var itemExportExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) => {
                DataExportHelper.ExportToExcelXml(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable");
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyInv, new ToolStripSeparator(), itemEdit, itemPay, new ToolStripSeparator(), itemPrint, itemSoa, new ToolStripSeparator(), itemExportCsv, itemExportExcel, new ToolStripSeparator(), itemDelete });

            gridInvoices.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridInvoices.ClearSelection();
                    gridInvoices.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridInvoices, gridInvoices.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
