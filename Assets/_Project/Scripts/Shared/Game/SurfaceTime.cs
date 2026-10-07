using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using UnityEngine;

namespace Hearthdelve.Shared.Game
{
    /// <summary>Why the surface clock is standing still (or that it isn't). For tests and the debug overlay.</summary>
    public enum SurfaceStill
    {
        Running,
        NotDaytime,
        AtCutoff,
        Talking,
        Menu,
        Held,
        Loading,
        Unfocused,
        Indoors,
    }

    /// <summary>
    /// Advances the one surface clock (4h): in the Boot scene beside <see cref="GameFlow"/>, the only thing that moves
    /// <see cref="GameState.Surface"/>. It runs only in the free daytime (after the opening's arrival day), stops at the
    /// cutoff, and stands still while someone talks, a full-screen menu or a transition is up, something holds it
    /// (<see cref="SurfacePause"/>: Decorate Mode, panels, story scenes) or the window has lost focus. Real time comes in
    /// capped per frame, never from the wall clock, so a suspended browser tab resumes where it was.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class SurfaceTime : MonoBehaviour
    {
        [SerializeField] SurfaceClockConfig m_Config;

        bool m_Focused = true;
        int m_LastShown = -1;
        SurfaceBand m_LastBand;
        int m_LastDay = -1;

        public static SurfaceTime Instance { get; private set; }

        /// <summary>The keeper is inside Tally Ho! (set by the property's areas; the clock pauses there only if tuned to).</summary>
        public static bool Indoors { get; set; }

        public SurfaceClockSettings Settings => SettingsOverride ?? (m_Config != null ? m_Config.settings : SurfaceClockSettings.Default);

        /// <summary>Tests: settings to use instead of the tuning asset (a fast clock, a different cutoff).</summary>
        public static SurfaceClockSettings? SettingsOverride { get; set; }

        /// <summary>The live clock, or null outside a game.</summary>
        public static SurfaceClock Clock => GameFlow.Instance != null && GameFlow.Instance.InGame ? GameFlow.Instance.State.Surface : null;

        public static SurfaceClockSettings CurrentSettings => Instance != null ? Instance.Settings : SettingsOverride ?? SurfaceClockSettings.Default;

        /// <summary>The minute on the clock face (rounded to the display step), or the day's start outside a game.</summary>
        public static int ShownMinute
        {
            get
            {
                SurfaceClock clock = Clock;
                SurfaceClockSettings s = CurrentSettings;
                return clock != null ? clock.Shown(s) : s.dayStartMinute;
            }
        }

        public static SurfaceBand Band => Clock != null ? Clock.Band(CurrentSettings) : SurfaceBand.Morning;

        /// <summary>The market trades now (daytime, before the cutoff).</summary>
        public static bool MarketOpen => Clock != null && MarketHours.IsOpen(Clock.WholeMinute, CurrentSettings);

        public void Configure(SurfaceClockConfig config) => m_Config = config;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnApplicationFocus(bool focus) => m_Focused = focus;
        void OnApplicationPause(bool paused) => m_Focused = !paused;

        /// <summary>Whether the clock would move this frame, and if not, why.</summary>
        public SurfaceStill Still
        {
            get
            {
                GameFlow flow = GameFlow.Instance;
                if (flow == null || !flow.InGame || flow.State.Phase != DayPhase.Daytime || flow.State.Story.Opening == OpeningStage.Arrival)
                    return SurfaceStill.NotDaytime;
                if (flow.State.Surface.AtCutoff(Settings)) return SurfaceStill.AtCutoff;
                if (flow.IsLoading || (flow.Transition != null && flow.Transition.IsCovering)) return SurfaceStill.Loading;
                if (StoryServices.Conversations != null && StoryServices.Conversations.IsTalking) return SurfaceStill.Talking;
                if (MenuPause.IsPaused) return SurfaceStill.Menu;
                if (SurfacePause.IsHeld) return SurfaceStill.Held;
                if (!m_Focused && !Application.isEditor) return SurfaceStill.Unfocused;
                if (Settings.pauseIndoors && Indoors) return SurfaceStill.Indoors;
                return SurfaceStill.Running;
            }
        }

        void Update()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame)
            {
                m_LastDay = -1;
                return;
            }
            SurfaceClockSettings settings = Settings;
            SurfaceClock clock = flow.State.Surface;
            if (Still == SurfaceStill.Running) clock.Advance(Time.unscaledDeltaTime, settings);
            Announce(flow.State.Day, clock, settings);
        }

        /// <summary>Publishes a display step, and a band when it begins (once each; again when a new day starts).</summary>
        void Announce(int day, SurfaceClock clock, in SurfaceClockSettings settings)
        {
            int shown = clock.Shown(settings);
            SurfaceBand band = clock.Band(settings);
            bool newDay = day != m_LastDay;
            if (newDay || shown != m_LastShown) EventBus<SurfaceTimeChanged>.Publish(new SurfaceTimeChanged(shown, band));
            if (newDay || band != m_LastBand) EventBus<SurfaceBandStarted>.Publish(new SurfaceBandStarted(band, day));
            m_LastDay = day;
            m_LastShown = shown;
            m_LastBand = band;
        }

        /// <summary>A new day (or a loaded one): the next frame announces the time afresh.</summary>
        public void Restart() => m_LastDay = -1;
    }
}
