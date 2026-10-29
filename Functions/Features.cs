using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Optional parts of the system that the administrator can switch off and back on (Settings → Features &amp; Pop-ups).
    /// Switching a part off only hides it: no records are deleted or changed, and everything comes back when it is
    /// switched on again. Screens ask <see cref="On"/> and listen to <see cref="Changed"/> to show or hide themselves.
    /// </summary>
    public static class Features
    {
        public const string RateMatrix = "Feature.RateMatrix";
        public const string SpecialTrips = "Feature.SpecialTrips";
        public const string Incidents = "Feature.Incidents";
        public const string CrewPay = "Feature.CrewPay";
        public const string BackupReminder = "Feature.BackupReminder";
        public const string ConfirmSaves = "Feature.ConfirmSaves";
        public const string QuietNotices = "Feature.QuietNotices";
        public const string Toasts = "EnableToastPopups";
        private static readonly Dictionary<string, bool> Defaults = new()
        {
            [RateMatrix] = true, [SpecialTrips] = true, [Incidents] = true, [CrewPay] = true,
            [BackupReminder] = true, [ConfirmSaves] = true, [QuietNotices] = true, [Toasts] = true
        };
        private static readonly Dictionary<string, bool> cache = new();
        private static readonly object gate = new();
        /// <summary>Raised on the UI thread with the key that changed.</summary>
        public static event Action<string>? Changed;
        public static bool On(string key)
        {
            lock (gate)
            {
                if (cache.TryGetValue(key, out bool v)) return v;
                bool def = !Defaults.TryGetValue(key, out bool d) || d;
                bool result = def;
                try
                {
                    string s = MJdatabase.GetSystemSetting(key, def ? "True" : "False").Trim();
                    result = s.Length == 0 ? def : !(s.Equals("False", StringComparison.OrdinalIgnoreCase) || s == "0" || s.Equals("Off", StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
                cache[key] = result;
                return result;
            }
        }
        public static void Set(string key, bool on, string label)
        {
            if (On(key) == on) return;
            MJdatabase.SetSystemSetting(key, on ? "True" : "False", label + (on ? " (on)" : " (off)"));
            lock (gate) cache[key] = on;
            try { MJdatabase.LogSecurityEvent(SessionContext.CurrentUsername, "SETTINGS", $"{label} turned {(on ? "on" : "off")}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            AppLog.Info("Features", $"{label} turned {(on ? "on" : "off")}");
            try { Changed?.Invoke(key); } catch (Exception ex) { AppLog.Error("Features.Changed", ex, key); }
        }
        /// <summary>Forgets cached values (after a restore or when settings were changed elsewhere).</summary>
        public static void Reload() { lock (gate) cache.Clear(); }
        /// <summary>Calls <paramref name="apply"/> now and whenever one of <paramref name="keys"/> changes, until <paramref name="owner"/> is disposed.</summary>
        public static void Bind(Control owner, Action apply, params string[] keys)
        {
            void Handler(string k)
            {
                if (Array.IndexOf(keys, k) < 0 || owner.IsDisposed) return;
                if (owner.InvokeRequired) { try { owner.BeginInvoke(new Action(apply)); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
                else apply();
            }
            Changed += Handler;
            owner.Disposed += (s, e) => Changed -= Handler;
            apply();
        }
        /// <summary>A short note for screens reached from a link while their part of the system is switched off.</summary>
        public static void TellOff(Control? near, string what)
        {
            try
            {
                var f = near?.FindForm() ?? Form.ActiveForm;
                if (f != null) ToastHelper.ShowToast(f, $"{what} is turned off. An administrator can turn it back on in Settings → Features & Pop-ups.", ToastType.Info);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
    /// <summary>
    /// Shows information-only messages. When "quiet notices" is on (the default), simple confirmations such as
    /// "Saved successfully" appear as a small notice in the corner that fades away instead of a window you must click.
    /// Questions, warnings and errors always use a normal window.
    /// </summary>
    public static class QuietBox
    {
        private static Form? FormOf(IWin32Window? owner) => owner as Form ?? (owner as Control)?.FindForm() ?? Form.ActiveForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[Application.OpenForms.Count - 1] : null);
        private static bool TryToast(IWin32Window? owner, string text, string caption)
        {
            if (!Features.On(Features.QuietNotices) || !Features.On(Features.Toasts)) return false;
            var f = FormOf(owner);
            if (f == null || f.IsDisposed || !f.Visible || f.WindowState == FormWindowState.Minimized) return false;
            // A modal dialog that is about to close is a poor host; show it on the main window when possible.
            if (f.Modal && f.Owner != null && f.Owner.Visible) f = f.Owner;
            string msg = string.IsNullOrWhiteSpace(caption) || text.StartsWith(caption, StringComparison.OrdinalIgnoreCase) ? text : caption + " — " + text;
            msg = msg.Replace("\r", "").Replace("\n\n", " ").Replace("\n", " ").Trim();
            if (msg.Length > 260) return false; // long explanations stay in a window so they can be read
            ToastHelper.ShowToast(f, msg, ToastType.Info);
            return true;
        }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (buttons == MessageBoxButtons.OK && icon == MessageBoxIcon.Information && TryToast(null, text, caption)) return DialogResult.OK;
            return MessageBox.Show(text, caption, buttons, icon);
        }
        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (buttons == MessageBoxButtons.OK && icon == MessageBoxIcon.Information && TryToast(owner, text, caption)) return DialogResult.OK;
            return owner == null ? MessageBox.Show(text, caption, buttons, icon) : MessageBox.Show(owner, text, caption, buttons, icon);
        }
    }
    /// <summary>An on/off switch.</summary>
    public sealed class ToggleSwitch : Control
    {
        private bool on, hover;
        public event EventHandler? CheckedChanged;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Checked { get => on; set { if (on == value) return; on = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); } }
        public void SetQuietly(bool v) { on = v; Invalidate(); }
        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            Size = new Size(46, 26);
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Size = new Size(S(46), S(26)); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); if (Enabled) Checked = !Checked; Focus(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode is Keys.Space or Keys.Enter) { Checked = !Checked; e.Handled = true; } }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color back = Parent?.BackColor ?? AppTheme.Surface;
            g.Clear(back.A == 255 ? back : AppTheme.Surface);
            var track = new Rectangle(1, 1, Width - 3, Height - 3);
            Color fill = on ? (hover ? AppTheme.AccentHover : AppTheme.Accent) : (hover ? AppTheme.BorderStrong : AppTheme.Border);
            if (!Enabled) fill = AppTheme.SurfaceAlt;
            using (var p = Gfx.RoundedRect(track, track.Height / 2))
            {
                using var b = new SolidBrush(fill);
                g.FillPath(b, p);
                if (Focused) { using var fp = new Pen(AppTheme.FocusRing, 1.5f); g.DrawPath(fp, p); }
            }
            int d = track.Height - S(6);
            int x = on ? track.Right - d - S(3) : track.X + S(3);
            using (var kb = new SolidBrush(Color.White)) g.FillEllipse(kb, x, track.Y + S(3), d, d);
        }
    }
}
