using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Applies a <see cref="WeaponDefinition"/> to a TDE combo weapon: each
    /// <see cref="AttackData"/> sets the damage, timing and reach of the matching
    /// <see cref="MeleeWeapon"/>, so combat is tuned in the weapon asset.
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
            MeleeWeapon[] attacks = GetComponents<MeleeWeapon>();
            for (int i = 0; i < attacks.Length && i < m_Definition.combo.Count; i++)
            {
                AttackData data = m_Definition.combo[i];
                MeleeWeapon attack = attacks[i];
                attack.MinDamageCaused = data.damage;
                attack.MaxDamageCaused = data.damage;
                attack.InitialDelay = FrameData.ToSeconds(data.startupFrames);
                attack.ActiveDuration = FrameData.ToSeconds(data.activeFrames);
                attack.TimeBetweenUses = FrameData.ToSeconds(Mathf.Clamp(data.cancelFrame, 1, data.TotalFrames));
                // The weapon turns towards the aim, so reach is along its local X axis.
                attack.AreaOffset = new Vector3(data.hitboxOffset.x, 0f, 0f);
                attack.AreaSize = new Vector3(data.hitboxSize.x, data.hitboxSize.y, 1f);
            }

            ComboWeapon combo = GetComponent<ComboWeapon>();
            combo.DroppableCombo = true;
            combo.DropComboDelay = Mathf.Max(0.05f, m_Definition.comboLinkWindow);
        }
    }
}
