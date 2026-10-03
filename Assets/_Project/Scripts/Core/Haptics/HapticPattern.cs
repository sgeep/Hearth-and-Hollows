using System;
using UnityEngine;

namespace Hearthdelve.Core.Haptics
{
    /// <summary>Motor strengths at one moment: the low motor gives heavy thuds, the high motor a light buzz.</summary>
    public readonly struct HapticSample
    {
        public readonly float Low;
        public readonly float High;

        public HapticSample(float low, float high)
        {
            Low = low;
            High = high;
        }

        public bool IsSilent => Low <= 0f && High <= 0f;
        public static HapticSample Silent => default;
    }

    /// <summary>One point of a pattern's envelope. Strengths are interpolated between keys.</summary>
    [Serializable]
    public struct HapticKey
    {
        [Min(0)] public float time;
        [Range(0, 1)] public float low;
        [Range(0, 1)] public float high;

        public HapticKey(float time, float low, float high)
        {
            this.time = time;
            this.low = low;
            this.high = high;
        }
    }

    /// <summary>
    /// A named haptic pattern (GDD §9A): an envelope for the two motors. Gameplay plays
    /// patterns by reference or id, never raw motor values.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Haptics/Pattern", fileName = "Haptic_")]
    public sealed class HapticPattern : ScriptableObject
    {
        [Tooltip("The name gameplay uses, e.g. Tap.Firm.")]
        public string id;
        [Tooltip("Envelope keys in time order. The pattern ends at the last key.")]
        public HapticKey[] keys = Array.Empty<HapticKey>();

        public float Duration => HapticCurve.Duration(keys);
        public HapticSample Sample(float time) => HapticCurve.Sample(keys, time);
    }

    /// <summary>Pure envelope maths for <see cref="HapticPattern"/>.</summary>
    public static class HapticCurve
    {
        public static float Duration(HapticKey[] keys) =>
            keys == null || keys.Length == 0 ? 0f : Math.Max(0f, keys[keys.Length - 1].time);

        /// <summary>
        /// Strengths at <paramref name="time"/>: the first key's values up to its time, linear
        /// between keys, and silent from the last key's time onwards.
        /// </summary>
        public static HapticSample Sample(HapticKey[] keys, float time)
        {
            if (keys == null || keys.Length == 0 || time < 0f) return HapticSample.Silent;
            if (time >= keys[keys.Length - 1].time) return HapticSample.Silent;
            if (time <= keys[0].time) return new HapticSample(keys[0].low, keys[0].high);

            for (int i = 1; i < keys.Length; i++)
            {
                if (time > keys[i].time) continue;
                HapticKey a = keys[i - 1], b = keys[i];
                float span = b.time - a.time;
                float t = span <= 1e-6f ? 1f : (time - a.time) / span;
                return new HapticSample(a.low + (b.low - a.low) * t, a.high + (b.high - a.high) * t);
            }
            return HapticSample.Silent;
        }
    }
}
