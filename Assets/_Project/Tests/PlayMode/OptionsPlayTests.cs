using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Settings;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
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

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-B, settings and accessibility in the game: Options from the main menu and the pause menu, a volume heard through the
    /// mixer at once, the Hollows' quarter-down on the mixer (never the listener), relaxed timing reaching only the keeper's
    /// stations, patient customers, and the text speed in a real conversation.
    /// </summary>
    public class OptionsPlayTests : LookTestFixture
    {
        string m_SaveDir, m_OptionsDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static PauseMenu Pause => PauseMenu.Instance;
        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();

        [SetUp]
        public void UseTempFiles()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            m_OptionsDir = Path.Combine(Path.GetTempPath(), "HearthdelveOptions_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            GameOptions.DirectoryOverride = m_OptionsDir;
            GameOptions.Reload();
        }

        [TearDown]
        public void ClearFiles()
        {
            if (Pause != null && Pause.IsOpen) Pause.Close();
            GameFlow.SaveDirectoryOverride = null;
            GameOptions.DirectoryOverride = null;
            GameOptions.Reload();
            SurfacePause.Clear();
            MusicHolds.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
            if (Directory.Exists(m_OptionsDir)) Directory.Delete(m_OptionsDir, true);
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

        IEnumerator Menu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
            yield return Frames(3);
        }

        IEnumerator FirstDelve()
        {
            yield return Menu();
            Flow.QuickNewGame();
            Flow.MarkHintSeen(Hearthdelve.Shared.Village.CommunityRules.GimpIntro);
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            yield return Revealed();
        }

        IEnumerator Daytime(OpeningStage opening)
        {
            yield return FirstDelve();
            Flow.State.Story.Opening = opening;
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            Flow.Sleep();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            yield return Frames(3);
        }

        static float Db(string parameter)
        {
            Assert.That(AudioMixerHub.Instance.Mixer.GetFloat(parameter, out float value), Is.True, parameter);
            return value;
        }

        [UnityTest]
        public IEnumerator TheMainMenu_OpensOptions_EveryTabHasItsLines_AndAVolumeIsHeardAtOnce()
        {
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.OptionsButton.onClick.Invoke();
            yield return null;
            OptionsScreen options = menu.Options;
            Assert.That(options.IsOpen, Is.True);
            Assert.That(menu.NewGameButton.gameObject.activeInHierarchy, Is.False, "Options covers the menu");
            Assert.That(options.Current, Is.EqualTo(OptionsScreen.Tab.Audio));
            Assert.That(options.Rows.Count, Is.EqualTo(3), "master, music, effects (no UI slider: interface sounds are effects)");
            Assert.That(options.Rows[0].ValueText, Is.EqualTo("100%"));

            options.Rows[2].Step(-1);
            yield return null;
            Assert.That(GameOptions.Current.effectsVolume, Is.EqualTo(0.95f).Within(1e-4f));
            Assert.That(Db(AudioMixerHub.EffectsVolume), Is.EqualTo(OptionsRules.ToDecibels(0.95f)).Within(0.05f), "the mixer has it at once");
            Assert.That(File.Exists(Path.Combine(m_OptionsDir, OptionsStore.FileName)), Is.True, "and it's saved");
            for (int i = 0; i < 25; i++) options.Rows[1].Step(-1);
            Assert.That(GameOptions.Current.musicVolume, Is.EqualTo(0f), "music all the way down");
            Assert.That(Db(AudioMixerHub.MusicVolume), Is.EqualTo(-80f).Within(0.05f));

            options.Show(OptionsScreen.Tab.Feel);
            Assert.That(options.Rows.Count, Is.EqualTo(6), "shake, flashes, hit-stop, vibration, its strength, reduced");
            options.Rows[3].Step(1);
            Assert.That(GameOptions.Current.vibration, Is.False);
            Assert.That(options.Rows[4].Usable, Is.False, "the strength doesn't apply with vibration off");
            options.Rows[4].Step(-1);
            Assert.That(GameOptions.Current.vibrationIntensity, Is.EqualTo(1f), "and can't be changed");
            options.Rows[0].Step(-1);
            Assert.That(GameOptions.Current.shake, Is.EqualTo(ShakeLevel.Low));

            options.Show(OptionsScreen.Tab.Display);
            Assert.That(options.Rows.Count, Is.EqualTo(OptionsScreen.IsWeb ? 1 : 2), "fullscreen, and on desktop the window size");
            options.Show(OptionsScreen.Tab.Accessibility);
            Assert.That(options.Rows.Count, Is.EqualTo(3), "text speed, relaxed timing, patient customers");
            options.Show(OptionsScreen.Tab.Controls);
            Assert.That(menu.ControlsPage.IsOpen, Is.True, "the controls tab is the controls page");

            // A new game keeps the options: they're the player's, not the keeper's.
            options.Close();
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            Assert.That(GameOptions.Current.musicVolume, Is.EqualTo(0f));
            Assert.That(GameOptions.Current.shake, Is.EqualTo(ShakeLevel.Low));
        }

        [UnityTest]
        public IEnumerator TheHollows_LowerEffectsOnTheMixer_NeverTheListener_AndTheMusicHasItsOwnGroup()
        {
            GameOptions.Change(o => o.effectsVolume = 0.8f);
            yield return FirstDelve();
            yield return Frames(5);
            Assert.That(AudioListener.volume, Is.EqualTo(1f), "the listener is left alone");
            Assert.That(AudioMixerHub.Instance.HollowsLevel, Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(Db(AudioMixerHub.EffectsVolume), Is.EqualTo(OptionsRules.ToDecibels(0.8f * 0.75f)).Within(0.05f), "the player's level, a quarter down");
            foreach (AudioSource source in AudioMixerHub.Instance.GetComponentsInChildren<AudioSource>(true))
                Assert.That(source.outputAudioMixerGroup, Is.SameAs(AudioMixerHub.MusicGroup), $"{source.name} plays through Music");
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Frames(5);
            Assert.That(Db(AudioMixerHub.EffectsVolume), Is.EqualTo(OptionsRules.ToDecibels(0.8f)).Within(0.05f), "home: the player's level as it is");
        }

        [UnityTest]
        public IEnumerator FromThePauseMenu_RelaxedTimingAndPatientCustomers_ReachOnlyTheKeepersEvening()
        {
            yield return Daytime(OpeningStage.Complete);
            Pause.Open();
            yield return null;
            Pause.OptionsButton.onClick.Invoke();
            yield return null;
            Assert.That(Pause.Options.IsOpen, Is.True);
            Assert.That(MenuPause.IsPaused, Is.True, "still paused under Options");
            Pause.Options.Show(OptionsScreen.Tab.Accessibility);
            Assert.That(Director.KeeperMinigames, Is.SameAs(Director.Minigames), "off: the keeper cooks like everyone");
            Pause.Options.Rows[1].Step(1);
            Pause.Options.Rows[2].Step(1);
            Assert.That(GameOptions.Current.relaxedTiming && GameOptions.Current.patientCustomers, Is.True);
            Pause.Close();
            yield return null;

            Assert.That(Director.KeeperMinigames, Is.Not.SameAs(Director.Minigames));
            float plain = Director.Minigames.Grill.bandMax - Director.Minigames.Grill.bandMin;
            float relaxed = Director.KeeperMinigames.Grill.bandMax - Director.KeeperMinigames.Grill.bandMin;
            Assert.That(relaxed, Is.GreaterThan(plain), "the keeper's grill band is wider");

            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            var grilled = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            foreach (var need in grilled.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new Hearthdelve.Shared.Inventory.IngredientStack(new Hearthdelve.Shared.Ingredients.IngredientItem(need.ingredient, Hearthdelve.Shared.Ingredients.Quality.Standard), 4, 1f));
            Director.SetMenu(new[] { grilled });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            yield return WaitUntil(() => Director.IsServing, 10f, "service");
            var customer = Director.SpawnCustomer(Director.Content.customers.First());
            Assert.That(customer, Is.Not.Null);
            var profile = customer.Logic.Profile;
            float expected = (profile != null ? profile.traits.orderPatience : Hearthdelve.Tavern.Customers.CustomerTraits.Default.orderPatience) * OptionsRules.PatientScale;
            Assert.That(customer.Logic.Traits.orderPatience, Is.EqualTo(expected).Within(1e-3f), "patient customers wait longer");
        }

        [UnityTest]
        public IEnumerator TextSpeed_Instant_WritesTheLineOutWhole_Normal_RevealsIt()
        {
            GameOptions.Change(o => o.textSpeed = TextSpeed.Instant);
            yield return Daytime(OpeningStage.FirstEvening);
            AreaPassage down = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).Single(p => p.To != null && p.To.Id == PropertyArea.TavernId);
            down.Pass(Keeper);
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 5f, "the first morning");
            var box = Object.FindAnyObjectByType<HearthDialogueUI>();
            yield return WaitUntil(() => !string.IsNullOrEmpty(box.Line), 5f, "a line");
            yield return Frames(2);
            Assert.That(box.IsRevealing, Is.False, "instant: the whole line at once");

            GameOptions.Change(o => o.textSpeed = TextSpeed.Normal);
            string first = box.Line;
            box.Advance();
            yield return WaitUntil(() => box.Line != first, 5f, "the next line");
            yield return Frames(1);
            Assert.That(box.IsRevealing, Is.True, "normal: written out letter by letter");
        }
    }
}
