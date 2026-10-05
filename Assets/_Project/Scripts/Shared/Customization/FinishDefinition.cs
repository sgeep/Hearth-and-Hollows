using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// An area-wide floor or wall finish (D5: whole areas, never per tile; the structure stays fixed). Its tiles repeat in
    /// a pattern of <see cref="pattern"/> cells across the floor, or along the back wall (three rows); optional edge
    /// columns finish the left and right sides, as the premade rooms' own edge cells do. Owned once bought, for every
    /// area.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Finish Definition", fileName = "Finish_")]
    public sealed class FinishDefinition : ScriptableObject
    {
        [Tooltip("Stable id, saved per area.")]
        public string id;
        public string nameKey;
        public FinishKind kind;
        public FurnitureTheme theme;
        [Min(0), Tooltip("Gold; 0 = free (starter finishes).")]
        public int price;
        [Min(0), Tooltip("The Renown catalogue tier that unlocks it (D14).")]
        public int catalogTier;
        public FurnitureSource sources = FurnitureSource.Bought;

        [Tooltip("Pattern size in cells (a wall's height is always its three rows).")]
        public Vector2Int pattern = Vector2Int.one;
        [Tooltip("The pattern's tiles, row by row from the bottom: index = y × width + x.")]
        public List<TileBase> tiles = new();
        [Tooltip("The leftmost column's tiles, bottom up (one per pattern row). Empty: the pattern.")]
        public List<TileBase> leftEdge = new();
        [Tooltip("The rightmost column's tiles, bottom up. Empty: the pattern.")]
        public List<TileBase> rightEdge = new();
        [Tooltip("What the catalogue shows for it.")]
        public Sprite swatch;
        [Tooltip("Recoloured from its tiles' drawn colours (D11), like furniture: channels and the ramps they take.")]
        public List<PaletteChannel> channels = new();
        public string palette;

        /// <summary>
        /// The tile for a cell <paramref name="column"/> across and <paramref name="row"/> up from the finish's
        /// bottom-left, in a band <paramref name="width"/> cells wide.
        /// </summary>
        public TileBase TileAt(int column, int row, int width)
        {
            int h = Mathf.Max(1, pattern.y);
            int y = ((row % h) + h) % h;
            if (column == 0 && leftEdge.Count > 0) return leftEdge[Mathf.Min(y, leftEdge.Count - 1)];
            if (column == width - 1 && rightEdge.Count > 0) return rightEdge[Mathf.Min(y, rightEdge.Count - 1)];
            int w = Mathf.Max(1, pattern.x);
            int x = ((column % w) + w) % w;
            int i = y * w + x;
            return i < tiles.Count ? tiles[i] : null;
        }
    }
}
