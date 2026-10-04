using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>Where the camera may sit in a room (4d). Pure logic, used by <see cref="RoomCameraBounds"/>.</summary>
    public static class RoomView
    {
        /// <summary>
        /// The centre of a view of the given half-size, kept inside <paramref name="bounds"/>; on an axis where the view
        /// is larger than the room, it centres on the room.
        /// </summary>
        public static Vector2 Clamp(Rect bounds, Vector2 position, float halfWidth, float halfHeight) =>
            new(ClampAxis(position.x, bounds.xMin, bounds.xMax, halfWidth), ClampAxis(position.y, bounds.yMin, bounds.yMax, halfHeight));

        static float ClampAxis(float value, float min, float max, float half) =>
            max - min <= half * 2f ? (min + max) / 2f : Mathf.Clamp(value, min + half, max - half);
    }
}
