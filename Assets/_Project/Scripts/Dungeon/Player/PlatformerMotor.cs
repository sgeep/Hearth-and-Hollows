using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    public struct MotorInput
    {
        public float MoveX;
        public float MoveY;
        /// <summary>True only on the step the button went down.</summary>
        public bool JumpPressed;
        public bool JumpHeld;
        public bool DodgePressed;
    }

    /// <summary>Contact state reported by the mover before each step.</summary>
    public struct MotorCollisions
    {
        public bool Grounded;
        /// <summary>Standing on a one-way platform (and not on solid ground).</summary>
        public bool OnOneWay;
        public bool WallLeft;
        public bool WallRight;
        public bool Ceiling;
    }

    public enum MotorState
    {
        Grounded,
        Airborne,
        WallSlide,
        Dodge,
    }

    /// <summary>
    /// Pure platformer movement logic: takes input + contacts, produces a velocity.
    /// Handles run, variable jump, coyote time, jump buffer, wall slide/jump, dodge roll
    /// with i-frames, and drop-through requests. Collision resolution is the mover's job.
    /// </summary>
    public sealed class PlatformerMotor
    {
        readonly MovementSettings m_Settings;
        readonly InputBuffer m_JumpBuffer;
        readonly InputBuffer m_DodgeBuffer;
        readonly Countdown m_Coyote = new();
        readonly Countdown m_WallCoyote = new();
        readonly Countdown m_InputLock = new();
        readonly Countdown m_DropThrough = new();
        readonly Countdown m_DodgeCooldown = new();

        Vector2 m_Velocity;
        float m_DodgeElapsed;
        bool m_DodgeGrounded;
        bool m_AirDodgeUsed;
        int m_LastWallDirection;
        bool m_WasGrounded;

        public PlatformerMotor(MovementSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_JumpBuffer = new InputBuffer(settings.jumpBufferTime);
            m_DodgeBuffer = new InputBuffer(settings.dodgeBufferTime);
        }

        public MovementSettings Settings => m_Settings;
        public Vector2 Velocity => m_Velocity;
        public MotorState State { get; private set; } = MotorState.Airborne;
        /// <summary>+1 facing right, -1 facing left.</summary>
        public int Facing { get; private set; } = 1;

        /// <summary>True while the mover should pass through one-way platforms.</summary>
        public bool IgnoreOneWay => m_DropThrough.IsRunning;

        public bool IsDodging => State == MotorState.Dodge;

        public bool IsInvulnerable =>
            State == MotorState.Dodge &&
            m_DodgeElapsed >= m_Settings.dodgeIFrameStart &&
            m_DodgeElapsed < m_Settings.dodgeIFrameEnd;

        /// <summary>
        /// Set by the controller while attacking or stunned: horizontal input, jumps and dodges
        /// are held (still buffered) and gravity still applies.
        /// </summary>
        public bool ActionLocked { get; set; }

        /// <summary>When set, replaces horizontal velocity this step (attack lunges).</summary>
        public float? HorizontalOverride { get; set; }

        public bool IsJumpBuffered => m_JumpBuffer.IsBuffered;
        public bool IsDodgeBuffered => m_DodgeBuffer.IsBuffered;

        public event Action Jumped;
        public event Action WallJumped;
        public event Action DodgeStarted;
        public event Action DodgeEnded;
        public event Action Landed;
        public event Action DroppedThrough;

        /// <summary>Knock the character back and suppress control briefly (e.g. when hit).</summary>
        public void ApplyKnockback(Vector2 velocity, float controlLockTime)
        {
            if (State == MotorState.Dodge) EndDodge();
            m_Velocity = velocity;
            m_InputLock.Start(controlLockTime);
        }

        public void SetFacing(int direction)
        {
            if (direction != 0) Facing = direction > 0 ? 1 : -1;
        }

        public void Step(float dt, in MotorInput input, in MotorCollisions contacts)
        {
            if (dt <= 0f) return;

            if (input.JumpPressed) m_JumpBuffer.Press();
            if (input.DodgePressed) m_DodgeBuffer.Press();

            bool touchingWall = contacts.WallLeft || contacts.WallRight;
            if (contacts.Grounded)
            {
                m_Coyote.Start(m_Settings.coyoteTime);
                m_AirDodgeUsed = false;
            }
            if (!contacts.Grounded && touchingWall)
            {
                m_LastWallDirection = contacts.WallLeft ? -1 : 1;
                m_WallCoyote.Start(m_Settings.wallCoyoteTime);
            }

            if (contacts.Grounded && !m_WasGrounded && m_Velocity.y <= 0f) Landed?.Invoke();
            m_WasGrounded = contacts.Grounded;

            if (State == MotorState.Dodge) StepDodge(dt, input, contacts);
            else StepNormal(dt, input, contacts, touchingWall);

            // Timers tick after use so a press is usable on the step it happens.
            m_JumpBuffer.Tick(dt);
            m_DodgeBuffer.Tick(dt);
            if (!contacts.Grounded) m_Coyote.Tick(dt);
            if (contacts.Grounded || !touchingWall) m_WallCoyote.Tick(dt);
            m_InputLock.Tick(dt);
            m_DropThrough.Tick(dt);
            m_DodgeCooldown.Tick(dt);
        }

        void StepNormal(float dt, in MotorInput input, in MotorCollisions contacts, bool touchingWall)
        {
            bool controllable = !ActionLocked && !m_InputLock.IsRunning;

            // Dodge start (held while action-locked; the controller cancels attacks first if allowed).
            if (!ActionLocked && m_DodgeBuffer.IsBuffered && !m_DodgeCooldown.IsRunning &&
                (contacts.Grounded || (m_Settings.allowAirDodge && !m_AirDodgeUsed)))
            {
                StartDodge(input, contacts);
                StepDodge(dt, input, contacts);
                return;
            }

            // Horizontal.
            if (HorizontalOverride.HasValue)
            {
                m_Velocity.x = HorizontalOverride.Value;
            }
            else
            {
                float target = controllable ? Mathf.Clamp(input.MoveX, -1f, 1f) * m_Settings.runSpeed : (ActionLocked ? 0f : m_Velocity.x);
                bool accelerating = Mathf.Abs(target) > 0.01f && (Mathf.Sign(target) == Mathf.Sign(m_Velocity.x) || Mathf.Abs(m_Velocity.x) < 0.01f);
                float rate = contacts.Grounded
                    ? (accelerating ? m_Settings.groundAcceleration : m_Settings.groundDeceleration)
                    : (accelerating ? m_Settings.airAcceleration : m_Settings.airDeceleration);
                m_Velocity.x = Mathf.MoveTowards(m_Velocity.x, target, rate * dt);
            }

            // Jumps: drop-through > ground/coyote jump > wall jump.
            if (!ActionLocked && m_JumpBuffer.IsBuffered)
            {
                if (contacts.Grounded && contacts.OnOneWay && input.MoveY < -0.5f)
                {
                    m_JumpBuffer.Consume();
                    m_DropThrough.Start(m_Settings.dropThroughTime);
                    m_Coyote.Stop();
                    m_Velocity.y = Mathf.Min(m_Velocity.y, 0f);
                    DroppedThrough?.Invoke();
                }
                else if (contacts.Grounded || m_Coyote.IsRunning)
                {
                    m_JumpBuffer.Consume();
                    m_Coyote.Stop();
                    m_Velocity.y = m_Settings.JumpVelocity;
                    Jumped?.Invoke();
                }
                else if (touchingWall || m_WallCoyote.IsRunning)
                {
                    int away = touchingWall ? (contacts.WallLeft ? 1 : -1) : -m_LastWallDirection;
                    m_JumpBuffer.Consume();
                    m_WallCoyote.Stop();
                    m_Velocity = new Vector2(away * m_Settings.wallJumpVelocity.x, m_Settings.wallJumpVelocity.y);
                    Facing = away;
                    m_InputLock.Start(m_Settings.wallJumpInputLock);
                    WallJumped?.Invoke();
                }
            }

            // Gravity (heavier when falling, or when rising with jump released).
            float gravity = m_Settings.Gravity;
            if (m_Velocity.y < 0f) gravity *= m_Settings.fallGravityMultiplier;
            else if (m_Velocity.y > 0f && !input.JumpHeld) gravity *= m_Settings.jumpCutGravityMultiplier;
            m_Velocity.y = Mathf.Max(m_Velocity.y - gravity * dt, -m_Settings.maxFallSpeed);

            if (contacts.Ceiling && m_Velocity.y > 0f) m_Velocity.y = 0f;

            bool pressingIntoWall = (contacts.WallLeft && input.MoveX < -0.1f) || (contacts.WallRight && input.MoveX > 0.1f);
            if (contacts.Grounded && m_Velocity.y <= 0f)
            {
                m_Velocity.y = 0f;
                State = MotorState.Grounded;
            }
            else if (!contacts.Grounded && pressingIntoWall && m_Velocity.y <= 0f && controllable)
            {
                m_Velocity.y = Mathf.Max(m_Velocity.y, -m_Settings.wallSlideSpeed);
                State = MotorState.WallSlide;
            }
            else
            {
                State = contacts.Grounded && m_Velocity.y <= 0f ? MotorState.Grounded : MotorState.Airborne;
            }

            // Re-check: a wall jump this step starts the input lock and must keep facing away from the wall.
            bool canTurn = !ActionLocked && !m_InputLock.IsRunning;
            if (canTurn && Mathf.Abs(input.MoveX) > 0.1f) Facing = input.MoveX > 0f ? 1 : -1;
        }

        void StartDodge(in MotorInput input, in MotorCollisions contacts)
        {
            m_DodgeBuffer.Consume();
            if (Mathf.Abs(input.MoveX) > 0.1f) Facing = input.MoveX > 0f ? 1 : -1;
            State = MotorState.Dodge;
            m_DodgeElapsed = 0f;
            m_DodgeGrounded = contacts.Grounded;
            if (!contacts.Grounded) m_AirDodgeUsed = true;
            m_InputLock.Stop();
            DodgeStarted?.Invoke();
        }

        void StepDodge(float dt, in MotorInput input, in MotorCollisions contacts)
        {
            // Jump out of a grounded roll.
            if (m_Settings.jumpCancelsDodge && m_JumpBuffer.IsBuffered && contacts.Grounded && m_DodgeElapsed > 0f)
            {
                EndDodge();
                StepNormal(dt, input, contacts, contacts.WallLeft || contacts.WallRight);
                return;
            }

            m_DodgeElapsed += dt;
            m_Velocity.x = Facing * m_Settings.DodgeSpeed;

            bool suspend = !m_DodgeGrounded && m_Settings.airDodgeSuspendsGravity;
            if (suspend || contacts.Grounded) m_Velocity.y = 0f;
            else m_Velocity.y = Mathf.Max(m_Velocity.y - m_Settings.Gravity * m_Settings.fallGravityMultiplier * dt, -m_Settings.maxFallSpeed);

            // Rolling into a wall ends the roll early.
            if ((Facing < 0 && contacts.WallLeft) || (Facing > 0 && contacts.WallRight)) m_Velocity.x = 0f;

            if (m_DodgeElapsed >= m_Settings.dodgeDuration) EndDodge();
        }

        void EndDodge()
        {
            State = MotorState.Airborne;
            m_DodgeCooldown.Start(m_Settings.dodgeCooldown);
            m_Velocity.x *= 0.5f;
            DodgeEnded?.Invoke();
        }
    }
}
