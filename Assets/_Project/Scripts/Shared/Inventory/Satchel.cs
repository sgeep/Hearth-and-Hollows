using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;

namespace Hearthdelve.Shared.Inventory
{
    /// <summary>
    /// The delve carry inventory (GDD §4.4). Fixed slot count; identical items stack up to
    /// <see cref="MaxStack"/> per slot, and merged stacks take the count-weighted freshness.
    /// Pure logic, no Unity dependencies beyond data types.
    /// </summary>
    public sealed class Satchel
    {
        readonly IngredientStack[] m_Slots;

        public Satchel(int capacity, int maxStack)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (maxStack < 1) throw new ArgumentOutOfRangeException(nameof(maxStack));
            m_Slots = new IngredientStack[capacity];
            MaxStack = maxStack;
        }

        public int Capacity => m_Slots.Length;
        public int MaxStack { get; }
        public IReadOnlyList<IngredientStack> Slots => m_Slots;

        /// <summary>Raised after any change to slot contents.</summary>
        public event Action Changed;

        public bool IsEmpty
        {
            get
            {
                foreach (var slot in m_Slots) if (!slot.IsEmpty) return false;
                return true;
            }
        }

        public int TotalCount
        {
            get
            {
                int total = 0;
                foreach (var slot in m_Slots) total += slot.Count;
                return total;
            }
        }

        /// <summary>How many of <paramref name="item"/> could be added right now.</summary>
        public int SpaceFor(IngredientItem item)
        {
            if (!item.IsValid) return 0;
            int space = 0;
            foreach (var slot in m_Slots)
            {
                if (slot.IsEmpty) space += MaxStack;
                else if (slot.Item == item) space += MaxStack - slot.Count;
            }
            return space;
        }

        public bool CanAccept(IngredientItem item) => SpaceFor(item) > 0;

        /// <summary>
        /// Adds up to <paramref name="count"/>, topping up matching stacks first, then empty slots.
        /// Returns how many could NOT be added.
        /// </summary>
        public int Add(IngredientItem item, int count = 1, float freshness = Freshness.Max)
        {
            if (!item.IsValid) throw new ArgumentException("Item has no definition.", nameof(item));
            if (count <= 0) return 0;

            int remaining = count;
            for (int i = 0; i < m_Slots.Length && remaining > 0; i++)
            {
                var slot = m_Slots[i];
                if (slot.IsEmpty || slot.Item != item || slot.Count >= MaxStack) continue;
                int moved = Math.Min(remaining, MaxStack - slot.Count);
                m_Slots[i] = new IngredientStack(item, slot.Count + moved, Freshness.Merge(slot.Count, slot.Freshness, moved, freshness));
                remaining -= moved;
            }
            for (int i = 0; i < m_Slots.Length && remaining > 0; i++)
            {
                if (!m_Slots[i].IsEmpty) continue;
                int moved = Math.Min(remaining, MaxStack);
                m_Slots[i] = new IngredientStack(item, moved, freshness);
                remaining -= moved;
            }

            if (remaining != count) Changed?.Invoke();
            return remaining;
        }

        /// <summary>Empties a slot and returns what was in it.</summary>
        public IngredientStack RemoveAt(int index)
        {
            CheckIndex(index);
            var removed = m_Slots[index];
            if (removed.IsEmpty) return removed;
            m_Slots[index] = IngredientStack.Empty;
            Changed?.Invoke();
            return removed;
        }

        /// <summary>
        /// Swap prompt: discard slot <paramref name="index"/> and place the incoming stack there
        /// (up to <see cref="MaxStack"/>). Returns the discarded contents; <paramref name="placed"/>
        /// is how many of the incoming items went in.
        /// </summary>
        public IngredientStack ReplaceAt(int index, IngredientStack incoming, out int placed)
        {
            CheckIndex(index);
            if (incoming.IsEmpty || !incoming.Item.IsValid) throw new ArgumentException("Incoming stack is empty.", nameof(incoming));

            var removed = m_Slots[index];
            placed = Math.Min(incoming.Count, MaxStack);
            m_Slots[index] = incoming.WithCount(placed);
            Changed?.Invoke();
            return removed;
        }

        /// <summary>
        /// Ages every stack by <paramref name="baseLoss"/> (Chilled parts lose less, see
        /// <see cref="FreshnessSettings"/>). Continuous, so it doesn't raise <see cref="Changed"/>.
        /// </summary>
        public void Decay(in FreshnessSettings settings, float baseLoss)
        {
            if (baseLoss <= 0f) return;
            for (int i = 0; i < m_Slots.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot.IsEmpty) continue;
                m_Slots[i] = slot.WithFreshness(Freshness.Decay(slot.Freshness, settings.LossFor(slot.Item, baseLoss)));
            }
        }

        public void Clear()
        {
            if (IsEmpty) return;
            Array.Clear(m_Slots, 0, m_Slots.Length);
            Changed?.Invoke();
        }

        void CheckIndex(int index)
        {
            if (index < 0 || index >= m_Slots.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
