using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-A, first impressions, in the game: the pause menu in each part of the day (holding the clock and the keeper's controls, and
    /// giving them back), quitting from each phase and what Continue then loads (nothing lost silently, nothing given twice), the first
    /// free morning's talk and prompts (once), a save that can't be read, the saved mark, and prompts naming the device in use.
    /// </summary>
    public class FirstImpressionsPlayTests : LookTestFixture
    {
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static PauseMenu Pause => PauseMenu.Instance;
        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            if (Pause != null && Pause.IsOpen) Pause.Close();
            GameFlow.SaveDirectoryOverride = null;
            SurfacePause.Clear();
            MusicHolds.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            InputDevices.Set(InputDeviceKind.KeyboardMouse);
            if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static IEnumerator Revealed()
        {
            int settled = 0;
            float started = Time.realtimeSinceStartup;
            while (settled < 5)
            {
                bool clear = !Flow.IsLoading && (Flow.Transition == null || !Flow.Transition.IsCovering);
                settled = clear ? settled + 1 : 0;
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(8f), "the scene revealed");
                yield return null;
            }
        }

        static bool MapOn(string map) => InputSystem.actions.FindActionMap(map).enabled;

        IEnumerator Menu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
            yield return Frames(3);
        }

        /// <summary>A quick new game to its first free day (day 2), the keeper waking upstairs. <paramref name="opening"/>: where the story stands.</summary>
        IEnumerator Daytime(OpeningStage opening = OpeningStage.Complete)
        {
            yield return Menu();
            Flow.QuickNewGame();
            Flow.State.Story.Opening = opening;
            Flow.MarkHintSeen(Hearthdelve.Shared.Village.CommunityRules.GimpIntro);
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            yield return Revealed();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            Flow.Sleep();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            yield return Frames(3);
        }

        IEnumerator Evening()
        {
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
        }

        IEnumerator BackAtTheMenu()
        {
            yield return WaitUntil(() => !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return Revealed();
            yield return Frames(3);
        }

        SaveData Saved => Flow.CheckSave().Data;

        // ---------- the pause menu ----------

        [UnityTest]
        public IEnumerator Pausing_InTheDay_HoldsTheClockAndTheKeeper_AndResumingGivesThemBack()
        {
            yield return Daytime();
            Assert.That(PauseMenu.Pausable, Is.True, "on foot in the day");
            Assert.That(MapOn(InputMaps.Tavern), Is.True);
            Pause.Open();
            yield return null;
            Assert.That(Pause.IsOpen, Is.True);
            Assert.That(MenuPause.IsPaused, Is.True, "the game is paused");
            Assert.That(SurfacePause.IsHeld, Is.True, "the clock holds");
            Assert.That(MapOn(InputMaps.Tavern), Is.False, "the keeper's controls are off");
            int minute = Flow.State.Surface.WholeMinute;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(Flow.State.Surface.WholeMinute, Is.EqualTo(minute), "no time passes");
            Pause.Close();
            yield return null;
            Assert.That(MenuPause.IsPaused, Is.False);
            Assert.That(SurfacePause.IsHeld, Is.False);
            Assert.That(MapOn(InputMaps.Tavern), Is.True, "the same controls as before");
            yield return null;
            Assert.That(PauseMenu.Pausable, Is.True, "free to pause again");
        }

        [UnityTest]
        public IEnumerator ThePauseMenu_WaitsForWhateverOwnsEscape()
        {
            yield return Daytime();
            // A conversation.
            Assert.That(StoryServices.Conversations.Play(OpeningRules.FirstMorning), Is.True);
            yield return Frames(2);
            Assert.That(PauseMenu.Pausable, Is.False, "talking");
            DialogueManager.StopConversation();
            yield return Frames(3);
            // Decorate Mode.
            DecorateMode.Instance.Enter();
            yield return Frames(3);
            Assert.That(PauseMenu.Pausable, Is.False, "decorating (Esc cancels, Start is the catalog)");
            DecorateMode.Instance.Leave();
            yield return Frames(3);
            // A story scene.
            var scene = new object();
            PauseRules.Block(scene);
            Assert.That(PauseMenu.Pausable, Is.False, "a story scene");
            PauseRules.Unblock(scene);
            yield return Frames(2);
            Assert.That(PauseMenu.Pausable, Is.True, "free again");
            // Prep is the evening's own screen: the pause menu is there too.
            yield return Evening();
            yield return Frames(2);
            Assert.That(PauseMenu.Pausable, Is.True, "Prep");
        }

        // ---------- quitting, phase by phase ----------

        [UnityTest]
        public IEnumerator QuittingTheDay_Saves_AndContinueComesBackToTheSameMoment()
        {
            yield return Daytime();
            Flow.State.Surface.Restore(10 * 60 + 30);
            int gold = Flow.State.Gold;
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            Assert.That(Pause.IsConfirming, Is.False, "the day is a resume point: no question");
            yield return BackAtTheMenu();
            Assert.That(Flow.InGame, Is.False);
            Assert.That(MenuPause.IsPaused, Is.False);
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Daytime)));
            Assert.That(Saved.day, Is.EqualTo(2));
            Assert.That(Flow.Continue(), Is.True);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime, 30f, "the day again");
            yield return Revealed();
            Assert.That(Flow.State.Surface.WholeMinute, Is.EqualTo(10 * 60 + 30), "the same minute");
            Assert.That(Flow.State.Gold, Is.EqualTo(gold));
            Assert.That(Pause.IsOpen, Is.False);
            Assert.That(MapOn(InputMaps.Tavern), Is.True, "the keeper walks again");
        }

        [UnityTest]
        public IEnumerator QuittingDuringPrep_AsksFirst_AndContinueIsTheDayBeforeTheEvening()
        {
            yield return Daytime();
            Flow.State.Surface.Restore(16 * 60);
            yield return Evening();
            Director.FillStoreroom();
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            yield return null;
            Assert.That(Pause.IsConfirming, Is.True, "the evening isn't saved part-way: it asks");
            Assert.That(Pause.ConfirmKey, Is.EqualTo(MenuLocKeys.QuitEvening));
            // Back first: nothing happens.
            Pause.ConfirmNo.onClick.Invoke();
            yield return null;
            Assert.That(Pause.IsConfirming, Is.False);
            Assert.That(Flow.InGame, Is.True);
            Pause.QuitMenuButton.onClick.Invoke();
            Pause.ConfirmYes.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Daytime)), "the last save: the day as it was left");
            Assert.That(Saved.surface.minute, Is.EqualTo(16 * 60), "the minute the evening began");
        }

        [UnityTest]
        public IEnumerator QuittingDuringService_AsksFirst_AndBanksNothing()
        {
            yield return Daytime();
            yield return Evening();
            int gold = Flow.State.Gold;
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Service, 10f, "service");
            yield return Frames(3);
            Assert.That(PauseMenu.Pausable, Is.True, "on foot in service");
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            Assert.That(Pause.ConfirmKey, Is.EqualTo(MenuLocKeys.QuitEvening));
            Pause.ConfirmYes.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Daytime)));
            Assert.That(Saved.gold, Is.EqualTo(gold), "nothing banked");
        }

        [UnityTest]
        public IEnumerator QuittingAtTheResults_BanksTheEveningOnce_AndContinueIsTheNightsDelve()
        {
            yield return Daytime();
            yield return Evening();
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Service, 10f, "service");
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            yield return Frames(3);
            Assert.That(PauseMenu.Pausable, Is.True, "the results are the evening's own screen");
            int expected = Flow.State.Gold + Director.Session.Ledger.Gold + Director.Session.Ledger.Tips;
            int day = Flow.State.Day;
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            Assert.That(Pause.IsConfirming, Is.False, "banked as closing up would bank it: no question");
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Delve)), "Continue begins the night's delve");
            Assert.That(Saved.gold, Is.EqualTo(expected), "the takings, once");
            Assert.That(Saved.day, Is.EqualTo(day));
            Assert.That(Flow.Continue(), Is.True);
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            Assert.That(Flow.State.Gold, Is.EqualTo(expected), "not banked twice");
        }

        [UnityTest]
        public IEnumerator QuittingMidDelve_AsksFirst_AbandonsIt_AndContinueBeginsItAgain()
        {
            yield return Daytime();
            yield return Evening();
            Flow.SkipService();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return Revealed();
            yield return Frames(5);
            int gold = Flow.State.Gold;
            Assert.That(PauseMenu.Pausable, Is.True, "on foot in the Hollows");
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            Assert.That(Pause.ConfirmKey, Is.EqualTo(MenuLocKeys.QuitDelve));
            Pause.ConfirmYes.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Delve)), "the delve's start is the save");
            Assert.That(Saved.gold, Is.EqualTo(gold), "nothing of the abandoned delve");
            Assert.That(Flow.Continue(), Is.True);
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the night's delve again");
        }

        [UnityTest]
        public IEnumerator QuittingAtTheDelvesResult_BringsItHome_AndContinueIsTheNight()
        {
            yield return Daytime();
            yield return Evening();
            Flow.SkipService();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return Revealed();
            Flow.DebugSkipPhase();
            yield return WaitUntil(() => Object.FindAnyObjectByType<DelveResultScreen>() is { IsOpen: true }, 10f, "the delve's result");
            yield return Frames(3);
            Assert.That(PauseMenu.Pausable, Is.True, "the delve's result is the moment itself");
            Assert.That(PauseMenu.CurrentPlan().Kind, Is.EqualTo(QuitKind.BankDelve));
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Night)), "brought home: Continue is the night");
            Assert.That(Flow.Continue(), Is.True);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
        }

        [UnityTest]
        public IEnumerator QuittingAtNight_Saves_AndContinueIsTheNight()
        {
            yield return Daytime();
            yield return Evening();
            Flow.SkipService();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return Revealed();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            yield return Frames(3);
            Assert.That(PauseMenu.Pausable, Is.True, "the night's screen");
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.phase, Is.EqualTo(nameof(DayPhase.Night)));
        }

        [UnityTest]
        public IEnumerator QuittingOnArrivalDay_AsksFirst_AndArrivalDayBeginsAgain()
        {
            yield return Menu();
            Flow.NewGame(new PlayerProfile { name = "Wren" });
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Arrival, 30f, "arrival day");
            yield return Revealed();
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 5f, "Orik's welcome");
            DialogueManager.StopConversation();
            yield return Frames(5);
            Assert.That(PauseMenu.CurrentPlan().Kind, Is.EqualTo(QuitKind.RestartArrival));
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            Assert.That(Pause.ConfirmKey, Is.EqualTo(MenuLocKeys.QuitArrival));
            Pause.ConfirmYes.onClick.Invoke();
            yield return BackAtTheMenu();
            Assert.That(Saved.story.openingStage, Is.EqualTo(nameof(OpeningStage.Arrival)));
            Assert.That(Saved.story.seenHints, Has.No.Member("beat:arrival"), "the welcome plays again");
        }

        // ---------- the first free morning ----------

        [UnityTest]
        public IEnumerator TheFirstMorning_OrikAndBoogSpeakOnce_AsTheKeeperComesDownstairs()
        {
            yield return Daytime(OpeningStage.FirstEvening);
            Assert.That(DialogueManager.IsConversationActive, Is.False, "nothing upstairs: it waits for them to come down");
            AreaPassage down = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).Single(p => p.To != null && p.To.Id == PropertyArea.TavernId);
            down.Pass(Keeper);
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 5f, "the first morning");
            Assert.That(DialogueManager.lastConversationStarted, Is.EqualTo(OpeningRules.FirstMorning));
            Assert.That(Flow.State.Story.SeenHints, Has.Member("beat:first_morning"));
            DialogueManager.StopConversation();
            yield return Frames(3);
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.FirstEvening), "the opening carries on to the first evening");
            AreaPassage up = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).Single(p => p.To != null && p.To.Id == PropertyArea.GuestRoomId);
            up.Pass(Keeper);
            yield return new WaitForSecondsRealtime(1f);
            down.Pass(Keeper);
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(DialogueManager.IsConversationActive, Is.False, "once");
        }

        [UnityTest]
        public IEnumerator AfterTheOpening_ComingDownstairs_IsQuiet()
        {
            yield return Daytime(OpeningStage.Complete);
            AreaPassage down = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).Single(p => p.To != null && p.To.Id == PropertyArea.TavernId);
            down.Pass(Keeper);
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(DialogueManager.IsConversationActive, Is.False);
        }

        [UnityTest]
        public IEnumerator TheGarden_MarketAndMenuBoardPrompts_ShowOnceEach()
        {
            yield return Daytime(OpeningStage.FirstEvening);
            SurfacePrompts prompts = Object.FindAnyObjectByType<SurfacePrompts>();
            Assert.That(prompts, Is.Not.Null);
            foreach (TavernInteractableKind kind in new[] { TavernInteractableKind.GardenBed, TavernInteractableKind.MarketStall, TavernInteractableKind.MenuBoard })
            {
                int before = prompts.ShownCount;
                EventBus<TavernInteractHint>.Publish(new TavernInteractHint(true, TavernHint.Use("x"), kind));
                Assert.That(prompts.ShownCount, Is.EqualTo(before + 1), $"{kind}: the first time");
                Assert.That(Flow.State.Story.SeenHints, Has.Member(SurfacePrompts.PromptFor(kind)));
                EventBus<TavernInteractHint>.Publish(new TavernInteractHint(true, TavernHint.Use("x"), kind));
                Assert.That(prompts.ShownCount, Is.EqualTo(before + 1), $"{kind}: never again");
            }
            EventBus<TavernInteractHint>.Publish(new TavernInteractHint(true, TavernHint.Use("x"), TavernInteractableKind.Grill));
            Assert.That(prompts.ShownCount, Is.EqualTo(3), "nothing for anything else");
        }

        // ---------- the main menu and saves ----------

        [UnityTest]
        public IEnumerator TheMainMenu_HasTheControls_TheVersion_AndQuitOnDesktop()
        {
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.ControlsButton, Is.Not.Null);
            Assert.That(menu.QuitButton.gameObject.activeSelf, Is.EqualTo(PauseMenu.CanQuitApplication));
            menu.ControlsButton.onClick.Invoke();
            yield return null;
            Assert.That(menu.ControlsPage.IsOpen, Is.True);
            Assert.That(menu.ContinueButton.gameObject.activeInHierarchy, Is.False, "the page covers the menu");
            menu.ControlsPage.Show(3);
            Assert.That(menu.ControlsPage.Page, Is.EqualTo(3));
            menu.ControlsPage.Show(5);
            Assert.That(menu.ControlsPage.Page, Is.EqualTo(0), "the pages wrap");
            Assert.That(Application.version, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator AnUnreadableSave_SaysSo_OffersTheBackup_AndNewGameStillAsks()
        {
            yield return Daytime();
            Flow.Save();
            int day = Saved.day;
            File.WriteAllText(Path.Combine(m_SaveDir, SaveStore.FileName), "{ damaged");
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            yield return Frames(3);
            Assert.That(menu.MessageKey, Is.EqualTo(MenuLocKeys.SaveUnreadableBackup));
            Assert.That(menu.ContinuesFromBackup, Is.True);
            Assert.That(menu.ContinueButton.gameObject.activeSelf, Is.True);
            menu.NewGameButton.onClick.Invoke();
            Assert.That(menu.IsConfirming, Is.True, "a damaged save is still asked about");
            menu.ConfirmNo.onClick.Invoke();
            menu.ContinueButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.InGame && Director != null, 30f, "the backup loaded");
            Assert.That(Flow.State.Day, Is.EqualTo(day));
            Flow.Save();
            Assert.That(Flow.CheckSave().Usable, Is.True, "saving again mends it");
            Assert.That(File.Exists(Path.Combine(m_SaveDir, SaveStore.UnreadableFileName)), Is.True, "the damaged one kept aside");
        }

        [UnityTest]
        public IEnumerator ASaveFromANewerVersion_SaysSo_AndIsNeverLoaded()
        {
            Directory.CreateDirectory(m_SaveDir);
            string newer = SaveSystem.ToJson(SaveSystem.Capture(new GameState(5, DayPhase.Daytime)))
                .Replace($"\"version\": {SaveSystem.CurrentVersion}", "\"version\": 999");
            File.WriteAllText(Path.Combine(m_SaveDir, SaveStore.FileName), newer);
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            yield return Frames(3);
            Assert.That(menu.MessageKey, Is.EqualTo(MenuLocKeys.SaveNewer));
            Assert.That(menu.ContinueButton.gameObject.activeSelf, Is.False);
            menu.NewGameButton.onClick.Invoke();
            Assert.That(menu.IsConfirming, Is.True, "replacing it is asked about");
        }

        [UnityTest]
        public IEnumerator Saving_ShowsTheSavedMark_Briefly()
        {
            yield return Daytime();
            var mark = Object.FindAnyObjectByType<SaveIndicator>();
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(mark.IsShowing, Is.False);
            Flow.Save();
            yield return null;
            Assert.That(mark.IsShowing, Is.True);
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(mark.IsShowing, Is.False, "gone again");
        }

        // ---------- the new screens at 320x180 (run by hand: BatchLogs/4ia/*.png) ----------

        [UnityTest, Explicit]
        public IEnumerator CaptureFirstImpressions()
        {
            const string out_ = "BatchLogs/4ia";
            Directory.CreateDirectory(out_);
            yield return Menu();
            yield return new WaitForSecondsRealtime(0.5f);
            TavernEveningCaptures.Capture($"{out_}/menu_new.png");
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.ControlsButton.onClick.Invoke();
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/menu_controls_day.png");
            menu.ControlsPage.Show(3);
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/menu_controls_decorate.png");

            yield return Daytime(OpeningStage.FirstEvening);
            Flow.Save();
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/saved_mark.png");
            Pause.Open();
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/pause.png");
            Pause.ControlsButton.onClick.Invoke();
            Pause.ControlsPage.Show(2);
            InputDevices.Set(InputDeviceKind.Gamepad);
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/pause_controls_hollows_pad.png");
            InputDevices.Set(InputDeviceKind.KeyboardMouse);
            Pause.Close();
            yield return Frames(2);
            EventBus<TavernInteractHint>.Publish(new TavernInteractHint(true, TavernHint.Use("x"), TavernInteractableKind.GardenBed));
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/prompt_garden.png");
            AreaPassage down = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).Single(p => p.To != null && p.To.Id == PropertyArea.TavernId);
            down.Pass(Keeper);
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 5f, "the first morning");
            yield return new WaitForSecondsRealtime(2f);
            TavernEveningCaptures.Capture($"{out_}/first_morning.png");
            DialogueManager.StopConversation();
            yield return Frames(3);
            yield return Evening();
            Pause.Open();
            Pause.QuitMenuButton.onClick.Invoke();
            yield return Frames(2);
            TavernEveningCaptures.Capture($"{out_}/pause_quit_evening.png");
            Pause.ConfirmNo.onClick.Invoke();
            Pause.Close();
        }

        // ---------- prompts name the device in use ----------

        [UnityTest]
        public IEnumerator Prompts_NameTheDeviceInUse()
        {
            yield return Daytime();
            InputDevices.Set(InputDeviceKind.KeyboardMouse);
            Assert.That(InputHints.TavernInteract(), Does.Contain("E"));
            Assert.That(InputHints.Binding(InputMaps.Minigame, MinigameActions.Action, compact: true), Is.EqualTo("Space"));
            Assert.That(InputHints.Binding(InputMaps.Minigame, MinigameActions.Aim, compact: true), Does.Not.Contain("Arrow"), "one way to aim, not every alternative");
            InputDevices.Set(InputDeviceKind.Gamepad);
            Assert.That(InputHints.TavernInteract(), Is.EqualTo("A"));
            Assert.That(InputHints.Binding(InputMaps.Minigame, MinigameActions.Cancel, compact: true), Is.EqualTo("B"));
        }
    }
}
