using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>
    /// A boss's second phase (4e step 2): below <see cref="FrenzySettings.atHealth"/> it roars (a shake, the
    /// <c>Boss.PhaseChange</c> rumble; it can't be hurt and doesn't act for <see cref="FrenzySettings.roarSeconds"/>),
    /// then fights harder: faster, tinted, its slams coming in pairs and its charges spilling parts.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class BossFrenzy : MonoBehaviour
    {
        [SerializeField] BossDefinition m_Boss;
        [SerializeField, Tooltip("The roar: sound, a big shake, the phase-change rumble.")]
        MMF_Player m_RoarFeedback;

        Health m_Health;
        BossEncounter m_Encounter;
        AIBrain m_Brain;
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        SpriteRenderer m_Body;
        EnemyAttack m_Slam;
        float m_RoarLeft;
        bool m_Roaring;
        bool m_Chained;

        public bool IsFrenzied { get; private set; }
        public bool IsRoaring => m_Roaring;

        public void Configure(BossDefinition boss, MMF_Player roar)
        {
            m_Boss = boss;
            m_RoarFeedback = roar;
        }

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_Encounter = GetComponent<BossEncounter>();
            m_Movement = GetComponent<CharacterMovement>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
            m_Body = m_Animator != null ? m_Animator.Renderer : null;
            EnemyAttack[] attacks = GetComponents<EnemyAttack>();
            m_Slam = attacks.Length > 0 ? attacks[0] : null;
            if (m_Slam != null) m_Slam.PhaseChanged += OnSlam;
        }

        void Start()
        {
            Character character = GetComponent<Character>();
            m_Brain = character != null ? character.CharacterBrain : GetComponent<AIBrain>();
        }

        void OnDestroy()
        {
            if (m_Slam != null) m_Slam.PhaseChanged -= OnSlam;
        }

        void Update()
        {
            if (m_Boss == null || !m_Boss.frenzy.enabled) return;
            if (m_Roaring)
            {
                m_RoarLeft -= Time.deltaTime;
                if (m_RoarLeft <= 0f) EndRoar();
                return;
            }
            if (IsFrenzied || m_Encounter == null || m_Encounter.State != BossEncounterState.Fighting) return;
            // Brought down by a killing blow: no frenzy now, only the end.
            if (m_Health is BossHealth { IsDowned: true }) return;
            if (m_Health.CurrentHealth > 0f && m_Health.CurrentHealth <= m_Health.MaximumHealth * m_Boss.frenzy.atHealth) BeginRoar();
        }

        void BeginRoar()
        {
            IsFrenzied = true;
            m_Roaring = true;
            m_RoarLeft = m_Boss.frenzy.roarSeconds;
            m_Health.Invulnerable = true;
            foreach (EnemyAttack attack in GetComponents<EnemyAttack>()) attack.Interrupt();
            GetComponent<ScrapEater>()?.StopEating();
            if (m_Brain != null) m_Brain.BrainActive = false;
            m_Movement?.SetMovement(Vector2.zero);
            m_Animator?.Hold(CharacterAnim.Hurt);
            if (m_Body != null) m_Body.color = m_Boss.frenzy.tint;
            m_RoarFeedback?.PlayFeedbacks(transform.position);
            EventBus<BossPhaseChanged>.Publish(new BossPhaseChanged(2));
        }

        void EndRoar()
        {
            m_Roaring = false;
            m_Health.Invulnerable = false;
            m_Animator?.Release();
            if (m_Movement != null) m_Movement.MovementSpeedMultiplier = m_Boss.frenzy.speedMultiplier;
            if (m_Brain != null && m_Health.CurrentHealth > 0f) m_Brain.BrainActive = true;
        }

        // Frenzied, a slam is followed at once by a second (the cooldown is skipped once; the lockout still gives a beat).
        void OnSlam(EnemyAttackPhase phase)
        {
            if (!IsFrenzied || m_Roaring || !m_Boss.frenzy.chainSlams || phase != EnemyAttackPhase.Cooldown) return;
            if (m_Chained)
            {
                m_Chained = false;
                return;
            }
            m_Chained = true;
            m_Slam.SkipCooldown();
        }
    }
}
