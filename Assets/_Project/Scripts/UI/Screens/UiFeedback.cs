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
        /// <summary>Vigor spent on surface work (4h Checkpoint B): a soft tick and a light tap.</summary>
        Vigor,
        /// <summary>A harvest into the storeroom (4h Checkpoint B): a small chime and a gentle pulse.</summary>
        Harvest,
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
        [SerializeField] MMF_Player m_Vigor;
        [SerializeField] MMF_Player m_Harvest;

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

        /// <summary>The surface's moments (4h Checkpoint B).</summary>
        public void ConfigureSurface(MMF_Player vigor, MMF_Player harvest)
        {
            m_Vigor = vigor;
            m_Harvest = harvest;
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
                UiMoment.Vigor => Instance.m_Vigor,
                UiMoment.Harvest => Instance.m_Harvest,
                _ => Instance.m_Confirm,
            };
            if (player != null) player.PlayFeedbacks();
        }
    }
}
