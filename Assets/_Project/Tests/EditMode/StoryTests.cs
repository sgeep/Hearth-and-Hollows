using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.Story.Editor;
using Hearthdelve.Story.Quests;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Typography;
using NUnit.Framework;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using PixelCrushers.LoveHate;
using PixelCrushers.QuestMachine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using CharacterInfo = UnityEngine.CharacterInfo;
using Faction = PixelCrushers.LoveHate.Faction;
using QuestState = PixelCrushers.QuestMachine.QuestState;
using SaveSystem = Hearthdelve.Shared.Save.SaveSystem;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4g Checkpoint A, pure and content: character identity, Hearth &amp; Hollows' relationship rules (who learns of a deed, Respect,
    /// the day clock), the version 8 save, and the story's content: one character id across the cast, Love/Hate, the Dialogue
    /// System and the staff; every line keyed in the Dialogue table; conditions that run.
    /// </summary>
    public class StoryTests
    {
        static StoryDatabase Story => AssetDatabase.LoadAssetAtPath<StoryDatabase>(StoryPaths.Database);

        static CharacterDefinition Character(string id, CharacterKind kind, bool tracked, SocialTraits values = default)
        {
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.id = id;
            c.kind = kind;
            c.tracked = tracked;
            c.values = values;
            return c;
        }

        // ---------- Character identity ----------

        [Test]
        public void CharacterIds_KeepBoogsStableId_AndTellAuthoredFromVisitors()
        {
            Assert.That(CharacterIds.Boog, Is.EqualTo("gunta"), "Boog's id never changes (saves, staff, events)");
            Assert.That(StaffIds.Boog, Is.EqualTo(CharacterIds.Boog));
            Assert.That(StaffIds.Orik, Is.EqualTo(CharacterIds.Orik));
            Assert.That(CharacterIds.IsAuthored("gunta") && CharacterIds.IsAuthored("old_phi"));
            Assert.That(CharacterIds.IsAuthored("Gunta") || CharacterIds.IsAuthored("visitor/1/2") || CharacterIds.IsAuthored(""), Is.False);
            Assert.That(CharacterIds.Visitor(3, 17), Is.EqualTo("visitor/3/17"));
            Assert.That(CharacterIds.IsVisitor("visitor/3/17") && !CharacterIds.IsVisitor("visitor/x"));
        }

        [Test]
        public void TheDirectory_ResolvesEveryKindOfPerson_TheSameWay()
        {
            var boog = Character("gunta", CharacterKind.Staff, true);
            var villager = Character("old_phi", CharacterKind.Villager, true);
            var resident = Character("settled_visitor_1", CharacterKind.Resident, true);
            var directory = new CharacterDirectory(new[] { boog, villager, Character("gunta", CharacterKind.Story, false), null });
            Assert.That(directory.All.Count(), Is.EqualTo(2), "a duplicate id or a missing character isn't added");
            Assert.That(directory.Add(resident), "a promoted Visitor joins at runtime");

            CharacterRef b = directory.Resolve("gunta");
            Assert.That((b.Kind, b.Definition, b.IsPersistent), Is.EqualTo((CharacterKind.Staff, boog, true)));
            CharacterRef v = directory.Resolve(CharacterIds.Visitor(2, 5));
            Assert.That((v.IsValid, v.Kind, v.Definition, v.IsPersistent), Is.EqualTo((true, CharacterKind.Visitor, (CharacterDefinition)null, false)),
                "a transient Visitor: an id, no definition, never saved");
            Assert.That(directory.Resolve("nobody").IsValid, Is.False);
            Assert.That(directory.Tracked(), Is.EquivalentTo(new[] { boog, villager, resident }));
        }

        // ---------- Relationship rules ----------

        [Test]
        public void Alignment_IsLoveHatesOwn_OverTheTraitsTheDeedShows()
        {
            var judge = new SocialTraits(60f, 90f, 10f);
            var deed = new SocialTraits(0f, 80f, 0f);
            // Love/Hate's Traits.Alignment over just the shown trait (nerve).
            Assert.That(RelationshipRules.Alignment(judge, deed), Is.EqualTo(Traits.Alignment(new[] { 90f }, new[] { 80f })).Within(1e-5f));
            Assert.That(RelationshipRules.Alignment(judge, deed), Is.EqualTo(0.95f).Within(1e-5f));
            Assert.That(RelationshipRules.Alignment(new SocialTraits(-100f, -100f, 0f), new SocialTraits(100f, 100f, 0f)), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(RelationshipRules.Alignment(judge, default), Is.EqualTo(1f), "a deed that shows nothing matches everyone");
            var all = new SocialTraits(20f, -40f, 60f);
            Assert.That(RelationshipRules.Alignment(judge, all), Is.EqualTo(Traits.Alignment(judge.ToArray(), all.ToArray())).Within(1e-5f),
                "a deed that shows everything: exactly Love/Hate's alignment");
        }

        [Test]
        public void Respect_IsTheDeedsRespect_ByHowWellItMatchesTheJudge_AndHowFreshItIs()
        {
            Assert.That(RelationshipRules.RespectWeight(0.5f), Is.Zero, "indifference earns none");
            Assert.That(RelationshipRules.RespectWeight(0.3f), Is.Zero, "a mismatch earns none (bad deeds lose respect through their own sign)");
            Assert.That(RelationshipRules.RespectWeight(1f), Is.EqualTo(1f));
            Assert.That(RelationshipRules.RespectChange(15f, 0.95f, 1f), Is.EqualTo(13.5f).Within(1e-4f), "Boog and the tusks");
            Assert.That(RelationshipRules.RespectChange(15f, 0.45f, 1f), Is.Zero, "Orik and the tusks");
            Assert.That(RelationshipRules.RespectChange(15f, 0.95f, 0.5f), Is.EqualTo(6.75f).Within(1e-4f), "a repeat, half as fresh");
            Assert.That(RelationshipRules.RespectChange(-20f, 1f, 1f), Is.EqualTo(-20f), "a deed against what they value");
        }

        [Test]
        public void Memories_AgeByTheGamesDays()
        {
            float expires = RelationshipRules.MemoryExpires(day: 3, memoryDays: 2);
            Assert.That(RelationshipRules.IsForgotten(expires, 3) || RelationshipRules.IsForgotten(expires, 5), Is.False, "remembered the day after next");
            Assert.That(RelationshipRules.IsForgotten(expires, 6), "forgotten after its days");
            Assert.That(RelationshipRules.IsForgotten(RelationshipRules.MemoryExpires(3, 0), 100000), Is.False, "0 days: for good");
        }

        [Test]
        public void ADeed_IsLearnedByTheStaff_OrByEveryone_NeverTheUntrackedOrThePlayer()
        {
            var cast = new[]
            {
                Character("player", CharacterKind.Player, true), Character("gunta", CharacterKind.Staff, true), Character("pip", CharacterKind.Staff, true),
                Character("old_phi", CharacterKind.Villager, true), Character("passer_by", CharacterKind.Villager, false),
            };
            var deed = ScriptableObject.CreateInstance<DeedDefinition>();
            deed.learners = DeedLearners.Staff;
            Assert.That(RelationshipRules.Learners(deed, cast), Is.EqualTo(new[] { "gunta", "pip" }));
            deed.learners = DeedLearners.Everyone;
            Assert.That(RelationshipRules.Learners(deed, cast), Is.EqualTo(new[] { "gunta", "pip", "old_phi" }));
            deed.source = DeedSource.TrophyDisplayed;
            Assert.That(RelationshipRules.DeedsFor(DeedSource.TrophyDisplayed, new[] { deed, null }), Is.EqualTo(new[] { deed }));
            Assert.That(RelationshipRules.DeedsFor(DeedSource.None, new[] { deed }), Is.Empty, "dialogue-only deeds come from no fact");
            Assert.That(RelationshipRules.TargetFaction(DeedTarget.Tavern), Is.EqualTo("tavern"));
        }

        // ---------- The save (version 8) ----------

        static GameState StoryGame()
        {
            var state = new GameState(4, DayPhase.Night);
            state.Story.Opening = OpeningStage.FirstEvening;
            state.Story.CreationComplete = true;
            state.Story.SeenHints.Add(OnboardingHints.Move);
            state.Story.SeenHints.Add("beat:arrival");
            state.Story.Player = new PlayerProfile { name = "Wren", body = "amazon", palette = "keeper_hair=keeper_hair.red;keeper_skin=keeper_skin.tan" };
            state.QuestObjects.Want("boogs_bomb");
            state.Story.Dialogue = "Variable={Alert=\"\"}";
            state.Story.Quests = "{\"staticQuestIds\":[\"proof_trophy_wall\"]}";
            state.Story.Relationships = new RelationshipData
            {
                values = { new RelationshipValueData { judge = "gunta", subject = "player", trait = "Respect", value = 13.5f } },
                memories = { new SocialMemoryData { judge = "gunta", deed = "displayed_trophy", actor = "player", target = "tavern", count = 2, impact = 25f, expires = RelationshipRules.Forever } },
            };
            return state;
        }

        [Test]
        public void TheStory_RoundTripsThroughTheSave()
        {
            SaveData saved = SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(StoryGame())));
            Assert.That(saved.version, Is.EqualTo(SaveSystem.CurrentVersion));
            GameState back = SaveSystem.Restore(saved, _ => null, _ => true);
            StoryState s = back.Story;
            Assert.That((s.Opening, s.CreationComplete, s.Player.name, s.Player.body, s.Player.palette),
                Is.EqualTo((OpeningStage.FirstEvening, true, "Wren", "amazon", "keeper_hair=keeper_hair.red;keeper_skin=keeper_skin.tan")));
            Assert.That(s.SeenHints, Is.EquivalentTo(new[] { OnboardingHints.Move, "beat:arrival" }));
            Assert.That(back.QuestObjects.Status("boogs_bomb"), Is.EqualTo(Hearthdelve.Shared.Quests.QuestObjectStatus.Wanted));
            Assert.That(s.Dialogue, Is.EqualTo("Variable={Alert=\"\"}"));
            Assert.That(s.Quests, Does.Contain("proof_trophy_wall"));
            RelationshipValueData v = s.Relationships.values.Single();
            Assert.That((v.judge, v.subject, v.trait, v.value), Is.EqualTo(("gunta", "player", "Respect", 13.5f)));
            SocialMemoryData m = s.Relationships.memories.Single();
            Assert.That((m.judge, m.deed, m.actor, m.target, m.count, m.expires), Is.EqualTo(("gunta", "displayed_trophy", "player", "tavern", 2, RelationshipRules.Forever)));
        }

        [Test]
        public void ASaveFromBefore4g_SkipsTheOpening_KeepsBram_AndInfersNothing()
        {
            SaveData v7 = SaveSystem.Capture(new GameState(9, DayPhase.Daytime));
            v7.version = 7;
            // The story and everything after it (version 9's quest objects) are newer than version 7.
            string json = Regex.Replace(SaveSystem.ToJson(v7), @",\s*""story"":.*$", "\n}", RegexOptions.Singleline);
            Assert.That(json, Does.Not.Contain("\"story\""), "a genuine version 7 file has no story");
            SaveData migrated = SaveSystem.FromJson(json);
            Assert.That(migrated.version, Is.EqualTo(SaveSystem.CurrentVersion));
            GameState state = SaveSystem.Restore(migrated, _ => null, _ => true);
            Assert.That(state.Story.OpeningComplete && state.Story.CreationComplete, "Continue never sends an old game through the opening or character creation");
            Assert.That(state.Story.SeenHints, Is.EquivalentTo(OnboardingHints.All), "nor shows it the first delve's prompts");
            Assert.That((state.Story.Player.name, state.Story.Player.body), Is.EqualTo(("Bram", "townsfolk")));
            Assert.That(state.Story.Relationships.IsEmpty && state.Story.Dialogue == string.Empty && state.Story.Quests == string.Empty,
                "no history inferred from the day, bosses or furniture");
        }

        [Test]
        public void AVersion1Save_ReachesTheCurrentVersion_ThroughEveryStep()
        {
            const string v1 = "{\"version\":1,\"day\":2,\"gold\":40,\"storeroom\":[]}";
            SaveData data = SaveSystem.FromJson(v1);
            Assert.That(data.version, Is.EqualTo(SaveSystem.CurrentVersion));
            Assert.That((data.story.openingComplete, data.story.openingStage, data.story.creationComplete), Is.EqualTo((true, "Complete", true)));
        }

        [Test]
        public void ABareState_HasNoOpeningAhead_SoToolsAndTestsNeverStartOne()
        {
            Assert.That(new GameState().Story.Opening, Is.EqualTo(OpeningStage.Complete), "only GameFlow.NewGame puts a game at the arrival");
            Assert.That(new GameState().Story.Player.name, Is.EqualTo(PlayerProfile.DefaultName));
        }

        // ---------- Content: one id everywhere ----------

        [Test]
        public void TheCast_HasOneStableIdAcrossStaffLoveHateAndTheDialogueSystem()
        {
            StoryDatabase story = Story;
            Assert.That(story, Is.Not.Null, "run Hearthdelve → Story → Update Story Content");
            var ids = story.characters.Select(c => c.id).ToList();
            Assert.That(ids, Is.Unique);
            Assert.That(ids, Is.SupersetOf(new[] { CharacterIds.Player, CharacterIds.Boog, CharacterIds.Orik }));
            Assert.That(ids.All(CharacterIds.IsAuthored));

            // Staff: each member of staff is the character with their id.
            foreach (string guid in AssetDatabase.FindAssets("t:StaffDefinition"))
            {
                var staff = AssetDatabase.LoadAssetAtPath<StaffDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (staff.id is StaffIds.Orik or StaffIds.Boog)
                    Assert.That(staff.character != null && staff.character.id == staff.id, $"{staff.name} is linked to its character");
            }

            // Love/Hate: a faction per tracked character, named by id, valuing what the character values; the player is faction 0.
            FactionDatabase factions = story.factions;
            Assert.That(factions.GetFaction(FactionDatabase.PlayerFactionID).name, Is.EqualTo(CharacterIds.Player));
            Assert.That(factions.relationshipTraitDefinitions.Select(t => t.name), Is.EqualTo(new[] { "Affinity", "Respect" }), "Affinity and Respect only (no Trust yet)");
            Assert.That(factions.personalityTraitDefinitions.Select(t => t.name), Is.EqualTo(SocialTraits.Names));
            foreach (CharacterDefinition c in story.characters.Where(c => c.tracked))
            {
                Faction f = factions.GetFaction(c.id);
                Assert.That(f, Is.Not.Null, $"{c.id} has a faction");
                Assert.That(f.traits, Is.EqualTo(c.values.ToArray()), $"{c.id}'s values");
                Assert.That(factions.GetRelationshipTrait(c.id, "player", 1), Is.EqualTo(c.respectForPlayer), $"{c.id} starts with their respect");
            }

            // The Dialogue System: every actor speaks for a character, every character with a conversation has an actor.
            DialogueDatabase dialogue = story.dialogue;
            var actorIds = dialogue.actors.Select(DialogueAdapter.CharacterId).ToList();
            Assert.That(actorIds, Has.None.Null, "every actor has a Character Id");
            Assert.That(actorIds, Is.Unique);
            Assert.That(ids, Is.SupersetOf(actorIds), "every actor is a character");
            foreach (CharacterDefinition c in story.characters.Where(c => c.HasConversation))
            {
                Assert.That(actorIds, Does.Contain(c.id), $"{c.id} speaks as an actor");
                Assert.That(dialogue.GetConversation(c.conversation), Is.Not.Null, $"{c.id}'s conversation '{c.conversation}' exists");
            }
            Assert.That(dialogue.actors.Single(a => DialogueAdapter.CharacterId(a) == CharacterIds.Player).IsPlayer);
        }

        [Test]
        public void ThePortraits_AreTheGeneratorsFrames()
        {
            foreach (CharacterDefinition c in Story.characters.Where(c => c.kind == CharacterKind.Staff))
            {
                PortraitDefinition p = c.portrait;
                Assert.That(p, Is.Not.Null, $"{c.id} has a portrait");
                Assert.That(p.still != null && p.blink != null && p.blink != p.still, $"{c.id}: a still face and a blink");
                Assert.That(p.talking.Length, Is.EqualTo(5), "the generator's talking cycle: the mouth at rest, then four");
                Assert.That(p.talking[0], Is.SameAs(p.still));
                Assert.That(p.still.rect.size, Is.EqualTo(new Vector2(32f, 32f)));
                Assert.That(p.Frame(false, 0f, true), Is.SameAs(p.blink));
                Assert.That(p.Frame(true, p.talkFrameSeconds * 1.5f, false), Is.SameAs(p.talking[1]));
            }
            Assert.That(Story.characters.Single(c => c.kind == CharacterKind.Player).portrait, Is.Null, "no portrait for the keeper in 4g");
        }

        [Test]
        public void TheDeeds_AreKnown_AndTheTrophyIsCheckpointAsProof()
        {
            List<DeedDefinition> deeds = Story.deeds;
            Assert.That(deeds.Select(d => d.id), Is.Unique);
            DeedDefinition trophy = deeds.Single(d => d.id == "displayed_trophy");
            Assert.That((trophy.source, trophy.target, trophy.learners, trophy.memoryDays), Is.EqualTo((DeedSource.TrophyDisplayed, DeedTarget.Tavern, DeedLearners.Staff, 0)));
            Assert.That(trophy.respect, Is.GreaterThan(0f));
            CharacterDefinition boog = Story.characters.Single(c => c.id == CharacterIds.Boog);
            CharacterDefinition pip = Story.characters.Single(c => c.id == CharacterIds.Orik);
            Assert.That(RelationshipRules.RespectChange(trophy.respect, RelationshipRules.Alignment(boog.values, trophy.shows), 1f), Is.GreaterThanOrEqualTo(10f),
                "Boog's tusks branch needs respect 10: one hanging earns it");
            Assert.That(RelationshipRules.RespectChange(trophy.respect, RelationshipRules.Alignment(pip.values, trophy.shows), 1f), Is.Zero, "Orik isn't impressed by monster parts");
        }

        [Test]
        public void BoogsBomb_IsTheQuestDatabasesQuest_KeyedForOurLocalization_AndMovedOnlyByFacts()
        {
            QuestDatabase quests = Story.quests;
            Assert.That(quests.questAssets.Select(q => StringField.GetStringValue(q.id)), Is.EqualTo(new[] { StoryBuilder.BoogsBombQuest }),
                "Checkpoint A's proof quest is retired");
            Quest quest = quests.questAssets.Single();
            Assert.That(StringField.GetStringValue(quest.title), Is.EqualTo(StoryLocKeys.BoogsBombTitle), "the title is a Localization key, not Quest Machine text");
            foreach (var (node, fact) in new[] { ("find", "QuestObjectBroughtHome"), ("return", "QuestObjectDelivered") })
            {
                var condition = (MessageQuestCondition)quest.nodeList.Single(n => StringField.GetStringValue(n.id) == node).conditionSet.conditionList.Single();
                Assert.That((StringField.GetStringValue(condition.message), StringField.GetStringValue(condition.parameter), condition.value.stringValue),
                    Is.EqualTo((QuestAdapter.FactMessage, fact, "boogs_bomb")), node);
            }
            Assert.That(quest.nodeList.Any(n => n.nodeType == QuestNodeType.Failure), Is.False, "death never fails it: there's no way to fail");
            Assert.That(QuestAdapter.Name(QuestState.Active), Is.EqualTo("active"));
        }

        // ---------- Content: the Dialogue table ----------

        [Test]
        public void EveryLine_HasAGuid_AndItsEnglishInTheDialogueTable()
        {
            DialogueDatabase dialogue = Story.dialogue;
            var english = StoryDialogue.English(dialogue).ToList();
            Assert.That(english, Is.Not.Empty);
            foreach (Conversation c in dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries.Where(e => !string.IsNullOrEmpty(e.DialogueText)))
                Assert.That(Field.LookupValue(e.fields, DialogueAdapter.GuidField), Is.Not.Empty, $"{c.Title} entry {e.id} has a Guid");
            Assert.That(english.Select(e => e.key), Is.Unique);
            var table = (StringTable)LocalizationEditorSettings.GetStringTableCollection(Loc.DialogueTable).GetTable("en");
            foreach (var (key, text) in english)
                Assert.That(table.GetEntry(key)?.Value, Is.EqualTo(text), $"Dialogue table '{key}' matches the database");
            Assert.That(table.Count, Is.EqualTo(english.Count), "no stale lines");
        }

        static float Width(string text)
        {
            Font font = GameFonts.Load();
            font.RequestCharactersInTexture(text, SilverMetrics.NativeSize);
            float width = 0f;
            foreach (char ch in text)
                if (font.GetCharacterInfo(ch, out CharacterInfo info, SilverMetrics.NativeSize)) width += info.advance;
            return Mathf.Max(0f, width - 1f);
        }

        /// <summary>Lines wrapped at word breaks in <paramref name="width"/> pixels.</summary>
        static int Lines(string text, float width)
        {
            int lines = 1;
            float line = 0f, space = Width(" ") + 1f;
            foreach (string word in text.Split(' '))
            {
                float w = Width(word);
                if (line > 0f && line + space + w > width)
                {
                    lines++;
                    line = w;
                }
                else line += (line > 0f ? space : 0f) + w;
            }
            return lines;
        }

        [Test]
        public void EveryLine_FitsTheBox_WithRoomToTranslate()
        {
            var problems = new List<string>();
            foreach (Conversation c in Story.dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries.Where(e => !string.IsNullOrEmpty(e.DialogueText)))
            {
                // A long name and a long place stand in for markup the player sees as words.
                string text = Regex.Replace(e.DialogueText, @"\[lua\(HH_PlayerName\(\)\)\]", "Bartholomew");
                text = StoryDialogue.Readable(text);
                bool player = Story.dialogue.GetActor(e.ActorID)?.IsPlayer == true;
                if (player && Width(text) > (StoryScene.ChoiceWidth - 8f) * 0.8f)
                    problems.Add($"{c.Title} {e.id}: \"{text}\" is {Width(text)} px; a choice holds {(StoryScene.ChoiceWidth - 8f) * 0.8f} with a fifth to spare");
                if (!player && Lines(text, StoryScene.TextWidth) > StoryScene.BodyLines - 1)
                    problems.Add($"{c.Title} {e.id}: \"{text}\" needs {Lines(text, StoryScene.TextWidth)} lines; English keeps the box's last line for translations");
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // ---------- Content: conditions that run ----------

        [Test]
        public void EveryConditionAndScript_RunsInTheDialogueSystemsLua()
        {
            StoryLua.Register();
            try
            {
                foreach (string name in StoryLua.All)
                    Assert.That(typeof(StoryLua).GetMethod(name), Is.Not.Null.And.Property("IsStatic").True, name);
                foreach (Conversation c in Story.dialogue.conversations)
                foreach (DialogueEntry e in c.dialogueEntries)
                {
                    if (!string.IsNullOrEmpty(e.conditionsString))
                    {
                        Lua.Result result = Lua.Run("return " + e.conditionsString, false, true);
                        Assert.That(result.isBool, $"{c.Title} {e.id}: '{e.conditionsString}' is a condition");
                    }
                    // Scripts only call our HH_ functions here: the ones that act need the story host, so they're checked by name.
                    if (!string.IsNullOrEmpty(e.userScript))
                        foreach (Match call in Regex.Matches(e.userScript, @"\b(\w+)\("))
                            Assert.That(StoryLua.All, Does.Contain(call.Groups[1].Value), $"{c.Title} {e.id} calls only HH_ functions");
                }
                Assert.That(Lua.Run("return HH_QuestState(\"proof_trophy_wall\")").asString, Is.EqualTo("unassigned"), "no story host: nothing assigned");
                Assert.That(Lua.Run("return HH_PlayerName()").asString, Is.EqualTo("Bram"));
            }
            finally
            {
                StoryLua.Unregister();
            }
        }

        [Test]
        public void Conversations_NeverCallLoveHateOrQuestMachineLuaDirectly()
        {
            var forbidden = new Regex(@"\b(GetAffinity|SetAffinity|ModifyAffinity|ReportDeed|KnowsDeed|ShareRumors|GetRelationshipTrait|SetQuestState|GetQuestState|CurrentQuestState)\b");
            foreach (Conversation c in Story.dialogue.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
                Assert.That(forbidden.IsMatch(e.conditionsString ?? "") || forbidden.IsMatch(e.userScript ?? ""), Is.False,
                    $"{c.Title} {e.id} goes through HH_ functions (plan §4)");
        }

        // ---------- The boundary ----------

        [Test]
        public void OnlyTheStoryAssemblies_ReferencePixelCrushers()
        {
            var pixelCrushers = new[] { "PixelCrushers", "DialogueSystem", "QuestMachine", "LoveHate" };
            foreach (string path in new[]
                     {
                         "Assets/_Project/Scripts/Core/Hearthdelve.Core.asmdef", "Assets/_Project/Scripts/Shared/Hearthdelve.Shared.asmdef",
                         "Assets/_Project/Scripts/Dungeon/Hearthdelve.Dungeon.asmdef", "Assets/_Project/Scripts/Tavern/Hearthdelve.Tavern.asmdef",
                         "Assets/_Project/Scripts/UI/Hearthdelve.UI.asmdef",
                     })
            {
                string json = System.IO.File.ReadAllText(path);
                foreach (string asm in pixelCrushers)
                    Assert.That(json.Contains($"\"{asm}\""), Is.False, $"{path} must not reference {asm}: gameplay reaches the story only through facts and Shared interfaces");
            }
        }
    }
}
