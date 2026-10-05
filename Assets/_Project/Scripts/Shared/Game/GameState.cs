using System.Collections.Generic;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using UnityEngine;

namespace Hearthdelve.Shared.Game
{
    /// <summary>The delve meal's buff, eaten in the daytime and waiting for tonight's delve.</summary>
    public readonly struct MealBuff
    {
        /// <summary>A dish's buff is scaled by its quality, up to this multiple (Premium ingredients can beat a perfect Fine dish).</summary>
        public const float MaxQualityScale = 1.25f;

        public readonly MealBuffKind Kind;
        public readonly float Amount;
        /// <summary>The dish it came from (for display).</summary>
        public readonly string RecipeId;

        public MealBuff(MealBuffKind kind, float amount, string recipeId)
        {
            Kind = kind;
            Amount = Mathf.Max(0f, amount);
            RecipeId = recipeId;
        }

        public bool IsActive => Kind != MealBuffKind.None && Amount > 0f;

        public static MealBuff None => default;

        /// <summary>The buff from eating <paramref name="recipe"/> cooked to <paramref name="dishQuality"/> (≈1 for a perfect Fine dish).</summary>
        public static MealBuff FromDish(RecipeDefinition recipe, float dishQuality) =>
            recipe == null ? None : new MealBuff(recipe.mealBuff.kind, recipe.mealBuff.amount * Mathf.Clamp(dishQuality, 0f, MaxQualityScale), recipe.id);
    }

    /// <summary>What tonight's delve starts with, from upgrades and the delve meal.</summary>
    public readonly struct DelveLoadout
    {
        /// <summary>Essence drain can't be slowed below this fraction of normal.</summary>
        public const float MinDrainMultiplier = 0.1f;

        public readonly int ExtraSatchelSlots;
        public readonly float MaxEssenceBonus;
        /// <summary>1 = normal drain.</summary>
        public readonly float DrainMultiplier;

        public DelveLoadout(int extraSatchelSlots, float maxEssenceBonus, float drainMultiplier)
        {
            ExtraSatchelSlots = extraSatchelSlots;
            MaxEssenceBonus = maxEssenceBonus;
            DrainMultiplier = drainMultiplier;
        }

        public static DelveLoadout None => new(0, 0f, 1f);

        public static DelveLoadout From(UpgradeEffects upgrades, MealBuff meal) => new(
            upgrades.SatchelSlots,
            upgrades.MaxEssence + (meal.Kind == MealBuffKind.MaxEssence ? meal.Amount : 0f),
            meal.Kind == MealBuffKind.SlowerDrain ? Mathf.Clamp(1f - meal.Amount, MinDrainMultiplier, 1f) : 1f);
    }

    public enum DelveOutcome
    {
        None,
        /// <summary>Left through the exit with the whole satchel.</summary>
        Extracted,
        /// <summary>Essence ran out: only the Lockbox stack came back.</summary>
        Died,
    }

    /// <summary>Today's numbers, for the debug end-of-day summary (GDD §7.3 balance check).</summary>
    public sealed class DaySummary
    {
        public DelveOutcome Delve;
        public int PartsBroughtBack;
        public int PartsLost;
        public int DishesServed;
        public int Gold;
        public int Tips;
        public int Walkouts;
        public int RenownChange;
        /// <summary>The tavern stayed shut this evening.</summary>
        public bool KeptShut;
        /// <summary>This evening was played since the game was loaded (the save doesn't keep the day's story).</summary>
        public bool EveningRecorded;
        /// <summary>Run Gold the delve brought home, and run Gold it lost.</summary>
        public int DelveGold;
        public int DelveGoldLost;

        /// <summary>Banked today: service takings and the delve's Gold.</summary>
        public int Earned => Gold + Tips + DelveGold;

        public void Reset()
        {
            Delve = DelveOutcome.None;
            PartsBroughtBack = PartsLost = DishesServed = Gold = Tips = Walkouts = RenownChange = DelveGold = DelveGoldLost = 0;
            KeptShut = EveningRecorded = false;
        }
    }

    /// <summary>
    /// Everything that persists between days (and goes in the save): the day and phase, gold,
    /// renown, the storeroom, upgrade levels, and a delve meal not yet used. Pure data;
    /// <see cref="DayRules"/> changes it.
    /// </summary>
    public sealed class GameState
    {
        readonly Dictionary<string, int> m_UpgradeLevels = new();
        readonly Dictionary<string, int> m_BossClears = new();

        public GameState(int day = 1, DayPhase phase = DayPhase.Daytime) => Cycle = new DayCycle(day, phase);

        public DayCycle Cycle { get; }
        public int Day => Cycle.Day;
        public DayPhase Phase => Cycle.Phase;
        public int Gold { get; internal set; }
        public int Renown { get; internal set; }
        public Storeroom Storeroom { get; } = new();
        public IReadOnlyDictionary<string, int> UpgradeLevels => m_UpgradeLevels;
        public MealBuff Meal { get; internal set; }
        public DaySummary Today { get; } = new();

        public int UpgradeLevel(string id) => id != null && m_UpgradeLevels.TryGetValue(id, out int level) ? level : 0;

        /// <summary>Each boss defeated, by stable id, and how many times (4e): story reactions, first-clear rewards, 4f's trophies.</summary>
        public IReadOnlyDictionary<string, int> BossClears => m_BossClears;
        public int TimesDefeated(string bossId) => bossId != null && m_BossClears.TryGetValue(bossId, out int n) ? n : 0;

        internal void SetBossClears(string id, int clears)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (clears <= 0) m_BossClears.Remove(id);
            else m_BossClears[id] = clears;
        }

        internal void SetUpgradeLevel(string id, int level)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (level <= 0) m_UpgradeLevels.Remove(id);
            else m_UpgradeLevels[id] = level;
        }

        /// <summary>Debug: add gold (negative removes, never below zero).</summary>
        public void AddGold(int amount) => Gold = Mathf.Max(0, Gold + amount);
    }
}
