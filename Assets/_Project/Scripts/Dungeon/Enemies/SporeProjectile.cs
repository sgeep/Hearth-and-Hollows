using System.Collections.Generic;
using Hearthdelve.Dungeon.Combat;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>Ballistic spore blob. Hurts the player on contact, pops on terrain.</summary>
    public sealed class SporeProjectile : MonoBehaviour
    {
        [SerializeField, Min(0)] float m_Gravity = 20f;
        [SerializeField, Min(0.05f)] float m_Radius = 0.2f;
        [SerializeField] LayerMask m_TerrainMask;
        [SerializeField, Min(0)] float m_Lifetime = 4f;

        Vector2 m_Velocity;
        float m_Damage;
        LayerMask m_PlayerMask;
        GameObject m_Owner;
        float m_Age;
        readonly List<Collider2D> m_Overlaps = new(4);

        public void ConfigureTerrain(LayerMask terrain) => m_TerrainMask = terrain;

        /// <summary>Fire from <paramref name="from"/> to land on <paramref name="to"/> after <paramref name="flightTime"/>.</summary>
        public void Launch(Vector2 from, Vector2 to, float flightTime, float damage, LayerMask playerMask, GameObject owner)
        {
            transform.position = from;
            m_Velocity = new Vector2((to.x - from.x) / flightTime, (to.y - from.y) / flightTime + 0.5f * m_Gravity * flightTime);
            m_Damage = damage;
            m_PlayerMask = playerMask;
            m_Owner = owner;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            m_Age += dt;
            if (m_Age > m_Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            m_Velocity.y -= m_Gravity * dt;
            Vector2 pos = (Vector2)transform.position + m_Velocity * dt;
            transform.position = pos;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(m_PlayerMask);
            if (Physics2D.OverlapCircle(pos, m_Radius, filter, m_Overlaps) > 0)
            {
                foreach (var col in m_Overlaps)
                {
                    var target = col.GetComponentInParent<IDamageable>();
                    if (target == null || target.Team != Team.Player) continue;
                    var hit = new DamageInfo
                    {
                        Amount = m_Damage,
                        Knockback = new Vector2(Mathf.Sign(m_Velocity.x) * 5f, 4f),
                        HitPoint = pos,
                        Instigator = m_Owner,
                    };
                    if (target.ReceiveHit(hit)) break;
                }
                Destroy(gameObject);
                return;
            }

            filter.SetLayerMask(m_TerrainMask);
            if (m_Age > 0.1f && Physics2D.OverlapCircle(pos, m_Radius, filter, m_Overlaps) > 0)
                Destroy(gameObject);
        }
    }
}
