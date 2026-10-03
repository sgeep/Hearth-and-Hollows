using System.Collections.Generic;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Navigation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Moves the character to the brain's target around walls and props. TDE has no 2D
    /// pathfinding, so this drives TDE's <see cref="CharacterMovement"/> from our grid A*
    /// (<see cref="NavGrid"/>, <see cref="GridPathfinder"/>, <see cref="PathFollower"/>). When the
    /// character's whole collision box can slide straight to the target it heads straight there;
    /// otherwise it follows a path, re-planned while the target moves. Without a
    /// <see cref="NavGrid"/> in the scene it simply heads straight for the target.
    /// </summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Pathfind To Target 2D")]
    public sealed class AIActionPathfindToTarget2D : AIAction
    {
        [Tooltip("How often the path is re-planned while the target is out of sight, in seconds.")]
        public float RepathInterval = 0.25f;
        [Tooltip("Stops moving when the body is this close to the target, in tiles.")]
        public float StopDistance = 0.1f;
        [Tooltip("How far to look for an open cell when the character or target stands in a partly blocked one, in tiles.")]
        public int NearestOpenCellRadius = 3;
        [Tooltip("Keep between these distances from the target (min, max), backing off when closer than min. Zero: close in.")]
        public Vector2 KeepDistance;
        [Range(0f, 1f), Tooltip("Erratic side-to-side wobble added to the heading (a bat's flight).")]
        public float Flutter;
        [Min(0f), Tooltip("How fast the flutter wobbles, in cycles per second.")]
        public float FlutterFrequency = 1.6f;

        readonly List<GridCell> m_Cells = new();
        readonly PathFollower m_Follower = new();
        CharacterMovement m_Movement;
        Collider2D m_Body;
        float m_NextRepath;
        float m_FlutterPhase;

        /// <summary>True while following a planned path rather than heading straight for the target.</summary>
        public bool IsFollowingPath { get; private set; }

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            var character = GetComponentInParent<Character>();
            m_Movement = character != null ? character.FindAbility<CharacterMovement>() : null;
            if (character != null)
                foreach (Collider2D candidate in character.GetComponents<Collider2D>())
                    if (!candidate.isTrigger) { m_Body = candidate; break; }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            m_Follower.Clear();
            // Spread re-planning (and flutter) across enemies that start chasing on the same frame.
            m_NextRepath = Time.time + Random.value * RepathInterval;
            m_FlutterPhase = Random.value * 2f * Mathf.PI;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            m_Follower.Clear();
            IsFollowingPath = false;
            m_Movement?.SetMovement(Vector2.zero);
        }

        public override void PerformAction()
        {
            if (m_Movement == null) return;
            if (_brain.Target == null)
            {
                m_Movement.SetMovement(Vector2.zero);
                return;
            }

            Vector2 feet = transform.position;
            Vector2 body = m_Body != null ? (Vector2)m_Body.bounds.center : feet;
            Vector2 half = m_Body != null ? (Vector2)m_Body.bounds.extents : Vector2.zero;
            // Where this body's centre would be with its feet on the target's feet.
            Vector2 goal = (Vector2)_brain.Target.position + (body - feet);

            float distance = Vector2.Distance(body, goal);
            if (distance <= StopDistance)
            {
                m_Movement.SetMovement(Vector2.zero);
                return;
            }
            if (KeepDistance.y > 0f && distance <= KeepDistance.y &&
                (NavGrid.Current == null || GridSweep.IsClear(NavGrid.Current.Map, NavGrid.Current.Space, body, goal, Vector2.one * 0.15f)))
            {
                // In the band: hold. Too close: back away, if there is room.
                IsFollowingPath = false;
                m_Follower.Clear();
                m_Movement.SetMovement(distance < KeepDistance.x ? Retreat(body, goal, half) : Vector2.zero);
                return;
            }

            Vector2 aim = goal;
            NavGrid grid = NavGrid.Current;
            IsFollowingPath = false;
            if (grid != null && !GridSweep.IsClear(grid.Map, grid.Space, body, goal, half))
            {
                if (!m_Follower.HasPath || Time.time >= m_NextRepath) Replan(grid, body, goal);
                if (m_Follower.TrySteer(grid.Map, grid.Space, body, half, out Vector2 waypoint))
                {
                    aim = waypoint;
                    IsFollowingPath = true;
                }
            }
            else
            {
                m_Follower.Clear();
            }

            Vector2 direction = aim - body;
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.zero;
            if (Flutter > 0f && direction != Vector2.zero)
            {
                // Sideways only when the wobble stays clear of walls, so flying never clips.
                Vector2 side = new Vector2(-direction.y, direction.x) * (Mathf.Sin(Time.time * FlutterFrequency * 2f * Mathf.PI + m_FlutterPhase) * Flutter);
                Vector2 wobbled = (direction + side).normalized;
                if (grid == null || GridSweep.IsClear(grid.Map, grid.Space, body, body + wobbled * 0.5f, half)) direction = wobbled;
            }
            m_Movement.SetMovement(direction);
        }

        /// <summary>Away from the goal, or along a side if the way back is blocked; still if both are.</summary>
        Vector2 Retreat(Vector2 body, Vector2 goal, Vector2 half)
        {
            Vector2 away = (body - goal).sqrMagnitude > 1e-6f ? (body - goal).normalized : Vector2.right;
            NavGrid grid = NavGrid.Current;
            if (grid == null) return away;
            Vector2 left = new(-away.y, away.x);
            foreach (Vector2 option in new[] { away, (away + left).normalized, (away - left).normalized, left, -left })
                if (GridSweep.IsClear(grid.Map, grid.Space, body, body + option * 0.6f, half))
                    return option;
            return Vector2.zero;
        }

        void Replan(NavGrid grid, Vector2 body, Vector2 goal)
        {
            m_NextRepath = Time.time + RepathInterval;
            GridMap map = grid.Map;
            GridSpace space = grid.Space;
            if (map.TryFindNearestWalkable(space.ToCell(body), NearestOpenCellRadius, out GridCell start) &&
                map.TryFindNearestWalkable(space.ToCell(goal), NearestOpenCellRadius, out GridCell end) &&
                GridPathfinder.TryFindPath(map, start, end, m_Cells))
                m_Follower.SetPath(m_Cells, space);
            else
                m_Follower.Clear();
        }
    }
}
