using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 6, 2026: a statement of account groups the service invoices of its trips (one invoice per trip, made in Billing with
    /// the typed SI No.). The statement no longer makes its own invoice; its total is the sum of its invoices.
    /// Statements saved before this (with their own SI-invoice) keep working the old way.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>Smallest freight Amount a statement line may carry (stops a typo like 0.01 from becoming the invoice amount).</summary>
        public const decimal MinStatementAmount = 1m;

        private static void EnsureInvoiceAmountHistory(SqliteConnection conn, SqliteTransaction? tx)
        {
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = @"CREATE TABLE IF NOT EXISTS InvoiceAmountHistory (Id INTEGER PRIMARY KEY AUTOINCREMENT, InvoiceId INTEGER NOT NULL, InvoiceNumber TEXT, SoaId INTEGER NOT NULL,
                              OldTotal REAL NOT NULL, OldFuel REAL NOT NULL, NewTotal REAL NOT NULL, NewFuel REAL NOT NULL, ChangedAt TEXT, ChangedBy TEXT, RevertedAt TEXT)";
            c.ExecuteNonQuery();
        }

        /// <summary>
        /// An unpaid invoice's amounts are about to be replaced by the statement line's: keep the old amounts (so cancelling the statement can put them back)
        /// and write an audit line with old and new. Does nothing when the amounts are the same.
        /// </summary>
        private static void RecordInvoiceAmountChange(SqliteConnection conn, SqliteTransaction tx, int invoiceId, string invoiceNo, int soaId, string soaNo, decimal newTotal, decimal newFuel)
        {
            EnsureInvoiceAmountHistory(conn, tx);
            decimal oldTotal, oldFuel;
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "SELECT COALESCE(TotalAmount,0), COALESCE(FuelSurcharge,0) FROM Invoices WHERE Id = @i";
                c.Parameters.AddWithValue("@i", invoiceId);
                using var r = c.ExecuteReader();
                if (!r.Read()) return;
                oldTotal = Centavos(Convert.ToDecimal(r.GetValue(0))); oldFuel = Centavos(Convert.ToDecimal(r.GetValue(1)));
            }
            if (oldTotal == newTotal && oldFuel == newFuel) return;
            string user = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername ?? "";
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using (var h = conn.CreateCommand())
            {
                h.Transaction = tx;
                h.CommandText = "INSERT INTO InvoiceAmountHistory (InvoiceId, InvoiceNumber, SoaId, OldTotal, OldFuel, NewTotal, NewFuel, ChangedAt, ChangedBy) VALUES (@i,@n,@s,@ot,@of,@nt,@nf,@at,@by)";
                h.Parameters.AddWithValue("@i", invoiceId); h.Parameters.AddWithValue("@n", invoiceNo ?? ""); h.Parameters.AddWithValue("@s", soaId);
                h.Parameters.AddWithValue("@ot", oldTotal); h.Parameters.AddWithValue("@of", oldFuel); h.Parameters.AddWithValue("@nt", newTotal); h.Parameters.AddWithValue("@nf", newFuel);
                h.Parameters.AddWithValue("@at", now); h.Parameters.AddWithValue("@by", user);
                h.ExecuteNonQuery();
            }
            using (var a = conn.CreateCommand())
            {
                a.Transaction = tx;
                a.CommandText = @"INSERT INTO AuditTrail (Timestamp, Username, UserRole, ActionType, TableName, RecordId, RecordIdentifier, FieldName, OldValue, NewValue, ReasonOrNotes, IpOrMachine)
                                  VALUES (@ts, @u, @r, 'UPDATE', 'Invoices', @rid, @rident, 'TotalAmount / FuelSurcharge', @old, @new, @rsn, @ip)";
                a.Parameters.AddWithValue("@ts", now); a.Parameters.AddWithValue("@u", user); a.Parameters.AddWithValue("@r", SessionContext.ActiveUser?.Role ?? "System");
                a.Parameters.AddWithValue("@rid", invoiceId); a.Parameters.AddWithValue("@rident", invoiceNo ?? "");
                a.Parameters.AddWithValue("@old", $"total {oldTotal:N2}, fuel {oldFuel:N2}"); a.Parameters.AddWithValue("@new", $"total {newTotal:N2}, fuel {newFuel:N2}");
                a.Parameters.AddWithValue("@rsn", $"Amounts replaced by the lines of statement {soaNo}"); a.Parameters.AddWithValue("@ip", Environment.MachineName);
                a.ExecuteNonQuery();
            }
        }

        /// <summary>Cancelling a statement: invoices whose amounts it replaced get their earlier amounts back (they are unpaid: a live receipt blocks the cancel).</summary>
        private static void RestoreInvoiceAmountsOfSoa(int soaId)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                EnsureInvoiceAmountHistory(conn, tx);
                var rows = new List<(int Hid, int Inv, string No, decimal OldT, decimal OldF, decimal NewT, decimal NewF)>();
                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = "SELECT Id, InvoiceId, COALESCE(InvoiceNumber,''), OldTotal, OldFuel, NewTotal, NewFuel FROM InvoiceAmountHistory WHERE SoaId = @s AND RevertedAt IS NULL";
                    c.Parameters.AddWithValue("@s", soaId);
                    using var r = c.ExecuteReader();
                    while (r.Read()) rows.Add((r.GetInt32(0), r.GetInt32(1), r.GetString(2), Convert.ToDecimal(r.GetValue(3)), Convert.ToDecimal(r.GetValue(4)), Convert.ToDecimal(r.GetValue(5)), Convert.ToDecimal(r.GetValue(6))));
                }
                string user = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername ?? "";
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                foreach (var h in rows)
                {
                    using (var u = conn.CreateCommand())
                    {
                        u.Transaction = tx;
                        u.CommandText = "UPDATE Invoices SET TotalAmount = @t, FuelSurcharge = @f, NetAmount = @t - COALESCE(Deductions,0) WHERE Id = @i AND COALESCE(Status,'') = 'Unpaid' COLLATE NOCASE";
                        u.Parameters.AddWithValue("@t", h.OldT); u.Parameters.AddWithValue("@f", h.OldF); u.Parameters.AddWithValue("@i", h.Inv);
                        u.ExecuteNonQuery();
                    }
                    using (var u = conn.CreateCommand())
                    {
                        u.Transaction = tx;
                        u.CommandText = "UPDATE InvoiceAmountHistory SET RevertedAt = @at WHERE Id = @h";
                        u.Parameters.AddWithValue("@at", now); u.Parameters.AddWithValue("@h", h.Hid);
                        u.ExecuteNonQuery();
                    }
                    using (var a = conn.CreateCommand())
                    {
                        a.Transaction = tx;
                        a.CommandText = @"INSERT INTO AuditTrail (Timestamp, Username, UserRole, ActionType, TableName, RecordId, RecordIdentifier, FieldName, OldValue, NewValue, ReasonOrNotes, IpOrMachine)
                                          VALUES (@ts, @u, @r, 'UPDATE', 'Invoices', @rid, @rident, 'TotalAmount / FuelSurcharge', @old, @new, 'Statement cancelled: the invoice got its earlier amounts back', @ip)";
                        a.Parameters.AddWithValue("@ts", now); a.Parameters.AddWithValue("@u", user); a.Parameters.AddWithValue("@r", SessionContext.ActiveUser?.Role ?? "System");
                        a.Parameters.AddWithValue("@rid", h.Inv); a.Parameters.AddWithValue("@rident", h.No);
                        a.Parameters.AddWithValue("@old", $"total {h.NewT:N2}, fuel {h.NewF:N2}"); a.Parameters.AddWithValue("@new", $"total {h.OldT:N2}, fuel {h.OldF:N2}"); a.Parameters.AddWithValue("@ip", Environment.MachineName);
                        a.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>The trip's service invoice (newest that is not cancelled), from a list made by <see cref="GetTripInvoices"/>.</summary>
        public static TripInvoice? TripInvoiceFor(IEnumerable<TripInvoice> all, int deliveryId, int specialTripId)
        {
            if (deliveryId > 0) return all.Where(i => i.DeliveryId == deliveryId).OrderByDescending(i => i.Id).FirstOrDefault();
            if (specialTripId > 0) return all.Where(i => i.SpecialTripId == specialTripId).OrderByDescending(i => i.Id).FirstOrDefault();
            return null;
        }

        /// <summary>True when the statement still has the invoice it made itself before Nov 6 (an "SI no." invoice with no trip).</summary>
        public static bool SoaHasOwnInvoice(int soaId)
        {
            if (soaId <= 0) return false;
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            return SoaHasOwnInvoice(conn, null, soaId);
        }
        private static bool SoaHasOwnInvoice(SqliteConnection conn, SqliteTransaction? tx, int soaId)
        {
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = "SELECT COUNT(*) FROM Invoices WHERE COALESCE(SoaId,0) = @sid AND COALESCE(DeliveryId,0) = 0 AND COALESCE(SpecialTripId,0) = 0 AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
            c.Parameters.AddWithValue("@sid", soaId);
            return Convert.ToInt32(c.ExecuteScalar() ?? 0) > 0;
        }

        /// <summary>
        /// Puts the service invoice of every trip on the statement onto it (and takes off the ones whose trip was removed).
        /// Throws with the reasons when a trip has no invoice that can go on a statement, or when the total is not the invoices' total.
        /// </summary>
        private static void LinkTripInvoicesToSoa(SqliteConnection conn, SqliteTransaction tx, StatementOfAccount soa, int soaId, List<StatementOfAccountItem> items)
        {
            var problems = new List<string>();
            var found = new List<(int Id, string Number, decimal Net, decimal Freight, decimal Fuel)>();
            var lineDriven = new HashSet<int>(); // unpaid invoices that were on no statement: the statement line's Amount and Fuel become the invoice's
            int unlinked = (items ?? new()).Count(i => i.DeliveryId <= 0 && i.SpecialTripId <= 0);
            if (unlinked > 0) problems.Add($"{unlinked} line(s) are not linked to a trip, so they have no service invoice. Add trips from the list instead of typing lines.");
            var trips = (items ?? new()).Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0)
                .GroupBy(i => (i.DeliveryId > 0 ? i.DeliveryId : 0, i.DeliveryId > 0 ? 0 : i.SpecialTripId)).ToList();
            foreach (var g in trips)
            {
                var (did, sid) = g.Key;
                var first = g.First();
                string label = !string.IsNullOrWhiteSpace(first.TripReportNumber) ? first.TripReportNumber : $"trip {first.TripNumber}";
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = @"SELECT i.Id, i.InvoiceNumber, i.NetAmount, COALESCE(i.Status,''), COALESCE(i.ClientName,''),
                                         CASE WHEN s.Id IS NULL OR COALESCE(s.Status,'') = 'Cancelled' COLLATE NOCASE THEN 0 ELSE s.Id END, COALESCE(s.SoaNumber,''),
                                         (SELECT COALESCE(SUM(r.AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = i.Id AND COALESCE(r.IsVoid,0) = 0),
                                         COALESCE(i.TotalAmount,0), COALESCE(i.FuelSurcharge,0)
                                  FROM Invoices i LEFT JOIN StatementsOfAccount s ON s.Id = COALESCE(i.SoaId,0)
                                  WHERE " + (did > 0 ? "COALESCE(i.DeliveryId,0) = @k" : "COALESCE(i.SpecialTripId,0) = @k") + @"
                                    AND COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE
                                  ORDER BY i.Id DESC LIMIT 1";
                c.Parameters.AddWithValue("@k", did > 0 ? did : sid);
                using var r = c.ExecuteReader();
                if (!r.Read())
                {
                    // Nov 9 (statement first): a trip may go on a statement before its service invoice exists. Its amounts are the statement lines'
                    // (typed on the statement); the invoice is made later in Billing with these amounts locked. Same client as the statement is still required.
                    string tripClient;
                    r.Close();
                    using (var cc = conn.CreateCommand())
                    {
                        cc.Transaction = tx;
                        cc.CommandText = did > 0 ? "SELECT COALESCE(ClientName,'') FROM Deliveries WHERE Id = @k" : "SELECT COALESCE(Company,'') FROM SpecialTrips WHERE Id = @k";
                        cc.Parameters.AddWithValue("@k", did > 0 ? did : sid);
                        tripClient = Convert.ToString(cc.ExecuteScalar()) ?? "";
                    }
                    if (!string.Equals(tripClient.Trim(), (soa.ClientName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                        problems.Add($"{label}: it is for {tripClient}, not {soa.ClientName}. One statement is for one client only.");
                    else if (g.Sum(x => x.Amount) < MinStatementAmount) problems.Add($"{label}: type its Amount (at least PHP {MinStatementAmount:N2}) before saving the statement.");
                    else found.Add((0, "", 0m, Centavos(g.Sum(x => x.Amount)), Centavos(g.Sum(x => x.FuelSurcharge))));
                    continue;
                }
                int id = r.GetInt32(0);
                string no = r.IsDBNull(1) ? "" : r.GetString(1);
                decimal net = Centavos(Convert.ToDecimal(r.GetValue(2)));
                string st = r.GetString(3), client = r.GetString(4);
                int onSoa = Convert.ToInt32(r.GetValue(5));
                string onSoaNo = r.GetString(6);
                decimal paid = Centavos(Convert.ToDecimal(r.GetValue(7)));
                decimal invTotal = Centavos(Convert.ToDecimal(r.GetValue(8))), invFuel = Centavos(Convert.ToDecimal(r.GetValue(9)));
                if (onSoa > 0 && onSoa != soaId) problems.Add($"{label}: SI {no} is already on {onSoaNo}.");
                else if (paid > 0 || st.Equals("Paid", StringComparison.OrdinalIgnoreCase)) problems.Add($"{label}: SI {no} already has a collection receipt, so it can't go on a statement.");
                else if (st.Equals("Draft", StringComparison.OrdinalIgnoreCase) || net <= 0) problems.Add($"{label}: SI {no} has no amount yet. Finish it in Billing.");
                else if (!string.Equals(client.Trim(), (soa.ClientName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) problems.Add($"{label}: SI {no} is for {client}, not {soa.ClientName}.");
                else if (onSoa == 0 && st.Equals("Unpaid", StringComparison.OrdinalIgnoreCase) && paid <= 0)
                {
                    if (g.Sum(x => x.Amount) < MinStatementAmount) problems.Add($"{label}: type its Amount (at least PHP {MinStatementAmount:N2}) before saving the statement.");
                    else { lineDriven.Add(id); found.Add((id, no, net, Centavos(g.Sum(x => x.Amount)), Centavos(g.Sum(x => x.FuelSurcharge)))); }
                }
                else found.Add((id, no, net, invTotal - invFuel, invFuel));
            }
            if (found.Count == 0 && problems.Count == 0) problems.Add("Add at least one completed trip report to the statement.");
            if (problems.Count > 0) throw new InvalidOperationException("This statement can't be saved yet:\n\n• " + string.Join("\n• ", problems));

            // Nov 7: the statement's totals are worked out from its invoices' freight and fuel surcharge (VAT and the 2% withholding on the freight only).
            var totals = SoaTotals.From(found.Sum(f => f.Freight), found.Sum(f => f.Fuel));
            if (Math.Abs(totals.Due - soa.TotalAmount) > 0.05m)
                throw new InvalidOperationException($"The statement's total (PHP {soa.TotalAmount:N2}) must be what its service invoices work out to (PHP {totals.Due:N2}: freight PHP {totals.TotalSales:N2}, fuel surcharge PHP {totals.Fuel:N2}, less withholding tax PHP {totals.Withholding:N2}). Nothing was saved.");

            // Invoices of trips taken off the statement go back to being lone invoices (only while nothing is paid on them).
            var keep = new HashSet<int>(found.Where(f => f.Id > 0).Select(f => f.Id));
            var drop = new List<(int Id, string No, decimal Paid, decimal Freight)>();
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = @"SELECT Id, InvoiceNumber, (SELECT COALESCE(SUM(AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = Invoices.Id AND COALESCE(r.IsVoid,0) = 0),
                                         COALESCE(TotalAmount,0), COALESCE(FuelSurcharge,0)
                                  FROM Invoices WHERE COALESCE(SoaId,0) = @sid AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
                c.Parameters.AddWithValue("@sid", soaId);
                using var r = c.ExecuteReader();
                while (r.Read()) if (!keep.Contains(r.GetInt32(0))) drop.Add((r.GetInt32(0), r.IsDBNull(1) ? "" : r.GetString(1), Centavos(Convert.ToDecimal(r.GetValue(2))), Centavos(Convert.ToDecimal(r.GetValue(3))) - Centavos(Convert.ToDecimal(r.GetValue(4)))));
            }
            var paidDrop = drop.Where(d => d.Paid > 0).ToList();
            if (paidDrop.Count > 0) throw new InvalidOperationException($"SI {string.Join(", ", paidDrop.Select(d => d.No))} on this statement already has a collection receipt, so its trip can't be taken off.");
            foreach (var d in drop)
            {
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                // Back to a lone invoice: no withholding share (the computation belongs to the statement only).
                u.CommandText = "UPDATE Invoices SET SoaId = 0, Deductions = 0, NetAmount = TotalAmount WHERE Id = @id";
                u.Parameters.AddWithValue("@id", d.Id);
                u.ExecuteNonQuery();
            }
            // The statement's withholding is worked out once on its total (as on the statement sheet) and shared out over its invoices,
            // so each invoice's net due is what is collected for it and the shares add up to the statement's total due exactly.
            var shares = SoaTotals.Share(totals.Withholding, found.Select(f => f.Freight).ToArray());
            for (int k = 0; k < found.Count; k++)
            {
                var f = found[k];
                if (f.Id <= 0) continue; // no invoice yet: it takes its share of the withholding when it is made (ApplyStatementToNewInvoice)
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = lineDriven.Contains(f.Id)
                    ? "UPDATE Invoices SET SoaId = @sid, TotalAmount = @gross, FuelSurcharge = @fuel, Deductions = @w, NetAmount = @gross - @w WHERE Id = @id"
                    : "UPDATE Invoices SET SoaId = @sid, Deductions = @w, NetAmount = TotalAmount - @w WHERE Id = @id";
                if (lineDriven.Contains(f.Id))
                {
                    u.Parameters.AddWithValue("@gross", f.Freight + f.Fuel); u.Parameters.AddWithValue("@fuel", f.Fuel);
                    RecordInvoiceAmountChange(conn, tx, f.Id, f.Number, soaId, soa.SoaNumber, f.Freight + f.Fuel, f.Fuel);
                }
                u.Parameters.AddWithValue("@sid", soaId);
                u.Parameters.AddWithValue("@w", shares[k]);
                u.Parameters.AddWithValue("@id", f.Id);
                u.ExecuteNonQuery();
            }
            soa.SalesInvoiceNo = string.Join(", ", found.Where(f => f.Id > 0).Select(f => f.Number));
            // The saved figures of the statement are the formula's.
            soa.TotalSalesVatInc = totals.TotalSales; soa.VatAmount = totals.Vat; soa.NetOfVat = totals.NetOfVat; soa.FuelSurchargeTotal = totals.Fuel; soa.WithholdingTax = totals.Withholding; soa.TotalAmount = totals.Due;
            using (var u = conn.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE StatementsOfAccount SET SalesInvoiceNo = @si, TotalSalesVatInc = @ts, VatAmount = @va, NetOfVat = @nv, FuelSurchargeTotal = @fs, WithholdingTax = @wt, TotalAmount = @ta WHERE Id = @sid";
                u.Parameters.AddWithValue("@si", soa.SalesInvoiceNo);
                u.Parameters.AddWithValue("@ts", totals.TotalSales); u.Parameters.AddWithValue("@va", totals.Vat); u.Parameters.AddWithValue("@nv", totals.NetOfVat);
                u.Parameters.AddWithValue("@fs", totals.Fuel); u.Parameters.AddWithValue("@wt", totals.Withholding); u.Parameters.AddWithValue("@ta", totals.Due);
                u.Parameters.AddWithValue("@sid", soaId);
                u.ExecuteNonQuery();
            }
        }
    }
}
