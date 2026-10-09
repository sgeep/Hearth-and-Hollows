using Hearthdelve.Editor;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>The background music (2026-10-07): which tune each moment plays, Decorate Mode's hold, and the tracks' import.</summary>
    public class MusicTests
    {
        [TearDown]
        public void ClearHolds() => MusicHolds.Clear();

        // 2026-10-08 (the owner's call): the day's tune only outdoors in Kariaston; the evening's only once service has begun.
        [TestCase(DayPhase.Daytime, true, false, MusicCue.Day)]
        [TestCase(DayPhase.Daytime, false, false, MusicCue.None)]
        [TestCase(DayPhase.Evening, false, true, MusicCue.Service)]
        [TestCase(DayPhase.Evening, false, false, MusicCue.None)]
        [TestCase(DayPhase.Delve, false, false, MusicCue.Cellars)]
        [TestCase(DayPhase.Night, true, false, MusicCue.None)]
        public void EachPartOfTheDay_HasItsTune(DayPhase phase, bool outdoors, bool serving, MusicCue cue) =>
            Assert.That(MusicRules.Pick(true, phase, MusicCue.None, outdoors: outdoors, serving: serving), Is.EqualTo(cue));

        [Test]
        public void TheEveningsTune_IsServiceAndItsResults_NeverPrep()
        {
            Assert.That(MusicRules.IsServing("Service"), Is.True);
            Assert.That(MusicRules.IsServing("Results"), Is.True);
            Assert.That(MusicRules.IsServing("Prep"), Is.False);
            Assert.That(MusicRules.IsServing(null), Is.False);
        }

        [Test]
        public void ArrivalDay_IsQuiet_TheFirstMusicIsTheHollows()
        {
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicCue.None, arriving: true), Is.EqualTo(MusicCue.None));
            Assert.That(MusicRules.Pick(true, DayPhase.Delve, MusicCue.None), Is.EqualTo(MusicCue.Cellars));
        }

        [Test]
        public void EffectsAreAQuarterDown_InTheHollowsOnly()
        {
            Assert.That(MusicRules.EffectsLevel(true, DayPhase.Delve, 0.75f), Is.EqualTo(0.75f));
            Assert.That(MusicRules.EffectsLevel(true, DayPhase.Daytime, 0.75f), Is.EqualTo(1f));
            Assert.That(MusicRules.EffectsLevel(true, DayPhase.Evening, 0.75f), Is.EqualTo(1f));
            Assert.That(MusicRules.EffectsLevel(false, DayPhase.Delve, 0.75f), Is.EqualTo(1f));
            // The 4i-C playtest: effects a quarter down everywhere, and the Hollows' quarter on top.
            Assert.That(MusicRules.EffectsLevel(true, DayPhase.Daytime, 0.75f, 0.75f), Is.EqualTo(0.75f));
            Assert.That(MusicRules.EffectsLevel(true, DayPhase.Delve, 0.75f, 0.75f), Is.EqualTo(0.5625f).Within(1e-5f));
        }

        [Test]
        public void TheMenu_IsQuiet() => Assert.That(MusicRules.Pick(false, DayPhase.Daytime, MusicCue.Decorate), Is.EqualTo(MusicCue.None));

        [Test]
        public void DecorateMode_HoldsItsTune_AndTheDayResumesAfter()
        {
            var decorate = new object();
            MusicHolds.Hold(decorate, MusicCue.Decorate);
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicHolds.Current), Is.EqualTo(MusicCue.Decorate));
            Assert.That(MusicRules.PausesFor(MusicCue.Day, MusicCue.Decorate), "the day's tune waits, paused");
            Assert.That(MusicRules.PausesFor(MusicCue.Day, MusicCue.Service), Is.False, "the evening starts the day's afresh tomorrow");
            MusicHolds.Release(decorate);
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicHolds.Current), Is.EqualTo(MusicCue.None), "Tally Ho! is quiet again");
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicHolds.Current, outdoors: true), Is.EqualTo(MusicCue.Day), "and Kariaston has its tune");
        }

        [Test]
        public void EveryCue_HasAVorbisTrack_ThatLoops_AQuarterDown()
        {
            var config = AssetDatabase.LoadAssetAtPath<MusicConfig>(MusicContent.ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.volume, Is.EqualTo(0.140625f).Within(1e-5f), "the owner's 25% as the new 100% (4i-C playtest)");
            Assert.That(config.effects, Is.EqualTo(0.75f).Within(1e-4f), "effects a quarter down (4i-C playtest)");
            // The owner's Quirkii at 0.75; 4i-C's balance pass evened the rest by their measured loudness (Coastal Market about
            // 1.5 dB hot, Otherworld about 4 dB; Continue is already quieter).
            Assert.That(config.Level(MusicCue.Day), Is.EqualTo(0.6375f).Within(1e-4f), "Quirkii a quarter under the rest, then 15% down (playtest 2)");
            Assert.That(config.Level(MusicCue.Decorate), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(config.Level(MusicCue.Service), Is.EqualTo(0.85f).Within(1e-4f));
            Assert.That(config.Level(MusicCue.Cellars), Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(MusicRules.Duck(true, config.duckUnderDialogue), Is.EqualTo(0.7f).Within(1e-4f), "about 3 dB down while someone talks");
            Assert.That(MusicRules.Duck(false, config.duckUnderDialogue), Is.EqualTo(1f));
            Assert.That(config.effectsInHollows, Is.EqualTo(0.75f).Within(1e-4f));
            foreach (MusicCue cue in new[] { MusicCue.Day, MusicCue.Decorate, MusicCue.Service, MusicCue.Cellars })
            {
                AudioClip clip = config.Clip(cue);
                Assert.That(clip, Is.Not.Null, cue.ToString());
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), cue.ToString());
                Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False, $"{cue}: loaded only when it plays");
            }
        }
    }
}
