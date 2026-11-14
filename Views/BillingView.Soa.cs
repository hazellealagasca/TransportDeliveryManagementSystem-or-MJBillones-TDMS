using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 10: New invoice starts from a Statement of Account. Pick the statement and the client, trips and every amount come from it
    /// (freight, fuel, withholding, total due, by the one formula in SoaTotals); only the Service Invoice No. is typed. One invoice bills
    /// the whole statement. "Bill one trip directly" keeps the old trip form. Settings → Features → "New invoice bills a Statement of Account".
    /// </summary>
    public partial class BillingView
    {
        private Label lblSoa = null!;
        private ComboBox cbSoa = null!;
        private CheckBox chkDirectTrip = null!;
        private List<SoaToInvoice> soaChoices = new List<SoaToInvoice>();

        private bool SoaFeatureOn => Features.On(Features.InvoiceFromSoa);
        /// <summary>True while the New invoice form is billing a statement (not a saved invoice, not "one trip directly").</summary>
        private bool SoaModeOn => cbSoa != null && SoaFeatureOn && selectedInvoice == null && !chkDirectTrip.Checked;

        private void BuildSoaPicker()
        {
            lblSoa = new Label { AutoSize = true, Dock = DockStyle.Top, Font = lblDel.Font, ForeColor = lblDel.ForeColor, Padding = lblDel.Padding, Text = "Statement of Account *:", UseMnemonic = false };
            cbSoa = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = cbDelivery.Font, Margin = cbDelivery.Margin, FormattingEnabled = true };
            chkDirectTrip = new CheckBox { AutoSize = true, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), ForeColor = lblDel.ForeColor, Text = "Bill one trip directly instead (no Statement of Account)", Padding = new Padding(0, 4, 0, 4), UseVisualStyleBackColor = true };
            pnlFormBody.Controls.Add(chkDirectTrip);
            pnlFormBody.Controls.Add(cbSoa);
            pnlFormBody.Controls.Add(lblSoa);
            cbSoa.SelectedIndexChanged += (s, e) => OnSoaPicked();
            chkDirectTrip.CheckedChanged += (s, e) => { SwitchInvoiceSource(); ScheduleBillGuide(); };
        }

        /// <summary>
        /// The "Bill one trip directly" box was ticked or unticked: drop everything the other route filled in (client, trips text, amounts,
        /// header) so nothing stale stays editable, then show the right picker. Layout is suspended so the swap doesn't flicker.
        /// </summary>
        private void SwitchInvoiceSource()
        {
            if (cbSoa == null) return;
            if (isBindingForm) { ApplyInvoiceSource(); return; } // Clear form / programmatic reset does its own clearing
            pnlFormBody.SuspendLayout();
            bool was = isBindingForm; isBindingForm = true;
            try
            {
                if (selectedInvoice == null)
                {
                    cbSoa.SelectedIndex = -1;
                    cbDelivery.SelectedIndex = -1; cbDelivery.Enabled = true;
                    cbClient.SelectedIndex = -1; cbClient.Text = "";
                    txtLoadingSchedule.Text = "";
                    numTotalAmount.SetSafe(0); numFuel.SetSafe(0);
                    lblTariffMatch.Visible = false;
                    lblFormHeader.Text = "Generate Service Invoice";
                    RecalculateNet(this, EventArgs.Empty);
                }
                ApplyInvoiceSource();
            }
            finally { isBindingForm = was; pnlFormBody.ResumeLayout(true); }
        }

        /// <summary>Statements that can be billed with one invoice now (re-read from the database).</summary>
        private void ReloadSoaChoices()
        {
            if (cbSoa == null) return;
            try { soaChoices = SoaFeatureOn ? MJdatabase.GetSoasWaitingForOwnInvoice() : new List<SoaToInvoice>(); }
            catch (Exception ex) { ErrLog.Swallowed(ex); soaChoices = new List<SoaToInvoice>(); }
            bool was = isBindingForm; isBindingForm = true;
            try
            {
                cbSoa.Items.Clear();
                foreach (var s in soaChoices) cbSoa.Items.Add(s.Label);
                cbSoa.SelectedIndex = -1;
            }
            finally { isBindingForm = was; }
        }

        private SoaToInvoice? SelectedSoa() => cbSoa != null && cbSoa.SelectedIndex >= 0 && cbSoa.SelectedIndex < soaChoices.Count ? soaChoices[cbSoa.SelectedIndex] : null;

        /// <summary>Shows the Statement of Account box (default) or the Completed Delivery Trip box (direct route / saved invoice).</summary>
        private void ApplyInvoiceSource()
        {
            if (cbSoa == null) return;
            bool newForm = SoaFeatureOn && selectedInvoice == null;
            bool soa = SoaModeOn;
            chkDirectTrip.Visible = false;   // Nov 10: an order is billed only through its Statement of Account (no direct trip invoice)
            lblSoa.Visible = cbSoa.Visible = soa;
            lblDel.Visible = cbDelivery.Visible = !soa;
            bool ownSoa = selectedInvoice != null && selectedInvoice.SoaId > 0 && selectedInvoice.DeliveryId <= 0 && string.IsNullOrWhiteSpace(selectedInvoice.DispatchNumber);
            lblLoadingSchedule.Text = soa ? "Trips on the statement:" : ownSoa ? "Statement of Account:" : "Completed trip report:";
            txtLoadingSchedule.PlaceholderText = soa ? "The trips of the picked statement..." : "The completed trip report of the picked trip...";
            if (soa)
            {
                // The trip box has no say while a statement is being billed.
                bool was = isBindingForm; isBindingForm = true;
                try { cbDelivery.SelectedIndex = -1; } finally { isBindingForm = was; }
                if (SelectedSoa() == null) { numTotalAmount.Enabled = false; numFuel.Enabled = false; }
            }
            else if (selectedInvoice == null && cbDelivery.SelectedIndex < 0) { numTotalAmount.Enabled = false; numFuel.Enabled = false; lblTariffMatch.Visible = false; } // no trip picked yet: nothing to bill, amounts stay locked until one is
        }

        /// <summary>A statement was picked: client, trips and every amount are filled from it and locked.</summary>
        private void OnSoaPicked()
        {
            if (isBindingForm) return;
            var s = SelectedSoa();
            if (s == null) { ScheduleBillGuide(); return; }
            var t = s.Totals;
            lblFormHeader.Text = $"New invoice · {s.SoaNumber}";
            if (!cbClient.Items.Contains(s.ClientName)) cbClient.Items.Add(s.ClientName);
            cbClient.SelectedItem = s.ClientName; cbClient.Text = s.ClientName;
            txtLoadingSchedule.Text = $"{s.Trips} trip{(s.Trips == 1 ? "" : "s")}" + (s.TripReports.Length > 0 ? " · " + s.TripReports : "");
            decimal freightOnly = t.TotalSales - t.Fuel; // TotalSales now includes the fuel surcharge; the form has the two typed apart
            if (freightOnly > numTotalAmount.Maximum) numTotalAmount.Maximum = freightOnly * 2;
            if (t.Fuel > numFuel.Maximum) numFuel.Maximum = t.Fuel * 2;
            numTotalAmount.SetSafe(freightOnly);
            numFuel.SetSafe(t.Fuel);
            numTotalAmount.Enabled = false; numFuel.Enabled = false;
            lblTariffMatch.Text = $"🔒 From {s.SoaNumber}: total sales PHP {t.TotalSales:N2} (freight PHP {freightOnly:N2} + fuel surcharge PHP {t.Fuel:N2}, VAT inclusive) − withholding tax PHP {t.Withholding:N2} = total amount due PHP {t.Due:N2}. Nothing is typed here except the Service Invoice No.";
            lblTariffMatch.Visible = true;
            RecalculateNet(this, EventArgs.Empty);
            ScheduleBillGuide();
        }

        /// <summary>"Issue Invoice" on a statement: opens New invoice on that statement when it can be billed whole; false when it can't (the caller uses the per-trip form).</summary>
        private bool OpenInvoiceForSoa(int soaId)
        {
            if (!SoaFeatureOn || MJdatabase.WhySoaCannotBeInvoiced(soaId) != null) return false;
            OpenNewRecordForm();
            chkDirectTrip.Checked = false;
            ReloadSoaChoices();
            int idx = soaChoices.FindIndex(x => x.SoaId == soaId);
            if (idx < 0) return false;
            cbSoa.SelectedIndex = idx;
            UpdateBillGuide();
            return true;
        }

        /// <summary>The checklist lines of a statement invoice.</summary>
        private void AddSoaChecks(List<PaymentCheck> checks, string client)
        {
            var s = SelectedSoa();
            if (s == null)
            {
                checks.Add(new PaymentCheck(soaChoices.Count == 0 ? "No Statement of Account is waiting for an invoice (save a statement first)" : "Pick a Statement of Account to invoice (Statement of Account is required)", false));
                return;
            }
            string? why = null;
            try { why = MJdatabase.WhySoaCannotBeInvoiced(s.SoaId); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            checks.Add(new PaymentCheck(why ?? $"{s.SoaNumber} is saved and not billed yet: one invoice bills its {s.Trips} trip{(s.Trips == 1 ? "" : "s")}", why == null));
            bool clientOk = string.Equals(s.ClientName, client, StringComparison.OrdinalIgnoreCase);
            checks.Add(new PaymentCheck(clientOk ? $"Client is the statement's client ({s.ClientName})" : $"Client must be {s.ClientName} (the statement's client)", clientOk));
            checks.Add(new PaymentCheck($"Amounts come from the statement: total amount due PHP {s.Totals.Due:N2}", true));
        }

        /// <summary>The statement card on the left and its computation on the right.</summary>
        private void UpdateBillSoa()
        {
            if (billTape == null || billNetTape == null) return;
            var s = SelectedSoa();
            if (s == null) { billTape.Set("The statement being billed", Array.Empty<(string, string)>(), "", "", ""); return; }
            billTape.Set("The statement being billed", new List<(string, string)> { ("Statement", s.SoaNumber), ("Client", s.ClientName), ("Trips", s.Trips.ToString("N0")), ("Trip reports", s.TripReports.Length > 0 ? s.TripReports : "—") },
                "Total amount due", "₱" + s.Totals.Due.ToString("N2"), "worked out on the statement");
            var t = s.Totals;
            var rows = new List<(string, string)> { ("Total Sales + Fuel surcharge (VAT Inclusive)", "₱" + t.TotalSales.ToString("N2")), ("Less: VAT (12%)", "₱" + t.Vat.ToString("N2")), ("Amount: Net of VAT", "₱" + t.NetOfVat.ToString("N2")),
                ("Less: Withholding Tax (2%)", "₱" + t.Withholding.ToString("N2")) };
            rows.Add(("Payment due", $"{Fmt.Date(DateTime.Today.AddDays(15))} · 15 days"));
            billNetTape.Set("What this invoice bills", rows, "Total amount due", "₱" + t.Due.ToString("N2"), "");
        }

        /// <summary>Issue invoice for the picked statement: one invoice, the statement's figures, the typed SI No.</summary>
        private void SaveInvoiceForSelectedSoa()
        {
            try
            {
                var s = SelectedSoa();
                if (s == null) { MessageBox.Show(FindForm(), "Pick a Statement of Account to invoice.", "Statement of Account", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                string invNum = txtInvoiceNumber.Text.Trim();
                var siWhy = MJdatabase.CheckSiNumber(invNum, 0);
                if (siWhy != null) { MessageBox.Show(FindForm(), siWhy, "Service Invoice No.", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtInvoiceNumber.Focus(); return; }
                var inv = MJdatabase.SaveInvoiceForSoa(s.SoaId, invNum, billNumberGuard.CrossTypeConfirmed);
                BillAfterSave(inv.Id, inv.InvoiceNumber);
                QuietBox.Show($"Service Invoice {inv.InvoiceNumber} was issued for statement {s.SoaNumber}: total amount due PHP {inv.NetAmount:N2}, 15-day credit terms.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Can't issue this invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadSoaChoices();
            }
            catch (Exception ex)
            {
                ErrLog.Error("BillingView.SoaInvoice", ex);
                MessageBox.Show(FindForm(), $"Failed to save invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { BillSaveFinished(); }
        }
    }
}
