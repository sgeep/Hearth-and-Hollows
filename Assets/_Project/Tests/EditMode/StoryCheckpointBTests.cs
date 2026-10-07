using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Quests;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.Story.Editor;
using Hearthdelve.UI.Localization;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using PixelCrushers.QuestMachine;
using UnityEditor;
using UnityEngine;
using SaveSystem = Hearthdelve.Shared.Save.SaveSystem;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4g Checkpoint B: the keeper's looks and name, the save's new story state and its migration, the opening's stages, the quest
    /// object's rules, the bomb's content, and the boundary between the story tooling and hand-authored dialogue.
    /// </summary>
    public sealed class StoryCheckpointBTests
    {
        static KeeperLooks Looks => AssetDatabase.LoadAssetAtPath<KeeperLooks>(KeeperContent.LooksPath);
        static DialogueDatabase Dialogue => AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);

        // ---------- The keeper ----------

        [TestCase(null, "Bram")]
        [TestCase("", "Bram")]
        [TestCase("   ", "Bram")]
        [TestCase("Wren", "Wren")]
        [TestCase("  Mae   Rose ", "Mae Rose")]
        [TestCase("O'Dell-Fyn", "O'Dell-Fyn")]
        [TestCase("Ash7!#", "Ash")]
        [TestCase("Bartholomewssonius", "Bartholomewssoni")]
        [TestCase("Žofie", "Žofie")]
        public void AName_IsKeptAsTyped_WithinItsRules(string typed, string kept) => Assert.That(KeeperRules.CleanName(typed), Is.EqualTo(kept));

        [Test]
        public void Choices_StepRoundInBothDirections()
        {
            Assert.That(KeeperRules.Step(0, -1, 4), Is.EqualTo(3));
            Assert.That(KeeperRules.Step(3, 1, 4), Is.EqualTo(0));
            Assert.That(KeeperRules.Step(1, 1, 4), Is.EqualTo(2));
            Assert.That(KeeperRules.Step(0, 1, 0), Is.Zero);
        }

        static readonly CharacterAnim[] k_KeeperAnims =
        {
            CharacterAnim.Idle, CharacterAnim.Walk, CharacterAnim.Attack, CharacterAnim.Hurt, CharacterAnim.Dodge, CharacterAnim.Die,
            CharacterAnim.Charge, CharacterAnim.ChargeHold, CharacterAnim.HeavyAttack,
        };

        [Test]
        public void EveryBody_IsAWholeFigure_WithEveryAnimationTheKeeperPlays()
        {
            KeeperLooks looks = Looks;
            Assert.That(looks, Is.Not.Null, "run Hearthdelve → Story → Update Story Content");
            Assert.That(looks.bodies.Select(b => b.id), Is.EqualTo(new[] { "townsfolk", "amazon", "dwarf", "orc" }));
            foreach (KeeperBody body in looks.bodies)
            {
                Assert.That(body.animations != null && body.shadow != null, body.id);
                foreach (CharacterAnim action in k_KeeperAnims)
                {
                    SpriteAnim anim = body.animations.Find(action);
                    Assert.That(anim, Is.Not.Null, $"{body.id} {action}");
                    Assert.That(anim.frontRight.Length > 0 && anim.frontRight.All(f => f != null), $"{body.id} {action}: frames");
                    Assert.That(body.shadow.Find(action), Is.Not.Null, $"{body.id} {action}: shadow");
                }
                // Walking and idling are drawn for all four facings.
                foreach (CharacterAnim action in new[] { CharacterAnim.Idle, CharacterAnim.Walk })
                {
                    SpriteAnim anim = body.animations.Find(action);
                    Assert.That(new[] { anim.frontLeft, anim.backRight, anim.backLeft }.All(f => f.Length == anim.frontRight.Length && f.All(s => s != null)),
                        $"{body.id} {action}: four facings");
                }
            }
        }

        [Test]
        public void EveryBody_IsRecolouredOnlyThroughMinifantasyRamps_AndItsOwnColoursAreAChoice()
        {
            KeeperLooks looks = Looks;
            foreach (KeeperBody body in looks.bodies)
            {
                Assert.That(body.channels.Select(c => c.kind), Has.Some.EndsWith("skin").And.Some.EqualTo("keeper_hair").And.Some.EqualTo("keeper_outfit"), body.id);
                Texture2D sheet = body.animations.Find(CharacterAnim.Idle).frontRight[0].texture;
                Assert.That(sheet.isReadable, $"{body.id}: its sheets are imported readable, so they can be recoloured");
                var drawn = new HashSet<int>(sheet.GetPixels32().Where(p => p.a > 0).Select(p => (p.r << 16) | (p.g << 8) | p.b));
                foreach (PaletteChannel channel in body.channels)
                {
                    // Each channel's colours are really in the drawing.
                    foreach (Color32 c in channel.source)
                        Assert.That(drawn.Contains((c.r << 16) | (c.g << 8) | c.b), $"{body.id} {channel.kind}: #{c.r:x2}{c.g:x2}{c.b:x2} is in the drawing");
                    // The body as drawn is one of the choices, and choosing it changes nothing.
                    PalettePick pick = body.drawnAs.Single(p => p.kind == channel.kind);
                    PaletteRamp ramp = looks.palettes.Ramp(pick.ramp);
                    Assert.That(ramp, Is.Not.Null, $"{body.id} {channel.kind}");
                    Dictionary<int, Color32> mapping = FurniturePalette.Mapping(new[] { (channel.source, ramp.colors) });
                    foreach (var pair in mapping)
                        Assert.That((pair.Value.r << 16) | (pair.Value.g << 8) | pair.Value.b, Is.EqualTo(pair.Key), $"{body.id} {channel.kind} as drawn is exact");
                    Assert.That(looks.palettes.For(channel.kind).Count, Is.GreaterThanOrEqualTo(2), $"{channel.kind} offers a choice");
                }
            }
            foreach (PaletteRamp ramp in looks.palettes.ramps)
                Assert.That(ramp.colors.Length, Is.GreaterThanOrEqualTo(2), ramp.id);
        }

        [Test]
        public void EveryCreatorOption_HasItsName()
        {
            var english = CreatorLocKeys.English.Select(e => e.key).Concat(KeeperContent.English().Select(e => e.key)).ToHashSet();
            foreach (KeeperBody body in Looks.bodies) Assert.That(english, Does.Contain(body.nameKey), body.id);
            foreach (PaletteRamp ramp in Looks.palettes.ramps) Assert.That(english, Does.Contain(ramp.nameKey), ramp.id);
        }

        [Test]
        public void APalette_IsKeptOnlyForTheBodysOwnParts()
        {
            KeeperBody orc = Looks.Body("orc");
            string palette = FurniturePalette.Format(new Dictionary<string, string> { ["keeper_skin"] = "keeper_skin.tan", ["keeper_hair"] = "keeper_hair.red" });
            Assert.That(KeeperRules.ForBody(palette, orc), Is.EqualTo("keeper_hair=keeper_hair.red"), "an orc has no human skin to recolour");
            Assert.That(Looks.Body("nobody"), Is.SameAs(Looks.bodies[0]), "an unknown body (an edited save) is the first");
        }

        // ---------- The save ----------

        [Test]
        public void AVersion8Save_IsPastTheOpening_WithItsKeeperMade_AndNoQuestObjects()
        {
            SaveData v8 = SaveSystem.Capture(new GameState(3, DayPhase.Night));
            v8.version = 8;
            string json = Regex.Replace(SaveSystem.ToJson(v8), @",\s*""questObjects"":\s*\[\s*\]", string.Empty);
            json = Regex.Replace(json, @"""openingStage"":\s*""[^""]*"",\s*""creationComplete"":\s*\w+,\s*""seenHints"":\s*\[[^\]]*\],", string.Empty);
            json = json.Replace("\"openingComplete\": true", "\"openingComplete\": false");
            Assert.That(json, Does.Not.Contain("openingStage").And.Not.Contain("questObjects"), "a genuine version 8 file");
            SaveData migrated = SaveSystem.FromJson(json);
            Assert.That(migrated.version, Is.EqualTo(SaveSystem.CurrentVersion), "through version 9 and on");
            GameState state = SaveSystem.Restore(migrated, _ => null, _ => true);
            Assert.That((state.Story.Opening, state.Story.CreationComplete), Is.EqualTo((OpeningStage.Complete, true)),
                "a Checkpoint A playtest save never goes back through creation or the opening");
            Assert.That(state.Story.SeenHints, Is.EquivalentTo(OnboardingHints.All));
            Assert.That(state.QuestObjects.All, Is.Empty);
            Assert.That((state.Story.Player.name, state.Story.Player.body, state.Story.Player.palette), Is.EqualTo(("Bram", "townsfolk", "")));
        }

        [Test]
        public void TheOpeningStage_IsSavedByName_AndAnUnknownNameFallsBackOnTheOldFlag()
        {
            var state = new GameState(1, DayPhase.Daytime);
            state.Story.Opening = OpeningStage.Homecoming;
            SaveData saved = SaveSystem.Capture(state);
            Assert.That(saved.story.openingStage, Is.EqualTo("Homecoming"));
            saved.story.openingStage = "SomethingNew";
            saved.story.openingComplete = false;
            Assert.That(SaveSystem.Restore(saved, _ => null, _ => true).Story.Opening, Is.EqualTo(OpeningStage.Arrival));
        }

        // ---------- The opening ----------

        [Test]
        public void TheOpening_PlaysEachBeatAtItsStageAndPartOfTheDay_AndTheOnceBeatsOnlyOnce()
        {
            var seen = new HashSet<string>();
            Assert.That(OpeningRules.Beat(OpeningStage.Arrival, "Arrival", seen)?.Conversation, Is.EqualTo(OpeningRules.Arrival));
            Assert.That(OpeningRules.Beat(OpeningStage.Homecoming, "Night", seen)?.After, Is.EqualTo(OpeningStage.FirstEvening));
            Assert.That(OpeningRules.Beat(OpeningStage.FirstEvening, "Prep", seen)?.Conversation, Is.EqualTo(OpeningRules.FirstEvening));
            Assert.That(OpeningRules.Beat(OpeningStage.FirstEvening, "Results", seen)?.After, Is.EqualTo(OpeningStage.Complete));
            Assert.That(OpeningRules.Beat(OpeningStage.Complete, "Results", seen), Is.Null);
            Assert.That(OpeningRules.Beat(OpeningStage.FirstDelve, "Night", seen), Is.Null, "the night after a quick game isn't a homecoming");
            seen.Add("beat:arrival");
            Assert.That(OpeningRules.Beat(OpeningStage.Arrival, "Arrival", seen), Is.Null, "the arrival isn't replayed after a Continue");
        }

        [Test]
        public void TheArrival_GoesStraightDown_AndTheDelvesEnd_IsTheHomecoming()
        {
            var state = new GameState(1, DayPhase.Daytime);
            state.Story.Opening = OpeningStage.Arrival;
            DayRules.StartOpeningDelve(state);
            Assert.That((state.Day, state.Phase, state.Story.Opening, state.Today.KeptShut), Is.EqualTo((1, DayPhase.Delve, OpeningStage.FirstDelve, true)));
            DayRules.CompleteDelve(state, DelveReport.Extraction(new Hearthdelve.Shared.Inventory.Satchel(6, 3)));
            Assert.That((state.Phase, state.Story.Opening), Is.EqualTo((DayPhase.Night, OpeningStage.Homecoming)));
            Assert.Throws<System.InvalidOperationException>(() => DayRules.StartOpeningDelve(new GameState(1, DayPhase.Daytime)), "only on arrival day");
        }

        // ---------- Quest objects ----------

        static QuestObjectDefinition Bomb(bool lostOnDeath = true)
        {
            var d = ScriptableObject.CreateInstance<QuestObjectDefinition>();
            d.id = "boogs_bomb";
            d.questId = "boogs_bomb";
            d.floor = 1;
            d.afterRoomsCleared = 2;
            d.lostOnDeath = lostOnDeath;
            return d;
        }

        [Test]
        public void AQuestObject_IsWanted_ThenHome_ThenDelivered_AndNeverGoesBack()
        {
            var ledger = new QuestObjectLedger();
            Assert.That(ledger.Status("boogs_bomb"), Is.EqualTo(QuestObjectStatus.None));
            Assert.That(ledger.BringHome("boogs_bomb"), Is.False, "nothing comes home that wasn't wanted");
            Assert.That(ledger.Want("boogs_bomb"));
            Assert.That(ledger.Deliver("boogs_bomb"), Is.False, "it has to come home first");
            Assert.That(ledger.BringHome("boogs_bomb"));
            Assert.That(ledger.Want("boogs_bomb"), Is.False, "home stays home");
            Assert.That(ledger.Deliver("boogs_bomb"));
            Assert.That(ledger.Deliver("boogs_bomb"), Is.False, "handed over once: its reward can't be given twice");
            Assert.That(ledger.Status("boogs_bomb"), Is.EqualTo(QuestObjectStatus.Delivered));
        }

        [Test]
        public void TheBomb_LiesInTheSecondFightOfTheFirstFloor_OnlyWhileWanted_AndNotCarried()
        {
            QuestObjectDefinition bomb = Bomb();
            var ledger = new QuestObjectLedger();
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 1, 2, false), Is.False, "not before the quest");
            ledger.Want(bomb.id);
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 1, 2, false));
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 1, 1, false), Is.False);
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 2, 2, false), Is.False, "another floor");
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 1, 2, true), Is.False, "already carried");
            ledger.BringHome(bomb.id);
            Assert.That(QuestObjectRules.PlaceHere(bomb, ledger, 1, 2, false), Is.False, "home: never again");
        }

        [Test]
        public void ExtractingBringsItHome_DyingLosesIt_AndALaterDelveFindsItAgain()
        {
            QuestObjectDefinition bomb = Bomb();
            var state = new GameState(2, DayPhase.Delve);
            state.QuestObjects.Want(bomb.id);
            var loot = new RunLoot();
            loot.AddQuestObject(bomb.id);
            var satchel = new Hearthdelve.Shared.Inventory.Satchel(6, 3);
            Assert.That(satchel.Slots.All(s => s.IsEmpty), "it takes no satchel slot");

            // Death: lost; the quest still wants it, so it lies there again next time.
            DeathPenaltyResult died = DeathPenalty.Resolve(satchel, DeathPenalty.KeepNothing, 0);
            DayRules.CompleteDelve(state, DelveReport.Death(died, 0, null, null, loot.QuestObjects), questObjects: _ => bomb);
            Assert.That(state.QuestObjects.Status(bomb.id), Is.EqualTo(QuestObjectStatus.Wanted));
            Assert.That(QuestObjectRules.PlaceHere(bomb, state.QuestObjects, 1, 2, false), "a later delve finds it again");

            // Extraction: home for good.
            state.Cycle.Advance();
            state.Cycle.Advance();
            state.Cycle.Advance();
            DayRules.CompleteDelve(state, DelveReport.Extraction(satchel, 0, null, null, loot.QuestObjects), questObjects: _ => bomb);
            Assert.That(state.QuestObjects.Status(bomb.id), Is.EqualTo(QuestObjectStatus.Home));
            Assert.That(DayRules.QuestObjectsHome, Is.EqualTo(new[] { bomb.id }));
        }

        [Test]
        public void AnObjectKeptThroughDeath_ComesHomeAnyway()
        {
            QuestObjectDefinition keepsake = Bomb(lostOnDeath: false);
            var ledger = new QuestObjectLedger();
            ledger.Want(keepsake.id);
            Assert.That(QuestObjectRules.EndDelve(ledger, new[] { keepsake.id }, extracted: false, _ => keepsake), Is.EqualTo(new[] { keepsake.id }));
        }

        [Test]
        public void RunLoot_CarriesEachQuestObjectOnce()
        {
            var loot = new RunLoot();
            int added = 0;
            loot.QuestObjectAdded += _ => added++;
            loot.AddQuestObject("boogs_bomb");
            loot.AddQuestObject("boogs_bomb");
            Assert.That((loot.QuestObjects.Count, added, loot.Carries("boogs_bomb")), Is.EqualTo((1, 1, true)));
        }

        // ---------- Boog's bomb: content ----------

        [Test]
        public void TheBomb_IsAQuestObjectOfTheGame_TiedToItsQuest_WithItsArtAndName()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");
            QuestObjectDefinition bomb = database.QuestObject("boogs_bomb");
            Assert.That(bomb, Is.Not.Null);
            Assert.That(bomb.questId, Is.EqualTo(StoryBuilder.BoogsBombQuest));
            Assert.That(bomb.frames.Length, Is.EqualTo(10));
            Assert.That(bomb.frames.All(f => f != null));
            Assert.That((bomb.floor, bomb.afterRoomsCleared, bomb.lostOnDeath, bomb.offeredAgain), Is.EqualTo((1, 2, true, true)));
            Assert.That(QuestObjectContent.English.Select(e => e.key), Does.Contain("quest_object.boogs_bomb"));
            Assert.That(AssetDatabase.LoadAssetAtPath<Hearthdelve.Dungeon.Rooms.RunSettings>(DungeonRunBuilder.SettingsPath).questObjectPickup, Is.Not.Null);
        }

        [Test]
        public void ReturningTheBomb_IsADeedForBoogAlone_ThatEarnsHisRespect()
        {
            DeedDefinition deed = AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).deeds.Single(d => d.id == StoryBuilder.ReturnedBoogsBomb);
            Assert.That((deed.source, deed.target, deed.character, deed.learners), Is.EqualTo((DeedSource.None, DeedTarget.Character, CharacterIds.Boog, DeedLearners.Target)));
            Assert.That(RelationshipRules.TargetFaction(deed), Is.EqualTo(CharacterIds.Boog));
            CharacterDefinition boog = AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).characters.Single(c => c.id == CharacterIds.Boog);
            CharacterDefinition orik = AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).characters.Single(c => c.id == CharacterIds.Orik);
            Assert.That(RelationshipRules.Learners(deed, new[] { boog, orik }), Is.EqualTo(new[] { CharacterIds.Boog }));
            Assert.That(RelationshipRules.RespectChange(deed.respect, RelationshipRules.Alignment(boog.values, deed.shows), 1f), Is.GreaterThan(5f),
                "Boog values nerve: going down for her means something");
            Assert.That(AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).factions.GetAffinity(CharacterIds.Boog, CharacterIds.Boog), Is.EqualTo(100f),
                "he cares about himself, so a deed done for him pleases him");
        }

        // ---------- Dialogue: the seeded conversations ----------

        [Test]
        public void EverySeededConversation_IsOrdinaryDialogueSystemData_ReachableAndLinkedThroughout()
        {
            DialogueDatabase db = Dialogue;
            foreach (string title in StoryDialogue.SeedTitles)
            {
                Conversation c = db.GetConversation(title);
                Assert.That(c, Is.Not.Null, title);
                Assert.That(c.dialogueEntries[0].Title, Is.EqualTo("START"), title);
                // Every link lands on an entry that exists (in this conversation or another).
                foreach (DialogueEntry e in c.dialogueEntries)
                foreach (Link l in e.outgoingLinks)
                    Assert.That(db.GetConversation(l.destinationConversationID)?.GetDialogueEntry(l.destinationDialogueID), Is.Not.Null,
                        $"{title} {e.id} → {l.destinationConversationID}:{l.destinationDialogueID}");
                // Every entry is reachable from START.
                var reached = new HashSet<int> { 0 };
                var queue = new Queue<int>(reached);
                while (queue.Count > 0)
                    foreach (Link l in c.GetDialogueEntry(queue.Dequeue()).outgoingLinks)
                        if (l.destinationConversationID == c.id && reached.Add(l.destinationDialogueID)) queue.Enqueue(l.destinationDialogueID);
                Assert.That(c.dialogueEntries.Select(e => e.id).Where(id => !reached.Contains(id)), Is.Empty, $"{title}: unreachable entries");
            }
            Assert.That(StoryDialogue.SeededTitles(), Is.SupersetOf(StoryDialogue.SeedTitles), "each seed is logged, so it's never written again");
        }

        [Test]
        public void TheOpeningsConversations_AndTheCastsConversations_AllExist()
        {
            foreach (string title in OpeningRules.Conversations) Assert.That(Dialogue.GetConversation(title), Is.Not.Null, title);
            foreach (CharacterDefinition c in AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).characters.Where(c => c.HasConversation))
                Assert.That(Dialogue.GetConversation(c.conversation), Is.Not.Null, c.id);
        }

        [Test]
        public void Conversations_CallOnlyTheGamesOwnFunctions_AndNameOnlyRealQuestsAndDeeds()
        {
            var called = new List<string>();
            var quests = AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).quests.questAssets
                .Select(q => PixelCrushers.StringField.GetStringValue(q.id)).ToHashSet();
            var deeds = AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database).deeds.Select(d => d.id).ToHashSet();
            foreach (Conversation c in Dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
            foreach (string code in new[] { e.conditionsString, e.userScript, e.DialogueText })
            {
                if (string.IsNullOrEmpty(code)) continue;
                foreach (Match m in Regex.Matches(code, @"\b(HH_\w+)\(")) called.Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(code, @"HH_(?:QuestState|GiveQuest)\(""([^""]+)""")) Assert.That(quests, Does.Contain(m.Groups[1].Value), $"{c.Title} {e.id}");
                foreach (Match m in Regex.Matches(code, @"HH_Deed\(""([^""]+)""")) Assert.That(deeds, Does.Contain(m.Groups[1].Value), $"{c.Title} {e.id}");
                Assert.That(code, Does.Not.Contain("proof_trophy_wall"), $"{c.Title} {e.id}: the proof quest is retired");
            }
            Assert.That(called.Distinct().Except(StoryLua.Names), Is.Empty, "every function is registered");
            Assert.That(StoryLua.Names, Is.Unique);
        }

        // ---------- The builder boundary ----------

        const string ScratchFolder = "Assets/_Project/Tests/ScratchDialogue";

        [Test]
        public void HandEdits_SurviveTheStoryTooling_AndADeletedConversationStaysDeleted()
        {
            if (!AssetDatabase.IsValidFolder(ScratchFolder)) AssetDatabase.CreateFolder("Assets/_Project/Tests", "ScratchDialogue");
            string path = ScratchFolder + "/Scratch.asset", log = ScratchFolder + "/Seeds.txt";
            try
            {
                // A hand-made Boog/Talk that isn't the tooling's: kept as it is.
                var db = ScriptableObject.CreateInstance<DialogueDatabase>();
                AssetDatabase.CreateAsset(db, path);
                Template template = Template.FromDefault();
                Conversation mine = template.CreateConversation(template.GetNextConversationID(db), StoryDialogue.BoogTalk);
                mine.dialogueEntries.Add(template.CreateDialogueEntry(0, mine.id, "START"));
                db.conversations.Add(mine);

                db = StoryDialogue.Ensure(path, log);
                Assert.That(db.GetConversation(StoryDialogue.BoogTalk).dialogueEntries.Count, Is.EqualTo(1), "someone else's Boog/Talk is never replaced");
                Assert.That(db.GetConversation(StoryDialogueSeedsTitles.BoogBomb), Is.Not.Null, "the rest are seeded");

                // Edit a line and a link in the node editor's place, delete a conversation, and run the tooling again.
                Conversation bomb = db.GetConversation(StoryDialogueSeedsTitles.BoogBomb);
                DialogueEntry line = bomb.dialogueEntries.First(e => e.DialogueText.StartsWith("i lost something"));
                line.DialogueText = "i misplaced my favorite bomb. it happens.";
                line.outgoingLinks.RemoveAt(0);
                int links = line.outgoingLinks.Count;
                int entries = bomb.dialogueEntries.Count;
                db.conversations.Remove(db.GetConversation(OpeningRules.FirstEvening));
                EditorUtility.SetDirty(db);

                db = StoryDialogue.Ensure(path, log);
                bomb = db.GetConversation(StoryDialogueSeedsTitles.BoogBomb);
                Assert.That(bomb.dialogueEntries.Count, Is.EqualTo(entries));
                line = bomb.dialogueEntries.First(e => e.DialogueText.StartsWith("i misplaced"));
                Assert.That(line.outgoingLinks.Count, Is.EqualTo(links), "an edited link stays edited");
                Assert.That(db.GetConversation(OpeningRules.FirstEvening), Is.Null, "a deleted conversation stays deleted");
                Assert.That(db.conversations.Count(c => c.Title == StoryDialogueSeedsTitles.BoogBomb), Is.EqualTo(1), "nothing is duplicated");
            }
            finally
            {
                AssetDatabase.DeleteAsset(ScratchFolder);
            }
        }

        /// <summary>The seed titles the test names (the seeds themselves are internal to the tooling).</summary>
        static class StoryDialogueSeedsTitles
        {
            public const string BoogBomb = "Boog/Bomb";
        }
    }
}
