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
                    if (FacesTable(seat, tables)) seats.Add((p, seat));
            }
            return seats;
        }
    }
}
