using System;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6, 2026: the special trip form opens full screen inside this page (like + Add order on Dispatch) instead of a popup:
    /// the menu, top bar and Operations tabs are hidden until the form closes.
    /// </summary>
    public partial class SpecialTripHistoryView
    {
        private SpecialTripFormDialog? pageForm;

        public bool FormIsOpenAndDirty => pageForm != null && !pageForm.IsDisposed && pageForm.HasUnsavedTyping;
        public void DiscardAndCloseForm() => pageForm?.CloseWithoutAsking();

        private void SetFullScreen(bool on)
        {
            try
            {
                MainForm.Instance?.SetFocusMode(on);
                Control? c = Parent;
                while (c != null && !(c is OperationsHubView)) c = c.Parent;
                (c as OperationsHubView)?.SetTabsVisible(!on);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Opens the special trip form (new when <paramref name="tripId"/> is 0) as a full page in this tab.</summary>
        public void OpenSpecialTripDialog(int tripId = 0, Action<SpecialTripFormDialog>? prefill = null)
        {
            if (pageForm != null && !pageForm.IsDisposed) { pageForm.BringToFront(); pageForm.Focus(); return; }
            SpecialTripFormDialog dlg;
            try { dlg = new SpecialTripFormDialog(tripId); }
            catch (Exception ex)
            {
                ErrLog.Error("SpecialTripHistoryView.OpenSpecialTripDialog", ex);
                MessageBox.Show(FindForm(), $"The special trip form could not be opened: {ex.Message}", "Special trip", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            prefill?.Invoke(dlg);
            dlg.TopLevel = false;
            dlg.FormBorderStyle = FormBorderStyle.None;
            dlg.MinimumSize = System.Drawing.Size.Empty;
            dlg.Dock = DockStyle.Fill;
            pageForm = dlg;
            dlg.FormClosed += (s, e) =>
            {
                var go = dlg.PendingNavigation;
                bool saved = dlg.IsSaved;
                pageForm = null;
                Controls.Remove(dlg);
                SetFullScreen(false);
                if (saved) { try { LoadData(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
                if (go != null) BeginInvoke(go);
            };
            Controls.Add(dlg);
            dlg.BringToFront();
            SetFullScreen(true);
            dlg.Show();
            dlg.EnsureShown();
            dlg.Focus();
        }
    }
}
