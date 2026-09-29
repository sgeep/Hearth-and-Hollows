using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Tap tuning. Target: 5–10 seconds per drink at default tuning (≈5.5 s: ~5.3 s pouring to
    /// the line plus reaction time).
    /// </summary>
    [Serializable]
    public struct TapSettings
    {
        [Min(0.01f), Tooltip("Glass fraction filled per second while pouring (liquid + foam).")]
        public float pourRate;
        [Range(0, 1), Tooltip("The fill line, as a fraction of the glass.")]
        public float fillLine;
        [Range(0, 0.5f), Tooltip("Within this distance of the line scores full marks.")]
        public float fillTolerance;
        [Min(0.01f), Tooltip("Distance beyond the tolerance at which the fill score reaches zero.")]
        public float fillFalloff;

        [Header("Foam")]
        [Range(0, 1), Tooltip("Share of the pour that turns to foam with the glass fully tilted (Aim down).")]
        public float foamShareTilted;
        [Range(0, 1), Tooltip("Share of the pour that turns to foam with the glass upright (Aim up).")]
        public float foamShareUpright;
        [Range(0, 1)] public float foamBandMin;
        [Range(0, 1)] public float foamBandMax;
        [Min(0.01f)] public float foamFalloff;
        [Min(0.01f), Tooltip("How quickly the glass tilts toward the stick position.")]
        public float tiltSpeed;
        [Min(1), Tooltip("Gives up (scores 0) if nothing is poured by then.")]
        public float timeout;

        [Header("Staff auto-play")]
        [Range(0, 1), Tooltip("Largest tilt error for a skill-0 staff member; scales down with skill.")]
        public float autoMaxTiltError;
        [Range(0, 0.5f), Tooltip("Largest fill-line error for a skill-0 staff member; scales down with skill.")]
        public float autoMaxLineError;

        public static TapSettings Default => new()
        {
            pourRate = 0.16f,
            fillLine = 0.85f,
            fillTolerance = 0.03f,
            fillFalloff = 0.2f,
            foamShareTilted = 0.05f,
            foamShareUpright = 0.45f,
            foamBandMin = 0.12f,
            foamBandMax = 0.22f,
            foamFalloff = 0.15f,
            tiltSpeed = 3f,
            timeout = 12f,
            autoMaxTiltError = 0.5f,
            autoMaxLineError = 0.15f,
        };

        public float FoamBandCenter => (foamBandMin + foamBandMax) * 0.5f;
    }

    /// <summary>
    /// Tap: hold Action to pour, release at the line. Tilting the glass (Aim up/down) changes
    /// how much of the pour becomes foam; the head should land in the foam band. Overflowing
    /// scores zero. Score = fill accuracy × foam accuracy.
    /// </summary>
    public sealed class TapMinigame : IMinigame
    {
        readonly TapSettings m_Settings;
        bool m_Started;

        public TapMinigame(TapSettings settings) => m_Settings = settings;

        public TapSettings Settings => m_Settings;
        public float Liquid { get; private set; }
        public float Foam { get; private set; }
        public float Total => Liquid + Foam;
        /// <summary>0 = fully tilted (little foam), 1 = upright (lots of foam).</summary>
        public float Tilt { get; private set; } = 0.5f;
        public bool IsPouring { get; private set; }
        public bool Overflowed { get; private set; }
        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }
        public float FoamFraction => Total > 0f ? Foam / Total : 0f;

        public void Begin()
        {
            Liquid = Foam = 0f;
            Tilt = 0.5f;
            Elapsed = 0f;
            m_Started = IsPouring = Overflowed = IsComplete = false;
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;

            float tiltTarget = Mathf.Clamp01(0.5f + 0.5f * input.Aim.y);
            Tilt = Mathf.MoveTowards(Tilt, tiltTarget, m_Settings.tiltSpeed * deltaTime);

            IsPouring = input.ActionHeld || input.ActionPressed;
            if (IsPouring)
            {
                m_Started = true;
                float poured = m_Settings.pourRate * deltaTime;
                float foamShare = FoamShareAt(Tilt, m_Settings);
                Foam += poured * foamShare;
                Liquid += poured * (1f - foamShare);
                if (Total >= 1f)
                {
                    Overflowed = true;
                    IsComplete = true;
                    return;
                }
            }

            if (m_Started && !IsPouring) IsComplete = true;
            else if (!m_Started && Elapsed >= m_Settings.timeout) IsComplete = true;
        }

        public float Evaluate()
        {
            if (!IsComplete || Overflowed || Total <= 0f) return 0f;
            return FillScore(Total, m_Settings) * FoamScore(FoamFraction, m_Settings);
        }

        public static float FoamShareAt(float tilt, in TapSettings s) => Mathf.Lerp(s.foamShareTilted, s.foamShareUpright, Mathf.Clamp01(tilt));

        public static float FillScore(float total, in TapSettings s)
        {
            float off = Mathf.Abs(total - s.fillLine) - s.fillTolerance;
            return off <= 0f ? 1f : Mathf.Clamp01(1f - off / s.fillFalloff);
        }

        public static float FoamScore(float foamFraction, in TapSettings s)
        {
            if (foamFraction >= s.foamBandMin && foamFraction <= s.foamBandMax) return 1f;
            float off = foamFraction < s.foamBandMin ? s.foamBandMin - foamFraction : foamFraction - s.foamBandMax;
            return Mathf.Clamp01(1f - off / s.foamFalloff);
        }

        /// <summary>The tilt that makes the foam land in the middle of the band.</summary>
        public static float IdealTilt(in TapSettings s) =>
            Mathf.Clamp01(Mathf.InverseLerp(s.foamShareTilted, s.foamShareUpright, s.FoamBandCenter));
    }

    /// <summary>Pours with an imperfect tilt and releases near the line; errors grow as skill drops.</summary>
    public sealed class TapAutoPlayer : IMinigameAutoPlayer
    {
        readonly TapMinigame m_Game;
        readonly float m_TiltTarget;
        readonly float m_ReleaseAt;

        public TapAutoPlayer(TapMinigame game, float skill, IRandom random)
        {
            m_Game = game;
            float miss = 1f - Mathf.Clamp01(skill);
            var s = game.Settings;
            m_TiltTarget = Mathf.Clamp01(TapMinigame.IdealTilt(s) + (random.Value() * 2f - 1f) * miss * s.autoMaxTiltError);
            m_ReleaseAt = Mathf.Min(0.99f, s.fillLine + (random.Value() * 2f - 1f) * miss * s.autoMaxLineError);
        }

        public MinigameInput NextInput(float deltaTime)
        {
            var input = new MinigameInput { Aim = new Vector2(0f, m_TiltTarget * 2f - 1f) };
            // Settle the tilt before pouring, then pour until the release point.
            bool tilted = Mathf.Abs(m_Game.Tilt - m_TiltTarget) < 0.02f;
            float nextTotal = m_Game.Total + m_Game.Settings.pourRate * deltaTime;
            input.ActionHeld = tilted && nextTotal < m_ReleaseAt || (m_Game.IsPouring && nextTotal < m_ReleaseAt);
            return input;
        }
    }
}
