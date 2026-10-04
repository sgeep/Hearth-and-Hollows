using System;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// The root of a built room prefab (4d): its size, where the player arrives, its gated exits and its enemies.
    /// The room's bottom-left tile is at its origin; the camera keeps inside its rectangle.
    /// </summary>
    public sealed class RoomInstance : MonoBehaviour
    {
        [SerializeField] Vector2Int m_Size;
        [SerializeField] Transform m_Arrival;
        [SerializeField] RoomExit[] m_Exits = Array.Empty<RoomExit>();
        [SerializeField] Transform m_Enemies;

        public Vector2Int Size => m_Size;
        public Transform Arrival => m_Arrival;
        public RoomExit[] Exits => m_Exits;
        /// <summary>The room's tiles in world tile coordinates.</summary>
        public RectInt TileBounds => new(Vector2Int.RoundToInt(transform.position), m_Size);

        public void Configure(Vector2Int size, Transform arrival, RoomExit[] exits, Transform enemies)
        {
            m_Size = size;
            m_Arrival = arrival;
            m_Exits = exits;
            m_Enemies = enemies;
        }

        /// <summary>Enemies in the room still alive.</summary>
        public int LivingEnemies()
        {
            if (m_Enemies == null) return 0;
            int living = 0;
            foreach (Health health in m_Enemies.GetComponentsInChildren<Health>())
                if (health.isActiveAndEnabled && health.CurrentHealth > 0f) living++;
            return living;
        }

        public void SetExitsOpen(bool open, bool instant)
        {
            foreach (RoomExit exit in m_Exits)
                if (exit != null) exit.SetOpen(open, instant);
        }
    }
}
