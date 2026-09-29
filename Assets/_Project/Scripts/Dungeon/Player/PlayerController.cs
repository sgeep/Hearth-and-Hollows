using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// Thin glue: feeds input into <see cref="PlatformerMotor"/> and <see cref="ComboLogic"/>,
    /// moves via <see cref="KinematicMover2D"/>, and sweeps the weapon hitbox on active frames.
    /// </summary>
    [RequireComponent(typeof(KinematicMover2D), typeof(PlayerInputReader))]
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerMovementConfig m_MovementConfig;
        [SerializeField] WeaponDefinition m_Weapon;
        [SerializeField] LayerMask m_EnemyMask;

        KinematicMover2D m_Mover;
        PlayerInputReader m_Input;
        PlayerVitals m_Vitals;
        MeleeHitbox m_Hitbox;
        readonly List<IDamageable> m_Landed = new();

        public PlatformerMotor Motor { get; private set; }
        public ComboLogic Combo { get; private set; }
        public WeaponDefinition Weapon => m_Weapon;
        public KinematicMover2D Mover => m_Mover;
        public int Facing => Motor?.Facing ?? 1;

        /// <summary>Debug: overrides the weapon's element (to test Seared/Chilled/Inedible harvests).</summary>
        public Element? ElementOverride { get; set; }
        public Element CurrentElement => ElementOverride ?? (m_Weapon != null ? m_Weapon.element : Element.None);

        /// <summary>World-space hitbox of the attack in progress (for visuals and gizmos).</summary>
        public bool TryGetActiveHitbox(out Vector2 center, out Vector2 size)
        {
            var attack = Combo?.Current;
            center = default;
            size = default;
            if (attack == null) return false;
            center = MeleeHitbox.Center(m_Mover.Position, attack.hitboxOffset, Facing);
            size = attack.hitboxSize;
            return true;
        }

        public void Configure(PlayerMovementConfig movement, WeaponDefinition weapon, LayerMask enemyMask)
        {
            m_MovementConfig = movement;
            m_Weapon = weapon;
            m_EnemyMask = enemyMask;
        }

        void Awake()
        {
            m_Mover = GetComponent<KinematicMover2D>();
            m_Input = GetComponent<PlayerInputReader>();
            m_Vitals = GetComponent<PlayerVitals>();
            Build();
        }

        void Build()
        {
            if (m_MovementConfig == null || m_Weapon == null || m_Weapon.combo.Count == 0)
            {
                Debug.LogError("PlayerController needs a movement config and a weapon with at least one attack.", this);
                enabled = false;
                return;
            }
            Motor = new PlatformerMotor(m_MovementConfig.movement);
            Combo = new ComboLogic(m_Weapon.combo, m_Weapon.inputBuffer, m_Weapon.comboLinkWindow);
            Combo.ActiveStarted += _ => m_Hitbox.Begin();
            m_Hitbox = new MeleeHitbox(Team.Player, m_EnemyMask);
        }

        void OnEnable() => PlayerLocator.Register(transform);
        void OnDisable() => PlayerLocator.Unregister(transform);

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var input = m_Input.Consume();
            bool defeated = m_Vitals != null && m_Vitals.IsDefeated;
            bool stunned = m_Vitals != null && m_Vitals.IsStunned;
            if (defeated) input = default;

            HandleCombatInput(input, stunned);
            Combo.Tick(dt);

            Motor.ActionLocked = Combo.IsBusy || stunned || defeated;
            Motor.HorizontalOverride = LungeVelocity();

            var motorInput = new MotorInput
            {
                MoveX = input.Move.x,
                MoveY = input.Move.y,
                JumpPressed = input.JumpPressed,
                JumpHeld = input.JumpHeld,
                DodgePressed = input.DodgePressed,
            };
            Motor.Step(dt, motorInput, m_Mover.Contacts);
            m_Mover.IgnoreOneWay = Motor.IgnoreOneWay;
            m_Mover.Move(Motor.Velocity * dt);

            if (Combo.IsActive) SweepHitbox();
        }

        void HandleCombatInput(in PlayerFrameInput input, bool stunned)
        {
            if (stunned)
            {
                if (Combo.IsBusy) Combo.Cancel();
                return;
            }

            // Dodge cancels an attack outside its active frames; jump cancels once recovery is cancellable.
            bool wantsDodge = input.DodgePressed || Motor.IsDodgeBuffered;
            bool wantsJump = input.JumpPressed || Motor.IsJumpBuffered;
            if (Combo.IsBusy && ((wantsDodge && !Combo.IsActive) || (wantsJump && Combo.InCancelWindow)))
                Combo.Cancel();

            if (input.AttackPressed && !Motor.IsDodging) Combo.PressAttack();
        }

        float? LungeVelocity()
        {
            var attack = Combo.Current;
            if (attack == null) return null;
            float start = FrameData.ToSeconds(attack.lungeStartFrame);
            float end = FrameData.ToSeconds(attack.lungeStartFrame + attack.lungeFrames);
            return Combo.Elapsed >= start && Combo.Elapsed < end ? attack.lungeSpeed * Facing : 0f;
        }

        void SweepHitbox()
        {
            var attack = Combo.Current;
            var hit = new DamageInfo
            {
                Amount = attack.damage,
                Element = CurrentElement,
                CleanKillCategories = m_Weapon.cleanKillCategories,
                Knockback = new Vector2(attack.knockback.x * Facing, attack.knockback.y),
                StaggerTime = attack.staggerTime,
                HitStop = attack.hitStop,
                ScreenShake = attack.screenShake,
                Instigator = gameObject,
            };
            m_Landed.Clear();
            var center = MeleeHitbox.Center(m_Mover.Position, attack.hitboxOffset, Facing);
            if (m_Hitbox.Sweep(center, attack.hitboxSize, hit, m_Landed) > 0)
            {
                // One freeze + shake per swing, however many enemies it connects with.
                EventBus<HitStopRequested>.Publish(new HitStopRequested(attack.hitStop));
                EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(attack.screenShake));
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !TryGetActiveHitbox(out var c, out var s)) return;
            Gizmos.color = Combo.IsActive ? Color.red : new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireCube(c, s);
        }
    }
}
