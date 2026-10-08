using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4h Checkpoint D, "This is a community", in the game: Gimp's night in the keeper's room (once, after an own delve, the morning
    /// intact, never again on Continue), his visits to Boog by the stairs, the village overheard (one bubble, giving way to
    /// conversations), familiar faces at dinner as ordinary customers, Glimmer's light, and Decorate Mode holding the room still.
    /// </summary>
    public class VillageCheckpointDTests : LookTestFixture
    {
        string m_SaveDir;
        readonly object m_Hold = new();

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        static VillagePresence Presence => VillagePresence.Instance;
        static readonly Vector2 k_Origin = new(200f, 0f);

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            AmbientMoments.ResetForTests();
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            SurfaceTime.SettingsOverride = null;
            SurfacePause.Clear();
            MusicHolds.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

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

        /// <summary>A new game to its first free day (day 2), the keeper waking upstairs; the clock held unless <paramref name="hold"/> is off.</summary>
        IEnumerator Daytime(bool hold = true)
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
            yield return Revealed();
            yield return Sleep(hold);
        }

        IEnumerator Sleep(bool hold)
        {
            Flow.Sleep();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            if (hold) SurfacePause.Hold(m_Hold);
            yield return Frames(2);
        }

        /// <summary>The evening kept shut, the delve, the night, sleep: the next morning (Gimp's scene, if due, plays to its end).</summary>
        IEnumerator NextDay(bool hold = true, bool finishGimp = true)
        {
            SurfacePause.Release(m_Hold);
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            Flow.SkipService();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            yield return Sleep(hold);
            if (finishGimp) yield return FinishGimpsNight();
        }

        static IEnumerator FinishGimpsNight()
        {
            yield return Frames(3);
            if (NightVisitor.Instance == null || !NightVisitor.Instance.Playing) yield break;
            yield return WaitUntil(() => DialogueManager.IsConversationActive || !NightVisitor.Instance.Playing, 5f, "Gimp's conversation");
            if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
            yield return WaitUntil(() => !NightVisitor.Instance.Playing, 10f, "the morning");
        }

        static void Outside()
        {
            if (SurfaceArea.Current == null || SurfaceArea.Current.Id != SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
        }

        /// <summary>The keeper downstairs in Tally Ho!'s main room (from the village or from upstairs, where every day starts).</summary>
        static void Inside()
        {
            if (SurfaceArea.Current != null && SurfaceArea.Current.Id == SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            SurfaceArea main = SurfaceArea.Find(SurfaceArea.TavernId);
            PropertyArea.Current = main.GetComponent<PropertyArea>();
            SurfaceArea.Enter(main);
        }

        static void KeeperAtVillage(Vector2 tile) => KeeperAtWorld(k_Origin + tile);

        static void KeeperAtWorld(Vector2 at)
        {
            Keeper.position = at;
            Keeper.transform.position = new Vector3(at.x, at.y, Keeper.transform.position.z);
            Physics2D.SyncTransforms();
        }

        static IEnumerator At(int minute)
        {
            Flow.State.Surface.Restore(minute);
            yield return Frames(2);
            Presence.Refresh();
            yield return null;
        }

        static Villager Here(string id) => Villager.All.Single(v => v.CharacterId == id && v.Shown);

        static ScheduleWorld World => VillageLife.World().Value;

        // ---------- Gimp's night ----------

        [UnityTest]
        public IEnumerator GimpComesUpTheHatch_TheFirstMorningAfterAnOwnDelve_Once_AndTheMorningIsIntact()
        {
            yield return Daytime();
            Assert.That(Flow.State.Day, Is.EqualTo(2));
            Assert.That(NightVisitor.Instance.Playing, Is.False, "not the first free morning: the keeper hasn't been down on their own yet");
            Assert.That(Flow.State.Story.SeenHints, Has.No.Member(CommunityRules.GimpIntro));
            Light2D roomLight = SurfaceArea.Find(SurfaceArea.GuestRoomId).Lights[0];
            float day = roomLight.intensity;

            yield return NextDay(finishGimp: false);
            Assert.That(Flow.State.Day, Is.EqualTo(3));
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 5f, "Gimp's conversation");
            Assert.That(NightVisitor.Instance.Playing);
            Assert.That(DialogueManager.lastConversationStarted, Is.EqualTo(NightVisitor.Conversation));
            roomLight = SurfaceArea.Find(SurfaceArea.GuestRoomId).Lights[0];   // the tavern is loaded afresh each morning
            Assert.That(roomLight.intensity, Is.LessThan(day * 0.5f), "it's night in the keeper's room");
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.None), "silence");
            Assert.That(SurfaceTime.Instance.Still, Is.Not.EqualTo(SurfaceStill.Running), "the clock holds");
            Assert.That(PropertyArea.Current.Id, Is.EqualTo(PropertyArea.GuestRoomId), "in the keeper's room");
            // The night didn't touch the morning sleep made.
            Assert.That(Flow.State.Vigor.Current, Is.EqualTo(Flow.VigorSettings.maxVigor), "Vigor refilled");
            Assert.That(Flow.State.Surface.WholeMinute, Is.LessThanOrEqualTo(SurfaceTime.CurrentSettings.dayStartMinute + 10));
            DialogueManager.StopConversation();
            yield return WaitUntil(() => !NightVisitor.Instance.Playing, 10f, "the morning");
            Assert.That(Flow.State.Story.SeenHints, Has.Member(CommunityRules.GimpIntro), "once per save");
            Assert.That(roomLight.intensity, Is.EqualTo(day).Within(1e-3f), "the light is back exactly");
            yield return Frames(3);
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.Day), "the day's tune");
            Assert.That(Flow.State.Day, Is.EqualTo(3), "an ordinary morning of the same day");

            // Continue: never again.
            Flow.Save();
            Flow.QuitToMenu();
            yield return WaitUntil(() => !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return Revealed();
            Assert.That(Flow.Continue());
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(NightVisitor.Instance.Playing, Is.False, "not on Continue");
            Assert.That(DialogueManager.IsConversationActive, Is.False);
            // Nor the next morning.
            yield return NextDay(finishGimp: false);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(NightVisitor.Instance.Playing, Is.False, "an ordinary night");
        }

        // ---------- his visits ----------

        [UnityTest]
        public IEnumerator Gimp_VisitsBoog_OnHisDays_DownTheStairs_AndTalks()
        {
            yield return Daytime();
            Flow.MarkHintSeen(CommunityRules.GimpIntro);
            for (int i = 0; i < 6 && !VillageDays.GimpVisit(World.Seed, Flow.State.Day, World.Settings); i++) yield return NextDay();
            Assert.That(VillageDays.GimpVisit(World.Seed, Flow.State.Day, World.Settings), "one of his days within six");
            Inside();
            KeeperAtWorld(new Vector2(22f, 8f));
            yield return At(13 * 60 + 50);
            Assert.That(Villager.All.Any(v => v.CharacterId == CharacterIds.Gimp && v.Shown), Is.False, "not yet");
            yield return At(14 * 60);
            Villager gimp = Here(CharacterIds.Gimp);
            Assert.That(gimp.Walking, "down the stairs, on foot");
            Assert.That(Vector2.Distance(gimp.transform.position, gimp.Entrance.Value), Is.LessThan(1.5f), "from the stairs, not the front door");
            yield return WaitUntil(() => !Here(CharacterIds.Gimp).Walking, 30f, "at his table");
            Assert.That(Here(CharacterIds.Gimp).Activity, Is.EqualTo("boog"));
            Assert.That(Director.Layout.Seats.Any(s => Vector2.Distance(s.SitPoint, Here(CharacterIds.Gimp).transform.position) < 0.05f));
            yield return Frames(2);
            Here(CharacterIds.Gimp).Talk.Use();
            yield return Frames(3);
            Assert.That(DialogueManager.lastConversationStarted, Is.EqualTo("Gimp/Hub"));
            DialogueManager.StopConversation();
            yield return Frames(2);
            yield return At(16 * 60 + 30);
            yield return WaitUntil(() => !Villager.All.Any(v => v.CharacterId == CharacterIds.Gimp && v.Shown), 30f, "gone back down");
        }

        // ---------- overheard ----------

        [UnityTest]
        public IEnumerator TheVillage_TalksAmongItself_OneBubble_AndGivesWayToAConversation()
        {
            yield return Daytime();
            SurfacePause.Release(m_Hold);
            SurfaceTime.SettingsOverride = new SurfaceClockSettings
            {
                dayStartMinute = 480, cutoffMinute = 1020, afternoonStartMinute = 720, realSecondsPerGameMinute = 1000f,
                maxRealSecondsPerFrame = 0.1f, displayStepMinutes = 10, marketOpenMinute = 480, marketCloseMinute = 1020,
            };
            Outside();
            KeeperAtVillage(new Vector2(38f, 9f));
            yield return At(12 * 60);
            yield return WaitUntil(() => !Here(CharacterIds.Bart).Walking, 30f, "Bart at the market");
            IBarkService barks = StoryServices.Barks;
            var bubbles = Object.FindAnyObjectByType<AmbientBarks>();
            yield return WaitUntil(() => barks.IsBarking, 6f, "an exchange overheard");
            Assert.That(bubbles.ShowingSpeaker, Is.EqualTo(CharacterIds.Bart).Or.EqualTo(CharacterIds.Musashi));
            Assert.That(bubbles.Showing, Is.Not.Null.And.Not.Empty);
            for (int i = 0; i < 30; i++)
            {
                Assert.That(bubbles.Bubbles, Is.LessThanOrEqualTo(1), "one bubble at a time");
                yield return null;
            }
            // Talking to someone stops it at once.
            Here(CharacterIds.Musashi).Talk.Use();
            yield return Frames(2);
            Assert.That(barks.IsBarking, Is.False, "a conversation comes first");
            Assert.That(bubbles.Bubbles, Is.Zero);
            DialogueManager.StopConversation();
            yield return Frames(3);
            // And the village stays quiet for a while after.
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.That(barks.IsBarking, Is.False, "a cooldown between exchanges");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Overheard_NeverPlays_InDecorateMode_OrAMenu()
        {
            yield return Daytime();
            SurfacePause.Release(m_Hold);
            MenuPause.Push();
            Assert.That(AmbientBarks.Blocked, "a menu");
            MenuPause.Pop();
            DecorateMode.Instance.Enter();
            yield return Frames(2);
            Assert.That(AmbientBarks.Blocked, "Decorate Mode");
            Assert.That(StoryServices.Barks.Play(StoryDialogueTitles.GrimOgrin), Is.False);
            DecorateMode.Instance.Leave();
            yield return Frames(2);
            Assert.That(AmbientBarks.Blocked, Is.False);
        }

        static class StoryDialogueTitles
        {
            public const string GrimOgrin = "Ambient/GrimOgrin";
        }

        // ---------- dinner ----------

        [UnityTest]
        public IEnumerator AFamiliarFace_ComesToDinner_AsAnOrdinaryCustomer_InTheirOwnLook()
        {
            yield return Daytime();
            SurfacePause.Release(m_Hold);
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            Director.ArrivalsPaused = true;
            Assert.That(Director.IsServing, "the doors open");
            Assert.That(Director.FamiliarFaces.Count, Is.LessThanOrEqualTo(2));
            var maximo = TavernContentOf(Director).namedPatrons.Single(p => p.character == CharacterIds.Maximo);
            CustomerAgent agent = Director.SpawnFamiliarFace(maximo);
            Assert.That(agent.Logic.CharacterId, Is.EqualTo(CharacterIds.Maximo));
            Assert.That(agent.Look.Layers[0].sprite.name, Does.StartWith("KnightIdle"), "the blue knight, as in Kariaston");
            Assert.That(Villager.All.Count(v => v.CharacterId == CharacterIds.Maximo && v.Shown), Is.Zero, "nobody else is him tonight");
            Time.timeScale = 4f;
            yield return WaitUntil(() => agent.Logic.State is CustomerState.Ordering or CustomerState.WaitingForFood, 40f, "seated and ordering");
            Time.timeScale = 1f;
            Assert.That(Director.Agents.Count(a => a.Logic.CharacterId == CharacterIds.Maximo), Is.EqualTo(1));
        }

        static TavernContent TavernContentOf(TavernDirector director) =>
            (TavernContent)typeof(TavernDirector).GetField("m_Content", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(director);

        // ---------- Glimmer ----------

        [UnityTest]
        public IEnumerator OgrinsLight_ShowsAtHisWindow_OnItsEvenings_AndOnlyThen()
        {
            yield return Daytime();
            Flow.MarkHintSeen(CommunityRules.GimpIntro);
            Outside();
            KeeperAtVillage(new Vector2(50f, 9f));
            bool sawLit = false, sawDark = false;
            for (int i = 0; i < 10 && !(sawLit && sawDark); i++)
            {
                if (i > 0)
                {
                    yield return NextDay();
                    Outside();
                    KeeperAtVillage(new Vector2(50f, 9f));
                }
                yield return At(17 * 60);
                yield return WaitUntil(() => Here(CharacterIds.Ogrin).Indoors, 30f, "Ogrin home");
                bool due = VillageDays.GlimmerEvening(World.Seed, Flow.State.Day, World.Settings);
                yield return new WaitForSeconds(1.6f);
                // Kariaston is loaded afresh each day: find tonight's.
                Assert.That(Object.FindAnyObjectByType<WindowLight>().Lit, Is.EqualTo(due), $"day {Flow.State.Day}");
                if (due) sawLit = true;
                else sawDark = true;
            }
            Assert.That(sawLit, "a lit evening within ten");
            yield return At(16 * 60);
            yield return new WaitForSeconds(1.6f);
            Assert.That(Object.FindAnyObjectByType<WindowLight>().Lit, Is.False, "never before five");
        }

        // ---------- captures (run by hand: BatchLogs/community_*.png) ----------

        [UnityTest, Explicit]
        public IEnumerator CaptureTheCommunity()
        {
            yield return Daytime();
            yield return NextDay(finishGimp: false);
            yield return WaitUntil(() => DialogueManager.IsConversationActive, 6f, "Gimp's night");
            yield return new WaitForSecondsRealtime(2f);
            TavernEveningCaptures.Capture("BatchLogs/community_night.png");
            DialogueManager.StopConversation();
            yield return WaitUntil(() => !NightVisitor.Instance.Playing, 10f, "the morning");

            // Overheard at the market.
            SurfacePause.Release(m_Hold);
            SurfaceTime.SettingsOverride = new SurfaceClockSettings
            {
                dayStartMinute = 480, cutoffMinute = 1020, afternoonStartMinute = 720, realSecondsPerGameMinute = 1000f,
                maxRealSecondsPerFrame = 0.1f, displayStepMinutes = 10, marketOpenMinute = 480, marketCloseMinute = 1020,
            };
            AmbientMoments.ResetForTests();
            Outside();
            KeeperAtVillage(new Vector2(38f, 9f));
            yield return At(12 * 60);
            yield return WaitUntil(() => StoryServices.Barks.IsBarking, 30f, "an exchange");
            yield return new WaitForSecondsRealtime(0.6f);
            TavernEveningCaptures.Capture("BatchLogs/community_overheard.png");

            // Gimp at the bar, on one of his days.
            for (int i = 0; i < 6 && !VillageDays.GimpVisit(World.Seed, Flow.State.Day, World.Settings); i++) yield return NextDay(hold: false);
            Inside();
            KeeperAtWorld(new Vector2(22f, 7f));
            yield return At(14 * 60 + 10);
            yield return WaitUntil(() => Villager.All.Any(v => v.CharacterId == CharacterIds.Gimp && v.Shown && !v.Walking), 30f, "Gimp at his table");
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture("BatchLogs/community_gimp.png");

            // A familiar face at dinner.
            SurfaceTime.SettingsOverride = null;
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            Director.ArrivalsPaused = true;
            TavernContent content = TavernContentOf(Director);
            Director.SpawnFamiliarFace(content.namedPatrons.Single(p => p.character == CharacterIds.Maximo));
            Director.SpawnFamiliarFace(content.namedPatrons.Single(p => p.character == CharacterIds.Kaloren));
            Time.timeScale = 4f;
            yield return new WaitForSeconds(24f);
            Time.timeScale = 1f;
            TavernEveningCaptures.Capture("BatchLogs/community_dinner.png");
        }

        // ---------- Decorate Mode ----------

        [UnityTest]
        public IEnumerator DecorateMode_HoldsTheRoomStill_AndAfterwardsEveryoneFindsTheirSpot()
        {
            yield return Daytime();
            Inside();
            KeeperAtWorld(new Vector2(14f, 3f));
            yield return At(10 * 60 + 50);
            yield return At(11 * 60);
            yield return WaitUntil(() => Villager.All.Any(v => v.CharacterId == CharacterIds.Maximo && v.Shown && v.Area == PropertyArea.TavernId && v.Walking), 20f, "Maximo walking in");
            Villager maximo = Villager.All.Single(v => v.CharacterId == CharacterIds.Maximo && v.Shown && v.Area == PropertyArea.TavernId);
            DecorateMode.Instance.Enter();
            yield return Frames(2);
            Vector3 held = maximo.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(Vector3.Distance(maximo.transform.position, held), Is.LessThan(0.01f), "nobody strolls through the furniture being moved");
            DecorateMode.Instance.Leave();
            yield return Frames(3);
            Assert.That(maximo.Walking, Is.False);
            Assert.That(Vector2.Distance(maximo.transform.position, ScheduleAnchor.Find("tavern.table").Spot), Is.LessThan(0.05f), "at his seat");
        }
    }
}
