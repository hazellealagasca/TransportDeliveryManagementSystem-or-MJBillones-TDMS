using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class SpecialTripColumnDef
    {
        public string Key { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public int Width { get; set; } = 150;
        public bool IsFill { get; set; } = false;

        public SpecialTripColumnDef Clone()
        {
            return new SpecialTripColumnDef
            {
                Key = this.Key,
                HeaderText = this.HeaderText,
                Width = this.Width,
                IsFill = this.IsFill
            };
        }
    }

    public class SpecialTripStopPayload
    {
        public string TableTitle { get; set; } = "Delivery Details";
        public string TableDescription { get; set; } = "add a line or row and column";
        public bool IsSchemaOnly { get; set; } = false;
        public List<SpecialTripColumnDef> Columns { get; set; } = new List<SpecialTripColumnDef>();
        public Dictionary<string, string> CellValues { get; set; } = new Dictionary<string, string>();
    }

    public class SpecialTripTableEditorDialog : Form
    {
        public string EditedTableTitle { get; private set; } = "Delivery Details";
        public string EditedTableDescription { get; private set; } = "add a line or row and column";
        public List<SpecialTripColumnDef> EditedColumns { get; private set; } = new List<SpecialTripColumnDef>();
        public List<Dictionary<string, string>> EditedRows { get; private set; } = new List<Dictionary<string, string>>();

        private readonly string defaultDepartureTime;
        private TextBox txtTableTitle = null!;
        private TextBox txtTableDesc = null!;
        private DataGridView gridColumns = null!;
        private TextBox txtColumnName = null!;
        private DataGridView gridRows = null!;
        private bool isSyncingColumns = false;
        private int customColCounter = 1;

        public SpecialTripTableEditorDialog(
            string currentTitle,
            string currentDescription,
            List<SpecialTripColumnDef> currentColumns,
            List<Dictionary<string, string>> currentRows,
            string defaultDepTime = "08:00 AM")
        {
            EditedTableTitle = string.IsNullOrWhiteSpace(currentTitle) ? "Delivery Details" : currentTitle;
            EditedTableDescription = currentDescription ?? "add a line or row and column";
            defaultDepartureTime = string.IsNullOrWhiteSpace(defaultDepTime) ? "08:00 AM" : defaultDepTime;

            EditedColumns = currentColumns != null && currentColumns.Count > 0
                ? currentColumns.Select(c => c.Clone()).ToList()
                : GetDefaultColumns();

            EditedRows = currentRows != null
                ? currentRows.Select(r => new Dictionary<string, string>(r, StringComparer.OrdinalIgnoreCase)).ToList()
                : new List<Dictionary<string, string>>();

            customColCounter = EditedColumns.Count + 1;

            InitializeEditorLayout();
            UIThemeHelper.ApplyTheme(this);
            ApplyDialogCustomStyling();
            PopulateInitialData();
        }

        public static List<SpecialTripColumnDef> GetDefaultColumns()
        {
            return new List<SpecialTripColumnDef>
            {
                new SpecialTripColumnDef { Key = "colStdId", HeaderText = "Standard ID", Width = 130, IsFill = false },
                new SpecialTripColumnDef { Key = "colDetails", HeaderText = "Delivery Details", Width = 230, IsFill = true },
                new SpecialTripColumnDef { Key = "colRoute", HeaderText = "Driver Route", Width = 180, IsFill = false },
                new SpecialTripColumnDef { Key = "colDepTime", HeaderText = "Departure Time", Width = 135, IsFill = false },
                new SpecialTripColumnDef { Key = "colRemarks", HeaderText = "Remarks / Note", Width = 180, IsFill = false }
            };
        }

        private Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Label lblHeaderSub = null!;
        private Button btnAddCol = null!;
        private Button btnRenameCol = null!;
        private Button btnDeleteCol = null!;
        private Button btnMoveUpCol = null!;
        private Button btnMoveDownCol = null!;
        private Button btnResetCols = null!;
        private Button btnAddRow = null!;
        private Button btnDuplicateRow = null!;
        private Button btnDeleteRow = null!;
        private Button btnClearRows = null!;
        private Button btnApply = null!;
        private Button btnCancel = null!;

        private Label lblRowsCount = null!;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private void InitializeEditorLayout()
        {
            this.Text = "Edit Store Table";
            var work = Screen.FromPoint(Cursor.Position).WorkingArea;
            this.Size = new Size(Math.Min(D(1320), (int)(work.Width * 0.92)), Math.Min(D(860), (int)(work.Height * 0.92)));
            this.MinimumSize = new Size(Math.Min(D(1000), work.Width), Math.Min(D(680), work.Height));
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.BackColor = AppTheme.Canvas;
            this.Font = new Font("Segoe UI", 9.5F);
            // ---------- Header ----------
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = D(76), BackColor = AppTheme.Surface };
            lblHeaderTitle = new Label
            {
                Text = "Edit store table",
                Font = new Font("Segoe UI Semibold", 15F),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(D(26), D(12))
            };

            lblHeaderSub = new Label
            {
                Text = "Rename the table, choose which columns it has, and fill in each store's crates and details.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextMuted,
                AutoSize = true,
                Location = new Point(D(28), D(44))
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);

            pnlHeader.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1); };
            // ---------- Footer ----------
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = D(72), BackColor = AppTheme.Surface, Padding = new Padding(D(28), D(16), D(28), D(16)) };
            pnlFooter.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0); };
            Label lblFooterHint = new Label
            {
                Text = "Nothing is saved until you click Apply changes.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = D(420),
                TextAlign = ContentAlignment.MiddleLeft
            };

            FlowLayoutPanel flowFooter = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface };
            btnCancel = new Button { Text = "Cancel", Size = new Size(D(120), D(40)), Margin = new Padding(0, 0, D(10), 0) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnApply = new Button { Text = "✓  Apply changes", Size = new Size(D(180), D(40)), Margin = new Padding(0) };
            btnApply.Click += BtnApply_Click;

            flowFooter.Controls.Add(btnCancel);
            flowFooter.Controls.Add(btnApply);
            pnlFooter.Controls.Add(lblFooterHint);
            pnlFooter.Controls.Add(flowFooter);

            // ---------- Body ----------
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(24), D(20), D(24), D(20)), BackColor = AppTheme.Canvas };
            // Card 1: table title & description
            Panel pnlTitleCard = CreateCard();
            pnlTitleCard.Dock = DockStyle.Top;
            pnlTitleCard.Height = D(104);
            TableLayoutPanel tblMeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = AppTheme.Surface };
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tblMeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tblMeta.RowStyles.Add(new RowStyle(SizeType.Absolute, D(26)));
            tblMeta.RowStyles.Add(new RowStyle(SizeType.Absolute, D(36)));
            txtTableTitle = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), PlaceholderText = "e.g. Delivery Details", Margin = new Padding(0, 0, D(12), 0) };
            txtTableDesc = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), PlaceholderText = "e.g. Stores, crates and DR numbers for this route", Margin = new Padding(D(12), 0, 0, 0) };
            tblMeta.Controls.Add(FieldLabel("Table title", new Padding(0)), 0, 0);
            tblMeta.Controls.Add(FieldLabel("Description (optional)", new Padding(D(12), 0, 0, 0)), 1, 0);
            tblMeta.Controls.Add(txtTableTitle, 0, 1);
            tblMeta.Controls.Add(txtTableDesc, 1, 1);
            pnlTitleCard.Controls.Add(tblMeta);

            Panel pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = D(16), BackColor = AppTheme.Canvas };
            // Main area: columns (left) | rows (right)
            TableLayoutPanel tblMain = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = AppTheme.Canvas, Margin = new Padding(0) };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(330)));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(16)));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // ----- Columns card -----
            Panel pnlColumnsCard = CreateCard();
            pnlColumnsCard.Dock = DockStyle.Fill;
            pnlColumnsCard.Margin = new Padding(0);
            Panel colsHead = SectionHeader("Columns", "Click a name to rename it in place.");
            gridColumns = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            var colOrd = new DataGridViewTextBoxColumn { Name = "colOrder", HeaderText = "#", FillWeight = 14, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            var colHeaderName = new DataGridViewTextBoxColumn { Name = "colHeaderText", HeaderText = "Column name", FillWeight = 70 };
            var colRemoveBtn = new DataGridViewButtonColumn { Name = "colRemoveCol", HeaderText = "", Text = "✕", UseColumnTextForButtonValue = true, FillWeight = 16, FlatStyle = FlatStyle.Flat };
            gridColumns.Columns.AddRange(colOrd, colHeaderName, colRemoveBtn);
            gridColumns.SelectionChanged += GridColumns_SelectionChanged;
            gridColumns.CellValueChanged += GridColumns_CellValueChanged;
            gridColumns.CellValidating += (s, e) =>
            {
                if (isSyncingColumns || e.RowIndex < 0 || e.RowIndex >= EditedColumns.Count || gridColumns.Columns[e.ColumnIndex].Name != "colHeaderText") return;
                string typed = System.Text.RegularExpressions.Regex.Replace((e.FormattedValue?.ToString() ?? "").Trim(), @"\s+", " ");
                gridColumns.Rows[e.RowIndex].ErrorText = "";
                if (typed.Length == 0)
                {
                    gridColumns.Rows[e.RowIndex].ErrorText = "A column needs a name.";
                    e.Cancel = true;
                    return;
                }
                if (EditedColumns.Where((c, i) => i != e.RowIndex).Any(c => c.HeaderText.Trim().Equals(typed, StringComparison.OrdinalIgnoreCase)))
                {
                    gridColumns.Rows[e.RowIndex].ErrorText = $"\"{typed}\" is already a column.";
                    MessageBox.Show($"There is already a column called \"{typed}\". Column headers must be unique.", "Duplicate Column", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            };
            gridColumns.CellContentClick += GridColumns_CellContentClick;

            Panel pnlGridColsFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = AppTheme.Border };
            pnlGridColsFrame.Controls.Add(gridColumns);
            Panel pnlColControls = new Panel { Dock = DockStyle.Bottom, Height = D(176), BackColor = AppTheme.Surface, Padding = new Padding(0, D(14), 0, 0) };
            Label lblColInput = FieldLabel("Column name", new Padding(0));
            lblColInput.Dock = DockStyle.Top;
            lblColInput.Height = D(24);
            txtColumnName = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F), PlaceholderText = "Type a column name…" };
            TableLayoutPanel tblColBtns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, BackColor = AppTheme.Surface, Padding = new Padding(0, D(10), 0, 0) };
            for (int i = 0; i < 3; i++) tblColBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tblColBtns.RowStyles.Add(new RowStyle(SizeType.Absolute, D(44)));
            tblColBtns.RowStyles.Add(new RowStyle(SizeType.Absolute, D(44)));
            btnAddCol = new Button { Text = "+ Add" };
            btnAddCol.Click += BtnAddCol_Click;
            btnRenameCol = new Button { Text = "Rename" };
            btnRenameCol.Click += BtnRenameCol_Click;
            btnDeleteCol = new Button { Text = "Delete" };
            btnDeleteCol.Click += BtnDeleteCol_Click;
            btnMoveUpCol = new Button { Text = "↑  Up" };
            btnMoveUpCol.Click += BtnMoveUpCol_Click;
            btnMoveDownCol = new Button { Text = "↓  Down" };
            btnMoveDownCol.Click += BtnMoveDownCol_Click;
            btnResetCols = new Button { Text = "↺  Defaults" };
            btnResetCols.Click += BtnResetCols_Click;
            var colButtons = new[] { btnAddCol, btnRenameCol, btnDeleteCol, btnMoveUpCol, btnMoveDownCol, btnResetCols };
            for (int i = 0; i < colButtons.Length; i++)
            {
                var b = colButtons[i];
                b.Dock = DockStyle.Fill;
                b.Margin = new Padding(i % 3 == 0 ? 0 : D(4), D(3), i % 3 == 2 ? 0 : D(4), D(3));
                tblColBtns.Controls.Add(b, i % 3, i / 3);
            }
            pnlColControls.Controls.Add(tblColBtns);
            pnlColControls.Controls.Add(txtColumnName);
            pnlColControls.Controls.Add(lblColInput);
            pnlColumnsCard.Controls.Add(pnlGridColsFrame);
            pnlColumnsCard.Controls.Add(pnlColControls);
            pnlColumnsCard.Controls.Add(colsHead);
            // ----- Rows card -----
            Panel pnlRowsCard = CreateCard();
            pnlRowsCard.Dock = DockStyle.Fill;
            pnlRowsCard.Margin = new Padding(0);
            Panel rowsHead = SectionHeader("Stores & details", "Click any cell to type. Use Tab to move to the next cell.");
            lblRowsCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextMuted, Location = new Point(D(160), D(4)) };
            rowsHead.Controls.Add(lblRowsCount);
            FlowLayoutPanel flowRowBtns = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface, Padding = new Padding(0, D(4), 0, 0) };
            btnAddRow = new Button { Text = "+ Add row", Size = new Size(D(118), D(38)), Margin = new Padding(D(8), 0, 0, 0) };
            btnAddRow.Click += BtnAddRow_Click;

            btnDuplicateRow = new Button { Text = "Duplicate", Size = new Size(D(108), D(38)), Margin = new Padding(D(8), 0, 0, 0) };
            btnDuplicateRow.Click += BtnDuplicateRow_Click;

            btnDeleteRow = new Button { Text = "Delete row", Size = new Size(D(112), D(38)), Margin = new Padding(D(8), 0, 0, 0) };
            btnDeleteRow.Click += BtnDeleteRow_Click;

            btnClearRows = new Button { Text = "Clear all", Size = new Size(D(100), D(38)), Margin = new Padding(D(8), 0, 0, 0) };
            btnClearRows.Click += BtnClearRows_Click;

            flowRowBtns.Controls.Add(btnClearRows);
            flowRowBtns.Controls.Add(btnDeleteRow);
            flowRowBtns.Controls.Add(btnDuplicateRow);
            flowRowBtns.Controls.Add(btnAddRow);
            rowsHead.Controls.Add(flowRowBtns);
            gridRows = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            InputGuards.DigitsOnlyColumns(gridRows, "colECrate", "colGCrate");
            gridRows.CellContentClick += GridRows_CellContentClick;

            gridRows.RowsAdded += (s, e) => UpdateRowCount();
            gridRows.RowsRemoved += (s, e) => UpdateRowCount();
            Panel pnlGridRowsFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = AppTheme.Border };
            pnlGridRowsFrame.Controls.Add(gridRows);
            pnlRowsCard.Controls.Add(pnlGridRowsFrame);
            pnlRowsCard.Controls.Add(rowsHead);
            tblMain.Controls.Add(pnlColumnsCard, 0, 0);
            tblMain.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Margin = new Padding(0) }, 1, 0);
            tblMain.Controls.Add(pnlRowsCard, 2, 0);
            pnlBody.Controls.Add(tblMain);
            pnlBody.Controls.Add(pnlSpacer1);
            pnlBody.Controls.Add(pnlTitleCard);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private Panel CreateCard()
        {
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(16), D(20), D(18)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            return card;
        }
        private Label FieldLabel(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = AppTheme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Margin = margin,
                Padding = new Padding(0, 0, 0, D(4))
            };
        }
        private Panel SectionHeader(string title, string hint)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = D(60), BackColor = AppTheme.Surface };
            var t = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), ForeColor = AppTheme.TextPrimary, Location = new Point(0, 0) };
            var h = new Label { Text = hint, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, Location = new Point(D(1), D(28)) };
            p.Controls.Add(t);
            p.Controls.Add(h);
            return p;
        }
        private void UpdateRowCount()
        {
            if (lblRowsCount == null || gridRows == null) return;
            int n = gridRows.Rows.Count;
            lblRowsCount.Text = n == 1 ? "· 1 row" : $"· {n} rows";
        }
        private void ApplyDialogCustomStyling()
        {
            UIThemeHelper.SetButtonRole(btnApply, "primary");
            UIThemeHelper.SetButtonRole(btnCancel, "secondary");
            UIThemeHelper.SetButtonRole(btnAddCol, "primary");
            UIThemeHelper.SetButtonRole(btnRenameCol, "secondary");
            UIThemeHelper.SetButtonRole(btnDeleteCol, "danger");
            UIThemeHelper.SetButtonRole(btnMoveUpCol, "secondary");
            UIThemeHelper.SetButtonRole(btnMoveDownCol, "secondary");
            UIThemeHelper.SetButtonRole(btnResetCols, "secondary");
            UIThemeHelper.SetButtonRole(btnAddRow, "primary");
            UIThemeHelper.SetButtonRole(btnDuplicateRow, "secondary");
            UIThemeHelper.SetButtonRole(btnDeleteRow, "danger");
            UIThemeHelper.SetButtonRole(btnClearRows, "secondary");
            foreach (var b in new[] { btnApply, btnCancel, btnAddRow, btnDuplicateRow, btnDeleteRow, btnClearRows, btnAddCol, btnRenameCol, btnDeleteCol, btnMoveUpCol, btnMoveDownCol, btnResetCols })
            {
                b.Font = new Font("Segoe UI Semibold", 9.25F);
            }
            btnApply.Size = new Size(D(180), D(40));
            btnCancel.Size = new Size(D(120), D(40));
            foreach (var b in new[] { btnAddRow, btnDuplicateRow, btnDeleteRow, btnClearRows }) b.Height = D(38);
            // Editable cell grid: keep cell-by-cell selection and comfortable rows.
            gridRows.SelectionMode = DataGridViewSelectionMode.CellSelect;
            gridRows.EditMode = DataGridViewEditMode.EditOnEnter;
            gridRows.RowTemplate.Height = D(40);
            gridRows.ColumnHeadersHeight = D(42);
            gridRows.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            gridRows.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            gridRows.GridColor = AppTheme.GridLine;
            gridColumns.RowTemplate.Height = D(40);
            gridColumns.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            gridColumns.ColumnHeadersVisible = false;
            gridColumns.Columns["colOrder"]!.DefaultCellStyle.ForeColor = AppTheme.TextMuted;
            gridColumns.Columns["colRemoveCol"]!.DefaultCellStyle.ForeColor = AppTheme.DangerText;
            gridColumns.Columns["colRemoveCol"]!.DefaultCellStyle.BackColor = AppTheme.Surface;
            gridColumns.Columns["colRemoveCol"]!.DefaultCellStyle.SelectionBackColor = AppTheme.GridSelection;
            ThemeManager.StyleWindowChrome(this);
        }

        private void PopulateInitialData()
        {
            txtTableTitle.Text = EditedTableTitle;
            txtTableDesc.Text = EditedTableDescription;

            RefreshColumnsGrid();
            RebuildRowsGridColumnsAndPopulate(EditedRows);
        }

        private void RefreshColumnsGrid(int selectIndex = -1)
        {
            isSyncingColumns = true;
            gridColumns.Rows.Clear();
            for (int i = 0; i < EditedColumns.Count; i++)
            {
                var c = EditedColumns[i];
                int rIdx = gridColumns.Rows.Add((i + 1).ToString(), c.HeaderText);
                gridColumns.Rows[rIdx].Tag = c.Key;
            }
            if (selectIndex >= 0 && selectIndex < gridColumns.Rows.Count)
            {
                gridColumns.ClearSelection();
                gridColumns.Rows[selectIndex].Selected = true;
                txtColumnName.Text = EditedColumns[selectIndex].HeaderText;
            }
            else if (gridColumns.Rows.Count > 0)
            {
                txtColumnName.Text = EditedColumns[0].HeaderText;
            }
            isSyncingColumns = false;
        }

        private List<Dictionary<string, string>> CaptureCurrentRowsFromGrid()
        {
            gridRows.EndEdit();
            var list = new List<Dictionary<string, string>>();
            foreach (DataGridViewRow row in gridRows.Rows)
            {
                if (row.IsNewRow) continue;
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (row.Tag is Dictionary<string, string> existingDict)
                {
                    foreach (var kvp in existingDict)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
                foreach (var col in EditedColumns)
                {
                    if (gridRows.Columns.Contains(col.Key))
                    {
                        dict[col.Key] = row.Cells[col.Key].Value?.ToString() ?? string.Empty;
                    }
                }
                list.Add(dict);
            }
            return list;
        }

        private void RebuildRowsGridColumnsAndPopulate(List<Dictionary<string, string>> rowsData)
        {
            gridRows.SuspendLayout();
            gridRows.Rows.Clear();
            gridRows.Columns.Clear();

            var colSeq = new DataGridViewTextBoxColumn
            {
                Name = "colSeq",
                HeaderText = "#",
                Width = D(48),
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            gridRows.Columns.Add(colSeq);

            foreach (var colDef in EditedColumns)
            {
                var dgvCol = new DataGridViewTextBoxColumn
                {
                    Name = colDef.Key,
                    HeaderText = colDef.HeaderText,
                    Width = D(colDef.Width > 60 ? colDef.Width : 150),
                    MinimumWidth = D(90)
                };
                if (colDef.IsFill)
                {
                    dgvCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvCol.MinimumWidth = D(180);
                }
                gridRows.Columns.Add(dgvCol);
            }

            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDel",
                HeaderText = "",
                Text = "✕",
                UseColumnTextForButtonValue = true,
                Width = D(48),
                FlatStyle = FlatStyle.Flat
            };
            colDel.DefaultCellStyle.ForeColor = AppTheme.DangerText;
            colDel.DefaultCellStyle.BackColor = AppTheme.Surface;
            colDel.DefaultCellStyle.SelectionBackColor = AppTheme.GridSelection;
            gridRows.Columns.Add(colDel);

            for (int i = 0; i < rowsData.Count; i++)
            {
                var rowDict = rowsData[i];
                int rIdx = gridRows.Rows.Add();
                var gridRow = gridRows.Rows[rIdx];
                gridRow.Tag = new Dictionary<string, string>(rowDict, StringComparer.OrdinalIgnoreCase);
                gridRow.Cells["colSeq"].Value = (i + 1).ToString();

                foreach (var colDef in EditedColumns)
                {
                    if (rowDict.TryGetValue(colDef.Key, out string? val))
                    {
                        gridRow.Cells[colDef.Key].Value = val;
                    }
                    else
                    {
                        gridRow.Cells[colDef.Key].Value = string.Empty;
                    }
                }
            }

            gridRows.ResumeLayout();
            UpdateRowCount();
        }

        private void GridColumns_SelectionChanged(object? sender, EventArgs e)
        {
            if (isSyncingColumns) return;
            if (gridColumns.SelectedRows.Count > 0)
            {
                int idx = gridColumns.SelectedRows[0].Index;
                if (idx >= 0 && idx < EditedColumns.Count)
                {
                    txtColumnName.Text = EditedColumns[idx].HeaderText;
                }
            }
        }

        private void GridColumns_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (isSyncingColumns || e.RowIndex < 0 || e.RowIndex >= EditedColumns.Count) return;
            if (gridColumns.Columns[e.ColumnIndex].Name == "colHeaderText")
            {
                string newHeader = gridColumns.Rows[e.RowIndex].Cells["colHeaderText"].Value?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(newHeader))
                {
                    EditedColumns[e.RowIndex].HeaderText = newHeader;
                    txtColumnName.Text = newHeader;
                    string colKey = EditedColumns[e.RowIndex].Key;
                    if (gridRows.Columns[colKey] is DataGridViewColumn targetCol)
                    {
                        targetCol.HeaderText = newHeader;
                    }
                }
                else
                {
                    gridColumns.Rows[e.RowIndex].Cells["colHeaderText"].Value = EditedColumns[e.RowIndex].HeaderText;
                }
            }
        }

        private void GridColumns_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= EditedColumns.Count) return;
            if (gridColumns.Columns[e.ColumnIndex].Name == "colRemoveCol")
            {
                RemoveColumnAt(e.RowIndex);
            }
        }

        private void BtnAddCol_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            var rowsSnapshot = CaptureCurrentRowsFromGrid();

            string header = System.Text.RegularExpressions.Regex.Replace(txtColumnName.Text.Trim(), @"\s+", " ");
            if (string.IsNullOrWhiteSpace(header))
            {
                do { header = $"Custom Column {customColCounter++}"; }
                while (EditedColumns.Any(c => c.HeaderText.Trim().Equals(header, StringComparison.OrdinalIgnoreCase)));
            }
            else if (EditedColumns.Any(c => c.HeaderText.Trim().Equals(header, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"There is already a column called \"{header}\". Column headers must be unique.", "Duplicate Column", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtColumnName.Focus();
                txtColumnName.SelectAll();
                return;
            }

            string uniqueKey = $"colCustom_{DateTime.Now.Ticks % 100000}_{customColCounter++}";
            while (EditedColumns.Any(c => c.Key.Equals(uniqueKey, StringComparison.OrdinalIgnoreCase)))
            {
                uniqueKey = $"colCustom_{Guid.NewGuid():N}".Substring(0, 18);
            }

            EditedColumns.Add(new SpecialTripColumnDef
            {
                Key = uniqueKey,
                HeaderText = header,
                Width = 150,
                IsFill = false
            });

            RefreshColumnsGrid(EditedColumns.Count - 1);
            RebuildRowsGridColumnsAndPopulate(rowsSnapshot);
            txtColumnName.Clear();
            txtColumnName.Focus();
        }

        private void BtnRenameCol_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            if (gridColumns.SelectedRows.Count == 0)
            {
                QuietBox.Show("Please select a column header from the list on the left to rename.", "Select Column", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idx = gridColumns.SelectedRows[0].Index;
            string newTitle = txtColumnName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                MessageBox.Show("Please enter a non-empty column header name.", "Column Header Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtColumnName.Focus();
                return;
            }
            newTitle = System.Text.RegularExpressions.Regex.Replace(newTitle, @"\s+", " ");
            if (EditedColumns.Where((c, i) => i != idx).Any(c => c.HeaderText.Trim().Equals(newTitle, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"There is already a column called \"{newTitle}\". Column headers must be unique.", "Duplicate Column", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtColumnName.Focus();
                txtColumnName.SelectAll();
                return;
            }

            EditedColumns[idx].HeaderText = newTitle;
            RefreshColumnsGrid(idx);
            if (gridRows.Columns[EditedColumns[idx].Key] is DataGridViewColumn renamedCol)
            {
                renamedCol.HeaderText = newTitle;
            }
        }

        private void BtnDeleteCol_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            if (gridColumns.SelectedRows.Count == 0)
            {
                QuietBox.Show("Please select a column header to delete.", "Select Column", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RemoveColumnAt(gridColumns.SelectedRows[0].Index);
        }

        private void RemoveColumnAt(int index)
        {
            if (index < 0 || index >= EditedColumns.Count) return;
            if (EditedColumns.Count <= 1)
            {
                QuietBox.Show("The table must keep at least one data column.", "Cannot Delete Last Column", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rowsSnapshot = CaptureCurrentRowsFromGrid();
            EditedColumns.RemoveAt(index);

            int nextIdx = Math.Min(index, EditedColumns.Count - 1);
            RefreshColumnsGrid(nextIdx);
            RebuildRowsGridColumnsAndPopulate(rowsSnapshot);
        }

        private void BtnMoveUpCol_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            if (gridColumns.SelectedRows.Count == 0) return;
            int idx = gridColumns.SelectedRows[0].Index;
            if (idx <= 0) return;

            var rowsSnapshot = CaptureCurrentRowsFromGrid();
            var item = EditedColumns[idx];
            EditedColumns.RemoveAt(idx);
            EditedColumns.Insert(idx - 1, item);

            RefreshColumnsGrid(idx - 1);
            RebuildRowsGridColumnsAndPopulate(rowsSnapshot);
        }

        private void BtnMoveDownCol_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            if (gridColumns.SelectedRows.Count == 0) return;
            int idx = gridColumns.SelectedRows[0].Index;
            if (idx < 0 || idx >= EditedColumns.Count - 1) return;

            var rowsSnapshot = CaptureCurrentRowsFromGrid();
            var item = EditedColumns[idx];
            EditedColumns.RemoveAt(idx);
            EditedColumns.Insert(idx + 1, item);

            RefreshColumnsGrid(idx + 1);
            RebuildRowsGridColumnsAndPopulate(rowsSnapshot);
        }

        private void BtnResetCols_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            if (MessageBox.Show("Are you sure you want to reset the columns to their default headers?\n\nColumns you added are kept; built-in columns get their original names back.", "Reset to Default", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            var rowsSnapshot = CaptureCurrentRowsFromGrid();
            var defaults = GetDefaultColumns();

            foreach (var defCol in defaults)
            {
                var existing = EditedColumns.FirstOrDefault(c => c.Key.Equals(defCol.Key, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.HeaderText = defCol.HeaderText;
                }
                else
                {
                    EditedColumns.Add(defCol.Clone());
                }
            }

            RefreshColumnsGrid(0);
            RebuildRowsGridColumnsAndPopulate(rowsSnapshot);
        }

        private void BtnAddRow_Click(object? sender, EventArgs e)
        {
            gridRows.EndEdit();
            int nextSeq = gridRows.Rows.Count + 1;
            int rIdx = gridRows.Rows.Add();
            var row = gridRows.Rows[rIdx];
            row.Cells["colSeq"].Value = nextSeq.ToString();

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["colStdId"] = $"ST-{nextSeq:D3}",
                ["colDetails"] = $"Stop {nextSeq} Delivery",
                ["colRoute"] = "Standard Route Stop",
                ["colDepTime"] = defaultDepartureTime,
                ["colRemarks"] = string.Empty
            };
            row.Tag = dict;

            foreach (var col in EditedColumns)
            {
                if (dict.TryGetValue(col.Key, out string? defaultVal))
                {
                    row.Cells[col.Key].Value = defaultVal;
                }
                else
                {
                    row.Cells[col.Key].Value = string.Empty;
                }
            }

            RenumberRows();
            if (gridRows.Columns.Count > 1)
            {
                gridRows.CurrentCell = row.Cells[1];
                gridRows.BeginEdit(true);
            }
        }

        private void BtnDuplicateRow_Click(object? sender, EventArgs e)
        {
            gridRows.EndEdit();
            if (gridRows.CurrentRow == null || gridRows.CurrentRow.Index < 0)
            {
                BtnAddRow_Click(sender, e);
                return;
            }

            var srcRow = gridRows.CurrentRow;
            int nextSeq = gridRows.Rows.Count + 1;
            int rIdx = gridRows.Rows.Add();
            var dstRow = gridRows.Rows[rIdx];
            dstRow.Cells["colSeq"].Value = nextSeq.ToString();

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in EditedColumns)
            {
                string val = srcRow.Cells[col.Key].Value?.ToString() ?? string.Empty;
                if (col.Key.Equals("colStdId", StringComparison.OrdinalIgnoreCase))
                {
                    val = $"ST-{nextSeq:D3}";
                }
                dstRow.Cells[col.Key].Value = val;
                dict[col.Key] = val;
            }
            dstRow.Tag = dict;
            RenumberRows();
        }

        private void BtnDeleteRow_Click(object? sender, EventArgs e)
        {
            gridRows.EndEdit();
            if (gridRows.CurrentRow == null || gridRows.CurrentRow.Index < 0)
            {
                QuietBox.Show("Please select a row in the table to delete.", "Select Row", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            gridRows.Rows.RemoveAt(gridRows.CurrentRow.Index);
            RenumberRows();
        }

        private void BtnClearRows_Click(object? sender, EventArgs e)
        {
            gridRows.EndEdit();
            gridRows.Rows.Clear();
        }

        private void GridRows_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= gridRows.Rows.Count) return;
            if (gridRows.Columns[e.ColumnIndex].Name == "colDel")
            {
                gridRows.Rows.RemoveAt(e.RowIndex);
                RenumberRows();
            }
        }

        private void RenumberRows()
        {
            for (int i = 0; i < gridRows.Rows.Count; i++)
            {
                gridRows.Rows[i].Cells["colSeq"].Value = (i + 1).ToString();
            }
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            gridColumns.EndEdit();
            gridRows.EndEdit();

            if (EditedColumns.Count == 0)
            {
                MessageBox.Show("Please keep at least one column in the table.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var blankCol = EditedColumns.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.HeaderText));
            if (blankCol != null)
            {
                MessageBox.Show("Every column needs a header name. Rename or remove the blank column first.", "Column Header Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dupHeaders = EditedColumns.GroupBy(c => c.HeaderText.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupHeaders.Count > 0)
            {
                MessageBox.Show("Column headers must be unique. These appear more than once:\n\n• " + string.Join("\n• ", dupHeaders) + "\n\nRename one of each before applying.", "Duplicate Columns", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var badNumber = CaptureCurrentRowsFromGrid().Select((r, i) => (r, i)).SelectMany(x => x.r.Where(kv => (kv.Key.Equals("colECrate", StringComparison.OrdinalIgnoreCase) || kv.Key.Equals("colGCrate", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(kv.Value) && !kv.Value.Trim().All(char.IsDigit)).Select(kv => x.i + 1)).FirstOrDefault();
            if (badNumber > 0)
            {
                MessageBox.Show($"Row {badNumber}: crate counts must be whole numbers (digits only).", "Check crates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Are you sure you want to save these table changes (columns and rows)?", "Save table", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            EditedTableTitle = string.IsNullOrWhiteSpace(txtTableTitle.Text) ? "Delivery Details" : txtTableTitle.Text.Trim();
            EditedTableDescription = txtTableDesc.Text.Trim();
            EditedRows = CaptureCurrentRowsFromGrid();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
