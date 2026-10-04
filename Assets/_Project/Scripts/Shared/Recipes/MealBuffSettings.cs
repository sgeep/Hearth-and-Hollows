using System;
using UnityEngine;

namespace Hearthdelve.Shared.Recipes
{
    /// <summary>What a dish eaten before a delve does (GDD §3.1 Morning Prep, §3.3 "pre-delve meals grant run buffs").</summary>
    public enum MealBuffKind
    {
        /// <summary>Not offered as delve meal.</summary>
        None,
        /// <summary>Adds max Essence for the next delve.</summary>
        MaxEssence,
        /// <summary>Essence drains more slowly on the next delve.</summary>
        SlowerDrain,
    }

    [Serializable]
    public struct MealBuffSettings
    {
        public MealBuffKind kind;
        [Min(0), Tooltip("For a perfect Fine dish. MaxEssence: Essence added. SlowerDrain: fraction slower (0.2 = 20%). The dish's quality scales it.")]
        public float amount;
    }
}
