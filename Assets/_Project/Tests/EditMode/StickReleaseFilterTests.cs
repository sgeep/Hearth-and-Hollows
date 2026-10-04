using Hearthdelve.Core.Input;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>A released stick's spring-back is not movement; real pushes, gentle walking and keyboards are untouched.</summary>
    public class StickReleaseFilterTests
    {
        static StickReleaseFilter New() => new(StickReleaseSettings.Default);

        [Test]
        public void TheSpringBack_AfterAFlick_ReadsAsRest()
        {
            var filter = New();
            Assert.That(filter.Filter(new Vector2(-0.75f, -0.66f), 0f), Is.EqualTo(new Vector2(-0.75f, -0.66f)));
            Assert.That(filter.Filter(new Vector2(-0.2f, -0.18f), 0.016f), Is.EqualTo(new Vector2(-0.2f, -0.18f)), "easing off the same way still moves");
            Assert.That(filter.Filter(new Vector2(0.3f, 0.28f), 0.033f), Is.EqualTo(Vector2.zero), "the overshoot the other way");
            Assert.That(filter.Filter(new Vector2(0.05f, 0.03f), 0.05f), Is.EqualTo(Vector2.zero));
            Assert.That(filter.Filter(Vector2.zero, 0.066f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ARealPushTheOtherWay_PassesAtOnce()
        {
            var filter = New();
            filter.Filter(Vector2.left, 0f);
            Assert.That(filter.Filter(new Vector2(0.8f, 0f), 0.016f), Is.EqualTo(new Vector2(0.8f, 0f)));
            Assert.That(filter.Filter(Vector2.right, 0.033f), Is.EqualTo(Vector2.right), "keyboards reverse instantly");
        }

        [Test]
        public void AGentlePushTheOtherWay_PassesOnceTheWindowIsOver()
        {
            var filter = New();
            filter.Filter(Vector2.up, 0f);
            Assert.That(filter.Filter(new Vector2(0f, -0.3f), 0.05f), Is.EqualTo(Vector2.zero));
            Assert.That(filter.Filter(new Vector2(0f, -0.3f), 0.2f), Is.EqualTo(new Vector2(0f, -0.3f)), "held, it's meant");
        }

        [Test]
        public void GentleWalking_WithNoRecentRealPush_IsUntouched()
        {
            var filter = New();
            Assert.That(filter.Filter(new Vector2(0.2f, 0f), 0f), Is.EqualTo(new Vector2(0.2f, 0f)));
            Assert.That(filter.Filter(new Vector2(-0.2f, 0f), 0.016f), Is.EqualTo(new Vector2(-0.2f, 0f)), "small turns at walking pace pass");
            Assert.That(filter.Filter(new Vector2(0f, 0.3f), 0.033f), Is.EqualTo(new Vector2(0f, 0.3f)), "sideways after a real push is not a spring-back");
        }
    }
}
