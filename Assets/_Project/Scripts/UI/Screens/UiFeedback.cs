using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>The menus' moments.</summary>
    public enum UiMoment
    {
        /// <summary>An ordinary choice: a click, no vibration.</summary>
        Confirm,
        /// <summary>A step forward in the day (open the doors, descend, sleep, start or continue): a click and a very light tap.</summary>
        Commit,
        /// <summary>An upgrade bought: a chime and a gentle success pulse.</summary>
        Buy,
        /// <summary>A Results line counting in: a soft tick.</summary>
        Tick,
        /// <summary>The evening's takings: a chime (no vibration).</summary>
        Takings,
    }

    /// <summary>
    /// The menus' sound and (rarely) touch (4c step 6): one combined feedback per moment, kept small, because menus are
    /// used over and over; only commitments and purchases are felt at all. One per scene; screens and buttons play
    /// through it, and with none (tests, a scene without one) nothing plays.
    /// </summary>
    public sealed class UiFeedback : MonoBehaviour
    {
        [SerializeField] MMF_Player m_Confirm;
        [SerializeField] MMF_Player m_Commit;
        [SerializeField] MMF_Player m_Buy;
        [SerializeField] MMF_Player m_Tick;
        [SerializeField] MMF_Player m_Takings;

        public static UiFeedback Instance { get; private set; }
        /// <summary>The last moment asked for (tests).</summary>
        public static UiMoment? Last { get; private set; }

        public void Configure(MMF_Player confirm, MMF_Player commit, MMF_Player buy, MMF_Player tick, MMF_Player takings)
        {
            m_Confirm = confirm;
            m_Commit = commit;
            m_Buy = buy;
            m_Tick = tick;
            m_Takings = takings;
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(UiMoment moment)
        {
            Last = moment;
            if (Instance == null) return;
            MMF_Player player = moment switch
            {
                UiMoment.Commit => Instance.m_Commit,
                UiMoment.Buy => Instance.m_Buy,
                UiMoment.Tick => Instance.m_Tick,
                UiMoment.Takings => Instance.m_Takings,
                _ => Instance.m_Confirm,
            };
            if (player != null) player.PlayFeedbacks();
        }
    }
}
