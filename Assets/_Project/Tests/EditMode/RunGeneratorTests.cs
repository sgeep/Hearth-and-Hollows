using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Dungeon.Rooms;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4d step 2: the seeded run generator, checked across hundreds of seeds against the real Cellars rooms and the
    /// real tuning: every floor is a valid one-way graph, every route can reach a rope, the floors lead down to the
    /// arena, rooms fit their places and encounters fit their rooms.
    /// </summary>
    public class RunGeneratorTests
    {
        const string SettingsPath = "Assets/_Project/Data/Dungeon/RunSettings.asset";
        static readonly IEnumerable<int> k_Seeds = Enumerable.Range(1, 300);

        static RunSettings Settings
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<RunSettings>(SettingsPath);
                Assert.That(settings, Is.Not.Null, "Run Hearthdelve > Generate > 4d Dungeon (Rooms).");
                return settings;
            }
        }

        static RunGraph Generate(int seed) => RunGenerator.Generate(seed, Settings.tuning, Settings.Catalog());

        [Test]
        public void TheSameSeed_GivesTheSameRun_AndSeedsDiffer()
        {
            Assert.That(Generate(42).Describe(), Is.EqualTo(Generate(42).Describe()));
            int distinct = k_Seeds.Take(30).Select(s => Generate(s).Describe().Substring(Generate(s).Describe().IndexOf('\n'))).Distinct().Count();
            Assert.That(distinct, Is.GreaterThan(25), "different seeds give different runs");
        }

        [Test]
        public void EveryFloor_IsAConnectedOneWayGraph_FromItsFirstRoomToItsEnds()
        {
            foreach (int seed in k_Seeds)
            {
                RunGraph run = Generate(seed);
                Assert.That(run.Floors, Has.Count.EqualTo(3));
                foreach (FloorGraph floor in run.Floors)
                {
                    string at = $"seed {seed}, floor {floor.Floor}";
                    Assert.That(floor.Start.Id, Is.Zero, at);
                    Assert.That(floor.Start.Kind, Is.EqualTo(floor.Floor == 1 ? RoomKind.Start : RoomKind.Combat), $"{at}: the first floor opens quietly, deeper ones with a fight");
                    Assert.That(RunGraph.Reachable(floor), Has.Count.EqualTo(floor.Nodes.Count), $"{at}: no disconnected rooms");
                    foreach (FloorNode node in floor.Nodes)
                    {
                        if (node.IsEnd) Assert.That(node.Next, Is.Empty, $"{at}: an end leads nowhere");
                        else Assert.That(node.Next, Is.Not.Empty, $"{at}: node {node.Id} is a dead end");
                        Assert.That(node.Next, Is.Unique, at);
                        foreach (int next in node.Next)
                            Assert.That(floor.Node(next).Layer, Is.GreaterThan(node.Layer), $"{at}: one way only (no exit leads back)");
                    }
                }
            }
        }

        [Test]
        public void TheFloorsLeadDown_AndTheLastEndsInTheArena()
        {
            foreach (int seed in k_Seeds)
            {
                RunGraph run = Generate(seed);
                for (int f = 0; f < 3; f++)
                {
                    FloorGraph floor = run.Floors[f];
                    Assert.That(RunGraph.Ends(floor, RoomKind.Descent).Count(), Is.EqualTo(f < 2 ? 1 : 0), $"seed {seed}, floor {f + 1}: the hole down");
                    Assert.That(RunGraph.Ends(floor, RoomKind.Arena).Count(), Is.EqualTo(f == 2 ? 1 : 0), $"seed {seed}, floor {f + 1}: the arena");
                }
            }
        }

        [Test]
        public void EveryRoute_CanTakeARope_BeforeLeavingTheFloor()
        {
            foreach (int seed in k_Seeds)
            foreach (FloorGraph floor in Generate(seed).Floors)
            {
                Assert.That(RunGraph.Ends(floor, RoomKind.Extraction), Is.Not.Empty, $"seed {seed}, floor {floor.Floor}");
                // Whoever reaches the way on (hole or arena) is offered a rope right beside it.
                foreach (FloorNode node in floor.Nodes.Where(n => n.Next.Any(next => floor.Node(next).Kind is RoomKind.Descent or RoomKind.Arena)))
                    Assert.That(node.Next.Any(next => floor.Node(next).Kind == RoomKind.Extraction), $"seed {seed}, floor {floor.Floor}: node {node.Id} offers no rope");
            }
        }

        [Test]
        public void ARoute_HasThreeToFourFights_PerFloor_AndEveryFloorOffersAChoice()
        {
            RunSettings settings = Settings;
            var runTotals = new List<int>();
            foreach (int seed in k_Seeds)
            {
                RunGraph run = Generate(seed);
                int shortestRun = 0;
                foreach (FloorGraph floor in run.Floors)
                {
                    FloorTuning tuning = settings.tuning.Floor(floor.Floor - 1);
                    var (shortest, longest) = RunGraph.FightsOnTheWayOn(floor);
                    Assert.That(shortest, Is.GreaterThanOrEqualTo(tuning.minFights), $"seed {seed}, floor {floor.Floor}");
                    Assert.That(longest, Is.LessThanOrEqualTo(tuning.maxFights), $"seed {seed}, floor {floor.Floor}");
                    shortestRun += shortest;
                    Assert.That(floor.Nodes.Any(n => n.Next.Count(next => floor.Node(next).Kind == RoomKind.Combat) >= 2),
                        $"seed {seed}, floor {floor.Floor}: a choice between fights");
                }
                runTotals.Add(shortestRun);
            }
            Assert.That(runTotals.Min(), Is.GreaterThanOrEqualTo(9));
            Assert.That(runTotals.Max(), Is.LessThanOrEqualTo(12));
        }

        [Test]
        public void Rooms_FitTheirPlaces_AndEncounters_FitTheirRooms()
        {
            RunSettings settings = Settings;
            Dictionary<string, RoomCatalogEntry> rooms = settings.Catalog().ToDictionary(r => r.Id);
            foreach (int seed in k_Seeds)
            foreach (FloorGraph floor in Generate(seed).Floors)
            {
                FloorTuning tuning = settings.tuning.Floor(floor.Floor - 1);
                var fightsOnThisFloor = new List<string>();
                foreach (FloorNode node in floor.Nodes)
                {
                    string at = $"seed {seed}, floor {floor.Floor}, node {node.Id}";
                    Assert.That(rooms.ContainsKey(node.RoomId), at);
                    RoomCatalogEntry room = rooms[node.RoomId];
                    Assert.That(room.Kind, Is.EqualTo(node.Kind), at);
                    Assert.That(room.Exits, Is.GreaterThanOrEqualTo(node.Next.Count), $"{at}: enough exits");
                    foreach (EncounterSpawn spawn in node.Encounter)
                        Assert.That(spawn.Point, Is.LessThan(spawn.Kind == EnemyKind.Bat ? room.PerchSpawns : room.GroundSpawns), $"{at}: a real spawn point");
                    Assert.That(node.Encounter.Select(e => (e.Kind == EnemyKind.Bat, e.Point)), Is.Unique, $"{at}: one enemy per point");
                    if (node.Kind == RoomKind.Combat)
                    {
                        Assert.That(node.Encounter.Count, Is.InRange(1, tuning.maxEnemies), at);
                        fightsOnThisFloor.Add(node.RoomId);
                    }
                    else if (node.Kind != RoomKind.Arena) Assert.That(node.Encounter, Is.Empty, $"{at}: quiet");
                }
                // A layout repeats on a floor only when the floor needs more rooms with enough exits than the pool has.
                Assert.That(fightsOnThisFloor.GroupBy(r => r).Max(g => g.Count()), Is.LessThanOrEqualTo(2), $"seed {seed}, floor {floor.Floor}: a fight room more than twice");
            }
        }

        [Test]
        public void TheArena_HoldsThePlaceholderFight()
        {
            ArenaPlaceholder placeholder = Settings.tuning.arenaPlaceholder;
            foreach (int seed in k_Seeds.Take(50))
            {
                FloorNode arena = RunGraph.Ends(Generate(seed).Floors[2], RoomKind.Arena).Single();
                Assert.That(arena.Encounter.Count(e => e.Kind == EnemyKind.Slime), Is.EqualTo(placeholder.slimes));
                Assert.That(arena.Encounter.Count(e => e.Kind == EnemyKind.Bat), Is.EqualTo(placeholder.bats));
                Assert.That(arena.Encounter.Count(e => e.Kind == EnemyKind.Spider), Is.EqualTo(placeholder.spiders));
            }
        }

        [Test]
        public void DeeperFloors_HaveMoreEnemies()
        {
            var perFloor = new double[3];
            foreach (int seed in k_Seeds)
            {
                RunGraph run = Generate(seed);
                for (int f = 0; f < 3; f++)
                    perFloor[f] += run.Floors[f].Nodes.Where(n => n.Kind == RoomKind.Combat).Average(n => n.Encounter.Count);
            }
            Assert.That(perFloor[1], Is.GreaterThan(perFloor[0]));
            Assert.That(perFloor[2], Is.GreaterThan(perFloor[1]));
        }

        [Test]
        public void ThePool_HasTheRoomsARunNeeds()
        {
            List<RoomCatalogEntry> rooms = Settings.Catalog();
            Assert.That(rooms.Count(r => r.Kind == RoomKind.Combat), Is.InRange(8, 12), "8–12 fight layouts");
            foreach (RoomKind kind in new[] { RoomKind.Start, RoomKind.Extraction, RoomKind.Descent, RoomKind.Arena })
                Assert.That(rooms.Any(r => r.Kind == kind), kind.ToString());
            foreach (int exits in new[] { 1, 2, 3 })
                Assert.That(rooms.Any(r => r.Kind == RoomKind.Combat && r.Exits == exits), $"a fight with {exits} exits");
        }
    }
}
