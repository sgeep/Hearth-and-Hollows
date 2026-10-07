using System;
using System.Collections;
using System.IO;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4g Checkpoint A's dialogue box at 320×180 (not a check): Boog and Orik during an evening's service, the reveal, the ▼, the
    /// choices with the ▶, and Boog's branch after the tusks went up. Output in <c>HD_CAPTURE_DIR</c> (default
    /// <c>BatchLogs/story</c>). Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class StoryCaptures : LookTestFixture
    {
        static string Out => Environment.GetEnvironmentVariable("HD_CAPTURE_DIR") is { Length: > 0 } dir ? dir : "BatchLogs/story";
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static HearthDialogueUI Box => Object.FindAnyObjectByType<HearthDialogueUI>();

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveCaptures_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            MenuPause.Clear();
            Time.timeScale = 1f;
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator InTavern(TavernPhase phase)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, phase.ToString());
            yield return WaitUntil(() => !(Flow.Transition is TransitionScreen t && t.IsCovering), 5f, "revealed");
            yield return null;
        }

        static void Shot(string name)
        {
            TavernEveningCaptures.Capture($"{Out}/{name}.png");
        }

        /// <summary>The line all there (a press during the reveal finishes it).</summary>
        static IEnumerator Finish()
        {
            for (int i = 0; i < 10 && Box.IsRevealing; i++)
            {
                Box.Advance();
                yield return null;
            }
            yield return null;
        }

        /// <summary>The line finished, then on to the next.</summary>
        static IEnumerator Next()
        {
            yield return Finish();
            Box.Advance();
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheDialogueBox_During_Service()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null && Loc.IsReady, 20f, "the menu");
            GameFlow.Instance.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && DelveRunController.Active != null &&
                                         MoreMountains.TopDownEngine.LevelManager.HasInstance && MoreMountains.TopDownEngine.LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            var player = MoreMountains.TopDownEngine.LevelManager.Instance.Players[0];
            player.GetComponent<Hearthdelve.Dungeon.Harvest.SatchelCarrier>().Satchel
                .Add(new Hearthdelve.Shared.Ingredients.IngredientItem(Flow.Database.Ingredient("spider_leg"), Hearthdelve.Shared.Ingredients.Quality.Fine), 3);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the result");
            result.Proceed();
            yield return InTavern(TavernPhase.Night);
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime);
            DaytimeActions.BeginEvening();
            yield return InTavern(TavernPhase.Prep);
            Director.SetMenu(new[] { System.Linq.Enumerable.First(Director.Content.recipes, r => r.id == "grilled_spider_leg") });
            Director.OpenService();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Service, 5f, "service");
            yield return new WaitForSeconds(1.5f);

            // Orik greets the keeper by name, mid-reveal, then waiting with the ▼.
            StoryServices.Conversations.Talk(CharacterIds.Orik);
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("01_pip_revealing");
            yield return Finish();
            yield return new WaitForSecondsRealtime(0.4f);
            Shot("02_pip_waiting");
            Box.Advance();
            yield return null;

            // Boog's offer, then the choices.
            StoryServices.Conversations.Talk(CharacterIds.Boog);
            yield return null;
            yield return Finish();
            Shot("03_boog_offer");
            Box.Advance();
            yield return null;
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Shot("04_boog_choices_first");
            EventSystem.current.SetSelectedGameObject(Box.ChoiceButton(1).gameObject);
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Shot("05_boog_choices_second");
            Box.Choose(0);
            yield return null;
            yield return Finish();
            Box.Advance();
            yield return null;

            // The tusks go up (the deed), and Boog's branch.
            StoryHost.Instance.Commit(DeedSource.TrophyDisplayed);
            StoryServices.Conversations.Talk(CharacterIds.Boog);
            yield return null;
            yield return Finish();
            Shot("06_boog_tusks");
            Box.Advance();
            yield return null;
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Shot("07_boog_tusks_choices");
            Box.Choose(1);
            yield return null;
            yield return Finish();
            yield return new WaitForSecondsRealtime(0.3f);
            Shot("08_boog_answer");
            Box.Advance();
            yield return null;
        }
    }
}
