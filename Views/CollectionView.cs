using System;
using System.Collections.Generic;
using System.Drawing;
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

        public CollectionView()
        {
            InitializeComponent();
            gridCollections.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridCollections.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridCollections.Rows[e.RowIndex].DataBoundItem is CollectionReceipt item) RecordDetailsDialog.ShowDetails(this, item); };
            gridCollections.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridCollections.Columns.Count) return;
                string col = gridCollections.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            LoadData();
            UIThemeHelper.ApplyTheme(this);
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
            gridCollections.ShowCellToolTips = true;
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
                var _amt = remaining > 0 ? remaining : inv.NetAmount; if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2; numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
            }
        }

        private void GridCollections_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridCollections.SelectedRows.Count > 0 && gridCollections.SelectedRows[0].DataBoundItem is CollectionReceipt cr)
            {
                selectedCollection = cr;
                var _amt = selectedCollection.AmountPaid; if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2; numAmountPaid.Value = Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt));
                cbPaymentMethod.Text = selectedCollection.PaymentMethod;
                txtRefNo.Text = selectedCollection.ReferenceNumber;
                txtNotes.Text = selectedCollection.Notes;
            }
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
                if (cbInvoice.Items.Count > 0) cbInvoice.SelectedIndex = 0;

                allReceipts = MJdatabase.GetCollectionReceipts();
                FilterGridData();
                var receipts = allReceipts;

                decimal totalPaid = receipts.Sum(c => c.AmountPaid);
                lblSummary.Text = $"📊 Total Collections: {receipts.Count:N0}  |  💵 Total Settled Payments: PHP {totalPaid:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load collection records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
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
                MessageBox.Show("Collection Receipt issued successfully and Invoice marked as PAID!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
            }
            catch (Exception ex)
            {
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

                MJdatabase.SaveLookupItem("Payment Method", pmethod);

                selectedCollection.AmountPaid = numAmountPaid.Value;
                selectedCollection.PaymentMethod = pmethod;
                selectedCollection.ReferenceNumber = txtRefNo.Text.Trim();
                selectedCollection.Notes = txtNotes.Text.Trim();

                MJdatabase.SaveCollectionReceipt(selectedCollection);
                MessageBox.Show("Collection Receipt updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
            }
            catch (Exception ex)
            {
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

                var res = MessageBox.Show($"Are you sure you want to delete Collection Receipt {selectedCollection.ReceiptNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("CollectionReceipts", selectedCollection.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Collection Receipt deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            {
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
                ? allReceipts
                : allReceipts.Where(r =>
                    (!string.IsNullOrEmpty(r.ReceiptNumber) && r.ReceiptNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.InvoiceNumber) && r.InvoiceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.PaymentMethod) && r.PaymentMethod.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.ReferenceNumber) && r.ReferenceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.Notes) && r.Notes.ToLower().Contains(q))
                ).ToList();

            gridCollections.DataSource = null;
            gridCollections.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Payment Collections Register Log ({allReceipts.Count})"
                : $"Payment Collections Register Log - Showing {filtered.Count} of {allReceipts.Count}";
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
                CollectionReceipt sampleCr = new CollectionReceipt
                {
                    ReceiptNumber = "0302",
                    InvoiceNumber = "SI NO. 0668",
                    PaymentDate = new DateTime(2026, 2, 24),
                    AmountPaid = 207047.14m,
                    PaymentMethod = "Check",
                    ReferenceNumber = "FEB. 24, 2026",
                    Notes = "Payment for HAULING SERVICES"
                };
                PrintHelper.PrintOfficialCollectionReceipt(sampleCr);
            }
            else
            {
                PrintHelper.PrintOfficialCollectionReceipt(selectedCollection);
            }
        }

        private void ClearFields()
        {
            selectedCollection = null;
            cbPaymentMethod.Text = "Cash";
            txtRefNo.Text = "";
            txtNotes.Text = "";
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedCollection != null) RecordDetailsDialog.ShowDetails(this, selectedCollection);
            });
            var itemCopyOr = new ToolStripMenuItem("📋 Copy Receipt (OR) #", null, (s, e) => {
                if (selectedCollection != null) { Clipboard.SetText(selectedCollection.ReceiptNumber); ToastHelper.ShowToast(this, "Receipt # copied ✓"); }
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Official Receipt (OR)", null, BtnPrint_Click);
            var itemDelete = new ToolStripMenuItem("🗑️ Void / Delete Receipt", null, BtnDelete_Click);

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyOr, new ToolStripSeparator(), itemPrint, new ToolStripSeparator(), itemDelete });

            gridCollections.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridCollections.ClearSelection();
                    gridCollections.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridCollections, gridCollections.PointToClient(Cursor.Position));
                }
            };
        }
    }
}
