using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// What each staff account may open. The Owner / Administrator chooses this per account in
    /// Settings → User Accounts → right-click → "Access & permissions…". Administrators can always use everything.
    /// Stored in SystemSettings as "UserAccess.&lt;username&gt;" = the screens that are hidden for that account.
    /// </summary>
    public static class Permissions
    {
        public sealed record Area(string Module, string Tab, string Label, string Description)
        {
            public string Key => Module + "." + Tab;
        }
        public static readonly Area[] Areas =
        {
            new("Operations", "Dispatch", "Loading schedule & dispatch", "Create and edit loading orders and dispatches."),
            new("Operations", "SpecialTrips", "Special trip history", "Book and manage special trips."),
            new("Operations", "Incidents", "Incident & damage logs", "Log damaged or missing crates."),
            new("Operations", "TripReports", "Trip reports", "File trip reports when trucks come back."),
            new("Operations", "History", "Order history", "Look up every past order, dispatch and special trip."),
            new("Resources", "Trucks", "Trucks", "The fleet list and truck profiles."),
            new("Resources", "Crew", "Crew management", "Drivers and aides."),
            new("Resources", "CrewHistory", "Crew trip history", "Which trips each crew member made."),
            new("Resources", "History", "Resource history", "Changes made to trucks and crew."),
            new("Payments", "StatementOfAccount", "Statements of account", "Monthly statements for clients."),
            new("Payments", "Billing", "Billing invoices", "Create and manage invoices."),
            new("Payments", "Collections", "Collections & receipts", "Record payments received."),
            new("Payments", "History", "Finance history", "Changes made to billing and collections."),
            new("MasterData", "Clients", "Clients", "Client companies, their stores and details."),
            new("MasterData", "Routes", "Delivery routes", "Routes and the stores on them."),
            new("MasterData", "Rates", "Rate Matrix", "The price list by route and truck type."),
            new("MasterData", "History", "Master data history", "Changes made to clients, routes and rates."),
        };
        public static readonly (string Module, string Label)[] Modules =
        {
            ("Operations", "Operations"), ("Resources", "Resources"), ("Payments", "Payments"), ("MasterData", "Master Data")
        };
        private static string SettingKey(string username) => "UserAccess." + (username ?? "").Trim().ToLowerInvariant();
        private static readonly Dictionary<string, HashSet<string>> cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object gate = new();
        /// <summary>The screens hidden for an account.</summary>
        public static HashSet<string> DeniedFor(string username)
        {
            lock (gate)
            {
                if (cache.TryGetValue(username ?? "", out var hit)) return new HashSet<string>(hit, StringComparer.OrdinalIgnoreCase);
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (var k in MJdatabase.GetSystemSetting(SettingKey(username ?? ""), "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) set.Add(k);
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
                cache[username ?? ""] = set;
                return new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
            }
        }
        /// <summary>Saves the hidden screens for an account. Only an administrator can do this.</summary>
        public static void SetDenied(string username, IEnumerable<string> denied)
        {
            if (!SessionContext.IsAdmin) throw new UnauthorizedAccessException("Only the Owner / Administrator can change what an account can use.");
            var set = new HashSet<string>(denied, StringComparer.OrdinalIgnoreCase);
            MJdatabase.SetSystemSetting(SettingKey(username), string.Join(",", set.OrderBy(x => x)), "Screens hidden for this account");
            lock (gate) cache[username] = set;
            string summary = set.Count == 0 ? "full access" : "hidden: " + string.Join(", ", Areas.Where(a => set.Contains(a.Key)).Select(a => a.Label));
            try { MJdatabase.LogSecurityEvent(SessionContext.CurrentUsername, "PERMISSIONS", $"Access for '{username}' changed ({summary})"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        public static void Reload() { lock (gate) cache.Clear(); }
        /// <summary>True when the signed-in person may open this screen (e.g. "Operations", "Dispatch").</summary>
        public static bool Can(string module, string tab)
        {
            if (SessionContext.IsAdmin) return true;
            string u = SessionContext.CurrentUsername;
            if (string.IsNullOrWhiteSpace(u)) return true;
            return !DeniedFor(u).Contains(module + "." + tab);
        }
        /// <summary>True when at least one screen of the module is open to the signed-in person.</summary>
        public static bool CanAny(string module) => Areas.Where(a => a.Module == module).Any(a => Can(a.Module, a.Tab));
        /// <summary>First screen of the module the person may open, or null.</summary>
        public static string? FirstAllowed(string module, IEnumerable<string> tabsInOrder) => tabsInOrder.FirstOrDefault(t => Can(module, t));
        public static string LabelOf(string module, string tab) => Areas.FirstOrDefault(a => a.Module == module && a.Tab == tab)?.Label ?? tab;
        /// <summary>Short note when a link leads to a screen this account can't open.</summary>
        public static void TellDenied(System.Windows.Forms.Control? near, string module, string tab)
        {
            try
            {
                var f = near?.FindForm() ?? System.Windows.Forms.Form.ActiveForm;
                if (f != null) ToastHelper.ShowToast(f, $"Your account can't open {LabelOf(module, tab)}. Ask the administrator if you need it.", ToastType.Info);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
