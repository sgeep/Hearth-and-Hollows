using System.Collections;
using System.IO;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Scene;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-D, save compatibility in the game: real saves from 4f (version 7) and 4h/4i (version 10), and day-2 saves in version 8's
    /// and 9's shapes, Continue from the main menu to where they were, and save again as the current version.
    /// </summary>
    public class SaveContinueTests : BootFixture
    {
        const string Fixtures = "Assets/_Project/Tests/EditMode/Fixtures/Saves";

        IEnumerator ContinueFrom(string file)
        {
            Directory.CreateDirectory(SaveDir);
            File.Copy(Path.Combine(Fixtures, file), Path.Combine(SaveDir, SaveStore.FileName), true);
            yield return Boot();
            Assert.That(Flow.PeekSave(), Is.Not.Null, $"{file}: Continue is offered");
            Assert.That(Flow.Continue(), $"{file}: it continues");
        }

        [TestCase("v7_day2_daytime.json", ExpectedResult = null)]
        [TestCase("v8_day2_daytime.json", ExpectedResult = null)]
        [TestCase("v9_day2_daytime.json", ExpectedResult = null)]
        [TestCase("v10_day2_daytime.json", ExpectedResult = null)]
        [UnityTest]
        public IEnumerator ADaytimeSave_FromAnyVersion_ContinuesIntoTheDay_AndSavesAsTheCurrentVersion(string file)
        {
            yield return ContinueFrom(file);
            yield return WaitUntil(() => InDaytimeNow, 30f, "the daytime");
            yield return Revealed();
            Assert.That((Flow.State.Day, Director.Phase), Is.EqualTo((2, TavernPhase.Daytime)));
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Complete), "past the opening, with nothing replayed");
            Assert.That(Flow.State.Vigor.Current, Is.EqualTo(Flow.VigorSettings.maxVigor));
            Flow.Save();
            Assert.That(Flow.PeekSave().version, Is.EqualTo(SaveSystem.CurrentVersion));
        }

        /// <summary>Saves the external playtest's own build wrote (playtest-0.4i.1): every later build must continue them.</summary>
        [UnityTest]
        public IEnumerator TheTesterBuildsSaves_ContinueAtNight_AndIntoTheDay()
        {
            yield return ContinueFrom("v10_tester-0.4i.1_day4_night.json");
            yield return WaitUntil(() => IsIn(TavernPhase.Night), 30f, "the night");
            Assert.That(Flow.State.Day, Is.EqualTo(4));
            Flow.QuitToMenu();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.MainMenu, 20f, "the menu");
            File.Copy(Path.Combine(Fixtures, "v10_tester-0.4i.1_day5_daytime.json"), Path.Combine(SaveDir, SaveStore.FileName), true);
            yield return Revealed();
            Assert.That(Flow.Continue());
            yield return WaitUntil(() => InDaytimeNow, 30f, "the daytime");
            yield return Revealed();
            Assert.That(Flow.State.Day, Is.EqualTo(5));
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Complete));
        }

        [UnityTest]
        public IEnumerator AVersion7Save_AtTheNightsDelve_ContinuesIntoTheHollows()
        {
            yield return ContinueFrom("v7_day2_delve.json");
            yield return WaitUntil(() => InDungeonNow, 30f, "the delve");
            Assert.That((Flow.State.Day, Flow.State.Gold), Is.EqualTo((2, 25)));
        }

        [UnityTest]
        public IEnumerator A4hArrivalSave_ContinuesIntoArrivalDay_AndAFirstDelveSave_IntoTheHollows()
        {
            yield return ContinueFrom("v10_day1_arrival.json");
            yield return WaitUntil(() => IsIn(TavernPhase.Arrival), 30f, "arrival day");
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.Arrival));
            Flow.QuitToMenu();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.MainMenu, 20f, "the menu");
            File.Copy(Path.Combine(Fixtures, "v10_day1_firstdelve.json"), Path.Combine(SaveDir, SaveStore.FileName), true);
            yield return Revealed();
            Assert.That(Flow.Continue());
            yield return WaitUntil(() => InDungeonNow, 30f, "the first delve");
            Assert.That(Flow.State.Story.Opening, Is.EqualTo(OpeningStage.FirstDelve));
        }
    }
}
