using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;

namespace Hearthdelve.Shared.Recipes
{
    /// <summary>The ingredients that went into one dish.</summary>
    public sealed class CookedIngredients
    {
        public readonly List<IngredientStack> Used = new();
        public FlavorTags Flavors;

        public int TotalCount
        {
            get
            {
                int n = 0;
                foreach (var s in Used) n += s.Count;
                return n;
            }
        }
    }

    /// <summary>Recipe matching against the storeroom (GDD §10.4 RecipeSystem). Pure logic.</summary>
    public static class RecipeMatcher
    {
        /// <summary>
        /// Takes the ingredients for one serving: all required slots or nothing, plus any optional
        /// slots in stock. Returns null (and takes nothing) if a required slot can't be filled.
        /// </summary>
        public static CookedIngredients TryTake(RecipeDefinition recipe, Storeroom storeroom)
        {
            if (recipe == null || storeroom == null || !HasRequired(recipe, storeroom)) return null;

            var cooked = new CookedIngredients { Flavors = recipe.flavors };
            foreach (var slot in recipe.slots)
            {
                if (slot == null) continue;
                var taken = storeroom.Take(slot.Accepts, slot.count);
                if (taken == null) continue; // optional slot not in stock (required ones were checked)
                cooked.Used.AddRange(taken);
                if (slot.optional)
                    foreach (var s in taken) cooked.Flavors |= s.Item.Definition.flavors;
            }
            return cooked;
        }

        public static bool CanCook(RecipeDefinition recipe, Storeroom storeroom) =>
            recipe != null && storeroom != null && HasRequired(recipe, storeroom);

        /// <summary>How many servings the current stock can make (up to <paramref name="cap"/>).</summary>
        public static int ServingsAvailable(RecipeDefinition recipe, Storeroom storeroom, int cap = 99)
        {
            if (recipe == null || storeroom == null) return 0;
            var trial = storeroom.Clone();
            int servings = 0;
            while (servings < cap && TakeRequired(recipe, trial)) servings++;
            return servings;
        }

        static bool HasRequired(RecipeDefinition recipe, Storeroom storeroom) => TakeRequired(recipe, storeroom.Clone());

        /// <summary>Takes only the required slots, in order, from <paramref name="stock"/>.</summary>
        static bool TakeRequired(RecipeDefinition recipe, Storeroom stock)
        {
            bool any = false;
            foreach (var slot in recipe.slots)
            {
                if (slot == null || slot.optional) continue;
                if (stock.Take(slot.Accepts, slot.count) == null) return false;
                any = true;
            }
            return any;
        }
    }
}
