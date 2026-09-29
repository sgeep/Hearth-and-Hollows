using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// Placeholder presentation until the layered sprite rig exists: facing flip, squash and
    /// stretch, dodge/i-frame feedback, and a slash sprite over the active hitbox.
    /// </summary>
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] PlayerController m_Controller;
        [SerializeField] PlayerVitals m_Vitals;
        [SerializeField] Transform m_Body;
        [SerializeField] SpriteRenderer m_BodyRenderer;
        [SerializeField] SpriteRenderer m_Slash;
        [SerializeField] Color m_DodgeTint = new(0.6f, 0.85f, 1f, 1f);
        [SerializeField] Color m_HurtTint = new(1f, 0.4f, 0.4f, 1f);
        [SerializeField, Min(0)] float m_SquashRecovery = 12f;

        Color m_BaseColor;
        Vector3 m_Squash = Vector3.one;

        public void Configure(PlayerController controller, PlayerVitals vitals, Transform body, SpriteRenderer bodyRenderer, SpriteRenderer slash)
        {
            m_Controller = controller;
            m_Vitals = vitals;
            m_Body = body;
            m_BodyRenderer = bodyRenderer;
            m_Slash = slash;
        }

        void Start()
        {
            m_BaseColor = m_BodyRenderer.color;
            var motor = m_Controller.Motor;
            if (motor == null) return;
            motor.Jumped += () => m_Squash = new Vector3(0.75f, 1.3f, 1f);
            motor.WallJumped += () => m_Squash = new Vector3(0.75f, 1.3f, 1f);
            motor.Landed += () => m_Squash = new Vector3(1.3f, 0.7f, 1f);
            motor.DodgeStarted += () => m_Squash = new Vector3(1.25f, 0.6f, 1f);
        }

        void LateUpdate()
        {
            var motor = m_Controller.Motor;
            if (motor == null) return;

            m_Squash = Vector3.Lerp(m_Squash, Vector3.one, 1f - Mathf.Exp(-m_SquashRecovery * Time.deltaTime));
            var squash = motor.IsDodging ? new Vector3(1.25f, 0.6f, 1f) : m_Squash;
            m_Body.localScale = new Vector3(squash.x * motor.Facing, squash.y, 1f);

            Color color = m_BaseColor;
            if (m_Vitals != null && m_Vitals.IsStunned) color = m_HurtTint;
            else if (motor.IsInvulnerable) color = m_DodgeTint;
            if (m_Vitals != null && m_Vitals.IsInvulnerable && !motor.IsDodging)
                color.a = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f; // post-hit flicker
            m_BodyRenderer.color = color;

            bool showSlash = m_Controller.Combo != null && m_Controller.Combo.IsActive && m_Controller.TryGetActiveHitbox(out _, out _);
            m_Slash.enabled = showSlash;
            if (showSlash && m_Controller.TryGetActiveHitbox(out var center, out var size))
            {
                m_Slash.transform.position = center;
                m_Slash.transform.localScale = new Vector3(size.x * m_Controller.Facing, size.y, 1f);
            }
        }
    }
}
