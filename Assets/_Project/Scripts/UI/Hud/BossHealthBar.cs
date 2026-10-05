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
        [SerializeField] LocalizedSuperText m_Name;
        [SerializeField] Image m_Fill;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public float Fraction => m_Fill != null ? m_Fill.fillAmount : 0f;
        public string BossId { get; private set; }

        public void Configure(GameObject root, LocalizedSuperText name, Image fill)
        {
            m_Root = root;
            m_Name = name;
            m_Fill = fill;
        }

        void Awake()
        {
            if (m_Root != null) m_Root.SetActive(false);
        }

        void OnEnable()
        {
            EventBus<BossEncounterStarted>.Subscribe(OnStarted);
            EventBus<BossHealthChanged>.Subscribe(OnHealth);
            EventBus<BossEncounterEnded>.Subscribe(OnEnded);
        }

        void OnDisable()
        {
            EventBus<BossEncounterStarted>.Unsubscribe(OnStarted);
            EventBus<BossHealthChanged>.Unsubscribe(OnHealth);
            EventBus<BossEncounterEnded>.Unsubscribe(OnEnded);
        }

        void OnStarted(BossEncounterStarted e)
        {
            BossId = e.BossId;
            m_Name?.Set(LocKeys.BossName(e.BossId));
            Show(e.Health, e.MaxHealth);
            if (m_Root != null) m_Root.SetActive(true);
        }

        void OnHealth(BossHealthChanged e) => Show(e.Health, e.MaxHealth);

        void OnEnded(BossEncounterEnded e)
        {
            if (m_Root != null) m_Root.SetActive(false);
        }

        void Show(float health, float max)
        {
            if (m_Fill != null) m_Fill.fillAmount = max > 0f ? Mathf.Clamp01(health / max) : 0f;
        }
    }
}
