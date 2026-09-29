using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Dungeon.Combat
{
    public enum WeaponType
    {
        Cleaver,
        FilletingBlade,
        Tenderizer,
        SkewerSpear,
        FryingPan,
        Traditional,
    }

    public enum WeaponRarity
    {
        Common,
        Fine,
        Masterwork,
        Legendary,
    }

    /// <summary>A weapon's combo, element, and harvest specialty (GDD §4.2).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Weapon Definition", fileName = "Weapon_")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public WeaponType weaponType = WeaponType.Cleaver;
        public WeaponRarity rarity = WeaponRarity.Common;
        public Element element = Element.None;

        [Tooltip("Killing a monster with this weapon is a Clean Kill for parts of these categories.")]
        public IngredientCategory cleanKillCategories = IngredientCategory.Meat | IngredientCategory.Offal;

        [Tooltip("Combo chain, in order. The last hit loops back to the first.")]
        public List<AttackData> combo = new();

        [Min(0), Tooltip("How early an attack press is remembered.")]
        public float inputBuffer = 0.15f;
        [Min(0), Tooltip("After an attack fully recovers, how long a press still continues the combo.")]
        public float comboLinkWindow = 0.2f;

        /// <summary>Damage of the weakest attack in the combo (the clean finisher).</summary>
        public float LightestHitDamage
        {
            get
            {
                float lightest = float.MaxValue;
                foreach (var attack in combo)
                    if (attack != null && attack.damage > 0f && attack.damage < lightest) lightest = attack.damage;
                return lightest == float.MaxValue ? 0f : lightest;
            }
        }
    }
}
