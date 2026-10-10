using System.Collections;
using System.Linq;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Tavern.Scene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-D's performance pass: on the web a tune is decoded once and kept (unloading never freed the browser's copy and loading
    /// decoded another: about 200 MB more each day); everywhere else a stopped tune is unloaded, as before.
    /// </summary>
    public class MusicMemoryTests : BootFixture
    {
        bool m_Keep;

        [SetUp]
        public void RememberThePlatformsChoice() => m_Keep = MusicDirector.KeepDecoded;

        [TearDown]
        public void PutItBack() => MusicDirector.KeepDecoded = m_Keep;

        IEnumerator TheDaysTuneStartsThenStops(System.Action<AudioClip> stopped)
        {
            yield return StartDaytime(gimpSeen: true, hold: true);
            var music = MusicDirector.Instance;
            AudioClip day = music.Config.Clip(MusicCue.Day);
            SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
            yield return WaitUntil(() => music.GetComponents<AudioSource>().Any(s => s.clip == day && s.isPlaying), 10f, "the day's tune");
            SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            yield return WaitUntil(() => !music.GetComponents<AudioSource>().Any(s => s.clip == day && s.isPlaying), 10f, "quiet indoors");
            yield return Frames(3);
            stopped(day);
        }

        [UnityTest]
        public IEnumerator OnTheWeb_AStoppedTune_StaysDecoded()
        {
            MusicDirector.KeepDecoded = true;
            AudioClip day = null;
            yield return TheDaysTuneStartsThenStops(c => day = c);
            Assert.That(day.loadState, Is.EqualTo(AudioDataLoadState.Loaded), "kept, so it's never decoded twice");
        }

        [UnityTest]
        public IEnumerator Elsewhere_AStoppedTune_IsUnloaded()
        {
            MusicDirector.KeepDecoded = false;
            AudioClip day = null;
            yield return TheDaysTuneStartsThenStops(c => day = c);
            Assert.That(day.loadState, Is.EqualTo(AudioDataLoadState.Unloaded), "its memory given back");
        }
    }
}
