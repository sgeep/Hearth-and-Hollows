using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>One catalogue tier (D14): the Renown that opens it, and the words that announce it.</summary>
    [Serializable]
    public sealed class CatalogTier
    {
        [Min(0)] public int renown;
        [Tooltip("Localization key (UI table) of its name in the catalogue.")]
        public string nameKey;
        [Tooltip("Localization key (UI table) of the line on the Night screen when it opens.")]
        public string announceKey;
    }

    /// <summary>Why a piece or finish can't be bought now.</summary>
    public enum PurchaseProblem
    {
        None,
        /// <summary>Its catalogue tier needs more Renown.</summary>
        Locked,
        /// <summary>Not sold (found in the Hollows, a trophy, story, or the starting furniture).</summary>
        NotForSale,
        /// <summary>A unique piece already owned, or a finish already owned.</summary>
        AlreadyOwned,
        NotEnoughGold,
    }

    /// <summary>
    /// Buying and selling furniture and finishes (D7, D12, D14), against the game's Gold and Renown. Buying one gives one
    /// copy, into storage; selling takes one from storage for its sell-back price; only bought pieces sell. Delivery is
    /// immediate (4f). Pure logic, EditMode-tested.
    /// </summary>
    public static class FurnitureShop
    {
        /// <summary>The highest tier open at this Renown (0 with no tiers).</summary>
        public static int OpenTier(int renown, IReadOnlyList<int> thresholds)
        {
            int open = 0;
            if (thresholds == null) return open;
            for (int i = 0; i < thresholds.Count; i++)
                if (renown >= thresholds[i]) open = i;
            return open;
        }

        public static bool IsOpen(int tier, int renown, IReadOnlyList<int> thresholds) => tier <= OpenTier(renown, thresholds);

        /// <summary>The Renown a tier needs (0 for tier 0 or an unknown tier).</summary>
        public static int RenownFor(int tier, IReadOnlyList<int> thresholds) =>
            thresholds != null && tier >= 0 && tier < thresholds.Count ? thresholds[tier] : 0;

        public static PurchaseProblem CanBuy(FurnitureDefinition piece, GameState state, IReadOnlyList<int> thresholds)
        {
            if (piece == null || !piece.ForSale) return PurchaseProblem.NotForSale;
            if (piece.unique && state.Furniture.OwnedCount(piece.id) > 0) return PurchaseProblem.AlreadyOwned;
            if (!IsOpen(piece.catalogTier, state.Renown, thresholds)) return PurchaseProblem.Locked;
            if (state.Gold < piece.price) return PurchaseProblem.NotEnoughGold;
            return PurchaseProblem.None;
        }

        /// <summary>Buys one copy into storage. False (and nothing changes) when it can't be bought.</summary>
        public static bool Buy(GameState state, FurnitureDefinition piece, IReadOnlyList<int> thresholds)
        {
            if (CanBuy(piece, state, thresholds) != PurchaseProblem.None) return false;
            state.Gold -= piece.price;
            state.Furniture.AddOwned(piece.id, 1);
            return true;
        }

        /// <summary>Whether a copy can be sold: a bought piece with one in storage (<paramref name="inStorage"/>, counted by the caller, which knows what's carried).</summary>
        public static bool CanSell(FurnitureDefinition piece, int inStorage) => piece != null && piece.CanSell && inStorage > 0;

        /// <summary>Sells one copy from storage for its sell-back price.</summary>
        public static bool Sell(GameState state, FurnitureDefinition piece, int inStorage)
        {
            if (!CanSell(piece, inStorage)) return false;
            state.Gold += piece.SellPrice;
            state.Furniture.AddOwned(piece.id, -1);
            return true;
        }

        public static PurchaseProblem CanBuy(FinishDefinition finish, GameState state, IReadOnlyList<int> thresholds)
        {
            if (finish == null) return PurchaseProblem.NotForSale;
            if (state.Furniture.OwnsFinish(finish.id)) return PurchaseProblem.AlreadyOwned;
            if ((finish.sources & FurnitureSource.Bought) == 0) return PurchaseProblem.NotForSale;
            if (!IsOpen(finish.catalogTier, state.Renown, thresholds)) return PurchaseProblem.Locked;
            if (state.Gold < finish.price) return PurchaseProblem.NotEnoughGold;
            return PurchaseProblem.None;
        }

        /// <summary>Buys a finish: owned from then on, for every area.</summary>
        public static bool Buy(GameState state, FinishDefinition finish, IReadOnlyList<int> thresholds)
        {
            if (CanBuy(finish, state, thresholds) != PurchaseProblem.None) return false;
            state.Gold -= finish.price;
            state.Furniture.OwnFinish(finish.id);
            return true;
        }

        /// <summary>
        /// Tiers opened since the last announcement, lowest first (the Night screen announces each, once). Marks them
        /// announced.
        /// </summary>
        public static List<int> Announce(FurnitureState furniture, int renown, IReadOnlyList<int> thresholds)
        {
            var opened = new List<int>();
            int open = OpenTier(renown, thresholds);
            for (int tier = furniture.AnnouncedTier + 1; tier <= open; tier++) opened.Add(tier);
            if (open > furniture.AnnouncedTier) furniture.AnnouncedTier = open;
            return opened;
        }
    }
}
