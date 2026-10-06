using Hearthdelve.Core.Pathfinding;
using NUnit.Framework;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>When a walker has arrived, at any frame rate (the 4f web check: Pip circling the pass at three frames a second).</summary>
    public class ArrivalTests
    {
        [Test]
        public void AtANormalFrameRate_TheRadiusDecides()
        {
            Assert.That(Arrival.Reach(0.2f, 3f, 1f / 60f), Is.EqualTo(0.2f));
        }

        [Test]
        public void AtASlowFrameRate_OneFramesTravelCountsAsThere()
        {
            Assert.That(Arrival.Reach(0.2f, 3f, 1f / 3f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void StandingStill_OrAPausedFrame_UsesTheRadius()
        {
            Assert.That(Arrival.Reach(0.2f, 0f, 1f / 3f), Is.EqualTo(0.2f));
            Assert.That(Arrival.Reach(0.2f, 3f, 0f), Is.EqualTo(0.2f));
            Assert.That(Arrival.Reach(0.2f, -3f, 1f), Is.EqualTo(0.2f));
        }

        [Test]
        public void SettingOffAgain_AlwaysNeedsMoreThanTheReach()
        {
            Assert.That(Arrival.Resume(0.35f, 0.2f), Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(Arrival.Resume(0.35f, 1f), Is.GreaterThan(1f));
        }
    }
}
