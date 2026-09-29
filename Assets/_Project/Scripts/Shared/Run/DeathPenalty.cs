using System;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;

namespace Hearthdelve.Shared.Run
{
    public readonly struct DeathPenaltyResult
    {
        /// <summary>The single part saved in the Lockbox, if any.</summary>
        public readonly IngredientItem? Kept;
        public readonly int ItemsLost;
        public readonly int RunCurrencyLost;

        public DeathPenaltyResult(IngredientItem? kept, int itemsLost, int runCurrencyLost)
        {
            Kept = kept;
            ItemsLost = itemsLost;
            RunCurrencyLost = runCurrencyLost;
        }
    }

    /// <summary>
    /// Death / Essence-depletion rule (CLAUDE.md): the player keeps exactly one part of their
    /// choosing (the Lockbox); the rest of the haul and all unspent run currency is lost.
    /// Banked gold, relics, unlocks, and tavern progress are untouched (they aren't passed in).
    /// </summary>
    public static class DeathPenalty
    {
        public const int KeepNothing = -1;

        /// <param name="keepSlotIndex">Satchel slot to keep one part from, or <see cref="KeepNothing"/>.</param>
        public static DeathPenaltyResult Resolve(Satchel satchel, int keepSlotIndex, int runCurrency)
        {
            if (satchel == null) throw new ArgumentNullException(nameof(satchel));
            if (keepSlotIndex != KeepNothing && (keepSlotIndex < 0 || keepSlotIndex >= satchel.Capacity))
                throw new ArgumentOutOfRangeException(nameof(keepSlotIndex));

            IngredientItem? kept = null;
            if (keepSlotIndex != KeepNothing)
            {
                var slot = satchel.Slots[keepSlotIndex];
                if (!slot.IsEmpty) kept = slot.Item;
            }

            int lost = satchel.TotalCount - (kept.HasValue ? 1 : 0);
            satchel.Clear();
            return new DeathPenaltyResult(kept, lost, Math.Max(0, runCurrency));
        }
    }
}
