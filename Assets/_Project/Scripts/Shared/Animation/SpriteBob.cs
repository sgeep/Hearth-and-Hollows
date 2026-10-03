using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    /// <summary>Bobs a sprite up and down by whole art pixels (a marker over a target). Presentation only.</summary>
    public sealed class SpriteBob : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Height of the bob, in art pixels.")]
        int m_Pixels = 1;
        [SerializeField, Min(0.01f), Tooltip("Seconds for one bob up and down.")]
        float m_Period = 0.8f;
        [SerializeField] int m_PixelsPerUnit = 8;

        Vector3 m_Rest;

        void Awake() => m_Rest = transform.localPosition;

        void OnEnable() => transform.localPosition = m_Rest;

        void Update()
        {
            float up = Mathf.PingPong(Time.unscaledTime * 2f * m_Pixels / m_Period, m_Pixels);
            transform.localPosition = m_Rest + Vector3.up * (Mathf.Round(up) / m_PixelsPerUnit);
        }
    }
}
