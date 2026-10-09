using Hearthdelve.Core.Haptics;
using Hearthdelve.Editor;
using Hearthdelve.Tavern.Minigames;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>The tavern's feedback mapping (4c step 6): which moment a minigame event is, and how strong continuous rumble is.</summary>
    public class TavernFeedbackRulesTests
    {
        static readonly TavernFeedbackSettings S = TavernFeedbackSettings.Default;
        static readonly GrillSettings Grill = GrillSettings.Default;
        static readonly TapSettings Tap = TapSettings.Default;

        [Test]
        public void TheGrill_IsQuietWhileCooking_AndWarnsMoreAndMore_PastTheBand()
        {
            Assert.That(TavernFeedbackRules.GrillWarning(0.2f, Grill, S), Is.Zero, "raw: nothing");
            Assert.That(TavernFeedbackRules.GrillWarning(Grill.BandCenter, Grill, S), Is.Zero, "in the band: nothing");
            float early = TavernFeedbackRules.GrillWarning(Grill.bandMax + 0.02f, Grill, S);
            float late = TavernFeedbackRules.GrillWarning(0.97f, Grill, S);
            Assert.That(early, Is.GreaterThan(0f), "just past the band: a hint");
            Assert.That(late, Is.GreaterThan(early * 2f), "near the burn: much stronger");
            Assert.That(late, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void AFlip_IsPlain_Perfect_OrBurned()
        {
            Assert.That(TavernFeedbackRules.Flip(0.6f, false, S), Is.EqualTo(FlipFeel.Flip));
            Assert.That(TavernFeedbackRules.Flip(1f, false, S), Is.EqualTo(FlipFeel.Perfect));
            Assert.That(TavernFeedbackRules.Flip(0f, true, S), Is.EqualTo(FlipFeel.Burned), "a burn is a failure, whatever the score");
        }

        [Test]
        public void Pouring_RumblesSteadily_AndRisesTowardTheLine()
        {
            Assert.That(TavernFeedbackRules.Pour(false, 0.5f, Tap, S), Is.EqualTo((0f, 0f)), "not pouring: still");
            var start = TavernFeedbackRules.Pour(true, 0.1f, Tap, S);
            var near = TavernFeedbackRules.Pour(true, Tap.fillLine * 0.95f, Tap, S);
            var over = TavernFeedbackRules.Pour(true, Tap.fillLine * 1.1f, Tap, S);
            Assert.That(start.low, Is.EqualTo(S.pourBase), "the stream");
            Assert.That(near.high, Is.GreaterThan(start.high));
            Assert.That(over.high, Is.GreaterThan(near.high), "past the line it only gets more urgent");
        }

        [Test]
        public void TheLine_IsReachedOnce_AsTheFillCrossesIt()
        {
            float line = Tap.fillLine - Tap.fillTolerance;
            Assert.That(TavernFeedbackRules.ReachedLine(line - 0.01f, line + 0.001f, Tap), Is.True);
            Assert.That(TavernFeedbackRules.ReachedLine(line + 0.001f, line + 0.02f, Tap), Is.False, "already there");
            Assert.That(TavernFeedbackRules.ReachedLine(0.2f, 0.3f, Tap), Is.False);
        }

        [Test]
        public void APour_EndsClean_Plain_OrOverflowing()
        {
            Assert.That(TavernFeedbackRules.Pour(true, 1f, S), Is.EqualTo(PourResult.Overflow));
            Assert.That(TavernFeedbackRules.Pour(false, 0.9f, S), Is.EqualTo(PourResult.Clean));
            Assert.That(TavernFeedbackRules.Pour(false, 0.5f, S), Is.EqualTo(PourResult.Plain));
        }

        [Test]
        public void Cuts_AreCleanOrRagged_AndABoard_GoodOrNot()
        {
            Assert.That(TavernFeedbackRules.Cut(0.9f, S), Is.EqualTo(CutFeel.Clean));
            Assert.That(TavernFeedbackRules.Cut(0.4f, S), Is.EqualTo(CutFeel.Ragged));
            Assert.That(TavernFeedbackRules.GoodChop(0.8f, S), Is.True);
            Assert.That(TavernFeedbackRules.GoodChop(0.5f, S), Is.False);
        }

        [Test]
        public void ABrush_IsSoft_AndACollision_IsHard_InProportion()
        {
            var brush = TavernFeedbackRules.Bump(0.55f, S);
            var knock = TavernFeedbackRules.Bump(1.4f, S);
            Assert.That(brush.feel, Is.EqualTo(BumpFeel.Soft));
            Assert.That(knock.feel, Is.EqualTo(BumpFeel.Hard));
            Assert.That(knock.intensity, Is.GreaterThan(brush.intensity), "stronger bumps feel stronger");
            Assert.That(brush.intensity, Is.GreaterThan(0f).And.LessThan(0.6f), "a brush is felt, lightly");
        }

        [Test]
        public void TheSpillWarning_ComesOnce_AsTheMeterPassesIt()
        {
            Assert.That(TavernFeedbackRules.SpillWarning(0.34f, 0.68f, S), Is.True);
            Assert.That(TavernFeedbackRules.SpillWarning(0.68f, 0.99f, S), Is.False, "once");
            Assert.That(TavernFeedbackRules.SpillWarning(0f, 0.34f, S), Is.False);
        }
    }
}
