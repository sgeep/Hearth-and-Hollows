using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.UI.World
{
    /// <summary>
    /// A speech bubble on the overlay canvas that follows a character in the world, so its
    /// text stays sharp while the game renders at the pixel-art resolution. Shown while the
    /// player stands within <see cref="m_ShowDistance"/> tiles of the speaker. It is placed after
    /// Cinemachine has moved the camera, from the camera position the Pixel Perfect Camera snaps
    /// to, in whole art pixels, so it moves in lockstep with the world instead of sliding over it.
    /// </summary>
    [DefaultExecutionOrder(1000)] // after the Cinemachine brain's LateUpdate
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
        PixelPerfectCamera m_PixelPerfect;
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
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera != null) m_PixelPerfect = m_Camera.GetComponent<PixelPerfectCamera>();
            }
            if (m_Player == null) m_Player = FindPlayer();

            bool near = m_Player != null && Vector2.Distance(m_Player.position, m_Speaker.position) <= m_ShowDistance;
            if (m_Visual.activeSelf != near) m_Visual.SetActive(near);
            if (!near || m_Camera == null) return;

            m_Rect.anchorMin = m_Rect.anchorMax = Vector2.zero;
            m_Rect.anchoredPosition = CanvasPosition(m_Speaker.position + (Vector3)m_Offset);
        }

        /// <summary>
        /// A world point's position on the canvas, measured from the camera position the world is
        /// actually drawn from (the snapped one) and rounded to whole art pixels.
        /// </summary>
        Vector2 CanvasPosition(Vector3 world)
        {
            Vector2 size = m_CanvasRect.rect.size;
            if (m_PixelPerfect == null || !m_Camera.orthographic)
            {
                Vector3 viewport = m_Camera.WorldToViewportPoint(world);
                return new Vector2(Mathf.Round(viewport.x * size.x), Mathf.Round(viewport.y * size.y));
            }

            int pixelsPerUnit = m_PixelPerfect.assetsPPU;
            Vector3 camera = m_PixelPerfect.RoundToPixel(m_Camera.transform.position);
            Vector2 fromCentre = (Vector2)(world - camera) * pixelsPerUnit;
            fromCentre = new Vector2(Mathf.Round(fromCentre.x), Mathf.Round(fromCentre.y));
            float viewHeight = 2f * m_Camera.orthographicSize * pixelsPerUnit;
            float viewWidth = viewHeight * m_Camera.aspect;
            return new Vector2((0.5f + fromCentre.x / viewWidth) * size.x, (0.5f + fromCentre.y / viewHeight) * size.y);
        }

        static Transform FindPlayer()
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }
    }
}
