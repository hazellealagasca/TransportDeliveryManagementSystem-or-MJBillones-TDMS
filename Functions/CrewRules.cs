using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Crew rules that do not need a screen: address, driver and aide licence, status-change reason and the list filters.
    /// The Crew screen and the quick "Add driver / Add aide" window both use these, so they can never disagree.
    /// </summary>
    public static class CrewRules
    {
        public const string LicPro = "Professional Driver's License";
        public const string LicNonPro = "Non-Professional Driver's License";
        public const string LicNone = "None";
        public const int AddressMax = 250;
        public const int ReasonMax = 500;
        public const string TypeRegular = "Regular";
        public const string TypeSeasonal = "Seasonal";
        public const string StatusChangeKind = "Status change";

        /// <summary>Null when fine, otherwise the problem. The address is required and at most 250 characters.</summary>
        public static string? CheckAddress(string? address)
        {
            string a = InputRules.Tidy(address);
            if (a.Length == 0) return "Enter the address.";
            if (a.Length > AddressMax) return $"The address can be at most {AddressMax} characters.";
            return null;
        }

        /// <summary>Null when fine, otherwise the problem. A status change needs a description of at most 500 characters.</summary>
        public static string? CheckStatusReason(string? reason)
        {
            string r = InputRules.Tidy(reason);
            if (r.Length == 0) return "Enter the reason for the status change.";
            if (r.Length > ReasonMax) return $"The reason can be at most {ReasonMax} characters.";
            return null;
        }

        /// <summary>
        /// A driver must have a Professional licence with its issue and expiry dates (the Non-Professional choice is gone for drivers).
        /// An aide's licence is optional: None or blank is fine; if one is given its dates are checked. Returns null when fine, otherwise the problem and the field.
        /// </summary>
        public static (string Message, string Field)? CheckLicense(bool driver, string? licenseClass, DateTime? issued, DateTime? expiry)
        {
            string lc = (licenseClass ?? "").Trim();
            bool none = lc.Length == 0 || lc.Equals(LicNone, StringComparison.OrdinalIgnoreCase);
            if (driver)
            {
                if (lc != LicPro) return ("A driver must have a Professional Driver's License.", "license");
            }
            else
            {
                if (none) return null;
                if (lc != LicPro && lc != LicNonPro) return ("Choose the licence type, or leave it as None.", "license");
            }
            if (issued == null) return ("Enter the date the license was issued.", "issued");
            if (expiry == null) return ("Enter the license expiry date.", "expiry");
            var iss = issued.Value.Date; var exp = expiry.Value.Date;
            if (iss > DateTime.Today) return ("The license issue date can't be in the future.", "issued");
            if (exp <= iss) return ("The license expiry date must be after the issue date.", "expiry");
            if ((exp - iss).TotalDays > 366 * 10 + 3) return ("A Philippine driver's license is valid for at most 10 years. Check the dates.", "expiry");
            if ((exp - iss).TotalDays < 180) return ("The license validity looks too short. Check the dates.", "expiry");
            if (driver && exp < DateTime.Today) return ($"This driver's license expired on {Fmt.Date(exp)}. Enter the renewed license before saving.", "expiry");
            return null;
        }

        /// <summary>The crew list narrowed by employment type (All / Regular / Seasonal) and status (All / Active / On trip / Inactive).</summary>
        public static List<Crew> Filter(IEnumerable<Crew> crews, string? type, string? status)
        {
            string t = (type ?? "All").Trim(), s = (status ?? "All").Trim();
            return crews.Where(c =>
                (t.Equals("All", StringComparison.OrdinalIgnoreCase) || t.Length == 0 || c.EmploymentType.Equals(t, StringComparison.OrdinalIgnoreCase)) &&
                (s.Equals("All", StringComparison.OrdinalIgnoreCase) || s.Length == 0 || c.AvailabilityStatus.Equals(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }
}
