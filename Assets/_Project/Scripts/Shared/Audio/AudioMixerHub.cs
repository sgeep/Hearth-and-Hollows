using Hearthdelve.Shared.Settings;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>
    /// The one audio mixer, in Boot (4i-B): Master, Music and Effects, each a volume the player sets in Options
    /// (<see cref="GameOptions"/>), as mixer parameters in decibels. The Hollows' quarter-down on effects (2026-10-07) is a mixer
    /// parameter too now (folded into Effects while the delve runs), so <c>AudioListener.volume</c> is never used.
    /// <para>
    /// Every sound is routed to Music or Effects by the generators. As a safety net, a scene that loads with a sound still unrouted
    /// has it sent to Effects, so nothing ever slips past the player's sliders.
    /// </para>
    /// </summary>
    public sealed class AudioMixerHub : MonoBehaviour
    {
        public const string MasterVolume = "MasterVolume", MusicVolume = "MusicVolume", EffectsVolume = "EffectsVolume";

        [SerializeField] AudioMixer m_Mixer;
        [SerializeField] AudioMixerGroup m_Music;
        [SerializeField] AudioMixerGroup m_Effects;

        float m_HollowsLevel = 1f;

        public static AudioMixerHub Instance { get; private set; }
        public static AudioMixerGroup MusicGroup => Instance != null ? Instance.m_Music : null;
        public static AudioMixerGroup EffectsGroup => Instance != null ? Instance.m_Effects : null;
        public AudioMixer Mixer => m_Mixer;

        public void Configure(AudioMixer mixer, AudioMixerGroup music, AudioMixerGroup effects)
        {
            m_Mixer = mixer;
            m_Music = music;
            m_Effects = effects;
        }

        /// <summary>
        /// Effects' level under the player's slider: the game's own level for effects (a quarter down since the 4i-C playtest),
        /// and a quarter down again in the Hollows. Set by the music director.
        /// </summary>
        public float HollowsLevel
        {
            get => m_HollowsLevel;
            set
            {
                if (Mathf.Approximately(m_HollowsLevel, value)) return;
                m_HollowsLevel = value;
                Apply();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            // The old way of lowering effects is gone for good: the listener stays at full.
            AudioListener.volume = 1f;
        }

        void OnEnable()
        {
            GameOptions.Changed += Apply;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            GameOptions.Changed -= Apply;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start() => Apply();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The player's volumes (and the Hollows' level) onto the mixer.</summary>
        public void Apply()
        {
            if (m_Mixer == null) return;
            PlayerOptions o = GameOptions.Current;
            m_Mixer.SetFloat(MasterVolume, OptionsRules.ToDecibels(o.masterVolume));
            m_Mixer.SetFloat(MusicVolume, OptionsRules.ToDecibels(o.musicVolume));
            m_Mixer.SetFloat(EffectsVolume, OptionsRules.ToDecibels(o.effectsVolume * m_HollowsLevel));
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Route(scene);

        /// <summary>Anything in the scene that would play outside the mixer goes to Effects (music sources are routed by the director).</summary>
        public int Route(Scene scene)
        {
            if (m_Effects == null || !scene.IsValid()) return 0;
            int routed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
                {
                    if (player.FeedbacksList == null) continue;
                    foreach (MMF_Feedback feedback in player.FeedbacksList)
                        if (feedback is MMF_Sound sound && sound.SfxAudioMixerGroup == null)
                        {
                            sound.SfxAudioMixerGroup = m_Effects;
                            routed++;
                        }
                }
                foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                    if (source.outputAudioMixerGroup == null && source.gameObject != gameObject)
                    {
                        source.outputAudioMixerGroup = m_Effects;
                        routed++;
                    }
            }
            if (routed > 0 && Debug.isDebugBuild) Debug.Log($"[Hearthdelve] {routed} unrouted sound(s) in {scene.name} sent to Effects.");
            return routed;
        }
    }
}
