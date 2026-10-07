using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Where the player is looking, for a character with no weapon to aim (the tavern keeper, 4e playtest): toward the mouse
    /// on keyboard and mouse, or along the right stick on a gamepad. Zero when there's nothing to look at (the right stick
    /// centred, or the gameplay map off for a station, a menu or a minigame), so the character faces where it walks.
    /// The <see cref="Hearthdelve.Shared.Animation.CharacterSpriteAnimator"/> turns the character to it; presentation only.
    /// </summary>
    [RequireComponent(typeof(Character))]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("The mouse this close to the character's middle, in tiles, is ignored (it would flicker the facing).")]
        float m_DeadZone = 0.4f;
        [SerializeField, Tooltip("The character's middle above its feet, in tiles: where the look is measured from.")]
        float m_EyeHeight = 0.6f;

        Character m_Character;

        /// <summary>The direction looked in, normalised, or zero.</summary>
        public Vector2 Direction { get; private set; }

        Vector2 m_Held;
        float m_HeldUntil;

        void Awake() => m_Character = GetComponent<Character>();

        /// <summary>Faces <paramref name="direction"/> for a moment, whatever the mouse or stick say (4h: working a garden bed).</summary>
        public void Hold(Vector2 direction, float seconds)
        {
            if (direction.sqrMagnitude < 1e-4f) return;
            m_Held = direction.normalized;
            m_HeldUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        }

        void Update()
        {
            Direction = Vector2.zero;
            if (Time.unscaledTime < m_HeldUntil)
            {
                Direction = m_Held;
                return;
            }
            if (m_Character.LinkedInputManager is not HearthdelveInputManager input || !input.GameplayMapActive) return;
            if (input.PointerAim)
            {
                Camera camera = Camera.main;
                if (camera == null) return;
                Vector2 pointer = camera.ScreenToWorldPoint(input.MousePosition);
                Vector2 look = pointer - ((Vector2)transform.position + new Vector2(0f, m_EyeHeight));
                if (look.magnitude > m_DeadZone) Direction = look.normalized;
            }
            else if (input.SecondaryMovement.sqrMagnitude > 0.04f)
            {
                Direction = input.SecondaryMovement.normalized;
            }
        }
    }
}
