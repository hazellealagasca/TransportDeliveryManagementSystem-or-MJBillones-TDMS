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

        /// <summary>Why this SI number can't be used, or null when it is fine. <paramref name="exceptInvoiceId"/> is the invoice being edited or reinstated.
        /// The rules live in <see cref="NumberIndex.Check"/> (plan 17): strict key, cancelled numbers stay used, a typing-mistake number is kept for its own record.</summary>
        public static string? CheckSiNumber(string? number, int exceptInvoiceId = 0)
        {
            var c = LoadNumberIndex().Check(NumberKind.SI, number, exceptInvoiceId);
            return c.Blocked ? c.Why() : null;
        }

        /// <summary>Why this receipt number can't be used, or null. A receipt for a statement is stored once per invoice with the same
        /// number, so rows of <paramref name="sameReceiptGroup"/> (the receipt being edited) don't count as a repeat.</summary>
        public static string? CheckReceiptNumber(string? number, string? sameReceiptGroup = null)
        {
            var c = LoadNumberIndex().Check(NumberKind.CR, number, 0, sameReceiptGroup);
            return c.Blocked ? c.Why() : null;
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
