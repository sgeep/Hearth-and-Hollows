using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Shared.Ingredients
{
    /// <summary>Static data for one ingredient (GDD §5.1). Quality and prep state live on the item, not here.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Ingredient Definition", fileName = "Ingredient_")]
    public sealed class IngredientDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves and recipes. Never change after shipping.")]
        public string id;
        public LocalizedString displayName;
        public IngredientCategory category = IngredientCategory.Meat;
        public FlavorTags flavors;
        public Rarity rarity;
        [Min(0)] public int baseValue = 5;
        public Sprite icon;
        [Tooltip("Tint used for placeholder art until a real icon exists.")]
        public Color placeholderColor = Color.white;
    }
}
