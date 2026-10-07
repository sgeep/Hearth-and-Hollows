using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Village
{
    /// <summary>
    /// The village's daylight (4h): its global light follows the surface clock: soft morning, plain noon, gold toward five,
    /// and the dusk held once the village winds down. Read from the one clock, never from <c>Time.time</c>. The darkest it
    /// gets stays readable (CLAUDE.md, lighting never costs readability).
    /// </summary>
    public sealed class SurfaceDaylight : MonoBehaviour
    {
        [SerializeField] Light2D m_Light;
        [SerializeField, Tooltip("Colour over the day: 0 = the day's start, 1 = the cutoff (five).")]
        Gradient m_Colour = DefaultColour();
        [SerializeField, Tooltip("Intensity over the day: 0 = the day's start, 1 = the cutoff.")]
        AnimationCurve m_Intensity = new(new Keyframe(0f, 0.9f), new Keyframe(0.45f, 1f), new Keyframe(0.85f, 0.95f), new Keyframe(1f, 0.74f));
        [SerializeField, Min(0f), Tooltip("Seconds a change of light eases over (a step of the clock, or arriving).")]
        float m_EaseSeconds = 1.5f;

        float m_Target = -1f;
        float m_Shown = -1f;

        public Light2D Light => m_Light;
        /// <summary>Where on the day's curve the light is aimed (0 morning, 1 five).</summary>
        public float Target => m_Target;

        public void Configure(Light2D light) => m_Light = light;

        static Gradient DefaultColour()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.88f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 0.98f), 0.4f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.72f), 0.85f),
                    new GradientColorKey(new Color(0.95f, 0.72f, 0.62f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        /// <summary>0 at the day's start, 1 at the cutoff and after.</summary>
        public static float DayFraction(int minute, in SurfaceClockSettings settings)
        {
            int span = Mathf.Max(1, settings.cutoffMinute - settings.dayStartMinute);
            return Mathf.Clamp01((minute - settings.dayStartMinute) / (float)span);
        }

        void OnEnable()
        {
            EventBus<SurfaceTimeChanged>.Subscribe(OnTime);
            m_Target = DayFraction(SurfaceTime.ShownMinute, SurfaceTime.CurrentSettings);
            m_Shown = m_Target;
            Apply(m_Shown);
        }

        void OnDisable() => EventBus<SurfaceTimeChanged>.Unsubscribe(OnTime);

        void OnTime(SurfaceTimeChanged e) => m_Target = DayFraction(e.Minute, SurfaceTime.CurrentSettings);

        void Update()
        {
            if (Mathf.Approximately(m_Shown, m_Target)) return;
            float rate = m_EaseSeconds > 0f ? Time.unscaledDeltaTime / m_EaseSeconds : 1f;
            m_Shown = Mathf.MoveTowards(m_Shown, m_Target, Mathf.Max(rate, 0.0001f));
            Apply(m_Shown);
        }

        void Apply(float t)
        {
            if (m_Light == null) return;
            m_Light.color = m_Colour.Evaluate(t);
            m_Light.intensity = m_Intensity.Evaluate(t);
        }
    }
}
