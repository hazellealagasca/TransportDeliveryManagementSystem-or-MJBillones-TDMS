using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One step of a special trip's flow (for the step circles, the flow bar and the "Next" banner).</summary>
    public sealed class StFlowStep
    {
        public string Key = "";          // trip, transit, complete, report, invoice, soa, receipt
        public string Title = "";
        public string Status = "";
        public bool Done;
        /// <summary>Not done and not needed any more (e.g. the invoice already has its receipt, so it can't go on a statement).</summary>
        public bool Skipped;
        /// <summary>What the step's button does: complete / report / invoice / soa / receipt (null = no button).</summary>
        public string? ActionKey;
        public string? ActionText;
        /// <summary>The record the step made (SI No., SOA no., receipt no.) for "Show".</summary>
        public string? Number;
    }

    /// <summary>
    /// Nov 6, 2026: a special trip's flow. Special trip → In transit → Completed → then only the ticked steps, in order:
    /// Trip report → Service invoice (Billing, typed SI No.) → Statement of account → Collection receipt.
    /// </summary>
    public sealed class SpecialTripFlowState
    {
        public List<StFlowStep> Steps = new();
        public string NextText = "";
        public string? NextActionKey, NextActionText, NextNumber;
        /// <summary>info, warn, success or danger (the banner colour).</summary>
        public string Tone = "info";
        public TripInvoice? Invoice;
        public CollectionReceipt? Receipt;
        public SpecialTripLinks Links = new();

        public static SpecialTripFlowState Compute(SpecialTrip? trip, bool withReport, bool withInvoice, bool withSoa, bool withCollection)
        {
            var f = new SpecialTripFlowState();
            if (trip == null || trip.Id <= 0)
            {
                f.Steps.Add(new StFlowStep { Key = "trip", Title = "Special trip", Status = "Fill in the form and save" });
                f.Steps.Add(new StFlowStep { Key = "complete", Title = "Completed", Status = "When the truck is back" });
                if (withReport) f.Steps.Add(new StFlowStep { Key = "report", Title = "Trip report", Status = "After it is completed" });
                if (withInvoice) f.Steps.Add(new StFlowStep { Key = "invoice", Title = "Service invoice", Status = "Billing, typed SI No." });
                if (withSoa) f.Steps.Add(new StFlowStep { Key = "soa", Title = "Statement (SOA)", Status = "Groups the client's invoices" });
                if (withCollection) f.Steps.Add(new StFlowStep { Key = "receipt", Title = "Collection receipt", Status = "One receipt, full amount" });
                f.NextText = "Fill in the trip and save it. Tick the steps this trip needs below.";
                return f;
            }
            bool cancelled = RowMenu.Is(trip.Status, "Cancelled"), completed = RowMenu.Is(trip.Status, "Completed");
            bool inTransit = RowMenu.Is(trip.Status, "In-Transit") || RowMenu.Is(trip.Status, "In Transit");
            try { f.Links = MJdatabase.GetSpecialTripLinks().TryGetValue(trip.Id, out var l) ? l : new SpecialTripLinks(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            try { f.Invoice = MJdatabase.TripInvoiceFor(MJdatabase.GetTripInvoices(), 0, trip.Id); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            try { if (f.Invoice != null) f.Receipt = MJdatabase.GetCollectionReceipts().FirstOrDefault(r => r.InvoiceId == f.Invoice.Id); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            var ti = f.Invoice;
            bool hasReport = f.Links.HasReport;
            bool reportOk = !withReport || hasReport;

            f.Steps.Add(new StFlowStep { Key = "trip", Title = "Scheduled", Status = trip.DispatchDate.ToString("MMM d"), Done = true });
            f.Steps.Add(new StFlowStep { Key = "transit", Title = "In transit", Status = inTransit ? "On the road" : completed ? "Done" : "", Done = inTransit || completed });
            f.Steps.Add(new StFlowStep { Key = "complete", Title = "Completed", Status = completed ? (trip.CompletedAt?.ToString("MMM d") ?? "Done") : cancelled ? "Cancelled" : "When the truck is back", Done = completed,
                ActionKey = !completed && !cancelled ? "complete" : null, ActionText = !completed && !cancelled ? "Mark trip completed" : null });
            if (withReport)
                f.Steps.Add(new StFlowStep { Key = "report", Title = "Trip report", Done = hasReport,
                    Status = hasReport ? f.Links.TripReportNumber : completed ? "Needed now" : "After it is completed",
                    ActionKey = completed || hasReport ? "report" : null, ActionText = hasReport ? "Open" : completed ? "File trip report" : null });
            if (withInvoice)
            {
                bool can = completed && reportOk && ti == null;
                f.Steps.Add(new StFlowStep { Key = "invoice", Title = "Service invoice", Done = ti != null,
                    Status = ti != null ? $"SI {ti.Number} · {ti.Status}" : can ? "Needed now (Billing)" : withReport ? "After the trip report" : "After it is completed",
                    ActionKey = ti != null || can ? "invoice" : null, ActionText = ti != null ? "Show" : can ? "Make service invoice" : null, Number = ti?.Number });
            }
            if (withSoa)
            {
                bool on = ti != null && ti.SoaId > 0, can = ti != null && ti.Open && !on, paidAlone = ti != null && !ti.Open && !on;
                f.Steps.Add(new StFlowStep { Key = "soa", Title = "Statement (SOA)", Done = on, Skipped = paidAlone,
                    Status = on ? $"On {ti!.SoaNumber}" : can ? "Needed now" : paidAlone ? "Not on a statement" : "After the service invoice",
                    ActionKey = on || can ? "soa" : null, ActionText = on ? "Show" : can ? "Add to a statement" : null, Number = on ? ti!.SoaNumber : null });
            }
            if (withCollection)
            {
                var r = f.Receipt;
                bool can = ti != null && ti.Open && (!withSoa || ti.SoaId > 0);
                f.Steps.Add(new StFlowStep { Key = "receipt", Title = "Collection receipt", Done = r != null,
                    Status = r != null ? $"Receipt {r.ReceiptNumber}" : can ? (ti!.SoaId > 0 ? $"Needed: one receipt for {ti.SoaNumber}" : $"Needed: PHP {ti.Net:N2}") : withSoa ? "After the statement" : "After the service invoice",
                    ActionKey = r != null || can ? "receipt" : null, ActionText = r != null ? "Show" : can ? "Record receipt" : null, Number = r?.ReceiptNumber });
            }

            // The "Next" banner: the first step that is not done yet.
            if (cancelled)
            {
                f.Tone = "danger";
                f.NextText = "This special trip was cancelled.";
                return f;
            }
            var next = f.Steps.FirstOrDefault(s => !s.Done && !s.Skipped);
            if (next == null)
            {
                f.Tone = "success";
                f.NextText = "All done: every ticked step is finished.";
                return f;
            }
            f.NextActionKey = next.ActionKey;
            f.NextActionText = next.ActionText;
            f.NextNumber = next.Number;
            int days = (DateTime.Today - trip.DispatchDate.Date).Days;
            switch (next.Key)
            {
                case "transit":
                case "complete":
                    f.Tone = days > 0 ? "warn" : "info";
                    f.NextText = days < 0 ? $"Leaves on {trip.DispatchDate:ddd, MMM d} at {trip.DepartureTime}. Mark it completed once the truck is back."
                        : days == 0 ? $"Leaves today at {trip.DepartureTime}. Mark it completed once the truck is back."
                        : $"Dated {trip.DispatchDate:MMM d} ({days} day{(days == 1 ? "" : "s")} ago). Mark it completed once the truck is back.";
                    f.NextActionKey = "complete"; f.NextActionText = "Mark trip completed";
                    break;
                case "report": f.NextText = "Completed. File the trip report (stops, sign-offs, crew pay)."; break;
                case "invoice": f.NextText = $"Ready to bill: make its service invoice in Billing and type the SI No. (trip charge PHP {trip.TripCharge:N2})."; break;
                case "soa": f.NextText = $"SI {ti?.Number} is issued. Add it to {trip.Company}'s statement of account."; break;
                case "receipt": f.Tone = ti != null && ti.DueDate.Date < DateTime.Today ? "warn" : "info";
                    f.NextText = ti != null && ti.SoaId > 0 ? $"Collect {ti.SoaNumber} with one receipt for the full amount." : $"Collect SI {ti?.Number}: PHP {ti?.Net:N2}" + (ti != null && ti.DueDate.Date < DateTime.Today ? $", was due {ti.DueDate:MMM d}." : ".");
                    break;
                default: f.NextText = next.Status; break;
            }
            return f;
        }
    }
}
