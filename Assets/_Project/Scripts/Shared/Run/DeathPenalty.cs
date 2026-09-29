using System;
using Hearthdelve.Shared.Inventory;

namespace Hearthdelve.Shared.Run
{
    public readonly struct DeathPenaltyResult
    {
        /// <summary>The stack saved in the Lockbox (empty if nothing was kept).</summary>
        public readonly IngredientStack Kept;
        public readonly int ItemsLost;
        public readonly int RunCurrencyLost;

        public DeathPenaltyResult(IngredientStack kept, int itemsLost, int runCurrencyLost)
        {
            Kept = kept;
            ItemsLost = itemsLost;
            RunCurrencyLost = runCurrencyLost;
        }

        public bool KeptSomething => !Kept.IsEmpty;
    }

    /// <summary>
    /// Death / Essence-depletion rule (CLAUDE.md): the player keeps the whole stack in one
    /// satchel slot of their choosing (the Lockbox); the rest of the haul and all unspent run
    /// currency is lost. Banked gold, relics, unlocks, and tavern progress are untouched
    /// (they aren't passed in).
    /// </summary>
    public static class DeathPenalty
    {
        public const int KeepNothing = -1;

        /// <param name="keepSlotIndex">Satchel slot whose stack is kept, or <see cref="KeepNothing"/>.</param>
        public static DeathPenaltyResult Resolve(Satchel satchel, int keepSlotIndex, int runCurrency)
        {
            if (satchel == null) throw new ArgumentNullException(nameof(satchel));
            if (keepSlotIndex != KeepNothing && (keepSlotIndex < 0 || keepSlotIndex >= satchel.Capacity))
                throw new ArgumentOutOfRangeException(nameof(keepSlotIndex));

            var kept = keepSlotIndex == KeepNothing ? IngredientStack.Empty : satchel.Slots[keepSlotIndex];
            int lost = satchel.TotalCount - kept.Count;
            satchel.Clear();
            return new DeathPenaltyResult(kept, lost, Math.Max(0, runCurrency));
        }
    }
}
