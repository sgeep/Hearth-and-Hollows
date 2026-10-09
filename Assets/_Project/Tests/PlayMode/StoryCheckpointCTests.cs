using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Quests;
using Hearthdelve.Shared.Story;
using Hearthdelve.Story;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Story.Relationships;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using LevelManager = MoreMountains.TopDownEngine.LevelManager;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4g Checkpoint C through the real game (saves in a temp folder): the troll's fall, read loudly by Boog and quietly by Orik,
    /// said once each and still remembered after a scene change, a save and Continue; repeats that fade; a wish kept, which Orik
    /// values more; Boog's bomb remembered alongside the troll; the tusks never hiding the bomb offer; the tavern's panels
    /// stepping aside while someone talks.
    /// </summary>
    public class StoryCheckpointCTests : BootFixture
    {
        static StoryHost Host => StoryHost.Instance;
        static RelationshipAdapter Social => Host.Relationships;
        static HearthDialogueUI Box => Object.FindAnyObjectByType<HearthDialogueUI>();

        // ---------- The way through ----------

        IEnumerator BootToMenu()
        {
            yield return Boot();
            yield return WaitUntil(() => Host != null && Host.Relationships != null, 10f, "the story");
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase, string what)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, what);
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 5f, "the scene revealed");
            yield return null;
        }

        IEnumerator ToTheDelve()
        {
            yield return BootToMenu();
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return null;
        }

        IEnumerator Home()
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract());
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night");
        }

        // ---------- Talking ----------

        IEnumerator Talk(string id)
        {
            Assert.That(StoryServices.Conversations.Talk(id), $"talking to {id}");
            yield return null;
            yield return null;
            Assert.That(Box.IsOpen);
        }

        static IEnumerator Next()
        {
            for (int i = 0; i < 10 && Box.IsRevealing; i++)
            {
                Box.Advance();
                yield return null;
            }
            Box.Advance();
            yield return null;
            yield return null;
        }

        static IEnumerator Choose(string text)
        {
            yield return WaitUntil(() => Box.IsChoosing && !Box.ChoicesLocked, 2f, $"the choice \"{text}\"");
            int i = Enumerable.Range(0, Box.ResponseCount).First(n => Box.ChoiceText(n) == text);
            Box.Choose(i);
            yield return null;
            yield return null;
        }

        static IEnumerator ToEnd()
        {
            for (int i = 0; i < 12 && Box.IsOpen; i++)
            {
                Assert.That(Box.IsChoosing, Is.False, $"a choice waits: {(Box.IsChoosing ? Box.ChoiceText(0) : string.Empty)}");
                yield return Next();
            }
            Assert.That(Box.IsOpen, Is.False);
        }

        // (The troll's fall, the tusks' priority and the night's panel stepping aside are in StoryCheckpointATests: the test review
        // merged them into its tests on the same path.)

        // ---------- Repeats, and two readings ----------

        [UnityTest]
        public IEnumerator RepeatedDeeds_Fade_AndAWishKept_MattersMoreToOrik()
        {
            yield return ToTheDelve();
            yield return Home();

            // Butchery: only the keeper's own, only when it's fine; each repeat counts for less, the fifth for nothing.
            float before = Social.Respect("gunta"), orik = Social.Respect("pip");
            EventBus<PartButchered>.Publish(new PartButchered("spider_leg", "cut", 2, 0.95f, CharacterIds.Boog));
            EventBus<PartButchered>.Publish(new PartButchered("spider_leg", "cut", 2, 0.6f, "keeper"));
            Assert.That(Social.Respect("gunta"), Is.EqualTo(before), "Boog's own cuts and a routine one aren't remarkable");
            var gains = new float[6];
            for (int i = 0; i < gains.Length; i++)
            {
                float was = Social.Respect("gunta");
                EventBus<PartButchered>.Publish(new PartButchered("spider_leg", "cut", 2, 0.95f, "keeper"));
                gains[i] = Social.Respect("gunta") - was;
            }
            Assert.That(gains[0], Is.GreaterThan(5f), "the first fine cut");
            Assert.That(gains[1], Is.EqualTo(gains[0] * 0.5f).Within(0.05f));
            Assert.That(gains[2], Is.EqualTo(gains[0] * 0.25f).Within(0.05f));
            Assert.That((gains[4], gains[5]), Is.EqualTo((0f, 0f)), "no farming");
            Assert.That(Social.Respect("pip"), Is.EqualTo(orik), "the block is Boog's business");

            // A wish kept: both learn of it; Orik respects it more.
            float boogWas = Social.Respect("gunta"), orikWas = Social.Respect("pip");
            EventBus<CustomerRequestCompleted>.Publish(new CustomerRequestCompleted("visitor/1/1", 1, "spider_skewer", 0.95f, 4, 1));
            float boogWish = Social.Respect("gunta") - boogWas, orikWish = Social.Respect("pip") - orikWas;
            Assert.That(orikWish, Is.GreaterThan(boogWish), "follow-through is Orik's kind of thing");
            EventBus<CustomerRequestCompleted>.Publish(new CustomerRequestCompleted("visitor/1/2", 2, "spider_skewer", 0.7f, 4, 1));
            Assert.That(Social.TimesSeen("pip", "kept_a_wish"), Is.EqualTo(1), "an ordinary request met isn't a deed");

            // What they say about it: Boog the block first (his hub's order), Orik the wish.
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("someone asked for something special"), "the wish comes before the block in Boog's hub");
            yield return ToEnd();
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("i saw you at the block. clean cuts"));
            yield return ToEnd();
            yield return Talk(CharacterIds.Orik);
            Assert.That(Box.Line, Is.EqualTo("you remembered that patron's request, and made it. folk come back to places that remember them."));
            yield return ToEnd();
        }

        // ---------- The bomb, with the troll ----------

        [UnityTest]
        public IEnumerator BoogRemembersHisBomb_AlongsideTheTroll_InHisOwnOrder()
        {
            yield return ToTheDelve();
            yield return Home();
            // Boog's bomb, home and handed over (the quest's own path is Checkpoint B's test).
            Host.GiveQuest("boogs_bomb", CharacterIds.Boog);
            Flow.State.QuestObjects.BringHome("boogs_bomb");
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("is that... you found her!"), "her return comes before everything");
            yield return Next();
            yield return Next();
            yield return Choose("she's all yours.");
            yield return ToEnd();
            Assert.That(Social.Remembers("gunta", "returned_boogs_bomb"));

            // Then the troll falls (its first clear): Boog puts the two together.
            EventBus<BossFirstCleared>.Publish(new BossFirstCleared("larder_troll"));
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("you killed the Larder Troll"));
            yield return Next();
            Assert.That(Box.Line, Is.EqualTo("first my bomb, now the troll. you're the best thing to happen to this kitchen since the stove."));
            yield return Next();
            yield return Choose("it nearly ate me.");
            Assert.That(Box.Line, Is.EqualTo("nearly. best word in the language."));
            yield return ToEnd();
            // Next time: his bomb.
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("i told her about you"));
            yield return ToEnd();
            // And she can be asked about: the shelf.
            yield return Talk(CharacterIds.Boog);
            yield return Next();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Does.StartWith("she's on the shelf over the stove now"));
            yield return ToEnd();
        }
    }
}
