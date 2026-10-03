using System.Collections.Generic;
using Hearthdelve.Core.Pathfinding;
using UnityEngine;

namespace Hearthdelve.Shared.Navigation
{
    /// <summary>
    /// The walkable grid of the loaded floor, baked from its colliders on first use: a cell is
    /// blocked when any solid collider on the obstacle layers overlaps it, even partly. Pathing
    /// logic stays in <see cref="GridPathfinder"/>; this only reads the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavGrid : MonoBehaviour
    {
        /// <summary>
        /// Overlap test size: under a tile, so a collider ending on a cell edge doesn't block the
        /// next cell. Shapes carry a contact skin (Physics2D's default contact offset), and a probe
        /// of nearly a full tile picks up every wall in the neighbouring tiles too.
        /// </summary>
        const float k_Probe = 0.9f;

        [SerializeField, Tooltip("The tiles covered, in world tile coordinates.")]
        RectInt m_Bounds = new(0, 0, 16, 16);
        [SerializeField, Tooltip("Layers whose solid colliders block movement.")]
        LayerMask m_Obstacles;

        GridMap m_Map;

        /// <summary>The grid of the active floor, if it has one.</summary>
        public static NavGrid Current { get; private set; }

        public RectInt Bounds => m_Bounds;
        public GridSpace Space => new(m_Bounds.position);

        public GridMap Map
        {
            get
            {
                if (m_Map == null) Bake();
                return m_Map;
            }
        }

        public void Configure(RectInt bounds, LayerMask obstacles)
        {
            m_Bounds = bounds;
            m_Obstacles = obstacles;
            m_Map = null;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Reads the colliders now. Call again if obstacles move.</summary>
        public void Bake()
        {
            Physics2D.SyncTransforms();
            var map = new GridMap(Mathf.Max(1, m_Bounds.width), Mathf.Max(1, m_Bounds.height));
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = m_Obstacles };
            var hits = new List<Collider2D>();
            GridSpace space = Space;
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var cell = new GridCell(x, y);
                if (Physics2D.OverlapBox(space.CellCentre(cell), Vector2.one * k_Probe, 0f, filter, hits) > 0)
                    map.SetBlocked(cell, true);
            }
            m_Map = map;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(m_Bounds.center, new Vector3(m_Bounds.width, m_Bounds.height, 0f));
            if (m_Map == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
            GridSpace space = Space;
            for (int y = 0; y < m_Map.Height; y++)
            for (int x = 0; x < m_Map.Width; x++)
                if (!m_Map.IsWalkable(new GridCell(x, y)))
                    Gizmos.DrawCube(space.CellCentre(new GridCell(x, y)), Vector3.one * 0.9f);
        }
    }
}
