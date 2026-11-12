# Truck box: typing that works, and the suggestion where you pick (12 Nov 2026)

## What the user saw (build 9951de3, running)
Clicking "Assigned Truck" opens the full list, and typing does nothing. No suggestion is visible.

## Why
1. **Typing is ignored while the list is open.** `SearchPicker.HandleKeyPress` (Functions/SearchPicker.cs) returns
   when `box.DroppedDown` is true. Clicking the box opens the normal list, so after a click typing never filters.
   The normal list's own letter-jump doesn't help either: every item starts with the status badge ("● ACTIVE …"),
   not the plate.
2. **What you type is never shown.** Even when the search list does open (typing without clicking first), the box
   still shows "Type to search", so it looks like nothing was typed.
3. **The suggestion is far from the Truck box.** It is in the footer under the crate table (section 1); the Truck box
   is in section 2. With the list open it is off screen, and the list itself does not mark the suggested truck.

## Fix
1. **Typing always works.** When the normal list is open and a letter or digit is typed, close it and open the search
   list with that letter. Backspace, Up/Down, Enter and Esc work as now.
2. **Show what is typed.** The search list gets a top row "Search: cao▏" (and the box shows the typed text in place of
   the grey hint). Matches search the plate *and* the tonner, so "4 ton", "4-tonner", "cao" or "1116" all work.
   "No truck matches 'xyz'" when nothing matches.
3. **Suggested truck at the top of both lists.** When a suggestion exists, the suggested truck is drawn first in the
   search list and marked in the normal list with a chip on the right: "★ Suggested · fits 193 of 280". The order
   and positions of the items are not changed (picking still maps to the same truck), only the drawing.
4. **Suggestion line right under the Truck box** (section 2), when no truck is picked:
   "★ Suggested: 4-tonner · CAO 1116 · fits 193 of 280 crates   [Use this truck]" in 10.5 pt bold.
   The footer in section 1 keeps its larger "SUGGESTED TRUCK" line.
5. Same for the Driver box (items 1 and 2 only), since it uses the same search.

## Not changing
The truck is never picked for you. Items, their order, how the pick is saved, capacity rules, business rules.

## Where
- `Functions/SearchPicker.cs`: type while dropped down, query row, optional "pinned" item callback, empty-match row.
- `Views/DispatchManagementView.cs`: Attach search text (plate + type + tonner), `DrawStatusComboBoxItem` chip,
  hint text shows the query, new suggestion line under `cbTruck`.
- `Views/DispatchManagementView.Capacity.cs`: share `suggestedTruckId` with the list and the new line.

## Checks
- Verify: click to open the list, type "cao" → search list opens with only matching trucks and shows "cao";
  Enter picks the right truck (original index). Typing "4 ton" finds 4-tonners.
- Verify: with a suggestion, it is the first row of the search list and has the chip; picking another truck works.
- Existing `SearchPicker_DispatchBoxes_*` and TruckSuggestion checks still pass; zero-warning build;
  screenshots of the open list and the line under the Truck box.
