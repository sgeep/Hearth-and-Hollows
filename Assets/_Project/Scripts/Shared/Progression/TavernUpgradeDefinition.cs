using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Shared.Progression
{
    /// <summary>What an upgrade improves (GDD §7.2).</summary>
    public enum UpgradeKind
    {
        /// <summary>Extra satchel slots on every delve.</summary>
        SatchelSlots,
        /// <summary>Extra max Essence on every delve.</summary>
        MaxEssence,
        /// <summary>Extra seats in the tavern.</summary>
        Seats,
    }

    [Serializable]
    public struct UpgradeLevel
    {
        [Min(0), Tooltip("Gold to buy this level.")]
        public int cost;
        [Tooltip("How much this level adds (slots, Essence, or seats).")]
        public float amount;
    }

    /// <summary>An upgrade bought at Night, level by level; each level costs more (GDD §7.2).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Tavern Upgrade Definition", fileName = "Upgrade_")]
    public sealed class TavernUpgradeDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves. Never change after shipping.")]
        public string id;
        public LocalizedString displayName;
        public UpgradeKind kind;
        [Tooltip("In purchase order. The number of entries is the max level.")]
        public List<UpgradeLevel> levels = new();

        public int MaxLevel => levels.Count;
    }
}
