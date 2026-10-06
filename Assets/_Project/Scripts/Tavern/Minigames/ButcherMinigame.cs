using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Butcher Block tuning (4f Checkpoint C, D17). Target: 5–10 seconds a part (three strokes of about a second, with the
    /// knife lined up between them).
    /// </summary>
    [Serializable]
    public struct ButcherSettings
    {
        [Range(0, 0.45f), Tooltip("Cut lines stay at least this far (board fraction) from the board's sides.")]
        public float edgeMargin;
        [Range(0, 0.3f), Tooltip("Most a line leans: how far its foot is from its head, as a board fraction.")]
        public float slant;
        [Min(0.1f), Tooltip("Seconds a stroke takes from the top of the part to the bottom (held Action).")]
        public float strokeSeconds;
        [Min(0), Tooltip("Off the line by at most this much (board fraction), the knife cuts clean.")]
        public float perfectDistance;
        [Min(0.01f), Tooltip("Distance beyond perfect at which the cut is wasted.")]
        public float falloff;
        [Min(0.1f), Tooltip("Knife speed with the stick / keys (board widths per second).")]
        public float knifeSpeed;
        [Min(1), Tooltip("Seconds for the whole part; lines not cut by then are wasted.")]
        public float timeLimit;
        [Range(0, 1), Tooltip("A stroke scoring at least this is a clean cut (its own feedback); below, ragged.")]
        public float cleanStroke;

        [Header("Screen placement (the mouse maps onto this span)")]
        [Range(0, 1)] public float boardLeft;
        [Range(0.1f, 1)] public float boardWidth;

        [Header("Staff auto-play")]
        [Range(0, 0.5f), Tooltip("How far off the line a skill-0 cook steers; scales down with skill.")]
        public float autoMaxError;
        [Min(0), Tooltip("Hesitation before each stroke for a skill-0 cook; scales down with skill.")]
        public float autoMaxHesitation;

        public static ButcherSettings Default => new()
        {
            edgeMargin = 0.18f,
            slant = 0.16f,
            strokeSeconds = 1f,
            perfectDistance = 0.03f,
            falloff = 0.14f,
            knifeSpeed = 0.9f,
            timeLimit = 9f,
            cleanStroke = 0.8f,
            boardLeft = 0.3f,
            boardWidth = 0.4f,
            autoMaxError = 0.12f,
            autoMaxHesitation = 0.6f,
        };
    }

    /// <summary>
    /// The Butcher Block (D17): an enlarged part with dotted cut lines. Hold Action to draw the knife down from the top of
    /// the line nearest it; steer with the mouse or the stick so it follows the line, which leans. A stroke scores by how
    /// close the knife stayed along it; letting go early wastes the rest of that line. Lines not cut in time are wasted.
    /// Score = the average over the lines; the score sets how many cuts the part gives (<c>ButcherRules.Yield</c>).
    /// </summary>
    public sealed class ButcherMinigame : IMinigame
    {
        readonly ButcherSettings m_Settings;
        readonly float[] m_Top, m_Bottom, m_Scores;
        readonly bool[] m_Cut;

        float m_StrokeSum;
        int m_StrokeSamples;

        public ButcherMinigame(ButcherSettings settings, int lines, IRandom random)
        {
            m_Settings = settings;
            int count = Math.Max(1, lines);
            random ??= new SeededRandom();
            m_Top = new float[count];
            m_Bottom = new float[count];
            m_Scores = new float[count];
            m_Cut = new bool[count];
            float span = 1f - 2f * settings.edgeMargin;
            for (int i = 0; i < count; i++)
            {
                float middle = settings.edgeMargin + span * (i + 0.5f) / count;
                float lean = (random.Value() * 2f - 1f) * settings.slant;
                m_Top[i] = Mathf.Clamp01(middle - lean / 2f);
                m_Bottom[i] = Mathf.Clamp01(middle + lean / 2f);
            }
        }

        public ButcherSettings Settings => m_Settings;
        public int LineCount => m_Top.Length;
        /// <summary>Knife position, 0–1 across the board.</summary>
        public float Knife { get; private set; }
        /// <summary>The line being cut (−1 between strokes).</summary>
        public int Stroke { get; private set; } = -1;
        /// <summary>How far down the stroke is, 0 (top) to 1 (bottom).</summary>
        public float StrokeProgress { get; private set; }
        /// <summary>Is the knife within the clean distance of its line right now (feedback)?</summary>
        public bool OnTrack { get; private set; }
        public float TimeLeft { get; private set; }
        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }

        /// <summary>A stroke began, on this line.</summary>
        public event Action<int> StrokeStarted;
        /// <summary>A stroke ended: the line and its score.</summary>
        public event Action<int, float> StrokeFinished;

        /// <summary>Where line <paramref name="line"/> runs at depth <paramref name="t"/> (0 top, 1 bottom), board fraction.</summary>
        public float LineAt(int line, float t) => Mathf.Lerp(m_Top[line], m_Bottom[line], Mathf.Clamp01(t));
        public bool IsCut(int line) => m_Cut[line];
        public float LineScore(int line) => m_Scores[line];

        public void Begin()
        {
            Knife = 0.5f;
            Elapsed = 0f;
            IsComplete = false;
            Stroke = -1;
            StrokeProgress = 0f;
            TimeLeft = m_Settings.timeLimit;
            Array.Clear(m_Scores, 0, m_Scores.Length);
            Array.Clear(m_Cut, 0, m_Cut.Length);
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;
            TimeLeft -= deltaTime;
            Knife = input.PointerActive
                ? Mathf.Clamp01(input.Pointer)
                : Mathf.Clamp01(Knife + Mathf.Clamp(input.Aim.x, -1f, 1f) * m_Settings.knifeSpeed * deltaTime);

            if (Stroke < 0)
            {
                if ((input.ActionPressed || input.ActionHeld) && TryStartStroke()) Advance(deltaTime);
            }
            else if (!input.ActionHeld) EndStroke();
            else Advance(deltaTime);

            if (AllCut() || TimeLeft <= 0f)
            {
                if (Stroke >= 0) EndStroke();
                IsComplete = true;
            }
        }

        bool TryStartStroke()
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_Top.Length; i++)
            {
                if (m_Cut[i]) continue;
                float d = Mathf.Abs(m_Top[i] - Knife);
                if (d < bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }
            if (best < 0) return false;
            Stroke = best;
            StrokeProgress = 0f;
            m_StrokeSum = 0f;
            m_StrokeSamples = 0;
            StrokeStarted?.Invoke(best);
            return true;
        }

        void Advance(float deltaTime)
        {
            StrokeProgress = Mathf.Min(1f, StrokeProgress + deltaTime / m_Settings.strokeSeconds);
            float distance = Mathf.Abs(Knife - LineAt(Stroke, StrokeProgress));
            OnTrack = distance <= m_Settings.perfectDistance;
            m_StrokeSum += Sample(distance, m_Settings);
            m_StrokeSamples++;
            if (StrokeProgress >= 1f) EndStroke();
        }

        void EndStroke()
        {
            int line = Stroke;
            // Letting go early wastes the rest of the line: the average so far, times how far the knife got.
            float average = m_StrokeSamples > 0 ? m_StrokeSum / m_StrokeSamples : 0f;
            float score = average * StrokeProgress;
            m_Scores[line] = score;
            m_Cut[line] = true;
            Stroke = -1;
            OnTrack = false;
            StrokeFinished?.Invoke(line, score);
        }

        bool AllCut()
        {
            foreach (bool cut in m_Cut)
                if (!cut) return false;
            return Stroke < 0;
        }

        public float Evaluate()
        {
            float sum = 0f;
            foreach (float s in m_Scores) sum += s;
            return m_Scores.Length > 0 ? sum / m_Scores.Length : 0f;
        }

        /// <summary>One moment of a stroke: full marks within the clean distance, falling to nothing beyond.</summary>
        public static float Sample(float distance, in ButcherSettings s)
        {
            if (distance <= s.perfectDistance) return 1f;
            return Mathf.Clamp01(1f - (distance - s.perfectDistance) / s.falloff);
        }
    }

    /// <summary>
    /// A cook at the block (Gunta): lines the knife up on each line's head, holds, and follows the line with a wobble that
    /// shrinks with skill; hesitates a little between strokes.
    /// </summary>
    public sealed class ButcherAutoPlayer : IMinigameAutoPlayer
    {
        readonly ButcherMinigame m_Game;
        readonly float m_Skill;
        readonly IRandom m_Random;
        int m_Line = -1;
        float m_Offset, m_Wait, m_Phase;

        public ButcherAutoPlayer(ButcherMinigame game, float skill, IRandom random)
        {
            m_Game = game;
            m_Skill = Mathf.Clamp01(skill);
            m_Random = random ?? new SeededRandom();
        }

        public MinigameInput NextInput(float deltaTime)
        {
            if (m_Game.IsComplete) return default;
            ButcherSettings s = m_Game.Settings;
            float error = (1f - m_Skill) * s.autoMaxError;
            if (m_Game.Stroke >= 0)
            {
                // Following the line, with a slow wobble.
                m_Phase += deltaTime * 5f;
                float target = m_Game.LineAt(m_Game.Stroke, m_Game.StrokeProgress + deltaTime / s.strokeSeconds) + m_Offset + Mathf.Sin(m_Phase) * error * 0.5f;
                return Steer(target, deltaTime, true);
            }
            int next = -1;
            for (int i = 0; i < m_Game.LineCount; i++)
                if (!m_Game.IsCut(i))
                {
                    next = i;
                    break;
                }
            if (next < 0) return default;
            if (next != m_Line)
            {
                m_Line = next;
                m_Offset = (m_Random.Value() * 2f - 1f) * error;
                m_Wait = (1f - m_Skill) * s.autoMaxHesitation * (0.5f + 0.5f * m_Random.Value());
                m_Phase = m_Random.Value() * 6f;
            }
            float head = m_Game.LineAt(next, 0f) + m_Offset;
            if (Mathf.Abs(head - m_Game.Knife) > s.knifeSpeed * deltaTime) return Steer(head, deltaTime, false);
            if (m_Wait > 0f)
            {
                m_Wait -= deltaTime;
                return default;
            }
            return new MinigameInput { ActionPressed = true, ActionHeld = true };
        }

        MinigameInput Steer(float target, float deltaTime, bool held)
        {
            float step = Mathf.Max(1e-4f, m_Game.Settings.knifeSpeed * deltaTime);
            float aim = Mathf.Clamp((target - m_Game.Knife) / step, -1f, 1f);
            return new MinigameInput { Aim = new Vector2(aim, 0f), ActionHeld = held };
        }
    }
}
