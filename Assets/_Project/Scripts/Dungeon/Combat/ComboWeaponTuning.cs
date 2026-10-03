using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Applies a <see cref="WeaponDefinition"/> to a TDE combo weapon: each combo
    /// <see cref="AttackData"/> sets the damage, timing, reach, knockback and hit-stop of the matching
    /// <see cref="CombatMeleeWeapon"/>, so combat is tuned in the weapon asset.
    /// </summary>
    [RequireComponent(typeof(ComboWeapon))]
    public sealed class ComboWeaponTuning : MonoBehaviour
    {
        [SerializeField] WeaponDefinition m_Definition;

        public WeaponDefinition Definition => m_Definition;

        public void Configure(WeaponDefinition definition) => m_Definition = definition;

        void Awake() => Apply();

        public void Apply()
        {
            if (m_Definition == null) return;
            CombatMeleeWeapon[] attacks = GetComponents<CombatMeleeWeapon>();
            for (int i = 0; i < attacks.Length && i < m_Definition.combo.Count; i++)
                attacks[i].Apply(m_Definition, m_Definition.combo[i], isHeavy: false);

            ComboWeapon combo = GetComponent<ComboWeapon>();
            combo.DroppableCombo = true;
            combo.DropComboDelay = Mathf.Max(0.05f, m_Definition.comboLinkWindow);
        }
    }
}
