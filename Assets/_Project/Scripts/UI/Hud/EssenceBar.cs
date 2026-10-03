using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// The Essence meter on the HUD. Listens for <see cref="EssenceChanged"/>; never touches gameplay.
    /// Low Essence switches to the red fill and pulses (two cues, not just a colour); taking damage
    /// flashes the bar.
    /// </summary>
    public sealed class EssenceBar : MonoBehaviour
    {
        const float k_FlashTime = 0.15f;

        [SerializeField] Image m_Fill;
        [SerializeField] Color m_Normal = new(0.45f, 0.85f, 0.95f);
        [SerializeField] Color m_Low = new(0.95f, 0.35f, 0.3f);
        [SerializeField, Tooltip("Optional art: the fill sprite normally and when low (the colours above are then white).")]
        Sprite m_NormalSprite;
        [SerializeField] Sprite m_LowSprite;
        [SerializeField, Tooltip("Optional: an overlay flashed when damage is taken.")]
        Image m_Flash;

        float m_FlashUntil = float.NegativeInfinity;

        public float Shown { get; private set; } = 1f;
        /// <summary>True while Essence is low: the bar pulses.</summary>
        public bool IsLow { get; private set; }
        /// <summary>True for a moment after damage.</summary>
        public bool IsFlashing => Time.unscaledTime < m_FlashUntil;

        public void Configure(Image fill) => m_Fill = fill;

        /// <summary>Uses Minifantasy fill sprites (normal and low) and a damage-flash overlay.</summary>
        public void ConfigureArt(Sprite normal, Sprite low, Image flash)
        {
            m_NormalSprite = normal;
            m_LowSprite = low;
            m_Flash = flash;
            m_Normal = Color.white;
            m_Low = Color.white;
        }

        void OnEnable() => EventBus<EssenceChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<EssenceChanged>.Unsubscribe(OnChanged);

        void OnChanged(EssenceChanged e)
        {
            Shown = e.Normalized;
            IsLow = e.IsLow;
            if (e.FromDamage) m_FlashUntil = Time.unscaledTime + k_FlashTime;
            if (m_Fill == null) return;
            m_Fill.fillAmount = Shown;
            if (m_NormalSprite != null) m_Fill.sprite = IsLow && m_LowSprite != null ? m_LowSprite : m_NormalSprite;
        }

        // Colour after every change of the frame (Essence changes every frame while it drains). Real time:
        // it keeps pulsing under the death screen's pause, and doesn't depend on hit-stop.
        void LateUpdate()
        {
            if (m_Flash != null) m_Flash.enabled = IsFlashing;
            if (m_Fill == null) return;
            if (!IsLow)
            {
                m_Fill.color = m_Normal;
                return;
            }
            float pulse = Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * 1.5f) * 0.5f + 0.5f;
            Color dim = m_Low * 0.55f;
            dim.a = 1f;
            m_Fill.color = Color.Lerp(dim, m_Low, pulse);
        }
    }
}
