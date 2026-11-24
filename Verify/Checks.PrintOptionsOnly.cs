using System.Text.Json;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>The print window only has print options; what a form shows is edited in Settings → Print layouts (the same window in layout mode).</summary>
public static partial class Checks
{
    private static PrintFormDef SoaDef() { var (soa, items) = PrintSample(SoaTypes.Normal); return PrintForms.StatementOfAccount(soa, items); }
    private static PrintStudioDialog ShowStudio(PrintFormDef def, PrintStudioMode mode)
    {
        var dlg = new PrintStudioDialog(def, mode) { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Opacity = 0 };
        dlg.Show(); Pump(500);
        return dlg;
    }
    private static List<Control> StudioControls(Form f) => f.Controls.OfType<Control>().SelectMany(Flat).ToList();

    [Check]
    public static string PrintWindow_PrintMode_HasOnlyPrintOptions_AndDropDowns()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            // a copy label saved earlier that is not in the standard list must still show
            var def = SoaDef();
            var saved = PrintStore.Load(def); saved.CopyLabel = "Office copy"; saved.Watermark = "Urgent"; PrintStore.Save(saved);
            using var dlg = ShowStudio(SoaDef(), PrintStudioMode.Print);
            var all = StudioControls(dlg);
            var texts = all.OfType<TextBox>().Where(t => t.Parent is not NumericUpDown).ToList();
            if (texts.Count > 0) bad.Add($"{texts.Count} text boxes in the print window ({string.Join(", ", texts.Select(t => "'" + t.Text + "'"))})");
            var groups = all.OfType<Label>().Select(l => l.Text).Where(t => t.Length > 0 && t == t.ToUpperInvariant() && t.Any(char.IsLetter)).ToList();
            if (groups.Count != 1 || groups[0] != "PRINT OPTIONS") bad.Add("groups: " + string.Join(", ", groups));
            foreach (var g in new[] { "Document", "Company header", "Details on the form", "Include", "Table columns", "Signature lines", "Notes & footer" })
                if (all.OfType<Label>().Any(l => l.Text.Equals(g, StringComparison.OrdinalIgnoreCase))) bad.Add("group still present: " + g);
            var boxes = all.OfType<CheckBox>().Select(c => c.Text).ToList();
            if (boxes.Count != 1 || !boxes[0].StartsWith("Remember")) bad.Add("check boxes: " + string.Join(" | ", boxes));
            var combos = all.OfType<ComboBox>().ToList();
            string Caption(ComboBox c) => c.Parent?.Controls.OfType<Label>().FirstOrDefault()?.Text ?? "";
            foreach (var cap in new[] { "Copy label", "Watermark", "Paper", "Orientation", "Text size", "Margins", "Style" })
                if (!combos.Any(c => Caption(c) == cap)) bad.Add("missing option: " + cap);
            foreach (var c in combos) if (c.DropDownStyle != ComboBoxStyle.DropDownList) bad.Add($"'{Caption(c)}' is not a drop-down list");
            var copy = combos.FirstOrDefault(c => Caption(c) == "Copy label"); var wm = combos.FirstOrDefault(c => Caption(c) == "Watermark");
            if (copy == null || !copy.Items.Contains("(none)") || !copy.Items.Contains("Office copy") || copy.Text != "Office copy") bad.Add("saved copy label not shown in the list");
            if (wm == null || !wm.Items.Contains("(none)") || !wm.Items.Contains("Urgent") || wm.Text != "Urgent") bad.Add("saved watermark not shown in the list");
            var names = all.OfType<Button>().Select(b => b.Text).ToList();
            if (!names.Any(n => n.Contains("Print")) || !names.Contains("Cancel") || names.Any(n => n.Contains("Save layout"))) bad.Add("buttons: " + string.Join(" | ", names));
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string PrintWindow_LayoutMode_ShowsLayoutGroups_NoValueEditing_NoPrintButtons()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            using var dlg = ShowStudio(SoaDef(), PrintStudioMode.Layout);
            var all = StudioControls(dlg);
            var labels = all.OfType<Label>().Select(l => l.Text.ToUpperInvariant()).ToList();
            foreach (var g in new[] { "Document", "Company header", "Details on the form", "Include", "Table columns", "Signature lines", "Notes & footer" })
                if (!labels.Contains(g.ToUpperInvariant())) bad.Add("missing group " + g);
            // a record value (the client on the statement) has no box; fixed text (payment instructions) does
            if (all.OfType<TextBox>().Any(t => t.Text == "Sample client")) bad.Add("a record value is editable");
            if (!all.OfType<TextBox>().Any(t => t.Text.StartsWith("Please make checks payable"))) bad.Add("fixed text is not editable");
            var names = all.OfType<Button>().Select(b => b.Text).ToList();
            if (!names.Contains("Save layout") || !names.Contains("Cancel") || names.Any(n => n.Contains("Print") || n.Contains("Save as"))) bad.Add("buttons: " + string.Join(" | ", names));
            if (!all.OfType<LinkLabel>().Any(l => l.Text == "Reset layout to standard")) bad.Add("no Reset layout to standard");
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string PrintLayout_SavedInLayoutMode_UsedByPrint_SurvivesPrintSaveAndPrintReset()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            const string custom = "My Custom Statement";
            string key = SoaDef().Key;
            PrintStore.Reset(key);
            // 1) layout mode: change the title, untick a column, Save layout
            using (var dlg = ShowStudio(SoaDef(), PrintStudioMode.Layout))
            {
                var all = StudioControls(dlg);
                var title = all.OfType<TextBox>().FirstOrDefault(t => !t.Multiline && t.Text == "Statement of Account");
                var fuel = all.OfType<CheckBox>().FirstOrDefault(c => c.Text == "Fuel surcharge");
                if (title == null || fuel == null) return "layout window has no title box / Fuel surcharge column box";
                title.Text = custom; fuel.Checked = false;
                all.OfType<Button>().First(b => b.Text == "Save layout").PerformClick(); Pump(200);
            }
            // 2) a later print of the real form uses it
            string Html(PrintOptions o) => PrintKit.Page(SoaDef(), o, false);
            void Expect(string when, PrintOptions o, string paper, string copy)
            {
                string h = Html(o);
                if (o.Title != custom || !h.Contains(custom)) bad.Add(when + ": title not the saved one");
                if (o.Col("fuel") || h.Contains("<th class='num red'>")) bad.Add(when + ": the unticked column is back");
                if (o.Paper != paper) bad.Add($"{when}: paper {o.Paper}, expected {paper}");
                if (o.CopyLabel != copy) bad.Add($"{when}: copy label '{o.CopyLabel}', expected '{copy}'");
            }
            var def = SoaDef();
            var o1 = PrintStore.Load(def);
            Expect("after Save layout", o1, def.Defaults.Paper, def.Defaults.CopyLabel);
            // 3) print window Save (what "Remember my choices" does): the layout comes through unchanged
            o1.Paper = "A4"; o1.CopyLabel = "Client copy"; o1.Orientation = "Portrait"; o1.Watermark = "Draft";
            PrintStore.Save(o1);
            var o2 = PrintStore.Load(SoaDef());
            Expect("after print Save", o2, "A4", "Client copy");
            if (o2.Orientation != "Portrait" || o2.Watermark != "Draft") bad.Add("print options not remembered");
            // the print window itself opens with the same layout in its preview
            using (var dlg = ShowStudio(SoaDef(), PrintStudioMode.Print))
            {
                var combos = StudioControls(dlg).OfType<ComboBox>().ToList();
                if (!combos.Any(c => c.Text == "A4")) bad.Add("print window does not show the remembered paper");
            }
            // 4) print window Reset to defaults: print options back, layout kept
            PrintStore.ResetPrintOptions(key);
            var o3 = PrintStore.Load(SoaDef());
            Expect("after print Reset", o3, def.Defaults.Paper, def.Defaults.CopyLabel);
            if (o3.Orientation != def.Defaults.Orientation || o3.Watermark != def.Defaults.Watermark) bad.Add("print options not reset to the form's defaults");
            // 5) layout reset: layout back to standard, remembered print options kept
            o3.Paper = "Legal"; PrintStore.Save(o3);
            PrintStore.ResetLayout(key);
            var o4 = PrintStore.Load(SoaDef());
            if (o4.Title != def.Defaults.Title || !o4.Col("fuel")) bad.Add("Reset layout did not restore the standard layout");
            if (o4.Paper != "Legal") bad.Add("Reset layout wiped the print options");
            PrintStore.Reset(key);
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string PrintLayout_RecordValuesAreNeverSaved_SubtitleAndNoteOnlyWhenChanged()
    {
        Fresh();
        var bad = new List<string>();
        var def = SoaDef();
        PrintStore.Reset(def.Key);
        var o = PrintStore.Load(def);
        PrintStore.SaveLayout(def, o);   // nothing changed in Settings
        string raw = MJdatabase.GetSystemSetting("PrintLayout." + def.Key, "");
        if (raw.Contains("Sample client") || raw.Contains(def.Defaults.Subtitle)) bad.Add("a record value was saved with the layout");
        var other = PrintForms.StatementOfAccount(new StatementOfAccount { SoaNumber = "SOA-OTHER", ClientName = "Other client", IssueDate = DateTime.Today }, PrintSample(SoaTypes.Normal).Items);
        var again = PrintStore.Load(other);
        if (again.Subtitle != other.Defaults.Subtitle || again.F("billed_to") != "Other client") bad.Add("another record lost its own reference or values");
        // a changed note is stored and printed on every copy
        o.Notes = "Please pay within 15 days."; PrintStore.SaveLayout(def, o);
        if (PrintStore.Load(other).Notes != "Please pay within 15 days.") bad.Add("the note typed in Settings is not used by another record");
        // an older whole-form save (before the two halves were kept apart) keeps its layout and print options through a print Save
        var legacy = def.Defaults.Clone(); legacy.Title = "Legacy title"; legacy.Paper = "Legal";
        MJdatabase.SetSystemSetting("PrintLayout." + def.Key, JsonSerializer.Serialize(legacy), "Print layout");
        var lo = PrintStore.Load(def);
        if (lo.Title != "Legacy title" || lo.Paper != "Legal") bad.Add("an older saved layout is not read");
        lo.Paper = "A4"; PrintStore.Save(lo);
        var after = PrintStore.Load(def);
        if (after.Title != "Legacy title" || after.Paper != "A4") bad.Add("print Save changed an older layout");
        PrintStore.Reset(def.Key);
        return string.Join("; ", bad);
    }

