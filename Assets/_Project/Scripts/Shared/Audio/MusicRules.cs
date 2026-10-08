using System.Collections.Generic;
using Hearthdelve.Shared.Game;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>What's playing (2026-10-07: HeatleyBros tracks, docs/CREDITS.md). None is silence.</summary>
    public enum MusicCue
    {
        None,
        /// <summary>The free day, in Tally Ho! and Kariaston ("Quirkii").</summary>
        Day,
        /// <summary>Decorate Mode ("Continue").</summary>
        Decorate,
        /// <summary>The evening at Tally Ho!: Prep, service, the results ("Coastal Market").</summary>
        Service,
        /// <summary>The first region of the Hollows, the Cellars ("Otherworld"). Later regions add their own cue.</summary>
        Cellars,
    }

    /// <summary>
    /// Which music the moment wants (pure). The day's phase decides; something that holds a cue (Decorate Mode) wins over it.
    /// The menu, arrival day (the opening: the first music is the Hollows', the owner's call) and the night's summary are quiet.
    /// </summary>
    public static class MusicRules
    {
        public static MusicCue Pick(bool inGame, DayPhase phase, MusicCue held, bool arriving = false)
        {
            if (!inGame || arriving) return MusicCue.None;
            if (held != MusicCue.None) return held;
            return phase switch
            {
                DayPhase.Daytime => MusicCue.Day,
                DayPhase.Evening => MusicCue.Service,
                DayPhase.Delve => MusicCue.Cellars,
                _ => MusicCue.None,
            };
        }

        /// <summary>
        /// Leaving <paramref name="from"/> for <paramref name="to"/>: whether to pause it (and resume it later where it was)
        /// rather than stop it. Only the day pauses for Decorate Mode, so leaving decorating picks the day's tune up again.
        /// </summary>
        public static bool PausesFor(MusicCue from, MusicCue to) => from == MusicCue.Day && to == MusicCue.Decorate;

        /// <summary>How loud everything but the music is: quieter in the Hollows (the owner's call, 2026-10-07).</summary>
        public static float EffectsLevel(bool inGame, DayPhase phase, float inHollows) => inGame && phase == DayPhase.Delve ? inHollows : 1f;
    }

    /// <summary>
    /// Things that ask for a cue over the day's (Decorate Mode), by key; the latest holder wins, and releasing restores the rest.
    /// </summary>
    public static class MusicHolds
    {
        static readonly List<(object key, MusicCue cue)> s_Holds = new();

        public static MusicCue Current => s_Holds.Count > 0 ? s_Holds[^1].cue : MusicCue.None;

        public static void Hold(object key, MusicCue cue)
        {
            Release(key);
            if (key != null && cue != MusicCue.None) s_Holds.Add((key, cue));
        }

        public static void Release(object key) => s_Holds.RemoveAll(h => h.key == key);

        public static void Set(object key, MusicCue cue, bool held)
        {
            if (held) Hold(key, cue);
            else Release(key);
        }

        /// <summary>Tests, and leaving a game.</summary>
        public static void Clear() => s_Holds.Clear();
    }
}
