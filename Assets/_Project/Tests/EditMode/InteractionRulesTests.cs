using Hearthdelve.Tavern.Scene;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>Which station, pass or seat the player is about to use (4c).</summary>
    public class InteractionRulesTests
    {
        static InteractionCandidate At(float x, float y, float reach = 1f, bool available = true) => new(new Vector2(x, y), reach, available);

        [Test]
        public void PicksTheNearestInReach()
        {
            var candidates = new[] { At(0f, 0f), At(0.5f, 0f), At(3f, 0f) };
            Assert.That(InteractionRules.Pick(new Vector2(0.4f, 0f), candidates), Is.EqualTo(1));
        }

        [Test]
        public void NothingOutOfReach_OrUnavailable()
        {
            Assert.That(InteractionRules.Pick(Vector2.zero, new[] { At(2f, 0f) }), Is.EqualTo(-1), "out of reach");
            Assert.That(InteractionRules.Pick(Vector2.zero, new[] { At(0.2f, 0f, available: false), At(0.8f, 0f) }), Is.EqualTo(1), "skips what can't be used now");
            Assert.That(InteractionRules.Pick(Vector2.zero, new[] { At(0.5f, 0f, reach: 0.4f), At(0f, 1.2f, reach: 1.5f) }), Is.EqualTo(1), "each has its own reach");
        }

        [Test]
        public void KeepsTheCurrentTarget_UntilAnotherIsClearlyNearer()
        {
            var candidates = new[] { At(0f, 0f), At(1f, 0f) };
            // Just past halfway, but within the stickiness: the highlight doesn't flicker.
            Assert.That(InteractionRules.Pick(new Vector2(0.55f, 0f), candidates, current: 0), Is.EqualTo(0));
            Assert.That(InteractionRules.Pick(new Vector2(0.55f, 0f), candidates), Is.EqualTo(1), "without a current target the nearer one wins");
            Assert.That(InteractionRules.Pick(new Vector2(0.8f, 0f), candidates, current: 0), Is.EqualTo(1), "clearly nearer takes over");
        }

        [Test]
        public void DropsTheCurrentTarget_WhenItGoesOutOfReachOrUnavailable()
        {
            Assert.That(InteractionRules.Pick(new Vector2(1.5f, 0f), new[] { At(0f, 0f), At(2f, 0f) }, current: 0), Is.EqualTo(1));
            Assert.That(InteractionRules.Pick(new Vector2(0.4f, 0f), new[] { At(0f, 0f, available: false), At(1f, 0f) }, current: 0), Is.EqualTo(1));
            Assert.That(InteractionRules.Pick(new Vector2(5f, 0f), new[] { At(0f, 0f) }, current: 0), Is.EqualTo(-1));
        }
    }
}
