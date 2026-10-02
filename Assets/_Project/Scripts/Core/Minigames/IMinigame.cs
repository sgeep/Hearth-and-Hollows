using UnityEngine;

namespace Hearthdelve.Core.Minigames
{
    /// <summary>Everything a minigame reads from the player (or an auto-player) in one tick.</summary>
    public struct MinigameInput
    {
        public bool ActionPressed;
        public bool ActionHeld;
        public bool ActionReleased;
        /// <summary>Stick / WASD, each axis -1..1.</summary>
        public Vector2 Aim;
        /// <summary>Horizontal movement -1..1 (movement-based minigames such as Serving).</summary>
        public float Move;
        /// <summary>True when the mouse moved this tick; <see cref="Pointer"/> is then valid.</summary>
        public bool PointerActive;
        /// <summary>Horizontal pointer position, 0–1 across the minigame's play area (e.g. the chopping board).</summary>
        public float Pointer;
    }

    /// <summary>
    /// Common contract for every station minigame (CLAUDE.md), so a staff member can
    /// auto-resolve any station by driving the same loop with an <see cref="IMinigameAutoPlayer"/>.
    /// </summary>
    public interface IMinigame
    {
        void Begin();
        void Tick(float deltaTime, in MinigameInput input);
        bool IsComplete { get; }
        float Elapsed { get; }
        /// <summary>Score from 0 (ruined) to 1 (perfect).</summary>
        float Evaluate();
    }

    /// <summary>Produces inputs for a specific minigame instance, e.g. a staff member of a given skill.</summary>
    public interface IMinigameAutoPlayer
    {
        MinigameInput NextInput(float deltaTime);
    }

    public static class MinigameRunner
    {
        /// <summary>Runs a minigame to completion with an auto-player (tests and simulations).</summary>
        public static float RunToCompletion(IMinigame game, IMinigameAutoPlayer player, float deltaTime = 1f / 60f, float maxSeconds = 60f)
        {
            game.Begin();
            float t = 0f;
            while (!game.IsComplete && t < maxSeconds)
            {
                game.Tick(deltaTime, player.NextInput(deltaTime));
                t += deltaTime;
            }
            return game.Evaluate();
        }
    }
}
