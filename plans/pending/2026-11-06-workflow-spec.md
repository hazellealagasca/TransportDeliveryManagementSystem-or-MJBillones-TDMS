```text
Please implement the following complete, system-wide workflow updates across the Transport Delivery Management System for M.J. Billones Transport Service (.NET Windows Forms, C#, SQLite). Ensure all changes preserve existing database records, maintain data integrity, build cleanly with zero compiler warnings/errors, and strictly adhere to all domain boundaries.

================================================================================
CORE OPERATIONAL BOUNDARIES & BUSINESS RULES
================================================================================
1. Operations & Internal Documentation Only: This application is not a payroll or salary disbursement system. Any crew wage or meal allowance rates tracked in Crew Management exist solely as internal operational documentation and dispatch audit tracking for M.J. Billones. They must never be exposed to or calculated within client-facing documents.
2. Zero Partial Payments: Financial settlements are strictly 100% full payments. Remove all partial balance calculations, partial statuses, and partial payment allowances across the entire system. An invoice or Statement of Account is strictly Unpaid until settled in full (Paid).
3. Strict Separation of Normal and Special Trips: Normal delivery trips and Special Trips cannot be combined into the same Statement of Account (SOA). They require separate SOA generation, distinct printing templates, and separate collection receipts.
4. Explicit Entity Statuses: Maintain explicit status tracking on Trip Reports ('Draft', 'Completed', 'Invoiced') and Collection Receipts ('Completed'). Invoiced and paid records must be cleanly moved to their respective History archives.
5. High-Density Text Overviews: Replace large, bulky UI cards and panels with detailed, high-density text summary blocks for trip and crate overviews to preserve vertical screen space.

================================================================================
MODULE 1: DISPATCH ORDER CREATION (DispatchManagementView)
================================================================================
1. Hide Top Cargo/Temperature Controls: Remove or hide the top cargo/temperature category dropdown (cbCargoType) and any panels displaying "Cold / Chilled" or "Dry and Chilled Mix" above the crate section.
2. Combi as Descriptive Text Only: Treat "Combi" strictly as an informational text entry (e.g., in route notes or trip remarks) indicating a combined dry/chilled trip, without altering input schemas.
3. Strict Crate Inputs: Only display two operational crate numeric inputs:
   - G-Crates (numGCrate)
   - E-Crates (numECrate)
   - Total Crates (txtTotalCrates = G-Crates + E-Crates, read-only auto-tally).
4. Route Store Breakdown: The store breakdown table will only track Store Code, Store Name, Delivery Receipt (DR) Number, G-Crate count, and E-Crate count.
5. Crate Summary Overview: Replace large recommendation card panels with a compact, high-density text summary line showing total crates and truck capacity utilization.

================================================================================
MODULE 2: TRIP REPORT EXECUTION & HISTORY (TripReportView)
================================================================================
1. Action Button Transformation: Rename the primary action button from "Save Changes" / "Complete Trip" to "Issue Invoice".
2. Progress Bar Stepper: Add a visible 4-step horizontal progress bar across the top of the form:
   - Step 1: Store Timestamps and Signatures
   - Step 2: Gate Times (Time In / Time Out)
   - Step 3: Verification and Signatory Check
   - Step 4: Issue Invoice
3. Display Simplification: Remove all columns, metrics, and cards displaying "Delivered Crates" and "Return Rate" from the grid and form. Focus strictly on proof of delivery, store arrival/departure times, gate hours, and delivery receipts.
4. Detailed Text-Only Trip & Crate Summary: Replace bulky stacked banner panels and crate panels with a single clean, high-density text overview block showing:
   - Dispatch Number, Reference Number, Date, Departure Time
   - Client Company, Route, Destination, Store Count
   - Truck Plate, Tonnage, Assigned Driver, Assigned Helper
   - Dispatched Crate Counts: G-Crates, E-Crates, and Total Crates
5. Multi-Selection & Auto-Population:
   - Enable multi-selection on gridTripReports with a toolbar button: "Issue Invoice (Selected)".
   - Clicking this validates the selected trip reports and opens the Service Invoice view (BillingView) pre-populated with dispatch number, client, destination, and calculated rate.
   - Provide a clear "Back to Trip Reports" navigation button in the invoice view to return safely without creating unwanted records.
6. Status Tracking & History Archiving:
   - Add an explicit Status field to TripReports: 'Draft', 'Completed', 'Invoiced'.
   - The active Trip Report grid will only display active trips (Status != 'Invoiced').
   - Once an invoice is issued for a trip report, set its status to 'Invoiced' and move it out of the active operational queue into Trip Report History.

================================================================================
MODULE 3: SERVICE INVOICE / BILLING (BillingView)
================================================================================
1. Remove Gross Amount Zero Validation: Remove the blocking validation "if (total <= 0)" that triggers error dialogs. Allow zero-gross drafts or pending-rate invoices without blocking the user.
2. Navigation Control: Provide a dedicated "Back to Trip Reports" button in the form header to allow seamless navigation during batch operations.
3. Remove Partial Payment States: Remove all UI states, table styling, and logic referencing Status = 'Partial'. Invoices exist strictly as 'Unpaid' until fully settled ('Paid').

================================================================================
MODULE 4: STATEMENT OF ACCOUNT (StatementOfAccountView)
================================================================================
1. Multi-Invoice Selection by Client:
   - Replace raw delivery date range population with an invoice selection interface.
   - When a Client Company is selected, load all unassigned, 'Unpaid' Service Invoices for that client.
   - Allow checking multiple invoices (e.g., all deliveries for the week or month) to consolidate into a single SOA.
2. Strict Segregation (Normal vs. Special Trips):
   - Introduce an SOA type selector: "Normal Operations SOA" vs. "Special Trips SOA".
   - Normal SOA only allows selecting standard route invoices and uses the official multi-drop store DR format.
   - Special Trips SOA only allows selecting special trip invoices and uses the dedicated Special Trip billing layout.
   - Special trips require separate printing and a separate collection receipt.
3. Complete Exclusion of Internal Crew Labor / Wage Fields:
   - Remove all input controls, table columns, or calculations related to DriverWage, HelperWage, MealAllowance, BasePay, DieselCost, and Excess from the SOA form, models, and printouts.
   - The "Driver / Helper" column must strictly display employee names as delivery crew proof of identification (e.g., "Juan Dela Cruz / Pedro Santos"), with zero monetary figures.
   - Commercial line items are strictly: Delivery Receipt Number, Destination Store, Vehicle Plate Number, Tonnage, Drops, Freight Tariff Amount, and Fuel Surcharge.
   - Summary calculations are strictly: Total Sales (VAT inclusive), 12% VAT extraction, Net of VAT, 2% Expanded Withholding Tax (EWT), and Total Amount Due.
4. Workflow Transition: Directly upon saving an SOA, display an action button: "Proceed to Collection Receipt", which transitions to Collections carrying the SOA reference and full net balance.

================================================================================
MODULE 5: COLLECTIONS AND RECEIPTS (CollectionView)
================================================================================
1. Button Transformation: Rename "Update Receipt" to "Payment Completed".
2. 100% Full Payment Enforcement:
   - Remove all remaining balance calculations, partial payment allowances, and overpayment adjustment dialogs.
   - Payment amount is strictly locked to 100% of the net balance due (Amount Paid = Full Net Amount Due).
3. Status Tracking: Ensure the CollectionReceipts table maintains an explicit Status field with value 'Completed'.
4. Atomic Settlement: When "Payment Completed" is clicked:
   - Save the Collection Receipt with Status = 'Completed'.
   - Mark the parent SOA as 'Paid'.
   - Mark all linked child Service Invoices as 'Paid'.
   - Move the settled SOA and Invoices into Finance History.
5. Automated Print Prompt:
   - After recording payment, prompt: "Payment recorded in full. Do you want to print the Official Collection Receipt now?"
   - If confirmed, immediately generate and display the print layout via PrintHelper.PrintOfficialCollectionReceipt() (using the appropriate slip format for Normal vs. Special trips).

================================================================================
MODULE 6: INTERNAL PERSONNEL DOCUMENTATION QUARANTINE
================================================================================
1. Quarantine all crew wage and food allowance tracking strictly within internal crew management views (CrewManagementView / CrewTripHistoryView).
2. Ensure no data pipeline connects these internal operational tracking metrics to client billing, SOAs, invoices, or customer receipts.

================================================================================
VERIFICATION REQUIREMENTS
================================================================================
1. Ensure SQLite schema upgrades safely handle new status and reference columns without data loss.
2. Verify that navigating Back from Service Invoice does not leave orphan records.
3. Confirm that normal and special trip invoices cannot be selected together in the same SOA.
4. Ensure the solution builds cleanly with zero compile errors and zero compiler warnings.
```