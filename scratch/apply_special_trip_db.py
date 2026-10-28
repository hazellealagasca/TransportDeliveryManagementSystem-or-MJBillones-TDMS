import os

path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# Replace GetOpenConnection with new SqliteConnection(connectionString) and conn.Open()
target_code = '''        public static string GenerateNextSpecialTripReference()
        {
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string prefix = $"ST-{dateStr}-";
            int nextSeq = 1;
            using (var conn = GetOpenConnection())
            {
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
            using (var conn = GetOpenConnection())
            {
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
            using (var conn = GetOpenConnection())
            {
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
            using (var conn = GetOpenConnection())
            {
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
            using (var conn = GetOpenConnection())
            {
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
            using (var conn = GetOpenConnection())
            {
                using var trans = conn.BeginTransaction();
                try
                {
                    using (var cmdStops = conn.CreateCommand())
                    {
                        cmdStops.Transaction = trans;
                        cmdStops.CommandText = "DELETE FROM SpecialTripStops WHERE SpecialTripId = @id";
                        cmdStops.Parameters.AddWithValue("@id", id);
                        cmdStops.ExecuteNonQuery();
                    }
                    using (var cmdTrip = conn.CreateCommand())
                    {
                        cmdTrip.Transaction = trans;
                        cmdTrip.CommandText = "DELETE FROM SpecialTrips WHERE Id = @id";
                        cmdTrip.Parameters.AddWithValue("@id", id);
                        cmdTrip.ExecuteNonQuery();
                    }
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
                        SaveStatementOfAccount(soa);
                    }
                }

                if (trip.WithServiceInvoice)
                {
                    var existingInv = GetInvoices().FirstOrDefault(i => i.DispatchNumber == trip.OrderReference);
                    if (existingInv == null)
                    {
                        var inv = new Invoice
                        {
                            InvoiceNumber = $"INV-{trip.OrderReference}",
                            DispatchNumber = trip.OrderReference,
                            LoadingScheduleNumber = "SPECIAL-TRIP",
                            ClientName = trip.Company,
                            IssueDate = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(15),
                            Status = "Unpaid"
                        };
                        SaveInvoice(inv);
                    }
                }

                if (trip.WithCollection)
                {
                    var existingCr = GetCollectionReceipts().FirstOrDefault(c => c.ReferenceNumber == trip.OrderReference);
                    if (existingCr == null)
                    {
                        var cr = new CollectionReceipt
                        {
                            ReceiptNumber = $"CR-{trip.OrderReference}",
                            InvoiceNumber = $"INV-{trip.OrderReference}",
                            PaymentDate = DateTime.Now,
                            PaymentMethod = "Cash",
                            ReferenceNumber = trip.OrderReference,
                            Notes = $"Special Trip {trip.OrderReference} payment collection draft"
                        };
                        SaveCollectionReceipt(cr);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialTrip] QueueSpecialTripFinanceDrafts error: {ex.Message}");
            }
        }'''

fixed_code = '''        public static string GenerateNextSpecialTripReference()
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
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    using (var cmdStops = conn.CreateCommand())
                    {
                        cmdStops.Transaction = trans;
                        cmdStops.CommandText = "DELETE FROM SpecialTripStops WHERE SpecialTripId = @id";
                        cmdStops.Parameters.AddWithValue("@id", id);
                        cmdStops.ExecuteNonQuery();
                    }
                    using (var cmdTrip = conn.CreateCommand())
                    {
                        cmdTrip.Transaction = trans;
                        cmdTrip.CommandText = "DELETE FROM SpecialTrips WHERE Id = @id";
                        cmdTrip.Parameters.AddWithValue("@id", id);
                        cmdTrip.ExecuteNonQuery();
                    }
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
                    var existingInv = GetInvoices().FirstOrDefault(i => i.DispatchNumber == trip.OrderReference);
                    if (existingInv == null)
                    {
                        var inv = new Invoice
                        {
                            InvoiceNumber = $"INV-{trip.OrderReference}",
                            DispatchNumber = trip.OrderReference,
                            LoadingScheduleNumber = "SPECIAL-TRIP",
                            ClientName = trip.Company,
                            IssueDate = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(15),
                            Status = "Unpaid"
                        };
                        SaveInvoice(inv);
                    }
                }

                if (trip.WithCollection)
                {
                    var existingCr = GetCollectionReceipts().FirstOrDefault(c => c.ReferenceNumber == trip.OrderReference);
                    if (existingCr == null)
                    {
                        var cr = new CollectionReceipt
                        {
                            ReceiptNumber = $"CR-{trip.OrderReference}",
                            InvoiceNumber = $"INV-{trip.OrderReference}",
                            PaymentDate = DateTime.Now,
                            PaymentMethod = "Cash",
                            ReferenceNumber = trip.OrderReference,
                            Notes = $"Special Trip {trip.OrderReference} payment collection draft"
                        };
                        SaveCollectionReceipt(cr);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialTrip] QueueSpecialTripFinanceDrafts error: {ex.Message}");
            }
        }'''

if target_code in text:
    text = text.replace(target_code, fixed_code, 1)
    print("Replaced target_code (LF)")
elif target_code.replace('\n', '\r\n') in text:
    text = text.replace(target_code.replace('\n', '\r\n'), fixed_code.replace('\n', '\r\n'), 1)
    print("Replaced target_code (CRLF)")
else:
    raise Exception("target_code not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("SUCCESS: MJdatabase.cs updated with fixed connection string!")
