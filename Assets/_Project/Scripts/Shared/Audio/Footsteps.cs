using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>
    /// The keeper's footsteps (4i-C): a step on each of the walk's two footfalls (its first frame and its middle one), so they keep
    /// time with the feet. The ground decides the sound: the Hollows' stone, Tally Ho!'s boards indoors, Kariaston's paths outside.
    /// Presentation only (it reads the animator); through the mixer's Effects group like every effect.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Footsteps : MonoBehaviour
    {
        [SerializeField] CharacterSpriteAnimator m_Animator;
        [SerializeField, Tooltip("This keeper is in the Hollows (the delve's prefab): always stone.")]
        bool m_Hollows;
        [SerializeField] AudioClip[] m_Village;
        [SerializeField] AudioClip[] m_Tavern;
        [SerializeField] AudioClip[] m_Stone;
        [SerializeField, Range(0f, 1f), Tooltip("Kariaston's paths.")] float m_Volume = 0.35f;
        [SerializeField, Range(0f, 1f), Tooltip("Tally Ho!'s boards.")] float m_TavernVolume = 0.35f;
        [SerializeField, Range(0f, 1f), Tooltip("The Hollows' stone.")] float m_StoneVolume = 0.35f;
        [SerializeField] Vector2 m_Pitch = new(0.94f, 1.06f);

        AudioSource m_Source;
        int m_LastFrame = -1;
        int m_LastClip = -1;

        public int Steps { get; private set; }

        /// <summary>How loud a step plays (set by the balance pass from the clips' measured loudness).</summary>
        public float Volume
        {
            get => m_Volume;
            set => m_Volume = Mathf.Clamp01(value);
        }

        public float TavernVolume
        {
            get => m_TavernVolume;
            set => m_TavernVolume = Mathf.Clamp01(value);
        }

        public float StoneVolume
        {
            get => m_StoneVolume;
            set => m_StoneVolume = Mathf.Clamp01(value);
        }

        public void Configure(CharacterSpriteAnimator animator, bool hollows, AudioClip[] village, AudioClip[] tavern, AudioClip[] stone)
        {
            m_Animator = animator;
            m_Hollows = hollows;
            m_Village = village;
            m_Tavern = tavern;
            m_Stone = stone;
        }

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.spatialBlend = 0f;
            if (m_Source.outputAudioMixerGroup == null) m_Source.outputAudioMixerGroup = AudioMixerHub.EffectsGroup;
        }

        void LateUpdate()
        {
            if (m_Animator == null || m_Animator.Current != CharacterAnim.Walk || m_Animator.ShownFrames < 2)
            {
                m_LastFrame = -1;
                return;
            }
            int frame = m_Animator.ShownFrame;
            if (frame == m_LastFrame) return;
            m_LastFrame = frame;
            if (frame == 0 || frame == m_Animator.ShownFrames / 2) Step();
        }

        AudioClip[] Ground => m_Hollows ? m_Stone : SurfaceTime.Indoors ? m_Tavern : m_Village;

        void Step()
        {
            AudioClip[] clips = Ground;
            if (clips == null || clips.Length == 0 || Time.timeScale <= 0f) return;
            // Never the same clip twice running: repeated steps are where sameness shows.
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == m_LastClip) i = (i + 1) % clips.Length;
            m_LastClip = i;
            m_Source.pitch = Random.Range(m_Pitch.x, m_Pitch.y);
            m_Source.PlayOneShot(clips[i], m_Hollows ? m_StoneVolume : SurfaceTime.Indoors ? m_TavernVolume : m_Volume);
            Steps++;
        }
    }
}
