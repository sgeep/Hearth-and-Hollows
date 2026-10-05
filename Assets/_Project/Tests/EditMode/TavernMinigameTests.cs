using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Tavern.Minigames;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class GrillMinigameTests
    {
        const float Dt = 1f / 60f;
        static readonly GrillSettings S = GrillSettings.Default;

        static void RunUntilMeter(GrillMinigame g, float meter)
        {
            while (!g.IsComplete && (g.IsPausing || g.Meter + S.cookRate * Dt < meter)) g.Tick(Dt, default);
        }

        static void Flip(GrillMinigame g) => g.Tick(Dt, new MinigameInput { ActionPressed = true });

        [Test]
        public void FlippingBothSidesInBand_ScoresOne()
        {
            var g = new GrillMinigame(S);
            g.Begin();
            RunUntilMeter(g, S.BandCenter); Flip(g);
            RunUntilMeter(g, S.BandCenter); Flip(g);
            Assert.That(g.IsComplete);
            Assert.That(g.Evaluate(), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void EarlyFlip_IsUndercooked()
        {
            Assert.That(GrillMinigame.ScoreFlip(S.bandMin - 0.2f, S), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(GrillMinigame.ScoreFlip(0f, S), Is.EqualTo(0f));
        }

        [Test]
        public void LateFlip_IsOvercooked_AndBurningScoresZero()
        {
            float midway = (S.bandMax + GrillMinigame.BurnAt) * 0.5f;
            Assert.That(GrillMinigame.ScoreFlip(midway, S), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(GrillMinigame.ScoreFlip(GrillMinigame.BurnAt, S), Is.EqualTo(0f));
        }

        [Test]
        public void NeverFlipping_BurnsBothSides_AndCompletes()
        {
            var g = new GrillMinigame(S);
            g.Begin();
            for (int i = 0; i < 60 * 30 && !g.IsComplete; i++) g.Tick(Dt, default);
            Assert.That(g.IsComplete);
            Assert.That(g.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void DefaultTuning_ExpertPlay_TakesFiveToTenSeconds()
        {
            var g = new GrillMinigame(S);
            float score = MinigameRunner.RunToCompletion(g, new GrillAutoPlayer(g, 1f, new SeededRandom(1)));
            Assert.That(score, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(g.Elapsed, Is.InRange(5f, 10f));
        }
    }

    public class TapMinigameTests
    {
        const float Dt = 1f / 60f;
        static readonly TapSettings S = TapSettings.Default;

        /// <summary>Tilts the glass to <paramref name="tilt"/> (it stays there), then pours to <paramref name="total"/> and lets go.</summary>
        static TapMinigame PourTo(float total, float tilt)
        {
            var t = new TapMinigame(S);
            t.Begin();
            for (int i = 0; i < 120 && Mathf.Abs(t.Tilt - tilt) > 0.005f; i++)
                t.Tick(Dt, new MinigameInput { Aim = new Vector2(0f, Mathf.Clamp((tilt - t.Tilt) / (S.tiltSpeed * Dt), -1f, 1f)) });
            while (t.Total + S.pourRate * Dt < total && !t.IsComplete) t.Tick(Dt, new MinigameInput { ActionHeld = true });
            t.Tick(Dt, default); // release
            return t;
        }

        static float IdealAim => TapMinigame.IdealTilt(S);

        /// <summary>
        /// 4e playtest: on a keyboard (W/S are all or nothing) a short tap tilts the glass and it stays tilted, so a clean pour
        /// is reachable; before, the tilt sprang back to level and the head always overflowed the band.
        /// </summary>
        [Test]
        public void AKeyboard_TapsTheTilt_AndItHolds_ForACleanPour()
        {
            var t = new TapMinigame(S);
            t.Begin();
            // S held for a fifth of a second, then let go.
            for (int i = 0; i < 12; i++) t.Tick(Dt, new MinigameInput { Aim = new Vector2(0f, -1f) });
            float tilted = t.Tilt;
            for (int i = 0; i < 30; i++) t.Tick(Dt, default);
            Assert.That(t.Tilt, Is.EqualTo(tilted), "the glass stays tilted when the key is let go");
            while (t.Total + S.pourRate * Dt < S.fillLine && !t.IsComplete) t.Tick(Dt, new MinigameInput { ActionHeld = true });
            t.Tick(Dt, default);
            Assert.That(t.FoamFraction, Is.InRange(S.foamBandMin, S.foamBandMax), "the head in the band");
            Assert.That(t.Evaluate(), Is.GreaterThanOrEqualTo(0.85f), "a clean pour");
        }

        [Test]
        public void ALevelGlass_UntouchedTilt_StillPoursAPassableDrink()
        {
            var t = PourTo(S.fillLine, 0.5f);
            Assert.That(t.Evaluate(), Is.InRange(0.6f, 0.85f), "not clean, but not a failure");
        }

        [Test]
        public void PouringToTheLine_WithIdealTilt_ScoresNearlyOne()
        {
            var t = PourTo(S.fillLine, IdealAim);
            Assert.That(t.IsComplete);
            Assert.That(t.Evaluate(), Is.GreaterThan(0.97f));
        }

        [Test]
        public void Overflowing_ScoresZero()
        {
            var t = new TapMinigame(S);
            t.Begin();
            for (int i = 0; i < 60 * 20 && !t.IsComplete; i++) t.Tick(Dt, new MinigameInput { ActionHeld = true });
            Assert.That(t.Overflowed);
            Assert.That(t.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void UprightGlass_MakesTooMuchFoam()
        {
            var ideal = PourTo(S.fillLine, IdealAim);
            var upright = PourTo(S.fillLine, 1f);
            Assert.That(upright.FoamFraction, Is.GreaterThan(S.foamBandMax));
            Assert.That(upright.Evaluate(), Is.LessThan(ideal.Evaluate()));
        }

        [Test]
        public void StoppingShortOfTheLine_LowersScore()
        {
            Assert.That(PourTo(S.fillLine - 0.15f, IdealAim).Evaluate(), Is.LessThan(0.5f));
        }

        [Test]
        public void NotPouring_TimesOutWithZero()
        {
            var t = new TapMinigame(S);
            t.Begin();
            for (int i = 0; i < 60 * 15 && !t.IsComplete; i++) t.Tick(Dt, default);
            Assert.That(t.IsComplete);
            Assert.That(t.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void DefaultTuning_ExpertPlay_TakesFiveToTenSeconds()
        {
            var t = new TapMinigame(S);
            float score = MinigameRunner.RunToCompletion(t, new TapAutoPlayer(t, 1f, new SeededRandom(1)));
            Assert.That(score, Is.GreaterThan(0.95f));
            Assert.That(t.Elapsed, Is.InRange(5f, 10f));
        }
    }

    /// <summary>Serving, top-down (4c): time and spill, scored against par for the shortest walkable path.</summary>
    public class ServingMinigameTests
    {
        const float Dt = 1f / 60f;
        static readonly ServingSettings S = ServingSettings.Default;

        /// <summary>Walks <paramref name="walked"/> tiles at the carry speed, then serves <paramref name="shortest"/> tiles from the pass.</summary>
        static float Carry(ServingMinigame m, float walked, float shortest, float speedFactor = 1f)
        {
            m.Begin();
            float seconds = walked / (S.carrySpeed * speedFactor);
            for (float t = 0f; t < seconds; t += Dt) m.Tick(Dt, default);
            m.Deliver(shortest);
            return m.Evaluate();
        }

        [Test]
        public void TheShortestWay_ScoresOne()
        {
            Assert.That(Carry(new ServingMinigame(S), 12f, 12f), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Wandering_ScoresBelowTheShortestWay()
        {
            float direct = Carry(new ServingMinigame(S), 12f, 12f);
            float roundabout = Carry(new ServingMinigame(S), 30f, 12f);
            Assert.That(roundabout, Is.LessThan(direct));
            Assert.That(roundabout, Is.LessThan(0.5f), "two and a half times the walk");
        }

        [Test]
        public void ParComes_FromTheShortestPath_ToWhereItWasServed()
        {
            var m = new ServingMinigame(S);
            Carry(m, 8f, 8f);
            Assert.That(m.ParTime, Is.EqualTo(8f / S.carrySpeed * S.parFactor + S.parSlack).Within(1e-4f));
            Assert.That(m.Deliver(8f), Is.False, "only once");
        }

        [Test]
        public void OnlyCompletes_WhenServedOrDropped()
        {
            var m = new ServingMinigame(S);
            m.Begin();
            for (int i = 0; i < 600; i++) m.Tick(Dt, default);
            Assert.That(m.IsComplete, Is.False, "walking around doesn't hand the plate over");
            Assert.That(m.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void EachBump_Spills_AndLowersTheScore()
        {
            var m = new ServingMinigame(S);
            m.Begin();
            Assert.That(m.RegisterBump(), Is.True);
            m.Deliver(0f);
            Assert.That(m.Spill, Is.EqualTo(S.spillPerBump).Within(1e-5f));
            Assert.That(m.Evaluate(), Is.EqualTo(1f - S.spillPerBump * S.spillPenalty).Within(1e-4f));
        }

        [Test]
        public void HarderBumps_SpillMore()
        {
            var soft = new ServingMinigame(S);
            soft.Begin();
            soft.RegisterBump(ServingMinigame.BumpStrength(1f, S.carrySpeed));
            var hard = new ServingMinigame(S);
            hard.Begin();
            hard.RegisterBump(ServingMinigame.BumpStrength(8f, S.carrySpeed));
            Assert.That(hard.Spill, Is.GreaterThan(soft.Spill));
            Assert.That(ServingMinigame.BumpStrength(0f, S.carrySpeed), Is.EqualTo(0.5f), "even a brush counts a little");
            Assert.That(ServingMinigame.BumpStrength(100f, S.carrySpeed), Is.EqualTo(1.5f), "and there is a cap");
        }

        [Test]
        public void BumpCooldown_PreventsDoubleCounting()
        {
            var m = new ServingMinigame(S);
            m.Begin();
            m.RegisterBump();
            Assert.That(m.RegisterBump(), Is.False);
        }

        [Test]
        public void FullSpill_DropsThePlate_ScoringZero()
        {
            var m = new ServingMinigame(S);
            m.Begin();
            for (int i = 0; i < 3; i++)
            {
                m.RegisterBump();
                for (int f = 0; f < 60; f++) m.Tick(Dt, default); // wait out the cooldown
            }
            Assert.That(m.Dropped);
            Assert.That(m.IsComplete);
            Assert.That(m.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void DefaultTuning_ATypicalTrip_TakesThreeToFiveSeconds()
        {
            foreach (float tiles in new[] { 12f, 15f })
                Assert.That(tiles / S.carrySpeed, Is.InRange(3f, 5f));
        }
    }

    /// <summary>Staff auto-resolve drives the same IMinigame loop; lower skill must mean lower scores.</summary>
    public class AutoPlayerTests
    {
        static float Average(System.Func<int, float> run)
        {
            float sum = 0f;
            for (int seed = 1; seed <= 40; seed++) sum += run(seed);
            return sum / 40f;
        }

        static float Grill(float skill, int seed)
        {
            var g = new GrillMinigame(GrillSettings.Default);
            return MinigameRunner.RunToCompletion(g, MinigameFactory.CreateAutoPlayer(g, skill, new SeededRandom(seed)));
        }

        static float Tap(float skill, int seed)
        {
            var t = new TapMinigame(TapSettings.Default);
            return MinigameRunner.RunToCompletion(t, MinigameFactory.CreateAutoPlayer(t, skill, new SeededRandom(seed)));
        }

        /// <summary>Staff serving: the auto-player's speed decides how long the 12-tile walk takes.</summary>
        static float Serve(float skill, int seed)
        {
            var s = new ServingMinigame(ServingSettings.Default);
            var player = (ServingAutoPlayer)MinigameFactory.CreateAutoPlayer(s, skill, new SeededRandom(seed));
            s.Begin();
            float seconds = 12f / (ServingSettings.Default.carrySpeed * player.SpeedFactor);
            for (float t = 0f; t < seconds; t += 1f / 60f) s.Tick(1f / 60f, player.NextInput(1f / 60f));
            s.Deliver(12f);
            return s.Evaluate();
        }

        [Test]
        public void Grill_StaffSkill_ScoresBelowExpert_ButUsable()
        {
            float expert = Average(s => Grill(1f, s)), staff = Average(s => Grill(0.6f, s));
            Assert.That(staff, Is.LessThan(expert));
            Assert.That(staff, Is.InRange(0.4f, 0.97f));
        }

        [Test]
        public void Tap_StaffSkill_ScoresBelowExpert_ButUsable()
        {
            float expert = Average(s => Tap(1f, s)), staff = Average(s => Tap(0.6f, s));
            Assert.That(staff, Is.LessThan(expert));
            Assert.That(staff, Is.InRange(0.3f, 0.97f));
        }

        [Test]
        public void Serving_LowerSkill_IsSlower_AndScoresLower()
        {
            Assert.That(Average(s => Serve(0.3f, s)), Is.LessThan(Average(s => Serve(1f, s))));
        }

        [Test]
        public void Factory_HasAnAutoPlayerForEveryStation()
        {
            var f = new MinigameFactory(GrillSettings.Default, TapSettings.Default, ServingSettings.Default);
            var random = new SeededRandom(1);
            Assert.That(MinigameFactory.CreateAutoPlayer(f.CreateCook(Shared.Recipes.CookStation.Grill), 0.5f, random), Is.Not.Null);
            Assert.That(MinigameFactory.CreateAutoPlayer(f.CreateCook(Shared.Recipes.CookStation.Tap), 0.5f, random), Is.Not.Null);
            Assert.That(MinigameFactory.CreateAutoPlayer(f.CreateServing(), 0.5f, random), Is.Not.Null);
        }
    }
}
