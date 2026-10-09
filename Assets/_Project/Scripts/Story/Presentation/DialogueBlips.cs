using UnityEngine;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>When a dialogue blip sounds (4i-C; pure, EditMode-tested).</summary>
    public static class BlipRules
    {
        /// <summary>A blip on every <paramref name="every"/>th letter written, never closer than <paramref name="minGap"/> seconds.</summary>
        public static bool Due(int printed, int every, float sinceLast, float minGap) =>
            printed > 0 && every > 0 && printed % every == 1 % every && sinceLast >= minGap;
    }

    /// <summary>
    /// The little sounds of a line being written out (4i-C): a blip every few letters, in the speaker's register (the low file for
    /// the deep voices, <c>CharacterDefinition.lowVoice</c>) at their own pitch (<c>voicePitch</c>, a whole number of semitones so
    /// the pitched synth blips stay in tune; round 2, 2026-10-09: no random variation). Silent for a look and the narration.
    /// Hooked to the dialogue box's text as it prints; through the mixer's Effects group.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class DialogueBlips : MonoBehaviour
    {
        [SerializeField] AudioClip[] m_Clips;
        [SerializeField, Tooltip("The low register (the deep voices).")] AudioClip[] m_LowClips;
        [SerializeField, Min(1)] int m_Every = 3;
        [SerializeField, Min(0f), Tooltip("0.079 s: Leohpaz's advice for these synth blips.")] float m_MinGap = 0.079f;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.3f;
        [SerializeField, Range(0f, 1f)] float m_LowVolume = 0.3f;
        [SerializeField, Range(0f, 0.3f), Tooltip("Random pitch per blip. 0: pitched blips stay in tune (round 2).")] float m_Jitter;

        AudioSource m_Source;
        SuperTextMesh m_Text;
        float m_Pitch = 1f, m_Last = -10f;
        bool m_Silent, m_Low;
        int m_Printed;

        public int Blips { get; private set; }

        /// <summary>How loud a blip plays (set by the balance pass from the clips' measured loudness).</summary>
        public float Volume
        {
            get => m_Volume;
            set => m_Volume = Mathf.Clamp01(value);
        }

        public void Configure(AudioClip[] clips) => m_Clips = clips;

        /// <summary>Round 2: both registers, the gap and no jitter.</summary>
        public void Configure(AudioClip[] high, AudioClip[] low)
        {
            m_Clips = high;
            m_LowClips = low;
            m_MinGap = 0.079f;
            m_Jitter = 0f;
        }

        public AudioClip[] HighClips => m_Clips;
        public AudioClip[] LowClips => m_LowClips;

        public float LowVolume
        {
            get => m_LowVolume;
            set => m_LowVolume = Mathf.Clamp01(value);
        }

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.spatialBlend = 0f;
            if (m_Source.outputAudioMixerGroup == null) m_Source.outputAudioMixerGroup = Hearthdelve.Shared.Audio.AudioMixerHub.EffectsGroup;
        }

        /// <summary>Listens to <paramref name="text"/> as it prints.</summary>
        public void Attach(SuperTextMesh text)
        {
            if (m_Text != null) m_Text.OnPrintEvent -= OnPrinted;
            m_Text = text;
            if (m_Text != null) m_Text.OnPrintEvent += OnPrinted;
        }

        void OnDestroy()
        {
            if (m_Text != null) m_Text.OnPrintEvent -= OnPrinted;
        }

        /// <summary>A new line begins: whose voice, or none.</summary>
        public void NewLine(float pitch, bool silent) => NewLine(pitch, false, silent);

        /// <summary>A new line begins: whose voice (pitch and register), or none.</summary>
        public void NewLine(float pitch, bool low, bool silent)
        {
            m_Pitch = pitch <= 0f ? 1f : pitch;
            m_Low = low;
            m_Silent = silent;
            m_Printed = 0;
        }

        void OnPrinted()
        {
            m_Printed++;
            AudioClip[] clips = m_Low && m_LowClips != null && m_LowClips.Length > 0 ? m_LowClips : m_Clips;
            if (m_Silent || clips == null || clips.Length == 0 || m_Source == null) return;
            if (!BlipRules.Due(m_Printed, m_Every, Time.unscaledTime - m_Last, m_MinGap)) return;
            m_Last = Time.unscaledTime;
            m_Source.pitch = m_Pitch * (m_Jitter > 0f ? Random.Range(1f - m_Jitter, 1f + m_Jitter) : 1f);
            m_Source.PlayOneShot(clips[Random.Range(0, clips.Length)], clips == m_LowClips ? m_LowVolume : m_Volume);
            Blips++;
        }
    }
}
