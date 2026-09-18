# Transport Delivery Management System for M.J. Billones Transport Service

An enterprise-grade desktop transport, logistics, and fleet delivery management system custom-engineered for **M.J. Billones Transport Service**. Designed with a modern Mild Red and Burgundy aesthetic, responsive high-contrast data grids, embedded SQLite database engine with WAL concurrency, interactive analog clock time pickers, dual capacity/temperature recommendation algorithms, and tax-free official collection receipts.

---

## 🌟 System Highlights & Key Capabilities

- **Role-Based Access Control**: Strict separation between **Admin** (complete system access, user account management, categories, and database backup/restore) and **Office Staff** (daily hauling, dispatching, and billing workflows).
- **Standardized Crate Accounting**: Specialized hauling breakdown tracking **G-Crates** and **E-Crates** across loading orders, vehicle limits, and trip completion returns.
- **Cargo Temperature & Storage Intelligence**: Fleet capability and order matching for **Cold / Chilled** (active chiller refrigeration: 0°C to 4°C) vs. **Dry Goods** (ambient hauling), with automated temperature mismatch warnings.
- **Interactive Analog Clock Time Pickers (`🕒`)**: Intuitive draggable clock face dialogs for setting departure and store arrival timestamps with minute-level precision.
- **Clean Official Collection Receipts**: Generates print-ready Official Collection Receipts itemizing Gross Freight Amount, Incident Loss Deductions, Net Amount Due, Amount Paid, and Remaining Balance.
- **Responsive 100% Full-Width Data Grids**: Proportional column distribution (`AutoSizeColumnsMode = Fill`) with smooth horizontal/vertical scrolling across all 12 operational views.
- **Reliable Local Storage**: Embedded SQLite database (`mj_billones.db`) with Write-Ahead Logging (`WAL`), transactional lifecycle transitions, and automatic schema migrations.

---

## 📂 System Architecture & Modules

```
Transport Delivery Management System
├── Functions/
│   ├── Printer.cs        # HTML/CSS print generator for receipts, invoices & audit reports
│   ├── Security.cs       # SHA-256 hashing, regex validators (phones, plates, names, licenses)
│   └── UITheme.cs         # Burgundy/Mild Red color palette, fonts, card styles & grid styling
├── Database/
│   └── MJdatabase.cs     # SQLite engine, transactional operations, lifecycle state machines
├── Models/
│   └── Models.cs         # Data models (User, Crew, Truck, Schedule, Delivery, Invoice, etc.)
└── Views/
    ├── DashboardView               # Real-time KPIs, fleet status counters & revenue widgets
    ├── LoadingManagementView       # Client delivery orders, G-Crate/E-Crate loads & Cold/Dry types
    ├── TruckManagementView         # Vehicle fleet, plate numbers, capacities & Cold/Dry specs
    ├── CrewManagementView          # Drivers & aides, contact numbers, licenses, wages & allowances
    ├── DispatchManagementView      # Trip assignment with dynamic capacity & temperature matching
    ├── TripReportView              # Delivery completion, delivered/returned crates & manager sign-offs
    ├── IncidentReportView          # Damage/delay logging, repair estimates & photo evidence
    ├── BillingView                 # 15-day freight service billing invoices & deduction clamping
    ├── CollectionView              # Payment receipts (Cash, Check, Transfer) & receipt printing
    ├── RateMatrixView              # Fixed/per-crate freight pricing by route and season
    ├── UserManagementView          # Admin user account creation, role assignment & password updates
    ├── ReportsView                 # 8 audit reports with date range filtering and CSV export
    ├── BackupRestoreView           # Database snapshot creation and single-click restoration
    └── AnalogClockTimePickerDialog # Interactive analog clock picker for operational time fields
```

---

## 🚚 Operational Workflow & State Machine

