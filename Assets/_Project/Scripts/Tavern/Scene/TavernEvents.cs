using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Staff;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>What a tavern interactable is: decides what using it does (stations, the pass, seats, the door).</summary>
    public enum TavernInteractableKind
    {
        Grill,
        Tap,
        StewPot,
        /// <summary>The Butcher Block (4f Checkpoint C): used from the Prep screen, at mise en place.</summary>
        ButcherBlock,
        Pass,
        Seat,
        Door,
        /// <summary>Someone to talk to (4g): a member of staff, later anyone with something to say.</summary>
        Person,
    }

    /// <summary>What the interaction hint says (the UI turns it into localized text).</summary>
    public enum TavernHintKind
    {
        /// <summary>"E: Grill": nothing to do there right now.</summary>
        Use,
        /// <summary>"E: cook Kebab".</summary>
        Cook,
        /// <summary>"E: pick up Kebab" (at the pass).</summary>
        PickUp,
        /// <summary>"E: serve Kebab" (next to someone who ordered it).</summary>
        Serve,
        /// <summary>"E: put Kebab back on the pass".</summary>
        PutBack,
        /// <summary>"They ordered Gelbrew" (next to someone who ordered something else).</summary>
        WrongDish,
        /// <summary>"E: make Cellar Stew" (at the empty pot).</summary>
        StartStew,
        /// <summary>"Cellar Stew is simmering".</summary>
        Simmering,
        /// <summary>"E: 3 helpings left".</summary>
        StewReady,
        /// <summary>"Orik is working here".</summary>
        Staffed,
        /// <summary>"E: talk to Boog" (4g).</summary>
        Talk,
    }

    /// <summary>The interaction hint's content: its kind, and the name, dish, count or staff member it mentions.</summary>
    public readonly struct TavernHint : IEquatable<TavernHint>
    {
        public readonly TavernHintKind Kind;
        /// <summary>Localization key (UI table) of the target's name, for <see cref="TavernHintKind.Use"/>.</summary>
        public readonly string NameKey;
        public readonly RecipeDefinition Dish;
        public readonly int Count;
        public readonly StaffDefinition Staff;

        public TavernHint(TavernHintKind kind, string nameKey = null, RecipeDefinition dish = null, int count = 0, StaffDefinition staff = null)
        {
            Kind = kind;
            NameKey = nameKey;
            Dish = dish;
            Count = count;
            Staff = staff;
        }

        public static TavernHint Use(string nameKey) => new(TavernHintKind.Use, nameKey);

        public bool Equals(TavernHint other) =>
            Kind == other.Kind && NameKey == other.NameKey && Dish == other.Dish && Count == other.Count && Staff == other.Staff;

        public override bool Equals(object obj) => obj is TavernHint other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Kind, NameKey, Dish, Count, Staff);
    }

    /// <summary>The player's interaction hint: shown while something is in reach, saying what Interact would do.</summary>
    public readonly struct TavernInteractHint : IEvent
    {
        public readonly bool Visible;
        public readonly TavernHint Hint;
        /// <summary>Localization key of the target's name (same as <c>Hint.NameKey</c> for a plain "use" hint).</summary>
        public string NameKey => Hint.NameKey;

        public TavernInteractHint(bool visible, TavernHint hint)
        {
            Visible = visible;
            Hint = hint;
        }
    }

    /// <summary>The player pressed Interact at a station, the pass, a seat or the door.</summary>
    public readonly struct TavernInteracted : IEvent
    {
        public readonly TavernInteractableKind Kind;
        public readonly TavernInteractable Target;

        public TavernInteracted(TavernInteractableKind kind, TavernInteractable target)
        {
            Kind = kind;
            Target = target;
        }
    }

    /// <summary>A carried plate was bumped (for feedback: step 6). <see cref="Spill"/> is the meter after the bump.</summary>
    /// <summary>What happened to the plate the keeper carries.</summary>
    public enum PlateMoment
    {
        PickedUp,
        PutBack,
        Served,
        Dropped,
    }

    /// <summary>The keeper picked a plate up from the pass, put it back, served it, or dropped it (feedback listens).</summary>
    public readonly struct KeeperPlate : IEvent
    {
        public readonly PlateMoment Moment;
        public readonly RecipeDefinition Dish;
        /// <summary>The serving score when served (0–1).</summary>
        public readonly float Score;

        public KeeperPlate(PlateMoment moment, RecipeDefinition dish, float score = 0f)
        {
            Moment = moment;
            Dish = dish;
            Score = score;
        }
    }

    public readonly struct ServingBumped : IEvent
    {
        public readonly float Strength;
        public readonly float Spill;
        public readonly bool Dropped;
        public readonly bool ByPlayer;

        public ServingBumped(float strength, float spill, bool dropped, bool byPlayer)
        {
            Strength = strength;
            Spill = spill;
            Dropped = dropped;
            ByPlayer = byPlayer;
        }
    }
}
