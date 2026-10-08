using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.UI.Localization;
using NUnit.Framework;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4i-A, first impressions: quitting by phase, when the pause menu opens, prompts' device, first-day guidance, safe saves, the new words.</summary>
    public class FirstImpressionsTests
    {
        // ---------- quitting, phase by phase (D2) ----------

        [TestCase(DayPhase.Daytime, OpeningStage.Complete, null, false, QuitKind.SaveAndQuit, false, true)]
        [TestCase(DayPhase.Daytime, OpeningStage.FirstEvening, null, false, QuitKind.SaveAndQuit, false, true)]
        [TestCase(DayPhase.Daytime, OpeningStage.Arrival, "Arrival", false, QuitKind.RestartArrival, true, false)]
        [TestCase(DayPhase.Evening, OpeningStage.Complete, "Prep", false, QuitKind.DiscardEvening, true, false)]
        [TestCase(DayPhase.Evening, OpeningStage.Complete, "Service", false, QuitKind.DiscardEvening, true, false)]
        [TestCase(DayPhase.Evening, OpeningStage.FirstEvening, "Results", false, QuitKind.BankEvening, false, true)]
        [TestCase(DayPhase.Delve, OpeningStage.Complete, null, false, QuitKind.AbandonDelve, true, false)]
        [TestCase(DayPhase.Delve, OpeningStage.FirstDelve, null, false, QuitKind.AbandonDelve, true, false)]
        [TestCase(DayPhase.Delve, OpeningStage.Complete, null, true, QuitKind.BankDelve, false, true)]
        [TestCase(DayPhase.Night, OpeningStage.Homecoming, "Night", false, QuitKind.SaveAndQuit, false, true)]
        [TestCase(DayPhase.Night, OpeningStage.Complete, "Night", false, QuitKind.SaveAndQuit, false, true)]
        public void Quitting_FollowsThePhase(DayPhase phase, OpeningStage opening, string tavernPhase, bool delveOver, QuitKind kind, bool warns, bool saves)
        {
            QuitPlan plan = QuitRules.Plan(phase, opening, tavernPhase, delveOver);
            Assert.That(plan.Kind, Is.EqualTo(kind));
            Assert.That(plan.Warns, Is.EqualTo(warns), "asks first exactly when something since the last save is given up");
            Assert.That(plan.Saves, Is.EqualTo(saves), "saves on the way out only where the moment is a resume point (or was just banked)");
        }

        // ---------- when the pause menu opens ----------

        [Test]
        public void ThePauseMenu_OpensOnFoot_AndOnThePhaseScreens_NeverOverAnythingElse()
        {
            Assert.That(PauseRules.CanOpen(true, false, false, false, onFoot: true, menuPaused: false, onPhaseScreen: false), Is.True, "walking about");
            Assert.That(PauseRules.CanOpen(true, false, false, false, onFoot: false, menuPaused: false, onPhaseScreen: true), Is.True, "Prep, results, night");
            Assert.That(PauseRules.CanOpen(true, false, false, false, onFoot: false, menuPaused: true, onPhaseScreen: true), Is.True, "the delve's result (paused under it)");
            Assert.That(PauseRules.CanOpen(true, false, false, false, onFoot: true, menuPaused: true, onPhaseScreen: false), Is.False, "another menu paused the game");
            Assert.That(PauseRules.CanOpen(true, false, false, false, onFoot: false, menuPaused: false, onPhaseScreen: false), Is.False, "a panel, a station or Decorate Mode has Esc");
            Assert.That(PauseRules.CanOpen(true, false, talking: true, false, true, false, false), Is.False, "a conversation");
            Assert.That(PauseRules.CanOpen(true, loading: true, false, false, true, false, false), Is.False, "a load");
            Assert.That(PauseRules.CanOpen(true, false, false, blocked: true, true, false, false), Is.False, "a story scene");
            Assert.That(PauseRules.CanOpen(inGame: false, false, false, false, true, false, false), Is.False, "the main menu");
        }

        [Test]
        public void AStoryScene_HoldsThePauseMenuOff_UntilItLetsGo()
        {
            var scene = new object();
            PauseRules.Block(scene);
            Assert.That(PauseRules.IsBlocked, Is.True);
            PauseRules.Unblock(scene);
            Assert.That(PauseRules.IsBlocked, Is.False);
        }

        // ---------- prompts follow the last meaningful input (A4) ----------

        [Test]
        public void Prompts_FollowAPressOrAPush_NotDrift()
        {
            var kb = InputDeviceKind.KeyboardMouse;
            var pad = InputDeviceKind.Gamepad;
            Assert.That(InputDeviceRules.After(kb, pad, isButton: true, 0f, false), Is.EqualTo(pad), "a button on the pad");
            Assert.That(InputDeviceRules.After(kb, pad, isButton: false, 0.9f, false), Is.EqualTo(pad), "a stick pushed");
            Assert.That(InputDeviceRules.After(kb, pad, isButton: false, 0.2f, false), Is.EqualTo(kb), "a stick resting off-centre");
            Assert.That(InputDeviceRules.After(pad, kb, isButton: false, 3f, isPointerMotion: true), Is.EqualTo(pad), "the mouse merely moving");
            Assert.That(InputDeviceRules.After(pad, kb, isButton: true, 0f, false), Is.EqualTo(kb), "a key");
            Assert.That(InputDeviceRules.After(pad, kb, isButton: false, 1f, false), Is.EqualTo(kb), "the wheel");
            Assert.That(InputDeviceRules.Group(pad), Is.EqualTo("Gamepad"));
            Assert.That(InputDeviceRules.Group(kb), Is.EqualTo("Keyboard&Mouse"));
        }

        // ---------- first-day guidance (D3) ----------

        [Test]
        public void TheFirstMorning_PlaysOnce_AsTheKeeperComesDownstairs_OnDayTwosStageOnly()
        {
            var seen = new HashSet<string>();
            OpeningBeat? beat = OpeningRules.Beat(OpeningStage.FirstEvening, OpeningRules.Downstairs, seen);
            Assert.That(beat, Is.Not.Null);
            Assert.That(beat.Value.Conversation, Is.EqualTo(OpeningRules.FirstMorning));
            Assert.That(beat.Value.After, Is.EqualTo(OpeningStage.FirstEvening), "it doesn't move the opening on");
            seen.Add(beat.Value.OnceId);
            Assert.That(OpeningRules.Beat(OpeningStage.FirstEvening, OpeningRules.Downstairs, seen), Is.Null, "once");
            foreach (OpeningStage other in new[] { OpeningStage.Arrival, OpeningStage.FirstDelve, OpeningStage.Homecoming, OpeningStage.Complete })
                Assert.That(OpeningRules.Beat(other, OpeningRules.Downstairs, new HashSet<string>()), Is.Null, $"not at {other} (an old save past the opening never gets it)");
            Assert.That(OpeningRules.Conversations, Does.Contain(OpeningRules.FirstMorning));
        }

        [Test]
        public void ThePrompts_ShowOnceEach_InTheDaytime_AfterArrivalDay()
        {
            var seen = new HashSet<string>();
            foreach (string id in FirstDayPrompts.All)
            {
                Assert.That(FirstDayPrompts.Due(id, true, DayPhase.Daytime, OpeningStage.FirstEvening, seen), Is.True, id);
                Assert.That(FirstDayPrompts.Due(id, true, DayPhase.Daytime, OpeningStage.Arrival, seen), Is.False, $"{id}: not on arrival day");
                Assert.That(FirstDayPrompts.Due(id, true, DayPhase.Evening, OpeningStage.Complete, seen), Is.False, $"{id}: not in the evening");
                Assert.That(FirstDayPrompts.Due(id, false, DayPhase.Daytime, OpeningStage.Complete, seen), Is.False, $"{id}: not out of a game");
                seen.Add(id);
                Assert.That(FirstDayPrompts.Due(id, true, DayPhase.Daytime, OpeningStage.Complete, seen), Is.False, $"{id}: once");
            }
        }

        // ---------- saves the player can trust (A6) ----------

        string m_Dir;

        [SetUp]
        public void MakeDir() => m_Dir = Path.Combine(Path.GetTempPath(), "hh_save_test_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void RemoveDir()
        {
            if (Directory.Exists(m_Dir)) Directory.Delete(m_Dir, true);
        }

        static string Save(int day)
        {
            var state = new GameState(day, DayPhase.Daytime);
            return SaveSystem.ToJson(SaveSystem.Capture(state));
        }

        [Test]
        public void EachSave_KeepsTheOneBeforeAsTheBackup()
        {
            var store = new SaveStore(m_Dir);
            store.Write(Save(1));
            Assert.That(store.BackupExists, Is.False, "nothing to back up the first time");
            store.Write(Save(2));
            Assert.That(store.CheckMain().Data.day, Is.EqualTo(2));
            Assert.That(store.CheckBackup().Data.day, Is.EqualTo(1));
            store.Write(Save(3));
            Assert.That(store.CheckBackup().Data.day, Is.EqualTo(2), "the backup is the last good save before the current one");
        }

        [Test]
        public void AnUnreadableSave_IsSetAside_NeverOverTheBackup_AndTheBackupStillLoads()
        {
            var store = new SaveStore(m_Dir);
            store.Write(Save(4));
            store.Write(Save(5));
            File.WriteAllText(store.FilePath, "{ this is not a save");
            Assert.That(store.CheckMain().Problem, Is.EqualTo(SaveProblem.Unreadable));
            Assert.That(store.CheckBackup().Usable, Is.True);
            Assert.That(store.CheckBackup().Data.day, Is.EqualTo(4));

            // The game goes on from the backup and saves: the damaged file is kept aside, the good backup untouched.
            store.Write(Save(6));
            Assert.That(store.CheckMain().Data.day, Is.EqualTo(6));
            Assert.That(store.CheckBackup().Data.day, Is.EqualTo(4), "a damaged save never becomes the backup");
            Assert.That(File.ReadAllText(store.UnreadablePath), Does.StartWith("{ this is not"), "kept aside, not destroyed");
        }

        [Test]
        public void ASaveFromANewerVersion_IsRecognised_AndAnEmptyOneIsUnreadable()
        {
            string newer = Save(7).Replace($"\"version\": {SaveSystem.CurrentVersion}", "\"version\": 999");
            Assert.That(SaveSystem.Check(newer).Problem, Is.EqualTo(SaveProblem.Newer));
            Assert.That(SaveSystem.Check("").Problem, Is.EqualTo(SaveProblem.Unreadable));
            Assert.That(SaveSystem.Check(null).Problem, Is.EqualTo(SaveProblem.Unreadable));
            Assert.That(SaveSystem.Check(Save(7)).Usable, Is.True);
            Assert.That(new SaveStore(m_Dir).CheckMain().Problem, Is.EqualTo(SaveProblem.Missing));
        }

        [Test]
        public void ADamagedSave_IsRecognised_WithoutTheReaderEverThrowing()
        {
            string whole = Save(8);
            Assert.That(SaveSystem.LooksWhole(whole), Is.True);
            Assert.That(SaveSystem.LooksWhole(whole.Substring(0, whole.Length / 2)), Is.False, "cut off part-way");
            Assert.That(SaveSystem.LooksWhole("not a save"), Is.False);
            Assert.That(SaveSystem.LooksWhole("{ \"a\": \"}{\" }"), Is.True, "braces inside a string don't count");
            Assert.That(SaveSystem.LooksWhole("{ \"a\": \"\\\"}\" }"), Is.True, "nor an escaped quote");
            Assert.That(SaveSystem.Check(whole.Substring(0, whole.Length / 2)).Problem, Is.EqualTo(SaveProblem.Unreadable));
            Assert.That(SaveSystem.Check("{ \"version\": 0 }").Problem, Is.EqualTo(SaveProblem.Unreadable));
        }

        // ---------- the words (A3, A5, A6) ----------

        static Dictionary<string, string> English => MenuLocKeys.English.ToDictionary(e => e.key, e => e.english);

        [Test]
        public void EveryControlsPage_IsComplete_AndFitsItsColumns()
        {
            Dictionary<string, string> english = English;
            Assert.That(MenuLocKeys.Pages.Length, Is.EqualTo(5), "the day, the evening, the Hollows, decorating, menus");
            foreach (MenuLocKeys.Page page in MenuLocKeys.Pages)
            {
                Assert.That(english.ContainsKey(page.Title), page.Title);
                Assert.That(page.Rows.Length, Is.InRange(1, MenuLocKeys.MaxRows), $"{page.Title} fits the panel");
                foreach (MenuLocKeys.Row row in page.Rows)
                {
                    Assert.That(StoryTests.Width(english[row.Action]), Is.LessThanOrEqualTo(122f), row.Action);
                    Assert.That(StoryTests.Width(english[row.Keyboard]), Is.LessThanOrEqualTo(92f), row.Keyboard);
                    Assert.That(StoryTests.Width(english[row.Gamepad]), Is.LessThanOrEqualTo(62f), row.Gamepad);
                }
            }
            Assert.That(StoryTests.Width(string.Format(english[MenuLocKeys.ControlsPage], "menus and talking", 5, 5)), Is.LessThanOrEqualTo(280f));
            Assert.That(StoryTests.Width(english[MenuLocKeys.ControlsFooter]), Is.LessThanOrEqualTo(280f));
            Assert.That(StoryTests.Width(english[MenuLocKeys.ControlsKeyboard]), Is.LessThanOrEqualTo(92f), "the keyboard column's header");
            Assert.That(StoryTests.Width(english[MenuLocKeys.ControlsGamepad]), Is.LessThanOrEqualTo(62f), "the controller column's header");
        }

        [Test]
        public void TheMessages_FitTheirBoxes()
        {
            Dictionary<string, string> english = English;
            foreach (string key in new[] { MenuLocKeys.QuitArrival, MenuLocKeys.QuitEvening, MenuLocKeys.QuitDelve })
                Assert.That(StoryTests.Lines(english[key], 264f), Is.LessThanOrEqualTo(4), key);
            foreach (string key in new[] { MenuLocKeys.SaveUnreadable, MenuLocKeys.SaveUnreadableBackup, MenuLocKeys.SaveNewer })
                Assert.That(StoryTests.Lines(english[key], 284f), Is.LessThanOrEqualTo(2), key);
            foreach (string key in new[] { MenuLocKeys.PromptGarden, MenuLocKeys.PromptMarket, MenuLocKeys.PromptMenuBoard })
                Assert.That(StoryTests.Lines(english[key], 280f), Is.LessThanOrEqualTo(2), key);
            Assert.That(StoryTests.Width(string.Format(english[MenuLocKeys.BackupFrom], 99)), Is.LessThanOrEqualTo(140f));
            Assert.That(StoryTests.Width(string.Format("day {0}, {1}", 99, "the night's delve")), Is.LessThanOrEqualTo(140f), "the Continue line in the narrower panel");
            Assert.That(StoryTests.Width(string.Format(english[MenuLocKeys.Version], "0.4i-a")), Is.LessThanOrEqualTo(90f));
            Assert.That(StoryTests.Width(english[MenuLocKeys.Saved]), Is.LessThanOrEqualTo(40f));
        }
    }
}
