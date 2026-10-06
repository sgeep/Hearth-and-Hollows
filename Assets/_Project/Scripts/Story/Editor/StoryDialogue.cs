using System;
using System.Collections.Generic;
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

        public static DialogueDatabase Ensure(string path)
        {
            var db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<DialogueDatabase>();
                db.description = "Hearth & Hollows dialogue (4g). Authored here, in the Dialogue System's editor; its English is copied into the Dialogue string table by Hearthdelve → Story → Update Story Content.";
                AssetDatabase.CreateAsset(db, path);
            }
            Template template = Template.FromDefault();
            Actor player = EnsureActor(db, template, DialogueAdapter.PlayerActor, CharacterIds.Player, true);
            Actor boog = EnsureActor(db, template, "Boog", CharacterIds.Boog, false);
            RenamePipToOrik(db);
            Actor pip = EnsureActor(db, template, "Orik", CharacterIds.Orik, false);
            if (db.GetConversation(BoogTalk) == null) WriteBoog(db, template, player, boog);
            if (db.GetConversation(OrikTalk) == null) WriteOrik(db, template, player, pip);
            EnsureGuids(db);
            EditorUtility.SetDirty(db);
            return db;
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

        // ---------- Checkpoint A's conversations (temporary writing; the Dialogue System editor owns them from here) ----------

        sealed class Writer
        {
            readonly DialogueDatabase m_Db;
            readonly Template m_Template;
            readonly Conversation m_Conversation;
            readonly int m_Npc, m_Player;
            int m_Next = 1;

            public Writer(DialogueDatabase db, Template template, string title, Actor player, Actor npc, string description)
            {
                m_Db = db;
                m_Template = template;
                m_Player = player.id;
                m_Npc = npc.id;
                m_Conversation = template.CreateConversation(template.GetNextConversationID(db), title);
                m_Conversation.ActorID = m_Player;
                m_Conversation.ConversantID = m_Npc;
                Field.SetValue(m_Conversation.fields, "Description", description);
                DialogueEntry start = template.CreateDialogueEntry(0, m_Conversation.id, "START");
                start.ActorID = m_Player;
                start.ConversantID = m_Npc;
                start.Sequence = "None()";
                start.canvasRect = new Rect(20f, 20f, DialogueEntry.CanvasRectWidth, DialogueEntry.CanvasRectHeight);
                m_Conversation.dialogueEntries.Add(start);
                db.conversations.Add(m_Conversation);
            }

            public DialogueEntry Start => m_Conversation.dialogueEntries[0];

            public DialogueEntry Npc(string text, int column, int row, string condition = null) => Entry(m_Npc, m_Player, text, column, row, condition, null);

            public DialogueEntry Player(string text, int column, int row, string script = null) => Entry(m_Player, m_Npc, text, column, row, null, script);

            DialogueEntry Entry(int actor, int conversant, string text, int column, int row, string condition, string script)
            {
                DialogueEntry e = m_Template.CreateDialogueEntry(m_Next++, m_Conversation.id, string.Empty);
                e.ActorID = actor;
                e.ConversantID = conversant;
                e.DialogueText = text;
                if (!string.IsNullOrEmpty(condition)) e.conditionsString = condition;
                if (!string.IsNullOrEmpty(script)) e.userScript = script;
                e.canvasRect = new Rect(20f + column * 200f, 20f + row * 60f, DialogueEntry.CanvasRectWidth, DialogueEntry.CanvasRectHeight);
                m_Conversation.dialogueEntries.Add(e);
                return e;
            }

            public void Link(DialogueEntry from, params DialogueEntry[] to)
            {
                foreach (DialogueEntry t in to)
                    from.outgoingLinks.Add(new Link(m_Conversation.id, from.id, m_Conversation.id, t.id));
            }
        }

        const string BoogRemembersTusks = "HH_Remembers(\"gunta\", \"displayed_trophy\")";

        /// <summary>
        /// Boog's Checkpoint A conversation: the proof that a deed done in Decorate Mode reaches him, is remembered and respected,
        /// and changes what he says. The first branch whose condition holds is taken.
        /// </summary>
        static void WriteBoog(DialogueDatabase db, Template template, Actor player, Actor boog)
        {
            var w = new Writer(db, template, BoogTalk, player, boog,
                "Checkpoint A (temporary writing): the tusks proof. Boog remembers the trophy (Love/Hate memory) and respects it (Respect), or offers the proof quest.");

            // He remembers the tusks going up, and respects it.
            DialogueEntry tusks = w.Npc("you hung the Larder Troll's tusks over the bar. i've been looking at them for an hour.", 0, 1,
                $"{BoogRemembersTusks} and HH_Respect(\"gunta\") >= 10");
            DialogueEntry save = w.Npc("if the stove catches fire again, they're the first thing i'm saving. after the bomb.", 0, 2);
            DialogueEntry looks = w.Player("they do look good up there.", 0, 3);
            DialogueEntry terrifying = w.Npc("they look terrifying. that's what good looks like.", 0, 4);
            DialogueEntry again = w.Player("again? the stove's been on fire?", 1, 3);
            DialogueEntry once = w.Npc("only the once. twice. it's fine, i was there both times.", 1, 4);
            w.Link(tusks, save);
            w.Link(save, looks, again);
            w.Link(looks, terrifying);
            w.Link(again, once);

            // He remembers, without the respect (a fallback: the values say this shouldn't happen).
            DialogueEntry noticed = w.Npc("the tusks are up. good. they keep an eye on the stew for me.", 2, 1, BoogRemembersTusks);

            // The proof quest is under way.
            DialogueEntry waiting = w.Npc("the wall over the bar is still bare. it's begging for something with teeth.", 3, 1,
                "HH_QuestState(\"proof_trophy_wall\") == \"active\"");

            // Otherwise: the offer.
            DialogueEntry quiet = w.Npc("this kitchen's too quiet. nothing on the walls is looking at us.", 4, 1);
            DialogueEntry bring = w.Npc("bring me something big from the Hollows. something with teeth. it goes over the bar.", 4, 2);
            DialogueEntry yes = w.Player("i'll see what i can find.", 4, 3, "HH_GiveQuest(\"proof_trophy_wall\", \"gunta\")");
            DialogueEntry cutlery = w.Npc("big teeth. small teeth are just cutlery.", 4, 4);
            DialogueEntry later = w.Player("maybe later.", 5, 3);
            DialogueEntry sneak = w.Npc("later is when things sneak up on you. but fine.", 5, 4);
            w.Link(quiet, bring);
            w.Link(bring, yes, later);
            w.Link(yes, cutlery);
            w.Link(later, sneak);

            w.Link(w.Start, tusks, noticed, waiting, quiet);
        }

        /// <summary>Orik's Checkpoint A conversation: he remembers the tusks too (affinity, no respect), and greets the keeper by name.</summary>
        static void WriteOrik(DialogueDatabase db, Template template, Actor player, Actor pip)
        {
            var w = new Writer(db, template, OrikTalk, player, pip,
                "Checkpoint A (temporary writing): Orik remembers the trophy; otherwise he greets the keeper by name.");
            DialogueEntry tusks = w.Npc("the tusks over the bar are a talking point. a guest asked if they bite. i said only on weekends.", 0, 1,
                "HH_Remembers(\"pip\", \"displayed_trophy\")");
            DialogueEntry bite = w.Player("do they?", 0, 2);
            DialogueEntry check = w.Npc("i haven't checked. i'm not going to check.", 0, 3);
            w.Link(tusks, bite);
            w.Link(bite, check);
            DialogueEntry hello = w.Npc("good evening, [lua(HH_PlayerName())]. the ledger and i are on speaking terms again.", 1, 1);
            w.Link(w.Start, tusks, hello);
        }
    }
}
