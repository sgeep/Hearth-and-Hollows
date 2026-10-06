using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Shared.Inventory
{
    /// <summary>Something a supply source sells, and for how much gold.</summary>
    [Serializable]
    public sealed class SupplyOffer
    {
        public IngredientDefinition ingredient;
        [Min(0)] public int price = 2;
        [Min(1), Tooltip("How many one purchase gives.")]
        public int bundle = 1;
    }

    /// <summary>
    /// A place the storeroom is stocked from with gold (4f Checkpoint C, D19). Today the Brackenford market list in the
    /// daytime panel; later the village shop, and farming, ranching and fishing feed the same storeroom through more
    /// sources. Bought food is Standard quality and fresh, and ages like everything else.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Supply Source", fileName = "Supply_")]
    public sealed class SupplySource : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public IngredientSource source = IngredientSource.Market;
        public List<SupplyOffer> offers = new();
        [Tooltip("Quality of everything bought here.")]
        public Quality quality = Quality.Standard;
    }

    /// <summary>Buying from a supply source (pure).</summary>
    public static class SupplyRules
    {
        /// <summary>What one purchase of <paramref name="offer"/> delivers: Standard (the source's quality), fully fresh.</summary>
        public static IngredientStack Delivery(SupplySource source, SupplyOffer offer) =>
            new(new IngredientItem(offer.ingredient, source != null ? source.quality : Quality.Standard), Math.Max(1, offer.bundle), 1f);

        public static bool CanAfford(int gold, SupplyOffer offer) => offer != null && offer.ingredient != null && gold >= offer.price;
    }

    /// <summary>
    /// The Butcher Block's breakdown (D17, pure): how many cuts a part gives for a score, and the cuts themselves, which keep
    /// the part's quality and freshness.
    /// </summary>
    public static class ButcherRules
    {
        /// <summary>Cuts from one part for a minigame score (0–1): the minimum, plus one per threshold met, up to the maximum.</summary>
        public static int Yield(ButcheringSettings settings, float score)
        {
            if (settings == null) return 0;
            int cuts = settings.minCuts;
            if (settings.thresholds != null)
                foreach (float threshold in settings.thresholds)
                    if (score >= threshold) cuts++;
            return Mathf.Clamp(cuts, 0, Math.Max(settings.minCuts, settings.maxCuts));
        }

        /// <summary>
        /// Breaks down one part from the storeroom (the exact item: its ingredient, quality and state): the part is taken and
        /// its cuts go back in, with its quality and freshness. Returns the cuts, or an empty stack (nothing taken) when the
        /// part isn't there or can't be butchered.
        /// </summary>
        public static IngredientStack Butcher(Storeroom storeroom, IngredientItem part, float score)
        {
            if (storeroom == null || !part.IsValid || !part.Definition.Butcherable) return default;
            List<IngredientStack> taken = storeroom.Take(item => item == part, 1);
            if (taken == null || taken.Count == 0) return default;
            IngredientStack cuts = Cuts(part.Definition, part.Quality, taken[0].Freshness, score);
            if (!cuts.IsEmpty) storeroom.Add(cuts);
            return cuts;
        }

        /// <summary>The cuts as a stack: the part's quality, its freshness, raw.</summary>
        public static IngredientStack Cuts(IngredientDefinition part, Quality quality, float freshness, float score) =>
            part == null || !part.Butcherable
                ? default
                : new IngredientStack(new IngredientItem(part.butchering.cut, quality), Yield(part.butchering, score), freshness);
    }
}
