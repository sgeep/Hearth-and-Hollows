using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// Every movement tuning value (units are world units = 32 px tiles, and seconds).
    /// Lives inside <see cref="PlayerMovementConfig"/> so it can be edited in the inspector,
    /// and is a plain serializable class so the motor logic can be unit-tested.
    /// </summary>
    [Serializable]
    public sealed class MovementSettings
    {
        [Header("Run")]
        [Min(0)] public float runSpeed = 8f;
        [Min(0)] public float groundAcceleration = 90f;
        [Min(0)] public float groundDeceleration = 110f;
        [Min(0)] public float airAcceleration = 60f;
        [Min(0)] public float airDeceleration = 35f;

        [Header("Jump")]
        [Min(0.1f), Tooltip("Apex height of a full (held) jump, in tiles.")]
        public float jumpHeight = 3.2f;
        [Min(0.05f), Tooltip("Seconds from takeoff to apex of a full jump. Gravity is derived from height and this.")]
        public float timeToApex = 0.36f;
        [Min(1f), Tooltip("Gravity multiplier while falling. Higher = snappier landings.")]
        public float fallGravityMultiplier = 1.7f;
        [Min(1f), Tooltip("Gravity multiplier while rising with jump released (variable jump height).")]
        public float jumpCutGravityMultiplier = 2.6f;
        [Min(0)] public float maxFallSpeed = 20f;
        [Min(0), Tooltip("Grace period after walking off a ledge in which a jump still works.")]
        public float coyoteTime = 0.1f;
        [Min(0), Tooltip("A jump pressed this long before landing still fires on landing.")]
        public float jumpBufferTime = 0.12f;

        [Header("Wall")]
        [Min(0)] public float wallSlideSpeed = 3f;
        public Vector2 wallJumpVelocity = new(9f, 15f);
        [Min(0), Tooltip("Horizontal input is ignored this long after a wall jump so it pushes off.")]
        public float wallJumpInputLock = 0.14f;
        [Min(0), Tooltip("Grace period after leaving a wall in which a wall jump still works.")]
        public float wallCoyoteTime = 0.08f;

        [Header("Dodge Roll")]
        [Min(0)] public float dodgeDistance = 4.5f;
        [Min(0.01f)] public float dodgeDuration = 0.3f;
        [Min(0), Tooltip("I-frames begin this many seconds into the roll.")]
        public float dodgeIFrameStart = 0.0f;
        [Min(0), Tooltip("I-frames end this many seconds into the roll.")]
        public float dodgeIFrameEnd = 0.26f;
        [Min(0)] public float dodgeCooldown = 0.35f;
        [Min(0)] public float dodgeBufferTime = 0.1f;
        public bool allowAirDodge = true;
        [Tooltip("Air dodges hold altitude instead of falling.")]
        public bool airDodgeSuspendsGravity = true;
        [Tooltip("Jumping during a grounded roll cancels it.")]
        public bool jumpCancelsDodge = true;

        [Header("Drop-through Platforms")]
        [Min(0), Tooltip("How long one-way platforms are ignored after dropping through.")]
        public float dropThroughTime = 0.25f;

        public float Gravity => JumpMath.Gravity(jumpHeight, timeToApex);
        public float JumpVelocity => JumpMath.LaunchVelocity(jumpHeight, timeToApex);
        public float DodgeSpeed => dodgeDistance / dodgeDuration;
    }

    public static class JumpMath
    {
        /// <summary>Constant gravity that makes a jump of height h peak after time t: g = 2h / t².</summary>
        public static float Gravity(float height, float timeToApex) => 2f * height / (timeToApex * timeToApex);

        /// <summary>Launch speed for height h peaking at time t: v = 2h / t.</summary>
        public static float LaunchVelocity(float height, float timeToApex) => 2f * height / timeToApex;
    }
}
