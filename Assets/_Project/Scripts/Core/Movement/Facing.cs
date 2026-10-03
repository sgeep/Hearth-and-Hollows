using System;

namespace Hearthdelve.Core.Movement
{
    /// <summary>
    /// The four facings Minifantasy characters are drawn in. "Front" faces the camera
    /// (moving down the screen); "Back" faces away (moving up).
    /// </summary>
    public enum Facing4
    {
        FrontRight,
        FrontLeft,
        BackRight,
        BackLeft,
    }

    /// <summary>
    /// Maps 8-direction movement or aim onto the four drawn facings. An axis with no clear
    /// input keeps its previous side, so walking straight up, down or sideways doesn't flip
    /// the sprite.
    /// </summary>
    public static class FacingLogic
    {
        public static Facing4 FromDirection(float x, float y, Facing4 current, float deadZone = 0.2f)
        {
            bool right = IsRight(current);
            bool front = IsFront(current);
            if (Math.Abs(x) > deadZone) right = x > 0f;
            if (Math.Abs(y) > deadZone) front = y < 0f;
            return Combine(front, right);
        }

        public static bool IsRight(Facing4 facing) => facing is Facing4.FrontRight or Facing4.BackRight;
        public static bool IsFront(Facing4 facing) => facing is Facing4.FrontRight or Facing4.FrontLeft;

        public static Facing4 Combine(bool front, bool right) =>
            front
                ? (right ? Facing4.FrontRight : Facing4.FrontLeft)
                : (right ? Facing4.BackRight : Facing4.BackLeft);
    }
}
