using System.Collections.Generic;
using Hearthdelve.Shared.Customization;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// An area's floor and wall finishes (D5: whole areas, never per tile; the room's structure stays as built): lays the
    /// finish's tiles over the room's floor and back-wall tilemaps. A finish recoloured through palette ramps (the
    /// panelling in walnut) gets baked tiles, shared by every cell (D11). Null leaves the room as it was built.
    /// </summary>
    public sealed class AreaFinishes : MonoBehaviour
    {
        [SerializeField] Tilemap m_Floor;
        [SerializeField, Tooltip("The floor tilemap's cells the floor finish covers.")]
        RectInt m_FloorCells;
        [SerializeField] Tilemap m_Wall;
        [SerializeField, Tooltip("The wall tilemap's cells (three rows) the wall finish covers.")]
        RectInt m_WallCells;

        static readonly Dictionary<(int tile, string palette), TileBase> s_Recoloured = new();

        public Tilemap Floor => m_Floor;
        public Tilemap Wall => m_Wall;
        public FinishDefinition FloorFinish { get; private set; }
        public FinishDefinition WallFinish { get; private set; }

        public void Configure(Tilemap floor, RectInt floorCells, Tilemap wall, RectInt wallCells)
        {
            m_Floor = floor;
            m_FloorCells = floorCells;
            m_Wall = wall;
            m_WallCells = wallCells;
        }

        public void Apply(FinishDefinition floor, FinishDefinition wall, PaletteLibrary palettes)
        {
            if (floor != null && floor.kind == FinishKind.Floor && floor != FloorFinish) Lay(m_Floor, m_FloorCells, floor, palettes);
            if (wall != null && wall.kind == FinishKind.Wall && wall != WallFinish) Lay(m_Wall, m_WallCells, wall, palettes);
            if (floor != null) FloorFinish = floor;
            if (wall != null) WallFinish = wall;
        }

        static void Lay(Tilemap map, RectInt cells, FinishDefinition finish, PaletteLibrary palettes)
        {
            if (map == null || cells.width <= 0 || cells.height <= 0) return;
            var positions = new Vector3Int[cells.width * cells.height];
            var tiles = new TileBase[positions.Length];
            int i = 0;
            for (int y = cells.yMin; y < cells.yMax; y++)
            for (int x = cells.xMin; x < cells.xMax; x++)
            {
                positions[i] = new Vector3Int(x, y, 0);
                tiles[i] = Recolour(finish.TileAt(x - cells.xMin, y - cells.yMin, cells.width), finish, palettes);
                i++;
            }
            map.SetTiles(positions, tiles);
        }

        static TileBase Recolour(TileBase tile, FinishDefinition finish, PaletteLibrary palettes)
        {
            if (tile is not Tile source || finish.channels.Count == 0 || string.IsNullOrEmpty(finish.palette) || palettes == null) return tile;
            var key = (source.GetHashCode(), finish.palette);
            if (s_Recoloured.TryGetValue(key, out TileBase made) && made != null) return made;
            var baked = ScriptableObject.CreateInstance<Tile>();
            baked.name = $"{source.name} ({finish.palette})";
            baked.sprite = FurnitureRecolour.Apply(source.sprite, finish.channels, finish.palette, palettes);
            baked.colliderType = Tile.ColliderType.None;
            s_Recoloured[key] = baked;
            return baked;
        }
    }
}
