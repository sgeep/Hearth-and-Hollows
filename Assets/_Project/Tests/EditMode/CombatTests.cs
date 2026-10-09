using Hearthdelve.Dungeon.Combat;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// The damage the cleaver deals: its attack's damage scaled by the run's light or heavy multiplier (CombatMeleeWeapon). The test
    /// review (2026-10-09) retired the tests of <c>DamageCalculator.Apply</c> and <c>ComboLogic</c> with that code, which nothing
    /// called (the combo and the health pool are TDE's); this is the part that ships.
    /// </summary>
    public class DamageCalculatorTests
    {
        [Test]
        public void Scale_MultipliesTheBaseDamage()
        {
            Assert.That(DamageCalculator.Scale(12f, 1f), Is.EqualTo(12f));
            Assert.That(DamageCalculator.Scale(12f, 1.5f), Is.EqualTo(18f));
            Assert.That(DamageCalculator.Scale(12f, 0.5f), Is.EqualTo(6f));
        }

        [Test]
        public void Scale_NeverGoesBelowZero()
        {
            Assert.That(DamageCalculator.Scale(12f, -1f), Is.EqualTo(0f));
            Assert.That(DamageCalculator.Scale(-3f, 2f), Is.EqualTo(0f));
            Assert.That(DamageCalculator.Scale(12f, 0f), Is.EqualTo(0f));
        }
    }
}
