using UnityEngine;

namespace Hearthdelve.Core.Services
{
    /// <summary>
    /// Single owner of Time.timeScale. Menus (death screen, swap prompt) and hit-stop both
    /// freeze time; tracking them separately means ending one never unfreezes the other.
    /// </summary>
    public static class GamePause
    {
        static int s_MenuPauses;
        static bool s_HitStop;

        public static bool IsMenuPaused => s_MenuPauses > 0;
        public static bool IsHitStopped => s_HitStop;

        public static void PushMenuPause() { s_MenuPauses++; Apply(); }

        public static void PopMenuPause()
        {
            if (s_MenuPauses > 0) s_MenuPauses--;
            Apply();
        }

        public static void SetHitStop(bool active)
        {
            if (s_HitStop == active) return;
            s_HitStop = active;
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            s_MenuPauses = 0;
            s_HitStop = false;
            Time.timeScale = 1f;
        }

        static void Apply() => Time.timeScale = (s_MenuPauses > 0 || s_HitStop) ? 0f : 1f;
    }
}
