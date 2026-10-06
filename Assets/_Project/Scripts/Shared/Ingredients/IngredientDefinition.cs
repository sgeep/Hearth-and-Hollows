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
        [Tooltip("Where it usually comes from (4f). Recipes never depend on this.")]
        public IngredientSource source = IngredientSource.Hollows;
        [Tooltip("Can it be broken down at the Butcher Block, and into what (4f, D17)? Empty: it can't.")]
        public ButcheringSettings butchering = new();

        public bool Butcherable => butchering != null && butchering.cut != null && butchering.maxCuts > 0;
    }

    /// <summary>
    /// A part's breakdown at the Butcher Block (D17): into a separate cut ingredient, how many from one part by how well it
    /// was cut. Cuts keep the part's quality and freshness. Larger parts later join with data alone.
    /// </summary>
    [System.Serializable]
    public sealed class ButcheringSettings
    {
        [Tooltip("The cut ingredient (\"spider leg cuts\").")]
        public IngredientDefinition cut;
        [Min(0), Tooltip("Cuts from a ragged job.")]
        public int minCuts = 1;
        [Min(0), Tooltip("Cuts from a clean one.")]
        public int maxCuts = 3;
        [Tooltip("Score (0–1) needed for each cut above the minimum, in order: e.g. 0.5 for the second, 0.85 for the third.")]
        public float[] thresholds = { 0.5f, 0.85f };
    }
}
