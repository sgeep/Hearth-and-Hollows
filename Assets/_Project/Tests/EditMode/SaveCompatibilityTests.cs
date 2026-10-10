using System.Collections.Generic;
using System.IO;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4i-D, save compatibility: saves from every version since 4f load in 4i. The v7 and v10 files are real, written by the game
    /// (4f's captures; 4h and 4i's own play), and the v8 and v9 files are a real day-2 save in those versions' own shapes, the
    /// fields their versions didn't have yet removed (from SaveData at 48eec144 and milestone-4g). Each restores with no warnings,
    /// keeps what it had, and gains only what its version lacked.
    /// </summary>
    public class SaveCompatibilityTests
    {
        public const string Folder = "Assets/_Project/Tests/EditMode/Fixtures/Saves";

        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");

        public static IEnumerable<TestCaseData> Fixtures()
        {
            yield return new TestCaseData("v7_day2_delve.json", 7, 2, DayPhase.Delve, 25);
            yield return new TestCaseData("v7_day2_daytime.json", 7, 2, DayPhase.Daytime, 0);
            yield return new TestCaseData("v8_day2_daytime.json", 8, 2, DayPhase.Daytime, 0);
            yield return new TestCaseData("v9_day2_daytime.json", 9, 2, DayPhase.Daytime, 0);
            yield return new TestCaseData("v10_day1_arrival.json", 10, 1, DayPhase.Daytime, 0);
            yield return new TestCaseData("v10_day1_firstdelve.json", 10, 1, DayPhase.Delve, 0);
            yield return new TestCaseData("v10_day2_daytime.json", 10, 2, DayPhase.Daytime, 0);
        }

        [TestCaseSource(nameof(Fixtures))]
        public void EverySaveSince4f_LoadsIn4i_WithNothingLostOrInvented(string file, int version, int day, DayPhase phase, int gold)
        {
            string json = File.ReadAllText(Path.Combine(Folder, file));
            Assert.That(SaveSystem.FromJson(json).version, Is.EqualTo(SaveSystem.CurrentVersion), "migrated to the current version");
            StringAssert.Contains($"\"version\": {version}", json, "the fixture is the version it claims");
            SaveCheck check = SaveSystem.Check(json);
            Assert.That(check.Usable, $"{file}: {check.Problem} {check.Detail}");

            var warnings = new List<string>();
            GameState state = SaveSystem.Restore(check.Data, Database, warnings);
            Assert.That(warnings, Is.Empty, string.Join("\n", warnings));
            Assert.That((state.Day, state.Phase, state.Gold), Is.EqualTo((day, phase, gold)), "where the save was");
            Assert.That(state.Story.CreationComplete, "every save since 4g is past creation; older ones are a legacy keeper");
            if (version <= 8) Assert.That(state.Story.Opening, Is.EqualTo(OpeningStage.Complete), "a save from before the opening never plays it");
            if (version <= 9)
            {
                Assert.That(state.WorldSeed, Is.EqualTo(WorldSeed.From(json)), "version 10's seed, derived from the old file itself");
                Assert.That(state.Garden.Bed("garden_1").Crop, Is.Empty.Or.Null, "the beds start empty");
            }

            // And it saves again as the current version, the same game.
            SaveData again = SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state)));
            Assert.That((again.version, again.day, again.gold), Is.EqualTo((SaveSystem.CurrentVersion, day, gold)));
            GameState back = SaveSystem.Restore(again, Database, warnings);
            Assert.That(warnings, Is.Empty);
            Assert.That((back.WorldSeed, back.Story.Opening, back.Furniture.OwnedCount("butcher_block")),
                Is.EqualTo((state.WorldSeed, state.Story.Opening, state.Furniture.OwnedCount("butcher_block"))), "nothing granted twice");
        }

        [Test]
        public void TheV10Saves_AreUntouchedByLoading()
        {
            foreach (string file in new[] { "v10_day1_arrival.json", "v10_day1_firstdelve.json", "v10_day2_daytime.json" })
            {
                string json = File.ReadAllText(Path.Combine(Folder, file));
                SaveData data = SaveSystem.FromJson(json);
                GameState state = SaveSystem.Restore(data, Database);
                SaveData again = SaveSystem.Capture(state);
                Assert.That(again.world.seed, Is.EqualTo(data.world.seed), file);
                Assert.That(again.surface.minute, Is.EqualTo(data.surface.minute), file);
                Assert.That(again.story.openingStage, Is.EqualTo(data.story.openingStage), file);
                Assert.That(again.garden.beds.Count, Is.EqualTo(data.garden.beds.Count), file);
            }
        }
    }
}
