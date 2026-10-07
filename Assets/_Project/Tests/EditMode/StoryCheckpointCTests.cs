using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Editor;
using Hearthdelve.Story.Relationships;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using SaveSystem = Hearthdelve.Shared.Save.SaveSystem;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4g Checkpoint C: the deed vocabulary, how Boog and Orik each read it, repeats, the relationship save, the hubs and callbacks
    /// in the dialogue database, and the old names staying out of player-facing text.
    /// </summary>
    public sealed class StoryCheckpointCTests
    {
        static StoryDatabase Story => AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database);
        static DialogueDatabase Dialogue => AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);
        static DeedDefinition Deed(string id) => Story.deeds.Single(d => d.id == id);
        static CharacterDefinition Boog => Story.characters.Single(c => c.id == CharacterIds.Boog);
        static CharacterDefinition Orik => Story.characters.Single(c => c.id == CharacterIds.Orik);

        static float RespectFrom(CharacterDefinition judge, DeedDefinition deed) =>
            RelationshipRules.RespectChange(deed.respect, RelationshipRules.Alignment(judge.values, deed.shows), 1f);

        // ---------- The vocabulary ----------

        [Test]
        public void TheDeeds_AreAFewRemarkableThings_EachFromItsOwnFact()
        {
            Assert.That(Story.deeds.Select(d => d.id), Is.EquivalentTo(new[]
            {
                StoryBuilder.DisplayedTrophy, StoryBuilder.ReturnedBoogsBomb, StoryBuilder.FelledLarderTroll, StoryBuilder.KeptAWish, StoryBuilder.FineButchery,
            }), "a compact vocabulary: not every gameplay event is a deed");
            Assert.That((Deed(StoryBuilder.FelledLarderTroll).source, Deed(StoryBuilder.FelledLarderTroll).subject), Is.EqualTo((DeedSource.BossFirstCleared, "larder_troll")));
            Assert.That((Deed(StoryBuilder.KeptAWish).source, Deed(StoryBuilder.KeptAWish).minimum), Is.EqualTo((DeedSource.RequestKept, 0.9f)));
            Assert.That((Deed(StoryBuilder.FineButchery).source, Deed(StoryBuilder.FineButchery).minimum), Is.EqualTo((DeedSource.PartButchered, 0.9f)));
            Assert.That(Deed(StoryBuilder.ReturnedBoogsBomb).source, Is.EqualTo(DeedSource.None), "only his conversation commits it");
            Assert.That(Deed(StoryBuilder.FelledLarderTroll).memoryDays, Is.Zero, "a boss's fall is remembered for good");
            Assert.That(Deed(StoryBuilder.KeptAWish).memoryDays, Is.GreaterThan(0), "a kept wish fades in time");
        }

        [Test]
        public void EachDeed_IsLearnedByThoseItConcerns()
        {
            CharacterDefinition[] cast = { Boog, Orik };
            IEnumerable<string> Learners(string deed) => RelationshipRules.Learners(Deed(deed), cast);
            Assert.That(Learners(StoryBuilder.FelledLarderTroll), Is.EquivalentTo(new[] { CharacterIds.Boog, CharacterIds.Orik }));
            Assert.That(Learners(StoryBuilder.KeptAWish), Is.EquivalentTo(new[] { CharacterIds.Boog, CharacterIds.Orik }));
            Assert.That(Learners(StoryBuilder.FineButchery), Is.EqualTo(new[] { CharacterIds.Boog }), "the block is Boog's business");
            Assert.That(Learners(StoryBuilder.ReturnedBoogsBomb), Is.EqualTo(new[] { CharacterIds.Boog }));
        }

        [Test]
        public void OnlyRemarkableFacts_MakeDeeds()
        {
            DeedDefinition troll = Deed(StoryBuilder.FelledLarderTroll), wish = Deed(StoryBuilder.KeptAWish), butchery = Deed(StoryBuilder.FineButchery);
            Assert.That(RelationshipRules.Qualifies(troll, DeedSource.BossFirstCleared, "larder_troll"));
            Assert.That(RelationshipRules.Qualifies(troll, DeedSource.BossFirstCleared, "frostvault_boss"), Is.False, "another boss isn't this deed");
            Assert.That(RelationshipRules.Qualifies(wish, DeedSource.RequestKept, "stew", 0.95f, RelationshipRules.Keeper));
            Assert.That(RelationshipRules.Qualifies(wish, DeedSource.RequestKept, "stew", 0.7f, RelationshipRules.Keeper), Is.False, "an ordinary request met isn't remarkable");
            Assert.That(RelationshipRules.Qualifies(butchery, DeedSource.PartButchered, "spider_leg", 0.92f, RelationshipRules.Keeper));
            Assert.That(RelationshipRules.Qualifies(butchery, DeedSource.PartButchered, "spider_leg", 0.92f, CharacterIds.Boog), Is.False, "Boog's own cuts aren't the keeper's");
            Assert.That(RelationshipRules.Qualifies(butchery, DeedSource.PartButchered, "spider_leg", 0.6f, RelationshipRules.Keeper), Is.False, "routine butchery");
            Assert.That(RelationshipRules.Qualifies(butchery, DeedSource.RequestKept, "spider_leg", 1f), Is.False, "the wrong fact");
        }

        // ---------- Two characters, one deed ----------

        [Test]
        public void BoogAndOrik_ReadTheSameDeeds_Differently()
        {
            DeedDefinition troll = Deed(StoryBuilder.FelledLarderTroll), wish = Deed(StoryBuilder.KeptAWish), tusks = Deed(StoryBuilder.DisplayedTrophy);
            float boogTroll = RespectFrom(Boog, troll), orikTroll = RespectFrom(Orik, troll);
            Assert.That(boogTroll, Is.GreaterThan(orikTroll * 1.5f), "the troll: Boog loudly (nerve), Orik quietly");
            Assert.That(orikTroll, Is.GreaterThan(3f), "but Orik does respect it");
            Assert.That(RespectFrom(Orik, wish), Is.GreaterThan(RespectFrom(Boog, wish)), "a wish kept matters more to Orik (warmth, follow-through)");
            Assert.That(RespectFrom(Orik, tusks), Is.Zero, "a grotesque trophy alone doesn't impress him");
            Assert.That(RespectFrom(Boog, Deed(StoryBuilder.FineButchery)), Is.GreaterThan(5f), "clean, nervy work at the block is Boog's kind of thing");
        }

        // ---------- Repeats ----------

        [Test]
        public void Repeats_FadeFast_AndStopCounting()
        {
            Assert.That(Enumerable.Range(0, 6).Select(RelationshipRules.RepeatFactor), Is.EqualTo(new[] { 1f, 0.5f, 0.25f, 0.1f, 0f, 0f }));
            float total = Enumerable.Range(0, 50).Sum(RelationshipRules.RepeatFactor);
            Assert.That(total, Is.LessThan(2f), "fifty repeats are worth less than two first times: no farming");
            AnimationCurve curve = RelationshipAdapter.RepeatCurve();
            for (int i = 0; i < 8; i++) Assert.That(curve.Evaluate(i), Is.EqualTo(RelationshipRules.RepeatFactor(i)).Within(1e-4f), $"Love/Hate's curve, count {i}");
        }

        // ---------- The save ----------

        [Test]
        public void NewDeedsMemories_RoundTripThroughTheSave_WithoutAVersionChange()
        {
            var state = new GameState(5, DayPhase.Night);
            state.Story.Relationships = new RelationshipData
            {
                memories =
                {
                    new SocialMemoryData { judge = "gunta", deed = StoryBuilder.FelledLarderTroll, actor = "player", target = "tavern", count = 1, impact = 30f, expires = RelationshipRules.Forever },
                    new SocialMemoryData { judge = "pip", deed = StoryBuilder.KeptAWish, actor = "player", target = "tavern", count = 2, impact = 15f, expires = 12.5f },
                    new SocialMemoryData { judge = "gunta", deed = StoryBuilder.ReturnedBoogsBomb, actor = "player", target = "gunta", count = 1, impact = 40f, expires = RelationshipRules.Forever },
                },
            };
            state.Story.Dialogue = "Variable={hh_boog_troll=true}";
            SaveData saved = SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state)));
            Assert.That(saved.version, Is.EqualTo(9), "Checkpoint C adds no schema: the save stays version 9");
            GameState back = SaveSystem.Restore(saved, _ => null, _ => true);
            Assert.That(back.Story.Relationships.memories.Select(m => (m.judge, m.deed, m.target, m.count, m.expires)), Is.EqualTo(new[]
            {
                ("gunta", StoryBuilder.FelledLarderTroll, "tavern", 1, RelationshipRules.Forever),
                ("pip", StoryBuilder.KeptAWish, "tavern", 2, 12.5f),
                ("gunta", StoryBuilder.ReturnedBoogsBomb, "gunta", 1, RelationshipRules.Forever),
            }));
            Assert.That(back.Story.Dialogue, Does.Contain("hh_boog_troll"), "a callback said stays said");
        }

        // ---------- The dialogue database ----------

        static IEnumerable<(Conversation c, DialogueEntry e, string code)> Code()
        {
            foreach (Conversation c in Dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
            foreach (string code in new[] { e.conditionsString, e.userScript })
                if (!string.IsNullOrEmpty(code)) yield return (c, e, code);
        }

        [Test]
        public void EveryMemoryAConversationAsksAbout_IsARealCharacterAndDeed()
        {
            var tracked = Story.characters.Where(c => c.tracked).Select(c => c.id).ToHashSet();
            var deeds = Story.deeds.Select(d => d.id).ToHashSet();
            int asked = 0;
            foreach (var (c, e, code) in Code())
            foreach (Match m in Regex.Matches(code, @"HH_Remembers\(""([^""]+)"",\s*""([^""]+)""\)"))
            {
                asked++;
                Assert.That(tracked, Does.Contain(m.Groups[1].Value), $"{c.Title} {e.id}: a character with memories");
                Assert.That(deeds, Does.Contain(m.Groups[2].Value), $"{c.Title} {e.id}: a deed");
            }
            Assert.That(asked, Is.GreaterThanOrEqualTo(8), "Boog and Orik remember several things");
            foreach (string deed in new[] { StoryBuilder.FelledLarderTroll, StoryBuilder.KeptAWish, StoryBuilder.FineButchery, StoryBuilder.ReturnedBoogsBomb, StoryBuilder.DisplayedTrophy })
                Assert.That(Code().Any(x => x.code.Contains($"\"{deed}\"")), $"some line remembers {deed}");
        }

        [Test]
        public void EveryOneTimeLine_MarksItselfSaid_AndIsReadUnderTheSameName()
        {
            var read = new HashSet<string>();
            var set = new HashSet<string>();
            foreach (var (c, e, code) in Code())
            {
                foreach (Match m in Regex.Matches(code, @"not Variable\[""(hh_[a-z_]+)""\]")) read.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(code, @"Variable\[""(hh_[a-z_]+)""\]\s*=\s*true")) set.Add(m.Groups[1].Value);
            }
            Assert.That(read, Is.Not.Empty);
            Assert.That(read, Is.EquivalentTo(set), "each once-only line sets the flag it checks");
        }

        [Test]
        public void TheHubs_PutTheQuestFirst_AndFallBackToTheEverydayConversation()
        {
            Assert.That(Boog.conversation, Is.EqualTo(StoryDialogue.BoogHub));
            Assert.That(Orik.conversation, Is.EqualTo(StoryDialogue.OrikHub));
            Conversation hub = Dialogue.GetConversation(StoryDialogue.BoogHub), bomb = Dialogue.GetConversation("Boog/Bomb"),
                talk = Dialogue.GetConversation(StoryDialogue.BoogTalk);
            List<Link> start = hub.GetDialogueEntry(0).outgoingLinks;
            DialogueEntry first = hub.GetDialogueEntry(start[0].destinationDialogueID);
            Assert.That(first.isGroup && first.conditionsString.Contains("HH_HasQuestObject"), "her return comes first");
            Assert.That(first.outgoingLinks.Select(l => (l.destinationConversationID, l.destinationDialogueID)), Is.EqualTo(new[] { (bomb.id, 0) }));
            Assert.That((start[start.Count - 1].destinationConversationID == talk.id) ||
                        hub.GetDialogueEntry(start[start.Count - 1].destinationDialogueID).outgoingLinks.Any(l => l.destinationConversationID == talk.id),
                "the last branch is Boog/Talk");
            // Everything the opening and the quest need is reachable whatever Boog remembers: the memory remarks above the bomb
            // offer in Boog/Talk are said once, never every time.
            foreach (DialogueEntry e in talk.dialogueEntries.Where(e => e.conditionsString != null && e.conditionsString.Contains("HH_Remembers")))
                Assert.That(e.conditionsString, Does.Contain("not Variable["), $"Boog/Talk {e.id} is said once");
        }

        // ---------- Old names ----------

        [Test]
        public void PlayerFacingText_UsesTheCurrentNames()
        {
            var old = new[] { "Pip", "Gunta", "Gundra", "Ashbelly", "Marrowby", "Brackenford", "Sunken Flagon" };
            var bad = new List<string>();
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            foreach (var table in collection.StringTables)
            foreach (var entry in table.Values)
            foreach (string name in old)
                if (!string.IsNullOrEmpty(entry.Value) && Regex.IsMatch(entry.Value, $@"\b{name}\b"))
                    bad.Add($"{collection.TableCollectionName}/{entry.Key}: {name}");
            foreach (Actor a in Dialogue.actors)
                if (old.Contains(a.Name)) bad.Add($"actor {a.Name}");
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }
    }
}
