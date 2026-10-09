using Hearthdelve.Shared.Engine;
using NUnit.Framework;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4i-C: the time steps put back when they've drifted, never while time is meant to be scaled.</summary>
    public class TimeBaselineTests
    {
        [Test]
        public void ADriftedStep_IsRestored_OnlyAtTheNormalTimeScale()
        {
            Assert.That(TimeBaselineRules.ShouldRestore(1f, 0.03f, 0.3333f), Is.True, "the stuck cap (2026-10-09) is undone");
            Assert.That(TimeBaselineRules.ShouldRestore(1f, 0.3333f, 0.3333f), Is.False, "nothing to undo");
            Assert.That(TimeBaselineRules.ShouldRestore(0.09f, 0.03f, 0.3333f), Is.False, "a hit-stop's scaled step is meant");
            Assert.That(TimeBaselineRules.ShouldRestore(0f, 0f, 0.3333f), Is.False, "a pause is meant");
        }
    }
}
