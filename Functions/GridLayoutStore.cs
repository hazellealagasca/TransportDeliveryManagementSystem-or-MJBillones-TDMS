using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One column of a customizable table: built-in or user-defined ("custom").</summary>
    public sealed class GridColumnPref
    {
        public string Key { get; set; } = "";
        public string Header { get; set; } = "";
        public string DefaultHeader { get; set; } = "";
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
        /// <summary>Relative width (fill weight). 0 means "use the built-in width".</summary>
        public float Weight { get; set; }
        public float DefaultWeight { get; set; } = 100;
        public bool Custom { get; set; }
        /// <summary>Locked columns can be renamed and moved but not hidden (e.g. the company name).</summary>
        public bool Locked { get; set; }
        public GridColumnPref Clone() => (GridColumnPref)MemberwiseClone();
    }
    /// <summary>
    /// Saves and applies table layouts (column order, visibility, names, widths and custom columns).
    /// Layouts are stored in System Settings so they survive restarts and apply to every user of the system.
    /// </summary>
    public static class GridLayoutStore
    {
        public const string CustomPrefix = "cf_";
        private static string SettingKey(string gridKey) => $"GridLayout.{gridKey}";
        /// <summary>The built-in columns of a grid, as the defaults the user can always reset to.</summary>
        public static List<GridColumnPref> DefaultsFrom(DataGridView grid, IEnumerable<string>? lockedKeys = null, IEnumerable<string>? excludeKeys = null)
        {
            var locked = new HashSet<string>(lockedKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var exclude = new HashSet<string>(excludeKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            int i = 0;
            return grid.Columns.Cast<DataGridViewColumn>()
                .Where(c => !exclude.Contains(c.Name) && !c.Name.StartsWith(CustomPrefix, StringComparison.Ordinal))
                .OrderBy(c => c.DisplayIndex)
                .Select(c => new GridColumnPref
                {
                    Key = c.Name,
                    Header = c.HeaderText,
                    DefaultHeader = c.HeaderText,
                    Visible = c.Visible,
                    Order = i++,
                    Weight = c.FillWeight,
                    DefaultWeight = c.FillWeight,
                    Locked = locked.Contains(c.Name)
                }).ToList();
        }
        /// <summary>The saved layout merged with the defaults (new built-in columns are added, removed ones dropped).</summary>
        public static List<GridColumnPref> Load(string gridKey, List<GridColumnPref> defaults)
        {
            List<GridColumnPref>? saved = null;
            try
            {
                string raw = MJdatabase.GetSystemSetting(SettingKey(gridKey), "");
                if (!string.IsNullOrWhiteSpace(raw)) saved = JsonSerializer.Deserialize<List<GridColumnPref>>(raw);
            }
            catch { saved = null; }
            if (saved == null || saved.Count == 0) return defaults.Select(d => d.Clone()).ToList();
            var result = new List<GridColumnPref>();
            foreach (var s in saved.OrderBy(x => x.Order))
            {
                if (s.Custom)
                {
                    if (string.IsNullOrWhiteSpace(s.Key) || !s.Key.StartsWith(CustomPrefix, StringComparison.Ordinal)) continue;
                    if (s.DefaultWeight <= 0) s.DefaultWeight = 100;
                    result.Add(s);
                    continue;
                }
                var d = defaults.FirstOrDefault(x => string.Equals(x.Key, s.Key, StringComparison.OrdinalIgnoreCase));
                if (d == null) continue;
                var merged = d.Clone();
                merged.Header = string.IsNullOrWhiteSpace(s.Header) ? d.Header : s.Header;
                merged.Visible = d.Locked || s.Visible;
                merged.Weight = s.Weight > 0 ? s.Weight : d.Weight;
                result.Add(merged);
            }
            foreach (var d in defaults)
                if (!result.Any(r => string.Equals(r.Key, d.Key, StringComparison.OrdinalIgnoreCase))) result.Add(d.Clone());
            for (int i = 0; i < result.Count; i++) result[i].Order = i;
            return result;
        }
        public static void Save(string gridKey, List<GridColumnPref> prefs)
        {
            try
            {
                for (int i = 0; i < prefs.Count; i++) prefs[i].Order = i;
                MJdatabase.SetSystemSetting(SettingKey(gridKey), JsonSerializer.Serialize(prefs), "Table layout");
            }
            catch { }
        }
        public static void Reset(string gridKey)
        {
            try { MJdatabase.SetSystemSetting(SettingKey(gridKey), "", "Table layout"); } catch { }
        }
        /// <summary>
        /// Applies a layout to a grid: adds/removes custom columns (built by <paramref name="customFactory"/>),
        /// then sets each column's name, visibility, width and position. Columns not in the layout
        /// (such as a Delete button column) are kept at the end.
        /// </summary>
        public static void Apply(DataGridView grid, List<GridColumnPref> prefs, Func<GridColumnPref, DataGridViewColumn> customFactory)
        {
            foreach (var col in grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Name.StartsWith(CustomPrefix, StringComparison.Ordinal)).ToList())
            {
                if (!prefs.Any(p => p.Custom && p.Key == col.Name)) grid.Columns.Remove(col);
            }
            foreach (var p in prefs.Where(p => p.Custom))
            {
                if (grid.Columns.Contains(p.Key)) continue;
                var c = customFactory(p);
                c.Name = p.Key;
                grid.Columns.Add(c);
            }
            int idx = 0;
            foreach (var p in prefs.OrderBy(x => x.Order))
            {
                if (!grid.Columns.Contains(p.Key)) continue;
                var c = grid.Columns[p.Key]!;
                c.HeaderText = p.Header;
                c.Visible = p.Locked || p.Visible;
                if (p.Weight > 0) c.FillWeight = Math.Clamp(p.Weight, 10f, 1000f);
                c.DisplayIndex = Math.Min(idx++, grid.Columns.Count - 1);
            }
            foreach (var c in grid.Columns.Cast<DataGridViewColumn>().Where(c => !prefs.Any(p => p.Key == c.Name)).ToList())
            {
                c.DisplayIndex = grid.Columns.Count - 1;
            }
        }
        // ------------------------------------------------------------------ Custom field values
        public static Dictionary<string, string> ReadValues(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
            try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(); }
            catch { return new Dictionary<string, string>(); }
        }
        public static string WriteValues(Dictionary<string, string> values)
        {
            var clean = values.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value.Trim());
            return JsonSerializer.Serialize(clean);
        }
        public static string NewCustomKey(IEnumerable<GridColumnPref> existing)
        {
            string key;
            var rnd = new Random();
            do { key = CustomPrefix + rnd.Next(100000, 999999); } while (existing.Any(e => e.Key == key));
            return key;
        }
    }
}
