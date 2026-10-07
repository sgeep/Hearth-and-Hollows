using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// Keeps a property area's ways clear in an existing layout (2026-10-07: the stairs' approach and the hatch became reserved
    /// tiles). A standing piece that now covers a reserved tile moves to its starting-layout spot if that's free, otherwise to the
    /// nearest tile it fits; nothing else in the layout changes. Pieces can't be put on reserved tiles any more (the placement
    /// check refuses), so this only ever acts on layouts made before a tile was reserved.
    /// </summary>
    public static class LayoutRepair
    {
        /// <summary>Returns how many pieces moved (the area's layout in <paramref name="state"/> is updated when any did).</summary>
        public static int ClearReserved(FurnitureState state, AreaShape shape, Func<string, FurnitureDefinition> lookup, string area,
            Func<string, Vector2Int?> startingCell, int radius = 8)
        {
            if (state == null || shape == null || lookup == null) return 0;
            var pieces = new List<PlacedFurniture>();
            foreach (PlacedFurniture p in state.Layout(area)) pieces.Add(p.Clone());
            int moved = 0;
            foreach (PlacedFurniture piece in pieces)
            {
                var layout = new FurnitureLayout(shape, lookup, pieces);
                if (layout.Check(piece).Problem != PlacementProblem.BlocksTheEntrance) continue;
                foreach (Vector2Int cell in Candidates(startingCell?.Invoke(piece.definition), piece.cell, radius))
                {
                    PlacedFurniture candidate = piece.Clone();
                    candidate.cell = cell;
                    candidate.nudge = Vector2Int.zero;
                    if (!layout.Check(candidate).IsValid) continue;
                    piece.cell = cell;
                    piece.nudge = Vector2Int.zero;
                    moved++;
                    break;
                }
            }
            if (moved > 0) state.SetLayout(area, pieces);
            return moved;
        }

        static IEnumerable<Vector2Int> Candidates(Vector2Int? starting, Vector2Int from, int radius)
        {
            if (starting.HasValue) yield return starting.Value;
            for (int r = 1; r <= radius; r++)
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == r) yield return from + new Vector2Int(dx, dy);
        }
    }
}