```
[ 1. Loading Schedule ]
      • Client, Destination, Cargo Type (Cold / Dry)
      • G-Crates & E-Crates Breakdown
      • Departure Time (Analog Clock 🕒)
      • Status: "Pending"
             │
             ▼
[ 2. Delivery Dispatch ]
      • Fleet Recommendation: Capacity Fit + Temperature Match (Cold / Dry)
      • Assign Available Truck + Driver + Aides
      • Status: "In Transit" (Truck marked "In Use", Crew locked)
             │
             ▼
[ 3. Trip Completion Report ]
      • Arrival & Departure Times (Analog Clock 🕒)
      • Delivered vs. Returned Crates Reconciliation
      • Store Manager Sign-Off
      • Status: "Completed" (Truck & Crew released to "Available")
             │
             ▼
[ 4. Billing Invoice ]
      • Client Billing Statement (15-Day Terms)
      • Gross Trip Amount - Incident Deductions = Net Due
      • Status: "Unpaid"
             │
             ▼
[ 5. Collection & Official Receipt ]
      • Record Payment (Cash, Check, Bank Transfer, Card)
      • Print Official Collection Receipt
      • Status: "Paid" or "Partially Paid"
```

---

## 🗄️ Database Schema & Entities

The system operates on an embedded SQLite database (`mj_billones.db`) with zero external server dependencies:

| Table Name | Primary Purpose |
| :--- | :--- |
| **`Users`** | System accounts, SHA-256 password hashes, legal names, contact numbers, and roles (`Admin`, `Office Staff`). |
| **`Trucks`** | Fleet vehicles, plate numbers, categories, crate capacities, storage types (`Cold / Chilled`, `Dry Goods`), and statuses (`Available`, `In Use`, `Under Maintenance`, `Inactive`). |
| **`Crews`** | Driver and aide personnel, licenses, contact numbers, daily wages, food allowances, availability, and photo attachments. |
| **`LoadingSchedules`** | Client delivery orders, destination branches, schedule dates, departure times, G-Crate/E-Crate counts, cargo types, and peak season tags. |
| **`Deliveries`** | Operational dispatch records connecting loading schedules with assigned trucks, drivers, and aides. |
| **`TripReports`** | Trip completion registers recording store arrival/departure times, delivered/returned crates, and store manager sign-offs. |
| **`IncidentReports`** | Cargo damage, vehicle breakdowns, route delays, estimated repair costs, crew liability flags, and photo documentation. |
| **`RateMatrix`** | Freight rate schedule by route, destination, vehicle category, and seasonal surcharge. |
| **`Invoices`** | Freight service billing invoices, gross freight amounts, incident deductions, net amounts due, and payment statuses. |
| **`CollectionReceipts`** | Payment transaction logs, official receipt numbers (`CRV-...`), payment modes, and collector details. |
| **`LookupCategories`** | Dynamic dropdown list items (Truck categories, destinations, clients, incident causes) managed via the top bar. |

---

## 🔑 Default Login Credentials

| Username | Password | Role | Permissions & Access |
| :--- | :--- | :--- | :--- |
| **`admin`** | `admin123` | **Admin** | Full system access: User Accounts, Database Backup/Restore, Categories, and all Operational Modules. |
| **`staff`** | `staff123` | **Office Staff** | Operational access: Loading Schedules, Dispatches, Trip Reports, Billing, Collections, and Reports. |

---

## 💻 Tech Stack & Requirements

- **Framework**: .NET 9.0 (C# 13.0) Windows Forms (`net9.0-windows`)
- **Database Engine**: SQLite (`Microsoft.Data.Sqlite 10.0.10`) with WAL journaling
- **IDE**: Microsoft Visual Studio 2022 / VS Code with .NET SDK
- **OS**: Windows 10 / Windows 11 (x64)

### Building & Running from Source:

```powershell
# 1. Navigate to the repository directory
cd "Transport Delivery Management System for M.J. Billones Transport Service"

# 2. Build the solution
dotnet build

# 3. Publish self-contained or framework-dependent release binaries
dotnet publish -c Release -r win-x64 --self-contained false -o bin\Release\Publish
```

---

## 📄 License & Confidentiality

Developed specifically for **M.J. Billones Transport Service**. All rights reserved. Designed for professional commercial logistics operations and technical evaluation.
