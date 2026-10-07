using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Core.Presentation
{
    /// <summary>
    /// The screen's spoken-for areas (2026-10-07): HUD elements register the screen rectangle they cover (in pixels, origin bottom
    /// left), and world signs that must stay readable (a door's reward sign) step down out of them. Gameplay and the UI never
    /// reference each other; they meet here. The maths is pure (<see cref="DropToClear"/>).
    /// </summary>
    public static class ScreenReservations
    {
        static readonly Dictionary<object, Func<Rect?>> s_Areas = new();

        /// <summary>Registers (or replaces) an owner's area; the function returns null while the owner isn't showing.</summary>
        public static void Add(object owner, Func<Rect?> screenRect)
        {
            if (owner != null && screenRect != null) s_Areas[owner] = screenRect;
        }

        public static void Remove(object owner)
        {
            if (owner != null) s_Areas.Remove(owner);
        }

        /// <summary>The areas spoken for right now.</summary>
        public static List<Rect> Current()
        {
            var areas = new List<Rect>();
            foreach (Func<Rect?> area in s_Areas.Values)
                if (area() is { } r && r.width > 0f && r.height > 0f) areas.Add(r);
            return areas;
        }

        /// <summary>
        /// How far (pixels) <paramref name="item"/> must move down to clear every area it overlaps by more than touching, with
        /// <paramref name="margin"/> pixels to spare. Moving down can meet another area beneath; that's cleared too. 0 when clear.
        /// </summary>
        public static float DropToClear(Rect item, IReadOnlyList<Rect> areas, float margin = 2f)
        {
            float drop = 0f;
            for (int pass = 0; pass < 8; pass++)
            {
                Rect moved = item;
                moved.y -= drop;
                float more = 0f;
                foreach (Rect area in areas)
                {
                    bool across = moved.xMax > area.xMin + 0.5f && moved.xMin < area.xMax - 0.5f;
                    bool down = moved.yMax > area.yMin - margin + 0.5f && moved.yMin < area.yMax - 0.5f;
                    if (across && down) more = Mathf.Max(more, moved.yMax - (area.yMin - margin));
                }
                if (more <= 0f) return drop;
                drop += more;
            }
            return drop;
        }
    }
}
