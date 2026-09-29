using System;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Tavern.Customers
{
    /// <summary>Timing and taste of one customer type (GDD §6.3).</summary>
    [Serializable]
    public struct CustomerTraits
    {
        [Min(0.1f)] public float walkSpeed;
        [Min(0), Tooltip("How long they'll wait by the door for a free seat.")]
        public float seatPatience;
        [Min(0), Tooltip("How long they'll wait for food after ordering.")]
        public float orderPatience;
        [Min(0), Tooltip("Time spent reading the menu after sitting down.")]
        public float orderDelay;
        [Min(0)] public float eatSeconds;
        public FlavorTags liked;
        public FlavorTags disliked;
        public bool hasFavoriteStation;
        public CookStation favoriteStation;
        [Range(0, 1)] public float favoriteStationBonus;
        [Min(0), Tooltip("Multiplies tips.")]
        public float generosity;

        public static CustomerTraits Default => new()
        {
            walkSpeed = 2.5f,
            seatPatience = 15f,
            orderPatience = 45f,
            orderDelay = 1.5f,
            eatSeconds = 5f,
            generosity = 1f,
            favoriteStationBonus = 0.2f,
        };
    }

    [CreateAssetMenu(menuName = "Hearthdelve/Customer Profile", fileName = "Customer_")]
    public sealed class CustomerProfile : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public CustomerTraits traits = CustomerTraits.Default;
        [Min(0), Tooltip("Relative chance this type walks in.")]
        public float spawnWeight = 1f;
        public Color placeholderColor = Color.white;
    }
}
