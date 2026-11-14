using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 14 (Module 2, master data): the client type (Corporate or Individual), the one automatic store of an Individual client, and the
    /// store codes the system makes when a code is left blank. A generated code is a real, unique code (so routes, store matching and DR
    /// checks work unchanged) that shows as "(no code)" on screens and prints blank.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>What a generated store code shows as on screens.</summary>
        public const string NoCodeText = "(no code)";

        // ------------------------------------------------------------------ generated codes

        /// <summary>The prefix of a client's generated codes: the first 3 letters of its name in capitals ("STO" when the name has no letters).</summary>
        public static string StoreCodePrefix(string? client)
        {
            string letters = new string(CleanStoreText(client).Where(char.IsLetter).ToArray()).ToUpperInvariant();
            letters = Regex.Replace(letters, "[^A-Z]", "");
            if (letters.Length == 0) return "STO";
            return letters.Length > 3 ? letters.Substring(0, 3) : letters;
        }

        /// <summary>
        /// The next free generated code for the client: <c>PREFIX-NNN</c>, one more than the highest number the client already uses with
        /// that prefix, and never a code any store (of any client, archived too) or any of <paramref name="alsoTaken"/> already has.
        /// </summary>
        public static string GenerateStoreCode(string? client, IEnumerable<string>? alsoTaken = null)
        {
            string prefix = StoreCodePrefix(client);
            var pattern = new Regex("^" + Regex.Escape(prefix) + @"-(\d+)$", RegexOptions.IgnoreCase);
            var taken = new HashSet<string>((alsoTaken ?? Array.Empty<string>()).Select(c => CleanStoreCode(c)).Where(c => c.Length > 0), StringComparer.OrdinalIgnoreCase);
            int max = 0;
            void Note(string code) { var m = pattern.Match(CleanStoreText(code)); if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) max = Math.Max(max, n); }
            foreach (var st in GetClientStores(CleanStoreText(client), includeArchived: true)) Note(st.StoreCode);
            foreach (var c in taken) Note(c);
            for (int n = max + 1; n < 1000000; n++)
            {
                string code = $"{prefix}-{n:D3}";
                if (!taken.Contains(code) && FindStoreByCodeAnyClient(code) == null) return code;
            }
            throw new InvalidOperationException("No free store code is left for this client.");
        }

        /// <summary>
        /// Settles the code of a store about to be saved by <see cref="SaveClientStore"/>: a blank code (or "(no code)") keeps the code the
        /// system already made for this store, or makes a new one; a code that was typed is the user's own.
        /// </summary>
        private static void ResolveStoreCodeForSave(ClientStore store, ClientStore? old)
        {
            string typed = CleanStoreCode(string.Equals(CleanStoreText(store.StoreCode), NoCodeText, StringComparison.OrdinalIgnoreCase) ? "" : store.StoreCode);
            if (typed.Length == 0)
            {
                // A store saved before this rule with no code at all stays as it was (its routes hold its name only); only new stores get a code.
                if (old != null && !old.IsGeneratedCode && CleanStoreCode(old.StoreCode).Length == 0) { store.StoreCode = ""; store.IsGeneratedCode = false; return; }
                if (old != null && old.IsGeneratedCode && CleanStoreCode(old.StoreCode).Length > 0) store.StoreCode = old.StoreCode;
                else store.StoreCode = GenerateStoreCode(store.ClientCompanyName);
                store.IsGeneratedCode = true;
                return;
            }
            store.StoreCode = typed;
            store.IsGeneratedCode = old != null && old.IsGeneratedCode && string.Equals(CleanStoreCode(old.StoreCode), typed, StringComparison.OrdinalIgnoreCase);
        }

        private static HashSet<string> generatedCodes = new(StringComparer.OrdinalIgnoreCase);
        private static string generatedCodesFor = "";
        private static long generatedCodesAt;
        private static int generatedCodesVersion, generatedCodesSeen = -1;
        private static readonly object generatedCodesLock = new();

        /// <summary>Forget the remembered list of generated codes (a store was written).</summary>
        internal static void InvalidateGeneratedCodes() { lock (generatedCodesLock) generatedCodesVersion++; }

        /// <summary>The codes the system made (stores saved with a blank code), upper case. Remembered for a moment; a store write refreshes it.</summary>
        public static HashSet<string> GeneratedStoreCodes()
        {
            lock (generatedCodesLock)
            {
                long now = Environment.TickCount64;
                if (generatedCodesSeen == generatedCodesVersion && generatedCodesFor == connectionString && now - generatedCodesAt < 3000) return generatedCodes;
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT TRIM(StoreCode) FROM ClientStores WHERE COALESCE(IsGeneratedCode, 0) = 1 AND TRIM(COALESCE(StoreCode, '')) <> ''";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) set.Add(r.GetString(0));
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
                generatedCodes = set; generatedCodesFor = connectionString; generatedCodesAt = now; generatedCodesSeen = generatedCodesVersion;
                return set;
            }
        }

        /// <summary>True when this code was made by the system (it is not one the user typed).</summary>
        public static bool IsGeneratedStoreCode(string? code)
        {
            string c = CleanStoreText(code);
            return c.Length > 0 && GeneratedStoreCodes().Contains(c);
        }

        /// <summary>The code as a screen shows it: "(no code)" for a generated code, otherwise the code.</summary>
        public static string StoreCodeForScreen(string? code) => IsGeneratedStoreCode(code) ? NoCodeText : (code ?? "");

        /// <summary>The store's code as a screen shows it: "(no code)" when the store was saved with a blank code.</summary>
        public static string StoreCodeForScreen(ClientStore store) => store.IsGeneratedCode && (store.StoreCode ?? "").Trim().Length > 0 ? NoCodeText : (store.StoreCode ?? "");

        /// <summary>The code as a printout shows it: blank for a generated code, otherwise the code.</summary>
        public static string StoreCodeForPrint(string? code) => IsGeneratedStoreCode(code) ? "" : (code ?? "");

        // ------------------------------------------------------------------ Individual clients

        /// <summary>The code of an Individual client's one store: <c>IND-</c> and the client's id in four digits.</summary>
        public static string IndividualStoreCode(int clientId) => "IND-" + clientId.ToString("D4");

        /// <summary>True when the named client is an Individual client (false for Corporate clients and unknown names).</summary>
        public static bool IsIndividualClient(string? clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName)) return false;
            return GetClientByCompanyName(clientName)?.IsIndividual == true;
        }

        /// <summary>An Individual client's one store (null when the client is not Individual or has no store yet).</summary>
        public static ClientStore? GetIndividualStore(string? clientName)
        {
            var cl = string.IsNullOrWhiteSpace(clientName) ? null : GetClientByCompanyName(clientName);
            if (cl == null || !cl.IsIndividual) return null;
            string code = IndividualStoreCode(cl.Id);
            var stores = GetClientStores(cl.CompanyName);
            return stores.FirstOrDefault(s => string.Equals(CleanStoreCode(s.StoreCode), code, StringComparison.OrdinalIgnoreCase)) ?? stores.FirstOrDefault();
        }

        /// <summary>
        /// Creates or updates the one store of an Individual client: code <c>IND-####</c>, the client's name, the drop-off address, a generated
        /// code. Called by <see cref="SaveClient"/>. Exactly one row is kept; saving again only changes its name and address.
        /// </summary>
        public static void EnsureIndividualStore(string clientName, string dropOffAddress)
        {
            clientName = CleanStoreText(clientName);
            var cl = GetClientByCompanyName(clientName);
            if (cl == null || !cl.IsIndividual) return;
            string code = IndividualStoreCode(cl.Id), address = CleanStoreText(dropOffAddress);
            var all = GetClientStores(clientName, includeArchived: true);
            var existing = all.FirstOrDefault(s => string.Equals(CleanStoreCode(s.StoreCode), code, StringComparison.OrdinalIgnoreCase));
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (existing != null)
            {
                cmd.CommandText = "UPDATE ClientStores SET StoreName = @n, Address = @a, ClientId = @cid, ClientCompanyName = @cl, IsGeneratedCode = 1, IsArchived = 0, ArchivedAt = '' WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", existing.Id);
            }
            else
            {
                var owner = FindStoreByCodeAnyClient(code);
                if (owner != null) code = GenerateStoreCode(clientName); // the IND code belongs to another client's store: never take it over
                cmd.CommandText = "INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson, Address, IsGeneratedCode) VALUES (@cid, @cl, @c, @n, '', '', @a, 1)";
                cmd.Parameters.AddWithValue("@c", code);
            }
            cmd.Parameters.AddWithValue("@n", clientName);
            cmd.Parameters.AddWithValue("@a", address);
            cmd.Parameters.AddWithValue("@cid", cl.Id);
            cmd.Parameters.AddWithValue("@cl", clientName);
            cmd.ExecuteNonQuery();
            InvalidateGeneratedCodes();
            try { AppEvents.Raise(AppDataKind.Stores); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
