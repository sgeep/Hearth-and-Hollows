using UnityEngine;

namespace Hearthdelve.Shared.Run
{
    /// <summary>What a run power changes. Each is a nudge to tuning that already exists (4d step 4).</summary>
    public enum RunPowerEffect
    {
        /// <summary>Max Essence + amount (and the same Essence now).</summary>
        MaxEssence,
        /// <summary>Essence drains this fraction slower.</summary>
        SlowerDrain,
        /// <summary>Hits cost this fraction less Essence.</summary>
        LighterHits,
        /// <summary>Light (combo) attacks deal this fraction more damage.</summary>
        LightDamage,
        /// <summary>Heavy (charged) attacks deal this fraction more damage.</summary>
        HeavyDamage,
        /// <summary>The dodge roll's cooldown is this fraction shorter.</summary>
        FasterDodge,
        /// <summary>Clearing a room restores this much Essence.</summary>
        EssenceOnClear,
        /// <summary>Overkill (bruised or destroyed parts) needs this fraction more spare damage.</summary>
        GentleKills,
    }

    /// <summary>
    /// A run power (GDD §7.2): a boon chosen one of three in a power room, lasting until the delve ends. Its name and
    /// description are in the UI string table (<c>power.&lt;id&gt;</c>, <c>power.&lt;id&gt;.desc</c>, which takes the
    /// amount as shown by <see cref="RunPowers.ShownAmount"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Dungeon/Run Power", fileName = "Power_")]
    public sealed class RunPowerDefinition : ScriptableObject
    {
        public string id;
        public RunPowerEffect effect;
        [Min(0f), Tooltip("A flat amount (Essence) or a fraction (0.3 = 30%), by effect.")]
        public float amount = 0.25f;
        [Tooltip("16×16, on the choice card and the HUD.")]
        public Sprite icon;
    }
}
