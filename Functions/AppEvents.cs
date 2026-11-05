using System;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>What kind of data changed (rule 16e).</summary>
    public enum AppDataKind { Stores, Routes, Dispatch, SpecialTrips, Billing }

    /// <summary>
    /// Rule 16e: one app-wide "data changed" signal. Saves raise it; open screens listen and reload their lookups and
    /// summaries, so a store added in Master Data shows at once in an open Dispatch form. Listeners must unsubscribe when disposed.
    /// </summary>
    public static class AppEvents
    {
        public static event Action<AppDataKind>? DataChanged;
        /// <summary>Goes up on every change; caches compare it to know they are stale.</summary>
        public static int Version { get; private set; }
        public static void Raise(AppDataKind kind)
        {
            Version++;
            var h = DataChanged;
            if (h == null) return;
            foreach (Action<AppDataKind> one in h.GetInvocationList())
                try { one(kind); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
