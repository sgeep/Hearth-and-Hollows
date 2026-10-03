using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>One step of the heavy attack: how long the button must be held, and the attack it releases.</summary>
    [Serializable]
    public sealed class HeavyChargeStep
    {
        [Min(0), Tooltip("Hold time (seconds) from which releasing gives this step. The first step is 0: a tap.")]
        public float chargeTime;
        public AttackData attack = new();
    }

    /// <summary>
    /// Pure rules for the hold-to-charge heavy attack (GDD §4.1). Releasing gives the last step
    /// whose charge time has passed. TDE's <c>ChargeWeapon</c> runs the charge; this converts our
    /// charge times into its per-step durations, which must give the same answer.
    /// </summary>
    public static class HeavyCharge
    {
        /// <summary>The smallest step duration handed to TDE, so a step never lasts zero time.</summary>
        public const float MinStepDuration = 0.01f;

        /// <summary>Index of the step a release after <paramref name="held"/> seconds gives, or -1 without steps.</summary>
        public static int StepAt(float held, IReadOnlyList<float> chargeTimes)
        {
            if (chargeTimes == null || chargeTimes.Count == 0) return -1;
            int step = 0;
            for (int i = 1; i < chargeTimes.Count; i++)
                if (held >= chargeTimes[i]) step = i;
            return step;
        }

        /// <summary>
        /// TDE charge-step durations: step i stays current until the next step's charge time, so its
        /// duration is the gap to the next one (the last step's duration doesn't matter).
        /// </summary>
        public static float[] StepDurations(IReadOnlyList<float> chargeTimes)
        {
            if (chargeTimes == null) return Array.Empty<float>();
            var durations = new float[chargeTimes.Count];
            for (int i = 0; i < durations.Length; i++)
            {
                float next = i + 1 < chargeTimes.Count ? chargeTimes[i + 1] : chargeTimes[i] + 1f;
                durations[i] = Math.Max(MinStepDuration, next - chargeTimes[i]);
            }
            return durations;
        }

        /// <summary>Which step TDE's charge weapon is on after <paramref name="held"/> seconds, given the durations above.</summary>
        public static int TdeStepAt(float held, IReadOnlyList<float> durations)
        {
            float total = 0f;
            for (int i = 0; i < durations.Count; i++)
            {
                total += durations[i];
                if (total > held) return i;
            }
            return durations.Count - 1;
        }

        /// <summary>Problems with a step list (empty if fine): the first step must be a tap, and times must rise.</summary>
        public static List<string> Validate(IReadOnlyList<float> chargeTimes)
        {
            var problems = new List<string>();
            if (chargeTimes == null || chargeTimes.Count == 0)
            {
                problems.Add("no heavy steps");
                return problems;
            }
            if (chargeTimes[0] != 0f) problems.Add("the first step must have charge time 0 (a tap)");
            for (int i = 1; i < chargeTimes.Count; i++)
                if (chargeTimes[i] <= chargeTimes[i - 1]) problems.Add($"step {i} must need a longer hold than step {i - 1}");
            return problems;
        }
    }
}
