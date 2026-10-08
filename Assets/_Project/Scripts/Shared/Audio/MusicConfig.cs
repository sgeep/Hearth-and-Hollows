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
    }

    /// <summary>The music's tracks and tuning (2026-10-07; first-pass values). Read by <see cref="MusicDirector"/> in Boot.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Audio/Music", fileName = "MusicConfig")]
    public sealed class MusicConfig : ScriptableObject
    {
        public List<MusicTrack> tracks = new();
        [Range(0f, 1f), Tooltip("Music volume (0.75: a quarter down, the owner's starting point). The options menu (4i) will scale this.")]
        public float volume = 0.75f;
        [Min(0f), Tooltip("Seconds to fade one tune out and the next in (real time: dialogue and pauses don't stop a fade).")]
        public float fadeSeconds = 1.5f;

        public AudioClip Clip(MusicCue cue)
        {
            foreach (MusicTrack t in tracks)
                if (t != null && t.cue == cue) return t.clip;
            return null;
        }
    }
}
