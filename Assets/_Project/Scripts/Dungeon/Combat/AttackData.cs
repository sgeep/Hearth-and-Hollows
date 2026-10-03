using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Frame data for one player attack (a combo hit or a heavy charge step). Frames are authored
    /// at 60 fps (see <see cref="FrameData.FramesPerSecond"/>) regardless of the render/physics rate.
    /// </summary>
    [Serializable]
    public sealed class AttackData
    {
        public string debugName = "Attack";

        [Header("Frame Data (60 fps)")]
        [Min(0)] public int startupFrames = 5;
        [Min(1)] public int activeFrames = 3;
        [Min(0)] public int recoveryFrames = 14;
        [Min(0), Tooltip("Frame (from the start of the attack) at which the next combo hit or a dodge may cancel the recovery.")]
        public int cancelFrame = 12;

        [Header("Damage")]
        [Min(0)] public float damage = 10f;
        [Tooltip("Hitbox centre along the aim (x) for combo hits. Heavy steps are centred on the player.")]
        public Vector2 hitboxOffset = new(0.9f, 0.75f);
        [Tooltip("Hitbox size in tiles. For heavy steps (a spin), x is the diameter of the circle.")]
        public Vector2 hitboxSize = new(1.5f, 1.1f);
        [Min(0), Tooltip("Knockback: the enemy slides away from the player starting at this speed (tiles/s), stopping after 0.15 s (about force × 0.075 tiles).")]
        public float knockbackForce = 7f;
        [Min(0), Tooltip("How long a hit enemy is staggered, in seconds (scaled by the enemy's stagger multiplier).")]
        public float staggerTime = 0.25f;

        [Header("Feel")]
        [Min(0), Tooltip("Freeze-frame on hit, in seconds of real time.")]
        public float hitStop = 0.06f;
        [Min(0)] public float screenShake = 0.15f;

        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    }

    public static class FrameData
    {
        public const float FramesPerSecond = 60f;

        public static float ToSeconds(int frames) => frames / FramesPerSecond;

        /// <summary>How long a struck target ignores a swing: past the end of its active window, by a frame, so it hits once.</summary>
        public static float OneHitInvincibility(float activeDuration) => System.Math.Max(0.1f, activeDuration + ToSeconds(1));
    }
}
