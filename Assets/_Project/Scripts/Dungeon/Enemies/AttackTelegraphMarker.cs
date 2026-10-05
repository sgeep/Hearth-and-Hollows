using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// A mark on the floor during an attack's telegraph (4e, the Larder Troll): where a slam will land (the hitbox's
    /// area) or the line a charge will run along. It grows in over the telegraph, so its fullness says "now". Big
    /// attacks need it: an alert over the head doesn't say where at 320×180.
    /// </summary>
    [RequireComponent(typeof(EnemyAttack))]
    public sealed class AttackTelegraphMarker : MonoBehaviour
    {
        public enum Shape
        {
            /// <summary>The attack's hitbox, placed as the bite places it.</summary>
            Area,
            /// <summary>A strip from the enemy along the attack's direction, as long as its travel.</summary>
            Line,
        }

        [SerializeField, Tooltip("Which EnemyAttack (by index among this enemy's attacks) it marks.")]
        int m_AttackIndex;
        [SerializeField] Shape m_Shape;
        [SerializeField, Tooltip("A sprite renderer (a 1×1 white pixel, tinted) on the floor's sorting layer.")]
        SpriteRenderer m_Mark;
        [SerializeField] Color m_Colour = new(0.9f, 0.15f, 0.1f, 0.45f);
        [SerializeField, Min(0.1f), Tooltip("Width of a line mark, in tiles.")]
        float m_LineWidth = 1.2f;

        EnemyAttack m_Attack;
        bool m_Showing;

        public bool IsShowing => m_Showing;

        public void Configure(int attackIndex, Shape shape, SpriteRenderer mark)
        {
            m_AttackIndex = attackIndex;
            m_Shape = shape;
            m_Mark = mark;
        }

        void Awake()
        {
            EnemyAttack[] attacks = GetComponents<EnemyAttack>();
            m_Attack = m_AttackIndex >= 0 && m_AttackIndex < attacks.Length ? attacks[m_AttackIndex] : null;
            if (m_Attack != null) m_Attack.PhaseChanged += OnPhase;
            if (m_Mark != null) m_Mark.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Attack != null) m_Attack.PhaseChanged -= OnPhase;
        }

        void OnPhase(EnemyAttackPhase phase)
        {
            m_Showing = phase == EnemyAttackPhase.Telegraph;
            if (m_Mark == null) return;
            m_Mark.gameObject.SetActive(m_Showing);
            if (!m_Showing) return;
            // Placed once, at the telegraph's start: the attack is committed to it.
            EnemyAttackSettings s = m_Attack.Settings;
            Vector2 direction = m_Attack.Direction;
            Transform mark = m_Mark.transform;
            // Sizes are in tiles; the mark's sprite may be any size (a 1-pixel sprite is an eighth of a tile).
            Vector2 unit = m_Mark.sprite != null ? (Vector2)m_Mark.sprite.bounds.size : Vector2.one;
            unit = new Vector2(Mathf.Max(1e-4f, unit.x), Mathf.Max(1e-4f, unit.y));
            if (m_Shape == Shape.Area)
            {
                mark.position = (Vector2)transform.position + direction * s.hitboxOffset.x + Vector2.up * s.hitboxOffset.y;
                mark.rotation = Quaternion.identity;
                mark.localScale = new Vector3(s.hitboxSize.x / unit.x, s.hitboxSize.y / unit.y, 1f);
            }
            else
            {
                // As far as the charge can go: to the first wall, pillar or prop in its way (where it will stop, stunned).
                Vector2 origin = (Vector2)transform.position + Vector2.up * s.hitboxOffset.y;
                RaycastHit2D hit = Physics2D.CircleCast(origin, m_LineWidth * 0.4f, direction, s.travelDistance, LayerMask.GetMask(Hearthdelve.Core.Layers.Obstacles));
                float length = hit.collider != null ? Mathf.Max(0.5f, hit.distance) : s.travelDistance;
                mark.position = origin + direction * (length / 2f);
                mark.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                mark.localScale = new Vector3(length / unit.x, m_LineWidth / unit.y, 1f);
            }
            m_Mark.color = new Color(m_Colour.r, m_Colour.g, m_Colour.b, 0f);
        }

        void Update()
        {
            if (!m_Showing || m_Mark == null || m_Attack.Cycle == null) return;
            float t = m_Attack.Settings.telegraph > 0f ? Mathf.Clamp01(m_Attack.Cycle.PhaseElapsed / m_Attack.Settings.telegraph) : 1f;
            m_Mark.color = new Color(m_Colour.r, m_Colour.g, m_Colour.b, m_Colour.a * Mathf.Lerp(0.25f, 1f, t));
        }
    }
}
