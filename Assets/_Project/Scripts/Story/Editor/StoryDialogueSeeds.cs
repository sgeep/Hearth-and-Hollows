using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hearthdelve.Shared.Story;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace Hearthdelve.Story.Editor
{
    /// <summary>
    /// First drafts of the story's conversations, written once into the Dialogue System database as ordinary conversations, entries
    /// and links (4g Checkpoint B). <see cref="StoryDialogue"/> seeds each title once and logs it; from then on the Dialogue System's
    /// node editor is the only place it changes. Nothing here runs at play time, and nothing reads these drafts back: the database is
    /// the source.
    /// </summary>
    static class StoryDialogueSeeds
    {
        public sealed class Cast
        {
            public Actor Player, Boog, Orik, Narration, Musashi, Maximo, Kaloren, Grim, Ogrin, Bart;
        }

        /// <summary>A conversation to seed: its title, and how to write it (into conversation id <c>id</c>, or a new id when −1).</summary>
        public sealed class Seed
        {
            public string Title;
            public Action<DialogueDatabase, Template, Cast, int> Write;
        }

        public const string BoogBomb = "Boog/Bomb";

        /// <summary>In writing order: Boog/Bomb before the conversations that link into it.</summary>
        public static readonly Seed[] All =
        {
            new() { Title = BoogBomb, Write = WriteBoogBomb },
            new() { Title = StoryDialogue.BoogTalk, Write = WriteBoogTalk },
            new() { Title = StoryDialogue.OrikTalk, Write = WriteOrikTalk },
            new() { Title = OpeningRules.Arrival, Write = WriteArrival },
            new() { Title = OpeningRules.Homecoming, Write = WriteHomecoming },
            new() { Title = OpeningRules.FirstEvening, Write = WriteFirstEvening },
            new() { Title = OpeningRules.FirstTakings, Write = WriteFirstTakings },
            // 4g Checkpoint C: who to talk to first, in front of the Talk conversations they fall back to.
            new() { Title = StoryDialogue.BoogHub, Write = WriteBoogHub },
            new() { Title = StoryDialogue.OrikHub, Write = WriteOrikHub },
            // 4h Checkpoint A: things to look at in Tally Ho! and Kariaston, and five o'clock.
            new() { Title = SurfaceConversations.PhiPortrait, Write = (db, t, c, id) => WriteLook(db, t, c, id, SurfaceConversations.PhiPortrait,
                "Phi'rai. Old Phi, to anyone who wanted to keep their teeth.") },
            new() { Title = SurfaceConversations.Tankards, Write = (db, t, c, id) => WriteLook(db, t, c, id, SurfaceConversations.Tankards,
                "five tankards, polished, on a shelf nobody drinks from.") },
            new() { Title = SurfaceConversations.Hatch, Write = (db, t, c, id) => WriteLook(db, t, c, id, SurfaceConversations.Hatch,
                "the cellar hatch. the Hollows can wait for dark.") },
            new() { Title = SurfaceConversations.Memorial, Write = (db, t, c, id) => WriteLook(db, t, c, id, SurfaceConversations.Memorial,
                "Karias. the letters are worn smooth where people touch them.") },
            new() { Title = SurfaceConversations.OrikFive, Write = WriteOrikFive },
            // 2026-10-07: Musashi, who keeps the market cart.
            new() { Title = StoryDialogue.MusashiHub, Write = WriteMusashiHub },
            // 4h Checkpoint C: Kariaston's people.
            new() { Title = StoryDialogue.MaximoHub, Write = WriteMaximoHub },
            new() { Title = StoryDialogue.KalorenHub, Write = WriteKalorenHub },
            new() { Title = StoryDialogue.GrimHub, Write = WriteGrimHub },
            new() { Title = StoryDialogue.OgrinHub, Write = WriteOgrinHub },
            new() { Title = StoryDialogue.BartHub, Write = WriteBartHub },
        };

        // ---------- The writer ----------

        sealed class Writer
        {
            readonly Template m_Template;
            readonly Conversation m_Conversation;
            readonly Actor m_Player, m_Npc;
            int m_Next = 1;

            public Writer(DialogueDatabase db, Template template, int id, string title, Actor player, Actor npc, string description)
            {
                m_Template = template;
                m_Player = player;
                m_Npc = npc;
                m_Conversation = template.CreateConversation(id >= 0 ? id : template.GetNextConversationID(db), title);
                m_Conversation.ActorID = player.id;
                m_Conversation.ConversantID = npc.id;
                Field.SetValue(m_Conversation.fields, "Description", description);
                DialogueEntry start = template.CreateDialogueEntry(0, m_Conversation.id, "START");
                start.ActorID = player.id;
                start.ConversantID = npc.id;
                start.Sequence = "None()";
                start.canvasRect = new Rect(20f, 20f, DialogueEntry.CanvasRectWidth, DialogueEntry.CanvasRectHeight);
                m_Conversation.dialogueEntries.Add(start);
                db.conversations.Add(m_Conversation);
            }

            public Conversation Conversation => m_Conversation;
            public DialogueEntry Start => m_Conversation.dialogueEntries[0];

            public DialogueEntry Npc(string text, int column, int row, string condition = null, string script = null) =>
                Entry(m_Npc, m_Player, text, column, row, condition, script);

            /// <summary>Another speaker in the same conversation (Boog in Orik's, for example).</summary>
            public DialogueEntry Say(Actor speaker, string text, int column, int row, string condition = null, string script = null) =>
                Entry(speaker, m_Player, text, column, row, condition, script);

            public DialogueEntry Player(string text, int column, int row, string script = null, string condition = null) =>
                Entry(m_Player, m_Npc, text, column, row, condition, script);

            /// <summary>A group: no line of its own, a branch point (with a condition) that passes straight on.</summary>
            public DialogueEntry Group(string title, int column, int row, string condition)
            {
                DialogueEntry e = Entry(m_Npc, m_Player, string.Empty, column, row, condition, null);
                e.isGroup = true;
                e.Title = title;
                return e;
            }

            DialogueEntry Entry(Actor actor, Actor conversant, string text, int column, int row, string condition, string script)
            {
                DialogueEntry e = m_Template.CreateDialogueEntry(m_Next++, m_Conversation.id, string.Empty);
                e.ActorID = actor.id;
                e.ConversantID = conversant.id;
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

            /// <summary>A link into another conversation's START (the Dialogue System's cross-conversation link).</summary>
            public void LinkTo(DialogueEntry from, Conversation other) =>
                from.outgoingLinks.Add(new Link(m_Conversation.id, from.id, other.id, 0));
        }

        static Conversation Find(DialogueDatabase db, string title) =>
            db.GetConversation(title) ?? throw new InvalidOperationException($"'{title}' must be seeded first.");

        // ---------- Conditions ----------

        const string Bomb = "\"boogs_bomb\"";
        const string BoogRemembersTusks = "HH_Remembers(\"gunta\", \"displayed_trophy\")";
        static readonly string BombDelivered = $"HH_QuestObject({Bomb}) == \"delivered\"";
        static readonly string BombHome = $"HH_HasQuestObject({Bomb})";
        static readonly string BombWanted = $"HH_QuestState({Bomb}) == \"active\"";
        const string OpeningDone = "HH_OpeningStage() == \"Complete\"";
        const string Arriving = "HH_OpeningStage() == \"Arrival\"";

        // ---------- Boog's Bomb (Step 6) ----------

        /// <summary>
        /// Boog's Bomb: the offer (accept, ask, or not now: declining never closes it), the reminder, the return (the bomb handed over,
        /// its reward and the deed, once) and afterwards. The first branch whose condition holds is taken.
        /// </summary>
        static void WriteBoogBomb(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, BoogBomb, c.Player, c.Boog,
                "Boog's Bomb (4g Checkpoint B): the offer, the reminder, her return and afterwards. Played from Boog/Talk and at the end of Act1/FirstTakings.");

            // Afterwards: she's home and on the shelf.
            DialogueEntry shelf = w.Npc("she's on the shelf over the stove now. i dust her. don't tell Orik i dust her.", 0, 1, BombDelivered);

            // Her return.
            DialogueEntry found = w.Npc("is that... you found her! give her here. careful. no, carefuller.", 1, 1, BombHome);
            DialogueEntry scratch = w.Npc("not a scratch on her. well. the usual number of scratches.", 1, 2);
            DialogueEntry askResearch = w.Player("so what's the research, Boog?", 1, 3);
            DialogueEntry yours = w.Player("she's all yours.", 2, 3);
            DialogueEntry first = w.Npc("...she's the first thing i ever made that went off when i meant her to. before her, things went off when they wanted.", 1, 4);
            DialogueEntry phi = w.Npc("Old Phi let me keep her. she said everyone needs one thing that does what they hoped it would.", 1, 5);
            DialogueEntry reward = w.Npc("here. for your trouble. i was saving it for fuses.", 2, 6,
                script: $"if HH_HasQuestObject({Bomb}) then HH_Deed(\"returned_boogs_bomb\"); HH_DeliverQuestObject({Bomb}) end");
            DialogueEntry remember = w.Npc("you went all the way down for her. i won't forget it, keeper.", 2, 7);
            w.Link(found, scratch);
            w.Link(scratch, askResearch, yours);
            w.Link(askResearch, first);
            w.Link(first, phi);
            w.Link(phi, reward);
            w.Link(yours, reward);
            w.Link(reward, remember);

            // Still looking.
            DialogueEntry anySign = w.Npc("any sign of her? round, black, smoking a little. she won't come if you call.", 3, 1, BombWanted);
            DialogueEntry looking = w.Player("i'll keep looking.", 3, 2);
            DialogueEntry where = w.Npc("first floor of the Cellars, a couple of fights in. that's where the spiders and i disagreed.", 3, 3);
            w.Link(anySign, looking);
            w.Link(looking, where);

            // The offer.
            DialogueEntry lost = w.Npc("i lost something in the Hollows. my favorite bomb.", 4, 1);
            DialogueEntry favorite = w.Player("your favorite bomb?", 4, 2);
            DialogueEntry wherePlayer = w.Player("where did you lose her?", 5, 2);
            DialogueEntry huh = w.Npc("you don't have a favorite bomb? huh. well, she's mine.", 4, 3);
            DialogueEntry cellars = w.Npc("in the Cellars, first floor, a couple of fights in. the spiders and i had a disagreement.", 5, 3);
            DialogueEntry ask = w.Npc("will you bring her back?", 4, 4);
            DialogueEntry yes = w.Player("i'll bring her back.", 4, 5, $"HH_GiveQuest({Bomb}, \"gunta\")");
            DialogueEntry why = w.Player("what do you need her for?", 5, 5);
            DialogueEntry notNow = w.Player("not now.", 6, 5);
            DialogueEntry normal = w.Npc("you will? she's round, black, and her fuse is still going. that's normal. don't worry about it.", 4, 6);
            DialogueEntry research = w.Npc("research.", 5, 6);
            DialogueEntry whatResearch = w.Player("what research?", 5, 7);
            DialogueEntry privateKind = w.Npc("the private kind. will you look or not?", 5, 8);
            DialogueEntry patient = w.Npc("she'll keep. she's very patient, for a bomb.", 6, 6);
            w.Link(lost, favorite, wherePlayer);
            w.Link(favorite, huh);
            w.Link(wherePlayer, cellars);
            w.Link(huh, ask);
            w.Link(cellars, ask);
            w.Link(ask, yes, why, notNow);
            w.Link(yes, normal);
            w.Link(why, research);
            w.Link(research, whatResearch);
            w.Link(whatResearch, privateKind);
            w.Link(privateKind, yes, notNow);
            w.Link(notNow, patient);

            w.Link(w.Start, shelf, found, anySign, lost);
        }

        // ---------- Boog/Talk and Orik/Talk (Checkpoint B's versions of Checkpoint A's proofs) ----------

        /// <summary>
        /// Boog, any time: the tusks (Checkpoint A's deed, remembered and respected), arrival day, his bomb (into Boog/Bomb), and
        /// otherwise the stove, with the bomb to ask about.
        /// </summary>
        static void WriteBoogTalk(DialogueDatabase db, Template template, Cast c, int id)
        {
            Conversation bomb = Find(db, BoogBomb);
            var w = new Writer(db, template, id, StoryDialogue.BoogTalk, c.Player, c.Boog,
                "Boog, any time (4g Checkpoint B). The first branch whose condition holds is taken; his bomb is Boog/Bomb.");

            DialogueEntry handOver = w.Group("her return", 0, 1, BombHome);
            w.LinkTo(handOver, bomb);

            DialogueEntry tusks = w.Npc("you hung the Larder Troll's tusks over the bar. i've been looking at them for an hour.", 1, 1,
                $"{BoogRemembersTusks} and HH_Respect(\"gunta\") >= 10");
            DialogueEntry save = w.Npc("if the stove catches fire again, they're the first thing i'm saving. after the bomb.", 1, 2);
            DialogueEntry looks = w.Player("they do look good up there.", 1, 3);
            DialogueEntry terrifying = w.Npc("they look terrifying. that's what good looks like.", 1, 4);
            DialogueEntry again = w.Player("again? the stove's been on fire?", 2, 3);
            DialogueEntry once = w.Npc("only the once. twice. it's fine, i was there both times.", 2, 4);
            w.Link(tusks, save);
            w.Link(save, looks, again);
            w.Link(looks, terrifying);
            w.Link(again, once);

            DialogueEntry hatch = w.Npc("the hatch is in the floor. you can't miss it. people do miss it, and then they fall in. that works too.", 3, 1, Arriving);

            DialogueEntry stove = w.Npc("the stove's hot. the stove's always hot. that's how you know it's working.", 4, 1);
            DialogueEntry aboutBomb = w.Player("about your bomb...", 4, 2, condition: $"{OpeningDone} and not ({BombDelivered})");
            DialogueEntry carryOn = w.Player("carry on.", 5, 2);
            DialogueEntry toBomb = w.Group("to Boog/Bomb", 4, 3, null);
            w.Link(stove, aboutBomb, carryOn);
            w.Link(aboutBomb, toBomb);
            w.LinkTo(toBomb, bomb);

            w.Link(w.Start, handOver, tusks, hatch, stove);
        }

        /// <summary>
        /// Orik, any time: the tusks (Checkpoint A), arrival day, Boog's bomb home, and otherwise the ledger, with three things to
        /// ask him (Phi, why he won't go down, the books).
        /// </summary>
        static void WriteOrikTalk(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.OrikTalk, c.Player, c.Orik,
                "Orik, any time (4g Checkpoint B). The first branch whose condition holds is taken.");
            DialogueEntry tusks = w.Npc("the tusks over the bar are a talking point. a guest asked if they bite. i said only on weekends.", 0, 1,
                "HH_Remembers(\"pip\", \"displayed_trophy\")");
            DialogueEntry bite = w.Player("do they?", 0, 2);
            DialogueEntry check = w.Npc("i haven't checked. i'm not going to check.", 0, 3);
            w.Link(tusks, bite);
            w.Link(bite, check);

            DialogueEntry counting = w.Npc("the hatch is in the floor. i'll be up here, counting things. it's what i'm for.", 1, 1, Arriving);

            DialogueEntry incident = w.Npc("Boog's bomb is home. i've entered it in the incident book. in advance.", 2, 1, BombDelivered);

            DialogueEntry hello = w.Npc("good evening, [lua(HH_PlayerName())]. the ledger and i are on speaking terms again.", 3, 1);
            DialogueEntry askPhi = w.Player("tell me about Phi.", 3, 2);
            DialogueEntry askDown = w.Player("why won't you go down?", 4, 2);
            DialogueEntry askBooks = w.Player("how are the books?", 5, 2);
            DialogueEntry never = w.Player("never mind.", 6, 2);
            DialogueEntry rebuilt = w.Npc("we rebuilt this place together, long ago. she'd been one of the Fortunate Five. the road kept calling her.", 3, 3);
            DialogueEntry left = w.Npc("she left, and i kept it open without her for years. when the Hollows turned bad, i left too.", 3, 4);
            DialogueEntry late = w.Npc("decades later she came home, found me, and said i was late for work. so i came back.", 3, 5);
            DialogueEntry family = w.Npc("my family went down for three hundred years. somebody had to come up and count what they left.", 4, 3);
            DialogueEntry red = w.Npc("in the red. a cheerful sort of red. we've had worse reds.", 5, 3);
            w.Link(hello, askPhi, askDown, askBooks, never);
            w.Link(askPhi, rebuilt);
            w.Link(rebuilt, left);
            w.Link(left, late);
            w.Link(askDown, family);
            w.Link(askBooks, red);

            w.Link(w.Start, tusks, counting, incident, hello);
        }

        // ---------- The Act I opening (Step 5) ----------

        /// <summary>Arrival day: Orik and Boog meet the new keeper; Phi is missing below; the storeroom is empty; the hatch is there.</summary>
        static void WriteArrival(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, OpeningRules.Arrival, c.Player, c.Orik,
                "Act I opening: the keeper arrives at Tally Ho! (played once, as arrival day begins). Ends with the keeper free to walk to the hatch.");
            DialogueEntry late = w.Npc("ahh, you must be [lua(HH_PlayerName())]! Phi's letter said you'd come. it didn't say you'd be this late.", 0, 1);
            DialogueEntry road = w.Player("the road was long.", 0, 2);
            DialogueEntry where = w.Player("where is Phi'rai?", 1, 2);
            DialogueEntry question = w.Npc("that's the question, isn't it.", 1, 3);
            DialogueEntry nine = w.Npc("nine days ago she went down into the Hollows. she said a week at most. Phi is never late. never, in all these years.", 0, 4);
            DialogueEntry yours = w.Npc("her letter says if she isn't back, Tally Ho! is yours to keep. so you're the keeper now. i'm Orik. i keep the books.", 0, 5);
            DialogueEntry fire = w.Say(c.Boog, "and i'm Boog! i keep the fire. mostly in the stove.", 0, 6);
            DialogueEntry list = w.Npc("Boog cooks. Boog also keeps a list of the things he's set alight. it's longer than the menu.", 0, 7);
            DialogueEntry onions = w.Npc("and we can't open without food. the storeroom holds three onions and a smell.", 0, 8);
            DialogueEntry smell = w.Say(c.Boog, "the smell's mine. i'm keeping it.", 0, 9);
            DialogueEntry hatch = w.Say(c.Boog, "but the hatch is right there! down in the Hollows, everything's an ingredient if you're brave about it.", 0, 10);
            DialogueEntry stayUp = w.Npc("i don't go down. my family went down for three hundred years. i came up. i'm staying up.", 0, 11);
            DialogueEntry teeth = w.Say(c.Boog, "bring back anything with meat on it. or teeth. i can work with teeth.", 0, 12);
            DialogueEntry go = w.Player("i'll go down.", 0, 13);
            w.Link(late, road, where);
            w.Link(road, nine);
            w.Link(where, question);
            w.Link(question, nine);
            w.Link(nine, yours);
            w.Link(yours, fire);
            w.Link(fire, list);
            w.Link(list, onions);
            w.Link(onions, smell);
            w.Link(smell, hatch);
            w.Link(hatch, stayUp);
            w.Link(stayUp, teeth);
            w.Link(teeth, go);
            w.Link(w.Start, late);
        }

        /// <summary>Home from the first delve (that night): what came up becomes supper. Boog is delighted; Orik counts it, and remembers Phi.</summary>
        static void WriteHomecoming(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, OpeningRules.Homecoming, c.Player, c.Boog,
                "Act I opening: home from the first delve (played once, that night). Branches on whether anything came home.");
            DialogueEntry supper = w.Npc("you're back! and you brought... let me see. oh. oh, that's a whole supper.", 0, 1, "HH_PartsHome() > 0");
            DialogueEntry counted = w.Say(c.Orik, "i've counted it. [lua(HH_PartsHome())] parts. by my sums that's an evening. a small one.", 0, 2);
            DialogueEntry cooking = w.Npc("you see? the Hollows go in, supper comes out. that's all cooking is.", 0, 3);
            DialogueEntry smell = w.Say(c.Orik, "Phi used to come up that hatch smelling just like you do now. i never asked what of.", 0, 4);
            DialogueEntry besides = w.Player("what else did she bring up?", 0, 5);
            DialogueEntry whatSmell = w.Player("what do i smell like?", 1, 5);
            DialogueEntry answers = w.Say(c.Orik, "answers, sometimes. never to anything i'd asked.", 0, 6);
            DialogueEntry deep = w.Npc("deep. you smell deep. it's a good smell on a cook.", 1, 6);
            w.Link(supper, counted);
            w.Link(counted, cooking);
            w.Link(cooking, smell);
            w.Link(smell, besides, whatSmell);
            w.Link(besides, answers);
            w.Link(whatSmell, deep);

            DialogueEntry back = w.Npc("you're back! that's the important part. the other important part was food.", 2, 1);
            DialogueEntry dropped = w.Say(c.Orik, "the Hollows keep what you drop. the books are taking it well. so am i.", 2, 2);
            DialogueEntry onions = w.Npc("tomorrow we fry the onions and serve them with confidence.", 2, 3);
            w.Link(back, dropped);
            w.Link(dropped, onions);

            DialogueEntry sleep = w.Say(c.Orik, "sleep. we open tomorrow evening. i'll have the board ready.", 1, 8);
            w.Link(answers, sleep);
            w.Link(deep, sleep);
            w.Link(onions, sleep);
            w.Link(w.Start, supper, back);
        }

        /// <summary>The first evening's prep: the menu, the stations, carrying plates, being paid. Only that.</summary>
        static void WriteFirstEvening(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, OpeningRules.FirstEvening, c.Player, c.Orik,
                "Act I opening: the first evening's prep (played once). Just the loop: the menu, cooking, serving, being paid.");
            DialogueEntry board = w.Npc("the board is yours. pick tonight's dishes from what's in the storeroom.", 0, 1);
            DialogueEntry stove = w.Say(c.Boog, "you cook at the stations, put it on the pass, carry it to whoever's hungry. want me on a station? just say.", 0, 2);
            DialogueEntry floor = w.Npc("they pay when they're fed. i'll be on the floor too. try not to be slower than me.", 0, 3);
            w.Link(board, stove);
            w.Link(stove, floor);
            w.Link(w.Start, board);
        }

        /// <summary>The first night's takings: Orik's verdict, then Boog has a question (Boog/Bomb), and the opening is over.</summary>
        static void WriteFirstTakings(DialogueDatabase db, Template template, Cast c, int id)
        {
            Conversation bomb = Find(db, BoogBomb);
            var w = new Writer(db, template, id, OpeningRules.FirstTakings, c.Player, c.Orik,
                "Act I opening: the first night's takings (played once). Leads into Boog/Bomb; the opening is complete when it ends.");
            DialogueEntry takings = w.Npc("that's the first night's takings. Phi's first night was worse. i have it written down.", 0, 1);
            DialogueEntry second = w.Npc("she used to say the first night is for finding out what's wrong with the second.", 0, 2);
            DialogueEntry ask = w.Say(c.Boog, "keeper. can i ask you something? a small thing. a medium thing.", 0, 3);
            DialogueEntry toBomb = w.Group("to Boog/Bomb", 0, 4, null);
            w.Link(takings, second);
            w.Link(second, ask);
            w.Link(ask, toBomb);
            w.LinkTo(toBomb, bomb);
            w.Link(w.Start, takings);
        }

        // ---------- Checkpoint C: the hubs (priority) and what they remember ----------

        /// <summary>A callback said once: true until its entry has played (a Dialogue System variable, saved with the dialogue).</summary>
        static string Unsaid(string flag) => $"Variable[\"{flag}\"] ~= true";
        static string Said(string flag) => $"Variable[\"{flag}\"] = true";
        static string Remembers(string who, string deed) => $"HH_Remembers(\"{who}\", \"{deed}\")";

        /// <summary>
        /// Talking to Boog (4g Checkpoint C), in priority order: his bomb coming home (the quest) and arrival day first; then, once
        /// each, what he remembers the keeper doing (the troll, his bomb, a wish kept, a clean cut); then Boog/Talk, his everyday
        /// conversation. Reorder or rewrite freely in the node editor: the first branch whose condition holds is taken.
        /// </summary>
        static void WriteBoogHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            Conversation bomb = Find(db, BoogBomb), talk = Find(db, StoryDialogue.BoogTalk);
            var w = new Writer(db, template, id, StoryDialogue.BoogHub, c.Player, c.Boog,
                "Boog, any time (4g Checkpoint C): the quest first, then one-time callbacks to what he remembers, then Boog/Talk.");
            DialogueEntry handOver = w.Group("critical: her return", 0, 1, BombHome);
            w.LinkTo(handOver, bomb);
            DialogueEntry arriving = w.Group("critical: arrival day", 1, 1, Arriving);
            w.LinkTo(arriving, talk);

            // The troll (his respect for nerve, loudly), with his bomb if he remembers that too.
            const string troll = "hh_boog_troll";
            DialogueEntry killed = w.Npc("you killed the Larder Troll. the actual Larder Troll. the one that eats the Cellars.", 2, 1,
                $"{Remembers("gunta", "felled_larder_troll")} and {Unsaid(troll)}", Said(troll));
            DialogueEntry both = w.Npc("first my bomb, now the troll. you're the best thing to happen to this kitchen since the stove.", 2, 2,
                Remembers("gunta", "returned_boogs_bomb"));
            DialogueEntry right = w.Npc("i've said for years it was edible. now it's dead, and i'm right.", 3, 2);
            DialogueEntry edible = w.Player("is it edible?", 2, 3);
            DialogueEntry nearly = w.Player("it nearly ate me.", 3, 3);
            DialogueEntry brave = w.Npc("parts of it. the brave parts. i'll find out which.", 2, 4);
            DialogueEntry word = w.Npc("nearly. best word in the language.", 3, 4);
            w.Link(killed, both, right);
            w.Link(both, edible, nearly);
            w.Link(right, edible, nearly);
            w.Link(edible, brave);
            w.Link(nearly, word);

            // His bomb, the next time they talk.
            const string shelf = "hh_boog_bomb";
            DialogueEntry listener = w.Npc("i told her about you. the bomb. i tell her things. she's a good listener, for a bomb.", 4, 1,
                $"{Remembers("gunta", "returned_boogs_bomb")} and {Unsaid(shelf)}", Said(shelf));

            // A wish kept: he heard it from the kitchen.
            const string wish = "hh_boog_wish";
            DialogueEntry thanks = w.Npc("someone asked for something special and got it. i heard them say thank you. over the stove. the stove's loud.", 5, 1,
                $"{Remembers("gunta", "kept_a_wish")} and {Unsaid(wish)}", Said(wish));

            // A clean cut at the block: what he respects most, after explosions.
            const string block = "hh_boog_butchery";
            DialogueEntry gristle = w.Npc("i saw you at the block. clean cuts. you didn't flinch at the gristle. i flinch at the gristle, and i love gristle.", 6, 1,
                $"{Remembers("gunta", "fine_butchery")} and {Unsaid(block)}", Said(block));
            DialogueEntry stove = w.Npc("you could work my stove. don't. but you could.", 6, 2, "HH_Respect(\"gunta\") >= 25");
            w.Link(gristle, stove);

            DialogueEntry everyday = w.Group("everyday: Boog/Talk", 7, 1, null);
            w.LinkTo(everyday, talk);
            w.Link(w.Start, handOver, arriving, killed, listener, thanks, gristle, everyday);
        }

        /// <summary>
        /// Talking to Orik (4g Checkpoint C): arrival day first; then, once each, what he remembers (the troll, quietly, and Phi;
        /// a wish kept, which he respects more than Boog does); then Orik/Talk, his everyday conversation.
        /// </summary>
        static void WriteOrikHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            Conversation talk = Find(db, StoryDialogue.OrikTalk);
            var w = new Writer(db, template, id, StoryDialogue.OrikHub, c.Player, c.Orik,
                "Orik, any time (4g Checkpoint C): arrival day first, then one-time callbacks to what he remembers, then Orik/Talk.");
            DialogueEntry arriving = w.Group("critical: arrival day", 0, 1, Arriving);
            w.LinkTo(arriving, talk);

            // The troll: a line in the ledger, and Phi.
            const string troll = "hh_orik_troll";
            DialogueEntry resolved = w.Npc("the Larder Troll is dead. i've moved it from 'risks' to 'resolved'. first entry in that column in years.", 1, 1,
                $"{Remembers("pip", "felled_larder_troll")} and {Unsaid(troll)}", Said(troll));
            DialogueEntry twice = w.Npc("Phi went after it, you know. twice. she came back both times and wouldn't say a word about it.", 1, 2);
            DialogueEntry after = w.Player("what was she after?", 1, 3);
            DialogueEntry back = w.Player("she'll come back.", 2, 3);
            DialogueEntry guarding = w.Npc("not the troll, she said. whatever it was sitting on.", 1, 4);
            DialogueEntry chair = w.Npc("she always has. i keep her chair dusted. don't tell Boog i dust things. he'll want to compare.", 2, 4);
            w.Link(resolved, twice);
            w.Link(twice, after, back);
            w.Link(after, guarding);
            w.Link(back, chair);

            // A wish kept: follow-through is what he respects.
            const string wish = "hh_orik_wish";
            DialogueEntry remembered = w.Npc("you remembered that patron's request, and made it. people come back to places that remember them.", 3, 1,
                $"{Remembers("pip", "kept_a_wish")} and {Unsaid(wish)}", Said(wish));
            DialogueEntry phiWay = w.Npc("Phi ran it that way. i'd started to think i was the only one who remembered how.", 3, 2, "HH_Respect(\"pip\") >= 15");
            w.Link(remembered, phiWay);

            DialogueEntry everyday = w.Group("everyday: Orik/Talk", 4, 1, null);
            w.LinkTo(everyday, talk);
            w.Link(w.Start, arriving, resolved, remembered, everyday);
        }

        // ---------- 4h Checkpoint A: things to look at, and five o'clock ----------

        /// <summary>
        /// Something looked at (4h): one line in the voice of no one (the narration speaker: no name, no portrait). Seeded once, like
        /// every conversation; the node editor owns it from then on.
        /// </summary>
        static void WriteLook(DialogueDatabase db, Template template, Cast c, int id, string title, string line)
        {
            var w = new Writer(db, template, id, title, c.Player, c.Narration,
                "4h Checkpoint A: something to look at. One line; add more, or conditions, here in the node editor.");
            DialogueEntry look = w.Npc(line, 0, 1);
            w.Link(w.Start, look);
        }

        /// <summary>
        /// Five o'clock (4h Checkpoint A): Orik notices the village winding down, the first time the keeper is inside Tally Ho! after
        /// five. Played once (the game records it); nothing ends or starts because of it.
        /// </summary>
        static void WriteOrikFive(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, SurfaceConversations.OrikFive, c.Player, c.Orik,
                "4h Checkpoint A: five o'clock, once. The village winds down; nothing is forced.");
            DialogueEntry five = w.Npc("that's five. the village is putting its boots by the door.", 0, 1);
            DialogueEntry yet = w.Npc("we open when you say so. i'll be here, counting.", 0, 2);
            w.Link(w.Start, five);
            w.Link(five, yet);
        }

        // ---------- Musashi (the owner's canon, 2026-10-07) ----------

        /// <summary>
        /// Musashi at the market cart: the first time, who he is (the Fortunate Five, the taste the Hollows took); every time after,
        /// a word about the produce and four things to ask (cooking blind, the curse, family, the hours). Toshi is only a name here:
        /// the quest to find him is later. A draft to rewrite in the node editor.
        /// </summary>
        static void WriteMusashiHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.MusashiHub, c.Player, c.Musashi,
                "Musashi at the market cart (2026-10-07): the first meeting once, then his everyday talk. Toshi's quest comes later.");
            const string met = "hh_musashi_met";
            DialogueEntry first = w.Npc("ah. Phi's new keeper. Orik said you'd find the cart. he says everyone does, eventually.", 0, 1, Unsaid(met), Said(met));
            DialogueEntry name = w.Npc("i'm Musashi. i sell what grows, what's caught, and what Grim swears is fresh.", 0, 2);
            DialogueEntry knewPhi = w.Player("you knew Phi?", 0, 3);
            DialogueEntry good = w.Player("what's good today?", 1, 3);
            DialogueEntry five = w.Npc("we were five, once. Phi, Grim, me, and two more. people called us the Fortunate Five. we were, mostly.", 0, 4);
            DialogueEntry everything = w.Npc("everything, i'm told. i can't taste a thing. the Hollows took that. so you'll have to tell me.", 1, 4);
            w.Link(first, name);
            w.Link(name, knewPhi, good);
            w.Link(knewPhi, five);
            w.Link(good, everything);

            DialogueEntry hello = w.Npc("morning, [lua(HH_PlayerName())]. the onions are loud today. you can hear a good onion, if you listen.", 3, 1);
            DialogueEntry askCook = w.Player("how do you cook without tasting?", 3, 2);
            DialogueEntry askCurse = w.Player("what happened, down there?", 4, 2);
            DialogueEntry askFamily = w.Player("any family?", 5, 2);
            DialogueEntry askHours = w.Player("when do you close?", 6, 2);
            DialogueEntry bye = w.Player("see you later.", 7, 2);
            DialogueEntry blind = w.Npc("i don't, anymore. i smell, i listen, and i watch faces. yours will do.", 3, 3);
            DialogueEntry curse = w.Npc("something down there liked my cooking too much. it took the taste for itself. i hope it chokes.", 4, 3);
            DialogueEntry sell = w.Npc("so now i sell. someone should make something good with all this, even if i can't tell.", 4, 4);
            DialogueEntry brother = w.Npc("a brother. Toshi. he went further down than any of us, and he hasn't come back up.", 5, 3);
            DialogueEntry name2 = w.Npc("if you ever hear his name below, tell me. that's all i ask.", 5, 4);
            DialogueEntry five2 = w.Npc("at five. after that, the onions need their sleep. so do i.", 6, 3);
            w.Link(hello, askCook, askCurse, askFamily, askHours, bye);
            w.Link(askCook, blind);
            w.Link(askCurse, curse);
            w.Link(curse, sell);
            w.Link(askFamily, brother);
            w.Link(brother, name2);
            w.Link(askHours, five2);

            w.Link(w.Start, first, hello);
        }

        // ---------- Kariaston's people (4h Checkpoint C) ----------

        static string Doing(string who, string activity) => $"HH_Doing(\"{who}\") == \"{activity}\"";
        const string TrollFelled = "HH_TimesDefeated(\"larder_troll\") >= 1";

        /// <summary>
        /// Maximo, wherever his day has him (the memorial, lunch at Tally Ho!, watching it, his porch, a vigil): the first time, a
        /// grand welcome with one honest sentence in it; once each, the troll and the nights below; every time, a greeting for what
        /// he's doing and his questions. Karias stays a name and a few true words: no sealing, no history lecture.
        /// </summary>
        static void WriteMaximoHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.MaximoHub, c.Player, c.Maximo,
                "Maximo (4h Checkpoint C): the first meeting once, callbacks once, then a greeting for what he's doing (HH_Doing) and his questions.");
            const string met = "hh_maximo_met";
            DialogueEntry hark = w.Npc("hark! the new keeper of Tally Ho!, the watch's own tavern! Kariaston bids you welcome, by my mouth.", 0, 1, Unsaid(met), Said(met));
            DialogueEntry name = w.Npc("i am Maximo: founder of this village, mayor of it, and its watchman, until the Hollows close or i do.", 0, 2);
            DialogueEntry askFounder = w.Player("you founded Kariaston?", 0, 3);
            DialogueEntry askWatch = w.Player("a watchman? over what?", 1, 3);
            DialogueEntry named = w.Npc("i named it. the building was mostly other people. i gave speeches while they lifted things.", 0, 4);
            DialogueEntry karias = w.Npc("it's named for Karias. my apprentice. he was better than me. i don't say that often. i say it to him.", 0, 5);
            DialogueEntry under = w.Npc("over what lies under your tavern, and under that. someone must watch it. i volunteered, long ago, loudly.", 1, 4);
            DialogueEntry ends = w.Npc("i don't go down anymore. you will. so we keep watch from both ends, you and i.", 1, 5);
            DialogueEntry forth = w.Npc("now! go forth and be magnificent. or open at five. either is a service to the realm.", 0, 6);
            w.Link(hark, name);
            w.Link(name, askFounder, askWatch);
            w.Link(askFounder, named);
            w.Link(named, karias);
            w.Link(karias, forth);
            w.Link(askWatch, under);
            w.Link(under, ends);
            w.Link(ends, forth);

            const string troll = "hh_maximo_troll";
            DialogueEntry felled = w.Npc("the Larder Troll, felled by a keeper of my own village! i shall have a song written. Bart will refuse. i'll write it.", 2, 1,
                $"{TrollFelled} and {Unsaid(troll)}", Said(troll));
            DialogueEntry careful = w.Npc("and keeper: come back up. every time. that's the part of the song that matters.", 2, 2);
            w.Link(felled, careful);

            const string nights = "hh_maximo_nights";
            DialogueEntry fifth = w.Npc("five nights below, and up again each morning. keep doing that part. i'll keep a lamp lit at this end.", 3, 1,
                $"HH_Day() >= 6 and {Unsaid(nights)}", Said(nights));

            DialogueEntry proclaim = w.Npc("good morning, keeper! today's proclamation: the weather is adequate. the realm rejoices.", 4, 1, Doing("maximo", "proclaim"));
            DialogueEntry lunch = w.Npc("ah, the keeper of this fine house! the soup is a triumph. i've proclaimed it. Orik has added it to my book.", 5, 1, Doing("maximo", "lunch"));
            DialogueEntry watching = w.Npc("i'm watching your tavern. not you: the tavern, and what's under it. carry on being watched.", 6, 1, Doing("maximo", "watch"));
            DialogueEntry vigil = w.Npc("evening, keeper. i stand with him a while at dusk. he never liked the dark coming on alone.", 7, 1, Doing("maximo", "vigil"));
            DialogueEntry home = w.Npc("the watchman's day is done. the watch never is. that's the sort of thing i say at bedtime.", 8, 1);
            DialogueEntry askKarias = w.Player("tell me about Karias.", 5, 3);
            DialogueEntry askDecree = w.Player("any proclamations?", 6, 3);
            DialogueEntry bye = w.Player("good day, Maximo.", 7, 3);
            DialogueEntry staff = w.Npc("he held his staff like a wish come true. the best wizard i ever taught. the only one, but still.", 5, 4);
            DialogueEntry nose = w.Npc("the statue gets his nose wrong. i keep meaning to say. i never do.", 5, 5);
            DialogueEntry decree = w.Npc("by order of the mayor: nobody is to be glum before noon. after noon, glum in moderation.", 6, 4);
            foreach (DialogueEntry greeting in new[] { proclaim, lunch, watching, home }) w.Link(greeting, askKarias, askDecree, bye);
            w.Link(vigil, askKarias, bye);
            w.Link(askKarias, staff);
            w.Link(staff, nose);
            w.Link(askDecree, decree);
            w.Link(w.Start, hark, felled, fifth, proclaim, lunch, watching, vigil, home);
        }

        /// <summary>
        /// Kaloren Frosthand: kind first, strange second. The first time, a courteous neighbour who likes to be useful; once each, a
        /// few small oddities (old stonework, breathing) that can be shrugged off; every time, a greeting for what he's doing (on a
        /// herb day, at Grim and Ogrin's door) and his questions. Nothing says what he is or ties him to Ogrin's illness.
        /// </summary>
        static void WriteKalorenHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.KalorenHub, c.Player, c.Kaloren,
                "Kaloren (4h Checkpoint C): the first meeting once, small oddities once each, then a greeting for what he's doing (HH_Doing) and his questions.");
            const string met = "hh_kaloren_met";
            DialogueEntry hello = w.Npc("oh, hello. you must be the new keeper. i'm Kaloren. i live in the tower: the tall thing with the pointed hat.", 0, 1, Unsaid(met), Said(met));
            DialogueEntry useful = w.Npc("if anything at Tally Ho! needs mending, or warming, or gently persuading, do knock. i like to be useful.", 0, 2);
            DialogueEntry askStove = w.Player("can you light the stove?", 0, 3);
            DialogueEntry thanks = w.Player("thank you, Kaloren.", 1, 3);
            DialogueEntry warm = w.Npc("i can make it warm. i don't do fire. fire and i had a disagreement once, and fire won.", 0, 4);
            DialogueEntry light = w.Npc("not at all. it's good to see a light on at Tally Ho! again.", 1, 4);
            w.Link(hello, useful);
            w.Link(useful, askStove, thanks);
            w.Link(askStove, warm);
            w.Link(thanks, light);

            const string stone = "hh_kaloren_stone";
            DialogueEntry arches = w.Npc("the Cellars under you have lovely stonework. the arches were better before the third collapse.", 2, 1,
                $"HH_Day() >= 3 and {Unsaid(stone)}", Said(stone));
            DialogueEntry askThird = w.Player("the third collapse?", 2, 2);
            DialogueEntry second = w.Npc("did i say third? the second. i read a great deal. very old books. dreadfully old.", 2, 3);
            w.Link(arches, askThird);
            w.Link(askThird, second);

            const string breath = "hh_kaloren_breath";
            DialogueEntry forgot = w.Npc("...forgive me. i was listening so hard i forgot to breathe. it happens at my age.", 3, 1,
                $"HH_Day() >= 5 and {Unsaid(breath)}", Said(breath));

            DialogueEntry tower = w.Npc("good morning. i was tidying the stairs. there are rather a lot of them. it keeps an old man honest.", 4, 1, Doing("kaloren", "tower"));
            DialogueEntry reading = w.Npc("i'm reading about the Cellars. whoever wrote this has never been in them. charming, though.", 5, 1, Doing("kaloren", "reading"));
            DialogueEntry herbs = w.Npc("ah, keeper. Ogrin's herbs. every third day, like the post. they help him. they don't mend him.", 6, 1, Doing("kaloren", "herbs"));
            DialogueEntry plain = w.Npc("hello again, keeper. a fine day for it, whatever it turns out to be.", 7, 1);
            DialogueEntry askHerbs = w.Player("where do the herbs grow?", 5, 3);
            DialogueEntry askCure = w.Player("will they cure him?", 6, 3);
            DialogueEntry askGloves = w.Player("why the gloves? it's warm out.", 7, 3);
            DialogueEntry bye = w.Player("see you, Kaloren.", 8, 3);
            DialogueEntry cold = w.Npc("here and there. the shy ones grow in cold places. i'm good with cold places.", 5, 4);
            DialogueEntry honest = w.Npc("no. i wish they would. they give him good days, and good days are worth a great deal.", 6, 4);
            DialogueEntry fine = w.Npc("i feel the cold terribly. or rather, i don't, which is worse. never mind. lovely day.", 7, 4);
            foreach (DialogueEntry greeting in new[] { tower, reading, plain }) w.Link(greeting, askHerbs, askGloves, bye);
            w.Link(herbs, askHerbs, askCure, bye);
            w.Link(askHerbs, cold);
            w.Link(askCure, honest);
            w.Link(askGloves, fine);
            w.Link(w.Start, hello, arches, forgot, tower, reading, herbs, plain);
        }

        /// <summary>
        /// Grim (a dwarf: restrained Scots): dry, practical, his own man. The first time, who he is to Phi and to the boy; once each,
        /// a herb day and the keeper's knife; every time, a greeting for what he's doing and his questions. His livelihood is still
        /// open, so nothing here says what he does for a living. Ogrin is "the boy"; nobody says "dad".
        /// </summary>
        static void WriteGrimHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.GrimHub, c.Player, c.Grim,
                "Grim (4h Checkpoint C): the first meeting once, callbacks once, then a greeting for what he's doing (HH_Doing) and his questions.");
            const string met = "hh_grim_met";
            DialogueEntry heard = w.Npc("you're Phi's keeper. aye, i heard. the whole village heard. Maximo made a speech at the well.", 0, 1, Unsaid(met), Said(met));
            DialogueEntry grim = w.Npc("Grim. i live in the brown house with the boy. if he asks you for stories, tell him the true ones.", 0, 2);
            DialogueEntry askPhi = w.Player("you knew Phi?", 0, 3);
            DialogueEntry askBoy = w.Player("the boy?", 1, 3);
            DialogueEntry delved = w.Npc("i delved with her, a long while back. her, Musashi and the rest. that's all you'll get on an empty stomach.", 0, 4);
            DialogueEntry mine = w.Npc("Ogrin. he's mine. no' by blood. he'll talk your ear off about the Hollows. he's never been. he'll no' be going.", 1, 4);
            w.Link(heard, grim);
            w.Link(grim, askPhi, askBoy);
            w.Link(askPhi, delved);
            w.Link(askBoy, mine);

            const string herbDay = "hh_grim_herbs";
            DialogueEntry herbs = w.Npc("Kaloren's been by with the herbs. never takes a coin. never says where he picks them. i never ask. we get on.", 2, 1,
                $"HH_Today(\"herbs\") and {Unsaid(herbDay)}", Said(herbDay));
            const string knife = "hh_grim_knife";
            DialogueEntry blade = w.Npc("let's see your knife. ...aye. that'll do. keep it dry, and dinnae lend it to Boog.", 3, 1,
                $"HH_Day() >= 3 and {Unsaid(knife)}", Said(knife));

            DialogueEntry chores = w.Npc("morning. splitting stone for the wall. it doesnae need splitting. i need to split it.", 4, 1, Doing("grim", "chores"));
            DialogueEntry errand = w.Npc("Musashi's onions are fine today. i told him so. he cannae taste them, so someone has to.", 5, 1, Doing("grim", "errand"));
            DialogueEntry pond = w.Npc("watching the boy. from over here, so he doesnae know. he knows.", 6, 1, Doing("grim", "pond"));
            DialogueEntry home = w.Npc("evening. the boy's in. the kettle's on. that's the day done, near enough.", 7, 1);
            DialogueEntry askDelver = w.Player("you were a delver?", 5, 3);
            DialogueEntry askOgrin = w.Player("how's Ogrin?", 6, 3);
            DialogueEntry bye = w.Player("see you, Grim.", 7, 3);
            DialogueEntry stopped = w.Npc("aye. a lot of years. then one time i came back up with more than i went down with, and i stopped.", 5, 4);
            DialogueEntry good = w.Npc("good day today. he's out. dinnae tell him i said so; he'll think he's cured.", 6, 4, "HH_Today(\"ogrin_well\")");
            DialogueEntry tired = w.Npc("tired. it comes and it goes. Kaloren's herbs help. nothing fixes it.", 7, 4);
            foreach (DialogueEntry greeting in new[] { chores, errand, pond, home }) w.Link(greeting, askDelver, askOgrin, bye);
            w.Link(askDelver, stopped);
            w.Link(askOgrin, good, tired);
            w.Link(w.Start, heard, herbs, blade, chores, errand, pond, home);
        }

        /// <summary>
        /// Ogrin: a ten-year-old with opinions first, a sick child second. The first time (outside, or at his window on a bad day),
        /// questions about what's really down there; once each, the troll, the herbs and Boog's bomb; every time, a greeting for
        /// what he's doing and his questions. He calls Grim "Grim"; no line here calls him anything else.
        /// </summary>
        static void WriteOgrinHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.OgrinHub, c.Player, c.Ogrin,
                "Ogrin (4h Checkpoint C): the first meeting once (outside, or at his window), callbacks once, then a greeting for what he's doing (HH_Doing).");
            const string met = "hh_ogrin_met";
            string inBed = $"({Doing("ogrin", "bed")} or {Doing("ogrin", "home")})";
            DialogueEntry window = w.Npc("oh! it's you. the keeper. i'm in bed. it's not catching. it's just me. what's down the hatch? the real stuff.", 0, 1,
                $"{Unsaid(met)} and {inBed}", Said(met));
            DialogueEntry outside = w.Npc("you're the new keeper! you go down the hatch! every night! what's down there? the real stuff, not the Grim version.", 1, 1,
                Unsaid(met), Said(met));
            DialogueEntry slimes = w.Player("slimes, mostly.", 0, 3);
            DialogueEntry danger = w.Player("it's dangerous down there.", 1, 3);
            DialogueEntry knew = w.Npc("i knew it! i drew slimes on my map. Grim said i was guessing. i was guessing correctly.", 0, 4);
            DialogueEntry looks = w.Npc("everyone says that. i know what dangerous is. i'm asking what it looks like.", 1, 4);
            DialogueEntry back = w.Npc("come back and tell me things. i've got the Cellars mostly right. the rest is dragons till i know better.", 0, 5);
            w.Link(window, slimes, danger);
            w.Link(outside, slimes, danger);
            w.Link(slimes, knew);
            w.Link(danger, looks);
            w.Link(knew, back);
            w.Link(looks, back);

            const string troll = "hh_ogrin_troll";
            DialogueEntry big = w.Npc("you killed the Larder Troll? how big? bigger than Grim? bigger than the cart? i'm drawing it bigger than the cart.", 2, 1,
                $"{TrollFelled} and {Unsaid(troll)}", Said(troll));
            const string herbs = "hh_ogrin_herbs";
            DialogueEntry leaves = w.Npc("Kaloren brought the leaves. they taste like a cellar. they work, though. tomorrow's a going-out day, probably.", 3, 1,
                $"HH_Today(\"herbs\") and {Unsaid(herbs)}", Said(herbs));
            const string bomb = "hh_ogrin_bomb";
            DialogueEntry name = w.Npc("is it true Boog keeps a bomb on a shelf? does it have a name? it should have a name. i'd call it Gerald.", 4, 1,
                $"HH_Day() >= 3 and {Unsaid(bomb)}", Said(bomb));

            DialogueEntry yard = w.Npc("Grim's splitting the same rock again. he says it's for the wall. there is no wall.", 5, 1, Doing("ogrin", "yard"));
            DialogueEntry maps = w.Npc("i'm drawing the Hollows. this is the Cellars. this is where i think the troll lives. tell me if i'm wrong.", 6, 1, Doing("ogrin", "maps"));
            DialogueEntry goat = w.Npc("Bart's playing the one about the goat. it's my favorite. the goat wins.", 7, 1, Doing("ogrin", "listening"));
            DialogueEntry bed = w.Npc("i'm all right. i'm tired in a way sleeping doesn't fix. don't make the face. everyone makes the face.", 8, 1, Doing("ogrin", "bed"));
            DialogueEntry night = w.Npc("Grim says bed. i say the moon's barely up. Grim wins. Grim always wins. good night, keeper.", 9, 1);
            DialogueEntry askMap = w.Player("what's on your map?", 6, 3);
            DialogueEntry askFeel = w.Player("how are you feeling?", 7, 3);
            DialogueEntry bye = w.Player("bye, Ogrin.", 8, 3);
            DialogueEntry trade = w.Npc("a lot of question marks. you could fix some. i'll trade you: one true story, one of my drawings.", 6, 4);
            DialogueEntry interesting = w.Npc("fine. i'm always fine, then i'm not, then i am again. ask me something interesting.", 7, 4);
            foreach (DialogueEntry greeting in new[] { yard, maps, goat }) w.Link(greeting, askMap, askFeel, bye);
            w.Link(bed, askMap, bye);
            w.Link(askMap, trade);
            w.Link(askFeel, interesting);
            w.Link(w.Start, window, outside, big, leaves, name, yard, maps, goat, bed, night);
        }

        /// <summary>
        /// Bart (an orc bard with a southern Texas voice, in his words and rhythm, never in misspellings): the first Visitor who
        /// stayed. The first time, who he is and why he never left; once each, the troll's verse and Maximo's song; every time, a
        /// greeting for what he's doing and his questions. He sings about ordinary people; Maximo makes speeches about heroes.
        /// </summary>
        static void WriteBartHub(DialogueDatabase db, Template template, Cast c, int id)
        {
            var w = new Writer(db, template, id, StoryDialogue.BartHub, c.Player, c.Bart,
                "Bart (4h Checkpoint C): the first meeting once, callbacks once, then a greeting for what he's doing (HH_Doing) and his questions.");
            const string met = "hh_bart_met";
            DialogueEntry howdy = w.Npc("well now, howdy. you're the keeper. i'm Bart. i play a little, sing a little, and listen a whole lot more.", 0, 1, Unsaid(met), Said(met));
            DialogueEntry passing = w.Npc("i was passing through on my way someplace else. played one night at Tally Ho!, and i'm still fixing to leave.", 0, 2);
            DialogueEntry askSing = w.Player("what do you sing about?", 0, 3);
            DialogueEntry askStay = w.Player("why did you stay?", 1, 3);
            DialogueEntry folks = w.Npc("folks. ordinary ones. a song about a hero's just a list. a baker who's scared of bread, now that's a song.", 0, 4);
            DialogueEntry fed = w.Npc("Phi fed me, and folks here kept being interesting. a fellow can't leave in the middle of a good story.", 1, 4);
            DialogueEntry verse = w.Npc("you do anything worth a verse, i'll hear about it. i always hear about it.", 0, 5);
            w.Link(howdy, passing);
            w.Link(passing, askSing, askStay);
            w.Link(askSing, folks);
            w.Link(askStay, fed);
            w.Link(folks, verse);
            w.Link(fed, verse);

            const string troll = "hh_bart_troll";
            DialogueEntry heard = w.Npc("heard you laid out the Larder Troll. i'm working on a verse. tell me true: did it roar, or did it scream?", 2, 1,
                $"{TrollFelled} and {Unsaid(troll)}", Said(troll));
            DialogueEntry roared = w.Player("it roared.", 2, 2);
            DialogueEntry screamed = w.Player("it screamed.", 3, 2);
            DialogueEntry rhymes = w.Npc("roared it is. a scream don't rhyme with nothing, and a roar rhymes with Orik's floor.", 2, 3);
            DialogueEntry dignity = w.Npc("screamed, huh. i'll put roared. folks want a troll to keep a little dignity.", 3, 3);
            w.Link(heard, roared, screamed);
            w.Link(roared, rhymes);
            w.Link(screamed, dignity);
            const string mayor = "hh_bart_mayor";
            DialogueEntry song = w.Npc("Maximo asked me to write a song about him. i said i'd think on it. i've been thinking on it a good long while.", 4, 1,
                $"HH_Day() >= 4 and {Unsaid(mayor)}", Said(mayor));

            DialogueEntry tuning = w.Npc("morning. just tuning. this string's been sharp since spring. i reckon it's got opinions.", 5, 1, Doing("bart", "tuning"));
            DialogueEntry gossip = w.Npc("Grim says Musashi's onions are fine. Musashi says Grim's fine. that's about as close as those two get to a hug.", 6, 1, Doing("bart", "gossip"));
            DialogueEntry playing = w.Npc("this one's about the goat. Ogrin's favorite. the goat wins every verse. that goat's got grit.", 7, 1, Doing("bart", "playing"));
            DialogueEntry evening = w.Npc("evening, keeper. the wagon's warm and the song's half done. come by tomorrow, i'll have the other half.", 8, 1);
            DialogueEntry askNews = w.Player("any news?", 6, 3);
            DialogueEntry askFive = w.Player("a song about the Fortunate Five?", 7, 3);
            DialogueEntry bye = w.Player("see you, Bart.", 8, 3);
            DialogueEntry news = w.Npc("Maximo proclaimed the weather adequate again. that's three days running. folks worry he's going soft.", 6, 4);
            DialogueEntry half = w.Npc("half of one. i won't sing the half i don't know. making things up about real folks gets you a black eye.", 7, 4);
            foreach (DialogueEntry greeting in new[] { tuning, gossip, playing, evening }) w.Link(greeting, askNews, askFive, bye);
            w.Link(askNews, news);
            w.Link(askFive, half);
            w.Link(w.Start, howdy, heard, song, tuning, gossip, playing, evening);
        }

        // ---------- Checkpoint A's proofs (only to recognise them unedited) ----------

        /// <summary>
        /// Whether <paramref name="conversation"/> is one of Checkpoint A's proof conversations exactly as its tooling wrote it (lines,
        /// speakers, conditions, scripts and links). Only then is it replaced; anything touched by hand is kept.
        /// </summary>
        public static bool IsUneditedCheckpointA(Conversation conversation, Template template, Cast cast)
        {
            var scratch = ScriptableObject.CreateInstance<DialogueDatabase>();
            try
            {
                if (conversation.Title == StoryDialogue.BoogTalk) LegacyBoog(scratch, template, cast);
                else if (conversation.Title == StoryDialogue.OrikTalk) LegacyOrik(scratch, template, cast);
                else return false;
                return Signature(scratch.conversations[0]) == Signature(conversation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scratch);
            }
        }

        /// <summary>What a conversation says and does, without its ids, positions or descriptions.</summary>
        public static string Signature(Conversation conversation)
        {
            var text = new StringBuilder();
            foreach (DialogueEntry e in conversation.dialogueEntries.OrderBy(e => e.id))
            {
                text.Append(e.id).Append('|').Append(e.ActorID).Append('|').Append(e.DialogueText).Append('|').Append(e.MenuText).Append('|')
                    .Append(e.conditionsString).Append('|').Append(e.userScript).Append('|').Append(e.isGroup).Append('|');
                foreach (Link l in e.outgoingLinks) text.Append(l.destinationConversationID == conversation.id ? "" : "x").Append(l.destinationDialogueID).Append(',');
                text.Append('\n');
            }
            return text.ToString();
        }

        static void LegacyBoog(DialogueDatabase db, Template template, Cast c)
        {
            var w = new Writer(db, template, -1, StoryDialogue.BoogTalk, c.Player, c.Boog, string.Empty);
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
            DialogueEntry noticed = w.Npc("the tusks are up. good. they keep an eye on the stew for me.", 2, 1, BoogRemembersTusks);
            DialogueEntry waiting = w.Npc("the wall over the bar is still bare. it's begging for something with teeth.", 3, 1,
                "HH_QuestState(\"proof_trophy_wall\") == \"active\"");
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

        static void LegacyOrik(DialogueDatabase db, Template template, Cast c)
        {
            var w = new Writer(db, template, -1, StoryDialogue.OrikTalk, c.Player, c.Orik, string.Empty);
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
