# Truck suggestion line: show the tonner and plate, larger text (12 Nov 2026)

## Goal
The suggestion line under the crate table is small and only shows the plate. Make it easy to read
at a glance: tonner, plate number and capacity, in larger text, with a clear button to use it.

## Today (master a6e92e0)
- Footer label `lblRec` is Segoe UI Semibold 8 pt (DispatchManagementView.cs:436).
- Text: "ℹ Total 193 crates · suggested truck CAO1116 (280 crates)".
- "Use suggested truck" is a small 8.75 pt link under it.
- Truck types in the data look like "FORWARD 6 TONS", "FORWARD 8 TONS".

## New look
```
 SUGGESTED TRUCK                                         [ Use this truck ]
 6-tonner  ·  CAO 1116  ·  fits 193 of 280 crates (69%)
```
- Line 1: small caps label "SUGGESTED TRUCK" (9 pt, muted) and a real button on the right.
- Line 2: tonner and plate in **bold 12 pt**, capacity in 10.5 pt.
- Tonner is read from the truck type ("FORWARD 6 TONS" → "6-tonner"). If no number is found,
  the truck type is shown as is (e.g. "Refrigerated Van").
- Same green / red colours as today (red when no single truck fits, with the message in 10.5 pt).
- Truck already picked: the capacity line becomes "6-tonner · CAO 1116 · 193 / 280 crates (69% full)"
  in the same larger size; the soft note "A smaller truck (4-tonner · ABC 123) is free and fits 193 crates"
  also shows the tonner, with a "Use it" button.
- The duplicate crate line at the bottom bar is not touched.

## Not changing
How the truck is chosen (still only suggested, the user decides), the capacity check, saving, any rule.

## Where
- `Views/DispatchManagementView.cs` footer text (~line 1670) and fonts (~436, ~689).
- `Views/DispatchManagementView.Capacity.cs`: `ApplyLiveCapacityStatus`, `lnkUseSuggested` → button.
- Small helper `TonnerOf(Truck)` (e.g. in Functions/CapacityValidator.cs or the Capacity partial).

## Checks
- Verify: `TonnerOf("FORWARD 6 TONS") == "6-tonner"`, `"Refrigerated Van"` stays as is.
- Update the TruckSuggestion check for the button; check the line shows tonner and plate.
- Look at the screen at normal and large print (no clipped text); zero-warning build.
