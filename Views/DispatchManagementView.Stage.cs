using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 28: the Status column tells where an order is in its whole life (Scheduled, Current, For trip report, For Statement of Account,
    /// For Service Invoice, For Collection, Completed). The order stays in the active list until it is paid. The four "For ..." pills are clickable.
    /// </summary>
    public partial class DispatchManagementView
    {
        /// <summary>Dispatch id → stage. Built once per list load (not per row); rows missing from it are worked out from the status alone.</summary>
        private Dictionary<int, DispatchStageInfo> stageMap = new Dictionary<int, DispatchStageInfo>();

        internal DispatchStageInfo StageOf(Delivery d)
            => stageMap.TryGetValue(d.Id, out var s) ? s : DispatchStage.For(d, null, false, false, 0);

        /// <summary>The status chip filter for a stage ("" when the filter is a plain status).</summary>
        private static DispatchStageKind? StageOfFilter(string filter) => filter switch
        {
            "stage:ForTripReport" => DispatchStageKind.ForTripReport,
            "stage:ForSoa" => DispatchStageKind.ForSoa,
            "stage:ForInvoice" => DispatchStageKind.ForInvoice,
            "stage:ForCollection" => DispatchStageKind.ForCollection,
            "Completed" => DispatchStageKind.Paid,   // Completed now means paid
            _ => null
        };

        private async Task RebuildStageMapAsync(List<Delivery> rows)
        {
            try { stageMap = await Task.Run(() => DispatchStage.BuildMap(rows)); }
            catch (Exception ex) { ErrLog.Swallowed(ex); stageMap = new Dictionary<int, DispatchStageInfo>(); }
        }

        /// <summary>Pill colours of the stage (back, text). Open stages keep the colours of their dispatch status.</summary>
        private static (Color Back, Color Fore)? StageColors(DispatchStageKind k) => DispatchStage.PillColors(k);   // shared with Order History

        /// <summary>Nov 28: the Status column never gets narrower than the longest stage label (bold) plus the pill padding, so "For Statement of Account" is never cut.</summary>
        private int StatusColumnMinWidth()
        {
            using var bold = new Font(gridDispatch.Font, FontStyle.Bold);
            int w = DispatchStage.AllLabels.Max(s => TextRenderer.MeasureText(s, bold).Width);
            return w + (int)Math.Ceiling(24 * (gridDispatch.DeviceDpi / 96.0));
        }

        private Delivery? DeliveryAtRow(int rowIndex)
            => rowIndex >= 0 && rowIndex < gridDispatch.Rows.Count ? gridDispatch.Rows[rowIndex].DataBoundItem as Delivery : null;

        private bool IsStageClickCell(int rowIndex, int colIndex, out Delivery? d, out DispatchStageInfo? stage)
        {
            d = null; stage = null;
            if (rowIndex < 0 || colIndex < 0 || gridDispatch.Columns[colIndex].DataPropertyName != "Status") return false;
            d = DeliveryAtRow(rowIndex);
            if (d == null) return false;
            stage = StageOf(d);
            return stage.Clickable;
        }

        private void SetupStageClicks()
        {
            gridDispatch.CellMouseMove += (s, e) =>
            {
                gridDispatch.Cursor = IsStageClickCell(e.RowIndex, e.ColumnIndex, out _, out _) ? Cursors.Hand : Cursors.Default;
            };
            gridDispatch.CellMouseLeave += (s, e) => gridDispatch.Cursor = Cursors.Default;
            gridDispatch.CellToolTipTextNeeded += (s, e) =>
            {
                if (IsStageClickCell(e.RowIndex, e.ColumnIndex, out _, out var st) && st != null) e.ToolTipText = "Click to " + char.ToLowerInvariant(st.Action[0]) + st.Action.Substring(1);
            };
            gridDispatch.CellMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || ModifierKeys != Keys.None) return;
                if (IsStageClickCell(e.RowIndex, e.ColumnIndex, out var d, out var st) && d != null && st != null) RunStageAction(d, st);
            };
        }

        /// <summary>Does the next step of the order: trip report, statement, invoice or collection receipt (the screens apply their own permissions).</summary>
        private void RunStageAction(Delivery d, DispatchStageInfo st)
        {
            try { MainForm.Instance?.OpenNextStepForDispatch(d, st); }
            catch (Exception ex) { ErrLog.Error("DispatchManagementView.Stage", ex); QuietBox.Show("Could not open the next step: " + ex.Message, "Dispatch", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
