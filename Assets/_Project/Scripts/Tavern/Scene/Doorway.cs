using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// When the keeper is going through a doorway (4h, after the owner's Checkpoint B playtest): standing in its trigger and
    /// pushing the way through, by the stick or keys or by moving. Pushing counts even when a wall or the stairs stop the keeper,
    /// so there's no exact line to walk; and someone just arrived, walking away from the door they came out of, never bounces
    /// back. A doorway with no direction goes through on stepping in (the old rule).
    /// </summary>
    public static class Doorway
    {
        /// <summary>How firmly the keeper must push the way through (the stick's or keys' share along the direction).</summary>
        public const float PushThreshold = 0.3f;

        public static bool GoingThrough(Rigidbody2D keeper, Vector2 through, bool entering, ref Vector2 lastPosition, ref bool tracked)
        {
            Vector2 position = keeper.position;
            Vector2 moved = tracked ? position - lastPosition : Vector2.zero;
            lastPosition = position;
            tracked = true;
            if (through == Vector2.zero) return entering;
            Vector2 way = through.normalized;
            Vector2 push = keeper.TryGetComponent(out Character character) && character.LinkedInputManager != null
                ? character.LinkedInputManager.PrimaryMovement
                : Vector2.zero;
            return Vector2.Dot(push, way) > PushThreshold || Vector2.Dot(moved, way) > 0.001f;
        }
    }
}
