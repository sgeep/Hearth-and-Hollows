using System.Collections.Generic;
using Hearthdelve.Tavern.Customers;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    /// <summary>A customer's look comes from their seed: fixed for one customer, varied between customers (4c decision 3).</summary>
    public class AppearanceRulesTests
    {
        [Test]
        public void TheSameSeed_AlwaysGivesTheSameLook()
        {
            for (int seed = 0; seed < 50; seed++)
                Assert.That(AppearanceRules.Pick(seed, 4, 10, 3, 20, 5, 0.3f), Is.EqualTo(AppearanceRules.Pick(seed, 4, 10, 3, 20, 5, 0.3f)));
        }

        [Test]
        public void DifferentSeeds_GiveVariedLooks_WithinEveryList()
        {
            var looks = new HashSet<AppearanceChoice>();
            var tops = new HashSet<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                AppearanceChoice c = AppearanceRules.Pick(seed, 4, 10, 3, 20, 5, 0.3f);
                Assert.That(c.Body, Is.InRange(0, 3));
                Assert.That(c.Top, Is.InRange(0, 9));
                Assert.That(c.Trousers, Is.InRange(0, 2));
                Assert.That(c.Head, Is.InRange(0, 19));
                Assert.That(c.Beard, Is.InRange(-1, 4));
                looks.Add(c);
                tops.Add(c.Top);
            }
            Assert.That(looks.Count, Is.GreaterThan(150), "mostly different customers");
            Assert.That(tops.Count, Is.EqualTo(10), "every top gets worn, including the last");
        }

        [Test]
        public void EmptyLists_AndBeardChance()
        {
            AppearanceChoice none = AppearanceRules.Pick(7, 0, 0, 0, 0, 0, 1f);
            Assert.That(new[] { none.Body, none.Top, none.Trousers, none.Head, none.Beard }, Is.All.EqualTo(-1));
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(AppearanceRules.Pick(seed, 1, 1, 1, 1, 3, 0f).Beard, Is.EqualTo(-1), "never a beard at 0");
                Assert.That(AppearanceRules.Pick(seed, 1, 1, 1, 1, 3, 1f).Beard, Is.InRange(0, 2), "always a beard at 1 (dwarves)");
            }
        }
    }
}
