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
    public partial class CollectionView : UserControl, IRefreshable
    {
        private CollectionReceipt? selectedCollection = null;
        private List<Invoice> unpaidInvoices = new List<Invoice>();
        private List<CollectionReceipt> allReceipts = new List<CollectionReceipt>();
        private string currentSortColumn = "ReceiptNumber";
        private bool isSortAscending = true;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private float currentFormWidth = 400F;

        public CollectionView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridCollections.BringToFront();
            SetupGridColumns();
            InitListChips();
            SetupContextMenu();
            gridCollections.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridCollections.Columns.Count) return;
                string col = gridCollections.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            LoadData();

            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Collections to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridCollections, "Collection_Receipts", "Collections and Payment Receipts"));
            menuActions.Items.Add("🖨️ Print Collections Register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            txtRefNo.TextChanged += (s, e) => HandleRefNoInput();

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F);
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
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to receipts");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Invoice being paid", "Pick the invoice this payment settles.", lblInv, cbInvoice, pnlLinkedInvoice),
                new FullPageFormKit.Section("Payment", "Amount, date, method and reference of the payment.", tblPaymentRow, lblRef, txtRefNo, lblNotes, txtNotes)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New receipt";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridCollections.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridCollections.Rows[e.RowIndex].DataBoundItem is not CollectionReceipt) return;
                gridCollections.ClearSelection();
                gridCollections.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearFields();
            lblFormHeader.Text = "New payment receipt";
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
            gridCollections.AutoGenerateColumns = false;
            gridCollections.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCollections.ScrollBars = ScrollBars.Both;
            gridCollections.AllowUserToResizeColumns = true;
            gridCollections.ShowCellToolTips = false;
            gridCollections.Columns.Clear();
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReceiptNumber", HeaderText = "Receipt #", FillWeight = 13, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNumber", HeaderText = "Invoice #", FillWeight = 13, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 11, MinimumWidth = 85, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AmountPaid", HeaderText = "Amount Paid", DefaultCellStyle = { Format = "N2" }, FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentMethod", HeaderText = "Method", FillWeight = 12, MinimumWidth = 85, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReferenceNumber", HeaderText = "Reference #", FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "Notes", FillWeight = 23, MinimumWidth = 120, ReadOnly = true });
        }

        private void CbInvoice_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbInvoice.SelectedIndex >= 0 && cbInvoice.SelectedIndex < unpaidInvoices.Count)
            {
                var inv = unpaidInvoices[cbInvoice.SelectedIndex];
                decimal collected = MJdatabase.GetTotalCollectedForInvoice(inv.Id);
                decimal remaining = Math.Max(0, inv.NetAmount - collected);
                lblLinkedInvoiceInfo.Text = $"📄 Invoice: {inv.InvoiceNumber} | {inv.ClientName} | Balance: PHP {remaining:N2}";
                var _amt = remaining > 0 ? remaining : inv.NetAmount;
                if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2;
                numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
            }
            else
            {
                lblLinkedInvoiceInfo.Text = "📄 Linked Invoice: (None Selected)";
            }
        }

        private void GridCollections_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridCollections.SelectedRows.Count > 0 && gridCollections.SelectedRows[0].DataBoundItem is CollectionReceipt cr)
            {
                BindCollectionToForm(cr);
            }
        }

        private void HandleRefNoInput()
        {
            string refNum = txtRefNo.Text.Trim();
            if (string.IsNullOrWhiteSpace(refNum))
            {
                selectedCollection = null;
                lblFormHeader.Text = "Record Payment Receipt";
                SetButtonStates(false);
                return;
            }

            var matched = allReceipts.FirstOrDefault(r => (!string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)) ||
                                                          (!string.IsNullOrWhiteSpace(r.ReceiptNumber) && r.ReceiptNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)));
            if (matched != null)
            {
                BindCollectionToForm(matched);
                for (int i = 0; i < gridCollections.Rows.Count; i++)
                {
                    if (gridCollections.Rows[i].DataBoundItem is CollectionReceipt rowCr && rowCr.Id == matched.Id)
                    {
                        gridCollections.ClearSelection();
                        gridCollections.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedCollection = null;
                lblFormHeader.Text = $"Receipt Ref: {refNum} (New)";
                SetButtonStates(false);
            }
        }

        private void BindCollectionToForm(CollectionReceipt cr)
        {
            selectedCollection = cr;
            lblFormHeader.Text = $"Receipt #{selectedCollection.ReceiptNumber}";
            lblLinkedInvoiceInfo.Text = !string.IsNullOrEmpty(selectedCollection.InvoiceNumber)
                ? $"📄 Linked Invoice: {selectedCollection.InvoiceNumber}"
                : "📄 Linked Invoice: (None)";
            var _amt = selectedCollection.AmountPaid;
            if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2;
            numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
            cbPaymentMethod.Text = selectedCollection.PaymentMethod;
            txtRefNo.Text = selectedCollection.ReferenceNumber;
            txtNotes.Text = selectedCollection.Notes;
            SetButtonStates(true);
        }

        private void LoadData()
        {
            try
            {
                var methods = MJdatabase.GetLookupItems("Payment Method");
                cbPaymentMethod.Items.Clear();
                foreach (var m in methods) cbPaymentMethod.Items.Add(m);

                unpaidInvoices = MJdatabase.GetInvoices().FindAll(i => i.Status == "Unpaid" || i.Status == "Overdue" || i.Status == "Partial");
                cbInvoice.Items.Clear();
                foreach (var inv in unpaidInvoices)
                {
                    decimal collected = MJdatabase.GetTotalCollectedForInvoice(inv.Id);
                    decimal remaining = Math.Max(0, inv.NetAmount - collected);
                    cbInvoice.Items.Add($"{inv.InvoiceNumber} - {inv.ClientName} (Balance: PHP {remaining:N2} / Total: PHP {inv.NetAmount:N2})");
                }
                cbInvoice.SelectedIndex = -1; cbInvoice.Text = "";

                allReceipts = MJdatabase.GetCollectionReceipts();
                FilterGridData();
                var receipts = allReceipts;

                decimal totalPaid = receipts.Sum(c => c.AmountPaid);
                lblSummary.Text = $"📊 Total Collections: {receipts.Count:N0}  |  💵 Total Settled Payments: PHP {totalPaid:N2}";
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Failed to load collection records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (cbInvoice.SelectedIndex < 0)
                {
                    ValidationHelper.ApplyValidationState(cbInvoice, false, "Invoice selection is required.");
                    valErrors.Add("Invoice selection is required.");
                    firstInvalid ??= cbInvoice;
                }
                if (!ValidationHelper.ValidatePositiveNumber(numAmountPaid, "Payment Amount", valErrors)) firstInvalid ??= numAmountPaid;
                if (!ValidationHelper.ValidateRequiredCombo(cbPaymentMethod, "Payment Method", valErrors)) firstInvalid ??= cbPaymentMethod;

                string curMethod = cbPaymentMethod.Text.Trim();
                if ((curMethod.IndexOf("Check", StringComparison.OrdinalIgnoreCase) >= 0 || curMethod.IndexOf("Transfer", StringComparison.OrdinalIgnoreCase) >= 0) && string.IsNullOrWhiteSpace(txtRefNo.Text))
                {
                    ValidationHelper.ApplyValidationState(txtRefNo, false, "Reference Number / Check # is required for non-cash payment.");
                    valErrors.Add("Reference Number / Check # is required for " + curMethod + ".");
                    firstInvalid ??= txtRefNo;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (cbInvoice.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select an invoice.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (numAmountPaid.Value <= 0)
                {
                    MessageBox.Show("Payment amount must be greater than PHP 0.00.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selInv = unpaidInvoices[cbInvoice.SelectedIndex];
                decimal collected = MJdatabase.GetTotalCollectedForInvoice(selInv.Id);
                decimal remainingBalance = Math.Max(0, selInv.NetAmount - collected);

                if (numAmountPaid.Value > remainingBalance)
                {
                    var confirmOverpay = MessageBox.Show($"The entered payment (PHP {numAmountPaid.Value:N2}) exceeds the outstanding remaining balance (PHP {remainingBalance:N2}).\n\nWould you like to adjust the payment to the exact remaining balance of PHP {remainingBalance:N2}?", "Payment Exceeds Remaining Balance", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirmOverpay == DialogResult.Yes)
                    {
                        var _amt = remainingBalance; if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2; numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
                    }
                    else
                    {
                        return;
                    }
                }

                string pmethod = cbPaymentMethod.Text.Trim();
                if (string.IsNullOrWhiteSpace(pmethod))
                {
                    MessageBox.Show("Payment Method is mandatory. Please select or enter a payment method (e.g. Cash, Check, Bank Transfer, GCash).", "Payment Method Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbPaymentMethod.Focus();
                    return;
                }

                string refNum = txtRefNo.Text.Trim();
                if (!string.IsNullOrWhiteSpace(refNum))
                {
                    var existingReceipts = MJdatabase.GetCollectionReceipts();
                    var duplicateReceipt = existingReceipts.Find(r => (selectedCollection == null || r.Id != selectedCollection.Id) && !string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase));
                    if (duplicateReceipt != null)
                    {
                        var confirm = MessageBox.Show($"Payment Reference Number '{refNum}' was already recorded in Receipt #{duplicateReceipt.ReceiptNumber} on {duplicateReceipt.PaymentDate:MM/dd/yyyy}.\n\nDo you want to proceed and record this duplicate payment reference?", "Duplicate Reference Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm != DialogResult.Yes)
                        {
                            txtRefNo.Focus();
                            return;
                        }
                    }
                }

                MJdatabase.SaveLookupItem("Payment Method", pmethod);

                CollectionReceipt cr = new CollectionReceipt
                {
                    InvoiceId = selInv.Id,
                    InvoiceNumber = selInv.InvoiceNumber,
                    PaymentDate = DateTime.Now,
                    AmountPaid = numAmountPaid.Value,
                    PaymentMethod = pmethod,
                    ReferenceNumber = txtRefNo.Text.Trim(),
                    Notes = txtNotes.Text.Trim()
                };

                MJdatabase.SaveCollectionReceipt(cr);
                QuietBox.Show("Collection Receipt issued successfully and Invoice marked as PAID!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Failed to record collection receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedCollection == null)
                {
                    MessageBox.Show("Please select a collection receipt to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (numAmountPaid.Value <= 0)
                {
                    MessageBox.Show("Payment amount must be greater than PHP 0.00.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var allInvoices = MJdatabase.GetInvoices();
                var matchedInv = allInvoices.Find(i => i.Id == selectedCollection.InvoiceId);
                if (matchedInv != null)
                {
                    decimal otherCollected = MJdatabase.GetTotalCollectedForInvoice(selectedCollection.InvoiceId) - selectedCollection.AmountPaid;
                    decimal maxAllowed = Math.Max(0, matchedInv.NetAmount - otherCollected);
                    if (numAmountPaid.Value > maxAllowed)
                    {
                        var confirmOverpay = MessageBox.Show($"The updated payment (PHP {numAmountPaid.Value:N2}) exceeds the outstanding balance for Invoice #{matchedInv.InvoiceNumber} (PHP {maxAllowed:N2}).\n\nWould you like to adjust the payment to the exact balance of PHP {maxAllowed:N2}?", "Payment Exceeds Balance", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirmOverpay == DialogResult.Yes)
                        {
                            var _amt = maxAllowed; if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2; numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                string pmethod = cbPaymentMethod.Text.Trim();
                if (string.IsNullOrWhiteSpace(pmethod))
                {
                    MessageBox.Show("Payment Method is mandatory. Please select or enter a payment method (e.g. Cash, Check, Bank Transfer, GCash).", "Payment Method Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbPaymentMethod.Focus();
                    return;
                }

                string refNum = txtRefNo.Text.Trim();
                if (!string.IsNullOrWhiteSpace(refNum))
                {
                    var existingReceipts = MJdatabase.GetCollectionReceipts();
                    var duplicateReceipt = existingReceipts.Find(r => (selectedCollection == null || r.Id != selectedCollection.Id) && !string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase));
                    if (duplicateReceipt != null)
                    {
                        var confirm = MessageBox.Show($"Payment Reference Number '{refNum}' was already recorded in Receipt #{duplicateReceipt.ReceiptNumber} on {duplicateReceipt.PaymentDate:MM/dd/yyyy}.\n\nDo you want to proceed and record this duplicate payment reference?", "Duplicate Reference Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm != DialogResult.Yes)
                        {
                            txtRefNo.Focus();
                            return;
                        }
                    }
                }

                if ((pmethod.IndexOf("Check", StringComparison.OrdinalIgnoreCase) >= 0 || pmethod.IndexOf("Transfer", StringComparison.OrdinalIgnoreCase) >= 0) && string.IsNullOrWhiteSpace(refNum))
                {
                    MessageBox.Show($"A reference / check number is required for {pmethod} payments.", "Reference Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRefNo.Focus();
                    return;
                }
                MJdatabase.SaveLookupItem("Payment Method", pmethod);

                selectedCollection.AmountPaid = numAmountPaid.Value;
                selectedCollection.PaymentMethod = pmethod;
                selectedCollection.ReferenceNumber = txtRefNo.Text.Trim();
                selectedCollection.Notes = txtNotes.Text.Trim();

                MJdatabase.SaveCollectionReceipt(selectedCollection);
                QuietBox.Show("Collection Receipt updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Failed to update collection receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedCollection == null)
                {
                    MessageBox.Show("Please select a collection receipt to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Collection Receipt {selectedCollection.ReceiptNumber}?\n\n" + MJdatabase.DeletePolicyNote("CollectionReceipts"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("CollectionReceipts", selectedCollection.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Collection Receipt. It may be locked or referenced by other financial records.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            gridCollections.ClearSelection();
            ClearFields();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allReceipts
                : allReceipts.Where(r =>
                    (!string.IsNullOrEmpty(r.ReceiptNumber) && r.ReceiptNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.InvoiceNumber) && r.InvoiceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.PaymentMethod) && r.PaymentMethod.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.ReferenceNumber) && r.ReferenceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.Notes) && r.Notes.ToLower().Contains(q))
                ).ToList();

            filtered = ApplyListChips(filtered);
            gridCollections.DataSource = null;
            gridCollections.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q) && filtered.Count == allReceipts.Count
                ? $"Payment Collections Register Log ({allReceipts.Count})"
                : $"Payment Collections Register Log - Showing {filtered.Count} of {allReceipts.Count}";

            if (filtered.Count > 0)
            {
                gridCollections.ClearSelection();
                gridCollections.Rows[0].Selected = true;
            }
            else
            {
                ClearFields();
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

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            PrintHelper.PrintGridReport("Official Collection Receipts Log Statement", gridCollections, "Official collection receipt log including amounts paid, payment channels, and bank/check references.");
        }

        private void BtnPrintSlip_Click(object? sender, EventArgs e)
        {
            if (selectedCollection == null)
            {
                QuietBox.Show("Please select a collection receipt from the list first, then click Print Slip.", "Select a Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintOfficialCollectionReceipt(selectedCollection);
        }

        private void ClearFields()
        {
            selectedCollection = null;
            lblFormHeader.Text = "Record Payment Receipt";
            lblLinkedInvoiceInfo.Text = "📄 Linked Invoice: (None Selected)";
            cbPaymentMethod.Text = "Cash";
            txtRefNo.Text = "";
            txtNotes.Text = "";
            if (cbInvoice.SelectedIndex >= 0)
            {
                CbInvoice_SelectedIndexChanged(cbInvoice, EventArgs.Empty);
            }
            SetButtonStates(false);
        }

        private void SetButtonStates(bool isEditing)
        {
            string refNum = txtRefNo.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(refNum)
                ? allReceipts.FirstOrDefault(r => (!string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)) ||
                                                  (!string.IsNullOrWhiteSpace(r.ReceiptNumber) && r.ReceiptNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)))
                : null;
            bool exists = (selectedCollection != null || matched != null) && !string.IsNullOrWhiteSpace(refNum);

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

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridCollections, (m, rows) =>
            {
                var crs = rows.Select(r => r.DataBoundItem).OfType<CollectionReceipt>().ToList();
                if (crs.Count == 0) return;
                if (crs.Count > 1)
                {
                    m.Items.AddRange(RowMenu.Header($"💵  {crs.Count} receipts", $"{RowMenu.Money(crs.Sum(c => c.AmountPaid))} collected"));
                    m.Items.Add(RowMenu.Item("Print the selected receipts (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Collection Receipts", gridCollections, "Selected collection receipts.")));
                    m.Items.Add(RowMenu.Item("Copy receipt numbers", () => RowMenu.Copy(this, string.Join(", ", crs.Select(c => c.ReceiptNumber)), "Receipt numbers copied")));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.ExportSub(gridCollections, "Collection_Receipts", "Collection Receipts", () => BtnPrint_Click(this, EventArgs.Empty)));
                    return;
                }
                var cr = crs[0];
                selectedCollection = cr;
                m.Items.AddRange(RowMenu.Header($"💵  Receipt {cr.ReceiptNumber}",
                    $"{RowMenu.Money(cr.AmountPaid)} · {cr.PaymentMethod}{(string.IsNullOrWhiteSpace(cr.ReferenceNumber) ? "" : " #" + cr.ReferenceNumber)} · {cr.PaymentDate:MMM d, yyyy} · invoice {cr.InvoiceNumber}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, cr)));
                m.Items.Add(RowMenu.Item("Edit receipt", () => { SetFormVisibility(true); numAmountPaid.Focus(); }));
                m.Items.Add(RowMenu.Good("Print official receipt (OR)", () => PrintHelper.PrintOfficialCollectionReceipt(cr)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Item($"Show invoice {cr.InvoiceNumber}", () => FinanceHubView.Of(this)?.ShowInvoices(cr.InvoiceNumber), "", !string.IsNullOrWhiteSpace(cr.InvoiceNumber)));
                m.Items.Add(RowMenu.Item("Other payments for this invoice", () => RowMenu.Search(txtGridSearch, cr.InvoiceNumber), "", !string.IsNullOrWhiteSpace(cr.InvoiceNumber)));
                m.Items.Add(RowMenu.CopySub(this, ("Receipt number", cr.ReceiptNumber), ("Invoice number", cr.InvoiceNumber), ("Reference / check #", cr.ReferenceNumber),
                    ("Amount", cr.AmountPaid.ToString("N2")),
                    ("Summary", $"OR {cr.ReceiptNumber}: {RowMenu.Money(cr.AmountPaid)} by {cr.PaymentMethod} on {cr.PaymentDate:MMMM d, yyyy} for invoice {cr.InvoiceNumber}")));
                m.Items.Add(RowMenu.ExportSub(gridCollections, "Collection_Receipts", "Collection Receipts", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Void / delete receipt…", () => { selectedCollection = cr; BtnDelete_Click(this, EventArgs.Empty); }));
            }, allowMultiSelect: true);
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
        /// <summary>Opens a new receipt with the invoice already picked (from Billing → Record payment).</summary>
        public void PrefillForInvoice(Invoice inv)
        {
            LoadData();
            int idx = unpaidInvoices.FindIndex(i => i.Id == inv.Id);
            if (idx < 0)
            {
                QuietBox.Show($"Invoice {inv.InvoiceNumber} has no balance left to collect (status: {inv.Status}).", "Nothing to collect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenNewRecordForm();
            cbInvoice.SelectedIndex = idx;
            numAmountPaid.Focus();
        }
    }
}
