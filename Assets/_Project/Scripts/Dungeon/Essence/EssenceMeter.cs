using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Essence
{
    /// <summary>Base Essence tuning (the delve timer and the player's only health pool).</summary>
    [Serializable]
    public struct EssenceSettings
    {
        [Min(1)] public float baseMax;
        [Min(0), Tooltip("Essence lost per second in the dungeon.")]
        public float drainPerSecond;
        [Min(0), Tooltip("Essence lost per point of damage taken.")]
        public float damageMultiplier;
        [Range(0, 1), Tooltip("Below this fraction the HUD warns the player.")]
        public float lowThreshold;

        public static EssenceSettings Default => new()
        {
            baseMax = 100f,
            // 4d playtest (2026-10-05): drain alone lasts about 3⅓ minutes at base: floor 1 and some of floor 2, not a full
            // Cellars run (about 6 minutes). A full run needs gear: upgrades, the delve meal, powers (GDD §4.4, §7.2).
            drainPerSecond = 0.5f,
            damageMultiplier = 1f,
            lowThreshold = 0.25f,
        };
    }

    /// <summary>Permanent upgrades bought in the tavern (applied on top of <see cref="EssenceSettings"/>).</summary>
    public struct EssenceModifiers
    {
        public float MaxBonus;
        /// <summary>1 = normal drain, 0.8 = 20% slower.</summary>
        public float DrainMultiplier;

        public static EssenceModifiers None => new() { MaxBonus = 0f, DrainMultiplier = 1f };
    }

    /// <summary>
    /// Essence: drains over time and on damage; reaching zero ends the delve (treated as death).
    /// Pure logic; <see cref="Depleted"/> fires exactly once.
    /// </summary>
    public sealed class EssenceMeter
    {
        readonly EssenceSettings m_Settings;
        readonly EssenceModifiers m_Modifiers;

        public EssenceMeter(EssenceSettings settings, EssenceModifiers modifiers)
        {
            m_Settings = settings;
            m_Modifiers = modifiers;
            Max = Math.Max(1f, settings.baseMax + modifiers.MaxBonus);
            Current = Max;
        }

        public float Max { get; private set; }
        /// <summary>The upgrade and delve meal modifiers this meter was built with.</summary>
        public EssenceModifiers Modifiers => m_Modifiers;
        public float Current { get; private set; }
        public float Normalized => Current / Max;
        public bool IsLow => Normalized <= m_Settings.lowThreshold;
        public bool IsDepleted { get; private set; }
        public float DrainPerSecond => m_Settings.drainPerSecond * Math.Max(0f, m_Modifiers.DrainMultiplier) * Math.Max(0f, RunDrainMultiplier)
                                       * Math.Max(0f, EncounterDrainMultiplier);

        /// <summary>An encounter's rule (4e: a boss fight pauses drain): 1 = normal, 0 = no passive drain. Hits still cost.</summary>
        public float EncounterDrainMultiplier { get; set; } = 1f;

        /// <summary>The run's powers (4d step 4): 1 = normal drain.</summary>
        public float RunDrainMultiplier { get; set; } = 1f;
        /// <summary>The run's powers (4d step 4): 1 = hits cost their full Essence.</summary>
        public float HitCostMultiplier { get; set; } = 1f;

        /// <summary>Raises the maximum and Essence alike (a run power).</summary>
        public void RaiseMax(float amount)
        {
            if (IsDepleted || amount <= 0f) return;
            Max += amount;
            Current += amount;
            Changed?.Invoke(false);
        }

        /// <summary>When true, time-based drain stops (menus, cutscenes, safe rooms). Damage still applies.</summary>
        public bool Paused { get; set; }

        /// <summary>Argument is true when the change came from damage.</summary>
        public event Action<bool> Changed;
        public event Action Depleted;

        public void Tick(float deltaTime)
        {
            if (Paused || IsDepleted || deltaTime <= 0f) return;
            Drain(DrainPerSecond * deltaTime, fromDamage: false);
        }

        /// <summary>Returns the Essence actually lost.</summary>
        public float TakeDamage(float damage)
        {
            if (IsDepleted || damage <= 0f) return 0f;
            return Drain(damage * m_Settings.damageMultiplier * Math.Max(0f, HitCostMultiplier), fromDamage: true);
        }

        public void Restore(float amount)
        {
            if (IsDepleted || amount <= 0f) return;
            float before = Current;
            Current = Math.Min(Max, Current + amount);
            if (Current != before) Changed?.Invoke(false);
        }

        float Drain(float amount, bool fromDamage)
        {
            if (amount <= 0f) return 0f;
            float before = Current;
            Current = Math.Max(0f, Current - amount);
            Changed?.Invoke(fromDamage);
            if (Current <= 0f && !IsDepleted)
            {
                IsDepleted = true;
                Depleted?.Invoke();
            }
            return before - Current;
        }
    }
}
