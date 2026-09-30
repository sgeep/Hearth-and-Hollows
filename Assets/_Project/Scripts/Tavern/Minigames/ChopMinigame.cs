using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Chopping tuning. Target: 5–10 seconds to chop a two-ingredient stew batch at default
    /// tuning (3–4 cuts per ingredient at a steady ~0.8 s per cut).
    /// </summary>
    [Serializable]
    public struct ChopSettings
    {
        [Min(1), Tooltip("Fewest guide lines on one ingredient.")]
        public int minLines;
        [Min(1), Tooltip("Most guide lines on one ingredient.")]
        public int maxLines;
        [Range(0, 0.45f), Tooltip("Guide lines stay at least this far (board fraction) from the ends.")]
        public float edgeMargin;
        [Range(0, 0.45f), Tooltip("Random nudge of each guide line, as a fraction of the spacing between lines.")]
        public float jitter;
        [Min(0), Tooltip("A cut within this distance (board fraction) of its line scores full marks.")]
        public float perfectDistance;
        [Min(0.01f), Tooltip("Distance beyond perfect at which a cut scores zero.")]
        public float falloff;
        [Min(0.1f), Tooltip("Knife speed with the stick / keys (board widths per second).")]
        public float knifeSpeed;
        [Min(1), Tooltip("Seconds to chop one ingredient; lines left uncut score zero.")]
        public float itemTimeLimit;
        [Min(0), Tooltip("Pause between ingredients.")]
        public float itemPause;

        [Header("Screen placement (the mouse maps onto this span)")]
        [Range(0, 1), Tooltip("Left edge of the chopping board, as a fraction of the screen width.")]
        public float boardLeft;
        [Range(0.1f, 1), Tooltip("Width of the chopping board, as a fraction of the screen width.")]
        public float boardWidth;

        [Header("Staff auto-play")]
        [Range(0, 0.5f), Tooltip("Largest cut error (board fraction) for a skill-0 staff member; scales down with skill.")]
        public float autoMaxError;
        [Min(0), Tooltip("Hesitation before each cut for a skill-0 staff member; scales down with skill.")]
        public float autoMaxHesitation;

        public static ChopSettings Default => new()
        {
            minLines = 3,
            maxLines = 4,
            edgeMargin = 0.15f,
            jitter = 0.25f,
            perfectDistance = 0.015f,
            falloff = 0.1f,
            knifeSpeed = 1.1f,
            itemTimeLimit = 6f,
            itemPause = 0.35f,
            boardLeft = 0.25f,
            boardWidth = 0.5f,
            autoMaxError = 0.12f,
            autoMaxHesitation = 0.6f,
        };
    }

    /// <summary>
    /// Chopping: each ingredient shows guide lines across a board. Move the knife (mouse, or
    /// stick/keys) and press Action to cut; each cut counts for the nearest uncut line and scores
    /// by how close it landed. Lines still uncut when an ingredient's time runs out score zero.
    /// Score = the average over every line of every ingredient.
    /// </summary>
    public sealed class ChopMinigame : IMinigame
    {
        readonly ChopSettings m_Settings;
        readonly float[][] m_Lines;
        readonly float[][] m_CutAt;
        readonly float[][] m_Scores;
        float m_Pause;

        public ChopMinigame(ChopSettings settings, int items, IRandom random)
        {
            m_Settings = settings;
            int count = Math.Max(1, items);
            random ??= new SeededRandom();
            m_Lines = new float[count][];
            m_CutAt = new float[count][];
            m_Scores = new float[count][];
            for (int i = 0; i < count; i++)
            {
                m_Lines[i] = MakeLines(settings, random);
                m_CutAt[i] = new float[m_Lines[i].Length];
                m_Scores[i] = new float[m_Lines[i].Length];
            }
        }

        public ChopSettings Settings => m_Settings;
        public int ItemCount => m_Lines.Length;
        /// <summary>The ingredient being chopped (0-based).</summary>
        public int Item { get; private set; }
        /// <summary>Knife position, 0–1 across the board.</summary>
        public float Knife { get; private set; }
        public float ItemTimeLeft { get; private set; }
        public bool IsPausing => m_Pause > 0f;
        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }
        public int TotalLines
        {
            get
            {
                int n = 0;
                foreach (var l in m_Lines) n += l.Length;
                return n;
            }
        }

        /// <summary>Raised with the score of each cut.</summary>
        public event Action<float> Cut;

        public float[] LinesOf(int item) => m_Lines[item];
        public bool IsCut(int item, int line) => !float.IsNaN(m_CutAt[item][line]);
        /// <summary>Where the knife came down for that line (NaN if uncut).</summary>
        public float CutPosition(int item, int line) => m_CutAt[item][line];
        public float CutScore(int item, int line) => m_Scores[item][line];

        public void Begin()
        {
            Item = 0;
            Knife = 0.5f;
            Elapsed = 0f;
            m_Pause = 0f;
            IsComplete = false;
            for (int i = 0; i < m_Lines.Length; i++)
            {
                Array.Fill(m_CutAt[i], float.NaN);
                Array.Clear(m_Scores[i], 0, m_Scores[i].Length);
            }
            ItemTimeLeft = m_Settings.itemTimeLimit;
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;

            if (m_Pause > 0f)
            {
                m_Pause -= deltaTime;
                if (m_Pause <= 0f)
                {
                    Item++;
                    ItemTimeLeft = m_Settings.itemTimeLimit;
                }
                return;
            }

            Knife = input.PointerActive
                ? Mathf.Clamp01(input.Pointer)
                : Mathf.Clamp01(Knife + Mathf.Clamp(input.Aim.x, -1f, 1f) * m_Settings.knifeSpeed * deltaTime);
            ItemTimeLeft -= deltaTime;
            if (input.ActionPressed) CutNearest();
            if (AllCut(Item) || ItemTimeLeft <= 0f) FinishItem();
        }

        void CutNearest()
        {
            var lines = m_Lines[Item];
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < lines.Length; i++)
            {
                if (IsCut(Item, i)) continue;
                float d = Mathf.Abs(lines[i] - Knife);
                if (d < bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }
            if (best < 0) return;
            float score = ScoreCut(bestDistance, m_Settings);
            m_CutAt[Item][best] = Knife;
            m_Scores[Item][best] = score;
            Cut?.Invoke(score);
        }

        bool AllCut(int item)
        {
            for (int i = 0; i < m_Lines[item].Length; i++) if (!IsCut(item, i)) return false;
            return true;
        }

        void FinishItem()
        {
            if (Item >= m_Lines.Length - 1)
            {
                IsComplete = true;
                return;
            }
            m_Pause = Mathf.Max(m_Settings.itemPause, 1e-4f);
        }

        public float Evaluate()
        {
            float sum = 0f;
            int n = 0;
            foreach (var scores in m_Scores)
            {
                foreach (var s in scores) sum += s;
                n += scores.Length;
            }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>Score for a cut <paramref name="distance"/> (board fraction) from its guide line.</summary>
        public static float ScoreCut(float distance, in ChopSettings s)
        {
            if (distance <= s.perfectDistance) return 1f;
            return Mathf.Clamp01(1f - (distance - s.perfectDistance) / s.falloff);
        }

        static float[] MakeLines(in ChopSettings s, IRandom random)
        {
            int count = random.Range(Math.Max(1, s.minLines), Math.Max(s.minLines, s.maxLines));
            var lines = new float[count];
            float span = 1f - 2f * s.edgeMargin;
            float spacing = span / count;
            for (int i = 0; i < count; i++)
            {
                float nudge = (random.Value() * 2f - 1f) * s.jitter * spacing;
                lines[i] = Mathf.Clamp(s.edgeMargin + spacing * (i + 0.5f) + nudge, s.edgeMargin, 1f - s.edgeMargin);
            }
            return lines;
        }
    }

    /// <summary>Slides the knife to each line left to right and cuts; lower skill cuts less accurately and hesitates longer.</summary>
    public sealed class ChopAutoPlayer : IMinigameAutoPlayer
    {
        readonly ChopMinigame m_Game;
        readonly float m_Skill;
        readonly IRandom m_Random;
        int m_Item = -1, m_Line = -1;
        float m_Target, m_Wait;

        public ChopAutoPlayer(ChopMinigame game, float skill, IRandom random)
        {
            m_Game = game;
            m_Skill = Mathf.Clamp01(skill);
            m_Random = random;
        }

        public MinigameInput NextInput(float deltaTime)
        {
            if (m_Game.IsComplete || m_Game.IsPausing) return default;
            int line = NextUncut();
            if (line < 0) return default;
            if (m_Item != m_Game.Item || m_Line != line)
            {
                m_Item = m_Game.Item;
                m_Line = line;
                var s = m_Game.Settings;
                m_Target = m_Game.LinesOf(m_Item)[line] + (m_Random.Value() * 2f - 1f) * (1f - m_Skill) * s.autoMaxError;
                m_Wait = (1f - m_Skill) * s.autoMaxHesitation * (0.5f + 0.5f * m_Random.Value());
            }

            float step = m_Game.Settings.knifeSpeed * deltaTime;
            float offset = m_Target - m_Game.Knife;
            if (Mathf.Abs(offset) > step) return new MinigameInput { Aim = new Vector2(Mathf.Sign(offset), 0f) };
            if (m_Wait > 0f)
            {
                m_Wait -= deltaTime;
                return new MinigameInput { Aim = new Vector2(offset / step, 0f) };
            }
            return new MinigameInput { Aim = new Vector2(offset / step, 0f), ActionPressed = true, ActionHeld = true };
        }

        int NextUncut()
        {
            var lines = m_Game.LinesOf(m_Game.Item);
            for (int i = 0; i < lines.Length; i++) if (!m_Game.IsCut(m_Game.Item, i)) return i;
            return -1;
        }
    }
}
