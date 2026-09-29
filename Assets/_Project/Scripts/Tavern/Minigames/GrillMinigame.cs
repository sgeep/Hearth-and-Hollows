using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Grill tuning. Target: 5–10 seconds per dish at default tuning (≈6.5 s for good play:
    /// two sides × ~3 s to reach the band, plus the flip pause).
    /// </summary>
    [Serializable]
    public struct GrillSettings
    {
        [Min(0.01f), Tooltip("Doneness gained per second (0 → 1 is raw → burnt).")]
        public float cookRate;
        [Range(0, 1)] public float bandMin;
        [Range(0, 1)] public float bandMax;
        [Min(0.01f), Tooltip("How far below the band an early flip can be before it scores zero.")]
        public float undercookFalloff;
        [Min(0), Tooltip("Pause between sides.")]
        public float flipPause;
        [Min(1)] public int sides;

        public static GrillSettings Default => new()
        {
            cookRate = 0.22f,
            bandMin = 0.55f,
            bandMax = 0.75f,
            undercookFalloff = 0.4f,
            flipPause = 0.4f,
            sides = 2,
        };

        public float BandCenter => (bandMin + bandMax) * 0.5f;
    }

    /// <summary>
    /// Grill: a doneness meter rises; press Action to flip while it's in the target band.
    /// Early flips are undercooked, late flips overcooked; reaching 1 burns the side (0).
    /// Final score is the average of both sides.
    /// </summary>
    public sealed class GrillMinigame : IMinigame
    {
        public const float BurnAt = 1f;

        readonly GrillSettings m_Settings;
        readonly float[] m_SideScores;
        float m_PauseRemaining;

        public GrillMinigame(GrillSettings settings)
        {
            m_Settings = settings;
            m_SideScores = new float[Math.Max(1, settings.sides)];
        }

        public GrillSettings Settings => m_Settings;
        public float Meter { get; private set; }
        public int Side { get; private set; }
        public int SideCount => m_SideScores.Length;
        public bool IsPausing => m_PauseRemaining > 0f;
        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }
        /// <summary>Raised with (side, score) when a side is flipped or burns.</summary>
        public event Action<int, float> SideFinished;

        public float SideScore(int side) => m_SideScores[side];

        public void Begin()
        {
            Meter = 0f;
            Side = 0;
            Elapsed = 0f;
            m_PauseRemaining = 0f;
            IsComplete = false;
            Array.Clear(m_SideScores, 0, m_SideScores.Length);
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;

            if (m_PauseRemaining > 0f)
            {
                m_PauseRemaining -= deltaTime;
                if (m_PauseRemaining <= 0f) Meter = 0f;
                return;
            }

            Meter += m_Settings.cookRate * deltaTime;
            if (input.ActionPressed) FinishSide(ScoreFlip(Meter, m_Settings));
            else if (Meter >= BurnAt) FinishSide(0f);
        }

        void FinishSide(float score)
        {
            m_SideScores[Side] = score;
            SideFinished?.Invoke(Side, score);
            Side++;
            if (Side >= m_SideScores.Length)
            {
                IsComplete = true;
                return;
            }
            m_PauseRemaining = m_Settings.flipPause;
            if (m_PauseRemaining <= 0f) Meter = 0f;
        }

        public float Evaluate()
        {
            float sum = 0f;
            foreach (var s in m_SideScores) sum += s;
            return sum / m_SideScores.Length;
        }

        /// <summary>Score for flipping at doneness <paramref name="meter"/>.</summary>
        public static float ScoreFlip(float meter, in GrillSettings s)
        {
            if (meter >= BurnAt) return 0f;
            if (meter < s.bandMin) return Mathf.Clamp01(1f - (s.bandMin - meter) / s.undercookFalloff);
            if (meter > s.bandMax) return Mathf.Clamp01(1f - (meter - s.bandMax) / (BurnAt - s.bandMax));
            return 1f;
        }
    }

    /// <summary>Flips near the band centre, with timing error that grows as skill drops.</summary>
    public sealed class GrillAutoPlayer : IMinigameAutoPlayer
    {
        /// <summary>Largest meter error at skill 0.</summary>
        public const float MaxError = 0.6f;

        readonly GrillMinigame m_Game;
        readonly float m_Skill;
        readonly IRandom m_Random;
        int m_PlannedSide = -1;
        float m_Target;

        public GrillAutoPlayer(GrillMinigame game, float skill, IRandom random)
        {
            m_Game = game;
            m_Skill = Mathf.Clamp01(skill);
            m_Random = random;
        }

        public MinigameInput NextInput(float deltaTime)
        {
            if (m_Game.IsComplete || m_Game.IsPausing) return default;
            if (m_PlannedSide != m_Game.Side)
            {
                m_PlannedSide = m_Game.Side;
                float error = (m_Random.Value() * 2f - 1f) * (1f - m_Skill) * MaxError;
                m_Target = m_Game.Settings.BandCenter + error;
            }
            // Next tick's meter is what the flip will be scored at.
            float next = m_Game.Meter + m_Game.Settings.cookRate * deltaTime;
            return new MinigameInput { ActionPressed = next >= m_Target, ActionHeld = next >= m_Target };
        }
    }
}
