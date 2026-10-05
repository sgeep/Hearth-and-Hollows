using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Hud
{
    /// <summary>The boss's name and health across the top of the screen while its encounter runs (4e).</summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Label;
        [SerializeField] Image m_Fill;
        [SerializeField, Tooltip("The fill in the boss's second phase.")]
        Color m_FrenzyColour = new(0.95f, 0.45f, 0.1f);
        [SerializeField, Min(0f), Tooltip("Seconds the defeat caption stays.")]
        float m_CaptionSeconds = 3f;

        Color m_Colour;
        float m_HideAt = -1f;
        // The frenzy caption's end, after which the bar shows the boss's name again.
        float m_NameAt = -1f;
        string m_BossName;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public float Fraction => m_Fill != null ? m_Fill.fillAmount : 0f;
        public string BossId { get; private set; }
        public bool IsCaption { get; private set; }

        public void Configure(GameObject root, LocalizedSuperText name, Image fill)
        {
            m_Root = root;
            m_Label = name;
            m_Fill = fill;
        }

        void Awake()
        {
            if (m_Root != null) m_Root.SetActive(false);
            if (m_Fill != null) m_Colour = m_Fill.color;
        }

        /// <summary>The bar says the boss is raging (its frenzy's roar).</summary>
        public bool ShowsFrenzy { get; private set; }

        void Update()
        {
            if (m_NameAt >= 0f && Time.unscaledTime >= m_NameAt)
            {
                m_NameAt = -1f;
                ShowsFrenzy = false;
                if (!IsCaption) m_Label?.Set(LocKeys.BossName(m_BossName));
            }
            if (m_HideAt < 0f || Time.unscaledTime < m_HideAt) return;
            m_HideAt = -1f;
            IsCaption = false;
            if (m_Root != null) m_Root.SetActive(false);
        }

        void OnEnable()
        {
            EventBus<BossEncounterStarted>.Subscribe(OnStarted);
            EventBus<BossHealthChanged>.Subscribe(OnHealth);
            EventBus<BossEncounterEnded>.Subscribe(OnEnded);
            EventBus<BossPhaseChanged>.Subscribe(OnPhase);
        }

        void OnDisable()
        {
            EventBus<BossEncounterStarted>.Unsubscribe(OnStarted);
            EventBus<BossHealthChanged>.Unsubscribe(OnHealth);
            EventBus<BossEncounterEnded>.Unsubscribe(OnEnded);
            EventBus<BossPhaseChanged>.Unsubscribe(OnPhase);
        }

        void OnStarted(BossEncounterStarted e)
        {
            BossId = e.BossId;
            m_BossName = e.BossId;
            IsCaption = false;
            ShowsFrenzy = false;
            m_NameAt = -1f;
            m_HideAt = -1f;
            if (m_Fill != null) m_Fill.color = m_Colour;
            m_Label?.Set(LocKeys.BossName(e.BossId));
            Show(e.Health, e.MaxHealth);
            if (m_Root != null) m_Root.SetActive(true);
        }

        void OnHealth(BossHealthChanged e) => Show(e.Health, e.MaxHealth);

        void OnPhase(BossPhaseChanged e)
        {
            if (e.Phase < 2) return;
            if (m_Fill != null) m_Fill.color = m_FrenzyColour;
            // Not colour alone (4e sign-off): the bar says so while it roars.
            if (m_Label != null && !IsCaption && m_BossName != null)
            {
                m_Label.Set(LocKeys.BossFrenzy, Loc.UI(LocKeys.BossName(m_BossName)));
                ShowsFrenzy = true;
                m_NameAt = Time.unscaledTime + 2f;
            }
        }

        // Defeated: "the Larder Troll falls" over an empty bar for a moment. Otherwise the bar simply goes.
        void OnEnded(BossEncounterEnded e)
        {
            if (!e.Defeated || m_Label == null)
            {
                if (m_Root != null) m_Root.SetActive(false);
                return;
            }
            IsCaption = true;
            ShowsFrenzy = false;
            m_NameAt = -1f;
            Show(0f, 1f);
            m_Label.Set(LocKeys.BossDefeated, Loc.UI(LocKeys.BossName(e.BossId)));
            m_HideAt = Time.unscaledTime + m_CaptionSeconds;
        }

        void Show(float health, float max)
        {
            if (m_Fill != null) m_Fill.fillAmount = max > 0f ? Mathf.Clamp01(health / max) : 0f;
        }
    }
}
