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
        DelveReport(DelveOutcome outcome, List<IngredientStack> haul, int partsLost)
        {
            Outcome = outcome;
            Haul = haul;
            PartsLost = partsLost;
        }

        public DelveOutcome Outcome { get; }
        public IReadOnlyList<IngredientStack> Haul { get; }
        public int PartsLost { get; }

        public int PartsBroughtBack
        {
            get
            {
                int n = 0;
                foreach (var s in Haul) n += s.Count;
                return n;
            }
        }

        /// <summary>Left through the exit: everything in the satchel comes home (freshness as it is now).</summary>
        public static DelveReport Extraction(Satchel satchel)
        {
            var haul = new List<IngredientStack>();
            if (satchel != null)
                foreach (var slot in satchel.Slots)
                    if (!slot.IsEmpty) haul.Add(slot);
            return new DelveReport(DelveOutcome.Extracted, haul, 0);
        }

        /// <summary>Died: only the Lockbox stack comes home.</summary>
        public static DelveReport Death(DeathPenaltyResult result)
        {
            var haul = new List<IngredientStack>();
            if (result.KeptSomething) haul.Add(result.Kept);
            return new DelveReport(DelveOutcome.Died, haul, result.ItemsLost);
        }

        /// <summary>Debug skip: nothing brought back.</summary>
        public static DelveReport Empty => new(DelveOutcome.None, new List<IngredientStack>(), 0);
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
        public static void StartDelve(GameState state) => Require(state, DayPhase.Morning).Cycle.AdvanceTo(DayPhase.Delve);

        /// <summary>The haul goes into the storeroom, the breakfast buff is used up, and it's evening.</summary>
        public static void CompleteDelve(GameState state, DelveReport report)
        {
            Require(state, DayPhase.Delve);
            if (report == null) throw new ArgumentNullException(nameof(report));
            state.Storeroom.AddRange(report.Haul);
            state.Today.Delve = report.Outcome;
            state.Today.PartsBroughtBack += report.PartsBroughtBack;
            state.Today.PartsLost += report.PartsLost;
            state.Meal = MealBuff.None;
            state.Cycle.AdvanceTo(DayPhase.Evening);
        }

        /// <summary>Service takings (payments and tips) are banked; renown changes; it's night.</summary>
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
            state.Cycle.AdvanceTo(DayPhase.Night);
        }

        /// <summary>Closed without serving (nothing to cook, or by choice).</summary>
        public static void SkipService(GameState state) => Require(state, DayPhase.Evening).Cycle.AdvanceTo(DayPhase.Night);

        /// <summary>Eat breakfast: one buff per morning.</summary>
        public static bool EatMeal(GameState state, MealBuff meal)
        {
            Require(state, DayPhase.Morning);
            if (state.Meal.IsActive || !meal.IsActive) return false;
            state.Meal = meal;
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
            state.Cycle.AdvanceTo(DayPhase.Morning);
        }

        static GameState Require(GameState state, DayPhase phase)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Phase != phase) throw new InvalidOperationException($"Needs {phase}, but it's {state.Phase}.");
            return state;
        }
    }
}
