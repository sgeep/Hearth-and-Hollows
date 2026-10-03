using UnityEngine;

namespace Hearthdelve.UI.Debugging
{
    /// <summary>Fades a debug line (the test floor's controls) out after a few seconds, so it doesn't sit over the HUD.</summary>
    public sealed class FadeOutAfter : MonoBehaviour
    {
        [SerializeField, Min(0f)] float m_Delay = 6f;
        [SerializeField, Min(0.01f)] float m_Fade = 1f;

        CanvasGroup m_Group;
        float m_Started;

        void OnEnable()
        {
            if (!TryGetComponent(out m_Group)) m_Group = gameObject.AddComponent<CanvasGroup>();
            m_Started = Time.unscaledTime;
            m_Group.alpha = 1f;
        }

        void Update() => m_Group.alpha = Mathf.Clamp01(1f - (Time.unscaledTime - m_Started - m_Delay) / m_Fade);
    }
}
