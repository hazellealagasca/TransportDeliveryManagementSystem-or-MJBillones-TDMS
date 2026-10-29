using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Entry points for every printout. Each one gathers the record's data into a form (see <see cref="PrintForms"/>)
    /// and opens the print window, where the person picks what to include before printing or saving as PDF.
    /// </summary>
    public static class PrintHelper
    {
        public static void PrintGridReport(string moduleTitle, DataGridView grid, string additionalNotes = "")
            => PrintStudio.Show(() => PrintForms.GridReport(moduleTitle, grid, additionalNotes));
        public static void PrintSelectedOrAllGridReport(string moduleTitle, DataGridView grid, string additionalNotes = "")
            => PrintStudio.Show(() => PrintForms.GridReport(moduleTitle, grid, additionalNotes));
        public static void PrintCrewDossierAndHistory(Crew crew, List<Delivery> history)
            => PrintStudio.Show(() => PrintForms.CrewDossier(crew, history));
        public static void PrintOfficialCollectionReceipt(CollectionReceipt cr)
            => PrintStudio.Show(() => PrintForms.CollectionReceipt(cr));
        public static void PrintStatementOfAccount(StatementOfAccount soa, List<Invoice> invoices)
            => PrintStudio.Show(() => PrintForms.StatementOfInvoices(soa, invoices));
        public static void PrintOfficialStatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem>? items = null)
            => PrintStudio.Show(() => PrintForms.StatementOfAccount(soa, items ?? MJdatabase.GetStatementOfAccountItems(soa.Id)));
        public static void PrintCrewPaySlip(Crew crew, List<Delivery> trips, DateTime startDate, DateTime endDate)
            => PrintStudio.Show(() => PrintForms.PaySlip(crew, trips, startDate, endDate));
        public static void PrintEndToEndDeliveryDossier(int deliveryId, string dispatchNumber = "")
        {
            var delivery = FindDelivery(deliveryId, dispatchNumber);
            if (delivery == null)
            {
                MessageBox.Show("Delivery record could not be found for the specified dispatch.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PrintStudio.Show(() =>
            {
                var stops = MJdatabase.GetDispatchStops(delivery.Id);
                if (stops.Count == 0) stops = MJdatabase.GetOrBuildTripStopsForDelivery(delivery);
                var incident = MJdatabase.GetIncidentReports().FirstOrDefault(i => i.DeliveryId == delivery.Id || i.DispatchNumber == delivery.DispatchNumber);
                var trip = MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == delivery.Id || t.DispatchNumber == delivery.DispatchNumber);
                return PrintForms.DeliveryDossier(delivery, incident, trip, stops);
            });
        }
        public static void PrintEndToEndDeliveryDossier(Delivery delivery, IncidentReport? incident, TripReport? trip, List<DispatchStop>? stops)
            => PrintStudio.Show(() => PrintForms.DeliveryDossier(delivery, incident, trip, stops));
        public static void PrintOfficialLoadingScheduleFromDeliveries(List<Delivery> deliveries)
        {
            if (deliveries == null || deliveries.Count == 0)
            {
                QuietBox.Show("No dispatch / loading orders available to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintStudio.Show(() => PrintForms.LoadingSheetFromDeliveries(deliveries));
        }
        public static void PrintOfficialLoadingScheduleSheet(List<LoadingSchedule> schedules)
        {
            if (schedules == null || schedules.Count == 0)
            {
                QuietBox.Show("No loading schedules available to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintStudio.Show(() => PrintForms.LoadingSheetFromSchedules(schedules));
        }
        public static void PrintOfficialTripReport(int deliveryId, string dispatchNumber = "")
        {
            var delivery = FindDelivery(deliveryId, dispatchNumber);
            if (delivery == null)
            {
                MessageBox.Show("Dispatch record could not be found.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PrintStudio.Show(() =>
            {
                var trip = MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == delivery.Id || t.DispatchNumber == delivery.DispatchNumber);
                return PrintForms.TripReport(delivery, trip, MJdatabase.GetOrBuildTripStopsForDelivery(delivery));
            });
        }
        public static void PrintOfficialTripReport(Delivery delivery, TripReport? trip, List<DispatchStop>? stops)
            => PrintStudio.Show(() => PrintForms.TripReport(delivery, trip, stops ?? MJdatabase.GetOrBuildTripStopsForDelivery(delivery)));
        private static Delivery? FindDelivery(int id, string number)
        {
            try
            {
                return MJdatabase.GetDeliveries().FirstOrDefault(d => d.Id == id || (!string.IsNullOrEmpty(number) && d.DispatchNumber == number));
            }
            catch { return null; }
        }
    }
}
