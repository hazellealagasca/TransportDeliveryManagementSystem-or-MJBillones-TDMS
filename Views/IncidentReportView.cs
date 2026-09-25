using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class IncidentReportView : UserControl, IRefreshable
    {
        private IncidentReport? selectedIncident = null;
        private bool isBindingForm = false;
        private List<IncidentReport> allIncidents = new List<IncidentReport>();
        private string currentSortColumn = "Id";
        private bool isSortAscending = true;
        private List<Delivery> deliveries = new List<Delivery>();
        private string currentPhotoPath = string.Empty;
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        public IncidentReportView()
        {
            InitializeComponent();
            gridIncidents.BringToFront();
            SetupGridColumns();
            SetupContextMenu();
            gridIncidents.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridIncidents.Rows[e.RowIndex].DataBoundItem is IncidentReport item) RecordDetailsDialog.ShowDetails(this, item); };
            gridIncidents.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridIncidents.Columns.Count) return;
                string col = gridIncidents.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            gridIncidents.CurrentCellDirtyStateChanged += (s, e) => { if (gridIncidents.IsCurrentCellDirty && gridIncidents.CurrentCell is DataGridViewCheckBoxCell) gridIncidents.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridIncidents.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridIncidents.Rows[e.RowIndex].DataBoundItem is IncidentReport inc) {
                    try
                    {
                        bool success = MJdatabase.ToggleIncidentChargeable(inc.Id, inc.ChargeableToCrew);
                        if (!success)
                        {
                            inc.ChargeableToCrew = !inc.ChargeableToCrew;
                            gridIncidents.InvalidateRow(e.RowIndex);
                            MessageBox.Show("Failed to update chargeable status.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        if (selectedIncident != null && selectedIncident.Id == inc.Id) {
                            chkChargeable.Checked = inc.ChargeableToCrew;
                        }
                        gridIncidents.InvalidateRow(e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        inc.ChargeableToCrew = !inc.ChargeableToCrew;
                        gridIncidents.InvalidateRow(e.RowIndex);
                        MessageBox.Show($"Failed to update incident chargeable status: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            SetupDraftAutoSave();
            LoadData();

            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Incident Logs to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridIncidents, "Incident_Reports", "Goods Damage and Incident Reports"));
            menuActions.Items.Add("🖨️ Print Incident Investigation Report", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
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
                Name = "btnCloseForm",
                Text = "✕ Close",
                Size = new Size(80, 28),
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
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

        public void PrefillFromTripVariance(int deliveryId, string dispatchNumber, int missingCrates, DateTime tripDate)
        {
            for (int i = 0; i < deliveries.Count; i++)
            {
                if (deliveries[i].Id == deliveryId || deliveries[i].DispatchNumber == dispatchNumber)
                {
                    cbDelivery.SelectedIndex = i;
                    break;
                }
            }
            numDamagedQty.Value = Math.Max(numDamagedQty.Minimum, Math.Min(numDamagedQty.Maximum, missingCrates));
            numEstimatedCost.Value = Math.Max(numEstimatedCost.Minimum, Math.Min(numEstimatedCost.Maximum, missingCrates * 450m));
            txtCause.Text = "Missing / Damaged Crates from Trip Delivery";
            chkChargeable.Checked = true;
            txtDescription.Text = $"Automated variance log: {missingCrates} crate(s) discrepancy noted on delivery {dispatchNumber} on {tripDate:MM/dd/yyyy}.";
        }

        private void SetupGridColumns()
        {
            gridIncidents.ReadOnly = false;
            gridIncidents.EditMode = DataGridViewEditMode.EditOnEnter;
            gridIncidents.AutoGenerateColumns = false;
            gridIncidents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridIncidents.ScrollBars = ScrollBars.Both;
            gridIncidents.AllowUserToResizeColumns = true;
            gridIncidents.ShowCellToolTips = true;
            gridIncidents.Columns.Clear();
            gridIncidents.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "ChargeableToCrew", HeaderText = "Charge Crew", FillWeight = 10, MinimumWidth = 90, ReadOnly = false });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 12, MinimumWidth = 100, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IncidentDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 10, MinimumWidth = 85, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DamagedQuantity", HeaderText = "Damaged", FillWeight = 9, MinimumWidth = 75, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstimatedCost", HeaderText = "Est. Cost", DefaultCellStyle = { Format = "N2" }, FillWeight = 11, MinimumWidth = 90, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cause", HeaderText = "Cause", FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Remarks", FillWeight = 18, MinimumWidth = 120, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhotoPath", HeaderText = "Photo", FillWeight = 8, MinimumWidth = 65, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 8, MinimumWidth = 75, ReadOnly = true });
        }

        private void GridIncidents_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridIncidents.SelectedRows.Count > 0 && gridIncidents.SelectedRows[0].DataBoundItem is IncidentReport inc)
            {
                isBindingForm = true;
                selectedIncident = inc;

                int dIdx = deliveries.FindIndex(d => d.Id == selectedIncident.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;

                numDamagedQty.Value = Math.Max(0, selectedIncident.DamagedQuantity);
                numEstimatedCost.Value = Math.Max(0, selectedIncident.EstimatedCost);
                txtCause.Text = selectedIncident.Cause;
                chkChargeable.Checked = selectedIncident.ChargeableToCrew;
                txtDescription.Text = selectedIncident.Description;

                currentPhotoPath = selectedIncident.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = "Incident Details Form";
                isBindingForm = false;
            }
        }

        private void DisplayPhotoPreview(string photoPath)
        {
            try
            {
                string abs = SecurityHelper.GetAbsolutePhotoPath(photoPath);
                if (!string.IsNullOrEmpty(abs) && File.Exists(abs))
                {
                    SecurityHelper.SafeSetPictureBoxImage(picPhoto, abs);
                    btnViewPhoto.Enabled = true;
                    btnRemovePhoto.Enabled = true;
                }
                else
                {
                    SecurityHelper.SafeSetPictureBoxImage(picPhoto, null);
                    btnViewPhoto.Enabled = false;
                    btnRemovePhoto.Enabled = false;
                }
            }
            catch
            {
                SecurityHelper.SafeSetPictureBoxImage(picPhoto, null);
                btnViewPhoto.Enabled = false;
                btnRemovePhoto.Enabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                deliveries = MJdatabase.GetDeliveries();
                cbDelivery.Items.Clear();
                foreach (var d in deliveries)
                {
                    cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo}");
                }
                if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;

                allIncidents = MJdatabase.GetIncidentReports();
                FilterGridData();

                int totalDamaged = allIncidents.Sum(i => i.DamagedQuantity);
                decimal totalCost = allIncidents.Sum(i => i.EstimatedCost);
                lblSummary.Text = $"📊 Total Incident Reports: {allIncidents.Count:N0}  |  📦 Total Damaged: {totalDamaged:N0} Units  |  ⚠️ Estimated Loss: PHP {totalCost:N2}";

                ClearForm();
                CheckAndPromptDraft();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load incident reports: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUploadPhoto_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Cargo Damage / Incident Proof Photo",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!SecurityHelper.IsValidImageFile(ofd.FileName, out string errorMsg))
                    {
                        MessageBox.Show(errorMsg, "Invalid Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        string savedRelPath = SecurityHelper.SaveReferencePhoto(ofd.FileName, "Incidents");
                        currentPhotoPath = savedRelPath;
                        DisplayPhotoPreview(currentPhotoPath);
                        MessageBox.Show("Photo uploaded and attached successfully for reference!", "Upload Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save photo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnViewPhoto_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPhotoPath))
            {
                MessageBox.Show("No photo attached to view.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PhotoViewerDialog viewer = new PhotoViewerDialog(currentPhotoPath, $"Incident Photo - {txtCause.Text}");
            viewer.ShowDialog(this);
        }

        private void BtnRemovePhoto_Click(object? sender, EventArgs e)
        {
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(currentPhotoPath);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbDelivery.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a trip order.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = deliveries[cbDelivery.SelectedIndex];

                if (string.IsNullOrWhiteSpace(txtCause.Text))
                {
                    MessageBox.Show("Please enter or select the primary cause of the incident.", "Incident Cause Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCause.Focus();
                    return;
                }

                IncidentReport ir = new IncidentReport
                {
                    DeliveryId = selDel.Id,
                    DispatchNumber = selDel.DispatchNumber,
                    IncidentDate = DateTime.Now,
                    DamagedQuantity = (int)numDamagedQty.Value,
                    EstimatedCost = numEstimatedCost.Value,
                    Cause = txtCause.Text.Trim(),
                    ChargeableToCrew = chkChargeable.Checked,
                    Description = txtDescription.Text.Trim(),
                    Status = "Logged",
                    PhotoPath = currentPhotoPath
                };

                MJdatabase.SaveIncidentReport(ir);
                MJdatabase.ClearFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
                MessageBox.Show("Incident & cargo damage log saved successfully!", "Log Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedIncident == null)
                {
                    MessageBox.Show("Please select an incident record from the table to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCause.Text))
                {
                    MessageBox.Show("Please enter or select the primary cause of the incident.", "Incident Cause Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCause.Focus();
                    return;
                }

                selectedIncident.DamagedQuantity = (int)numDamagedQty.Value;
                selectedIncident.EstimatedCost = numEstimatedCost.Value;
                selectedIncident.Cause = txtCause.Text.Trim();
                selectedIncident.ChargeableToCrew = chkChargeable.Checked;
                selectedIncident.Description = txtDescription.Text.Trim();
                selectedIncident.PhotoPath = currentPhotoPath;

                MJdatabase.SaveIncidentReport(selectedIncident);
                MessageBox.Show("Incident record updated successfully!", "Update Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedIncident == null)
                {
                    MessageBox.Show("Please select an incident to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete incident log for Dispatch '{selectedIncident.DispatchNumber}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("IncidentReports", selectedIncident.Id);
                    if (deleted)
                    {
                        MessageBox.Show("Incident report deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete incident report. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            selectedIncident = null;
            if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
            numDamagedQty.Value = 2;
            numEstimatedCost.Value = 1500;
            txtCause.Text = "";
            chkChargeable.Checked = false;
            txtDescription.Text = "";
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(currentPhotoPath);
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allIncidents
                : allIncidents.Where(i =>
                    (!string.IsNullOrEmpty(i.DispatchNumber) && i.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Cause) && i.Cause.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Description) && i.Description.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Status) && i.Status.ToLower().Contains(q))
                ).ToList();

            switch (currentSortColumn)
            {
                case "Id":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Id).ToList() : filtered.OrderByDescending(i => i.Id).ToList();
                    break;
                case "DispatchNumber":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.DispatchNumber).ToList() : filtered.OrderByDescending(i => i.DispatchNumber).ToList();
                    break;
                case "IncidentDate":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.IncidentDate).ToList() : filtered.OrderByDescending(i => i.IncidentDate).ToList();
                    break;
                case "DamagedQuantity":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.DamagedQuantity).ToList() : filtered.OrderByDescending(i => i.DamagedQuantity).ToList();
                    break;
                case "EstimatedCost":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.EstimatedCost).ToList() : filtered.OrderByDescending(i => i.EstimatedCost).ToList();
                    break;
                case "Cause":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Cause).ToList() : filtered.OrderByDescending(i => i.Cause).ToList();
                    break;
                case "Status":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Status).ToList() : filtered.OrderByDescending(i => i.Status).ToList();
                    break;
            }

            foreach (DataGridViewColumn col in gridIncidents.Columns)
            {
                string baseText = col.HeaderText.Replace(" ▲", "").Replace(" ▼", "");
                col.HeaderText = col.DataPropertyName == currentSortColumn ? baseText + (isSortAscending ? " ▲" : " ▼") : baseText;
            }

            gridIncidents.SelectionChanged -= GridIncidents_SelectionChanged;
            gridIncidents.DataSource = null;
            gridIncidents.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Incident & Damage Logs ({allIncidents.Count})"
                : $"Active Incident & Damage Logs ({filtered.Count} of {allIncidents.Count})";

            if (filtered.Count > 0)
            {
                gridIncidents.ClearSelection();
                gridIncidents.Rows[0].Selected = true;
                isBindingForm = true;
                selectedIncident = filtered[0];
                int dIdx = deliveries.FindIndex(d => d.Id == selectedIncident.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;
                numDamagedQty.Value = Math.Max(0, selectedIncident.DamagedQuantity);
                numEstimatedCost.Value = Math.Max(0, selectedIncident.EstimatedCost);
                txtCause.Text = selectedIncident.Cause;
                chkChargeable.Checked = selectedIncident.ChargeableToCrew;
                txtDescription.Text = selectedIncident.Description;
                currentPhotoPath = selectedIncident.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = "Incident Details Form";
                isBindingForm = false;
            }
            else
            {
                isBindingForm = true;
                selectedIncident = null;
                txtDescription.Text = "";
                txtCause.Text = "";
                numDamagedQty.Value = 0;
                numEstimatedCost.Value = 0;
                lblFormHeader.Text = "Incident Details Form";
                isBindingForm = false;
            }
            gridIncidents.SelectionChanged += GridIncidents_SelectionChanged;
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
            try
            {
                PrintHelper.PrintGridReport("Incident & Cargo Damage Incident Logs", gridIncidents, "Official log of cargo damage, transit incidents, and driver liability charges.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (selectedIncident != null) RecordDetailsDialog.ShowDetails(this, selectedIncident);
            });
            var itemCopyInc = new ToolStripMenuItem("📋 Copy Dispatch Reference", null, (s, e) => {
                if (selectedIncident != null) { Clipboard.SetText(selectedIncident.DispatchNumber); ToastHelper.ShowToast(this, "Dispatch ref copied ✓"); }
            });
            var itemEdit = new ToolStripMenuItem("✏️ Edit Incident Details", null, (s, e) => {
                SetFormVisibility(true);
                cbDelivery.Focus();
            });
            var itemPrint = new ToolStripMenuItem("📄 Print Incident Investigation Report", null, BtnPrint_Click);
            var itemDelete = new ToolStripMenuItem("🗑️ Delete Incident Record", null, BtnDelete_Click);

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemCopyInc, new ToolStripSeparator(), itemEdit, itemPrint, new ToolStripSeparator(), itemDelete });

            gridIncidents.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridIncidents.ClearSelection();
                    gridIncidents.Rows[e.RowIndex].Selected = true;
                    menu.Show(gridIncidents, gridIncidents.PointToClient(Cursor.Position));
                }
            };
        }

        private void SetupDraftAutoSave()
        {
            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };
            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();
            tmrAutoSaveDraft.Start();
        }

        private void CheckAndPromptDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) pnlFormBody.Controls.Remove(pnlDraftBanner);
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Incident Report", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlFormBody.Controls.Add(pnlDraftBanner);
                    pnlDraftBanner.BringToFront();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentReport] CheckAndPromptDraft: {ex.Message}");
            }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("DeliveryIndex", out var dIdx) && int.TryParse(dIdx, out int idx) && idx >= 0 && idx < cbDelivery.Items.Count)
                {
                    cbDelivery.SelectedIndex = idx;
                }
                if (map.TryGetValue("DamagedQuantity", out var dq) && decimal.TryParse(dq, out var dqVal))
                {
                    numDamagedQty.Value = Math.Min(numDamagedQty.Maximum, Math.Max(numDamagedQty.Minimum, dqVal));
                }
                if (map.TryGetValue("EstimatedCost", out var ec) && decimal.TryParse(ec, out var ecVal))
                {
                    numEstimatedCost.Value = Math.Min(numEstimatedCost.Maximum, Math.Max(numEstimatedCost.Minimum, ecVal));
                }
                if (map.TryGetValue("Cause", out var c) && !string.IsNullOrWhiteSpace(c))
                {
                    txtCause.Text = c;
                }
                if (map.TryGetValue("Chargeable", out var ch) && bool.TryParse(ch, out var chVal))
                {
                    chkChargeable.Checked = chVal;
                }
                if (map.TryGetValue("Description", out var desc) && !string.IsNullOrWhiteSpace(desc))
                {
                    txtDescription.Text = desc;
                }
                if (map.TryGetValue("PhotoPath", out var pp))
                {
                    currentPhotoPath = pp ?? "";
                    DisplayPhotoPreview(currentPhotoPath);
                }

                isBindingForm = false;
                ToastHelper.ShowToast(this, "Incident Report draft restored ✓", ToastType.Success);
            }
            catch (Exception ex)
            {
                isBindingForm = false;
                System.Diagnostics.Debug.WriteLine($"[IncidentReport] RestoreDraft: {ex.Message}");
            }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("IncidentReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Incident Report draft discarded ✓", ToastType.Info);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedIncident != null) return;
            if (string.IsNullOrWhiteSpace(txtCause.Text) && string.IsNullOrWhiteSpace(txtDescription.Text) && numDamagedQty.Value == 0 && string.IsNullOrWhiteSpace(currentPhotoPath)) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["DeliveryIndex"] = cbDelivery.SelectedIndex.ToString(),
                    ["DamagedQuantity"] = numDamagedQty.Value.ToString(),
                    ["EstimatedCost"] = numEstimatedCost.Value.ToString(),
                    ["Cause"] = txtCause.Text.Trim(),
                    ["Chargeable"] = chkChargeable.Checked.ToString(),
                    ["Description"] = txtDescription.Text.Trim(),
                    ["PhotoPath"] = currentPhotoPath
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("IncidentReport", SessionContext.CurrentUsername, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentReport] SaveCurrentDraft: {ex.Message}");
            }
        }
    }
}
