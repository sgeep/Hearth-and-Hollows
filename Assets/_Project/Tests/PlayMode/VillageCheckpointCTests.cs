using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4h Checkpoint C, "The village has people", in the game: Kariaston's people where their schedules say, one of each, talkable,
    /// moving at the day's broad beats (walking when the keeper can see them), Maximo crossing into Tally Ho! for lunch, Kaloren's
    /// herbs on a herb day (and not again on a reload), Ogrin's good and bad days, five o'clock, and a save and Continue that
    /// rebuilds the village from the clock alone.
    /// </summary>
    public class VillageCheckpointCTests : BootFixture
    {
        static VillagePresence Presence => VillagePresence.Instance;
        static readonly Vector2 k_Origin = VillageSpots.Origin;   // Kariaston's origin (KariastonBuilder.Origin)

        /// <summary>The schedules' named places (VillageContent's anchors).</summary>
        static class Spots
        {
            public const string MemorialSquare = "square.memorial";
            public const string MarketCart = "market.cart";
            public const string BartWagon = "bart.wagon";
            public const string GrimYard = "grim.yard";
            public const string MarketFront = "market.front";
            public const string SquareBench = "square.bench";
            public const string TavernTable = "tavern.table";
            public const string TallyWatch = "tally.watch";
            public const string KalorenTower = "kaloren.tower";
            public const string CottageDoor = "cottage.door";
            public const string OgrinYard = "ogrin.yard";
            public const string OgrinWindow = "ogrin.window";
            public const string MarketSide = "market.side";
            public const string TavernKitchen = "tavern.kitchen";
            public const string TavernBar = "tavern.bar";
        }

        static readonly string[] k_People = { CharacterIds.Maximo, CharacterIds.Kaloren, CharacterIds.Grim, CharacterIds.Ogrin, CharacterIds.Bart, CharacterIds.Musashi };

        /// <summary>
        /// The first free day; the clock held (the tests move it), the keeper out in Kariaston. Checkpoint D's night in the keeper's
        /// room is D's to test: here it has already happened. <paramref name="seed"/>: the world seed must satisfy it.
        /// </summary>
        IEnumerator Daytime(bool outside = true, Func<int, VillageLifeSettings, bool> seed = null)
        {
            yield return StartDaytime(seed, gimpSeen: true, hold: true);
            if (outside) Outside();
            yield return Frames(2);
        }

        IEnumerator NextVillageDay(bool outside)
        {
            yield return NextDay(hold: true);
            if (outside) Outside();
            yield return Frames(2);
        }

        static void Outside()
        {
            if (SurfaceArea.Current == null || SurfaceArea.Current.Id != SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
        }

        static void Inside()
        {
            if (SurfaceArea.Current != null && SurfaceArea.Current.Id == SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
        }

        /// <summary>The keeper to a spot in the village (village tiles).</summary>
        static void KeeperAt(Vector2 villageTile)
        {
            Vector2 at = k_Origin + villageTile;
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

        static IEnumerable<Villager> ShownCopies(string id) => Villager.All.Where(v => v.CharacterId == id && v.Shown);
        static Villager Here(string id) => ShownCopies(id).Single();

        static IEnumerator Arrived(string id, float seconds = 40f)
        {
            yield return WaitUntil(() => ShownCopies(id).Count() == 1 && !Here(id).Walking, seconds, $"{id} there");
        }

        /// <summary>Until everyone has finished walking to where the clock says (one copy each, standing).</summary>
        static IEnumerator Settled(float seconds = 45f)
        {
            yield return WaitUntil(() => k_People.All(id => ShownCopies(id).Count() == 1 && !Here(id).Walking), seconds, "everyone settled");
        }

        static void AssertOneEach()
        {
            foreach (string id in k_People) Assert.That(ShownCopies(id).Count(), Is.LessThanOrEqualTo(1), $"one {id} at most");
        }

        static Vector2 AnchorAt(string anchor) => ScheduleAnchor.Find(anchor).Spot;

        // ---------- in the village, one of each, talkable ----------

        [UnityTest]
        public IEnumerator EveryoneLivesInKariaston_OneOfEach_AndTalkingOpensTheirFirstMeeting()
        {
            yield return Daytime();
            KeeperAt(VillageSpots.Square);
            yield return At(9 * 60);
            foreach (string id in k_People)
            {
                Assert.That(ShownCopies(id).Count(), Is.EqualTo(1), $"{id} is about, once");
                Assert.That(Here(id).gameObject.scene.name, Is.EqualTo("Kariaston"), $"{id} in the morning is in the village");
            }
            Assert.That(Here(CharacterIds.Maximo).At.Id, Is.EqualTo(Spots.MemorialSquare), "Maximo before the memorial in the morning");
            Assert.That(Here(CharacterIds.Musashi).At.Id, Is.EqualTo(Spots.MarketCart), "Musashi still at his cart");
            Assert.That(Here(CharacterIds.Bart).At.Id, Is.EqualTo(Spots.BartWagon));
            Assert.That(Here(CharacterIds.Grim).At.Id, Is.EqualTo(Spots.GrimYard));

            foreach (string id in new[] { CharacterIds.Maximo, CharacterIds.Kaloren, CharacterIds.Grim, CharacterIds.Ogrin, CharacterIds.Bart })
            {
                Villager v = Here(id);
                Assert.That(v.Talk.IsAvailable, $"{id} can be talked to");
                v.Talk.Use();
                yield return Frames(3);
                Assert.That(DialogueManager.IsConversationActive, $"{id}'s conversation");
                Assert.That(DialogueManager.lastConversationStarted, Is.EqualTo($"{char.ToUpperInvariant(id[0])}{id.Substring(1)}/Hub"));
                DialogueEntry line = DialogueManager.currentConversationState.subtitle.dialogueEntry;
                Assert.That(line.conditionsString, Does.Contain($"hh_{id}_met"), $"{id}: the first meeting");
                Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Talking), "talking holds the clock");
                DialogueManager.StopConversation();
                yield return Frames(3);
            }
            // The second time, an everyday greeting.
            Here(CharacterIds.Bart).Talk.Use();
            yield return Frames(3);
            Assert.That(DialogueManager.currentConversationState.subtitle.dialogueEntry.conditionsString ?? string.Empty, Does.Not.Contain("_met"));
            DialogueManager.StopConversation();
        }

        // ---------- the day's broad beats ----------

        [UnityTest]
        public IEnumerator AtABeat_TheySeenWalkToTheirNextPlace_AndTheUnseenAreSimplyThere()
        {
            yield return Daytime();
            // Between Bart's wagon and the market: at eleven he walks over (the keeper sees him go).
            KeeperAt(VillageSpots.BetweenWagonAndMarket);
            yield return At(10 * 60 + 50);
            Assert.That(Here(CharacterIds.Bart).At.Id, Is.EqualTo(Spots.BartWagon));
            Vector2 from = Here(CharacterIds.Bart).transform.position;
            yield return At(11 * 60);
            Assert.That(Here(CharacterIds.Bart).Walking, "Bart strolls over");
            Assert.That(Here(CharacterIds.Bart).Talk.IsAvailable, Is.False, "not while walking");
            float seen = 0f;
            yield return WaitUntil(() =>
            {
                AssertOneEach();
                seen = Mathf.Max(seen, Vector2.Distance(Here(CharacterIds.Bart).transform.position, from));
                return !Here(CharacterIds.Bart).Walking;
            }, 40f, "Bart at the market");
            Assert.That(Here(CharacterIds.Bart).At.Id, Is.EqualTo(Spots.MarketFront));
            Assert.That(Vector2.Distance(Here(CharacterIds.Bart).transform.position, AnchorAt(Spots.MarketFront)), Is.LessThan(0.05f));
            Assert.That(Here(CharacterIds.Bart).Talk.IsAvailable, "talkable once he's there");

            // Far across the village from Kaloren's tower: at eleven he's simply at the square's bench.
            KeeperAt(VillageSpots.FarWest);
            yield return At(10 * 60 + 55);
            yield return At(11 * 60 + 10);
            Villager kaloren = Here(CharacterIds.Kaloren);
            Assert.That((kaloren.At.Id, kaloren.Walking), Is.EqualTo((Spots.SquareBench, false)), "no long walk nobody watches");
        }

        [UnityTest]
        public IEnumerator Maximo_GoesInToTallyHo_ForLunch_OneOfHimAtATime_AndOutAgain()
        {
            yield return Daytime();
            // Watching him from the square: he walks to Tally Ho!'s door and in.
            KeeperAt(VillageSpots.Square);
            yield return At(10 * 60 + 50);
            Assert.That(Here(CharacterIds.Maximo).At.Id, Is.EqualTo(Spots.MemorialSquare));
            yield return At(11 * 60);
            Villager outside = Here(CharacterIds.Maximo);
            Assert.That(outside.gameObject.scene.name, Is.EqualTo("Kariaston"));
            Assert.That(outside.Walking, "off to lunch, on foot");
            yield return WaitUntil(() =>
            {
                AssertOneEach();
                return ShownCopies(CharacterIds.Maximo).Any(v => v.gameObject.scene.name == "Tavern");
            }, 45f, "Maximo inside");
            Assert.That(outside.Shown, Is.False, "gone from the village");
            yield return Arrived(CharacterIds.Maximo);
            Villager inside = Here(CharacterIds.Maximo);
            Assert.That((inside.At.Id, inside.Activity), Is.EqualTo((Spots.TavernTable, "lunch")));
            Assert.That(Director.Layout.Seats.Any(s => Vector2.Distance(s.SitPoint, inside.transform.position) < 0.05f), "at a seat of today's furniture");

            // The keeper inside at two: he's out (the keeper didn't see him leave through the village), back by Tally Ho!.
            Inside();
            yield return Frames(2);
            yield return At(14 * 60);
            yield return WaitUntil(() => { AssertOneEach(); return ShownCopies(CharacterIds.Maximo).Any(v => v.gameObject.scene.name == "Kariaston"); }, 45f, "Maximo out");
            yield return Arrived(CharacterIds.Maximo);
            Assert.That(Here(CharacterIds.Maximo).At.Id, Is.EqualTo(Spots.TallyWatch), "watching Tally Ho!, from a distance");
        }

        // ---------- Kaloren's herbs ----------

        [UnityTest]
        public IEnumerator OnAHerbDay_KalorenWalksTheHerbsToTheCottage_AndAReloadDoesntRepeatIt()
        {
            yield return Daytime(seed: (s, settings) => VillageDays.HerbDay(s, 2, settings));
            ScheduleWorld world = VillageLife.World().Value;
            Assert.That(VillageDays.HerbDay(world.Seed, Flow.State.Day, world.Settings), "a herb day (the seed pinned)");
            HerbVisit visit = Object.FindAnyObjectByType<HerbVisit>();

            KeeperAt(VillageSpots.EastRoad);
            yield return At(9 * 60 + 20);
            Assert.That(Here(CharacterIds.Kaloren).At.Id, Is.EqualTo(Spots.KalorenTower));
            yield return At(9 * 60 + 30);
            Assert.That(Here(CharacterIds.Kaloren).Walking, "he sets off with them");
            yield return Fast(Arrived(CharacterIds.Kaloren));
            Villager kaloren = Here(CharacterIds.Kaloren);
            Assert.That((kaloren.At.Id, kaloren.Activity), Is.EqualTo((Spots.CottageDoor, HerbVisit.Activity)));
            Assert.That(visit.Handovers, Is.EqualTo(1), "the herbs change hands");
            Assert.That(kaloren.Emote.Last, Is.Not.Null, "shown over his head");
            Villager to = Villager.Find(CharacterIds.Grim).Shown && Villager.Find(CharacterIds.Grim).At?.Id == Spots.GrimYard
                ? Villager.Find(CharacterIds.Grim) : Villager.Find(CharacterIds.Ogrin);
            yield return WaitUntil(() => to.Emote.Last != null, 5f, "and taken");
            Assert.That(VillageLife.Doing(CharacterIds.Kaloren), Is.EqualTo("herbs"), "what the conversations read");

            // A save and Continue mid-visit: he's there, done, and nothing plays again.
            yield return SaveQuitAndContinue(() => InDaytimeNow, "the daytime");
            SurfacePause.Hold(ClockHold);
            Outside();
            KeeperAt(VillageSpots.EastRoad);
            yield return Frames(3);
            Presence.Refresh();
            yield return Frames(2);
            Assert.That((Here(CharacterIds.Kaloren).At.Id, Here(CharacterIds.Kaloren).Walking), Is.EqualTo((Spots.CottageDoor, false)), "already there");
            Assert.That(Object.FindAnyObjectByType<HerbVisit>().Handovers, Is.EqualTo(0), "no second handover");
        }

        // ---------- Ogrin's days ----------

        [UnityTest, Category("Slow")]
        public IEnumerator Ogrin_IsOutOnGoodDays_AndAtHisWindowOnBadOnes_TalkableEitherWay()
        {
            yield return Daytime(seed: (s, settings) => VillageDays.OgrinWell(s, 2, settings) != VillageDays.OgrinWell(s, 3, settings));
            bool sawGood = false, sawBad = false;
            for (int i = 0; i < 2; i++)
            {
                if (i > 0) yield return NextVillageDay(outside: true);
                KeeperAt(VillageSpots.BeforeTheCottage);
                yield return At(9 * 60);
                ScheduleWorld world = VillageLife.World().Value;
                bool well = VillageDays.OgrinWell(world.Seed, Flow.State.Day, world.Settings);
                Villager ogrin = Here(CharacterIds.Ogrin);
                if (well)
                {
                    sawGood = true;
                    Assert.That((ogrin.At.Id, ogrin.Indoors), Is.EqualTo((Spots.OgrinYard, false)), $"day {Flow.State.Day}: out in the yard");
                    Assert.That(ogrin.Animator.gameObject.activeInHierarchy, "seen");
                }
                else
                {
                    sawBad = true;
                    Assert.That((ogrin.At.Id, ogrin.Indoors), Is.EqualTo((Spots.OgrinWindow, true)), $"day {Flow.State.Day}: in bed");
                    Assert.That(ogrin.Animator.gameObject.activeInHierarchy, Is.False, "unseen, behind the window");
                    Assert.That(Vector2.Distance(ogrin.Talk.transform.position, AnchorAt(Spots.OgrinWindow)), Is.LessThan(0.05f), "talked to at the window");
                    Assert.That(Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None).Any(l => l.name == "Ogrin's Window Glow"), "a light in his window");
                    Assert.That(VillageLife.Doing(CharacterIds.Grim), Is.EqualTo("chores"), "Grim's morning goes on: his own man");
                }
                Assert.That(ogrin.Talk.IsAvailable, "talkable");
            }
            Assert.That(sawGood && sawBad, "a good day and a bad one");
        }

        // ---------- five o'clock, and a Continue ----------

        [UnityTest]
        public IEnumerator AtFive_EveryoneIsSomewhereFindable_AndNothingIsForced()
        {
            yield return Daytime();
            KeeperAt(VillageSpots.Square);
            yield return At(17 * 60);
            yield return Fast(Settled());
            foreach (string id in k_People)
            {
                Assert.That(ShownCopies(id).Count(), Is.EqualTo(1), $"{id} somewhere at five");
                Assert.That(Here(id).gameObject.scene.name, Is.EqualTo("Kariaston"), $"{id} at home in the village");
            }
            yield return Frames(2);
            foreach (string id in k_People) Assert.That(Here(id).Talk.IsAvailable, $"{id} can still be found and talked to");
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Daytime), "five never starts Prep");
        }

        [UnityTest]
        public IEnumerator ASaveAndContinue_RebuildsTheVillage_FromTheClockAlone()
        {
            yield return Daytime();
            KeeperAt(VillageSpots.Square);
            yield return At(12 * 60 + 10);
            yield return Fast(Settled());
            var before = k_People.ToDictionary(id => id, id => (Here(id).At?.Id, Here(id).Activity));
            Assert.That(before[CharacterIds.Maximo].Item1, Is.EqualTo(Spots.TavernTable), "lunch");
            yield return SaveQuitAndContinue(() => InDaytimeNow, "the daytime");
            SurfacePause.Hold(ClockHold);
            yield return Frames(3);
            Assert.That(Flow.State.Surface.WholeMinute, Is.InRange(12 * 60 + 10, 12 * 60 + 19));
            foreach (string id in k_People)
            {
                Assert.That(ShownCopies(id).Count(), Is.EqualTo(1), $"{id} back, once");
                Assert.That((Here(id).At?.Id, Here(id).Activity), Is.EqualTo(before[id]), $"{id} where they were");
                Assert.That(Here(id).Walking, Is.False, $"{id} simply there");
            }
            Assert.That(Flow.State.Vigor.Current, Is.EqualTo(Flow.VigorSettings.maxVigor), "the day's Vigor intact");
        }

        // ---------- captures (run by hand: BatchLogs/village_*.png) ----------

        [UnityTest, Explicit]
        public IEnumerator CaptureTheVillagersAtGameScale()
        {
            yield return Daytime();
            foreach (var (minute, at, name) in new[]
            {
                (9 * 60, VillageSpots.SquareSouth, "morning_square"), (9 * 60 + 40, VillageSpots.BeforeTheCottage, "morning_cottage"),
                (12 * 60 + 30, VillageSpots.ByTheMarket, "midday_market"), (15 * 60, VillageSpots.TheGreen, "afternoon_green"),
                (17 * 60, VillageSpots.SquareSouth, "evening"),
            })
            {
                KeeperAt(at);
                yield return At(minute);
                yield return Settled();
                yield return new WaitForSecondsRealtime(1.5f);
                TavernEveningCaptures.Capture($"BatchLogs/village_{name}.png");
            }
            KeeperAt(VillageSpots.SquareSouth);
            yield return At(9 * 60);
            yield return Settled();
            Here(CharacterIds.Maximo).Talk.Use();
            yield return new WaitForSecondsRealtime(1.5f);
            TavernEveningCaptures.Capture("BatchLogs/village_talk_maximo.png");
            DialogueManager.StopConversation();
            yield return At(12 * 60 + 30);
            Inside();
            yield return Settled();
            yield return new WaitForSecondsRealtime(1f);
            TavernEveningCaptures.Capture("BatchLogs/village_tavern_lunch.png");
        }

        // ---------- the music (2026-10-07) ----------

        [UnityTest]
        public IEnumerator TheMusic_FollowsTheDay_DecorateModeHasItsOwn_AndTheEveningItsOwn()
        {
            // 2026-10-08 (the owner's call): Tally Ho! is quiet by day, Kariaston has the day's tune, Decorate Mode its own; Prep is
            // quiet and the evening's tune begins with service; every tune waits a moment before it fades in.
            yield return Daytime(outside: false);
            var music = Hearthdelve.Shared.Audio.MusicDirector.Instance;
            Assert.That(music, Is.Not.Null, "the music lives in Boot");
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.None), "Tally Ho! by day: quiet");
            AudioSource day = music.GetComponents<AudioSource>().FirstOrDefault(s => s.clip == music.Config.Clip(Hearthdelve.Shared.Audio.MusicCue.Day));
            Assert.That(day == null || !day.isPlaying, "nothing playing indoors");
            Outside();
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.Day), "out in Kariaston");
            float wanted = Time.realtimeSinceStartup;
            day = music.GetComponents<AudioSource>().Single(s => s.clip == music.Config.Clip(Hearthdelve.Shared.Audio.MusicCue.Day));
            Assert.That(day.loop, "it loops");
            yield return WaitUntil(() => day.isPlaying, 10f, "the day's tune");
            Assert.That(Time.realtimeSinceStartup - wanted, Is.GreaterThanOrEqualTo(music.Config.startDelay - 0.1f), "a quiet moment before it fades in");
            Inside();
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.None), "back indoors: quiet");
            DecorateMode.Instance.Enter();
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.Decorate), "decorating has its tune");
            DecorateMode.Instance.Leave();
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.None), "and Tally Ho! is quiet again");
            SurfacePause.Release(ClockHold);
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.None), "Prep: quiet");
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Service, 10f, "service");
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.Service), "service begins the evening's tune");
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "the results");
            yield return Frames(3);
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.Service), "and carries through the results");
            Director.FinishEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            Assert.That(music.Current, Is.EqualTo(Hearthdelve.Shared.Audio.MusicCue.Cellars));
        }

        /// <summary>2026-10-08: someone is always listening, never two, across a change of scenes; the day's tune is heard.</summary>
        [UnityTest]
        public IEnumerator OneListener_OnEveryFrame_ThroughAChangeOfScenes()
        {
            yield return Daytime(outside: true);
            var music = Hearthdelve.Shared.Audio.MusicDirector.Instance;
            yield return Frames(3);
            AudioSource day = music.GetComponents<AudioSource>().Single(s => s.clip == music.Config.Clip(Hearthdelve.Shared.Audio.MusicCue.Day));
            yield return WaitUntil(() => day.isPlaying, 10f, "the day's tune, once its clip has loaded");
            Assert.That(Object.FindAnyObjectByType<Hearthdelve.Shared.Audio.ListenerKeeper>(), Is.Not.Null, "Boot keeps a fallback ear");
            SurfacePause.Release(ClockHold);
            Flow.StartEvening();
            int frames = 0;
            while (frames < 3 || Flow.IsLoading)
            {
                yield return null;
                int hearing = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled);
                Assert.That(hearing, Is.EqualTo(1), $"listeners hearing on frame {frames} of the load");
                if (++frames > 2000) Assert.Fail("the evening never loaded");
            }
            Assert.That(Object.FindAnyObjectByType<Hearthdelve.Shared.Audio.ListenerKeeper>().Hearing, Is.False, "the scene's own camera hears once it's loaded");
        }

        // ---------- nobody traps the keeper ----------

        [UnityTest]
        public IEnumerator SomeoneArrivingWhereTheKeeperStands_StaysWalkThrough_UntilTheKeeperMovesOff()
        {
            yield return Daytime();
            Vector2 market = AnchorAt(Spots.MarketSide) - k_Origin;
            KeeperAt(market + new Vector2(-6f, -6f));
            yield return At(10 * 60 + 50);
            yield return At(11 * 60);
            KeeperAt(market);
            yield return Fast(Arrived(CharacterIds.Grim));
            Villager grim = Here(CharacterIds.Grim);
            Collider2D feet = grim.transform.Find("Feet").GetComponent<Collider2D>();
            yield return Frames(3);
            Assert.That(feet.enabled, Is.False, "walk-through while the keeper stands in his spot");
            KeeperAt(market + new Vector2(0f, -3f));
            yield return Frames(3);
            Assert.That(feet.enabled, "solid again once the keeper steps away");
            // Boog and Orik keep their tavern posts (their staff agents) and their schedules say so.
            Assert.That(VillageLife.Now(CharacterIds.Boog).anchor, Is.EqualTo(Spots.TavernKitchen));
            Assert.That(VillageLife.Now(CharacterIds.Orik).anchor, Is.EqualTo(Spots.TavernBar));
            Assert.That(Object.FindObjectsByType<StaffAgent>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(2), "the staff are in Tally Ho!");
        }
    }
}
