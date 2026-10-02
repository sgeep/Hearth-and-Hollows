using Hearthdelve.Core.Haptics;
using Lofelt.NiceVibrations;
using UnityEngine;

namespace Hearthdelve.Shared.Haptics
{
    /// <summary>
    /// Sends motor strengths to the current gamepad through Nice Vibrations. Each call loads
    /// a short rumble, so if the service stops updating the motors switch off by themselves.
    /// With no supported controller connected it reports itself unavailable and does nothing.
    /// </summary>
    public sealed class NiceVibrationsHapticOutput : IHapticOutput
    {
        /// <summary>How long one update keeps the motors running if no further update arrives.</summary>
        const int k_HoldMs = 120;

        GamepadRumble m_Rumble = new()
        {
            durationsMs = new[] { k_HoldMs },
            totalDurationMs = k_HoldMs,
            lowFrequencyMotorSpeeds = new float[1],
            highFrequencyMotorSpeeds = new float[1],
        };

        public bool IsAvailable => GamepadRumbler.IsConnected();

        public void SetMotors(float low, float high)
        {
            m_Rumble.lowFrequencyMotorSpeeds[0] = Mathf.Clamp01(low);
            m_Rumble.highFrequencyMotorSpeeds[0] = Mathf.Clamp01(high);
            GamepadRumbler.Load(m_Rumble);
            GamepadRumbler.Play();
        }

        public void Stop() => GamepadRumbler.Stop();
    }
}
