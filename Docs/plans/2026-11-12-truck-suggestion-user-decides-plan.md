# Truck suggestion: suggest the smallest truck, the user decides (12 Nov 2026)

## Goal
When crates are entered on the dispatch form (for example 1 crate), the system suggests the
smallest free truck that fits. The suggestion never selects a truck by itself; the user always picks.

## Today (master 9dfd203)
- `MJdatabase.GetTruckRecommendation` (Database/MJdatabase.cs:5373) already picks the smallest free
  truck, but sizes trucks by `CapacityCrates`, not the category matrix used by `CapacityValidator`.
- `DispatchManagementView.cs:1224` auto-selects the suggested truck when a dispatch is loaded from
  a loading schedule (takes the decision away from the user).
- `DispatchManagementView.cs:1692` footer suggestion passes cargo type `""`, ignoring Temp Type.
- Once a truck is picked, nothing says a smaller free truck would fit.
- Settings text says the feature "picks it for you".

## Changes
1. **Never auto-select.** Remove the auto-select at line 1224. The Truck box stays as the user left it.
2. **Same sizing as the capacity check.** `GetTruckRecommendation` orders and fits trucks by
   `CapacityValidator.MatrixFor(truck)` limits for the load's temp type (Dry → DryMax, Cold → ColdMax,
   Combi → must pass `CapacityValidator` without a block), smallest first, tie broken by plate number.
   Existing rules kept: busy trucks skipped, blocked/not-ready statuses skipped, cold loads skip
   "Dry Goods" trucks, multi-truck message when nothing fits.
3. **Footer passes the Temp Type** (and E/G split for Combi where the form has it).
4. **"Use suggested truck" button** on the footer line when no truck is picked. Clicking it selects
   that truck in the Truck box (the user's own action). Hidden when the switch is off or there is no suggestion.
5. **Soft note when a bigger truck is picked.** If the picked truck fits and a smaller free truck also
   fits, add an info line: "A smaller truck (PLATE) is free and fits N crates." with a "Use it" link.
   No reason required, nothing blocked, saving unchanged. The capacity warning and overload reason for a
   too-small truck stay exactly as they are.
6. **Settings wording**: "Suggest the best truck" description becomes "suggests the smallest free truck
   that fits the crates; you choose the truck" (FeaturesSettingsView.cs:22, SystemCustomizationView.cs:152).

## Not changing
How deliveries are saved, the capacity check and its reasons, the on/off switch, any business rule.

## Checks
- Verify check: 1 crate → smallest free fitting truck; busy truck skipped; Cold skips dry-only;
  bigger-than-needed truck order not blocked.
- Verify check: loading a dispatch from a schedule leaves the Truck box unselected.
- Build with zero warnings; run the dispatch/capacity/truck Verify checks on a copy of the db.
