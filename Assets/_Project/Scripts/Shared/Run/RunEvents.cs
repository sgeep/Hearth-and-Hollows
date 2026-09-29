using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;

namespace Hearthdelve.Shared.Run
{
    // Events that cross from gameplay (Dungeon) to presentation (UI). Neither side references the other.

    /// <summary>Published once per delve so HUD elements can observe the satchel.</summary>
    public readonly struct SatchelBound : IEvent
    {
        public readonly Satchel Satchel;
        public SatchelBound(Satchel satchel) => Satchel = satchel;
    }

    public readonly struct EssenceChanged : IEvent
    {
        public readonly float Current;
        public readonly float Max;
        public readonly bool IsLow;
        /// <summary>True when the change came from taking damage (HUD flashes).</summary>
        public readonly bool FromDamage;

        public EssenceChanged(float current, float max, bool isLow, bool fromDamage)
        {
            Current = current;
            Max = max;
            IsLow = isLow;
            FromDamage = fromDamage;
        }

        public float Normalized => Max > 0f ? Current / Max : 0f;
    }

    [Flags]
    public enum HarvestFlags
    {
        None = 0,
        CleanKill = 1 << 0,
        Overkill = 1 << 1,
        Destroyed = 1 << 2,
        Finisher = 1 << 3,
    }

    /// <summary>Feedback for the HUD harvest feed: what a kill produced and why.</summary>
    public readonly struct HarvestFeedback : IEvent
    {
        public readonly IngredientItem Item;
        public readonly int Count;
        public readonly HarvestFlags Flags;

        public HarvestFeedback(IngredientItem item, int count, HarvestFlags flags)
        {
            Item = item;
            Count = count;
            Flags = flags;
        }
    }

    /// <summary>Show/hide the "satchel full — swap?" hint while standing on a pickup.</summary>
    public readonly struct SatchelFullHint : IEvent
    {
        public readonly bool Visible;
        public SatchelFullHint(bool visible) => Visible = visible;
    }

    /// <summary>
    /// Ask the UI to show the swap prompt. The UI answers through <see cref="OnChosen"/> with the
    /// slot index to discard, or -1 to leave the new part on the ground.
    /// </summary>
    public readonly struct SwapPromptRequested : IEvent
    {
        public readonly Satchel Satchel;
        public readonly IngredientItem Incoming;
        public readonly int IncomingCount;
        public readonly Action<int> OnChosen;

        public SwapPromptRequested(Satchel satchel, IngredientItem incoming, int incomingCount, Action<int> onChosen)
        {
            Satchel = satchel;
            Incoming = incoming;
            IncomingCount = incomingCount;
            OnChosen = onChosen;
        }
    }

    public enum DefeatReason
    {
        EssenceDepleted,
    }

    /// <summary>
    /// Ask the UI to show the death screen. The UI answers through <see cref="OnChosen"/> with the
    /// slot to keep one part from, or <see cref="DeathPenalty.KeepNothing"/>.
    /// </summary>
    public readonly struct DeathScreenRequested : IEvent
    {
        public readonly Satchel Satchel;
        public readonly DefeatReason Reason;
        public readonly Action<int> OnChosen;

        public DeathScreenRequested(Satchel satchel, DefeatReason reason, Action<int> onChosen)
        {
            Satchel = satchel;
            Reason = reason;
            OnChosen = onChosen;
        }
    }

    /// <summary>Raised after the death penalty is applied, before the level restarts.</summary>
    public readonly struct DelveEnded : IEvent
    {
        public readonly DeathPenaltyResult Result;
        public DelveEnded(DeathPenaltyResult result) => Result = result;
    }
}
