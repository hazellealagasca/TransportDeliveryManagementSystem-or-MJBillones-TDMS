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
    public class CrewTripHistoryRow
    {
        public int DeliveryId { get; set; }
        public int CrewId { get; set; }
        public string CrewCode { get; set; } = string.Empty;
        public string CrewMemberName { get; set; } = string.Empty;
        public string CrewRole { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public DateTime DispatchDate { get; set; }
        public string LoadingScheduleInfo { get; set; } = string.Empty;
        public string TruckInfo { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int GCrate { get; set; }
        public int ECrate { get; set; }
        public int TotalCrates { get; set; }
        public string Status { get; set; } = string.Empty;
        public Delivery DeliveryRef { get; set; } = null!;
        public Crew? CrewRef { get; set; }
    }

    public class CrewTripHistoryView : UserControl, IRefreshable
    {
        private readonly List<CrewTripHistoryRow> allRows = new List<CrewTripHistoryRow>();
        private List<CrewTripHistoryRow> filteredRows = new List<CrewTripHistoryRow>();
        private List<Crew> allCrews = new List<Crew>();
        private List<Delivery> allDeliveries = new List<Delivery>();

        private DataGridView gridHistory = null!;
        private ComboBox cbCrew = null!;
        private ComboBox cbRole = null!;
        private ComboBox cbDateFilter = null!;
        private TextBox txtSearch = null!;
        private StatTile tileTrips = null!, tileCrates = null!, tileDone = null!, tileUpcoming = null!, tileTrucks = null!;
        private TileStrip tiles = null!;
        private PageHeader header = null!;
        private Label lblGridCount = null!;
        private string? statusTileFilter;
        public CrewTripHistoryView()
        {
            InitializeCustomComponents();
            UIThemeHelper.ApplyTheme(this);
            SetupContextMenu();
            _ = LoadDataAsync();
        }

        private int D(int v) => PageKit.D(this, v);
        private void InitializeCustomComponents()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.Canvas;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.Padding = new Padding(D(4), D(8), D(4), D(4));
            // 1. Header: title, description, actions
            header = PageKit.Header("Crew trip history", "Every trip each driver and aide was assigned to — truck, client, crates and status.");
            header.AddActions(
                PageKit.Action("🖨  Print crew dossier", "primary", (s, e) => PrintCrewDossier()),
                PageKit.Action("📄  Delivery dossier", "secondary", (s, e) => PrintSelectedDeliveryDossier()),
                PageKit.Action("↻  Refresh", "secondary", (s, e) => RefreshData()));
            // 2. Summary tiles (the last three are quick filters)
            tiles = new TileStrip();
            tileTrips = tiles.Add(new StatTile("Trips", AppTheme.Info, "dispatches in view"));
            tileCrates = tiles.Add(new StatTile("Crates hauled", AppTheme.Success, "G · E crates"));
            tileDone = tiles.Add(new StatTile("Completed", AppTheme.Success, "click to show only these") { Clickable = true, Key = "done" });
            tileUpcoming = tiles.Add(new StatTile("Scheduled / on the road", AppTheme.Warning, "click to show only these") { Clickable = true, Key = "open" });
            tileTrucks = tiles.Add(new StatTile("Trucks used", AppTheme.Accent, "different trucks"));
            tiles.FilterChanged += t => { statusTileFilter = t?.Key as string; ApplyFilter(); };
            // 3. Filter bar
            var filterCard = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = AppTheme.Surface,
                Padding = new Padding(D(12), D(10), D(12), D(10)),
                Height = D(58)
            };
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(150)));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(160)));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterCard.Paint += (s, e) => PaintCard(filterCard, e);
            cbCrew = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, D(8), 0), DropDownWidth = D(360) };
            cbCrew.SelectedIndexChanged += (s, e) => ApplyFilter();

            cbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, D(8), 0) };
            cbRole.Items.AddRange(new object[] { "All Roles", "Driver", "Aide", "Mechanic", "Fabricator" });
            cbRole.SelectedIndex = 0;
            cbRole.SelectedIndexChanged += (s, e) => ApplyFilter();

            cbDateFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, D(8), 0) };
            cbDateFilter.Items.AddRange(new object[] { "All Time", "Today", "Last 7 Days", "This Month", "Last 30 Days" });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            txtSearch = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, D(1), D(8), 0) };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            foreach (var cb in new[] { cbCrew, cbRole, cbDateFilter }) PageKit.StyleCombo(cb);
            PageKit.StyleSearch(txtSearch, "Search dispatch #, truck, client, destination…");
            var btnReset = new LinkLabel
            {
                Text = "Clear filters",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                LinkColor = AppTheme.AccentText,
                ActiveLinkColor = AppTheme.AccentHover,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Margin = new Padding(D(4), D(6), 0, 0)
            };
            btnReset.LinkClicked += (s, e) =>
            {
                cbCrew.SelectedIndex = 0;
                cbRole.SelectedIndex = 0;
                txtSearch.Clear();
                cbDateFilter.SelectedIndex = 0;
                tiles.ClearSelection();
            };

            filterCard.Controls.Add(cbCrew, 0, 0);
            filterCard.Controls.Add(cbRole, 1, 0);
            filterCard.Controls.Add(cbDateFilter, 2, 0);
            filterCard.Controls.Add(txtSearch, 3, 0);
            filterCard.Controls.Add(btnReset, 4, 0);
            // 4. Table
            var pnlGridCard = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(12), D(8), D(12), D(4)) };
            pnlGridCard.Paint += (s, e) => PaintCard(pnlGridCard, e);
            gridHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            SetupGridColumns();

            gridHistory.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) ShowSelectedDeliveryDetails();
            };

            gridHistory.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ShowSelectedDeliveryDetails(); e.Handled = true; e.SuppressKeyPress = true; } };
            lblGridCount = new Label
            {
                Dock = DockStyle.Bottom,
                Height = D(30),
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            pnlGridCard.Controls.Add(gridHistory);
            pnlGridCard.Controls.Add(lblGridCount);

            // Stack: Fill first, then the Top bars in reverse order.
            this.Controls.Add(pnlGridCard);
            this.Controls.Add(PageKit.Gap(this, 12));
            this.Controls.Add(filterCard);
            this.Controls.Add(PageKit.Gap(this, 12));
            this.Controls.Add(tiles);
            this.Controls.Add(PageKit.Gap(this, 14));
            this.Controls.Add(header);
            this.ResumeLayout(false);
            this.HandleCreated += (s, e) => { tiles.Height = D(96); filterCard.Height = D(58); };
        }

        private static void PaintCard(Control c, PaintEventArgs e)
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        }


        private void SetupGridColumns()
        {
            gridHistory.Columns.Clear();

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CrewCode",
                HeaderText = "Crew ID",
                FillWeight = 7, MinimumWidth = 70,
                DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CrewMemberName",
                HeaderText = "Crew member",
                FillWeight = 14, MinimumWidth = 130
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CrewRole",
                HeaderText = "Role",
                FillWeight = 6, MinimumWidth = 60
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DispatchNumber",
                HeaderText = "Dispatch #",
                FillWeight = 10, MinimumWidth = 95,
                DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DispatchDate",
                HeaderText = "Date",
                FillWeight = 8, MinimumWidth = 85,
                DefaultCellStyle = { Format = "MM/dd/yyyy" }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LoadingScheduleInfo",
                HeaderText = "Loading order",
                FillWeight = 11, MinimumWidth = 100
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TruckInfo",
                HeaderText = "Truck",
                FillWeight = 9, MinimumWidth = 90
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ClientName",
                HeaderText = "Client",
                FillWeight = 16, MinimumWidth = 130
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Destination",
                HeaderText = "Destination / route",
                FillWeight = 13, MinimumWidth = 110
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "GCrate",
                HeaderText = "G",
                FillWeight = 4, MinimumWidth = 45,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ECrate",
                HeaderText = "E",
                FillWeight = 4, MinimumWidth = 45,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalCrates",
                HeaderText = "Total",
                FillWeight = 5, MinimumWidth = 55,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                FillWeight = 9, MinimumWidth = 105,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            gridHistory.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridHistory.Columns[e.ColumnIndex].DataPropertyName == "Status")
                {
                    string status = e.Value?.ToString() ?? "";
                    if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                        e.CellStyle.BackColor = Pal.C(240, 253, 244);
                        e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                    }
                    else if (string.Equals(status, "In Transit", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Pal.C(29, 78, 216);
                        e.CellStyle.BackColor = Pal.C(239, 246, 255);
                        e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                    }
                    else if (string.Equals(status, "Delayed", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Pal.C(185, 28, 28);
                        e.CellStyle.BackColor = Pal.C(254, 242, 242);
                        e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                    }
                }
            };
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridHistory, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not CrewTripHistoryRow row) return;
                m.Items.AddRange(RowMenu.Header($"🚚  {row.DispatchNumber} · {RowMenu.Short(row.CrewMemberName, 24)}", $"{row.CrewRole} · {row.DispatchDate:MMM d, yyyy} · {RowMenu.Short(row.ClientName, 22)} · {row.Status}"));
                m.Items.Add(RowMenu.Item("Open dispatch details", () => ShowSelectedDeliveryDetails()));
                m.Items.Add(RowMenu.Item($"Show only {row.CrewMemberName}'s trips", () => SelectCrew(row.CrewId), "", row.CrewId > 0));
                m.Items.Add(RowMenu.Item("Trip report for this dispatch", () => MainForm.Instance?.OpenTripReportForDispatch(row.DeliveryRef), "", row.DeliveryRef != null && RowMenu.Is(row.Status, "Completed")));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("Crew dossier & trip history", () => PrintCrewDossier()),
                    RowMenu.Item("End-to-end delivery dossier", () => PrintSelectedDeliveryDossier())));
                m.Items.Add(RowMenu.CopySub(this, ("Dispatch number", row.DispatchNumber), ("Crew member", row.CrewMemberName), ("Truck", row.TruckInfo), ("Destination", row.Destination)));
                m.Items.Add(RowMenu.Item("Find this client's trips", () => RowMenu.Search(txtSearch, row.ClientName)));
                m.Items.Add(RowMenu.ExportSub(gridHistory, "Crew_Trip_History", "Crew Trip and Loading Schedule History"));
            });

        }

        public async void RefreshData()
        {
            await LoadDataAsync();
        }

        public async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                allCrews = await MJdatabase.GetCrewsAsync();
                allDeliveries = await MJdatabase.GetDeliveriesAsync();

                PopulateCrewDropdown();
                RebuildHistoryRows();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load crew trip history: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateCrewDropdown()
        {
            int prevCrewId = -1;
            if (cbCrew.SelectedItem is CrewComboItem prevItem)
            {
                prevCrewId = prevItem.CrewId;
            }

            cbCrew.Items.Clear();
            cbCrew.Items.Add(new CrewComboItem { CrewId = -1, DisplayText = "All crew members" });
            foreach (var c in allCrews.OrderBy(x => x.LastName).ThenBy(x => x.FirstName))
            {
                cbCrew.Items.Add(new CrewComboItem
                {
                    CrewId = c.Id,
                    DisplayText = $"{c.FullName}  ·  {c.Role}  ·  {c.CrewCode}"
                });
            }

            int matchIdx = 0;
            if (prevCrewId > 0)
            {
                for (int i = 0; i < cbCrew.Items.Count; i++)
                {
                    if (cbCrew.Items[i] is CrewComboItem item && item.CrewId == prevCrewId)
                    {
                        matchIdx = i;
                        break;
                    }
                }
            }
            cbCrew.SelectedIndex = matchIdx;
        }

        public void SelectCrew(int crewId)
        {
            for (int i = 0; i < cbCrew.Items.Count; i++)
            {
                if (cbCrew.Items[i] is CrewComboItem item && item.CrewId == crewId)
                {
                    cbCrew.SelectedIndex = i;
                    break;
                }
            }
        }

        private void RebuildHistoryRows()
        {
            allRows.Clear();

            var crewMap = allCrews.ToDictionary(c => c.Id);
            var crewByName = allCrews.ToDictionary(c => c.FullName.Trim().ToLowerInvariant(), c => c);

            foreach (var del in allDeliveries)
            {
                var addedCrewIdsForThisDel = new HashSet<int>();

                if (del.DriverId > 0 && crewMap.TryGetValue(del.DriverId, out var driverCrew))
                {
                    addedCrewIdsForThisDel.Add(driverCrew.Id);
                    allRows.Add(CreateRow(del, driverCrew, "Driver"));
                }
                else if (!string.IsNullOrEmpty(del.DriverName))
                {
                    string normName = del.DriverName.Trim().ToLowerInvariant();
                    if (crewByName.TryGetValue(normName, out var matchedDriver))
                    {
                        addedCrewIdsForThisDel.Add(matchedDriver.Id);
                        allRows.Add(CreateRow(del, matchedDriver, "Driver"));
                    }
                }

                if (del.AideId > 0 && crewMap.TryGetValue(del.AideId, out var directAide) && !addedCrewIdsForThisDel.Contains(directAide.Id))
                {
                    addedCrewIdsForThisDel.Add(directAide.Id);
                    allRows.Add(CreateRow(del, directAide, "Aide"));
                }

                if (!string.IsNullOrEmpty(del.AideIds))
                {
                    var ids = del.AideIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var sId in ids)
                    {
                        if (int.TryParse(sId, out int aId) && crewMap.TryGetValue(aId, out var multiAide) && !addedCrewIdsForThisDel.Contains(multiAide.Id))
                        {
                            addedCrewIdsForThisDel.Add(multiAide.Id);
                            allRows.Add(CreateRow(del, multiAide, "Aide"));
                        }
                    }
                }

                if (addedCrewIdsForThisDel.Count == 0 && !string.IsNullOrEmpty(del.AideNames))
                {
                    var names = del.AideNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var n in names)
                    {
                        string normAideName = n.Trim().ToLowerInvariant();
                        if (crewByName.TryGetValue(normAideName, out var namedAide) && !addedCrewIdsForThisDel.Contains(namedAide.Id))
                        {
                            addedCrewIdsForThisDel.Add(namedAide.Id);
                            allRows.Add(CreateRow(del, namedAide, "Aide"));
                        }
                    }
                }
            }

            allRows.Sort((a, b) =>
            {
                int c = b.DispatchDate.CompareTo(a.DispatchDate);
                if (c != 0) return c;
                return b.DeliveryId.CompareTo(a.DeliveryId);
            });
        }

        private CrewTripHistoryRow CreateRow(Delivery del, Crew crew, string tripRole)
        {
            return new CrewTripHistoryRow
            {
                DeliveryId = del.Id,
                CrewId = crew.Id,
                CrewCode = crew.CrewCode,
                CrewMemberName = crew.FullName,
                CrewRole = tripRole,
                DispatchNumber = del.DispatchNumber,
                DispatchDate = del.DispatchDate,
                LoadingScheduleInfo = !string.IsNullOrEmpty(del.LoadingScheduleInfo) ? del.LoadingScheduleInfo : "Direct Dispatch",
                TruckInfo = del.TruckInfo,
                ClientName = del.ClientName,
                Destination = del.Destination,
                GCrate = del.GCrate,
                ECrate = del.ECrate,
                TotalCrates = del.TotalCrates,
                Status = del.Status,
                DeliveryRef = del,
                CrewRef = crew
            };
        }

        private void ApplyFilter()
        {
            int selectedCrewId = -1;
            if (cbCrew.SelectedItem is CrewComboItem cItem)
            {
                selectedCrewId = cItem.CrewId;
            }

            string roleFilter = cbRole.SelectedItem?.ToString() ?? "All Roles";
            string dateFilter = cbDateFilter.SelectedItem?.ToString() ?? "All Time";
            string q = txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            DateTime now = DateTime.Today;

            var filtered = allRows.AsEnumerable();

            if (selectedCrewId > 0)
            {
                filtered = filtered.Where(r => r.CrewId == selectedCrewId);
            }

            if (roleFilter != "All Roles")
            {
                filtered = filtered.Where(r =>
                    r.CrewRole.IndexOf(roleFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (r.CrewRef != null && r.CrewRef.Role.IndexOf(roleFilter, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (!string.IsNullOrEmpty(q))
            {
                filtered = filtered.Where(r =>
                    r.CrewCode.ToLowerInvariant().Contains(q) ||
                    r.CrewMemberName.ToLowerInvariant().Contains(q) ||
                    r.DispatchNumber.ToLowerInvariant().Contains(q) ||
                    r.LoadingScheduleInfo.ToLowerInvariant().Contains(q) ||
                    r.TruckInfo.ToLowerInvariant().Contains(q) ||
                    r.ClientName.ToLowerInvariant().Contains(q) ||
                    r.Destination.ToLowerInvariant().Contains(q) ||
                    r.Status.ToLowerInvariant().Contains(q));
            }

            if (dateFilter == "Today")
            {
                filtered = filtered.Where(r => r.DispatchDate.Date == now);
            }
            else if (dateFilter == "Last 7 Days")
            {
                DateTime cutoff = now.AddDays(-7);
                filtered = filtered.Where(r => r.DispatchDate.Date >= cutoff);
            }
            else if (dateFilter == "This Month")
            {
                filtered = filtered.Where(r => r.DispatchDate.Year == now.Year && r.DispatchDate.Month == now.Month);
            }
            else if (dateFilter == "Last 30 Days")
            {
                DateTime cutoff = now.AddDays(-30);
                filtered = filtered.Where(r => r.DispatchDate.Date >= cutoff);
            }

            // Tiles count what the other filters left, before the tile filter itself
            var beforeTile = filtered.ToList();
            bool IsDone(CrewTripHistoryRow r) => DispatchGate.IsCompleted(r.Status);
            bool IsOpen(CrewTripHistoryRow r) => !IsDone(r) && !DispatchGate.IsCancelled(r.Status);
            if (statusTileFilter == "done") filtered = beforeTile.Where(IsDone);
            else if (statusTileFilter == "open") filtered = beforeTile.Where(IsOpen);
            filteredRows = filtered.ToList();
            gridHistory.DataSource = filteredRows;

            // One dispatch has several crew rows (driver + aides): count trips and crates once per dispatch.
            var trips = filteredRows.GroupBy(r => r.DeliveryId).Select(g => g.First()).ToList();
            tileTrips.Value = trips.Count.ToString("N0");
            tileTrips.Note = filteredRows.Count == trips.Count ? "dispatches in view" : $"{filteredRows.Count:N0} crew assignments";
            tileCrates.Value = trips.Sum(r => r.TotalCrates).ToString("N0");
            tileCrates.Note = $"G {trips.Sum(r => r.GCrate):N0}  ·  E {trips.Sum(r => r.ECrate):N0}";
            tileDone.Value = beforeTile.Where(IsDone).Select(r => r.DeliveryId).Distinct().Count().ToString("N0");
            tileUpcoming.Value = beforeTile.Where(IsOpen).Select(r => r.DeliveryId).Distinct().Count().ToString("N0");
            tileTrucks.Value = trips.Select(r => r.TruckInfo).Where(t => !string.IsNullOrEmpty(t)).Distinct().Count().ToString("N0");
            string who = cbCrew.SelectedItem is CrewComboItem ci && ci.CrewId > 0 ? $" for {ci.DisplayText}" : "";
            lblGridCount.Text = $"Showing {filteredRows.Count:N0} of {allRows.Count:N0} assignments{who}  ·  double-click or press Enter to open a dispatch, right-click for more";
        }

        private CrewTripHistoryRow? GetSelectedRow()
        {
            if (gridHistory.CurrentRow != null && gridHistory.CurrentRow.DataBoundItem is CrewTripHistoryRow row)
            {
                return row;
            }
            return null;
        }

        private void ShowSelectedDeliveryDetails()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Please select a trip history record first.", "Select Trip", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RecordDetailsDialog.ShowDetails(this, row.DeliveryRef);
        }

        private void PrintCrewDossier()
        {
            Crew? targetCrew = null;
            if (cbCrew.SelectedItem is CrewComboItem item && item.CrewId > 0)
            {
                targetCrew = allCrews.FirstOrDefault(c => c.Id == item.CrewId);
            }
            else
            {
                var row = GetSelectedRow();
                if (row != null && row.CrewRef != null)
                {
                    targetCrew = row.CrewRef;
                }
            }

            if (targetCrew == null)
            {
                MessageBox.Show("Please select a specific crew member from the dropdown or click a row in the grid first.", "Select Crew Member", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var crewDeliveries = allRows.Where(r => r.CrewId == targetCrew.Id).Select(r => r.DeliveryRef).Distinct().ToList();
            PrintHelper.PrintCrewDossierAndHistory(targetCrew, crewDeliveries);
        }

        private void PrintSelectedDeliveryDossier()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Please select a trip record from the grid first.", "Select Trip", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintEndToEndDeliveryDossier(row.DeliveryRef.Id, row.DeliveryRef.DispatchNumber);
        }

        private class CrewComboItem
        {
            public int CrewId { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public override string ToString() => DisplayText;
        }
    }
}
