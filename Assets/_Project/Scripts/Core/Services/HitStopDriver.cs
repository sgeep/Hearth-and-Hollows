using Hearthdelve.Core.Events;
using UnityEngine;

namespace Hearthdelve.Core.Services
{
    /// <summary>
    /// Applies <see cref="HitStop"/> to time via <see cref="GamePause"/>. Listens for <see cref="HitStopRequested"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class HitStopDriver : MonoBehaviour
    {
        readonly HitStop m_HitStop = new();

        void OnEnable() => EventBus<HitStopRequested>.Subscribe(OnRequested);

        void OnDisable()
        {
            EventBus<HitStopRequested>.Unsubscribe(OnRequested);
            m_HitStop.Cancel();
            GamePause.SetHitStop(false);
        }

        void OnRequested(HitStopRequested evt)
        {
            if (!GameSettings.HitStopEnabled) return;
            m_HitStop.Request(evt.Duration);
            GamePause.SetHitStop(m_HitStop.IsActive);
        }

        void Update() => GamePause.SetHitStop(m_HitStop.Tick(Time.unscaledDeltaTime));
    }
}
