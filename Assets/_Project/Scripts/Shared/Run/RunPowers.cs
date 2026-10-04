using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Random;

namespace Hearthdelve.Shared.Run
{
    /// <summary>What the run's powers add up to, applied on top of the base tuning and the day's loadout.</summary>
    public readonly struct RunModifiers
    {
        public readonly float MaxEssenceBonus;
        /// <summary>1 = normal drain.</summary>
        public readonly float DrainMultiplier;
        /// <summary>1 = hits cost their full Essence.</summary>
        public readonly float HitCostMultiplier;
        public readonly float LightDamageMultiplier;
        public readonly float HeavyDamageMultiplier;
        public readonly float DodgeCooldownMultiplier;
        public readonly float EssenceOnClear;
        /// <summary>Scales the overkill and destroy thresholds of the harvest rules.</summary>
        public readonly float OverkillToleranceMultiplier;

        public RunModifiers(float maxEssenceBonus, float drain, float hitCost, float light, float heavy, float dodge, float onClear, float overkill)
        {
            MaxEssenceBonus = maxEssenceBonus;
            DrainMultiplier = drain;
            HitCostMultiplier = hitCost;
            LightDamageMultiplier = light;
            HeavyDamageMultiplier = heavy;
            DodgeCooldownMultiplier = dodge;
            EssenceOnClear = onClear;
            OverkillToleranceMultiplier = overkill;
        }

        public static RunModifiers None => new(0f, 1f, 1f, 1f, 1f, 1f, 0f, 1f);
    }

    /// <summary>
    /// The powers taken this run (pure logic). Each power can be taken once; reductions multiply (two 30% cuts make
    /// 49%), bonuses add, and no multiplier falls below <see cref="MinMultiplier"/>.
    /// </summary>
    public sealed class RunPowers
    {
        public const float MinMultiplier = 0.2f;

        readonly List<RunPowerDefinition> m_Taken = new();

        public IReadOnlyList<RunPowerDefinition> Taken => m_Taken;
        public RunModifiers Modifiers { get; private set; } = RunModifiers.None;
        /// <summary>A power was taken, with the new totals.</summary>
        public event Action<RunPowerDefinition> Changed;

        public bool Has(RunPowerDefinition power) => power != null && m_Taken.Any(p => p.id == power.id);

        /// <summary>False if it's missing or already taken.</summary>
        public bool Take(RunPowerDefinition power)
        {
            if (power == null || Has(power)) return false;
            m_Taken.Add(power);
            Modifiers = Combine(m_Taken);
            Changed?.Invoke(power);
            return true;
        }

        public static RunModifiers Combine(IEnumerable<RunPowerDefinition> powers)
        {
            float max = 0f, drain = 1f, hit = 1f, light = 1f, heavy = 1f, dodge = 1f, onClear = 0f, overkill = 1f;
            foreach (RunPowerDefinition p in powers)
            {
                if (p == null) continue;
                float a = Math.Max(0f, p.amount);
                switch (p.effect)
                {
                    case RunPowerEffect.MaxEssence: max += a; break;
                    case RunPowerEffect.SlowerDrain: drain *= 1f - a; break;
                    case RunPowerEffect.LighterHits: hit *= 1f - a; break;
                    case RunPowerEffect.LightDamage: light += a; break;
                    case RunPowerEffect.HeavyDamage: heavy += a; break;
                    case RunPowerEffect.FasterDodge: dodge *= 1f - a; break;
                    case RunPowerEffect.EssenceOnClear: onClear += a; break;
                    case RunPowerEffect.GentleKills: overkill += a; break;
                }
            }
            return new RunModifiers(max, Floor(drain), Floor(hit), light, heavy, Floor(dodge), onClear, overkill);
        }

        static float Floor(float multiplier) => Math.Max(MinMultiplier, multiplier);

        /// <summary>
        /// Up to <paramref name="count"/> different powers from the pool that this run hasn't taken, in a random order
        /// (seeded, so a run's seed and choices replay its offers).
        /// </summary>
        public List<RunPowerDefinition> Offer(IEnumerable<RunPowerDefinition> pool, int count, IRandom random)
        {
            List<RunPowerDefinition> open = (pool ?? Enumerable.Empty<RunPowerDefinition>())
                .Where(p => p != null && !string.IsNullOrEmpty(p.id) && !Has(p))
                .GroupBy(p => p.id).Select(g => g.First()).ToList();
            for (int i = open.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i);
                (open[i], open[j]) = (open[j], open[i]);
            }
            return open.Take(Math.Max(0, count)).ToList();
        }

        /// <summary>The number a power's description shows: flat for Essence, a percentage for the rest.</summary>
        public static int ShownAmount(RunPowerDefinition power) => power == null ? 0
            : power.effect is RunPowerEffect.MaxEssence or RunPowerEffect.EssenceOnClear
                ? (int)Math.Round(power.amount)
                : (int)Math.Round(power.amount * 100f);
    }
}
