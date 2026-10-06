using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
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
    /// 4g Checkpoint B at 320×180 (not a check): the creator (each body, the letters), the arrival and the hatch, the keeper in play,
    /// the first delve's prompts, Boog's bomb on the floor and found, and Boog's conversation about her. Output in
    /// <c>HD_CAPTURE_DIR</c> (default <c>BatchLogs/story</c>). Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class StoryCheckpointBCaptures : LookTestFixture
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

        static void Shot(string name) => TavernEveningCaptures.Capture($"{Out}/{name}.png");

        static IEnumerator Menu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the menu");
            yield return WaitUntil(() => Loc.IsReady && (Flow.Transition == null || !Flow.Transition.IsCovering), 10f, "revealed");
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, phase.ToString());
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 5f, "revealed");
            yield return null;
        }

        static IEnumerator Finish()
        {
            for (int i = 0; i < 10 && Box.IsRevealing; i++)
            {
                Box.Advance();
                yield return null;
            }
            yield return null;
        }

        static IEnumerator Next()
        {
            yield return Finish();
            Box.Advance();
            yield return null;
            yield return null;
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator TheCreator()
        {
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.NewGameButton.onClick.Invoke();
            CharacterCreatorScreen creator = menu.Creator;
            yield return Frames(20);
            Shot("creator_townsfolk");
            creator.Change(CharacterCreatorScreen.HairRow, 3);
            creator.Change(CharacterCreatorScreen.OutfitRow, 5);
            yield return Frames(20);
            Shot("creator_townsfolk_recoloured");
            foreach (string body in new[] { "amazon", "dwarf", "orc" })
            {
                creator.Change(CharacterCreatorScreen.BodyRow, 1);
                yield return Frames(30);
                Shot($"creator_{body}");
            }
            creator.Change(CharacterCreatorScreen.SkinRow, 1);
            creator.Change(CharacterCreatorScreen.OutfitRow, 2);
            yield return Frames(20);
            Shot("creator_orc_recoloured");
            creator.RowFor(CharacterCreatorScreen.NameRow).button.onClick.Invoke();
            yield return Frames(3);
            foreach (string key in new[] { "<delete>", "<delete>", "<delete>", "<delete>", "w|W", "r|R", "e|E", "n|N" }) creator.Press(key);
            yield return Frames(3);
            Shot("creator_naming");
            creator.Press(CharacterCreatorScreen.KeyDone);
            creator.Change(CharacterCreatorScreen.BodyRow, -1);
            creator.Change(CharacterCreatorScreen.HairRow, 2);
            yield return Frames(10);
            Shot("creator_dwarf_named");
            creator.BeginButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Arrival);
            yield return WaitUntil(() => Box.IsOpen, 3f, "Orik");
            yield return Frames(10);
            Shot("arrival_orik_revealing");
            yield return Finish();
            Shot("arrival_orik");
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return Frames(3);
            Shot("arrival_choices");
            for (int i = 0; i < 30 && Box.IsOpen; i++)
            {
                if (Box.IsChoosing)
                {
                    yield return WaitUntil(() => !Box.ChoicesLocked, 2f, "unlocked");
                    if (Box.Line.StartsWith("bring back")) Shot("arrival_boog");
                    Box.Choose(0);
                    yield return Frames(2);
                    continue;
                }
                if (Box.SpeakerName == "Boog" && Box.Line.StartsWith("and i'm Boog"))
                {
                    yield return Finish();
                    Shot("arrival_boog_hello");
                }
                yield return Next();
            }
            yield return Frames(30);
            var hatch = Object.FindAnyObjectByType<OpeningHatch>();
            var keeper = Object.FindAnyObjectByType<TavernInteractor>();
            Teleport(keeper, hatch.Interactable.UsePoint + new Vector2(1.5f, -0.5f));
            yield return Frames(30);
            Shot("arrival_keeper_dwarf");
            Teleport(keeper, hatch.Interactable.UsePoint);
            yield return Frames(20);
            Shot("arrival_hatch");
        }

        [UnityTest]
        public IEnumerator TheFirstDelve_AndTheBomb()
        {
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.NewGameButton.onClick.Invoke();
            menu.Creator.Change(CharacterCreatorScreen.BodyRow, -1);
            menu.Creator.BeginButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Arrival);
            yield return WaitUntil(() => Box.IsOpen, 3f, "Orik");
            DialogueManager.StopAllConversations();
            yield return Frames(3);
            Flow.BeginFirstDelve();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 5f, "revealed");
            Player = LevelManager.Instance.Players[0];
            yield return Frames(30);
            Shot("delve_prompt_move");
            EventBus<EssenceChanged>.Publish(new EssenceChanged(40f, 50f, false, true));
            yield return new WaitForSecondsRealtime(3f);
            Shot("delve_prompt_essence");

            // The bomb, as the second fight leaves it (placed by hand here, beside the keeper).
            Flow.WantQuestObject("boogs_bomb");
            RunSettings settings = Resources.FindObjectsOfTypeAll<RunSettings>().First();
            QuestObjectPickup bomb = Object.Instantiate(settings.questObjectPickup, (Vector2)Player.transform.position + new Vector2(2f, 0f), Quaternion.identity);
            bomb.Set(Flow.Database.QuestObject("boogs_bomb"));
            yield return Frames(40);
            Shot("delve_bomb");
            Teleport(Player, bomb.transform.position);
            yield return WaitUntil(() => DelveRunController.Active.Loot.Carries("boogs_bomb"), 3f, "picked up");
            yield return Frames(5);
            Shot("delve_bomb_found");
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "result");
            yield return Frames(5);
            Shot("delve_result_bomb_home");
            result.Proceed();
            yield return InTavern(TavernPhase.Night);
            yield return WaitUntil(() => Box.IsOpen, 4f, "the homecoming");
            yield return Finish();
            Shot("homecoming_boog");
            DialogueManager.StopAllConversations();
            yield return Frames(3);

            // Boog, handed his bomb.
            Assert.That(StoryServices.Conversations.Talk(CharacterIds.Boog));
            yield return Frames(3);
            yield return Finish();
            Shot("bomb_return");
            yield return Next();
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return Frames(3);
            Shot("bomb_return_choices");
            DialogueManager.StopAllConversations();
        }

        [UnityTest]
        public IEnumerator BoogsOffer()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the menu");
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && DelveRunController.Active != null, 30f, "the delve");
            yield return Frames(10);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "result");
            result.Proceed();
            yield return InTavern(TavernPhase.Night);
            Assert.That(StoryServices.Conversations.Talk(CharacterIds.Boog));
            yield return Frames(3);
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return WaitUntil(() => !Box.ChoicesLocked, 2f, "unlocked");
            Box.Choose(0);
            yield return Frames(3);
            yield return Finish();
            Shot("bomb_offer");
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "choices");
            yield return Frames(3);
            Shot("bomb_offer_choices");
            yield return WaitUntil(() => !Box.ChoicesLocked, 2f, "unlocked");
            Box.Choose(1);
            yield return Frames(3);
            yield return Next();
            yield return Next();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "ask");
            yield return Frames(3);
            Shot("bomb_ask");
            DialogueManager.StopAllConversations();
        }
    }
}
