using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Hud
{
    /// <summary>The Essence meter on the HUD. Listens for <see cref="EssenceChanged"/>; never touches gameplay.</summary>
    public sealed class EssenceBar : MonoBehaviour
    {
        [SerializeField] Image m_Fill;
        [SerializeField] Color m_Normal = new(0.45f, 0.85f, 0.95f);
        [SerializeField] Color m_Low = new(0.95f, 0.35f, 0.3f);

        public float Shown { get; private set; } = 1f;
        /// <summary>True while Essence is low: the bar pulses.</summary>
        public bool IsLow { get; private set; }

        public void Configure(Image fill) => m_Fill = fill;

        void OnEnable() => EventBus<EssenceChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<EssenceChanged>.Unsubscribe(OnChanged);

        void OnChanged(EssenceChanged e)
        {
            Shown = e.Normalized;
            IsLow = e.IsLow;
            if (m_Fill == null) return;
            m_Fill.fillAmount = Shown;
            m_Fill.color = e.IsLow ? m_Low : m_Normal;
        }

        // Low Essence: the bar pulses between its low colour and a dimmer one (real time: it keeps
        // pulsing under the death screen's pause, and doesn't depend on hit-stop).
        void Update()
        {
            if (!IsLow || m_Fill == null) return;
            float pulse = Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * 1.5f) * 0.5f + 0.5f;
            m_Fill.color = Color.Lerp(m_Low * 0.55f, m_Low, pulse);
        }
    }
}
