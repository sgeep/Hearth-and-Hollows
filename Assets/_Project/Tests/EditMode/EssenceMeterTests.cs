using Hearthdelve.Dungeon.Essence;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class EssenceMeterTests
    {
        static EssenceSettings Settings(float max = 100f, float drain = 2f, float dmgMult = 1f, float low = 0.25f) =>
            new() { baseMax = max, drainPerSecond = drain, damageMultiplier = dmgMult, lowThreshold = low };

        [Test]
        public void StartsFull()
        {
            var m = new EssenceMeter(Settings(), EssenceModifiers.None);
            Assert.That(m.Current, Is.EqualTo(100f));
            Assert.That(m.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void DrainsOverTime()
        {
            var m = new EssenceMeter(Settings(drain: 2f), EssenceModifiers.None);
            m.Tick(5f);
            Assert.That(m.Current, Is.EqualTo(90f).Within(1e-4f));
        }

        [Test]
        public void Paused_DoesNotDrain_ButDamageStillApplies()
        {
            var m = new EssenceMeter(Settings(), EssenceModifiers.None) { Paused = true };
            m.Tick(10f);
            Assert.That(m.Current, Is.EqualTo(100f));
            m.TakeDamage(5f);
            Assert.That(m.Current, Is.EqualTo(95f));
        }

        [Test]
        public void Damage_UsesMultiplier_AndReportsLoss()
        {
            var m = new EssenceMeter(Settings(dmgMult: 1.5f), EssenceModifiers.None);
            float lost = m.TakeDamage(10f);
            Assert.That(lost, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(m.Current, Is.EqualTo(85f).Within(1e-4f));
        }

        [Test]
        public void Changed_FlagsDamageVersusDrain()
        {
            var m = new EssenceMeter(Settings(), EssenceModifiers.None);
            bool? lastFromDamage = null;
            m.Changed += fromDamage => lastFromDamage = fromDamage;
            m.Tick(1f);
            Assert.That(lastFromDamage, Is.False);
            m.TakeDamage(1f);
            Assert.That(lastFromDamage, Is.True);
        }

        [Test]
        public void ClampsAtZero_AndDepletedFiresExactlyOnce()
        {
            var m = new EssenceMeter(Settings(max: 10f), EssenceModifiers.None);
            int depleted = 0;
            m.Depleted += () => depleted++;

            m.TakeDamage(25f);
            m.TakeDamage(5f);
            m.Tick(10f);

            Assert.That(m.Current, Is.EqualTo(0f));
            Assert.That(m.IsDepleted);
            Assert.That(depleted, Is.EqualTo(1));
        }

        [Test]
        public void DrainToZero_OverTime_Depletes()
        {
            var m = new EssenceMeter(Settings(max: 10f, drain: 1f), EssenceModifiers.None);
            bool depleted = false;
            m.Depleted += () => depleted = true;
            for (int i = 0; i < 11 * 60; i++) m.Tick(1f / 60f);
            Assert.That(depleted);
        }

        [Test]
        public void Restore_ClampsToMax_AndDoesNotReviveDepleted()
        {
            var m = new EssenceMeter(Settings(max: 50f), EssenceModifiers.None);
            m.TakeDamage(10f);
            m.Restore(100f);
            Assert.That(m.Current, Is.EqualTo(50f));

            m.TakeDamage(60f);
            m.Restore(20f);
            Assert.That(m.Current, Is.EqualTo(0f), "depletion is final for this delve");
        }

        [Test]
        public void IsLow_BelowThreshold()
        {
            var m = new EssenceMeter(Settings(max: 100f, low: 0.25f), EssenceModifiers.None);
            m.TakeDamage(74f);
            Assert.That(m.IsLow, Is.False);
            m.TakeDamage(1f);
            Assert.That(m.IsLow, Is.True);
        }

        [Test]
        public void Upgrades_RaiseMax_AndSlowDrain()
        {
            var mods = new EssenceModifiers { MaxBonus = 50f, DrainMultiplier = 0.5f };
            var m = new EssenceMeter(Settings(max: 100f, drain: 2f), mods);
            Assert.That(m.Max, Is.EqualTo(150f));
            m.Tick(10f);
            Assert.That(m.Current, Is.EqualTo(140f).Within(1e-4f));
        }
    }
}
