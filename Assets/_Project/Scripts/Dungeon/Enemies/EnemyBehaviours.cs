using Hearthdelve.Core.Events;
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

    /// <summary>
    /// Cellar Shroom — stationary. Telegraph: swells and glows; attack: lobs a spore blob in
    /// an arc at where the player stood when the telegraph began. Dodge or step aside.
    /// </summary>
    public sealed class ShroomBehaviour : EnemyController
    {
        [SerializeField] SporeProjectile m_ProjectilePrefab;
        Vector2 m_AimPoint;

        public void ConfigureProjectile(SporeProjectile prefab) => m_ProjectilePrefab = prefab;

        protected override void OnTelegraphStart(Transform player)
        {
            m_AimPoint = player != null ? (Vector2)player.position : Mover.Position + new Vector2(m_Facing * 3f, 0f);
            FaceTowards(m_AimPoint.x - Mover.Position.x);
            SetSwell(1.25f);
        }

        protected override void OnActiveStart(Transform player)
        {
            SetSwell(0.9f);
            if (m_ProjectilePrefab == null) return;
            var origin = Mover.Position + new Vector2(Attack.projectileSpawnOffset.x * m_Facing, Attack.projectileSpawnOffset.y);
            var spore = Instantiate(m_ProjectilePrefab, origin, Quaternion.identity);
            spore.Launch(origin, m_AimPoint + Vector2.up * 0.5f, Attack.projectileFlightTime, Attack.damage, m_PlayerMask, gameObject);
        }

        protected override void OnAttackOver() => SetSwell(1f);
        protected override void OnInterrupted() => SetSwell(1f);

        void SetSwell(float s)
        {
            if (m_Body == null) return;
            float sign = Mathf.Sign(m_Body.transform.localScale.x);
            m_Body.transform.localScale = new Vector3(s * sign, s, 1f);
        }
    }

    /// <summary>Training dummy: never attacks, refills health. For tuning combo feel.</summary>
    public sealed class DummyBehaviour : EnemyController
    {
    }
}
