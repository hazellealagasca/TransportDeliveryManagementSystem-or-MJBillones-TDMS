using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>A labelled value on a printout. The label is editable; values marked FromData come from the record.</summary>
    public sealed class PrintField
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public bool On { get; set; } = true;
        public bool FromData { get; set; }
        public bool Multiline { get; set; }
        public PrintField() { }
        public PrintField(string key, string label, string value, bool fromData = true, bool on = true, bool multiline = false)
        { Key = key; Label = label; Value = value ?? ""; FromData = fromData; On = on; Multiline = multiline; }
    }
    /// <summary>A signature box: caption ("Prepared by"), the person's name and their role.</summary>
    public sealed class PrintSig
    {
        public string Caption { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool On { get; set; } = true;
        public bool FromData { get; set; }
        public PrintSig() { }
        public PrintSig(string caption, string name, string role, bool fromData = false, bool on = true)
        { Caption = caption; Name = name ?? ""; Role = role ?? ""; FromData = fromData; On = on; }
    }
    /// <summary>An include/exclude switch for a part of the printout or a table column.</summary>
    public sealed class PrintChoice
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public bool On { get; set; } = true;
        public PrintChoice() { }
        public PrintChoice(string key, string label, bool on = true) { Key = key; Label = label; On = on; }
    }
    /// <summary>Everything the person can change before printing a form.</summary>
    public sealed class PrintOptions
    {
        public string FormKey { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string CopyLabel { get; set; } = "";
        public string Watermark { get; set; } = "";
        public string Paper { get; set; } = "Letter";
        public string Orientation { get; set; } = "Portrait";
        public int FontPct { get; set; } = 100;
        public string Margins { get; set; } = "Normal";
        public string Tone { get; set; } = "Black & white";
        public bool ShowLogo { get; set; } = true;
        public List<PrintField> Header { get; set; } = new();
        public List<PrintField> Fields { get; set; } = new();
        public List<PrintChoice> Sections { get; set; } = new();
        public List<PrintChoice> Columns { get; set; } = new();
        public List<PrintSig> Signatures { get; set; } = new();
        public string Notes { get; set; } = "";
        public string Footer { get; set; } = "";
        public bool HasRowScope { get; set; }
        public bool SelectedRowsOnly { get; set; }
        public int SelectedRowCount { get; set; }
        public bool HasBlankRows { get; set; }
        public int BlankRows { get; set; } = 8;
        public bool On(string key) => Sections.FirstOrDefault(s => s.Key == key)?.On ?? true;
        public bool Col(string key) => Columns.FirstOrDefault(s => s.Key == key)?.On ?? true;
        public PrintField? Field(string key) => Fields.FirstOrDefault(f => f.Key == key) ?? Header.FirstOrDefault(f => f.Key == key);
        public string F(string key) => Field(key)?.Value ?? "";
        public bool FOn(string key) { var f = Field(key); return f != null && f.On; }
        public string L(string key) => Field(key)?.Label ?? "";
        public PrintOptions Clone() => JsonSerializer.Deserialize<PrintOptions>(JsonSerializer.Serialize(this))!;
    }
    /// <summary>A printable form: its defaults (filled from the record) and how to draw its body.</summary>
    public sealed class PrintFormDef
    {
        public string Key = "";
        public string Name = "";
        public string FileName = "Printout";
        public PrintOptions Defaults = new();
        public Func<PrintOptions, string> Body = _ => "";
        public string ExtraCss = "";
    }
    /// <summary>Remembers each form's layout choices (not the record's data) in System Settings.</summary>
    public static class PrintStore
    {
        private static string Key(string form) => $"PrintLayout.{form}";
        public static PrintOptions Load(PrintFormDef def)
        {
            var o = def.Defaults.Clone();
            try
            {
                string raw = Database.MJdatabase.GetSystemSetting(Key(def.Key), "");
                if (string.IsNullOrWhiteSpace(raw)) return o;
                var s = JsonSerializer.Deserialize<PrintOptions>(raw);
                if (s == null) return o;
                o.Title = string.IsNullOrWhiteSpace(s.Title) ? o.Title : s.Title;
                o.CopyLabel = s.CopyLabel; o.Watermark = s.Watermark; o.Paper = s.Paper; o.Orientation = s.Orientation;
                o.FontPct = s.FontPct is >= 70 and <= 140 ? s.FontPct : 100; o.Margins = s.Margins; o.Tone = s.Tone; o.ShowLogo = s.ShowLogo;
                o.Footer = s.Footer;
                if (!string.IsNullOrWhiteSpace(s.Notes) && string.IsNullOrWhiteSpace(o.Notes)) o.Notes = s.Notes;
                if (o.HasBlankRows && s.BlankRows is >= 0 and <= 40) o.BlankRows = s.BlankRows;
                MergeFields(o.Header, s.Header);
                MergeFields(o.Fields, s.Fields);
                foreach (var c in o.Sections) { var m = s.Sections.FirstOrDefault(x => x.Key == c.Key); if (m != null) c.On = m.On; }
                foreach (var c in o.Columns) { var m = s.Columns.FirstOrDefault(x => x.Key == c.Key); if (m != null) c.On = m.On; }
                if (s.Signatures.Count > 0)
                {
                    var merged = new List<PrintSig>();
                    for (int i = 0; i < s.Signatures.Count; i++)
                    {
                        var saved = s.Signatures[i];
                        var def0 = i < o.Signatures.Count ? o.Signatures[i] : null;
                        merged.Add(new PrintSig(saved.Caption, def0 != null && def0.FromData && !string.IsNullOrWhiteSpace(def0.Name) ? def0.Name : saved.Name, saved.Role, def0?.FromData ?? false, saved.On));
                    }
                    o.Signatures = merged;
                }
            }
            catch { }
            return o;
        }
        private static void MergeFields(List<PrintField> target, List<PrintField> saved)
        {
            foreach (var f in target)
            {
                var m = saved.FirstOrDefault(x => x.Key == f.Key);
                if (m == null) continue;
                f.On = m.On;
                if (!string.IsNullOrWhiteSpace(m.Label)) f.Label = m.Label;
                if (!f.FromData) f.Value = m.Value;
            }
        }
        public static void Save(PrintOptions o)
        {
            try
            {
                var copy = o.Clone();
                foreach (var f in copy.Fields.Where(f => f.FromData)) f.Value = "";
                copy.Notes = "";
                Database.MJdatabase.SetSystemSetting(Key(o.FormKey), JsonSerializer.Serialize(copy), "Print layout");
            }
            catch { }
        }
        public static void Reset(string formKey)
        {
            try { Database.MJdatabase.SetSystemSetting(Key(formKey), "", "Print layout"); } catch { }
        }
    }
    /// <summary>The shared page: company header, title block, the form's body, notes, signatures and footer.</summary>
    public static class PrintKit
    {
        public static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");
        public static string Money(decimal v) => $"₱{v:N2}";
        public static string Setting(string key, string fallback)
        {
            try { var v = Database.MJdatabase.GetSystemSetting(key, fallback); return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim(); } catch { return fallback; }
        }
        public static string OwnerName => Setting("CompanyOwner", "Marie Ann J. Billones");
        public static string CurrentUserName
        {
            get
            {
                var u = SessionContext.ActiveUser;
                string n = (u?.FullName ?? u?.Username ?? "").Trim();
                return System.Text.RegularExpressions.Regex.Replace(n, @"\s*\([^)]*\)\s*$", "");
            }
        }
        /// <summary>The editable company header lines every form starts with.</summary>
        public static List<PrintField> DefaultHeader() => new()
        {
            new PrintField("company", "Company name", Setting("CompanyName", "M.J. BILLONES TRANSPORT SERVICE"), false),
            new PrintField("tagline", "Tagline", Setting("CompanyTagline", "24/7 Delivery & Transport Hauling Services"), false),
            new PrintField("address", "Address", Setting("CompanyAddress", "Jasmin St., San Francisco, Tarlac City"), false),
            new PrintField("contact", "Contact", Setting("CompanyContact", ""), false),
            new PrintField("tin", "TIN", Setting("CompanyTin", "708-839-875-000"), false),
            new PrintField("owner", "Proprietor", OwnerName, false, on: false)
        };
        public static string LogoDataUri()
        {
            try
            {
                byte[]? custom = BrandAssets.CustomLogoPngBytes();
                if (custom != null && custom.Length > 0) return "data:image/png;base64," + Convert.ToBase64String(custom);
                foreach (var dir in new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() })
                {
                    string p = Path.Combine(dir, "Assets", "logo.png");
                    if (File.Exists(p)) return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(p));
                }
            }
            catch { }
            return "";
        }
        // ---------------------------------------------------------------- building blocks for form bodies
        /// <summary>Label/value grid from the form's fields (only those switched on), <paramref name="cols"/> pairs per row.</summary>
        public static string FieldGrid(PrintOptions o, int cols, params string[] keys)
        {
            var list = keys.Select(k => o.Field(k)).Where(f => f != null && f.On).Select(f => f!).ToList();
            if (list.Count == 0) return "";
            var sb = new StringBuilder("<table class='kv'>");
            for (int i = 0; i < list.Count; i += cols)
            {
                sb.Append("<tr>");
                for (int c = 0; c < cols; c++)
                {
                    if (i + c < list.Count)
                    {
                        var f = list[i + c];
                        sb.Append($"<td class='k'>{Esc(f.Label)}</td><td class='v'>{Multi(f.Value)}</td>");
                    }
                    else sb.Append("<td class='k'></td><td class='v'></td>");
                }
                sb.Append("</tr>");
            }
            return sb.Append("</table>").ToString();
        }
        public static string Multi(string? s) => Esc(s).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
        public static string SectionTitle(string text) => $"<div class='sec-title'>{Esc(text)}</div>";
        /// <summary>A table whose columns follow the form's column switches. Each column: key, header, css class (num/center/'').</summary>
        public static string Table(PrintOptions o, IEnumerable<(string Key, string Header, string Css)> columns, IEnumerable<IDictionary<string, string>> rows, IDictionary<string, string>? totals = null, int blankRows = 0, string cssClass = "grid")
        {
            var cols = columns.Where(c => o.Col(c.Key)).ToList();
            if (cols.Count == 0) return "";
            var sb = new StringBuilder($"<table class='{cssClass}'><thead><tr>");
            foreach (var c in cols) sb.Append($"<th class='{c.Css}'>{Esc(c.Header)}</th>");
            sb.Append("</tr></thead><tbody>");
            int n = 0;
            foreach (var r in rows)
            {
                n++;
                sb.Append("<tr>");
                foreach (var c in cols) sb.Append($"<td class='{c.Css}'>{(r.TryGetValue(c.Key, out var v) ? v : "")}</td>");
                sb.Append("</tr>");
            }
            for (int i = 0; i < blankRows; i++)
            {
                sb.Append("<tr class='blank'>");
                foreach (var c in cols) sb.Append("<td>&nbsp;</td>");
                sb.Append("</tr>");
            }
            if (n == 0 && blankRows == 0) sb.Append($"<tr><td colspan='{cols.Count}' class='empty'>No records.</td></tr>");
            sb.Append("</tbody>");
            if (totals != null)
            {
                sb.Append("<tfoot><tr>");
                foreach (var c in cols) sb.Append($"<td class='{c.Css}'>{(totals.TryGetValue(c.Key, out var v) ? v : "")}</td>");
                sb.Append("</tr></tfoot>");
            }
            return sb.Append("</table>").ToString();
        }
        /// <summary>Right-aligned summary box: label/amount lines, the last one emphasised.</summary>
        public static string Summary(IEnumerable<(string Label, string Value)> lines, string title = "")
        {
            var list = lines.ToList();
            var sb = new StringBuilder("<table class='summary'>");
            if (title.Length > 0) sb.Append($"<tr><th colspan='2'>{Esc(title)}</th></tr>");
            for (int i = 0; i < list.Count; i++)
                sb.Append($"<tr class='{(i == list.Count - 1 ? "grand" : "")}'><td>{Esc(list[i].Label)}</td><td class='num'>{Esc(list[i].Value)}</td></tr>");
            return sb.Append("</table>").ToString();
        }
        public static string Stats(IEnumerable<(string Label, string Value)> items)
        {
            var list = items.ToList();
            if (list.Count == 0) return "";
            var sb = new StringBuilder("<table class='stats'><tr>");
            foreach (var (l, v) in list) sb.Append($"<td><div class='sv'>{Esc(v)}</div><div class='sl'>{Esc(l)}</div></td>");
            return sb.Append("</tr></table>").ToString();
        }
        public static string Checks(IEnumerable<(string Label, bool Ticked)> items)
            => string.Join("&nbsp;&nbsp;&nbsp;&nbsp;", items.Select(i => $"<span class='chk'>{(i.Ticked ? "☑" : "☐")} {Esc(i.Label)}</span>"));
        public static Dictionary<string, string> Row(params (string Key, object? Value)[] cells)
            => cells.ToDictionary(c => c.Key, c => c.Value is string s ? (s.StartsWith("<") ? s : Esc(s)) : Esc(Convert.ToString(c.Value, CultureInfo.CurrentCulture)));
        // ---------------------------------------------------------------- the page
        public static string Page(PrintFormDef def, PrintOptions o, bool autoPrint, bool preview = false)
        {
            string body;
            try { body = def.Body(o); }
            catch (Exception ex) { body = $"<p class='empty'>Couldn't build this form: {Esc(ex.Message)}</p>"; }
            bool color = o.Tone == "Color";
            string ink = "#000000", accent = color ? "#0f3d6e" : "#000000", soft = color ? "#e8eef6" : "#efefef", line = color ? "#9fb3c8" : "#555555";
            string paper = o.Paper switch { "A4" => "A4", "Legal" => "legal", "Long (8.5 x 13)" => "8.5in 13in", _ => "letter" };
            string orient = o.Orientation == "Landscape" ? " landscape" : "";
            if (paper == "8.5in 13in" && o.Orientation == "Landscape") { paper = "13in 8.5in"; orient = ""; }
            string margin = o.Margins switch { "Narrow" => "7mm 8mm", "Wide" => "18mm 20mm", _ => "12mm 13mm" };
            double basePx = 12.5 * o.FontPct / 100.0;
            var h = o.Header.ToDictionary(f => f.Key);
            string HV(string k) => h.TryGetValue(k, out var f) && f.On && !string.IsNullOrWhiteSpace(f.Value) ? f.Value : "";
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><meta http-equiv='X-UA-Compatible' content='IE=edge'/>");
            sb.Append($"<title>{Esc(o.Title)}</title><style>");
            sb.Append($"@page {{ size: {paper}{orient}; margin: {margin}; }}");
            sb.Append($"html, body {{ background:#ffffff; }} body {{ font-family: 'Segoe UI', Arial, Helvetica, sans-serif; font-size:{basePx.ToString("0.##", CultureInfo.InvariantCulture)}px; color:{ink}; margin:{(preview ? "18px" : "16px")}; line-height:1.35; }}");
            sb.Append(".sheet { position:relative; }");
            sb.Append($".hdr {{ width:100%; border-collapse:collapse; border-bottom:2px solid {accent}; margin-bottom:10px; }} .hdr td {{ vertical-align:middle; padding:0 0 8px 0; }}");
            sb.Append(".logo { max-height:58px; max-width:120px; margin-right:12px; }");
            sb.Append($".co {{ font-size:1.45em; font-weight:800; letter-spacing:.02em; text-transform:uppercase; color:{accent}; }} .co-sub {{ font-size:.9em; color:#222; margin-top:1px; }}");
            sb.Append($".doc {{ text-align:right; }} .doc-title {{ font-size:1.35em; font-weight:800; text-transform:uppercase; letter-spacing:.03em; color:{accent}; }} .doc-sub {{ font-size:.95em; font-weight:600; margin-top:2px; }}");
            sb.Append($".copy {{ display:inline-block; border:1.5px solid {ink}; padding:1px 8px; font-size:.8em; font-weight:800; letter-spacing:.08em; margin-bottom:4px; }}");
            sb.Append($".sec-title {{ font-size:.95em; font-weight:800; text-transform:uppercase; letter-spacing:.04em; color:{accent}; border-bottom:1px solid {line}; padding:2px 0 3px 0; margin:14px 0 6px 0; }}");
            sb.Append($"table.kv {{ width:100%; border-collapse:collapse; }} table.kv td {{ padding:3px 6px; vertical-align:top; border-bottom:1px dotted {line}; }} table.kv td.k {{ width:16%; font-weight:700; white-space:nowrap; color:#222; }} table.kv td.v {{ width:34%; }}");
            sb.Append($"table.grid {{ width:100%; border-collapse:collapse; margin-top:4px; border:1.5px solid {ink}; }} table.grid th {{ background:{(color ? accent : "#000")}; color:#fff; padding:5px 6px; font-size:.86em; text-transform:uppercase; border:1px solid {ink}; text-align:left; }}");
            sb.Append($"table.grid td {{ border:1px solid {line}; padding:4px 6px; font-size:.92em; vertical-align:top; }} table.grid tbody tr:nth-child(even) td {{ background:{soft}; }} table.grid tfoot td {{ font-weight:800; border-top:2px solid {ink}; background:#fff; }}");
            sb.Append("table.grid tr.blank td { height:20px; background:#fff; } .num { text-align:right !important; white-space:nowrap; } .center { text-align:center !important; } .empty { text-align:center; font-style:italic; color:#555; padding:10px; }");
            sb.Append($"table.summary {{ border-collapse:collapse; min-width:300px; margin-left:auto; margin-top:8px; border:1.5px solid {ink}; }} table.summary th {{ background:{soft}; text-align:left; padding:5px 8px; font-size:.86em; text-transform:uppercase; }} table.summary td {{ padding:4px 8px; border-bottom:1px solid {line}; }} table.summary tr.grand td {{ font-weight:800; font-size:1.12em; border-top:2px solid {ink}; }}");
            sb.Append($"table.stats {{ width:100%; border-collapse:separate; border-spacing:6px 0; margin:6px -6px; }} table.stats td {{ border:1.5px solid {ink}; text-align:center; padding:6px; }} .sv {{ font-size:1.35em; font-weight:800; }} .sl {{ font-size:.78em; text-transform:uppercase; font-weight:700; color:#333; }}");
            sb.Append($".note {{ border:1px solid {line}; background:{soft}; padding:6px 9px; margin-top:12px; font-size:.92em; }} .chk {{ white-space:nowrap; font-weight:600; }}");
            sb.Append(".sigs { width:100%; border-collapse:separate; border-spacing:18px 0; margin:34px -18px 0 -18px; page-break-inside:avoid; } .sigs td { vertical-align:bottom; text-align:center; width:25%; }");
            sb.Append($".sig-cap {{ text-align:left; font-size:.85em; font-weight:700; margin-bottom:26px; }} .sig-name {{ border-top:1.5px solid {ink}; padding-top:3px; font-weight:800; text-transform:uppercase; }} .sig-role {{ font-size:.82em; color:#333; }}");
            sb.Append($".foot {{ margin-top:16px; border-top:1px solid {line}; padding-top:5px; font-size:.78em; color:#333; }} .foot td {{ padding:0; }}");
            sb.Append(".wm { position:fixed; top:38%; left:0; width:100%; text-align:center; font-size:96px; font-weight:900; color:rgba(0,0,0,0.07); transform:rotate(-24deg); -ms-transform:rotate(-24deg); z-index:0; pointer-events:none; letter-spacing:.1em; }");
            sb.Append(".toolbar { text-align:right; margin-bottom:10px; } .toolbar button { background:#000; color:#fff; border:0; padding:8px 18px; font-weight:700; cursor:pointer; border-radius:4px; font-size:13px; }");
            sb.Append("@media print { .toolbar { display:none !important; } body { margin:0; } * { -webkit-print-color-adjust:exact !important; print-color-adjust:exact !important; } thead { display:table-header-group; } tr { page-break-inside:avoid; } }");
            sb.Append(def.ExtraCss);
            sb.Append("</style></head><body>");
            if (!preview) sb.Append("<div class='toolbar'><button onclick='window.print()'>🖨  Print / Save as PDF</button></div>");
            if (!string.IsNullOrWhiteSpace(o.Watermark)) sb.Append($"<div class='wm'>{Esc(o.Watermark.ToUpperInvariant())}</div>");
            sb.Append("<div class='sheet'>");
            // header
            string logo = o.ShowLogo ? LogoDataUri() : "";
            sb.Append("<table class='hdr'><tr>");
            sb.Append("<td style='width:60%'><table style='border-collapse:collapse'><tr>");
            if (logo.Length > 0) sb.Append($"<td style='padding:0;vertical-align:middle'><img class='logo' src='{logo}'/></td>");
            sb.Append("<td style='padding:0;vertical-align:middle'>");
            if (HV("company").Length > 0) sb.Append($"<div class='co'>{Esc(HV("company"))}</div>");
            if (HV("tagline").Length > 0) sb.Append($"<div class='co-sub'>{Esc(HV("tagline"))}</div>");
            if (HV("address").Length > 0) sb.Append($"<div class='co-sub'>{Esc(HV("address"))}</div>");
            var line2 = new List<string>();
            if (HV("contact").Length > 0) line2.Add($"{Esc(h["contact"].Label)}: {Esc(HV("contact"))}");
            if (HV("tin").Length > 0) line2.Add($"{Esc(h["tin"].Label)}: {Esc(HV("tin"))}");
            if (HV("owner").Length > 0) line2.Add($"{Esc(h["owner"].Label)}: {Esc(HV("owner"))}");
            if (line2.Count > 0) sb.Append($"<div class='co-sub'>{string.Join(" &nbsp;|&nbsp; ", line2)}</div>");
            sb.Append("</td></tr></table></td>");
            sb.Append("<td class='doc' style='width:40%'>");
            if (!string.IsNullOrWhiteSpace(o.CopyLabel)) sb.Append($"<div class='copy'>{Esc(o.CopyLabel.ToUpperInvariant())}</div>");
            sb.Append($"<div class='doc-title'>{Esc(o.Title)}</div>");
            if (!string.IsNullOrWhiteSpace(o.Subtitle)) sb.Append($"<div class='doc-sub'>{Multi(o.Subtitle)}</div>");
            sb.Append("</td></tr></table>");
            sb.Append(body);
            if (!string.IsNullOrWhiteSpace(o.Notes)) sb.Append($"<div class='note'><b>Notes:</b> {Multi(o.Notes)}</div>");
            var sigs = o.Signatures.Where(s => s.On).ToList();
            if (sigs.Count > 0 && o.On("signatures"))
            {
                for (int i = 0; i < sigs.Count; i += 4)
                {
                    sb.Append("<table class='sigs'><tr>");
                    foreach (var s in sigs.Skip(i).Take(4))
                        sb.Append($"<td><div class='sig-cap'>{Esc(s.Caption)}</div><div class='sig-name'>{(string.IsNullOrWhiteSpace(s.Name) ? "&nbsp;" : Esc(s.Name))}</div><div class='sig-role'>{Esc(s.Role)}&nbsp;</div></td>");
                    for (int pad = sigs.Skip(i).Take(4).Count(); pad < 4; pad++) sb.Append("<td></td>");
                    sb.Append("</tr></table>");
                }
            }
            if (o.On("printed") || !string.IsNullOrWhiteSpace(o.Footer))
            {
                sb.Append("<table class='foot' style='width:100%'><tr>");
                sb.Append($"<td>{Multi(o.Footer)}</td>");
                sb.Append(o.On("printed") ? $"<td style='text-align:right;white-space:nowrap'>Printed {DateTime.Now:MMM d, yyyy h:mm tt}{(CurrentUserName.Length > 0 ? " by " + Esc(CurrentUserName) : "")}</td>" : "<td></td>");
                sb.Append("</tr></table>");
            }
            sb.Append("</div>");
            if (autoPrint) sb.Append("<script>window.onload=function(){setTimeout(function(){window.print();},350);};</script>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
        /// <summary>Sections every form offers in addition to its own.</summary>
        public static List<PrintChoice> CommonSections() => new()
        {
            new PrintChoice("signatures", "Signature lines"),
            new PrintChoice("printed", "\"Printed on … by …\" line")
        };
        public static string AmountInWords(decimal amount)
        {
            long pesos = (long)Math.Floor(amount);
            int cents = (int)Math.Round((amount - pesos) * 100);
            string w = pesos == 0 ? "Zero" : Words(pesos);
            return $"{w} Peso{(pesos == 1 ? "" : "s")}" + (cents > 0 ? $" and {cents:00}/100" : " Only");
        }
        private static string Words(long n)
        {
            string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            if (n < 20) return ones[n];
            if (n < 100) return tens[n / 10] + (n % 10 > 0 ? "-" + ones[n % 10] : "");
            if (n < 1000) return ones[n / 100] + " Hundred" + (n % 100 > 0 ? " " + Words(n % 100) : "");
            foreach (var (div, name) in new[] { (1_000_000_000L, "Billion"), (1_000_000L, "Million"), (1_000L, "Thousand") })
                if (n >= div) return Words(n / div) + " " + name + (n % div > 0 ? " " + Words(n % div) : "");
            return n.ToString();
        }
    }
}
