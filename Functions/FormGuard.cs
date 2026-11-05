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
        /// <summary>True when the form may be left: nothing unsaved, or the person agreed to discard it (the form is then cleared).</summary>
        public static bool ConfirmDiscard(Control view)
        {
            if (view is not IUnsavedForm u || !u.FormIsOpenAndDirty) return true;
            var ok = MessageBox.Show(view.FindForm(), "Discard your unsaved changes?\n\nWhat you typed in this form will be cleared.", "Unsaved changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            if (ok) u.DiscardAndCloseForm();
            return ok;
        }

        /// <summary>Like <see cref="ConfirmDiscard"/>, for a hub or module: checks every visible page inside it.</summary>
        public static bool ConfirmLeave(Control? root)
        {
            if (root == null) return true;
            if (root is IUnsavedForm) return ConfirmDiscard(root);
            foreach (Control c in root.Controls)
                if (c.Visible && !ConfirmLeave(c)) return false;
            return true;
        }
    }
}
