using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class HarvestRulesTests
    {
        IngredientDefinition m_Haunch;   // Meat
        IngredientDefinition m_Gel;      // Liquid
        readonly List<Object> m_Created = new();

        [SetUp]
        public void SetUp()
        {
            m_Haunch = Make("rat_haunch", IngredientCategory.Meat);
            m_Gel = Make("slime_gel", IngredientCategory.Liquid);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_Created) Object.DestroyImmediate(o);
            m_Created.Clear();
        }

        IngredientDefinition Make(string id, IngredientCategory category)
        {
            var def = ScriptableObject.CreateInstance<IngredientDefinition>();
            def.id = id;
            def.category = category;
            m_Created.Add(def);
            return def;
        }

        static HarvestPart Part(IngredientDefinition def, Quality q = Quality.Standard, int min = 1, int max = 1, float chance = 1f) =>
            new() { ingredient = def, baseQuality = q, minCount = min, maxCount = max, dropChance = chance };

        static KillContext Kill(IngredientCategory clean = IngredientCategory.None, float overkill = 0f,
            Element element = Element.None, bool finisher = false) =>
            new() { CleanKillCategories = clean, Overkill = overkill, MaxHealth = 20f, Element = element, IsFinisher = finisher };

        static HarvestRuleSettings Rules(float destroyChance = 0.5f)
        {
            var s = HarvestRuleSettings.Default; // overkill ≥ 50% of max, destroy ≥ 150%
            s.destroyChance = destroyChance;
            return s;
        }

        static HarvestDrop Single(IReadOnlyList<HarvestPart> parts, KillContext kill, HarvestRuleSettings rules, int seed = 1)
        {
            var drops = HarvestRules.Resolve(parts, kill, rules, new SeededRandom(seed));
            Assert.That(drops.Count, Is.EqualTo(1));
            return drops[0];
        }

        [Test]
        public void PlainKill_KeepsBaseQuality_Raw()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Standard));
            Assert.That(d.Item.Prep, Is.EqualTo(PrepState.Raw));
            Assert.That(d.Flags, Is.EqualTo(HarvestFlags.None));
            Assert.That(d.Count, Is.EqualTo(1));
        }

        [Test]
        public void CleanKill_MatchingCategory_RaisesOneTier()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(clean: IngredientCategory.Meat | IngredientCategory.Offal), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Fine));
            Assert.That(d.Flags & HarvestFlags.CleanKill, Is.EqualTo(HarvestFlags.CleanKill));
        }

        [Test]
        public void CleanKill_OnlyAppliesToMatchingCategories()
        {
            var drops = HarvestRules.Resolve(new[] { Part(m_Haunch), Part(m_Gel) },
                Kill(clean: IngredientCategory.Meat), Rules(), new SeededRandom(1));
            Assert.That(drops[0].Item.Quality, Is.EqualTo(Quality.Fine));
            Assert.That(drops[1].Item.Quality, Is.EqualTo(Quality.Standard));
        }

        [Test]
        public void Overkill_LowersOneTier()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(overkill: 10f), Rules()); // 50% of 20
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Poor));
            Assert.That(d.Flags & HarvestFlags.Overkill, Is.EqualTo(HarvestFlags.Overkill));
        }

        [Test]
        public void SmallOverkill_BelowThreshold_IsNotPenalised()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(overkill: 9f), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Standard));
        }

        [Test]
        public void CleanKillAndOverkill_Cancel()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(clean: IngredientCategory.Meat, overkill: 12f), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Standard));
        }

        [Test]
        public void HeavyOverkill_WithCertainDestroyChance_DestroysPart()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(overkill: 30f), Rules(destroyChance: 1f)); // 150%
            Assert.That(d.Destroyed);
            Assert.That(d.Count, Is.EqualTo(0));
        }

        [Test]
        public void HeavyOverkill_WithZeroDestroyChance_StillDrops()
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(overkill: 30f), Rules(destroyChance: 0f));
            Assert.That(d.Destroyed, Is.False);
            Assert.That(d.Count, Is.EqualTo(1));
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Poor));
        }

        [TestCase(Element.None, PrepState.Raw)]
        [TestCase(Element.Fire, PrepState.Seared)]
        [TestCase(Element.Ice, PrepState.Chilled)]
        [TestCase(Element.Poison, PrepState.Inedible)]
        public void Element_SetsPrepState(Element element, PrepState expected)
        {
            var d = Single(new[] { Part(m_Haunch) }, Kill(element: element), Rules());
            Assert.That(d.Item.Prep, Is.EqualTo(expected));
        }

        [Test]
        public void Quality_ClampsAtPremium()
        {
            var d = Single(new[] { Part(m_Haunch, Quality.Premium) }, Kill(clean: IngredientCategory.Meat), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Premium));
        }

        [Test]
        public void Quality_ClampsAtPoor()
        {
            var d = Single(new[] { Part(m_Haunch, Quality.Poor) }, Kill(overkill: 12f), Rules());
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Poor));
        }

        [Test]
        public void Finisher_GuaranteesPremium_AndIgnoresOverkill()
        {
            var d = Single(new[] { Part(m_Haunch, Quality.Poor) }, Kill(overkill: 100f, finisher: true), Rules(destroyChance: 1f));
            Assert.That(d.Item.Quality, Is.EqualTo(Quality.Premium));
            Assert.That(d.Destroyed, Is.False);
            Assert.That(d.Flags & HarvestFlags.Finisher, Is.EqualTo(HarvestFlags.Finisher));
        }

        [Test]
        public void DropChance_Zero_NeverDrops()
        {
            var drops = HarvestRules.Resolve(new[] { Part(m_Haunch, chance: 0f) }, Kill(), Rules(), new SeededRandom(7));
            Assert.That(drops, Is.Empty);
        }

        [Test]
        public void DropChance_Partial_IsDeterministicForSeed_AndRoughlyCorrect()
        {
            var parts = new[] { Part(m_Haunch, chance: 0.3f) };
            int Count(int seed)
            {
                var rng = new SeededRandom(seed);
                int n = 0;
                for (int i = 0; i < 2000; i++) n += HarvestRules.Resolve(parts, Kill(), Rules(), rng).Count;
                return n;
            }
            Assert.That(Count(42), Is.EqualTo(Count(42)), "same seed, same result");
            Assert.That(Count(42) / 2000f, Is.EqualTo(0.3f).Within(0.04f));
        }

        [Test]
        public void Count_IsWithinMinMax()
        {
            var rng = new SeededRandom(3);
            var parts = new[] { Part(m_Haunch, min: 1, max: 3) };
            var seen = new HashSet<int>();
            for (int i = 0; i < 200; i++) seen.Add(HarvestRules.Resolve(parts, Kill(), Rules(), rng)[0].Count);
            Assert.That(seen, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void NullOrEmptyParts_AreSkipped()
        {
            var drops = HarvestRules.Resolve(new[] { null, new HarvestPart() }, Kill(), Rules(), new SeededRandom(1));
            Assert.That(drops, Is.Empty);
        }
    }
}
