using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using LevelManager = MoreMountains.TopDownEngine.LevelManager;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4g Checkpoint C at 320×180 (not a check): Boog's and Orik's callbacks to the troll over the night (its panel stepped aside),
    /// and Boog's bomb on the floor with its marker. Output in <c>HD_CAPTURE_DIR</c> (default <c>BatchLogs/story</c>). Explicit.
    /// </summary>
    [Explicit]
    public class StoryCheckpointCCaptures : LookTestFixture
    {
        static string Out => Environment.GetEnvironmentVariable("HD_CAPTURE_DIR") is { Length: > 0 } dir ? dir : "BatchLogs/story";
        string m_SaveDir;
        static GameFlow Flow => GameFlow.Instance;
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
            RoomRunner.StartInArenaOverride = false;
            MenuPause.Clear();
            Time.timeScale = 1f;
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static void Shot(string name) => TavernEveningCaptures.Capture($"{Out}/{name}.png");

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static IEnumerator Finish()
        {
            for (int i = 0; i < 10 && Box.IsRevealing; i++)
            {
                Box.Advance();
                yield return null;
            }
            yield return Frames(2);
        }

        static IEnumerator Next()
        {
            yield return Finish();
            Box.Advance();
            yield return Frames(2);
        }

        IEnumerator Delve(bool arena)
        {
            RoomRunner.StartInArenaOverride = arena;
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "tables");
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0, 30f, "delve");
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 6f, "revealed");
            Player = LevelManager.Instance.Players[0];
        }

        [UnityTest]
        public IEnumerator TheTrollCallbacks()
        {
            yield return Delve(arena: true);
            Player.GetComponent<EssenceHealth>().GodMode = true;
            var encounter = Object.FindAnyObjectByType<BossEncounter>();
            yield return WaitUntil(() => encounter.State == BossEncounterState.Fighting, 8f, "troll");
            var health = encounter.GetComponent<BossHealth>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "defeat");
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "result");
            result.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && TavernDirector.Instance != null && TavernDirector.Instance.Phase == TavernPhase.Night, 30f, "night");
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 6f, "revealed");
            Shot("c_night_before_talk");
            Assert.That(StoryServices.Conversations.Talk(CharacterIds.Boog));
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Finish();
            Shot("c_boog_troll");
            yield return Next();
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return Frames(3);
            Shot("c_boog_troll_choices");
            DialogueManager.StopAllConversations();
            yield return Frames(3);
            Assert.That(StoryServices.Conversations.Talk(CharacterIds.Orik));
            yield return Finish();
            Shot("c_orik_troll");
            yield return Next();
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return Frames(3);
            Shot("c_orik_troll_choices");
            DialogueManager.StopAllConversations();
        }

        [UnityTest]
        public IEnumerator TheBombAndItsMarker()
        {
            yield return Delve(arena: false);
            Flow.WantQuestObject("boogs_bomb");
            RunSettings settings = Resources.FindObjectsOfTypeAll<RunSettings>().First();
            QuestObjectPickup bomb = Object.Instantiate(settings.questObjectPickup, (Vector2)Player.transform.position + new Vector2(3f, 2f), Quaternion.identity);
            bomb.Set(Flow.Database.QuestObject("boogs_bomb"));
            yield return new WaitForSecondsRealtime(1f);
            Shot("c_bomb_marker");
            yield return new WaitForSecondsRealtime(0.4f);
            Shot("c_bomb_marker_2");
        }
    }
}
