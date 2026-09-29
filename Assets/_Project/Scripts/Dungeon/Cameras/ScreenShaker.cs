using Hearthdelve.Core.Events;
using Hearthdelve.Core.Services;
using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Cameras
{
    /// <summary>
    /// Turns <see cref="ScreenShakeRequested"/> into a Cinemachine impulse, scaled by the
    /// player's screen-shake setting (0 disables it).
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class ScreenShaker : MonoBehaviour
    {
        CinemachineImpulseSource m_Source;

        void Awake() => m_Source = GetComponent<CinemachineImpulseSource>();
        void OnEnable() => EventBus<ScreenShakeRequested>.Subscribe(OnShake);
        void OnDisable() => EventBus<ScreenShakeRequested>.Unsubscribe(OnShake);

        void OnShake(ScreenShakeRequested evt)
        {
            float force = evt.Force * GameSettings.ScreenShakeScale;
            if (force > 0f) m_Source.GenerateImpulseWithForce(force);
        }
    }
}
