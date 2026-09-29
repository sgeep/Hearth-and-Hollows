using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Player;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Shared enemy body: gravity, patrol/chase, stagger and knockback, and an
    /// <see cref="AttackCycle"/> whose phases subclasses turn into distinct attacks.
    /// </summary>
    [RequireComponent(typeof(KinematicMover2D), typeof(EnemyHealth))]
    public abstract class EnemyController : MonoBehaviour
    {
        [SerializeField] protected EnemyDefinition m_Definition;
        [SerializeField] protected TelegraphIndicator m_Telegraph;
        [SerializeField] protected SpriteRenderer m_Body;
        [SerializeField] protected LayerMask m_PlayerMask;
        [SerializeField, Tooltip("Shown when the player's lightest hit would finish this monster as a Clean Kill.")]
        SpriteRenderer m_CleanKillIcon;

        protected KinematicMover2D Mover { get; private set; }
        protected EnemyHealth Health { get; private set; }
        protected AttackCycle Cycle { get; private set; }
        protected MeleeHitbox Hitbox { get; private set; }
        protected EnemyAttackSettings Attack => m_Definition.attack;
        protected Vector2 m_Velocity;
        protected int m_Facing = -1;

        readonly Countdown m_Stagger = new();
        readonly Countdown m_HurtFlash = new();
        const float k_HitFlashTime = 0.15f;
        const float k_HitShake = 0.06f;
        const float k_ArmorTremble = 0.04f;
        float m_SinceHit;
        int m_PatrolDirection = -1;
        Transform m_CachedPlayerTransform;
        PlayerController m_CachedPlayer;

        public EnemyDefinition Definition => m_Definition;
        public EnemyAttackPhase AttackPhase => Cycle?.Phase ?? EnemyAttackPhase.Ready;

        public void Configure(EnemyDefinition definition, TelegraphIndicator telegraph, SpriteRenderer body, LayerMask playerMask, SpriteRenderer cleanKillIcon = null)
        {
            m_Definition = definition;
            m_Telegraph = telegraph;
            m_Body = body;
            m_PlayerMask = playerMask;
            m_CleanKillIcon = cleanKillIcon;
        }

        /// <summary>True while the clean-kill cue is showing.</summary>
        public bool InCleanKillRange { get; private set; }

        protected virtual void Awake()
        {
            Mover = GetComponent<KinematicMover2D>();
            Health = GetComponent<EnemyHealth>();
            if (m_Definition == null)
            {
                Debug.LogError($"{name} has no EnemyDefinition.", this);
                enabled = false;
                return;
            }
            Health.Initialize(m_Definition.maxHealth);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;

            Cycle = new AttackCycle(Attack.telegraph, Attack.active, Attack.recovery, Attack.cooldown);
            Cycle.PhaseChanged += OnPhaseChanged;
            Hitbox = new MeleeHitbox(Team.Enemy, m_PlayerMask);
        }

        protected virtual void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            m_Stagger.Tick(dt);
            m_HurtFlash.Tick(dt);
            m_SinceHit += dt;
            Cycle.Tick(dt);

            if (m_Definition.regenerates && m_SinceHit > 2f) Health.RefillToMax();

            var player = PlayerLocator.Player;
            if (m_Stagger.IsRunning)
                m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, 0f, 30f * dt);
            else if (Cycle.IsAttacking)
                AttackTick(dt, player);
            else
                Think(dt, player);

            var contacts = Mover.Contacts;
            if (contacts.Grounded && m_Velocity.y <= 0f) m_Velocity.y = 0f;
            else m_Velocity.y = Mathf.Max(m_Velocity.y - m_Definition.gravity * dt, -20f);

            Mover.Move(m_Velocity * dt);
        }

        protected virtual void LateUpdate()
        {
            if (m_Body == null) return;
            var scale = m_Body.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (m_Facing > 0 ? 1f : -1f);
            m_Body.transform.localScale = scale;
            if (m_HurtFlash.IsRunning && !Cycle.IsAttacking) m_Body.color = Color.white;
            else if (!Cycle.IsAttacking) m_Body.color = m_Definition.placeholderColor;

            // Hit shake: jolt the sprite sideways. Unscaled time so it reads during hit-stop.
            float shake = m_HurtFlash.IsRunning ? Mathf.Sin(Time.unscaledTime * 90f) * k_HitShake : 0f;
            // Super-armored wind-up trembles, so "this can't be interrupted" reads at a glance.
            if (m_Definition.superArmorWhileAttacking && Cycle.Phase == EnemyAttackPhase.Telegraph)
                shake += Mathf.Sin(Time.unscaledTime * 55f) * k_ArmorTremble;
            m_Body.transform.localPosition = new Vector3(shake, 0f, 0f);

            UpdateCleanKillCue();
        }

        void UpdateCleanKillCue()
        {
            var player = GetPlayer();
            var harvest = HarvestSystem.Instance;
            InCleanKillRange = player != null && player.Weapon != null && harvest != null && !Health.IsDead &&
                HarvestRules.InCleanKillRange(Health.Current, Health.Max, player.Weapon.LightestHitDamage,
                    m_Definition.harvest, player.Weapon.cleanKillCategories, harvest.Rules);

            if (m_CleanKillIcon == null) return;
            m_CleanKillIcon.enabled = InCleanKillRange;
            if (InCleanKillRange)
                m_CleanKillIcon.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f));
        }

        PlayerController GetPlayer()
        {
            var t = PlayerLocator.Player;
            if (t != m_CachedPlayerTransform)
            {
                m_CachedPlayerTransform = t;
                m_CachedPlayer = t != null ? t.GetComponent<PlayerController>() : null;
            }
            return m_CachedPlayer;
        }

        void Think(float dt, Transform player)
        {
            var contacts = Mover.Contacts;
            if (player != null)
            {
                Vector2 toPlayer = (Vector2)player.position - Mover.Position;
                if (Mathf.Abs(toPlayer.x) <= m_Definition.aggroRange && Mathf.Abs(toPlayer.y) < 3f)
                {
                    FaceTowards(toPlayer.x);
                    if (Mathf.Abs(toPlayer.x) <= m_Definition.attackRange && contacts.Grounded && Cycle.TryStart())
                    {
                        m_Velocity.x = 0f;
                        return;
                    }
                    bool canAdvance = Mover.HasGroundAhead(m_Facing) && !(m_Facing < 0 ? contacts.WallLeft : contacts.WallRight);
                    float desired = canAdvance && Mathf.Abs(toPlayer.x) > m_Definition.attackRange * 0.8f ? m_Facing * m_Definition.moveSpeed : 0f;
                    m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, desired, 40f * dt);
                    return;
                }
            }

            // Patrol: walk until a wall or ledge, then turn.
            if (m_Definition.moveSpeed <= 0f)
            {
                m_Velocity.x = 0f;
                return;
            }
            bool blocked = !Mover.HasGroundAhead(m_PatrolDirection) || (m_PatrolDirection < 0 ? contacts.WallLeft : contacts.WallRight);
            if (blocked && contacts.Grounded) m_PatrolDirection = -m_PatrolDirection;
            m_Facing = m_PatrolDirection;
            m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, m_PatrolDirection * m_Definition.moveSpeed * 0.5f, 40f * dt);
        }

        protected void FaceTowards(float dx)
        {
            if (Mathf.Abs(dx) > 0.05f) m_Facing = dx > 0f ? 1 : -1;
        }

        void OnPhaseChanged(EnemyAttackPhase phase)
        {
            switch (phase)
            {
                case EnemyAttackPhase.Telegraph:
                    m_Telegraph?.Show();
                    OnTelegraphStart(PlayerLocator.Player);
                    break;
                case EnemyAttackPhase.Active:
                    m_Telegraph?.Hide();
                    Hitbox.Begin();
                    OnActiveStart(PlayerLocator.Player);
                    break;
                case EnemyAttackPhase.Recovery:
                    OnActiveEnd();
                    break;
                case EnemyAttackPhase.Cooldown:
                    m_Telegraph?.Hide();
                    m_Telegraph?.HideMarker();
                    OnAttackOver();
                    break;
            }
        }

        /// <summary>Sweep the attack hitbox in front of the enemy (call during Active).</summary>
        protected void SweepAttack(Vector2 offset, Vector2 size)
        {
            var hit = new DamageInfo
            {
                Amount = Attack.damage,
                Knockback = new Vector2(Attack.knockback.x * m_Facing, Attack.knockback.y),
                Instigator = gameObject,
            };
            Hitbox.Sweep(MeleeHitbox.Center(Mover.Position, offset, m_Facing), size, hit);
        }

        void OnDamaged(DamageInfo hit, DamageResult result)
        {
            m_SinceHit = 0f;
            m_HurtFlash.Start(k_HitFlashTime);
            bool armored = m_Definition.superArmorWhileAttacking && Cycle.IsAttacking;
            if (armored) return;

            if (Cycle.IsAttacking)
            {
                Cycle.Interrupt();
                OnInterrupted();
            }
            m_Stagger.Start(hit.StaggerTime);
            m_Velocity = hit.Knockback * m_Definition.knockbackMultiplier;
        }

        void OnDied(DamageInfo hit, DamageResult result)
        {
            m_Telegraph?.Hide();
            m_Telegraph?.HideMarker();
            var kill = new KillContext
            {
                Element = hit.Element,
                CleanKillCategories = hit.CleanKillCategories,
                Overkill = result.Overkill,
                MaxHealth = Health.Max,
                IsFinisher = hit.IsFinisher,
            };
            EventBus<EnemyKilled>.Publish(new EnemyKilled(m_Definition, kill, Mover.Position + Vector2.up * 0.5f));
            Destroy(gameObject);
        }

        // Attack hooks — each enemy type turns the shared cycle into its own attack.
        protected virtual void OnTelegraphStart(Transform player) { }
        protected virtual void OnActiveStart(Transform player) { }
        protected virtual void AttackTick(float dt, Transform player) => m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, 0f, 40f * dt);
        protected virtual void OnActiveEnd() { }
        protected virtual void OnAttackOver() { }
        protected virtual void OnInterrupted() { }

        protected virtual void OnDrawGizmosSelected()
        {
            if (m_Definition == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            var pos = Application.isPlaying && Mover != null ? Mover.Position : (Vector2)transform.position;
            Gizmos.DrawWireCube(MeleeHitbox.Center(pos, Attack.hitboxOffset, m_Facing), Attack.hitboxSize);
        }
    }
}
