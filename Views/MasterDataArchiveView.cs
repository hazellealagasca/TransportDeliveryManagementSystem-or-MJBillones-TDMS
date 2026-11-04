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
    /// Master Data → Archive: archived clients, stores and delivery routes. Nothing here is deleted; each record can
    /// be restored, and past orders, trip reports and invoices keep showing it either way.
    /// </summary>
    public class MasterDataArchiveView : UserControl, IRefreshable
    {
        private DataGridView grid = null!;
        private TextBox txtSearch = null!;
        private ChoiceChip chipKind = null!;
        private Label lblTitle = null!;
        private List<MJdatabase.ArchivedItem> items = new List<MJdatabase.ArchivedItem>();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);



        public MasterDataArchiveView()
        {
            Build();
            UIThemeHelper.ApplyTheme(this);
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            RefreshData();
        }



        private void Build()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Padding = new Padding(D(12));
            Font = new Font("Segoe UI", 9.5F);
            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(16), D(20), D(16)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var header = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Padding = new Padding(0, 0, 0, D(8)) };
            lblTitle = new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Archive", Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary };
            var sub = new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Archived clients, stores and delivery routes. They are hidden from new orders but never deleted — past records still show them. Click Restore to bring one back.", Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, Padding = new Padding(0, D(2), 0, D(8)) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, BackColor = AppTheme.Surface };
            txtSearch = new TextBox { Width = D(300), PlaceholderText = "🔍 Search name, code or client…", Font = new Font("Segoe UI", 10F), Margin = new Padding(0, D(8), D(12), D(4)) };
            txtSearch.TextChanged += (s, e) => Fill();
            chipKind = new ChoiceChip("Show", new[] { "Everything", "Clients", "Stores", "Routes" }) { Margin = new Padding(0, D(4), D(8), 0) };
            chipKind.SelectionChanged += (s, e) => Fill();
            flow.Controls.Add(txtSearch);
            flow.Controls.Add(chipKind);
            header.Controls.Add(flow);
            header.Controls.Add(sub);
            header.Controls.Add(lblTitle);



            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = D(38),
                EnableHeadersVisualStyles = false
            };
            grid.RowTemplate.Height = D(36);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colKind", HeaderText = "Type", FillWeight = 50, MinimumWidth = D(80) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Code", FillWeight = 50, MinimumWidth = D(80) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", FillWeight = 170, MinimumWidth = D(160) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colClient", HeaderText = "Client", FillWeight = 130, MinimumWidth = D(140) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOn", HeaderText = "Archived on", FillWeight = 60, MinimumWidth = D(100) });
            var colRestore = new DataGridViewButtonColumn { Name = "colRestore", HeaderText = "", Text = "↩ Restore", UseColumnTextForButtonValue = true, FillWeight = 50, MinimumWidth = D(96), FlatStyle = FlatStyle.Flat };
            colRestore.DefaultCellStyle.ForeColor = AppTheme.Accent;
            colRestore.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            grid.Columns.Add(colRestore);
            grid.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "colRestore" && grid.Rows[e.RowIndex].Tag is MJdatabase.ArchivedItem it) Restore(it);
            };
            card.Controls.Add(grid);
            card.Controls.Add(header);
            Controls.Add(card);
        }



        public void RefreshData()
        {
            try { items = MJdatabase.GetArchivedItems(); }
            catch (Exception ex) { ErrLog.Error("MasterDataArchiveView", ex); items = new List<MJdatabase.ArchivedItem>(); }
            Fill();
        }



        private void Fill()
        {
            string q = (txtSearch.Text ?? "").Trim();
            string kind = chipKind.SelectedIndex switch { 1 => "Client", 2 => "Store", 3 => "Route", _ => "" };
            var shown = items.Where(i => (kind.Length == 0 || i.Kind == kind) &&
                (q.Length == 0 || i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || i.Code.Contains(q, StringComparison.OrdinalIgnoreCase) || i.Client.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
            grid.Rows.Clear();
            foreach (var it in shown)
            {
                int r = grid.Rows.Add(it.Kind, it.Code.Length == 0 ? "—" : it.Code, it.Name, it.Kind == "Client" ? "—" : (it.Client.Length == 0 ? "—" : it.Client), it.ArchivedOn.Length == 0 ? "—" : it.ArchivedOn);
                grid.Rows[r].Tag = it;
            }
            grid.ClearSelection();
            lblTitle.Text = items.Count == 0 ? "Archive (empty)" : shown.Count == items.Count ? $"Archive ({items.Count})" : $"Archive ({shown.Count} of {items.Count})";
        }



        private void Restore(MJdatabase.ArchivedItem it)
        {
            string what = it.Kind switch { "Client" => $"client \"{it.Name}\"", "Store" => $"store \"{(it.Code.Length > 0 ? it.Code + " - " : "")}{it.Name}\" of {it.Client}", _ => $"route \"{it.Code} - {it.Name}\"" };
            if (it.Kind == "Store" && items.Any(x => x.Kind == "Client" && string.Equals(x.Name, it.Client, StringComparison.OrdinalIgnoreCase)))
            {
                QuietBox.Show($"{it.Client} is archived too. Restore the client first, then its store.", "Restore store", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Are you sure you want to restore {what}?\n\nIt will be offered again for new orders.", "Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                switch (it.Kind)
                {
                    case "Client": MJdatabase.SetClientActive(it.Name, true); break;
                    case "Store": MJdatabase.RestoreClientStore(it.Id); break;
                    case "Route": MJdatabase.RestoreDeliveryRoute(it.Id, $"{it.Code} - {it.Name}"); break;
                }
                ToastHelper.ShowToast(this, $"{it.Kind} restored ✓", ToastType.Success);
            }
            catch (Exception ex)
            {
                ErrLog.Error("MasterDataArchiveView", ex);
                MessageBox.Show(ex.Message, "Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RefreshData();
        }
    }
}
