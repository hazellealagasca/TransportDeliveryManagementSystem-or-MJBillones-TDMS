using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Where one trip stands from trip report to settlement: trip report → live invoice → statement → receipt.
    /// Used by Order History, the printed dossier and Finance History so every screen tells the same story.
    /// </summary>
    public sealed class OrderChain
    {
        public string TripReportNo = "";
        public string TripReportStatus = "";
        /// <summary>The live invoice that bills the trip (its own service invoice or an old statement's invoice); null when not billed.</summary>
        public LiveTripInvoice? Invoice;
        public string ReceiptNo = "";
        public string PayMethod = "";
        public string PayReference = "";
        public DateTime? PaidOn;
        public decimal PaidAmount;

        public string SoaNumber => Invoice?.SoaNumber ?? "";
        public bool IsPaid => Invoice != null && string.Equals(Invoice.Status, PayStatus.Paid, StringComparison.OrdinalIgnoreCase);

        /// <summary>One short phrase: Waiting for a trip report, Ready to invoice, Invoiced, On a statement, Paid.</summary>
        public string Stage
        {
            get
            {
                if (TripReportNo.Length == 0 && Invoice == null) return "Waiting for a trip report";
                if (Invoice == null) return string.Equals(TripReportStatus, Models.TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase) ? "Trip report is a draft" : "Ready to invoice";
                if (IsPaid) return "Paid";
                return SoaNumber.Length > 0 ? "On a statement, waiting for payment" : "Invoiced, waiting for payment";
            }
        }

        /// <summary>"TRIP-… → SI-2001 → SOA-2026-0005 → receipt 0006 (Nov 7, 2026)": the whole story on one line.</summary>
        public string Line()
        {
            var parts = new List<string>();
            if (TripReportNo.Length > 0) parts.Add(TripReportNo);
            if (Invoice != null) parts.Add("SI " + Invoice.Number);
            if (SoaNumber.Length > 0) parts.Add(SoaNumber);
            if (ReceiptNo.Length > 0) parts.Add("receipt " + ReceiptNo + (PaidOn.HasValue ? $" ({Fmt.Date(PaidOn.Value)})" : ""));
            return parts.Count == 0 ? Stage : string.Join(" → ", parts);
        }
    }

    /// <summary>A trip that more than one live invoice bills.</summary>
    public sealed class DuplicateBilling
    {
        public string TripLabel = "";
        public List<string> Invoices = new();
    }

    /// <summary>One trip of a settled record (a statement can bill several), with its trip report.</summary>
    public sealed class SettledTrip
    {
        public int DeliveryId;
        public int SpecialTripId;
        public string Number = "";
        public string TripReportNo = "";
    }

    public static partial class MJdatabase
    {
        private static (Dictionary<int, OrderChain> ByDelivery, Dictionary<int, OrderChain> BySpecialTrip) LoadOrderChains()
        {
            EnsureWorkflowColumns();
            var (liveD, liveS) = LoadLiveInvoices(0);
            var byD = new Dictionary<int, OrderChain>();
            var byS = new Dictionary<int, OrderChain>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // Trip reports (the newest not-void one of each trip).
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0), COALESCE(TripReportNumber,''), COALESCE(Status,'') FROM TripReports WHERE COALESCE(IsVoid,0) = 0 ORDER BY Id";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    int did = r.GetInt32(0), sid = r.GetInt32(1);
                    OrderChain ch = did > 0 ? Get(byD, did) : sid > 0 ? Get(byS, sid) : new OrderChain();
                    ch.TripReportNo = r.GetString(2); ch.TripReportStatus = r.GetString(3);
                }
            }
            foreach (var kv in liveD) Get(byD, kv.Key).Invoice = kv.Value;
            foreach (var kv in liveS) Get(byS, kv.Key).Invoice = kv.Value;

            // The newest live receipt of each invoice.
            var receipts = new Dictionary<int, (string No, string Method, string Ref, DateTime? On, decimal Amount)>();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT InvoiceId, COALESCE(ReceiptNumber,''), COALESCE(PaymentDate,''), COALESCE(PaymentMethod,''), COALESCE(ReferenceNumber,''), COALESCE(AmountPaid,0) FROM CollectionReceipts WHERE COALESCE(IsVoid,0) = 0 ORDER BY Id";
                using var r = c.ExecuteReader();
                while (r.Read())
                    receipts[r.GetInt32(0)] = (r.GetString(1), r.GetString(3), r.GetString(4), DateTime.TryParse(r.GetString(2), out var d) ? d : null, Centavos(Convert.ToDecimal(r.GetValue(5))));
            }
            foreach (var ch in byD.Values.Concat(byS.Values)) Fill(ch);
            return (byD, byS);

            static OrderChain Get(Dictionary<int, OrderChain> map, int key) { if (!map.TryGetValue(key, out var c)) map[key] = c = new OrderChain(); return c; }
            void Fill(OrderChain ch)
            {
                if (ch.Invoice == null || !receipts.TryGetValue(ch.Invoice.InvoiceId, out var rc)) return;
                ch.ReceiptNo = rc.No; ch.PayMethod = rc.Method; ch.PayReference = rc.Ref; ch.PaidOn = rc.On; ch.PaidAmount = rc.Amount;
            }
        }

        /// <summary>Both maps in one load: dispatch id → chain, special trip id → chain.</summary>
        public static (Dictionary<int, OrderChain> ByDelivery, Dictionary<int, OrderChain> BySpecialTrip) GetOrderChains() => LoadOrderChains();

        /// <summary>Dispatch id → where that dispatch stands from trip report to receipt.</summary>
        public static Dictionary<int, OrderChain> GetOrderChainsByDelivery() => LoadOrderChains().ByDelivery;

        /// <summary>Special trip id → where that special trip stands from trip report to receipt.</summary>
        public static Dictionary<int, OrderChain> GetOrderChainsBySpecialTrip() => LoadOrderChains().BySpecialTrip;

        /// <summary>Where one dispatch (or special trip) stands; never null.</summary>
        public static OrderChain GetOrderChainForTrip(int deliveryId, int specialTripId = 0)
        {
            var (byD, byS) = LoadOrderChains();
            if (deliveryId > 0 && byD.TryGetValue(deliveryId, out var a)) return a;
            if (specialTripId > 0 && byS.TryGetValue(specialTripId, out var b)) return b;
            return new OrderChain();
        }

        /// <summary>Trips that more than one live invoice bills (for example a trip invoiced on its own and again through an old statement).</summary>
        public static List<DuplicateBilling> GetTripsBilledMoreThanOnce()
        {
            EnsureSpecialTripColumns();
            var perTrip = new Dictionary<(bool Special, int Id), Dictionary<int, string>>(); // trip → invoice id → number
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            void Add(bool special, int id, int invId, string no)
            {
                if (id <= 0) return;
                if (!perTrip.TryGetValue((special, id), out var m)) perTrip[(special, id)] = m = new Dictionary<int, string>();
                m[invId] = no;
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $@"SELECT i.Id, COALESCE(i.InvoiceNumber,''), COALESCE(i.DeliveryId,0), COALESCE(i.SpecialTripId,0) FROM Invoices i
                                   WHERE {NotLiveInvoiceSql} AND (COALESCE(i.DeliveryId,0) > 0 OR COALESCE(i.SpecialTripId,0) > 0)";
                using var r = c.ExecuteReader();
                while (r.Read()) { Add(false, r.GetInt32(2), r.GetInt32(0), r.GetString(1)); Add(true, r.GetInt32(3), r.GetInt32(0), r.GetString(1)); }
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $@"SELECT DISTINCT i.Id, COALESCE(i.InvoiceNumber,''), COALESCE(x.DeliveryId,0), COALESCE(x.SpecialTripId,0)
                                   FROM Invoices i JOIN StatementsOfAccount s ON s.Id = i.SoaId AND LOWER(COALESCE(s.Status,'')) <> 'cancelled'
                                   JOIN StatementOfAccountItems x ON x.SoaId = s.Id
                                   WHERE {NotLiveInvoiceSql} AND COALESCE(i.DeliveryId,0) = 0 AND COALESCE(i.SpecialTripId,0) = 0";
                using var r = c.ExecuteReader();
                while (r.Read()) { Add(false, r.GetInt32(2), r.GetInt32(0), r.GetString(1)); Add(true, r.GetInt32(3), r.GetInt32(0), r.GetString(1)); }
            }
            var dispatchNo = new Dictionary<int, string>(); var specialNo = new Dictionary<int, string>();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT Id, COALESCE(DispatchNumber,'') FROM Deliveries";
                using var r = c.ExecuteReader(); while (r.Read()) dispatchNo[r.GetInt32(0)] = r.GetString(1);
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT Id, COALESCE(OrderReference,'') FROM SpecialTrips";
                using var r = c.ExecuteReader(); while (r.Read()) specialNo[r.GetInt32(0)] = r.GetString(1);
            }
            return perTrip.Where(kv => kv.Value.Count > 1)
                .Select(kv => new DuplicateBilling
                {
                    TripLabel = (kv.Key.Special ? specialNo : dispatchNo).TryGetValue(kv.Key.Id, out var n) && n.Length > 0 ? n : (kv.Key.Special ? "Special trip " : "Dispatch ") + kv.Key.Id,
                    Invoices = kv.Value.OrderBy(x => x.Key).Select(x => x.Value).ToList()
                })
                .OrderBy(d => d.TripLabel, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// The trips behind each settled record (same order as <paramref name="records"/>): a statement's trips come from its lines,
        /// a lone invoice's trip from the invoice itself.
        /// </summary>
        public static List<List<SettledTrip>> ResolveSettledTrips(IReadOnlyList<(StatementOfAccount? Soa, List<TripInvoice> Invoices, string ReceiptNo, DateTime PaidOn)> records)
        {
            var result = new List<List<SettledTrip>>();
            if (records.Count == 0) return result;
            var (chD, chS) = LoadOrderChains();
            var dispatchNo = new Dictionary<int, string>(); var specialNo = new Dictionary<int, string>();
            var soaLines = new Dictionary<int, List<(int Delivery, int Special)>>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = "SELECT Id, COALESCE(DispatchNumber,'') FROM Deliveries";
                    using var r = c.ExecuteReader(); while (r.Read()) dispatchNo[r.GetInt32(0)] = r.GetString(1);
                }
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = "SELECT Id, COALESCE(OrderReference,'') FROM SpecialTrips";
                    using var r = c.ExecuteReader(); while (r.Read()) specialNo[r.GetInt32(0)] = r.GetString(1);
                }
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = "SELECT SoaId, COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0) FROM StatementOfAccountItems WHERE COALESCE(DeliveryId,0) > 0 OR COALESCE(SpecialTripId,0) > 0";
                    using var r = c.ExecuteReader();
                    while (r.Read())
                    {
                        int soa = r.GetInt32(0);
                        if (!soaLines.TryGetValue(soa, out var l)) soaLines[soa] = l = new List<(int, int)>();
                        l.Add((r.GetInt32(1), r.GetInt32(2)));
                    }
                }
            }
            SettledTrip Make(int did, int sid)
            {
                var ch = did > 0 ? (chD.TryGetValue(did, out var a) ? a : null) : (chS.TryGetValue(sid, out var b) ? b : null);
                string no = did > 0 ? (dispatchNo.TryGetValue(did, out var dn) ? dn : "") : (specialNo.TryGetValue(sid, out var sn) ? sn : "");
                return new SettledTrip { DeliveryId = did, SpecialTripId = did > 0 ? 0 : sid, Number = no, TripReportNo = ch?.TripReportNo ?? "" };
            }
            foreach (var rec in records)
            {
                var keys = new List<(int, int)>();
                foreach (var inv in rec.Invoices)
                    if (inv.DeliveryId > 0 || inv.SpecialTripId > 0) keys.Add((inv.DeliveryId, inv.DeliveryId > 0 ? 0 : inv.SpecialTripId));
                if (rec.Soa != null && soaLines.TryGetValue(rec.Soa.Id, out var lines)) keys.AddRange(lines.Select(l => (l.Delivery, l.Delivery > 0 ? 0 : l.Special)));
                result.Add(keys.Distinct().Select(k => Make(k.Item1, k.Item2)).Where(t => t.Number.Length > 0).OrderBy(t => t.Number, StringComparer.OrdinalIgnoreCase).ToList());
            }
            return result;
        }
    }
}
