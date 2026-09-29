using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Serving tuning. Target: 5–10 seconds per delivery to a typical mid-floor table (about
    /// 8–12 tiles from the pass at the default carry speed).
    /// </summary>
    [Serializable]
    public struct ServingSettings
    {
        [Min(0.1f), Tooltip("Walking speed while carrying a plate (tiles per second).")]
        public float carrySpeed;
        [Min(0.05f), Tooltip("How close to the customer counts as arrived.")]
        public float arriveDistance;
        [Min(0), Tooltip("Spill added by each bump into someone crossing the floor.")]
        public float spillPerBump;
        [Range(0, 1), Tooltip("Score lost at a full spill meter (just before dropping).")]
        public float spillPenalty;
        [Min(1), Tooltip("Par time = distance / carry speed × this.")]
        public float parFactor;
        [Min(0)] public float parSlack;
        [Min(0), Tooltip("Can't be bumped again for this long after a bump.")]
        public float bumpCooldown;

        public static ServingSettings Default => new()
        {
            carrySpeed = 1.6f,
            arriveDistance = 0.4f,
            spillPerBump = 0.4f,
            spillPenalty = 0.6f,
            parFactor = 1.2f,
            parSlack = 0.5f,
            bumpCooldown = 0.6f,
        };
    }

    /// <summary>
    /// Serving: carry a plate along the tavern floor (x axis) from the pass to a table. Each
    /// bump into a customer crossing the floor spills; a full spill meter drops the plate (0).
    /// Score = time factor (vs par) × spill factor.
    /// </summary>
    public sealed class ServingMinigame : IMinigame
    {
        public const float DropAt = 1f;

        readonly ServingSettings m_Settings;
        readonly float m_Start;
        float m_BumpCooldown;

        public ServingMinigame(ServingSettings settings, float startX, float targetX)
        {
            m_Settings = settings;
            m_Start = startX;
            Target = targetX;
            Position = startX;
        }

        public ServingSettings Settings => m_Settings;
        public float Position { get; private set; }
        public float Target { get; }
        public float Spill { get; private set; }
        public bool Dropped { get; private set; }
        public bool Arrived { get; private set; }
        public bool IsComplete => Arrived || Dropped;
        public float Elapsed { get; private set; }
        public float ParTime => Mathf.Abs(Target - m_Start) / m_Settings.carrySpeed * m_Settings.parFactor + m_Settings.parSlack;

        public void Begin()
        {
            Position = m_Start;
            Spill = 0f;
            Elapsed = 0f;
            Dropped = Arrived = false;
            m_BumpCooldown = 0f;
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;
            if (m_BumpCooldown > 0f) m_BumpCooldown -= deltaTime;
            Position += Mathf.Clamp(input.Move, -1f, 1f) * m_Settings.carrySpeed * deltaTime;
            if (Mathf.Abs(Position - Target) <= m_Settings.arriveDistance) Arrived = true;
        }

        /// <summary>Called by the world when the carrier collides with someone. Returns true if it counted.</summary>
        public bool RegisterBump()
        {
            if (IsComplete || m_BumpCooldown > 0f) return false;
            m_BumpCooldown = m_Settings.bumpCooldown;
            Spill = Mathf.Min(DropAt, Spill + m_Settings.spillPerBump);
            if (Spill >= DropAt) Dropped = true;
            return true;
        }

        public float Evaluate()
        {
            if (Dropped || !Arrived) return 0f;
            float par = ParTime;
            float time = Elapsed <= par ? 1f : Mathf.Clamp01(1f - (Elapsed - par) / par);
            float spill = 1f - Spill * m_Settings.spillPenalty;
            return Mathf.Clamp01(time * spill);
        }
    }

    /// <summary>Walks the plate to the table; lower skill walks slower and hesitates.</summary>
    public sealed class ServingAutoPlayer : IMinigameAutoPlayer
    {
        readonly ServingMinigame m_Game;
        readonly float m_Speed;

        public ServingAutoPlayer(ServingMinigame game, float skill, IRandom random)
        {
            m_Game = game;
            float s = Mathf.Clamp01(skill);
            m_Speed = Mathf.Lerp(0.55f, 1f, s) * Mathf.Lerp(0.9f, 1f, random.Value());
        }

        public MinigameInput NextInput(float deltaTime) =>
            new() { Move = Mathf.Sign(m_Game.Target - m_Game.Position) * m_Speed };
    }
}
