using System;
using System.Collections.Generic;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models
{
    public interface IRefreshable
    {
        void RefreshData();
    }

    public static class NameHelper
    {
        public static string FormatFullName(string? lastName, string? firstName, string? middleName)
        {
            string l = (lastName ?? "").Trim();
            string f = (firstName ?? "").Trim();
            string m = (middleName ?? "").Trim();

            if (string.IsNullOrEmpty(l) && string.IsNullOrEmpty(f)) return m;
            if (string.IsNullOrEmpty(l)) return string.IsNullOrEmpty(m) ? f : $"{f} {m}";
            if (string.IsNullOrEmpty(f)) return string.IsNullOrEmpty(m) ? l : $"{l} {m}";
            return string.IsNullOrEmpty(m) ? $"{l}, {f}" : $"{l}, {f} {m}";
        }

        public static (string LastName, string FirstName, string MiddleName) SplitFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, string.Empty, string.Empty);

            string raw = fullName.Trim();

            if (raw.Contains(','))
            {
                var parts = raw.Split(new[] { ',' }, 2);
                string lastName = parts[0].Trim();
                string rest = parts[1].Trim();

                if (string.IsNullOrEmpty(rest))
                    return (lastName, string.Empty, string.Empty);

                var restTokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (restTokens.Length == 1)
                {
                    return (lastName, restTokens[0], string.Empty);
                }
                else
                {
                    string middle = restTokens[restTokens.Length - 1];
                    string first = string.Join(" ", restTokens, 0, restTokens.Length - 1);
                    return (lastName, first, middle);
                }
            }

            var tokens = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1)
            {
                return (string.Empty, tokens[0], string.Empty);
            }
            if (tokens.Length == 2)
            {
                return (tokens[1], tokens[0], string.Empty);
            }

            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "de", "del", "dela", "delos", "san", "santa", "sto", "sta"
            };

            if (tokens.Length >= 3 && prefixes.Contains(tokens[tokens.Length - 2]))
            {
                string lastName = $"{tokens[tokens.Length - 2]} {tokens[tokens.Length - 1]}";
                if (tokens.Length == 3)
                {
                    return (lastName, tokens[0], string.Empty);
                }
                else
                {
                    string middle = tokens[tokens.Length - 3];
                    string first = string.Join(" ", tokens, 0, tokens.Length - 3);
                    return (lastName, first, middle);
                }
            }

            if (tokens.Length == 3)
            {
                return (tokens[2], tokens[0], tokens[1]);
            }
            else
            {
                string lastName = tokens[tokens.Length - 1];
                string middle = tokens[tokens.Length - 2];
                string first = string.Join(" ", tokens, 0, tokens.Length - 2);
                return (lastName, first, middle);
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Office Staff";
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string SecurityQuestion { get; set; } = string.Empty;
        public string SecurityAnswer { get; set; } = string.Empty;
    }

    public class LoginAudit
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Username { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string IpOrMachine { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class AuditTrailEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Username { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int RecordId { get; set; }
        public string RecordIdentifier { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ReasonOrNotes { get; set; } = string.Empty;
        public string IpOrMachine { get; set; } = string.Empty;
    }

    public class SystemActivityLogItem
    {
        public DateTime TimestampDt { get; set; } = DateTime.MinValue;
        public string Timestamp { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string User_Or_Sender { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
    }

    public class Crew
    {
        public int Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Driver";
        public string ContactNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public decimal DailyWage { get; set; }
        public decimal FoodAllowance { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsSeasonal { get; set; } = false;
        public string PhotoPath { get; set; } = string.Empty;
        public string AvailabilityStatus => !IsActive ? "Inactive" : (IsAvailable ? "Active" : "On Trip");
    }

    public class Truck
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string TruckType { get; set; } = "Refrigerated Van";
        public int CapacityCrates { get; set; } = 300;
        public string Status { get; set; } = "Active";
        public string Remarks { get; set; } = string.Empty;
        public string CargoType { get; set; } = "Cold / Chilled";
    }

    public class RateMatrix
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string TruckType { get; set; } = "Refrigerated Van";
        public bool IsPeakSeason { get; set; } = false;
        public decimal RateAmount { get; set; }
    }

    public class ClientCompany
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPersonLastName { get; set; } = string.Empty;
        public string ContactPersonFirstName { get; set; } = string.Empty;
        public string ContactPersonMiddleName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class LoadingSchedule
    {
        public int Id { get; set; }
        public string ScheduleNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; } = DateTime.Now;
        public string DepartureTime { get; set; } = "08:00 AM";
        public int GCrate { get; set; }
        public int ECrate { get; set; }
        public int TotalCrates { get; set; }
        public bool IsPeak { get; set; } = false;
        public string Status { get; set; } = "Pending";
        public string Remarks { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public string CargoType { get; set; } = "Cold / Chilled";
        public int RescheduleCount { get; set; } = 0;
        public DateTime? OriginalScheduleDate { get; set; }
        public string RescheduleReason { get; set; } = string.Empty;
    }

    public class Delivery
    {
        public int Id { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int LoadingScheduleId { get; set; } = 0;
        public string LoadingScheduleInfo { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public bool IsPeak { get; set; } = false;
        public int TruckId { get; set; }
        public string TruckInfo { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public int AideId { get; set; }
        public string AideName { get; set; } = string.Empty;
        public string AideIds { get; set; } = string.Empty;
        public string AideNames { get; set; } = string.Empty;
        public int GCrate { get; set; }
        public int ECrate { get; set; }
        public int TotalCrates { get; set; }
        public DateTime DispatchDate { get; set; } = DateTime.Now;
        public string DepartureTime { get; set; } = "08:00 AM";
        public string Status { get; set; } = "Pending";
        public string Remarks { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public string CargoType { get; set; } = "Cold / Chilled";
        public bool IsMultiDrop { get; set; } = false;
        public int TotalStopsCount { get; set; } = 1;
        public List<DispatchStop> Stops { get; set; } = new List<DispatchStop>();
        public string RouteTypeDisplay => IsMultiDrop ? $"🗺️ Multi-Drop ({TotalStopsCount})" : "📍 Direct";
    }

    public class DispatchStop
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public int StopSequence { get; set; } = 1;
        public int? LoadingScheduleId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int GCrate { get; set; }
        public int ECrate { get; set; }
        public int TotalCrates { get; set; }
        public string Status { get; set; } = "Pending";
        public string ArrivalTime { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public int DeliveredGCrates { get; set; }
        public int DeliveredECrates { get; set; }
        public int DeliveredCrates { get; set; }
        public int ReturnedGCrates { get; set; }
        public int ReturnedECrates { get; set; }
        public int ReturnedCrates { get; set; }
        public string StoreManagerLastName { get; set; } = string.Empty;
        public string StoreManagerFirstName { get; set; } = string.Empty;
        public string StoreManagerMiddleName { get; set; } = string.Empty;
        public string StoreManagerName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class TripReport
    {
        public int Id { get; set; }
        public string TripReportNumber { get; set; } = string.Empty;
        public int DeliveryId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public int DeliveredGCrates { get; set; }
        public int DeliveredECrates { get; set; }
        public int DeliveredCrates { get; set; }
        public int ReturnedGCrates { get; set; }
        public int ReturnedECrates { get; set; }
        public int ReturnedCrates { get; set; }
        public string StoreManagerLastName { get; set; } = string.Empty;
        public string StoreManagerFirstName { get; set; } = string.Empty;
        public string StoreManagerMiddleName { get; set; } = string.Empty;
        public string StoreManagerName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; } = DateTime.Now;
    }

    public class IncidentReport
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; } = DateTime.Now;
        public int DamagedQuantity { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Cause { get; set; } = string.Empty;
        public bool ChargeableToCrew { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string PhotoPath { get; set; } = string.Empty;
    }

    public class StatementOfAccount
    {
        public int Id { get; set; }
        public string SoaNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string PeriodCovered { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Issued";
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SoaId { get; set; }
        public int DeliveryId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string LoadingScheduleNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(15);
        public decimal TotalAmount { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = "Unpaid";
    }

    public class CollectionReceipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class TruckRecommendation
    {
        public int TotalCrates { get; set; }
        public Truck? RecommendedTruck { get; set; }
        public int RequiredTruckCount { get; set; } = 1;
        public string RecommendationMessage { get; set; } = string.Empty;
        public double CapacityUtilization { get; set; }
        public bool IsMultiTruckNeeded { get; set; }
    }
}
