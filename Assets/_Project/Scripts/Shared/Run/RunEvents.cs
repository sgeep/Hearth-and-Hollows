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

    /// <summary>Show/hide the "leave the dungeon" prompt while standing at the exit.</summary>
    public readonly struct DelveExitHint : IEvent
    {
        public readonly bool Visible;
        public DelveExitHint(bool visible) => Visible = visible;
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
        public readonly IngredientStack Incoming;
        public readonly Action<int> OnChosen;

        public SwapPromptRequested(Satchel satchel, IngredientStack incoming, Action<int> onChosen)
        {
            Satchel = satchel;
            Incoming = incoming;
            OnChosen = onChosen;
        }
    }

    public enum DefeatReason
    {
        EssenceDepleted,
    }

    /// <summary>
    /// Ask the UI to show the death screen. The UI answers through <see cref="OnChosen"/> with the
    /// slot whose whole stack is kept, or <see cref="DeathPenalty.KeepNothing"/>.
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

    /// <summary>
    /// Ask the UI to show the delve's result (what came home, what was lost). The UI calls
    /// <see cref="OnContinue"/> when the player moves on.
    /// </summary>
    public readonly struct DelveResultRequested : IEvent
    {
        public readonly Game.DelveReport Report;
        /// <summary>True when the day loop continues to the tavern; false when the floor restarts (played on its own).</summary>
        public readonly bool BackToTavern;
        public readonly Action OnContinue;

        public DelveResultRequested(Game.DelveReport report, bool backToTavern, Action onContinue)
        {
            Report = report;
            BackToTavern = backToTavern;
            OnContinue = onContinue;
        }
    }

    /// <summary>Raised after the death penalty is applied, before the player leaves the dungeon.</summary>
    public readonly struct DelveEnded : IEvent
    {
        public readonly DeathPenaltyResult Result;
        public DelveEnded(DeathPenaltyResult result) => Result = result;
    }

    /// <summary>The run is leaving the current room (4d): the screen covers over <see cref="FadeSeconds"/>.</summary>
    public readonly struct RoomTransitionStarted : IEvent
    {
        public readonly float FadeSeconds;
        public RoomTransitionStarted(float fadeSeconds) => FadeSeconds = fadeSeconds;
    }

    /// <summary>A room has loaded and the player stands in it; the screen uncovers over <see cref="FadeSeconds"/>.</summary>
    public readonly struct RoomEntered : IEvent
    {
        /// <summary>How many rooms this run has entered before this one.</summary>
        public readonly int Index;
        public readonly string RoomId;
        /// <summary>True when the room has enemies, so its exits are sealing.</summary>
        public readonly bool Sealed;
        public readonly float FadeSeconds;

        public RoomEntered(int index, string roomId, bool @sealed, float fadeSeconds)
        {
            Index = index;
            RoomId = roomId;
            Sealed = @sealed;
            FadeSeconds = fadeSeconds;
        }
    }

    /// <summary>The last enemy in the room fell and its exits are opening.</summary>
    public readonly struct RoomCleared : IEvent
    {
        public readonly int Index;
        public readonly string RoomId;

        public RoomCleared(int index, string roomId)
        {
            Index = index;
            RoomId = roomId;
        }
    }
}
