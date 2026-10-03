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

        readonly List<GridCell> m_Cells = new();
        readonly PathFollower m_Follower = new();
        CharacterMovement m_Movement;
        Collider2D m_Body;
        float m_NextRepath;

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
            // Spread re-planning across enemies that start chasing on the same frame.
            m_NextRepath = Time.time + Random.value * RepathInterval;
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

            if (Vector2.Distance(body, goal) <= StopDistance)
            {
                m_Movement.SetMovement(Vector2.zero);
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
            m_Movement.SetMovement(direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.zero);
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
