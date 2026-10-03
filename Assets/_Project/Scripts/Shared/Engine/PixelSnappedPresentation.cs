using Hearthdelve.Core.Movement;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Presentation-only pixel snapping for the player (CLAUDE.md, Camera and pixel-perfect). The
    /// character's gameplay position (rigidbody, colliders) is untouched. Its model is drawn at a
    /// display position on the art-pixel grid (see <see cref="PixelStepping"/>), and the camera
    /// follows <see cref="Anchor"/>, which sits at that same position. The camera therefore lands
    /// exactly on the grid, the player stays on one screen pixel, and diagonal movement scrolls the
    /// world in clean diagonal steps.
    /// </summary>
    [DefaultExecutionOrder(-100)] // after interpolation has moved the player, before Cinemachine's LateUpdate
    public sealed class PixelSnappedPresentation : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Art pixels per world unit (the Pixel Perfect Camera's assets PPU).")]
        int m_PixelsPerUnit = 8;

        Transform m_Model;
        Vector3 m_ModelRest;
        Transform m_Anchor;
        Vector2Int m_Display;
        PixelStepping.State m_Stepping;
        Vector2 m_LastTarget;
        bool m_HasDisplay;

        /// <summary>What the camera follows: the display position, on the art-pixel grid.</summary>
        public Transform Anchor => m_Anchor;

        /// <summary>Where the player is drawn, in world units.</summary>
        public Vector2 DisplayPosition => (Vector2)m_Display / m_PixelsPerUnit;

        public int PixelsPerUnit => m_PixelsPerUnit;

        void Awake()
        {
            var character = GetComponent<Character>();
            if (character != null && character.CharacterModel != null)
            {
                m_Model = character.CharacterModel.transform;
                m_ModelRest = m_Model.localPosition;
            }
            m_Anchor = new GameObject("PixelAnchor").transform;
            m_Anchor.SetParent(transform, false);
        }

        void OnEnable() => m_HasDisplay = false;

        void LateUpdate()
        {
            Vector2 target = (Vector2)transform.position * m_PixelsPerUnit;
            if (m_HasDisplay) m_Display = PixelStepping.Step(m_Display, target, target - m_LastTarget, ref m_Stepping);
            else
            {
                m_Display = Vector2Int.RoundToInt(target);
                m_Stepping = default;
            }
            m_HasDisplay = true;
            m_LastTarget = target;

            Vector3 offset = (Vector3)(DisplayPosition - (Vector2)transform.position);
            if (m_Model != null) m_Model.localPosition = m_ModelRest + offset;
            m_Anchor.localPosition = offset;
        }
    }
}
