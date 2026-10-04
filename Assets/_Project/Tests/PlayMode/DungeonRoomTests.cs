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
    /// and never back, the hole drops to the next floor, ropes end the run, and the arena's placeholder fight ends a full
    /// run. The navigation grid and the camera follow every room.
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
        public IEnumerator AFullRun_DropsThroughThreeFloors_ToTheArena_WhosePlaceholderFightEndsTheRun()
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
            Assert.That(Node.Encounter.Count, Is.GreaterThanOrEqualTo(6), "the placeholder elite wave");
            yield return ClearRoom();
            Assert.That(rope.gameObject.activeInHierarchy, "the rope appears once the arena is clear");

            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "climbed out");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            Assert.That(Runner.RoomsEntered, Is.GreaterThanOrEqualTo(12), "a full run");
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
