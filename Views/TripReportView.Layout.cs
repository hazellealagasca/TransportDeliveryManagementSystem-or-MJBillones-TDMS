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
    /// Full-page Trip Report form: the form replaces the list (same tab), with a back button at the top left,
    /// numbered section cards, a taller store table and a crew pay section for the driver and aides.
    /// </summary>
    public partial class TripReportView
    {
        private const int FormContentMaxWidth = 1100;
        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private bool isEditingExistingReport = false;
        private Panel? pnlHeaderSpacer;
        private FlowLayoutPanel? flowFormActions;
        private Button? btnPrintFromForm;
        // ------------------------------------------------------------------ Crew pay
        private Panel pnlCrewPay = null!;
        private DataGridView gridCrewPay = null!;
        private Label lblCrewPayHint = null!;
        private Label lblCrewPayTotal = null!;
        private Button btnCrewPayReset = null!;
        private int crewPayDeliveryId = 0;
        private bool isBindingCrewPay = false;
        private List<Crew>? crewCache;
        private const string CrewPayHintText = "Starts from each person's daily wage and food allowance in Crew Management. Change any amount here for this trip only.";
        // ------------------------------------------------------------------ Page layout
        private void BuildTripReportCards()
        {
            pnlFormBody.SuspendLayout();
            pnlFormBody.Controls.Clear();
            lblDel.AutoSize = false;
            lblDel.Height = Dpi(26);
            lblDel.Font = new Font("Segoe UI Semibold", 9F);
            lblDel.ForeColor = AppTheme.TextSecondary;
            lblDel.TextAlign = ContentAlignment.BottomLeft;
            cbDelivery.Font = new Font("Segoe UI", 10F);
            lblRem.AutoSize = false;
            lblRem.Height = Dpi(30);
            lblRem.Font = new Font("Segoe UI Semibold", 9F);
            lblRem.ForeColor = AppTheme.TextSecondary;
            lblRem.TextAlign = ContentAlignment.BottomLeft;
            txtRemarks.Height = Dpi(64);
            txtRemarks.Font = new Font("Segoe UI", 10F);
            foreach (var l in new[] { lblDelGCrate, lblDelECrate, lblRetGCrate, lblRetECrate })
            {
                l.Font = new Font("Segoe UI Semibold", 9F);
                l.ForeColor = AppTheme.TextSecondary;
            }
            foreach (var n in new[] { numDeliveredGCrates, numDeliveredECrates, numReturnedGCrates, numReturnedECrates })
            {
                n.Font = new Font("Segoe UI", 10F);
            }
            tblDeliveredCrates.Dock = DockStyle.Fill;
            tblReturnedCrates.Dock = DockStyle.Fill;
            tblDeliveredCrates.Margin = new Padding(0, 0, Dpi(12), 0);
            tblReturnedCrates.Margin = new Padding(Dpi(12), 0, 0, 0);
            var tblCrateTotals = new TableLayoutPanel
            {
                Height = Dpi(64),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0)
            };
            tblCrateTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrateTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrateTotals.Controls.Add(tblDeliveredCrates, 0, 0);
            tblCrateTotals.Controls.Add(tblReturnedCrates, 1, 0);
            btnLogIncident.Height = Dpi(40);
            btnLogIncident.Font = new Font("Segoe UI Semibold", 9.5F);
            var card1 = CreateSectionCard(1, "Dispatch order", "Pick the delivery this report closes out. Its route, truck and crew load automatically.",
                lblDel, cbDelivery, lblDispatchInfoBanner);
            var card2 = CreateSectionCard(2, "Stores & crates", "What each store received and what came back.",
                pnlMultiDropInfo, CreateGap(Dpi(12)), tblCrateTotals, CreateGap(Dpi(8)), btnLogIncident, tblTimes, tblManagerNames);
            var card3 = CreateSectionCard(3, "Driver & aide pay", "Daily wage and meal allowance of each crew member on this trip, recorded with the report for reference.",
                pnlCrewPay);
            var card4 = CreateSectionCard(4, "Plant gate & sign-off", "Truck times at the plant, the people who signed, and any remarks.",
                pnlTripFooter, lblRem, txtRemarks);
            tripAttachments = new AttachmentPanel("Proof of delivery (POD)") { Height = Dpi(250) };
            tripAttachments.AttachmentsChanged += (s, e) => FilterGridData();
            var card5 = CreateSectionCard(5, "Attachments", "Proof of delivery, receipts, trip sheets and photos for this trip. Drop files in or click Add.", tripAttachments);
            pnlFormBody.Controls.Add(card5);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card4);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card3);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card2);
            pnlFormBody.Controls.Add(CreateSectionSpacer());
            pnlFormBody.Controls.Add(card1);
            pnlFormBody.ResumeLayout(true);
        }
        private Panel CreateGap(int h) => new Panel { Height = h, BackColor = AppTheme.Surface };
        private Panel CreateSectionSpacer() => new Panel { Dock = DockStyle.Top, Height = Dpi(16), BackColor = AppTheme.Canvas };
        private Panel CreateSectionCard(int step, string title, string subtitle, params Control?[] contentTopToBottom)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface,
                Padding = new Padding(Dpi(24), Dpi(18), Dpi(24), Dpi(22)),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Resize += (s, e) => card.Invalidate();
            for (int i = contentTopToBottom.Length - 1; i >= 0; i--)
            {
                var c = contentTopToBottom[i];
                if (c == null) continue;
                c.Dock = DockStyle.Top;
                card.Controls.Add(c);
            }
            var header = new Panel { Dock = DockStyle.Top, Height = Dpi(56), BackColor = AppTheme.Surface };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                int d = Dpi(30);
                var circle = new Rectangle(0, Dpi(2), d, d);
                using (var b = new SolidBrush(AppTheme.AccentSoft)) g.FillEllipse(b, circle);
                using (var nf = new Font("Segoe UI Semibold", 10F))
                    TextRenderer.DrawText(g, step.ToString(), nf, circle, AppTheme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                using (var tf = new Font("Segoe UI Semibold", 12F))
                    TextRenderer.DrawText(g, title, tf, new Point(d + Dpi(12), 0), AppTheme.TextPrimary, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                using (var sf = new Font("Segoe UI", 9F))
                    TextRenderer.DrawText(g, subtitle, sf, new Rectangle(d + Dpi(12), Dpi(24), header.Width - d - Dpi(12), Dpi(22)), AppTheme.TextMuted, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                using var pen = new Pen(AppTheme.Border);
                g.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            header.Resize += (s, e) => header.Invalidate();
            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(10), BackColor = AppTheme.Surface };
            card.Controls.Add(gap);
            card.Controls.Add(header);
            return card;
        }
        private void StyleFullPageForm()
        {
            foreach (Control c in pnlForm.Controls)
            {
                if (c.Name == "pnlFormResizeGrip") c.Visible = false;
            }
            pnlForm.BackColor = AppTheme.Canvas;
            pnlFormBody.BackColor = AppTheme.Canvas;
            pnlFormActionsBar.BackColor = AppTheme.Surface;
            pnlFormHeaderBar.BackColor = AppTheme.Canvas;
            pnlFormHeaderBar.Height = Dpi(58);
            pnlFormHeaderBar.Padding = new Padding(0, Dpi(6), 0, Dpi(12));
            btnCloseForm.Text = "←  Back to trip reports";
            btnCloseForm.AutoSize = false;
            btnCloseForm.MinimumSize = Size.Empty;
            btnCloseForm.Padding = new Padding(0);
            btnCloseForm.Dock = DockStyle.Left;
            btnCloseForm.Width = Dpi(190);
            btnCloseForm.Font = new Font("Segoe UI Semibold", 9.5F);
            UIThemeHelper.SetButtonRole(btnCloseForm, "secondary");
            btnCloseForm.ForeColor = AppTheme.TextPrimary;
            pnlHeaderSpacer ??= new Panel { Dock = DockStyle.Left, Width = Dpi(16), BackColor = AppTheme.Canvas };
            if (!pnlFormHeaderBar.Controls.Contains(pnlHeaderSpacer)) pnlFormHeaderBar.Controls.Add(pnlHeaderSpacer);
            lblFormHeader.AutoSize = false;
            lblFormHeader.AutoEllipsis = true;
            lblFormHeader.Dock = DockStyle.Fill;
            lblFormHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblFormHeader.Font = new Font("Segoe UI Semibold", 15F);
            lblFormHeader.ForeColor = AppTheme.TextPrimary;
            lblFormHeader.BackColor = AppTheme.Canvas;
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(pnlHeaderSpacer, 1);
            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 2);
            pnlFormBody.Padding = new Padding(0, 0, 0, Dpi(16));
            pnlForm.Resize -= PnlForm_ResizeCenter;
            pnlForm.Resize += PnlForm_ResizeCenter;
            BuildFormActionBar();
            StyleCrewPayGrid();
            CenterFormContent();
        }
        private void PnlForm_ResizeCenter(object? sender, EventArgs e) => CenterFormContent();
        private void CenterFormContent()
        {
            if (pnlForm == null || pnlForm.Width <= 0) return;
            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);
            pnlFormHeaderBar.Padding = new Padding(side, pnlFormHeaderBar.Padding.Top, side, pnlFormHeaderBar.Padding.Bottom);
            pnlFormBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(16));
            pnlFormActionsBar.Padding = new Padding(side, Dpi(12), side, Dpi(12));
        }
        private void BuildFormActionBar()
        {
            if (flowFormActions != null) return;
            flowFormActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = AppTheme.Surface,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblUpdateDelete.Controls.Remove(btnSave);
            tblUpdateDelete.Controls.Remove(btnUpdate);
            pnlFormActionsBar.Controls.Remove(btnClear);
            tblUpdateDelete.Visible = false;
            btnPrintFromForm = new Button { Text = "🖨  Print trip report", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPrintFromForm.Click += (s, e) => PrintSelectedTripReport();
            btnSave.Text = "✔  Complete trip";
            btnUpdate.Text = "💾  Save changes";
            btnClear.Text = "Clear form";
            foreach (var b in new[] { btnSave, btnUpdate, btnPrintFromForm, btnClear })
            {
                b.Dock = DockStyle.None;
                b.Height = Dpi(40);
                b.Margin = new Padding(Dpi(10), 0, 0, 0);
                b.Font = new Font("Segoe UI Semibold", 9.5F);
                b.Width = Dpi(180);
            }
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnUpdate, "primary");
            UIThemeHelper.SetButtonRole(btnPrintFromForm, "secondary");
            UIThemeHelper.SetButtonRole(btnClear, "secondary");
            // Right-to-left: the main action sits at the far right.
            flowFormActions.Controls.Add(btnSave);
            flowFormActions.Controls.Add(btnUpdate);
            flowFormActions.Controls.Add(btnPrintFromForm);
            flowFormActions.Controls.Add(btnClear);
            pnlFormActionsBar.Controls.Add(flowFormActions);
            flowFormActions.BringToFront();
            pnlFormActionsBar.Height = Dpi(66);
        }
        // ------------------------------------------------------------------ New / edit modes
        private void SetFormVisibility(bool visible)
        {
            tblMasterContent.SuspendLayout();
            pnlForm.Visible = visible;
            pnlGridCard.Visible = !visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
                tblMasterContent.ColumnStyles[0].Width = visible ? 100F : 0F;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].SizeType = visible ? SizeType.Absolute : SizeType.Percent;
                tblMasterContent.ColumnStyles[1].Width = visible ? 0F : 100F;
            }
            pnlForm.Margin = new Padding(0);
            tblMasterContent.ResumeLayout(true);
            if (visible)
            {
                CenterFormContent();
                pnlFormBody.AutoScrollPosition = new Point(0, 0);
            }
            if (gridTripReports.Columns.Count > 0)
            {
                gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New trip report";
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }
        private void OpenNewTripReport()
        {
            isEditingExistingReport = false;
            ClearFields();
            ApplyFormMode();
            SetFormVisibility(true);
            LoadSelectedDeliveryIntoForm();
            cbDelivery.Focus();
        }
        private void OpenEditTripReport(TripReport? tr)
        {
            if (tr == null) return;
            if (selectedTripReport != tr)
            {
                for (int i = 0; i < gridTripReports.Rows.Count; i++)
                {
                    if (gridTripReports.Rows[i].DataBoundItem == tr)
                    {
                        gridTripReports.ClearSelection();
                        gridTripReports.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            if (selectedTripReport != tr) return;
            isEditingExistingReport = true;
            ApplyFormMode();
            SetFormVisibility(true);
            SyncAttachmentOwner();
        }
        private void ApplyFormMode()
        {
            btnSave.Visible = !isEditingExistingReport;
            btnUpdate.Visible = isEditingExistingReport;
            btnClear.Visible = !isEditingExistingReport;
            cbDelivery.Enabled = !isEditingExistingReport;
            lblFormHeader.Text = isEditingExistingReport && selectedTripReport != null
                ? $"Trip Report  ·  {selectedTripReport.TripReportNumber}"
                : "New Trip Report";
        }
        private Delivery? GetSelectedDelivery()
        {
            if (cbDelivery.SelectedIndex < 0) return null;
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            return allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
        }
        private AttachmentPanel? tripAttachments;
        /// <summary>Points the attachments box at the trip's dispatch: the picked dispatch, or the report being edited.</summary>
        private void SyncAttachmentOwner()
        {
            if (tripAttachments == null) return;
            var d = GetSelectedDelivery();
            if (d == null && isEditingExistingReport && selectedTripReport != null)
                d = allDeliveries.FirstOrDefault(x => x.Id == selectedTripReport.DeliveryId || string.Equals(x.DispatchNumber, selectedTripReport.DispatchNumber, StringComparison.OrdinalIgnoreCase));
            if (d != null) { tripAttachments.SetOwner("Dispatch", d.Id, d.DispatchNumber ?? ""); return; }
            if (isEditingExistingReport && selectedTripReport != null && selectedTripReport.DeliveryId > 0)
                tripAttachments.SetOwner("Dispatch", selectedTripReport.DeliveryId, selectedTripReport.DispatchNumber ?? "");
            else tripAttachments.SetOwner("Dispatch", 0, "");
        }
        private void LoadSelectedDeliveryIntoForm()
        {
            var d = GetSelectedDelivery();
            SyncAttachmentOwner();
            if (d == null)
            {
                LoadCrewPay(null);
                return;
            }
            bool wasBinding = isBindingForm;
            isBindingForm = false;
            UpdateMultiDropTripInfo(d);
            RecalculateTotalsFromStoreGrid();
            LoadCrewPay(d);
            isBindingForm = wasBinding;
        }
        // ------------------------------------------------------------------ Crew pay section
        private void InitializeCrewPaySection()
        {
            pnlCrewPay = new Panel { Dock = DockStyle.Top, Height = Dpi(200), BackColor = AppTheme.Surface };
            lblCrewPayHint = new Label
            {
                Dock = DockStyle.Top,
                Height = Dpi(28),
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                AutoEllipsis = true,
                Text = CrewPayHintText
            };
            gridCrewPay = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Vertical,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(34),
                RowTemplate = { Height = Dpi(34) },
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false
            };
            var moneyEdit = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) };
            DataGridViewTextBoxColumn Money(string name, string header) => new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = 80,
                MinimumWidth = Dpi(80),
                ValueType = typeof(decimal),
                DefaultCellStyle = new DataGridViewCellStyle(moneyEdit)
            };
            gridCrewPay.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPayRole", HeaderText = "Role", FillWeight = 55, MinimumWidth = Dpi(60), ReadOnly = true });
            gridCrewPay.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPayName", HeaderText = "Name", FillWeight = 150, MinimumWidth = Dpi(120), ReadOnly = true });
            gridCrewPay.Columns.Add(Money("colPayBase", "Base pay"));
            gridCrewPay.Columns.Add(Money("colPayAllow", "Meal allowance"));
            var colExtra = Money("colPayExtra", "Extra / OT"); colExtra.Visible = false;
            var colDeduct = Money("colPayDeduct", "Deductions"); colDeduct.Visible = false;
            gridCrewPay.Columns.Add(colExtra);
            gridCrewPay.Columns.Add(colDeduct);
            gridCrewPay.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPayNet",
                HeaderText = "Total",
                FillWeight = 85,
                MinimumWidth = Dpi(90),
                ReadOnly = true,
                ValueType = typeof(decimal),
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 9.5F) }
            });
            gridCrewPay.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPayNotes", HeaderText = "Notes", FillWeight = 110, MinimumWidth = Dpi(90), DefaultCellStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) } });
            gridCrewPay.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string col = gridCrewPay.Columns[e.ColumnIndex].Name;
                if (col is not ("colPayBase" or "colPayAllow" or "colPayExtra" or "colPayDeduct")) return;
                string txt = (e.FormattedValue?.ToString() ?? "").Replace(",", "").Replace("PHP", "").Replace("₱", "").Trim();
                if (txt.Length == 0) return;
                if (!decimal.TryParse(txt, out var v) || v < 0)
                {
                    gridCrewPay.Rows[e.RowIndex].ErrorText = "Enter an amount of 0 or more, e.g. 650 or 650.50";
                    e.Cancel = true;
                }
                else
                {
                    gridCrewPay.Rows[e.RowIndex].ErrorText = "";
                }
            };
            gridCrewPay.CellParsing += (s, e) =>
            {
                string col = gridCrewPay.Columns[e.ColumnIndex].Name;
                if (col is not ("colPayBase" or "colPayAllow" or "colPayExtra" or "colPayDeduct")) return;
                string txt = (e.Value?.ToString() ?? "").Replace(",", "").Replace("PHP", "").Replace("₱", "").Trim();
                e.Value = txt.Length == 0 ? 0m : (decimal.TryParse(txt, out var v) ? v : 0m);
                e.ParsingApplied = true;
            };
            gridCrewPay.DataError += (s, e) => { e.ThrowException = false; };
            gridCrewPay.CellValueChanged += (s, e) =>
            {
                if (isBindingCrewPay || e.RowIndex < 0) return;
                RecalculateCrewPay();
            };
            gridCrewPay.CellEndEdit += (s, e) => { if (e.RowIndex >= 0) gridCrewPay.Rows[e.RowIndex].ErrorText = ""; };
            var pnlPayFooter = new Panel { Dock = DockStyle.Bottom, Height = Dpi(52), BackColor = AppTheme.Surface, Padding = new Padding(0, Dpi(10), 0, 0) };
            btnCrewPayReset = new Button
            {
                Text = "↺  Use standard rates",
                Dock = DockStyle.Left,
                Width = Dpi(190),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F),
                Cursor = Cursors.Hand
            };
            UIThemeHelper.SetButtonRole(btnCrewPayReset, "secondary");
            btnCrewPayReset.Click += (s, e) => ResetCrewPayToStandardRates();
            lblCrewPayTotal = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = AppTheme.TextPrimary,
                Text = "Total crew pay: PHP 0.00"
            };
            pnlPayFooter.Controls.Add(lblCrewPayTotal);
            pnlPayFooter.Controls.Add(btnCrewPayReset);
            pnlCrewPay.Controls.Add(gridCrewPay);
            pnlCrewPay.Controls.Add(pnlPayFooter);
            pnlCrewPay.Controls.Add(lblCrewPayHint);
            SizeCrewPaySection(0);
        }
        private void StyleCrewPayGrid()
        {
            if (gridCrewPay == null) return;
            gridCrewPay.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridCrewPay.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridCrewPay.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            gridCrewPay.ColumnHeadersHeight = Dpi(34);
        }
        private void SizeCrewPaySection(int rows)
        {
            if (pnlCrewPay == null) return;
            pnlCrewPay.Height = Dpi(28) + Dpi(34) + Math.Max(1, rows) * Dpi(34) + Dpi(4) + Dpi(52);
        }
        private List<Crew> GetCrewCache()
        {
            if (crewCache == null)
            {
                try { crewCache = MJdatabase.GetCrews(); }
                catch { crewCache = new List<Crew>(); }
            }
            return crewCache;
        }
        /// <summary>Fills the pay table with the dispatch's driver and aides: saved pay if this trip has some, otherwise their standard rates.</summary>
        private void LoadCrewPay(Delivery? d)
        {
            if (gridCrewPay == null) return;
            isBindingCrewPay = true;
            try
            {
                gridCrewPay.Rows.Clear();
                crewPayDeliveryId = d?.Id ?? 0;
                lblCrewPayHint.Text = CrewPayHintText;
                if (d == null)
                {
                    SizeCrewPaySection(0);
                    lblCrewPayTotal.Text = "Pick a dispatch order to see its crew.";
                    btnCrewPayReset.Enabled = false;
                    return;
                }
                var crews = GetCrewCache();
                var recorded = MJdatabase.GetTripCrewPay(d.Id).GroupBy(p => p.CrewId).ToDictionary(g => g.Key, g => g.First());
                var crewIds = MJdatabase.GetDeliveryCrewIds(d);
                bool missingRates = false;
                foreach (var (crewId, role) in crewIds)
                {
                    var crew = crews.FirstOrDefault(c => c.Id == crewId);
                    string name = crew?.FullName ?? (role == "Driver" ? d.DriverName : $"Aide #{crewId}");
                    TripCrewPay p;
                    if (recorded.TryGetValue(crewId, out var saved))
                    {
                        p = saved;
                    }
                    else
                    {
                        p = new TripCrewPay
                        {
                            DeliveryId = d.Id,
                            CrewId = crewId,
                            BasePay = crew?.DailyWage ?? 0,
                            Allowance = crew?.FoodAllowance ?? 0
                        };
                        if ((crew?.DailyWage ?? 0) <= 0) missingRates = true;
                    }
                    p.CrewName = name;
                    p.Role = role;
                    int r = gridCrewPay.Rows.Add(role, name, p.BasePay, p.Allowance, p.Extra, p.Deduction, p.NetPay, p.Notes);
                    gridCrewPay.Rows[r].Tag = p;
                }
                if (missingRates)
                {
                    lblCrewPayHint.Text = "⚠  Some crew have no daily wage set yet. Type their pay here, or set their rates in Crew Management.";
                }
                btnCrewPayReset.Enabled = gridCrewPay.Rows.Count > 0;
                SizeCrewPaySection(gridCrewPay.Rows.Count);
            }
            finally
            {
                isBindingCrewPay = false;
            }
            RecalculateCrewPay();
        }
        private static decimal CellMoney(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value;
            if (v is decimal m) return m;
            return decimal.TryParse((v?.ToString() ?? "").Replace(",", ""), out var p) ? p : 0m;
        }
        private void RecalculateCrewPay()
        {
            if (gridCrewPay == null) return;
            decimal total = 0;
            isBindingCrewPay = true;
            try
            {
                foreach (DataGridViewRow row in gridCrewPay.Rows)
                {
                    decimal net = CellMoney(row, "colPayBase") + CellMoney(row, "colPayAllow");
                    row.Cells["colPayNet"].Value = net;
                    row.Cells["colPayNet"].Style.ForeColor = net < 0 ? AppTheme.DangerText : AppTheme.TextPrimary;
                    total += net;
                }
            }
            finally
            {
                isBindingCrewPay = false;
            }
            if (crewPayDeliveryId == 0) return;
            lblCrewPayTotal.Text = gridCrewPay.Rows.Count == 0
                ? "No driver or aides are assigned to this dispatch."
                : $"Total crew pay for this trip:  PHP {total:N2}";
        }
        private void ResetCrewPayToStandardRates()
        {
            if (gridCrewPay == null || gridCrewPay.Rows.Count == 0) return;
            gridCrewPay.EndEdit();
            var crews = GetCrewCache();
            isBindingCrewPay = true;
            try
            {
                foreach (DataGridViewRow row in gridCrewPay.Rows)
                {
                    if (row.Tag is not TripCrewPay p) continue;
                    var crew = crews.FirstOrDefault(c => c.Id == p.CrewId);
                    row.Cells["colPayBase"].Value = crew?.DailyWage ?? 0m;
                    row.Cells["colPayAllow"].Value = crew?.FoodAllowance ?? 0m;
                    row.Cells["colPayExtra"].Value = 0m;
                    row.Cells["colPayDeduct"].Value = 0m;
                }
            }
            finally
            {
                isBindingCrewPay = false;
            }
            RecalculateCrewPay();
            ToastHelper.ShowToast(this, "Crew pay reset to standard rates", ToastType.Info);
        }
        private List<TripCrewPay> CollectCrewPay()
        {
            var list = new List<TripCrewPay>();
            if (gridCrewPay == null) return list;
            gridCrewPay.EndEdit();
            foreach (DataGridViewRow row in gridCrewPay.Rows)
            {
                if (row.Tag is not TripCrewPay src) continue;
                list.Add(new TripCrewPay
                {
                    DeliveryId = crewPayDeliveryId,
                    CrewId = src.CrewId,
                    CrewName = src.CrewName,
                    Role = src.Role,
                    BasePay = CellMoney(row, "colPayBase"),
                    Allowance = CellMoney(row, "colPayAllow"),
                    Extra = CellMoney(row, "colPayExtra"),
                    Deduction = CellMoney(row, "colPayDeduct"),
                    Notes = row.Cells["colPayNotes"].Value?.ToString()?.Trim() ?? ""
                });
            }
            return list;
        }
        /// <summary>Saves the pay table for the dispatch it was loaded for. Returns false (after telling the user) if it failed.</summary>
        private bool SaveCrewPayFor(int deliveryId)
        {
            if (deliveryId <= 0 || deliveryId != crewPayDeliveryId) return true;
            try
            {
                MJdatabase.SaveTripCrewPay(deliveryId, CollectCrewPay());
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.FindForm(), "The trip report was saved, but the crew pay could not be saved: " + ex.Message, "Crew pay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
