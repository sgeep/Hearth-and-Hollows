using UnityEngine;

namespace Hearthdelve.UI.World
{
    /// <summary>
    /// A speech bubble on the overlay canvas that follows a character in the world, so its
    /// text stays sharp while the game renders at the pixel-art resolution. Shown while the
    /// player stands within <see cref="m_ShowDistance"/> tiles of the speaker.
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] Transform m_Speaker;
        [SerializeField, Tooltip("World offset from the speaker's feet to the bubble's tail, in tiles.")]
        Vector2 m_Offset = new(0f, 1.5f);
        [SerializeField, Min(0f)] float m_ShowDistance = 4f;
        [SerializeField] GameObject m_Visual;

        RectTransform m_Rect;
        RectTransform m_CanvasRect;
        Camera m_Camera;
        Transform m_Player;

        public bool IsVisible => m_Visual != null && m_Visual.activeSelf;

        public void Configure(Transform speaker, GameObject visual, Vector2 offset, float showDistance)
        {
            m_Speaker = speaker;
            m_Visual = visual;
            m_Offset = offset;
            m_ShowDistance = showDistance;
        }

        void Awake()
        {
            m_Rect = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) m_CanvasRect = (RectTransform)canvas.rootCanvas.transform;
        }

        void LateUpdate()
        {
            if (m_Speaker == null || m_Visual == null || m_CanvasRect == null) return;
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Player == null) m_Player = FindPlayer();

            bool near = m_Player != null && Vector2.Distance(m_Player.position, m_Speaker.position) <= m_ShowDistance;
            if (m_Visual.activeSelf != near) m_Visual.SetActive(near);
            if (!near || m_Camera == null) return;

            Vector3 viewport = m_Camera.WorldToViewportPoint(m_Speaker.position + (Vector3)m_Offset);
            Vector2 size = m_CanvasRect.rect.size;
            m_Rect.anchorMin = m_Rect.anchorMax = Vector2.zero;
            m_Rect.anchoredPosition = new Vector2(Mathf.Round(viewport.x * size.x), Mathf.Round(viewport.y * size.y));
        }

        static Transform FindPlayer()
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }
    }
}
