using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 17: an incident that is chargeable to the crew names WHO is charged: the trip's driver and/or aides
    /// (IncidentReports.ChargedCrewIds / ChargedCrewNames). The estimated cost is shared equally. Record only, as before:
    /// nothing in pay, statements, invoices or receipts reads it.
    /// </summary>
    public static partial class MJdatabase
    {
        private static bool incidentCrewColumnsReady;

        private static void EnsureIncidentCrewColumns(SqliteConnection conn)
        {
            if (incidentCrewColumnsReady) return;
            AddColumnIfMissing(conn, "IncidentReports", "ChargedCrewIds", "TEXT DEFAULT ''");
            AddColumnIfMissing(conn, "IncidentReports", "ChargedCrewNames", "TEXT DEFAULT ''");
            incidentCrewColumnsReady = true;
        }

        /// <summary>The trip's crew who can be charged: the driver first, then the aides (Id and name), without repeats.</summary>
        public static List<(int Id, string Name, string Role)> GetTripCrewForCharge(Delivery? d)
        {
            var list = new List<(int Id, string Name, string Role)>();
            if (d == null) return list;
            Dictionary<int, Crew> byId;
            try { byId = GetCrews().GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()); }
            catch (Exception ex) { ErrLog.Swallowed(ex); byId = new Dictionary<int, Crew>(); }
            string NameOf(int id, string fallback) => byId.TryGetValue(id, out var c) && !string.IsNullOrWhiteSpace(c.FullName) ? c.FullName.Trim() : fallback.Trim();
            if (d.DriverId > 0) list.Add((d.DriverId, NameOf(d.DriverId, d.DriverName), "Driver"));
            var aideIds = (d.AideIds ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out int v) ? v : 0).Where(v => v > 0).ToList();
            if (aideIds.Count == 0 && d.AideId > 0) aideIds.Add(d.AideId);
            // Names are "Last, First Middle" and the dispatch joins several with ", ", so the saved text can't be split back reliably:
            // the crew list gives the name; the saved text is only a fallback for a single aide.
            string single = aideIds.Count == 1 ? (!string.IsNullOrWhiteSpace(d.AideName) ? d.AideName : d.AideNames ?? "") : "";
            if (single.Trim().Equals("None", StringComparison.OrdinalIgnoreCase)) single = "";
            // Nov 18: several aides: "Last, First Middle, Last, First Middle" splits into 2 parts per name, so pair the parts back up
            // when the count matches (only a fallback for a crew member who is no longer in the crew list).
            var parts = (d.AideNames ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var paired = aideIds.Count > 1 && parts.Length == aideIds.Count * 2
                ? Enumerable.Range(0, aideIds.Count).Select(i => parts[2 * i] + ", " + parts[2 * i + 1]).ToList()
                : aideIds.Count > 1 && parts.Length == aideIds.Count ? parts.ToList() : new List<string>();
            for (int i = 0; i < aideIds.Count; i++)
            {
                int id = aideIds[i];
                if (list.Any(x => x.Id == id)) continue;
                string fallback = single.Length > 0 ? single : i < paired.Count ? paired[i] : $"Crew #{id}";
                list.Add((id, NameOf(id, fallback), "Aide"));
            }
            return list.Where(x => x.Name.Length > 0).ToList();
        }
    }
}
