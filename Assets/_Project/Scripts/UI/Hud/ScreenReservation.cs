using Hearthdelve.Core.Presentation;
using UnityEngine;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// Marks a HUD element's rectangle as spoken for while it shows (2026-10-07: the Essence bar, its icon and the run's gold), so
    /// world signs (a door's reward) step out from under it (<see cref="ScreenReservations"/>).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenReservation : MonoBehaviour
    {
        readonly Vector3[] m_Corners = new Vector3[4];
        RectTransform m_Rect;
        Canvas m_Canvas;

        void OnEnable()
        {
            m_Rect = (RectTransform)transform;
            m_Canvas = GetComponentInParent<Canvas>();
            ScreenReservations.Add(this, Area);
        }

        void OnDisable() => ScreenReservations.Remove(this);

        /// <summary>The element's rectangle on screen, in pixels (null while it isn't drawn).</summary>
        public Rect? Area()
        {
            if (m_Rect == null || !isActiveAndEnabled || m_Canvas == null || !m_Canvas.enabled) return null;
            Canvas root = m_Canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            m_Rect.GetWorldCorners(m_Corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, m_Corners[0]), b = RectTransformUtility.WorldToScreenPoint(camera, m_Corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
