using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class SpecialTripHistoryView : UserControl, IRefreshable
    {
        private TextBox txtSearch = null!;
        private ComboBox cbStatusFilter = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private Button btnNewSpecialTrip = null!;
        private Button btnExportCsv = null!;
        private Button btnRefresh = null!;

        private Label lblKpiTotal = null!;
        private Label lblKpiPending = null!;
        private Label lblKpiInTransit = null!;
        private Label lblKpiCompleted = null!;

        private DataGridView gridSpecialTrips = null!;
        private List<SpecialTrip> allSpecialTrips = new List<SpecialTrip>();

        public SpecialTripHistoryView()
        {
            InitializeCustomComponent();
            LoadData();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void InitializeCustomComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Pal.White,
                Padding = new Padding(24, 16, 24, 12)
            };

            TableLayoutPanel tblHeaderContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tblHeaderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tblHeaderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));

            Panel pnlTitleBlock = new Panel { Dock = DockStyle.Fill };
            Label lblTitle = new Label
            {
                Text = "🌟  Special Trip History",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 4)
            };
            Label lblSubtitle = new Label
            {
                Text = "Historical archive, itemized stop breakdown, and lifecycle execution of special on-demand dispatches",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(100, 116, 139),
                AutoSize = true,
                Location = new Point(2, 34)
            };
            pnlTitleBlock.Controls.Add(lblTitle);
            pnlTitleBlock.Controls.Add(lblSubtitle);

            FlowLayoutPanel flowKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            Panel cardCompleted = CreateKpiCard("Completed", "0", Pal.C(16, 185, 129), out lblKpiCompleted);
            Panel cardInTransit = CreateKpiCard("In-Transit", "0", Pal.C(14, 165, 233), out lblKpiInTransit);
            Panel cardPending = CreateKpiCard("Pending", "0", Pal.C(245, 158, 11), out lblKpiPending);
            Panel cardTotal = CreateKpiCard("Total Trips", "0", Pal.C(99, 102, 241), out lblKpiTotal);
            flowKpis.Controls.Add(cardCompleted);
            flowKpis.Controls.Add(cardInTransit);
            flowKpis.Controls.Add(cardPending);
            flowKpis.Controls.Add(cardTotal);

            tblHeaderContent.Controls.Add(pnlTitleBlock, 0, 0);
            tblHeaderContent.Controls.Add(flowKpis, 1, 0);
            pnlHeader.Controls.Add(tblHeaderContent);

            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(24, 10, 24, 10)
            };

            FlowLayoutPanel flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            txtSearch = new TextBox
            {
                Width = 220,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "🔍 Search ref, company, driver...",
                Margin = new Padding(0, 2, 10, 0)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbStatusFilter = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 2, 10, 0)
            };
            cbStatusFilter.Items.AddRange(new object[] { "All Statuses", "Pending", "In-Transit", "Completed", "Cancelled" });
            cbStatusFilter.SelectedIndex = 0;
            cbStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            Label lblFrom = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 8, 4, 0), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60), Margin = new Padding(0, 2, 10, 0) };
            dtpFrom.ValueChanged += (s, e) => ApplyFilter();

            Label lblTo = new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 8, 4, 0), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30), Margin = new Padding(0, 2, 10, 0) };
            dtpTo.ValueChanged += (s, e) => ApplyFilter();

            flowFilters.Controls.Add(txtSearch);
            flowFilters.Controls.Add(cbStatusFilter);
            flowFilters.Controls.Add(lblFrom);
            flowFilters.Controls.Add(dtpFrom);
            flowFilters.Controls.Add(lblTo);
            flowFilters.Controls.Add(dtpTo);

            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false
            };

            btnNewSpecialTrip = new Button
            {
                Text = "✨ + New Special Trip",
                BackColor = Pal.C(14, 116, 144),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(160, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnNewSpecialTrip.FlatAppearance.BorderSize = 0;
            btnNewSpecialTrip.Click += (s, e) => OpenSpecialTripDialog();

            btnExportCsv = new Button
            {
                Text = "📊 Export CSV",
                BackColor = Pal.C(248, 250, 252),
                ForeColor = Pal.C(51, 65, 85),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(110, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnExportCsv.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnExportCsv.Click += BtnExportCsv_Click;

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                BackColor = Pal.C(248, 250, 252),
                ForeColor = Pal.C(71, 85, 105),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(95, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnRefresh.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnRefresh.Click += (s, e) => LoadData();

            flowButtons.Controls.Add(btnNewSpecialTrip);
            flowButtons.Controls.Add(btnExportCsv);
            flowButtons.Controls.Add(btnRefresh);

            pnlToolbar.Controls.Add(flowFilters);
            pnlToolbar.Controls.Add(flowButtons);

            Panel pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 12, 24, 16)
            };

            gridSpecialTrips = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 36 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            gridSpecialTrips.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridSpecialTrips.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 41, 59);
            gridSpecialTrips.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gridSpecialTrips.DefaultCellStyle.SelectionBackColor = Pal.C(224, 242, 254);
            gridSpecialTrips.DefaultCellStyle.SelectionForeColor = Pal.C(15, 23, 42);
            SetupGridColumns();

            gridSpecialTrips.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridSpecialTrips.Rows[e.RowIndex].Tag is SpecialTrip trip)
                {
                    OpenSpecialTripDialog(trip.Id);
                }
            };

            gridSpecialTrips.CellContentClick += GridSpecialTrips_CellContentClick;

            SetupRowMenu();
            pnlGridContainer.Controls.Add(gridSpecialTrips);

            this.Controls.Add(pnlGridContainer);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
        }

        private Panel CreateKpiCard(string title, string value, Color accent, out Label lblVal)
        {
            Panel card = new Panel
            {
                Size = new Size(115, 68),
                BackColor = Pal.C(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(8, 6, 8, 6)
            };

            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Pal.C(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 16
            };

            lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblVal);
            card.Controls.Add(lblT);
            return card;
        }

        private void SetupGridColumns()
        {
            gridSpecialTrips.Columns.Clear();

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRef",
                HeaderText = "Order Reference",
                Width = 145,
                DefaultCellStyle = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Pal.C(14, 116, 144) }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDate",
                HeaderText = "Dispatch Date",
                Width = 105
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompany",
                HeaderText = "Company",
                Width = 170
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAddress",
                HeaderText = "Address / Route",
                Width = 180,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colServiceType",
                HeaderText = "Service Type",
                Width = 140
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTruck",
                HeaderText = "Truck",
                Width = 110
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDriver",
                HeaderText = "Driver",
                Width = 130
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAide",
                HeaderText = "Aide",
                Width = 120
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStops",
                HeaderText = "Stops",
                Width = 65,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPaymentFlags",
                HeaderText = "Payment Setup",
                Width = 150
            });

            DataGridViewButtonColumn colBtnView = new DataGridViewButtonColumn
            {
                Name = "colBtnView",
                HeaderText = "",
                Text = "👁️ View",
                UseColumnTextForButtonValue = true,
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnResched = new DataGridViewButtonColumn
            {
                Name = "colBtnResched",
                HeaderText = "",
                Text = "📅 Resched",
                UseColumnTextForButtonValue = true,
                Width = 85,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnComplete = new DataGridViewButtonColumn
            {
                Name = "colBtnComplete",
                HeaderText = "",
                Text = "✅ Complete",
                UseColumnTextForButtonValue = true,
                Width = 90,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnDel = new DataGridViewButtonColumn
            {
                Name = "colBtnDel",
                HeaderText = "",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = 45,
                FlatStyle = FlatStyle.Flat
            };

            gridSpecialTrips.Columns.AddRange(colBtnView, colBtnResched, colBtnComplete, colBtnDel);
        }

        private void LoadData()
        {
            try
            {
                allSpecialTrips = MJdatabase.GetSpecialTrips();
                UpdateKpiCards();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialTripHistory] LoadData error: {ex.Message}");
            }
        }

        private void UpdateKpiCards()
        {
            int total = allSpecialTrips.Count;
            int pending = allSpecialTrips.Count(t => t.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            int inTransit = allSpecialTrips.Count(t => t.Status.Equals("In-Transit", StringComparison.OrdinalIgnoreCase));
            int completed = allSpecialTrips.Count(t => t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

            lblKpiTotal.Text = total.ToString();
            lblKpiPending.Text = pending.ToString();
            lblKpiInTransit.Text = inTransit.ToString();
            lblKpiCompleted.Text = completed.ToString();
        }

        private void ApplyFilter()
        {
            string query = txtSearch.Text.Trim();
            string status = cbStatusFilter.SelectedItem?.ToString() ?? "All Statuses";
            DateTime fromDate = dtpFrom.Value.Date;
            DateTime toDate = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

            var filtered = allSpecialTrips.Where(t =>
            {
                if (t.DispatchDate < fromDate || t.DispatchDate > toDate) return false;

                if (!status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase) &&
                    !t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    bool match = t.OrderReference.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.Company.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.Address.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.DriverName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.TruckPlate.Contains(query, StringComparison.OrdinalIgnoreCase);
                    if (!match) return false;
                }

                return true;
            }).OrderByDescending(t => t.Id).ToList();

            PopulateGrid(filtered);
        }

        private void PopulateGrid(List<SpecialTrip> trips)
        {
            gridSpecialTrips.Rows.Clear();
            foreach (var t in trips)
            {
                int rIdx = gridSpecialTrips.Rows.Add();
                var row = gridSpecialTrips.Rows[rIdx];
                row.Tag = t;

                row.Cells["colRef"].Value = t.OrderReference;
                row.Cells["colDate"].Value = t.DispatchDate.ToString("yyyy-MM-dd");
                row.Cells["colCompany"].Value = t.Company;
                row.Cells["colAddress"].Value = !string.IsNullOrWhiteSpace(t.Address) ? t.Address : "Direct Dispatch Route";
                row.Cells["colServiceType"].Value = t.ServiceType;
                row.Cells["colTruck"].Value = !string.IsNullOrWhiteSpace(t.TruckPlate) ? t.TruckPlate : "Unassigned";
                row.Cells["colDriver"].Value = !string.IsNullOrWhiteSpace(t.DriverName) ? t.DriverName : "Unassigned";
                row.Cells["colAide"].Value = !string.IsNullOrWhiteSpace(t.AideName) ? t.AideName : "None";
                row.Cells["colStops"].Value = t.TotalStopsCount.ToString();
                row.Cells["colStatus"].Value = t.Status;

                List<string> flags = new List<string>();
                if (t.ModeOfPaymentWithReport) flags.Add("Report");
                if (t.WithSOA) flags.Add("SOA");
                if (t.WithServiceInvoice) flags.Add("Invoice");
                if (t.WithCollection) flags.Add("Receipt");
                row.Cells["colPaymentFlags"].Value = flags.Count > 0 ? string.Join(", ", flags) : "Standard";

                if (t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["colStatus"].Style.ForeColor = Pal.C(5, 150, 105);
                }
                else if (t.Status.Equals("In-Transit", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["colStatus"].Style.ForeColor = Pal.C(2, 132, 199);
                }
                else if (t.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["colStatus"].Style.ForeColor = Pal.C(148, 163, 184);
                }
                else
                {
                    row.Cells["colStatus"].Style.ForeColor = Pal.C(217, 119, 6);
                }
            }
        }

        private void GridSpecialTrips_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !(gridSpecialTrips.Rows[e.RowIndex].Tag is SpecialTrip trip)) return;

            int idxView = gridSpecialTrips.Columns["colBtnView"]?.Index ?? -1;
            int idxResched = gridSpecialTrips.Columns["colBtnResched"]?.Index ?? -1;
            int idxComp = gridSpecialTrips.Columns["colBtnComplete"]?.Index ?? -1;
            int idxDel = gridSpecialTrips.Columns["colBtnDel"]?.Index ?? -1;

            if (e.ColumnIndex == idxView) OpenSpecialTripDialog(trip.Id);
            else if (e.ColumnIndex == idxResched) RescheduleTrip(trip);
            else if (e.ColumnIndex == idxComp) CompleteTrip(trip);
            else if (e.ColumnIndex == idxDel) DeleteTrip(trip);
        }
        private void SetupRowMenu()
        {
            RowMenu.Attach(gridSpecialTrips, (m, rows) =>
            {
                if (rows[0].Tag is not SpecialTrip t) return;
                bool done = RowMenu.Is(t.Status, "Completed"), cancelled = RowMenu.Is(t.Status, "Cancelled");
                m.Items.AddRange(RowMenu.Header($"🌟  {t.OrderReference}", $"{RowMenu.Short(t.Company, 26)} · {t.DispatchDate:MMM d} {t.DepartureTime} · {t.TruckPlate} · {t.Status}"));
                m.Items.Add(RowMenu.Item(done || cancelled ? "Open trip" : "Open / edit trip", () => OpenSpecialTripDialog(t.Id), "Enter"));
                m.Items.Add(RowMenu.Good("Mark as completed", () => CompleteTrip(t), !done && !cancelled));
                m.Items.Add(RowMenu.Item("Reschedule…", () => RescheduleTrip(t), "", !done && !cancelled));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(t.Company), "", !string.IsNullOrWhiteSpace(t.Company)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Order reference", t.OrderReference), ("Client", t.Company), ("Address", t.Address),
                    ("Summary", $"{t.OrderReference} — {t.Company}, {t.DispatchDate:dddd, MMMM d} at {t.DepartureTime}; truck {t.TruckPlate}, driver {t.DriverName}{(string.IsNullOrWhiteSpace(t.AideName) ? "" : ", aide " + t.AideName)}. {t.Status}.")));
                m.Items.Add(RowMenu.Item("Other trips for this client", () => RowMenu.Search(txtSearch, t.Company)));
                m.Items.Add(RowMenu.ExportSub(gridSpecialTrips, "Special_Trips", "Special Trip History"));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete trip…", () => DeleteTrip(t), !done || SessionContext.IsAdmin));
            });
            gridSpecialTrips.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && gridSpecialTrips.CurrentRow?.Tag is SpecialTrip t) { OpenSpecialTripDialog(t.Id); e.Handled = true; e.SuppressKeyPress = true; }
            };
        }
        private void RescheduleTrip(SpecialTrip trip)
        {
            if (RowMenu.Is(trip.Status, "Completed"))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' has already been completed and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (RowMenu.Is(trip.Status, "Cancelled"))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' is cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var dlg = new RescheduleDeliveryDialog(trip);
            if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK) LoadData();
        }
        private void CompleteTrip(SpecialTrip trip)
        {
            if (RowMenu.Is(trip.Status, "Completed"))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' is already marked as Completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (RowMenu.Is(trip.Status, "Cancelled"))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' was cancelled, so it can't be completed.", "Trip Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (trip.DispatchDate.Date > DateTime.Today)
            {
                var early = MessageBox.Show($"Special Trip '{trip.OrderReference}' is scheduled for {trip.DispatchDate:MMMM d, yyyy}, which is still ahead.\n\nMark it completed anyway?", "Trip is in the future", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (early != DialogResult.Yes) return;
            }
            var dr = MessageBox.Show(
                $"Mark Special Trip '{trip.OrderReference}' for {trip.Company} as Completed?\n\nThis will stamp completion and queue any enabled payment workflows (SOA, Invoices, Collections).",
                "Confirm Mark Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;
            if (MJdatabase.MarkSpecialTripCompleted(trip.Id))
            {
                ToastHelper.ShowToast(this, $"{trip.OrderReference} marked completed", ToastType.Success);
                LoadData();
            }
        }
        private void DeleteTrip(SpecialTrip trip)
        {
            if (RowMenu.Is(trip.Status, "Completed") && !SessionContext.IsAdmin)
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' is completed and may already be billed. Only an administrator can delete it.", "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dr = MessageBox.Show(
                $"Are you sure you want to remove Special Trip '{trip.OrderReference}'?\n\n" + MJdatabase.DeletePolicyNote("SpecialTrips"),
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.Yes) return;
            try
            {
                if (MJdatabase.DeleteSpecialTrip(trip.Id))
                {
                    ToastHelper.ShowToast(this, MJdatabase.LastDeleteSummary, ToastType.Info);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Can't remove this trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void OpenSpecialTripDialog(int tripId = 0, Action<SpecialTripFormDialog>? prefill = null)
        {
            using (var dlg = new SpecialTripFormDialog(tripId))
            {
                prefill?.Invoke(dlg);
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (gridSpecialTrips.Rows.Count == 0)
            {
                MessageBox.Show("No special trip records to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                sfd.FileName = $"SpecialTrips_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Order Reference,Dispatch Date,Company,Address,Service Type,Truck,Driver,Aide,Stops,Status,Payment Setup");
                        foreach (DataGridViewRow r in gridSpecialTrips.Rows)
                        {
                            if (r.Tag is SpecialTrip t)
                            {
                                string flagsVal = r.Cells["colPaymentFlags"].Value?.ToString() ?? "Standard";
                                string line = $"\"{t.OrderReference}\",\"{t.DispatchDate:yyyy-MM-dd}\",\"{t.Company}\",\"{t.Address}\",\"{t.ServiceType}\",\"{t.TruckPlate}\",\"{t.DriverName}\",\"{t.AideName}\",{t.TotalStopsCount},\"{t.Status}\",\"{flagsVal}\"";
                                sb.AppendLine(line);
                            }
                        }
                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Special Trip history successfully exported to CSV!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
