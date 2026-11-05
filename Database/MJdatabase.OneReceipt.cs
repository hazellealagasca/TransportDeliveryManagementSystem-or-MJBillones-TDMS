using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One service invoice (the typed SI No.) for the whole amount of a trip.</summary>
    public sealed class TripInvoice
    {
        public int Id;
        public string Number = "";
        public int DeliveryId;
        public int SpecialTripId;
        public int SoaId;            // 0 = not on a statement (a cancelled statement counts as none)
        public string SoaNumber = "";
        public string Client = "";
        public decimal Gross, Deductions, Net, Collected;
        public string Status = "";
        public DateTime IssueDate, DueDate;
        /// <summary>Issued with an amount and nothing paid yet: can go on a statement or be collected.</summary>
        public bool Open => Net > 0 && Collected <= 0 && !IsStatus("Paid") && !IsStatus("Partial") && !IsStatus("Draft") && !IsStatus("Cancelled");
        public bool IsStatus(string s) => string.Equals(Status, s, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What one collection receipt pays: a statement (all its open invoices) or one invoice not on a statement.</summary>
    public sealed class PayTarget
    {
        public int SoaId;
        public string SoaNumber = "";
        public string Client = "";
        public List<TripInvoice> Invoices = new();
        public decimal Gross => Invoices.Sum(i => i.Gross);
        public decimal Deductions => Invoices.Sum(i => i.Deductions);
        public decimal Net => Invoices.Sum(i => i.Net);
        public bool IsSoa => SoaId > 0;
        public string Title => IsSoa ? $"{SoaNumber} ({Invoices.Count} invoice{(Invoices.Count == 1 ? "" : "s")}: {string.Join(", ", Invoices.Select(i => i.Number))})" : $"SI {Invoices[0].Number}";
        public DateTime IssueDate => Invoices.Max(i => i.IssueDate);
        public DateTime DueDate => Invoices.Min(i => i.DueDate);
    }

    /// <summary>
    /// Nov 6, 2026: Trip → Trip Report → Service Invoice (one per trip, typed SI No.) → SOA (several invoices of one client)
    /// → ONE collection receipt for the SOA's full total (or for a lone invoice's full amount). No partial payments.
    /// A statement's receipt is stored as one row per invoice with the same receipt number, so every invoice total still works.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>Every service invoice that is not cancelled, with what is collected on it.</summary>
        public static List<TripInvoice> GetTripInvoices()
        {
            EnsureSpecialTripColumns();
            var list = new List<TripInvoice>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT i.Id, i.InvoiceNumber, COALESCE(i.DeliveryId,0), COALESCE(i.SpecialTripId,0),
                                     CASE WHEN s.Id IS NULL OR COALESCE(s.Status,'') = 'Cancelled' COLLATE NOCASE THEN 0 ELSE s.Id END,
                                     COALESCE(s.SoaNumber,''), COALESCE(i.ClientName,''), i.TotalAmount, i.Deductions, i.NetAmount, COALESCE(i.Status,''),
                                     i.IssueDate, i.DueDate,
                                     (SELECT COALESCE(SUM(r.AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = i.Id AND COALESCE(r.IsVoid,0) = 0)
                              FROM Invoices i LEFT JOIN StatementsOfAccount s ON s.Id = COALESCE(i.SoaId,0)
                              WHERE COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE";
            using var r = c.ExecuteReader();
            while (r.Read())
            {
                var t = new TripInvoice
                {
                    Id = r.GetInt32(0),
                    Number = r.IsDBNull(1) ? "" : r.GetString(1),
                    DeliveryId = Convert.ToInt32(r.GetValue(2)),
                    SpecialTripId = Convert.ToInt32(r.GetValue(3)),
                    SoaId = Convert.ToInt32(r.GetValue(4)),
                    SoaNumber = r.GetString(5),
                    Client = r.GetString(6),
                    Gross = Centavos(Convert.ToDecimal(r.GetValue(7))),
                    Deductions = Centavos(Convert.ToDecimal(r.GetValue(8))),
                    Net = Centavos(Convert.ToDecimal(r.GetValue(9))),
                    Status = r.GetString(10),
                    IssueDate = DateTime.TryParse(r.IsDBNull(11) ? "" : r.GetString(11), out var d1) ? d1 : DateTime.Today,
                    DueDate = DateTime.TryParse(r.IsDBNull(12) ? "" : r.GetString(12), out var d2) ? d2 : DateTime.Today,
                    Collected = Centavos(Convert.ToDecimal(r.GetValue(13)))
                };
                if (t.SoaId == 0) t.SoaNumber = "";
                list.Add(t);
            }
            return list;
        }

        /// <summary>Why a trip can't go on a statement yet (null = it can), using its service invoice.</summary>
        public static string? WhyTripNotReadyForSoa(TripInvoice? inv, int thisSoaId)
        {
            if (inv == null) return "No service invoice yet (make it in Billing)";
            if (inv.SoaId > 0 && inv.SoaId != thisSoaId) return $"On {inv.SoaNumber}";
            if (inv.IsStatus("Draft") || inv.Net <= 0) return $"SI {inv.Number} has no amount yet";
            if (inv.Collected > 0 || inv.IsStatus("Paid")) return $"SI {inv.Number} is already paid";
            return null;
        }

        /// <summary>What can be collected now: each statement with open invoices, and each open invoice that is not on a statement.</summary>
        public static List<PayTarget> GetPayTargets()
        {
            var open = GetTripInvoices().Where(i => i.Open).ToList();
            var list = new List<PayTarget>();
            foreach (var g in open.Where(i => i.SoaId > 0).GroupBy(i => i.SoaId))
                list.Add(new PayTarget { SoaId = g.Key, SoaNumber = g.First().SoaNumber, Client = g.First().Client, Invoices = g.OrderBy(i => i.Number).ToList() });
            foreach (var i in open.Where(i => i.SoaId == 0))
                list.Add(new PayTarget { Client = i.Client, Invoices = new List<TripInvoice> { i } });
            return list.OrderBy(t => t.DueDate).ThenBy(t => t.Title).ToList();
        }

        /// <summary>All rows (one per invoice) of a receipt number that are not voided.</summary>
        public static List<CollectionReceipt> GetReceiptRows(string receiptNumber)
            => GetCollectionReceipts().Where(r => string.Equals(r.ReceiptNumber?.Trim(), receiptNumber?.Trim(), StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.Id).ToList();

        /// <summary>
        /// Records ONE collection receipt for the full amount of the statement (every open invoice on it) or of the lone invoice.
        /// Each invoice turns Paid. Returns the rows written.
        /// </summary>
        public static List<CollectionReceipt> SaveOneReceipt(string receiptNumber, PayTarget target, string method, string reference, string notes)
        {
            string no = (receiptNumber ?? "").Trim();
            var why = CheckReceiptNumber(no);
            if (why != null) throw new InvalidOperationException(why);
            if (target == null || target.Invoices.Count == 0) throw new InvalidOperationException("Pick the statement or the invoice being paid.");
            if (string.IsNullOrWhiteSpace(method)) throw new InvalidOperationException("Choose the payment method.");
            if (target.Invoices.Count > 1) EnsureReceiptSchemaForSoa();
            var rows = new List<CollectionReceipt>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    foreach (var ti in target.Invoices)
                    {
                        // Re-read inside the transaction: still open, still on this statement, nothing paid.
                        using (var chk = conn.CreateCommand())
                        {
                            chk.Transaction = tx;
                            chk.CommandText = @"SELECT NetAmount, COALESCE(Status,''), COALESCE(SoaId,0),
                                                (SELECT COALESCE(SUM(AmountPaid),0) FROM CollectionReceipts WHERE InvoiceId = @id AND COALESCE(IsVoid,0) = 0)
                                                FROM Invoices WHERE Id = @id";
                            chk.Parameters.AddWithValue("@id", ti.Id);
                            using var rd = chk.ExecuteReader();
                            if (!rd.Read()) throw new InvalidOperationException($"Invoice {ti.Number} no longer exists.");
                            decimal net = Centavos(Convert.ToDecimal(rd.GetValue(0)));
                            string st = rd.GetString(1);
                            decimal paid = Centavos(Convert.ToDecimal(rd.GetValue(3)));
                            if (st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || st.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException($"Invoice {ti.Number} is {st.ToLowerInvariant()}, so it can't be collected.");
                            if (paid > 0) throw new InvalidOperationException($"Invoice {ti.Number} already has a receipt. Reload the list.");
                            if (net != ti.Net) throw new InvalidOperationException($"Invoice {ti.Number} changed (now PHP {net:N2}). Reload the list.");
                        }
                        var cr = new CollectionReceipt
                        {
                            ReceiptNumber = no,
                            InvoiceId = ti.Id,
                            InvoiceNumber = ti.Number,
                            PaymentDate = DateTime.Now,
                            AmountPaid = ti.Net,
                            PaymentMethod = method.Trim(),
                            ReferenceNumber = (reference ?? "").Trim(),
                            Notes = (notes ?? "").Trim(),
                            SoaId = target.SoaId
                        };
                        using (var ins = conn.CreateCommand())
                        {
                            ins.Transaction = tx;
                            ins.CommandText = @"INSERT INTO CollectionReceipts (ReceiptNumber, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes, SoaId)
                                                VALUES (@rn, @iid, @in, @pd, @ap, @pm, @rf, @n, @sid); SELECT last_insert_rowid();";
                            ins.Parameters.AddWithValue("@rn", cr.ReceiptNumber);
                            ins.Parameters.AddWithValue("@iid", cr.InvoiceId);
                            ins.Parameters.AddWithValue("@in", cr.InvoiceNumber);
                            ins.Parameters.AddWithValue("@pd", cr.PaymentDate.ToString("yyyy-MM-dd"));
                            ins.Parameters.AddWithValue("@ap", cr.AmountPaid);
                            ins.Parameters.AddWithValue("@pm", cr.PaymentMethod);
                            ins.Parameters.AddWithValue("@rf", cr.ReferenceNumber);
                            ins.Parameters.AddWithValue("@n", cr.Notes);
                            ins.Parameters.AddWithValue("@sid", cr.SoaId);
                            cr.Id = Convert.ToInt32(ins.ExecuteScalar() ?? 0);
                        }
                        RecalculateInvoiceStatus(conn, tx, ti.Id);
                        rows.Add(cr);
                    }
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            try
            {
                string what = target.IsSoa ? $"statement {target.SoaNumber} ({string.Join(", ", target.Invoices.Select(i => "SI " + i.Number))})" : $"SI {target.Invoices[0].Number}";
                LogAuditTrail("PAYMENT_COLLECTED", "CollectionReceipts", rows[0].Id, no, "AmountPaid", "", $"PHP {target.Net:N2}", $"Collection receipt {no} for {what} via {method} (Ref: {reference})");
                EventNotificationService.Publish("Payment Collected", $"Receipt #{no} for {what} (PHP {target.Net:N2})", ToastType.Success);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return rows;
        }

        /// <summary>Changes the receipt number, method, reference and notes of every row of one receipt (the amounts never change).</summary>
        public static void UpdateReceiptDetails(string oldNumber, string newNumber, string method, string reference, string notes)
        {
            string oldNo = (oldNumber ?? "").Trim(), newNo = (newNumber ?? "").Trim();
            if (!string.Equals(oldNo, newNo, StringComparison.OrdinalIgnoreCase))
            {
                var why = CheckReceiptNumber(newNo, oldNo);
                if (why != null) throw new InvalidOperationException(why);
            }
            if (string.IsNullOrWhiteSpace(method)) throw new InvalidOperationException("Choose the payment method.");
            int n;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "UPDATE CollectionReceipts SET ReceiptNumber=@nn, PaymentMethod=@pm, ReferenceNumber=@rf, Notes=@n WHERE TRIM(ReceiptNumber) = @on COLLATE NOCASE AND COALESCE(IsVoid,0) = 0";
                c.Parameters.AddWithValue("@nn", newNo);
                c.Parameters.AddWithValue("@pm", method.Trim());
                c.Parameters.AddWithValue("@rf", (reference ?? "").Trim());
                c.Parameters.AddWithValue("@n", (notes ?? "").Trim());
                c.Parameters.AddWithValue("@on", oldNo);
                n = c.ExecuteNonQuery();
            }
            if (n == 0) throw new InvalidOperationException($"Receipt {oldNo} was not found (it may have been voided).");
            try { LogAuditTrail("UPDATE", "CollectionReceipts", 0, newNo, "ReceiptNumber", oldNo, newNo, $"Receipt {oldNo} details updated ({n} row(s)): {method}, ref {reference}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        // ------------------------------------------------------------------ Schema: a receipt number shared by the rows of one statement
        private static bool receiptSchemaChecked, receiptSoaColumnChecked;
        /// <summary>Adds CollectionReceipts.SoaId (the statement a receipt pays) when it is missing. Cheap after the first call.</summary>
        public static void EnsureReceiptSoaColumn()
        {
            if (receiptSoaColumnChecked) return;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                bool has = false;
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = "PRAGMA table_info(CollectionReceipts)";
                    using var r = c.ExecuteReader();
                    while (r.Read()) if (r.GetString(1).Equals("SoaId", StringComparison.OrdinalIgnoreCase)) has = true;
                }
                if (!has)
                {
                    using var a = conn.CreateCommand();
                    a.CommandText = "ALTER TABLE CollectionReceipts ADD COLUMN SoaId INTEGER DEFAULT 0";
                    a.ExecuteNonQuery();
                }
                receiptSoaColumnChecked = true;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        /// <summary>
        /// Adds CollectionReceipts.SoaId and, once, rebuilds the table without the UNIQUE rule on ReceiptNumber (after a backup),
        /// so one statement's receipt can be stored once per invoice. The app checks that a number is never reused elsewhere.
        /// </summary>
        public static void EnsureReceiptSchemaForSoa()
        {
            if (receiptSchemaChecked) return;
            EnsureReceiptSoaColumn();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            var cols = new List<string>();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "PRAGMA table_info(CollectionReceipts)";
                using var r = c.ExecuteReader();
                while (r.Read()) cols.Add(r.GetString(1));
            }
            if (!cols.Any(x => x.Equals("SoaId", StringComparison.OrdinalIgnoreCase)))
            {
                using var a = conn.CreateCommand();
                a.CommandText = "ALTER TABLE CollectionReceipts ADD COLUMN SoaId INTEGER DEFAULT 0";
                a.ExecuteNonQuery();
                cols.Add("SoaId");
            }
            string createSql;
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='CollectionReceipts'";
                createSql = Convert.ToString(c.ExecuteScalar()) ?? "";
            }
            var uniqueRx = new System.Text.RegularExpressions.Regex(@"ReceiptNumber\s+TEXT\s+(NOT\s+NULL\s+UNIQUE|UNIQUE\s+NOT\s+NULL|UNIQUE)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!uniqueRx.IsMatch(createSql)) { receiptSchemaChecked = true; return; }

            string backup = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "Backups", $"before_one_receipt_per_soa_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            conn.Close();
            if (!BackupDatabase(backup)) throw new InvalidOperationException("Couldn't back up the database before changing the receipts table: " + LastBackupError);
            conn.Open();

            var indexSql = new List<string>();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT sql FROM sqlite_master WHERE type IN ('index','trigger') AND tbl_name='CollectionReceipts' AND sql IS NOT NULL";
                using var r = c.ExecuteReader();
                while (r.Read()) indexSql.Add(r.GetString(0));
            }
            string newSql = uniqueRx.Replace(createSql, "ReceiptNumber TEXT NOT NULL", 1);
            newSql = System.Text.RegularExpressions.Regex.Replace(newSql, @"CREATE\s+TABLE\s+(IF\s+NOT\s+EXISTS\s+)?""?CollectionReceipts""?", "CREATE TABLE CollectionReceipts_new", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string colList = string.Join(", ", cols.Select(x => "\"" + x + "\""));
            int before, after;
            using (var p = conn.CreateCommand()) { p.CommandText = "PRAGMA foreign_keys=OFF; PRAGMA legacy_alter_table=ON;"; p.ExecuteNonQuery(); }
            try
            {
                using var tx = conn.BeginTransaction();
                try
                {
                    using var c = conn.CreateCommand();
                    c.Transaction = tx;
                    c.CommandText = "SELECT COUNT(*) FROM CollectionReceipts"; before = Convert.ToInt32(c.ExecuteScalar());
                    c.CommandText = newSql; c.ExecuteNonQuery();
                    c.CommandText = $"INSERT INTO CollectionReceipts_new ({colList}) SELECT {colList} FROM CollectionReceipts"; c.ExecuteNonQuery();
                    c.CommandText = "DROP TABLE CollectionReceipts"; c.ExecuteNonQuery();
                    c.CommandText = "ALTER TABLE CollectionReceipts_new RENAME TO CollectionReceipts"; c.ExecuteNonQuery();
                    foreach (var s in indexSql) { c.CommandText = s; c.ExecuteNonQuery(); }
                    c.CommandText = "CREATE INDEX IF NOT EXISTS IX_CollectionReceipts_ReceiptNumber ON CollectionReceipts(ReceiptNumber)"; c.ExecuteNonQuery();
                    c.CommandText = "SELECT COUNT(*) FROM CollectionReceipts"; after = Convert.ToInt32(c.ExecuteScalar());
                    if (before != after) throw new InvalidOperationException($"Receipts table rebuild: {before} rows before, {after} after. Nothing was changed.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            finally
            {
                using var p = conn.CreateCommand(); p.CommandText = "PRAGMA legacy_alter_table=OFF; PRAGMA foreign_keys=ON;"; p.ExecuteNonQuery();
            }
            receiptSchemaChecked = true;
            try { LogAuditTrail("SCHEMA", "CollectionReceipts", 0, "CollectionReceipts", "ReceiptNumber", "UNIQUE", "shared by one statement's invoices", $"Receipts table rebuilt so one SOA has one receipt number ({after} rows kept). Backup: {backup}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        // ------------------------------------------------------------------ One-time cleanup of the records made before Nov 6 (asked for by Hazel)
        /// <summary>
        /// Once, after a backup: receipts with system numbers get 0001… in date order (old number kept in the notes); SI-1001's two
        /// receipts become one; invoice 0765's receipt is raised to the full amount; the two system SI numbers are renamed 2451 and 8574.
        /// Every change is written to the audit log.
        /// </summary>
        public static void RunPaymentCleanupOnce()
        {
            const string key = "Nov6_PaymentCleanupDone";
            if (GetSystemSetting(key, "0") == "1") return;
            EnsureReceiptSchemaForSoa();
            string backup = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "Backups", $"before_payment_cleanup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            if (!BackupDatabase(backup)) throw new InvalidOperationException("Couldn't back up the database before the payment cleanup: " + LastBackupError);
            var log = new List<(string Table, int Id, string Ident, string Field, string Old, string New, string Why)>();
            var touchedInvoices = new HashSet<int>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                bool hasVoidReason = false, hasVoidedAt = false;
                using (var c0 = conn.CreateCommand())
                {
                    c0.CommandText = "PRAGMA table_info(CollectionReceipts)";
                    using var r0 = c0.ExecuteReader();
                    while (r0.Read()) { string cn = r0.GetString(1); if (cn.Equals("VoidReason", StringComparison.OrdinalIgnoreCase)) hasVoidReason = true; if (cn.Equals("VoidedAt", StringComparison.OrdinalIgnoreCase)) hasVoidedAt = true; }
                }
                using var tx = conn.BeginTransaction();
                try
                {
                    SqliteCommand Cmd(string sql, params (string, object)[] ps)
                    {
                        var c = conn.CreateCommand(); c.Transaction = tx; c.CommandText = sql;
                        foreach (var (n, v) in ps) c.Parameters.AddWithValue(n, v);
                        return c;
                    }
                    // 1. Renumber receipts that are not 4–8 digits, in date order, from 0001 (skipping numbers already used).
                    var all = new List<(int Id, string No, string Date, string Notes, int InvoiceId)>();
                    using (var c = Cmd("SELECT Id, ReceiptNumber, PaymentDate, COALESCE(Notes,''), InvoiceId FROM CollectionReceipts WHERE COALESCE(IsVoid,0) = 0 ORDER BY PaymentDate, Id"))
                    using (var r = c.ExecuteReader())
                        while (r.Read()) all.Add((r.GetInt32(0), r.GetString(1).Trim(), r.GetString(2), r.GetString(3), r.GetInt32(4)));
                    var used = new HashSet<string>(all.Where(a => System.Text.RegularExpressions.Regex.IsMatch(a.No, @"^\d{4,8}$")).Select(a => a.No.TrimStart('0')));
                    int next = 1;
                    foreach (var a in all.Where(a => !System.Text.RegularExpressions.Regex.IsMatch(a.No, @"^\d{4,8}$")))
                    {
                        while (used.Contains(next.ToString())) next++;
                        string nn = next.ToString("D4");
                        used.Add(next.ToString());
                        string notes = (a.Notes.Length > 0 ? a.Notes + " | " : "") + $"Old receipt no. {a.No}";
                        using (var c = Cmd("UPDATE CollectionReceipts SET ReceiptNumber=@n, Notes=@notes WHERE Id=@id", ("@n", nn), ("@notes", notes), ("@id", a.Id))) c.ExecuteNonQuery();
                        log.Add(("CollectionReceipts", a.Id, nn, "ReceiptNumber", a.No, nn, "Nov 6 cleanup: receipt numbers are 4 to 8 digits; renumbered in date order"));
                    }
                    // Invoice id by its number.
                    int InvoiceId(string number)
                    {
                        using var c = Cmd("SELECT Id FROM Invoices WHERE TRIM(InvoiceNumber) = @n COLLATE NOCASE LIMIT 1", ("@n", number));
                        return Convert.ToInt32(c.ExecuteScalar() ?? 0);
                    }
                    List<(int Id, string No, decimal Amt, string Ref, string Notes)> ActiveReceipts(int invId)
                    {
                        var l = new List<(int, string, decimal, string, string)>();
                        using var c = Cmd("SELECT Id, ReceiptNumber, AmountPaid, COALESCE(ReferenceNumber,''), COALESCE(Notes,'') FROM CollectionReceipts WHERE InvoiceId=@i AND COALESCE(IsVoid,0)=0 ORDER BY Id", ("@i", invId));
                        using var r = c.ExecuteReader();
                        while (r.Read()) l.Add((r.GetInt32(0), r.GetString(1), Centavos(Convert.ToDecimal(r.GetValue(2))), r.GetString(3), r.GetString(4)));
                        return l;
                    }
                    // 2. SI-1001: keep the first receipt for the total of both; void the second ("merged").
                    int si1001 = InvoiceId("SI-1001");
                    if (si1001 > 0)
                    {
                        var rs = ActiveReceipts(si1001);
                        if (rs.Count > 1)
                        {
                            var first = rs[0];
                            decimal total = rs.Sum(x => x.Amt);
                            string refs = string.Join(" + ", rs.Select(x => x.Ref).Where(x => x.Length > 0));
                            string notes = (first.Notes.Length > 0 ? first.Notes + " | " : "") + $"Merged {rs.Count} receipts ({string.Join(" + ", rs.Select(x => x.Amt.ToString("N2")))}){(refs.Length > 0 ? ", refs " + refs : "")}";
                            using (var c = Cmd("UPDATE CollectionReceipts SET AmountPaid=@a, Notes=@n WHERE Id=@id", ("@a", total), ("@n", notes), ("@id", first.Id))) c.ExecuteNonQuery();
                            log.Add(("CollectionReceipts", first.Id, first.No, "AmountPaid", first.Amt.ToString("N2"), total.ToString("N2"), "Nov 6 cleanup: one receipt per invoice; SI-1001's receipts merged into the first"));
                            foreach (var x in rs.Skip(1))
                            {
                                using (var c = Cmd("UPDATE CollectionReceipts SET IsVoid=1" + (hasVoidReason ? ", VoidReason=@why" : "") + (hasVoidedAt ? ", VoidedAt=@at, VoidedBy=@by" : "") + ", Notes=@n WHERE Id=@id",
                                    ("@why", "merged"), ("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), ("@by", "Nov 6 cleanup (merged)"), ("@n", (x.Notes.Length > 0 ? x.Notes + " | " : "") + $"Voided: merged into receipt {first.No}"), ("@id", x.Id))) c.ExecuteNonQuery();
                                log.Add(("CollectionReceipts", x.Id, x.No, "IsVoid", "0", "1", $"Nov 6 cleanup: merged into receipt {first.No} (reason: merged)"));
                            }
                            touchedInvoices.Add(si1001);
                        }
                    }
                    // 3. Invoice 0765: raise its receipt to the full amount (no partial payments).
                    int i0765 = InvoiceId("0765");
                    if (i0765 > 0)
                    {
                        decimal net;
                        using (var c = Cmd("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", i0765))) net = Centavos(Convert.ToDecimal(c.ExecuteScalar() ?? 0));
                        var rs = ActiveReceipts(i0765);
                        if (rs.Count == 1 && rs[0].Amt < net)
                        {
                            var x = rs[0];
                            using (var c = Cmd("UPDATE CollectionReceipts SET AmountPaid=@a, Notes=@n WHERE Id=@id", ("@a", net), ("@n", (x.Notes.Length > 0 ? x.Notes + " | " : "") + $"Raised from {x.Amt:N2} to {net:N2} (no partial payments)"), ("@id", x.Id))) c.ExecuteNonQuery();
                            log.Add(("CollectionReceipts", x.Id, x.No, "AmountPaid", x.Amt.ToString("N2"), net.ToString("N2"), "Nov 6 cleanup: no partial payments; receipt raised to the invoice's full amount"));
                            touchedInvoices.Add(i0765);
                        }
                    }
                    // 4. System SI numbers renamed to the typed ones.
                    foreach (var (oldNo, newNo) in new[] { ("INV-20260923-161652-85947", "2451"), ("INV-ST-20261105-0001", "8574") })
                    {
                        int id = InvoiceId(oldNo);
                        if (id == 0 || InvoiceId(newNo) > 0) continue;
                        if (oldNo.StartsWith("INV-ST-", StringComparison.OrdinalIgnoreCase))
                        {
                            // Keep the special trip's link before the "INV-<order ref>" name goes away.
                            try
                            {
                                using var c = Cmd("UPDATE Invoices SET SpecialTripId = (SELECT Id FROM SpecialTrips WHERE OrderReference = @r LIMIT 1) WHERE Id=@id AND COALESCE(SpecialTripId,0)=0 AND EXISTS (SELECT 1 FROM SpecialTrips WHERE OrderReference = @r)", ("@r", oldNo.Substring(4)), ("@id", id));
                                c.ExecuteNonQuery();
                            }
                            catch (Exception ex) { ErrLog.Swallowed(ex); }
                        }
                        using (var c = Cmd("UPDATE Invoices SET InvoiceNumber=@n WHERE Id=@id", ("@n", newNo), ("@id", id))) c.ExecuteNonQuery();
                        using (var c = Cmd("UPDATE CollectionReceipts SET InvoiceNumber=@n WHERE InvoiceId=@id", ("@n", newNo), ("@id", id))) c.ExecuteNonQuery();
                        log.Add(("Invoices", id, newNo, "InvoiceNumber", oldNo, newNo, "Nov 6 cleanup: SI numbers are typed; system number replaced by the printed SI No."));
                    }
                    foreach (int inv in touchedInvoices) RecalculateInvoiceStatus(conn, tx, inv);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            SetSystemSetting(key, "1", "Nov 6, 2026 payment records cleanup done (backup: " + Path.GetFileName(backup) + ")");
            foreach (var e in log)
                try { LogAuditTrail("UPDATE", e.Table, e.Id, e.Ident, e.Field, e.Old, e.New, e.Why); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
