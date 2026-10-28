# Rule: Billing Workflow & Domain Terminology

1. **System Terminology Guardrails**:
   - Strictly prohibit "tray" and "trays"; always use "crate" and "crates" (`E-Crate`, `G-Crate`).
   - Strictly prohibit "franchise" and "Franchise"; use "Trip Report", "Official Trip Report", or "Route".

2. **Finance Hub Workflow Precedence**:
   - Order tabs in `FinanceHubView`:
     1. `📑 Statement of Account (SOA)` (The primary starting point)
     2. `🧾 Billing Invoices` (Sales Invoices / SI NO. linked to SOA or standalone)
     3. `💵 Collections & Receipts` (Payments against Invoices)
     4. `📜 Finance History`
   - Statement of Account generation must support the client's official Excel layout:
     - Header: M.J. Billones Transport Service, Jasmin St. San Francisco, Tarlac City, Marie Ann J. Billones - Proprietor, VAT Reg. TIN: 708-839-875-000.
     - Client Header: "MORE THAN CAKES, INC." (or selected client), Address, TIN, Date.
     - Trip multi-drop line items with DR# per store, Plate No, Tons, Drops, Rate Matrix Amount, Fuel Surcharge, and Crew.
     - Tax calculations: 12% VAT extraction, 2% Expanded Withholding Tax (EWT), and Total Amount Due.
     - SI NO. linkage to Billing Invoices.
