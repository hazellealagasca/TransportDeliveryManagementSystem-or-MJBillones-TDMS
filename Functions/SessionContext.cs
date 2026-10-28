using System;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class SessionContext
    {
        private static User? activeUser;

        public static User? ActiveUser
        {
            get => activeUser;
            set => activeUser = value;
        }

        public static string CurrentUsername
        {
            get
            {
                if (activeUser != null && !string.IsNullOrWhiteSpace(activeUser.Username))
                {
                    return activeUser.Username.Trim().ToLowerInvariant();
                }
                return (Environment.UserName ?? "default").Trim().ToLowerInvariant();
            }
        }

        private static readonly string[] AdminRoleNames = { "Admin", "Administrator", "Owner", "Owner/Admin", "Owner / Admin", "Business Owner" };
        /// <summary>Exact-match check so a role like "Admin Assistant" does not silently get full admin rights.</summary>
        public static bool IsAdminRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            string r = role.Trim();
            foreach (var name in AdminRoleNames)
            {
                if (r.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        public static bool IsAdmin
        {
            get
            {
                if (activeUser == null) return false;
                return IsAdminRole(activeUser.Role);
            }
        }

        public static bool IsDispatcher
        {
            get
            {
                if (activeUser == null) return false;
                string r = activeUser.Role ?? "";
                return r.Contains("Dispatcher", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool IsBillingClerk
        {
            get
            {
                if (activeUser == null) return false;
                string r = activeUser.Role ?? "";
                return r.Contains("Billing", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool CanAccessOperations
        {
            get
            {
                if (activeUser == null) return false;
                if (IsAdmin || IsDispatcher) return true;
                if (IsBillingClerk) return false;
                return true;
            }
        }

        public static bool CanAccessFinance
        {
            get
            {
                if (activeUser == null) return false;
                if (IsAdmin || IsBillingClerk) return true;
                if (IsDispatcher) return false;
                return true;
            }
        }

        public static bool CanAccessResources
        {
            get
            {
                if (activeUser == null) return false;
                if (IsAdmin || IsDispatcher) return true;
                if (IsBillingClerk) return false;
                return true;
            }
        }

        public static bool CanAccessMasterData
        {
            get
            {
                if (activeUser == null) return false;
                if (IsAdmin) return true;
                string r = activeUser.Role ?? "";
                return r.Equals("Office Staff", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool CanAccessSettings
        {
            get => IsAdmin;
        }
    }
}
