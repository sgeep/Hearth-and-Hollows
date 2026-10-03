using System;

namespace Hearthdelve.Core.Haptics
{
    /// <summary>The player's vibration settings, as the haptic maths needs them.</summary>
    public readonly struct HapticSettings
    {
        public readonly bool Enabled;
        /// <summary>The intensity slider, 0–1.</summary>
        public readonly float Intensity;
        /// <summary>The reduced-intensity accessibility option.</summary>
        public readonly bool Reduced;
        /// <summary>The strongest output allowed while <see cref="Reduced"/> is on, 0–1.</summary>
        public readonly float ReducedCap;

        public HapticSettings(bool enabled, float intensity, bool reduced, float reducedCap)
        {
            Enabled = enabled;
            Intensity = intensity;
            Reduced = reduced;
            ReducedCap = reducedCap;
        }
    }

    /// <summary>
    /// Maps a gameplay value onto a rumble strength: values from <see cref="inMin"/> to
    /// <see cref="inMax"/> become <see cref="outMin"/> to <see cref="outMax"/>, clamped at
    /// both ends. <see cref="exponent"/> above 1 keeps the rumble low until late (a "rising"
    /// feel); below 1 makes it kick in early.
    /// </summary>
    [Serializable]
    public struct HapticRamp
    {
        public float inMin;
        public float inMax;
        public float outMin;
        public float outMax;
        public float exponent;

        public HapticRamp(float inMin, float inMax, float outMin, float outMax, float exponent = 1f)
        {
            this.inMin = inMin;
            this.inMax = inMax;
            this.outMin = outMin;
            this.outMax = outMax;
            this.exponent = exponent;
        }

        public float Evaluate(float value)
        {
            float span = inMax - inMin;
            if (Math.Abs(span) < 1e-6f) return value >= inMax ? outMax : outMin;
            float t = HapticMath.Clamp01((value - inMin) / span);
            if (exponent > 0f && Math.Abs(exponent - 1f) > 1e-6f) t = (float)Math.Pow(t, exponent);
            return outMin + (outMax - outMin) * t;
        }
    }

    /// <summary>Pure haptic maths: settings scaling, layering and timing.</summary>
    public static class HapticMath
    {
        /// <summary>
        /// The strength actually sent to the controller for a pattern authored at
        /// <paramref name="amplitude"/> (0–1): zero when vibration is off, scaled by the
        /// intensity slider, and capped when the reduced-intensity option is on.
        /// </summary>
        public static float ApplySettings(float amplitude, HapticSettings settings)
        {
            if (!settings.Enabled) return 0f;
            float result = Clamp01(amplitude) * Clamp01(settings.Intensity);
            if (settings.Reduced) result = Math.Min(result, Clamp01(settings.ReducedCap));
            return result;
        }

        /// <summary>
        /// A controller has one output, so a one-shot playing over a continuous rumble
        /// takes the stronger of the two rather than adding up.
        /// </summary>
        public static float Layer(float continuous, float oneShot) => Math.Max(Clamp01(continuous), Clamp01(oneShot));

        /// <summary>
        /// Seconds between warning heartbeats. No heartbeat (returns -1) above
        /// <paramref name="lowThreshold"/>; from there the beat speeds up from
        /// <paramref name="slowInterval"/> to <paramref name="fastInterval"/> as the
        /// normalized value falls to zero.
        /// </summary>
        public static float HeartbeatInterval(float normalized, float lowThreshold, float slowInterval, float fastInterval)
        {
            if (lowThreshold <= 0f || normalized > lowThreshold) return -1f;
            float t = Clamp01(normalized / lowThreshold);
            return fastInterval + (slowInterval - fastInterval) * t;
        }

        public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
