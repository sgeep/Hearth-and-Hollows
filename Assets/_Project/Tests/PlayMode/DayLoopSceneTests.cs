using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// The Phase 3 loop end to end through the real scenes: Boot → main menu → new game → morning
    /// → delve → exit → evening service → night upgrade → save → reload → the upgrade and stock
    /// persist. Saves go to a temp folder. Any logged error fails the test.
    /// </summary>
    public class DayLoopSceneTests
    {
        string m_SaveDir;

        [SetUp]
        public void SetUp()
        {
            m_SaveDir = Path.Combine(Application.temporaryCachePath, "hearthdelve_test_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        /// <summary>
        /// Leaves an empty scene behind: the loop keeps Boot plus a content scene loaded
        /// additively, and later tests that build their own geometry must not share it.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.CreateScene("DayLoopTestsEmpty_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            GameFlow.SaveDirectoryOverride = null;
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator Boot()
        {
            var op = SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return WaitForScene(GameScenes.MainMenu);
        }

        /// <summary>Waits until the flow has finished loading <paramref name="scene"/> (and a frame for Start).</summary>
        static IEnumerator WaitForScene(string scene, float seconds = 15f)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while ((GameFlow.Instance == null || GameFlow.Instance.IsLoading || GameFlow.Instance.LoadedScene != scene) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return null;
            Assert.That(GameFlow.Instance.LoadedScene, Is.EqualTo(scene), $"{scene} should be loaded");
        }

        static bool Visible<T>() where T : Component =>
            Object.FindFirstObjectByType<T>().GetComponent<UIDocument>().rootVisualElement.Q("screen").style.display.value == DisplayStyle.Flex;

        static System.Action UseInputWithoutFocus()
        {
            var original = InputSystem.settings;
            var settings = Object.Instantiate(original);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            return () =>
            {
                InputSystem.settings = original;
                Object.Destroy(settings);
            };
        }

        [UnityTest]
        public IEnumerator FullDay_Delve_Extract_Serve_Upgrade_Save_Reload()
        {
            yield return Boot();
            var flow = GameFlow.Instance;
            Assert.That(flow.HasSave, Is.False);

            // ---------- New game → Morning ----------
            flow.NewGame();
            yield return WaitForScene(GameScenes.Tavern);
            var director = TavernDirector.Instance;
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Morning));
            Assert.That(flow.State.Day, Is.EqualTo(1));
            Assert.That(Visible<TavernMorningScreen>());
            Assert.That(director.CanDebugFill, Is.False, "the debug fill is off by default in the day loop");

            // ---------- Delve ----------
            director.Descend();
            yield return WaitForScene(GameScenes.Dungeon);
            var run = DelveRunController.Active;
            Assert.That(run.Satchel.Capacity, Is.EqualTo(6), "no satchel upgrade yet");
            var haunch = flow.Database.Ingredient("rat_haunch");
            var gel = flow.Database.Ingredient("slime_gel");
            run.Satchel.Add(new IngredientItem(haunch, Quality.Standard), 3);
            run.Satchel.Add(new IngredientItem(gel, Quality.Standard), 3);

            // Walk up to the exit (teleport) and press Interact.
            var exit = Object.FindFirstObjectByType<DelveExit>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            player.GetComponent<KinematicMover2D>().Teleport(exit.transform.position + new Vector3(0f, 0.05f, 0f));
            var restoreInput = UseInputWithoutFocus();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return null;
                yield return null;
                Assert.That(exit.PlayerInReach, "standing at the exit");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                restoreInput();
            }

            // ---------- Evening service ----------
            yield return WaitForScene(GameScenes.Tavern);
            director = TavernDirector.Instance;
            Assert.That(flow.Phase, Is.EqualTo(DayPhase.Evening));
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Prep));
            Assert.That(director.Storeroom, Is.SameAs(flow.State.Storeroom), "the tavern cooks from the persistent storeroom");
            Assert.That(director.Storeroom.TotalCount, Is.EqualTo(6), "the whole satchel came home");
            Assert.That(director.Storeroom.Stacks.All(s => s.Freshness < 1f), "parts aged in the dungeon");
            Assert.That(flow.State.Today.Delve, Is.EqualTo(DelveOutcome.Extracted));
            Assert.That(director.ActiveSeats, Is.EqualTo(6));
            Assert.That(director.Layout.transform.Find("Stool 7").GetComponent<SpriteRenderer>().enabled, Is.False, "unbought seats are hidden");

            var grilled = director.Content.recipes.First(r => r.id == "grilled_haunch");
            var gelbrew = director.Content.recipes.First(r => r.id == "gelbrew");
            director.ToggleMenu(grilled);
            director.ToggleMenu(gelbrew);
            Assert.That(director.StaffAssignment, Is.EqualTo(StaffStation.Serving));
            director.OpenService();
            var staff = director.StaffMember;
            var grillCook = new StaffCook(director.Session, director.Minigames, CookStation.Grill, staff.skill, staff.qualityCap, 0.2f, new SeededRandom(5));
            var tapCook = new StaffCook(director.Session, director.Minigames, CookStation.Tap, staff.skill, staff.qualityCap, 0.2f, new SeededRandom(6));
            Time.timeScale = 6f;
            for (int i = 0; i < 3; i++) director.SpawnCustomer(director.Content.customers[i % director.Content.customers.Count]);
            float timeout = Time.realtimeSinceStartup + 40f;
            while (director.Session.Ledger.DishesServed < 2 && director.Phase == TavernPhase.Service && Time.realtimeSinceStartup < timeout)
            {
                grillCook.Tick(Time.deltaTime);
                tapCook.Tick(Time.deltaTime);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.That(director.Session.Ledger.DishesServed, Is.GreaterThanOrEqualTo(2));
            director.EndServiceNow();
            yield return null;
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Results));

            // ---------- Night: bank, upgrade, save ----------
            director.FinishEvening();
            yield return null;
            Assert.That(flow.Phase, Is.EqualTo(DayPhase.Night));
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Night));
            Assert.That(Visible<TavernNightScreen>());
            Assert.That(flow.State.Gold, Is.GreaterThan(0), "service gold is banked");
            Assert.That(flow.HasSave, "autosaved at Night");

            flow.DebugAddGold(500); // the test is about persistence, not balance
            var satchelUpgrade = flow.Database.Upgrade("satchel_slots");
            Assert.That(flow.BuyUpgrade(satchelUpgrade));
            int goldAfter = flow.State.Gold;
            int stockAfter = flow.State.Storeroom.TotalCount;
            Assert.That(stockAfter, Is.GreaterThan(0), "some stock is left for tomorrow");

            // ---------- Reload from disk ----------
            yield return Boot();
            flow = GameFlow.Instance;
            Assert.That(flow.InGame, Is.False, "a fresh boot has no game until Continue");
            Assert.That(flow.HasSave);
            Assert.That(flow.Continue());
            yield return WaitForScene(GameScenes.Tavern);
            director = TavernDirector.Instance;
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Night), "resumes where it was saved");
            Assert.That(flow.State.UpgradeLevel("satchel_slots"), Is.EqualTo(1));
            Assert.That(flow.State.Gold, Is.EqualTo(goldAfter));
            Assert.That(flow.State.Storeroom.TotalCount, Is.EqualTo(stockAfter));

            // ---------- Next day: the upgrade applies ----------
            float freshBefore = flow.State.Storeroom.Stacks[0].Freshness;
            director.Sleep();
            yield return WaitForScene(GameScenes.Tavern);
            Assert.That((flow.State.Day, flow.Phase), Is.EqualTo((2, DayPhase.Morning)));
            Assert.That(flow.State.Storeroom.Stacks[0].Freshness, Is.LessThan(freshBefore), "overnight storeroom loss");
            TavernDirector.Instance.Descend();
            yield return WaitForScene(GameScenes.Dungeon);
            Assert.That(DelveRunController.Active.Satchel.Capacity, Is.EqualTo(7), "the bought satchel slot");
        }

        [UnityTest]
        public IEnumerator Breakfast_CookedOnTheGrill_BuffsTheNextDelve()
        {
            yield return Boot();
            var flow = GameFlow.Instance;
            flow.NewGame();
            yield return WaitForScene(GameScenes.Tavern);
            var director = TavernDirector.Instance;
            var grilled = director.Content.recipes.First(r => r.id == "grilled_haunch");
            flow.State.Storeroom.Add(new Shared.Inventory.IngredientStack(new IngredientItem(flow.Database.Ingredient("rat_haunch"), Quality.Fine), 1));
            yield return null;
            Assert.That(director.BreakfastOptions(), Has.Member(grilled));
            Assert.That(director.BreakfastOptions().Any(r => r.station == CookStation.StewPot), Is.False, "no stews at breakfast");
            Assert.That(director.CanCookBreakfast(grilled));

            Assert.That(director.CookBreakfast(grilled));
            var grill = director.Player.ActiveCook as Tavern.Minigames.GrillMinigame;
            Assert.That(grill, Is.Not.Null, "breakfast uses the station's minigame");
            yield return null;
            Assert.That(Visible<TavernMorningScreen>(), Is.False, "the morning screen steps aside for the minigame");

            var restoreInput = UseInputWithoutFocus();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                // Flip each side in the golden band with Space.
                for (int side = 0; side < grill.SideCount; side++)
                {
                    float timeout = Time.realtimeSinceStartup + 10f;
                    while ((grill.Side != side || grill.IsPausing || grill.Meter < grill.Settings.BandCenter) && Time.realtimeSinceStartup < timeout)
                        yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                    yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return null;
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                restoreInput();
            }
            yield return null;

            var meal = flow.State.Meal;
            Assert.That(meal.IsActive, "breakfast eaten");
            Assert.That(meal.Kind, Is.EqualTo(MealBuffKind.MaxEssence));
            Assert.That(meal.Amount, Is.GreaterThan(grilled.mealBuff.amount * 0.8f), "a well-flipped Fine haunch gives most of the buff");
            Assert.That(director.CanCookBreakfast(grilled), Is.False, "one breakfast per morning");
            Assert.That(flow.State.Storeroom.TotalCount, Is.Zero, "the haunch was eaten");

            director.Descend();
            yield return WaitForScene(GameScenes.Dungeon);
            var vitals = Object.FindFirstObjectByType<PlayerVitals>();
            Assert.That(vitals.Essence.Modifiers.MaxBonus, Is.EqualTo(meal.Amount).Within(1e-4f), "the buff reaches the dungeon");
        }

        [UnityTest]
        public IEnumerator DyingInTheDelve_KeepsOnlyTheLockbox_AndGoesToEvening()
        {
            yield return Boot();
            var flow = GameFlow.Instance;
            flow.NewGame();
            yield return WaitForScene(GameScenes.Tavern);
            TavernDirector.Instance.Descend();
            yield return WaitForScene(GameScenes.Dungeon);

            var run = DelveRunController.Active;
            var haunch = flow.Database.Ingredient("rat_haunch");
            var cap = flow.Database.Ingredient("shroom_cap");
            run.Satchel.Add(new IngredientItem(haunch, Quality.Fine), 3);
            run.Satchel.Add(new IngredientItem(cap, Quality.Standard), 2);

            System.Action<int> choose = null;
            void Capture(Shared.Run.DeathScreenRequested e) => choose = e.OnChosen;
            Core.Events.EventBus<Shared.Run.DeathScreenRequested>.Subscribe(Capture);
            Object.FindFirstObjectByType<PlayerVitals>().Essence.TakeDamage(100000f);
            yield return null;
            Core.Events.EventBus<Shared.Run.DeathScreenRequested>.Unsubscribe(Capture);
            Assert.That(choose, Is.Not.Null);
            choose(1); // keep the caps

            yield return WaitForScene(GameScenes.Tavern);
            Assert.That(flow.Phase, Is.EqualTo(DayPhase.Evening));
            Assert.That(flow.State.Storeroom.TotalCount, Is.EqualTo(2));
            Assert.That(flow.State.Storeroom.Stacks[0].Item.Definition, Is.SameAs(cap));
            Assert.That(flow.State.Today.Delve, Is.EqualTo(DelveOutcome.Died));
            Assert.That(Time.timeScale, Is.EqualTo(1f), "the death pause was released");
        }
    }
}
