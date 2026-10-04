using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>A doorway in a room's wall: two tiles wide, cut through the wall's two rows.</summary>
    public readonly struct RoomSocket
    {
        /// <summary>The doorway's tiles (world tile coordinates, x right and y up, the room's bottom-left at 0, 0).</summary>
        public readonly RectInt Cells;

        public RoomSocket(RectInt cells) => Cells = cells;

        /// <summary>The middle of the doorway's floor edge, on the room side.</summary>
        public Vector2 Threshold(bool north) => new(Cells.x + Cells.width / 2f, north ? Cells.y : Cells.yMax);
    }

    /// <summary>
    /// An authored room (4d), parsed from rows of characters and checked. Pure logic: the editor builds room prefabs
    /// from it, and tests validate every room with it.
    /// <list type="bullet">
    /// <item><c>#</c> wall, <c>t</c> wall with a torch, <c>.</c> floor, <c>o</c> pillar (two rows: top over base).</item>
    /// <item>Props: <c>c</c> crate, <c>b</c> barrel, <c>B</c> open barrel, <c>u</c> cauldron, <c>s</c> statue, <c>T</c> table (two tiles: T and the tile to its right).</item>
    /// <item>Fixed enemies: <c>S</c> green slime, <c>V</c> bat (asleep; must hang directly under a wall), <c>X</c> giant spider.</item>
    /// <item>Spawn points for the encounter chosen when the run is generated (4d step 2): <c>m</c> on the ground, <c>v</c> a
    /// perch for a bat (directly under a wall).</item>
    /// <item><c>P</c> where the player arrives; <c>R</c> the rope out; <c>H</c> the hole down to the next floor.</item>
    /// <item><c>x</c> an exit: a gated doorway through the north wall, two tiles wide and both wall rows tall.</item>
    /// <item><c>e</c> the entrance: a doorway through the south wall, the same size, where the player came in (one way).</item>
    /// </list>
    /// The top row of text is the north wall. Rooms are 1–1.5 screens (a screen is 40×22.5 tiles at 320×180).
    /// </summary>
    public sealed class RoomLayout
    {
        public const int MinWidth = 40, MaxWidth = 60, MinHeight = 23, MaxHeight = 34;
        /// <summary>
        /// The HUD's top-left corner (the Essence bar and its icon, about 8 tiles across) covers the north wall's first
        /// columns whenever the camera is at the room's west edge, so a bat asleep there can't be seen (the step 2
        /// playtest): perches start at this column.
        /// </summary>
        public const int FirstPerchColumn = 9;
        const string k_Known = "#t.oScbBusTVXPRxemvH";

        readonly string[] m_Rows;
        readonly List<string> m_Problems = new();
        readonly List<RoomSocket> m_Exits = new();
        readonly List<(char kind, Vector2Int cell)> m_Enemies = new();
        readonly List<Vector2Int> m_GroundSpawns = new();
        readonly List<Vector2Int> m_PerchSpawns = new();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<RoomSocket> Exits => m_Exits;
        public RoomSocket? Entrance { get; private set; }
        /// <summary>Where the player arrives (the <c>P</c> tile).</summary>
        public Vector2Int? Arrival { get; private set; }
        public IReadOnlyList<(char kind, Vector2Int cell)> Enemies => m_Enemies;
        public Vector2Int? Rope { get; private set; }
        /// <summary>The hole down to the next floor (the <c>H</c> tile).</summary>
        public Vector2Int? Hole { get; private set; }
        /// <summary>Ground spawn points (<c>m</c>), bottom row first, left to right.</summary>
        public IReadOnlyList<Vector2Int> GroundSpawns => m_GroundSpawns;
        /// <summary>Bat perches (<c>v</c>), bottom row first, left to right.</summary>
        public IReadOnlyList<Vector2Int> PerchSpawns => m_PerchSpawns;
        /// <summary>What's wrong with the layout; empty when it's valid.</summary>
        public IReadOnlyList<string> Problems => m_Problems;
        public bool IsValid => m_Problems.Count == 0;

        public RoomLayout(IReadOnlyList<string> rows)
        {
            m_Rows = rows == null ? Array.Empty<string>() : new List<string>(rows).ToArray();
            Height = m_Rows.Length;
            Width = Height > 0 ? m_Rows[0].Length : 0;
            Validate();
        }

        /// <summary>The character at a tile (x right, y up from the bottom-left); outside the room is wall.</summary>
        public char At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height || m_Rows[Height - 1 - y].Length <= x ? '#' : m_Rows[Height - 1 - y][x];

        public static bool IsWall(char c) => c is '#' or 't';
        public static bool IsProp(char c) => c is 'c' or 'b' or 'B' or 'u' or 's' or 'T';
        public static bool IsEnemy(char c) => c is 'S' or 'V' or 'X';
        public static bool IsDoorway(char c) => c is 'x' or 'e';

        /// <summary>Whether a character can stand on the tile (tables cover the tile to the right of their T too).</summary>
        public bool IsWalkable(int x, int y)
        {
            char c = At(x, y);
            if (IsWall(c) || c == 'o' || IsProp(c)) return false;
            return At(x - 1, y) != 'T';
        }

        void Validate()
        {
            if (Height == 0 || Width == 0)
            {
                m_Problems.Add("the room is empty");
                return;
            }
            for (int r = 0; r < Height; r++)
            {
                if (m_Rows[r].Length != Width) m_Problems.Add($"row {r} is {m_Rows[r].Length} wide, not {Width}");
                foreach (char c in m_Rows[r])
                    if (k_Known.IndexOf(c) < 0) m_Problems.Add($"unknown character '{c}' in row {r}");
            }
            if (Width < MinWidth || Width > MaxWidth || Height < MinHeight || Height > MaxHeight)
                m_Problems.Add($"{Width}×{Height} is outside {MinWidth}–{MaxWidth} × {MinHeight}–{MaxHeight} tiles (1–1.5 screens)");
            if (m_Problems.Count > 0) return;

            var arrivals = new List<Vector2Int>();
            var ropes = new List<Vector2Int>();
            var holes = new List<Vector2Int>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = At(x, y);
                bool border = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                if (border && !IsWall(c) && !IsDoorway(c)) m_Problems.Add($"the border is open at ({x}, {y})");
                if (c == 'P') arrivals.Add(new Vector2Int(x, y));
                if (c == 'R') ropes.Add(new Vector2Int(x, y));
                if (c == 'H') holes.Add(new Vector2Int(x, y));
                if (c == 'm') m_GroundSpawns.Add(new Vector2Int(x, y));
                if (c == 'v') m_PerchSpawns.Add(new Vector2Int(x, y));
                if (IsEnemy(c)) m_Enemies.Add((c, new Vector2Int(x, y)));
                if (c is 'V' or 'v' && !IsWall(At(x, y + 1))) m_Problems.Add($"the bat at ({x}, {y}) has no wall directly above it to hang from");
                if (c is 'V' or 'v' && x < FirstPerchColumn) m_Problems.Add($"the bat at ({x}, {y}) would sleep under the Essence bar (perches start at column {FirstPerchColumn})");
            }

            if (arrivals.Count != 1) m_Problems.Add($"there must be exactly one P (arrival), not {arrivals.Count}");
            else Arrival = arrivals[0];
            if (ropes.Count > 1) m_Problems.Add("there is more than one rope");
            else if (ropes.Count == 1) Rope = ropes[0];
            if (holes.Count > 1) m_Problems.Add("there is more than one hole");
            else if (holes.Count == 1) Hole = holes[0];

            foreach (RectInt cells in Doorways('x'))
            {
                if (cells.width != 2 || cells.height != 2 || cells.yMax != Height) m_Problems.Add($"exit at ({cells.x}, {cells.y}) must be 2×2 through the north wall");
                else m_Exits.Add(new RoomSocket(cells));
            }
            m_Exits.Sort((a, b) => a.Cells.x.CompareTo(b.Cells.x));
            List<RectInt> entrances = Doorways('e');
            if (entrances.Count != 1) m_Problems.Add($"there must be exactly one entrance (e), not {entrances.Count}");
            else if (entrances[0].width != 2 || entrances[0].height != 2 || entrances[0].y != 0) m_Problems.Add("the entrance must be 2×2 through the south wall");
            else Entrance = new RoomSocket(entrances[0]);
            if (m_Exits.Count == 0 && Rope == null && Hole == null) m_Problems.Add("a room needs an exit, the rope out or the hole down");

            if (Arrival is { } start) CheckReachable(start);
        }

        /// <summary>The doorways of one kind, as the bounding boxes of their connected tiles.</summary>
        List<RectInt> Doorways(char kind)
        {
            var found = new List<RectInt>();
            var seen = new HashSet<Vector2Int>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var cell = new Vector2Int(x, y);
                if (At(x, y) != kind || seen.Contains(cell)) continue;
                int minX = x, minY = y, maxX = x, maxY = y;
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(cell);
                seen.Add(cell);
                while (queue.Count > 0)
                {
                    Vector2Int c = queue.Dequeue();
                    minX = Math.Min(minX, c.x); maxX = Math.Max(maxX, c.x);
                    minY = Math.Min(minY, c.y); maxY = Math.Max(maxY, c.y);
                    foreach (Vector2Int n in Neighbours(c))
                        if (At(n.x, n.y) == kind && seen.Add(n)) queue.Enqueue(n);
                }
                found.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
            return found;
        }

        /// <summary>Everything the player must be able to reach from the arrival: each exit, the rope and every enemy.</summary>
        void CheckReachable(Vector2Int start)
        {
            var reached = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int n in Neighbours(c))
                    if (n.x >= 0 && n.y >= 0 && n.x < Width && n.y < Height && IsWalkable(n.x, n.y) && reached.Add(n))
                        queue.Enqueue(n);
            }
            foreach (RoomSocket exit in m_Exits)
                if (!reached.Contains(new Vector2Int(exit.Cells.x, exit.Cells.y)) && !reached.Contains(new Vector2Int(exit.Cells.x + 1, exit.Cells.y)))
                    m_Problems.Add($"the exit at ({exit.Cells.x}, {exit.Cells.y}) can't be reached from P");
            if (Rope is { } rope && !reached.Contains(rope)) m_Problems.Add("the rope can't be reached from P");
            if (Hole is { } hole && !reached.Contains(hole)) m_Problems.Add("the hole can't be reached from P");
            foreach (Vector2Int spawn in m_GroundSpawns)
                if (!reached.Contains(spawn)) m_Problems.Add($"the spawn point at ({spawn.x}, {spawn.y}) can't be reached from P");
            foreach (Vector2Int perch in m_PerchSpawns)
                if (!reached.Contains(perch)) m_Problems.Add($"the perch at ({perch.x}, {perch.y}) can't be reached from P");
            foreach (var (kind, cell) in m_Enemies)
                if (!reached.Contains(cell)) m_Problems.Add($"the {kind} at ({cell.x}, {cell.y}) can't be reached from P");
        }

        static IEnumerable<Vector2Int> Neighbours(Vector2Int c)
        {
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x - 1, c.y);
            yield return new Vector2Int(c.x, c.y + 1);
            yield return new Vector2Int(c.x, c.y - 1);
        }
    }
}
