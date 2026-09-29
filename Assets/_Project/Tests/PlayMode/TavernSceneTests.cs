using System.Collections;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
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
