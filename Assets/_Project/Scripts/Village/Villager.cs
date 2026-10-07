using System.Collections.Generic;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// A named villager standing at their place in Kariaston (2026-10-07: Musashi at the market cart). Talking to them is the
    /// keeper's Interact, like the staff: the story decides what's said (the character's hub conversation). Free time only, and
    /// not while someone's already talking. Where they stand is fixed for now; Checkpoint C's schedules will move them.
    /// </summary>
    public sealed class Villager : MonoBehaviour
    {
        static readonly List<Villager> s_All = new();

        [SerializeField, Tooltip("The character's stable id (CharacterIds).")] string m_CharacterId;
        [SerializeField, Tooltip("Their name in the Content table, for the 'talk to' prompt.")] string m_NameKey;
        [SerializeField] TavernInteractable m_Talk;

        public static IReadOnlyList<Villager> All => s_All;
        public string CharacterId => m_CharacterId;
        public TavernInteractable Talk => m_Talk;

        public static Villager Find(string characterId) => s_All.Find(v => v.m_CharacterId == characterId);

        public void Configure(string characterId, string nameKey, TavernInteractable talk)
        {
            m_CharacterId = characterId;
            m_NameKey = nameKey;
            m_Talk = talk;
        }

        void OnEnable()
        {
            s_All.Add(this);
            if (m_Talk == null) return;
            m_Talk.Describe = () => new TavernHint(TavernHintKind.Talk, m_NameKey);
            m_Talk.Used += OnUsed;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            if (m_Talk == null) return;
            m_Talk.Describe = null;
            m_Talk.Used -= OnUsed;
        }

        void OnUsed(TavernInteractable _) => StoryServices.Conversations?.Talk(m_CharacterId);

        void Update()
        {
            if (m_Talk == null) return;
            IConversationService talk = StoryServices.Conversations;
            bool daytime = TavernDirector.Instance != null && TavernDirector.Instance.Phase == TavernPhase.Daytime;
            bool available = daytime && talk != null && !talk.IsTalking && talk.CanTalk(m_CharacterId);
            if (available != m_Talk.IsAvailable) m_Talk.SetAvailable(available);
        }
    }
}
