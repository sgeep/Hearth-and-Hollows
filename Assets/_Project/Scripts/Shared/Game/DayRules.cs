using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Run;

namespace Hearthdelve.Shared.Game
{
    /// <summary>What a delve brought back.</summary>
    public sealed class DelveReport
    {
        DelveReport(DelveOutcome outcome, List<IngredientStack> haul, int partsLost, int goldSecured, int goldLost, IEnumerable<string> bosses = null,
            IEnumerable<string> curiosKept = null, IEnumerable<string> curiosLost = null, IEnumerable<string> questObjects = null)
        {
            QuestObjectsCarried = questObjects != null ? new List<string>(questObjects) : new List<string>();
            BossesDefeated = bosses != null ? new List<string>(bosses) : new List<string>();
            CuriosKept = curiosKept != null ? new List<string>(curiosKept) : new List<string>();
            CuriosLost = curiosLost != null ? new List<string>(curiosLost) : new List<string>();
            Outcome = outcome;
            Haul = haul;
            PartsLost = partsLost;
            GoldSecured = goldSecured;
            GoldLost = goldLost;
        }

        public DelveOutcome Outcome { get; }
        public IReadOnlyList<IngredientStack> Haul { get; }
        public int PartsLost { get; }
        /// <summary>Run Gold brought home (extraction): banked when the delve completes.</summary>
        public int GoldSecured { get; }
        /// <summary>Run Gold left in the dungeon (death). Gold already banked is never lost.</summary>
        public int GoldLost { get; }
        /// <summary>Bosses defeated on the delve (4e). A victory is a victory: recorded even if the delve then ends in death.</summary>
        public IReadOnlyList<string> BossesDefeated { get; }
        /// <summary>Furnishings found on the delve that come home (extraction): owned when the delve completes (D9).</summary>
        public IReadOnlyList<string> CuriosKept { get; }
        /// <summary>Furnishings found on the delve and lost with it (death). The Lockbox never holds them.</summary>
        public IReadOnlyList<string> CuriosLost { get; }
        /// <summary>Quest objects carried when the delve ended (4g Checkpoint B): home on extraction, lost on a death by their policy.</summary>
        public IReadOnlyList<string> QuestObjectsCarried { get; }

        public int PartsBroughtBack
        {
            get
            {
                int n = 0;
                foreach (var s in Haul) n += s.Count;
                return n;
            }
        }

        /// <summary>Left through the exit: everything in the satchel comes home (freshness as it is now), and the run's Gold with it.</summary>
        public static DelveReport Extraction(Satchel satchel, int runGold = 0, IEnumerable<string> bosses = null, IEnumerable<string> curios = null,
            IEnumerable<string> questObjects = null)
        {
            var haul = new List<IngredientStack>();
            if (satchel != null)
                foreach (var slot in satchel.Slots)
                    if (!slot.IsEmpty) haul.Add(slot);
            return new DelveReport(DelveOutcome.Extracted, haul, 0, Math.Max(0, runGold), 0, bosses, curios, null, questObjects);
        }

        /// <summary>Died: only the Lockbox stack comes home; the run's Gold is lost.</summary>
        public static DelveReport Death(DeathPenaltyResult result, int runGold = 0, IEnumerable<string> bosses = null, IEnumerable<string> curios = null,
            IEnumerable<string> questObjects = null)
        {
            var haul = new List<IngredientStack>();
            if (result.KeptSomething) haul.Add(result.Kept);
            return new DelveReport(DelveOutcome.Died, haul, result.ItemsLost, 0, Math.Max(0, runGold), bosses, null, curios, questObjects);
        }

        /// <summary>Debug skip: nothing brought back.</summary>
        public static DelveReport Empty => new(DelveOutcome.None, new List<IngredientStack>(), 0, 0, 0);
    }

    /// <summary>What an evening's service earned.</summary>
    public readonly struct ServiceReport
    {
        public readonly int DishesServed;
        public readonly int Gold;
        public readonly int Tips;
        public readonly int RenownChange;
        public readonly int Walkouts;

        public ServiceReport(int dishesServed, int gold, int tips, int renownChange, int walkouts)
        {
            DishesServed = dishesServed;
            Gold = gold;
            Tips = tips;
            RenownChange = renownChange;
            Walkouts = walkouts;
        }
    }

    /// <summary>
    /// Every change the day loop makes to <see cref="GameState"/> (GDD §3). Each checks it's
    /// the right phase and moves the cycle on. Pure logic.
    /// </summary>
    public static class DayRules
    {
        /// <summary>The day is done: the tavern opens for the evening (the daytime placeholder's button; later the village day's end).</summary>
        public static void StartEvening(GameState state) => Require(state, DayPhase.Daytime).Cycle.AdvanceTo(DayPhase.Evening);

