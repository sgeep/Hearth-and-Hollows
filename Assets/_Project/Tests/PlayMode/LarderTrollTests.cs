using System.Collections;
using System.Linq;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.UI.Hud;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4e step 1, in the <c>Dungeon</c> scene starting in the arena: the Larder Troll is found eating, enters (named on
    /// the boss bar) and fights; passive drain pauses while it does and comes back when it falls; its slam marks the
    /// floor and hurts there; its charge into a wall stuns it; and its defeat clears the arena.
    /// </summary>
    public class LarderTrollTests : LookTestFixture
    {
        static RoomRunner Runner => RoomRunner.Active;
        static RoomInstance Room => Runner.Current;

        BossEncounter Encounter => Object.FindAnyObjectByType<BossEncounter>();
        Character Troll => Encounter.GetComponent<Character>();

        [SetUp]
        public void StartInTheArena()
        {
            RoomRunner.SeedOverride = 20261004;
            RoomRunner.StartInArenaOverride = true;
        }

        [TearDown]
        public void Release()
        {
            RoomRunner.SeedOverride = 0;
            RoomRunner.StartInArenaOverride = false;
        }

        IEnumerator LoadArena()
        {
            yield return Load("Dungeon");
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the arena");
            Assert.That(Runner.Node.Kind, Is.EqualTo(RoomKind.Arena));
        }

        IEnumerator UntilFighting()
        {
            yield return WaitUntil(() => Encounter != null && Encounter.State == BossEncounterState.Fighting, 6f, "the fight");
        }

        [UnityTest]
        public IEnumerator TheTroll_IsFoundEating_ThenEnters_ThenFights()
        {
            yield return LoadArena();
            Assert.That(Encounter, Is.Not.Null, "the arena holds the troll");
            Assert.That(Object.FindObjectsByType<EnemyIdentity>(), Has.Length.EqualTo(1), "and nothing else");
            var bar = Object.FindAnyObjectByType<BossHealthBar>();
            yield return WaitUntil(() => Encounter.State == BossEncounterState.Entrance, 4f, "the entrance");
            Assert.That(Troll.CharacterBrain.BrainActive, Is.False, "it doesn't act during the entrance");
            yield return null;
            Assert.That(bar.IsShown, "named on the boss bar");
            Assert.That(bar.BossId, Is.EqualTo("larder_troll"));
            Assert.That(bar.Fraction, Is.EqualTo(1f).Within(1e-3f));
            yield return UntilFighting();
            Assert.That(Troll.CharacterBrain.BrainActive, "then it fights");
        }

        [UnityTest]
        public IEnumerator DrainPauses_DuringTheFight_HitsStillCost_AndDrainReturnsAfter()
        {
            yield return LoadArena();
            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            yield return UntilFighting();
            FreezeEnemies();
            float before = essence.Essence.Current;
            yield return new WaitForSeconds(1.5f);
            Assert.That(essence.Essence.Current, Is.EqualTo(before).Within(1e-3f), "no passive drain while the troll fights");
            essence.Essence.TakeDamage(10f);
            Assert.That(essence.Essence.Current, Is.EqualTo(before - 10f).Within(1e-3f), "hits still cost");

            var health = Troll.GetComponent<Health>();
            health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => Encounter == null || Encounter.State == BossEncounterState.Defeated, 3f, "the troll to fall");
            yield return WaitUntil(() => Runner.Encounter.IsCleared, 5f, "the arena to clear");
            float after = essence.Essence.Current;
            yield return new WaitForSeconds(1f);
            Assert.That(essence.Essence.Current, Is.LessThan(after), "drain is back once the fight is over");
            Assert.That(Object.FindAnyObjectByType<BossHealthBar>().IsShown, Is.False, "the bar goes");
        }

        [UnityTest]
        public IEnumerator TheSlam_MarksTheFloor_AndHurtsThere()
        {
            yield return LoadArena();
            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            yield return UntilFighting();
            FreezeEnemies();
            Teleport(Player, (Vector2)Troll.transform.position + new Vector2(0f, -1.2f));
            yield return new WaitForFixedUpdate();
            EnemyAttack slam = Troll.GetComponents<EnemyAttack>()[0];
            AttackTelegraphMarker mark = Troll.GetComponents<AttackTelegraphMarker>().First();
            float before = essence.Essence.Current;
            Assert.That(slam.Begin(Player.transform), "the slam starts");
            yield return null;
            Assert.That(mark.IsShowing, "the floor shows where it lands");
            yield return WaitUntil(() => slam.Cycle.Phase == EnemyAttackPhase.Recovery, 3f, "the slam to land");
            yield return null;
            Assert.That(mark.IsShowing, Is.False);
            Assert.That(essence.Essence.Current, Is.LessThan(before - 10f), "standing in the mark costs");
        }

        [UnityTest]
        public IEnumerator TheCharge_IntoAWall_StunsTheTroll_ThenItRecovers()
        {
            yield return LoadArena();
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return UntilFighting();
            FreezeEnemies();
            // The troll on the left side, the player between it and the west wall: it charges west, the player steps aside.
            Vector2 left = (Vector2)Room.transform.position + new Vector2(10f, Room.Size.y / 2f);
            Teleport(Troll, left);
            Teleport(Player, left + new Vector2(-6f, 0f));
            yield return new WaitForFixedUpdate();
            EnemyAttack charge = Troll.GetComponents<EnemyAttack>()[1];
            var stun = Troll.GetComponent<ChargeStun>();
            Assert.That(charge.Begin(Player.transform), "the charge starts");
            yield return null;
            Assert.That(Troll.GetComponents<AttackTelegraphMarker>()[1].IsShowing, "its line is marked");
            yield return WaitUntil(() => charge.Cycle.Phase == EnemyAttackPhase.Active, 2f, "the charge");
            Teleport(Player, left + new Vector2(-3f, 5f));
            yield return WaitUntil(() => stun.IsStunned, 3f, "the troll to hit the wall");
            Assert.That(Troll.GetComponent<CharacterMovement>() != null);
            yield return WaitUntil(() => !stun.IsStunned, 4f, "the troll to recover");
            Assert.That(stun.Stuns, Is.EqualTo(1));
        }
    }
}
