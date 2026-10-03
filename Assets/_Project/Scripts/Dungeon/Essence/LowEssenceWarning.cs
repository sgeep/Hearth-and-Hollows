using Hearthdelve.Core.Haptics;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Essence
{
    /// <summary>
    /// The low-Essence heartbeat (GDD §9A): below the low threshold a heartbeat plays (sound and the
    /// Heartbeat.Warning haptic, one feedback), speeding up as Essence falls towards zero
    /// (<see cref="HapticMath.HeartbeatInterval"/>, intervals in <see cref="EssenceConfig"/>). It stops
    /// above the threshold, at death and in god mode. The HUD shows the low state from
    /// <c>EssenceChanged.IsLow</c>.
    /// </summary>
    [RequireComponent(typeof(EssenceHealth))]
    public sealed class LowEssenceWarning : MonoBehaviour
    {
        [SerializeField, Tooltip("One heartbeat: sound and haptic together.")]
        MMF_Player m_Heartbeat;

        EssenceHealth m_Health;
        float m_NextBeat;

        public bool IsWarning { get; private set; }
        /// <summary>Heartbeats played so far (for tests and debugging).</summary>
        public int Beats { get; private set; }
        /// <summary>Seconds between beats right now, or -1 when not warning.</summary>
        public float Interval { get; private set; } = -1f;

        public void Configure(MMF_Player heartbeat) => m_Heartbeat = heartbeat;

        void Awake() => m_Health = GetComponent<EssenceHealth>();

        void Update()
        {
            EssenceMeter meter = m_Health.Essence;
            EssenceConfig config = m_Health.Config;
            if (meter == null || config == null || m_Health.GodMode || m_Health.CurrentHealth <= 0f || meter.Paused)
            {
                Stop();
                return;
            }
            Interval = HapticMath.HeartbeatInterval(meter.Normalized, config.essence.lowThreshold, config.lowWarningSlowInterval, config.lowWarningFastInterval);
            if (Interval < 0f)
            {
                Stop();
                return;
            }
            if (!IsWarning) m_NextBeat = Time.time;
            IsWarning = true;
            if (Time.time < m_NextBeat) return;
            m_Heartbeat?.PlayFeedbacks(transform.position);
            Beats++;
            m_NextBeat = Time.time + Interval;
        }

        void Stop()
        {
            IsWarning = false;
            Interval = -1f;
        }
    }
}
