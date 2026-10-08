using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// The first free day's prompts (4i-A, D3): one short line over the interaction hint the first time the keeper reaches the
    /// garden, Musashi's market and the menu board in the daytime (<see cref="FirstDayPrompts"/>). Each once per game; a few
    /// seconds, then gone. Orik and Boog say the rest in their own words on the first morning.
    /// </summary>
    public sealed class SurfacePrompts : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] LocalizedSuperText m_Text;
        [SerializeField, Min(0.5f)] float m_Hold = 7f;
        [SerializeField, Min(0.01f)] float m_Fade = 0.4f;

        float m_ShownAt = float.NegativeInfinity;

        /// <summary>The prompt showing now (its id), or null.</summary>
        public string Showing { get; private set; }

        /// <summary>How many prompts this has shown (tests: a second visit shows none).</summary>
        public int ShownCount { get; private set; }

        public void Configure(CanvasGroup group, LocalizedSuperText text)
        {
            m_Group = group;
            m_Text = text;
        }

        void Awake()
        {
            if (m_Group != null) m_Group.alpha = 0f;
        }

        void OnEnable() => EventBus<TavernInteractHint>.Subscribe(OnHint);
        void OnDisable() => EventBus<TavernInteractHint>.Unsubscribe(OnHint);

        /// <summary>The prompt for what the keeper faces, if it's one of the three.</summary>
        public static string PromptFor(TavernInteractableKind kind) => kind switch
        {
            TavernInteractableKind.GardenBed => FirstDayPrompts.Garden,
            TavernInteractableKind.MarketStall => FirstDayPrompts.Market,
            TavernInteractableKind.MenuBoard => FirstDayPrompts.MenuBoard,
            _ => null,
        };

        static string KeyFor(string prompt) => prompt switch
        {
            FirstDayPrompts.Garden => MenuLocKeys.PromptGarden,
            FirstDayPrompts.Market => MenuLocKeys.PromptMarket,
            _ => MenuLocKeys.PromptMenuBoard,
        };

        void OnHint(TavernInteractHint hint)
        {
            if (!hint.Visible) return;
            string prompt = PromptFor(hint.Target);
            GameFlow flow = GameFlow.Instance;
            bool inGame = flow != null && flow.InGame;
            if (prompt == null || !FirstDayPrompts.Due(prompt, inGame, inGame ? flow.State.Phase : DayPhase.Daytime,
                    inGame ? flow.State.Story.Opening : OpeningStage.Arrival, inGame ? flow.State.Story.SeenHints : null)) return;
            flow.MarkHintSeen(prompt);
            Showing = prompt;
            ShownCount++;
            m_ShownAt = Time.unscaledTime;
            m_Text?.Set(KeyFor(prompt));
        }

        void Update()
        {
            if (m_Group == null) return;
            float age = Time.unscaledTime - m_ShownAt;
            m_Group.alpha = age < m_Hold ? 1f : Mathf.Clamp01(1f - (age - m_Hold) / m_Fade);
            if (m_Group.alpha <= 0f) Showing = null;
        }
    }
}
