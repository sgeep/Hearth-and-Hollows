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
            IEnumerable<string> curiosKept = null, IEnumerable<string> curiosLost = null)
        {
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
        public static DelveReport Extraction(Satchel satchel, int runGold = 0, IEnumerable<string> bosses = null, IEnumerable<string> curios = null)
        {
            var haul = new List<IngredientStack>();
            if (satchel != null)
                foreach (var slot in satchel.Slots)
                    if (!slot.IsEmpty) haul.Add(slot);
            return new DelveReport(DelveOutcome.Extracted, haul, 0, Math.Max(0, runGold), 0, bosses, curios);
        }

        /// <summary>Died: only the Lockbox stack comes home; the run's Gold is lost.</summary>
        public static DelveReport Death(DeathPenaltyResult result, int runGold = 0, IEnumerable<string> bosses = null, IEnumerable<string> curios = null)
        {
            var haul = new List<IngredientStack>();
            if (result.KeptSomething) haul.Add(result.Kept);
            return new DelveReport(DelveOutcome.Died, haul, result.ItemsLost, 0, Math.Max(0, runGold), bosses, null, curios);
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
        /// Back from the night's delve: the haul goes into the storeroom, the run's Gold into the purse, the delve meal is
        /// used up, and it's night.
        /// </summary>
        public static void CompleteDelve(GameState state, DelveReport report, Func<string, Customization.FurnitureDefinition> furniture = null,
            IEnumerable<BossTrophy> trophies = null)
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
        public static bool Buy(GameState state, Inventory.SupplySource source, Inventory.SupplyOffer offer)
        {
            Require(state, DayPhase.Daytime);
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

        /// <summary>Overnight: storeroom stock loses a little freshness, and a new day begins.</summary>
        public static void Sleep(GameState state, in FreshnessSettings freshness)
        {
            Require(state, DayPhase.Night);
            state.Storeroom.Decay(freshness, freshness.overnightLoss);
            state.Today.Reset();
            state.Cycle.AdvanceTo(DayPhase.Daytime);
        }

        static GameState Require(GameState state, DayPhase phase)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Phase != phase) throw new InvalidOperationException($"Needs {phase}, but it's {state.Phase}.");
            return state;
        }
    }
}
