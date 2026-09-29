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

        static TapMinigame PourTo(float total, float aimY)
        {
            var t = new TapMinigame(S);
            t.Begin();
            var aim = new Vector2(0f, aimY);
            for (int i = 0; i < 60; i++) t.Tick(Dt, new MinigameInput { Aim = aim }); // settle tilt
            while (t.Total + S.pourRate * Dt < total && !t.IsComplete) t.Tick(Dt, new MinigameInput { ActionHeld = true, Aim = aim });
            t.Tick(Dt, new MinigameInput { Aim = aim }); // release
            return t;
        }

        static float IdealAim => TapMinigame.IdealTilt(S) * 2f - 1f;

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

    public class ServingMinigameTests
    {
        const float Dt = 1f / 60f;
        static readonly ServingSettings S = ServingSettings.Default;

        static float Walk(ServingMinigame m)
        {
            m.Begin();
            while (!m.IsComplete) m.Tick(Dt, new MinigameInput { Move = Mathf.Sign(m.Target - m.Position) });
            return m.Evaluate();
        }

        [Test]
        public void DirectDelivery_ScoresOne()
        {
            Assert.That(Walk(new ServingMinigame(S, 15f, 5f)), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void EachBump_Spills_AndLowersScore()
        {
            var m = new ServingMinigame(S, 15f, 5f);
            m.Begin();
            Assert.That(m.RegisterBump(), Is.True);
            while (!m.IsComplete) m.Tick(Dt, new MinigameInput { Move = -1f });
            Assert.That(m.Spill, Is.EqualTo(S.spillPerBump).Within(1e-5f));
            Assert.That(m.Evaluate(), Is.EqualTo(1f - S.spillPerBump * S.spillPenalty).Within(1e-4f));
        }

        [Test]
        public void BumpCooldown_PreventsDoubleCounting()
        {
            var m = new ServingMinigame(S, 15f, 5f);
            m.Begin();
            m.RegisterBump();
            Assert.That(m.RegisterBump(), Is.False);
        }

        [Test]
        public void FullSpill_DropsThePlate_ScoringZero()
        {
            var m = new ServingMinigame(S, 15f, 5f);
            m.Begin();
            for (int i = 0; i < 3; i++)
            {
                m.RegisterBump();
                for (int f = 0; f < 60; f++) m.Tick(Dt, default); // wait out the cooldown in place
            }
            Assert.That(m.Dropped);
            Assert.That(m.IsComplete);
            Assert.That(m.Evaluate(), Is.EqualTo(0f));
        }

        [Test]
        public void Dawdling_PastPar_LowersScore()
        {
            var m = new ServingMinigame(S, 15f, 5f);
            m.Begin();
            for (int i = 0; i < 60 * 4; i++) m.Tick(Dt, default); // stand still for 4 s
            while (!m.IsComplete) m.Tick(Dt, new MinigameInput { Move = -1f });
            Assert.That(m.Evaluate(), Is.LessThan(0.8f));
        }

        [Test]
        public void DefaultTuning_MidFloorTable_TakesFiveToTenSeconds()
        {
            var m = new ServingMinigame(S, 15f, 5f); // 10 tiles
            Walk(m);
            Assert.That(m.Elapsed, Is.InRange(5f, 10f));
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

        static float Serve(float skill, int seed)
        {
            var s = new ServingMinigame(ServingSettings.Default, 15f, 5f);
            return MinigameRunner.RunToCompletion(s, MinigameFactory.CreateAutoPlayer(s, skill, new SeededRandom(seed)));
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
            Assert.That(MinigameFactory.CreateAutoPlayer(f.CreateServing(0f, 5f), 0.5f, random), Is.Not.Null);
        }
    }
}
