using System.Collections.Generic;

namespace Hearthdelve.Core.Haptics
{
    /// <summary>Where mixed haptics go: a controller, or nothing where rumble isn't supported.</summary>
    public interface IHapticOutput
    {
        /// <summary>False when there is nothing to rumble (no controller, web build).</summary>
        bool IsAvailable { get; }
        void SetMotors(float low, float high);
        void Stop();
    }

    /// <summary>Used wherever rumble is unsupported: haptics do nothing and nothing else changes.</summary>
    public sealed class NullHapticOutput : IHapticOutput
    {
        public bool IsAvailable => false;
        public void SetMotors(float low, float high) { }
        public void Stop() { }
    }

    /// <summary>
    /// Mixes playing one-shot patterns with continuous channels (pour speed, grill heat) into
    /// one pair of motor strengths, then applies the player's settings. A controller has one
    /// output, so layers take the strongest value rather than adding up. Pure logic.
    /// </summary>
    public sealed class HapticMixer
    {
        struct Voice
        {
            public HapticKey[] Keys;
            public float Elapsed;
            public float Scale;
        }

        readonly List<Voice> m_Voices = new();
        readonly Dictionary<string, HapticSample> m_Continuous = new();

        public int ActiveVoices => m_Voices.Count;
        public bool IsIdle => m_Voices.Count == 0 && m_Continuous.Count == 0;

        /// <summary>Starts a one-shot. <paramref name="scale"/> multiplies the authored strengths (0–1).</summary>
        public void Play(HapticKey[] keys, float scale = 1f)
        {
            if (keys == null || keys.Length == 0 || scale <= 0f) return;
            m_Voices.Add(new Voice { Keys = keys, Elapsed = 0f, Scale = HapticMath.Clamp01(scale) });
        }

        /// <summary>Sets a continuous channel's level; it holds until set again. Zero removes it.</summary>
        public void SetContinuous(string channel, float low, float high)
        {
            if (string.IsNullOrEmpty(channel)) return;
            low = HapticMath.Clamp01(low);
            high = HapticMath.Clamp01(high);
            if (low <= 0f && high <= 0f) m_Continuous.Remove(channel);
            else m_Continuous[channel] = new HapticSample(low, high);
        }

        public void StopAll()
        {
            m_Voices.Clear();
            m_Continuous.Clear();
        }

        /// <summary>Advances time and returns the strengths to send to the controller.</summary>
        public HapticSample Tick(float deltaTime, HapticSettings settings)
        {
            float low = 0f, high = 0f;
            foreach (var level in m_Continuous.Values)
            {
                low = HapticMath.Layer(low, level.Low);
                high = HapticMath.Layer(high, level.High);
            }

            for (int i = m_Voices.Count - 1; i >= 0; i--)
            {
                Voice voice = m_Voices[i];
                HapticSample sample = HapticCurve.Sample(voice.Keys, voice.Elapsed);
                low = HapticMath.Layer(low, sample.Low * voice.Scale);
                high = HapticMath.Layer(high, sample.High * voice.Scale);

                voice.Elapsed += deltaTime > 0f ? deltaTime : 0f;
                if (voice.Elapsed >= HapticCurve.Duration(voice.Keys)) m_Voices.RemoveAt(i);
                else m_Voices[i] = voice;
            }

            return new HapticSample(HapticMath.ApplySettings(low, settings), HapticMath.ApplySettings(high, settings));
        }
    }
}
