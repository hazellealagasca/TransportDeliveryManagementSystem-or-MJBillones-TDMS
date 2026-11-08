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
        private readonly System.Collections.Generic.List<Control> hiddenForForm = new();

        public bool FormIsOpenAndDirty => pageForm != null && !pageForm.IsDisposed && pageForm.HasUnsavedTyping;
        public void DiscardAndCloseForm()
        {
            if (keptForm != null && !keptForm.IsDisposed) { var k = keptForm; keptForm = null; k.CloseWithoutAsking(); k.Dispose(); }
            pageForm?.CloseWithoutAsking();
        }

        // Nov 9: "Keep for later": the open form is hidden (not closed) and comes back when the person returns to this tab.
        private SpecialTripFormDialog? keptForm;
        private bool keepHooked;
        public bool HasKeptDraft => keptForm != null && !keptForm.IsDisposed;
        public void KeepAndCloseForm()
        {
            var dlg = pageForm;
            if (dlg == null || dlg.IsDisposed) return;
            keptForm = dlg;
            pageForm = null;
            Controls.Remove(dlg);
            dlg.Visible = false;
            foreach (var c in hiddenForForm) c.Visible = true; // the list comes back
            hiddenForForm.Clear();
            SetFullScreen(false);
            if (keepHooked) return;
            keepHooked = true;
            VisibleChanged += (s, e) =>
            {
                if (Visible && HasKeptDraft && IsHandleCreated) BeginInvoke(new Action(ResumeKeptForm));
            };
        }
        private void ResumeKeptForm()
        {
            var dlg = keptForm;
            if (dlg == null || dlg.IsDisposed || !Visible) return;
            keptForm = null;
            pageForm = dlg;
            hiddenForForm.Clear();
            foreach (Control c in Controls) if (c.Visible) { hiddenForForm.Add(c); c.Visible = false; }
            Controls.Add(dlg);
            dlg.Visible = true;
            dlg.BringToFront();
            SetFullScreen(true);
            dlg.Focus();
            try { var f = FindForm(); if (f != null) ToastHelper.ShowToast(f, "Restored your unsaved changes.", ToastType.Info); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

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

        /// <summary>Opens a saved trip's details window (like the dispatch details): steps, next action, tiles. Edit opens the full-screen form.</summary>
        public void ShowSpecialTripDetails(int tripId)
            => SpecialTripDetailsDialog.ShowFor(this, tripId, id => OpenSpecialTripDialog(id), () => { try { LoadData(); } catch (Exception ex) { ErrLog.Swallowed(ex); } });

        /// <summary>Opens the special trip form (new when <paramref name="tripId"/> is 0) as a full page in this tab.</summary>
        public void OpenSpecialTripDialog(int tripId = 0, Action<SpecialTripFormDialog>? prefill = null)
        {
            if (HasKeptDraft) { ResumeKeptForm(); return; } // a kept draft comes back as it was left, never a blank form
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
            dlg.KeepRequested += KeepAndCloseForm;
            dlg.FormClosed += (s, e) =>
            {
                var go = dlg.PendingNavigation;
                bool saved = dlg.IsSaved;
                pageForm = null;
                Controls.Remove(dlg);
                foreach (var c in hiddenForForm) c.Visible = true; // the list comes back
                hiddenForForm.Clear();
                SetFullScreen(false);
                if (saved) { try { LoadData(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
                if (go != null) BeginInvoke(go);
            };
            // Nov 6: the form takes the whole page (the list, its totals and toolbar are hidden until it closes).
            hiddenForForm.Clear();
            foreach (Control c in Controls) if (c.Visible) { hiddenForForm.Add(c); c.Visible = false; }
            Controls.Add(dlg);
            dlg.BringToFront();
            SetFullScreen(true);
            dlg.Show();
            dlg.EnsureShown();
            dlg.Focus();
        }
    }
}
