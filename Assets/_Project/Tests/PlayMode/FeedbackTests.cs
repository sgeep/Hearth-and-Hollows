using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
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
    /// Step 6, the feedback and haptics pass on the test floor: each moment plays its one combined
    /// feedback with the right named pattern, once, and the quiet moments stay quiet.
    /// </summary>
    public class FeedbackTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";
        static readonly Vector2 k_Arena = new(20.5f, 2.3f);

        readonly List<string> m_Patterns = new();
        EnemyIdentity Enemy;

        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();
        Satchel Satchel => Player.GetComponent<SatchelCarrier>().Satchel;
        SatchelCarrier Carrier => Player.GetComponent<SatchelCarrier>();
        int Played(string id) => m_Patterns.Count(p => p == id);

        void Record(string id) => m_Patterns.Add(id);

        [SetUp]
        public void Listen()
        {
            m_Patterns.Clear();
            HapticService.PatternPlayed += Record;
        }

        [TearDown]
        public void StopListening() => HapticService.PatternPlayed -= Record;

        /// <summary>Loads the floor, keeps one enemy (frozen) at an offset from the player, aiming at it.</summary>
        IEnumerator Setup(string enemyId, Vector2 offset)
        {
            yield return Load(TestFloorScene);
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
            {
                if (Enemy == null && enemy.Definition.id == enemyId) Enemy = enemy;
                else Object.Destroy(enemy.gameObject);
            }
            Enemy.GetComponent<EnemyPerch>()?.Detach();
            Enemy.GetComponent<Character>().CharacterBrain.BrainActive = false;
            Essence.GodMode = true;
            Teleport(Player, k_Arena);
            Teleport(Enemy, k_Arena + offset);
            Player.GetComponent<AimControlSwitcher>().enabled = false;
            var handle = Player.GetComponent<CharacterHandleWeapon>();
            handle.WeaponAimComponent.AimControl = WeaponAim.AimControls.Script;
            handle.WeaponAimComponent.SetCurrentAim(offset.normalized);
            yield return new WaitForFixedUpdate();
            yield return null;
            m_Patterns.Clear();
        }

        [UnityTest]
        public IEnumerator LightAndHeavyHits_EachPlayOneCombinedFeedback_TheHeavyHarder()
        {
            yield return Setup("giant_spider", new Vector2(1.2f, 0f));
            var health = Enemy.GetComponent<Health>();
            var shake = Object.FindAnyObjectByType<ScreenShakeListener>();

            float before = health.CurrentHealth;
            yield return Click();
            yield return WaitUntil(() => health.CurrentHealth < before, 1f, "a light hit to land");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Played(HapticIds.TapFirm), Is.EqualTo(1), "one Tap.Firm per light hit: the enemy's side no longer plays its own");
            Assert.That(Played(HapticIds.HitHeavy), Is.Zero);
            float light = shake.LastForce;
            Assert.That(light, Is.GreaterThan(0f), "a light hit shakes");

            yield return new WaitForSeconds(0.6f);
            health.SetHealth(health.MaximumHealth);
            Teleport(Enemy, k_Arena + new Vector2(1.1f, 0f));
            m_Patterns.Clear();
            before = health.CurrentHealth;
            InputSystem.QueueStateEvent(Pointer, new MouseState().WithButton(MouseButton.Right));
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(Pointer, new MouseState());
            yield return WaitUntil(() => health.CurrentHealth < before, 1f, "a heavy hit to land");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(before - health.CurrentHealth, Is.EqualTo(Player.GetComponentInChildren<HeavyWeaponTuning>().Definition.heavy[0].attack.damage).Within(0.01f),
                "the spin hits the spider once, not every frame its hitbox is open");
            Assert.That(Played(HapticIds.HitHeavy), Is.EqualTo(1), $"a heavy hit plays Hit.Heavy once (patterns {string.Join(",", m_Patterns)})");
            Assert.That(Played(HapticIds.TapFirm), Is.Zero, "and not the light pattern");
            Assert.That(Played(HapticIds.HitTaken), Is.Zero, "the frozen spider stays frozen after its stagger, so nothing hit the player");
            Assert.That(shake.LastForce, Is.GreaterThan(light), "and shakes harder");
        }

        [UnityTest]
        public IEnumerator ChargingTheHeavy_TicksAtEachStrongerLevel()
        {
            // The spider well out of reach: only the charge is felt.
            yield return Setup("giant_spider", new Vector2(6f, 0f));
            int levels = Player.GetComponentInChildren<HeavyWeaponTuning>().Definition.heavy.Count;
            InputSystem.QueueStateEvent(Pointer, new MouseState().WithButton(MouseButton.Right));
            yield return new WaitForSeconds(1.2f);
            InputSystem.QueueStateEvent(Pointer, new MouseState());
            yield return new WaitForSeconds(0.3f);
            Assert.That(Played(HapticIds.CueThreshold), Is.EqualTo(levels - 1), "one tick per level past the first");
        }

        [UnityTest]
        public IEnumerator ACleanKillRings_AnOverkillThuds()
        {
            yield return Setup("bat", new Vector2(4f, 0f));
            var health = Enemy.GetComponent<Health>();
            health.Damage(health.CurrentHealth, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            Assert.That(Played(HapticIds.KillClean), Is.EqualTo(1), "a clean kill plays Kill.Clean");
            Assert.That(Played(HapticIds.BumpSoft), Is.Zero);

            Enemy = null;
            yield return Setup("giant_spider", new Vector2(4f, 0f));
            health = Enemy.GetComponent<Health>();
            health.Damage(health.CurrentHealth + health.MaximumHealth, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            Assert.That(Played(HapticIds.BumpSoft), Is.EqualTo(1), "an overkill thuds");
            Assert.That(Played(HapticIds.KillClean), Is.Zero);
        }

        [UnityTest]
        public IEnumerator AFullSatchel_BuzzesOnce_WhenItFirstRefusesAPart()
        {
            yield return Setup("bat", new Vector2(8f, 0f));
            IngredientDefinition wing = Enemy.Definition.harvest[0].ingredient;
            Satchel.Clear();
            foreach (Quality quality in new[] { Quality.Poor, Quality.Standard, Quality.Fine, Quality.Premium })
                Satchel.Add(new IngredientItem(wing, quality), Satchel.MaxStack, 0.9f);
            Satchel.Add(new IngredientItem(wing, Quality.Standard, PrepState.Seared), Satchel.MaxStack, 0.9f);
            Satchel.Add(new IngredientItem(wing, Quality.Fine, PrepState.Seared), Satchel.MaxStack, 0.9f);
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(wing, Quality.Fine, PrepState.Chilled), 1, 1f), k_Arena + new Vector2(2f, 0f), null);

            Teleport(Player, pickup.transform.position);
            yield return WaitUntil(() => Carrier.Blocked == pickup, 1f, "the full satchel to block the pickup");
            yield return new WaitForSeconds(0.5f);
            Assert.That(Played(HapticIds.BuzzFailure), Is.EqualTo(1), "one buzz, not one per frame stood on it");
        }

        [UnityTest]
        public IEnumerator ATelegraph_IsFeltOnlyWhenAimedAtThePlayer()
        {
            yield return Setup("green_slime", new Vector2(2f, 0f));
            EnemyAttack leap = Enemy.GetComponents<EnemyAttack>().First(a => a.Settings.kind == EnemyAttackKind.Leap);

            var decoy = new GameObject("Decoy").transform;
            decoy.position = k_Arena + new Vector2(2f, 2f);
            Assert.That(leap.Begin(decoy), "the slime leaps at something else");
            yield return null;
            Assert.That(Played(HapticIds.CueThreshold), Is.Zero, "not felt when the attack isn't for the player");
            yield return WaitUntil(() => leap.Cycle.Phase == EnemyAttackPhase.Ready, 6f, "the leap to finish");

            Teleport(Enemy, k_Arena + new Vector2(2f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.That(leap.Begin(Player.transform), "the slime leaps at the player");
            yield return null;
            Assert.That(Played(HapticIds.CueThreshold), Is.EqualTo(1), "felt when aimed at the player");
        }

        [UnityTest]
        public IEnumerator TheDodge_HasASoundButNoHaptic()
        {
            yield return Setup("giant_spider", new Vector2(8f, 0f));
            var dash = Player.GetComponent<CharacterDash2D>();
            Assert.That(dash.AbilityStartFeedbacks, Is.Not.Null, "the dodge has a feedback");
            Hold(Key.W, Key.Space);
            yield return WaitUntil(() => Essence.Invulnerable, 1f, "the dodge");
            ReleaseKeys();
            yield return new WaitForSeconds(0.4f);
            Assert.That(m_Patterns, Is.Empty, "no haptic on a dodge");
        }
    }
}
