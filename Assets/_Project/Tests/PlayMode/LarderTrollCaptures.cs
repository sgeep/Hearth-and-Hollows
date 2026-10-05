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
                yield return WaitUntil(() => !stun.IsStunned, 4f, "the troll to recover");

                // Step 2: a part on the floor, eaten (the player kept far off); then the frenzy; then the fall.
                var health = troll.GetComponent<Health>();
                health.SetHealth(health.MaximumHealth * 0.7f);
                Vector2 middle = (Vector2)room.transform.position + new Vector2(room.Size.x / 2f, room.Size.y / 2f + 4f);
                Teleport(troll, middle);
                troll.CharacterBrain.BrainActive = true;
                troll.GetComponent<Hearthdelve.Dungeon.Bosses.LarderScraps>().Drop((Vector2)troll.transform.position + new Vector2(2f, -1.5f));
                var eater = troll.GetComponent<Hearthdelve.Dungeon.Bosses.ScrapEater>();
                Vector2 far = (Vector2)room.transform.position + new Vector2(room.Size.x - 3f, 3f);
                for (float t = 0f; t < 6f && !eater.IsEating; t += Time.deltaTime)
                {
                    Teleport(Player, far);
                    yield return null;
                }
                Teleport(Player, (Vector2)troll.transform.position + new Vector2(-4f, -4f));
                yield return new WaitForSeconds(0.4f);
                TavernEveningCaptures.Capture("BatchLogs/troll_6_eating.png");
                yield return WaitUntil(() => !eater.IsEating, 4f, "the meal");
                FreezeEnemies();
                health.Damage(health.CurrentHealth - health.MaximumHealth * 0.45f, Player.gameObject, 0f, 0f, Vector3.zero);
                yield return new WaitForSeconds(0.5f);
                TavernEveningCaptures.Capture("BatchLogs/troll_7_frenzy.png");
                yield return new WaitForSeconds(1.2f);
                FreezeEnemies();
                health.Damage(health.CurrentHealth + 10f, Player.gameObject, 0f, 0f, Vector3.zero);
                yield return new WaitForSecondsRealtime(1.2f);
                TavernEveningCaptures.Capture("BatchLogs/troll_8_falls.png");
            }
            finally
            {
                RoomRunner.SeedOverride = 0;
                RoomRunner.StartInArenaOverride = false;
            }
        }
    }
}
