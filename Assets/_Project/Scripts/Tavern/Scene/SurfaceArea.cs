using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>How the camera behaves in an area: holding still on a one-screen room, or following the keeper inside bounds.</summary>
    public enum SurfaceCameraMode
    {
        Hold,
        Follow,
    }

    /// <summary>
    /// One place on the surface the keeper can be in (4h): a room of Tally Ho! (beside its <see cref="PropertyArea"/>) or
    /// the village. It owns how the camera shows it and which global lights are on while the keeper is there: only the
    /// current area's global lights are lit, so two scenes' ambient lights never stack over the same sprites. Areas in
    /// different scenes find each other through the static list (H1: Kariaston and the tavern are loaded side by side).
    /// </summary>
    public sealed class SurfaceArea : MonoBehaviour
    {
        public const string TavernId = "tavern";
        public const string GuestRoomId = "guest_room";
        public const string KariastonId = "kariaston";

        static readonly List<SurfaceArea> s_All = new();
        static SurfaceArea s_Current;

        [SerializeField] string m_Id = TavernId;
        [SerializeField, Tooltip("Inside Tally Ho! (the clock can be tuned to pause indoors).")]
        bool m_Indoors = true;
        [SerializeField, Tooltip("Where the keeper is when no area has been entered yet (the tavern's main room).")]
        bool m_Default;
        [SerializeField] SurfaceCameraMode m_Camera = SurfaceCameraMode.Hold;
        [SerializeField, Tooltip("Hold: where the camera holds (world).")]
        Vector2 m_HoldPoint;
        [SerializeField, Tooltip("Follow: the rectangle the view stays inside (world).")]
        Rect m_Bounds;
        [SerializeField, Tooltip("This area's global lights: lit only while the keeper is here.")]
        Light2D[] m_Lights = Array.Empty<Light2D>();

        public static IReadOnlyList<SurfaceArea> All => s_All;
        public string Id => m_Id;
        public bool Indoors => m_Indoors;
        public SurfaceCameraMode CameraMode => m_Camera;
        public Vector2 HoldPoint => m_HoldPoint;
        public Rect Bounds => m_Bounds;
        public IReadOnlyList<Light2D> Lights => m_Lights;

        /// <summary>Where the keeper is: the area last entered, or the default (the tavern) while none has been.</summary>
        public static SurfaceArea Current
        {
            get
            {
                if (s_Current != null && s_Current.isActiveAndEnabled) return s_Current;
                foreach (SurfaceArea a in s_All)
                    if (a.m_Default) return a;
                return null;
            }
        }

        /// <summary>The keeper moved into another area (after the camera and lights have switched).</summary>
        public static event Action<SurfaceArea> Entered;

        public static SurfaceArea Find(string id)
        {
            foreach (SurfaceArea a in s_All)
                if (a.m_Id == id) return a;
            return null;
        }

        public void Configure(string id, bool indoors, bool isDefault, SurfaceCameraMode camera, Vector2 holdPoint, Rect bounds, params Light2D[] lights)
        {
            m_Id = id;
            m_Indoors = indoors;
            m_Default = isDefault;
            m_Camera = camera;
            m_HoldPoint = holdPoint;
            m_Bounds = bounds;
            m_Lights = lights ?? Array.Empty<Light2D>();
        }

        /// <summary>The keeper is now in <paramref name="area"/>: lights, the clock's indoor flag, the camera.</summary>
        public static void Enter(SurfaceArea area)
        {
            if (area == null) return;
            s_Current = area;
            ApplyLights();
            SurfaceTime.Indoors = area.m_Indoors;
            SurfaceCamera.Snap();
            Entered?.Invoke(area);
        }

        /// <summary>Lights the current area's global lights and darkens every other area's (a light two areas share stays lit).</summary>
        public static void ApplyLights()
        {
            SurfaceArea current = Current;
            if (current == null) return;
            // Off first, then on: two global lights on the same layers are never lit together, even for a moment (URP refuses it).
            foreach (SurfaceArea a in s_All)
            foreach (Light2D light in a.m_Lights)
                if (light != null && Array.IndexOf(current.m_Lights, light) < 0) light.enabled = false;
            foreach (Light2D light in current.m_Lights)
                if (light != null) light.enabled = true;
        }

        /// <summary>Tests and leaving the game: forget the area entered.</summary>
        public static void ResetCurrent() => s_Current = null;

        void OnEnable()
        {
            s_All.Add(this);
            // An area loaded after the keeper is somewhere else starts dark (the village loads beside the lit tavern).
            ApplyLights();
        }

        void OnDisable()
        {
            s_All.Remove(this);
            if (s_Current == this) s_Current = null;
        }

        void OnDrawGizmosSelected()
        {
            if (m_Camera != SurfaceCameraMode.Follow) return;
            Gizmos.color = new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
        }
    }
}
