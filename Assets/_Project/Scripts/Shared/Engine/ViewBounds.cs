using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>Keeps a camera view inside a rectangle (4h, the village; the same rule as the dungeon's rooms). Pure.</summary>
    public static class ViewBounds
    {
        /// <summary>
        /// The centre of a view of the given half-size kept inside <paramref name="bounds"/>; on an axis where the view is
        /// larger than the bounds, it centres on them.
        /// </summary>
        public static Vector2 Clamp(Rect bounds, Vector2 position, float halfWidth, float halfHeight) =>
            new(Axis(position.x, bounds.xMin, bounds.xMax, halfWidth), Axis(position.y, bounds.yMin, bounds.yMax, halfHeight));

        static float Axis(float value, float min, float max, float half) =>
            max - min <= half * 2f ? (min + max) / 2f : Mathf.Clamp(value, min + half, max - half);
    }
}
