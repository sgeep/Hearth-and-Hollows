using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Quests;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using SaveSystem = Hearthdelve.Shared.Save.SaveSystem;
using LevelManager = MoreMountains.TopDownEngine.LevelManager;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4g Checkpoint B through the real game (saves in a temp folder): New Game → the keeper's creator → the Act I opening (the
    /// arrival, the typewriter, the hatch, the first delve's prompts, the homecoming, the first evening and its takings) → Boog's
    /// Bomb (declined, accepted, found, lost to a death, found again, brought home, handed over) → save, quit and Continue; and an
    /// old save that skips it all.
    /// </summary>
    public class StoryCheckpointBTests : LookTestFixture
    {
        const int k_Seed = 20261004;

        string m_SaveDir;
        Gamepad m_Pad;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static StoryHost Host => StoryHost.Instance;
        static HearthDialogueUI Box => Object.FindAnyObjectByType<HearthDialogueUI>();
        static RoomRunner Runner => RoomRunner.Active;
        static string Selected => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : null;

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            RoomRunner.SeedOverride = k_Seed;
            m_Pad = InputSystem.AddDevice<Gamepad>();
        }

        [TearDown]
        public void ClearSaves()
        {
            InputSystem.RemoveDevice(m_Pad);
            GameFlow.SaveDirectoryOverride = null;
            RoomRunner.SeedOverride = 0;
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        SaveData SavedGame() => SaveSystem.FromJson(File.ReadAllText(Path.Combine(m_SaveDir, SaveStore.FileName)));

        // ---------- The way through the game ----------

        IEnumerator BootToMenu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady && Host != null && Host.Relationships != null, 10f, "the string tables and the story");
            // As a player would: the menu's fade from black is over (until then the cover takes the pointer).
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 5f, "the menu revealed");
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase, string what)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, what);
            yield return WaitUntil(() => !(Flow.Transition is TransitionScreen t && t.IsCovering), 5f, "the scene revealed");
            yield return null;
        }

        IEnumerator InDelve()
        {
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0 && Runner != null && Runner.Current != null, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return null;
        }

        IEnumerator QuickGameToTheNight()
        {
            yield return BootToMenu();
            Flow.QuickNewGame();
            yield return InDelve();
            yield return ExtractAndGoHome();
        }

        IEnumerator ExtractAndGoHome(Action<DelveResultScreen> check = null)
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "extracted");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            check?.Invoke(result);
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night, home again");
        }

        /// <summary>Night → sleep → the daytime → the evening, kept shut → the night's delve.</summary>
        IEnumerator NextDelve()
        {
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next day");
            Director.OpenForEvening();
            yield return InTavern(TavernPhase.Prep, "the evening");
            Director.CloseForTheNight();
            yield return InDelve();
        }

        // ---------- Talking ----------

        IEnumerator Press(Key key)
        {
            Hold(key);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            yield return null;
        }

        IEnumerator PressPad(GamepadButton button)
        {
            InputSystem.QueueStateEvent(m_Pad, new GamepadState().WithButton(button));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(m_Pad, new GamepadState());
            yield return null;
            yield return null;
        }

        IEnumerator ClickAt(Vector2 position)
        {
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = position });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = position });
            yield return null;
        }

        /// <summary>Where a UI element is on screen, whatever its canvas draws with (the menu's has a camera; the dialogue box's doesn't).</summary>
        static Vector2 Centre(Transform t)
        {
            var rect = (RectTransform)t;
            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        /// <summary>One press finishes the reveal, the next moves on (Space).</summary>
        IEnumerator NextLine()
        {
            if (Box.IsRevealing) yield return Press(Key.Space);
            Assert.That(Box.IsRevealing, Is.False, "the reveal finished on the first press");
            yield return Press(Key.Space);
        }

        /// <summary>Lines until a choice (or the end).</summary>
        IEnumerator ToChoice()
        {
            for (int i = 0; i < 12 && Box.IsOpen && !Box.IsChoosing; i++) yield return NextLine();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "a choice");
        }

        IEnumerator Choose(string text)
        {
            int index = Enumerable.Range(0, Box.ResponseCount).FirstOrDefault(i => Box.ChoiceText(i) == text);
            Assert.That(Box.ChoiceText(index), Is.EqualTo(text), $"the choice \"{text}\" among {string.Join(", ", Enumerable.Range(0, Box.ResponseCount).Select(Box.ChoiceText))}");
            yield return WaitUntil(() => !Box.ChoicesLocked, 2f, "the choices unlocked");
            Box.Choose(index);
            yield return null;
            yield return null;
        }

        IEnumerator UntilClosed()
        {
            for (int i = 0; i < 16 && Box.IsOpen; i++)
            {
                if (Box.IsChoosing) Assert.Fail($"a choice waits: {Box.ChoiceText(0)}");
                yield return NextLine();
            }
            Assert.That(Box.IsOpen, Is.False, "the conversation ended");
        }

        IEnumerator Talk(string characterId)
        {
            Assert.That(StoryServices.Conversations.Talk(characterId), $"talking to {characterId}");
            yield return null;
            yield return null;
            Assert.That(Box.IsOpen, "the box is open");
        }

        static void ListenAsAtEndOfFrame(string questId)
        {
            PixelCrushers.QuestMachine.Quest quest = Host.Quests.Journal.FindQuest(questId);
            foreach (PixelCrushers.QuestMachine.QuestNode node in quest.nodeList.Where(n => n.GetState() == PixelCrushers.QuestMachine.QuestNodeState.Active))
            foreach (var condition in node.conditionSet.conditionList.OfType<PixelCrushers.QuestMachine.MessageQuestCondition>())
                MessageSystem.AddListener(condition, condition.runtimeMessage, condition.runtimeParameter);
        }

        // ---------- Step 4: the creator ----------

        [UnityTest]
        public IEnumerator NewGame_MakesTheKeeper_ByKeyboardGamepadAndMouse_AndTheKeeperLooksAsMadeInPlay()
        {
            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            CharacterCreatorScreen creator = menu.Creator;
            menu.NewGameButton.onClick.Invoke();
            yield return null;
            Assert.That(creator.IsOpen && !menu.IsConfirming, "New Game opens the creator (nothing to replace, no question)");
            Assert.That(Selected, Is.EqualTo("Row name"), "the name has the focus");
            Assert.That((creator.ValueText(CharacterCreatorScreen.NameRow), creator.ValueText(CharacterCreatorScreen.BodyRow)), Is.EqualTo(("Bram", "townsfolk")));

            // The name: Enter opens it, typing replaces it, the letters (by gamepad) add to it, Enter keeps it.
            yield return Press(Key.Enter);
            Assert.That(creator.IsNaming);
            for (int i = 0; i < 4; i++) yield return Press(Key.Backspace);
            foreach (char c in "Wren") InputSystem.QueueTextEvent(Keys, c);
            yield return null;
            yield return null;
            Assert.That(creator.TypedName, Is.EqualTo("Wren"));
            yield return PressPad(GamepadButton.DpadRight);
            Assert.That(Selected, Does.StartWith("Key "), "a gamepad brings the letters back");
            yield return PressPad(GamepadButton.South);
            Assert.That(creator.TypedName.Length, Is.EqualTo(5), "A types the chosen letter");
            yield return Press(Key.Backspace);
            yield return Press(Key.Enter);
            Assert.That(creator.IsNaming, Is.False);
            Assert.That(creator.ValueText(CharacterCreatorScreen.NameRow), Is.EqualTo("Wren"));
            Assert.That(Selected, Is.EqualTo("Row name"));

            // The look, by keyboard: down a row, right twice (townsfolk → warrior → dwarf).
            Sprite before = creator.PreviewSprite;
            yield return Press(Key.DownArrow);
            Assert.That(Selected, Is.EqualTo("Row body"));
            yield return Press(Key.RightArrow);
            Assert.That(creator.ValueText(CharacterCreatorScreen.BodyRow), Is.EqualTo("warrior"));
            yield return Press(Key.RightArrow);
            Assert.That((creator.Profile.body, creator.ValueText(CharacterCreatorScreen.BodyRow)), Is.EqualTo(("dwarf", "dwarf")));
            Assert.That(Selected, Is.EqualTo("Row body"), "left and right change the row, never the focus");
            Assert.That(creator.ValueText(CharacterCreatorScreen.SkinRow), Is.EqualTo("rosy"), "the dwarf as drawn");

            // The skin, by gamepad.
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(Selected, Is.EqualTo("Row skin"));
            yield return PressPad(GamepadButton.DpadRight);
            Assert.That(creator.ValueText(CharacterCreatorScreen.SkinRow), Is.Not.EqualTo("rosy"));

            // The hair, by mouse (the row's arrow).
            string hair = creator.ValueText(CharacterCreatorScreen.HairRow);
            Assert.That(hair, Is.EqualTo("golden"));
            Vector2 arrowAt = Centre(creator.RowFor(CharacterCreatorScreen.HairRow).right.transform);
            yield return ClickAt(arrowAt);
            Assert.That(creator.ValueText(CharacterCreatorScreen.HairRow), Is.Not.EqualTo(hair));
            yield return null;
            Assert.That(creator.PreviewSprite, Is.Not.Null.And.Not.SameAs(before), "the preview shows the keeper as chosen");
            Assert.That(creator.PreviewSprite.texture.name, Does.Contain("keeper_hair"), "recoloured, not tinted");
            string palette = creator.Profile.palette;

            // Begin: Tally Ho!, arrival day.
            yield return ClickAt(Centre(creator.BeginButton.transform));
            yield return InTavern(TavernPhase.Arrival, "the arrival");
            PlayerProfile keeper = Flow.State.Story.Player;
            Assert.That((keeper.name, keeper.body, keeper.palette), Is.EqualTo(("Wren", "dwarf", palette)));
            Assert.That((Flow.State.Story.CreationComplete, Flow.State.Story.Opening, Flow.State.Day, Flow.Phase), Is.EqualTo((true, OpeningStage.Arrival, 1, DayPhase.Daytime)));
            SaveData saved = SavedGame();
            Assert.That((saved.version, saved.story.player.name, saved.story.player.body, saved.story.player.palette, saved.story.openingStage, saved.story.creationComplete),
                Is.EqualTo((SaveSystem.CurrentVersion, "Wren", "dwarf", palette, "Arrival", true)), "saved at once");

            // In play: the dwarf, recoloured; the name reaches dialogue through HH_PlayerName.
            var appearance = Object.FindObjectsByType<KeeperAppearance>().Single(a => a.GetComponentInParent<Character>() != null);
            CharacterSpriteAnimator animator = appearance.GetComponent<CharacterSpriteAnimator>();
            Assert.That(animator.Set.name, Does.StartWith("Anim_KeeperDwarf").And.Contain("keeper_hair"));
            Assert.That(Lua.Run("return HH_PlayerName()").asString, Is.EqualTo("Wren"));
            yield return WaitUntil(() => Box.IsOpen, 3f, "the arrival's conversation");
            Assert.That(Box.Line, Does.StartWith("ahh, you must be Wren! Phi's letter"), "the owner's line");
            DialogueManager.StopAllConversations();
        }

        // ---------- Step 5: the opening ----------

        [UnityTest]
        public IEnumerator TheArrival_TheTypewriter_TheHatch_TheFirstDelvesPrompts_AndTheHomecoming()
        {
            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.NewGameButton.onClick.Invoke();
            yield return null;
            menu.Creator.BeginButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Arrival, "the arrival");

            // The arrival: Orik, a line revealed as it's spoken.
            yield return WaitUntil(() => Box.IsOpen, 3f, "Orik");
            Assert.That((Box.SpeakerName, Box.Line), Is.EqualTo(("Orik", "ahh, you must be Bram! Phi's letter said you'd come. it didnae say you'd be this late.")));
            Assert.That(Box.IsRevealing, "revealed as it's spoken");
            Assert.That(Box.CharactersPerSecond, Is.EqualTo(AssetDatabaseFree.Settings.charactersPerSecond), "one tunable speed");
            // A click while revealing finishes the line, and doesn't move on.
            Vector2 boxAt = Centre(Object.FindAnyObjectByType<DialogueBoxClick>().transform);
            Assert.That(Flow.Transition.IsCovering, Is.False, "the conversation began once the scene was revealed");
            yield return ClickAt(boxAt);
            yield return null;
            yield return null;
            Assert.That((Box.IsRevealing, Box.IsOpen, Box.IsChoosing), Is.EqualTo((false, true, false)), "the whole line, still there");
            Assert.That(Box.Line, Does.StartWith("ahh, you must be Bram"));

            // Holding the confirm that moves on brings up the choices, but can't take one.
            Hold(Key.Enter);
            yield return null;
            yield return null;
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            for (int i = 0; i < 6; i++) yield return null;
            Assert.That((Box.IsChoosing, Box.ChoicesLocked), Is.EqualTo((true, true)), "the held press doesn't choose");
            ReleaseKeys();
            yield return null;
            yield return null;
            Assert.That((Box.IsChoosing, Box.ChoicesLocked), Is.EqualTo((true, false)), "let go: the choices are open");
            Assert.That((Box.ChoiceText(0), Box.ChoiceText(1)), Is.EqualTo(("the road was long.", "where is Phi'rai?")));
            yield return Press(Key.DownArrow);
            yield return Press(Key.Enter);
            yield return WaitUntil(() => !Box.IsChoosing, 2f, "the choice taken");
            Assert.That(Box.Line, Is.EqualTo("aye, that's the question, isn't it."));
            // The next press (gamepad A) finishes the reveal, the one after moves on.
            yield return PressPad(GamepadButton.South);
            Assert.That(Box.Line, Is.EqualTo("aye, that's the question, isn't it."));
            yield return PressPad(GamepadButton.South);
            Assert.That(Box.Line, Does.StartWith("nine days ago"));
            yield return ToChoice();
            Assert.That(Box.ChoiceText(0), Is.EqualTo("i'll go down."));
            yield return Choose("i'll go down.");
            yield return WaitUntil(() => !Box.IsOpen, 2f, "the arrival over");
            Assert.That(Flow.State.Story.SeenHints, Does.Contain("beat:arrival"));

            // On foot: the hatch, used like a station.
            var hatch = Object.FindAnyObjectByType<OpeningHatch>();
            Assert.That(hatch.IsOpen, "the hatch is there on arrival day");
            PropertyArea room = PropertyArea.All.First(a => a.Id == PropertyArea.GuestRoomId);
            Assert.That(room.Bounds.Contains(Vector2Int.FloorToInt((Vector2)hatch.transform.position - room.Origin)), "in the keeper's room upstairs (2026-10-07)");
            var keeper = Object.FindAnyObjectByType<TavernInteractor>();
            Teleport(keeper, hatch.Interactable.UsePoint);
            yield return null;
            yield return null;
            Assert.That(keeper.Target, Is.SameAs(hatch.Interactable), "the hatch is the keeper's target");
            yield return Press(Key.E);
            yield return InDelve();
            Assert.That((Flow.State.Day, Flow.Phase, Flow.State.Story.Opening), Is.EqualTo((1, DayPhase.Delve, OpeningStage.FirstDelve)));

            // The first delve's prompts: one at a time, once each.
            var prompts = Object.FindAnyObjectByType<OnboardingPrompts>();
            yield return WaitUntil(() => prompts.Showing == OnboardingHints.Move, 3f, "the move prompt");
            Assert.That(prompts.ShownText, Does.StartWith("move with").And.Not.Contain("{"));
            EventBus<Hearthdelve.Shared.Run.EssenceChanged>.Publish(new Hearthdelve.Shared.Run.EssenceChanged(40f, 50f, false, true));
            yield return WaitUntil(() => prompts.Showing == OnboardingHints.Essence, 6f, "the Essence prompt");
            Assert.That(prompts.ShownText, Does.StartWith("that was Essence"));
            Assert.That(Flow.State.Story.SeenHints, Is.SupersetOf(new[] { OnboardingHints.Move, OnboardingHints.Essence }));

            // Home with nothing: Boog is glad anyway; Orik counts.
            yield return ExtractAndGoHome();
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Homecoming));
            yield return WaitUntil(() => Box.IsOpen, 3f, "the homecoming");
            Assert.That((Box.SpeakerName, Box.Line), Is.EqualTo(("Boog", "you're back! that's the important part. the other important part was food.")));
            yield return UntilClosed();
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.FirstEvening));
            OpeningHatch after = Object.FindAnyObjectByType<OpeningHatch>();
            Assert.That(after.IsOpen, "it stays in the floor of the keeper's room");
            Assert.That(after.Interactable.IsAvailable, Is.False, "but it's the way down only on arrival day (a look in the daytime)");
        }

        /// <summary>The settings asset as the box reads it (via the box, so the test needs no editor API).</summary>
        static class AssetDatabaseFree
        {
            public static DialogueSettings Settings => Resources.FindObjectsOfTypeAll<DialogueSettings>().Single();
        }

        [UnityTest]
        public IEnumerator TheFirstEvening_TeachesTheLoop_TheTakingsEndTheOpening_AndBoogAsksAboutHisBomb_AcrossAContinue()
        {
            yield return QuickGameToTheNight();
            // As if home from the opening's first delve (its homecoming played).
            Flow.State.Story.Opening = OpeningStage.FirstEvening;
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "day 2");
            Assert.That(SavedGame().story.openingStage, Is.EqualTo("FirstEvening"), "saved with the day");

            // Quit and Continue: no creator, no arrival; the opening goes on where it was.
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "day 2, continued");
            Assert.That(Object.FindAnyObjectByType<MainMenuScreen>(), Is.Null);
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.FirstEvening));

            Director.OpenForEvening();
            yield return InTavern(TavernPhase.Prep, "the first evening");
            yield return WaitUntil(() => Box.IsOpen, 3f, "Orik at the board");
            Assert.That(Box.Line, Does.StartWith("the board's yours"));
            yield return UntilClosed();
            Assert.That(Flow.State.Story.SeenHints, Does.Contain("beat:first_evening"));

            // An evening's service, cut short, to its takings.
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return null;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "the takings");
            yield return WaitUntil(() => Box.IsOpen, 3f, "Orik's verdict");
            Assert.That(Box.Line, Does.StartWith("that's the first night's takings"));
            yield return NextLine();
            yield return NextLine();
            Assert.That((Box.SpeakerName, Box.Line), Is.EqualTo(("Boog", "keeper. can i ask you something? a small thing. a medium thing.")));
            yield return NextLine();
            Assert.That(Box.Line, Is.EqualTo("i lost something in the Hollows. my favorite bomb."), "into Boog/Bomb");
            yield return ToChoice();
            yield return Choose("where did you lose her?");
            Assert.That(Box.Line, Does.StartWith("in the Cellars, first floor"));
            yield return ToChoice();
            yield return Choose("i'll bring her back.");
            Assert.That(Host.Quests.State("boogs_bomb"), Is.EqualTo("active"), "Quest Machine: under way");
            Assert.That(Flow.State.QuestObjects.Status("boogs_bomb"), Is.EqualTo(QuestObjectStatus.Wanted), "and the Hollows will hold her");
            yield return UntilClosed();
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Complete), "the opening is over");
            Director.FinishEvening();
            yield return InDelve();
            SaveData saved = SavedGame();
            Assert.That((saved.story.openingStage, saved.questObjects.Single().id, saved.questObjects.Single().status), Is.EqualTo(("Complete", "boogs_bomb", "Wanted")));
            Assert.That(saved.story.quests, Does.Contain("boogs_bomb"));
        }

        // ---------- Step 6: Boog's Bomb ----------

        RoomInstance Room => Runner.Current;
        FloorNode Node => Runner.Node;
        FloorNode Target(RoomExit exit) => Runner.Floor.Node(Node.Next[exit.Index]);
        RoomExit ExitTo(Func<FloorNode, bool> wanted) => Room.Exits.Where(e => e.Index < Node.Next.Count).FirstOrDefault(e => wanted(Target(e)));

        IEnumerator TakeExit(RoomExit exit)
        {
            Assert.That(exit, Is.Not.Null, $"{Node.RoomId}: an exit to take");
            int entered = Runner.RoomsEntered;
            Teleport(Player, (Vector2)exit.transform.position + new Vector2(0f, 0.3f));
            yield return WaitUntil(() => Runner.RoomsEntered == entered + 1 && !Runner.IsTransitioning, 5f, "the next room");
            FreezeEnemies();
            yield return null;
        }

        IEnumerator ClearRoom()
        {
            foreach (EnemyIdentity enemy in Room.GetComponentsInChildren<EnemyIdentity>())
            {
                var health = enemy.GetComponent<Health>();
                health.Damage(health.CurrentHealth + 100f, Player.gameObject, 0f, 0f, Vector3.zero);
            }
            yield return WaitUntil(() => Runner.Encounter.IsCleared, 5f, "the room to clear");
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>Fights through the first floor until the bomb lies on the floor (the second fight cleared).</summary>
        IEnumerator FightToTheBomb()
        {
            Player.GetComponent<EssenceHealth>().GodMode = true;
            FreezeEnemies();
            QuestObjectPickup bomb = null;
            for (int guard = 0; guard < 12 && bomb == null; guard++)
            {
                Assert.That(Runner.Floor.Floor, Is.EqualTo(1), "still on the first floor");
                if (Runner.Encounter.IsSealed) yield return ClearRoom();
                bomb = Object.FindAnyObjectByType<QuestObjectPickup>();
                if (bomb != null) break;
                yield return TakeExit(ExitTo(n => n.Kind == RoomKind.Combat) ?? ExitTo(n => n.Kind != RoomKind.Extraction));
            }
            Assert.That(bomb, Is.Not.Null, "the bomb lies in the second fight's room");
            Assert.That(bomb.ObjectId, Is.EqualTo("boogs_bomb"));
            yield return new WaitForSeconds(0.5f);
            Teleport(Player, bomb.transform.position);
            yield return WaitUntil(() => DelveRunController.Active.Loot.Carries("boogs_bomb"), 3f, "picked up");
            var feed = Object.FindAnyObjectByType<HarvestFeed>();
            Assert.That(feed.Lines[0].text.GetComponent<SuperTextMesh>().text, Is.EqualTo("found: Boog's bomb!"), "a moment: the find, by name");
            Assert.That(Player.GetComponent<Hearthdelve.Dungeon.Harvest.SatchelCarrier>().Satchel.Slots.All(s => s.IsEmpty), "no satchel slot");
        }

        [UnityTest]
        public IEnumerator BoogsBomb_DeclinedThenTaken_LostToADeath_FoundAgain_BroughtHome_AndHandedOver_AcrossSaveQuitAndContinue()
        {
            yield return QuickGameToTheNight();

            // Declined: the quest stays open to take.
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("the stove's hot"));
            yield return ToChoice();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Is.EqualTo("i lost something in the Hollows. my favorite bomb."));
            yield return ToChoice();
            yield return Choose("your favorite bomb?");
            yield return ToChoice();
            yield return Choose("not now.");
            Assert.That(Box.Line, Does.StartWith("she'll keep"));
            yield return UntilClosed();
            Assert.That((Host.Quests.State("boogs_bomb"), Flow.State.QuestObjects.Status("boogs_bomb")), Is.EqualTo(("unassigned", QuestObjectStatus.None)));

            // Taken, after asking (research; the private kind).
            yield return Talk(CharacterIds.Boog);
            yield return ToChoice();
            yield return Choose("about your bomb...");
            yield return ToChoice();
            yield return Choose("where did you lose her?");
            yield return ToChoice();
            yield return Choose("what do you need her for?");
            Assert.That(Box.Line, Is.EqualTo("research."));
            yield return ToChoice();
            yield return Choose("what research?");
            Assert.That(Box.Line, Does.StartWith("the private kind"));
            yield return ToChoice();
            yield return Choose("i'll bring her back.");
            yield return UntilClosed();
            Assert.That((Host.Quests.State("boogs_bomb"), Flow.State.QuestObjects.Status("boogs_bomb")), Is.EqualTo(("active", QuestObjectStatus.Wanted)));
            ListenAsAtEndOfFrame("boogs_bomb");
            yield return Talk(CharacterIds.Boog);
            yield return ToChoice();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Does.StartWith("any sign of her?"), "his reminder");
            DialogueManager.StopAllConversations();
            yield return null;

            // Found, then lost with a death: never in the Lockbox, the quest goes on.
            yield return NextDelve();
            yield return FightToTheBomb();
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var essence = Player.GetComponent<EssenceHealth>();
            essence.GodMode = false;
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            Assert.That(death.CuriosLostText, Is.EqualTo("lost with you: Boog's bomb"));
            death.Confirm.onClick.Invoke();
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            Assert.That(result.CuriosText, Is.EqualTo("lost: Boog's bomb. still down there somewhere"));
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "home, without her");
            Assert.That((Host.Quests.State("boogs_bomb"), Flow.State.QuestObjects.Status("boogs_bomb")), Is.EqualTo(("active", QuestObjectStatus.Wanted)), "death never fails it");

            // Found again on a later delve, and brought home.
            yield return NextDelve();
            yield return FightToTheBomb();
            yield return ExtractAndGoHome(r => Assert.That(r.CuriosText, Is.EqualTo("brought home: Boog's bomb")));
            Assert.That(Flow.State.QuestObjects.Status("boogs_bomb"), Is.EqualTo(QuestObjectStatus.Home));
            Assert.That(SavedGame().questObjects.Single().status, Is.EqualTo("Home"), "saved as soon as the delve ended");
            yield return WaitUntil(() => Host.Quests.Journal.FindQuest("boogs_bomb").GetNode("find").GetState() == PixelCrushers.QuestMachine.QuestNodeState.True,
                2f, "Quest Machine: she's home");
            ListenAsAtEndOfFrame("boogs_bomb");

            // Handed over: his conversation changes; the reward and the deed, once.
            int gold = Flow.State.Gold;
            float affinity = Host.Relationships.Affinity("gunta"), respect = Host.Relationships.Respect("gunta"), orik = Host.Relationships.Affinity("pip");
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("is that... you found her!"));
            yield return ToChoice();
            yield return Choose("so what's the research, Boog?");
            Assert.That(Box.Line, Does.StartWith("...she's the first thing i ever made"));
            yield return NextLine();
            Assert.That(Box.Line, Does.StartWith("Old Phi let me keep her"));
            yield return NextLine();
            Assert.That(Box.Line, Does.StartWith("here. for your trouble"));
            Assert.That(Flow.State.Gold, Is.EqualTo(gold + 60), "the reward, from Hearth & Hollows");
            Assert.That(Flow.State.QuestObjects.Status("boogs_bomb"), Is.EqualTo(QuestObjectStatus.Delivered));
            yield return UntilClosed();
            yield return WaitUntil(() => Host.Quests.State("boogs_bomb") == "successful", 2f, "Quest Machine: done");
            Assert.That(Host.Relationships.Remembers("gunta", "returned_boogs_bomb"), "Boog remembers it");
            Assert.That(Host.Relationships.Affinity("gunta"), Is.GreaterThan(affinity));
            Assert.That(Host.Relationships.Respect("gunta"), Is.GreaterThan(respect + 5f), "and respects it");
            Assert.That(Host.Relationships.Affinity("pip"), Is.EqualTo(orik), "only Boog learns of it");

            // Save, quit, Continue: all of it comes back, and nothing is given twice.
            Assert.That(SavedGame().questObjects.Single().status, Is.EqualTo("Delivered"));
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued");
            Assert.That((Host.Quests.State("boogs_bomb"), Flow.State.QuestObjects.Status("boogs_bomb"), Flow.State.Gold),
                Is.EqualTo(("successful", QuestObjectStatus.Delivered, gold + 60)));
            Assert.That(Host.Relationships.Remembers("gunta", "returned_boogs_bomb"));
            Assert.That(Lua.Run("return HH_DeliverQuestObject(\"boogs_bomb\")").asBool, Is.False, "never handed over twice");
            // Checkpoint C: he tells her about you, once; then she can still be asked about (the shelf).
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("i told her about you"));
            yield return UntilClosed();
            yield return Talk(CharacterIds.Boog);
            yield return ToChoice();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Does.StartWith("she's on the shelf over the stove now"));
            yield return UntilClosed();
            yield return Talk(CharacterIds.Orik);
            Assert.That(Box.Line, Is.EqualTo("Boog's bomb is home. i've put it in the incident book. in advance, mind."));
            yield return UntilClosed();
        }

        // ---------- Old saves ----------

        [UnityTest]
        public IEnumerator ACheckpointASave_Continues_WithoutTheCreatorOrTheOpening()
        {
            yield return QuickGameToTheNight();
            // Rewrite it as Checkpoint A wrote it (version 8): a new game then marked its opening "not complete".
            string path = Path.Combine(m_SaveDir, SaveStore.FileName);
            SaveData data = SavedGame();
            data.version = 8;
            string json = SaveSystem.ToJson(data);
            json = Regex.Replace(json, @",\s*""questObjects"":\s*\[\s*\]", string.Empty);
            json = Regex.Replace(json, @"""openingStage"":\s*""[^""]*"",\s*""creationComplete"":\s*\w+,\s*""seenHints"":\s*\[[^\]]*\],", string.Empty);
            json = json.Replace("\"openingComplete\": true", "\"openingComplete\": false");
            File.WriteAllText(path, json);

            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued");
            Assert.That((Flow.State.Story.Opening, Flow.State.Story.CreationComplete), Is.EqualTo((OpeningStage.Complete, true)));
            Assert.That(Flow.State.Story.Player.name, Is.EqualTo("Bram"));
            for (int i = 0; i < 90; i++) yield return null;
            Assert.That(Box.IsOpen, Is.False, "no homecoming for an old game");
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next day: the daytime, not an arrival");
            Assert.That(SavedGame().version, Is.EqualTo(SaveSystem.CurrentVersion));
        }
    }
}
