using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Box hitbox swept every physics step while an attack is active. Each target is hit at
    /// most once per activation. Targets are found by layer and must implement <see cref="IDamageable"/>.
    /// </summary>
    public sealed class MeleeHitbox
    {
        readonly Team m_OwnerTeam;
        readonly LayerMask m_TargetMask;
        readonly HashSet<IDamageable> m_AlreadyHit = new();
        readonly List<Collider2D> m_Overlaps = new(8);

        public MeleeHitbox(Team ownerTeam, LayerMask targetMask)
        {
            m_OwnerTeam = ownerTeam;
            m_TargetMask = targetMask;
        }

        /// <summary>Start a new activation (clears the already-hit set).</summary>
        public void Begin() => m_AlreadyHit.Clear();

        /// <summary>Hit everything new in the box. Returns how many hits landed.</summary>
        public int Sweep(Vector2 center, Vector2 size, DamageInfo hit, List<IDamageable> landed = null)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(m_TargetMask);
            int count = Physics2D.OverlapBox(center, size, 0f, filter, m_Overlaps);
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                var target = m_Overlaps[i].GetComponentInParent<IDamageable>();
                if (target == null || target.Team == m_OwnerTeam || !m_AlreadyHit.Add(target)) continue;
                hit.HitPoint = m_Overlaps[i].ClosestPoint(center);
                if (target.ReceiveHit(hit))
                {
                    hits++;
                    landed?.Add(target);
                }
            }
            return hits;
        }

        /// <summary>World-space centre for a hitbox authored for a right-facing owner.</summary>
        public static Vector2 Center(Vector2 ownerPosition, Vector2 offset, int facing) =>
            ownerPosition + new Vector2(offset.x * facing, offset.y);
    }
}
