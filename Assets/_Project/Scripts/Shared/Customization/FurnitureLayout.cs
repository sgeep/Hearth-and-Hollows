using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>The fixed shape of an area (D5: structure fixed): where pieces may go, the tiles kept clear, and the service's points.</summary>
    public sealed class AreaShape
    {
        public string Id;
        public AreaKind Kind;
        /// <summary>The area's (0, 0) cell in world tiles.</summary>
        public Vector2 Origin;
        /// <summary>The whole area in cells (the walkable grid's extent).</summary>
        public RectInt Bounds;
        /// <summary>Where standing and floor pieces may go; also the walkable floor before furniture.</summary>
        public RectInt Floor;
        /// <summary>Where wall pieces hang.</summary>
        public RectInt WallBand;
        /// <summary>Tiles nothing standing may cover (the entrance and the tile inside it).</summary>
        public HashSet<Vector2Int> Reserved = new();
        /// <summary>The service's points, in world tiles: inside the door, the queue spots front first, where staff rest.</summary>
        public Vector2 Door;
        public List<Vector2> Queue = new();
        public Vector2 Rest;

        public Vector2Int CellOf(Vector2 world) => Vector2Int.FloorToInt(world - Origin);
    }

    /// <summary>Why a piece can't go where it's held (the ghost turns red and says so).</summary>
    public enum PlacementProblem
    {
        None,
        /// <summary>It isn't drawn that way round, or can't be mirrored.</summary>
        CannotStandThatWay,
        /// <summary>Off the floor.</summary>
        OutsideTheRoom,
        /// <summary>A wall piece off the back wall.</summary>
        NotOnTheWall,
        /// <summary>A wall-bound piece (the kitchen range) away from the back wall.</summary>
        NotAgainstTheBackWall,
        /// <summary>On top of another piece of its kind.</summary>
        Overlaps,
        /// <summary>On the entrance or the tile inside it.</summary>
        BlocksTheEntrance,
        /// <summary>A surface item with no table or shelf under it.</summary>
        NeedsASurface,
    }

    public readonly struct PlacementCheck
    {
        public readonly PlacementProblem Problem;
        /// <summary>The piece in the way, for <see cref="PlacementProblem.Overlaps"/> (-1 otherwise).</summary>
        public readonly int Blocker;

        public PlacementCheck(PlacementProblem problem, int blocker = -1)
        {
            Problem = problem;
            Blocker = blocker;
        }

        public bool IsValid => Problem == PlacementProblem.None;
    }

    /// <summary>
    /// One area's pieces and the placement rules (4f plan §6; D1, D5, D6, D13): generous, with a clear reason when a
    /// spot won't do. Standing pieces stay on the floor, off the entrance, and never share footprint cells or bodies
    /// with each other; floor pieces (rugs) go under anything but not on each other; wall pieces hang on the back
    /// wall's band; wall-bound pieces stand against the back wall; surface items need a surface. Pure logic.
    /// </summary>
    public sealed class FurnitureLayout
    {
        const float k_BodySlack = 0.001f;

        readonly AreaShape m_Shape;
        readonly Func<string, FurnitureDefinition> m_Lookup;
        readonly List<PlacedFurniture> m_Pieces;

        public FurnitureLayout(AreaShape shape, Func<string, FurnitureDefinition> lookup, IEnumerable<PlacedFurniture> pieces)
        {
            m_Shape = shape;
            m_Lookup = lookup;
            m_Pieces = new List<PlacedFurniture>();
            foreach (PlacedFurniture p in pieces) m_Pieces.Add(p.Clone());
        }

        public AreaShape Shape => m_Shape;
        public IReadOnlyList<PlacedFurniture> Pieces => m_Pieces;

        public FurnitureDefinition Definition(string id) => m_Lookup(id);

        public ResolvedFurniture Resolve(PlacedFurniture p) => FurnitureGeometry.Resolve(m_Lookup(p?.definition), p, m_Shape.Origin);

        public List<ResolvedFurniture> ResolveAll()
        {
            var all = new List<ResolvedFurniture>();
            foreach (PlacedFurniture p in m_Pieces)
            {
                ResolvedFurniture r = Resolve(p);
                if (r != null) all.Add(r);
            }
            return all;
        }

        public PlacedFurniture Find(int uid) => m_Pieces.Find(p => p.uid == uid);

        /// <summary>Copies of the pieces, for undo and saving.</summary>
        public List<PlacedFurniture> Snapshot() => m_Pieces.ConvertAll(p => p.Clone());

        public void Restore(IEnumerable<PlacedFurniture> pieces)
        {
            m_Pieces.Clear();
            foreach (PlacedFurniture p in pieces) m_Pieces.Add(p.Clone());
        }

        /// <summary>Adds a piece (after <see cref="Check"/>). Surface items riding on it keep their place relative to it.</summary>
        public void Add(PlacedFurniture piece) => m_Pieces.Add(piece);

        /// <summary>Takes a piece out (to be carried, or to storage); the surface items on it come out too.</summary>
        public List<PlacedFurniture> Remove(int uid)
        {
            var removed = new List<PlacedFurniture>();
            PlacedFurniture piece = Find(uid);
            if (piece == null) return removed;
            m_Pieces.Remove(piece);
            removed.Add(piece);
            foreach (PlacedFurniture rider in m_Pieces.FindAll(p => p.host == uid))
            {
                m_Pieces.Remove(rider);
                removed.Add(rider);
            }
            return removed;
        }

        /// <summary>The pieces covering a cell, the one to pick first first: surface items, then standing, wall, floor.</summary>
        public List<PlacedFurniture> At(Vector2Int cell)
        {
            var found = new List<(int rank, PlacedFurniture piece)>();
            foreach (PlacedFurniture p in m_Pieces)
            {
                ResolvedFurniture r = Resolve(p);
                if (r == null || !r.Footprint.Contains(cell)) continue;
                int rank = r.Definition.layer switch
                {
                    FurnitureLayer.Surface => 0,
                    FurnitureLayer.Standing => 1,
                    FurnitureLayer.Wall => 2,
                    _ => 3,
                };
                found.Add((rank, p));
            }
            found.Sort((a, b) => a.rank.CompareTo(b.rank));
            return found.ConvertAll(f => f.piece);
        }

        /// <summary>Whether <paramref name="candidate"/> may stand where it says (pieces with its uid are ignored: it's the one being moved).</summary>
        public PlacementCheck Check(PlacedFurniture candidate)
        {
            FurnitureDefinition d = m_Lookup(candidate?.definition);
            ResolvedFurniture r = FurnitureGeometry.Resolve(d, candidate, m_Shape.Origin);
            if (r == null) return new PlacementCheck(PlacementProblem.CannotStandThatWay);
            RectInt f = r.Footprint;

            switch (d.layer)
            {
                case FurnitureLayer.Wall:
                    if (!Inside(m_Shape.WallBand, f)) return new PlacementCheck(PlacementProblem.NotOnTheWall);
                    break;
                case FurnitureLayer.Surface:
                    PlacedFurniture host = Find(candidate.host);
                    ResolvedFurniture hostResolved = host != null ? Resolve(host) : null;
                    if (hostResolved == null || !hostResolved.Surfaces.Exists(s => CellAt(s) == candidate.cell))
                        return new PlacementCheck(PlacementProblem.NeedsASurface);
                    break;
                default:
                    if (!Inside(m_Shape.Floor, f)) return new PlacementCheck(PlacementProblem.OutsideTheRoom);
                    if (d.wallBound && f.yMax != m_Shape.Floor.yMax) return new PlacementCheck(PlacementProblem.NotAgainstTheBackWall);
                    if (d.layer == FurnitureLayer.Standing)
                        for (int x = f.xMin; x < f.xMax; x++)
                        for (int y = f.yMin; y < f.yMax; y++)
                            if (m_Shape.Reserved.Contains(new Vector2Int(x, y))) return new PlacementCheck(PlacementProblem.BlocksTheEntrance);
                    break;
            }

            foreach (PlacedFurniture other in m_Pieces)
            {
                if (other.uid == candidate.uid) continue;
                FurnitureDefinition od = m_Lookup(other.definition);
                if (od == null || od.layer != d.layer) continue;
                ResolvedFurniture o = FurnitureGeometry.Resolve(od, other, m_Shape.Origin);
                if (o == null) continue;
                if (d.layer == FurnitureLayer.Surface)
                {
                    if (other.host == candidate.host && other.cell == candidate.cell) return new PlacementCheck(PlacementProblem.Overlaps, other.uid);
                    continue;
                }
                if (o.Footprint.Overlaps(f)) return new PlacementCheck(PlacementProblem.Overlaps, other.uid);
                if (d.layer == FurnitureLayer.Standing && BodiesOverlap(r, o)) return new PlacementCheck(PlacementProblem.Overlaps, other.uid);
            }
            return new PlacementCheck(PlacementProblem.None);
        }

        Vector2Int CellAt(Vector2 world) => m_Shape.CellOf(world);

        static bool Inside(RectInt area, RectInt f) => f.xMin >= area.xMin && f.yMin >= area.yMin && f.xMax <= area.xMax && f.yMax <= area.yMax;

        static bool BodiesOverlap(ResolvedFurniture a, ResolvedFurniture b)
        {
            foreach (Rect ra in a.Bodies)
            foreach (Rect rb in b.Bodies)
                if (ra.xMin < rb.xMax - k_BodySlack && rb.xMin < ra.xMax - k_BodySlack && ra.yMin < rb.yMax - k_BodySlack && rb.yMin < ra.yMax - k_BodySlack)
                    return true;
            return false;
        }
    }
}
