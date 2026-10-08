using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Village;
using Hearthdelve.Story.Editor;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4h Checkpoint D: Gimp's first night (when it's due, once), his irregular visits, the overheard exchanges' content, familiar
    /// faces at dinner, Glimmer's evenings, and the restraint the story asks for (Gimp and Maximo stay a question).
    /// </summary>
    public class CommunityTests
    {
        static readonly VillageLifeSettings k_Settings = VillageLifeSettings.Default;
        static DialogueDatabase Dialogue => AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);

        // ---------- Gimp's first night ----------

        [TestCase(true, 3, false, 480, true, TestName = "Due_TheFirstMorningAfterAnOwnDelve")]
        [TestCase(true, 7, false, 485, true, TestName = "Due_Later_ForASaveThatSkippedIt")]
        [TestCase(true, 2, false, 480, false, TestName = "NotDue_TheFirstFreeDay")]
        [TestCase(false, 3, false, 480, false, TestName = "NotDue_DuringTheOpening")]
        [TestCase(true, 3, true, 480, false, TestName = "NotDue_Again")]
        [TestCase(true, 3, false, 760, false, TestName = "NotDue_OnAMiddayContinue")]
        public void GimpsNight(bool openingComplete, int day, bool seen, int minute, bool due) =>
            Assert.That(CommunityRules.GimpIntroDue(openingComplete, day, seen, minute, 480), Is.EqualTo(due));

        [Test]
        public void TheNightScene_IsQuiet() =>
            Assert.That(MusicRules.Pick(true, DayPhase.Daytime, MusicCue.Silence), Is.EqualTo(MusicCue.None));

        // ---------- his visits ----------

        [Test]
        public void GimpVisits_AreIrregular_NeverTwoDaysRunning_AndTheSameOnEveryLoad()
        {
            foreach (int seed in new[] { 1, 42, 12345, -7 })
            {
                var days = Enumerable.Range(1, 120).Where(d => VillageDays.GimpVisit(seed, d, k_Settings)).ToList();
                Assert.That(days.Count, Is.InRange(20, 50), $"seed {seed}: now and then");
                for (int i = 1; i < days.Count; i++) Assert.That(days[i] - days[i - 1], Is.GreaterThan(1), $"seed {seed}: never two days running");
                var gaps = Enumerable.Range(1, days.Count - 1).Select(i => days[i] - days[i - 1]).Distinct().Count();
                Assert.That(gaps, Is.GreaterThanOrEqualTo(3), $"seed {seed}: no clock to set by him");
                foreach (int d in days) Assert.That(VillageDays.GimpVisit(seed, d, k_Settings), $"seed {seed} day {d}: the same on every load");
            }
        }

        [Test]
        public void GimpsSchedule_NeedsHisNight_AndHisDay()
        {
            var schedule = AssetDatabase.LoadAssetAtPath<ScheduleDefinition>($"{VillageContent.Folder}/Schedule_gimp.asset");
            Assert.That(schedule, Is.Not.Null);
            ScheduleBlock visit = schedule.blocks.Single();
            Assert.That(visit.anchor, Is.EqualTo(VillageContent.TavernGimp));
            int day = Enumerable.Range(2, 30).First(d => VillageDays.GimpVisit(9, d, k_Settings));
            var before = new ScheduleWorld(day, 9, true, k_Settings, null, _ => false);
            var after = new ScheduleWorld(day, 9, true, k_Settings, null, b => b == CommunityRules.GimpIntro);
            Assert.That(ScheduleRules.Resolve(schedule, before, 15 * 60), Is.Null, "not before he's met the keeper");
            Assert.That(ScheduleRules.Resolve(schedule, after, 15 * 60)?.activity, Is.EqualTo("boog"));
            Assert.That(ScheduleRules.Resolve(schedule, after, 17 * 60), Is.Null, "gone before the evening");
            var otherDay = new ScheduleWorld(day + 1, 9, true, k_Settings, null, b => b == CommunityRules.GimpIntro);
            Assert.That(ScheduleRules.Resolve(schedule, otherDay, 15 * 60), Is.Null, "not the next day");
        }

        // ---------- dinner and evenings ----------

        [Test]
        public void FamiliarFaces_AreNoneToTwo_EachOnce_TheSameEveningOnReload()
        {
            var candidates = new List<CommunityRules.Patron>
            {
                new(CharacterIds.Maximo, 0.35f), new(CharacterIds.Bart, 0.25f), new(CharacterIds.Grim, 0.2f), new(CharacterIds.Musashi, 0.15f), new(CharacterIds.Kaloren, 0.1f),
            };
            int none = 0, some = 0;
            for (int day = 1; day <= 200; day++)
            {
                List<string> tonight = CommunityRules.Tonight(77, day, candidates);
                Assert.That(tonight.Count, Is.LessThanOrEqualTo(2));
                Assert.That(tonight, Is.Unique);
                Assert.That(tonight, Is.EqualTo(CommunityRules.Tonight(77, day, candidates)), "a reload keeps the evening");
                if (tonight.Count == 0) none++;
                else some++;
            }
            Assert.That(none, Is.GreaterThan(30), "not every night");
            Assert.That(some, Is.GreaterThan(80), "but often enough to notice");
        }

        [Test]
        public void TheNamedPatrons_AreThePeopleOfKariaston_InTheirOwnLooks()
        {
            var tavern = AssetDatabase.LoadAssetAtPath<Hearthdelve.Tavern.Scene.TavernContent>(EditorPaths.Data + "/Tavern/TavernContent.asset");
            Assert.That(tavern.namedPatrons.Select(p => p.character),
                Is.EquivalentTo(new[] { CharacterIds.Maximo, CharacterIds.Bart, CharacterIds.Grim, CharacterIds.Musashi, CharacterIds.Kaloren }));
            foreach (var p in tavern.namedPatrons)
            {
                Assert.That(p.profile, Is.Not.Null, p.character);
                Assert.That(p.layers, Is.Not.Empty.And.All.Not.Null, p.character);
                Assert.That(p.layers.Length, Is.LessThanOrEqualTo(6), $"{p.character}: the customer has six layers");
                Assert.That(p.shadow, Is.Not.Null, p.character);
            }
        }

        [Test]
        public void GlimmersEvenings_AreNowAndThen_AndTheSameOnReload()
        {
            int lit = Enumerable.Range(1, 200).Count(d => VillageDays.GlimmerEvening(5, d, k_Settings));
            Assert.That(lit, Is.InRange(25, 80));
            for (int d = 1; d <= 30; d++) Assert.That(VillageDays.GlimmerEvening(5, d, k_Settings), Is.EqualTo(VillageDays.GlimmerEvening(5, d, k_Settings)));
        }

        // ---------- what's said ----------

        static IEnumerable<DialogueEntry> Lines(string title) =>
            Dialogue.GetConversation(title).dialogueEntries.Where(e => !string.IsNullOrEmpty(e.DialogueText));

        [Test]
        public void GimpsNight_AsksAfterPhi_ExplainsTheArrangement_MentionsBoog_AndExplainsNothingElse()
        {
            string all = string.Join("\n", Lines(StoryDialogue.GimpIntruder).Select(e => e.DialogueText));
            StringAssert.Contains("where's Phi?", all);
            StringAssert.Contains("arrangement", all);
            StringAssert.Contains("Boog", all);
            StringAssert.Contains("come back up", all, "he knows the Hollows");
            Assert.That(Regex.IsMatch(all, @"\b(Maximo|Karias|seal|sealing|war|ancient|centur|hundred years|lich|Glimmer)", RegexOptions.IgnoreCase), Is.False,
                "nothing of his past or the village's");
            Conversation night = Dialogue.GetConversation(StoryDialogue.GimpIntruder);
            Assert.That(night.dialogueEntries.Count(e => Dialogue.GetActor(e.ActorID)?.IsPlayer == true && !string.IsNullOrEmpty(e.DialogueText)),
                Is.GreaterThanOrEqualTo(4), "the keeper gets to question him");
        }

        [Test]
        public void GimpDetestsMaximo_AndWontSayWhy()
        {
            string hub = string.Join("\n", Lines(StoryDialogue.GimpHub).Select(e => e.DialogueText));
            string boog = string.Join("\n", Lines(StoryDialogue.AmbientGimpBoog).Select(e => e.DialogueText));
            StringAssert.Contains("don't say that name", hub);
            StringAssert.Contains("drop it", hub, "he refuses to elaborate");
            StringAssert.Contains("don't say that name", boog);
            Assert.That(Regex.IsMatch(hub + "\n" + boog, @"\b(seal|sealing|Karias|war|fought|beside|together|centur|hundred years|old friend|alive)", RegexOptions.IgnoreCase), Is.False,
                "the reason stays a question (GDD open story question)");
        }

        [Test]
        public void EveryOverheardLine_FitsItsBubble_AndEveryExchangeHasSomethingToSay()
        {
            var problems = new List<string>();
            foreach (Conversation c in Dialogue.conversations.Where(c => c.Title.StartsWith("Ambient/")))
            {
                DialogueEntry start = c.dialogueEntries.First(e => e.id == 0);
                Assert.That(c.GetDialogueEntry(start.outgoingLinks.Last().destinationDialogueID).conditionsString, Is.Null.Or.Empty, $"{c.Title}: an everyday exchange last");
                foreach (DialogueEntry e in c.dialogueEntries.Where(e => !string.IsNullOrEmpty(e.DialogueText)))
                {
                    string text = StoryDialogue.Readable(e.DialogueText);
                    if (StoryTests.Lines(text, StoryScene.BarkTextWidth) > 2) problems.Add($"{c.Title} {e.id}: \"{text}\" takes more than two lines");
                    Assert.That(Dialogue.GetActor(e.ActorID)?.IsPlayer, Is.Not.True, $"{c.Title} {e.id}: the keeper only overhears");
                }
            }
            Assert.That(Dialogue.conversations.Count(c => c.Title.StartsWith("Ambient/")), Is.EqualTo(7));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void Gimp_IsAHollower_WithAHalfElfPortrait_AndHisOwnHub()
        {
            var gimp = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{StoryPaths.Characters}/Character_gimp.asset");
            Assert.That(gimp, Is.Not.Null);
            Assert.That(gimp.kind, Is.EqualTo(CharacterKind.Hollower));
            Assert.That(gimp.conversation, Is.EqualTo(StoryDialogue.GimpHub));
            Assert.That(gimp.portrait, Is.Not.Null);
            Assert.That(gimp.affinityToPlayer, Is.LessThan(0f), "standoffish from the start");
        }

        [Test]
        public void OgrinsLight_IsMentionedOnlyOnItsEvenings_AndNeverExplained()
        {
            DialogueEntry light = Lines(StoryDialogue.OgrinHub).Single(e => e.DialogueText.StartsWith("my light came again"));
            StringAssert.Contains("HH_Today(\"glimmer\")", light.conditionsString);
            Assert.That(Regex.IsMatch(light.DialogueText, @"\b(Glimmer|fairy|spirit|guardian|seal)", RegexOptions.IgnoreCase), Is.False);
        }
    }
}
