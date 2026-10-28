using System;

using System.Collections.Generic;

using System.Data;

using System.IO;

using System.Linq;

using System.Security.Cryptography;

using System.Text;

using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database

{

    public enum AuthResult

    {

        Success,

        PendingApproval,

        InvalidCredentials,

        Inactive

    }

    public static partial class MJdatabase

    {

        private static string dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mj_billones.db");

        private static string connectionString = $"Data Source={dbFilePath};Default Timeout=10;Foreign Keys=True;";

        public static string DbFilePath => dbFilePath;

        // Single source of truth for status vocabulary used by conflict checks and resource locking.

        public const string ActiveDeliveryStatusesSql = "('Scheduled', 'Pending', 'In Transit', 'Rescheduled', 'Dispatched')";

        public const string ActiveSpecialTripStatusesSql = "('Pending', 'Scheduled', 'In Transit', 'Rescheduled')";

        public const string BlockedTruckStatusesSql = "('Under Maintenance', 'Maintenance', 'Under Repair', 'Inactive')";

        public static bool IsTruckBlockedStatus(string? status)

        {

            if (string.IsNullOrWhiteSpace(status)) return false;

            string s = status.Trim();

            return s.Equals("Under Maintenance", StringComparison.OrdinalIgnoreCase) ||

                   s.Equals("Maintenance", StringComparison.OrdinalIgnoreCase) ||

                   s.Equals("Under Repair", StringComparison.OrdinalIgnoreCase) ||

                   s.Equals("Inactive", StringComparison.OrdinalIgnoreCase);

        }

        public static bool IsTruckReadyStatus(string? status)

        {

            if (string.IsNullOrWhiteSpace(status)) return false;

            string s = status.Trim();

            return s.Equals("Active", StringComparison.OrdinalIgnoreCase) || s.Equals("Available", StringComparison.OrdinalIgnoreCase);

        }

        private static List<int> ParseIdList(string? csv)

        {

            var list = new List<int>();

            if (string.IsNullOrWhiteSpace(csv)) return list;

            foreach (var s in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

            {

                if (int.TryParse(s.Trim(), out int v) && v > 0 && !list.Contains(v)) list.Add(v);

            }

            return list;

        }

        // SQL fragment that matches an exact id inside the comma/semicolon separated AideIds column.

        private const string AideIdsContainsSql = "(',' || REPLACE(REPLACE(COALESCE(AideIds, ''), ' ', ''), ';', ',') || ',') LIKE ('%,' || @aidToken || ',%')";

        /// <summary>

        /// Recomputes truck "In Use" and crew availability from the active trips that have started (dispatch date today or earlier,

        /// or already In Transit). Trucks under maintenance or inactive are never touched.

        /// </summary>

        public static void ReconcileResourceStatuses(SqliteConnection conn, SqliteTransaction? tx)

        {

            var busyTrucks = new HashSet<int>();

            var busyCrew = new HashSet<int>();

            string today = DateTime.Today.ToString("yyyy-MM-dd");

            using (var cmd = conn.CreateCommand())

            {

                cmd.Transaction = tx;

                cmd.CommandText = $"SELECT TruckId, DriverId, AideId, AideIds FROM Deliveries WHERE Status IN {ActiveDeliveryStatusesSql} AND (Status = 'In Transit' OR DATE(DispatchDate) <= DATE(@today))";

                cmd.Parameters.AddWithValue("@today", today);

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    if (!r.IsDBNull(0) && r.GetInt32(0) > 0) busyTrucks.Add(r.GetInt32(0));

                    if (!r.IsDBNull(1) && r.GetInt32(1) > 0) busyCrew.Add(r.GetInt32(1));

                    if (!r.IsDBNull(2) && r.GetInt32(2) > 0) busyCrew.Add(r.GetInt32(2));

                    foreach (int a in ParseIdList(r.IsDBNull(3) ? "" : r.GetString(3))) busyCrew.Add(a);

                }

            }

            try

            {

                using var cmd = conn.CreateCommand();

                cmd.Transaction = tx;

                cmd.CommandText = $"SELECT TruckId, DriverId, AideId FROM SpecialTrips WHERE Status IN {ActiveSpecialTripStatusesSql} AND (Status = 'In Transit' OR DATE(DispatchDate) <= DATE(@today))";

                cmd.Parameters.AddWithValue("@today", today);

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    if (!r.IsDBNull(0) && r.GetInt32(0) > 0) busyTrucks.Add(r.GetInt32(0));

                    if (!r.IsDBNull(1) && r.GetInt32(1) > 0) busyCrew.Add(r.GetInt32(1));

                    if (!r.IsDBNull(2) && r.GetInt32(2) > 0) busyCrew.Add(r.GetInt32(2));

                }

            }

            catch (SqliteException ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] Reconcile special trips: {ex.Message}");

            }

            using (var cmd = conn.CreateCommand())

            {

                cmd.Transaction = tx;

                string inList = busyTrucks.Count > 0 ? string.Join(",", busyTrucks) : "-1";

                cmd.CommandText = $"UPDATE Trucks SET Status = CASE WHEN Id IN ({inList}) THEN 'In Use' ELSE 'Active' END WHERE Status IN ('In Use', 'Active', 'Available', 'On Trip')";

                cmd.ExecuteNonQuery();

            }

            using (var cmd = conn.CreateCommand())

            {

                cmd.Transaction = tx;

                string inList = busyCrew.Count > 0 ? string.Join(",", busyCrew) : "-1";

                cmd.CommandText = $"UPDATE Crews SET IsAvailable = CASE WHEN Id IN ({inList}) THEN 0 ELSE 1 END";

                cmd.ExecuteNonQuery();

            }

        }

        public static void ReconcileResourceStatuses()

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var tx = conn.BeginTransaction();

                ReconcileResourceStatuses(conn, tx);

                tx.Commit();

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] ReconcileResourceStatuses: {ex.Message}");

            }

        }

        /// <summary>Recomputes an invoice's payment status from its collected receipts. Draft and Cancelled invoices are left alone.</summary>

        private static void RecalculateInvoiceStatus(SqliteConnection conn, SqliteTransaction? tx, int invoiceId)

        {

            if (invoiceId <= 0) return;

            decimal netAmount = 0;

            DateTime dueDate = DateTime.Today;

            string curStatus = "";

            using (var invCmd = conn.CreateCommand())

            {

                invCmd.Transaction = tx;

                invCmd.CommandText = "SELECT NetAmount, DueDate, Status FROM Invoices WHERE Id = @invId";

                invCmd.Parameters.AddWithValue("@invId", invoiceId);

                using var r = invCmd.ExecuteReader();

                if (!r.Read()) return;

                netAmount = Convert.ToDecimal(r.GetDouble(0));

                if (DateTime.TryParse(r.GetString(1), out var dd)) dueDate = dd;

                curStatus = r.IsDBNull(2) ? "" : r.GetString(2);

            }

            if (curStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || curStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase)) return;

            decimal totalCollected;

            using (var sumCmd = conn.CreateCommand())

            {

                sumCmd.Transaction = tx;

                sumCmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @invId AND COALESCE(IsVoid,0) = 0";

                sumCmd.Parameters.AddWithValue("@invId", invoiceId);

                totalCollected = Convert.ToDecimal(sumCmd.ExecuteScalar() ?? 0);

            }

            string newStatus;

            if (netAmount > 0 && totalCollected >= netAmount) newStatus = "Paid";

            else if (dueDate.Date < DateTime.Today) newStatus = "Overdue";

            else if (totalCollected > 0) newStatus = "Partial";

            else newStatus = "Unpaid";

            using var updCmd = conn.CreateCommand();

            updCmd.Transaction = tx;

            updCmd.CommandText = "UPDATE Invoices SET Status = @s WHERE Id = @invId";

            updCmd.Parameters.AddWithValue("@s", newStatus);

            updCmd.Parameters.AddWithValue("@invId", invoiceId);

            updCmd.ExecuteNonQuery();

        }

        private const string Pbkdf2Prefix = "PBKDF2$";

        private const int Pbkdf2Iterations = 100000;

        private static string Pbkdf2Hash(string value)

        {

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(value), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);

            return $"{Pbkdf2Prefix}{Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        }

        private static bool Pbkdf2Verify(string value, string stored)

        {

            var parts = stored.Split('$');

            if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;

            try

            {

                byte[] salt = Convert.FromBase64String(parts[2]);

                byte[] expected = Convert.FromBase64String(parts[3]);

                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(value), salt, iterations, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);

            }

            catch (FormatException)

            {

                return false;

            }

        }

        private static string LegacySha256Hex(string value)

        {

            using var sha = SHA256.Create();

            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));

            var sb = new StringBuilder();

            foreach (byte b in bytes) sb.Append(b.ToString("x2"));

            return sb.ToString();

        }

        private static bool IsLegacySha256Hex(string stored) =>

            stored.Length == 64 && stored.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

        /// <summary>Returns a new salted PBKDF2 hash for storage. Never use it to compare passwords; use VerifyPassword.</summary>

        public static string HashPassword(string rawPassword)

        {

            if (string.IsNullOrEmpty(rawPassword)) return "";

            return Pbkdf2Hash(rawPassword);

        }

        /// <summary>

        /// Checks a typed password against the stored value. Supports the new PBKDF2 format plus the old unsalted SHA-256

        /// and plaintext values (needsUpgrade = true so the caller can re-store it as PBKDF2).

        /// </summary>

        public static bool VerifyPassword(string input, string? stored, out bool needsUpgrade)

        {

            needsUpgrade = false;

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(stored)) return false;

            if (stored.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))

            {

                return Pbkdf2Verify(input, stored);

            }

            needsUpgrade = true;

            if (IsLegacySha256Hex(stored))

            {

                return string.Equals(LegacySha256Hex(input), stored, StringComparison.OrdinalIgnoreCase);

            }

            return string.Equals(input, stored, StringComparison.Ordinal);

        }

        private static string NormalizeSecurityAnswer(string? answer) => (answer ?? "").Trim().ToLowerInvariant();

        public static string HashSecurityAnswer(string? answer)

        {

            string norm = NormalizeSecurityAnswer(answer);

            return string.IsNullOrEmpty(norm) ? "" : Pbkdf2Hash(norm);

        }

        public static bool VerifySecurityAnswer(string? input, string? stored)

        {

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(stored)) return false;

            string norm = NormalizeSecurityAnswer(input);

            if (stored.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal)) return Pbkdf2Verify(norm, stored);

            return string.Equals(stored.Trim(), input.Trim(), StringComparison.OrdinalIgnoreCase);

        }

        public static string? ValidateRecordDeletion(string tableName, int id)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    if (tableName.Equals("LoadingSchedules", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = "SELECT DispatchNumber FROM Deliveries WHERE LoadingScheduleId = @id AND Status <> 'Cancelled'";

                        cmd.Parameters.AddWithValue("@id", id);

                        using (var r = cmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE LOADING SCHEDULE:\nThis schedule is assigned to active Dispatch Order #{r.GetString(0)}.\nPlease cancel or remove the dispatch order first.";

                            }

                        }

                    }

                    else if (tableName.Equals("Trucks", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = $"SELECT DispatchNumber FROM Deliveries WHERE TruckId = @id AND Status IN {ActiveDeliveryStatusesSql}";

                        cmd.Parameters.AddWithValue("@id", id);

                        using (var r = cmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE VEHICLE:\nThis vehicle is currently assigned to an active delivery ({r.GetString(0)}).";

                            }

                        }

                    }

                    else if (tableName.Equals("Crews", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = $"SELECT DispatchNumber, AideIds FROM Deliveries WHERE Status IN {ActiveDeliveryStatusesSql}";

                        using (var r = cmd.ExecuteReader())

                        {

                            while (r.Read())

                            {

                                string dsp = r.GetString(0);

                                string aideIds = r.IsDBNull(1) ? "" : r.GetString(1);

                                var tokens = aideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

                                if (tokens.Contains(id.ToString()))

                                {

                                    return $"CANNOT DELETE CREW PERSONNEL:\nThis crew member is currently assigned as an active aide on delivery ({dsp}).";

                                }

                            }

                        }

                        var cmdDriver = conn.CreateCommand();

                        cmdDriver.CommandText = $"SELECT DispatchNumber FROM Deliveries WHERE (DriverId = @id OR AideId = @id) AND Status IN {ActiveDeliveryStatusesSql}";

                        cmdDriver.Parameters.AddWithValue("@id", id);

                        using (var r2 = cmdDriver.ExecuteReader())

                        {

                            if (r2.Read())

                            {

                                return $"CANNOT DELETE CREW PERSONNEL:\nThis crew member is currently assigned to an active delivery ({r2.GetString(0)}).";

                            }

                        }

                    }

                    else if (tableName.Equals("Deliveries", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = "SELECT InvoiceNumber FROM Invoices WHERE DeliveryId = @id AND Status <> 'Cancelled'";

                        cmd.Parameters.AddWithValue("@id", id);

                        using (var r = cmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE DISPATCH ORDER:\nThis delivery has an official invoice billed (#{r.GetString(0)}). Please void or delete the invoice first.";

                            }

                        }

                        var trCmd = conn.CreateCommand();

                        trCmd.CommandText = "SELECT TripReportNumber FROM TripReports WHERE DeliveryId = @id AND COALESCE(IsVoid,0) = 0";

                        trCmd.Parameters.AddWithValue("@id", id);

                        using (var r = trCmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE DISPATCH ORDER:\nThis delivery already has a completed Trip Report (#{r.GetString(0)}). Please delete the trip report first.";

                            }

                        }

                        var irCmd = conn.CreateCommand();

                        irCmd.CommandText = "SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @id AND COALESCE(Status,'') <> 'Voided'";

                        irCmd.Parameters.AddWithValue("@id", id);

                        if (Convert.ToInt64(irCmd.ExecuteScalar() ?? 0) > 0)

                        {

                            return "CANNOT DELETE DISPATCH ORDER:\nThis delivery has incident reports logged against it. Please delete those incident reports first.";

                        }

                    }

                    else if (tableName.Equals("Invoices", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = "SELECT ReceiptNumber FROM CollectionReceipts WHERE InvoiceId = @id AND COALESCE(IsVoid,0) = 0";

                        cmd.Parameters.AddWithValue("@id", id);

                        using (var r = cmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE INVOICE:\nThis invoice has payment receipts recorded (#{r.GetString(0)}). Please delete or void the collection receipts first.";

                            }

                        }

                    }

                    else if (tableName.Equals("TripReports", StringComparison.OrdinalIgnoreCase))

                    {

                        var cmd = conn.CreateCommand();

                        cmd.CommandText = "SELECT i.InvoiceNumber FROM Invoices i JOIN TripReports tr ON tr.DeliveryId = i.DeliveryId WHERE tr.Id = @id AND i.Status <> 'Cancelled'";

                        cmd.Parameters.AddWithValue("@id", id);

                        using (var r = cmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"CANNOT DELETE TRIP REPORT:\nThis completed trip is already billed on Invoice #{r.GetString(0)}. Please void or delete the invoice first.";

                            }

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                return $"VALIDATION ERROR:\nUnable to verify record dependencies due to database error: {ex.Message}.\nDeletion blocked for safety.";

            }

            return null;

        }

        public static bool ToggleLoadingSchedulePeak(int id, bool isPeak)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE LoadingSchedules SET IsPeak = @isPeak WHERE Id = @id";

                cmd.Parameters.AddWithValue("@isPeak", isPeak ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool ToggleCrewAvailable(int id, bool isAvailable)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Crews SET IsAvailable = @isAvailable WHERE Id = @id";

                cmd.Parameters.AddWithValue("@isAvailable", isAvailable ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool ToggleCrewActive(int id, bool isActive)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Crews SET IsActive = @isActive WHERE Id = @id";

                cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool ToggleUserActive(int id, bool isActive)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Users SET IsActive = @isActive WHERE Id = @id";

                cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool ToggleRateMatrixPeak(int id, bool isPeak)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE RateMatrix SET IsPeakSeason = @isPeak WHERE Id = @id";

                cmd.Parameters.AddWithValue("@isPeak", isPeak ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool ToggleIncidentChargeable(int id, bool chargeable)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE IncidentReports SET ChargeableToCrew = @chargeable WHERE Id = @id";

                cmd.Parameters.AddWithValue("@chargeable", chargeable ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

            catch

            {

                return false;

            }

        }

        public static bool DeleteRecord(string tableName, int id)

        {

            var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "Users", "Crews", "Trucks", "LoadingSchedules", "Deliveries", "TripReports", "Invoices", "CollectionReceipts", "IncidentReports", "RateMatrix", "LookupCategories", "StatementsOfAccount"

            };

            if (!allowedTables.Contains(tableName)) return false;

            LastDeleteSummary = "";

            // History is never hard-deleted: cancel / void / deactivate instead (see MJdatabase.Integrity.cs).

            var soft = SoftDelete(tableName, id);

            if (soft.HasValue) return soft.Value;

            LastDeleteSummary = "Deleted.";

            try

            {

                if (tableName.Equals("CollectionReceipts", StringComparison.OrdinalIgnoreCase))

                {

                    DeleteCollectionReceipt(id);

                    return true;

                }

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    using (var tx = conn.BeginTransaction())

                    {

                        try

                        {

                            if (tableName.Equals("Deliveries", StringComparison.OrdinalIgnoreCase))

                            {

                                var getCmd = conn.CreateCommand();

                                getCmd.Transaction = tx;

                                getCmd.CommandText = "SELECT TruckId, DriverId, AideId, AideIds, LoadingScheduleId, Status FROM Deliveries WHERE Id = @id";

                                getCmd.Parameters.AddWithValue("@id", id);

                                using (var r = getCmd.ExecuteReader())

                                {

                                    if (r.Read())

                                    {

                                        int tid = r.GetInt32(0);

                                        int drid = r.GetInt32(1);

                                        int aid = r.IsDBNull(2) ? 0 : r.GetInt32(2);

                                        string aideIds = r.IsDBNull(3) ? "" : r.GetString(3);

                                        int lsid = r.GetInt32(4);

                                        string status = r.GetString(5);

                                        var aideList = new HashSet<int>();

                                        if (aid > 0) aideList.Add(aid);

                                        foreach (var s in aideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

                                        {

                                            if (int.TryParse(s.Trim(), out int p) && p > 0) aideList.Add(p);

                                        }

                                        var lsCmd = conn.CreateCommand();

                                        lsCmd.Transaction = tx;

                                        lsCmd.CommandText = "UPDATE LoadingSchedules SET Status = 'Pending' WHERE Id = @lsid AND Status <> 'Completed'";

                                        lsCmd.Parameters.AddWithValue("@lsid", lsid);

                                        lsCmd.ExecuteNonQuery();

                                        if (!status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                                        {

                                            var trCmd = conn.CreateCommand();

                                            trCmd.Transaction = tx;

                                            trCmd.CommandText = $"UPDATE Trucks SET Status = 'Active' WHERE Id = @tid AND Status NOT IN {BlockedTruckStatusesSql}";

                                            trCmd.Parameters.AddWithValue("@tid", tid);

                                            trCmd.ExecuteNonQuery();

                                            var drCmd = conn.CreateCommand();

                                            drCmd.Transaction = tx;

                                            drCmd.CommandText = "UPDATE Crews SET IsAvailable = 1 WHERE Id = @drid";

                                            drCmd.Parameters.AddWithValue("@drid", drid);

                                            drCmd.ExecuteNonQuery();

                                            foreach (int a in aideList)

                                            {

                                                var aCmd = conn.CreateCommand();

                                                aCmd.Transaction = tx;

                                                aCmd.CommandText = "UPDATE Crews SET IsAvailable = 1 WHERE Id = @aid";

                                                aCmd.Parameters.AddWithValue("@aid", a);

                                                aCmd.ExecuteNonQuery();

                                            }

                                        }

                                    }

                                }

                            }

                            else if (tableName.Equals("TripReports", StringComparison.OrdinalIgnoreCase))

                            {

                                var getCmd = conn.CreateCommand();

                                getCmd.Transaction = tx;

                                getCmd.CommandText = "SELECT DeliveryId FROM TripReports WHERE Id = @id";

                                getCmd.Parameters.AddWithValue("@id", id);

                                var didObj = getCmd.ExecuteScalar();

                                if (didObj != null && didObj != DBNull.Value)

                                {

                                    int did = Convert.ToInt32(didObj);

                                    var delCmd = conn.CreateCommand();

                                    delCmd.Transaction = tx;

                                    delCmd.CommandText = "UPDATE Deliveries SET Status = 'In Transit' WHERE Id = @did";

                                    delCmd.Parameters.AddWithValue("@did", did);

                                    delCmd.ExecuteNonQuery();

                                    var lsCmd = conn.CreateCommand();

                                    lsCmd.Transaction = tx;

                                    lsCmd.CommandText = "UPDATE LoadingSchedules SET Status = 'Assigned' WHERE Id = (SELECT LoadingScheduleId FROM Deliveries WHERE Id = @did)";

                                    lsCmd.Parameters.AddWithValue("@did", did);

                                    lsCmd.ExecuteNonQuery();

                                }

                            }

                            string recordIdent = $"Record #{id}";

                            try

                            {

                                var identCmd = conn.CreateCommand();

                                identCmd.Transaction = tx;

                                if (tableName.Equals("Deliveries", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT DispatchNumber FROM Deliveries WHERE Id = @id";

                                else if (tableName.Equals("LoadingSchedules", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT ScheduleNumber FROM LoadingSchedules WHERE Id = @id";

                                else if (tableName.Equals("Trucks", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT PlateNumber FROM Trucks WHERE Id = @id";

                                else if (tableName.Equals("Crews", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT FullName FROM Crews WHERE Id = @id";

                                else if (tableName.Equals("Invoices", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT InvoiceNumber FROM Invoices WHERE Id = @id";

                                else if (tableName.Equals("TripReports", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT TripReportNumber FROM TripReports WHERE Id = @id";

                                else if (tableName.Equals("IncidentReports", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT IncidentNumber FROM IncidentReports WHERE Id = @id";

                                else if (tableName.Equals("Users", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT Username FROM Users WHERE Id = @id";

                                else if (tableName.Equals("RateMatrix", StringComparison.OrdinalIgnoreCase)) identCmd.CommandText = "SELECT Destination FROM RateMatrix WHERE Id = @id";

                                else identCmd.CommandText = "SELECT Id FROM " + tableName + " WHERE Id = @id";

                                identCmd.Parameters.AddWithValue("@id", id);

                                var obj = identCmd.ExecuteScalar();

                                if (obj != null && obj != DBNull.Value) recordIdent = obj.ToString() ?? recordIdent;

                            }

                            catch { }

                            var cmd = conn.CreateCommand();

                            cmd.Transaction = tx;

                            cmd.CommandText = $"DELETE FROM {tableName} WHERE Id = @id";

                            cmd.Parameters.AddWithValue("@id", id);

                            cmd.ExecuteNonQuery();

                            if (tableName.Equals("Deliveries", StringComparison.OrdinalIgnoreCase) || tableName.Equals("TripReports", StringComparison.OrdinalIgnoreCase))

                            {

                                ReconcileResourceStatuses(conn, tx);

                            }

                            tx.Commit();

                            LogAuditTrail("DELETE", tableName, id, recordIdent, "", "", "", $"Permanently deleted record #{id} ({recordIdent}) from {tableName}");

                            return true;

                        }

                        catch

                        {

                            tx.Rollback();

                            throw;

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] DeleteRecord({tableName}, {id}): {ex.Message}");

                return false;

            }

        }

        public static void InitializeDatabase()

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var pragmaCmd = conn.CreateCommand();

                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON;";

                pragmaCmd.ExecuteNonQuery();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    CREATE TABLE IF NOT EXISTS Users (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        Username TEXT UNIQUE NOT NULL,

                        Password TEXT NOT NULL,

                        Role TEXT NOT NULL,

                        FullName TEXT NOT NULL,

                        ContactNumber TEXT,

                        IsActive INTEGER DEFAULT 1,

                        CreatedAt TEXT NOT NULL

                    );

                    CREATE TABLE IF NOT EXISTS Crews (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        FullName TEXT NOT NULL,

                        Role TEXT NOT NULL,

                        ContactNumber TEXT,

                        LicenseNumber TEXT,

                        DailyWage REAL DEFAULT 0,

                        FoodAllowance REAL DEFAULT 0,

                        IsAvailable INTEGER DEFAULT 1,

                        IsActive INTEGER DEFAULT 1,

                        IsSeasonal INTEGER DEFAULT 0,

                        PhotoPath TEXT

                    );

                    CREATE TABLE IF NOT EXISTS Trucks (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        PlateNumber TEXT UNIQUE NOT NULL,

                        TruckType TEXT NOT NULL,

                        CapacityCrates INTEGER DEFAULT 300,

                        Status TEXT DEFAULT 'Available',

                        Remarks TEXT

                    );

                    CREATE TABLE IF NOT EXISTS RateMatrix (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        RouteName TEXT NOT NULL,

                        Destination TEXT NOT NULL,

                        TruckType TEXT NOT NULL,

                        IsPeakSeason INTEGER DEFAULT 0,

                        RateAmount REAL NOT NULL

                    );

                    CREATE TABLE IF NOT EXISTS LoadingSchedules (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        ScheduleNumber TEXT UNIQUE NOT NULL,

                        ClientName TEXT NOT NULL,

                        Destination TEXT NOT NULL,

                        ScheduleDate TEXT NOT NULL,

                        DepartureTime TEXT DEFAULT '08:00 AM',

                        GCrate INTEGER DEFAULT 0,

                        ECrate INTEGER DEFAULT 0,

                        TotalCrates INTEGER NOT NULL,

                        IsPeak INTEGER DEFAULT 0,

                        Status TEXT DEFAULT 'Pending',

                        Remarks TEXT,

                        CancellationReason TEXT DEFAULT '',

                        RescheduleCount INTEGER DEFAULT 0,

                        OriginalScheduleDate TEXT,

                        RescheduleReason TEXT DEFAULT ''

                    );

                    CREATE TABLE IF NOT EXISTS Deliveries (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        DispatchNumber TEXT UNIQUE NOT NULL,

                        LoadingScheduleId INTEGER DEFAULT 0,

                        LoadingScheduleInfo TEXT,

                        ClientName TEXT DEFAULT '',

                        Destination TEXT DEFAULT '',

                        IsPeak INTEGER DEFAULT 0,

                        TruckId INTEGER NOT NULL,

                        TruckInfo TEXT,

                        DriverId INTEGER NOT NULL,

                        DriverName TEXT,

                        AideId INTEGER NOT NULL,

                        AideName TEXT,

                        AideIds TEXT DEFAULT '',

                        AideNames TEXT DEFAULT '',

                        GCrate INTEGER DEFAULT 0,

                        ECrate INTEGER DEFAULT 0,

                        TotalCrates INTEGER DEFAULT 0,

                        DispatchDate TEXT NOT NULL,

                        DepartureTime TEXT,

                        Status TEXT DEFAULT 'Pending',

                        Remarks TEXT,

                        CancellationReason TEXT DEFAULT '',

                        CargoType TEXT DEFAULT 'Cold / Chilled',

                        IsMultiDrop INTEGER DEFAULT 0,

                        TotalStopsCount INTEGER DEFAULT 1,

                        FOREIGN KEY (TruckId) REFERENCES Trucks(Id),

                        FOREIGN KEY (DriverId) REFERENCES Crews(Id)

                    );

                    CREATE TABLE IF NOT EXISTS TripReports (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        TripReportNumber TEXT UNIQUE NOT NULL,

                        DeliveryId INTEGER NOT NULL,

                        DispatchNumber TEXT,

                        ArrivalTime TEXT,

                        DepartureTime TEXT,

                        DeliveredGCrates INTEGER DEFAULT 0,

                        DeliveredECrates INTEGER DEFAULT 0,

                        DeliveredCrates INTEGER NOT NULL,

                        ReturnedGCrates INTEGER DEFAULT 0,

                        ReturnedECrates INTEGER DEFAULT 0,

                        ReturnedCrates INTEGER NOT NULL,

                        StoreManagerName TEXT,

                        Remarks TEXT,

                        ReportDate TEXT NOT NULL,

                        FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id)

                    );

                    CREATE TABLE IF NOT EXISTS IncidentReports (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        DeliveryId INTEGER NOT NULL,

                        DispatchNumber TEXT,

                        IncidentDate TEXT NOT NULL,

                        DamagedQuantity INTEGER NOT NULL,

                        EstimatedCost REAL NOT NULL,

                        Cause TEXT NOT NULL,

                        ChargeableToCrew INTEGER DEFAULT 0,

                        Description TEXT,

                        Status TEXT DEFAULT 'Pending',

                        PhotoPath TEXT,

                        FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id)

                    );

                    CREATE TABLE IF NOT EXISTS DispatchStops (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        DeliveryId INTEGER NOT NULL,

                        StopSequence INTEGER NOT NULL,

                        LoadingScheduleId INTEGER,

                        ClientName TEXT NOT NULL,

                        Destination TEXT NOT NULL,

                        GCrate INTEGER DEFAULT 0,

                        ECrate INTEGER DEFAULT 0,

                        TotalCrates INTEGER DEFAULT 0,

                        Status TEXT DEFAULT 'Pending',

                        ArrivalTime TEXT,

                        DepartureTime TEXT,

                        DeliveredGCrates INTEGER DEFAULT 0,

                        DeliveredECrates INTEGER DEFAULT 0,

                        DeliveredCrates INTEGER DEFAULT 0,

                        ReturnedGCrates INTEGER DEFAULT 0,

                        ReturnedECrates INTEGER DEFAULT 0,

                        ReturnedCrates INTEGER DEFAULT 0,

                        StoreManagerLastName TEXT DEFAULT '',

                        StoreManagerFirstName TEXT DEFAULT '',

                        StoreManagerMiddleName TEXT DEFAULT '',

                        StoreManagerName TEXT DEFAULT '',

                        Remarks TEXT DEFAULT '',

                        StoreCode TEXT DEFAULT '',

                        DrNumbers TEXT DEFAULT '',

                        OthersBox TEXT DEFAULT '',

                        OthersPfoods TEXT DEFAULT '',

                        OthersMi TEXT DEFAULT '',

                        FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE

                    );

                    CREATE TABLE IF NOT EXISTS LoadingScheduleStops (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        LoadingScheduleId INTEGER NOT NULL,

                        StopSequence INTEGER NOT NULL,

                        StoreCode TEXT DEFAULT '',

                        StoreName TEXT NOT NULL,

                        ECrate INTEGER DEFAULT 0,

                        GCrate INTEGER DEFAULT 0,

                        DrNumbers TEXT DEFAULT '',

                        FOREIGN KEY (LoadingScheduleId) REFERENCES LoadingSchedules(Id) ON DELETE CASCADE

                    );

                    CREATE TABLE IF NOT EXISTS StatementsOfAccount (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        SoaNumber TEXT UNIQUE NOT NULL,

                        ClientName TEXT NOT NULL,

                        PeriodCovered TEXT NOT NULL,

                        IssueDate TEXT NOT NULL,

                        TotalAmount REAL NOT NULL,

                        Status TEXT DEFAULT 'Issued'

                    );

                    CREATE TABLE IF NOT EXISTS Invoices (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        InvoiceNumber TEXT UNIQUE NOT NULL,

                        SoaId INTEGER,

                        DeliveryId INTEGER,

                        ClientName TEXT NOT NULL,

                        IssueDate TEXT NOT NULL,

                        DueDate TEXT NOT NULL,

                        TotalAmount REAL NOT NULL,

                        Deductions REAL DEFAULT 0,

                        NetAmount REAL NOT NULL,

                        Status TEXT DEFAULT 'Unpaid',

                        FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id)

                    );

                    CREATE TABLE IF NOT EXISTS CollectionReceipts (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        ReceiptNumber TEXT UNIQUE NOT NULL,

                        InvoiceId INTEGER NOT NULL,

                        InvoiceNumber TEXT,

                        PaymentDate TEXT NOT NULL,

                        AmountPaid REAL NOT NULL,

                        PaymentMethod TEXT DEFAULT 'Cash',

                        ReferenceNumber TEXT,

                        Notes TEXT,

                        FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)

                    );

                    CREATE TABLE IF NOT EXISTS Clients (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        CompanyName TEXT UNIQUE NOT NULL,

                        ContactPerson TEXT DEFAULT '',

                        ContactNumber TEXT DEFAULT '',

                        Address TEXT DEFAULT '',

                        Email TEXT DEFAULT '',

                        TIN TEXT DEFAULT ''

                    );

                    CREATE TABLE IF NOT EXISTS ClientStores (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        ClientId INTEGER NOT NULL DEFAULT 0,

                        ClientCompanyName TEXT NOT NULL DEFAULT '',

                        StoreCode TEXT NOT NULL DEFAULT '',

                        StoreName TEXT NOT NULL DEFAULT '',

                        DestinationRoute TEXT NOT NULL DEFAULT '',

                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP

                    );

                    CREATE TABLE IF NOT EXISTS DeliveryRoutes (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        ClientCompanyName TEXT NOT NULL DEFAULT '',

                        RouteCode TEXT UNIQUE NOT NULL,

                        RouteName TEXT NOT NULL,

                        Description TEXT DEFAULT '',

                        ConnectedPlaces TEXT NOT NULL,

                        IsActive INTEGER DEFAULT 1,

                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,

                        UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP

                    );

                    CREATE TABLE IF NOT EXISTS LookupCategories (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        CategoryType TEXT NOT NULL,

                        ItemValue TEXT NOT NULL,

                        UNIQUE(CategoryType, ItemValue)

                    );

                    CREATE TABLE IF NOT EXISTS LoginAudit (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,

                        Username TEXT NOT NULL,

                        EventType TEXT NOT NULL,

                        IpOrMachine TEXT NOT NULL,

                        Details TEXT

                    );

                    CREATE TABLE IF NOT EXISTS SystemSettings (

                        SettingKey TEXT PRIMARY KEY,

                        SettingValue TEXT NOT NULL,

                        Description TEXT,

                        UpdatedAt TEXT NOT NULL

                    );

                    CREATE TABLE IF NOT EXISTS FormDrafts (

                        ModuleKey TEXT NOT NULL,

                        UserKey TEXT NOT NULL,

                        DraftData TEXT NOT NULL,

                        LastSaved TEXT NOT NULL,

                        PRIMARY KEY (ModuleKey, UserKey)

                    );

                    CREATE TABLE IF NOT EXISTS AuditTrail (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,

                        Username TEXT NOT NULL,

                        UserRole TEXT NOT NULL,

                        ActionType TEXT NOT NULL,

                        TableName TEXT NOT NULL,

                        RecordId INTEGER NOT NULL,

                        RecordIdentifier TEXT NOT NULL,

                        FieldName TEXT,

                        OldValue TEXT,

                        NewValue TEXT,

                        ReasonOrNotes TEXT,

                        IpOrMachine TEXT

                    );

                    CREATE TABLE IF NOT EXISTS SystemNotifications (

                        Id TEXT PRIMARY KEY,

                        Title TEXT NOT NULL,

                        Message TEXT NOT NULL,

                        Timestamp TEXT NOT NULL,

                        Type TEXT NOT NULL,

                        Category TEXT NOT NULL,

                        IsRead INTEGER NOT NULL DEFAULT 0,

                        TargetModule TEXT,

                        TargetRecordId TEXT

                    );

                    CREATE INDEX IF NOT EXISTS idx_sys_notifs_time ON SystemNotifications(Timestamp DESC);

                    CREATE INDEX IF NOT EXISTS idx_sys_notifs_read ON SystemNotifications(IsRead);

                    CREATE INDEX IF NOT EXISTS idx_sys_notifs_cat ON SystemNotifications(Category);

                    CREATE TABLE IF NOT EXISTS SpecialTrips (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OrderReference TEXT NOT NULL UNIQUE,
                        DispatchDate TEXT NOT NULL,
                        Company TEXT,
                        DepartureTime TEXT,
                        Address TEXT,
                        ServiceType TEXT,
                        Status TEXT DEFAULT 'Pending',
                        ModeOfPaymentWithReport INTEGER DEFAULT 1,
                        WithSOA INTEGER DEFAULT 0,
                        WithServiceInvoice INTEGER DEFAULT 0,
                        WithCollection INTEGER DEFAULT 0,
                        TruckId INTEGER DEFAULT 0,
                        TruckPlate TEXT,
                        DriverId INTEGER DEFAULT 0,
                        DriverName TEXT,
                        AideId INTEGER DEFAULT 0,
                        AideName TEXT,
                        CreatedAt TEXT,
                        CompletedAt TEXT,
                        Remarks TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_special_trips_order ON SpecialTrips(OrderReference);
                    CREATE INDEX IF NOT EXISTS idx_special_trips_date ON SpecialTrips(DispatchDate);
                    CREATE INDEX IF NOT EXISTS idx_special_trips_status ON SpecialTrips(Status);

                    CREATE TABLE IF NOT EXISTS SpecialTripStops (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SpecialTripId INTEGER NOT NULL,
                        SequenceNo INTEGER DEFAULT 1,
                        StandardId TEXT,
                        DeliveryDetails TEXT,
                        DriverRoute TEXT,
                        DepartureTime TEXT,
                        ExtraColumnsJson TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_special_trip_stops_trip ON SpecialTripStops(SpecialTripId);

                ";

                cmd.ExecuteNonQuery();

                string[] migrations = new string[]

                {

                    "ALTER TABLE Crews ADD COLUMN PhotoPath TEXT;",

                    "ALTER TABLE Crews ADD COLUMN IsSeasonal INTEGER DEFAULT 0;",

                    "ALTER TABLE IncidentReports ADD COLUMN PhotoPath TEXT;",

                    "ALTER TABLE LoadingSchedules ADD COLUMN DepartureTime TEXT DEFAULT '08:00 AM';",

                    "ALTER TABLE LoadingSchedules ADD COLUMN GCrate INTEGER DEFAULT 0;",

                    "ALTER TABLE LoadingSchedules ADD COLUMN ECrate INTEGER DEFAULT 0;",

                    "ALTER TABLE LoadingSchedules ADD COLUMN CancellationReason TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN GCrate INTEGER DEFAULT 0;",

                    "ALTER TABLE Deliveries ADD COLUMN ECrate INTEGER DEFAULT 0;",

                    "ALTER TABLE Deliveries ADD COLUMN TotalCrates INTEGER DEFAULT 0;",

                    "ALTER TABLE Deliveries ADD COLUMN AideIds TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN AideNames TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN CancellationReason TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN DeliveredGCrates INTEGER DEFAULT 0;",

                    "ALTER TABLE TripReports ADD COLUMN DeliveredECrates INTEGER DEFAULT 0;",

                    "ALTER TABLE TripReports ADD COLUMN ReturnedGCrates INTEGER DEFAULT 0;",

                    "ALTER TABLE TripReports ADD COLUMN ReturnedECrates INTEGER DEFAULT 0;",

                    "ALTER TABLE Users ADD COLUMN SecurityQuestion TEXT DEFAULT '';",

                    "ALTER TABLE Users ADD COLUMN SecurityAnswer TEXT DEFAULT '';",

                    "ALTER TABLE Crews ADD COLUMN LastName TEXT DEFAULT '';",

                    "ALTER TABLE Crews ADD COLUMN FirstName TEXT DEFAULT '';",

                    "ALTER TABLE Crews ADD COLUMN MiddleName TEXT DEFAULT '';",

                    "ALTER TABLE Users ADD COLUMN LastName TEXT DEFAULT '';",

                    "ALTER TABLE Users ADD COLUMN FirstName TEXT DEFAULT '';",

                    "ALTER TABLE Users ADD COLUMN MiddleName TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN LastName TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN FirstName TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN MiddleName TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN TIN TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN ExtraFieldsJson TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN IsActive INTEGER DEFAULT 1;",

                    "ALTER TABLE Crews ADD COLUMN LicenseClass TEXT DEFAULT 'None';",

                    "ALTER TABLE Crews ADD COLUMN LicenseIssued TEXT DEFAULT '';",

                    "ALTER TABLE Crews ADD COLUMN LicenseExpiry TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN Make TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN Model TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN YearModel TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN ChassisNumber TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN EngineNumber TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN OdometerKm INTEGER DEFAULT 0;",

                    "ALTER TABLE Trucks ADD COLUMN RegistrationExpiry TEXT DEFAULT '';",

                    "ALTER TABLE Trucks ADD COLUMN InsuranceExpiry TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN DeactivatedAt TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN CreatedAt TEXT DEFAULT '';",

                    "ALTER TABLE CollectionReceipts ADD COLUMN IsVoid INTEGER DEFAULT 0;",

                    "ALTER TABLE CollectionReceipts ADD COLUMN VoidedAt TEXT DEFAULT '';",

                    "ALTER TABLE CollectionReceipts ADD COLUMN VoidedBy TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN IsVoid INTEGER DEFAULT 0;",

                    "ALTER TABLE TripReports ADD COLUMN VoidedAt TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN VoidedBy TEXT DEFAULT '';",

                    "ALTER TABLE ClientStores ADD COLUMN ExtraFieldsJson TEXT DEFAULT '';",

                    "CREATE TABLE IF NOT EXISTS ClientStores (Id INTEGER PRIMARY KEY AUTOINCREMENT, ClientId INTEGER NOT NULL DEFAULT 0, ClientCompanyName TEXT NOT NULL DEFAULT '', StoreCode TEXT NOT NULL DEFAULT '', StoreName TEXT NOT NULL DEFAULT '', DestinationRoute TEXT NOT NULL DEFAULT '', CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP);",

                    "ALTER TABLE TripReports ADD COLUMN StoreManagerLastName TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN StoreManagerFirstName TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN StoreManagerMiddleName TEXT DEFAULT '';",

                    "ALTER TABLE LoadingSchedules ADD COLUMN RescheduleCount INTEGER DEFAULT 0;",

                    "ALTER TABLE LoadingSchedules ADD COLUMN OriginalScheduleDate TEXT;",

                    "ALTER TABLE LoadingSchedules ADD COLUMN RescheduleReason TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN ClientName TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN Destination TEXT DEFAULT '';",

                    "ALTER TABLE Deliveries ADD COLUMN IsPeak INTEGER DEFAULT 0;",

                    "ALTER TABLE DispatchStops ADD COLUMN StoreCode TEXT DEFAULT '';",

                    "ALTER TABLE DispatchStops ADD COLUMN DrNumbers TEXT DEFAULT '';",

                    "ALTER TABLE DispatchStops ADD COLUMN OthersBox TEXT DEFAULT '';",

                    "ALTER TABLE DispatchStops ADD COLUMN OthersPfoods TEXT DEFAULT '';",

                    "ALTER TABLE DispatchStops ADD COLUMN OthersMi TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN TruckTimeIn TEXT DEFAULT '2042';",

                    "ALTER TABLE TripReports ADD COLUMN TruckTimeOut TEXT DEFAULT '0139';",

                    "ALTER TABLE TripReports ADD COLUMN DtrApprovedBy TEXT DEFAULT 'RAMIL MENDOZA';",

                    "ALTER TABLE TripReports ADD COLUMN ReturnCheckedBy TEXT DEFAULT '';",

                    "ALTER TABLE TripReports ADD COLUMN DtrPreparedBy TEXT DEFAULT 'XERXES RYAN MANRIQUE';",

                    "ALTER TABLE TripReports ADD COLUMN DrPreparedBy TEXT DEFAULT 'VINJOHN VILLAMOR';",

                    "ALTER TABLE TripReports ADD COLUMN DrCheckedBy TEXT DEFAULT 'JOE-EM FLORES';",

                    "ALTER TABLE TripReports ADD COLUMN GuardName TEXT DEFAULT '';",

                    "CREATE TABLE IF NOT EXISTS TripCrewPay (Id INTEGER PRIMARY KEY AUTOINCREMENT, DeliveryId INTEGER NOT NULL, CrewId INTEGER NOT NULL, CrewName TEXT DEFAULT '', Role TEXT DEFAULT 'Driver', BasePay REAL DEFAULT 0, Allowance REAL DEFAULT 0, Extra REAL DEFAULT 0, Deduction REAL DEFAULT 0, Notes TEXT DEFAULT '', UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UNIQUE(DeliveryId, CrewId));",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN ClientAddress TEXT DEFAULT '';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN ClientTin TEXT DEFAULT '';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN DeliveryType TEXT DEFAULT 'REF';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN TotalSalesVatInc REAL DEFAULT 0;",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN VatAmount REAL DEFAULT 0;",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN NetOfVat REAL DEFAULT 0;",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN FuelSurchargeTotal REAL DEFAULT 0;",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN WithholdingTax REAL DEFAULT 0;",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN SalesInvoiceNo TEXT DEFAULT '';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN PreparedBy TEXT DEFAULT 'Marie Ann J. Billones';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN ReceivedBy1 TEXT DEFAULT 'Ma''am Monette Rubonal';",

                    "ALTER TABLE StatementsOfAccount ADD COLUMN ReceivedBy2 TEXT DEFAULT 'Ma''am Francis Pauline Rodillas';",

                    "ALTER TABLE DeliveryRoutes ADD COLUMN ClientCompanyName TEXT DEFAULT '';",

                    "UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';",
                    "UPDATE DeliveryRoutes SET Description = 'RCS Villasis Route EODLAB (Ilocos Norte & Ilocos Sur)' WHERE RouteCode = 'R1';",
                    "UPDATE DeliveryRoutes SET Description = 'RCS Villasis Route EODGAA (La Union)' WHERE RouteCode = 'R2';",

                    @"CREATE TABLE IF NOT EXISTS StatementOfAccountItems (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        SoaId INTEGER NOT NULL,

                        TripNumber INTEGER NOT NULL,

                        DateReceived TEXT,

                        Zone TEXT,

                        DrNumbers TEXT,

                        Destination TEXT,

                        DeliveryType TEXT DEFAULT 'REF',

                        PlateNumber TEXT,

                        Tonnage TEXT,

                        DropCount INTEGER DEFAULT 0,

                        Amount REAL DEFAULT 0,

                        FuelSurcharge REAL DEFAULT 0,

                        DriverHelper TEXT,

                        DriverWage REAL DEFAULT 0,

                        HelperWage REAL DEFAULT 0,

                        DistanceKm REAL DEFAULT 0,

                        DieselCost REAL DEFAULT 0,

                        Excess REAL DEFAULT 0,

                        DeliveryId INTEGER DEFAULT 0

                    );"

                };

                foreach (var mig in migrations)

                {

                    try

                    {

                        var migCmd = conn.CreateCommand();

                        migCmd.CommandText = mig;

                        migCmd.ExecuteNonQuery();

                    }

                    catch { }

                }

                try

                {

                    var dataMigCmd = conn.CreateCommand();

                    dataMigCmd.CommandText = @"

                        UPDATE LoadingSchedules SET GCrate = TotalCrates WHERE (GCrate = 0 AND ECrate = 0 AND TotalCrates > 0);

                        UPDATE Deliveries SET GCrate = (SELECT TotalCrates FROM LoadingSchedules WHERE LoadingSchedules.Id = Deliveries.LoadingScheduleId) WHERE (GCrate = 0 AND ECrate = 0);

                        UPDATE Deliveries SET TotalCrates = GCrate + ECrate WHERE TotalCrates = 0;

                        UPDATE Deliveries SET AideIds = CAST(AideId AS TEXT) WHERE (AideIds IS NULL OR AideIds = '') AND AideId > 0;

                        UPDATE Deliveries SET AideNames = AideName WHERE (AideNames IS NULL OR AideNames = '') AND AideName IS NOT NULL AND AideName <> '';

                        UPDATE Deliveries SET 

                            ClientName = (SELECT ClientName FROM LoadingSchedules WHERE LoadingSchedules.Id = Deliveries.LoadingScheduleId),

                            Destination = (SELECT Destination FROM LoadingSchedules WHERE LoadingSchedules.Id = Deliveries.LoadingScheduleId),

                            CargoType = COALESCE((SELECT CargoType FROM LoadingSchedules WHERE LoadingSchedules.Id = Deliveries.LoadingScheduleId), 'Cold / Chilled'),

                            IsPeak = COALESCE((SELECT IsPeak FROM LoadingSchedules WHERE LoadingSchedules.Id = Deliveries.LoadingScheduleId), 0)

                        WHERE (ClientName IS NULL OR ClientName = '') AND LoadingScheduleId > 0;

                        UPDATE TripReports SET DeliveredGCrates = DeliveredCrates WHERE (DeliveredGCrates = 0 AND DeliveredECrates = 0 AND DeliveredCrates > 0);

                        UPDATE TripReports SET ReturnedGCrates = ReturnedCrates WHERE (ReturnedGCrates = 0 AND ReturnedECrates = 0 AND ReturnedCrates > 0);

                    ";

                    dataMigCmd.ExecuteNonQuery();

                }

                catch { }

                                try

                {

                    var migCmd = conn.CreateCommand();

                    migCmd.CommandText = @"

                        ALTER TABLE Trucks ADD COLUMN CargoType TEXT DEFAULT 'Cold / Chilled';

                    ";

                    migCmd.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migCmd2 = conn.CreateCommand();

                    migCmd2.CommandText = @"

                        ALTER TABLE LoadingSchedules ADD COLUMN CargoType TEXT DEFAULT 'Cold / Chilled';

                    ";

                    migCmd2.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migCmd3 = conn.CreateCommand();

                    migCmd3.CommandText = "ALTER TABLE Deliveries ADD COLUMN CargoType TEXT DEFAULT 'Cold / Chilled';";

                    migCmd3.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migCmd5 = conn.CreateCommand();

                    migCmd5.CommandText = "ALTER TABLE Deliveries ADD COLUMN IsMultiDrop INTEGER DEFAULT 0;";

                    migCmd5.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migCmd6 = conn.CreateCommand();

                    migCmd6.CommandText = "ALTER TABLE Deliveries ADD COLUMN TotalStopsCount INTEGER DEFAULT 1;";

                    migCmd6.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migCmd4 = conn.CreateCommand();

                    migCmd4.CommandText = "ALTER TABLE LookupCategories ADD COLUMN ExtraValue TEXT;";

                    migCmd4.ExecuteNonQuery();

                } catch { }

                try

                {

                    var migDelCmd = conn.CreateCommand();
                    migDelCmd.CommandText = "UPDATE Deliveries SET Status = 'In Transit' WHERE Status = 'Delayed'; UPDATE Deliveries SET Status = 'Scheduled' WHERE Status = 'Pending';";
                    migDelCmd.ExecuteNonQuery();

                } catch { }

                try
                {
                    // One-time client rename (Goldilocks -> Red Ribbon). Guarded by a flag so it never re-runs,

                    // and it no longer rewrites the audit trail or notification history.

                    using var flagCmd = conn.CreateCommand();

                    flagCmd.CommandText = "SELECT COUNT(*) FROM SystemSettings WHERE SettingKey = 'Migration_RedRibbonRename_Done'";

                    bool renameDone = Convert.ToInt64(flagCmd.ExecuteScalar() ?? 0) > 0;

                    if (!renameDone)

                    {
                        var migRedRibbon = conn.CreateCommand();

                        migRedRibbon.CommandText = @"

                            UPDATE Clients SET CompanyName = 'RED RIBBON BAKESHOP INC.' WHERE UPPER(CompanyName) LIKE '%GOLDILOCKS%';

                            UPDATE LookupCategories SET ItemValue = 'Red Ribbon Bakeshop Inc.' WHERE UPPER(ItemValue) LIKE '%GOLDILOCKS%';

                            UPDATE LoadingSchedules SET ClientName = REPLACE(REPLACE(REPLACE(ClientName, 'Goldilocks Bakeshop Inc.', 'Red Ribbon Bakeshop Inc.'), 'GOLDILOCKS CORPORATION', 'RED RIBBON BAKESHOP INC.'), 'Goldilocks', 'Red Ribbon') WHERE UPPER(ClientName) LIKE '%GOLDILOCKS%';

                            UPDATE Deliveries SET LoadingScheduleInfo = REPLACE(REPLACE(REPLACE(LoadingScheduleInfo, 'Goldilocks Bakeshop Inc.', 'Red Ribbon Bakeshop Inc.'), 'GOLDILOCKS CORPORATION', 'RED RIBBON BAKESHOP INC.'), 'Goldilocks', 'Red Ribbon') WHERE UPPER(LoadingScheduleInfo) LIKE '%GOLDILOCKS%';

                            UPDATE Invoices SET ClientName = REPLACE(REPLACE(REPLACE(ClientName, 'Goldilocks Bakeshop Inc.', 'Red Ribbon Bakeshop Inc.'), 'GOLDILOCKS CORPORATION', 'RED RIBBON BAKESHOP INC.'), 'Goldilocks', 'Red Ribbon') WHERE UPPER(ClientName) LIKE '%GOLDILOCKS%';

                            UPDATE StatementsOfAccount SET ClientName = REPLACE(REPLACE(REPLACE(ClientName, 'Goldilocks Bakeshop Inc.', 'Red Ribbon Bakeshop Inc.'), 'GOLDILOCKS CORPORATION', 'RED RIBBON BAKESHOP INC.'), 'Goldilocks', 'Red Ribbon') WHERE UPPER(ClientName) LIKE '%GOLDILOCKS%';

                            UPDATE DispatchStops SET ClientName = REPLACE(REPLACE(REPLACE(ClientName, 'Goldilocks Bakeshop Inc.', 'Red Ribbon Bakeshop Inc.'), 'GOLDILOCKS CORPORATION', 'RED RIBBON BAKESHOP INC.'), 'Goldilocks', 'Red Ribbon') WHERE UPPER(ClientName) LIKE '%GOLDILOCKS%';

                            INSERT OR IGNORE INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt)

                            VALUES ('Migration_RedRibbonRename_Done', 'True', 'One-time client rename migration completed', datetime('now'));

                        ";

                        migRedRibbon.ExecuteNonQuery();

                    }
                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] RedRibbon migration: {ex.Message}"); }

                try

                {

                    // Normalize truck status vocabulary: 'Available' and 'Active' mean the same thing; store 'Active'.

                    using var truckNorm = conn.CreateCommand();

                    truckNorm.CommandText = "UPDATE Trucks SET Status = 'Active' WHERE Status = 'Available';";

                    truckNorm.ExecuteNonQuery();

                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] Truck status normalize: {ex.Message}"); }

                try

                {

                    // Replace placeholder company settings with the official business details (only if still the placeholder).

                    using var compCmd = conn.CreateCommand();

                    compCmd.CommandText = @"

                        UPDATE SystemSettings SET SettingValue = '708-839-875-000', UpdatedAt = datetime('now') WHERE SettingKey = 'CompanyTin' AND SettingValue = '123-456-789-000';

                        UPDATE SystemSettings SET SettingValue = 'Jasmin St., San Francisco, Tarlac City', UpdatedAt = datetime('now') WHERE SettingKey = 'CompanyAddress' AND SettingValue = 'Cavite / Batangas Operations Depot';

                    ";

                    compCmd.ExecuteNonQuery();

                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] Company settings fix: {ex.Message}"); }

                SeedDefaultData(conn);

                EnsureDefaultLookupCategories(conn);

                EnsureDefaultDeliveryRoutes(conn);

                MigrateExistingNames(conn);

                EnsureDataIntegrity(conn);

            }

            try { NormalizeClientRecords(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] NormalizeClientRecords: {ex.Message}"); }

            try { ReconcileAllRouteStores(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] ReconcileAllRouteStores: {ex.Message}"); }

            PerformStartupSanityCheck();

        }

        private static void MigrateExistingNames(SqliteConnection conn)

        {

            try

            {

                var usersToMigrate = new List<(int Id, string FullName)>();

                using (var uCmd = conn.CreateCommand())

                {

                    uCmd.CommandText = "SELECT Id, FullName FROM Users WHERE (LastName IS NULL OR LastName = '') AND FullName IS NOT NULL AND FullName <> ''";

                    using var reader = uCmd.ExecuteReader();

                    while (reader.Read()) usersToMigrate.Add((reader.GetInt32(0), reader.GetString(1)));

                }

                foreach (var (id, fullName) in usersToMigrate)

                {

                    var (last, first, middle) = NameHelper.SplitFullName(fullName);

                    using var upCmd = conn.CreateCommand();

                    upCmd.CommandText = "UPDATE Users SET LastName = @l, FirstName = @f, MiddleName = @m WHERE Id = @id";

                    upCmd.Parameters.AddWithValue("@l", last);

                    upCmd.Parameters.AddWithValue("@f", first);

                    upCmd.Parameters.AddWithValue("@m", middle);

                    upCmd.Parameters.AddWithValue("@id", id);

                    upCmd.ExecuteNonQuery();

                }

                var crewsToMigrate = new List<(int Id, string FullName)>();

                using (var cCmd = conn.CreateCommand())

                {

                    cCmd.CommandText = "SELECT Id, FullName FROM Crews WHERE (LastName IS NULL OR LastName = '') AND FullName IS NOT NULL AND FullName <> ''";

                    using var reader = cCmd.ExecuteReader();

                    while (reader.Read()) crewsToMigrate.Add((reader.GetInt32(0), reader.GetString(1)));

                }

                foreach (var (id, fullName) in crewsToMigrate)

                {

                    var (last, first, middle) = NameHelper.SplitFullName(fullName);

                    using var upCmd = conn.CreateCommand();

                    upCmd.CommandText = "UPDATE Crews SET LastName = @l, FirstName = @f, MiddleName = @m WHERE Id = @id";

                    upCmd.Parameters.AddWithValue("@l", last);

                    upCmd.Parameters.AddWithValue("@f", first);

                    upCmd.Parameters.AddWithValue("@m", middle);

                    upCmd.Parameters.AddWithValue("@id", id);

                    upCmd.ExecuteNonQuery();

                }

                var clientsToMigrate = new List<(int Id, string ContactPerson)>();

                using (var clCmd = conn.CreateCommand())

                {

                    clCmd.CommandText = "SELECT Id, ContactPerson FROM Clients WHERE (LastName IS NULL OR LastName = '') AND ContactPerson IS NOT NULL AND ContactPerson <> ''";

                    using var reader = clCmd.ExecuteReader();

                    while (reader.Read()) clientsToMigrate.Add((reader.GetInt32(0), reader.GetString(1)));

                }

                foreach (var (id, contact) in clientsToMigrate)

                {

                    var (last, first, middle) = NameHelper.SplitFullName(contact);

                    using var upCmd = conn.CreateCommand();

                    upCmd.CommandText = "UPDATE Clients SET LastName = @l, FirstName = @f, MiddleName = @m WHERE Id = @id";

                    upCmd.Parameters.AddWithValue("@l", last);

                    upCmd.Parameters.AddWithValue("@f", first);

                    upCmd.Parameters.AddWithValue("@m", middle);

                    upCmd.Parameters.AddWithValue("@id", id);

                    upCmd.ExecuteNonQuery();

                }

                var tripsToMigrate = new List<(int Id, string StoreManager)>();

                using (var trCmd = conn.CreateCommand())

                {

                    trCmd.CommandText = "SELECT Id, StoreManagerName FROM TripReports WHERE (StoreManagerLastName IS NULL OR StoreManagerLastName = '') AND StoreManagerName IS NOT NULL AND StoreManagerName <> ''";

                    using var reader = trCmd.ExecuteReader();

                    while (reader.Read()) tripsToMigrate.Add((reader.GetInt32(0), reader.GetString(1)));

                }

                foreach (var (id, sm) in tripsToMigrate)

                {

                    var (last, first, middle) = NameHelper.SplitFullName(sm);

                    using var upCmd = conn.CreateCommand();

                    upCmd.CommandText = "UPDATE TripReports SET StoreManagerLastName = @l, StoreManagerFirstName = @f, StoreManagerMiddleName = @m WHERE Id = @id";

                    upCmd.Parameters.AddWithValue("@l", last);

                    upCmd.Parameters.AddWithValue("@f", first);

                    upCmd.Parameters.AddWithValue("@m", middle);

                    upCmd.Parameters.AddWithValue("@id", id);

                    upCmd.ExecuteNonQuery();

                }

                try

                {

                    using var setCmd = conn.CreateCommand();

                    setCmd.CommandText = @"

                        INSERT OR IGNORE INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt)

                        VALUES ('AllowMultiDestDispatch', 'True', 'Allow multiple destinations in 1 dispatch trip', datetime('now'));

                    ";

                    setCmd.ExecuteNonQuery();

                }

                catch { }

            }

            catch { }

        }

        public static string GetSystemSetting(string key, string defaultValue = "")

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT SettingValue FROM SystemSettings WHERE SettingKey = @k";

                cmd.Parameters.AddWithValue("@k", key);

                var val = cmd.ExecuteScalar();

                return val != null && val != DBNull.Value ? val.ToString()! : defaultValue;

            }

            catch

            {

                return defaultValue;

            }

        }

        public static void SetSystemSetting(string key, string value, string description = "")

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    INSERT INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt)

                    VALUES (@k, @v, @d, datetime('now'))

                    ON CONFLICT(SettingKey) DO UPDATE SET

                        SettingValue = @v,

                        Description = CASE WHEN @d <> '' THEN @d ELSE Description END,

                        UpdatedAt = datetime('now');

                ";

                cmd.Parameters.AddWithValue("@k", key);

                cmd.Parameters.AddWithValue("@v", value);

                cmd.Parameters.AddWithValue("@d", description);

                cmd.ExecuteNonQuery();

            }

            catch

            {

            }

        }

        public static void EnsureDefaultLookupCategories(SqliteConnection conn)

        {

            var checkCmd = conn.CreateCommand();

            checkCmd.CommandText = "SELECT COUNT(*) FROM LookupCategories";

            long count = (long)(checkCmd.ExecuteScalar() ?? 0);

            if (count == 0)

            {

                var defaults = new (string Category, string Value, string Extra)[]

                {

                    ("Truck Type / Category", "4-Wheeler Van", "300"),

                    ("Truck Type / Category", "6-Wheeler Truck", "600"),

                    ("Truck Type / Category", "10-Wheeler Heavy Truck", "1000"),

                    ("Truck Type / Category", "Refrigerated Van", "450"),

                    ("Client Company", "San Miguel Foods", ""),

                    ("Client Company", "Monde Nissin", ""),

                    ("Client Company", "Universal Robina Corp", ""),

                    ("Payment Method", "Bank Transfer", ""),

                    ("Payment Method", "Check", ""),

                    ("Payment Method", "Cash", ""),

                    ("Payment Method", "GCash", ""),

                    ("Crew Role", "Driver", ""),

                    ("Crew Role", "Aide/Helper", ""),

                    ("Crew Role", "Mechanic", ""),

                    ("Crew Role", "Fabricator", "")

                };

                foreach (var d in defaults)

                {

                    var insCmd = conn.CreateCommand();

                    insCmd.CommandText = "INSERT OR IGNORE INTO LookupCategories (CategoryType, ItemValue, ExtraValue) VALUES (@t, @v, @e);";

                    insCmd.Parameters.AddWithValue("@t", d.Category);

                    insCmd.Parameters.AddWithValue("@v", d.Value);

                    insCmd.Parameters.AddWithValue("@e", d.Extra);

                    insCmd.ExecuteNonQuery();

                }

            }

            else

            {

                foreach (var role in new[] { "Driver", "Aide/Helper", "Mechanic", "Fabricator" })

                {

                    var roleCmd = conn.CreateCommand();

                    roleCmd.CommandText = "INSERT OR IGNORE INTO LookupCategories (CategoryType, ItemValue, ExtraValue) VALUES ('Crew Role', @r, '');";

                    roleCmd.Parameters.AddWithValue("@r", role);

                    roleCmd.ExecuteNonQuery();

                }

            }

        }

        private static void SeedDefaultData(SqliteConnection conn)

        {

            var checkCmd = conn.CreateCommand();

            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";

            long userCount = (long)(checkCmd.ExecuteScalar() ?? 0);

            if (userCount == 0)

            {

                var insertAdmin = conn.CreateCommand();

                insertAdmin.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt)

                                            VALUES ('admin', @p1, 'Admin', 'Marie Ann J. Billones', '0917-123-4567', 1, @now)";

                insertAdmin.Parameters.AddWithValue("@p1", HashPassword("admin123"));

                insertAdmin.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                insertAdmin.ExecuteNonQuery();

                var insertStaff = conn.CreateCommand();

                insertStaff.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt)

                                            VALUES ('staff', @p2, 'Office Staff', 'Juan Dela Cruz', '0918-765-4321', 1, @now)";

                insertStaff.Parameters.AddWithValue("@p2", HashPassword("staff123"));

                insertStaff.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                insertStaff.ExecuteNonQuery();

                var insertCategories = conn.CreateCommand();

                insertCategories.CommandText = @"

                    INSERT OR IGNORE INTO LookupCategories (CategoryType, ItemValue) VALUES

                    ('Role', 'Admin'),

                    ('Role', 'Office Staff'),

                    ('Crew Role', 'Driver'),

                    ('Crew Role', 'Aide/Helper'),

                    ('Crew Role', 'Mechanic'),

                    ('Crew Role', 'Fabricator');

                ";

                insertCategories.ExecuteNonQuery();

            }

        }

        public static (AuthResult Result, User? User) AuthenticateUserDetailed(string username, string password)

        {

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))

                return (AuthResult.InvalidCredentials, null);

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT * FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u))";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                using (var reader = cmd.ExecuteReader())

                {

                    if (reader.Read())

                    {

                        int userId = reader.GetInt32(0);

                        string dbUser = reader.GetString(1);

                        string dbPass = reader.GetString(2);

                        string dbRole = reader.GetString(3);

                        string dbName = reader.GetString(4);

                        string dbContact = reader.IsDBNull(5) ? "" : reader.GetString(5);

                        bool dbActive = reader.GetInt32(6) == 1;

                        DateTime dbCreatedAt = DateTime.TryParse(reader.GetString(7), out var parsedDt) ? parsedDt : DateTime.Now;

                        bool isMatch = VerifyPassword(password, dbPass, out bool needsUpgrade);

                        string hashedInput = isMatch && needsUpgrade ? HashPassword(password) : dbPass;

                        if (!isMatch)

                        {

                            return (AuthResult.InvalidCredentials, null);

                        }

                        if (!dbActive)

                        {

                            return (AuthResult.Inactive, null);

                        }

                        if (dbPass != hashedInput)

                        {

                            try

                            {

                                using (var upgradeCmd = conn.CreateCommand())

                                {

                                    upgradeCmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";

                                    upgradeCmd.Parameters.AddWithValue("@p", hashedInput);

                                    upgradeCmd.Parameters.AddWithValue("@id", userId);

                                    upgradeCmd.ExecuteNonQuery();

                                }

                            }

                            catch { }

                        }

                        string secQ = "";

                        string secA = "";

                        string ln = "";

                        string fn = "";

                        string mn = "";

                        for (int i = 0; i < reader.FieldCount; i++)

                        {

                            string colName = reader.GetName(i);

                            if (colName.Equals("SecurityQuestion", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                secQ = reader.GetString(i);

                            else if (colName.Equals("SecurityAnswer", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                secA = reader.GetString(i);

                            else if (colName.Equals("LastName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                ln = reader.GetString(i);

                            else if (colName.Equals("FirstName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                fn = reader.GetString(i);

                            else if (colName.Equals("MiddleName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                mn = reader.GetString(i);

                        }

                        if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(dbName))

                        {

                            var s = NameHelper.SplitFullName(dbName);

                            ln = s.LastName;

                            fn = s.FirstName;

                            mn = s.MiddleName;

                        }

                        var user = new User

                        {

                            Id = userId,

                            Username = dbUser,

                            Password = hashedInput,

                            Role = dbRole,

                            FullName = dbName,

                            ContactNumber = dbContact,

                            IsActive = dbActive,

                            CreatedAt = dbCreatedAt,

                            SecurityQuestion = secQ,

                            SecurityAnswer = secA,

                            LastName = ln,

                            FirstName = fn,

                            MiddleName = mn

                        };

                        return (AuthResult.Success, user);

                    }

                }

            }

            return (AuthResult.InvalidCredentials, null);

        }

        public static void LogSecurityEvent(string username, string eventType, string details)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = @"INSERT INTO LoginAudit (Timestamp, Username, EventType, IpOrMachine, Details)

                                        VALUES (@ts, @u, @e, @m, @d)";

                    cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.Parameters.AddWithValue("@u", string.IsNullOrWhiteSpace(username) ? "UNKNOWN" : username.Trim());

                    cmd.Parameters.AddWithValue("@e", eventType);

                    cmd.Parameters.AddWithValue("@m", Environment.MachineName);

                    cmd.Parameters.AddWithValue("@d", details ?? "");

                    cmd.ExecuteNonQuery();

                }

            }

            catch { }

        }

        private static DateTime? GetLastSecurityEventTime(SqliteConnection conn, string username, string eventType)

        {

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT MAX(Timestamp) FROM LoginAudit WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@u)) AND EventType = @e";

            cmd.Parameters.AddWithValue("@u", username ?? "");

            cmd.Parameters.AddWithValue("@e", eventType);

            var v = cmd.ExecuteScalar();

            if (v == null || v == DBNull.Value) return null;

            return DateTime.TryParse(v.ToString(), out var dt) ? dt : null;

        }

        /// <summary>Number of failed logins for this username since the last success or lockout (within the last 15 minutes). Survives app restarts.</summary>

        public static int GetFailedLoginCountSinceReset(string username)

        {

            if (string.IsNullOrWhiteSpace(username)) return 0;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                DateTime since = DateTime.Now.AddMinutes(-15);

                var lastOk = GetLastSecurityEventTime(conn, username, "LOGIN_SUCCESS");

                var lastLock = GetLastSecurityEventTime(conn, username, "LOCKED_OUT");

                if (lastOk.HasValue && lastOk.Value > since) since = lastOk.Value;

                if (lastLock.HasValue && lastLock.Value > since) since = lastLock.Value;

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM LoginAudit WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@u)) AND EventType = 'LOGIN_FAILED' AND Timestamp > @since";

                cmd.Parameters.AddWithValue("@u", username);

                cmd.Parameters.AddWithValue("@since", since.ToString("yyyy-MM-dd HH:mm:ss"));

                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] GetFailedLoginCountSinceReset: {ex.Message}");

                return 0;

            }

        }

        /// <summary>Seconds left on an active lockout for this username (0 if not locked). Survives app restarts.</summary>

        public static int GetLoginLockoutSecondsRemaining(string username, int lockSeconds = 60)

        {

            if (string.IsNullOrWhiteSpace(username)) return 0;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                var lastLock = GetLastSecurityEventTime(conn, username, "LOCKED_OUT");

                if (!lastLock.HasValue) return 0;

                int remaining = (int)Math.Ceiling((lastLock.Value.AddSeconds(lockSeconds) - DateTime.Now).TotalSeconds);

                return Math.Max(0, Math.Min(lockSeconds, remaining));

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] GetLoginLockoutSecondsRemaining: {ex.Message}");

                return 0;

            }

        }

        public static async Task LogSecurityEventAsync(string username, string eventType, string details)

        {

            await Task.Run(() => LogSecurityEvent(username, eventType, details));

        }

        public static List<LoginAudit> GetSecurityAuditLogs(int limit = 200)

        {

            var list = new List<LoginAudit>();

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = $"SELECT Id, Timestamp, Username, EventType, IpOrMachine, Details FROM LoginAudit ORDER BY Id DESC LIMIT {limit}";

                    using (var reader = cmd.ExecuteReader())

                    {

                        while (reader.Read())

                        {

                            list.Add(new LoginAudit

                            {

                                Id = reader.GetInt32(0),

                                Timestamp = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.Now,

                                Username = reader.GetString(2),

                                EventType = reader.GetString(3),

                                IpOrMachine = reader.GetString(4),

                                Details = reader.IsDBNull(5) ? "" : reader.GetString(5)

                            });

                        }

                    }

                }

            }

            catch { }

            return list;

        }

        public static async Task<List<LoginAudit>> GetSecurityAuditLogsAsync(int limit = 200)

        {

            return await Task.Run(() => GetSecurityAuditLogs(limit));

        }

        public static void LogAuditTrail(string actionType, string tableName, int recordId, string recordIdentifier, string? fieldName, string? oldValue, string? newValue, string? reason)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = @"

                        INSERT INTO AuditTrail (Timestamp, Username, UserRole, ActionType, TableName, RecordId, RecordIdentifier, FieldName, OldValue, NewValue, ReasonOrNotes, IpOrMachine)

                        VALUES (@ts, @u, @r, @act, @tbl, @rid, @rident, @fld, @old, @new, @rsn, @ip);

                    ";

                    cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.Parameters.AddWithValue("@u", SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername);

                    cmd.Parameters.AddWithValue("@r", SessionContext.ActiveUser?.Role ?? "System");

                    cmd.Parameters.AddWithValue("@act", actionType ?? "UPDATE");

                    cmd.Parameters.AddWithValue("@tbl", tableName ?? "");

                    cmd.Parameters.AddWithValue("@rid", recordId);

                    cmd.Parameters.AddWithValue("@rident", recordIdentifier ?? "");

                    cmd.Parameters.AddWithValue("@fld", fieldName ?? "");

                    cmd.Parameters.AddWithValue("@old", oldValue ?? "");

                    cmd.Parameters.AddWithValue("@new", newValue ?? "");

                    cmd.Parameters.AddWithValue("@rsn", reason ?? "");

                    cmd.Parameters.AddWithValue("@ip", Environment.MachineName);

                    cmd.ExecuteNonQuery();

                }

            }

            catch { }

        }

        public static async Task LogAuditTrailAsync(string actionType, string tableName, int recordId, string recordIdentifier, string? fieldName, string? oldValue, string? newValue, string? reason)

        {

            await Task.Run(() => LogAuditTrail(actionType, tableName, recordId, recordIdentifier, fieldName, oldValue, newValue, reason));

        }

        public static List<AuditTrailEntry> GetAuditTrail(int limit = 500)

        {

            return GetAuditTrail(null, null, limit);

        }

        public static List<AuditTrailEntry> GetAuditTrail(string[]? tableNames, string? actionFilter = null, int limit = 500)

        {

            var list = new List<AuditTrailEntry>();

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    var conditions = new List<string>();

                    if (tableNames != null && tableNames.Length > 0)

                    {

                        var tblParams = new List<string>();

                        for (int i = 0; i < tableNames.Length; i++)

                        {

                            tblParams.Add($"@tbl{i}");

                            cmd.Parameters.AddWithValue($"@tbl{i}", tableNames[i]);

                        }

                        conditions.Add($"TableName IN ({string.Join(",", tblParams)})");

                    }

                    if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("All", StringComparison.OrdinalIgnoreCase) && !actionFilter.Equals("All Actions", StringComparison.OrdinalIgnoreCase))

                    {

                        conditions.Add("ActionType = @act");

                        cmd.Parameters.AddWithValue("@act", actionFilter);

                    }

                    string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

                    cmd.CommandText = $"SELECT Id, Timestamp, Username, UserRole, ActionType, TableName, RecordId, RecordIdentifier, FieldName, OldValue, NewValue, ReasonOrNotes, IpOrMachine FROM AuditTrail {whereClause} ORDER BY Id DESC LIMIT {limit}";

                    using (var reader = cmd.ExecuteReader())

                    {

                        while (reader.Read())

                        {

                            list.Add(new AuditTrailEntry

                            {

                                Id = reader.GetInt32(0),

                                Timestamp = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.Now,

                                Username = reader.GetString(2),

                                UserRole = reader.GetString(3),

                                ActionType = reader.GetString(4),

                                TableName = reader.GetString(5),

                                RecordId = reader.GetInt32(6),

                                RecordIdentifier = reader.GetString(7),

                                FieldName = reader.IsDBNull(8) ? "" : reader.GetString(8),

                                OldValue = reader.IsDBNull(9) ? "" : reader.GetString(9),

                                NewValue = reader.IsDBNull(10) ? "" : reader.GetString(10),

                                ReasonOrNotes = reader.IsDBNull(11) ? "" : reader.GetString(11),

                                IpOrMachine = reader.IsDBNull(12) ? "" : reader.GetString(12)

                            });

                        }

                    }

                }

            }

            catch { }

            return list;

        }

        public static async Task<List<AuditTrailEntry>> GetAuditTrailAsync(int limit = 500)

        {

            return await Task.Run(() => GetAuditTrail(null, null, limit));

        }

        public static async Task<List<AuditTrailEntry>> GetAuditTrailAsync(string[]? tableNames, string? actionFilter = null, int limit = 500)

        {

            return await Task.Run(() => GetAuditTrail(tableNames, actionFilter, limit));

        }

        public static void SaveSystemNotification(SystemNotification notif)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    INSERT INTO SystemNotifications (Id, Title, Message, Timestamp, Type, Category, IsRead, TargetModule, TargetRecordId)

                    VALUES (@id, @title, @message, @timestamp, @type, @category, @isRead, @targetModule, @targetRecordId)

                    ON CONFLICT(Id) DO UPDATE SET

                        Title = excluded.Title,

                        Message = excluded.Message,

                        Type = excluded.Type,

                        Category = excluded.Category,

                        IsRead = excluded.IsRead,

                        TargetModule = excluded.TargetModule,

                        TargetRecordId = excluded.TargetRecordId;

                ";

                cmd.Parameters.AddWithValue("@id", notif.Id);

                cmd.Parameters.AddWithValue("@title", notif.Title ?? "");

                cmd.Parameters.AddWithValue("@message", notif.Message ?? "");

                cmd.Parameters.AddWithValue("@timestamp", notif.Timestamp.ToString("o"));

                cmd.Parameters.AddWithValue("@type", notif.Type.ToString());

                cmd.Parameters.AddWithValue("@category", notif.Category ?? "System");

                cmd.Parameters.AddWithValue("@isRead", notif.IsRead ? 1 : 0);

                cmd.Parameters.AddWithValue("@targetModule", (object?)notif.TargetModule ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@targetRecordId", (object?)notif.TargetRecordId ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                PruneOldSystemNotifications(150);

            }

            catch { }

        }

        public static List<SystemNotification> GetSystemNotifications(int limit = 50, bool unreadOnly = false, string? category = null, bool? isAdmin = null)

        {

            bool admin = isAdmin ?? SessionContext.IsAdmin;

            var list = new List<SystemNotification>();

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                string query = "SELECT Id, Title, Message, Timestamp, Type, Category, IsRead, TargetModule, TargetRecordId FROM SystemNotifications WHERE 1=1";

                if (!admin)

                {

                    query += " AND LOWER(Category) NOT IN ('security', 'system', 'audit') AND LOWER(COALESCE(TargetModule, '')) NOT IN ('settings', 'audit', 'users') AND LOWER(Title) NOT LIKE '%security%' AND LOWER(Title) NOT LIKE '%user%' AND LOWER(Title) NOT LIKE '%session%' AND LOWER(Title) NOT LIKE '%password%' AND LOWER(Title) NOT LIKE '%login%' AND LOWER(Title) NOT LIKE '%auth%' AND LOWER(Title) NOT LIKE '%backup%' AND LOWER(Title) NOT LIKE '%database%'";

                }

                if (unreadOnly)

                {

                    query += " AND IsRead = 0";

                }

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))

                {

                    query += " AND Category = @cat";

                    cmd.Parameters.AddWithValue("@cat", category);

                }

                query += $" ORDER BY Timestamp DESC LIMIT {limit}";

                cmd.CommandText = query;

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    string id = reader.GetString(0);

                    string title = reader.GetString(1);

                    string message = reader.GetString(2);

                    string tsStr = reader.GetString(3);

                    DateTime.TryParse(tsStr, out DateTime ts);

                    string typeStr = reader.GetString(4);

                    Enum.TryParse(typeStr, true, out ToastType toastType);

                    string cat = reader.GetString(5);

                    bool isRead = reader.GetInt32(6) == 1;

                    string? targetMod = reader.IsDBNull(7) ? null : reader.GetString(7);

                    string? targetRec = reader.IsDBNull(8) ? null : reader.GetString(8);

                    var item = new SystemNotification

                    {

                        Id = id,

                        Title = title,

                        Message = message,

                        Timestamp = ts == default ? DateTime.Now : ts,

                        Type = toastType,

                        Category = cat,

                        IsRead = isRead,

                        TargetModule = targetMod,

                        TargetRecordId = targetRec

                    };

                    if (admin || !item.IsAdminOnly)

                    {

                        list.Add(item);

                    }

                }

            }

            catch { }

            return list;

        }

        public static int GetUnreadSystemNotificationCount(bool? isAdmin = null)

        {

            bool admin = isAdmin ?? SessionContext.IsAdmin;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                if (admin)

                {

                    cmd.CommandText = "SELECT COUNT(*) FROM SystemNotifications WHERE IsRead = 0";

                }

                else

                {

                    cmd.CommandText = "SELECT COUNT(*) FROM SystemNotifications WHERE IsRead = 0 AND LOWER(Category) NOT IN ('security', 'system', 'audit') AND LOWER(COALESCE(TargetModule, '')) NOT IN ('settings', 'audit', 'users') AND LOWER(Title) NOT LIKE '%security%' AND LOWER(Title) NOT LIKE '%user%' AND LOWER(Title) NOT LIKE '%session%' AND LOWER(Title) NOT LIKE '%password%' AND LOWER(Title) NOT LIKE '%login%' AND LOWER(Title) NOT LIKE '%auth%' AND LOWER(Title) NOT LIKE '%backup%' AND LOWER(Title) NOT LIKE '%database%'";

                }

                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

            }

            catch

            {

                return 0;

            }

        }

        public static void MarkSystemNotificationAsRead(string id)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE SystemNotifications SET IsRead = 1 WHERE Id = @id";

                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();

            }

            catch { }

        }

        public static void MarkAllSystemNotificationsAsRead(bool? isAdmin = null)

        {

            bool admin = isAdmin ?? SessionContext.IsAdmin;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                if (admin)

                {

                    cmd.CommandText = "UPDATE SystemNotifications SET IsRead = 1 WHERE IsRead = 0";

                }

                else

                {

                    cmd.CommandText = "UPDATE SystemNotifications SET IsRead = 1 WHERE IsRead = 0 AND LOWER(Category) NOT IN ('security', 'system', 'audit') AND LOWER(COALESCE(TargetModule, '')) NOT IN ('settings', 'audit', 'users') AND LOWER(Title) NOT LIKE '%security%' AND LOWER(Title) NOT LIKE '%user%' AND LOWER(Title) NOT LIKE '%session%' AND LOWER(Title) NOT LIKE '%password%' AND LOWER(Title) NOT LIKE '%login%' AND LOWER(Title) NOT LIKE '%auth%' AND LOWER(Title) NOT LIKE '%backup%' AND LOWER(Title) NOT LIKE '%database%'";

                }

                cmd.ExecuteNonQuery();

            }

            catch { }

        }

        public static void DeleteSystemNotification(string id)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "DELETE FROM SystemNotifications WHERE Id = @id";

                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();

            }

            catch { }

        }

        public static void ClearAllSystemNotifications(bool? isAdmin = null)

        {

            bool admin = isAdmin ?? SessionContext.IsAdmin;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                if (admin)

                {

                    cmd.CommandText = "DELETE FROM SystemNotifications";

                }

                else

                {

                    cmd.CommandText = "DELETE FROM SystemNotifications WHERE LOWER(Category) NOT IN ('security', 'system', 'audit') AND LOWER(COALESCE(TargetModule, '')) NOT IN ('settings', 'audit', 'users') AND LOWER(Title) NOT LIKE '%security%' AND LOWER(Title) NOT LIKE '%user%' AND LOWER(Title) NOT LIKE '%session%' AND LOWER(Title) NOT LIKE '%password%' AND LOWER(Title) NOT LIKE '%login%' AND LOWER(Title) NOT LIKE '%auth%' AND LOWER(Title) NOT LIKE '%backup%' AND LOWER(Title) NOT LIKE '%database%'";

                }

                cmd.ExecuteNonQuery();

            }

            catch { }

        }

        public static void PruneOldSystemNotifications(int keepCount = 150)

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = $@"

                    DELETE FROM SystemNotifications

                    WHERE Id NOT IN (

                        SELECT Id FROM SystemNotifications

                        ORDER BY Timestamp DESC

                        LIMIT {keepCount}

                    );

                ";

                cmd.ExecuteNonQuery();

            }

            catch { }

        }

        public static bool IsUsernameTaken(string username)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u))";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                return Convert.ToInt64(cmd.ExecuteScalar() ?? 0) > 0;

            }

        }

        public static string? GetUserSecurityQuestion(string username)

        {

            if (string.IsNullOrWhiteSpace(username)) return null;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT SecurityQuestion FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u))";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                var res = cmd.ExecuteScalar();

                if (res == null || res == DBNull.Value) return null;

                string q = res.ToString() ?? "";

                return string.IsNullOrWhiteSpace(q) ? "(No security question set — please ask the administrator to reset your password.)" : q;

            }

        }

        public static (string Question, string Answer) GetUserSecurityCredentials(int userId)

        {

            if (userId <= 0) return ("", "");

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "SELECT SecurityQuestion, SecurityAnswer FROM Users WHERE Id = @id";

                    cmd.Parameters.AddWithValue("@id", userId);

                    using (var reader = cmd.ExecuteReader())

                    {

                        if (reader.Read())

                        {

                            string q = reader.IsDBNull(0) ? "" : reader.GetString(0);

                            string a = reader.IsDBNull(1) ? "" : reader.GetString(1);

                            return (q.Trim(), a.Trim());

                        }

                    }

                }

            }

            catch { }

            return ("", "");

        }

        public static string? GetRawUserSecurityQuestion(string username)

        {

            if (string.IsNullOrWhiteSpace(username)) return null;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT SecurityQuestion FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u))";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                var res = cmd.ExecuteScalar();

                if (res == null || res == DBNull.Value) return null;

                string q = res.ToString() ?? "";

                return string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            }

        }

        public static (bool Success, string Message) VerifySecurityAnswerAndResetPassword(string username, string answer, string newPassword)

        {

            if (string.IsNullOrWhiteSpace(username)) return (false, "Username is required.");

            if (string.IsNullOrWhiteSpace(answer)) return (false, "Security answer is required.");

            if (string.IsNullOrWhiteSpace(newPassword)) return (false, "New password is required.");

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, SecurityAnswer, ContactNumber FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u))";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                using (var reader = cmd.ExecuteReader())

                {

                    if (!reader.Read())

                    {

                        return (false, "Account not found.");

                    }

                    int id = reader.GetInt32(0);

                    string dbAnswer = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    string contact = reader.IsDBNull(2) ? "" : reader.GetString(2);

                    reader.Close();

                    if (string.IsNullOrWhiteSpace(dbAnswer))

                    {

                        LogSecurityEvent(username, "PASSWORD_RESET_FAILED", "No security question configured");

                        return (false, "No security question is set up for this account. Please ask the administrator to reset your password.");

                    }

                    using (var lockCmd = conn.CreateCommand())

                    {

                        lockCmd.CommandText = "SELECT COUNT(*) FROM LoginAudit WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@u)) AND EventType = 'PASSWORD_RESET_FAILED' AND Timestamp > @since";

                        lockCmd.Parameters.AddWithValue("@u", username.Trim());

                        lockCmd.Parameters.AddWithValue("@since", DateTime.Now.AddMinutes(-15).ToString("yyyy-MM-dd HH:mm:ss"));

                        if (Convert.ToInt32(lockCmd.ExecuteScalar() ?? 0) >= 5)

                        {

                            return (false, "Too many incorrect answers. Please wait 15 minutes or ask the administrator to reset your password.");

                        }

                    }

                    bool matched = VerifySecurityAnswer(answer, dbAnswer);

                    if (!matched)

                    {

                        LogSecurityEvent(username, "PASSWORD_RESET_FAILED", "Incorrect security answer provided");

                        return (false, "Incorrect security answer. Please try again.");

                    }

                    using (var updateCmd = conn.CreateCommand())

                    {

                        updateCmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";

                        updateCmd.Parameters.AddWithValue("@p", HashPassword(newPassword));

                        updateCmd.Parameters.AddWithValue("@id", id);

                        updateCmd.ExecuteNonQuery();

                    }

                    LogSecurityEvent(username, "PASSWORD_RESET_SUCCESS", "Password reset successfully via security question verification");

                    return (true, "Password has been reset successfully. You can now log in with your new password.");

                }

            }

        }

        public static bool UpdateUserSecurityQuestion(int userId, string question, string answer)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE Users SET SecurityQuestion = @q, SecurityAnswer = @a WHERE Id = @id";

                    cmd.Parameters.AddWithValue("@q", question.Trim());

                    cmd.Parameters.AddWithValue("@a", HashSecurityAnswer(answer));

                    cmd.Parameters.AddWithValue("@id", userId);

                    cmd.ExecuteNonQuery();

                    LogAuditTrail("SECURITY_UPDATE", "Users", userId, $"User #{userId}", "SecurityQuestion", "", question, "Security recovery question configured/updated");

                    return true;

                }

            }

            catch

            {

                return false;

            }

        }

        public static bool UpdateUserPassword(int userId, string newPassword)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";

                    cmd.Parameters.AddWithValue("@p", HashPassword(newPassword));

                    cmd.Parameters.AddWithValue("@id", userId);

                    cmd.ExecuteNonQuery();

                    LogAuditTrail("PASSWORD_CHANGE", "Users", userId, $"User #{userId}", "Password", "", "********", "Account password updated");

                    return true;

                }

            }

            catch

            {

                return false;

            }

        }

        public static void RegisterStaffUser(User user)

        {

            if (string.IsNullOrWhiteSpace(user.FullName))

            {

                user.FullName = NameHelper.FormatFullName(user.LastName, user.FirstName, user.MiddleName);

            }

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, SecurityQuestion, SecurityAnswer, LastName, FirstName, MiddleName)

                                    VALUES (@u, @p, @r, @f, @c, @active, @now, @sq, @sa, @ln, @fn, @mn)";

                cmd.Parameters.AddWithValue("@u", user.Username.Trim());

                cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));

                cmd.Parameters.AddWithValue("@r", string.IsNullOrWhiteSpace(user.Role) ? "Office Staff" : user.Role.Trim());

                cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@f", user.FullName.Trim());

                cmd.Parameters.AddWithValue("@c", (user.ContactNumber ?? "").Trim());

                cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.Parameters.AddWithValue("@sq", user.SecurityQuestion ?? "");

                cmd.Parameters.AddWithValue("@sa", HashSecurityAnswer(user.SecurityAnswer));

                cmd.Parameters.AddWithValue("@ln", user.LastName ?? "");

                cmd.Parameters.AddWithValue("@fn", user.FirstName ?? "");

                cmd.Parameters.AddWithValue("@mn", user.MiddleName ?? "");

                cmd.ExecuteNonQuery();

            }

            LogSecurityEvent(user.Username, "ACCOUNT_REGISTERED", $"Staff user registered ({user.Role}): {user.FullName} ({user.ContactNumber})");

        }

        public static async Task RegisterStaffUserAsync(User user)

        {

            await Task.Run(() => RegisterStaffUser(user));

        }

        public static void ApproveUser(int userId)

        {

            string username = "";

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var getCmd = conn.CreateCommand();

                getCmd.CommandText = "SELECT Username FROM Users WHERE Id = @id";

                getCmd.Parameters.AddWithValue("@id", userId);

                username = getCmd.ExecuteScalar()?.ToString() ?? "";

                var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Users SET IsActive = 1 WHERE Id = @id";

                cmd.Parameters.AddWithValue("@id", userId);

                cmd.ExecuteNonQuery();

            }

            LogSecurityEvent(username, "USER_APPROVED", $"Owner approved user ID #{userId} ({username})");

        }

        public static async Task ApproveUserAsync(int userId)

        {

            await Task.Run(() => ApproveUser(userId));

        }

        public static void RejectUser(int userId)

        {

            string username = "";

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var getCmd = conn.CreateCommand();

                getCmd.CommandText = "SELECT Username FROM Users WHERE Id = @id";

                getCmd.Parameters.AddWithValue("@id", userId);

                username = getCmd.ExecuteScalar()?.ToString() ?? "";

                var cmd = conn.CreateCommand();

                cmd.CommandText = "DELETE FROM Users WHERE Id = @id AND IsActive = 0";

                cmd.Parameters.AddWithValue("@id", userId);

                cmd.ExecuteNonQuery();

            }

            LogSecurityEvent(username, "USER_REJECTED", $"Owner rejected pending user ID #{userId} ({username})");

        }

        public static async Task RejectUserAsync(int userId)

        {

            await Task.Run(() => RejectUser(userId));

        }

        public static int GetPendingUsersCount()

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE IsActive = 0";

                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

            }

        }

        public static async Task<int> GetPendingUsersCountAsync()

        {

            return await Task.Run(GetPendingUsersCount);

        }

        public static User? AuthenticateUser(string username, string password)

        {

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))

                return null;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT * FROM Users WHERE TRIM(LOWER(Username)) = TRIM(LOWER(@u)) AND IsActive = 1";

                cmd.Parameters.AddWithValue("@u", username.Trim());

                using (var reader = cmd.ExecuteReader())

                {

                    if (reader.Read())

                    {

                        int userId = reader.GetInt32(0);

                        string dbUser = reader.GetString(1);

                        string dbPass = reader.GetString(2);

                        string dbRole = reader.GetString(3);

                        string dbName = reader.GetString(4);

                        string dbContact = reader.IsDBNull(5) ? "" : reader.GetString(5);

                        bool dbActive = reader.GetInt32(6) == 1;

                        DateTime dbCreatedAt = DateTime.TryParse(reader.GetString(7), out var parsedDt) ? parsedDt : DateTime.Now;

                        bool isMatch = VerifyPassword(password, dbPass, out bool needsUpgrade);

                        string hashedInput = isMatch && needsUpgrade ? HashPassword(password) : dbPass;

                        if (isMatch)

                        {

                            if (dbPass != hashedInput)

                            {

                                try

                                {

                                    using (var upgradeCmd = conn.CreateCommand())

                                    {

                                        upgradeCmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";

                                        upgradeCmd.Parameters.AddWithValue("@p", hashedInput);

                                        upgradeCmd.Parameters.AddWithValue("@id", userId);

                                        upgradeCmd.ExecuteNonQuery();

                                    }

                                }

                                catch { }

                            }

                            string secQ = "";

                            string secA = "";

                            string ln = "";

                            string fn = "";

                            string mn = "";

                            for (int i = 0; i < reader.FieldCount; i++)

                            {

                                string colName = reader.GetName(i);

                                if (colName.Equals("SecurityQuestion", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                    secQ = reader.GetString(i);

                                else if (colName.Equals("SecurityAnswer", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                    secA = reader.GetString(i);

                                else if (colName.Equals("LastName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                    ln = reader.GetString(i);

                                else if (colName.Equals("FirstName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                    fn = reader.GetString(i);

                                else if (colName.Equals("MiddleName", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))

                                    mn = reader.GetString(i);

                            }

                            if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(dbName))

                            {

                                var s = NameHelper.SplitFullName(dbName);

                                ln = s.LastName;

                                fn = s.FirstName;

                                mn = s.MiddleName;

                            }

                            return new User

                            {

                                Id = userId,

                                Username = dbUser,

                                Password = hashedInput,

                                Role = dbRole,

                                FullName = dbName,

                                ContactNumber = dbContact,

                                IsActive = dbActive,

                                CreatedAt = dbCreatedAt,

                                SecurityQuestion = secQ,

                                SecurityAnswer = secA,

                                LastName = ln,

                                FirstName = fn,

                                MiddleName = mn

                            };

                        }

                    }

                }

            }

            return null;

        }

        public static List<User> GetUsers()

        {

            var list = new List<User>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, LastName, FirstName, MiddleName, SecurityQuestion, SecurityAnswer FROM Users ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        string fn = reader.IsDBNull(4) ? "" : reader.GetString(4);

                        string ln = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : "";

                        string first = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : "";

                        string mid = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : "";

                        string secQ = reader.FieldCount > 11 && !reader.IsDBNull(11) ? reader.GetString(11) : "";

                        string secA = reader.FieldCount > 12 && !reader.IsDBNull(12) ? reader.GetString(12) : "";

                        if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(fn))

                        {

                            var s = NameHelper.SplitFullName(fn);

                            ln = s.LastName;

                            first = s.FirstName;

                            mid = s.MiddleName;

                        }

                        if (string.IsNullOrEmpty(fn))

                        {

                            fn = NameHelper.FormatFullName(ln, first, mid);

                        }

                        list.Add(new User

                        {

                            Id = reader.GetInt32(0),

                            Username = reader.GetString(1),

                            Password = reader.GetString(2),

                            Role = reader.GetString(3),

                            FullName = fn,

                            ContactNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            IsActive = reader.GetInt32(6) == 1,

                            CreatedAt = DateTime.TryParse(reader.GetString(7), out var dt) ? dt : DateTime.Now,

                            LastName = ln,

                            FirstName = first,

                            MiddleName = mid,

                            SecurityQuestion = secQ,

                            SecurityAnswer = secA

                        });

                    }

                }

            }

            return list;

        }

        public static bool CanDeactivateOrDeleteAdmin(int userId)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE LOWER(TRIM(Role)) IN ('admin', 'administrator', 'owner', 'owner/admin', 'owner / admin', 'business owner') AND IsActive = 1 AND Id <> @id";

                cmd.Parameters.AddWithValue("@id", userId);

                long activeAdminCount = (long)(cmd.ExecuteScalar() ?? 0);

                return activeAdminCount > 0;

            }

        }

        public static void SaveUser(User user)

        {

            bool wasNew = user.Id <= 0;

            if (string.IsNullOrWhiteSpace(user.FullName))

            {

                user.FullName = NameHelper.FormatFullName(user.LastName, user.FirstName, user.MiddleName);

            }

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (user.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, LastName, FirstName, MiddleName)

                                        VALUES (@u, @p, @r, @f, @c, @a, @now, @ln, @fn, @mn)";

                    cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));

                    cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                }

                else

                {

                    string storedPassword = "";

                    using (var pwCmd = conn.CreateCommand())

                    {

                        pwCmd.CommandText = "SELECT Password FROM Users WHERE Id = @id";

                        pwCmd.Parameters.AddWithValue("@id", user.Id);

                        storedPassword = pwCmd.ExecuteScalar()?.ToString() ?? "";

                    }

                    // The User object carries the stored hash when the password was not changed; only re-hash a newly typed password.

                    bool passwordChanged = !string.IsNullOrEmpty(user.Password) && !string.Equals(user.Password, storedPassword, StringComparison.Ordinal);

                    if (passwordChanged)

                    {

                        cmd.CommandText = @"UPDATE Users SET Username=@u, Password=@p, Role=@r, FullName=@f, ContactNumber=@c, IsActive=@a, LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE Id=@id";

                        string newHash = HashPassword(user.Password);

                        cmd.Parameters.AddWithValue("@p", newHash);

                        user.Password = newHash;

                    }

                    else

                    {

                        cmd.CommandText = @"UPDATE Users SET Username=@u, Role=@r, FullName=@f, ContactNumber=@c, IsActive=@a, LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE Id=@id";

                    }

                    cmd.Parameters.AddWithValue("@id", user.Id);

                }

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@u", user.Username);

                cmd.Parameters.AddWithValue("@r", user.Role);

                cmd.Parameters.AddWithValue("@f", user.FullName);

                cmd.Parameters.AddWithValue("@c", user.ContactNumber);

                cmd.Parameters.AddWithValue("@a", user.IsActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@ln", user.LastName ?? "");

                cmd.Parameters.AddWithValue("@fn", user.FirstName ?? "");

                cmd.Parameters.AddWithValue("@mn", user.MiddleName ?? "");

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    user.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : "UPDATE", "Users", user.Id, user.Username, "Role", "", user.Role, $"User account @{user.Username} ({user.Role}) {(isNew ? "created" : "updated")}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "User Account Created" : "User Account Updated", $"User @{user.Username} ({user.Role})", ToastType.Success);

            }

            catch { }

        }

        public static List<Crew> GetCrews()

        {

            UpdatePendingDeliveriesToTransit();

            var list = new List<Crew>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, FullName, Role, ContactNumber, LicenseNumber, DailyWage, FoodAllowance, IsAvailable, IsActive, IsSeasonal, PhotoPath, LastName, FirstName, MiddleName, COALESCE(LicenseClass, 'None'), COALESCE(LicenseIssued, ''), COALESCE(LicenseExpiry, '') FROM Crews ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        string fn = reader.IsDBNull(1) ? "" : reader.GetString(1);

                        string ln = reader.FieldCount > 11 && !reader.IsDBNull(11) ? reader.GetString(11) : "";

                        string first = reader.FieldCount > 12 && !reader.IsDBNull(12) ? reader.GetString(12) : "";

                        string mid = reader.FieldCount > 13 && !reader.IsDBNull(13) ? reader.GetString(13) : "";

                        if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(fn))

                        {

                            var s = NameHelper.SplitFullName(fn);

                            ln = s.LastName;

                            first = s.FirstName;

                            mid = s.MiddleName;

                        }

                        if (string.IsNullOrEmpty(fn))

                        {

                            fn = NameHelper.FormatFullName(ln, first, mid);

                        }

                        string photoPath = "";

                        try { if (reader.FieldCount > 10 && !reader.IsDBNull(10)) photoPath = reader.GetString(10); } catch { }

                        bool isSeasonal = false;

                        try { if (reader.FieldCount > 9 && !reader.IsDBNull(9)) isSeasonal = reader.GetInt32(9) == 1; } catch { }

                        list.Add(new Crew

                        {

                            Id = reader.GetInt32(0),

                            FullName = fn,

                            Role = reader.GetString(2),

                            ContactNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            LicenseNumber = reader.IsDBNull(4) ? "" : reader.GetString(4),

                            DailyWage = Convert.ToDecimal(reader.GetDouble(5)),

                            FoodAllowance = Convert.ToDecimal(reader.GetDouble(6)),

                            IsAvailable = reader.GetInt32(7) == 1,

                            IsActive = reader.GetInt32(8) == 1,

                            IsSeasonal = isSeasonal,

                            PhotoPath = photoPath,

                            LastName = ln,

                            FirstName = first,

                            MiddleName = mid,

                            LicenseClass = reader.FieldCount > 14 && !reader.IsDBNull(14) && reader.GetString(14).Length > 0 ? reader.GetString(14) : "None",

                            LicenseIssued = reader.FieldCount > 15 && !reader.IsDBNull(15) && DateTime.TryParse(reader.GetString(15), out var lIss) ? lIss : (DateTime?)null,

                            LicenseExpiry = reader.FieldCount > 16 && !reader.IsDBNull(16) && DateTime.TryParse(reader.GetString(16), out var lExp) ? lExp : (DateTime?)null

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<Crew>> GetCrewsAsync()

        {

            return await Task.Run(() => GetCrews());

        }

        public static void SaveCrew(Crew crew)

        {

            bool wasNew = crew.Id <= 0;

            if (string.IsNullOrWhiteSpace(crew.FullName))

            {

                crew.FullName = NameHelper.FormatFullName(crew.LastName, crew.FirstName, crew.MiddleName);

            }

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (crew.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO Crews (FullName, Role, ContactNumber, LicenseNumber, DailyWage, FoodAllowance, IsAvailable, IsActive, IsSeasonal, PhotoPath, LastName, FirstName, MiddleName, LicenseClass, LicenseIssued, LicenseExpiry)

                                        VALUES (@f, @r, @c, @l, @w, @fa, @av, @ac, @s, @photo, @ln, @fn, @mn, @lc, @li, @le)";

                }

                else

                {

                    cmd.CommandText = @"UPDATE Crews SET FullName=@f, Role=@r, ContactNumber=@c, LicenseNumber=@l, DailyWage=@w, FoodAllowance=@fa, IsAvailable=@av, IsActive=@ac, IsSeasonal=@s, PhotoPath=@photo, LastName=@ln, FirstName=@fn, MiddleName=@mn, LicenseClass=@lc, LicenseIssued=@li, LicenseExpiry=@le WHERE Id=@id";

                    cmd.Parameters.AddWithValue("@id", crew.Id);

                }

                cmd.Parameters.AddWithValue("@f", crew.FullName);

                cmd.Parameters.AddWithValue("@r", crew.Role);

                cmd.Parameters.AddWithValue("@c", crew.ContactNumber);

                // The driver's license number is no longer stored (privacy); only its class and validity dates are.

                cmd.Parameters.AddWithValue("@l", "");

                cmd.Parameters.AddWithValue("@lc", string.IsNullOrWhiteSpace(crew.LicenseClass) ? "None" : crew.LicenseClass);

                cmd.Parameters.AddWithValue("@li", crew.LicenseIssued?.ToString("yyyy-MM-dd") ?? "");

                cmd.Parameters.AddWithValue("@le", crew.LicenseExpiry?.ToString("yyyy-MM-dd") ?? "");

                cmd.Parameters.AddWithValue("@w", crew.DailyWage);

                cmd.Parameters.AddWithValue("@fa", crew.FoodAllowance);

                cmd.Parameters.AddWithValue("@av", crew.IsAvailable ? 1 : 0);

                cmd.Parameters.AddWithValue("@ac", crew.IsActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@s", crew.IsSeasonal ? 1 : 0);

                cmd.Parameters.AddWithValue("@photo", crew.PhotoPath ?? "");

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@ln", crew.LastName ?? "");

                cmd.Parameters.AddWithValue("@fn", crew.FirstName ?? "");

                cmd.Parameters.AddWithValue("@mn", crew.MiddleName ?? "");

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    crew.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : "UPDATE", "Crews", crew.Id, crew.FullName, "Role", "", crew.Role, $"Crew member {crew.FullName} ({crew.Role}) {(isNew ? "registered" : "updated")}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "Crew Member Added" : "Crew Member Updated", $"{crew.FullName} ({crew.Role})", ToastType.Success);

            }

            catch { }

        }

        public static string? ValidateDuplicateCrew(int crewId, string fullName, string? contactNumber = null, string? licenseNumber = null, string? lastName = null, string? firstName = null)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var nameCmd = conn.CreateCommand();

                if (!string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(firstName))

                {

                    nameCmd.CommandText = "SELECT Id, Role FROM Crews WHERE ((TRIM(LOWER(LastName)) = TRIM(LOWER(@ln)) AND TRIM(LOWER(FirstName)) = TRIM(LOWER(@fn))) OR TRIM(LOWER(FullName)) = TRIM(LOWER(@name))) AND Id <> @id";

                    nameCmd.Parameters.AddWithValue("@ln", lastName.Trim());

                    nameCmd.Parameters.AddWithValue("@fn", firstName.Trim());

                }

                else

                {

                    nameCmd.CommandText = "SELECT Id, Role FROM Crews WHERE TRIM(LOWER(FullName)) = TRIM(LOWER(@name)) AND Id <> @id";

                }

                nameCmd.Parameters.AddWithValue("@name", fullName.Trim());

                nameCmd.Parameters.AddWithValue("@id", crewId);

                using (var r = nameCmd.ExecuteReader())

                {

                    if (r.Read())

                    {

                        return $"DUPLICATE CREW NAME DETECTED:\nA crew member named '{fullName}' is already registered as '{r.GetString(1)}' (ID: {r.GetInt32(0)}).";

                    }

                }

                if (!string.IsNullOrWhiteSpace(contactNumber))

                {

                    string cleanPhone = contactNumber.Trim().Replace(" ", "").Replace("-", "");

                    if (cleanPhone.Length >= 7)

                    {

                        var phoneCmd = conn.CreateCommand();

                        phoneCmd.CommandText = "SELECT Id, FullName, Role FROM Crews WHERE REPLACE(REPLACE(ContactNumber, ' ', ''), '-', '') = @phone AND Id <> @id";

                        phoneCmd.Parameters.AddWithValue("@phone", cleanPhone);

                        phoneCmd.Parameters.AddWithValue("@id", crewId);

                        using (var r = phoneCmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"DUPLICATE CONTACT NUMBER:\nContact number '{contactNumber}' is already registered to '{r.GetString(1)}' ({r.GetString(2)}, ID: {r.GetInt32(0)}).";

                            }

                        }

                    }

                }

                if (!string.IsNullOrWhiteSpace(licenseNumber))

                {

                    string cleanLic = licenseNumber.Trim().Replace(" ", "").Replace("-", "");

                    if (!cleanLic.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !cleanLic.Equals("NONE", StringComparison.OrdinalIgnoreCase))

                    {

                        var licCmd = conn.CreateCommand();

                        licCmd.CommandText = "SELECT Id, FullName FROM Crews WHERE REPLACE(REPLACE(UPPER(LicenseNumber), ' ', ''), '-', '') = @lic AND Id <> @id";

                        licCmd.Parameters.AddWithValue("@lic", cleanLic.ToUpper());

                        licCmd.Parameters.AddWithValue("@id", crewId);

                        using (var r = licCmd.ExecuteReader())

                        {

                            if (r.Read())

                            {

                                return $"DUPLICATE DRIVER'S LICENSE:\nDriver's License #{licenseNumber} is already registered to '{r.GetString(1)}' (ID: {r.GetInt32(0)}).";

                            }

                        }

                    }

                }

            }

            return null;

        }

        public static List<Delivery> GetCrewHistory(int crewId)

        {

            var list = new List<Delivery>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT Id, DispatchNumber, LoadingScheduleId, LoadingScheduleInfo, TruckId, TruckInfo, DriverId, DriverName, AideId, AideName, DispatchDate, DepartureTime, Status, Remarks FROM Deliveries WHERE DriverId = @id OR AideId = @id OR {AideIdsContainsSql} ORDER BY DispatchDate DESC, Id DESC";

                cmd.Parameters.AddWithValue("@id", crewId);

                cmd.Parameters.AddWithValue("@aidToken", crewId.ToString());

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(new Delivery

                        {

                            Id = reader.GetInt32(0),

                            DispatchNumber = reader.GetString(1),

                            LoadingScheduleId = reader.GetInt32(2),

                            LoadingScheduleInfo = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            TruckId = reader.GetInt32(4),

                            TruckInfo = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            DriverId = reader.GetInt32(6),

                            DriverName = reader.IsDBNull(7) ? "" : reader.GetString(7),

                            AideId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),

                            AideName = reader.IsDBNull(9) ? "" : reader.GetString(9),

                            DispatchDate = DateTime.TryParse(reader.GetString(10), out var dt) ? dt : DateTime.Today,

                            DepartureTime = reader.IsDBNull(11) ? "08:00 AM" : reader.GetString(11),

                            Status = reader.GetString(12),

                            Remarks = reader.IsDBNull(13) ? "" : reader.GetString(13)

                        });

                    }

                }

            }

            return list;

        }

        public static (decimal TotalCollected, decimal TotalToBeCollected, decimal TotalBilled) GetFinancialTotals()

        {

            decimal totalCollected = 0;

            decimal totalBilled = 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd1 = conn.CreateCommand();

                cmd1.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE COALESCE(IsVoid,0) = 0";

                totalCollected = Convert.ToDecimal(cmd1.ExecuteScalar() ?? 0);

                var cmd2 = conn.CreateCommand();

                cmd2.CommandText = "SELECT COALESCE(SUM(NetAmount), 0) FROM Invoices WHERE Status <> 'Cancelled'";

                totalBilled = Convert.ToDecimal(cmd2.ExecuteScalar() ?? 0);

            }

            decimal totalToBeCollected = Math.Max(0, totalBilled - totalCollected);

            return (totalCollected, totalToBeCollected, totalBilled);

        }

        public static List<Truck> GetTrucks()

        {

            UpdatePendingDeliveriesToTransit();

            var list = new List<Truck>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, PlateNumber, TruckType, CapacityCrates, Status, Remarks, COALESCE(CargoType, 'Cold / Chilled') FROM Trucks ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        string rawSt = reader.IsDBNull(4) ? "Active" : reader.GetString(4);

                        list.Add(new Truck

                        {

                            Id = reader.GetInt32(0),

                            PlateNumber = reader.GetString(1),

                            TruckType = reader.GetString(2),

                            CapacityCrates = reader.GetInt32(3),

                            Status = rawSt.Equals("Available", StringComparison.OrdinalIgnoreCase) ? "Active" : rawSt,

                            Remarks = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            CargoType = reader.IsDBNull(6) ? "Cold / Chilled" : reader.GetString(6)

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<Truck>> GetTrucksAsync()

        {

            return await Task.Run(() => GetTrucks());

        }

        public static bool IsPlateNumberExists(string plateNumber, int excludeId = 0)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM Trucks WHERE LOWER(PlateNumber) = LOWER(@p) AND Id <> @id";

                cmd.Parameters.AddWithValue("@p", plateNumber.Trim());

                cmd.Parameters.AddWithValue("@id", excludeId);

                long count = (long)(cmd.ExecuteScalar() ?? 0);

                return count > 0;

            }

        }

        public static void SaveTruck(Truck truck)

        {

            bool wasNew = truck.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (truck.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO Trucks (PlateNumber, TruckType, CapacityCrates, Status, Remarks, CargoType)

                                        VALUES (@p, @t, @c, @s, @r, @cargo)";

                }

                else

                {

                    cmd.CommandText = @"UPDATE Trucks SET PlateNumber=@p, TruckType=@t, CapacityCrates=@c, Status=@s, Remarks=@r, CargoType=@cargo WHERE Id=@id";

                    cmd.Parameters.AddWithValue("@id", truck.Id);

                }

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@p", truck.PlateNumber.Trim());

                cmd.Parameters.AddWithValue("@t", truck.TruckType);

                cmd.Parameters.AddWithValue("@c", truck.CapacityCrates);

                cmd.Parameters.AddWithValue("@s", string.IsNullOrWhiteSpace(truck.Status) || truck.Status.Equals("Available", StringComparison.OrdinalIgnoreCase) ? "Active" : truck.Status);

                cmd.Parameters.AddWithValue("@r", truck.Remarks ?? "");

                cmd.Parameters.AddWithValue("@cargo", string.IsNullOrWhiteSpace(truck.CargoType) ? "Cold / Chilled" : truck.CargoType);

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    truck.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : "UPDATE", "Trucks", truck.Id, truck.PlateNumber, "Status", "", truck.Status, $"Truck {truck.PlateNumber} ({truck.TruckType}, {truck.CapacityCrates} crates) {(isNew ? "registered" : "updated")}, Status: {truck.Status}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "Truck Registered" : "Truck Updated", $"Plate {truck.PlateNumber} ({truck.TruckType}, {truck.CapacityCrates} crates)", ToastType.Success);

            }

            catch { }

        }

        public static TruckRecommendation GetTruckRecommendation(int totalCrates, DateTime dispatchDate, string? cargoType = null)

        {

            var rec = new TruckRecommendation { TotalCrates = totalCrates };

            if (totalCrates <= 0)

            {

                rec.RecommendationMessage = "Enter crate quantity to calculate vehicle recommendation.";

                return rec;

            }

            var trucks = GetTrucks();

            var busyTruckIds = new HashSet<int>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT TruckId FROM Deliveries WHERE DATE(DispatchDate) = DATE(@dd) AND Status IN {ActiveDeliveryStatusesSql}";

                cmd.Parameters.AddWithValue("@dd", dispatchDate.ToString("yyyy-MM-dd"));

                using (var r = cmd.ExecuteReader())

                {

                    while (r.Read()) if (!r.IsDBNull(0)) busyTruckIds.Add(r.GetInt32(0));

                }

                try

                {

                    using var stCmd = conn.CreateCommand();

                    stCmd.CommandText = $"SELECT TruckId FROM SpecialTrips WHERE DATE(DispatchDate) = DATE(@dd) AND Status IN {ActiveSpecialTripStatusesSql}";

                    stCmd.Parameters.AddWithValue("@dd", dispatchDate.ToString("yyyy-MM-dd"));

                    using var r2 = stCmd.ExecuteReader();

                    while (r2.Read()) if (!r2.IsDBNull(0)) busyTruckIds.Add(r2.GetInt32(0));

                }

                catch (SqliteException) { }

            }

            bool needsCold = !string.IsNullOrWhiteSpace(cargoType) &&

                             (cargoType.Contains("Cold", StringComparison.OrdinalIgnoreCase) || cargoType.Contains("Chill", StringComparison.OrdinalIgnoreCase) || cargoType.Contains("Mix", StringComparison.OrdinalIgnoreCase));

            bool isFutureDate = dispatchDate.Date > DateTime.Today;

            var availableTrucks = trucks.Where(t =>

                                            !IsTruckBlockedStatus(t.Status) &&

                                            (IsTruckReadyStatus(t.Status) || isFutureDate) &&

                                            !busyTruckIds.Contains(t.Id) &&

                                            (!needsCold || !string.Equals(t.CargoType, "Dry Goods", StringComparison.OrdinalIgnoreCase)))

                                        .OrderBy(t => t.CapacityCrates)

                                        .ToList();

            if (availableTrucks.Count == 0)

            {

                rec.RecommendationMessage = needsCold

                    ? "⚠️ No refrigerated truck is free on this date. Check maintenance status or reschedule another trip."

                    : "⚠️ No truck is free on this date. Check maintenance status or reschedule another trip.";

                rec.IsMultiTruckNeeded = true;

                return rec;

            }

            var optimalTruck = availableTrucks.FirstOrDefault(t => t.CapacityCrates >= totalCrates);

            if (optimalTruck != null)

            {

                rec.RecommendedTruck = optimalTruck;

                rec.RequiredTruckCount = 1;

                rec.CapacityUtilization = Math.Round(((double)totalCrates / optimalTruck.CapacityCrates) * 100.0, 1);

                rec.RecommendationMessage = $"✓ Recommended: {optimalTruck.PlateNumber} ({optimalTruck.TruckType} - {optimalTruck.CapacityCrates} crates) - {rec.CapacityUtilization}% capacity utilized.";

                rec.IsMultiTruckNeeded = false;

            }

            else

            {

                var maxTruck = availableTrucks.OrderByDescending(t => t.CapacityCrates).First();

                int countNeeded = maxTruck.CapacityCrates > 0 ? (int)Math.Ceiling((double)totalCrates / maxTruck.CapacityCrates) : 1;

                rec.RecommendedTruck = maxTruck;

                rec.RequiredTruckCount = countNeeded;

                rec.IsMultiTruckNeeded = true;

                rec.RecommendationMessage = $"⚠️ Volume ({totalCrates} crates) exceeds single truck capacity ({maxTruck.CapacityCrates} crates max). Minimum {countNeeded} truck trips required.";

            }

            return rec;

        }

        public static List<RateMatrix> GetRateMatrix()

        {

            var list = new List<RateMatrix>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT * FROM RateMatrix ORDER BY RouteName";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(new RateMatrix

                        {

                            Id = reader.GetInt32(0),

                            RouteName = reader.GetString(1),

                            Destination = reader.GetString(2),

                            TruckType = reader.GetString(3),

                            IsPeakSeason = reader.GetInt32(4) == 1,

                            RateAmount = Convert.ToDecimal(reader.GetDouble(5))

                        });

                    }

                }

            }

            return list;

        }

        public static RateMatrix? FindMatchingRate(string destination, bool isPeak = false, string truckType = "")

        {

            return FindMatchingRate(destination, isPeak, truckType, out _);

        }

        /// <summary>

        /// Finds the rate for a destination. Peak/regular season must always match. Truck type is preferred;

        /// if no rate exists for that truck type, a rate for the same route is used and truckTypeMatched is false.

        /// When several rates tie, the most recently added one wins.

        /// </summary>

        public static RateMatrix? FindMatchingRate(string destination, bool isPeak, string truckType, out bool truckTypeMatched)

        {

            truckTypeMatched = false;

            try

            {

                if (string.IsNullOrWhiteSpace(destination)) return null;

                var rates = GetRateMatrix().Where(r => r.IsPeakSeason == isPeak).OrderByDescending(r => r.Id).ToList();

                if (rates.Count == 0) return null;

                var routes = GetDeliveryRoutes();

                var targetRoute = FindRouteByNameOrCode(destination, routes);

                bool TypeOk(RateMatrix r) => !string.IsNullOrWhiteSpace(truckType) && r.TruckType.Equals(truckType, StringComparison.OrdinalIgnoreCase);

                bool RouteOk(RateMatrix r)

                {

                    if (targetRoute != null)

                    {

                        if (FindRouteByNameOrCode(r.RouteName, routes)?.Id == targetRoute.Id) return true;

                        if (FindRouteByNameOrCode(r.Destination, routes)?.Id == targetRoute.Id) return true;

                    }

                    if (!string.IsNullOrWhiteSpace(r.Destination) && destination.Contains(r.Destination, StringComparison.OrdinalIgnoreCase)) return true;

                    if (!string.IsNullOrWhiteSpace(r.RouteName) && (destination.Contains(r.RouteName, StringComparison.OrdinalIgnoreCase) || r.RouteName.Contains(destination, StringComparison.OrdinalIgnoreCase))) return true;

                    return false;

                }

                var withType = rates.FirstOrDefault(r => TypeOk(r) && RouteOk(r));

                if (withType != null)

                {

                    truckTypeMatched = true;

                    return withType;

                }

                return rates.FirstOrDefault(RouteOk);

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] FindMatchingRate: {ex.Message}");

            }

            return null;

        }

        public static decimal CalculateDeliveryTariff(Delivery del, out string matchDescription)

        {

            try

            {

                var schedules = GetLoadingSchedules();

                var ls = schedules.FirstOrDefault(s => s.Id == del.LoadingScheduleId);

                string dest = ls != null ? ls.Destination : (del.LoadingScheduleInfo ?? "");

                bool isPeak = ls != null && ls.IsPeak;

                string trkType = "";

                if (del.TruckId > 0)

                {

                    try

                    {

                        using var trkConn = new SqliteConnection(connectionString);

                        trkConn.Open();

                        using var trkCmd = trkConn.CreateCommand();

                        trkCmd.CommandText = "SELECT TruckType FROM Trucks WHERE Id = @tid";

                        trkCmd.Parameters.AddWithValue("@tid", del.TruckId);

                        var tRes = trkCmd.ExecuteScalar();

                        if (tRes != null && tRes != DBNull.Value) trkType = tRes.ToString()?.Trim() ?? "";

                    }

                    catch { }

                }

                if (string.IsNullOrWhiteSpace(trkType) && !string.IsNullOrWhiteSpace(del.TruckInfo))

                {

                    int openP = del.TruckInfo.IndexOf('(');

                    int closeP = del.TruckInfo.LastIndexOf(')');

                    if (openP >= 0 && closeP > openP)

                    {

                        trkType = del.TruckInfo.Substring(openP + 1, closeP - openP - 1).Trim();

                    }

                    else

                    {

                        trkType = del.TruckInfo.Trim();

                    }

                }

                if (del.IsMultiDrop)

                {

                    var stops = GetDispatchStops(del.Id);

                    if (stops != null && stops.Count > 0)

                    {

                        var rates = GetRateMatrix();

                        var matchedCombinedRate = rates.FirstOrDefault(r =>

                            r.IsPeakSeason == isPeak &&

                            (string.IsNullOrWhiteSpace(trkType) || r.TruckType.Equals(trkType, StringComparison.OrdinalIgnoreCase)) &&

                            stops.All(s => !string.IsNullOrWhiteSpace(s.Destination) && (r.Destination.Contains(s.Destination, StringComparison.OrdinalIgnoreCase) || r.RouteName.Contains(s.Destination, StringComparison.OrdinalIgnoreCase)))

                        );

                        if (matchedCombinedRate != null)

                        {

                            matchDescription = $"🏷️ Multi-Drop Combined Rate: {matchedCombinedRate.RouteName} ({matchedCombinedRate.Destination}) - {(matchedCombinedRate.IsPeakSeason ? "Peak" : "Regular")}";

                            return matchedCombinedRate.RateAmount;

                        }

                        decimal totalStopTariff = 0m;

                        int ratedStops = 0;

                        bool allTypesMatched = true;

                        foreach (var stop in stops)

                        {

                            var stopRate = FindMatchingRate(stop.Destination, isPeak, trkType, out bool stopTypeOk);

                            if (stopRate != null)

                            {

                                totalStopTariff += stopRate.RateAmount;

                                ratedStops++;

                                if (!stopTypeOk) allTypesMatched = false;

                            }

                        }

                        int unrated = stops.Count - ratedStops;

                        string warn = unrated > 0 ? $" ⚠️ {unrated} stop(s) have no rate — enter the amount manually." : (!allTypesMatched ? " ⚠️ Truck type not in Rate Matrix — verify amount." : "");

                        matchDescription = $"🏷️ Multi-Drop Consolidated Rate: {stops.Count} stops ({ratedStops} rated, Total ₱{totalStopTariff:N2}).{warn}";

                        return totalStopTariff;

                    }

                }

                var matchedRate = FindMatchingRate(dest, isPeak, trkType, out bool typeMatched);

                if (matchedRate != null)

                {

                    string typeWarn = typeMatched ? "" : $" ⚠️ No rate for truck type '{trkType}' — using {matchedRate.TruckType} rate, please verify.";

                    matchDescription = $"🏷️ Rate Matrix: {matchedRate.RouteName} ({matchedRate.Destination}) - {(matchedRate.IsPeakSeason ? "Peak" : "Regular")}{typeWarn}";

                    return matchedRate.RateAmount;

                }

                matchDescription = "⚠️ No Rate Matrix entry matches this route/season. Enter the amount manually or add the rate in Rate Matrix.";

                return 0m;

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] CalculateDeliveryTariff: {ex.Message}");

                matchDescription = "⚠️ Could not compute the rate. Enter the amount manually.";

                return 0m;

            }

        }

        public static (int AddedCount, int UpdatedCount, List<string> SavedDestinations) SaveMultipleRateMatrix(

            List<string> destinations,

            string truckType,

            bool isPeakSeason,

            decimal rateAmount,

            bool isCombinedRoute = false)

        {

            int added = 0;

            int updated = 0;

            var saved = new List<string>();

            if (destinations == null || destinations.Count == 0)

                return (0, 0, saved);

            SaveLookupItem("Truck Type / Category", truckType);

            if (isCombinedRoute)

            {

                string combined = string.Join(", ", destinations.Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).Distinct());

                if (!string.IsNullOrEmpty(combined))

                {

                    var existing = GetRateMatrix().FirstOrDefault(r =>

                        r.Destination.Equals(combined, StringComparison.OrdinalIgnoreCase) &&

                        r.TruckType.Equals(truckType, StringComparison.OrdinalIgnoreCase) &&

                        r.IsPeakSeason == isPeakSeason);

                    if (existing != null)

                    {

                        existing.RateAmount = rateAmount;

                        existing.RouteName = combined;

                        SaveRateMatrix(existing);

                        updated++;

                    }

                    else

                    {

                        SaveRateMatrix(new RateMatrix

                        {

                            RouteName = combined,

                            Destination = combined,

                            TruckType = truckType,

                            IsPeakSeason = isPeakSeason,

                            RateAmount = rateAmount

                        });

                        added++;

                    }

                    saved.Add(combined);

                }

            }

            else

            {

                var existingRates = GetRateMatrix();

                foreach (var rawDest in destinations)

                {

                    string dest = rawDest.Trim();

                    if (string.IsNullOrEmpty(dest)) continue;

                    var matchedRoute = FindRouteByNameOrCode(dest);

                    string routeTitle = matchedRoute != null ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}" : dest;

                    string fullDest = matchedRoute != null && matchedRoute.PlacesList.Count > 0

                        ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {matchedRoute.FormattedPlaces}"

                        : routeTitle;

                    var existing = existingRates.FirstOrDefault(r =>

                        (r.Destination.Equals(fullDest, StringComparison.OrdinalIgnoreCase) || r.RouteName.Equals(routeTitle, StringComparison.OrdinalIgnoreCase)) &&

                        r.TruckType.Equals(truckType, StringComparison.OrdinalIgnoreCase) &&

                        r.IsPeakSeason == isPeakSeason);

                    if (existing != null)

                    {

                        existing.RateAmount = rateAmount;

                        existing.RouteName = routeTitle;

                        existing.Destination = fullDest;

                        SaveRateMatrix(existing);

                        updated++;

                    }

                    else

                    {

                        SaveRateMatrix(new RateMatrix

                        {

                            RouteName = routeTitle,

                            Destination = fullDest,

                            TruckType = truckType,

                            IsPeakSeason = isPeakSeason,

                            RateAmount = rateAmount

                        });

                        added++;

                    }

                    saved.Add(routeTitle);

                }

            }

            LogSecurityEvent("SYSTEM", "RATE_MATRIX_BATCH", $"Configured {saved.Count} rates for {truckType} (Peak={isPeakSeason}) @ PHP {rateAmount:N2}");

            return (added, updated, saved);

        }

        public static void SaveRateMatrix(RateMatrix rm)

        {

            bool wasNew = rm.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (rm.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO RateMatrix (RouteName, Destination, TruckType, IsPeakSeason, RateAmount)

                                        VALUES (@r, @d, @t, @p, @a)";

                }

                else

                {

                    cmd.CommandText = @"UPDATE RateMatrix SET RouteName=@r, Destination=@d, TruckType=@t, IsPeakSeason=@p, RateAmount=@a WHERE Id=@id";

                    cmd.Parameters.AddWithValue("@id", rm.Id);

                }

                cmd.Parameters.AddWithValue("@r", rm.RouteName);

                cmd.Parameters.AddWithValue("@d", rm.Destination);

                cmd.Parameters.AddWithValue("@t", rm.TruckType);

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@p", rm.IsPeakSeason ? 1 : 0);

                cmd.Parameters.AddWithValue("@a", rm.RateAmount);

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    rm.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : "UPDATE", "RateMatrix", rm.Id, rm.Destination, "RateAmount", "", rm.RateAmount.ToString("N2"), $"Rate matrix for {rm.Destination} ({rm.TruckType}, {(rm.IsPeakSeason ? "Peak" : "Regular")}): PHP {rm.RateAmount:N2} {(isNew ? "added" : "updated")}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "Rate Matrix Entry Added" : "Rate Matrix Entry Updated", $"{rm.RouteName} ({rm.TruckType}): PHP {rm.RateAmount:N2}", ToastType.Success);

            }

            catch { }

        }

        private static void EnsureDefaultDeliveryRoutes(SqliteConnection conn)

        {

            try

            {

                string r1Places = "203077 - RR-Laoag Bacarra; 204526 - RR-San Nicolas; 202400 - RR-PMCC Vigan; 204848 - RR-Candon; 200977 - RR-Vigan; 206235 - RR-Narvacan";

                string r2Places = "204799 - RR-Agoo La Union; 200999 - RR-San Fernando La Union; 201082 - RR-Manna Mall; 205581 - RR-Bauang";

                using var updCmd = conn.CreateCommand();

                updCmd.CommandText = @"

                    UPDATE DeliveryRoutes SET RouteCode = 'R1' WHERE RouteCode = 'RT-001';

                    UPDATE DeliveryRoutes SET RouteCode = 'R2' WHERE RouteCode = 'RT-002';

                    UPDATE DeliveryRoutes SET RouteCode = 'R3' WHERE RouteCode = 'RT-003';

                    UPDATE DeliveryRoutes SET RouteCode = 'R4' WHERE RouteCode = 'RT-004';

                    UPDATE DeliveryRoutes SET RouteName = 'EODLAB (Ilocos Route)', Description = 'RCS Villasis Route EODLAB (Ilocos Norte & Ilocos Sur)', ConnectedPlaces = @r1p WHERE RouteCode = 'R1' AND ConnectedPlaces LIKE '%Tarlac City Central Depot%';

                    UPDATE DeliveryRoutes SET RouteName = 'EODGAA (La Union Route)', Description = 'RCS Villasis Route EODGAA (La Union)', ConnectedPlaces = @r2p WHERE RouteCode = 'R2' AND ConnectedPlaces LIKE '%San Fernando Central Depot%';

                ";

                updCmd.Parameters.AddWithValue("@r1p", r1Places);

                updCmd.Parameters.AddWithValue("@r2p", r2Places);

                updCmd.ExecuteNonQuery();

                using var checkCmd = conn.CreateCommand();

                checkCmd.CommandText = "SELECT COUNT(*) FROM DeliveryRoutes";

                long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);

                if (count > 0) return;

                var initialRoutes = new (string Code, string Name, string Description, string Places)[]

                {

                    ("R1", "EODLAB (Ilocos Route)", "RCS Villasis Route EODLAB (Ilocos Norte & Ilocos Sur)", r1Places),

                    ("R2", "EODGAA (La Union Route)", "RCS Villasis Route EODGAA (La Union)", r2Places),

                    ("R3", "Route 3", "Pangasinan Northern Arterial Run", "Tarlac City Depot; Gerona Branch; Paniqui Station; Rosales Commercial Mall; Urdaneta Central Distribution Hub"),

                    ("R4", "Route 4", "Bulacan - Metro Manila Commercial Run", "Bulacan North Depot; Malolos City Store; Guiguinto Industrial Hub; Marilao Central Mall; Valenzuela Metro Gateway")

                };

                foreach (var r in initialRoutes)

                {

                    using var ins = conn.CreateCommand();

                    ins.CommandText = "INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive) VALUES (@client, @code, @name, @desc, @places, 1)";

                    ins.Parameters.AddWithValue("@client", "BCS GROUP COMPANY");

                    ins.Parameters.AddWithValue("@code", r.Code);

                    ins.Parameters.AddWithValue("@name", r.Name);

                    ins.Parameters.AddWithValue("@desc", r.Description);

                    ins.Parameters.AddWithValue("@places", r.Places);

                    ins.ExecuteNonQuery();

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] EnsureDefaultDeliveryRoutes: {ex.Message}");

            }

        }

        public static List<DeliveryRoute> GetDeliveryRoutes(bool activeOnly = false)

        {

            var list = new List<DeliveryRoute>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = activeOnly

                    ? "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE IsActive = 1 ORDER BY RouteName ASC"

                    : "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes ORDER BY RouteName ASC";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    DateTime cAt = DateTime.TryParse(reader.GetString(6), out var cdt) ? cdt : DateTime.Now;

                    DateTime uAt = DateTime.TryParse(reader.GetString(7), out var udt) ? udt : DateTime.Now;

                    list.Add(new DeliveryRoute

                    {

                        Id = reader.GetInt32(0),

                        RouteCode = reader.GetString(1),

                        RouteName = reader.GetString(2),

                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),

                        ConnectedPlaces = reader.GetString(4),

                        IsActive = reader.GetInt32(5) == 1,

                        CreatedAt = cAt,

                        UpdatedAt = uAt,

                        ClientCompanyName = reader.IsDBNull(8) ? "" : reader.GetString(8)

                    });

                }

            }

            return list;

        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesAsync(bool activeOnly = false)

        {

            return await Task.Run(() => GetDeliveryRoutes(activeOnly));

        }

        public static List<DeliveryRoute> GetDeliveryRoutesByClient(string clientCompanyName, bool activeOnly = false)

        {

            var list = new List<DeliveryRoute>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                string sql = "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(clientCompanyName))

                {

                    sql += " AND (LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@client)) OR ClientCompanyName = '' OR ClientCompanyName IS NULL)";

                    cmd.Parameters.AddWithValue("@client", clientCompanyName.Trim());

                }

                if (activeOnly)

                {

                    sql += " AND IsActive = 1";

                }

                sql += " ORDER BY RouteName ASC";

                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    DateTime cAt = DateTime.TryParse(reader.GetString(6), out var cdt) ? cdt : DateTime.Now;

                    DateTime uAt = DateTime.TryParse(reader.GetString(7), out var udt) ? udt : DateTime.Now;

                    list.Add(new DeliveryRoute

                    {

                        Id = reader.GetInt32(0),

                        RouteCode = reader.GetString(1),

                        RouteName = reader.GetString(2),

                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),

                        ConnectedPlaces = reader.GetString(4),

                        IsActive = reader.GetInt32(5) == 1,

                        CreatedAt = cAt,

                        UpdatedAt = uAt,

                        ClientCompanyName = reader.IsDBNull(8) ? "" : reader.GetString(8)

                    });

                }

            }

            return list;

        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesByClientAsync(string clientCompanyName, bool activeOnly = false)

        {

            return await Task.Run(() => GetDeliveryRoutesByClient(clientCompanyName, activeOnly));

        }

        public static bool SaveDeliveryRoute(DeliveryRoute r)

        {

            List<string>? previousPlaces = null;

            if (r.Id > 0)

            {

                try { previousPlaces = GetDeliveryRoutes(activeOnly: false).FirstOrDefault(x => x.Id == r.Id)?.PlacesList; } catch { }

            }

            bool ok = SaveDeliveryRouteCore(r);

            if (ok)

            {

                try

                {

                    if (!string.IsNullOrWhiteSpace(r.ClientCompanyName)) EnsureClientExists(r.ClientCompanyName);

                    SyncClientStoresFromRoute(r, previousPlaces);

                }

                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] route→store sync: {ex.Message}"); }

            }

            return ok;

        }

        private static bool SaveDeliveryRouteCore(DeliveryRoute r)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                if (r.Id == 0 && !string.IsNullOrWhiteSpace(r.RouteCode))
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT Id, COALESCE(ClientCompanyName, ''), RouteName FROM DeliveryRoutes WHERE RouteCode = @rc LIMIT 1";

                    checkCmd.Parameters.AddWithValue("@rc", r.RouteCode);
                    int existingId = 0;

                    string existingClient = "", existingName = "";

                    using (var existingReader = checkCmd.ExecuteReader())

                    {
                        if (existingReader.Read())

                        {

                            existingId = existingReader.GetInt32(0);

                            existingClient = existingReader.GetString(1).Trim();

                            existingName = existingReader.IsDBNull(2) ? "" : existingReader.GetString(2);

                        }

                    }

                    if (existingId > 0)

                    {

                        string thisClient = (r.ClientCompanyName ?? "").Trim();

                        if (!string.IsNullOrEmpty(existingClient) && !string.IsNullOrEmpty(thisClient) && !existingClient.Equals(thisClient, StringComparison.OrdinalIgnoreCase))

                        {

                            throw new InvalidOperationException($"Route code '{r.RouteCode}' is already used by '{existingName}' ({existingClient}). Please use a different route code.");

                        }

                        r.Id = existingId;

                    }
                }

                if (r.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt)

                                        VALUES (@client, @c, @n, @d, @p, @a, datetime('now'), datetime('now'));

                                        SELECT last_insert_rowid();";

                }

                else

                {

                    cmd.CommandText = @"UPDATE DeliveryRoutes 

                                        SET ClientCompanyName = @client, RouteCode = @c, RouteName = @n, Description = @d, ConnectedPlaces = @p, IsActive = @a, UpdatedAt = datetime('now')

                                        WHERE Id = @id;";

                    cmd.Parameters.AddWithValue("@id", r.Id);

                }

                cmd.Parameters.AddWithValue("@client", r.ClientCompanyName ?? "");

                cmd.Parameters.AddWithValue("@c", r.RouteCode);

                cmd.Parameters.AddWithValue("@n", r.RouteName);

                cmd.Parameters.AddWithValue("@d", r.Description ?? "");

                cmd.Parameters.AddWithValue("@p", r.ConnectedPlaces);

                cmd.Parameters.AddWithValue("@a", r.IsActive ? 1 : 0);

                if (r.Id == 0)

                {

                    r.Id = Convert.ToInt32(cmd.ExecuteScalar());

                }

                else

                {

                    cmd.ExecuteNonQuery();

                }

                return true;

            }

        }

        public static bool DeleteDeliveryRoute(int id)

        {

            // Routes are referenced by past orders, so "delete" deactivates them instead.

            bool ok = ToggleDeliveryRouteActive(id, false);

            if (ok) LogAuditTrail("DEACTIVATE", "DeliveryRoutes", id, $"Route #{id}", "IsActive", "1", "0", "Route deactivated instead of deleted (history is kept)");

            return ok;

        }

        public static bool ToggleDeliveryRouteActive(int id, bool isActive)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE DeliveryRoutes SET IsActive = @a, UpdatedAt = datetime('now') WHERE Id = @id";

                cmd.Parameters.AddWithValue("@a", isActive ? 1 : 0);

                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;

            }

        }

        public static string GenerateNextRouteCode()

        {

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM DeliveryRoutes";

                int nextId = Convert.ToInt32(cmd.ExecuteScalar() ?? 0) + 1;

                using var existsCmd = conn.CreateCommand();

                existsCmd.CommandText = "SELECT COUNT(*) FROM DeliveryRoutes WHERE UPPER(RouteCode) = UPPER(@c)";

                var p = existsCmd.Parameters.Add("@c", SqliteType.Text);

                while (true)

                {

                    p.Value = $"R{nextId}";

                    if (Convert.ToInt64(existsCmd.ExecuteScalar() ?? 0) == 0) break;

                    nextId++;

                }

                return $"R{nextId}";

            }

            catch

            {

                return $"R{DateTime.Now:fff}";

            }

        }

        public static DeliveryRoute? FindRouteByNameOrCode(string text)

        {

            if (string.IsNullOrWhiteSpace(text)) return null;

            return FindRouteByNameOrCode(text, GetDeliveryRoutes());

        }

        private static bool StartsWithWhole(string text, string prefix)

        {

            if (string.IsNullOrEmpty(prefix) || !text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

            if (text.Length == prefix.Length) return true;

            return !char.IsLetterOrDigit(text[prefix.Length]);

        }

        public static DeliveryRoute? FindRouteByNameOrCode(string text, List<DeliveryRoute> routes)

        {

            if (string.IsNullOrWhiteSpace(text) || routes == null || routes.Count == 0) return null;

            string clean = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"^🛣️\s*", "");

            // 1) Exact code / name matches first.

            var exact = routes.FirstOrDefault(r =>

                string.Equals(r.RouteCode, clean, StringComparison.OrdinalIgnoreCase) ||

                string.Equals(r.RouteName, clean, StringComparison.OrdinalIgnoreCase));

            if (exact != null) return exact;

            // 2) "R3" style shorthand.

            if (clean.Length >= 2 && clean.StartsWith("r", StringComparison.OrdinalIgnoreCase) && int.TryParse(clean.Substring(1), out int num))

            {

                var byNum = routes.FirstOrDefault(r => r.RouteCode.Equals($"R{num}", StringComparison.OrdinalIgnoreCase) || r.RouteName.Equals($"Route {num}", StringComparison.OrdinalIgnoreCase));

                if (byNum != null) return byNum;

            }

            // 3) Text that starts with a full code or name followed by a separator ("R1 - ...", "EODLAB (Ilocos Route): ...").

            var prefixed = routes

                .Where(r => StartsWithWhole(clean, r.RouteCode) || StartsWithWhole(clean, r.RouteName) ||

                            clean.IndexOf($"[{r.RouteName}]", StringComparison.OrdinalIgnoreCase) >= 0 ||

                            clean.IndexOf($"[{r.RouteCode}]", StringComparison.OrdinalIgnoreCase) >= 0)

                .OrderByDescending(r => Math.Max(r.RouteName.Length, r.RouteCode.Length))

                .FirstOrDefault();

            if (prefixed != null) return prefixed;

            // 4) Distinctive first word of the route name (e.g. "EODLAB"), matched as a whole word. Generic words are ignored.

            var generic = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Route", "Run", "Trip", "Delivery", "Main", "North", "South" };

            var matches = routes.Where(r =>

            {

                string shortName = r.RouteName.Split(new[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

                if (shortName.Length < 3 || generic.Contains(shortName)) return false;

                return System.Text.RegularExpressions.Regex.IsMatch(clean, $@"(^|[^A-Za-z0-9]){System.Text.RegularExpressions.Regex.Escape(shortName)}($|[^A-Za-z0-9])", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            }).ToList();

            return matches.Count == 1 ? matches[0] : null;

        }

        public static List<LoadingSchedule> GetLoadingSchedules()

        {

            UpdatePendingDeliveriesToTransit();

            UpdatePastDueLoadingSchedules();

            var list = new List<LoadingSchedule>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, ScheduleNumber, ClientName, Destination, ScheduleDate, DepartureTime, GCrate, ECrate, TotalCrates, IsPeak, Status, Remarks, CancellationReason, RescheduleCount, OriginalScheduleDate, RescheduleReason, CargoType FROM LoadingSchedules ORDER BY CASE WHEN Status IN ('Scheduled', 'Pending') THEN 1 WHEN Status IN ('Assigned', 'Loading') THEN 2 WHEN Status IN ('Dispatched', 'Completed') THEN 3 WHEN Status = 'Cancelled' THEN 4 ELSE 5 END ASC, ScheduleDate DESC, Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime sDate = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Today;

                        int gCrate = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                        int eCrate = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);

                        int totalCrates = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);

                        if (totalCrates == 0 && (gCrate > 0 || eCrate > 0)) totalCrates = gCrate + eCrate;

                        if (gCrate == 0 && eCrate == 0 && totalCrates > 0) gCrate = totalCrates;

                        int reschedCount = reader.FieldCount > 13 && !reader.IsDBNull(13) ? reader.GetInt32(13) : 0;

                        DateTime? origDate = null;

                        if (reader.FieldCount > 14 && !reader.IsDBNull(14))

                        {

                            if (DateTime.TryParse(reader.GetString(14), out var od)) origDate = od;

                        }

                        string reschedReason = reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetString(15) : "";

                        string cargoType = reader.FieldCount > 16 && !reader.IsDBNull(16) ? reader.GetString(16) : "Cold / Chilled";

                        list.Add(new LoadingSchedule

                        {

                            Id = reader.GetInt32(0),

                            ScheduleNumber = reader.GetString(1),

                            ClientName = reader.GetString(2),

                            Destination = reader.GetString(3),

                            ScheduleDate = sDate,

                            DepartureTime = reader.IsDBNull(5) ? "08:00 AM" : reader.GetString(5),

                            GCrate = gCrate,

                            ECrate = eCrate,

                            TotalCrates = totalCrates,

                            IsPeak = !reader.IsDBNull(9) && reader.GetInt32(9) == 1,

                            Status = reader.IsDBNull(10) ? "Pending" : reader.GetString(10),

                            Remarks = reader.IsDBNull(11) ? "" : reader.GetString(11),

                            CancellationReason = reader.IsDBNull(12) ? "" : reader.GetString(12),

                            RescheduleCount = reschedCount,

                            OriginalScheduleDate = origDate,

                            RescheduleReason = reschedReason,

                            CargoType = cargoType

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<LoadingSchedule>> GetLoadingSchedulesAsync()

        {

            return await Task.Run(() => GetLoadingSchedules());

        }

        public static void SaveLoadingSchedule(LoadingSchedule ls)

        {

            try { EnsureClientExists(ls.ClientName); } catch { }

            bool wasNew = ls.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                ls.TotalCrates = ls.GCrate + ls.ECrate;

                if (ls.Id == 0)

                {

                    if (string.IsNullOrWhiteSpace(ls.ScheduleNumber))

                    {

                        ls.ScheduleNumber = $"LS-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";

                    }

                    cmd.CommandText = @"INSERT INTO LoadingSchedules (ScheduleNumber, ClientName, Destination, ScheduleDate, DepartureTime, GCrate, ECrate, TotalCrates, IsPeak, Status, Remarks, CancellationReason, RescheduleCount, OriginalScheduleDate, RescheduleReason, CargoType)

                                        VALUES (@sn, @c, @d, @sd, @dt, @gc, @ec, @tc, @ip, @s, @r, @cr, @rc, @osd, @rr, @cargo)";

                }

                else

                {

                    cmd.CommandText = @"UPDATE LoadingSchedules SET ScheduleNumber=@sn, ClientName=@c, Destination=@d, ScheduleDate=@sd, DepartureTime=@dt, GCrate=@gc, ECrate=@ec, TotalCrates=@tc, IsPeak=@ip, Status=@s, Remarks=@r, CancellationReason=@cr, RescheduleCount=@rc, OriginalScheduleDate=@osd, RescheduleReason=@rr, CargoType=@cargo WHERE Id=@id";

                    cmd.Parameters.AddWithValue("@id", ls.Id);

                }

                cmd.Parameters.AddWithValue("@sn", ls.ScheduleNumber);

                cmd.Parameters.AddWithValue("@c", ls.ClientName);

                cmd.Parameters.AddWithValue("@d", ls.Destination);

                cmd.Parameters.AddWithValue("@sd", ls.ScheduleDate.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("@dt", ls.DepartureTime ?? "08:00 AM");

                cmd.Parameters.AddWithValue("@gc", ls.GCrate);

                cmd.Parameters.AddWithValue("@ec", ls.ECrate);

                cmd.Parameters.AddWithValue("@tc", ls.TotalCrates);

                cmd.Parameters.AddWithValue("@ip", ls.IsPeak ? 1 : 0);

                cmd.Parameters.AddWithValue("@s", ls.Status);

                cmd.Parameters.AddWithValue("@r", ls.Remarks);

                cmd.Parameters.AddWithValue("@cr", ls.CancellationReason ?? "");

                cmd.Parameters.AddWithValue("@rc", ls.RescheduleCount);

                cmd.Parameters.AddWithValue("@osd", ls.OriginalScheduleDate.HasValue ? (object)ls.OriginalScheduleDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);

                cmd.Parameters.AddWithValue("@rr", ls.RescheduleReason ?? "");

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@cargo", string.IsNullOrEmpty(ls.CargoType) ? "Cold / Chilled" : ls.CargoType);

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    ls.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : (ls.Status == "Cancelled" ? "CANCEL" : "UPDATE"), "LoadingSchedules", ls.Id, ls.ScheduleNumber, "Status", "", ls.Status, $"Loading schedule #{ls.ScheduleNumber} for {ls.ClientName} ({ls.TotalCrates} crates) {(isNew ? "created" : "updated")}, Status: {ls.Status}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "Loading Order Created" : "Loading Order Updated", $"Order #{ls.ScheduleNumber} for {ls.ClientName} ({ls.TotalCrates} crates)", ToastType.Success);

            }

            catch { }

        }

        public static (bool Success, string Message) RescheduleLoadingSchedule(int scheduleId, DateTime newDate, string newTime, string reason, string operatorUsername)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var trans = conn.BeginTransaction())

                {

                    try

                    {

                        var checkCmd = conn.CreateCommand();

                        checkCmd.Transaction = trans;

                        checkCmd.CommandText = "SELECT Id, ScheduleNumber, ClientName, ScheduleDate, DepartureTime, Status, RescheduleCount, OriginalScheduleDate FROM LoadingSchedules WHERE Id = @id";

                        checkCmd.Parameters.AddWithValue("@id", scheduleId);

                        using (var reader = checkCmd.ExecuteReader())

                        {

                            if (!reader.Read())

                            {

                                return (false, "Loading schedule order was not found.");

                            }

                            string schedNum = reader.GetString(1);

                            string clientName = reader.GetString(2);

                            string oldDateStr = reader.GetString(3);

                            string oldTimeStr = reader.IsDBNull(4) ? "08:00 AM" : reader.GetString(4);

                            string status = reader.IsDBNull(5) ? "Pending" : reader.GetString(5);

                            int curCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                            string origDateStr = reader.IsDBNull(7) ? "" : reader.GetString(7);

                            if (status.Equals("In Transit", StringComparison.OrdinalIgnoreCase) ||

                                status.Equals("Completed", StringComparison.OrdinalIgnoreCase))

                            {

                                return (false, $"Cannot reschedule loading order in '{status}' status.");

                            }

                            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                            {

                                return (false, "Cannot reschedule a cancelled loading order.");

                            }

                            reader.Close();

                            var delCheckCmd = conn.CreateCommand();

                            delCheckCmd.Transaction = trans;

                            delCheckCmd.CommandText = "SELECT DispatchNumber, Status FROM Deliveries WHERE LoadingScheduleId = @lsid AND Status <> 'Cancelled'";

                            delCheckCmd.Parameters.AddWithValue("@lsid", scheduleId);

                            using (var delReader = delCheckCmd.ExecuteReader())

                            {

                                if (delReader.Read())

                                {

                                    string delStatus = delReader.GetString(1);

                                    if (delStatus.Equals("In Transit", StringComparison.OrdinalIgnoreCase) ||

                                        delStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))

                                    {

                                        return (false, $"Linked delivery dispatch {delReader.GetString(0)} is '{delStatus}'. Cannot reschedule.");

                                    }

                                }

                            }

                            string effectiveOrigDate = string.IsNullOrEmpty(origDateStr) ? oldDateStr : origDateStr;

                            int newCount = curCount + 1;

                            var updateCmd = conn.CreateCommand();

                            updateCmd.Transaction = trans;

                            updateCmd.CommandText = @"UPDATE LoadingSchedules

                                                      SET ScheduleDate = @sd,

                                                          DepartureTime = @dt,

                                                          Status = 'Rescheduled',

                                                          RescheduleCount = @rc,

                                                          OriginalScheduleDate = @osd,

                                                          RescheduleReason = @rr

                                                      WHERE Id = @id";

                            updateCmd.Parameters.AddWithValue("@id", scheduleId);

                            updateCmd.Parameters.AddWithValue("@sd", newDate.ToString("yyyy-MM-dd"));

                            updateCmd.Parameters.AddWithValue("@dt", string.IsNullOrWhiteSpace(newTime) ? oldTimeStr : newTime);

                            updateCmd.Parameters.AddWithValue("@rc", newCount);

                            updateCmd.Parameters.AddWithValue("@osd", effectiveOrigDate);

                            updateCmd.Parameters.AddWithValue("@rr", reason ?? "");

                            updateCmd.ExecuteNonQuery();

                            var updateDelCmd = conn.CreateCommand();

                            updateDelCmd.Transaction = trans;

                            updateDelCmd.CommandText = @"UPDATE Deliveries

                                                         SET DispatchDate = @dd,

                                                             DepartureTime = @dt

                                                         WHERE LoadingScheduleId = @lsid AND Status <> 'Cancelled'";

                            updateDelCmd.Parameters.AddWithValue("@lsid", scheduleId);

                            updateDelCmd.Parameters.AddWithValue("@dd", newDate.ToString("yyyy-MM-dd"));

                            updateDelCmd.Parameters.AddWithValue("@dt", string.IsNullOrWhiteSpace(newTime) ? oldTimeStr : newTime);

                            updateDelCmd.ExecuteNonQuery();

                            trans.Commit();

                            LogAuditTrail("RESCHEDULE", "LoadingSchedules", scheduleId, schedNum, "ScheduleDate", $"{oldDateStr} {oldTimeStr}", $"{newDate:yyyy-MM-dd} {newTime}", reason);

                            try

                            {

                                EventNotificationService.Publish("Loading Rescheduled", $"Order #{schedNum} for {clientName} moved to {newDate:MMM dd, yyyy} {newTime} ({reason})", ToastType.Warning, null, "Operations", "Loading", schedNum);

                            }

                            catch { }

                            return (true, $"Order #{schedNum} successfully rescheduled to {newDate:MMM dd, yyyy} {newTime}.");

                        }

                    }

                    catch (Exception ex)

                    {

                        trans.Rollback();

                        return (false, $"Reschedule failed: {ex.Message}");

                    }

                }




            }

        }

        public static (bool Success, string Message) RescheduleDelivery(int deliveryId, DateTime newDate, string newTime, string reason, string operatorUsername, int? newTruckId = null, string? newTruckInfo = null, int? newDriverId = null, string? newDriverName = null, int? newAideId = null, string? newAideName = null)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var trans = conn.BeginTransaction())

                {

                    try

                    {

                        string dspNum = "";

                        string clientName = "";

                        string status = "";

                        string oldDateStr = "";

                        string oldTimeStr = "";

                        string oldRemarks = "";

                        int linkedScheduleId = 0;

                        int curTruckId = 0, curDriverId = 0, curAideId = 0;

                        string curAideIds = "", curAideNames = "";

                        using (var checkCmd = conn.CreateCommand())

                        {

                            checkCmd.Transaction = trans;

                            checkCmd.CommandText = "SELECT Id, DispatchNumber, ClientName, DispatchDate, DepartureTime, Status, Remarks, LoadingScheduleId, TruckId, DriverId, AideId, AideIds, AideNames FROM Deliveries WHERE Id = @id";

                            checkCmd.Parameters.AddWithValue("@id", deliveryId);

                            using (var reader = checkCmd.ExecuteReader())

                            {

                                if (!reader.Read())

                                {

                                    return (false, "Dispatch order record not found.");

                                }

                                dspNum = reader.IsDBNull(1) ? "" : reader.GetString(1);

                                clientName = reader.IsDBNull(2) ? "" : reader.GetString(2);

                                oldDateStr = reader.IsDBNull(3) ? "" : reader.GetString(3);

                                oldTimeStr = reader.IsDBNull(4) ? "" : reader.GetString(4);

                                status = reader.IsDBNull(5) ? "" : reader.GetString(5);

                                oldRemarks = reader.IsDBNull(6) ? "" : reader.GetString(6);

                                linkedScheduleId = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);

                                curTruckId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);

                                curDriverId = reader.IsDBNull(9) ? 0 : reader.GetInt32(9);

                                curAideId = reader.IsDBNull(10) ? 0 : reader.GetInt32(10);

                                curAideIds = reader.IsDBNull(11) ? "" : reader.GetString(11);

                                curAideNames = reader.IsDBNull(12) ? "" : reader.GetString(12);

                            }

                        }

                        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))

                        {

                            return (false, "Cannot reschedule a completed delivery dispatch.");

                        }

                        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                        {

                            return (false, "Cannot reschedule a cancelled delivery dispatch.");

                        }

                        int effTruck = newTruckId.HasValue && newTruckId.Value > 0 ? newTruckId.Value : curTruckId;

                        int effDriver = newDriverId.HasValue && newDriverId.Value > 0 ? newDriverId.Value : curDriverId;

                        var aideIdList = ParseIdList(curAideIds).ToList();

                        if (aideIdList.Count == 0 && curAideId > 0) aideIdList.Add(curAideId);

                        var aideNameList = (curAideNames ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

                        if (newAideId.HasValue && newAideId.Value > 0 && newAideId.Value != curAideId)

                        {

                            int idx = curAideId > 0 ? aideIdList.IndexOf(curAideId) : -1;

                            if (idx >= 0)

                            {

                                aideIdList[idx] = newAideId.Value;

                                if (idx < aideNameList.Count) aideNameList[idx] = newAideName ?? "";

                            }

                            else

                            {

                                aideIdList.Insert(0, newAideId.Value);

                                aideNameList.Insert(0, newAideName ?? "");

                            }

                        }

                        string? conflict = ValidateResourceConflict(deliveryId, 0, effTruck, effDriver, aideIdList, newDate);

                        if (!string.IsNullOrEmpty(conflict))

                        {

                            return (false, conflict);

                        }

                        string updatedRemarks = string.IsNullOrWhiteSpace(oldRemarks)

                            ? $"Rescheduled to {newDate:yyyy-MM-dd} {newTime} ({reason})"

                            : $"{oldRemarks} | Rescheduled to {newDate:yyyy-MM-dd} {newTime} ({reason})";

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = trans;
                            var setClauses = new List<string>
                            {
                                "DispatchDate = @dd",
                                "DepartureTime = @dt",
                                "Status = 'Rescheduled'",
                                "Remarks = @rem"
                            };
                            updateCmd.Parameters.AddWithValue("@id", deliveryId);
                            updateCmd.Parameters.AddWithValue("@dd", newDate.ToString("yyyy-MM-dd"));
                            updateCmd.Parameters.AddWithValue("@dt", newTime);
                            updateCmd.Parameters.AddWithValue("@rem", updatedRemarks);

                            if (newTruckId.HasValue && newTruckId.Value > 0)
                            {
                                setClauses.Add("TruckId = @trId");
                                setClauses.Add("TruckInfo = @trInfo");
                                updateCmd.Parameters.AddWithValue("@trId", newTruckId.Value);
                                updateCmd.Parameters.AddWithValue("@trInfo", newTruckInfo ?? "");
                            }
                            if (newDriverId.HasValue && newDriverId.Value > 0)
                            {
                                setClauses.Add("DriverId = @drId");
                                setClauses.Add("DriverName = @drName");
                                updateCmd.Parameters.AddWithValue("@drId", newDriverId.Value);
                                updateCmd.Parameters.AddWithValue("@drName", newDriverName ?? "");
                            }
                            if (newAideId.HasValue && newAideId.Value > 0)
                            {
                                setClauses.Add("AideId = @aiId");
                                setClauses.Add("AideName = @aiName");
                                setClauses.Add("AideIds = @aiIds");

                                setClauses.Add("AideNames = @aiNames");

                                updateCmd.Parameters.AddWithValue("@aiId", newAideId.Value);
                                updateCmd.Parameters.AddWithValue("@aiName", newAideName ?? "");
                                updateCmd.Parameters.AddWithValue("@aiIds", string.Join(",", aideIdList));

                                updateCmd.Parameters.AddWithValue("@aiNames", string.Join(", ", aideNameList));

                            }

                            updateCmd.CommandText = $"UPDATE Deliveries SET {string.Join(", ", setClauses)} WHERE Id = @id";
                            updateCmd.ExecuteNonQuery();
                        }

                        if (linkedScheduleId > 0)

                        {

                            using (var updateSchedCmd = conn.CreateCommand())

                            {

                                updateSchedCmd.Transaction = trans;

                                updateSchedCmd.CommandText = @"UPDATE LoadingSchedules SET

                                                              ScheduleDate = @sd,

                                                              DepartureTime = @dt,

                                                              Status = 'Rescheduled',

                                                              RescheduleCount = RescheduleCount + 1,

                                                              RescheduleReason = @rr

                                                              WHERE Id = @sid";

                                updateSchedCmd.Parameters.AddWithValue("@sid", linkedScheduleId);

                                updateSchedCmd.Parameters.AddWithValue("@sd", newDate.ToString("yyyy-MM-dd"));

                                updateSchedCmd.Parameters.AddWithValue("@dt", newTime);

                                updateSchedCmd.Parameters.AddWithValue("@rr", reason);

                                updateSchedCmd.ExecuteNonQuery();

                            }

                        }

                        ReconcileResourceStatuses(conn, trans);

                        trans.Commit();

                        LogAuditTrail("RESCHEDULE", "Deliveries", deliveryId, dspNum, "DispatchDate", $"{oldDateStr} {oldTimeStr}", $"{newDate:yyyy-MM-dd} {newTime}", reason);

                        try

                        {

                            EventNotificationService.Publish("Dispatch Rescheduled", $"Dispatch #{dspNum} for {clientName} moved to {newDate:MMM dd, yyyy} {newTime} ({reason})", ToastType.Warning, null, "Operations", "Dispatch", dspNum);

                        }

                        catch { }

                        return (true, $"Dispatch #{dspNum} successfully rescheduled to {newDate:MMM dd, yyyy} {newTime}.");

                    }

                    catch (Exception ex)

                    {

                        trans.Rollback();

                        return (false, $"Reschedule failed: {ex.Message}");

                    }

                }

            }

        }

        public static (bool Success, string Message) RescheduleSpecialTrip(int tripId, DateTime newDate, string newTime, string reason, string operatorUsername, int? newTruckId = null, string? newTruckPlate = null, int? newDriverId = null, string? newDriverName = null, int? newAideId = null, string? newAideName = null)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var trip = GetSpecialTripById(tripId);
                        if (trip == null) return (false, "Special Trip record not found.");
                        if (trip.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            return (false, "Cannot reschedule a completed special trip.");
                        if (trip.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            return (false, "Cannot reschedule a cancelled special trip.");

                        int stTruck = newTruckId.HasValue && newTruckId.Value > 0 ? newTruckId.Value : trip.TruckId;

                        int stDriver = newDriverId.HasValue && newDriverId.Value > 0 ? newDriverId.Value : trip.DriverId;

                        int stAide = newAideId.HasValue && newAideId.Value > 0 ? newAideId.Value : trip.AideId;

                        string? stConflict = ValidateResourceConflict(0, tripId, stTruck, stDriver, stAide > 0 ? new List<int> { stAide } : new List<int>(), newDate);

                        if (!string.IsNullOrEmpty(stConflict))

                        {

                            return (false, stConflict);

                        }

                        string updatedRemarks = string.IsNullOrWhiteSpace(trip.Remarks)
                            ? $"Rescheduled to {newDate:yyyy-MM-dd} {newTime} ({reason})"
                            : $"{trip.Remarks} | Rescheduled to {newDate:yyyy-MM-dd} {newTime} ({reason})";

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = trans;
                            var setClauses = new List<string>
                            {
                                "DispatchDate = @dd",
                                "DepartureTime = @dt",
                                "Status = 'Rescheduled'",
                                "Remarks = @rem"
                            };
                            updateCmd.Parameters.AddWithValue("@id", tripId);
                            updateCmd.Parameters.AddWithValue("@dd", newDate.ToString("yyyy-MM-dd HH:mm:ss"));
                            updateCmd.Parameters.AddWithValue("@dt", newTime);
                            updateCmd.Parameters.AddWithValue("@rem", updatedRemarks);

                            if (newTruckId.HasValue && newTruckId.Value > 0)
                            {
                                setClauses.Add("TruckId = @trId");
                                setClauses.Add("TruckPlate = @trPlate");
                                updateCmd.Parameters.AddWithValue("@trId", newTruckId.Value);
                                updateCmd.Parameters.AddWithValue("@trPlate", newTruckPlate ?? "");
                            }
                            if (newDriverId.HasValue && newDriverId.Value > 0)
                            {
                                setClauses.Add("DriverId = @drId");
                                setClauses.Add("DriverName = @drName");
                                updateCmd.Parameters.AddWithValue("@drId", newDriverId.Value);
                                updateCmd.Parameters.AddWithValue("@drName", newDriverName ?? "");
                            }
                            if (newAideId.HasValue && newAideId.Value > 0)
                            {
                                setClauses.Add("AideId = @aiId");
                                setClauses.Add("AideName = @aiName");
                                updateCmd.Parameters.AddWithValue("@aiId", newAideId.Value);
                                updateCmd.Parameters.AddWithValue("@aiName", newAideName ?? "");
                            }

                            updateCmd.CommandText = $"UPDATE SpecialTrips SET {string.Join(", ", setClauses)} WHERE Id = @id";
                            updateCmd.ExecuteNonQuery();
                        }

                        ReconcileResourceStatuses(conn, trans);

                        trans.Commit();
                        LogAuditTrail("RESCHEDULE", "SpecialTrips", tripId, trip.OrderReference, "DispatchDate", $"{trip.DispatchDate:yyyy-MM-dd} {trip.DepartureTime}", $"{newDate:yyyy-MM-dd} {newTime}", reason);
                        try
                        {
                            EventNotificationService.Publish("Special Trip Rescheduled", $"Special Trip '{trip.OrderReference}' for {trip.Company} moved to {newDate:MMM dd, yyyy} {newTime} ({reason})", ToastType.Warning, null, "Operations", "SpecialTrip", trip.OrderReference);
                        }
                        catch { }
                        return (true, $"Special Trip '{trip.OrderReference}' has been successfully rescheduled to {newDate:MMM dd, yyyy} at {newTime}.");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return (false, $"Failed to reschedule special trip: {ex.Message}");
                    }
                }
            }
        }

        public static DateTime CombineDateAndTime(DateTime date, string? timeStr)

        {

            if (string.IsNullOrWhiteSpace(timeStr))

            {

                return date.Date;

            }

            if (DateTime.TryParse(timeStr.Trim(), out var parsedTime))

            {

                return date.Date.Add(parsedTime.TimeOfDay);

            }

            if (DateTime.TryParseExact(timeStr.Trim(), new[] { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm", "h:mmtt", "hh:mmtt", "h:mm:ss tt" },

                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var exactTime))

            {

                return date.Date.Add(exactTime.TimeOfDay);

            }

            return date.Date;

        }

        private static DateTime lastResourceReconcile = DateTime.MinValue;

        private static readonly object reconcileLock = new object();

        private static DateTime lastTransitCheck = DateTime.MinValue;

        private static bool transitCheckRunning;

        /// <summary>The moment a dispatch is due to leave: its dispatch date plus its departure time (8:00 AM if the time can't be read).</summary>

        public static DateTime DepartureDateTime(DateTime date, string? time)

        {

            if (DateTime.TryParse(time, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t) || DateTime.TryParse(time, out t))

                return date.Date + t.TimeOfDay;

            return date.Date.AddHours(8);

        }

        public static DateTime DepartureDateTime(Delivery d) => DepartureDateTime(d.DispatchDate, d.DepartureTime);

        /// <summary>

        /// Once a scheduled dispatch reaches its date and departure time it is on the road: the status moves to

        /// In Transit automatically (and the truck and crew are locked to it). Runs every 30 seconds from the main

        /// window and whenever a list loads; it never moves an order back.

        /// </summary>

        public static int UpdatePendingDeliveriesToTransit()

        {

            int moved = 0;

            bool run;

            lock (reconcileLock)

            {

                run = !transitCheckRunning && (DateTime.Now - lastTransitCheck).TotalSeconds >= 10;

                if (run) { transitCheckRunning = true; lastTransitCheck = DateTime.Now; }

            }

            if (run)

            {

                try

                {

                    var due = new List<(int Id, string Number, string Prev, DateTime Leaves)>();

                    using (var conn = new SqliteConnection(connectionString))

                    {

                        conn.Open();

                        using (var cmd = conn.CreateCommand())

                        {

                            cmd.CommandText = "SELECT Id, DispatchNumber, DispatchDate, DepartureTime, Status FROM Deliveries WHERE Status IN ('Scheduled', 'Pending', 'Rescheduled', 'Dispatched')";

                            using var r = cmd.ExecuteReader();

                            var now = DateTime.Now;

                            while (r.Read())

                            {

                                if (!DateTime.TryParse(r.IsDBNull(2) ? "" : r.GetValue(2).ToString(), out var date)) continue;

                                var leaves = DepartureDateTime(date, r.IsDBNull(3) ? "" : r.GetString(3));

                                if (leaves <= now) due.Add((r.GetInt32(0), r.IsDBNull(1) ? "" : r.GetString(1), r.IsDBNull(4) ? "" : r.GetString(4), leaves));

                            }

                        }

                        if (due.Count > 0)

                        {

                            using var tx = conn.BeginTransaction();

                            foreach (var d in due)

                            {

                                using var u = conn.CreateCommand();

                                u.Transaction = tx;

                                u.CommandText = "UPDATE Deliveries SET Status = 'In Transit' WHERE Id = @id AND Status IN ('Scheduled', 'Pending', 'Rescheduled', 'Dispatched')";

                                u.Parameters.AddWithValue("@id", d.Id);

                                moved += u.ExecuteNonQuery();

                            }

                            tx.Commit();

                        }

                    }

                    foreach (var d in due)

                        LogAuditTrail("AUTO_TRANSIT", "Deliveries", d.Id, d.Number, "Status", d.Prev, "In Transit", $"Departure time reached ({d.Leaves:MMM d, yyyy h:mm tt}): dispatch is now on the road");

                }

                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] auto in-transit: {ex.Message}"); }

                finally { lock (reconcileLock) transitCheckRunning = false; }

            }

            // Lock trucks/crew for trips on the road (and release finished ones). Throttled: called from several list loaders.

            bool reconcile;

            lock (reconcileLock)

            {

                reconcile = moved > 0 || (DateTime.Now - lastResourceReconcile).TotalSeconds >= 60;

                if (reconcile) lastResourceReconcile = DateTime.Now;

            }

            if (reconcile) { try { ReconcileResourceStatuses(); } catch { } }

            return moved;

        }

        /// <summary>The truck left before its set time: put the dispatch In Transit now.</summary>

        public static (bool ok, string message) StartTripNow(int deliveryId, string? user = null)

        {

            var d = GetDeliveryById(deliveryId);

            if (d == null) return (false, "This dispatch order no longer exists.");

            if (!(d.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Rescheduled", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase)))

                return (false, $"Dispatch #{d.DispatchNumber} is already {d.Status.ToLowerInvariant()}.");

            if (d.DispatchDate.Date > DateTime.Today)

                return (false, $"Dispatch #{d.DispatchNumber} is scheduled for {d.DispatchDate:MMMM d, yyyy}. Reschedule it to today first if it is leaving now.");

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var u = conn.CreateCommand();

                u.CommandText = "UPDATE Deliveries SET Status = 'In Transit' WHERE Id = @id";

                u.Parameters.AddWithValue("@id", d.Id);

                u.ExecuteNonQuery();

            }

            LogAuditTrail("UPDATE", "Deliveries", d.Id, d.DispatchNumber, "Status", d.Status, "In Transit", $"Trip started early by {user ?? "user"} (set departure {DepartureDateTime(d):h:mm tt})");

            lock (reconcileLock) lastResourceReconcile = DateTime.MinValue;

            try { ReconcileResourceStatuses(); } catch { }

            return (true, $"Dispatch #{d.DispatchNumber} is now in transit.");

        }

        public static int UpdateOverdueInvoices()

        {

            int updatedCount = 0;

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "SELECT Id, InvoiceNumber, DueDate, Status FROM Invoices WHERE Status IN ('Unpaid', 'Partial')";

                    var toUpdate = new List<(int Id, string InvNum, string PrevStatus, DateTime DueDate)>();

                    using (var reader = cmd.ExecuteReader())

                    {

                        DateTime today = DateTime.Today;

                        while (reader.Read())

                        {

                            int id = reader.GetInt32(0);

                            string invNum = reader.IsDBNull(1) ? "" : reader.GetString(1);

                            DateTime dueDate = DateTime.TryParse(reader.GetString(2), out var dt) ? dt : today;

                            string prevStatus = reader.IsDBNull(3) ? "Unpaid" : reader.GetString(3);

                            if (dueDate.Date < today)

                            {

                                toUpdate.Add((id, invNum, prevStatus, dueDate));

                            }

                        }

                    }

                    if (toUpdate.Count > 0)

                    {

                        using (var tx = conn.BeginTransaction())

                        {

                            foreach (var item in toUpdate)

                            {

                                var uCmd = conn.CreateCommand();

                                uCmd.Transaction = tx;

                                uCmd.CommandText = "UPDATE Invoices SET Status = 'Overdue' WHERE Id = @id AND Status IN ('Unpaid', 'Partial')";

                                uCmd.Parameters.AddWithValue("@id", item.Id);

                                int rows = uCmd.ExecuteNonQuery();

                                if (rows > 0) updatedCount++;

                            }

                            tx.Commit();

                        }

                        foreach (var item in toUpdate)

                        {

                            LogAuditTrail("AUTO_OVERDUE", "Invoices", item.Id, item.InvNum, "Status", item.PrevStatus, "Overdue", $"Invoice credit term expired on {item.DueDate:yyyy-MM-dd}: Automatic transition to Overdue");

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] UpdateOverdueInvoices: {ex.Message}");

            }

            return updatedCount;

        }

        public static int UpdatePastDueLoadingSchedules()

        {

            int updatedCount = 0;

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = $"SELECT Id, ScheduleNumber, ClientName, Destination, ScheduleDate, Status FROM LoadingSchedules WHERE Status IN ('Pending', 'Rescheduled') AND Id NOT IN (SELECT LoadingScheduleId FROM Deliveries WHERE LoadingScheduleId > 0 AND Status IN {ActiveDeliveryStatusesSql})";

                    var toUpdate = new List<(int Id, string SchedNum, string ClientName, string Dest, DateTime SchedDate)>();

                    using (var reader = cmd.ExecuteReader())

                    {

                        DateTime today = DateTime.Today;

                        while (reader.Read())

                        {

                            int id = reader.GetInt32(0);

                            string schedNum = reader.IsDBNull(1) ? "" : reader.GetString(1);

                            string clientName = reader.IsDBNull(2) ? "" : reader.GetString(2);

                            string dest = reader.IsDBNull(3) ? "" : reader.GetString(3);

                            DateTime schedDate = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : today;

                            if (schedDate.Date < today)

                            {

                                toUpdate.Add((id, schedNum, clientName, dest, schedDate));

                            }

                        }

                    }

                    if (toUpdate.Count > 0)

                    {

                        using (var tx = conn.BeginTransaction())

                        {

                            foreach (var item in toUpdate)

                            {

                                var uCmd = conn.CreateCommand();

                                uCmd.Transaction = tx;

                                uCmd.CommandText = "UPDATE LoadingSchedules SET Status = 'Past Due' WHERE Id = @id AND Status IN ('Pending', 'Rescheduled')";

                                uCmd.Parameters.AddWithValue("@id", item.Id);

                                int rows = uCmd.ExecuteNonQuery();

                                if (rows > 0) updatedCount++;

                            }

                            tx.Commit();

                        }

                        foreach (var item in toUpdate)

                        {

                            LogAuditTrail("AUTO_PAST_DUE", "LoadingSchedules", item.Id, item.SchedNum, "Status", "Pending/Rescheduled", "Past Due", $"Scheduled date ({item.SchedDate:yyyy-MM-dd}) passed without dispatch assignment: Flagged as Past Due");

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] UpdatePastDueLoadingSchedules: {ex.Message}");

            }

            return updatedCount;

        }

        public static int UpdateDelayedDeliveries()

        {

            return 0;

        }

        public static (int transitStarted, int invoicesOverdue, int schedulesPastDue, int deliveriesDelayed) RunOperationalLifecycleAutomation()

        {

            int transit = UpdatePendingDeliveriesToTransit();

            int overdue = UpdateOverdueInvoices();

            int pastDue = UpdatePastDueLoadingSchedules();

            int delayed = 0;

            return (transit, overdue, pastDue, delayed);

        }

        /// <summary>

        /// A dispatch counts as "For Completion" once it has been In Transit past its dispatch day.

        /// The status in the database stays 'In Transit' (so the truck and crew stay locked) until someone marks the order complete.

        /// </summary>

        public static bool IsAwaitingCompletion(Delivery d)

        {

            if (d == null) return false;

            return string.Equals(d.Status, "In Transit", StringComparison.OrdinalIgnoreCase) && d.DispatchDate.Date < DateTime.Today;

        }

        /// <summary>

        /// Marks a dispatch order as fully completed: releases the truck, driver and aides, closes the linked

        /// loading schedule(s) and marks pending stops as delivered. A trip report can still be filed afterwards.

        /// </summary>

        public static (bool ok, string message) CompleteDelivery(int deliveryId, string? completedBy = null)

        {

            if (deliveryId <= 0) return (false, "No dispatch order selected.");

            try

            {

                var planned = GetDeliveryById(deliveryId);

                if (planned != null && planned.DispatchDate.Date > DateTime.Today)

                    return (false, $"Dispatch #{planned.DispatchNumber} is scheduled for {planned.DispatchDate:MMMM d, yyyy}. It can't be marked completed before its dispatch date.");

                if (planned != null && !planned.Status.Equals("In Transit", StringComparison.OrdinalIgnoreCase) && !planned.Status.Equals("Delayed", StringComparison.OrdinalIgnoreCase)

                    && !planned.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !planned.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)

                    && DepartureDateTime(planned) > DateTime.Now)

                    return (false, $"Dispatch #{planned.DispatchNumber} hasn't left yet — it goes in transit automatically at {DepartureDateTime(planned):h:mm tt}.\n\nIf the truck already left, use \"Start trip now\" first, then complete it when it arrives.");

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var tx = conn.BeginTransaction();

                string dspNum = "", status = "", aideIdsCsv = "";

                int truckId = 0, driverId = 0, aideId = 0, lsId = 0;

                using (var get = conn.CreateCommand())

                {

                    get.Transaction = tx;

                    get.CommandText = "SELECT DispatchNumber, Status, TruckId, DriverId, AideId, AideIds, LoadingScheduleId FROM Deliveries WHERE Id = @id";

                    get.Parameters.AddWithValue("@id", deliveryId);

                    using var r = get.ExecuteReader();

                    if (!r.Read()) return (false, "This dispatch order no longer exists.");

                    dspNum = r.IsDBNull(0) ? "" : r.GetString(0);

                    status = r.IsDBNull(1) ? "" : r.GetString(1);

                    truckId = r.IsDBNull(2) ? 0 : r.GetInt32(2);

                    driverId = r.IsDBNull(3) ? 0 : r.GetInt32(3);

                    aideId = r.IsDBNull(4) ? 0 : r.GetInt32(4);

                    aideIdsCsv = r.IsDBNull(5) ? "" : r.GetString(5);

                    lsId = r.IsDBNull(6) ? 0 : r.GetInt32(6);

                }

                if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return (false, $"Dispatch #{dspNum} is already completed.");

                if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return (false, $"Dispatch #{dspNum} was cancelled and cannot be completed.");

                void Exec(string sql, params (string name, object value)[] ps)

                {

                    using var c = conn.CreateCommand();

                    c.Transaction = tx;

                    c.CommandText = sql;

                    foreach (var (n, v) in ps) c.Parameters.AddWithValue(n, v);

                    c.ExecuteNonQuery();

                }

                Exec("UPDATE Deliveries SET Status = 'Completed' WHERE Id = @id", ("@id", deliveryId));

                if (truckId > 0) Exec($"UPDATE Trucks SET Status = 'Active' WHERE Id = @tid AND Status NOT IN {BlockedTruckStatusesSql}", ("@tid", truckId));

                if (driverId > 0) Exec("UPDATE Crews SET IsAvailable = 1 WHERE Id = @cid", ("@cid", driverId));

                var aides = ParseIdList(aideIdsCsv);

                if (aideId > 0 && !aides.Contains(aideId)) aides.Add(aideId);

                foreach (int a in aides) Exec("UPDATE Crews SET IsAvailable = 1 WHERE Id = @cid", ("@cid", a));

                Exec("UPDATE DispatchStops SET Status = 'Delivered' WHERE DeliveryId = @did AND (Status IS NULL OR Status = '' OR Status = 'Pending')", ("@did", deliveryId));

                Exec("UPDATE LoadingSchedules SET Status = 'Completed' WHERE Id IN (SELECT LoadingScheduleId FROM DispatchStops WHERE DeliveryId = @did AND LoadingScheduleId IS NOT NULL)", ("@did", deliveryId));

                if (lsId > 0) Exec("UPDATE LoadingSchedules SET Status = 'Completed' WHERE Id = @lsid", ("@lsid", lsId));

                ReconcileResourceStatuses(conn, tx);

                tx.Commit();

                try

                {

                    LogAuditTrail("ORDER_COMPLETED", "Deliveries", deliveryId, dspNum, "Status", status, "Completed",

                        $"Dispatch #{dspNum} marked as completed{(string.IsNullOrWhiteSpace(completedBy) ? "" : " by " + completedBy)}");

                }

                catch { }

                return (true, $"Dispatch #{dspNum} is now completed. The truck and crew are available again.");

            }

            catch (Exception ex)

            {

                return (false, $"Could not complete the order: {ex.Message}");

            }

        }

        public static Delivery? GetDeliveryById(int id)
        {
            if (id <= 0) return null;
            return GetDeliveries().FirstOrDefault(d => d.Id == id);
        }

        public static List<Delivery> GetDeliveries()

        {

            UpdatePendingDeliveriesToTransit();

            var list = new List<Delivery>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, DispatchNumber, LoadingScheduleId, LoadingScheduleInfo, TruckId, TruckInfo, DriverId, DriverName, AideId, AideName, DispatchDate, DepartureTime, Status, Remarks, GCrate, ECrate, TotalCrates, AideIds, AideNames, CancellationReason, COALESCE(CargoType, 'Cold / Chilled'), COALESCE(IsMultiDrop, 0), COALESCE(TotalStopsCount, 1), COALESCE(ClientName, ''), COALESCE(Destination, ''), COALESCE(IsPeak, 0) FROM Deliveries ORDER BY CASE WHEN Status IN ('Scheduled', 'Pending') THEN 1 WHEN Status = 'In Transit' THEN 2 WHEN Status = 'Completed' THEN 3 WHEN Status = 'Cancelled' THEN 4 ELSE 5 END ASC, DispatchDate DESC, Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime dDate = DateTime.TryParse(reader.GetString(10), out var dt) ? dt : DateTime.Today;

                        int aideId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);

                        string aideName = reader.IsDBNull(9) ? "" : reader.GetString(9);

                        string aideIds = reader.IsDBNull(17) ? "" : reader.GetString(17);

                        string aideNames = reader.IsDBNull(18) ? "" : reader.GetString(18);

                        if (string.IsNullOrEmpty(aideIds) && aideId > 0) aideIds = aideId.ToString();

                        if (string.IsNullOrEmpty(aideNames) && !string.IsNullOrEmpty(aideName)) aideNames = aideName;

                        int gCrate = reader.IsDBNull(14) ? 0 : reader.GetInt32(14);

                        int eCrate = reader.IsDBNull(15) ? 0 : reader.GetInt32(15);

                        int totalCrates = reader.IsDBNull(16) ? 0 : reader.GetInt32(16);

                        if (totalCrates == 0 && (gCrate > 0 || eCrate > 0)) totalCrates = gCrate + eCrate;

                        if (gCrate == 0 && eCrate == 0 && totalCrates > 0) gCrate = totalCrates;

                        string loadingSchedInfo = reader.IsDBNull(3) ? "" : reader.GetString(3);

                        string clientName = reader.IsDBNull(23) ? "" : reader.GetString(23);

                        string dest = reader.IsDBNull(24) ? "" : reader.GetString(24);

                        bool isPeak = !reader.IsDBNull(25) && reader.GetInt32(25) == 1;

                        if (string.IsNullOrEmpty(clientName) && !string.IsNullOrEmpty(loadingSchedInfo))

                        {

                            int dashIdx = loadingSchedInfo.IndexOf('-');

                            int openParen = loadingSchedInfo.LastIndexOf('(');

                            int closeParen = loadingSchedInfo.LastIndexOf(')');

                            if (dashIdx >= 0 && openParen > dashIdx)

                            {

                                clientName = loadingSchedInfo.Substring(dashIdx + 1, openParen - dashIdx - 1).Trim();

                                if (closeParen > openParen && string.IsNullOrEmpty(dest))

                                {

                                    dest = loadingSchedInfo.Substring(openParen + 1, closeParen - openParen - 1).Trim();

                                }

                            }

                            else if (dashIdx >= 0)

                            {

                                clientName = loadingSchedInfo.Substring(dashIdx + 1).Trim();

                            }

                        }

                        list.Add(new Delivery

                        {

                            Id = reader.GetInt32(0),

                            DispatchNumber = reader.GetString(1),

                            LoadingScheduleId = reader.GetInt32(2),

                            LoadingScheduleInfo = loadingSchedInfo,

                            ClientName = clientName,

                            Destination = dest,

                            IsPeak = isPeak,

                            TruckId = reader.GetInt32(4),

                            TruckInfo = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            DriverId = reader.GetInt32(6),

                            DriverName = reader.IsDBNull(7) ? "" : reader.GetString(7),

                            AideId = aideId,

                            AideName = aideName,

                            AideIds = aideIds,

                            AideNames = aideNames,

                            GCrate = gCrate,

                            ECrate = eCrate,

                            TotalCrates = totalCrates,

                            DispatchDate = dDate,

                            DepartureTime = reader.IsDBNull(11) ? "08:00 AM" : reader.GetString(11),

                            Status = reader.IsDBNull(12) ? "Scheduled" : (reader.GetString(12) == "Pending" ? "Scheduled" : reader.GetString(12)),

                            Remarks = reader.IsDBNull(13) ? "" : reader.GetString(13),

                            CancellationReason = reader.IsDBNull(19) ? "" : reader.GetString(19),

                            CargoType = reader.IsDBNull(20) ? "Cold / Chilled" : reader.GetString(20),

                            IsMultiDrop = !reader.IsDBNull(21) && reader.GetInt32(21) == 1,

                            TotalStopsCount = reader.IsDBNull(22) ? 1 : reader.GetInt32(22)

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<Delivery>> GetDeliveriesAsync()

        {

            return await Task.Run(() => GetDeliveries());

        }

        /// <summary>Saves only an order's notes (remarks) and records the change in the audit trail.</summary>

        public static void UpdateDeliveryRemarks(int deliveryId, string remarks)

        {

            string oldRemarks = "", number = "";

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var get = conn.CreateCommand())

                {

                    get.CommandText = "SELECT COALESCE(Remarks, ''), COALESCE(DispatchNumber, '') FROM Deliveries WHERE Id = @id";

                    get.Parameters.AddWithValue("@id", deliveryId);

                    using var r = get.ExecuteReader();

                    if (r.Read()) { oldRemarks = r.GetString(0); number = r.GetString(1); }

                }

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Deliveries SET Remarks = @r WHERE Id = @id";

                cmd.Parameters.AddWithValue("@r", remarks ?? "");

                cmd.Parameters.AddWithValue("@id", deliveryId);

                cmd.ExecuteNonQuery();

            }

            try { LogAuditTrail("UPDATE", "Deliveries", deliveryId, number, "Remarks", oldRemarks, remarks ?? "", "Order notes edited"); } catch { }

        }

        public static void SaveDelivery(Delivery d)

        {

            try { EnsureClientExists(d.ClientName); } catch { }

            bool wasNew = d.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var tx = conn.BeginTransaction())

                {

                    try

                    {

                        var cmd = conn.CreateCommand();

                        cmd.Transaction = tx;

                        d.TotalCrates = d.GCrate + d.ECrate;

                        var aideIdList = new List<int>();

                        if (!string.IsNullOrEmpty(d.AideIds))

                        {

                            foreach (var s in d.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

                            {

                                if (int.TryParse(s.Trim(), out int parsedId) && parsedId > 0)

                                {

                                    aideIdList.Add(parsedId);

                                }

                            }

                        }

                        if (aideIdList.Count == 0 && d.AideId > 0)

                        {

                            aideIdList.Add(d.AideId);

                        }

                        if (d.Id == 0)

                        {

                            if (string.IsNullOrWhiteSpace(d.DispatchNumber))

                            {

                                d.DispatchNumber = $"DSP-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";

                            }

                            if (string.IsNullOrEmpty(d.LoadingScheduleInfo) && !string.IsNullOrEmpty(d.ClientName))

                            {

                                d.LoadingScheduleInfo = $"{d.DispatchNumber} - {d.ClientName} ({d.Destination})";

                            }

                            cmd.CommandText = @"INSERT INTO Deliveries (DispatchNumber, LoadingScheduleId, LoadingScheduleInfo, TruckId, TruckInfo, DriverId, DriverName, AideId, AideName, AideIds, AideNames, GCrate, ECrate, TotalCrates, DispatchDate, DepartureTime, Status, Remarks, CancellationReason, CargoType, IsMultiDrop, TotalStopsCount, ClientName, Destination, IsPeak)

                                                VALUES (@dn, @lsid, @lsi, @tid, @ti, @drid, @drn, @aid, @ain, @aids, @ains, @gc, @ec, @tc, @dd, @dt, @s, @r, @cr, @ct, @md, @tsc, @cn, @dest, @pk);

                                                SELECT last_insert_rowid();";

                            if (d.LoadingScheduleId > 0)

                            {

                                var updateLs = conn.CreateCommand();

                                updateLs.Transaction = tx;

                                updateLs.CommandText = "UPDATE LoadingSchedules SET Status='Assigned' WHERE Id=@lsid";

                                updateLs.Parameters.AddWithValue("@lsid", d.LoadingScheduleId);

                                updateLs.ExecuteNonQuery();

                            }

                            if (d.DispatchDate.Date <= DateTime.Today && d.Status != "Cancelled")

                            {

                                var updateTr = conn.CreateCommand();

                                updateTr.Transaction = tx;

                                updateTr.CommandText = $"UPDATE Trucks SET Status='In Use' WHERE Id=@tid AND Status NOT IN {BlockedTruckStatusesSql}";

                                updateTr.Parameters.AddWithValue("@tid", d.TruckId);

                                updateTr.ExecuteNonQuery();

                                var lockCrew = conn.CreateCommand();

                                lockCrew.Transaction = tx;

                                lockCrew.CommandText = "UPDATE Crews SET IsAvailable=0 WHERE Id = @drid";

                                lockCrew.Parameters.AddWithValue("@drid", d.DriverId);

                                lockCrew.ExecuteNonQuery();

                                foreach (int aideId in aideIdList)

                                {

                                    var lockAide = conn.CreateCommand();

                                    lockAide.Transaction = tx;

                                    lockAide.CommandText = "UPDATE Crews SET IsAvailable=0 WHERE Id = @aid";

                                    lockAide.Parameters.AddWithValue("@aid", aideId);

                                    lockAide.ExecuteNonQuery();

                                }

                            }

                        }

                        else

                        {

                            int oldTruckId = 0;

                            int oldDriverId = 0;

                            var oldAideIds = new HashSet<int>();

                            using (var oldCmd = conn.CreateCommand())

                            {

                                oldCmd.Transaction = tx;

                                oldCmd.CommandText = "SELECT TruckId, DriverId, AideId, AideIds FROM Deliveries WHERE Id = @id";

                                oldCmd.Parameters.AddWithValue("@id", d.Id);

                                using var rOld = oldCmd.ExecuteReader();

                                if (rOld.Read())

                                {

                                    oldTruckId = rOld.GetInt32(0);

                                    oldDriverId = rOld.GetInt32(1);

                                    int aid = rOld.IsDBNull(2) ? 0 : rOld.GetInt32(2);

                                    string aidesStr = rOld.IsDBNull(3) ? "" : rOld.GetString(3);

                                    if (aid > 0) oldAideIds.Add(aid);

                                    foreach (var s in aidesStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

                                    {

                                        if (int.TryParse(s.Trim(), out int pid) && pid > 0) oldAideIds.Add(pid);

                                    }

                                }

                            }

                            if (oldTruckId > 0 && oldTruckId != d.TruckId)

                            {

                                var relOldTrk = conn.CreateCommand();

                                relOldTrk.Transaction = tx;

                                relOldTrk.CommandText = $"UPDATE Trucks SET Status='Active' WHERE Id=@otid AND Status NOT IN {BlockedTruckStatusesSql}";

                                relOldTrk.Parameters.AddWithValue("@otid", oldTruckId);

                                relOldTrk.ExecuteNonQuery();

                            }

                            if (oldDriverId > 0 && oldDriverId != d.DriverId)

                            {

                                var relOldDrv = conn.CreateCommand();

                                relOldDrv.Transaction = tx;

                                relOldDrv.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id=@odrid";

                                relOldDrv.Parameters.AddWithValue("@odrid", oldDriverId);

                                relOldDrv.ExecuteNonQuery();

                            }

                            foreach (int oldAid in oldAideIds)

                            {

                                if (!aideIdList.Contains(oldAid))

                                {

                                    var relOldAid = conn.CreateCommand();

                                    relOldAid.Transaction = tx;

                                    relOldAid.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id=@oaid";

                                    relOldAid.Parameters.AddWithValue("@oaid", oldAid);

                                    relOldAid.ExecuteNonQuery();

                                }

                            }

                            cmd.CommandText = @"UPDATE Deliveries SET LoadingScheduleId=@lsid, LoadingScheduleInfo=@lsi, TruckId=@tid, TruckInfo=@ti, DriverId=@drid, DriverName=@drn, AideId=@aid, AideName=@ain, AideIds=@aids, AideNames=@ains, GCrate=@gc, ECrate=@ec, TotalCrates=@tc, DispatchDate=@dd, DepartureTime=@dt, Status=@s, Remarks=@r, CancellationReason=@cr, CargoType=@ct, IsMultiDrop=@md, TotalStopsCount=@tsc, ClientName=@cn, Destination=@dest, IsPeak=@pk WHERE Id=@id";

                            cmd.Parameters.AddWithValue("@id", d.Id);

                            if (d.Status == "Completed" || d.Status == "Cancelled")

                            {

                                var releaseTr = conn.CreateCommand();

                                releaseTr.Transaction = tx;

                                releaseTr.CommandText = $"UPDATE Trucks SET Status='Active' WHERE Id=@tid AND Status NOT IN {BlockedTruckStatusesSql}";

                                releaseTr.Parameters.AddWithValue("@tid", d.TruckId);

                                releaseTr.ExecuteNonQuery();

                                var releaseDriver = conn.CreateCommand();

                                releaseDriver.Transaction = tx;

                                releaseDriver.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id = @drid";

                                releaseDriver.Parameters.AddWithValue("@drid", d.DriverId);

                                releaseDriver.ExecuteNonQuery();

                                foreach (int aideId in aideIdList)

                                {

                                    var releaseAide = conn.CreateCommand();

                                    releaseAide.Transaction = tx;

                                    releaseAide.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id = @aid";

                                    releaseAide.Parameters.AddWithValue("@aid", aideId);

                                    releaseAide.ExecuteNonQuery();

                                }

                                var stopsToUpdate = d.Stops;

                                if (d.IsMultiDrop && (stopsToUpdate == null || stopsToUpdate.Count == 0) && d.Id > 0)

                                {

                                    stopsToUpdate = GetDispatchStops(d.Id);

                                }

                                if (d.IsMultiDrop && stopsToUpdate != null && stopsToUpdate.Count > 0)

                                {

                                    foreach (var stop in stopsToUpdate)

                                    {

                                        if (stop.LoadingScheduleId.HasValue && stop.LoadingScheduleId.Value > 0)

                                        {

                                            var updateLsStop = conn.CreateCommand();

                                            updateLsStop.Transaction = tx;

                                            updateLsStop.CommandText = d.Status == "Completed"

                                                ? "UPDATE LoadingSchedules SET Status='Completed' WHERE Id=@lsid"

                                                : "UPDATE LoadingSchedules SET Status='Pending' WHERE Id=@lsid";

                                            updateLsStop.Parameters.AddWithValue("@lsid", stop.LoadingScheduleId.Value);

                                            updateLsStop.ExecuteNonQuery();

                                        }

                                    }

                                }

                                else if (d.LoadingScheduleId > 0)

                                {

                                    var updateLs = conn.CreateCommand();

                                    updateLs.Transaction = tx;

                                    updateLs.CommandText = d.Status == "Completed"

                                        ? "UPDATE LoadingSchedules SET Status='Completed' WHERE Id=@lsid"

                                        : "UPDATE LoadingSchedules SET Status='Pending' WHERE Id=@lsid";

                                    updateLs.Parameters.AddWithValue("@lsid", d.LoadingScheduleId);

                                    updateLs.ExecuteNonQuery();

                                }

                            }

                            else if (d.DispatchDate.Date <= DateTime.Today)

                            {

                                var lockTr = conn.CreateCommand();

                                lockTr.Transaction = tx;

                                lockTr.CommandText = $"UPDATE Trucks SET Status='In Use' WHERE Id=@tid AND Status NOT IN {BlockedTruckStatusesSql}";

                                lockTr.Parameters.AddWithValue("@tid", d.TruckId);

                                lockTr.ExecuteNonQuery();

                                var lockDriver = conn.CreateCommand();

                                lockDriver.Transaction = tx;

                                lockDriver.CommandText = "UPDATE Crews SET IsAvailable=0 WHERE Id = @drid";

                                lockDriver.Parameters.AddWithValue("@drid", d.DriverId);

                                lockDriver.ExecuteNonQuery();

                                foreach (int aideId in aideIdList)

                                {

                                    var lockAide = conn.CreateCommand();

                                    lockAide.Transaction = tx;

                                    lockAide.CommandText = "UPDATE Crews SET IsAvailable=0 WHERE Id = @aid";

                                    lockAide.Parameters.AddWithValue("@aid", aideId);

                                    lockAide.ExecuteNonQuery();

                                }

                            }

                        }

                        cmd.Parameters.AddWithValue("@dn", d.DispatchNumber);

                        cmd.Parameters.AddWithValue("@lsid", d.LoadingScheduleId);

                        cmd.Parameters.AddWithValue("@lsi", d.LoadingScheduleInfo);

                        cmd.Parameters.AddWithValue("@tid", d.TruckId);

                        cmd.Parameters.AddWithValue("@ti", d.TruckInfo);

                        cmd.Parameters.AddWithValue("@drid", d.DriverId);

                        cmd.Parameters.AddWithValue("@drn", d.DriverName);

                        cmd.Parameters.AddWithValue("@aid", d.AideId);

                        cmd.Parameters.AddWithValue("@ain", d.AideName);

                        cmd.Parameters.AddWithValue("@aids", d.AideIds ?? "");

                        cmd.Parameters.AddWithValue("@ains", d.AideNames ?? "");

                        cmd.Parameters.AddWithValue("@gc", d.GCrate);

                        cmd.Parameters.AddWithValue("@ec", d.ECrate);

                        cmd.Parameters.AddWithValue("@tc", d.TotalCrates);

                        cmd.Parameters.AddWithValue("@dd", d.DispatchDate.ToString("yyyy-MM-dd"));

                        cmd.Parameters.AddWithValue("@dt", d.DepartureTime ?? "08:00 AM");

                        cmd.Parameters.AddWithValue("@s", d.Status);

                        cmd.Parameters.AddWithValue("@r", d.Remarks);

                        cmd.Parameters.AddWithValue("@cr", d.CancellationReason ?? "");

                        cmd.Parameters.AddWithValue("@ct", string.IsNullOrEmpty(d.CargoType) ? "Cold / Chilled" : d.CargoType);

                        cmd.Parameters.AddWithValue("@md", d.IsMultiDrop ? 1 : 0);

                        cmd.Parameters.AddWithValue("@tsc", d.TotalStopsCount > 0 ? d.TotalStopsCount : 1);

                        cmd.Parameters.AddWithValue("@cn", d.ClientName ?? "");

                        cmd.Parameters.AddWithValue("@dest", d.Destination ?? "");

                        cmd.Parameters.AddWithValue("@pk", d.IsPeak ? 1 : 0);

                        if (d.Id == 0)

                        {

                            object? insertRes = cmd.ExecuteScalar();

                            if (insertRes != null && insertRes != DBNull.Value)

                            {

                                d.Id = Convert.ToInt32(insertRes);

                            }

                        }

                        else

                        {

                            cmd.ExecuteNonQuery();

                        }

                        if (d.Id > 0)

                        {

                            if (d.Stops != null && d.Stops.Count > 0)

                            {

                                SaveDispatchStopsInternal(conn, tx, d.Id, d.Stops);

                            }

                            else

                            {

                                using var countCmd = conn.CreateCommand();

                                countCmd.Transaction = tx;

                                countCmd.CommandText = "SELECT COUNT(*) FROM DispatchStops WHERE DeliveryId = @did";

                                countCmd.Parameters.AddWithValue("@did", d.Id);

                                long existingCount = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);

                                if (existingCount == 0)

                                {

                                    var autoStops = GetOrBuildTripStopsForDelivery(d);

                                    if (autoStops.Count > 0)

                                    {

                                        SaveDispatchStopsInternal(conn, tx, d.Id, autoStops);

                                    }

                                }

                            }

                        }

                        ReconcileResourceStatuses(conn, tx);

                        tx.Commit();

                        try

                        {

                            bool isNew = wasNew;

                            LogAuditTrail(isNew ? "DISPATCH_CREATED" : (d.Status == "Cancelled" ? "CANCEL" : "UPDATE"), "Deliveries", d.Id, d.DispatchNumber, "Status", "", d.Status, $"Dispatch #{d.DispatchNumber} {(isNew ? "created" : "updated")}, Truck: {d.TruckInfo}, Driver: {d.DriverName}, Status: {d.Status}");

                            EventNotificationService.Publish(isNew ? "Trip Dispatched" : "Trip Updated", $"Dispatch #{d.DispatchNumber} assigned to {d.TruckInfo}", ToastType.Success);

                        }

                        catch { }

                    }

                    catch

                    {

                        tx.Rollback();

                        throw;

                    }

                }

            }

        }

        public static string? ValidateDispatchConflict(int deliveryId, int truckId, int driverId, List<int>? aideIds, DateTime dispatchDate)

        {

            return ValidateResourceConflict(deliveryId, 0, truckId, driverId, aideIds, dispatchDate);

        }

        /// <summary>

        /// Checks truck/driver/aide availability for a date against regular dispatches AND special trips.

        /// Pass the id of the record being edited (deliveryId or specialTripId) so it is excluded from the check.

        /// </summary>

        public static string? ValidateResourceConflict(int deliveryId, int specialTripId, int truckId, int driverId, List<int>? aideIds, DateTime dispatchDate)

        {

            bool isNew = deliveryId <= 0 && specialTripId <= 0;

            bool isToday = dispatchDate.Date == DateTime.Today;

            string dd = dispatchDate.ToString("yyyy-MM-dd");

            var crewToCheck = new List<(int Id, string Label)>();

            if (driverId > 0) crewToCheck.Add((driverId, "DRIVER"));

            if (aideIds != null) foreach (int aid in aideIds.Where(a => a > 0).Distinct()) crewToCheck.Add((aid, "AIDE/HELPER"));

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                if (truckId > 0)

                {

                    var checkTr = conn.CreateCommand();

                    checkTr.CommandText = "SELECT PlateNumber, Status FROM Trucks WHERE Id = @tid";

                    checkTr.Parameters.AddWithValue("@tid", truckId);

                    using (var r = checkTr.ExecuteReader())

                    {

                        if (r.Read())

                        {

                            string plate = r.GetString(0);

                            string status = r.IsDBNull(1) ? "" : r.GetString(1);

                            if (IsTruckBlockedStatus(status))

                            {

                                return $"TRUCK NOT AVAILABLE:\nVehicle '{plate}' is marked as '{status}' and cannot be dispatched.";

                            }

                            if (isNew && isToday && !IsTruckReadyStatus(status))

                            {

                                return $"TRUCK UNAVAILABLE:\nVehicle '{plate}' is currently marked as '{status}' and cannot be dispatched for today.";

                            }

                        }

                    }

                }

                foreach (var c in crewToCheck)

                {

                    var checkCrew = conn.CreateCommand();

                    checkCrew.CommandText = "SELECT FullName, IsAvailable, IsActive FROM Crews WHERE Id = @cid";

                    checkCrew.Parameters.AddWithValue("@cid", c.Id);

                    using var r = checkCrew.ExecuteReader();

                    if (r.Read())

                    {

                        string name = r.GetString(0);

                        bool avail = r.GetInt32(1) == 1;

                        bool active = r.IsDBNull(2) || r.GetInt32(2) == 1;

                        if (!active)

                        {

                            return $"{c.Label} INACTIVE:\n'{name}' is marked inactive and cannot be assigned.";

                        }

                        if (isNew && isToday && !avail)

                        {

                            return $"{c.Label} ON TRIP:\n'{name}' is currently marked as 'On Trip' and cannot be assigned to a new dispatch for today.";

                        }

                    }

                }

                if (truckId > 0)

                {

                    var cmdTruck = conn.CreateCommand();

                    cmdTruck.CommandText = $@"SELECT DispatchNumber, Status FROM Deliveries

                                             WHERE TruckId = @tid AND DATE(DispatchDate) = DATE(@dd) AND Id <> @id AND Status IN {ActiveDeliveryStatusesSql}";

                    cmdTruck.Parameters.AddWithValue("@tid", truckId);

                    cmdTruck.Parameters.AddWithValue("@dd", dd);

                    cmdTruck.Parameters.AddWithValue("@id", deliveryId);

                    using (var reader = cmdTruck.ExecuteReader())

                    {

                        if (reader.Read())

                        {

                            return $"TRUCK DISPATCH CONFLICT:\nThe selected vehicle is already on active dispatch #{reader.GetString(0)} ({reader.GetString(1)}) for {dispatchDate:MMM dd, yyyy}.";

                        }

                    }

                }

                foreach (var c in crewToCheck)

                {

                    var cmdCrew = conn.CreateCommand();

                    cmdCrew.CommandText = $@"SELECT DispatchNumber, Status FROM Deliveries

                                            WHERE (DriverId = @cid OR AideId = @cid OR {AideIdsContainsSql})

                                              AND DATE(DispatchDate) = DATE(@dd) AND Id <> @id AND Status IN {ActiveDeliveryStatusesSql}";

                    cmdCrew.Parameters.AddWithValue("@cid", c.Id);

                    cmdCrew.Parameters.AddWithValue("@aidToken", c.Id.ToString());

                    cmdCrew.Parameters.AddWithValue("@dd", dd);

                    cmdCrew.Parameters.AddWithValue("@id", deliveryId);

                    using var reader = cmdCrew.ExecuteReader();

                    if (reader.Read())

                    {

                        return $"{c.Label} ASSIGNMENT CONFLICT:\nThe selected crew member is already assigned to active dispatch #{reader.GetString(0)} ({reader.GetString(1)}) for {dispatchDate:MMM dd, yyyy}.";

                    }

                }

                try

                {

                    var cmdSt = conn.CreateCommand();

                    cmdSt.CommandText = $@"SELECT OrderReference, Status, TruckId, DriverId, AideId FROM SpecialTrips

                                          WHERE DATE(DispatchDate) = DATE(@dd) AND Id <> @sid AND Status IN {ActiveSpecialTripStatusesSql}";

                    cmdSt.Parameters.AddWithValue("@dd", dd);

                    cmdSt.Parameters.AddWithValue("@sid", specialTripId);

                    using var reader = cmdSt.ExecuteReader();

                    while (reader.Read())

                    {

                        string reference = reader.IsDBNull(0) ? "" : reader.GetString(0);

                        int stTruck = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                        int stDriver = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                        int stAide = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                        if (truckId > 0 && stTruck == truckId)

                        {

                            return $"TRUCK DISPATCH CONFLICT:\nThe selected vehicle is already booked on Special Trip {reference} for {dispatchDate:MMM dd, yyyy}.";

                        }

                        foreach (var c in crewToCheck)

                        {

                            if (c.Id == stDriver || c.Id == stAide)

                            {

                                return $"{c.Label} ASSIGNMENT CONFLICT:\nThe selected crew member is already booked on Special Trip {reference} for {dispatchDate:MMM dd, yyyy}.";

                            }

                        }

                    }

                }

                catch (SqliteException ex)

                {

                    System.Diagnostics.Debug.WriteLine($"[DB] Special trip conflict check: {ex.Message}");

                }

            }

            return null;

        }

        public static List<DispatchStop> GetLoadingScheduleStops(int loadingScheduleId)

        {

            var list = new List<DispatchStop>();

            if (loadingScheduleId <= 0) return list;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, LoadingScheduleId, StopSequence, StoreCode, StoreName, ECrate, GCrate, DrNumbers FROM LoadingScheduleStops WHERE LoadingScheduleId = @lsid ORDER BY StopSequence ASC, Id ASC";

                cmd.Parameters.AddWithValue("@lsid", loadingScheduleId);

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    int ec = r.IsDBNull(5) ? 0 : r.GetInt32(5);

                    int gc = r.IsDBNull(6) ? 0 : r.GetInt32(6);

                    list.Add(new DispatchStop

                    {

                        Id = r.GetInt32(0),

                        LoadingScheduleId = r.GetInt32(1),

                        StopSequence = r.GetInt32(2),

                        StoreCode = r.IsDBNull(3) ? "" : r.GetString(3),

                        Destination = r.IsDBNull(4) ? "" : r.GetString(4),

                        ECrate = ec,

                        GCrate = gc,

                        TotalCrates = ec + gc,

                        DeliveredECrates = ec,

                        DeliveredGCrates = gc,

                        DeliveredCrates = ec + gc,

                        ReturnedECrates = ec,

                        ReturnedGCrates = gc,

                        ReturnedCrates = ec + gc,

                        DrNumbers = r.IsDBNull(7) ? "" : r.GetString(7)

                    });

                }

            }

            catch { }

            return list;

        }

        public static void SaveLoadingScheduleStops(int loadingScheduleId, List<DispatchStop> stops)

        {

            if (loadingScheduleId <= 0 || stops == null) return;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var tx = conn.BeginTransaction();

                using (var delCmd = conn.CreateCommand())

                {

                    delCmd.Transaction = tx;

                    delCmd.CommandText = "DELETE FROM LoadingScheduleStops WHERE LoadingScheduleId = @lsid";

                    delCmd.Parameters.AddWithValue("@lsid", loadingScheduleId);

                    delCmd.ExecuteNonQuery();

                }

                for (int i = 0; i < stops.Count; i++)

                {

                    var s = stops[i];

                    using var ins = conn.CreateCommand();

                    ins.Transaction = tx;

                    ins.CommandText = @"INSERT INTO LoadingScheduleStops (LoadingScheduleId, StopSequence, StoreCode, StoreName, ECrate, GCrate, DrNumbers)

                                        VALUES (@lsid, @seq, @sc, @sn, @ec, @gc, @dr)";

                    ins.Parameters.AddWithValue("@lsid", loadingScheduleId);

                    ins.Parameters.AddWithValue("@seq", i + 1);

                    ins.Parameters.AddWithValue("@sc", s.StoreCode ?? "");

                    ins.Parameters.AddWithValue("@sn", s.Destination ?? "");

                    ins.Parameters.AddWithValue("@ec", s.ECrate);

                    ins.Parameters.AddWithValue("@gc", s.GCrate);

                    ins.Parameters.AddWithValue("@dr", s.DrNumbers ?? "");

                    ins.ExecuteNonQuery();

                }

                tx.Commit();

            }

            catch { }

        }

        public static List<DispatchStop> GetOrBuildTripStopsForDelivery(Delivery d)

        {

            if (d == null) return new List<DispatchStop>();

            var existing = GetDispatchStops(d.Id);

            if (existing.Count > 0)

            {

                foreach (var s in existing)

                {

                    if (string.IsNullOrWhiteSpace(s.StoreCode))

                    {

                        var split = DeliveryRoute.SplitStoreCodeAndName(s.Destination);

                        if (!string.IsNullOrEmpty(split.StoreCode))

                        {

                            s.StoreCode = split.StoreCode;

                            s.Destination = split.StoreName;

                        }

                    }

                    if (s.DeliveredECrates == 0 && s.DeliveredGCrates == 0 && (s.ECrate > 0 || s.GCrate > 0))

                    {

                        s.DeliveredECrates = s.ECrate;

                        s.DeliveredGCrates = s.GCrate;

                        s.DeliveredCrates = s.ECrate + s.GCrate;

                    }

                    if (s.ReturnedECrates == 0 && s.ReturnedGCrates == 0 && (s.DeliveredECrates > 0 || s.DeliveredGCrates > 0))

                    {

                        s.ReturnedECrates = s.DeliveredECrates;

                        s.ReturnedGCrates = s.DeliveredGCrates;

                        s.ReturnedCrates = s.DeliveredCrates;

                    }

                }

                return existing;

            }

            if (d.LoadingScheduleId > 0)

            {

                var lsStops = GetLoadingScheduleStops(d.LoadingScheduleId);

                if (lsStops.Count > 0)

                {

                    foreach (var s in lsStops)

                    {

                        s.DeliveryId = d.Id;

                        s.ClientName = d.ClientName;

                    }

                    return lsStops;

                }

            }

            var route = FindRouteByNameOrCode(d.Destination) ?? FindRouteByNameOrCode(d.LoadingScheduleInfo);

            var built = new List<DispatchStop>();

            if (route != null && route.PlacesList.Count > 0)

            {

                int count = route.PlacesList.Count;

                int baseE = count > 0 ? d.ECrate / count : 0;

                int remE = count > 0 ? d.ECrate % count : 0;

                int baseG = count > 0 ? d.GCrate / count : 0;

                int remG = count > 0 ? d.GCrate % count : 0;

                for (int i = 0; i < count; i++)

                {

                    var (code, name) = DeliveryRoute.SplitStoreCodeAndName(route.PlacesList[i]);

                    int ec = baseE + (i < remE ? 1 : 0);

                    int gc = baseG + (i < remG ? 1 : 0);

                    built.Add(new DispatchStop

                    {

                        DeliveryId = d.Id,

                        StopSequence = i + 1,

                        LoadingScheduleId = d.LoadingScheduleId > 0 ? d.LoadingScheduleId : null,

                        ClientName = d.ClientName,

                        StoreCode = code,

                        Destination = name,

                        ECrate = ec,

                        GCrate = gc,

                        TotalCrates = ec + gc,

                        DeliveredECrates = ec,

                        DeliveredGCrates = gc,

                        DeliveredCrates = ec + gc,

                        ReturnedECrates = ec,

                        ReturnedGCrates = gc,

                        ReturnedCrates = ec + gc,

                        Status = "Pending"

                    });

                }

            }

            else

            {

                var (code, name) = DeliveryRoute.SplitStoreCodeAndName(string.IsNullOrWhiteSpace(d.Destination) ? d.LoadingScheduleInfo : d.Destination);

                built.Add(new DispatchStop

                {

                    DeliveryId = d.Id,

                    StopSequence = 1,

                    LoadingScheduleId = d.LoadingScheduleId > 0 ? d.LoadingScheduleId : null,

                    ClientName = d.ClientName,

                    StoreCode = code,

                    Destination = name,

                    ECrate = d.ECrate,

                    GCrate = d.GCrate,

                    TotalCrates = d.TotalCrates,

                    DeliveredECrates = d.ECrate,

                    DeliveredGCrates = d.GCrate,

                    DeliveredCrates = d.TotalCrates,

                    ReturnedECrates = d.ECrate,

                    ReturnedGCrates = d.GCrate,

                    ReturnedCrates = d.TotalCrates,

                    Status = "Pending"

                });

            }

            return built;

        }

        public static List<DispatchStop> GetDispatchStops(int deliveryId)

        {

            var list = new List<DispatchStop>();

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, DeliveryId, StopSequence, LoadingScheduleId, ClientName, Destination, GCrate, ECrate, TotalCrates, Status, ArrivalTime, DepartureTime, DeliveredGCrates, DeliveredECrates, DeliveredCrates, ReturnedGCrates, ReturnedECrates, ReturnedCrates, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName, StoreManagerName, Remarks, StoreCode, DrNumbers, OthersBox, OthersPfoods, OthersMi FROM DispatchStops WHERE DeliveryId = @did ORDER BY StopSequence ASC, Id ASC";

                cmd.Parameters.AddWithValue("@did", deliveryId);

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    string destRaw = r.IsDBNull(5) ? "" : r.GetString(5);

                    string sc = r.FieldCount > 23 && !r.IsDBNull(23) ? r.GetString(23) : "";

                    if (string.IsNullOrWhiteSpace(sc))

                    {

                        var sp = DeliveryRoute.SplitStoreCodeAndName(destRaw);

                        if (!string.IsNullOrEmpty(sp.StoreCode))

                        {

                            sc = sp.StoreCode;

                            destRaw = sp.StoreName;

                        }

                    }

                    list.Add(new DispatchStop

                    {

                        Id = r.GetInt32(0),

                        DeliveryId = r.GetInt32(1),

                        StopSequence = r.GetInt32(2),

                        LoadingScheduleId = r.IsDBNull(3) ? null : r.GetInt32(3),

                        ClientName = r.IsDBNull(4) ? "" : r.GetString(4),

                        Destination = destRaw,

                        GCrate = r.IsDBNull(6) ? 0 : r.GetInt32(6),

                        ECrate = r.IsDBNull(7) ? 0 : r.GetInt32(7),

                        TotalCrates = r.IsDBNull(8) ? 0 : r.GetInt32(8),

                        Status = r.IsDBNull(9) ? "Pending" : r.GetString(9),

                        ArrivalTime = r.IsDBNull(10) ? "" : r.GetString(10),

                        DepartureTime = r.IsDBNull(11) ? "" : r.GetString(11),

                        DeliveredGCrates = r.IsDBNull(12) ? 0 : r.GetInt32(12),

                        DeliveredECrates = r.IsDBNull(13) ? 0 : r.GetInt32(13),

                        DeliveredCrates = r.IsDBNull(14) ? 0 : r.GetInt32(14),

                        ReturnedGCrates = r.IsDBNull(15) ? 0 : r.GetInt32(15),

                        ReturnedECrates = r.IsDBNull(16) ? 0 : r.GetInt32(16),

                        ReturnedCrates = r.IsDBNull(17) ? 0 : r.GetInt32(17),

                        StoreManagerLastName = r.IsDBNull(18) ? "" : r.GetString(18),

                        StoreManagerFirstName = r.IsDBNull(19) ? "" : r.GetString(19),

                        StoreManagerMiddleName = r.IsDBNull(20) ? "" : r.GetString(20),

                        StoreManagerName = r.IsDBNull(21) ? "" : r.GetString(21),

                        Remarks = r.IsDBNull(22) ? "" : r.GetString(22),

                        StoreCode = sc,

                        DrNumbers = r.FieldCount > 24 && !r.IsDBNull(24) ? r.GetString(24) : "",

                        OthersBox = r.FieldCount > 25 && !r.IsDBNull(25) ? r.GetString(25) : "",

                        OthersPfoods = r.FieldCount > 26 && !r.IsDBNull(26) ? r.GetString(26) : "",

                        OthersMi = r.FieldCount > 27 && !r.IsDBNull(27) ? r.GetString(27) : ""

                    });

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] GetTripReports: {ex.Message}");

            }

            return list;

        }

        public static void SaveDispatchStops(int deliveryId, List<DispatchStop> stops)

        {

            if (deliveryId <= 0 || stops == null) return;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var tx = conn.BeginTransaction();

                SaveDispatchStopsInternal(conn, tx, deliveryId, stops);

                tx.Commit();

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"[DB] SaveDispatchStops: {ex.Message}");

                throw;

            }

        }

        private static void SaveDispatchStopsInternal(SqliteConnection conn, SqliteTransaction tx, int deliveryId, List<DispatchStop> stops)

        {

            using (var delCmd = conn.CreateCommand())

            {

                delCmd.Transaction = tx;

                delCmd.CommandText = "DELETE FROM DispatchStops WHERE DeliveryId = @did";

                delCmd.Parameters.AddWithValue("@did", deliveryId);

                delCmd.ExecuteNonQuery();

            }

            for (int i = 0; i < stops.Count; i++)

            {

                var s = stops[i];

                s.DeliveryId = deliveryId;

                s.StopSequence = i + 1;

                s.TotalCrates = s.GCrate + s.ECrate;

                s.DeliveredCrates = s.DeliveredGCrates + s.DeliveredECrates;

                s.ReturnedCrates = s.ReturnedGCrates + s.ReturnedECrates;

                if (string.IsNullOrWhiteSpace(s.StoreManagerName))

                {

                    s.StoreManagerName = NameHelper.FormatFullName(s.StoreManagerLastName, s.StoreManagerFirstName, s.StoreManagerMiddleName);

                }

                using var ins = conn.CreateCommand();

                ins.Transaction = tx;

                ins.CommandText = @"INSERT INTO DispatchStops (DeliveryId, StopSequence, LoadingScheduleId, ClientName, Destination, GCrate, ECrate, TotalCrates, Status, ArrivalTime, DepartureTime, DeliveredGCrates, DeliveredECrates, DeliveredCrates, ReturnedGCrates, ReturnedECrates, ReturnedCrates, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName, StoreManagerName, Remarks, StoreCode, DrNumbers, OthersBox, OthersPfoods, OthersMi)

                                    VALUES (@did, @seq, @lsid, @cn, @dest, @gc, @ec, @tc, @st, @at, @dt, @dgc, @dec, @dc, @rgc, @rec, @rc, @smln, @smfn, @smmn, @smn, @rem, @sc, @dr, @ob, @op, @om)";

                ins.Parameters.AddWithValue("@did", deliveryId);

                ins.Parameters.AddWithValue("@seq", s.StopSequence);

                ins.Parameters.AddWithValue("@lsid", s.LoadingScheduleId.HasValue ? (object)s.LoadingScheduleId.Value : DBNull.Value);

                ins.Parameters.AddWithValue("@cn", s.ClientName ?? "");

                ins.Parameters.AddWithValue("@dest", s.Destination ?? "");

                ins.Parameters.AddWithValue("@gc", s.GCrate);

                ins.Parameters.AddWithValue("@ec", s.ECrate);

                ins.Parameters.AddWithValue("@tc", s.TotalCrates);

                ins.Parameters.AddWithValue("@st", string.IsNullOrEmpty(s.Status) ? "Pending" : s.Status);

                ins.Parameters.AddWithValue("@at", s.ArrivalTime ?? "");

                ins.Parameters.AddWithValue("@dt", s.DepartureTime ?? "");

                ins.Parameters.AddWithValue("@dgc", s.DeliveredGCrates);

                ins.Parameters.AddWithValue("@dec", s.DeliveredECrates);

                ins.Parameters.AddWithValue("@dc", s.DeliveredCrates);

                ins.Parameters.AddWithValue("@rgc", s.ReturnedGCrates);

                ins.Parameters.AddWithValue("@rec", s.ReturnedECrates);

                ins.Parameters.AddWithValue("@rc", s.ReturnedCrates);

                ins.Parameters.AddWithValue("@smln", s.StoreManagerLastName ?? "");

                ins.Parameters.AddWithValue("@smfn", s.StoreManagerFirstName ?? "");

                ins.Parameters.AddWithValue("@smmn", s.StoreManagerMiddleName ?? "");

                ins.Parameters.AddWithValue("@smn", s.StoreManagerName ?? "");

                ins.Parameters.AddWithValue("@rem", s.Remarks ?? "");

                ins.Parameters.AddWithValue("@sc", s.StoreCode ?? "");

                ins.Parameters.AddWithValue("@dr", s.DrNumbers ?? "");

                ins.Parameters.AddWithValue("@ob", s.OthersBox ?? "");

                ins.Parameters.AddWithValue("@op", s.OthersPfoods ?? "");

                ins.Parameters.AddWithValue("@om", s.OthersMi ?? "");

                ins.ExecuteNonQuery();

            }

        }

        public static (bool Success, string Message, Delivery? NewDelivery) CloneDeliveryForFutureDate(int sourceDeliveryId, DateTime targetDate, string targetTime)

        {

            try

            {

                var all = GetDeliveries();

                var src = all.FirstOrDefault(d => d.Id == sourceDeliveryId);

                if (src == null) return (false, "Source dispatch order not found.", null);

                var aideIdList = new List<int>();

                if (!string.IsNullOrEmpty(src.AideIds))

                {

                    foreach (var s in src.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

                    {

                        if (int.TryParse(s.Trim(), out int parsedId) && parsedId > 0)

                        {

                            aideIdList.Add(parsedId);

                        }

                    }

                }

                if (aideIdList.Count == 0 && src.AideId > 0)

                {

                    aideIdList.Add(src.AideId);

                }

                string? conflict = ValidateDispatchConflict(0, src.TruckId, src.DriverId, aideIdList, targetDate);

                if (!string.IsNullOrEmpty(conflict))

                {

                    return (false, conflict, null);

                }

                var stops = GetDispatchStops(sourceDeliveryId);

                var cloned = new Delivery

                {

                    Id = 0,

                    LoadingScheduleId = src.LoadingScheduleId,

                    LoadingScheduleInfo = src.LoadingScheduleInfo,

                    TruckId = src.TruckId,

                    TruckInfo = src.TruckInfo,

                    DriverId = src.DriverId,

                    DriverName = src.DriverName,

                    AideId = src.AideId,

                    AideName = src.AideName,

                    AideIds = src.AideIds,

                    AideNames = src.AideNames,

                    GCrate = src.GCrate,

                    ECrate = src.ECrate,

                    TotalCrates = src.TotalCrates,

                    DispatchDate = targetDate,

                    DepartureTime = string.IsNullOrWhiteSpace(targetTime) ? src.DepartureTime : targetTime,

                    Status = "Pending",

                    Remarks = $"Scheduled from Dispatch #{src.DispatchNumber}",

                    CargoType = src.CargoType,

                    IsMultiDrop = src.IsMultiDrop,

                    TotalStopsCount = src.TotalStopsCount

                };

                SaveDelivery(cloned);

                if (stops != null && stops.Count > 0)

                {

                    var newStops = new List<DispatchStop>();

                    foreach (var s in stops)

                    {

                        newStops.Add(new DispatchStop

                        {

                            DeliveryId = cloned.Id,

                            StopSequence = s.StopSequence,

                            LoadingScheduleId = s.LoadingScheduleId,

                            ClientName = s.ClientName,

                            Destination = s.Destination,

                            GCrate = s.GCrate,

                            ECrate = s.ECrate,

                            TotalCrates = s.TotalCrates,

                            Status = "Pending",

                            Remarks = s.Remarks

                        });

                    }

                    SaveDispatchStops(cloned.Id, newStops);

                    cloned.Stops = newStops;

                }

                LogAuditTrail("CLONE_DISPATCH", "Deliveries", cloned.Id, cloned.DispatchNumber, "Status", "", "Pending", $"Cloned future dispatch from #{src.DispatchNumber} for {targetDate:MMM dd, yyyy}");

                return (true, $"Future Dispatch #{cloned.DispatchNumber} scheduled for {targetDate:MMM dd, yyyy} {cloned.DepartureTime}.", cloned);

            }

            catch (Exception ex)

            {

                return (false, $"Failed to clone dispatch: {ex.Message}", null);

            }

        }

        public static string? ValidateLoadingScheduleConflict(int scheduleId, string clientName, string destination, DateTime scheduleDate, string? departureTime = null)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (!string.IsNullOrWhiteSpace(departureTime))

                {

                    cmd.CommandText = @"SELECT ScheduleNumber, Status, DepartureTime FROM LoadingSchedules

                                        WHERE TRIM(LOWER(ClientName)) = TRIM(LOWER(@c))

                                          AND TRIM(LOWER(Destination)) = TRIM(LOWER(@d))

                                          AND DATE(ScheduleDate) = DATE(@sd)

                                          AND TRIM(LOWER(DepartureTime)) = TRIM(LOWER(@dt))

                                          AND Id <> @id

                                          AND Status IN ('Pending', 'Assigned', 'In Transit', 'Rescheduled')";

                    cmd.Parameters.AddWithValue("@dt", departureTime.Trim());

                }

                else

                {

                    cmd.CommandText = @"SELECT ScheduleNumber, Status, DepartureTime FROM LoadingSchedules

                                        WHERE TRIM(LOWER(ClientName)) = TRIM(LOWER(@c))

                                          AND TRIM(LOWER(Destination)) = TRIM(LOWER(@d))

                                          AND DATE(ScheduleDate) = DATE(@sd)

                                          AND Id <> @id

                                          AND Status IN ('Pending', 'Assigned', 'In Transit', 'Rescheduled')";

                }

                cmd.Parameters.AddWithValue("@c", clientName.Trim());

                cmd.Parameters.AddWithValue("@d", destination.Trim());

                cmd.Parameters.AddWithValue("@sd", scheduleDate.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("@id", scheduleId);

                using (var reader = cmd.ExecuteReader())

                {

                    if (reader.Read())

                    {

                        string sn = reader.GetString(0);

                        string status = reader.GetString(1);

                        string dt = reader.IsDBNull(2) ? "08:00 AM" : reader.GetString(2);

                        return $"DUPLICATE LOADING SCHEDULE CONFLICT:\nAn active schedule ({sn} - {status}) for '{clientName}' to '{destination}' at {dt} is already booked for {scheduleDate:MMM dd, yyyy}.\n\nIf this is a second batch/trip order for the same client today, please specify a different Departure Time.";

                    }

                }

            }

            return null;

        }

        public static List<TripReport> GetTripReports()

        {

            var list = new List<TripReport>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, TripReportNumber, DeliveryId, DispatchNumber, ArrivalTime, DepartureTime, DeliveredCrates, ReturnedCrates, StoreManagerName, Remarks, ReportDate, DeliveredGCrates, DeliveredECrates, ReturnedGCrates, ReturnedECrates, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName, TruckTimeIn, TruckTimeOut, DtrApprovedBy, ReturnCheckedBy, DtrPreparedBy, DrPreparedBy, DrCheckedBy, GuardName FROM TripReports WHERE COALESCE(IsVoid,0) = 0 ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime rDate = DateTime.TryParse(reader.GetString(10), out var dt) ? dt : DateTime.Today;

                        int delCrates = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                        int retCrates = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);

                        int delGCrate = reader.IsDBNull(11) ? 0 : reader.GetInt32(11);

                        int delECrate = reader.IsDBNull(12) ? 0 : reader.GetInt32(12);

                        int retGCrate = reader.IsDBNull(13) ? 0 : reader.GetInt32(13);

                        int retECrate = reader.IsDBNull(14) ? 0 : reader.GetInt32(14);

                        if (delCrates == 0 && (delGCrate > 0 || delECrate > 0)) delCrates = delGCrate + delECrate;

                        if (delGCrate == 0 && delECrate == 0 && delCrates > 0) delGCrate = delCrates;

                        if (retCrates == 0 && (retGCrate > 0 || retECrate > 0)) retCrates = retGCrate + retECrate;

                        if (retGCrate == 0 && retECrate == 0 && retCrates > 0) retGCrate = retCrates;

                        string sm = reader.IsDBNull(8) ? "" : reader.GetString(8);

                        string smln = reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetString(15) : "";

                        string smfn = reader.FieldCount > 16 && !reader.IsDBNull(16) ? reader.GetString(16) : "";

                        string smmn = reader.FieldCount > 17 && !reader.IsDBNull(17) ? reader.GetString(17) : "";

                        if (string.IsNullOrEmpty(smln) && string.IsNullOrEmpty(smfn) && !string.IsNullOrEmpty(sm))

                        {

                            var s = NameHelper.SplitFullName(sm);

                            smln = s.LastName;

                            smfn = s.FirstName;

                            smmn = s.MiddleName;

                        }

                        if (string.IsNullOrEmpty(sm))

                        {

                            sm = NameHelper.FormatFullName(smln, smfn, smmn);

                        }

                        list.Add(new TripReport

                        {

                            Id = reader.GetInt32(0),

                            TripReportNumber = reader.GetString(1),

                            DeliveryId = reader.GetInt32(2),

                            DispatchNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            ArrivalTime = reader.IsDBNull(4) ? "" : reader.GetString(4),

                            DepartureTime = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            DeliveredGCrates = delGCrate,

                            DeliveredECrates = delECrate,

                            DeliveredCrates = delCrates,

                            ReturnedGCrates = retGCrate,

                            ReturnedECrates = retECrate,

                            ReturnedCrates = retCrates,

                            StoreManagerName = sm,

                            Remarks = reader.IsDBNull(9) ? "" : reader.GetString(9),

                            ReportDate = rDate,

                            StoreManagerLastName = smln,

                            StoreManagerFirstName = smfn,

                            StoreManagerMiddleName = smmn,

                            TruckTimeIn = reader.FieldCount > 18 && !reader.IsDBNull(18) ? reader.GetString(18) : "2042",

                            TruckTimeOut = reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetString(19) : "0139",

                            DtrApprovedBy = reader.FieldCount > 20 && !reader.IsDBNull(20) ? reader.GetString(20) : "RAMIL MENDOZA",

                            ReturnCheckedBy = reader.FieldCount > 21 && !reader.IsDBNull(21) ? reader.GetString(21) : "",

                            DtrPreparedBy = reader.FieldCount > 22 && !reader.IsDBNull(22) ? reader.GetString(22) : "XERXES RYAN MANRIQUE",

                            DrPreparedBy = reader.FieldCount > 23 && !reader.IsDBNull(23) ? reader.GetString(23) : "VINJOHN VILLAMOR",

                            DrCheckedBy = reader.FieldCount > 24 && !reader.IsDBNull(24) ? reader.GetString(24) : "JOE-EM FLORES",

                            GuardName = reader.FieldCount > 25 && !reader.IsDBNull(25) ? reader.GetString(25) : ""

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<TripReport>> GetTripReportsAsync()

        {

            return await Task.Run(() => GetTripReports());

        }

        // ------------------------------------------------------------------ Crew pay per trip

        private static TripCrewPay ReadTripCrewPay(SqliteDataReader r) => new TripCrewPay

        {

            Id = r.GetInt32(0),

            DeliveryId = r.GetInt32(1),

            CrewId = r.GetInt32(2),

            CrewName = r.IsDBNull(3) ? "" : r.GetString(3),

            Role = r.IsDBNull(4) ? "" : r.GetString(4),

            BasePay = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetDouble(5)),

            Allowance = r.IsDBNull(6) ? 0 : Convert.ToDecimal(r.GetDouble(6)),

            Extra = r.IsDBNull(7) ? 0 : Convert.ToDecimal(r.GetDouble(7)),

            Deduction = r.IsDBNull(8) ? 0 : Convert.ToDecimal(r.GetDouble(8)),

            Notes = r.IsDBNull(9) ? "" : r.GetString(9)

        };

        private const string TripCrewPayColumns = "Id, DeliveryId, CrewId, CrewName, Role, BasePay, Allowance, Extra, Deduction, Notes";

        /// <summary>Pay recorded for the driver and aides of one dispatch (empty when none has been saved yet).</summary>

        public static List<TripCrewPay> GetTripCrewPay(int deliveryId)

        {

            var list = new List<TripCrewPay>();

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT {TripCrewPayColumns} FROM TripCrewPay WHERE DeliveryId = @d ORDER BY CASE WHEN Role = 'Driver' THEN 0 ELSE 1 END, Id";

                cmd.Parameters.AddWithValue("@d", deliveryId);

                using var r = cmd.ExecuteReader();

                while (r.Read()) list.Add(ReadTripCrewPay(r));

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetTripCrewPay: {ex.Message}"); }

            return list;

        }

        /// <summary>All recorded trip pay, keyed by (DeliveryId, CrewId).</summary>

        public static Dictionary<(int DeliveryId, int CrewId), TripCrewPay> GetAllTripCrewPay()

        {

            var map = new Dictionary<(int, int), TripCrewPay>();

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT {TripCrewPayColumns} FROM TripCrewPay";

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    var p = ReadTripCrewPay(r);

                    map[(p.DeliveryId, p.CrewId)] = p;

                }

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetAllTripCrewPay: {ex.Message}"); }

            return map;

        }

        /// <summary>Replaces the pay recorded for one dispatch's crew.</summary>

        public static void SaveTripCrewPay(int deliveryId, List<TripCrewPay> pay)

        {

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            using var tx = conn.BeginTransaction();

            using (var del = conn.CreateCommand())

            {

                del.Transaction = tx;

                del.CommandText = "DELETE FROM TripCrewPay WHERE DeliveryId = @d";

                del.Parameters.AddWithValue("@d", deliveryId);

                del.ExecuteNonQuery();

            }

            foreach (var p in pay.Where(x => x.CrewId > 0).GroupBy(x => x.CrewId).Select(g => g.First()))

            {

                using var ins = conn.CreateCommand();

                ins.Transaction = tx;

                ins.CommandText = @"INSERT INTO TripCrewPay (DeliveryId, CrewId, CrewName, Role, BasePay, Allowance, Extra, Deduction, Notes, UpdatedAt)

                                    VALUES (@d, @c, @n, @r, @b, @a, @e, @x, @no, datetime('now'))";

                ins.Parameters.AddWithValue("@d", deliveryId);

                ins.Parameters.AddWithValue("@c", p.CrewId);

                ins.Parameters.AddWithValue("@n", p.CrewName ?? "");

                ins.Parameters.AddWithValue("@r", p.Role ?? "");

                ins.Parameters.AddWithValue("@b", (double)p.BasePay);

                ins.Parameters.AddWithValue("@a", (double)p.Allowance);

                ins.Parameters.AddWithValue("@e", (double)p.Extra);

                ins.Parameters.AddWithValue("@x", (double)p.Deduction);

                ins.Parameters.AddWithValue("@no", p.Notes ?? "");

                ins.ExecuteNonQuery();

            }

            tx.Commit();

            try

            {

                LogAuditTrail("UPDATE", "TripCrewPay", deliveryId, $"Delivery {deliveryId}", "CrewPay", "", $"{pay.Count} crew, total PHP {pay.Sum(x => x.NetPay):N2}", "Trip crew pay saved");

            }

            catch { }

        }

        /// <summary>Crew IDs on a dispatch: the driver first, then each aide.</summary>

        public static List<(int CrewId, string Role)> GetDeliveryCrewIds(Delivery d)

        {

            var list = new List<(int, string)>();

            if (d == null) return list;

            if (d.DriverId > 0) list.Add((d.DriverId, "Driver"));

            var aideIds = new List<int>();

            foreach (var t in (d.AideIds ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))

                if (int.TryParse(t.Trim(), out int id) && id > 0 && !aideIds.Contains(id)) aideIds.Add(id);

            if (aideIds.Count == 0 && d.AideId > 0) aideIds.Add(d.AideId);

            foreach (var id in aideIds) if (id != d.DriverId) list.Add((id, "Aide"));

            return list;

        }

        /// <summary>

        /// What a crew member earns for one trip: the pay saved on that trip's report when there is one,

        /// otherwise their standard daily wage and food allowance.

        /// </summary>

        public static TripCrewPay ResolveTripPay(Crew crew, Delivery trip, Dictionary<(int DeliveryId, int CrewId), TripCrewPay>? recorded)

        {

            if (recorded != null && recorded.TryGetValue((trip.Id, crew.Id), out var p)) return p;

            return new TripCrewPay

            {

                DeliveryId = trip.Id,

                CrewId = crew.Id,

                CrewName = crew.FullName,

                Role = trip.DriverId == crew.Id ? "Driver" : "Aide",

                BasePay = crew.DailyWage,

                Allowance = crew.FoodAllowance

            };

        }

        public static void SaveTripReport(TripReport tr)

        {

            bool wasNew = tr.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var tx = conn.BeginTransaction())

                {

                    try

                    {

                        var cmd = conn.CreateCommand();

                        cmd.Transaction = tx;

                        tr.DeliveredCrates = tr.DeliveredGCrates + tr.DeliveredECrates;

                        tr.ReturnedCrates = tr.ReturnedGCrates + tr.ReturnedECrates;

                        if (string.IsNullOrWhiteSpace(tr.StoreManagerName))

                        {

                            tr.StoreManagerName = NameHelper.FormatFullName(tr.StoreManagerLastName, tr.StoreManagerFirstName, tr.StoreManagerMiddleName);

                        }

                        if (tr.Id == 0)

                        {

                            using (var dupCmd = conn.CreateCommand())

                            {

                                dupCmd.Transaction = tx;

                                dupCmd.CommandText = "SELECT TripReportNumber FROM TripReports WHERE DeliveryId = @did AND COALESCE(IsVoid,0) = 0 LIMIT 1";

                                dupCmd.Parameters.AddWithValue("@did", tr.DeliveryId);

                                var existingTr = dupCmd.ExecuteScalar();

                                if (existingTr != null && existingTr != DBNull.Value)

                                {

                                    throw new InvalidOperationException($"A trip report (#{existingTr}) already exists for this dispatch. Open and edit that report instead of filing a new one.");

                                }

                            }

                            using (var stCmd = conn.CreateCommand())

                            {

                                stCmd.Transaction = tx;

                                stCmd.CommandText = "SELECT Status FROM Deliveries WHERE Id = @did";

                                stCmd.Parameters.AddWithValue("@did", tr.DeliveryId);

                                string delStatus = stCmd.ExecuteScalar()?.ToString() ?? "";

                                if (delStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                                {

                                    throw new InvalidOperationException("This dispatch was cancelled. A trip report cannot be filed for a cancelled dispatch.");

                                }

                                if (!delStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !delStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase))

                                {

                                    throw new InvalidOperationException($"This dispatch is still \"{delStatus}\". Mark it completed in Dispatch before filing its trip report.");

                                }

                            }

                            tr.TripReportNumber =$"TRIP-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";

                            cmd.CommandText = @"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, ArrivalTime, DepartureTime, DeliveredGCrates, DeliveredECrates, DeliveredCrates, ReturnedGCrates, ReturnedECrates, ReturnedCrates, StoreManagerName, Remarks, ReportDate, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName, TruckTimeIn, TruckTimeOut, DtrApprovedBy, ReturnCheckedBy, DtrPreparedBy, DrPreparedBy, DrCheckedBy, GuardName)

                                                VALUES (@trn, @did, @dn, @at, @dt, @dgc, @dec, @dc, @rgc, @rec, @rc, @sm, @r, @rd, @smln, @smfn, @smmn, @tti, @tto, @dtra, @rcb, @dtrp, @drp, @drc, @gn)";

                            var updateDel = conn.CreateCommand();

                            updateDel.Transaction = tx;

                            updateDel.CommandText = "UPDATE Deliveries SET Status='Completed' WHERE Id=@did";

                            updateDel.Parameters.AddWithValue("@did", tr.DeliveryId);

                            updateDel.ExecuteNonQuery();

                            var releaseTr = conn.CreateCommand();

                            releaseTr.Transaction = tx;

                            releaseTr.CommandText = $"UPDATE Trucks SET Status='Active' WHERE Id=(SELECT TruckId FROM Deliveries WHERE Id=@did) AND Status NOT IN {BlockedTruckStatusesSql}";

                            releaseTr.Parameters.AddWithValue("@did", tr.DeliveryId);

                            releaseTr.ExecuteNonQuery();

                            var releaseDriver = conn.CreateCommand();

                            releaseDriver.Transaction = tx;

                            releaseDriver.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id=(SELECT DriverId FROM Deliveries WHERE Id=@did)";

                            releaseDriver.Parameters.AddWithValue("@did", tr.DeliveryId);

                            releaseDriver.ExecuteNonQuery();

                            var getAidesCmd = conn.CreateCommand();

                            getAidesCmd.Transaction = tx;

                            getAidesCmd.CommandText = "SELECT AideId, AideIds FROM Deliveries WHERE Id=@did";

                            getAidesCmd.Parameters.AddWithValue("@did", tr.DeliveryId);

                            using (var r = getAidesCmd.ExecuteReader())

                            {

                                if (r.Read())

                                {

                                    int singleAideId = r.GetInt32(0);

                                    string multiAideIds = r.IsDBNull(1) ? "" : r.GetString(1);

                                    var tokens = multiAideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                                    var idsToRelease = new HashSet<int>();

                                    if (singleAideId > 0) idsToRelease.Add(singleAideId);

                                    foreach (var t in tokens)

                                    {

                                        if (int.TryParse(t.Trim(), out int parsed) && parsed > 0) idsToRelease.Add(parsed);

                                    }

                                    foreach (int aideId in idsToRelease)

                                    {

                                        var releaseAideCmd = conn.CreateCommand();

                                        releaseAideCmd.Transaction = tx;

                                        releaseAideCmd.CommandText = "UPDATE Crews SET IsAvailable=1 WHERE Id=@aid";

                                        releaseAideCmd.Parameters.AddWithValue("@aid", aideId);

                                        releaseAideCmd.ExecuteNonQuery();

                                    }

                                }

                            }

                            var updateLs = conn.CreateCommand();

                            updateLs.Transaction = tx;

                            updateLs.CommandText = "UPDATE LoadingSchedules SET Status='Completed' WHERE Id=(SELECT LoadingScheduleId FROM Deliveries WHERE Id=@did)";

                            updateLs.Parameters.AddWithValue("@did", tr.DeliveryId);

                            updateLs.ExecuteNonQuery();

                            var updateStopsCmd = conn.CreateCommand();

                            updateStopsCmd.Transaction = tx;

                            updateStopsCmd.CommandText = "UPDATE DispatchStops SET Status='Delivered' WHERE DeliveryId=@did AND Status='Pending'";

                            updateStopsCmd.Parameters.AddWithValue("@did", tr.DeliveryId);

                            updateStopsCmd.ExecuteNonQuery();

                            var updateStopsLs = conn.CreateCommand();

                            updateStopsLs.Transaction = tx;

                            updateStopsLs.CommandText = "UPDATE LoadingSchedules SET Status='Completed' WHERE Id IN (SELECT LoadingScheduleId FROM DispatchStops WHERE DeliveryId=@did AND LoadingScheduleId IS NOT NULL)";

                            updateStopsLs.Parameters.AddWithValue("@did", tr.DeliveryId);

                            updateStopsLs.ExecuteNonQuery();

                        }

                        else

                        {

                            cmd.CommandText = @"UPDATE TripReports SET ArrivalTime=@at, DepartureTime=@dt, DeliveredGCrates=@dgc, DeliveredECrates=@dec, DeliveredCrates=@dc, ReturnedGCrates=@rgc, ReturnedECrates=@rec, ReturnedCrates=@rc, StoreManagerName=@sm, Remarks=@r, StoreManagerLastName=@smln, StoreManagerFirstName=@smfn, StoreManagerMiddleName=@smmn, TruckTimeIn=@tti, TruckTimeOut=@tto, DtrApprovedBy=@dtra, ReturnCheckedBy=@rcb, DtrPreparedBy=@dtrp, DrPreparedBy=@drp, DrCheckedBy=@drc, GuardName=@gn WHERE Id=@id";

                            cmd.Parameters.AddWithValue("@id", tr.Id);

                        }

                        cmd.Parameters.AddWithValue("@trn", tr.TripReportNumber);

                        cmd.Parameters.AddWithValue("@did", tr.DeliveryId);

                        cmd.Parameters.AddWithValue("@dn", tr.DispatchNumber);

                        cmd.Parameters.AddWithValue("@at", tr.ArrivalTime);

                        cmd.Parameters.AddWithValue("@dt", tr.DepartureTime);

                        cmd.Parameters.AddWithValue("@dgc", tr.DeliveredGCrates);

                        cmd.Parameters.AddWithValue("@dec", tr.DeliveredECrates);

                        cmd.Parameters.AddWithValue("@dc", tr.DeliveredCrates);

                        cmd.Parameters.AddWithValue("@rgc", tr.ReturnedGCrates);

                        cmd.Parameters.AddWithValue("@rec", tr.ReturnedECrates);

                        cmd.Parameters.AddWithValue("@rc", tr.ReturnedCrates);

                        cmd.Parameters.AddWithValue("@sm", tr.StoreManagerName);

                        cmd.Parameters.AddWithValue("@r", tr.Remarks);

                        cmd.Parameters.AddWithValue("@rd", tr.ReportDate.ToString("yyyy-MM-dd"));

                        cmd.Parameters.AddWithValue("@smln", (tr.StoreManagerLastName ?? "").Trim());

                        cmd.Parameters.AddWithValue("@smfn", (tr.StoreManagerFirstName ?? "").Trim());

                        cmd.Parameters.AddWithValue("@smmn", (tr.StoreManagerMiddleName ?? "").Trim());

                        cmd.Parameters.AddWithValue("@tti", tr.TruckTimeIn ?? "2042");

                        cmd.Parameters.AddWithValue("@tto", tr.TruckTimeOut ?? "0139");

                        cmd.Parameters.AddWithValue("@dtra", tr.DtrApprovedBy ?? "RAMIL MENDOZA");

                        cmd.Parameters.AddWithValue("@rcb", tr.ReturnCheckedBy ?? "");

                        cmd.Parameters.AddWithValue("@dtrp", tr.DtrPreparedBy ?? "XERXES RYAN MANRIQUE");

                        cmd.Parameters.AddWithValue("@drp", tr.DrPreparedBy ?? "VINJOHN VILLAMOR");

                        cmd.Parameters.AddWithValue("@drc", tr.DrCheckedBy ?? "JOE-EM FLORES");

                        cmd.Parameters.AddWithValue("@gn", tr.GuardName ?? "");

                        cmd.ExecuteNonQuery();

                        ReconcileResourceStatuses(conn, tx);

                        tx.Commit();

                        try

                        {

                            bool isNew = wasNew;

                            LogAuditTrail(isNew ? "INSERT" : "UPDATE", "TripReports", tr.Id, tr.TripReportNumber, "Delivered/Returned", "", $"Delivered: {tr.DeliveredCrates}, Returned: {tr.ReturnedCrates}", $"Trip report for Dispatch #{tr.DispatchNumber} {(isNew ? "filed" : "updated")}");

                            EventNotificationService.Publish(isNew ? "Trip Report Saved" : "Trip Report Updated", $"Report #{tr.TripReportNumber} for dispatch #{tr.DispatchNumber}", ToastType.Success);

                        }

                        catch { }

                    }

                    catch

                    {

                        tx.Rollback();

                        throw;

                    }

                }

            }

        }

        public static List<IncidentReport> GetIncidentReports()

        {

            var list = new List<IncidentReport>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, DeliveryId, DispatchNumber, IncidentDate, DamagedQuantity, EstimatedCost, Cause, ChargeableToCrew, Description, Status, PhotoPath FROM IncidentReports WHERE COALESCE(Status,'') <> 'Voided' ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime iDate = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.Today;

                        list.Add(new IncidentReport

                        {

                            Id = reader.GetInt32(0),

                            DeliveryId = reader.GetInt32(1),

                            DispatchNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),

                            IncidentDate = iDate,

                            DamagedQuantity = reader.GetInt32(4),

                            EstimatedCost = Convert.ToDecimal(reader.GetDouble(5)),

                            Cause = reader.IsDBNull(6) ? "" : reader.GetString(6),

                            ChargeableToCrew = reader.GetInt32(7) == 1,

                            Description = reader.IsDBNull(8) ? "" : reader.GetString(8),

                            Status = reader.GetString(9),

                            PhotoPath = reader.IsDBNull(10) ? "" : reader.GetString(10)

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<IncidentReport>> GetIncidentReportsAsync()

        {

            return await Task.Run(() => GetIncidentReports());

        }

        public static void SaveIncidentReport(IncidentReport ir)

        {

            bool wasNew = ir.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (ir.Id == 0)

                {

                    var checkCmd = conn.CreateCommand();

                    checkCmd.CommandText = "SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @did AND Cause = @cause AND DATE(IncidentDate) = DATE(@idate) AND COALESCE(Status,'') <> 'Voided'";

                    checkCmd.Parameters.AddWithValue("@did", ir.DeliveryId);

                    checkCmd.Parameters.AddWithValue("@cause", ir.Cause.Trim());

                    checkCmd.Parameters.AddWithValue("@idate", ir.IncidentDate.ToString("yyyy-MM-dd"));

                    long existing = (long)(checkCmd.ExecuteScalar() ?? 0);

                    if (existing > 0)

                    {

                        throw new InvalidOperationException($"An incident report for dispatch with cause '{ir.Cause}' on {ir.IncidentDate:yyyy-MM-dd} is already logged.");

                    }

                    cmd.CommandText = @"INSERT INTO IncidentReports (DeliveryId, DispatchNumber, IncidentDate, DamagedQuantity, EstimatedCost, Cause, ChargeableToCrew, Description, Status, PhotoPath)

                                        VALUES (@did, @dn, @incDate, @dq, @ec, @c, @ch, @desc, @s, @photo)";

                }

                else

                {

                    cmd.CommandText = @"UPDATE IncidentReports SET IncidentDate=@incDate, DamagedQuantity=@dq, EstimatedCost=@ec, Cause=@c, ChargeableToCrew=@ch, Description=@desc, Status=@s, PhotoPath=@photo WHERE Id=@id";

                    cmd.Parameters.AddWithValue("@id", ir.Id);

                }

                cmd.Parameters.AddWithValue("@did", ir.DeliveryId);

                cmd.Parameters.AddWithValue("@dn", ir.DispatchNumber);

                cmd.Parameters.AddWithValue("@incDate", ir.IncidentDate.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("@dq", ir.DamagedQuantity);

                cmd.Parameters.AddWithValue("@ec", ir.EstimatedCost);

                cmd.Parameters.AddWithValue("@c", ir.Cause);

                cmd.Parameters.AddWithValue("@ch", ir.ChargeableToCrew ? 1 : 0);

                cmd.Parameters.AddWithValue("@desc", ir.Description);

                bool isNew = wasNew;

                cmd.Parameters.AddWithValue("@s", ir.Status);

                cmd.Parameters.AddWithValue("@photo", ir.PhotoPath ?? "");

                cmd.ExecuteNonQuery();

                if (isNew)

                {

                    using var idCmd = conn.CreateCommand();

                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    ir.Id = Convert.ToInt32(idCmd.ExecuteScalar());

                }

                LogAuditTrail(isNew ? "ADD" : "UPDATE", "IncidentReports", ir.Id, ir.DispatchNumber, "Cause", "", ir.Cause, $"Incident for Dispatch #{ir.DispatchNumber}: {ir.Cause} (Damaged: {ir.DamagedQuantity}, Cost: PHP {ir.EstimatedCost:N2}) {(isNew ? "filed" : "updated")}");

            }

            try

            {

                bool isNew = wasNew;

                EventNotificationService.Publish(isNew ? "Incident Logged" : "Incident Updated", $"Dispatch #{ir.DispatchNumber}: {ir.Cause} ({ir.DamagedQuantity} damaged crates)", ToastType.Warning);

            }

            catch { }

        }

        public static List<Invoice> GetInvoices()

        {

            UpdateOverdueInvoices();

            var list = new List<Invoice>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT i.Id, i.InvoiceNumber, i.SoaId, i.DeliveryId, i.ClientName, i.IssueDate, i.DueDate, i.TotalAmount, i.Deductions, i.NetAmount, i.Status, COALESCE(d.DispatchNumber, ''), COALESCE(ls.ScheduleNumber, '') FROM Invoices i LEFT JOIN Deliveries d ON i.DeliveryId = d.Id LEFT JOIN LoadingSchedules ls ON d.LoadingScheduleId = ls.Id ORDER BY i.Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime issDate = DateTime.TryParse(reader.GetString(5), out var d1) ? d1 : DateTime.Today;

                        DateTime dueDate = DateTime.TryParse(reader.GetString(6), out var d2) ? d2 : DateTime.Today.AddDays(15);

                        list.Add(new Invoice

                        {

                            Id = reader.GetInt32(0),

                            InvoiceNumber = reader.GetString(1),

                            SoaId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),

                            DeliveryId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),

                            ClientName = reader.GetString(4),

                            IssueDate = issDate,

                            DueDate = dueDate,

                            TotalAmount = Convert.ToDecimal(reader.GetDouble(7)),

                            Deductions = Convert.ToDecimal(reader.GetDouble(8)),

                            NetAmount = Convert.ToDecimal(reader.GetDouble(9)),

                            Status = reader.GetString(10),

                            DispatchNumber = reader.IsDBNull(11) ? "" : reader.GetString(11),

                            LoadingScheduleNumber = reader.IsDBNull(12) ? "" : reader.GetString(12)

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<Invoice>> GetInvoicesAsync()

        {

            return await Task.Run(() => GetInvoices());

        }

        public static string GenerateNextInvoiceNumber()

        {

            string datePart = DateTime.Today.ToString("yyyyMMdd");

            string prefix = $"INV-{datePart}-";

            int next = 1;

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "SELECT InvoiceNumber FROM Invoices WHERE InvoiceNumber LIKE @pfx";

                    cmd.Parameters.AddWithValue("@pfx", prefix + "%");

                    using var r = cmd.ExecuteReader();

                    while (r.Read())

                    {

                        string tail = r.GetString(0).Substring(prefix.Length);

                        if (tail.Length <= 6 && int.TryParse(tail, out int n) && n >= next) next = n + 1;

                    }

                }

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] GenerateNextInvoiceNumber: {ex.Message}"); }

            return $"{prefix}{next:D4}";

        }

        public static void SaveInvoice(Invoice inv)

        {

            bool wasNew = inv.Id <= 0;

            if (wasNew && inv.DeliveryId > 0)

            {

                var d = GetDeliveryById(inv.DeliveryId);

                if (d != null && !string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) && !string.Equals(d.Status, "Delivered", StringComparison.OrdinalIgnoreCase))

                    throw new InvalidOperationException($"Dispatch {d.DispatchNumber} is still \"{d.Status}\". Mark it completed in Dispatch before billing it.");

            }

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                if (inv.Id == 0)

                {

                    if (string.IsNullOrWhiteSpace(inv.InvoiceNumber))

                    {

                        inv.InvoiceNumber = GenerateNextInvoiceNumber();

                    }

                    cmd.CommandText = @"INSERT INTO Invoices (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status)

                                        VALUES (@in, @soa, @did, @c, @id, @dd, @ta, @d, @na, @s);

                                        SELECT last_insert_rowid();";

                }

                else

                {

                    cmd.CommandText = @"UPDATE Invoices SET InvoiceNumber=@in, SoaId=@soa, DeliveryId=@did, ClientName=@c, IssueDate=@id, DueDate=@dd, TotalAmount=@ta, Deductions=@d, NetAmount=@na, Status=@s WHERE Id=@invId";

                    cmd.Parameters.AddWithValue("@invId", inv.Id);

                }

                cmd.Parameters.AddWithValue("@in", inv.InvoiceNumber);

                cmd.Parameters.AddWithValue("@soa", inv.SoaId);

                cmd.Parameters.AddWithValue("@did", inv.DeliveryId);

                cmd.Parameters.AddWithValue("@c", inv.ClientName);

                cmd.Parameters.AddWithValue("@id", inv.IssueDate.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("@dd", inv.DueDate.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("@ta", inv.TotalAmount);

                cmd.Parameters.AddWithValue("@d", inv.Deductions);

                cmd.Parameters.AddWithValue("@na", inv.NetAmount);

                cmd.Parameters.AddWithValue("@s", inv.Status);

                var invIdObj = cmd.ExecuteScalar();

                if (wasNew && invIdObj != null && invIdObj != DBNull.Value) inv.Id = Convert.ToInt32(invIdObj);

                if (!wasNew) RecalculateInvoiceStatus(conn, null, inv.Id);

            }

            try

            {

                bool isNew = wasNew;

                LogAuditTrail(isNew ? "INVOICE_CREATED" : "UPDATE", "Invoices", inv.Id, inv.InvoiceNumber, "NetAmount", "", inv.NetAmount.ToString("F2"), $"Invoice {(isNew ? "created" : "updated")} for {inv.ClientName}, Status: {inv.Status}, Net: PHP {inv.NetAmount:N2}");

                EventNotificationService.Publish(isNew ? "Invoice Created" : "Invoice Updated", $"Invoice #{inv.InvoiceNumber} for {inv.ClientName} (PHP {inv.NetAmount:N2})", ToastType.Success);

            }

            catch { }

        }

        public static List<StatementOfAccount> GetStatementsOfAccount()

        {

            var list = new List<StatementOfAccount>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT Id, SoaNumber, ClientName, PeriodCovered, IssueDate, TotalAmount, Status,

                                           COALESCE(ClientAddress, ''), COALESCE(ClientTin, ''), COALESCE(DeliveryType, 'REF'),

                                           COALESCE(TotalSalesVatInc, 0), COALESCE(VatAmount, 0), COALESCE(NetOfVat, 0),

                                           COALESCE(FuelSurchargeTotal, 0), COALESCE(WithholdingTax, 0), COALESCE(SalesInvoiceNo, ''),

                                           COALESCE(PreparedBy, 'Marie Ann J. Billones'), COALESCE(ReceivedBy1, ''), COALESCE(ReceivedBy2, '')

                                    FROM StatementsOfAccount ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime issDate = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Today;

                        list.Add(new StatementOfAccount

                        {

                            Id = reader.GetInt32(0),

                            SoaNumber = reader.GetString(1),

                            ClientName = reader.GetString(2),

                            PeriodCovered = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            IssueDate = issDate,

                            TotalAmount = Convert.ToDecimal(reader.GetDouble(5)),

                            Status = reader.GetString(6),

                            ClientAddress = reader.GetString(7),

                            ClientTin = reader.GetString(8),

                            DeliveryType = reader.GetString(9),

                            TotalSalesVatInc = Convert.ToDecimal(reader.GetDouble(10)),

                            VatAmount = Convert.ToDecimal(reader.GetDouble(11)),

                            NetOfVat = Convert.ToDecimal(reader.GetDouble(12)),

                            FuelSurchargeTotal = Convert.ToDecimal(reader.GetDouble(13)),

                            WithholdingTax = Convert.ToDecimal(reader.GetDouble(14)),

                            SalesInvoiceNo = reader.GetString(15),

                            PreparedBy = reader.GetString(16),

                            ReceivedBy1 = reader.GetString(17),

                            ReceivedBy2 = reader.GetString(18)

                        });

                    }

                }

            }

            return list;

        }

        public static StatementOfAccount? GetStatementOfAccountById(int id)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT Id, SoaNumber, ClientName, PeriodCovered, IssueDate, TotalAmount, Status,

                                           COALESCE(ClientAddress, ''), COALESCE(ClientTin, ''), COALESCE(DeliveryType, 'REF'),

                                           COALESCE(TotalSalesVatInc, 0), COALESCE(VatAmount, 0), COALESCE(NetOfVat, 0),

                                           COALESCE(FuelSurchargeTotal, 0), COALESCE(WithholdingTax, 0), COALESCE(SalesInvoiceNo, ''),

                                           COALESCE(PreparedBy, 'Marie Ann J. Billones'), COALESCE(ReceivedBy1, ''), COALESCE(ReceivedBy2, '')

                                    FROM StatementsOfAccount WHERE Id = @id";

                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = cmd.ExecuteReader())

                {

                    if (reader.Read())

                    {

                        DateTime issDate = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Today;

                        var soa = new StatementOfAccount

                        {

                            Id = reader.GetInt32(0),

                            SoaNumber = reader.GetString(1),

                            ClientName = reader.GetString(2),

                            PeriodCovered = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            IssueDate = issDate,

                            TotalAmount = Convert.ToDecimal(reader.GetDouble(5)),

                            Status = reader.GetString(6),

                            ClientAddress = reader.GetString(7),

                            ClientTin = reader.GetString(8),

                            DeliveryType = reader.GetString(9),

                            TotalSalesVatInc = Convert.ToDecimal(reader.GetDouble(10)),

                            VatAmount = Convert.ToDecimal(reader.GetDouble(11)),

                            NetOfVat = Convert.ToDecimal(reader.GetDouble(12)),

                            FuelSurchargeTotal = Convert.ToDecimal(reader.GetDouble(13)),

                            WithholdingTax = Convert.ToDecimal(reader.GetDouble(14)),

                            SalesInvoiceNo = reader.GetString(15),

                            PreparedBy = reader.GetString(16),

                            ReceivedBy1 = reader.GetString(17),

                            ReceivedBy2 = reader.GetString(18)

                        };

                        soa.Items = GetStatementOfAccountItems(soa.Id);

                        return soa;

                    }

                }

            }

            return null;

        }

        /// <summary>Dispatches already listed on a statement of account (other than the one being edited), mapped to that statement's number.</summary>

        public static Dictionary<int, string> GetBilledDeliveryIds(int excludeSoaId = 0)

        {

            var map = new Dictionary<int, string>();

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT i.DeliveryId, COALESCE(s.SoaNumber, '') FROM StatementOfAccountItems i

                                    LEFT JOIN StatementsOfAccount s ON s.Id = i.SoaId

                                    WHERE i.DeliveryId > 0 AND i.SoaId <> @ex";

                cmd.Parameters.AddWithValue("@ex", excludeSoaId);

                using var r = cmd.ExecuteReader();

                while (r.Read())

                {

                    int id = r.GetInt32(0);

                    if (!map.ContainsKey(id)) map[id] = r.GetString(1);

                }

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetBilledDeliveryIds: {ex.Message}"); }

            return map;

        }

        public static List<StatementOfAccountItem> GetStatementOfAccountItems(int soaId)

        {

            var list = new List<StatementOfAccountItem>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT Id, SoaId, TripNumber, COALESCE(DateReceived, ''), COALESCE(Zone, ''),

                                           COALESCE(DrNumbers, ''), COALESCE(Destination, ''), COALESCE(DeliveryType, 'REF'),

                                           COALESCE(PlateNumber, ''), COALESCE(Tonnage, '6T'), DropCount, Amount, FuelSurcharge,

                                           COALESCE(DriverHelper, ''), DriverWage, HelperWage, DistanceKm, DieselCost, Excess, DeliveryId

                                    FROM StatementOfAccountItems WHERE SoaId = @soaId ORDER BY TripNumber ASC, Id ASC";

                cmd.Parameters.AddWithValue("@soaId", soaId);

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(new StatementOfAccountItem

                        {

                            Id = reader.GetInt32(0),

                            SoaId = reader.GetInt32(1),

                            TripNumber = reader.GetInt32(2),

                            DateReceived = reader.GetString(3),

                            Zone = reader.GetString(4),

                            DrNumbers = reader.GetString(5),

                            Destination = reader.GetString(6),

                            DeliveryType = reader.GetString(7),

                            PlateNumber = reader.GetString(8),

                            Tonnage = reader.GetString(9),

                            DropCount = reader.GetInt32(10),

                            Amount = Convert.ToDecimal(reader.GetDouble(11)),

                            FuelSurcharge = Convert.ToDecimal(reader.GetDouble(12)),

                            DriverHelper = reader.GetString(13),

                            DriverWage = Convert.ToDecimal(reader.GetDouble(14)),

                            HelperWage = Convert.ToDecimal(reader.GetDouble(15)),

                            DistanceKm = Convert.ToDecimal(reader.GetDouble(16)),

                            DieselCost = Convert.ToDecimal(reader.GetDouble(17)),

                            Excess = Convert.ToDecimal(reader.GetDouble(18)),

                            DeliveryId = reader.GetInt32(19)

                        });

                    }

                }

            }

            return list;

        }

        public static string GenerateNextSoaNumber()

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                string yearPrefix = $"SOA-{DateTime.Today.Year}-";

                cmd.CommandText = "SELECT SoaNumber FROM StatementsOfAccount WHERE SoaNumber LIKE @pfx";

                cmd.Parameters.AddWithValue("@pfx", yearPrefix + "%");

                int next = 1;

                using (var r = cmd.ExecuteReader())

                {

                    while (r.Read())

                    {

                        string tail = r.GetString(0).Substring(yearPrefix.Length);

                        if (tail.Length <= 6 && int.TryParse(tail, out int n) && n >= next) next = n + 1;

                    }

                }

                return $"{yearPrefix}{next:D4}";

            }

        }

        public static int SaveStatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var tx = conn.BeginTransaction())

                {

                    try

                    {

                        if (string.IsNullOrWhiteSpace(soa.SoaNumber))

                        {

                            soa.SoaNumber = GenerateNextSoaNumber();

                        }

                        var cmd = conn.CreateCommand();

                        cmd.Transaction = tx;

                        if (soa.Id == 0)

                        {

                            cmd.CommandText = @"INSERT INTO StatementsOfAccount

                                (SoaNumber, ClientName, ClientAddress, ClientTin, PeriodCovered, IssueDate, DeliveryType,

                                 TotalSalesVatInc, VatAmount, NetOfVat, FuelSurchargeTotal, WithholdingTax, TotalAmount,

                                 SalesInvoiceNo, PreparedBy, ReceivedBy1, ReceivedBy2, Status)

                                VALUES

                                (@sn, @cn, @ca, @ct, @pc, @id, @dt,

                                 @ts, @va, @nv, @fs, @wt, @ta,

                                 @si, @pb, @r1, @r2, @st);

                                SELECT last_insert_rowid();";

                        }

                        else

                        {

                            cmd.CommandText = @"UPDATE StatementsOfAccount SET

                                ClientName=@cn, ClientAddress=@ca, ClientTin=@ct, PeriodCovered=@pc, IssueDate=@id, DeliveryType=@dt,

                                TotalSalesVatInc=@ts, VatAmount=@va, NetOfVat=@nv, FuelSurchargeTotal=@fs, WithholdingTax=@wt, TotalAmount=@ta,

                                SalesInvoiceNo=@si, PreparedBy=@pb, ReceivedBy1=@r1, ReceivedBy2=@r2, Status=@st

                                WHERE Id=@rowId;

                                SELECT @rowId;";

                            cmd.Parameters.AddWithValue("@rowId", soa.Id);

                        }

                        cmd.Parameters.AddWithValue("@sn", soa.SoaNumber);

                        cmd.Parameters.AddWithValue("@cn", soa.ClientName);

                        cmd.Parameters.AddWithValue("@ca", soa.ClientAddress);

                        cmd.Parameters.AddWithValue("@ct", soa.ClientTin);

                        cmd.Parameters.AddWithValue("@pc", soa.PeriodCovered);

                        cmd.Parameters.AddWithValue("@id", soa.IssueDate.ToString("yyyy-MM-dd"));

                        cmd.Parameters.AddWithValue("@dt", soa.DeliveryType);

                        cmd.Parameters.AddWithValue("@ts", soa.TotalSalesVatInc);

                        cmd.Parameters.AddWithValue("@va", soa.VatAmount);

                        cmd.Parameters.AddWithValue("@nv", soa.NetOfVat);

                        cmd.Parameters.AddWithValue("@fs", soa.FuelSurchargeTotal);

                        cmd.Parameters.AddWithValue("@wt", soa.WithholdingTax);

                        cmd.Parameters.AddWithValue("@ta", soa.TotalAmount);

                        cmd.Parameters.AddWithValue("@si", soa.SalesInvoiceNo);

                        cmd.Parameters.AddWithValue("@pb", soa.PreparedBy);

                        cmd.Parameters.AddWithValue("@r1", soa.ReceivedBy1);

                        cmd.Parameters.AddWithValue("@r2", soa.ReceivedBy2);

                        cmd.Parameters.AddWithValue("@st", soa.Status);

                        int soaId = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                        soa.Id = soaId;

                        var delItemsCmd = conn.CreateCommand();

                        delItemsCmd.Transaction = tx;

                        delItemsCmd.CommandText = "DELETE FROM StatementOfAccountItems WHERE SoaId = @sid";

                        delItemsCmd.Parameters.AddWithValue("@sid", soaId);

                        delItemsCmd.ExecuteNonQuery();

                        if (items != null && items.Count > 0)

                        {

                            foreach (var itm in items)

                            {

                                var insItm = conn.CreateCommand();

                                insItm.Transaction = tx;

                                insItm.CommandText = @"INSERT INTO StatementOfAccountItems

                                    (SoaId, TripNumber, DateReceived, Zone, DrNumbers, Destination, DeliveryType, PlateNumber,

                                     Tonnage, DropCount, Amount, FuelSurcharge, DriverHelper, DriverWage, HelperWage,

                                     DistanceKm, DieselCost, Excess, DeliveryId)

                                    VALUES

                                    (@sid, @tn, @dr, @zn, @dn, @dst, @dt, @pn,

                                     @ton, @dc, @amt, @fsc, @dh, @dw, @hw,

                                     @dist, @dsc, @exs, @did);";

                                insItm.Parameters.AddWithValue("@sid", soaId);

                                insItm.Parameters.AddWithValue("@tn", itm.TripNumber);

                                insItm.Parameters.AddWithValue("@dr", itm.DateReceived);

                                insItm.Parameters.AddWithValue("@zn", itm.Zone);

                                insItm.Parameters.AddWithValue("@dn", itm.DrNumbers);

                                insItm.Parameters.AddWithValue("@dst", itm.Destination);

                                insItm.Parameters.AddWithValue("@dt", itm.DeliveryType);

                                insItm.Parameters.AddWithValue("@pn", itm.PlateNumber);

                                insItm.Parameters.AddWithValue("@ton", itm.Tonnage);

                                insItm.Parameters.AddWithValue("@dc", itm.DropCount);

                                insItm.Parameters.AddWithValue("@amt", itm.Amount);

                                insItm.Parameters.AddWithValue("@fsc", itm.FuelSurcharge);

                                insItm.Parameters.AddWithValue("@dh", itm.DriverHelper);

                                insItm.Parameters.AddWithValue("@dw", itm.DriverWage);

                                insItm.Parameters.AddWithValue("@hw", itm.HelperWage);

                                insItm.Parameters.AddWithValue("@dist", itm.DistanceKm);

                                insItm.Parameters.AddWithValue("@dsc", itm.DieselCost);

                                insItm.Parameters.AddWithValue("@exs", itm.Excess);

                                insItm.Parameters.AddWithValue("@did", itm.DeliveryId);

                                insItm.ExecuteNonQuery();

                            }

                        }

                        if (!string.IsNullOrWhiteSpace(soa.SalesInvoiceNo))

                        {

                            string rawSi = soa.SalesInvoiceNo.Trim();

                            string invNum = rawSi.StartsWith("SI", StringComparison.OrdinalIgnoreCase) ? rawSi : $"SI-{rawSi}";

                            decimal gross = soa.TotalSalesVatInc > 0 ? soa.TotalSalesVatInc : soa.TotalAmount;

                            decimal ded = soa.WithholdingTax;

                            decimal net = soa.TotalAmount;

                            var chkInvCmd = conn.CreateCommand();

                            chkInvCmd.Transaction = tx;

                            chkInvCmd.CommandText = "SELECT Id FROM Invoices WHERE SoaId = @sid OR InvoiceNumber = @invNum LIMIT 1";

                            chkInvCmd.Parameters.AddWithValue("@sid", soaId);

                            chkInvCmd.Parameters.AddWithValue("@invNum", invNum);

                            var existingInvObj = chkInvCmd.ExecuteScalar();

                            if (existingInvObj != null && existingInvObj != DBNull.Value)

                            {

                                int existingInvId = Convert.ToInt32(existingInvObj);

                                var updInvCmd = conn.CreateCommand();

                                updInvCmd.Transaction = tx;

                                updInvCmd.CommandText = @"UPDATE Invoices SET

                                    InvoiceNumber = @invNum, ClientName = @cn, IssueDate = @id, DueDate = @dd,

                                    TotalAmount = @ta, Deductions = @ded, NetAmount = @net, SoaId = @sid

                                    WHERE Id = @iid";

                                updInvCmd.Parameters.AddWithValue("@invNum", invNum);

                                updInvCmd.Parameters.AddWithValue("@cn", soa.ClientName);

                                updInvCmd.Parameters.AddWithValue("@id", soa.IssueDate.ToString("yyyy-MM-dd"));

                                updInvCmd.Parameters.AddWithValue("@dd", soa.IssueDate.AddDays(15).ToString("yyyy-MM-dd"));

                                updInvCmd.Parameters.AddWithValue("@ta", gross);

                                updInvCmd.Parameters.AddWithValue("@ded", ded);

                                updInvCmd.Parameters.AddWithValue("@net", net);

                                updInvCmd.Parameters.AddWithValue("@sid", soaId);

                                updInvCmd.Parameters.AddWithValue("@iid", existingInvId);

                                updInvCmd.ExecuteNonQuery();

                                RecalculateInvoiceStatus(conn, tx, existingInvId);

                            }

                            else

                            {

                                var insInvCmd = conn.CreateCommand();

                                insInvCmd.Transaction = tx;

                                insInvCmd.CommandText = @"INSERT INTO Invoices

                                    (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status)

                                    VALUES

                                    (@invNum, @sid, 0, @cn, @id, @dd, @ta, @ded, @net, 'Unpaid');";

                                insInvCmd.Parameters.AddWithValue("@invNum", invNum);

                                insInvCmd.Parameters.AddWithValue("@sid", soaId);

                                insInvCmd.Parameters.AddWithValue("@cn", soa.ClientName);

                                insInvCmd.Parameters.AddWithValue("@id", soa.IssueDate.ToString("yyyy-MM-dd"));

                                insInvCmd.Parameters.AddWithValue("@dd", soa.IssueDate.AddDays(15).ToString("yyyy-MM-dd"));

                                insInvCmd.Parameters.AddWithValue("@ta", gross);

                                insInvCmd.Parameters.AddWithValue("@ded", ded);

                                insInvCmd.Parameters.AddWithValue("@net", net);

                                insInvCmd.ExecuteNonQuery();

                            }

                        }

                        tx.Commit();

                        try

                        {

                            EventNotificationService.Publish("SOA Saved", $"Statement #{soa.SoaNumber} for {soa.ClientName} (PHP {soa.TotalAmount:N2})", ToastType.Success);

                        }

                        catch { }

                        return soaId;

                    }

                    catch

                    {

                        tx.Rollback();

                        throw;

                    }

                }

            }

        }

        public static void DeleteStatementOfAccount(int id)

        {

            // Statements are cancelled, not erased; their line items stay for the audit trail.

            SoftDelete("StatementsOfAccount", id);

        }

        public static async Task<List<StatementOfAccount>> GetStatementsOfAccountAsync()

        {

            return await Task.Run(() => GetStatementsOfAccount());

        }

        public static int SaveStatementOfAccount(StatementOfAccount soa, List<int> invoiceIds)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var tx = conn.BeginTransaction())

                {

                    try

                    {

                        var cmd = conn.CreateCommand();

                        cmd.Transaction = tx;

                        if (soa.Id == 0)

                        {

                            soa.SoaNumber = $"SOA-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";

                            cmd.CommandText = @"INSERT INTO StatementsOfAccount (SoaNumber, ClientName, PeriodCovered, IssueDate, TotalAmount, Status)

                                                VALUES (@sn, @cn, @pc, @issueDate, @ta, @st);

                                                SELECT last_insert_rowid();";

                        }

                        else

                        {

                            cmd.CommandText = @"UPDATE StatementsOfAccount SET ClientName=@cn, PeriodCovered=@pc, IssueDate=@issueDate, TotalAmount=@ta, Status=@st WHERE Id=@rowId;

                                                SELECT @rowId;";

                            cmd.Parameters.AddWithValue("@rowId", soa.Id);

                        }

                        cmd.Parameters.AddWithValue("@sn", soa.SoaNumber);

                        cmd.Parameters.AddWithValue("@cn", soa.ClientName);

                        cmd.Parameters.AddWithValue("@pc", soa.PeriodCovered);

                        cmd.Parameters.AddWithValue("@issueDate", soa.IssueDate.ToString("yyyy-MM-dd"));

                        cmd.Parameters.AddWithValue("@ta", soa.TotalAmount);

                        cmd.Parameters.AddWithValue("@st", soa.Status);

                        int soaId = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                        soa.Id = soaId;

                        if (invoiceIds != null && invoiceIds.Count > 0)

                        {

                            var clearCmd = conn.CreateCommand();

                            clearCmd.Transaction = tx;

                            clearCmd.CommandText = "UPDATE Invoices SET SoaId = 0 WHERE SoaId = @soaId";

                            clearCmd.Parameters.AddWithValue("@soaId", soaId);

                            clearCmd.ExecuteNonQuery();

                            foreach (int invId in invoiceIds)

                            {

                                var linkCmd = conn.CreateCommand();

                                linkCmd.Transaction = tx;

                                linkCmd.CommandText = "UPDATE Invoices SET SoaId = @soaId WHERE Id = @invId";

                                linkCmd.Parameters.AddWithValue("@soaId", soaId);

                                linkCmd.Parameters.AddWithValue("@invId", invId);

                                linkCmd.ExecuteNonQuery();

                            }

                        }

                        tx.Commit();

                        try

                        {

                            EventNotificationService.Publish("SOA Saved", $"Statement #{soa.SoaNumber} for {soa.ClientName} (PHP {soa.TotalAmount:N2})", ToastType.Success);

                        }

                        catch { }

                        return soaId;

                    }

                    catch

                    {

                        tx.Rollback();

                        throw;

                    }

                }

            }

        }

        public static List<Invoice> GetInvoicesForSoa(int soaId)

        {

            var list = new List<Invoice>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT i.Id, i.InvoiceNumber, i.SoaId, i.DeliveryId, i.ClientName, i.IssueDate, i.DueDate, i.TotalAmount, i.Deductions, i.NetAmount, i.Status, COALESCE(d.DispatchNumber, ''), COALESCE(ls.ScheduleNumber, '') FROM Invoices i LEFT JOIN Deliveries d ON i.DeliveryId = d.Id LEFT JOIN LoadingSchedules ls ON d.LoadingScheduleId = ls.Id WHERE i.SoaId = @soaId ORDER BY i.Id ASC";

                cmd.Parameters.AddWithValue("@soaId", soaId);

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime issDate = DateTime.TryParse(reader.GetString(5), out var d1) ? d1 : DateTime.Today;

                        DateTime dueDate = DateTime.TryParse(reader.GetString(6), out var d2) ? d2 : DateTime.Today.AddDays(15);

                        list.Add(new Invoice

                        {

                            Id = reader.GetInt32(0),

                            InvoiceNumber = reader.GetString(1),

                            SoaId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),

                            DeliveryId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),

                            ClientName = reader.GetString(4),

                            IssueDate = issDate,

                            DueDate = dueDate,

                            TotalAmount = Convert.ToDecimal(reader.GetDouble(7)),

                            Deductions = Convert.ToDecimal(reader.GetDouble(8)),

                            NetAmount = Convert.ToDecimal(reader.GetDouble(9)),

                            Status = reader.GetString(10),

                            DispatchNumber = reader.IsDBNull(11) ? "" : reader.GetString(11),

                            LoadingScheduleNumber = reader.IsDBNull(12) ? "" : reader.GetString(12)

                        });

                    }

                }

            }

            return list;

        }

        /// <summary>Copies the live database using SQLite's online backup API (consistent even while the app is writing).</summary>

        private static void SafeCopyDatabase(string targetPath)

        {

            string? dir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string tmpPath = targetPath + ".tmp";

            if (File.Exists(tmpPath)) File.Delete(tmpPath);

            using (var source = new SqliteConnection(connectionString))

            using (var dest = new SqliteConnection($"Data Source={tmpPath};Pooling=False;"))

            {

                source.Open();

                dest.Open();

                source.BackupDatabase(dest);

            }

            File.Move(tmpPath, targetPath, true);

        }

        public static string PerformAutoBackup()

        {

            try

            {

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string backupDir = Path.Combine(baseDir, "Backups");

                if (!Directory.Exists(backupDir))

                {

                    Directory.CreateDirectory(backupDir);

                }

                if (!File.Exists(dbFilePath)) return "";

                try

                {

                    using var chkConn = new SqliteConnection(connectionString);

                    chkConn.Open();

                    using var chkCmd = chkConn.CreateCommand();

                    chkCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";

                    chkCmd.ExecuteNonQuery();

                }

                catch { }

                string backupFileName = $"AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

                string backupPath = Path.Combine(backupDir, backupFileName);

                SafeCopyDatabase(backupPath);

                var autoBackups = new DirectoryInfo(backupDir).GetFiles("AutoBackup_*.bak")

                    .OrderByDescending(f => f.CreationTime)

                    .ToList();

                if (autoBackups.Count > 20)

                {

                    for (int i = 20; i < autoBackups.Count; i++)

                    {

                        try { autoBackups[i].Delete(); } catch { }

                    }

                }

                return backupPath;

            }

            catch

            {

                return "";

            }

        }

        public static decimal GetTotalCollectedForInvoice(int invoiceId)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @id AND COALESCE(IsVoid,0) = 0";

                cmd.Parameters.AddWithValue("@id", invoiceId);

                return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);

            }

        }

        public static List<CollectionReceipt> GetCollectionReceipts()

        {

            var list = new List<CollectionReceipt>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, ReceiptNumber, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes FROM CollectionReceipts WHERE COALESCE(IsVoid,0) = 0 ORDER BY Id DESC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        DateTime pDate = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Today;

                        list.Add(new CollectionReceipt

                        {

                            Id = reader.GetInt32(0),

                            ReceiptNumber = reader.GetString(1),

                            InvoiceId = reader.GetInt32(2),

                            InvoiceNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            PaymentDate = pDate,

                            AmountPaid = Convert.ToDecimal(reader.GetDouble(5)),

                            PaymentMethod = reader.GetString(6),

                            ReferenceNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),

                            Notes = reader.IsDBNull(8) ? "" : reader.GetString(8)

                        });

                    }

                }

            }

            return list;

        }

        public static async Task<List<CollectionReceipt>> GetCollectionReceiptsAsync()

        {

            return await Task.Run(() => GetCollectionReceipts());

        }

        public static void DeleteCollectionReceipt(int receiptId)

        {

            // Payment records are never erased: voiding keeps the row and stops it counting toward the invoice.

            SoftDelete("CollectionReceipts", receiptId);

        }

        public static void SaveCollectionReceipt(CollectionReceipt cr)

        {

            bool wasNew = cr.Id <= 0;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var tx = conn.BeginTransaction())

                {

                    try

                    {

                        if (cr.AmountPaid <= 0) throw new ArgumentException("Collection amount must be greater than PHP 0.00.");

                        if (cr.InvoiceId <= 0) throw new ArgumentException("A valid invoice must be selected for payment collection.");

                        decimal netAmount = 0;

                        string invStatus = "";

                        using (var invCmd = conn.CreateCommand())

                        {

                            invCmd.Transaction = tx;

                            invCmd.CommandText = "SELECT NetAmount, Status FROM Invoices WHERE Id = @iid";

                            invCmd.Parameters.AddWithValue("@iid", cr.InvoiceId);

                            using var r = invCmd.ExecuteReader();

                            if (!r.Read()) throw new ArgumentException("The selected invoice no longer exists.");

                            netAmount = Convert.ToDecimal(r.GetDouble(0));

                            invStatus = r.IsDBNull(1) ? "" : r.GetString(1);

                        }

                        if (invStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                            throw new InvalidOperationException("Payments cannot be recorded against a cancelled invoice.");

                        if (invStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))

                            throw new InvalidOperationException("This invoice is still a draft. Finalize its amount before recording payments.");

                        int previousInvoiceId = 0;

                        if (!wasNew)

                        {

                            using var prevCmd = conn.CreateCommand();

                            prevCmd.Transaction = tx;

                            prevCmd.CommandText = "SELECT InvoiceId FROM CollectionReceipts WHERE Id = @id";

                            prevCmd.Parameters.AddWithValue("@id", cr.Id);

                            var prev = prevCmd.ExecuteScalar();

                            if (prev != null && prev != DBNull.Value) previousInvoiceId = Convert.ToInt32(prev);

                        }

                        decimal otherPaid;

                        using (var sumCmd = conn.CreateCommand())

                        {

                            sumCmd.Transaction = tx;

                            sumCmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @iid AND Id <> @id AND COALESCE(IsVoid,0) = 0";

                            sumCmd.Parameters.AddWithValue("@iid", cr.InvoiceId);

                            sumCmd.Parameters.AddWithValue("@id", cr.Id);

                            otherPaid = Convert.ToDecimal(sumCmd.ExecuteScalar() ?? 0);

                        }

                        if (otherPaid + cr.AmountPaid > netAmount + 0.009m)

                        {

                            throw new InvalidOperationException($"This payment would exceed the invoice balance. Remaining balance: PHP {Math.Max(0, netAmount - otherPaid):N2}.");

                        }

                        var cmd = conn.CreateCommand();

                        cmd.Transaction = tx;

                        if (wasNew)

                        {

                            cr.ReceiptNumber = $"CR-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";

                            cmd.CommandText = @"INSERT INTO CollectionReceipts (ReceiptNumber, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes)

                                                VALUES (@rn, @iid, @in, @pd, @ap, @pm, @rf, @n);

                                                SELECT last_insert_rowid();";

                        }

                        else

                        {

                            cmd.CommandText = @"UPDATE CollectionReceipts SET InvoiceId=@iid, InvoiceNumber=@in, PaymentDate=@pd, AmountPaid=@ap, PaymentMethod=@pm, ReferenceNumber=@rf, Notes=@n WHERE Id=@id;

                                                SELECT @id;";

                            cmd.Parameters.AddWithValue("@id", cr.Id);

                        }

                        cmd.Parameters.AddWithValue("@rn", cr.ReceiptNumber);

                        cmd.Parameters.AddWithValue("@iid", cr.InvoiceId);

                        cmd.Parameters.AddWithValue("@in", cr.InvoiceNumber ?? "");

                        cmd.Parameters.AddWithValue("@pd", cr.PaymentDate.ToString("yyyy-MM-dd"));

                        cmd.Parameters.AddWithValue("@ap", cr.AmountPaid);

                        cmd.Parameters.AddWithValue("@pm", cr.PaymentMethod ?? "Cash");

                        cmd.Parameters.AddWithValue("@rf", cr.ReferenceNumber ?? "");

                        cmd.Parameters.AddWithValue("@n", cr.Notes ?? "");

                        var newIdObj = cmd.ExecuteScalar();

                        if (wasNew && newIdObj != null && newIdObj != DBNull.Value) cr.Id = Convert.ToInt32(newIdObj);

                        RecalculateInvoiceStatus(conn, tx, cr.InvoiceId);

                        if (previousInvoiceId > 0 && previousInvoiceId != cr.InvoiceId)

                        {

                            RecalculateInvoiceStatus(conn, tx, previousInvoiceId);

                        }

                        tx.Commit();

                        try

                        {

                            bool isNew = wasNew;

                            LogAuditTrail(isNew ? "PAYMENT_COLLECTED" : "UPDATE", "CollectionReceipts", cr.Id, cr.ReceiptNumber, "AmountPaid", "", $"PHP {cr.AmountPaid:N2}", $"Collection receipt for Invoice #{cr.InvoiceNumber} via {cr.PaymentMethod} (Ref: {cr.ReferenceNumber})");

                            EventNotificationService.Publish(isNew ? "Payment Collected" : "Payment Updated", $"Receipt #{cr.ReceiptNumber} for invoice #{cr.InvoiceNumber} (PHP {cr.AmountPaid:N2})", ToastType.Success);

                        }

                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] SaveCollectionReceipt log: {ex.Message}"); }

                    }

                    catch

                    {

                        tx.Rollback();

                        throw;

                    }

                }

            }

        }

        public static void PerformStartupSanityCheck()

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    using (var checkCmd = conn.CreateCommand())

                    {

                        checkCmd.CommandText = "PRAGMA integrity_check;";

                        var checkResult = checkCmd.ExecuteScalar()?.ToString();

                        if (checkResult != null && !checkResult.Equals("ok", StringComparison.OrdinalIgnoreCase))

                        {

                            EventNotificationService.Publish("Database Warning", $"Integrity check reported non-standard state: {checkResult}", ToastType.Warning);

                        }

                    }

                    ReconcileResourceStatuses(conn, null);

                    var fixSchedulesCmd = conn.CreateCommand();

                    fixSchedulesCmd.CommandText = @"

                        UPDATE LoadingSchedules

                        SET Status = 'Completed'

                        WHERE Status = 'Assigned'

                          AND Id IN (

                              SELECT LoadingScheduleId

                              FROM Deliveries

                              WHERE Status = 'Completed'

                          );

                    ";

                    fixSchedulesCmd.ExecuteNonQuery();

                }

                RunOperationalLifecycleAutomation();

            }

            catch { }

        }

        public static event Action? OnCategoriesChanged;

        public static void NotifyCategoriesChanged()

        {

            OnCategoriesChanged?.Invoke();

        }

                public static List<(string ItemValue, string ExtraValue)> GetLookupItemsWithExtra(string categoryType)

        {

            var list = new List<(string ItemValue, string ExtraValue)>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT ItemValue, COALESCE(ExtraValue, '') FROM LookupCategories WHERE CategoryType = @t ORDER BY ItemValue ASC";

                cmd.Parameters.AddWithValue("@t", categoryType);

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add((reader.GetString(0), reader.GetString(1)));

                    }

                }

            }

            return list;

        }

        public static int GetTruckTypeCapacity(string truckType)

        {

            if (string.IsNullOrWhiteSpace(truckType)) return 300;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT ExtraValue FROM LookupCategories WHERE (CategoryType = 'Truck Type / Category' OR CategoryType = 'TruckType') AND TRIM(LOWER(ItemValue)) = TRIM(LOWER(@v))";

                cmd.Parameters.AddWithValue("@v", truckType.Trim());

                var res = cmd.ExecuteScalar();

                if (res != null && int.TryParse(res.ToString(), out int cap) && cap > 0)

                {

                    return cap;

                }

            }

            return 300;

        }

        public static void SaveLookupItemWithExtra(string categoryType, string itemValue, string extraValue)

        {

            if (string.IsNullOrWhiteSpace(itemValue)) return;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    INSERT INTO LookupCategories (CategoryType, ItemValue, ExtraValue)

                    VALUES (@t, @v, @e)

                    ON CONFLICT(CategoryType, ItemValue) DO UPDATE SET ExtraValue = @e;

                ";

                cmd.Parameters.AddWithValue("@t", categoryType);

                cmd.Parameters.AddWithValue("@v", itemValue.Trim());

                cmd.Parameters.AddWithValue("@e", extraValue.Trim());

                try { cmd.ExecuteNonQuery(); }

                catch

                {

                    var fallback = conn.CreateCommand();

                    fallback.CommandText = "UPDATE LookupCategories SET ExtraValue = @e WHERE CategoryType = @t AND ItemValue = @v";

                    fallback.Parameters.AddWithValue("@t", categoryType);

                    fallback.Parameters.AddWithValue("@v", itemValue.Trim());

                    fallback.Parameters.AddWithValue("@e", extraValue.Trim());

                    if (fallback.ExecuteNonQuery() == 0)

                    {

                        var ins = conn.CreateCommand();

                        ins.CommandText = "INSERT INTO LookupCategories (CategoryType, ItemValue, ExtraValue) VALUES (@t, @v, @e)";

                        ins.Parameters.AddWithValue("@t", categoryType);

                        ins.Parameters.AddWithValue("@v", itemValue.Trim());

                        ins.Parameters.AddWithValue("@e", extraValue.Trim());

                        ins.ExecuteNonQuery();

                    }

                }

            }

            NotifyCategoriesChanged();

            LogAuditTrail("ADD", "LookupCategories", 0, itemValue.Trim(), "ExtraValue", "", extraValue.Trim(), $"Saved category item '{itemValue.Trim()}' (Extra: {extraValue.Trim()}) under '{categoryType}'");

        }

        public static List<ClientCompany> GetClients()

        {

            var list = new List<ClientCompany>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN, COALESCE(ExtraFieldsJson, ''), COALESCE(IsActive, 1), COALESCE(DeactivatedAt, ''), COALESCE(CreatedAt, '') FROM Clients ORDER BY CompanyName ASC";

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        string cp = reader.IsDBNull(2) ? "" : reader.GetString(2);

                        string ln = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetString(6) : "";

                        string fn = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : "";

                        string mn = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : "";

                        if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(cp))

                        {

                            var s = NameHelper.SplitFullName(cp);

                            ln = s.LastName;

                            fn = s.FirstName;

                            mn = s.MiddleName;

                        }

                        if (string.IsNullOrEmpty(cp))

                        {

                            cp = NameHelper.FormatFullName(ln, fn, mn);

                        }

                        list.Add(new ClientCompany

                        {

                            Id = reader.GetInt32(0),

                            CompanyName = reader.GetString(1),

                            ContactPerson = cp,

                            ContactNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            Address = reader.IsDBNull(4) ? "" : reader.GetString(4),

                            Email = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            ContactPersonLastName = ln,

                            ContactPersonFirstName = fn,

                            ContactPersonMiddleName = mn,

                            TIN = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : "",

                            ExtraFieldsJson = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : "",

                            IsActive = reader.FieldCount <= 11 || reader.IsDBNull(11) || Convert.ToInt64(reader.GetValue(11)) != 0,

                            DeactivatedAt = reader.FieldCount > 12 && !reader.IsDBNull(12) && DateTime.TryParse(reader.GetValue(12)?.ToString(), out var dAt) ? dAt : (DateTime?)null,

                            CreatedAt = reader.FieldCount > 13 && !reader.IsDBNull(13) && DateTime.TryParse(reader.GetValue(13)?.ToString(), out var cAt) ? cAt : (DateTime?)null

                        });

                    }

                }

            }

            if (list.Count == 0)

            {

                var lookupClients = GetLookupItems("Client Company");

                if (lookupClients.Count > 0)

                {

                    foreach (var c in lookupClients)

                    {

                        SaveClient(new ClientCompany { CompanyName = c });

                    }

                    return GetClients();

                }

            }

            return list;

        }

        public static ClientCompany? GetClientByCompanyName(string companyName)

        {

            if (string.IsNullOrWhiteSpace(companyName)) return null;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN, COALESCE(ExtraFieldsJson, ''), COALESCE(IsActive, 1), COALESCE(DeactivatedAt, ''), COALESCE(CreatedAt, '') FROM Clients WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@name)) LIMIT 1";

                cmd.Parameters.AddWithValue("@name", companyName.Trim());

                using (var reader = cmd.ExecuteReader())

                {

                    if (reader.Read())

                    {

                        string cp = reader.IsDBNull(2) ? "" : reader.GetString(2);

                        string ln = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetString(6) : "";

                        string fn = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : "";

                        string mn = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : "";

                        if (string.IsNullOrEmpty(ln) && string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(cp))

                        {

                            var s = NameHelper.SplitFullName(cp);

                            ln = s.LastName;

                            fn = s.FirstName;

                            mn = s.MiddleName;

                        }

                        if (string.IsNullOrEmpty(cp))

                        {

                            cp = NameHelper.FormatFullName(ln, fn, mn);

                        }

                        return new ClientCompany

                        {

                            Id = reader.GetInt32(0),

                            CompanyName = reader.GetString(1),

                            ContactPerson = cp,

                            ContactNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),

                            Address = reader.IsDBNull(4) ? "" : reader.GetString(4),

                            Email = reader.IsDBNull(5) ? "" : reader.GetString(5),

                            ContactPersonLastName = ln,

                            ContactPersonFirstName = fn,

                            ContactPersonMiddleName = mn,

                            TIN = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : "",

                            ExtraFieldsJson = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : "",

                            IsActive = reader.FieldCount <= 11 || reader.IsDBNull(11) || Convert.ToInt64(reader.GetValue(11)) != 0,

                            DeactivatedAt = reader.FieldCount > 12 && !reader.IsDBNull(12) && DateTime.TryParse(reader.GetValue(12)?.ToString(), out var dAt) ? dAt : (DateTime?)null,

                            CreatedAt = reader.FieldCount > 13 && !reader.IsDBNull(13) && DateTime.TryParse(reader.GetValue(13)?.ToString(), out var cAt) ? cAt : (DateTime?)null

                        };

                    }

                }

            }

            return null;

        }

        public static void SaveClient(ClientCompany client)

        {

            if (string.IsNullOrWhiteSpace(client.CompanyName)) return;

            string name = client.CompanyName.Trim();

            if (string.IsNullOrWhiteSpace(client.ContactPerson))

            {

                client.ContactPerson = NameHelper.FormatFullName(client.ContactPersonLastName, client.ContactPersonFirstName, client.ContactPersonMiddleName);

            }

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    INSERT INTO Clients (CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN, ExtraFieldsJson, IsActive, CreatedAt)

                    VALUES (@name, @person, @contact, @addr, @email, @ln, @fn, @mn, @tin, @extra, 1, datetime('now','localtime'))

                    ON CONFLICT(CompanyName) DO UPDATE SET

                        ContactPerson = @person,

                        ContactNumber = @contact,

                        Address = @addr,

                        Email = @email,

                        LastName = @ln,

                        FirstName = @fn,

                        MiddleName = @mn,

                        TIN = @tin,

                        ExtraFieldsJson = CASE WHEN @extra = '' THEN COALESCE(ExtraFieldsJson, '') ELSE @extra END;

                ";

                cmd.Parameters.AddWithValue("@name", name);

                cmd.Parameters.AddWithValue("@person", (client.ContactPerson ?? "").Trim());

                cmd.Parameters.AddWithValue("@contact", (client.ContactNumber ?? "").Trim());

                cmd.Parameters.AddWithValue("@addr", (client.Address ?? "").Trim());

                cmd.Parameters.AddWithValue("@email", (client.Email ?? "").Trim());

                cmd.Parameters.AddWithValue("@ln", (client.ContactPersonLastName ?? "").Trim());

                cmd.Parameters.AddWithValue("@fn", (client.ContactPersonFirstName ?? "").Trim());

                cmd.Parameters.AddWithValue("@mn", (client.ContactPersonMiddleName ?? "").Trim());

                cmd.Parameters.AddWithValue("@tin", (client.TIN ?? "").Trim());

                cmd.Parameters.AddWithValue("@extra", client.ExtraFieldsJson ?? "");

                cmd.ExecuteNonQuery();

            }

            SaveLookupItem("Client Company", name);

            NotifyCategoriesChanged();

            LogAuditTrail("UPDATE", "Clients", 0, name, "ContactPerson", "", client.ContactPerson, $"Client company profile {name} saved");

            try

            {

                EventNotificationService.Publish("Client Saved", $"{client.CompanyName} (Contact: {client.ContactPerson})", ToastType.Success);

            }

            catch { }

        }


        public static List<ClientStore> GetClientStores(string clientName)
        {
            var list = new List<ClientStore>();
            if (string.IsNullOrWhiteSpace(clientName)) return list;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, CreatedAt, COALESCE(ExtraFieldsJson, '') FROM ClientStores WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@cl)) ORDER BY StoreCode ASC";

                cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ClientStore
                        {
                            Id = reader.GetInt32(0),
                            ClientId = reader.GetInt32(1),
                            ClientCompanyName = reader.GetString(2),
                            StoreCode = reader.GetString(3),
                            StoreName = reader.GetString(4),
                            DestinationRoute = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            CreatedAt = reader.IsDBNull(6) ? "" : reader.GetString(6),

                            ExtraFieldsJson = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : ""

                        });
                    }
                }
            }
            if (list.Count == 0)
            {
                // Only routes explicitly assigned to this client: unassigned routes must not invent stores for a new client.

                var routes = GetDeliveryRoutesByClient(clientName, activeOnly: false)

                    .Where(r => string.Equals((r.ClientCompanyName ?? "").Trim(), clientName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var r in routes)
                {
                    if (string.IsNullOrWhiteSpace(r.ConnectedPlaces)) continue;
                    var parts = r.ConnectedPlaces.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int idx = 1;
                    foreach (var p in parts)
                    {
                        string storeName = p.Trim();
                        if (string.IsNullOrWhiteSpace(storeName)) continue;
                        string code = $"{r.RouteCode}-S{idx:D2}";
                        var cs = new ClientStore
                        {
                            ClientId = 0,
                            ClientCompanyName = clientName,
                            StoreCode = code,
                            StoreName = storeName,
                            DestinationRoute = $"{r.RouteCode} - {r.RouteName}"
                        };
                        SaveClientStore(cs);
                        list.Add(cs);
                        idx++;
                    }
                }
            }
            return list;
        }

        public static void SaveClientStore(ClientStore store)
        {
            if (store == null || string.IsNullOrWhiteSpace(store.StoreName)) return;
            if (!string.IsNullOrWhiteSpace(store.StoreCode) && IsStoreCodeTaken(store.ClientCompanyName, store.StoreCode, store.Id))

                throw new InvalidOperationException($"Store code \"{store.StoreCode.Trim()}\" is already used by another store of {store.ClientCompanyName}. Each store needs its own code.");

            var old = store.Id > 0 ? GetClientStoreById(store.Id) : null;

            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                if (store.Id > 0)
                {
                    cmd.CommandText = @"UPDATE ClientStores SET 
                        StoreCode = @sc, 
                        StoreName = @sn, 
                        DestinationRoute = @dr, 
                        ClientCompanyName = @cl,

                        ExtraFieldsJson = @extra

                        WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", store.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO ClientStores 
                        (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson) 

                        VALUES (@cid, @cl, @sc, @sn, @dr, @extra);

                        SELECT last_insert_rowid();";

                    cmd.Parameters.AddWithValue("@cid", store.ClientId);
                }
                cmd.Parameters.AddWithValue("@cl", (store.ClientCompanyName ?? "").Trim());
                cmd.Parameters.AddWithValue("@sc", (store.StoreCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@sn", (store.StoreName ?? "").Trim());
                cmd.Parameters.AddWithValue("@dr", (store.DestinationRoute ?? "").Trim());
                cmd.Parameters.AddWithValue("@extra", store.ExtraFieldsJson ?? "");

                if (store.Id > 0) cmd.ExecuteNonQuery();

                else store.Id = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

            }
            // Keep the delivery route's store list in step with the client's stores.

            try

            {

                string client = (store.ClientCompanyName ?? "").Trim();

                string oldName = old?.StoreName ?? "", oldRoute = old?.DestinationRoute ?? "";

                string newName = (store.StoreName ?? "").Trim(), newRoute = (store.DestinationRoute ?? "").Trim();

                var oldR = ResolveRouteFromLabel(client, oldRoute);

                var newR = ResolveRouteFromLabel(client, newRoute);

                if (oldR != null && newR != null && oldR.Id == newR.Id)

                {

                    if (!SamePlace(oldName, newName)) ReplaceRoutePlace(newR, oldName, newName);

                    else AddRoutePlace(newR, newName);

                }

                else

                {

                    if (oldR != null && oldName.Length > 0) RemoveRoutePlace(oldR, oldName);

                    if (newR != null) AddRoutePlace(newR, newName);

                }

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] store→route sync: {ex.Message}"); }

        }

        public static void DeleteClientStore(int storeId)
        {
            if (storeId <= 0) return;
            var old = GetClientStoreById(storeId);

            if (old != null && IsClientStoreInUse(old, out var usedOn))

                throw new InvalidOperationException($"Store \"{old.StoreCode} - {old.StoreName}\" appears on a past {usedOn}, so it can't be removed without breaking that history. Rename it instead if it changed.");

            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM ClientStores WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", storeId);
                cmd.ExecuteNonQuery();
            }
            try

            {

                if (old != null)

                {

                    var r = ResolveRouteFromLabel(old.ClientCompanyName, old.DestinationRoute);

                    if (r != null) RemoveRoutePlace(r, old.StoreName);

                }

            }

            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] store delete→route sync: {ex.Message}"); }

        }

        // ------------------------------------------------------------------ Client ⇄ route ⇄ store links

        private static ClientStore? GetClientStoreById(int id)

        {

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT Id, ClientId, COALESCE(ClientCompanyName,''), COALESCE(StoreCode,''), COALESCE(StoreName,''), COALESCE(DestinationRoute,'') FROM ClientStores WHERE Id = @id";

            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();

            if (!r.Read()) return null;

            return new ClientStore { Id = r.GetInt32(0), ClientId = r.IsDBNull(1) ? 0 : r.GetInt32(1), ClientCompanyName = r.GetString(2), StoreCode = r.GetString(3), StoreName = r.GetString(4), DestinationRoute = r.GetString(5) };

        }

        private static string NormPlace(string s) => System.Text.RegularExpressions.Regex.Replace((s ?? "").Trim(), @"\s+", " ").ToUpperInvariant();

        private static bool SamePlace(string a, string b) => NormPlace(a) == NormPlace(b);

        /// <summary>The route a client store points at ("R1 - EODLAB (Ilocos Route)" → route R1 of that client).</summary>

        private static DeliveryRoute? ResolveRouteFromLabel(string clientName, string label)

        {

            label = (label ?? "").Trim();

            if (label.Length == 0) return null;

            int dash = label.IndexOf(" - ", StringComparison.Ordinal);

            string code = dash > 0 ? label.Substring(0, dash).Trim() : label;

            var routes = GetDeliveryRoutesByClient(clientName ?? "", activeOnly: false);

            return routes.FirstOrDefault(r => string.Equals(r.RouteCode, code, StringComparison.OrdinalIgnoreCase) && string.Equals((r.ClientCompanyName ?? "").Trim(), (clientName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))

                ?? routes.FirstOrDefault(r => string.Equals(r.RouteCode, code, StringComparison.OrdinalIgnoreCase))

                ?? routes.FirstOrDefault(r => string.Equals($"{r.RouteCode} - {r.RouteName}", label, StringComparison.OrdinalIgnoreCase));

        }

        public static string RouteLabel(DeliveryRoute r) => $"{r.RouteCode} - {r.RouteName}";

        private static void WriteRoutePlaces(int routeId, List<string> places)

        {

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "UPDATE DeliveryRoutes SET ConnectedPlaces = @p, UpdatedAt = datetime('now') WHERE Id = @id";

            cmd.Parameters.AddWithValue("@p", string.Join("; ", places));

            cmd.Parameters.AddWithValue("@id", routeId);

            cmd.ExecuteNonQuery();

        }

        private static void AddRoutePlace(DeliveryRoute r, string place)

        {

            if (string.IsNullOrWhiteSpace(place)) return;

            var list = r.PlacesList;

            if (list.Any(p => SamePlace(p, place))) return;

            list.Add(place.Trim());

            WriteRoutePlaces(r.Id, list);

        }

        private static void RemoveRoutePlace(DeliveryRoute r, string place)

        {

            var list = r.PlacesList;

            int before = list.Count;

            list.RemoveAll(p => SamePlace(p, place));

            if (list.Count != before) WriteRoutePlaces(r.Id, list);

        }

        private static void ReplaceRoutePlace(DeliveryRoute r, string oldPlace, string newPlace)

        {

            var list = r.PlacesList;

            int i = list.FindIndex(p => SamePlace(p, oldPlace));

            if (i >= 0) list[i] = newPlace.Trim();

            else if (!list.Any(p => SamePlace(p, newPlace))) list.Add(newPlace.Trim());

            WriteRoutePlaces(r.Id, list);

        }

        /// <summary>

        /// Makes the client's store list match a route: every store on the route exists as one of the client's stores

        /// (assigned to that route), and stores taken off the route are left unassigned rather than deleted.

        /// </summary>

        public static void SyncClientStoresFromRoute(DeliveryRoute route, List<string>? previousPlaces = null)

        {

            string client = (route.ClientCompanyName ?? "").Trim();

            if (client.Length == 0 || route.Id <= 0) return;

            string label = RouteLabel(route);

            var stores = GetClientStores(client);

            int clientId = GetClientByCompanyName(client)?.Id ?? 0;

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            using var tx = conn.BeginTransaction();

            void Exec(string sql, params (string, object)[] ps)

            {

                using var cmd = conn.CreateCommand();

                cmd.Transaction = tx;

                cmd.CommandText = sql;

                foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v);

                cmd.ExecuteNonQuery();

            }

            bool OnThisRoute(ClientStore st)

            {

                var lbl = (st.DestinationRoute ?? "").Trim();

                if (lbl.Length == 0) return false;

                int dash = lbl.IndexOf(" - ", StringComparison.Ordinal);

                string code = dash > 0 ? lbl.Substring(0, dash).Trim() : lbl;

                return string.Equals(code, route.RouteCode, StringComparison.OrdinalIgnoreCase);

            }

            // Route renamed: refresh the label stored on its stores.

            foreach (var st in stores.Where(x => OnThisRoute(x) && !string.Equals(x.DestinationRoute.Trim(), label, StringComparison.Ordinal)))

                Exec("UPDATE ClientStores SET DestinationRoute = @l WHERE Id = @id", ("@l", label), ("@id", st.Id));

            var places = route.PlacesList;

            int seq = stores.Count(OnThisRoute);

            foreach (var place in places)

            {

                var match = stores.FirstOrDefault(x => SamePlace(x.StoreName, place));

                if (match != null)

                {

                    if (!OnThisRoute(match)) Exec("UPDATE ClientStores SET DestinationRoute = @l WHERE Id = @id", ("@l", label), ("@id", match.Id));

                    continue;

                }

                string code;

                do { seq++; code = $"{route.RouteCode}-S{seq:D2}"; } while (stores.Any(x => string.Equals(x.StoreCode, code, StringComparison.OrdinalIgnoreCase)));

                Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute) VALUES (@cid, @cl, @sc, @sn, @l)",

                    ("@cid", clientId), ("@cl", client), ("@sc", code), ("@sn", place.Trim()), ("@l", label));

                stores.Add(new ClientStore { StoreCode = code, StoreName = place.Trim(), DestinationRoute = label, ClientCompanyName = client });

            }

            if (previousPlaces != null)

            {

                foreach (var gone in previousPlaces.Where(p => !places.Any(n => SamePlace(n, p))))

                {

                    foreach (var st in stores.Where(x => x.Id > 0 && OnThisRoute(x) && SamePlace(x.StoreName, gone)))

                        Exec("UPDATE ClientStores SET DestinationRoute = '' WHERE Id = @id", ("@id", st.Id));

                }

            }

            tx.Commit();

        }

        /// <summary>At start-up: every route's stores exist as the client's stores (fills in anything missing).</summary>

        private static void ReconcileAllRouteStores()

        {

            foreach (var r in GetDeliveryRoutes(activeOnly: false).Where(r => !string.IsNullOrWhiteSpace(r.ClientCompanyName)))

            {

                try { SyncClientStoresFromRoute(r); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] reconcile {r.RouteCode}: {ex.Message}"); }

            }

        }

        /// <summary>

        /// One client list for the whole system: every client name used anywhere gets a Master Data client record,

        /// and spellings that differ only in capitals or spaces are made to match that record.

        /// </summary>

        private static void NormalizeClientRecords()

        {

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            var names = new List<string>();

            void Collect(string sql)

            {

                try

                {

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = sql;

                    using var r = cmd.ExecuteReader();

                    while (r.Read()) if (!r.IsDBNull(0)) names.Add(r.GetString(0).Trim());

                }

                catch { }

            }

            Collect("SELECT CompanyName FROM Clients");

            int registered = names.Count;

            Collect("SELECT ItemValue FROM LookupCategories WHERE CategoryType = 'Client Company'");

            Collect("SELECT DISTINCT ClientName FROM Deliveries WHERE COALESCE(ClientName,'') <> ''");

            Collect("SELECT DISTINCT ClientName FROM LoadingSchedules WHERE COALESCE(ClientName,'') <> ''");

            Collect("SELECT DISTINCT ClientCompanyName FROM DeliveryRoutes WHERE COALESCE(ClientCompanyName,'') <> ''");

            Collect("SELECT DISTINCT ClientCompanyName FROM ClientStores WHERE COALESCE(ClientCompanyName,'') <> ''");

            // The registered spelling wins; otherwise the first spelling seen.

            var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < names.Count; i++)

            {

                string n = System.Text.RegularExpressions.Regex.Replace(names[i], @"\s+", " ");

                if (n.Length == 0 || canonical.ContainsKey(n)) continue;

                canonical[n] = n;

                if (i >= registered)

                {

                    using var ins = conn.CreateCommand();

                    ins.CommandText = "INSERT OR IGNORE INTO Clients (CompanyName) VALUES (@n)";

                    ins.Parameters.AddWithValue("@n", n);

                    ins.ExecuteNonQuery();

                }

            }

            var targets = new (string Table, string Column)[]

            {

                ("ClientStores", "ClientCompanyName"), ("DeliveryRoutes", "ClientCompanyName"), ("LoadingSchedules", "ClientName"),

                ("Deliveries", "ClientName"), ("DispatchStops", "ClientName"), ("StatementsOfAccount", "ClientName"), ("Invoices", "ClientName")

            };

            foreach (var name in canonical.Values)

            {

                foreach (var (t, c) in targets)

                {

                    try

                    {

                        using var up = conn.CreateCommand();

                        up.CommandText = $"UPDATE {t} SET {c} = @n WHERE LOWER(TRIM({c})) = LOWER(@n) AND {c} <> @n";

                        up.Parameters.AddWithValue("@n", name);

                        up.ExecuteNonQuery();

                    }

                    catch { }

                }

                try

                {

                    using var del = conn.CreateCommand();

                    del.CommandText = "DELETE FROM LookupCategories WHERE CategoryType = 'Client Company' AND LOWER(TRIM(ItemValue)) = LOWER(@n) AND ItemValue <> @n";

                    del.Parameters.AddWithValue("@n", name);

                    del.ExecuteNonQuery();

                    using var add = conn.CreateCommand();

                    add.CommandText = "INSERT INTO LookupCategories (CategoryType, ItemValue) SELECT 'Client Company', @n WHERE NOT EXISTS (SELECT 1 FROM LookupCategories WHERE CategoryType = 'Client Company' AND ItemValue = @n)";

                    add.Parameters.AddWithValue("@n", name);

                    add.ExecuteNonQuery();

                }

                catch { }

            }

        }

        /// <summary>Client names for pick-lists: the Master Data clients (plus any name only in the client category list).</summary>

        public static List<string> GetClientNames()

        {

            var names = new List<string>();

            // Only active clients are offered for new orders; deactivated ones stay in billing and history.

            var inactive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try

            {

                foreach (var c in GetClients())

                {

                    string n = c.CompanyName.Trim();

                    if (n.Length == 0) continue;

                    if (c.IsActive) names.Add(n); else inactive.Add(n);

                }

            }

            catch { }

            try { names.AddRange(GetLookupItems("Client Company").Select(n => n.Trim()).Where(n => n.Length > 0 && !inactive.Contains(n))); } catch { }

            return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

        }

        /// <summary>Registers a client name in Master Data if it isn't there yet (used when an order names a new client).</summary>

        public static void EnsureClientExists(string companyName)

        {

            string n = (companyName ?? "").Trim();

            if (n.Length == 0 || GetClientByCompanyName(n) != null) return;

            SaveClient(new ClientCompany { CompanyName = n });

        }

        public static ClientTripSummary GetClientTripSummary(string clientName)
        {
            var summary = new ClientTripSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT 
                            COUNT(CASE WHEN LOWER(Status) = 'completed' THEN 1 END) AS CompletedCount,
                            MAX(CASE WHEN LOWER(Status) = 'completed' THEN DispatchDate END) AS LatestCompleted,
                            COUNT(CASE WHEN LOWER(Status) = 'cancelled' THEN 1 END) AS CancelledCount,
                            MAX(CASE WHEN LOWER(Status) = 'cancelled' THEN DispatchDate END) AS LatestCancelled
                        FROM Deliveries 
                        WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))";
                    cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.CompletedCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.LatestCompletedDate = r.IsDBNull(1) ? "" : r.GetString(1);
                            summary.CancelledCount = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                            summary.LatestCancelledDate = r.IsDBNull(3) ? "" : r.GetString(3);
                        }
                    }

                    var cmdSpecial = conn.CreateCommand();
                    cmdSpecial.CommandText = @"
                        SELECT COUNT(*), MAX(DispatchDate) 
                        FROM SpecialTrips 
                        WHERE LOWER(TRIM(Company)) = LOWER(TRIM(@cl))";
                    cmdSpecial.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmdSpecial.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.SpecialCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.LatestSpecialDate = r.IsDBNull(1) ? "" : r.GetString(1);
                        }
                    }
                }
            }
            catch
            {
            }
            return summary;
        }

        public static ClientPaymentSummary GetClientPaymentSummary(string clientName)
        {
            var summary = new ClientPaymentSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT 
                            COUNT(*),
                            COUNT(CASE WHEN LOWER(Status) NOT IN ('paid', 'draft') THEN 1 END),

                            COALESCE(SUM(NetAmount), 0)

                        FROM Invoices 

                        WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl)) AND LOWER(Status) <> 'cancelled'";

                    cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.TotalInvoices = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.UnpaidInvoices = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                            summary.TotalBilledAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                        }
                    }

                    var cmdCr = conn.CreateCommand();
                    cmdCr.CommandText = @"
                        SELECT cr.ReceiptNumber, cr.PaymentDate, COALESCE(SUM(cr.AmountPaid), 0)
                        FROM CollectionReceipts cr
                        JOIN Invoices inv ON cr.InvoiceId = inv.Id

                        WHERE LOWER(TRIM(inv.ClientName)) = LOWER(TRIM(@cl)) AND COALESCE(cr.IsVoid,0) = 0

                        GROUP BY cr.ReceiptNumber, cr.PaymentDate
                        ORDER BY cr.PaymentDate DESC, cr.Id DESC LIMIT 1";
                    cmdCr.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmdCr.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.LatestReceiptNumber = r.IsDBNull(0) ? "" : r.GetString(0);
                            summary.LatestReceiptDate = r.IsDBNull(1) ? "" : r.GetString(1);
                            summary.TotalPaidAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                        }
                    }
                    summary.PaymentStatus = summary.UnpaidInvoices > 0 ? $"{summary.UnpaidInvoices} unpaid" : "Up to date";

                }
            }
            catch
            {
            }
            return summary;
        }

        /// <summary>

        /// Renames a client everywhere its name is stored (stores, routes, schedules, orders, statements, invoices,

        /// special trips and the client pick-lists), so a rename never leaves the old records behind under the old name.

        /// </summary>

        public static void RenameClient(string oldName, string newName)

        {

            oldName = (oldName ?? "").Trim();

            newName = (newName ?? "").Trim();

            if (oldName.Length == 0 || newName.Length == 0 || oldName == newName) return;

            using var conn = new SqliteConnection(connectionString);

            conn.Open();

            using var tx = conn.BeginTransaction();

            void Exec(string sql)

            {

                using var cmd = conn.CreateCommand();

                cmd.Transaction = tx;

                cmd.CommandText = sql;

                cmd.Parameters.AddWithValue("@old", oldName);

                cmd.Parameters.AddWithValue("@new", newName);

                cmd.ExecuteNonQuery();

            }

            Exec("UPDATE Clients SET CompanyName = @new WHERE LOWER(TRIM(CompanyName)) = LOWER(@old)");

            var targets = new (string Table, string Column)[]

            {

                ("ClientStores", "ClientCompanyName"), ("DeliveryRoutes", "ClientCompanyName"), ("LoadingSchedules", "ClientName"),

                ("Deliveries", "ClientName"), ("DispatchStops", "ClientName"), ("StatementsOfAccount", "ClientName"),

                ("Invoices", "ClientName"), ("SpecialTrips", "Company")

            };

            foreach (var (t, c) in targets)

            {

                try { Exec($"UPDATE {t} SET {c} = @new WHERE LOWER(TRIM({c})) = LOWER(@old)"); }

                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RenameClient {t}.{c}: {ex.Message}"); }

            }

            try { Exec("UPDATE LookupCategories SET ItemValue = @new WHERE CategoryType = 'Client Company' AND LOWER(TRIM(ItemValue)) = LOWER(@old)"); } catch { }

            tx.Commit();

            NotifyCategoriesChanged();

            LogAuditTrail("UPDATE", "Clients", 0, newName, "CompanyName", oldName, newName, $"Client renamed from {oldName} to {newName}");

        }

        /// <summary>How many orders and invoices still reference a client (a client with history should not be deleted).</summary>

        public static (int Orders, int Invoices) CountClientHistory(string companyName)

        {

            int orders = 0, invoices = 0;

            try

            {

                using var conn = new SqliteConnection(connectionString);

                conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = "SELECT COUNT(*) FROM Deliveries WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@n))";

                    cmd.Parameters.AddWithValue("@n", companyName ?? "");

                    orders = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                }

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = "SELECT COUNT(*) FROM Invoices WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@n))";

                    cmd.Parameters.AddWithValue("@n", companyName ?? "");

                    invoices = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                }

            }

            catch { }

            return (orders, invoices);

        }

        public static void DeleteClient(string companyName)

        {

            // Clients are never erased: deactivate them so their orders, invoices and payments keep their owner.

            SetClientActive(companyName, false);

        }

        public static List<string> GetLookupItems(string categoryType)

        {

            var list = new List<string>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT ItemValue FROM LookupCategories WHERE CategoryType = @t ORDER BY ItemValue ASC";

                cmd.Parameters.AddWithValue("@t", categoryType);

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(reader.GetString(0));

                    }

                }

            }

            return list;

        }

        public static void SaveLookupItem(string categoryType, string itemValue)

        {

            if (string.IsNullOrWhiteSpace(itemValue)) return;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "INSERT OR IGNORE INTO LookupCategories (CategoryType, ItemValue) VALUES (@t, @v)";

                cmd.Parameters.AddWithValue("@t", categoryType);

                cmd.Parameters.AddWithValue("@v", itemValue.Trim());

                cmd.ExecuteNonQuery();

            }

            NotifyCategoriesChanged();

            LogAuditTrail("ADD", "LookupCategories", 0, itemValue.Trim(), "ItemValue", "", itemValue.Trim(), $"Added category item '{itemValue.Trim()}' under '{categoryType}'");

        }

        public static bool DeleteLookupItem(string categoryType, string itemValue)

        {

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmd = conn.CreateCommand();

                    cmd.CommandText = "DELETE FROM LookupCategories WHERE CategoryType = @t AND ItemValue = @v";

                    cmd.Parameters.AddWithValue("@t", categoryType);

                    cmd.Parameters.AddWithValue("@v", itemValue.Trim());

                    int affected = cmd.ExecuteNonQuery();

                    if (affected > 0)

                    {

                        NotifyCategoriesChanged();

                        LogAuditTrail("DELETE", "LookupCategories", 0, itemValue.Trim(), "ItemValue", itemValue.Trim(), "", $"Deleted category item '{itemValue.Trim()}' from '{categoryType}'");

                        return true;

                    }

                }

            }

            catch { }

            return false;

        }

        public static List<GlobalSearchResult> GlobalSearch(string query)

        {

            var results = new List<GlobalSearchResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;

            string clean = query.Trim().ToLowerInvariant();

            try

            {

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    var cmdTrucks = conn.CreateCommand();

                    cmdTrucks.CommandText = "SELECT Id, PlateNumber, TruckType, Status, CapacityCrates FROM Trucks WHERE LOWER(PlateNumber) LIKE @q OR LOWER(TruckType) LIKE @q";

                    cmdTrucks.Parameters.AddWithValue("@q", $"%{clean}%");

                    using (var r = cmdTrucks.ExecuteReader())

                    {

                        while (r.Read())

                        {

                            results.Add(new GlobalSearchResult

                            {

                                Category = "Trucks & Vehicles",

                                Title = $"Truck {r.GetString(1)} ({r.GetString(2)})",

                                Subtitle = $"Status: {r.GetString(3)} | Capacity: {r.GetInt32(4)} Crates",

                                Tag = r.GetInt32(0),

                                TargetModule = "Trucks"

                            });

                        }

                    }

                    var cmdCrews = conn.CreateCommand();

                    cmdCrews.CommandText = "SELECT Id, FullName, Role, ContactNumber, IsAvailable FROM Crews WHERE LOWER(FullName) LIKE @q OR LOWER(ContactNumber) LIKE @q OR LOWER(Role) LIKE @q";

                    cmdCrews.Parameters.AddWithValue("@q", $"%{clean}%");

                    using (var r = cmdCrews.ExecuteReader())

                    {

                        while (r.Read())

                        {

                            results.Add(new GlobalSearchResult

                            {

                                Category = "Crew Personnel",

                                Title = $"{r.GetString(1)} ({r.GetString(2)})",

                                Subtitle = $"Contact: {(r.IsDBNull(3) ? "N/A" : r.GetString(3))} | Status: {(r.GetInt32(4) == 1 ? "Active" : "On Trip")}",

                                Tag = r.GetInt32(0),

                                TargetModule = "Crews"

                            });

                        }

                    }

                    var cmdDeliveries = conn.CreateCommand();

                    cmdDeliveries.CommandText = "SELECT Id, DispatchNumber, DriverName, TruckInfo, DispatchDate, Status FROM Deliveries WHERE LOWER(DispatchNumber) LIKE @q OR LOWER(DriverName) LIKE @q OR LOWER(TruckInfo) LIKE @q";

                    cmdDeliveries.Parameters.AddWithValue("@q", $"%{clean}%");

                    using (var r = cmdDeliveries.ExecuteReader())

                    {

                        while (r.Read())

                        {

                            results.Add(new GlobalSearchResult

                            {

                                Category = "Dispatches",

                                Title = $"Dispatch #{r.GetString(1)}",

                                Subtitle = $"Driver: {r.GetString(2)} | Truck: {r.GetString(3)} | Date: {r.GetString(4)} | Status: {r.GetString(5)}",

                                Tag = r.GetInt32(0),

                                TargetModule = "Deliveries"

                            });

                        }

                    }

                    var cmdInvoices = conn.CreateCommand();

                    cmdInvoices.CommandText = "SELECT Id, InvoiceNumber, ClientName, IssueDate, NetAmount, Status FROM Invoices WHERE LOWER(InvoiceNumber) LIKE @q OR LOWER(ClientName) LIKE @q";

                    cmdInvoices.Parameters.AddWithValue("@q", $"%{clean}%");

                    using (var r = cmdInvoices.ExecuteReader())

                    {

                        while (r.Read())

                        {

                            results.Add(new GlobalSearchResult

                            {

                                Category = "Billing Invoices",

                                Title = $"Invoice #{r.GetString(1)} - {r.GetString(2)}",

                                Subtitle = $"Date: {r.GetString(3)} | Net Amount: ₱{Convert.ToDecimal(r.GetDouble(4)):N2} | Status: {r.GetString(5)}",

                                Tag = r.GetInt32(0),

                                TargetModule = "Invoices"

                            });

                        }

                    }

                    var cmdSchedules = conn.CreateCommand();

                    cmdSchedules.CommandText = "SELECT Id, ScheduleNumber, ClientName, Destination, ScheduleDate, TotalCrates, Status FROM LoadingSchedules WHERE LOWER(ScheduleNumber) LIKE @q OR LOWER(ClientName) LIKE @q OR LOWER(Destination) LIKE @q";

                    cmdSchedules.Parameters.AddWithValue("@q", $"%{clean}%");

                    using (var r = cmdSchedules.ExecuteReader())

                    {

                        while (r.Read())

                        {

                            results.Add(new GlobalSearchResult

                            {

                                Category = "Loading Schedules",

                                Title = $"Schedule #{r.GetString(1)} - {r.GetString(2)}",

                                Subtitle = $"Destination: {r.GetString(3)} | Date: {r.GetString(4)} | Crates: {r.GetInt32(5)} | Status: {r.GetString(6)}",

                                Tag = r.GetInt32(0),

                                TargetModule = "LoadingSchedules"

                            });

                        }

                    }

                }

            }

            catch { }

            return results;

        }

        /// <summary>Last reason a backup failed, in plain words.</summary>

        public static string LastBackupError { get; private set; } = "";

        public static bool BackupDatabase(string targetPath)

        {

            LastBackupError = "";

            try

            {

                if (!File.Exists(dbFilePath)) { LastBackupError = "The app's database file was not found."; return false; }

                if (string.Equals(Path.GetFullPath(targetPath), Path.GetFullPath(dbFilePath), StringComparison.OrdinalIgnoreCase))

                { LastBackupError = "Choose a different file name or folder. A backup can't overwrite the database the app is using."; return false; }

                using (var conn = new SqliteConnection(connectionString))

                {

                    conn.Open();

                    // Repair damaged indexes first so the backup is clean; stop if the data itself is damaged.

                    string live = CheckAndRepairIndexes(conn, out _);

                    if (!live.Equals("ok", StringComparison.OrdinalIgnoreCase)) { LastBackupError = $"The current database has damage that can't be repaired automatically ({live})."; return false; }

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";

                    cmd.ExecuteNonQuery();

                }

                SafeCopyDatabase(targetPath);

                // Verify the file that was written.

                using (var verify = new SqliteConnection($"Data Source={targetPath};Mode=ReadOnly;Pooling=False;"))

                {

                    verify.Open();

                    using var c = verify.CreateCommand();

                    c.CommandText = "PRAGMA integrity_check;";

                    string result = c.ExecuteScalar()?.ToString() ?? "";

                    if (!result.Equals("ok", StringComparison.OrdinalIgnoreCase)) { LastBackupError = $"The backup was written but failed its check ({result}). Try saving it to another folder."; return false; }

                }

                SqliteConnection.ClearAllPools();

                return true;

            }

            catch (Exception ex) { LastBackupError = ex.Message; System.Diagnostics.Debug.WriteLine($"[DB] BackupDatabase: {ex.Message}"); }

            return false;

        }

        /// <summary>Last reason a restore was refused or failed, in plain words.</summary>

        public static string LastRestoreError { get; private set; } = "";

        public static bool RestoreDatabase(string sourcePath)

        {

            LastRestoreError = "";

            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

            string preRestore = Path.Combine(backupDir, $"PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            string work = Path.Combine(Path.GetTempPath(), $"mj_restore_{Guid.NewGuid():N}.db");

            bool liveTouched = false;

            try

            {

                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) { LastRestoreError = "The selected file no longer exists."; return false; }

                if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(dbFilePath), StringComparison.OrdinalIgnoreCase))

                { LastRestoreError = "That is the database the app is using right now. Choose a backup file (for example one in the Backups folder) instead."; return false; }

                // Work on a private copy, so a file that is open in another program, or is read-only, can still be used.

                try

                {

                    using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                    using var outFs = new FileStream(work, FileMode.Create, FileAccess.Write);

                    fs.CopyTo(outFs);

                    if (File.Exists(sourcePath + "-wal"))

                    {

                        using var w = new FileStream(sourcePath + "-wal", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                        using var wo = new FileStream(work + "-wal", FileMode.Create, FileAccess.Write);

                        w.CopyTo(wo);

                    }

                }

                catch (Exception ex) { LastRestoreError = $"The backup file couldn't be read: {ex.Message}"; return false; }

                using (var check = new SqliteConnection($"Data Source={work};Pooling=False;"))

                {

                    try { check.Open(); }

                    catch (Exception ex) { LastRestoreError = $"The file isn't a readable SQLite database: {ex.Message}"; return false; }

                    {

                        // Index damage in the backup is rebuilt on the private copy; only real data damage stops the restore.

                        string integrity;

                        try { integrity = CheckAndRepairIndexes(check, out _); }

                        catch (Exception ex) { LastRestoreError = $"The file isn't a valid database: {ex.Message}"; return false; }

                        if (!integrity.Equals("ok", StringComparison.OrdinalIgnoreCase)) { LastRestoreError = $"The backup file is damaged and can't be repaired ({integrity})."; return false; }

                    }

                    using (var cmd = check.CreateCommand())

                    {

                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('Users', 'Deliveries', 'Trucks');";

                        var found = new List<string>();

                        using (var r = cmd.ExecuteReader()) while (r.Read()) found.Add(r.GetString(0));

                        var missing = new[] { "Users", "Deliveries", "Trucks" }.Where(t => !found.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();

                        if (missing.Count > 0) { LastRestoreError = $"This file isn't an M.J. Billones backup (missing the {string.Join(", ", missing)} table{(missing.Count > 1 ? "s" : "")})."; return false; }

                    }

                    using (var cmd = check.CreateCommand())

                    {

                        cmd.CommandText = "SELECT COUNT(*) FROM Users;";

                        if (Convert.ToInt32(cmd.ExecuteScalar() ?? 0) == 0) { LastRestoreError = "This backup has no user accounts, so nobody could sign in after restoring it."; return false; }

                    }

                }

                // Safety copy of the current data first.

                try { if (File.Exists(dbFilePath)) SafeCopyDatabase(preRestore); }

                catch (Exception ex) { LastRestoreError = $"Couldn't save a safety copy of the current data first, so nothing was changed: {ex.Message}"; return false; }

                // Copy page by page into the live database with SQLite's backup API. Unlike overwriting the file,

                // this works while the app (or its timers) still has the database open.

                SqliteConnection.ClearAllPools();

                using (var src = new SqliteConnection($"Data Source={work};Pooling=False;"))

                using (var live = new SqliteConnection($"Data Source={dbFilePath};Default Timeout=30;Pooling=False;"))

                {

                    src.Open();

                    live.Open();

                    liveTouched = true;

                    src.BackupDatabase(live);

                    using var ck = live.CreateCommand();

                    ck.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";

                    try { ck.ExecuteNonQuery(); } catch { }

                }

                SqliteConnection.ClearAllPools();

                InitializeDatabase();   // brings an older backup up to the current schema

                return true;

            }

            catch (Exception ex)

            {

                LastRestoreError = ex is SqliteException se && (se.SqliteErrorCode == 5 || se.SqliteErrorCode == 6)

                    ? "The database is busy (another program, or a second copy of this app, is using it). Close it and try again."

                    : ex.Message;

                if (liveTouched && File.Exists(preRestore))

                {

                    try

                    {

                        SqliteConnection.ClearAllPools();

                        using var back = new SqliteConnection($"Data Source={preRestore};Pooling=False;");

                        using var live = new SqliteConnection($"Data Source={dbFilePath};Default Timeout=30;Pooling=False;");

                        back.Open(); live.Open();

                        back.BackupDatabase(live);

                        LastRestoreError += " Your previous data was put back.";

                    }

                    catch { LastRestoreError += $" A copy of your previous data is in {preRestore}."; }

                }

                return false;

            }

            finally

            {

                foreach (var f in new[] { work, work + "-wal", work + "-shm" }) try { if (File.Exists(f)) File.Delete(f); } catch { }

            }

        }

        public static List<CollectionReceipt> GetCollectionsForInvoice(int invoiceId)

        {

            var list = new List<CollectionReceipt>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"

                    SELECT Id, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes

                    FROM CollectionReceipts

                    WHERE InvoiceId = @invId AND COALESCE(IsVoid,0) = 0

                    ORDER BY Id DESC";

                cmd.Parameters.AddWithValue("@invId", invoiceId);

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(new CollectionReceipt

                        {

                            Id = reader.GetInt32(0),

                            InvoiceId = reader.GetInt32(1),

                            InvoiceNumber = reader.GetString(2),

                            PaymentDate = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.Today,

                            AmountPaid = reader.GetDecimal(4),

                            PaymentMethod = reader.GetString(5),

                            ReferenceNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),

                            Notes = reader.IsDBNull(7) ? "" : reader.GetString(7)

                        });

                    }

                }

            }

            return list;

        }

        public static decimal GetTotalPaidForInvoice(int invoiceId)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @invId AND COALESCE(IsVoid,0) = 0";

                cmd.Parameters.AddWithValue("@invId", invoiceId);

                var result = cmd.ExecuteScalar();

                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;

            }

        }

        public static void UpdateInvoicePaymentStatus(int invoiceId, string status)

        {

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Invoices SET Status = @status WHERE Id = @id";

                cmd.Parameters.AddWithValue("@status", status);

                cmd.Parameters.AddWithValue("@id", invoiceId);

                cmd.ExecuteNonQuery();

            }

        }

        public static bool IsTruckOnActiveTrip(int truckId, int excludeDeliveryId = 0)

        {

            if (truckId <= 0) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT COUNT(1) FROM Deliveries WHERE TruckId = @tid AND Status IN {ActiveDeliveryStatusesSql} AND Id != @excludeId";

                cmd.Parameters.AddWithValue("@tid", truckId);

                cmd.Parameters.AddWithValue("@excludeId", excludeDeliveryId);

                var count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;

            }

        }

        public static bool IsDriverOnActiveTrip(int driverId, int excludeDeliveryId = 0)

        {

            if (driverId <= 0) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT COUNT(1) FROM Deliveries WHERE DriverId = @did AND Status IN {ActiveDeliveryStatusesSql} AND Id != @excludeId";

                cmd.Parameters.AddWithValue("@did", driverId);

                cmd.Parameters.AddWithValue("@excludeId", excludeDeliveryId);

                var count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;

            }

        }

        public static void SaveFormDraft(string moduleKey, string userKey, string draftData)

        {

            if (string.IsNullOrWhiteSpace(moduleKey)) return;

            string uKey = string.IsNullOrWhiteSpace(userKey) ? "default" : userKey.Trim().ToLowerInvariant();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = @"

                        INSERT INTO FormDrafts (ModuleKey, UserKey, DraftData, LastSaved)

                        VALUES (@m, @u, @d, @t)

                        ON CONFLICT(ModuleKey, UserKey) DO UPDATE SET

                            DraftData = excluded.DraftData,

                            LastSaved = excluded.LastSaved;

                    ";

                    cmd.Parameters.AddWithValue("@m", moduleKey.Trim());

                    cmd.Parameters.AddWithValue("@u", uKey);

                    cmd.Parameters.AddWithValue("@d", draftData ?? "");

                    cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();

                }

            }

        }

        public static (string Data, DateTime LastSaved)? GetFormDraft(string moduleKey, string userKey)

        {

            if (string.IsNullOrWhiteSpace(moduleKey)) return null;

            string uKey = string.IsNullOrWhiteSpace(userKey) ? "default" : userKey.Trim().ToLowerInvariant();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = "SELECT DraftData, LastSaved FROM FormDrafts WHERE ModuleKey = @m AND UserKey = @u LIMIT 1";

                    cmd.Parameters.AddWithValue("@m", moduleKey.Trim());

                    cmd.Parameters.AddWithValue("@u", uKey);

                    using (var reader = cmd.ExecuteReader())

                    {

                        if (reader.Read())

                        {

                            string data = reader.GetString(0);

                            string dateStr = reader.GetString(1);

                            DateTime dt = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now;

                            return (data, dt);

                        }

                    }

                }

            }

            return null;

        }

        public static void ClearFormDraft(string moduleKey, string userKey)

        {

            if (string.IsNullOrWhiteSpace(moduleKey)) return;

            string uKey = string.IsNullOrWhiteSpace(userKey) ? "default" : userKey.Trim().ToLowerInvariant();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = "DELETE FROM FormDrafts WHERE ModuleKey = @m AND UserKey = @u";

                    cmd.Parameters.AddWithValue("@m", moduleKey.Trim());

                    cmd.Parameters.AddWithValue("@u", uKey);

                    cmd.ExecuteNonQuery();

                }

            }

        }

        public static string PerformTimedRollingBackup(int maxSnapshots = 5)

        {

            try

            {

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string backupDir = Path.Combine(baseDir, "Backups", "AutoSave");

                if (!Directory.Exists(backupDir))

                {

                    Directory.CreateDirectory(backupDir);

                }

                if (!File.Exists(dbFilePath)) return "";

                try

                {

                    using var chkConn = new SqliteConnection(connectionString);

                    chkConn.Open();

                    using var chkCmd = chkConn.CreateCommand();

                    chkCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";

                    chkCmd.ExecuteNonQuery();

                }

                catch { }

                string backupFileName = $"AutoSave_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

                string backupPath = Path.Combine(backupDir, backupFileName);

                SafeCopyDatabase(backupPath);

                var snapshots = new DirectoryInfo(backupDir).GetFiles("AutoSave_Snapshot_*.bak")

                    .OrderByDescending(f => f.CreationTime)

                    .ToList();

                if (snapshots.Count > maxSnapshots)

                {

                    for (int i = maxSnapshots; i < snapshots.Count; i++)

                    {

                        try { snapshots[i].Delete(); } catch { }

                    }

                }

                return backupPath;

            }

            catch

            {

                return "";

            }

        }

    
        public static List<TruckCategoryItem> GetTruckCategories()

        {

            var list = new List<TruckCategoryItem>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT lc.Id, lc.ItemValue, COALESCE(lc.ExtraValue, '300'),

                                           (SELECT COUNT(*) FROM Trucks t WHERE TRIM(LOWER(t.TruckType)) = TRIM(LOWER(lc.ItemValue)))

                                    FROM LookupCategories lc

                                    WHERE lc.CategoryType = 'Truck Type / Category' OR lc.CategoryType = 'TruckType'

                                    ORDER BY lc.ItemValue ASC";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    int cap = 300;

                    string extra = reader.IsDBNull(2) ? "300" : reader.GetString(2);

                    int.TryParse(extra, out cap);

                    list.Add(new TruckCategoryItem

                    {

                        Id = reader.GetInt32(0),

                        CategoryName = reader.GetString(1),

                        CrateCapacity = cap > 0 ? cap : 300,

                        AssignedTrucksCount = reader.GetInt32(3)

                    });

                }

            }

            return list;

        }

        public static bool SaveTruckCategory(string categoryName, int crateCapacity, string? oldCategoryName = null)

        {

            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                if (!string.IsNullOrWhiteSpace(oldCategoryName) && !oldCategoryName.Trim().Equals(categoryName.Trim(), StringComparison.OrdinalIgnoreCase))

                {

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @newVal, ExtraValue = @cap WHERE (CategoryType = 'Truck Type / Category' OR CategoryType = 'TruckType') AND ItemValue = @oldVal";

                    cmd.Parameters.AddWithValue("@newVal", categoryName.Trim());

                    cmd.Parameters.AddWithValue("@cap", crateCapacity.ToString());

                    cmd.Parameters.AddWithValue("@oldVal", oldCategoryName.Trim());

                    cmd.ExecuteNonQuery();

                    using var updTrucks = conn.CreateCommand();

                    updTrucks.CommandText = "UPDATE Trucks SET TruckType = @newVal WHERE TruckType = @oldVal";

                    updTrucks.Parameters.AddWithValue("@newVal", categoryName.Trim());

                    updTrucks.Parameters.AddWithValue("@oldVal", oldCategoryName.Trim());

                    updTrucks.ExecuteNonQuery();

                }

                else

                {

                    SaveLookupItemWithExtra("Truck Type / Category", categoryName.Trim(), crateCapacity.ToString());

                }

                return true;

            }

        }

        public static bool DeleteTruckCategory(string categoryName)

        {

            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            bool r1 = DeleteLookupItem("Truck Type / Category", categoryName.Trim());

            bool r2 = DeleteLookupItem("TruckType", categoryName.Trim());

            return r1 || r2;

        }

        public static List<CrewRoleItem> GetCrewRoles()

        {

            var list = new List<CrewRoleItem>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT lc.Id, lc.ItemValue,

                                           (SELECT COUNT(*) FROM Crews c WHERE TRIM(LOWER(c.Role)) = TRIM(LOWER(lc.ItemValue)) AND c.IsActive = 1)

                                    FROM LookupCategories lc

                                    WHERE lc.CategoryType = 'Crew Role'

                                    ORDER BY lc.ItemValue ASC";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    list.Add(new CrewRoleItem

                    {

                        Id = reader.GetInt32(0),

                        RoleName = reader.GetString(1),

                        ActiveCrewCount = reader.GetInt32(2)

                    });

                }

            }

            return list;

        }

        public static bool SaveCrewRole(string roleName, string? oldRoleName = null)

        {

            if (string.IsNullOrWhiteSpace(roleName)) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                if (!string.IsNullOrWhiteSpace(oldRoleName) && !oldRoleName.Trim().Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase))

                {

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @newVal WHERE CategoryType = 'Crew Role' AND ItemValue = @oldVal";

                    cmd.Parameters.AddWithValue("@newVal", roleName.Trim());

                    cmd.Parameters.AddWithValue("@oldVal", oldRoleName.Trim());

                    cmd.ExecuteNonQuery();

                    using var updCrew = conn.CreateCommand();

                    updCrew.CommandText = "UPDATE Crews SET Role = @newVal WHERE Role = @oldVal";

                    updCrew.Parameters.AddWithValue("@newVal", roleName.Trim());

                    updCrew.Parameters.AddWithValue("@oldVal", oldRoleName.Trim());

                    updCrew.ExecuteNonQuery();

                }

                else

                {

                    SaveLookupItem("Crew Role", roleName.Trim());

                }

                return true;

            }

        }

        public static bool DeleteCrewRole(string roleName)

        {

            if (string.IsNullOrWhiteSpace(roleName)) return false;

            return DeleteLookupItem("Crew Role", roleName.Trim());

        }

        public static string GenerateNextSpecialTripReference()
        {
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string prefix = $"ST-{dateStr}-";
            int nextSeq = 1;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT OrderReference FROM SpecialTrips WHERE OrderReference LIKE @prefix ORDER BY Id DESC LIMIT 1";
                cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string lastRef = result.ToString() ?? string.Empty;
                    string[] parts = lastRef.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedSeq))
                    {
                        nextSeq = parsedSeq + 1;
                    }
                }
            }
            return $"{prefix}{nextSeq:D4}";
        }

        public static List<SpecialTrip> GetSpecialTrips(string? status = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchQuery = null)
        {
            var list = new List<SpecialTrip>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                var clauses = new List<string>();
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    clauses.Add("Status = @status");
                    cmd.Parameters.AddWithValue("@status", status.Trim());
                }
                if (fromDate.HasValue)
                {
                    clauses.Add("DispatchDate >= @fromDate");
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd 00:00:00"));
                }
                if (toDate.HasValue)
                {
                    clauses.Add("DispatchDate <= @toDate");
                    cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd 23:59:59"));
                }
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    clauses.Add("(OrderReference LIKE @query OR Company LIKE @query OR Address LIKE @query OR DriverName LIKE @query OR TruckPlate LIKE @query)");
                    cmd.Parameters.AddWithValue("@query", $"%{searchQuery.Trim()}%");
                }
                string where = clauses.Count > 0 ? "WHERE " + string.Join(" AND ", clauses) : "";
                cmd.CommandText = $"SELECT * FROM SpecialTrips {where} ORDER BY Id DESC";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var trip = new SpecialTrip
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        OrderReference = reader["OrderReference"]?.ToString() ?? string.Empty,
                        DispatchDate = DateTime.TryParse(reader["DispatchDate"]?.ToString(), out var dt) ? dt : DateTime.Now,
                        Company = reader["Company"]?.ToString() ?? string.Empty,
                        DepartureTime = reader["DepartureTime"]?.ToString() ?? "08:00 AM",
                        Address = reader["Address"]?.ToString() ?? string.Empty,
                        ServiceType = reader["ServiceType"]?.ToString() ?? "Dedicated / Special Delivery",
                        Status = reader["Status"]?.ToString() ?? "Pending",
                        ModeOfPaymentWithReport = Convert.ToInt32(reader["ModeOfPaymentWithReport"]) == 1,
                        WithSOA = Convert.ToInt32(reader["WithSOA"]) == 1,
                        WithServiceInvoice = Convert.ToInt32(reader["WithServiceInvoice"]) == 1,
                        WithCollection = Convert.ToInt32(reader["WithCollection"]) == 1,
                        TruckId = reader["TruckId"] != DBNull.Value ? Convert.ToInt32(reader["TruckId"]) : 0,
                        TruckPlate = reader["TruckPlate"]?.ToString() ?? string.Empty,
                        DriverId = reader["DriverId"] != DBNull.Value ? Convert.ToInt32(reader["DriverId"]) : 0,
                        DriverName = reader["DriverName"]?.ToString() ?? string.Empty,
                        AideId = reader["AideId"] != DBNull.Value ? Convert.ToInt32(reader["AideId"]) : 0,
                        AideName = reader["AideName"]?.ToString() ?? string.Empty,
                        CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var cdt) ? cdt : DateTime.Now,
                        CompletedAt = reader["CompletedAt"] != DBNull.Value && DateTime.TryParse(reader["CompletedAt"]?.ToString(), out var cpdt) ? (DateTime?)cpdt : null,
                        Remarks = reader["Remarks"]?.ToString() ?? string.Empty
                    };
                    list.Add(trip);
                }
            }

            foreach (var trip in list)
            {
                trip.Stops = GetSpecialTripStops(trip.Id);
            }

            return list;
        }

        public static SpecialTrip? GetSpecialTripById(int id)
        {
            if (id <= 0) return null;
            SpecialTrip? trip = null;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SpecialTrips WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    trip = new SpecialTrip
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        OrderReference = reader["OrderReference"]?.ToString() ?? string.Empty,
                        DispatchDate = DateTime.TryParse(reader["DispatchDate"]?.ToString(), out var dt) ? dt : DateTime.Now,
                        Company = reader["Company"]?.ToString() ?? string.Empty,
                        DepartureTime = reader["DepartureTime"]?.ToString() ?? "08:00 AM",
                        Address = reader["Address"]?.ToString() ?? string.Empty,
                        ServiceType = reader["ServiceType"]?.ToString() ?? "Dedicated / Special Delivery",
                        Status = reader["Status"]?.ToString() ?? "Pending",
                        ModeOfPaymentWithReport = Convert.ToInt32(reader["ModeOfPaymentWithReport"]) == 1,
                        WithSOA = Convert.ToInt32(reader["WithSOA"]) == 1,
                        WithServiceInvoice = Convert.ToInt32(reader["WithServiceInvoice"]) == 1,
                        WithCollection = Convert.ToInt32(reader["WithCollection"]) == 1,
                        TruckId = reader["TruckId"] != DBNull.Value ? Convert.ToInt32(reader["TruckId"]) : 0,
                        TruckPlate = reader["TruckPlate"]?.ToString() ?? string.Empty,
                        DriverId = reader["DriverId"] != DBNull.Value ? Convert.ToInt32(reader["DriverId"]) : 0,
                        DriverName = reader["DriverName"]?.ToString() ?? string.Empty,
                        AideId = reader["AideId"] != DBNull.Value ? Convert.ToInt32(reader["AideId"]) : 0,
                        AideName = reader["AideName"]?.ToString() ?? string.Empty,
                        CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var cdt) ? cdt : DateTime.Now,
                        CompletedAt = reader["CompletedAt"] != DBNull.Value && DateTime.TryParse(reader["CompletedAt"]?.ToString(), out var cpdt) ? (DateTime?)cpdt : null,
                        Remarks = reader["Remarks"]?.ToString() ?? string.Empty
                    };
                }
            }
            if (trip != null)
            {
                trip.Stops = GetSpecialTripStops(trip.Id);
            }
            return trip;
        }

        public static List<SpecialTripStop> GetSpecialTripStops(int specialTripId)
        {
            var list = new List<SpecialTripStop>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SpecialTripStops WHERE SpecialTripId = @tripId ORDER BY SequenceNo ASC, Id ASC";
                cmd.Parameters.AddWithValue("@tripId", specialTripId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SpecialTripStop
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        SpecialTripId = Convert.ToInt32(reader["SpecialTripId"]),
                        SequenceNo = Convert.ToInt32(reader["SequenceNo"]),
                        StandardId = reader["StandardId"]?.ToString() ?? string.Empty,
                        DeliveryDetails = reader["DeliveryDetails"]?.ToString() ?? string.Empty,
                        DriverRoute = reader["DriverRoute"]?.ToString() ?? string.Empty,
                        DepartureTime = reader["DepartureTime"]?.ToString() ?? string.Empty,
                        ExtraColumnsJson = reader["ExtraColumnsJson"]?.ToString() ?? string.Empty
                    });
                }
            }
            return list;
        }

        public static bool SaveSpecialTrip(SpecialTrip trip)
        {
            if (trip == null) return false;
            string stStatus = trip.Status.Trim();

            bool stActive = !stStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !stStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);

            if (stActive && (trip.TruckId > 0 || trip.DriverId > 0 || trip.AideId > 0))

            {

                var aides = trip.AideId > 0 ? new List<int> { trip.AideId } : new List<int>();

                string? conflict = ValidateResourceConflict(0, trip.Id, trip.TruckId, trip.DriverId, aides, trip.DispatchDate);

                if (!string.IsNullOrEmpty(conflict))

                {

                    throw new InvalidOperationException(conflict);

                }

            }

            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    if (string.IsNullOrWhiteSpace(trip.OrderReference))
                    {
                        trip.OrderReference = GenerateNextSpecialTripReference();
                    }

                    if (trip.Id <= 0)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = trans;
                        cmd.CommandText = @"INSERT INTO SpecialTrips 
                            (OrderReference, DispatchDate, Company, DepartureTime, Address, ServiceType, Status, 
                             ModeOfPaymentWithReport, WithSOA, WithServiceInvoice, WithCollection, 
                             TruckId, TruckPlate, DriverId, DriverName, AideId, AideName, CreatedAt, CompletedAt, Remarks)
                            VALUES 
                            (@orderRef, @dispDate, @comp, @depTime, @addr, @svcType, @status, 
                             @mopReport, @wSoa, @wInv, @wColl, 
                             @trId, @trPlate, @drId, @drName, @aiId, @aiName, @createdAt, @completedAt, @remarks);
                            SELECT last_insert_rowid();";
                        cmd.Parameters.AddWithValue("@orderRef", trip.OrderReference.Trim());
                        cmd.Parameters.AddWithValue("@dispDate", trip.DispatchDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@comp", trip.Company.Trim());
                        cmd.Parameters.AddWithValue("@depTime", trip.DepartureTime.Trim());
                        cmd.Parameters.AddWithValue("@addr", trip.Address.Trim());
                        cmd.Parameters.AddWithValue("@svcType", trip.ServiceType.Trim());
                        cmd.Parameters.AddWithValue("@status", trip.Status.Trim());
                        cmd.Parameters.AddWithValue("@mopReport", trip.ModeOfPaymentWithReport ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wSoa", trip.WithSOA ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wInv", trip.WithServiceInvoice ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wColl", trip.WithCollection ? 1 : 0);
                        cmd.Parameters.AddWithValue("@trId", trip.TruckId);
                        cmd.Parameters.AddWithValue("@trPlate", trip.TruckPlate.Trim());
                        cmd.Parameters.AddWithValue("@drId", trip.DriverId);
                        cmd.Parameters.AddWithValue("@drName", trip.DriverName.Trim());
                        cmd.Parameters.AddWithValue("@aiId", trip.AideId);
                        cmd.Parameters.AddWithValue("@aiName", trip.AideName.Trim());
                        cmd.Parameters.AddWithValue("@createdAt", trip.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@completedAt", trip.CompletedAt.HasValue ? trip.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@remarks", trip.Remarks.Trim());

                        trip.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    else
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = trans;
                        cmd.CommandText = @"UPDATE SpecialTrips SET 
                            OrderReference = @orderRef, DispatchDate = @dispDate, Company = @comp, DepartureTime = @depTime, 
                            Address = @addr, ServiceType = @svcType, Status = @status, 
                            ModeOfPaymentWithReport = @mopReport, WithSOA = @wSoa, WithServiceInvoice = @wInv, WithCollection = @wColl, 
                            TruckId = @trId, TruckPlate = @trPlate, DriverId = @drId, DriverName = @drName, AideId = @aiId, AideName = @aiName, 
                            CompletedAt = @completedAt, Remarks = @remarks
                            WHERE Id = @id";
                        cmd.Parameters.AddWithValue("@id", trip.Id);
                        cmd.Parameters.AddWithValue("@orderRef", trip.OrderReference.Trim());
                        cmd.Parameters.AddWithValue("@dispDate", trip.DispatchDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@comp", trip.Company.Trim());
                        cmd.Parameters.AddWithValue("@depTime", trip.DepartureTime.Trim());
                        cmd.Parameters.AddWithValue("@addr", trip.Address.Trim());
                        cmd.Parameters.AddWithValue("@svcType", trip.ServiceType.Trim());
                        cmd.Parameters.AddWithValue("@status", trip.Status.Trim());
                        cmd.Parameters.AddWithValue("@mopReport", trip.ModeOfPaymentWithReport ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wSoa", trip.WithSOA ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wInv", trip.WithServiceInvoice ? 1 : 0);
                        cmd.Parameters.AddWithValue("@wColl", trip.WithCollection ? 1 : 0);
                        cmd.Parameters.AddWithValue("@trId", trip.TruckId);
                        cmd.Parameters.AddWithValue("@trPlate", trip.TruckPlate.Trim());
                        cmd.Parameters.AddWithValue("@drId", trip.DriverId);
                        cmd.Parameters.AddWithValue("@drName", trip.DriverName.Trim());
                        cmd.Parameters.AddWithValue("@aiId", trip.AideId);
                        cmd.Parameters.AddWithValue("@aiName", trip.AideName.Trim());
                        cmd.Parameters.AddWithValue("@completedAt", trip.CompletedAt.HasValue ? trip.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@remarks", trip.Remarks.Trim());
                        cmd.ExecuteNonQuery();
                    }

                    using (var delCmd = conn.CreateCommand())
                    {
                        delCmd.Transaction = trans;
                        delCmd.CommandText = "DELETE FROM SpecialTripStops WHERE SpecialTripId = @tripId";
                        delCmd.Parameters.AddWithValue("@tripId", trip.Id);
                        delCmd.ExecuteNonQuery();
                    }

                    if (trip.Stops != null && trip.Stops.Count > 0)
                    {
                        int seq = 1;
                        foreach (var stop in trip.Stops)
                        {
                            using var insStop = conn.CreateCommand();
                            insStop.Transaction = trans;
                            insStop.CommandText = @"INSERT INTO SpecialTripStops 
                                (SpecialTripId, SequenceNo, StandardId, DeliveryDetails, DriverRoute, DepartureTime, ExtraColumnsJson)
                                VALUES (@tId, @seq, @stdId, @delDet, @drRt, @depTm, @extra);";
                            insStop.Parameters.AddWithValue("@tId", trip.Id);
                            insStop.Parameters.AddWithValue("@seq", seq++);
                            insStop.Parameters.AddWithValue("@stdId", stop.StandardId ?? string.Empty);
                            insStop.Parameters.AddWithValue("@delDet", stop.DeliveryDetails ?? string.Empty);
                            insStop.Parameters.AddWithValue("@drRt", stop.DriverRoute ?? string.Empty);
                            insStop.Parameters.AddWithValue("@depTm", stop.DepartureTime ?? string.Empty);
                            insStop.Parameters.AddWithValue("@extra", stop.ExtraColumnsJson ?? string.Empty);
                            insStop.ExecuteNonQuery();
                        }
                    }

                    ReconcileResourceStatuses(conn, trans);

                    trans.Commit();
                    return true;
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public static bool DeleteSpecialTrip(int id)
        {
            if (id <= 0) return false;
            // Special trips are cancelled, not erased; their stops stay for the history.

            return SoftDelete("SpecialTrips", id) ?? false;

        }

        public static bool MarkSpecialTripCompleted(int id)
        {
            var trip = GetSpecialTripById(id);
            if (trip == null) return false;
            trip.Status = "Completed";
            trip.CompletedAt = DateTime.Now;
            bool ok = SaveSpecialTrip(trip);
            if (ok)
            {
                QueueSpecialTripFinanceDrafts(trip);
            }
            return ok;
        }

        public static void QueueSpecialTripFinanceDrafts(SpecialTrip trip)
        {
            if (trip == null) return;
            try
            {
                if (trip.WithSOA)
                {
                    var existingSoa = GetStatementsOfAccount().FirstOrDefault(s => s.PeriodCovered.Contains(trip.OrderReference));
                    if (existingSoa == null)
                    {
                        var soa = new StatementOfAccount
                        {
                            SoaNumber = $"SOA-{trip.OrderReference}",
                            ClientName = trip.Company,
                            ClientAddress = trip.Address,
                            PeriodCovered = $"Special Trip {trip.OrderReference} ({trip.DispatchDate:yyyy-MM-dd})",
                            IssueDate = DateTime.Now,
                            DeliveryType = "SPECIAL",
                            Status = "Draft",
                            TotalAmount = 0
                        };
                        SaveStatementOfAccount(soa, soa.Items ?? new List<StatementOfAccountItem>());
                    }
                }

                if (trip.WithServiceInvoice)
                {
                    string draftInvNum = $"INV-{trip.OrderReference}";

                    var existingInv = GetInvoices().FirstOrDefault(i => i.InvoiceNumber.Equals(draftInvNum, StringComparison.OrdinalIgnoreCase));

                    if (existingInv == null)
                    {
                        // Created as a Draft (no amount yet). Billing must set the amount before it can be collected.

                        var inv = new Invoice
                        {
                            InvoiceNumber = draftInvNum,

                            DispatchNumber = trip.OrderReference,
                            LoadingScheduleNumber = "SPECIAL-TRIP",
                            ClientName = trip.Company,
                            IssueDate = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(15),
                            Status = "Draft"

                        };
                        SaveInvoice(inv);
                    }
                }

                if (trip.WithCollection)
                {
                    // A receipt needs a real amount and a finalized invoice, so it can't be auto-created. Remind the user instead.

                    EventNotificationService.Publish("Collection Pending", $"Special Trip {trip.OrderReference} for {trip.Company} is completed. Record its collection once the invoice amount is final.", ToastType.Info, null, "Finance", "Collections", trip.OrderReference);

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialTrip] QueueSpecialTripFinanceDrafts error: {ex.Message}");
            }
        }
}

}

