using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Pathfinding;
using UnityEngine;

namespace Hearthdelve.Shared.Navigation
{
    /// <summary>
    /// Published when obstacles move (furniture placed, moved or removed): the grid is invalidated
    /// and rebakes on next use, and path followers re-plan.
    /// </summary>
    public readonly struct NavigationLayoutChanged : IEvent { }

    /// <summary>
    /// The walkable grid of the loaded floor or room, baked from its colliders on first use: a cell
    /// is blocked when any solid collider on the obstacle layers overlaps it, even partly. The
    /// layout is not assumed fixed: <see cref="Invalidate"/> (or a <see cref="NavigationLayoutChanged"/>
    /// event) drops the grid and the next use rebakes it, and <see cref="Version"/> tells path
    /// followers to re-plan. Pathing logic stays in <see cref="GridPathfinder"/>; this only reads
    /// the scene.
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
        [SerializeField, Tooltip("The floor's grid (Current). Off: a grid only its own users hold, beside another scene's (4h Checkpoint C: Kariaston's, loaded beside the tavern's).")]
        bool m_Global = true;

        GridMap m_Map;
        int m_Version;

        /// <summary>The grid of the active floor, if it has one.</summary>
        public static NavGrid Current { get; private set; }

        /// <summary>Raised after each bake, with the new grid.</summary>
        public event Action<NavGrid> Rebuilt;

        /// <summary>Goes up with every bake. Paths planned on an older version are stale.</summary>
        public int Version => m_Version;

        /// <summary>True while a baked grid is current (false after <see cref="Invalidate"/> until the next use).</summary>
        public bool IsBaked => m_Map != null;

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

        public void Configure(RectInt bounds, LayerMask obstacles, bool global = true)
        {
            m_Bounds = bounds;
            m_Obstacles = obstacles;
            m_Global = global;
            m_Map = null;
        }

        void OnEnable()
        {
            if (m_Global) Current = this;
            EventBus<NavigationLayoutChanged>.Subscribe(OnLayoutChanged);
        }

        void OnDisable()
        {
            EventBus<NavigationLayoutChanged>.Unsubscribe(OnLayoutChanged);
            if (Current == this) Current = null;
        }

        void OnLayoutChanged(NavigationLayoutChanged _) => Invalidate();

        /// <summary>Drops the grid: the next use reads the colliders again. Cheap; call whenever obstacles change.</summary>
        public void Invalidate() => m_Map = null;

        /// <summary>Reads the colliders now (same as <see cref="Bake"/>).</summary>
        public void Rebuild() => Bake();

        /// <summary>Reads the colliders now.</summary>
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
            m_Version++;
            Rebuilt?.Invoke(this);
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
