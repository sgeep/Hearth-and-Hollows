using Hearthdelve.Core.Haptics;
using Hearthdelve.Core.Services;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class HapticMathTests
    {
        static HapticSettings Settings(bool enabled = true, float intensity = 1f, bool reduced = false, float cap = 0.4f) =>
            new(enabled, intensity, reduced, cap);

        [Test]
        public void VibrationOff_IsSilent()
        {
            Assert.That(HapticMath.ApplySettings(1f, Settings(enabled: false)), Is.EqualTo(0f));
        }

        [Test]
        public void IntensitySlider_ScalesEveryPattern()
        {
            Assert.That(HapticMath.ApplySettings(0.8f, Settings(intensity: 0.5f)), Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(HapticMath.ApplySettings(0.8f, Settings(intensity: 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void ReducedOption_CapsStrongPatterns_ButLeavesWeakOnesAlone()
        {
            Assert.That(HapticMath.ApplySettings(1f, Settings(reduced: true, cap: 0.4f)), Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(HapticMath.ApplySettings(0.2f, Settings(reduced: true, cap: 0.4f)), Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void OutOfRangeValues_AreClamped()
        {
            Assert.That(HapticMath.ApplySettings(3f, Settings(intensity: 2f)), Is.EqualTo(1f));
            Assert.That(HapticMath.ApplySettings(-1f, Settings()), Is.EqualTo(0f));
        }

        [Test]
        public void GameSettings_FeedTheSameMaths()
        {
            GameSettings.VibrationEnabled = true;
            GameSettings.VibrationIntensity = 0.5f;
            GameSettings.ReducedVibration = false;
            try
            {
                Assert.That(HapticMath.ApplySettings(1f, GameSettings.Haptics), Is.EqualTo(0.5f).Within(1e-5f));
                GameSettings.ReducedVibration = true;
                Assert.That(HapticMath.ApplySettings(1f, GameSettings.Haptics),
                    Is.EqualTo(GameSettings.ReducedVibrationCap).Within(1e-5f));
                GameSettings.VibrationEnabled = false;
                Assert.That(HapticMath.ApplySettings(1f, GameSettings.Haptics), Is.EqualTo(0f));
            }
            finally
            {
                GameSettings.VibrationEnabled = true;
                GameSettings.VibrationIntensity = 1f;
                GameSettings.ReducedVibration = false;
            }
        }

        [Test]
        public void Ramp_MapsPourSpeedToRumble_Linearly()
        {
            var pour = new HapticRamp(0f, 1f, 0.1f, 0.7f);
            Assert.That(pour.Evaluate(0f), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(pour.Evaluate(0.5f), Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(pour.Evaluate(1f), Is.EqualTo(0.7f).Within(1e-5f));
        }

        [Test]
        public void Ramp_ClampsOutsideItsInputRange()
        {
            var burn = new HapticRamp(0.7f, 1f, 0f, 1f);
            Assert.That(burn.Evaluate(0.2f), Is.EqualTo(0f));
            Assert.That(burn.Evaluate(1.5f), Is.EqualTo(1f));
        }

        [Test]
        public void Ramp_WithExponentAboveOne_StaysLowUntilLate()
        {
            var linear = new HapticRamp(0f, 1f, 0f, 1f);
            var rising = new HapticRamp(0f, 1f, 0f, 1f, exponent: 2f);
            Assert.That(rising.Evaluate(0.5f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(rising.Evaluate(0.5f), Is.LessThan(linear.Evaluate(0.5f)));
            Assert.That(rising.Evaluate(1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Ramp_NeverDecreasesAsTheValueRises()
        {
            var ramp = new HapticRamp(0.2f, 0.9f, 0.05f, 0.8f, exponent: 1.5f);
            float previous = ramp.Evaluate(0f);
            for (float v = 0.05f; v <= 1f; v += 0.05f)
            {
                float current = ramp.Evaluate(v);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous - 1e-6f));
                previous = current;
            }
        }

        [Test]
        public void Ramp_WithNoInputSpan_ActsAsAStep()
        {
            var step = new HapticRamp(0.5f, 0.5f, 0.1f, 0.9f);
            Assert.That(step.Evaluate(0.4f), Is.EqualTo(0.1f));
            Assert.That(step.Evaluate(0.5f), Is.EqualTo(0.9f));
        }

        [Test]
        public void Layer_TakesTheStronger_NotTheSum()
        {
            Assert.That(HapticMath.Layer(0.6f, 0.7f), Is.EqualTo(0.7f));
            Assert.That(HapticMath.Layer(0.6f, 0.2f), Is.EqualTo(0.6f));
            Assert.That(HapticMath.Layer(0.9f, 0.9f), Is.EqualTo(0.9f));
        }

        [Test]
        public void Heartbeat_IsSilentAboveTheLowThreshold()
        {
            Assert.That(HapticMath.HeartbeatInterval(0.5f, 0.25f, 1.2f, 0.4f), Is.EqualTo(-1f));
        }

        [Test]
        public void Heartbeat_SpeedsUpAsEssenceFalls()
        {
            float atThreshold = HapticMath.HeartbeatInterval(0.25f, 0.25f, 1.2f, 0.4f);
            float halfway = HapticMath.HeartbeatInterval(0.125f, 0.25f, 1.2f, 0.4f);
            float empty = HapticMath.HeartbeatInterval(0f, 0.25f, 1.2f, 0.4f);
            Assert.That(atThreshold, Is.EqualTo(1.2f).Within(1e-5f));
            Assert.That(halfway, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(empty, Is.EqualTo(0.4f).Within(1e-5f));
        }

        [Test]
        public void Heartbeat_WithNoThreshold_NeverPlays()
        {
            Assert.That(HapticMath.HeartbeatInterval(0f, 0f, 1.2f, 0.4f), Is.EqualTo(-1f));
        }
    }
}
