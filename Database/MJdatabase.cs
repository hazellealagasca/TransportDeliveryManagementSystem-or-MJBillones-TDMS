using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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

    public static class MJdatabase
    {
        private static string dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mj_billones.db");
        private static string connectionString = $"Data Source={dbFilePath};Default Timeout=10;Foreign Keys=True;";

        public static string DbFilePath => dbFilePath;

        public static string HashPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword)) return "";
            if (rawPassword.Length == 64 && rawPassword.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return rawPassword.ToLowerInvariant();
            }
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
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
                                return $"CANNOT DELETE LOADING SCHEDULE:\\nThis schedule is assigned to active Dispatch Order #{r.GetString(0)}.\\nPlease cancel or remove the dispatch order first.";
                            }
                        }
                    }
                    else if (tableName.Equals("Trucks", StringComparison.OrdinalIgnoreCase))
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT DispatchNumber FROM Deliveries WHERE TruckId = @id AND Status IN ('Pending', 'In Transit')";
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
                        cmd.CommandText = "SELECT DispatchNumber, AideIds FROM Deliveries WHERE Status IN ('Pending', 'In Transit')";
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
                        cmdDriver.CommandText = "SELECT DispatchNumber FROM Deliveries WHERE (DriverId = @id OR AideId = @id) AND Status IN ('Pending', 'In Transit')";
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
                        cmd.CommandText = "SELECT InvoiceNumber FROM Invoices WHERE DeliveryId = @id";
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return $"CANNOT DELETE DISPATCH ORDER:\nThis delivery has an official invoice billed (#{r.GetString(0)}). Please void or delete the invoice first.";
                            }
                        }
                    }
                    else if (tableName.Equals("Invoices", StringComparison.OrdinalIgnoreCase))
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT ReceiptNumber FROM CollectionReceipts WHERE InvoiceId = @id";
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
                        cmd.CommandText = "SELECT i.InvoiceNumber FROM Invoices i JOIN TripReports tr ON tr.DeliveryId = i.DeliveryId WHERE tr.Id = @id";
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
                                        lsCmd.CommandText = "UPDATE LoadingSchedules SET Status = 'Pending' WHERE Id = @lsid";
                                        lsCmd.Parameters.AddWithValue("@lsid", lsid);
                                        lsCmd.ExecuteNonQuery();

                                        if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || status.Equals("In Transit", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var trCmd = conn.CreateCommand();
                                            trCmd.Transaction = tx;
                                            trCmd.CommandText = "UPDATE Trucks SET Status = 'Available' WHERE Id = @tid";
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

                            var cmd = conn.CreateCommand();
                            cmd.Transaction = tx;
                            cmd.CommandText = $"DELETE FROM {tableName} WHERE Id = @id";
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();

                            tx.Commit();
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
            catch { return false; }
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
                        CancellationReason TEXT DEFAULT ''
                    );

                    CREATE TABLE IF NOT EXISTS Deliveries (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DispatchNumber TEXT UNIQUE NOT NULL,
                        LoadingScheduleId INTEGER NOT NULL,
                        LoadingScheduleInfo TEXT,
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
                        FOREIGN KEY (LoadingScheduleId) REFERENCES LoadingSchedules(Id),
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
                        Email TEXT DEFAULT ''
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
                    "ALTER TABLE TripReports ADD COLUMN StoreManagerLastName TEXT DEFAULT '';",
                    "ALTER TABLE TripReports ADD COLUMN StoreManagerFirstName TEXT DEFAULT '';",
                    "ALTER TABLE TripReports ADD COLUMN StoreManagerMiddleName TEXT DEFAULT '';"
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
                    var migCmd4 = conn.CreateCommand();
                    migCmd4.CommandText = "ALTER TABLE LookupCategories ADD COLUMN ExtraValue TEXT;";
                    migCmd4.ExecuteNonQuery();
                } catch { }

                SeedDefaultData(conn);
                EnsureDefaultLookupCategories(conn);
                MigrateExistingNames(conn);
            }

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
                    ("Destination Route", "Metro Manila", ""),
                    ("Destination Route", "Batangas / Lipa", ""),
                    ("Destination Route", "Laguna / Calamba", ""),
                    ("Destination Route", "Cavite / Dasmariñas", ""),
                    ("Destination Route", "Pampanga / San Fernando", ""),
                    ("Client Company", "San Miguel Foods", ""),
                    ("Client Company", "Monde Nissin", ""),
                    ("Client Company", "Universal Robina Corp", ""),
                    ("Payment Method", "Bank Transfer", ""),
                    ("Payment Method", "Check", ""),
                    ("Payment Method", "Cash", ""),
                    ("Payment Method", "GCash", ""),
                    ("Crew Role", "Driver", ""),

                    ("Crew Role", "Aide/Helper", ""),

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
                    ('CrewRole', 'Driver'),
                    ('CrewRole', 'Aide/Helper');
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

                        string hashedInput = HashPassword(password);
                        bool isMatch = string.Equals(dbPass, hashedInput, StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(dbPass, password, StringComparison.Ordinal);

                        if (!isMatch)
                        {
                            return (AuthResult.InvalidCredentials, null);
                        }

                        if (!dbActive)
                        {
                            return (AuthResult.PendingApproval, null);
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

                        var user = new User
                        {
                            Id = userId,
                            Username = dbUser,
                            Password = hashedInput,
                            Role = dbRole,
                            FullName = dbName,
                            ContactNumber = dbContact,
                            IsActive = dbActive,
                            CreatedAt = dbCreatedAt
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
                                    VALUES (@u, @p, @r, @f, @c, 0, @now, @sq, @sa, @ln, @fn, @mn)";
                cmd.Parameters.AddWithValue("@u", user.Username.Trim());
                cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));
                cmd.Parameters.AddWithValue("@r", "Office Staff");
                cmd.Parameters.AddWithValue("@f", user.FullName.Trim());
                cmd.Parameters.AddWithValue("@c", (user.ContactNumber ?? "").Trim());
                cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@sq", user.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", (user.SecurityAnswer ?? "").Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@ln", user.LastName ?? "");
                cmd.Parameters.AddWithValue("@fn", user.FirstName ?? "");
                cmd.Parameters.AddWithValue("@mn", user.MiddleName ?? "");
                cmd.ExecuteNonQuery();
            }
            LogSecurityEvent(user.Username, "ACCOUNT_REGISTERED", $"Staff self-registration: {user.FullName} ({user.ContactNumber})");
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

                        string hashedInput = HashPassword(password);
                        bool isMatch = string.Equals(dbPass, hashedInput, StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(dbPass, password, StringComparison.Ordinal);

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

                            return new User
                            {
                                Id = userId,
                                Username = dbUser,
                                Password = hashedInput,
                                Role = dbRole,
                                FullName = dbName,
                                ContactNumber = dbContact,
                                IsActive = dbActive,
                                CreatedAt = dbCreatedAt
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
                cmd.CommandText = "SELECT Id, Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, LastName, FirstName, MiddleName FROM Users ORDER BY Id DESC";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fn = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        string ln = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : "";
                        string first = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : "";
                        string mid = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : "";

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
                            MiddleName = mid
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
                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE (Role LIKE '%Admin%' OR Role LIKE '%Owner%') AND IsActive = 1 AND Id <> @id";
                cmd.Parameters.AddWithValue("@id", userId);
                long activeAdminCount = (long)(cmd.ExecuteScalar() ?? 0);
                return activeAdminCount > 0;
            }
        }

        public static void SaveUser(User user)
        {
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
                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        cmd.CommandText = @"UPDATE Users SET Username=@u, Password=@p, Role=@r, FullName=@f, ContactNumber=@c, IsActive=@a, LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE Id=@id";
                        cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));
                    }
                    else
                    {
                        cmd.CommandText = @"UPDATE Users SET Username=@u, Role=@r, FullName=@f, ContactNumber=@c, IsActive=@a, LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE Id=@id";
                    }
                    cmd.Parameters.AddWithValue("@id", user.Id);
                }
                cmd.Parameters.AddWithValue("@u", user.Username);
                cmd.Parameters.AddWithValue("@r", user.Role);
                cmd.Parameters.AddWithValue("@f", user.FullName);
                cmd.Parameters.AddWithValue("@c", user.ContactNumber);
                cmd.Parameters.AddWithValue("@a", user.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@ln", user.LastName ?? "");
                cmd.Parameters.AddWithValue("@fn", user.FirstName ?? "");
                cmd.Parameters.AddWithValue("@mn", user.MiddleName ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Crew> GetCrews()
        {
            var list = new List<Crew>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, FullName, Role, ContactNumber, LicenseNumber, DailyWage, FoodAllowance, IsAvailable, IsActive, IsSeasonal, PhotoPath, LastName, FirstName, MiddleName FROM Crews ORDER BY Id DESC";
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
                            MiddleName = mid
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
                    cmd.CommandText = @"INSERT INTO Crews (FullName, Role, ContactNumber, LicenseNumber, DailyWage, FoodAllowance, IsAvailable, IsActive, IsSeasonal, PhotoPath, LastName, FirstName, MiddleName)
                                        VALUES (@f, @r, @c, @l, @w, @fa, @av, @ac, @s, @photo, @ln, @fn, @mn)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE Crews SET FullName=@f, Role=@r, ContactNumber=@c, LicenseNumber=@l, DailyWage=@w, FoodAllowance=@fa, IsAvailable=@av, IsActive=@ac, IsSeasonal=@s, PhotoPath=@photo, LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", crew.Id);
                }
                cmd.Parameters.AddWithValue("@f", crew.FullName);
                cmd.Parameters.AddWithValue("@r", crew.Role);
                cmd.Parameters.AddWithValue("@c", crew.ContactNumber);
                cmd.Parameters.AddWithValue("@l", crew.LicenseNumber ?? "");
                cmd.Parameters.AddWithValue("@w", crew.DailyWage);
                cmd.Parameters.AddWithValue("@fa", crew.FoodAllowance);
                cmd.Parameters.AddWithValue("@av", crew.IsAvailable ? 1 : 0);
                cmd.Parameters.AddWithValue("@ac", crew.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@s", crew.IsSeasonal ? 1 : 0);
                cmd.Parameters.AddWithValue("@photo", crew.PhotoPath ?? "");
                cmd.Parameters.AddWithValue("@ln", crew.LastName ?? "");
                cmd.Parameters.AddWithValue("@fn", crew.FirstName ?? "");
                cmd.Parameters.AddWithValue("@mn", crew.MiddleName ?? "");
                cmd.ExecuteNonQuery();
            }
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
                cmd.CommandText = "SELECT * FROM Deliveries WHERE DriverId = @id OR AideId = @id OR AideIds LIKE @idPattern ORDER BY DispatchDate DESC, Id DESC";
                cmd.Parameters.AddWithValue("@id", crewId);
                cmd.Parameters.AddWithValue("@idPattern", $"%{crewId}%");
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
                            AideId = reader.GetInt32(8),
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
                cmd1.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts";
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
            var list = new List<Truck>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Trucks ORDER BY Id DESC";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Truck
                        {
                            Id = reader.GetInt32(0),
                            PlateNumber = reader.GetString(1),
                            TruckType = reader.GetString(2),
                            CapacityCrates = reader.GetInt32(3),
                            Status = reader.GetString(4),
                            Remarks = reader.IsDBNull(5) ? "" : reader.GetString(5)
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

            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                if (truck.Id == 0)
                {
                    cmd.CommandText = @"INSERT INTO Trucks (PlateNumber, TruckType, CapacityCrates, Status, Remarks)
                                        VALUES (@p, @t, @c, @s, @r)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE Trucks SET PlateNumber=@p, TruckType=@t, CapacityCrates=@c, Status=@s, Remarks=@r WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", truck.Id);
                }
                cmd.Parameters.AddWithValue("@p", truck.PlateNumber.Trim());
                cmd.Parameters.AddWithValue("@t", truck.TruckType);
                cmd.Parameters.AddWithValue("@c", truck.CapacityCrates);
                cmd.Parameters.AddWithValue("@s", truck.Status);
                cmd.Parameters.AddWithValue("@r", truck.Remarks ?? "");
                cmd.ExecuteNonQuery();

            }
        }

        public static FleetRecommendation GetFleetRecommendation(int totalCrates, DateTime dispatchDate)
        {
            var rec = new FleetRecommendation { TotalCrates = totalCrates };
            if (totalCrates <= 0)
            {
                rec.RecommendationMessage = "Enter crate quantity to calculate vehicle recommendation.";
                return rec;
            }

            var trucks = GetTrucks();
            var deliveries = GetDeliveries();

            var busyTruckIds = new HashSet<int>();
            foreach (var d in deliveries)
            {
                if (d.DispatchDate.Date == dispatchDate.Date && (d.Status == "Pending" || d.Status == "In Transit"))
                {
                    busyTruckIds.Add(d.TruckId);
                }
            }

            var availableTrucks = trucks.Where(t => t.Status == "Available" && !busyTruckIds.Contains(t.Id))
                                        .OrderBy(t => t.CapacityCrates)
                                        .ToList();

            if (availableTrucks.Count == 0)
            {

                var anyMatchingTruck = trucks.FirstOrDefault(t => t.CapacityCrates >= totalCrates);
                if (anyMatchingTruck != null)
                {
                    rec.RecommendedTruck = anyMatchingTruck;
                    rec.RequiredTruckCount = 1;
                    rec.CapacityUtilization = Math.Round(((double)totalCrates / anyMatchingTruck.CapacityCrates) * 100.0, 1);
                    rec.RecommendationMessage = $"✓ Fleet Match: {anyMatchingTruck.PlateNumber} ({anyMatchingTruck.TruckType} - {anyMatchingTruck.CapacityCrates} crates) handles {totalCrates} crates ({rec.CapacityUtilization}% capacity).";
                    rec.IsMultiTruckNeeded = false;
                    return rec;
                }

                rec.RecommendationMessage = "⚠️ No single truck currently available for this schedule date with sufficient capacity.";
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
                int countNeeded = (int)Math.Ceiling((double)totalCrates / maxTruck.CapacityCrates);
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
            try
            {
                var rates = GetRateMatrix();
                if (rates == null || rates.Count == 0) return null;

                var match = rates.FirstOrDefault(r =>
                    r.IsPeakSeason == isPeak &&
                    !string.IsNullOrWhiteSpace(r.Destination) &&
                    destination.Contains(r.Destination, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(truckType) || r.TruckType.Equals(truckType, StringComparison.OrdinalIgnoreCase))
                );
                if (match != null) return match;

                match = rates.FirstOrDefault(r =>
                    r.IsPeakSeason == isPeak &&
                    !string.IsNullOrWhiteSpace(r.RouteName) &&
                    (destination.Contains(r.RouteName, StringComparison.OrdinalIgnoreCase) || r.RouteName.Contains(destination, StringComparison.OrdinalIgnoreCase))
                );
                if (match != null) return match;

                match = rates.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.Destination) &&
                    destination.Contains(r.Destination, StringComparison.OrdinalIgnoreCase)
                );
                if (match != null) return match;
            }
            catch { }
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
                if (string.IsNullOrWhiteSpace(trkType)) trkType = "Refrigerated Van";

                var matchedRate = FindMatchingRate(dest, isPeak, trkType);
                if (matchedRate != null)
                {
                    matchDescription = $"🏷️ Rate Matrix: {matchedRate.RouteName} ({matchedRate.Destination}) - {(matchedRate.IsPeakSeason ? "Peak" : "Regular")}";
                    return matchedRate.RateAmount;
                }

                int crates = del.TotalCrates > 0 ? del.TotalCrates : (del.GCrate + del.ECrate > 0 ? del.GCrate + del.ECrate : 300);
                matchDescription = $"💡 Standard Tariff: {crates} Crates @ ₱45.00/crate (Unconfigured Route)";
                return crates * 45.00m;
            }
            catch
            {
                matchDescription = "💡 Base Freight Rate";
                return 8500m;
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
                    SaveLookupItem("Destination Route", combined);
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

                    SaveLookupItem("Destination Route", dest);

                    var existing = existingRates.FirstOrDefault(r =>
                        r.Destination.Equals(dest, StringComparison.OrdinalIgnoreCase) &&
                        r.TruckType.Equals(truckType, StringComparison.OrdinalIgnoreCase) &&
                        r.IsPeakSeason == isPeakSeason);

                    if (existing != null)
                    {
                        existing.RateAmount = rateAmount;
                        existing.RouteName = dest;
                        SaveRateMatrix(existing);
                        updated++;
                    }
                    else
                    {
                        SaveRateMatrix(new RateMatrix
                        {
                            RouteName = dest,
                            Destination = dest,
                            TruckType = truckType,
                            IsPeakSeason = isPeakSeason,
                            RateAmount = rateAmount
                        });
                        added++;
                    }
                    saved.Add(dest);
                }
            }

            LogSecurityEvent("SYSTEM", "RATE_MATRIX_BATCH", $"Configured {saved.Count} rates for {truckType} (Peak={isPeakSeason}) @ PHP {rateAmount:N2}");
            return (added, updated, saved);
        }

        public static void SaveRateMatrix(RateMatrix rm)
        {
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
                cmd.Parameters.AddWithValue("@p", rm.IsPeakSeason ? 1 : 0);
                cmd.Parameters.AddWithValue("@a", rm.RateAmount);
                cmd.ExecuteNonQuery();

            }
        }

        public static List<LoadingSchedule> GetLoadingSchedules()
        {
            var list = new List<LoadingSchedule>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, ScheduleNumber, ClientName, Destination, ScheduleDate, DepartureTime, GCrate, ECrate, TotalCrates, IsPeak, Status, Remarks, CancellationReason FROM LoadingSchedules ORDER BY Id DESC";
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
                            CancellationReason = reader.IsDBNull(12) ? "" : reader.GetString(12)
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
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                ls.TotalCrates = ls.GCrate + ls.ECrate;

                if (ls.Id == 0)
                {
                    ls.ScheduleNumber = $"LS-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";
                    cmd.CommandText = @"INSERT INTO LoadingSchedules (ScheduleNumber, ClientName, Destination, ScheduleDate, DepartureTime, GCrate, ECrate, TotalCrates, IsPeak, Status, Remarks, CancellationReason)
                                        VALUES (@sn, @c, @d, @sd, @dt, @gc, @ec, @tc, @ip, @s, @r, @cr)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE LoadingSchedules SET ClientName=@c, Destination=@d, ScheduleDate=@sd, DepartureTime=@dt, GCrate=@gc, ECrate=@ec, TotalCrates=@tc, IsPeak=@ip, Status=@s, Remarks=@r, CancellationReason=@cr WHERE Id=@id";
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
                cmd.ExecuteNonQuery();

            }
        }

        public static List<Delivery> GetDeliveries()
        {
            var list = new List<Delivery>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, DispatchNumber, LoadingScheduleId, LoadingScheduleInfo, TruckId, TruckInfo, DriverId, DriverName, AideId, AideName, DispatchDate, DepartureTime, Status, Remarks, GCrate, ECrate, TotalCrates, AideIds, AideNames, CancellationReason FROM Deliveries ORDER BY Id DESC";
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
                            AideId = aideId,
                            AideName = aideName,
                            AideIds = aideIds,
                            AideNames = aideNames,
                            GCrate = gCrate,
                            ECrate = eCrate,
                            TotalCrates = totalCrates,
                            DispatchDate = dDate,
                            DepartureTime = reader.IsDBNull(11) ? "08:00 AM" : reader.GetString(11),
                            Status = reader.IsDBNull(12) ? "Pending" : reader.GetString(12),
                            Remarks = reader.IsDBNull(13) ? "" : reader.GetString(13),
                            CancellationReason = reader.IsDBNull(19) ? "" : reader.GetString(19)
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

        public static void SaveDelivery(Delivery d)
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
                            d.DispatchNumber = $"DSP-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";
                            cmd.CommandText = @"INSERT INTO Deliveries (DispatchNumber, LoadingScheduleId, LoadingScheduleInfo, TruckId, TruckInfo, DriverId, DriverName, AideId, AideName, AideIds, AideNames, GCrate, ECrate, TotalCrates, DispatchDate, DepartureTime, Status, Remarks, CancellationReason)
                                                VALUES (@dn, @lsid, @lsi, @tid, @ti, @drid, @drn, @aid, @ain, @aids, @ains, @gc, @ec, @tc, @dd, @dt, @s, @r, @cr)";

                            var updateLs = conn.CreateCommand();
                            updateLs.Transaction = tx;
                            updateLs.CommandText = "UPDATE LoadingSchedules SET Status='Assigned' WHERE Id=@lsid";
                            updateLs.Parameters.AddWithValue("@lsid", d.LoadingScheduleId);
                            updateLs.ExecuteNonQuery();

                            var updateTr = conn.CreateCommand();
                            updateTr.Transaction = tx;
                            updateTr.CommandText = "UPDATE Trucks SET Status='In Use' WHERE Id=@tid";
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
                                relOldTrk.CommandText = "UPDATE Trucks SET Status='Available' WHERE Id=@otid";
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

                            cmd.CommandText = @"UPDATE Deliveries SET LoadingScheduleId=@lsid, LoadingScheduleInfo=@lsi, TruckId=@tid, TruckInfo=@ti, DriverId=@drid, DriverName=@drn, AideId=@aid, AideName=@ain, AideIds=@aids, AideNames=@ains, GCrate=@gc, ECrate=@ec, TotalCrates=@tc, DispatchDate=@dd, DepartureTime=@dt, Status=@s, Remarks=@r, CancellationReason=@cr WHERE Id=@id";
                            cmd.Parameters.AddWithValue("@id", d.Id);

                            if (d.Status == "Completed" || d.Status == "Cancelled")
                            {
                                var releaseTr = conn.CreateCommand();
                                releaseTr.Transaction = tx;
                                releaseTr.CommandText = "UPDATE Trucks SET Status='Available' WHERE Id=@tid";
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

                                var updateLs = conn.CreateCommand();
                                updateLs.Transaction = tx;
                                updateLs.CommandText = d.Status == "Completed"
                                    ? "UPDATE LoadingSchedules SET Status='Completed' WHERE Id=@lsid"
                                    : "UPDATE LoadingSchedules SET Status='Pending' WHERE Id=@lsid";
                                updateLs.Parameters.AddWithValue("@lsid", d.LoadingScheduleId);
                                updateLs.ExecuteNonQuery();
                            }
                            else
                            {
                                var lockTr = conn.CreateCommand();
                                lockTr.Transaction = tx;
                                lockTr.CommandText = "UPDATE Trucks SET Status='In Use' WHERE Id=@tid";
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
                        cmd.ExecuteNonQuery();

                        tx.Commit();
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
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();

                var checkTr = conn.CreateCommand();
                checkTr.CommandText = "SELECT PlateNumber, Status FROM Trucks WHERE Id = @tid";
                checkTr.Parameters.AddWithValue("@tid", truckId);
                using (var r = checkTr.ExecuteReader())
                {
                    if (r.Read())
                    {
                        string plate = r.GetString(0);
                        string status = r.GetString(1);
                        if (deliveryId == 0 && !status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"TRUCK UNAVAILABLE:\nVehicle '{plate}' is currently marked as '{status}' and cannot be dispatched.";
                        }
                    }
                }

                var checkDr = conn.CreateCommand();
                checkDr.CommandText = "SELECT FullName, IsAvailable FROM Crews WHERE Id = @drid";
                checkDr.Parameters.AddWithValue("@drid", driverId);
                using (var r = checkDr.ExecuteReader())
                {
                    if (r.Read())
                    {
                        string name = r.GetString(0);
                        int isAvail = r.GetInt32(1);
                        if (deliveryId == 0 && isAvail == 0)
                        {
                            return $"DRIVER ON TRIP:\nDriver '{name}' is currently marked as 'On Trip' and cannot be assigned to a new dispatch.";
                        }
                    }
                }

                if (aideIds != null && aideIds.Count > 0)
                {
                    foreach (int aid in aideIds)
                    {
                        var checkAid = conn.CreateCommand();
                        checkAid.CommandText = "SELECT FullName, IsAvailable FROM Crews WHERE Id = @aid";
                        checkAid.Parameters.AddWithValue("@aid", aid);
                        using (var r = checkAid.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                string name = r.GetString(0);
                                int isAvail = r.GetInt32(1);
                                if (deliveryId == 0 && isAvail == 0)
                                {
                                    return $"AIDE/HELPER ON TRIP:\nHelper '{name}' is currently marked as 'On Trip' and cannot be assigned to a new dispatch.";
                                }
                            }
                        }
                    }
                }

                var cmdTruck = conn.CreateCommand();
                cmdTruck.CommandText = @"SELECT DispatchNumber, Status FROM Deliveries
                                         WHERE TruckId = @tid AND DATE(DispatchDate) = DATE(@dd) AND Id <> @id AND Status IN ('Pending', 'In Transit')";
                cmdTruck.Parameters.AddWithValue("@tid", truckId);
                cmdTruck.Parameters.AddWithValue("@dd", dispatchDate.ToString("yyyy-MM-dd"));
                cmdTruck.Parameters.AddWithValue("@id", deliveryId);

                using (var reader = cmdTruck.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string dsp = reader.GetString(0);
                        string status = reader.GetString(1);
                        return $"TRUCK DISPATCH CONFLICT:\nThe selected vehicle is already scheduled/in transit on active dispatch #{dsp} ({status}) for {dispatchDate:MMM dd, yyyy}.";
                    }
                }

                var cmdDriver = conn.CreateCommand();
                cmdDriver.CommandText = @"SELECT DispatchNumber, Status FROM Deliveries
                                          WHERE DriverId = @drid AND DATE(DispatchDate) = DATE(@dd) AND Id <> @id AND Status IN ('Pending', 'In Transit')";
                cmdDriver.Parameters.AddWithValue("@drid", driverId);
                cmdDriver.Parameters.AddWithValue("@dd", dispatchDate.ToString("yyyy-MM-dd"));
                cmdDriver.Parameters.AddWithValue("@id", deliveryId);

                using (var reader = cmdDriver.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string dsp = reader.GetString(0);
                        string status = reader.GetString(1);
                        return $"DRIVER ASSIGNMENT CONFLICT:\nThe selected driver is already assigned to active dispatch #{dsp} ({status}) for {dispatchDate:MMM dd, yyyy}.";
                    }
                }

                if (aideIds != null && aideIds.Count > 0)
                {
                    foreach (int aid in aideIds)
                    {
                        var cmdAide = conn.CreateCommand();
                        cmdAide.CommandText = @"SELECT DispatchNumber, Status FROM Deliveries
                                                WHERE (AideId = @aid OR AideIds LIKE @aidPattern) AND DATE(DispatchDate) = DATE(@dd) AND Id <> @id AND Status IN ('Pending', 'In Transit')";
                        cmdAide.Parameters.AddWithValue("@aid", aid);
                        cmdAide.Parameters.AddWithValue("@aidPattern", $"%{aid}%");
                        cmdAide.Parameters.AddWithValue("@dd", dispatchDate.ToString("yyyy-MM-dd"));
                        cmdAide.Parameters.AddWithValue("@id", deliveryId);

                        using (var reader = cmdAide.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string dsp = reader.GetString(0);
                                string status = reader.GetString(1);
                                return $"AIDE/HELPER CONFLICT:\nThe selected helper is already assigned to active dispatch #{dsp} ({status}) for {dispatchDate:MMM dd, yyyy}.";
                            }
                        }
                    }
                }
            }
            return null;
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
                                          AND Status IN ('Pending', 'Assigned', 'In Transit')";
                    cmd.Parameters.AddWithValue("@dt", departureTime.Trim());
                }
                else
                {
                    cmd.CommandText = @"SELECT ScheduleNumber, Status, DepartureTime FROM LoadingSchedules
                                        WHERE TRIM(LOWER(ClientName)) = TRIM(LOWER(@c))
                                          AND TRIM(LOWER(Destination)) = TRIM(LOWER(@d))
                                          AND DATE(ScheduleDate) = DATE(@sd)
                                          AND Id <> @id
                                          AND Status IN ('Pending', 'Assigned', 'In Transit')";
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
                cmd.CommandText = "SELECT Id, TripReportNumber, DeliveryId, DispatchNumber, ArrivalTime, DepartureTime, DeliveredCrates, ReturnedCrates, StoreManagerName, Remarks, ReportDate, DeliveredGCrates, DeliveredECrates, ReturnedGCrates, ReturnedECrates, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName FROM TripReports ORDER BY Id DESC";
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
                            StoreManagerMiddleName = smmn
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

        public static void SaveTripReport(TripReport tr)
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
                        tr.DeliveredCrates = tr.DeliveredGCrates + tr.DeliveredECrates;
                        tr.ReturnedCrates = tr.ReturnedGCrates + tr.ReturnedECrates;

                        if (string.IsNullOrWhiteSpace(tr.StoreManagerName))
                        {
                            tr.StoreManagerName = NameHelper.FormatFullName(tr.StoreManagerLastName, tr.StoreManagerFirstName, tr.StoreManagerMiddleName);
                        }

                        if (tr.Id == 0)
                        {
                            tr.TripReportNumber = $"TRIP-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";
                            cmd.CommandText = @"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, ArrivalTime, DepartureTime, DeliveredGCrates, DeliveredECrates, DeliveredCrates, ReturnedGCrates, ReturnedECrates, ReturnedCrates, StoreManagerName, Remarks, ReportDate, StoreManagerLastName, StoreManagerFirstName, StoreManagerMiddleName)
                                                VALUES (@trn, @did, @dn, @at, @dt, @dgc, @dec, @dc, @rgc, @rec, @rc, @sm, @r, @rd, @smln, @smfn, @smmn)";

                            var updateDel = conn.CreateCommand();
                            updateDel.Transaction = tx;
                            updateDel.CommandText = "UPDATE Deliveries SET Status='Completed' WHERE Id=@did";
                            updateDel.Parameters.AddWithValue("@did", tr.DeliveryId);
                            updateDel.ExecuteNonQuery();

                            var releaseTr = conn.CreateCommand();
                            releaseTr.Transaction = tx;
                            releaseTr.CommandText = "UPDATE Trucks SET Status='Available' WHERE Id=(SELECT TruckId FROM Deliveries WHERE Id=@did)";
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
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE TripReports SET ArrivalTime=@at, DepartureTime=@dt, DeliveredGCrates=@dgc, DeliveredECrates=@dec, DeliveredCrates=@dc, ReturnedGCrates=@rgc, ReturnedECrates=@rec, ReturnedCrates=@rc, StoreManagerName=@sm, Remarks=@r, StoreManagerLastName=@smln, StoreManagerFirstName=@smfn, StoreManagerMiddleName=@smmn WHERE Id=@id";
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
                        cmd.ExecuteNonQuery();

                        tx.Commit();
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
                cmd.CommandText = "SELECT Id, DeliveryId, DispatchNumber, IncidentDate, DamagedQuantity, EstimatedCost, Cause, ChargeableToCrew, Description, Status, PhotoPath FROM IncidentReports ORDER BY Id DESC";
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
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                if (ir.Id == 0)
                {
                    var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT COUNT(*) FROM IncidentReports WHERE DeliveryId = @did AND Cause = @cause AND DATE(IncidentDate) = DATE(@idate)";
                    checkCmd.Parameters.AddWithValue("@did", ir.DeliveryId);
                    checkCmd.Parameters.AddWithValue("@cause", ir.Cause.Trim());
                    checkCmd.Parameters.AddWithValue("@idate", ir.IncidentDate.ToString("yyyy-MM-dd"));
                    long existing = (long)(checkCmd.ExecuteScalar() ?? 0);
                    if (existing > 0)
                    {
                        throw new InvalidOperationException($"An incident report for dispatch with cause '{ir.Cause}' on {ir.IncidentDate:yyyy-MM-dd} is already logged.");
                    }

                    cmd.CommandText = @"INSERT INTO IncidentReports (DeliveryId, DispatchNumber, IncidentDate, DamagedQuantity, EstimatedCost, Cause, ChargeableToCrew, Description, Status, PhotoPath)
                                        VALUES (@did, @dn, @id, @dq, @ec, @c, @ch, @desc, @s, @photo)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE IncidentReports SET DamagedQuantity=@dq, EstimatedCost=@ec, Cause=@c, ChargeableToCrew=@ch, Description=@desc, Status=@s, PhotoPath=@photo WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", ir.Id);
                }
                cmd.Parameters.AddWithValue("@did", ir.DeliveryId);
                cmd.Parameters.AddWithValue("@dn", ir.DispatchNumber);
                cmd.Parameters.AddWithValue("@id", ir.IncidentDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@dq", ir.DamagedQuantity);
                cmd.Parameters.AddWithValue("@ec", ir.EstimatedCost);
                cmd.Parameters.AddWithValue("@c", ir.Cause);
                cmd.Parameters.AddWithValue("@ch", ir.ChargeableToCrew ? 1 : 0);
                cmd.Parameters.AddWithValue("@desc", ir.Description);
                cmd.Parameters.AddWithValue("@s", ir.Status);
                cmd.Parameters.AddWithValue("@photo", ir.PhotoPath ?? "");
                cmd.ExecuteNonQuery();

            }
        }

        public static List<Invoice> GetInvoices()
        {
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

        public static void SaveInvoice(Invoice inv)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                if (inv.Id == 0)
                {
                    inv.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";
                    cmd.CommandText = @"INSERT INTO Invoices (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status)
                                        VALUES (@in, @soa, @did, @c, @id, @dd, @ta, @d, @na, @s)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE Invoices SET SoaId=@soa, DeliveryId=@did, ClientName=@c, IssueDate=@id, DueDate=@dd, TotalAmount=@ta, Deductions=@d, NetAmount=@na, Status=@s WHERE Id=@invId";
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
                cmd.ExecuteNonQuery();
            }
        }

        public static List<StatementOfAccount> GetStatementsOfAccount()
        {
            var list = new List<StatementOfAccount>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, SoaNumber, ClientName, PeriodCovered, IssueDate, TotalAmount, Status FROM StatementsOfAccount ORDER BY Id DESC";
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
                            Status = reader.GetString(6)
                        });
                    }
                }
            }
            return list;
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
                                                VALUES (@sn, @cn, @pc, @id, @ta, @st);
                                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE StatementsOfAccount SET ClientName=@cn, PeriodCovered=@pc, IssueDate=@id, TotalAmount=@ta, Status=@st WHERE Id=@id;
                                                SELECT @id;";
                            cmd.Parameters.AddWithValue("@id", soa.Id);
                        }
                        cmd.Parameters.AddWithValue("@sn", soa.SoaNumber);
                        cmd.Parameters.AddWithValue("@cn", soa.ClientName);
                        cmd.Parameters.AddWithValue("@pc", soa.PeriodCovered);
                        cmd.Parameters.AddWithValue("@id", soa.IssueDate.ToString("yyyy-MM-dd"));
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

                File.Copy(dbFilePath, backupPath, true);

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
                cmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @id";
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
                cmd.CommandText = "SELECT Id, ReceiptNumber, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes FROM CollectionReceipts ORDER BY Id DESC";
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
            int invoiceId = 0;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var getCmd = conn.CreateCommand();
                getCmd.CommandText = "SELECT InvoiceId FROM CollectionReceipts WHERE Id = @id";
                getCmd.Parameters.AddWithValue("@id", receiptId);
                var res = getCmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    invoiceId = Convert.ToInt32(res);
                }

                var delCmd = conn.CreateCommand();
                delCmd.CommandText = "DELETE FROM CollectionReceipts WHERE Id = @id";
                delCmd.Parameters.AddWithValue("@id", receiptId);
                delCmd.ExecuteNonQuery();

                if (invoiceId > 0)
                {
                    var sumCmd = conn.CreateCommand();
                    sumCmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @invId";
                    sumCmd.Parameters.AddWithValue("@invId", invoiceId);
                    decimal totalCollected = Convert.ToDecimal(sumCmd.ExecuteScalar() ?? 0);

                    var invCmd = conn.CreateCommand();
                    invCmd.CommandText = "SELECT NetAmount FROM Invoices WHERE Id = @invId";
                    invCmd.Parameters.AddWithValue("@invId", invoiceId);
                    var netRes = invCmd.ExecuteScalar();
                    if (netRes != null && netRes != DBNull.Value)
                    {
                        decimal netAmount = Convert.ToDecimal(netRes);
                        string newStatus = "Unpaid";
                        if (totalCollected >= netAmount && netAmount > 0)
                        {
                            newStatus = "Paid";
                        }
                        else if (totalCollected > 0)
                        {
                            newStatus = "Partial";
                        }

                        var updCmd = conn.CreateCommand();
                        updCmd.CommandText = "UPDATE Invoices SET Status = @s WHERE Id = @invId";
                        updCmd.Parameters.AddWithValue("@s", newStatus);
                        updCmd.Parameters.AddWithValue("@invId", invoiceId);
                        updCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void SaveCollectionReceipt(CollectionReceipt cr)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (cr.AmountPaid <= 0) throw new ArgumentException("Collection amount must be greater than PHP 0.00.");
                        if (cr.InvoiceId <= 0) throw new ArgumentException("A valid invoice must be selected for payment collection.");

                        var cmd = conn.CreateCommand();
                        cmd.Transaction = tx;
                        if (cr.Id == 0)
                        {
                            cr.ReceiptNumber = $"CR-{DateTime.Now:yyyyMMdd-HHmmss}-{DateTime.Now.Millisecond:D3}{Random.Shared.Next(10, 99)}";
                            cmd.CommandText = @"INSERT INTO CollectionReceipts (ReceiptNumber, InvoiceId, InvoiceNumber, PaymentDate, AmountPaid, PaymentMethod, ReferenceNumber, Notes)
                                                VALUES (@rn, @iid, @in, @pd, @ap, @pm, @rf, @n)";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE CollectionReceipts SET InvoiceId=@iid, InvoiceNumber=@in, PaymentDate=@pd, AmountPaid=@ap, PaymentMethod=@pm, ReferenceNumber=@rf, Notes=@n WHERE Id=@id";
                            cmd.Parameters.AddWithValue("@id", cr.Id);
                        }
                        cmd.Parameters.AddWithValue("@rn", cr.ReceiptNumber);
                        cmd.Parameters.AddWithValue("@iid", cr.InvoiceId);
                        cmd.Parameters.AddWithValue("@in", cr.InvoiceNumber);
                        cmd.Parameters.AddWithValue("@pd", cr.PaymentDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@ap", cr.AmountPaid);
                        cmd.Parameters.AddWithValue("@pm", cr.PaymentMethod);
                        cmd.Parameters.AddWithValue("@rf", cr.ReferenceNumber);
                        cmd.Parameters.AddWithValue("@n", cr.Notes);
                        cmd.ExecuteNonQuery();

                        var checkSum = conn.CreateCommand();
                        checkSum.Transaction = tx;
                        checkSum.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId=@iid";
                        checkSum.Parameters.AddWithValue("@iid", cr.InvoiceId);
                        decimal totalPaid = Convert.ToDecimal(checkSum.ExecuteScalar() ?? 0);

                        var checkInv = conn.CreateCommand();
                        checkInv.Transaction = tx;
                        checkInv.CommandText = "SELECT NetAmount FROM Invoices WHERE Id=@iid";
                        checkInv.Parameters.AddWithValue("@iid", cr.InvoiceId);
                        decimal netAmount = Convert.ToDecimal(checkInv.ExecuteScalar() ?? 0);

                        var updateInv = conn.CreateCommand();
                        updateInv.Transaction = tx;
                        if (totalPaid >= netAmount && netAmount > 0)
                        {
                            updateInv.CommandText = "UPDATE Invoices SET Status='Paid' WHERE Id=@iid";
                        }
                        else if (totalPaid > 0)
                        {
                            updateInv.CommandText = "UPDATE Invoices SET Status='Partial' WHERE Id=@iid";
                        }
                        else
                        {
                            updateInv.CommandText = "UPDATE Invoices SET Status='Unpaid' WHERE Id=@iid";
                        }
                        updateInv.Parameters.AddWithValue("@iid", cr.InvoiceId);
                        updateInv.ExecuteNonQuery();

                        tx.Commit();
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

                    var fixTrucksCmd = conn.CreateCommand();
                    fixTrucksCmd.CommandText = @"
                        UPDATE Trucks
                        SET Status = 'Available'
                        WHERE Status = 'In Use'
                          AND Id NOT IN (
                              SELECT DISTINCT TruckId
                              FROM Deliveries
                              WHERE Status IN ('Pending', 'In Transit')
                          );
                    ";
                    fixTrucksCmd.ExecuteNonQuery();

                    var activeCrewIds = new HashSet<int>();
                    using (var actCmd = conn.CreateCommand())
                    {
                        actCmd.CommandText = "SELECT DriverId, AideId, AideIds FROM Deliveries WHERE Status IN ('Pending', 'In Transit')";
                        using var rAct = actCmd.ExecuteReader();
                        while (rAct.Read())
                        {
                            activeCrewIds.Add(rAct.GetInt32(0));
                            int aid = rAct.IsDBNull(1) ? 0 : rAct.GetInt32(1);
                            if (aid > 0) activeCrewIds.Add(aid);
                            string aids = rAct.IsDBNull(2) ? "" : rAct.GetString(2);
                            foreach (var s in aids.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (int.TryParse(s.Trim(), out int pid) && pid > 0) activeCrewIds.Add(pid);
                            }
                        }
                    }

                    var fixDriversCmd = conn.CreateCommand();
                    if (activeCrewIds.Count > 0)
                    {
                        var pNames = activeCrewIds.Select((id, idx) => $"@cid{idx}").ToList();
                        fixDriversCmd.CommandText = $"UPDATE Crews SET IsAvailable = 1 WHERE IsAvailable = 0 AND Id NOT IN ({string.Join(",", pNames)})";
                        int pIdx = 0;
                        foreach (int cid in activeCrewIds)
                        {
                            fixDriversCmd.Parameters.AddWithValue($"@cid{pIdx++}", cid);
                        }
                    }
                    else
                    {
                        fixDriversCmd.CommandText = "UPDATE Crews SET IsAvailable = 1 WHERE IsAvailable = 0";
                    }
                    fixDriversCmd.ExecuteNonQuery();

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
        }

        public static List<ClientCompany> GetClients()
        {
            var list = new List<ClientCompany>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName FROM Clients ORDER BY CompanyName ASC";
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
                            ContactPersonMiddleName = mn
                        });
                    }
                }
            }

            if (list.Count == 0)
            {
                var lookupClients = GetLookupItems("Client Company");
                foreach (var c in lookupClients)
                {
                    SaveClient(new ClientCompany { CompanyName = c });
                }
                return GetClients();
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
                cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName FROM Clients WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@name)) LIMIT 1";
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
                            ContactPersonMiddleName = mn
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
                    INSERT INTO Clients (CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName)
                    VALUES (@name, @person, @contact, @addr, @email, @ln, @fn, @mn)
                    ON CONFLICT(CompanyName) DO UPDATE SET
                        ContactPerson = @person,
                        ContactNumber = @contact,
                        Address = @addr,
                        Email = @email,
                        LastName = @ln,
                        FirstName = @fn,
                        MiddleName = @mn;
                ";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@person", (client.ContactPerson ?? "").Trim());
                cmd.Parameters.AddWithValue("@contact", (client.ContactNumber ?? "").Trim());
                cmd.Parameters.AddWithValue("@addr", (client.Address ?? "").Trim());
                cmd.Parameters.AddWithValue("@email", (client.Email ?? "").Trim());
                cmd.Parameters.AddWithValue("@ln", (client.ContactPersonLastName ?? "").Trim());
                cmd.Parameters.AddWithValue("@fn", (client.ContactPersonFirstName ?? "").Trim());
                cmd.Parameters.AddWithValue("@mn", (client.ContactPersonMiddleName ?? "").Trim());
                cmd.ExecuteNonQuery();
            }

            SaveLookupItem("Client Company", name);
            NotifyCategoriesChanged();
        }

        public static void DeleteClient(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Clients WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@name))";
                cmd.Parameters.AddWithValue("@name", companyName.Trim());
                cmd.ExecuteNonQuery();
            }
            DeleteLookupItem("Client Company", companyName);
            NotifyCategoriesChanged();
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
                                Category = "Fleet & Trucks",
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
                                Subtitle = $"Contact: {(r.IsDBNull(3) ? "N/A" : r.GetString(3))} | Status: {(r.GetInt32(4) == 1 ? "Available" : "On Trip")}",
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

        public static bool BackupDatabase(string targetPath)
        {
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    cmd.ExecuteNonQuery();
                }

                if (File.Exists(dbFilePath))
                {
                    File.Copy(dbFilePath, targetPath, true);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool RestoreDatabase(string sourcePath)
        {
            string backupSnapshot = dbFilePath + ".pre_restore.bak";
            try
            {
                if (!File.Exists(sourcePath)) return false;

                string testConnStr = $"Data Source={sourcePath};Default Timeout=5;";
                using (var testConn = new SqliteConnection(testConnStr))
                {
                    testConn.Open();
                    using var testCmd = testConn.CreateCommand();
                    testCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                    testCmd.ExecuteScalar();
                }

                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(dbFilePath))
                {
                    File.Copy(dbFilePath, backupSnapshot, true);
                }

                string walPath = dbFilePath + "-wal";
                string shmPath = dbFilePath + "-shm";

                if (File.Exists(walPath)) try { File.Delete(walPath); } catch { }
                if (File.Exists(shmPath)) try { File.Delete(shmPath); } catch { }

                File.Copy(sourcePath, dbFilePath, true);

                InitializeDatabase();
                if (File.Exists(backupSnapshot)) try { File.Delete(backupSnapshot); } catch { }
                return true;
            }
            catch
            {
                try
                {
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (File.Exists(backupSnapshot))
                    {
                        File.Copy(backupSnapshot, dbFilePath, true);
                        InitializeDatabase();
                    }
                }
                catch { }
                return false;
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
                    WHERE InvoiceId = @invId
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
                cmd.CommandText = "SELECT COALESCE(SUM(AmountPaid), 0) FROM CollectionReceipts WHERE InvoiceId = @invId";
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
                cmd.CommandText = "SELECT COUNT(1) FROM Deliveries WHERE TruckId = @tid AND Status IN ('Pending', 'Dispatched', 'In Transit') AND Id != @excludeId";
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
                cmd.CommandText = "SELECT COUNT(1) FROM Deliveries WHERE DriverId = @did AND Status IN ('Pending', 'Dispatched', 'In Transit') AND Id != @excludeId";
                cmd.Parameters.AddWithValue("@did", driverId);
                cmd.Parameters.AddWithValue("@excludeId", excludeDeliveryId);
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

    }
}
