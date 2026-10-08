using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.UI.Localization;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Story.Editor
{
    /// <summary>
    /// The Dialogue System database's bootstrap and its localization (4g). The database is authored in the Dialogue System's editor
    /// (decision D1): <see cref="Ensure"/> creates it, with its actors and Checkpoint A's temporary conversations, only when it
    /// doesn't exist, and never rewrites a conversation; on every run it gives entries without one a <see cref="DialogueAdapter.GuidField"/>
    /// (the key of their line in the Dialogue table), and <see cref="FillTable"/> writes the database's English into the table.
    /// </summary>
    public static class StoryDialogue
    {
        public const string BoogTalk = "Boog/Talk";
        public const string OrikTalk = "Orik/Talk";
        /// <summary>4g Checkpoint C: who to talk to first, in priority order (the quest, then what they remember, then Talk).</summary>
        public const string BoogHub = "Boog/Hub";
        public const string OrikHub = "Orik/Hub";
        /// <summary>Musashi at the market cart (2026-10-07).</summary>
        public const string MusashiHub = "Musashi/Hub";
        /// <summary>4h Checkpoint C: Kariaston's people, each a hub (first meeting, callbacks, then what they're doing).</summary>
        public const string MaximoHub = "Maximo/Hub";
        public const string KalorenHub = "Kaloren/Hub";
        public const string GrimHub = "Grim/Hub";
        public const string OgrinHub = "Ogrin/Hub";
        public const string BartHub = "Bart/Hub";
        /// <summary>4h Checkpoint D: Gimp's night in the keeper's room (once) and his hub; the village's overheard exchanges.</summary>
        public const string GimpIntruder = "Gimp/Intruder";
        public const string GimpHub = "Gimp/Hub";
        public const string AmbientGrimOgrin = "Ambient/GrimOgrin";
        public const string AmbientKalorenGrim = "Ambient/KalorenGrim";
        public const string AmbientBartOgrin = "Ambient/BartOgrin";
        public const string AmbientMusashiBart = "Ambient/MusashiBart";
        public const string AmbientMaximoMusashi = "Ambient/MaximoMusashi";
        public const string AmbientMaximoOrik = "Ambient/MaximoOrik";
        public const string AmbientGimpBoog = "Ambient/GimpBoog";

        /// <param name="seedLog">The seed log (tests use their own); the project's by default.</param>
        public static DialogueDatabase Ensure(string path, string seedLog = null)
        {
            seedLog ??= SeedLogPath;
            var db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<DialogueDatabase>();
                db.description = "Hearth & Hollows dialogue (4g). Authored here, in the Dialogue System's editor; its English is copied into the Dialogue string table by Hearthdelve → Story → Update Story Content.";
                AssetDatabase.CreateAsset(db, path);
            }
            Template template = Template.FromDefault();
            var cast = new StoryDialogueSeeds.Cast
            {
                Player = EnsureActor(db, template, DialogueAdapter.PlayerActor, CharacterIds.Player, true),
                Boog = EnsureActor(db, template, "Boog", CharacterIds.Boog, false),
            };
            RenamePipToOrik(db);
            cast.Orik = EnsureActor(db, template, "Orik", CharacterIds.Orik, false);
            // 4h: the voice of things looked at.
            cast.Narration = EnsureActor(db, template, "Narration", CharacterIds.Narration, false);
            cast.Musashi = EnsureActor(db, template, "Musashi", CharacterIds.Musashi, false);
            cast.Maximo = EnsureActor(db, template, "Maximo", CharacterIds.Maximo, false);
            cast.Kaloren = EnsureActor(db, template, "Kaloren", CharacterIds.Kaloren, false);
            cast.Grim = EnsureActor(db, template, "Grim", CharacterIds.Grim, false);
            cast.Ogrin = EnsureActor(db, template, "Ogrin", CharacterIds.Ogrin, false);
            cast.Bart = EnsureActor(db, template, "Bart", CharacterIds.Bart, false);
            cast.Gimp = EnsureActor(db, template, "Gimp", CharacterIds.Gimp, false);
            Seed(db, template, cast, seedLog);
            EnsureGuids(db);
            EditorUtility.SetDirty(db);
            return db;
        }

        // ---------- Seeding, once (the builder boundary, 4g Checkpoint B) ----------

        /// <summary>
        /// Every conversation this tooling has ever written, one title per line. A title here is never written again, whatever has
        /// happened to it since: edited, renamed or deleted in the Dialogue System's editor, it stays that way. Delete a line (and
        /// the conversation) to have it seeded afresh.
        /// </summary>
        public static string SeedLogPath => StoryPaths.Root + "/DialogueSeeds.txt";

        /// <summary>The titles this tooling seeds (tests check each is in the database and well formed).</summary>
        public static IEnumerable<string> SeedTitles => StoryDialogueSeeds.All.Select(s => s.Title);

        public static HashSet<string> SeededTitles(string seedLog = null)
        {
            seedLog ??= SeedLogPath;
            var titles = new HashSet<string>(StringComparer.Ordinal);
            if (File.Exists(seedLog))
                foreach (string line in File.ReadAllLines(seedLog))
                    if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#")) titles.Add(line.Trim());
            return titles;
        }

        static void LogSeeded(IEnumerable<string> titles, string seedLog)
        {
            var all = SeededTitles(seedLog);
            all.UnionWith(titles);
            var lines = new List<string>
            {
                "# Dialogue System conversations Hearthdelve's story tooling has seeded (StoryDialogue). A title listed here is never",
                "# written again: the Dialogue System's editor owns it. Remove a line (and its conversation) to have it seeded afresh.",
            };
            lines.AddRange(all.OrderBy(t => t, StringComparer.Ordinal));
            File.WriteAllLines(seedLog, lines);
            AssetDatabase.ImportAsset(seedLog);
        }

        /// <summary>
        /// Writes each seed conversation not yet seeded. Checkpoint A's two proof conversations were seeded before the log existed:
        /// each is replaced by its Checkpoint B version only if it's exactly as Checkpoint A wrote it; one changed by hand is kept
        /// (with a warning) and logged as seeded.
        /// </summary>
        static void Seed(DialogueDatabase db, Template template, StoryDialogueSeeds.Cast cast, string seedLog)
        {
            HashSet<string> seeded = SeededTitles(seedLog);
            var written = new List<string>();
            foreach (StoryDialogueSeeds.Seed seed in StoryDialogueSeeds.All)
            {
                if (seeded.Contains(seed.Title)) continue;
                Conversation existing = db.GetConversation(seed.Title);
                if (existing != null)
                {
                    if (!StoryDialogueSeeds.IsUneditedCheckpointA(existing, template, cast))
                    {
                        Debug.LogWarning($"[Hearthdelve] Dialogue: '{seed.Title}' already exists and isn't the tooling's own; it's kept as it is.");
                        written.Add(seed.Title);
                        continue;
                    }
                    int id = existing.id;
                    // Its old lines leave the Dialogue table with it (FillTable removes keys no longer in the database).
                    db.conversations.Remove(existing);
                    seed.Write(db, template, cast, id);
                }
                else seed.Write(db, template, cast, -1);
                written.Add(seed.Title);
            }
            if (written.Count > 0) LogSeeded(written, seedLog);
        }

        /// <summary>
        /// The owner's rename (2026-10-06): Pip became Orik, a dwarf (same character id). A one-off migration of the database's names,
        /// as the Dialogue System's editor would make it by hand: the actor's name and the conversation's title; lines and Guids stay.
        /// </summary>
        static void RenamePipToOrik(DialogueDatabase db)
        {
            Actor actor = db.actors.FirstOrDefault(a => DialogueAdapter.CharacterId(a) == CharacterIds.Orik && a.Name == "Pip");
            if (actor != null) actor.Name = "Orik";
            Conversation talk = db.GetConversation("Pip/Talk");
            if (talk != null && db.GetConversation(OrikTalk) == null)
            {
                talk.Title = OrikTalk;
                Field.SetValue(talk.fields, "Description", "Checkpoint A (temporary writing): Orik remembers the trophy; otherwise he greets the keeper by name.");
            }
        }

        /// <summary>The actor speaking for <paramref name="characterId"/>, created if no actor has that character id.</summary>
        static Actor EnsureActor(DialogueDatabase db, Template template, string name, string characterId, bool isPlayer)
        {
            Actor actor = db.actors.FirstOrDefault(a => DialogueAdapter.CharacterId(a) == characterId);
            if (actor != null) return actor;
            actor = template.CreateActor(template.GetNextActorID(db), name, isPlayer);
            Field.SetValue(actor.fields, DialogueAdapter.CharacterIdField, characterId);
            db.actors.Add(actor);
            return actor;
        }

        /// <summary>Gives every spoken entry a stable Guid (the Localization bridge's key convention), once.</summary>
        static void EnsureGuids(DialogueDatabase db)
        {
            foreach (Conversation c in db.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
            {
                if (string.IsNullOrEmpty(e.DialogueText) && string.IsNullOrEmpty(e.MenuText)) continue;
                if (string.IsNullOrEmpty(Field.LookupValue(e.fields, DialogueAdapter.GuidField)))
                    Field.SetValue(e.fields, DialogueAdapter.GuidField, Guid.NewGuid().ToString());
            }
        }

        /// <summary>Every line's English, keyed as the Dialogue table keys it.</summary>
        public static IEnumerable<(string key, string english)> English(DialogueDatabase db)
        {
            if (db == null) yield break;
            foreach (Conversation c in db.conversations)
            foreach (DialogueEntry e in c.dialogueEntries)
            {
                string guid = Field.LookupValue(e.fields, DialogueAdapter.GuidField);
                if (string.IsNullOrEmpty(guid)) continue;
                if (!string.IsNullOrEmpty(e.DialogueText)) yield return (guid, e.DialogueText);
                if (!string.IsNullOrEmpty(e.MenuText)) yield return (guid + "_MenuText", e.MenuText);
            }
        }

        /// <summary>The Dialogue table's English, from the database (other locales are never touched; lines no longer in the database are removed).</summary>
        public static void FillTable(DialogueDatabase db) => LocalizationBuilder.FillTable(Loc.DialogueTable, English(db), new[] { string.Empty });

        /// <summary>A line's text as a player reads it: the Dialogue System's markup (<c>[lua(…)]</c>, <c>[var=…]</c>, <c>[em1]</c>) removed.</summary>
        public static string Readable(string text) => Regex.Replace(text ?? string.Empty, @"\[(lua\([^\]]*\)|var=[^\]]*|/?em\d|f|pic=[^\]]*|position=[^\]]*|auto|nosubtitle|a)\]", "");
    }
}
