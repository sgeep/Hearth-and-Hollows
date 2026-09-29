using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Shared.Run
{
    /// <summary>
    /// TEMPORARY stand-in for the storeroom until the Phase 3 save system: holds parts that
    /// survived a delve (Lockbox picks) in memory for the current play session only.
    /// </summary>
    public static class PersistentStash
    {
        static readonly List<IngredientItem> s_Items = new();

        public static IReadOnlyList<IngredientItem> Items => s_Items;

        public static void Deposit(IngredientItem item)
        {
            if (item.IsValid) s_Items.Add(item);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => s_Items.Clear();
    }
}
