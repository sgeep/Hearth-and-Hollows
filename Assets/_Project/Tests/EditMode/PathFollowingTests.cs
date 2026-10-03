using System.Collections.Generic;
using Hearthdelve.Core.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class GridSpaceTests
    {
        [Test]
        public void WorldPoints_MapToTheTileTheyAreIn_AndBack()
        {
            var space = new GridSpace(new Vector2Int(-11, -6));
            Assert.That(space.ToCell(new Vector2(-11f, -6f)), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(space.ToCell(new Vector2(-10.01f, -5.01f)), Is.EqualTo(new GridCell(0, 0)));
            Assert.That(space.ToCell(new Vector2(-10f, -5f)), Is.EqualTo(new GridCell(1, 1)));
            Assert.That(space.ToCell(new Vector2(-11.5f, -6.5f)), Is.EqualTo(new GridCell(-1, -1)));
            Assert.That(space.CellCentre(new GridCell(2, 3)), Is.EqualTo(new Vector2(-8.5f, -2.5f)));
            Assert.That(space.ToCell(space.CellCentre(new GridCell(7, 4))), Is.EqualTo(new GridCell(7, 4)));
        }
    }

    public class GridSweepTests
    {
        static readonly GridSpace k_Space = new(Vector2Int.zero);
        // A slime's collision box.
        static readonly Vector2 k_Half = new(0.4f, 0.25f);

        [Test]
        public void OpenFloor_IsClear()
        {
            GridMap map = TestMaps.Map(
                ".....",
                ".....",
                ".....");
            Assert.That(GridSweep.IsClear(map, k_Space, new Vector2(0.5f, 0.5f), new Vector2(4.5f, 2.5f), k_Half));
        }

        [Test]
        public void AWallAcrossTheLine_IsNotClear()
        {
            GridMap map = TestMaps.Map(
                "..#..",
                "..#..",
                "..#..");
            Assert.That(GridSweep.IsClear(map, k_Space, new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), k_Half), Is.False);
        }

        [Test]
        public void ABoxThatWouldClipACorner_IsNotClear_EvenThoughItsCentreLineIs()
        {
            GridMap map = TestMaps.Map(
                "....",
                "....",
                "#...",
                "....");
            // The centre line passes 0.2–0.3 tiles right of the wall; the box reaches 0.4 each side.
            var from = new Vector2(1.2f, 0.5f);
            var to = new Vector2(1.3f, 3.5f);
            Assert.That(GridSweep.IsClear(map, k_Space, from, to, Vector2.zero), "a point would pass");
            Assert.That(GridSweep.IsClear(map, k_Space, from, to, k_Half), Is.False, "the box would clip the corner");
        }

        [Test]
        public void AOneTileCorridor_FitsABoxNarrowerThanATile()
        {
            GridMap map = TestMaps.Map(
                "#####",
                ".....",
                "#####");
            Assert.That(GridSweep.IsClear(map, k_Space, new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), k_Half));
            Assert.That(GridSweep.IsClear(map, k_Space, new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), new Vector2(0.4f, 0.55f)), Is.False);
        }

        [Test]
        public void TouchingACellEdge_IsNotOverlappingIt()
        {
            GridMap map = TestMaps.Map(
                "#.#");
            Assert.That(GridSweep.IsBoxClear(map, k_Space, new Vector2(1.5f, 0.5f), new Vector2(0.5f, 0.5f)));
            Assert.That(GridSweep.IsBoxClear(map, k_Space, new Vector2(1.5f, 0.5f), new Vector2(0.51f, 0.4f)), Is.False);
        }

        [Test]
        public void LeavingTheMap_IsNotClear()
        {
            GridMap map = TestMaps.Map("...");
            Assert.That(GridSweep.IsBoxClear(map, k_Space, new Vector2(0.3f, 0.5f), k_Half), Is.False);
        }
    }

    public class NearestWalkableTests
    {
        [Test]
        public void AnOpenCell_IsItsOwnNearest()
        {
            GridMap map = TestMaps.Map("...");
            Assert.That(map.TryFindNearestWalkable(new GridCell(1, 0), 3, out GridCell nearest));
            Assert.That(nearest, Is.EqualTo(new GridCell(1, 0)));
        }

        [Test]
        public void ABlockedCell_FindsTheClosestOpenOne_PreferringStraightNeighbours()
        {
            GridMap map = TestMaps.Map(
                ".##",
                "###",
                "##.");
            Assert.That(map.TryFindNearestWalkable(new GridCell(1, 1), 3, out GridCell nearest));
            Assert.That(nearest == new GridCell(0, 2) || nearest == new GridCell(2, 0));

            GridMap straight = TestMaps.Map(
                "...",
                "##.",
                "...");
            Assert.That(straight.TryFindNearestWalkable(new GridCell(0, 1), 2, out nearest));
            Assert.That(System.Math.Abs(nearest.X - 0) + System.Math.Abs(nearest.Y - 1), Is.EqualTo(1));
        }

        [Test]
        public void NothingOpenWithinTheRadius_Fails()
        {
            GridMap map = TestMaps.Map(
                "#####",
                "#####",
                "#####",
                "####.");
            Assert.That(map.TryFindNearestWalkable(new GridCell(0, 3), 2, out _), Is.False);
            Assert.That(map.TryFindNearestWalkable(new GridCell(0, 3), 4, out _));
        }
    }

    public class PathFollowerTests
    {
        static readonly GridSpace k_Space = new(Vector2Int.zero);
        static readonly Vector2 k_Half = new(0.4f, 0.25f);

        static PathFollower Follow(GridMap map, GridCell start, GridCell goal)
        {
            var cells = new List<GridCell>();
            Assert.That(GridPathfinder.TryFindPath(map, start, goal, cells), "no path");
            var follower = new PathFollower();
            follower.SetPath(cells, k_Space);
            return follower;
        }

        [Test]
        public void InTheOpen_AimsStraightAtTheEnd()
        {
            GridMap map = TestMaps.Map(
                "......",
                "......",
                "......");
            PathFollower follower = Follow(map, new GridCell(0, 0), new GridCell(5, 2));
            Assert.That(follower.TrySteer(map, k_Space, new Vector2(0.5f, 0.5f), k_Half, out Vector2 point));
            Assert.That(point, Is.EqualTo(new Vector2(5.5f, 2.5f)));
        }

        [Test]
        public void AroundACorner_AimsAtTheTurn_NotThroughTheWall()
        {
            GridMap map = TestMaps.Map(
                "......",
                "####..",
                "......");
            PathFollower follower = Follow(map, new GridCell(0, 0), new GridCell(0, 2));
            Assert.That(follower.TrySteer(map, k_Space, new Vector2(0.5f, 0.5f), k_Half, out Vector2 point));
            Assert.That(GridSweep.IsClear(map, k_Space, new Vector2(0.5f, 0.5f), point, k_Half));
            Assert.That(point.x, Is.GreaterThan(4f), "goes to the gap first");
            Assert.That(point.y, Is.LessThan(2f), "not past the wall yet");
        }

        [Test]
        public void ReachingTheLastWaypoint_EndsThePath()
        {
            GridMap map = TestMaps.Map("....");
            PathFollower follower = Follow(map, new GridCell(0, 0), new GridCell(3, 0));
            Assert.That(follower.TrySteer(map, k_Space, new Vector2(3.5f, 0.5f), k_Half, out _), Is.False);
            Assert.That(follower.HasPath, Is.False);
        }

        /// <summary>
        /// The real guarantee: a box moving at a fixed speed towards wherever the follower says
        /// never overlaps a blocked cell, and gets there. Covers a U-shaped wall, a doorway and a
        /// column of props, at several speeds and box sizes.
        /// </summary>
        [TestCase(0.4f, 0.25f, 2.5f)]
        [TestCase(0.35f, 0.225f, 4f)]
        [TestCase(0.45f, 0.45f, 3f)]
        public void AMovingBox_FollowsThePath_WithoutEverOverlappingABlockedCell(float halfX, float halfY, float speed)
        {
            string[] rows =
            {
                "....................",
                "..2.................",
                ".....#########......",
                ".....#.......#..c...",
                ".....#...1...#..c.4.",
                ".....#.......#..c...",
                "................c...",
                "######..###########.",
                "....................",
                "..3.................",
            };
            GridMap map = TestMaps.Map(rows);
            var half = new Vector2(halfX, halfY);
            AssertReaches(map, TestMaps.Find(rows, '1'), TestMaps.Find(rows, '2'), half, speed);
            AssertReaches(map, TestMaps.Find(rows, '1'), TestMaps.Find(rows, '3'), half, speed);
            AssertReaches(map, TestMaps.Find(rows, '4'), TestMaps.Find(rows, '3'), half, speed);
            AssertReaches(map, TestMaps.Find(rows, '3'), TestMaps.Find(rows, '4'), half, speed);
        }

        static void AssertReaches(GridMap map, GridCell start, GridCell goal, Vector2 half, float speed)
        {
            var cells = new List<GridCell>();
            Assert.That(GridPathfinder.TryFindPath(map, start, goal, cells));
            var follower = new PathFollower();
            follower.SetPath(cells, k_Space);

            const float dt = 1f / 60f;
            Vector2 body = k_Space.CellCentre(start);
            Vector2 end = k_Space.CellCentre(goal);
            for (int frame = 0; frame < 60 * 60; frame++)
            {
                Assert.That(GridSweep.IsBoxClear(map, k_Space, body, half), $"overlapped a blocked cell at {body} going {start} → {goal}");
                if (!follower.TrySteer(map, k_Space, body, half, out Vector2 aim)) aim = end;
                Vector2 step = aim - body;
                if (Vector2.Distance(body, end) <= PathFollower.ArrivalRadius) return;
                body += step.normalized * Mathf.Min(speed * dt, step.magnitude);
            }
            Assert.Fail($"never reached {goal} from {start}; stopped at {body}");
        }
    }

    /// <summary>Small maps drawn as text, top row first: '#' and 'c' are blocked.</summary>
    static class TestMaps
    {
        public static GridMap Map(params string[] rows)
        {
            var map = new GridMap(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            for (int x = 0; x < rows[row].Length; x++)
                map.SetBlocked(x, rows.Length - 1 - row, rows[row][x] is '#' or 'c');
            return map;
        }

        public static GridCell Find(string[] rows, char marker)
        {
            for (int row = 0; row < rows.Length; row++)
            {
                int x = rows[row].IndexOf(marker);
                if (x >= 0) return new GridCell(x, rows.Length - 1 - row);
            }
            throw new System.ArgumentException($"no '{marker}' on the map");
        }
    }
}
