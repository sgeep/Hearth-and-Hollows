using System.Collections.Generic;
using Hearthdelve.Core.Events;
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
            /// <summary>0–1 along the fade (the source's volume is this times the music volume).</summary>
            public float Level;
            public bool PauseAtSilence;
            public MusicCue Cue;
            /// <summary>Real time before which a wanted tune doesn't start (the quiet before it fades in).</summary>
            public float StartAt;
        }

        /// <summary>The tavern's part of the evening, as last announced ("Prep", "Service", "Results", …).</summary>
        static string s_TavernPhase;

        void OnEnable() => EventBus<TavernPhaseStarted>.Subscribe(OnTavernPhase);
        void OnDisable() => EventBus<TavernPhaseStarted>.Unsubscribe(OnTavernPhase);
        static void OnTavernPhase(TavernPhaseStarted e) => s_TavernPhase = e.Phase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_TavernPhase = null;

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
                return MusicRules.Pick(inGame, inGame ? flow.State.Phase : DayPhase.Daytime, MusicHolds.Current,
                    inGame && flow.State.Story.Opening == Hearthdelve.Shared.Story.OpeningStage.Arrival,
                    outdoors: !SurfaceTime.Indoors, serving: MusicRules.IsServing(s_TavernPhase));
            }
        }

        void Update()
        {
            if (m_Config == null) return;
            MusicCue wanted = Wanted;
            if (wanted != Current) Switch(wanted);
            // Sound effects in the Hollows a quarter down (2026-10-07): since 4i-B a mixer parameter (the Effects group), so the
            // music and the listener are untouched.
            GameFlow flow = GameFlow.Instance;
            bool inGame = flow != null && flow.InGame;
            if (AudioMixerHub.Instance != null)
                AudioMixerHub.Instance.HollowsLevel = MusicRules.EffectsLevel(inGame, inGame ? flow.State.Phase : DayPhase.Daytime, m_Config.effectsInHollows);
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
                // 4i-B: through the mixer's Music group (the player's Music and Master sliders).
                source.outputAudioMixerGroup = AudioMixerHub.MusicGroup;
                source.ignoreListenerPause = true;
                source.volume = 0f;
                m_Voices[to] = voice = new Voice { Source = source, Cue = to };
            }
            voice.Target = 1f;
            voice.PauseAtSilence = false;
            if (!voice.Source.isPlaying) voice.StartAt = Time.unscaledTime + m_Config.startDelay;
            Begin(voice);
        }

        /// <summary>
        /// Starts a wanted voice once its audio is in memory (2026-10-08). Loading runs in the background (on the web, the
        /// browser decodes the whole track), and a clip told to play before it has finished is left to the platform to sort
        /// out, so a voice waits, silent, until its clip has loaded.
        /// </summary>
        static void Begin(Voice voice)
        {
            if (voice.Source.isPlaying) return;
            AudioClip clip = voice.Source.clip;
            if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed) clip.LoadAudioData();
            // Loading starts at once; playing waits out the quiet before the tune.
            if (clip.loadState != AudioDataLoadState.Loaded || Time.unscaledTime < voice.StartAt) return;
            // Paused: picks up where it was. Stopped: from the top.
            if (voice.Source.time > 0f) voice.Source.UnPause();
            else voice.Source.Play();
        }

        float m_Duck = 1f;

        /// <summary>The music's dialogue duck now (1: none). Tests.</summary>
        public float Duck => m_Duck;

        void Fade(float dt)
        {
            float step = m_Config.fadeSeconds > 0f ? dt / m_Config.fadeSeconds : 1f;
            // 4i-C: a little down while someone's talking (MusicRules.Duck; the config's level, 1 turns it off).
            bool talking = Hearthdelve.Shared.Story.StoryServices.Conversations != null && Hearthdelve.Shared.Story.StoryServices.Conversations.IsTalking;
            m_Duck = Mathf.MoveTowards(m_Duck, MusicRules.Duck(talking, m_Config.duckUnderDialogue), dt / Mathf.Max(0.01f, m_Config.duckSeconds));
            foreach (KeyValuePair<MusicCue, Voice> pair in m_Voices)
            {
                Voice v = pair.Value;
                if (v.Target > 0f && !v.Source.isPlaying)
                {
                    // Waiting for its clip: the fade begins when the music does.
                    Begin(v);
                    if (!v.Source.isPlaying) continue;
                }
                float level = Mathf.MoveTowards(v.Level, v.Target, step);
                v.Level = level;
                v.Source.volume = Mathf.Min(1f, level * m_Config.volume * m_Config.Level(v.Cue) * m_Duck);
                if (v.Target > 0f || level > 0f || !v.Source.isPlaying) continue;
                if (v.PauseAtSilence) v.Source.Pause();
                else
                {
                    v.Source.Stop();
                    v.Source.time = 0f;
                    if (v.Source.clip != null && v.Source.clip.loadState == AudioDataLoadState.Loaded) v.Source.clip.UnloadAudioData();
                }
            }
        }
    }
}
