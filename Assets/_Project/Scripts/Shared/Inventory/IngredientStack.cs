using System;
using Hearthdelve.Shared.Ingredients;

namespace Hearthdelve.Shared.Inventory
{
    /// <summary>
    /// A count of identical parts plus their freshness (0–1). Used by the satchel, pickups,
    /// the Lockbox, and the storeroom. Empty when <see cref="Count"/> is 0.
    /// </summary>
    public readonly struct IngredientStack
    {
        public readonly IngredientItem Item;
        public readonly int Count;
        /// <summary>1 = just harvested, 0 = about to spoil. Per stack (CLAUDE.md).</summary>
        public readonly float Freshness;

        public IngredientStack(IngredientItem item, int count, float freshness = 1f)
        {
            Item = count > 0 ? item : default;
            Count = Math.Max(0, count);
            Freshness = count > 0 ? Inventory.Freshness.Clamp(freshness) : 0f;
        }

        public bool IsEmpty => Count == 0;
        public static IngredientStack Empty => default;

        public IngredientStack WithCount(int count) => new(Item, count, Freshness);
        public IngredientStack WithFreshness(float freshness) => new(Item, Count, freshness);
    }

    /// <summary>Freshness rules shared by every inventory.</summary>
    public static class Freshness
    {
        public const float Max = 1f;

        public static float Clamp(float value) => Math.Clamp(value, 0f, Max);

        /// <summary>Freshness after merging two stacks: the count-weighted average.</summary>
        public static float Merge(int countA, float freshnessA, int countB, float freshnessB)
        {
            int total = countA + countB;
            if (total <= 0) return 0f;
            return Clamp((countA * Clamp(freshnessA) + countB * Clamp(freshnessB)) / total);
        }

        /// <summary>
        /// Order stock is used in: least fresh first; on a tie, lower quality first.
        /// Negative when <paramref name="a"/> should be used before <paramref name="b"/>.
        /// </summary>
        public static int UseOrder(in IngredientStack a, in IngredientStack b)
        {
            int byFreshness = a.Freshness.CompareTo(b.Freshness);
            if (byFreshness != 0) return byFreshness;
            return ((int)a.Item.Quality).CompareTo((int)b.Item.Quality);
        }

        // Overnight storeroom decay (slowed by preservation upgrades) is designed for but not
        // implemented yet; it will be a pure function here that returns the decayed freshness.
    }
}
