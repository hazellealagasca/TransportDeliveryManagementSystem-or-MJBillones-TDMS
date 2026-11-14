using System;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// A store code the system made (the store was saved with the code left blank) shows as "(no code)" in grey in a table. The real code
    /// stays the cell's value, so matching, saving and checks are unchanged. The cell being edited shows the real code, so typing
    /// over it works as usual.
    /// </summary>
    public static class GeneratedCodeDisplay
    {
        /// <summary>Shows generated codes in the named column of the table as "(no code)".</summary>
        public static void Attach(DataGridView grid, string codeColumn)
        {
            grid.CellFormatting += (s, e) =>
            {
                try
                {
                    if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != codeColumn) return;
                    string code = (e.Value?.ToString() ?? "").Trim();
                    if (code.Length == 0 || ReferenceEquals(grid.CurrentCell, grid.Rows[e.RowIndex].Cells[e.ColumnIndex])) return;
                    if (!MJdatabase.IsGeneratedStoreCode(code)) return;
                    e.Value = MJdatabase.NoCodeText;
                    e.CellStyle!.ForeColor = AppTheme.TextMuted;
                    e.FormattingApplied = true;
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            };
        }
    }
}
