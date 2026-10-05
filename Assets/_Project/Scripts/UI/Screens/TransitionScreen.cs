using System.Collections;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The day's transitions (Boot scene): every scene change fades to black, names where the day is going
    /// ("daytime · day 2", "evening · day 2", the tavern closing and the way down, "night · day 2") and fades in.
    /// Real time, so a paused game doesn't stall it. Blocks input while the screen is covered.
    /// </summary>
    public sealed class TransitionScreen : MonoBehaviour, ISceneTransition
    {
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] LocalizedSuperText m_Caption;
        [SerializeField, Min(0f)] float m_FadeOutSeconds = 0.35f;
        [SerializeField, Min(0f), Tooltip("Seconds the caption holds on black before the new scene fades in.")] float m_HoldSeconds = 0.7f;
        [SerializeField, Min(0f)] float m_FadeInSeconds = 0.45f;

        GameFlow m_Flow;
        bool m_HasCaption;

        public bool IsCovering => m_Group != null && m_Group.alpha > 0f;
        public string CaptionKey { get; private set; }

        public void Configure(CanvasGroup group, LocalizedSuperText caption)
        {
            m_Group = group;
            m_Caption = caption;
        }

        // Awake: GameFlow (earlier in the execution order) exists, and its Start begins the first load.
        void Awake()
        {
            m_Caption.gameObject.SetActive(false);
            m_Flow = GameFlow.Instance;
            if (m_Flow == null)
            {
                SetAlpha(0f);
                return;
            }
            // The game starts on black and fades into the main menu.
            SetAlpha(1f);
            m_Flow.Transition = this;
            m_Flow.PhaseChanged += OnPhaseChanged;
        }

        void OnDestroy()
        {
            if (m_Flow == null) return;
            if (ReferenceEquals(m_Flow.Transition, this)) m_Flow.Transition = null;
            m_Flow.PhaseChanged -= OnPhaseChanged;
        }

        public IEnumerator Cover()
        {
            ShowCaption();
            yield return Fade(m_Group.alpha, 1f, m_FadeOutSeconds);
        }

        public IEnumerator Reveal()
        {
            if (m_HasCaption) yield return new WaitForSecondsRealtime(m_HoldSeconds);
            yield return Fade(1f, 0f, m_FadeInSeconds);
            m_Caption.gameObject.SetActive(false);
        }

        // Every phase change now loads a scene, which shows its caption; nothing to do in place.
        void OnPhaseChanged() { }

        void ShowCaption()
        {
            GameState state = m_Flow != null ? m_Flow.State : null;
            CaptionKey = state == null ? null : state.Phase switch
            {
                DayPhase.Daytime => LoopLocKeys.MorningTitle,
                // After an evening, the tavern closes; otherwise (a new game, a resumed save) straight into the Hollows.
                DayPhase.Delve => state.Today.EveningRecorded ? LoopLocKeys.TransitionDelve : LoopLocKeys.TransitionFirstDelve,
                DayPhase.Evening => LoopLocKeys.TransitionEvening,
                _ => LoopLocKeys.NightTitle,
            };
            m_HasCaption = CaptionKey != null;
            m_Caption.gameObject.SetActive(m_HasCaption);
            if (m_HasCaption) m_Caption.Set(CaptionKey, state.Day);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float alpha)
        {
            m_Group.alpha = alpha;
            m_Group.blocksRaycasts = alpha > 0f;
        }
    }
}
