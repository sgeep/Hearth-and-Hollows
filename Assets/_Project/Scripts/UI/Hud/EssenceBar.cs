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

        public void Configure(Image fill) => m_Fill = fill;

        void OnEnable() => EventBus<EssenceChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<EssenceChanged>.Unsubscribe(OnChanged);

        void OnChanged(EssenceChanged e)
        {
            Shown = e.Normalized;
            if (m_Fill == null) return;
            m_Fill.fillAmount = Shown;
            m_Fill.color = e.IsLow ? m_Low : m_Normal;
        }
    }
}
