using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Story;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4h Checkpoint A through the real game (saves in a temp folder): the free daytime loads Tally Ho! and Kariaston together; the
    /// keeper wakes upstairs and walks; the stairs and the front door go both ways without reloading anything, switching the camera
    /// and the lights; the surface clock runs and stands still for a conversation, a menu, Decorate Mode and the market; the
    /// market trades until five and is closed after; five o'clock never starts Prep, and the keeper keeps their feet; the menu
    /// board asks before the evening begins (before five or after), and the village unloads for Prep; the storeroom, the delve meal
    /// and decorating are places in the room.
    /// </summary>
    public class SurfaceCheckpointATests : LookTestFixture
    {
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static SurfaceClock Clock => Flow.State.Surface;

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
            SurfaceTime.SettingsOverride = null;
            SurfacePause.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        // ---------- The way in ----------

        /// <summary>A game past its opening, on a free daytime (day 2: a quiet delve, the night, sleep).</summary>
        IEnumerator Daytime()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            yield return Revealed();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            // Wait for each reveal, as a player must (the cover takes input): back-to-back transitions overlap their fades.
            yield return Revealed();
            Flow.Sleep();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
        }

        /// <summary>
        /// Until the scene is in view and settled: not loading and uncovered for several frames running (a fade from clear starts
        /// at alpha 0, so one uncovered frame can't tell "revealed" from "about to cover").
        /// </summary>
        static IEnumerator Revealed()
        {
            int settled = 0;
            float started = Time.realtimeSinceStartup;
            while (settled < 5)
            {
                bool clear = !Flow.IsLoading && (Flow.Transition == null || !Flow.Transition.IsCovering);
                settled = clear ? settled + 1 : 0;
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(8f), "the scene revealed");
                yield return null;
            }
        }

        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();

        static bool OnFootNow => InputMaps.Find(InputMaps.Tavern, TavernActions.Interact) is { enabled: true };

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        static TavernInteractable Place(TavernInteractableKind kind) => TavernInteractable.All.First(t => t.Kind == kind);

        // ---------- Waking, walking, the stairs and the door ----------

        [UnityTest]
        public IEnumerator TheDaytime_LoadsTallyHoAndKariaston_AndTheKeeperWakesUpstairs_OnFoot()
        {
            yield return Daytime();
            Assert.That(Flow.LoadedScenes, Is.EqualTo(new[] { GameScenes.Tavern, GameScenes.Kariaston }));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameScenes.Tavern));
            Assert.That(PropertyArea.Current.Id, Is.EqualTo(PropertyArea.GuestRoomId), "upstairs, in Bram's room");
            Assert.That(SurfaceArea.Current.Id, Is.EqualTo(SurfaceArea.GuestRoomId));
            Vector2 wake = TavernDirector.WakePoint(AreaFurniture.Find(PropertyArea.GuestRoomId));
            Assert.That(Vector2.Distance(Keeper.position, wake), Is.LessThan(0.6f), "beside the bed");
            Assert.That(OnFootNow, "the daytime is walked, not a menu");
            Assert.That(Find<MorningScreen>().IsShown, Is.False, "no morning panel");
            Assert.That(Find<SurfaceClockView>().IsShown, "the clock shows");

            // Walking works.
            Vector2 before = Keeper.position;
            Hold(Key.D);
            yield return new WaitForSeconds(0.4f);
            ReleaseKeys();
            Assert.That(Keeper.position.x, Is.GreaterThan(before.x + 0.5f));
        }

        [UnityTest]
        public IEnumerator TheStairs_StillGoBothWays()
        {
            yield return Daytime();
            AreaPassage down = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).First(p => p.From.Id == PropertyArea.GuestRoomId);
            Assert.That(down.CanPass, "the stairs work in the daytime");
            down.Pass(Keeper);
            yield return Frames(2);
            Assert.That((PropertyArea.Current.Id, SurfaceArea.Current.Id), Is.EqualTo((PropertyArea.TavernId, SurfaceArea.TavernId)));
            AreaPassage up = Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).First(p => p.From.Id == PropertyArea.TavernId);
            up.Pass(Keeper);
            yield return Frames(2);
            Assert.That(PropertyArea.Current.Id, Is.EqualTo(PropertyArea.GuestRoomId));
        }

        [UnityTest]
        public IEnumerator TheFrontDoor_TakesTheKeeperOutAndBack_WithoutReloading_AndSwitchesTheViewAndTheLights()
        {
            yield return Daytime();
            int players = GameObject.FindGameObjectsWithTag("Player").Length, staff = Object.FindObjectsByType<StaffAgent>(FindObjectsSortMode.None).Length;
            SurfaceDoor inside = SurfaceDoor.Find(SurfaceDoor.FrontInside), outside = SurfaceDoor.Find(SurfaceDoor.FrontOutside);
            Assert.That(inside, Is.Not.Null);
            Assert.That(outside, Is.Not.Null, "the village's side of the door is loaded beside the tavern");
            Light2D ambient = SurfaceArea.Find(SurfaceArea.TavernId).Lights[0], daylight = SurfaceArea.Find(SurfaceArea.KariastonId).Lights[0];
            Assert.That(ambient, Is.Not.SameAs(daylight));

            // Walk down the stairs, then out through the door on foot (the trigger, not a shortcut).
            Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).First(p => p.From.Id == PropertyArea.GuestRoomId).Pass(Keeper);
            yield return Frames(2);
            Teleport(Keeper, inside.transform.position + new Vector3(0f, 1.5f, 0f));
            yield return Frames(2);
            Hold(Key.S);
            yield return WaitUntil(() => SurfaceArea.Current.Id == SurfaceArea.KariastonId, 3f, "outside");
            ReleaseKeys();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Flow.IsLoading, Is.False, "nothing reloaded");
            Assert.That(Vector2.Distance(Keeper.position, outside.Arrival), Is.LessThan(0.6f), "on Tally Ho!'s step");
            Assert.That((daylight.enabled, ambient.enabled), Is.EqualTo((true, false)), "one global light at a time");
            yield return Frames(2);
            Transform view = GameObject.Find(TavernView.CameraName).transform;
            Rect bounds = SurfaceArea.Current.Bounds;
            Assert.That(bounds.Contains((Vector2)view.position), "the camera follows the keeper inside the village");
            Assert.That(Mathf.Abs(view.position.x - Keeper.position.x), Is.LessThan(1f), "centred on the keeper horizontally (well inside the bounds)");

            // Back in, and out and in again: cheap every time, nothing doubled.
            for (int i = 0; i < 3; i++)
            {
                outside.Pass(Keeper);
                yield return Frames(2);
                Assert.That(SurfaceArea.Current.Id, Is.EqualTo(SurfaceArea.TavernId));
                Assert.That((ambient.enabled, daylight.enabled), Is.EqualTo((true, false)));
                Assert.That((Vector2)view.position, Is.EqualTo(SurfaceArea.Current.HoldPoint), "the camera holds on the room again");
                inside.Pass(Keeper);
                yield return Frames(2);
                Assert.That(SurfaceArea.Current.Id, Is.EqualTo(SurfaceArea.KariastonId));
            }
            outside.Pass(Keeper);
            yield return Frames(2);
            Assert.That(GameObject.FindGameObjectsWithTag("Player").Length, Is.EqualTo(players));
            Assert.That(Object.FindObjectsByType<StaffAgent>(FindObjectsSortMode.None).Length, Is.EqualTo(staff));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(3), "Boot, the tavern and the village");
        }

        // ---------- The clock ----------

        [UnityTest]
        public IEnumerator TheClock_RunsInTheDaytime_AndStandsStillForTalkMenusAndDecorating()
        {
            SurfaceClockSettings fast = SurfaceClockSettings.Default;
            fast.realSecondsPerGameMinute = 0.02f;
            SurfaceTime.SettingsOverride = fast;
            yield return Daytime();
            yield return WaitUntil(() => SurfaceTime.Instance.Still == SurfaceStill.Running, 3f, "the clock running");
            double start = Clock.Minute;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Clock.Minute, Is.GreaterThan(start + 2), "it runs");

            // A conversation.
            Assert.That(StoryServices.Conversations.Play(SurfaceConversations.PhiPortrait));
            yield return Frames(2);
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Talking));
            // A look has no speaker: the name line is empty, never a missing-string key.
            var box = Object.FindAnyObjectByType<Hearthdelve.Story.Presentation.HearthDialogueUI>();
            Assert.That(box.SpeakerName, Is.Empty);
            Assert.That(box.GetComponentsInChildren<SuperTextMesh>().Select(t => t.text), Has.None.StartsWith("#"), "no missing strings in the box");
            double talking = Clock.Minute;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Clock.Minute, Is.EqualTo(talking), "talking costs no time");
            DialogueManager.StopAllConversations();
            yield return Frames(3);
            Assert.That(OnFootNow, "on foot again after talking");

            // A full-screen menu.
            MenuPause.Push();
            yield return Frames(2);
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Menu));
            MenuPause.Pop();

            // Decorating.
            DecorateMode.Instance.Enter();
            yield return Frames(2);
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Held));
            double decorating = Clock.Minute;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Clock.Minute, Is.EqualTo(decorating));
            DecorateMode.Instance.Leave();
            // Leaving fades back to the room (the clock waits for that too), then runs again.
            yield return WaitUntil(() => SurfaceTime.Instance.Still == SurfaceStill.Running, 3f, "running again after decorating");
            Assert.That(OnFootNow, "on foot again after decorating");
        }

        [UnityTest]
        public IEnumerator TheDecorateKey_OpensDecorateMode_IndoorsOnly()
        {
            yield return Daytime();
            Hold(Key.Tab);
            yield return Frames(3);
            ReleaseKeys();
            yield return Frames(2);
            Assert.That(DecorateMode.Instance.IsActive, "Tab opens Decorate Mode upstairs");
            DecorateMode.Instance.Leave();
            yield return Frames(2);
            SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
            yield return Frames(2);
            Assert.That(DecorateMode.Instance.CanEnter, Is.False, "the village isn't decorated");
        }

        // ---------- The market, five o'clock and Prep ----------

        [UnityTest]
        public IEnumerator TheMarket_TradesBeforeFive_HoldingTheClock_AndIsPackedUpAfter()
        {
            yield return Daytime();
            Flow.DebugAddGold(20);
            MarketStall stall = Object.FindAnyObjectByType<MarketStall>();
            Assert.That(stall.IsOpen);
            stall.Interactable.Use();
            yield return Frames(2);
            MarketPanel market = Find<MarketPanel>();
            Assert.That(market.IsOpen, "the stall opens the market");
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Held), "browsing costs no time");
            int stock = Flow.State.Storeroom.TotalCount;
            Assert.That(market.Buy(0));
            Assert.That(Flow.State.Storeroom.TotalCount, Is.GreaterThan(stock));
            market.Close();
            yield return Frames(2);
            Assert.That(OnFootNow);

            Clock.Set(1020, SurfaceTime.CurrentSettings);
            yield return Frames(3);
            Assert.That(stall.IsOpen, Is.False);
            Assert.That(stall.Interactable.Hint.Kind, Is.EqualTo(TavernHintKind.Note), "it says it's closed");
            stall.Interactable.Use();
            yield return Frames(2);
            Assert.That(market.IsOpen, Is.False, "closed at five");
            Assert.That(Flow.BuyFromMarket(Flow.Database.market.offers[0]), Is.False);
        }

        [UnityTest]
        public IEnumerator FiveOClock_NeverStartsPrep_TheKeeperStillWalksAndTalks_AndPrepStartsWhenChosen()
        {
            yield return Daytime();
            SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
            yield return Frames(2);
            Clock.Set(1020, SurfaceTime.CurrentSettings);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(SurfaceTime.Band, Is.EqualTo(SurfaceBand.Evening));
            Assert.That((Flow.Phase, Director.Phase), Is.EqualTo((DayPhase.Daytime, TavernPhase.Daytime)), "five o'clock is not the evening");
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.AtCutoff));
            Assert.That(OnFootNow, "still on foot outside");

            // Back in: Orik notices, once, and then the keeper walks on.
            SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            yield return WaitUntil(() => StoryServices.Conversations.IsTalking, 3f, "five o'clock");
            DialogueManager.StopAllConversations();
            yield return Frames(3);
            Assert.That(Flow.State.Story.SeenHints, Does.Contain(FiveOClock.SeenId));
            Assert.That(OnFootNow);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(StoryServices.Conversations.IsTalking, Is.False, "once");
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Daytime));
            Assert.That(StoryServices.Conversations.Talk(Shared.Characters.CharacterIds.Orik), "Orik can still be talked to");
            DialogueManager.StopAllConversations();
            yield return Frames(3);

            // The menu board asks; the evening begins only on yes.
            PrepConfirm ask = Find<PrepConfirm>();
            Place(TavernInteractableKind.MenuBoard).Use();
            yield return Frames(2);
            Assert.That(ask.IsOpen);
            ask.Cancel();
            yield return Frames(2);
            Assert.That((ask.IsOpen, Director.Phase, OnFootNow), Is.EqualTo((false, TavernPhase.Daytime, true)), "not yet");
            Place(TavernInteractableKind.MenuBoard).Use();
            yield return Frames(2);
            ask.Confirm();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "Prep");
            Assert.That(Flow.LoadedScenes, Is.EqualTo(new[] { GameScenes.Tavern }), "the village unloads for the evening");
            Assert.That(Flow.Phase, Is.EqualTo(DayPhase.Evening));
        }

        [UnityTest]
        public IEnumerator Prep_CanBeginBeforeFive()
        {
            yield return Daytime();
            Assert.That(SurfaceTime.Band, Is.EqualTo(SurfaceBand.Morning));
            EventBus<DaytimePlaceUsed>.Publish(new DaytimePlaceUsed(TavernInteractableKind.MenuBoard));
            yield return Frames(2);
            Find<PrepConfirm>().Confirm();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "Prep");
            Assert.That(Flow.IsLoaded(GameScenes.Kariaston), Is.False);
        }

        // ---------- The storeroom and the delve meal ----------

        [UnityTest]
        public IEnumerator TheStoreroomShelves_AndTheStations_OpenTheStoreroomAndTheDelveMeal()
        {
            yield return Daytime();
            MorningScreen panel = Find<MorningScreen>();
            Place(TavernInteractableKind.Storeroom).Use();
            yield return Frames(3);
            Assert.That((panel.Mode, panel.IsShown), Is.EqualTo((DaytimePanel.Storeroom, true)));
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Held));
            Assert.That(panel.Options, Is.Empty, "the shelves show the stock, not the meals");
            panel.Close();
            yield return Frames(2);
            Assert.That((panel.IsShown, OnFootNow), Is.EqualTo((false, true)));

            // The Grill in the daytime: its delve meals, cooked at its panel.
            Director.FillStoreroom();
            TavernInteractable grill = Place(TavernInteractableKind.Grill);
            Assert.That(grill.IsAvailable, "the Grill cooks the delve meal in the daytime");
            grill.Use();
            yield return Frames(3);
            Assert.That((panel.Mode, panel.Station), Is.EqualTo((DaytimePanel.Meal, CookStation.Grill)));
            Assert.That(panel.Options.All(r => r.station == CookStation.Grill));
            int card = panel.Options.ToList().FindIndex(Director.CanCookDelveMeal);
            Assume.That(card, Is.GreaterThanOrEqualTo(0), "the filled storeroom can make a Grill meal");
            panel.Cook(card);
            yield return Frames(2);
            Assert.That(KeeperWork.Instance.ActiveCook, Is.Not.Null, "the station's panel");
            KeeperWork.Instance.FinishCook(1f);
            yield return Frames(2);
            Assert.That(Director.HasEatenDelveMeal);
            Assert.That(OnFootNow, "back on foot");
        }
    }
}
