using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>What the checks before an invoice save found (so the save can record confirmations and the audit entry after it commits).</summary>
    public sealed class InvoicePrecheck
    {
        public NumberCheck? Number;
        public string? RenamedFrom;
    }

    /// <summary>The live (not cancelled) invoice that bills one trip.</summary>
    public sealed class LiveTripInvoice
    {
        public int InvoiceId;
        public string Number = "";
        public string Status = "";
        public decimal Net;
        public DateTime IssueDate;
        /// <summary>The live statement the invoice is on (0 = none).</summary>
        public int SoaId;
        public string SoaNumber = "";
        /// <summary>True for an old statement's own whole-statement invoice: it bills the trip only through the statement's lines.</summary>
        public bool OnOldStatement;
        /// <summary>"invoice 0765" or "invoice SI-2001 on statement SOA-2026-0005", for messages.</summary>
        public string Describe() => SoaNumber.Length > 0 ? $"invoice {Number} on statement {SoaNumber}" : $"invoice {Number}";
    }

    /// <summary>
    /// One place that answers "which live invoice bills this trip?". Two kinds exist side by side: the service invoice made for the
    /// trip itself (Nov 6 workflow) and an old statement's whole-statement invoice, which has no trip link and bills its trips
    /// through the statement's lines. Every screen that asks the question must see both, or a trip can be billed twice.
    /// </summary>
    public static partial class MJdatabase
    {
        private const string NotLiveInvoiceSql = "LOWER(COALESCE(i.Status,'')) NOT IN ('cancelled','void','voided')";

        private static (Dictionary<int, LiveTripInvoice> ByDelivery, Dictionary<int, LiveTripInvoice> BySpecialTrip) LoadLiveInvoices(int exceptInvoiceId)
        {
            var byDelivery = new Dictionary<int, LiveTripInvoice>();
            var bySpecial = new Dictionary<int, LiveTripInvoice>();
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            static LiveTripInvoice Read(SqliteDataReader r) => new LiveTripInvoice
            {
                InvoiceId = r.GetInt32(0),
                Number = r.GetString(1).Trim(),
                Status = r.GetString(2),
                Net = Centavos(Convert.ToDecimal(r.GetValue(3))),
                IssueDate = DateTime.TryParse(r.GetString(4), out var d) ? d : DateTime.MinValue,
                SoaId = r.GetInt32(5),
                SoaNumber = r.GetString(6),
            };

            // 1. The trip's own service invoice (oldest first, so the newest one wins).
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $@"SELECT i.Id, COALESCE(i.InvoiceNumber,''), COALESCE(i.Status,''), COALESCE(i.NetAmount,0), COALESCE(i.IssueDate,''),
                                          CASE WHEN s.Id IS NULL OR LOWER(COALESCE(s.Status,'')) = 'cancelled' THEN 0 ELSE s.Id END,
                                          CASE WHEN s.Id IS NULL OR LOWER(COALESCE(s.Status,'')) = 'cancelled' THEN '' ELSE COALESCE(s.SoaNumber,'') END,
                                          COALESCE(i.DeliveryId,0), COALESCE(i.SpecialTripId,0)
                                   FROM Invoices i LEFT JOIN StatementsOfAccount s ON s.Id = COALESCE(i.SoaId,0)
                                   WHERE {NotLiveInvoiceSql} AND i.Id <> @ex AND (COALESCE(i.DeliveryId,0) > 0 OR COALESCE(i.SpecialTripId,0) > 0)
                                   ORDER BY i.Id";
                c.Parameters.AddWithValue("@ex", exceptInvoiceId);
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    var live = Read(r);
                    int did = r.GetInt32(7), sid = r.GetInt32(8);
                    if (did > 0) byDelivery[did] = live;
                    if (sid > 0) bySpecial[sid] = live;
                }
            }

            // 2. An old statement's whole-statement invoice bills every trip on its lines (the trip's own invoice wins when both exist).
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $@"SELECT i.Id, COALESCE(i.InvoiceNumber,''), COALESCE(i.Status,''), COALESCE(i.NetAmount,0), COALESCE(i.IssueDate,''),
                                          s.Id, COALESCE(s.SoaNumber,''), COALESCE(x.DeliveryId,0), COALESCE(x.SpecialTripId,0)
                                   FROM Invoices i
                                   JOIN StatementsOfAccount s ON s.Id = i.SoaId AND LOWER(COALESCE(s.Status,'')) <> 'cancelled'
                                   JOIN StatementOfAccountItems x ON x.SoaId = s.Id
                                   WHERE {NotLiveInvoiceSql} AND i.Id <> @ex AND COALESCE(i.DeliveryId,0) = 0 AND COALESCE(i.SpecialTripId,0) = 0
                                     AND (COALESCE(x.DeliveryId,0) > 0 OR COALESCE(x.SpecialTripId,0) > 0)
                                   ORDER BY i.Id";
                c.Parameters.AddWithValue("@ex", exceptInvoiceId);
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    var live = Read(r);
                    live.OnOldStatement = true;
                    int did = r.GetInt32(7), sid = r.GetInt32(8);
                    if (did > 0 && (!byDelivery.TryGetValue(did, out var cur) || cur.OnOldStatement)) byDelivery[did] = live;
                    if (sid > 0 && (!bySpecial.TryGetValue(sid, out var cur2) || cur2.OnOldStatement)) bySpecial[sid] = live;
                }
            }
            return (byDelivery, bySpecial);
        }

        /// <summary>Dispatch id → the live invoice that bills it (the trip's own service invoice, or an old statement's invoice).</summary>
        public static Dictionary<int, LiveTripInvoice> GetLiveInvoicesByDelivery() => LoadLiveInvoices(0).ByDelivery;

        /// <summary>Special trip id → the live invoice that bills it.</summary>
        public static Dictionary<int, LiveTripInvoice> GetLiveInvoicesBySpecialTrip() => LoadLiveInvoices(0).BySpecialTrip;

        /// <summary>
        /// The live invoice that bills this dispatch (<paramref name="deliveryId"/> &gt; 0) or special trip, or null when it is not billed.
        /// A cancelled invoice never counts. <paramref name="exceptInvoiceId"/> leaves out the invoice being saved.
        /// </summary>
        public static LiveTripInvoice? GetLiveInvoiceForTrip(int deliveryId, int specialTripId = 0, int exceptInvoiceId = 0)
        {
            var (byDelivery, bySpecial) = LoadLiveInvoices(exceptInvoiceId);
            if (deliveryId > 0 && byDelivery.TryGetValue(deliveryId, out var a)) return a;
            if (specialTripId > 0 && bySpecial.TryGetValue(specialTripId, out var b)) return b;
            return null;
        }

        /// <summary>True when the statement exists and is not cancelled.</summary>
        private static bool SoaIsLive(int soaId)
        {
            if (soaId <= 0) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = "SELECT COALESCE(Status,'') FROM StatementsOfAccount WHERE Id = @s";
            c.Parameters.AddWithValue("@s", soaId);
            var st = c.ExecuteScalar();
            return st != null && !string.Equals(Convert.ToString(st), "Cancelled", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Rules every save of a service invoice must pass, whichever screen or flow saves it:
        /// the number can't repeat, a trip has one live invoice, and an invoice on a live statement keeps its amounts and client.
        /// </summary>
        private static InvoicePrecheck CheckInvoiceBeforeSave(Invoice inv, bool crossTypeConfirmed = false)
        {
            var result = new InvoicePrecheck();
            string storedNo = ""; int storedSoa = 0, storedDelivery = 0, storedSpecial = 0; decimal storedTotal = 0, storedDed = 0, storedFuel = 0; string storedClient = "";
            if (inv.Id > 0)
            {
                EnsureSpecialTripColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT COALESCE(InvoiceNumber,''), COALESCE(SoaId,0), COALESCE(TotalAmount,0), COALESCE(Deductions,0), COALESCE(ClientName,''), COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0), COALESCE(FuelSurcharge,0) FROM Invoices WHERE Id = @id";
                c.Parameters.AddWithValue("@id", inv.Id);
                using var r = c.ExecuteReader();
                if (r.Read())
                {
                    storedNo = r.GetString(0); storedSoa = r.GetInt32(1); storedTotal = Centavos(Convert.ToDecimal(r.GetValue(2)));
                    storedDed = Centavos(Convert.ToDecimal(r.GetValue(3))); storedClient = r.GetString(4); storedDelivery = r.GetInt32(5); storedSpecial = r.GetInt32(6); storedFuel = Centavos(Convert.ToDecimal(r.GetValue(7)));
                }
            }

            // 1. The number (plan 17): required, well formed, never repeated; checked here INSIDE the write transaction so two PCs cannot both take it.
            //    A same-digits service invoice / receipt pair needs the person's confirmation. Not rechecked when the number is unchanged.
            bool isNew = inv.Id <= 0;
            bool renamed = !isNew && !string.Equals(storedNo.Trim(), (inv.InvoiceNumber ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            if (isNew || renamed)
            {
                var chk = LoadNumberIndex().Check(NumberKind.SI, inv.InvoiceNumber, inv.Id);
                if (chk.Blocked) throw new InvalidOperationException(chk.Why() + " Nothing was saved.");
                if (chk.TickNeeded && !crossTypeConfirmed)
                    throw new NumberNeedsConfirmationException(chk.TickMessage() + " Confirm it on the form, then save again. Nothing was saved.");
                result.Number = chk;
            }
            if (renamed)
            {
                // Changing the number of a saved invoice: Administrator only, and only before it is used downstream (statement, payment). The old number is released.
                if (!SessionContext.IsAdmin)
                    throw new InvalidOperationException($"Only the Administrator can change the Service Invoice No. of a saved invoice (now {storedNo}). If it was typed wrong, ask the Administrator, or cancel the invoice as a typing mistake and enter it again. Nothing was saved.");
                if ((storedSoa > 0 && SoaIsLive(storedSoa)) || GetTotalCollectedForInvoice(inv.Id) > 0)
                    throw new InvalidOperationException($"Invoice {storedNo} is on a statement or already paid, so its number can't be changed. Nothing was saved.");
                result.RenamedFrom = storedNo.Trim();
            }

            // 2. One live invoice per dispatch. An old statement's invoice that bills the trip counts as well.
            if (inv.DeliveryId > 0)
            {
                var live = GetLiveInvoiceForTrip(inv.DeliveryId, 0, inv.Id);
                if (live != null)
                {
                    string no = GetDeliveryById(inv.DeliveryId)?.DispatchNumber ?? "This trip";
                    throw new InvalidOperationException($"Dispatch {no} is already billed by {live.Describe()}. A trip can be invoiced only once. Nothing was saved.");
                }
            }

            // 3. A trip's invoice on a live statement is part of the statement's total: its amounts and client change on the statement.
            if (inv.Id > 0 && storedSoa > 0 && (storedDelivery > 0 || storedSpecial > 0) && SoaIsLive(storedSoa)
                && (Centavos(inv.TotalAmount) != storedTotal || Centavos(inv.Deductions) != storedDed || Centavos(inv.FuelSurcharge) != storedFuel
                    || !string.Equals((inv.ClientName ?? "").Trim(), storedClient.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Invoice {storedNo} is on a statement, so its amounts and client can't change here. Take its trip off the Statement of Account first. Nothing was saved.");

            // 4. Payments are always in full and there are no part payments, so once a receipt exists the invoice stays as it was paid.
            //    (Raising a paid invoice would leave it Unpaid with a receipt on it, which Collections refuses: it could never be settled.)
            if (inv.Id > 0 && GetTotalCollectedForInvoice(inv.Id) > 0
                && (Centavos(inv.TotalAmount) != storedTotal || Centavos(inv.Deductions) != storedDed || Centavos(inv.FuelSurcharge) != storedFuel
                    || !string.Equals((inv.ClientName ?? "").Trim(), storedClient.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Invoice {storedNo} already has a collection receipt, so its amounts and client can't change. Void the receipt in Collections & Receipts first. Nothing was saved.");
            return result;
        }
    }
}
