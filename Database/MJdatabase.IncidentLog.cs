using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>Nov 17: one line of Resources → Incident log: an incident of a trip with the trip's client, truck and crew.</summary>
    public sealed class IncidentLogRow
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public DateTime IncidentDate { get; set; }
        public string DispatchNumber { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string TruckInfo { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string AideNames { get; set; } = "";
        public string Cause { get; set; } = "";
        public int DamagedQuantity { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool ChargeableToCrew { get; set; }
        public string ChargedCrewIds { get; set; } = "";
        public string ChargedCrewNames { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Status { get; set; } = "";
        /// <summary>"No", "Yes" (older lines without names) or the names with each one's share.</summary>
        public string ChargedTo
        {
            get
            {
                if (!ChargeableToCrew) return "No";
                if (string.IsNullOrWhiteSpace(ChargedCrewNames)) return "Yes";
                int n = ChargedCrewIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
                return n > 1 ? $"{ChargedCrewNames} (PHP {Math.Round(EstimatedCost / n, 2):N2} each)" : ChargedCrewNames;
            }
        }
        /// <summary>Text the search box looks in.</summary>
        public string SearchText => string.Join(" ", DispatchNumber, ClientName, TruckInfo, DriverName, AideNames, Cause, ChargedCrewNames, Notes);
    }

    public static partial class MJdatabase
    {
        /// <summary>
        /// Nov 17: every incident recorded on a trip report (step 3), newest first, with the trip's client, truck, driver and aides.
        /// Voided lines (taken off a trip report) are left out. Read-only: nothing in pay, statements, invoices or receipts reads it.
        /// </summary>
        public static List<IncidentLogRow> GetIncidentLog()
        {
            var list = new List<IncidentLogRow>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            EnsureIncidentCrewColumns(conn);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT i.Id, i.DeliveryId, i.IncidentDate, COALESCE(NULLIF(i.DispatchNumber,''), d.DispatchNumber, ''), COALESCE(d.ClientName,''), COALESCE(d.TruckInfo,''),
                       COALESCE(d.DriverName,''), COALESCE(NULLIF(d.AideNames,''), d.AideName, ''), COALESCE(i.Cause,''), COALESCE(i.DamagedQuantity,0), COALESCE(i.EstimatedCost,0),
                       COALESCE(i.ChargeableToCrew,0), COALESCE(i.ChargedCrewIds,''), COALESCE(i.ChargedCrewNames,''), COALESCE(i.Description,''), COALESCE(i.Status,'')
                FROM IncidentReports i
                LEFT JOIN Deliveries d ON d.Id = i.DeliveryId
                WHERE COALESCE(i.Status,'') <> 'Voided'
                ORDER BY i.IncidentDate DESC, i.Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string aides = r.GetString(7);
                list.Add(new IncidentLogRow
                {
                    Id = r.GetInt32(0),
                    DeliveryId = r.GetInt32(1),
                    IncidentDate = DateTime.TryParse(r.GetValue(2)?.ToString(), out var dt) ? dt : DateTime.MinValue,
                    DispatchNumber = r.GetString(3),
                    ClientName = r.GetString(4),
                    TruckInfo = r.GetString(5),
                    DriverName = r.GetString(6),
                    AideNames = aides.Equals("None", StringComparison.OrdinalIgnoreCase) ? "" : aides,
                    Cause = r.GetString(8),
                    DamagedQuantity = Convert.ToInt32(r.GetValue(9)),
                    EstimatedCost = Convert.ToDecimal(r.GetValue(10)),
                    ChargeableToCrew = Convert.ToInt32(r.GetValue(11)) == 1,
                    ChargedCrewIds = r.GetString(12),
                    ChargedCrewNames = r.GetString(13),
                    Notes = r.GetString(14),
                    Status = r.GetString(15)
                });
            }
            return list;
        }
    }
}
