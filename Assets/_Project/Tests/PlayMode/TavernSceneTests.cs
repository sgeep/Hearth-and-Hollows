using System.Collections;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
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

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>End-to-end runs of the generated TavernGreybox scene. Any logged error fails the test.</summary>
    public class TavernSceneTests
    {
        const string k_Scene = "TavernGreybox";

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        static IEnumerator Load()
        {
            var op = SceneManager.LoadSceneAsync(k_Scene, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
        }

        static VisualElement Root<T>() where T : Component =>
            Object.FindFirstObjectByType<T>().GetComponent<UIDocument>().rootVisualElement;

        [UnityTest]
        public IEnumerator PrepScreen_IsLocalized_AndDebugFillStocksMixedQuality()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Prep));
            Assert.That(Root<TavernPrepScreen>().Q<Label>("title").text, Is.EqualTo("Evening Prep"));

            director.FillStoreroom();
            yield return null;
            Assert.That(director.Storeroom.TotalCount, Is.GreaterThan(0));
            Assert.That(director.Storeroom.Stacks.Select(s => s.Item.Quality).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(director.Content.recipes, Has.Count.EqualTo(5));
            Assert.That(director.Content.recipes.All(r => director.ServingsAvailable(r) > 0), "a debug fill can make every recipe");
        }

        [UnityTest]
        public IEnumerator MenuSelection_IsCappedAtThree()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            director.FillStoreroom();
            foreach (var r in director.Content.recipes) director.ToggleMenu(r);
            Assert.That(director.SelectedMenu, Has.Count.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Service_RunsEndToEnd_WithStaffOnly_AndShowsResults()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            director.FillStoreroom();
            foreach (var r in director.Content.recipes.Where(r => r.station == CookStation.Grill).Take(2)) director.ToggleMenu(r);
            director.AssignStaff(StaffStation.Serving); // the scene's staff helper carries plates
            director.OpenService();
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Service));

            // A second, test-only cook works the grill through the same IMinigame auto-resolve path.
            var staff = director.StaffMember;
            var cook = new StaffCook(director.Session, director.Minigames, CookStation.Grill, staff.skill, staff.qualityCap, 0.2f, new SeededRandom(9));

            Time.timeScale = 6f;
            var profiles = director.Content.customers;
            director.SpawnCustomer(profiles[0]);
            director.SpawnCustomer(profiles[1]);
            float timeout = Time.realtimeSinceStartup + 40f;
            while (director.Session.Ledger.DishesServed < 2 && Time.realtimeSinceStartup < timeout)
            {
                cook.Tick(Time.deltaTime);
                yield return null;
            }
            Time.timeScale = 1f;

            var ledger = director.Session.Ledger;
            Assert.That(ledger.DishesServed, Is.GreaterThanOrEqualTo(2), "customers were cooked for, served, and paid without player input");
            Assert.That(ledger.Gold, Is.GreaterThan(0));

            director.EndServiceNow();
            yield return null;
            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Results));
            var results = Root<TavernResultsScreen>();
            Assert.That(results.Q("screen").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(results.Q("rows").childCount, Is.GreaterThanOrEqualTo(5));
        }

        [UnityTest]
        public IEnumerator CarryingAPlate_HighlightsTheCustomerInteractWouldServe_AndInteractServesThem()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            var grilled = director.Content.recipes.First(r => r.id == "grilled_haunch");
            director.Storeroom.Add(new IngredientStack(new IngredientItem(grilled.slots[0].ingredient, Quality.Standard), 1));
            director.ToggleMenu(grilled);
            director.OpenService();

            // Batch runs have no window focus; by default the Input System ignores keyboards without it.
            var originalSettings = InputSystem.settings;
            var settings = Object.Instantiate(originalSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Time.timeScale = 6f;
                director.SpawnCustomer(director.Content.customers[0]);
                float timeout = Time.realtimeSinceStartup + 20f;
                while (director.Session.Tickets.Count == 0 && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(director.Session.Tickets, Has.Count.EqualTo(1), "someone ordered the only serving");
                var ticket = director.Session.Tickets[0];
                var agent = director.Agents.First(a => a.Logic == ticket.Customer);
                director.Session.StartCooking(ticket, this);
                director.Session.FinishCooking(ticket, 1f);
                Assert.That(agent.IsHighlighted, Is.False);

                // Pick the plate up at the pass.
                var player = director.Player;
                var p = player.transform.position;
                player.transform.position = new Vector3(director.Layout.Pass.X, p.y, p.z);
                yield return Press(keyboard, Key.E);
                Assert.That(player.Carrying, Is.Not.Null, "Interact at the pass picks the dish up");
                Assert.That(agent.IsHighlighted, Is.False, "out of reach");

                // Walk left to the customer.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                timeout = Time.realtimeSinceStartup + 20f;
                while (player.X - agent.X > 0.3f && Time.realtimeSinceStartup < timeout) yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;

                Assert.That(player.ServeTarget, Is.SameAs(agent));
                Assert.That(agent.IsHighlighted, "the customer who'd receive the plate is highlighted");
                Assert.That(director.Agents.Where(a => a != agent).All(a => !a.IsHighlighted));

                yield return Press(keyboard, Key.E);
                Assert.That(player.Carrying, Is.Null);
                Assert.That(agent.Logic.State, Is.EqualTo(CustomerState.Eating), "Interact served them");
                Assert.That(agent.IsHighlighted, Is.False, "highlight clears once the plate is handed over");
            }
            finally
            {
                Time.timeScale = 1f;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = originalSettings;
                Object.Destroy(settings);
            }
        }

        static IEnumerator Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            Assert.That(keyboard[key].isPressed, $"{key} press reached the virtual keyboard");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SellingOut_ClosesEarly_OnceTheLastOrderIsServedAndPaid()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            var grilled = director.Content.recipes.First(r => r.id == "grilled_haunch");
            var haunch = grilled.slots[0].ingredient;
            director.Storeroom.Add(new IngredientStack(new IngredientItem(haunch, Quality.Standard), 1));
            director.ToggleMenu(grilled);
            director.AssignStaff(StaffStation.Serving);
            director.OpenService();

            var staff = director.StaffMember;
            var cook = new StaffCook(director.Session, director.Minigames, CookStation.Grill, staff.skill, staff.qualityCap, 0.2f, new SeededRandom(4));
            Time.timeScale = 6f;
            director.SpawnCustomer(director.Content.customers[0]);
            float timeout = Time.realtimeSinceStartup + 40f;
            while (director.Phase == TavernPhase.Service && Time.realtimeSinceStartup < timeout)
            {
                if (director.Session.AllSoldOut) Assert.That(director.Session.CanAdmitCustomer, Is.False, "the door closes once everything is sold out");
                cook.Tick(Time.deltaTime);
                yield return null;
            }
            Time.timeScale = 1f;
            yield return null;

            Assert.That(director.Phase, Is.EqualTo(TavernPhase.Results));
            Assert.That(director.Session.ClosedEarly);
            Assert.That(director.Session.Ledger.DishesServed, Is.EqualTo(1), "the last order was still served and paid for");
            Assert.That(director.Session.Remaining, Is.GreaterThan(0f));
            var rows = Root<TavernResultsScreen>().Q("rows").Query<Label>().ToList();
            Assert.That(rows.Select(l => l.text), Has.Member("Everything sold out, so we closed early."));
        }

        [UnityTest]
        public IEnumerator SoldOut_IsMarkedOnHud_AndNextCustomerLeavesWithSmallPenalty()
        {
            yield return Load();
            var director = TavernDirector.Instance;
            var grilled = director.Content.recipes.First(r => r.id == "grilled_haunch");
            var haunch = grilled.slots[0].ingredient;
            director.Storeroom.Add(new IngredientStack(new IngredientItem(haunch, Quality.Standard), 1));
            director.ToggleMenu(grilled);
            director.OpenService();

            Time.timeScale = 6f;
            var profile = director.Content.customers[0];
            director.SpawnCustomer(profile);
            float timeout = Time.realtimeSinceStartup + 15f;
            while (!director.Session.IsSoldOut(grilled) && Time.realtimeSinceStartup < timeout) yield return null;
            director.SpawnCustomer(profile);
            timeout = Time.realtimeSinceStartup + 15f;
            while (director.Session.Ledger.SoldOutLeaves == 0 && Time.realtimeSinceStartup < timeout) yield return null;
            Time.timeScale = 1f;
            yield return null;

            Assert.That(director.Session.IsSoldOut(grilled));
            Assert.That(Root<TavernHud>().Query<Label>(className: "hd-tag--sold-out").ToList(), Is.Not.Empty, "HUD marks the dish sold out");
            Assert.That(director.Session.Ledger.SoldOutLeaves, Is.EqualTo(1));
            Assert.That(director.Session.Ledger.Walkouts, Is.EqualTo(0));
            Assert.That(director.Session.Ledger.Renown, Is.EqualTo(director.Content.economy.service.soldOutRenown));
        }
    }
}
