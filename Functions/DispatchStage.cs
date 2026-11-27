using System;
using System.Collections.Generic;
using System.Drawing;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Where a dispatch order is in its whole life (Nov 28). Open = it has not finished its trip yet: Scheduled, Current, Delayed (the label is the dispatch status).</summary>
    public enum DispatchStageKind { Open, ForTripReport, ForSoa, ForInvoice, ForCollection, Paid, Cancelled }

    /// <summary>The stage of one dispatch: what the Status column shows and what clicking it does.</summary>
    public sealed class DispatchStageInfo
    {
        public DispatchStageKind Kind;
        public string Label = "";
        /// <summary>The live statement the trip is on (0 = none).</summary>
        public int SoaId;
        /// <summary>The live invoice that bills the trip (0 = none).</summary>
        public int InvoiceId;
        /// <summary>The four "For ..." stages can be clicked to do the next step.</summary>
        public bool Clickable => Kind == DispatchStageKind.ForTripReport || Kind == DispatchStageKind.ForSoa || Kind == DispatchStageKind.ForInvoice || Kind == DispatchStageKind.ForCollection;
        /// <summary>Paid or cancelled: the order leaves the active list.</summary>
        public bool IsFinished => Kind == DispatchStageKind.Paid || Kind == DispatchStageKind.Cancelled;
        /// <summary>"Click to ..." for the tooltip and the first item of the right-click menu (empty when not clickable).</summary>
        public string Action => Kind switch
        {
            DispatchStageKind.ForTripReport => "Create the trip report",
            DispatchStageKind.ForSoa => "Make the Statement of Account",
            DispatchStageKind.ForInvoice => "Issue the Service Invoice",
            DispatchStageKind.ForCollection => "Record the Collection Receipt",
            _ => ""
        };
    }

    /// <summary>
    /// Scheduled → Current → For trip report → For Statement of Account → For Service Invoice → For Collection → Completed (Paid).
    /// One pure rule (no screen, no database) so a check can test every stage. The stored Delivery.Status never changes.
    /// </summary>
    public static class DispatchStage
    {
        public const string LabelTripReport = "For trip report";
        public const string LabelSoa = "For Statement of Account";
        public const string LabelInvoice = "For Service Invoice";
        public const string LabelCollection = "For Collection";
        public const string LabelPaid = "Completed";

        /// <summary>
        /// <paramref name="chain"/>: the trip's trip report / live invoice / receipt (null = none). <paramref name="onLiveSoa"/> and
        /// <paramref name="soaId"/>: the trip is a line of a live (not cancelled) statement. <paramref name="soaHasInvoice"/>: that statement has a live invoice.
        /// </summary>
        public static DispatchStageInfo For(Delivery d, OrderChain? chain, bool onLiveSoa, bool soaHasInvoice, int soaId, DateTime? today = null)
        {
            var info = new DispatchStageInfo { SoaId = onLiveSoa ? soaId : 0, InvoiceId = chain?.Invoice?.InvoiceId ?? 0 };
            if (info.SoaId == 0 && chain?.Invoice != null && chain.Invoice.SoaId > 0) info.SoaId = chain.Invoice.SoaId;
            if (DispatchGate.IsCancelled(d.Status)) { info.Kind = DispatchStageKind.Cancelled; info.Label = StatusText.Display(d.Status); return info; }
            if (chain != null && chain.IsPaid) { info.Kind = DispatchStageKind.Paid; info.Label = LabelPaid; return info; }
            // A Draft report does not count: the trip is still waiting for its (finished) trip report.
            bool reportDone = chain != null && chain.TripReportNo.Length > 0 && !string.Equals(chain.TripReportStatus, TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase);
            // Nov 28: on the road (Current / Delayed) only on its dispatch day; from the next day the trip is over and waits for its trip report.
            bool onRoad = string.Equals((d.Status ?? "").Trim(), "In Transit", StringComparison.OrdinalIgnoreCase) || string.Equals((d.Status ?? "").Trim(), "Delayed", StringComparison.OrdinalIgnoreCase);
            if (onRoad && !reportDone && d.DispatchDate.Date < (today ?? DateTime.Today).Date) { info.Kind = DispatchStageKind.ForTripReport; info.Label = LabelTripReport; return info; }
            // Not back from the trip yet: Scheduled / Current / Delayed keep the words the app already uses.
            if (!DispatchGate.IsCompleted(d.Status)) { info.Kind = DispatchStageKind.Open; info.Label = StatusText.Display(d.Status); return info; }
            if (!reportDone) { info.Kind = DispatchStageKind.ForTripReport; info.Label = LabelTripReport; return info; }
            bool invoiced = chain!.Invoice != null || (onLiveSoa && soaHasInvoice);
            if (invoiced) { info.Kind = DispatchStageKind.ForCollection; info.Label = LabelCollection; return info; }
            if (onLiveSoa) { info.Kind = DispatchStageKind.ForInvoice; info.Label = LabelInvoice; return info; }
            info.Kind = DispatchStageKind.ForSoa; info.Label = LabelSoa;
            return info;
        }

        /// <summary>One stage map for the whole list (three queries, not one per row): dispatch id → its stage.</summary>
        public static Dictionary<int, DispatchStageInfo> BuildMap(IEnumerable<Delivery> deliveries)
            => BuildMap(deliveries, MJdatabase.GetOrderChainsByDelivery());

        /// <summary>Nov 28: the same map from chains the caller already loaded (Order History), so nothing is queried twice.</summary>
        public static Dictionary<int, DispatchStageInfo> BuildMap(IEnumerable<Delivery> deliveries, IDictionary<int, OrderChain> chains)
        {
            var soas = MJdatabase.GetLiveSoaIdsByDelivery();
            var map = new Dictionary<int, DispatchStageInfo>();
            foreach (var d in deliveries)
            {
                chains.TryGetValue(d.Id, out var chain);
                bool on = soas.TryGetValue(d.Id, out int soaId);
                map[d.Id] = For(d, chain, on, on && chain?.Invoice != null, on ? soaId : 0);
            }
            return map;
        }

        // Nov 28: raw colours on purpose. Pal.C() re-skins blues to the accent colour and turns pale blues into greys, which made
        // For Statement of Account / For Service Invoice grey or rose; the pills keep their own hue, with a dark-mode pair.
        private static (Color Back, Color Fore) Tone(int[] light, int[] dark)
        {
            var c = ThemeManager.IsDark ? dark : light;
            return (Color.FromArgb(c[0], c[1], c[2]), Color.FromArgb(c[3], c[4], c[5]));
        }

        /// <summary>Pill colours (back, text) of a stage, shared by the Dispatch list and Order History. Open has none here: use <see cref="OpenPillColors"/>.</summary>
        public static (Color Back, Color Fore)? PillColors(DispatchStageKind k) => k switch
        {
            DispatchStageKind.ForTripReport => Tone(new[] { 254, 215, 170, 154, 52, 18 }, new[] { 90, 50, 20, 253, 186, 116 }),       // amber / orange
            DispatchStageKind.ForSoa => Tone(new[] { 191, 219, 254, 29, 78, 216 }, new[] { 30, 58, 110, 147, 197, 253 }),            // blue
            DispatchStageKind.ForInvoice => Tone(new[] { 199, 210, 254, 55, 48, 163 }, new[] { 49, 46, 129, 165, 180, 252 }),        // indigo
            DispatchStageKind.ForCollection => Tone(new[] { 233, 213, 255, 126, 34, 206 }, new[] { 76, 29, 149, 216, 180, 254 }),    // purple
            DispatchStageKind.Paid => Tone(new[] { 220, 252, 231, 22, 101, 52 }, new[] { 20, 83, 45, 134, 239, 172 }),               // green
            DispatchStageKind.Cancelled => Tone(new[] { 254, 226, 226, 153, 27, 27 }, new[] { 127, 29, 29, 252, 165, 165 }),         // red
            _ => null
        };

        /// <summary>Colours of an Open stage by its stored status: Current teal, Delayed yellow, Scheduled / Pending slate.</summary>
        public static (Color Back, Color Fore) OpenPillColors(string? status)
        {
            string s = (status ?? "").Trim();
            if (string.Equals(s, "In Transit", StringComparison.OrdinalIgnoreCase)) return Tone(new[] { 204, 251, 241, 15, 118, 110 }, new[] { 19, 78, 74, 94, 234, 212 });
            if (string.Equals(s, "Delayed", StringComparison.OrdinalIgnoreCase)) return Tone(new[] { 254, 243, 199, 180, 83, 9 }, new[] { 92, 60, 10, 252, 211, 77 });
            return Tone(new[] { 226, 232, 240, 51, 65, 85 }, new[] { 51, 65, 85, 203, 213, 225 });
        }

        /// <summary>The one place both screens ask for a pill's colours.</summary>
        public static (Color Back, Color Fore) ColorsFor(DispatchStageInfo stage, string? status)
            => stage.Kind == DispatchStageKind.Open ? OpenPillColors(status) : PillColors(stage.Kind) ?? OpenPillColors(status);

        /// <summary>Colours for a stage word shown in any grid (the theme's status-pill painter asks this first), so every screen agrees with Order History.</summary>
        public static bool TryLabelColors(string? label, out (Color Back, Color Fore) colors)
        {
            string s = (label ?? "").Trim();
            DispatchStageKind? kind = null;
            if (string.Equals(s, LabelTripReport, StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.ForTripReport;
            else if (string.Equals(s, LabelSoa, StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.ForSoa;
            else if (string.Equals(s, LabelInvoice, StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.ForInvoice;
            else if (string.Equals(s, LabelCollection, StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.ForCollection;
            else if (string.Equals(s, LabelPaid, StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.Paid;
            else if (string.Equals(s, "Cancelled", StringComparison.OrdinalIgnoreCase)) kind = DispatchStageKind.Cancelled;
            if (kind != null) { colors = PillColors(kind.Value)!.Value; return true; }
            if (string.Equals(s, "Current", StringComparison.OrdinalIgnoreCase)) { colors = OpenPillColors("In Transit"); return true; }
            if (string.Equals(s, "Delayed", StringComparison.OrdinalIgnoreCase)) { colors = OpenPillColors("Delayed"); return true; }
            if (string.Equals(s, "Scheduled", StringComparison.OrdinalIgnoreCase)) { colors = OpenPillColors("Scheduled"); return true; }
            colors = default; return false;
        }

        /// <summary>The longest stage label, so a list can size its Status column to fit it.</summary>
        public static readonly string[] AllLabels = { LabelTripReport, LabelSoa, LabelInvoice, LabelCollection, LabelPaid, "Scheduled", "Current", "Delayed", "Cancelled" };
    }
}
