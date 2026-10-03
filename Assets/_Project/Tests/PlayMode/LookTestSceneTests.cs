using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Debugging;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.World;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>Records what the haptic service sends, standing in for a controller.</summary>
    public sealed class RecordingHapticOutput : IHapticOutput
    {
        public bool IsAvailable { get; set; } = true;
        public float PeakLow { get; private set; }
        public float PeakHigh { get; private set; }
        public int Sets { get; private set; }
        public int Stops { get; private set; }

        public void SetMotors(float low, float high)
        {
            Sets++;
            PeakLow = Mathf.Max(PeakLow, low);
            PeakHigh = Mathf.Max(PeakHigh, high);
        }

        public void Stop() => Stops++;
    }

    /// <summary>
    /// Shared set-up for tests that play the 4a look-test scenes: loads a scene, waits for
    /// TDE to spawn the player, and feeds input through virtual devices.
    /// </summary>
    public abstract class LookTestFixture
    {
        protected const string DungeonScene = "LookTest_Dungeon";
        protected const string TavernScene = "LookTest_Tavern";

        protected Keyboard Keys;
        protected Mouse Pointer;
        protected Character Player;
        protected RecordingHapticOutput Haptics;

        InputSettings.BackgroundBehavior m_Background;
        InputSettings.EditorInputBehaviorInPlayMode m_EditorBehavior;

        [SetUp]
        public void SetUpInput()
        {
            EventBusRegistry.ClearAll();
            // Tests run without window focus, so input must not depend on it.
            InputSettings settings = InputSystem.settings;
            m_Background = settings.backgroundBehavior;
            m_EditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Keys = InputSystem.AddDevice<Keyboard>();
            Pointer = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void TearDownInput()
        {
            InputSystem.RemoveDevice(Keys);
            InputSystem.RemoveDevice(Pointer);
            InputSettings settings = InputSystem.settings;
            settings.backgroundBehavior = m_Background;
            settings.editorInputBehaviorInPlayMode = m_EditorBehavior;
            GameSettings.VibrationEnabled = true;
            GameSettings.ReducedVibration = false;
            Time.timeScale = 1f;
            EventBusRegistry.ClearAll();
        }

        protected IEnumerator Load(string scene)
        {
            Assert.That(Application.CanStreamedLevelBeLoaded(scene), $"{scene} is not in the build list. Run Hearthdelve > Generate > 4a Look Test (All).");
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0)
                   && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(LevelManager.Instance.Players, Is.Not.Empty, "TDE did not spawn the player");
            Player = LevelManager.Instance.Players[0];
            Haptics = new RecordingHapticOutput();
            HapticService.Instance.Output = Haptics;
            yield return null;
            yield return null;
        }

        protected void Hold(params Key[] keys) => InputSystem.QueueStateEvent(Keys, new KeyboardState(keys));
        protected void ReleaseKeys() => InputSystem.QueueStateEvent(Keys, new KeyboardState());

        protected IEnumerator Click()
        {
            InputSystem.QueueStateEvent(Pointer, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState());
            yield return null;
        }

        protected static IEnumerator WaitUntil(Func<bool> condition, float seconds, string what)
        {
            float deadline = Time.time + seconds;
            while (!condition() && Time.time < deadline) yield return null;
            Assert.That(condition(), "Timed out waiting for: " + what);
        }

        protected void Teleport(Component character, Vector2 position)
        {
            character.transform.position = position;
            var body = character.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
        }

        protected static void FreezeEnemies()
        {
            foreach (AIBrain brain in Object.FindObjectsByType<AIBrain>()) brain.BrainActive = false;
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
                enemy.GetComponent<CharacterMovement>().SetMovement(Vector2.zero);
        }
    }

    public class LookTestDungeonTests : LookTestFixture
    {
        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();

        [UnityTest]
        public IEnumerator Scene_UsesTheLockedTopDownSetup()
        {
            yield return Load(DungeonScene);

            Assert.That(Physics2D.gravity, Is.EqualTo(Vector2.zero), "top-down: no gravity");
            Assert.That(GraphicsSettings.transparencySortMode, Is.EqualTo(TransparencySortMode.CustomAxis));
            Assert.That(GraphicsSettings.transparencySortAxis, Is.EqualTo(Vector3.up), "Y-sorting axis");

            var pixelPerfect = Camera.main.GetComponent<PixelPerfectCamera>();
            Assert.That(pixelPerfect.assetsPPU, Is.EqualTo(8));
            Assert.That(new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY), Is.EqualTo(new Vector2Int(320, 180)));
            Assert.That(pixelPerfect.gridSnapping, Is.EqualTo(PixelPerfectCamera.GridSnapping.None), "smooth scrolling: no snapping to the art-pixel grid");

            Assert.That(InputManager.Instance, Is.InstanceOf<HearthdelveInputManager>(), "never the legacy InputManager");
            Assert.That(GameManager.Instance.TargetFrameRate, Is.EqualTo(-1), "platform frame pacing (requestAnimationFrame on the web)");
            Assert.That(InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move).enabled);
            Assert.That(InputSystem.actions.FindActionMap(InputMaps.Tavern).enabled, Is.False, "exactly one gameplay map is active");

            Assert.That(Player.CharacterType, Is.EqualTo(Character.CharacterTypes.Player));
            Assert.That(Player.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer(Layers.Player)));
            Assert.That(Player.GetComponent<SortingGroup>().sortingLayerName, Is.EqualTo(SortingLayers.YSorted));
            Assert.That(Player.GetComponentInChildren<SpriteRenderer>().sprite, Is.Not.Null, "real Minifantasy art");
            Assert.That(Player.GetComponent<SatchelCarrier>().Satchel.Capacity, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator Essence_IsThePlayersOnlyHealth_AndDrainsOverTime()
        {
            var changes = new List<EssenceChanged>();
            EventBus<EssenceChanged>.Subscribe(changes.Add);
            yield return Load(DungeonScene);
            FreezeEnemies();

            Health[] pools = Player.GetComponentsInChildren<Health>();
            Assert.That(pools.Length, Is.EqualTo(1), "no second health pool");
            Assert.That(pools[0], Is.SameAs(Essence));
            Assert.That(Player.CharacterHealth, Is.SameAs(Essence));
            Assert.That(Essence.MaximumHealth, Is.EqualTo(Essence.Essence.Max));

            float before = Essence.CurrentHealth;
            yield return new WaitForSeconds(0.5f);
            Assert.That(Essence.CurrentHealth, Is.LessThan(before), "time drain");
            Assert.That(Essence.CurrentHealth, Is.EqualTo(Essence.Essence.Current), "TDE health mirrors the meter");
            Assert.That(changes, Is.Not.Empty);
            Assert.That(Object.FindAnyObjectByType<EssenceBar>().Shown, Is.EqualTo(Essence.Essence.Normalized).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Keyboard_MovesThePlayer_InEightDirections()
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            Teleport(Player, new Vector2(-3f, -2f));
            yield return new WaitForFixedUpdate();

            Vector2 start = Player.transform.position;
            Hold(Key.D, Key.W);
            yield return new WaitForSeconds(0.4f);
            ReleaseKeys();
            yield return null;
            Vector2 moved = (Vector2)Player.transform.position - start;
            Assert.That(moved.x, Is.GreaterThan(0.5f), "right");
            Assert.That(moved.y, Is.GreaterThan(0.5f), "up, at the same time");

            yield return new WaitForSeconds(0.3f);
            Vector2 rest = Player.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector2.Distance(rest, Player.transform.position), Is.LessThan(0.05f), "stops when the keys are released");
        }

        [UnityTest]
        public IEnumerator Walls_BlockThePlayer()
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            Teleport(Player, new Vector2(-9f, -2f));
            Hold(Key.A);
            yield return new WaitForSeconds(1.2f);
            ReleaseKeys();
            Assert.That(Player.transform.position.x, Is.GreaterThan(-11.05f), "the west wall is at x = -11");
        }

        [UnityTest]
        public IEnumerator Dodge_Rolls_WithInvulnerability()
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            Teleport(Player, new Vector2(-3f, -2f));
            yield return new WaitForFixedUpdate();
            Vector2 start = Player.transform.position;

            Hold(Key.D);
            yield return null;
            yield return null;
            Hold(Key.D, Key.Space);
            yield return WaitUntil(() => Player.MovementState.CurrentState == CharacterStates.MovementStates.Dashing, 0.5f, "the dodge roll to start");
            Assert.That(Essence.Invulnerable, "i-frames while rolling");
            float essence = Essence.CurrentHealth;
            Essence.Damage(20f, null, 0f, 0f, Vector3.zero);
            Assert.That(Essence.CurrentHealth, Is.EqualTo(essence).Within(0.5f), "a hit during the roll does no damage");

            Hold(Key.D);
            yield return WaitUntil(() => Player.MovementState.CurrentState != CharacterStates.MovementStates.Dashing, 1f, "the dodge roll to end");
            ReleaseKeys();
            yield return null;
            Assert.That(Essence.Invulnerable, Is.False);
            Assert.That(Player.transform.position.x - start.x, Is.GreaterThan(2f), "the roll covers ground");
        }

        /// <summary>
        /// The sprite animator is presentation only (CLAUDE.md): with every one switched off,
        /// the dodge, its i-frames and taking damage behave exactly the same.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_DoesNotDependOnTheSpriteAnimator()
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            foreach (var animator in Object.FindObjectsByType<Hearthdelve.Shared.Animation.CharacterSpriteAnimator>())
                animator.enabled = false;
            Teleport(Player, new Vector2(-3f, -2f));
            yield return new WaitForFixedUpdate();

            Hold(Key.D);
            yield return null;
            yield return null;
            Hold(Key.D, Key.Space);
            yield return WaitUntil(() => Player.MovementState.CurrentState == CharacterStates.MovementStates.Dashing, 0.5f, "the dodge roll to start");
            Assert.That(Essence.Invulnerable, "i-frames come from TDE's dash, not the animation");
            Hold(Key.D);
            yield return WaitUntil(() => Player.MovementState.CurrentState != CharacterStates.MovementStates.Dashing, 1f, "the dodge roll to end");
            ReleaseKeys();
            yield return new WaitForSeconds(0.1f);

            float before = Essence.CurrentHealth;
            Essence.Damage(10f, null, 0f, 0f, Vector3.zero);
            Assert.That(before - Essence.CurrentHealth, Is.GreaterThanOrEqualTo(9.99f), "damage applies without an animator");
        }

        [UnityTest]
        public IEnumerator EnemyContact_DrainsEssence_WithOneCombinedFeedback()
        {
            var damaged = new List<CharacterDamaged>();
            EventBus<CharacterDamaged>.Subscribe(damaged.Add);
            yield return Load(DungeonScene);
            FreezeEnemies();
            var shake = Object.FindAnyObjectByType<ScreenShakeListener>();

            float before = Essence.CurrentHealth;
            EnemyIdentity slime = Object.FindAnyObjectByType<EnemyIdentity>();
            Teleport(slime, Player.transform.position);
            yield return WaitUntil(() => damaged.Any(d => d.TargetIsPlayer), 1.5f, "the slime to hurt the player");

            CharacterDamaged hit = damaged.First(d => d.TargetIsPlayer);
            Assert.That(hit.Target, Is.SameAs(Player.gameObject));
            Assert.That(before - Essence.CurrentHealth, Is.GreaterThanOrEqualTo(slime.Definition.attack.damage * 0.99f), "damage comes out of Essence");
            Assert.That(Essence.Invulnerable, "post-hit invulnerability");

            yield return null;
            yield return null;
            Assert.That(Haptics.PeakLow, Is.GreaterThan(0.5f), "the Hit.Taken haptic played");
            Assert.That(shake.LastForce, Is.GreaterThan(0f), "screen shake, in the same feedback");
        }

        [UnityTest]
        public IEnumerator Combo_KillsTheSlime_WhichDropsAHarvest_ThePlayerPicksUp()
        {
            var died = new List<CharacterDied>();
            var killed = new List<EnemyKilled>();
            var harvest = new List<HarvestFeedback>();
            EventBus<CharacterDied>.Subscribe(died.Add);
            EventBus<EnemyKilled>.Subscribe(killed.Add);
            EventBus<HarvestFeedback>.Subscribe(harvest.Add);
            yield return Load(DungeonScene);
            FreezeEnemies();
            Essence.GodMode = true;
            Object.FindAnyObjectByType<HarvestSystem>().Scatter = 0f; // drops land where the slime died, out of reach

            // Aim right, at a slime standing one tile away. (Aim normally follows the mouse or the stick.)
            Player.GetComponent<AimControlSwitcher>().enabled = false;
            var handle = Player.GetComponent<CharacterHandleWeapon>();
            handle.WeaponAimComponent.AimControl = WeaponAim.AimControls.Script;
            handle.WeaponAimComponent.SetCurrentAim(Vector3.right);
            Teleport(Player, new Vector2(-3f, -2f));
            EnemyIdentity[] slimes = Object.FindObjectsByType<EnemyIdentity>();
            EnemyIdentity target = slimes[0];
            foreach (EnemyIdentity other in slimes.Skip(1)) Teleport(other, new Vector2(8f, 3f));
            Teleport(target, new Vector2(-1.5f, -2f));
            foreach (DamageOnTouch touch in target.GetComponentsInChildren<DamageOnTouch>()) touch.enabled = false;
            yield return new WaitForFixedUpdate();

            var targetHealth = target.GetComponent<Health>();
            Assert.That(targetHealth.MaximumHealth, Is.EqualTo(target.Definition.maxHealth), "stats come from the definition");
            var weapons = new HashSet<Weapon>();
            for (int swing = 0; swing < 30 && died.Count == 0; swing++)
            {
                // Press again as soon as the last attack allows, the way a player strings a combo.
                yield return Click();
                float until = Time.time + 0.14f;
                while (Time.time < until)
                {
                    weapons.Add(handle.CurrentWeapon);
                    yield return null;
                }
                Teleport(target, new Vector2(-1.5f, -2f)); // undo knockback so every swing connects
            }

            Assert.That(died.Count, Is.EqualTo(1), "the slime died");
            Assert.That(died[0].IsPlayer, Is.False);
            Assert.That(weapons.Count, Is.GreaterThan(1), "the combo moved on to its next attack");
            Assert.That(killed.Count, Is.EqualTo(1));
            Assert.That(killed[0].Definition, Is.SameAs(target.Definition));
            Assert.That(Haptics.PeakLow, Is.GreaterThan(0.3f), "the hit feedback's haptic played");
            Assert.That(harvest, Is.Not.Empty, "the harvest rules ran");

            IngredientPickup[] drops = Object.FindObjectsByType<IngredientPickup>();
            Assert.That(drops, Is.Not.Empty, "a harvest drop spawned");
            Assert.That(drops[0].Item.IsValid);
            Assert.That(drops[0].GetComponentInChildren<SpriteRenderer>().sprite, Is.Not.Null, "with its ingredient icon");

            var satchel = Player.GetComponent<SatchelCarrier>().Satchel;
            Assert.That(satchel.IsEmpty);
            Teleport(Player, drops[0].transform.position);
            yield return WaitUntil(() => !satchel.IsEmpty, 1f, "the pickup to go into the satchel");
            Assert.That(satchel.Slots[0].Item.Definition, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator EssenceAtZero_KillsThePlayer_Once()
        {
            var defeats = new List<PlayerDefeated>();
            var died = new List<CharacterDied>();
            EventBus<PlayerDefeated>.Subscribe(defeats.Add);
            EventBus<CharacterDied>.Subscribe(died.Add);
            yield return Load(DungeonScene);
            FreezeEnemies();

            Essence.Damage(Essence.MaximumHealth * 3f, null, 0f, 0f, Vector3.zero);
            yield return null;

            Assert.That(Essence.CurrentHealth, Is.EqualTo(0f));
            Assert.That(Essence.Essence.IsDepleted);
            Assert.That(Player.ConditionState.CurrentState, Is.EqualTo(CharacterStates.CharacterConditions.Dead));
            Assert.That(defeats.Count, Is.EqualTo(1));
            Assert.That(defeats[0].Reason, Is.EqualTo(DefeatReason.EssenceDepleted));
            Assert.That(died.Count(d => d.IsPlayer), Is.EqualTo(1));

            Essence.Damage(50f, null, 0f, 0f, Vector3.zero);
            yield return new WaitForSeconds(0.3f);
            Assert.That(defeats.Count, Is.EqualTo(1), "defeat is reported exactly once");
        }

        [UnityTest]
        public IEnumerator Haptics_RespectSettings_AndDoNothingWithoutAController()
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            HapticPattern tap = HapticService.Instance.Library.Find(HapticIds.TapFirm);
            Assert.That(tap, Is.Not.Null);

            // Vibration off: nothing reaches the controller.
            GameSettings.VibrationEnabled = false;
            HapticService.Play(HapticIds.TapFirm);
            yield return null;
            yield return null;
            Assert.That(Haptics.Sets, Is.EqualTo(0));

            // Reduced intensity caps the output.
            GameSettings.VibrationEnabled = true;
            GameSettings.ReducedVibration = true;
            HapticService.Play(HapticIds.HitHeavy);
            yield return null;
            yield return null;
            Assert.That(Haptics.Sets, Is.GreaterThan(0));
            Assert.That(Haptics.PeakLow, Is.LessThanOrEqualTo(GameSettings.ReducedVibrationCap + 1e-4f));
            GameSettings.ReducedVibration = false;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Haptics.Stops, Is.GreaterThan(0), "motors are switched off when the pattern ends");

            // No controller (and web builds): patterns still run their course, nothing is sent, nothing breaks.
            var none = new RecordingHapticOutput { IsAvailable = false };
            HapticService.Instance.Output = none;
            HapticService.Play(HapticIds.HitHeavy);
            HapticService.Play("No.Such.Pattern");
            HapticService.SetContinuous("pour", 0.5f, 0.5f);
            yield return null;
            yield return null;
            Assert.That(none.Sets, Is.EqualTo(0));
            HapticService.Instance.Output = new NullHapticOutput();
            yield return null;
            HapticService.StopAll();
            yield return null;
            Assert.That(HapticService.Instance.LastSample.IsSilent);
        }
    }

    public class LookTestTavernTests : LookTestFixture
    {
        [UnityTest]
        public IEnumerator Tavern_UsesTheSameCharacter_WithoutCombat()
        {
            yield return Load(TavernScene);

            Assert.That(InputSystem.actions.FindActionMap(InputMaps.Tavern).enabled);
            Assert.That(InputSystem.actions.FindActionMap(InputMaps.Dungeon).enabled, Is.False);
            Assert.That(Player.GetComponent<CharacterHandleWeapon>(), Is.Null, "attacks are disabled in the tavern");
            Assert.That(Player.GetComponent<EssenceHealth>(), Is.Null, "Essence only exists in the dungeon");
            Assert.That(Player.GetComponent<TopDownController2D>(), Is.Not.Null, "a TDE character, so combat could be added later");

            Vector2 start = Player.transform.position;
            Hold(Key.A);
            yield return new WaitForSeconds(0.4f);
            ReleaseKeys();
            yield return null;
            Assert.That(start.x - Player.transform.position.x, Is.GreaterThan(0.5f), "walks with the Tavern map");
        }

        /// <summary>The player's feet after walking into a piece of furniture from below, starting two tiles in front of it.</summary>
        IEnumerator WalkUpInto(Transform piece, float xOffset)
        {
            Bounds art = piece.GetComponent<SpriteRenderer>().bounds;
            Teleport(Player, new Vector2(art.center.x + xOffset, art.min.y - 2f));
            yield return new WaitForFixedUpdate();
            Hold(Key.W);
            yield return new WaitForSeconds(1.5f);
            ReleaseKeys();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        void AssertBlockedInFront(Transform piece)
        {
            var body = Player.GetComponent<BoxCollider2D>();
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(body.bounds.center, body.bounds.size * 0.95f, 0f, LayerMask.GetMask(Layers.Obstacles));
            Assert.That(overlaps.Select(c => c.name), Is.Empty, "the player's body is inside solid furniture");

            float feet = Player.transform.position.y;
            Assert.That(feet, Is.LessThan(piece.position.y), $"the player got past the front of {piece.name}, so it sorts behind it");
            Assert.That(piece.position.y - feet, Is.LessThan(0.75f), $"the player should walk right up to {piece.name}");
        }

        [UnityTest]
        public IEnumerator Bar_BlocksThePlayer_ApproachingFromBelow()
        {
            yield return Load(TavernScene);
            Transform bar = GameObject.Find("Furniture/Bar").transform;
            foreach (float x in new[] { -2f, 0f, 2.5f })
            {
                yield return WalkUpInto(bar, x);
                AssertBlockedInFront(bar);
            }
        }

        [UnityTest]
        public IEnumerator Tables_BlockThePlayer_ApproachingFromBelow()
        {
            yield return Load(TavernScene);
            foreach (string name in new[] { "Furniture/TableSetA", "Furniture/TableSetB" })
            {
                Transform table = GameObject.Find(name).transform;
                yield return WalkUpInto(table, 0f);
                AssertBlockedInFront(table);
            }
        }

        /// <summary>
        /// The Y-sort contract for solid furniture: what blocks movement ends exactly at the
        /// sort point (the bottom of the art), so a character stopped in front of a piece always
        /// draws in front of it and one behind it always draws behind.
        /// </summary>
        [UnityTest]
        public IEnumerator SolidFurniture_FootprintEndsAtItsSortPoint()
        {
            yield return Load(TavernScene);
            Transform furniture = GameObject.Find("Furniture").transform;
            Assert.That(furniture.childCount, Is.GreaterThan(0));
            foreach (Transform piece in furniture)
            {
                Collider2D[] solids = piece.GetComponentsInChildren<Collider2D>().Where(c => !c.isTrigger).ToArray();
                Assert.That(solids, Is.Not.Empty, $"{piece.name} has no collision");
                Assert.That(solids.Min(c => c.bounds.min.y), Is.EqualTo(piece.position.y).Within(0.02f), $"{piece.name}: footprint and sort point differ");
                Assert.That(piece.GetComponent<SpriteRenderer>().spriteSortPoint, Is.EqualTo(SpriteSortPoint.Pivot));
                Assert.That(piece.GetComponent<SpriteRenderer>().sprite.pivot.y, Is.EqualTo(0f), $"{piece.name}: the pivot is the bottom of the art");
            }
        }

        [UnityTest]
        public IEnumerator SpeechBubble_ShowsLocalizedText_WhenThePlayerIsNear()
        {
            yield return Load(TavernScene);
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables to preload");

            var bubble = Object.FindAnyObjectByType<SpeechBubble>();
            Assert.That(bubble, Is.Not.Null);
            // The cook stands behind the bar; from the door the player is out of earshot.
            Transform cook = GameObject.Find("Cook").transform;
            Teleport(Player, (Vector2)cook.position + new Vector2(8f, -8f));
            yield return null;
            yield return null;
            Assert.That(bubble.IsVisible, Is.False);

            Teleport(Player, (Vector2)cook.position + new Vector2(0f, -2.5f));
            yield return null;
            yield return null;
            Assert.That(bubble.IsVisible);

            var text = bubble.GetComponentInChildren<SuperTextMesh>();
            Assert.That(text.text, Is.EqualTo(Loc.UI(LocKeys.LookTestGreeting)));
            Assert.That(text.text, Does.Not.StartWith("#"), "a real string from the table, not a missing key");
            Assert.That(text.text, Is.Not.Empty);
        }

        /// <summary>
        /// The visual baseline is URP 2D lit sprites (CLAUDE.md): everything drawn in the world uses
        /// the lit sprite shader, and each environment has an ambient light plus a local light
        /// that reach every sorting layer.
        /// </summary>
        [UnityTest]
        public IEnumerator BothScenes_UseLitSprites_AndRepresentativeLighting()
        {
            const string litShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
            foreach (string scene in new[] { DungeonScene, TavernScene })
            {
                yield return Load(scene);

                Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)
                    .Where(r => r is SpriteRenderer || r is UnityEngine.Tilemaps.TilemapRenderer).ToArray();
                Assert.That(renderers, Is.Not.Empty, scene);
                foreach (Renderer renderer in renderers)
                    Assert.That(renderer.sharedMaterial != null ? renderer.sharedMaterial.shader.name : "none", Is.EqualTo(litShader), $"{scene}/{renderer.name}");

                Light2D[] lights = Object.FindObjectsByType<Light2D>();
                Assert.That(lights.Count(l => l.lightType == Light2D.LightType.Global), Is.EqualTo(1), $"{scene}: one ambient light");
                Assert.That(lights.Count(l => l.lightType == Light2D.LightType.Point), Is.GreaterThanOrEqualTo(1), $"{scene}: a local light");
                int[] allLayers = SortingLayer.layers.Select(l => l.id).ToArray();
                foreach (Light2D light in lights)
                    Assert.That(light.targetSortingLayers, Is.EquivalentTo(allLayers), $"{scene}/{light.name} must light every sorting layer");
            }
        }

        [UnityTest]
        public IEnumerator Localization_Preloads_SoNoLookupBlocks()
        {
            yield return Load(TavernScene);
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables to preload");

            foreach (LocalizedSuperText label in Object.FindObjectsByType<LocalizedSuperText>(FindObjectsInactive.Include))
            {
                string shown = label.GetComponent<SuperTextMesh>().text;
                Assert.That(shown, Is.Not.Empty, label.name);
                Assert.That(shown, Does.Not.StartWith("#"), $"{label.name}: missing key '{label.Key}'");
            }
        }

        [UnityTest]
        public IEnumerator LookTestOverlay_CyclesTheReferenceResolution()
        {
            yield return Load(TavernScene);
            var overlay = Object.FindAnyObjectByType<LookTestOverlay>();
            var pixelPerfect = Camera.main.GetComponent<PixelPerfectCamera>();
            Assert.That(overlay.Resolution, Is.EqualTo(new Vector2Int(320, 180)));

            overlay.Cycle();
            Assert.That(new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY), Is.EqualTo(new Vector2Int(240, 135)));
            overlay.Cycle();
            Assert.That(new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY), Is.EqualTo(new Vector2Int(160, 90)));
            overlay.Cycle();
            Assert.That(new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY), Is.EqualTo(new Vector2Int(320, 180)));
        }
    }
}
