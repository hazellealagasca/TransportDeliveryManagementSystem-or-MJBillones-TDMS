# 🚚 M.J. Billones Transport Service - Web Logistics Management Application

A modern, responsive, enterprise-grade web application version of the **Transport Delivery Management System (TDMS)** custom-engineered for **M.J. Billones Transport Service**.

Designed with the signature **Mild Red and Burgundy / MJ Crimson** palette, high-contrast operational data tables, pre-seeded embedded SQLite database records, interactive analog clock time pickers, dual temperature/capacity truck recommendation intelligence, and official printable collection receipts.

---

## 🌐 Live Web Demo & GitHub Pages Link

Once deployed to GitHub Pages on your repository:
**[https://hazellealagasca.github.io/TransportDeliveryManagementSystem-or-MJBillones-TDMS/](https://hazellealagasca.github.io/TransportDeliveryManagementSystem-or-MJBillones-TDMS/)**

> **Instant Local Access:**
> You can also run this application locally without installing any servers or software! Simply double-click `index.html` in Windows Explorer or open it in Google Chrome, Microsoft Edge, or Mozilla Firefox.

---

## 🔑 Login Credentials

| Role | Username | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Owner / Admin** | `admin` | `admin123` | Complete system control: User Management, Rate Matrix, System Backup/Reset, and all operations. |
| **Office Staff / Dispatcher** | `staff` | `staff123` | Daily operations: Loading Schedules, Trip Dispatches, Trip Reports, Billing, Collections, and Audit Reports. |

*(Quick 1-Click Demo Access buttons are also available directly on the login screen for rapid review).*

---

## 🌟 Key Capabilities & Features

1. **Role-Based Security & Audit Trail**:
   - Distinct views and permission safeguards for **Owner/Admin** vs **Office Staff**.
   - Built-in account recovery with security questions.
   - Comprehensive audit logging recording user, action, timestamp, and details for every transaction.

2. **Cargo Temperature & Storage Intelligence**:
   - Cargo tagging for **Cold / Chilled** (0°C to 4°C active chiller refrigeration) vs **Dry Goods** (ambient hauling).
   - Automated truck matching engine that scores available vehicles, flags temperature mismatches, and prevents under-capacity assignments.

3. **Standardized Crate Accounting**:
   - Specialized hauling breakdown tracking **G-Crates** and **E-Crates** across loading orders, vehicle limits, and trip returns.
   - Automatic discrepancy detection: missing or unreturned crates automatically trigger an incident log.

4. **Interactive Analog Clock Time Picker (`🕒`)**:
   - Intuitive, draggable analog clock face modal with minute precision and AM/PM selection, replicating the desktop app's time dialog.

5. **Print-Ready Official Documents**:
   - **Delivery Dispatch Slip & Manifest** (`DSP-...`)
   - **Official Collection Receipt** (`CR-...`) with amount received, payment mode, and authorized collector sign-off.
   - **15-Day Billing Statement & Invoice** (`INV-...`) with automatic incident loss deduction clamping.

6. **Zero-Dependency Portability & Embedded Storage**:
   - Fully client-side single page architecture using `localStorage` persistence.
   - Pre-loaded with all original database records from `mj_billones.db` (20 trucks, 33 crew members, routes, and schedules).
   - Single-click **Export Database JSON**, **Import Backup JSON**, and **Reset to Original Seed**.

---

## 🚀 One-Click GitHub Deployment

This web application is committed to the `gh-pages` branch. To push it directly to your GitHub repository:

```powershell
# Open terminal inside the web application folder:
cd "d:\Downloads\MJ billones oct 25 2026\MJ_Billones_Transport_Web_System"

# Push to your GitHub repository:
git push -u origin gh-pages --force
```

### Enabling GitHub Pages in Repository Settings:
1. Go to your GitHub repository: `https://github.com/hazellealagasca/TransportDeliveryManagementSystem-or-MJBillones-TDMS`
2. Click **Settings** ➔ **Pages** (in the left sidebar).
3. Under **Build and deployment** ➔ **Source**, select **Deploy from a branch**.
4. Under **Branch**, select `gh-pages` and `/ (root)`, then click **Save**.
5. Within 1 minute, your live site will be live at:
   `https://hazellealagasca.github.io/TransportDeliveryManagementSystem-or-MJBillones-TDMS/`
