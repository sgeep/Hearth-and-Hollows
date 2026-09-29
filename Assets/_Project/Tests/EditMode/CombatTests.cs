using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Combat;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class DamageCalculatorTests
    {
        [Test]
        public void NonLethalHit_ReducesHealth_NoOverkill()
        {
            var r = DamageCalculator.Apply(30f, 10f);
            Assert.That(r.Dealt, Is.EqualTo(10f));
            Assert.That(r.RemainingHealth, Is.EqualTo(20f));
            Assert.That(r.Killed, Is.False);
            Assert.That(r.Overkill, Is.EqualTo(0f));
        }

        [Test]
        public void LethalHit_ReportsOverkill()
        {
            var r = DamageCalculator.Apply(5f, 25f);
            Assert.That(r.Killed);
            Assert.That(r.Dealt, Is.EqualTo(5f));
            Assert.That(r.Overkill, Is.EqualTo(20f));
            Assert.That(r.RemainingHealth, Is.EqualTo(0f));
        }

        [Test]
        public void ExactKill_HasZeroOverkill()
        {
            var r = DamageCalculator.Apply(10f, 10f);
            Assert.That(r.Killed);
            Assert.That(r.Overkill, Is.EqualTo(0f));
        }

        [Test]
        public void HittingTheDead_DoesNothing()
        {
            var r = DamageCalculator.Apply(0f, 50f);
            Assert.That(r.Killed, Is.False);
            Assert.That(r.Dealt, Is.EqualTo(0f));
        }

        [Test]
        public void NegativeDamage_IsIgnored()
        {
            var r = DamageCalculator.Apply(10f, -5f);
            Assert.That(r.RemainingHealth, Is.EqualTo(10f));
        }
    }

    public class HitStopTests
    {
        [Test]
        public void Request_ActivatesThenExpires()
        {
            var stop = new HitStop();
            stop.Request(0.1f);
            Assert.That(stop.IsActive);
            Assert.That(stop.Tick(0.05f), Is.True);
            Assert.That(stop.Tick(0.06f), Is.False);
            Assert.That(stop.IsActive, Is.False);
        }

        [Test]
        public void OverlappingRequests_TakeLongest_DoNotStack()
        {
            var stop = new HitStop();
            stop.Request(0.1f);
            stop.Request(0.05f);
            stop.Request(0.08f);
            Assert.That(stop.Remaining, Is.EqualTo(0.1f).Within(1e-6f));
        }

        [Test]
        public void Request_IsClampedToMax()
        {
            var stop = new HitStop { MaxDuration = 0.2f };
            stop.Request(5f);
            Assert.That(stop.Remaining, Is.EqualTo(0.2f).Within(1e-6f));
        }

        [Test]
        public void ZeroOrNegativeRequest_IsIgnored()
        {
            var stop = new HitStop();
            stop.Request(0f);
            stop.Request(-1f);
            Assert.That(stop.IsActive, Is.False);
        }
    }
}
