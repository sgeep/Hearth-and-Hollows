using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>When the time steps have drifted from the project's own (4i-C; pure, EditMode-tested).</summary>
    public static class TimeBaselineRules
    {
        /// <summary>
        /// At the normal time scale, a step that differs from the project's is drift to undo. While time is scaled (a hit-stop,
        /// a pause) the scaled steps are meant, and left alone.
        /// </summary>
        public static bool ShouldRestore(float timeScale, float current, float baseline) =>
            Mathf.Approximately(timeScale, 1f) && !Mathf.Approximately(current, baseline);
    }

    /// <summary>
    /// Keeps Unity's time steps at the project's own values whenever time runs normally (4i-C). TDE's <c>MMTimeManager</c> scales
    /// <c>Time.maximumDeltaTime</c> and <c>Time.fixedDeltaTime</c> with the time scale from the values it found when it started;
    /// each scene's manager starts afresh, so one that started during an earlier one's hit-stop or pause took the reduced values
    /// as normal, and they compounded across scene loads (found 2026-10-09: the cap on a frame's game time stuck at 0.03 s
    /// instead of 0.33 s, so any frame slower than 30 ms ran the game in slow motion: a slow machine or a web hitch, and the
    /// village tests ran three to four times slower late in a full run). Made at start-up, in no scene; vendor code untouched.
    /// </summary>
    public sealed class TimeBaseline : MonoBehaviour
    {
        static TimeBaseline s_Instance;
        static float s_MaxDelta, s_FixedDelta;

        /// <summary>The project's own steps, as they were before any scene could change them.</summary>
        public static float MaxDelta => s_MaxDelta;
        public static float FixedDelta => s_FixedDelta;

        /// <summary>How many times drift was undone (tests).</summary>
        public static int Restored { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Instance = null;
            Restored = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (s_Instance != null) return;
            s_MaxDelta = Time.maximumDeltaTime;
            s_FixedDelta = Time.fixedDeltaTime;
            var go = new GameObject("TimeBaseline");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<TimeBaseline>();
        }

        void LateUpdate()
        {
            float scale = Time.timeScale;
            if (TimeBaselineRules.ShouldRestore(scale, Time.maximumDeltaTime, s_MaxDelta))
            {
                Time.maximumDeltaTime = s_MaxDelta;
                Restored++;
            }
            if (TimeBaselineRules.ShouldRestore(scale, Time.fixedDeltaTime, s_FixedDelta))
            {
                Time.fixedDeltaTime = s_FixedDelta;
                Restored++;
            }
        }
    }
}
