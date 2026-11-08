using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One typed number that exists in the system, with who owns it and in which state (plan 17.5).</summary>
    public sealed class NumberEntry
    {
        public NumberKind Kind;
        public string Text = "";
        public string Key = "";
        public string Core = "";
        public int OwnerId;
        /// <summary>Active, Pending (cancelled as a typing mistake, waiting to be reinstated), Cancelled / Voided (burned).</summary>
        public string State = "Active";
        public string Client = "";
        public DateTime? Date;
        public string Detail = "";
        public DateTime? ReenterUntil;
        public bool IsBurned => State == "Cancelled" || State == "Voided";
        public string Describe()
        {
            string when = Date.HasValue ? $", {Date.Value:MMM d, yyyy}" : "";
            return Kind switch
            {
                NumberKind.SI => $"the service invoice {Text}{(Client.Length > 0 ? " of " + Client : "")}{when}",
                NumberKind.CR => $"the collection receipt {Text}{(Client.Length > 0 ? " of " + Client : "")}{when}",
                NumberKind.DR => $"DR {Text}{(Detail.Length > 0 ? " on " + Detail : "")}",
                _ => $"dispatch {Text}{(Client.Length > 0 ? " of " + Client : "")}{when}"
            };
        }
    }

    /// <summary>The answer to "may this number be typed here?" (plan 17.4).</summary>
    public sealed class NumberCheck
    {
        public NumberKind Kind;
        public string Key = "";
        public bool Empty;
        public string? FormatError;
        /// <summary>The same-type owner that makes the number unusable (null when free).</summary>
        public NumberEntry? Conflict;
        /// <summary>Same digits on a different kind of document.</summary>
        public List<NumberEntry> CrossHits = new();
        /// <summary>True when a service invoice / collection receipt pair needs the person's confirmation and has none yet.</summary>
        public bool TickNeeded;
        public List<string> Warnings = new();
        public string? Hint;
        public string? Recommendation;

        public bool Blocked => Empty || FormatError != null || Conflict != null;

        /// <summary>The message to show when <see cref="Blocked"/>.</summary>
        public string Why()
        {
            if (Empty) return $"Type the {NumberKey.Label(Kind)} (it is not made by the system).";
            if (FormatError != null) return FormatError;
            if (Conflict == null) return "";
            var c = Conflict;
            string who = c.Describe();
            return c.State switch
            {
                "Pending" => $"{NumberKey.Label(Kind)} {Text(c)} is kept for {who}, cancelled as a typing mistake. Only that record can use it again (reinstate it) until {c.ReenterUntil:MMM d, yyyy}.",
                "Cancelled" or "Voided" => $"{NumberKey.Label(Kind)} {Text(c)} was used by {who} (cancelled). A number can be used only once, even after the document is cancelled.",
                _ => $"{NumberKey.Label(Kind)} {Text(c)} is already used by {who}. A number can be used only once."
            };
        }
        private static string Text(NumberEntry c) => c.Text;

        /// <summary>The confirmation text for a service invoice / collection receipt pair (empty when none is needed).</summary>
        public string TickMessage()
        {
            var pair = CrossHits.FirstOrDefault(h => h.Kind == NumberKind.SI || h.Kind == NumberKind.CR);
            if (pair == null) return "";
            string mine = Kind == NumberKind.SI ? "Service Invoice No." : "Collection Receipt No.";
            return $"{pair.Text} is also a {NumberKey.Label(pair.Kind)} ({pair.Describe()}). This field is the {mine} Check that you copied the number printed on the {(Kind == NumberKind.SI ? "service invoice" : "official receipt")}.";
        }
    }

    /// <summary>Thrown by a save when a same-digits service invoice / receipt pair has not been confirmed.</summary>
    public sealed class NumberNeedsConfirmationException : InvalidOperationException
    {
        public NumberNeedsConfirmationException(string message) : base(message) { }
    }

    /// <summary>
    /// A snapshot of every typed number in the system (service invoices, collection receipts, dispatch numbers, DRs) so a typed number can
    /// be judged instantly (the screens check it on every keystroke). Built from the document tables themselves, so rows made by any path count.
    /// </summary>
    public sealed class NumberIndex
    {
        public readonly List<NumberEntry> Entries = new();
        private readonly HashSet<string> acks = new(StringComparer.Ordinal);
        public DateTime LoadedAt = DateTime.Now;

        internal void AddAck(string kind, string key, string otherKind, string otherKey) => acks.Add($"{kind}|{key}|{otherKind}|{otherKey}");
        internal bool Acked(NumberKind k, string key, NumberKind other, string otherKey) => acks.Contains($"{k}|{key}|{other}|{otherKey}");

        private static readonly Regex SiFormat = new(@"^[A-Za-z0-9][A-Za-z0-9\-/]*$", RegexOptions.Compiled);
        private static readonly Regex CrFormat = new(@"^\d{4,8}$", RegexOptions.Compiled);

        /// <param name="exceptInvoiceId">The invoice being edited or reinstated (its own number is not a clash).</param>
        /// <param name="exceptReceipt">The receipt number being edited (its own number is not a clash).</param>
        public NumberCheck Check(NumberKind kind, string? text, int exceptInvoiceId = 0, string? exceptReceipt = null)
        {
            var r = new NumberCheck { Kind = kind };
            string t = (text ?? "").Trim();
            r.Key = NumberKey.Of(kind, t);
            if (t.Length == 0) { r.Empty = true; return r; }
            if (kind == NumberKind.SI)
            {
                if (t.Length > MJdatabase.SiNumberMaxLength) { r.FormatError = $"The Service Invoice No. can be at most {MJdatabase.SiNumberMaxLength} characters."; return r; }
                if (!SiFormat.IsMatch(t)) { r.FormatError = "The Service Invoice No. can only have letters, digits, - and /."; return r; }
            }
            else if (kind == NumberKind.CR && !CrFormat.IsMatch(t))
            {
                r.FormatError = "The Collection Receipt No. must be 4 to 8 digits (numbers only).";
                return r;
            }

            string? exceptKey = kind == NumberKind.CR && !string.IsNullOrWhiteSpace(exceptReceipt) ? NumberKey.Of(NumberKind.CR, exceptReceipt) : null;
            foreach (var e in Entries)
            {
                if (e.Kind != kind || e.Key != r.Key) continue;
                if (kind == NumberKind.SI && exceptInvoiceId > 0 && e.OwnerId == exceptInvoiceId) continue;
                if (exceptKey != null && e.Key == exceptKey) continue;
                // Prefer the most informative owner: a live one over a cancelled one.
                if (r.Conflict == null || (r.Conflict.State != "Active" && e.State == "Active")) r.Conflict = e;
            }
            if (r.Conflict != null) return r;

            if (kind == NumberKind.SI || kind == NumberKind.CR)
            {
                string core = NumberKey.Core(t);
                if (core.Length >= 3)
                {
                    foreach (var e in Entries)
                    {
                        if (e.Kind == kind || e.Core != core || e.State == "Pending") continue;
                        if (r.CrossHits.Any(h => h.Kind == e.Kind && h.Key == e.Key)) continue;
                        r.CrossHits.Add(e);
                    }
                    var pairs = r.CrossHits.Where(h => h.Kind == NumberKind.SI || h.Kind == NumberKind.CR).ToList();
                    r.TickNeeded = pairs.Any(h => !Acked(kind, r.Key, h.Kind, h.Key));
                    foreach (var h in r.CrossHits.Take(3))
                        r.Warnings.Add($"{h.Text} is also a {NumberKey.Label(h.Kind)} ({h.Describe()}).");
                    if (r.CrossHits.Count > 0)
                        r.Recommendation = kind == NumberKind.SI
                            ? "Check that you are copying the number printed on the service invoice, not another document."
                            : "Check that you are copying the number printed on the official receipt, not another document.";
                }
                AddGapAndHint(r, kind, t);
            }
            return r;
        }

        private void AddGapAndHint(NumberCheck r, NumberKind kind, string typed)
        {
            long highest = 0;
            foreach (var e in Entries)
                if (e.Kind == kind && e.Key.Length is > 0 and <= 12 && e.Key.All(char.IsDigit) && long.TryParse(e.Key, NumberStyles.None, CultureInfo.InvariantCulture, out long n) && n > highest) highest = n;
            int width = kind == NumberKind.CR ? Math.Max(4, typed.Length) : 0;
            string F(long n) => width > 0 ? n.ToString().PadLeft(width, '0') : n.ToString();
            if (highest > 0) r.Hint = $"Last {NumberKey.Short(kind)} used: {F(highest)} · next: {F(highest + 1)}";
            if (r.Key.Length is > 0 and <= 12 && r.Key.All(char.IsDigit) && long.TryParse(r.Key, NumberStyles.None, CultureInfo.InvariantCulture, out long typedN) && highest > 0 && typedN > highest + 1)
                r.Warnings.Add(typedN - 1 > highest + 1
                    ? $"{F(typedN)} skips {F(highest + 1)} to {F(typedN - 1)}: those numbers are not used yet. Check the number."
                    : $"{F(typedN)} skips {F(highest + 1)}, which is not used yet. Check the number.");
        }
    }

    public static partial class MJdatabase
    {
        private static bool numberSchemaReady;
        /// <summary>How long a number cancelled as a typing mistake stays kept for its corrected record.</summary>
        public const int PendingReenterDays = 7;

        /// <summary>
        /// Adds the columns and indexes that keep typed numbers unique, and back-fills the key of every existing row (once per database).
        /// Safe to call often; call it before opening a write transaction (it may write).
        /// </summary>
        public static void EnsureNumberSchema()
        {
            if (numberSchemaReady) return;
            try
            {
                EnsureSpecialTripColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                foreach (var (col, ddl) in new[] { ("NumberKey", "TEXT"), ("CancelKind", "TEXT"), ("CancelReason", "TEXT"), ("CancelledAt", "TEXT"), ("CancelledBy", "TEXT"), ("ReenterUntil", "TEXT"), ("ReinstatedAt", "TEXT") })
                    AddColumnIfMissing(conn, "Invoices", col, ddl);
                foreach (var (col, ddl) in new[] { ("NumberKey", "TEXT"), ("VoidKind", "TEXT"), ("VoidReason", "TEXT"), ("ReenterUntil", "TEXT") })
                    AddColumnIfMissing(conn, "CollectionReceipts", col, ddl);
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = @"CREATE TABLE IF NOT EXISTS NumberAcks (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        Kind TEXT NOT NULL, NumberKey TEXT NOT NULL, OtherKind TEXT NOT NULL, OtherKey TEXT NOT NULL,
                                        ConfirmedBy TEXT, ConfirmedAt TEXT,
                                        UNIQUE (Kind, NumberKey, OtherKind, OtherKey));";
                    c.ExecuteNonQuery();
                }
                using (var tx = conn.BeginTransaction())
                {
                    BackfillKeys(conn, tx, "Invoices", "InvoiceNumber", NumberKind.SI);
                    BackfillKeys(conn, tx, "CollectionReceipts", "ReceiptNumber", NumberKind.CR);
                    tx.Commit();
                }
                foreach (var sql in new[]
                {
                    "CREATE UNIQUE INDEX IF NOT EXISTS ux_invoices_numberkey ON Invoices(NumberKey) WHERE NumberKey IS NOT NULL AND NumberKey <> ''",
                    "CREATE UNIQUE INDEX IF NOT EXISTS ux_invoices_number_ci ON Invoices(UPPER(TRIM(InvoiceNumber)))",
                    "CREATE INDEX IF NOT EXISTS ix_receipts_numberkey ON CollectionReceipts(NumberKey)"
                })
                {
                    // Same approach as the other unique indexes: when old data already clashes the index is skipped (and logged), never half-made.
                    try { using var c = conn.CreateCommand(); c.CommandText = sql; c.ExecuteNonQuery(); }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }
                numberSchemaReady = true;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private static void BackfillKeys(SqliteConnection conn, SqliteTransaction tx, string table, string column, NumberKind kind)
        {
            var rows = new List<(int Id, string Text)>();
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = $"SELECT Id, COALESCE({column}, '') FROM {table} WHERE NumberKey IS NULL";
                using var r = c.ExecuteReader();
                while (r.Read()) rows.Add((r.GetInt32(0), r.GetString(1)));
            }
            foreach (var (id, text) in rows)
            {
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = $"UPDATE {table} SET NumberKey = @k WHERE Id = @id";
                u.Parameters.AddWithValue("@k", NumberKey.Of(kind, text));
                u.Parameters.AddWithValue("@id", id);
                u.ExecuteNonQuery();
            }
        }

        private static DateTime? ParseWhen(string? s) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        /// <summary>Reads every typed number from the database (see <see cref="NumberIndex"/>).</summary>
        public static NumberIndex LoadNumberIndex()
        {
            EnsureNumberSchema();
            var idx = new NumberIndex();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT Id, COALESCE(InvoiceNumber,''), COALESCE(NumberKey,''), COALESCE(ClientName,''), COALESCE(IssueDate,''), COALESCE(Status,''), COALESCE(CancelKind,''), COALESCE(ReenterUntil,'') FROM Invoices";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    string text = r.GetString(1).Trim();
                    string key = r.GetString(2).Length > 0 ? r.GetString(2) : NumberKey.Of(NumberKind.SI, text);
                    string state = "Active";
                    DateTime? until = ParseWhen(r.GetString(7));
                    if (r.GetString(5).Equals(PayStatusCancelled, StringComparison.OrdinalIgnoreCase))
                        state = r.GetString(6) == "TypingMistake" && until.HasValue && until.Value >= DateTime.Now ? "Pending" : "Cancelled";
                    idx.Entries.Add(new NumberEntry { Kind = NumberKind.SI, Text = text, Key = key, Core = NumberKey.Core(text), OwnerId = r.GetInt32(0), State = state, Client = r.GetString(3), Date = ParseWhen(r.GetString(4)), ReenterUntil = until });
                }
            }

            // A receipt is stored as one row per invoice with the same number: one entry per receipt number.
            var groups = new Dictionary<string, NumberEntry>(StringComparer.Ordinal);
            var groupAllVoid = new Dictionary<string, bool>(StringComparer.Ordinal);
            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"SELECT r.Id, COALESCE(r.ReceiptNumber,''), COALESCE(r.NumberKey,''), COALESCE(r.PaymentDate,''), COALESCE(v.ClientName,''),
                                         COALESCE(r.IsVoid,0), COALESCE(r.VoidKind,''), COALESCE(r.ReenterUntil,'')
                                  FROM CollectionReceipts r LEFT JOIN Invoices v ON v.Id = r.InvoiceId ORDER BY r.Id";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    string text = r.GetString(1).Trim();
                    string key = r.GetString(2).Length > 0 ? r.GetString(2) : NumberKey.Of(NumberKind.CR, text);
                    bool isVoid = Convert.ToInt32(r.GetValue(5)) == 1;
                    DateTime? until = ParseWhen(r.GetString(7));
                    if (!groups.TryGetValue(key, out var e))
                    {
                        e = new NumberEntry { Kind = NumberKind.CR, Text = text, Key = key, Core = NumberKey.Core(text), OwnerId = r.GetInt32(0), Client = r.GetString(4), Date = ParseWhen(r.GetString(3)), ReenterUntil = until };
                        groups[key] = e; groupAllVoid[key] = true;
                    }
                    if (!isVoid) groupAllVoid[key] = false;
                    else if (r.GetString(6) == "TypingMistake" && until.HasValue) e.ReenterUntil = until;
                    else if (!e.ReenterUntil.HasValue) e.ReenterUntil = null;
                    if (e.Client.Length == 0) e.Client = r.GetString(4);
                    if (isVoid && r.GetString(6) == "TypingMistake") e.Detail = "TypingMistake";
                }
            }
            foreach (var kv in groups)
            {
                var e = kv.Value;
                if (!groupAllVoid[kv.Key]) e.State = "Active";
                else e.State = e.Detail == "TypingMistake" && e.ReenterUntil.HasValue && e.ReenterUntil.Value >= DateTime.Now ? "Pending" : "Voided";
                e.Detail = "";
                idx.Entries.Add(e);
            }

            // Dispatch numbers and DRs (read only here: they warn, they never ask for a tick).
            try
            {
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT Id, COALESCE(DispatchNumber,''), COALESCE(ClientName,''), COALESCE(DispatchDate,''), COALESCE(Status,'') FROM Deliveries";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    string text = r.GetString(1).Trim();
                    if (text.Length == 0) continue;
                    idx.Entries.Add(new NumberEntry { Kind = NumberKind.DSP, Text = text, Key = NumberKey.Of(NumberKind.DSP, text), Core = NumberKey.Core(text), OwnerId = r.GetInt32(0), Client = r.GetString(2), Date = ParseWhen(r.GetString(3)), State = r.GetString(4).Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled" : "Active" });
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            try
            {
                using var c = conn.CreateCommand();
                c.CommandText = @"SELECT ds.DeliveryId, COALESCE(ds.DrNumbers,''), COALESCE(ds.StoreCode,''), COALESCE(ds.Destination,''), COALESCE(d.DispatchNumber,'')
                                  FROM DispatchStops ds JOIN Deliveries d ON d.Id = ds.DeliveryId
                                  WHERE COALESCE(ds.DrNumbers,'') <> '' AND COALESCE(d.Status,'') <> 'Cancelled'";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    string where = $"{r.GetString(4)} · {r.GetString(3)}{(r.GetString(2).Length > 0 ? " (" + r.GetString(2) + ")" : "")}";
                    foreach (var dr in DrList.Parse(r.GetString(1)))
                        idx.Entries.Add(new NumberEntry { Kind = NumberKind.DR, Text = dr, Key = NumberKey.Of(NumberKind.DR, dr), Core = NumberKey.Core(dr), OwnerId = r.GetInt32(0), Detail = where });
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }

            try
            {
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT Kind, NumberKey, OtherKind, OtherKey FROM NumberAcks";
                using var r = c.ExecuteReader();
                while (r.Read()) idx.AddAck(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return idx;
        }

        private const string PayStatusCancelled = "Cancelled";

        /// <summary>Remembers that the person confirmed each same-digits service invoice / receipt pair of <paramref name="check"/>.</summary>
        public static void RecordNumberAcks(NumberCheck check, string text)
        {
            if (check == null || check.CrossHits.Count == 0) return;
            try
            {
                EnsureNumberSchema();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                foreach (var h in check.CrossHits.Where(h => h.Kind == NumberKind.SI || h.Kind == NumberKind.CR))
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = "INSERT OR IGNORE INTO NumberAcks (Kind, NumberKey, OtherKind, OtherKey, ConfirmedBy, ConfirmedAt) VALUES (@k, @nk, @ok, @okey, @by, @at)";
                    c.Parameters.AddWithValue("@k", check.Kind.ToString());
                    c.Parameters.AddWithValue("@nk", check.Key);
                    c.Parameters.AddWithValue("@ok", h.Kind.ToString());
                    c.Parameters.AddWithValue("@okey", h.Key);
                    c.Parameters.AddWithValue("@by", SessionContext.CurrentUsername);
                    c.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    c.ExecuteNonQuery();
                }
                LogAuditTrail("NUMBER_CONFIRMED", check.Kind == NumberKind.SI ? "Invoices" : "CollectionReceipts", 0, text, "Number", "", check.Key,
                    $"Confirmed that {NumberKey.Label(check.Kind)} {text} may share its digits with: {string.Join("; ", check.CrossHits.Take(3).Select(h => NumberKey.Label(h.Kind) + " " + h.Text))}");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
