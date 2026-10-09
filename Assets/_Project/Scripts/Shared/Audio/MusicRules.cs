using System.Collections.Generic;
using UnityEngine;
using Hearthdelve.Shared.Game;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>What's playing (2026-10-07: HeatleyBros tracks, docs/CREDITS.md). None is silence.</summary>
    public enum MusicCue
    {
        None,
        /// <summary>The free day out in Kariaston ("Quirkii"; since 2026-10-08 Tally Ho! itself is quiet).</summary>
        Day,
        /// <summary>Decorate Mode ("Continue").</summary>
        Decorate,
        /// <summary>The evening's service and its results ("Coastal Market"; since 2026-10-08 not during Prep).</summary>
        Service,
        /// <summary>The first region of the Hollows, the Cellars ("Otherworld"). Later regions add their own cue.</summary>
        Cellars,
        /// <summary>Held for a quiet story moment (4h Checkpoint D: Gimp in the night): nothing plays.</summary>
        Silence,
    }

    /// <summary>
    /// Which music the moment wants (pure). Something that holds a cue (Decorate Mode) wins over the rest. Otherwise (the owner's
    /// call, 2026-10-08): the day's tune only out in Kariaston (<paramref name="outdoors"/>), Tally Ho! itself quiet by day; the
    /// evening's tune only once service has begun (<paramref name="serving"/>: service and its results), so Prep is quiet; the Hollows
    /// their own. The menu, arrival day (the first music is the Hollows', the owner's call) and the night's summary are quiet.
    /// </summary>
    public static class MusicRules
    {
        public static MusicCue Pick(bool inGame, DayPhase phase, MusicCue held, bool arriving = false, bool outdoors = false, bool serving = false)
        {
            if (!inGame || arriving) return MusicCue.None;
            if (held == MusicCue.Silence) return MusicCue.None;
            if (held != MusicCue.None) return held;
            return phase switch
            {
                DayPhase.Daytime => outdoors ? MusicCue.Day : MusicCue.None,
                DayPhase.Evening => serving ? MusicCue.Service : MusicCue.None,
                DayPhase.Delve => MusicCue.Cellars,
                _ => MusicCue.None,
            };
        }

        /// <summary>The tavern's parts of the evening that have the evening's music: service and its results (never Prep).</summary>
        public static bool IsServing(string tavernPhase) => tavernPhase is "Service" or "Results";

        /// <summary>
        /// Leaving <paramref name="from"/> for <paramref name="to"/>: whether to pause it (and resume it later where it was)
        /// rather than stop it. Only the day pauses for Decorate Mode, so leaving decorating picks the day's tune up again.
        /// </summary>
        public static bool PausesFor(MusicCue from, MusicCue to) => from == MusicCue.Day && to == MusicCue.Decorate;

        /// <summary>How loud everything but the music is: quieter in the Hollows (the owner's call, 2026-10-07).</summary>
        public static float EffectsLevel(bool inGame, DayPhase phase, float inHollows, float everywhere = 1f) =>
            everywhere * (inGame && phase == DayPhase.Delve ? inHollows : 1f);

        /// <summary>4i-C: the music's level while someone's talking (<paramref name="underDialogue"/>), otherwise full.</summary>
        public static float Duck(bool talking, float underDialogue) => talking ? Mathf.Clamp01(underDialogue) : 1f;
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
