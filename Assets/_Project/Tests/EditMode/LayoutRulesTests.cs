using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4f step 2: placement rules (D1, D5, D6) and the layout check (D13), on the real starting tavern: generous rules
    /// with a reason each, and only problems that make service impossible keep the doors shut.
    /// </summary>
    public class LayoutRulesTests
    {
        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");

        /// <summary>The tavern's shape and service points, as the scene configures them.</summary>
        static AreaShape Tavern()
        {
            var shape = new AreaShape
            {
                Id = "tavern",
                Kind = AreaKind.Tavern,
                Origin = Vector2.zero,
                Bounds = new RectInt(0, 0, 28, 17),
                Floor = new RectInt(1, 2, 26, 12),
                WallBand = new RectInt(1, 14, 26, 3),
                Reserved = new HashSet<Vector2Int> { new(13, 2), new(13, 3) },
                Door = new Vector2(13.5f, 2.4f),
                Rest = new Vector2(25.5f, 5.5f),
                // The stairs up to the guest room (Checkpoint B): their cells kept clear, their flight solid.
                Fixtures = { new Rect(9f, 12f, 2.25f, 2f) },
            };
            for (int x = 9; x < 13; x++)
            for (int y = 12; y < 14; y++)
                shape.Reserved.Add(new Vector2Int(x, y));
            for (int i = 0; i < 6; i++) shape.Queue.Add(new Vector2(12.25f - i, 2.6f));
            return shape;
        }

        static FurnitureLayout Starting() => new(Tavern(), Database.Furniture, Database.startingFurniture.Layout("tavern"));

        static PlacedFurniture Piece(FurnitureLayout layout, string id, Vector2Int cell) =>
            layout.Pieces.First(p => p.definition == id && p.cell == cell);

        static PlacedFurniture New(string id, int x, int y, int turns = 0) =>
            new() { uid = 900 + x * 31 + y, definition = id, cell = new Vector2Int(x, y), turns = turns };

        // ---------- The walkable grid ----------

        /// <summary>The check's grid, built from bodies by NavGrid's own rule, is the grid the starting room bakes (recorded in PlayMode).</summary>
        [Test]
        public void TheChecksGrid_IsTheStartingRoomsBakedGrid()
        {
            string[] baseline = File.ReadAllLines("Assets/_Project/Tests/PlayMode/Baselines/TavernStarting.txt")
                .Where(l => l.StartsWith("grid|") && !l.StartsWith("grid|bounds")).ToArray();
            FurnitureLayout layout = Starting();
            GridMap map = LayoutCheck.Walkable(layout.Shape, layout.ResolveAll());
            foreach (string line in baseline)
            {
                string[] parts = line.Split('|');
                int y = int.Parse(parts[1]);
                var row = new string(Enumerable.Range(0, map.Width).Select(x => map.IsWalkable(new GridCell(x, y)) ? '.' : '#').ToArray());
                Assert.That(row, Is.EqualTo(parts[2]), $"row {y}");
            }
        }

        // ---------- The check (D13) ----------

        [Test]
        public void TheStartingTavern_IsReadyForService()
        {
            FurnitureLayout layout = Starting();
            LayoutReport report = LayoutCheck.For(layout.Shape, layout.ResolveAll());
            Assert.That(report.Issues, Is.Empty, string.Join(", ", report.Issues.Select(i => i.Kind)));
            Assert.That(report.CanOpen);
            Assert.That((report.Seats, report.ReachableSeats), Is.EqualTo((6, 6)));
        }

        [Test]
        public void AStoredStation_KeepsTheDoorsShut_AndSaysWhich()
        {
            FurnitureLayout layout = Starting();
            layout.Remove(Piece(layout, "kitchen_range", new Vector2Int(19, 12)).uid);
            LayoutReport report = LayoutCheck.For(layout.Shape, layout.ResolveAll());
            Assert.That(report.CanOpen, Is.False);
            LayoutIssue issue = report.Issues.Single();
            Assert.That((issue.Kind, issue.Station, issue.Blocking), Is.EqualTo((LayoutIssueKind.StationMissing, StationKind.Grill, true)));
        }

        [Test]
        public void AWalledOffStation_KeepsTheDoorsShut()
        {
            FurnitureLayout layout = Starting();
            // Barrels all round the stew pot's front and back spots and its sides.
            foreach (var (x, y) in new[] { (23, 10), (24, 10), (25, 10), (26, 10), (23, 11), (23, 12), (23, 13), (26, 11), (26, 12), (26, 13), (24, 13), (25, 13) })
                if (layout.Check(New("cellar_barrel", x, y)).IsValid) layout.Add(New("cellar_barrel", x, y));
            LayoutReport report = LayoutCheck.For(layout.Shape, layout.ResolveAll());
            Assert.That(report.Issues.Any(i => i.Kind == LayoutIssueKind.StationUnreachable && i.Station == StationKind.StewPot && i.Blocking));
            Assert.That(report.CanOpen, Is.False);
        }

        [Test]
        public void NoChairFacingATable_KeepsTheDoorsShut()
        {
            FurnitureLayout layout = Starting();
            foreach (PlacedFurniture chair in layout.Pieces.Where(p => p.definition == "tavern_chair").ToList()) layout.Remove(chair.uid);
            LayoutReport report = LayoutCheck.For(layout.Shape, layout.ResolveAll());
            Assert.That(report.Issues.Single().Kind, Is.EqualTo(LayoutIssueKind.NoSeats));
            Assert.That(report.CanOpen, Is.False);
        }

        [Test]
        public void PartialProblems_AreWarnings_TheDoorsStillOpen()
        {
            FurnitureLayout layout = Starting();
            // A barrel in the queue by the door, and barrels hemming in the first table's west chair.
            layout.Add(New("cellar_barrel", 10, 2));
            foreach (var (x, y) in new[] { (2, 5), (3, 5), (4, 5), (2, 6), (4, 6) }) layout.Add(New("cellar_barrel", x, y));
            LayoutReport report = LayoutCheck.For(layout.Shape, layout.ResolveAll());
            Assert.That(report.CanOpen, "warnings never keep the doors shut (D13)");
            Assert.That(report.Issues.Any(i => i.Kind == LayoutIssueKind.QueueBlocked && !i.Blocking && i.Count >= 1));
            Assert.That(report.Issues.Any(i => i.Kind == LayoutIssueKind.SeatsUnreachable && !i.Blocking), string.Join(", ", report.Issues.Select(i => i.Kind)));
            Assert.That(report.ReachableSeats, Is.LessThan(report.Seats));
        }

        // ---------- Placement rules ----------

        [Test]
        public void TheStartingLayout_IsValid_PieceByPiece()
        {
            FurnitureLayout layout = Starting();
            foreach (PlacedFurniture p in layout.Pieces)
                Assert.That(layout.Check(p).Problem, Is.EqualTo(PlacementProblem.None), $"{p.definition}#{p.uid}");
        }

        [Test]
        public void StandingPieces_StayOnTheFloor_OffTheEntrance_AndApart()
        {
            FurnitureLayout layout = Starting();
            Assert.That(layout.Check(New("cellar_barrel", 0, 5)).Problem, Is.EqualTo(PlacementProblem.OutsideTheRoom), "into the side wall");
            Assert.That(layout.Check(New("cellar_barrel", 10, 14)).Problem, Is.EqualTo(PlacementProblem.OutsideTheRoom), "into the back wall");
            Assert.That(layout.Check(New("cellar_barrel", 13, 3)).Problem, Is.EqualTo(PlacementProblem.BlocksTheEntrance));
            Assert.That(layout.Check(New("cellar_barrel", 4, 7)).Problem, Is.EqualTo(PlacementProblem.Overlaps), "on the first table");
            Assert.That(layout.Check(New("cellar_barrel", 12, 5)).IsValid, "open floor");
        }

        [Test]
        public void Bodies_MayNotOverlap_EvenWhenFootprintsDont()
        {
            FurnitureLayout layout = Starting();
            // An east-facing chair on the tile up and right of the third table: its own tile, but its body reaches the table's.
            PlacementCheck check = layout.Check(New("tavern_chair", 19, 5, 1));
            Assert.That(check.Problem, Is.EqualTo(PlacementProblem.Overlaps));
            Assert.That(check.Blocker, Is.EqualTo(Piece(layout, "table_round_a", new Vector2Int(18, 4)).uid));
        }

        /// <summary>The Checkpoint A playtest: a barrel couldn't go back beside the others. Barrels on neighbouring tiles touch.</summary>
        [Test]
        public void Barrels_StandOnNeighbouringTiles()
        {
            FurnitureLayout layout = Starting();
            Assert.That(layout.Check(New("cellar_barrel", 24, 8)).IsValid, "beside the barrel at (25, 8)");
            Assert.That(layout.Check(New("cellar_barrel", 25, 9)).IsValid, "on top of it, beside the one at (26, 9)");
            PlacedFurniture moved = Piece(layout, "cellar_barrel", new Vector2Int(25, 8));
            layout.Remove(moved.uid);
            Assert.That(layout.Check(moved).IsValid, "and back where it came from");
        }

        [Test]
        public void TheGlasses_StandOnTheLowShelf_AndMoveWithIt()
        {
            FurnitureLayout layout = Starting();
            PlacedFurniture shelf = Piece(layout, "low_shelf", new Vector2Int(24, 14));
            PlacedFurniture glasses = layout.Pieces.Single(p => p.definition == "shelf_glasses");
            Assert.That((glasses.host, glasses.anchor), Is.EqualTo((shelf.uid, 0)));
            Vector2 before = layout.ResolveAll().Single(r => r.Placement.uid == glasses.uid).Art[0].Position;
            Assert.That(Vector2.Distance(before, new Vector2(24.75f, 15.5f)), Is.LessThan(1e-4f), "where 4e put them");

            List<PlacedFurniture> lifted = layout.Remove(shelf.uid);
            Assert.That(lifted.Select(p => p.definition), Is.EquivalentTo(new[] { "low_shelf", "shelf_glasses" }), "a shelf's glasses come away with it");
            shelf.cell = new Vector2Int(5, 15);
            foreach (PlacedFurniture p in lifted) layout.Add(p);
            Vector2 after = layout.ResolveAll().Single(r => r.Placement.uid == glasses.uid).Art[0].Position;
            Assert.That(Vector2.Distance(after, new Vector2(5.75f, 16.5f)), Is.LessThan(1e-4f), "and stand on it wherever it goes");

            PlacedFurniture loose = New("shelf_glasses", 0, 0);
            Assert.That(layout.Check(loose).Problem, Is.EqualTo(PlacementProblem.NeedsASurface), "glasses need a surface");
            Assert.That(layout.NearestSurface(new Vector2(5.9f, 16.2f)), Is.EqualTo((shelf.uid, 0)), "the shelf's surface is in reach");
            loose.host = shelf.uid;
            Assert.That(layout.Check(loose).Problem, Is.EqualTo(PlacementProblem.Overlaps), "one row of glasses per surface");
        }

        /// <summary>The Checkpoint A playtest: picking by what's drawn (a chair sits a quarter tile off its cell).</summary>
        [Test]
        public void PointingPicks_WhatsDrawnThere()
        {
            FurnitureLayout layout = Starting();
            // The first table's west chair is drawn from x 2.875 to 3.625: its left edge is over the tile to the west.
            Assert.That(layout.AtPoint(new Vector2(2.95f, 7.6f)).First().definition, Is.EqualTo("tavern_chair"));
            Assert.That(layout.At(new Vector2Int(2, 7)), Is.Empty, "no piece's tile there");
            Assert.That(layout.AtPoint(new Vector2(24.9f, 15.6f)).First().definition, Is.EqualTo("shelf_glasses"), "the glasses before their shelf");
        }

        [Test]
        public void WallPieces_HangOnTheBackWall_AndTheKitchenRangeStandsAgainstIt()
        {
            FurnitureLayout layout = Starting();
            Assert.That(layout.Check(New("wall_sign", 6, 8)).Problem, Is.EqualTo(PlacementProblem.NotOnTheWall));
            Assert.That(layout.Check(New("wall_sign", 8, 16)).IsValid, "higher up the wall");
            Assert.That(layout.Check(New("wall_sign", 12, 15)).Problem, Is.EqualTo(PlacementProblem.Overlaps), "half over the sign already there");

            PlacedFurniture range = Piece(layout, "kitchen_range", new Vector2Int(19, 12)).Clone();
            range.cell = new Vector2Int(9, 6);
            Assert.That(layout.Check(range).Problem, Is.EqualTo(PlacementProblem.NotAgainstTheBackWall), "D6: the Grill is wall-bound");
            range.cell = new Vector2Int(13, 12);
            Assert.That(layout.Check(range).IsValid, "it slides along the back wall");
        }

        [Test]
        public void Turning_FollowsEachPiecesRotationMode()
        {
            FurnitureLayout layout = Starting();
            PlacedFurniture table = New("table_round_a", 12, 5);
            table.turns = 1;
            Assert.That(layout.Check(table).Problem, Is.EqualTo(PlacementProblem.CannotStandThatWay), "a round table doesn't turn");
            for (int t = 0; t < 4; t++)
                Assert.That(layout.Check(New("tavern_chair", 12, 5, t)).IsValid, $"the chair stands at each of its drawn facings ({t})");
            PlacedFurniture mirrored = New("cellar_barrel", 12, 5);
            mirrored.flipped = true;
            Assert.That(layout.Check(mirrored).Problem, Is.EqualTo(PlacementProblem.CannotStandThatWay), "D3: flipping is opt-in");
        }

        [Test]
        public void AtFindsTheTopPieceFirst()
        {
            FurnitureLayout layout = Starting();
            List<PlacedFurniture> here = layout.At(new Vector2Int(4, 7));
            Assert.That(here.First().definition, Is.EqualTo("table_round_a"));
            Assert.That(layout.At(new Vector2Int(15, 15)).Single().definition, Is.EqualTo("wall_fireplace"));
            Assert.That(layout.At(new Vector2Int(12, 5)), Is.Empty);
        }
    }
}
