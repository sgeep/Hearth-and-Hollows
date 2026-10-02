using Hearthdelve.Core.Movement;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class FacingTests
    {
        [TestCase(1f, -1f, Facing4.FrontRight)]
        [TestCase(-1f, -1f, Facing4.FrontLeft)]
        [TestCase(1f, 1f, Facing4.BackRight)]
        [TestCase(-1f, 1f, Facing4.BackLeft)]
        public void Diagonals_MapToTheirFacing(float x, float y, Facing4 expected)
        {
            Assert.That(FacingLogic.FromDirection(x, y, Facing4.FrontRight), Is.EqualTo(expected));
            Assert.That(FacingLogic.FromDirection(x, y, Facing4.BackLeft), Is.EqualTo(expected));
        }

        [Test]
        public void StraightUpOrDown_KeepsTheHorizontalSide()
        {
            Assert.That(FacingLogic.FromDirection(0f, 1f, Facing4.FrontLeft), Is.EqualTo(Facing4.BackLeft));
            Assert.That(FacingLogic.FromDirection(0f, 1f, Facing4.FrontRight), Is.EqualTo(Facing4.BackRight));
            Assert.That(FacingLogic.FromDirection(0f, -1f, Facing4.BackLeft), Is.EqualTo(Facing4.FrontLeft));
        }

        [Test]
        public void StraightSideways_KeepsFrontOrBack()
        {
            Assert.That(FacingLogic.FromDirection(1f, 0f, Facing4.BackLeft), Is.EqualTo(Facing4.BackRight));
            Assert.That(FacingLogic.FromDirection(-1f, 0f, Facing4.FrontRight), Is.EqualTo(Facing4.FrontLeft));
        }

        [Test]
        public void NoInput_KeepsTheCurrentFacing()
        {
            foreach (Facing4 facing in System.Enum.GetValues(typeof(Facing4)))
                Assert.That(FacingLogic.FromDirection(0f, 0f, facing), Is.EqualTo(facing));
        }

        [Test]
        public void InputInsideTheDeadZone_IsIgnored()
        {
            Assert.That(FacingLogic.FromDirection(-0.1f, 0.1f, Facing4.FrontRight), Is.EqualTo(Facing4.FrontRight));
            Assert.That(FacingLogic.FromDirection(-0.1f, 0.9f, Facing4.FrontRight), Is.EqualTo(Facing4.BackRight));
        }

        [Test]
        public void Combine_RoundTrips()
        {
            foreach (Facing4 facing in System.Enum.GetValues(typeof(Facing4)))
                Assert.That(FacingLogic.Combine(FacingLogic.IsFront(facing), FacingLogic.IsRight(facing)), Is.EqualTo(facing));
        }
    }
}
