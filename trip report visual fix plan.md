# Trip Reports: visual fix plan (short version)

Plan only. No code changed. The long version is in `trip report visual fix plan (detailed).md`.

## Problems (from your screenshot and the real data)
1. The table is wider than the screen, so Dispatch #, Date and Client are hidden off the left.
2. Destinations are up to 177 characters long (multi-stop routes), so they never fit one line.
3. Every row is red, so red tells you nothing. The selected row is red on red.
4. The Create button is a pale pink that clashes with the dark theme.
5. The four tiles don't match the table under them (7 reports vs 15 trips) and say confusing things.

## What I will do

**Table**
- Fewer columns: Dispatch (number with the date and "1 day ago" under it), Client, Route, Truck, Driver, Report status, Button. This removes the Date and Report # columns.
- Route shows a short line (`R3 · Route 3` plus first stop ➔ last stop, "9 stops"). Hovering shows the full route.
- Fits the screen with no sideways scrolling. Dispatch # stays visible.
- Status becomes a small pill: grey = new, amber = 3+ days, red = 7+ days. Only old trips are red.
- Button and selected row use the theme colours.

**Tiles** (light gray, smaller, simple)
- About 64 px high and 200 px wide, light gray with dark text, caption + number + short note.
- They describe the table you are looking at, with **Special Trips in their own tile**: Dispatches need a report (9), Special trips need a report (3), Overdue (3), Draft saved (0).
- The table header reads "12 completed trips need a report (9 dispatches, 3 special trips)". Special trips get a small "Special trip" tag in the Dispatch cell.
- Clicking a tile filters the table (Dispatches shows only regular dispatches, Special trips shows only special trips). Hovering explains it.
- Only the Trip Reports tiles change. Other screens keep theirs.

**Small fixes**
- Search box wide enough; clear "X" inside it.
- Filter dropdowns wide enough for names like `GOLDILOCKS CORPORATION`.
- A message when the list is empty or fails to load.

## How I will check
Build with zero warnings, then screenshot the screen on a copy of the real data in dark and light themes.

## Not included
Logic fixes (see the instructions file), the inner report form, and any data changes.
