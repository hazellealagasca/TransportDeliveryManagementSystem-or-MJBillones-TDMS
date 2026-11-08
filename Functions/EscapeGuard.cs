using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Escape on an open form. While a table cell is being typed in, or a drop-down list is open, Escape only cancels
    /// that (it used to close the whole form and lose what was typed). Otherwise it asks before leaving the form.
    /// </summary>
    public static class EscapeGuard
    {
        /// <summary>True when the caller should close (and clear) the form now. On Keep the form is already hidden with its typing kept, so this returns false.</summary>
        public static bool AllowClose(Control view)
        {
            Control? c = FocusedControl(view);
            for (var p = c; p != null; p = p.Parent)
            {
                if (p is DataGridView g && g.IsCurrentCellInEditMode) return false;
                if (p is ComboBox cb && cb.DroppedDown) return false;
                if (p == view) break;
            }
            var u = view as IUnsavedForm;
            if (u != null && !u.FormIsOpenAndDirty) return true; // nothing typed: leave without asking (Nov 6)
            // Keep is only offered where the screen can hold the typing (it implements IUnsavedForm).
            switch (FormGuard.AskLeave(view.FindForm(), allowKeep: u != null))
            {
                case LeaveChoice.Keep: u!.KeepAndCloseForm(); return false; // the screen already hid its form, nothing more to close
                case LeaveChoice.Discard: return true;
                default: return false;
            }
        }

        private static Control? FocusedControl(Control view)
        {
            Control? c = view.FindForm();
            while (c is ContainerControl cc && cc.ActiveControl != null) c = cc.ActiveControl;
            return c;
        }
    }
}
