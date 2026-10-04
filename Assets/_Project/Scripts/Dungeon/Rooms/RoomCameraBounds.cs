using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// Keeps the camera's view inside the current room (4d). Rooms are rectangles, so this clamps the camera's
    /// position against the room's edges using the lens as it will render (after <see cref="CinemachinePixelPerfect"/>
    /// has corrected its size); where the view is wider or taller than the room it centres on the room instead.
    /// Immediate and exact, with no shape to bake (Cinemachine's 2D confiner bakes its shape over several frames).
    /// </summary>
    [AddComponentMenu("Hearthdelve/Room Camera Bounds")]
    public sealed class RoomCameraBounds : CinemachineExtension
    {
        [SerializeField, Tooltip("The room the view stays inside, in world units.")]
        Rect m_Bounds = new(0f, 0f, 40f, 24f);
        [SerializeField, Tooltip("Off: the camera follows freely (outside a room).")]
        bool m_Active;

        public Rect Bounds => m_Bounds;
        public bool IsActive => m_Active;

        public void SetBounds(Rect bounds)
        {
            m_Bounds = bounds;
            m_Active = true;
        }

        public void Clear() => m_Active = false;

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (!m_Active || stage != CinemachineCore.Stage.Finalize || !state.Lens.Orthographic) return;
            float halfHeight = state.Lens.OrthographicSize;
            float halfWidth = halfHeight * state.Lens.Aspect;
            Vector3 position = state.GetFinalPosition();
            Vector2 clamped = RoomView.Clamp(m_Bounds, position, halfWidth, halfHeight);
            state.PositionCorrection += new Vector3(clamped.x - position.x, clamped.y - position.y, 0f);
        }
    }
}
