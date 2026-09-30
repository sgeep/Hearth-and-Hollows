using System;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Shared.Inventory
{
    /// <summary>How fast parts lose freshness (GDD §4.4, §5.1).</summary>
    [Serializable]
    public struct FreshnessSettings
    {
        [Min(0), Tooltip("Freshness lost per minute while carried in the satchel (1 = fully fresh).")]
        public float dungeonLossPerMinute;
        [Min(0), Tooltip("Freshness every storeroom stack loses each night.")]
        public float overnightLoss;
        [Range(0, 1), Tooltip("Chilled (ice-killed) parts lose freshness at this fraction of the normal rate.")]
        public float chilledMultiplier;

        public static FreshnessSettings Default => new()
        {
            dungeonLossPerMinute = 0.08f,
            overnightLoss = 0.08f,
            chilledMultiplier = 0.5f,
        };

        /// <summary>The loss for this item: Chilled parts keep better.</summary>
        public float LossFor(IngredientItem item, float baseLoss) =>
            item.Prep == PrepState.Chilled ? baseLoss * chilledMultiplier : baseLoss;
    }

    [CreateAssetMenu(menuName = "Hearthdelve/Config/Freshness", fileName = "FreshnessConfig")]
    public sealed class FreshnessConfig : ScriptableObject
    {
        public FreshnessSettings freshness = FreshnessSettings.Default;
    }
}
