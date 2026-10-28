using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class SpecialTripFormDialog : Form
    {
        public int SavedSpecialTripId { get; private set; } = 0;
        public bool IsSaved { get; private set; } = false;

        private int currentTripId = 0;
        private SpecialTrip? loadedTrip = null;

        private Panel pnlHeader = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblFlow = null!;

        private TextBox txtOrderRef = null!;
        private DateTimePicker dtpDispatchDate = null!;
        private ComboBox cbCompany = null!;
        private ComboBox cbDepartureTime = null!;
        private TextBox txtAddress = null!;

        private Label lblTableTitle = null!;
        private Label lblTableDesc = null!;
        private DataGridView gridDeliveryDetails = null!;
        private Button btnAddItem = null!;
        private Button btnEditTable = null!;
        private List<SpecialTripColumnDef> currentColumns = new List<SpecialTripColumnDef>();

        private ComboBox cbServiceType = null!;
        private ComboBox cbStatus = null!;
        private CheckBox chkPaymentWithReport = null!;
        private CheckBox chkWithSOA = null!;
        private CheckBox chkWithInvoice = null!;
        private CheckBox chkWithCollection = null!;

        private ComboBox cbTruck = null!;
        private ComboBox cbDriver = null!;
        private ComboBox cbAide = null!;

        private Button btnSave = null!;
        private Button btnUpdate = null!;
        private Button btnClear = null!;
        private Button btnClose = null!;
        private Button btnReschedule = null!;

        private List<Truck> availableTrucks = new List<Truck>();
        private List<Crew> availableDrivers = new List<Crew>();
        private List<Crew> availableAides = new List<Crew>();
        private List<ClientCompany> availableClients = new List<ClientCompany>();

        public SpecialTripFormDialog(int specialTripId = 0)
        {
            this.currentTripId = specialTripId;
            this.currentColumns = SpecialTripTableEditorDialog.GetDefaultColumns();
            InitializeCustomComponent();
            LoadDropdownResources();
            if (specialTripId > 0)
            {
                LoadExistingTrip(specialTripId);
            }
            else
            {
                ResetFormForNew();
            }
            UIThemeHelper.ApplyTheme(this);
            RestoreCustomFormStyling();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "Special Trip Form";
            this.Size = new Size(1020, 880);
            this.MinimumSize = new Size(940, 740);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(24, 12, 24, 12)
            };

            lblTitle = new Label
            {
                Text = "Special Trip Form",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Pal.White,
                AutoSize = true,
                Location = new Point(24, 10)
            };

            lblSubtitle = new Label
            {
                Text = "Direct on-demand delivery configuration, resource assignment, and itemized multi-stop routing",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(148, 163, 184),
                AutoSize = true,
                Location = new Point(25, 38)
            };

            Panel pnlBreadcrumb = new Panel
            {
                Location = new Point(24, 58),
                Size = new Size(930, 22),
                BackColor = Color.Transparent
            };

            lblFlow = new Label
            {
                Text = "Create Special Trip  ➔  Type Date  ➔  Assign Truck & Driver  ➔  Assign Route & Items  ➔  Complete -> Save to History",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Pal.C(224, 242, 254),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            pnlBreadcrumb.Controls.Add(lblFlow);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(pnlBreadcrumb);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Pal.White,
                Padding = new Padding(24, 12, 24, 12)
            };

            FlowLayoutPanel flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            btnReschedule = new Button
            {
                Text = "📅 Reschedule",
                BackColor = Pal.C(234, 88, 12),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(130, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                Visible = false
            };
            btnReschedule.FlatAppearance.BorderSize = 0;
            btnReschedule.Click += BtnReschedule_Click;

            btnSave = new Button
            {
                Text = "💾 Save",
                BackColor = Pal.C(14, 116, 144),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnUpdate = new Button
            {
                Text = "🔄 Update",
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                Visible = false
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            btnClear = new Button
            {
                Text = "🧹 Clear",
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(51, 65, 85),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(100, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnClear.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClear.Click += (s, e) => ResetFormForNew();

            btnClose = new Button
            {
                Text = "✕ Close",
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(95, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnClose.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClose.Click += (s, e) => this.Close();

            flowActions.Controls.Add(btnSave);
            flowActions.Controls.Add(btnUpdate);
            flowActions.Controls.Add(btnReschedule);
            flowActions.Controls.Add(btnClear);
            flowActions.Controls.Add(btnClose);
            pnlFooter.Controls.Add(flowActions);

            Panel pnlScrollBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            Panel pnlCard = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Pal.White,
                Padding = new Padding(24)
            };

            TableLayoutPanel tblHeaderFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 3,
                Height = 195,
                Padding = new Padding(0, 0, 0, 10)
            };
            tblHeaderFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblHeaderFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Panel pnlOrderRef = CreateFieldBlock("Order Reference / Dispatch", out txtOrderRef);
            txtOrderRef.ReadOnly = true;
            txtOrderRef.BackColor = Pal.C(241, 245, 249);
            tblHeaderFields.Controls.Add(pnlOrderRef, 0, 0);

            Panel pnlDate = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 10) };
            Label lblDate = new Label { Text = "Dispatch Date", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Dock = DockStyle.Top, Height = 20 };
            dtpDispatchDate = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5F), Height = 30 };
            pnlDate.Controls.Add(dtpDispatchDate);
            pnlDate.Controls.Add(lblDate);
            tblHeaderFields.Controls.Add(pnlDate, 1, 0);

            Panel pnlCompany = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 10) };
            Label lblComp = new Label { Text = "Company", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Dock = DockStyle.Top, Height = 20 };
            cbCompany = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 9.5F), Height = 30 };
            cbCompany.SelectedIndexChanged += CbCompany_SelectedIndexChanged;
            cbCompany.TextChanged += CbCompany_TextChanged;
            pnlCompany.Controls.Add(cbCompany);
            pnlCompany.Controls.Add(lblComp);
            tblHeaderFields.Controls.Add(pnlCompany, 0, 1);

            Panel pnlDeparture = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 10) };
            Label lblDep = new Label { Text = "Departure Time", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Dock = DockStyle.Top, Height = 20 };
            cbDepartureTime = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 9.5F), Height = 30 };
            string[] times = { "06:00 AM", "07:00 AM", "08:00 AM", "09:00 AM", "10:00 AM", "11:00 AM", "12:00 PM", "01:00 PM", "02:00 PM", "03:00 PM", "04:00 PM", "05:00 PM", "06:00 PM", "08:00 PM", "10:00 PM" };
            cbDepartureTime.Items.AddRange(times);
            cbDepartureTime.Text = "08:00 AM";
            pnlDeparture.Controls.Add(cbDepartureTime);
            pnlDeparture.Controls.Add(lblDep);
            tblHeaderFields.Controls.Add(pnlDeparture, 1, 1);

            Panel pnlAddr = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
            Label lblAddr = new Label { Text = "Address", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Dock = DockStyle.Top, Height = 20 };
            txtAddress = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9.5F), Height = 30 };
            pnlAddr.Controls.Add(txtAddress);
            pnlAddr.Controls.Add(lblAddr);
            tblHeaderFields.Controls.Add(pnlAddr, 0, 2);
            tblHeaderFields.SetColumnSpan(pnlAddr, 2);

            Panel pnlTableSection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 250,
                Padding = new Padding(0, 10, 0, 10)
            };

            Panel pnlTableToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38
            };

            lblTableTitle = new Label
            {
                Text = "Delivery Details",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 8),
                Cursor = Cursors.Hand
            };
            lblTableTitle.DoubleClick += BtnEditTable_Click;

            lblTableDesc = new Label
            {
                Text = "add a line or row and column",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Pal.C(100, 116, 139),
                AutoSize = true,
                Location = new Point(125, 10),
                Cursor = Cursors.Hand
            };
            lblTableDesc.DoubleClick += BtnEditTable_Click;

            FlowLayoutPanel flowTableBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            btnAddItem = new Button
            {
                Text = "➕ Add",
                BackColor = Pal.C(14, 116, 144),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += BtnAddItem_Click;

            btnEditTable = new Button
            {
                Text = "✎ Edit",
                BackColor = Pal.C(22, 163, 74),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnEditTable.FlatAppearance.BorderSize = 0;
            btnEditTable.Click += BtnEditTable_Click;

            flowTableBtns.Controls.Add(btnAddItem);
            flowTableBtns.Controls.Add(btnEditTable);

            pnlTableToolbar.Controls.Add(lblTableTitle);
            pnlTableToolbar.Controls.Add(lblTableDesc);
            pnlTableToolbar.Controls.Add(flowTableBtns);

            gridDeliveryDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 30 }
            };

            gridDeliveryDetails.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridDeliveryDetails.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 41, 59);
            gridDeliveryDetails.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gridDeliveryDetails.DefaultCellStyle.SelectionBackColor = Pal.C(224, 242, 254);
            gridDeliveryDetails.DefaultCellStyle.SelectionForeColor = Pal.C(15, 23, 42);
            RebuildGridColumns();
            gridDeliveryDetails.CellContentClick += GridDeliveryDetails_CellContentClick;
            gridDeliveryDetails.ColumnHeaderMouseDoubleClick += (s, e) => BtnEditTable_Click(s, e);

            pnlTableSection.Controls.Add(gridDeliveryDetails);
            pnlTableSection.Controls.Add(pnlTableToolbar);

            TableLayoutPanel tblBottomSections = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 240,
                Padding = new Padding(0, 10, 0, 0)
            };
            tblBottomSections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblBottomSections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Panel pnlLeftBlock = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };

            Label lblSvc = new Label { Text = "Service Type", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            cbServiceType = new ComboBox { Location = new Point(0, 20), Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbServiceType.Items.AddRange(new object[] { "Dedicated / Special Delivery", "Non-Crate / Specialized Cargo", "Bulk / Boxed Goods", "Equipment / Oversized Cargo", "Fragile / High-Value Cargo", "Hotshot / Urgent Delivery", "Inter-Branch Transfer", "Event / Urgent Dispatch", "Return Trip" });
            cbServiceType.SelectedIndex = 0;

            Label lblStat = new Label { Text = "Status", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Location = new Point(0, 55), AutoSize = true };
            cbStatus = new ComboBox { Location = new Point(0, 75), Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cbStatus.Items.AddRange(new object[] { "Pending", "In-Transit", "Completed", "Cancelled" });
            cbStatus.SelectedIndex = 0;

            Panel pnlPaymentBox = new Panel
            {
                Location = new Point(0, 112),
                Size = new Size(420, 120),
                BackColor = Pal.C(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12, 8, 12, 8)
            };

            chkPaymentWithReport = new CheckBox { Text = "Mode of payment with my report", Checked = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Pal.C(15, 23, 42), Location = new Point(12, 10), AutoSize = true };
            chkWithSOA = new CheckBox { Text = "With SOA", Font = new Font("Segoe UI", 9F), ForeColor = Pal.C(51, 65, 85), Location = new Point(12, 35), AutoSize = true };
            chkWithInvoice = new CheckBox { Text = "With Service Invoice", Font = new Font("Segoe UI", 9F), ForeColor = Pal.C(51, 65, 85), Location = new Point(12, 60), AutoSize = true };
            chkWithCollection = new CheckBox { Text = "With collection", Font = new Font("Segoe UI", 9F), ForeColor = Pal.C(51, 65, 85), Location = new Point(12, 85), AutoSize = true };
            pnlPaymentBox.Controls.Add(chkPaymentWithReport);
            pnlPaymentBox.Controls.Add(chkWithSOA);
            pnlPaymentBox.Controls.Add(chkWithInvoice);
            pnlPaymentBox.Controls.Add(chkWithCollection);

            pnlLeftBlock.Controls.Add(lblSvc);
            pnlLeftBlock.Controls.Add(cbServiceType);
            pnlLeftBlock.Controls.Add(lblStat);
            pnlLeftBlock.Controls.Add(cbStatus);
            pnlLeftBlock.Controls.Add(pnlPaymentBox);
            tblBottomSections.Controls.Add(pnlLeftBlock, 0, 0);

            Panel pnlRightBlock = new Panel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0) };

            Label lblTrk = new Label { Text = "Assign Truck", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Location = new Point(0, 0), AutoSize = true };
            cbTruck = new ComboBox { Location = new Point(0, 20), Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };

            Label lblDrv = new Label { Text = "Assign Driver", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Location = new Point(0, 65), AutoSize = true };
            cbDriver = new ComboBox { Location = new Point(0, 85), Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };

            Label lblAid = new Label { Text = "Assign Aide", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Location = new Point(0, 130), AutoSize = true };
            cbAide = new ComboBox { Location = new Point(0, 150), Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };

            pnlRightBlock.Controls.Add(lblTrk);
            pnlRightBlock.Controls.Add(cbTruck);
            pnlRightBlock.Controls.Add(lblDrv);
            pnlRightBlock.Controls.Add(cbDriver);
            pnlRightBlock.Controls.Add(lblAid);
            pnlRightBlock.Controls.Add(cbAide);
            tblBottomSections.Controls.Add(pnlRightBlock, 1, 0);

            pnlCard.Controls.Add(tblBottomSections);
            pnlCard.Controls.Add(pnlTableSection);
            pnlCard.Controls.Add(tblHeaderFields);

            pnlScrollBody.Controls.Add(pnlCard);

            this.Controls.Add(pnlScrollBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
            pnlScrollBody.BringToFront();
        }

        private void RestoreCustomFormStyling()
        {
            pnlHeader.BackColor = Pal.C(15, 23, 42);
            lblTitle.ForeColor = Pal.White;
            lblSubtitle.ForeColor = Pal.C(148, 163, 184);
            lblFlow.ForeColor = Pal.C(224, 242, 254);
            btnAddItem.BackColor = Pal.C(14, 116, 144);
            btnAddItem.ForeColor = Pal.White;
            btnEditTable.BackColor = Pal.C(22, 163, 74);
            btnEditTable.ForeColor = Pal.White;
            btnSave.BackColor = Pal.C(14, 116, 144);
            btnSave.ForeColor = Pal.White;
            btnUpdate.BackColor = Pal.C(30, 41, 59);
            btnUpdate.ForeColor = Pal.White;
            btnReschedule.BackColor = Pal.C(234, 88, 12);
            btnReschedule.ForeColor = Pal.White;
            UpdateTableTitleAndDescPositions();
        }

        private void UpdateTableTitleAndDescPositions()
        {
            lblTableTitle.Location = new Point(0, 8);
            lblTableDesc.Location = new Point(lblTableTitle.Right + 12, 10);
        }

        private void RebuildGridColumns()
        {
            gridDeliveryDetails.Columns.Clear();

            DataGridViewTextBoxColumn colSeq = new DataGridViewTextBoxColumn
            {
                Name = "colSeq",
                HeaderText = "#",
                Width = 40,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            gridDeliveryDetails.Columns.Add(colSeq);

            if (currentColumns == null || currentColumns.Count == 0)
            {
                currentColumns = SpecialTripTableEditorDialog.GetDefaultColumns();
            }

            foreach (var colDef in currentColumns)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = colDef.Key,
                    HeaderText = colDef.HeaderText,
                    Width = colDef.Width > 50 ? colDef.Width : 150
                };
                if (colDef.IsFill)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.MinimumWidth = 140;
                }
                gridDeliveryDetails.Columns.Add(col);
            }

            DataGridViewButtonColumn colDel = new DataGridViewButtonColumn
            {
                Name = "colDel",
                HeaderText = "",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = 45,
                FlatStyle = FlatStyle.Flat
            };
            colDel.DefaultCellStyle.ForeColor = Pal.C(220, 38, 38);
            colDel.DefaultCellStyle.BackColor = Pal.C(254, 242, 242);
            gridDeliveryDetails.Columns.Add(colDel);
        }

        private List<Dictionary<string, string>> CaptureGridRows()
        {
            gridDeliveryDetails.EndEdit();
            var rows = new List<Dictionary<string, string>>();
            foreach (DataGridViewRow r in gridDeliveryDetails.Rows)
            {
                if (r.IsNewRow) continue;
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (r.Tag is Dictionary<string, string> savedDict)
                {
                    foreach (var kvp in savedDict)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
                foreach (var colDef in currentColumns)
                {
                    if (gridDeliveryDetails.Columns.Contains(colDef.Key))
                    {
                        dict[colDef.Key] = r.Cells[colDef.Key].Value?.ToString() ?? string.Empty;
                    }
                }
                rows.Add(dict);
            }
            return rows;
        }

        private void PopulateGridRows(List<Dictionary<string, string>> rows)
        {
            gridDeliveryDetails.Rows.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var dict = rows[i];
                int rIdx = gridDeliveryDetails.Rows.Add();
                var row = gridDeliveryDetails.Rows[rIdx];
                row.Tag = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                row.Cells["colSeq"].Value = (i + 1).ToString();

                foreach (var colDef in currentColumns)
                {
                    if (gridDeliveryDetails.Columns.Contains(colDef.Key))
                    {
                        row.Cells[colDef.Key].Value = dict.TryGetValue(colDef.Key, out string? v) ? v : string.Empty;
                    }
                }
            }
            RenumberStops();
        }

        private Panel CreateFieldBlock(string labelText, out TextBox field)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 10) };
            Label l = new Label { Text = labelText, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105), Dock = DockStyle.Top, Height = 20 };
            field = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9.5F), Height = 30 };
            p.Controls.Add(field);
            p.Controls.Add(l);
            return p;
        }

        private void LoadDropdownResources()
        {
            try
            {
                availableClients = MJdatabase.GetClients();
                cbCompany.Items.Clear();
                var registeredCompanyNames = availableClients
                    .Select(c => c.CompanyName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
                cbCompany.Items.AddRange(registeredCompanyNames.ToArray());

                availableTrucks = MJdatabase.GetTrucks().Where(t => t.Status == "Active" || t.Status == "Available").ToList();
                cbTruck.Items.Clear();
                cbTruck.Items.Add("-- Select Truck --");
                foreach (var trk in availableTrucks)
                {
                    cbTruck.Items.Add($"{trk.PlateNumber} ({trk.TruckType} - {trk.CapacityCrates} Crates)");
                }
                cbTruck.SelectedIndex = 0;

                var allCrew = MJdatabase.GetCrews().Where(c => c.IsActive).ToList();
                availableDrivers = allCrew.Where(c => c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase)).ToList();
                availableAides = allCrew.Where(c => !c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase)).ToList();

                cbDriver.Items.Clear();
                cbDriver.Items.Add("-- Select Driver --");
                foreach (var d in availableDrivers)
                {
                    cbDriver.Items.Add(d.FullName);
                }
                cbDriver.SelectedIndex = 0;

                cbAide.Items.Clear();
                cbAide.Items.Add("-- Select Aide / Helper --");
                foreach (var a in availableAides)
                {
                    cbAide.Items.Add(a.FullName);
                }
                cbAide.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialTripDialog] LoadDropdownResources error: {ex.Message}");
            }
        }

        private void CbCompany_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyClientAddress(cbCompany.Text.Trim());
        }

        private void CbCompany_TextChanged(object? sender, EventArgs e)
        {
            ApplyClientAddress(cbCompany.Text.Trim());
        }

        private void ApplyClientAddress(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return;
            var matched = availableClients.FirstOrDefault(c => c.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase));
            if (matched != null && !string.IsNullOrWhiteSpace(matched.Address))
            {
                if (string.IsNullOrWhiteSpace(txtAddress.Text) || txtAddress.Text.StartsWith("Auto:"))
                {
                    txtAddress.Text = matched.Address;
                }
            }
        }
        /// <summary>Starts a new Special Trip with what is already known (from a dispatch or loading order that turned out to be non-crate).</summary>
        public void Prefill(string? company, DateTime date, string? serviceType, string? note)
        {
            if (currentTripId > 0) return;
            if (!string.IsNullOrWhiteSpace(company)) cbCompany.Text = company.Trim();
            dtpDispatchDate.Value = date < dtpDispatchDate.MinDate ? DateTime.Today : date;
            if (!string.IsNullOrWhiteSpace(serviceType))
            {
                int i = cbServiceType.FindStringExact(serviceType);
                if (i >= 0) cbServiceType.SelectedIndex = i;
            }
        }

        private void ResetFormForNew()
        {
            currentTripId = 0;
            loadedTrip = null;
            txtOrderRef.Text = MJdatabase.GenerateNextSpecialTripReference();
            dtpDispatchDate.Value = DateTime.Now;
            cbCompany.Text = string.Empty;
            txtAddress.Text = string.Empty;
            cbDepartureTime.Text = "08:00 AM";
            cbServiceType.SelectedIndex = 0;
            cbStatus.SelectedIndex = 0;

            chkPaymentWithReport.Checked = true;
            chkWithSOA.Checked = false;
            chkWithInvoice.Checked = false;
            chkWithCollection.Checked = false;

            cbTruck.SelectedIndex = 0;
            cbDriver.SelectedIndex = 0;
            cbAide.SelectedIndex = 0;

            lblTableTitle.Text = "Delivery Details";
            lblTableDesc.Text = "add a line or row and column";
            UpdateTableTitleAndDescPositions();

            currentColumns = SpecialTripTableEditorDialog.GetDefaultColumns();
            RebuildGridColumns();
            AddInitialDefaultStop();

            btnUpdate.Visible = false;
            btnReschedule.Visible = false;
            btnSave.Text = "💾 Save";
        }

        private void BtnReschedule_Click(object? sender, EventArgs e)
        {
            if (currentTripId <= 0)
            {
                MessageBox.Show("Please save or open a valid Special Trip record before rescheduling.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var trip = MJdatabase.GetSpecialTripById(currentTripId);
            if (trip == null)
            {
                MessageBox.Show("Special Trip record could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (trip.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' has already been completed and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (trip.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' is cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpDispatchDate.Value >= dtpDispatchDate.MinDate && dtpDispatchDate.Value <= dtpDispatchDate.MaxDate)
            {
                trip.DispatchDate = dtpDispatchDate.Value.Date;
            }
            if (!string.IsNullOrWhiteSpace(cbDepartureTime.Text))
            {
                trip.DepartureTime = cbDepartureTime.Text.Trim();
            }
            if (cbTruck.SelectedIndex > 0 && cbTruck.SelectedIndex - 1 < availableTrucks.Count)
            {
                var trk = availableTrucks[cbTruck.SelectedIndex - 1];
                trip.TruckId = trk.Id;
                trip.TruckPlate = trk.PlateNumber;
            }
            if (cbDriver.SelectedIndex > 0 && cbDriver.SelectedIndex - 1 < availableDrivers.Count)
            {
                var drv = availableDrivers[cbDriver.SelectedIndex - 1];
                trip.DriverId = drv.Id;
                trip.DriverName = drv.FullName;
            }
            if (cbAide.SelectedIndex > 0 && cbAide.SelectedIndex - 1 < availableAides.Count)
            {
                var aid = availableAides[cbAide.SelectedIndex - 1];
                trip.AideId = aid.Id;
                trip.AideName = aid.FullName;
            }

            using (var dlg = new RescheduleDeliveryDialog(trip))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadExistingTrip(trip.Id);
                    cbStatus.Text = "Rescheduled";
                }
            }
        }

        private void AddInitialDefaultStop()
        {
            int rowIdx = gridDeliveryDetails.Rows.Add();
            var row = gridDeliveryDetails.Rows[rowIdx];
            row.Cells["colSeq"].Value = "1";
            if (gridDeliveryDetails.Columns.Contains("colStdId")) row.Cells["colStdId"].Value = "ST-001";
            if (gridDeliveryDetails.Columns.Contains("colDetails")) row.Cells["colDetails"].Value = "Initial Stop Delivery";
            if (gridDeliveryDetails.Columns.Contains("colRoute")) row.Cells["colRoute"].Value = "Direct Special Route";
            if (gridDeliveryDetails.Columns.Contains("colDepTime")) row.Cells["colDepTime"].Value = cbDepartureTime.Text;
            if (gridDeliveryDetails.Columns.Contains("colRemarks")) row.Cells["colRemarks"].Value = string.Empty;
        }

        private void BtnAddItem_Click(object? sender, EventArgs e)
        {
            gridDeliveryDetails.EndEdit();
            int nextSeq = gridDeliveryDetails.Rows.Count + 1;
            int rowIdx = gridDeliveryDetails.Rows.Add();
            var row = gridDeliveryDetails.Rows[rowIdx];
            row.Cells["colSeq"].Value = nextSeq.ToString();
            if (gridDeliveryDetails.Columns.Contains("colStdId")) row.Cells["colStdId"].Value = $"ST-{nextSeq:D3}";
            if (gridDeliveryDetails.Columns.Contains("colDetails")) row.Cells["colDetails"].Value = $"Stop {nextSeq} Delivery";
            if (gridDeliveryDetails.Columns.Contains("colRoute")) row.Cells["colRoute"].Value = "Standard Route Stop";
            if (gridDeliveryDetails.Columns.Contains("colDepTime")) row.Cells["colDepTime"].Value = cbDepartureTime.Text;
            if (gridDeliveryDetails.Columns.Contains("colRemarks")) row.Cells["colRemarks"].Value = string.Empty;
            RenumberStops();
        }

        private void BtnEditTable_Click(object? sender, EventArgs e)
        {
            var rowsSnapshot = CaptureGridRows();
            using (var dlg = new SpecialTripTableEditorDialog(
                lblTableTitle.Text,
                lblTableDesc.Text,
                currentColumns,
                rowsSnapshot,
                cbDepartureTime.Text))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    lblTableTitle.Text = dlg.EditedTableTitle;
                    lblTableDesc.Text = dlg.EditedTableDescription;
                    UpdateTableTitleAndDescPositions();

                    currentColumns = dlg.EditedColumns.Select(c => c.Clone()).ToList();
                    RebuildGridColumns();
                    PopulateGridRows(dlg.EditedRows);
                }
            }
        }

        private void GridDeliveryDetails_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            int delColIdx = gridDeliveryDetails.Columns["colDel"]?.Index ?? -1;
            if (e.RowIndex >= 0 && delColIdx >= 0 && e.ColumnIndex == delColIdx)
            {
                gridDeliveryDetails.Rows.RemoveAt(e.RowIndex);
                RenumberStops();
            }
        }

        private void RenumberStops()
        {
            for (int i = 0; i < gridDeliveryDetails.Rows.Count; i++)
            {
                gridDeliveryDetails.Rows[i].Cells["colSeq"].Value = (i + 1).ToString();
            }
        }

        private void LoadExistingTrip(int tripId)
        {
            loadedTrip = MJdatabase.GetSpecialTripById(tripId);
            if (loadedTrip == null)
            {
                MessageBox.Show("Special Trip record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetFormForNew();
                return;
            }

            currentTripId = loadedTrip.Id;
            txtOrderRef.Text = loadedTrip.OrderReference;
            dtpDispatchDate.Value = loadedTrip.DispatchDate;
            cbCompany.Text = loadedTrip.Company;
            cbDepartureTime.Text = loadedTrip.DepartureTime;
            txtAddress.Text = loadedTrip.Address;

            cbServiceType.SelectedItem = loadedTrip.ServiceType;
            if (cbServiceType.SelectedIndex < 0 && cbServiceType.Items.Count > 0) cbServiceType.SelectedIndex = 0;

            cbStatus.SelectedItem = loadedTrip.Status;
            if (cbStatus.SelectedIndex < 0 && cbStatus.Items.Count > 0) cbStatus.SelectedIndex = 0;

            chkPaymentWithReport.Checked = loadedTrip.ModeOfPaymentWithReport;
            chkWithSOA.Checked = loadedTrip.WithSOA;
            chkWithInvoice.Checked = loadedTrip.WithServiceInvoice;
            chkWithCollection.Checked = loadedTrip.WithCollection;

            if (loadedTrip.TruckId > 0 && !string.IsNullOrWhiteSpace(loadedTrip.TruckPlate))
            {
                for (int i = 0; i < cbTruck.Items.Count; i++)
                {
                    if (cbTruck.Items[i]?.ToString()?.Contains(loadedTrip.TruckPlate) == true)
                    {
                        cbTruck.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(loadedTrip.DriverName))
            {
                for (int i = 0; i < cbDriver.Items.Count; i++)
                {
                    if (cbDriver.Items[i]?.ToString()?.Equals(loadedTrip.DriverName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cbDriver.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(loadedTrip.AideName))
            {
                for (int i = 0; i < cbAide.Items.Count; i++)
                {
                    if (cbAide.Items[i]?.ToString()?.Equals(loadedTrip.AideName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cbAide.SelectedIndex = i;
                        break;
                    }
                }
            }

            lblTableTitle.Text = "Delivery Details";
            lblTableDesc.Text = "add a line or row and column";
            currentColumns = SpecialTripTableEditorDialog.GetDefaultColumns();

            var restoredRows = new List<Dictionary<string, string>>();
            bool hasSchemaOnlyMarker = false;
            if (loadedTrip.Stops != null && loadedTrip.Stops.Count > 0)
            {
                bool schemaRestored = false;
                foreach (var stop in loadedTrip.Stops)
                {
                    var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["colStdId"] = stop.StandardId ?? string.Empty,
                        ["colDetails"] = stop.DeliveryDetails ?? string.Empty,
                        ["colRoute"] = stop.DriverRoute ?? string.Empty,
                        ["colDepTime"] = stop.DepartureTime ?? string.Empty
                    };

                    bool skipRow = false;
                    if (!string.IsNullOrWhiteSpace(stop.ExtraColumnsJson))
                    {
                        try
                        {
                            var payload = JsonSerializer.Deserialize<SpecialTripStopPayload>(stop.ExtraColumnsJson);
                            if (payload != null)
                            {
                                if (!schemaRestored && payload.Columns != null && payload.Columns.Count > 0)
                                {
                                    lblTableTitle.Text = string.IsNullOrWhiteSpace(payload.TableTitle) ? "Delivery Details" : payload.TableTitle;
                                    lblTableDesc.Text = payload.TableDescription ?? "add a line or row and column";
                                    currentColumns = payload.Columns.Select(c => c.Clone()).ToList();
                                    schemaRestored = true;
                                }
                                if (payload.IsSchemaOnly)
                                {
                                    skipRow = true;
                                    hasSchemaOnlyMarker = true;
                                }
                                else if (payload.CellValues != null)
                                {
                                    foreach (var kvp in payload.CellValues)
                                    {
                                        rowDict[kvp.Key] = kvp.Value ?? string.Empty;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            try
                            {
                                var legacyDict = JsonSerializer.Deserialize<Dictionary<string, string>>(stop.ExtraColumnsJson);
                                if (legacyDict != null)
                                {
                                    foreach (var kvp in legacyDict)
                                    {
                                        rowDict[kvp.Key] = kvp.Value ?? string.Empty;
                                        if (currentColumns.All(c => !c.Key.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            currentColumns.Add(new SpecialTripColumnDef
                                            {
                                                Key = kvp.Key,
                                                HeaderText = kvp.Key.Replace("dyn_", "").Replace("_", " "),
                                                Width = 140
                                            });
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (!skipRow)
                    {
                        restoredRows.Add(rowDict);
                    }
                }
            }

            UpdateTableTitleAndDescPositions();
            RebuildGridColumns();

            if (restoredRows.Count > 0)
            {
                PopulateGridRows(restoredRows);
            }
            else if (!hasSchemaOnlyMarker)
            {
                AddInitialDefaultStop();
            }

            btnUpdate.Visible = true;
            btnReschedule.Visible = true;
            btnSave.Text = "💾 Save Copy as New";
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (currentTripId > 0 && btnUpdate.Visible)
            {
                currentTripId = 0;
                txtOrderRef.Text = MJdatabase.GenerateNextSpecialTripReference();
            }
            SaveTripInternal(isNew: true);
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (currentTripId <= 0) return;
            SaveTripInternal(isNew: false);
        }

        private string GetRowValueOrFallback(Dictionary<string, string> rowDict, string preferredKey, int fallbackColIndex, string defaultValue = "")
        {
            if (rowDict.TryGetValue(preferredKey, out string? val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }
            if (fallbackColIndex >= 0 && fallbackColIndex < currentColumns.Count)
            {
                string colKey = currentColumns[fallbackColIndex].Key;
                if (rowDict.TryGetValue(colKey, out string? posVal) && !string.IsNullOrWhiteSpace(posVal))
                {
                    return posVal;
                }
            }
            return defaultValue;
        }

        private void SaveTripInternal(bool isNew)
        {
            gridDeliveryDetails.EndEdit();

            string orderRef = txtOrderRef.Text.Trim();
            string company = cbCompany.Text.Trim();
            if (string.IsNullOrWhiteSpace(company))
            {
                MessageBox.Show("Please enter or select a Company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbCompany.Focus();
                return;
            }

            string depText = cbDepartureTime.Text.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(depText, @"^(0?[1-9]|1[0-2]):[0-5][0-9]\s*(AM|PM)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                MessageBox.Show("Please enter the departure time like 08:00 AM.", "Invalid Departure Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbDepartureTime.Focus();
                return;
            }
            string statusText = cbStatus.Text.Trim();
            bool needsCrew = statusText.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || statusText.Equals("Completed", StringComparison.OrdinalIgnoreCase);
            if (needsCrew && (cbTruck.SelectedIndex <= 0 || cbDriver.SelectedIndex <= 0))
            {
                MessageBox.Show($"A trip that is '{statusText}' needs a truck and a driver. Pick them first.", "Truck and Driver Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                (cbTruck.SelectedIndex <= 0 ? (Control)cbTruck : cbDriver).Focus();
                return;
            }
            if (statusText.Equals("Completed", StringComparison.OrdinalIgnoreCase) && dtpDispatchDate.Value.Date > DateTime.Today)
            {
                var early = MessageBox.Show($"This trip is dated {dtpDispatchDate.Value:MMMM d, yyyy}, which is still ahead. Save it as Completed anyway?", "Trip is in the future", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (early != DialogResult.Yes) return;
            }
            if (cbDriver.SelectedIndex > 0 && cbAide.SelectedIndex > 0 && cbDriver.SelectedIndex - 1 < availableDrivers.Count && cbAide.SelectedIndex - 1 < availableAides.Count
                && availableDrivers[cbDriver.SelectedIndex - 1].Id == availableAides[cbAide.SelectedIndex - 1].Id)
            {
                MessageBox.Show("The same person can't be both the driver and the aide.", "Check the crew", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbAide.Focus();
                return;
            }
            SpecialTrip trip = new SpecialTrip
            {
                Id = isNew ? 0 : currentTripId,
                OrderReference = orderRef,
                DispatchDate = dtpDispatchDate.Value,
                Company = company,
                DepartureTime = cbDepartureTime.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                ServiceType = cbServiceType.Text.Trim(),
                Status = cbStatus.Text.Trim(),
                ModeOfPaymentWithReport = chkPaymentWithReport.Checked,
                WithSOA = chkWithSOA.Checked,
                WithServiceInvoice = chkWithInvoice.Checked,
                WithCollection = chkWithCollection.Checked,
                CreatedAt = isNew ? DateTime.Now : (loadedTrip?.CreatedAt ?? DateTime.Now),
                CompletedAt = cbStatus.Text.Trim() == "Completed" ? (DateTime?)DateTime.Now : (loadedTrip?.CompletedAt),
                Remarks = $"Special Trip dispatch for {company}"
            };

            if (cbTruck.SelectedIndex > 0 && cbTruck.SelectedIndex - 1 < availableTrucks.Count)
            {
                var trk = availableTrucks[cbTruck.SelectedIndex - 1];
                trip.TruckId = trk.Id;
                trip.TruckPlate = trk.PlateNumber;
            }

            if (cbDriver.SelectedIndex > 0 && cbDriver.SelectedIndex - 1 < availableDrivers.Count)
            {
                var drv = availableDrivers[cbDriver.SelectedIndex - 1];
                trip.DriverId = drv.Id;
                trip.DriverName = drv.FullName;
            }

            if (cbAide.SelectedIndex > 0 && cbAide.SelectedIndex - 1 < availableAides.Count)
            {
                var aid = availableAides[cbAide.SelectedIndex - 1];
                trip.AideId = aid.Id;
                trip.AideName = aid.FullName;
            }

            trip.Stops = new List<SpecialTripStop>();
            var allRows = CaptureGridRows();
            int seq = 1;

            if (allRows.Count == 0)
            {
                var schemaPayload = new SpecialTripStopPayload
                {
                    TableTitle = lblTableTitle.Text,
                    TableDescription = lblTableDesc.Text,
                    IsSchemaOnly = true,
                    Columns = currentColumns.Select(c => c.Clone()).ToList(),
                    CellValues = new Dictionary<string, string>()
                };
                trip.Stops.Add(new SpecialTripStop
                {
                    SequenceNo = 0,
                    StandardId = string.Empty,
                    DeliveryDetails = string.Empty,
                    DriverRoute = string.Empty,
                    DepartureTime = string.Empty,
                    ExtraColumnsJson = JsonSerializer.Serialize(schemaPayload)
                });
            }
            else
            {
                foreach (var rowDict in allRows)
                {
                    string stdId = GetRowValueOrFallback(rowDict, "colStdId", 0, $"ST-{seq:D3}");
                    string details = GetRowValueOrFallback(rowDict, "colDetails", 1, string.Empty);
                    string route = GetRowValueOrFallback(rowDict, "colRoute", 2, string.Empty);
                    string depTime = GetRowValueOrFallback(rowDict, "colDepTime", 3, cbDepartureTime.Text);

                    var payload = new SpecialTripStopPayload
                    {
                        TableTitle = lblTableTitle.Text,
                        TableDescription = lblTableDesc.Text,
                        IsSchemaOnly = false,
                        Columns = currentColumns.Select(c => c.Clone()).ToList(),
                        CellValues = rowDict
                    };

                    trip.Stops.Add(new SpecialTripStop
                    {
                        SequenceNo = seq++,
                        StandardId = stdId,
                        DeliveryDetails = details,
                        DriverRoute = route,
                        DepartureTime = depTime,
                        ExtraColumnsJson = JsonSerializer.Serialize(payload)
                    });
                }
            }

            try
            {
                bool success = MJdatabase.SaveSpecialTrip(trip);
                if (success)
                {
                    SavedSpecialTripId = trip.Id;
                    IsSaved = true;

                    if (trip.Status == "Completed")
                    {
                        MJdatabase.QueueSpecialTripFinanceDrafts(trip);
                    }

                    MessageBox.Show(
                        $"Special Trip '{trip.OrderReference}' successfully {(isNew ? "created and saved" : "updated")}!\n\nStatus: {trip.Status}\nStops Configured: {trip.Stops.Count}\nSaved in Special Trip History.",
                        "Special Trip Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Special Trip: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