        /// <summary>
        /// The Act I opening (4g Checkpoint B): the keeper goes down the hatch on arrival day, straight from the daytime to the
        /// first delve (there's nothing to serve yet). The opening moves on to the first delve.
        /// </summary>
        public static void StartOpeningDelve(GameState state)
        {
            Require(state, DayPhase.Daytime);
            if (state.Story.Opening != Story.OpeningStage.Arrival) throw new InvalidOperationException("Only the arrival goes straight down.");
            state.Today.KeptShut = true;
            state.Cycle.AdvanceTo(DayPhase.Evening);
            state.Cycle.AdvanceTo(DayPhase.Delve);
            state.Story.Opening = Story.OpeningStage.FirstDelve;
        }

        /// <summary>
        /// Back from the night's delve: the haul goes into the storeroom, the run's Gold into the purse, the delve meal is
        /// used up, and it's night.
        /// </summary>
        /// <summary>The quest objects the last <see cref="CompleteDelve"/> brought home (for the facts).</summary>
        public static List<string> QuestObjectsHome { get; private set; } = new();

        public static void CompleteDelve(GameState state, DelveReport report, Func<string, Customization.FurnitureDefinition> furniture = null,
            IEnumerable<BossTrophy> trophies = null, Func<string, Quests.QuestObjectDefinition> questObjects = null)
        {
            Require(state, DayPhase.Delve);
            if (report == null) throw new ArgumentNullException(nameof(report));
            state.Storeroom.AddRange(report.Haul);
            // Run Gold is banked only if it came home (GDD §4.4); banked Gold is never at risk.
            state.Gold += report.GoldSecured;
            state.Today.Delve = report.Outcome;
            state.Today.PartsBroughtBack += report.PartsBroughtBack;
            state.Today.PartsLost += report.PartsLost;
            state.Today.DelveGold += report.GoldSecured;
            state.Today.DelveGoldLost += report.GoldLost;
            foreach (string boss in report.BossesDefeated) state.SetBossClears(boss, state.TimesDefeated(boss) + 1);
            // Furnishings found on the delve: owned now if they came home (D9), marked new in storage.
            foreach (string id in report.CuriosKept)
            {
                Customization.FurnitureDefinition definition = furniture?.Invoke(id);
                if (definition != null && state.Furniture.Receive(definition)) state.Today.CuriosKept++;
            }
            state.Today.CuriosLost += report.CuriosLost.Count;
            // A boss's trophy (D10): granted with the victory, whatever the delve's end.
            TrophyRules.GrantEarned(state, trophies);
            // Quest objects (4g Checkpoint B): home on extraction; lost with a death (by their policy), still wanted.
            QuestObjectsHome = Quests.QuestObjectRules.EndDelve(state.QuestObjects, report.QuestObjectsCarried, report.Outcome == DelveOutcome.Extracted, questObjects);
            // The opening: home from the first delve.
            if (state.Story.Opening == Story.OpeningStage.FirstDelve) state.Story.Opening = Story.OpeningStage.Homecoming;
            state.Meal = MealBuff.None;
            state.Cycle.AdvanceTo(DayPhase.Night);
        }

        /// <summary>Service takings (payments and tips) are banked; renown changes; the tavern closes and the night's delve is next.</summary>
        public static void CompleteService(GameState state, ServiceReport report)
        {
            Require(state, DayPhase.Evening);
            state.Gold += Math.Max(0, report.Gold) + Math.Max(0, report.Tips);
            state.Renown += report.RenownChange;
            state.Today.DishesServed += report.DishesServed;
            state.Today.Gold += report.Gold;
            state.Today.Tips += report.Tips;
            state.Today.Walkouts += report.Walkouts;
            state.Today.RenownChange += report.RenownChange;
            state.Today.EveningRecorded = true;
            state.Cycle.AdvanceTo(DayPhase.Delve);
        }

        /// <summary>Kept shut without serving (nothing to cook, or by choice): on to the night's delve.</summary>
        public static void SkipService(GameState state)
        {
            Require(state, DayPhase.Evening);
            state.Today.KeptShut = true;
            state.Today.EveningRecorded = true;
            state.Cycle.AdvanceTo(DayPhase.Delve);
        }

        /// <summary>Eat the delve meal: one a day, cooked in the daytime; its buff waits for tonight's delve.</summary>
        public static bool EatMeal(GameState state, MealBuff meal)
        {
            Require(state, DayPhase.Daytime);
            if (state.Meal.IsActive || !meal.IsActive) return false;
            state.Meal = meal;
            return true;
        }

