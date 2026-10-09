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

        /// <summary>The rope up and the hole down are safe ground: no Essence drain there (after the type pass, the owner's call).</summary>
        [Test]
        public void TheRopeAndTheHole_PauseTheEssenceDrain_NowhereElse()
        {
            var seen = new HashSet<RoomKind>();
            foreach (int seed in k_Seeds.Take(20))
            foreach (FloorGraph floor in Generate(seed).Floors)
            foreach (FloorNode node in floor.Nodes)
            {
                seen.Add(node.Kind);
                Assert.That(node.PausesEssenceDrain, Is.EqualTo(node.Kind is RoomKind.Start or RoomKind.Extraction or RoomKind.Descent), $"seed {seed}: {node.Kind}");
            }
            Assert.That(seen, Is.SupersetOf(new[] { RoomKind.Extraction, RoomKind.Descent, RoomKind.Combat, RoomKind.Arena }));
        }

        /// <summary>2026-10-07 (the owner's playtest): bats don't only hang from walls; some fly in the room's open middle.</summary>
        [Test]
        public void Bats_SometimesFlyInTheOpen_OnGroundSpawns_AndStillOnPerches()
        {
            int open = 0, perched = 0;
            foreach (int seed in k_Seeds)
            foreach (FloorGraph floor in Generate(seed).Floors)
            foreach (FloorNode node in floor.Nodes)
            foreach (EncounterSpawn spawn in node.Encounter.Where(e => e.Kind == EnemyKind.Bat))
            {
                if (spawn.InOpen) open++;
                else perched++;
            }
            Assert.That(open, Is.GreaterThan(0), "some bats in the open");
            Assert.That(perched, Is.GreaterThan(0), "and some still asleep on the walls");
        }

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
                        Assert.That(spawn.Point, Is.LessThan(spawn.Kind == EnemyKind.Bat && !spawn.InOpen ? room.PerchSpawns : room.GroundSpawns), $"{at}: a real spawn point");
                    Assert.That(node.Encounter.Select(e => (e.Kind == EnemyKind.Bat && !e.InOpen, e.Point)), Is.Unique, $"{at}: one enemy per point");
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
        public void WithoutABoss_TheArenaHoldsTheStandInFight()
        {
            // 4e: the Larder Troll is the arena's encounter (LarderTrollTests); settings without a boss keep 4d's stand-in.
            RunTuning tuning = UnityEngine.JsonUtility.FromJson<RunTuning>(UnityEngine.JsonUtility.ToJson(Settings.tuning));
            tuning.boss = null;
            ArenaPlaceholder placeholder = tuning.arenaPlaceholder;
            foreach (int seed in k_Seeds.Take(50))
            {
                FloorNode arena = RunGraph.Ends(RunGenerator.Generate(seed, tuning, Settings.Catalog()).Floors[2], RoomKind.Arena).Single();
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
        public void EveryFight_HasAReward_InItsFloorsRange_AndQuietRoomsHaveNone()
        {
            RunTuning tuning = Settings.tuning;
            Assert.That(tuning.ingredientRewards, Is.Not.Empty, "the dungeon's ingredients to give");
            foreach (int seed in k_Seeds)
            foreach (FloorGraph floor in Generate(seed).Floors)
            {
                FloorTuning t = tuning.Floor(floor.Floor - 1);
                foreach (FloorNode node in floor.Nodes)
                {
                    string at = $"seed {seed}, floor {floor.Floor}, node {node.Id}";
                    if (node.Kind != RoomKind.Combat)
                    {
                        Assert.That(node.Reward.Kind, Is.EqualTo(RewardKind.None), $"{at}: quiet rooms give nothing (the arena's reward is 4e's boss)");
                        continue;
                    }
                    RoomReward reward = node.Reward;
                    if (reward.Kind == RewardKind.Gold) Assert.That(reward.Amount, Is.InRange(t.minGold, t.maxGold), at);
                    else if (reward.Kind == RewardKind.Power) Assert.That(reward.Amount, Is.EqualTo(1), at);
                    else if (reward.Kind == RewardKind.Curio) Assert.That(tuning.curios, Is.Not.Null, $"{at}: curio rooms only with a pool");
                    else
                    {
                        Assert.That(reward.Kind, Is.EqualTo(RewardKind.Ingredient), at);
                        Assert.That(reward.Amount, Is.InRange(t.minParts, t.maxParts), at);
                        Assert.That(reward.Quality is Hearthdelve.Shared.Ingredients.Quality.Standard or Hearthdelve.Shared.Ingredients.Quality.Fine, at);
                        IngredientRewardOption option = tuning.ingredientRewards.Single(o => o.ingredient.id == reward.ItemId);
                        Assert.That(option.fromFloor, Is.LessThanOrEqualTo(floor.Floor), $"{at}: {reward.ItemId} isn't found this high up");
                    }
                }
            }
        }

        [Test]
        public void AChoiceOfFights_IsAChoiceOfRewards()
        {
            foreach (int seed in k_Seeds)
            foreach (FloorGraph floor in Generate(seed).Floors)
            foreach (FloorNode node in floor.Nodes)
            {
                List<FloorNode> fights = node.Next.Select(floor.Node).Where(n => n.Kind == RoomKind.Combat).ToList();
                if (fights.Count >= 2)
                    Assert.That(fights.Select(n => n.Reward.Kind).Distinct().Count(), Is.GreaterThan(1), $"seed {seed}, floor {floor.Floor}, node {node.Id}");
            }
        }

        [Test]
        public void DeeperFloors_PayMore_AndBothRewardsTurnUp()
        {
            var gold = new List<double>[] { new(), new(), new() };
            int ingredients = 0, golds = 0;
            foreach (int seed in k_Seeds)
            {
                RunGraph run = Generate(seed);
                for (int f = 0; f < 3; f++)
                    foreach (FloorNode node in run.Floors[f].Nodes.Where(n => n.Kind == RoomKind.Combat))
                    {
                        if (node.Reward.Kind == RewardKind.Gold)
                        {
                            gold[f].Add(node.Reward.Amount);
                            golds++;
                        }
                        else if (node.Reward.Kind == RewardKind.Ingredient) ingredients++;
                    }
            }
            Assert.That(gold[1].Average(), Is.GreaterThan(gold[0].Average()));
            Assert.That(gold[2].Average(), Is.GreaterThan(gold[1].Average()));
            Assert.That(ingredients, Is.GreaterThan(golds / 3), "ingredient rooms are common");
            Assert.That(golds, Is.GreaterThan(ingredients / 3), "Gold rooms are common");
        }

        [Test]
        public void PowerRooms_TurnUp_ButNotTooOften()
        {
            Assert.That(Settings.tuning.powers, Has.Length.EqualTo(8), "the eight run powers");
            int fights = 0, powers = 0, runsWithNone = 0;
            foreach (int seed in k_Seeds)
            {
                List<FloorNode> combat = Generate(seed).Floors.SelectMany(f => f.Nodes).Where(n => n.Kind == RoomKind.Combat).ToList();
                fights += combat.Count;
                int here = combat.Count(n => n.Reward.Kind == RewardKind.Power);
                powers += here;
                if (here == 0) runsWithNone++;
            }
            float share = powers / (float)fights;
            Assert.That(share, Is.InRange(0.12f, 0.35f), "about one fight in five");
            Assert.That(runsWithNone, Is.LessThan(30), "nearly every run offers a power somewhere");
        }

        [Test]
        public void WithoutPowers_ThereAreNoPowerRooms()
        {
            RunTuning tuning = UnityEngine.JsonUtility.FromJson<RunTuning>(UnityEngine.JsonUtility.ToJson(Settings.tuning));
            tuning.powers = System.Array.Empty<Hearthdelve.Shared.Run.RunPowerDefinition>();
            foreach (int seed in k_Seeds.Take(100))
                Assert.That(RunGenerator.Generate(seed, tuning, Settings.Catalog()).Floors.SelectMany(f => f.Nodes).Any(n => n.Reward.Kind == RewardKind.Power), Is.False);
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
