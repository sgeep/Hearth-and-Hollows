using Hearthdelve.Core.Haptics;
using UnityEngine;

namespace Hearthdelve.Core.Services
{
    /// <summary>
    /// Player-facing accessibility/feel toggles (GDD §4.1, §12). Runtime-only until the
    /// settings menu and save system exist.
    /// </summary>
    public static class GameSettings
    {
        /// <summary>0 = off, 1 = default. Multiplies every screen shake.</summary>
        public static float ScreenShakeScale = 1f;
        public static bool HitStopEnabled = true;
        /// <summary>Screen and sprite flashes (photosensitivity option).</summary>
        public static bool FlashEnabled = true;

        public static bool VibrationEnabled = true;
        /// <summary>The vibration intensity slider, 0–1. Scales every haptic.</summary>
        public static float VibrationIntensity = 1f;
        /// <summary>Accessibility option: caps every haptic at <see cref="ReducedVibrationCap"/>.</summary>
        public static bool ReducedVibration;
        public const float ReducedVibrationCap = 0.4f;

        public static HapticSettings Haptics =>
            new(VibrationEnabled, VibrationIntensity, ReducedVibration, ReducedVibrationCap);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            ScreenShakeScale = 1f;
            HitStopEnabled = true;
            FlashEnabled = true;
            VibrationEnabled = true;
            VibrationIntensity = 1f;
            ReducedVibration = false;
        }
    }
}
