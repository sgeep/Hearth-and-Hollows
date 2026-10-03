using System.Collections.Generic;
using Hearthdelve.Core.Pathfinding;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class GridPathfinderTests
    {
        /// <summary>Builds a map from rows drawn top-down: '#' is blocked, anything else is open.</summary>
        static GridMap Map(params string[] rows)
        {
            var map = new GridMap(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            for (int x = 0; x < rows[row].Length; x++)
                map.SetBlocked(x, rows.Length - 1 - row, rows[row][x] == '#');
            return map;
        }

        static List<GridCell> Find(GridMap map, GridCell start, GridCell goal, out bool found)
        {
            var path = new List<GridCell>();
            found = GridPathfinder.TryFindPath(map, start, goal, path);
            return path;
        }

        static void AssertStepsAreLegal(GridMap map, List<GridCell> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Assert.That(map.IsWalkable(path[i]), $"{path[i]} is blocked");
                if (i == 0) continue;
                int dx = path[i].X - path[i - 1].X;
                int dy = path[i].Y - path[i - 1].Y;
                Assert.That(System.Math.Abs(dx) <= 1 && System.Math.Abs(dy) <= 1 && (dx != 0 || dy != 0),
                    $"{path[i - 1]} → {path[i]} is not one step");
                if (dx != 0 && dy != 0)
                {
                    Assert.That(map.IsWalkable(new GridCell(path[i - 1].X + dx, path[i - 1].Y)), "cut a corner");
                    Assert.That(map.IsWalkable(new GridCell(path[i - 1].X, path[i - 1].Y + dy)), "cut a corner");
                }
            }
        }

        [Test]
        public void OpenRoom_StraightLine_IsOneCellPerStep()
        {
            var map = Map(".....", ".....", ".....");
            var path = Find(map, new GridCell(0, 1), new GridCell(4, 1), out bool found);
            Assert.That(found);
            Assert.That(path, Has.Count.EqualTo(5));
            Assert.That(path[0], Is.EqualTo(new GridCell(0, 1)));
            Assert.That(path[4], Is.EqualTo(new GridCell(4, 1)));
        }

        [Test]
        public void OpenRoom_UsesDiagonals()
        {
            var map = Map("....", "....", "....", "....");
            var path = Find(map, new GridCell(0, 0), new GridCell(3, 3), out bool found);
            Assert.That(found);
            Assert.That(path, Has.Count.EqualTo(4));
            Assert.That(GridPathfinder.Length(path), Is.EqualTo(3 * 1.41421356f).Within(1e-4f));
        }

        [Test]
        public void StartEqualsGoal_ReturnsThatCell()
        {
            var map = Map("...");
            var path = Find(map, new GridCell(1, 0), new GridCell(1, 0), out bool found);
            Assert.That(found);
            Assert.That(path, Is.EqualTo(new[] { new GridCell(1, 0) }));
        }

        [Test]
        public void WalksAroundAWall()
        {
            var map = Map(
                ".....",
                ".###.",
                ".....");
            var path = Find(map, new GridCell(2, 0), new GridCell(2, 2), out bool found);
            Assert.That(found);
            AssertStepsAreLegal(map, path);
            Assert.That(path[0], Is.EqualTo(new GridCell(2, 0)));
            Assert.That(path[path.Count - 1], Is.EqualTo(new GridCell(2, 2)));
        }

        [Test]
        public void DoesNotCutCorners()
        {
            // The only diagonal from (0,0) to (1,1) squeezes between two blocked cells.
            var map = Map(
                "#.",
                ".#");
            Find(map, new GridCell(0, 0), new GridCell(1, 1), out bool found);
            Assert.That(found, Is.False);
        }

        [Test]
        public void DiagonalPastASingleCorner_GoesAroundIt()
        {
            var map = Map(
                "..",
                ".#");
            var path = Find(map, new GridCell(0, 0), new GridCell(1, 1), out bool found);
            Assert.That(found);
            AssertStepsAreLegal(map, path);
            Assert.That(path, Is.EqualTo(new[] { new GridCell(0, 0), new GridCell(0, 1), new GridCell(1, 1) }));
        }

        [Test]
        public void SealedOffGoal_IsUnreachable_AndClearsThePath()
        {
            var map = Map(
                "..#.",
                "..#.",
                "..#.");
            var path = new List<GridCell> { new GridCell(9, 9) };
            bool found = GridPathfinder.TryFindPath(map, new GridCell(0, 0), new GridCell(3, 0), path);
            Assert.That(found, Is.False);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void BlockedOrOutOfBoundsEnds_Fail()
        {
            var map = Map(".#.");
            Find(map, new GridCell(0, 0), new GridCell(1, 0), out bool toBlocked);
            Find(map, new GridCell(1, 0), new GridCell(0, 0), out bool fromBlocked);
            Find(map, new GridCell(0, 0), new GridCell(5, 0), out bool outside);
            Assert.That(toBlocked, Is.False);
            Assert.That(fromBlocked, Is.False);
            Assert.That(outside, Is.False);
        }

        [Test]
        public void FindsTheShortestOfTwoRoutes()
        {
            // The gap on the right is closer to both ends than the one on the left.
            var map = Map(
                ".......",
                ".####..",
                ".......");
            var path = Find(map, new GridCell(4, 0), new GridCell(4, 2), out bool found);
            Assert.That(found);
            AssertStepsAreLegal(map, path);
            // Around the wall's end without cutting its corner: right, up, up, left.
            Assert.That(path, Does.Contain(new GridCell(5, 1)));
            Assert.That(path, Has.Count.EqualTo(5));
        }

        [Test]
        public void SameQuery_GivesTheSamePath()
        {
            var map = Map(
                "......",
                ".##...",
                "......",
                "...##.",
                "......");
            var a = Find(map, new GridCell(0, 0), new GridCell(5, 4), out _);
            var b = Find(map, new GridCell(0, 0), new GridCell(5, 4), out _);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void TavernSizedRoom_WithTables_FindsALegalPath()
        {
            var map = new GridMap(40, 22);
            for (int x = 4; x < 36; x += 6)
            for (int y = 4; y < 18; y += 5)
            {
                map.SetBlocked(x, y, true);
                map.SetBlocked(x + 1, y, true);
                map.SetBlocked(x, y + 1, true);
                map.SetBlocked(x + 1, y + 1, true);
            }
            var path = Find(map, new GridCell(0, 0), new GridCell(39, 21), out bool found);
            Assert.That(found);
            AssertStepsAreLegal(map, path);
        }
    }
}
