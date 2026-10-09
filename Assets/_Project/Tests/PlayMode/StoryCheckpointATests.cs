using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Story.Relationships;
using Hearthdelve.Tavern.Scene;
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
    /// 4g Checkpoint A's proof, through the real game (saves in a temp folder): the Larder Troll falls, its tusks come home and go
    /// up in Decorate Mode; <see cref="TrophyDisplayed"/> becomes the <c>displayed_trophy</c> deed, which Boog (a social stand-in,
    /// not in the scene) evaluates through Love/Hate: Affinity and Respect rise and he remembers it; his conversation takes the
    /// branch about the tusks; it all survives a scene change, a save, quitting and Continue. Plus the Quest Machine proof, the
    /// dialogue box's controls, the day clock and old saves.
    /// </summary>
    public class StoryCheckpointATests : BootFixture
    {
        [SetUp]
        public void AddPad() => m_Pad = InputSystem.AddDevice<Gamepad>();

        [TearDown]
        public void RemovePad() => InputSystem.RemoveDevice(m_Pad);

        Gamepad m_Pad;

        static StoryHost Host => StoryHost.Instance;
        static RelationshipAdapter Social => Host.Relationships;
        static HearthDialogueUI Box => Object.FindAnyObjectByType<HearthDialogueUI>();

        // ---------- The way through the game ----------

        IEnumerator BootToMenu()
        {
            yield return Boot();
            yield return WaitUntil(() => Host != null && Host.Relationships != null, 10f, "the story");
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase, string what)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, what);
            // As a player would: the fade from black is over (until then it takes the pointer).
            yield return WaitUntil(() => !(Flow.Transition is TransitionScreen t && t.IsCovering), 5f, "the scene revealed");
            yield return null;
        }

        IEnumerator NewGameToTheDelve()
        {
            yield return BootToMenu();
            GameFlow.Instance.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return null;
        }

        IEnumerator ExtractAndGoHome()
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "extracted");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night, home again");
        }

        IEnumerator DefeatTheTroll()
        {
            Player.GetComponent<EssenceHealth>().GodMode = true;
            var encounter = Object.FindAnyObjectByType<BossEncounter>();
            yield return WaitUntil(() => encounter.State == BossEncounterState.Fighting, 8f, "the troll");
            var health = encounter.GetComponent<BossHealth>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "the defeat recorded");
        }

        /// <summary>Opens Decorate from the night and hangs the homecoming trophy on the back wall.</summary>
        static IEnumerator HangTheTusks()
        {
            Object.FindAnyObjectByType<NightScreen>().DecorateButton.onClick.Invoke();
            yield return null;
            DecorateMode mode = DecorateMode.Instance;
            Assert.That(mode.HomecomingPiece, Is.EqualTo("trophy_larder_troll"), "the tusks, on the cursor");
            bool placed = false;
            for (int x = 4; x < 24 && !placed; x++)
            for (int y = 13; y <= 15 && !placed; y++)
            {
                mode.SetCursor(new Vector2Int(x, y));
                placed = mode.CarriedCheck.IsValid;
            }
            Assert.That(placed, "a spot on the back wall");
            mode.Place();
            yield return null;
        }

        SaveData SavedGame() => SaveSystem.FromJson(File.ReadAllText(Path.Combine(SaveDir, SaveStore.FileName)));

        // ---------- Talking ----------

        IEnumerator Talk(string characterId)
        {
            Assert.That(StoryServices.Conversations.Talk(characterId), $"talking to {characterId}");
            yield return null;
            yield return null;
            Assert.That(Box.IsOpen, "the box is open");
        }

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

        /// <summary>One press finishes the reveal, the next moves on (Space).</summary>
        IEnumerator NextLine()
        {
            if (Box.IsRevealing) yield return Press(Key.Space);
            Assert.That(Box.IsRevealing, Is.False, "the reveal finished on the first press");
            yield return null;
            Assert.That(Box.ContinueShown, "▼ while it waits");
            yield return Press(Key.Space);
        }

        /// <summary>Picks the choice with these words (from Checkpoint C's tests, merged here by the test review).</summary>
        static IEnumerator Choose(string text)
        {
            yield return WaitUntil(() => Box.IsChoosing && !Box.ChoicesLocked, 2f, $"the choice \"{text}\"");
            int i = Enumerable.Range(0, Box.ResponseCount).First(n => Box.ChoiceText(n) == text);
            Box.Choose(i);
            yield return null;
            yield return null;
        }

        /// <summary>The first line someone says now (then the conversation is closed unheard).</summary>
        IEnumerator FirstLine(string id, System.Action<string> line)
        {
            yield return Talk(id);
            line(Box.Line);
            DialogueManager.StopAllConversations();
            yield return null;
        }

        IEnumerator UntilClosed()
        {
            for (int i = 0; i < 6 && Box.IsOpen; i++)
            {
                if (Box.IsChoosing) Assert.Fail("a choice waits");
                yield return NextLine();
            }
            Assert.That(Box.IsOpen, Is.False, "the conversation ended");
        }

        /// <summary>A left click where the pointer is (the fixture's click puts it in the corner).</summary>
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

        static Vector2 Centre(Transform t)
        {
            var rect = (RectTransform)t;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        static string Selected => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : null;

        // ---------- The proof ----------

        [UnityTest]
        public IEnumerator TheTrollsFall_AndTheTusks_ReachBoogAndOrik_ChangeWhatTheySay_OnceEach_AndSurviveSceneChangesSaveQuitAndContinue()
        {
            RoomRunner.StartInArenaOverride = true;
            yield return NewGameToTheDelve();
            float trollBoog0 = Social.Respect("gunta"), trollOrik0 = Social.Respect("pip");
            yield return DefeatTheTroll();
            yield return ExtractAndGoHome();
            Assert.That(Flow.State.Furniture.PendingHomecoming, Is.EqualTo("trophy_larder_troll"));

            // The troll's fall (Checkpoint C): both learned of it, from the first clear; each reads it by their own values.
            Assert.That(Social.Remembers("gunta", "felled_larder_troll") && Social.Remembers("pip", "felled_larder_troll"));
            float trollBoog = Social.Respect("gunta") - trollBoog0, trollOrik = Social.Respect("pip") - trollOrik0;
            Assert.That(trollBoog, Is.GreaterThan(trollOrik * 1.5f), "Boog's respect for nerve");
            Assert.That(trollOrik, Is.GreaterThan(3f), "Orik's, quieter");

            // Before the tusks: Boog hasn't seen them. The troll's fall is the first thing he talks about, loudly; Orik quietly, and
            // Phi. Then their everyday branches, with Boog's bomb to ask about.
            Assert.That(Social.Remembers("gunta", "displayed_trophy"), Is.False);
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Is.EqualTo("you killed the Larder Troll. the actual Larder Troll. the one that eats the Cellars."));
            yield return NextLine();
            Assert.That(Box.Line, Does.StartWith("i've said for years it was edible"), "no bomb to remember yet");
            yield return NextLine();
            yield return Choose("is it edible?");
            Assert.That(Box.Line, Does.StartWith("parts of it. the brave parts"));
            yield return UntilClosed();
            yield return Talk(CharacterIds.Orik);
            Assert.That(Box.Line, Does.StartWith("aye, the Larder Troll is dead. i've moved it from 'risks' to 'resolved'"));
            yield return NextLine();
            Assert.That(Box.Line, Does.StartWith("Phi went after it"));
            yield return NextLine();
            yield return Choose("what was she after?");
            Assert.That(Box.Line, Is.EqualTo("no' the troll, she said. whatever it was sitting on."));
            yield return UntilClosed();
            // Said once: next time, their everyday conversations.
            string orikLine = null;
            yield return FirstLine(CharacterIds.Orik, l => orikLine = l);
            Assert.That(orikLine, Does.StartWith("good evening, Bram"));
            // The tusks' baseline: after everything the troll's fall changed.
            float boogAffinity0 = Social.Affinity("gunta"), boogRespect0 = Social.Respect("gunta"), pipAffinity0 = Social.Affinity("pip"), pipRespect0 = Social.Respect("pip");
            yield return Talk(CharacterIds.Boog);
            Assert.That((Box.SpeakerId, Box.SpeakerName), Is.EqualTo(("gunta", "Boog")));
            Assert.That(Box.Line, Does.StartWith("the stove's hot"));
            yield return NextLine();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            Assert.That((Box.ResponseCount, Box.ChoiceText(0), Box.ChoiceText(1)), Is.EqualTo((2, "about your bomb...", "carry on.")));
            Assert.That(Selected, Is.EqualTo("Choice0"), "the first choice has the focus");
            yield return Press(Key.DownArrow);
            yield return Press(Key.Enter);
            yield return WaitUntil(() => !Box.IsOpen, 2f, "carry on: the conversation ends");
            Assert.That(MenuPause.IsPaused, Is.False);

            // The deed: the tusks go up. Boog and Orik learn of it at once, though neither is in Decorate Mode.
            yield return HangTheTusks();
            Assert.That(Social.Remembers("gunta", "displayed_trophy") && Social.Remembers("pip", "displayed_trophy"), "both remember");
            float boogAffinity = Social.Affinity("gunta"), boogRespect = Social.Respect("gunta"), pipAffinity = Social.Affinity("pip");
            Assert.That(boogAffinity, Is.GreaterThan(boogAffinity0), "Boog likes the keeper more (Love/Hate's evaluation)");
            Assert.That(boogRespect - boogRespect0, Is.EqualTo(13.5f).Within(0.01f), "and respects them: 15 × the nerve match (0.9)");
            Assert.That(pipAffinity, Is.GreaterThan(pipAffinity0), "Orik likes it too");
            Assert.That(Social.Respect("pip"), Is.EqualTo(pipRespect0).Within(0.01f), "but isn't impressed by monster parts");
            DecorateMode.Instance.Leave();
            yield return null;

            // Saved with the layout.
            StorySaveData saved = SavedGame().story;
            Assert.That(saved.relationships.values.Any(v => v.judge == "gunta" && v.subject == "player" && v.trait == "Respect" && Mathf.Abs(v.value - boogRespect) < 0.01f));
            Assert.That(saved.relationships.memories.Select(m => (m.judge, m.deed)), Is.SupersetOf(new[] { ("gunta", "displayed_trophy"), ("pip", "displayed_trophy") }));
            Assert.That(saved.dialogue, Is.Not.Empty);

            // After: a different branch, read from his memory and his respect. The choice by gamepad.
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("you hung the Larder Troll's tusks over the bar"));
            yield return NextLine();
            Assert.That(Box.Line, Does.Contain("after the bomb"));
            yield return NextLine();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(Selected, Is.EqualTo("Choice1"), "down moves to the second choice");
            yield return PressPad(GamepadButton.South);
            yield return WaitUntil(() => !Box.IsChoosing && Box.Line.StartsWith("only the once"), 2f, "Boog's answer");
            yield return UntilClosed();

            // A scene change (sleep: the tavern scene reloads): the stand-ins live in Boot; the day clock moves on.
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next day");
            Assert.That((Social.Day, GameTime.mode, GameTime.time), Is.EqualTo((2, GameTimeMode.Manual, 2f)));
            Assert.That(Social.Remembers("gunta", "displayed_trophy"), "remembered the next day");
            Assert.That(Social.Remembers("gunta", "felled_larder_troll"));
            Assert.That((Social.Affinity("gunta"), Social.Respect("gunta")), Is.EqualTo((boogAffinity, boogRespect)));

            // Quit and Continue: a new Boot, a new host, the same Boog.
            yield return BootToMenu();
            Assert.That((Social.Remembers("gunta", "displayed_trophy"), Social.Respect("gunta")), Is.EqualTo((false, 0f)), "nothing until a game is loaded");
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the day, continued");
            Assert.That(Social.Affinity("gunta"), Is.EqualTo(boogAffinity).Within(1e-3f));
            Assert.That(Social.Respect("gunta"), Is.EqualTo(boogRespect).Within(1e-3f));
            Assert.That(Social.Affinity("pip"), Is.EqualTo(pipAffinity).Within(1e-3f));
            Assert.That((Social.Remembers("gunta", "displayed_trophy"), Social.TimesSeen("gunta", "displayed_trophy")), Is.EqualTo((true, 1)));
            Assert.That(Social.Remembers("gunta", "felled_larder_troll") && Social.Remembers("pip", "felled_larder_troll"), "the troll remembered after Continue");
            Assert.That(Social.TimesSeen("gunta", "felled_larder_troll"), Is.EqualTo(1), "no duplicate deed after the reload");
            yield return FirstLine(CharacterIds.Orik, l => orikLine = l);
            // Orik's troll remark stays said (Checkpoint C); with the tusks up, his first line is now his own tusks remark.
            Assert.That(orikLine, Does.Not.StartWith("aye, the Larder Troll is dead"), "Orik's troll remark stays said");
            Assert.That(orikLine, Does.StartWith("the tusks over the bar"), "and the tusks have his attention");
            // Said once (Checkpoint C), and the dialogue's own state came back too: his everyday branch, not the tusks or the troll
            // again; and the tusks never hide the bomb offer (Checkpoint C's priority test, merged here).
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("the stove's hot"), "the tusks and the troll remarks stay said after Continue");
            yield return NextLine();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Is.EqualTo("i lost something in the Hollows. my favorite bomb."), "the quest is never out of reach");
            DialogueManager.StopAllConversations();
            yield return null;

            // Hanging them again (Shift+F1's way): the same deed, less fresh.
            float respectBefore = Social.Respect("gunta");
            Flow.DebugTrophyHomecoming();
            Assert.That(Flow.State.Furniture.PendingHomecoming, Is.EqualTo("trophy_larder_troll"));
            Assert.That(Social.TimesSeen("gunta", "displayed_trophy"), Is.EqualTo(1));

            // A new game starts the story afresh.
            Flow.QuitToMenu();
            yield return WaitUntil(() => Object.FindAnyObjectByType<MainMenuScreen>() != null && !Flow.IsLoading, 10f, "the menu");
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.NewGameButton.onClick.Invoke();
            yield return null;
            Assert.That(menu.IsConfirming, "starting over asks first");
            menu.ConfirmYes.onClick.Invoke();
            menu.Creator.BeginButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Tavern, 30f, "the arrival");
            Assert.That((Social.Remembers("gunta", "displayed_trophy"), Social.Respect("gunta"), Social.Affinity("gunta")), Is.EqualTo((false, 0f, 10f)));
            Assert.That(Host.Quests.State("boogs_bomb"), Is.EqualTo("unassigned"));
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Arrival), "a new game has its opening ahead");
            Assert.That(respectBefore, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator TheDialogueBox_TakesKeyboardMouseAndGamepad_PausesTheWorld_TheNightStepsAside_AndGivesTheInputBack()
        {
            yield return NewGameToTheDelve();
            yield return ExtractAndGoHome();
            string[] before = InputMaps.Snapshot();
            // The night's summary steps aside while someone talks (Checkpoint C's presentation test, merged here by the test review).
            var aside = Object.FindAnyObjectByType<NightScreen>().GetComponentInParent<StepAsideWhileTalking>();
            Assert.That(aside, Is.Not.Null);
            Assert.That(aside.SteppedAside, Is.False);

            // Orik, by name: the keeper's name comes from the save, through the dialogue's markup.
            yield return Talk(CharacterIds.Orik);
            Assert.That(Time.timeScale, Is.Zero, "the world waits");
            Assert.That(InputMaps.Snapshot(), Is.EqualTo(new[] { InputMaps.UI }), "only the UI map while talking");
            Assert.That((Box.SpeakerName, Box.Line), Is.EqualTo(("Orik", "good evening, Bram. the ledger and me are on speaking terms again.")));
            var portrait = Object.FindObjectsByType<UnityEngine.UI.Image>().Single(i => i.name == "Portrait");
            Assert.That(portrait.gameObject.activeInHierarchy, "his portrait");
            Assert.That(Host.Characters.Definition("pip").portrait.talking, Does.Contain(portrait.sprite), "talking while the line is revealed");
            Assert.That(Box.IsRevealing, "the line is revealed as it's spoken");
            yield return Press(Key.E);
            Assert.That(Box.IsRevealing, Is.False, "E finishes the reveal");
            Assert.That(Box.IsOpen, "and doesn't skip the line");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(aside.SteppedAside, "the night's summary makes way for the conversation");
            // A click on the box moves on: to his questions (Checkpoint B), where "never mind." ends it.
            Vector2 at = Centre(Object.FindAnyObjectByType<DialogueBoxClick>().transform);
            yield return ClickAt(at);
            yield return WaitUntil(() => Box.IsChoosing, 2f, "his questions");
            Assert.That(Box.ResponseCount, Is.EqualTo(4));
            Assert.That(Box.ChoiceText(3), Is.EqualTo("never mind."));
            yield return ClickAt(Centre(Box.ChoiceButton(3).transform));
            yield return WaitUntil(() => !Box.IsOpen, 2f, "never mind: closed");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(InputMaps.Snapshot(), Is.EqualTo(before), "the maps that were on are on again");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(aside.SteppedAside, Is.False, "and the night's summary comes back after");

            // Boog's choices by mouse: hovering moves the focus and the ▶; a click chooses.
            yield return Talk(CharacterIds.Boog);
            yield return NextLine();
            yield return WaitUntil(() => Box.IsChoosing, 2f, "the choices");
            Vector2 second = Centre(Box.ChoiceButton(1).transform);
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = second });
            yield return null;
            yield return null;
            Assert.That(Selected, Is.EqualTo("Choice1"), "hovering focuses the choice");
            Assert.That(Enumerable.Range(0, 4).Where(Box.PointerShown), Is.EqualTo(new[] { 1 }), "▶ beside it, and only it");
            Assert.That(Object.FindObjectsByType<SuperTextMesh>().Single(t => t.name == "Pointer1").drawText, Is.EqualTo("‣"), "drawn");
            yield return ClickAt(second);
            yield return WaitUntil(() => !Box.IsOpen, 2f, "carry on: the choice taken, the conversation over");

            // Gamepad A moves on as well, and chooses.
            yield return Talk(CharacterIds.Orik);
            yield return PressPad(GamepadButton.South);
            Assert.That(Box.IsRevealing || Box.IsChoosing, Is.False, "A finishes the reveal and stops there");
            yield return PressPad(GamepadButton.South);
            yield return WaitUntil(() => Box.IsChoosing, 2f, "then moves on");
            // Down to the fourth (the module may let the first nudge after the choices appear settle the focus).
            for (int i = 0; i < 5 && Selected != "Choice3"; i++) yield return PressPad(GamepadButton.DpadDown);
            Assert.That(Selected, Is.EqualTo("Choice3"));
            yield return PressPad(GamepadButton.South);
            yield return WaitUntil(() => !Box.IsOpen, 2f, "A chooses never mind");
        }

        [UnityTest]
        public IEnumerator AMemory_LastsItsGameDays_NeverRealOrPausedTime()
        {
            yield return BootToMenu();
            var deed = ScriptableObject.CreateInstance<DeedDefinition>();
            deed.id = "test_kindness";
            deed.shows = new SocialTraits(0f, 0f, 80f);
            deed.impact = 30f;
            deed.respect = 10f;
            deed.memoryDays = 2;
            Social.Add(deed);
            Social.SetDay(5);
            DeedReaction? pip = null;
            void Seen(DeedReaction r) { if (r.Judge == "pip") pip = r; }
            Social.Reacted += Seen;
            Social.Commit(deed, new[] { "pip" });
            Assert.That(pip.HasValue && pip.Value.Remembered && pip.Value.Affinity > 0f, "Orik saw it and liked it");
            Assert.That(pip.Value.Respect, Is.EqualTo(RelationshipRules.RespectChange(10f, 1f - 0f / 200f, 1f)).Within(0.01f), "warmth for warmth: full respect");
            Assert.That(Social.Remembers("gunta", "test_kindness"), Is.False, "only those it was committed to learn of it");

            // Paused and idle in real time: still remembered (Love/Hate cleans its memory every 2 real seconds).
            MenuPause.Push();
            yield return new WaitForSecondsRealtime(2.5f);
            MenuPause.Pop();
            Assert.That(Social.Remembers("pip", "test_kindness"), "real time never ages a memory");
            Social.SetDay(7);
            Assert.That(Social.Remembers("pip", "test_kindness"), "the last day it lasts");
            Social.SetDay(8);
            Assert.That(Social.Remembers("pip", "test_kindness"), Is.False, "forgotten after two days");

            // A repeat is less fresh: Love/Hate's acclimatization scales Respect too.
            Social.SetDay(5);
            DeedReaction? first = null, again = null;
            void Count(DeedReaction r) { if (r.Judge == "pip") { if (first == null) first = r; else again = r; } }
            Social.Reacted -= Seen;
            Social.Reacted += Count;
            Host.Clear();
            Social.Commit(deed, new[] { "pip" });
            Social.Commit(deed, new[] { "pip" });
            Social.Reacted -= Count;
            Assert.That(again.Value.Respect, Is.LessThan(first.Value.Respect));
            Assert.That(Social.TimesSeen("pip", "test_kindness"), Is.EqualTo(2));

            // Recorded and given back exactly.
            RelationshipData recorded = Social.Record();
            float affinity = Social.Affinity("pip"), respect = Social.Respect("pip");
            Social.Clear();
            Assert.That(Social.Remembers("pip", "test_kindness"), Is.False);
            Social.Apply(recorded);
            Assert.That((Social.Affinity("pip"), Social.Respect("pip"), Social.TimesSeen("pip", "test_kindness")), Is.EqualTo((affinity, respect, 2)));
            Host.Clear();
        }

        [UnityTest]
        public IEnumerator ASaveFromBefore4g_Continues_WithoutTheOpening_AsBram_WithNothingInferred()
        {
            RoomRunner.StartInArenaOverride = true;
            yield return NewGameToTheDelve();
            yield return DefeatTheTroll();
            yield return ExtractAndGoHome();
            // Rewrite it as a version 7 save (4f): no story at all.
            string path = Path.Combine(SaveDir, SaveStore.FileName);
            SaveData data = SavedGame();
            data.version = 7;
            string json = Regex.Replace(SaveSystem.ToJson(data), @",\s*""story"":.*$", "\n}", RegexOptions.Singleline);
            File.WriteAllText(path, json);

            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued");
            Assert.That(Flow.State.Story.OpeningComplete, "no opening for an old game");
            Assert.That((Flow.State.Story.Player.name, Flow.State.Story.Player.body), Is.EqualTo(("Bram", "townsfolk")));
            Assert.That(Flow.State.TimesDefeated("larder_troll"), Is.EqualTo(1));
            Assert.That(Social.Remembers("gunta", "displayed_trophy") || Social.Respect("gunta") != 0f, Is.False, "the troll's fall isn't turned into history");
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("the stove's hot"), "no tusks branch: his everyday line");
            DialogueManager.StopAllConversations();
            yield return null;
            Assert.That(Box.IsOpen, Is.False);
            Flow.Save();
            Assert.That((SavedGame().version, SavedGame().story.openingComplete), Is.EqualTo((SaveSystem.CurrentVersion, true)), "saved at the current version, still without its opening");
        }

        [UnityTest]
        public IEnumerator Boot_HostsTheStory_StandInsAndMiddleware()
        {
            yield return BootToMenu();
            Assert.That(StoryServices.State, Is.SameAs(Host));
            Assert.That(StoryServices.Conversations, Is.SameAs(Host));
            Assert.That(Social.StandIns, Is.EquivalentTo(new[] { "gunta", "pip", "musashi", "maximo", "kaloren", "grim", "ogrin", "bart", "gimp" }),
                "a stand-in per tracked character, keyed by id (Musashi since 2026-10-07; Kariaston's people since 4h Checkpoint C; Gimp since D)");
            Assert.That(Host.GetComponentsInChildren<PixelCrushers.LoveHate.FactionMember>().Length, Is.EqualTo(10), "the nine, and the keeper as the actor");
            Assert.That(DialogueManager.masterDatabase.GetConversation("Boog/Talk"), Is.Not.Null);
            Assert.That(Lua.Run("return HH_Respect(\"gunta\")").asFloat, Is.Zero);
            Assert.That(Host.Quests.Journal, Is.Not.Null);
            Assert.That(GameTime.mode, Is.EqualTo(GameTimeMode.Manual), "memories run on the game's days");
            Assert.That(Host.CanTalk("gunta") && Host.CanTalk("pip") && !Host.CanTalk("player"));
            Assert.That(Object.FindObjectsByType<PixelCrushers.SaveSystem>().Length, Is.EqualTo(1), "one Pixel Crushers save component: a serializer, never a second save");
        }
    }
}
