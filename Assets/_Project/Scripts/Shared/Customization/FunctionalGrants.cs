using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// A unique working piece the property must have, given to a save from before it existed (4h, after the Checkpoint A
    /// playtest: the storeroom shelves became furniture you can move). A unique piece is never sold or destroyed, so "owns none"
    /// means "never had it": the grant happens once, with no save-version bump. It arrives placed where the starting room has
    /// it, or on the nearest tile it fits; if nowhere fits, in storage, marked new.
    /// </summary>
    public static class FunctionalGrants
    {
        public const string StoreroomShelves = "storeroom_shelves";

        /// <summary>Returns true when the piece was given (placed or stored); false when the save already owns it.</summary>
        public static bool GrantOnce(FurnitureState state, FurnitureLayout layout, string area, FurnitureDefinition definition, Vector2Int preferred,
            out bool placed)
        {
            placed = false;
            if (state == null || layout == null || definition == null || state.OwnedCount(definition.id) > 0) return false;
            foreach (Vector2Int cell in Around(preferred, 6))
            {
                var candidate = new PlacedFurniture { uid = state.NextUid, definition = definition.id, cell = cell };
                if (!layout.Check(candidate).IsValid) continue;
                candidate.uid = state.TakeUid();
                state.AddOwnedCopies(definition.id, 1);
                var pieces = new List<PlacedFurniture>(state.Layout(area)) { candidate };
                state.SetLayout(area, pieces);
                placed = true;
                return true;
            }
            state.Receive(definition);
            return true;
        }

        /// <summary>The preferred cell, then rings around it, nearest first.</summary>
        static IEnumerable<Vector2Int> Around(Vector2Int centre, int radius)
        {
            yield return centre;
            for (int r = 1; r <= radius; r++)
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == r) yield return centre + new Vector2Int(dx, dy);
        }
    }
}
