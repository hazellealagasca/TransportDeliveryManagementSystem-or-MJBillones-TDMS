# TDMS — What the System Is All About

## Purpose
The Transport Delivery Management System (TDMS) is a Windows desktop app for **M.J. Billones Transport Service**, a company that hauls cold and dry goods to client stores. It replaces paper and spreadsheet tracking with one program that follows a delivery from the client's order to the money collected.

## The problem it solves
- Orders, trucks, crew and payments were tracked in separate places.
- Trucks could be double-booked or sent over capacity, or sent without refrigeration for cold cargo.
- Billing and collection depended on trip details that were easy to lose.

## Who uses it
- **Admin**: everything, including user accounts, categories, backup/restore, security audit and system settings.
- **Office Staff**: daily work only (orders, dispatch, trip reports, billing, collection).

## How a delivery flows through the system
1. **Order (Loading Schedule)**: client, store, route, cargo type (Cold or Dry), G-crate and E-crate counts, date and time.
2. **Dispatch**: assign a truck and crew. The system checks truck capacity and cargo-temperature match, and recommends a truck.
3. **Trip Report**: after the trip, record delivered and returned crates, times, incidents and crew pay. Only completed dispatches can be reported and billed.
4. **Billing**: invoices per trip and Statements of Account (SOA), with deductions for incidents.
5. **Collection**: record payments (cash, check, transfer) and issue official collection receipts. Fully paid records move to Finance History.

## Main areas of the app
| Area | What it covers |
|---|---|
| Operations | Dashboard, Dispatch, Trip Reports, Incident Reports, Special Trips, Order History |
| Finance | Billing, Statement of Account, Collection, Settled records, Rate Matrix |
| Resources | Trucks, Crew (profiles, trip history, pay) |
| Master data | Clients, Stores and Routes, Categories, Archive |
| Admin | Users, Security Audit, Backup/Restore, Appearance and Features settings, Reports with CSV export |

## Technology
- C# on .NET 9, Windows Forms
- Embedded SQLite database (`mj_billones.db`) with automatic migrations; no server needed
- Automatic backups and rolling snapshots
- Light and dark themes, printable forms and receipts, a `Verify` project with automated data checks

## Current state
- Branch `workflow-overhaul-2026-11-06` is reworking the awaiting-report flow, the payment guide and crew pay.
- A crash audit is in [CRASH_AUDIT_REPORT.md](CRASH_AUDIT_REPORT.md). Its fixes are proposed, not applied.
- A shorter summary is in [SYSTEM_SUMMARY.md](SYSTEM_SUMMARY.md).
