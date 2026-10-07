using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using UnityEngine;

namespace Hearthdelve.Shared.Surface
{
    /// <summary>The surface day's broad parts (4h): the world's routines change at their edges.</summary>
    public enum SurfaceBand
    {
        Morning,
        Afternoon,
        /// <summary>From the cutoff (5 PM): the village winds down and the clock rests.</summary>
        Evening,
    }

    /// <summary>
    /// Tuning for the surface day (4h Checkpoint A; test values, not balance). Times are minutes of the day (8:00 = 480).
    /// </summary>
    [Serializable]
    public struct SurfaceClockSettings
    {
        [Tooltip("When the day begins (minutes of the day; 480 = 8:00).")]
        public int dayStartMinute;
        [Tooltip("When the village winds down and the clock stops (1020 = 17:00).")]
        public int cutoffMinute;
        [Tooltip("When the afternoon begins (720 = 12:00).")]
        public int afternoonStartMinute;
        [Min(0.05f), Tooltip("Real seconds per game minute while the clock runs (3.9: 8:00 to 17:00 in about 35 minutes).")]
        public float realSecondsPerGameMinute;
        [Min(0.01f), Tooltip("The most real time one frame may add (a stalled or suspended frame never skips the day).")]
        public float maxRealSecondsPerFrame;
        [Min(1), Tooltip("The clock moves on screen (and the world's routines step) every this many game minutes.")]
        public int displayStepMinutes;
        [Tooltip("When the market opens and closes (minutes of the day).")]
        public int marketOpenMinute;
        public int marketCloseMinute;
        [Tooltip("Pause the clock while the keeper is inside Tally Ho! (a tuning switch to compare in the playtest).")]
        public bool pauseIndoors;

        public static SurfaceClockSettings Default => new()
        {
            dayStartMinute = 8 * 60,
            cutoffMinute = 17 * 60,
            afternoonStartMinute = 12 * 60,
            realSecondsPerGameMinute = 35f * 60f / (9 * 60),
            maxRealSecondsPerFrame = 0.1f,
            displayStepMinutes = 10,
            marketOpenMinute = 8 * 60,
            marketCloseMinute = 17 * 60,
            pauseIndoors = false,
        };
    }

    /// <summary>
    /// The one surface clock (4h): the minute of the day, advanced by real time only while it runs, and never past the
    /// cutoff. It drives the world's texture (market hours, light, later routines); it never ends the day or starts Prep.
    /// Pure logic, never read from <c>Time.time</c> or the wall clock.
    /// </summary>
    public sealed class SurfaceClock
    {
        public SurfaceClock(int startMinute = 8 * 60) => Minute = startMinute;

        /// <summary>The minute of the day, with its fraction.</summary>
        public double Minute { get; private set; }

        public int WholeMinute => (int)Math.Floor(Minute);

        /// <summary>The start of the day.</summary>
        public void Reset(in SurfaceClockSettings settings) => Minute = settings.dayStartMinute;

        /// <summary>Sets the minute directly (tests, debugging), held to the day's start and the cutoff.</summary>
        public void Set(int minute, in SurfaceClockSettings settings) =>
            Minute = Math.Clamp(minute, Math.Min(settings.dayStartMinute, settings.cutoffMinute), settings.cutoffMinute);

        /// <summary>The village has wound down: the clock is at the cutoff and stays there.</summary>
        public bool AtCutoff(in SurfaceClockSettings settings) => Minute >= settings.cutoffMinute;

        /// <summary>
        /// Moves the clock on by <paramref name="realSeconds"/> of running time, capped per call (a stalled frame or a
        /// suspended browser tab adds at most the cap). Returns whether the minute changed.
        /// </summary>
        public bool Advance(float realSeconds, in SurfaceClockSettings settings)
        {
            if (realSeconds <= 0f || AtCutoff(settings) || settings.realSecondsPerGameMinute <= 0f) return false;
            float step = Math.Min(realSeconds, Math.Max(0f, settings.maxRealSecondsPerFrame));
            double before = Minute;
            Minute = Math.Min(settings.cutoffMinute, Minute + step / settings.realSecondsPerGameMinute);
            return Minute != before;
        }

        public SurfaceBand Band(in SurfaceClockSettings settings) => BandAt(WholeMinute, settings);

        /// <summary>The displayed minute: the clock's minute rounded down to the display step.</summary>
        public int Shown(in SurfaceClockSettings settings) => Snap(WholeMinute, settings.displayStepMinutes);

        public static SurfaceBand BandAt(int minute, in SurfaceClockSettings settings) =>
            minute >= settings.cutoffMinute ? SurfaceBand.Evening : minute >= settings.afternoonStartMinute ? SurfaceBand.Afternoon : SurfaceBand.Morning;

        public static int Snap(int minute, int step) => step <= 1 ? minute : minute / step * step;

        /// <summary>The 12-hour face of a minute: 13:05 is (1, 5, pm). Words and order are the string table's.</summary>
        public static (int hour, int minute, bool pm) Face(int minuteOfDay)
        {
            int m = ((minuteOfDay % 1440) + 1440) % 1440;
            int hour24 = m / 60;
            int hour = hour24 % 12 == 0 ? 12 : hour24 % 12;
            return (hour, m % 60, hour24 >= 12);
        }
    }

    /// <summary>When the market trades (4h): from opening until the cutoff. Pure.</summary>
    public static class MarketHours
    {
        public static bool IsOpen(int minute, in SurfaceClockSettings settings) => minute >= settings.marketOpenMinute && minute < settings.marketCloseMinute;
    }

    /// <summary>
    /// Things that hold the surface clock still besides conversations, full-screen menus and transitions (which the clock
    /// checks itself): Decorate Mode, panels, story scenes. Each holder adds and removes itself by key; nesting is safe.
    /// </summary>
    public static class SurfacePause
    {
        static readonly HashSet<object> s_Holds = new();

        public static bool IsHeld => s_Holds.Count > 0;

        public static void Hold(object key)
        {
            if (key != null) s_Holds.Add(key);
        }

        public static void Release(object key)
        {
            if (key != null) s_Holds.Remove(key);
        }

        public static void Set(object key, bool held)
        {
            if (held) Hold(key);
            else Release(key);
        }

        /// <summary>Tests, and leaving a scene with holds outstanding.</summary>
        public static void Clear() => s_Holds.Clear();
    }

    /// <summary>The surface clock moved a display step (and once when a day starts): routines, lights, the market and the HUD listen.</summary>
    public readonly struct SurfaceTimeChanged : IEvent
    {
        public readonly int Minute;
        public readonly SurfaceBand Band;

        public SurfaceTimeChanged(int minute, SurfaceBand band)
        {
            Minute = minute;
            Band = band;
        }
    }

    /// <summary>The market stall was used while it trades (4h): the UI opens the market panel over the existing market.</summary>
    public readonly struct MarketStallUsed : IEvent { }

    /// <summary>A new band began (published once as it starts; also once when a day starts). The 5 PM wind-down is Evening.</summary>
    public readonly struct SurfaceBandStarted : IEvent
    {
        public readonly SurfaceBand Band;
        public readonly int Day;

        public SurfaceBandStarted(SurfaceBand band, int day)
        {
            Band = band;
            Day = day;
        }
    }
}
