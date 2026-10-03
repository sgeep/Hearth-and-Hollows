using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>
    /// Steers a character's collision box along a grid path. Following cell centres one by one
    /// zig-zags, so the follower aims at the furthest waypoint the whole box can slide to in a
    /// straight line (<see cref="GridSweep"/>); around corners that is the next turn, in the open
    /// it is the end of the path.
    /// </summary>
    public sealed class PathFollower
    {
        /// <summary>A waypoint this close counts as reached.</summary>
        public const float ArrivalRadius = 0.12f;

        readonly List<Vector2> m_Points = new();
        int m_Index;
        // The last waypoint the body actually reached: always a clean place to settle back to.
        Vector2 m_Anchor;
        // The current waypoint was checked as a clean slide from a point on the line the body is
        // moving along, so the rest of that line is clean too. It is not checked again: the
        // check is cautious and could fail halfway along a line it passed at the start.
        bool m_Committed;

        public bool HasPath => m_Index < m_Points.Count;
        public int RemainingWaypoints => m_Points.Count - m_Index;

        public void SetPath(IReadOnlyList<GridCell> cells, GridSpace space)
        {
            m_Points.Clear();
            m_Index = 0;
            if (cells == null) return;
            foreach (GridCell cell in cells) m_Points.Add(space.CellCentre(cell));
            if (m_Points.Count > 0) m_Anchor = m_Points[0];
            m_Committed = false;
        }

        public void Clear()
        {
            m_Points.Clear();
            m_Index = 0;
            m_Committed = false;
        }

        /// <summary>
        /// The point to move towards from <paramref name="body"/> (the box centre), or false once
        /// the last waypoint is reached.
        /// </summary>
        public bool TrySteer(GridMap map, GridSpace space, Vector2 body, Vector2 halfExtents, out Vector2 point)
        {
            if (m_Points.Count > 0 && Vector2.Distance(body, m_Points[m_Points.Count - 1]) <= ArrivalRadius) m_Index = m_Points.Count;
            while (m_Index < m_Points.Count && Vector2.Distance(body, m_Points[m_Index]) <= ArrivalRadius)
            {
                m_Anchor = m_Points[m_Index++];
                m_Committed = false;
            }
            if (m_Index >= m_Points.Count)
            {
                point = body;
                return false;
            }

            for (int i = m_Points.Count - 1; i > m_Index; i--)
            {
                if (!GridSweep.IsClear(map, space, body, m_Points[i], halfExtents)) continue;
                m_Index = i;
                m_Committed = true;
                break;
            }
            if (!m_Committed) m_Committed = GridSweep.IsClear(map, space, body, m_Points[m_Index], halfExtents);

            // If not even the next waypoint is a clean slide from here (the body drifted off the
            // waypoint it last reached), settle back onto that one first and go on from its centre.
            point = m_Committed ? m_Points[m_Index] : m_Anchor;
            return true;
        }
    }
}
