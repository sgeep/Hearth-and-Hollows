using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// A brief "saved" in the corner each time the game saves (4i-A, A6): there a moment, then gone, in real time (a paused or
    /// slowed game still shows it). Never in the way: no input, no sound.
    /// </summary>
    public sealed class SaveIndicator : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_Group;
        [SerializeField, Min(0.1f)] float m_Hold = 1.2f;
        [SerializeField, Min(0.01f)] float m_Fade = 0.4f;

        float m_ShownAt = float.NegativeInfinity;

        public bool IsShowing => m_Group != null && m_Group.alpha > 0f;

        public void Configure(CanvasGroup group) => m_Group = group;

        void Awake()
        {
            if (m_Group != null) m_Group.alpha = 0f;
        }

        void OnEnable() => EventBus<GameSaved>.Subscribe(OnSaved);
        void OnDisable() => EventBus<GameSaved>.Unsubscribe(OnSaved);

        void OnSaved(GameSaved _) => m_ShownAt = Time.unscaledTime;

        void Update()
        {
            if (m_Group == null) return;
            float age = Time.unscaledTime - m_ShownAt;
            m_Group.alpha = age < m_Hold ? 1f : Mathf.Clamp01(1f - (age - m_Hold) / m_Fade);
        }
    }
}
