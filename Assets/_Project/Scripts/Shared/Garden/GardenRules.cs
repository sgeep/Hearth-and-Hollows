using System;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Surface;

namespace Hearthdelve.Shared.Garden
{
    /// <summary>What a garden action came to.</summary>
    public enum GardenResult
    {
        Done,
        /// <summary>No bed by that id.</summary>
        UnknownBed,
        /// <summary>No crop (or one with no produce).</summary>
        NoCrop,
        /// <summary>Something's already planted there.</summary>
        Occupied,
        /// <summary>Not enough Vigor today: nothing happened.</summary>
        TooTired,
        /// <summary>Nothing growing there to tend.</summary>
        NotGrowing,
        /// <summary>Already tended today.</summary>
        TendedToday,
        /// <summary>Not ready to harvest.</summary>
        NotReady,
    }

    /// <summary>How a bed looks.</summary>
    public enum BedStage
    {
        Empty,
        Seeds,
        Sprouting,
        Growing,
        Ready,
    }

    /// <summary>
    /// The garden's rules (4h Checkpoint B), pure. Planting and tending cost Vigor (harvesting costs what the tuning says, 0 for
    /// now); each is all or nothing. Crops grow one step per new day, once (a bed remembers the day it last grew); an untended
    /// crop still grows and never dies or rots, it only misses the Fine harvest. A ready crop waits as long as it likes.
    /// </summary>
    public static class GardenRules
    {
        public static bool IsReady(BedState bed, CropDefinition crop) => bed != null && !bed.IsEmpty && crop != null && bed.Grown >= crop.growthDays;

        public static bool IsGrowing(BedState bed, CropDefinition crop) => bed != null && !bed.IsEmpty && crop != null && bed.Grown < crop.growthDays;

        /// <summary>Nights until it's ready (0 when ready or empty).</summary>
        public static int DaysToReady(BedState bed, CropDefinition crop) => IsGrowing(bed, crop) ? crop.growthDays - bed.Grown : 0;

        public static bool TendedToday(BedState bed, int day) => bed != null && bed.LastTendedDay == day && day > 0;

        /// <summary>The tended days a crop needs for a Fine harvest (at least half of its growing days, by default).</summary>
        public static int TendsForFine(CropDefinition crop) =>
            crop == null ? int.MaxValue : Math.Max(1, (int)Math.Ceiling(crop.fineTendedShare * crop.growthDays - 1e-4));

        /// <summary>Fine when tended on enough of its growing days, Standard otherwise.</summary>
        public static Quality HarvestQuality(BedState bed, CropDefinition crop) =>
            bed != null && crop != null && bed.TendedDays >= TendsForFine(crop) ? Quality.Fine : Quality.Standard;

        public static BedStage Stage(BedState bed, CropDefinition crop)
        {
            if (bed == null || bed.IsEmpty || crop == null) return BedStage.Empty;
            if (IsReady(bed, crop)) return BedStage.Ready;
            if (bed.Grown <= 0) return BedStage.Seeds;
            return bed.Grown * 2 < crop.growthDays ? BedStage.Sprouting : BedStage.Growing;
        }

        // ---------- planting ----------

        public static GardenResult CanPlant(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, in VigorSettings costs)
        {
            BedState bed = garden?.Bed(bedId);
            if (bed == null) return GardenResult.UnknownBed;
            if (crop == null || crop.produce == null || string.IsNullOrEmpty(crop.id)) return GardenResult.NoCrop;
            if (!bed.IsEmpty) return GardenResult.Occupied;
            if (vigor == null || !vigor.CanAfford(costs.Cost(VigorActivity.PlantBed))) return GardenResult.TooTired;
            return GardenResult.Done;
        }

