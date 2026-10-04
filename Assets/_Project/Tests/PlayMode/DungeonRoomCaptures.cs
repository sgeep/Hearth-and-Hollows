using System.Collections;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Renders the 4d rooms at 320×180 into <c>BatchLogs/</c> so they can be looked at: the start room, a fight with
    /// its gates down, the gates up once it's clear, and the bigger room. Explicit, so it only runs when asked for by name.
    /// </summary>
    [Explicit]
    public class DungeonRoomCaptures : LookTestFixture
    {
        static RoomRunner Runner => RoomRunner.Active;

        IEnumerator Settle()
        {
            for (int i = 0; i < 30; i++) yield return null;
        }

        IEnumerator Next(string expected)
        {
            RoomExit exit = Runner.Current.Exits[0];
            Teleport(Player, (Vector2)exit.transform.position + new Vector2(0f, 0.3f));
            yield return WaitUntil(() => Runner.CurrentDefinition.id == expected && !Runner.IsTransitioning, 5f, expected);
            FreezeEnemies();
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator CaptureTheRooms()
        {
            yield return Load("Dungeon");
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the first room");
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_start.png");

            // Near the exit, to see the gate.
            Teleport(Player, (Vector2)Runner.Current.Exits[0].transform.position + new Vector2(0f, -3f));
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_start_exit.png");

            yield return Next("slime_hall");
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_slime_hall_sealed.png");
            Teleport(Player, (Vector2)Runner.Current.Exits[0].transform.position + new Vector2(0f, -3f));
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_slime_hall_gate_down.png");

            foreach (EnemyIdentity enemy in Runner.Current.GetComponentsInChildren<EnemyIdentity>())
            {
                var health = enemy.GetComponent<Health>();
                health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            }
            yield return new WaitForSeconds(1f);
            TavernEveningCaptures.Capture("BatchLogs/room_slime_hall_gate_up.png");

            yield return Next("spider_den");
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_spider_den.png");
            Teleport(Player, new Vector2(10f, 24f));
            yield return Settle();
            TavernEveningCaptures.Capture("BatchLogs/room_spider_den_corner.png");
        }
    }
}
