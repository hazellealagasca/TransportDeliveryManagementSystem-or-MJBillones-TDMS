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
        public string CrewCode => $"CRW-{Id:D3}";
        /// <summary>"Professional Driver's License", "Non-Professional Driver's License" or "None". The license number itself is not stored.</summary>
        public string LicenseClass { get; set; } = "None";
        public DateTime? LicenseIssued { get; set; }
        public DateTime? LicenseExpiry { get; set; }
        public bool HasLicense => !string.IsNullOrWhiteSpace(LicenseClass) && !LicenseClass.Equals("None", StringComparison.OrdinalIgnoreCase);
        public bool IsDriverRole => (Role ?? "").IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
        public bool LicenseExpiredOn(DateTime day) => HasLicense && LicenseExpiry.HasValue && LicenseExpiry.Value.Date < day.Date;
        /// <summary>Short text for lists, e.g. "Professional · exp. Mar 4, 2029" or "None".</summary>
        public string LicenseDisplay
        {
            get
            {
                if (!HasLicense) return "None";
                string kind = LicenseClass.StartsWith("Professional", StringComparison.OrdinalIgnoreCase) ? "Professional" : "Non-Professional";
                if (!LicenseExpiry.HasValue) return kind + " · expiry not set";
                return LicenseExpiry.Value.Date < DateTime.Today ? $"{kind} · EXPIRED {LicenseExpiry:MMM d, yyyy}" : $"{kind} · exp. {LicenseExpiry:MMM d, yyyy}";
            }
        }
    }
    /// <summary>Extra specifications kept for a truck's profile.</summary>
    public class TruckSpecs
    {
        public int TruckId { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string YearModel { get; set; } = string.Empty;
        public string ChassisNumber { get; set; } = string.Empty;
        public string EngineNumber { get; set; } = string.Empty;
        public int OdometerKm { get; set; }
        public DateTime? RegistrationExpiry { get; set; }
        public DateTime? InsuranceExpiry { get; set; }
    }
    /// <summary>A file attached to a record (dispatch / trip, truck): POD, receipt, trip sheet, photo…</summary>
    public class Attachment
    {
        public int Id { get; set; }
        public string OwnerType { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public string OwnerRef { get; set; } = string.Empty;
        public string Category { get; set; } = "Other";
        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string AddedBy { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.Now;
        public bool IsImage => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" }.Contains(System.IO.Path.GetExtension(FileName).ToLowerInvariant());
        public string SizeDisplay => SizeBytes >= 1024 * 1024 ? $"{SizeBytes / 1024.0 / 1024.0:0.#} MB" : $"{Math.Max(1, SizeBytes / 1024)} KB";
    }
    /// <summary>One maintenance or repair job done on a truck.</summary>
    public class TruckMaintenance
    {
        public int Id { get; set; }
        public int TruckId { get; set; }
        public DateTime ServiceDate { get; set; } = DateTime.Today;
        public string Task { get; set; } = string.Empty;
        public string Category { get; set; } = "Preventive";
        public int OdometerKm { get; set; }
        public decimal Cost { get; set; }
        public string ServiceProvider { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime? NextDueDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
    /// <summary>A violation, performance remark or incident note kept on a crew member's record.</summary>
    public class CrewRecord
    {
        public int Id { get; set; }
        public int CrewId { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Today;
        public string Kind { get; set; } = "Violation";
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string Severity { get; set; } = "Minor";
        public string CreatedBy { get; set; } = string.Empty;
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
        public string TIN { get; set; } = string.Empty;
        public string ContactPersonLastName { get; set; } = string.Empty;
        public string ContactPersonFirstName { get; set; } = string.Empty;
        public string ContactPersonMiddleName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Values of user-defined client columns, as a JSON object keyed by column key.</summary>
        public string ExtraFieldsJson { get; set; } = string.Empty;
        /// <summary>False when the client was deactivated (the soft delete for clients).</summary>
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ClientStore
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientCompanyName { get; set; } = string.Empty;
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string DestinationRoute { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        /// <summary>Values of user-defined store columns, as a JSON object keyed by column key.</summary>
        public string ExtraFieldsJson { get; set; } = string.Empty;
    }

    public class ClientTripSummary
    {
        public int CompletedCount { get; set; }
        public string LatestCompletedDate { get; set; } = string.Empty;
        public int SpecialCount { get; set; }
        public string LatestSpecialDate { get; set; } = string.Empty;
        public int CancelledCount { get; set; }
        public string LatestCancelledDate { get; set; } = string.Empty;
    }

    public class ClientPaymentSummary
    {
        public int TotalInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public string LatestReceiptNumber { get; set; } = string.Empty;
        public string LatestReceiptDate { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Current";
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
        public string RouteTypeDisplay => IsMultiDrop ? $"🗺️ {TotalStopsCount} Stops" : "📍 Direct";
        public string AideDisplay => !string.IsNullOrWhiteSpace(AideNames) ? AideNames : (!string.IsNullOrWhiteSpace(AideName) ? AideName : "None");
        public string CleanRouteDisplay
        {
            get
            {
                string raw = !string.IsNullOrWhiteSpace(Destination) ? Destination.Trim() : LoadingScheduleInfo.Trim();
                if (string.IsNullOrWhiteSpace(raw)) return "Direct Route";
                int colonIdx = raw.IndexOf(':');
                if (colonIdx > 0)
                {
                    return raw.Substring(0, colonIdx).Trim();
                }
                int arrowIdx = raw.IndexOf("➔");
                if (arrowIdx > 0)
                {
                    return raw.Substring(0, arrowIdx).Trim() + $" (+{Math.Max(1, TotalStopsCount - 1)} stops)";
                }
                return raw;
            }
        }
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
        public string StoreCode { get; set; } = string.Empty;
        public string DrNumbers { get; set; } = string.Empty;
        public string OthersBox { get; set; } = string.Empty;
        public string OthersPfoods { get; set; } = string.Empty;
        public string OthersMi { get; set; } = string.Empty;
    }

    public class SpecialTrip
    {
        public int Id { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public DateTime DispatchDate { get; set; } = DateTime.Now;
        public string Company { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = "08:00 AM";
        public string Address { get; set; } = string.Empty;
        public string ServiceType { get; set; } = "Dedicated / Special Delivery";
        public string Status { get; set; } = "Pending";
        public bool ModeOfPaymentWithReport { get; set; } = true;
        public bool WithSOA { get; set; } = false;
        public bool WithServiceInvoice { get; set; } = false;
        public bool WithCollection { get; set; } = false;
        public int TruckId { get; set; }
        public string TruckPlate { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public int AideId { get; set; }
        public string AideName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<SpecialTripStop> Stops { get; set; } = new List<SpecialTripStop>();
        public int TotalStopsCount => Stops?.Count ?? 0;
    }

    public class SpecialTripStop
    {
        public int Id { get; set; }
        public int SpecialTripId { get; set; }
        public int SequenceNo { get; set; } = 1;
        public string StandardId { get; set; } = string.Empty;
        public string DeliveryDetails { get; set; } = string.Empty;
        public string DriverRoute { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string ExtraColumnsJson { get; set; } = string.Empty;
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
        public string TruckTimeIn { get; set; } = "2042";
        public string TruckTimeOut { get; set; } = "0139";
        public string DtrApprovedBy { get; set; } = "RAMIL MENDOZA";
        public string ReturnCheckedBy { get; set; } = string.Empty;
        public string DtrPreparedBy { get; set; } = "XERXES RYAN MANRIQUE";
        public string DrPreparedBy { get; set; } = "VINJOHN VILLAMOR";
        public string DrCheckedBy { get; set; } = "JOE-EM FLORES";
        public string GuardName { get; set; } = string.Empty;
    }

    /// <summary>What one crew member (driver or aide) is paid for one trip.</summary>
    public class TripCrewPay
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public int CrewId { get; set; }
        public string CrewName { get; set; } = string.Empty;
        public string Role { get; set; } = "Driver";
        public decimal BasePay { get; set; }
        public decimal Allowance { get; set; }
        public decimal Extra { get; set; }
        public decimal Deduction { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal NetPay => BasePay + Allowance + Extra - Deduction;
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
        public string ClientAddress { get; set; } = string.Empty;
        public string ClientTin { get; set; } = string.Empty;
        public string PeriodCovered { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public string DeliveryType { get; set; } = "REF";
        public decimal TotalSalesVatInc { get; set; }
        public decimal VatAmount { get; set; }
        public decimal NetOfVat { get; set; }
        public decimal FuelSurchargeTotal { get; set; }
        public decimal WithholdingTax { get; set; }
        public decimal TotalAmount { get; set; }
        public string SalesInvoiceNo { get; set; } = string.Empty;
        public string PreparedBy { get; set; } = "Marie Ann J. Billones";
        public string ReceivedBy1 { get; set; } = "Ma'am Monette Rubonal";
        public string ReceivedBy2 { get; set; } = "Ma'am Francis Pauline Rodillas";
        public string Status { get; set; } = "Issued";
        public List<StatementOfAccountItem> Items { get; set; } = new List<StatementOfAccountItem>();
    }

    public class StatementOfAccountItem
    {
        public int Id { get; set; }
        public int SoaId { get; set; }
        public int TripNumber { get; set; }
        public string DateReceived { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string DrNumbers { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = "REF";
        public string PlateNumber { get; set; } = string.Empty;
        public string Tonnage { get; set; } = "6T";
        public int DropCount { get; set; }
        public decimal Amount { get; set; }
        public decimal FuelSurcharge { get; set; }
        public string DriverHelper { get; set; } = string.Empty;
        public decimal DriverWage { get; set; }
        public decimal HelperWage { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal DieselCost { get; set; }
        public decimal Excess { get; set; }
        public int DeliveryId { get; set; }
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

    public class DeliveryRoute
    {
        public int Id { get; set; }
        public string ClientCompanyName { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ConnectedPlaces { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public List<string> PlacesList => (ConnectedPlaces ?? "").Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        public string FormattedPlaces => string.Join(" ➔ ", PlacesList);
        public int TotalStops => PlacesList.Count;
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string ShortcutDisplay => string.IsNullOrWhiteSpace(RouteCode) ? RouteName : $"{RouteCode} - {RouteName} ({TotalStops} stores)";

        public static (string StoreCode, string StoreName) SplitStoreCodeAndName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ("", "");
            string clean = raw.Trim();
            int dashIdx = clean.IndexOf(" - ", StringComparison.Ordinal);
            if (dashIdx > 0 && dashIdx <= 12)
            {
                string left = clean.Substring(0, dashIdx).Trim();
                string right = clean.Substring(dashIdx + 3).Trim();
                if (left.All(c => char.IsDigit(c) || char.IsLetter(c)))
                    return (left, right);
            }
            int spaceIdx = clean.IndexOf(' ');
            if (spaceIdx >= 4 && spaceIdx <= 10)
            {
                string firstToken = clean.Substring(0, spaceIdx).Trim();
                if (firstToken.All(char.IsDigit))
                    return (firstToken, clean.Substring(spaceIdx + 1).Trim());
            }
            return ("", clean);
        }
    }

    public class TruckCategoryItem
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CrateCapacity { get; set; } = 300;
        public int AssignedTrucksCount { get; set; }
    }

    public class CrewRoleItem
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int ActiveCrewCount { get; set; }
    }
}
