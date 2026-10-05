using System.Collections.Generic;
using Hearthdelve.Core.Pathfinding;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    public enum LayoutIssueKind
    {
        /// <summary>Nobody can get in from the door.</summary>
        EntranceBlocked,
        /// <summary>A required station isn't out (stored).</summary>
        StationMissing,
        /// <summary>A station is out but nobody can reach it.</summary>
        StationUnreachable,
        PassMissing,
        PassUnreachable,
        /// <summary>No chair faces a table.</summary>
        NoSeats,
        /// <summary>Seats nobody can reach (<see cref="LayoutIssue.Count"/> of them).</summary>
        SeatsUnreachable,
        /// <summary>Staff can't get from where they rest to the pass or a station.</summary>
        StaffCantReach,
        /// <summary>Furniture stands where customers queue by the door (<see cref="LayoutIssue.Count"/> spots).</summary>
        QueueBlocked,
    }

    /// <summary>One problem the layout check found. Blocking problems keep the doors shut (D13); the rest are warnings.</summary>
    public readonly struct LayoutIssue
    {
        public readonly LayoutIssueKind Kind;
        public readonly bool Blocking;
        public readonly StationKind Station;
        public readonly int Count;
        /// <summary>The piece concerned (for jumping to it), or -1.</summary>
        public readonly int Uid;

        public LayoutIssue(LayoutIssueKind kind, bool blocking, StationKind station = StationKind.None, int count = 0, int uid = -1)
        {
            Kind = kind;
            Blocking = blocking;
            Station = station;
            Count = count;
            Uid = uid;
        }
    }

    public sealed class LayoutReport
    {
        public readonly List<LayoutIssue> Issues = new();
        public int Seats;
        public int ReachableSeats;

        public bool CanOpen
        {
            get
            {
                foreach (LayoutIssue i in Issues)
                    if (i.Blocking) return false;
                return true;
            }
        }

        public static readonly LayoutReport Clear = new();
    }

    /// <summary>
    /// Checks a layout against what service needs, not against the authored room (D13; 4f plan §6): only what makes
    /// service impossible blocks opening (no way in, a required station or the pass missing or unreachable, no seat
    /// anyone can reach); partial problems are warnings, and the pieces concerned simply go unused. The walkable grid
    /// is built here from the pieces' bodies with the same rule <c>NavGrid</c> bakes by, so the check and the room agree
    /// (a PlayMode test holds them to it). Pure logic.
    /// </summary>
    public static class LayoutCheck
    {
        /// <summary>The stations a tavern evening needs (4f: one of each, D6).</summary>
        public static readonly StationKind[] RequiredStations = { StationKind.Grill, StationKind.Tap, StationKind.StewPot };

        /// <summary>NavGrid's overlap probe: a cell is blocked when a body overlaps the middle 0.9 of it, even partly.</summary>
        const float k_Probe = 0.45f;
        /// <summary>Physics2D's default contact offset: shapes count as a hair larger in overlap queries.</summary>
        const float k_Skin = 0.01f;

        /// <summary>The area's walkable cells: its floor, minus every cell a standing piece's body overlaps.</summary>
        public static GridMap Walkable(AreaShape shape, IEnumerable<ResolvedFurniture> pieces)
        {
            var map = new GridMap(Mathf.Max(1, shape.Bounds.width), Mathf.Max(1, shape.Bounds.height));
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                map.SetBlocked(x, y, !shape.Floor.Contains(new Vector2Int(x + shape.Bounds.xMin, y + shape.Bounds.yMin)));
            foreach (Rect fixture in shape.Fixtures) Block(map, shape, fixture);
            foreach (ResolvedFurniture p in pieces)
            {
                if (!p.Definition.BlocksMovement) continue;
                foreach (Rect body in p.Bodies) Block(map, shape, body);
            }
            return map;
        }

        static void Block(GridMap map, AreaShape shape, Rect body)
        {
            Vector2 min = body.min - shape.Origin - (Vector2)shape.Bounds.min, max = body.max - shape.Origin - (Vector2)shape.Bounds.min;
            int x0 = Mathf.FloorToInt(min.x - 1f), x1 = Mathf.CeilToInt(max.x + 1f), y0 = Mathf.FloorToInt(min.y - 1f), y1 = Mathf.CeilToInt(max.y + 1f);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(map.Height - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(map.Width - 1, x1); x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                if (min.x - k_Skin < cx + k_Probe && max.x + k_Skin > cx - k_Probe && min.y - k_Skin < cy + k_Probe && max.y + k_Skin > cy - k_Probe)
                    map.SetBlocked(x, y, true);
            }
        }

        /// <summary>Every cell reachable on foot from <paramref name="from"/> (no corner cutting, as <see cref="GridPathfinder"/>).</summary>
        public static bool[,] Reachable(GridMap map, GridCell from)
        {
            var seen = new bool[map.Width, map.Height];
            if (!map.IsWalkable(from)) return seen;
            var open = new Queue<GridCell>();
            open.Enqueue(from);
            seen[from.X, from.Y] = true;
            while (open.Count > 0)
            {
                GridCell c = open.Dequeue();
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var n = new GridCell(c.X + dx, c.Y + dy);
                    if (!map.IsWalkable(n) || seen[n.X, n.Y]) continue;
                    seen[n.X, n.Y] = true;
                    open.Enqueue(n);
                }
            }
            return seen;
        }

        static GridCell Cell(AreaShape shape, Vector2 world)
        {
            Vector2Int c = Vector2Int.FloorToInt(world - shape.Origin) - shape.Bounds.min;
            return new GridCell(c.x, c.y);
        }

        static bool At(bool[,] reach, GridCell c) => c.X >= 0 && c.Y >= 0 && c.X < reach.GetLength(0) && c.Y < reach.GetLength(1) && reach[c.X, c.Y];

        /// <summary>Whether someone standing on a reachable cell is within <paramref name="reach"/> of a use point.</summary>
        static bool CanUse(AreaShape shape, bool[,] reach, ResolvedFurniture piece)
        {
            int w = reach.GetLength(0), h = reach.GetLength(1);
            foreach (Vector2 use in piece.UsePoints)
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!reach[x, y]) continue;
                    Vector2 centre = shape.Origin + shape.Bounds.min + new Vector2(x + 0.5f, y + 0.5f);
                    if (Vector2.Distance(centre, use) <= piece.Definition.reach) return true;
                }
            return false;
        }

        /// <summary>Whether a point's cell can be walked to from the area's door (seat approaches are chosen by it).</summary>
        public static System.Func<Vector2, bool> ReachableFromDoor(AreaShape shape, IReadOnlyList<ResolvedFurniture> pieces)
        {
            GridMap map = Walkable(shape, pieces);
            bool[,] fromDoor = Reachable(map, Cell(shape, shape.Door));
            return p => At(fromDoor, Cell(shape, p));
        }

        /// <summary>The tavern's check: can an evening's service run in this layout?</summary>
        public static LayoutReport Tavern(AreaShape shape, IReadOnlyList<ResolvedFurniture> pieces)
        {
            var report = new LayoutReport();
            GridMap map = Walkable(shape, pieces);
            bool[,] fromDoor = Reachable(map, Cell(shape, shape.Door));
            if (!map.IsWalkable(Cell(shape, shape.Door)))
            {
                report.Issues.Add(new LayoutIssue(LayoutIssueKind.EntranceBlocked, true));
                return report;
            }

            ResolvedFurniture pass = null;
            var stations = new Dictionary<StationKind, ResolvedFurniture>();
            foreach (ResolvedFurniture p in pieces)
            {
                if (p.Definition.function == FurnitureFunction.Pass && pass == null) pass = p;
                if (p.Definition.function == FurnitureFunction.Station && !stations.ContainsKey(p.Definition.station)) stations[p.Definition.station] = p;
            }

            foreach (StationKind kind in RequiredStations)
            {
                if (!stations.TryGetValue(kind, out ResolvedFurniture station)) report.Issues.Add(new LayoutIssue(LayoutIssueKind.StationMissing, true, kind));
                else if (!CanUse(shape, fromDoor, station)) report.Issues.Add(new LayoutIssue(LayoutIssueKind.StationUnreachable, true, kind, uid: station.Placement.uid));
            }
            if (pass == null) report.Issues.Add(new LayoutIssue(LayoutIssueKind.PassMissing, true));
            else if (!CanUse(shape, fromDoor, pass)) report.Issues.Add(new LayoutIssue(LayoutIssueKind.PassUnreachable, true, uid: pass.Placement.uid));

            var seats = FurnitureRules.UsableSeats(pieces, p => At(fromDoor, Cell(shape, p)));
            report.Seats = seats.Count;
            int firstUnreached = -1;
            foreach (var (piece, seat) in seats)
            {
                if (At(fromDoor, Cell(shape, seat.Approach))) report.ReachableSeats++;
                else if (firstUnreached < 0) firstUnreached = piece.Placement.uid;
            }
            if (seats.Count == 0) report.Issues.Add(new LayoutIssue(LayoutIssueKind.NoSeats, true));
            else if (report.ReachableSeats == 0) report.Issues.Add(new LayoutIssue(LayoutIssueKind.SeatsUnreachable, true, count: seats.Count, uid: firstUnreached));
            else if (report.ReachableSeats < seats.Count)
                report.Issues.Add(new LayoutIssue(LayoutIssueKind.SeatsUnreachable, false, count: seats.Count - report.ReachableSeats, uid: firstUnreached));

            // Staff walk from where they rest; a job they can't reach leaves them idle.
            bool[,] fromRest = Reachable(map, Cell(shape, shape.Rest));
            foreach (ResolvedFurniture work in stations.Values)
                if (CanUse(shape, fromDoor, work) && !CanUse(shape, fromRest, work))
                {
                    report.Issues.Add(new LayoutIssue(LayoutIssueKind.StaffCantReach, false, work.Definition.station, uid: work.Placement.uid));
                    break;
                }
            if (pass != null && CanUse(shape, fromDoor, pass) && !CanUse(shape, fromRest, pass) && !report.Issues.Exists(i => i.Kind == LayoutIssueKind.StaffCantReach))
                report.Issues.Add(new LayoutIssue(LayoutIssueKind.StaffCantReach, false, uid: pass.Placement.uid));

            int queueBlocked = 0;
            foreach (Vector2 spot in shape.Queue)
                if (!map.IsWalkable(Cell(shape, spot))) queueBlocked++;
            if (queueBlocked > 0) report.Issues.Add(new LayoutIssue(LayoutIssueKind.QueueBlocked, false, count: queueBlocked));
            return report;
        }

        /// <summary>A guest room's check (4f step 6): only that its doorway can be reached.</summary>
        public static LayoutReport GuestRoom(AreaShape shape, IReadOnlyList<ResolvedFurniture> pieces)
        {
            var report = new LayoutReport();
            GridMap map = Walkable(shape, pieces);
            if (!map.IsWalkable(Cell(shape, shape.Door))) report.Issues.Add(new LayoutIssue(LayoutIssueKind.EntranceBlocked, false));
            return report;
        }

        public static LayoutReport For(AreaShape shape, IReadOnlyList<ResolvedFurniture> pieces) =>
            shape.Kind == AreaKind.Tavern ? Tavern(shape, pieces) : GuestRoom(shape, pieces);
    }
}
