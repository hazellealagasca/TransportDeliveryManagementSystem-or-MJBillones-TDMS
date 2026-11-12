# Address optional for all, and the crate total in the suggestion line (12 Nov 2026)

## Part A: every address is optional
Store addresses are already optional (9dfd203). Addresses that are still required on master e87ef83:

| Where | Today | Change |
|---|---|---|
| Clients › head office Address (Views/ClientManagementView.cs:949) | Save refused when blank ("Please fill in these required fields… Address") | Optional; label "Address (optional)"; blank saves |
| Settings › Business address (Views/SystemCustomizationView.cs:96) | Label shows "Business address *" (not checked on save) | Drop the "*"; label "Business address (optional)" |
| Client branches (grid on the client screen) | Not required | No change (check only) |

Printouts (receipt, SOA, invoice, trip papers): a blank address prints as an empty line, never "—" or an error.
Screens that show the address (client strip, details) already skip it when blank.

**Left as is:** the Special trip "Destination address" check (SpecialTripFormDialog.cs:1244): special trips are
hidden from the app, so nobody sees it. Company name, TIN, contact person and contact number stay required.

## Part B: the total in the suggestion line
The old line started with "Total 193 crates"; the new larger line dropped it. Put it back in both places:

Footer (section 1):
```
 SUGGESTED TRUCK                                              [ Use this truck ]
 Total 193 crates  ·  6-tonner  ·  CAO 1116  ·  fits 193 of 280 crates (69%)
```
Line under the Truck box (section 2):
```
 ★ Total 193 crates · Suggested: 6-tonner · CAO 1116 · fits 193 of 280   [Use this truck]
```
Truck already picked (unchanged apart from the total): "Total 193 crates · 6-tonner · CAO 1116 · 193 / 280 crates (69% full)".
"Total N crates" uses the same bold 12 pt as the tonner and plate.

## Not changing
Business rules, saving, the capacity check, the truck still only suggested.

## Checks
- Verify: a client with a blank address saves; the settings label has no "*"; a receipt and SOA with a blank client
  address print without error.
- Verify: footer and the line under the Truck box both contain "Total N crates".
- Zero-warning build; related checks on a copy of the database; screenshot of both lines.
