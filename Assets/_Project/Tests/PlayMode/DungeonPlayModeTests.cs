using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Shared.Ingredients;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Smoke tests that run the real MonoBehaviours through the physics loop: collision,
    /// one-way platforms, walls, a hit that kills, and Essence depletion.
    /// Requires the project layers (Hearthdelve/Setup/Configure Project Settings).
    /// </summary>
    public class DungeonPlayModeTests
    {
        readonly List<Object> m_Created = new();

        [SetUp]
        public void SetUp()
        {
            EventBusRegistry.ClearAll();
            Assert.That(LayerMask.NameToLayer(Layers.Ground), Is.GreaterThanOrEqualTo(0), "Project layers not configured.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_Created)
                if (o != null) Object.Destroy(o);
            m_Created.Clear();
            EventBusRegistry.ClearAll();
        }

        T Track<T>(T o) where T : Object
        {
            m_Created.Add(o);
            return o;
        }

        GameObject Solid(Vector2 center, Vector2 size, string layer)
        {
            var go = Track(new GameObject($"Test{layer}") { layer = LayerMask.NameToLayer(layer) });
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        PlayerController SpawnPlayer(Vector2 position, EssenceSettings? essence = null)
        {
            var movement = Track(ScriptableObject.CreateInstance<PlayerMovementConfig>());
            var weapon = Track(ScriptableObject.CreateInstance<WeaponDefinition>());
            weapon.cleanKillCategories = IngredientCategory.Meat;
            weapon.combo = new List<AttackData>
            {
                new()
                {
                    startupFrames = 2, activeFrames = 4, recoveryFrames = 6, cancelFrame = 8, damage = 50f,
                    hitboxOffset = new Vector2(0.8f, 0.7f), hitboxSize = new Vector2(1.4f, 1.2f), lungeSpeed = 0f,
                },
            };
            var essenceConfig = Track(ScriptableObject.CreateInstance<EssenceConfig>());
            if (essence.HasValue) essenceConfig.essence = essence.Value;

            var go = Track(new GameObject("TestPlayer"));
            go.SetActive(false); // configure before Awake runs
            go.layer = LayerMask.NameToLayer(Layers.Player);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.4f);
            box.offset = new Vector2(0f, 0.7f);
            go.AddComponent<KinematicMover2D>().Configure(Layers.Mask(Layers.Ground), Layers.Mask(Layers.OneWayPlatform));
            go.AddComponent<PlayerInputReader>().Override = default(PlayerFrameInput);
            var controller = go.AddComponent<PlayerController>();
            controller.Configure(movement, weapon, Layers.Mask(Layers.Enemy));
            go.AddComponent<PlayerVitals>().Configure(essenceConfig);
            go.SetActive(true);
            return controller;
        }

        static IEnumerator FixedSteps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        static void SetInput(PlayerController player, PlayerFrameInput input) =>
            player.GetComponent<PlayerInputReader>().Override = input;

        [UnityTest]
        public IEnumerator Player_FallsAndLandsOnGround()
        {
            Solid(new Vector2(0f, -0.5f), new Vector2(20f, 1f), Layers.Ground);
            var player = SpawnPlayer(new Vector2(0f, 3f));

            yield return FixedSteps(90);

            Assert.That(player.Mover.Contacts.Grounded, Is.True);
            Assert.That(player.Motor.State, Is.EqualTo(MotorState.Grounded));
            Assert.That(player.Mover.Position.y, Is.EqualTo(0f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByWall()
        {
            Solid(new Vector2(0f, -0.5f), new Vector2(20f, 1f), Layers.Ground);
            Solid(new Vector2(3.5f, 2f), new Vector2(1f, 4f), Layers.Ground);
            var player = SpawnPlayer(new Vector2(0f, 0.05f));
            SetInput(player, new PlayerFrameInput { Move = Vector2.right });

            yield return FixedSteps(90);

            // Wall face at x = 3, player half-width 0.3.
            Assert.That(player.Mover.Position.x, Is.EqualTo(2.7f).Within(0.05f));
            Assert.That(player.Mover.Contacts.WallRight, Is.True);
        }

        [UnityTest]
        public IEnumerator Player_JumpsUpThroughOneWay_LandsOnIt_ThenDropsThrough()
        {
            Solid(new Vector2(0f, -0.5f), new Vector2(20f, 1f), Layers.Ground);
            Solid(new Vector2(0f, 2.4f), new Vector2(6f, 0.2f), Layers.OneWayPlatform); // top at 2.5
            var player = SpawnPlayer(new Vector2(0f, 0.05f));
            yield return FixedSteps(10);

            SetInput(player, new PlayerFrameInput { JumpPressed = true, JumpHeld = true });
            yield return FixedSteps(1);
            SetInput(player, new PlayerFrameInput { JumpHeld = true });
            yield return FixedSteps(90);

            Assert.That(player.Mover.Position.y, Is.EqualTo(2.5f).Within(0.05f), "should pass up through and land on top");
            Assert.That(player.Mover.Contacts.OnOneWay, Is.True);

            SetInput(player, new PlayerFrameInput { Move = Vector2.down, JumpPressed = true, JumpHeld = true });
            yield return FixedSteps(1);
            SetInput(player, default);
            yield return FixedSteps(90);

            Assert.That(player.Mover.Position.y, Is.EqualTo(0f).Within(0.05f), "down + jump drops through to the floor");
        }

        [UnityTest]
        public IEnumerator Attack_KillsEnemy_PublishesKillContextForHarvest()
        {
            Solid(new Vector2(0f, -0.5f), new Vector2(20f, 1f), Layers.Ground);
            var player = SpawnPlayer(new Vector2(0f, 0.05f));

            var meat = Track(ScriptableObject.CreateInstance<IngredientDefinition>());
            meat.category = IngredientCategory.Meat;
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.maxHealth = 10f;
            def.moveSpeed = 0f;
            def.aggroRange = 0f;
            def.harvest = new List<HarvestPart> { new() { ingredient = meat } };

            var enemy = Track(new GameObject("TestEnemy"));
            enemy.SetActive(false);
            enemy.layer = LayerMask.NameToLayer(Layers.Enemy);
            enemy.transform.position = new Vector3(1f, 0.05f, 0f);
            enemy.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var box = enemy.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.6f);
            box.offset = new Vector2(0f, 0.3f);
            enemy.AddComponent<KinematicMover2D>().Configure(Layers.Mask(Layers.Ground), Layers.Mask(Layers.OneWayPlatform));
            enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<DummyBehaviour>().Configure(def, null, null, Layers.Mask(Layers.Player));
            enemy.SetActive(true);

            EnemyKilled? killed = null;
            EventBus<EnemyKilled>.Subscribe(e => killed = e);

            yield return FixedSteps(5);
            SetInput(player, new PlayerFrameInput { AttackPressed = true });
            yield return FixedSteps(1);
            SetInput(player, default);
            yield return FixedSteps(20);

            Assert.That(killed.HasValue, "the hit should kill the 10 HP enemy");
            Assert.That(killed.Value.Definition, Is.SameAs(def));
            Assert.That(killed.Value.Kill.Overkill, Is.EqualTo(40f).Within(1e-3f));
            Assert.That(killed.Value.Kill.MaxHealth, Is.EqualTo(10f));
            Assert.That(killed.Value.Kill.CleanKillCategories & IngredientCategory.Meat, Is.EqualTo(IngredientCategory.Meat));
            Assert.That(enemy == null, "the dead enemy is destroyed");
        }

        [UnityTest]
        public IEnumerator EssenceDrainToZero_PublishesPlayerDefeated()
        {
            Solid(new Vector2(0f, -0.5f), new Vector2(20f, 1f), Layers.Ground);
            bool defeated = false;
            EventBus<PlayerDefeated>.Subscribe(_ => defeated = true);

            var player = SpawnPlayer(new Vector2(0f, 0.05f),
                new EssenceSettings { baseMax = 1f, drainPerSecond = 10f, damageMultiplier = 1f, lowThreshold = 0.25f });

            yield return new WaitForSeconds(0.3f);

            Assert.That(defeated, Is.True);
            Assert.That(player.GetComponent<PlayerVitals>().IsDefeated, Is.True);
        }
    }
}
