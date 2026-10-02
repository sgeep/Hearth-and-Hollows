using Hearthdelve.Core.Animation;
using Hearthdelve.Core.Movement;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    /// <summary>
    /// Shows a TDE character with Minifantasy sprite sheets: picks the action from the
    /// character's state (idle, walk, dodge, attack, hurt, dead) and one of the four drawn
    /// facings from its movement or aim. An optional second renderer shows the matching
    /// shadow sheet.
    /// </summary>
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteAnimationSet m_Set;
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] SpriteAnimationSet m_ShadowSet;
        [SerializeField] SpriteRenderer m_ShadowRenderer;
        [SerializeField] Facing4 m_InitialFacing = Facing4.FrontRight;

        Character m_Character;
        TopDownController m_Controller;
        Health m_Health;
        CharacterHandleWeapon m_HandleWeapon;

        CharacterAnim m_Current = CharacterAnim.Idle;
        CharacterAnim m_OneShot;
        bool m_OneShotActive;
        float m_Time;
        Weapon.WeaponStates m_LastWeaponState = Weapon.WeaponStates.WeaponIdle;

        public Facing4 Facing { get; private set; }
        public CharacterAnim Current => m_Current;

        public void Configure(SpriteAnimationSet set, SpriteRenderer renderer, SpriteAnimationSet shadowSet, SpriteRenderer shadowRenderer)
        {
            m_Set = set;
            m_Renderer = renderer;
            m_ShadowSet = shadowSet;
            m_ShadowRenderer = shadowRenderer;
        }

        void Awake()
        {
            Facing = m_InitialFacing;
            m_Character = GetComponentInParent<Character>();
            if (m_Character == null) return;
            m_Controller = m_Character.GetComponent<TopDownController>();
            m_Health = m_Character.GetComponent<Health>();
            m_HandleWeapon = m_Character.GetComponent<CharacterHandleWeapon>();
        }

        void OnEnable()
        {
            if (m_Health != null) m_Health.OnHit += OnHit;
            m_OneShotActive = false;
            m_Time = 0f;
        }

        void OnDisable()
        {
            if (m_Health != null) m_Health.OnHit -= OnHit;
        }

        void OnHit() => PlayOneShot(CharacterAnim.Hurt);

        /// <summary>Plays an action once, then returns to the state-driven animation.</summary>
        public void PlayOneShot(CharacterAnim action)
        {
            if (m_Set == null || m_Set.Find(action) == null) return;
            m_OneShot = action;
            m_OneShotActive = true;
            m_Current = action;
            m_Time = 0f;
        }

        void LateUpdate()
        {
            if (m_Set == null || m_Renderer == null) return;

            UpdateFacing();
            CharacterAnim wanted = StateAnimation();

            if (wanted == CharacterAnim.Die || wanted == CharacterAnim.Dodge) m_OneShotActive = false;
            if (m_OneShotActive)
            {
                SpriteAnim oneShot = m_Set.Find(m_OneShot);
                int count = oneShot.For(Facing).Length;
                if (SpriteAnimationMath.IsFinished(m_Time, count, oneShot.frameDuration, false)) m_OneShotActive = false;
                else wanted = m_OneShot;
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

        void UpdateFacing()
        {
            if (m_Character != null && m_Character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead) return;

            Vector2 direction = Vector2.zero;
            bool attacking = m_OneShotActive && m_OneShot == CharacterAnim.Attack;
            if (attacking && m_HandleWeapon != null && m_HandleWeapon.WeaponAimComponent != null)
                direction = m_HandleWeapon.WeaponAimComponent.CurrentAim;
            else if (m_Controller != null && m_Controller.CurrentMovement.sqrMagnitude > 0.01f)
                direction = m_Controller.CurrentMovement.normalized;

            Facing = FacingLogic.FromDirection(direction.x, direction.y, Facing);
        }

        void Show(SpriteAnimationSet set, SpriteRenderer target)
        {
            SpriteAnim anim = set.Find(m_Current) ?? set.Find(CharacterAnim.Idle);
            if (anim == null) return;
            Sprite[] frames = anim.For(Facing);
            if (frames == null || frames.Length == 0) return;
            target.sprite = frames[SpriteAnimationMath.FrameAt(m_Time, frames.Length, anim.frameDuration, anim.loop)];
        }
    }
}
