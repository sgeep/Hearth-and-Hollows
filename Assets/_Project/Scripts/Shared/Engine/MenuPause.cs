using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Pauses gameplay under a menu (the swap prompt, the death screen). It goes through MMFeedbacks'
    /// time manager, which also runs hit-stop freeze frames, so the two never fight over
    /// <c>Time.timeScale</c>. Pauses nest: gameplay resumes when the last one is popped. Without a
    /// time manager in the scene it sets the time scale directly.
    /// </summary>
    public static class MenuPause
    {
        static int s_Count;
        static bool s_Direct;

        public static bool IsPaused => s_Count > 0;

        public static void Push()
        {
            if (s_Count++ > 0) return;
            s_Direct = Object.FindAnyObjectByType<MMTimeManager>() == null;
            if (s_Direct) Time.timeScale = 0f;
            else MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0f, false, 0f, true);
        }

        public static void Pop()
        {
            if (s_Count == 0 || --s_Count > 0) return;
            if (s_Direct) Time.timeScale = 1f;
            else MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Unfreeze, 1f, 0f, false, 0f, false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            s_Count = 0;
            s_Direct = false;
        }
    }
}
