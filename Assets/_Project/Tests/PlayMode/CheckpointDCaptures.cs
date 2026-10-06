using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
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
    /// Not a check: renders 4f Checkpoint D for review at 320×180 into <c>BatchLogs/checkpointD/</c>: a special request in
    /// service (the sparkle on the bubble and the rail), met (a heart) and missed (a frown), the results with the requests
    /// line, the Prep menu, the trophy's first spot on the reworked back wall; and through the day loop the Night screen,
    /// the catalog's last tier, the guest room and the market. Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class CheckpointDCaptures : LookTestFixture
    {
        const string k_Out = "BatchLogs/checkpointD";
        string m_SaveDir;

        static TavernDirector Director => TavernDirector.Instance;
        static GameFlow Flow => GameFlow.Instance;

        [SetUp]
        public void Prepare()
        {
            Directory.CreateDirectory(k_Out);
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveCaptures_" + Guid.NewGuid().ToString("N"));
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
            TavernEveningCaptures.Capture($"{k_Out}/{name}.png");
        }

        static CustomerRequestSettings Always(int max) => new()
        {
            chance = 1f, maxPerEvening = max, ordersBeforeFirst = 0, bonusFraction = 0.5f, minBonusGold = 2, bonusRenown = 1,
        };

        [UnityTest]
        public IEnumerator CaptureRequestsInService()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("prep_menu");

            // The trophy's first spot on the reworked back wall.
            AreaFurniture tavern = AreaFurniture.Tavern;
            tavern.State.Receive(tavern.Definition("trophy_larder_troll"));
            tavern.State.PendingHomecoming = "trophy_larder_troll";
            DecorateMode mode = DecorateMode.Instance;
            mode.Enter(tavern);
            yield return Shot("homecoming_clean_spot");
            mode.Place();
            mode.Leave();
            yield return null;

            // Two patrons ask; one is served, one waits until closing.
            RecipeDefinition dish = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            foreach (RecipeSlot slot in dish.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new IngredientStack(new IngredientItem(slot.ingredient, Quality.Fine), 6, 1f));
            Director.AssignStaff(StaffStation.None);
            Director.AssignCook(StaffStation.None);
            Director.SetMenu(new[] { dish });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Director.Session.ConfigureRequests(Always(2));
            CustomerProfile patient = Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First();
            CustomerAgent first = Director.SpawnCustomer(patient);
            yield return new WaitForSecondsRealtime(1.5f);
            CustomerAgent second = Director.SpawnCustomer(patient);
            yield return WaitUntil(() => first.Logic.State == CustomerState.WaitingForFood && second.Logic.State == CustomerState.WaitingForFood, 40f, "two orders");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("request_in_service");
            Ticket t = Director.Session.Tickets.First(x => x.Customer == first.Logic);
            Director.Session.StartCooking(t, this);
            Director.Session.FinishCooking(t, 1f);
            Director.Session.StartDelivery(t, this);
            Director.Session.Deliver(t, first.Logic, 1f);
            Time.timeScale = 4f;
            yield return WaitUntil(() => first.Logic.RequestOutcome == RequestOutcome.Completed, 30f, "the first request met");
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("request_met");
            Director.EndServiceNow();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("request_missed");
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            Object.FindAnyObjectByType<EveningResultsScreen>().DoneButton.onClick.Invoke();
            yield return Shot("results_with_requests");
        }

        [UnityTest]
        public IEnumerator CaptureTheDayLoop()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            yield return new WaitForSecondsRealtime(0.5f);
            LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel.Add(new IngredientItem(Flow.Database.Ingredient("spider_leg"), Quality.Fine), 3);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the result");
            result.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "night");
            var cover = Object.FindAnyObjectByType<TransitionScreen>();
            if (cover != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("night");

            // The catalog's last tier, then the guest room.
            var night = Object.FindAnyObjectByType<NightScreen>();
            night.DecorateButton.onClick.Invoke();
            yield return null;
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            screen.OpenStorage();
            DecorateCatalogue catalogue = screen.Catalogue;
            catalogue.Tab(2);
            yield return null;
            catalogue.Select(Math.Max(0, catalogue.Count - 1));
            yield return Shot("catalog_page");
            catalogue.Close();
            DecorateMode.Instance.SwitchArea();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("guest_room");
            DecorateMode.Instance.Leave();
            yield return null;

            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime, 30f, "daytime");
            if ((cover = Object.FindAnyObjectByType<TransitionScreen>()) != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.3f);
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            daytime.MarketButton.onClick.Invoke();
            daytime.Market.Rows[1].buy.onClick.Invoke();
            daytime.Market.Rows[3].buy.onClick.Invoke();
            yield return Shot("market");
        }
    }
}
