using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Makes an incoming attack readable: a "!" icon (shape, not just colour, for colour-blind
    /// players), a pulsing body tint, and an optional ground marker where an attack will land.
    /// </summary>
    public sealed class TelegraphIndicator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Body;
        [SerializeField] SpriteRenderer m_Icon;
        [SerializeField] SpriteRenderer m_GroundMarker;
        [SerializeField] Color m_FlashColor = new(1f, 0.95f, 0.55f, 1f);
        [SerializeField, Min(0)] float m_PulseRate = 14f;

        Color m_BaseColor;
        bool m_Showing;
        float m_Timer;
        Vector2 m_MarkerPosition;

        public void Configure(SpriteRenderer body, SpriteRenderer icon, SpriteRenderer groundMarker, Color? flashColor = null)
        {
            m_Body = body;
            m_Icon = icon;
            m_GroundMarker = groundMarker;
            if (flashColor.HasValue) m_FlashColor = flashColor.Value;
        }

        void Awake()
        {
            if (m_Body != null) m_BaseColor = m_Body.color;
            Hide();
            HideMarker();
        }

        public void Show()
        {
            m_Showing = true;
            m_Timer = 0f;
            if (m_Icon != null) m_Icon.enabled = true;
        }

        /// <summary>Show the landing marker at a world position (slime leap).</summary>
        public void ShowMarker(Vector2 worldPosition)
        {
            if (m_GroundMarker == null) return;
            m_MarkerPosition = worldPosition;
            m_GroundMarker.transform.position = worldPosition;
            m_GroundMarker.enabled = true;
        }

        public void HideMarker()
        {
            if (m_GroundMarker != null) m_GroundMarker.enabled = false;
        }

        public void Hide()
        {
            m_Showing = false;
            if (m_Icon != null) m_Icon.enabled = false;
            if (m_Body != null) m_Body.color = m_BaseColor;
        }

        void LateUpdate()
        {
            // The marker is a child for convenience but must stay where the attack will land.
            if (m_GroundMarker != null && m_GroundMarker.enabled) m_GroundMarker.transform.position = m_MarkerPosition;
        }

        void Update()
        {
            if (!m_Showing || m_Body == null) return;
            m_Timer += Time.deltaTime;
            float t = 0.5f + 0.5f * Mathf.Sin(m_Timer * m_PulseRate);
            m_Body.color = Color.Lerp(m_BaseColor, m_FlashColor, t);
            if (m_Icon != null) m_Icon.transform.localScale = Vector3.one * (1f + 0.15f * t);
        }
    }
}
