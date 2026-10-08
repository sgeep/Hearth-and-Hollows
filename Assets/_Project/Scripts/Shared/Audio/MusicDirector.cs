using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>
    /// The background music (2026-10-07), in Boot beside <see cref="GameFlow"/>: each frame it asks <see cref="MusicRules"/>
    /// which cue the moment wants and crossfades to it in real time (a conversation or a pause never holds a fade). Every tune
    /// loops. The day's tune pauses for Decorate Mode and picks up where it was; anything else stops, and a stopped tune's
    /// audio data is unloaded (only what's playing is decoded: the web build decodes whole tracks in memory).
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        [SerializeField] MusicConfig m_Config;

        sealed class Voice
        {
            public AudioSource Source;
            public float Target;
            public bool PauseAtSilence;
        }

        readonly Dictionary<MusicCue, Voice> m_Voices = new();

        public static MusicDirector Instance { get; private set; }

        /// <summary>The cue playing (or fading in) now.</summary>
        public MusicCue Current { get; private set; } = MusicCue.None;

        public MusicConfig Config => m_Config;

        public void Configure(MusicConfig config) => m_Config = config;

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

        /// <summary>The cue the moment wants.</summary>
        public static MusicCue Wanted
        {
            get
            {
                GameFlow flow = GameFlow.Instance;
                bool inGame = flow != null && flow.InGame;
                return MusicRules.Pick(inGame, inGame ? flow.State.Phase : DayPhase.Daytime, MusicHolds.Current);
            }
        }

        void Update()
        {
            if (m_Config == null) return;
            MusicCue wanted = Wanted;
            if (wanted != Current) Switch(wanted);
            Fade(Time.unscaledDeltaTime);
        }

        void Switch(MusicCue to)
        {
            if (m_Voices.TryGetValue(Current, out Voice from))
            {
                from.Target = 0f;
                from.PauseAtSilence = MusicRules.PausesFor(Current, to);
            }
            Current = to;
            AudioClip clip = m_Config.Clip(to);
            if (clip == null) return;
            if (!m_Voices.TryGetValue(to, out Voice voice))
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
                source.volume = 0f;
                m_Voices[to] = voice = new Voice { Source = source };
            }
            voice.Target = 1f;
            voice.PauseAtSilence = false;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            if (!voice.Source.isPlaying)
            {
                // Paused: picks up where it was. Stopped: from the top.
                if (voice.Source.time > 0f) voice.Source.UnPause();
                else voice.Source.Play();
            }
        }

        void Fade(float dt)
        {
            float step = m_Config.fadeSeconds > 0f ? dt / m_Config.fadeSeconds : 1f;
            foreach (KeyValuePair<MusicCue, Voice> pair in m_Voices)
            {
                Voice v = pair.Value;
                float level = Mathf.MoveTowards(v.Source.volume / Mathf.Max(0.0001f, m_Config.volume), v.Target, step);
                v.Source.volume = level * m_Config.volume;
                if (v.Target > 0f || level > 0f || !v.Source.isPlaying) continue;
                if (v.PauseAtSilence) v.Source.Pause();
                else
                {
                    v.Source.Stop();
                    v.Source.time = 0f;
                    if (v.Source.clip != null) v.Source.clip.UnloadAudioData();
                }
            }
        }
    }
}
