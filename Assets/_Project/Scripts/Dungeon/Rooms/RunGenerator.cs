using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Random;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// Generates a delve from a seed (4d): three floors, each a one-way graph of authored rooms. Pure and deterministic:
    /// the same seed, tuning and rooms always give the same run.
    /// <list type="bullet">
    /// <item>A floor is a series of steps. The first floor opens with a quiet start room; deeper floors open with the
    /// fight the player drops into. Each step has one or two rooms (at least one step per floor offers a choice), and
    /// every room leads to at least one room of the next step.</item>
    /// <item>Every room of the last step leads to both of the floor's ends: the rope out (extraction) and the way on
    /// (the hole down, or on the last floor the boss arena). So every route reaches a rope, and the choice at the end
    /// of a floor is "leave with what I have, or go deeper". Some floors also offer an early rope partway through.</item>
    /// <item>Rooms are chosen by kind and by how many exits they need, avoiding repeats where the pool allows.</item>
    /// <item>Each fight gets an encounter from the floor's tuning, placed on the room's spawn points.</item>
    /// <item>Each fight gets a reward (4d step 3): run Gold or a dungeon ingredient, rolled now so a seed replays it, richer
    /// deeper down. Where a room offers a choice of fights, the choice offers different kinds of reward.</item>
    /// </list>
    /// </summary>
    public static class RunGenerator
    {
        public static RunGraph Generate(int seed, RunTuning tuning, IReadOnlyList<RoomCatalogEntry> rooms)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (rooms == null || rooms.Count == 0) throw new ArgumentException("No rooms to build a run from.", nameof(rooms));
            var random = new SeededRandom(seed);
            // How often each fight room has been used this run.
            var used = new Dictionary<string, int>();
            var floors = new List<FloorGraph>();
            for (int f = 0; f < RunTuning.Floors; f++)
                floors.Add(BuildFloor(f, tuning, rooms, random, used));
            return new RunGraph(seed, floors);
        }

        static FloorGraph BuildFloor(int index, RunTuning tuning, IReadOnlyList<RoomCatalogEntry> rooms, SeededRandom random, Dictionary<string, int> used)
        {
            FloorTuning t = tuning.Floor(index);
            bool last = index == RunTuning.Floors - 1;
            int fights = random.Range(Math.Min(t.minFights, t.maxFights), Math.Max(t.minFights, t.maxFights));

            // The steps: the opening room, then the fights after it, one or two rooms each.
            var steps = new List<List<FloorNode>> { new() { new FloorNode { Kind = index == 0 ? RoomKind.Start : RoomKind.Combat } } };
            int after = Math.Max(1, index == 0 ? fights : fights - 1);
            int maxWidth = Math.Max(1, t.maxWidth);
            var widths = new int[after];
            for (int i = 0; i < after; i++)
                widths[i] = maxWidth > 1 && random.Value() < t.branchChance ? random.Range(2, maxWidth) : 1;
            // Every floor offers at least one choice.
            if (maxWidth > 1 && widths.All(w => w == 1)) widths[random.Range(0, after - 1)] = 2;
            foreach (int width in widths)
                steps.Add(Enumerable.Range(0, width).Select(_ => new FloorNode { Kind = RoomKind.Combat }).ToList());

            // The ends: every room of the last step leads to both, the rope on a side chosen once per floor.
            var extraction = new FloorNode { Kind = RoomKind.Extraction };
            var wayOn = new FloorNode { Kind = last ? RoomKind.Arena : RoomKind.Descent };
            bool ropeLeft = random.Value() < 0.5f;
            steps.Add(ropeLeft ? new List<FloorNode> { extraction, wayOn } : new List<FloorNode> { wayOn, extraction });

            var nodes = new List<FloorNode>();
            for (int s = 0; s < steps.Count; s++)
                for (int lane = 0; lane < steps[s].Count; lane++)
                {
                    FloorNode node = steps[s][lane];
                    node.Id = nodes.Count;
                    node.Layer = s;
                    node.Lane = lane;
                    nodes.Add(node);
                }
            for (int s = 0; s + 2 < steps.Count; s++) Connect(steps[s], steps[s + 1], t.crossChance, random);
            foreach (FloorNode node in steps[^2]) node.Next.AddRange(steps[^1].Select(n => n.Id));

            // Sometimes an early rope partway through, off one room (never the last step: that already has one).
            if (steps.Count > 4 && random.Value() < t.earlyExtractionChance)
            {
                // Off a fight after the first step and before the last fight step (whose rooms already lead to a rope).
                int step = random.Range(1, steps.Count - 3);
                List<FloorNode> candidates = steps[step].Where(n => n.Kind == RoomKind.Combat && n.Next.Count < 3).ToList();
                if (candidates.Count > 0)
                {
                    FloorNode parent = candidates[random.Range(0, candidates.Count - 1)];
                    var early = new FloorNode { Kind = RoomKind.Extraction, Id = nodes.Count, Layer = step + 1, Lane = steps[step + 1].Count };
                    nodes.Add(early);
                    if (random.Value() < 0.5f) parent.Next.Insert(0, early.Id);
                    else parent.Next.Add(early.Id);
                }
            }

            var onThisFloor = new HashSet<string>();
            foreach (FloorNode node in nodes) AssignRoom(node, rooms, random, used, onThisFloor);
            foreach (FloorNode node in nodes) AssignEncounter(node, t, tuning.arenaPlaceholder, rooms, random);
            foreach (FloorNode node in nodes)
                if (node.Kind == RoomKind.Combat) node.Reward = RollReward(PickRewardKind(t, random), index + 1, t, tuning, random);
            VaryChoices(nodes, index + 1, t, tuning, random);
            return new FloorGraph(index + 1, nodes);
        }

        /// <summary>Links one step to the next so that every room has a way on and every room of the next step a way in.</summary>
        static void Connect(List<FloorNode> from, List<FloorNode> to, float crossChance, SeededRandom random)
        {
            void Link(FloorNode a, FloorNode b)
            {
                if (!a.Next.Contains(b.Id)) a.Next.Add(b.Id);
            }

            if (from.Count == 1)
                foreach (FloorNode node in to) Link(from[0], node);
            else if (to.Count == 1)
                foreach (FloorNode node in from) Link(node, to[0]);
            else
            {
                // Side by side: each route carries straight on, every room of the next step gets a way in, and sometimes they cross.
                for (int i = 0; i < from.Count; i++) Link(from[i], to[Math.Min(i, to.Count - 1)]);
                for (int j = 0; j < to.Count; j++)
                    if (!from.Any(n => n.Next.Contains(to[j].Id))) Link(from[Math.Min(j, from.Count - 1)], to[j]);
                if (random.Value() < crossChance) Link(from[random.Range(0, from.Count - 1)], to[random.Range(0, to.Count - 1)]);
            }
            // Exits lead left to right: children in lane order.
            foreach (FloorNode node in from) node.Next.Sort();
        }

        /// <summary>
        /// Chooses the node's room: of its kind, with enough exits; preferring one not used on this floor, then the least
        /// used this run, and among those the fewest spare exits (spare exits stay shut).
        /// </summary>
        static void AssignRoom(FloorNode node, IReadOnlyList<RoomCatalogEntry> rooms, SeededRandom random, Dictionary<string, int> used, HashSet<string> onThisFloor)
        {
            int exits = node.Next.Count;
            List<RoomCatalogEntry> fits = rooms.Where(r => r.Kind == node.Kind && r.Exits >= exits).ToList();
            if (fits.Count == 0) throw new InvalidOperationException($"No {node.Kind} room with {exits} exits in the pool.");
            (int, int, int) Score(RoomCatalogEntry r) => (onThisFloor.Contains(r.Id) ? 1 : 0, used.TryGetValue(r.Id, out int n) ? n : 0, r.Exits - exits);
            (int, int, int) best = fits.Select(Score).Min();
            List<RoomCatalogEntry> pick = fits.Where(r => Score(r).Equals(best)).ToList();
            RoomCatalogEntry room = pick[random.Range(0, pick.Count - 1)];
            node.RoomId = room.Id;
            // Quiet rooms repeat by design (one start, one rope room, one hole room); fights shouldn't.
            if (node.Kind != RoomKind.Combat) return;
            used[room.Id] = used.TryGetValue(room.Id, out int uses) ? uses + 1 : 1;
            onThisFloor.Add(room.Id);
        }

        static void AssignEncounter(FloorNode node, FloorTuning tuning, ArenaPlaceholder arena, IReadOnlyList<RoomCatalogEntry> rooms, SeededRandom random)
        {
            if (node.Kind is not (RoomKind.Combat or RoomKind.Arena)) return;
            RoomCatalogEntry room = rooms.First(r => r.Id == node.RoomId);
            List<int> ground = Shuffled(room.GroundSpawns, random), perches = Shuffled(room.PerchSpawns, random);

            void Place(EnemyKind kind)
            {
                List<int> points = kind == EnemyKind.Bat ? perches : ground;
                if (points.Count == 0) return;
                node.Encounter.Add(new EncounterSpawn(kind, points[0]));
                points.RemoveAt(0);
            }

            if (node.Kind == RoomKind.Arena)
            {
                // TEMPORARY (4d): the arena's stand-in fight; the 4e boss replaces it.
                for (int i = 0; i < arena.spiders; i++) Place(EnemyKind.Spider);
                for (int i = 0; i < arena.bats; i++) Place(EnemyKind.Bat);
                for (int i = 0; i < arena.slimes; i++) Place(EnemyKind.Slime);
                return;
            }

            int count = random.Range(Math.Min(tuning.minEnemies, tuning.maxEnemies), Math.Max(tuning.minEnemies, tuning.maxEnemies));
            for (int i = 0; i < count; i++)
            {
                EnemyKind kind = PickKind(tuning, perches.Count > 0, ground.Count > 0, random);
                if (perches.Count == 0 && ground.Count == 0) break;
                Place(kind);
            }
        }

        static EnemyKind PickKind(FloorTuning tuning, bool perch, bool ground, SeededRandom random)
        {
            var kinds = new List<EnemyKind>();
            if (ground) kinds.Add(EnemyKind.Slime);
            if (perch) kinds.Add(EnemyKind.Bat);
            if (ground) kinds.Add(EnemyKind.Spider);
            float total = kinds.Sum(tuning.Weight);
            if (total <= 0f) return kinds.Count > 0 ? kinds[0] : EnemyKind.Slime;
            float roll = random.Value() * total;
            foreach (EnemyKind kind in kinds)
            {
                roll -= tuning.Weight(kind);
                if (roll < 0f) return kind;
            }
            return kinds[^1];
        }

        static RewardKind PickRewardKind(FloorTuning t, SeededRandom random)
        {
            float total = t.goldWeight + t.ingredientWeight;
            if (total <= 0f) return RewardKind.Gold;
            return random.Value() * total < t.goldWeight ? RewardKind.Gold : RewardKind.Ingredient;
        }

        /// <summary>The payload for a reward of the given kind on this floor (Gold, or which part, how many, how good).</summary>
        static RoomReward RollReward(RewardKind kind, int floor, FloorTuning t, RunTuning tuning, SeededRandom random)
        {
            if (kind == RewardKind.Ingredient)
            {
                var options = (tuning.ingredientRewards ?? Array.Empty<IngredientRewardOption>())
                    .Where(o => o != null && o.ingredient != null && o.fromFloor <= floor && o.weight > 0f).ToList();
                if (options.Count > 0)
                {
                    float roll = random.Value() * options.Sum(o => o.weight);
                    IngredientRewardOption pick = options[^1];
                    foreach (IngredientRewardOption option in options)
                    {
                        roll -= option.weight;
                        if (roll < 0f)
                        {
                            pick = option;
                            break;
                        }
                    }
                    int parts = random.Range(Math.Min(t.minParts, t.maxParts), Math.Max(t.minParts, t.maxParts));
                    var quality = random.Value() < t.fineChance ? Hearthdelve.Shared.Ingredients.Quality.Fine : Hearthdelve.Shared.Ingredients.Quality.Standard;
                    return RoomReward.Ingredient(pick.ingredient.id, quality, parts);
                }
            }
            return RoomReward.Gold(random.Range(Math.Min(t.minGold, t.maxGold), Math.Max(t.minGold, t.maxGold)));
        }

        /// <summary>A choice between fights should be a choice between rewards: if every fight on offer gives the same kind, the last gives the other.</summary>
        static void VaryChoices(List<FloorNode> nodes, int floor, FloorTuning t, RunTuning tuning, SeededRandom random)
        {
            foreach (FloorNode node in nodes)
            {
                List<FloorNode> fights = node.Next.Select(id => nodes[id]).Where(n => n.Kind == RoomKind.Combat).ToList();
                if (fights.Count < 2 || fights.Select(n => n.Reward.Kind).Distinct().Count() > 1) continue;
                RewardKind other = fights[0].Reward.Kind == RewardKind.Gold ? RewardKind.Ingredient : RewardKind.Gold;
                // With at most two rooms a step, a choice of two fights is always the whole next step, so one change serves every room offering it.
                fights[^1].Reward = RollReward(other, floor, t, tuning, random);
            }
        }

        static List<int> Shuffled(int count, SeededRandom random)
        {
            var list = Enumerable.Range(0, count).ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
