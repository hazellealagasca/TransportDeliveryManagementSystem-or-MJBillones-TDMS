import os

path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Update signature and body of RescheduleDelivery
target_sig = 'public static (bool Success, string Message) RescheduleDelivery(int deliveryId, DateTime newDate, string newTime, string reason, string operatorUsername)'
replacement_sig = 'public static (bool Success, string Message) RescheduleDelivery(int deliveryId, DateTime newDate, string newTime, string reason, string operatorUsername, int? newTruckId = null, string? newTruckInfo = null, int? newDriverId = null, string? newDriverName = null, int? newAideId = null, string? newAideName = null)'

if target_sig in text:
    text = text.replace(target_sig, replacement_sig, 1)
    print("Replaced RescheduleDelivery signature")
else:
    raise Exception("target_sig not found")

target_body = '''                        using (var updateCmd = conn.CreateCommand())

                        {

                            updateCmd.Transaction = trans;

                            updateCmd.CommandText = @"UPDATE Deliveries SET

                                                      DispatchDate = @dd,

                                                      DepartureTime = @dt,

                                                      Status = 'Pending',

                                                      Remarks = @rem

                                                      WHERE Id = @id";

                            updateCmd.Parameters.AddWithValue("@id", deliveryId);

                            updateCmd.Parameters.AddWithValue("@dd", newDate.ToString("yyyy-MM-dd"));

                            updateCmd.Parameters.AddWithValue("@dt", newTime);

                            updateCmd.Parameters.AddWithValue("@rem", updatedRemarks);

                            updateCmd.ExecuteNonQuery();

                        }'''

replacement_body = '''                        using (var updateCmd = conn.CreateCommand())
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
                                updateCmd.Parameters.AddWithValue("@aiId", newAideId.Value);
                                updateCmd.Parameters.AddWithValue("@aiName", newAideName ?? "");
                            }

                            updateCmd.CommandText = $"UPDATE Deliveries SET {string.Join(", ", setClauses)} WHERE Id = @id";
                            updateCmd.ExecuteNonQuery();
                        }'''

if target_body in text:
    text = text.replace(target_body, replacement_body, 1)
    print("Replaced updateCmd body (LF)")
elif target_body.replace('\n', '\r\n') in text:
    text = text.replace(target_body.replace('\n', '\r\n'), replacement_body.replace('\n', '\r\n'), 1)
    print("Replaced updateCmd body (CRLF)")
else:
    raise Exception("target_body not found")

# 2. Add RescheduleSpecialTrip right after RescheduleDelivery
resched_spec_code = '''
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

                        using (var logCmd = conn.CreateCommand())
                        {
                            logCmd.Transaction = trans;
                            logCmd.CommandText = @"INSERT INTO AuditTrail (Timestamp, Username, Action, Module, RecordId, OldValue, NewValue, ReasonOrNotes, IpOrMachine)
                                                  VALUES (datetime('now'), @usr, 'Reschedule', 'SpecialTrips', @recId, @oldVal, @newVal, @reason, @ip)";
                            logCmd.Parameters.AddWithValue("@usr", operatorUsername ?? "System");
                            logCmd.Parameters.AddWithValue("@recId", trip.OrderReference);
                            logCmd.Parameters.AddWithValue("@oldVal", $"{trip.DispatchDate:yyyy-MM-dd} {trip.DepartureTime}");
                            logCmd.Parameters.AddWithValue("@newVal", $"{newDate:yyyy-MM-dd} {newTime}");
                            logCmd.Parameters.AddWithValue("@reason", reason ?? "");
                            logCmd.Parameters.AddWithValue("@ip", Environment.MachineName);
                            logCmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return (true, $"Special Trip '{trip.OrderReference}' has been successfully rescheduled to {newDate:MM/dd/yyyy} at {newTime}.");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return (false, $"Failed to reschedule special trip: {ex.Message}");
                    }
                }
            }
        }
'''

# Find end of RescheduleDelivery
marker = 'return (true, $"Dispatch #{dspNum} has been successfully rescheduled to {newDate:MM/dd/yyyy} at {newTime}.");\n                    }\n                    catch'
marker_crlf = 'return (true, $"Dispatch #{dspNum} has been successfully rescheduled to {newDate:MM/dd/yyyy} at {newTime}.");\r\n                    }\r\n                    catch'

end_resched = 'return (false, $"Failed to reschedule dispatch: {ex.Message}");\n                }\n            }\n        }'
end_resched_crlf = 'return (false, $"Failed to reschedule dispatch: {ex.Message}");\r\n                }\r\n            }\r\n        }'

if end_resched in text:
    text = text.replace(end_resched, end_resched + resched_spec_code, 1)
    print("Added RescheduleSpecialTrip (LF)")
elif end_resched_crlf in text:
    text = text.replace(end_resched_crlf, end_resched_crlf + resched_spec_code.replace('\n', '\r\n'), 1)
    print("Added RescheduleSpecialTrip (CRLF)")
else:
    raise Exception("end_resched not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("SUCCESS: MJdatabase.cs updated with RescheduleDelivery and RescheduleSpecialTrip!")
