using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The room's ambient light follows the day: cool daylight in the morning, the warm evening light for
    /// prep and service, dim and blue at night. The fires and lamps keep their own glow. Presentation only;
    /// tune the moods here.
    /// </summary>
    public sealed class TavernMood : MonoBehaviour
    {
        [Serializable]
        public struct Mood
        {
            public Color colour;
            [Min(0f)] public float intensity;
        }

        [SerializeField] Light2D m_Ambient;
        [SerializeField] Mood m_Morning = new() { colour = new Color(0.93f, 0.94f, 0.9f), intensity = 0.95f };
        [SerializeField] Mood m_Evening = new() { colour = new Color(1f, 0.86f, 0.68f), intensity = 0.72f };
        [SerializeField] Mood m_Night = new() { colour = new Color(0.55f, 0.6f, 0.9f), intensity = 0.42f };
        [SerializeField, Min(0f), Tooltip("Seconds to change from one mood to the next.")] float m_BlendSeconds = 1.5f;

        TavernDirector m_Director;
        Mood m_From, m_To;
        float m_Blend = 1f;

        public void Configure(Light2D ambient, Color eveningColour, float eveningIntensity)
        {
            m_Ambient = ambient;
            m_Evening = new Mood { colour = eveningColour, intensity = eveningIntensity };
        }

        public Mood Current => m_To;

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director == null || m_Ambient == null) return;
            m_To = MoodFor(m_Director.Phase);
            Apply(m_To);
            m_Director.PhaseChanged += OnPhase;
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= OnPhase;
        }

        void OnPhase()
        {
            Mood next = MoodFor(m_Director.Phase);
            if (next.colour == m_To.colour && Mathf.Approximately(next.intensity, m_To.intensity)) return;
            m_From = new Mood { colour = m_Ambient.color, intensity = m_Ambient.intensity };
            m_To = next;
            m_Blend = m_BlendSeconds > 0f ? 0f : 1f;
            if (m_Blend >= 1f) Apply(m_To);
        }

        void Update()
        {
            if (m_Blend >= 1f || m_Ambient == null) return;
            m_Blend = Mathf.Min(1f, m_Blend + Time.unscaledDeltaTime / m_BlendSeconds);
            float t = Mathf.SmoothStep(0f, 1f, m_Blend);
            Apply(new Mood { colour = Color.Lerp(m_From.colour, m_To.colour, t), intensity = Mathf.Lerp(m_From.intensity, m_To.intensity, t) });
        }

        Mood MoodFor(TavernPhase phase) => phase switch
        {
            TavernPhase.Daytime => m_Morning,
            TavernPhase.Night => m_Night,
            _ => m_Evening,
        };

        void Apply(Mood mood)
        {
            m_Ambient.color = mood.colour;
            m_Ambient.intensity = mood.intensity;
        }
    }
}
