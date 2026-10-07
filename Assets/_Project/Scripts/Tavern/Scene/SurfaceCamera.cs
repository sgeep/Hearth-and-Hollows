using Hearthdelve.Shared.Engine;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The surface's camera (4h), on the tavern's Cinemachine camera: it holds still on a one-screen room (as the tavern
    /// always has) and, in the village, follows the keeper's display position with no damping, kept inside the area's
    /// bounds (<see cref="ViewBounds"/>). It moves the virtual camera's own transform before the brain runs (LateUpdate,
    /// early), so Cinemachine and the pixel-perfect correction still own the final view (CLAUDE.md, camera and scrolling).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SurfaceCamera : MonoBehaviour
    {
        static SurfaceCamera s_Instance;

        Transform m_Target;

        void OnEnable() => s_Instance = this;

        void OnDisable()
        {
            if (s_Instance == this) s_Instance = null;
        }

        /// <summary>Puts the camera where the current area wants it at once (after a door or the stairs).</summary>
        public static void Snap()
        {
            if (s_Instance != null) s_Instance.Place(true);
        }

        void LateUpdate() => Place(false);

        void Place(bool snap)
        {
            SurfaceArea area = SurfaceArea.Current;
            if (area == null) return;
            Vector2 at;
            if (area.CameraMode == SurfaceCameraMode.Hold)
            {
                if (!snap) return;
                at = area.HoldPoint;
            }
            else
            {
                Transform target = Target();
                if (target == null) return;
                Camera main = Camera.main;
                float halfHeight = main != null ? main.orthographicSize : 11.25f;
                float halfWidth = halfHeight * (main != null ? main.aspect : 16f / 9f);
                at = ViewBounds.Clamp(area.Bounds, target.position, halfWidth, halfHeight);
            }
            transform.position = new Vector3(at.x, at.y, transform.position.z);
            if (snap && Camera.main != null) Camera.main.transform.position = new Vector3(at.x, at.y, Camera.main.transform.position.z);
        }

        /// <summary>The keeper's display position (the pixel-snapped anchor when there is one), found once.</summary>
        Transform Target()
        {
            if (m_Target != null) return m_Target;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return null;
            var snapped = player.GetComponent<PixelSnappedPresentation>();
            m_Target = snapped != null && snapped.Anchor != null ? snapped.Anchor : player.transform;
            return m_Target;
        }
    }
}
