using MoreMountains.TopDownEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// TDE's 2D controller for our floors. TDE counts a character as grounded only over a collider on the
    /// Ground layer, and our rooms have none: every floor is walkable and nothing falls (no gravity). Without
    /// this, characters sat in TDE's Falling state for good, never Walking, so anything reading the movement
    /// state (the sprite animator's walk) never saw them walk. Grounded everywhere except over a hole.
    /// </summary>
    public sealed class FloorController2D : TopDownController2D
    {
        protected override void CheckIfGrounded()
        {
            bool wasGrounded = _groundedLastFrame;
            base.CheckIfGrounded();
            Grounded = !OverHole;
            JustGotGrounded = Grounded && !wasGrounded;
            _groundedLastFrame = Grounded;
        }
    }
}
