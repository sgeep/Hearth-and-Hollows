using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using UnityEngine;

namespace Hearthdelve.Tavern.Customers
{
    /// <summary>How well a dish suits a customer, and what they order. Pure logic.</summary>
    public static class Preferences
    {
        public const float Neutral = 0.5f;

        /// <summary>0–1: 0.5 for a neutral dish, up for liked flavors / favourite station, down for disliked.</summary>
        public static float FlavorMatch(FlavorTags dishFlavors, CookStation station, in CustomerTraits traits, in ServiceEconomySettings s)
        {
            float score = Neutral
                          + s.likedFlavorBonus * CountBits(dishFlavors & traits.liked)
                          - s.dislikedFlavorPenalty * CountBits(dishFlavors & traits.disliked);
            if (traits.hasFavoriteStation && station == traits.favoriteStation) score += traits.favoriteStationBonus;
            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Picks from the dishes still available, weighted toward ones the customer likes.
        /// Returns null if nothing is available (everything on the menu is sold out).
        /// </summary>
        public static RecipeDefinition ChooseOrder(IReadOnlyList<RecipeDefinition> available, in CustomerTraits traits,
            in ServiceEconomySettings s, IRandom random)
        {
            if (available == null || available.Count == 0) return null;
            float total = 0f;
            var weights = new float[available.Count];
            for (int i = 0; i < available.Count; i++)
            {
                // Anyone might order anything, but liked dishes are far more likely.
                float match = FlavorMatch(available[i].flavors, available[i].station, traits, s);
                weights[i] = s.baseOrderWeight + match * match;
                total += weights[i];
            }
            float pick = random.Value() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0f) return available[i];
            }
            return available[available.Count - 1];
        }

        static int CountBits(FlavorTags tags)
        {
            int n = 0;
            for (int v = (int)tags; v != 0; v &= v - 1) n++;
            return n;
        }
    }
}
