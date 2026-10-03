using System;
using System.Collections.Generic;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>
    /// A* over a <see cref="GridMap"/> with 8-direction movement. Diagonal steps are only
    /// allowed when both cells they pass between are walkable, so nothing cuts a corner.
    /// Rooms are small (about 40×22 tiles), so a simple open list is enough.
    /// </summary>
    public static class GridPathfinder
    {
        const int k_StraightCost = 10;
        const int k_DiagonalCost = 14;

        static readonly (int dx, int dy)[] k_Neighbours =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        /// <summary>
        /// Finds the cheapest path from <paramref name="start"/> to <paramref name="goal"/>.
        /// On success <paramref name="path"/> holds every cell from start to goal inclusive.
        /// Returns false (and clears the path) when either end is blocked or unreachable.
        /// </summary>
        public static bool TryFindPath(GridMap map, GridCell start, GridCell goal, List<GridCell> path)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();
            if (!map.IsWalkable(start) || !map.IsWalkable(goal)) return false;
            if (start == goal)
            {
                path.Add(start);
                return true;
            }

            int cellCount = map.Width * map.Height;
            var cost = new int[cellCount];
            var cameFrom = new int[cellCount];
            var closed = new bool[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                cost[i] = int.MaxValue;
                cameFrom[i] = -1;
            }

            var open = new List<(int priority, int order, GridCell cell)>();
            int order = 0;
            cost[map.Index(start)] = 0;
            open.Add((Heuristic(start, goal), order++, start));

            while (open.Count > 0)
            {
                GridCell current = PopCheapest(open);
                int currentIndex = map.Index(current);
                if (closed[currentIndex]) continue;
                closed[currentIndex] = true;

                if (current == goal)
                {
                    Rebuild(map, cameFrom, start, goal, path);
                    return true;
                }

                foreach (var (dx, dy) in k_Neighbours)
                {
                    var next = new GridCell(current.X + dx, current.Y + dy);
                    if (!map.IsWalkable(next)) continue;
                    bool diagonal = dx != 0 && dy != 0;
                    if (diagonal &&
                        (!map.IsWalkable(new GridCell(current.X + dx, current.Y)) ||
                         !map.IsWalkable(new GridCell(current.X, current.Y + dy))))
                        continue;

                    int nextIndex = map.Index(next);
                    if (closed[nextIndex]) continue;
                    int newCost = cost[currentIndex] + (diagonal ? k_DiagonalCost : k_StraightCost);
                    if (newCost >= cost[nextIndex]) continue;

                    cost[nextIndex] = newCost;
                    cameFrom[nextIndex] = currentIndex;
                    open.Add((newCost + Heuristic(next, goal), order++, next));
                }
            }

            return false;
        }

        /// <summary>Path length in tiles (a diagonal step counts as √2).</summary>
        public static float Length(IReadOnlyList<GridCell> path)
        {
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                bool diagonal = path[i].X != path[i - 1].X && path[i].Y != path[i - 1].Y;
                length += diagonal ? 1.41421356f : 1f;
            }
            return length;
        }

        // Octile distance: exact for 8-direction movement on an open grid.
        static int Heuristic(GridCell a, GridCell b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return k_StraightCost * (dx + dy) + (k_DiagonalCost - 2 * k_StraightCost) * Math.Min(dx, dy);
        }

        // Ties go to the earliest-added entry so results are deterministic.
        static GridCell PopCheapest(List<(int priority, int order, GridCell cell)> open)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].priority < open[best].priority ||
                    (open[i].priority == open[best].priority && open[i].order < open[best].order))
                    best = i;
            }
            GridCell cell = open[best].cell;
            open[best] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            return cell;
        }

        static void Rebuild(GridMap map, int[] cameFrom, GridCell start, GridCell goal, List<GridCell> path)
        {
            int index = map.Index(goal);
            int startIndex = map.Index(start);
            while (index != startIndex)
            {
                path.Add(new GridCell(index % map.Width, index / map.Width));
                index = cameFrom[index];
            }
            path.Add(start);
            path.Reverse();
        }
    }
}
