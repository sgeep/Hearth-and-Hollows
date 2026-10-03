using System.Collections.Generic;
using System.Linq;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Applies a <see cref="WeaponDefinition"/>'s heavy steps to a TDE <see cref="ChargeWeapon"/>:
    /// one <see cref="CombatMeleeWeapon"/> per step, with charge durations from
    /// <see cref="HeavyCharge"/>. Hold the heavy button to charge; release fires the last step
    /// reached. Runs in Awake, before TDE initialises the charge weapon.
    /// </summary>
    [RequireComponent(typeof(ChargeWeapon))]
    public sealed class HeavyWeaponTuning : MonoBehaviour
    {
        [SerializeField] WeaponDefinition m_Definition;

        public WeaponDefinition Definition => m_Definition;

        public void Configure(WeaponDefinition definition) => m_Definition = definition;

        void Awake() => Apply();

        public void Apply()
        {
            if (m_Definition == null || m_Definition.heavy.Count == 0) return;
            var charge = GetComponent<ChargeWeapon>();
            CombatMeleeWeapon[] steps = GetComponents<CombatMeleeWeapon>();
            List<float> times = m_Definition.heavy.Select(s => s.chargeTime).ToList();
            float[] durations = HeavyCharge.StepDurations(times);

            charge.Weapons ??= new List<ChargeWeaponStep>();
            int count = Mathf.Min(steps.Length, m_Definition.heavy.Count);
            while (charge.Weapons.Count < count) charge.Weapons.Add(new ChargeWeaponStep());
            if (charge.Weapons.Count > count) charge.Weapons.RemoveRange(count, charge.Weapons.Count - count);
            for (int i = 0; i < count; i++)
            {
                steps[i].Apply(m_Definition, m_Definition.heavy[i].attack, isHeavy: true);
                ChargeWeaponStep step = charge.Weapons[i];
                step.TargetWeapon = steps[i];
                step.ChargeDuration = durations[i];
                // Releasing part-way through a step still gives that step: it was reached.
                step.TriggerIfChargeInterrupted = true;
            }
            charge.ReleaseMode = ChargeWeapon.ReleaseModes.OnInputRelease;
            charge.AllowInitialShot = false;
        }
    }
}
