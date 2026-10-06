using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using UnityEngine;

namespace Hearthdelve.Shared.Economy
{
    [Serializable]
    public struct DishScoringSettings
    {
        [Header("Ingredient quality multipliers")]
        public float poor;
        public float standard;
        public float fine;
        public float premium;

        [Header("Freshness")]
        [Range(0, 1), Tooltip("Multiplier for completely stale ingredients (freshness 0). Fully fresh = 1.")]
        public float freshnessAtZero;

        [Header("Serving")]
        [Range(0, 1), Tooltip("Multiplier for the worst successful delivery (slow, spilled). A perfect delivery = 1.")]
        public float servingAtZero;

        public static DishScoringSettings Default => new()
        {
            poor = 0.6f, standard = 0.8f, fine = 1.0f, premium = 1.25f,
            freshnessAtZero = 0.5f,
            servingAtZero = 0.6f,
        };
    }

    [Serializable]
    public struct ServiceEconomySettings
    {
        [Header("Satisfaction weights (normalised)")]
        [Min(0)] public float dishWeight;
        [Min(0)] public float flavorWeight;
        [Min(0)] public float waitWeight;

        [Header("Tips")]
        [Range(0, 1), Tooltip("No tip below this satisfaction.")]
        public float tipThreshold;
        [Min(0), Tooltip("Tip at full satisfaction, as a fraction of the dish value (scaled by customer generosity).")]
        public float maxTipFraction;

        [Header("Renown")]
        [Range(0, 1), Tooltip("Satisfaction that earns zero renown.")]
        public float renownNeutral;
        [Min(0), Tooltip("Renown per point of satisfaction above/below neutral.")]
        public float renownScale;
        [Tooltip("Renown change when a customer runs out of patience and walks out.")]
        public int walkoutRenown;
        [Tooltip("Renown change when a customer leaves because everything they'd order is sold out. Smaller than a walkout.")]
        public int soldOutRenown;

        [Header("Flavor preferences")]
        [Range(0, 1), Tooltip("Flavor match gained per liked flavor in a dish (neutral dish = 0.5).")]
        public float likedFlavorBonus;
        [Range(0, 1), Tooltip("Flavor match lost per disliked flavor in a dish.")]
        public float dislikedFlavorPenalty;
        [Min(0), Tooltip("Base chance weight of ordering any available dish; higher = less picky ordering.")]
        public float baseOrderWeight;

        public static ServiceEconomySettings Default => new()
        {
            dishWeight = 0.6f, flavorWeight = 0.25f, waitWeight = 0.15f,
            tipThreshold = 0.5f, maxTipFraction = 0.5f,
            renownNeutral = 0.45f, renownScale = 4f,
            walkoutRenown = -3, soldOutRenown = -1,
            likedFlavorBonus = 0.25f, dislikedFlavorPenalty = 0.3f, baseOrderWeight = 0.1f,
        };
    }

    /// <summary>
    /// Dish score = recipe value × ingredient quality × freshness × minigame score (GDD §5.3).
    /// Pure logic.
    /// </summary>
    public static class DishScoring
    {
        public static float QualityMultiplier(Quality quality, in DishScoringSettings s) => quality switch
        {
            Quality.Poor => s.poor,
            Quality.Standard => s.standard,
            Quality.Fine => s.fine,
            _ => s.premium,
        };

        public static float FreshnessMultiplier(float freshness, in DishScoringSettings s) =>
            Mathf.Lerp(s.freshnessAtZero, 1f, Freshness.Clamp(freshness));

        /// <summary>Count-weighted average quality and freshness multipliers of the ingredients used.</summary>
        public static (float quality, float freshness) IngredientMultipliers(IReadOnlyList<IngredientStack> used, in DishScoringSettings s)
        {
            float q = 0f, f = 0f;
            int n = 0;
            if (used != null)
            {
                foreach (var stack in used)
                {
                    if (stack.IsEmpty) continue;
                    q += QualityMultiplier(stack.Item.Quality, s) * stack.Count;
                    f += FreshnessMultiplier(stack.Freshness, s) * stack.Count;
                    n += stack.Count;
                }
            }
            return n == 0 ? (1f, 1f) : (q / n, f / n);
        }

        /// <summary>Cooking score × serving factor. A dropped plate never reaches here.</summary>
        public static float MinigameScore(float cookScore, float servingScore, in DishScoringSettings s) =>
            Mathf.Clamp01(cookScore) * Mathf.Lerp(s.servingAtZero, 1f, Mathf.Clamp01(servingScore));

        /// <summary>Dish quality: ingredient quality × freshness × minigame score (1 ≈ a perfect Fine dish).</summary>
        public static float DishQuality(IReadOnlyList<IngredientStack> used, float minigameScore, in DishScoringSettings s)
        {
            var (quality, freshness) = IngredientMultipliers(used, s);
            return quality * freshness * Mathf.Clamp01(minigameScore);
        }

        /// <summary>Gold value: recipe value × dish quality.</summary>
        public static float DishValue(int recipeValue, float dishQuality) => Mathf.Max(0f, recipeValue * dishQuality);
    }

    /// <summary>Satisfaction, payment, tips, and renown for one customer. Pure logic.</summary>
    public static class ServiceEconomy
    {
        /// <param name="dishQuality">From <see cref="DishScoring.DishQuality"/>; capped at 1 for satisfaction.</param>
        /// <param name="flavorMatch">0–1, how well the dish suits the customer.</param>
        /// <param name="waitFraction">0–1, share of their patience used up before the food arrived.</param>
        public static float Satisfaction(float dishQuality, float flavorMatch, float waitFraction, in ServiceEconomySettings s)
        {
            float total = s.dishWeight + s.flavorWeight + s.waitWeight;
            if (total <= 0f) return 0f;
            float score = s.dishWeight * Mathf.Clamp01(dishQuality)
                        + s.flavorWeight * Mathf.Clamp01(flavorMatch)
                        + s.waitWeight * (1f - Mathf.Clamp01(waitFraction));
            return Mathf.Clamp01(score / total);
        }

        public static int Payment(float dishValue) => Mathf.Max(0, Mathf.RoundToInt(dishValue));

        public static int Tip(float dishValue, float satisfaction, float generosity, in ServiceEconomySettings s)
        {
            if (satisfaction < s.tipThreshold) return 0;
            float t = Mathf.InverseLerp(s.tipThreshold, 1f, satisfaction);
            return Mathf.Max(0, Mathf.RoundToInt(dishValue * s.maxTipFraction * t * Mathf.Max(0f, generosity)));
        }

        public static int Renown(float satisfaction, in ServiceEconomySettings s) =>
            Mathf.RoundToInt(RenownExact(satisfaction, s));

        /// <summary>
        /// Renown from one patron before rounding (4f Checkpoint D): an evening adds these up and rounds the total, so a
        /// run of decent-but-not-great dishes still counts instead of each rounding to nothing.
        /// </summary>
        public static float RenownExact(float satisfaction, in ServiceEconomySettings s) =>
            (Mathf.Clamp01(satisfaction) - s.renownNeutral) * s.renownScale;
    }
}
