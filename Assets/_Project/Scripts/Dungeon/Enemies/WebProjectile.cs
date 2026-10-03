using Hearthdelve.Core;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// The spider's web: flies straight, hurts the player through its <see cref="DamageOnTouch"/>,
    /// and is stopped by walls and props. Its sprite is one of eight drawn directions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WebProjectile : MonoBehaviour
    {
        [SerializeField, Tooltip("Sprites for E, NE, N, NW, W, SW, S, SE (counter-clockwise from east).")]
        Sprite[] m_Directions = new Sprite[8];
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] DamageOnTouch m_Damage;
        [SerializeField, Min(0.01f)] float m_Radius = 0.15f;

        Rigidbody2D m_Body;
        Vector2 m_Velocity;
        float m_Remaining;
        int m_Obstacles;

        public void Configure(Sprite[] directions, SpriteRenderer renderer, DamageOnTouch damage)
        {
            m_Directions = directions;
            m_Renderer = renderer;
            m_Damage = damage;
        }

        /// <summary>Sends the web off. <paramref name="owner"/> is never hit by it.</summary>
        public void Launch(Vector2 direction, float speed, float range, float damage, GameObject owner)
        {
            m_Body = GetComponent<Rigidbody2D>();
            m_Velocity = direction.normalized * speed;
            m_Remaining = range / Mathf.Max(0.01f, speed);
            m_Obstacles = LayerMask.GetMask(Layers.Obstacles);
            if (m_Damage != null)
            {
                m_Damage.MinDamageCaused = damage;
                m_Damage.MaxDamageCaused = damage;
                if (owner != null) m_Damage.IgnoreGameObject(owner);
                m_Damage.HitDamageableEvent ??= new UnityEvent<Health>();
                m_Damage.HitDamageableEvent.AddListener(_ => Destroy(gameObject));
            }
            if (m_Renderer != null && m_Directions != null && m_Directions.Length == 8)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                int index = (Mathf.RoundToInt(angle / 45f) % 8 + 8) % 8;
                m_Renderer.sprite = m_Directions[index];
            }
        }

        void FixedUpdate()
        {
            if (m_Body == null) return;
            m_Remaining -= Time.fixedDeltaTime;
            Vector2 next = m_Body.position + m_Velocity * Time.fixedDeltaTime;
            if (m_Remaining <= 0f || Physics2D.OverlapCircle(next, m_Radius, m_Obstacles) != null)
            {
                Destroy(gameObject);
                return;
            }
            m_Body.MovePosition(next);
        }
    }
}
