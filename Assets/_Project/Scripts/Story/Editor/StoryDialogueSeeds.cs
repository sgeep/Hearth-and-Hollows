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
            public Actor Player, Boog, Orik, Narration;
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
