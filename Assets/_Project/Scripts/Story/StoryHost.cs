using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.Story.Quests;
using Hearthdelve.Story.Relationships;
using PixelCrushers.LoveHate;
using PixelCrushers.QuestMachine;
using UnityEngine;

namespace Hearthdelve.Story
{
    /// <summary>
    /// The story layer's home in the Boot scene (4g): persistent like <see cref="GameFlow"/>, beside the Dialogue Manager, Quest
    /// Machine and the Love/Hate faction manager it drives. It is where gameplay facts become story: each fact a deed defines is
    /// committed to the characters who learn of it, and every fact is passed to Quest Machine as a message. It keeps the
    /// middleware's state in the game's own save (<see cref="IStoryStateParticipant"/>) and answers "can I talk to them?"
    /// (<see cref="IConversationService"/>). Flow: gameplay → facts (EventBus) → adapters → Pixel Crushers.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class StoryHost : MonoBehaviour, IStoryStateParticipant, IConversationService
    {
        [SerializeField] StoryDatabase m_Database;
        [SerializeField] FactionManager m_Factions;
        [SerializeField] QuestJournal m_Journal;
        [SerializeField, Tooltip("Log each deed's effect on each character (development builds).")] bool m_LogDeeds = true;

        public static StoryHost Instance { get; private set; }

        public StoryDatabase Database => m_Database;
        public CharacterDirectory Characters { get; private set; }
        public RelationshipAdapter Relationships { get; private set; }
        public QuestAdapter Quests { get; private set; }
        public DialogueAdapter Dialogue { get; private set; }
        /// <summary>Messages from the last restore (dropped relationships).</summary>
        public List<string> LastWarnings { get; } = new();

        public void Configure(StoryDatabase database, FactionManager factions, QuestJournal journal)
        {
            m_Database = database;
            m_Factions = factions;
            m_Journal = journal;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Characters = m_Database != null ? m_Database.Directory() : new CharacterDirectory(null);
            Dialogue = new DialogueAdapter(Characters);
            Quests = new QuestAdapter(m_Journal);
            StoryServices.Register(this, this);
        }

        void Start()
        {
            // After the faction manager's Awake, which swaps in its runtime copy of the database: the stand-ins use that copy.
            if (m_Factions != null)
            {
                var root = new GameObject("Social Stand-ins").transform;
                root.SetParent(transform, false);
                Relationships = new RelationshipAdapter(m_Factions, root, Characters, m_Database != null ? m_Database.deeds : null);
                Relationships.Initialize();
                Relationships.Reacted += OnReacted;
            }
            StoryLua.Register();
            Dialogue.SetPlayerName(PlayerProfile.DefaultName);
            if (GameFlow.Instance != null) GameFlow.Instance.PhaseChanged += OnPhaseChanged;
        }

        void OnEnable()
        {
            EventBus<TrophyDisplayed>.Subscribe(OnTrophyDisplayed);
            EventBus<BossDefeated>.Subscribe(OnBossDefeated);
            EventBus<CurioBroughtHome>.Subscribe(OnCurioBroughtHome);
        }

        void OnDisable()
        {
            EventBus<TrophyDisplayed>.Unsubscribe(OnTrophyDisplayed);
            EventBus<BossDefeated>.Unsubscribe(OnBossDefeated);
            EventBus<CurioBroughtHome>.Unsubscribe(OnCurioBroughtHome);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            if (GameFlow.Instance != null) GameFlow.Instance.PhaseChanged -= OnPhaseChanged;
            StoryServices.Unregister(this);
            if (Relationships != null) Relationships.Reacted -= OnReacted;
            Instance = null;
        }

        void OnPhaseChanged()
        {
            GameState state = GameFlow.Instance != null ? GameFlow.Instance.State : null;
            if (state != null) Relationships?.SetDay(state.Day);
        }

        // ---------- Facts → story ----------

        void OnTrophyDisplayed(TrophyDisplayed e)
        {
            Commit(DeedSource.TrophyDisplayed);
            Quests?.Fact(nameof(TrophyDisplayed), e.FurnitureId);
        }

        void OnBossDefeated(BossDefeated e) => Quests?.Fact(nameof(BossDefeated), e.BossId);

        void OnCurioBroughtHome(CurioBroughtHome e) => Quests?.Fact(nameof(CurioBroughtHome), e.FurnitureId);

        /// <summary>Commits every deed the fact defines to the characters who learn of it.</summary>
        public void Commit(DeedSource source)
        {
            if (Relationships == null || m_Database == null) return;
            foreach (DeedDefinition deed in RelationshipRules.DeedsFor(source, m_Database.deeds))
                Relationships.Commit(deed, RelationshipRules.Learners(deed, Characters.All));
        }

        void OnReacted(DeedReaction r)
        {
            if (m_LogDeeds && Debug.isDebugBuild)
                Debug.Log($"[Hearthdelve] {r.Judge} learned of {r.Deed}: affinity {r.Affinity:+0.#;-0.#;0}, respect {r.Respect:+0.#;-0.#;0}" +
                          $"{(r.Remembered ? ", remembered" : string.Empty)} ({Relationships.Describe(r.Judge)}).");
        }

        // ---------- IConversationService ----------

        public bool IsTalking => Dialogue != null && Dialogue.IsTalking;
        public bool CanTalk(string characterId) => Dialogue != null && Dialogue.CanTalk(characterId);
        public bool Talk(string characterId) => Dialogue != null && Dialogue.Talk(characterId);

        // ---------- IStoryStateParticipant ----------

        public void Capture(GameState state)
        {
            if (state == null) return;
            state.Story.Dialogue = Dialogue?.Record() ?? string.Empty;
            state.Story.Quests = Quests?.Record() ?? string.Empty;
            if (Relationships != null) state.Story.Relationships = Relationships.Record();
        }

        public void Restore(GameState state)
        {
            if (state == null) return;
            LastWarnings.Clear();
            Relationships?.SetDay(state.Day);
            Dialogue?.Apply(state.Story.Dialogue);
            Dialogue?.SetPlayerName(state.Story.Player?.name ?? PlayerProfile.DefaultName);
            Quests?.Apply(state.Story.Quests);
            Relationships?.Apply(state.Story.Relationships, LastWarnings);
            foreach (string w in LastWarnings) Debug.LogWarning($"[Hearthdelve] Story: {w}");
        }

        public void Clear()
        {
            Dialogue?.Clear();
            Dialogue?.SetPlayerName(PlayerProfile.DefaultName);
            Quests?.Clear();
            Relationships?.Clear();
            Relationships?.SetDay(1);
        }
    }
}
