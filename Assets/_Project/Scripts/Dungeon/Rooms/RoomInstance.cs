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
        [SerializeField, Tooltip("Where the run's encounter stands: ground points, bottom row first.")]
        Transform[] m_GroundSpawns = Array.Empty<Transform>();
        [SerializeField, Tooltip("Bat perches, under the walls.")]
        Transform[] m_PerchSpawns = Array.Empty<Transform>();
        [SerializeField, Tooltip("Shown once the room is clear (the arena's rope).")]
        GameObject[] m_RevealOnClear = Array.Empty<GameObject>();
        [SerializeField, Tooltip("The hole down, in a descent room.")]
        FloorDescent m_Descent;

        public Vector2Int Size => m_Size;
        public Transform Arrival => m_Arrival;
        public RoomExit[] Exits => m_Exits;
        public Transform Enemies => m_Enemies;
        public Transform[] GroundSpawns => m_GroundSpawns;
        public Transform[] PerchSpawns => m_PerchSpawns;
        public FloorDescent Descent => m_Descent;
        /// <summary>The room's tiles in world tile coordinates.</summary>
        public RectInt TileBounds => new(Vector2Int.RoundToInt(transform.position), m_Size);

        public void Configure(Vector2Int size, Transform arrival, RoomExit[] exits, Transform enemies)
        {
            m_Size = size;
            m_Arrival = arrival;
            m_Exits = exits;
            m_Enemies = enemies;
        }

        public void ConfigureRun(Transform[] groundSpawns, Transform[] perchSpawns, GameObject[] revealOnClear, FloorDescent descent)
        {
            m_GroundSpawns = groundSpawns;
            m_PerchSpawns = perchSpawns;
            m_RevealOnClear = revealOnClear;
            m_Descent = descent;
        }

        /// <summary>Places one enemy of the run's encounter on its spawn point.</summary>
        public GameObject Spawn(EncounterSpawn spawn, GameObject prefab)
        {
            Transform[] points = spawn.Kind == EnemyKind.Bat && !spawn.InOpen ? m_PerchSpawns : m_GroundSpawns;
            if (prefab == null) return null;
            Vector3 at;
            if (spawn.Kind == EnemyKind.Boss) at = BossPoint();
            else if (spawn.Point >= 0 && spawn.Point < points.Length) at = points[spawn.Point].position;
            else return null;
            GameObject enemy = Instantiate(prefab, at, Quaternion.identity, m_Enemies);
            enemy.name = prefab.name;
            // A bat placed in the open flies there on purpose (no wall to hang from, and no warning about it).
            if (spawn.InOpen && enemy.TryGetComponent(out Hearthdelve.Dungeon.Enemies.EnemyPerch perch)) perch.InOpen = true;
            return enemy;
        }

        /// <summary>Where a boss stands: the ground spawn nearest the middle of the room's far end, facing the way in.</summary>
        public Vector3 BossPoint()
        {
            Vector2 wanted = (Vector2)transform.position + new Vector2(Size.x / 2f, Size.y - 7f);
            Transform best = null;
            foreach (Transform point in m_GroundSpawns)
                if (point != null && (best == null || Vector2.Distance(point.position, wanted) < Vector2.Distance(best.position, wanted)))
                    best = point;
            return best != null ? best.position : (Vector3)wanted;
        }

        /// <summary>Shows what waits for the room to be clear (the arena's rope), or hides it.</summary>
        public void SetRevealed(bool revealed)
        {
            foreach (GameObject thing in m_RevealOnClear)
                if (thing != null) thing.SetActive(revealed);
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
