using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Village;
using Hearthdelve.Story;
using Hearthdelve.Story.Editor;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4h Checkpoint C: the village's schedules. Pure resolution (broad beats, boundaries, conditions), the seeded days (Kaloren's
    /// herbs every third day, Ogrin's good and bad days), the authored schedules kept relaxed for a four-minute day, and the
    /// cast's data and first conversations.
    /// </summary>
    public class VillageScheduleTests
    {
        static readonly VillageLifeSettings k_Settings = VillageLifeSettings.Default;
        static ScheduleWorld World(int day, int seed = 12345) => new(day, seed, true, k_Settings);

        static ScheduleDefinition Authored(string character) =>
            AssetDatabase.LoadAssetAtPath<ScheduleDefinition>($"{VillageContent.Folder}/Schedule_{character}.asset")
            ?? throw new InvalidOperationException($"No schedule for {character} (run Update Surface).");

        static readonly string[] k_Villagers = { CharacterIds.Maximo, CharacterIds.Kaloren, CharacterIds.Grim, CharacterIds.Ogrin, CharacterIds.Bart, CharacterIds.Musashi };

        // ---------- resolution ----------

        static List<ScheduleBlock> FourBeats() => new()
        {
            new ScheduleBlock(480, 660, "a", "morning"),
            new ScheduleBlock(660, 840, "b", "midday"),
            new ScheduleBlock(840, 1020, "c", "afternoon"),
            new ScheduleBlock(1020, 1440, "d", "evening"),
        };

        [TestCase(480, "morning")]
        [TestCase(600, "morning")]
        [TestCase(659, "morning")]
        [TestCase(660, "midday")]
        [TestCase(839, "midday")]
        [TestCase(840, "afternoon")]
        [TestCase(1019, "afternoon")]
        [TestCase(1020, "evening")]
        [TestCase(1439, "evening")]
        public void ABlock_CoversItsBeat_FromItsStart_ToJustBeforeTheNext(int minute, string activity) =>
            Assert.That(ScheduleRules.Resolve(FourBeats(), World(1), minute).activity, Is.EqualTo(activity));

        [Test]
        public void BeforeTheDay_NoBlock_MeansNowhere() => Assert.That(ScheduleRules.Resolve(FourBeats(), World(1), 470), Is.Null);

        [Test]
        public void TheFirstMatchingBlock_Wins_SoASpecialDayGoesFirst()
        {
            var blocks = new List<ScheduleBlock>
            {
                new(570, 660, "cottage", "herbs", ScheduleCondition.On(DayRule.HerbDay)),
                new(480, 660, "tower", "tower"),
            };
            int herb = Enumerable.Range(1, 3).First(d => VillageDays.HerbDay(12345, d, k_Settings));
            Assert.That(ScheduleRules.Resolve(blocks, World(herb), 600).activity, Is.EqualTo("herbs"));
            Assert.That(ScheduleRules.Resolve(blocks, World(herb), 560).activity, Is.EqualTo("tower"), "before the visit");
            Assert.That(ScheduleRules.Resolve(blocks, World(herb + 1), 600).activity, Is.EqualTo("tower"), "not a herb day");
        }

        [Test]
        public void Conditions_CombineWithAnd_AndNegate()
        {
            var opening = new ScheduleCondition { kind = ScheduleConditionKind.OpeningComplete };
            var dayThree = ScheduleCondition.FromDay(3);
            var block = new List<ScheduleBlock> { new(480, 1440, "x", "y", opening, dayThree) };
            Assert.That(ScheduleRules.Resolve(block, new ScheduleWorld(3, 1, true, k_Settings), 500), Is.Not.Null);
            Assert.That(ScheduleRules.Resolve(block, new ScheduleWorld(2, 1, true, k_Settings), 500), Is.Null, "too early");
            Assert.That(ScheduleRules.Resolve(block, new ScheduleWorld(3, 1, false, k_Settings), 500), Is.Null, "arrival day");
            var notFive = new List<ScheduleBlock> { new(480, 1440, "x", "y", new ScheduleCondition { kind = ScheduleConditionKind.DayAtLeast, number = 5, negate = true }) };
            Assert.That(ScheduleRules.Resolve(notFive, World(4), 500), Is.Not.Null);
            Assert.That(ScheduleRules.Resolve(notFive, World(5), 500), Is.Null);
        }

        [Test]
        public void AQuestObjectCondition_ReadsItsStatus()
        {
            var block = new List<ScheduleBlock>
            {
                new(480, 1440, "x", "y", new ScheduleCondition { kind = ScheduleConditionKind.QuestObject, id = "boogs_bomb", status = "home" }),
            };
            Assert.That(ScheduleRules.Resolve(block, new ScheduleWorld(1, 1, true, k_Settings, id => id == "boogs_bomb" ? "home" : "none"), 500), Is.Not.Null);
            Assert.That(ScheduleRules.Resolve(block, new ScheduleWorld(1, 1, true, k_Settings, _ => "wanted"), 500), Is.Null);
        }

        // ---------- seeded days ----------

        [Test]
        public void TheHerbs_ComeExactlyEveryThirdDay_PlacedByTheWorldSeed()
        {
            foreach (int seed in new[] { 1, 7, 12345, -99, int.MaxValue })
            {
                var days = Enumerable.Range(1, 60).Where(d => VillageDays.HerbDay(seed, d, k_Settings)).ToList();
                Assert.That(days.Count, Is.EqualTo(20), $"seed {seed}");
                for (int i = 1; i < days.Count; i++) Assert.That(days[i] - days[i - 1], Is.EqualTo(3), $"seed {seed}");
            }
            var firsts = Enumerable.Range(0, 30).Select(seed => Enumerable.Range(1, 3).First(d => VillageDays.HerbDay(seed, d, k_Settings))).Distinct().Count();
            Assert.That(firsts, Is.GreaterThan(1), "different worlds start the herbs on different days");
        }

        [Test]
        public void TheVillage_IsTheSame_OnEveryLoadOfTheSameDay()
        {
            for (int day = 1; day <= 40; day++)
            {
                Assert.That(VillageDays.OgrinWell(4242, day, k_Settings), Is.EqualTo(VillageDays.OgrinWell(4242, day, k_Settings)));
                Assert.That(VillageDays.HerbDay(4242, day, k_Settings), Is.EqualTo(VillageDays.HerbDay(4242, day, k_Settings)));
                Assert.That(VillageDays.MaximoVigil(4242, day, k_Settings), Is.EqualTo(VillageDays.MaximoVigil(4242, day, k_Settings)));
            }
            // A pure function of its inputs: the hash doesn't depend on the process (System.HashCode would).
            Assert.That(VillageDays.Hash(1, 2, 3), Is.EqualTo(VillageDays.Hash(1, 2, 3)));
            Assert.That(VillageDays.Hash(1, 2, 3), Is.Not.EqualTo(VillageDays.Hash(1, 3, 3)));
        }

        [Test]
        public void Ogrin_HasGoodAndBadDays_TheHerbsHelp_AndNothingCuresHim()
        {
            int afterHerbs = 0, wellAfterHerbs = 0, ordinary = 0, wellOrdinary = 0;
            foreach (int seed in Enumerable.Range(1, 40))
            for (int day = 2; day <= 61; day++)
            {
                bool well = VillageDays.OgrinWell(seed, day, k_Settings);
                if (VillageDays.HerbDay(seed, day - 1, k_Settings)) { afterHerbs++; if (well) wellAfterHerbs++; }
                else { ordinary++; if (well) wellOrdinary++; }
            }
            float after = wellAfterHerbs / (float)afterHerbs, usual = wellOrdinary / (float)ordinary;
            Assert.That(after, Is.GreaterThan(usual), "the day after the herbs leans well");
            Assert.That(after, Is.LessThan(1f), "the herbs relieve; they never cure");
            Assert.That(usual, Is.InRange(0.4f, 0.8f), "most days are good, some are not");
            // Never a run of well days so long it reads as cured: in sixty days of every world, he has a bad day.
            foreach (int seed in Enumerable.Range(1, 40))
                Assert.That(Enumerable.Range(1, 60).Any(d => !VillageDays.OgrinWell(seed, d, k_Settings)), $"seed {seed}");
        }

        // ---------- the authored schedules ----------

        [Test]
        public void EveryVillager_HasASchedule_WhoseAnchorsExist()
        {
            var anchors = new HashSet<string>(VillageContent.KariastonAnchors.Select(a => a.id)) { VillageContent.TavernTable };
            foreach (string id in k_Villagers)
            {
                ScheduleDefinition s = Authored(id);
                Assert.That(s.character, Is.EqualTo(id));
                foreach (ScheduleBlock b in s.blocks) Assert.That(anchors, Does.Contain(b.anchor), $"{id}: {b}");
            }
            // Boog and Orik keep their posts in Tally Ho! (placed by their staff agents).
            Assert.That(Authored(CharacterIds.Boog).blocks.Single().anchor, Is.EqualTo(VillageContent.TavernKitchen));
            Assert.That(Authored(CharacterIds.Orik).blocks.Single().anchor, Is.EqualTo(VillageContent.TavernBar));
        }

        [Test]
        public void AFourMinuteDay_StaysRelaxed_AFewPlacesEach_LongStays_SomewhereAtFive()
        {
            foreach (string id in k_Villagers)
            foreach (int seed in new[] { 3, 11, 12345 })
            for (int day = 1; day <= 12; day++)
            {
                ScheduleWorld world = World(day, seed);
                List<ScheduleBlock> visits = ScheduleRules.Day(Authored(id).blocks, world, VillageContent.Morning, VillageContent.Evening);
                Assert.That(visits.Count, Is.InRange(1, 5), $"{id} day {day}: at most four moves between 8 and 5 ({string.Join(", ", visits)})");
                Assert.That(visits, Has.None.Null, $"{id} is somewhere all day");
                // Each stay lasts an hour of game time or more (about half a real minute at 0.43 s a minute).
                foreach (ScheduleBlock b in visits.Where(b => b.from < VillageContent.Evening)) Assert.That(Math.Min(b.to, VillageContent.Evening + 1) - Math.Max(b.from, VillageContent.Morning), Is.GreaterThanOrEqualTo(60), $"{id}: {b}");
                // Findable after five, when the clock rests.
                Assert.That(ScheduleRules.Resolve(Authored(id).blocks, world, VillageContent.Evening), Is.Not.Null, $"{id} at five");
            }
        }

        [Test]
        public void Kaloren_VisitsTheCottage_OnHerbDaysOnly_OnceEach()
        {
            ScheduleDefinition kaloren = Authored(CharacterIds.Kaloren);
            for (int day = 1; day <= 30; day++)
            {
                ScheduleWorld world = World(day);
                int visits = ScheduleRules.Day(kaloren.blocks, world, VillageContent.Morning, VillageContent.Evening).Count(b => b.activity == "herbs");
                Assert.That(visits, Is.EqualTo(VillageDays.HerbDay(world.Seed, day, k_Settings) ? 1 : 0), $"day {day}");
            }
        }

        [Test]
        public void Ogrin_StaysIn_OnABadDay_AndIsOut_OnAGoodOne()
        {
            ScheduleDefinition ogrin = Authored(CharacterIds.Ogrin);
            for (int day = 1; day <= 20; day++)
            {
                ScheduleWorld world = World(day);
                bool well = VillageDays.OgrinWell(world.Seed, day, k_Settings);
                var places = ScheduleRules.Day(ogrin.blocks, world, VillageContent.Morning, VillageContent.Evening - 1).Select(b => b.anchor).ToList();
                if (well) Assert.That(places, Has.None.EqualTo(VillageContent.OgrinWindow), $"day {day}: out");
                else Assert.That(places, Is.EqualTo(new[] { VillageContent.OgrinWindow }), $"day {day}: at his window all day");
            }
        }

        [Test]
        public void NothingAboutWhereVillagersStand_IsSaved()
        {
            // No position anywhere in the save, at any depth: the village is rebuilt from day + minute + story + world seed.
            var seen = new HashSet<Type>();
            void Check(Type t, string path)
            {
                if (!seen.Add(t) || t.IsPrimitive || t == typeof(string) || t.IsEnum) return;
                Assert.That(t, Is.Not.EqualTo(typeof(Vector2)).And.Not.EqualTo(typeof(Vector3)).And.Not.EqualTo(typeof(Vector2Int)), path);
                if (t.IsArray) { Check(t.GetElementType(), path + "[]"); return; }
                if (t.IsGenericType) { foreach (Type a in t.GetGenericArguments()) Check(a, path + "<>"); return; }
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.That(f.Name, Does.Not.Match("(?i)villager|schedule|npc"), $"{path}.{f.Name}");
                    Check(f.FieldType, $"{path}.{f.Name}");
                }
            }
            Check(typeof(SaveData), "SaveData");
        }

        // ---------- the cast ----------

        static readonly string[] k_NewCast = { CharacterIds.Maximo, CharacterIds.Kaloren, CharacterIds.Grim, CharacterIds.Ogrin, CharacterIds.Bart };

        static DialogueDatabase Dialogue => AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);

        [Test]
        public void EachNewVillager_HasAStableId_AName_APortrait_AHub_AndValues()
        {
            foreach (string id in k_NewCast)
            {
                var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{StoryPaths.Characters}/Character_{id}.asset");
                Assert.That(c, Is.Not.Null, id);
                Assert.That(c.id, Is.EqualTo(id));
                Assert.That(CharacterIds.IsAuthored(c.id), id);
                Assert.That(c.kind, Is.EqualTo(CharacterKind.Villager), id);
                Assert.That(c.tracked, id);
                Assert.That(c.displayName.TableEntryReference.Key, Is.EqualTo($"villager.{id}"), id);
                Assert.That(c.portrait, Is.Not.Null, id);
                Assert.That(c.portrait.still, Is.Not.Null, id);
                Assert.That(c.portrait.talking, Has.Length.EqualTo(5), id);
                Assert.That(Dialogue.GetConversation(c.conversation), Is.Not.Null, $"{id}: {c.conversation}");
                Assert.That(Dialogue.actors.Any(a => Field.LookupValue(a.fields, global::Hearthdelve.Story.Dialogue.DialogueAdapter.CharacterIdField) == id), $"{id} is a Dialogue System actor");
            }
        }

        [Test]
        public void EachHub_MeetsOnce_ThenGreetsByWhatTheyreDoing()
        {
            foreach (string title in new[] { StoryDialogue.MaximoHub, StoryDialogue.KalorenHub, StoryDialogue.GrimHub, StoryDialogue.OgrinHub, StoryDialogue.BartHub })
            {
                Conversation hub = Dialogue.GetConversation(title);
                Assert.That(hub, Is.Not.Null, title);
                DialogueEntry start = hub.dialogueEntries.First(e => e.id == 0);
                var first = hub.GetDialogueEntry(start.outgoingLinks[0].destinationDialogueID);
                Assert.That(first.conditionsString, Does.Contain("_met\"] ~= true"), $"{title}: the first meeting comes first, once");
                Assert.That(first.userScript, Does.Contain("_met\"] = true"), title);
                // Every once-only line checks and sets its own variable.
                foreach (DialogueEntry e in hub.dialogueEntries.Where(e => !string.IsNullOrEmpty(e.userScript) && e.userScript.Contains("Variable[")))
                {
                    string flag = Regex.Match(e.userScript, "Variable\\[\"(hh_[a-z_]+)\"\\]").Groups[1].Value;
                    Assert.That(e.conditionsString, Does.Contain($"Variable[\"{flag}\"] ~= true"), $"{title} [{e.id}]");
                }
                Assert.That(hub.dialogueEntries.Any(e => e.conditionsString != null && e.conditionsString.Contains("HH_Doing")), $"{title} reads the schedule");
                // The last way out of START has no condition: there's always something to say.
                DialogueEntry last = hub.GetDialogueEntry(start.outgoingLinks.Last().destinationDialogueID);
                Assert.That(string.IsNullOrEmpty(last.conditionsString), $"{title}: an unconditional greeting last");
            }
        }

        [Test]
        public void NoLine_HasOgrinCallGrimDad_TheBeatIsReserved()
        {
            var dad = new Regex(@"\b(dad|daddy|da|father|papa|pa)\b", RegexOptions.IgnoreCase);
            var bad = new List<string>();
            int ogrin = Dialogue.actors.First(a => a.Name == "Ogrin").id, grim = Dialogue.actors.First(a => a.Name == "Grim").id;
            foreach (Conversation c in Dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
                if ((e.ActorID == ogrin || e.ActorID == grim || e.ConversantID == ogrin || e.ConversantID == grim) && !string.IsNullOrEmpty(e.DialogueText) && dad.IsMatch(e.DialogueText))
                    bad.Add($"{c.Title} [{e.id}]: {e.DialogueText}");
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }

        [Test]
        public void TheMysteries_StayQuestions_InTheFirstDrafts()
        {
            // Restraint (PLAN_4H §18-20): no first draft names what Kaloren is, explains the seal, or says what Ogrin is.
            var spoilers = new Regex(@"\b(lich|phylactery|undead|sealing|the seal|warden)\b", RegexOptions.IgnoreCase);
            var bad = new List<string>();
            foreach (string title in new[] { StoryDialogue.MaximoHub, StoryDialogue.KalorenHub, StoryDialogue.GrimHub, StoryDialogue.OgrinHub, StoryDialogue.BartHub })
            foreach (DialogueEntry e in Dialogue.GetConversation(title).dialogueEntries)
                if (!string.IsNullOrEmpty(e.DialogueText) && spoilers.IsMatch(e.DialogueText)) bad.Add($"{title} [{e.id}]: {e.DialogueText}");
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }
    }
}
