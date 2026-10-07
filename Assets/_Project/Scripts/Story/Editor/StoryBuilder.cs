using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using PixelCrushers.LoveHate;
using PixelCrushers.QuestMachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using Faction = PixelCrushers.LoveHate.Faction;
using Object = UnityEngine.Object;

namespace Hearthdelve.Story.Editor
{
    /// <summary>
    /// The story's content and its place in Boot (4g Checkpoint A), in one idempotent pass (<c>Hearthdelve → Story → Update Story
    /// Content</c>, or <see cref="UpdateBatch"/>):
    /// <list type="bullet">
    /// <item>portraits from <c>Tools/portraits</c>, and a <see cref="CharacterDefinition"/> for the player, Boog and Orik (created once:
    /// their values and starting feelings are then tuned on the asset), linked from the staff definitions;</item>
    /// <item>the deeds;</item>
    /// <item>the Love/Hate faction database, generated from the characters every run (never edit it by hand);</item>
    /// <item>the Quest Machine proof quest and quest database;</item>
    /// <item>the Dialogue System database, created with its actors and the Checkpoint A conversations only when it doesn't exist. After
    /// that the Dialogue System's editor is where dialogue is written (decision D1): this pass only gives new entries their Guid field
    /// and writes their English into the Dialogue string table;</item>
    /// <item>Boot, in place: the story host, the Dialogue Manager, Quest Machine, the faction manager and the dialogue box.</item>
    /// </list>
    /// </summary>
    public static class StoryBuilder
    {
        [MenuItem("Hearthdelve/Story/Update Story Content", priority = 40)]
        public static void Update()
        {
            InputActionsBuilder.Build(force: false);
            LocalizationBuilder.Build();
            foreach (string folder in new[] { StoryPaths.Root, StoryPaths.Characters, StoryPaths.Deeds, StoryPaths.Portraits, StoryPaths.Quests })
                EditorPaths.Ensure(folder);

            Dictionary<string, PortraitDefinition> portraits = Portraits();
            List<CharacterDefinition> cast = Characters(portraits);
            LinkStaff(cast);
            List<DeedDefinition> deeds = Deeds();
            FactionDatabase factions = Factions(cast);
            QuestDatabase quests = Quests();
            DialogueDatabase dialogue = StoryDialogue.Ensure(StoryPaths.Dialogue);
            StoryDialogue.FillTable(dialogue);

            var database = LoadOrCreate<StoryDatabase>(StoryPaths.Database);
            database.characters = cast;
            database.deeds = deeds;
            database.dialogue = dialogue;
            database.quests = quests;
            database.factions = factions;
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            StoryScene.UpdateBoot(database);
            AssetDatabase.SaveAssets();
            // 4g Checkpoint B: the keeper's looks, the bomb, and the main menu's creator (the menu panel is rebuilt in place).
            MinifantasyImporter.ImportAll();
            KeeperContent.Build();
            BootBuilder.Generate();
            Debug.Log("[Hearthdelve] Story content updated.");
        }

