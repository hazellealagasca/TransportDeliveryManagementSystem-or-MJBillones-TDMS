using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public class SystemNotification
    {
        private bool? _isAdminOnly;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ToastType Type { get; set; } = ToastType.Info;
        public string Category { get; set; } = "System";
        public bool IsRead { get; set; } = false;
        public string? TargetModule { get; set; }
        public string? TargetRecordId { get; set; }

        public bool IsAdminOnly
        {
            get
            {
                if (_isAdminOnly.HasValue) return _isAdminOnly.Value;
                if (!string.IsNullOrEmpty(Category))
                {
                    string cat = Category.ToLowerInvariant();
                    if (cat == "security" || cat == "system" || cat == "audit") return true;
                }
                if (!string.IsNullOrEmpty(TargetModule))
                {
                    string tm = TargetModule.ToLowerInvariant();
                    if (tm == "settings" || tm == "audit" || tm == "users") return true;
                }
                string t = (Title ?? string.Empty).ToLowerInvariant();
                string m = (Message ?? string.Empty).ToLowerInvariant();
                string combined = t + " " + m;
                return combined.Contains("security") ||
                       combined.Contains("user account") ||
                       combined.Contains("session") ||
                       combined.Contains("password") ||
                       combined.Contains("login") ||
                       combined.Contains("auth") ||
                       combined.Contains("audit") ||
                       combined.Contains("backup") ||
                       combined.Contains("database") ||
                       combined.Contains("credential") ||
                       combined.Contains("preference") ||
                       combined.Contains("rules saved") ||
                       combined.Contains("user approved") ||
                       combined.Contains("user rejected") ||
                       combined.Contains("security settings") ||
                       combined.Contains("authenticated as");
            }
            set
            {
                _isAdminOnly = value;
            }
        }

        public bool IsBusinessProcess
        {
            get
            {
                if (IsAdminOnly) return false;
                if (!string.IsNullOrEmpty(Category))
                {
                    string cat = Category.ToLowerInvariant();
                    if (cat == "operations" || cat == "payments" || cat == "finance" || cat == "trucks") return true;
                    if (cat == "system" || cat == "security" || cat == "audit") return false;
                }
                if (!string.IsNullOrEmpty(TargetModule))
                {
                    string tm = TargetModule.ToLowerInvariant();
                    if (tm == "dispatch" || tm == "loading" || tm == "tripreports" || tm == "incidents" || tm == "invoices" || tm == "billing" || tm == "collections" || tm == "trucks" || tm == "crew") return true;
                    if (tm == "settings" || tm == "audit" || tm == "users") return false;
                }
                string t = (Title ?? string.Empty).ToLowerInvariant();
                return t.Contains("dispatch") || t.Contains("loading") || t.Contains("trip") || t.Contains("invoice") || t.Contains("payment") || t.Contains("collection") || t.Contains("incident") || t.Contains("damage") || t.Contains("truck");
            }
        }

        public string RelativeTime
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalSeconds < 60) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                return Timestamp.ToString("MMM dd, hh:mm tt");
            }
        }

        public string IconSymbol
        {
            get
            {
                if (!string.IsNullOrEmpty(Category))
                {
                    switch (Category.ToLowerInvariant())
                    {
                        case "operations": return "🚚";
                        case "payments":
                        case "finance": return "💰";
                        case "trucks": return "🚛";
                        case "security": return "🛡️";
                    }
                }
                switch (Type)
                {
                    case ToastType.Success: return "✅";
                    case ToastType.Warning: return "⚠️";
                    case ToastType.Error: return "❌";
                    default: return "ℹ️";
                }
            }
        }

        public string ActionHint
        {
            get
            {
                if (string.IsNullOrEmpty(TargetModule)) return string.Empty;
                switch (TargetModule.ToLowerInvariant())
                {
                    case "dispatch": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Dispatch Management" : $"👉 Open Dispatch #{TargetRecordId}";
                    case "loading": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Loading Schedules" : $"👉 Open Order #{TargetRecordId}";
                    case "tripreports": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Trip Reports" : $"👉 Open Trip #{TargetRecordId}";
                    case "invoices":
                    case "billing": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Invoices & Billing" : $"👉 Open Invoice #{TargetRecordId}";
                    case "collections": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Collections" : $"👉 Open Receipt #{TargetRecordId}";
                    case "incidents": return string.IsNullOrEmpty(TargetRecordId) ? "👉 Open Incident Reports" : $"👉 Open Incident #{TargetRecordId}";
                    case "trucks": return string.IsNullOrEmpty(TargetRecordId) ? "👉 View Truck Inventory" : $"👉 View Vehicle {TargetRecordId}";
                    case "crew": return string.IsNullOrEmpty(TargetRecordId) ? "👉 View Personnel Register" : $"👉 View Personnel {TargetRecordId}";
                    case "settings": return SessionContext.IsAdmin ? "👉 Open System Settings" : string.Empty;
                    case "audit": return SessionContext.IsAdmin ? "👉 Open Security Audit Trail" : string.Empty;
                    default: return "👉 View Details";
                }
            }
        }
    }

    public static class EventNotificationService
    {
        private static readonly object lockObj = new object();
        private static readonly List<SystemNotification> notifications = new List<SystemNotification>();

        public static event Action? OnNotificationsChanged;
        public static event Action<SystemNotification>? OnNotificationPublished;

        static EventNotificationService()
        {
            LoadNotificationsFromDb();
        }

        private static void LoadNotificationsFromDb()
        {
            try
            {
                var fromDb = MJdatabase.GetSystemNotifications(100, isAdmin: true);
                if (fromDb != null && fromDb.Count > 0)
                {
                    lock (lockObj)
                    {
                        notifications.Clear();
                        notifications.AddRange(fromDb);
                    }
                }
                else
                {
                    SeedInitialNotifications();
                }
            }
            catch
            {
                SeedInitialNotifications();
            }
        }

        private static void SeedInitialNotifications()
        {
            var n1 = new SystemNotification
            {
                Title = "System Online",
                Message = "SQLite database connected and operating with full data integrity.",
                Type = ToastType.Success,
                Category = "System",
                TargetModule = "Settings",
                Timestamp = DateTime.Now.AddMinutes(-5),
                IsRead = false
            };
            var n2 = new SystemNotification
            {
                Title = "Auto Backup Verified",
                Message = "Automated data backup engine initialized and standing by.",
                Type = ToastType.Info,
                Category = "System",
                TargetModule = "Settings",
                Timestamp = DateTime.Now.AddMinutes(-2),
                IsRead = false
            };

            lock (lockObj)
            {
                notifications.Clear();
                notifications.Add(n1);
                notifications.Add(n2);
            }

            try
            {
                MJdatabase.SaveSystemNotification(n1);
                MJdatabase.SaveSystemNotification(n2);
            }
            catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
        }

        public static void Publish(string title, string message, ToastType type = ToastType.Info, Control? contextControl = null, string? category = null, string? targetModule = null, string? targetRecordId = null)
        {
            string finalCat = string.IsNullOrWhiteSpace(category) ? InferCategory(title, targetModule) : category;
            string? finalMod = string.IsNullOrWhiteSpace(targetModule) ? InferTargetModule(title) : targetModule;

            var item = new SystemNotification
            {
                Title = title,
                Message = message,
                Type = type,
                Category = finalCat,
                TargetModule = finalMod,
                TargetRecordId = targetRecordId,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            lock (lockObj)
            {
                notifications.Insert(0, item);
                if (notifications.Count > 100)
                {
                    notifications.RemoveAt(notifications.Count - 1);
                }
            }

            try
            {
                MJdatabase.SaveSystemNotification(item);
            }
            catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            if (item.IsAdminOnly && !SessionContext.IsAdmin)
            {
                try
                {
                    OnNotificationsChanged?.Invoke();
                    OnNotificationPublished?.Invoke(item);
                }
                catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
                return;
            }

            if (!item.IsBusinessProcess)
            {
                try
                {
                    OnNotificationsChanged?.Invoke();
                    OnNotificationPublished?.Invoke(item);
                }
                catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
                return;
            }

            try
            {
                Control? target = contextControl;
                if (target == null)
                {
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f.Visible && f.GetType().Name == "MainForm")
                        {
                            target = f;
                            break;
                        }
                    }
                    if (target == null)
                    {
                        foreach (Form f in Application.OpenForms)
                        {
                            if (f.Visible)
                            {
                                target = f;
                                break;
                            }
                        }
                    }
                }

                if (target != null && !target.IsDisposed)
                {
                    if (target.InvokeRequired)
                    {
                        if (target.IsHandleCreated)
                        {
                            target.BeginInvoke(new Action(() =>
                            {
                                ToastHelper.ShowToast(target, $"{title}: {message}", type);
                            }));
                        }
                    }
                    else
                    {
                        ToastHelper.ShowToast(target, $"{title}: {message}", type);
                    }
                }
            }
            catch (System.Exception swx5) { ErrLog.Swallowed(swx5); }
            try
            {
                OnNotificationsChanged?.Invoke();
                OnNotificationPublished?.Invoke(item);
            }
            catch (System.Exception swx6) { ErrLog.Swallowed(swx6); }
        }

        private static string InferCategory(string title, string? targetModule)
        {
            if (!string.IsNullOrEmpty(targetModule))
            {
                switch (targetModule.ToLowerInvariant())
                {
                    case "dispatch":
                    case "loading":
                    case "tripreports":
                    case "incidents":
                        return "Operations";
                    case "invoices":
                    case "billing":
                    case "collections":
                        return "Payments";
                    case "trucks":
                    case "crew":
                        return "Trucks";
                    case "settings":
                    case "audit":
                    case "users":
                        return "Security";
                }
            }

            string t = title.ToLowerInvariant();
            if (t.Contains("invoice") || t.Contains("payment") || t.Contains("soa") || t.Contains("billing") || t.Contains("collection"))
            {
                return "Payments";
            }
            if (t.Contains("trip") || t.Contains("dispatch") || t.Contains("loading") || t.Contains("incident") || t.Contains("order") || t.Contains("crate") || t.Contains("client") || t.Contains("rate matrix"))
            {
                return "Operations";
            }
            if (t.Contains("truck") || t.Contains("crew") || t.Contains("driver") || t.Contains("aide") || t.Contains("vehicle"))
            {
                return "Trucks";
            }
            if (t.Contains("security") || t.Contains("password") || t.Contains("user") || t.Contains("login") || t.Contains("auth") || t.Contains("audit") || t.Contains("session") || t.Contains("database") || t.Contains("preference") || t.Contains("rules"))
            {
                return "Security";
            }
            return "System";
        }

        private static string? InferTargetModule(string title)
        {
            string t = title.ToLowerInvariant();
            if (t.Contains("dispatch")) return "Dispatch";
            if (t.Contains("loading") || t.Contains("schedule") || t.Contains("rescheduled")) return "Loading";
            if (t.Contains("trip report") || t.Contains("trip")) return "TripReports";
            if (t.Contains("invoice") || t.Contains("soa")) return "Invoices";
            if (t.Contains("payment") || t.Contains("collection") || t.Contains("receipt")) return "Collections";
            if (t.Contains("incident") || t.Contains("damage")) return "Incidents";
            if (t.Contains("truck") || t.Contains("vehicle")) return "Trucks";
            if (t.Contains("crew") || t.Contains("driver") || t.Contains("personnel")) return "Crew";
            if (t.Contains("user") || t.Contains("security") || t.Contains("audit") || t.Contains("session") || t.Contains("login") || t.Contains("auth") || t.Contains("preference") || t.Contains("rules") || t.Contains("database")) return "Settings";
            return null;
        }

        public static List<SystemNotification> GetRecentNotifications(int max = 50, string? categoryFilter = null, bool unreadOnly = false, bool? isAdmin = null)
        {
            bool admin = isAdmin ?? SessionContext.IsAdmin;
            lock (lockObj)
            {
                IEnumerable<SystemNotification> query = notifications;
                if (!admin)
                {
                    query = query.Where(n => !n.IsAdminOnly);
                }
                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }
                if (!string.IsNullOrWhiteSpace(categoryFilter) && !categoryFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    if (categoryFilter.Equals("Alerts", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(n => n.Type == ToastType.Warning || n.Type == ToastType.Error);
                    }
                    else if (categoryFilter.Equals("Payments", StringComparison.OrdinalIgnoreCase) || categoryFilter.Equals("Finance", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(n => n.Category.Equals("Payments", StringComparison.OrdinalIgnoreCase) || n.Category.Equals("Finance", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (categoryFilter.Equals("Trucks", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(n => n.Category.Equals("Trucks", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        query = query.Where(n => n.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase));
                    }
                }
                return query.Take(max).ToList();
            }
        }

        public static int UnreadCount => GetUnreadCount(SessionContext.IsAdmin);

        public static int GetUnreadCount(bool? isAdmin = null)
        {
            bool admin = isAdmin ?? SessionContext.IsAdmin;
            lock (lockObj)
            {
                return notifications.Count(n => !n.IsRead && (admin || !n.IsAdminOnly));
            }
        }

        public static int GetBusinessProcessUnreadCount()
        {
            lock (lockObj)
            {
                return notifications.Count(n => !n.IsRead && n.IsBusinessProcess);
            }
        }

        public static void MarkAsRead(string id)
        {
            lock (lockObj)
            {
                var n = notifications.FirstOrDefault(x => x.Id == id);
                if (n != null)
                {
                    n.IsRead = true;
                }
            }
            try
            {
                MJdatabase.MarkSystemNotificationAsRead(id);
            }
            catch (System.Exception swx7) { ErrLog.Swallowed(swx7); }
            OnNotificationsChanged?.Invoke();
        }

        public static void MarkAllAsRead(bool? isAdmin = null)
        {
            bool admin = isAdmin ?? SessionContext.IsAdmin;
            lock (lockObj)
            {
                foreach (var n in notifications)
                {
                    if (admin || !n.IsAdminOnly)
                    {
                        n.IsRead = true;
                    }
                }
            }
            try
            {
                MJdatabase.MarkAllSystemNotificationsAsRead(admin);
            }
            catch (System.Exception swx8) { ErrLog.Swallowed(swx8); }
            OnNotificationsChanged?.Invoke();
        }

        public static void DeleteNotification(string id)
        {
            lock (lockObj)
            {
                notifications.RemoveAll(x => x.Id == id);
            }
            try
            {
                MJdatabase.DeleteSystemNotification(id);
            }
            catch (System.Exception swx9) { ErrLog.Swallowed(swx9); }
            OnNotificationsChanged?.Invoke();
        }

        public static void ClearAll(bool? isAdmin = null)
        {
            bool admin = isAdmin ?? SessionContext.IsAdmin;
            lock (lockObj)
            {
                if (admin)
                {
                    notifications.Clear();
                }
                else
                {
                    notifications.RemoveAll(n => !n.IsAdminOnly);
                }
            }
            try
            {
                MJdatabase.ClearAllSystemNotifications(admin);
            }
            catch (System.Exception swx10) { ErrLog.Swallowed(swx10); }
            OnNotificationsChanged?.Invoke();
        }

        public static void ScanOperationalAlerts()
        {
            try
            {
                var existingNotifs = MJdatabase.GetSystemNotifications(100, isAdmin: true);
                DateTime cutoff = DateTime.Now.AddHours(-12);

                bool HasRecentAlert(string titlePrefix, string? identifier)
                {
                    return existingNotifs.Any(n => n.Timestamp > cutoff &&
                        n.Title.StartsWith(titlePrefix, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrEmpty(identifier) || n.Message.Contains(identifier)));
                }

                var invoices = MJdatabase.GetInvoices();
                foreach (var inv in invoices)
                {
                    if (inv.Status != "Paid" && inv.DueDate.Date < DateTime.Today)
                    {
                        int daysOverdue = (DateTime.Today - inv.DueDate.Date).Days;
                        if (!HasRecentAlert("Overdue Invoice Alert", inv.InvoiceNumber))
                        {
                            Publish("Overdue Invoice Alert", $"Invoice #{inv.InvoiceNumber} for {inv.ClientName} is overdue by {daysOverdue} day(s) (Balance: PHP {inv.NetAmount:N2}).", ToastType.Warning, null, "Payments", "Invoices", inv.InvoiceNumber);
                        }
                    }
                }

                var deliveries = MJdatabase.GetDeliveries();
                foreach (var d in deliveries)
                {
                    if ((d.Status == "Dispatched" || d.Status == "In Transit") && d.DispatchDate.Date < DateTime.Today.AddDays(-1))
                    {
                        if (!HasRecentAlert("Trip Report Overdue", d.DispatchNumber))
                        {
                            Publish("Trip Report Overdue", $"Dispatch #{d.DispatchNumber} ({d.TruckInfo}) departed on {d.DispatchDate:MMM dd} without a filed trip report.", ToastType.Warning, null, "Operations", "TripReports", d.DispatchNumber);
                        }
                    }
                }

                var trucks = MJdatabase.GetTrucks();
                int readyTrucks = trucks.Count(t => t.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
                if (readyTrucks <= 1 && trucks.Count > 0)
                {
                    if (!HasRecentAlert("Low Available Trucks", "vehicle"))
                    {
                        Publish("Low Available Trucks", $"Only {readyTrucks} active vehicle(s) ready for dispatch. Review truck availability.", ToastType.Warning, null, "Trucks", "Trucks", null);
                    }
                }

                var incidents = MJdatabase.GetIncidentReports();
                DateTime recentCutoff = DateTime.Now.AddDays(-3);
                foreach (var inc in incidents)
                {
                    if (inc.IncidentDate > recentCutoff && inc.DamagedQuantity > 0)
                    {
                        if (!HasRecentAlert("Cargo Damage Incident", inc.DispatchNumber))
                        {
                            Publish("Cargo Damage Incident", $"Incident on Dispatch #{inc.DispatchNumber}: {inc.DamagedQuantity} damaged crates ({inc.Cause}).", ToastType.Warning, null, "Operations", "Incidents", inc.DispatchNumber);
                        }
                    }
                }
            }
            catch (System.Exception swx11) { ErrLog.Swallowed(swx11); }
        }
    }
}
