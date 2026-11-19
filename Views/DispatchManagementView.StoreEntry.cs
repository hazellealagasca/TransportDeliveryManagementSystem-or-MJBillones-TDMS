using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Step 1 entry row: Store code, Store name, G crates, E crates, Temp Type and "＋ Add store". Stores are added to the
    /// table only through this row (the table itself can't be typed in).
    /// </summary>
    public partial class DispatchManagementView
    {
        private TextBox? txtEntryCode, txtEntryName, txtEntryG, txtEntryE;
        private Button? btnEntryAdd;
        private TableLayoutPanel? pnlStoreEntryRow;
        private TextBoxStoreLookup? entryLookup;

        private const int EntrySeasonCol = 6;

        private Label EntryCaption(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = false,
                UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = AppTheme.TextSecondary,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, Dpi(4), 4, Dpi(4)),
                Margin = new Padding(0, 0, Dpi(8), 0)
            };
        }

        private TextBox EntryBox(string name, string placeholder, bool number)
        {
            var tb = new TextBox
            {
                Name = name,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = placeholder,
                Margin = new Padding(0, 0, Dpi(8), 0),
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary
            };
            if (number) { tb.MaxLength = 6; tb.TextAlign = HorizontalAlignment.Right; InputGuards.DigitsOnly(tb); }
            else tb.MaxLength = 80;
            return tb;
        }

        /// <summary>The entry row. The Season box is placed in its last column while delivery routes are off (see PlaceSeasonBox).</summary>
        private TableLayoutPanel BuildStoreEntryRow()
        {
            if (cbTempType == null) EnsureTempTypeControls();

            var tbl = new TableLayoutPanel
            {
                Name = "pnlStoreEntryRow",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 7,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 3),
                Padding = new Padding(0, 0, 0, Dpi(10))
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));   // code
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));   // name
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));   // G
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));   // E
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));   // temp
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // add button
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // season (routes off)
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(36)));

            txtEntryCode = EntryBox("txtEntryStoreCode", "Code", false);
            txtEntryName = EntryBox("txtEntryStoreName", "Name", false);
            txtEntryG = EntryBox("txtEntryGCrates", "0", true);
            txtEntryE = EntryBox("txtEntryECrates", "0", true);

            lblTempType!.Dock = DockStyle.Fill;
            lblTempType.Font = new Font("Segoe UI Semibold", 9F);
            lblTempType.ForeColor = AppTheme.TextSecondary;
            lblTempType.TextAlign = ContentAlignment.BottomLeft;
            lblTempType.AutoSize = false;
            lblTempType.Padding = new Padding(0, Dpi(4), 4, Dpi(4));
            lblTempType.Margin = new Padding(0, 0, Dpi(8), 0);
            cbTempType!.Dock = DockStyle.Fill;
            cbTempType.Margin = new Padding(0, 0, Dpi(8), 0);

            btnEntryAdd = new Button
            {
                Name = "btnAddStoreRow",
                Text = "＋ Add store",
                AutoSize = true,
                Height = Dpi(34),
                Padding = new Padding(Dpi(10), 0, Dpi(10), 0),
                Font = new Font("Segoe UI Semibold", 9F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Margin = new Padding(0, 0, Dpi(8), Dpi(1))
            };
            UIThemeHelper.SetButtonRole(btnEntryAdd, "primary");
            storeTips.SetToolTip(btnEntryAdd, "Add this store and its crates to the table below");
            btnEntryAdd.Click += (s, e) => AddStoreFromEntryRow();

            tbl.Controls.Add(EntryCaption("Store code"), 0, 0);
            tbl.Controls.Add(txtEntryCode, 0, 1);
            tbl.Controls.Add(EntryCaption("Store name"), 1, 0);
            tbl.Controls.Add(txtEntryName, 1, 1);
            tbl.Controls.Add(EntryCaption("G crates"), 2, 0);
            tbl.Controls.Add(txtEntryG, 2, 1);
            tbl.Controls.Add(EntryCaption("E crates"), 3, 0);
            tbl.Controls.Add(txtEntryE, 3, 1);
            tbl.Controls.Add(lblTempType, 4, 0);
            tbl.Controls.Add(cbTempType, 4, 1);
            tbl.Controls.Add(btnEntryAdd, 5, 1);

            entryLookup = new TextBoxStoreLookup(txtEntryCode, txtEntryName, () => cbClient.Text,
                () => storeListRoute != null ? $"{storeListRoute.RouteCode} - {storeListRoute.RouteName}" : cbDestination.Text);

            foreach (Control c in new Control[] { txtEntryG, txtEntryE, cbTempType })
                c.KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Enter) return;
                    e.Handled = e.SuppressKeyPress = true;
                    AddStoreFromEntryRow();
                };

            // A different client has different stores: whatever was typed for the old one goes.
            cbClient.TextChanged += (s, e) => { if (!isBindingForm) ClearEntryRow(); };

            pnlStoreEntryRow = tbl;
            return tbl;
        }

        /// <summary>With delivery routes off the Season / Peak Season box rides at the right end of the entry row; with them on it stays in the route row.</summary>
        private void PlaceSeasonBox(bool routesOn, TableLayoutPanel routeCargoRow, Control seasonLabel, Control seasonBox)
        {
            if (pnlStoreEntryRow == null) return;
            if (routesOn)
            {
                routeCargoRow.Controls.Add(seasonLabel, 1, 0);
                routeCargoRow.Controls.Add(seasonBox, 1, 1);
            }
            else
            {
                pnlStoreEntryRow.Controls.Add(seasonLabel, EntrySeasonCol, 0);
                pnlStoreEntryRow.Controls.Add(seasonBox, EntrySeasonCol, 1);
                seasonBox.MinimumSize = new Size(Dpi(130), 0);
            }
            routeCargoRow.Visible = routesOn;
        }

        private bool EntryRowHasText =>
            (txtEntryCode?.Text.Trim().Length ?? 0) > 0 || (txtEntryName?.Text.Trim().Length ?? 0) > 0
            || EntryNumber(txtEntryG) > 0 || EntryNumber(txtEntryE) > 0;

        private static int EntryNumber(TextBox? tb) => tb != null && int.TryParse(tb.Text.Trim(), out int n) ? Math.Max(0, n) : 0;

        private void ClearEntryRow()
        {
            if (txtEntryCode == null || txtEntryName == null || txtEntryG == null || txtEntryE == null) return;
            txtEntryCode.Text = ""; txtEntryName.Text = ""; txtEntryG.Text = ""; txtEntryE.Text = "";
        }

        /// <summary>Checks the entry row and, when it is good, appends the store to the table exactly like a typed row. Returns true when a row was added.</summary>
        private bool AddStoreFromEntryRow()
        {
            if (gridRouteStores == null || txtEntryCode == null || txtEntryName == null || txtEntryG == null || txtEntryE == null || entryLookup == null) return false;
            var owner = this.FindForm();
            if (cbClient.Text.Trim().Length == 0)
            {
                ShowStoreWarning(StoreDirectory.NeedClientMessage);
                MessageBox.Show(owner, StoreDirectory.NeedClientMessage, "Client required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusStep(cbClient);
                return false;
            }
            if (txtEntryCode.Text.Trim().Length == 0 && txtEntryName.Text.Trim().Length == 0)
            {
                MessageBox.Show(owner, "Enter the store code or store name (pick it from the list).", "Store required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntryCode.Focus();
                return false;
            }
            int g = EntryNumber(txtEntryG), e = EntryNumber(txtEntryE);
            if (g + e <= 0)
            {
                MessageBox.Show(owner, "Enter at least 1 crate (G or E) for this store.", "Crates required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntryG.Focus();
                return false;
            }

            var st = entryLookup.Confirm(owner ?? (IWin32Window)this, s =>
            {
                string c = StoreDirectory.Norm(s.StoreCode), n = StoreDirectory.Norm(s.StoreName);
                bool dup = GetGridStoreList().Any(x =>
                    (c.Length > 0 && string.Equals(x.Code, c, StringComparison.OrdinalIgnoreCase)) ||
                    (n.Length > 0 && string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase)));
                return dup ? $"{(n.Length > 0 ? n : c)} is already in the table for this order." : null;
            });
            if (st == null) return false;

            pnlRouteStoresBreakdown!.Visible = true;
            int rIdx = gridRouteStores.Rows.Add();
            var row = gridRouteStores.Rows[rIdx];
            SetStoreRowType(row, false);
            SizeStoreBreakdown(gridRouteStores.Rows.Count);
            storeGuard?.FillRow(row, st);
            if (gridRouteStores.Columns.Contains("colGCrate")) row.Cells["colGCrate"].Value = g.ToString();
            if (gridRouteStores.Columns.Contains("colECrate")) row.Cells["colECrate"].Value = e.ToString();
            RecalculateTotalsFromRouteStoresGrid();
            UpdateNoOfDrops();
            UpdateSaveStoresToRouteButton();

            ClearEntryRow();
            txtEntryCode.Focus();
            return true;
        }

        /// <summary>Step 1 "Next": the entry row still holds a store that wasn't added. Returns false when the user stays on the step.</summary>
        private bool ResolvePendingEntryRow()
        {
            if (!EntryRowHasText) return true;
            var r = MessageBox.Show(this.FindForm(), "You typed a store that isn't added yet. Add it now?", "Store not added", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes) return AddStoreFromEntryRow();
            ClearEntryRow();
            return true;
        }
    }
}
