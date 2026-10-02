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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            ScreenShakeScale = 1f;
            HitStopEnabled = true;
        }
    }
}
