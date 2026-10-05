using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// Rules over placed furniture that don't need the scene. Seating comes from placed pieces (D16): a seat anchor
    /// counts when the sitter faces a table within reach. Pure logic, EditMode-tested.
    /// </summary>
    public static class FurnitureRules
    {
        /// <summary>How far in front of a seat a table may stand, in tiles.</summary>
        public const float TableReach = 1f;
        const float k_Step = 0.25f;

        /// <summary>Whether a table's body lies within <see cref="TableReach"/> of the seat in the way the sitter faces.</summary>
        public static bool FacesTable(PlacedSeat seat, IReadOnlyList<Rect> tableBodies)
        {
            Vector2 dir = seat.Facing;
            if (dir == Vector2.zero || tableBodies == null) return false;
            for (float d = k_Step; d <= TableReach + 0.0001f; d += k_Step)
            {
                Vector2 at = seat.Position + dir * d;
                foreach (Rect body in tableBodies)
                    if (Contains(body, at) || Contains(body, at + Vector2.up * k_Step)) return true;
            }
            return false;
        }

        static bool Contains(Rect r, Vector2 p) => p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax;

        /// <summary>The usable seats of an area's pieces, in the order the pieces were placed (seat numbers follow it).</summary>
        public static List<(ResolvedFurniture piece, PlacedSeat seat)> UsableSeats(IReadOnlyList<ResolvedFurniture> pieces)
        {
            var tables = new List<Rect>();
            foreach (ResolvedFurniture p in pieces)
                if (p.Definition.function == FurnitureFunction.Table) tables.AddRange(p.Bodies);
            var seats = new List<(ResolvedFurniture, PlacedSeat)>();
            foreach (ResolvedFurniture p in pieces)
            {
                if (p.Definition.function != FurnitureFunction.Seat) continue;
                foreach (PlacedSeat seat in p.Seats)
                {
                    if (seat.Facing == Vector2Int.zero)
                    {
                        if (TryFaceAny(seat, tables, out PlacedSeat faced)) seats.Add((p, faced));
                    }
                    else if (FacesTable(seat, tables)) seats.Add((p, seat));
                }
            }
            return seats;
        }

        /// <summary>
        /// The usable seats, each with an approach someone can actually reach (<paramref name="reachable"/>): its drawn one if
        /// it's clear, otherwise the first clear of beside it (left, right), behind the sitter, or in front. A chair facing
        /// down in a row of chairs, whose drawn approach is the next chair, is stepped onto from behind instead.
        /// </summary>
        public static List<(ResolvedFurniture piece, PlacedSeat seat)> UsableSeats(IReadOnlyList<ResolvedFurniture> pieces, System.Func<Vector2, bool> reachable)
        {
            List<(ResolvedFurniture, PlacedSeat)> seats = UsableSeats(pieces);
            if (reachable == null) return seats;
            for (int i = 0; i < seats.Count; i++) seats[i] = (seats[i].Item1, WithApproach(seats[i].Item2, reachable));
            return seats;
        }

        public static PlacedSeat WithApproach(PlacedSeat seat, System.Func<Vector2, bool> reachable)
        {
            if (reachable(seat.Approach)) return seat;
            // The seat point is the chair's foot: behind a chair facing down is past its own tile, a tile and a bit up.
            Vector2 p = seat.Position;
            Vector2 behind = seat.Facing == Vector2Int.down ? new Vector2(0f, 1.4f) : -(Vector2)seat.Facing * 0.9f;
            foreach (Vector2 candidate in new[] { p + new Vector2(-0.9f, 0f), p + new Vector2(0.9f, 0f), p + behind, p + new Vector2(0f, -0.9f) })
                if (reachable(candidate)) return new PlacedSeat(seat.Position, candidate, seat.Facing);
            return seat;
        }

        static readonly Vector2Int[] k_AnyWay = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        /// <summary>
        /// A seat without a back (a stool) faces whichever table stands beside it, trying east, west, north, then south;
        /// it's stepped onto from below, or from the side when the table is below it.
        /// </summary>
        public static bool TryFaceAny(PlacedSeat seat, IReadOnlyList<Rect> tables, out PlacedSeat faced)
        {
            foreach (Vector2Int dir in k_AnyWay)
            {
                var candidate = new PlacedSeat(seat.Position, ApproachFor(seat.Position, dir), dir);
                if (!FacesTable(candidate, tables)) continue;
                faced = candidate;
                return true;
            }
            faced = seat;
            return false;
        }

        /// <summary>Where a sitter facing <paramref name="dir"/> steps on from: below the seat, or beside it when facing down.</summary>
        public static Vector2 ApproachFor(Vector2 seat, Vector2Int dir) => dir == Vector2Int.down ? seat + new Vector2(0.9f, 0f) : seat + new Vector2(0f, -0.9f);
    }
}
