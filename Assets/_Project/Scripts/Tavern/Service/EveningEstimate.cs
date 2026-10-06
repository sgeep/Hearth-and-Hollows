using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    /// <summary>How well an evening is played, for an estimate (4f Checkpoint D balance pass).</summary>
    [Serializable]
    public struct PlayStyle
    {
        [Range(0, 1)] public float cookScore;
        [Range(0, 1)] public float servingScore;
        [Range(0, 1)] public float chopScore;
        [Range(0, 1), Tooltip("Share of a patron's patience used before the food arrives.")]
        public float waitFraction;
        [Range(0, 1), Tooltip("0.5: dishes neither liked nor disliked.")]
        public float flavorMatch;

        public static PlayStyle Weak => new() { cookScore = 0.6f, servingScore = 0.6f, chopScore = 0.4f, waitFraction = 0.65f, flavorMatch = 0.5f };
        public static PlayStyle Competent => new() { cookScore = 0.82f, servingScore = 0.8f, chopScore = 0.7f, waitFraction = 0.4f, flavorMatch = 0.55f };
        public static PlayStyle Strong => new() { cookScore = 0.95f, servingScore = 0.95f, chopScore = 0.95f, waitFraction = 0.2f, flavorMatch = 0.6f };
    }

    /// <summary>What one estimated evening earned.</summary>
    public sealed class EveningResult
    {
        public int Covers;
        public int Gold;
        public int Tips;
        public int Renown;
        public int IngredientCost;
        public int RequestBonus;
        public readonly Dictionary<string, int> Dishes = new();
        public int Takings => Gold + Tips + RequestBonus;
        public int Profit => Takings - IngredientCost;
    }

    /// <summary>
    /// A deterministic estimate of one evening's service from real content (4f Checkpoint D): the patrons the evening admits
    /// order the menu's dishes in turn while the stock lasts, each cooked and served at the given play style, and paid,
    /// tipped and judged by the game's own rules (<see cref="DishScoring"/>, <see cref="ServiceEconomy"/>,
    /// <see cref="RecipeMatcher"/>). It doesn't simulate walking, queues or walkouts; it's for comparing kinds of evening.
    /// Pure logic.
    /// </summary>
    public static class EveningEstimate
    {
        /// <summary>Patrons one evening admits on average: arrivals until last orders.</summary>
        public static int ExpectedCovers(in ServiceSettings s)
        {
            float open = Mathf.Max(0f, s.lengthSeconds - s.lastOrdersSeconds);
            float gap = Mathf.Max(0.1f, (s.minArrivalGap + s.maxArrivalGap) * 0.5f);
            return Mathf.FloorToInt(open / gap) + 1;
        }

        /// <param name="ingredientCost">Gold already spent on the stock (market purchases); parts from the Hollows cost nothing here.</param>
        /// <param name="generosity">The patrons' average generosity.</param>
        /// <param name="requests">Special requests completed this evening (each pays <paramref name="request"/>'s bonus on its dish).</param>
        public static EveningResult Run(IReadOnlyList<RecipeDefinition> menu, Storeroom stock, int covers, PlayStyle play, in DishScoringSettings scoring,
            in ServiceEconomySettings economy, in StewPotSettings pot, float generosity = 1f, int ingredientCost = 0, int requests = 0,
            CustomerRequestSettings request = default)
        {
            var result = new EveningResult { IngredientCost = ingredientCost };
            if (menu == null || menu.Count == 0 || stock == null) return result;
            var helpings = new Dictionary<RecipeDefinition, (int left, CookedIngredients batch)>();
            float renownExact = 0f;
            int next = 0;
            for (int c = 0; c < covers; c++)
            {
                RecipeDefinition dish = null;
                CookedIngredients used = null;
                for (int tries = 0; tries < menu.Count && dish == null; tries++)
                {
                    RecipeDefinition r = menu[(next + tries) % menu.Count];
                    if (r == null) continue;
                    if (r.station == CookStation.StewPot)
                    {
                        helpings.TryGetValue(r, out var pan);
                        if (pan.left <= 0)
                        {
                            CookedIngredients batch = RecipeMatcher.TryTake(r, stock);
                            if (batch == null) continue;
                            pan = (Mathf.RoundToInt(Mathf.Lerp(pot.minHelpings, pot.maxHelpings, play.chopScore)), batch);
                        }
                        pan.left--;
                        helpings[r] = pan;
                        dish = r;
                        used = pan.batch;
                    }
                    else if ((used = RecipeMatcher.TryTake(r, stock)) != null) dish = r;
                }
                if (dish == null) break;
                next++;
                float minigame = DishScoring.MinigameScore(play.cookScore, play.servingScore, scoring);
                float quality = DishScoring.DishQuality(used.Used, minigame, scoring);
                float value = DishScoring.DishValue(dish.baseValue, quality);
                float satisfaction = ServiceEconomy.Satisfaction(quality, play.flavorMatch, play.waitFraction, economy);
                result.Covers++;
                result.Gold += ServiceEconomy.Payment(value);
                result.Tips += ServiceEconomy.Tip(value, satisfaction, generosity, economy);
                renownExact += ServiceEconomy.RenownExact(satisfaction, economy);
                if (result.Covers <= requests)
                {
                    result.RequestBonus += CustomerRequestRules.BonusGold(value, request);
                    result.Renown += request.bonusRenown;
                }
                result.Dishes.TryGetValue(dish.id, out int n);
                result.Dishes[dish.id] = n + 1;
            }
            result.Renown += Mathf.RoundToInt(renownExact);
            return result;
        }
    }
}
