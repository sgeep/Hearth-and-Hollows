using System;
using UnityEngine;

namespace Hearthdelve.Core.Movement
{
    /// <summary>
    /// Pure stepping rule for the player's pixel-snapped presentation
    /// (Shared/Engine/PixelSnappedPresentation): moves a display position, held on the art-pixel
    /// grid, after a free-moving target, like a Bresenham line along the direction of movement.
    /// - Moving along one axis (or standing still): each axis steps once it is half a pixel behind;
    ///   whole pixels are always taken.
    /// - Moving diagonally: the faster (lead) axis steps the same way, and the other (follow) axis
    ///   only steps on the lead axis's frames, once it is a quarter of a pixel behind in the direction
    ///   it is moving, and never by more than the lead axis. Both axes then step together, so the
    ///   world scrolls in clean diagonal steps instead of a zig-zag, whatever sub-pixel phase the
    ///   movement started at. The follow axis only steps on its own once 1.25 px behind.
    /// - A fractional step back against an axis's last step needs 0.75 px, so a slow axis never
    ///   steps back and forth.
    /// - The lead axis only changes when the other axis is clearly faster (10%), so movement at
    ///   exactly 45 degrees does not flip the lead on float noise and break the lockstep.
    /// The display stays within 1.25 px of the target (within 0.75 px when not moving diagonally).
    /// </summary>
    public static class PixelStepping
    {
        public const float MustStep = 0.5f;
        public const float FollowStep = 0.25f;
        public const float ReverseStep = 0.75f;
        public const float FollowAlone = 1.25f;
        /// <summary>How much faster the other axis must move to take the lead.</summary>
        public const float LeadSwitch = 1.1f;
        /// <summary>Per-frame movement below this many pixels counts as not moving on that axis.</summary>
        public const float Still = 0.001f;
        /// <summary>Beyond this many pixels the display jumps straight to the target (teleport, respawn).</summary>
        public const float Resync = 8f;

        /// <summary>What the rule remembers between frames.</summary>
        public struct State
        {
            /// <summary>Sign of each axis's last step.</summary>
            public Vector2Int LastStep;
            /// <summary>Lead axis during diagonal movement: 0 none, 1 X, 2 Y.</summary>
            public int Lead;
        }

        /// <summary>
        /// The next display position, in whole art pixels, for a target given in art pixels.
        /// <paramref name="movement"/> is how far the target moved this frame (art pixels).
        /// </summary>
        public static Vector2Int Step(Vector2Int display, Vector2 target, Vector2 movement, ref State state)
        {
            Vector2 delta = target - display;
            if (Math.Abs(delta.x) > Resync || Math.Abs(delta.y) > Resync)
            {
                state = default;
                return Vector2Int.RoundToInt(target);
            }

            int stepX, stepY;
            float moveX = Math.Abs(movement.x), moveY = Math.Abs(movement.y);
            if (moveX > Still && moveY > Still)
            {
                bool xLeads = state.Lead switch
                {
                    1 => !(moveY > moveX * LeadSwitch),
                    2 => moveX > moveY * LeadSwitch,
                    _ => moveX >= moveY,
                };
                state.Lead = xLeads ? 1 : 2;

                float leadDelta = xLeads ? delta.x : delta.y, followDelta = xLeads ? delta.y : delta.x;
                float followMovement = xLeads ? movement.y : movement.x;

                int lead = AxisStep(leadDelta, xLeads ? state.LastStep.x : state.LastStep.y);
                int follow = lead != 0
                    ? FollowAxisStep(followDelta, followMovement, Math.Abs(lead))
                    : Math.Abs(followDelta) >= FollowAlone ? Math.Sign(followDelta) : 0;

                stepX = xLeads ? lead : follow;
                stepY = xLeads ? follow : lead;
            }
            else
            {
                state.Lead = 0;
                stepX = AxisStep(delta.x, state.LastStep.x);
                stepY = AxisStep(delta.y, state.LastStep.y);
            }

            if (stepX != 0) state.LastStep.x = Math.Sign(stepX);
            if (stepY != 0) state.LastStep.y = Math.Sign(stepY);
            return new Vector2Int(display.x + stepX, display.y + stepY);
        }

        /// <summary>Whole pixels, plus one more once half a pixel behind (0.75 px against the last step).</summary>
        static int AxisStep(float delta, int lastStep)
        {
            int step = (int)Math.Truncate(delta);
            float rest = delta - step;
            float threshold = lastStep != 0 && Math.Sign(rest) == -lastStep ? ReverseStep : MustStep;
            if (Math.Abs(rest) >= threshold) step += Math.Sign(rest);
            return step;
        }

        /// <summary>The follow axis on a lead-axis frame: a quarter pixel behind in its direction of movement, capped at the lead step.</summary>
        static int FollowAxisStep(float delta, float movement, int cap)
        {
            int step = (int)Math.Truncate(delta);
            float rest = delta - step;
            if (Math.Abs(rest) >= FollowStep && Math.Sign(rest) == Math.Sign(movement)) step += Math.Sign(rest);
            return Math.Clamp(step, -cap, cap);
        }
    }
}
