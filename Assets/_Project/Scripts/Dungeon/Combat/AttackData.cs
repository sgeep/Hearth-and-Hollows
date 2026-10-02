using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>
    /// Frame data for one attack in a combo. Frames are authored at 60 fps (see
    /// <see cref="FrameData.FramesPerSecond"/>) regardless of the render/physics rate.
    /// </summary>
    [Serializable]
    public sealed class AttackData
    {
        public string debugName = "Attack";

        [Header("Frame Data (60 fps)")]
        [Min(0)] public int startupFrames = 5;
        [Min(1)] public int activeFrames = 3;
        [Min(0)] public int recoveryFrames = 14;
        [Min(0), Tooltip("Frame (from the start of the attack) at which the next combo hit, jump, or dodge may cancel the recovery.")]
        public int cancelFrame = 12;

        [Header("Damage")]
        [Min(0)] public float damage = 10f;
        [Tooltip("Hitbox centre relative to the character, for a right-facing character.")]
        public Vector2 hitboxOffset = new(0.9f, 0.75f);
        public Vector2 hitboxSize = new(1.5f, 1.1f);
        public Vector2 knockback = new(4f, 2f);
        [Min(0), Tooltip("How long a hit enemy is staggered.")]
        public float staggerTime = 0.25f;

        [Header("Movement")]
        [Tooltip("Forward speed applied during the lunge frames.")]
        public float lungeSpeed = 3f;
        [Min(0)] public int lungeStartFrame = 2;
        [Min(0)] public int lungeFrames = 5;

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
    }
}
