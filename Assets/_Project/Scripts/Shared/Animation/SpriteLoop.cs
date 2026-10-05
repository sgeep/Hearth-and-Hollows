using Hearthdelve.Core.Animation;
using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    /// <summary>Loops a few frames on a SpriteRenderer (torches, candles and other animated props).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteLoop : MonoBehaviour
    {
        [SerializeField] Sprite[] m_Frames;
        [SerializeField, Min(0.01f)] float m_FrameDuration = 0.2f;

        SpriteRenderer m_Renderer;
        float m_Time;

        public System.Collections.Generic.IReadOnlyList<Sprite> Frames => m_Frames;
        public float FrameDuration => m_FrameDuration;

        public void Configure(Sprite[] frames, float frameDuration)
        {
            m_Frames = frames;
            m_FrameDuration = frameDuration;
        }

        void Awake() => m_Renderer = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (m_Frames == null || m_Frames.Length == 0) return;
            m_Renderer.sprite = m_Frames[SpriteAnimationMath.FrameAt(m_Time, m_Frames.Length, m_FrameDuration, true)];
            m_Time += Time.deltaTime;
        }
    }
}
