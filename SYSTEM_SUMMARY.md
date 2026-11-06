# System Summary — M.J. Billones Transport Delivery Management System (TDMS)

A Windows desktop app for **M.J. Billones Transport Service** that runs the whole delivery cycle: ordering, dispatch, trip reporting, billing and collection.

## Tech
- C# / .NET 9 WinForms (`net9.0-windows`)
- Embedded SQLite (`mj_billones.db`, WAL mode) via `Microsoft.Data.Sqlite`, with automatic schema migrations
- Burgundy / mild-red themed UI with dark mode support
- `Verify/` project holds automated data and workflow checks

## Users and roles
- **Admin**: full access, including user accounts, categories, backup/restore, security audit and settings.
- **Office Staff**: daily dispatch, trip, billing and collection work.

## Workflow
1. **Order / Loading Schedule**: client, store, route, cargo type (Cold or Dry), G-Crate and E-Crate counts.
2. **Dispatch**: assign truck and crew. The system checks truck capacity and temperature match.
3. **Trip Report**: records delivered and returned crates, times (analog clock picker), incidents and crew pay.
4. **Billing**: invoices and Statements of Account (SOA), with incident deductions.
5. **Collection**: payments (cash, check, transfer) and official collection receipts. Settled records go to Finance History.

## Main modules (`Views/`)
- **Operations**: Dashboard, Dispatch, Trip Report, Incident Report, Special Trips, Order History
- **Finance**: Billing, Statement of Account, Collection, Finance Hub, Settled records, Rate Matrix
- **Resources and master data**: Trucks, Crew (profiles, trip history, pay), Clients, Stores and Routes, Categories, Archive
- **Admin**: Users, Security and Audit, Backup/Restore, Appearance and Features settings, Reports with CSV export

## Code layout
- `Database/`: `MJdatabase.*.cs` partial classes (workflow, payments, SOA, archive, integrity, backups)
- `Views/`: screens
- `Forms/`: dialogs
- `Functions/`: shared helpers (printing, security, permissions, theming, notifications, validation)
- `Models/`: data models
- `MainForm.*.cs`: app shell (sidebar, header, notifications, focus mode)

## Branch
`workflow-overhaul-2026-11-06` is in progress. It reworks the awaiting-report flow, the payment guide and crew pay.
