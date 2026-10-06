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
    /// <item>portraits from <c>Tools/portraits</c>, and a <see cref="CharacterDefinition"/> for the player, Boog and Pip (created once:
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
            Configure(boog, CharacterIds.Boog, CharacterKind.Staff, new LocalizedString(Loc.ContentTable, "staff.gunta"), portraits, StoryDialogue.BoogTalk);

            // Pip: looks after people and the books; the Hollows worry her.
            CharacterDefinition pip = LoadOrCreate<CharacterDefinition>($"{StoryPaths.Characters}/Character_pip.asset", c =>
            {
                c.values = new SocialTraits(40f, -30f, 80f);
                c.affinityToPlayer = 20f;
                c.respectForPlayer = 5f;
                c.affinityToTavern = 90f;
                c.affinityToVillage = 50f;
            });
            Configure(pip, CharacterIds.Pip, CharacterKind.Staff, new LocalizedString(Loc.ContentTable, "staff.pip"), portraits, StoryDialogue.PipTalk);
            return new List<CharacterDefinition> { player, boog, pip };
        }

        static void Configure(CharacterDefinition c, string id, CharacterKind kind, LocalizedString name, Dictionary<string, PortraitDefinition> portraits, string conversation)
        {
            c.id = id;
            c.kind = kind;
            c.displayName = name;
            c.portrait = portraits.TryGetValue(id, out PortraitDefinition p) ? p : null;
            if (string.IsNullOrEmpty(c.conversation)) c.conversation = conversation;
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
            return new List<DeedDefinition> { trophy };
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
            }
            EditorUtility.SetDirty(db);
            return db;
        }

        // ---------- Quest Machine ----------

        public const string ProofQuest = "proof_trophy_wall";

        /// <summary>
        /// Checkpoint A's proof quest (temporary; Boog's Bomb replaces it in Step 6): Boog wants something with teeth over the bar.
        /// One objective, completed by the <c>TrophyDisplayed</c> fact. Its title is a Localization key: Quest Machine's own text
        /// tables are not used.
        /// </summary>
        static QuestDatabase Quests()
        {
            string path = $"{StoryPaths.Quests}/Quest_{ProofQuest}.asset";
            var quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
            if (quest == null)
            {
                var builder = new QuestBuilder("Something with teeth (Checkpoint A proof)", ProofQuest, StoryLocKeys.ProofQuestTitle);
                QuestNode start = builder.GetStartNode();
                QuestNode hang = builder.AddConditionNode(start, "hang", "Hang a trophy over the bar");
                var heard = ScriptableObject.CreateInstance<MessageQuestCondition>();
                heard.message = new StringField(global::Hearthdelve.Story.Quests.QuestAdapter.FactMessage);
                heard.parameter = new StringField("TrophyDisplayed");
                hang.conditionSet.conditionList.Add(heard);
                builder.AddSuccessNode(hang);
                quest = QuestEditorAssetUtility.SaveQuestAsAsset(builder.ToQuest(), path);
            }
            var db = LoadOrCreate<QuestDatabase>(StoryPaths.QuestDatabase);
            db.questAssets.Clear();
            db.questAssets.Add(quest);
            EditorUtility.SetDirty(db);
            return db;
        }
    }
}
