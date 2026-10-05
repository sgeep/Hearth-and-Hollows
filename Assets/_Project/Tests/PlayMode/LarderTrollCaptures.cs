using System.Collections;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Renders the Larder Troll's arena at 320×180 into <c>BatchLogs/troll_*.png</c> (4e): found eating, the entrance
    /// with the boss bar, the slam's floor mark, the charge's line, and the stun after a wall. Explicit.
    /// </summary>
    [Explicit]
    public class LarderTrollCaptures : LookTestFixture
    {
        static RoomRunner Runner => RoomRunner.Active;

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureTheTroll()
        {
            RoomRunner.SeedOverride = 20261004;
            RoomRunner.StartInArenaOverride = true;
            try
            {
                yield return Load("Dungeon");
                yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the arena");
                Player.GetComponent<EssenceHealth>().GodMode = true;
                var encounter = Object.FindAnyObjectByType<BossEncounter>();
                Character troll = encounter.GetComponent<Character>();
                // Walk the player up near the troll so it's on screen.
                Teleport(Player, (Vector2)troll.transform.position + new Vector2(-3f, -6f));
                yield return Frames(20);
                TavernEveningCaptures.Capture("BatchLogs/troll_1_eating.png");
                yield return WaitUntil(() => encounter.State == BossEncounterState.Fighting, 6f, "the fight");
                FreezeEnemies();

                EnemyAttack[] attacks = troll.GetComponents<EnemyAttack>();
                Teleport(Player, (Vector2)troll.transform.position + new Vector2(0.5f, -1.8f));
                yield return new WaitForFixedUpdate();
                attacks[0].Begin(Player.transform);
                yield return new WaitForSeconds(0.6f);
                TavernEveningCaptures.Capture("BatchLogs/troll_2_slam_mark.png");
                yield return WaitUntil(() => attacks[0].Cycle.Phase == EnemyAttackPhase.Active, 2f, "the slam");
                yield return Frames(2);
                TavernEveningCaptures.Capture("BatchLogs/troll_3_slam.png");
                yield return WaitUntil(() => attacks[0].Cycle.Phase == EnemyAttackPhase.Ready, 5f, "the slam to finish");

                RoomInstance room = Runner.Current;
                Vector2 left = (Vector2)room.transform.position + new Vector2(10f, room.Size.y / 2f);
                Teleport(troll, left);
                Teleport(Player, left + new Vector2(-6f, 0f));
                yield return new WaitForFixedUpdate();
                attacks[1].Begin(Player.transform);
                yield return new WaitForSeconds(0.6f);
                TavernEveningCaptures.Capture("BatchLogs/troll_4_charge_line.png");
                yield return WaitUntil(() => attacks[1].Cycle.Phase == EnemyAttackPhase.Active, 2f, "the charge");
                Teleport(Player, left + new Vector2(-3f, 4f));
                var stun = troll.GetComponent<ChargeStun>();
                yield return WaitUntil(() => stun.IsStunned, 3f, "the wall");
                yield return new WaitForSeconds(0.3f);
                TavernEveningCaptures.Capture("BatchLogs/troll_5_stunned.png");
            }
            finally
            {
                RoomRunner.SeedOverride = 0;
                RoomRunner.StartInArenaOverride = false;
            }
        }
    }
}
