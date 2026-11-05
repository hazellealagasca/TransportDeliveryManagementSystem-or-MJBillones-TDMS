using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Escape on an open form. While a table cell is being typed in, or a drop-down list is open, Escape only cancels
    /// that (it used to close the whole form and lose what was typed). Otherwise it asks before leaving the form.
    /// </summary>
    public static class EscapeGuard
    {
        /// <summary>True when the form may close now (the person said Yes).</summary>
        public static bool AllowClose(Control view)
        {
            Control? c = FocusedControl(view);
            for (var p = c; p != null; p = p.Parent)
            {
                if (p is DataGridView g && g.IsCurrentCellInEditMode) return false;
                if (p is ComboBox cb && cb.DroppedDown) return false;
                if (p == view) break;
            }
            if (view is IUnsavedForm u && !u.FormIsOpenAndDirty) return true; // nothing typed: leave without asking (Nov 6)
            var owner = view.FindForm();
            return MessageBox.Show(owner, "Discard your unsaved changes?\n\nWhat you typed in this form will be cleared.", "Unsaved changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private static Control? FocusedControl(Control view)
        {
            Control? c = view.FindForm();
            while (c is ContainerControl cc && cc.ActiveControl != null) c = cc.ActiveControl;
            return c;
        }
    }
}
