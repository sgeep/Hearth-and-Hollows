using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Giant Rat — telegraph: stops, crouches low and flashes with a "!"; attack: a fast
    /// horizontal lunge that hits along its path. Punishable during recovery.
    /// </summary>
    public sealed class RatBehaviour : EnemyController
    {
        protected override void OnTelegraphStart(Transform player)
        {
            m_Velocity.x = 0f;
            if (player != null) FaceTowards(player.position.x - transform.position.x);
            SetCrouch(0.65f);
        }

        protected override void OnActiveStart(Transform player)
        {
            SetCrouch(1f);
            m_Velocity = new Vector2(m_Facing * Attack.lungeSpeed, 2f);
        }

        protected override void AttackTick(float dt, Transform player)
        {
            if (Cycle.Phase == EnemyAttackPhase.Active)
            {
                m_Velocity.x = m_Facing * Attack.lungeSpeed;
                SweepAttack(Attack.hitboxOffset, Attack.hitboxSize);
            }
            else
            {
                m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, 0f, 35f * dt);
            }
        }

        protected override void OnAttackOver() => SetCrouch(1f);
        protected override void OnInterrupted() => SetCrouch(1f);

        void SetCrouch(float y)
        {
            if (m_Body == null) return;
            var s = m_Body.transform.localScale;
            m_Body.transform.localScale = new Vector3(s.x, y, 1f);
        }
    }
}
