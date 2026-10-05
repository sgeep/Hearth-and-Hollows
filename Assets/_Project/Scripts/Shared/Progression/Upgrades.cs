using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Progression
{
    /// <summary>The combined effect of every upgrade level owned.</summary>
    public struct UpgradeEffects
    {
        public int SatchelSlots;
        public float MaxEssence;
    }

    /// <summary>Buying upgrades and adding up their effects. Pure logic.</summary>
    public static class Upgrades
    {
        /// <summary>Gold for the next level, or -1 if the upgrade is maxed.</summary>
        public static int NextCost(TavernUpgradeDefinition upgrade, int currentLevel) =>
            upgrade != null && currentLevel >= 0 && currentLevel < upgrade.MaxLevel ? upgrade.levels[currentLevel].cost : -1;

        public static bool IsMaxed(TavernUpgradeDefinition upgrade, int currentLevel) => upgrade == null || currentLevel >= upgrade.MaxLevel;

        public static bool CanBuy(TavernUpgradeDefinition upgrade, int currentLevel, int gold)
        {
            int cost = NextCost(upgrade, currentLevel);
            return cost >= 0 && gold >= cost;
        }

        /// <summary>Total effect of the owned levels (<paramref name="levelOf"/> gives the level owned per upgrade id).</summary>
        public static UpgradeEffects Effects(IEnumerable<TavernUpgradeDefinition> upgrades, Func<string, int> levelOf)
        {
            var e = new UpgradeEffects();
            if (upgrades == null) return e;
            foreach (var u in upgrades)
            {
                if (u == null) continue;
                int owned = Math.Min(levelOf(u.id), u.MaxLevel);
                float total = 0f;
                for (int i = 0; i < owned; i++) total += u.levels[i].amount;
                switch (u.kind)
                {
                    case UpgradeKind.SatchelSlots: e.SatchelSlots += Mathf.RoundToInt(total); break;
                    case UpgradeKind.MaxEssence: e.MaxEssence += total; break;
                }
            }
            return e;
        }
    }
}
