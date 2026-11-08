using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>A control that draws and sizes itself (custom-painted): <see cref="UiZoom"/> tells it to re-measure instead of scaling it from outside.</summary>
    public interface IZoomAware { void ZoomChanged(); }

    /// <summary>
    /// "Display size": makes the text and controls of a screen bigger or smaller (80 – 150 %) without touching Windows scaling.
    /// Pure Windows Forms. <see cref="Apply"/> always computes from the control's ORIGINAL (remembered) values, so zooming never compounds and 100 % restores
    /// the exact original sizes. It changes fonts, fixed sizes, grid row / header heights, table row / column sizes, padding and margins only:
    /// never a value, a rule, a calculation or a printout. The choice is remembered per PC (per Windows user), not in the shared database.
    /// </summary>
    public static class UiZoom
    {
        public static readonly float[] Steps = { 0.8f, 0.9f, 1.0f, 1.1f, 1.25f, 1.5f };
        private static float factor = 1f;
        /// <summary>False in tests: the choice is not written to this PC's settings file.</summary>
        public static bool PersistToDisk { get; set; } = true;
        private static bool loaded;
        /// <summary>Current factor, 0.8 – 1.5 (1.0 = the normal size).</summary>
        public static float Factor { get { if (!loaded) Load(); return factor; } }
        /// <summary>Raised after the factor changed (on the caller's thread).</summary>
        public static event Action? Changed;

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MJ Billones Transport", "TDMS", "ui-zoom.txt");

        public static void Load()
        {
            loaded = true;
            try
            {
                if (File.Exists(FilePath) && float.TryParse(File.ReadAllText(FilePath).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                    factor = Nearest(f);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); factor = 1f; }
        }

        private static float Nearest(float f) => Steps.OrderBy(s => Math.Abs(s - f)).First();

        public static void Set(float f)
        {
            if (!loaded) { if (PersistToDisk) Load(); else loaded = true; }
            f = Nearest(f);
            if (Math.Abs(f - factor) < 0.001f) return;
            factor = f;
            if (PersistToDisk) try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, f.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            Changed?.Invoke();
        }
        public static void StepUp() { int i = Array.IndexOf(Steps, Factor); if (i < Steps.Length - 1) Set(Steps[i + 1]); }
        public static void StepDown() { int i = Array.IndexOf(Steps, Factor); if (i > 0) Set(Steps[i - 1]); }
        public static void Reset() => Set(1f);
        public static string Label => $"{Math.Round(Factor * 100)}%";

        // ---------------------------------------------------------------- applying
        /// <summary>
        /// What a control looked like before zooming (Font, Size, ...) and what we last set it to (Last*). When the current value is not what we
        /// last set, some other code changed it meanwhile (for example a form opening resets a padding): that value becomes the new original,
        /// so zooming never overwrites later layout decisions with stale values.
        /// </summary>
        private sealed class Base
        {
            public Font? Font, LastFont;
            public Size Size, Min, LastSize, LastMin;
            public Padding Pad, Margin, LastPad, LastMargin;
            public int RowH, HeaderH, LastRowH, LastHeaderH;
            public Font? CellFont, HeaderFont, LastCellFont, LastHeaderFont;
            public (SizeType T, float W)[]? Rows, Cols, LastRows, LastCols;
            public bool Applied, FontAmbient;
        }
        private static readonly ConditionalWeakTable<Control, Base> bases = new();
        private static readonly Dictionary<(string, float, FontStyle, GraphicsUnit), Font> fonts = new();

        private static Font Scaled(Font b, float f)
        {
            var key = (b.FontFamily.Name, (float)Math.Round(b.Size * f, 2), b.Style, b.Unit);
            lock (fonts)
            {
                if (!fonts.TryGetValue(key, out var font)) fonts[key] = font = new Font(b.FontFamily, key.Item2, b.Style, b.Unit);
                return font;
            }
        }
        private static int S(int v, float f) => (int)Math.Round(v * f);
        private static Size S(Size s, float f) => new(S(s.Width, f), S(s.Height, f));
        private static Padding S(Padding p, float f) => new(S(p.Left, f), S(p.Top, f), S(p.Right, f), S(p.Bottom, f));

        /// <summary>Scales <paramref name="root"/> and everything inside it to the current display size.</summary>
        public static void Apply(Control? root)
        {
            if (root == null || root.IsDisposed) return;
            using (new RedrawFreeze(root))
            {
                root.SuspendLayout();
                try { Walk(root, Factor, true); }
                finally { root.ResumeLayout(true); }
            }
        }

        private static bool NoRecurse(Control c) => c is DataGridView or NumericUpDown or DomainUpDown or ComboBox or DateTimePicker or TextBoxBase or ScrollBar;

        private static void Walk(Control c, float f, bool isRoot)
        {
            if (c is IZoomAware za) { try { za.ZoomChanged(); } catch (Exception ex) { ErrLog.Swallowed(ex); } return; }
            // A container with AutoScaleMode.Font rescales ITS CHILDREN whenever its font changes, which would scale everything twice. Zoom does the scaling itself.
            if (c is ContainerControl cc && cc.AutoScaleMode != AutoScaleMode.None) cc.AutoScaleMode = AutoScaleMode.None;
            var g0 = c as DataGridView;
            var b = bases.GetValue(c, ctl => new Base
            {
                FontAmbient = ctl.Parent != null && ReferenceEquals(ctl.Font, ctl.Parent.Font), // follows its parent's font: the parent's font is scaled instead
                Font = ctl.Font, LastFont = ctl.Font, Size = ctl.Size, LastSize = ctl.Size, Min = ctl.MinimumSize, LastMin = ctl.MinimumSize,
                Pad = ctl.Padding, LastPad = ctl.Padding, Margin = ctl.Margin, LastMargin = ctl.Margin,
                RowH = g0?.RowTemplate.Height ?? 0, LastRowH = g0?.RowTemplate.Height ?? 0, HeaderH = g0?.ColumnHeadersHeight ?? 0, LastHeaderH = g0?.ColumnHeadersHeight ?? 0,
                CellFont = g0?.DefaultCellStyle.Font, LastCellFont = g0?.DefaultCellStyle.Font, HeaderFont = g0?.ColumnHeadersDefaultCellStyle.Font, LastHeaderFont = g0?.ColumnHeadersDefaultCellStyle.Font,
                Rows = (ctl as TableLayoutPanel)?.RowStyles.Cast<RowStyle>().Select(r => (r.SizeType, r.Height)).ToArray(),
                Cols = (ctl as TableLayoutPanel)?.ColumnStyles.Cast<ColumnStyle>().Select(r => (r.SizeType, r.Width)).ToArray(),
                LastRows = (ctl as TableLayoutPanel)?.RowStyles.Cast<RowStyle>().Select(r => (r.SizeType, r.Height)).ToArray(),
                LastCols = (ctl as TableLayoutPanel)?.ColumnStyles.Cast<ColumnStyle>().Select(r => (r.SizeType, r.Width)).ToArray()
            });
            if (c is TableLayoutPanel tl0 && b.Rows != null && b.Cols != null)
            {
                // a form opening / closing changes which column is Absolute or Percent: whatever it is now is the new original
                var curRows = tl0.RowStyles.Cast<RowStyle>().Select(r => (r.SizeType, r.Height)).ToArray();
                var curCols = tl0.ColumnStyles.Cast<ColumnStyle>().Select(r => (r.SizeType, r.Width)).ToArray();
                if (!curRows.SequenceEqual(b.LastRows!)) { b.Rows = curRows; b.LastRows = curRows; }
                if (!curCols.SequenceEqual(b.LastCols!)) { b.Cols = curCols; b.LastCols = curCols; }
            }
            // Something else changed it since we last set it: that is the new original.
            bool scaleFont = isRoot || !b.FontAmbient;
            if (scaleFont && !ReferenceEquals(c.Font, b.LastFont)) { b.Font = c.Font; b.LastFont = c.Font; }
            // Only the dimension we control counts as "changed by someone else" (a docked control's other dimension is set by the layout all the time).
            bool stretchW = c.Dock == DockStyle.None && (c.Anchor & AnchorStyles.Left) != 0 && (c.Anchor & AnchorStyles.Right) != 0;
            bool stretchH = c.Dock == DockStyle.None && (c.Anchor & AnchorStyles.Top) != 0 && (c.Anchor & AnchorStyles.Bottom) != 0;
            bool useW = c.Dock is DockStyle.None or DockStyle.Left or DockStyle.Right && !stretchW;
            bool useH = c.Dock is DockStyle.None or DockStyle.Top or DockStyle.Bottom && !stretchH;
            useW = useW && !stretchW; useH = useH && !stretchH;
            if (useW && c.Width != b.LastSize.Width) { b.Size = new Size(c.Width, b.Size.Height); b.LastSize = new Size(c.Width, b.LastSize.Height); }
            if (useH && c.Height != b.LastSize.Height) { b.Size = new Size(b.Size.Width, c.Height); b.LastSize = new Size(b.LastSize.Width, c.Height); }
            if (c.MinimumSize != b.LastMin) { b.Min = c.MinimumSize; b.LastMin = c.MinimumSize; }
            if (c.Padding != b.LastPad) { b.Pad = c.Padding; b.LastPad = c.Padding; }
            if (c.Margin != b.LastMargin) { b.Margin = c.Margin; b.LastMargin = c.Margin; }
            if (g0 != null)
            {
                if (g0.RowTemplate.Height != b.LastRowH) { b.RowH = g0.RowTemplate.Height; b.LastRowH = b.RowH; }
                if (g0.ColumnHeadersHeight != b.LastHeaderH) { b.HeaderH = g0.ColumnHeadersHeight; b.LastHeaderH = b.HeaderH; }
                if (!ReferenceEquals(g0.DefaultCellStyle.Font, b.LastCellFont)) { b.CellFont = g0.DefaultCellStyle.Font; b.LastCellFont = b.CellFont; }
                if (!ReferenceEquals(g0.ColumnHeadersDefaultCellStyle.Font, b.LastHeaderFont)) { b.HeaderFont = g0.ColumnHeadersDefaultCellStyle.Font; b.LastHeaderFont = b.HeaderFont; }
            }
            if (Math.Abs(f - 1f) < 0.001f && !b.Applied) { if (!NoRecurse(c)) foreach (Control child in c.Controls) Walk(child, f, false); return; } // at 100 % nothing was changed yet: only remember the originals
            b.Applied = true;
            if (scaleFont && b.Font != null) { c.Font = Scaled(b.Font, f); b.LastFont = c.Font; }
            if (!isRoot)
            {
                c.Padding = S(b.Pad, f); b.LastPad = c.Padding;
                c.Margin = S(b.Margin, f); b.LastMargin = c.Margin;
                if (!c.AutoSize && c is not Form)
                {
                    if (useW) { c.Width = S(b.Size.Width, f); b.LastSize = new Size(c.Width, b.LastSize.Height); }
                    if (useH) { c.Height = S(b.Size.Height, f); b.LastSize = new Size(b.LastSize.Width, c.Height); }
                }
                c.MinimumSize = S(b.Min, f); b.LastMin = c.MinimumSize;
            }
            if (g0 != null)
            {
                if (g0.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize && b.HeaderH > 0) { g0.ColumnHeadersHeight = S(b.HeaderH, f); b.LastHeaderH = g0.ColumnHeadersHeight; }
                if (b.RowH > 0)
                {
                    g0.RowTemplate.Height = S(b.RowH, f); b.LastRowH = g0.RowTemplate.Height;
                    foreach (DataGridViewRow r in g0.Rows) if (!r.IsNewRow) r.Height = S(b.RowH, f);
                }
                if (b.CellFont != null) { g0.DefaultCellStyle.Font = Scaled(b.CellFont, f); b.LastCellFont = g0.DefaultCellStyle.Font; }
                if (b.HeaderFont != null) { g0.ColumnHeadersDefaultCellStyle.Font = Scaled(b.HeaderFont, f); b.LastHeaderFont = g0.ColumnHeadersDefaultCellStyle.Font; }
            }
            else if (c is TableLayoutPanel t && b.Rows != null && b.Cols != null)
            {
                for (int i = 0; i < t.RowStyles.Count && i < b.Rows.Length; i++) if (t.RowStyles[i].SizeType == SizeType.Absolute && b.Rows[i].T == SizeType.Absolute) t.RowStyles[i].Height = b.Rows[i].W * f;
                for (int i = 0; i < t.ColumnStyles.Count && i < b.Cols.Length; i++) if (t.ColumnStyles[i].SizeType == SizeType.Absolute && b.Cols[i].T == SizeType.Absolute) t.ColumnStyles[i].Width = b.Cols[i].W * f;
                b.LastRows = t.RowStyles.Cast<RowStyle>().Select(r => (r.SizeType, r.Height)).ToArray();
                b.LastCols = t.ColumnStyles.Cast<ColumnStyle>().Select(r => (r.SizeType, r.Width)).ToArray();
            }
            if (NoRecurse(c)) return;
            foreach (Control child in c.Controls) Walk(child, f, false);
        }
    }
}
