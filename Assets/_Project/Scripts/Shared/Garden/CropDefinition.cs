using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Shared.Garden
{
    /// <summary>
    /// A crop the garden can grow (4h Checkpoint B): what it produces, in how many days, how much, and how it looks as it
    /// grows. Only what the starter garden needs; seasons, water, soil and the rest are not data until they're game.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Garden/Crop", fileName = "Crop_")]
    public sealed class CropDefinition : ScriptableObject
    {
        [Tooltip("Stable id, saved in the garden's beds.")]
        public string id;
        [Tooltip("UI table key of the crop's name.")]
        public string nameKey;
        [Tooltip("The ingredient a harvest gives (an existing one: it goes into the storeroom like any other).")]
        public IngredientDefinition produce;
        [Min(1), Tooltip("Nights of growth from planting to ready.")]
        public int growthDays = 2;
        [Min(1), Tooltip("How many of the ingredient a harvest gives.")]
        public int yield = 3;
        [Range(0f, 1f), Tooltip("Tended on at least this share of its growing days: the harvest is Fine (otherwise Standard).")]
        public float fineTendedShare = 0.5f;

        [Header("Looks (one plant per tile of the bed)")]
        public Sprite seeds;
        public Sprite sprout;
        public Sprite growing;
        public Sprite ready;
        [Tooltip("The crop on the planting choice.")]
        public Sprite icon;
    }
}
