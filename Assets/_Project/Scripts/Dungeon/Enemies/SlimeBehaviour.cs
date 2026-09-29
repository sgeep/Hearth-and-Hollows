using Hearthdelve.Core.Events;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Green Slime — telegraph: squashes down and a marker appears where it will land;
    /// attack: leaps to the marked spot and slams, hitting a wide area on landing.
    /// Heavy: hits don't interrupt the leap once it starts winding up.
    /// </summary>
    public sealed class SlimeBehaviour : EnemyController
    {
        Vector2 m_Target;
        bool m_Landed;
        float m_ActiveElapsed;

        protected override void OnTelegraphStart(Transform player)
        {
            m_Velocity.x = 0f;
            float from = Mover.Position.x;
            float to = player != null ? player.position.x : from + m_Facing * 2f;
            to = Mathf.Clamp(to, from - Attack.leapMaxDistance, from + Attack.leapMaxDistance);
            m_Target = new Vector2(to, Mover.Bounds.min.y);
            FaceTowards(to - from);
            m_Telegraph?.ShowMarker(m_Target);
            SetSquash(1.25f, 0.7f);
        }

        protected override void OnActiveStart(Transform player)
        {
            SetSquash(0.85f, 1.2f);
            // Ballistic hop: vy reaches leapHeight; vx covers the distance in the time aloft.
            float g = m_Definition.gravity;
            float vy = Mathf.Sqrt(2f * g * Attack.leapHeight);
            float airTime = 2f * vy / g;
            m_Velocity = new Vector2((m_Target.x - Mover.Position.x) / airTime, vy);
            m_Landed = false;
            m_ActiveElapsed = 0f;
            // Keep the cycle in Active until we land.
            Cycle.Active = airTime + 0.05f;
        }

        protected override void AttackTick(float dt, Transform player)
        {
            if (Cycle.Phase != EnemyAttackPhase.Active)
            {
                m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, 0f, 40f * dt);
                return;
            }
            m_ActiveElapsed += dt;
            if (!m_Landed && m_ActiveElapsed > 0.1f && Mover.Contacts.Grounded && m_Velocity.y <= 0f)
            {
                m_Landed = true;
                m_Velocity.x = 0f;
                SetSquash(1.35f, 0.6f);
                m_Telegraph?.HideMarker();
                SweepAttack(Attack.hitboxOffset, Attack.hitboxSize);
                EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.2f));
            }
        }

        protected override void OnActiveEnd()
        {
            m_Telegraph?.HideMarker();
            if (!m_Landed) m_Velocity.x = 0f;
        }

        protected override void OnAttackOver() => SetSquash(1f, 1f);
        protected override void OnInterrupted() => SetSquash(1f, 1f);

        void SetSquash(float x, float y)
        {
            if (m_Body == null) return;
            float sign = Mathf.Sign(m_Body.transform.localScale.x);
            m_Body.transform.localScale = new Vector3(x * sign, y, 1f);
        }
    }
}
