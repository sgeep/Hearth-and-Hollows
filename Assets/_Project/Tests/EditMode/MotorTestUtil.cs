using Hearthdelve.Dungeon.Player;

namespace Hearthdelve.Tests
{
    static class MotorTestUtil
    {
        public const float Dt = 1f / 60f;

        public static readonly MotorCollisions Ground = new() { Grounded = true };
        public static readonly MotorCollisions OneWay = new() { Grounded = true, OnOneWay = true };
        public static readonly MotorCollisions Air = new();
        public static readonly MotorCollisions WallRight = new() { WallRight = true };
        public static readonly MotorCollisions WallLeft = new() { WallLeft = true };

        public static MotorInput None => new();
        public static MotorInput Jump => new() { JumpPressed = true, JumpHeld = true };
        public static MotorInput Hold => new() { JumpHeld = true };
        public static MotorInput Dodge => new() { DodgePressed = true };
        public static MotorInput Move(float x) => new() { MoveX = x };

        public static void Run(PlatformerMotor motor, int steps, MotorInput input, MotorCollisions contacts)
        {
            for (int i = 0; i < steps; i++) motor.Step(Dt, input, contacts);
        }

        /// <summary>Default settings with no randomness in the numbers the tests rely on.</summary>
        public static MovementSettings Settings() => new()
        {
            coyoteTime = 0.1f,
            jumpBufferTime = 0.12f,
            dodgeDuration = 0.3f,
            dodgeDistance = 4.5f,
            dodgeIFrameStart = 0.05f,
            dodgeIFrameEnd = 0.25f,
            dodgeCooldown = 0.35f,
            dropThroughTime = 0.25f,
        };
    }
}
