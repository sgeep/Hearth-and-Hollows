using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;

namespace Hearthdelve.Tavern.Service
{
    /// <summary>
    /// Debug action (Phase 2 has no dungeon link yet): fills the storeroom with the given
    /// ingredients at mixed quality and freshness.
    /// </summary>
    public static class DebugStockFiller
    {
        public const float MinFreshness = 0.25f;

        public static void Fill(Storeroom storeroom, IReadOnlyList<IngredientDefinition> ingredients, IRandom random,
            int stacksPerIngredient = 3, int minCount = 2, int maxCount = 5)
        {
            var stacks = new List<IngredientStack>();
            foreach (var def in ingredients)
            {
                if (def == null) continue;
                for (int i = 0; i < stacksPerIngredient; i++)
                {
                    // Cycle through qualities so every fill is visibly mixed, then randomise.
                    var quality = (Quality)((i + random.Range(0, 3)) % 4);
                    float freshness = MinFreshness + random.Value() * (1f - MinFreshness);
                    stacks.Add(new IngredientStack(new IngredientItem(def, quality), random.Range(minCount, maxCount), freshness));
                }
            }
            storeroom.AddRange(stacks);
        }
    }
}
