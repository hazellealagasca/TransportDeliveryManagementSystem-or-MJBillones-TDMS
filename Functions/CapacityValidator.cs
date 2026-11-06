using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Outcome of a capacity check.</summary>
    public enum CapacityState
    {
        /// <summary>S1 — fits the truck's matrix.</summary>
        Allow,
        /// <summary>S2 — Combi load above the Combi matrix but within the truck maximum: allowed with a reason.</summary>
        ExceedWithReason,
        /// <summary>S3 — pure Dry or Cold load over its limit: exceed-capacity warning, saved only with a reason.</summary>
        BlockPure,
        /// <summary>S4 — Combi load over Cold max, Dry max or the truck maximum: exceed-capacity warning, saved only with a reason.</summary>
        BlockCombi,
        /// <summary>D4 — no crates entered.</summary>
        NoCrates,
        /// <summary>D6 — Combi without both a cold and a dry part.</summary>
        CombiSplitMissing,
        /// <summary>D7 — Cold or Combi cargo on a Dry-only truck.</summary>
        WrongTruck
    }




    public sealed class CapacityResult
    {
        public CapacityState State { get; init; }
        public Truck? Truck { get; init; }
        public string Cargo { get; init; } = "";
        public int Cold { get; init; }
        public int Dry { get; init; }
        public int Total => Cold + Dry;
        public TruckCapacity Matrix { get; init; } = new TruckCapacity();
        /// <summary>The limit that applies to this load (the one shown as "max").</summary>
        public int Limit { get; init; }
        /// <summary>Crates above the matrix (S2: above Combi max; S3/S4: above the broken limit).</summary>
        public int Over { get; init; }
        public string Title { get; init; } = "";
        public string Message { get; init; } = "";
        public bool CanSave => State == CapacityState.Allow || State == CapacityState.ExceedWithReason;
        public bool IsBlocked => !CanSave;
        public string Code => State switch
        {
            CapacityState.Allow => "S1",
            CapacityState.ExceedWithReason => "S2",
            CapacityState.BlockPure => "S3",
            CapacityState.BlockCombi => "S4",
            CapacityState.NoCrates => "D4",
            CapacityState.CombiSplitMissing => "D6",
            _ => "D7"
        };
    }




    /// <summary>
    /// The one capacity check used by every dispatch screen. Order: no crates (D4) → Combi split missing (D6) →
    /// Cold/Combi on a Dry truck (D7) → pure load over its limit (S3) → Combi over Cold/Dry/truck max (S4) →
    /// Combi above the Combi matrix but within the truck max (S2, needs a reason) → fits (S1).
    /// The weight check is not used.
    /// </summary>
    public static class CapacityValidator
    {
        public const int MinReasonLength = 5;




        /// <summary>Splits G+E crates into cold and dry by cargo type (Combi uses the split the user entered).</summary>
        public static (int cold, int dry) SplitFor(string? cargo, int totalCrates, int combiCold, int combiDry)
        {
            return MJdatabase.NormalizeCargo(cargo) switch
            {
                MJdatabase.CargoDry => (0, totalCrates),
                MJdatabase.CargoCombi => (combiCold, combiDry),
                _ => (totalCrates, 0)
            };
        }




        public static TruckCapacity MatrixFor(Truck truck)
        {
            TruckCapacity? m = null;
            try { m = MJdatabase.GetCapacityFor(truck.TruckType); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            // No matrix for this category yet: fall back to the truck's own capacity for pure loads.
            return m ?? new TruckCapacity { CategoryName = truck.TruckType ?? "", DryMax = truck.CapacityCrates, ColdMax = truck.CapacityCrates, CombiColdMax = 0, CombiDryMax = 0 };
        }




        /// <summary>
        /// Nov 6 workflow: the dispatch form no longer asks for a cargo / temperature type, so a load is checked as its total
        /// crates (G + E) against the truck's maximum. (Combi is only a note in the remarks.)
        /// </summary>
        public static CapacityResult ValidateTotal(Truck truck, int totalCrates)
        {
            var m = MatrixFor(truck);
            int total = Math.Max(0, totalCrates), max = m.TruckMax;
            string plate = truck.PlateNumber;
            CapacityResult R(CapacityState st, int over, string title, string msg) =>
                new CapacityResult { State = st, Truck = truck, Cargo = MJdatabase.NormalizeCargo(truck.CargoType), Dry = total, Matrix = m, Limit = max, Over = over, Title = title, Message = msg };
            if (total == 0) return R(CapacityState.NoCrates, 0, "No crates entered", "Enter the G-crates and E-crates for this trip before checking the truck.");
            if (max > 0 && total > max)
                return R(CapacityState.BlockPure, total - max, "Over the truck's capacity", $"{total:N0} crates is {total - max:N0} more than {plate} can carry ({max:N0} crates).");
            return R(CapacityState.Allow, 0, "Fits", $"{total:N0} of {max:N0} crates ({Pct(total, max)}).");
        }

        public static CapacityResult Validate(Truck truck, string? cargoType, int cold, int dry, decimal? weightKg = null)
        {
            string cargo = MJdatabase.NormalizeCargo(cargoType);
            string truckCargo = MJdatabase.NormalizeCargo(truck.CargoType);
            var m = MatrixFor(truck);
            cold = Math.Max(0, cold);
            dry = Math.Max(0, dry);
            int total = cold + dry;
            string plate = truck.PlateNumber;
            CapacityResult R(CapacityState s, int limit, int over, string title, string msg) =>
                new CapacityResult { State = s, Truck = truck, Cargo = cargo, Cold = cold, Dry = dry, Matrix = m, Limit = limit, Over = over, Title = title, Message = msg };




            // 1. D4
            if (total == 0)
                return R(CapacityState.NoCrates, 0, 0, "No crates entered", "Enter the crates for this trip before checking the truck.");
            // 2. D6
            if (cargo == MJdatabase.CargoCombi && (cold == 0 || dry == 0))
                return R(CapacityState.CombiSplitMissing, 0, 0, "Combi needs both parts", "A Combi (Cold + Dry) load needs both cold crates and dry crates. Enter the split, or change the cargo type to Dry Goods or Cold / Chilled.");
            // 3. D7
            if (truckCargo == MJdatabase.CargoDry && cargo != MJdatabase.CargoDry)
                return R(CapacityState.WrongTruck, 0, 0, "This truck can't keep goods cold", $"{plate} is a Dry Goods truck. {cargo} cargo needs a refrigerated (Cold / Chilled or Combi) truck.");
            // 4. Weight: not used.
            // 5. S3 — pure loads
            if (cargo == MJdatabase.CargoDry)
            {
                if (total > m.DryMax)
                    return R(CapacityState.BlockPure, m.DryMax, total - m.DryMax, "Over the truck's dry capacity", $"{total:N0} dry crates is {total - m.DryMax:N0} more than {plate} can carry ({m.DryMax:N0} dry crates).");
                return R(CapacityState.Allow, m.DryMax, 0, "Fits", $"{total:N0} of {m.DryMax:N0} dry crates ({Pct(total, m.DryMax)}).");
            }
            if (cargo == MJdatabase.CargoCold)
            {
                if (total > m.ColdMax)
                    return R(CapacityState.BlockPure, m.ColdMax, total - m.ColdMax, "Over the truck's cold capacity", $"{total:N0} cold crates is {total - m.ColdMax:N0} more than {plate} can carry chilled ({m.ColdMax:N0} cold crates).");
                return R(CapacityState.Allow, m.ColdMax, 0, "Fits", $"{total:N0} of {m.ColdMax:N0} cold crates ({Pct(total, m.ColdMax)}).");
            }
            // 6. Combi
            if (cold > m.ColdMax)
                return R(CapacityState.BlockCombi, m.ColdMax, cold - m.ColdMax, "Too many cold crates", $"{cold:N0} cold crates is more than {plate}'s cold maximum of {m.ColdMax:N0}.");
            if (dry > m.DryMax)
                return R(CapacityState.BlockCombi, m.DryMax, dry - m.DryMax, "Too many dry crates", $"{dry:N0} dry crates is more than {plate}'s dry maximum of {m.DryMax:N0}.");
            if (total > m.TruckMax)
                return R(CapacityState.BlockCombi, m.TruckMax, total - m.TruckMax, "Over the truck's maximum", $"{total:N0} crates is {total - m.TruckMax:N0} more than {plate} can physically carry ({m.TruckMax:N0}).");
            if (m.CombiMax > 0 && total > m.CombiMax)
                return R(CapacityState.ExceedWithReason, m.CombiMax, total - m.CombiMax, $"Over matrix +{total - m.CombiMax}", $"{total:N0} crates is {total - m.CombiMax:N0} above {plate}'s Combi plan ({m.CombiColdMax} cold + {m.CombiDryMax} dry = {m.CombiMax}), but within its maximum of {m.TruckMax:N0}. You can go ahead if you give a reason.");
            int lim = m.CombiMax > 0 ? m.CombiMax : m.TruckMax;
            return R(CapacityState.Allow, lim, 0, "Fits", $"{cold:N0} cold + {dry:N0} dry = {total:N0} of {lim:N0} crates ({Pct(total, lim)}).");
        }




        private static string Pct(int a, int b) => b <= 0 ? "—" : $"{Math.Round(a * 100.0 / b):0}% full";




        /// <summary>Trucks that could carry this load with no exception (S1), smallest first.</summary>
        public static List<(Truck Truck, CapacityResult Result)> TrucksThatFit(IEnumerable<Truck> trucks, string? cargo, int cold, int dry, int exceptTruckId = 0)
        {
            return trucks.Where(t => t.Id != exceptTruckId)
                .Select(t => (t, Validate(t, cargo, cold, dry)))
                .Where(x => x.Item2.State == CapacityState.Allow)
                .OrderBy(x => x.Item2.Limit)
                .ThenBy(x => x.t.PlateNumber)
                .ToList();
        }
    }
}
