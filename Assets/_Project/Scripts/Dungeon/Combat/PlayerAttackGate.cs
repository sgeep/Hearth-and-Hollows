using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Keeps the light combo and the heavy attack apart: while the heavy is charging or swinging the
    /// combo can't start, and while a combo hit is under way the heavy can't. Also slows the player
    /// while charging (<see cref="WeaponDefinition.heavyChargeMoveMultiplier"/>).
    /// </summary>
    [DefaultExecutionOrder(-50)] // before TDE's abilities read their input
    public sealed class PlayerAttackGate : MonoBehaviour
    {
        CharacterHandleWeapon m_Light;
        CharacterHandleSecondaryWeapon m_Heavy;
        CharacterMovement m_Movement;
        bool m_Slowed;

        /// <summary>True while the heavy attack is charging or its swing is under way.</summary>
        public bool HeavyBusy { get; private set; }

        void Awake()
        {
            foreach (CharacterHandleWeapon handle in GetComponents<CharacterHandleWeapon>())
            {
                if (handle is CharacterHandleSecondaryWeapon secondary) m_Heavy = secondary;
                else if (m_Light == null) m_Light = handle;
            }
            m_Movement = GetComponent<CharacterMovement>();
        }

        void OnDisable() => SetSlowed(false, 1f);

        void Update()
        {
            var charge = m_Heavy != null ? m_Heavy.CurrentWeapon as ChargeWeapon : null;
            bool charging = charge != null && charge.Charging;
            HeavyBusy = charging || (charge != null && StepInUse(charge));
            bool lightBusy = m_Light != null && m_Light.CurrentWeapon != null &&
                             m_Light.CurrentWeapon.WeaponState.CurrentState is Weapon.WeaponStates.WeaponDelayBeforeUse or Weapon.WeaponStates.WeaponUse;

            if (m_Light != null && m_Light.AbilityPermitted == HeavyBusy) m_Light.PermitAbility(!HeavyBusy);
            if (m_Heavy != null && m_Heavy.AbilityPermitted == (lightBusy && !HeavyBusy)) m_Heavy.PermitAbility(!(lightBusy && !HeavyBusy));

            float multiplier = charge != null && charge.TryGetComponent(out HeavyWeaponTuning tuning) && tuning.Definition != null
                ? tuning.Definition.heavyChargeMoveMultiplier
                : 1f;
            SetSlowed(charging, multiplier);
        }

        void SetSlowed(bool slowed, float multiplier)
        {
            if (m_Movement == null || slowed == m_Slowed) return;
            m_Slowed = slowed;
            m_Movement.MovementSpeedMultiplier = slowed ? multiplier : 1f;
        }

        static bool StepInUse(ChargeWeapon charge)
        {
            if (charge.Weapons == null) return false;
            foreach (ChargeWeaponStep step in charge.Weapons)
                if (step.TargetWeapon != null && step.TargetWeapon.WeaponState.CurrentState is Weapon.WeaponStates.WeaponDelayBeforeUse or Weapon.WeaponStates.WeaponUse)
                    return true;
            return false;
        }
    }
}
