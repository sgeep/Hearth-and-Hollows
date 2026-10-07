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
    public class StoryCheckpointCTests : LookTestFixture
    {
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static StoryHost Host => StoryHost.Instance;
        static RelationshipAdapter Social => Host.Relationships;
        static HearthDialogueUI Box => Object.FindAnyObjectByType<HearthDialogueUI>();

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            RoomRunner.StartInArenaOverride = false;
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        // ---------- The way through ----------

        IEnumerator BootToMenu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady && Host != null && Host.Relationships != null, 10f, "the story");
            yield return WaitUntil(() => Flow.Transition == null || !Flow.Transition.IsCovering, 5f, "the menu revealed");
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

        IEnumerator DefeatTheTroll()
        {
            Player.GetComponent<EssenceHealth>().GodMode = true;
            var encounter = Object.FindAnyObjectByType<BossEncounter>();
            yield return WaitUntil(() => encounter.State == BossEncounterState.Fighting, 8f, "the troll");
            var health = encounter.GetComponent<BossHealth>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "the defeat recorded");
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

        /// <summary>The first line someone says now (then the conversation is closed unheard).</summary>
        IEnumerator FirstLine(string id, Action<string> line)
        {
            yield return Talk(id);
            line(Box.Line);
            DialogueManager.StopAllConversations();
            yield return null;
        }

        // ---------- The troll ----------

        [UnityTest]
        public IEnumerator TheTrollsFall_BoogLoudly_OrikQuietly_SaidOnce_AndStillRememberedAfterContinue()
        {
            RoomRunner.StartInArenaOverride = true;
            yield return ToTheDelve();
            float boogRespect = Social.Respect("gunta"), orikRespect = Social.Respect("pip");
            yield return DefeatTheTroll();
            yield return Home();

            // Both learned of it, from the first clear; each reads it by their own values.
            Assert.That(Social.Remembers("gunta", "felled_larder_troll") && Social.Remembers("pip", "felled_larder_troll"));
            float boogGain = Social.Respect("gunta") - boogRespect, orikGain = Social.Respect("pip") - orikRespect;
            Assert.That(boogGain, Is.GreaterThan(orikGain * 1.5f), "Boog's respect for nerve");
            Assert.That(orikGain, Is.GreaterThan(3f), "Orik's, quieter");

            // Boog, loudly.
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Is.EqualTo("you killed the Larder Troll. the actual Larder Troll. the one that eats the Cellars."));
            yield return Next();
            Assert.That(Box.Line, Does.StartWith("i've said for years it was edible"), "no bomb to remember yet");
            yield return Next();
            yield return Choose("is it edible?");
            Assert.That(Box.Line, Does.StartWith("parts of it. the brave parts"));
            yield return ToEnd();

            // Orik, quietly, and Phi.
            yield return Talk(CharacterIds.Orik);
            Assert.That(Box.Line, Does.StartWith("aye, the Larder Troll is dead. i've moved it from 'risks' to 'resolved'"));
            yield return Next();
            Assert.That(Box.Line, Does.StartWith("Phi went after it"));
            yield return Next();
            yield return Choose("what was she after?");
            Assert.That(Box.Line, Is.EqualTo("no' the troll, she said. whatever it was sitting on."));
            yield return ToEnd();

            // Said once: next time, their everyday conversations.
            string boog = null, orik = null;
            yield return FirstLine(CharacterIds.Boog, l => boog = l);
            yield return FirstLine(CharacterIds.Orik, l => orik = l);
            Assert.That(boog, Does.StartWith("the stove's hot"));
            Assert.That(orik, Does.StartWith("good evening, Bram"));

            // A scene change, a save, quit and Continue: remembered, still not repeated.
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next day");
            Assert.That(Social.Remembers("gunta", "felled_larder_troll"));
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "continued");
            Assert.That(Social.Remembers("gunta", "felled_larder_troll") && Social.Remembers("pip", "felled_larder_troll"), "remembered after Continue");
            Assert.That(Social.Respect("gunta"), Is.EqualTo(boogRespect + boogGain).Within(1e-3f), "nothing replayed or lost");
            Assert.That(Social.TimesSeen("gunta", "felled_larder_troll"), Is.EqualTo(1), "no duplicate deed after the reload");
            yield return FirstLine(CharacterIds.Boog, l => boog = l);
            yield return FirstLine(CharacterIds.Orik, l => orik = l);
            Assert.That(boog, Does.StartWith("the stove's hot"), "the callback stays said");
            Assert.That(orik, Does.StartWith("good evening, Bram"));
        }

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

        // ---------- Priority ----------

        [UnityTest]
        public IEnumerator TheTusks_AreRemarkedOnce_AndNeverHideTheBombOffer()
        {
            yield return ToTheDelve();
            yield return Home();
            Host.Commit(DeedSource.TrophyDisplayed);
            Assert.That(Social.Remembers("gunta", "displayed_trophy"));
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("you hung the Larder Troll's tusks"));
            yield return Next();
            yield return Next();
            yield return Choose("they do look good up there.");
            yield return ToEnd();
            yield return Talk(CharacterIds.Boog);
            Assert.That(Box.Line, Does.StartWith("the stove's hot"));
            yield return Next();
            yield return Choose("about your bomb...");
            Assert.That(Box.Line, Is.EqualTo("i lost something in the Hollows. my favorite bomb."), "the quest is never out of reach");
            DialogueManager.StopAllConversations();
        }

        // ---------- Presentation ----------

        [UnityTest]
        public IEnumerator TheNightsPanel_StepsAside_WhileSomeoneTalks()
        {
            yield return ToTheDelve();
            yield return Home();
            var aside = Object.FindAnyObjectByType<NightScreen>().GetComponentInParent<StepAsideWhileTalking>();
            Assert.That(aside, Is.Not.Null);
            Assert.That(aside.SteppedAside, Is.False);
            yield return Talk(CharacterIds.Orik);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(aside.SteppedAside, "the night's summary makes way for the conversation");
            DialogueManager.StopAllConversations();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(aside.SteppedAside, Is.False, "and comes back after");
        }
    }
}
