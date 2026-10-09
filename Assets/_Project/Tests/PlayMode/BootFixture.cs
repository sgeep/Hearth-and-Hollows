using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// The one base for tests that play the whole game from Boot (the test review, 2026-10-09; it replaced about sixteen copies of
    /// the same helpers, whose teardowns differed). Each test gets its own save folder, a fixed world seed and instant scene
    /// transitions (the fades are kept only by the tests about them, through <see cref="RealTransitions"/>); afterwards everything
    /// a test can leave behind is let go, in order. A daytime test starts from a written day-2 save and Continue
    /// (<see cref="StartDaytime"/>): the real new game → delve → night → sleep is played once per run to write it, and stays the
    /// path of the day-loop tests that are about it.
    /// </summary>
    public abstract class BootFixture : LookTestFixture
    {
        /// <summary>Every new game's world seed unless a test asks for another: runs don't vary by chance.</summary>
        protected const int DefaultSeed = 20261009;

        protected static GameFlow Flow => GameFlow.Instance;
        protected static TavernDirector Director => TavernDirector.Instance;
        protected static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();

        /// <summary>The test's hold on the surface clock (<see cref="StartDaytime"/> with <c>hold</c>).</summary>
        protected readonly object ClockHold = new();

        protected string SaveDir { get; private set; }

        /// <summary>The day-2 morning as the real path leaves it (the save's text), written once per run.</summary>
        static string s_Day2;

        bool m_RealTransitions;

        [SetUp]
        public void SetUpBoot()
        {
            SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = SaveDir;
            GameFlow.NewGameSeedOverride = DefaultSeed;
            AmbientMoments.ResetForTests();
            m_RealTransitions = false;
        }

        [TearDown]
        public void TearDownBoot()
        {
            // 1. Conversations and menus closed.
            if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) PauseMenu.Instance.Close();
            // 2-3. Every hold of the session let go.
            MenuPause.Clear();
            SurfacePause.Clear();
            MusicHolds.Clear();
            SurfaceTime.SettingsOverride = null;
            RoomRunner.SeedOverride = 0;
            RoomRunner.StartInArenaOverride = false;
            DecorateMode.Sandbox = false;
            GameFlow.NewGameSeedOverride = null;
            GameFlow.SaveDirectoryOverride = null;
            // (4-5: the device, time scale, vSync and frame rate are put back by LookTestFixture.)
            Time.timeScale = 1f;
            // 6. The test's files.
            if (Directory.Exists(SaveDir)) Directory.Delete(SaveDir, true);
            // 7. What the run looked like at the end of the test, for slowdowns late in a run.
            Debug.Log($"[Diagnose] {TestContext.CurrentContext.Test.Name}: frame {Time.unscaledDeltaTime * 1000f:0.0} ms, timeScale {Time.timeScale}, " +
                      $"targetFrameRate {Application.targetFrameRate}, vSync {QualitySettings.vSyncCount}, maxDelta {Time.maximumDeltaTime}, fixedDelta {Time.fixedDeltaTime}, " +
                      $"managed {GC.GetTotalMemory(false) / 1048576} MB, villagers {Villager.All.Count}, " +
                      $"objects {Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}");
        }

        // ---------- waiting ----------

        protected static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>
        /// Until the scene is in view and settled: not loading and uncovered for several frames running (a fade from clear starts at
        /// alpha 0, so one uncovered frame can't tell "revealed" from "about to cover").
        /// </summary>
        protected static IEnumerator Revealed()
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

        protected static bool IsIn(TavernPhase phase) => !Flow.IsLoading && Director != null && Director.Phase == phase;
        protected static bool InDungeonNow => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon;
        protected static bool InDaytimeNow => IsIn(TavernPhase.Daytime) && Flow.IsLoaded(GameScenes.Kariaston);

        // ---------- the game ----------

        /// <summary>Boot to the main menu, the tables loaded, the transitions instant (unless <see cref="RealTransitions"/>).</summary>
        protected IEnumerator Boot()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            if (!m_RealTransitions) Instant(true);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
        }

        /// <summary>The fades as the player sees them, for tests about them (call before <see cref="Boot"/>, or after to restore them).</summary>
        protected void RealTransitions()
        {
            m_RealTransitions = true;
            Instant(false);
        }

        static readonly string[] k_FadeFields = { "m_FadeOutSeconds", "m_HoldSeconds", "m_FadeInSeconds" };
        static float[] s_Fades;

        /// <summary>The Boot transition's three timings set to nothing (or back): every load still covers, captions and all, at once.</summary>
        static void Instant(bool on)
        {
            TransitionScreen screen = Object.FindAnyObjectByType<TransitionScreen>(FindObjectsInactive.Include);
            if (screen == null) return;
            FieldInfo[] fields = k_FadeFields.Select(f => typeof(TransitionScreen).GetField(f, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
            Assert.That(fields.All(f => f != null), "the transition's timings are where the tests expect");
            s_Fades ??= fields.Select(f => (float)f.GetValue(screen)).ToArray();
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(screen, on ? 0f : s_Fades[i]);
        }

        /// <summary>A new game without the opening, to its first delve (day 1).</summary>
        protected IEnumerator NewGameToTheDelve()
        {
            yield return Boot();
            Flow.QuickNewGame();
            yield return WaitUntil(() => InDungeonNow, 30f, "the first delve");
            yield return Revealed();
        }

        /// <summary>The delve over with nothing gained, to the night in Tally Ho!.</summary>
        protected static IEnumerator EmptyDelveToTheNight()
        {
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => IsIn(TavernPhase.Night), 30f, "the night");
            yield return Revealed();
        }

        /// <summary>Sleep, to the next morning upstairs with Kariaston loaded.</summary>
        protected static IEnumerator SleepToTheMorning()
        {
            Flow.Sleep();
            yield return WaitUntil(() => InDaytimeNow, 30f, "the daytime");
            yield return Revealed();
        }

        /// <summary>The real path to day 2: a new game, its delve, the night and sleep (what <see cref="StartDaytime"/> writes once).</summary>
        protected IEnumerator RealDaytime()
        {
            yield return NewGameToTheDelve();
            yield return EmptyDelveToTheNight();
            yield return SleepToTheMorning();
        }

        /// <summary>
        /// The first free day (day 2), from a written save and Continue: the state the real path leaves (written once a run by
        /// <see cref="RealDaytime"/>) with this test's world seed, opening and onboarding. <paramref name="seed"/>: a seed that
        /// satisfies it (asked of the pure day rules, with the day and the village's settings), so tests of seeded days find
        /// them at once. <paramref name="gimpSeen"/>: Gimp's night in the keeper's room already happened (it's only due from day
        /// 3). <paramref name="hold"/>: the clock held (released by <see cref="NextDay"/> and the teardown).
        /// </summary>
        protected IEnumerator StartDaytime(Func<int, VillageLifeSettings, bool> seed = null, OpeningStage opening = OpeningStage.Complete,
            bool gimpSeen = false, bool hold = false)
        {
            if (s_Day2 == null)
            {
                yield return RealDaytime();
                Assert.That(Flow.State.Day, Is.EqualTo(2));
                s_Day2 = File.ReadAllText(Path.Combine(SaveDir, SaveStore.FileName));
            }
            // Booted first: the village's settings (for the seed) come from the game's database.
            yield return Boot();
            SaveData data = SaveSystem.FromJson(s_Day2);
            Assert.That(data, Is.Not.Null, "the day-2 save reads");
            if (seed != null)
            {
                VillageLifeSettings settings = VillageLife.Settings;
                data.world.seed = Enumerable.Range(1, 100000).First(s => seed(s, settings));
            }
            else data.world.seed = DefaultSeed;
            data.story.openingStage = opening.ToString();
            data.story.openingComplete = opening == OpeningStage.Complete;
            if (gimpSeen && !data.story.seenHints.Contains(CommunityRules.GimpIntro)) data.story.seenHints.Add(CommunityRules.GimpIntro);
            Directory.CreateDirectory(SaveDir);
            File.WriteAllText(Path.Combine(SaveDir, SaveStore.FileName), SaveSystem.ToJson(data));
            Assert.That(Flow.Continue(), "the day-2 save continues");
            yield return WaitUntil(() => InDaytimeNow, 30f, "the daytime");
            yield return Revealed();
            Assert.That(Flow.State.Day, Is.EqualTo(2));
            if (hold) SurfacePause.Hold(ClockHold);
            yield return Frames(3);
        }

        /// <summary>The evening kept shut, an empty delve, the night and sleep: the next morning (the clock held again if it was).</summary>
        protected IEnumerator NextDay(bool hold = false)
        {
            SurfacePause.Release(ClockHold);
            Flow.StartEvening();
            yield return WaitUntil(() => IsIn(TavernPhase.Prep), 30f, "the evening");
            yield return Revealed();
            Flow.SkipService();
            yield return EmptyDelveToTheNight();
            yield return SleepToTheMorning();
            if (hold) SurfacePause.Hold(ClockHold);
            yield return Frames(2);
        }

        /// <summary>Save, quit to the menu and Continue, back at the same point of the day.</summary>
        protected IEnumerator SaveQuitAndContinue(Func<bool> arrived, string where)
        {
            Flow.Save();
            Flow.QuitToMenu();
            yield return WaitUntil(() => !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return Revealed();
            Assert.That(Flow.Continue(), "Continue");
            yield return WaitUntil(arrived, 30f, where);
            yield return Revealed();
        }
    }
}
