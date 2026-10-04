using System.Collections;
using System.Linq;
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
    /// Renders a seeded 4d run at 320×180 into <c>BatchLogs/</c> so its rooms can be looked at: the start, a fight with
    /// its gates down and up (with the signs over the exits), more fights, the hole, a deeper floor and the arena.
    /// Explicit, so it only runs when asked for by name.
    /// </summary>
    [Explicit]
    public class DungeonRoomCaptures : LookTestFixture
    {
        static RoomRunner Runner => RoomRunner.Active;
        static FloorNode Node => Runner.Node;

        [TearDown]
        public void ReleaseTheSeed() => RoomRunner.SeedOverride = 0;

        IEnumerator Settle()
        {
            for (int i = 0; i < 30; i++) yield return null;
        }

        void Shot(string name) => TavernEveningCaptures.Capture($"BatchLogs/run_{Runner.RoomsEntered:00}_f{Runner.Floor.Floor}_{name}.png");

        IEnumerator Clear()
        {
            foreach (EnemyIdentity enemy in Runner.Current.GetComponentsInChildren<EnemyIdentity>())
            {
                var health = enemy.GetComponent<Health>();
                health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            }
            yield return new WaitForSeconds(1f);
        }

        IEnumerator Go(System.Func<FloorNode, bool> wanted)
        {
            RoomExit exit = Runner.Current.Exits.Where(e => e.Index < Node.Next.Count).FirstOrDefault(e => wanted(Runner.Floor.Node(Node.Next[e.Index])))
                            ?? Runner.Current.Exits.First(e => e.Index < Node.Next.Count);
            int entered = Runner.RoomsEntered;
            Teleport(Player, (Vector2)exit.transform.position + new Vector2(0f, 0.3f));
            yield return WaitUntil(() => Runner.RoomsEntered == entered + 1 && !Runner.IsTransitioning, 5f, "the next room");
            FreezeEnemies();
            yield return new WaitForSeconds(0.5f);
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator CaptureARun()
        {
            RoomRunner.SeedOverride = 20261004;
            yield return Load("Dungeon");
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the first room");
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return Settle();
            Shot($"{Node.RoomId}");

            for (int floor = 1; floor <= 3; floor++)
            {
                for (int guard = 0; guard < 12 && Node.Kind is not (RoomKind.Descent or RoomKind.Arena); guard++)
                {
                    if (Node.Kind == RoomKind.Combat)
                    {
                        Shot($"{Node.RoomId}_sealed");
                        yield return Clear();
                        // Near the exits, to see the gates and their signs.
                        Teleport(Player, (Vector2)Runner.Current.Exits[0].transform.position + new Vector2(0f, -3f));
                        yield return Settle();
                        Shot($"{Node.RoomId}_open");
                    }
                    yield return Go(n => n.Kind is RoomKind.Descent or RoomKind.Arena or RoomKind.Combat);
                }
                if (Node.Kind == RoomKind.Arena) break;
                Shot($"{Node.RoomId}");
                Teleport(Player, Runner.Current.Descent.transform.position);
                yield return WaitUntil(() => Runner.Floor.Floor == floor + 1 && !Runner.IsTransitioning, 5f, "the next floor");
                FreezeEnemies();
                yield return Settle();
                Shot($"{Node.RoomId}_landed");
            }
            Shot("arena_sealed");
            yield return Clear();
            Shot("arena_clear");
        }
    }
}
