using Hearthdelve.Core.Animation;
using Hearthdelve.Core.Movement;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    /// <summary>
    /// Shows a TDE character with Minifantasy sprite sheets: picks the action from the
    /// character's state (idle, walk, dodge, attack, heavy charge, hurt, dead) and one of the four
    /// drawn facings from its movement or aim. An optional second renderer shows the matching
    /// shadow sheet. Presentation only (CLAUDE.md, Animation): gameplay code tells it what is
    /// happening (an enemy's telegraphed attack, a held pose), and it never drives gameplay.
    /// </summary>
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteAnimationSet m_Set;
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] SpriteAnimationSet m_ShadowSet;
        [SerializeField] SpriteRenderer m_ShadowRenderer;
        [SerializeField] Facing4 m_InitialFacing = Facing4.FrontRight;
        [SerializeField, Tooltip("Faces where its weapon aims, or where it looks without one (the player: the mouse or the right stick), except while rolling. Off: faces where it moves.")]
        bool m_FaceAim;

        Hearthdelve.Shared.Engine.PlayerLook m_Look;

        Character m_Character;
        TopDownController m_Controller;
        Health m_Health;
        CharacterHandleWeapon m_HandleWeapon;
        CharacterHandleSecondaryWeapon m_HandleHeavy;

        CharacterAnim m_Current = CharacterAnim.Idle;
        CharacterAnim m_OneShot;
        bool m_OneShotActive;
        float m_Time;
        Weapon.WeaponStates m_LastWeaponState = Weapon.WeaponStates.WeaponIdle;
        bool m_WasCharging;
        bool m_HeavyWasInUse;
        float m_ChargeStartedAt;

        // A telegraphed attack (enemies) or a held pose (a sleeping bat), set by gameplay code.
        bool m_TelegraphedActive;
        CharacterAnim m_Telegraphed;
        float m_TelegraphTime;
        int m_ReleaseFrame;
        float m_TelegraphStartedAt;
        Vector2 m_TelegraphDirection;
        bool m_HeldActive;
        CharacterAnim m_Held;

        public Facing4 Facing { get; private set; }
        /// <summary>The body's renderer.</summary>
        public SpriteRenderer Renderer => m_Renderer;
        public CharacterAnim Current => m_Current;
        /// <summary>Whether the drawing is mirrored (a one-facing sheet shown for a left facing).</summary>
        public bool Mirrored => m_Renderer != null && m_Renderer.flipX;
        public SpriteAnimationSet Set => m_Set;
        public bool IsTelegraphing => m_TelegraphedActive;

        public void Configure(SpriteAnimationSet set, SpriteRenderer renderer, SpriteAnimationSet shadowSet, SpriteRenderer shadowRenderer)
        {
            m_Set = set;
            m_Renderer = renderer;
            m_ShadowSet = shadowSet;
            m_ShadowRenderer = shadowRenderer;
        }

        /// <summary>Faces where its weapon aims rather than where it moves (the player, 4e playtest).</summary>
        public bool FaceAim
        {
            get => m_FaceAim;
            set => m_FaceAim = value;
        }

        void Awake()
        {
            Facing = m_InitialFacing;
            m_Character = GetComponentInParent<Character>();
            if (m_Character == null) return;
            m_Controller = m_Character.GetComponent<TopDownController>();
            m_Look = m_Character.GetComponent<Hearthdelve.Shared.Engine.PlayerLook>();
            m_Health = m_Character.GetComponent<Health>();
            // The secondary handle (the heavy attack) is a subclass of the primary one.
            foreach (CharacterHandleWeapon handle in m_Character.GetComponents<CharacterHandleWeapon>())
            {
                if (handle is CharacterHandleSecondaryWeapon secondary) m_HandleHeavy = secondary;
                else if (m_HandleWeapon == null) m_HandleWeapon = handle;
            }
        }

        void OnEnable()
        {
            if (m_Health != null) m_Health.OnHit += OnHit;
            m_OneShotActive = false;
            m_TelegraphedActive = false;
            m_Time = 0f;
        }

        void OnDisable()
        {
            if (m_Health != null) m_Health.OnHit -= OnHit;
        }

        // An attack that isn't interrupted keeps its animation; the hit reaction plays Hurt when it is.
        void OnHit()
        {
            if (!m_TelegraphedActive) PlayOneShot(CharacterAnim.Hurt);
        }

        /// <summary>Plays an action once, then returns to the state-driven animation.</summary>
        public void PlayOneShot(CharacterAnim action)
        {
            if (m_Set == null || m_Set.Find(action) == null) return;
            m_OneShot = action;
            m_OneShotActive = true;
            m_Current = action;
            m_Time = 0f;
        }

        /// <summary>
        /// Plays an attack animation timed to its telegraph: the frames before
        /// <paramref name="releaseFrame"/> fill the telegraph, and the rest play after it. Facing
        /// stays towards <paramref name="direction"/> until <see cref="StopTelegraphed"/>.
        /// </summary>
        public void PlayTelegraphed(CharacterAnim action, float telegraph, int releaseFrame, Vector2 direction)
        {
            if (m_Set == null || m_Set.Find(action) == null) return;
            m_TelegraphedActive = true;
            m_Telegraphed = action;
            m_TelegraphTime = telegraph;
            m_ReleaseFrame = releaseFrame;
            m_TelegraphStartedAt = Time.time;
            m_TelegraphDirection = direction;
            m_OneShotActive = false;
        }

        public void StopTelegraphed() => m_TelegraphedActive = false;

        /// <summary>Holds an action (looping or on its last frame) until released with <see cref="Release"/>.</summary>
        public void Hold(CharacterAnim action)
        {
            if (m_Set == null || m_Set.Find(action) == null) return;
            m_HeldActive = true;
            m_Held = action;
        }

        public void Release() => m_HeldActive = false;

        void LateUpdate()
        {
            if (m_Set == null || m_Renderer == null) return;
            UpdateFacing();
            CharacterAnim wanted = StateAnimation();
            bool dead = wanted == CharacterAnim.Die;
            if (dead || wanted == CharacterAnim.Dodge)
            {
                m_OneShotActive = false;
                m_TelegraphedActive = false;
                m_HeldActive = false;
            }

            if (m_TelegraphedActive)
            {
                ShowTelegraphed();
                return;
            }
            if (m_OneShotActive)
            {
                SpriteAnim oneShot = m_Set.Find(m_OneShot);
                int count = oneShot.For(Facing).Length;
                if (SpriteAnimationMath.IsFinished(m_Time, count, oneShot.frameDuration, false)) m_OneShotActive = false;
                else wanted = m_OneShot;
            }
            else if (m_HeldActive && !dead)
            {
                wanted = m_Held;
            }

            if (wanted != m_Current)
            {
                m_Current = wanted;
                m_Time = 0f;
            }
            Show(m_Set, m_Renderer);
            if (m_ShadowSet != null && m_ShadowRenderer != null) Show(m_ShadowSet, m_ShadowRenderer);
            m_Time += Time.deltaTime;
        }

        CharacterAnim StateAnimation()
        {
            if (m_Character == null) return CharacterAnim.Idle;
            if (m_Character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead) return CharacterAnim.Die;

            if (m_HandleWeapon != null && m_HandleWeapon.CurrentWeapon != null)
            {
                Weapon.WeaponStates state = m_HandleWeapon.CurrentWeapon.WeaponState.CurrentState;
                if (state == Weapon.WeaponStates.WeaponUse && m_LastWeaponState != Weapon.WeaponStates.WeaponUse)
                    PlayOneShot(CharacterAnim.Attack);
                m_LastWeaponState = state;
            }

            if (m_HandleHeavy != null && m_HandleHeavy.CurrentWeapon is ChargeWeapon charge)
            {
                bool inUse = AnyStepInUse(charge);
                if (inUse && !m_HeavyWasInUse) PlayOneShot(CharacterAnim.HeavyAttack);
                m_HeavyWasInUse = inUse;

                if (charge.Charging && !m_WasCharging) m_ChargeStartedAt = Time.time;
                m_WasCharging = charge.Charging;
                if (charge.Charging && !m_OneShotActive)
                {
                    // Wind up once, then loop the charged pose until release.
                    SpriteAnim windUp = m_Set.Find(CharacterAnim.Charge);
                    float windUpLength = windUp != null ? SpriteAnimationMath.Length(windUp.For(Facing).Length, windUp.frameDuration) : 0f;
                    return Time.time - m_ChargeStartedAt < windUpLength || m_Set.Find(CharacterAnim.ChargeHold) == null
                        ? CharacterAnim.Charge
                        : CharacterAnim.ChargeHold;
                }
            }

            switch (m_Character.MovementState.CurrentState)
            {
                case CharacterStates.MovementStates.Dashing:
                    return m_Set.Find(CharacterAnim.Dodge) != null ? CharacterAnim.Dodge : CharacterAnim.Walk;
                case CharacterStates.MovementStates.Walking:
                case CharacterStates.MovementStates.Running:
                    return CharacterAnim.Walk;
                default:
                    return CharacterAnim.Idle;
            }
        }

        static bool AnyStepInUse(ChargeWeapon charge)
        {
            if (charge.Weapons == null) return false;
            foreach (ChargeWeaponStep step in charge.Weapons)
                if (step.TargetWeapon != null && step.TargetWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponUse)
                    return true;
            return false;
        }

        void UpdateFacing()
        {
            if (m_Character != null && m_Character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead) return;
            Vector2 direction = Vector2.zero;
            bool attacking = m_OneShotActive && m_OneShot == CharacterAnim.Attack;
            if (m_TelegraphedActive)
                direction = m_TelegraphDirection;
            else if ((attacking || m_FaceAim && !Rolling) && m_HandleWeapon != null && m_HandleWeapon.WeaponAimComponent != null &&
                     m_HandleWeapon.WeaponAimComponent.CurrentAim.sqrMagnitude > 0.01f)
                direction = m_HandleWeapon.WeaponAimComponent.CurrentAim;
            else if (m_FaceAim && !Rolling && m_Look != null && m_Look.Direction != Vector2.zero)
                direction = m_Look.Direction;
            else if (m_Controller != null && m_Controller.CurrentMovement.sqrMagnitude > 0.01f)
                direction = m_Controller.CurrentMovement.normalized;
            Facing = FacingLogic.FromDirection(direction.x, direction.y, Facing);
        }

        bool Rolling => m_Character != null && m_Character.MovementState != null &&
                        m_Character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing;

        void ShowTelegraphed()
        {
            m_Current = m_Telegraphed;
            float elapsed = Time.time - m_TelegraphStartedAt;
            ShowTelegraphed(m_Set, m_Renderer, elapsed);
            if (m_ShadowSet != null && m_ShadowRenderer != null) ShowTelegraphed(m_ShadowSet, m_ShadowRenderer, elapsed);
        }

        void ShowTelegraphed(SpriteAnimationSet set, SpriteRenderer target, float elapsed)
        {
            SpriteAnim anim = set.Find(m_Telegraphed) ?? set.Find(CharacterAnim.Idle);
            if (anim == null) return;
            Sprite[] frames = anim.For(Facing);
            if (frames == null || frames.Length == 0) return;
            target.flipX = false;
            target.sprite = frames[SpriteAnimationMath.TelegraphedFrame(elapsed, m_TelegraphTime, m_ReleaseFrame, frames.Length, anim.frameDuration)];
        }

        void Show(SpriteAnimationSet set, SpriteRenderer target)
        {
            SpriteAnim anim = set.Find(m_Current) ?? set.Find(CharacterAnim.Idle);
            if (anim == null) return;
            // A front-only sheet (the dodge) borrows the walk's back frames, so a character facing away keeps facing away.
            if (anim.walkForBack && (Facing == Facing4.BackRight || Facing == Facing4.BackLeft) && !anim.HasOwn(Facing))
                anim = set.Find(CharacterAnim.Walk) ?? anim;
            Sprite[] frames = anim.For(Facing, out bool mirrored);
            if (frames == null || frames.Length == 0) return;
            target.flipX = mirrored;
            target.sprite = frames[SpriteAnimationMath.FrameAt(m_Time, frames.Length, anim.frameDuration, anim.loop)];
        }
    }
}
