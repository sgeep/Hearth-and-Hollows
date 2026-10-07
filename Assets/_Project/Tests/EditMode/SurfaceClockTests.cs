using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4h Checkpoint A, the surface day's rules: the one clock (it runs gently, stops at five, never skips under a stalled frame),
    /// its bands and face, market hours, the content each part of the day loads, the village camera's bounds, and the holds
    /// that keep the clock still. Pure.
    /// </summary>
    public class SurfaceClockTests
    {
        readonly List<Object> m_Made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
            SurfacePause.Clear();
        }

        static SurfaceClockSettings Settings => SurfaceClockSettings.Default;

        // ---------- The clock ----------

        [Test]
        public void TheDefault_RunsEightToFive_InAboutFourMinutes()
        {
            SurfaceClockSettings s = Settings;
            Assert.That((s.dayStartMinute, s.cutoffMinute, s.afternoonStartMinute), Is.EqualTo((480, 1020, 720)));
            float realMinutes = (s.cutoffMinute - s.dayStartMinute) * s.realSecondsPerGameMinute / 60f;
            Assert.That(realMinutes, Is.InRange(3f, 5f), "a short day while there's little to fill it (35 minutes, then 12, now about 4)");
            Assert.That((s.marketOpenMinute, s.marketCloseMinute), Is.EqualTo((480, 1020)));
            Assert.That(s.pauseIndoors, Is.False, "the clock runs indoors for the first playtest");
        }

        [Test]
        public void TheClock_AdvancesByRealTime_InGameMinutes()
        {
            SurfaceClockSettings s = Settings;
            s.maxRealSecondsPerFrame = 1000f;
            var clock = new SurfaceClock();
            clock.Reset(s);
            Assert.That(clock.WholeMinute, Is.EqualTo(480));
            Assert.That(clock.Advance(s.realSecondsPerGameMinute * 30f, s));
            Assert.That(clock.WholeMinute, Is.EqualTo(510).Within(1));
        }

        [Test]
        public void ARealDelta_IsCapped_SoAStalledFrameOrSuspendedTabNeverSkipsTheDay()
        {
            SurfaceClockSettings s = Settings;
            var clock = new SurfaceClock();
            clock.Reset(s);
            clock.Advance(300f, s);
            Assert.That(clock.Minute - 480, Is.LessThanOrEqualTo(s.maxRealSecondsPerFrame / s.realSecondsPerGameMinute + 1e-6));
            Assert.That(clock.WholeMinute, Is.EqualTo(480), "five minutes of a hidden tab is a fraction of a game minute");
        }

        [Test]
        public void TheClock_StopsAtTheCutoff_AndGoesNoFurther()
        {
            SurfaceClockSettings s = Settings;
            s.maxRealSecondsPerFrame = 1000f;
            var clock = new SurfaceClock();
            clock.Set(1019, s);
            Assert.That(clock.AtCutoff(s), Is.False);
            for (int i = 0; i < 100; i++) clock.Advance(60f, s);
            Assert.That(clock.WholeMinute, Is.EqualTo(1020));
            Assert.That(clock.AtCutoff(s));
            Assert.That(clock.Advance(60f, s), Is.False, "nothing moves past five");
            Assert.That(clock.WholeMinute, Is.EqualTo(1020));
        }

        [Test]
        public void SettingTheClock_IsHeldToTheDay()
        {
            var clock = new SurfaceClock();
            clock.Set(2000, Settings);
            Assert.That(clock.WholeMinute, Is.EqualTo(1020));
            clock.Set(100, Settings);
            Assert.That(clock.WholeMinute, Is.EqualTo(480));
        }

        [Test]
        public void NoTimePasses_ForNothing()
        {
            var clock = new SurfaceClock();
            clock.Reset(Settings);
            Assert.That(clock.Advance(0f, Settings), Is.False);
            Assert.That(clock.Advance(-5f, Settings), Is.False);
            Assert.That(clock.WholeMinute, Is.EqualTo(480));
        }

        [TestCase(480, SurfaceBand.Morning)]
        [TestCase(719, SurfaceBand.Morning)]
        [TestCase(720, SurfaceBand.Afternoon)]
        [TestCase(1019, SurfaceBand.Afternoon)]
        [TestCase(1020, SurfaceBand.Evening)]
        public void Bands_FollowTheMinute(int minute, SurfaceBand band) => Assert.That(SurfaceClock.BandAt(minute, Settings), Is.EqualTo(band));

        [Test]
        public void TheFace_MovesInTenMinuteSteps()
        {
            var clock = new SurfaceClock();
            clock.Set(487, Settings);
            Assert.That(clock.Shown(Settings), Is.EqualTo(480));
            clock.Set(499, Settings);
            Assert.That(clock.Shown(Settings), Is.EqualTo(490));
        }

        [TestCase(480, 8, 0, false)]
        [TestCase(725, 12, 5, true)]
        [TestCase(780, 1, 0, true)]
        [TestCase(1020, 5, 0, true)]
        [TestCase(0, 12, 0, false)]
        public void TheFace_ReadsTwelveHours(int minute, int hour, int minutes, bool pm) =>
            Assert.That(SurfaceClock.Face(minute), Is.EqualTo((hour, minutes, pm)));

        // ---------- Five o'clock and the market ----------

        [TestCase(479, false)]
        [TestCase(480, true)]
        [TestCase(1019, true)]
        [TestCase(1020, false)]
        public void TheMarket_TradesFromMorningUntilFive(int minute, bool open) => Assert.That(MarketHours.IsOpen(minute, Settings), Is.EqualTo(open));

        [Test]
        public void AfterFive_TheMarketRefusesToSell_AndNothingElseChanges()
        {
            var onion = ScriptableObject.CreateInstance<IngredientDefinition>();
            m_Made.Add(onion);
            onion.id = "onion";
            var market = ScriptableObject.CreateInstance<SupplySource>();
            m_Made.Add(market);
            var offer = new SupplyOffer { ingredient = onion, price = 2, bundle = 1 };
            market.offers.Add(offer);
            var state = new GameState();
            state.AddGold(10);
            state.Surface.Set(900, Settings);
            Assert.That(DayRules.Buy(state, market, offer, Settings), "open in the afternoon");
            state.Surface.Set(1020, Settings);
            Assert.That(DayRules.Buy(state, market, offer, Settings), Is.False, "packed up at five");
            Assert.That((state.Gold, state.Storeroom.TotalCount), Is.EqualTo((8, 1)));
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Daytime), "five o'clock never moves the day on");
        }

        [Test]
        public void TheCutoff_IsNeverAPhaseChange()
        {
            var state = new GameState();
            SurfaceClockSettings s = Settings;
            s.maxRealSecondsPerFrame = 1000f;
            for (int i = 0; i < 200; i++) state.Surface.Advance(60f, s);
            Assert.That(state.Surface.AtCutoff(s));
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Daytime));
        }

        // ---------- What each part of the day loads ----------

        [Test]
        public void TheFreeDaytime_LoadsTheTavernAndTheVillage_TheTavernFirst() =>
            Assert.That(GameScenes.ContentFor(DayPhase.Daytime, OpeningStage.Complete), Is.EqualTo(new[] { GameScenes.Tavern, GameScenes.Kariaston }));

        [Test]
        public void ArrivalDay_IsTheTavernAlone() =>
            Assert.That(GameScenes.ContentFor(DayPhase.Daytime, OpeningStage.Arrival), Is.EqualTo(new[] { GameScenes.Tavern }));

        [TestCase(DayPhase.Evening, GameScenes.Tavern)]
        [TestCase(DayPhase.Night, GameScenes.Tavern)]
        [TestCase(DayPhase.Delve, GameScenes.Dungeon)]
        public void TheEvening_TheDelveAndTheNight_LoadOneScene(DayPhase phase, string scene) =>
            Assert.That(GameScenes.ContentFor(phase, OpeningStage.Complete), Is.EqualTo(new[] { scene }));

        [Test]
        public void TheOpeningsLaterStages_AreFreeDaytimes() =>
            Assert.That(GameScenes.ContentFor(DayPhase.Daytime, OpeningStage.FirstEvening), Does.Contain(GameScenes.Kariaston));

        // ---------- The village camera ----------

        [Test]
        public void TheVillageView_StaysInsideItsBounds()
        {
            var bounds = new Rect(200f, 0f, 72f, 48f);
            Assert.That(ViewBounds.Clamp(bounds, new Vector2(200f, 0f), 20f, 11.25f), Is.EqualTo(new Vector2(220f, 11.25f)));
            Assert.That(ViewBounds.Clamp(bounds, new Vector2(236f, 24f), 20f, 11.25f), Is.EqualTo(new Vector2(236f, 24f)));
            Assert.That(ViewBounds.Clamp(bounds, new Vector2(400f, 99f), 20f, 11.25f), Is.EqualTo(new Vector2(252f, 36.75f)));
        }

        // ---------- Holds ----------

        [Test]
        public void Holds_Nest_AndReleaseByKey()
        {
            object a = new(), b = new();
            Assert.That(SurfacePause.IsHeld, Is.False);
            SurfacePause.Hold(a);
            SurfacePause.Hold(b);
            SurfacePause.Hold(a);
            SurfacePause.Release(a);
            Assert.That(SurfacePause.IsHeld, "b still holds it");
            SurfacePause.Release(b);
            Assert.That(SurfacePause.IsHeld, Is.False);
            SurfacePause.Set(a, true);
            Assert.That(SurfacePause.IsHeld);
            SurfacePause.Set(a, false);
            Assert.That(SurfacePause.IsHeld, Is.False);
        }

        // ---------- Boundaries ----------

        [Test]
        public void TheVillage_NeverReferencesTheDungeon()
        {
            string json = System.IO.File.ReadAllText("Assets/_Project/Scripts/Village/Hearthdelve.Village.asmdef");
            Assert.That(json.Contains("\"Hearthdelve.Dungeon\""), Is.False, "Village and Tavern never depend on Dungeon (H15)");
            Assert.That(System.IO.File.ReadAllText("Assets/_Project/Scripts/Tavern/Hearthdelve.Tavern.asmdef").Contains("\"Hearthdelve.Dungeon\""), Is.False);
        }

        [Test]
        public void OnlyTheClock_ReadsTimeForTheHour()
        {
            // One source of truth (CLAUDE.md, 4h): nothing else in the surface's code reads Time.time to decide the hour.
            foreach (string file in System.IO.Directory.GetFiles("Assets/_Project/Scripts/Village", "*.cs").Concat(new[]
                     {
                         "Assets/_Project/Scripts/Tavern/Scene/SurfaceArea.cs", "Assets/_Project/Scripts/Tavern/Scene/SurfaceDoor.cs",
                         "Assets/_Project/Scripts/Tavern/Scene/FiveOClock.cs", "Assets/_Project/Scripts/UI/Tavern/SurfaceClockView.cs",
                     }))
            {
                var code = System.IO.File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//"));
                Assert.That(code.Any(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"Time\.(time|unscaledTime|realtimeSinceStartup)\b")), Is.False, file);
            }
        }
    }
}
