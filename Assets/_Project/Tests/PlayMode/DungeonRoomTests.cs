using System.Collections;
using System.Linq;
using Hearthdelve.Core;
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
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4d step 1, in the <c>Dungeon</c> scene: rooms load one at a time, the player arrives at each room's P, the
    /// gates seal while anything in the room is alive and open when it's clear, an open doorway leads to the next
    /// room (the last is unloaded with whatever was left in it), and the navigation grid and the camera follow the room.
    /// </summary>
    public class DungeonRoomTests : LookTestFixture
    {
        const string RunScene = "Dungeon";

        static RoomRunner Runner => RoomRunner.Active;

        IEnumerator LoadRun()
        {
            yield return Load(RunScene);
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the first room");
            // Long tests shouldn't run out of Essence.
            Player.GetComponent<EssenceHealth>().GodMode = true;
        }

        static RoomInstance Room => Runner.Current;

        /// <summary>Steps the player into an open exit's doorway and waits for the next room.</summary>
        IEnumerator TakeExit(int index, string expected)
        {
            RoomExit exit = Room.Exits[index];
            Assert.That(exit.IsOpen, "the exit is open");
            Teleport(Player, (Vector2)exit.transform.position + new Vector2(0f, 0.3f));
            yield return WaitUntil(() => Runner.CurrentDefinition.id == expected && !Runner.IsTransitioning, 5f, $"the room {expected}");
            FreezeEnemies();
            yield return null;
        }

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

        [UnityTest]
        public IEnumerator TheFirstRoom_IsQuiet_ItsExitIsOpen_AndThePlayerArrivesAtItsP()
        {
            yield return LoadRun();
            Assert.That(Runner.CurrentDefinition.id, Is.EqualTo("start"));
            Assert.That(Runner.RoomsEntered, Is.EqualTo(1));
            Assert.That(Runner.Encounter.IsCleared, "no enemies, nothing sealed");
            Assert.That(Room.Exits.All(e => e.IsOpen));
            Assert.That(Vector2.Distance(Player.transform.position, Room.Arrival.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator EnteringAFight_SealsTheExits_AndASealedExitLeadsNowhere()
        {
            yield return LoadRun();
            RoomInstance first = Room;
            yield return TakeExit(0, "slime_hall");
            Assert.That(first == null, "the last room is unloaded");
            Assert.That(Runner.RoomsEntered, Is.EqualTo(2));
            Assert.That(Vector2.Distance(Player.transform.position, Room.Arrival.position), Is.LessThan(0.2f), "arrives at the new room's P");
            Assert.That(Runner.Encounter.IsSealed);
            Assert.That(Room.LivingEnemies(), Is.EqualTo(3));
            Assert.That(Room.Exits, Has.Length.EqualTo(2));
            Assert.That(Room.Exits.All(e => !e.IsOpen), "the gates drop as the fight starts");

            // Standing in a sealed doorway goes nowhere.
            Teleport(Player, (Vector2)Room.Exits[0].transform.position + new Vector2(0f, 0.3f));
            for (int i = 0; i < 20; i++) yield return null;
            Assert.That(Runner.CurrentDefinition.id, Is.EqualTo("slime_hall"));
            Assert.That(Runner.IsTransitioning, Is.False);
        }

        [UnityTest]
        public IEnumerator ClearingTheRoom_OpensEveryExit_Once()
        {
            yield return LoadRun();
            yield return TakeExit(0, "slime_hall");
            int cleared = 0;
            void OnCleared(RoomCleared _) => cleared++;
            EventBus<RoomCleared>.Subscribe(OnCleared);
            try
            {
                Assert.That(Room.Exits.All(e => !e.IsOpen));
                yield return ClearRoom();
                Assert.That(Room.Exits.All(e => e.IsOpen), "every gate rises");
                Assert.That(cleared, Is.EqualTo(1));
            }
            finally
            {
                EventBus<RoomCleared>.Unsubscribe(OnCleared);
            }
        }

        [UnityTest]
        public IEnumerator EitherExit_LeadsOn_AndPartsLeftLyingStayBehind()
        {
            yield return LoadRun();
            yield return TakeExit(0, "slime_hall");
            yield return ClearRoom();
            // A part left lying on the floor (whether or not the kills dropped any).
            EnemyDefinition slime = Object.FindObjectsByType<EnemyIdentity>(FindObjectsInactive.Include).First().Definition;
            HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(slime.harvest[0].ingredient, Quality.Standard), 1, 1f),
                (Vector2)Room.Arrival.position + new Vector2(3f, 3f), null);
            yield return null;
            Assert.That(Object.FindObjectsByType<IngredientPickup>(), Is.Not.Empty);
            yield return TakeExit(1, "spider_den");
            Assert.That(Object.FindObjectsByType<IngredientPickup>(), Is.Empty, "left behind with the room");
            Assert.That(Runner.RoomsEntered, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator TheNavigationGrid_FollowsTheRoom()
        {
            yield return LoadRun();
            yield return TakeExit(0, "slime_hall");
            NavGrid grid = NavGrid.Current;
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.Bounds, Is.EqualTo(Room.TileBounds), "covers the new room");
            GridMap map = grid.Map;
            Assert.That(map.Width, Is.EqualTo(44));
            Assert.That(map.Height, Is.EqualTo(26));
            Vector2Int arrival = Vector2Int.FloorToInt(Room.Arrival.position);
            Assert.That(map.IsWalkable(new GridCell(arrival.x, arrival.y)), "the arrival is walkable");
            Assert.That(map.IsWalkable(new GridCell(0, 10)), Is.False, "the west wall is blocked");
            // A pillar (layout column 9, text rows 10–11 → y 14–15).
            Assert.That(map.IsWalkable(new GridCell(9, 15)), Is.False, "a pillar is blocked");
        }

        [UnityTest]
        public IEnumerator TheCamera_FollowsThePlayer_ButStaysInsideTheRoom()
        {
            yield return LoadRun();
            Camera camera = Camera.main;
            Rect room = new(Vector2.zero, Room.Size);

            IEnumerator CheckAt(Vector2 position)
            {
                Teleport(Player, position);
                for (int i = 0; i < 10; i++) yield return null;
                float height = camera.orthographicSize * 2f, width = height * camera.aspect;
                Vector2 centre = camera.transform.position;
                string at = $"at {position}: view {width:F2}×{height:F2} centred {centre}, ortho {camera.orthographicSize:F3}, aspect {camera.aspect:F3}, screen {Screen.width}×{Screen.height}, player {(Vector2)Player.transform.position}";
                // Where the view is smaller than the room it stays inside it; where it's larger it centres on the room.
                if (width <= room.width) Assert.That(centre.x - width / 2f >= -0.02f && centre.x + width / 2f <= room.width + 0.02f, "x inside " + at);
                else Assert.That(centre.x, Is.EqualTo(room.center.x).Within(0.05f), at);
                if (height <= room.height) Assert.That(centre.y - height / 2f >= -0.02f && centre.y + height / 2f <= room.height + 0.02f, "y inside " + at);
                else Assert.That(centre.y, Is.EqualTo(room.center.y).Within(0.05f), at);
            }

            yield return CheckAt(new Vector2(1.5f, 2.5f));
            yield return CheckAt(new Vector2(room.width - 1.5f, room.height - 2.5f));
        }

        [UnityTest]
        public IEnumerator TheRouteEndsAtTheRope()
        {
            yield return LoadRun();
            yield return TakeExit(0, "slime_hall");
            yield return ClearRoom();
            yield return TakeExit(0, "spider_den");
            yield return ClearRoom();
            yield return TakeExit(0, "rope");
            Assert.That(Room.Exits, Is.Empty);
            Assert.That(Runner.Encounter.IsCleared);
            Assert.That(Room.GetComponentInChildren<DelveExit>(), Is.Not.Null, "the way out");
            Assert.That(DelveRunController.Active, Is.Not.Null);
        }
    }
}
