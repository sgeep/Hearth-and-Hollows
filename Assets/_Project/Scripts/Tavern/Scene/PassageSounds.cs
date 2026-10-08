using Hearthdelve.Core.Events;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// 4i-C (a gap in the feedback audit): going through a door or up and down the stairs is heard, as the screen covers. One
    /// combined feedback for each, beside the tavern's other moments.
    /// </summary>
    public sealed class PassageSounds : MonoBehaviour
    {
        [SerializeField] MMF_Player m_Door;
        [SerializeField] MMF_Player m_Stairs;

        public MMF_Player Door => m_Door;
        public MMF_Player Stairs => m_Stairs;
        /// <summary>How many doors and how many stair passages have been heard (tests).</summary>
        public int Doors { get; private set; }
        public int StairsHeard { get; private set; }

        public void Configure(MMF_Player door, MMF_Player stairs)
        {
            m_Door = door;
            m_Stairs = stairs;
        }

        void OnEnable() => EventBus<AreaPassageStarted>.Subscribe(OnStarted);
        void OnDisable() => EventBus<AreaPassageStarted>.Unsubscribe(OnStarted);

        void OnStarted(AreaPassageStarted e)
        {
            MMF_Player player = e.Kind == PassageKind.Door ? m_Door : m_Stairs;
            if (player == null) return;
            player.PlayFeedbacks();
            if (e.Kind == PassageKind.Door) Doors++;
            else StairsHeard++;
        }
    }
}
