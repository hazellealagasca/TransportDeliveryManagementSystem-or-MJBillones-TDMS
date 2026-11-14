using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Maintenance history of every truck (the trucks list's "Maintenance history"): plate, category, crate/temp type, capacity (as they were when the record was written),
    /// date of maintenance, type (Service, Breakdown, Decommissioned, Back in service), description and estimated fix date. Read only.
    /// </summary>
    public sealed class TruckMaintenanceHistoryDialog : Form
    {
        public static readonly string[] ColumnHeaders = { "Plate No.", "Truck category", "Crate/Temp type", "Capacity", "Date of maintenance", "Type", "Description", "Est. fix date" };
        private readonly DataGridView grid = ProfileKit.Grid();

        /// <summary>The cells of one history row, in the order of <see cref="ColumnHeaders"/>.</summary>
        public static string[] RowCells(TruckMaintenance m) => new[]
        {
            m.PlateNumber, m.TruckCategory, m.CrateTempType, m.Capacity.ToString("N0"), Fmt.Date(m.ServiceDate), m.EntryType, m.Task,
            Fmt.Date(m.NextDueDate) is { Length: > 0 } d ? d : "—"
        };

        public TruckMaintenanceHistoryDialog()
        {
            Text = "Truck maintenance history";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(ProfileKit.D(this, 1060), ProfileKit.D(this, 620));
            BackColor = AppTheme.Canvas;
            List<TruckMaintenance> rows;
            try { rows = MJdatabase.GetAllTruckMaintenance(); } catch (Exception ex) { ErrLog.Error("TruckMaintenanceHistoryDialog", ex); rows = new List<TruckMaintenance>(); }
            float[] weights = { 70, 100, 100, 65, 95, 95, 190, 90 };
            for (int i = 0; i < ColumnHeaders.Length; i++) grid.Columns.Add(ProfileKit.Col("c" + i, ColumnHeaders[i], weights[i], i == 3));
            foreach (var m in rows)
            {
                int r = grid.Rows.Add(RowCells(m));
                grid.Rows[r].Tag = m;
                var typeCell = grid.Rows[r].Cells[5];
                if (m.EntryType == MJdatabase.TruckEntryBreakdown) typeCell.Style.ForeColor = AppTheme.DangerText;
                else if (m.EntryType == MJdatabase.TruckEntryDecommissioned) typeCell.Style.ForeColor = AppTheme.TextMuted;
                else if (m.EntryType == MJdatabase.TruckEntryBackInService) typeCell.Style.ForeColor = AppTheme.SuccessText;
            }
            var close = new Button { Text = "Close", Dock = DockStyle.Right, Width = ProfileKit.D(this, 100), DialogResult = DialogResult.Cancel, Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(close, "primary");
            var footer = new Panel { Dock = DockStyle.Bottom, Height = ProfileKit.D(this, 56), BackColor = AppTheme.Surface, Padding = new Padding(ProfileKit.D(this, 20), ProfileKit.D(this, 10), ProfileKit.D(this, 20), ProfileKit.D(this, 10)) };
            footer.Controls.Add(close);
            var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(ProfileKit.D(this, 20), ProfileKit.D(this, 12), ProfileKit.D(this, 20), ProfileKit.D(this, 12)) };
            body.Controls.Add(ProfileKit.Card(grid, 1));
            body.Controls.Add(ProfileKit.Section("Maintenance history", $"{rows.Count} record(s), newest first · breakdowns, decommissions and returns to service are written when a truck's status changes"));
            Controls.Add(body);
            Controls.Add(footer);
            CancelButton = close;
            UIThemeHelper.ApplyTheme(this);
        }
    }
}
