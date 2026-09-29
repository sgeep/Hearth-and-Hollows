using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Shared.Recipes
{
    /// <summary>Where a dish is cooked. Serving is not a cooking station.</summary>
    public enum CookStation
    {
        Grill,
        Tap,
    }

    public enum SlotMatch
    {
        /// <summary>A specific ingredient (e.g. Rat Haunch).</summary>
        Ingredient,
        /// <summary>Any ingredient in these categories (e.g. any Meat or Offal).</summary>
        Category,
    }

    /// <summary>One ingredient requirement of a recipe (GDD §5.3).</summary>
    [Serializable]
    public sealed class RecipeSlot
    {
        public SlotMatch match = SlotMatch.Ingredient;
        public IngredientDefinition ingredient;
        public IngredientCategory categories;
        [Min(1)] public int count = 1;
        [Tooltip("Used if in stock; adds its flavors to the dish.")]
        public bool optional;

        /// <summary>Inedible parts never go into a dish.</summary>
        public bool Accepts(IngredientItem item)
        {
            if (!item.IsValid || item.Prep == PrepState.Inedible) return false;
            return match == SlotMatch.Ingredient
                ? ReferenceEquals(item.Definition, ingredient)
                : (item.Definition.category & categories) != 0;
        }
    }

    [CreateAssetMenu(menuName = "Hearthdelve/Recipe Definition", fileName = "Recipe_")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public CookStation station = CookStation.Grill;
        public List<RecipeSlot> slots = new();
        [Min(0), Tooltip("Gold value of a perfect dish before quality, freshness, and minigame multipliers.")]
        public int baseValue = 10;
        public FlavorTags flavors;
        public Sprite icon;
        [Tooltip("Tint for placeholder art until a real icon exists.")]
        public Color placeholderColor = Color.white;
    }
}
