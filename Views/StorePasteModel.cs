using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public enum PasteStatus { Unchecked, Ok, AlreadyRegistered, Duplicate, Invalid }

    /// <summary>One pasted line: the Code and Name as typed (cleaned after Classify), its status and the text to show for it.</summary>
    public record PasteRow(string Code, string Name, PasteStatus Status = PasteStatus.Unchecked, string Message = "");

    public record PasteResult(int Added, int Skipped, List<string> Problems);

    /// <summary>
    /// The no-UI parts of typing and pasting stores in Clients > Company stores: the "Will save as" preview, reading
    /// tab-separated text from Excel, giving each row a status, and adding the valid rows. The paste adds client stores only
    /// (through <see cref="MJdatabase.RegisterCompanyStore"/>); it never puts a store on a route.
    /// </summary>
    public static class StorePasteModel
    {
        /// <summary>"Will save as DD1" for a typed code that cleans to something different; "" when there is nothing to say.</summary>
        public static string CodePreview(string? typed)
        {
            string raw = MJdatabase.CleanStoreText(typed);
            if (raw.Length == 0) return "";
            string clean = MJdatabase.CleanStoreCode(raw);
            if (clean.Length == 0) return "Nothing usable in this code: use letters, numbers and hyphens.";
            return string.Equals(clean, raw, StringComparison.Ordinal) ? "" : "Will save as " + clean;
        }

        /// <summary>Reads pasted text: one row per non-empty line, first two tab-separated cells are the code and the name. A "Code / Name" header line is skipped.</summary>
        public static List<PasteRow> ParseRows(string? text)
        {
            var rows = new List<PasteRow>();
            foreach (var line in (text ?? "").Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                if (line.Trim().Length == 0) continue;
                var cells = line.Split('\t');
                string code = cells[0].Trim(), name = cells.Length > 1 ? cells[1].Trim() : "";
                if (rows.Count == 0 && IsHeader(code, name)) continue;
                rows.Add(new PasteRow(code, name));
            }
            return rows;
        }

        private static bool IsHeader(string code, string name) =>
            (code.Equals("code", StringComparison.OrdinalIgnoreCase) || code.Equals("store code", StringComparison.OrdinalIgnoreCase)) &&
            (name.Equals("name", StringComparison.OrdinalIgnoreCase) || name.Equals("store name", StringComparison.OrdinalIgnoreCase));

        /// <summary>Gives each row its status. Codes come back cleaned. A row repeating an earlier row's code or name is a Duplicate; a code or name already taken is AlreadyRegistered.</summary>
        public static List<PasteRow> Classify(string client, IEnumerable<PasteRow> rows)
        {
            var result = new List<PasteRow>();
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool noClient = MJdatabase.CleanStoreText(client).Length == 0;
            foreach (var r in rows)
            {
                string code = MJdatabase.CleanStoreCode(r.Code), name = MJdatabase.CleanStoreText(r.Name);
                if (noClient) { result.Add(new PasteRow(code, name, PasteStatus.Invalid, "Choose the client company first.")); continue; }
                if (code.Length == 0) { result.Add(new PasteRow(code, name, PasteStatus.Invalid, "Invalid code: use letters, numbers and hyphens.")); continue; }
                if (name.Length == 0) { result.Add(new PasteRow(code, name, PasteStatus.Invalid, "Missing store name.")); continue; }
                if (name.Length > MJdatabase.StoreNameMaxLength) { result.Add(new PasteRow(code, name, PasteStatus.Invalid, $"Name too long (at most {MJdatabase.StoreNameMaxLength} characters).")); continue; }
                if (codes.Contains(code) || names.Contains(name)) { result.Add(new PasteRow(code, name, PasteStatus.Duplicate, "Duplicate in the list.")); continue; }
                codes.Add(code); names.Add(name);
                string? taken = MJdatabase.CheckCompanyStore(client, code, name);
                result.Add(taken == null ? new PasteRow(code, name, PasteStatus.Ok, "OK") : new PasteRow(code, name, PasteStatus.AlreadyRegistered, taken.Replace("Record Already Exists: ", "Already registered: ")));
            }
            return result;
        }

        /// <summary>Registers each Ok row as a client store (same path as the single add). Everything else, and any row that fails on the way, is skipped and reported.</summary>
        public static PasteResult AddValid(string client, IEnumerable<PasteRow> classified)
        {
            var all = classified.ToList();
            int added = 0;
            var problems = new List<string>();
            foreach (var r in all.Where(x => x.Status == PasteStatus.Ok))
            {
                try { MJdatabase.RegisterCompanyStore(client, r.Code, r.Name); added++; }
                catch (InvalidOperationException ex) { problems.Add($"{r.Code}: {ex.Message}"); }
                catch (Exception ex) { ErrLog.Error("StorePasteModel.AddValid", ex); problems.Add($"{r.Code}: {ex.Message}"); }
            }
            return new PasteResult(added, all.Count - added, problems);
        }
    }
}
