using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: renders the screens the typography pass touches at 320×180, for before/after comparison. The folder
    /// comes from the <c>HD_CAPTURE_DIR</c> environment variable (default <c>BatchLogs/typography/after</c>), so the same
    /// shots can be taken before and after a change. Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class TypographyCaptures : LookTestFixture
    {
        static string Out => Environment.GetEnvironmentVariable("HD_CAPTURE_DIR") is { Length: > 0 } dir ? dir : "BatchLogs/typography/after";
        string m_SaveDir;

        static TavernDirector Director => TavernDirector.Instance;
        static GameFlow Flow => GameFlow.Instance;

        [SetUp]
        public void Prepare()
        {
            Directory.CreateDirectory(Out);
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTypeCaptures_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void Restore()
        {
            GameFlow.SaveDirectoryOverride = null;
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator Shot(string name)
        {
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture($"{Out}/{name}.png");
        }

        static void Stock(string id, int count)
        {
            IngredientDefinition definition = Director.Content.recipes.SelectMany(r => r.slots).Select(s => s.ingredient)
                .FirstOrDefault(i => i != null && i.id == id);
            if (definition != null) Director.Storeroom.Add(new IngredientStack(new IngredientItem(definition, Quality.Fine), count, 1f));
        }

        [UnityTest]
        public IEnumerator CaptureTheTavern()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            Stock("spider_leg", 2);
            Stock("bat_wing", 4);
            yield return new WaitForSecondsRealtime(0.5f);
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return Shot("prep");

            prep.ButcherButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot("butcher_block");
            prep.Butcher.Close();
            yield return null;

            DecorateMode mode = DecorateMode.Instance;
            mode.Enter(AreaFurniture.Tavern);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot("decorate");
            mode.Leave();
            yield return null;

            // Service: a patron's request on the rail and the bubble, one dish served, then the results.
            RecipeDefinition dish = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            foreach (RecipeSlot slot in dish.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new IngredientStack(new IngredientItem(slot.ingredient, Quality.Fine), 6, 1f));
            Director.AssignStaff(StaffStation.None);
            Director.AssignCook(StaffStation.None);
            Director.SetMenu(new[] { dish });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Director.Session.ConfigureRequests(new CustomerRequestSettings
            {
                chance = 1f, maxPerEvening = 1, ordersBeforeFirst = 0, bonusFraction = 0.5f, minBonusGold = 2, bonusRenown = 1,
            });
            CustomerProfile patient = Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First();
            CustomerAgent first = Director.SpawnCustomer(patient);
            yield return WaitUntil(() => first.Logic.State == CustomerState.WaitingForFood, 40f, "an order");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("tavern_hud");
            Ticket t = Director.Session.Tickets.First(x => x.Customer == first.Logic);
            Director.Session.StartCooking(t, this);
            Director.Session.FinishCooking(t, 1f);
            Director.Session.StartDelivery(t, this);
            Director.Session.Deliver(t, first.Logic, 1f);
            Time.timeScale = 4f;
            yield return WaitUntil(() => first.Logic.RequestOutcome == RequestOutcome.Completed, 30f, "the request met");
            Time.timeScale = 1f;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            Object.FindAnyObjectByType<EveningResultsScreen>().DoneButton.onClick.Invoke();
            yield return Shot("results");
        }

        [UnityTest]
        public IEnumerator CaptureTheDayLoop()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("main_menu");
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            var cover = Object.FindAnyObjectByType<TransitionScreen>();
            if (cover != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.5f);
            Satchel satchel = LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel;
            satchel.Add(new IngredientItem(Flow.Database.Ingredient("spider_leg"), Quality.Fine), 3);
            satchel.Add(new IngredientItem(Flow.Database.Ingredient("bat_wing"), Quality.Standard), 2);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("dungeon_hud");
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the result");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("delve_result");
            result.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "night");
            if ((cover = Object.FindAnyObjectByType<TransitionScreen>()) != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("night");

            var night = Object.FindAnyObjectByType<NightScreen>();
            night.DecorateButton.onClick.Invoke();
            yield return null;
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            screen.OpenStorage();
            DecorateCatalogue catalogue = screen.Catalogue;
            // A shop page (the tables): the densest list, with a chosen piece's details.
            catalogue.Tab(2);
            yield return null;
            catalogue.Select(1);
            yield return Shot("catalog");
            catalogue.Close();
            DecorateMode.Instance.Leave();
            yield return null;

            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime, 30f, "daytime");
            if ((cover = Object.FindAnyObjectByType<TransitionScreen>()) != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("daytime");
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            daytime.MarketButton.onClick.Invoke();
            daytime.Market.Rows[1].buy.onClick.Invoke();
            yield return Shot("market");
        }
    }
}
