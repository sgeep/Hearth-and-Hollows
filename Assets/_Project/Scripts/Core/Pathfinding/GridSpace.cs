using UnityEngine;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>
    /// Where a <see cref="GridMap"/> sits in the world. Cell (0, 0) covers the world tile whose
    /// bottom-left corner is <see cref="Origin"/>; 1 cell = 1 tile = 1 world unit.
    /// </summary>
    public readonly struct GridSpace
    {
        public readonly Vector2Int Origin;

        public GridSpace(Vector2Int origin) => Origin = origin;

        public GridCell ToCell(Vector2 world) =>
            new(Mathf.FloorToInt(world.x) - Origin.x, Mathf.FloorToInt(world.y) - Origin.y);

        public Vector2 CellCentre(GridCell cell) => new(cell.X + Origin.x + 0.5f, cell.Y + Origin.y + 0.5f);
    }
}
