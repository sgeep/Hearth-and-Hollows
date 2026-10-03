using Hearthdelve.Core.Haptics;
using Hearthdelve.Core.Services;
using UnityEngine;

namespace Hearthdelve.Shared.Haptics
{
    /// <summary>
    /// Plays named haptic patterns (GDD §9A, §10.4). Gameplay calls the static methods; they
    /// do nothing when no service is in the scene or rumble isn't supported, so callers never
    /// need to check. Strengths are mixed each frame in unscaled time (a hit-stop doesn't
    /// freeze the rumble) and always pass through the player's vibration settings.
    /// </summary>
    public sealed class HapticService : MonoBehaviour
    {
        static HapticService s_Instance;

        [SerializeField] HapticLibrary m_Library;

        readonly HapticMixer m_Mixer = new();
        IHapticOutput m_Output;
        bool m_Rumbling;

        public static HapticService Instance => s_Instance;

        /// <summary>Where the mixed result goes. Tests replace it with a recorder.</summary>
        public IHapticOutput Output
        {
            get => m_Output ??= CreateDefaultOutput();
            set
            {
                m_Output?.Stop();
                m_Output = value;
                m_Rumbling = false;
            }
        }

        public HapticLibrary Library
        {
            get => m_Library;
            set => m_Library = value;
        }

        /// <summary>The strengths last sent (after settings), for debug overlays and tests.</summary>
        public HapticSample LastSample { get; private set; }

        public static void Play(string patternId, float scale = 1f)
        {
            if (s_Instance == null || s_Instance.m_Library == null) return;
            Play(s_Instance.m_Library.Find(patternId), scale);
        }

        /// <summary>Raised with a pattern's id whenever one is played (debug overlays and tests).</summary>
        public static event System.Action<string> PatternPlayed;

        public static void Play(HapticPattern pattern, float scale = 1f)
        {
            if (s_Instance == null || pattern == null) return;
            s_Instance.m_Mixer.Play(pattern.keys, scale);
            PatternPlayed?.Invoke(pattern.id);
        }

        /// <summary>Sets a continuous rumble level (0–1 per motor) that holds until set again.</summary>
        public static void SetContinuous(string channel, float low, float high)
        {
            if (s_Instance != null) s_Instance.m_Mixer.SetContinuous(channel, low, high);
        }

        public static void StopAll()
        {
            if (s_Instance != null) s_Instance.Silence();
        }

        static IHapticOutput CreateDefaultOutput()
        {
            // Web builds have no rumble: the Input System exposes no gamepad haptics there.
            if (Application.platform == RuntimePlatform.WebGLPlayer) return new NullHapticOutput();
            return new NiceVibrationsHapticOutput();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Instance = null;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }

        void Update()
        {
            HapticSample sample = m_Mixer.Tick(Time.unscaledDeltaTime, GameSettings.Haptics);
            LastSample = sample;

            IHapticOutput output = Output;
            if (!output.IsAvailable)
            {
                m_Rumbling = false;
                return;
            }

            if (sample.IsSilent)
            {
                if (m_Rumbling) output.Stop();
                m_Rumbling = false;
            }
            else
            {
                output.SetMotors(sample.Low, sample.High);
                m_Rumbling = true;
            }
        }

        void Silence()
        {
            m_Mixer.StopAll();
            LastSample = HapticSample.Silent;
            if (m_Rumbling) m_Output?.Stop();
            m_Rumbling = false;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Silence();
        }

        void OnDisable() => Silence();

        void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }
    }
}
