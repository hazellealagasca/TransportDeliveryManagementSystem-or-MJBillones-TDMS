using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 6, 2026: the service invoice (SI) number and the collection receipt number are typed by the person, never made
    /// by the system. Both are required and can't repeat (cancelled invoices and voided receipts keep their numbers).
    /// </summary>
    public static partial class MJdatabase
    {
        public const int SiNumberMaxLength = 30;
        private static bool DraftsOff => true;
        /// <summary>Links a service invoice made in Billing to its special trip (Nov 6).</summary>
        public static void LinkSpecialTripInvoice(int invoiceId, int tripId) { if (invoiceId > 0 && tripId > 0) LinkInvoiceToSpecialTrip(invoiceId, tripId); }

        /// <summary>Why this SI number can't be used, or null when it is fine. <paramref name="exceptInvoiceId"/> is the invoice being edited.</summary>
        public static string? CheckSiNumber(string? number, int exceptInvoiceId = 0)
        {
            string n = (number ?? "").Trim();
            if (n.Length == 0) return "Type the Service Invoice No. (it is not made by the system).";
            if (n.Length > SiNumberMaxLength) return $"The Service Invoice No. can be at most {SiNumberMaxLength} characters.";
            if (!Regex.IsMatch(n, @"^[A-Za-z0-9][A-Za-z0-9\-/]*$")) return "The Service Invoice No. can only have letters, digits, - and /.";
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = "SELECT InvoiceNumber, ClientName, COALESCE(Status,'') FROM Invoices WHERE TRIM(InvoiceNumber) = @n COLLATE NOCASE AND Id <> @id LIMIT 1";
            c.Parameters.AddWithValue("@n", n);
            c.Parameters.AddWithValue("@id", exceptInvoiceId);
            using var r = c.ExecuteReader();
            if (r.Read())
            {
                string st = r.GetString(2);
                return $"Service Invoice No. {n} is already used ({(r.IsDBNull(1) ? "" : r.GetString(1))}{(st.Length > 0 ? ", " + st : "")}). Numbers can't repeat.";
            }
            return null;
        }

        /// <summary>Why this receipt number can't be used, or null. A receipt for a statement is stored once per invoice with the same
        /// number, so rows of <paramref name="sameReceiptGroup"/> (the receipt being edited) don't count as a repeat.</summary>
        public static string? CheckReceiptNumber(string? number, string? sameReceiptGroup = null)
        {
            string n = (number ?? "").Trim();
            if (n.Length == 0) return "Type the Collection Receipt No. (it is not made by the system).";
            if (!Regex.IsMatch(n, @"^\d{4,8}$")) return "The Collection Receipt No. must be 4 to 8 digits (numbers only).";
            string key = n.TrimStart('0');
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = "SELECT ReceiptNumber, COALESCE(IsVoid,0) FROM CollectionReceipts";
            using var r = c.ExecuteReader();
            while (r.Read())
            {
                string other = r.IsDBNull(0) ? "" : r.GetString(0).Trim();
                if (sameReceiptGroup != null && string.Equals(other, sameReceiptGroup.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                if (other.Length > 0 && other.All(char.IsDigit) && other.TrimStart('0') == key)
                    return $"Collection Receipt No. {n} is already used{(Convert.ToInt32(r.GetValue(1)) == 1 ? " (by a voided receipt)" : "")}. Numbers can't repeat.";
                if (string.Equals(other, n, StringComparison.OrdinalIgnoreCase))
                    return $"Collection Receipt No. {n} is already used. Numbers can't repeat.";
            }
            return null;
        }

        /// <summary>True when the special trip has a service invoice that is not cancelled.</summary>
        public static bool SpecialTripHasActiveInvoice(int tripId)
        {
            if (tripId <= 0) return false;
            var trip = GetSpecialTripById(tripId);
            return trip != null && FindSpecialTripInvoice(trip, includeCancelled: false) != null;
        }

        /// <summary>Nov 6: drafts are no longer kept; clears the ones saved before, once.</summary>
        public static void ClearSavedDraftsOnce()
        {
            const string key = "Nov6_FormDraftsCleared";
            try
            {
                if (GetSystemSetting(key, "0") == "1") return;
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using var c = conn.CreateCommand();
                    c.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='FormDrafts'";
                    if (Convert.ToInt32(c.ExecuteScalar() ?? 0) > 0)
                    {
                        c.CommandText = "DELETE FROM FormDrafts";
                        c.ExecuteNonQuery();
                    }
                }
                SetSystemSetting(key, "1", "Saved form drafts cleared once (Nov 6, 2026: forms start blank, no drafts)");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