        public static void UpdateBatch()
        {
            try
            {
                Update();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        internal static T LoadOrCreate<T>(string path, Action<T> created = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            created?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------- Portraits ----------

        /// <summary>Imports each composed strip (still, blink, talking 1–4) and points a portrait asset at its frames.</summary>
        static Dictionary<string, PortraitDefinition> Portraits()
        {
            var sheets = MinifantasySheets.PortraitIds.Select(MinifantasySheets.PortraitSheet).ToList();
            MinifantasyImporter.Import(sheets);
            var result = new Dictionary<string, PortraitDefinition>();
            foreach (string id in MinifantasySheets.PortraitIds)
            {
                string file = $"{id}_portrait";
                Sprite[] frames = MinifantasyImporter.Row(MinifantasySheets.Portraits, file, 0, 6);
                if (frames.Any(f => f == null)) throw new InvalidOperationException($"The {id} portrait has fewer than 6 frames: run Tools/portraits/compose.py.");
                var portrait = LoadOrCreate<PortraitDefinition>($"{StoryPaths.Portraits}/Portrait_{id}.asset");
                portrait.still = frames[0];
                portrait.blink = frames[1];
                // The generator's talking cycle: the face's own mouth, then talking 1 to 4.
                portrait.talking = new[] { frames[0], frames[2], frames[3], frames[4], frames[5] };
                EditorUtility.SetDirty(portrait);
                result[id] = portrait;
            }
            return result;
        }

        // ---------- Characters ----------

        static List<CharacterDefinition> Characters(Dictionary<string, PortraitDefinition> portraits)
        {
            CharacterDefinition player = LoadOrCreate<CharacterDefinition>($"{StoryPaths.Characters}/Character_player.asset", c =>
            {
                c.kind = CharacterKind.Player;
            });
            player.id = CharacterIds.Player;
            player.kind = CharacterKind.Player;
            player.tracked = false;
            EditorUtility.SetDirty(player);

            // Boog: a sapper at heart. Daring first, good work close behind; kind in his own way.
            CharacterDefinition boog = LoadOrCreate<CharacterDefinition>($"{StoryPaths.Characters}/Character_gunta.asset", c =>
            {
                c.values = new SocialTraits(60f, 90f, 10f);
                c.affinityToPlayer = 10f;
                c.respectForPlayer = 0f;
                c.affinityToTavern = 60f;
                c.affinityToVillage = 20f;
            });
            Configure(boog, CharacterIds.Boog, CharacterKind.Staff, new LocalizedString(Loc.ContentTable, "staff.gunta"), portraits, StoryDialogue.BoogHub);

            // Orik: looks after people and the books; the Hollows worry him.
            CharacterDefinition pip = LoadOrCreate<CharacterDefinition>($"{StoryPaths.Characters}/Character_pip.asset", c =>
            {
                c.values = new SocialTraits(40f, -30f, 80f);
                c.affinityToPlayer = 20f;
                c.respectForPlayer = 5f;
                c.affinityToTavern = 90f;
                c.affinityToVillage = 50f;
            });
            Configure(pip, CharacterIds.Orik, CharacterKind.Staff, new LocalizedString(Loc.ContentTable, "staff.pip"), portraits, StoryDialogue.OrikHub);
            return new List<CharacterDefinition> { player, boog, pip };
        }

        static void Configure(CharacterDefinition c, string id, CharacterKind kind, LocalizedString name, Dictionary<string, PortraitDefinition> portraits, string conversation)
        {
            c.id = id;
            c.kind = kind;
            c.displayName = name;
            c.portrait = portraits.TryGetValue(id, out PortraitDefinition p) ? p : null;
            // Empty, or one of the builder's own earlier titles (Pip/Talk became Orik/Talk; Checkpoint C's hubs put the Talk
            // conversations behind the quest and the callbacks): the builder's current one. A title set by hand is kept.
            if (string.IsNullOrEmpty(c.conversation) || c.conversation is "Pip/Talk" or StoryDialogue.BoogTalk or StoryDialogue.OrikTalk)
                c.conversation = conversation;
            c.tracked = true;
            EditorUtility.SetDirty(c);
        }

        static void LinkStaff(List<CharacterDefinition> cast)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:StaffDefinition"))
            {
                var staff = AssetDatabase.LoadAssetAtPath<StaffDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                CharacterDefinition c = cast.FirstOrDefault(x => x.id == staff.id);
                if (c == null || staff.character == c) continue;
                staff.character = c;
                EditorUtility.SetDirty(staff);
            }
        }

        // ---------- Deeds ----------

        public const string DisplayedTrophy = "displayed_trophy";
        public const string ReturnedBoogsBomb = "returned_boogs_bomb";
        public const string FelledLarderTroll = "felled_larder_troll";
        public const string KeptAWish = "kept_a_wish";
        public const string FineButchery = "fine_butchery";

        static List<DeedDefinition> Deeds()
        {
            // Checkpoint A's deed: a boss's trophy hung at home. It shows nerve; the staff learn of it; it's remembered for good.
            DeedDefinition trophy = LoadOrCreate<DeedDefinition>($"{StoryPaths.Deeds}/Deed_{DisplayedTrophy}.asset", d =>
            {
                d.shows = new SocialTraits(0f, 80f, 0f);
                d.impact = 25f;
                d.respect = 15f;
                d.memoryDays = 0;
            });
            trophy.id = DisplayedTrophy;
            trophy.source = DeedSource.TrophyDisplayed;
            trophy.target = DeedTarget.Tavern;
            trophy.learners = DeedLearners.Staff;
            EditorUtility.SetDirty(trophy);

            // 4g Checkpoint B: Boog's bomb, brought back from the Hollows and handed over. Done for Boog, and only he learns of it
            // (in the conversation that hands it over: HH_Deed). It shows nerve (the keeper went down for it) and a little warmth;
            // remembered for good. Made once: tune it on the asset.
            DeedDefinition bomb = LoadOrCreate<DeedDefinition>($"{StoryPaths.Deeds}/Deed_{ReturnedBoogsBomb}.asset", d =>
            {
                d.shows = new SocialTraits(0f, 70f, 30f);
                d.impact = 40f;
                d.respect = 12f;
                d.memoryDays = 0;
            });
            bomb.id = ReturnedBoogsBomb;
            bomb.source = DeedSource.None;
            bomb.target = DeedTarget.Character;
            bomb.character = CharacterIds.Boog;
            bomb.learners = DeedLearners.Target;
            EditorUtility.SetDirty(bomb);

            // 4g Checkpoint C: the few remarkable things the household notices. Each made once (tune it on the asset); the
            // values decide who's impressed by what (Boog: nerve and craft; Orik: warmth and craft, not nerve).
            DeedDefinition troll = Deed(FelledLarderTroll, DeedSource.BossFirstCleared, DeedTarget.Tavern, DeedLearners.Staff,
                new SocialTraits(50f, 70f, 0f), impact: 30f, respect: 15f, memoryDays: 0);
            troll.subject = "larder_troll";
            EditorUtility.SetDirty(troll);
            DeedDefinition wish = Deed(KeptAWish, DeedSource.RequestKept, DeedTarget.Tavern, DeedLearners.Staff,
                new SocialTraits(40f, 0f, 60f), impact: 15f, respect: 8f, memoryDays: 7, minimum: 0.9f);
            DeedDefinition butchery = Deed(FineButchery, DeedSource.PartButchered, DeedTarget.Tavern, DeedLearners.Named,
                new SocialTraits(60f, 60f, 0f), impact: 10f, respect: 8f, memoryDays: 3, minimum: 0.9f);
            butchery.learnerIds = new[] { CharacterIds.Boog };
            EditorUtility.SetDirty(butchery);
            return new List<DeedDefinition> { trophy, bomb, troll, wish, butchery };
        }

        /// <summary>A deed asset made once with these values (later runs set only its id, source, target and learners).</summary>
        static DeedDefinition Deed(string id, DeedSource source, DeedTarget target, DeedLearners learners, SocialTraits shows, float impact, float respect,
            int memoryDays, float minimum = 0f)
        {
            DeedDefinition deed = LoadOrCreate<DeedDefinition>($"{StoryPaths.Deeds}/Deed_{id}.asset", d =>
            {
                d.shows = shows;
                d.impact = impact;
                d.respect = respect;
                d.memoryDays = memoryDays;
                d.minimum = minimum;
            });
            deed.id = id;
            deed.source = source;
            deed.target = target;
            deed.learners = learners;
            EditorUtility.SetDirty(deed);
            return deed;
        }

        // ---------- Love/Hate ----------

        /// <summary>
        /// The faction database, from the cast: the player (Love/Hate's player faction, id 0), the two places deeds are done for,
        /// and one faction per tracked character with their values and starting feelings. Rebuilt every run.
        /// </summary>
        static FactionDatabase Factions(List<CharacterDefinition> cast)
        {
            var db = LoadOrCreate<FactionDatabase>(StoryPaths.Factions);
            db.personalityTraitDefinitions = SocialTraits.Names.Select(n => new TraitDefinition(n, $"Hearth & Hollows value ({n}).")).ToArray();
            db.relationshipTraitDefinitions = new[]
            {
                new TraitDefinition(StoryFactions.Affinity, "(Required) How much they like them."),
                new TraitDefinition(StoryFactions.Respect, "How much they respect them (4g: moved by deeds that match their values)."),
            };
            db.presets = Array.Empty<Preset>();
            db.factions = Array.Empty<Faction>();
            db.nextID = 0;
            int player = db.CreateNewFaction(StoryFactions.Player, "The keeper.");
            if (player != FactionDatabase.PlayerFactionID) throw new InvalidOperationException("The player must be Love/Hate's faction 0.");
            db.CreateNewFaction(StoryFactions.Tavern, "Tally Ho!");
            db.CreateNewFaction(StoryFactions.Village, "Kariaston.");
            foreach (CharacterDefinition c in cast.Where(c => c.tracked && c.kind != CharacterKind.Player))
            {
                int id = db.CreateNewFaction(c.id, c.name);
                Faction f = db.GetFaction(id);
                f.traits = c.values.ToArray();
            }
            foreach (CharacterDefinition c in cast.Where(c => c.tracked && c.kind != CharacterKind.Player))
            {
                db.SetPersonalRelationshipTrait(c.id, StoryFactions.Player, 0, c.affinityToPlayer);
                db.SetPersonalRelationshipTrait(c.id, StoryFactions.Player, 1, c.respectForPlayer);
                db.SetPersonalRelationshipTrait(c.id, StoryFactions.Tavern, 0, c.affinityToTavern);
                db.SetPersonalRelationshipTrait(c.id, StoryFactions.Village, 0, c.affinityToVillage);
                // Everyone cares about themselves (4g Checkpoint B): a deed done for them pleases them as much as its impact.
                db.SetPersonalRelationshipTrait(c.id, c.id, 0, 100f);
            }
            EditorUtility.SetDirty(db);
            return db;
        }

        // ---------- Quest Machine ----------

        public const string ProofQuest = "proof_trophy_wall";
        public const string BoogsBombQuest = QuestObjectContent.BoogsBombQuest;

        /// <summary>
        /// Boog's Bomb (4g Checkpoint B), replacing Checkpoint A's proof quest (its asset is deleted; a save that has it drops it on
        /// load). Two objectives, each completed by a gameplay fact sent as a Quest Machine message: bring her home (the bomb comes
        /// up with an extraction: <c>QuestObjectBroughtHome</c>), then hand her to Boog (<c>QuestObjectDelivered</c>, from his
        /// conversation). Death never fails it. Created once: from then on Quest Machine's editor owns it. Its title is a
        /// Localization key; the reward is given by Hearth &amp; Hollows code, never a quest action.
        /// </summary>
        static QuestDatabase Quests()
        {
            string proof = $"{StoryPaths.Quests}/Quest_{ProofQuest}.asset";
            if (AssetDatabase.LoadAssetAtPath<Quest>(proof) != null) AssetDatabase.DeleteAsset(proof);
            string path = $"{StoryPaths.Quests}/Quest_{BoogsBombQuest}.asset";
            var quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
            if (quest == null)
            {
                var builder = new QuestBuilder("Boog's Bomb", BoogsBombQuest, StoryLocKeys.BoogsBombTitle);
                QuestNode start = builder.GetStartNode();
                QuestNode find = builder.AddConditionNode(start, "find", "Bring her home from the Cellars");
                find.conditionSet.conditionList.Add(Heard(nameof(Hearthdelve.Shared.Game.QuestObjectBroughtHome), QuestObjectContent.BoogsBomb));
                QuestNode hand = builder.AddConditionNode(find, "return", "Give her back to Boog");
                hand.conditionSet.conditionList.Add(Heard(nameof(Hearthdelve.Shared.Game.QuestObjectDelivered), QuestObjectContent.BoogsBomb));
                builder.AddSuccessNode(hand);
                quest = QuestEditorAssetUtility.SaveQuestAsAsset(builder.ToQuest(), path);
            }
            var db = LoadOrCreate<QuestDatabase>(StoryPaths.QuestDatabase);
            db.questAssets.Clear();
            db.questAssets.Add(quest);
            EditorUtility.SetDirty(db);
            return db;
        }

        /// <summary>A condition true when the fact arrives for this id (Hearth &amp; Hollows' "HH Fact" message).</summary>
        static MessageQuestCondition Heard(string fact, string id)
        {
            var heard = ScriptableObject.CreateInstance<MessageQuestCondition>();
            heard.message = new StringField(global::Hearthdelve.Story.Quests.QuestAdapter.FactMessage);
            heard.parameter = new StringField(fact);
            heard.value = new MessageValue { stringValue = id };
            return heard;
        }
    }
}
