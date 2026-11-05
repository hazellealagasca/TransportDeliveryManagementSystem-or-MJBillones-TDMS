using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Delivery receipt (DR) numbers. A store can have several DRs; they are stored as one text, "DR-1001, DR-1002",
    /// shown one per line on screen and on printouts, and typed with commas, semicolons or new lines between them.
    /// Spaces are trimmed and the same DR is kept only once (ignoring upper / lower case). A "/" is part of a DR
    /// number, never a separator.
    /// </summary>
    public static class DrList
    {
        private static readonly char[] Separators = { ',', ';', '\r', '\n', '\t' };


        /// <summary>The DR numbers in <paramref name="text"/>, in order, trimmed and without repeats.</summary>
        public static List<string> Parse(string? text)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                string dr = Regex.Replace(part.Trim(), @"\s+", " ");
                if (dr.Length > 0 && seen.Add(dr)) list.Add(dr);
            }
            return list;
        }


        /// <summary>How DRs are stored: "DR-1001, DR-1002".</summary>
        public static string Join(IEnumerable<string> drs) => string.Join(", ", drs);


        /// <summary>Cleans typed text into the stored form.</summary>
        public static string Normalize(string? text) => Join(Parse(text));


        /// <summary>One DR per line, for a table cell.</summary>
        public static string Lines(string? text) => string.Join(Environment.NewLine, Parse(text));


        /// <summary>One DR per line for a printout (HTML-safe).</summary>
        public static string Html(string? text, string prefix = "") =>
            // Each DR stays on one line (a narrow column used to break "DR-3001" into "DR-" and "3001").
            string.Join("<br/>", Parse(text).Select(d => "<span style=\"white-space:nowrap\">" + WebUtility.HtmlEncode(prefix + d) + "</span>"));

        /// <summary>Adds the DRs of <paramref name="more"/> after <paramref name="first"/>, keeping each DR once.</summary>
        public static List<string> Merge(IEnumerable<string> first, IEnumerable<string> more)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in first.Concat(more))
                if (!string.IsNullOrWhiteSpace(d) && seen.Add(d.Trim())) list.Add(d.Trim());
            return list;
        }


        public static int Count(string? text) => Parse(text).Count;
    }
}
