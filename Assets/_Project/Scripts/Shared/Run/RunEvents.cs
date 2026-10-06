using System;
using System.Collections.Generic;
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
    /// <summary>
    /// The campfire's prompt (4e sign-off): shown while the player is near a fire that still has warmth to give, saying
    /// to stand by it; <see cref="Warming"/> while it's giving Essence back.
    /// </summary>
    public readonly struct CampfireHint : IEvent
    {
        public readonly bool Visible;
        public readonly bool Warming;

        public CampfireHint(bool visible, bool warming)
        {
            Visible = visible;
            Warming = warming;
        }
    }

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
        /// <summary>Furnishings found on the delve (4f Checkpoint C): lost with it, never in the Lockbox.</summary>
        public readonly IReadOnlyList<string> Curios;
        /// <summary>Quest objects carried (4g Checkpoint B): lost with the delve, never in the Lockbox; they turn up again later.</summary>
        public readonly IReadOnlyList<string> QuestObjects;

        public DeathScreenRequested(Satchel satchel, DefeatReason reason, Action<int> onChosen, IReadOnlyList<string> curios = null,
            IReadOnlyList<string> questObjects = null)
        {
            Satchel = satchel;
            Reason = reason;
            OnChosen = onChosen;
            Curios = curios ?? Array.Empty<string>();
            QuestObjects = questObjects ?? Array.Empty<string>();
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
        /// <summary>1, 2 or 3.</summary>
        public readonly int Floor;
        /// <summary>The run's seed (replays the same run).</summary>
        public readonly int Seed;

        public RoomEntered(int index, string roomId, bool @sealed, float fadeSeconds, int floor = 1, int seed = 0)
        {
            Index = index;
            RoomId = roomId;
            Sealed = @sealed;
            FadeSeconds = fadeSeconds;
            Floor = floor;
            Seed = seed;
        }
    }

    /// <summary>A furnishing was picked up in the Hollows (4f Checkpoint C): run-bound until the delve ends well.</summary>
    public readonly struct CurioFound : IEvent
    {
        public readonly string FurnitureId;
        /// <summary>How many the delve carries now.</summary>
        public readonly int Total;

        public CurioFound(string furnitureId, int total)
        {
            FurnitureId = furnitureId;
            Total = total;
        }
    }

    /// <summary>The run's unbanked Gold changed (4d step 3), with the new total.</summary>
    public readonly struct RunGoldChanged : IEvent
    {
        public readonly int Gold;
        public RunGoldChanged(int gold) => Gold = gold;
    }

    /// <summary>A power spark was touched (4d step 4): the run is paused; answer with the power chosen, or null to leave it.</summary>
    public readonly struct RunPowerOfferRequested : IEvent
    {
        public readonly IReadOnlyList<RunPowerDefinition> Options;
        public readonly Action<RunPowerDefinition> OnChosen;

        public RunPowerOfferRequested(IReadOnlyList<RunPowerDefinition> options, Action<RunPowerDefinition> onChosen)
        {
            Options = options;
            OnChosen = onChosen;
        }
    }

    /// <summary>The run took a power; <see cref="All"/> is every power it holds now.</summary>
    public readonly struct RunPowerTaken : IEvent
    {
        public readonly RunPowerDefinition Power;
        public readonly IReadOnlyList<RunPowerDefinition> All;

        public RunPowerTaken(RunPowerDefinition power, IReadOnlyList<RunPowerDefinition> all)
        {
            Power = power;
            All = all;
        }
    }

    /// <summary>A boss encounter began (4e): its id (the name is the UI string boss.&lt;id&gt;) and its health.</summary>
    public readonly struct BossEncounterStarted : IEvent
    {
        public readonly string BossId;
        public readonly float Health;
        public readonly float MaxHealth;

        public BossEncounterStarted(string bossId, float health, float maxHealth)
        {
            BossId = bossId;
            Health = health;
            MaxHealth = maxHealth;
        }
    }

    public readonly struct BossHealthChanged : IEvent
    {
        public readonly float Health;
        public readonly float MaxHealth;

        public BossHealthChanged(float health, float maxHealth)
        {
            Health = health;
            MaxHealth = maxHealth;
        }
    }

    /// <summary>A boss entered a new phase (4e step 2: the troll's frenzy is phase 2).</summary>
    public readonly struct BossPhaseChanged : IEvent
    {
        public readonly int Phase;
        public BossPhaseChanged(int phase) => Phase = phase;
    }

    /// <summary>A boss encounter ended: defeated, or not (the player died, or the room went away).</summary>
    public readonly struct BossEncounterEnded : IEvent
    {
        public readonly string BossId;
        public readonly bool Defeated;

        public BossEncounterEnded(string bossId, bool defeated)
        {
            BossId = bossId;
            Defeated = defeated;
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

    /// <summary>An enemy can be finished with the Harvest Finisher (it just became eligible): the onboarding's cue (4g Checkpoint B).</summary>
    public readonly struct FinisherAvailable : IEvent
    {
        public readonly bool Boss;
        public FinisherAvailable(bool boss) => Boss = boss;
    }
}
