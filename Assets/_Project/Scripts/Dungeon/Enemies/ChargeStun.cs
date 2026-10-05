using Hearthdelve.Shared.Animation;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>Pure rules for a charge running into something (4e).</summary>
    public static class ChargeRules
    {
        /// <summary>
        /// Blocked: over a stretch of the charge it covered less than <paramref name="ratio"/> of the distance its speed
        /// should have (a pillar, a wall or a prop stopped it).
        /// </summary>
        public static bool IsBlocked(float expected, float moved, float ratio = 0.3f) => expected > 0.01f && moved < expected * ratio;
    }

    /// <summary>
    /// A charge that stuns its owner when it runs into something (4e, the Larder Troll): watches one swoop attack whose
    /// <see cref="EnemyAttackSettings.stunOnBlock"/> is set. When the charge is stopped short, the attack ends, the impact
    /// plays, and the enemy stands dazed (its hurt pose, the brain off) for that long: the opening to punish.
    /// </summary>
    [RequireComponent(typeof(EnemyAttack))]
    public sealed class ChargeStun : MonoBehaviour
    {
        [SerializeField, Tooltip("Which EnemyAttack (by index among this enemy's attacks) is the charge.")]
        int m_AttackIndex = 1;
        [SerializeField, Tooltip("Running into the wall: the thud, a shake and a hard bump.")]
        MMF_Player m_ImpactFeedback;
        [SerializeField, Min(0.02f), Tooltip("Seconds of the charge each blocked check looks back over.")]
        float m_Window = 0.12f;

        EnemyAttack m_Attack;
        AIBrain m_Brain;
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        Health m_Health;
        Vector2 m_WindowStart;
        float m_WindowTime;
        float m_ChargeTime;
        bool m_Charging;
        float m_StunnedUntil;
        bool m_PausedBrain;

        public bool IsStunned { get; private set; }
        /// <summary>Times the charge has been stopped short (tests).</summary>
        public int Stuns { get; private set; }

        public void Configure(int attackIndex, MMF_Player impact)
        {
            m_AttackIndex = attackIndex;
            m_ImpactFeedback = impact;
        }

        void Awake()
        {
            EnemyAttack[] attacks = GetComponents<EnemyAttack>();
            m_Attack = m_AttackIndex >= 0 && m_AttackIndex < attacks.Length ? attacks[m_AttackIndex] : null;
            m_Movement = GetComponent<CharacterMovement>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
            m_Health = GetComponent<Health>();
            if (m_Attack != null) m_Attack.PhaseChanged += OnPhase;
        }

        void Start()
        {
            Character character = GetComponent<Character>();
            m_Brain = character != null ? character.CharacterBrain : GetComponent<AIBrain>();
        }

        void OnDestroy()
        {
            if (m_Attack != null) m_Attack.PhaseChanged -= OnPhase;
        }

        void OnPhase(EnemyAttackPhase phase)
        {
            m_Charging = phase == EnemyAttackPhase.Active && m_Attack.Settings.stunOnBlock > 0f;
            m_WindowStart = transform.position;
            m_WindowTime = 0f;
            m_ChargeTime = 0f;
        }

        void Update()
        {
            if (IsStunned)
            {
                if (Time.time >= m_StunnedUntil || m_Health == null || m_Health.CurrentHealth <= 0f) Recover();
                return;
            }
            if (!m_Charging) return;
            m_ChargeTime += Time.deltaTime;
            m_WindowTime += Time.deltaTime;
            if (m_WindowTime < m_Window) return;
            EnemyAttackSettings s = m_Attack.Settings;
            float speed = s.travelDistance / Mathf.Max(0.01f, s.active);
            float moved = Vector2.Distance(m_WindowStart, transform.position);
            // The first moment of the charge is the body getting going; judge it once it's under way.
            if (m_ChargeTime > m_Window * 1.5f && ChargeRules.IsBlocked(speed * m_WindowTime, moved)) Stun(s.stunOnBlock);
            m_WindowStart = transform.position;
            m_WindowTime = 0f;
        }

        void Stun(float seconds)
        {
            m_Charging = false;
            m_Attack.Interrupt();
            IsStunned = true;
            Stuns++;
            m_StunnedUntil = Time.time + seconds;
            m_Movement?.SetMovement(Vector2.zero);
            if (m_Brain != null && m_Brain.BrainActive)
            {
                m_Brain.BrainActive = false;
                m_PausedBrain = true;
            }
            m_Animator?.Hold(CharacterAnim.Hurt);
            m_ImpactFeedback?.PlayFeedbacks(transform.position);
        }

        void Recover()
        {
            IsStunned = false;
            m_Animator?.Release();
            // A frenzy roar that began during the daze wakes the brain itself when it ends.
            bool roaring = TryGetComponent(out Hearthdelve.Dungeon.Bosses.BossFrenzy frenzy) && frenzy.IsRoaring;
            if (m_PausedBrain && !roaring && m_Brain != null && m_Health != null && m_Health.CurrentHealth > 0f) m_Brain.BrainActive = true;
            m_PausedBrain = false;
        }
    }
}
