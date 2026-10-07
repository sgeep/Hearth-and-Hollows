using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Story;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A place in Tally Ho! the keeper used in the daytime that opens a screen (4h): the storeroom shelves, a station for the
    /// delve meal, the menu board. The UI listens and opens the existing panel (gameplay never references the UI).
    /// </summary>
    public readonly struct DaytimePlaceUsed : IEvent
    {
        public readonly TavernInteractableKind Kind;
        /// <summary>For a station: which one (the delve meal it can cook).</summary>
        public readonly CookStation Station;

        public DaytimePlaceUsed(TavernInteractableKind kind, CookStation station = CookStation.Grill)
        {
            Kind = kind;
            Station = station;
        }
    }

    /// <summary>
    /// A fixed thing in Tally Ho! the keeper uses in the free daytime (4h Checkpoint A): the storeroom shelves, the menu board, the
    /// pinned plans, and things to look at. Reuses the tavern's interaction (<see cref="TavernInteractable"/>); usable only while
    /// the keeper is on foot in the daytime. What it does: the storeroom and the menu board ask the UI for their screens
    /// (<see cref="DaytimePlaceUsed"/>), the plans open Decorate Mode, a thing to look at plays its one-line conversation.
    /// </summary>
    [RequireComponent(typeof(TavernInteractable))]
    public sealed class DaytimeFixture : MonoBehaviour
    {
        [SerializeField, Tooltip("Inspect: the conversation (Dialogue System title) it plays.")]
        string m_Conversation;

        TavernInteractable m_Interactable;

        public TavernInteractable Interactable => m_Interactable != null ? m_Interactable : m_Interactable = GetComponent<TavernInteractable>();
        public string Conversation => m_Conversation;

        public void Configure(string conversation) => m_Conversation = conversation;

        void OnEnable()
        {
            Interactable.Used += OnUsed;
        }

        void OnDisable()
        {
            if (m_Interactable != null) m_Interactable.Used -= OnUsed;
        }

        static bool Daytime => TavernDirector.Instance == null || TavernDirector.Instance.Phase == TavernPhase.Daytime;

        void Update()
        {
            bool talking = StoryServices.Conversations != null && StoryServices.Conversations.IsTalking;
            bool usable = Daytime && !talking && (Interactable.Kind != TavernInteractableKind.Plans || DecorateMode.Instance == null || DecorateMode.Instance.CanEnter);
            if (usable != Interactable.IsAvailable) Interactable.SetAvailable(usable);
        }

        void OnUsed(TavernInteractable target)
        {
            if (!Daytime) return;
            switch (target.Kind)
            {
                case TavernInteractableKind.Storeroom:
                case TavernInteractableKind.MenuBoard:
                    EventBus<DaytimePlaceUsed>.Publish(new DaytimePlaceUsed(target.Kind));
                    break;
                case TavernInteractableKind.Plans:
                    DecorateMode.Instance?.Enter();
                    break;
                case TavernInteractableKind.Inspect:
                    if (!string.IsNullOrEmpty(m_Conversation)) StoryServices.Conversations?.Play(m_Conversation);
                    break;
            }
        }
    }
}
