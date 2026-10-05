using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4b combat on the test floor, with the real player, enemies, TDE and physics: the heavy
    /// attack's charge, hit-stop, stagger and knockback, every enemy attack's telegraph, the dodge,
    /// the sleeping bat and the spider keeping its distance.
    /// </summary>
    public class DungeonCombatTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";
        // An open stretch along the bottom of room C (no props within 7 tiles to the east).
        static readonly Vector2 k_Arena = new(20.5f, 2.3f);

        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();

        [TearDown]
        public void RestoreSettings() => GameSettings.HitStopEnabled = true;

        /// <summary>Loads the floor, keeps one enemy of the given id (frozen), and puts the player in the arena.</summary>
        IEnumerator Setup(string enemyId, Vector2 offset)
        {
            yield return Load(TestFloorScene);
            EnemyIdentity keep = null;
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
            {
                if (keep == null && enemy.Definition.id == enemyId) keep = enemy;
                else Object.Destroy(enemy.gameObject);
            }
            Assert.That(keep, Is.Not.Null, $"the test floor has a {enemyId}");
            Enemy = keep;
            // A bat in these tests is already awake and flying, not hanging from its wall.
            Enemy.GetComponent<EnemyPerch>()?.Detach();
            Freeze(Enemy);
            Teleport(Player, k_Arena);
            Teleport(Enemy, k_Arena + offset);
            Player.GetComponent<AimControlSwitcher>().enabled = false;
            var handle = Player.GetComponent<CharacterHandleWeapon>();
            handle.WeaponAimComponent.AimControl = WeaponAim.AimControls.Script;
            handle.WeaponAimComponent.SetCurrentAim(offset.normalized);
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        EnemyIdentity Enemy;

        static void Freeze(EnemyIdentity enemy)
        {
            AIBrain brain = enemy.GetComponent<Character>().CharacterBrain;
            brain.BrainActive = false;
            enemy.GetComponent<CharacterMovement>().SetMovement(Vector2.zero);
        }

        EnemyAttack AttackOfKind(EnemyAttackKind kind) => Enemy.GetComponents<EnemyAttack>().First(a => a.Settings.kind == kind);

        IEnumerator HoldHeavy(float seconds)
        {
            InputSystem.QueueStateEvent(Pointer, new MouseState().WithButton(MouseButton.Right));
            yield return new WaitForSeconds(seconds);
            InputSystem.QueueStateEvent(Pointer, new MouseState());
        }

        [UnityTest]
        public IEnumerator HeavyAttack_ChargedHitsHarderThanATap_WithItsSpinAnimation()
        {
            yield return Setup("giant_spider", new Vector2(1.1f, 0f));
            Essence.GodMode = true;
            var health = Enemy.GetComponent<Health>();
            WeaponDefinition cleaver = Player.GetComponentInChildren<HeavyWeaponTuning>().Definition;
            var animator = Player.GetComponentInChildren<CharacterSpriteAnimator>();

            float before = health.CurrentHealth;
            yield return HoldHeavy(0.1f);
            yield return WaitUntil(() => health.CurrentHealth < before, 1f, "a tapped heavy to land");
            float tap = before - health.CurrentHealth;
            Assert.That(tap, Is.EqualTo(cleaver.heavy[0].attack.damage).Within(0.01f), "a tap gives the first step");
            yield return new WaitForSeconds(0.8f);

            health.SetHealth(health.MaximumHealth);
            Teleport(Enemy, k_Arena + new Vector2(1.1f, 0f));
            before = health.CurrentHealth;
            var seen = new HashSet<CharacterAnim>();
            InputSystem.QueueStateEvent(Pointer, new MouseState().WithButton(MouseButton.Right));
            float until = Time.time + 1.1f;
            while (Time.time < until)
            {
                seen.Add(animator.Current);
                yield return null;
            }
            InputSystem.QueueStateEvent(Pointer, new MouseState());
            yield return WaitUntil(() => health.CurrentHealth < before, 1f, "a charged heavy to land");
            float charged = before - health.CurrentHealth;
            yield return new WaitForSeconds(0.1f);
            seen.Add(animator.Current);

            // The full step, or all the target had left (the full charge, 40 since 4e, can exceed a small enemy's health).
            Assert.That(charged, Is.EqualTo(Mathf.Min(cleaver.heavy[^1].attack.damage, before)).Within(0.01f), "a full charge gives the last step");
            Assert.That(charged, Is.GreaterThan(tap));
            Assert.That(seen, Has.Member(CharacterAnim.Charge), "winds up");
            Assert.That(seen, Has.Member(CharacterAnim.ChargeHold), "then holds the charge");
            Assert.That(seen, Has.Member(CharacterAnim.HeavyAttack), "releases with the spin");
        }

        [UnityTest]
        public IEnumerator LightHit_FreezesTimeBriefly_UnlessHitStopIsOff()
        {
            yield return Setup("giant_spider", new Vector2(1.2f, 0f));
            Essence.GodMode = true;
            Assert.That(Object.FindAnyObjectByType<MoreMountains.Feedbacks.MMTimeManager>(), Is.Not.Null, "the floor has a time manager for freeze frames");
            var health = Enemy.GetComponent<Health>();

            float lowest = 1f;
            float before = health.CurrentHealth;
            yield return Click();
            float until = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < until)
            {
                lowest = Mathf.Min(lowest, Time.timeScale);
                yield return null;
            }
            Assert.That(health.CurrentHealth, Is.LessThan(before), "the hit landed");
            Assert.That(lowest, Is.LessThan(0.2f), "hit-stop froze time");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "and let go again");

            GameSettings.HitStopEnabled = false;
            yield return new WaitForSeconds(0.6f);
            Teleport(Enemy, k_Arena + new Vector2(1.2f, 0f));
            lowest = 1f;
            before = health.CurrentHealth;
            yield return Click();
            until = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < until)
            {
                lowest = Mathf.Min(lowest, Time.timeScale);
                yield return null;
            }
            Assert.That(health.CurrentHealth, Is.LessThan(before), "the hit landed");
            Assert.That(lowest, Is.EqualTo(1f), "no hit-stop when the setting is off");
        }

        [UnityTest]
        public IEnumerator Hit_InterruptsATelegraphedSwoop_StaggersAndKnocksTheBatBack()
        {
            yield return Setup("bat", new Vector2(1.3f, 0f));
            Essence.GodMode = true;
            EnemyAttack swoop = AttackOfKind(EnemyAttackKind.Swoop);
            var reaction = Enemy.GetComponent<HitReaction>();
            Assert.That(swoop.Begin(Player.transform), "the bat starts its swoop");
            Assert.That(swoop.Cycle.Phase, Is.EqualTo(EnemyAttackPhase.Telegraph));
            float distanceBefore = Vector2.Distance(Enemy.transform.position, Player.transform.position);

            yield return Click();
            yield return WaitUntil(() => reaction.LastHit.HasValue, 0.4f, "the hit to land during the telegraph");
            Assert.That(swoop.Cycle.Phase, Is.EqualTo(EnemyAttackPhase.Cooldown), "the swoop was interrupted");
            Assert.That(reaction.IsStaggered, "the bat is staggered");
            Assert.That(swoop.Alert.activeSelf, Is.False, "the alert went away with the attack");
            yield return new WaitForSeconds(0.15f);
            Assert.That(Vector2.Distance(Enemy.transform.position, Player.transform.position), Is.GreaterThan(distanceBefore + 0.3f), "knocked back, away from the player");
            Assert.That(reaction.LastHit.Value.Weapon, Is.Not.Null, "the hit knows its weapon (for the harvest)");
        }

        [UnityTest]
        public IEnumerator Hit_DoesNotInterruptTheSlimesLeap()
        {
            yield return Setup("green_slime", new Vector2(1.3f, 0f));
            Essence.GodMode = true;
            EnemyAttack leap = AttackOfKind(EnemyAttackKind.Leap);
            var reaction = Enemy.GetComponent<HitReaction>();
            Assert.That(leap.Begin(Player.transform));
            yield return Click();
            yield return WaitUntil(() => reaction.LastHit.HasValue, 0.4f, "the hit to land during the telegraph");
            Assert.That(leap.Cycle.Phase, Is.Not.EqualTo(EnemyAttackPhase.Cooldown), "super armour: the leap carries on");
            Assert.That(reaction.IsStaggered, Is.False);
            yield return WaitUntil(() => leap.Cycle.Phase == EnemyAttackPhase.Active, 1f, "the leap itself");
        }

        // The fairness rule (GDD §4.1): nothing hurts the player before its telegraph has fully played.
        [UnityTest]
        public IEnumerator SlimeLeap_IsTelegraphedFully_BeforeItHurts() => TelegraphThenHurt("green_slime", EnemyAttackKind.Leap, 2f);

        [UnityTest]
        public IEnumerator BatSwoop_IsTelegraphedFully_BeforeItHurts() => TelegraphThenHurt("bat", EnemyAttackKind.Swoop, 2.5f);

        [UnityTest]
        public IEnumerator SpiderBite_IsTelegraphedFully_BeforeItHurts() => TelegraphThenHurt("giant_spider", EnemyAttackKind.Bite, 1.2f);

        [UnityTest]
        public IEnumerator SpiderWeb_IsTelegraphedFully_BeforeItHurts() => TelegraphThenHurt("giant_spider", EnemyAttackKind.Spit, 5f);

        IEnumerator TelegraphThenHurt(string enemyId, EnemyAttackKind kind, float distance)
        {
            var damaged = new List<CharacterDamaged>();
            EventBus<CharacterDamaged>.Subscribe(damaged.Add);
            yield return Setup(enemyId, new Vector2(distance, 0f));
            EnemyAttack attack = AttackOfKind(kind);
            float telegraphAt = -1f;
            bool alertShown = false;
            attack.PhaseChanged += phase =>
            {
                if (phase == EnemyAttackPhase.Telegraph) telegraphAt = Time.time;
            };
            Assert.That(attack.Begin(Player.transform), $"{enemyId} starts its {kind}");
            while (attack.Cycle.Phase == EnemyAttackPhase.Telegraph)
            {
                alertShown |= attack.Alert.activeSelf;
                Assert.That(damaged.Any(d => d.TargetIsPlayer), Is.False, "nothing hurts during the telegraph");
                yield return null;
            }
            yield return WaitUntil(() => damaged.Any(d => d.TargetIsPlayer), 2f, $"the {kind} to hurt the player");
            float hurtAt = Time.time;

            Assert.That(alertShown, "the alert showed during the telegraph");
            Assert.That(hurtAt - telegraphAt, Is.GreaterThanOrEqualTo(attack.Settings.telegraph - 0.02f), "hurt only after the full telegraph");
            // Read from the hit itself: Essence also drains over time.
            Assert.That(damaged.First(d => d.TargetIsPlayer).Damage, Is.EqualTo(attack.Settings.damage).Within(0.01f), "the attack's damage, from its data");
        }

        /// <summary>4e playtest: the roll goes through an enemy in the way instead of shoving it.</summary>
        [UnityTest]
        public IEnumerator Dodge_RollsThroughAnEnemy_WithoutPushingIt()
        {
            yield return Setup("green_slime", new Vector2(1.5f, 0f));
            Vector2 slimeAt = Enemy.transform.position;
            Hold(Key.D);
            yield return null;
            yield return null;
            Hold(Key.D, Key.Space);
            bool ghosted = false;
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForFixedUpdate();
                ghosted |= Player.GetComponent<Hearthdelve.Dungeon.Player.DodgeThroughEnemies>().IsGhosted;
            }
            ReleaseKeys();
            yield return new WaitForSeconds(0.4f);
            Assert.That(ghosted, "ghosted through the roll");
            Assert.That(Player.transform.position.x, Is.GreaterThan(slimeAt.x + 0.5f), "came out the other side");
            Assert.That(Vector2.Distance(Enemy.transform.position, slimeAt), Is.LessThan(0.2f), "the slime wasn't pushed");
            Assert.That(Player.GetComponent<Hearthdelve.Dungeon.Player.DodgeThroughEnemies>().IsGhosted, Is.False, "solid again after");
        }

        [UnityTest]
        public IEnumerator Dodge_AvoidsTheSlimesLeap()
        {
            var damaged = new List<CharacterDamaged>();
            EventBus<CharacterDamaged>.Subscribe(damaged.Add);
            yield return Setup("green_slime", new Vector2(2f, 0f));
            EnemyAttack leap = AttackOfKind(EnemyAttackKind.Leap);
            bool active = false;
            leap.PhaseChanged += phase => active |= phase == EnemyAttackPhase.Active;
            Assert.That(leap.Begin(Player.transform));
            // Read the telegraph, then roll sideways out of the landing spot as the leap starts.
            yield return WaitUntil(() => leap.Cycle.Phase == EnemyAttackPhase.Telegraph && leap.Cycle.PhaseElapsed >= leap.Settings.telegraph - 0.12f, 2f, "near the end of the telegraph");
            Hold(Key.W);
            yield return null;
            yield return null;
            Hold(Key.W, Key.Space);
            bool invulnerable = false;
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                invulnerable |= Essence.Invulnerable;
            }
            Hold(Key.W);
            yield return WaitUntil(() => leap.Cycle.Phase == EnemyAttackPhase.Cooldown, 2f, "the leap to finish");
            ReleaseKeys();
            Assert.That(active, "the leap happened");
            Assert.That(invulnerable, "the dodge has i-frames");
            Assert.That(damaged.Any(d => d.TargetIsPlayer), Is.False,
                $"the dodge avoided the leap (player ended at {(Vector2)Player.transform.position}, slime at {(Vector2)Enemy.transform.position})");
        }

        /// <summary>
        /// Sleeping bats hang from walls, never in open air: each bat on the floor starts on a perch
        /// (a wall right above it, the hanging pose, no ground shadow). When the player comes near it
        /// lets go, unfolds in place, then flies (shadow back) and gives chase.
        /// </summary>
        [UnityTest]
        public IEnumerator Bats_HangFromWalls_UntilWoken_ThenUnfoldAndGiveChase()
        {
            yield return Load(TestFloorScene);
            Essence.GodMode = true;
            EnemyPerch[] bats = Object.FindObjectsByType<EnemyPerch>();
            Assert.That(bats.Length, Is.GreaterThanOrEqualTo(2), "the floor has its bats");
            foreach (EnemyPerch perch in bats)
            {
                Assert.That(perch.IsPerched, $"{perch.name} at {(Vector2)perch.transform.position} hangs from a wall");
                Assert.That(EnemyPerch.WallAbove(perch.transform.position, 0.7f), $"{perch.name}: a wall right above it");
                Assert.That(perch.GetComponentInChildren<CharacterSpriteAnimator>().Current, Is.EqualTo(CharacterAnim.Sleep), $"{perch.name}: the hanging pose");
                Assert.That(perch.transform.Find("Model/Shadow").gameObject.activeSelf, Is.False, $"{perch.name}: no ground shadow while hanging");
            }

            EnemyPerch bat = bats.OrderBy(b => b.transform.position.x).First();
            AIBrain brain = bat.GetComponent<Character>().CharacterBrain;
            var animator = bat.GetComponentInChildren<CharacterSpriteAnimator>();
            Vector2 perchedAt = bat.transform.position;
            // Within its wake range, but outside its swoop range, so it has to fly closer first.
            Teleport(Player, perchedAt + new Vector2(0f, -3.8f));
            yield return WaitUntil(() => brain.CurrentState.StateName == "Wake", 1f, "the bat to wake");
            yield return null;
            Assert.That(bat.IsPerched, Is.False, "it let go of the wall");
            Assert.That(animator.Current, Is.EqualTo(CharacterAnim.Wake), "unfolding");
            Assert.That(Vector2.Distance(bat.transform.position, perchedAt), Is.LessThan(0.05f), "in place, before it flies");
            yield return WaitUntil(() => brain.CurrentState.StateName == "Chase", 1f, "the chase");
            Assert.That(bat.transform.Find("Model/Shadow").gameObject.activeSelf, "flying: its shadow is back");
            yield return new WaitForSeconds(0.5f);
            Assert.That(animator.Current, Is.Not.EqualTo(CharacterAnim.Sleep).And.Not.EqualTo(CharacterAnim.Wake), "flying");
            Assert.That(Vector2.Distance(bat.transform.position, perchedAt), Is.GreaterThan(0.2f), "it flies after the player");
        }

        /// <summary>A bat placed away from any wall hovers in its flying idle (and says so), rather than hanging in mid-air.</summary>
        [UnityTest]
        public IEnumerator ABatWithoutAWall_HoversInsteadOfHanging_AndWarns()
        {
            yield return Load(TestFloorScene);
            Essence.GodMode = true;
            EnemyPerch original = Object.FindAnyObjectByType<EnemyPerch>();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("has no wall above it to hang from"));
            EnemyPerch stray = Object.Instantiate(original, (Vector3)(k_Arena + new Vector2(3f, 1f)), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.That(stray.HasPerch, Is.False);
            Assert.That(stray.IsPerched, Is.False);
            Assert.That(stray.GetComponentInChildren<CharacterSpriteAnimator>().Current, Is.Not.EqualTo(CharacterAnim.Sleep), "never the hanging pose in open air");
            Assert.That(stray.transform.Find("Model/Shadow").gameObject.activeSelf, "a hovering bat keeps its shadow");
        }

        /// <summary>
        /// The step 4 playtest: the slime and bat chased right into the player and shoved them around,
        /// and the bat was then too close to ever swoop. Now they hold off at their stand-off distance and
        /// attack a player who doesn't move at all.
        /// </summary>
        [UnityTest]
        public IEnumerator SlimeAndBat_HoldOffInsteadOfPushing_AndAttackAStillPlayer([Values("green_slime", "bat")] string enemyId)
        {
            yield return Setup(enemyId, new Vector2(5f, 0f));
            Essence.GodMode = true;
            Vector2 standing = Player.transform.position;
            EnemyAttack attack = Enemy.GetComponent<EnemyAttack>();
            bool telegraphed = false;
            attack.PhaseChanged += phase => telegraphed |= phase == EnemyAttackPhase.Telegraph;
            AIBrain brain = Enemy.GetComponent<Character>().CharacterBrain;
            brain.Target = Player.transform;
            brain.BrainActive = true;
            brain.TransitionToState("Chase");

            float closest = float.MaxValue;
            float until = Time.time + 6f;
            while (!telegraphed && Time.time < until)
            {
                closest = Mathf.Min(closest, Vector2.Distance(Enemy.transform.position, Player.transform.position));
                yield return null;
            }
            Assert.That(telegraphed, $"the {enemyId} attacked a player who stood still");
            Assert.That(Vector2.Distance(Player.transform.position, standing), Is.LessThan(0.1f), $"the {enemyId} didn't shove the player while closing in");
            Assert.That(closest, Is.GreaterThanOrEqualTo(Enemy.Definition.keepDistance.x * 0.8f), "it kept its stand-off");
        }

        [UnityTest]
        public IEnumerator Spider_BacksOffToKeepItsDistance()
        {
            yield return Setup("giant_spider", new Vector2(2.3f, 0f));
            Essence.GodMode = true;
            float keep = Enemy.Definition.keepDistance.x;
            AIBrain brain = Enemy.GetComponent<Character>().CharacterBrain;
            brain.BrainActive = true;
            yield return WaitUntil(() => Vector2.Distance(Enemy.transform.position, Player.transform.position) >= keep - 0.3f, 3f, "the spider to back away");
        }
    }
}
