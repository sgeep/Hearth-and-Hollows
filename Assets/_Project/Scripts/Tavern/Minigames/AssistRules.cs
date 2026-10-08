using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Relaxed cooking timing (4i-B, an accessibility option, off by default): the keeper's own minigames with wider targets
    /// (bands about their centres, perfect distances, falloffs) and a gentler pace (slower meters and knives, longer time limits).
    /// Scores are scored exactly as usual: there is no penalty, and staff never use it. Pure; the scales are
    /// <see cref="Hearthdelve.Shared.Settings.OptionsRules"/>'s.
    /// </summary>
    public static class AssistRules
    {
        /// <summary>A band widened by <paramref name="scale"/> about its centre, kept inside 0–1.</summary>
        public static (float min, float max) Widen(float min, float max, float scale)
        {
            float centre = (min + max) * 0.5f, half = (max - min) * 0.5f * scale;
            return (Mathf.Max(0f, centre - half), Mathf.Min(1f, centre + half));
        }

        public static GrillSettings Relaxed(GrillSettings s, float bandScale, float pace)
        {
            (s.bandMin, s.bandMax) = Widen(s.bandMin, s.bandMax, bandScale);
            s.undercookFalloff *= bandScale;
            s.cookRate *= pace;
            return s;
        }

        public static TapSettings Relaxed(TapSettings s, float bandScale, float pace)
        {
            s.fillTolerance *= bandScale;
            s.fillFalloff *= bandScale;
            (s.foamBandMin, s.foamBandMax) = Widen(s.foamBandMin, s.foamBandMax, bandScale);
            s.foamFalloff *= bandScale;
            s.pourRate *= pace;
            s.timeout /= pace;
            return s;
        }

        public static ChopSettings Relaxed(ChopSettings s, float bandScale, float pace)
        {
            s.perfectDistance *= bandScale;
            s.falloff *= bandScale;
            s.knifeSpeed *= pace;
            s.itemTimeLimit /= pace;
            return s;
        }

        public static ButcherSettings Relaxed(ButcherSettings s, float bandScale, float pace)
        {
            s.perfectDistance *= bandScale;
            s.falloff *= bandScale;
            s.knifeSpeed *= pace;
            s.timeLimit /= pace;
            return s;
        }

        /// <summary>The same factory with every station relaxed (serving is untouched: it has no timing to relax).</summary>
        public static MinigameFactory Relaxed(MinigameFactory f, float bandScale, float pace) => new(
            Relaxed(f.Grill, bandScale, pace), Relaxed(f.Tap, bandScale, pace), f.Serving,
            Relaxed(f.Chop, bandScale, pace), Relaxed(f.Butcher, bandScale, pace));
    }
}