    [Check]
    public static string PrintLayouts_Registry_BuildsEveryForm_AndSettingsTabListsThem()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var entries = PrintLayoutRegistry.All();
            if (entries.Select(e => e.Key).Distinct().Count() != entries.Count) bad.Add("duplicate keys");
            foreach (var e in entries)
            {
                try
                {
                    var def = e.Build();
                    if (def.Key != e.Key) bad.Add($"{e.Name}: key {def.Key} != {e.Key}");
                    string html = PrintKit.Page(def, PrintStore.Load(def), false);
                    if (html.Contains("Couldn't build this form")) bad.Add($"{e.Name}: body failed");
                }
                catch (Exception ex) { bad.Add($"{e.Name}: {ex.Message}"); }
            }
            foreach (var k in new[] { "OfficialReceipt", "OfficialReceiptSpecial", "SOA_Official", "SOA_Special", "SOAInvoices", "TripReport", "DeliveryDossier", "LoadingSheet", "PaySlip", "CrewDossier" })
                if (!entries.Any(e => e.Key == k)) bad.Add("not listed: " + k);
            SignInAsAdmin();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1300, 800), Opacity = 0 };
            // The Print layouts tab is hidden in Settings (Nov 25) but still registered under the "PrintLayouts" key;
            // the layouts view itself still lists every form.
            var settings = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.SettingsView(SessionContext.ActiveUser, "PrintLayouts");
            settings.Dock = DockStyle.Fill; host.Controls.Add(settings); host.Show(); Pump(800);
            var tab = Flat(settings).OfType<Button>().FirstOrDefault(b => (b.Tag as string) == "PrintLayouts");
            if (tab == null) bad.Add("Settings has no Print layouts tab");
            else if (tab.Visible) bad.Add("Print layouts tab should be hidden");
            host.Controls.Remove(settings); settings.Dispose();
            var view = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.PrintLayoutsSettingsView();
            view.Dock = DockStyle.Fill; host.Controls.Add(view); Pump(800);
            int buttons = Flat(view).OfType<Button>().Count(b => b.Text == "Edit layout…");
            if (buttons != entries.Count) bad.Add($"{buttons} Edit buttons for {entries.Count} forms");
            return string.Join("; ", bad);
        });
    }
}
