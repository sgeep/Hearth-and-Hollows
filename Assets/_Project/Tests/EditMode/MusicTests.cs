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

        [TestCase(DayPhase.Daytime, MusicCue.Day)]
        [TestCase(DayPhase.Evening, MusicCue.Service)]
        [TestCase(DayPhase.Delve, MusicCue.Cellars)]
        [TestCase(DayPhase.Night, MusicCue.None)]
        public void EachPartOfTheDay_HasItsTune(DayPhase phase, MusicCue cue) => Assert.That(MusicRules.Pick(true, phase, MusicCue.None), Is.EqualTo(cue));

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
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicHolds.Current), Is.EqualTo(MusicCue.Day));
        }

        [Test]
        public void EveryCue_HasAVorbisTrack_ThatLoops_AQuarterDown()
        {
            var config = AssetDatabase.LoadAssetAtPath<MusicConfig>(MusicContent.ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.volume, Is.EqualTo(0.5625f).Within(1e-4f), "a quarter down, twice");
            Assert.That(config.Level(MusicCue.Day), Is.EqualTo(0.75f).Within(1e-4f), "Quirkii a quarter under the rest");
            foreach (MusicCue other in new[] { MusicCue.Decorate, MusicCue.Service, MusicCue.Cellars })
                Assert.That(config.Level(other), Is.EqualTo(1f).Within(1e-4f), $"{other} at the music volume");
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
