using System.Collections.Generic;
using Hearthdelve.Shared.Inventory;
using UnityEngine;

namespace Hearthdelve.Shared.Run
{
    /// <summary>
    /// TEMPORARY stand-in for the storeroom until the Phase 3 save system: holds stacks that
    /// survived a delve (Lockbox picks) in memory for the current play session only.
    /// </summary>
    public static class PersistentStash
    {
        static readonly List<IngredientStack> s_Stacks = new();

        public static IReadOnlyList<IngredientStack> Stacks => s_Stacks;

        public static int TotalCount
        {
            get
            {
                int total = 0;
                foreach (var stack in s_Stacks) total += stack.Count;
                return total;
            }
        }

        public static void Deposit(IngredientStack stack)
        {
            if (!stack.IsEmpty && stack.Item.IsValid) s_Stacks.Add(stack);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => s_Stacks.Clear();
    }
}
