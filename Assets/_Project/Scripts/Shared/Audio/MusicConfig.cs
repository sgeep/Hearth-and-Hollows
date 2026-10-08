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
        [Range(0f, 1f), Tooltip("This track's own level, under the music volume, to even out tracks mastered louder than the rest (Quirkii 0.75: a quarter down, the owner's call, 2026-10-08).")]
        public float level = 1f;
    }

    /// <summary>The music's tracks and tuning (2026-10-07; first-pass values). Read by <see cref="MusicDirector"/> in Boot.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Audio/Music", fileName = "MusicConfig")]
    public sealed class MusicConfig : ScriptableObject
    {
        public List<MusicTrack> tracks = new();
        [Range(0f, 1f), Tooltip("Music volume (0.5625: a quarter down, then a quarter down again, the owner's calls). The options menu (4i) will scale this.")]
        public float volume = 0.5625f;
        [Range(0f, 1f), Tooltip("Sound effects in the Hollows (0.75: a quarter down, the owner's call). Applied to the listener there; the music is kept at its own level.")]
        public float effectsInHollows = 0.75f;
        [Min(0f), Tooltip("Seconds to fade one tune out and the next in (real time: dialogue and pauses don't stop a fade).")]
        public float fadeSeconds = 1.5f;

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
