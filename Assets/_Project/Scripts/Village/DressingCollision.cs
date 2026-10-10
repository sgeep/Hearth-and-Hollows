using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Village
{
    /// <summary>
    /// Kariaston's dressing made solid by what it is, not by where it stands (the owner's request, 2026-10-08): the village is hand
    /// laid out and will be rearranged, so nothing here is a wall placed at a spot.
    /// <list type="bullet">
    /// <item><b>Props by drawing:</b> every sprite in the scene showing one of the listed drawings (a fence run, a sign post, Tally Ho!'s
    /// board; since the Crossroads also the tall drawings, over the ground they'd hide a figure on) gets the footprints fitted to
    /// that drawing's own pixels (measured when the list is built), as the scene
    /// loads, wherever it was put and however it got there (copied, moved, newly dragged in). A prop that already carries its own
    /// collider is left alone.</item>
    /// <item><b>Tiles by kind:</b> every tilemap in the scene holding tiles that collide (the water) collides, one cell per tile, so
    /// water painted anywhere later is solid too. (The pond's merged composite collider had produced no shape at all.)</item>
    /// </list>
    /// Runs in Awake, before the village's walking grid is baked, so villagers path round the same things the keeper bumps into.
    /// </summary>
    public sealed class DressingCollision : MonoBehaviour
    {
        /// <summary>One drawing that blocks: its footprint in the sprite's own space (units), the bottom edge at its art's lowest pixel.</summary>
        [Serializable]
        public sealed class Solid
        {
            public Sprite sprite;
            [Tooltip("The footprint's centre, relative to the sprite's pivot, in world units (unflipped).")]
            public Vector2 offset;
            public Vector2 size;
        }

        public const string FootprintName = "Footprint";

        [SerializeField] List<Solid> m_Solids = new();
        [SerializeField, Tooltip("The physics layer of everything made solid here (the keeper and the walking grid collide with it).")]
        string m_Layer = "Obstacles";

        public IReadOnlyList<Solid> Solids => m_Solids;

        public void Configure(List<Solid> solids) => m_Solids = solids;

        void Awake() => Apply();

        /// <summary>Makes the scene's listed drawings and colliding tiles solid (again: anything already done is left as it is).</summary>
        public void Apply()
        {
            int layer = LayerMask.NameToLayer(m_Layer);
            if (layer < 0) layer = gameObject.layer;
            // A drawing may have several footprints (a hall's porch and body; a canopy's bands, 2026-10-10).
            var byDrawing = new Dictionary<Sprite, List<Solid>>();
            foreach (Solid s in m_Solids)
                if (s != null && s.sprite != null)
                {
                    if (!byDrawing.TryGetValue(s.sprite, out List<Solid> list)) byDrawing[s.sprite] = list = new List<Solid>();
                    list.Add(s);
                }

            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.sprite != null && byDrawing.TryGetValue(r.sprite, out List<Solid> solids) && !HasCollider(r))
                        foreach (Solid solid in solids) Footprint(r, solid, layer);
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true)) Tiles(map, layer);
            }
            Physics2D.SyncTransforms();
        }

        /// <summary>A prop that already carries a collider (its own, or footprints from an earlier pass) is left as it is.</summary>
        static bool HasCollider(SpriteRenderer prop) => prop.GetComponentInChildren<Collider2D>(true) != null;

        /// <summary>A footprint under the prop (a child, so it moves with it).</summary>
        static void Footprint(SpriteRenderer prop, Solid solid, int layer)
        {
            var go = new GameObject(FootprintName) { layer = layer };
            go.transform.SetParent(prop.transform, false);
            Vector3 scale = prop.transform.lossyScale;
            Vector2 offset = solid.offset;
            if (prop.flipX) offset.x = -offset.x;
            go.transform.localPosition = new Vector3(offset.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), offset.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(solid.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), solid.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
        }

        /// <summary>A tilemap with any colliding tile collides, cell by cell (no composite merge).</summary>
        static void Tiles(Tilemap map, int layer)
        {
            bool solid = false;
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                if (map.HasTile(cell) && map.GetColliderType(cell) != Tile.ColliderType.None)
                {
                    solid = true;
                    break;
                }
            if (!solid) return;
            var collider = map.GetComponent<TilemapCollider2D>();
            if (collider == null) collider = map.gameObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.None;
            collider.enabled = true;
            map.gameObject.layer = layer;
            collider.ProcessTilemapChanges();
        }
    }
}
