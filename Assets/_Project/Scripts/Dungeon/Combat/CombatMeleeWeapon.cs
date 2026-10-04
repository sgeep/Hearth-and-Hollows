using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>What a player attack carries to whatever it hits, beyond TDE's damage and knockback.</summary>
    public readonly struct HitContext
    {
        public readonly WeaponDefinition Weapon;
        public readonly AttackData Attack;
        public readonly bool IsHeavy;
        public readonly Vector2 From;

        public HitContext(WeaponDefinition weapon, AttackData attack, bool isHeavy, Vector2 from)
        {
            Weapon = weapon;
            Attack = attack;
            IsHeavy = isHeavy;
            From = from;
        }
    }

    /// <summary>Anything that reacts to a player attack beyond losing health (stagger, the kill's harvest context).</summary>
    public interface IHitReceiver
    {
        void ReceiveHit(in HitContext hit);
    }

    /// <summary>
    /// A TDE melee attack that knows which <see cref="AttackData"/> and <see cref="WeaponDefinition"/>
    /// it belongs to. TDE still deals the damage and knockback; when the attack lands, this passes the
    /// rest of the hit (stagger time, weapon for the harvest) to the target's <see cref="IHitReceiver"/>.
    /// Extends TDE through its virtual <c>CreateDamageArea</c> instead of modifying it.
    /// </summary>
    [AddComponentMenu("Hearthdelve/Weapons/Combat Melee Weapon")]
    public class CombatMeleeWeapon : MeleeWeapon
    {
        [SerializeField] WeaponDefinition m_Weapon;
        [SerializeField] bool m_IsHeavy;
        AttackData m_Attack;

        public AttackData Attack => m_Attack;
        public WeaponDefinition Weapon => m_Weapon;
        public bool IsHeavy => m_IsHeavy;

        /// <summary>Sets TDE's damage, timing, reach, knockback and hit-stop from the attack's data.</summary>
        public void Apply(WeaponDefinition weapon, AttackData attack, bool isHeavy)
        {
            m_Weapon = weapon;
            m_Attack = attack;
            m_IsHeavy = isHeavy;
            if (attack == null) return;

            MinDamageCaused = attack.damage;
            MaxDamageCaused = attack.damage;
            InitialDelay = FrameData.ToSeconds(attack.startupFrames);
            ActiveDuration = FrameData.ToSeconds(attack.activeFrames);
            // One hit per target per swing: the target stays invincible to this swing until its hitbox closes
            // (a shorter window let the heavy's 12-frame spin land twice).
            InvincibilityDuration = FrameData.OneHitInvincibility(ActiveDuration);
            TimeBetweenUses = FrameData.ToSeconds(Mathf.Clamp(attack.cancelFrame, 1, attack.TotalFrames));
            if (isHeavy)
            {
                // The heavy is a spin: a circle around the player.
                DamageAreaShape = MeleeDamageAreaShapes.Circle;
                AreaOffset = Vector3.zero;
                AreaSize = new Vector3(attack.hitboxSize.x, attack.hitboxSize.x, 1f);
            }
            else
            {
                // The weapon turns towards the aim, so reach is along its local X axis.
                AreaOffset = new Vector3(attack.hitboxOffset.x, 0f, 0f);
                AreaSize = new Vector3(attack.hitboxSize.x, attack.hitboxSize.y, 1f);
            }
            // Knockback is the target's HitReaction slide: TDE's force knockback doesn't move our 2D characters.
            Knockback = DamageOnTouch.KnockbackStyles.NoKnockback;

            // The hit's one combined feedback (hit-stop, shake, sound, haptic) takes its sizes from the data.
            if (HitDamageableFeedback is MMF_Player player)
            {
                MMF_HitStop hitStop = player.GetFeedbackOfType<MMF_HitStop>();
                if (hitStop != null) hitStop.FreezeFrameDuration = attack.hitStop;
                MMF_ScreenShake shake = player.GetFeedbackOfType<MMF_ScreenShake>();
                if (shake != null) shake.Force = attack.screenShake;
            }
        }

        /// <summary>The run's powers (4d step 4) scale the attack's damage as the swing begins.</summary>
        protected override void EnableDamageArea()
        {
            if (_damageOnTouch != null && m_Attack != null)
            {
                Hearthdelve.Shared.Run.RunModifiers run = Run.DelveRunController.CurrentModifiers;
                float damage = DamageCalculator.Scale(m_Attack.damage, m_IsHeavy ? run.HeavyDamageMultiplier : run.LightDamageMultiplier);
                _damageOnTouch.MinDamageCaused = damage;
                _damageOnTouch.MaxDamageCaused = damage;
            }
            base.EnableDamageArea();
        }

        protected override void CreateDamageArea()
        {
            base.CreateDamageArea();
            if (_damageOnTouch == null) return;
            _damageOnTouch.HitDamageableEvent ??= new UnityEvent<Health>();
            _damageOnTouch.HitDamageableEvent.AddListener(OnHitDamageable);
        }

        void OnHitDamageable(Health health)
        {
            if (health == null || m_Attack == null) return;
            Vector2 from = Owner != null ? (Vector2)Owner.transform.position : (Vector2)transform.position;
            var receiver = health.GetComponentInParent<IHitReceiver>();
            receiver?.ReceiveHit(new HitContext(m_Weapon, m_Attack, m_IsHeavy, from));
        }
    }
}
