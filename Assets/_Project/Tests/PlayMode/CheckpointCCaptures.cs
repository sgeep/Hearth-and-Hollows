using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Run;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Scene;
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
    /// Not a check: renders 4f Checkpoint C for review at 320×180 into <c>BatchLogs/checkpointC/</c>: curios on the floor and
    /// in the HUD, the result and death screens, the tusks' homecoming and the room with them up, the market, Prep with the
    /// staff row and the menu's pages, the Butcher Block (list, cut, Gunta at it), Gunta and Orik at work, Orik's ledger, and
    /// a new piece in storage. Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class CheckpointCCaptures : LookTestFixture
    {
        const string k_Out = "BatchLogs/checkpointC";
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;

        [SetUp]
        public void UseTempSaves()
        {
            Directory.CreateDirectory(k_Out);
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveCaptures_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            RoomRunner.StartInArenaOverride = false;
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

        IEnumerator NewGameToTheDelve()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            GameFlow.Instance.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return new WaitForSecondsRealtime(0.5f);
            FreezeEnemies();
        }

        static IEnumerator InTavern(TavernPhase phase)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, phase.ToString());
            // The scene change's cover (the day and phase card) has to lift before a shot.
            var cover = Object.FindAnyObjectByType<TransitionScreen>();
            if (cover != null) yield return WaitUntil(() => !cover.IsCovering, 10f, "the cover to lift");
            yield return new WaitForSecondsRealtime(0.3f);
        }

        IEnumerator PickUpAll()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            foreach (CurioPickup pickup in Object.FindObjectsByType<CurioPickup>())
            {
                Player.transform.position = pickup.transform.position;
                if (Player.TryGetComponent(out Rigidbody2D body)) body.position = pickup.transform.position;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CaptureTheDelve()
        {
            // Curios: a spider's rare drop and a room's cache, on the floor; then carried, in the HUD and the feed.
            yield return NewGameToTheDelve();
            CurioPool pool = RoomRunner.Active.Settings.tuning.curios;
            float chance = pool.enemyDropChance;
            pool.enemyDropChance = 1f;
            var spider = ScriptableObject.CreateInstance<EnemyDefinition>();
            spider.id = pool.entries.First(e => e.droppedBy != null && e.droppedBy.Length > 0).droppedBy[0];
            EventBus<EnemyKilled>.Publish(new EnemyKilled(spider, default, (Vector2)Player.transform.position + new Vector2(2.5f, 1f)));
            pool.enemyDropChance = chance;
            RoomRunner.Active.GrantReward(RoomReward.Curio());
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("curio_on_the_floor");
            yield return PickUpAll();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("curio_found_hud");
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("result_curios_kept");
            result.Proceed();
            yield return InTavern(TavernPhase.Night);

            // A new piece in storage.
            Object.FindAnyObjectByType<NightScreen>().DecorateButton.onClick.Invoke();
            yield return null;
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            screen.OpenStorage();
            yield return Shot("storage_new_badge");
            screen.Catalogue.Close();
            DecorateMode.Instance.Leave();
            yield return null;

            // The market, next daytime.
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime);
            yield return null;
            Flow.DebugAddGold(30);
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            yield return Shot("daytime_with_market");
            daytime.MarketButton.onClick.Invoke();
            yield return null;
            daytime.Market.Rows[0].buy.onClick.Invoke();
            daytime.Market.Rows[2].buy.onClick.Invoke();
            yield return Shot("market");
            daytime.Market.Done.onClick.Invoke();

            // Dying with a curio: the death screen and the result say what was lost.
            daytime.DescendButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Prep);
            Object.FindAnyObjectByType<PrepScreen>().CloseButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return new WaitForSecondsRealtime(0.5f);
            FreezeEnemies();
            RoomRunner.Active.GrantReward(RoomReward.Curio());
            yield return PickUpAll();
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include); // this delve's scene
            var essence = Player.GetComponent<EssenceHealth>();
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("death_curio_lost");
            death.KeepNothingButton.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("result_curio_lost");
            Object.Destroy(spider);
        }

        [UnityTest]
        public IEnumerator CaptureTheTrophy()
        {
            RoomRunner.StartInArenaOverride = true;
            yield return NewGameToTheDelve();
            Player.GetComponent<EssenceHealth>().GodMode = true;
            var encounter = Object.FindAnyObjectByType<Hearthdelve.Dungeon.Bosses.BossEncounter>();
            yield return WaitUntil(() => encounter.State == Hearthdelve.Dungeon.Bosses.BossEncounterState.Fighting, 8f, "the troll");
            var health = encounter.GetComponent<Hearthdelve.Dungeon.Bosses.BossHealth>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "the defeat recorded");
            yield return new WaitForSecondsRealtime(1f);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("result_trophy");
            result.Proceed();
            yield return InTavern(TavernPhase.Night);
            Object.FindAnyObjectByType<NightScreen>().DecorateButton.onClick.Invoke();
            yield return null;
            DecorateMode mode = DecorateMode.Instance;
            yield return Shot("homecoming");
            for (int x = 6; x < 24 && !mode.CarriedCheck.IsValid; x++)
            for (int y = 14; y <= 15 && !mode.CarriedCheck.IsValid; y++)
                mode.SetCursor(new Vector2Int(x, y));
            yield return Shot("homecoming_on_the_wall");
            mode.Place();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("homecoming_placed");
            mode.Leave();
            yield return null;
            Object.FindAnyObjectByType<NightScreen>().gameObject.SetActive(false);
            foreach (Canvas c in Object.FindObjectsByType<Canvas>()) c.enabled = false;
            yield return Shot("tusks_on_the_wall");
        }

        [UnityTest]
        public IEnumerator CaptureTheKitchen()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return new WaitForSecondsRealtime(0.5f);
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return Shot("prep_page1");
            prep.NextPage();
            yield return Shot("prep_page2");
            prep.NextPage();
            prep.CookButton.onClick.Invoke(); // Gunta: the Grill
            yield return Shot("prep_gunta_grill");

            // The Butcher Block: the list, a cut mid-stroke, Gunta at it.
            prep.ButcherButton.onClick.Invoke();
            yield return null;
            ButcherPanel panel = prep.Butcher;
            yield return Shot("butcher_list");
            Assert.That(panel.CutYourself(0));
            var game = (ButcherMinigame)KeeperWork.Instance.ActiveCook;
            var hand = new ButcherAutoPlayer(game, 0.8f, new Hearthdelve.Core.Random.SeededRandom(5));
            for (int i = 0; i < 60 * 3 && !game.IsComplete && !(game.Stroke == 1 && game.StrokeProgress > 0.5f); i++) game.Tick(1f / 60f, hand.NextInput(1f / 60f));
            yield return Shot("butcher_cut");
            for (int i = 0; i < 60 * 20 && !game.IsComplete; i++) game.Tick(1f / 60f, hand.NextInput(1f / 60f));
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot("butcher_result");
            panel.LetStaff(0);
            Time.timeScale = 3f;
            yield return WaitUntil(() => Director.CookButchering, 10f, "Gunta at the block");
            Time.timeScale = 1f;
            panel.gameObject.SetActive(true);
            foreach (Canvas c in Object.FindObjectsByType<Canvas>()) c.enabled = false;
            yield return Shot("gunta_at_the_block");
            foreach (Canvas c in Object.FindObjectsByType<Canvas>()) c.enabled = true;
            yield return WaitUntil(() => !Director.Cook.HasTask, 10f, "Gunta done");
            panel.Close();

            // Service: Gunta grills, Orik carries; then the results and Orik's ledger.
            RecipeDefinition grilled = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            foreach (var need in grilled.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new IngredientStack(new IngredientItem(need.ingredient, Quality.Standard), 4, 1f));
            Director.SetMenu(new[] { grilled });
            Director.AssignCook(StaffStation.Grill);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).Skip(1).First());
            bool gunta = false;
            void Done(StaffWorkDone e) => gunta |= e.StaffId == StaffIds.Boog;
            EventBus<StaffWorkDone>.Subscribe(Done);
            Time.timeScale = 3f;
            yield return WaitUntil(() => Director.Cook.WorkingTask || Director.Session.Tickets.Any(t => t.State == TicketState.Cooking), 40f, "Gunta grilling");
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("service_gunta_and_pip");
            Time.timeScale = 4f;
            yield return WaitUntil(() => gunta && Director.Session.Ledger.Gold > 0, 60f, "a dish paid for");
            Time.timeScale = 1f;
            EventBus<StaffWorkDone>.Unsubscribe(Done);
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            results.DoneButton.onClick.Invoke();
            yield return Shot("results_ledger");
        }
    }
}
