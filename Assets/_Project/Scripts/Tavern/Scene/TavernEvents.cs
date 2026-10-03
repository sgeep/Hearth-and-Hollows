using Hearthdelve.Core.Events;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>What a tavern interactable is: decides what using it does (stations, the pass, seats, the door).</summary>
    public enum TavernInteractableKind
    {
        Grill,
        Tap,
        StewPot,
        Pass,
        Seat,
        Door,
    }

    /// <summary>The player's interaction hint: shown with the target's name while something is in reach.</summary>
    public readonly struct TavernInteractHint : IEvent
    {
        public readonly bool Visible;
        /// <summary>Localization key of the target's name (UI table).</summary>
        public readonly string NameKey;

        public TavernInteractHint(bool visible, string nameKey)
        {
            Visible = visible;
            NameKey = nameKey;
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
}
