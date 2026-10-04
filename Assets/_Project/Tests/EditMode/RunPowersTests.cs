using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Shared.Run;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>Run powers (4d step 4): taking them, what they add up to, and what a power room offers.</summary>
    public class RunPowersTests
    {
        readonly List<RunPowerDefinition> m_Made = new();

        RunPowerDefinition Power(string id, RunPowerEffect effect, float amount)
        {
            var p = ScriptableObject.CreateInstance<RunPowerDefinition>();
            p.id = id;
            p.effect = effect;
            p.amount = amount;
            m_Made.Add(p);
            return p;
        }

        [TearDown]
        public void Clean()
        {
            foreach (RunPowerDefinition p in m_Made) Object.DestroyImmediate(p);
            m_Made.Clear();
        }

        List<RunPowerDefinition> Pool() => new()
        {
            Power("a", RunPowerEffect.MaxEssence, 25f), Power("b", RunPowerEffect.SlowerDrain, 0.3f), Power("c", RunPowerEffect.LighterHits, 0.3f),
            Power("d", RunPowerEffect.LightDamage, 0.25f), Power("e", RunPowerEffect.HeavyDamage, 0.4f), Power("f", RunPowerEffect.FasterDodge, 0.4f),
            Power("g", RunPowerEffect.EssenceOnClear, 10f), Power("h", RunPowerEffect.GentleKills, 0.5f),
        };

        [Test]
        public void NoPowers_ChangeNothing()
        {
            RunModifiers m = new RunPowers().Modifiers;
            Assert.That(m.MaxEssenceBonus, Is.Zero);
            Assert.That(new[] { m.DrainMultiplier, m.HitCostMultiplier, m.LightDamageMultiplier, m.HeavyDamageMultiplier, m.DodgeCooldownMultiplier, m.OverkillToleranceMultiplier },
                Is.All.EqualTo(1f));
            Assert.That(m.EssenceOnClear, Is.Zero);
        }

        [Test]
        public void EachPower_ChangesItsOwnNumber()
        {
            var powers = new RunPowers();
            foreach (RunPowerDefinition p in Pool()) Assert.That(powers.Take(p));
            RunModifiers m = powers.Modifiers;
            Assert.That(m.MaxEssenceBonus, Is.EqualTo(25f));
            Assert.That(m.DrainMultiplier, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(m.HitCostMultiplier, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(m.LightDamageMultiplier, Is.EqualTo(1.25f).Within(1e-5f));
            Assert.That(m.HeavyDamageMultiplier, Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(m.DodgeCooldownMultiplier, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(m.EssenceOnClear, Is.EqualTo(10f));
            Assert.That(m.OverkillToleranceMultiplier, Is.EqualTo(1.5f).Within(1e-5f));
        }

        [Test]
        public void APower_IsTakenOnce_AndSaysSo()
        {
            var powers = new RunPowers();
            RunPowerDefinition hide = Power("thick_hide", RunPowerEffect.LighterHits, 0.3f);
            var seen = new List<RunPowerDefinition>();
            powers.Changed += seen.Add;
            Assert.That(powers.Take(hide));
            Assert.That(powers.Take(hide), Is.False, "once a run");
            Assert.That(powers.Take(null), Is.False);
            Assert.That(powers.Taken, Has.Count.EqualTo(1));
            Assert.That(seen, Is.EqualTo(new[] { hide }));
        }

        [Test]
        public void Reductions_Multiply_AndNeverReachZero()
        {
            RunModifiers two = RunPowers.Combine(new[] { Power("x", RunPowerEffect.LighterHits, 0.3f), Power("y", RunPowerEffect.LighterHits, 0.3f) });
            Assert.That(two.HitCostMultiplier, Is.EqualTo(0.49f).Within(1e-5f));
            RunModifiers all = RunPowers.Combine(new[] { Power("z", RunPowerEffect.SlowerDrain, 1.5f) });
            Assert.That(all.DrainMultiplier, Is.EqualTo(RunPowers.MinMultiplier), "Essence always drains a little");
        }

        [Test]
        public void TheOffer_IsThreeDifferentPowers_NotYetTaken()
        {
            List<RunPowerDefinition> pool = Pool();
            var powers = new RunPowers();
            powers.Take(pool[0]);
            powers.Take(pool[3]);
            for (int seed = 1; seed <= 200; seed++)
            {
                List<RunPowerDefinition> offer = powers.Offer(pool, 3, new SeededRandom(seed));
                Assert.That(offer, Has.Count.EqualTo(3));
                Assert.That(offer.Select(p => p.id).Distinct().Count(), Is.EqualTo(3));
                Assert.That(offer, Has.None.SameAs(pool[0]).And.None.SameAs(pool[3]));
            }
        }

        [Test]
        public void TheOffer_FollowsTheSeed_AndVaries()
        {
            List<RunPowerDefinition> pool = Pool();
            var powers = new RunPowers();
            string Offer(int seed) => string.Join(",", powers.Offer(pool, 3, new SeededRandom(seed)).Select(p => p.id));
            Assert.That(Offer(7), Is.EqualTo(Offer(7)));
            Assert.That(Enumerable.Range(1, 40).Select(Offer).Distinct().Count(), Is.GreaterThan(10));
            var seen = Enumerable.Range(1, 200).SelectMany(s => powers.Offer(pool, 3, new SeededRandom(s))).Select(p => p.id).Distinct();
            Assert.That(seen.Count(), Is.EqualTo(8), "every power turns up");
        }

        [Test]
        public void TheOffer_ShrinksAsThePoolRunsOut()
        {
            List<RunPowerDefinition> pool = Pool();
            var powers = new RunPowers();
            foreach (RunPowerDefinition p in pool.Take(6)) powers.Take(p);
            Assert.That(powers.Offer(pool, 3, new SeededRandom(1)), Has.Count.EqualTo(2));
            foreach (RunPowerDefinition p in pool) powers.Take(p);
            Assert.That(powers.Offer(pool, 3, new SeededRandom(1)), Is.Empty);
            Assert.That(powers.Offer(null, 3, new SeededRandom(1)), Is.Empty);
        }

        [Test]
        public void Descriptions_ShowEssenceFlat_AndTheRestAsPercentages()
        {
            Assert.That(RunPowers.ShownAmount(Power("a", RunPowerEffect.MaxEssence, 25f)), Is.EqualTo(25));
            Assert.That(RunPowers.ShownAmount(Power("g", RunPowerEffect.EssenceOnClear, 10f)), Is.EqualTo(10));
            Assert.That(RunPowers.ShownAmount(Power("c", RunPowerEffect.LighterHits, 0.3f)), Is.EqualTo(30));
        }

        // ---------- Essence ----------

        static EssenceSettings Essence() => new() { baseMax = 100f, drainPerSecond = 2f, damageMultiplier = 1f, lowThreshold = 0.25f };

        [Test]
        public void RaisingMaxEssence_FillsTheNewPart()
        {
            var m = new EssenceMeter(Essence(), EssenceModifiers.None);
            m.TakeDamage(40f);
            m.RaiseMax(25f);
            Assert.That(m.Max, Is.EqualTo(125f));
            Assert.That(m.Current, Is.EqualTo(85f));
        }

        [Test]
        public void RunMultipliers_SlowTheDrain_AndLightenHits()
        {
            var m = new EssenceMeter(Essence(), EssenceModifiers.None) { RunDrainMultiplier = 0.5f, HitCostMultiplier = 0.7f };
            m.Tick(10f);
            Assert.That(m.Current, Is.EqualTo(90f).Within(1e-4f), "half of 2 per second");
            Assert.That(m.TakeDamage(10f), Is.EqualTo(7f).Within(1e-4f));
        }
    }
}
