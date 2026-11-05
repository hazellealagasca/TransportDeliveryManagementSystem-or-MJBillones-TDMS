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
    public partial class SpecialTripHistoryView : UserControl, IRefreshable, IUnsavedForm
    {
        private TextBox txtSearch = null!;
        private ComboBox cbStatusFilter = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private DateFilterChip dateChip = null!;
        private Button btnNewSpecialTrip = null!;
        private Button btnExportCsv = null!;
        private Button btnRefresh = null!;

        private TileStrip tripTiles = null!;
        private StatTile tAll = null!, tPending = null!, tTransit = null!, tDone = null!;
        private bool syncingTiles;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
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

            // Page title and summary tiles in the same style as Trucks, Crew and Order History.
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(D(24), D(16), D(24), D(12))
            };

            var pageHeader = PageKit.Header("Special Trip History", "Every special (non-crate) trip, its stops, crew and status. Double-click a row to open it.");
            tripTiles = new TileStrip();
            tAll = tripTiles.Add(new StatTile("All trips", AppTheme.Accent, "") { Clickable = true, Key = "" });
            tPending = tripTiles.Add(new StatTile("Pending", AppTheme.Warning, "not on the road yet") { Clickable = true, Key = "Pending" });
            tTransit = tripTiles.Add(new StatTile("In transit", AppTheme.Info, "on the road") { Clickable = true, Key = "In-Transit" });
            tDone = tripTiles.Add(new StatTile("Completed", AppTheme.Success, "delivered") { Clickable = true, Key = "Completed" });
            tripTiles.FilterChanged += t =>
            {
                // A tile works like the Status filter: pick the matching status there (All trips = all statuses).
                string key = t?.Key as string ?? "";
                int idx = key.Length == 0 ? 0 : cbStatusFilter.Items.IndexOf(key);
                syncingTiles = true;
                cbStatusFilter.SelectedIndex = Math.Max(0, idx);
                syncingTiles = false;
                ApplyFilter();
            };
            var tilesGap = new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = Color.Transparent };
            pnlHeader.Controls.Add(tripTiles);
            pnlHeader.Controls.Add(tilesGap);
            pnlHeader.Controls.Add(pageHeader);
            // Fixed to its content (an auto-sizing panel with a tile row grows to fill the page and hides the list below).
            void FitHeader()
            {
                int w = Math.Max(D(200), pnlHeader.ClientSize.Width - pnlHeader.Padding.Horizontal);
                int h = pnlHeader.Padding.Vertical + pageHeader.GetPreferredSize(new Size(w, 0)).Height + tilesGap.Height + tripTiles.Height;
                if (pnlHeader.Height != h) pnlHeader.Height = h;
            }
            pnlHeader.Resize += (s, e) => FitHeader();
            tripTiles.SizeChanged += (s, e) => FitHeader();
            pnlHeader.HandleCreated += (s, e) => FitHeader();
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = (int)Math.Round(52 * DeviceDpi / 96f),
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(24, 8, 24, 4)
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
                Width = D(260),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "🔍 Search ref, company, driver...",
                Margin = new Padding(0, 2, 10, 0)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbStatusFilter = new ComboBox
            {
                Width = D(160),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 2, 10, 0)
            };
            cbStatusFilter.Items.AddRange(new object[] { "All statuses", "Pending", "In-Transit", "Completed", "Cancelled" });
            cbStatusFilter.SelectedIndex = 0;
            PageKit.StyleCombo(cbStatusFilter);
            cbStatusFilter.SelectedIndexChanged += (s, e) => { if (!syncingTiles) tripTiles?.ClearSelection(); ApplyFilter(); };
            Label lblFrom = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 8, 4, 0), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-60), Margin = new Padding(0, 2, 10, 0) };
            dtpFrom.ValueChanged += (s, e) => ApplyFilter();

            Label lblTo = new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 8, 4, 0), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30), Margin = new Padding(0, 2, 10, 0) };
            dtpTo.ValueChanged += (s, e) => ApplyFilter();

            flowFilters.Controls.Add(txtSearch);
            flowFilters.Controls.Add(cbStatusFilter);
            dateChip = new DateFilterChip(DatePresets.Orders, "All dates") { Margin = new Padding(0, 0, 10, 0) };
            dateChip.RangeChanged += (s, e) => ApplyFilter();
            flowFilters.Controls.Add(dateChip);
            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false
            };

            btnNewSpecialTrip = new Button
            {
                Text = "✨ + New special trip",
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
                    ShowSpecialTripDetails(trip.Id);
                }
            };

            gridSpecialTrips.CellContentClick += GridSpecialTrips_CellContentClick;

            SetupRowMenu();
            pnlGridContainer.Controls.Add(gridSpecialTrips);

            this.Controls.Add(pnlGridContainer);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
        }

        private void SetupGridColumns()
        {
            gridSpecialTrips.Columns.Clear();

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRef",
                HeaderText = "Order Reference",
                Width = D(145),
                DefaultCellStyle = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Pal.C(14, 116, 144) }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDate",
                HeaderText = "Dispatch Date",
                Width = D(105)
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompany",
                HeaderText = "Company",
                Width = D(170)
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAddress",
                HeaderText = "Destination Address",
                Width = D(180),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colServiceType",
                HeaderText = "Service Type",
                Width = D(140),
                Visible = false // Service Type is no longer asked on the form
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTruck",
                HeaderText = "Truck",
                Width = D(110)
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDriver",
                HeaderText = "Driver",
                Width = D(130)
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAide",
                HeaderText = "Aide",
                Width = D(120)
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStops",
                HeaderText = "Stops",
                Width = D(65),
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                Width = D(110),
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) }
            });

            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCharge",
                HeaderText = "Trip Charge",
                Width = D(105),
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMop",
                HeaderText = "Mode of Payment",
                Width = D(125)
            });
            gridSpecialTrips.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPaymentFlags",
                HeaderText = "Checklist",
                Width = D(230),
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True, Font = new Font("Segoe UI Semibold", 8.5F) }
            });

            DataGridViewButtonColumn colBtnView = new DataGridViewButtonColumn
            {
                Name = "colBtnView",
                HeaderText = "",
                Text = "👁️ View",
                UseColumnTextForButtonValue = true,
                Width = D(70),
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnResched = new DataGridViewButtonColumn
            {
                Name = "colBtnResched",
                HeaderText = "",
                Text = "📅 Resched",
                UseColumnTextForButtonValue = true,
                Width = D(85),
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnComplete = new DataGridViewButtonColumn
            {
                Name = "colBtnComplete",
                HeaderText = "",
                Text = "✅ Complete",
                UseColumnTextForButtonValue = true,
                Width = D(90),
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnDel = new DataGridViewButtonColumn
            {
                Name = "colBtnDel",
                HeaderText = "",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = D(45),
                FlatStyle = FlatStyle.Flat
            };

            gridSpecialTrips.Columns.AddRange(colBtnView, colBtnResched, colBtnComplete, colBtnDel);
            UIThemeHelper.FitColumnHeaders(gridSpecialTrips);
        }

        private void LoadData()
        {
            try
            {
                allSpecialTrips = MJdatabase.GetSpecialTrips();
                try { specialLinks = MJdatabase.GetSpecialTripLinks(); } catch (Exception lx) { ErrLog.Swallowed(lx); specialLinks = new Dictionary<int, SpecialTripLinks>(); }
                UpdateKpiCards();
                ApplyFilter();
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[SpecialTripHistory] LoadData error: {ex.Message}"); ErrLog.Swallowed(ex); }
        }

        private void UpdateKpiCards()
        {
            int total = allSpecialTrips.Count;
            int pending = allSpecialTrips.Count(t => t.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            int inTransit = allSpecialTrips.Count(t => t.Status.Equals("In-Transit", StringComparison.OrdinalIgnoreCase));
            int completed = allSpecialTrips.Count(t => t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

            tAll.Value = total.ToString("N0");
            tPending.Value = pending.ToString("N0");
            tTransit.Value = inTransit.ToString("N0");
            tDone.Value = completed.ToString("N0");
        }

        private void ApplyFilter()
        {
            string query = txtSearch.Text.Trim();
            string status = cbStatusFilter.SelectedItem?.ToString() ?? "All statuses";
            var range = dateChip?.Range ?? DateRange.All;
            var filtered = allSpecialTrips.Where(t =>
            {
                if (!range.Contains(t.DispatchDate)) return false;
                if (!status.Equals("All statuses", StringComparison.OrdinalIgnoreCase) &&
                    !t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    bool match = t.OrderReference.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.Company.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.Address.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 (t.DestinationAddress ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 (t.ModeOfPayment ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.DriverName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 t.TruckPlate.Contains(query, StringComparison.OrdinalIgnoreCase);
                    if (!match) return false;
                }

                return true;
            }).OrderByDescending(t => t.Id).ToList();

            PopulateGrid(filtered);
            UpdateSpecialSummary(filtered);
        }
        // ------------------------------------------------------------------ All-in-One Summary (follows search, filters and the clicked trip)
        private AllInOneSummaryBar? specialSummary;
        private List<SpecialTrip> specialSummaryShown = new List<SpecialTrip>();
        private void UpdateSpecialSummary(List<SpecialTrip> shown)
        {
            specialSummaryShown = shown ?? new List<SpecialTrip>();
            RenderSpecialSummary();
        }
        private void RenderSpecialSummary()
        {
            if (gridSpecialTrips?.Parent == null) return;
            try
            {
                if (specialSummary == null)
                {
                    specialSummary = AllInOneSummaryBar.AttachBelow(gridSpecialTrips, "SpecialTrips");
                    gridSpecialTrips.SelectionChanged += (s, e) => RenderSpecialSummary();
                }
                var list = specialSummaryShown;
                bool Cancelled(SpecialTrip t) => RowMenu.Is(t.Status, "Cancelled");
                int waiting = list.Count(t => RowMenu.Is(t.Status, "Pending", "Scheduled", "Assigned"));
                int road = list.Count(t => RowMenu.Is(t.Status, "In Transit", "In-Transit", "Dispatched"));
                int completed = list.Count(t => RowMenu.Is(t.Status, "Completed", "Delivered"));
                int cancelled = list.Count(Cancelled);
                int stops = list.Where(t => !Cancelled(t)).Sum(t => Math.Max(1, t.TotalStopsCount));
                var live = list.Where(t => !Cancelled(t)).ToList();
                int repNeed = live.Count(t => t.WithTripReport), repDone = live.Count(t => t.WithTripReport && LinksOf(t).HasReport);
                int soaNeed = live.Count(t => t.WithSOA), soaDone = live.Count(t => t.WithSOA && LinksOf(t).OnSoa);
                int invNeed = live.Count(t => t.WithServiceInvoice), invDone = live.Count(t => t.WithServiceInvoice && LinksOf(t).InvoiceId > 0);
                int colNeed = live.Count(t => t.WithCollection), colDone = live.Count(t => t.WithCollection && LinksOf(t).IsCollected);
                decimal charges = live.Sum(t => t.TripCharge);
                string Top(IEnumerable<string> xs) => string.Join(", ", xs.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(3).Select(g => $"{g.Key} ({g.Count()})"));
                int trucks = list.Where(t => !Cancelled(t) && t.TruckId > 0).Select(t => t.TruckId).Distinct().Count();
                var m = new List<SummaryMetric>
                {
                    new("Special trips shown", list.Count.ToString("N0"), SummaryTone.Blue, $"of {allSpecialTrips.Count:N0} trips", Strong: true),
                    new("Waiting to leave", waiting.ToString("N0"), SummaryTone.Purple, "pending / scheduled"),
                    new("On the road", road.ToString("N0"), SummaryTone.Amber, "in transit now", Strong: road > 0),
                    new("Completed", completed.ToString("N0"), SummaryTone.Green, "finished trips"),
                    new("Cancelled", cancelled.ToString("N0"), SummaryTone.Red, "not counted below"),
                    new("Stops", stops.ToString("N0"), SummaryTone.Teal, "on the trips shown"),
                    new("Trucks used", trucks.ToString("N0"), SummaryTone.Neutral, "different trucks"),
                    new("Trip charges", AllInOneSummaryBar.Money(charges), SummaryTone.Blue, "on the trips shown"),
                    new("Trip reports", $"{repDone:N0} / {repNeed:N0}", SummaryTone.Teal, repNeed - repDone > 0 ? $"{repNeed - repDone:N0} pending" : "all done", Strong: false),
                    new("On SOA", $"{soaDone:N0} / {soaNeed:N0}", SummaryTone.Purple, soaNeed - soaDone > 0 ? $"{soaNeed - soaDone:N0} still to bill" : "all billed"),
                    new("Service invoices", $"{invDone:N0} / {invNeed:N0}", SummaryTone.Green, invNeed - invDone > 0 ? $"{invNeed - invDone:N0} pending" : "all made"),
                    new("Collections", $"{colDone:N0} / {colNeed:N0}", SummaryTone.Amber, colNeed - colDone > 0 ? $"{colNeed - colDone:N0} still to collect" : "all paid", Strong: colNeed - colDone > 0),
                };
                string dests = Top(live.Select(t => t.DestinationAddress));
                string modes = Top(live.Select(t => t.ModeOfPayment));
                string context = $"Special trips shown: {list.Count:N0} (follows your search and filters)" + (dests.Length > 0 ? $"   ·   Destinations: {dests}" : "") + (modes.Length > 0 ? $"   ·   Payment: {modes}" : "");
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "trip", "trips")}  ·  {road:N0} on the road  ·  {stops:N0} stops";
                var sel = gridSpecialTrips.SelectedRows.Count == 1 ? gridSpecialTrips.SelectedRows[0].Tag as SpecialTrip : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected trip", sel.OrderReference, SummaryTone.Purple, $"{sel.Status} · {sel.Company}", Strong: true));
                    m.Add(new SummaryMetric("Its stops", Math.Max(1, sel.TotalStopsCount).ToString("N0"), SummaryTone.Teal, string.IsNullOrWhiteSpace(sel.DestinationAddress) ? sel.Address : sel.DestinationAddress));
                    m.Add(new SummaryMetric("Truck & driver", string.IsNullOrWhiteSpace(sel.TruckPlate) ? "—" : sel.TruckPlate, SummaryTone.Neutral, string.IsNullOrWhiteSpace(sel.DriverName) ? "no driver yet" : sel.DriverName));
                    m.Add(new SummaryMetric("Its charge", sel.TripCharge > 0 ? AllInOneSummaryBar.Money(sel.TripCharge) : "—", SummaryTone.Blue, string.IsNullOrWhiteSpace(sel.ModeOfPayment) ? "mode of payment not set" : sel.ModeOfPayment));
                    m.Add(new SummaryMetric("Its checklist", ChecklistText(sel), SummaryTone.Teal, string.IsNullOrWhiteSpace(sel.DestinationAddress) ? "no destination yet" : sel.DestinationAddress));
                    context = $"Selected trip {sel.OrderReference} — {sel.Company} · {sel.DispatchDate:MMM d, yyyy} · {sel.Status}   ·   Special trips shown: {list.Count:N0}";
                }
                specialSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        private Dictionary<int, SpecialTripLinks> specialLinks = new Dictionary<int, SpecialTripLinks>();
        private SpecialTripLinks LinksOf(SpecialTrip t) => specialLinks.TryGetValue(t.Id, out var l) ? l : new SpecialTripLinks();
        /// <summary>The ticked checklist items with where each stands, e.g. "Trip Report ✓ · SOA: ready · Invoice ✓ · Collection: pending".</summary>
        private string ChecklistText(SpecialTrip t)
        {
            var l = LinksOf(t);
            var parts = new List<string>();
            if (t.WithTripReport) parts.Add(l.HasReport ? "Trip Report ✓" : "Trip Report: pending");
            if (t.WithSOA) parts.Add(l.OnSoa ? $"SOA ✓ ({l.SoaNumber})" : "SOA: ready to add");
            if (t.WithServiceInvoice) parts.Add(l.InvoiceId > 0 ? $"Invoice ✓ ({l.InvoiceNumber})" : "Invoice: pending");
            if (t.WithCollection) parts.Add(l.IsCollected ? "Collection ✓ paid" : "Collection: pending");
            return parts.Count > 0 ? string.Join(" · ", parts) : "None ticked";
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
                row.Cells["colAddress"].Value = !string.IsNullOrWhiteSpace(t.DestinationAddress) ? t.DestinationAddress : (!string.IsNullOrWhiteSpace(t.Address) ? t.Address + "  (client address — no destination yet)" : "—  no destination yet");
                row.Cells["colCharge"].Value = t.TripCharge > 0 ? "₱" + t.TripCharge.ToString("N2") : "—";
                row.Cells["colMop"].Value = string.IsNullOrWhiteSpace(t.ModeOfPayment) ? "— not set" : t.ModeOfPayment;
                row.Cells["colServiceType"].Value = t.ServiceType;
                row.Cells["colTruck"].Value = !string.IsNullOrWhiteSpace(t.TruckPlate) ? t.TruckPlate : "Unassigned";
                row.Cells["colDriver"].Value = !string.IsNullOrWhiteSpace(t.DriverName) ? t.DriverName : "Unassigned";
                row.Cells["colAide"].Value = !string.IsNullOrWhiteSpace(t.AideName) ? t.AideName : "None";
                row.Cells["colStops"].Value = t.TotalStopsCount.ToString();
                row.Cells["colStatus"].Value = t.Status;

                row.Cells["colPaymentFlags"].Value = ChecklistText(t);
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

            if (e.ColumnIndex == idxView) ShowSpecialTripDetails(trip.Id);
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
                m.Items.Add(RowMenu.Item("Open trip details", () => ShowSpecialTripDetails(t.Id), "Enter"));
                m.Items.Add(RowMenu.Item("Edit trip", () => OpenSpecialTripDialog(t.Id)));
                m.Items.Add(RowMenu.Good("Mark as completed", () => CompleteTrip(t), !done && !cancelled));
                m.Items.Add(RowMenu.Item("Reschedule…", () => RescheduleTrip(t), "", !done && !cancelled));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(t.Company), "", !string.IsNullOrWhiteSpace(t.Company)));
                var lk = LinksOf(t);
                if (t.WithCollection) m.Items.Add(RowMenu.Good(lk.IsCollected ? "Collection: paid ✓" : $"Record collection ({(string.IsNullOrWhiteSpace(t.ModeOfPayment) ? "payment" : t.ModeOfPayment)})…", () => MainForm.Instance?.RecordSpecialTripCollection(t), !lk.IsCollected && !cancelled));
                if (lk.InvoiceId > 0) m.Items.Add(RowMenu.Item($"Show invoice {lk.InvoiceNumber}", () => MainForm.Instance?.ShowFinanceRecords("Billing", lk.InvoiceNumber)));
                if (lk.OnSoa) m.Items.Add(RowMenu.Item($"Show statement {lk.SoaNumber}", () => MainForm.Instance?.ShowFinanceRecords("StatementOfAccount", lk.SoaNumber)));
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
                if (e.KeyCode == Keys.Enter && gridSpecialTrips.CurrentRow?.Tag is SpecialTrip t) { ShowSpecialTripDetails(t.Id); e.Handled = true; e.SuppressKeyPress = true; }
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
                QuietBox.Show($"Special Trip '{trip.OrderReference}' is already marked as Completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            { ErrLog.Error("SpecialTripHistoryView", ex); 
                MessageBox.Show(ex.Message, "Can't remove this trip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Nov 6: OpenSpecialTripDialog now opens the form as a full page (SpecialTripHistoryView.Page.cs).

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (gridSpecialTrips.Rows.Count == 0)
            {
                QuietBox.Show("No special trip records to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        QuietBox.Show("Special Trip history successfully exported to CSV!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    { ErrLog.Error("SpecialTripHistoryView", ex); 
                        MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
