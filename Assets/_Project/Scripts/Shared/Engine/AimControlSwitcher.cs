using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Locked design: aim by movement direction on a gamepad and by the mouse on keyboard and
    /// mouse. Switches the current weapon's aim control to follow the device last used.
    /// </summary>
    [RequireComponent(typeof(CharacterHandleWeapon))]
    public sealed class AimControlSwitcher : MonoBehaviour
    {
        CharacterHandleWeapon m_HandleWeapon;
        Character m_Character;

        void Awake()
        {
            m_HandleWeapon = GetComponent<CharacterHandleWeapon>();
            m_Character = GetComponent<Character>();
        }

        void Update()
        {
            WeaponAim aim = m_HandleWeapon.WeaponAimComponent;
            if (aim == null || m_Character == null) return;
            bool pointer = m_Character.LinkedInputManager is HearthdelveInputManager input && input.PointerAim;
            WeaponAim.AimControls wanted = pointer ? WeaponAim.AimControls.Mouse : WeaponAim.AimControls.PrimaryMovement;
            if (aim.AimControl != wanted) aim.AimControl = wanted;
        }
    }
}
