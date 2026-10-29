using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Makes every window follow dark mode, including small dialogs that were built before theming existed:
    /// dark title bar, themed inputs / grids / scrollbars, and a contrast fix for any label that would be unreadable.
    /// It watches for newly opened windows, so no dialog has to remember to opt in.
    /// </summary>
    public static class DarkModeKit
    {
        private static System.Windows.Forms.Timer? timer;
        private static readonly ConditionalWeakTable<Form, object> done = new ConditionalWeakTable<Form, object>();
        public static void Install()
        {
            if (timer != null) return;
            timer = new System.Windows.Forms.Timer { Interval = 200 };
            timer.Tick += (s, e) => Sweep();
            timer.Start();
        }
        private static void Sweep()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f == null || f.IsDisposed || done.TryGetValue(f, out _)) continue;
                    done.Add(f, true);
                    GuardAutoComplete(f);
                    if (f is LoginForm || f is MainForm) continue;
                    Apply(f);
                }
            }
            catch (InvalidOperationException) { /* the open-forms list changed while sweeping; next tick */ }
        }
        /// <summary>
        /// Windows' suggestion list for auto-complete boxes runs on its own thread. If a window is closed while that list
        /// is open (or was just used), WinForms throws "Cross-thread operation not valid". Turning auto-complete off
        /// just before the window closes prevents it.
        /// </summary>
        public static void GuardAutoComplete(Form f)
        {
            f.FormClosing += (s, e) => { if (!e.Cancel) StopAutoComplete(f); };
        }
        public static void StopAutoComplete(Control root)
        {
            try
            {
                foreach (Control c in root.Controls)
                {
                    if (c is TextBox t && (t.AutoCompleteMode != AutoCompleteMode.None || t.AutoCompleteSource != AutoCompleteSource.None))
                    {
                        t.AutoCompleteMode = AutoCompleteMode.None;
                        t.AutoCompleteSource = AutoCompleteSource.None;
                    }
                    else if (c is ComboBox cb && cb.AutoCompleteMode != AutoCompleteMode.None)
                    {
                        cb.AutoCompleteMode = AutoCompleteMode.None;
                        if (cb.AutoCompleteSource == AutoCompleteSource.CustomSource) cb.AutoCompleteSource = AutoCompleteSource.None;
                    }
                    if (c.HasChildren) StopAutoComplete(c);
                }
            }
            catch { }
        }
        /// <summary>Applies the active theme to a window and everything in it (safe to call more than once).</summary>
        public static void Apply(Form f)
        {
            try
            {
                ThemeManager.StyleWindowChrome(f);
                if (!ThemeManager.IsDark) return;
                // Windows already drawn from theme tokens only need native parts (scrollbars, lists); older ones get the full theme pass.
                string n = f.GetType().Name;
                bool modern = n is "DispatchDetailsDialog" or "PrintStudioDialog" or "TableCustomizeDialog" or "SpecialTripTableEditorDialog"
                    or "TruckProfileDialog" or "CrewProfileDialog" or "AttachmentViewer" or "UserProfileDialog" or "QuickAddRouteDialog";
                if (!modern) UIThemeHelper.ApplyTheme(f);
                foreach (Control c in f.Controls) Native(c);
                f.Invalidate(true);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DarkModeKit] {f.GetType().Name}: {ex.Message}"); }
        }
        /// <summary>Dark-mode native styling for a control tree built after the window opened (lazy tabs).</summary>
        public static void Native(Control c)
        {
            if (c is ListBox || c is ListView || c is TreeView || c is DataGridView || (c is ScrollableControl sc && sc.AutoScroll) || (c is TextBox t && t.Multiline))
                ThemeManager.StyleNativeControl(c);
            if (c is ListView lv) { lv.BackColor = AppTheme.Surface; lv.ForeColor = AppTheme.TextPrimary; }
            if (c is ComboBox cb) { cb.BackColor = AppTheme.InputBack; cb.ForeColor = AppTheme.TextPrimary; UIThemeHelper.StyleInput(cb); }
            if (c is DateTimePicker dtp) { dtp.CalendarForeColor = AppTheme.TextPrimary; dtp.CalendarMonthBackground = AppTheme.InputBack; dtp.CalendarTitleBackColor = AppTheme.Accent; dtp.CalendarTitleForeColor = AppTheme.OnAccent; UIThemeHelper.StyleInput(dtp); }
            if (c is TextBox tb && tb.BorderStyle != BorderStyle.None) UIThemeHelper.StyleInput(tb);
            if (c is ListBox lb && lb.BackColor.GetBrightness() > 0.8f) { lb.BackColor = AppTheme.Surface; lb.ForeColor = AppTheme.TextPrimary; }
            foreach (Control child in c.Controls) Native(child);
        }
    }
}
