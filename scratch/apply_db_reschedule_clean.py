import os

path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target_sig = 'public static (bool Success, string Message) RescheduleDelivery(int deliveryId, DateTime newDate, string newTime, string reason, string operatorUsername)'
replacement_sig = 'public static (bool Success, string Message) RescheduleDelivery(int deliveryId, DateTime newDate, string newTime, string reason, string operatorUsername, int? newTruckId = null, string? newTruckInfo = null, int? newDriverId = null, string? newDriverName = null, int? newAideId = null, string? newAideName = null)'

if target_sig in text:
    text = text.replace(target_sig, replacement_sig, 1)
    print("Replaced signature")
else:
    print("Signature already replaced or not found")

target_marker = 'updateCmd.CommandText = @"UPDATE Deliveries SET'
idx_target = text.find(target_marker)
if idx_target != -1:
    idx_start = text.rfind('using (var updateCmd = conn.CreateCommand())', 0, idx_target)
    idx_end = text.find('}', text.find('updateCmd.ExecuteNonQuery();', idx_target)) + 1
    
    rep_cmd = '''using (var updateCmd = conn.CreateCommand())
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
    text = text[:idx_start] + rep_cmd + text[idx_end:]
    print("Replaced updateCmd block")
else:
    print("target_marker not found, skipping updateCmd block replacement")

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
'''

target_after = 'return (false, $"Reschedule failed: {ex.Message}");'
idx_after = text.find(target_after)
if idx_after != -1 and "RescheduleSpecialTrip" not in text:
    idx_close_bracket = text.find('}', text.find('}', text.find('}', idx_after) + 1) + 1) + 1
    text = text[:idx_close_bracket] + resched_spec_code + text[idx_close_bracket:]
    print("Inserted RescheduleSpecialTrip")
else:
    print("RescheduleSpecialTrip already exists or target_after not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Finished updating Database/MJdatabase.cs")
