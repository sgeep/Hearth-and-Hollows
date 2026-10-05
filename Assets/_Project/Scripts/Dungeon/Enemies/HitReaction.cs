using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// An enemy's simple response to the player's hits (4b): a short knockback slide away from the
    /// player, then the hit interrupts its attack and holds it still for the attack's stagger time,
    /// unless it has super armour while attacking (<see cref="StaggerRules"/>). The slide runs through
    /// TDE's movement ability, so walls and props stop it. (TDE's own force knockback is cancelled by
    /// its 2D controller's MovePosition on our dynamic bodies.) It also remembers the last hit, so a
    /// kill knows which weapon made it (the harvest's clean-kill and element rules).
    /// </summary>
    [RequireComponent(typeof(EnemyIdentity))]
    public sealed class HitReaction : MonoBehaviour, IHitReceiver
    {
        EnemyIdentity m_Identity;
        Health m_Health;
        AIBrain m_Brain;
        // Whether this stagger switched the brain off (it leaves a brain someone else switched off alone).
        bool m_PausedBrain;
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        EnemyAttack[] m_Attacks;
        EnemyPerch m_Perch;
        float m_StaggerUntil;
        bool m_Staggered;
        Vector2 m_KnockDirection;
        float m_KnockForce, m_KnockStarted = float.NegativeInfinity;
        bool m_Knocked;
        float m_SavedAcceleration, m_SavedDeceleration, m_SavedSpeed;

        /// <summary>The most recent player hit, if any.</summary>
        public HitContext? LastHit { get; private set; }
        /// <summary>When the player last hit it (the Harvest Finisher's window, 4e).</summary>
        public float LastHitTime { get; private set; } = float.NegativeInfinity;
        public bool IsStaggered => m_Staggered;

        void Awake()
        {
            m_Identity = GetComponent<EnemyIdentity>();
            m_Health = GetComponent<Health>();
            m_Movement = GetComponent<CharacterMovement>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
            m_Attacks = GetComponents<EnemyAttack>();
            m_Perch = GetComponent<EnemyPerch>();
            var character = GetComponent<Character>();
            m_Brain = character != null ? character.CharacterBrain : null;
        }

        public void ReceiveHit(in HitContext hit)
        {
            LastHit = hit;
            LastHitTime = Time.time;
            if (m_Health != null && m_Health.CurrentHealth <= 0f) return;
            EnemyDefinition definition = m_Identity.Definition;
            bool attacking = false;
            foreach (EnemyAttack attack in m_Attacks) attacking |= attack.IsAttacking;
            if (definition == null || StaggerRules.IgnoresHit(definition.superArmorWhileAttacking, attacking)) return;

            foreach (EnemyAttack attack in m_Attacks) attack.Interrupt();
            // A hanging bat lets go of its wall when hit, rather than sliding along it.
            if (m_Perch != null && m_Perch.IsPerched) m_Perch.Detach();
            else StartKnockback(hit, definition.knockbackMultiplier);
            m_StaggerUntil = StaggerRules.StaggerUntil(m_StaggerUntil, Time.time, hit.Attack != null ? hit.Attack.staggerTime : 0f, definition.staggerMultiplier);
            if (m_StaggerUntil <= Time.time) return;
            m_Staggered = true;
            if (m_Brain != null && m_Brain.BrainActive)
            {
                m_Brain.BrainActive = false;
                m_PausedBrain = true;
            }
            m_Movement?.SetMovement(Vector2.zero);
            m_Animator?.Release();
            m_Animator?.PlayOneShot(CharacterAnim.Hurt);
        }

        void StartKnockback(in HitContext hit, float multiplier)
        {
            if (m_Movement == null || hit.Attack == null || hit.Attack.knockbackForce <= 0f || multiplier <= 0f) return;
            Vector2 away = (Vector2)transform.position - hit.From;
            m_KnockDirection = away.sqrMagnitude > 1e-6f ? away.normalized : Vector2.right;
            m_KnockForce = hit.Attack.knockbackForce * multiplier;
            m_KnockStarted = Time.time;
            if (!m_Knocked)
            {
                m_SavedAcceleration = m_Movement.Acceleration;
                m_SavedDeceleration = m_Movement.Deceleration;
                m_SavedSpeed = m_Movement.MovementSpeed;
            }
            m_Knocked = true;
            m_Movement.Acceleration = 0f;
            m_Movement.Deceleration = 0f;
        }

        void Update()
        {
            if (m_Knocked)
            {
                float speed = StaggerRules.KnockbackSpeed(m_KnockForce, 1f, Time.time - m_KnockStarted);
                if (speed > 0f)
                {
                    m_Movement.MovementSpeed = speed;
                    m_Movement.SetMovement(m_KnockDirection);
                }
                else
                {
                    m_Knocked = false;
                    m_Movement.SetMovement(Vector2.zero);
                    m_Movement.Acceleration = m_SavedAcceleration;
                    m_Movement.Deceleration = m_SavedDeceleration;
                    m_Movement.MovementSpeed = m_SavedSpeed;
                }
            }
            if (!m_Staggered || Time.time < m_StaggerUntil) return;
            m_Staggered = false;
            if (m_PausedBrain && m_Brain != null && m_Health != null && m_Health.CurrentHealth > 0f) m_Brain.BrainActive = true;
            m_PausedBrain = false;
        }
    }
}
