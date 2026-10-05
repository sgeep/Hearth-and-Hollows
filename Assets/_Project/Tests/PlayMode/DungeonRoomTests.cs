using System.Collections;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Screens;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4d, in the <c>Dungeon</c> scene with a fixed seed: the generated run's rooms load one at a time, the gates seal
    /// while anything is alive and open (only the exits the graph uses) when it's clear, exits lead where the graph says
    /// and never back, the hole drops to the next floor, ropes end the run, and the arena's boss ends a full run. The navigation grid and the camera follow every room.
    /// </summary>
    public class DungeonRoomTests : LookTestFixture
    {
        const string RunScene = "Dungeon";
        const int k_Seed = 20261004;

        static RoomRunner Runner => RoomRunner.Active;
        static RoomInstance Room => Runner.Current;
        static FloorNode Node => Runner.Node;

        [SetUp]
        public void FixTheSeed() => RoomRunner.SeedOverride = k_Seed;

        [TearDown]
        public void ReleaseTheSeed() => RoomRunner.SeedOverride = 0;

        IEnumerator LoadRun()
        {
            yield return Load(RunScene);
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the first room");
            // Long tests shouldn't run out of Essence.
            Player.GetComponent<EssenceHealth>().GodMode = true;
            FreezeEnemies();
        }

        FloorNode Target(RoomExit exit) => Runner.Floor.Node(Node.Next[exit.Index]);

        /// <summary>Steps into an open exit and waits for the room it leads to.</summary>
        IEnumerator TakeExit(RoomExit exit)
        {
            Assert.That(exit, Is.Not.Null, $"{Node.RoomId}: an exit to take");
            Assert.That(exit.IsOpen, $"exit {exit.Index} is open");
            FloorNode target = Target(exit);
            int entered = Runner.RoomsEntered;
            Teleport(Player, (Vector2)exit.transform.position + new Vector2(0f, 0.3f));
            yield return WaitUntil(() => Runner.RoomsEntered == entered + 1 && !Runner.IsTransitioning, 5f, $"the room behind exit {exit.Index}");
            Assert.That(Node, Is.SameAs(target), "the exit led where the graph says");
            FreezeEnemies();
            yield return null;
        }

        /// <summary>The used exit whose room is what we want (the first that matches).</summary>
        RoomExit ExitTo(System.Func<FloorNode, bool> wanted) =>
            Room.Exits.Where(e => e.Index < Node.Next.Count).FirstOrDefault(e => wanted(Target(e)));

        IEnumerator ClearRoom()
        {
            foreach (EnemyIdentity enemy in Room.GetComponentsInChildren<EnemyIdentity>())
            {
                var health = enemy.GetComponent<Health>();
                health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            }
            yield return WaitUntil(() => Runner.Encounter.IsCleared, 5f, "the room to clear");
            // Let the gates finish rising.
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Descend()
        {
            int floor = Runner.Floor.Floor;
            Assert.That(Room.Descent, Is.Not.Null, "a hole");
            Teleport(Player, Room.Descent.transform.position);
            yield return WaitUntil(() => Runner.Floor.Floor == floor + 1 && !Runner.IsTransitioning, 5f, $"floor {floor + 1}");
            FreezeEnemies();
            yield return null;
        }

        void AssertRoomBound()
        {
            Assert.That(NavGrid.Current.Bounds, Is.EqualTo(Room.TileBounds), $"{Node.RoomId}: the grid covers the room");
            Vector2Int arrival = Vector2Int.FloorToInt(Room.Arrival.position);
            Assert.That(NavGrid.Current.Map.IsWalkable(new GridCell(arrival.x, arrival.y)), $"{Node.RoomId}: the arrival is walkable");
            Assert.That(Vector2.Distance(Player.transform.position, Room.Arrival.position), Is.LessThan(0.2f), $"{Node.RoomId}: arrived at its P");
        }

        /// <summary>Goes on toward the way down (or the arena), never a rope, until the room is of the given kind.</summary>
        IEnumerator WalkTo(RoomKind kind)
        {
            for (int guard = 0; guard < 30 && Node.Kind != kind; guard++)
            {
                AssertRoomBound();
                if (Node.Kind == RoomKind.Descent)
                {
                    yield return Descend();
                    continue;
                }
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                RoomExit exit = ExitTo(n => n.Kind == kind) ?? ExitTo(n => n.Kind != RoomKind.Extraction);
                yield return TakeExit(exit);
            }
            Assert.That(Node.Kind, Is.EqualTo(kind));
        }

        [UnityTest]
        public IEnumerator TheRun_IsGeneratedFromTheSeed_AndOpensQuietly()
        {
            yield return LoadRun();
            Assert.That(Runner.Graph.Seed, Is.EqualTo(k_Seed));
            Assert.That(Runner.Floor.Floor, Is.EqualTo(1));
            Assert.That(Node.Kind, Is.EqualTo(RoomKind.Start));
            Assert.That(Runner.Encounter.IsCleared, "no enemies, nothing sealed");
            foreach (RoomExit exit in Room.Exits)
            {
                Assert.That(exit.IsOpen, Is.EqualTo(exit.Index < Node.Next.Count), $"exit {exit.Index}: open only if the run uses it");
                Assert.That(exit.IsUnused, Is.EqualTo(exit.Index >= Node.Next.Count), $"exit {exit.Index}: bricked up if the run doesn't use it");
            }
            AssertRoomBound();
        }

        [UnityTest]
        public IEnumerator AFight_SealsItsExits_AndOpensOnlyTheUsedOnes_WhenClear()
        {
            yield return LoadRun();
            yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat));
            Assert.That(Runner.Encounter.IsSealed);
            Assert.That(Room.LivingEnemies(), Is.EqualTo(Node.Encounter.Count), "the run's encounter, placed");
            Assert.That(Room.Exits.All(e => !e.IsOpen), "the gates drop");
            // A sealed doorway goes nowhere.
            int entered = Runner.RoomsEntered;
            Teleport(Player, (Vector2)Room.Exits[0].transform.position + new Vector2(0f, 0.3f));
            for (int i = 0; i < 20; i++) yield return null;
            Assert.That(Runner.RoomsEntered, Is.EqualTo(entered));
            // Step back out of the doorway (or the gate rising would take the player straight through).
            Teleport(Player, Room.Arrival.position);

            int cleared = 0;
            void OnCleared(RoomCleared _) => cleared++;
            EventBus<RoomCleared>.Subscribe(OnCleared);
            try
            {
                yield return ClearRoom();
                foreach (RoomExit exit in Room.Exits)
                {
                    Assert.That(exit.IsOpen, Is.EqualTo(exit.Index < Node.Next.Count), $"exit {exit.Index}");
                    Assert.That(exit.IsUnused, Is.EqualTo(exit.Index >= Node.Next.Count), $"exit {exit.Index}: a spare exit is wall");
                }
                Assert.That(cleared, Is.EqualTo(1));
            }
            finally
            {
                EventBus<RoomCleared>.Unsubscribe(OnCleared);
            }
        }

        [UnityTest]
        public IEnumerator ChoosingABranch_LeavesTheOtherBehind_AndPartsLeftLyingStayBehind()
        {
            yield return LoadRun();
            // Find a room with a choice between two fights.
            for (int guard = 0; guard < 10 && Node.Next.Count(n => Runner.Floor.Node(n).Kind == RoomKind.Combat) < 2; guard++)
            {
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat));
            }
            if (Runner.Encounter.IsSealed) yield return ClearRoom();
            RoomExit[] fights = Room.Exits.Where(e => e.Index < Node.Next.Count && Target(e).Kind == RoomKind.Combat).ToArray();
            Assert.That(fights.Length, Is.GreaterThanOrEqualTo(2), "a choice of fights on the first floor");
            FloorNode from = Node, abandoned = Target(fights[0]);

            // A part left on the floor.
            EnemyDefinition slime = Object.FindObjectsByType<EnemyIdentity>(FindObjectsInactive.Include).First().Definition;
            HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(slime.harvest[0].ingredient, Quality.Standard), 1, 1f),
                (Vector2)Room.Arrival.position + new Vector2(3f, 3f), null);
            yield return null;
            Assert.That(Object.FindObjectsByType<IngredientPickup>(), Is.Not.Empty);

            yield return TakeExit(fights[1]);
            Assert.That(Object.FindObjectsByType<IngredientPickup>(), Is.Empty, "left behind with the room");
            Assert.That(Node, Is.Not.SameAs(abandoned));
            Assert.That(Node.Next.Concat(new[] { Node.Id }).Select(id => Runner.Floor.Node(id).Layer).All(l => l > from.Layer), "nothing leads back");
        }

        [UnityTest]
        public IEnumerator ARope_EndsTheRun_WithTheDelveResult()
        {
            yield return LoadRun();
            // Take fights until a rope is on offer, then take it.
            for (int guard = 0; guard < 12 && Node.Kind != RoomKind.Extraction; guard++)
            {
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Extraction) ?? ExitTo(n => n.Kind == RoomKind.Combat));
            }
            Assert.That(Node.Kind, Is.EqualTo(RoomKind.Extraction));
            Assert.That(Runner.Floor.Floor, Is.EqualTo(1));
            Assert.That(Room.GetComponentInChildren<DelveExit>(), Is.Not.Null, "the rope");
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "climbed out");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
        }

        [UnityTest]
        public IEnumerator AFullRun_DropsThroughThreeFloors_ToTheArena_WhoseBossEndsTheRun()
        {
            yield return LoadRun();
            yield return WalkTo(RoomKind.Descent);
            Assert.That(Runner.Floor.Floor, Is.EqualTo(1));
            Assert.That(Room.Descent, Is.Not.Null, "the hole down");
            yield return Descend();
            Assert.That(Runner.Floor.Floor, Is.EqualTo(2));
            Assert.That(Node.Kind, Is.EqualTo(RoomKind.Combat), "dropped into a fight");
            AssertRoomBound();

            yield return WalkTo(RoomKind.Descent);
            yield return Descend();
            Assert.That(Runner.Floor.Floor, Is.EqualTo(3));

            yield return WalkTo(RoomKind.Arena);
            AssertRoomBound();
            var rope = Room.GetComponentInChildren<DelveExit>(true);
            Assert.That(rope, Is.Not.Null);
            Assert.That(rope.gameObject.activeInHierarchy, Is.False, "the way out waits for the fight");
            Assert.That(Runner.Encounter.IsSealed);
            Assert.That(Room.LivingEnemies(), Is.EqualTo(Node.Encounter.Count));
            Assert.That(Node.Encounter.Select(e => e.Kind), Is.EqualTo(new[] { EnemyKind.Boss }), "the Larder Troll (4e), not 4d's stand-in wave");
            yield return ClearRoom();
            Assert.That(rope.gameObject.activeInHierarchy, "the rope appears once the arena is clear");

            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "climbed out");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            Assert.That(Runner.RoomsEntered, Is.GreaterThanOrEqualTo(12), "a full run");
        }

        /// <summary>
        /// 4e playtest: the room before the arena lights a campfire once it's clear (no other room has one), and standing by it
        /// gives back half the delver's Essence, then it burns low.
        /// </summary>
        [UnityTest]
        public IEnumerator TheRoomBeforeTheBoss_LightsACampfire_ThatGivesBackHalfYourEssence()
        {
            yield return LoadRun();
            yield return WalkTo(RoomKind.Descent);
            yield return Descend();
            yield return WalkTo(RoomKind.Descent);
            yield return Descend();
            for (int guard = 0; guard < 10 && !Node.Next.Any(id => Runner.Floor.Node(id).Kind == RoomKind.Arena); guard++)
            {
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                Assert.That(Object.FindObjectsByType<Campfire>(), Is.Empty, $"{Node.RoomId}: no campfire before the last room");
                yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat));
            }
            Assert.That(Object.FindObjectsByType<Campfire>(), Is.Empty, "not until it's clear");
            yield return ClearRoom();
            Campfire fire = Object.FindAnyObjectByType<Campfire>();
            Assert.That(fire, Is.Not.Null, "lit once the room before the arena is clear");
            Assert.That(fire.transform.IsChildOf(Room.transform), "in this room");

            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            essence.SetEncounterDrain(0f);
            essence.SetHealth(essence.MaximumHealth * 0.2f);
            float before = essence.CurrentHealth;
            Teleport(Player, (Vector2)fire.transform.position + new Vector2(0f, -1.2f));
            yield return WaitUntil(() => fire.IsSpent, 5f, "the fire to give all it has");
            Assert.That(essence.CurrentHealth - before, Is.EqualTo(essence.MaximumHealth * 0.5f).Within(1.5f), "half the delver's Essence back");
            essence.SetEncounterDrain(1f);
        }

        /// <summary>4e playtest: the room with the hole down has a smaller campfire, giving back a quarter of the delver's Essence.</summary>
        [UnityTest]
        public IEnumerator TheHoleDown_HasASmallerCampfire_ThatGivesBackAQuarter()
        {
            yield return LoadRun();
            yield return WalkTo(RoomKind.Descent);
            Campfire fire = Object.FindAnyObjectByType<Campfire>();
            Assert.That(fire, Is.Not.Null, "a campfire by the hole");
            Assert.That(fire.transform.IsChildOf(Room.transform));
            Assert.That(Vector2.Distance(fire.transform.position, Room.Descent.transform.position), Is.GreaterThan(3f), "clear of the hole");

            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            essence.SetEncounterDrain(0f);
            essence.SetHealth(essence.MaximumHealth * 0.2f);
            float before = essence.CurrentHealth;
            Teleport(Player, (Vector2)fire.transform.position + new Vector2(0f, -1.2f));
            yield return WaitUntil(() => fire.IsSpent, 4f, "the fire to give all it has");
            Assert.That(essence.CurrentHealth - before, Is.EqualTo(essence.MaximumHealth * 0.25f).Within(1.5f), "a quarter back");
            essence.SetEncounterDrain(1f);
        }

        /// <summary>Goes on through fights, preferring one whose reward is the given kind, until standing in such a room.</summary>
        IEnumerator WalkToReward(RewardKind kind)
        {
            for (int guard = 0; guard < 20 && !(Node.Kind == RoomKind.Combat && Node.Reward.Kind == kind); guard++)
            {
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                RoomExit fight = ExitTo(n => n.Kind == RoomKind.Combat && n.Reward.Kind == kind) ?? ExitTo(n => n.Kind == RoomKind.Combat);
                if (fight != null) yield return TakeExit(fight);
                // The end of the floor without one: down the hole to the next.
                else if (Node.Kind == RoomKind.Descent) yield return Descend();
                else yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Descent));
            }
            Assert.That(Node.Reward.Kind, Is.EqualTo(kind), $"a {kind} room on the way down");
        }

        [UnityTest]
        public IEnumerator EachDoor_ShowsWhatItsRoomPromises()
        {
            yield return LoadRun();
            for (int room = 0; room < 6; room++)
            {
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                foreach (RoomExit exit in Room.Exits.Where(e => e.Index < Node.Next.Count))
                {
                    FloorNode target = Target(exit);
                    string expected = target.Kind switch
                    {
                        RoomKind.Extraction => "ArrowUp",
                        RoomKind.Descent => "ArrowDown",
                        RoomKind.Arena => "Swords",
                        _ => target.Reward.Kind switch { RewardKind.Gold => "GoldCoin", RewardKind.Power => "Lightning", _ => "Food" },
                    };
                    Assert.That(exit.Marker, Is.Not.Null, $"{Node.RoomId} exit {exit.Index}");
                    Assert.That(exit.Marker.name, Does.Contain(expected), $"{Node.RoomId} exit {exit.Index} leads to {target.Kind} {target.Reward}");
                }
                RoomExit next = ExitTo(n => n.Kind == RoomKind.Combat);
                if (next == null) break;
                yield return TakeExit(next);
            }
        }

        [UnityTest]
        public IEnumerator AGoldRoom_LeavesItsGold_AndPickingItUpAddsToTheRun()
        {
            yield return LoadRun();
            yield return WalkToReward(RewardKind.Gold);
            int promised = Node.Reward.Amount;
            yield return ClearRoom();
            var coin = Room.GetComponentInChildren<GoldPickup>();
            Assert.That(coin, Is.Not.Null, "the Gold appears when the room is clear");
            Assert.That(coin.Amount, Is.EqualTo(promised));
            var hud = Object.FindAnyObjectByType<Hearthdelve.UI.Hud.RunGoldView>();
            Assert.That(hud.Shown, Is.Zero);
            Teleport(Player, coin.transform.position);
            yield return WaitUntil(() => DelveRunController.Active.Loot.Gold == promised, 3f, "the Gold picked up");
            Assert.That(hud.Shown, Is.EqualTo(promised), "the HUD shows the run's Gold");
            Assert.That(coin == null, "the coin is gone");
        }

        [UnityTest]
        public IEnumerator AnIngredientRoom_LeavesItsParts()
        {
            yield return LoadRun();
            yield return WalkToReward(RewardKind.Ingredient);
            RoomReward reward = Node.Reward;
            yield return ClearRoom();
            // Kills may drop parts of their own: the reward is the stack of the promised part and quality.
            IngredientPickup[] drops = Object.FindObjectsByType<IngredientPickup>();
            Assert.That(drops.Any(p => p.Stack.Item.Definition.id == reward.ItemId && p.Stack.Item.Quality == reward.Quality && p.Stack.Count == reward.Amount),
                $"the reward {reward} on the floor");
        }

        /// <summary>Clears a power room and steps onto its spark.</summary>
        IEnumerator TouchASpark()
        {
            yield return WalkToReward(RewardKind.Power);
            yield return ClearRoom();
            var spark = Room.GetComponentInChildren<Hearthdelve.Dungeon.Powers.PowerPickup>();
            Assert.That(spark, Is.Not.Null, "a spark appears when the room is clear");
            Teleport(Player, spark.transform.position);
        }

        [UnityTest]
        public IEnumerator APowerRoom_OffersThreePowers_AndTheChoiceLastsTheRun()
        {
            yield return LoadRun();
            var screen = Object.FindAnyObjectByType<RunPowerScreen>(FindObjectsInactive.Include);
            var hud = Object.FindAnyObjectByType<Hearthdelve.UI.Hud.RunPowersHud>();
            var essence = Player.GetComponent<EssenceHealth>();
            yield return TouchASpark();
            yield return WaitUntil(() => screen.IsOpen, 3f, "the choice of powers");
            Assert.That(Hearthdelve.Shared.Engine.MenuPause.IsPaused, "the run waits");
            Assert.That(screen.Options, Has.Count.EqualTo(3));
            Assert.That(screen.Options.Select(p => p.id).Distinct().Count(), Is.EqualTo(3));
            foreach (RunPowerScreen.Card card in screen.Cards)
                Assert.That(card.icon.sprite, Is.Not.Null, "every card has its icon");

            // Take Deep Reserves if it's offered (its effect is easy to see), otherwise the first.
            int pick = Mathf.Max(0, screen.Options.ToList().FindIndex(p => p.effect == RunPowerEffect.MaxEssence));
            RunPowerDefinition chosen = screen.Options[pick];
            float max = essence.Essence.Max;
            screen.Choose(pick);
            yield return null;
            Assert.That(Hearthdelve.Shared.Engine.MenuPause.IsPaused, Is.False, "the run goes on");
            Assert.That(DelveRunController.Active.Powers.Has(chosen));
            Assert.That(hud.Shown, Is.EqualTo(1), "its icon on the HUD");
            Assert.That(Room.GetComponentInChildren<Hearthdelve.Dungeon.Powers.PowerPickup>() == null, "the spark is used up");
            if (chosen.effect == RunPowerEffect.MaxEssence) Assert.That(essence.Essence.Max, Is.EqualTo(max + chosen.amount).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator BackingOut_LeavesTheSpark_ToComeBackTo()
        {
            yield return LoadRun();
            var screen = Object.FindAnyObjectByType<RunPowerScreen>(FindObjectsInactive.Include);
            yield return TouchASpark();
            yield return WaitUntil(() => screen.IsOpen, 3f, "the choice of powers");
            string[] first = screen.Options.Select(p => p.id).ToArray();
            screen.Choose(-1);
            yield return null;
            var spark = Room.GetComponentInChildren<Hearthdelve.Dungeon.Powers.PowerPickup>();
            Assert.That(spark, Is.Not.Null, "still there");
            Assert.That(DelveRunController.Active.Powers.Taken, Is.Empty);
            Assert.That(screen.IsOpen, Is.False, "standing on it doesn't reopen the choice");
            Teleport(Player, Room.Arrival.position);
            yield return new WaitForSeconds(0.2f);
            Teleport(Player, spark.transform.position);
            yield return WaitUntil(() => screen.IsOpen, 3f, "the choice again");
            Assert.That(screen.Options.Select(p => p.id), Is.EqualTo(first), "the same three");
            screen.Choose(0);
        }

        [UnityTest]
        public IEnumerator Powers_ChangeTheirTuning()
        {
            yield return LoadRun();
            RunSettings settings = Runner.Settings;
            RunPowerDefinition Find(RunPowerEffect effect) => settings.tuning.powers.First(p => p.effect == effect);
            var essence = Player.GetComponent<EssenceHealth>();
            var dash = Player.GetComponent<CharacterDash2D>();
            float cooldown = dash.Cooldown.RefillDuration;
            DelveRunController run = DelveRunController.Active;

            Assert.That(run.TakePower(Find(RunPowerEffect.FasterDodge)));
            Assert.That(dash.Cooldown.RefillDuration, Is.EqualTo(cooldown * (1f - Find(RunPowerEffect.FasterDodge).amount)).Within(1e-4f), "a quicker roll");

            Assert.That(run.TakePower(Find(RunPowerEffect.LighterHits)));
            essence.GodMode = false;
            float before = essence.Essence.Current;
            essence.Essence.TakeDamage(10f);
            Assert.That(before - essence.Essence.Current, Is.EqualTo(10f * (1f - Find(RunPowerEffect.LighterHits).amount)).Within(1e-3f), "hits cost less");

            // Second wind: Essence back as the next room is cleared.
            Assert.That(run.TakePower(Find(RunPowerEffect.EssenceOnClear)));
            essence.Essence.TakeDamage(30f);
            essence.GodMode = true;
            if (!Runner.Encounter.IsSealed) yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat));
            float low = essence.Essence.Current;
            yield return ClearRoom();
            Assert.That(essence.Essence.Current, Is.GreaterThan(low + Find(RunPowerEffect.EssenceOnClear).amount - 1f), "Essence back on the clear");
        }

        [UnityTest]
        public IEnumerator RunGold_ComesHome_OnExtraction()
        {
            yield return LoadRun();
            DelveRunController.Active.Loot.AddGold(27);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract());
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            Assert.That(result.Report.GoldSecured, Is.EqualTo(27));
            Assert.That(result.Report.GoldLost, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RunGold_IsLost_OnDeath()
        {
            yield return LoadRun();
            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            DelveRunController.Active.Loot.AddGold(27);
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            death.Confirm.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            Assert.That(result.Report.GoldSecured, Is.Zero);
            Assert.That(result.Report.GoldLost, Is.EqualTo(27));
        }

        [UnityTest]
        public IEnumerator TheCamera_StaysInsideEveryRoom()
        {
            yield return LoadRun();
            Camera camera = Camera.main;
            for (int room = 0; room < 3; room++)
            {
                Rect bounds = new(Vector2.zero, Room.Size);
                foreach (Vector2 corner in new[] { new Vector2(1.5f, 2.5f), new Vector2(bounds.width - 1.5f, bounds.height - 2.5f) })
                {
                    Teleport(Player, corner);
                    for (int i = 0; i < 10; i++) yield return null;
                    float height = camera.orthographicSize * 2f, width = height * camera.aspect;
                    Vector2 centre = camera.transform.position;
                    string at = $"{Node.RoomId} at {corner}: view {width:F1}×{height:F1} centred {centre}";
                    if (width <= bounds.width) Assert.That(centre.x - width / 2f >= -0.02f && centre.x + width / 2f <= bounds.width + 0.02f, at);
                    else Assert.That(centre.x, Is.EqualTo(bounds.center.x).Within(0.05f), at);
                    if (height <= bounds.height) Assert.That(centre.y - height / 2f >= -0.02f && centre.y + height / 2f <= bounds.height + 0.02f, at);
                    else Assert.That(centre.y, Is.EqualTo(bounds.center.y).Within(0.05f), at);
                }
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat) ?? ExitTo(_ => true));
            }
        }
    }
}
