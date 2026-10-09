using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>One cue's track.</summary>
    [Serializable]
    public sealed class MusicTrack
    {
        public MusicCue cue;
        public AudioClip clip;
        [Range(0f, 1f), Tooltip("This track's own level, under the music volume, to even out tracks mastered louder than the rest (Quirkii 0.6375: a quarter down, the owner's call, 2026-10-08, then 15% down after the second 4i-C playtest).")]
        public float level = 1f;
    }

    /// <summary>The music's tracks and tuning (2026-10-07; first-pass values). Read by <see cref="MusicDirector"/> in Boot.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Audio/Music", fileName = "MusicConfig")]
    public sealed class MusicConfig : ScriptableObject
    {
        public List<MusicTrack> tracks = new();
        [Range(0f, 1f), Tooltip("Music volume at the player's 100% (0.140625: what 25% gave before, the owner's call after the 4i-C playtest; it was 0.5625, a quarter down twice). The music slider scales it down from there.")]
        public float volume = 0.140625f;
        [Range(0f, 1f), Tooltip("Sound effects everywhere, at the player's 100% (0.75: a quarter down, the owner's call after the 4i-C playtest). On the mixer's Effects volume, with the Hollows' level on top.")]
        public float effects = 0.75f;
        [Range(0f, 1f), Tooltip("Sound effects in the Hollows (0.75: a quarter down, the owner's call). Applied on the mixer's Effects volume there (since 4i-B); the music is kept at its own level.")]
        public float effectsInHollows = 0.75f;
        [Min(0f), Tooltip("Seconds to fade one tune out and the next in (real time: dialogue and pauses don't stop a fade).")]
        public float fadeSeconds = 1.5f;
        [Min(0f), Tooltip("Seconds of quiet before a tune starts to fade in (1.5, the owner's call, 2026-10-08). Real time.")]
        public float startDelay = 1.5f;
        [Range(0f, 1f), Tooltip("4i-C: the music while a conversation is open (0.7, about 3 dB down, so the blips and the line come forward). 1: no ducking.")]
        public float duckUnderDialogue = 0.7f;
        [Min(0.01f), Tooltip("Seconds the duck takes to settle, either way (real time).")]
        public float duckSeconds = 0.4f;

        public AudioClip Clip(MusicCue cue) => Track(cue)?.clip;

        /// <summary>The cue's own level (1 when it has no track).</summary>
        public float Level(MusicCue cue) => Track(cue)?.level ?? 1f;

        MusicTrack Track(MusicCue cue)
        {
            foreach (MusicTrack t in tracks)
                if (t != null && t.cue == cue) return t;
            return null;
        }
    }
}
