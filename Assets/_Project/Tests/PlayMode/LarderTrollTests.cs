using System.Collections;
using System.Linq;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
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

            var health = Troll.GetComponent<BossHealth>();
            health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            // Brought down (step 3), then let fall without the finisher.
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => Encounter == null || Encounter.State == BossEncounterState.Defeated, 3f, "the troll to fall");
            var bar = Object.FindAnyObjectByType<BossHealthBar>();
            Assert.That(bar.IsCaption && bar.IsShown, $"the bar says it fell (state {(Encounter != null ? Encounter.State.ToString() : "gone")}, shown {bar.IsShown}, caption {bar.IsCaption}, health {health.CurrentHealth})");
            yield return WaitUntil(() => Runner.Encounter.IsCleared, 5f, "the arena to clear");
            float after = essence.Essence.Current;
            yield return new WaitForSeconds(1f);
            Assert.That(essence.Essence.Current, Is.LessThan(after), "drain is back once the fight is over");
            yield return WaitUntil(() => !bar.IsShown, 5f, "the caption to go");
        }

        [UnityTest]
        public IEnumerator TheEntrance_ShowsTheTroll_AndHoldsThePlayer_ThenHandsBack()
        {
            yield return LoadArena();
            yield return WaitUntil(() => Encounter.State == BossEncounterState.Entrance, 4f, "the entrance");
            Assert.That(Runner.CameraFollow, Is.SameAs(Troll.transform), "the camera finds it at its meal");
            Assert.That(Hearthdelve.Core.Input.InputMaps.Find(Hearthdelve.Core.Input.InputMaps.Dungeon, "Attack").enabled, Is.False, "the player waits");
            yield return UntilFighting();
            Assert.That(Runner.CameraFollow, Is.Not.Null.And.Not.SameAs(Troll.transform), "back on the player");
            Assert.That(Vector2.Distance(Runner.CameraFollow.position, Player.transform.position), Is.LessThan(1f), "following the player again");
            Assert.That(Hearthdelve.Core.Input.InputMaps.Find(Hearthdelve.Core.Input.InputMaps.Dungeon, "Attack").enabled, "and the player acts");
        }

        /// <summary>Keeps the player far off (out of the slam's reach and line), so the troll's choice is the food.</summary>
        IEnumerator KeepAway(float seconds, System.Func<bool> until)
        {
            Vector2 corner = (Vector2)Room.transform.position + new Vector2(Room.Size.x - 3f, 3f);
            for (float t = 0f; t < seconds && !until(); t += Time.deltaTime)
            {
                Teleport(Player, corner);
                yield return null;
            }
        }

        IEnumerator AMealOnTheFloor(System.Action<IngredientPickup> found)
        {
            yield return LoadArena();
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return UntilFighting();
            var health = Troll.GetComponent<Health>();
            health.SetHealth(health.MaximumHealth * 0.7f);
            IngredientPickup part = Troll.GetComponent<LarderScraps>().Drop((Vector2)Troll.transform.position + new Vector2(2f, -2f));
            Assert.That(part, Is.Not.Null, "a part shaken loose");
            found(part);
        }

        [UnityTest]
        public IEnumerator APartOnTheFloor_IsEaten_AndHealsTheTroll()
        {
            IngredientPickup part = null;
            yield return AMealOnTheFloor(p => part = p);
            var eater = Troll.GetComponent<ScrapEater>();
            var health = Troll.GetComponent<Health>();
            yield return KeepAway(6f, () => eater.IsEating);
            Assert.That(eater.IsEating, "it went for the part");
            float atMeal = health.CurrentHealth;
            yield return KeepAway(4f, () => eater.Eaten > 0);
            Assert.That(eater.Eaten, Is.EqualTo(1));
            Assert.That(health.CurrentHealth, Is.EqualTo(atMeal + health.MaximumHealth * 0.08f).Within(1f), "healed by the meal");
            Assert.That(part == null, "eaten up");
        }

        [UnityTest]
        public IEnumerator TakingThePartFirst_DeniesTheMeal_AndItGoesHomeInTheSatchel()
        {
            IngredientPickup part = null;
            yield return AMealOnTheFloor(p => part = p);
            yield return WaitUntil(() => part.IsCollectable, 2f, "the part to land");
            var satchel = Player.GetComponent<SatchelCarrier>().Satchel;
            int before = satchel.TotalCount;
            Teleport(Player, part.transform.position);
            yield return WaitUntil(() => part == null, 2f, "the part taken");
            Assert.That(satchel.TotalCount, Is.EqualTo(before + 1), "an ordinary part in the satchel");
            yield return new WaitForSeconds(0.5f);
            var eater = Troll.GetComponent<ScrapEater>();
            Assert.That(eater.Eaten, Is.Zero);
            Assert.That(eater.IsEating, Is.False);
        }

        [UnityTest]
        public IEnumerator HittingItHard_WhileItEats_SpoilsTheMeal()
        {
            IngredientPickup part = null;
            yield return AMealOnTheFloor(p => part = p);
            var eater = Troll.GetComponent<ScrapEater>();
            var health = Troll.GetComponent<Health>();
            yield return KeepAway(6f, () => eater.IsEating);
            Assert.That(eater.IsEating);
            float atMeal = health.CurrentHealth;
            health.Damage(50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            yield return null;
            Assert.That(eater.Spoiled, Is.EqualTo(1), "the meal is ruined");
            Assert.That(eater.IsEating, Is.False);
            Assert.That(part == null, "the part is gone");
            Assert.That(health.CurrentHealth, Is.LessThan(atMeal), "nothing healed");
        }

        [UnityTest]
        public IEnumerator AtHalfHealth_ItRoars_ThenFightsFaster()
        {
            yield return LoadArena();
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return UntilFighting();
            var health = Troll.GetComponent<Health>();
            var frenzy = Troll.GetComponent<BossFrenzy>();
            health.Damage(health.MaximumHealth * 0.55f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            yield return null;
            Assert.That(frenzy.IsFrenzied && frenzy.IsRoaring, "it roars");
            float during = health.CurrentHealth;
            health.Damage(100f, Player.gameObject, 0f, 0f, Vector3.zero);
            Assert.That(health.CurrentHealth, Is.EqualTo(during), "it can't be hurt while it roars");
            Assert.That(Troll.CharacterBrain.BrainActive, Is.False, "and doesn't act");
            yield return WaitUntil(() => !frenzy.IsRoaring, 3f, "the roar to end");
            Assert.That(Troll.CharacterBrain.BrainActive, "then it fights");
            Assert.That(Troll.GetComponent<CharacterMovement>().MovementSpeedMultiplier, Is.GreaterThan(1f), "faster");
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
