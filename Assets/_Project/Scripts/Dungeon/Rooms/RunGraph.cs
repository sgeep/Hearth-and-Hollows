using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>The Cellars' enemies the run can place at spawn points.</summary>
    public enum EnemyKind
    {
        Slime,
        Bat,
        Spider,
    }

    /// <summary>One enemy of a room's encounter: what it is and which spawn point (ground or perch, by kind) it stands on.</summary>
    public readonly struct EncounterSpawn
    {
        public readonly EnemyKind Kind;
        /// <summary>Index into the room's ground spawns (or its perches, for a bat).</summary>
        public readonly int Point;

        public EncounterSpawn(EnemyKind kind, int point)
        {
            Kind = kind;
            Point = point;
        }

        public override string ToString() => $"{Kind}@{Point}";
    }

    /// <summary>A room of a floor's graph: what it is for, which authored room it uses, its encounter and where its exits lead.</summary>
    public sealed class FloorNode
    {
        public int Id { get; internal set; }
        public RoomKind Kind { get; internal set; }
        /// <summary>Its step along the floor (0 = the floor's first room).</summary>
        public int Layer { get; internal set; }
        /// <summary>Its position across the step, left to right (exits lead to children in this order).</summary>
        public int Lane { get; internal set; }
        public string RoomId { get; internal set; }
        /// <summary>The nodes this room's exits lead to, left to right (exit 0 → Next[0]).</summary>
        public List<int> Next { get; } = new();
        public List<EncounterSpawn> Encounter { get; } = new();

        /// <summary>Where the run ends or leaves the floor: no exits.</summary>
        public bool IsEnd => Kind is RoomKind.Extraction or RoomKind.Descent or RoomKind.Arena;
    }

    /// <summary>One floor of a run: a one-way graph of rooms from its first room to its ends (extraction, descent or the arena).</summary>
    public sealed class FloorGraph
    {
        public FloorGraph(int floor, List<FloorNode> nodes)
        {
            Floor = floor;
            Nodes = nodes;
        }

        /// <summary>1, 2 or 3.</summary>
        public int Floor { get; }
        public IReadOnlyList<FloorNode> Nodes { get; }
        public FloorNode Start => Nodes[0];
        public FloorNode Node(int id) => Nodes[id];
    }

    /// <summary>A whole generated delve: its seed and its floors, top to bottom.</summary>
    public sealed class RunGraph
    {
        public RunGraph(int seed, IReadOnlyList<FloorGraph> floors)
        {
            Seed = seed;
            Floors = floors;
        }

        public int Seed { get; }
        public IReadOnlyList<FloorGraph> Floors { get; }

        /// <summary>The whole run as text (same seed and tuning → same text), for tests and bug reports.</summary>
        public string Describe()
        {
            var text = new StringBuilder($"seed {Seed}\n");
            foreach (FloorGraph floor in Floors)
            {
                text.Append($"floor {floor.Floor}\n");
                foreach (FloorNode node in floor.Nodes)
                    text.Append($"  {node.Id} L{node.Layer}.{node.Lane} {node.Kind} {node.RoomId} -> [{string.Join(",", node.Next)}] {{{string.Join(" ", node.Encounter)}}}\n");
            }
            return text.ToString();
        }

        /// <summary>The shortest and longest number of fights on a way from a floor's first room to its way on (descent or arena).</summary>
        public static (int shortest, int longest) FightsOnTheWayOn(FloorGraph floor)
        {
            int shortest = int.MaxValue, longest = 0;
            void Walk(FloorNode node, int fights)
            {
                if (node.Kind == RoomKind.Combat) fights++;
                if (node.Kind is RoomKind.Descent or RoomKind.Arena)
                {
                    shortest = System.Math.Min(shortest, fights);
                    longest = System.Math.Max(longest, fights);
                }
                foreach (int next in node.Next) Walk(floor.Node(next), fights);
            }
            Walk(floor.Start, 0);
            return (shortest, longest);
        }

        /// <summary>Every node reachable from the floor's first room.</summary>
        public static HashSet<int> Reachable(FloorGraph floor)
        {
            var seen = new HashSet<int> { floor.Start.Id };
            var stack = new Stack<FloorNode>();
            stack.Push(floor.Start);
            while (stack.Count > 0)
                foreach (int next in stack.Pop().Next)
                    if (seen.Add(next)) stack.Push(floor.Node(next));
            return seen;
        }

        /// <summary>The ends of a floor, by kind.</summary>
        public static IEnumerable<FloorNode> Ends(FloorGraph floor, RoomKind kind) => floor.Nodes.Where(n => n.Kind == kind);
    }
}
