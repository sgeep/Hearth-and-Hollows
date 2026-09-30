using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;

namespace Hearthdelve.Shared.Inventory
{
    /// <summary>
    /// The tavern's ingredient stock (GDD §10.4 InventorySystem). Unlimited space; identical
    /// items merge into one stack with count-weighted freshness. Stock is taken least-fresh
    /// first, then lower quality first (CLAUDE.md). Pure logic.
    /// </summary>
    public sealed class Storeroom
    {
        readonly List<IngredientStack> m_Stacks = new();

        public IReadOnlyList<IngredientStack> Stacks => m_Stacks;

        /// <summary>Raised after any change to stock.</summary>
        public event Action Changed;

        public int TotalCount
        {
            get
            {
                int total = 0;
                foreach (var s in m_Stacks) total += s.Count;
                return total;
            }
        }

        public void Add(IngredientStack stack)
        {
            if (stack.IsEmpty || !stack.Item.IsValid) return;
            AddSilently(stack);
            Changed?.Invoke();
        }

        public void AddRange(IEnumerable<IngredientStack> stacks)
        {
            bool any = false;
            foreach (var s in stacks)
            {
                if (s.IsEmpty || !s.Item.IsValid) continue;
                AddSilently(s);
                any = true;
            }
            if (any) Changed?.Invoke();
        }

        void AddSilently(IngredientStack stack)
        {
            for (int i = 0; i < m_Stacks.Count; i++)
            {
                var existing = m_Stacks[i];
                if (existing.Item != stack.Item) continue;
                m_Stacks[i] = new IngredientStack(stack.Item, existing.Count + stack.Count,
                    Freshness.Merge(existing.Count, existing.Freshness, stack.Count, stack.Freshness));
                return;
            }
            m_Stacks.Add(stack);
        }

        public int CountMatching(Predicate<IngredientItem> match)
        {
            int total = 0;
            foreach (var s in m_Stacks) if (match(s.Item)) total += s.Count;
            return total;
        }

        /// <summary>
        /// Takes <paramref name="count"/> matching parts in use order (least fresh, then lower
        /// quality). Returns the portions taken, or null — taking nothing — if there aren't enough.
        /// </summary>
        public List<IngredientStack> Take(Predicate<IngredientItem> match, int count)
        {
            if (count <= 0) return new List<IngredientStack>();
            if (CountMatching(match) < count) return null;

            var order = new List<int>();
            for (int i = 0; i < m_Stacks.Count; i++) if (match(m_Stacks[i].Item)) order.Add(i);
            order.Sort((a, b) => Freshness.UseOrder(m_Stacks[a], m_Stacks[b]));

            var taken = new List<IngredientStack>();
            int remaining = count;
            foreach (int i in order)
            {
                if (remaining == 0) break;
                var s = m_Stacks[i];
                int n = Math.Min(remaining, s.Count);
                taken.Add(s.WithCount(n));
                m_Stacks[i] = s.WithCount(s.Count - n);
                remaining -= n;
            }
            m_Stacks.RemoveAll(s => s.IsEmpty);
            Changed?.Invoke();
            return taken;
        }

        /// <summary>Ages every stack by <paramref name="baseLoss"/> (overnight). Chilled parts lose less.</summary>
        public void Decay(in FreshnessSettings settings, float baseLoss)
        {
            if (baseLoss <= 0f || m_Stacks.Count == 0) return;
            for (int i = 0; i < m_Stacks.Count; i++)
            {
                var s = m_Stacks[i];
                m_Stacks[i] = s.WithFreshness(Freshness.Decay(s.Freshness, settings.LossFor(s.Item, baseLoss)));
            }
            Changed?.Invoke();
        }

        /// <summary>Independent copy (used to test whether a recipe can be made without touching stock).</summary>
        public Storeroom Clone()
        {
            var copy = new Storeroom();
            copy.m_Stacks.AddRange(m_Stacks);
            return copy;
        }

        public void Clear()
        {
            if (m_Stacks.Count == 0) return;
            m_Stacks.Clear();
            Changed?.Invoke();
        }
    }
}
