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

        public static bool IsAdmin
        {
            get
            {
                if (activeUser == null) return false;
                string r = activeUser.Role ?? "";
                return r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                       r.Equals("Owner/Admin", StringComparison.OrdinalIgnoreCase) ||
                       r.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                       r.Contains("Owner", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
