using System;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>A cell on a room's tile grid. 1 cell = 1 tile = 1 world unit.</summary>
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int X;
        public readonly int Y;

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(GridCell a, GridCell b) => a.Equals(b);
        public static bool operator !=(GridCell a, GridCell b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// Walkability of a rectangular room, built from its tilemap. Cells outside the
    /// rectangle are never walkable.
    /// </summary>
    public sealed class GridMap
    {
        readonly bool[] m_Blocked;

        public GridMap(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            m_Blocked = new bool[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        public bool InBounds(GridCell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

        public bool IsWalkable(GridCell cell) => InBounds(cell) && !m_Blocked[Index(cell)];

        public void SetBlocked(GridCell cell, bool blocked)
        {
            if (!InBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell));
            m_Blocked[Index(cell)] = blocked;
        }

        public void SetBlocked(int x, int y, bool blocked) => SetBlocked(new GridCell(x, y), blocked);

        /// <summary>
        /// The walkable cell nearest to <paramref name="cell"/> (itself if walkable), searching
        /// outwards ring by ring up to <paramref name="maxRadius"/>. A character pressed against a
        /// prop can stand in a cell the prop partly covers; paths start from the nearest open one.
        /// </summary>
        public bool TryFindNearestWalkable(GridCell cell, int maxRadius, out GridCell nearest)
        {
            nearest = cell;
            if (IsWalkable(cell)) return true;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                int best = int.MaxValue;
                for (int y = cell.Y - radius; y <= cell.Y + radius; y++)
                for (int x = cell.X - radius; x <= cell.X + radius; x++)
                {
                    if (Math.Max(Math.Abs(x - cell.X), Math.Abs(y - cell.Y)) != radius) continue;
                    var candidate = new GridCell(x, y);
                    if (!IsWalkable(candidate)) continue;
                    int distance = (x - cell.X) * (x - cell.X) + (y - cell.Y) * (y - cell.Y);
                    if (distance >= best) continue;
                    best = distance;
                    nearest = candidate;
                }
                if (best != int.MaxValue) return true;
            }
            return false;
        }

        internal int Index(GridCell cell) => cell.Y * Width + cell.X;
    }
}
