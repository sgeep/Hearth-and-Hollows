using System;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Navigation;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// One telegraphed enemy attack, timed by <see cref="AttackCycle"/> from its
    /// <see cref="EnemyAttackSettings"/> (so all timings live in the <see cref="EnemyDefinition"/>):
    /// Telegraph (stop, face the target, show the alert, flash, wind-up animation) → Active (leap,
    /// swoop, bite or spit; the only time it can hurt) → Recovery → Cooldown. The TDE brain starts it
    /// through <see cref="AIActionEnemyAttack"/>; a hit can interrupt it (<see cref="HitReaction"/>).
    /// </summary>
    [RequireComponent(typeof(EnemyIdentity))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        /// <summary>The least time between the end of one of an enemy's attacks and the start of its next.</summary>
        public const float Lockout = 0.35f;

        [SerializeField, Min(0), Tooltip("Which of the definition's attacks: 0 is the main attack, 1 the first of the others, and so on.")]
        int m_AttackIndex;
        [SerializeField, Tooltip("Trigger collider with a DamageOnTouch; only active during the attack's active phase.")]
        GameObject m_Hitbox;
        [SerializeField, Tooltip("The alert icon shown during the telegraph.")]
        GameObject m_Alert;
        [SerializeField, Tooltip("Played when the telegraph starts: flash and sound.")]
        MMF_Player m_TelegraphFeedback;
        [SerializeField, Tooltip("Spit only: the projectile fired.")]
        WebProjectile m_Projectile;
        [SerializeField, Tooltip("The body sprite, raised along an arc during a leap (the shadow stays down).")]
        Transform m_Body;
        [SerializeField, Tooltip("A light haptic cue at the telegraph, only when the attack is aimed at the player and close.")]
        MMF_Player m_TelegraphCue;
        [SerializeField, Min(0f), Tooltip("The cue only plays within this distance of the player, in tiles.")]
        float m_CueRange = 6f;

        EnemyAttack[] m_Siblings;
        Character m_Character;
        CharacterMovement m_Movement;
        Health m_Health;
        CharacterSpriteAnimator m_Animator;
        Collider2D m_Collider;
        DamageOnTouch m_HitboxDamage;
        BoxCollider2D m_HitboxShape;
        Vector3 m_BodyRest;
        Vector2 m_Direction = Vector2.right;
        Vector2 m_TargetPoint;
        bool m_Dashing;
        float m_SavedAcceleration, m_SavedDeceleration, m_SavedSpeed;
        bool m_SavedKnockbackImmunity;
        bool m_TargetIsPlayer;

        public EnemyAttackSettings Settings { get; private set; }
        public AttackCycle Cycle { get; private set; }
        public bool IsAttacking => Cycle != null && Cycle.IsAttacking;
        /// <summary>When the last attack ended (finished or interrupted).</summary>
        public float FinishedAt { get; private set; } = float.NegativeInfinity;
        public GameObject Alert => m_Alert;
        public Vector2 Direction => m_Direction;
        public event Action<EnemyAttackPhase> PhaseChanged;

        public void Configure(int attackIndex, GameObject hitbox, GameObject alert, MMF_Player telegraphFeedback, WebProjectile projectile, Transform body, MMF_Player telegraphCue = null)
        {
            m_TelegraphCue = telegraphCue;
            m_AttackIndex = attackIndex;
            m_Hitbox = hitbox;
            m_Alert = alert;
            m_TelegraphFeedback = telegraphFeedback;
            m_Projectile = projectile;
            m_Body = body;
        }

        void Awake()
        {
            m_Siblings = GetComponents<EnemyAttack>();
            m_Character = GetComponent<Character>();
            m_Movement = GetComponent<CharacterMovement>();
            m_Health = GetComponent<Health>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
            foreach (Collider2D candidate in GetComponents<Collider2D>())
                if (!candidate.isTrigger) { m_Collider = candidate; break; }
            if (m_Body != null) m_BodyRest = m_Body.localPosition;

            EnemyDefinition definition = GetComponent<EnemyIdentity>().Definition;
            Settings = Pick(definition, m_AttackIndex) ?? new EnemyAttackSettings();
            Cycle = new AttackCycle(Settings.telegraph, Settings.active, Settings.recovery, Settings.cooldown);
            Cycle.PhaseChanged += OnPhase;

            if (m_Hitbox != null)
            {
                m_HitboxDamage = m_Hitbox.GetComponent<DamageOnTouch>();
                m_HitboxShape = m_Hitbox.GetComponent<BoxCollider2D>();
                if (m_HitboxDamage != null)
                {
                    m_HitboxDamage.Owner = gameObject;
                    m_HitboxDamage.MinDamageCaused = Settings.damage;
                    m_HitboxDamage.MaxDamageCaused = Settings.damage;
                    m_HitboxDamage.DamageCausedKnockbackType = DamageOnTouch.KnockbackStyles.NoKnockback;
                }
                if (m_HitboxShape != null) m_HitboxShape.size = Settings.hitboxSize;
                m_Hitbox.SetActive(false);
            }
            if (m_Alert != null) m_Alert.SetActive(false);
        }

        static EnemyAttackSettings Pick(EnemyDefinition definition, int index)
        {
            if (definition == null) return null;
            if (index == 0) return definition.attack;
            int other = index - 1;
            return other < definition.otherAttacks.Count ? definition.otherAttacks[other] : null;
        }

        void OnDisable()
        {
            if (IsAttacking) Interrupt();
        }

        /// <summary>Whether this attack could start against <paramref name="target"/> now.</summary>
        public bool CanStart(Transform target)
        {
            if (!isActiveAndEnabled || target == null || Cycle == null || Cycle.Phase != EnemyAttackPhase.Ready) return false;
            if (m_Health != null && m_Health.CurrentHealth <= 0f) return false;
            foreach (EnemyAttack sibling in m_Siblings)
            {
                if (sibling.IsAttacking) return false;
                if (Time.time < sibling.FinishedAt + Lockout) return false;
            }
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance < Settings.minRange || distance > Settings.maxRange) return false;
            return Settings.kind == EnemyAttackKind.Bite || ClearLine(target.position);
        }

        // Leaps, swoops and webs need line of sight: no telegraphing an attack through a wall or round a
        // corner. Only the line, not the whole body: walls stop a leap anyway, and a body-sized check refused
        // attacks whenever the player stood near a wall or prop.
        bool ClearLine(Vector2 targetFeet)
        {
            NavGrid grid = NavGrid.Current;
            if (grid == null) return true;
            Vector2 feet = transform.position;
            Vector2 body = m_Collider != null ? (Vector2)m_Collider.bounds.center : feet;
            return GridSweep.IsClear(grid.Map, grid.Space, body, targetFeet + (body - feet), Vector2.one * 0.15f);
        }

        /// <summary>Starts the telegraph against <paramref name="target"/>. False if the attack can't start.</summary>
        public bool Begin(Transform target)
        {
            if (!CanStart(target)) return false;
            m_TargetPoint = target.position;
            m_TargetIsPlayer = target.CompareTag("Player");
            Vector2 toTarget = m_TargetPoint - (Vector2)transform.position;
            m_Direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector2.right;
            return Cycle.TryStart();
        }

        /// <summary>Abandons the attack (a stagger): it goes straight to cooldown.</summary>
        public void Interrupt() => Cycle?.Interrupt();

        void Update()
        {
            Cycle?.Tick(Time.deltaTime);
            if (Cycle == null || Cycle.Phase != EnemyAttackPhase.Active) return;
            if (m_Dashing) m_Movement?.SetMovement(m_Direction);
            if (Settings.kind == EnemyAttackKind.Leap && m_Body != null && Settings.active > 0f)
            {
                float t = Mathf.Clamp01(Cycle.PhaseElapsed / Settings.active);
                m_Body.localPosition = m_BodyRest + Vector3.up * (Mathf.Sin(t * Mathf.PI) * Settings.arcHeight);
            }
        }

        void OnPhase(EnemyAttackPhase phase)
        {
            switch (phase)
            {
                case EnemyAttackPhase.Telegraph:
                    m_Movement?.SetMovement(Vector2.zero);
                    m_Animator?.PlayTelegraphed(Settings.animation, Settings.telegraph, Settings.releaseFrame, m_Direction);
                    if (m_Alert != null) m_Alert.SetActive(true);
                    m_TelegraphFeedback?.PlayFeedbacks(transform.position);
                    // Felt only when it's coming for the player: a room full of enemies shouldn't buzz constantly.
                    if (m_TargetIsPlayer && Vector2.Distance(transform.position, m_TargetPoint) <= m_CueRange) m_TelegraphCue?.PlayFeedbacks(transform.position);
                    if (m_Health != null && GetComponent<EnemyIdentity>().Definition is { superArmorWhileAttacking: true })
                    {
                        m_SavedKnockbackImmunity = m_Health.ImmuneToKnockback;
                        m_Health.ImmuneToKnockback = true;
                    }
                    break;
                case EnemyAttackPhase.Active:
                    if (m_Alert != null) m_Alert.SetActive(false);
                    StartActive();
                    break;
                case EnemyAttackPhase.Recovery:
                    EndActive();
                    break;
                case EnemyAttackPhase.Cooldown:
                    EndActive();
                    if (m_Alert != null) m_Alert.SetActive(false);
                    m_TelegraphFeedback?.StopFeedbacks();
                    m_Animator?.StopTelegraphed();
                    if (m_Health != null && GetComponent<EnemyIdentity>().Definition is { superArmorWhileAttacking: true })
                        m_Health.ImmuneToKnockback = m_SavedKnockbackImmunity;
                    FinishedAt = Time.time;
                    break;
            }
            PhaseChanged?.Invoke(phase);
        }

        void StartActive()
        {
            switch (Settings.kind)
            {
                case EnemyAttackKind.Leap:
                    float distance = Mathf.Min(Settings.travelDistance, Vector2.Distance(transform.position, m_TargetPoint));
                    StartDash(distance / Mathf.Max(0.01f, Settings.active));
                    PlaceHitbox(Settings.hitboxOffset);
                    break;
                case EnemyAttackKind.Swoop:
                    StartDash(Settings.travelDistance / Mathf.Max(0.01f, Settings.active));
                    PlaceHitbox(Settings.hitboxOffset);
                    break;
                case EnemyAttackKind.Bite:
                    PlaceHitbox(m_Direction * Settings.hitboxOffset.x + Vector2.up * Settings.hitboxOffset.y);
                    break;
                case EnemyAttackKind.Spit:
                    Fire();
                    break;
            }
        }

        void PlaceHitbox(Vector2 localPosition)
        {
            if (m_Hitbox == null) return;
            m_Hitbox.transform.localPosition = localPosition;
            m_Hitbox.SetActive(true);
        }

        void Fire()
        {
            if (m_Projectile == null) return;
            Vector2 start = (Vector2)transform.position + m_Direction * Settings.projectileSpawnOffset.x + Vector2.up * Settings.projectileSpawnOffset.y;
            WebProjectile web = Instantiate(m_Projectile, start, Quaternion.identity);
            web.Launch(m_Direction, Settings.projectileSpeed, Settings.projectileRange, Settings.damage, gameObject);
        }

        void StartDash(float speed)
        {
            if (m_Movement == null) return;
            m_SavedAcceleration = m_Movement.Acceleration;
            m_SavedDeceleration = m_Movement.Deceleration;
            m_SavedSpeed = m_Movement.MovementSpeed;
            // Full speed at once: the leap or swoop is over in a fraction of a second.
            m_Movement.Acceleration = 0f;
            m_Movement.Deceleration = 0f;
            m_Movement.MovementSpeed = speed;
            m_Movement.SetMovement(m_Direction);
            m_Dashing = true;
        }

        void EndActive()
        {
            if (m_Hitbox != null) m_Hitbox.SetActive(false);
            if (m_Body != null) m_Body.localPosition = m_BodyRest;
            if (!m_Dashing || m_Movement == null) return;
            m_Dashing = false;
            m_Movement.SetMovement(Vector2.zero);
            m_Movement.Acceleration = m_SavedAcceleration;
            m_Movement.Deceleration = m_SavedDeceleration;
            m_Movement.MovementSpeed = m_SavedSpeed;
        }
    }
}
