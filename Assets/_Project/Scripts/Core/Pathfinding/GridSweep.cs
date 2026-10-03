using UnityEngine;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>
    /// Whether a character's collision box can slide in a straight line without touching a
    /// blocked cell. Checking the centre line alone is not enough: a box cuts corners its centre
    /// clears. The box is sampled along the segment and padded by half the sample spacing, so a
    /// clear answer is never wrong (it may be cautious near walls).
    /// </summary>
    public static class GridSweep
    {
        public const float SampleSpacing = 0.1f;

        public static bool IsClear(GridMap map, GridSpace space, Vector2 from, Vector2 to, Vector2 halfExtents)
        {
            Vector2 half = halfExtents + Vector2.one * (SampleSpacing * 0.5f);
            float distance = Vector2.Distance(from, to);
            int samples = Mathf.Max(1, Mathf.CeilToInt(distance / SampleSpacing));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 centre = Vector2.Lerp(from, to, i / (float)samples);
                if (!IsBoxClear(map, space, centre, half)) return false;
            }
            return true;
        }

        /// <summary>True when every cell the box overlaps is walkable. Touching a cell's edge is not overlapping it.</summary>
        public static bool IsBoxClear(GridMap map, GridSpace space, Vector2 centre, Vector2 halfExtents)
        {
            const float edge = 1e-4f;
            GridCell min = space.ToCell(centre - halfExtents + Vector2.one * edge);
            GridCell max = space.ToCell(centre + halfExtents - Vector2.one * edge);
            for (int y = min.Y; y <= max.Y; y++)
            for (int x = min.X; x <= max.X; x++)
                if (!map.IsWalkable(new GridCell(x, y)))
                    return false;
            return true;
        }
    }
}
