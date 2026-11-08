using System;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>A page with an add / edit form that can hold unsaved typing (Nov 6, 2026: forms start blank, ask before discarding).</summary>
    public interface IUnsavedForm
    {
        /// <summary>The form is open and the person typed or picked something that is not saved yet.</summary>
        bool FormIsOpenAndDirty { get; }
        /// <summary>Closes the form and clears every field, so it opens blank next time.</summary>
        void DiscardAndCloseForm();
        /// <summary>Leaves the form but keeps what was typed; it comes back as it was left (see <see cref="KeptDraftState"/>).</summary>
        void KeepAndCloseForm();
        /// <summary>A form was left with "Keep for later" and has not been reopened yet.</summary>
        bool HasKeptDraft { get; }
    }

    /// <summary>What the person chose in the "Unsaved changes" pop-up.</summary>
    public enum LeaveChoice { Stay, Discard, Keep }

    /// <summary>
    /// Bookkeeping for "Keep for later". A screen holds one of these. While <see cref="Keeping"/> the screen hides its form without
    /// clearing the fields; afterwards the add button reads "Resume draft", and coming back to the screen reopens the form with the typing.
    /// </summary>
    public sealed class KeptDraftState
    {
        /// <summary>The form was left with Keep and has not been reopened.</summary>
        public bool Kept { get; private set; }
        /// <summary>True only while the screen hides its form for Keep (do not clear the fields, do not reset the dirty flag).</summary>
        public bool Keeping;
        /// <summary>True only while the screen reopens a kept form (do not reset the dirty flag).</summary>
        public bool Resuming;
        private string? savedToggleText;
        private bool hooked;

        /// <summary>Call right after the form was hidden with <see cref="Keeping"/> set. <paramref name="resume"/> reopens the form.</summary>
        public void MarkKept(Control view, Button? toggle, Action resume)
        {
            Kept = true;
            if (toggle != null) { savedToggleText = toggle.Text; toggle.Text = "Resume draft"; }
            if (hooked) return;
            hooked = true;
            // Coming back to the screen (another module or tab and back) reopens the form by itself.
            view.VisibleChanged += (s, e) =>
            {
                if (!view.Visible || !Kept || view.IsDisposed || !view.IsHandleCreated) return;
                view.BeginInvoke(new Action(() => { if (Kept && view.Visible && !view.IsDisposed) resume(); }));
            };
        }

        /// <summary>The form is being shown again (resumed, or replaced by another record): the draft is no longer waiting.</summary>
        public void Restored(Button? toggle)
        {
            if (!Kept) return;
            Kept = false;
            if (toggle != null && savedToggleText != null) toggle.Text = savedToggleText;
            savedToggleText = null;
        }

        /// <summary>Reopens the kept form through the screen's own show routine and tells the person.</summary>
        public void Resume(Control view, Action<bool> setFormVisible)
        {
            Resuming = true;
            try { setFormVisible(true); }
            finally { Resuming = false; }
            try { var f = view.FindForm(); if (f != null) ToastHelper.ShowToast(f, "Restored your unsaved changes.", ToastType.Info); }
            catch (Exception swx) { ErrLog.Swallowed(swx); }
        }
    }

    /// <summary>
    /// Notices when the person changes a field. Only changes made while the field has the keyboard focus count, so filling a
    /// form from code (loading a record, clearing, auto numbers) never marks it as changed.
    /// </summary>
    public sealed class FormDirtyTracker
    {
        private bool dirty;
        public bool IsDirty => dirty;
        public void Reset() => dirty = false;
        public void MarkDirty() => dirty = true;

        public FormDirtyTracker(Control root) => Hook(root);

        private void Hook(Control c)
        {
            // Nov 9: controls named "noDirty…" (search / filter boxes) never count as typing on the form.
            if ((c.Name ?? "").StartsWith("noDirty", StringComparison.Ordinal)) return;
            switch (c)
            {
                case TextBoxBase t: t.TextChanged += Changed; break;
                case ComboBox cb: cb.SelectedIndexChanged += Changed; cb.TextChanged += Changed; break;
                case NumericUpDown n: n.ValueChanged += Changed; break;
                case DateTimePicker d: d.ValueChanged += Changed; break;
                case CheckBox k: k.CheckedChanged += Changed; break;
                case RadioButton r: r.CheckedChanged += Changed; break;
                case DataGridView g: g.CellValueChanged += Changed; g.RowsRemoved += (s, e) => { if (g.ContainsFocus) dirty = true; }; break;
            }
            foreach (Control child in c.Controls) Hook(child);
            c.ControlAdded += (s, e) => { if (e.Control != null) Hook(e.Control); };
        }

        private void Changed(object? sender, EventArgs e)
        {
            if (sender is Control c && c.ContainsFocus) dirty = true;
        }
    }

    public static class FormGuard
    {
        /// <summary>
        /// The one place the "Unsaved changes" pop-up lives. With <paramref name="allowKeep"/> it offers Keep for later / Discard / Stay here;
        /// without it (the screen is about to be destroyed) only Discard / Stay here. Esc and the X mean Stay here.
        /// </summary>
        public static LeaveChoice AskLeave(IWin32Window? owner, bool allowKeep)
        {
            if (!allowKeep)
            {
                var r2 = MessageBox.Ask(owner, "Discard your unsaved changes?\n\nWhat you typed in this form will be cleared.", "Unsaved changes", MessageBoxIcon.Question,
                    new[] { ("Discard", DialogResult.No), ("Stay here", DialogResult.Cancel) }, defaultIndex: 1);
                return r2 == DialogResult.No ? LeaveChoice.Discard : LeaveChoice.Stay;
            }
            var r = MessageBox.Ask(owner, "You have unsaved changes on this form.\n\nKeep them for later (they will be here when you come back), or discard what you typed?", "Unsaved changes", MessageBoxIcon.Question,
                new[] { ("Keep for later", DialogResult.Yes), ("Discard", DialogResult.No), ("Stay here", DialogResult.Cancel) }, defaultIndex: 0);
            return r == DialogResult.Yes ? LeaveChoice.Keep : r == DialogResult.No ? LeaveChoice.Discard : LeaveChoice.Stay;
        }

        /// <summary>True when the form may be left: nothing unsaved, or the person chose Keep (the typing stays) or Discard (the form is cleared).</summary>
        public static bool ConfirmDiscard(Control view, bool allowKeep = true)
        {
            if (view is not IUnsavedForm u || !u.FormIsOpenAndDirty) return true;
            switch (AskLeave(view.FindForm(), allowKeep))
            {
                case LeaveChoice.Keep: u.KeepAndCloseForm(); return true;
                case LeaveChoice.Discard: u.DiscardAndCloseForm(); return true;
                default: return false;
            }
        }

        /// <summary>Like <see cref="ConfirmDiscard"/>, for a hub or module: checks every visible page inside it.</summary>
        public static bool ConfirmLeave(Control? root, bool allowKeep = true)
        {
            if (root == null) return true;
            if (root is IUnsavedForm) return ConfirmDiscard(root, allowKeep);
            foreach (Control c in root.Controls)
                if (c.Visible && !ConfirmLeave(c, allowKeep)) return false;
            return true;
        }
    }
}
