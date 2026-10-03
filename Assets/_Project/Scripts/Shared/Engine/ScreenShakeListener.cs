using Hearthdelve.Core.Events;
using Hearthdelve.Core.Services;
using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Turns <see cref="ScreenShakeRequested"/> into a Cinemachine impulse, scaled by the
    /// player's screen shake setting (0 turns shake off).
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class ScreenShakeListener : MonoBehaviour
    {
        CinemachineImpulseSource m_Source;

        /// <summary>The force of the last impulse actually generated (after settings).</summary>
        public float LastForce { get; private set; }

        void Awake() => m_Source = GetComponent<CinemachineImpulseSource>();
        void OnEnable() => EventBus<ScreenShakeRequested>.Subscribe(OnShake);
        void OnDisable() => EventBus<ScreenShakeRequested>.Unsubscribe(OnShake);

        void OnShake(ScreenShakeRequested e)
        {
            float force = e.Force * Mathf.Max(0f, GameSettings.ScreenShakeScale);
            if (force <= 0f) return;
            LastForce = force;
            m_Source.GenerateImpulseWithForce(force);
        }
    }
}