        /// <summary>Prepares the bed and plants <paramref name="crop"/> on <paramref name="day"/> (seeds are free in 4h).</summary>
        public static GardenResult Plant(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, int day, in VigorSettings costs)
        {
            GardenResult can = CanPlant(garden, vigor, bedId, crop, costs);
            if (can != GardenResult.Done) return can;
            vigor.Spend(costs.Cost(VigorActivity.PlantBed));
            BedState bed = garden.Bed(bedId);
            bed.Clear();
            bed.Crop = crop.id;
            bed.PlantedDay = day;
            // It first grows on the next new day.
            bed.LastGrownDay = day;
            return GardenResult.Done;
        }

        // ---------- tending ----------

        public static GardenResult CanTend(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, int day, in VigorSettings costs)
        {
            BedState bed = garden?.Bed(bedId);
            if (bed == null) return GardenResult.UnknownBed;
            if (!IsGrowing(bed, crop)) return GardenResult.NotGrowing;
            if (TendedToday(bed, day)) return GardenResult.TendedToday;
            if (vigor == null || !vigor.CanAfford(costs.Cost(VigorActivity.TendBed))) return GardenResult.TooTired;
            return GardenResult.Done;
        }

        /// <summary>Tends a growing bed: once per bed per day, counted toward a Fine harvest.</summary>
        public static GardenResult Tend(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, int day, in VigorSettings costs)
        {
            GardenResult can = CanTend(garden, vigor, bedId, crop, day, costs);
            if (can != GardenResult.Done) return can;
            vigor.Spend(costs.Cost(VigorActivity.TendBed));
            BedState bed = garden.Bed(bedId);
            bed.TendedDays++;
            bed.LastTendedDay = day;
            return GardenResult.Done;
        }

        // ---------- harvesting ----------

        public static GardenResult CanHarvest(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, in VigorSettings costs)
        {
            BedState bed = garden?.Bed(bedId);
            if (bed == null) return GardenResult.UnknownBed;
            if (crop == null || crop.produce == null) return GardenResult.NoCrop;
            if (!IsReady(bed, crop)) return GardenResult.NotReady;
            if (vigor == null || !vigor.CanAfford(costs.Cost(VigorActivity.HarvestBed))) return GardenResult.TooTired;
            return GardenResult.Done;
        }

        /// <summary>Harvests a ready bed: its produce (fresh; Fine or Standard) comes out and the bed is empty again.</summary>
        public static GardenResult Harvest(GardenState garden, Vigor vigor, string bedId, CropDefinition crop, in VigorSettings costs, out IngredientStack produce)
        {
            produce = IngredientStack.Empty;
            GardenResult can = CanHarvest(garden, vigor, bedId, crop, costs);
            if (can != GardenResult.Done) return can;
            vigor.Spend(costs.Cost(VigorActivity.HarvestBed));
            BedState bed = garden.Bed(bedId);
            produce = new IngredientStack(new IngredientItem(crop.produce, HarvestQuality(bed, crop)), crop.yield, Freshness.Max);
            bed.Clear();
            return GardenResult.Done;
        }

        // ---------- overnight ----------

        /// <summary>
        /// A new day (<paramref name="day"/>) has begun: each planted bed that hasn't grown for it grows one step (a ready one
        /// just waits). Calling it again for the same day changes nothing. Returns how many beds grew.
        /// </summary>
        public static int GrowOvernight(GardenState garden, int day, Func<string, CropDefinition> crops, in GardenSettings settings)
        {
            if (garden == null || crops == null) return 0;
            int grew = 0;
            foreach (BedState bed in garden.Beds)
            {
                if (bed.IsEmpty || bed.LastGrownDay >= day) continue;
                CropDefinition crop = crops(bed.Crop);
                bed.LastGrownDay = day;
                if (crop == null || IsReady(bed, crop)) continue;
                // The stricter rule, for the playtest to try: no tending yesterday, no growth tonight.
                if (settings.untendedPausesGrowth && bed.LastTendedDay != day - 1) continue;
                bed.Grown++;
                grew++;
            }
            return grew;
        }
    }
}
