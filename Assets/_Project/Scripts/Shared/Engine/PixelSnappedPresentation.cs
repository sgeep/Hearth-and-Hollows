using Hearthdelve.Core.Movement;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// The point the camera follows (<see cref="Anchor"/>), plus optional presentation-only pixel
    /// snapping for the player. The game scrolls smoothly (CLAUDE.md, Camera), so snapping is off and
    /// the anchor sits at the player's real position. The look-test comparison modes (F4) that snap
    /// the camera to a pixel grid turn it on: the model is then drawn at a display position on that
    /// grid (see <see cref="PixelStepping"/>) and the anchor follows it, so the player stays on one
    /// screen pixel and diagonals scroll in clean steps. Gameplay positions are never touched.
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
        public Vector2 DisplayPosition => Snapping ? (Vector2)m_Display / m_PixelsPerUnit : (Vector2)transform.position;

        /// <summary>The grid the player is drawn on, in grid steps per world unit (follows the camera's pixel grid).</summary>
        public int PixelsPerUnit
        {
            get => m_PixelsPerUnit;
            set
            {
                value = Mathf.Max(1, value);
                if (value == m_PixelsPerUnit) return;
                m_PixelsPerUnit = value;
                m_HasDisplay = false;
            }
        }

        /// <summary>
        /// Draw the player on the camera's pixel grid. Off for smooth scrolling (the game's camera);
        /// only a camera that snaps to a grid needs it.
        /// </summary>
        public bool Snapping { get; set; }

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
            if (!Snapping)
            {
                m_HasDisplay = false;
                if (m_Model != null) m_Model.localPosition = m_ModelRest;
                m_Anchor.localPosition = Vector3.zero;
                return;
            }

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
