using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Tavern.Scene;
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
    /// Not a check: renders the day loop's screens (main menu, Morning, the nightfall card, Night) at 320×180 to
    /// <c>BatchLogs/</c> for layout review. Explicit, so it only runs when asked for by name.
    /// </summary>
    [Explicit]
    public class DayLoopCaptures : LookTestFixture
    {
        string m_SaveDir;
        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            if (m_SaveDir != null && Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        [UnityTest]
        public IEnumerator CaptureTheDayScreens()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveCaptures_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "strings");
            yield return new WaitForSeconds(0.6f);
            TavernEveningCaptures.Capture("BatchLogs/day_menu.png");

            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            // A new game starts with a delve: home with nothing, sleep, and day 2's daytime.
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0, 30f, "the first delve");
            yield return null;
            FreezeEnemies();
            var first = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => first.IsOpen, 5f, "result");
            first.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the first night");
            yield return new WaitForSeconds(0.6f);
            TavernEveningCaptures.Capture("BatchLogs/day_first_night.png");
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime, 30f, "morning");
            IngredientDefinition leg = Flow.Database.Ingredient("spider_leg");
            Flow.State.Storeroom.Add(new IngredientStack(new IngredientItem(leg, Quality.Fine), 2, 0.9f));
            Flow.State.Storeroom.Add(new IngredientStack(new IngredientItem(Flow.Database.Ingredient("slime_gel"), Quality.Standard), 3, 0.7f));
            yield return new WaitForSeconds(1.6f);
            TavernEveningCaptures.Capture("BatchLogs/day_morning.png");
            var morning = Object.FindAnyObjectByType<MorningScreen>();
            morning.Cook(morning.Options.ToList().FindIndex(r => r.id == "grilled_spider_leg"));
            yield return null;
            KeeperWork.Instance.FinishCook(0.9f);
            yield return new WaitForSeconds(0.2f);
            TavernEveningCaptures.Capture("BatchLogs/day_morning_ate.png");

            // The evening, kept shut; the way down; the delve result; the night.
            morning.DescendButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "evening");
            yield return new WaitForSeconds(0.6f);
            TavernEveningCaptures.Capture("BatchLogs/day_evening.png");
            Director.CloseForTheNight();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            yield return null;
            FreezeEnemies();
            DelveRunController.Active.Loot.AddGold(24);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "result");
            yield return new WaitForSecondsRealtime(0.5f);
            TavernEveningCaptures.Capture("BatchLogs/day_delve_result.png");
            result.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "night");
            TavernEveningCaptures.Capture("BatchLogs/day_nightfall.png");
            Flow.DebugAddGold(120);
            yield return new WaitForSeconds(2.5f);
            TavernEveningCaptures.Capture("BatchLogs/day_night.png");
        }
    }
}
