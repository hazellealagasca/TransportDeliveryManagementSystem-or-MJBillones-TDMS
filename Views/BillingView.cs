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
        private float currentFormWidth = 420F;

        public void PrefillForDelivery(Delivery del)
        {
            if (del == null) return;
            del = DispatchGate.Fresh(del);
            if (!DispatchGate.RequireCompleted(FindForm(), del, "bill it")) return;
            SetFormVisibility(true);
            int idx = completedDeliveries.FindIndex(d => d.Id == del.Id);
            if (idx >= 0 && idx < cbDelivery.Items.Count)
            {
                cbDelivery.SelectedIndex = idx;
            }
            else
            {
                completedDeliveries.Insert(0, del);
                cbDelivery.Items.Insert(0, $"{del.DispatchNumber} - {del.LoadingScheduleInfo} (Driver: {del.DriverName})");
                cbDelivery.SelectedIndex = 0;
            }
            if (string.IsNullOrWhiteSpace(txtInvoiceNumber.Text))
            {
                txtInvoiceNumber.Text = MJdatabase.GenerateNextInvoiceNumber();
            }
        }

        public BillingView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridInvoices.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
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

            txtInvoiceNumber.Text = "";
            txtInvoiceNumber.TextChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    HandleInvoiceNumberInput();
                }
            };

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 420F, 480F);
            InitializeFormToggle();
            SetButtonStates(false);
            UIThemeHelper.ApplyTheme(this);
                    InitFullPageForm();
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
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) SetFormVisibility(false); else OpenNewRecordForm(); };
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Width = 72,
                Height = 30,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }


        // ------------------------------------------------------------------ Full-page form (same tab, back button, section cards)
        private FullPageFormKit? formKit;
        private void InitFullPageForm()
        {
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to invoices");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Invoice & order", "Which dispatch this invoice bills and who it is for.", lblInvoiceNumber, txtInvoiceNumber, lblDel, cbDelivery, lblLoadingSchedule, txtLoadingSchedule, lblClient, cbClient),
                new FullPageFormKit.Section("Amounts", "Gross amount, deductions and the net amount due.", pnlClientSpacer, tblAmounts, pnlAmountsSpacer, pnlNetDue, pnlNetDueSpacer, lblTariffMatch)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New invoice";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridInvoices.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridInvoices.Rows[e.RowIndex].DataBoundItem is not Invoice) return;
                gridInvoices.ClearSelection();
                gridInvoices.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New invoice";
            SetFormVisibility(true);
        }
        private void SetFormVisibility(bool visible)
        {
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
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
            gridInvoices.ShowCellToolTips = false;
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
                    e.CellStyle.BackColor = Pal.C(220, 252, 231);
                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Partial", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(254, 243, 199);
                    e.CellStyle.ForeColor = Pal.C(180, 83, 9);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(254, 226, 226);
                    e.CellStyle.ForeColor = Pal.C(185, 28, 28);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(241, 245, 249);
                    e.CellStyle.ForeColor = Pal.C(51, 65, 85);
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
                lblTariffMatch.Text = calculatedAmount > 0 ? $"✓ {tariffDesc} -> PHP {calculatedAmount:N2}" : tariffDesc;
                lblTariffMatch.Visible = true;

                if (calculatedAmount > numTotalAmount.Maximum) numTotalAmount.Maximum = calculatedAmount * 2;
                numTotalAmount.Value = Math.Max(numTotalAmount.Minimum, Math.Min(numTotalAmount.Maximum, calculatedAmount));

                var incidents = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == del.Id && !ir.ChargeableToCrew && ir.Status != "Rejected");
                decimal totalDeductions = incidents.Sum(ir => ir.EstimatedCost);
                if (totalDeductions > numDeductions.Maximum) numDeductions.Maximum = totalDeductions * 2;
                numDeductions.Value = Math.Max(numDeductions.Minimum, Math.Min(numDeductions.Maximum, totalDeductions));

                if (incidents.Count > 0)
                {
                    string incNote = string.Join("; ", incidents.Select(i => $"{i.Cause} ({i.DamagedQuantity} damaged crates - PHP {i.EstimatedCost:N2})"));
                    lblTariffMatch.Text = $"{(calculatedAmount > 0 ? "✓ " : "")}{tariffDesc} (Less incident damage PHP {totalDeductions:N2}) -> PHP {Math.Max(0, calculatedAmount - totalDeductions):N2}";
                }

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
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridInvoices.SelectedRows.Count > 0 && gridInvoices.SelectedRows[0].DataBoundItem is Invoice inv)
            {
                BindInvoiceToForm(inv);
            }
        }

        private void HandleInvoiceNumberInput()
        {
            string invNum = txtInvoiceNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(invNum))
            {
                selectedInvoice = null;
                lblFormHeader.Text = "Generate Service Invoice";
                SetButtonStates(false);
                return;
            }

            var matched = allInvoices.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                BindInvoiceToForm(matched);
                for (int i = 0; i < gridInvoices.Rows.Count; i++)
                {
                    if (gridInvoices.Rows[i].DataBoundItem is Invoice rowInv && rowInv.Id == matched.Id)
                    {
                        gridInvoices.ClearSelection();
                        gridInvoices.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedInvoice = null;
                lblFormHeader.Text = $"Invoice #{invNum} (New)";
                SetButtonStates(false);
            }
        }

        private void BindInvoiceToForm(Invoice inv)
        {
            isBindingForm = true;
            try
            {
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

                txtInvoiceNumber.Text = selectedInvoice.InvoiceNumber;
                cbClient.Text = selectedInvoice.ClientName;
                numTotalAmount.Value = selectedInvoice.TotalAmount;
                numDeductions.Value = selectedInvoice.Deductions;
                RecalculateNet(this, EventArgs.Empty);
                lblDueDate.Text = $"Payment Due Date (15 Days): {selectedInvoice.DueDate:MM/dd/yyyy}";
                lblFormHeader.Text = $"Invoice #{selectedInvoice.InvoiceNumber}";
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
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
                    cbDelivery.SelectedIndex = -1; cbDelivery.Text = "";
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
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (cbDelivery.SelectedIndex < 0 || completedDeliveries.Count <= cbDelivery.SelectedIndex)
                {
                    ValidationHelper.ApplyValidationState(cbDelivery, false, "Completed delivery selection is required.");
                    valErrors.Add("Completed delivery selection is required.");
                    firstInvalid ??= cbDelivery;
                }
                if (!ValidationHelper.ValidateRequiredText(txtInvoiceNumber, "Invoice Number", valErrors)) firstInvalid ??= txtInvoiceNumber;
                if (!ValidationHelper.ValidateRequiredCombo(cbClient, "Client Company", valErrors)) firstInvalid ??= cbClient;
                if (!ValidationHelper.ValidatePositiveNumber(numTotalAmount, "Total Gross Amount", valErrors)) firstInvalid ??= numTotalAmount;
                if (numDeductions.Value > numTotalAmount.Value)
                {
                    ValidationHelper.ApplyValidationState(numDeductions, false, "Deductions cannot exceed Gross Amount.");
                    valErrors.Add("Deductions cannot exceed the gross invoice amount.");
                    firstInvalid ??= numDeductions;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (cbDelivery.SelectedIndex < 0 || completedDeliveries.Count <= cbDelivery.SelectedIndex)
                {
                    MessageBox.Show("Please select a completed trip to invoice.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = completedDeliveries[cbDelivery.SelectedIndex];
                if (!DispatchGate.RequireCompleted(this, DispatchGate.Fresh(selDel), "bill it")) return;
                var existingInvoices = MJdatabase.GetInvoices();
                var duplicateInvoice = existingInvoices.Find(i => (selectedInvoice == null || i.Id != selectedInvoice.Id) && i.DeliveryId == selDel.Id);
                if (duplicateInvoice != null)
                {
                    MessageBox.Show($"DUPLICATE INVOICE BLOCKED:\nDelivery #{selDel.DispatchNumber} has already been invoiced under Invoice #{duplicateInvoice.InvoiceNumber} (Date: {duplicateInvoice.IssueDate:MMM dd, yyyy}). Duplicate invoices for the same delivery trip are strictly prohibited.", "Duplicate Invoice Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                string invNum = txtInvoiceNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(invNum))
                {
                    invNum = MJdatabase.GenerateNextInvoiceNumber();
                    txtInvoiceNumber.Text = invNum;
                }

                var duplicateNum = existingInvoices.Find(i => (selectedInvoice == null || i.Id != selectedInvoice.Id) &&
                    !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase));
                if (duplicateNum != null)
                {
                    MessageBox.Show($"Invoice Reference Number '{invNum}' is already in use by another invoice (Client: {duplicateNum.ClientName}). Please provide a unique reference number.", "Duplicate Reference Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
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
                    InvoiceNumber = invNum,
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

                string invNum = txtInvoiceNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(invNum))
                {
                    MessageBox.Show("Please enter an Invoice Reference Number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
                    return;
                }

                var existingInvoices = MJdatabase.GetInvoices();
                var duplicateNum = existingInvoices.Find(i => i.Id != selectedInvoice.Id &&
                    !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase));
                if (duplicateNum != null)
                {
                    MessageBox.Show($"Invoice Reference Number '{invNum}' is already in use by another invoice (Client: {duplicateNum.ClientName}). Please provide a unique reference number.", "Duplicate Reference Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cbClient.Text))
                {
                    MessageBox.Show("Please pick the client this invoice is for.", "Client Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbClient.Focus();
                    return;
                }
                selectedInvoice.InvoiceNumber = invNum;

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

                if (string.Equals(selectedInvoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                {
                    // A draft (e.g. from a completed special trip) becomes a real invoice once it has an amount.
                    selectedInvoice.Status = "Unpaid";
                }
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

                var res = MessageBox.Show($"Are you sure you want to delete Invoice {selectedInvoice.InvoiceNumber}?\n\n" + MJdatabase.DeletePolicyNote("Invoices"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("Invoices", selectedInvoice.Id);
                    if (deleted)
                    {
                        MessageBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            gridInvoices.ClearSelection();
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
                    lblFormHeader.Text = "Generate Service Invoice";
                    isBindingForm = false;
                }
                SetButtonStates(false);
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
            txtInvoiceNumber.Text = "";
            lblFormHeader.Text = "Generate Service Invoice";
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
            SetButtonStates(false);
        }

        private void SetButtonStates(bool isEditing)
        {
            string invNum = txtInvoiceNumber.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(invNum)
                ? allInvoices.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase))
                : null;
            bool exists = (selectedInvoice != null || matched != null) && !string.IsNullOrWhiteSpace(invNum);

            btnSave.Enabled = !exists;
            btnSave.Visible = !exists;
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            ApplyButtonVisualState(btnSave, !exists, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, exists, Pal.C(25, 135, 84), Pal.White);
        }

        private void ApplyButtonVisualState(Button btn, bool enabled, Color enabledBack, Color enabledFore)
        {
            if (enabled)
            {
                btn.BackColor = enabledBack;
                btn.ForeColor = enabledFore;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Pal.C(226, 232, 240);
                btn.ForeColor = Pal.C(148, 163, 184);
                btn.Cursor = Cursors.Default;
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Invoice>? OnRecordPaymentRequested { get; set; }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridInvoices, (m, rows) =>
            {
                var invs = rows.Select(r => r.DataBoundItem).OfType<Invoice>().ToList();
                if (invs.Count == 0) return;
                if (invs.Count > 1) { BuildMultiInvoiceMenu(m, invs); return; }
                var inv = invs[0];
                selectedInvoice = inv;
                decimal paid = 0;
                try { paid = MJdatabase.GetTotalCollectedForInvoice(inv.Id); } catch { }
                decimal balance = Math.Max(0, inv.NetAmount - paid);
                bool isPaid = RowMenu.Is(inv.Status, "Paid") || (inv.NetAmount > 0 && balance <= 0);
                bool isDraft = RowMenu.Is(inv.Status, "Draft");
                m.Items.AddRange(RowMenu.Header($"🧾  Invoice {inv.InvoiceNumber}",
                    $"{RowMenu.Short(inv.ClientName, 28)} · {RowMenu.Money(inv.NetAmount)} · {inv.Status}{(isPaid || isDraft ? "" : $" · balance {RowMenu.Money(balance)}")} · due {inv.DueDate:MMM d}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, inv)));
                m.Items.Add(RowMenu.Item(isDraft ? "Complete this draft invoice" : "Edit invoice", () => { SetFormVisibility(true); cbDelivery.Focus(); }));
                if (!isPaid && !isDraft) m.Items.Add(RowMenu.Good($"Record payment ({RowMenu.Money(balance)} due)", () => OnRecordPaymentRequested?.Invoke(inv)));
                if (paid > 0) m.Items.Add(RowMenu.Item($"Show payments ({RowMenu.Money(paid)} received)", () => FinanceHubView.Of(this)?.ShowCollections(inv.InvoiceNumber)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Item($"Open dispatch {inv.DispatchNumber}", () => RowMenu.OpenDispatch(this, inv.DeliveryId, inv.DispatchNumber), "", inv.DeliveryId > 0 || !string.IsNullOrWhiteSpace(inv.DispatchNumber)));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(inv.ClientName), "", !string.IsNullOrWhiteSpace(inv.ClientName)));
                m.Items.Add(RowMenu.Item("Generate statement of account…", () => BtnGenerateSoa_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("End-to-end delivery dossier", () => PrintHelper.PrintEndToEndDeliveryDossier(inv.DeliveryId, inv.DispatchNumber), "", inv.DeliveryId > 0),
                    RowMenu.Item("Selected invoices (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Client Billing Invoices", gridInvoices, "Selected service invoices.")),
                    RowMenu.Item("Whole invoice register", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Invoice number", inv.InvoiceNumber), ("Dispatch number", inv.DispatchNumber), ("Client", inv.ClientName),
                    ("Amount due", balance.ToString("N2")),
                    ("Payment reminder", $"Invoice {inv.InvoiceNumber} for {inv.ClientName}: {RowMenu.Money(inv.NetAmount)}, balance {RowMenu.Money(balance)}, due {inv.DueDate:MMMM d, yyyy}.")));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other invoices for this client", () => RowMenu.Search(txtGridSearch, inv.ClientName)),
                    RowMenu.Item("Statements of account for this client", () => FinanceHubView.Of(this)?.ShowStatements(inv.ClientName))));
                m.Items.Add(RowMenu.ExportSub(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger(paid > 0 ? "Delete invoice… (has payments)" : "Delete / void invoice…", () => { selectedInvoice = inv; BtnDelete_Click(this, EventArgs.Empty); }));
            }, allowMultiSelect: true);
        }
        private void BuildMultiInvoiceMenu(ContextMenuStrip m, List<Invoice> invs)
        {
            decimal total = invs.Sum(i => i.NetAmount);
            m.Items.AddRange(RowMenu.Header($"🧾  {invs.Count} invoices", $"{RowMenu.Money(total)} in total · {invs.Select(i => i.ClientName).Distinct().Count()} client(s)"));
            m.Items.Add(RowMenu.Item("Print the selected invoices (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Client Billing Invoices", gridInvoices, "Selected service invoices.")));
            m.Items.Add(RowMenu.Item("Copy invoice numbers", () => RowMenu.Copy(this, string.Join(", ", invs.Select(i => i.InvoiceNumber)), $"{invs.Count} invoice numbers copied")));
            m.Items.Add(RowMenu.Item("Copy as a list with amounts", () => RowMenu.Copy(this, string.Join(Environment.NewLine, invs.Select(i => $"{i.InvoiceNumber}\t{i.ClientName}\t{i.NetAmount:N2}\t{i.Status}")), "List copied")));
            m.Items.Add(RowMenu.Sep());
            m.Items.Add(RowMenu.ExportSub(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable", () => BtnPrint_Click(this, EventArgs.Empty)));
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
    }
}
