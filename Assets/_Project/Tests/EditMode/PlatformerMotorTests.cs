using Hearthdelve.Dungeon.Player;
using NUnit.Framework;
using UnityEngine;
using static Hearthdelve.Tests.MotorTestUtil;

namespace Hearthdelve.Tests
{
    public class JumpMathTests
    {
        [Test]
        public void DerivedGravityAndVelocity_ReachConfiguredApex()
        {
            const float height = 3.2f, apexTime = 0.36f;
            float g = JumpMath.Gravity(height, apexTime);
            float v = JumpMath.LaunchVelocity(height, apexTime);

            // Kinematics: apex height = v² / 2g, apex time = v / g.
            Assert.That(v * v / (2f * g), Is.EqualTo(height).Within(1e-4f));
            Assert.That(v / g, Is.EqualTo(apexTime).Within(1e-4f));
        }

        [Test]
        public void SimulatedHeldJump_PeaksNearConfiguredHeight()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            motor.Step(Dt, Jump, Ground);

            float y = 0f, peak = 0f;
            for (int i = 0; i < 120; i++)
            {
                y += motor.Velocity.y * Dt;
                peak = Mathf.Max(peak, y);
                motor.Step(Dt, Hold, Air);
            }
            // Discrete integration at 60 Hz lands within a few percent of the analytic apex.
            Assert.That(peak, Is.EqualTo(settings.jumpHeight).Within(settings.jumpHeight * 0.06f));
        }
    }

    public class CoyoteAndBufferTests
    {
        [Test]
        public void Jump_FromGround_SetsLaunchVelocity()
        {
            var motor = new PlatformerMotor(Settings());
            motor.Step(Dt, Jump, Ground);
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f));
        }

        [Test]
        public void Jump_WithinCoyoteWindow_AfterLeavingLedge_Works()
        {
            var motor = new PlatformerMotor(Settings());
            Run(motor, 3, None, Ground);
            Run(motor, 4, None, Air); // ~0.067 s after leaving the ledge, inside 0.1 s
            motor.Step(Dt, Jump, Air);
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f));
        }

        [Test]
        public void Jump_AfterCoyoteWindow_DoesNothing()
        {
            var motor = new PlatformerMotor(Settings());
            Run(motor, 3, None, Ground);
            Run(motor, 10, None, Air); // ~0.167 s, past 0.1 s
            motor.Step(Dt, Jump, Air);
            Assert.That(motor.Velocity.y, Is.LessThan(0f));
        }

        [Test]
        public void Coyote_IsSpentByAJump_NoDoubleJump()
        {
            var motor = new PlatformerMotor(Settings());
            motor.Step(Dt, Jump, Ground);
            Run(motor, 2, Hold, Air);
            float before = motor.Velocity.y;
            motor.Step(Dt, Jump, Air);
            Assert.That(motor.Velocity.y, Is.LessThan(before), "second press in the air must not re-launch");
        }

        [Test]
        public void BufferedJump_PressedJustBeforeLanding_FiresOnLanding()
        {
            var motor = new PlatformerMotor(Settings());
            Run(motor, 20, None, Air);            // falling, coyote long gone
            motor.Step(Dt, Jump, Air);            // pressed in the air: too early
            Assert.That(motor.Velocity.y, Is.LessThan(0f));
            Run(motor, 4, Hold, Air);             // ~0.067 s later, still inside 0.12 s buffer
            motor.Step(Dt, Hold, Ground);         // touch down
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f), "buffered jump should fire on landing");
        }

        [Test]
        public void BufferedJump_Expires()
        {
            var motor = new PlatformerMotor(Settings());
            Run(motor, 20, None, Air);
            motor.Step(Dt, Jump, Air);
            Run(motor, 12, Hold, Air);            // 0.2 s: buffer expired
            motor.Step(Dt, Hold, Ground);
            Assert.That(motor.Velocity.y, Is.EqualTo(0f));
            Assert.That(motor.State, Is.EqualTo(MotorState.Grounded));
        }

        [Test]
        public void ReleasingJumpEarly_GivesLowerJump()
        {
            float Peak(bool hold)
            {
                var motor = new PlatformerMotor(Settings());
                motor.Step(Dt, Jump, Ground);
                float y = 0f, peak = 0f;
                for (int i = 0; i < 120; i++)
                {
                    y += motor.Velocity.y * Dt;
                    peak = Mathf.Max(peak, y);
                    motor.Step(Dt, hold ? Hold : None, Air);
                }
                return peak;
            }
            Assert.That(Peak(false), Is.LessThan(Peak(true) * 0.8f));
        }
    }

    public class WallTests
    {
        [Test]
        public void PressingIntoWall_WhileFalling_ClampsFallSpeed()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            Run(motor, 60, Move(1f), WallRight);
            Assert.That(motor.State, Is.EqualTo(MotorState.WallSlide));
            Assert.That(motor.Velocity.y, Is.GreaterThanOrEqualTo(-settings.wallSlideSpeed - 1e-4f));
        }

        [Test]
        public void NotPressingIntoWall_FallsNormally()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            Run(motor, 60, None, WallRight);
            Assert.That(motor.Velocity.y, Is.LessThan(-settings.wallSlideSpeed));
        }

        [Test]
        public void WallJump_PushesAwayFromWall_AndLocksInput()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            Run(motor, 20, Move(1f), WallRight);
            motor.Step(Dt, new MotorInput { MoveX = 1f, JumpPressed = true, JumpHeld = true }, WallRight);

            Assert.That(motor.Velocity.x, Is.LessThan(0f), "must push left, away from a right wall");
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f));
            Assert.That(motor.Facing, Is.EqualTo(-1));

            // Holding toward the wall during the lock must not immediately cancel the push-off.
            motor.Step(Dt, new MotorInput { MoveX = 1f, JumpHeld = true }, Air);
            Assert.That(motor.Velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void WallJump_FromLeftWall_PushesRight()
        {
            var motor = new PlatformerMotor(Settings());
            Run(motor, 20, Move(-1f), WallLeft);
            motor.Step(Dt, Jump, WallLeft);
            Assert.That(motor.Velocity.x, Is.GreaterThan(0f));
        }
    }

    public class DodgeTests
    {
        [Test]
        public void Dodge_HasIFramesOnlyInsideWindow()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            motor.Step(Dt, Dodge, Ground);
            Assert.That(motor.IsDodging);
            Assert.That(motor.IsInvulnerable, Is.False, "i-frames start at 0.05 s");

            Run(motor, 4, None, Ground); // ~0.083 s
            Assert.That(motor.IsInvulnerable, Is.True);

            Run(motor, 11, None, Ground); // ~0.267 s, past the 0.25 s end
            Assert.That(motor.IsInvulnerable, Is.False);
        }

        [Test]
        public void Dodge_MovesAtDodgeSpeed_InFacingDirection()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            motor.Step(Dt, new MotorInput { DodgePressed = true, MoveX = -1f }, Ground);
            Assert.That(motor.Velocity.x, Is.EqualTo(-settings.DodgeSpeed).Within(1e-4f));
        }

        [Test]
        public void Dodge_EndsAfterDuration_ThenCooldownBlocksImmediateReroll()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            motor.Step(Dt, Dodge, Ground);
            Run(motor, 19, None, Ground); // 0.33 s > 0.3 s
            Assert.That(motor.IsDodging, Is.False);

            motor.Step(Dt, Dodge, Ground);
            Assert.That(motor.IsDodging, Is.False, "cooldown should block");

            Run(motor, 25, None, Ground); // cooldown elapsed, but the buffered press expired
            motor.Step(Dt, Dodge, Ground);
            Assert.That(motor.IsDodging, Is.True);
        }

        [Test]
        public void AirDodge_OnlyOncePerAirtime()
        {
            var settings = Settings();
            settings.dodgeCooldown = 0f;
            var motor = new PlatformerMotor(settings);
            Run(motor, 5, None, Air);
            motor.Step(Dt, Dodge, Air);
            Assert.That(motor.IsDodging);
            Run(motor, 20, None, Air);
            motor.Step(Dt, Dodge, Air);
            Assert.That(motor.IsDodging, Is.False);
        }

        [Test]
        public void ActionLocked_HoldsDodgeUntilUnlocked()
        {
            var motor = new PlatformerMotor(Settings()) { ActionLocked = true };
            motor.Step(Dt, Dodge, Ground);
            Assert.That(motor.IsDodging, Is.False);
            motor.ActionLocked = false;
            motor.Step(Dt, None, Ground);
            Assert.That(motor.IsDodging, Is.True, "buffered dodge fires once unlocked");
        }
    }

    public class DropThroughTests
    {
        static MotorInput DownJump => new() { MoveY = -1f, JumpPressed = true, JumpHeld = true };

        [Test]
        public void DownJump_OnOneWay_DropsThrough()
        {
            var motor = new PlatformerMotor(Settings());
            motor.Step(Dt, DownJump, OneWay);
            Assert.That(motor.IgnoreOneWay, Is.True);
            Assert.That(motor.Velocity.y, Is.LessThanOrEqualTo(0f), "must not jump");
        }

        [Test]
        public void DownJump_OnSolidGround_JumpsNormally()
        {
            var motor = new PlatformerMotor(Settings());
            motor.Step(Dt, DownJump, Ground);
            Assert.That(motor.IgnoreOneWay, Is.False);
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f));
        }

        [Test]
        public void DropThrough_ExpiresAfterConfiguredTime()
        {
            var motor = new PlatformerMotor(Settings());
            motor.Step(Dt, DownJump, OneWay);
            Run(motor, 16, None, Air); // ~0.27 s > 0.25 s
            Assert.That(motor.IgnoreOneWay, Is.False);
        }
    }

    public class RunTests
    {
        [Test]
        public void Running_ReachesButDoesNotExceedRunSpeed()
        {
            var settings = Settings();
            var motor = new PlatformerMotor(settings);
            Run(motor, 60, Move(1f), Ground);
            Assert.That(motor.Velocity.x, Is.EqualTo(settings.runSpeed).Within(1e-4f));
        }

        [Test]
        public void ActionLocked_StopsVoluntaryMovement_ButLungeOverrideApplies()
        {
            var motor = new PlatformerMotor(Settings()) { ActionLocked = true };
            Run(motor, 30, Move(1f), Ground);
            Assert.That(motor.Velocity.x, Is.EqualTo(0f).Within(1e-4f));

            motor.HorizontalOverride = 3f;
            motor.Step(Dt, Move(-1f), Ground);
            Assert.That(motor.Velocity.x, Is.EqualTo(3f));
        }

        [Test]
        public void Knockback_OverridesVelocity_AndLocksControl()
        {
            var motor = new PlatformerMotor(Settings());
            motor.ApplyKnockback(new Vector2(-6f, 5f), 0.2f);
            motor.Step(Dt, Move(1f), Air);
            Assert.That(motor.Velocity.x, Is.EqualTo(-6f).Within(1e-4f), "input ignored during hit-stun");
        }
    }
}