        /// <summary>
        /// Buys one offer at a supply source (the Brackenford market; daytime only): gold out, the goods straight into the
        /// storeroom, Standard and fresh (D19). False when it can't be afforded.
        /// </summary>
        public static bool Buy(GameState state, Inventory.SupplySource source, Inventory.SupplyOffer offer) => Buy(state, source, offer, null);

        /// <summary>
        /// As above, and only while the market trades (4h): with <paramref name="hours"/>, a purchase after the market has closed
        /// for the day (by the surface clock) is refused.
        /// </summary>
        public static bool Buy(GameState state, Inventory.SupplySource source, Inventory.SupplyOffer offer, Surface.SurfaceClockSettings? hours)
        {
            Require(state, DayPhase.Daytime);
            if (hours is { } h && !Surface.MarketHours.IsOpen(state.Surface.WholeMinute, h)) return false;
            if (!Inventory.SupplyRules.CanAfford(state.Gold, offer)) return false;
            state.Gold -= offer.price;
            state.Storeroom.Add(Inventory.SupplyRules.Delivery(source, offer));
            return true;
        }

        /// <summary>Buys the next level if affordable (Night only).</summary>
        public static bool BuyUpgrade(GameState state, TavernUpgradeDefinition upgrade)
        {
            Require(state, DayPhase.Night);
            int level = state.UpgradeLevel(upgrade != null ? upgrade.id : null);
            if (!Upgrades.CanBuy(upgrade, level, state.Gold)) return false;
            state.Gold -= Upgrades.NextCost(upgrade, level);
            state.SetUpgradeLevel(upgrade.id, level + 1);
            return true;
        }

        /// <summary>Overnight: storeroom stock loses a little freshness, and a new day begins (no garden growth: see the overload).</summary>
        public static void Sleep(GameState state, in FreshnessSettings freshness) => Sleep(state, freshness, null, Garden.GardenSettings.Default);

        /// <summary>
        /// Overnight, in this order (4h Checkpoint B): storeroom stock loses a little freshness; a new day begins; the garden grows
        /// once for that day (each bed remembers the day it grew, so nothing grows twice); Vigor is full again. The surface clock's
        /// morning is set by <see cref="GameFlow"/> straight after. Only from the Night, so a second call can't run another night.
        /// </summary>
        public static void Sleep(GameState state, in FreshnessSettings freshness, Func<string, Garden.CropDefinition> crops, in Garden.GardenSettings garden)
        {
            Require(state, DayPhase.Night);
            state.Storeroom.Decay(freshness, freshness.overnightLoss);
            state.Today.Reset();
            state.Cycle.AdvanceTo(DayPhase.Daytime);
            Garden.GardenRules.GrowOvernight(state.Garden, state.Day, crops, garden);
            state.Vigor.Refill();
        }

        // ---------- the garden (4h Checkpoint B; daytime only) ----------

        public static Garden.GardenResult PlantBed(GameState state, string bedId, Garden.CropDefinition crop, in Surface.VigorSettings vigor) =>
            Daytime(state) ? Garden.GardenRules.Plant(state.Garden, state.Vigor, bedId, crop, state.Day, vigor) : Garden.GardenResult.UnknownBed;

        public static Garden.GardenResult TendBed(GameState state, string bedId, Garden.CropDefinition crop, in Surface.VigorSettings vigor) =>
            Daytime(state) ? Garden.GardenRules.Tend(state.Garden, state.Vigor, bedId, crop, state.Day, vigor) : Garden.GardenResult.UnknownBed;

        /// <summary>Harvests a ready bed: its produce goes into the storeroom as an ordinary, fresh stack.</summary>
        public static Garden.GardenResult HarvestBed(GameState state, string bedId, Garden.CropDefinition crop, in Surface.VigorSettings vigor,
            out IngredientStack produce)
        {
            produce = IngredientStack.Empty;
            if (!Daytime(state)) return Garden.GardenResult.UnknownBed;
            Garden.GardenResult result = Garden.GardenRules.Harvest(state.Garden, state.Vigor, bedId, crop, vigor, out produce);
            if (result == Garden.GardenResult.Done) state.Storeroom.Add(produce);
            return result;
        }

        static bool Daytime(GameState state) => state != null && state.Phase == DayPhase.Daytime;

        static GameState Require(GameState state, DayPhase phase)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Phase != phase) throw new InvalidOperationException($"Needs {phase}, but it's {state.Phase}.");
            return state;
        }
    }
}
