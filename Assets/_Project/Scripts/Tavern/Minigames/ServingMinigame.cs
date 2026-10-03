using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>
    /// Serving tuning (4c: a top-down room). A typical trip from the pass to a table is 10–15 tiles of
    /// walking, about 3–5 seconds at the carry speed.
    /// </summary>
    [Serializable]
    public struct ServingSettings
    {
        [Min(0.1f), Tooltip("Walking speed while carrying a plate (tiles per second; the keeper walks at 6 without one).")]
        public float carrySpeed;
        [Min(0.05f), Tooltip("How near a seated customer the carrier must be to serve them (tiles).")]
        public float arriveDistance;
        [Min(0), Tooltip("Spill added by a normal bump into a customer walking across the floor.")]
        public float spillPerBump;
        [Range(0, 1), Tooltip("Score lost at a full spill meter (just before dropping).")]
        public float spillPenalty;
        [Min(1), Tooltip("Par time = shortest walkable distance / carry speed × this, plus the slack.")]
        public float parFactor;
        [Min(0)] public float parSlack;
        [Min(0), Tooltip("Can't be bumped again for this long after a bump.")]
        public float bumpCooldown;

        [Header("Staff auto-play")]
        [Range(0.1f, 1), Tooltip("Walking speed (fraction of carry speed) of a skill-0 staff member; skill 1 walks at full speed.")]
        public float autoSlowestSpeed;

        public static ServingSettings Default => new()
        {
            carrySpeed = 4f,
            arriveDistance = 1.2f,
            spillPerBump = 0.34f,
            spillPenalty = 0.6f,
            parFactor = 1.25f,
            parSlack = 0.75f,
            bumpCooldown = 0.6f,
            autoSlowestSpeed = 0.55f,
        };
    }

    /// <summary>
    /// Serving, top-down (4c): carry a plate from the pass through the room to a seated customer. The
    /// world does the walking; this keeps the time, the spill and the score. Bumping into a customer
    /// who is walking across the floor spills (harder bumps spill more); a full meter drops the plate (0).
    /// When the plate is served, the score compares the time taken with par for the shortest walkable
    /// path from the pass to where it was served, so wandering costs quality. Pure logic.
    /// </summary>
    public sealed class ServingMinigame : IMinigame
    {
        public const float DropAt = 1f;

        readonly ServingSettings m_Settings;
        float m_BumpCooldown;

        public ServingMinigame(ServingSettings settings) => m_Settings = settings;

        public ServingSettings Settings => m_Settings;
        public float Spill { get; private set; }
        public bool Dropped { get; private set; }
        /// <summary>Served (handed over).</summary>
        public bool Arrived { get; private set; }
        public bool IsComplete => Arrived || Dropped;
        public float Elapsed { get; private set; }
        /// <summary>Shortest walkable distance from the pass to where the plate was served (set on delivery).</summary>
        public float ShortestDistance { get; private set; }
        public float ParTime => ShortestDistance / m_Settings.carrySpeed * m_Settings.parFactor + m_Settings.parSlack;

        public void Begin()
        {
            Spill = 0f;
            Elapsed = 0f;
            ShortestDistance = 0f;
            Dropped = Arrived = false;
            m_BumpCooldown = 0f;
        }

        public void Tick(float deltaTime, in MinigameInput input)
        {
            if (IsComplete || deltaTime <= 0f) return;
            Elapsed += deltaTime;
            if (m_BumpCooldown > 0f) m_BumpCooldown -= deltaTime;
        }

        /// <summary>Hands the plate over, <paramref name="shortestDistance"/> (tiles of walkable path) from the pass. False if already finished.</summary>
        public bool Deliver(float shortestDistance)
        {
            if (IsComplete) return false;
            ShortestDistance = Mathf.Max(0f, shortestDistance);
            Arrived = true;
            return true;
        }

        /// <summary>
        /// The carrier collided with someone walking across the floor; <paramref name="strength"/> scales the
        /// spill (see <see cref="BumpStrength"/>). Returns true if it counted (not within the cooldown).
        /// </summary>
        public bool RegisterBump(float strength = 1f)
        {
            if (IsComplete || m_BumpCooldown > 0f) return false;
            m_BumpCooldown = m_Settings.bumpCooldown;
            Spill = Mathf.Min(DropAt, Spill + m_Settings.spillPerBump * Mathf.Max(0f, strength));
            if (Spill >= DropAt) Dropped = true;
            return true;
        }

        /// <summary>How hard a bump is: the speed the two close at, against the carry speed (0.5 a brush, 1.5 a collision at a run).</summary>
        public static float BumpStrength(float closingSpeed, float carrySpeed) =>
            Mathf.Clamp(closingSpeed / Mathf.Max(0.01f, carrySpeed), 0.5f, 1.5f);

        public float Evaluate()
        {
            if (Dropped || !Arrived) return 0f;
            float par = ParTime;
            float time = Elapsed <= par ? 1f : Mathf.Clamp01(1f - (Elapsed - par) / par);
            float spill = 1f - Spill * m_Settings.spillPenalty;
            return Mathf.Clamp01(time * spill);
        }
    }

    /// <summary>
    /// Staff serving: the world walks them to the table on the grid; skill sets how fast (lower skill
    /// walks slower and so scores lower). <see cref="NextInput"/>'s Move is that speed, as a fraction of
    /// the carry speed.
    /// </summary>
    public sealed class ServingAutoPlayer : IMinigameAutoPlayer
    {
        public ServingAutoPlayer(ServingMinigame game, float skill, IRandom random)
        {
            float s = Mathf.Clamp01(skill);
            SpeedFactor = Mathf.Lerp(game.Settings.autoSlowestSpeed, 1f, s) * Mathf.Lerp(0.9f, 1f, random.Value());
        }

        /// <summary>Walking speed while carrying, as a fraction of the carry speed.</summary>
        public float SpeedFactor { get; }

        public MinigameInput NextInput(float deltaTime) => new() { Move = SpeedFactor };
    }
}
