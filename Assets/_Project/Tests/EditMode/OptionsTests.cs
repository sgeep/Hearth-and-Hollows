using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Services;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Settings;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4i-B, settings and accessibility: the options' rules and file, the mixer and its routing, relaxed timing, patient customers, Esc kept through a fade.</summary>
    public class OptionsTests
    {
        string m_Dir;

        [SetUp]
        public void MakeDir()
        {
            m_Dir = Path.Combine(Path.GetTempPath(), "hh_options_test_" + System.Guid.NewGuid().ToString("N"));
            GameOptions.DirectoryOverride = m_Dir;
            GameOptions.Reload();
        }

        [TearDown]
        public void RemoveDir()
        {
            GameOptions.DirectoryOverride = null;
            GameOptions.Reload();
            if (Directory.Exists(m_Dir)) Directory.Delete(m_Dir, true);
        }

        // ---------- the rules ----------

        [Test]
        public void Volumes_MoveInStepsOfFivePercent_AndStayBetweenSilenceAndFull()
        {
            Assert.That(OptionsRules.StepVolume(1f, 1), Is.EqualTo(1f));
            Assert.That(OptionsRules.StepVolume(1f, -1), Is.EqualTo(0.95f).Within(1e-5f));
            Assert.That(OptionsRules.StepVolume(0.05f, -1), Is.EqualTo(0f));
            Assert.That(OptionsRules.StepVolume(0f, -1), Is.EqualTo(0f));
            Assert.That(OptionsRules.StepVolume(0.52f, 0), Is.EqualTo(0.5f).Within(1e-5f), "an off-grid value is put back on the grid");
            float v = 0f;
            for (int i = 0; i < 20; i++) v = OptionsRules.StepVolume(v, 1);
            Assert.That(v, Is.EqualTo(1f).Within(1e-5f), "twenty steps from silence is full");
        }

        [Test]
        public void Volumes_AreDecibelsOnTheMixer_ZeroIsSilence()
        {
            Assert.That(OptionsRules.ToDecibels(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(OptionsRules.ToDecibels(0.5f), Is.EqualTo(-6.02f).Within(0.01f));
            Assert.That(OptionsRules.ToDecibels(0f), Is.EqualTo(-80f));
            Assert.That(OptionsRules.ToDecibels(0.0001f), Is.EqualTo(-80f));
        }

        [Test]
        public void TheDefaults_AreFullVolume_FullFeel_NormalText_AndNoAssists()
        {
            var o = new PlayerOptions();
            Assert.That((o.masterVolume, o.musicVolume, o.effectsVolume), Is.EqualTo((1f, 1f, 1f)));
            Assert.That(o.shake, Is.EqualTo(ShakeLevel.Full));
            Assert.That(o.flashes && o.hitStop && o.vibration && !o.reducedVibration, Is.True);
            Assert.That(o.vibrationIntensity, Is.EqualTo(1f));
            Assert.That(o.fullscreen, Is.True);
            Assert.That(o.textSpeed, Is.EqualTo(TextSpeed.Normal));
            Assert.That(o.relaxedTiming, Is.False, "relaxed timing is off by default");
            Assert.That(o.patientCustomers, Is.False, "patient customers are off by default");
        }

        [Test]
        public void WindowSizes_AreWholeMultiplesOf320By180_ThatFitTheDisplay()
        {
            Assert.That(OptionsRules.WindowScales(1920, 1080), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(OptionsRules.WindowScales(2560, 1440), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(OptionsRules.WindowScales(300, 160), Is.EqualTo(new[] { 1 }), "always at least 1×");
            Assert.That(OptionsRules.WindowScale(0, 1920, 1080), Is.EqualTo(6), "unchosen: the largest that fits");
            Assert.That(OptionsRules.WindowScale(3, 1920, 1080), Is.EqualTo(3));
            Assert.That(OptionsRules.WindowScale(8, 1920, 1080), Is.EqualTo(6), "too big for this display: the largest that fits");
        }

        [Test]
        public void TextSpeed_SlowIsSlower_InstantIsWhole()
        {
            Assert.That(OptionsRules.TextSpeedScale(TextSpeed.Slow), Is.LessThan(1f));
            Assert.That(OptionsRules.TextSpeedScale(TextSpeed.Normal), Is.EqualTo(1f));
            Assert.That(float.IsPositiveInfinity(OptionsRules.TextSpeedScale(TextSpeed.Instant)), Is.True);
        }

        [Test]
        public void AnythingOutOfRange_IsBroughtBack()
        {
            var o = new PlayerOptions { masterVolume = 3f, musicVolume = -1f, vibrationIntensity = 0f, shake = (ShakeLevel)9, textSpeed = (TextSpeed)(-2), windowScale = 99, version = 0 };
            o = OptionsRules.Sanitized(o);
            Assert.That(o.masterVolume, Is.EqualTo(1f));
            Assert.That(o.musicVolume, Is.EqualTo(0f));
            Assert.That(o.vibrationIntensity, Is.EqualTo(0.1f).Within(1e-5f), "intensity never reaches nothing (vibration off is its own switch)");
            Assert.That(o.shake, Is.EqualTo(ShakeLevel.Full));
            Assert.That(o.textSpeed, Is.EqualTo(TextSpeed.Normal));
            Assert.That(o.windowScale, Is.EqualTo(12));
            Assert.That(o.version, Is.EqualTo(PlayerOptions.CurrentVersion));
        }

        // ---------- the file (never the save) ----------

        [Test]
        public void TheOptions_LiveInTheirOwnFile_AndComeBackAsTheyWereLeft()
        {
            GameOptions.Change(o =>
            {
                o.musicVolume = 0.35f;
                o.shake = ShakeLevel.Low;
                o.textSpeed = TextSpeed.Instant;
                o.relaxedTiming = true;
            });
            Assert.That(File.Exists(Path.Combine(m_Dir, OptionsStore.FileName)), Is.True);
            GameOptions.Reload();
            Assert.That(GameOptions.Current.musicVolume, Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(GameOptions.Current.shake, Is.EqualTo(ShakeLevel.Low));
            Assert.That(GameOptions.Current.textSpeed, Is.EqualTo(TextSpeed.Instant));
            Assert.That(GameOptions.Current.relaxedTiming, Is.True);
        }

        [Test]
        public void AMissingOrDamagedFile_GivesTheDefaults_WithoutThrowing()
        {
            Assert.That(new OptionsStore(m_Dir).Load().masterVolume, Is.EqualTo(1f), "missing");
            Directory.CreateDirectory(m_Dir);
            File.WriteAllText(Path.Combine(m_Dir, OptionsStore.FileName), "{\"masterVolume\": 0.2, \"shak");
            Assert.DoesNotThrow(() => new OptionsStore(m_Dir).Load());
            Assert.That(new OptionsStore(m_Dir).Load().masterVolume, Is.EqualTo(1f), "damaged");
            File.WriteAllText(Path.Combine(m_Dir, OptionsStore.FileName), "\0\0\0");
            Assert.That(new OptionsStore(m_Dir).Load().masterVolume, Is.EqualTo(1f), "garbage");
        }

        [Test]
        public void TheOptions_AreNeverPartOfTheSave_WhichStaysAtVersion10()
        {
            Assert.That(SaveSystem.CurrentVersion, Is.EqualTo(10));
            var saveFields = typeof(SaveData).GetFields().Select(f => f.Name).ToList();
            foreach (var f in typeof(PlayerOptions).GetFields())
                if (f.Name != "version") Assert.That(saveFields, Has.No.Member(f.Name), $"{f.Name} is an option, not save data");
            Assert.That(OptionsStore.FileName, Is.Not.EqualTo(SaveStore.FileName));
        }

        [Test]
        public void AChange_ReachesTheGamesFeel_AtOnce()
        {
            GameOptions.Change(o =>
            {
                o.shake = ShakeLevel.Off;
                o.flashes = false;
                o.hitStop = false;
                o.vibration = false;
                o.vibrationIntensity = 0.5f;
                o.reducedVibration = true;
            });
            Assert.That(GameSettings.ScreenShakeScale, Is.EqualTo(0f));
            Assert.That(GameSettings.FlashEnabled || GameSettings.HitStopEnabled || GameSettings.VibrationEnabled, Is.False);
            Assert.That(GameSettings.VibrationIntensity, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(GameSettings.ReducedVibration, Is.True);
            GameOptions.Change(o => o.shake = ShakeLevel.Low);
            Assert.That(GameSettings.ScreenShakeScale, Is.EqualTo(OptionsRules.ShakeScale(ShakeLevel.Low)).And.GreaterThan(0f).And.LessThan(1f));
            GameOptions.ResetToDefaults();
            Assert.That(GameSettings.ScreenShakeScale, Is.EqualTo(1f));
            Assert.That(GameSettings.VibrationEnabled, Is.True);
        }

        // ---------- the mixer ----------

        [Test]
        public void TheMixer_HasMusicAndEffectsUnderMaster_EachAVolumeTheGameSets()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerBuilder.MixerPath);
            Assert.That(mixer, Is.Not.Null);
            var groups = mixer.FindMatchingGroups(string.Empty).Select(g => g.name).ToList();
            Assert.That(groups, Is.EquivalentTo(new[] { "Master", "Music", "Effects" }), "no UI group: interface sounds are effects (4i-B)");
            foreach (string p in new[] { AudioMixerHub.MasterVolume, AudioMixerHub.MusicVolume, AudioMixerHub.EffectsVolume })
                Assert.That(mixer.GetFloat(p, out _), Is.True, $"{p} is exposed");
        }

        [Test]
        public void EverySoundInThePrefabs_GoesThroughTheMixer()
        {
            List<string> unrouted = AudioRouting.Unrouted();
            Assert.That(unrouted, Is.Empty, string.Join("\n", unrouted));
        }

        [Test]
        public void EverySoundInTheGamesScenes_GoesThroughTheMixer_AndBootHasTheHub()
        {
            var problems = new List<string>();
            foreach (string path in new[] { BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene })
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects()) AudioRouting.Collect(root, Path.GetFileNameWithoutExtension(path), problems);
                    if (path == BootBuilder.BootScene)
                    {
                        AudioMixerHub hub = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AudioMixerHub>(true)).FirstOrDefault();
                        Assert.That(hub, Is.Not.Null, "Boot keeps the mixer hub");
                        Assert.That(hub.Mixer, Is.Not.Null);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void NoCodeLowersTheListener_TheHollowsQuarterDownIsTheMixers()
        {
            foreach (string file in Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (file.EndsWith("AudioMixerHub.cs")) continue;
                Assert.That(text.Contains("AudioListener.volume ="), Is.False, $"{file} sets AudioListener.volume");
            }
        }

        // ---------- the menus ----------

        [Test]
        public void Options_SitsBetweenNewGameAndControls_OnTheMainMenu_AndInThePauseMenu()
        {
            Scene menuScene = EditorSceneManager.OpenScene(BootBuilder.MainMenuScene, OpenSceneMode.Additive);
            try
            {
                MainMenuScreen menu = menuScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuScreen>(true)).Single();
                Assert.That(menu.OptionsButton, Is.Not.Null);
                Assert.That(menu.Options, Is.Not.Null);
                int newGame = menu.NewGameButton.transform.GetSiblingIndex();
                int options = menu.OptionsButton.transform.GetSiblingIndex();
                int controls = menu.ControlsButton.transform.GetSiblingIndex();
                Assert.That(newGame < options && options < controls, Is.True, $"order {newGame}, {options}, {controls}");
                Assert.That(menu.Options.Controls, Is.SameAs(menu.ControlsPage), "the controls tab is the 4i-A controls page");
            }
            finally
            {
                EditorSceneManager.CloseScene(menuScene, true);
            }
            Scene boot = EditorSceneManager.OpenScene(BootBuilder.BootScene, OpenSceneMode.Additive);
            try
            {
                PauseMenu pause = boot.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PauseMenu>(true)).Single();
                Assert.That(pause.OptionsButton, Is.Not.Null);
                Assert.That(pause.Options, Is.Not.Null);
                Assert.That(pause.Options.GetComponentsInChildren<OptionRow>(true).Length, Is.EqualTo(6));
            }
            finally
            {
                EditorSceneManager.CloseScene(boot, true);
            }
        }

        // ---------- relaxed timing and patient customers ----------

        static MinigameFactory Factory() => new(GrillSettings.Default, TapSettings.Default, ServingSettings.Default);

        static MinigameFactory Relaxed(MinigameFactory f) => AssistRules.Relaxed(f, OptionsRules.RelaxedBandScale, OptionsRules.RelaxedPace);

        [Test]
        public void RelaxedTiming_WidensEveryTarget_AboutItsCentre_AndSlowsThePace()
        {
            MinigameFactory plain = Factory(), relaxed = Relaxed(plain);
            Assert.That(relaxed.Grill.bandMax - relaxed.Grill.bandMin, Is.GreaterThan(plain.Grill.bandMax - plain.Grill.bandMin));
            Assert.That(relaxed.Grill.BandCenter, Is.EqualTo(plain.Grill.BandCenter).Within(1e-5f));
            Assert.That(relaxed.Grill.cookRate, Is.LessThan(plain.Grill.cookRate));
            Assert.That(relaxed.Tap.fillTolerance, Is.GreaterThan(plain.Tap.fillTolerance));
            Assert.That(relaxed.Tap.foamBandMax - relaxed.Tap.foamBandMin, Is.GreaterThan(plain.Tap.foamBandMax - plain.Tap.foamBandMin));
            Assert.That(relaxed.Tap.pourRate, Is.LessThan(plain.Tap.pourRate));
            Assert.That(relaxed.Chop.perfectDistance, Is.GreaterThan(plain.Chop.perfectDistance));
            Assert.That(relaxed.Chop.knifeSpeed, Is.LessThan(plain.Chop.knifeSpeed));
            Assert.That(relaxed.Chop.itemTimeLimit, Is.GreaterThan(plain.Chop.itemTimeLimit));
            Assert.That(relaxed.Butcher.perfectDistance, Is.GreaterThan(plain.Butcher.perfectDistance));
            Assert.That(relaxed.Butcher.timeLimit, Is.GreaterThan(plain.Butcher.timeLimit));
            Assert.That(relaxed.Serving, Is.EqualTo(plain.Serving), "serving has no timing to relax");
            Assert.That(plain.Grill.bandMin, Is.EqualTo(GrillSettings.Default.bandMin), "the ordinary factory (staff's) is untouched");
        }

        [Test]
        public void ABandWidened_StaysInsideTheMeter()
        {
            (float min, float max) = AssistRules.Widen(0.9f, 1f, 3f);
            Assert.That(min, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(max, Is.EqualTo(1f));
            (min, max) = AssistRules.Widen(0f, 0.2f, 3f);
            Assert.That(min, Is.EqualTo(0f));
        }

        [Test]
        public void RelaxedTiming_ScoresTheSameWayItAlwaysHas_NoPenalty()
        {
            // The same grill, stopped in the middle of its band: a perfect side either way. A stop just past the plain band's
            // edge is a worse side plainly and a perfect one relaxed: the score's rule is the same, only the target is wider.
            GrillSettings plain = GrillSettings.Default, relaxed = AssistRules.Relaxed(plain, OptionsRules.RelaxedBandScale, OptionsRules.RelaxedPace);
            Assert.That(GrillScore(plain, plain.BandCenter), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(GrillScore(relaxed, relaxed.BandCenter), Is.EqualTo(1f).Within(1e-4f), "perfect is still perfect: nothing is taken off");
            float justShort = plain.bandMin - 0.03f;
            Assert.That(GrillScore(relaxed, justShort), Is.GreaterThan(GrillScore(plain, justShort)));
        }

        static float GrillScore(GrillSettings s, float stopAt) => GrillMinigame.ScoreFlip(stopAt, s);

        [Test]
        public void PatientCustomers_WaitLonger_OnlyWhenAskedFor()
        {
            CustomerTraits traits = CustomerTraits.Default;
            CustomerTraits same = CustomerLogic.Patient(traits, OptionsRules.Patience(false));
            CustomerTraits patient = CustomerLogic.Patient(traits, OptionsRules.Patience(true));
            Assert.That(same.seatPatience, Is.EqualTo(traits.seatPatience));
            Assert.That(same.orderPatience, Is.EqualTo(traits.orderPatience));
            Assert.That(patient.seatPatience, Is.EqualTo(traits.seatPatience * OptionsRules.PatientScale).Within(1e-4f));
            Assert.That(patient.orderPatience, Is.EqualTo(traits.orderPatience * OptionsRules.PatientScale).Within(1e-4f));
            Assert.That(patient.orderDelay, Is.EqualTo(traits.orderDelay), "only their patience changes, not how they order");
        }

        // ---------- Esc through a fade ----------

        [Test]
        public void AnEscDuringAFade_IsKept_ForAMoment_AndOnlyThen()
        {
            Assert.That(PauseRules.Queues(inGame: true, loading: true), Is.True);
            Assert.That(PauseRules.Queues(inGame: true, loading: false), Is.False, "any other refusal (a panel, a conversation) is meant");
            Assert.That(PauseRules.Queues(inGame: false, loading: true), Is.False, "not on the way out to the menu");
            Assert.That(PauseRules.StillQueued(10f, 10f + PauseRules.QueueSeconds - 0.01f), Is.True);
            Assert.That(PauseRules.StillQueued(10f, 10f + PauseRules.QueueSeconds + 0.01f), Is.False);
            Assert.That(PauseRules.StillQueued(-1f, 0f), Is.False);
        }
    }
}
