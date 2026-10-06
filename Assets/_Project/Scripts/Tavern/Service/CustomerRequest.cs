using System;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    /// <summary>
    /// Special requests (4f Checkpoint D): now and then a patron particularly wants tonight's dish they order. It's the same
    /// order through the same service; doing it well earns a little more, missing it disappoints them. Tuning, not law.
    /// </summary>
    [Serializable]
    public struct CustomerRequestSettings
    {
        [Range(0f, 1f), Tooltip("Chance an order is a special request (until the evening's cap).")]
        public float chance;
        [Min(0), Tooltip("Special requests in one evening, at most.")]
        public int maxPerEvening;
        [Min(0), Tooltip("Orders placed before the first request can come (the evening settles in first).")]
        public int ordersBeforeFirst;
        [Min(0f), Tooltip("Thanks for a request met: this share of the dish's value, as a tip.")]
        public float bonusFraction;
        [Min(0), Tooltip("The thanks is at least this much gold.")]
        public int minBonusGold;
        [Tooltip("Renown for a request met (on top of the usual).")]
        public int bonusRenown;

        public static CustomerRequestSettings Default => new()
        {
            chance = 0.2f,
            maxPerEvening = 2,
            ordersBeforeFirst = 1,
            bonusFraction = 0.5f,
            minBonusGold = 2,
            bonusRenown = 1,
        };
    }

    /// <summary>How a special request ended.</summary>
    public enum RequestOutcome
    {
        Open,
        Completed,
        /// <summary>They ran out of patience waiting for it.</summary>
        WalkedOut,
        /// <summary>It couldn't be made after all (the last of its stock went elsewhere, or a dropped plate).</summary>
        SoldOut,
        /// <summary>The evening ended before it reached them.</summary>
        ClosingTime,
    }

    /// <summary>The request rules (pure).</summary>
    public static class CustomerRequestRules
    {
        /// <summary>
        /// Is the order being placed now a special request? Only an order the stock can make right now reaches this (the
        /// service checks), so a request is never for a dish that's off the menu, sold out or unmakeable.
        /// </summary>
        public static bool IsRequest(int ordersBefore, int requestsSoFar, in CustomerRequestSettings s, IRandom random) =>
            random != null && requestsSoFar < s.maxPerEvening && ordersBefore >= s.ordersBeforeFirst && random.Value() < s.chance;

        /// <summary>The thanks for a request met: a share of the dish's value, at least the minimum.</summary>
        public static int BonusGold(float dishValue, in CustomerRequestSettings s) =>
            Mathf.Max(s.minBonusGold, Mathf.RoundToInt(Mathf.Max(0f, dishValue) * s.bonusFraction));
    }
}
