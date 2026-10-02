using System;
using UnityEngine;

namespace Hearthdelve.Shared.Inventory
{
    [Serializable]
    public struct SatchelSettings
    {
        [Min(1)] public int capacity;
        [Min(1), Tooltip("How many identical parts fit in one slot.")]
        public int maxStack;

        public static SatchelSettings Default => new() { capacity = 6, maxStack = 3 };
    }
}
