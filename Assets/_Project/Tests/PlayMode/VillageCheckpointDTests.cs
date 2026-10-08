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
            Assert.That(MusicHolds.Current, Is.EqualTo(MusicCue.None), "the night's silence let go");
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.None), "and the room is as quiet as Tally Ho! is by day");
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

        /// <summary>2026-10-08 (the owner's playtest): an exchange in the square kept showing after the keeper went into Tally Ho!.</summary>
        [UnityTest]
        public IEnumerator Overheard_Stops_WhenTheKeeperGoesIndoors_AndShortLinesStayUpLongEnough()
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
            // A line stays up at least the minimum, however short.
            string first = bubbles.Showing;
            float shown = Time.realtimeSinceStartup;
            yield return WaitUntil(() => bubbles.Showing != first, 15f, "the next line");
            Assert.That(Time.realtimeSinceStartup - shown, Is.GreaterThanOrEqualTo(3.4f), "long enough to read");
            yield return WaitUntil(() => barks.IsBarking && bubbles.Showing != null, 6f, "still talking");
            // In through the front door: it ends at once and doesn't follow the keeper in.
            SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            yield return Frames(2);
            Assert.That(barks.IsBarking, Is.False, "the square's exchange ended at the door");
            Assert.That(bubbles.Bubbles, Is.Zero);
            for (int i = 0; i < 60; i++)
            {
                Assert.That(bubbles.Bubbles, Is.Zero, "nothing from outside shows indoors");
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
            // The capture renders into a 320x180 texture; give the camera its own projection back (batch mode leaves it stale).
            Camera.main.ResetProjectionMatrix();
            Camera.main.ResetWorldToCameraMatrix();
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

        // ---------- 2026-10-08: collision by what things are, and Ogrin's lit window ----------

        static DressingCollision Dressing => Object.FindAnyObjectByType<DressingCollision>();

        [UnityTest]
        public IEnumerator TheWater_IsSolid_ForTheKeeperAndTheVillagersWalkingGrid()
        {
            yield return Daytime();
            Outside();
            yield return new WaitForFixedUpdate();
            int obstacles = LayerMask.GetMask("Obstacles");
            foreach (Vector2 tile in new[] { new Vector2(12f, 25f), new Vector2(8f, 24.5f), new Vector2(13f, 27f) })
                Assert.That(Physics2D.OverlapPoint(k_Origin + tile, obstacles), Is.Not.Null, $"the pond at {tile} is solid");
            // The keeper, moving north from the bank, is stopped by the water.
            KeeperAtVillage(new Vector2(12f, 22.6f));
            yield return new WaitForFixedUpdate();
            var hits = new RaycastHit2D[4];
            int n = Keeper.Cast(Vector2.up, hits, 3f);
            Assert.That(Enumerable.Range(0, n).Any(i => hits[i].collider.name == "Pond"), Is.True, "walking north runs into the pond");
            var grid = Object.FindObjectsByType<Hearthdelve.Shared.Navigation.NavGrid>(FindObjectsSortMode.None).Single(g => g.gameObject.scene == Dressing.gameObject.scene);
            if (!grid.IsBaked) grid.Bake();
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(k_Origin + new Vector2(12f, 25f))), Is.False, "villagers path round it too");
        }

        [UnityTest]
        public IEnumerator EveryFence_Sign_AndTheTavernBoard_IsSolidAtItsOwnBase()
        {
            yield return Daytime();
            Outside();
            yield return new WaitForFixedUpdate();
            var drawings = Dressing.Solids.Select(s => s.sprite).ToHashSet();
            var props = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => drawings.Contains(r.sprite)).ToList();
            Assert.That(props.Count(r => r.sprite.name.Contains("FenceRun")), Is.GreaterThan(10), "the fences");
            Assert.That(props.Any(r => r.sprite.name.Contains("TankardBoard")), Is.True, "Tally Ho!'s board");
            Assert.That(props.Any(r => r.sprite.name.Contains("PostSign")), Is.True, "the sign posts");
            foreach (SpriteRenderer prop in props)
            {
                Collider2D footprint = prop.GetComponentInChildren<Collider2D>();
                Assert.That(footprint, Is.Not.Null, $"{prop.name} is solid");
                Assert.That(footprint.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Obstacles")));
                // The footprint sits at the bottom of the art: inside the drawing, at its foot (the Y-sort rule).
                Assert.That(footprint.bounds.min.y, Is.GreaterThanOrEqualTo(prop.bounds.min.y - 0.01f), prop.name);
                Assert.That(footprint.bounds.max.y, Is.LessThanOrEqualTo(prop.bounds.min.y + 0.6f), $"{prop.name}: only the base, not the whole drawing");
            }
        }

        [UnityTest]
        public IEnumerator AFencePutAnywhere_IsSolid_WithoutAnyWallBeingPlaced()
        {
            yield return Daytime();
            Outside();
            Sprite fence = Dressing.Solids.First(s => s.sprite.name.Contains("FenceRun")).sprite;
            var go = new GameObject("A New Fence");
            SceneManager.MoveGameObjectToScene(go, Dressing.gameObject.scene);
            go.transform.position = k_Origin + new Vector2(30f, 5f);
            go.AddComponent<SpriteRenderer>().sprite = fence;
            Dressing.Apply();
            yield return new WaitForFixedUpdate();
            Collider2D footprint = go.GetComponentInChildren<Collider2D>();
            Assert.That(footprint, Is.Not.Null, "solid where it was put");
            Assert.That(Physics2D.OverlapPoint(footprint.bounds.center, LayerMask.GetMask("Obstacles")), Is.EqualTo(footprint));
            go.transform.position += new Vector3(4f, 2f, 0f);
            yield return new WaitForFixedUpdate();
            Assert.That(Physics2D.OverlapPoint(go.GetComponentInChildren<Collider2D>().bounds.center, LayerMask.GetMask("Obstacles")), Is.Not.Null, "and still solid once moved");
            Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator OgrinsWindow_IsLitWhileHesInBed_AndDarkWhileHesOut()
        {
            yield return Daytime();
            Outside();
            bool sawLit = false, sawDark = false;
            for (int day = 0; day < 4 && !(sawLit && sawDark); day++)
            {
                foreach (int minute in new[] { 9 * 60, 12 * 60 + 30, 15 * 60 + 30, 17 * 60 })
                {
                    yield return At(minute);
                    yield return WaitUntil(() => Villager.All.Where(v => v.CharacterId == CharacterIds.Ogrin && v.Shown).All(v => !v.Walking), 30f, "Ogrin settled");
                    yield return Frames(2);
                    bool indoors = Villager.All.Any(v => v.CharacterId == CharacterIds.Ogrin && v.Shown && v.Indoors);
                    bool lit = LitWindow.IsLitAt("ogrin.window");
                    Assert.That(lit, Is.EqualTo(indoors), $"day {Flow.State.Day} at {minute / 60}:{minute % 60:00}: the window lit exactly while he's behind it");
                    sawLit |= lit;
                    sawDark |= !lit;
                }
                if (!(sawLit && sawDark)) yield return NextDay();
                Outside();
            }
            Assert.That(sawLit, Is.True, "lit when he's in bed");
            Assert.That(sawDark, Is.True, "dark when he's out");
        }

        [UnityTest, Explicit]
        public IEnumerator CaptureOgrinsWindow()
        {
            yield return Daytime();
            Outside();
            KeeperAtVillage(new Vector2(50f, 9.5f));
            yield return At(17 * 60);
            yield return WaitUntil(() => Villager.All.Any(v => v.CharacterId == CharacterIds.Ogrin && v.Shown && v.Indoors), 60f, "Ogrin home");
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture($"BatchLogs/window_{(LitWindow.IsLitAt("ogrin.window") ? "lit" : "dark")}_evening.png");
            yield return At(12 * 60 + 30);
            yield return new WaitForSecondsRealtime(6f);
            TavernEveningCaptures.Capture($"BatchLogs/window_{(LitWindow.IsLitAt("ogrin.window") ? "lit" : "dark")}_midday.png");
        }

        // ---------- 4i-A: candidate stills for the main menu's backdrop (run by hand: BatchLogs/menu/*.png) ----------

        [UnityTest, Explicit]
        public IEnumerator CaptureMenuBackdrops()
        {
            const string out_ = "BatchLogs/menu";
            System.IO.Directory.CreateDirectory(out_);
            yield return Daytime();
            Flow.MarkHintSeen(CommunityRules.GimpIntro);
            Outside();
            // The keeper out of the picture: the village as it is.
            foreach (SpriteRenderer r in Keeper.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
            var shots = new (string name, Vector2 tile, int minute)[]
            {
                ("tallyho_left", new Vector2(51f, 31f), 15 * 60 + 30),
                ("tallyho_right", new Vector2(22f, 32f), 15 * 60 + 30),
                ("tallyho_right_low", new Vector2(23f, 29f), 15 * 60 + 30),
                ("tallyho_left_low", new Vector2(50f, 28f), 15 * 60 + 30),
            };
            foreach (var (name, tile, minute) in shots)
            {
                KeeperAtVillage(tile);
                yield return At(minute);
                yield return new WaitForSecondsRealtime(2.5f);
                foreach (Canvas c in Object.FindObjectsByType<Canvas>()) c.enabled = false;
                yield return null;
                TavernEveningCaptures.Capture($"{out_}/{name}.png");
                foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) c.enabled = true;
            }
        }

        // ---------- the field guide's shots (run by hand: BatchLogs/guide/*.png) ----------

        [UnityTest, Explicit]
        public IEnumerator CaptureTheFieldGuide()
        {
            const string out_ = "BatchLogs/guide";
            System.IO.Directory.CreateDirectory(out_);
            yield return Daytime();
            Flow.MarkHintSeen(CommunityRules.GimpIntro);
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture($"{out_}/wake_room.png");

            Inside();
            KeeperAtWorld(new Vector2(14f, 6f));
            yield return new WaitForSecondsRealtime(1.5f);
            TavernEveningCaptures.Capture($"{out_}/tavern_day.png");

            Outside();
            KeeperAtVillage(new Vector2(36f, 15.5f));
            yield return At(9 * 60);
            yield return new WaitForSecondsRealtime(1.5f);
            TavernEveningCaptures.Capture($"{out_}/village_square.png");

            KeeperAtVillage(new Vector2(36.9f, 10.9f));
            yield return Frames(3);
            Here(CharacterIds.Musashi).Talk.Use();
            yield return new WaitForSecondsRealtime(2f);
            TavernEveningCaptures.Capture($"{out_}/talk_musashi.png");
            DialogueManager.StopConversation();
            yield return Frames(3);

            GardenBed bed = GardenBed.Find(Hearthdelve.Shared.Garden.GardenConfig.Bed1);
            KeeperAtWorld((Vector2)bed.transform.position + new Vector2(0f, -2.2f));
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture($"{out_}/garden.png");
            bed.Interactable.Use();
            yield return new WaitForSecondsRealtime(0.8f);
            TavernEveningCaptures.Capture($"{out_}/garden_plant.png");
            Object.FindAnyObjectByType<Hearthdelve.UI.Tavern.GardenPanel>()?.Close();
            yield return Frames(3);

            KeeperAtVillage(new Vector2(24f, 19f));
            yield return At(15 * 60);
            yield return WaitUntil(() => !Here(CharacterIds.Bart).Walking, 40f, "Bart on the green");
            yield return new WaitForSecondsRealtime(2f);
            TavernEveningCaptures.Capture($"{out_}/village_green.png");

            SurfacePause.Release(m_Hold);
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            Director.FillStoreroom();
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture($"{out_}/prep.png");

            Flow.SkipService();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return Revealed();
            yield return new WaitForSecondsRealtime(1.5f);
            TavernEveningCaptures.Capture($"{out_}/delve_start.png");
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
