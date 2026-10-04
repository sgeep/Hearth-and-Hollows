using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Dungeon.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>4d step 1: room layouts, their checks, and the seal-until-clear rule.</summary>
    public class RoomTests
    {
        /// <summary>A valid 40×24 room: an exit at x 19–20, the entrance below it, the arrival, a slime and a bat under the wall.</summary>
        static List<string> Room(int width = 40, int height = 24)
        {
            var rows = new List<string>();
            for (int r = 0; r < height; r++)
            {
                char[] row = new string(r < 2 || r >= height - 2 ? '#' : '.', width).ToCharArray();
                row[0] = row[width - 1] = '#';
                rows.Add(new string(row));
            }
            Set(rows, 19, 0, 'x'); Set(rows, 20, 0, 'x'); Set(rows, 19, 1, 'x'); Set(rows, 20, 1, 'x');
            Set(rows, 19, height - 2, 'e'); Set(rows, 20, height - 2, 'e'); Set(rows, 19, height - 1, 'e'); Set(rows, 20, height - 1, 'e');
            Set(rows, 19, height - 4, 'P');
            Set(rows, 10, 10, 'S');
            Set(rows, 30, 2, 'V');
            return rows;
        }

        /// <summary>Sets the character at text column <paramref name="x"/>, text row <paramref name="row"/> (0 = top).</summary>
        static void Set(List<string> rows, int x, int row, char c)
        {
            char[] chars = rows[row].ToCharArray();
            chars[x] = c;
            rows[row] = new string(chars);
        }

        static void AssertProblem(List<string> rows, string fragment)
        {
            var layout = new RoomLayout(rows);
            Assert.That(layout.IsValid, Is.False, "should be rejected");
            Assert.That(layout.Problems, Has.Some.Contains(fragment), string.Join("\n", layout.Problems));
        }

        [Test]
        public void AValidRoom_ParsesItsSize_Exits_Entrance_Arrival_AndEnemies()
        {
            var layout = new RoomLayout(Room());
            Assert.That(layout.IsValid, string.Join("\n", layout.Problems));
            Assert.That(layout.Width, Is.EqualTo(40));
            Assert.That(layout.Height, Is.EqualTo(24));
            Assert.That(layout.Exits, Has.Count.EqualTo(1));
            Assert.That(layout.Exits[0].Cells, Is.EqualTo(new RectInt(19, 22, 2, 2)), "through the north wall's two rows (y up)");
            Assert.That(layout.Entrance?.Cells, Is.EqualTo(new RectInt(19, 0, 2, 2)));
            Assert.That(layout.Arrival, Is.EqualTo(new Vector2Int(19, 3)));
            Assert.That(layout.Enemies.Select(e => e.kind), Is.EquivalentTo(new[] { 'S', 'V' }));
            Assert.That(layout.At(-1, 0), Is.EqualTo('#'), "outside is wall");
        }

        [Test]
        public void Exits_AreOrderedLeftToRight()
        {
            List<string> rows = Room(44, 24);
            foreach (int x in new[] { 30, 31 })
            {
                Set(rows, x, 0, 'x');
                Set(rows, x, 1, 'x');
            }
            Set(rows, 30, 2, '.');
            var layout = new RoomLayout(rows);
            Assert.That(layout.IsValid, string.Join("\n", layout.Problems));
            Assert.That(layout.Exits.Select(e => e.Cells.x), Is.EqualTo(new[] { 19, 30 }));
        }

        [Test]
        public void Rows_MustAllBeTheSameWidth() { List<string> rows = Room(); rows[5] += "."; AssertProblem(rows, "wide"); }

        [Test]
        public void UnknownCharacters_AreRejected() { List<string> rows = Room(); Set(rows, 5, 5, '?'); AssertProblem(rows, "unknown character"); }

        [Test]
        public void Rooms_AreOneToOneAndAHalfScreens() => AssertProblem(Room(36, 24), "1–1.5 screens");

        [Test]
        public void TheBorder_MustBeClosed() { List<string> rows = Room(); Set(rows, 0, 10, '.'); AssertProblem(rows, "border is open"); }

        [Test]
        public void AnExit_MustBeTwoByTwo_ThroughTheNorthWall()
        {
            List<string> rows = Room();
            Set(rows, 20, 0, '#');
            Set(rows, 20, 1, '#');
            AssertProblem(rows, "2×2 through the north wall");
        }

        [Test]
        public void ARoom_NeedsExactlyOneEntrance_AndOneArrival()
        {
            List<string> rows = Room();
            Set(rows, 19, 23, '#'); Set(rows, 20, 23, '#'); Set(rows, 19, 22, '#'); Set(rows, 20, 22, '#');
            AssertProblem(rows, "exactly one entrance");
            rows = Room();
            Set(rows, 19, 20, '.');
            AssertProblem(rows, "exactly one P");
        }

        [Test]
        public void ARoom_NeedsAnExit_OrTheRope()
        {
            List<string> rows = Room();
            foreach (int x in new[] { 19, 20 })
            {
                Set(rows, x, 0, '#');
                Set(rows, x, 1, '#');
            }
            AssertProblem(rows, "needs an exit, the rope out or the hole down");
            Set(rows, 25, 12, 'R');
            Assert.That(new RoomLayout(rows).IsValid, "the rope room is fine with no exits");
        }

        [Test]
        public void EverythingImportant_MustBeReachableFromTheArrival()
        {
            List<string> rows = Room();
            // A wall right across the room, below the exit and the enemies.
            for (int x = 1; x < 39; x++) Set(rows, x, 15, '#');
            AssertProblem(rows, "can't be reached");
        }

        [Test]
        public void ATable_BlocksTheTileToItsRightToo()
        {
            List<string> rows = Room();
            Set(rows, 5, 5, 'T');
            var layout = new RoomLayout(rows);
            Assert.That(layout.IsWalkable(5, 18), Is.False);
            Assert.That(layout.IsWalkable(6, 18), Is.False);
            Assert.That(layout.IsWalkable(7, 18), Is.True);
        }

        [Test]
        public void ABat_MustHangUnderAWall() { List<string> rows = Room(); Set(rows, 12, 10, 'V'); AssertProblem(rows, "no wall directly above"); }

        // ------------------------------------------------------------------ encounter

        [Test]
        public void AnEncounter_StaysSealed_UntilTheLastEnemyFalls_AndClearsOnce()
        {
            var encounter = new RoomEncounter(3);
            int cleared = 0;
            encounter.Cleared += () => cleared++;
            Assert.That(encounter.IsSealed);
            Assert.That(encounter.Update(2), Is.False);
            Assert.That(encounter.Update(1), Is.False);
            Assert.That(encounter.IsSealed);
            Assert.That(encounter.Update(0), Is.True, "the call that clears it");
            Assert.That(encounter.IsCleared);
            Assert.That(encounter.Update(0), Is.False, "only once");
            Assert.That(cleared, Is.EqualTo(1));
        }

        [Test]
        public void ARoomWithNoEnemies_IsClearFromTheStart()
        {
            var encounter = new RoomEncounter(0);
            bool raised = false;
            encounter.Cleared += () => raised = true;
            Assert.That(encounter.IsCleared);
            Assert.That(encounter.Update(0), Is.False);
            Assert.That(raised, Is.False, "nothing to clear");
        }

        // ------------------------------------------------------------------ the built rooms

        static IEnumerable<RoomDefinition> Definitions() =>
            AssetDatabase.FindAssets("t:RoomDefinition").Select(g => AssetDatabase.LoadAssetAtPath<RoomDefinition>(AssetDatabase.GUIDToAssetPath(g)));

        [Test]
        public void EveryRoom_IsValid_AndItsPrefabMatchesItsLayout()
        {
            RoomDefinition[] rooms = Definitions().ToArray();
            Assert.That(rooms, Is.Not.Empty, "Run Hearthdelve > Generate > 4d Dungeon (Rooms).");
            foreach (RoomDefinition room in rooms)
            {
                RoomLayout layout = room.Parse();
                Assert.That(layout.IsValid, $"{room.name}:\n" + string.Join("\n", layout.Problems));
                Assert.That(room.id, Is.Not.Empty, room.name);
                Assert.That(room.prefab, Is.Not.Null, $"{room.name} has no prefab");
                Assert.That(room.prefab.Size, Is.EqualTo(new Vector2Int(layout.Width, layout.Height)), room.name);
                Assert.That(room.prefab.Exits, Has.Length.EqualTo(layout.Exits.Count), room.name);
                Assert.That(room.prefab.Exits.Select(e => e.Index), Is.EqualTo(Enumerable.Range(0, layout.Exits.Count)), $"{room.name}: exits numbered left to right");
                Assert.That(room.prefab.Arrival, Is.Not.Null, room.name);
                Assert.That(room.prefab.GetComponentsInChildren<Hearthdelve.Dungeon.Enemies.EnemyIdentity>(true), Has.Length.EqualTo(layout.Enemies.Count),
                    $"{room.name}: an enemy per marker");
            }
        }

        [Test]
        public void TheCamera_StaysInsideTheRoom_OrCentresOnIt()
        {
            var room = new Rect(0f, 0f, 44f, 26f);
            // A 40×22.5 view (320×180 at 8 PPU) in a 44×26 room: halves of 20 and 11.25.
            Assert.That(RoomView.Clamp(room, new Vector2(1.5f, 2.5f), 20f, 11.25f), Is.EqualTo(new Vector2(20f, 11.25f)), "the bottom-left corner");
            Assert.That(RoomView.Clamp(room, new Vector2(43f, 25f), 20f, 11.25f), Is.EqualTo(new Vector2(24f, 14.75f)), "the top-right corner");
            Assert.That(RoomView.Clamp(room, new Vector2(22f, 13f), 20f, 11.25f), Is.EqualTo(new Vector2(22f, 13f)), "free in the middle");
            Assert.That(RoomView.Clamp(room, new Vector2(5f, 13f), 30f, 11.25f), Is.EqualTo(new Vector2(22f, 13f)), "wider than the room: centred");
        }

        [Test]
        public void EveryRoom_HasWhatItsKindNeeds()
        {
            foreach (RoomDefinition room in Definitions())
            {
                RoomLayout layout = room.Parse();
                string at = $"{room.name} ({room.kind})";
                switch (room.kind)
                {
                    case RoomKind.Start:
                        Assert.That(layout.Exits, Is.Not.Empty, at);
                        Assert.That(layout.Enemies.Count + layout.GroundSpawns.Count, Is.Zero, $"{at}: quiet");
                        break;
                    case RoomKind.Combat:
                        Assert.That(layout.Exits, Is.Not.Empty, at);
                        Assert.That(layout.GroundSpawns.Count, Is.GreaterThanOrEqualTo(6), $"{at}: room for the deepest floor's fights");
                        Assert.That(layout.Rope == null && layout.Hole == null, at);
                        break;
                    case RoomKind.Extraction:
                        Assert.That(layout.Rope, Is.Not.Null, at);
                        Assert.That(layout.Exits, Is.Empty, $"{at}: the run ends here");
                        break;
                    case RoomKind.Descent:
                        Assert.That(layout.Hole, Is.Not.Null, at);
                        Assert.That(layout.Exits, Is.Empty, $"{at}: the way on is the hole");
                        Assert.That(room.prefab.Descent, Is.Not.Null, at);
                        break;
                    case RoomKind.Arena:
                        Assert.That(layout.Rope, Is.Not.Null, $"{at}: the way home once it's clear");
                        Assert.That(layout.Exits, Is.Empty, at);
                        Assert.That(layout.GroundSpawns.Count, Is.GreaterThanOrEqualTo(8), at);
                        break;
                }
            }
        }

        [Test]
        public void SpawnPoints_AreParsed_InOrder_AndPerchesMustHangUnderAWall()
        {
            List<string> rows = Room();
            Set(rows, 5, 10, 'm');
            Set(rows, 25, 15, 'm');
            Set(rows, 8, 2, 'v');
            var layout = new RoomLayout(rows);
            Assert.That(layout.IsValid, string.Join("\n", layout.Problems));
            Assert.That(layout.GroundSpawns, Is.EqualTo(new[] { new Vector2Int(25, 8), new Vector2Int(5, 13) }), "bottom row first");
            Assert.That(layout.PerchSpawns, Is.EqualTo(new[] { new Vector2Int(8, 21) }));
            Set(rows, 12, 12, 'v');
            AssertProblem(rows, "no wall directly above");
        }

        [Test]
        public void ARoom_CanEndInAHole()
        {
            List<string> rows = Room();
            foreach (int x in new[] { 19, 20 })
            {
                Set(rows, x, 0, '#');
                Set(rows, x, 1, '#');
            }
            Set(rows, 19, 10, 'H');
            var layout = new RoomLayout(rows);
            Assert.That(layout.IsValid, string.Join("\n", layout.Problems));
            Assert.That(layout.Hole, Is.EqualTo(new Vector2Int(19, 13)));
        }

        [Test]
        public void RoomIds_AreUnique()
        {
            string[] ids = Definitions().Select(r => r.id).ToArray();
            Assert.That(ids, Is.Unique);
        }
    }
}
